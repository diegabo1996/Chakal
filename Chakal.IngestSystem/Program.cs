using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Prometheus;
using Chakal.Application;
using Chakal.Infrastructure;

namespace Chakal.IngestSystem
{
    /// <summary>
    /// Entry point for the application
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            // Register metrics
            ConfigureMetrics();
            
            await host.RunAsync();
        }

        /// <summary>
        /// Creates the host builder
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>The host builder</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    
                    var logLevel = hostContext.Configuration["LOG_LEVEL"] ?? "Debug";
                    if (Enum.TryParse<LogLevel>(logLevel, true, out var parsedLevel))
                    {
                        logging.SetMinimumLevel(parsedLevel);
                    }
                    else
                    {
                        logging.SetMinimumLevel(LogLevel.Debug);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Register application services
                    services.AddApplicationServices();
                    
                    // Register infrastructure services
                    services.AddInfrastructureServices(hostContext.Configuration);
                    
                    // Register worker services
                    services.AddHostedService<IngestWorker>();
                    services.AddHostedService<BulkWriterWorker>();
                    
                    // Add health checks
                    services.AddHealthChecks()
                        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(app =>
                    {
                        // Configure HTTP pipeline
                        app.UseRouting();
                        
                        // Add Prometheus metrics middleware
                        app.UseHttpMetrics();
                        
                        app.UseEndpoints(endpoints =>
                        {
                            // Health check endpoint
                            endpoints.MapHealthChecks("/healthz", new HealthCheckOptions
                            {
                                Predicate = _ => true,
                                AllowCachingResponses = false
                            });
                            
                            // Prometheus metrics endpoint
                            endpoints.MapMetrics();
                        });
                    });
                });
        
        /// <summary>
        /// Configure Prometheus metrics
        /// </summary>
        private static void ConfigureMetrics()
        {
            // Define custom metrics
            Metrics.CreateCounter(
                "chakal_events_ingested_total", 
                "Total number of events ingested",
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_type" }
                });
            
            Metrics.CreateCounter(
                "chakal_events_persisted_total", 
                "Total number of events persisted to ClickHouse",
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_type" }
                });
            
            Metrics.CreateCounter(
                "chakal_events_broadcast_dropped_total", 
                "Total number of events dropped from broadcast channel due to backpressure",
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_type" }
                });
                
            Metrics.CreateCounter(
                "chakal_events_batched_total", 
                "Total number of events batched for persistence",
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_type" }
                });
            
            Metrics.CreateCounter(
                "chakal_batches_persisted_total", 
                "Total number of batches persisted to storage",
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_type" }
                });
        }
    }
}
