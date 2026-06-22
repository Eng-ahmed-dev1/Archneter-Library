using Archneter.Core.Models;

namespace Archneter.Core.Abstractions
{
    /// <summary>
    /// Defines the contract for an architecture generator.
    /// Implementing classes handle scaffolding specific architectural styles.
    /// </summary>
    public interface IArchitectureGenerator
    {
        /// <summary>
        /// Asynchronously generates the project architecture based on the provided options.
        /// </summary>
        /// <param name="options">The project configuration options specifying name, type, and features.</param>
        /// <returns>A task that represents the asynchronous generation operation.</returns>
        Task GenerateAsync(ProjectOptions options);
    }
}