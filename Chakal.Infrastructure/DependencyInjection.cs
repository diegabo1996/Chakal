using System;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Chakal.Core.Interfaces;
using Chakal.Infrastructure.Persistence;
using Chakal.Infrastructure.Sources;

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
            // Register ClickHouse services
            services.AddSingleton<ClickHouseConnectionFactory>();
            services.AddSingleton<IEventPersistence, ClickHouseEventPersistence>();
            
            // Always use MockEventSource for simplicity and to avoid TikTokLiveSharp issues
            services.AddSingleton<IEventSource, MockEventSource>();
            
            // Register BulkWriter
            services.AddSingleton<BulkWriter>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<BulkWriter>>();
                var persistence = provider.GetRequiredService<IEventPersistence>();
                
                // Find the event channel registered with the name "PersistChannel"
                var eventChannel = provider.GetRequiredService<IEventChannel>();
                
                // We need to access the channel reader through reflection
                // This is a workaround because we can't directly access the Reader property from IEventChannel
                var channelReaderField = eventChannel.GetType().GetProperty("Reader");
                if (channelReaderField == null)
                {
                    throw new InvalidOperationException("Unable to get Reader from EventChannel");
                }
                
                var channelReader = channelReaderField.GetValue(eventChannel) as ChannelReader<object>;
                if (channelReader == null)
                {
                    throw new InvalidOperationException("Invalid channel reader type");
                }
                
                var options = new BulkWriterOptions
                {
                    MaxBatchSize = 5000,
                    MaxWaitTimeMs = 500
                };
                
                return new BulkWriter(logger, persistence, channelReader, options);
            });
            
            return services;
        }
    }
} 