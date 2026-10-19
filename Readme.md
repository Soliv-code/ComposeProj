# 🚀 ComposeProj: ASP.NET Core Minimal API + Docker Compose

Чистый, контейнеризированный RESTful API для управления товарами, построенный на **.NET 10 (Minimal API)** и **Entity Framework Core**. Проект полностью оркестрируется через **Docker Compose** и нативно интегрирован с **Visual Studio**.

## ✨ Основные возможности

- 🛠 **CRUD операции**: Полное управление товарами (создание, чтение, обновление, удаление).
- 🔍 **Продвинутые запросы**: Поддержка пагинации, сортировки (по дате, цене, ID) и фильтрации (поиск по названию, диапазон цен).
- 🐳 **Docker & Compose**: Полная изоляция окружения. База данных и API запускаются одной командой или через F5 в Visual Studio.
- 🐘 **PostgreSQL + pgvector**: Использование современной БД с расширением для векторного поиска (готовность к AI-фичам).
- 🛡 **Конфигурация и безопасность**: Вынос секретов и строк подключения в `.env`.
- ⚕️ **Healthchecks**: Умный запуск API только после полной готовности базы данных.
- 🕒 **Работа со временем**: Автоматическая конвертация UTC в Московское время на уровне API.
- 📝 **Авто-документация**: Встроенный Swagger UI.

##  Технологический стек

- **Backend**: ASP.NET Core 10 (Minimal API), C#
- **ORM**: Entity Framework Core (Npgsql)
- **Database**: PostgreSQL 17 (образ `pgvector/pgvector`)
- **Containerization**: Docker, Docker Compose
- **IDE**: Visual Studio 2026 (Container Orchestration)

##  Структура проекта

    ├── Docker/
    │   └── SQL/                     # Скрипты инициализации и сидинга БД
    │       ├── 01-init.sql          # Создание таблиц и расширений
    │       └── 02-seed.sql          # Тестовые данные (148 товаров)
    ├── SRC/
    │   └── Products.API/            # Основной проект API
    │       ├── Database/            # ApplicationDbContext
    │       ├── Endpoints/           # Minimal API маршруты
    │       ├── Entities/            # Сущности и DTOs
    │       ── Program.cs           # Точка входа и настройка сервисов
    ├── .env                         # Переменные окружения (не коммитится в Git)
    ├── .env.example                 # Пример переменных окружения
    ├── docker-compose.yml           # Основной файл оркестрации
    └── docker-compose.override.yml  # Локальные переопределения для VS

##  Как запустить

### Предварительные требования
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Visual Studio 2026](https://visualstudio.microsoft.com/) (или .NET SDK + Docker CLI)

### Шаг 1: Настройка переменных окружения
Убедитесь, что в корне проекта существует файл `.env.example`, переименуйте его в `.env`. 
Если его нет, создайте его на основе следующего шаблона:
```
# ==========================================
# PostgreSQL Configuration
# ==========================================
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_DB=products_db

# ==========================================
# API Connection String Variables
# (Используются в docker-compose.yml для сборки строки подключения)
# ==========================================
DB_HOST=products_postgres
DB_PORT=5432
DB_NAME=products_db
DB_USER=postgres
DB_PASSWORD=postgres
```

### Шаг 2: Запуск

**Вариант А: Через Visual Studio (Рекомендуется)**
1. Откройте решение `ComposeProj.slnx`.
2. Убедитесь, что проект `docker-compose` назначен запускаемым (Set as Startup Project).
3. Нажмите **F5**. Visual Studio сама соберет образы, запустит контейнеры и откроет Swagger.

**Вариант Б: Через терминал (Docker CLI)**

    docker-compose up -d --build

### Шаг 3: Использование
После запуска API будет доступно по адресу:
👉 **http://localhost:5000/swagger**

База данных PostgreSQL доступна для внешних клиентов (например, DBeaver):
- **Host:** `localhost`
- **Port:** `5432`
- **User:** `postgres`
- **Password:** `postgres`

##  API Endpoints

| Метод | Endpoint | Описание |
|-------|----------|----------|
| `GET` | `/products` | Получить список товаров (с пагинацией, фильтрами и сортировкой) |
| `GET` | `/products/{id}` | Получить товар по UUID |
| `POST` | `/products` | Создать новый товар |
| `PUT` | `/products/{id}` | Обновить существующий товар |
| `DELETE` | `/products/{id}` | Удалить товар |

## 🗺 Планы на будущее (Roadmap)

- [x] Интеграция **Redis** для кэширования с корректной инвалидацией при мутациях (POST/PUT/DELETE).
- [ ] Интеграция **Seq + Serilog** для красивого структурированного логирования.
- [ ] Добавление аутентификации и авторизации через **Keycloak + JWT**.
- [ ] Полнотекстовый поиск с использованием `tsvector` и `pgvector`.
- [ ] Покрытие кода Unit и Integration тестами.

---
*Создано с ❤️ и большим количеством чая (и сигарет) в 3:30 ночи.*