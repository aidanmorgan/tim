# CAT-053 · pulley declaration readiness spec

This is the Story 7.0 CAT-053-D declaration readiness spec for the fixed pulley. Rope route, length and tension facts shared by every rope element live in [CAT-058 rope_anchor](CAT-058-rope_anchor.md) (rows R3–R27 there); this file holds the pulley-specific facts and refers to those rows instead of repeating them. Baseline commit is `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge; current-engine files are cited at the same commit.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-053 |
| Kind | `pulley`, catalogue title "Pulley" |
| Requirement anchor | [CAT-053](../requirements.md#current-cat-053); retained behaviour [todo-140](../requirements.md#todo-140) and [todo-141](../requirements.md#todo-141) |
| Mapped identities | [EL-206 Fixed pulley](../invest/named-elements.md#element-206) (candidate — Batch A confirms); [EL-207 Moving pulley](../invest/named-elements.md#element-207) (candidate variant — Batch A confirms; the requirement keeps finite-inertia/frictional pulleys as separate future models, see Open question 4) |
| Roadmap story | 10.2 Pulley Wheel & Tensile Rope Dynamics (CAT-053, CAT-058) in [epics.md](../../../_bmad-output/planning-artifacts/epics.md) |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- One static body. Legacy colliders: a navy mount box centred at (0, 0.55, −0.1) m, full size 0.5 × 0.15 × 0.4 m (half-extents 0.25, 0.075, 0.2), plus a collision sphere of radius 0.4 m at the origin standing in for the wheel — `parts/PulleyPart.cs@a6c914e:L21-L24`; `AddBox` halves the size (`reference/cpu/MachinePart.cs@a6c914e:L352-L355`); parts are static unless they set `Dynamic` (`reference/cpu/MachinePart.cs@a6c914e:L161`).
- Wheel (groove) radius 0.4 m and rope-plane depth 0.12 m — `engine/MachineData.cs@a6c914e:L95-L98`; `parts/PulleyPart.cs@a6c914e:L9`.
- Pick radius 0.65 m — `parts/PulleyPart.cs@a6c914e:L21`.
- Current types: static `RigidBodyDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`), `ColliderDeclaration` Box and Sphere (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`). The current shape set has no cylinder or disc for a true wheel (Open question 1).

### Mass and material

- Mass: none — the requirement's pulley is fixed and frictionless; the legacy part had no wheel inertia (`parts/PulleyPart.cs@a6c914e:L6`).
- Material: legacy static default restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236`). Declare through `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`); values are an owner decision (Open question 6). Rope-to-groove friction is zero (frictionless pulley).

### Constraints and joints

- The pulley owns no joint of its own. It is a guide inside a rope route: the route enters and leaves the groove, and the physical route length includes the finite-radius tangent legs and the wrapped arc (requirement; supersedes the legacy point guide, CAT-058 R24).
- Story 10.2 must add the rope-route declaration (CAT-058) with a guide entry that carries the groove centre, radius, groove plane and winding side.

### Typed sockets and ports

- One socket `Tie`, domain `Rope`, bidirectional, at local (0, 0.4, 0.12) m (top of the groove, in the rope plane) — `parts/PulleyPart.cs@a6c914e:L14-L18`. Attachment role `Guide`: accepts two rope spans, never an end (`engine/RopeNetwork.cs@a6c914e:L76-L77`, `engine/RopeNetwork.cs@a6c914e:L34-L35`).
- Current `WorkshopConnections` has no rope domain or `Tie` socket (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`); Story 10.2 adds them (shared with CAT-058).

### Sensors and activation

None — no sensor or activation port in legacy or requirements.

### Work and energy stores

None — fixed, frictionless and massless: it redirects tension without storing or dissipating work.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| none | — | — | — | — | `parts/catalog/pulley.tres@a6c914e:L14` (`Parameters = {}`) |

Groove radius 0.4 m and rope-plane depth 0.12 m are fixed declaration constants, not player parameters (`engine/MachineData.cs@a6c914e:L97-L98`).

### Cosmetic curves and UI bindings

- Wheel angle follows rope travel: Δangle = Δ(route distance at this guide) / 0.4 m, wrapped to [0, 2π) — `parts/PulleyPart.cs@a6c914e:L34-L38`. The wheel turns only while the route is complete and taut (`engine/RopeNetwork.cs@a6c914e:L50-L58`).
- Rope artwork around the wheel: tangent legs meet curved arcs outside the cream rim; threading favours the local upper side and keeps that winding during motion — `DESIGN.md@a6c914e:L284` and harvest P14.
- The current `AnimationFeedbackSource` set (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L6-L7`) has no rope-travel source; Story 10.2 adds a committed rope-travel binding for the wheel.

### Art

- Scene `parts/scenes/pulley.tscn` is a bare `Node3D` with the part script (`parts/scenes/pulley.tscn@a6c914e:L1-L6`).
- Meshes — `parts/PulleyPart.cs@a6c914e:L22-L32`: navy (`#293954`) mount box as the collider; cream (`#fff8e9`) post 0.12 × 0.55 × 0.12 at (0, 0.3, −0.13); rotating wheel node with a catalogue-colour cylinder r 0.4, height 0.17, rotated 90° about X; cream rim ring r 0.4, thickness 0.035 at z 0.1; navy spoke bar 0.57 × 0.065 × 0.04 at z 0.12; gold (`#f7cb52`) marker sphere r 0.06 at (0.27, 0, 0.15).
- Palette: "Pulley / rope anchor" `#d69c47` — `DESIGN.md@a6c914e:L187`; `parts/catalog/pulley.tres@a6c914e:L13`.
- Toolbox pictogram: wheel circle r 6 with hub r 2, two hanging legs, top bracket — `ui/WorkshopIcons.cs@a6c914e:L96`.

### Catalogue and inventory entry

- Id `pulley`, title "Pulley", category "Ropes", description "Redirect a continuous rope. Connect both sides to loads or anchors; unfinished ropes cannot carry tension." — `parts/catalog/pulley.tres@a6c914e:L8-L14`.
- Add `WorkshopPartKind.Pulley` (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3`) and append a `WorkshopInventoryPolicy.Free` row (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).
- Levels: `counterweight` places two locked pulleys; `pulley_depth` places one locked pulley and stocks one more (harvest P12).

## 3. Engine capabilities

Capability families: see the CAT-053 row of the [general-engine element map](../general-engine-element-map.md) and the `pulley` entry in [catalogue-elements.json](../../coverage/catalogue-elements.json). Not duplicated here.

**Exists now**

- Static box/sphere bodies and colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`.
- TGS Soft contact solver for load–bench and load–load contact: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L225-L226`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878-L884`.
- Activation chain used by the lessons (Switch → Lamp): `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L62`, `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`.

**Missing**

- Rope domain, `Tie` socket, guide attachment role — Story 10.2.
- Tension-only rope route constraint with finite-radius groove tangents and wrapped arcs, unbranched endpoint routes, and admission rules — Story 10.2.
- Committed rope-travel read for wheel spin and rope artwork — Story 10.2.
- Counterweight lifting and pendulum qualification through pulleys — Story 10.4.
- Moving (load-attached) pulley and finite-inertia/frictional sheaves — not scheduled; separate future models per the requirement (Open question 4).

**Element dependencies**

- Requires two dynamic endpoints or an anchor plus a load: [CAT-067 weight](CAT-067-weight.md), [CAT-058 rope_anchor](CAT-058-rope_anchor.md).
- The lessons need the existing Switch and Lamp (delivered CAT-063 and CAT-035).
- Impact-lever rope sockets ([CAT-034](CAT-034-impact_lever.md)) route through pulleys in Story 10.2.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| P1 | Legacy pulley: fixed, frictionless, no moving-block ratio, no wheel inertia. | `parts/PulleyPart.cs@a6c914e:L6` | carry forward (fixed, frictionless, no inertia) | Matches the requirement; moving-block and inertia variants stay separate. |
| P2 | Groove radius 0.4 m; tie socket at (0, 0.4, 0.12), so rope spans lie in the plane z = 0.12 of the pulley frame. | `engine/MachineData.cs@a6c914e:L95-L98`; `parts/PulleyPart.cs@a6c914e:L9`, `parts/PulleyPart.cs@a6c914e:L14-L18` | carry forward | Authored geometry (re-declare as f32); level loads sit at z 0.12 to share that plane. |
| P3 | A guide accepts two spans; a route ending at a guide is incomplete and transmits nothing ("unfinished ropes cannot carry tension"). | `engine/RopeNetwork.cs@a6c914e:L34-L35`, `engine/RopeNetwork.cs@a6c914e:L76-L77`; `parts/catalog/pulley.tres@a6c914e:L11` | carry forward | Requirement: slack/open route transmits no tension. |
| P4 | Colliders: navy mount box half-extents (0.25, 0.075, 0.2) at (0, 0.55, −0.1) and a 0.4 m sphere at the wheel centre; pick radius 0.65. | `parts/PulleyPart.cs@a6c914e:L21-L24` | carry forward the box; sphere subject to Open question 1 | A sphere is not a wheel; the requirement wants true groove geometry. |
| P5 | Wheel artwork: post, cylinder, rim ring, spoke bar and gold marker (dimensions in Art). | `parts/PulleyPart.cs@a6c914e:L22-L32` | carry forward | Authored art; palette preserved. |
| P6 | Wheel angle advanced by distance / radius, wrapped to 2π, stored as transactional runtime state of the part. | `parts/PulleyPart.cs@a6c914e:L11-L13`, `parts/PulleyPart.cs@a6c914e:L34-L38` | carry forward the relation; do not carry forward the per-part state and `AdvanceRope` call | Per-element update path; the animation worker derives spin from committed rope travel. |
| P7 | Guides animate only when the route is complete and taut; each guide uses its own distance delta along the route. | `engine/RopeNetwork.cs@a6c914e:L50-L58` | carry forward the rule | Wheel travel follows committed physical travel. |
| P8 | Acceptance: with unequal masses the wheel turns; with equal masses it does not; an open route leaves it at 0; Reset returns every wheel to angle 0. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L58-L62`, `CuriousContraptions.tests/RopeTests.cs@a6c914e:L171`; `CuriousContraptions.tests/RopeOwnershipTests.cs@a6c914e:L54-L59` | carry forward | Wheel witness follows physical travel. |
| P9 | Acceptance: two fixed pulleys between masses m1, m2 accelerate at g (m2 − m1)/(m1 + m2); after 0.25 s each load's vertical speed is within 0.015 m/s of that (1e-5 in the direct binding test), the sum of the two heights is conserved within 0.001 m, the route stays taut at its length ±0.001 m, and reversing link direction and order gives the same motion. Cases (1, 4), (4, 1), (1, 1). | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L21-L68`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L87-L144`; `CuriousContraptions.tests/RopeOwnershipTests.cs@a6c914e:L23-L63` | carry forward (re-freeze tolerances under f32) | Requirement: equal/unequal masses and declaration order. |
| P10 | Acceptance: over 900 ticks neither load penetrates the bench and the route never exceeds its length + 0.002 m. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L217-L241` | carry forward | Requirement todo-141 floor contact. |
| P11 | Acceptance: a fixed guide transfers tension but never pushes; a dynamic guide shares the reaction with both loads in one solve. | `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L18-L45` | carry forward the behaviour; do not carry forward the point-guide numbers | Point-guide geometry is superseded by finite-radius grooves. |
| P12 | Levels: `counterweight` (19, "A helping weight") places locked pulleys at (−2, 6, 0) and (2, 6, 0); `pulley_depth` (20, "Around the corner") places a locked pulley at (−2, 6, 0), stocks one pulley, and its solution adds `pulley_1` at (2, 6, 2). Solution spans: 5, 4, 0.81203175 m and 5, 4.472136, 0.81203175 m. | `content/puzzles.json@a6c914e:L5146-L5271`, `content/puzzles.json@a6c914e:L5473-L5505`, `content/puzzles.json@a6c914e:L5508-L5517`, `content/puzzles.json@a6c914e:L5583-L5645`, `content/puzzles.json@a6c914e:L5846-L5908`, `content/puzzles.json@a6c914e:L5910-L5942` | carry forward | Lesson set-ups to rebuild in Epic 15; lengths follow the connect-time rule (CAT-058 R6). |
| P13 | Lesson authoring: the depth lesson is the counterweight lesson with the right pulley unlocked, renamed `pulley_1`, moved to z 2, and the solution weight moved to z 2.12; hint texts. | `tools/Campaign/Program.cs@a6c914e:L189-L223`, `tools/Campaign/Program.cs@a6c914e:L408-L413` | carry forward | Campaign rebuild input. |
| P14 | Artwork route geometry: the rim arc lies on the 0.4 m radius in the wheel plane at depth 0.12; free legs stay outside the wheel; tangency is measured in the wheel plane even when a leg changes depth; the wrapped sweep is below π; a close starting load threads over the top (clockwise); symmetric routes use the top; a 1 mm move does not flip the winding; reversing endpoints mirrors the route. The `PulleyRopeRoute` source is already absent at `a6c914e`; only these test assertions remain. | `CuriousContraptions.tests/PulleyRopeRouteTests.cs@a6c914e:L24-L56`, `CuriousContraptions.tests/PulleyRopeRouteTests.cs@a6c914e:L112-L140` | carry forward | Requirement: rim tangent/arc artwork agrees with physical length; the same tangent/arc geometry defines the physical route. |
| P15 | Rope artwork reads committed poses only: scene edits and an unpublished solver step do not move it; it rebuilds identically after Reset; two endpoint knot spheres are visible on a complete pulley route. | `CuriousContraptions.tests/PulleyRopeRouteTests.cs@a6c914e:L58-L102`, `CuriousContraptions.tests/PulleyRopeRouteTests.cs@a6c914e:L142-L169` | carry forward the behaviour | Presentation reads committed state; the legacy `RopeVisual` class is not carried. |
| P16 | Pulley wheel accumulator rolled back after a failed tick. | `CuriousContraptions.tests/RemainingReadingCheckpointTests.cs@a6c914e:L57-L93` | do not carry forward | CPU transactional rollback; the current worker never faults a tick. |
| P17 | Point-guide route geometry and analytic stop sweeps. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L20-L22`; `CuriousContraptions.tests/RoutedRopeTests.cs@a6c914e:L104-L159` | do not carry forward | Superseded approximation and CPU solver path (CAT-058 R24, R26). |
| P18 | Historical context: "counterweight/3D pulley-routing lessons at 19–20"; fixed pulley routing supported, moving-block ratios future work. | `reference/P0-022-before/source-README.md@a6c914e:L29`, `reference/P0-022-before/source-README.md@a6c914e:L99` | carry forward as context | Confirms lesson order and the moving-pulley gap. |

### Files harvested

- `parts/PulleyPart.cs`
- `parts/catalog/pulley.tres`
- `parts/scenes/pulley.tscn`
- `engine/MachineData.cs`
- `engine/RopeNetwork.cs`
- `engine/physics/PhysicsRopeJoint.cs`
- `reference/cpu/MachinePart.cs`
- `reference/P0-022-before/source-README.md`
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `reference/p025-production-isolated-20261004/src/ui/WorkshopIcons.cs` (checked, duplicate of the surviving pictogram)
- `tools/Campaign/Program.cs`
- `content/puzzles.json`
- `CuriousContraptions.tests/RopeTests.cs`
- `CuriousContraptions.tests/RoutedRopeTests.cs`
- `CuriousContraptions.tests/PulleyRopeRouteTests.cs`
- `CuriousContraptions.tests/RopeOwnershipTests.cs`
- `CuriousContraptions.tests/SceneRopeBindingTests.cs`
- `CuriousContraptions.tests/RemainingReadingCheckpointTests.cs`
- `engine/SceneRopeJoint.cs`, `engine/ConnectionPort.cs` (harvested into CAT-058; no pulley-only knowledge)
- `reference/p054-guide/puzzles-candidate.json` (checked; same pulley levels as `content/puzzles.json` apart from the orientation encoding)
- `engine/bridge/*` (checked, no element knowledge)

## 5. Acceptance outline

Requirement row: [CAT-053](../requirements.md#current-cat-053); story 10.2.

- **Chrome UI recipe.** Place two Pulleys high, a light Weight under the left and a heavy Weight under the right with toolbox and gizmo; connect weight → left pulley → right pulley → weight as rope links with the contextual wiring UI. Verify the committed configuration, the four-socket route and its measured length.
- **Positive.** Run: the heavy weight descends and lifts the light one; both wheels turn with committed rope travel.
- **Negative/control.** Equal masses stay balanced and wheels stay still; a route ending at a pulley (open) is dashed, transmits nothing and the load falls.
- **Boundaries.** Unequal and equal masses, reversed declaration order, 3D (depth) route as in `pulley_depth`, pendulum through a pulley, invalid graphs (branch, loop, missing length), impossible all-static fixed-length route rejected, route without a dynamic participant rejected.
- **Run/Reset.** Reset restores weight poses, wheel angles to 0, and exact span lengths; replay matches.
- **Save/Load.** Save/load keeps pulley identities, `tie` sockets and span lengths.
- **Integrations.** Lever rope sockets (CAT-034), anchors (CAT-058), counterweight lessons with Switch → Lamp (CAT-063, CAT-035).

## 6. Open questions

1. Wheel collider: legacy used a 0.4 m sphere; the current shape set has no cylinder/disc. Keep a sphere, add a shape, or make the wheel non-colliding? The cylinder/disc capability is scheduled in Story 10.1 (ENGINE-CYLINDER), scheduled before the first shaft wheel by the owner on 9 Oct 2026.
2. Confirm the groove radius 0.4 m and rope-plane depth 0.12 m as the declared constants, and whether pulley size is ever configurable. Unspecified — owner decision.
3. Winding side: is the groove side chosen automatically (legacy artwork favoured the upper side) or authored per connection? It now changes physical length, not just artwork. Unspecified — owner decision.
4. EL-207 moving pulley: a CAT-053 variant or a separate future element? The requirement defers finite-inertia/frictional variants. Unspecified — owner decision.
5. Supported orientations (rotated or tilted sheave planes) for the "3D direction" test. Unspecified — owner decision.
6. Pulley contact material values. Unspecified — owner decision.
7. **Epic story vs requirement conflict.** Story 10.2 says "cutting or releasing tension uncouples the bodies". No legacy rope implements cutting (`engine/RopeNetwork.cs`, `engine/physics/PhysicsRopeJoint.cs`), CAT-053 requires only that a slack or open route transmits no tension ([CAT-053](../requirements.md#current-cat-053)), and cutting resistance belongs to the separate EL-067 steel cable. Is rope cutting in scope for Story 10.2? Unspecified — owner decision.
