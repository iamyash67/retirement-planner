# Retirement Planner

A web app for planning retirement savings. Users log in, set a retirement goal (current age,
retirement age, target amount and current savings), record monthly investments, and track how
close they are to the target.

When a goal is created, the app works out the monthly contribution needed to close the gap by
the retirement age. Each recorded investment is added to current savings, and progress is shown
as a percentage of the target. Each month can be recorded only once.

## Architecture

```
Angular SPA  ──HTTP/JSON──▶  ASP.NET Core Web API  ──ADO.NET──▶  MySQL (stored procedures)
```

The API is split into layers, each with one responsibility:

| Layer | Folder | Responsibility |
|---|---|---|
| Controllers | `Controllers/` | HTTP endpoints, request validation, status codes |
| Services | `Services/` | Business logic; coordinates repositories |
| Repositories | `Repositories/` | Data access through MySQL stored procedures and parameterised queries |
| DTOs | `DTO/` | Request shapes accepted by the API |
| Models | `Models/` | Domain entities returned by services and repositories |

Services and repositories are defined by interfaces (`Services/Interfaces`, `Repositories/Interfaces`)
and registered with dependency injection in `Program.cs`, so each layer depends on abstractions
rather than concrete classes.

The Angular frontend uses standalone components (login, profile, goals, dashboard header) and
services that call the API, with a route guard protecting the dashboard pages.

## Tech stack

- **Backend:** ASP.NET Core 10 Web API, MySql.Data (ADO.NET), Swagger (Swashbuckle)
- **Frontend:** Angular 18, Angular Material, RxJS
- **Database:** MySQL 8.4 in Docker, with schema, stored procedures and seed data as SQL scripts
- **Tooling:** .NET 10 SDK (pinned in `backend/global.json`), Node 20 (`frontend/.nvmrc`), Docker Compose

## Project structure

```
backend/
  RetirementPlanner.sln
  global.json                   .NET SDK pin
  RetirementPlanner.Api/
    Controllers/                User, Goal and FinancialData endpoints
    Services/                   Business logic (+ Interfaces/)
    Repositories/               Data access (+ Interfaces/)
    DTO/                        Request models
    Models/                     Domain models
    Program.cs                  DI registration, CORS, Swagger
database/
  01_schema.sql                 Tables
  02_procedures.sql             Stored procedures
  03_seed.sql                   Demo user
frontend/
  src/app/
    login/ profile/ goals/ dashboard/   Components
    services/                           API clients and auth guard
    models/                             TypeScript interfaces
docker-compose.yml              MySQL service
.env.example                    Template for local database passwords
```

## Setup

Prerequisites: .NET 10 SDK, Node 20, Docker Desktop. You do not need a local MySQL server.

### 1. Database (MySQL 8.4 in Docker, host port 3307)

```bash
cp .env.example .env    # then set real passwords in .env
docker compose up -d
docker compose ps       # wait until the mysql service shows "healthy"
```

On first start the container creates the `retirement_planner` database and the `rpt_app` user
from `.env`, then runs the scripts in `database/` in order. The data lives in the `mysql-data`
volume. To wipe it and re-run the scripts: `docker compose down -v && docker compose up -d`.

Compose publishes the container's port 3306 on host port **3307**, so it does not clash with a
local MySQL server on 3306.

### 2. Backend (http://localhost:5294, Swagger at `/swagger`)

```bash
cd backend/RetirementPlanner.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Port=3307;Database=retirement_planner;User=rpt_app;Password=<MYSQL_PASSWORD from .env>;"
dotnet run --launch-profile http
```

### 3. Frontend (http://localhost:4200)

```bash
cd frontend
nvm use
npm install
npm start
```

Log in with `demo` / `demo123`.

## API

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/user/login` | Validate credentials, return profile |
| GET | `/api/goal/{profileId}` | Get the profile's goal |
| POST | `/api/goal` | Create a goal (one per profile) |
| POST | `/api/financial/Add-Investment` | Record a month's investment (409 if already recorded) |
| GET | `/api/financial/progress/{goalId}` | Percentage of target saved |
