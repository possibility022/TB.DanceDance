# Contract: Competition management endpoints

Base path: `api/competitions`. Auth: bearer JWT, scope `tbdancedanceapi.read` (same policy as the
My Videos endpoints). All routes operate only on competitions owned by the caller (FR-007).

| Method | Path | Request | Response | Notes |
|---|---|---|---|---|
| POST | `api/competitions` | `CreateCompetitionRequest { Name, Date?, Location?, CommentVisibility }` | `CompetitionResponse` | Creates a competition owned by the caller (FR-001). 400 if `Name` empty/whitespace. |
| GET | `api/competitions` | — | `ListMyCompetitionsResponse { Competitions: CompetitionSummaryResponse[] }` | Caller's competitions, newest first, each with a `VideoCount` (FR-008). |
| GET | `api/competitions/{competitionId}` | — | `CompetitionResponse` | One competition with its grouped videos. 404/empty if not found or not owned. |
| PATCH | `api/competitions/{competitionId}` | `RenameCompetitionRequest { NewName }` | 200 / 404 | Rename (FR-004). 404 if not found or not owned; 400 if `NewName` empty/whitespace. |
| DELETE | `api/competitions/{competitionId}` | — | 200 / 404 | Deletes the competition; every grouped video's `CompetitionId` is cleared first — videos are never deleted (FR-006). 404 if not found or not owned. |
| PUT | `api/competitions/{competitionId}/videos/{videoId}` | — | 200 / 400 | Adds the caller's own video to the competition (FR-002). 400 if the video is not owned by the caller, or is already grouped into a different competition (FR-003). Idempotent if the video is already in *this* competition. |
| DELETE | `api/competitions/{competitionId}/videos/{videoId}` | — | 200 / 404 | Removes a video from the competition; the video becomes standalone (FR-005). 404 if the competition isn't owned by the caller or the video isn't currently in it. |

### `CompetitionResponse`

```json
{
  "id": "guid",
  "name": "string",
  "date": "datetime?",
  "location": "string?",
  "commentVisibility": 0,
  "createdDateTime": "datetime",
  "videos": [ /* VideoInformation, same shape used elsewhere for a video listing */ ]
}
```

### `CompetitionSummaryResponse`

Same shape as `CompetitionResponse` but with `videoCount: number` instead of `videos`.

`commentVisibility` is an int: `0 = AuthenticatedOnly`, `1 = OwnerOnly` (default), `2 = Public` —
identical encoding to `Video.CommentVisibility`.

---

# Contract: Competition sharing endpoints

Base path: `api/competitions/{competitionId}/share` (create) and the existing `api/share/{linkId}`
family (resolve/stream), per FR-009 through FR-013.

| Method | Path | Request | Response | Notes |
|---|---|---|---|---|
| POST | `api/competitions/{competitionId}/share` | `CreateSharedLinkRequest { ExpirationDays, AllowComments, AllowAnonymousComments }` | `SharedLinkResponse` | Owner-only (auth required). Produces one link for the whole competition (FR-009). 400 if the competition isn't found or isn't owned by the caller. |
| GET | `api/share/{linkId}` | — | `SharedVideoInfoResponse` | Unchanged route; when the link targets a competition, `isCompetition = true` and `videos` lists every grouped video (FR-010). When it targets a single video, behavior is unchanged from before this feature (FR-014). Anonymous access allowed. |
| GET | `api/share/{linkId}/videos/{videoId}/stream` | — | video stream | Streams one video that belongs to the link's competition (FR-010). Anonymous access allowed, same JWT-via-query-param exception as other streaming endpoints. |
| GET | `api/share/{linkId}/stream` | — | video stream | Unchanged: streams the link's single video when the link targets a video, not a competition. |

### `SharedVideoInfoResponse` (when `isCompetition = true`)

```json
{
  "videoId": "00000000-0000-0000-0000-000000000000",
  "name": "competition display name",
  "commentVisibility": 0,
  "allowCommentsOnThisLink": true,
  "allowAnonymousCommentsOnThisLink": false,
  "isCompetition": true,
  "videos": [
    { "videoId": "guid", "name": "string", "duration": "timespan?", "recordedDateTime": "datetime" }
  ]
}
```

When `isCompetition = false`, the shape and meaning are unchanged from the pre-existing
single-video response (`videos` is empty).

---

# Contract: Competition comment endpoints

Existing comment routes are reused unchanged at the surface level (FR-011, FR-012, FR-013); the
server resolves the link to a competition or a video internally.

| Method | Path | Notes |
|---|---|---|
| POST | `api/share/{linkId}/comments` | Posts into the combined thread when `linkId` resolves to a competition; into the video's own thread otherwise. Same anonymous/authenticated rules as today. |
| GET | `api/share/{linkId}/comments` | Lists the combined thread for a competition link, or a single video's thread otherwise, filtered by the same `CommentVisibility` rules (sourced from `Competition.CommentVisibility` or `Video.CommentVisibility` respectively). |
| GET | `api/comments/competition/{competitionId}` | Owner-facing listing of a competition's combined thread (mirrors `api/comments/video/{videoId}`). |
| PATCH/DELETE/`.../hide`/`.../unhide`/`.../report` `api/comments/{commentId}` | Unchanged; authorization for a competition comment resolves against `Competition.OwnerUserId` instead of a video owner. |
