-- SQL/init.sql

-- Расширение под будущий RAG. Ставится один раз, ничего не ломает.
CREATE EXTENSION IF NOT EXISTS vector;

-- Опционально: полезные расширения на будущее
-- CREATE EXTENSION IF NOT EXISTS "uuid-ossp";   -- если не нравится gen_random_uuid()
-- CREATE EXTENSION IF NOT EXISTS pg_trgm;       -- триграммы для LIKE-поиска

-- Сами таблицы лучше создавать через миграции C# (EF Core), 
-- а не здесь. Но если пока MVP и хочешь руками — вот:
CREATE TABLE IF NOT EXISTS products (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(255) NOT NULL,
    price       DECIMAL(10,2) NOT NULL CHECK (price >= 0),
    description TEXT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);