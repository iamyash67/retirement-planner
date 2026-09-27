# CLAUDE.md

## Project overview

Retirement Planner: users log in, create one retirement goal (current age, retirement age,
target, current savings), record monthly investments, and track progress toward the target.

- **Backend:** ASP.NET Core 10 Web API in `backend/RetirementPlanner.Api` (Dapper + MySqlConnector, unit of work)
- **Frontend:** Angular 18 (standalone components, Angular Material) in `frontend/`
- **Database:** MySQL 8 (the `mysql:8.4` image via Docker Compose, host port 3307); schema via DbUp migrations in
  `backend/RetirementPlanner.Api/Migrations`, applied on API startup
- **Planned:** Python microservices, added later

See `README.md` for the architecture, setup and API routes.

## Run

```bash
# Database (needs .env; copy .env.example first)
docker compose up -d

# Backend: http://localhost:5294, Swagger at /swagger
# Connection string comes from user-secrets (MySQL is on host port 3307):
#   dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
#     "Server=localhost;Port=3307;Database=retirement_planner;User=retirement_planner_app;Password=<MYSQL_PASSWORD from .env>;"
cd backend/RetirementPlanner.Api && dotnet run --launch-profile http

# Frontend: http://localhost:4200 (Node 20, see frontend/.nvmrc)
cd frontend && nvm use && npm install && npm start
```

## Build and test

```bash
cd backend && dotnet build
cd backend && dotnet test
cd frontend && npx ng build
cd frontend && npx ng test --watch=false
```

Tests live in `backend/RetirementPlanner.Tests`: unit tests for services with mocked repositories,
and integration tests for repositories and migrations that use Testcontainers MySQL, so Docker must be running.

## Smoke test

Run this after `docker compose down -v && docker compose up -d` and `dotnet run --launch-profile http`,
which seeds the demo user in Development:

1. POST `/api/user/login` with `demo@example.com` / `demo123` returns 200 and the profile.
2. POST `/api/goal` creates a goal for the demo user.
3. GET `/api/goal/{profileId}` returns that goal.
4. POST `/api/financial/Add-Investment` for the current month returns 200 with CurrentSavings increased.
5. GET `/api/financial/progress/{goalId}` returns the percentage.
6. Repeating the same investment returns 409.

```bash
B=http://localhost:5294/api; Y=$(date +%Y); M=$((10#$(date +%m)))
curl -s -X POST $B/user/login -H 'Content-Type: application/json' -d '{"userName":"demo@example.com","password":"demo123"}'
curl -s -X POST $B/goal -H 'Content-Type: application/json' -d '{"profileId":1,"currentAge":30,"retirementAge":60,"targetSavings":1000000,"currentSavings":100000}'
curl -s $B/goal/1
curl -s -X POST $B/financial/Add-Investment -H 'Content-Type: application/json' -d "{\"goalId\":1,\"year\":$Y,\"month\":$M,\"monthlyInvestment\":2500}"
curl -s $B/financial/progress/1
curl -s -w ' [%{http_code}]\n' -X POST $B/financial/Add-Investment -H 'Content-Type: application/json' -d "{\"goalId\":1,\"year\":$Y,\"month\":$M,\"monthlyInvestment\":2500}"
```

## Rules

- The project name is Retirement Planner.
- Keep the layered structure: Controllers → Services → Repositories, with `DTO/` and `Models/`.
  Services and repositories have interfaces (`Services/Interfaces`, `Repositories/Interfaces`)
  registered in `Program.cs`. Follow SOLID.
- Schema changes go in a new `Migrations/VNNN__description.sql`; never edit a migration that has run on a shared database.
  Only `IDbConnectionFactory` may create database connections; repositories use the scoped `IUnitOfWork`.
- Never commit secrets. `.env` and user-secrets stay local; only `.env.example` with placeholders is committed.
- Use one feature branch per task, conventional commits (`feat:`, `fix:`, `chore:`, …), and open a PR with `gh pr create`.
- Name branches after the feature: `feat/...`, `fix/...`, `refactor/...`, `docs/...`.
- Open one PR per feature. PRs are squash-merged.
- Never use version labels in branch names, commits, tags, docs or comments.
- Every backend change comes with tests.
- Run `dotnet build`, `dotnet test` and `ng build` before committing.
- Don't change existing behaviour unless the task asks for it.
