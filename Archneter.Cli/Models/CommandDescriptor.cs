using Archnet.Cli.Commands;

namespace Archnet.Cli.Models
{
    public class CommandDescriptor
    {
        public string Name { get; set; } = string.Empty;

        public IArchCommand Command { get; set; } = default!;
    }
}