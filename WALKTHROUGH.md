# Professional walkthrough recording guide

Target length: about 6 minutes. Keep the demo focused on design choices and observable behavior. Use the narration as a guide and adjust it to your own voice.

## Before recording

- Start SQL Server and Redis with `docker compose up -d`, then run the API and open Swagger.
- Confirm startup, admin seeding, and login with both admin and regular-user accounts.
- Prepare a regular user and tasks with High, Medium, and Low priorities. Keep a task ID ready for the detail/status demo.
- Authorize Swagger in advance if possible. Keep access and refresh token values out of view. If showing login, scroll the response so token strings are not visible.
- Close unrelated windows and notifications. Maximize Swagger, use readable browser zoom, and keep the API terminal available for the logs and worker section.
- Check your audio and speak slightly slower than usual. Pause between sections and avoid moving the mouse while explaining.

## Recording script

### 0:00-0:20 | Opening

**Show:** Swagger UI with the API title visible.

**Say:** “Hello, I’m [your name]. This is my .NET Task Management API, built for the backend developer technical task. I’ll walk through the architecture, authentication and authorization, task behavior, Redis caching, background processing, and the business rules.”

### 0:20-1:05 | Architecture

**Show:** Solution tree and the four projects.

**Say:** “The solution is split into four projects. Domain contains the user and task entities and enums. Application contains service contracts, DTOs, and use cases. Infrastructure implements persistence, repositories, Redis, token and password services, and the background worker. The API wires these dependencies together and exposes the controllers. This separates business operations from HTTP and database details.”

### 1:05-1:35 | Startup, database, and admin seed

**Show:** Terminal with the API running; optionally show `docker compose ps` and the README setup section.

**Say:** “SQL Server and Redis run through Docker Compose. On startup, the API applies the EF Core migration and seeds an administrator if the configured admin email does not exist. Local secrets are stored with .NET user secrets and are not committed to the repository.”

### 1:35-2:15 | Authentication and authorization

**Show:** Swagger auth endpoints, then `/api/auth/me` with a regular-user token already authorized. Do not show token contents.

**Say:** “Users can register and log in. Passwords are stored as hashes, and successful login returns a short-lived access token and a refresh token. Protected routes use JWT authentication. The current-user endpoint returns the signed-in user’s profile. Admin routes require the Admin role, while task routes are scoped to the authenticated user.”

### 2:15-2:55 | Admin user management

**Show:** `GET /api/admin/users` as admin. Briefly show the create and delete routes, using disposable data only if executing them.

**Say:** “The seeded admin can list users, create users, and delete users. These routes require the Admin role. User deletion is a soft delete, so the user is excluded from normal queries without physically removing the database row. A regular user attempting an admin route receives 403 Forbidden.”

### 2:55-4:05 | Task endpoints and business rules

**Show:** `POST /api/tasks`, `GET /api/tasks`, `GET /api/tasks/{id}`, and `PATCH /api/tasks/{id}/status`. Show a list ordered High, Medium, then Low.

**Say:** “A user can create tasks, list their tasks, fetch a task by ID, and update its status. The API takes the owner from the authenticated user rather than trusting a client-supplied owner. The list sorts by priority first, then creation time. Creating a task with the same title twice for the same user on the same UTC date returns a conflict. If a user requests another user’s task, the API returns not found.”

### 4:05-4:50 | Redis caching

**Show:** Fetch a task, run `docker exec anyware-redis redis-cli EXISTS "task:<task-id>"` and show `1`; update task status, run it again and show `0`.

**Say:** “The task detail endpoint checks Redis first and loads from SQL Server on a cache miss. It caches the response for ten minutes. Updating the task status removes that key, so the next read gets the latest data and caches it again. I verified the key appears after a read and is removed by an update.”

### 4:50-5:25 | Background processing and logging

**Show:** Create a task, show it move to InProgress, and briefly show API request logs.

**Say:** “Task creation saves the task and queues its ID for an in-process background worker. The worker simulates processing, updates the task to InProgress, and invalidates its cached detail. Serilog records HTTP requests and application events. This exercise uses an in-memory queue, so queued work does not survive an API process restart.”

### 5:25-5:50 | Error handling and tests

**Show:** A duplicate-task 409 response and the passing test summary.

**Say:** “Global exception middleware returns consistent JSON errors without exposing unexpected internal exception details. Unit tests cover task creation and queueing, duplicate prevention, ownership checks, and cache invalidation. The solution builds cleanly and all four tests pass.”

### 5:50-6:05 | Close

**Show:** Swagger or the solution tree.

**Say:** “That concludes the walkthrough. The README contains setup instructions, API routes, and implementation assumptions. Thank you for watching.”

## After recording

- Watch the recording and check audio, readable text, and that tokens or unrelated notifications are not visible.
- Trim setup delays and mistakes, but leave enough context for each action and response to make sense.
- Export at 1080p if available and keep the original in case you need to correct anything.
