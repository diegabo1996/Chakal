using System;

namespace Chakal.Infrastructure.Models.Dimensions
{
    /// <summary>
    /// Room dimension model mapped to the 'dim_rooms' table in ClickHouse
    /// </summary>
    public class DimRoom
    {
        /// <summary>
        /// Room ID (primary key)
        /// </summary>
        public ulong RoomId { get; set; }
        
        /// <summary>
        /// Host user ID
        /// </summary>
        public ulong HostUserId { get; set; }
        
        /// <summary>
        /// Room title
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// Language used in the room
        /// </summary>
        public string Language { get; set; }
        
        /// <summary>
        /// Start time of the stream/room
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// End time of the stream/room (if ended)
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// Date and time when the room was inserted
        /// </summary>
        public DateTime InsertedAt { get; set; }
        
        /// <summary>
        /// Version for ReplacingMergeTree engine
        /// </summary>
        public byte Version { get; set; } = 1;
    }
} 