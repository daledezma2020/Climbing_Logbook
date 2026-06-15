# Repository Guidelines

## Project Structure & Module Organization

This repository contains two applications:

- `api/`: ASP.NET Core 8 Web API. Controllers define endpoints, services contain business logic, and `Data/` plus `Migrations/` manage PostgreSQL through Entity Framework Core. Models live in `Models/`; response shapes belong in `DTO/`.
- `web/`: React 19 and TypeScript client built with Vite. Page-level views are in `src/pages/`, reusable UI and layout code in `src/components/`, API hooks in `src/hooks/`, shared types in `src/types/`, and static files in `public/`.

Do not commit generated output from `web/dist/`, `api/bin/`, or `api/obj/`.

## Build, Test, and Development Commands

Run backend commands from `api/`:

```bash
dotnet restore       # Restore NuGet packages
dotnet build         # Compile the API
dotnet run           # Start the API and development Swagger UI
dotnet ef database update  # Apply EF Core migrations
```

Run frontend commands from `web/`:

```bash
npm ci               # Install the locked dependency set
npm run dev          # Start Vite at http://localhost:5173
npm run build        # Type-check and create a production build
npm run lint         # Run ESLint across TypeScript/React files
npm run preview      # Serve the production build locally
```

## Coding Style & Naming Conventions

Use four-space indentation for C# and follow standard .NET naming: `PascalCase` for public types and members, `camelCase` for locals and parameters, and `INameService` for interfaces. Keep controllers thin and put data access or business rules in services.

TypeScript runs in strict mode. Use `PascalCase` for components, `camelCase` for functions and hooks, and the `@/` alias for imports from `web/src`. Follow ESLint and avoid unused declarations.

## Testing Guidelines

No automated test project is configured. Before opening a PR, run `dotnet build`, `npm run lint`, and `npm run build`, then manually verify affected endpoints and browser workflows. Add backend tests in `api.Tests/` and frontend tests beside source files as `*.test.tsx`.

## Commit & Pull Request Guidelines

Recent commits use short, imperative messages prefixed with `chore:`. Continue that pattern and use a more specific type such as `fix:` or `feat:` when appropriate. Keep each commit focused.

Pull requests should explain the behavior changed, list verification performed, and link related issues. Include screenshots for visible UI changes and note database migrations or configuration changes explicitly.

## Security & Configuration

Store the PostgreSQL `DefaultConnection` value with .NET user secrets or local development settings. Never commit credentials, tokens, or production connection strings.
