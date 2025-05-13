using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Chakal.Core.Interfaces;
using Chakal.Core.Models.Events;
using TikTokLiveSharp.Client;
using TikTokLiveSharp.Events;

using Chakal.Infrastructure.Models.Facts;
using TikTokLiveSharp.Events.Enums;
using Newtonsoft.Json;

namespace Chakal.Infrastructure.Sources;

/// <summary>
/// TikTok Live implementation of <see cref="IEventSource"/> using TikTokLiveSharp.
/// </summary>
public class TikTokEventSource : IEventSource, IDisposable, IAsyncDisposable
{
    private readonly ILogger<TikTokEventSource> _logger;
    private readonly string _host;
    private readonly TikTokLiveClient _client;
    private readonly IEventProcessor _eventProcessor;

    private EventProcessingDelegate? _callback;
    private CancellationTokenSource? _cts;

    public bool IsConnected => _client?.Connected ?? false;
    public ulong RoomId { get; private set; }

    public string RoomHost => _host;

    public TikTokEventSource(
        IConfiguration cfg,
        ILogger<TikTokEventSource> logger,
        IEventProcessor eventProcessor)
    {
        _logger = logger;
        _eventProcessor = eventProcessor;

        _host = cfg["TIKTOK_HOST"]
            ?? throw new ArgumentException("TIKTOK_HOST environment variable is required");
        // Create TikTokLiveSharp client; host without @ and without spaces

        // Specify the constructor explicitly to resolve ambiguity
        _client = new TikTokLiveClient(
            _host,
            null, // Pass null for optional float? parameters
            null,
            null,
            null, // Pass null for optional string parameters
            false, // Default values for optional bool parameters
            false,
            null, // Pass null for optional Dictionary<string, object>
            false,
            false,
            null, // Pass null for optional IWebProxy
            null, // Pass null for optional string parameters
            0,    // Default value for uint
            false,
            TikTokLiveSharp.Client.Config.LogLevel.Error, // Example LogLevel
            false,
            false,
            null, // Pass null for optional string parameters
            null  // Pass null for optional string parameters
        );
        RegisterCallbacks();
    }

    /*────────────────────────────────────────────── START / STOP */

    public Task StartAsync(CancellationToken ct = default)
        => StartAsync(null, ct);

    public async Task StartAsync(EventProcessingDelegate? cb, CancellationToken ct = default)
    {
        _callback = cb ?? PassthroughProcessorAsync;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _logger.LogInformation("Connecting to TikTok Live @{Host}…", _host);
        await _client.RunAsync(_cts.Token);    // blocks until cancellation
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Stopping TikTokEventSource…");
        _cts?.Cancel();
        return Task.CompletedTask;
    }

    /*──────────────────────────────────────────── CALLBACKS TikTokLiveSharp */

    private void RegisterCallbacks()
    {
        _client.OnConnected += (_, _) =>
            _logger.LogInformation("Connected to room {RoomId}", RoomId = ulong.TryParse(_client.RoomID, out var id) ? id : 0);

        _client.OnDisconnected += (_, _) =>
            _logger.LogInformation("Disconnected from TikTok Live");

        _client.OnJoin += async (_, e) =>
        {
            _logger.LogDebug(JsonConvert.SerializeObject(e));
            var socialEvt = new SocialEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                UserId = e.User.Id is long id ? (ulong)id : 0,
                Username = e.User?.UniqueId ?? "unknown",
                SocialType = SocialInteractionType.Join
            };
            await _callback!(socialEvt, _cts!.Token);
        };
        _client.OnLike += async (_, e) =>
        {
            _logger.LogDebug(JsonConvert.SerializeObject(e));
            var socialEvt = new SocialEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                UserId = e.Sender?.Id is long id ? (ulong)id : 0,
                Username = e.Sender?.UniqueId ?? "unknown",
                SocialType = SocialInteractionType.Like,
                Count = (uint)e.Count
            };
            await _callback!(socialEvt, _cts!.Token);
        };

        _client.OnFollow += async (_, e) =>
        {
            _logger.LogDebug(JsonConvert.SerializeObject(e));
            var socialEvt = new SocialEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                UserId = e.User.Id is long id ? (ulong)id : 0,
                Username = e.User?.UniqueId ?? "unknown",
                SocialType = SocialInteractionType.Follow
            };
            await _callback!(socialEvt, _cts!.Token);
        };

        _client.OnShare += async (_, e) =>
        {
            _logger.LogDebug(JsonConvert.SerializeObject(e));
            var socialEvt = new SocialEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                UserId = e.User.Id is long id ? (ulong)id : 0,
                Username = e.User?.UniqueId ?? "unknown",
                SocialType = SocialInteractionType.Share
            };
            await _callback!(socialEvt, _cts!.Token);
        };

        _client.OnChatMessage += async (_, e) =>
        {
            _logger.LogDebug(JsonConvert.SerializeObject(e));
            var chat = new ChatEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                MessageId = (ulong)e.MessageId,
                UserId = e.Sender?.Id is long id ? (ulong)id : 0,
                Username = e.Sender?.UniqueId ?? "unknown",
                Text = e.Message
            };
            await _callback!(chat, _cts!.Token);
        };

        _client.OnGiftMessage += async (_, e) =>
        {
            _logger.LogDebug(JsonConvert.SerializeObject(e));
            var gift = new GiftEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                UserId = e.User?.Id is long id ? (ulong)id : 0,
                Username = e.User?.UniqueId ?? "unknown",
                GiftId = (uint)(e.Gift?.Id ?? 0),
                DiamondCount = (uint)(e.Gift?.DiamondCost ?? 0),
                StreakTotal = (uint)e.RepeatCount,
                RepeatEnd = e.RepeatCount > 0
            };
            await _callback!(gift, _cts!.Token);
        };

        _client.OnSocialMessage += async (_, e) =>
        {
            _logger.LogDebug(JsonConvert.SerializeObject(e));
            var socialEvt = new SocialEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                UserId = e.Sender?.Id is long id ? (ulong)id : 0,
                Username = e.Sender?.UniqueId ?? "unknown",
                SocialType = (SocialInteractionType)MapSocialType(e.ShareType.ToString())
            };
            await _callback!(socialEvt, _cts!.Token);
        };

        _client.OnSubscribe += async (_, e) =>
        {
            _logger.LogDebug(JsonConvert.SerializeObject(e));
            var subEvt = new SubscriptionEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                UserId = e.User?.Id is long id ? (ulong)id : 0,
                Username = e.User?.UniqueId ?? "unknown",
                MonthsTotal = 1, // Default value since TotalMonths is not available
                SubTier = 1,     // Default value since SubTier is not available
                IsRenew = false  // Default value since IsRenew is not available
            };
            await _callback!(subEvt, _cts!.Token);
        };

        _client.OnControlMessage += async (_, e) =>
        {
            _logger.LogDebug(JsonConvert.SerializeObject(e));
            var type = e.Action switch
            {
                ControlAction.Stream_Ended => ControlEventType.LiveEnd,
                ControlAction.Stream_Paused => ControlEventType.LivePause,
                ControlAction.Stream_Unpaused => ControlEventType.LiveResume,
                _ => ControlEventType.LiveStart
            };

            var ctrl = new ControlEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                ControlType = type,
                Value = e.BaseDescription
            };
            await _callback!(ctrl, _cts!.Token);
        };

        _client.OnRoomUpdate += async (_, e) =>
        {
            _logger.LogDebug(JsonConvert.SerializeObject(e));
            var stats = new RoomStatsEvent
            {
                EventTime = DateTime.UtcNow,
                RoomId = RoomId,
                ViewerCount = (uint)e.NumberOfViewers
            };
            await _callback!(stats, _cts!.Token);
        };
    }

    /*──────────────────────────────────────────── UTIL */

    private static SocialType MapSocialType(string raw) => raw switch
    {
        TikTokLiveClient.SocialTypes.LikeType => SocialType.Like,
        TikTokLiveClient.SocialTypes.FollowType => SocialType.Follow,
        TikTokLiveClient.SocialTypes.ShareType => SocialType.Share,
        TikTokLiveClient.SocialTypes.JoinType => SocialType.Join,
        _ => SocialType.Unknown
    };

    private async Task PassthroughProcessorAsync(object evt, CancellationToken ct)
    {
        // Reuse the EventProcessor to avoid duplicating mapping
        switch (evt)
        {
            case ChatEvent c: await _eventProcessor.ProcessChatEventAsync(c, ct); break;
            case GiftEvent g: await _eventProcessor.ProcessGiftEventAsync(g, ct); break;
            case SocialEvent s: await _eventProcessor.ProcessSocialEventAsync(s, ct); break;
            case SubscriptionEvent sb: await _eventProcessor.ProcessSubscriptionEventAsync(sb, ct); break;
            case ControlEvent ctrl: await _eventProcessor.ProcessControlEventAsync(ctrl, ct); break;
            case RoomStatsEvent rs: await _eventProcessor.ProcessRoomStatsEventAsync(rs, ct); break;
        }
    }

    public void Dispose() => _client?.Stop();
    public ValueTask DisposeAsync() => new(_client?.Stop() ?? Task.CompletedTask);
}
