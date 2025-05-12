using System;
using System.Collections.Generic;

namespace Chakal.Core.Models.Events
{
    /// <summary>
    /// Represents a chat message event
    /// </summary>
    public class ChatEvent : BaseEvent
    {
        /// <summary>
        /// Unique message identifier
        /// </summary>
        public ulong MessageId { get; set; }
        
        /// <summary>
        /// User ID of the message author
        /// </summary>
        public ulong UserId { get; set; }
        
        /// <summary>
        /// Username/nickname of the message author
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Message text content
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// Emotes used in the message (Name -> Count)
        /// </summary>
        public Dictionary<string, uint> Emotes { get; set; } = new();
        
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