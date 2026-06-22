using Archneter.Cli.Attributes;
using Archneter.Cli.Models;
using Archneter.Cli.Services;
using Archneter.Core.Enums;
using Archneter.Core.Models;

namespace Archneter.Cli.Commands;

/// <summary>
/// Represents the 'new' command, responsible for collecting user options and scaffolding the requested architecture.
/// </summary>
[Command("new")]
[Description("Scaffold a new architecture solution")]
[CommandSyntax("new <ProjectName> --arch <type> [--services <S1,S2,...>] [--modules <M1,M2,...>] [--features <F1,F2,...>] [--tests <true|false>] [--dry-run]")]
[CommandOption("--arch <type>",
    "Architecture template to generate",
    "  clean            → Clean Architecture (default)",
    "  microservices    → Microservices",
    "  n-tier           → N-Tier (PL → BLL → DAL)",
    "  verticalslice    → Vertical Slice",
    "  modularmonolith  → Modular Monolith")]
[CommandOption("--services <names>",
    "Comma-separated service names (microservices only)",
    "  e.g. Order,Product,Identity")]
[CommandOption("--modules <names>",
    "Comma-separated module names (modular monolith only)",
    "  e.g. Sales,Catalog,Inventory")]
[CommandOption("--features <names>",
    "Comma-separated feature names (vertical slice only)",
    "  e.g. Orders,Catalog,Cart")]
[CommandOption("--tests <true|false>",
    "Scaffold test projects (default: false)")]
[CommandOption("--dry-run",
    "Preview the generated structure without writing any files",
    "  no real dotnet commands run, nothing is created on disk")]
[CommandExample("archneter new CleanApp --arch clean --tests true")]
[CommandExample("archneter new LegacyApp --arch n-tier --tests true")]
[CommandExample("archneter new DistributedApp --arch microservices --services Order,Product,Identity --tests true")]
[CommandExample("archneter new MonolithApp --arch modularmonolith --modules Sales,Catalog --tests true")]
[CommandExample("archneter new SliceApp --arch verticalslice --features Orders,Cart --tests true")]
public sealed class NewCommand : IArchCommand
{
    private readonly GeneratorFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewCommand"/> class.
    /// </summary>
    /// <param name="factory">The factory used to resolve the specific architecture generator.</param>
    public NewCommand(GeneratorFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Asynchronously executes the scaffolding process based on user-provided CLI arguments.
    /// </summary>
    /// <param name="context">The context of the executed command.</param>
    public async Task ExecuteAsync(CommandContext context)
    {
        // ── Project name ──────────────────────────────────────────────────────────
        var projectName = context.ProjectName;

        if (string.IsNullOrWhiteSpace(projectName))
        {
            Console.WriteLine("Error: project name is required.");
            Console.WriteLine("Usage: archneter new <ProjectName> --arch <type>");
            return;
        }

        // ── Architecture type ─────────────────────────────────────────────────────
        var archFlag = context.Options.GetValueOrDefault("--arch", "clean").ToLowerInvariant();

        var archType = archFlag switch
        {
            "clean" => ArchitectureType.CleanArchitecture,
            "microservices" => ArchitectureType.Microservices,
            "verticalslice" => ArchitectureType.VerticalSlice,
            "modularmonolith" => ArchitectureType.ModularMonolith,
            "n-tier" => ArchitectureType.NTier,
            "ntier" => ArchitectureType.NTier,
            _ => ArchitectureType.CleanArchitecture
        };

        // ── Tests flag ────────────────────────────────────────────────────────────
        var testsFlag = context.Options.GetValueOrDefault("--tests", "false");
        var generateTests = testsFlag.Equals("true", StringComparison.OrdinalIgnoreCase);

        // ── Dry-run flag ──────────────────────────────────────────────────────────
        var isDryRun = context.Flags.Contains("--dry-run") || context.Options.ContainsKey("--dry-run");

        // ── Service names (microservices only) ────────────────────────────────────
        var serviceNames = new List<string>();

        if (archType == ArchitectureType.Microservices || archType == ArchitectureType.ModularMonolith || archType == ArchitectureType.VerticalSlice)
        {
            var noun = archType switch
            {
                ArchitectureType.VerticalSlice => "feature",
                ArchitectureType.ModularMonolith => "module",
                _ => "service"
            };
            var nounPlural = archType switch
            {
                ArchitectureType.VerticalSlice => "features",
                ArchitectureType.ModularMonolith => "modules",
                _ => "services"
            };

            var expectedFlag = archType switch
            {
                ArchitectureType.VerticalSlice => "--features",
                ArchitectureType.ModularMonolith => "--modules",
                _ => "--services"
            };

            var hasRaw = false;
            var rawString = string.Empty;

            if (context.Options.TryGetValue(expectedFlag, out var flagRaw))
            {
                hasRaw = true; rawString = flagRaw;
            }
            else if (context.Options.TryGetValue("--features", out var featRaw))
            {
                hasRaw = true; rawString = featRaw;
            }
            else if (context.Options.TryGetValue("--modules", out var modRaw))
            {
                hasRaw = true; rawString = modRaw;
            }
            else if (context.Options.TryGetValue("--services", out var svcRaw))
            {
                hasRaw = true; rawString = svcRaw;
            }

            if (hasRaw && !string.IsNullOrWhiteSpace(rawString))
            {
                // Option A — inline
                serviceNames = rawString
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
            }
            else
            {
                // Option B — interactive wizard
                Console.Write($"  How many {nounPlural}? (e.g. 3): ");
                if (!int.TryParse(Console.ReadLine()?.Trim(), out var count) || count <= 0)
                {
                    Console.WriteLine($"Error: invalid number of {nounPlural}.");
                    return;
                }

                for (int i = 1; i <= count; i++)
                {
                    Console.Write($"  {char.ToUpper(noun[0]) + noun.Substring(1)} {i} name: ");
                    var svcName = Console.ReadLine()?.Trim() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(svcName))
                        serviceNames.Add(svcName);
                }
            }

            if (serviceNames.Count == 0)
            {
                Console.WriteLine($"Error: at least one {noun} name is required.");
                return;
            }
        }

        // ── Build options & run generator ─────────────────────────────────────────
        var options = new ProjectOptions
        {
            ProjectName = projectName,
            Architecture = archType,
            GenerateTests = generateTests,
            ServiceNames = serviceNames
        };

        var generator = _factory.Create(archType, isDryRun);
        await generator.GenerateAsync(options);
    }
}