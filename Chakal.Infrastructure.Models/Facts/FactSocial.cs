using System;

namespace Chakal.Infrastructure.Models.Facts
{
    /// <summary>
    /// Social interaction types enumeration
    /// </summary>
    public enum SocialType
    {
        /// <summary>
        /// Like interaction
        /// </summary>
        Unknown = 0,
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
    /// Social interaction fact model mapped to the 'fact_social' table in ClickHouse
    /// </summary>
    public class FactSocial
    {
        /// <summary>
        /// Event timestamp with millisecond precision
        /// </summary>
        public DateTime EventTime { get; set; }
        
        /// <summary>
        /// Room ID
        /// </summary>
        public ulong RoomId { get; set; }
        
        /// <summary>
        /// User ID who performed the social interaction
        /// </summary>
        public ulong UserId { get; set; }
        
        /// <summary>
        /// Type of social interaction
        /// </summary>
        public SocialType SocialType { get; set; }
        
        /// <summary>
        /// Count of interactions (default 1)
        /// </summary>
        public uint Count { get; set; } = 1;
    }
} 