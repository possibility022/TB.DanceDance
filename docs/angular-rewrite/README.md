# Dance Dance — Frontend Rewrite Specification (Angular)

## What this is

This folder is a **requirements specification** for re-implementing the **Dance Dance** web
front‑end in **Angular**. The application already exists today (as a React single‑page app); your
job is to **redesign and rebuild the same product** in Angular.

You are not porting code. You are building a new front‑end that delivers the **same capabilities**
to the same users, against a backend that already exists. Treat the documents here as the source of
truth for *what the app must do and the constraints it must respect* — **not** for how it should
look or how it should be structured internally.

## Read these in order

| File | Contents |
|------|----------|
| [00-context-and-constraints.md](./00-context-and-constraints.md) | The ground rules: what's fixed, what's yours to decide, glossary. **Read first.** |
| [01-authentication-and-authorization.md](./01-authentication-and-authorization.md) | OIDC auth contract. This is **stable** and reused verbatim — full details provided. |
| [02-app-shell-and-navigation.md](./02-app-shell-and-navigation.md) | Navigation, routing map, cookie consent, privacy policy. |
| [03-feature-videos.md](./03-feature-videos.md) | Browsing/playing videos: group lessons, private library, the player, rename. |
| [04-feature-events.md](./04-feature-events.md) | Events: listing, creating, per‑event videos. |
| [05-feature-comments.md](./05-feature-comments.md) | Commenting, anonymous comments, moderation, visibility. |
| [06-feature-sharing.md](./06-feature-sharing.md) | Sharing a video by link and viewing a shared link. |
| [07-feature-upload.md](./07-feature-upload.md) | Uploading recordings (private / event / group). |
| [08-feature-access-management.md](./08-feature-access-management.md) | Requesting access and the admin approval screen. |
| [09-configuration-build-deployment.md](./09-configuration-build-deployment.md) | Env config, Docker, hosting, GitHub workflows. |
| [10-data-formatting-and-enums.md](./10-data-formatting-and-enums.md) | Dates, identifiers, and shared enumerations. |

## The three rules that shape everything

1. **You design the layout and UX.** No document here prescribes page layout, component structure,
   visual hierarchy, or navigation arrangement. Where current behavior is described, treat it as the
   *functional baseline* — the capabilities that must exist — and feel free to present them better.

2. **Use Bulma for styling.** The app must be styled with [Bulma](https://bulma.io). Beyond that,
   the look is yours to design.

3. **The API schema is NOT in these docs and will be provided to you separately.** These specs
   deliberately contain **no** endpoint paths, request/response shapes, or field names, because the
   backend API is being reworked. Wherever a feature needs the backend, the spec says *what data or
   action is needed* in abstract terms. **Wait for the API schema** before wiring up data access, and
   map these capabilities onto whatever the new schema provides.
