using System;

namespace Chakal.Core.Models.Events
{
    /// <summary>
    /// Base class for all events in the system
    /// </summary>
    public abstract class BaseEvent
    {
        /// <summary>
        /// Event timestamp with millisecond precision
        /// </summary>
        public DateTime EventTime { get; set; }
        
        /// <summary>
        /// Room ID where the event occurred
        /// </summary>
        public ulong RoomId { get; set; }
    }
} 