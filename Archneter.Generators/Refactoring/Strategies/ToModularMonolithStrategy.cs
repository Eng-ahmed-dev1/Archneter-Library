using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.Refactoring.Strategies;

/// <summary>
/// Refactors an existing project into a Modular Monolith architecture:
///   ProjectName.Host          (Web API entry point)
///   ProjectName.Shared        (cross-module contracts)
///   ProjectName.Modules.{X}   (one classlib per detected module/feature)
/// </summary>
public class ToModularMonolithStrategy : BaseRefactoringStrategy
{
    public ToModularMonolithStrategy(ICliService cli) : base(cli) { }
    public override async Task ExecuteAsync(RefactorOptions options, AnalyzedProject analyzedProject)
    {
        var name     = analyzedProject.ProjectName;
        var root     = options.ProjectDirectory;
        var isDryRun = options.DryRun;
        IsDryRun = isDryRun;

        WriteHeader($"Refactoring → Modular Monolith  [{name}]");

        if (!options.SkipBackup)
            CreateBackup(root, isDryRun);

        // ── Infer modules ────────────────────────────────────────────────
        var modules = InferModules(analyzedProject.Files);
        WriteInfo($"  Detected modules: {string.Join(", ", modules)}");

        // ── Create projects ──────────────────────────────────────────────
        WriteHeader("Creating projects");

        await RunDotnetAsync(root, "new", "webapi",  "-n", $"{name}.Host",   "--force");
        await RunDotnetAsync(root, "new", "classlib", "-n", $"{name}.Shared", "--force");

        if (!analyzedProject.HasSolution)
            await RunDotnetAsync(root, "new", "sln", "-n", name);

        var slnPath = isDryRun
            ? Path.Combine(root, $"{name}.sln")
            : Directory.GetFiles(root, $"{name}.sln*").FirstOrDefault() ?? Path.Combine(root, $"{name}.sln");

        var allProjects = new List<string> { $"{name}.Host", $"{name}.Shared" };
        foreach (var module in modules)
        {
            var moduleName = $"{name}.Modules.{module}";
            await RunDotnetAsync(root, "new", "classlib", "-n", moduleName, "--force");
            allProjects.Add(moduleName);
        }

        var projectPaths = allProjects.Select(p => Path.Combine(root, p, $"{p}.csproj"));
        await Cli.AddMultipleToSolutionAsync(slnPath, projectPaths);

        // Modules → Shared
        foreach (var module in modules)
        {
            var moduleName = $"{name}.Modules.{module}";
            var moduleCsproj = Path.Combine(root, moduleName, $"{moduleName}.csproj");
            var sharedCsproj = Path.Combine(root, moduleName, "..", $"{name}.Shared", $"{name}.Shared.csproj");
            await Cli.AddReferenceAsync(moduleCsproj, sharedCsproj);
        }

        // Host → Modules
        var hostCsproj = Path.Combine(root, $"{name}.Host", $"{name}.Host.csproj");
        var moduleCsprojs = modules.Select(m => Path.Combine(root, $"{name}.Host", "..", $"{name}.Modules.{m}", $"{name}.Modules.{m}.csproj"));
        if (moduleCsprojs.Any())
            await Cli.AddReferencesAsync(hostCsproj, moduleCsprojs);

        // ── Move files ───────────────────────────────────────────────────
        WriteHeader("Moving files");

        foreach (var file in analyzedProject.Files)
        {
            var module    = InferModule(file.FileName, modules);
            var subFolder = ResolveSubFolder(file);

            string targetProject;
            string newNs;

            if (file.TargetLayer == ProjectLayer.Shared)
            {
                targetProject = $"{name}.Shared";
                newNs = NamespaceRewriter.BuildNamespace(name, "Shared", subFolder);
            }
            else if (file.TargetLayer == ProjectLayer.Presentation)
            {
                targetProject = $"{name}.Host";
                if (file.FileName == "Program.cs" || file.FileName == "Startup.cs")
                {
                    subFolder = "";
                    newNs = $"{name}.Host";
                }
                else
                {
                    subFolder = "Controllers";
                    newNs = NamespaceRewriter.BuildNamespace(name, "Host", "Controllers");
                }
            }
            else
            {
                targetProject = $"{name}.Modules.{module}";
                newNs = NamespaceRewriter.BuildNamespace(name, $"Modules.{module}", subFolder);
            }

            var destPath = Path.Combine(root, targetProject, subFolder, file.FileName);
            file.TargetNamespace = newNs;
            file.SourcePath = MoveFile(file.SourcePath, destPath, name, newNs);
        }

        RewriteNamespaces(analyzedProject.Files);

        if (options.DeepRefactor)
        {
            WriteHeader("Applying Deep Refactoring");
            ApplyDeepRefactoring(analyzedProject.Files);
        }

        foreach (var proj in allProjects)
        {
            PropagatePackageReferences(analyzedProject, Path.Combine(root, proj, $"{proj}.csproj"));
        }

        ReportUnclassified(analyzedProject);

        WriteHeader("Done");
        WriteSuccess($"✅ Refactoring complete → Modular Monolith! ({modules.Count} modules)");
        if (isDryRun) WriteWarning("\n[dry-run] No files were actually created or moved.");
    }

    // ──────────────────────────────────────────────────────────────────────────

    private static HashSet<string> InferModules(List<ClassifiedFile> files)
    {
        var modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Try to infer from "Features/{Module}", "Modules/{Module}", or "UseCases/{Module}"
        foreach (var file in files)
        {
            var parts = file.SourcePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var featuresIdx = Array.FindIndex(parts, p => p.Equals("Features", StringComparison.OrdinalIgnoreCase) || 
                                                          p.Equals("Modules", StringComparison.OrdinalIgnoreCase) ||
                                                          p.Equals("UseCases", StringComparison.OrdinalIgnoreCase));
            if (featuresIdx >= 0 && featuresIdx + 1 < parts.Length)
            {
                var moduleName = parts[featuresIdx + 1];
                if (!string.IsNullOrWhiteSpace(moduleName) && !moduleName.EndsWith(".cs"))
                {
                    modules.Add(moduleName);
                }
            }
        }

        if (modules.Any()) return modules;

        // 2. Fallback to name-based parsing with CQRS prefix/suffix stripping
        var suffixes = new[] { "Controller", "Service", "Repository", "Handler", "Command", "Query", "Entity", "Model", "Validator", "DbContext" };
        var prefixes = new[] { "Create", "Update", "Delete", "Remove", "Add", "GetAll", "Get" };

        foreach (var file in files)
        {
            var domain = Path.GetFileNameWithoutExtension(file.FileName);
            
            // Recursively strip suffixes (e.g. CommandHandler -> Command -> Entity)
            bool stripped;
            do
            {
                stripped = false;
                foreach (var suffix in suffixes)
                {
                    if (domain.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    {
                        domain = domain[..^suffix.Length].Trim();
                        stripped = true;
                    }
                }
            } while (stripped && domain.Length > 0);

            // Strip prefixes
            foreach (var prefix in prefixes)
            {
                if (domain.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    domain = domain[prefix.Length..].Trim();
                    if (domain.StartsWith("By", StringComparison.OrdinalIgnoreCase)) domain = domain[2..].Trim();
                }
            }

            // Also ignore interfaces starting with I
            if (domain.StartsWith("I") && domain.Length > 2 && char.IsUpper(domain[1]))
            {
                domain = domain[1..];
            }

            if (!string.IsNullOrWhiteSpace(domain) && domain.Length > 2)
            {
                modules.Add(domain);
            }
        }

        return modules.Count > 0 ? modules : new HashSet<string> { "Core" };
    }

    private static string InferModule(string fileName, HashSet<string> modules)
    {
        foreach (var module in modules)
            if (fileName.Contains(module, StringComparison.OrdinalIgnoreCase))
                return module;
        return modules.First();
    }

    private static string ResolveSubFolder(ClassifiedFile file) => file.TargetLayer switch
    {
        ProjectLayer.Domain         => "Domain",
        ProjectLayer.Application    => "Application",
        ProjectLayer.Infrastructure => "Infrastructure",
        _                           => "Common",
    };

    // Reference helper removed as it's no longer used

    private void ReportUnclassified(AnalyzedProject project)
    {
        if (!project.UnclassifiedFiles.Any()) return;
        WriteWarning("\n⚠️  Unclassified files (move manually):");
        foreach (var f in project.UnclassifiedFiles)
            WriteWarning($"   • {Path.GetFileName(f)}");
    }
}