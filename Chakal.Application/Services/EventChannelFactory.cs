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
        
        /// <summary>
        /// Creates a new instance of <see cref="EventChannelFactory"/>
        /// </summary>
        /// <param name="loggerFactory">The logger factory</param>
        public EventChannelFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
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