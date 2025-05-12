using System;

namespace Chakal.Infrastructure.Models.Facts
{
    /// <summary>
    /// Subscription fact model mapped to the 'fact_sub' table in ClickHouse
    /// </summary>
    public class FactSub
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
        /// User ID of the subscriber
        /// </summary>
        public ulong UserId { get; set; }
        
        /// <summary>
        /// Subscription tier level
        /// </summary>
        public byte SubTier { get; set; }
        
        /// <summary>
        /// Total months subscribed
        /// </summary>
        public ushort MonthsTotal { get; set; }
        
        /// <summary>
        /// Flag indicating if this is a renewal (1) or a new subscription (0)
        /// </summary>
        public byte IsRenew { get; set; }
    }
} 