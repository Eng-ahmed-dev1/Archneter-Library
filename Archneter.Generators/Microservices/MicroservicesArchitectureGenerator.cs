using Archneter.Core.Abstractions;
using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.Microservices
{
    /// <summary>
    /// Generates a Microservices architecture solution with an API Gateway, Shared Contracts, and independent services.
    /// </summary>
    public class MicroservicesGenerator : IArchitectureGenerator
    {
        private readonly ICliService _cli;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicroservicesGenerator"/> class.
        /// </summary>
        /// <param name="cli">The CLI service used to execute commands.</param>
        public MicroservicesGenerator(ICliService cli)
        {
            _cli = cli;
        }

        /// <summary>
        /// Asynchronously scaffolds the entire Microservices ecosystem, creating projects and configuring references.
        /// </summary>
        /// <param name="options">The project configuration options containing the requested microservices.</param>
        public async Task GenerateAsync(ProjectOptions options)
        {
            var name = options.ProjectName;
            var serviceNames = options.ServiceNames;

            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), name);
            var srcPath = Path.Combine(rootPath, "src");
            var testsPath = Path.Combine(rootPath, "tests");

            var isDryRun = _cli is DryRunCliService;

            if (_cli is DryRunCliService dryRunService)
                dryRunService.PrintHeader(name, "Microservices");

            if (!isDryRun)
            {
                Directory.CreateDirectory(rootPath);
                Directory.CreateDirectory(srcPath);
            }

            // ── Solution ─────────────────────────────────────────────
            await _cli.RunAsync($"new sln -n {name}", rootPath);

            var slnPath = isDryRun
                ? Path.Combine(rootPath, $"{name}.sln")
                : Directory.GetFiles(rootPath, $"{name}.sln*").FirstOrDefault()
                  ?? throw new Exception($"Solution file not found in {rootPath}");

            // ── Shared Contracts ─────────────────────────────────────
            var contractsPath = Path.Combine(srcPath, $"{name}.Contracts");

            await _cli.CreateProjectAsync("classlib", $"{name}.Contracts", contractsPath);
            await _cli.AddToSolutionAsync(slnPath, $"{contractsPath}/{name}.Contracts.csproj");

            if (!isDryRun)
            {
                CreateDirectories(
                    Path.Combine(contractsPath, "Events"),
                    Path.Combine(contractsPath, "DTOs"),
                    Path.Combine(contractsPath, "Enums")
                );
            }

            // ── API Gateway ──────────────────────────────────────────
            var gatewayPath = Path.Combine(srcPath, $"{name}.ApiGateway");

            await _cli.CreateProjectAsync("webapi", $"{name}.ApiGateway", gatewayPath);

            await _cli.AddToSolutionAsync(
                slnPath,
                $"{gatewayPath}/{name}.ApiGateway.csproj");

            // Gateway depends on contracts
            await _cli.AddReferenceAsync(
                $"{gatewayPath}/{name}.ApiGateway.csproj",
                $"{contractsPath}/{name}.Contracts.csproj");

            if (!isDryRun)
            {
                CreateDirectories(
                    Path.Combine(gatewayPath, "Configuration"),
                    Path.Combine(gatewayPath, "Middlewares"),
                    Path.Combine(gatewayPath, "Extensions"),
                    Path.Combine(gatewayPath, "Routes")
                );

                // Optional: basic YARP config file
                File.WriteAllText(
                    Path.Combine(gatewayPath, "appsettings.json"),
                    """
                    {
                      "ReverseProxy": {
                        "Routes": {},
                        "Clusters": {}
                      }
                    }
                    """);
            }

            // ── Per-service scaffold ─────────────────────────────────
            foreach (var service in serviceNames)
            {
                await ScaffoldServiceAsync(
                    name,
                    service,
                    srcPath,
                    testsPath,
                    slnPath,
                    contractsPath,
                    isDryRun,
                    options.GenerateTests
                );
            }

            if (_cli is DryRunCliService footer)
                footer.PrintFooter();
            else
                Console.WriteLine(
                    $"Microservices solution '{name}' generated successfully " +
                    $"({string.Join(", ", serviceNames)}).");
        }

        // ── Service Scaffold ───────────────────────────────────────
        private async Task ScaffoldServiceAsync(
            string solutionName,
            string service,
            string srcPath,
            string testsPath,
            string slnPath,
            string contractsPath,
            bool isDryRun,
            bool generateTests)
        {
            var prefix = $"{solutionName}.{service}";
            var servicePath = Path.Combine(srcPath, $"{prefix}.Service");

            var domainPath = Path.Combine(servicePath, "Domain");
            var applicationPath = Path.Combine(servicePath, "Application");
            var infrastructurePath = Path.Combine(servicePath, "Infrastructure");
            var apiPath = Path.Combine(servicePath, "Api");

            if (!isDryRun)
                Directory.CreateDirectory(servicePath);

            // ── Projects ─────────────────────────────────────────────
            await _cli.CreateProjectAsync("classlib", $"{prefix}.Domain", domainPath);
            await _cli.CreateProjectAsync("classlib", $"{prefix}.Application", applicationPath);
            await _cli.CreateProjectAsync("classlib", $"{prefix}.Infrastructure", infrastructurePath);
            await _cli.CreateProjectAsync("webapi", $"{prefix}.Api", apiPath);

            // ── Solution references ──────────────────────────────────
            await _cli.AddToSolutionAsync(slnPath, $"{domainPath}/{prefix}.Domain.csproj");
            await _cli.AddToSolutionAsync(slnPath, $"{applicationPath}/{prefix}.Application.csproj");
            await _cli.AddToSolutionAsync(slnPath, $"{infrastructurePath}/{prefix}.Infrastructure.csproj");
            await _cli.AddToSolutionAsync(slnPath, $"{apiPath}/{prefix}.Api.csproj");

            // ── Dependencies ─────────────────────────────────────────
            await _cli.AddReferenceAsync(
                $"{applicationPath}/{prefix}.Application.csproj",
                $"{domainPath}/{prefix}.Domain.csproj");

            await _cli.AddReferenceAsync(
                $"{infrastructurePath}/{prefix}.Infrastructure.csproj",
                $"{applicationPath}/{prefix}.Application.csproj");

            await _cli.AddReferenceAsync(
                $"{apiPath}/{prefix}.Api.csproj",
                $"{applicationPath}/{prefix}.Application.csproj");

            await _cli.AddReferenceAsync(
                $"{apiPath}/{prefix}.Api.csproj",
                $"{infrastructurePath}/{prefix}.Infrastructure.csproj");

            // Contracts dependency
            await _cli.AddReferenceAsync(
                $"{applicationPath}/{prefix}.Application.csproj",
                $"{contractsPath}/{solutionName}.Contracts.csproj");

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

                    Path.Combine(apiPath, "Controllers"),
                    Path.Combine(apiPath, "Middlewares"),
                    Path.Combine(apiPath, "Extensions"),
                    Path.Combine(apiPath, "Configurations")
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
                    $"{apiPath}/{prefix}.Api.csproj");
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