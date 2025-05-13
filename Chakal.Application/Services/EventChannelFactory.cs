using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Chakal.Core.Interfaces;

namespace Chakal.Application.Services
{
    /// <summary>
    /// Factory for creating event channels
    /// </summary>
    public class EventChannelFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly Dictionary<string, IEventChannel> _channels = new();
        
        /// <summary>
        /// Creates a new instance of <see cref="EventChannelFactory"/>
        /// </summary>
        /// <param name="loggerFactory">The logger factory</param>
        public EventChannelFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
        
        /// <summary>
        /// Creates or gets an existing channel by name
        /// </summary>
        /// <param name="name">Name of the channel</param>
        /// <returns>An existing or new <see cref="IEventChannel"/> instance</returns>
        /// <exception cref="ArgumentException">Thrown when the channel name is invalid</exception>
        public IEventChannel CreateOrGet(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Channel name cannot be empty", nameof(name));
            }
            
            lock (_channels)
            {
                if (_channels.TryGetValue(name, out var existingChannel))
                {
                    return existingChannel;
                }
                
                // Create appropriate channel by name
                IEventChannel newChannel;
                
                switch (name)
                {
                    case "BroadcastChannel":
                        newChannel = CreateUnboundedChannel(name);
                        break;
                    
                    case "PersistChannel":
                        newChannel = CreateBoundedChannel(name, 20000, BoundedChannelFullMode.Wait);
                        break;
                    
                    default:
                        // Default to bounded channel for unknown names
                        newChannel = CreateBoundedChannel(name);
                        break;
                }
                
                _channels[name] = newChannel;
                return newChannel;
            }
        }
        
        /// <summary>
        /// Creates a bounded event channel
        /// </summary>
        /// <param name="name">Channel name</param>
        /// <param name="capacity">Channel capacity</param>
        /// <param name="fullMode">Mode when channel is full</param>
        /// <returns>A new <see cref="IEventChannel"/> instance</returns>
        public IEventChannel CreateBoundedChannel(string name, int capacity = 20000, BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait)
        {
            var options = new EventChannelOptions
            {
                Name = name,
                Capacity = capacity,
                IsBounded = true,
                FullMode = fullMode
            };
            
            var logger = _loggerFactory.CreateLogger<EventChannel>();
            return new EventChannel(logger, options);
        }
        
        /// <summary>
        /// Creates an unbounded event channel
        /// </summary>
        /// <param name="name">Channel name</param>
        /// <returns>A new <see cref="IEventChannel"/> instance</returns>
        public IEventChannel CreateUnboundedChannel(string name)
        {
            var options = new EventChannelOptions
            {
                Name = name,
                IsBounded = false
            };
            
            var logger = _loggerFactory.CreateLogger<EventChannel>();
            return new EventChannel(logger, options);
        }
    }
} 