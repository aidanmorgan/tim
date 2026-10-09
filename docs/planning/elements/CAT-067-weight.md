# CAT-067 · weight declaration readiness spec

This is the Story 7.0 CAT-067-D declaration readiness spec for the weight. Rope route, length, tension and pendulum facts live in [CAT-058 rope_anchor](CAT-058-rope_anchor.md); two-pulley counterbalance facts live in [CAT-053 pulley](CAT-053-pulley.md). This file holds the weight body, its mass parameter and its integrations. Baseline commit is `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge; current-engine files are cited at the same commit.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-067 |
| Kind | `weight`, catalogue title "Weight" |
| Requirement anchor | [CAT-067](../requirements.md#current-cat-067); retained behaviour [todo-141](../requirements.md#todo-141) and [todo-140](../requirements.md#todo-140) |
| Mapped identities | [EL-204 Weight](../invest/named-elements.md#element-204) (candidate — Batch A confirms) |
| Roadmap story | 10.4 Counterweight & Multi-Body Pendulum Lifting (CAT-067) in [epics.md](../../../_bmad-output/planning-artifacts/epics.md); first rope use in 10.2 |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- One dynamic body with a spherical collision envelope; radius r = 0.32 · ∛mass m — `parts/WeightPart.cs@a6c914e:L13-L15`, `parts/WeightPart.cs@a6c914e:L30-L32`; `engine/MachineData.cs@a6c914e:L99`. Derived from that formula: mass 4 (default) → 0.508 m (the legacy test pins 0.507–0.509, `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L591-L597`); mass 1 (lesson fixture) → 0.32 m; mass range 0.25–8 → 0.2016–0.64 m.
- "Locked" means fixed during editing only, never immovable in simulation — `parts/WeightPart.cs@a6c914e:L7-L8`.
- Pick radius r + 0.22 m — `parts/WeightPart.cs@a6c914e:L35`.
- Current types: dynamic `RigidBodyDeclaration` (mass bound 1/1024–1024 kg, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`) and one `ColliderShapeKind.Sphere` `ColliderDeclaration` (radius bound 1/16–16 m, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`). Both bounds admit the whole legacy range.

### Mass and material

- Mass: the authored `mass` parameter, default 4 kg (`parts/catalog/weight.tres@a6c914e:L14`), valid 0.25–8 (`parts/WeightPart.cs@a6c914e:L22-L27`).
- Inertia: solid sphere 0.4 m r² — legacy `parts/WeightPart.cs@a6c914e:L14-L15` with `engine/BodyDynamics.cs@a6c914e:L39-L44`; current `RigidMassProperties.Compile` computes the same for a sphere (`engine/gpu/RigidMassProperties.cs@a6c914e:L34-L38`).
- Material: restitution 0.08, linear drag 0 (`parts/WeightPart.cs@a6c914e:L33-L34`); bounce threshold 0.1 m/s and friction 0.3 from the legacy dynamic default (`reference/cpu/MachinePart.cs@a6c914e:L236`). No rolling resistance existed in legacy. Current pattern: the per-kind `BallMaterial` record (radius, mass, bounce, drag, buoyancy, friction, threshold, rolling resistance; `engine/gpu/WorkshopConstruction.cs@a6c914e:L38-L53`), except that the weight's radius and mass derive from its parameter (Open questions 2 and 3). Nearest heavy-sphere precedent: Bowling ball 0.28 m / 4 kg / bounce 0.14 / Crr 0.03 (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L51`); it is a precedent only, not the weight's values.

### Constraints and joints

None owned by the weight. It is a rope endpoint (role `Load`); the rope law and pendulum behaviour are declared by the rope route (CAT-058).

### Typed sockets and ports

- One socket `Tie`, domain `Rope`, bidirectional, at local (0, r + 0.08, 0) m (the eye above the body) — `parts/WeightPart.cs@a6c914e:L16-L21`; `engine/MachineData.cs@a6c914e:L100`. Role `Load`: one rope end (`engine/RopeNetwork.cs@a6c914e:L76-L77`).
- Rope domain and `Tie` socket do not exist in `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`; Story 10.2 adds them.

### Sensors and activation

None — the weight has no sensor or activation port. Its lessons trigger an existing Switch by contact.

### Work and energy stores

None declared. It exchanges gravitational potential and rope tension (EL-204: "Raising it requires actual work").

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `mass` | f32 | 0.25–8 (finite; others reject before the instance is added) | 4 | kg | `parts/WeightPart.cs@a6c914e:L22-L27`; `parts/catalog/weight.tres@a6c914e:L14`; `CuriousContraptions.tests/RopeTests.cs@a6c914e:L110-L124` |

The legacy enum `WeightParameter { Mass }` maps to the wire name `mass` (`engine/MachineData.cs@a6c914e:L92`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L308-L309`). The requirement lists one configuration mode, `mass`; no current UI control exists for it (Open question 4).

### Cosmetic curves and UI bindings

- No cosmetic curve: the body follows its committed physical pose (`DESIGN.md@a6c914e:L284`).
- Band count = ⌈mass⌉ conveys heavier loads (`parts/WeightPart.cs@a6c914e:L40-L46`); it is static art chosen from the parameter at construction.

### Art

- Scene `parts/scenes/weight.tscn` is a bare `Node3D` with the part script (`parts/scenes/weight.tscn@a6c914e:L1-L6`).
- Meshes — `parts/WeightPart.cs@a6c914e:L36-L46`: catalogue-colour cylinder radius 0.85 r, height 1.35 r; cream (`#fff8e9`) base band radius 0.87 r, height 0.12 r at y −0.55 r; gold (`#f7cb52`) eye ring radius 0.14, thickness 0.035 at y 0.72 r, rotated 90° about X; ⌈mass⌉ cream rings radius 0.86 r, thickness 0.015, spaced from y −0.35 r over 0.7 r.
- DESIGN: "Blue cylindrical loads with cream bands and gold eyes; heavier loads are visibly larger" — `DESIGN.md@a6c914e:L284`.
- Palette: "Weight" RGB 0.27, 0.39, 0.61 (`#45639c`) — `DESIGN.md@a6c914e:L184`; `parts/catalog/weight.tres@a6c914e:L13`.
- Toolbox pictogram: trapezoid body, ring on top, two band strokes — `ui/WorkshopIcons.cs@a6c914e:L95`.

### Catalogue and inventory entry

- Id `weight`, title "Weight", category "Ropes", description "A rope-linked load. Larger weights have more bands and pull harder. Connect through pulleys to lift a lighter load.", parameters `{"mass": 4.0}` — `parts/catalog/weight.tres@a6c914e:L8-L14`.
- Add `WorkshopPartKind.Weight` (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3`) and append a `WorkshopInventoryPolicy.Free` row (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).
- Levels: `counterweight` and `pulley_depth` each place a locked mass-1 `load` and stock one weight; the solution weight has mass 4 (harvest W9).

## 3. Engine capabilities

Capability families: see the CAT-067 row of the [general-engine element map](../general-engine-element-map.md) and the `weight` entry in [catalogue-elements.json](../../coverage/catalogue-elements.json). Not duplicated here.

**Exists now**

- Dynamic sphere body, collider and compiled sphere inertia: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L51`.
- Contact material with friction, restitution, threshold and rolling resistance, and the TGS Soft contact solver: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L225-L226`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878-L884`.
- A free weight that falls, rolls and collides can therefore be declared today; only its rope coupling is missing.

**Missing**

- Rope domain, `Tie` socket and `Load` role — Story 10.2.
- Tension-only rope constraint (hanging, counterbalance) — Story 10.2.
- Pendulum and counterweight qualification (period, damping, length/energy bounds, exact Reset) — Story 10.4.
- Parametric per-instance mass/radius in the kind declaration (current `BallMaterial` fixes radius and mass per kind) — Story 10.4 (or 10.2 if the weight lands there first).

**Element dependencies**

- [CAT-058 rope_anchor](CAT-058-rope_anchor.md) for hanging and pendulum; [CAT-053 pulley](CAT-053-pulley.md) for counterbalance; the lessons use the delivered Switch (CAT-063) and Lamp (CAT-035).

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| W1 | Translating load with a spherical collision envelope and a rope eye; Locked is edit-time only. | `parts/WeightPart.cs@a6c914e:L7-L8`, `parts/WeightPart.cs@a6c914e:L13` | carry forward | Matches "spherical body/socket"; a locked weight still moves in Run. |
| W2 | Mass must be finite and within 0.25–8; −1, 0, 20 and NaN reject before any instance is added. | `parts/WeightPart.cs@a6c914e:L22-L27`; `CuriousContraptions.tests/RopeTests.cs@a6c914e:L110-L124` | carry forward | Authored range and atomic rejection. |
| W3 | Catalogue default mass 4; lesson fixture loads use mass 1. | `parts/catalog/weight.tres@a6c914e:L14`; `content/puzzles.json@a6c914e:L5142-L5144` | carry forward | Requirement: default 4 is not the fixture mass 1. |
| W4 | Radius 0.32 ∛m; tie height r + 0.08. | `engine/MachineData.cs@a6c914e:L99-L100`; `parts/WeightPart.cs@a6c914e:L19-L20`, `parts/WeightPart.cs@a6c914e:L32` | carry forward | Size encodes mass; rope length depends on the tie height. |
| W5 | Solid-sphere dynamics with authored initial velocity. | `parts/WeightPart.cs@a6c914e:L14-L15`; `engine/BodyDynamics.cs@a6c914e:L39-L44` | carry forward the law; do not carry forward `BodyDynamics` | Current `RigidMassProperties` already compiles the sphere moment. |
| W6 | Restitution 0.08, drag 0, bounce threshold 0.1 m/s, friction 0.3. | `parts/WeightPart.cs@a6c914e:L33-L34`; `reference/cpu/MachinePart.cs@a6c914e:L236` | carry forward as candidate values (Open question 2) | Authored legacy values; current material also needs rolling resistance. |
| W7 | Artwork: cylinder body, base band, gold eye, ⌈mass⌉ bands (dimensions in Art). | `parts/WeightPart.cs@a6c914e:L36-L46` | carry forward the art; the collision envelope stays spherical (Open question 1) | Requirement: solid pose and rope attachment agree. |
| W8 | Rope eye socket at (0, r + 0.08, 0); role `Load` takes one rope end. | `parts/WeightPart.cs@a6c914e:L16-L21`; `engine/RopeNetwork.cs@a6c914e:L76-L77` | carry forward | Typed socket. |
| W9 | Levels: `counterweight` — locked `load` mass 1 at (−2, 1, 0.12), solution `weight_1` mass 4 at (2, 5, 0.12), inventory weight 1. `pulley_depth` — same load, solution `weight_1` mass 4 at (2, 5, 2.12), inventory weight 1 and pulley 1. Goal: lamp activated via the overhead Switch at (−2, 3.2, 0.12), rotated 180°. | `content/puzzles.json@a6c914e:L5072-L5145`, `content/puzzles.json@a6c914e:L5273-L5333`, `content/puzzles.json@a6c914e:L5399-L5471`, `content/puzzles.json@a6c914e:L5508-L5582`, `content/puzzles.json@a6c914e:L5781-L5845` | carry forward | Lesson set-ups to rebuild in Epic 15. |
| W10 | Lesson authoring source for both levels (positions, masses, rope spans by connect-time distance). | `tools/Campaign/Program.cs@a6c914e:L173-L223`, `tools/Campaign/Program.cs@a6c914e:L408-L413` | carry forward | Campaign rebuild input. |
| W11 | Acceptance: both lessons are won at precision 0, 0.45 and 1 within 1500 ticks; removing any one rope span fails the lesson; changing `weight_1` to mass 1 fails ("Equal counterweights cannot raise the load to the switch"). | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L70-L108` | carry forward | Requirement: equal/unequal ratios, slack release/open route. |
| W12 | Acceptance: counterbalance acceleration, height-sum conservation and order independence through two pulleys. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L21-L68` (detail in CAT-053 P9) | carry forward | Shared with CAT-053. |
| W13 | Acceptance: floor contact — a mass-1 and a mass-4 weight never sink below the bench while roped over pulleys for 900 ticks. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L217-L241` | carry forward | Requirement: floor contact. |
| W14 | Acceptance: 3D pendulum stays within rope length and gains no energy (detail in CAT-058 R20). | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L191-L215` | carry forward | Requirement: 3D pendulum, energy/length bounds. |
| W15 | Acceptance: a slack rope between two weights does not suppress their contact; equal masses exchange velocities scaled by the product of restitutions. | `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L223-L255` | carry forward | Connected-body collision stays enabled. |
| W16 | Acceptance: Reset rebuilds identical bodies, masses and ordered routes. | `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L257-L280` | carry forward | Exact Reset. |
| W17 | Integration (CAT-071): the default weight (r ≈ 0.508) is wider than the wound-spring bore, rests on the rim, never enters it, receives a physical tap and rises by more than 0.01 m but no more than (stored energy + initial KE)/(m g). | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L591-L597`, `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L618-L623`, `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L655-L661` | carry forward | Oversized-payload control for the wound spring. |
| W18 | Integration (optics): a mass-8 weight occludes a torch–solar path using its committed rotated body; disabling its collider restores the path. | `CuriousContraptions.tests/LightTests.cs@a6c914e:L180-L208` | carry forward | Occlusion follows committed body geometry (Epic 13 integration). |
| W19 | Integration (CAT-065): a default weight tethered over a trampoline (see CAT-058 R23). | `CuriousContraptions.tests/TrampolineRopeTests.cs@a6c914e:L45-L47` | carry forward | Tether integration. |
| W20 | Parameter name boundary `WeightParameter.Mass` ↔ `"mass"`; undefined enum values throw. | `engine/MachineData.cs@a6c914e:L92`; `CuriousContraptions.tests/SceneRopeBindingTests.cs@a6c914e:L308-L309` | carry forward | Typed parameter, string only at serialization. |

### Files harvested

- `parts/WeightPart.cs`
- `parts/catalog/weight.tres`
- `parts/scenes/weight.tscn`
- `engine/MachineData.cs`
- `engine/BodyDynamics.cs`
- `engine/RopeNetwork.cs`
- `reference/cpu/MachinePart.cs`
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `reference/p025-production-isolated-20261004/src/ui/WorkshopIcons.cs` (checked, duplicate of the surviving pictogram)
- `tools/Campaign/Program.cs`
- `content/puzzles.json`
- `CuriousContraptions.tests/RopeTests.cs`
- `CuriousContraptions.tests/SceneRopeBindingTests.cs`
- `CuriousContraptions.tests/WoundSpringTests.cs` (weight usage only)
- `CuriousContraptions.tests/LightTests.cs` (weight occluder only)
- `CuriousContraptions.tests/TrampolineRopeTests.cs` (weight usage only)
- `CuriousContraptions.tests/RopeOwnershipTests.cs` (harvested into CAT-053)
- `CuriousContraptions.tests/WoundSpringTests.cs.orig` (checked; superseded copy of WoundSpringTests, no extra weight knowledge)
- `CuriousContraptions.tests/LinearPusherTests.cs`, `CuriousContraptions.tests/TrampolineTests.cs` (checked; they set `WeightParameter.Mass` on `ball` parts, not weights)
- `reference/p054-guide/puzzles-candidate.json` (checked; same lessons apart from the orientation encoding)

## 5. Acceptance outline

Requirement row: [CAT-067](../requirements.md#current-cat-067); stories 10.2 and 10.4.

- **Chrome UI recipe.** Place a Weight from the toolbox; select its mass through the admitted control or authored fixture (Open question 4); place a Rope anchor or Pulleys and connect `tie` sockets with the contextual wiring UI. Verify committed mass, radius and connection.
- **Positive.** Hanging: the weight hangs taut from an anchor. Counterbalance: a mass-4 weight lifts a mass-1 load through two pulleys into the Switch and lights the Lamp. Pendulum: released from an angle, it swings in 3D with period T = 2π√(L/g) and damps gradually (story 10.4).
- **Negative/control.** Equal masses stay balanced; an unconnected or open-route weight falls freely and rests on the bench.
- **Boundaries.** Mass 0.25 and 8 accepted, out-of-range rejected atomically; slack release; floor contact; direction/order; energy and length bounds.
- **Run/Reset.** Reset restores initial pose, zero velocity and angular displacement exactly; replay matches.
- **Save/Load.** `mass` and rope endpoints survive save/load; unknown parameters reject.
- **Integrations.** Pulley (CAT-053), anchor (CAT-058), trampoline tether (CAT-065), wound-spring oversized control (CAT-071), optical occlusion (Epic 13).

## 6. Open questions

1. Collision shape versus artwork: legacy collides as a sphere but draws a cylinder (0.85 r radius, 1.35 r tall). Keep the sphere and reshape the art, or change the collider? A cylinder collider has no owning story — owner decision. Owner decision 9 Oct 2026: new cylinder-collider story before first shaft wheel; Story 10.1 (ENGINE-CYLINDER).
2. Contact material: confirm restitution 0.08, friction 0.3, threshold 0.1 m/s, drag 0, and set a rolling-resistance coefficient (none in legacy; current materials require one). Unspecified — owner decision.
3. Confirm the radius law 0.32 ∛m and tie offset 0.08 m. Unspecified — owner decision.
4. How the player sets `mass`: a UI control, or authored fixtures only? No current control exists. Unspecified — owner decision.
5. **Epic story vs requirement conflict.** Pendulum damping source for story 10.4 ("damps gradually"): legacy weight drag is 0 and the rope is lossless. Which explicit physical law supplies the damping? Unspecified — owner decision.
6. Pendulum length L for T = 2π√(L/g): from the anchor socket to the weight's centre of mass (rope length plus tie offset)? Unspecified — owner decision.
