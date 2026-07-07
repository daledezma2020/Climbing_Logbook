# Federated Climb Catalog and Logbook Redesign

## Summary

Replace the catalog-only route form with a logbook workflow backed by a federated search.

- Search the local database, OpenBeta, and OSM concurrently.
- Present one result list with consistent fields plus `Source` and `Result type` badges.
- Use OpenBeta Typesense for global climb-name search and GraphQL for authoritative imports.
- Use OSM only for gyms and outdoor climbing places, not individual climbs.
- Let users create a missing climb with freeform text and a map pin.
- Separate canonical climbs from users' logbook entries.

## Revised Domain Model

The existing `ClimbRoute` and `Location` schemas should be reworked rather than extended in place.

### Climb

Replace `ClimbRoute` with a canonical `Climb`:

- `Id`
- `Name`
- `Discipline`: initially `Bouldering`
- `GradeSystem`: initially `VScale`
- `Grade`
- `PlaceId`, nullable
- `BoardConfigurationId`, nullable
- `SetterId`, nullable for board or gym climbs
- `FirstAscentName`, nullable for outdoor climbs
- `PictureUrl` and `VideoUrl`, retained as climb-level media
- `CreatedAt` and `UpdatedAt`

Require exactly one canonical context:

- Outdoor and gym climbs reference a `Place`.
- Board climbs reference a `BoardConfiguration`.
- A climb should not require both `PlaceId` and `BoardConfigurationId`.
- The physical gym where a board climb was attempted belongs on the log entry, not the canonical climb.

This means board configuration is not a property of a gym. The user can select a board climb by choosing the board configuration first, then optionally select a gym or other physical place on the log entry to record where they climbed it. A gym can later advertise available board configurations if that feature is useful, but v1 should not model that relationship.

Remove stored `AverageRating`; calculate it from log entries.

The schema should be built for future sport and trad support even though v1 only exposes bouldering. Use enums/tables for `Discipline` and `GradeSystem`, keep the selected grade as a string, and avoid hard-coding V-scale assumptions outside validation/UI helpers.

### LogEntry

Add a separate user activity record:

- `Id`
- `ClimbId`
- `PlaceId`, nullable physical venue for board climbs
- `OccurredAt`
- `Status`: `Attempted` or `Completed`
- `Rating`, nullable, 1-5
- `Notes`, nullable
- `CreatedAt` and `UpdatedAt`

The create form creates a `LogEntry`. It creates/imports a `Climb` first only when the selected result is not already local.

### Place

Replace geographic uses of `Location` with `Place`:

- `Id`
- `Name`
- `Kind`: `Gym`, `Outdoor`, or `Custom`
- `ParentPlaceId`, nullable for area hierarchy
- `Latitude` and `Longitude` as `double`
- `Address`, `City`, `State`, `Country`
- `CreatedAt` and `UpdatedAt`

OpenBeta areas, crags, sectors, and boulders should all map to `Outdoor`. They can still use `ParentPlaceId` for hierarchy, but the UI should not expose separate outdoor/crag/boulder kinds unless a future feature needs that distinction.

Coordinates are required for new mapped places but may remain absent on migrated legacy records.

### BoardConfiguration

Add:

- `Id`
- `Name`
- `Manufacturer`
- `Year`
- `CreatedAt`

Migrate records such as `Moonboard 2016` from `Locations` into this table.

Seeded MoonBoard data must be updated to create board configurations first, then seed MoonBoard climbs with `BoardConfigurationId` instead of a location/place. MoonBoard seed locations should no longer become geographic records.

If future boards are added, seed data should use a stable external reference per board configuration and route so repeated seeding remains idempotent.

### Provenance

Do not add a single `Source` column. A climb or place can be represented by multiple providers.

Add:

- `ClimbExternalReference`
- `PlaceExternalReference`

Each contains:

- Local entity ID
- `Provider`: `OpenBeta`, `OpenStreetMap`, or `MoonBoardSeed`
- `ExternalId`
- `ExternalUrl`, nullable
- `RetrievedAt`

Enforce uniqueness on `(Provider, ExternalId)`.

## Federated Search

Add:

```http
GET /api/search?q={text}&bbox={west,south,east,north}&limit={number}
```

The backend starts all applicable providers concurrently using `Task.WhenAll`:

- Local database: search canonical climbs and places.
- OpenBeta Typesense: global climb and area name search.
- OSM Overpass: search climbing gyms and outdoor climbing places inside the active map bounds.

Return a homogeneous contract:

```ts
interface SearchResult {
  key: string;
  resultType: "climb" | "place";
  name: string;
  sources: ("local" | "openbeta" | "osm" | "moonboard")[];
  grade?: { system: "vscale" | "yds" | "french" | "font" | "other"; value: string };
  discipline?: "bouldering" | "sport" | "trad" | "other";
  placeName?: string;
  placeKind?: "gym" | "outdoor" | "custom";
  coordinates?: { latitude: number; longitude: number };
  localId?: number;
  externalId?: string;
}
```

Also return provider statuses so one outage does not fail the entire search:

```ts
providers: {
  local: "complete" | "failed";
  openbeta: "complete" | "failed";
  osm: "complete" | "failed" | "skipped";
}
```

### Result Handling

- Local climb: use its existing ID.
- OpenBeta climb: fetch authoritative details through GraphQL, import/upsert its place and climb, then create the log entry.
- OSM place: select or import the place, then search again or create a missing climb there.
- Freeform option: preserve typed text, require a V-grade and pin, then transactionally create the place, climb, and log entry.
- Board climb option: select a board configuration and climb, then optionally attach the log entry to a physical gym/place.

Display badges such as `Local`, `OpenBeta`, `OpenStreetMap`, `Climb`, and `Gym`.

## Matching and Deduplication

- Resolve exact external-reference matches first.
- OpenBeta imports with an existing climb UUID return the local climb.
- OSM imports with an existing node/way/relation ID return the local place.
- Match places by normalized name, compatible kind, and coordinates within 250 metres.
- Automatically reuse a place only when exactly one strong match exists.
- Show a confirmation choice for ambiguous matches.
- Do not automatically merge climbs using fuzzy names alone.

## API Additions

- `GET /api/search`
- `GET /api/catalog/openbeta/climbs/{uuid}`
- `POST /api/catalog/openbeta/climbs/{uuid}/import`
- `POST /api/catalog/osm/places/{type}/{id}/import`
- `POST /api/climbs/manual`
- `POST /api/log-entries`
- CRUD endpoints for log entries
- Read endpoints return embedded climb/place summaries so the frontend no longer joins separate collections by ID.

All third-party access runs through backend clients with caching, timeouts, configurable URLs, and partial-failure handling.

## Migration

- Rename/migrate `ClimbRoutes` into `Climbs`.
- Migrate MoonBoard location records into `BoardConfigurations`.
- Link seeded climbs to their board configuration.
- Update `SeedingService` and seed files so MoonBoard locations are no longer seeded as `Places`; board configuration seeding should be idempotent and route seeding should reference board configuration IDs.
- Convert real geographic locations into `Places`.
- Preserve existing climb media, comments, setters, timestamps, and ratings.
- Convert an existing nonzero rating into an initial legacy log entry so rating data is not lost.
- Mark seeded records with `MoonBoardSeed` references.
- Remove the old location and rating columns only after migration verification.

## Test Plan

- Verify all search providers start concurrently and partial failures preserve other results.
- Verify result mapping and source/type badges.
- Verify OpenBeta Typesense discovery followed by GraphQL detail import.
- Verify local and OpenBeta duplicate suppression.
- Verify bounded OSM place and gym search.
- Verify board climbs can be logged at a physical gym without changing canonical ownership.
- Verify MoonBoard seeding creates board configurations and links seeded climbs to them without creating fake geographic places.
- Verify discipline and grade-system enums/contracts can represent sport and trad records even though v1 filters them out of the UI.
- Verify manual climb/place/log creation is transactional.
- Verify place matching and ambiguous-match confirmation.
- Verify migration preserves seeded climbs, comments, media, setters, and ratings.
- Run `dotnet build`, backend tests, `npm run lint`, and `npm run build`.

## Assumptions and Risks

- Version one supports creating/logging bouldering and V-scale grades only.
- Backend contracts and persistence should allow future sport/trad disciplines and non-V-scale grade systems without another foundational schema redesign.
- OSM contributes places, not climb-name results.
- OpenBeta's website uses a public Typesense index for climb search, separate from GraphQL.
- The Typesense integration is public but not a formally stable API; isolate it behind an adapter and allow its configuration to change without a frontend deployment.
- OpenBeta and public OSM services have no assumed SLA; local and manual workflows must remain usable during outages.

## References

- [OpenBeta API](https://github.com/OpenBeta/openbeta-graphql)
- [OpenBeta frontend search implementation](https://github.com/OpenBeta/open-tacos/blob/develop/src/js/typesense/TypesenseClient.ts)
- [OSM climbing schema](https://wiki.openstreetmap.org/wiki/Climbing)
- [Overpass usage guidance](https://dev.overpass-api.de/overpass-doc/en/preface/commons.html)
