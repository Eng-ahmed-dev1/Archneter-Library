using Archneter.Core.Abstractions;
using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.NTier
{
    /// <summary>
    /// Generates a traditional N-Tier architecture solution comprising Presentation (PL), Business Logic (BLL), and Data Access (DAL) layers.
    /// </summary>
    public class NTierArchitectureGenerator : IArchitectureGenerator
    {
        private readonly ICliService _cli;

        /// <summary>
        /// Initializes a new instance of the <see cref="NTierArchitectureGenerator"/> class.
        /// </summary>
        /// <param name="cli">The CLI service used to execute commands.</param>
        public NTierArchitectureGenerator(ICliService cli)
        {
            _cli = cli;
        }

        /// <summary>
        /// Asynchronously scaffolds the N-Tier architecture layers and constructs internal directories.
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
                dryRunService.PrintHeader(name, "N-Tier Architecture");

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

            var dalPath = Path.Combine(srcPath, $"{name}.DAL");
            var bllPath = Path.Combine(srcPath, $"{name}.BLL");
            var plPath = Path.Combine(srcPath, $"{name}.PL");

            await _cli.CreateProjectAsync("classlib", $"{name}.DAL", dalPath);
            await _cli.CreateProjectAsync("classlib", $"{name}.BLL", bllPath);
            await _cli.CreateProjectAsync("webapi", $"{name}.PL", plPath);

            await _cli.AddToSolutionAsync(slnPath, $"{dalPath}/{name}.DAL.csproj");
            await _cli.AddToSolutionAsync(slnPath, $"{bllPath}/{name}.BLL.csproj");
            await _cli.AddToSolutionAsync(slnPath, $"{plPath}/{name}.PL.csproj");

            // Dependency flow: PL -> BLL -> DAL
            await _cli.AddReferenceAsync(
                $"{bllPath}/{name}.BLL.csproj",
                $"{dalPath}/{name}.DAL.csproj");

            await _cli.AddReferenceAsync(
                $"{plPath}/{name}.PL.csproj",
                $"{bllPath}/{name}.BLL.csproj");

            if (!isDryRun)
            {
                CreateDirectories(
                    // DAL
                    Path.Combine(dalPath, "Data"),
                    Path.Combine(dalPath, "Models"),
                    Path.Combine(dalPath, "Repositories"),
                    Path.Combine(dalPath, "Enums"),


                    // BLL
                    Path.Combine(bllPath, "DTOs"),
                    Path.Combine(bllPath, "Services"),
                    Path.Combine(bllPath, "Mapping"),
                    Path.Combine(bllPath, "Interfaces"),
                    Path.Combine(bllPath, "Exceptions"),


                    // PL
                    Path.Combine(plPath, "Controllers"),
                    Path.Combine(plPath, "Middlewares"),
                    Path.Combine(plPath, "Extensions"),
                    Path.Combine(plPath, "Filters"),
                    Path.Combine(plPath, "Properties")
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
                    $"{bllPath}/{name}.BLL.csproj");

                if (!isDryRun)
                    CreateDirectories(
                        Path.Combine(unitTestsPath, "BLL"),
                        Path.Combine(unitTestsPath, "DAL")
                    );

                var integrationTestsPath = Path.Combine(testsPath, $"{name}.Integration.Tests");
                await _cli.CreateProjectAsync("xunit", $"{name}.Integration.Tests", integrationTestsPath);
                await _cli.AddToSolutionAsync(slnPath, $"{integrationTestsPath}/{name}.Integration.Tests.csproj");
                await _cli.AddReferenceAsync(
                    $"{integrationTestsPath}/{name}.Integration.Tests.csproj",
                    $"{plPath}/{name}.PL.csproj");

                if (!isDryRun)
                    CreateDirectories(
                        Path.Combine(integrationTestsPath, "PL")
                    );
            }

            if (_cli is DryRunCliService footer)
                footer.PrintFooter();
            else
                Console.WriteLine($"N-Tier solution '{name}' generated successfully.");
        }

        private static void CreateDirectories(params string[] directories)
        {
            foreach (var directory in directories)
                Directory.CreateDirectory(directory);
        }
    }
}