using System;

namespace Chakal.Infrastructure.Models.Aggregates
{
    /// <summary>
    /// 1-minute room aggregation model mapped to the 'agg_room_1m' table in ClickHouse
    /// </summary>
    public class AggRoomMinute
    {
        /// <summary>
        /// Room ID
        /// </summary>
        public ulong RoomId { get; set; }
        
        /// <summary>
        /// Minute timestamp (rounded to start of minute)
        /// </summary>
        public DateTime TsMin { get; set; }
        
        /// <summary>
        /// Count of chat messages
        /// </summary>
        public uint Chats { get; set; }
        
        /// <summary>
        /// Count of likes
        /// </summary>
        public uint Likes { get; set; }
        
        /// <summary>
        /// Count of follows
        /// </summary>
        public uint Follows { get; set; }
        
        /// <summary>
        /// Count of shares
        /// </summary>
        public uint Shares { get; set; }
        
        /// <summary>
        /// Count of joins
        /// </summary>
        public uint Joins { get; set; }
        
        /// <summary>
        /// Count of gifts
        /// </summary>
        public uint Gifts { get; set; }
        
        /// <summary>
        /// Total diamonds from gifts
        /// </summary>
        public ulong Diamonds { get; set; }
        
        /// <summary>
        /// Count of subscriptions
        /// </summary>
        public uint Subs { get; set; }
    }
} 