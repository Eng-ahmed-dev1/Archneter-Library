using Archnet.Core.Abstractions;
using Archnet.Core.Enums;
using Archnet.Generators.CleanArchitecture;

namespace Archnet.Cli.Services
{
    public sealed class GeneratorFactory
    {
        public IArchitectureGenerator Get(ArchitectureType type)
        {
            return type switch
            {
                ArchitectureType.CleanArchitecture
                => new CleanArchitectureGenerator(),

                _ => throw new NotImplementedException(
                    "Architecture not supported yet"
                )
            };
        }
    }
}