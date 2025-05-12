/* ================================================================
   CHAKAL – ClickHouse schema
   Separador Tabix: ";;"  ➜  Ejecuta con Shift+Ctrl+Enter
===================================================================*/

/* 0 · Base de datos ───────────────────────────────────────────── */
CREATE DATABASE IF NOT EXISTS chakal
;;

USE chakal
;;

/* 1 · DIMENSIONES ─────────────────────────────────────────────── */
CREATE TABLE dim_users
(
    user_id        UInt64,
    unique_id      String,
    nickname       String,
    region         LowCardinality(String),
    follower_count UInt32,
    first_seen     DateTime DEFAULT now(),
    last_seen      DateTime,
    _version       UInt8    DEFAULT 1
)
ENGINE = ReplacingMergeTree(_version)
ORDER BY user_id
;;

CREATE TABLE dim_gifts
(
    gift_id      UInt32,
    name         String,
    coin_cost    UInt32,
    diamond_cost UInt32,
    is_exclusive UInt8,
    is_on_panel  UInt8,
    inserted_at  DateTime DEFAULT now(),
    _version     UInt8    DEFAULT 1
)
ENGINE = ReplacingMergeTree(_version)
ORDER BY gift_id
;;

CREATE TABLE dim_rooms
(
    room_id      UInt64,
    host_user_id UInt64,
    title        String,
    language     LowCardinality(String),
    start_time   DateTime,
    end_time     DateTime,
    inserted_at  DateTime DEFAULT now(),
    _version     UInt8    DEFAULT 1
)
ENGINE = ReplacingMergeTree(_version)
ORDER BY room_id
;;

/* 2 · TABLAS DE HECHOS ────────────────────────────────────────── */
CREATE TABLE fact_chat
(
    event_time   DateTime64(3),
    room_id      UInt64,
    message_id   UInt64,
    user_id      UInt64,
    text         String,
    emotes       Map(String, UInt32),
    reply_to_id  UInt64,
    device_type  LowCardinality(String)
)
ENGINE = MergeTree
PARTITION BY toYYYYMMDD(event_time)
ORDER BY (room_id, event_time, message_id)
TTL event_time + INTERVAL 180 DAY DELETE
;;

CREATE TABLE fact_gift
(
    event_time     DateTime64(3),
    room_id        UInt64,
    user_id        UInt64,
    gift_id        UInt32,
    diamond_count  UInt32,
    combo_id       UInt64,
    streak_total   UInt32,
    repeat_end     UInt8
)
ENGINE = MergeTree
PARTITION BY toYYYYMMDD(event_time)
ORDER BY (room_id, event_time, user_id, gift_id)
TTL event_time + INTERVAL 365 DAY DELETE
;;

CREATE TABLE fact_social
(
    event_time  DateTime64(3),
    room_id     UInt64,
    user_id     UInt64,
    social_type Enum8('like'=1,'follow'=2,'share'=3,'join'=4),
    count       UInt32 DEFAULT 1
)
ENGINE = MergeTree
PARTITION BY toYYYYMMDD(event_time)
ORDER BY (room_id, event_time, user_id, social_type)
TTL event_time + INTERVAL 180 DAY DELETE
;;

CREATE TABLE fact_sub
(
    event_time   DateTime64(3),
    room_id      UInt64,
    user_id      UInt64,
    sub_tier     UInt8,
    months_total UInt16,
    is_renew     UInt8
)
ENGINE = MergeTree
PARTITION BY toYYYYMMDD(event_time)
ORDER BY (room_id, event_time, user_id)
TTL event_time + INTERVAL 365 DAY DELETE
;;

CREATE TABLE fact_control
(
    event_time   DateTime64(3),
    room_id      UInt64,
    control_type Enum8('live_start'=1,'live_pause'=2,'live_resume'=3,'live_end'=4),
    value        String
)
ENGINE = MergeTree
PARTITION BY toYYYYMMDD(event_time)
ORDER BY (room_id, event_time)
TTL event_time + INTERVAL 365 DAY DELETE
;;

CREATE TABLE fact_room
(
    event_time    DateTime64(3),
    room_id       UInt64,
    viewer_count  UInt32,
    like_count    UInt32,
    share_count   UInt32
)
ENGINE = MergeTree
PARTITION BY toYYYYMMDD(event_time)
ORDER BY (room_id, event_time)
TTL event_time + INTERVAL 90 DAY DELETE
;;

/* 3 · AGREGADOS 1-MIN ─────────────────────────────────────────── */
CREATE TABLE agg_room_1m
(
    room_id   UInt64,
    ts_min    DateTime,
    chats     UInt32,
    likes     UInt32,
    follows   UInt32,
    shares    UInt32,
    joins     UInt32,
    gifts     UInt32,
    diamonds  UInt64,
    subs      UInt32
)
ENGINE = SummingMergeTree
PARTITION BY toYYYYMM(ts_min)
ORDER BY (room_id, ts_min)
;;

CREATE MATERIALIZED VIEW mv_to_agg_1m
TO agg_room_1m AS
SELECT
    room_id,
    toStartOfMinute(event_time) AS ts_min,
    count()                  FILTER (WHERE src = 'chat')                              AS chats,
    sum(cnt)                 FILTER (WHERE src = 'social' AND social_type = 'like')   AS likes,
    sum(cnt)                 FILTER (WHERE src = 'social' AND social_type = 'follow') AS follows,
    sum(cnt)                 FILTER (WHERE src = 'social' AND social_type = 'share')  AS shares,
    sum(cnt)                 FILTER (WHERE src = 'social' AND social_type = 'join')   AS joins,
    count()                  FILTER (WHERE src = 'gift')                              AS gifts,
    sum(diamond_count)       FILTER (WHERE src = 'gift')                              AS diamonds,
    count()                  FILTER (WHERE src = 'sub')                               AS subs
FROM
(
    SELECT room_id, event_time, 1        AS cnt, 'chat'   AS src, NULL AS social_type, 0 AS diamond_count FROM fact_chat
    UNION ALL
    SELECT room_id, event_time, 1        AS cnt, 'gift',       NULL, diamond_count     FROM fact_gift
    UNION ALL
    SELECT room_id, event_time, count    AS cnt, 'social', social_type, 0              FROM fact_social
    UNION ALL
    SELECT room_id, event_time, 1        AS cnt, 'sub',        NULL, 0                 FROM fact_sub
)
GROUP BY room_id, ts_min
;;

/* 4 · ÍNDICE DE TEXTO EN CHAT (opcional) ─────────────────────── */
ALTER TABLE fact_chat
    ADD INDEX idx_text tokenbf_v1(text) TYPE tokenbf_v1(1024,3,0) GRANULARITY 4
;;

/* ───── SCRIPT COMPLETO ───── */ 