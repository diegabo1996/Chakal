using System;

namespace Chakal.Infrastructure.Models.Dimensions
{
    /// <summary>
    /// Gift dimension model mapped to the 'dim_gifts' table in ClickHouse
    /// </summary>
    public class DimGift
    {
        /// <summary>
        /// Gift ID (primary key)
        /// </summary>
        public uint GiftId { get; set; }
        
        /// <summary>
        /// Gift name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Cost in coins
        /// </summary>
        public uint CoinCost { get; set; }
        
        /// <summary>
        /// Cost in diamonds
        /// </summary>
        public uint DiamondCost { get; set; }
        
        /// <summary>
        /// Flag indicating if the gift is exclusive
        /// </summary>
        public byte IsExclusive { get; set; }
        
        /// <summary>
        /// Flag indicating if the gift is on the panel
        /// </summary>
        public byte IsOnPanel { get; set; }
        
        /// <summary>
        /// Date and time when the gift was inserted
        /// </summary>
        public DateTime InsertedAt { get; set; }
        
        /// <summary>
        /// Version for ReplacingMergeTree engine
        /// </summary>
        public byte Version { get; set; } = 1;
    }
} 