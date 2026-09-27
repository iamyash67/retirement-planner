# Retirement Planner

A web app for planning retirement savings. Users log in, set a retirement goal (current age,
retirement age, target amount and current savings), record monthly investments, and track how
close they are to the target.

When a goal is created, the app works out the monthly contribution needed to close the gap by
the retirement age. Each recorded investment is added to current savings, and progress is shown
as a percentage of the target. Each month can be recorded only once.

## Architecture

```
Angular SPA  ──HTTP/JSON──▶  ASP.NET Core Web API  ──Dapper──▶  MySQL (schema via DbUp migrations)
```

The API is split into layers, each with one responsibility:

| Layer | Folder | Responsibility |
|---|---|---|
| Controllers | `Controllers/` | Translate HTTP to service calls and results to status codes; no business logic |
| Validators | `Validators/` | One FluentValidation validator per request DTO, run before every action |
| Mapping | `Mapping/` | Explicit extension methods between DTOs and domain models |
| Services | `Services/` | Business logic; coordinates repositories |
| Repositories | `Repositories/` | Data access with Dapper and parameterised SQL |
| Data | `Data/` | Connection factory, unit of work, migrator and development seeder |
| DTOs | `DTO/Requests`, `DTO/Responses` | The API contract: one request and one response type per endpoint |
| Models | `Models/` | Domain entities and commands used by services and repositories; never returned by the API |

Services and repositories are defined by interfaces (`Services/Interfaces`, `Repositories/Interfaces`)
and registered with dependency injection in `Program.cs`, so each layer depends on abstractions
rather than concrete classes.

All repositories in a request share one connection and, when a service needs it, one transaction,
through a scoped `IUnitOfWork`.

Every request DTO has a FluentValidation validator. A global action filter runs it before the action,
and failures return **400 ValidationProblemDetails** with one entry per field, keyed by the JSON field
name (`errors.retirementAge`). The Angular forms show these messages under the matching inputs.
Not-found (404), conflict (409) and bad-login (401) responses stay plain strings. Only `IDbConnectionFactory` creates connections. Unhandled exceptions
are returned as RFC 7807 ProblemDetails by a global exception handler. See
[docs/database.md](docs/database.md) for the schema, an ER diagram and the design decisions.

The Angular frontend uses standalone components (login, profile, goals, dashboard header) and
services that call the API, with a route guard protecting the dashboard pages.

## Tech stack

- **Backend:** ASP.NET Core 10 Web API, Dapper, MySqlConnector, DbUp, Swagger (Swashbuckle)
- **Frontend:** Angular 18, Angular Material, RxJS
- **Database:** MySQL 8.4 in Docker; schema managed by versioned DbUp migrations
- **Tests:** xUnit, Moq, Testcontainers (MySQL)
- **Tooling:** .NET 10 SDK (pinned in `backend/global.json`), Node 20 (`frontend/.nvmrc`), Docker Compose

## Project structure

```
backend/
  RetirementPlanner.sln
  global.json                   .NET SDK pin
  RetirementPlanner.Api/
    Controllers/                User, Goal and FinancialData endpoints
    Services/                   Business logic (+ Interfaces/)
    Repositories/               Data access with Dapper (+ Interfaces/)
    Data/                       Connection factory, unit of work, migrator, dev seeder (+ Interfaces/)
    Migrations/                 Versioned SQL migrations (V001__..., embedded, run on startup)
    Infrastructure/             Global exception handler (ProblemDetails)
    Validators/                 FluentValidation validators, one per request DTO
    Mapping/                    DTO <-> domain mapping extension methods
    DTO/                        Requests/ and Responses/ (the API contract)
    Models/                     Domain models
    Program.cs                  DI registration, migrations, CORS, Swagger
  RetirementPlanner.Tests/
    Services/ Infrastructure/   Unit tests with mocked repositories
    Integration/                Repository and migration tests against MySQL in Testcontainers
docker/mysql-init/              Creates the empty database on first container start
docs/database.md                Schema, ER diagram and design decisions
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

On first start the container creates the empty `retirement_planner` database and the
`retirement_planner_app` user from `.env`. The API creates the tables itself: it applies any pending
migrations from `Migrations/` on startup. The data lives in the `mysql-data` volume. To start from an
empty database: `docker compose down -v && docker compose up -d`.

Compose publishes the container's port 3306 on host port **3307**, so it does not clash with a
local MySQL server on 3306.

### 2. Backend (http://localhost:5294, Swagger at `/swagger`)

```bash
cd backend/RetirementPlanner.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost;Port=3307;Database=retirement_planner;User=retirement_planner_app;Password=<MYSQL_PASSWORD from .env>;"
dotnet run --launch-profile http
```

In the Development environment the API also seeds a demo user on startup.

### 3. Frontend (http://localhost:4200)

```bash
cd frontend
nvm use
npm install
npm start
```

Log in with `demo@example.com` / `demo123`.

### Tests

```bash
cd backend
dotnet test    # unit tests, plus integration tests that start MySQL with Testcontainers (Docker must be running)
```

## API

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/user/login` | Validate email and password, return profile |
| GET | `/api/goal/{profileId}` | Get the profile's goal |
| POST | `/api/goal` | Create a goal (one per profile); 201 with the goal |
| POST | `/api/financial/Add-Investment` | Record a month's investment (409 if already recorded) |
| GET | `/api/financial/progress/{goalId}` | Percentage of target saved |
