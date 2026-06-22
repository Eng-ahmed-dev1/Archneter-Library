namespace Archnet.Cli.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DescriptionAttribute : Attribute
    {
        public string Text { get; }

        public DescriptionAttribute(string text)
        {
            Text = text;
        }
    }
}