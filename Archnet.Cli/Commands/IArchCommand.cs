namespace Archnet.Cli.Commands;

public interface IArchCommand
{
    string Name { get; }
    Task ExecuteAsync();
}