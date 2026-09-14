-- Skema database Composta (SQLite)
-- Id memakai GUID string (Guid.NewGuid().ToString()) sehingga panjangnya tetap 36 karakter.
-- SQLite menerima nama tipe VARCHAR/DECIMAL/BOOLEAN/DATETIME dan memetakannya ke
-- type affinity TEXT, NUMERIC, dan INTEGER; besaran (n) dipakai sebagai batas rancangan.

PRAGMA foreign_keys = ON;

CREATE TABLE users (
    id          VARCHAR(36)  PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    email       VARCHAR(255) NOT NULL UNIQUE,
    joined_at   DATETIME     NOT NULL
);

CREATE TABLE badges (
    id          VARCHAR(36)  PRIMARY KEY,
    name        VARCHAR(50)  NOT NULL,
    criteria    VARCHAR(50)  NOT NULL,          -- format "CO2E:10", "BATCH:3", "HARVEST:1"
    icon_path   VARCHAR(255)
);

CREATE TABLE user_badges (
    user_id     VARCHAR(36)  NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    badge_id    VARCHAR(36)  NOT NULL REFERENCES badges(id) ON DELETE CASCADE,
    earned_at   DATETIME     NOT NULL,
    PRIMARY KEY (user_id, badge_id)
);

CREATE TABLE compost_batches (
    id                      VARCHAR(36)  PRIMARY KEY,
    user_id                 VARCHAR(36)  NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    start_date              DATETIME     NOT NULL,
    status                  VARCHAR(10)  NOT NULL DEFAULT 'Active'
                            CHECK (status IN ('Active', 'Curing', 'Ready', 'Archived')),
    brown_green_ratio       DECIMAL(5,2) NOT NULL DEFAULT 0,
    estimated_maturity_date DATETIME     NOT NULL
);

CREATE TABLE waste_categories (
    id                  VARCHAR(36)  PRIMARY KEY,
    name                VARCHAR(50)  NOT NULL,
    co2e_factor_per_kg  DECIMAL(6,3) NOT NULL DEFAULT 0,
    is_compostable      BOOLEAN      NOT NULL DEFAULT 1
);

CREATE TABLE waste_items (
    id                VARCHAR(36)   PRIMARY KEY,
    batch_id          VARCHAR(36)   NOT NULL REFERENCES compost_batches(id) ON DELETE CASCADE,
    category_id       VARCHAR(36)   REFERENCES waste_categories(id) ON DELETE SET NULL,
    item_type         VARCHAR(15)   NOT NULL CHECK (item_type IN ('compostable', 'noncompostable')),
    name              VARCHAR(100)  NOT NULL,
    weight            DECIMAL(8,2)  NOT NULL CHECK (weight >= 0),
    date_added        DATETIME      NOT NULL,
    brown_green_type  VARCHAR(5)    CHECK (brown_green_type IN ('Brown', 'Green')),  -- hanya compostable
    moisture_content  DECIMAL(5,2)  CHECK (moisture_content BETWEEN 0 AND 100),     -- hanya compostable
    reason            VARCHAR(255)                                                   -- hanya noncompostable
);

CREATE TABLE maintenance_schedules (
    id            VARCHAR(36)  PRIMARY KEY,
    batch_id      VARCHAR(36)  NOT NULL REFERENCES compost_batches(id) ON DELETE CASCADE,
    task_type     VARCHAR(15)  NOT NULL CHECK (task_type IN ('Turn', 'Water', 'CheckMoisture', 'Harvest')),
    due_date      DATETIME     NOT NULL,
    is_completed  BOOLEAN      NOT NULL DEFAULT 0
);

CREATE TABLE compost_recommendations (
    id          VARCHAR(36)  PRIMARY KEY,
    batch_id    VARCHAR(36)  NOT NULL REFERENCES compost_batches(id) ON DELETE CASCADE,
    message     VARCHAR(500) NOT NULL,
    priority    VARCHAR(10)  NOT NULL DEFAULT 'Low' CHECK (priority IN ('Low', 'Medium', 'High')),
    created_at  DATETIME     NOT NULL
);
