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
using Chakal.Infrastructure.Services;

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
    private readonly RawEventArchiverService _archiverService;

    private EventProcessingDelegate? _callback;
    private CancellationTokenSource? _cts;

    public bool IsConnected => _client?.Connected ?? false;
    public ulong RoomId { get; private set; }

    public string RoomHost => _host;

    public TikTokEventSource(
        IConfiguration cfg,
        ILogger<TikTokEventSource> logger,
        IEventProcessor eventProcessor,
        RawEventArchiverService archiverService)
    {
        _logger = logger;
        _eventProcessor = eventProcessor;
        _archiverService = archiverService;

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
            
            // Archive raw event
            ArchiveRawEvent(e, "join", socialEvt.EventTime);
            
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
            
            // Archive raw event
            ArchiveRawEvent(e, "like", socialEvt.EventTime);
            
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
            
            // Archive raw event
            ArchiveRawEvent(e, "follow", socialEvt.EventTime);
            
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
            
            // Archive raw event
            ArchiveRawEvent(e, "share", socialEvt.EventTime);
            
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
            
            // Archive raw event
            ArchiveRawEvent(e, "chat", chat.EventTime);
            
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
            
            // Archive raw event
            ArchiveRawEvent(e, "gift", gift.EventTime);
            
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
            
            // Archive raw event
            ArchiveRawEvent(e, "social", socialEvt.EventTime);
            
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
            
            // Archive raw event
            ArchiveRawEvent(e, "subscription", subEvt.EventTime);
            
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
            
            // Archive raw event
            ArchiveRawEvent(e, "control", ctrl.EventTime);
            
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
            
            // Archive raw event
            ArchiveRawEvent(e, "roomstats", stats.EventTime);
            
            await _callback!(stats, _cts!.Token);
        };
    }

    private void ArchiveRawEvent(object rawEvent, string eventType, DateTime eventTime)
    {
        // Verificar si el almacenamiento MinIO está habilitado
        var minioAvailable = Environment.GetEnvironmentVariable("MINIO_STORAGE_AVAILABLE");
        if (string.IsNullOrEmpty(minioAvailable) || !bool.TryParse(minioAvailable, out var isAvailable) || !isAvailable)
        {
            // MinIO deshabilitado, no hacer nada
            return;
        }

        try
        {
            var jsonString = JsonConvert.SerializeObject(rawEvent, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            var jsonObj = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(jsonString);
            
            if (jsonObj != null)
            {
                // Intentar obtener un ID único del evento
                string eventId = ExtractEventId(rawEvent, jsonObj, eventType);
                
                var envelope = new WebcastEnvelope
                {
                    EventId = eventId,
                    ReceivedAt = eventTime,
                    RoomId = RoomId,
                    EventType = eventType,
                    RawData = jsonObj
                };
                
                _archiverService.Enqueue(envelope);  // Fire and forget
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive raw event: {EventType}", eventType);
            // Don't throw - archiving should never block the main flow
        }
    }

    /// <summary>
    /// Extrae o genera un ID único para el evento
    /// </summary>
    private string ExtractEventId(object rawEvent, System.Collections.Generic.Dictionary<string, object> jsonObj, string eventType)
    {
        // Intentar obtener el ID basado en el tipo de evento
        try
        {
            // Comprobar si existe un ID específico en el evento
            switch (eventType)
            {
                case "chat":
                    // Para mensajes de chat, usar el MessageId si está disponible
                    if (jsonObj.TryGetValue("messageId", out var msgId) && msgId != null)
                    {
                        return $"chat_{msgId}";
                    }
                    break;
                    
                case "gift":
                    // Para regalos, combinar userId con timestamp para unicidad
                    if (jsonObj.TryGetValue("user", out var user) && user is Newtonsoft.Json.Linq.JObject userObj)
                    {
                        var userId = userObj["id"];
                        if (userId != null)
                        {
                            return $"gift_{userId}_{DateTime.UtcNow.Ticks}";
                        }
                    }
                    break;
                    
                case "subscription":
                    // Para suscripciones, combinar userId con timestamp
                    if (jsonObj.TryGetValue("user", out var subUser) && subUser is Newtonsoft.Json.Linq.JObject subUserObj)
                    {
                        var subUserId = subUserObj["id"];
                        if (subUserId != null)
                        {
                            return $"sub_{subUserId}_{DateTime.UtcNow.Ticks}";
                        }
                    }
                    break;
                    
                // Otros tipos de eventos pueden tener sus propias reglas
            }
            
            // Buscar campos comunes que podrían contener IDs
            if (jsonObj.TryGetValue("id", out var id) && id != null && !string.IsNullOrEmpty(id.ToString()))
            {
                return $"{eventType}_{id}";
            }
            
            if (jsonObj.TryGetValue("msgId", out var messageId) && messageId != null && !string.IsNullOrEmpty(messageId.ToString()))
            {
                return $"{eventType}_{messageId}";
            }
        }
        catch (Exception ex)
        {
            // Ignorar errores al intentar extraer ID, simplemente generaremos uno
            _logger.LogDebug(ex, "Error extracting event ID, will generate a random one");
        }
        
        // Si no se puede obtener un ID específico, generar uno aleatorio
        return $"{eventType}_{Guid.NewGuid()}";
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
