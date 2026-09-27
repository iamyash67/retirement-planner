# CLAUDE.md

## Project overview

Retirement Planner: users log in, create one retirement goal (current age, retirement age,
target, current savings), record monthly investments, and track progress toward the target.

- **Backend:** ASP.NET Core 10 Web API in `backend/RetirementPlanner.Api` (Dapper + MySqlConnector, unit of work,
  JWT access tokens + rotating refresh tokens)
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
# The API refuses to start without a JWT signing key (32+ bytes):
#   dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"
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

Run this after `docker compose down -v && docker compose up -d` and `dotnet run --launch-profile http`.
The API needs `Jwt:SigningKey` in user-secrets, and Development seeds the demo user.

1. POST `/api/auth/login` with `demo@example.com` / `demo123` returns 200 with an access token and sets the refresh cookie.
2. POST `/api/goals` creates a goal for the demo user (201 with the goal).
3. GET `/api/goals` and GET `/api/goals/{id}` return that goal.
4. POST `/api/goals/{id}/contributions` for the current month returns 201, and the goal's currentSavings increases.
5. GET `/api/goals/{id}/progress` returns the percentage.
6. Repeating the same contribution returns 409.
7. GET `/api/goals` without a token returns 401.
8. POST `/api/auth/refresh` with the cookie returns 200 and rotates it. After POST `/api/auth/logout` (204), refresh returns 401.

```bash
B=http://localhost:5294/api; Y=$(date +%Y); M=$((10#$(date +%m))); JAR=$(mktemp)
H='Content-Type: application/json'; code() { curl -s -o /dev/null -w '%{http_code}' "$@"; }
T=$(curl -s -c $JAR -X POST $B/auth/login -H "$H" -d '{"email":"demo@example.com","password":"demo123"}' \
  | sed -E 's/.*"accessToken":"([^"]+)".*/\1/'); echo "1 login token: ${T:0:20}..."
A="Authorization: Bearer $T"
G=$(curl -s -X POST $B/goals -H "$A" -H "$H" \
  -d '{"currentAge":30,"retirementAge":60,"targetSavings":1000000,"currentSavings":100000}' \
  | sed -E 's/.*"id":([0-9]+).*/\1/'); echo "2 goal id: $G"
echo "3 list: $(curl -s $B/goals -H "$A")"; echo "3 get: $(curl -s $B/goals/$G -H "$A")"
C="{\"year\":$Y,\"month\":$M,\"amount\":2500}"
echo "4 contribution: $(code -X POST $B/goals/$G/contributions -H "$A" -H "$H" -d "$C")  goal: $(curl -s $B/goals/$G -H "$A")"
echo "5 progress: $(curl -s $B/goals/$G/progress -H "$A")"
echo "6 repeat: $(code -X POST $B/goals/$G/contributions -H "$A" -H "$H" -d "$C") (expect 409)"
echo "7 no token: $(code $B/goals) (expect 401)"
echo "8 refresh: $(code -b $JAR -c $JAR -X POST $B/auth/refresh)  logout: $(code -b $JAR -c $JAR -X POST $B/auth/logout)  refresh after logout: $(code -b $JAR -X POST $B/auth/refresh) (expect 200 204 401)"
```

## Rules

- The project name is Retirement Planner.
- Keep the layered structure: Controllers → Services → Repositories, with `DTO/` and `Models/`.
  Services and repositories have interfaces (`Services/Interfaces`, `Repositories/Interfaces`)
  registered in `Program.cs`. Follow SOLID.
- Every endpoint has its own request and response DTO (`DTO/Requests`, `DTO/Responses`). Responses never
  expose domain models or credentials; map explicitly with the extension methods in `Mapping/`, no AutoMapper.
- Every request DTO has a FluentValidation validator in `Validators/`. Validation failures are 400
  ValidationProblemDetails with per-field errors; 404 and 409 stay plain strings. Controllers only translate
  HTTP to service calls and results to status codes.
- Schema changes go in a new `Migrations/VNNN__description.sql`; never edit a migration that has run on a shared database.
  Only `IDbConnectionFactory` may create database connections; repositories use the scoped `IUnitOfWork`.
- Every endpoint requires authentication unless it is marked `[AllowAnonymous]` (only `/api/auth/*` is).
  Take the user id from the access token (`User.GetUserId()`), never from the route or body; scope every
  goal query by that user so other users' data is a 404.
- Never commit secrets (including `Jwt:SigningKey`). `.env` and user-secrets stay local; only `.env.example` with placeholders is committed.
- Use one feature branch per task, conventional commits (`feat:`, `fix:`, `chore:`, …), and open a PR with `gh pr create`.
- Name branches after the feature: `feat/...`, `fix/...`, `refactor/...`, `docs/...`.
- Open one PR per feature. PRs are squash-merged.
- Never use version labels in branch names, commits, tags, docs or comments.
- Every backend change comes with tests.
- Run `dotnet build`, `dotnet test` and `ng build` before committing.
- Don't change existing behaviour unless the task asks for it.
