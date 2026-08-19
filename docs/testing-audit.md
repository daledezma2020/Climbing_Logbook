# Testing Audit

Last updated: July 10, 2026

## Strategy

The suite tests behavior at public application seams. Fast backend tests cover mapping, claims, and HTTP integrations; frontend tests cover API requests, hooks, forms, pages, authentication controls, and map search. Database behavior uses PostgreSQL and real EF Core migrations because the application depends on relational constraints and PostgreSQL-specific `ILike` queries.

External Auth0, OpenBeta, OpenStreetMap, and Nominatim calls are never made by automated tests. Coverage is reported but is not currently a merge threshold.

## Feature coverage

| Area | Automated coverage | Level |
| --- | --- | --- |
| Climb contexts and DTO mapping | Place, board, custom location, ratings, setters, and sources | Unit + integration |
| Catalog persistence | Create, retrieve, validation, ordering paths, delete, and cascade behavior | PostgreSQL integration |
| Logbook persistence | Create, validation, returned summaries, ratings, and cascade deletion | PostgreSQL integration |
| External imports | Response parsing, filtering, idempotency, references, and nearby-place reuse | Unit + integration |
| Federated search | Local search, limit behavior, stale frontend requests, and provider degradation | Unit + integration |
| Setter service | Create, find, update, existence, delete, and missing records | PostgreSQL integration |
| API request helper | JSON/auth headers, empty responses, validation details, and network failures | Frontend unit |
| Climb/log forms | Validation, payloads, authentication token use, callbacks, and errors | Component |
| Catalog/dashboard UI | Statistics, search filtering, and pagination | Component |
| Authentication UI | Authenticated/visitor navigation actions and user display | Unit + component |
| Location search | Successful selection and recoverable search failure | Component |
| Following | Idempotent follow/unfollow, self-follow rejection, counts, paged follower and following lists, and caller relationship flags | Unit + PostgreSQL integration |
| User search | Partial case-insensitive username and display-name matching, limit clamping, blank queries, and debounced/stale frontend requests | Unit + integration + component |
| Follow UI | Optimistic toggle with rollback, error surfacing, and sign-in redirect for visitors | Component |

## Deliberate exclusions

- Generated UI primitives and static styling are not tested independently; their consuming workflows are tested.
- Live third-party APIs and Auth0 token validation are excluded from deterministic CI.
- Browser end-to-end and visual-regression tests are not part of this first suite.
- Seeding is not automated yet because it depends on process-wide current-directory state and uses creation timestamps to infer whether records were added.
- Full authorization ownership is not testable because records do not yet store an Auth0 subject; see `docs/authorization.md`.

## Audit findings

These findings are documented rather than changed by the test-suite work:

1. **High — seeding endpoints are not protected.** `SeedingController` exposes write operations without `[Authorize]`.
2. **High — authorization is authentication-only.** Any authenticated user can currently mutate shared records; per-user ownership is not enforced.
3. **Medium — invalid service input can become HTTP 500.** Catalog validation uses `InvalidOperationException`, while most catalog controllers do not translate it into a client error.
4. **Medium — seeding is not reliably repeatable.** Setter seeding inserts duplicates, and location counts depend on a ten-second `CreatedAt` window.
5. **Medium — startup continues after migration failure.** `Program.cs` catches migration exceptions and starts the API, potentially leaving it available with an incompatible schema.
6. **Low — catalog pagination icon buttons have no accessible names.** Screen-reader users cannot distinguish previous and next actions.
7. **Low — place and board hooks do not consistently expose errors.** Some frontend load failures can only surface as empty data or unhandled promise rejection.

## Running the suite

```bash
dotnet test api.Tests --filter "Category=Unit"
dotnet test api.Tests --filter "Category=Integration"
cd web
npm test
npm run test:coverage
```

Set `CLIMBING_LOGBOOK_TEST_CONNECTION_STRING` before integration tests. It must point to a disposable PostgreSQL database whose name contains `test`; the suite drops and recreates that database between scenarios.

## Maintenance rule

Every application update requires a test-impact check. Update tests whenever observable behavior, validation, API contracts, persistence, authentication, or UI workflows change. When automation is not appropriate, record the reason and manual verification in the pull request.
