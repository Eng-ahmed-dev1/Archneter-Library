using Archnet.Core.Abstractions;
using Archnet.Core.Models;
namespace Archnet.Generators.CleanArchitecture
{
    public sealed class CleanArchitectureGenerator : IArchitectureGenerator
    {
        public Task GenerateAsync(ProjectOptions options)
        {
            var root = options.ProjectName;
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(
            Path.Combine(root, "src"));
            Directory.CreateDirectory(
            Path.Combine(root, "tests"));
            return Task.CompletedTask;
        }
    }
}