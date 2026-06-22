using Archneter.Cli.Commands;
using Archneter.Cli.Models;
using Archneter.Cli.Parsing;
using Archneter.Cli.Services;
using Archneter.Generators.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

// 1. Setup Dependency Injection
var services = new ServiceCollection();

// Register Core CLI Services
services.AddTransient<DotnetCliService>();
services.AddTransient<DryRunCliService>();

// Register Application Services
services.AddSingleton<ArgumentParser>();
services.AddSingleton<CommandRegistry>();
services.AddSingleton<CommandDispatcher>();

// Dynamically Register All Commands for DI resolution
var commandTypes = typeof(IArchCommand).Assembly.GetTypes()
    .Where(t => typeof(IArchCommand).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

foreach (var type in commandTypes)
{
    services.AddTransient(type);
}

var serviceProvider = services.BuildServiceProvider();

// 2. Parse CLI Arguments
var parser = serviceProvider.GetRequiredService<ArgumentParser>();
var context = args.Length == 0
    ? new CommandContext { Command = "help" }
    : parser.Parse(args);

// 3. Dispatch Command
var dispatcher = serviceProvider.GetRequiredService<CommandDispatcher>();
await dispatcher.DispatchAsync(context.Command, context);