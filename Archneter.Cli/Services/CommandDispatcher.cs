using Archnet.Cli.Commands;
using Archnet.Cli.Models;

namespace Archnet.Cli.Services
{
    public sealed class CommandDispatcher
    {
        private readonly IEnumerable<CommandDescriptor> _commands;

        public CommandDispatcher(IEnumerable<CommandDescriptor> commands)
        {
            _commands = commands;
        }

        public async Task DispatchAsync(string command, CommandContext context)
        {
            var target =
                _commands.FirstOrDefault(x => x.Name == command);

            if (target is null)
            {
                Console.WriteLine($"Unknown Command: {command}");
                return;
            }

            await target.Command.ExecuteAsync(context);
        }
    }
}