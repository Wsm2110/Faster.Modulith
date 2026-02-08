using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;

namespace Faster.Modulith.Contracts
{
    /// <summary>
    /// Configuration entry point for the Faster.Modulith system.
    /// This acts as a bridge to allow fluent configuration of modules 
    /// via the 'AddFasterModulith' extension method.
    /// </summary>
    public class FasterModulithOptions
    {
        /// <summary>
        /// The underlying ServiceCollection. 
        /// Exposed so module extensions can register their own services.
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// The application configuration (appsettings.json).
        /// Exposed so module extensions can read their specific settings.
        /// </summary>
        public IConfiguration Configuration { get; }

        public FasterModulithOptions(IServiceCollection services, IConfiguration configuration)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
    }
}