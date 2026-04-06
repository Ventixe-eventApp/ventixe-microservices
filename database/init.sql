-- Ventixe PostgreSQL Database Initialization
-- This script creates the complete database schema for Ventixe
-- It includes tables for events, users, bookings, and AI chat functionality

-- ============================================
-- ENABLE EXTENSIONS
-- ============================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pg_cron;


-- ============================================
-- EXISTING TABLES (Services Data)
-- ============================================

-- Events Table (from event-service)
CREATE TABLE IF NOT EXISTS events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_name VARCHAR(255) NOT NULL,
    artist_name VARCHAR(255),
    description TEXT,
    location VARCHAR(255) NOT NULL,
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP,
    price DECIMAL(10, 2),
    available_tickets INT DEFAULT 0,
    category VARCHAR(100),
    image_url VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_events_artist ON events(artist_name);
CREATE INDEX IF NOT EXISTS idx_events_location ON events(location);
CREATE INDEX IF NOT EXISTS idx_events_date ON events(start_date);
CREATE INDEX IF NOT EXISTS idx_events_category ON events(category);

-- Users Table (from user-service)
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) UNIQUE NOT NULL,
    name VARCHAR(255),
    phone VARCHAR(20),
    country VARCHAR(100),
    preferences JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_users_email ON users(email);

-- Accounts Table (from account-service)
CREATE TABLE IF NOT EXISTS accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID UNIQUE NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    username VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    is_verified BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_accounts_username ON accounts(username);
CREATE INDEX IF NOT EXISTS idx_accounts_user_id ON accounts(user_id);

-- Auth Users Table (from auth-service)
CREATE TABLE IF NOT EXISTS auth_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    last_login TIMESTAMP,
    login_count INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_auth_users_user_id ON auth_users(user_id);

-- Refresh Tokens Table (from auth-service)
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    is_revoked BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_expires_at ON refresh_tokens(expires_at);

-- Bookings Table (from booking-service)
CREATE TABLE IF NOT EXISTS bookings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    tickets_count INT NOT NULL CHECK (tickets_count > 0),
    total_price DECIMAL(10, 2) NOT NULL,
    status VARCHAR(50) DEFAULT 'confirmed',
    booking_reference VARCHAR(20) UNIQUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_bookings_user ON bookings(user_id);
CREATE INDEX IF NOT EXISTS idx_bookings_event ON bookings(event_id);
CREATE INDEX IF NOT EXISTS idx_bookings_status ON bookings(status);

-- Verification Codes Table (from verification-service)
CREATE TABLE IF NOT EXISTS verification_codes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    code VARCHAR(10) NOT NULL,
    type VARCHAR(50),
    is_used BOOLEAN DEFAULT FALSE,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_verification_codes_user_id ON verification_codes(user_id);
CREATE INDEX IF NOT EXISTS idx_verification_codes_expires_at ON verification_codes(expires_at);

-- Verification Attempts Table (from verification-service)
CREATE TABLE IF NOT EXISTS verification_attempts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    attempt_count INT DEFAULT 1,
    last_attempt_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    locked_until TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_verification_attempts_user_id ON verification_attempts(user_id);


-- ============================================
-- NEW TABLES FOR AI CHAT AGENT
-- ============================================

-- Conversations Table (AI Chat Sessions)
CREATE TABLE IF NOT EXISTS conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    title VARCHAR(255),
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ended_at TIMESTAMP,
    last_message_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    message_count INT DEFAULT 0,
    is_archived BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_conversations_user_id ON conversations(user_id);
CREATE INDEX IF NOT EXISTS idx_conversations_created_at ON conversations(created_at);
CREATE INDEX IF NOT EXISTS idx_conversations_active ON conversations(user_id) WHERE ended_at IS NULL;

-- Conversation Messages Table (Chat History)
CREATE TABLE IF NOT EXISTS conversation_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID NOT NULL REFERENCES conversations(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL,
    content TEXT NOT NULL,
    tool_calls JSONB,
    search_filters JSONB,
    suggested_events JSONB,
    metadata JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT valid_role CHECK (role IN ('user', 'assistant', 'system'))
);

CREATE INDEX IF NOT EXISTS idx_messages_conversation ON conversation_messages(conversation_id);
CREATE INDEX IF NOT EXISTS idx_messages_created_at ON conversation_messages(created_at);
CREATE INDEX IF NOT EXISTS idx_messages_role ON conversation_messages(role);

-- Cleanup Logs Table (Track auto-deletion jobs)
CREATE TABLE IF NOT EXISTS cleanup_logs (
    id SERIAL PRIMARY KEY,
    job_name VARCHAR(255),
    deleted_rows INT,
    executed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


-- ============================================
-- TTL / AUTO-DELETE JOBS
-- ============================================

-- Schedule deletion of conversations older than 30 days (runs daily at 2 AM UTC)
SELECT cron.schedule('cleanup_old_conversations', '0 2 * * *', $$
    INSERT INTO cleanup_logs (job_name, deleted_rows)
    SELECT 'cleanup_old_conversations', COUNT(*)
    FROM conversations 
    WHERE created_at < NOW() - INTERVAL '30 days'
    AND ended_at IS NOT NULL;
    
    DELETE FROM conversations 
    WHERE created_at < NOW() - INTERVAL '30 days'
    AND ended_at IS NOT NULL;
$$);

-- Schedule deletion of orphaned verification codes (runs daily at 3 AM UTC)
SELECT cron.schedule('cleanup_expired_verification_codes', '0 3 * * *', $$
    DELETE FROM verification_codes 
    WHERE expires_at < NOW();
$$);

-- Schedule deletion of revoked refresh tokens (runs daily at 4 AM UTC)
SELECT cron.schedule('cleanup_expired_refresh_tokens', '0 4 * * *', $$
    DELETE FROM refresh_tokens 
    WHERE expires_at < NOW()
    OR is_revoked = TRUE;
$$);


-- ============================================
-- VIEWS (OPTIONAL - For Analytics)
-- ============================================

-- View: Recent conversations with user info
CREATE OR REPLACE VIEW v_recent_conversations AS
SELECT 
    c.id,
    c.user_id,
    u.email,
    u.name,
    c.title,
    c.message_count,
    c.started_at,
    c.last_message_at,
    c.ended_at,
    (c.message_count / NULLIF(EXTRACT(EPOCH FROM (COALESCE(c.ended_at, NOW()) - c.started_at)) / 60, 0))::DECIMAL(10,2) AS avg_messages_per_minute
FROM conversations c
LEFT JOIN users u ON c.user_id = u.id
ORDER BY c.last_message_at DESC;

-- View: Events with booking count
CREATE OR REPLACE VIEW v_events_with_stats AS
SELECT 
    e.id,
    e.event_name,
    e.artist_name,
    e.location,
    e.start_date,
    e.price,
    COUNT(b.id) AS total_bookings,
    SUM(b.tickets_count) AS total_tickets_sold,
    COALESCE(e.available_tickets - SUM(b.tickets_count), e.available_tickets) AS remaining_tickets
FROM events e
LEFT JOIN bookings b ON e.id = b.event_id AND b.status = 'confirmed'
GROUP BY e.id, e.event_name, e.artist_name, e.location, e.start_date, e.price, e.available_tickets
ORDER BY e.start_date DESC;

-- View: Chat conversation summary
CREATE OR REPLACE VIEW v_conversation_summary AS
SELECT 
    c.id AS conversation_id,
    u.email AS user_email,
    COUNT(cm.id) AS total_messages,
    COUNT(CASE WHEN cm.role = 'user' THEN 1 END) AS user_messages,
    COUNT(CASE WHEN cm.role = 'assistant' THEN 1 END) AS assistant_messages,
    COUNT(DISTINCT cm.suggested_events) AS events_suggested,
    c.started_at,
    c.ended_at,
    EXTRACT(EPOCH FROM (COALESCE(c.ended_at, NOW()) - c.started_at))::INT AS duration_seconds
FROM conversations c
LEFT JOIN users u ON c.user_id = u.id
LEFT JOIN conversation_messages cm ON c.id = cm.conversation_id
GROUP BY c.id, u.email, c.started_at, c.ended_at;


-- ============================================
-- SAMPLE DATA (OPTIONAL - For Testing)
-- ============================================

-- Sample Events
INSERT INTO events (event_name, artist_name, description, location, start_date, end_date, price, available_tickets, category)
VALUES 
    ('Jazz Night 2026', 'Miles Davis Tribute', 'An evening of classic jazz', 'New York, NY', '2026-05-15 20:00:00', '2026-05-15 23:00:00', 50.00, 100, 'Jazz'),
    ('Rock Festival NYC', 'The Rolling Stones Experience', 'Ultimate rock experience', 'New York, NY', '2026-06-20 18:00:00', '2026-06-20 23:00:00', 75.00, 500, 'Rock'),
    ('Classical Symphony', 'New York Philharmonic', 'Beethoven & Mozart', 'New York, NY', '2026-05-25 19:30:00', '2026-05-25 22:00:00', 60.00, 200, 'Classical'),
    ('Electronic Music Festival', 'DJ Electro', 'Modern electronic beats', 'Los Angeles, CA', '2026-05-30 22:00:00', '2026-05-31 06:00:00', 45.00, 300, 'Electronic'),
    ('Country Music Concert', 'Nashville Stars', 'Country hits all night', 'Nashville, TN', '2026-06-10 19:00:00', '2026-06-10 22:30:00', 40.00, 250, 'Country')
ON CONFLICT DO NOTHING;

-- Sample Users
INSERT INTO users (email, name, phone, country)
VALUES 
    ('john@example.com', 'John Doe', '+1-555-0101', 'USA'),
    ('jane@example.com', 'Jane Smith', '+1-555-0102', 'USA'),
    ('bob@example.com', 'Bob Johnson', '+1-555-0103', 'USA')
ON CONFLICT DO NOTHING;


-- ============================================
-- PERMISSIONS / SECURITY
-- ============================================

-- Create application user (limited permissions)
-- Uncomment and modify as needed for your deployment
-- CREATE USER ventixe_app WITH PASSWORD 'strong_password';
-- GRANT CONNECT ON DATABASE ventixe_main TO ventixe_app;
-- GRANT USAGE ON SCHEMA public TO ventixe_app;
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO ventixe_app;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO ventixe_app;

-- ============================================
-- INITIAL SETUP COMPLETE
-- ============================================
-- Database schema is ready!
-- Tables: 12 main tables + 3 views
-- Auto-cleanup jobs: Enabled (3 jobs)
-- Sample data: Loaded

COMMIT;
