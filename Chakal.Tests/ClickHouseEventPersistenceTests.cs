using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Chakal.Core.Models.Events;
using Chakal.Infrastructure.Persistence;

namespace Chakal.Tests
{
    public class ClickHouseEventPersistenceTests
    {
        private readonly Mock<ILogger<ClickHouseEventPersistence>> _loggerMock;
        private readonly Mock<IConfiguration> _configurationMock;
        
        public ClickHouseEventPersistenceTests()
        {
            _loggerMock = new Mock<ILogger<ClickHouseEventPersistence>>();
            _configurationMock = new Mock<IConfiguration>();
        }
        
        [Fact]
        public void Constructor_WithConfiguration_InitializesCorrectly()
        {
            // Arrange
            _configurationMock.Setup(c => c["CLICKHOUSE_CONN"]).Returns("Host=test;Port=8123;Database=chakal;User=default;Password=password");
            
            // Act
            var persistence = new ClickHouseEventPersistence(_configurationMock.Object, _loggerMock.Object);
            
            // Assert
            // Verify that the logger was called with the connection string (with password masked)
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Host=test") && v.ToString().Contains("Password=***")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact]
        public void Constructor_WithNullConnectionString_UsesDefaultConnectionString()
        {
            // Arrange
            _configurationMock.Setup(c => c["CLICKHOUSE_CONN"]).Returns((string?)null);
            
            // Act
            var persistence = new ClickHouseEventPersistence(_configurationMock.Object, _loggerMock.Object);
            
            // Assert
            // Verify that the logger was called with the default connection string
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("localhost")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact]
        public async Task PersistChatEventsAsync_LogsCorrectly()
        {
            // Arrange
            _configurationMock.Setup(c => c["CLICKHOUSE_CONN"]).Returns("Host=localhost;Port=8123;Database=chakal;User=default;Password=password");
            
            var persistence = new ClickHouseEventPersistence(_configurationMock.Object, _loggerMock.Object);
            var chatEvents = new List<ChatEvent>
            {
                new ChatEvent
                {
                    EventTime = DateTime.UtcNow,
                    RoomId = 123456,
                    MessageId = 789012,
                    UserId = 345678,
                    Username = "TestUser",
                    Text = "Test message",
                    DeviceType = "mobile"
                }
            };
            
            // Act
            await persistence.PersistChatEventsAsync(chatEvents);
            
            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("MOCK: Persisting chat events")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        
        [Fact]
        public void Dispose_CalledMultipleTimes_OnlyDisposesOnce()
        {
            // Arrange
            _configurationMock.Setup(c => c["CLICKHOUSE_CONN"]).Returns("Host=localhost;Port=8123;Database=chakal;User=default;Password=password");
            
            var persistence = new ClickHouseEventPersistence(_configurationMock.Object, _loggerMock.Object);
            
            // Act
            persistence.Dispose();
            persistence.Dispose(); // Call twice
            
            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("disposed")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
} 