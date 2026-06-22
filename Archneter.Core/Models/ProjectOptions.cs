using Archneter.Core.Enums;

namespace Archneter.Core.Models
{
    /// <summary>
    /// Represents the configuration options chosen by the user for project scaffolding.
    /// </summary>
    public sealed class ProjectOptions
    {
        /// <summary>
        /// Gets or sets the name of the project or solution to generate.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the architectural style to scaffold.
        /// </summary>
        public ArchitectureType Architecture { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to generate unit and integration test projects.
        /// </summary>
        public bool GenerateTests { get; set; }

        /// <summary>
        /// Gets or sets the list of specific service, module, or feature names to scaffold internally.
        /// </summary>
        public List<string> ServiceNames { get; set; } = new();
    }
}