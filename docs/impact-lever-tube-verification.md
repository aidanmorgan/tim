# Impact lever — straight hollow tube obstruction

Status: **straight-tube shell/bore interaction verified; complete lever remains unfinished**.

Tested on base `100456c` plus [recorded source hashes, actual configurations and reproducible UI recipes](impact-lever-tube-recipes.json).

## Geometry and motion

`TubeBoxIntersection` clips the box against the tube's axial slab and projects the clipped vertices onto the cross-section plane. The convex projection's minimum and maximum radii determine intersection with the annular shell. This builds on the clipping/projection approach in [Geometric Tools' box/finite-cylinder reference](https://www.geometrictools.com/Documentation/IntersectionBoxCylinder.pdf), with our annular extension preserving the bore. It uses analytic cylinders/rims, not faceted collision meshes or filled bounding boxes.

The signed expansion margin is an implementation-specific clearance bound, not a contact normal or minimum translation distance. Increasing axial half-length and outer radius while decreasing inner radius contains the shell's Euclidean dilation; a known-disjoint positive expansion therefore bounds safe point travel from below. Bisection resolves this margin to 0.000003125. Negative values describe shell erosion. Degenerate empty erosions do not represent a substitute collision shape.

`RotatingTubeSweep` bounds vertex travel during fixed-axis, constant-speed rotation by angular speed × radial-reach upper bound. It advances conservatively between poses rather than trusting clear endpoints. Exact coaxial rotation has an invariant radial/axial occupancy check. Within the existing 0.0001 contact skin, up to nine adaptive direction probes distinguish approach from separation when tiny probes cannot resolve second-order curvature. The probes inspect geometry without advancing simulation or moving the beam. Contact trends and query outcomes are enum-typed. Unsupported geometry, lost numerical progress, deeply unresolved contact and iteration exhaustion are explicit errors—not clear-result fallbacks.

The shared hinge path dispatches declared straight `TubeProxy` shapes through the new query, including collars. The existing one-way angular contact blocks absorb motion into a shell and permit release. Initial shell penetration is rejected before Run; an initially clear bore is not rejected. Box and sphere handling remains in the same typed path. There are no new visuals or UI controls.

## Native verification and retained failures

All **1,321 native cases pass**, including 31 added cases:

- Fourteen static cases: open bore/end, shell/rim, beyond-end miss, a surrounding box, tangent contact, a near-end coupled miss that requires clipping, analytic outer/end/bore clearance, invalid/non-rigid inputs and two seeded cases with 160 arrangements independently checked for interior shell witnesses.
- Twelve rotating cases: analytic outer-shell contact at ordinary and 20,000 rad/s speed; an intermediate hit despite clear endpoint poses; inner-bore travel/contact; depth miss; invariant coaxial rotation; separation followed by a later collision; initial overlap; arbitrary whole-setup rotation; invalid motion; second-order tangent approach/release; and two seeded cases containing 80 arrangements compared with dense static pose checks.
- Five integrated cases: shell stop, depth miss, initial bore freedom followed by inner-shell stop, and ten-second falling loads against both shell sides with bounded world contact iterations and exact construction Reset.

The dense motion oracle uses the separately tested static intersection routine; it is not an independent geometry implementation or a universal numerical proof. The static sample witnesses and analytic contact equations provide independent checks.

The initial world test failed because the beam passed through the pipe to its 30° limit. A transformed exact-tangent boolean also exposed float transform roundoff; its original untransformed assertion remains exact, while the transformed case verifies margin within 0.00001 rather than inflating the geometry.

A harder test found a genuine release bug: a tiny direction probe missed second-order approach, then a margin-only shortcut blocked the reverse motion too. The adaptive enum-classified direction check removed that shortcut. The original no-deep-overlap and release assertions pass unchanged. Retained excerpts: `docs/playtest-results/impact-lever-tube-native-failures.txt`.

## Real-UI Playwright proof

Fresh contexts used actual palette clicks, world placement, 3D movement/resize handles, Run and Reset. No setters, numeric placement menus, imported constructions or storage edits. The three lever runs have exactly three expected parts, no links, position error below 0.025, pipe length error below 0.03, zero browser errors and exact serialized Run/Reset equality.

| Case | Observation |
| --- | --- |
| `impact-lever-tube-shell-v1` | 102 frames. Pipe at (1.5,4.083307,0), length 1.0156088. Beam stops against the outside at 0.14753227303879665 rad, with zero angular velocity/energy. |
| `impact-lever-tube-missed-v1` | 101 frames. Pipe at (1.5,4.0882916,2), length 1.0260905. Beam reaches its ordinary upper limit, π/6 rad. |
| `impact-lever-tube-bore-v1` | 102 frames. Pipe at (1.2,3,0), length 1.0059731. Run starts with the beam inside the open bore; falling bowling ball turns it until the inner shell stops it at 0.12658228014828235 rad. |
| `impact-lever-tube-overlap-v1` | A two-part construction intersecting the shell is rejected before Run. Inspected screenshot shows the instruction to move the beam clear; no Run event is emitted. |
| `impact-lever-tube-flight-regression-v1` | Existing trampoline-to-pipe construction matches the reference configuration. All 100 shared body-state frames match exactly, with zero errors and exact Reset. |

The bore recording is `.playwright-mcp/impact-lever-tube-bore-v1.webm`: 36.68 seconds, VP8, 1440×900, 25 fps. SHA-256 `d929b5edfebb078b1449b2cc64bb36360611929e53dea0b883bcbf983f59f69c`. Consecutive frames at 27.9–28.9 seconds were inspected: the ball falls, the beam continuously rotates within the visible open pipe and holds at the inner wall while the ball can continue moving. Diagnostics span 8.1 simulated seconds in 8.076 wall-clock seconds. This is focused desktop motion evidence, not mobile/60-fps or large-scene certification.

Local logs remain under `docs/playtest-results/<caseId>.json`, captures under `.playwright-mcp/`; source/configuration provenance is tracked in the linked recipe artifact.

## Build and remaining scope

Verification command: `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`. Both diagnostic and production Release browser publishes pass, along with `git diff --check`.

The lever remains unchecked. Bends and frustums, prescribed moving fixtures, beam/beam collisions, full fulcrum geometry, rope end sockets/angular coupling, guided-body overlap, broader load/friction interactions, campaign teaching and mobile/performance proof remain open. Straight collars on a bend/funnel do not establish collision coverage for its curved or tapered main shell. The repository-wide magic-string audit also remains unfinished; this increment keeps new collision decisions enum-typed.
