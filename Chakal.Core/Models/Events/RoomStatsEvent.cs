using System;

namespace Chakal.Core.Models.Events
{
    /// <summary>
    /// Represents room statistics event
    /// </summary>
    public class RoomStatsEvent : BaseEvent
    {
        /// <summary>
        /// Current viewer count
        /// </summary>
        public uint ViewerCount { get; set; }
        
        /// <summary>
        /// Accumulated like count
        /// </summary>
        public uint LikeCount { get; set; }
        
        /// <summary>
        /// Accumulated share count
        /// </summary>
        public uint ShareCount { get; set; }
    }
} 