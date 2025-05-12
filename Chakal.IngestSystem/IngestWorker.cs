using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Chakal.Core.Interfaces;
using Chakal.Infrastructure.Persistence;

namespace Chakal.IngestSystem
{
    /// <summary>
    /// Background service that manages the ingest pipeline
    /// </summary>
    public class IngestWorker : BackgroundService
    {
        private readonly ILogger<IngestWorker> _logger;
        private readonly IEventSource _eventSource;
        private readonly BulkWriter _bulkWriter;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="IngestWorker"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="eventSource">The event source</param>
        /// <param name="bulkWriter">The bulk writer</param>
        public IngestWorker(
            ILogger<IngestWorker> logger,
            IEventSource eventSource,
            BulkWriter bulkWriter)
        {
            _logger = logger;
            _eventSource = eventSource;
            _bulkWriter = bulkWriter;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Starting ingest worker for host: {Host}", _eventSource.RoomHost);
                
                // Start the event source
                await _eventSource.StartAsync(stoppingToken);
                
                _logger.LogInformation("Ingest worker started successfully. Listening for events from {Host}", _eventSource.RoomHost);
                
                // Wait until cancellation is requested
                while (!stoppingToken.IsCancellationRequested)
                {
                    // Periodically check connection status
                    if (!_eventSource.IsConnected)
                    {
                        _logger.LogWarning("Event source disconnected, attempting to reconnect");
                        await _eventSource.StartAsync(stoppingToken);
                    }
                    
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal cancellation
                _logger.LogInformation("Ingest worker stopping due to cancellation request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ingest worker");
            }
            finally
            {
                // Ensure event source is stopped
                try
                {
                    await _eventSource.StopAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping event source");
                }
                
                _logger.LogInformation("Ingest worker stopped");
            }
        }

        /// <inheritdoc />
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping ingest worker");
            
            // First stop the base service
            await base.StopAsync(cancellationToken);
            
            // Then make sure event source is stopped
            await _eventSource.StopAsync(cancellationToken);
            
            _logger.LogInformation("Ingest worker stopped");
        }
    }
} 