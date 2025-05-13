using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Chakal.Core.Interfaces;
using Chakal.Core.Models.Events;

namespace Chakal.Application.Services
{
    /// <summary>
    /// Service for processing events from the event source
    /// </summary>
    public class EventProcessor : IEventProcessor
    {
        private readonly ILogger<EventProcessor> _logger;
        private readonly IEventChannel _broadcastChannel;
        private readonly IEventChannel _persistChannel;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="EventProcessor"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="broadcastChannel">The broadcast channel for realtime distribution</param>
        /// <param name="persistChannel">The persistence channel for storing to database</param>
        public EventProcessor(
            ILogger<EventProcessor> logger,
            IEventChannel broadcastChannel,
            IEventChannel persistChannel)
        {
            _logger = logger;
            _broadcastChannel = broadcastChannel;
            _persistChannel = persistChannel;
        }

        /// <inheritdoc />
        public async Task ProcessChatEventAsync(ChatEvent chatEvent, CancellationToken cancellationToken = default)
        {
            // First ensure persistence (reliable delivery)
            await _persistChannel.WriteAsync(chatEvent, cancellationToken);
            
            // Then broadcast (best-effort)
            _broadcastChannel.TryWrite(chatEvent);
            
            _logger.LogDebug(
                "Processed chat from {Username} ({UserId}): {Message}", 
                chatEvent.Username, 
                chatEvent.UserId, 
                chatEvent.Text
            );
        }

        /// <inheritdoc />
        public async Task ProcessGiftEventAsync(GiftEvent giftEvent, CancellationToken cancellationToken = default)
        {
            // First ensure persistence (reliable delivery)
            await _persistChannel.WriteAsync(giftEvent, cancellationToken);
            
            // Then broadcast (best-effort)
            _broadcastChannel.TryWrite(giftEvent);
            
            _logger.LogDebug(
                "Processed gift from {Username} ({UserId}): {GiftName} worth {DiamondCount} diamonds", 
                giftEvent.Username, 
                giftEvent.UserId, 
                giftEvent.GiftName, 
                giftEvent.DiamondCount
            );
        }

        /// <inheritdoc />
        public async Task ProcessSocialEventAsync(SocialEvent socialEvent, CancellationToken cancellationToken = default)
        {
            // First ensure persistence (reliable delivery)
            await _persistChannel.WriteAsync(socialEvent, cancellationToken);
            
            // Then broadcast (best-effort)
            _broadcastChannel.TryWrite(socialEvent);
            
            _logger.LogDebug(
                "Processed social event from {Username} ({UserId}): {SocialType} x{Count}", 
                socialEvent.Username, 
                socialEvent.UserId, 
                socialEvent.SocialType, 
                socialEvent.Count
            );
        }

        /// <inheritdoc />
        public async Task ProcessSubscriptionEventAsync(SubscriptionEvent subscriptionEvent, CancellationToken cancellationToken = default)
        {
            // First ensure persistence (reliable delivery)
            await _persistChannel.WriteAsync(subscriptionEvent, cancellationToken);
            
            // Then broadcast (best-effort)
            _broadcastChannel.TryWrite(subscriptionEvent);
            
            _logger.LogDebug(
                "Processed subscription from {Username} ({UserId}): Tier {SubTier} for {MonthsTotal} months", 
                subscriptionEvent.Username, 
                subscriptionEvent.UserId, 
                subscriptionEvent.SubTier, 
                subscriptionEvent.MonthsTotal
            );
        }

        /// <inheritdoc />
        public async Task ProcessControlEventAsync(ControlEvent controlEvent, CancellationToken cancellationToken = default)
        {
            // First ensure persistence (reliable delivery)
            await _persistChannel.WriteAsync(controlEvent, cancellationToken);
            
            // Then broadcast (best-effort)
            _broadcastChannel.TryWrite(controlEvent);
            
            _logger.LogDebug(
                "Processed control event: {ControlType} for room {RoomId}", 
                controlEvent.ControlType, 
                controlEvent.RoomId
            );
        }

        /// <inheritdoc />
        public async Task ProcessRoomStatsEventAsync(RoomStatsEvent roomStatsEvent, CancellationToken cancellationToken = default)
        {
            // First ensure persistence (reliable delivery)
            await _persistChannel.WriteAsync(roomStatsEvent, cancellationToken);
            
            // Then broadcast (best-effort)
            _broadcastChannel.TryWrite(roomStatsEvent);
            
            _logger.LogDebug(
                "Processed room stats: {ViewerCount} viewers, {LikeCount} likes, {ShareCount} shares for room {RoomId}", 
                roomStatsEvent.ViewerCount, 
                roomStatsEvent.LikeCount, 
                roomStatsEvent.ShareCount, 
                roomStatsEvent.RoomId
            );
        }
    }
} 