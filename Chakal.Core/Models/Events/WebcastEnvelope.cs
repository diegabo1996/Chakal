using System;
using System.Text.Json.Serialization;

namespace Chakal.Core.Models.Events
{
    /// <summary>
    /// Envelope for raw webcast events containing all original data
    /// </summary>
    public class WebcastEnvelope
    {
        /// <summary>
        /// Unique identifier for the event
        /// </summary>
        public string EventId { get; set; }
        
        /// <summary>
        /// Timestamp when the event was received
        /// </summary>
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Room ID where the event occurred
        /// </summary>
        public ulong RoomId { get; set; }
        
        /// <summary>
        /// Type of the event
        /// </summary>
        public string EventType { get; set; }
        
        /// <summary>
        /// Raw event data as a JSON object
        /// </summary>
        [JsonExtensionData]
        public System.Collections.Generic.Dictionary<string, object> RawData { get; set; }
    }
} 