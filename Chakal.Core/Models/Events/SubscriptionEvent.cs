using System;

namespace Chakal.Core.Models.Events
{
    /// <summary>
    /// Represents a subscription event
    /// </summary>
    public class SubscriptionEvent : BaseEvent
    {
        /// <summary>
        /// User ID of the subscriber
        /// </summary>
        public ulong UserId { get; set; }
        
        /// <summary>
        /// Username/nickname of the subscriber
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Subscription tier level
        /// </summary>
        public byte SubTier { get; set; }
        
        /// <summary>
        /// Total months subscribed
        /// </summary>
        public ushort MonthsTotal { get; set; }
        
        /// <summary>
        /// Flag indicating if this is a renewal or a new subscription
        /// </summary>
        public bool IsRenew { get; set; }
    }
} 