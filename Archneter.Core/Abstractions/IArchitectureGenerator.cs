using Archnet.Core.Models;

namespace Archnet.Core.Abstractions
{
    public interface IArchitectureGenerator
    {
        Task GenerateAsync(ProjectOptions options);
    }
}