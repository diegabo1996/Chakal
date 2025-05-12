using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
//using TikTokLiveSharp.Client;
//using TikTokLiveSharp.Events;
//using TikTokLiveSharp.Events.MessageData;
using Chakal.Core.Interfaces;
using Chakal.Core.Models.Events;

namespace Chakal.Infrastructure.Sources
{
    /// <summary>
    /// TikTok Live implementation of <see cref="IEventSource"/>
    /// </summary>
    public class TikTokEventSource : IEventSource
    {
        private readonly ILogger<TikTokEventSource> _logger;
        //private readonly TikTokLiveClient _client;
        private readonly IEventProcessor _eventProcessor;
        private readonly string _hostName;
        private bool _disposed;
        
        /// <summary>
        /// Gets a value indicating whether the source is currently connected
        /// </summary>
        public bool IsConnected => false;
        
        /// <summary>
        /// Gets the current room ID
        /// </summary>
        public ulong RoomId { get; private set; }
        
        /// <summary>
        /// Gets the host username
        /// </summary>
        public string RoomHost => _hostName;
        
        /// <summary>
        /// Initializes a new instance of <see cref="TikTokEventSource"/>
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger</param>
        /// <param name="eventProcessor">Event processor for handling events</param>
        /// <exception cref="ArgumentException">Thrown when required configuration is missing</exception>
        public TikTokEventSource(
            IConfiguration configuration,
            ILogger<TikTokEventSource> logger,
            IEventProcessor eventProcessor)
        {
            throw new NotImplementedException("TikTokEventSource is not implemented. Please use MockEventSource instead.");
        }
        
        private void RegisterEventHandlers()
        {
            throw new NotImplementedException();
        }
        
        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
        
        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
} 