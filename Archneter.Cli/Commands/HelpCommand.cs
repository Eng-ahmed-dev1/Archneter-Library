using Archneter.Cli.Attributes;
using Archneter.Cli.Models;
using Archneter.Cli.Services;

namespace Archneter.Cli.Commands;

/// <summary>
/// Represents the 'help' command, responsible for displaying available CLI commands, options, and examples.
/// </summary>
[Command("help")]
[Description("Display available commands")]
[CommandSyntax("help")]
public sealed class HelpCommand : IArchCommand
{
    private readonly CommandRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpCommand"/> class.
    /// </summary>
    /// <param name="registry">The command registry used to fetch metadata.</param>
    public HelpCommand(CommandRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Asynchronously executes the help command, printing metadata to the console.
    /// </summary>
    /// <param name="context">The context of the executed command.</param>
    public Task ExecuteAsync(CommandContext context)
    {
        var writer = new CliConsoleWriter();
        var metadataList = _registry.GetCommandsMetadata().ToList();

        writer.WriteLine();
        writer.WriteLine("Archneter CLI");

        // USAGE
        writer.WriteHeader("Usage");
        writer.WriteRow("archneter <command> [options]", "", indent: 2, col1Width: 0);

        // COMMANDS
        writer.WriteHeader("Commands");
        int maxSyntaxLen = metadataList.Max(m => m.Syntax.Length);
        int commandPadding = Math.Max(maxSyntaxLen + 4, 30);

        foreach (var cmd in metadataList)
        {
            writer.WriteRow(cmd.Syntax, cmd.Description, indent: 2, col1Width: commandPadding);
        }

        // OPTIONS
        var commandsWithOpts = metadataList.Where(m => m.Options.Any()).ToList();
        if (commandsWithOpts.Any())
        {
            writer.WriteHeader("Options");
            foreach (var cmd in commandsWithOpts)
            {
                writer.WriteLine($"  For '{cmd.Name}':");
                foreach (var opt in cmd.Options)
                {
                    writer.WriteRow(opt.Template, opt.Description, indent: 4, col1Width: 28);
                    foreach (var detail in opt.Details)
                    {
                        writer.WriteLine($"      {detail}");
                    }
                    writer.WriteLine();
                }
            }
        }

        // EXAMPLES
        var examples = metadataList.SelectMany(m => m.Examples).ToList();
        if (examples.Any())
        {
            writer.WriteHeader("Examples");
            foreach (var ex in examples)
            {
                writer.WriteLine($"  {ex}");
            }
        }

        writer.WriteLine();
        return Task.CompletedTask;
    }
}