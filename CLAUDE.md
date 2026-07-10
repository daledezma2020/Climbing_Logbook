# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Climbing Logbook is a rock climbing session logbook and social app. It's a two-part monorepo:

- `api/` — ASP.NET Core (.NET 8) REST API backed by PostgreSQL via EF Core, secured with Auth0 JWT bearer auth
- `web/` — React 19 + TypeScript SPA built with Vite, authenticated via `@auth0/auth0-react`

Note: `readme.md` says .NET 9, but `api/api.csproj` targets `net8.0` (the real target). The web dev server runs on `http://localhost:5173`; the API runs on `http://localhost:5050` (HTTP) / `https://localhost:7191` (HTTPS).

## Commands

### Backend (`api/`)
```bash
dotnet run                                  # run API (auto-applies pending EF migrations on startup)
dotnet build                                # build
dotnet test ../api.Tests --filter "Category=Unit" # fast backend tests
dotnet ef migrations add <Name>            # create a new migration
dotnet ef database update                  # apply migrations manually (also auto-applied on startup)
```
PostgreSQL integration tests are in `api.Tests/Integration`. Set `CLIMBING_LOGBOOK_TEST_CONNECTION_STRING` to a dedicated database whose name contains `test`, then run `dotnet test ../api.Tests --filter "Category=Integration"`.

Secrets live in .NET user secrets (`UserSecretsId` in `api.csproj`), not committed config: the `DefaultConnection` connection string and the `Auth0:Domain` / `Auth0:Audience` values consumed in `Program.cs`.

Note: `dotnet build` may report file-copy/lock "errors" while the dev server is running — those are exe-copy locks, not compile failures.

### Frontend (`web/`)
```bash
npm install
npm run dev        # Vite dev server on :5173
npm run build      # tsc -b && vite build
npm run lint       # eslint
npm test           # run Vitest once
npm run test:coverage # run tests and create coverage reports
npm run preview    # preview production build
```
Frontend env vars (in `web/.env.local`, not committed): `VITE_API_BASE_URL` (defaults to `http://localhost:5050`), and required Auth0 config `VITE_AUTH0_DOMAIN`, `VITE_AUTH0_CLIENT_ID`, `VITE_AUTH0_AUDIENCE`. Without the Auth0 vars, `Auth0ProviderWithConfig` renders a config-required screen instead of the app.

## Test maintenance

Every application update must include an explicit test-impact check. When observable behavior, validation, API contracts, persistence, authentication, or UI workflows change, add or update the relevant backend or frontend tests in the same change. If no automated test change makes sense, document the reason and the required manual verification in the pull request.

## Architecture

### Domain model
The core entities are `Climb`, `Place`, `BoardConfiguration`, `Setter`, `LogEntry`, `Comment`, and the two external-reference join tables `ClimbExternalReference` / `PlaceExternalReference`. Enums live in `api/Models/Enums.cs` (`ClimbDiscipline`, `GradeSystem`, `PlaceKind`, `LogEntryStatus`, `ExternalProvider`).

Key rules configured in `ApplicationDbContext.OnModelCreating`:
- **A `Climb` belongs to exactly one context** — a `Place` (outdoor/gym), OR a `BoardConfiguration` (e.g. Moonboard), OR a custom location (`CustomLocationName` + lat/long). This is enforced by the `CK_Climbs_Context` check constraint, and re-validated in code by `ValidateClimbContext`. All three FK sets are nullable and mutually exclusive.
- Climb→Comments and Climb→LogEntries cascade on delete; Climb→Place/BoardConfiguration/Setter and Place→ParentPlace use `Restrict`.
- `ExternalProvider` + `ExternalId` is a unique index on both external-reference tables — this is how imported records are deduplicated.
- Migrations auto-apply at startup via `db.Database.Migrate()`. JSON is camelCase with enums serialized as strings (`JsonStringEnumConverter`).

### Backend — controller / service / EF pattern
Controllers are thin and delegate to services registered in `Program.cs`. Routes are `api/[controller]`.

- **`CatalogService` (`ICatalogService`) is the hub.** It backs the `Climbs`, `Places`, `BoardConfigurations`, `LogEntries`, and `Search` controllers, owns all catalog data access, and maps entities ↔ DTOs via the static `CatalogMapping`. All climb/place DTOs are defined together in `api/DTO/CatalogDtos.cs`.
- **External providers**: `OpenBetaClient` (`IOpenBetaClient`, registered as a typed `HttpClient`) imports climbs from OpenBeta; `OsmClient` (`IOsmClient`) imports places from OpenStreetMap. Import flows check the external-reference tables first, then geo-dedupe against existing places via `FindNearbyPlaceAsync` (haversine distance ≤ 250 m + normalized-name match) before inserting.
- **Search** (`CatalogService.SearchAsync`) fans out across providers in parallel (`local`, `openbeta`, optionally `osm`), tolerates per-provider failure (each is wrapped in `RunProviderAsync`, which records `complete`/`failed`/`skipped` in the response's `Providers` map), then dedupes results by `Key`.
- **`SetterService`** (`ISetterService`) resolves/creates setters by name; `CatalogService` uses it when a manual climb supplies a `SetterName` instead of an id. `ResolveBoardConfigurationAsync` does the same find-or-create by name for boards.
- **Seeding**: `SeedingController` / `SeedingService` load `api/seeding/*.json` (benchmarks, locations, setters — Moonboard data) via POST endpoints under `api/seeding`.
- When adding a field to a climb, update the model, `CreateManualClimbDto`, the entity-building block in `CreateManualClimbAsync`, and `CatalogMapping` — mapping is explicit in both directions, so a missed spot silently won't round-trip.

### Auth
- API: `Program.cs` configures JWT bearer auth against Auth0 (`Authority` = `https://{Auth0:Domain}/`, `Audience` = `Auth0:Audience`). Mutating endpoints (`POST`/`DELETE` on climbs, log entries, etc.) carry `[Authorize]`; reads are anonymous. `AuthController.Me` echoes the caller's claims.
- Web: `Auth0ProviderWithConfig` wraps the app in `main.tsx`. Two ways to attach a token: pass `accessToken` (obtained via `getAccessTokenSilently()`) into `apiFetch` options (see the mutation helpers in `catalog-hooks.ts`), or use the `useAuthenticatedApi().fetchWithAuth` helper. Read hooks call `apiFetch` without a token.

### Frontend — pages + custom data hooks
- **Routing** (`src/App.tsx`): `createBrowserRouter` with a shared `Layout`. Pages: `Home`, `Logbook`, `Climbs`, `LogClimb` (route `log/new`).
- **Data layer**: `src/lib/api.ts` exports `apiFetch<T>` (prepends `${VITE_API_BASE_URL}/api`, sets JSON headers, attaches optional bearer token, and normalizes error bodies into thrown `Error`s). Domain hooks in `src/hooks/catalog-hooks.ts` (`useClimbs`, `useLogEntries`, `usePlaces`, `useBoardConfigurations`, `useSearch`) own loading/error state and refetch after mutations. Mutation helpers (`createManualClimb`, `createLogEntry`, `importOpenBetaClimb`) are plain functions taking an `accessToken`. `useSearch` guards against out-of-order responses with a `requestId` ref. No React Query.
- **Types** in `src/types/` (`catalog.ts`, `setter.ts`) mirror the API DTOs.
- **UI**: shadcn/ui-style components in `src/components/ui/` (Radix + Tailwind v4 + class-variance-authority), configured via `components.json`. Forms use react-hook-form + zod. Feature components live in `src/components/log/` (`ManualClimbForm`, `LogEntryForm`, `LocationPicker`, `ResultBadges`, `ErrorToast`) and `src/components/layout/`.
- The `@` alias resolves to `src/` (set in `vite.config.ts` and tsconfig).

## Conventions
- CORS in `Program.cs` only allows `http://localhost:5173` (policy `AllowReactApp`).
- Change the API port in `web/.env.local` (`VITE_API_BASE_URL`), not in source.
