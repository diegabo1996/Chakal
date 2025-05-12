using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Prometheus;
using Chakal.Application;
using Chakal.Infrastructure;

namespace Chakal.IngestSystem
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }

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
                    if (Enum.TryParse<LogLevel>(logLevel, out var level))
                    {
                        logging.SetMinimumLevel(level);
                    }
                    else
                    {
                        logging.SetMinimumLevel(LogLevel.Debug);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Add application services
                    services.AddApplicationServices();
                    
                    // Add infrastructure services
                    services.AddInfrastructureServices(hostContext.Configuration);
                    
                    // Add health checks
                    services.AddHealthChecks();
                    
                    // Add our main worker
                    services.AddHostedService<IngestWorker>();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.Configure(app =>
                    {
                        // Enable health endpoint
                        app.UseHealthChecks("/healthz", new HealthCheckOptions
                        {
                            ResponseWriter = async (context, report) =>
                            {
                                context.Response.ContentType = "application/json";
                                await context.Response.WriteAsync($"{{\"status\": \"{report.Status}\", \"checks\": {report.Entries.Count}}}");
                            }
                        });
                        
                        // Enable Prometheus metrics
                        app.UseMetricServer("/metrics");
                    });
                });
    }
}
