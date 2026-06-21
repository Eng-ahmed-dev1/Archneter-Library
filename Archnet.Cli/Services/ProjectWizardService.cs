using Archnet.Core.Enums;
using Archnet.Core.Models;

namespace Archnet.Cli.Services
{
    public class ProjectWizardService
    {
        public ProjectOptions Create()
        {
            Console.WriteLine("Welcome To Archnet");
            Console.WriteLine();

            Console.Write("Project Name: ");
            var projectName = Console.ReadLine() ?? "";

            Console.WriteLine();
            Console.WriteLine("Choose Architecture");
            Console.WriteLine("1. Clean Architecture");
            Console.WriteLine("2. Vertical Slice");
            Console.WriteLine("3. Modular Monolith");
            Console.WriteLine("4. Microservices");
            Console.Write("Enter number :");

            var architecture =
                (ArchitectureType)int.Parse(Console.ReadLine()!);

            Console.WriteLine();

            Console.Write("Generate Tests (y/n): ");
            var tests = Console.ReadLine()?.ToLower() == "y";

            return new ProjectOptions
            {
                ProjectName = projectName,
                Architecture = architecture,
                GenerateTests = tests
            };
        }

    }
}