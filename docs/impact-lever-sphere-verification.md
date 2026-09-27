# Impact lever — spherical fixture obstruction

Status: **spherical fixture blocking verified; complete lever remains unfinished**.

Base revision `40e9596` plus the exact hashes, recipes and UI capture source in [sphere proof records](impact-lever-sphere-recipes.json).

## Implementation

The hinge obstruction path now uses `SweepSurfaceKind` for box/sphere dispatch, retaining typed contact status and direction flags through the shared flight clock. The old box-only helper was replaced, not retained as a compatibility path. Spheres use the existing continuous `RotatingBoxSweep` against their exact radius and transformed centre, with zero fixture velocity. They are not replaced by bounding boxes. Unsupported dispatch is an explicit error.

As with fixed boxes, contact absorbs beam motion into a fixture while permitting rotation away. Sphere overlaps are rejected before Run. The small status message now says to move the beam clear of solid parts and the workbench. A non-rigid sphere-owner transform is rejected instead of silently using an unscaled radius for an ellipsoid. No art, palette, icon, panel or animation timing changed.

This integration treats a bumper's spherical shell as a stationary obstruction. Its existing powered kick targets dynamic balls; it does not acquire a powered hinge strike here. Hollow surfaces, moving-fixture response and beam/beam collisions remain unimplemented in this obstruction path.

## Native evidence

All **1,290 native cases pass**, including fourteen new `ImpactLeverSphereObstructionTests` cases:

- Aligned positive and negative motion at 3 and 20,000 rad/s stops at the analytic sphere/beam contact angle; no tunnelling to the authored limit.
- A Z-depth miss remains clear; initial overlap is rejected without construction mutation.
- Inward motion releases contact; after moving away and removing the fixture, outward motion reaches the authored limit. Exact construction Reset restores the removed fixture too.
- Three authored 3D orientations preserve the same analytic contact angle.
- A beam in an empty corner of the sphere's bounding box remains unobstructed by the actual curved shell.
- Nonuniform scale, uniform shrink and reflection are explicitly rejected.
- Ten seconds of falling-ball pressure cannot drive the beam through the bumper, with bounded contact iterations and zero final angular velocity. The bumper does not falsely count this beam contact as a dynamic-ball hit.

For the local arrangement (sphere centre (1.5,1,0), radius 0.65, beam half-height 0.12), first contact solves `cos(a) - 1.5 sin(a) = 0.77`, giving approximately 0.1466983 rad. Tests compare against this independent equation, allowing the existing contact skin.

The initial five tests produced four failures against the box-only engine. Later, three extra orientation assertions failed because their manually estimated angle range was incorrect; the assertions were replaced by the same analytic equation/tolerance used for the aligned case. Physics/contact tolerances were not changed. Retained summaries: `docs/playtest-results/impact-lever-sphere-native-failures.txt`.

## Actual-UI Playwright evidence

Fresh browser contexts used palette clicks, world placement, movement/resize handles and Run/Reset. No game setters, imported solutions, numeric placement menu or storage edits were used. Each running case has exactly three expected parts, zero connections, positions within 0.025 units of its recipe, zero browser errors and exact Run/Reset equality.

| Case | Result |
| --- | --- |
| `impact-lever-sphere-blocked-v1` | 101 frames. Actual bumper at (1.5, 3.9959626, 0); lever at (0,3,0); bowling ball at (−1.2,5.988797,0). Beam stops at 0.14423967069249433 rad, about 8.3°, with zero angular velocity/energy. |
| `impact-lever-sphere-missed-v1` | 101 frames. Bumper at (1.5,3.9836607,2); beam reaches its normal upper limit, π/6 rad. |
| `impact-lever-sphere-overlap-v1` | Two-part overlapping construction remains in build mode and emits no Run event. Inspected screenshot shows the updated instruction to move the beam clear of solid parts. |
| `impact-lever-sphere-wall-regression-v1` | 101 frames. Actual wall construction matches the previous proof; all 100 shared body/hinge frames match exactly. |

Continuous blocked-case recording: 36.12 seconds, VP8, 1440×900 at 25 fps, SHA-256 `7f6266267fc9c25d7ed1fca3a61fd7b84db32b5895573d6cf513130bc333b63a`. Consecutive frames at seconds 27.4–28.4 were inspected: the falling ball rotates the beam continuously into the curved shell, which stops it while the ball remains free to move. Diagnostics span eight simulated seconds in 7.964 wall-clock seconds. This is focused desktop motion evidence, not mobile or 60-fps certification.

Local logs are under `docs/playtest-results/<caseId>.json`; captures are under `.playwright-mcp/`. The tracked recipe file identifies their configurations, actions and source hashes. Historical evidence is unchanged.

## Build and remaining work

Native command: `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`. Both diagnostic publish and production Release publish pass, as does `git diff --check`.

The lever stays unchecked. Remaining requirements include tube/bend/frustum obstruction, moving obstacle response, beam/beam contacts, full fulcrum coverage, rope end sockets/angular coupling, guided-body overlap, broader load/friction interactions, campaign lessons and mobile/performance proof. New shape selection is enum-typed; this does not claim that the repository-wide magic-string audit is finished.
