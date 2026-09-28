# Impact lever — hollow funnel obstruction

Status: **fixed frustum shell/bore interaction verified; complete lever remains unfinished**.

Tested on base `ffdac82` plus the source hashes, actual configurations and reproducible UI recipes in [the evidence manifest](impact-lever-frustum-recipes.json).

## Implementation

The [static hollow-frustum foundation](impact-lever-frustum-foundation.md) now participates in continuous beam flight. Declared `FrustumProxy` shapes use the existing `SweepSurfaceKind.Frustum` enum, with typed shape payloads and overloads throughout dispatch.

`RotatingShellSweep` replaces the former tube-only query; current callers and tests move forward together, with no old API alias. Typed tube/frustum implementations share the same travel bound, bounded direction probes and explicit failure conditions. The frustum clearance uses the slope-aware expansion bound, not a radial-only distance. No tessellated cone or filled-bore substitute is used. Static overlap is rejected before Run; clear open interiors remain usable. Existing one-way angular blocks absorb motion into fixtures and allow reverse release.

No part art, colours, icons, menus or controls changed.

## Native verification

All **1,359 native cases pass**. This increment adds 19:

- Thirteen continuous-query cases: independent outer-corner contact equations at 1 and 20,000 rad/s; intermediate contact despite clear endpoints; independent inner-corner contact; depth-missed and exact coaxial controls; reverse release and common 3D transform; initial overlap versus clear bore; curved tangent approach/release; both annular end faces with independent cap-arrival equations; invalid motion; and 80 seeded arrangements checked against 1,025-pose static sampling.
- Six integrated cases: shell/bore blocking with collars removed, depth miss, overlap rejection without starting or mutating the construction, and ten-second falling-load runs against both shell sides with bounded contact iterations and exact Reset.

The static pose sampling reuses the separately tested geometry query; it is not an independent geometry oracle or proof over every possible trajectory. Analytic contact equations and prior static surface-witness tests supply independent checks.

Before integration, all four positive world cases failed while the missed-depth control passed. Retained summary: `docs/playtest-results/impact-lever-frustum-native-failures.txt`. The same assertions pass after integration. No browser failure was observed in this increment.

## Actual-UI Playwright proof

Fresh browser contexts used the visible palette, placement clicks, translation/rotation handles, Run and Reset. No imported construction, state setter, storage edit, numeric placement menu or game-method call was used. Read-only diagnostics recorded actual construction and motion. Each running case verified three expected parts, zero links, position error below 0.025, expected orientation, exact serialized Run/Reset equality and no console errors.

All runs used Free workshop row 62 at Balanced precision 0.45:

| Case | Observed outcome |
| --- | --- |
| Outer shell | Reversed funnel at (1.5, 4.5837774, 0); beam stopped at **0.19790193319400554 rad**, angular speed/energy zero, before its authored limit. 102 frames. |
| Inner bore | Funnel at (1.5, 3, 0); beam began clear inside the bore, moved, then stopped at **0.35652214814725963 rad**, angular speed/energy zero. 101 frames. |
| Depth miss | Reversed funnel at (1.5, 4.5887623, 2); beam reached the authored upper limit **0.5235987755982988 rad**. 101 frames. |
| Initial overlap | Two-part construction stayed in build mode, emitted no Run event and displayed the instruction to move the beam clear of solids. Screenshot inspected. |
| Straight-tube regression | Actual construction matched the prior inner-bore case; all **102 shared body and hinge frames matched exactly**, including the 0.12658228014828235-rad stop. Exact Reset and no errors. |

The falling bowling ball was at (-1.2, 5.988797, 0), with the lever at (0, 3, 0). Actual configurations and quaternion checks are retained in the manifest; intended recipes alone are not the proof.

The bore video is `.playwright-mcp/impact-lever-frustum-bore-v1.webm`: VP8, 1440×900, 25 fps, 34.88 seconds. SHA-256: `f4fc1d284985e194afdd223dcdb9b2706ae6d9be75486707dd85cc8e5d862fff`. Reviewed consecutive frames from 26.1–27.1 seconds show the falling ball, progressive beam rotation into the visible inner shell, a stable stop and continuing ball motion. Eight simulated seconds spanned 7.972 wall seconds in sampled diagnostics. This is focused desktop motion evidence, not a 60-fps/mobile performance guarantee.

Full local observations and captures are under `docs/playtest-results/impact-lever-frustum-*.json` and `.playwright-mcp/impact-lever-frustum-*`. The tracked manifest preserves recipes, exact adapter references, capture source, configuration audits and source hashes.

## Build, review and remaining scope

Full native tests, production Release publish with diagnostics disabled and `git diff --check` pass. New closed-set choices use enums (`End`, `Route`, `Role`, `SweepSurfaceKind`, `ContactTrend`, `SphereSweepStatus`); shape dispatch retains typed proxies. Test/catalog string values occur at explicit serialization boundaries. This does not claim that the existing repository-wide magic-string audit is finished.

The lever stays unchecked in TODO: hollow bends, prescribed moving fixtures, beam-to-beam collisions, full fulcrum collision, end-rope coupling, guided-body overlap, broader friction/load interactions, campaign teaching and mobile proof remain unfinished. These tests establish fixed-funnel interaction, not complete lever correctness, whole-funnel collision with every part type, or completed campaign/difficulty testing.
