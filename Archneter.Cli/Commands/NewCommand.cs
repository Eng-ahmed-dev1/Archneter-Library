using Archnet.Cli.Attributes;
using Archnet.Cli.Services;
using Archnet.Core.Enums;
using Archnet.Core.Models;
using Archnet.Cli.Models;

namespace Archnet.Cli.Commands
{
    [Command("new")]
    [Description("Create a new architecture project")]
    public class NewCommand : IArchCommand
    {
        public async Task ExecuteAsync(CommandContext context)
        {
            if (string.IsNullOrWhiteSpace(context.ProjectName))
            {
                Console.WriteLine("Error: project name is required. Usage: archnet new <name> [--arch clean] [--tests true]");
                return;
            }

            var archKey = context.Options.GetValueOrDefault("--arch", "clean");
            var tests = context.Options.GetValueOrDefault("--tests", "false") == "true";

            if (!TryParseArchitecture(archKey, out var architecture))
            {
                Console.WriteLine($"Error: unknown architecture '{archKey}'. Supported: clean");
                return;
            }

            var options = new ProjectOptions
            {
                ProjectName = context.ProjectName,
                Architecture = architecture,
                GenerateTests = tests
            };

            var factory = new GeneratorFactory();
            var generator = factory.Get(options.Architecture);

            await generator.GenerateAsync(options);

            Console.WriteLine($"Project '{options.ProjectName}' created successfully.");
        }

        private static bool TryParseArchitecture(string key, out ArchitectureType architecture)
        {
            architecture = key.ToLowerInvariant() switch
            {
                "clean" => ArchitectureType.CleanArchitecture,
                "vsa" or "vertical-slice" => ArchitectureType.VerticalSlice,
                "modular" => ArchitectureType.ModularMonolith,
                "microservices" => ArchitectureType.Microservices,
                _ => default
            };

            return key.ToLowerInvariant() is "clean" or "vsa" or "vertical-slice" or "modular" or "microservices";
        }
    }
}