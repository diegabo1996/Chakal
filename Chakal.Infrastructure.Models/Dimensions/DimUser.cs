using System;

namespace Chakal.Infrastructure.Models.Dimensions
{
    /// <summary>
    /// User dimension model mapped to the 'dim_users' table in ClickHouse
    /// </summary>
    public class DimUser
    {
        /// <summary>
        /// User ID (primary key)
        /// </summary>
        public ulong UserId { get; set; }
        
        /// <summary>
        /// Unique identifier from the source platform
        /// </summary>
        public string UniqueId { get; set; }
        
        /// <summary>
        /// User nickname/username
        /// </summary>
        public string Nickname { get; set; }
        
        /// <summary>
        /// User's region or country
        /// </summary>
        public string Region { get; set; }
        
        /// <summary>
        /// Count of followers
        /// </summary>
        public uint FollowerCount { get; set; }
        
        /// <summary>
        /// Date and time when the user was first seen
        /// </summary>
        public DateTime FirstSeen { get; set; }
        
        /// <summary>
        /// Last date and time when the user was seen
        /// </summary>
        public DateTime LastSeen { get; set; }
        
        /// <summary>
        /// Version for ReplacingMergeTree engine
        /// </summary>
        public byte Version { get; set; } = 1;
    }
} 