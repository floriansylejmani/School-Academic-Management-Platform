# Deployment Guide

## Prerequisites

- [Docker 24+](https://docs.docker.com/get-docker/) with Compose plugin (`docker compose`)
- A `.env` file in the project root (see [Environment Variables](#environment-variables) below)

---

## Quick Start

```bash
# 1. Copy and configure environment
cp .env.example .env
# Edit .env — at minimum set JWT_SECRET_KEY, POSTGRES_PASSWORD

# 2. Build and start all services
docker compose up --build -d

# 3. Verify health
curl http://localhost:5000/health
# Expected: {"status":"ok"}
```

| Service    | URL                                        |
|------------|--------------------------------------------|
| Frontend   | http://localhost:3000                      |
| API        | http://localhost:5000                      |
| Health     | http://localhost:5000/health               |
| Swagger    | http://localhost:5000/swagger *(Dev only)* |
| PostgreSQL | localhost:5432                             |

---

## Services Overview

### `db` — PostgreSQL 16
- Starts first; exposes port 5432
- Data persisted in Docker volume `postgres_data`
- Health-checked with `pg_isready`

### `api` — ASP.NET Core 9
- Waits for `db` to be healthy before starting
- Runs on internal port 8080 (mapped to host port 5000 by default)
- Applies EF Core migrations automatically (`Database__AutoMigrate=true`)
- Seeds demo data on first run (`Database__SeedDemoData=true`)
- Data Protection keys persisted in Docker volume `api_data_protection`
- Health-checked via `GET /health`

### `frontend` — Next.js 15
- Waits for `api` to be healthy before starting
- Runs on port 3000
- `NEXT_PUBLIC_API_URL` is **baked in at build time** — must be set as a build `ARG`, not a runtime env var

---

## Environment Variables

Copy `.env.example` to `.env`. Required values to change before production:

| Variable | Default | Notes |
|---|---|---|
| `POSTGRES_DB` | `school_management` | Database name |
| `POSTGRES_USER` | `postgres` | Database user |
| `POSTGRES_PASSWORD` | `postgres` | **Change in production** |
| `JWT_SECRET_KEY` | `ChangeMeForDemoOnly_AtLeast32Characters!` | **Must be changed — min 32 chars** |
| `JWT_ACCESS_TOKEN_EXPIRY_MINUTES` | `60` | Access token lifetime |
| `JWT_REFRESH_TOKEN_EXPIRY_DAYS` | `7` | Refresh token lifetime |
| `FRONTEND_ORIGIN` | `http://localhost:3000` | API CORS allowed origin |
| `NEXT_PUBLIC_API_URL` | `http://localhost:5000/api` | API base URL — baked into frontend at build time |
| `DATABASE_AUTO_MIGRATE` | `true` | Apply EF migrations on startup |
| `DATABASE_SEED_DEMO_DATA` | `true` | Seed roles and default admin account |
| `PASSWORD_RESET_TOKEN_EXPIRY_MINUTES` | `30` | Password reset link validity |
| `PASSWORD_RESET_FRONTEND_URL` | `http://localhost:3000/reset-password` | URL embedded in reset emails |
| `API_PORT` | `5000` | Host port mapped to the API container |
| `FRONTEND_PORT` | `3000` | Host port mapped to the frontend container |
| `POSTGRES_PORT` | `5432` | Host port mapped to PostgreSQL |

---

## Production Checklist

- [ ] Set a strong random `JWT_SECRET_KEY` (minimum 32 characters, ideally 64+)
- [ ] Change `POSTGRES_USER` and `POSTGRES_PASSWORD`
- [ ] Set `FRONTEND_ORIGIN` to your public frontend URL (`https://yourapp.com`)
- [ ] Set `NEXT_PUBLIC_API_URL` to your public API URL (`https://api.yourapp.com/api`)
- [ ] Set `PASSWORD_RESET_FRONTEND_URL` to `https://yourapp.com/reset-password`
- [ ] Set `DATABASE_SEED_DEMO_DATA=false` (disable demo admin seeding)
- [ ] Set `DATABASE_AUTO_MIGRATE=false` once initial migrations are applied
- [ ] Serve both services behind an HTTPS reverse proxy (nginx, Traefik, Caddy)
- [ ] Mount or configure Data Protection key ring encryption
- [ ] Restrict PostgreSQL port (remove the `ports` mapping for `db` in production)

---

## Common Commands

```bash
# Start stack in foreground (see all logs)
docker compose up --build

# Start stack in background
docker compose up --build -d

# View API logs
docker compose logs -f api

# View frontend logs
docker compose logs -f frontend

# Stop stack
docker compose down

# Stop stack and wipe all data (volumes)
docker compose down -v

# Restart a single service
docker compose restart api

# Rebuild and restart a single service
docker compose up --build -d api
```

---

## Health Verification

After starting the stack, verify each tier:

```bash
# 1. Database — should show "accepting connections"
docker compose exec db pg_isready -U postgres -d school_management

# 2. API
curl http://localhost:5000/health
# Expected: {"status":"ok"}

# 3. Frontend — should return 200
curl -o /dev/null -s -w "%{http_code}" http://localhost:3000
```

---

## Demo Credentials

When `DATABASE_SEED_DEMO_DATA=true` (the default), the following admin account is created:

```
Email:    admin@school.com
Password: Admin@12345
Role:     Admin
```

Use the admin panel to create Teacher, Parent, and Student accounts.

---

## Logging

- **Development:** Console (Debug level) + rolling file sink at `logs/school-management-dev-*.log`
- **Production (Docker):** Console only — structured JSON lines collected by Docker's log driver. File sinks are disabled because the container runs as a non-root user without write access to `/app`.

To forward logs to an external system in production, configure Docker's log driver:

```yaml
# docker-compose.yml (add to api service)
logging:
  driver: "json-file"
  options:
    max-size: "10m"
    max-file: "5"
```

---

## Ports Reference

| Container | Internal Port | Host Port (default) |
|---|---|---|
| `school_api` | 8080 | 5000 |
| `school_frontend` | 3000 | 3000 |
| `school_db` | 5432 | 5432 |
