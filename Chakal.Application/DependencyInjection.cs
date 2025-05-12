using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Chakal.Core.Interfaces;
using Chakal.Application.Services;

namespace Chakal.Application
{
    /// <summary>
    /// Extension methods for setting up application services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds application services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register the event processor
            services.AddSingleton<IEventProcessor, EventProcessor>();
            
            // Register channel factory and create channels
            services.AddSingleton<EventChannelFactory>();
            
            // Register channels as singletons
            services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<EventChannelFactory>();
                return factory.CreateUnboundedChannel("BroadcastChannel");
            });
            
            services.AddSingleton(provider =>
            {
                var factory = provider.GetRequiredService<EventChannelFactory>();
                return factory.CreateBoundedChannel("PersistChannel", 20000, BoundedChannelFullMode.Wait);
            });
            
            return services;
        }
    }
} 