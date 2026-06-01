# Migration Progress

## Test project migration — `src/tests/TB.DanceDance.Tests`

Migrated the integration test project off the deleted `Application`/`Infrastructure` projects onto the
modular architecture. The whole solution builds again.

### What changed
- **csproj**: dropped `Application`/`Infrastructure` refs; added `TB.DanceDance.Access(.Contracts)`,
  `TB.DanceDance.Videos(.Contracts)`, `TB.DanceDance.Utilities`, `TB.DanceDance.API.Contracts`.
- **Fixture (`TestsFixture/DanceDbFixture.cs`)**: one Postgres container now backs both
  `AccessDbContext` and `VideosDbContext`; both are `MigrateAsync`'d (each owns disjoint schemas, no
  cross-module FK). Added `BuildServiceProvider(blobConn)` that wires
  `AddMediator().AddAccessModule().AddVideosModule()` + both `Add*ModuleInfrastructure` + blob factory.
- **`BaseTestClass`**: exposes `SeedAccessContext` / `SeedVideosContext` for arranging data and a
  `Send<TResponse>(IRequest<TResponse>)` helper that dispatches through the real DI wiring (resolves
  `IRequestHandler<,>` in a scope) — exercising the cross-module mediator path for real. Tests that hit
  blob storage override `BlobConnectionString`.
- **Builders (`TestDataBuilder`/`TestDataFactory`)**: rewritten to construct the now-private-ctor
  entities via their public `Factory.Create` methods; `SharedWith` rows are attached through the
  `Video.SharedWith` navigation (its `VideoId` is init-only). Seeding now splits across the two contexts.
- **Feature tests**: re-mapped from old services to handlers/queries (e.g. `AccessService` →
  `CanUserUploadTo*Request` / `DoesUserHasAccessToSharedWith` / `DoesUserHaveAccessToVideo*Query` /
  `ViewPrivateVideosQuery`; `VideoService`/`VideoUploaderService` → the upload/view/management handlers;
  `EventService.GetVideos` → `ViewVideosFromEventQuery`; `GroupService` → `ViewVideosFrom*Query`;
  `CommentService`/`SharedLinkService` → `CommentHandlers`/`SharedLinkHandlers`).
- **Migration tests**: `DanceDbMigrationTests` split into `AccessDbMigrationTests` + `VideosDbMigrationTests`
  (up + down-to-"0"). `BlobDataServiceTests` re-pointed to `TB.DanceDance.Utilities.Infrastructure`.
  Converter tests unchanged.

### Small production additions / fixes (filling gaps, matching the module's own convention)
- Added `Group.Factory.Create` and `GroupAdmin.Factory.Create` — both had private ctors and (unlike
  every sibling Access entity) no factory, so they were otherwise unconstructable from outside the module.
- Fixed `Event.Factory.Create` to assign `Id = Guid.NewGuid()` (it was the only Access factory that
  left `Id` default). `CreateEventCommandHandler` reads `@event.Id` to build the owner's
  `AssignedToEvent` *before* the event is tracked, so with a default `Id` the membership captured
  `Guid.Empty` and `SaveChanges` failed the `FK_AssignedToEvents_Events_EventId` constraint — a real
  bug the ported tests surfaced.

### Test execution
- The suite shares one Postgres container across all classes, and some handlers run global,
  side-effecting queries (notably the converter queue's `GetNextVideoToConvertQuery`, which picks and
  locks *any* eligible video). Added `[assembly: CollectionBehavior(DisableTestParallelization = true)]`
  so those globally-visible reads/writes don't race parallel collections. Full run: **187 passed,
  1 skipped, 0 failed** (~18 s with Docker warm).

### Adaptations the next person should know (the new handler surface is intentionally narrower)
- `VideoDto` carries no group id/name and no blob sizes; `CommentDto` carries no `SharedLinkId` and no
  resolved user display name (name resolution is an API-edge concern). Where old assertions used those,
  the ported tests assert on the persisted entity via the seed context, or on `UserId`/`VideoOwnerId`.
- `VideoUploaderService.GetUploadSasUri()` / `GetVideoSas()` have no standalone handler (internal to
  `CreateVideoUploadCommand` / streaming) — those two micro-tests were not ported.
- `VideoDataBuilder` no longer controls `Video.SharedDateTime` (init-only, set by the factory). The
  uploader "locks the newest" test was simplified to "returns the single eligible video and locks it".
- Dropped `GetUserEventsAndGroupsAsync_Throws_WhenUserIdIsNull` — the new `GetUserGroupsAndEvents`
  contract takes a `required` UserId and the handler's null behavior is not specified.

### Verify
`dotnet build` (solution is green). `dotnet test src/tests/TB.DanceDance.Tests/...` requires Docker
(Testcontainers: Postgres + Azurite).
