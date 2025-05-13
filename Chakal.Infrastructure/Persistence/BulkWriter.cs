using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Linq;
using Microsoft.Extensions.Logging;
using Chakal.Core.Interfaces;
using Chakal.Core.Models.Events;

namespace Chakal.Infrastructure.Persistence
{
    /// <summary>
    /// Options for configuring the bulk writer
    /// </summary>
    public class BulkWriterOptions
    {
        /// <summary>
        /// Maximum number of items to accumulate before committing
        /// </summary>
        public int MaxBatchSize { get; set; } = 5000;
        
        /// <summary>
        /// Maximum time (in milliseconds) to wait before committing, regardless of batch size
        /// </summary>
        public int MaxWaitTimeMs { get; set; } = 500;
    }
    
    /// <summary>
    /// Bulk writer for batching events to be persisted
    /// </summary>
    /// <remarks>
    /// This class is obsolete. Use <see cref="Chakal.IngestSystem.BulkWriterWorker"/> instead.
    /// </remarks>
    [Obsolete("This class is obsolete. Use BulkWriterWorker instead.")]
    public class BulkWriter : IDisposable, IAsyncDisposable
    {
        private readonly ILogger<BulkWriter> _logger;
        private readonly IEventPersistence _persistence;
        private readonly BulkWriterOptions _options;
        private readonly ChannelReader<object> _channelReader;
        private readonly CancellationTokenSource _cts;
        private readonly Task _processTask;
        private bool _disposed;
        
        // Buffers for batching events by type
        private readonly List<ChatEvent> _chatEvents = new();
        private readonly List<GiftEvent> _giftEvents = new();
        private readonly List<SocialEvent> _socialEvents = new();
        private readonly List<SubscriptionEvent> _subscriptionEvents = new();
        private readonly List<ControlEvent> _controlEvents = new();
        private readonly List<RoomStatsEvent> _roomStatsEvents = new();
        
        /// <summary>
        /// Initializes a new instance of <see cref="BulkWriter"/>
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="persistence">Event persistence service</param>
        /// <param name="channelReader">Channel reader for events</param>
        /// <param name="options">Configuration options</param>
        [Obsolete("This class is obsolete. Use BulkWriterWorker instead.")]
        public BulkWriter(
            ILogger<BulkWriter> logger,
            IEventPersistence persistence,
            ChannelReader<object> channelReader,
            BulkWriterOptions? options = null)
        {
            _logger = logger;
            _persistence = persistence;
            _channelReader = channelReader;
            _options = options ?? new BulkWriterOptions();
            _cts = new CancellationTokenSource();
            
            // Start processing task
            _processTask = Task.Run(ProcessEventsAsync);
            
            _logger.LogInformation(
                "Bulk writer started with batch size {BatchSize} and max wait time {MaxWaitTimeMs}ms", 
                _options.MaxBatchSize, 
                _options.MaxWaitTimeMs);
        }
        
        private async Task ProcessEventsAsync()
        {
            _logger.LogDebug("Bulk writer processing task started");
            
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.MaxWaitTimeMs));
            
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var dequeueTask = _channelReader.ReadAsync(_cts.Token).AsTask();
                    var timerTask = periodicTimer.WaitForNextTickAsync(_cts.Token).AsTask();
                    
                    // Wait for either an event or the timer
                    var completedTask = await Task.WhenAny(dequeueTask, timerTask);
                    
                    if (completedTask == dequeueTask)
                    {
                        // Event received
                        var @event = await dequeueTask;
                        await ProcessEventAsync(@event);
                        
                        // Check if we need to flush
                        if (GetTotalBufferedEvents() >= _options.MaxBatchSize)
                        {
                            await FlushEventsAsync();
                        }
                    }
                    else if (completedTask == timerTask)
                    {
                        // Timer elapsed, flush if we have any events
                        if (GetTotalBufferedEvents() > 0)
                        {
                            await FlushEventsAsync();
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
            {
                // Normal cancellation
                _logger.LogDebug("Bulk writer task cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk writer processing task");
            }
            finally
            {
                // Make sure we flush any remaining events
                try 
                {
                    await FlushEventsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error flushing events during shutdown");
                }
            }
        }
        
        private async Task ProcessEventAsync(object @event)
        {
            switch (@event)
            {
                case ChatEvent chatEvent:
                    _chatEvents.Add(chatEvent);
                    
                    // Persist user info with each chat event
                    await _persistence.PersistUserInfoAsync(
                        chatEvent.UserId,
                        $"user_{chatEvent.UserId}", // Placeholder for unique ID
                        chatEvent.Username,
                        "unknown", // Placeholder for region
                        0, // Placeholder for follower count 
                        _cts.Token);
                    break;
                    
                case GiftEvent giftEvent:
                    _giftEvents.Add(giftEvent);
                    
                    // Persist user and gift info with each gift event
                    await _persistence.PersistUserInfoAsync(
                        giftEvent.UserId,
                        $"user_{giftEvent.UserId}", // Placeholder for unique ID
                        giftEvent.Username,
                        "unknown", // Placeholder for region
                        0, // Placeholder for follower count
                        _cts.Token);
                        
                    await _persistence.PersistGiftInfoAsync(
                        giftEvent.GiftId,
                        giftEvent.GiftName ?? "Unknown",
                        0, // Placeholder for coin cost
                        giftEvent.DiamondCount,
                        false, // Placeholder for is exclusive
                        false, // Placeholder for is on panel
                        _cts.Token);
                    break;
                    
                case SocialEvent socialEvent:
                    _socialEvents.Add(socialEvent);
                    
                    // Persist user info with each social event
                    await _persistence.PersistUserInfoAsync(
                        socialEvent.UserId,
                        $"user_{socialEvent.UserId}", // Placeholder for unique ID
                        socialEvent.Username,
                        "unknown", // Placeholder for region
                        0, // Placeholder for follower count
                        _cts.Token);
                    break;
                    
                case SubscriptionEvent subscriptionEvent:
                    _subscriptionEvents.Add(subscriptionEvent);
                    
                    // Persist user info with each subscription event
                    await _persistence.PersistUserInfoAsync(
                        subscriptionEvent.UserId,
                        $"user_{subscriptionEvent.UserId}", // Placeholder for unique ID
                        subscriptionEvent.Username,
                        "unknown", // Placeholder for region
                        0, // Placeholder for follower count
                        _cts.Token);
                    break;
                    
                case ControlEvent controlEvent:
                    _controlEvents.Add(controlEvent);
                    
                    // If this is a LiveStart event, persist room info
                    if (controlEvent.ControlType == ControlEventType.LiveStart)
                    {
                        await _persistence.PersistRoomInfoAsync(
                            controlEvent.RoomId,
                            0, // We don't have host user id in the event
                            controlEvent.Value, // Assuming Value contains the title
                            "en", // Default language
                            controlEvent.EventTime,
                            _cts.Token);
                    }
                    // If this is a LiveEnd event, update room end time
                    else if (controlEvent.ControlType == ControlEventType.LiveEnd)
                    {
                        await _persistence.UpdateRoomEndTimeAsync(
                            controlEvent.RoomId,
                            controlEvent.EventTime,
                            _cts.Token);
                    }
                    break;
                    
                case RoomStatsEvent roomStatsEvent:
                    _roomStatsEvents.Add(roomStatsEvent);
                    break;
                    
                default:
                    _logger.LogWarning("Received unknown event type: {EventType}", @event.GetType().Name);
                    break;
            }
        }
        
        private int GetTotalBufferedEvents()
        {
            return _chatEvents.Count +
                   _giftEvents.Count +
                   _socialEvents.Count +
                   _subscriptionEvents.Count +
                   _controlEvents.Count +
                   _roomStatsEvents.Count;
        }
        
        private async Task FlushEventsAsync()
        {
            var tasks = new List<Task>();
            
            try
            {
                if (_chatEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} chat events", _chatEvents.Count);
                    tasks.Add(_persistence.PersistChatEventsAsync(
                        new List<ChatEvent>(_chatEvents), 
                        _cts.Token));
                    _chatEvents.Clear();
                }
                
                if (_giftEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} gift events", _giftEvents.Count);
                    tasks.Add(_persistence.PersistGiftEventsAsync(
                        new List<GiftEvent>(_giftEvents), 
                        _cts.Token));
                    _giftEvents.Clear();
                }
                
                if (_socialEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} social events", _socialEvents.Count);
                    tasks.Add(_persistence.PersistSocialEventsAsync(
                        new List<SocialEvent>(_socialEvents), 
                        _cts.Token));
                    _socialEvents.Clear();
                }
                
                if (_subscriptionEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} subscription events", _subscriptionEvents.Count);
                    tasks.Add(_persistence.PersistSubscriptionEventsAsync(
                        new List<SubscriptionEvent>(_subscriptionEvents), 
                        _cts.Token));
                    _subscriptionEvents.Clear();
                }
                
                if (_controlEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} control events", _controlEvents.Count);
                    tasks.Add(_persistence.PersistControlEventsAsync(
                        new List<ControlEvent>(_controlEvents), 
                        _cts.Token));
                    _controlEvents.Clear();
                }
                
                if (_roomStatsEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} room stats events", _roomStatsEvents.Count);
                    tasks.Add(_persistence.PersistRoomStatsEventsAsync(
                        new List<RoomStatsEvent>(_roomStatsEvents), 
                        _cts.Token));
                    _roomStatsEvents.Clear();
                }
                
                if (tasks.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} batches of events", tasks.Count);
                    await Task.WhenAll(tasks);
                    _logger.LogDebug("Flushed all batches successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing events");
                throw;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            
            _cts.Cancel();
            
            try
            {
                // Give the task some time to complete gracefully
                if (!_processTask.IsCompleted)
                {
                    _processTask.Wait(TimeSpan.FromSeconds(5));
                }
            }
            catch (AggregateException)
            {
                // Expected due to cancellation
            }
            
            _cts.Dispose();
            _disposed = true;
            
            _logger.LogInformation("Bulk writer disposed");
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            
            _cts.Cancel();
            
            try
            {
                // Give the task some time to complete gracefully
                if (!_processTask.IsCompleted)
                {
                    await _processTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
            }
            catch (OperationCanceledException)
            {
                // Expected due to cancellation
            }
            
            _cts.Dispose();
            _disposed = true;
            
            _logger.LogInformation("Bulk writer disposed asynchronously");
        }
    }
} 