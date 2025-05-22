using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Chakal.Core.Interfaces;
using Chakal.Core.Models.Events;

namespace Chakal.Infrastructure.Services
{
    /// <summary>
    /// Background service that archives raw events to S3-compatible storage
    /// </summary>
    public class RawEventArchiverService : BackgroundService
    {
        private readonly Channel<WebcastEnvelope> _queue = Channel.CreateUnbounded<WebcastEnvelope>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        private readonly IEventArchiver _archiver;
        private readonly ILogger<RawEventArchiverService> _logger;

        public RawEventArchiverService(IEventArchiver archiver, ILogger<RawEventArchiverService> logger)
        {
            _archiver = archiver;
            _logger = logger;
        }

        /// <summary>
        /// Enqueues a raw event for archiving
        /// </summary>
        /// <param name="e">The event to archive</param>
        public void Enqueue(WebcastEnvelope e)
        {
            if (e == null) return;
            
            // Fire and forget - use TryWrite to never block the caller
            if (!_queue.Writer.TryWrite(e))
            {
                _logger.LogWarning("Failed to enqueue event for archiving, queue is full");
            }
        }

        /// <summary>
        /// Processes the queue of raw events
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("Starting RawEventArchiverService");
            
            await foreach (var e in _queue.Reader.ReadAllAsync(ct))
            {
                try
                {
                    await _archiver.ArchiveAsync(e, ct);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error archiving event: {EventType} for room {RoomId}", 
                        e.EventType, e.RoomId);
                    
                    // Consider retry logic here if needed
                }
            }
        }
    }
} 