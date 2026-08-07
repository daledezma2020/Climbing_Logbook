# Authorization

Last updated: August 7, 2026

This system uses Auth0 for sign-in and uses access tokens to protect actions in the API.

The short version is: Auth0 confirms who the user is, the frontend receives a temporary access token from Auth0, and the backend checks that token before allowing protected changes.

This is a common production pattern for modern web apps. Instead of building our own password system, token issuer, password reset flow, and account security features, we use Auth0 as the identity provider. The application focuses on climbing logbook behavior, while Auth0 handles the specialized identity work.

The backend still enforces security. It does not blindly trust the frontend. It validates Auth0-issued tokens before allowing protected API actions.

## What Auth0 Handles

Auth0 is the trusted identity provider for the app. That means the application does not manage passwords directly.

Auth0 handles:

- Sign in
- Sign up
- Password management
- Returning the user to the app after login
- Issuing access tokens that prove the user has signed in

This keeps password and identity management outside of the app itself. The app only needs to know whether Auth0 says the user is authenticated.

## Frontend Authorization Flow

The React frontend is wrapped in an Auth0 provider in `web/src/main.tsx` using `web/src/auth/Auth0ProviderWithConfig.tsx`.

That provider gives the rest of the frontend access to the user's authentication state, including:

- Whether the user is signed in
- The user's profile information, such as name, email, and avatar
- A way to send the user to Auth0 for sign-in or sign-up
- A way to get an access token for API requests

The navbar uses this state to show the correct controls:

- Signed-out users see **Sign in** and **Sign up**
- Signed-in users see their profile information and **Sign out**

When the user starts the sign-in flow, the app redirects them to Auth0. After Auth0 completes login, the user is sent back to the frontend.

## Sending Protected Requests

Public data can still be loaded without a token. For example, users can view existing climbs and logbook entries without being signed in.

Protected actions require the frontend to include an access token with the API request. Examples include:

- Creating a manual climb
- Importing a climb
- Deleting a climb
- Creating a log entry
- Creating or importing a place
- Creating, updating, or deleting setters

For those actions, the frontend asks Auth0 for an access token, then sends that token to the backend in the request header:

```text
Authorization: Bearer <access-token>
```

In plain language, the token acts like a temporary badge. The frontend shows that badge to the backend when asking to perform a protected action.

## Backend Authorization Flow

The ASP.NET Core API is configured to trust Auth0-issued JWT bearer tokens in `api/Program.cs`.

The API configuration includes:

- The Auth0 domain, which identifies the Auth0 tenant that issued the token
- The Auth0 audience, which identifies this API as the intended recipient of the token

When a request reaches the backend, the API checks whether the requested endpoint requires authorization.

If the endpoint is public, the request can continue without a token.

If the endpoint is protected, the API expects a valid bearer token. The backend validates that token before running the endpoint logic.

The token must be:

- Issued by the configured Auth0 tenant
- Intended for this API
- Valid and not expired

If the token passes validation, the backend allows the request. If the token is missing or invalid, the backend rejects the request.

## Protected API Endpoints

Protected endpoints are marked with `[Authorize]` in the API controllers.

Currently protected areas include:

- `ClimbsController`: creating manual climbs, importing OpenBeta climbs, and deleting climbs
- `LogEntriesController`: creating log entries
- `PlacesController`: creating places and importing OpenStreetMap places
- `SettersController`: creating, updating, and deleting setters
- `AuthController`: reading the current authenticated user's profile from the token

Read-only endpoints are mostly left public so visitors can browse existing data without needing an account.

## How Frontend and Backend Connect

The complete flow looks like this:

1. The user clicks **Sign in** or **Sign up** in the frontend.
2. The browser is redirected to Auth0.
3. Auth0 authenticates the user.
4. Auth0 redirects the user back to the frontend.
5. The frontend asks Auth0 for an access token.
6. The frontend sends that token with protected API requests.
7. The backend validates the token.
8. If the token is valid, the backend allows the protected action.

This means both sides of the app have clear responsibilities.

The frontend is responsible for starting login, showing the correct signed-in or signed-out UI, and attaching the token when needed.

The backend is responsible for deciding which actions require authorization and rejecting protected requests that do not include a valid token.

## Configuration

The frontend and backend both need Auth0 configuration, but they do not read it from the same place.

The frontend reads configuration from `web/.env.local`. The backend reads configuration from ASP.NET Core configuration, and local development values should be stored in .NET user secrets.

The frontend `.env.local` file does not configure the backend. The backend user secrets do not configure the frontend. A new developer needs to set up both.

### Where to Find the Auth0 Values

The values come from the Auth0 dashboard.

For the frontend:

- `VITE_AUTH0_DOMAIN`: Auth0 Application domain
- `VITE_AUTH0_CLIENT_ID`: Auth0 Application client ID
- `VITE_AUTH0_AUDIENCE`: Auth0 API audience

For the backend:

- `Auth0:Domain`: Same Auth0 domain, without `https://` and without a trailing slash
- `Auth0:Audience`: Same Auth0 API audience used by the frontend

The Auth0 domain usually looks like this:

```text
your-tenant.region.auth0.com
```

The backend code adds `https://` and the trailing slash itself in `api/Program.cs`.

### Frontend Local Setup

The frontend expects Auth0 and API settings in `web/.env.local`, using `web/.env.example` as the template:

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

These values are bundled into the browser app. Do not put backend secrets, database credentials, private keys, or Auth0 client secrets in `web/.env.local`.

### Backend Local Setup

The backend expects matching Auth0 settings from ASP.NET Core configuration. For local development, use .NET user secrets from the `api/` directory:

```bash
cd api
dotnet user-secrets set "Auth0:Domain" "your-tenant.region.auth0.com"
dotnet user-secrets set "Auth0:Audience" "https://climbing-logbook-api"
```

You can confirm the values are present with:

```bash
dotnet user-secrets list
```

The committed `api/appsettings.json` file only contains placeholders:

```json
{
  "Auth0": {
    "Domain": "",
    "Audience": ""
  }
}
```

Do not commit real Auth0 production values, connection strings, credentials, tokens, or secrets.

### Local Setup for Kangentic Worktrees

Kangentic creates each task in a fresh `git worktree` checkout. A worktree only contains files that git tracks, so gitignored files like `web/.env.local` are not carried over. Without extra setup, a new worktree starts with no frontend Auth0 configuration and the app shows the "Auth0 configuration required" screen.

Kangentic can seed those files for you. In the main repo's `.kangentic/config.json`, list `web/.env.local` under `git.copyFiles`:

```json
"git": {
  "copyFiles": ["web/.env.local"]
}
```

The same setting is available in the Kangentic UI under Settings -> Git -> Copy Files, which takes a comma-separated list. Kangentic copies each entry into the new worktree at the same relative path, right after the worktree is created.

A few things to know about how `copyFiles` behaves:

- Entries are repo-relative paths to individual files. Globs and directories are not supported, so each file has to be listed on its own.
- Missing parent directories in the worktree are created automatically.
- If a source file does not exist, it is skipped silently. A typo in the path produces no error, just a worktree that is still missing the file.
- Entries starting with `.claude/` are ignored, so that directory cannot be seeded this way.

The backend needs nothing extra. .NET user secrets are keyed by the `UserSecretsId` in `api/api.csproj` and stored machine-globally, so every worktree checks out the same csproj and resolves the same secrets file.

Note that `.kangentic/config.json` is gitignored. This is a per-machine setting, so anyone cloning the repo has to set it up for themselves.

### Matching Frontend and Backend Values

The frontend audience and backend audience need to match. That is how Auth0 and the API agree that a token was meant for this backend.

In local development, the same Auth0 API audience should appear in both places:

```text
web/.env.local:
VITE_AUTH0_AUDIENCE=https://climbing-logbook-api

api user secrets:
Auth0:Audience = https://climbing-logbook-api
```

The Auth0 domain should also refer to the same Auth0 tenant:

```text
web/.env.local:
VITE_AUTH0_DOMAIN=your-tenant.region.auth0.com

api user secrets:
Auth0:Domain = your-tenant.region.auth0.com
```

## Important Files and Directories

These are the main places to look when reviewing or changing authorization.

### Frontend

- `web/src/auth/Auth0ProviderWithConfig.tsx`: Reads the Auth0 frontend configuration and wraps the React app with Auth0 support.
- `web/src/main.tsx`: Mounts the React app and places the Auth0 provider around the application.
- `web/src/components/layout/Navbar.tsx`: Shows sign-in, sign-up, sign-out, and signed-in user information.
- `web/src/lib/api.ts`: Central API helper that can attach an Auth0 access token to protected requests.
- `web/src/hooks/catalog-hooks.ts`: Contains frontend data actions for climbs, log entries, places, and imports. Protected actions get an access token before calling the API.
- `web/src/hooks/useAuthenticatedApi.ts`: Small helper for making authenticated API requests.
- `web/src/pages/LogClimb.tsx`: Example page that requires sign-in before letting a user log or import a climb.
- `web/src/components/log/LogEntryForm.tsx`: Gets an access token before creating a log entry.
- `web/src/components/log/ManualClimbForm.tsx`: Gets an access token before creating a manual climb.
- `web/.env.example`: Documents the frontend environment variables needed for Auth0 and the API base URL.

### Backend

- `api/Program.cs`: Configures JWT bearer authentication, authorization, CORS, and the Auth0 domain/audience settings used to validate tokens.
- `api/appsettings.json`: Contains placeholders for the backend Auth0 configuration.
- `api/api.csproj`: Includes the ASP.NET Core JWT bearer authentication package.
- `api/Controllers/AuthController.cs`: Provides a protected endpoint that returns basic information about the authenticated user from the token.
- `api/Controllers/ClimbsController.cs`: Protects create, import, and delete climb actions with `[Authorize]`.
- `api/Controllers/LogEntriesController.cs`: Protects log entry creation with `[Authorize]`.
- `api/Controllers/PlacesController.cs`: Protects place creation and import actions with `[Authorize]`.
- `api/Controllers/SettersController.cs`: Protects setter create, update, and delete actions with `[Authorize]`.

### Auth0 Configuration Outside the Repository

Some required authorization setup lives in the Auth0 dashboard rather than in this codebase.

Important Auth0 settings include:

- The frontend application's client ID
- The Auth0 domain
- The API audience
- Allowed callback URLs
- Allowed logout URLs
- Allowed web origins

Those Auth0 dashboard values need to match the environment variables used by the frontend and backend.

## Current Scope

The current authorization model answers this question:

> Is this person signed in?

That is enough to protect actions from anonymous users.

It does not yet fully answer:

> Does this signed-in person own this exact record?

To support per-user ownership, the backend would need to store the Auth0 user identifier on records such as log entries, then check that identifier before returning, editing, or deleting user-specific data.

In other words, the current system protects write actions behind login, but future work may be needed for user-specific private data and role-based permissions.

## Scalability and Future Environments

If the system grows to support multiple environments, each environment should have its own Auth0 configuration.

For example, the app may eventually have:

- Local development
- Staging or test
- Production

Each environment usually needs its own Auth0 Application. In many cases, each environment should also have its own Auth0 API audience.

This keeps each environment separated. Local development can use `http://localhost:5173`, while production can use the real deployed website URL. Those environments should not accidentally share callback URLs, logout URLs, client IDs, or API audiences in a way that could send users to the wrong place or cause one environment to trust tokens meant for another.

The system does not necessarily need a completely separate Auth0 tenant for every environment. A single Auth0 tenant can contain multiple Applications and APIs. Separate tenants are more useful when production needs stronger isolation, separate ownership, stricter security boundaries, or cleaner operational separation.

A practical future setup would be:

- Local or development: separate Auth0 Application and API audience
- Staging: separate Auth0 Application and API audience
- Production: separate Auth0 Application and API audience

In plain language:

> We can keep using Auth0 as the identity provider, but each environment should have its own Auth0 settings so development, staging, and production do not accidentally trust the wrong app or redirect users to the wrong place.
