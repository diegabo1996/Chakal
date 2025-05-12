using System.Threading;
using System.Threading.Tasks;
using Chakal.Core.Models.Events;

namespace Chakal.Core.Interfaces
{
    /// <summary>
    /// Interface for processing events from the stream
    /// </summary>
    public interface IEventProcessor
    {
        /// <summary>
        /// Process a chat message event
        /// </summary>
        /// <param name="chatEvent">The chat event to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ProcessChatEventAsync(ChatEvent chatEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process a gift event
        /// </summary>
        /// <param name="giftEvent">The gift event to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ProcessGiftEventAsync(GiftEvent giftEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process a social interaction event
        /// </summary>
        /// <param name="socialEvent">The social event to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ProcessSocialEventAsync(SocialEvent socialEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process a subscription event
        /// </summary>
        /// <param name="subscriptionEvent">The subscription event to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ProcessSubscriptionEventAsync(SubscriptionEvent subscriptionEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process a control event
        /// </summary>
        /// <param name="controlEvent">The control event to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ProcessControlEventAsync(ControlEvent controlEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Process a room stats event
        /// </summary>
        /// <param name="roomStatsEvent">The room stats event to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ProcessRoomStatsEventAsync(RoomStatsEvent roomStatsEvent, CancellationToken cancellationToken = default);
    }
} 