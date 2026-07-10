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

Backend tests live in `api.Tests/`; frontend tests live beside source files as `*.test.ts` or `*.test.tsx`. Run backend unit tests with `dotnet test api.Tests --filter "Category=Unit"` and frontend tests with `npm test` from `web/`. PostgreSQL integration tests require `CLIMBING_LOGBOOK_TEST_CONNECTION_STRING` pointing to a dedicated database whose name contains `test`.

For every application change, explicitly check whether observable behavior, validation, API contracts, persistence, authentication, or UI workflows changed. Add or update the relevant tests whenever they did. If no test change is appropriate, record why in the pull request. Before opening a PR, run the affected tests plus `dotnet build`, `npm run lint`, and `npm run build`, then manually verify workflows that are not automated.

## Commit & Pull Request Guidelines

Agents must follow `.agents/rules/never-commit.md`: never create commits and leave staging and commits to the user unless the user explicitly requests staging in the current conversation.

Recent commits use short, imperative messages prefixed with `chore:`. Continue that pattern and use a more specific type such as `fix:` or `feat:` when appropriate. Keep each commit focused.

Pull requests should explain the behavior changed, list verification performed, and link related issues. Include screenshots for visible UI changes and note database migrations or configuration changes explicitly.

## Security & Configuration

Store the PostgreSQL `DefaultConnection` value with .NET user secrets or local development settings. Never commit credentials, tokens, or production connection strings.
