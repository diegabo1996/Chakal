using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Chakal.Core.Interfaces;
using Chakal.Core.Models.Events;
using Chakal.Application.Services;

namespace Chakal.Tests
{
    public class EventProcessorTests
    {
        private readonly Mock<ILogger<EventProcessor>> _loggerMock;
        private readonly Mock<IEventChannel> _broadcastChannelMock;
        private readonly Mock<IEventChannel> _persistChannelMock;
        private readonly EventProcessor _eventProcessor;

        public EventProcessorTests()
        {
            _loggerMock = new Mock<ILogger<EventProcessor>>();
            _broadcastChannelMock = new Mock<IEventChannel>();
            _persistChannelMock = new Mock<IEventChannel>();
            
            _eventProcessor = new EventProcessor(
                _loggerMock.Object,
                _broadcastChannelMock.Object,
                _persistChannelMock.Object);
        }
        
        [Fact]
        public async Task ProcessChatEvent_ShouldPersistFirst_ThenBroadcast()
        {
            // Arrange
            var chatEvent = new ChatEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = 123456,
                MessageId = 789012,
                UserId = 345678,
                Username = "TestUser",
                Text = "Test message"
            };
            
            var sequence = new MockSequence();
            
            _persistChannelMock
                .InSequence(sequence)
                .Setup(c => c.WriteAsync(chatEvent, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
                
            _broadcastChannelMock
                .InSequence(sequence)
                .Setup(c => c.TryWrite(chatEvent))
                .Returns(true);
            
            // Act
            await _eventProcessor.ProcessChatEventAsync(chatEvent);
            
            // Assert
            _persistChannelMock.Verify(c => c.WriteAsync(chatEvent, It.IsAny<CancellationToken>()), Times.Once);
            _broadcastChannelMock.Verify(c => c.TryWrite(chatEvent), Times.Once);
        }
        
        [Fact]
        public async Task ProcessChatEvent_ShouldNotBlockIfBroadcastChannelFull()
        {
            // Arrange
            var chatEvent = new ChatEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = 123456,
                MessageId = 789012,
                UserId = 345678,
                Username = "TestUser",
                Text = "Test message"
            };
            
            _persistChannelMock
                .Setup(c => c.WriteAsync(chatEvent, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
                
            _broadcastChannelMock
                .Setup(c => c.TryWrite(chatEvent))
                .Returns(false); // Simulate full channel
            
            // Act
            await _eventProcessor.ProcessChatEventAsync(chatEvent);
            
            // Assert
            _persistChannelMock.Verify(c => c.WriteAsync(chatEvent, It.IsAny<CancellationToken>()), Times.Once);
            _broadcastChannelMock.Verify(c => c.TryWrite(chatEvent), Times.Once);
            // Test passes if we reach here without blocking
        }
        
        [Fact]
        public async Task ProcessGiftEvent_ShouldPersistFirst_ThenBroadcast()
        {
            // Arrange
            var giftEvent = new GiftEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = 123456,
                UserId = 345678,
                Username = "TestUser",
                GiftId = 42,
                GiftName = "TestGift",
                DiamondCount = 100
            };
            
            var sequence = new MockSequence();
            
            _persistChannelMock
                .InSequence(sequence)
                .Setup(c => c.WriteAsync(giftEvent, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
                
            _broadcastChannelMock
                .InSequence(sequence)
                .Setup(c => c.TryWrite(giftEvent))
                .Returns(true);
            
            // Act
            await _eventProcessor.ProcessGiftEventAsync(giftEvent);
            
            // Assert
            _persistChannelMock.Verify(c => c.WriteAsync(giftEvent, It.IsAny<CancellationToken>()), Times.Once);
            _broadcastChannelMock.Verify(c => c.TryWrite(giftEvent), Times.Once);
        }
    }
} 