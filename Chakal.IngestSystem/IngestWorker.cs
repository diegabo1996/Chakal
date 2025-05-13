using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Chakal.Core.Interfaces;
using Chakal.Infrastructure.Persistence;
using Chakal.Application.Services;
using Prometheus;

namespace Chakal.IngestSystem
{
    /// <summary>
    /// Background service that manages the ingest pipeline
    /// </summary>
    public class IngestWorker : BackgroundService
    {
        private readonly ILogger<IngestWorker> _logger;
        private readonly IEventSource _eventSource;
        private readonly IEventProcessor _eventProcessor;
        private readonly IEventChannel _persistChannel;
        private readonly IEventChannel _broadcastChannel;
        
        // Prometheus metrics
        private readonly Counter _eventsIngestedCounter;
        private readonly Counter _eventsPersistedCounter;
        private readonly Counter _eventsBroadcastDroppedCounter;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="IngestWorker"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        /// <param name="eventSource">The event source</param>
        /// <param name="eventProcessor">The event processor</param>
        /// <param name="channelFactory">The channel factory to get named channels</param>
        /// <param name="configuration">Configuration</param>
        public IngestWorker(
            ILogger<IngestWorker> logger,
            IEventSource eventSource,
            IEventProcessor eventProcessor,
            EventChannelFactory channelFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _eventSource = eventSource;
            _eventProcessor = eventProcessor;
            
            // Get the named channels
            _persistChannel = channelFactory.CreateOrGet("PersistChannel");
            _broadcastChannel = channelFactory.CreateOrGet("BroadcastChannel");
            
            // Initialize metrics
            _eventsIngestedCounter = Metrics.CreateCounter(
                "chakal_events_ingested_total", 
                "Total number of events ingested",
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_type" }
                });
            
            _eventsPersistedCounter = Metrics.CreateCounter(
                "chakal_events_persisted_total", 
                "Total number of events persisted to ClickHouse",
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_type" }
                });
            
            _eventsBroadcastDroppedCounter = Metrics.CreateCounter(
                "chakal_events_broadcast_dropped_total", 
                "Total number of events dropped from broadcast channel due to backpressure",
                new CounterConfiguration
                {
                    LabelNames = new[] { "event_type" }
                });
            
            _logger.LogInformation("IngestWorker initialized");
        }
        
        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("IngestWorker starting");
            
            try
            {
                await _eventSource.StartAsync(ProcessEventAsync, stoppingToken);
                _logger.LogInformation("Event source started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting event source");
                throw;
            }
        }
        
        /// <summary>
        /// Processes events from the event source
        /// </summary>
        /// <param name="event">The event to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task ProcessEventAsync(object @event, CancellationToken cancellationToken)
        {
            try
            {
                // Track ingested event
                IncrementIngestedCounter(@event);
                
                // First, ensure the event is persisted (reliable delivery)
                await SendToPersistChannel(@event, cancellationToken);
                
                // Then broadcast (best-effort, drop if full)
                SendToBroadcastChannel(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event of type {EventType}", @event.GetType().Name);
            }
        }
        
        private async Task SendToPersistChannel(object @event, CancellationToken cancellationToken)
        {
            string eventType = "unknown";
            
            try
            {
                switch (@event)
                {
                    case Core.Models.Events.ChatEvent chatEvent:
                        await _persistChannel.WriteAsync(chatEvent, cancellationToken);
                        eventType = "chat";
                        break;
                        
                    case Core.Models.Events.GiftEvent giftEvent:
                        await _persistChannel.WriteAsync(giftEvent, cancellationToken);
                        eventType = "gift";
                        break;
                        
                    case Core.Models.Events.SocialEvent socialEvent:
                        await _persistChannel.WriteAsync(socialEvent, cancellationToken);
                        eventType = "social";
                        break;
                        
                    case Core.Models.Events.SubscriptionEvent subscriptionEvent:
                        await _persistChannel.WriteAsync(subscriptionEvent, cancellationToken);
                        eventType = "subscription";
                        break;
                        
                    case Core.Models.Events.ControlEvent controlEvent:
                        await _persistChannel.WriteAsync(controlEvent, cancellationToken);
                        eventType = "control";
                        break;
                        
                    case Core.Models.Events.RoomStatsEvent roomStatsEvent:
                        await _persistChannel.WriteAsync(roomStatsEvent, cancellationToken);
                        eventType = "room_stats";
                        break;
                        
                    default:
                        _logger.LogWarning("Unknown event type: {EventType}", @event.GetType().Name);
                        return;
                }
                
                // Track persisted event
                _eventsPersistedCounter.WithLabels(eventType).Inc();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending {EventType} event to persist channel", eventType);
                throw;
            }
        }
        
        private void SendToBroadcastChannel(object @event)
        {
            // Use TryWrite for best-effort delivery (drop if channel is full)
            var success = false;
            string eventType = "unknown";
            
            try
            {
                switch (@event)
                {
                    case Core.Models.Events.ChatEvent chatEvent:
                        success = _broadcastChannel.TryWrite(chatEvent);
                        eventType = "chat";
                        break;
                        
                    case Core.Models.Events.GiftEvent giftEvent:
                        success = _broadcastChannel.TryWrite(giftEvent);
                        eventType = "gift";
                        break;
                        
                    case Core.Models.Events.SocialEvent socialEvent:
                        success = _broadcastChannel.TryWrite(socialEvent);
                        eventType = "social";
                        break;
                        
                    case Core.Models.Events.SubscriptionEvent subscriptionEvent:
                        success = _broadcastChannel.TryWrite(subscriptionEvent);
                        eventType = "subscription";
                        break;
                        
                    case Core.Models.Events.ControlEvent controlEvent:
                        success = _broadcastChannel.TryWrite(controlEvent);
                        eventType = "control";
                        break;
                        
                    case Core.Models.Events.RoomStatsEvent roomStatsEvent:
                        success = _broadcastChannel.TryWrite(roomStatsEvent);
                        eventType = "room_stats";
                        break;
                        
                    default:
                        // Already logged in the main process method
                        return;
                }
                
                if (!success)
                {
                    // Count metric for dropped events
                    _logger.LogDebug("Dropped event from broadcast channel due to backpressure: {EventType}", eventType);
                    _eventsBroadcastDroppedCounter.WithLabels(eventType).Inc();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting {EventType} event", eventType);
            }
        }
        
        private void IncrementIngestedCounter(object @event)
        {
            string eventType = @event switch
            {
                Core.Models.Events.ChatEvent => "chat",
                Core.Models.Events.GiftEvent => "gift",
                Core.Models.Events.SocialEvent => "social",
                Core.Models.Events.SubscriptionEvent => "subscription",
                Core.Models.Events.ControlEvent => "control",
                Core.Models.Events.RoomStatsEvent => "room_stats",
                _ => "unknown"
            };
            
            _eventsIngestedCounter.WithLabels(eventType).Inc();
        }
        
        /// <inheritdoc/>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("IngestWorker stopping");
            
            try
            {
                await _eventSource.StopAsync(cancellationToken);
                _logger.LogInformation("Event source stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping event source");
            }
            
            await base.StopAsync(cancellationToken);
        }
    }
} 