using Archnet.Core.Abstractions;
using Archnet.Core.Models;
using Archnet.Generators.Infrastructure;

namespace Archnet.Generators.CleanArchitecture
{
    public class CleanArchitectureGenerator : IArchitectureGenerator
    {
        public async Task GenerateAsync(ProjectOptions options)
        {
            var name = options.ProjectName;
            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), name);
            var srcPath = Path.Combine(rootPath, "src");
            var testsPath = Path.Combine(rootPath, "tests");

            Directory.CreateDirectory(rootPath);
            Directory.CreateDirectory(srcPath);

            await DotnetCliService.RunAsync($"new sln -n {name}", rootPath);

            var slnPath = Directory.GetFiles(rootPath, $"{name}.sln*").FirstOrDefault()
                ?? throw new Exception($"Solution file not found after creation in {rootPath}");

            var domainPath = Path.Combine(srcPath, $"{name}.Domain");
            var applicationPath = Path.Combine(srcPath, $"{name}.Application");
            var infrastructurePath = Path.Combine(srcPath, $"{name}.Infrastructure");
            var apiPath = Path.Combine(srcPath, $"{name}.Api");

            await DotnetCliService.CreateProjectAsync("classlib", $"{name}.Domain", domainPath);
            await DotnetCliService.CreateProjectAsync("classlib", $"{name}.Application", applicationPath);
            await DotnetCliService.CreateProjectAsync("classlib", $"{name}.Infrastructure", infrastructurePath);
            await DotnetCliService.CreateProjectAsync("webapi", $"{name}.Api", apiPath);

            await DotnetCliService.AddToSolutionAsync(slnPath, $"{domainPath}/{name}.Domain.csproj");
            await DotnetCliService.AddToSolutionAsync(slnPath, $"{applicationPath}/{name}.Application.csproj");
            await DotnetCliService.AddToSolutionAsync(slnPath, $"{infrastructurePath}/{name}.Infrastructure.csproj");
            await DotnetCliService.AddToSolutionAsync(slnPath, $"{apiPath}/{name}.Api.csproj");

            await DotnetCliService.AddReferenceAsync(
                $"{applicationPath}/{name}.Application.csproj",
                $"{domainPath}/{name}.Domain.csproj");

            await DotnetCliService.AddReferenceAsync(
                $"{infrastructurePath}/{name}.Infrastructure.csproj",
                $"{applicationPath}/{name}.Application.csproj");

            await DotnetCliService.AddReferenceAsync(
                $"{apiPath}/{name}.Api.csproj",
                $"{applicationPath}/{name}.Application.csproj");

            await DotnetCliService.AddReferenceAsync(
                $"{apiPath}/{name}.Api.csproj",
                $"{infrastructurePath}/{name}.Infrastructure.csproj");

            Directory.CreateDirectory(Path.Combine(applicationPath, "Common"));
            Directory.CreateDirectory(Path.Combine(applicationPath, "DependencyInjection"));
            Directory.CreateDirectory(Path.Combine(applicationPath, "DTOs"));
            Directory.CreateDirectory(Path.Combine(applicationPath, "UseCases"));

            if (options.GenerateTests)
            {
                Directory.CreateDirectory(testsPath);

                // Unit Tests
                var unitTestsPath = Path.Combine(testsPath, $"{name}.Unit.Tests");
                await DotnetCliService.CreateProjectAsync("xunit", $"{name}.Unit.Tests", unitTestsPath);
                await DotnetCliService.AddToSolutionAsync(slnPath, $"{unitTestsPath}/{name}.Unit.Tests.csproj");
                await DotnetCliService.AddReferenceAsync(
                    $"{unitTestsPath}/{name}.Unit.Tests.csproj",
                    $"{domainPath}/{name}.Domain.csproj");

                // Integration Tests
                var integrationTestsPath = Path.Combine(testsPath, $"{name}.Integration.Tests");
                await DotnetCliService.CreateProjectAsync("xunit", $"{name}.Integration.Tests", integrationTestsPath);
                await DotnetCliService.AddToSolutionAsync(slnPath, $"{integrationTestsPath}/{name}.Integration.Tests.csproj");
                await DotnetCliService.AddReferenceAsync(
                    $"{integrationTestsPath}/{name}.Integration.Tests.csproj",
                    $"{apiPath}/{name}.Api.csproj");
            }

            Console.WriteLine($"Clean Architecture solution '{name}' generated successfully.");
        }
    }
}