# Anyware Task Management API

An ASP.NET Core 8 task management API using a DDD-style four-project structure: Domain, Application, Infrastructure, and API.

## Requirements

- .NET 8 SDK
- Docker Desktop

## Run locally

1. **Configure local Docker secrets.** Docker Compose reads `.env` for the SQL Server password and, when using the full-stack Docker option below, the JWT signing key and seeded admin password. Copy the example file, then replace all three sample values with local-only secrets:

   ```powershell
   Copy-Item .env.example .env
   notepad .env
   ```

   `.env` is excluded from Git, so these secrets are not uploaded. If you already have a SQL Server Docker volume, keep the SQL password it was originally created with; changing `.env` does not change the password inside an existing database volume.

2. **Give the API its local connection and signing settings.** .NET user secrets stores these settings on your computer, outside the repository. Run the following from the repository folder. Replace each capitalized placeholder with your own value; the SQL password must be the same one you entered in `.env`.

   ```powershell
   dotnet user-secrets init --project src/AnywareTaskManagement.API
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=AnywareTaskManagementDb;User Id=sa;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;Encrypt=False" --project src/AnywareTaskManagement.API
   dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SECRET_KEY_AT_LEAST_32_CHARACTERS" --project src/AnywareTaskManagement.API
   dotnet user-secrets set "SeedAdmin:Password" "YOUR_STRONG_ADMIN_PASSWORD" --project src/AnywareTaskManagement.API
   ```

   Run `dotnet user-secrets init` only once for this project. The SQL connection string lets the API connect to your Docker SQL Server; `Jwt:Key` is a random signing secret with at least 32 characters; `SeedAdmin:Password` is the password for the admin account when it is first seeded. User secrets stay outside the repository and are not uploaded to GitHub.
3. Start only SQL Server and Redis for local development from the repository root: `docker compose up -d sqlserver redis`.
4. Run the API: `dotnet run --project src/AnywareTaskManagement.API`.
5. Open the Swagger URL printed by ASP.NET Core, usually `http://localhost:5154/swagger`. The initial EF migration is applied at startup.
6. Run the unit tests: `dotnet test AnywareTaskManagement.sln`.

The API connects to SQL Server at `localhost:1433` and Redis at `localhost:6379`. It fails at startup with a configuration error if the SQL connection string or JWT signing key is missing.

## Seeded administrator

The seed email defaults to `admin@example.com`. Set `SeedAdmin:Password` using the user-secrets command above; that password is used when the admin is first created. The seeder does not change an admin account that already exists in the database, so setting a new value will not reset an existing account's password. Set `SeedAdmin:Email` as a secret or environment setting if you want another address. No working admin password or signing key is stored in the repository.

## API overview

- `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `GET /api/auth/me`
- `POST /api/admin/users`, `GET /api/admin/users`, `DELETE /api/admin/users/{id}` (Admin role only)
- `POST /api/tasks`, `GET /api/tasks`, `GET /api/tasks/{id}`, `PATCH /api/tasks/{id}/status` (authenticated and scoped to the caller)

Use the access token as a Bearer token in Swagger's Authorize dialog. Task lists sort by priority descending, then creation time ascending. A user cannot create two tasks with the same title on the same UTC date. Task detail reads use Redis for ten minutes; status changes and worker updates invalidate the cache. Creating a task queues its ID for a hosted in-process worker that simulates processing and moves the task to `InProgress`.

## Design notes and assumptions

- Passwords are hashed with ASP.NET Core Identity's password hasher. Refresh tokens are signed JWTs with a separate token-use claim and seven-day expiry; they are stateless and cannot be revoked before expiry.
- Users are soft-deleted. Admin accounts cannot be deleted through the user endpoint.
- Admin user management is role-protected; task routes remain scoped to the authenticated user's own tasks.
- The queue is in memory, suitable for this exercise; queued work is lost if the API process stops.
- Global middleware returns safe JSON errors and logs unexpected failures; Serilog writes structured logs to the console.

## Stage 2: React frontend

The React 19 + TypeScript single-page app is in `frontend/`. It uses Vite, React Router, TanStack Query for server state, and React Hook Form + Zod for client validation. The task list uses server-side pagination, status filtering, and title search.

### Run the backend and frontend together

1. Complete the API secret setup and start SQL Server and Redis using steps 1–3 above (`docker compose up -d`).
2. In one terminal, start the backend from the repository root with `dotnet run --project src/AnywareTaskManagement.API --launch-profile http`. The API listens at `http://localhost:5154`.
3. In a second terminal, enter the frontend folder and install its dependencies: `cd frontend` then `npm install`.
4. Copy `frontend/.env.example` to `frontend/.env.local`. Set `VITE_API_URL=http://localhost:5154/api` if the API uses the default address.
5. Start the frontend with `npm run dev`. Open the URL Vite prints, normally `http://localhost:5173`.
6. Register a normal account to manage tasks. To use the admin area, sign in with the seeded administrator below.

Build the frontend with `cd frontend` and `npm run build`. Run the frontend unit and component tests from the same folder with `npm test`.

### Optional: start all services with Docker Compose

1. Copy `.env.example` to `.env` and replace all three sample values with local secrets. Keep the signing key at least 32 characters.
2. From the repository root run `docker compose up --build -d`.
3. Open `http://localhost:5173`. The API is available at `http://localhost:5154/swagger`.
4. Stop the stack with `docker compose down`. Add `-v` only if you also intend to remove the database and Redis data volumes.

### Environment variables

| Application | Variable | Purpose |
| --- | --- | --- |
| Frontend | `VITE_API_URL` | API base URL, including `/api` (example: `http://localhost:5154/api`) |
| Backend | `ConnectionStrings__DefaultConnection` | SQL Server connection string; local development may use .NET user secrets as described above |
| Backend | `ConnectionStrings__Redis` | Redis connection string (defaults to `localhost:6379`) |
| Backend | `Jwt__Key` | JWT signing key of at least 32 characters |
| Backend | `Jwt__Issuer` | JWT issuer and audience; defaults to `AnywareTaskManagement` |
| Backend | `SeedAdmin__Email` | Seed administrator email; defaults to `admin@example.com` |
| Backend | `SeedAdmin__Password` | Password used only when the seed administrator is first created |
| Backend | `Cors__AllowedOrigins__0` | First allowed browser origin; defaults to `http://localhost:5173` |
| Docker Compose | `ANYWARE_SQL_PASSWORD` | SQL Server SA password in root `.env` |
| Docker Compose | `ANYWARE_JWT_KEY` | API JWT signing key in root `.env` |
| Docker Compose | `ANYWARE_SEED_ADMIN_PASSWORD` | Seeded admin password in root `.env` |

The frontend has no API URL embedded in components. A sample environment file is provided at `frontend/.env.example`.

### Seeded administrator credentials

- Email: `admin@example.com` (unless overridden with `SeedAdmin:Email`)
- Password: the value you set for `SeedAdmin:Password` in .NET user secrets during setup. It is deliberately not committed to the repository. For an existing database, the seeder does not reset the password.

If the existing administrator password is unavailable, `dotnet run --project tools/ResetAdminPassword` prompts for a replacement without echoing or saving it. The utility updates only the configured administrator and preserves the database and tasks.

### Changes since Stage 1

- Added a restricted CORS policy for the configured frontend origin. The development fallback allows only `http://localhost:5173`; set `Cors:AllowedOrigins` for another exact origin.
- Added readable JSON details for otherwise empty framework-generated 401, 403, and 404 responses, such as opening an admin endpoint as a normal user.
- Aligned JWT validation's default issuer and audience with the token service, so tokens work when `Jwt:Issuer` is omitted.
- Added an atomic, conditional database update for the background worker. It advances a task only if its persisted status is still `Pending`, so a concurrent user status update is not overwritten.
- Enforced the task description maximum length (2,000 characters) on the API, matching the database limit and frontend validation.
- Updated the existing Docker Compose stack with API and frontend services plus SQL Server and Redis health checks, allowing the full application to start together.
- Added a paged task endpoint that applies the status/title filters and priority/creation ordering in the database. The original array endpoint remains available.

### Enhancements

- Added refresh-token recovery in the API client, sharing a single refresh request when concurrent calls receive an expired access token.
- Poll task data only while the background worker has a `Pending` task; task lists and details stop polling after the worker changes its status.
- Cached task and user data with TanStack Query and invalidate or update affected queries after writes to avoid full-page reloads.
- Preserve form values after server errors and disable submit controls while requests are running; use an in-page, accessible confirmation dialog for user deletion.
- Keep the background worker from overwriting a non-Pending status after its processing delay.
- Clear cached server state on logout and show an actionable message on the login screen after an expired session.
- Added container builds for the API and the static frontend so the whole stack can be brought up with one Compose command.
- Added server-side task pagination with filtered counts, to keep list requests small as a user's task history grows.
- Added Vitest and React Testing Library coverage for authentication form validation, readable login errors, token headers, and refresh-token retries.

### Assumptions

- The local frontend origin is Vite's default `http://localhost:5173`; use `Cors:AllowedOrigins` to permit a different origin explicitly.
- Registration and admin-created accounts share the backend's existing minimum password length of 8 characters and maximum name length of 100. Task title is limited to 200 characters and description to 2,000.
- The administrator password is chosen locally through user secrets and is not shared in source control. Admin creation/deletion walkthroughs use a disposable non-admin test account.
- Task refresh uses short-interval polling while a task remains Pending, matching the API's in-process background queue; no real-time push endpoint exists.

### AI tool use

ChatGPT was used to inspect the supplied task requirements and existing API, draft the React UI and API client, make the CORS and background-worker changes, and check the strict frontend build. I reviewed the API contracts and the submitted code; I will explain the implementation in my own words during the walkthrough.
