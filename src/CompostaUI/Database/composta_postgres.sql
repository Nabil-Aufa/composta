DROP TABLE IF EXISTS compost_recommendations, maintenance_schedules, waste_items,
    waste_categories, compost_batches, user_badges, badges, users CASCADE;

CREATE TABLE users (
    id          VARCHAR(36)  PRIMARY KEY,
    name        VARCHAR(100) NOT NULL,
    email       VARCHAR(255) NOT NULL UNIQUE,
    joined_at   TIMESTAMP    NOT NULL
);

CREATE TABLE badges (
    id          VARCHAR(36)  PRIMARY KEY,
    name        VARCHAR(50)  NOT NULL,
    criteria    VARCHAR(50)  NOT NULL,
    icon_path   VARCHAR(255)
);

CREATE TABLE user_badges (
    user_id     VARCHAR(36)  NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    badge_id    VARCHAR(36)  NOT NULL REFERENCES badges(id) ON DELETE CASCADE,
    earned_at   TIMESTAMP    NOT NULL,
    PRIMARY KEY (user_id, badge_id)
);

CREATE TABLE compost_batches (
    id                      VARCHAR(36)  PRIMARY KEY,
    user_id                 VARCHAR(36)  NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    start_date              TIMESTAMP    NOT NULL,
    status                  VARCHAR(10)  NOT NULL DEFAULT 'Active'
                            CHECK (status IN ('Active', 'Curing', 'Ready', 'Archived')),
    brown_green_ratio       NUMERIC(5,2) NOT NULL DEFAULT 0,
    estimated_maturity_date TIMESTAMP    NOT NULL
);

CREATE TABLE waste_categories (
    id                  VARCHAR(36)  PRIMARY KEY,
    name                VARCHAR(50)  NOT NULL,
    co2e_factor_per_kg  NUMERIC(6,3) NOT NULL DEFAULT 0,
    is_compostable      BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE waste_items (
    id                VARCHAR(36)   PRIMARY KEY,
    batch_id          VARCHAR(36)   NOT NULL REFERENCES compost_batches(id) ON DELETE CASCADE,
    category_id       VARCHAR(36)   REFERENCES waste_categories(id) ON DELETE SET NULL,
    item_type         VARCHAR(15)   NOT NULL CHECK (item_type IN ('compostable', 'noncompostable')),
    name              VARCHAR(100)  NOT NULL,
    weight            NUMERIC(8,2)  NOT NULL CHECK (weight >= 0),
    date_added        TIMESTAMP     NOT NULL,
    brown_green_type  VARCHAR(5)    CHECK (brown_green_type IN ('Brown', 'Green')),
    moisture_content  NUMERIC(5,2)  CHECK (moisture_content BETWEEN 0 AND 100),
    reason            VARCHAR(255)
);

CREATE TABLE maintenance_schedules (
    id            VARCHAR(36)  PRIMARY KEY,
    batch_id      VARCHAR(36)  NOT NULL REFERENCES compost_batches(id) ON DELETE CASCADE,
    task_type     VARCHAR(15)  NOT NULL CHECK (task_type IN ('Turn', 'Water', 'CheckMoisture', 'Harvest')),
    due_date      TIMESTAMP    NOT NULL,
    is_completed  BOOLEAN      NOT NULL DEFAULT FALSE
);

CREATE TABLE compost_recommendations (
    id          VARCHAR(36)  PRIMARY KEY,
    batch_id    VARCHAR(36)  NOT NULL REFERENCES compost_batches(id) ON DELETE CASCADE,
    message     VARCHAR(500) NOT NULL,
    priority    VARCHAR(10)  NOT NULL DEFAULT 'Low' CHECK (priority IN ('Low', 'Medium', 'High')),
    created_at  TIMESTAMP    NOT NULL
);

CREATE OR REPLACE FUNCTION st_select_users()
    RETURNS TABLE (id VARCHAR, name VARCHAR, email VARCHAR, joined_at TIMESTAMP)
    LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.name, u.email, u.joined_at
    FROM users u
    ORDER BY u.joined_at;
END;
$$;

CREATE OR REPLACE FUNCTION st_select_user_by_id(_id VARCHAR)
    RETURNS TABLE (id VARCHAR, name VARCHAR, email VARCHAR, joined_at TIMESTAMP)
    LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.name, u.email, u.joined_at
    FROM users u
    WHERE u.id = _id;
END;
$$;

CREATE OR REPLACE FUNCTION st_select_user_by_email(_email VARCHAR)
    RETURNS TABLE (id VARCHAR, name VARCHAR, email VARCHAR, joined_at TIMESTAMP)
    LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT u.id, u.name, u.email, u.joined_at
    FROM users u
    WHERE LOWER(u.email) = LOWER(TRIM(_email));
END;
$$;

CREATE OR REPLACE FUNCTION st_insert_user(_id VARCHAR, _name VARCHAR, _email VARCHAR, _joined_at TIMESTAMP)
    RETURNS INTEGER
    LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO users (id, name, email, joined_at)
    VALUES (_id, _name, LOWER(TRIM(_email)), _joined_at);
    RETURN 1;
END;
$$;

CREATE OR REPLACE FUNCTION st_select_categories()
    RETURNS TABLE (id VARCHAR, name VARCHAR, co2e_factor_per_kg NUMERIC, is_compostable BOOLEAN)
    LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT c.id, c.name, c.co2e_factor_per_kg, c.is_compostable
    FROM waste_categories c
    ORDER BY c.is_compostable DESC, c.name;
END;
$$;

CREATE OR REPLACE FUNCTION st_select_batches(_user_id VARCHAR DEFAULT NULL)
    RETURNS TABLE (id VARCHAR, user_id VARCHAR, start_date TIMESTAMP, status VARCHAR,
                    brown_green_ratio NUMERIC, estimated_maturity_date TIMESTAMP)
    LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT b.id, b.user_id, b.start_date, b.status, b.brown_green_ratio, b.estimated_maturity_date
    FROM compost_batches b
    WHERE _user_id IS NULL OR b.user_id = _user_id
    ORDER BY b.start_date;
END;
$$;

CREATE OR REPLACE FUNCTION st_select_waste_items(_batch_id VARCHAR)
    RETURNS TABLE (id VARCHAR, item_type VARCHAR, name VARCHAR, weight NUMERIC, date_added TIMESTAMP,
                    brown_green_type VARCHAR, moisture_content NUMERIC, reason VARCHAR,
                    category_id VARCHAR, category_name VARCHAR, co2e_factor_per_kg NUMERIC,
                    is_compostable BOOLEAN)
    LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT w.id, w.item_type, w.name, w.weight, w.date_added,
            w.brown_green_type, w.moisture_content, w.reason,
            c.id, c.name, c.co2e_factor_per_kg, c.is_compostable
    FROM waste_items w
    LEFT JOIN waste_categories c ON c.id = w.category_id
    WHERE w.batch_id = _batch_id
    ORDER BY w.date_added;
END;
$$;

CREATE OR REPLACE FUNCTION st_select_co2e_timeline(_user_id VARCHAR)
    RETURNS TABLE (tanggal DATE, co2e_harian NUMERIC, co2e_akumulatif NUMERIC)
    LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    WITH daily AS (
        SELECT
            DATE(w.date_added) AS dt,
            SUM(
                w.weight * CASE
                    WHEN COALESCE(c.co2e_factor_per_kg, 0) <= 0 THEN 0.5
                    ELSE c.co2e_factor_per_kg
                END
            ) AS daily_co2e
        FROM waste_items w
        INNER JOIN compost_batches b ON b.id = w.batch_id
        LEFT JOIN waste_categories c ON c.id = w.category_id
        WHERE b.user_id = _user_id
            AND w.item_type = 'compostable'
        GROUP BY DATE(w.date_added)
    )
    SELECT
        d.dt AS tanggal,
        d.daily_co2e AS co2e_harian,
        SUM(d.daily_co2e) OVER (ORDER BY d.dt) AS co2e_akumulatif
    FROM daily d
    ORDER BY d.dt;
END;
$$;

CREATE OR REPLACE FUNCTION st_select_schedules(_batch_id VARCHAR)
    RETURNS TABLE (id VARCHAR, task_type VARCHAR, due_date TIMESTAMP, is_completed BOOLEAN)
    LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT s.id, s.task_type, s.due_date, s.is_completed
    FROM maintenance_schedules s
    WHERE s.batch_id = _batch_id
    ORDER BY s.due_date;
END;
$$;

CREATE OR REPLACE FUNCTION st_insert_schedule(_id VARCHAR, _batch_id VARCHAR, _task_type VARCHAR,
                                            _due_date TIMESTAMP, _is_completed BOOLEAN DEFAULT FALSE)
    RETURNS INTEGER
    LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO maintenance_schedules (id, batch_id, task_type, due_date, is_completed)
    VALUES (_id, _batch_id, _task_type, _due_date, _is_completed);
    RETURN 1;
END;
$$;

CREATE OR REPLACE FUNCTION st_complete_schedule(_id VARCHAR)
    RETURNS INTEGER
    LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE maintenance_schedules
    SET is_completed = TRUE
    WHERE id = _id;

    IF FOUND THEN
        RETURN 1;
    ELSE
        RETURN 0;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION st_delete_schedule(_id VARCHAR)
    RETURNS INTEGER
    LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM maintenance_schedules WHERE id = _id;

    IF FOUND THEN
        RETURN 1;
    ELSE
        RETURN 0;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION st_select_schedules_by_batch(_batch_id VARCHAR)
    RETURNS TABLE (id VARCHAR, batch_id VARCHAR, task_type VARCHAR, due_date TIMESTAMP, is_completed BOOLEAN)
    LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT s.id, s.batch_id, s.task_type, s.due_date, s.is_completed
    FROM maintenance_schedules s
    WHERE s.batch_id = _batch_id
    ORDER BY s.due_date;
END;
$$;

CREATE OR REPLACE FUNCTION st_insert_recommendation(_id VARCHAR, _batch_id VARCHAR, _message VARCHAR,
                                                    _priority VARCHAR, _created_at TIMESTAMP)
    RETURNS INTEGER
    LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO compost_recommendations (id, batch_id, message, priority, created_at)
    VALUES (_id, _batch_id, _message, _priority, _created_at);
    RETURN 1;
END;
$$;

CREATE OR REPLACE FUNCTION st_select_recommendations_by_batch(_batch_id VARCHAR)
    RETURNS TABLE (id VARCHAR, batch_id VARCHAR, message VARCHAR, priority VARCHAR, created_at TIMESTAMP)
    LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT r.id, r.batch_id, r.message, r.priority, r.created_at
    FROM compost_recommendations r
    WHERE r.batch_id = _batch_id
    ORDER BY r.created_at DESC;
END;
$$;

CREATE OR REPLACE FUNCTION st_insert_batch(_id VARCHAR, _user_id VARCHAR, _start_date TIMESTAMP,
                                        _status VARCHAR, _brown_green_ratio NUMERIC,
                                        _estimated_maturity_date TIMESTAMP)
    RETURNS INTEGER
    LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO compost_batches (id, user_id, start_date, status, brown_green_ratio, estimated_maturity_date)
    VALUES (_id, _user_id, _start_date, _status, _brown_green_ratio, _estimated_maturity_date);
    RETURN 1;
END;
$$;

CREATE OR REPLACE FUNCTION st_insert_waste_item(_id VARCHAR, _batch_id VARCHAR, _category_id VARCHAR,
                                                _item_type VARCHAR, _name VARCHAR, _weight NUMERIC,
                                                _date_added TIMESTAMP, _brown_green_type VARCHAR,
                                                _moisture_content NUMERIC, _reason VARCHAR)
    RETURNS INTEGER
    LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO waste_items (id, batch_id, category_id, item_type, name, weight, date_added,
                            brown_green_type, moisture_content, reason)
    VALUES (_id, _batch_id, _category_id, _item_type, _name, _weight, _date_added,
            _brown_green_type, _moisture_content, _reason);
    RETURN 1;
END;
$$;

CREATE OR REPLACE FUNCTION st_update_batch(_id VARCHAR, _status VARCHAR, _brown_green_ratio NUMERIC,
                                        _estimated_maturity_date TIMESTAMP)
    RETURNS INTEGER
    LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE compost_batches
    SET status = _status,
        brown_green_ratio = _brown_green_ratio,
        estimated_maturity_date = _estimated_maturity_date
    WHERE id = _id;

    IF FOUND THEN
        UPDATE maintenance_schedules
        SET due_date = _estimated_maturity_date
        WHERE batch_id = _id
            AND task_type = 'Harvest'
            AND is_completed = FALSE;

        RETURN 1;
    ELSE
        RETURN 0;
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION st_delete_batch(_id VARCHAR)
    RETURNS INTEGER
    LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM compost_batches WHERE id = _id;

    IF FOUND THEN
        RETURN 1;
    ELSE
        RETURN 0;
    END IF;
END;
$$;

INSERT INTO users (id, name, email, joined_at) VALUES
    ('3f1c2a9e-5b7d-4c1e-9a2f-000000000001', 'Nabil Aufa Danaputra', 'nabil@composta.id', NOW() - INTERVAL '90 days'),
    ('3f1c2a9e-5b7d-4c1e-9a2f-000000000002', 'Pengguna Demo', 'demo@composta.id', NOW() - INTERVAL '30 days');

INSERT INTO waste_categories (id, name, co2e_factor_per_kg, is_compostable) VALUES
    ('7a2e4b10-1c3d-4e5f-8a9b-000000000001', 'Sisa Sayur & Buah',      0.500, TRUE),
    ('7a2e4b10-1c3d-4e5f-8a9b-000000000002', 'Daun Kering & Ranting',  0.300, TRUE),
    ('7a2e4b10-1c3d-4e5f-8a9b-000000000003', 'Kardus & Kertas',        0.400, TRUE),
    ('7a2e4b10-1c3d-4e5f-8a9b-000000000004', 'Ampas Kopi & Teh',       0.450, TRUE),
    ('7a2e4b10-1c3d-4e5f-8a9b-000000000005', 'Plastik',                0.000, FALSE),
    ('7a2e4b10-1c3d-4e5f-8a9b-000000000006', 'Kaca & Logam',           0.000, FALSE);

INSERT INTO compost_batches (id, user_id, start_date, status, brown_green_ratio, estimated_maturity_date) VALUES
    ('b8d0e6f2-4a1b-4c2d-9e3f-000000000005', '3f1c2a9e-5b7d-4c1e-9a2f-000000000001',
     NOW() - INTERVAL '85 days', 'Archived', 2.00, NOW() - INTERVAL '25 days'),
    ('b8d0e6f2-4a1b-4c2d-9e3f-000000000001', '3f1c2a9e-5b7d-4c1e-9a2f-000000000001',
     NOW() - INTERVAL '65 days', 'Ready',  2.00, NOW() - INTERVAL '5 days'),
    ('b8d0e6f2-4a1b-4c2d-9e3f-000000000002', '3f1c2a9e-5b7d-4c1e-9a2f-000000000001',
     NOW() - INTERVAL '40 days', 'Curing', 2.00, NOW() + INTERVAL '20 days'),
    ('b8d0e6f2-4a1b-4c2d-9e3f-000000000003', '3f1c2a9e-5b7d-4c1e-9a2f-000000000001',
     NOW() - INTERVAL '10 days', 'Active', 0.60, NOW() + INTERVAL '50 days'),
    ('b8d0e6f2-4a1b-4c2d-9e3f-000000000004', '3f1c2a9e-5b7d-4c1e-9a2f-000000000002',
     NOW() - INTERVAL '5 days', 'Active', 1.00, NOW() + INTERVAL '55 days');

INSERT INTO waste_items (id, batch_id, category_id, item_type, name, weight, date_added,
                         brown_green_type, moisture_content, reason) VALUES
    ('c4e6a8b0-2d3f-4a5b-8c7d-000000000010', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000005',
     '7a2e4b10-1c3d-4e5f-8a9b-000000000002', 'compostable', 'Daun kering taman', 4.00,
     NOW() - INTERVAL '85 days', 'Brown', 30, NULL),
    ('c4e6a8b0-2d3f-4a5b-8c7d-000000000011', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000005',
     '7a2e4b10-1c3d-4e5f-8a9b-000000000001', 'compostable', 'Sisa sayur pasar', 2.00,
     NOW() - INTERVAL '84 days', 'Green', 70, NULL),
    ('c4e6a8b0-2d3f-4a5b-8c7d-000000000001', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000001',
     '7a2e4b10-1c3d-4e5f-8a9b-000000000002', 'compostable', 'Daun kering', 5.00,
     NOW() - INTERVAL '65 days', 'Brown', 35, NULL),
    ('c4e6a8b0-2d3f-4a5b-8c7d-000000000002', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000001',
     '7a2e4b10-1c3d-4e5f-8a9b-000000000001', 'compostable', 'Sisa sayur', 2.50,
     NOW() - INTERVAL '64 days', 'Green', 65, NULL),
    ('c4e6a8b0-2d3f-4a5b-8c7d-000000000003', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000002',
     '7a2e4b10-1c3d-4e5f-8a9b-000000000002', 'compostable', 'Daun kering halaman', 6.00,
     NOW() - INTERVAL '40 days', 'Brown', 30, NULL),
    ('c4e6a8b0-2d3f-4a5b-8c7d-000000000004', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000002',
     '7a2e4b10-1c3d-4e5f-8a9b-000000000001', 'compostable', 'Sisa sayur dapur', 3.00,
     NOW() - INTERVAL '39 days', 'Green', 70, NULL),
    ('c4e6a8b0-2d3f-4a5b-8c7d-000000000005', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000003',
     '7a2e4b10-1c3d-4e5f-8a9b-000000000001', 'compostable', 'Kulit buah', 4.00,
     NOW() - INTERVAL '10 days', 'Green', 80, NULL),
    ('c4e6a8b0-2d3f-4a5b-8c7d-000000000006', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000003',
     '7a2e4b10-1c3d-4e5f-8a9b-000000000003', 'compostable', 'Kardus bekas', 3.00,
     NOW() - INTERVAL '9 days', 'Brown', 20, NULL),
    ('c4e6a8b0-2d3f-4a5b-8c7d-000000000007', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000003',
     '7a2e4b10-1c3d-4e5f-8a9b-000000000004', 'compostable', 'Ampas kopi', 1.00,
     NOW() - INTERVAL '8 days', 'Green', 60, NULL),
    ('c4e6a8b0-2d3f-4a5b-8c7d-000000000008', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000004',
     '7a2e4b10-1c3d-4e5f-8a9b-000000000001', 'compostable', 'Sisa sayur', 2.00,
     NOW() - INTERVAL '5 days', 'Green', 70, NULL),
    ('c4e6a8b0-2d3f-4a5b-8c7d-000000000009', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000004',
     '7a2e4b10-1c3d-4e5f-8a9b-000000000002', 'compostable', 'Daun kering', 2.00,
     NOW() - INTERVAL '4 days', 'Brown', 30, NULL);

INSERT INTO maintenance_schedules (id, batch_id, task_type, due_date, is_completed) VALUES
    ('d2f4b6c8-3e5a-4b7c-9d1e-000000000001', 'b8d0e6f2-4a1b-4c2d-9e3f-000000000002',
     'Turn', NOW() - INTERVAL '2 days', FALSE);
