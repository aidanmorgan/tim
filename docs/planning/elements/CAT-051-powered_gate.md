# CAT-051 · powered_gate declaration readiness spec

**Current delivery (24 October 2026):** [CAT-051 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Colliders (owner, 25 October 2026, "visible = solid"):** every part a player sees as solid collides with its true shape. Since [`engine-hollow-shapes`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-hollow-shapes) the sleeve is a true Tube from bore radius 0.65 m to its drawn outer radius 0.70 m over the 0.8 m sleeve, and each 0.78 m collar a true Tube from the bore to 0.78 m over its 0.18 m, reaching the mouths at x ±0.49 ([hollow geometry](../../hollow-geometry-design.md)); the sleeve's 16-box approximation is deleted, and a ball dropped on a collar stops at the drawn 0.78 m (`VisibleSolidColliderTests`). The PowerIn socket (sphere r 0.09 at (0, 1.65, 0.92)) and the indicator (sphere r 0.07 at (0.42, 1.65, 0.55)) collide as drawn (coordinator decision, 25 October 2026). Material: the frame now declares this doc's restitution 0.1, as the blade already did (it declared 1). Pair restitution is the product, as it was in the legacy engine, so 1 made a ball leave the frame at its own restitution; a drop onto the housing tests 0.55 × 0.1.

Story 7.0 CAT-051-D readiness spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Current-engine files are cited at the same commit. Values are authored as f32 game values under [the numeric contract](../../gpu-f32-physics.md#numeric-representation-and-precision). The blade law is the generic sprung prismatic slider with a supplied drive, which the planned [CAT-007 beam shutter](CAT-007-beam_shutter.md) declares too. The two parts differ only in geometry, mouths and integrations.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-051 |
| Kind | `powered_gate` (catalogue title "Powered gate", category Motion) |
| Requirement anchor | [CAT-051](../requirements.md#current-cat-051); retained behaviour [todo-244](../requirements.md#todo-244); consumers [CAT-051-I](../invest/current-consumers.md#cat-051-i) |
| Mapped identities | None found in [named-elements](../invest/named-elements.md). EL-184 mechanical ball gate (linkage-driven) and EL-163 powered ball diverter are separate elements. |
| Roadmap story | 8.2 Powered Gate Passageway Barrier ([epics](../../../_bmad-output/planning-artifacts/epics.md), Epic 8) Current [CAT-051 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status (22 November 2026) | **Delivered.** Package id `powered_gate`, directory `elements/PoweredGate`, palette category Motion, order 20 (`elements/PoweredGate/PoweredGateElementDeclaration.cs:24`–`:29`). Slice records: [8.2 Powered gate](../../../_bmad-output/implementation-artifacts/spec-8-2-powered-gate.md) (first commit `8f682f84`, 18 October 2026); the declared drive and Blocked state by [C-Gate](../../../_bmad-output/implementation-artifacts/spec-c-gate.md); true Tube sleeve and collars by `engine-hollow-shapes`. Residuals (section 8): a declared spring rest coordinate so the blade closes in every orientation (owner decision, 22 November 2026), beam occlusion, supply drain, one clearing-and-retry test. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026); the stories that own this part's gaps are `ENGINE-JOINT-KINDS` (spring rest position), `ENGINE-OPTICAL` (occlusion and the sleeve's Transmit response), `ENGINE-SUPPLY-DEMAND` (drain) and `ENGINE-ISLANDS-SLEEP` (power-bound trials and the resting blade). Browser qualification pending. Checked on `8d1d68aa`; published main is now `25df3cd5`, and its two extra commits touch none of this package's files. |

## 2. Declaration

### Delivered declaration (22 November 2026)

Read from `elements/PoweredGate/PoweredGateElementDeclaration.cs` and `powered_gate.tscn`.

- **Interfaces.** `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`:14`). Footprint: 2 bodies, 1 moving, 1 prismatic (`:34`).
- **Frame colliders (scene, static).** Sleeve Tube 0.8 m long, bore radius 0.65 m to outer radius 0.70 m; two collar Tubes 0.18 m long at x ±0.4, bore to 0.78 m; housing box 0.8 × 0.65 × 1.8 m at (0, 1.65, 0); two rail boxes 0.24 × 1.7 × 0.12 m at (0, 0.65, ±0.82); `PowerIn` socket sphere r 0.09 m at (0, 1.65, 0.92); indicator sphere r 0.07 m at (0.42, 1.65, 0.55).
- **Blade (dynamic).** Box half extents (0.06, 0.72, 0.72) m; 1 kg; moments (0.3456, 0.1740, 0.1740) kg·m² (`:80`–`:83`).
- **Material, both bodies.** Restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:45`–`:49`).
- **Slider (`:76`–`:86`).** Along +Y, travel 0 to 1.55 m; return spring 14 N/m with damping 4 N·s/m; blade and frame do not collide. Supplied drive: target 1.55 m, at most 2.8 m/s on a 14 m/s² stopping profile, 60 N and 120 W.
- **Parameters, stores, events.** None (`:32`, `:52`).
- **Ports.** `PowerIn`: `PortRole.PowerIn`, Electrical, Input, at (0, 1.65, 0.92), normal +Z; its supply consumer's target is the slider drive (`:36`–`:39`, `:88`–`:90`). Two tube mouths, Start at (−0.49, 0, 0) facing −X and End at (0.49, 0, 0) facing +X, bore radius 0.65 m, are scene locators (`Loc_Port_mouth_start`, `Loc_Port_mouth_end`), not connection ports.
- **Electrical node.** `("motor", Consumer, 12 V, 24 Ω, 0 F, 20 W)` (`:92`–`:95`): validated as finite and read by no law (only `Role` is read): unread dead data, to be consumed by `ENGINE-SUPPLY-DEMAND` or removed forward-only. Supply is on/off. The 20 W figure against the drive's 120 W bound has no behavioural effect.
- **Presentation (`:16`–`:21`).** Pick radius 1.5 m; `Visual/Blade` follows body 1; the indicator takes one colour per `CommittedActuatorState`: slate `#556573` unpowered, gold `#f7cb52` powered, ochre `#e8b764` blocked.

The declaration's own summary comment (`:10`–`:13`) still describes the deleted 16-box sleeve ring and says the collars are not yet solid. The scene and the tests are as stated here; the comment is stale and is corrected by the next slice that edits the package.

### Art
- Sleeve (0.40, 0.72, 0.79, alpha 0.16), cream collars and rails `#fff8e9`, navy housing `#293954`, gold blade `#f7cb52`, socket `#e8b764`. Design authority: [DESIGN.md](../../../DESIGN.md) "Powered gate" row. Icon `elements/PoweredGate/icon.svg`.
- **Art and solidity as shipped (table row `CuriousContraptions.tests/VisibleSolidColliderTests.cs:93`–`:96`).** Solid with a matching collider: `Visual_Housing`, `Visual_Rail_Start`, `Visual_Rail_End`, `Visual_Socket`, `Visual_Indicator` on the frame and `Blade/Visual_Blade` on the blade body. Hollow with a matching Tube collider: `Visual_Sleeve`, `Visual_Collar_Start`, `Visual_Collar_End`. Decorative: none. No visible-solid defect: neither `spec-engine-art-solidity.md` nor any `spec-elements-visible-solid-remainder*.md` contract lists a Powered gate node to repair. At a glance it differs from a Pipe by the navy actuator box and gold blade standing above the sleeve.

### Catalogue and inventory entry
- Id `powered_gate`, title "Powered gate", category Motion, order 20, colour (0.97, 0.796, 0.322), description "Electricity retracts the gold shutter. Without power it returns closed, stopping if a ball blocks its path. Its mouths snap to tubes." (`elements/PoweredGate/PoweredGateElementDeclaration.cs:24`–`:29`). "Without power it returns closed" is false today for a mount past horizontal (section 6, question 1) and becomes true with the declared spring rest coordinate. No level in `content/puzzles.json` places or stocks it.

## 3. Engine capabilities

Capability families: see the [CAT-051 map row](../general-engine-element-map.md) and [catalogue coverage](../../coverage/catalogue-elements.json). They are not duplicated here.

The slider, its spring and damping, the electrical supply route and the bounded drive are generic declared capabilities: `PrismaticSliderDescriptor` with its `PrismaticDriveDescriptor`, compiled to a prismatic row and a separate `PrismaticDriveDeclaration` row, and executed by `PrismaticMotorSolver.BeginSubstep`, `Solve`, `Observe` and `MeasureWork` (`CuriousContraptions.Simulation/PrismaticMotorSolver.cs:24`, `:70`, `:99`, `:117`). No solver, compiler or warm-up code names the gate. The committed prismatic record carries a moving-contact lane, which `ActuatorObservations.IsBlocked` reads (`engine/gpu/PhysicsPresentationRead.cs:15`).

**Capability probe (22 November 2026, this checkout).** Tests are in `CuriousContraptions.tests/`; they were found by name and line, and none was run.

| # | Capability the behaviour uses | Status | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| 1 | Prismatic slider with limits, spring and damping | present | `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`), `PrismaticConstraintSolver` (`CuriousContraptions.Simulation/PrismaticConstraintSolver.cs:65`) | `PoweredGateTests.PhysicsCompiler_CompilesGateWithDynamicBladeAndPrismaticJoint` (`PoweredGateTests.cs:151`); `LatchSupplyFixtureTests.ResetWhenOpenClearsDownstreamAndSpringCloses` (`:173`) |
| 2 | Supplied prismatic drive bounded in speed, effort and power | present, with an engine cost gap | `PrismaticDriveDescriptor` (`MechanicalContractValues.cs:89`); the power bound is enforced by a whole-world snapshot and up to eleven re-solves of the substep (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Motors.cs:35`–`:66`) | `DeclaredPrismaticDriveTests.ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep` (`:92`), `ASuppliedBoxWithTheGatesBladeAndLimitsButNoDeclaredDriveNeverActuates` (`:244`) |
| 3 | On/off supply from `PowerIn`; Electrical only | present | `SupplyDriveTarget` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:52`) | `PoweredGateTests.Connections_AcceptsElectricalAndRejectsActivation` (`:131`), `DeclaredDirectGraphFollowsOnlyItsSource` (`:209`) |
| 4 | True hollow Tube colliders for the sleeve and collars | present | `ColliderShapeKind.Tube` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `PoweredGateTests.PoweredGateScene_ExtractsItsSleeveCollarsFrameSocketsAndThreePortLocators` (`:56`); `VisibleSolidColliderTests.ABallDroppedOntoEachSolidFeatureStopsAtItsDrawnSurfaceAndReboundsAtTheProductRestitution` (`:725`, features `GateHousing` `:614` and `GateCollar` `:656`) |
| 5 | Tube mouths that snap to pipe mouths | present | `TubeMouthDescriptor` (`CuriousContraptions.Abstractions/SceneGeometry.cs:39`) | `TubeMouthSnapTests.APoweredGateSnapsOntoAPipeMouth` (`:203`) |
| 6 | Blocked state from the committed moving-contact lane; indicator from committed actuator state | present | `PresentationDriver.CommittedActuatorState` (`CuriousContraptions.Abstractions/ElementPresentation.cs:24`) | `DeclaredPrismaticDriveTests.BlockedNeedsContactInteriorAndRestTogether` (`:387`); `LatchSupplyFixtureTests.BallPartlyThroughStopsClosingBladeUncrushed` (`:289`); `DeclaredPresentationTests` (`DeclaredPresentationTests.cs:120`, `:184`) for the indicator colour per state |
| 7 | Reset; save and load | present | n/a (generic lifecycle) | `LatchSupplyFixtureTests.RunResetRestoresLatchOffBladeClosedAuthoredBattery` (`:330`), `SaveLoadPreservesFiveSocketsAndReplaysIdentically` (`:351`) |
| 8 | A beam occluded by the committed blade, rails and housing, and passed by the clear sleeve | **missing** | no beam contract exists in the engine | `ENGINE-OPTICAL`, under the optical conventions of [EL-176](EL-176-red-laser-emitter.md) section 10: every collider absorbs by default, so the sleeve Tube declares Transmit (1, 1, 1) as a criterion of `ENGINE-OPTICAL` (item 6). The acceptance row runs in the first optical element slice that places a gate (CAT-036 Laser or CAT-007 Beam shutter) |
| 9 | Drive work debiting the source (drain) | **missing** | supply is binary | `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`; owner decision 3a, 22 November 2026) |
| 10 | A slider spring with a declared rest coordinate, so the closed blade is preloaded | **missing** | a spring's rest is its coordinate zero, and preload through the limits is refused (`CuriousContraptions.Bundle/MechanicalValueDomain.cs:81`) | `ENGINE-JOINT-KINDS`, spring rest position (owner decision 6; coordinator decision 21, 22 November 2026) |

Counts: 7 present, 0 untested, 0 narrower, 3 missing. Row 2 is present for behaviour; its enforcement cost is owned by `ENGINE-ISLANDS-SLEEP`, whose contract confines drive-budget trials to the island they affect.

**Element dependencies.** Battery; Pipe for mouths and routes; supply partners Hold timer, Counter and Pressure plate; obstruction partners Cannon (muzzle) and Wound spring (plunger). All delivered, but the campaign teaches the Counter, Pressure plate, Cannon and Wound spring after the gate (taught at 24 with the Hold timer), so the gate rows that use them (facts 23 to 25) are cross-part rows owned by those parts' slices: [CAT-020](CAT-020-counter.md) and [CAT-052](CAT-052-pressure_plate.md) (fact 23), [CAT-016](CAT-016-cannon.md) (fact 24) and [CAT-071](CAT-071-wound_spring.md) (fact 25), each of which lists the row it owns (coordinator decisions 36 and 41). Fact 22's Hold timer is taught with the gate at 24, so fact 22 stays in the gate's slice.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Electric retracting shutter; shared rigid-body motion drives the visible blade | `parts/PoweredGatePart.cs@a6c914e:L7` | Carry forward | One-way data flow |
| 2 | Stroke 1.55 m; blade half extents (0.06, 0.72, 0.72); material (0.1, 0.1, 0.3) | `parts/PoweredGatePart.cs@a6c914e:L15`, `parts/PoweredGatePart.cs@a6c914e:L22-L23` | Carry forward | Authored values |
| 3 | PowerIn electrical input at (0, 1.65, 0.92); mouths ±0.49 m with the common bore | `parts/PoweredGatePart.cs@a6c914e:L24-L30` | Carry forward | Typed port and mouths |
| 4 | Frame geometry: sleeve, collars, housing and rails as in section 2 | `parts/PoweredGatePart.cs@a6c914e:L32-L46` | Carry forward | Committed functional geometry |
| 5 | Indicator colours; `Powered` event while powered | `parts/PoweredGatePart.cs@a6c914e:L57-L63` | Carry forward colours; event as presentation only | Per-element observe loop not carried forward |
| 6 | Controller constants: mass 1, maximum speed 2.8, acceleration 14, stiffness 14, damping 4, effort 60, power 120, endpoint tolerance 1e-5 | `engine/SlidingBlade.cs@a6c914e:L14-L21` | Carry forward magnitudes except the tolerance | Tolerance re-frozen in f32 (Open question 3) |
| 7 | Slider declaration: frame rotation (−π/2, 0, 0), range [0, stroke], connected collision disabled | `engine/SlidingBlade.cs@a6c914e:L28-L34` | Carry forward | Declaration data |
| 8 | Each tick: spring and damping loads always; drive only when powered, with the stopping profile and a work budget of power × Δt | `engine/SlidingBlade.cs@a6c914e:L36-L47` | Carry forward the law | The per-element `Prepare` loop is a CPU path |
| 9 | Classification: Closed ≤ 1e-5; Open within 1e-5 of stroke; Blocked when \|speed\| < 1e-5; otherwise Opening if powered, Closing if not | `engine/SlidingBlade.cs@a6c914e:L49-L53` | Carry forward as a typed enum; re-freeze thresholds | f32 |
| 10 | Opening is read from the live guide joint; a detached or missing guide rejects instead of defaulting | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L13-L52` | Carry forward | Explicit rejection |
| 11 | Acceptance: a gate rotated 90° about Z supports a settled ball (y 3.3999–3.4002 after 360 ticks); powering it drops the ball below y 1 within 180 ticks; unpowered it holds; Reset is exact | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L79-L116` | Carry forward the behaviour | Horizontal mounting control |
| 12 | Acceptance: a Delay cannot connect (activation rejected). Under Battery supply the opening rises ≤ 2.8 m/s × tick per tick at speed 0–2.8, reaching Open at exactly 1.55 m within 180 ticks. Reset gives Closed, 0 and 0 and keeps the connection | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L118-L150` | Carry forward | Typed domain; bounded motion |
| 13 | Acceptance: at rotations (0, 0, 0) and (20°, 30°, 40°), a ball at 1 m/s from local x −2 passes to x 1.99–2.01 when powered and stops at x < −0.39 when unpowered | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L152-L177` | Carry forward | Rotated passage |
| 14 | Acceptance: when supply is removed with a ball moving at 0.5 m/s at x −0.25, the closing blade traps it against the lower tube. State Blocked, opening between 0.001 and stroke − 0.001, both speeds ≤ 1e-7, contacts with blade and frame, separation ≥ −1e-6. Without removal there is no contact. Reset replays bit-identically | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L179-L240` | Carry forward the behaviour; not the 1e-6/1e-7 values | Game-grade envelope replaces CPU tolerances |
| 15 | Acceptance: unpowered return follows a spring-damper with energy (KE + ½kx²) never increasing and no drive use; Reset replay is exact, both rotated and excited at 2 m/s | `CuriousContraptions.tests/SlidingBladeDynamicsTests.cs@a6c914e:L24-L83` | Carry forward energy and replay; not the exact substep recurrence | Recurrence is the CPU integrator |
| 16 | Queries (all trace media) use the solved blade: closed blocks at about 1.9–2 m from local x −2; a moved blade clears; restoring blocks again; presentation never rewrites construction | `CuriousContraptions.tests/SlidingBladeOwnershipTests.cs@a6c914e:L25-L92` | Carry forward | Blade is the occluder |
| 17 | Acceptance: the powered blade keeps a stable shape; the rendered pose equals the physics pose within 1e-6; after 180 ticks it moves > 1.2 m and the passage sweep and light trace are clear; Reset restores pose and sweep | `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs@a6c914e:L32-L88` | Carry forward | Committed geometry |
| 18 | Acceptance: an unpowered blade keeps its closed pose for 60 ticks and obstructs the sweep | `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs@a6c914e:L90-L108` | Carry forward | Negative control |
| 19 | Acceptance: exactly one slider joint; per-substep drive work ≤ 120 × h and impulse ≤ 60 × h; travel stays within range; after supply loss there is no drive use and the blade returns to 0 at rest within 240 ticks; replay is identical | `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs@a6c914e:L110-L165` | Carry forward | Finite work; fail-closed |
| 20 | The return spring uses the owned orientation, not presentation; corrupting the scene transform does not change the outcome | `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs@a6c914e:L167-L217` | Carry forward | One-way data flow |
| 21 | The blade is dynamic, mass 1 kg, with positive-definite inertia; velocity = part +Y × opening speed; zero angular velocity; Reset restores dynamics | `CuriousContraptions.tests/SceneBodyDynamicsTests.cs@a6c914e:L286-L321` | Carry forward | Body declaration |
| 22 | Integration: a battery-supplied hold timer powers the gate while counting and drops it on expiry; retrigger repowers it | `CuriousContraptions.tests/HoldTimerTests.cs@a6c914e:L10-L26`, `CuriousContraptions.tests/HoldTimerTests.cs@a6c914e:L59-L100` | Carry forward | Electrical consumer |
| 23 | Integration: a counter powers the gate only at target and only with supply; a pressed, supplied pressure plate opens it; unloading closes it within 180 ticks | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L46-L70`; `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L44-L82` | Carry forward | Electrical consumer |
| 24 | Integration: a closed gate in front of a cannon muzzle makes the shot Obstructed with charge retained (45 J); opening it (> 1.1 m in 180 ticks) then a fresh trigger fires | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L452-L498` | Carry forward | Cross-element obstruction |
| 25 | Integration: a timer-driven gate rotated 90° blocks a released wound-spring plunger (spring Blocked, charge retained) | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L457-L501` | Carry forward | Cross-element obstruction |

### Files harvested
- `parts/PoweredGatePart.cs`
- `parts/catalog/powered_gate.tres`
- `parts/scenes/powered_gate.tscn`
- `engine/SlidingBlade.cs`
- `engine/TubeProxy.cs`
- `engine/TubeMouth.cs`
- `engine/physics/PersistentContactPair.cs` (ContactMaterial field order only)
- `reference/cpu/MachinePart.cs` (AddBox semantics only)
- `ui/WorkshopIcons.cs` (pictogram; not deleted by Epic 7)
- `CuriousContraptions.tests/PoweredGateTests.cs`
- `CuriousContraptions.tests/SlidingBladeDynamicsTests.cs`
- `CuriousContraptions.tests/SlidingBladeOwnershipTests.cs`
- `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs`
- `CuriousContraptions.tests/SceneBodyDynamicsTests.cs` (blade section)
- `CuriousContraptions.tests/AxialMotionOwnershipTests.cs` (unpowered moving blade reads Closing, L87)
- `CuriousContraptions.tests/HoldTimerTests.cs` (gate consumer)
- `CuriousContraptions.tests/CounterTests.cs` (gate consumer)
- `CuriousContraptions.tests/PressurePlateTests.cs` (gate consumer)
- `CuriousContraptions.tests/CannonTests.cs` (muzzle gate section)
- `CuriousContraptions.tests/WoundSpringTests.cs` (gate section)
- `CuriousContraptions.tests/BodyLocalBoxTests.cs` (checked, no element-specific knowledge)
- `content/puzzles.json` (checked, no element knowledge)
- `tools/P0-007-actual-path-probe/inputs.json` (checked, no element knowledge: file hashes only)
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json` (checked, no element knowledge: catalogue id lists only)

## 5. Acceptance outline

This outline points to [CAT-051](../requirements.md#current-cat-051), [todo-244](../requirements.md#todo-244) and Story 8.2 rather than restating them.
- **Chrome UI recipe.**
  1. In actual Chrome, place a Ramp and a Pipe run with the Powered gate snapped between pipe mouths.
  2. Place a Basketball upstream and a Battery.
  3. Wire Battery Supply to the gate's PowerIn with the contextual wiring UI.
  4. Verify the mouth alignment and the typed electrical connection from the committed construction read.
- **Positive.** Supply opens the blade at bounded acceleration and the ball rolls through (facts 12, 13).
- **Negative/control.** Unpowered, the ball stays upstream (fact 13). A Delay link is rejected (fact 12).
- **Boundaries.** Loaded or blocked closing stops at the ball without crushing it (fact 14); supply interruption mid-stroke; clearing and retry; rotated passage (fact 13); horizontal mounting (fact 11).
- **Run/Reset.** Reset closes the blade exactly and keeps connections (fact 12).
- **Save/Load.** A round trip preserves pose, mouths and connection; a reloaded construction behaves the same in the tested cases.
- **Integrations.** Hold timer, counter and pressure plate supplies; cannon muzzle and wound-spring plunger obstruction (facts 22–25), each after its partner element's story; facts 23 to 25 are owned by the Counter, Pressure plate, Cannon and Wound spring specs.
- **Status of the Blocked state.** Proven natively: the closing blade stops on a ball partly through the opening without ejecting it, reads Blocked on every tick after rest and shows the ochre cue; free travel, the spring return, a mid-stroke cut, both stops and a ball pressing on the closed blade's face read not Blocked (`LatchSupplyFixtureTests`, `DeclaredPrismaticDriveTests`, `DeclaredPresentationTests`). Browser case `cat-051-interrupt` is pending post-catalogue E2E.

**Native tests that exist (22 November 2026), in `CuriousContraptions.tests/`.** Each criterion of [CAT-051](../requirements.md#current-cat-051) against its test:

| Criterion | Test (file line) |
| --- | --- |
| Electrical-only supply retracts the blade within its bounds; no activation input | `DeclaredPrismaticDriveTests.ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep` (92); `PoweredGateTests.Connections_AcceptsElectricalAndRejectsActivation` (131) |
| Unpowered | `LatchSupplyFixtureTests.AuthoredDisabledBatteryNeverOpensAfterSet` (236), `BallUpstreamAtCutStaysUpstream` (273) |
| Loaded or blocked closing; no teleport through an obstruction | `LatchSupplyFixtureTests.BallPartlyThroughStopsClosingBladeUncrushed` (289); `DeclaredPrismaticDriveTests.BlockedNeedsContactInteriorAndRestTogether` (387) |
| Supply interruption | `LatchSupplyFixtureTests.MidStrokeResetStopsDriveAndReturnsClosed` (188), `SupplyOffOnKeepsMemoryAndRepowersWithoutNewSet` (250) |
| Rotated passage | `DeclaredPrismaticDriveTests.ABallRestingAgainstTheClosedTiltedBladePassesDownstreamWhenEnabledDuringRun` (403). `AnUnpoweredInvertedOrTiltedMountRestingInItsInteriorWithNothingInTheWayIsNotBlocked` (`:335`–`:351`) pins today's 0.70 m hang and is replaced in the rest-coordinate slice |
| Matching tube mouths | `TubeMouthSnapTests.APoweredGateSnapsOntoAPipeMouth` (203) |
| Rails, housing and blade collide as drawn | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (186) |
| Reset, save and load | `LatchSupplyFixtureTests.RunResetRestoresLatchOffBladeClosedAuthoredBattery` (330), `SaveLoadPreservesFiveSocketsAndReplaysIdentically` (351) |
| Supplied by a Counter and by a Pressure plate | `CatalogueIntegrationTests.AClockCountsTwoPulsesThenTheSuppliedCounterOpensItsGate` (69), `AWeightLoadsItsPlateAndOnlyTheWiredSupplyOpensTheGate` (139) |

**Not yet covered (residual acceptance, owned in section 8).**
- *Closes in every orientation (owner decision, 22 November 2026).* Given an unpowered gate mounted upright, on its side (rolled 90°, travel horizontal) and inverted (rolled 180°), when Run for 6 s, then the blade rests at its closed stop (opening below 5 mm) in all three; and given each mount supplied, then the blade reaches the open stop (within 2 cm of 1.55 m) inside a 4 s stall guard; control: the inverted mount with the rest coordinate left at zero hangs open (the delivered behaviour); boundary: a 135° mount. Planned `DeclaredPrismaticDriveTests.AnUnpoweredBladeClosesUprightOnItsSideAndInverted` (theory) and `ASuppliedBladeOpensFullyInEveryOrientation`, replacing the test at `:335`–`:351`.
- *Clearing and retry.* Given a blade resting Blocked on a ball partly through the opening, when supply returns, the blade lifts and the ball rolls clear, and supply is cut again, then the blade reaches its closed stop (opening below 5 mm, far smaller than any ball that could pass) within a 5 s stall guard and reads not Blocked; control: with supply never restored the blade stays Blocked on the ball for the same 5 s. Planned `LatchSupplyFixtureTests.ABlockedBladeClosesFullyOnceTheBallHasCleared`.
- *Beam occlusion.* Given a beam aimed through the bore, when the blade is closed, then the receiver reads none of the beam's channels, and with the blade fully open it reads them; control: a beam aimed beside the frame is unaffected by the blade, and with the blade open a ball resting in the bore blocks the beam while the clear sleeve alone does not; boundary: a blade stopped part-way blocks when its edge crosses the beam axis. Tolerances are the optical set (coordinator decision 21, 22 November 2026): ±2 % power, a 1 s guard, 0.5° direction and ±0.02 m end points. Planned in the first optical slice that places a gate, after `ENGINE-OPTICAL`.
- *Hold timer supply.* `HoldTimerTests.cs` references the gate; the test name was not confirmed in this pass.

## 6. Open questions

**All six are resolved; none needs the owner.**

| # | Resolution | Authority |
| --- | --- | --- |
| 1 | The return spring is the fail-closed law (owner, 18 October 2026), and it closes the blade in every mounting orientation with no orientation limit (owner decision, 22 November 2026). The spring declares a rest coordinate below the closed stop, so the closed blade is preloaded: a rest coordinate outside travel, which `SliderTravel` admits, with the starting offset a separate value (`ENGINE-JOINT-KINDS`). Worked value: rest 0.85 m below closed at 14 N/m gives 14 × 0.85 = 11.9 N of closing preload, above the blade's 9.81 N weight, so an inverted blade stays closed with 2.1 N to spare. Fully open and upright the drive must hold 14 × (1.55 + 0.85) + 9.81 = 33.6 + 9.81 = 43.4 N, inside its 60 N; on its side 33.6 N; inverted 33.6 − 9.81 = 23.8 N. Today the rest is the coordinate zero, so an inverted unpowered blade hangs open by 9.81 ÷ 14 = 0.70 m (0.50 m at 135°), which the delivered test at `DeclaredPrismaticDriveTests.cs:335`–`:351` pins | Owner decisions of 18 October and 22 November 2026; required change in section 8 |
| 3 | Blocked means something is in the way: a positive normal contact impulse from a body other than the frame during the tick, an interior coordinate more than the envelope's 0.5 mm resting contact slop from both stops, and an axial speed below the envelope's 0.01 m/s at-rest speed | Coordinator under the standing delegation, 22 October 2026 |
| 4 | Blocked stays a single state; no requirement asks for blocked-opening and blocked-closing | Coordinator under the standing delegation, 22 October 2026 |
| 2 | The gate's bore is the pipe bore, 0.65 m: its sleeve and collar Tubes and both mouths use it, and a pipe snaps onto either mouth | Delivered; `TubeMouthSnapTests.APoweredGateSnapsOntoAPipeMouth` |
| 5 | Supply is on/off now. Drain is one demand law for every powered part, the engine story `ENGINE-SUPPLY-DEMAND` (owner decision 3a, 22 November 2026) | Owner, 3 November 2026 ([deferred work](../../../_bmad-output/implementation-artifacts/deferred-work.md)) |
| 6 | The requirement row governs: the gate is electrical only. An activation pulse opens it only through a supplied part in its route (an Impact switch or State latch closing the supply), which is what the story's lesson needs | D15; `PoweredGateTests.Connections_AcceptsElectricalAndRejectsActivation`, `LatchSupplyFixtureTests.SetClosesRouteOneTickAfterStrikeWithoutTopologyChange` |

The questions were: 1 whether the spring is the fail-closed law; 2 the bore; 3 the Blocked thresholds; 4 one or two Blocked states; 5 supply drain; 6 activation against electrical opening.

## 7. Integration

- **Supply collaborators (all delivered; each `Supply` is an Electrical output into the gate's Electrical `PowerIn`).** Battery, Impact switch, State latch, Hold timer, Counter, Pressure plate and the electrical gates.
- **Passage collaborators (delivered).** Pipe and both bends at either mouth (bore radius 0.65 m); every ball. A Basketball (radius 0.34 m) and a Bowling ball pass the open bore; the 1.44 m blade covers the bore, 1.3 m across, when closed.
- **Later partners.** Counter, Pressure plate, Cannon muzzle and Wound spring plunger rows (facts 23 to 25) are owned by those parts' specs; optical parts (Epic 13) for beam occlusion.
- **Campaign.** No level in `content/puzzles.json` places or stocks the Powered gate. It is taught by a level still to be authored; nothing blocks that authoring.
- **Coverage binding.** Catalogue key `powered_gate` (CAT-051) in `docs/coverage/catalogue-elements.json`. No named identity maps to it (EL-184 and EL-163 are separate elements).
- **Lesson fixtures.** Not needed for the part.

## 8. Delivery and residuals

Delivered as recorded in section 1. Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes further element work.

| Residual | Owner | Tier | Prerequisite |
| --- | --- | --- | --- |
| Beam occlusion by the committed blade and frame; the clear sleeve passes light | First optical element slice that places a gate; the sleeve's Transmit (1, 1, 1) is a criterion of `ENGINE-OPTICAL` ([EL-176](EL-176-red-laser-emitter.md) section 10, item 6) | Data-only if `ENGINE-OPTICAL` publishes occlusion by any collider with a test; otherwise engine | the engine programme, then `ENGINE-OPTICAL` |
| Declared spring rest coordinate, then the gate declares it (rest 0.85 m below closed), with the three-orientation rows and the corrected inverted-mount test | `ENGINE-JOINT-KINDS` (owner decision 6; coordinator decision 21): "A prismatic slider declares its spring rest coordinate, which may lie outside its travel (a preload); `SliderTravel` admits it; the starting offset stays a separate value. An unpowered spring-closed slider closes upright, on its side and inverted." The Powered gate and the Beam shutter are its consumers. Then a data-only gate package slice | Engine, then data-only | the engine programme, then `ENGINE-JOINT-KINDS` |
| Supply drain (120 W drive; the unread node fields are consumed or deleted) | `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`), which re-qualifies the gate's supply cases | Engine | the engine programme, of which it is a story |
| Power bound enforced by whole-world snapshot and re-solve; no island sleeping | `ENGINE-ISLANDS-SLEEP`: its contract confines drive-budget trials to the island they affect, so the cost becomes proportional to the gate's island, not removed; and it adds island sleeping, which is not implemented today | Engine | the engine programme, of which it is a story |
| Clearing-and-retry test | The next slice that touches the package or its fixture; a missing test is a patch | Data-only | the engine programme |
| Stale summary comment in the declaration (`PoweredGateElementDeclaration.cs:10`–`:13`) | The same slice | Data-only | the engine programme |
| Browser qualification (`cat-051-interrupt` and the recipe in section 5) | Post-catalogue E2E phase | n/a | n/a |

**Later row owned here (coordinator decisions 36 and 41).** [EL-216](EL-216-capture-cradle.md) row A5 (`CradleTests.ASeparatelyPlacedGateReleasesTheCargo`): with `outlet_width` 0.62 and an unpowered Powered gate placed in the cradle's doorway, a Bowling ball lowered into the cup rolls through the doorway, rests against the closed blade for 5 s, and passes the blade when the gate is supplied; with no gate placed the ball rolls straight out. It is a cross-part row owned by this slice, because the gate is taught at 24, after the cradle at 8a; EL-216 keeps its Given, When, Then, control and variation.

No named identity is closed by this part. **What a residual slice's capability probe must re-check:** for occlusion, that the optical engine tests a ray against Tube, box and sphere colliders of a moving body at its committed pose; for the rest coordinate, that preload through a declared rest does not need the limit refusal at `MechanicalValueDomain.cs:81` relaxed for any other package, and that the Beam shutter can declare the same field; for drain, that prismatic and revolute drives share one demand law and that an exhausted source lets the spring close the blade (the fail-closed law of question 1).

## 9. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms with fixes.** Fixes in the package: the stale class comment and the missing spring rest coordinate (points 1 and 2). No engine code names this element (searched `engine/`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle` and the abstractions).

1. **Declaration data only.** Two bodies, scene colliders, one sprung slider with a supplied drive, one port, one electrical node, a one-consumer supply graph, three presentation bindings. **Fix:** the class summary comment at `elements/PoweredGate/PoweredGateElementDeclaration.cs:10`–`:13` still describes the deleted box-ring sleeve. The electrical node's numbers are validated as finite and read by no law (only `Role` is read): unread dead data, to be consumed by `ENGINE-SUPPLY-DEMAND` or removed forward-only; its 20 W against the 120 W drive has no behavioural effect.
2. **Generic laws.** Opening: the supplied prismatic drive law with speed, acceleration, effort and power bounds. Closing: the slider's 14 N/m spring and 4 N·s/m damper plus gravity. Blocked closing: contact between the blade and a ball. Passing a ball: contact with tube colliders at bore radius 0.65 m. **Required change (owner decision, 22 November 2026):** the blade must close unpowered in every orientation; today an inverted blade hangs 0.70 m open. The repair is general data, a declared spring rest coordinate (`ENGINE-JOINT-KINDS`, spring rest position), not gate logic.
3. **Three layers.** Colliders: three tubes, three boxes and two spheres on the static frame; the blade box on its body. The sleeve Tube declares the optical response Transmit (1, 1, 1) under `ENGINE-OPTICAL`; every other collider takes the default Absorb ([EL-176](EL-176-red-laser-emitter.md) section 10, item 6). Visuals: the matching meshes. Animation: indicator albedo from `CommittedActuatorState`. The blade art follows its body.
4. **One-way flow and typing.** Physics → committed actuator state → presentation. Typed `PortId`, `CoordinateKind`, `SupplyTerminal`; f32 values.
5. **Flat-state counts** (from `powered_gate.tscn` and the declaration). 2 bodies (1 dynamic); 9 colliders (3 tubes, 4 boxes, 2 spheres); 1 prismatic joint; 1 drive; 0 sensors, stores, events; 1 electrical node; 1 supply consumer; 1 anchor point; 1 port; 0 parameters; 3 presentation bindings and 1 body-pose binding.
6. **Performance fitness.** Nine colliders, the most among the slider parts, because the sleeve and collars are true tubes (the hollow-shapes slice replaced a sixteen-box ring with them). Static frame. Two engine gaps, both owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026). First, for this and every bounded prismatic drive, the power bound is enforced by a whole-world snapshot and up to eleven re-solves of the substep (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Motors.cs:35`–`:66`), scene-proportional work; that story confines drive-budget trials to the island they affect, so the cost becomes island-proportional. The revolute drive clamps in its own row. Second, island sleeping is not implemented; a resting body is still stepped, at the cost of its broadphase proxies and rows every substep (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP`.
7. **Scenario tests.** `PoweredGateTests`, `DeclaredPrismaticDriveTests` and `CatalogueIntegrationTests` run the production simulation; the generic drive test also uses a synthetic declaration.
