# Climbing Logbook

Rock climbing session logbook and social media web app.

## Tech Stack

**Frontend:** React 19, TypeScript, Vite
**Backend:** ASP.NET Core 8, C#

## Setup

### Auth0 configuration

This repo needs Auth0 configuration in two places because the frontend and backend read configuration from different systems.

The frontend reads Auth0 values from `web/.env.local`. Copy `web/.env.example` and fill in the values from the Auth0 dashboard:

```bash
cd web
cp .env.example .env.local
```

```text
VITE_AUTH0_DOMAIN=your-tenant.region.auth0.com
VITE_AUTH0_CLIENT_ID=your_spa_client_id
VITE_AUTH0_AUDIENCE=https://climbing-logbook-api
VITE_API_BASE_URL=http://localhost:5050
```

The backend reads Auth0 values from ASP.NET Core configuration. For local development, use .NET user secrets:

```bash
cd api
dotnet user-secrets set "Auth0:Domain" "your-tenant.region.auth0.com"
dotnet user-secrets set "Auth0:Audience" "https://climbing-logbook-api"
```

The Auth0 domain and client ID come from the Auth0 Application. The audience comes from the Auth0 API. The frontend `VITE_AUTH0_AUDIENCE` and backend `Auth0:Audience` must match.

See `docs/authorization.md` for more detail.

### Backend (API)
```bash
cd api
dotnet restore
dotnet run
```
API runs on `http://localhost:5050` with the default local profile.

### Frontend (Web)
```bash
cd web
npm install
npm run dev
```
Web app runs on `http://localhost:5173`

## Tests

Run fast backend tests from the repository root:

```bash
dotnet test api.Tests --filter "Category=Unit"
```

Backend integration tests use real PostgreSQL behavior. Set `CLIMBING_LOGBOOK_TEST_CONNECTION_STRING` to a dedicated database whose name contains `test`, then run:

```bash
dotnet test api.Tests --filter "Category=Integration"
```

Run frontend tests from `web/`:

```bash
npm test
npm run test:coverage
```

Whenever application behavior changes, check its test impact and update the corresponding tests. If automation is not appropriate, document the manual verification in the pull request.

## Development

- API includes Swagger UI at `/swagger` in development mode
- CORS configured to allow frontend at `localhost:5173`
- Database on local during development is PostgresSQL (Using user secrets to store connection string)

## Image storage

Avatars are the only images this API stores. Everything else is still a URL string pointing at
an external host.

- `Climb.PictureUrl` and `Climb.VideoUrl` are URL-only. Nothing is uploaded or served for them.
- User avatars resolve in three tiers: `AppUser.PictureUrl` if set, otherwise the Auth0
  `picture` claim, otherwise generated initials.

### Avatar uploads

`POST /api/users/me/avatar` accepts a multipart `file` field and writes it to local disk.

- Accepted types are JPEG, PNG, and WebP. The type is determined by sniffing the file's magic
  bytes, and the stored extension comes from that sniff, never from the client-supplied
  filename.
- The default size cap is 2 MB.
- Files are written to the directory named by `Storage:AvatarPath` (default `uploads/avatars`,
  relative to the API content root) under a generated GUID filename, and served back at
  `/uploads/avatars/{file}`. Replacing an avatar deletes the previously stored file.
- Uploading sets `AppUser.PictureUrl` to the served URL, so the tiers above are unchanged.

```json
"Storage": {
  "AvatarPath": "uploads/avatars",
  "AvatarMaxBytes": 2097152
}
```

The upload directory is created on startup and is gitignored. This is local-disk storage only.
Moving to S3 or Azure Blob means adding another `IAvatarStorage` implementation; nothing outside
that interface assumes a filesystem.

## User endpoints

- `GET /api/users/{username}` - public profile with aggregate stats and recent activity.
- `GET /api/users/me` - the caller's own profile, including their email. Requires a bearer token.
- `PUT /api/users/me` - update display name, username, bio, avatar URL, and home place. The user
  is resolved from the token; a client-supplied id is ignored. Returns 409 when the username is
  already taken.
- `POST /api/users/me/avatar` - avatar upload, described above.
- `GET /api/users/me/stats` - the home page KPI payload. Requires a bearer token.
- `GET /api/users/{username}/logentries?skip=0&take=25` - that user's log entries, paged.
- `POST` / `DELETE /api/users/{username}/follow` - follow or unfollow a climber. Both are
  idempotent, and following yourself returns 400.
- `GET /api/users/{username}/followers` and `.../following?skip=0&take=25` - paged connections.

## Feed, likes, and comments

The home page shows a feed of log entries from the climbers you follow, plus your own, newest
first.

- `GET /api/feed?skip=0&take=25` - the caller's feed. Requires a bearer token.
- `GET /api/logentries/{id}` - a single log entry.
- `POST` / `DELETE /api/logentries/{id}/like` - like or unlike a logged climb. Idempotent in both
  directions; a second like is a no-op rather than an error.
- `GET` / `POST /api/logentries/{id}/comments` - the thread on one logged climb.
- `GET` / `POST /api/climbs/{id}/comments` - the thread on the climb itself, shown on `/climbs/:id`.
- `GET /api/climbs/{id}/logentries?skip=0&take=25` - every ascent logged against a climb.
- `DELETE /api/comments/{id}` - delete your own comment. Returns 403 for anyone else's.

A comment targets exactly one of a climb or a log entry, enforced in the database by the
`CK_Comments_Target` check constraint, so one table backs both threads. Likes attach to log
entries only and are unique per `(user, log entry)`.

Reads are public; every write requires a bearer token. The author of a comment or like is always
resolved from the token and never accepted from the client.

### Home page KPIs

`GET /api/users/me/stats` returns four groups: core send stats, recent activity, places and
disciplines, and social. The home page renders only the cards the user has enabled, defaulting to
the core group; that choice lives in the browser's `localStorage`, not on the server.

Hardest-grade comparisons go through the per-system ladders in `api/Services/GradeOrdering.cs`.
Comparing grades numerically is wrong: it ranks `5.9` above `5.14a` and cannot compare V-scale to
Font at all.
