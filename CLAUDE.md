# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

Climbing Logbook is a rock climbing session logbook and social media web app. It's a two-part monorepo:

- `api/` — ASP.NET Core (.NET 8) REST API backed by PostgreSQL via EF Core
- `web/` — React 19 + TypeScript SPA built with Vite

Note: `readme.md` says .NET 9, but `api/api.csproj` targets `net8.0` (the real target). The web `dev` server runs on `http://localhost:5173`; the API runs on `http://localhost:5050` (HTTP) / `https://localhost:7191` (HTTPS). The frontend hardcodes `http://localhost:5050/api` as the API base URL in the hook files.

## Commands

### Backend (`api/`)
```bash
dotnet run                                  # run API (auto-applies pending EF migrations on startup)
dotnet build                                # build
dotnet ef migrations add <Name>            # create a new migration
dotnet ef database update                  # apply migrations manually (also auto-applied on startup)
```
There are no backend tests in the repo.

The DB connection string (`DefaultConnection`) is stored in .NET user secrets (UserSecretsId in `api.csproj`), not in committed config. Swagger UI is available at `/swagger` in Development.

### Frontend (`web/`)
```bash
npm install
npm run dev        # Vite dev server on :5173
npm run build      # tsc -b && vite build
npm run lint       # eslint
npm run preview    # preview production build
```

## Architecture

### Backend — layered controller/service/EF pattern
Each domain entity (ClimbRoute, Location, Setter) follows the same flow:

`Controller` → `IXService` interface (DI-registered in `Program.cs`) → `XService` (holds `ApplicationDbContext`) → EF Core → PostgreSQL.

- **Controllers** (`Controllers/`) are thin: validate `ModelState`, map DTO ↔ model, delegate to the service, and wrap everything in try/catch that logs and returns 500. Routes follow `api/[controller]`.
- **Services** (`Services/`) contain all data access; controllers never touch `ApplicationDbContext` directly.
- **DTOs** (`DTO/`) are used for inbound POST/PUT bodies (e.g. `ClimbRouteDto`); controllers manually map DTO fields onto entity models. When adding a field to an entity, update the model, the DTO, **and** both the create and update mapping blocks in the controller — `UpdateClimbRouteAsync` copies fields explicitly, so a missing field there silently won't persist.
- **Models** (`Models/`) use DataAnnotations for validation. Relationships are configured in `ApplicationDbContext.OnModelCreating`: ClimbRoute→Comments cascades on delete; ClimbRoute→Location and ClimbRoute→Setter use `Restrict` and have nullable FKs.
- **Migrations** are auto-applied at startup in `Program.cs` (`db.Database.Migrate()` for any pending migrations). JSON responses use camelCase.
- **Seeding**: `SeedingController` / `SeedingService` load `seeding/*.json` (benchmarks, locations, setters — Moonboard data) via POST endpoints under `api/seeding`.

### Frontend — pages + custom data hooks
- **Routing** (`src/App.tsx`): `createBrowserRouter` with a shared `Layout`; pages live in `src/pages/` (`Home`, `Routes`, `CreateRoute`, `EditRoute`).
- **Data fetching**: custom hooks in `src/hooks/` (`climbroute-hooks.ts`, `location-hooks.ts`, `setter-hooks.ts`) wrap `fetch` against the hardcoded `http://localhost:5050/api` base. Hooks own loading/error state and refetch after mutations. No React Query / state library — mutations call refetch manually.
- **Types** in `src/types/` mirror the API models/DTOs (e.g. `CreateClimbRouteInput`, `UpdateClimbRouteInput`).
- **UI**: shadcn/ui-style components in `src/components/ui/` (Radix UI + Tailwind v4 + class-variance-authority), configured via `components.json`. Forms use react-hook-form + zod. Shared form logic lives in `src/components/form components/RouteForm.tsx`; modals like `AddLocationModal`/`AddSetterModal` are reused across create/edit.
- The `@` alias resolves to `src/` (set in `vite.config.ts` and tsconfig).

## Conventions
- API base URL is hardcoded in the frontend hooks (`http://localhost:5050/api`) — change there if the API port changes.
- CORS in `Program.cs` only allows `http://localhost:5173`.
