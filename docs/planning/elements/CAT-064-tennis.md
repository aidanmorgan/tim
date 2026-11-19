# CAT-064 · Tennis ball — current declaration contract

Story12.1a owns Tennis; CAT-064 and EL-189/sequence-task-742 remain required. See [native spec](../../../_bmad-output/implementation-artifacts/spec-12-1a-tennis-ball.md), [requirements](../requirements.md#current-cat-064) and [testing guidance](../../testing-guidance.md). Browser qualification is pending until post-catalogue.

## Current declaration and source decisions

Tennis is a package-owned dynamic spherical capture payload, with no sockets, joints, energy stores or owned event rows. The package is elements/TennisBall, catalogue id tennis, title Tennis ball, Motion category. Its native felt-yellow scene and cream seam have a matching icon and use explicit managed MachinePart ownership.

The coordinator's19November2026 planning resolution preserves CAT's gameplay scale0.35kg/radius0.25m because the retained one-ball/two-ball load control at0.5kg depends on it. Parent Story12.1's explicit restitution0.85 supersedes historical0.78. The conflicting PRD real-world scale0.057kg/0.033m is superseded for this outcome. Friction0.3 and bounce threshold0.1m/s remain declared values. Rolling resistance0 adds no unprescribed rolling-loss mechanism; rolling arrest from resistance is not promised.

All five real parameters are BuildOnly and persist canonically:

| Parameter | Unit | Inclusive range | Default |
|---|---|---|---|
| mass | kg | 1/1024–1024 | 0.35 |
| radius | m | 1/16–16 | 0.25 |
| bounce | dimensionless | 0–1 | 0.85 |
| drag | 1/s | 0–0.125 | 0.04 |
| buoyancy | m/s² | 0–16 | 0 |

Independent mass and radius determine solid-sphere moments0.4×mass×radius² through the shared typed principal-moment operation. Radius drives all three sphere half-extents and the complete visual subtree scale radius/0.25. Bounce binds the exact base body's restitution; all colliders of that body share its contact material. Unknown, missing, malformed, nonfinite or out-of-domain inputs refuse atomically.

## Atmosphere and downstream ownership

Story12.1a explicitly freezes a uniform normalized atmosphere p=1. Shared authored world acceleration is(0,-9.81+buoyancy×1,0); shared linear damping is drag×1. Nonzero buoyancy executes; it is not a placeholder or an ignored control. No pressure input is admitted.

Variable pressure/profiles, finite atmosphere and applicable gas/liquid coupling remain with Story12.1b/S470 before their first consuming interaction, including Fan12.2. This follows the [contact/material-before-jet boundary](../invest/vertical-delivery.md#existing-element-order) and [mode-specific applicability](../general-engine-element-map.md). It does not claim fluid simulation or conserved airflow. Fan owns the matched light/heavy, no-fan, missed/blocked-jet controls and source lesson fixtures. Balloon remains a separate slice.

## Required acceptance

Native production tests prove configured mass/inertia/material/geometry, gravity and nonzero buoyancy, linear damping, rebound and stable supported rest; Basketball/Bowling provide matched controls. Receiver capture, Switch impact and the0.5kg Pressure plate one-versus-two-ball load control remain required. Save/load preserves decoded five-field configuration and repeats behavior; Run/Pause/Step/Reset restores authored state. Unknown references, incompatible units, duplicate property writers, invalid domains and run-control misuse refuse before installation.

Use independently justified quantity-specific tolerances and finite generous observation windows. Preserve causal impact/rebound and required occurrences, not historical impact ticks, whole trajectories, output bytes or bit-for-bit material-by-kind restrictions. Native geometry/art agreement is required now; actual Chrome construction/configuration/motion/Reset/save/load is pending at post-catalogue, not an early gate. Remote build/artifact/deployment qualification remains unverified unless independently established.

## Historical evidence interpretation

The following harvest records remain evidence of source behavior. Its numerical timing/replay wording is historical, governed by the current acceptance above and the owner's game-fidelity policy. Historical catalogue values do not override the explicit current defaults or five-field configuration.

## Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Material: mass 0.35, bounce 0.78, radius 0.25, buoyancy 0, drag 0.04. | `parts/catalog/tennis.tres@a6c914e:L14-L14` | Carry forward, except bounce pending the owner decision. Do not carry forward the legacy float parameter dictionary. The values enter the package declaration (f32); the compiled lanes are f32 since Story 9A.44, so f32 is no precondition of Story 12.1. |
| 2 | Legacy ball contact material: restitution = bounce, bounce threshold 0.1, friction 0.3 (argument order restitution, threshold, friction). | `reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/physics/PersistentContactPair.cs@a6c914e:L23-L23` | Carry forward as declared material values, not as a CPU solver path. |
| 3 | Legacy drag load = drag × world pressure; buoyant force = buoyancy × pressure × mass (zero for tennis). | `reference/cpu/MachineWorld.cs@a6c914e:L869-L883` | Carry forward the law as declaration input. Do not carry forward the per-substep CPU loop. |
| 4 | Legacy ball parameter enum: Radius, Mass, Bounce, Buoyancy, Drag; Sphere envelope; solid-sphere dynamics. | `reference/cpu/BallPart.cs@a6c914e:L4-L13` | Carry forward the field set, as the package's declared mass properties and collider material. |
| 5 | Acceptance fact: each ball kind dropped in isolation at zero pressure reaches first impact at √(2h/g) ± 0.02 s and rebounds to bounce² ± 0.015 of the fall height; replay after Reset is identical. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L67` | Carry forward as the rest/bounce acceptance. |
| 6 | Acceptance fact: a tennis ball dropped from y = 3 bounces (vertical speed > 0.1 at least once), never sinks more than 1 mm below the bench, and settles (< 0.05 m/s) within 2400 ticks. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L48` | Carry forward. |
| 7 | Acceptance fact: one tennis ball does not press a Pressure plate with minimum mass 0.5; two tennis balls (0.7 kg) do. | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L38-L62`; `parts/catalog/pressure_plate.tres@a6c914e:L14-L14` | Carry forward as the CAT-052 integration control. |
| 8 | Every catalogue ball must carry all five parameters; a missing one rejects. | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L81` | Carry forward as "unknown or incomplete material rejects". Do not carry forward the string-keyed parameter dictionary. |
| 9 | Lessons: tennis is the airflow payload in 22 levels. Canonical single lane `air_mail`: tennis locked at (-3, 5, 0), basket at (1.7, 0.9, 0), fan solution at (-4, 3.8, 0); "Air pushes a light ball much more than a heavy one." | `content/puzzles.json@a6c914e:L495-L706` | Carry forward as the Story 12.2 lesson set-up. |
| 10 | Depth lesson `third_dimension`: tennis at (-1, 5, -2), basket at (-1, 0.9, 2), fan at (-1, 3.8, -3) yawed 90°. | `content/puzzles.json@a6c914e:L918-L1128` | Carry forward. |
| 11 | Activation lesson `wind_signal`: tennis at (-3, 5, 0) strikes a Switch at (1.7, 0.9, 0) that lights a Lamp at (4.5, 1, 0). | `content/puzzles.json@a6c914e:L1811-L2092` | Carry forward. |
| 12 | Gated lesson `cold_start`: unpowered fan, Bowling trigger at (-5.8, 2.4, 0), Switch solution at (-5.8, 1.6, 0) wired to the fan. | `content/puzzles.json@a6c914e:L2093-L2444`; `tools/Campaign/Program.cs@a6c914e:L64-L75` | Carry forward. |
| 13 | The other 18 tennis lanes reuse the `air` module shifted in depth (two_deliveries, air_and_belt, wind_and_dominoes, powered_post, mixed_signals, start_and_topple, cross_breezes, spatial_signals, three_deliveries, triple_signal, cold_front, chain_mail, bounce_mail, relay_workshop, double_cold_start, cold_signals, depth_telegraph, cold_bridges). | `tools/Campaign/Program.cs@a6c914e:L16-L19`; fixture list in `docs/coverage/catalogue-elements.json` | Carry forward as campaign inputs for Epic 15. |
| 14 | The legacy catalogue stored no friction, threshold or rolling resistance for tennis. | `parts/catalog/tennis.tres@a6c914e:L14-L14` | Recorded gap; see Open questions. |

**Files harvested:**
- `parts/catalog/tennis.tres`
- `parts/scenes/ball.tscn` (no element knowledge: script reference only)
- `reference/cpu/BallPart.cs`
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `engine/physics/PersistentContactPair.cs`
- `CuriousContraptions.tests/PhysicsCalibrationTests.cs`
- `CuriousContraptions.tests/FloorTests.cs`
- `CuriousContraptions.tests/PressurePlateTests.cs`
- `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`
- `content/puzzles.json` (kept; not deleted by Epic 7)
- `tools/Campaign/Program.cs`
- `reference/p054-guide/puzzles-candidate.json` (no additional knowledge: the same tennis/fan placements in an older orientation schema)
