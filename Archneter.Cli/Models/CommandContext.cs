namespace Archnet.Cli.Models
{
    public class CommandContext
    {
        public string Command { get; set; } = "";
        public string ProjectName { get; set; } = "";
        public Dictionary<string, string> Options { get; set; } = new();
    }
}
