using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Chakal.Core.Interfaces;
using Chakal.Core.Models.Events;

namespace Chakal.Application.Services
{
    /// <summary>
    /// Options for configuring the event channel
    /// </summary>
    public class EventChannelOptions
    {
        /// <summary>
        /// Channel capacity (if bounded)
        /// </summary>
        public int Capacity { get; set; } = 20000;
        
        /// <summary>
        /// Whether the channel is bounded or unbounded
        /// </summary>
        public bool IsBounded { get; set; } = true;
        
        /// <summary>
        /// FullMode when channel is full (only for bounded channels)
        /// </summary>
        public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.Wait;
        
        /// <summary>
        /// Channel name for logging
        /// </summary>
        public string Name { get; set; } = "Default";
    }
    
    /// <summary>
    /// A generic implementation of <see cref="IEventChannel"/> using System.Threading.Channels
    /// </summary>
    public class EventChannel : IEventChannel
    {
        private readonly ILogger<EventChannel> _logger;
        private readonly Channel<object> _channel;
        private readonly EventChannelOptions _options;
        
        /// <summary>
        /// Creates a new instance of <see cref="EventChannel"/>
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="options">Channel configuration options</param>
        public EventChannel(ILogger<EventChannel> logger, EventChannelOptions options)
        {
            _logger = logger;
            _options = options;
            
            if (options.IsBounded)
            {
                var boundedOptions = new BoundedChannelOptions(options.Capacity)
                {
                    FullMode = options.FullMode,
                    SingleReader = false,
                    SingleWriter = false
                };
                _channel = Channel.CreateBounded<object>(boundedOptions);
                _logger.LogInformation("Created bounded {Name} channel with capacity {Capacity}, mode {Mode}", 
                    options.Name, options.Capacity, options.FullMode);
            }
            else
            {
                var unboundedOptions = new UnboundedChannelOptions
                {
                    SingleReader = false,
                    SingleWriter = false
                };
                _channel = Channel.CreateUnbounded<object>(unboundedOptions);
                _logger.LogInformation("Created unbounded {Name} channel", options.Name);
            }
        }

        /// <summary>
        /// Gets the channel reader
        /// </summary>
        public ChannelReader<object> Reader => _channel.Reader;
        
        /// <summary>
        /// Gets the channel writer
        /// </summary>
        public ChannelWriter<object> Writer => _channel.Writer;

        /// <inheritdoc />
        public ValueTask WriteAsync(ChatEvent chatEvent, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(chatEvent, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(GiftEvent giftEvent, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(giftEvent, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(SocialEvent socialEvent, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(socialEvent, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(SubscriptionEvent subscriptionEvent, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(subscriptionEvent, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(ControlEvent controlEvent, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(controlEvent, cancellationToken);
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(RoomStatsEvent roomStatsEvent, CancellationToken cancellationToken = default)
        {
            return _channel.Writer.WriteAsync(roomStatsEvent, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _channel.Writer.Complete();
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            _channel.Writer.Complete();
            return ValueTask.CompletedTask;
        }
    }
} 