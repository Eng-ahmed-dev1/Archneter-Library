using Archneter.Core.Abstractions;
using Archneter.Core.Enums;
using Archneter.Generators.CleanArchitecture;
using Archneter.Generators.Infrastructure;
using Archneter.Generators.Microservices;
using Archneter.Generators.NTier;
using Archneter.Generators.ModularMonolith;
using Archneter.Generators.VerticalSlice;
using Microsoft.Extensions.DependencyInjection;

namespace Archneter.Cli.Services;

/// <summary>
/// A factory responsible for resolving the appropriate architecture generator using Dependency Injection.
/// </summary>
public sealed class GeneratorFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratorFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The DI service provider.</param>
    public GeneratorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates and resolves the corresponding generator based on architecture type and execution mode.
    /// </summary>
    /// <param name="type">The architectural style to generate.</param>
    /// <param name="isDryRun">Whether to simulate execution without modifying the disk.</param>
    /// <returns>An instance of an <see cref="IArchitectureGenerator"/>.</returns>
    public IArchitectureGenerator Create(ArchitectureType type, bool isDryRun = false)
    {
        ICliService cli = isDryRun
            ? _serviceProvider.GetRequiredService<DryRunCliService>()
            : _serviceProvider.GetRequiredService<DotnetCliService>();

        Type genType = type switch
        {
            ArchitectureType.CleanArchitecture => typeof(CleanArchitectureGenerator),
            ArchitectureType.Microservices => typeof(MicroservicesGenerator),
            ArchitectureType.NTier => typeof(NTierArchitectureGenerator),
            ArchitectureType.ModularMonolith => typeof(ModularMonolithArchitectureGenerator),
            ArchitectureType.VerticalSlice => typeof(VerticalSliceArchitectureGenerator),
            _ => throw new NotSupportedException($"Architecture '{type}' is not supported yet.")
        };

        return (IArchitectureGenerator)ActivatorUtilities.CreateInstance(_serviceProvider, genType, cli);
    }
}