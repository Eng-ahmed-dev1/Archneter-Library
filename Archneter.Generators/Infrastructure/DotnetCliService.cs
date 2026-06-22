using System.Diagnostics;

namespace Archnet.Generators.Infrastructure
{
    public static class DotnetCliService
    {
        public static async Task RunAsync(string arguments, string workingDirectory)
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

        public static Task CreateProjectAsync(string template, string projectName, string outputPath)
            => RunAsync($"new {template} -n {projectName} -o \"{outputPath}\"", Directory.GetCurrentDirectory());

        public static Task AddToSolutionAsync(string slnPath, string projectPath)
            => RunAsync($"sln \"{slnPath}\" add \"{projectPath}\"", Directory.GetCurrentDirectory());

        public static Task AddReferenceAsync(string fromProjectPath, string toProjectPath)
            => RunAsync($"add \"{fromProjectPath}\" reference \"{toProjectPath}\"", Directory.GetCurrentDirectory());
    }
}