using Archnet.Cli.Services;
namespace Archnet.Cli.Commands
{
    public class NewCommand : IArchCommand
    {
        public string Name => "new";

        public async Task ExecuteAsync()
        {
            var wizard = new ProjectWizardService();
            var facory = new GeneratorFactory();
            var options = wizard.Create();
            var generator = facory.Get(options.Architecture);
            await generator.GenerateAsync(options);
            Console.WriteLine();
            Console.WriteLine("Project Created Successfully..");

        }
    }
}