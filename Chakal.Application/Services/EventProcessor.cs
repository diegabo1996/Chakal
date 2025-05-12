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
            await _broadcastChannel.WriteAsync(chatEvent, cancellationToken);
            await _persistChannel.WriteAsync(chatEvent, cancellationToken);
            
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
            await _broadcastChannel.WriteAsync(giftEvent, cancellationToken);
            await _persistChannel.WriteAsync(giftEvent, cancellationToken);
            
            _logger.LogDebug(
                "Processed gift from {Username} ({UserId}): {GiftName} x{Count} ({Diamonds} diamonds)", 
                giftEvent.Username, 
                giftEvent.UserId, 
                giftEvent.GiftName, 
                giftEvent.StreakTotal, 
                giftEvent.DiamondCount
            );
        }

        /// <inheritdoc />
        public async Task ProcessSocialEventAsync(SocialEvent socialEvent, CancellationToken cancellationToken = default)
        {
            await _broadcastChannel.WriteAsync(socialEvent, cancellationToken);
            await _persistChannel.WriteAsync(socialEvent, cancellationToken);
            
            _logger.LogDebug(
                "Processed social event from {Username} ({UserId}): {SocialType}", 
                socialEvent.Username, 
                socialEvent.UserId, 
                socialEvent.SocialType
            );
        }

        /// <inheritdoc />
        public async Task ProcessSubscriptionEventAsync(SubscriptionEvent subscriptionEvent, CancellationToken cancellationToken = default)
        {
            await _broadcastChannel.WriteAsync(subscriptionEvent, cancellationToken);
            await _persistChannel.WriteAsync(subscriptionEvent, cancellationToken);
            
            _logger.LogDebug(
                "Processed subscription from {Username} ({UserId}): Tier {Tier}, Month {Month}", 
                subscriptionEvent.Username, 
                subscriptionEvent.UserId, 
                subscriptionEvent.SubTier, 
                subscriptionEvent.MonthsTotal
            );
        }

        /// <inheritdoc />
        public async Task ProcessControlEventAsync(ControlEvent controlEvent, CancellationToken cancellationToken = default)
        {
            await _broadcastChannel.WriteAsync(controlEvent, cancellationToken);
            await _persistChannel.WriteAsync(controlEvent, cancellationToken);
            
            _logger.LogInformation(
                "Processed control event: {ControlType} with value {Value}", 
                controlEvent.ControlType, 
                controlEvent.Value
            );
        }

        /// <inheritdoc />
        public async Task ProcessRoomStatsEventAsync(RoomStatsEvent roomStatsEvent, CancellationToken cancellationToken = default)
        {
            await _broadcastChannel.WriteAsync(roomStatsEvent, cancellationToken);
            await _persistChannel.WriteAsync(roomStatsEvent, cancellationToken);
            
            _logger.LogDebug(
                "Processed room stats: {ViewerCount} viewers, {LikeCount} likes, {ShareCount} shares", 
                roomStatsEvent.ViewerCount, 
                roomStatsEvent.LikeCount, 
                roomStatsEvent.ShareCount
            );
        }
    }
} 