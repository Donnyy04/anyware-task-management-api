# Anyware Task Management API

An ASP.NET Core 8 task management API using a DDD-style four-project structure: Domain, Application, Infrastructure, and API.

## Requirements

- .NET 8 SDK
- Docker Desktop

## Run locally

1. Copy `.env.example` to `.env` and replace `ANYWARE_SQL_PASSWORD` with a strong local value.
2. Configure application secrets with .NET user secrets:

   ```powershell
   dotnet user-secrets init --project src/AnywareTaskManagement.API
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=AnywareTaskManagementDb;User Id=sa;Password=YOUR_SQL_PASSWORD;TrustServerCertificate=True;Encrypt=False" --project src/AnywareTaskManagement.API
   dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SECRET_KEY_AT_LEAST_32_CHARACTERS" --project src/AnywareTaskManagement.API
   dotnet user-secrets set "SeedAdmin:Password" "YOUR_STRONG_ADMIN_PASSWORD" --project src/AnywareTaskManagement.API
   ```

   Replace the sample values. Use the same SQL password in `.env` and the connection string. User secrets stay outside the repository.
3. Start SQL Server and Redis from the repository root: `docker compose up -d`.
4. Run the API: `dotnet run --project src/AnywareTaskManagement.API`.
5. Open the Swagger URL printed by ASP.NET Core, usually `http://localhost:5154/swagger`. The initial EF migration is applied at startup.
6. Run the unit tests: `dotnet test AnywareTaskManagement.sln`.

The API connects to SQL Server at `localhost:1433` and Redis at `localhost:6379`. It fails at startup with a configuration error if the SQL connection string or JWT signing key is missing.

## Seeded administrator

The seed email defaults to `admin@example.com`. Set `SeedAdmin:Password` using the user-secrets command above; that configured password is the admin password. The seeder creates the admin only when the email is not already present. Set `SeedAdmin:Email` as a secret or environment setting if you want another address. No working admin password or signing key is stored in the repository.

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
