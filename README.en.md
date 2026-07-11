# Hamrah Kolie — Dialysis Patient Support Platform (Technical README)

Modular Monolith on **ASP.NET Core 10 MVC + Razor** with **Vue 3 islands** (not a separate SPA).
Persian, RTL-first. Public pages are server-rendered for SEO; Vue enhances only interactive parts.

## Stack
- ASP.NET Core MVC, EF Core 10, ASP.NET Core Identity (permission-based RBAC)
- PostgreSQL (default) / SQL Server — switch via `Database:Provider`
- Serilog, Hangfire, Health Checks, Output Cache, Rate Limiting
- Vite + Vue 3 + TypeScript islands → built into `wwwroot/dist`

## Projects
`Domain` → `Application` → `Infrastructure` → `Web`, plus `Tests`.

## Run (dev)
```bash
# 1. set DB connection + super admin via env or appsettings
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=hamrahkolie;Username=postgres;Password=..."
export SUPERADMIN_EMAIL=admin@example.com SUPERADMIN_PASSWORD=Strong#123

# 2. build frontend islands
cd src/HamrahKolie.Web/ClientApp && npm install && npm run build && cd -

# 3. run (auto-applies migrations + seed)
dotnet run --project src/HamrahKolie.Web
```

## Docker
```bash
cp .env.example .env   # edit values
docker compose up -d --build
```

## Migrations
```bash
dotnet ef migrations add <Name> \
  --project src/HamrahKolie.Infrastructure \
  --startup-project src/HamrahKolie.Web \
  --output-dir Persistence/Migrations
```

## Notes
- No real secrets in git; use env vars / `.env`.
- Super admin is created from env vars, never a hardcoded password.
- External providers (payment gateway, SMS, maps, analytics) are wired via adapters and disabled until real credentials are supplied.
