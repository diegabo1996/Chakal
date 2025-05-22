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
        /// Lista de IDs mencionados con "@usuario"
        /// </summary>
        public IReadOnlyList<ulong> MentionedUserIds { get; set; } = Array.Empty<ulong>();
        
        /// <summary>
        /// Código ISO-639 reportado por TikTok
        /// </summary>
        public string Language { get; set; } = string.Empty;
        
        /// <summary>
        /// Type of device used to send the message
        /// </summary>
        public string DeviceType { get; set; }
    }
} 