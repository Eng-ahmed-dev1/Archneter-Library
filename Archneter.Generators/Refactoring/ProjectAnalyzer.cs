using Archneter.Core.Models;
using System.Text.RegularExpressions;

namespace Archneter.Generators.Refactoring;

/// <summary>
/// Analyzes an existing .NET project directory and classifies each source file
/// into an architectural layer based on naming conventions and file content.
/// </summary>
public class ProjectAnalyzer
{
    // ──────────────────────────────────────────────────────────────────────────
    // Classification rules: (pattern, layer, confidence, reason)
    // Evaluated top-to-bottom; first match wins.
    // ──────────────────────────────────────────────────────────────────────────
    private static readonly List<(Regex Pattern, ProjectLayer Layer, ClassificationConfidence Confidence, string Reason)> Rules = new()
    {
        // ── Presentation ──────────────────────────────────────────────────────
        (new Regex(@"Controller(s)?\.cs$",         RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Presentation, ClassificationConfidence.High,   "Controllers belong to the Presentation layer"),
        (new Regex(@"Middleware\.cs$",             RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Presentation, ClassificationConfidence.High,   "Middleware belongs to the Presentation layer"),
        (new Regex(@"Filter\.cs$",                 RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Presentation, ClassificationConfidence.High,   "Filters belong to the Presentation layer"),
        (new Regex(@"Endpoint(s)?\.cs$",           RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Presentation, ClassificationConfidence.High,   "Endpoints belong to the Presentation layer"),
        (new Regex(@"Program\.cs$",                RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Presentation, ClassificationConfidence.High,   "Program.cs is the API entry point"),
        (new Regex(@"Startup\.cs$",                RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Presentation, ClassificationConfidence.High,   "Startup.cs is the API entry point"),

        // ── Infrastructure ────────────────────────────────────────────────────
        (new Regex(@"Repository\.cs$",             RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Infrastructure, ClassificationConfidence.High, "Repositories belong to the Infrastructure layer"),
        (new Regex(@"DbContext\.cs$",              RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Infrastructure, ClassificationConfidence.High, "DbContext belongs to the Infrastructure layer"),
        (new Regex(@"Migration",                   RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Infrastructure, ClassificationConfidence.High, "EF Core migrations belong to Infrastructure"),
        (new Regex(@"Configuration\.cs$",          RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Infrastructure, ClassificationConfidence.Medium, "Entity configurations belong to Infrastructure"),
        (new Regex(@"EmailSender\.cs$",            RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Infrastructure, ClassificationConfidence.High, "Email senders are external service integrations"),
        (new Regex(@"(Smtp|Email|Sms)Service\.cs$",RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Infrastructure, ClassificationConfidence.High, "External communication services belong to Infrastructure"),
        (new Regex(@"(Cache|Redis|Rabbit|Kafka|Azure|Aws).*\.cs$", RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Infrastructure, ClassificationConfidence.High, "External platform integrations belong to Infrastructure"),

        // ── Application ───────────────────────────────────────────────────────
        (new Regex(@"Service\.cs$",                RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Application, ClassificationConfidence.Medium,  "Services typically belong to the Application layer"),
        (new Regex(@"(Handler|Command|Query)\.cs$",RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Application, ClassificationConfidence.High,   "CQRS handlers/commands/queries belong to Application"),
        (new Regex(@"(Validator|Validation)\.cs$", RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Application, ClassificationConfidence.High,   "Validators belong to the Application layer"),
        (new Regex(@"Mapping(Profile)?\.cs$",      RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Application, ClassificationConfidence.High,   "AutoMapper profiles belong to Application"),
        (new Regex(@"Dto\.cs$",                    RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Application, ClassificationConfidence.High,   "DTOs belong to the Application layer"),
        (new Regex(@"(Request|Response)\.cs$",     RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Application, ClassificationConfidence.Medium, "Request/Response models belong to Application"),

        // ── Domain ────────────────────────────────────────────────────────────
        (new Regex(@"(Entity|Aggregate|ValueObject)\.cs$", RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Domain, ClassificationConfidence.High, "Entities and aggregates belong to the Domain layer"),
        (new Regex(@"(IRepository|IService|IUnitOfWork)\.cs$", RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Domain, ClassificationConfidence.High, "Domain interfaces belong to the Domain layer"),
        (new Regex(@"^I[A-Z].*\.cs$",              RegexOptions.Compiled), ProjectLayer.Domain, ClassificationConfidence.Low,          "Interface naming convention suggests Domain"),
        (new Regex(@"(Event|DomainEvent)\.cs$",    RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Domain, ClassificationConfidence.High,         "Domain events belong to the Domain layer"),
        (new Regex(@"Enum(s)?\.cs$",               RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Domain, ClassificationConfidence.Medium,       "Enums typically belong to the Domain layer"),
        (new Regex(@"Exception\.cs$",              RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Domain, ClassificationConfidence.Medium,       "Domain exceptions belong to the Domain layer"),

        // ── Tests ─────────────────────────────────────────────────────────────
        (new Regex(@"(Test|Tests|Spec)\.cs$",      RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Tests, ClassificationConfidence.High, "Test files belong to the Tests layer"),
        (new Regex(@"Mock.*\.cs$",                 RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Tests, ClassificationConfidence.High, "Mock files belong to the Tests layer"),
        (new Regex(@"Fake.*\.cs$",                 RegexOptions.IgnoreCase | RegexOptions.Compiled), ProjectLayer.Tests, ClassificationConfidence.High, "Fake objects belong to the Tests layer"),
    };

    // Folders to skip entirely
    private static readonly HashSet<string> SkippedFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", ".idea", "node_modules", "_backup"
    };

    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Scans the given directory and returns a classified snapshot of the project.
    /// </summary>
    public virtual AnalyzedProject Analyze(string directory)
    {
        var result = new AnalyzedProject
        {
            RootDirectory = directory,
            ProjectName   = InferProjectName(directory),
        };

        result.HasSolution      = Directory.GetFiles(directory, "*.sln",  SearchOption.TopDirectoryOnly).Any()
                                || Directory.GetFiles(directory, "*.slnx", SearchOption.TopDirectoryOnly).Any();

        result.ExistingProjects = Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories)
                                           .Where(p => !IsInSkippedFolder(p, directory))
                                           .ToList();

        var csFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
                               .Where(f => !IsInSkippedFolder(f, directory))
                               .ToList();

        foreach (var file in csFiles)
        {
            var classified = ClassifyFile(file);
            if (classified.Confidence == ClassificationConfidence.Low && !Rules.Any(r => r.Pattern.IsMatch(Path.GetFileName(file))))
                result.UnclassifiedFiles.Add(file);
            else
                result.Files.Add(classified);
        }

        return result;
    }

    // ──────────────────────────────────────────────────────────────────────────

    private static ClassifiedFile ClassifyFile(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var originalNs = string.Empty;
        try
        {
            var content = File.ReadAllText(filePath);
            var match = new Regex(@"namespace\s+([\w.]+)", RegexOptions.Compiled).Match(content);
            if (match.Success)
                originalNs = match.Groups[1].Value;
        }
        catch { }

        foreach (var (pattern, layer, confidence, reason) in Rules)
        {
            if (pattern.IsMatch(fileName))
            {
                return new ClassifiedFile
                {
                    SourcePath             = filePath,
                    TargetLayer            = layer,
                    Confidence             = confidence,
                    ClassificationReason   = reason,
                    OriginalNamespace      = originalNs
                };
            }
        }

        var dirPath = Path.GetDirectoryName(filePath);
        if (dirPath != null)
        {
            if (dirPath.Contains("Models", StringComparison.OrdinalIgnoreCase) || 
                dirPath.Contains("Entities", StringComparison.OrdinalIgnoreCase) ||
                dirPath.Contains("Domain", StringComparison.OrdinalIgnoreCase))
            {
                return new ClassifiedFile { SourcePath = filePath, TargetLayer = ProjectLayer.Domain, Confidence = ClassificationConfidence.Medium, ClassificationReason = "Inferred from directory name (Models/Entities/Domain)", OriginalNamespace = originalNs };
            }
            if (dirPath.Contains("Services", StringComparison.OrdinalIgnoreCase) ||
                dirPath.Contains("Application", StringComparison.OrdinalIgnoreCase))
            {
                return new ClassifiedFile { SourcePath = filePath, TargetLayer = ProjectLayer.Application, Confidence = ClassificationConfidence.Medium, ClassificationReason = "Inferred from directory name (Services/Application)", OriginalNamespace = originalNs };
            }
            if (dirPath.Contains("Data", StringComparison.OrdinalIgnoreCase) ||
                dirPath.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase))
            {
                return new ClassifiedFile { SourcePath = filePath, TargetLayer = ProjectLayer.Infrastructure, Confidence = ClassificationConfidence.Medium, ClassificationReason = "Inferred from directory name (Data/Infrastructure)", OriginalNamespace = originalNs };
            }
        }

        // Content-based fallback for files whose name alone doesn't match
        var fallback = ClassifyByContent(filePath);
        fallback.OriginalNamespace = originalNs;
        return fallback;
    }

    private static ClassifiedFile ClassifyByContent(string filePath)
    {
        try
        {
            foreach (var content in File.ReadLines(filePath))
            {
                if (content.Contains("DbContext")         || content.Contains("IRepository"))
                    return Classified(filePath, ProjectLayer.Infrastructure, ClassificationConfidence.Medium, "Contains DbContext or IRepository usage");

                if (content.Contains(": ControllerBase")  || content.Contains(": Controller")  || content.Contains("[ApiController]"))
                    return Classified(filePath, ProjectLayer.Presentation, ClassificationConfidence.High, "Inherits from ControllerBase / has [ApiController]");

                if (content.Contains(": IRequestHandler") || content.Contains(": ICommandHandler") || content.Contains("MediatR"))
                    return Classified(filePath, ProjectLayer.Application, ClassificationConfidence.High, "Uses MediatR handler pattern");

                if (content.Contains(": AbstractValidator"))
                    return Classified(filePath, ProjectLayer.Application, ClassificationConfidence.High, "Inherits from AbstractValidator (FluentValidation)");

                if (content.Contains(": Entity<")         || content.Contains(": AggregateRoot"))
                    return Classified(filePath, ProjectLayer.Domain, ClassificationConfidence.High, "Inherits from Entity or AggregateRoot");
            }
        }
        catch
        {
            // Cannot read file — leave as unclassified
        }

        return Classified(filePath, ProjectLayer.Shared, ClassificationConfidence.Low, "Could not determine layer from name or content");
    }

    // ──────────────────────────────────────────────────────────────────────────

    private static ClassifiedFile Classified(string path, ProjectLayer layer, ClassificationConfidence confidence, string reason)
    {
        var originalNs = string.Empty;
        try
        {
            var content = File.ReadAllText(path);
            var match = new Regex(@"namespace\s+([\w.]+)", RegexOptions.Compiled).Match(content);
            if (match.Success)
                originalNs = match.Groups[1].Value;
        }
        catch { }

        return new() { SourcePath = path, TargetLayer = layer, Confidence = confidence, ClassificationReason = reason, OriginalNamespace = originalNs };
    }

    private static string InferProjectName(string directory)
    {
        // 1. Try to read from solution file first (to match the real solution name)
        var slnx = Directory.GetFiles(directory, "*.slnx", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (slnx is not null) return Path.GetFileNameWithoutExtension(slnx);
        
        var sln = Directory.GetFiles(directory, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (sln is not null) return Path.GetFileNameWithoutExtension(sln);

        // 2. Try to read the .csproj name
        var csproj = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (csproj is not null)
            return Path.GetFileNameWithoutExtension(csproj);

        // 3. Fallback to the directory name
        return new DirectoryInfo(directory).Name;
    }

    private static bool IsInSkippedFolder(string filePath, string root)
    {
        var relative = Path.GetRelativePath(root, filePath);
        var parts    = relative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(p => SkippedFolders.Contains(p));
    }
}