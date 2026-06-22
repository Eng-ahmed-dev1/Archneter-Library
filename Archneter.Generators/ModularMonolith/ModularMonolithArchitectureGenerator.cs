using Archneter.Core.Abstractions;
using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.ModularMonolith
{
    /// <summary>
    /// Generates a Modular Monolith architecture solution with a single Host API, a Shared layer, and independent feature modules.
    /// </summary>
    public class ModularMonolithArchitectureGenerator : IArchitectureGenerator
    {
        private readonly ICliService _cli;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModularMonolithArchitectureGenerator"/> class.
        /// </summary>
        /// <param name="cli">The CLI service used to execute commands.</param>
        public ModularMonolithArchitectureGenerator(ICliService cli)
        {
            _cli = cli;
        }

        /// <summary>
        /// Asynchronously scaffolds the entire Modular Monolith ecosystem, creating projects and configuring references.
        /// </summary>
        /// <param name="options">The project configuration options containing the requested modules.</param>
        public async Task GenerateAsync(ProjectOptions options)
        {
            var name = options.ProjectName;
            var moduleNames = options.ServiceNames;

            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), name);
            var srcPath = Path.Combine(rootPath, "src");
            var testsPath = Path.Combine(rootPath, "tests");
            var modulesPath = Path.Combine(srcPath, "Modules");

            var isDryRun = _cli is DryRunCliService;

            if (_cli is DryRunCliService dryRunService)
                dryRunService.PrintHeader(name, "Modular Monolith");

            if (!isDryRun)
            {
                Directory.CreateDirectory(rootPath);
                Directory.CreateDirectory(srcPath);
                Directory.CreateDirectory(modulesPath);
            }

            // ── Solution ─────────────────────────────────────────────
            await _cli.RunAsync($"new sln -n {name}", rootPath);

            var slnPath = isDryRun
                ? Path.Combine(rootPath, $"{name}.sln")
                : Directory.GetFiles(rootPath, $"{name}.sln*").FirstOrDefault()
                  ?? throw new Exception($"Solution file not found in {rootPath}");

            // ── Shared Contracts / Core ──────────────────────────────
            var sharedPath = Path.Combine(srcPath, $"{name}.Shared");

            await _cli.CreateProjectAsync("classlib", $"{name}.Shared", sharedPath);
            await _cli.AddToSolutionAsync(slnPath, $"{sharedPath}/{name}.Shared.csproj");

            if (!isDryRun)
            {
                CreateDirectories(
                    Path.Combine(sharedPath, "Events"),
                    Path.Combine(sharedPath, "DTOs"),
                    Path.Combine(sharedPath, "Enums"),
                    Path.Combine(sharedPath, "Interfaces")
                );
            }

            // ── Host (API) ───────────────────────────────────────────
            var hostPath = Path.Combine(srcPath, $"{name}.Host");

            await _cli.CreateProjectAsync("webapi", $"{name}.Host", hostPath);
            await _cli.AddToSolutionAsync(slnPath, $"{hostPath}/{name}.Host.csproj");

            // Host depends on shared
            await _cli.AddReferenceAsync(
                $"{hostPath}/{name}.Host.csproj",
                $"{sharedPath}/{name}.Shared.csproj");

            if (!isDryRun)
            {
                CreateDirectories(
                    Path.Combine(hostPath, "Configuration"),
                    Path.Combine(hostPath, "Middlewares"),
                    Path.Combine(hostPath, "Extensions")
                );
            }

            // ── Per-module scaffold ─────────────────────────────────
            foreach (var module in moduleNames)
            {
                await ScaffoldModuleAsync(
                    name,
                    module,
                    modulesPath,
                    testsPath,
                    slnPath,
                    sharedPath,
                    hostPath,
                    isDryRun,
                    options.GenerateTests
                );
            }

            if (_cli is DryRunCliService footer)
                footer.PrintFooter();
            else
                Console.WriteLine(
                    $"Modular Monolith solution '{name}' generated successfully " +
                    $"({string.Join(", ", moduleNames)}).");
        }

        // ── Module Scaffold ────────────────────────────────────────
        private async Task ScaffoldModuleAsync(
            string solutionName,
            string module,
            string modulesPath,
            string testsPath,
            string slnPath,
            string sharedPath,
            string hostPath,
            bool isDryRun,
            bool generateTests)
        {
            var prefix = $"{solutionName}.Modules.{module}";
            var modulePath = Path.Combine(modulesPath, module);

            var domainPath = Path.Combine(modulePath, "Domain");
            var applicationPath = Path.Combine(modulePath, "Application");
            var infrastructurePath = Path.Combine(modulePath, "Infrastructure");
            var presentationPath = Path.Combine(modulePath, "Presentation");

            if (!isDryRun)
                Directory.CreateDirectory(modulePath);

            // ── Projects ─────────────────────────────────────────────
            await _cli.CreateProjectAsync("classlib", $"{prefix}.Domain", domainPath);
            await _cli.CreateProjectAsync("classlib", $"{prefix}.Application", applicationPath);
            await _cli.CreateProjectAsync("classlib", $"{prefix}.Infrastructure", infrastructurePath);
            await _cli.CreateProjectAsync("classlib", $"{prefix}.Presentation", presentationPath);

            // ── Solution references ──────────────────────────────────
            await _cli.AddToSolutionAsync(slnPath, $"{domainPath}/{prefix}.Domain.csproj");
            await _cli.AddToSolutionAsync(slnPath, $"{applicationPath}/{prefix}.Application.csproj");
            await _cli.AddToSolutionAsync(slnPath, $"{infrastructurePath}/{prefix}.Infrastructure.csproj");
            await _cli.AddToSolutionAsync(slnPath, $"{presentationPath}/{prefix}.Presentation.csproj");

            // ── Dependencies ─────────────────────────────────────────
            await _cli.AddReferenceAsync(
                $"{applicationPath}/{prefix}.Application.csproj",
                $"{domainPath}/{prefix}.Domain.csproj");

            await _cli.AddReferenceAsync(
                $"{infrastructurePath}/{prefix}.Infrastructure.csproj",
                $"{applicationPath}/{prefix}.Application.csproj");

            await _cli.AddReferenceAsync(
                $"{presentationPath}/{prefix}.Presentation.csproj",
                $"{applicationPath}/{prefix}.Application.csproj");

            // Presentation often references Infrastructure for DI setup inside the module, or Host references Infrastructure directly.
            // In a clean Modular Monolith, we can have the Host reference Presentation and Infrastructure to wire everything up.
            await _cli.AddReferenceAsync(
                $"{hostPath}/{solutionName}.Host.csproj",
                $"{presentationPath}/{prefix}.Presentation.csproj");

            await _cli.AddReferenceAsync(
                $"{hostPath}/{solutionName}.Host.csproj",
                $"{infrastructurePath}/{prefix}.Infrastructure.csproj");

            // Shared dependency
            await _cli.AddReferenceAsync(
                $"{applicationPath}/{prefix}.Application.csproj",
                $"{sharedPath}/{solutionName}.Shared.csproj");

            // ── Folder structure ─────────────────────────────────────
            if (!isDryRun)
            {
                CreateDirectories(
                    Path.Combine(domainPath, "Entities"),
                    Path.Combine(domainPath, "ValueObjects"),
                    Path.Combine(domainPath, "Events"),
                    Path.Combine(domainPath, "Exceptions"),
                    Path.Combine(domainPath, "Enums"),

                    Path.Combine(applicationPath, "Common", "Interfaces"),
                    Path.Combine(applicationPath, "Common", "Behaviors"),
                    Path.Combine(applicationPath, "Common", "Models"),
                    Path.Combine(applicationPath, "Features"),
                    Path.Combine(applicationPath, "DependencyInjection"),

                    Path.Combine(infrastructurePath, "Persistence", "Configurations"),
                    Path.Combine(infrastructurePath, "Persistence", "Repositories"),
                    Path.Combine(infrastructurePath, "Messaging"),
                    Path.Combine(infrastructurePath, "Services"),
                    Path.Combine(infrastructurePath, "DependencyInjection"),

                    Path.Combine(presentationPath, "Controllers"),
                    Path.Combine(presentationPath, "Endpoints"),
                    Path.Combine(presentationPath, "DependencyInjection")
                );
            }

            // ── Tests ────────────────────────────────────────────────
            if (generateTests)
            {
                if (!isDryRun)
                    Directory.CreateDirectory(testsPath);

                var unitTestsPath = Path.Combine(testsPath, $"{prefix}.Unit.Tests");
                var integrationTestsPath = Path.Combine(testsPath, $"{prefix}.Integration.Tests");

                await _cli.CreateProjectAsync("xunit", $"{prefix}.Unit.Tests", unitTestsPath);
                await _cli.CreateProjectAsync("xunit", $"{prefix}.Integration.Tests", integrationTestsPath);

                await _cli.AddToSolutionAsync(slnPath, $"{unitTestsPath}/{prefix}.Unit.Tests.csproj");
                await _cli.AddToSolutionAsync(slnPath, $"{integrationTestsPath}/{prefix}.Integration.Tests.csproj");

                await _cli.AddReferenceAsync(
                    $"{unitTestsPath}/{prefix}.Unit.Tests.csproj",
                    $"{domainPath}/{prefix}.Domain.csproj");

                await _cli.AddReferenceAsync(
                    $"{integrationTestsPath}/{prefix}.Integration.Tests.csproj",
                    $"{presentationPath}/{prefix}.Presentation.csproj");
            }
        }

        // ── Helpers ───────────────────────────────────────────────
        private static void CreateDirectories(params string[] dirs)
        {
            foreach (var dir in dirs)
                Directory.CreateDirectory(dir);
        }
    }
}