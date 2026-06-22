using System.Reflection;
using Archneter.Cli.Attributes;
using Archneter.Cli.Commands;
using Archneter.Cli.Models;

namespace Archneter.Cli.Services
{
    /// <summary>
    /// Discovers and registers CLI commands using reflection, resolving instances via Dependency Injection.
    /// </summary>
    public sealed class CommandRegistry
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<CommandDescriptor> _commands;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandRegistry"/> class.
        /// </summary>
        /// <param name="serviceProvider">The DI service provider to resolve command instances.</param>
        public CommandRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _commands = DiscoverCommands();
        }

        /// <summary>
        /// Gets the list of available commands.
        /// </summary>
        public IReadOnlyList<CommandDescriptor> GetCommands() => _commands;

        /// <summary>
        /// Extracts metadata for all registered commands to generate the help documentation.
        /// </summary>
        public IEnumerable<CommandMetadata> GetCommandsMetadata()
        {
            return _commands.Select(c => ExtractMetadata(c.Name, c.Command.GetType()));
        }

        private List<CommandDescriptor> DiscoverCommands()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IArchCommand).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
                .Select(t =>
                {
                    var attr = t.GetCustomAttribute<CommandAttribute>();
                    if (attr is null) return null;

                    var instance = (IArchCommand)_serviceProvider.GetService(t)!;
                    if (instance is null) return null;

                    return new CommandDescriptor { Name = attr.Name, Command = instance };
                })
                .Where(x => x is not null)
                .Cast<CommandDescriptor>()
                .ToList();
        }

        private static CommandMetadata ExtractMetadata(string name, Type type)
        {
            var descAttr = type.GetCustomAttribute<DescriptionAttribute>();
            var syntaxAttr = type.GetCustomAttribute<CommandSyntaxAttribute>();
            var optionAttrs = type.GetCustomAttributes<CommandOptionAttribute>();
            var exampleAttrs = type.GetCustomAttributes<CommandExampleAttribute>();

            return new CommandMetadata
            {
                Name = name,
                Description = descAttr?.Text ?? string.Empty,
                Syntax = syntaxAttr?.Syntax ?? name,
                Options = optionAttrs.Select(o => new OptionMetadata
                {
                    Template = o.Template,
                    Description = o.Description,
                    Details = o.Details.ToList()
                }).ToList(),
                Examples = exampleAttrs.Select(e => e.Example).ToList()
            };
        }
    }
}
