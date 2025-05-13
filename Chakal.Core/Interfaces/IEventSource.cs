using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chakal.Core.Interfaces
{
    /// <summary>
    /// Delegate for processing events from the source
    /// </summary>
    /// <param name="event">The event to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public delegate Task EventProcessingDelegate(object @event, CancellationToken cancellationToken);
    
    /// <summary>
    /// Interface for a source of events
    /// </summary>
    public interface IEventSource : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Start receiving events from the source
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StartAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Start receiving events from the source with a custom processor
        /// </summary>
        /// <param name="processor">The event processor delegate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StartAsync(EventProcessingDelegate processor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stop receiving events from the source
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        Task StopAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Check if the source is currently connected and active
        /// </summary>
        bool IsConnected { get; }
        
        /// <summary>
        /// Get the current room ID
        /// </summary>
        ulong RoomId { get; }
        
        /// <summary>
        /// Get the room host username
        /// </summary>
        string RoomHost { get; }
    }
} 