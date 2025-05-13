using System;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Chakal.Core.Interfaces;
using Chakal.Infrastructure.Persistence;
using Chakal.Infrastructure.Sources;
using Chakal.Application.Services;

namespace Chakal.Infrastructure
{
    /// <summary>
    /// Extension methods for setting up infrastructure services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds infrastructure services to the service collection
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">Application configuration</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register ClickHouse persistence
            services.AddSingleton<IEventPersistence, ClickHouseEventPersistence>();

            // Always use MockEventSource for simplicity and to avoid TikTokLiveSharp issues
            //bool debugMode = false;
            //if (bool.TryParse(configuration["DEBUG_MODE"], out bool parsed))
            //{
            //    debugMode = parsed;
            //}

            //if (debugMode)
            //{
            //    services.AddSingleton<IEventSource, MockEventSource>();
            //}
            //else
            //{
            // Use TikTokEventSource in production
            services.AddSingleton<IEventSource, TikTokEventSource>();
            //}

            return services;
        }
    }
}