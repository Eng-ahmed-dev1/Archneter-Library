namespace Archnet.Cli.Commands;

public interface IArchCommand
{
    Task ExecuteAsync(CommandContext context);
}