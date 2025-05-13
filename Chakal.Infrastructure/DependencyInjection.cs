using System;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Chakal.Core.Interfaces;
using Chakal.Infrastructure.Persistence;
using Chakal.Infrastructure.Sources;
using Chakal.Infrastructure.Settings;
using Chakal.Infrastructure.Archiving;
using Chakal.Infrastructure.Services;
using Chakal.Application.Services;
using Minio;

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

            // Configure MinIO settings
            services.Configure<MinioSettings>(s =>
            {
                s.Endpoint = configuration["S3_ENDPOINT"]?.Replace("http://", "").Replace("https://", "") ?? "192.168.1.230:9100";
                s.AccessKey = configuration["S3_ACCESS_KEY"] ?? "vFpdN5CJGjY5o0jPKVww";
                s.SecretKey = configuration["S3_SECRET_KEY"] ?? "5KpII0ID5oOqFOmSR8G6rd7HJJNBFb9eD1Xb3dTB";
                s.BucketName = configuration["S3_BUCKET"] ?? "dev-chakal-raw";
                s.UseSSL = configuration["S3_ENDPOINT"]?.StartsWith("https://") ?? false;
            });

            // Register MinIO client
            services.AddSingleton<IMinioClient>(sp =>
            {
                var cfg = sp.GetRequiredService<IOptions<MinioSettings>>().Value;
                return new MinioClient()
                    .WithEndpoint(cfg.Endpoint)
                    .WithCredentials(cfg.AccessKey, cfg.SecretKey)
                    .WithSSL(cfg.UseSSL)
                    .Build();
            });

            // Register event archiver and background service
            services.AddSingleton<IEventArchiver, MinioEventArchiver>();
            services.AddSingleton<RawEventArchiverService>();
            services.AddHostedService(sp => sp.GetRequiredService<RawEventArchiverService>());

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