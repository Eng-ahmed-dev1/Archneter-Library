using Archnet.Cli.Commands;

namespace Archnet.Cli.Services
{
    public sealed class CommandDispatcher
    {
        private readonly IEnumerable<dynamic> _commands;

        public CommandDispatcher(IEnumerable<dynamic> commands)
        {
            _commands = commands;
        }

        public async Task DispatchAsync(string command, string[] args)
        {
            var target =
                _commands.FirstOrDefault(x => x.Name == command);

            if (target is null)
            {
                Console.WriteLine($"Unknown Command: {command}");
                return;
            }

            await target.Command.ExecuteAsync(args);
        }
    }
}