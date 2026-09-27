# CLAUDE.md

## Project overview

Retirement Planner: users log in, create one retirement goal (current age, retirement age,
target, current savings), record monthly investments, and track progress toward the target.

- **Backend:** ASP.NET Core 10 Web API in `backend/RetirementPlanner.Api` (ADO.NET + MySql.Data, stored procedures)
- **Frontend:** Angular 18 (standalone components, Angular Material) in `frontend/`
- **Database:** MySQL 8 (the `mysql:8.4` image via Docker Compose, host port 3307); scripts in `database/`
- **Planned:** Python microservices, added later

See `README.md` for the architecture, setup and API routes.

## Run

```bash
# Database (needs .env; copy .env.example first)
docker compose up -d

# Backend: http://localhost:5294, Swagger at /swagger
# Connection string comes from user-secrets (MySQL is on host port 3307):
#   dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
#     "Server=localhost;Port=3307;Database=retirement_planner;User=rpt_app;Password=<MYSQL_PASSWORD from .env>;"
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

There are no automated tests yet. The first backend change must add a test project
(for example `backend/RetirementPlanner.Api.Tests`) and add it to `backend/RetirementPlanner.sln`.

## Rules

- The project name is Retirement Planner.
- Keep the layered structure: Controllers → Services → Repositories, with `DTO/` and `Models/`.
  Services and repositories have interfaces (`Services/Interfaces`, `Repositories/Interfaces`)
  registered in `Program.cs`. Follow SOLID.
- Never commit secrets. `.env` and user-secrets stay local; only `.env.example` with placeholders is committed.
- Use one feature branch per task, conventional commits (`feat:`, `fix:`, `chore:`, …), and open a PR with `gh pr create`.
- Name branches after the feature: `feat/...`, `fix/...`, `refactor/...`, `docs/...`.
- Open one PR per feature. PRs are squash-merged.
- Never use version labels in branch names, commits, tags, docs or comments.
- Every backend change comes with tests.
- Run `dotnet build`, `dotnet test` and `ng build` before committing.
- Don't change existing behaviour unless the task asks for it.
