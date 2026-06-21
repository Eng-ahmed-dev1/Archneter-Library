using Archnet.Cli.Attributes;
using Archnet.Cli.Services;
using Archnet.Core.Enums;
using Archnet.Core.Models;

namespace Archnet.Cli.Commands;

[Command("new")]
public class NewCommand : IArchCommand
{
    public async Task ExecuteAsync(string[] args)
    {
        var projectName = args.Length > 0 ? args[0] : "DefaultProject";

        var options = ParseArgs(args, projectName);

        var factory = new GeneratorFactory();
        var generator = factory.Get(options.Architecture);

        await generator.GenerateAsync(options);

        Console.WriteLine("Project Created Successfully..");
    }

    private ProjectOptions ParseArgs(string[] args, string projectName)
    {
        var arch = GetArg(args, "--arch") ?? "clean";
        var tests = GetArg(args, "--tests") == "true";

        return new ProjectOptions
        {
            ProjectName = projectName,
            Architecture = ParseArchitecture(arch),
            GenerateTests = tests
        };
    }

    private ArchitectureType ParseArchitecture(string arch)
    {
        return arch.ToLower() switch
        {
            "clean" => ArchitectureType.CleanArchitecture,
            "vertical" => ArchitectureType.VerticalSlice,
            "modular" => ArchitectureType.ModularMonolith,
            "microservices" => ArchitectureType.Microservices,
            _ => ArchitectureType.CleanArchitecture
        };
    }

    private string? GetArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && i + 1 < args.Length)
                return args[i + 1];
        }

        return null;
    }
}