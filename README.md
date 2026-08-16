# Assignment & Submission Management System

> Authored by **Rakib Hassan** · submitted for candidacy evaluation at OnnoRokom Projukti Ltd. · licensed for evaluation only (see [LICENSE](LICENSE))

A role-based assignment and submission platform for a school or college: Admins run the
catalogue, Teachers author and grade work, Students see only what their enrolments entitle them
to. Every access decision is enforced **server-side, per resource** — the frontend is a thin,
fast client, not a gatekeeper.

![.NET](https://img.shields.io/badge/ASP.NET_Core-8-512BD4?logo=dotnet)
![Next.js](https://img.shields.io/badge/Next.js-16-000000?logo=nextdotjs)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![TypeScript](https://img.shields.io/badge/TypeScript-strict-3178C6?logo=typescript)
![Tests](https://img.shields.io/badge/tests-271_passing-2ea44f)
![License](https://img.shields.io/badge/license-Evaluation-ff6b6b)

---

## Table of contents

1. [Run it in one command](#run-it-in-one-command)
2. [Why this architecture](#why-this-architecture)
3. [Main features](#main-features)
4. [Business rules](#business-rules)
5. [Tech stack](#tech-stack)
6. [Project structure](#project-structure)
7. [Setup instructions](#setup-instructions)
8. [Database setup](#database-setup)
9. [Running the backend](#running-the-backend)
10. [Running the frontend](#running-the-frontend)
11. [Running the tests](#running-the-tests)
12. [Demo credentials](#demo-credentials)
13. [Environment variables](#environment-variables)
14. [API overview](#api-overview)
15. [Assumptions](#assumptions)
16. [Known limitations](#known-limitations)

---

## Run it in one command

```bash
git clone https://github.com/rakibhassanrh66/assignment-submission-management-system.git
cd assignment-submission-management-system
cp .env.example .env          # PowerShell: Copy-Item .env.example .env
# edit .env — replace every CHANGE_ME (see "Environment variables")
docker compose up --build
```

Then open:

| What | Where |
|---|---|
| Web app | <http://localhost:3000> |
| Swagger UI | <http://localhost:8080/swagger> |

Sign in with `admin@demo.test` / `Admin@123`. The database is created, migrated and seeded
automatically on first start — there is no manual SQL step.

## Live deployment

| What | Where |
|---|---|
| Frontend (Vercel) | <https://frontend-hazel-omega-38.vercel.app> |
| API (Swagger UI) | <https://shadow-gotta-designers-vinyl.trycloudflare.com/swagger> |

The frontend is built on Vercel with the API URL inlined. The API itself runs on the author's
machine behind a free Cloudflare quick tunnel — no cloud account or credit card required. A
quick-tunnel URL is **ephemeral**: if the `tunnel-api` container restarts, the URL changes and
the frontend's build-time API URL must be updated to match. See
[docker-compose.tunnel.yml](docker-compose.tunnel.yml).

---

## Why this architecture

Three roles — **Admin**, **Teacher**, **Student** — with authorization enforced **server-side on
every endpoint**. Role checks and ownership checks are independent layers: being a Teacher is not
enough to grade a submission — it must be a submission on an assignment that teacher created.

That second layer is the part worth looking at. `[Authorize(Roles = "Teacher")]` establishes that
the caller is *a* teacher and says nothing about whether this is *their* assignment;
[`IResourceAuthorizer`](backend/src/AssignmentSystem.Application/Common/Interfaces/IResourceAuthorizer.cs)
answers the second question and is called explicitly at each site that needs it. Every method
there takes the acting user's id as an argument rather than reading it internally, so at the call
site it is always visibly sourced from the token and never from the request body.

Business rules live in the Application layer and are expressed by throwing a domain exception that
names the rule. A middleware maps each exception to its own status code and an RFC 7807
`ProblemDetails` body, so controllers never map status codes by hand and adding a rule does not
mean editing a switch statement.

---

## Main features

**Admin**

- Create users of any role; deactivate rather than delete, so submissions keep their author
- Manage classes/courses and the subjects within them
- Allocate teachers to a subject *within a class* — the input to rule 3
- Enrol students into classes — the input to rule 2
- Read-only oversight of every assignment and every submission in the system

**Teacher**

- Create, update and delete assignments for subjects they are allocated to
- Set title, description, deadline, maximum marks, and whether late work and updates are allowed
- Publish a draft; students see nothing until then
- Read submissions for their own assignments, enter marks and feedback, and move a submission
  through its lifecycle

**Student**

- See published assignments for the classes they are enrolled in, and nothing else
- Read an assignment with its deadline, maximum marks and their own submission state
- Submit an answer, and update it while the window is open
- See status, marks and teacher feedback

**Throughout**

- JWT auth: 15-minute access tokens, 7-day rotating refresh tokens, reuse detection
- Pagination and filtering on every list endpoint
- Swagger/OpenAPI with a JWT bearer definition, so the docs page can call the API
- Serilog to console and a daily rolling file
- Rate limiting on the credential endpoints
- Dark, motion-driven UI: universal top navigation, breadcrumbs, page transitions and a
  cursor-field canvas (Lenis smooth scroll, `motion/react` animations, reduced-motion aware)

---

## Business rules

The eleven rules the system enforces, each proven by tests whose names are listed under
[Running the tests](#running-the-tests).

| # | Rule |
|---|---|
| 1 | Students never see Draft or Archived assignments |
| 2 | A student only reaches assignments for classes they are enrolled in |
| 3 | A teacher may only create assignments for a (subject, class) pair allocated to them |
| 4 | A teacher may only act on assignments they created |
| 5 | Submissions after the deadline are refused, or accepted and flagged `Late` if allowed |
| 6 | One submission per student per assignment |
| 7 | Updates require the assignment to permit them, the deadline to be open, and no grade yet |
| 8 | A student may only read or update their own submission |
| 9 | Marks fall within `[0, MaxMarks]` |
| 10 | Submission status follows a fixed transition table |
| 11 | Only a Draft can be published |

Two are worth expanding, because the brief leaves them open:

**Rule 7** as written says "update a submission before the deadline, if allowed". Taken literally
that would let a student rewrite an answer a teacher had already marked, leaving the marks and
feedback describing content that no longer exists. Grading therefore closes the window early, and
that is tested by `Update_After_Grading_Returns_409`.

**Rule 5** distinguishes late work permanently. A submission accepted after the deadline is stored
as `Late` rather than `Submitted`, and nothing ever transitions *into* `Late` afterwards — it is
set once, at creation.

---

## Tech stack

| Layer | Choice |
|---|---|
| API | ASP.NET Core 8 Web API, minimal hosting |
| ORM | EF Core 8 + Npgsql, code-first migrations |
| Database | PostgreSQL 16 |
| Auth | JWT bearer (15 min access + 7 day rotating refresh), `PasswordHasher<User>` |
| Validation | FluentValidation |
| Logging | Serilog (console + daily rolling file) |
| API docs | Swashbuckle / Swagger at `/swagger` |
| Tests | xUnit, Moq, FluentAssertions, Testcontainers |
| Frontend | Next.js 16 App Router, React 19, TypeScript (strict), Tailwind CSS 4, react-hook-form + zod, motion/react |

**Why PostgreSQL over MongoDB:** the domain is inherently relational — users, enrollments,
teacher-assignments, assignments and submissions all have clear foreign keys, and correctness
here depends on constraints the database can enforce (notably the unique
`(AssignmentId, StudentId)` index behind the one-submission-per-student rule).

---

## Project structure

```
.
├─ backend/
│  ├─ AssignmentSystem.sln
│  ├─ Directory.Build.props                  # shared TFM + authorship metadata
│  ├─ src/
│  │  ├─ AssignmentSystem.Domain/            # entities, enums, domain exceptions (no framework deps)
│  │  ├─ AssignmentSystem.Application/       # DTOs, services, validators — the business rules live here
│  │  ├─ AssignmentSystem.Infrastructure/    # EF Core, migrations, seeder, JWT, hashing
│  │  └─ AssignmentSystem.Api/               # controllers, middleware, Program.cs, Swagger
│  ├─ tests/
│  │  ├─ AssignmentSystem.UnitTests/         # policies and the authorizer, no database
│  │  └─ AssignmentSystem.IntegrationTests/  # real HTTP against a throwaway Postgres container
│  └─ Dockerfile
├─ frontend/
│  ├─ src/
│  │  ├─ app/                                # App Router: /login, /admin/*, /teacher/*, /student/*, /profile
│  │  ├─ components/                         # DataTable, Modal, Pagination, badges, layout, effects
│  │  └─ lib/                                # API client, session store, types, formatting
│  └─ Dockerfile
├─ .github/workflows/ci.yml
├─ docker-compose.yml
└─ .env.example
```

The backend is layered so that dependencies point inwards: `Domain` references nothing,
`Application` references `Domain`, `Infrastructure` and `Api` reference `Application`. The
business rules therefore have no dependency on EF Core or ASP.NET Core and are testable without
either.

---

## Setup instructions

### Option A — Docker (recommended)

Requires Docker Desktop.

```bash
cp .env.example .env
# edit .env, replacing every CHANGE_ME
docker compose up --build
```

Three services come up: `db` (PostgreSQL 16), `api` (port 8080) and `web` (port 3000). The API
waits on the database's health check before starting, so migrations never race it.

### Option B — running locally without Docker

Requires .NET 8 SDK, Node 22+, and a PostgreSQL 16 instance.

```bash
# 1. Database — either your own instance, or just the container:
docker compose up db -d

# 2. API
cd backend
dotnet restore
dotnet run --project src/AssignmentSystem.Api      # http://localhost:5000

# 3. Frontend, in a second terminal
cd frontend
npm install
npm run dev                                         # http://localhost:3000
```

When running the API outside Docker, set `ConnectionStrings__Default` to use `localhost` rather
than `db` as the host, and point `NEXT_PUBLIC_API_URL` at whichever port the API bound to.

---

## Database setup

**There is nothing to do by hand.** On startup the API runs EF Core migrations to create the
schema, then runs an idempotent seeder that populates demo users, classes, subjects, allocations,
enrolments and a few assignments and submissions.

- **Migrations** live in `backend/src/AssignmentSystem.Infrastructure/Persistence/Migrations/`.
- **Seeding** is [`DbSeeder`](backend/src/AssignmentSystem.Infrastructure/Persistence/DbSeeder.cs).
  It is idempotent — it derives deterministic ids from stable keys and skips rows that already
  exist, so restarting the stack never duplicates data. `DbSeederTests` asserts this.
- Set `SEED_ON_STARTUP=false` in `.env` to skip seeding.

The data model and the reasoning behind it — including why the `Marks` upper bound is a service
rule rather than a database `CHECK` — is covered under [Assumptions](#assumptions).

To create a new migration after changing an entity:

```bash
dotnet ef migrations add <Name> \
  --project backend/src/AssignmentSystem.Infrastructure \
  --startup-project backend/src/AssignmentSystem.Api
```

---

## Running the backend

```bash
dotnet run --project backend/src/AssignmentSystem.Api
```

Swagger UI is served at `/swagger` in Development. It carries a JWT bearer definition: call
`POST /api/v1/auth/login`, copy the `accessToken` from the response, click **Authorize**, and
paste it to try the protected routes.

Logs go to the console and to `logs/assignment-system-<date>.log` beside the binary, rolling
daily with seven files retained.

---

## Running the frontend

```bash
cd frontend
npm install
npm run dev
```

Other scripts: `npm run build`, `npm run lint`, `npm run typecheck`.

`NEXT_PUBLIC_API_URL` is read at **build** time, not run time — Next.js inlines `NEXT_PUBLIC_*`
values into the bundle. Changing it means rebuilding, which is why `docker-compose.yml` passes it
as a build arg as well as an environment variable.

---

## Running the tests

```bash
dotnet test backend/AssignmentSystem.sln
```

**Docker must be running.** The integration tests start their own throwaway `postgres:16`
container per run through Testcontainers, so there is no test database to configure and no state
carried between runs.

| Suite | Count | What it covers |
|---|---|---|
| `AssignmentSystem.UnitTests` | 88 | Status policies, the deadline boundary, the resource authorizer, auth, token rotation and login throttling — no database |
| `AssignmentSystem.IntegrationTests` | 183 | Every endpoint over real HTTP, the schema's constraints, the seeder, Swagger, rate limiting, and the full workflow |
| **Total** | **271** | |

Useful filters:

```bash
# One module
dotnet test backend/AssignmentSystem.sln --filter "FullyQualifiedName~StudentModuleTests"

# The role gate across all 42 routes
dotnet test backend/AssignmentSystem.sln --filter "FullyQualifiedName~AuthorizationMatrixTests"

# The whole product, end to end
dotnet test backend/AssignmentSystem.sln --filter "FullyQualifiedName~EndToEndWorkflowTests"
```

Frontend checks:

```bash
cd frontend && npm run lint && npm run typecheck && npm run build
```

Each business rule above is pinned by its own named tests — the test files live under
`backend/tests`, and the filters above show how to run any single rule's suite.

---

## Demo credentials

| Role | Email | Password |
|---|---|---|
| Admin | `admin@demo.test` | `Admin@123` |
| Teacher | `teacher@demo.test` | `Teacher@123` |
| Student | `student@demo.test` | `Student@123` |

These are seeded, hashed, throwaway evaluation accounts. They are intentionally the only
credentials in this repository, and they are not secrets.

Additional seeded accounts, which exist so the negative cases are testable rather than
hypothetical:

| Email | Password | Why it exists |
|---|---|---|
| `teacher2@demo.test` | `Teacher@123` | Teaches only in MATH-201 — a teacher who provably does not teach CS-101 |
| `student2@demo.test` | `Student@123` | Owns a graded submission belonging to someone other than `student@demo.test` |
| `student3@demo.test`, `student4@demo.test` | `Student@123` | Split across both classes, so class scoping has more than one shape |

---

## Environment variables

Copy [`.env.example`](.env.example) to `.env` and replace every `CHANGE_ME`. `.env` is
git-ignored and has never been committed.

| Variable | Purpose |
|---|---|
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` / `POSTGRES_PORT` | The `db` service |
| `ConnectionStrings__Default` | How the API reaches Postgres. Host is `db` under compose, `localhost` otherwise |
| `Jwt__Key` | HMAC-SHA256 signing key. **Must be at least 32 bytes** — the API refuses to start otherwise |
| `Jwt__Issuer` / `Jwt__Audience` | Token issuer and audience, validated on every request |
| `Jwt__AccessTokenMinutes` / `Jwt__RefreshTokenDays` | Token lifetimes (default 15 and 7) |
| `CORS__AllowedOrigins` | Comma-separated. **No wildcard fallback** — empty means no browser origin is accepted, rather than all of them |
| `RateLimit__AuthPermitPerWindow` / `RateLimit__AuthWindowSeconds` | Per-IP limit on `/auth/login` and `/auth/refresh` |
| `SEED_ON_STARTUP` | `true` to run the idempotent seeder on startup |
| `NEXT_PUBLIC_API_URL` | Where the browser finds the API. Inlined at build time |

Generate a signing key:

```bash
openssl rand -base64 48                                                   # bash
[Convert]::ToBase64String((1..48 | ForEach-Object { Get-Random -Max 256 }))  # PowerShell
```

---

## API overview

42 endpoints under `/api/v1`. Full detail, including request and response schemas, is in Swagger.

| Area | Routes | Who |
|---|---|---|
| Auth | `POST /auth/login`, `POST /auth/refresh` | Anonymous |
| | `GET /auth/me` | Any signed-in user |
| Meta | `GET /meta` | Anonymous |
| Users | `GET|POST /users`, `GET|PUT|DELETE /users/{id}` | Admin |
| Classes | `GET|POST /classes`, `GET|PUT|DELETE /classes/{id}` | Admin |
| Subjects | `GET|POST /subjects`, `GET|PUT|DELETE /subjects/{id}` | Admin |
| Allocations | `GET|POST /teacher-assignments`, `DELETE /teacher-assignments/{id}` | Admin |
| | `GET /teacher-assignments/mine` | Teacher |
| Enrolments | `GET|POST /enrollments`, `DELETE /enrollments/{id}` | Admin |
| Assignments | `GET /assignments` | Admin |
| | `GET /assignments/mine`, `POST /assignments`, `PUT|DELETE /assignments/{id}`, `POST /assignments/{id}/publish`, `GET /assignments/{id}/submissions` | Teacher |
| | `GET /assignments/available`, `POST /assignments/{id}/submit` | Student |
| | `GET /assignments/{id}` | Any — response shaped per role |
| Submissions | `GET /submissions` | Admin |
| | `PUT /submissions/{id}/grade`, `PUT /submissions/{id}/status` | Teacher |
| | `GET /submissions/mine`, `PUT /submissions/{id}` | Student |
| | `GET /submissions/{id}` | Any — ownership rule per role |

**Errors** are RFC 7807 `application/problem+json` throughout, with a `traceId` that ties a
response to a line in the log.

- **400** — the request could not be understood: malformed JSON, a bad Guid, or a query parameter
  the endpoint does not define
- **401** — no token, or an expired or invalid one
- **403** — authenticated, but not permitted (role or ownership)
- **404** — no such resource, *or* a resource the caller is not allowed to know exists
- **409** — understood, but conflicts with current state (duplicate, closed window, bad transition)
- **422** — understood and well-formed, but broke a validation rule; carries per-field errors
- **429** — too many attempts on a rate-limited endpoint (login / refresh)

The 400/422 split is deliberate: retrying is pointless for one and meaningful for the other.

**Query filters are strict.** A list endpoint rejects a parameter it cannot honour with 400
instead of ignoring it, so `available?status=Draft` fails loudly rather than returning published
rows and appearing to have filtered.

---

## Assumptions

Where the brief left something open, this is what was decided and why.

1. **A student belongs to one or more classes** via `Enrollment`; assignments are visible per
   class. Nothing in the brief limited a student to a single class, and the multi-class case is
   the more general one.
2. **A subject belongs to exactly one class**, and a teacher is allocated to *a subject within a
   class*. Allocating on the pair rather than on the subject alone is what makes "teaches
   Algorithms in CS-101 but not in CS-102" expressible.
3. **"Update a submission before the deadline, if allowed"** is governed by
   `Assignment.AllowUpdateBeforeDeadline`, **and** closes early once a teacher has graded the
   work. See [Business rules](#business-rules).
4. **Late submissions** are accepted only if `Assignment.AllowLateSubmission` is true, and are
   permanently flagged `Late` rather than `Submitted`.
5. **One submission per student per assignment.** Updates replace the content of that row; they
   never create a second one. Enforced by a unique index as well as by the service.
6. **Deleting a user deactivates them.** Hard deletion would take their submissions with it, or
   leave work with no author. `DELETE /users/{id}` sets `IsActive = false`, and an inactive user
   cannot log in or refresh. An admin cannot deactivate their own account.
7. **Deleting an assignment that has submissions is refused** (409) rather than cascading. The
   foreign key would happily take students' work with it.
8. **A `RefreshToken` table was added** beyond the specified data model, because refresh tokens
   must be stored, rotated and revocable rather than merely signed. Only a hash is stored.
9. **Replaying a revoked refresh token revokes the whole chain.** A replay means either an attack
   or a client bug, and in both cases ending every descendant session is the safe response.
10. **The `Marks` upper bound is enforced in the service layer, not as a DB `CHECK`**, because a
    PostgreSQL check constraint on `Submission` cannot reference `Assignment.MaxMarks`. The lower
    bound (`>= 0`) *is* a real database constraint.
11. **Admin oversight is read-only** for assignments and submissions. Editing another teacher's
    work would cut across rule 4, and the API would refuse it in any case.
12. **A teacher can read their own allocations** (`GET /teacher-assignments/mine`) even though the
    rest of that controller is admin-only. Without it a teacher has no way to discover which
    subject and class to create an assignment against, since both catalogues are admin-only.

---

## Known limitations

Stated plainly rather than left to be discovered.

- **The access token is in `localStorage`.** The browser calls the API directly, so the token must
  be readable by JavaScript to reach an `Authorization` header; an httpOnly cookie is not
  available to this architecture. An XSS bug would therefore expose a token. The mitigations are
  short access-token lifetimes (15 minutes) and refresh rotation with reuse detection, so a
  stolen pair is short-lived and detectable rather than permanent.
- **Enums cross the wire as integers.** No `JsonStringEnumConverter` is registered, so
  `status: 2` rather than `status: "Published"`. The frontend mirrors the numeric values with
  label maps. Strings would document better in Swagger; this is a contract change rather than a
  cosmetic one and was left alone rather than made late.
- **The strict query-parameter guard covers the six assignment and submission list endpoints
  only.** The admin catalogue lists (`users`, `classes`, `subjects`, `enrollments`,
  `teacher-assignments`) still ignore unknown parameters silently.
- **`StudentAssignmentDto` carries `TeacherName` but not `TeacherId`**, while
  `GET /assignments/available` accepts a `teacherId` filter. A student UI has no way to discover
  an id to filter by, so that filter is currently only reachable programmatically.
- **Client-side validation is not uniform.** Login, the assignment create/edit forms and the
  student submission form validate with zod before submitting. The admin quick-add panels
  (user, class, subject, allocation, enrolment) rely on the server's 422 and render the
  per-field messages it returns, so bad input is caught and explained but only after a round
  trip.
- **No in-app application settings screen.** "Manage application-level settings where
  necessary" is handled through environment configuration — token lifetimes, CORS origins,
  rate limits, seeding — rather than a UI. Nothing in the feature set needed a runtime-editable
  setting.
- **No attachment upload.** `AttachmentUrl` takes a link to work hosted elsewhere; file storage
  was out of scope.
- **No email, notifications or password reset.** An admin sets a password at creation time and
  there is no self-service way to change it.
- **No frontend unit tests.** The frontend is covered by TypeScript in strict mode, ESLint, a
  production build in CI, and manual verification of all three roles against the running stack.
  The automated testing effort went to the business rules, where the brief puts the emphasis.
- **Pagination is offset-based**, which is fine at this size and would drift under heavy
  concurrent writes.
- **The live API URL is ephemeral.** The frontend on Vercel has the tunnel URL baked in at build
  time; restarting the tunnel requires a rebuild with the new URL. The stack remains fully
  functional offline via Docker.

---

<sub>© 2026 Rakib Hassan · Evaluation build — not licensed for production use · sig:a24a5edb253940aa</sub>