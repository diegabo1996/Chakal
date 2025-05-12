using System;

namespace Chakal.Infrastructure.Models.Facts
{
    /// <summary>
    /// Gift fact model mapped to the 'fact_gift' table in ClickHouse
    /// </summary>
    public class FactGift
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
        /// User ID of the gift sender
        /// </summary>
        public ulong UserId { get; set; }
        
        /// <summary>
        /// Gift ID
        /// </summary>
        public uint GiftId { get; set; }
        
        /// <summary>
        /// Diamond count for this gift
        /// </summary>
        public uint DiamondCount { get; set; }
        
        /// <summary>
        /// Combo ID for multi-gifts
        /// </summary>
        public ulong ComboId { get; set; }
        
        /// <summary>
        /// Total count in the streak
        /// </summary>
        public uint StreakTotal { get; set; }
        
        /// <summary>
        /// Flag indicating if this is the end of a streak
        /// </summary>
        public byte RepeatEnd { get; set; }
    }
} 