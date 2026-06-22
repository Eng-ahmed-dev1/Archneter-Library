using System.Reflection;
using Archnet.Cli.Attributes;
using Archnet.Cli.Commands;
using Archnet.Cli.Models;
using Archnet.Cli.Parsing;
using Archnet.Cli.Services;

var commands =
    Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t =>
            typeof(IArchCommand).IsAssignableFrom(t) &&
            t is { IsClass: true, IsAbstract: false })
        .Select(t =>
        {
            var attr = t.GetCustomAttribute<CommandAttribute>();
            if (attr is null) return null;

            var instance = (IArchCommand)Activator.CreateInstance(t)!;
            return new CommandDescriptor { Name = attr.Name, Command = instance };
        })
        .Where(x => x is not null)
        .Cast<CommandDescriptor>()
        .ToList();

var dispatcher = new CommandDispatcher(commands);

if (args.Length == 0)
{
    await dispatcher.DispatchAsync("help", new CommandContext { Command = "help" });
    return;
}

var context = ArgumentParser.Parse(args);

await dispatcher.DispatchAsync(context.Command, context);