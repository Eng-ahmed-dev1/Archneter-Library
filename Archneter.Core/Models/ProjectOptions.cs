using Archnet.Core.Enums;

namespace Archnet.Core.Models
{
    public sealed class ProjectOptions
    {
        public string ProjectName { get; set; } = string.Empty;

        public ArchitectureType Architecture { get; set; }

        public bool GenerateTests { get; set; }
    }
}