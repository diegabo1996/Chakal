using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chakal.Core.Models.Events;

namespace Chakal.Core.Interfaces
{
    /// <summary>
    /// Interface for persisting events to a data store
    /// </summary>
    public interface IEventPersistence : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Persist chat events in bulk
        /// </summary>
        /// <param name="events">Collection of chat events to persist</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PersistChatEventsAsync(IEnumerable<ChatEvent> events, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Persist gift events in bulk
        /// </summary>
        /// <param name="events">Collection of gift events to persist</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PersistGiftEventsAsync(IEnumerable<GiftEvent> events, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Persist social events in bulk
        /// </summary>
        /// <param name="events">Collection of social events to persist</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PersistSocialEventsAsync(IEnumerable<SocialEvent> events, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Persist subscription events in bulk
        /// </summary>
        /// <param name="events">Collection of subscription events to persist</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PersistSubscriptionEventsAsync(IEnumerable<SubscriptionEvent> events, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Persist control events in bulk
        /// </summary>
        /// <param name="events">Collection of control events to persist</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PersistControlEventsAsync(IEnumerable<ControlEvent> events, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Persist room stats events in bulk
        /// </summary>
        /// <param name="events">Collection of room stats events to persist</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PersistRoomStatsEventsAsync(IEnumerable<RoomStatsEvent> events, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Persist user information
        /// </summary>
        /// <param name="userId">User ID</param>
        /// <param name="uniqueId">Platform-specific unique ID</param>
        /// <param name="nickname">User nickname/username</param>
        /// <param name="region">User region/country</param>
        /// <param name="followerCount">User follower count</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PersistUserInfoAsync(ulong userId, string uniqueId, string nickname, string region, uint followerCount, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Persist gift information
        /// </summary>
        /// <param name="giftId">Gift ID</param>
        /// <param name="name">Gift name</param>
        /// <param name="coinCost">Cost in coins</param>
        /// <param name="diamondCost">Cost in diamonds</param>
        /// <param name="isExclusive">Whether the gift is exclusive</param>
        /// <param name="isOnPanel">Whether the gift is on panel</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PersistGiftInfoAsync(uint giftId, string name, uint coinCost, uint diamondCost, bool isExclusive, bool isOnPanel, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Persist room information
        /// </summary>
        /// <param name="roomId">Room ID</param>
        /// <param name="hostUserId">Host user ID</param>
        /// <param name="title">Room title</param>
        /// <param name="language">Room language</param>
        /// <param name="startTime">Start time of the stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PersistRoomInfoAsync(ulong roomId, ulong hostUserId, string title, string language, DateTime startTime, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Update room end time
        /// </summary>
        /// <param name="roomId">Room ID</param>
        /// <param name="endTime">End time of the stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task UpdateRoomEndTimeAsync(ulong roomId, DateTime endTime, CancellationToken cancellationToken = default);
    }
} 