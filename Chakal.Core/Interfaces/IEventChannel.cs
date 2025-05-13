using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Chakal.Core.Models.Events;

namespace Chakal.Core.Interfaces
{
    /// <summary>
    /// Interface for a channel that handles events
    /// </summary>
    public interface IEventChannel : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets the underlying channel reader
        /// </summary>
        ChannelReader<object> Reader { get; }
        
        /// <summary>
        /// Write a chat event to the channel
        /// </summary>
        /// <param name="chatEvent">The chat event to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        ValueTask WriteAsync(ChatEvent chatEvent, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Try to write a chat event to the channel (best-effort, no blocking)
        /// </summary>
        /// <param name="chatEvent">The chat event to write</param>
        /// <returns>True if write succeeded, false if channel is full or closed</returns>
        bool TryWrite(ChatEvent chatEvent);
        
        /// <summary>
        /// Write a gift event to the channel
        /// </summary>
        /// <param name="giftEvent">The gift event to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        ValueTask WriteAsync(GiftEvent giftEvent, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Try to write a gift event to the channel (best-effort, no blocking)
        /// </summary>
        /// <param name="giftEvent">The gift event to write</param>
        /// <returns>True if write succeeded, false if channel is full or closed</returns>
        bool TryWrite(GiftEvent giftEvent);
        
        /// <summary>
        /// Write a social event to the channel
        /// </summary>
        /// <param name="socialEvent">The social event to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        ValueTask WriteAsync(SocialEvent socialEvent, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Try to write a social event to the channel (best-effort, no blocking)
        /// </summary>
        /// <param name="socialEvent">The social event to write</param>
        /// <returns>True if write succeeded, false if channel is full or closed</returns>
        bool TryWrite(SocialEvent socialEvent);
        
        /// <summary>
        /// Write a subscription event to the channel
        /// </summary>
        /// <param name="subscriptionEvent">The subscription event to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        ValueTask WriteAsync(SubscriptionEvent subscriptionEvent, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Try to write a subscription event to the channel (best-effort, no blocking)
        /// </summary>
        /// <param name="subscriptionEvent">The subscription event to write</param>
        /// <returns>True if write succeeded, false if channel is full or closed</returns>
        bool TryWrite(SubscriptionEvent subscriptionEvent);
        
        /// <summary>
        /// Write a control event to the channel
        /// </summary>
        /// <param name="controlEvent">The control event to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        ValueTask WriteAsync(ControlEvent controlEvent, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Try to write a control event to the channel (best-effort, no blocking)
        /// </summary>
        /// <param name="controlEvent">The control event to write</param>
        /// <returns>True if write succeeded, false if channel is full or closed</returns>
        bool TryWrite(ControlEvent controlEvent);
        
        /// <summary>
        /// Write a room stats event to the channel
        /// </summary>
        /// <param name="roomStatsEvent">The room stats event to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        ValueTask WriteAsync(RoomStatsEvent roomStatsEvent, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Try to write a room stats event to the channel (best-effort, no blocking)
        /// </summary>
        /// <param name="roomStatsEvent">The room stats event to write</param>
        /// <returns>True if write succeeded, false if channel is full or closed</returns>
        bool TryWrite(RoomStatsEvent roomStatsEvent);
    }
} 