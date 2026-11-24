# CAT-052 · pressure_plate — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-052 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch B declaration spec (CAT-052-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-052](../requirements.md#current-cat-052). Supplied-network facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

**Colliders (owner, 25 October 2026, "visible = solid"):** since [`elements-visible-solid-sweep`](../../../TODO.md) the three sockets collide as spheres r 0.075 at (∓1.18, −0.08, 0) and (0, −0.08, 1.18), appended after the base and the sensing top (collider 1, which the contact load names) (`elements/PressurePlate/pressure_plate.tscn`; `VisibleSolidColliderTests`). They take the plate's one material, restitution 0.1, which a ball dropped onto the PowerIn socket shows (product restitution 0.055). Placement bounds every collider (`PlacementPose`), so the sockets now count toward the bench edge: the plate reaches x ±1.255 and z +1.255 (one-sided: −z still ends at the base's −1.1) against the base's ±1.1 (`PlacementPoseTests.ProudSocketsCountTowardTheBenchEdge`). A ball on the sensing top still opens the plate's gate (`PressurePlateTests.TheSocketCollidersLeaveTheSensingTopAndItsActivationUnchanged`).

**Studs and ring.** Since `engine-multi-collider-dynamic` (published `28a2d302`) the four studs and the ochre ring collide through the hidden proxies `col_stud_0`–`3` and `col_visual_ring` (`elements/PressurePlate/pressure_plate.tscn:119–142`). The ring is a torus, so a centred ball still reaches the sensing face through its bore, and it is a supporting collider of the contact-load sensor, so a flat load resting on it counts.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-052 · `pressure_plate` |
| Requirement anchor | [current-cat-052](../requirements.md#current-cat-052); retained behaviour [todo-265](../requirements.md#todo-265) |
| Mapped identities | none owned. Related but distinct: [EL-059 Weight tray](../invest/named-elements.md#element-059) ("loaded support exposes measured force or threshold state"); no legacy implementation links them, so its requirement row stands as the source. |
| Roadmap story | 9.1 "Pressure Plate Surface Contact Load Sensor" (Epic 9) Current [CAT-052 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | **Delivered** (22 November 2026 check): package `elements/PressurePlate`, id `pressure_plate`; Story 9.1, repaired by 9R.4 and 9R.5, generic instance by C-Plate (native Pass `146588`). Sprint key `9-1-pressure-plate-surface-contact-load-sensor` stays in-progress only for its Chrome clause. Residuals: [section 7](#7-delivered-state-integration-and-residuals-22-november-2026). Every residual slice (the single-impact and ring-support tests, the refusal wording and ratings refactors) is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gap, contact-load rows evaluated against every contact and only on a static owner, is owned by `ENGINE-CONTACT-OBSERVATIONS`. |

## 2. Declaration

The delivered declaration is `elements/PressurePlate/PressurePlateElementDeclaration.cs` with the scene `elements/PressurePlate/pressure_plate.tscn` (read on `8d1d68aa`). It implements `IMechanicalElement`, `ISupplyElement`, `ISignalElement` and `ISceneGeometryElement` (`:10`). The legacy sources of the carried values are in section 4.

**Body and material.** One static body (`KinematicMode.Static`, `:64`), no mass, no joint, no payload (`:66–74`). Material: restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:57`), all f32. The top is fixed; nothing depresses.

**Colliders (physics graph).** Ten, baked in scene order from hidden proxies (`pressure_plate.tscn:101–150`). Physics reads no art.

| # | Shape | Size (m) | Centre (m) |
|---|---|---|---|
| 0 | box | 2.2 × 0.2 × 2.2 | (0, −0.12, 0) |
| 1 | box | 1.8 × 0.1 × 1.8 | (0, 0.03, 0); its top, y 0.08, is the sensing face |
| 2 | sphere | r 0.075 | (−1.18, −0.08, 0), the `PowerIn` socket |
| 3–6 | sphere | r 0.055 | (±1, 0.015, ±1), the studs |
| 7 | torus, axis Y | radii 0.282 to 0.318 (tube 0.018) | (0, 0.084, 0), the ring |
| 8–9 | sphere | r 0.075 | (1.18, −0.08, 0) and (0, −0.08, 1.18), the `Supply` and `ActivationOut` sockets |

Placement bounds every collider, so the plate reaches x ±1.255 and z +1.255 at the bench edge.

**Visuals (rendering graph).** No art mesh is used as a collider.

| Art node | Mesh and colour | Solidity | Moved by |
|---|---|---|---|
| `Visual/Visual_Base` | box 2.2 × 0.2 × 2.2 m, navy `#293954` | solid, collider 0 | nothing |
| `Visual/Visual_Top` | box 1.8 × 0.1 × 1.8 m, cream `#fff8e9` | solid, collider 1 | nothing |
| `Visual/Stud_0`–`3` | spheres r 0.055 m, slate, ochre or gold by load phase | solid, colliders 3–6 | nothing (colour bindings only) |
| `Visual/Visual_Ring` | torus, radii 0.282 to 0.318 m, ochre `#e8b764` | hollow, collider 7 | nothing |
| `Visual/Visual_Socket_PowerIn`, `_Supply`, `_ActivationOut` | spheres r 0.075 m, ochre | solid, colliders 2, 8, 9 | nothing |

Selection radius 1.5 (`:12`). Icon `elements/PressurePlate/icon.svg`. At a glance it is the low square slab with a cream top, four corner studs and a centre ring; no other delivered part is a flat square pad.

**Animation (animation graph).** Twelve presentation bindings (`:14–25`): the red, green and blue albedo of each stud take a state value from `CommittedContactLoadPhase` of node `signal`: slate (85, 101, 115)/255 when Empty, ochre (232, 183, 100)/255 when Underweight, gold (247, 203, 82)/255 when Loaded. The change is immediate (Linear, duration 0). They read the committed phase and write nothing back.

**Ports** (`:47–49`), in declared order. There is no activation input.

| `PortId` | Domain | Direction | Role | Signal kind | Position (m) | Facing |
|---|---|---|---|---|---|---|
| `PowerIn` | Electrical | Input | PowerIn | — | (−1.18, −0.08, 0) | −X |
| `ActivationOut` | Signal | Output | ActivationOut | Level | (0, −0.08, 1.18) | +Z |
| `Supply` | Electrical | Output | Supply | — | (1.18, −0.08, 0) | +X |

**Parameter** (`:39`). It is captured at Run; the editor refuses an out-of-range value and never clamps.

| Name | Kind | Unit | Minimum | Maximum | Default | Editor policy | Run edit |
|---|---|---|---|---|---|---|---|
| `minimum_mass` | Real | kilogram | 0.1 | 16 | 0.5 | Reject (default) | Build-only |

**Sensor node, supply node and event.** One signal node `signal`, a `SignalContactLoadDescriptor` (`:93`), with `ActivationOut` on its output (`:95–98`):

| Field | Value |
|---|---|
| Frame | package body 0 |
| Region collider | 1 (the top box); its x–z footprint bounds the sensor |
| Local normal and minimum alignment | +Y; 1 − 10⁻⁶ |
| Contact distance and depth margin | 10⁻⁴ m; 10⁻⁶ m |
| Minimum mass | the `minimum_mass` parameter, compared inclusively |
| Release hold | 0.1 s, 12 ticks at 120 Hz |
| Supporting colliders | 1 and 7 (the top and the ring) |

One supply node `contact`, a `SupplyContactDescriptor` closed while `signal` is `Latched`, between `PowerIn` and `Supply` (`:78–83`). Event `ContactLoad` (`:60`). `ElectricalNodes` holds one `Switch` row (`:85–88`); its ratings are unread (section 7).

**Compiled counts per instance.** 1 static body; 10 colliders (2 boxes, 7 spheres, 1 torus); 0 joints; 0 drives; 1 contact-load sensor row; 1 signal node with one committed memory row; 1 supply node; 0 stores; 3 ports; 12 bindings. All are fixed at compile.

**Catalogue.** Id `pressure_plate`, title "Pressure plate", category Control, palette order 21, colour (0.91, 0.72, 0.39), description "Senses resting mass on its top plate. Closes an internal electrical circuit while its threshold mass is met." (`:29–36`). Coverage key `pressure_plate`.

## 3. Engine capabilities and conformance

Map families, for navigation only (D14): ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction.

**Generic laws.** The package holds no script, callback, update loop or state machine. No rule reads "when a ball touches the plate"; each row is a law any package could declare.

| Behaviour | General law | Declared values |
|---|---|---|
| A body rests on it with little bounce | rigid contact with the product pair law for restitution, and friction | ten colliders; restitution 0.1 |
| It senses resting mass | contact-load sensor: each tick it sums the mass of each distinct dynamic body whose committed contact on a supporting collider lies in the region footprint, aligns with the normal and is within the contact distance or carried normal impulse; the phase is Empty, Underweight or Loaded by an inclusive comparison | the sensor fields above |
| Its level comes on at once and releases after a hold | signal network level-source rule: high on the first Loaded tick; with n ticks since the last Loaded tick it stays high while n ≤ 11 and is low from n = 12 | release hold 0.1 s |
| Its output is a level only a level indicator accepts | signal network level rule (`LevelDrivesLevelInputOnly`) | `PortSignalKind.Level` |
| The contact is closed while the level is high | supply network contact rule | `SignalContactState.Latched` |
| Studs | presentation bindings on the committed load phase | the twelve bindings |

**Capability probe.** 7 present, 0 untested, 0 narrower, 0 missing.

| Capability | Status | Symbol | Test |
|---|---|---|---|
| Contact-load sensor: each directly touching body once, inclusive threshold | present | `SignalContactLoadDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:58–60`) | `PressurePlateTests.ContactLoadSensorTracker_CountsEachBodyOnceAndSumsBodies`; `PressurePlateTests.OneDominoIsUnderweightAndTwoDominoesSum`; `PressurePlateTests.OnlyDirectContactCountsForAStack` |
| Only the top face counts; rotated mounting | present | the same descriptor | `PressurePlateTests.ContactLoadSensorTracker_CountsOnlyRestingContactsOnTheFace`; `PressurePlateTests.ContactLoadSensorTracker_TransformsARotatedFrame` |
| Per-instance threshold and its boundary | present | `ParameterDescriptor` range | `PressurePlateTests.TheInstanceMinimumMassDecidesItsOwnGate`; `PressurePlateTests.AMinimumOneF32AboveTheBodyMassStaysUnderweight`; `DeclaredContactLoadTests.AMissingOrOutOfRangeMinimumMassIsRefusedAtBothBoundaries` |
| Held level: immediate on, 0.1 s release | present | release hold on the node | `PressurePlateTests.TheLevelIsHighOnTheFirstQualifyingTick`; `PressurePlateTests.ABounceGapOfElevenStaysLitAndTwelveShowsOneDarkTick`; `DeclaredContactLoadTests.TheHoldIsATenthOfASecondAtEachCadence` |
| Supplied contact and level output | present | `SupplyContactDescriptor`; `PortSignalKind.Level` | `PressurePlateTests.ALoadedPlateWithoutSupplyLeavesItsGateClosed`; `PressurePlateTests.APlateActivationOutLightsOnlyASignalLamp`; `PressurePlateTests.TheLampAndTheSuppliedContactShareOneHeldLevelOnEveryTick` |
| Run, Reset and save | present | committed checkpoint | `PressurePlateTests.ABenchOfManyActivationPartsRunsResetsAndSavesWithNoActivationLimit`; `PressurePlateTests.TheReleaseHoldIsCommittedCheckpointState`; `DeclaredContactLoadTests.AdmittedMinimumMassesRoundTripAtBothBoundaries` |
| Stud colour by load phase | present | `PresentationDriver.CommittedContactLoadPhase` | `ComposableAnimationMappingTests` (the Underweight colour assertion at `:586–600`) |

**Conformance (owner addendum, 22 November 2026): conforms with fixes.**
- Declaration data only: holds. A search of `engine`, `ui` and every non-test `CuriousContraptions.*` project for the package id, class and namespace found no reference (`docs/planning/elements/readiness/audits/elements-batch-b.md` §5). The sensor is compiled from the descriptor; no code names the plate.
- Naming residue, a required forward-only refactor (section 7): the generic level rule's refusal message names a product, "A level output can only light a Signal lamp." (`engine/gpu/WorkshopConnections.cs:150`); `ENGINE-CAPABILITY-INSTANCES` rewords it to "A level output connects only to an input that admits a level." (audit E4; the one wording, as CAT-035 states), with the pinned constant `PressurePlateTests.cs:698`.
- Three layers: hold. Every static proud solid has its own collider, the ring a true hollow one; the studs' bindings change colour only; nothing is moved by a binding.
- One-way flow, typing and flat state: hold, with the counts in section 2. The sensor reads committed contacts; presentation reads the committed phase.
- Tests are scenarios, with one gap: no test drives a single impact to a consumer (section 7).
- The unread `ElectricalNodes` ratings are a required forward-only refactor (section 7).

**Performance fitness.** The body is static and is never integrated. Ten colliders against the two of the original slab has one reason: visible = solid. Each stud and socket is a drawn sphere standing clear of the slab, so each has its own primitive, and the ring stands 2.2 cm above the sensing face; it is a torus, not a disc, because a disc would hold a centred ball off the face. No collider is cosmetic. The sensor's region is bounded: it counts only the committed contacts of its two supporting colliders inside the region footprint. Engine gap, not an element fault: the engine finds those contacts by scanning every contact for every contact-load row (work proportional to loads × contacts, `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:220`–`237`), and only a static owner is admitted; owned by `ENGINE-CONTACT-OBSERVATIONS` (rows found by compiled handle, on any owner). The cost of the colliders for many instances is the recorded, unmeasured engine gap "Per-instance collider growth from visible = solid" (`_bmad-output/implementation-artifacts/deferred-work.md:177–179`), owned by the performance qualification gate P0-034. The authorities state no per-part budget, so none is claimed here.

**Dependencies.** Dynamic loads (Basketball, Bowling ball, Domino, Tennis ball, Weight); Battery and a supplied consumer (Powered gate); a Signal lamp for the level. All are delivered.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| P1 | Continuous electrical contact; sums masses directly resting on the top face — not impacts, stacked load transmission or a generated supply. | `parts/PressurePlatePart.cs@a6c914e:L8-L9` | carry forward |
| P2 | Top 0.08 m, half size 0.9 m; readable supported mass and phase (Empty before Run). | `parts/PressurePlatePart.cs@a6c914e:L14-L20` | carry forward |
| P3 | Sensor region, normal, alignment and minimum mass as in section 2. | `parts/PressurePlatePart.cs@a6c914e:L21-L25` | carry forward |
| P4 | Plate material restitution 0.1, bounce threshold 0.1, friction 0.3. | `parts/PressurePlatePart.cs@a6c914e:L28-L28` | carry forward |
| P5 | Ports and contact route as in section 2. | `parts/PressurePlatePart.cs@a6c914e:L29-L35` | carry forward |
| P6 | Minimum mass must be finite and within 0.1–16. | `parts/PressurePlatePart.cs@a6c914e:L36-L41` | carry forward |
| P7 | Geometry and art as in section 2. | `parts/PressurePlatePart.cs@a6c914e:L42-L54` | carry forward |
| P8 | Active exactly while Loaded; studs eased per frame toward gold (rate 8/s, SmoothStep) from ochre (Underweight) or slate. | `parts/PressurePlatePart.cs@a6c914e:L55-L62` | carry forward the colours and timing; do not carry forward the per-part `PreparePhysics`/`BeforeNetworks` update loop or the hand-written material writes (declared cosmetic binding instead) |
| P9 | Measurement: a contact counts only if the other body is dynamic, the contact normal aligns with the frame normal to at least the minimum alignment, and the contact point (frame-local) lies inside the region; each body counts once (by identity); mass = sum of body masses; Empty if 0, Underweight if below the minimum, Loaded otherwise (inclusive). | `engine/physics/PhysicsContactLoadSensor.cs@a6c914e:L30-L49` | carry forward the rule; do not carry forward the CPU contact-gap source (the shared solver's committed contacts feed it) |
| P10 | Declaration validation: non-empty finite region, finite non-zero normal (normalised), alignment in (0, 1], minimum mass > 0; one sensor per frame body, installation atomic. | `engine/physics/PhysicsContactLoadSensor.cs@a6c914e:L18-L29` and `engine/physics/PhysicsWorld.cs@a6c914e:L1033-L1044` | carry forward |
| P11 | Supply/load matrix after 240 ticks, with a Powered gate (`powered_gate`, CAT-051) as the supplied load (`CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L15-L15`): one ball → Loaded (with and without supply; the Powered gate is powered and opens only with supply); one tennis ball → Underweight; two tennis balls → Loaded; one weight → Loaded; reported mass = sum of resting bodies; disabling the loads' collision → Empty on the next tick and the gate closes; Reset → mass 0, Empty, inactive, wires kept. | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L38-L82` | carry forward |
| P12 | Rotated plate (20°, 30°, 40°) measures only its own top face: ball centre at local (0, 0.42, 0) presses; (0, 0.5, 0) (a gap), (1.2, 0.42, 0) (outside) and (0, −0.66, 0) (underside) do not. | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L83-L107` | carry forward |
| P13 | A delay cannot connect to the plate (no activation input, so a trigger cannot replace supply); minimum mass 3 round-trips through save/load. | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L108-L123` | carry forward |
| P14 | Thresholds 0, −1, 17, NaN and +∞ are rejected and the part is not added. | `CuriousContraptions.tests/PressurePlateTests.cs@a6c914e:L124-L140` | carry forward |
| P15 | A compound load (weight) or ball resting on the plate, level or tilted 37°, counts its mass once with exactly one body id; reading does not mutate contacts; moving or hiding scene nodes does not change the reading; changing `minimum_mass` during Run is rejected and the phase is unchanged; an upward impulse equal to the body's momentum per unit speed (1 m/s) lifts it and the reading becomes Empty after one step. | `CuriousContraptions.tests/PressureContactOwnershipTests.cs@a6c914e:L19-L71` | carry forward |
| P16 | Sphere, box and two-sphere compound loads of mass 2 count once (Loaded at threshold 1, Underweight at 3); side, below, gap, outside-region and static loads do not count. | `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs@a6c914e:L50-L92` | carry forward |
| P17 | A disabled frame collider or an opposite sensor normal reads 0. | `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs@a6c914e:L94-L103` | carry forward |
| P18 | Two bodies of 2 kg and 3 kg sum to 5 kg and are Loaded at minimum 5 (inclusive threshold); body ids reported in identity order. | `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs@a6c914e:L105-L119` | carry forward |
| P19 | Duplicate frame, unknown body and null declarations reject atomically; querying an undeclared frame rejects. | `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs@a6c914e:L121-L146` | carry forward |
| P20 | A failed physics step restores the reading. | `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs@a6c914e:L148-L160` | do not carry forward (CPU transaction rollback) |
| P21 | Physics-side query and declaration record. | `engine/physics/PhysicsWorld.cs@a6c914e:L118-L125` and `engine/SceneContactLoadSensorDeclaration.cs@a6c914e:L5-L6` | carry forward the declaration fields; not the CPU world |
| P22 | Parameter enum `MinimumMass` (wire name `minimum_mass`). | `engine/MachineData.cs@a6c914e:L86-L86` | carry forward at the serialization boundary |
| P23 | The contact reads the committed load phase (`Loaded`). | `engine/SceneElectricalContact.cs@a6c914e:L122-L124` and `engine/SceneElectricalContact.cs@a6c914e:L139-L139` | carry forward |
| P24 | Catalogue entry and scene. | `parts/catalog/pressure_plate.tres@a6c914e:L1-L14` and `parts/scenes/pressure_plate.tscn@a6c914e:L1-L6` | carry forward the data |

`reference/P0-022-before/docs/coverage/engine/*.json` list `pressure_plate` only as a catalogue id; `tools/P0-007-actual-path-probe/*` only compiles the legacy sources: no additional element knowledge. `engine/physics/CompliantContactLoad.cs`, `CompliantContactState.cs` and `PhysicsLoadSet.cs` mention contact load but implement compliant surfaces (trampoline/springboard), not this sensor.

### Files harvested

- `parts/PressurePlatePart.cs`
- `parts/catalog/pressure_plate.tres`
- `parts/scenes/pressure_plate.tscn`
- `parts/catalog/tennis.tres` (mass value only)
- `parts/catalog/weight.tres` (mass value only)
- `engine/physics/PhysicsContactLoadSensor.cs`
- `engine/physics/PhysicsWorld.cs`
- `engine/physics/ConvexSweep.cs`
- `engine/physics/PersistentContactPair.cs`
- `engine/SceneContactLoadSensorDeclaration.cs`
- `engine/SceneElectricalContact.cs`
- `engine/MachineData.cs`
- `CuriousContraptions.tests/PressurePlateTests.cs`
- `CuriousContraptions.tests/PressureContactOwnershipTests.cs`
- `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs`

## 5. Acceptance outline

Authority: [CAT-052](../requirements.md#current-cat-052), [todo-265](../requirements.md#todo-265); Story 9.1 adds `tools/e2e/cat-052.test.ts`.

- **Construction (Chrome UI).** Place Pressure plate, Battery and the consumer; Connect battery→plate (`Supply`→`PowerIn`) and plate→consumer; drop or place a Basketball/Bowling ball above the top face with the ordinary placement tools.
- **Positive.** A body of at least the threshold mass resting directly on the top face closes the contact; studs ease to gold.
- **Negative / controls.** Rolling off or lifting opens it, and darkens a lamp on `ActivationOut`, on the 12th tick after the last Loaded tick (the 0.1 s release hold; boundaries 11 / 12 / 13; P11, P15); a bounce whose contacts are 11 or fewer ticks apart keeps it steadily closed and lit; a single impact closes the contact for no longer than the 0.1 s release hold (owner decision 8; planned `PressurePlateTests.ASingleImpactClosesTheContactForItsHoldOnly`); the `ActivationOut` level is refused by Counter, Delay and latch inputs; a below-threshold body leaves the studs ochre and the contact open (P11); contact on the side or underside, or a body beside the plate, does not count (P12, P16); no supply → studs gold but consumer off (P11).
- **Boundaries.** Exactly-at-threshold mass is Loaded (P18); two light bodies together reach the threshold (P11); a compound or stacked load counts each directly touching body once (P15); thresholds 0.1 and 16 accepted, out-of-range rejected (P14); rotated mounting (P12).
- **Run/Reset.** Reset gives mass 0, Empty, slate studs, inactive (P11).
- **Save/Load.** `minimum_mass` round-trips (P13).
- **Integrations.** Battery-fed contact to the consumer; bouncing impact control (open question 3).

## 6. Open questions

1. **Threshold.** The Story 9.1 AC says activation for "mass > 0.2 kg"; the requirement row and the legacy say default 0.5 kg with an inclusive threshold (mass ≥ minimum). A Domino (0.4 kg) presses at 0.2 but not at 0.5. Which default and which comparison was the question. **Decided (owner decision h; delivered by C-Plate): `minimum_mass` is f32 kg, range 0.1–16 inclusive, default 0.5, compared inclusively (mass ≥ minimum); it supersedes 9.1's "> 0.2 kg". The editor refuses out-of-range values (no clamp).**
2. **Output to a Signal lamp.** The Story 9.1 AC wires the plate "to a Signal lamp"; the legacy and the requirement give the plate only a supplied electrical contact, the lamp is activation-only (CAT-035), and the supply network (Story 8.1) now precedes Story 9.1. Whether 9.1 proves the plate through the Story 8.1 supplied contact, or the plate gains an activation output the requirement does not define was the question. **Decided 18 Oct 2026 (coordinator under owner delegation; delivered by Story 9R.5): the plate gains an `ActivationOut` level output, high while Loaded (with the Q3 release hold) and low otherwise; like the latch's output it drives Signal lamps only.** Rationale: the player sees at a glance whether the plate is pressed, and 9.1's AC becomes achievable.
3. **Bouncing impacts.** The Story 9.1 AC and the requirement require that impact-only contact does not give sustained activation. The legacy counted any qualifying contact present at the tick, with no rest or dwell qualifier, so a bounce could close the contact for the ticks it touches. Whether a dwell or rest-speed qualifier is required, and its value was the question. **Decided 18 Oct 2026 (coordinator override; delivered by Story 9R.5) and confirmed by owner decision 8 (22 November 2026): no dwell; activation is immediate on the first qualifying tick, and release is held for 0.1 s (12 ticks at 120 Hz) after the last qualifying tick.** One held level governs both the lamp and the supplied contact. A ball bouncing on the plate therefore keeps the lamp lit and the contact closed through the bounce and releases 0.1 s after it leaves. Rationale: without the hold, the lamp flickers and downstream logic chatters on every bounce; the hold adds no lag at activation.

## 7. Delivered state, integration and residuals (22 November 2026)

Checked against `elements/PressurePlate`, its scene and its tests on published main `8d1d68aa`, and against `origin/main` `25df3cd5`, which changes none of them. Nothing was built or run.

**Disposition.** Delivered CAT part: package `elements/PressurePlate`, id `pressure_plate`, category Control, palette order 21 (`elements/PressurePlate/PressurePlateElementDeclaration.cs:29–34`). No slice is planned for this identity. It owns no named identity; EL-059 is a separate element. Every residual below is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.

**Residuals.**
- **Single impact (owner decision 8, 22 November 2026).** The 0.1 s single-impact pulse is confirmed: any qualifying impact closes the contact on its impact tick and for 11 more ticks (the 0.1 s release hold); a steady bounce keeps it closed; a ball that comes to rest gives the same final state. The requirement row's impact-only control is reworded to "a single impact closes the contact for no longer than the 0.1 s release hold" (the `requirements.md` edit belongs to that file's owner). The single-impact test is added: `PressurePlateTests.ASingleImpactClosesTheContactForItsHoldOnly` — a Basketball thrown across the plate so that it strikes once and lands off it; the supplied contact to a Powered gate is closed on no more than 12 consecutive ticks and open at the end. Control: the same ball coming to rest on the plate stays closed. Rows in [batch B readiness](readiness/batch-b.md) (gap 6).
- **Ring as a support.** The sensor's supporting colliders are 1 and 7. `DeclaredContactLoadTests.TheDeclaredSensorCompilesItsRegionAndMembersForEveryPlate` asserts the pair, and the tilted roll-off touches the ring in passing (`PressurePlateTests.cs:529–531`), but no test rests a flat load on the ring and expects Loaded. One positive row is added when the plate is next touched.
- **Required forward-only refactors.** Reword the level rule's refusal to "A level output connects only to an input that admits a level." (`ENGINE-CAPABILITY-INSTANCES`, audit E4; one wording with CAT-035 and CAT-037). Remove the unread `ElectricalNodes` ratings (12 V, 0.05 Ω, 100 W, `PressurePlateElementDeclaration.cs:85–88`, only checked finite at `CuriousContraptions.Bundle/BundleValidator.cs:873–876`). The deferred-work text for both is in batch B readiness.
- Requirement row CAT-052 stays unchecked until the post-catalogue browser phase.
- **Later row owned here (coordinator decisions 36 and 41).** [CAT-051](CAT-051-powered_gate.md) fact 23, plate half (`PressurePlateTests.cs:44–82` at harvest commit `a6c914e`): a pressed, supplied Pressure plate opens the Powered gate, and unloading closes it within 180 ticks. It is a cross-part row owned by this part's slice, because the plate is taught at 25, after the gate at 24; CAT-051 keeps the fact's text.

**Integration.**
- Delivered loads: Basketball 1 kg, Bowling ball 4 kg, Domino 0.4 kg, Weight, and the Tennis ball 0.35 kg, which stays below the 0.5 kg default alone and passes it in pairs (0.7 kg). The Baseball, 0.15 kg (the repair values of coordinator decision 36; published main declares 0.65 kg until the repair publishes), stays below the 0.5 kg default alone and in threes (0.45 kg) and passes it four together (0.6 kg).
- Delivered consumers: Battery and Powered gate; Signal lamp on the level output.
- [Current consumers](../invest/current-consumers.md#cat-052-i) names the Motor as the first visible consumer. As decided in [CAT-013 §7](CAT-013-both_gate.md#7-delivered-state-integration-and-residuals-22-november-2026), the native consumer stays the Powered gate and the Motor recipe is kept for the browser phase.
- Campaign: no level in `content/puzzles.json` places it.
- Coverage binding: catalogue key `pressure_plate` in `docs/coverage/catalogue-elements.json`.
