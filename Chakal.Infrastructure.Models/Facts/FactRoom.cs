using System;

namespace Chakal.Infrastructure.Models.Facts
{
    /// <summary>
    /// Room statistics fact model mapped to the 'fact_room' table in ClickHouse
    /// </summary>
    public class FactRoom
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