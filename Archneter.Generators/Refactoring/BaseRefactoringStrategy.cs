using Archneter.Core.Models;
using System.Diagnostics;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.Refactoring;

/// <summary>
/// Provides shared infrastructure for all refactoring strategies:
/// backup, directory creation, file moving, namespace rewriting,
/// dotnet CLI calls, and console reporting.
/// </summary>
public abstract class BaseRefactoringStrategy : IRefactoringStrategy
{
    protected bool IsDryRun { get; set; }
    protected readonly ICliService Cli;

    protected BaseRefactoringStrategy(ICliService cli)
    {
        Cli = cli;
    }

    public abstract Task ExecuteAsync(RefactorOptions options, AnalyzedProject analyzedProject);

    // ──────────────────────────────────────────────────────────────────────────
    // Backup
    // ──────────────────────────────────────────────────────────────────────────

    protected void CreateBackup(string sourceDirectory, bool dryRun)
    {
        var timestamp  = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupName = $"_backup_{timestamp}";
        var backupPath = Path.Combine(Directory.GetParent(sourceDirectory)!.FullName, backupName);

        if (dryRun)
        {
            WriteInfo($"[dry-run] Would create backup at: {backupPath}");
            return;
        }

        WriteInfo($"📦 Creating backup → {backupPath}");
        CopyDirectory(sourceDirectory, backupPath);
        WriteSuccess("✅ Backup created successfully.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Directory & file helpers
    // ──────────────────────────────────────────────────────────────────────────

    protected void EnsureDirectory(string path)
    {
        if (IsDryRun)
        {
            WriteInfo($"[dry-run] mkdir: {path}");
            return;
        }
        Directory.CreateDirectory(path);
    }

    /// <summary>
    /// Moves a source .cs file to <paramref name="destinationPath"/>.
    /// </summary>
    protected string MoveFile(string sourcePath, string destinationPath, string oldRootNamespace, string newNamespace)
    {
        if (IsDryRun)
        {
            WriteInfo($"[dry-run] MOVE  {Shorten(sourcePath)}");
            WriteInfo($"         → {Shorten(destinationPath)}");
            WriteInfo($"         namespace: {oldRootNamespace}.* → {newNamespace}");
            return destinationPath;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        int retries = 3;
        while (true)
        {
            try
            {
                if (File.Exists(destinationPath)) File.Delete(destinationPath);
                File.Copy(sourcePath, destinationPath, overwrite: true);
                break;
            }
            catch (IOException) when (retries > 0)
            {
                retries--;
                System.Threading.Thread.Sleep(500); // Wait for background file locks to release
            }
        }
        
        File.Delete(sourcePath);

        WriteSuccess($"  ✔  {Path.GetFileName(sourcePath)} → {Shorten(destinationPath)}");
        return destinationPath;
    }

    protected void RewriteNamespaces(IEnumerable<ClassifiedFile> files)
    {
        if (IsDryRun) return;

        var map = files
            .Where(f => !string.IsNullOrWhiteSpace(f.OriginalNamespace) && !string.IsNullOrWhiteSpace(f.TargetNamespace))
            .GroupBy(f => f.OriginalNamespace)
            .ToDictionary(g => g.Key, g => g.Select(x => x.TargetNamespace).Distinct().ToList());

        foreach (var file in files.Where(f => !string.IsNullOrEmpty(f.TargetNamespace)))
        {
            NamespaceRewriter.Rewrite(file.SourcePath, file.TargetNamespace, map);
        }
    }

    protected void ApplyDeepRefactoring(IEnumerable<ClassifiedFile> files)
    {
        if (IsDryRun) return;
        DeepRefactorer.Apply(files);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // dotnet CLI
    // ──────────────────────────────────────────────────────────────────────────

    protected void PropagatePackageReferences(AnalyzedProject analyzedProject, string targetProjectCsproj)
    {
        if (IsDryRun) return;

        // Collect all distinct package references from all original projects
        var packages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var proj in analyzedProject.ExistingProjects)
        {
            if (!File.Exists(proj)) continue;
            var content = File.ReadAllText(proj);
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"<PackageReference\s+Include=""([^""]+)""\s+Version=""([^""]+)""");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                packages.TryAdd(match.Groups[1].Value, match.Groups[2].Value);
            }
        }

        if (packages.Count == 0 || !File.Exists(targetProjectCsproj)) return;

        var targetContent = File.ReadAllText(targetProjectCsproj);
        var targetPackages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var targetMatches = System.Text.RegularExpressions.Regex.Matches(targetContent, @"<PackageReference\s+Include=""([^""]+)""");
        foreach (System.Text.RegularExpressions.Match match in targetMatches)
        {
            targetPackages.Add(match.Groups[1].Value);
        }

        var missing = packages.Where(p => !targetPackages.Contains(p.Key)).ToList();
        if (missing.Any())
        {
            var itemGroup = "  <ItemGroup>\n" + string.Join("\n", missing.Select(p => $"    <PackageReference Include=\"{p.Key}\" Version=\"{p.Value}\" />")) + "\n  </ItemGroup>\n</Project>";
            targetContent = targetContent.Replace("</Project>", itemGroup);
            File.WriteAllText(targetProjectCsproj, targetContent);
        }
    }

    protected Task RunDotnetAsync(string workingDir, params string[] args)
    {
        var argsStr = string.Join(" ", args);
        return Cli.RunAsync(argsStr, workingDir);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Console helpers
    // ──────────────────────────────────────────────────────────────────────────

    protected static void WriteHeader(string text)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\n{'─',60}");
        Console.WriteLine($"  {text}");
        Console.WriteLine($"{'─',60}");
        Console.ResetColor();
    }

    protected static void WriteSuccess(string text)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    protected static void WriteInfo(string text)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    protected static void WriteWarning(string text)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    protected static void WriteError(string text)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(text);
        Console.ResetColor();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));

        foreach (var dir in Directory.GetDirectories(source))
        {
            var dirName = Path.GetFileName(dir);
            // Skip binary/git folders in backup too
            if (dirName is "bin" or "obj" or ".git" or ".vs") continue;
            CopyDirectory(dir, Path.Combine(destination, dirName));
        }
    }

    private static string Shorten(string path)
    {
        var cwd = Directory.GetCurrentDirectory();
        return path.StartsWith(cwd) ? path[cwd.Length..].TrimStart(Path.DirectorySeparatorChar) : path;
    }
}