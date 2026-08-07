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
- `GET /api/users/{username}/logentries?skip=0&take=25` - that user's log entries, paged.
