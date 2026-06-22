namespace Archneter.Cli.Models
{
    /// <summary>
    /// Represents the parsed context and arguments of a CLI command execution.
    /// </summary>
    public class CommandContext
    {
        /// <summary>
        /// Gets or sets the name of the command being executed (e.g., 'new', 'help').
        /// </summary>
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target project name specified by the user.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a dictionary of key-value pair options passed to the command.
        /// </summary>
        public Dictionary<string, string> Options { get; set; } = new();

        /// <summary>
        /// Gets or sets a collection of boolean flags that appear without a value, e.g. --dry-run.
        /// </summary>
        public HashSet<string> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}