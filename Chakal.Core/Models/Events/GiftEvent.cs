using System;

namespace Chakal.Core.Models.Events
{
    /// <summary>
    /// Represents a gift sending event
    /// </summary>
    public class GiftEvent : BaseEvent
    {
        /// <summary>
        /// User ID of the gift sender
        /// </summary>
        public ulong UserId { get; set; }
        
        /// <summary>
        /// Username/nickname of the gift sender
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Gift identifier
        /// </summary>
        public uint GiftId { get; set; }
        
        /// <summary>
        /// Name of the gift
        /// </summary>
        public string GiftName { get; set; }
        
        /// <summary>
        /// Diamond value of the gift
        /// </summary>
        public uint DiamondCount { get; set; }
        
        /// <summary>
        /// Combo identifier for multi-gifts
        /// </summary>
        public ulong ComboId { get; set; }
        
        /// <summary>
        /// Total count in the streak
        /// </summary>
        public uint StreakTotal { get; set; }
        
        /// <summary>
        /// Flag indicating if this is the end of a streak
        /// </summary>
        public bool RepeatEnd { get; set; }
    }
} 