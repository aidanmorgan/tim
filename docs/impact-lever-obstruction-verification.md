# Impact lever — fixed-box obstruction increment

Status: **fixed-box/workbench obstruction verified; complete lever still unfinished**.

Tested on base `e9814cd` plus the source hashes in [reproducible recipes and audits](impact-lever-obstruction-recipes.json). This increment preserves the existing art, palette, icon and simulated beam animation.

## Geometry and response

`RotatingBoxObstacleSweep` queries a fixed-axis rotating OBB against a stationary OBB. Static overlap uses the six face axes and nine edge cross products described in [Geometric Tools' Dynamic Collision Detection using Oriented Bounding Boxes](https://www.geometrictools.com/Documentation/DynamicCollisionDetection.pdf). The paper is a geometry reference, not proof of this implementation. Our rotational time advancement separately bounds every vertex's projection velocity and acceleration on a separating plane; clear endpoint poses alone do not establish clearance.

The query distinguishes clearance, contact and initial overlap with `SphereSweepStatus`. Directional derivatives include rotating separating axes, allowing a beam touching one end of a finite wall to rotate away. An invariant axial projection prevents a depth-tangent face from being mistaken for a radial impact. The existing 0.0001 contact tolerance bounds the near-contact classification. Invalid inputs, nonrepresentable progress and iteration exhaustion are errors, never collision-free fallbacks.

`WorldHinges` queries the workbench deck/base and other visible, non-dynamic parts' declared box proxies, excluding the beam's own owner's fixtures. The shared flight clock stops at the earliest beam/box contact. `AngularBlock` enum flags constrain only motion into that contact; removed angular energy is not reflected into the beam. Sphere impulses use the constrained hinge response. Inward movement releases a contact, and constraints are recomputed each flight substep. Reset clears transient constraints. Authored angle limits remain distinct from an obstacle stop.

A beam initially penetrating a fixed box is rejected before Run by a typed exception. The existing small status line asks the player to move it clear; no inspector, fallback motion or automatic relocation was added.

## Native proof

All **1,276 native cases pass** after the final edits. Twenty added cases comprise:

- Thirteen geometric cases: analytic positive/negative hits at ordinary and 20,000 rad/s speeds; a thin intermediate obstacle with clear endpoint poses; separating touch and later return; axial tangent/depth miss; stationary touch versus initial overlap; arbitrary world orientation; rejected invalid input; and three seeded cases containing 180 arrangements checked against independently projected box corners at up to 1,025 sampled poses. Finite randomized coverage is not a universal mathematical proof.
- Five integrated cases: fixed wall, workbench and missed-depth control; initial-overlap rejection without construction mutation; and ten seconds of falling-weight pressure with bounded contact iterations, no wall crossing and exact Reset. Wall/deck cases also verify absorbed outward impulse and inward release.
- Two joint cases: negative/positive contact stops, exact energy removal, outward blocking, inward release, combined blocks, undefined-enum rejection and Reset.

The deck native fixture intentionally places the beam low to exercise deck collision; its stand intersects the tabletop. It is an isolated solver test, not a polished level or a claim that the whole part placement is valid.

The original wall/deck tests failed at the authored 30° limit rather than stopping at the obstacle. An early query also falsely reported contact for axial tangency. Retained excerpts: `docs/playtest-results/impact-lever-obstruction-native-failures.txt`. Neither failure was repaired by relaxing the assertions, increasing iteration limits or dropping simulation time.

Commands:

```sh
dotnet test CuriousContraptions.tests --no-restore --verbosity quiet
dotnet publish CuriousContraptions.web -p:PlaytestDiagnostics=true --no-restore --verbosity quiet
dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet
git diff --check
```

Both diagnostic and production publishes pass. New closed-set collision state uses enums; existing repository-wide string-selector audit remains unfinished.

## Actual-UI Playwright proof

All gameplay changes were made through palette clicks, 3D movement/resize handles, Run and Reset. No setters, imported construction, numeric placement menu or game-state mutation was used. Each successful run has the expected three parts and zero connections, positions within 0.025 units and wall dimensions within 0.03 of its recipe. All have zero browser errors and exact serialized Run/Reset equality.

| Case | Observed result |
| --- | --- |
| `impact-lever-blocked-wall-v3` | 100 diagnostic frames. Actual wall at (1.5, 3.5960987, 0), dimensions (0.6139894, 0.4, 1.4113392). Beam stops at 0.1547911108253363 rad (about 8.9°), zero angular velocity/energy, before its authored upper limit. |
| `impact-lever-wall-depth-miss-v1` | 101 frames. Wall moved to Z=2; beam reaches the authored upper limit, π/6 rad. |
| `impact-lever-overlap-rejected-v1` | A two-part overlapping beam/wall construction stays in build mode, emits no Run event and displays the inspected instruction to move the beam clear. Native coverage additionally verifies no construction mutation. |
| `impact-lever-obstruction-transfer-regression-v1` | 104 frames; all 101 shared body/hinge frames exactly match the previous aligned ball-transfer run. Actual configuration is identical. |
| `impact-lever-blocked-motion-v1` | Fresh recorded repeat matches all 100 shared body/hinge frames of blocked-wall v3, including its actual construction and exact Reset. |

The first blocked-wall attempt timed out loading while a publish was still active. The next attempt reported WebAssembly integrity mismatches from the reused browser context. Both failed attempts remain in local logs; neither is counted as gameplay evidence. A fresh isolated context loaded the completed build and was used for subsequent proofs; integrity checks were not bypassed.

Recording `.playwright-mcp/impact-lever-blocked-motion-v1.webm`: 41.32 seconds, VP8, 1440×900, 25 fps; SHA-256 `9a5719e3f9021499610e0d3c16d1b0c5f31cce04af3b51cfe18b50170a04ce50`. Consecutive frames at seconds 32.65–33.65 were inspected: the ball falls onto the beam, the beam continuously tilts into the wall and holds while the ball continues moving. This is focused desktop motion evidence, not mobile/60-fps certification. Diagnostic timestamps span 7.9 simulated seconds in 7.863 wall-clock seconds.

Full local logs: `docs/playtest-results/<caseId>.json`. Screenshots/video: `.playwright-mcp/`. Recipes, capture/overlap adapters, actual placements and source hashes are tracked in the linked JSON.

## Remaining scope

This does not complete the lever. Curved fixture shapes (spheres/tubes/bends/frustums), moving obstacle response, beam/beam collision, full fulcrum coverage, rope end sockets/angular rope coupling, guided-body overlap resolution, broader load/friction interactions, campaign teaching, mobile and broader performance proof remain open. Capturing a non-dynamic part's current box proxy is not proof of swept collision against a prescribed moving fixture. The complete component stays unchecked in TODO.
