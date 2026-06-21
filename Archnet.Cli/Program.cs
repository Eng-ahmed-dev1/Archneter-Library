using Archnet.Cli.Commands;
using Archnet.Cli.Services;

var dispatcher =
    new CommandDispatcher(
    [
        new NewCommand()
    ]);

if (args.Length == 0)
{
    Console.WriteLine(
        "Usage: archnet <command>");

    return;
}

await dispatcher.DispatchAsync(args[0]);