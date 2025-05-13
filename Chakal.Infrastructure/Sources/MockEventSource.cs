using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Chakal.Core.Interfaces;
using Chakal.Core.Models.Events;

namespace Chakal.Infrastructure.Sources
{
    /// <summary>
    /// A mock implementation of <see cref="IEventSource"/> for testing and debugging
    /// </summary>
    public class MockEventSource : IEventSource
    {
        private readonly ILogger<MockEventSource> _logger;
        private readonly IEventProcessor _eventProcessor;
        private readonly string _hostName;
        private readonly Random _random = new();
        private readonly int _intervalMs;
        private readonly CancellationTokenSource _cts = new();
        private Task? _runningTask;
        private bool _disposed;
        private bool _isConnected;
        private EventProcessingDelegate? _customProcessor;
        
        /// <summary>
        /// Gets a value indicating whether the source is currently connected
        /// </summary>
        public bool IsConnected => _isConnected;
        
        /// <summary>
        /// Gets the current room ID
        /// </summary>
        public ulong RoomId { get; private set; }
        
        /// <summary>
        /// Gets the host username
        /// </summary>
        public string RoomHost => _hostName;
        
        /// <summary>
        /// Initializes a new instance of <see cref="MockEventSource"/>
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger</param>
        /// <param name="eventProcessor">Event processor for handling events</param>
        /// <exception cref="ArgumentException">Thrown when required configuration is missing</exception>
        public MockEventSource(
            IConfiguration configuration,
            ILogger<MockEventSource> logger,
            IEventProcessor eventProcessor)
        {
            _logger = logger;
            _eventProcessor = eventProcessor;
            
            _hostName = configuration["TIKTOK_HOST"] ?? "mockhost";
            _intervalMs = 3000; // Generate events every 3 seconds
            
            RoomId = (ulong)_random.Next(100000, 999999);
            
            _logger.LogInformation("Initialized mock event source for @{HostName} with Room ID {RoomId}", _hostName, RoomId);
        }
        
        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return StartAsync(null, cancellationToken);
        }
        
        /// <inheritdoc />
        public Task StartAsync(EventProcessingDelegate? processor, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting mock event source for @{HostName}", _hostName);
            
            if (_isConnected)
            {
                _logger.LogWarning("Mock event source is already running");
                return Task.CompletedTask;
            }
            
            _isConnected = true;
            _customProcessor = processor;
            _runningTask = Task.Run(() => RunAsync(cancellationToken));
            
            // Send control event for room start
            var startEvent = new ControlEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                ControlType = ControlEventType.LiveStart,
                Value = _hostName
            };
            
            // Use custom processor if provided, otherwise use event processor
            if (_customProcessor != null)
            {
                _ = _customProcessor(startEvent, cancellationToken);
            }
            else
            {
                _ = _eventProcessor.ProcessControlEventAsync(startEvent, cancellationToken);
            }
            
            _logger.LogInformation("Mock event source started for @{HostName}", _hostName);
            
            return Task.CompletedTask;
        }
        
        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Mock event source is not running");
                return;
            }
            
            _logger.LogInformation("Stopping mock event source");
            
            // Send control event for room end
            var endEvent = new ControlEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                ControlType = ControlEventType.LiveEnd,
                Value = _hostName
            };
            
            // Use custom processor if provided, otherwise use event processor
            if (_customProcessor != null)
            {
                await _customProcessor(endEvent, cancellationToken);
            }
            else
            {
                await _eventProcessor.ProcessControlEventAsync(endEvent, cancellationToken);
            }
            
            _cts.Cancel();
            _isConnected = false;
            
            if (_runningTask != null)
            {
                try
                {
                    await _runningTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error waiting for mock event source task to complete");
                }
            }
            
            _logger.LogInformation("Mock event source stopped");
        }
        
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Mock event generation task started");
            
            try
            {
                while (!_cts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    // Generate a random event
                    await GenerateRandomEventAsync(cancellationToken);
                    
                    // Wait for the next event
                    await Task.Delay(_intervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (_cts.IsCancellationRequested || cancellationToken.IsCancellationRequested)
            {
                // Normal cancellation
                _logger.LogDebug("Mock event generation task cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in mock event generation task");
            }
            
            _logger.LogInformation("Mock event generation task stopped");
        }
        
        private async Task GenerateRandomEventAsync(CancellationToken cancellationToken)
        {
            // Pick a random event type
            var eventType = _random.Next(0, 6);
            
            try
            {
                switch (eventType)
                {
                    case 0:
                        await GenerateChatEventAsync(cancellationToken);
                        break;
                        
                    case 1:
                        await GenerateGiftEventAsync(cancellationToken);
                        break;
                        
                    case 2:
                        await GenerateSocialEventAsync(cancellationToken);
                        break;
                        
                    case 3:
                        await GenerateSubscriptionEventAsync(cancellationToken);
                        break;
                        
                    case 4:
                        await GenerateRoomStatsEventAsync(cancellationToken);
                        break;
                        
                    case 5:
                        // Control events are only generated at start/stop
                        await GenerateChatEventAsync(cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating mock event");
            }
        }
        
        private async Task GenerateChatEventAsync(CancellationToken cancellationToken)
        {
            var chatEvent = new ChatEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                MessageId = (ulong)_random.Next(100000, 999999),
                UserId = (ulong)_random.Next(10000, 99999),
                Username = $"MockUser{_random.Next(1, 100)}",
                Text = $"Mock chat message {_random.Next(1000, 9999)}",
                DeviceType = _random.Next(0, 2) == 0 ? "Android" : "iOS"
            };
            
            if (_random.Next(0, 10) < 3) // 30% chance to have emotes
            {
                chatEvent.Emotes = new Dictionary<string, uint>
                {
                    { "heart", (uint)_random.Next(1, 5) },
                    { "fire", (uint)_random.Next(1, 3) }
                };
            }
            
            if (_random.Next(0, 10) < 2) // 20% chance to be a reply
            {
                chatEvent.ReplyToId = (ulong)_random.Next(100000, 999999);
            }
            
            _logger.LogDebug("Generated chat event: {Username}: {Message}", chatEvent.Username, chatEvent.Text);
            
            // Use custom processor if provided, otherwise use event processor
            if (_customProcessor != null)
            {
                await _customProcessor(chatEvent, cancellationToken);
            }
            else
            {
                await _eventProcessor.ProcessChatEventAsync(chatEvent, cancellationToken);
            }
        }
        
        private async Task GenerateGiftEventAsync(CancellationToken cancellationToken)
        {
            string[] giftNames = { "Rose", "Ice Cream", "Diamond", "Crown", "Star" };
            uint[] diamondValues = { 1, 5, 10, 50, 100 };
            
            var index = _random.Next(0, giftNames.Length);
            
            var giftEvent = new GiftEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                UserId = (ulong)_random.Next(10000, 99999),
                Username = $"MockUser{_random.Next(1, 100)}",
                GiftId = (uint)(_random.Next(1000, 2000) + index),
                GiftName = giftNames[index],
                DiamondCount = diamondValues[index],
                ComboId = _random.Next(0, 10) < 3 ? (ulong)_random.Next(10000, 99999) : 0, // 30% chance for combo
                StreakTotal = _random.Next(0, 10) < 3 ? (uint)_random.Next(2, 10) : 1, // 30% chance for streak
                RepeatEnd = _random.Next(0, 10) < 8 // 80% chance to be end of streak/repeat
            };
            
            _logger.LogDebug("Generated gift event: {Username} sent {GiftName} (worth {Diamonds} diamonds)",
                giftEvent.Username, giftEvent.GiftName, giftEvent.DiamondCount);
            
            // Use custom processor if provided, otherwise use event processor
            if (_customProcessor != null)
            {
                await _customProcessor(giftEvent, cancellationToken);
            }
            else
            {
                await _eventProcessor.ProcessGiftEventAsync(giftEvent, cancellationToken);
            }
        }
        
        private async Task GenerateSocialEventAsync(CancellationToken cancellationToken)
        {
            var socialTypeValues = Enum.GetValues<SocialInteractionType>();
            var socialType = socialTypeValues[_random.Next(0, socialTypeValues.Length)];
            
            var socialEvent = new SocialEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                UserId = (ulong)_random.Next(10000, 99999),
                Username = $"MockUser{_random.Next(1, 100)}",
                SocialType = socialType,
                Count = (uint)_random.Next(1, 5)
            };
            
            _logger.LogDebug("Generated social event: {Username} {SocialType} x{Count}",
                socialEvent.Username, socialEvent.SocialType, socialEvent.Count);
            
            // Use custom processor if provided, otherwise use event processor
            if (_customProcessor != null)
            {
                await _customProcessor(socialEvent, cancellationToken);
            }
            else
            {
                await _eventProcessor.ProcessSocialEventAsync(socialEvent, cancellationToken);
            }
        }
        
        private async Task GenerateSubscriptionEventAsync(CancellationToken cancellationToken)
        {
            var subscriptionEvent = new SubscriptionEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                UserId = (ulong)_random.Next(10000, 99999),
                Username = $"MockUser{_random.Next(1, 100)}",
                SubTier = (byte)_random.Next(1, 4),
                MonthsTotal = (ushort)_random.Next(1, 24),
                IsRenew = _random.Next(0, 10) < 7 // 70% chance to be renewal
            };
            
            _logger.LogDebug("Generated subscription event: {Username} subscribed tier {Tier} for {Months} months (Renewal: {IsRenew})",
                subscriptionEvent.Username, subscriptionEvent.SubTier, subscriptionEvent.MonthsTotal, subscriptionEvent.IsRenew);
            
            // Use custom processor if provided, otherwise use event processor
            if (_customProcessor != null)
            {
                await _customProcessor(subscriptionEvent, cancellationToken);
            }
            else
            {
                await _eventProcessor.ProcessSubscriptionEventAsync(subscriptionEvent, cancellationToken);
            }
        }
        
        private async Task GenerateRoomStatsEventAsync(CancellationToken cancellationToken)
        {
            var roomStatsEvent = new RoomStatsEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                ViewerCount = (uint)_random.Next(50, 500),
                LikeCount = (uint)_random.Next(100, 2000),
                ShareCount = (uint)_random.Next(10, 100)
            };
            
            _logger.LogDebug("Generated room stats event: {Viewers} viewers, {Likes} likes, {Shares} shares",
                roomStatsEvent.ViewerCount, roomStatsEvent.LikeCount, roomStatsEvent.ShareCount);
            
            // Use custom processor if provided, otherwise use event processor
            if (_customProcessor != null)
            {
                await _customProcessor(roomStatsEvent, cancellationToken);
            }
            else
            {
                await _eventProcessor.ProcessRoomStatsEventAsync(roomStatsEvent, cancellationToken);
            }
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            
            _cts.Cancel();
            _cts.Dispose();
            _disposed = true;
            
            _logger.LogInformation("Mock event source disposed");
        }
        
        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            
            _cts.Cancel();
            
            if (_runningTask != null)
            {
                try
                {
                    await _runningTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error waiting for mock event source task to complete");
                }
            }
            
            _cts.Dispose();
            _disposed = true;
            
            _logger.LogInformation("Mock event source disposed asynchronously");
        }
    }
} 