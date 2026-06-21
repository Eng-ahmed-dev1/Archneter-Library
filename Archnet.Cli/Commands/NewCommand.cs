using Archnet.Cli.Services;
using Archnet.Core.Enums;
using Archnet.Core.Models;
namespace Archnet.Cli.Commands
{
    public class NewCommand : IArchCommand
    {
        public string Name => "new";
        private string? GetArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == name && i + 1 < args.Length)
                    return args[i + 1];
            }

            return null;
        }
        public async Task ExecuteAsync(string[] args)
        {
            var projectName = args.Length > 0 ? args[0] : "DefaultProject";

            var arch = GetArg(args, "--arch") ?? "clean";
            var tests = GetArg(args, "--tests") == "true";

            var options = new ProjectOptions
            {
                ProjectName = projectName,
                Architecture = ArchitectureType.CleanArchitecture,
                GenerateTests = tests
            };

            var factory = new GeneratorFactory();
            var generator = factory.Get(options.Architecture);

            await generator.GenerateAsync(options);

            Console.WriteLine("Project Created Successfully..");
        }
    }
}