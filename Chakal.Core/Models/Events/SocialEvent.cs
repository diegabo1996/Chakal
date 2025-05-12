using System;

namespace Chakal.Core.Models.Events
{
    /// <summary>
    /// Types of social interactions
    /// </summary>
    public enum SocialInteractionType
    {
        /// <summary>
        /// Like interaction
        /// </summary>
        Like = 1,
        
        /// <summary>
        /// Follow interaction
        /// </summary>
        Follow = 2,
        
        /// <summary>
        /// Share interaction
        /// </summary>
        Share = 3,
        
        /// <summary>
        /// Join room interaction
        /// </summary>
        Join = 4
    }
    
    /// <summary>
    /// Represents a social interaction event (like, follow, share, join)
    /// </summary>
    public class SocialEvent : BaseEvent
    {
        /// <summary>
        /// User ID who performed the social interaction
        /// </summary>
        public ulong UserId { get; set; }
        
        /// <summary>
        /// Username/nickname who performed the social interaction
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Type of social interaction
        /// </summary>
        public SocialInteractionType SocialType { get; set; }
        
        /// <summary>
        /// Count of interactions (default 1)
        /// </summary>
        public uint Count { get; set; } = 1;
    }
} 