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
            _logger.LogInformation("Starting mock event source for @{HostName}", _hostName);
            
            if (_isConnected)
            {
                _logger.LogWarning("Mock event source is already running");
                return Task.CompletedTask;
            }
            
            _isConnected = true;
            _runningTask = Task.Run(RunAsync);
            
            // Send control event for room start
            var startEvent = new ControlEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                ControlType = ControlEventType.LiveStart,
                Value = _hostName
            };
            
            _ = _eventProcessor.ProcessControlEventAsync(startEvent);
            
            _logger.LogInformation("Mock event source started for @{HostName}", _hostName);
            
            return Task.CompletedTask;
        }
        
        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Stopping mock event source for @{HostName}", _hostName);
            
            if (!_isConnected)
            {
                _logger.LogWarning("Mock event source is not running");
                return;
            }
            
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
            
            _isConnected = false;
            
            // Send control event for room end
            var endEvent = new ControlEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                ControlType = ControlEventType.LiveEnd,
                Value = _hostName
            };
            
            await _eventProcessor.ProcessControlEventAsync(endEvent);
            
            _logger.LogInformation("Mock event source stopped for @{HostName}", _hostName);
        }
        
        private async Task RunAsync()
        {
            _logger.LogDebug("Mock event generator task started");
            
            try
            {
                uint viewerCount = 100;
                uint likeCount = 0;
                uint shareCount = 0;
                ulong messageId = 1;
                
                // Send initial room stats
                await _eventProcessor.ProcessRoomStatsEventAsync(new RoomStatsEvent
                {
                    EventTime = DateTime.UtcNow,
                    RoomId = RoomId,
                    ViewerCount = viewerCount,
                    LikeCount = likeCount,
                    ShareCount = shareCount
                });
                
                while (!_cts.Token.IsCancellationRequested)
                {
                    // Wait for the next interval
                    await Task.Delay(_intervalMs, _cts.Token);
                    
                    // Update stats
                    viewerCount = (uint)Math.Max(1, viewerCount + _random.Next(-10, 20));
                    likeCount += (uint)_random.Next(0, 100);
                    shareCount += (uint)_random.Next(0, 5);
                    
                    // Send room stats update (20% chance)
                    if (_random.Next(100) < 20)
                    {
                        await _eventProcessor.ProcessRoomStatsEventAsync(new RoomStatsEvent
                        {
                            EventTime = DateTime.UtcNow,
                            RoomId = RoomId,
                            ViewerCount = viewerCount,
                            LikeCount = likeCount,
                            ShareCount = shareCount
                        });
                    }
                    
                    // Decide event type
                    int eventType = _random.Next(100);
                    
                    if (eventType < 60) // 60% chance of chat message
                    {
                        var chatEvent = new ChatEvent
                        {
                            EventTime = DateTime.UtcNow,
                            RoomId = RoomId,
                            MessageId = messageId++,
                            UserId = (ulong)_random.Next(10000, 99999),
                            Username = $"user_{_random.Next(1000, 9999)}",
                            Text = GetRandomMessage(),
                            ReplyToId = 0,
                            DeviceType = "unknown"
                        };
                        
                        await _eventProcessor.ProcessChatEventAsync(chatEvent);
                    }
                    else if (eventType < 75) // 15% chance of gift
                    {
                        var giftEvent = new GiftEvent
                        {
                            EventTime = DateTime.UtcNow,
                            RoomId = RoomId,
                            UserId = (ulong)_random.Next(10000, 99999),
                            Username = $"user_{_random.Next(1000, 9999)}",
                            GiftId = (uint)_random.Next(1, 100),
                            GiftName = GetRandomGiftName(),
                            DiamondCount = (uint)_random.Next(1, 1000),
                            ComboId = (ulong)_random.Next(100000, 999999),
                            StreakTotal = (uint)_random.Next(1, 10),
                            RepeatEnd = _random.Next(100) < 80 // 80% chance of repeat end
                        };
                        
                        await _eventProcessor.ProcessGiftEventAsync(giftEvent);
                    }
                    else if (eventType < 85) // 10% chance of like
                    {
                        var likeEvent = new SocialEvent
                        {
                            EventTime = DateTime.UtcNow,
                            RoomId = RoomId,
                            UserId = (ulong)_random.Next(10000, 99999),
                            Username = $"user_{_random.Next(1000, 9999)}",
                            SocialType = SocialInteractionType.Like,
                            Count = (uint)_random.Next(1, 10)
                        };
                        
                        await _eventProcessor.ProcessSocialEventAsync(likeEvent);
                    }
                    else if (eventType < 90) // 5% chance of follow
                    {
                        var followEvent = new SocialEvent
                        {
                            EventTime = DateTime.UtcNow,
                            RoomId = RoomId,
                            UserId = (ulong)_random.Next(10000, 99999),
                            Username = $"user_{_random.Next(1000, 9999)}",
                            SocialType = SocialInteractionType.Follow,
                            Count = 1
                        };
                        
                        await _eventProcessor.ProcessSocialEventAsync(followEvent);
                    }
                    else if (eventType < 95) // 5% chance of share
                    {
                        var shareEvent = new SocialEvent
                        {
                            EventTime = DateTime.UtcNow,
                            RoomId = RoomId,
                            UserId = (ulong)_random.Next(10000, 99999),
                            Username = $"user_{_random.Next(1000, 9999)}",
                            SocialType = SocialInteractionType.Share,
                            Count = 1
                        };
                        
                        await _eventProcessor.ProcessSocialEventAsync(shareEvent);
                    }
                    else // 5% chance of join
                    {
                        var joinEvent = new SocialEvent
                        {
                            EventTime = DateTime.UtcNow,
                            RoomId = RoomId,
                            UserId = (ulong)_random.Next(10000, 99999),
                            Username = $"user_{_random.Next(1000, 9999)}",
                            SocialType = SocialInteractionType.Join,
                            Count = 1
                        };
                        
                        await _eventProcessor.ProcessSocialEventAsync(joinEvent);
                    }
                }
            }
            catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
            {
                // Normal cancellation
                _logger.LogDebug("Mock event generator task cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in mock event generator task");
            }
            finally
            {
                _logger.LogInformation("Mock event generator task stopped");
            }
        }
        
        private string GetRandomMessage()
        {
            string[] messages =
            {
                "Hello from the chat!",
                "Great stream today!",
                "🔥🔥🔥",
                "I love your content!",
                "Greetings from Brazil!",
                "Can you say hi to me?",
                "This is amazing",
                "LOL 😂",
                "When is the next stream?",
                "I've been following for years!",
                "First time here, love the vibe",
                "👋👋👋",
                "omg this is so good",
                "How are you today?",
                "What's your favorite song?",
                "Can you dance to this?",
                "Do you have pets?",
                "Have you been to Paris?",
                "Sing a song please!",
                "I'm new here, how often do you stream?"
            };
            
            return messages[_random.Next(messages.Length)];
        }
        
        private string GetRandomGiftName()
        {
            string[] giftNames =
            {
                "Rose",
                "Ice Cream",
                "Guitar",
                "Doughnut",
                "Star",
                "Rocket",
                "Heart",
                "Universe",
                "Lion",
                "Crown",
                "Diamond",
                "Perfume",
                "Cake",
                "Microphone",
                "Trophy"
            };
            
            return giftNames[_random.Next(giftNames.Length)];
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