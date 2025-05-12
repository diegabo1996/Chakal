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
        public BulkWriter(
            ILogger<BulkWriter> logger,
            IEventPersistence persistence,
            ChannelReader<object> channelReader,
            BulkWriterOptions options = null)
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
                // Flush any remaining events before exiting
                try
                {
                    await FlushEventsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error flushing events on shutdown");
                }
                
                _logger.LogInformation("Bulk writer processing task stopped");
            }
        }
        
        private async Task ProcessEventAsync(object @event)
        {
            switch (@event)
            {
                case ChatEvent chatEvent:
                    _chatEvents.Add(chatEvent);
                    break;
                    
                case GiftEvent giftEvent:
                    _giftEvents.Add(giftEvent);
                    
                    // Also persist gift and user info immediately
                    await _persistence.PersistGiftInfoAsync(
                        giftEvent.GiftId,
                        giftEvent.GiftName,
                        0, // We don't have coin cost in the event
                        giftEvent.DiamondCount,
                        false, // We don't have exclusivity info in the event
                        false, // We don't have panel info in the event
                        _cts.Token);
                    
                    await _persistence.PersistUserInfoAsync(
                        giftEvent.UserId,
                        "", // We don't have uniqueId in the event
                        giftEvent.Username,
                        "", // We don't have region in the event
                        0, // We don't have follower count in the event
                        _cts.Token);
                    break;
                    
                case SocialEvent socialEvent:
                    _socialEvents.Add(socialEvent);
                    
                    // Also persist user info immediately
                    await _persistence.PersistUserInfoAsync(
                        socialEvent.UserId,
                        "", // We don't have uniqueId in the event
                        socialEvent.Username,
                        "", // We don't have region in the event
                        0, // We don't have follower count in the event
                        _cts.Token);
                    break;
                    
                case SubscriptionEvent subscriptionEvent:
                    _subscriptionEvents.Add(subscriptionEvent);
                    
                    // Also persist user info immediately
                    await _persistence.PersistUserInfoAsync(
                        subscriptionEvent.UserId,
                        "", // We don't have uniqueId in the event
                        subscriptionEvent.Username,
                        "", // We don't have region in the event
                        0, // We don't have follower count in the event
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
            
            if (_chatEvents.Count > 0)
            {
                tasks.Add(_persistence.PersistChatEventsAsync(
                    new List<ChatEvent>(_chatEvents), 
                    _cts.Token));
                _chatEvents.Clear();
            }
            
            if (_giftEvents.Count > 0)
            {
                tasks.Add(_persistence.PersistGiftEventsAsync(
                    new List<GiftEvent>(_giftEvents), 
                    _cts.Token));
                _giftEvents.Clear();
            }
            
            if (_socialEvents.Count > 0)
            {
                tasks.Add(_persistence.PersistSocialEventsAsync(
                    new List<SocialEvent>(_socialEvents), 
                    _cts.Token));
                _socialEvents.Clear();
            }
            
            if (_subscriptionEvents.Count > 0)
            {
                tasks.Add(_persistence.PersistSubscriptionEventsAsync(
                    new List<SubscriptionEvent>(_subscriptionEvents), 
                    _cts.Token));
                _subscriptionEvents.Clear();
            }
            
            if (_controlEvents.Count > 0)
            {
                tasks.Add(_persistence.PersistControlEventsAsync(
                    new List<ControlEvent>(_controlEvents), 
                    _cts.Token));
                _controlEvents.Clear();
            }
            
            if (_roomStatsEvents.Count > 0)
            {
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