using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.Refactoring.Strategies;

/// <summary>
/// Refactors an existing project into a Microservices architecture:
///   ProjectName.Gateway               (API Gateway)
///   ProjectName.Shared                (shared contracts/models)
///   ProjectName.Services.{X}/         (one web API per inferred service)
///     ProjectName.Services.{X}.Domain
///     ProjectName.Services.{X}.Application
///     ProjectName.Services.{X}.Infrastructure
///     ProjectName.Services.{X}.Api
/// </summary>
public class ToMicroservicesStrategy : BaseRefactoringStrategy
{
    public ToMicroservicesStrategy(ICliService cli) : base(cli) { }
    public override async Task ExecuteAsync(RefactorOptions options, AnalyzedProject analyzedProject)
    {
        var name     = analyzedProject.ProjectName;
        var root     = options.ProjectDirectory;
        var isDryRun = options.DryRun;
        IsDryRun = isDryRun;

        WriteHeader($"Refactoring → Microservices  [{name}]");

        if (!options.SkipBackup)
            CreateBackup(root, isDryRun);

        // ── Infer services ───────────────────────────────────────────────
        var services = InferServices(analyzedProject.Files);
        WriteInfo($"  Detected services: {string.Join(", ", services)}");

        // ── Create shared infrastructure ─────────────────────────────────
        WriteHeader("Creating shared projects");

        await RunDotnetAsync(root, "new", "webapi",  "-n", $"{name}.Gateway", "--force");
        await RunDotnetAsync(root, "new", "classlib", "-n", $"{name}.Shared",  "--force");

        if (!analyzedProject.HasSolution)
            await RunDotnetAsync(root, "new", "sln", "-n", name);

        var slnPath = isDryRun
            ? Path.Combine(root, $"{name}.sln")
            : Directory.GetFiles(root, $"{name}.sln*").FirstOrDefault() ?? Path.Combine(root, $"{name}.sln");

        var allProjects = new List<string> { $"{name}.Gateway", $"{name}.Shared" };

        // ── Create service projects ──────────────────────────────────────
        WriteHeader("Creating service projects");

        foreach (var service in services)
        {
            var layers = new[]
            {
                ($"{name}.Services.{service}.Domain",         "classlib"),
                ($"{name}.Services.{service}.Application",    "classlib"),
                ($"{name}.Services.{service}.Infrastructure", "classlib"),
                ($"{name}.Services.{service}.Api",            "webapi"),
            };

            foreach (var (proj, tmpl) in layers)
            {
                await RunDotnetAsync(root, "new", tmpl, "-n", proj, "--force");
                allProjects.Add(proj);
            }
        }

        await Cli.AddMultipleToSolutionAsync(slnPath, allProjects.Select(p => Path.Combine(root, p, $"{p}.csproj")));

        foreach (var service in services)
        {
            var domainProj = Path.Combine(root, $"{name}.Services.{service}.Domain", "..", $"{name}.Services.{service}.Domain", $"{name}.Services.{service}.Domain.csproj");
            var appProj = Path.Combine(root, $"{name}.Services.{service}.Application", "..", $"{name}.Services.{service}.Application", $"{name}.Services.{service}.Application.csproj");
            var infraProj = Path.Combine(root, $"{name}.Services.{service}.Infrastructure", "..", $"{name}.Services.{service}.Infrastructure", $"{name}.Services.{service}.Infrastructure.csproj");
            var sharedProj = Path.Combine(root, $"{name}.Services.{service}.Domain", "..", $"{name}.Shared", $"{name}.Shared.csproj");

            await Cli.AddReferenceAsync(Path.Combine(root, $"{name}.Services.{service}.Application", $"{name}.Services.{service}.Application.csproj"), domainProj);
            await Cli.AddReferencesAsync(Path.Combine(root, $"{name}.Services.{service}.Infrastructure", $"{name}.Services.{service}.Infrastructure.csproj"), new[] { domainProj, appProj });
            await Cli.AddReferencesAsync(Path.Combine(root, $"{name}.Services.{service}.Api", $"{name}.Services.{service}.Api.csproj"), new[] { appProj, infraProj });
            await Cli.AddReferenceAsync(Path.Combine(root, $"{name}.Services.{service}.Domain", $"{name}.Services.{service}.Domain.csproj"), sharedProj);
        }

        // ── Move files ───────────────────────────────────────────────────
        WriteHeader("Moving files");

        foreach (var file in analyzedProject.Files)
        {
            var service   = InferService(file.FileName, services);
            var (targetProject, subFolder) = ResolveDestination(file, name, service);
            var destPath = Path.Combine(root, targetProject, subFolder, file.FileName);
            var layerPart = LayerSuffix(file.TargetLayer, service);
            var newNs    = NamespaceRewriter.BuildNamespace(name, $"Services.{service}.{layerPart}", subFolder);

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
        WriteSuccess($"✅ Refactoring complete → Microservices! ({services.Count} services)");
        WriteWarning("⚠️  Remember: each service should have its own database and communicate via messaging or HTTP.");
        if (isDryRun) WriteWarning("\n[dry-run] No files were actually created or moved.");
    }

    // ──────────────────────────────────────────────────────────────────────────

    private static HashSet<string> InferServices(List<ClassifiedFile> files)
    {
        var services = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Try to infer from "Features/{Module}", "Modules/{Module}", or "UseCases/{Module}"
        foreach (var file in files)
        {
            var parts = file.SourcePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var featuresIdx = Array.FindIndex(parts, p => p.Equals("Features", StringComparison.OrdinalIgnoreCase) || 
                                                          p.Equals("Modules", StringComparison.OrdinalIgnoreCase) ||
                                                          p.Equals("UseCases", StringComparison.OrdinalIgnoreCase));
            if (featuresIdx >= 0 && featuresIdx + 1 < parts.Length)
            {
                var serviceName = parts[featuresIdx + 1];
                if (!string.IsNullOrWhiteSpace(serviceName) && !serviceName.EndsWith(".cs"))
                {
                    services.Add(serviceName);
                }
            }
        }

        if (services.Any()) return services;

        // 2. Fallback to name-based parsing with CQRS prefix/suffix stripping
        var suffixes = new[] { "Controller", "Service", "Repository", "Handler", "Command", "Query", "Entity", "Model", "Validator", "DbContext" };
        var prefixes = new[] { "Create", "Update", "Delete", "Remove", "Add", "GetAll", "Get" };

        foreach (var file in files)
        {
            var domain = Path.GetFileNameWithoutExtension(file.FileName);
            
            // Recursively strip suffixes
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
                services.Add(domain);
            }
        }

        return services.Count > 0 ? services : new HashSet<string> { "Core" };
    }

    private static string InferService(string fileName, HashSet<string> services)
    {
        foreach (var svc in services)
            if (fileName.Contains(svc, StringComparison.OrdinalIgnoreCase))
                return svc;
        return services.First();
    }

    private static (string Project, string SubFolder) ResolveDestination(ClassifiedFile file, string name, string service)
    {
        if (file.TargetLayer == ProjectLayer.Presentation && (file.FileName == "Program.cs" || file.FileName == "Startup.cs"))
            return ($"{name}.Services.{service}.Api", "");

        return file.TargetLayer switch
        {
            ProjectLayer.Domain         => ($"{name}.Services.{service}.Domain",         "Entities"),
            ProjectLayer.Application    => ($"{name}.Services.{service}.Application",    "Services"),
            ProjectLayer.Infrastructure => ($"{name}.Services.{service}.Infrastructure", "Repositories"),
            ProjectLayer.Presentation   => ($"{name}.Services.{service}.Api",            "Controllers"),
            _                           => ($"{name}.Services.{service}.Application",    "Common"),
        };
    }

    private static string LayerSuffix(ProjectLayer layer, string service) => layer switch
    {
        ProjectLayer.Domain         => "Domain",
        ProjectLayer.Application    => "Application",
        ProjectLayer.Infrastructure => "Infrastructure",
        ProjectLayer.Presentation   => "Api",
        _                           => "Application"
    };

    // AddRef helper removed as it's no longer used

    private void ReportUnclassified(AnalyzedProject project)
    {
        if (!project.UnclassifiedFiles.Any()) return;
        WriteWarning("\n⚠️  Unclassified files (move manually):");
        foreach (var f in project.UnclassifiedFiles)
            WriteWarning($"   • {Path.GetFileName(f)}");
    }
}