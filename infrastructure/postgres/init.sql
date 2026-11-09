-- ============================================================================
-- Enterprise Network Monitor — initial schema
-- Runs automatically on first container start (mounted into
-- /docker-entrypoint-initdb.d/ by the postgres image). EF Core migrations
-- take over from here for anything after v1 — treat this file as the
-- "day zero" bootstrap only, not a substitute for migrations.
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ----------------------------------------------------------------------------
-- Enum-backed columns
-- These are VARCHAR columns (not Postgres enum types) because the EF Core
-- configurations map the C# enums with HasConversion<string>() — keeping the
-- physical type as varchar avoids Npgsql enum-mapping errors at runtime.
-- ----------------------------------------------------------------------------

-- ----------------------------------------------------------------------------
-- Users (auth)
-- ----------------------------------------------------------------------------
CREATE TABLE users (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username        VARCHAR(64)  NOT NULL UNIQUE,
    email           VARCHAR(256) NOT NULL UNIQUE,
    password_hash   VARCHAR(512) NOT NULL,
    role            VARCHAR(32)  NOT NULL DEFAULT 'Viewer', -- Admin | Operator | Viewer
    is_active       BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    last_login_at   TIMESTAMPTZ
);

-- ----------------------------------------------------------------------------
-- Devices (switch/router/server inventory)
-- ----------------------------------------------------------------------------
CREATE TABLE devices (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            VARCHAR(128) NOT NULL,
    ip_address      VARCHAR(45)  NOT NULL,
    device_type     VARCHAR(32)  NOT NULL DEFAULT 'Other',
    location        VARCHAR(256),
    description     VARCHAR(512),

    -- SNMP polling config
    snmp_community  VARCHAR(128) DEFAULT 'public',
    snmp_port       INTEGER      DEFAULT 161,
    snmp_version    VARCHAR(8)   DEFAULT '2c',

    -- SSH inventory config (credentials go in a secrets store / vault in prod,
    -- these columns hold references, not raw secrets)
    ssh_port        INTEGER      DEFAULT 22,
    ssh_username    VARCHAR(128),
    ssh_credential_ref VARCHAR(256),

    is_monitored    BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT now(),

    UNIQUE (ip_address)
);

CREATE INDEX idx_devices_type ON devices (device_type);
CREATE INDEX idx_devices_monitored ON devices (is_monitored);

-- ----------------------------------------------------------------------------
-- Ping results (every 30s sweep) — this table grows fast, keep it lean
-- ----------------------------------------------------------------------------
CREATE TABLE ping_results (
    id              BIGSERIAL PRIMARY KEY,
    device_id       UUID NOT NULL REFERENCES devices(id) ON DELETE CASCADE,
    is_online       BOOLEAN NOT NULL,
    latency_ms      DOUBLE PRECISION,
    checked_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_ping_results_device_time ON ping_results (device_id, checked_at DESC);

-- ----------------------------------------------------------------------------
-- Resource samples (SNMP CPU/RAM polling)
-- ----------------------------------------------------------------------------
CREATE TABLE resource_samples (
    id              BIGSERIAL PRIMARY KEY,
    device_id       UUID NOT NULL REFERENCES devices(id) ON DELETE CASCADE,
    cpu_percent     DOUBLE PRECISION,
    memory_percent  DOUBLE PRECISION,
    sampled_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_resource_samples_device_time ON resource_samples (device_id, sampled_at DESC);

-- ----------------------------------------------------------------------------
-- Event log (state changes: online<->offline, threshold breaches, alerts sent)
-- ----------------------------------------------------------------------------
CREATE TABLE event_log_entries (
    id              BIGSERIAL PRIMARY KEY,
    device_id       UUID REFERENCES devices(id) ON DELETE SET NULL,
    event_type      VARCHAR(32) NOT NULL,
    message         VARCHAR(1024) NOT NULL,
    metadata        JSONB,
    occurred_at     TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX idx_event_log_device_time ON event_log_entries (device_id, occurred_at DESC);
CREATE INDEX idx_event_log_type ON event_log_entries (event_type);

-- ----------------------------------------------------------------------------
-- Alert rules (threshold engine config + dispatch channel)
-- ----------------------------------------------------------------------------
CREATE TABLE alert_rules (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    device_id           UUID REFERENCES devices(id) ON DELETE CASCADE, -- NULL = applies to all devices
    name                VARCHAR(128) NOT NULL,
    metric              VARCHAR(32)  NOT NULL, -- 'Availability' | 'Cpu' | 'Memory'
    threshold_percent   DOUBLE PRECISION,       -- null for Availability
    consecutive_breaches INTEGER NOT NULL DEFAULT 1,
    channel             VARCHAR(16) NOT NULL DEFAULT 'Email',
    destination         VARCHAR(256) NOT NULL, -- email address or telegram chat id
    is_enabled          BOOLEAN NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_triggered_at   TIMESTAMPTZ
);

CREATE INDEX idx_alert_rules_device ON alert_rules (device_id);

-- ----------------------------------------------------------------------------
-- Seed: a default admin user for first login.
-- Password is "ChangeMe123!" hashed with BCrypt — CHANGE THIS IMMEDIATELY
-- after first login in any non-local environment.
-- This hash was generated with BCrypt (work factor 11) for "ChangeMe123!".
-- ----------------------------------------------------------------------------
INSERT INTO users (username, email, password_hash, role)
VALUES (
    'admin',
    'admin@gmail.com',
    '$2b$11$cuDa4iGaCQU8rUmoTGNXaesHjdK26Yc8.beDU7kHiCAteHujNY3xO',
    'Admin'
);
