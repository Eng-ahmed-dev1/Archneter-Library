using Archneter.Core.Abstractions;
using Archneter.Core.Models;
using Archneter.Generators.Infrastructure;

namespace Archneter.Generators.VerticalSlice
{
    /// <summary>
    /// Generates a Vertical Slice architecture solution with a single Web API project structured by highly cohesive feature folders.
    /// </summary>
    public class VerticalSliceArchitectureGenerator : IArchitectureGenerator
    {
        private readonly ICliService _cli;

        /// <summary>
        /// Initializes a new instance of the <see cref="VerticalSliceArchitectureGenerator"/> class.
        /// </summary>
        /// <param name="cli">The CLI service used to execute commands.</param>
        public VerticalSliceArchitectureGenerator(ICliService cli)
        {
            _cli = cli;
        }

        /// <summary>
        /// Asynchronously scaffolds the Vertical Slice API and constructs the necessary internal feature slices.
        /// </summary>
        /// <param name="options">The project configuration options containing the requested features.</param>
        public async Task GenerateAsync(ProjectOptions options)
        {
            var name = options.ProjectName;
            var features = options.ServiceNames; // For Vertical Slice, this contains the features

            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), name);
            var srcPath = Path.Combine(rootPath, "src");
            var testsPath = Path.Combine(rootPath, "tests");

            var isDryRun = _cli is DryRunCliService;

            if (_cli is DryRunCliService dryRunService)
                dryRunService.PrintHeader(name, "Vertical Slice");

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

            // ── Main API Project ─────────────────────────────────────
            var apiPath = Path.Combine(srcPath, $"{name}.Api");
            await _cli.CreateProjectAsync("webapi", $"{name}.Api", apiPath);
            await _cli.AddToSolutionAsync(slnPath, $"{apiPath}/{name}.Api.csproj");

            if (!isDryRun)
            {
                // Create foundational folders
                CreateDirectories(
                    Path.Combine(apiPath, "Infrastructure", "Persistence"),
                    Path.Combine(apiPath, "Common", "Behaviors"),
                    Path.Combine(apiPath, "Common", "Exceptions"),
                    Path.Combine(apiPath, "Features")
                );

                // Create feature folders
                foreach (var feature in features)
                {
                    var featurePath = Path.Combine(apiPath, "Features", feature);
                    CreateDirectories(
                        Path.Combine(featurePath, "Commands"),
                        Path.Combine(featurePath, "Queries"),
                        Path.Combine(featurePath, "DTOs"),
                        Path.Combine(featurePath, "Endpoints"),
                        Path.Combine(featurePath, "Models")
                    );
                }
            }
            else
            {
                // For DryRun, simulate the folder structure creation output
                var dryRun = (DryRunCliService)_cli;
                dryRun.RunAsync($"mkdir -p src/{name}.Api/Infrastructure/Persistence", rootPath).GetAwaiter().GetResult();
                dryRun.RunAsync($"mkdir -p src/{name}.Api/Common/Behaviors", rootPath).GetAwaiter().GetResult();
                dryRun.RunAsync($"mkdir -p src/{name}.Api/Common/Exceptions", rootPath).GetAwaiter().GetResult();

                foreach (var feature in features)
                {
                    dryRun.RunAsync($"mkdir -p src/{name}.Api/Features/{feature}/Commands", rootPath).GetAwaiter().GetResult();
                    dryRun.RunAsync($"mkdir -p src/{name}.Api/Features/{feature}/Queries", rootPath).GetAwaiter().GetResult();
                    dryRun.RunAsync($"mkdir -p src/{name}.Api/Features/{feature}/DTOs", rootPath).GetAwaiter().GetResult();
                    dryRun.RunAsync($"mkdir -p src/{name}.Api/Features/{feature}/Endpoints", rootPath).GetAwaiter().GetResult();
                    dryRun.RunAsync($"mkdir -p src/{name}.Api/Features/{feature}/Models", rootPath).GetAwaiter().GetResult();
                }
            }

            // ── Tests ────────────────────────────────────────────────
            if (options.GenerateTests)
            {
                if (!isDryRun)
                    Directory.CreateDirectory(testsPath);

                var unitTestsPath = Path.Combine(testsPath, $"{name}.Unit.Tests");
                var integrationTestsPath = Path.Combine(testsPath, $"{name}.Integration.Tests");

                await _cli.CreateProjectAsync("xunit", $"{name}.Unit.Tests", unitTestsPath);
                await _cli.CreateProjectAsync("xunit", $"{name}.Integration.Tests", integrationTestsPath);

                await _cli.AddToSolutionAsync(slnPath, $"{unitTestsPath}/{name}.Unit.Tests.csproj");
                await _cli.AddToSolutionAsync(slnPath, $"{integrationTestsPath}/{name}.Integration.Tests.csproj");

                await _cli.AddReferenceAsync(
                    $"{unitTestsPath}/{name}.Unit.Tests.csproj",
                    $"{apiPath}/{name}.Api.csproj");

                await _cli.AddReferenceAsync(
                    $"{integrationTestsPath}/{name}.Integration.Tests.csproj",
                    $"{apiPath}/{name}.Api.csproj");

                if (!isDryRun)
                {
                    // Mirror features structure in tests
                    foreach (var feature in features)
                    {
                        CreateDirectories(
                            Path.Combine(unitTestsPath, "Features", feature),
                            Path.Combine(integrationTestsPath, "Features", feature)
                        );
                    }
                }
            }

            if (_cli is DryRunCliService footer)
                footer.PrintFooter();
            else
                Console.WriteLine(
                    $"Vertical Slice solution '{name}' generated successfully " +
                    $"with features: {string.Join(", ", features)}.");
        }

        private static void CreateDirectories(params string[] dirs)
        {
            foreach (var dir in dirs)
                Directory.CreateDirectory(dir);
        }
    }
}