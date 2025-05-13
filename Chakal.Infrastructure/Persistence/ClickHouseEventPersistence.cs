using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Chakal.Core.Interfaces;
using Chakal.Core.Models.Events;
using Octonica.ClickHouseClient;
using Octonica.ClickHouseClient.Types;
using Chakal.Infrastructure.Models.Facts;
using System.Data;

namespace Chakal.Infrastructure.Persistence
{
    /// <summary>
    /// ClickHouse implementation of <see cref="IEventPersistence"/>
    /// </summary>
    public class ClickHouseEventPersistence : IEventPersistence
    {
        private readonly ILogger<ClickHouseEventPersistence> _logger;
        private readonly string _connectionString;
        private bool _disposed;
        
        /// <summary>
        /// Initializes a new instance of <see cref="ClickHouseEventPersistence"/>
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger</param>
        public ClickHouseEventPersistence(
            IConfiguration configuration,
            ILogger<ClickHouseEventPersistence> logger)
        {
            _logger = logger;
            _connectionString = configuration["CLICKHOUSE_CONN"] ?? "Host=192.168.1.230;Port=9000;Database=chakal;User=default;Password=Chakal123!";
            
            _logger.LogInformation("ClickHouse persistence initialized with connection: {ConnectionString}", 
                _connectionString.Replace("Password=", "Password=***"));
        }
        
        /// <inheritdoc />
        public async Task PersistChatEventsAsync(IEnumerable<ChatEvent> events, CancellationToken cancellationToken = default)
        {
            var eventArray = events.ToArray();
            if (eventArray.Length == 0) return;
            
            _logger.LogInformation("Persisting {Count} chat events to ClickHouse", eventArray.Length);
            
            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO fact_chat
                    (event_time, room_id, user_id, message_id, text, device_type)
                    VALUES
                    (@eventTime, @roomId, @userId, @messageId, @message, @deviceType)
                ";
                
                // Insert events one by one instead of as an array to avoid type conversion issues
                foreach (var evt in eventArray)
                {
                    command.Parameters.Clear();
                    
                    var eventTimeParam = new ClickHouseParameter
                    {
                        ParameterName = "eventTime",
                        Value = evt.EventTime,
                        DbType = DbType.DateTime2
                    };
                    eventTimeParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                    eventTimeParam.Scale = 3; // 3 for milliseconds precision
                    command.Parameters.Add(eventTimeParam);
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "roomId",
                        Value = evt.RoomId
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "userId",
                        Value = evt.UserId
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "messageId",
                        Value = evt.MessageId
                    });
                    
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "message",
                        Value = evt.Text ?? string.Empty
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "deviceType",
                        Value = evt.DeviceType ?? string.Empty
                    });
                    
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                
                _logger.LogInformation("Successfully persisted {Count} chat events", eventArray.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting chat events to ClickHouse");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task PersistGiftEventsAsync(IEnumerable<GiftEvent> events, CancellationToken cancellationToken = default)
        {
            var eventArray = events.ToArray();
            if (eventArray.Length == 0) return;
            
            _logger.LogInformation("Persisting {Count} gift events to ClickHouse", eventArray.Length);
            
            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO fact_gift
                    (event_time, room_id, user_id, gift_id, diamond_count, streak_total, repeat_end)
                    VALUES
                    (@eventTime, @roomId, @userId, @giftId, @diamondCount, @streakTotal, @repeatEnd)
                ";
                
                // Insert events one by one instead of as an array to avoid type conversion issues
                foreach (var evt in eventArray)
                {
                    command.Parameters.Clear();
                    
                    var eventTimeParam = new ClickHouseParameter
                    {
                        ParameterName = "eventTime",
                        Value = evt.EventTime,
                        DbType = DbType.DateTime2
                    };
                    eventTimeParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                    eventTimeParam.Scale = 3; // 3 for milliseconds precision
                    command.Parameters.Add(eventTimeParam);
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "roomId",
                        Value = evt.RoomId
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "userId",
                        Value = evt.UserId
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "giftId",
                        Value = evt.GiftId
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "diamondCount",
                        Value = evt.DiamondCount
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "streakTotal",
                        Value = evt.StreakTotal
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "repeatEnd",
                        Value = evt.RepeatEnd
                    });
                    
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                
                _logger.LogInformation("Successfully persisted {Count} gift events", eventArray.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting gift events to ClickHouse");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task PersistSocialEventsAsync(IEnumerable<SocialEvent> events, CancellationToken cancellationToken = default)
        {
            var eventArray = events.ToArray();
            if (eventArray.Length == 0) return;
            
            _logger.LogInformation("Persisting {Count} social events to ClickHouse", eventArray.Length);
            
            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO fact_social
                    (event_time, room_id, user_id, social_type, count)
                    VALUES
                    (@eventTime, @roomId, @userId, @socialType, @count)
                ";
                
                // Insert events one by one instead of as an array to avoid type conversion issues
                foreach (var evt in eventArray)
                {
                    command.Parameters.Clear();
                    
                    var eventTimeParam = new ClickHouseParameter
                    {
                        ParameterName = "eventTime",
                        Value = evt.EventTime,
                        DbType = DbType.DateTime2
                    };
                    eventTimeParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                    eventTimeParam.Scale = 3; // 3 for milliseconds precision
                    command.Parameters.Add(eventTimeParam);
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "roomId",
                        Value = evt.RoomId
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "userId",
                        Value = evt.UserId
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "socialType",
                        Value = (int)evt.SocialType
                    });

                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "count",
                        Value = (int)evt.Count
                    });

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                
                _logger.LogInformation("Successfully persisted {Count} social events", eventArray.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting social events to ClickHouse");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task PersistSubscriptionEventsAsync(IEnumerable<SubscriptionEvent> events, CancellationToken cancellationToken = default)
        {
            var eventArray = events.ToArray();
            if (eventArray.Length == 0) return;
            
            _logger.LogInformation("Persisting {Count} subscription events to ClickHouse", eventArray.Length);
            
            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO fact_sub
                    (event_time, room_id, user_id, months_total, sub_tier, is_renew)
                    VALUES
                    (@eventTime, @roomId, @userId, @monthsTotal, @subTier, @isRenew)
                ";
                
                // Insert events one by one instead of as an array to avoid type conversion issues
                foreach (var evt in eventArray)
                {
                    command.Parameters.Clear();
                    
                    var eventTimeParam = new ClickHouseParameter
                    {
                        ParameterName = "eventTime",
                        Value = evt.EventTime,
                        DbType = DbType.DateTime2
                    };
                    eventTimeParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                    eventTimeParam.Scale = 3; // 3 for milliseconds precision
                    command.Parameters.Add(eventTimeParam);
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "roomId",
                        Value = evt.RoomId
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "userId",
                        Value = evt.UserId
                    });
                    
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "monthsTotal",
                        Value = evt.MonthsTotal
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "subTier",
                        Value = evt.SubTier
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "isRenew",
                        Value = evt.IsRenew
                    });
                    
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                
                _logger.LogInformation("Successfully persisted {Count} subscription events", eventArray.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting subscription events to ClickHouse");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task PersistControlEventsAsync(IEnumerable<ControlEvent> events, CancellationToken cancellationToken = default)
        {
            var eventArray = events.ToArray();
            if (eventArray.Length == 0) return;
            
            _logger.LogInformation("Persisting {Count} control events to ClickHouse", eventArray.Length);
            
            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO fact_control
                    (event_time, room_id, control_type, value)
                    VALUES
                    (@eventTime, @roomId, @controlType, @value)
                ";
                
                // Insert events one by one instead of as an array to avoid type conversion issues
                foreach (var evt in eventArray)
                {
                    command.Parameters.Clear();
                    
                    var eventTimeParam = new ClickHouseParameter
                    {
                        ParameterName = "eventTime",
                        Value = evt.EventTime,
                        DbType = DbType.DateTime2
                    };
                    eventTimeParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                    eventTimeParam.Scale = 3; // 3 for milliseconds precision
                    command.Parameters.Add(eventTimeParam);
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "roomId",
                        Value = evt.RoomId
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "controlType",
                        Value = (int)evt.ControlType
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "value",
                        Value = evt.Value ?? string.Empty
                    });
                    
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                
                _logger.LogInformation("Successfully persisted {Count} control events", eventArray.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting control events to ClickHouse");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task PersistRoomStatsEventsAsync(IEnumerable<RoomStatsEvent> events, CancellationToken cancellationToken = default)
        {
            var eventArray = events.ToArray();
            if (eventArray.Length == 0) return;
            
            _logger.LogInformation("Persisting {Count} room stats events to ClickHouse", eventArray.Length);
            
            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO fact_room
                    (event_time, room_id, viewer_count, like_count, share_count)
                    VALUES
                    (@eventTime, @roomId, @viewerCount, @likeCount, @shareCount)
                ";
                
                // Insert events one by one instead of as an array to avoid type conversion issues
                foreach (var evt in eventArray)
                {
                    command.Parameters.Clear();
                    
                    var eventTimeParam = new ClickHouseParameter
                    {
                        ParameterName = "eventTime",
                        Value = evt.EventTime,
                        DbType = DbType.DateTime2
                    };
                    eventTimeParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                    eventTimeParam.Scale = 3; // 3 for milliseconds precision
                    command.Parameters.Add(eventTimeParam);
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "roomId",
                        Value = evt.RoomId
                    });
                    
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "viewerCount",
                        Value = evt.ViewerCount
                    });
                    
                    // TikTok doesn't provide like count, use 0 as default
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "likeCount",
                        Value = evt.LikeCount
                    });
                    
                    // TikTok doesn't provide share count, use 0 as default
                    command.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "shareCount",
                        Value = evt.ShareCount
                    });
                    
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                
                _logger.LogInformation("Successfully persisted {Count} room stats events", eventArray.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting room stats events to ClickHouse");
                throw;
            }
        }
        
        /// <inheritdoc />
        public async Task PersistUserInfoAsync(
            ulong userId, 
            string uniqueId, 
            string nickname, 
            string region, 
            uint followerCount, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO dim_users
                    (user_id, unique_id, nickname, region, follower_count, last_seen, _version)
                    VALUES
                    (@userId, @uniqueId, @nickname, @region, @followerCount, @lastSeen, 1)
                ";
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "userId",
                    Value = userId
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "uniqueId",
                    Value = uniqueId
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "nickname",
                    Value = nickname
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "region",
                    Value = region
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "followerCount",
                    Value = followerCount
                });
                
                var lastSeenParam = new ClickHouseParameter
                {
                    ParameterName = "lastSeen",
                    Value = DateTime.UtcNow,
                    DbType = DbType.DateTime2
                };
                lastSeenParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                lastSeenParam.Scale = 3; // 3 for milliseconds precision
                command.Parameters.Add(lastSeenParam);
                
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting user info to ClickHouse: {UserId}, {Nickname}", userId, nickname);
                // Don't throw - just log the error, since this is supplemental data
            }
        }
        
        /// <inheritdoc />
        public async Task PersistGiftInfoAsync(
            uint giftId, 
            string name, 
            uint coinCost, 
            uint diamondCost, 
            bool isExclusive, 
            bool isOnPanel, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO dim_gifts
                    (gift_id, name, coin_cost, diamond_cost, is_exclusive, is_on_panel, inserted_at, _version)
                    VALUES
                    (@giftId, @name, @coinCost, @diamondCost, @isExclusive, @isOnPanel, @insertedAt, 1)
                ";
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "giftId",
                    Value = giftId
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "name",
                    Value = name
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "coinCost",
                    Value = coinCost
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "diamondCost",
                    Value = diamondCost
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "isExclusive",
                    Value = isExclusive ? (byte)1 : (byte)0
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "isOnPanel",
                    Value = isOnPanel ? (byte)1 : (byte)0
                });
                
                var insertedAtParam = new ClickHouseParameter
                {
                    ParameterName = "insertedAt",
                    Value = DateTime.UtcNow,
                    DbType = DbType.DateTime2
                };
                insertedAtParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                insertedAtParam.Scale = 3; // 3 for milliseconds precision
                command.Parameters.Add(insertedAtParam);
                
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting gift info to ClickHouse: {GiftId}, {Name}", giftId, name);
                // Don't throw - just log the error, since this is supplemental data
            }
        }
        
        /// <inheritdoc />
        public async Task PersistRoomInfoAsync(
            ulong roomId, 
            ulong hostUserId, 
            string title, 
            string language, 
            DateTime startTime, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO dim_rooms
                    (room_id, host_user_id, title, language, start_time, inserted_at, _version)
                    VALUES
                    (@roomId, @hostUserId, @title, @language, @startTime, @insertedAt, 1)
                ";
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "roomId",
                    Value = roomId
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "hostUserId",
                    Value = hostUserId
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "title",
                    Value = title
                });
                
                command.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "language",
                    Value = language
                });
                
                var startTimeParam = new ClickHouseParameter
                {
                    ParameterName = "startTime",
                    Value = startTime,
                    DbType = DbType.DateTime2
                };
                startTimeParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                startTimeParam.Scale = 3; // 3 for milliseconds precision
                command.Parameters.Add(startTimeParam);
                
                var insertedAtParam = new ClickHouseParameter
                {
                    ParameterName = "insertedAt",
                    Value = DateTime.UtcNow,
                    DbType = DbType.DateTime2
                };
                insertedAtParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                insertedAtParam.Scale = 3; // 3 for milliseconds precision
                command.Parameters.Add(insertedAtParam);
                
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting room info to ClickHouse: {RoomId}, {Title}", roomId, title);
                // Don't throw - just log the error, since this is supplemental data
            }
        }
        
        /// <inheritdoc />
        public async Task UpdateRoomEndTimeAsync(ulong roomId, DateTime endTime, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new ClickHouseConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                
                // First, fetch the current room data
                using var selectCommand = connection.CreateCommand();
                selectCommand.CommandText = @"
                    SELECT host_user_id, title, language, start_time
                    FROM dim_rooms
                    WHERE room_id = @roomId
                    ORDER BY _version DESC
                    LIMIT 1
                ";
                
                selectCommand.Parameters.Add(new ClickHouseParameter
                {
                    ParameterName = "roomId",
                    Value = roomId
                });
                
                using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
                
                if (await reader.ReadAsync(cancellationToken))
                {
                    var hostUserId = reader.GetUInt64(0);
                    var title = reader.GetString(1);
                    var language = reader.GetString(2);
                    var startTime = reader.GetDateTime(3);
                    
                    // Now insert a new version with the end time
                    using var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"
                        INSERT INTO dim_rooms
                        (room_id, host_user_id, title, language, start_time, end_time, inserted_at, _version)
                        VALUES
                        (@roomId, @hostUserId, @title, @language, @startTime, @endTime, @insertedAt, 
                         (SELECT max(_version) + 1 FROM dim_rooms WHERE room_id = @roomId))
                    ";
                    
                    insertCommand.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "roomId",
                        Value = roomId
                    });
                    
                    insertCommand.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "hostUserId",
                        Value = hostUserId
                    });
                    
                    insertCommand.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "title",
                        Value = title
                    });
                    
                    insertCommand.Parameters.Add(new ClickHouseParameter
                    {
                        ParameterName = "language",
                        Value = language
                    });
                    
                    var startTimeParam = new ClickHouseParameter
                    {
                        ParameterName = "startTime",
                        Value = startTime,
                        DbType = DbType.DateTime2
                    };
                    startTimeParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                    startTimeParam.Scale = 3; // 3 for milliseconds precision
                    insertCommand.Parameters.Add(startTimeParam);
                    
                    var endTimeParam = new ClickHouseParameter
                    {
                        ParameterName = "endTime",
                        Value = endTime,
                        DbType = DbType.DateTime2
                    };
                    endTimeParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                    endTimeParam.Scale = 3; // 3 for milliseconds precision
                    insertCommand.Parameters.Add(endTimeParam);
                    
                    var insertedAtParam = new ClickHouseParameter
                    {
                        ParameterName = "insertedAt",
                        Value = DateTime.UtcNow,
                        DbType = DbType.DateTime2
                    };
                    insertedAtParam.ClickHouseDbType = ClickHouseDbType.DateTime64;
                    insertedAtParam.Scale = 3; // 3 for milliseconds precision
                    insertCommand.Parameters.Add(insertedAtParam);
                    
                    await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Room {RoomId} not found when trying to update end time", roomId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating room end time in ClickHouse: {RoomId}, {EndTime}", roomId, endTime);
                // Don't throw - just log the error, since this is supplemental data
            }
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            _logger.LogInformation("ClickHouse persistence disposed");
        }
        
        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;
            
            _logger.LogInformation("ClickHouse persistence disposed asynchronously");
            return ValueTask.CompletedTask;
        }
    }
} 