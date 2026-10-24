# CAT-064 · tennis — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-064 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Legacy citations are `path@a6c914e:Lstart-Lend` and stay retrievable from git history after Epic 7. Current-tree citations also use the a6c914e baseline.

## Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-064 · `tennis` (catalogue title "Tennis ball") |
| Requirement anchor | [CAT-064](../requirements.md#current-cat-064); D/I/V row [CAT-064-I](../invest/current-consumers.md#cat-064-i) |
| Mapped identities | [EL-189 Tennis ball](../invest/named-elements.md#element-189). No TH, RAD or GAP identity. |
| Capability row | [general-engine-element-map, CAT-064](../general-engine-element-map.md) |
| Roadmap story | [12.1a Tennis ball](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership); retain every CAT-064 mode and mapped named-identity obligation. |
| Harvest-baseline status | Not started. No `WorkshopPartKind` member exists (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## Declaration

- **Bodies and shapes.** One dynamic sphere declared as a zero-parameter payload package, exactly as the Basketball and Bowling ball are since C-Balls (`elements/Ball`, `elements/BowlingBall`): the package declares its mass properties and one sphere collider baked from its scene, and the shared instance loop of `WorkshopPhysicsCompiler.Compile` emits one dynamic body, one material and one sphere collider. `DeclaredPhysics.IsPayload` makes it a payload (Receiver capture, triggers, Bumper work) with no engine edit. Radius 0.25 m (legacy catalogue).
- **Mass and material.** Declared in the package's mass properties and collider material (no engine arm, no per-kind engine table):

  | Field | Value | Source |
  | --- | --- | --- |
  | Radius | 0.25 m | `parts/catalog/tennis.tres@a6c914e:L14-L14` |
  | Mass | 0.35 kg | same |
  | Bounce (restitution) | 0.78 legacy; Story 12.1 states 0.85 | same; see Open questions |
  | Drag | 0.04 1/s | same |
  | Buoyancy | 0 m/s² | same |
  | Friction | 0.3 | legacy ball default, `reference/cpu/MachinePart.cs@a6c914e:L236-L236` |
  | Bounce threshold | 0.1 m/s | same |
  | Rolling resistance | unspecified | see Open questions |

  Admission bounds that apply: restitution 0–1, friction 0–1, rolling resistance 0–0.1 (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`); mass 1/1024–1024 kg and drag 0–0.125 1/s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L82`); sphere radius 1/16–16 m (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`). All tennis values fit.

  Storage type: the package declares f32; the compiled body, collider and material lanes are still binary16 until Story 9A.44 ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)). The tennis package follows whatever those lanes are when Story 12.1 lands.
- **Constraints and joints.** None. A free dynamic body.
- **Typed sockets and ports.** None.
- **Sensors and activation.** None owned. It is a payload for Receiver capture, Switch impact and Pressure plate load (see Legacy harvest).
- **Work and energy stores.** None.
- **Parameters.** None: the material is the package declaration (package `Parameters: []`), and a generic record carrying any typed field or parameter is refused (`WorkshopWireFlatBuffers.CarriesTypedField`). Legacy exposed mass, bounce, radius, buoyancy and drag as authored parameters ([CAT-064 configuration](../requirements.md#current-cat-064)); see Open questions.
- **Cosmetic curves and UI bindings.** None (the package declares no cosmetic curve).
- **Art.** The package's own scene: a sphere mesh committed at the declared radius, equal to its baked collider (as `elements/Ball/ball.tscn` and `elements/BowlingBall/bowling.tscn`; there is no runtime resize). Palette `#b8db59` (`DESIGN.md@a6c914e:L167-L167`); palette icon `ui/WorkshopIcons.cs@a6c914e:L40-L40`.
- **Catalogue and inventory entry.** Catalogue id `tennis`, title "Tennis ball", category Motion, description "Light, small and bouncy. Responds strongly to moving air." (`parts/catalog/tennis.tres@a6c914e:L8-L14`). The current catalogue form is the package's entry in the generated element bundle (Story 9A.14), as `elements/BowlingBall` is.

## Engine capabilities

Families: the [CAT-064 row](../general-engine-element-map.md) and `docs/coverage/catalogue-elements.json` (AerodynamicDrag, Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction).

- **Exists now.** RigidBodyDynamics, ContactImpulse, SlidingFriction, GeometryQuery and declared linear drag through the dynamic sphere path (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`; [f32 capability inventory](../../gpu-f32-physics.md)). Rolling resistance exists as a material field.
- **Missing.**
  - Buoyancy and EnvironmentState pressure. No buoyancy is compiled for a dynamic body (no ball package declares a fluid payload), and the world has no pressure value (gravity is the constant 9.81, `engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L124`). Built by Story 12.1. Tennis buoyancy is zero, so the dry mode does not wait for it ([applicability note](../general-engine-element-map.md)).
  - Airflow receiver (fan jet force on the ball): Story 12.2 (CAT-028). Gas decision owner S470 ([decisions](../invest/decisions.md#s470)).
- **Element dependencies.** Fan (CAT-028) for every airflow mode. The legacy lessons also use Basket/Receiver (CAT-004), Switch (CAT-063), Lamp (CAT-035), Bowling ball (CAT-014), Basketball (CAT-001), Ramp (CAT-054), Domino (CAT-023) and Conveyor (CAT-019). Pressure plate (CAT-052) gives the load control.

## Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Material: mass 0.35, bounce 0.78, radius 0.25, buoyancy 0, drag 0.04. | `parts/catalog/tennis.tres@a6c914e:L14-L14` | Carry forward, except bounce pending the owner decision. Do not carry forward the legacy float parameter dictionary. The values enter the package declaration (f32); the compiled lanes stay binary16 until Story 9A.44, which is not a precondition of Story 12.1. |
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

## Acceptance outline

Point of truth: [CAT-064](../requirements.md#current-cat-064) and [simulation and element acceptance](../requirements.md#accept-simulation).

- **Chrome construction.** Through the real palette, place a Tennis ball, a Basketball and a Bowling ball with the move gizmo on separate lanes, as `tools/e2e/cat-014.test.ts` does for the Bowling ball.
- **Positive.** Each ball drops and rests at its own radius; the tennis ball rebounds highest; its rest follows its declared drag and rolling resistance.
- **Negative or control.** Basketball and Bowling lanes released identically rebound lower. With a Fan (after Story 12.2): the tennis ball is deflected into the Receiver; a Bowling ball in the same jet is not; a Wall between fan and ball, or a missed jet, leaves the tennis ball falling straight.
- **Boundaries.** Rest on the bench within 1 mm; isolated drop timing and rebound ratio as fact 5; material admitted only bit-for-bit for its kind.
- **Run/Reset.** Reset restores identity rotation and exact position; a second Run replays the committed states.
- **Save/Load.** Save, reload the page, Load: kind and material survive and the behaviour repeats.
- **Integrations.** Receiver capture, Switch impact, and Pressure plate (two balls press, one does not).

## Open questions

1. **Bounce.** Legacy 0.78 (`parts/catalog/tennis.tres@a6c914e:L14-L14`) versus Story 12.1 "e = 0.85". Unspecified — owner decision.
2. **Rolling resistance.** No legacy value; Basketball declares 0.035 and Bowling 0.03 (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L51`). Unspecified — owner decision.
3. **Friction and bounce threshold.** Legacy used a shared default (0.3, 0.1). Confirm it stays the tennis declaration.
4. **Configurable material.** The requirement lists mass, bounce, radius, buoyancy and drag as configuration; the current engine fixes the material by kind. Decide whether these stay fixed per kind or become authored parameters.
5. **"Aerodynamic drag" in Story 12.1.** Legacy drag is linear (0.04 1/s × pressure). Confirm no quadratic drag is intended.
