using System;

namespace Chakal.Infrastructure.Models.Facts
{
    /// <summary>
    /// Control event types enumeration
    /// </summary>
    public enum ControlType
    {
        /// <summary>
        /// Live stream started
        /// </summary>
        LiveStart = 1,
        
        /// <summary>
        /// Live stream paused
        /// </summary>
        LivePause = 2,
        
        /// <summary>
        /// Live stream resumed
        /// </summary>
        LiveResume = 3,
        
        /// <summary>
        /// Live stream ended
        /// </summary>
        LiveEnd = 4
    }
    
    /// <summary>
    /// Control event fact model mapped to the 'fact_control' table in ClickHouse
    /// </summary>
    public class FactControl
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
        /// Type of control event
        /// </summary>
        public ControlType ControlType { get; set; }
        
        /// <summary>
        /// Value or additional information related to the control event
        /// </summary>
        public string Value { get; set; }
    }
} 