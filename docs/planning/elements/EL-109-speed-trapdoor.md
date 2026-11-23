# EL-109 · Speed-sensitive trapdoor named-identity spec

**Current delivery (24 October 2026):** [EL-109 delivery slice](../invest/named-elements.md#element-109). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every `path@a6c914e:Lstart-Lend` citation resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

**Build-ready revision, 21 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 were rewritten against current code; a `path:line` citation without a commit is in this checkout. Section 4 is the unchanged harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-109 |
| Name | Speed-sensitive trapdoor |
| Type | Mechanical |
| Anchor | [requirements.md#element-109](../requirements.md#element-109); [named entry](../invest/named-elements.md#element-109); scope index [todo-248](../requirements.md#todo-248) |
| Proof owner | S376 |
| Disposition | To deliver as a single element, after the engine programme, whose ENGINE-SENSOR-VOLUMES supplies the speed field it needs (section 3). Not a duplicate. The historical trap-door row [todo-220](../requirements.md#todo-220) ("a hinged/selective passage") is owned by EL-238 Selective trap door, ledgered on 21 November 2026; this identity selects by speed only. |
| Planned package | id `speed_trapdoor`, directory `elements/SpeedTrapdoor/`, class `SpeedTrapdoorElementDeclaration` |
| Refines / extends | No CAT spec. Reuses the passage sensor of [CAT-002](CAT-002-ball_detector.md), the memory and switched supply of [CAT-037](CAT-037-latch.md) and the supplied slider of [CAT-051](CAT-051-powered_gate.md) |
| Related | EL-107, EL-108; EL-162–EL-164; EL-238 |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SENSOR-VOLUMES (the in-tick passage sensor with a declared minimum crossing speed) and ENGINE-SUPPLY-DEMAND (the supplied drive); then ENGINE-JOINT-KINDS (spring rest position: the hatch spring's declared rest at coordinate 0); then S7. |

## 2. Declaration

The package implements `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). Closest delivered shape: the Powered gate (`elements/PoweredGate/PoweredGateElementDeclaration.cs:14`) for the sprung, supplied slider, with the Ball detector's sensor node and the State latch's memory-closed contact. `SupportedDomains = Mechanical | Electrical | Signal`. Footprint: 2 bodies, 1 moving, 1 prismatic constraint.

The door is a **sliding hatch** in the plane of the track, not a hinged flap held by a pin (section 6): a supplied prismatic drive with a closing spring is delivered and tested, while a latched release of a hinge is not.

### Bodies and colliders

Local axes: travel along +X, lateral Z, up Y; track top at y = 0. Boxes are full extents in metres.

| Body | Collider | Dimensions and place |
| --- | --- | --- |
| Track (static) | upstream floor | 3.3 × 0.18 × 1.3, x −3.0 to 0.3 |
| | downstream floor | 0.7 × 0.18 × 1.3, x 2.3 to 3.0 |
| | −Z side wall | 6.0 × 0.58 × 0.06 at z = −0.68, from y −0.18 to 0.4 |
| | +Z side wall | upper rail 6.0 × 0.4 × 0.06 at z = +0.68, from y 0 to 0.4; below it two skirts 3.3 × 0.18 × 0.06 (x −3.0 to 0.3) and 0.7 × 0.18 × 0.06 (x 2.3 to 3.0), leaving the slot the hatch slides out through |
| | hatch rails (2) | 0.06 × 0.06 × 1.4, centres (0.33, −0.21, 1.35) and (2.27, −0.21, 1.35): tops at y = −0.18 under the hatch's two end edges, from z 0.65 to 2.05. They are visible guides only: the slider declares `BodiesCollide` false (compiled to `ConnectedCollision.Disabled`, `engine/gpu/WorkshopPhysicsCompiler.cs:181`), so the hatch never touches the track body and the prismatic joint alone carries it (no rail friction acts against the closing spring) |
| | gate hoop | posts 0.08 × 1.08 × 0.08 at z = ±0.75 from y −0.18 to 0.9, and a lintel 1.58 × 0.08 × 0.08 at y = 0.9, at x = −2.7 |
| Hatch (dynamic, on the slider) | one box | 2.0 × 0.18 × 1.3 filling the opening from x 0.3 to 2.3, flush with the floor |

Track 6.0 m long and 1.3 m wide: the Ramp's default width, so it chains with ramps. Material, track and hatch: restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the gate's, `elements/PoweredGate/PoweredGateElementDeclaration.cs:47-48`).

**Opening length (hand calculation, repair of 22 November 2026; maximum revised 23 November 2026).** A ball's centre must be below floor level before it comes within one radius of the downstream lip, or it meets the lip corner and may hop onto the upper route. The Basketball, the largest delivered rolling ball (radius 0.34 m), falls its radius in √(2 · 0.34 / 9.81) = 0.263 s, so with a 2.0 m opening the limit is (2.0 − 0.34) / 0.263 = 6.3 m/s; a Bowling ball (radius 0.28 m, 0.239 s) reaches 7.2 m/s. At 6 m/s the Basketball's centre is only 0.34 − ½ · 9.81 · (1.66 / 6)² = −0.036 m, 36 mm below floor level, when it comes within one radius of the lip: too close to a hop for a test boundary. **Maximum supported crossing speed: 5.5 m/s**, where that depth is 0.34 − ½ · 9.81 · (1.66 / 5.5)² = −0.107 m. The draft's 1.2 m opening supported only (1.2 − 0.34) / 0.263 = 3.3 m/s, below its own 4 m/s row and its 5 m/s threshold ceiling.

### Slider and drive

One `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`) along the part's +Z, under the +Z wall: limits 0 (closed) to 1.35 m; stiffness 14 N/m and damping 4 N·s/m, the gate's fail-closed spring values (`PoweredGateElementDeclaration.cs:79`), with the spring's rest position declared at coordinate 0 (closed), so the unpowered hatch closes in every mounting orientation (owner decision 6 as extended by coordinator decision 25, 23 November 2026; owner ENGINE-JOINT-KINDS, spring rest position). With the 1 kg hatch, ω = √(14/1) = 3.74 rad/s and ζ = 4/(2·√(14·1)) = 0.53; hatch mass 1 kg with box moments 0.144, 0.474 and 0.336 kg·m² about its length, vertical and width axes (m·(b² + c²)/12 for 2.0 × 0.18 × 1.3 m); `BodiesCollide` false. Drive (`PrismaticDriveDescriptor`, `MechanicalContractValues.cs:89`), fixed target:

| Field | Value | Reason |
| --- | --- | --- |
| Target | 1.35 m | the whole opening is clear |
| MaxSpeed | 4 m/s | the hatch clears the Basketball's width (1.0 m of travel: its −Z edge passes z = +0.34) in about 0.3 s: 60 N to 2 m/s, then 120 W to 4 m/s (0.19 m in 0.08 s), cruise to 0.95 m (0.19 s), then braking |
| StoppingAcceleration | 20 m/s² | |
| MaxEffort | 60 N | the gate's; overcomes the 19 N spring at full stroke |
| MaxPower | 120 W | the gate's |

All **starting values**.

### Sensor, memory and supply

| Node | Descriptor | Values |
| --- | --- | --- |
| `gate` | `SignalPassageDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:54`) on body 0 | local position (−2.7, 0.4, 0), aperture axis +X, aperture radius 0.65, rearm clearance 0.02, **minimum speed from `speed_threshold`** |
| `release` | `SignalMemoryDescriptor` (`SignalContractValues.cs:64`) | set by `gate`; never reset within a Run |

Internal signal edge `gate.Output → release.Set`. Supply graph: `PowerIn` → contact `latch` (`SignalStateContactControl(release, Latched)`, `CuriousContraptions.Abstractions/SignalContactControl.cs:12`) → consumer `actuator` with `SupplyDriveTarget(Prismatic 0)`. Electrical node `actuator`: `ElectricalRole.Consumer`, rated 12 V, resistance 24 Ω, capacitance 0, `MaxPower` 20 W (the gate's `motor` node, `PoweredGateElementDeclaration.cs:94`). Event: `SimulationEventKind.Passage` on `gate`.

The gate is 3.0 m upstream of the opening. At the 5.5 m/s maximum supported speed a ball takes 3.0 / 5.5 = 0.55 s to cover that; the hatch clears its path in about 0.3 s plus one tick for the supply solve, leaving about 0.24 s. The drive's 120 W bound equals the default Battery's per-tick allowance (1 J per tick at 120 Hz), so the over-allowance limit of coordinator decision 27 never binds the hatch on a default Battery.

### Parameter and port

| Name | Type | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `speed_threshold` | Real | MetrePerSecond | 3 | 0.5 | 5 | Reject | BuildOnly |

Default 3 m/s separates a Basketball that rolled down a 0.8 m ramp (about 3.3 m/s) from one that rolled down a 0.3 m ramp (about 2.0 m/s).

| `PortId` | Domain | Direction | Role | Local position |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | `PowerIn` | upstream end face, (−3.0, −0.09, 0.5), normal −X |

### Metadata and bindings

Category `Motion`, display name "Speed trapdoor", palette order 119 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.76, 0.56, 0.32). Presentation: body binding `Visual/Hatch` → `PackageBodyIndex(1)`; the hoop's lamp `Visual/Lamp` blends slate `#556573` to gold `#f7cb52` on `PresentationDriver.CommittedActivation` of `release` (the latch's lamp binding, `elements/StateLatch/StateLatchElementDeclaration.cs:14`).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| `Visual/Track`, `Visual/Walls`, `Visual/Rails` | Ramp colour `#c28f52` boxes | solid, one collider each |
| `Visual/Hatch` | Ramp-colour box with a gold `#f7cb52` chevron printed on top | solid (the hatch collider); the chevron is decorative, printed flat |
| `Visual/Hoop` | cream `#fff8e9` posts and lintel | solid, box colliders |
| `Visual/Lamp` | sphere radius 0.06 on the lintel | solid, sphere collider |
| `Visual/Socket` | socket sphere | solid, sphere collider |

Icon: a track seen from the side with a gap, a ball above it and a speed chevron. It differs from the Ramp by the hoop and the chevroned hatch.

### Behaviour and numbers

A ball crossing the hoop forward at or above `speed_threshold` produces one passage pulse; a slower ball, a reverse crossing and a ball outside the aperture produce none. The pulse sets `release`; from the next tick's electrical solve (decision D7) the contact is closed and, if `PowerIn` is supplied, the drive slides the hatch out against its spring and holds it open. The ball reaches the opening and drops through. Without supply the memory still sets but the hatch does not move. The memory holds until Reset, so the hatch stays open for the rest of the Run; if the Battery then empties or is disabled, the spring closes it (row A10). Mass plays no part: equal speeds give equal routes. The hatch moves across the track, so a slow ball rolling over it applies no force along the slider. A crossing above 5.5 m/s is outside the supported range: the ball may strike the downstream lip, and no row asserts its route.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, StateTransaction. Map: "physics relative-speed observation controls a finite-work trapdoor through typed discrete state, not part-name velocity branch."

| Capability | Status | Evidence |
| --- | --- | --- |
| Forward-only passage sensor with rearm, on any static frame | present | `SignalContractValues.cs:54`; `BallDetectorTests.PassageSensorTracker_PositiveForwardPass_RotatedAndVariousSpeeds` (`CuriousContraptions.tests/BallDetectorTests.cs:152`), `PassageSensorTracker_NegativeControls_ReverseOutsideStationary` (`:257`) |
| A **minimum-speed predicate** on a passage sensor | narrower | the sensor reports the crossing speed (`PassageSensorRead.Speed`, `CuriousContraptions.Abstractions/PassageSensorValues.cs:38`) but its declaration has no speed lane (`PassageSensorValues.cs:10`) and `SignalPassageDescriptor` has no parameter. Owner: ENGINE-SENSOR-VOLUMES, in which the minimum crossing speed is a declared field of the in-tick passage sensor (gap audit story 4; below) |
| Memory set by a pulse, cleared by Reset | present | `StateLatchTests.Network_SetHoldsResetClearsAndSimultaneousRequestsResolveOff` (`CuriousContraptions.tests/StateLatchTests.cs:238`) |
| A memory-closed contact enabling a drive inside one package | present, untested | `ComposableSupplyRoutingTests.ComposedPoweredAssemblyChargesStoreAndEnablesDriveWhenRouteIsClosed` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:91`) routes a supply across three packages to a drive, and `PackageInternalSupplyEdgesAdmittedByValidationAndCompileToTopology` (`:499`) proves package-internal edges to a store; neither closes a memory contact onto a drive inside one package |
| Supplied prismatic drive with a closing spring, within speed, effort and power | present | `DeclaredPrismaticDriveTests.ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep` (`CuriousContraptions.tests/DeclaredPrismaticDriveTests.cs:92`) |
| Package-internal signal edge from a sensor to a memory | present, untested | internal edges are tested with fixtures (`ComposableSignalNodesTests.PublicAliasesOfAnInternalDeliveryCountOnce`, `CuriousContraptions.tests/ComposableSignalNodesTests.cs:191`); no delivered package wires a passage node to a memory |
| A slider face **carrying cargo that rolls over it**, then sliding out from under | present, untested | the gate's blade blocks a ball from the side (`DeclaredPrismaticDriveTests.ABallRestingAgainstTheClosedTiltedBladePassesDownstreamWhenEnabledDuringRun`, `:403`); none rolls across a slider's top face |
| A declared **spring rest position** on a prismatic coordinate, so an unpowered spring-returned slider closes in every mounting orientation | missing | the delivered slider spring has stiffness and damping only (`PrismaticSliderDescriptor`, `MechanicalContractValues.cs:104`) | ENGINE-JOINT-KINDS (spring rest position; owner decision 6 as extended by coordinator decision 25, 23 November 2026), proved by that story on its own fixtures; this part's row A10 is its first package consumer |

Counts: 3 present, 3 present-untested, 1 narrower, 1 missing.

**Requirement this part puts on ENGINE-SENSOR-VOLUMES (owner decision 3a; gap audit story 4).** That story evaluates passage crossings inside the physics tick, on the committed motion, relative to the sensor's frame. Its passage sensor carries an optional declared minimum crossing speed, bound to a Real parameter in m/s: a forward crossing below it leaves the sensor's phase and count unchanged and publishes nothing; a sensor that declares none counts every forward crossing. The field belongs to every passage sensor, not to this part; it touches the abstraction, bundle validation, wire and solver, and the story proves it with two differently parameterised fixtures, neither of them this part. This part asks nothing else of the story. A hinged door with a latched release would instead wait for ENGINE-JOINT-KINDS (a hold on a revolute coordinate and its release by a physical request; engine-stories-index C2), which is why the hatch slides. On the level track every row uses, the hatch moves along a horizontal axis, so its closing spring does not work against gravity. The hatch spring declares its rest position at coordinate 0 (closed), owner decision 6 as extended by coordinator decision 25, 23 November 2026; owner ENGINE-JOINT-KINDS (spring rest position).

## 4. Sources and legacy

- Requirement: "Physical sensor and supplied latch distinguish taught incoming speed"; outcome "Equal-size slow control stays on its declared route" ([element-109](../requirements.md#element-109)). Map: "physics relative-speed observation controls a finite-work trapdoor through typed discrete state, not part-name velocity branch"; decision S257 ([decisions](../invest/decisions.md#s257)).
- [todo-220](../requirements.md#todo-220): hinged/selective passage; test thresholds, heavy/light bodies and moving collision geometry.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Passage sensor declaration: owned rigid frame, aperture radius, rearm clearance | `engine/ScenePassageSensorDeclaration.cs@a6c914e:L3-L4` | carry forward (shape), do not carry forward (class) | Generic aperture sensor data. |
| 2 | Hinge with symmetric travel limits and connected collision disabled | `parts/ImpactLeverPart.cs@a6c914e:L17-L27` | carry forward | Hinge declaration pattern. |
| 3 | Accepted: outside-aperture passage disarms without counting; reverse or stationary motion publishes nothing | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L96-L107` | carry forward | Directional, physical sensing. |
| 4 | Slider controller: mass 1 kg, max speed 2.8 m/s, acceleration 14, return spring 14, damping 4, effort 60 N, power 120 W | `engine/SlidingBlade.cs@a6c914e:L12-L26` | carry forward (form), values scaled | Finite-work latch drive. |

No legacy trapdoor exists in code or tests. Row 2's hinge is not used by the current design.

**Files harvested:**
- `engine/ScenePassageSensorDeclaration.cs`
- `parts/ImpactLeverPart.cs` (hinge pattern)
- `CuriousContraptions.tests/PhysicsPassageSensorTests.cs`
- `engine/SlidingBlade.cs`
- `parts/PipeBendPart.cs` (material reference)
- Searched with no hit for `trapdoor` or `trap door`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Requirement row: [element-109](../requirements.md#element-109). Planned test class `SpeedTrapdoorTests`. "Lower route" means the ball's centre is below y = −0.5; "upper route" means it passes x = 3.0 above y = 0. Speeds are the committed crossing speed read at the hoop, not the launch speed.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given supply, threshold 3 and a Basketball crossing at 4 m/s, when Run, then it takes the lower route; an equal Basketball crossing at 1.5 m/s takes the upper route | `AFastBallDropsThroughAndAnEqualSlowBallCrossesOver` (theory) | each row is the other's control | at threshold 5 a Basketball crossing at 5.5 m/s (the maximum supported speed, 10 % above the threshold) takes the lower route without touching the downstream floor's top face; guard 10 s |
| A2 | Given crossings at 2.7 and 3.3 m/s, then only the second opens the hatch | `SpeedsEitherSideOfTheThresholdTakeDifferentRoutes` | A1 | ±10 % of the threshold: wider than the 0.1 m/s a ball loses to rolling resistance between a ramp foot and the hoop |
| A3 | Given no supply and a 4 m/s crossing, then the ball takes the upper route and the hatch has not moved | `WithoutSupplyAFastBallStaysOnTheUpperRoute` | A1 | hatch coordinate below 0.01 m |
| A4 | Given a Bowling ball and a Basketball at the same speed, then both take the same route | `MassDoesNotChangeTheRoute` (theory, above and below the threshold) | — | — |
| A5 | Given a 4 m/s crossing from the downstream side, then the hatch stays closed | `AReverseCrossingNeverOpensTheHatch` | A1 | — |
| A6 | Given a Bowling ball at rest on the hatch when a second, fast ball opens it, then the resting ball ends on the lower route, and its horizontal speed is under 1.5 m/s when its centre passes y = −0.5 (friction from a hatch sliding at up to 4 m/s brings a resting solid ball, moment 2/5 m r², to at most 2/7 × 4 = 1.14 m/s before it rolls without slip; the fall adds no horizontal speed) | `ABallRestingOnTheHatchFallsWhenItOpens` | — | the 1.5 m/s bound fails a hatch that flings its load or a contact that adds energy |
| A7 | `speed_threshold` 0.49, 5.01 and NaN are refused; 0.5 and 5 are admitted | `ThresholdOutsideItsRangeIsRefused` | — | exact limits; S4 row 2 replaces it |
| A8 | A Signal output offered to `PowerIn` is refused | S4 row 5; until it lands `ASignalOutputCannotSupplyTheTrapdoor` | a battery `Supply` is accepted | typed refusal |
| A9 | Reset closes the hatch and clears the memory, and A1 repeats; save and load keep the threshold and the wire | `ResetAndSaveLoadRestoreTheClosedHatch` | — | generic rows: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`, S4 rows 1 and 3 |
| A10 | Given supply, threshold 3 and a Basketball crossing at 4 m/s, so the hatch opens and holds at 1.35 m; when the Battery's `enabled` (a Run control) is set to Disabled 2 s after the crossing, then the hatch coordinate is below 0.01 m within 3 s. By hand: ω = 3.74 rad/s, ζ = 0.53, ω_d = 3.74 · √(1 − 0.53²) = 3.16 rad/s; released from 1.35 m the hatch first reaches 0 at ω_d · t = π − atan(√(1 − ζ²) / ζ) = 2.13 rad, t ≈ 0.68 s, where its closed limit stops it | `TheHatchClosesWhenItsSupplyIsLost` | supply kept: the hatch stays at 1.35 m ± 0.02 m over the same 3 s | 3 s is over four times the derived 0.68 s; a drive that self-locks when unsupplied, or a spring without its declared rest position, leaves the hatch open and fails |

No arrival tick is asserted.

### Integration

- Fed by CAT-054 Ramp and the pipe kit; supplied by CAT-005 Battery; all delivered.
- Campaign: coverage row "spiral delay, airlift and trapdoor variants", specialist routes 91–100, reuse 41–50, 111–120, 136–150 (`docs/planning/requirements.md:2398`). Owner decision 24e moves the first use to level **28b** in chapter 3 ([per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)).
- Coverage binding: a final package-source-binding section in this spec (Package, Named identity, Acceptance, Decision, Mode records), as EL-239 on published main; blocked on S7 (section 7).

### Delivery

- **Shape.** Single element slice after the engine programme (owner decision 3a), whose ENGINE-SENSOR-VOLUMES delivers the speed field. Not classified in delivery batches; it belongs in 1b "Selective passage" with the note "not data-only today".
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SENSOR-VOLUMES (the in-tick passage sensor with its declared minimum crossing speed) and ENGINE-SUPPLY-DEMAND (the supplied drive); then ENGINE-JOINT-KINDS (spring rest position, coordinator decision 25); then S7 (the coverage follow-on, which widens the reader, owner decision 15). S2–S4 optional.
- **Files (element slice).** `elements/SpeedTrapdoor/` (project, declaration, `speed_trapdoor.tscn`, `icon.svg`); `CuriousContraptions.tests/SpeedTrapdoorTests.cs` (the test project compiles every top-level `*.cs` by one rule on published main, so no compile-list line); one `CuriousContraptions.slnx` line; the binding section of section 7 once S7 admits it; until S3, the trim list and placement switch.
- **Review tier.** Engine tier (the element slice has two *present, untested* rows).
- **Disqualifiers the probe re-checks.** ENGINE-SENSOR-VOLUMES delivered without the minimum crossing speed field; a ball catching on the hatch seams as it rolls over (then reduce the seam, numbers only); a 5.5 m/s ball reaching the opening before the hatch has cleared it, or touching the downstream lip (then lengthen the approach or the opening; numbers only); placement refusing a 6.0 m part (not verified).

## 6. Open questions

All resolved.

1. **Selection is by speed only.** The row says "taught incoming speed" and its control is "equal-size slow". Selection by size or mass, and the historical todo-220 rule, belong to EL-238.
2. **The hatch stays open until Reset.** No re-arm within a Run: one clear lesson, and the memory needs no reset source.
3. **No `ActivationOut`** (D19): nothing in the row or a lesson uses the release state.
4. **Sliding hatch instead of a hinged door and latch pin.** Reason in section 2; decided under the standing delegation.
5. **Threshold range 0.5–5 m/s, opening 2.0 m, maximum supported crossing speed 5.5 m/s** (22 November 2026, audit H4; maximum lowered from 6 m/s on 23 November 2026 after the second audit, because at 6 m/s a Basketball is only 36 mm below floor level as it reaches the lip). Reason: the hand calculation in section 2; lowering the threshold ceiling to the 3.3 m/s a 1.2 m opening supports would leave almost no range above the 3 m/s default. All dimensions remain starting values under D1.
6. **Side walls reach the floor's underside and the hoop posts stand from it** (22 November 2026): the draft's walls and posts stood beside the floor on nothing. The +Z wall is split so the hatch has a slot.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** A static track, one slider body with a declared spring and supplied drive, one declared passage sensor, one declared memory, one internal signal edge, one memory-closed supply contact, one port and one parameter. There is no trapdoor rule, velocity branch or state machine in code.
2. **Generic laws.** The passage-sensor rule; the signal memory; the signal-state contact in the supply network; the prismatic drive law against a declared spring; contact and gravity. The one missing capability is general and stated element-free: **an optional declared minimum crossing speed on any passage sensor, bound to a Real parameter; a forward crossing below it leaves the sensor's phase and count unchanged and publishes nothing.** One owner, ENGINE-SENSOR-VOLUMES (owner decision 3a; the minimum crossing speed is a declared field of its in-tick passage sensor), proved on two differently parameterised fixtures, neither of them this part. A second missing capability is the declared spring rest position (owner ENGINE-JOINT-KINDS, coordinator decision 25), stated element-free: **a prismatic or revolute spring may declare its rest coordinate.**
3. **Three layers.** Colliders: fourteen. Static body: two floors, the −Z wall, the +Z upper rail and its two skirts, two hatch rails, two hoop posts and a lintel (boxes 11), a lamp sphere and a socket sphere. Hatch body: one box. Visuals: one mesh per collider, with the hatch's chevron printed flat. Animation: the lamp's colour from the memory's committed activation; decorative, nothing written back. `Visual/Hatch` follows body 1's pose.
4. **Flow and typing.** One typed Electrical `PowerIn`; `speed_threshold` is an f32 Real in m/s with `Reject` policy; sensor, memory and contact are typed nodes. The sensor is a bounded aperture (radius 0.65 m).
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 14 (boxes 12, spheres 2); joints 1 prismatic; drives 1; sensors 1; signal memories 1; internal signal edges 1; supply contacts 1; electrical nodes 1; stores 0; presentation bindings 2 (hatch body, lamp colour).
6. **Performance.** Fourteen colliders, thirteen static, against the Powered gate's eight: the track is a visible floor with walls, a slot and a hoop, each solid. A ball at up to 5.5 m/s crossing the hoop and the opening is covered by the engine's general means (the passage sensor's crossing test on the committed motion, inside the tick once ENGINE-SENSOR-VOLUMES replaces today's host-side tracker, and the velocity-fattened broadphase of ENGINE-SPATIAL-QUERIES); the part adds no check. The speed field is one comparison on a value the sensor already computes. No island sleeping exists today; a resting closed hatch costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
7. **Tests are scenarios.** Rows A1 to A10 run the production simulation; the hand numbers of section 2 choose the speeds.

**Verdict: conforms; blocked on two general capabilities stated element-free, each with one owner: the sensor's minimum crossing speed (ENGINE-SENSOR-VOLUMES) and the spring rest position (ENGINE-JOINT-KINDS).**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `speed_trapdoor` |
| Named identity | EL-109, `../invest/named-elements.md#element-109` |
| Acceptance | `todo-248` and `sequence-task-662` (`requirements.md:5269`, the element-109 row) |
| Decision | S376, `../invest/scope-corrections.md#s376` |
| Mode records | `Configuration=Adjustable` (`speed_threshold`) |

What it lacks today: `scope-corrections.md` has no `s376` block on published main; `todo-248` is a scope index shared by EL-107, EL-108 and EL-109 with no sequence-task anchor directly after it, which the reader requires; and one task source can bind only one package.
