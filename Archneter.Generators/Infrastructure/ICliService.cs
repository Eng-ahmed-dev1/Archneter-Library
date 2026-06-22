namespace Archneter.Generators.Infrastructure
{
    /// <summary>
    /// Defines a service for interacting with the command-line interface (CLI), such as the .NET SDK.
    /// </summary>
    public interface ICliService
    {
        /// <summary>
        /// Executes a raw CLI command asynchronously.
        /// </summary>
        /// <param name="arguments">The command arguments to pass.</param>
        /// <param name="workingDirectory">The directory where the command should be executed.</param>
        Task RunAsync(string arguments, string workingDirectory);

        /// <summary>
        /// Creates a new project from a template.
        /// </summary>
        /// <param name="template">The template short name (e.g., classlib, webapi).</param>
        /// <param name="projectName">The name of the new project.</param>
        /// <param name="outputPath">The physical directory path where the project should be created.</param>
        Task CreateProjectAsync(string template, string projectName, string outputPath);

        /// <summary>
        /// Adds an existing project to a solution file.
        /// </summary>
        /// <param name="slnPath">The path to the .sln file.</param>
        /// <param name="projectPath">The path to the .csproj file being added.</param>
        Task AddToSolutionAsync(string slnPath, string projectPath);

        /// <summary>
        /// Adds a project-to-project reference.
        /// </summary>
        /// <param name="fromProjectPath">The path of the project that needs the reference.</param>
        /// <param name="toProjectPath">The path of the target project being referenced.</param>
        Task AddReferenceAsync(string fromProjectPath, string toProjectPath);
    }
}