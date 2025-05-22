CREATE DATABASE IF NOT EXISTS mi_bd;
CREATE TABLE chakal.agg_room_1m (`room_id` UInt64, `ts_min` DateTime, `chats` UInt32, `likes` UInt32, `follows` UInt32, `shares` UInt32, `joins` UInt32, `gifts` UInt32, `diamonds` UInt64, `subs` UInt32) ENGINE = SummingMergeTree PARTITION BY toYYYYMM(ts_min) ORDER BY (room_id, ts_min) SETTINGS index_granularity = 8192;
CREATE TABLE chakal.dim_gifts (`gift_id` UInt32, `name` String, `coin_cost` UInt32, `diamond_cost` UInt32, `is_exclusive` UInt8, `is_on_panel` UInt8, `inserted_at` DateTime64(3) DEFAULT now64(), `_version` UInt8 DEFAULT 1) ENGINE = ReplacingMergeTree(_version) ORDER BY gift_id SETTINGS index_granularity = 8192;
CREATE TABLE chakal.dim_rooms (`room_id` UInt64, `host_user_id` UInt64, `title` String, `language` LowCardinality(String), `start_time` DateTime64(3), `end_time` DateTime64(3), `inserted_at` DateTime64(3) DEFAULT now64(), `_version` UInt8 DEFAULT 1) ENGINE = ReplacingMergeTree(_version) ORDER BY room_id SETTINGS index_granularity = 8192;
CREATE TABLE chakal.dim_users (`user_id` UInt64, `unique_id` String, `nickname` String, `region` LowCardinality(String), `follower_count` UInt32, `first_seen` DateTime64(3) DEFAULT now64(), `last_seen` DateTime64(3), `_version` UInt8 DEFAULT 1) ENGINE = ReplacingMergeTree(_version) ORDER BY user_id SETTINGS index_granularity = 8192;
CREATE TABLE chakal.fact_chat (
    event_time         DateTime64(3),
    room_id            UInt64,
    message_id         UInt64,
    user_id            UInt64,
    text               String,
    emotes             Map(String, UInt32),
    mentioned_user_ids Array(UInt64),
    language           LowCardinality(String),
    device_type        LowCardinality(String)
)
ENGINE = MergeTree
PARTITION BY toYYYYMMDD(event_time)
ORDER BY (room_id, event_time, message_id)
TTL event_time + INTERVAL 180 DAY DELETE;
CREATE TABLE chakal.fact_control (`event_time` DateTime64(3), `room_id` UInt64, `control_type` Enum8('live_start' = 1, 'live_pause' = 2, 'live_resume' = 3, 'live_end' = 4), `value` String) ENGINE = MergeTree PARTITION BY toYYYYMMDD(event_time) ORDER BY (room_id, event_time) SETTINGS index_granularity = 8192;
CREATE TABLE chakal.fact_gift (`event_time` DateTime64(3), `room_id` UInt64, `user_id` UInt64, `gift_id` UInt32, `diamond_count` UInt32, `combo_id` UInt64, `streak_total` UInt32, `repeat_end` UInt8) ENGINE = MergeTree PARTITION BY toYYYYMMDD(event_time) ORDER BY (room_id, event_time, user_id, gift_id) SETTINGS index_granularity = 8192;
CREATE TABLE chakal.fact_room (`event_time` DateTime64(3), `room_id` UInt64, `viewer_count` UInt32, `like_count` UInt32, `share_count` UInt32) ENGINE = MergeTree PARTITION BY toYYYYMMDD(event_time) ORDER BY (room_id, event_time) SETTINGS index_granularity = 8192;
CREATE TABLE chakal.fact_social (`event_time` DateTime64(3), `room_id` UInt64, `user_id` UInt64, `social_type` Enum8('like' = 1, 'follow' = 2, 'share' = 3, 'join' = 4), `count` UInt32 DEFAULT 1) ENGINE = MergeTree PARTITION BY toYYYYMMDD(event_time) ORDER BY (room_id, event_time, user_id, social_type) SETTINGS index_granularity = 8192;
CREATE TABLE chakal.fact_sub (`event_time` DateTime64(3), `room_id` UInt64, `user_id` UInt64, `sub_tier` UInt8, `months_total` UInt16, `is_renew` UInt8) ENGINE = MergeTree PARTITION BY toYYYYMMDD(event_time) ORDER BY (room_id, event_time, user_id) SETTINGS index_granularity = 8192;
CREATE MATERIALIZED VIEW chakal.mv_to_agg_1m TO chakal.agg_room_1m (`room_id` UInt64, `ts_min` DateTime, `chats` UInt64, `likes` UInt64, `follows` UInt64, `shares` UInt64, `joins` UInt64, `gifts` UInt64, `diamonds` UInt64, `subs` UInt64) AS SELECT room_id, toStartOfMinute(event_time) AS ts_min, countIf(src = 'chat') AS chats, sumIf(cnt, (src = 'social') AND (social_type = 'like')) AS likes, sumIf(cnt, (src = 'social') AND (social_type = 'follow')) AS follows, sumIf(cnt, (src = 'social') AND (social_type = 'share')) AS shares, sumIf(cnt, (src = 'social') AND (social_type = 'join')) AS joins, countIf(src = 'gift') AS gifts, sumIf(diamond_count, src = 'gift') AS diamonds, countIf(src = 'sub') AS subs FROM (SELECT room_id, event_time, 1 AS cnt, 'chat' AS src, NULL AS social_type, 0 AS diamond_count FROM chakal.fact_chat UNION ALL SELECT room_id, event_time, 1 AS cnt, 'gift', NULL, diamond_count FROM chakal.fact_gift UNION ALL SELECT room_id, event_time, count AS cnt, 'social', social_type, 0 FROM chakal.fact_social UNION ALL SELECT room_id, event_time, 1 AS cnt, 'sub', NULL, 0 FROM chakal.fact_sub) GROUP BY room_id, ts_min;