namespace Archneter.Core.Enums
{
    /// <summary>
    /// Specifies the various software architecture templates supported by the generator.
    /// </summary>
    public enum ArchitectureType
    {
        /// <summary>
        /// A 4-layer architecture comprising Domain, Application, Infrastructure, and API.
        /// </summary>
        CleanArchitecture = 1,

        /// <summary>
        /// A cohesive, feature-based architecture where each feature is a vertical slice.
        /// </summary>
        VerticalSlice = 2,

        /// <summary>
        /// A single host API with independent feature modules isolated in class libraries.
        /// </summary>
        ModularMonolith = 3,

        /// <summary>
        /// A distributed architecture with an API Gateway and independent microservices.
        /// </summary>
        Microservices = 4,

        /// <summary>
        /// A traditional 3-tier architecture with Presentation, Business Logic, and Data Access layers.
        /// </summary>
        NTier = 5
    }
}