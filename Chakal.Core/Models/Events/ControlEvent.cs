using System;

namespace Chakal.Core.Models.Events
{
    /// <summary>
    /// Types of control events
    /// </summary>
    public enum ControlEventType
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
    /// Represents a control event for managing live stream state
    /// </summary>
    public class ControlEvent : BaseEvent
    {
        /// <summary>
        /// Type of control event
        /// </summary>
        public ControlEventType ControlType { get; set; }
        
        /// <summary>
        /// Value or additional information related to the control event
        /// </summary>
        public string Value { get; set; }
    }
} 