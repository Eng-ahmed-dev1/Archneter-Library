using Archneter.Core.Abstractions;
using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.CleanArchitecture
{
    /// <summary>
    /// Generates a complete Clean Architecture solution with Domain, Application, Infrastructure, and Api layers.
    /// </summary>
    public class CleanArchitectureGenerator : IArchitectureGenerator
    {
        private readonly ICliService _cli;

        /// <summary>
        /// Initializes a new instance of the <see cref="CleanArchitectureGenerator"/> class.
        /// </summary>
        /// <param name="cli">The CLI service used to execute commands.</param>
        public CleanArchitectureGenerator(ICliService cli)
        {
            _cli = cli;
        }

        /// <summary>
        /// Asynchronously scaffolds the Clean Architecture layers, directories, and cross-project references.
        /// </summary>
        /// <param name="options">The project configuration options.</param>
        public async Task GenerateAsync(ProjectOptions options)
        {
            var name = options.ProjectName;
            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), name);
            var srcPath = Path.Combine(rootPath, "src");
            var testsPath = Path.Combine(rootPath, "tests");

            var isDryRun = _cli is DryRunCliService dryRun;
            if (_cli is DryRunCliService dryRunService)
                dryRunService.PrintHeader(name, "Clean Architecture");

            if (!isDryRun)
            {
                Directory.CreateDirectory(rootPath);
                Directory.CreateDirectory(srcPath);
            }

            await _cli.RunAsync($"new sln -n {name}", rootPath);

            var slnPath = isDryRun
                ? Path.Combine(rootPath, $"{name}.sln")
                : Directory.GetFiles(rootPath, $"{name}.sln*").FirstOrDefault()
                    ?? throw new Exception($"Solution file not found after creation in {rootPath}");

            var domainPath = Path.Combine(srcPath, $"{name}.Domain");
            var applicationPath = Path.Combine(srcPath, $"{name}.Application");
            var infrastructurePath = Path.Combine(srcPath, $"{name}.Infrastructure");
            var apiPath = Path.Combine(srcPath, $"{name}.Api");

            await _cli.CreateProjectAsync("classlib", $"{name}.Domain", domainPath);
            await _cli.CreateProjectAsync("classlib", $"{name}.Application", applicationPath);
            await _cli.CreateProjectAsync("classlib", $"{name}.Infrastructure", infrastructurePath);
            await _cli.CreateProjectAsync("webapi", $"{name}.Api", apiPath);

            await _cli.AddToSolutionAsync(slnPath, $"{domainPath}/{name}.Domain.csproj");
            await _cli.AddToSolutionAsync(slnPath, $"{applicationPath}/{name}.Application.csproj");
            await _cli.AddToSolutionAsync(slnPath, $"{infrastructurePath}/{name}.Infrastructure.csproj");
            await _cli.AddToSolutionAsync(slnPath, $"{apiPath}/{name}.Api.csproj");

            await _cli.AddReferenceAsync(
                $"{applicationPath}/{name}.Application.csproj",
                $"{domainPath}/{name}.Domain.csproj");

            await _cli.AddReferenceAsync(
                $"{infrastructurePath}/{name}.Infrastructure.csproj",
                $"{applicationPath}/{name}.Application.csproj");

            await _cli.AddReferenceAsync(
                $"{apiPath}/{name}.Api.csproj",
                $"{applicationPath}/{name}.Application.csproj");

            await _cli.AddReferenceAsync(
                $"{apiPath}/{name}.Api.csproj",
                $"{infrastructurePath}/{name}.Infrastructure.csproj");

            if (!isDryRun)
            {
                CreateDirectories(
                    // Domain
                    Path.Combine(domainPath, "Common"),
                    Path.Combine(domainPath, "Entities"),
                    Path.Combine(domainPath, "Enums"),
                    Path.Combine(domainPath, "Events"),
                    Path.Combine(domainPath, "Exceptions"),
                    Path.Combine(domainPath, "ValueObjects"),
                    Path.Combine(domainPath, "Constants"),

                    // Application
                    Path.Combine(applicationPath, "Common", "Behaviors"),
                    Path.Combine(applicationPath, "Common", "Exceptions"),
                    Path.Combine(applicationPath, "Common", "Interfaces"),
                    Path.Combine(applicationPath, "Common", "Mappings"),
                    Path.Combine(applicationPath, "Common", "Models"),
                    Path.Combine(applicationPath, "DependencyInjection"),
                    Path.Combine(applicationPath, "Features"),

                    // Infrastructure
                    Path.Combine(infrastructurePath, "Persistence", "Configurations"),
                    Path.Combine(infrastructurePath, "Persistence", "Repositories"),
                    Path.Combine(infrastructurePath, "Persistence", "Migrations"),
                    Path.Combine(infrastructurePath, "Persistence", "Interceptors"),
                    Path.Combine(infrastructurePath, "Services"),
                    Path.Combine(infrastructurePath, "Identity"),
                    Path.Combine(infrastructurePath, "ExternalServices"),
                    Path.Combine(infrastructurePath, "DependencyInjection"),

                    // Api
                    Path.Combine(apiPath, "Controllers"),
                    Path.Combine(apiPath, "Middlewares"),
                    Path.Combine(apiPath, "Extensions"),
                    Path.Combine(apiPath, "Filters"),
                    Path.Combine(apiPath, "Configurations"),
                    Path.Combine(apiPath, "Properties")
                );
            }

            if (options.GenerateTests)
            {
                if (!isDryRun)
                    Directory.CreateDirectory(testsPath);

                var unitTestsPath = Path.Combine(testsPath, $"{name}.Unit.Tests");
                await _cli.CreateProjectAsync("xunit", $"{name}.Unit.Tests", unitTestsPath);
                await _cli.AddToSolutionAsync(slnPath, $"{unitTestsPath}/{name}.Unit.Tests.csproj");
                await _cli.AddReferenceAsync(
                    $"{unitTestsPath}/{name}.Unit.Tests.csproj",
                    $"{domainPath}/{name}.Domain.csproj");

                if (!isDryRun)
                    CreateDirectories(
                        Path.Combine(unitTestsPath, "Domain"),
                        Path.Combine(unitTestsPath, "Application")
                    );

                var integrationTestsPath = Path.Combine(testsPath, $"{name}.Integration.Tests");
                await _cli.CreateProjectAsync("xunit", $"{name}.Integration.Tests", integrationTestsPath);
                await _cli.AddToSolutionAsync(slnPath, $"{integrationTestsPath}/{name}.Integration.Tests.csproj");
                await _cli.AddReferenceAsync(
                    $"{integrationTestsPath}/{name}.Integration.Tests.csproj",
                    $"{apiPath}/{name}.Api.csproj");

                if (!isDryRun)
                    CreateDirectories(
                        Path.Combine(integrationTestsPath, "Infrastructure"),
                        Path.Combine(integrationTestsPath, "Api")
                    );
            }

            if (_cli is DryRunCliService footer)
                footer.PrintFooter();
            else
                Console.WriteLine($"Clean Architecture solution '{name}' generated successfully.");
        }

        /// <summary>
        /// Helper method to safely create multiple directories.
        /// </summary>
        private static void CreateDirectories(params string[] directories)
        {
            foreach (var directory in directories)
                Directory.CreateDirectory(directory);
        }
    }
}