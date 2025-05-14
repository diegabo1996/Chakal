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