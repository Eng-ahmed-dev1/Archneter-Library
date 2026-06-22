using System.Reflection;
using Archnet.Cli.Attributes;
using Archnet.Cli.Models;

namespace Archnet.Cli.Commands
{
    [Command("help")]
    [Description("Display available commands")]
    public sealed class HelpCommand : IArchCommand
    {
        public Task ExecuteAsync(CommandContext context)
        {
            Console.WriteLine();
            Console.WriteLine("Archnet CLI");
            Console.WriteLine();
            Console.WriteLine("Available Commands:");
            Console.WriteLine();

            var commands =
                Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t =>
                        typeof(IArchCommand).IsAssignableFrom(t) &&
                        !t.IsInterface &&
                        !t.IsAbstract);

            foreach (var command in commands)
            {
                var commandAttr =
                    command.GetCustomAttribute<CommandAttribute>();

                if (commandAttr is null)
                    continue;

                var descriptionAttr =
                    command.GetCustomAttribute<DescriptionAttribute>();

                var description =
                    descriptionAttr?.Text ?? "No description";

                Console.WriteLine(
                    $"  {commandAttr.Name,-15} {description}");
            }

            return Task.CompletedTask;
        }
    }
}