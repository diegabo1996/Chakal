using System;
using System.Collections.Generic;

namespace Chakal.Infrastructure.Models.Facts
{
    /// <summary>
    /// Chat fact model mapped to the 'fact_chat' table in ClickHouse
    /// </summary>
    public class FactChat
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
        /// Message ID
        /// </summary>
        public ulong MessageId { get; set; }
        
        /// <summary>
        /// User ID of the message author
        /// </summary>
        public ulong UserId { get; set; }
        
        /// <summary>
        /// Message text content
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// Emotes used in the message (Name -> Count)
        /// </summary>
        public Dictionary<string, uint> Emotes { get; set; }
        
        /// <summary>
        /// ID of the message being replied to (if any)
        /// </summary>
        public ulong ReplyToId { get; set; }
        
        /// <summary>
        /// Type of device used to send the message
        /// </summary>
        public string DeviceType { get; set; }
    }
} 