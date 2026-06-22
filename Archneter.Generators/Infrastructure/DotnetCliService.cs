using System.Diagnostics;

namespace Archneter.Generators.Infrastructure
{
    /// <summary>
    /// A concrete implementation of <see cref="ICliService"/> that executes real commands against the installed .NET SDK.
    /// </summary>
    public class DotnetCliService : ICliService
    {
        /// <summary>
        /// Executes a raw dotnet command asynchronously using <see cref="Process"/>.
        /// </summary>
        /// <param name="arguments">The dotnet command arguments.</param>
        /// <param name="workingDirectory">The working directory context.</param>
        public async Task RunAsync(string arguments, string workingDirectory)
        {
            var process = new Process();

            process.StartInfo.FileName = "dotnet";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.WorkingDirectory = workingDirectory;

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
                throw new Exception($"Command failed: dotnet {arguments}\n{error}");

            if (!string.IsNullOrWhiteSpace(output))
                Console.WriteLine(output);
        }

        /// <summary>
        /// Scaffolds a new dotnet project.
        /// </summary>
        public Task CreateProjectAsync(string template, string projectName, string outputPath)
            => RunAsync($"new {template} -n {projectName} -o \"{outputPath}\"", Directory.GetCurrentDirectory());

        /// <summary>
        /// Adds a project file to the specified solution.
        /// </summary>
        public Task AddToSolutionAsync(string slnPath, string projectPath)
            => RunAsync($"sln \"{slnPath}\" add \"{projectPath}\"", Directory.GetCurrentDirectory());

        /// <summary>
        /// Adds a project reference from one project to another.
        /// </summary>
        public Task AddReferenceAsync(string fromProjectPath, string toProjectPath)
            => RunAsync($"add \"{fromProjectPath}\" reference \"{toProjectPath}\"", Directory.GetCurrentDirectory());
    }
}