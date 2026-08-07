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

There is no server-side image storage in this application. Every image is a URL string
pointing at an external host, and nothing is uploaded to or served by this API.

- User avatars default to the Auth0 `picture` claim. `AppUser.PictureUrl` holds an optional
  user-supplied URL that overrides it.
- `Climb.PictureUrl` and `Climb.VideoUrl` work the same way.

Supporting real file uploads is deliberately deferred. It is not a small addition, and none of
it exists yet:

- an upload endpoint on the API
- file-type and file-size validation
- a storage backend (S3, Azure Blob, or local disk) with the matching configuration
- a serving path for the stored files

This is a prerequisite for the avatar upload button on the profile page. Until it is built,
the profile page can only accept a URL.
