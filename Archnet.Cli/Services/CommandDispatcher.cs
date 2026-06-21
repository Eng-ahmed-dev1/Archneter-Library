using Archnet.Cli.Commands;

namespace Archnet.Cli.Services
{
    public sealed class CommandDispatcher
    {
        private readonly IEnumerable<IArchCommand> _commands;
        public CommandDispatcher(IEnumerable<IArchCommand> commands)
        => _commands = commands;

        public async Task DispatchAsync(string command)
        {
            var target =
    _commands.FirstOrDefault(
        x => string.Equals(x.Name, command, StringComparison.OrdinalIgnoreCase));

            if (target is null)
            {
                Console.WriteLine(
               $"Unknown Command: {command}");

                return;
            }
            await target.ExecuteAsync();
        }
    }
}