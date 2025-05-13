using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Chakal.Infrastructure.Persistence
{
    /// <summary>
    /// Factory for creating ClickHouse connections
    /// </summary>
    public class ClickHouseConnectionFactory : IDisposable, IAsyncDisposable
    {
        private readonly ILogger<ClickHouseConnectionFactory> _logger;
        private readonly string _connectionString;
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
        private bool _disposed;
        
        /// <summary>
        /// Initializes a new instance of <see cref="ClickHouseConnectionFactory"/>
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger</param>
        public ClickHouseConnectionFactory(
            IConfiguration configuration,
            ILogger<ClickHouseConnectionFactory> logger)
        {
            _logger = logger;
            _connectionString = configuration["CLICKHOUSE_CONN"] ?? "Host=192.168.1.230;Port=9000;Database=chakal;User=default;Password=Chakal123!";
            
            _logger.LogInformation("ClickHouse connection factory initialized with connection: {ConnectionString}", 
                _connectionString.Replace("Password=", "Password=***"));
        }
        
        /// <summary>
        /// Gets a connection to ClickHouse
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the connection operation</returns>
        public Task<object?> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("MOCK: Getting ClickHouse connection");
            return Task.FromResult<object?>(null);
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            
            _connectionSemaphore.Dispose();
            _disposed = true;
            
            _logger.LogInformation("ClickHouse connection factory disposed");
        }
        
        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;
            
            _connectionSemaphore.Dispose();
            _disposed = true;
            
            _logger.LogInformation("ClickHouse connection factory disposed asynchronously");
            return ValueTask.CompletedTask;
        }
    }
} 