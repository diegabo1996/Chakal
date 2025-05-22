using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Chakal.Core.Interfaces;
using Chakal.Core.Models.Events;
using Chakal.Application.Services;
using Prometheus;

namespace Chakal.IngestSystem
{
    /// <summary>
    /// Background service that handles bulk writing of events to persistent storage
    /// </summary>
    public class BulkWriterWorker : BackgroundService
    {
        private readonly ILogger<BulkWriterWorker> _logger;
        private readonly IEventPersistence _persistence;
        private readonly IEventChannel _persistChannel;

        // Configuration options
        private readonly int _maxBatchSize;
        private readonly int _maxWaitTimeMs;

        // Buffers for batching events by type
        private readonly List<ChatEvent> _chatEvents = new();
        private readonly List<GiftEvent> _giftEvents = new();
        private readonly List<SocialEvent> _socialEvents = new();
        private readonly List<SubscriptionEvent> _subscriptionEvents = new();
        private readonly List<ControlEvent> _controlEvents = new();
        private readonly List<RoomStatsEvent> _roomStatsEvents = new();

        // Prometheus metrics
        private readonly Counter _eventsBatchedCounter;
        private readonly Counter _batchesPersistedCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkWriterWorker"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="persistence">The event persistence service</param>
        /// <param name="channelFactory">The channel factory</param>
        /// <param name="configuration">Configuration</param>
        public BulkWriterWorker(
            ILogger<BulkWriterWorker> logger,
            IEventPersistence persistence,
            EventChannelFactory channelFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _persistence = persistence;

            // Get the persist channel
            _persistChannel = channelFactory.CreateOrGet("PersistChannel");

            // Get configuration options
            if (!int.TryParse(configuration["BULK_BATCH_SIZE"], out _maxBatchSize))
            {
                _maxBatchSize = 3; // Default batch size
            }

            if (!int.TryParse(configuration["BULK_WAIT_MS"], out _maxWaitTimeMs))
            {
                _maxWaitTimeMs = 1500; // Default wait time in milliseconds
            }

            // Initialize metrics
            _eventsBatchedCounter = Metrics.CreateCounter(
                "chakal_events_batched_total",
                "Total number of events batched for persistence",
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_type" }
                });

            _batchesPersistedCounter = Metrics.CreateCounter(
                "chakal_batches_persisted_total",
                "Total number of batches persisted to storage",
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_type" }
                });

            _logger.LogInformation("BulkWriterWorker initialized with batch size {BatchSize} and max wait time {MaxWaitTimeMs}ms",
                _maxBatchSize, _maxWaitTimeMs);
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BulkWriterWorker starting");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var readTask = _persistChannel.Reader.ReadAsync(stoppingToken).AsTask();
                    var @event = await readTask;
                    await ProcessEventAsync(@event, stoppingToken);
                    if (GetTotalBufferedEvents() >= _maxBatchSize)
                    {
                        // If we have reached the max batch size, flush immediately
                        await FlushEventsAsync(stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("BulkWriterWorker stopping (cancellation).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in BulkWriterWorker loop");
            }
            finally
            {
                await SafeFlushDuringShutdownAsync();
            }
        }

        private async Task SafeFlushDuringShutdownAsync()
        {
            if (GetTotalBufferedEvents() == 0) return;

            try
            {
                _logger.LogInformation("Flushing buffered events during shutdown…");
                await FlushEventsAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing events during shutdown");
            }
        }


        /// <summary>
        /// Process a single event and add it to the appropriate batch
        /// </summary>
        /// <param name="event">The event to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task ProcessEventAsync(object @event, CancellationToken cancellationToken)
        {
            switch (@event)
            {
                case ChatEvent chatEvent:
                    _chatEvents.Add(chatEvent);
                    _eventsBatchedCounter.WithLabels("chat").Inc();

                    // Persist user info with each chat event
                    await _persistence.PersistUserInfoAsync(
                        chatEvent.UserId,
                        $"user_{chatEvent.UserId}", // Used as unique ID
                        chatEvent.Username,
                        "unknown", // Region isn't available from TikTok event source
                        0, // Follower count isn't available
                        cancellationToken);
                    break;

                case GiftEvent giftEvent:
                    _giftEvents.Add(giftEvent);
                    _eventsBatchedCounter.WithLabels("gift").Inc();

                    // Persist user and gift info with each gift event
                    await _persistence.PersistUserInfoAsync(
                        giftEvent.UserId,
                        $"user_{giftEvent.UserId}", // Used as unique ID
                        giftEvent.Username,
                        "unknown", // Region isn't available
                        0, // Follower count isn't available
                        cancellationToken);

                    // Pass the gift ID directly as the gift name isn't available from TikTok
                    await _persistence.PersistGiftInfoAsync(
                        giftEvent.GiftId,
                        $"gift_{giftEvent.GiftId}", // Gift name isn't provided, use ID as placeholder
                        giftEvent.DiamondCount, // Use diamond count as coin cost
                        giftEvent.DiamondCount,
                        false, // Is exclusive flag isn't available
                        false, // Is on panel flag isn't available
                        cancellationToken);
                    break;

                case SocialEvent socialEvent:
                    _socialEvents.Add(socialEvent);
                    _eventsBatchedCounter.WithLabels("social").Inc();

                    // Persist user info with each social event
                    await _persistence.PersistUserInfoAsync(
                        socialEvent.UserId,
                        $"user_{socialEvent.UserId}", // Used as unique ID
                        socialEvent.Username,
                        "unknown", // Region isn't available
                        0, // Follower count isn't available
                        cancellationToken);
                    break;

                case SubscriptionEvent subscriptionEvent:
                    _subscriptionEvents.Add(subscriptionEvent);
                    _eventsBatchedCounter.WithLabels("subscription").Inc();

                    // Persist user info with each subscription event
                    await _persistence.PersistUserInfoAsync(
                        subscriptionEvent.UserId,
                        $"user_{subscriptionEvent.UserId}", // Used as unique ID
                        subscriptionEvent.Username,
                        "unknown", // Region isn't available
                        0, // Follower count isn't available
                        cancellationToken);
                    break;

                case ControlEvent controlEvent:
                    _controlEvents.Add(controlEvent);
                    _eventsBatchedCounter.WithLabels("control").Inc();

                    // If this is a LiveStart event, persist room info
                    if (controlEvent.ControlType == ControlEventType.LiveStart)
                    {
                        await _persistence.PersistRoomInfoAsync(
                            controlEvent.RoomId,
                            0, // Host user ID isn't available in the control event
                            controlEvent.Value, // Use the value field as the title
                            "en", // Default language
                            controlEvent.EventTime,
                            cancellationToken);
                    }
                    // If this is a LiveEnd event, update room end time
                    else if (controlEvent.ControlType == ControlEventType.LiveEnd)
                    {
                        await _persistence.UpdateRoomEndTimeAsync(
                            controlEvent.RoomId,
                            controlEvent.EventTime,
                            cancellationToken);
                    }
                    break;

                case RoomStatsEvent roomStatsEvent:
                    _roomStatsEvents.Add(roomStatsEvent);
                    _eventsBatchedCounter.WithLabels("room_stats").Inc();
                    break;

                default:
                    _logger.LogWarning("Received unknown event type: {EventType}", @event.GetType().Name);
                    break;
            }
        }

        /// <summary>
        /// Gets the total number of events currently buffered
        /// </summary>
        /// <returns>Total number of buffered events</returns>
        private int GetTotalBufferedEvents()
        {
            return _chatEvents.Count +
                   _giftEvents.Count +
                   _socialEvents.Count +
                   _subscriptionEvents.Count +
                   _controlEvents.Count +
                   _roomStatsEvents.Count;
        }

        /// <summary>
        /// Flush all buffered events to persistent storage
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task FlushEventsAsync(CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            try
            {
                if (_chatEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} chat events", _chatEvents.Count);
                    tasks.Add(_persistence.PersistChatEventsAsync(
                        new List<ChatEvent>(_chatEvents),
                        cancellationToken));
                    _batchesPersistedCounter.WithLabels("chat").Inc();
                    _chatEvents.Clear();
                }

                if (_giftEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} gift events", _giftEvents.Count);
                    tasks.Add(_persistence.PersistGiftEventsAsync(
                        new List<GiftEvent>(_giftEvents),
                        cancellationToken));
                    _batchesPersistedCounter.WithLabels("gift").Inc();
                    _giftEvents.Clear();
                }

                if (_socialEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} social events", _socialEvents.Count);
                    tasks.Add(_persistence.PersistSocialEventsAsync(
                        new List<SocialEvent>(_socialEvents),
                        cancellationToken));
                    _batchesPersistedCounter.WithLabels("social").Inc();
                    _socialEvents.Clear();
                }

                if (_subscriptionEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} subscription events", _subscriptionEvents.Count);
                    tasks.Add(_persistence.PersistSubscriptionEventsAsync(
                        new List<SubscriptionEvent>(_subscriptionEvents),
                        cancellationToken));
                    _batchesPersistedCounter.WithLabels("subscription").Inc();
                    _subscriptionEvents.Clear();
                }

                if (_controlEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} control events", _controlEvents.Count);
                    tasks.Add(_persistence.PersistControlEventsAsync(
                        new List<ControlEvent>(_controlEvents),
                        cancellationToken));
                    _batchesPersistedCounter.WithLabels("control").Inc();
                    _controlEvents.Clear();
                }

                if (_roomStatsEvents.Count > 0)
                {
                    _logger.LogDebug("Flushing {Count} room stats events", _roomStatsEvents.Count);
                    tasks.Add(_persistence.PersistRoomStatsEventsAsync(
                        new List<RoomStatsEvent>(_roomStatsEvents),
                        cancellationToken));
                    _batchesPersistedCounter.WithLabels("room_stats").Inc();
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
                // Don't rethrow - keep worker running even if persistence fails
            }
        }

        /// <inheritdoc/>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("BulkWriterWorker stopping");

            // Flush any remaining events
            try
            {
                await FlushEventsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing events during shutdown");
            }

            await base.StopAsync(cancellationToken);
        }
    }
}