# EL-100 · Vacuum nozzle — named-identity spec

**Current delivery (24 October 2026):** [EL-100 delivery slice](../invest/named-elements.md#element-100). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 are written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lstart-Lend` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-100 · Vacuum nozzle · Specialist |
| Anchor | [requirements.md#element-100](../requirements.md#element-100) ([sequence-task-653](../requirements.md#sequence-task-653)); [named entry](../invest/named-elements.md#element-100); scope index [todo-227](../requirements.md#todo-227) |
| Proof owner | S670 |
| Disposition | To deliver as a single element. Not a duplicate of the Fan: the flow runs toward the part, through a real opening, and it has no hub, store or shaft input |
| Planned package | id `vacuum_nozzle`, directory `elements/VacuumNozzle/`, class `VacuumNozzleElementDeclaration` |
| Refines / extends | [CAT-028 Fan](CAT-028-fan.md) (airflow source, Electrical input); [CAT-010 Bellows](CAT-010-bellows.md) by ledger note only: it uses no gas chamber |
| Status | not started; blocked on the engine programme (owner decision 3a), then `ENGINE-REACTION-THRUST` (its converging-flow clause), then S7 |

## 2. Declaration

A static hollow intake with a tube outlet and one supplied airflow source whose flow converges on a point inside the tube. The package implements `IAirflowSourceElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:40`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). Closest delivered shape: the Fan's Electrical mode (`elements/Fan/FanElementDeclaration.cs:46`–`:56`, `:72`–`:76`) on the Funnel's geometry (a Frustum with two mouths of different bore). `KinematicMode.Static`, `SupportedDomains = Mechanical | Electrical`. Footprint: 1 body, 0 moving, 0 constraints.

### Body and colliders

One static body. Local +X points out of the intake; the axis is 1.0 m above the surface the part stands on. Dimensions in metres.

| Collider | Kind | Dimensions and place |
| --- | --- | --- |
| Intake cone | Frustum about X | x 0 to 0.6; wall 0.05; outer radius 0.70 at x = 0 (inner 0.65) widening to 1.00 at x = 0.6 (inner 0.95) |
| Outlet tube | Tube about X | inner 0.65, outer 0.70, x −1.29 to 0 |
| Outlet collar | Torus, full sweep | centreline 0.715, tube radius 0.065, at x = −1.2 (the Clear pipe's collar) |
| Foot | Box | 0.3 × 0.22 × 0.6 centred (−1.2, −0.89, 0): from the collar's underside to the standing surface |
| Blower housing | Box | 0.5 × 0.3 × 0.5 centred (−0.6, 0.85, 0), on top of the tube |
| Socket | Sphere | radius 0.06 at (−0.6, 1.06, 0) |

Material: restitution 0.15, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the tube kit's, `elements/Pipe/PipeElementDeclaration.cs:62`).

**The lip.** The cone's outer wall touches the standing surface at the rim (0.95 + 0.05 = 1.0 below the axis), so its inner wall starts **0.05 m** above that surface: a ball meets a 0.05 m lip, then climbs the remaining 0.30 m over 0.6 m (26.6°) to the bore floor. The steady pull needed to roll a ball of radius r over a 0.05 m lip from rest is m × 9.81 × tan θ with cos θ = (r − 0.05) / r: 2.6 N for the Tennis ball, 6.0 N for the Basketball, 27 N for the Bowling ball. The default 12 N takes the first two over from rest; the Bowling ball crosses only on the speed it arrives with (it needs 1.0 m/s and arrives near 3).

**Mouths.** `TubeEnd.Start` at (0.6, 0, 0), bore radius 0.95, the open intake (nothing delivered has that bore, so nothing snaps to it); `TubeEnd.End` at (−1.29, 0, 0), bore radius 0.65, which snaps to every delivered tube part. The Funnel declares two unequal bores the same way (`CuriousContraptions.tests/TubeMouthSnapTests.cs:104`–`:105`).

**The finite opening.** The throat is the bore 1.30 m across (radius 0.65 m). Basketball (0.68 across), Bowling ball (0.56) and Tennis ball (0.50) pass. A Tennis ball with `radius` 0.7 or more does not: it lodges in the cone and blocks the inlet.

### Airflow source (one, declared)

| Field | Value | Reason |
| --- | --- | --- |
| Frame, origin, axis | body 0, (−1.2, 0, 0), +X | the sink point is inside the outlet tube, 1.2 m behind the throat, so cargo is still drawn while it crosses the throat |
| Flow sense | Converging (`ENGINE-REACTION-THRUST`, clause (b)) | force on a sample points from the sample to the origin |
| Ambient reaction | none (a converging source has no ambient share, and the body is static) | |
| Reach | 4.2 m | ends 2.4 m in front of the intake rim |
| Terminal radius | 2.0 m | radius 0.476 × distance from the origin: 0.57 at the throat, 0.86 at the rim, 2.0 at full reach |
| Flow speed | 8 m/s | so that `force` × flow speed stays within 120 W |
| Maximum force | parameter `force` | |
| Maximum power | `force` × 8 m/s | the Fan's product form (`FanElementDeclaration.cs:50`–`:51`) |
| Internal occluders | none | the part's own walls must block the flow: a sample whose line to the origin crosses the cone or tube wall gets nothing |
| Input | one `ElectricalAirflowInput` (`CuriousContraptions.Abstractions/AirflowValues.cs:37`) on supply consumer `consumer`, no selection, no enable latch | supply alone enables it |

### Parameter, port, supply

| Name | Type | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `force` | Real | Newton | 12 | 0 | 15 | Reject | BuildOnly |

| `PortId` | Domain | Direction | Role | Local position |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | `PowerIn` | the socket, (−0.6, 1.06, 0), normal +Y |

Supply graph: one consumer with `SupplyContinuousWorkTarget(source 1)` bound to `PowerIn` (`FanElementDeclaration.cs:72`–`:74`). Electrical node: `consumer`, `ElectricalRole.Consumer`, 12 V, 1.2 Ω, 0 F, `MaxPower` 120 W; a declared rating that nothing reads today, the airflow source's own power cap being what limits the debit. No stores. Reach and width are fixed (D13).

### Metadata and bindings

Category `Power`, display name "Vacuum nozzle", palette order 303 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.4, 0.72, 0.79). Presentation: `Visual/Rotor` turned by `PresentationDriver.CommittedFlowSpeed` with `PresentationCurve.IntegratedRate` (the Fan's blade binding, `FanElementDeclaration.cs:85`–`:88`).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| `Visual/Intake` | Fan-blue `#66b8c9` cone shell with a cream `#fff8e9` band printed at the rim | hollow, the Frustum |
| `Visual/Tube` | clear `#66b8c9` cylinder, alpha 0.16 | hollow, the Tube |
| `Visual/Collar` | cream torus | solid, the Torus |
| `Visual/Foot`, `Visual/Housing` | navy `#293954` boxes | solid, one Box each |
| `Visual/Socket` | navy sphere | solid, the Sphere |
| `Visual/Rotor` | four-blade rotor behind a grille drawn on the housing face | decorative: enclosed, and moved by a binding |
| `Visual/Arrows` | three navy chevrons printed on the cone's outer wall, pointing inward (−X) | decorative: printed flat |

No hose or duct is drawn: the outlet joins a pipe mouth directly, so no connection strand is involved (owner decision 1, 22 November 2026: hoses and ducts are solid strands owned by `ENGINE-CONNECTION-MATERIALS`; this design has none). Icon: a cone narrowing into a tube with three arrows entering the cone. It differs from the Fan (arrows leaving, a hub) and from the Funnel (no housing, foot or socket) by shape.

### Behaviour and numbers

Each exposed sample of a responsive body inside the cone receives `force` × weight × (1 − v/8) toward the origin, where v is its speed toward the origin; the source's total is capped by `force` and by power (the Fan's law, `CuriousContraptions.Abstractions/ConicalAirflow.cs:19`–`:33`). "Responsive material" is every body that declares an airflow receiver: the balls and the Balloon do (`elements/Balloon/BalloonElementDeclaration.cs:40`); a body that declares none is not drawn.

**Payment.** The debit is impulse × flow speed, not force × the cargo's own speed, so it is highest when the cargo is still: a ball at rest in the cone, or held against a blockage, costs the full 12 N × 8 m/s = **96 W** continuously (0.8 J per tick at 120 Hz), and a ball moving at v costs 96 (1 − v/8) W. At the 15 N maximum the standstill cost is 120 W, 1 J per tick. The default Battery's `power_limit` (`elements/Battery/BatteryElementDeclaration.cs:44`) supplies either, and its 3 600 J lasts 37 s of continuous holding at the default. With nothing in the cone nothing is paid.

Hand calculation, default 12 N, a ball at rest on the standing surface 1.5 m in front of the rim (3.3 m from the origin, where the cone is 1.57 m in radius and the ball's centre is 0.66 to 0.75 m below the axis, so all four samples are inside). Along the path m·v·dv/dx = 12 (1 − v/8), so the distance travelled to speed v is (8m/12)(−v − 8 ln(1 − v/8)). Over the 2.3 m to the point where the cone becomes narrower than the ball's samples (about 1.0 m from the origin), ignoring rolling inertia:

| Ball | Mass | Speed before the lip and climb | After rising 0.35 m (v² − 6.9) | Result |
| --- | --- | --- | --- | --- |
| Tennis ball | 0.35 kg | 7.0 m/s | 6.5 m/s | drawn in and out of the outlet |
| Basketball | 1 kg | 5.3 m/s | 4.6 m/s | drawn in and out of the outlet |
| Bowling ball | 4 kg | 3.1 m/s | 1.7 m/s | drawn in slowly; not asserted |

No ball can exceed 8 m/s. Past the origin the cone ends and the ball coasts on into whatever is joined to the outlet.

**Pointing down.** The nozzle picks up a Tennis ball (3.4 N of weight) or a Basketball (9.8 N, net 2.2 N) from below and **holds** it in the tube: the ball rises to the origin, the force falls away as it passes, and it bobs there for as long as the supply lasts, at up to 96 W. It does not lift cargo through and out. A Bowling ball (39 N) is left where it is.

**Blocked inlet.** A Wall between a ball and the intake cuts every sample's line to the origin: no force. A ball too large for the throat lodges in the cone and shadows whatever is behind it, and is itself held at the standstill cost. **Missing supply.** No flow, no force, no debit.

**Tick order.** Supply is resolved in the electrical phase; airflow acts in the physics step of the same tick.

## 3. Engine capabilities

Map row families: AerodynamicDrag, EnvironmentState, FiniteLedger, FiniteWorkActuation, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics (navigation only under D14; the part has no gas store, joint or pressure work).

| Capability | Status | Evidence |
| --- | --- | --- |
| Conical airflow source paid from an electrical consumer; nothing unwired | present | `AirflowSourceDescriptor` (`AirflowValues.cs:45`); `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`CuriousContraptions.tests/FanIntegrationTests.cs:540`) |
| A solid between source and receiver stops the transfer, re-evaluated as it moves | present | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`) |
| The source's own colliders occlude unless declared internal | present | `AirflowRuntimeTests.OnlyExactInternalCollidersAreExemptFromOcclusion` (`AirflowGeneralityTests.cs:216`) |
| A receiver at or above the flow speed takes no work | present | `AirflowRuntimeTests.ReceiverAtOrAboveFlowSpeedCannotConsumeSourceWork` (`AirflowGeneralityTests.cs:37`) |
| A stationary receiver is charged the full rated power | present | `AirflowRuntimeTests.OpposingJetsUseOneCoupledVelocity` (`AirflowGeneralityTests.cs:162`): each of two opposed 9 N jets pays 0.9 J in one tick, 108 W |
| A hollow Frustum a ball passes through inside its wall; two mouths of different bore | present | `HollowElementTests.ABallDroppedIntoTheFunnelsInletLeavesThroughItsThroatInsideTheWall` (`CuriousContraptions.tests/HollowElementTests.cs:437`); `TubeMouthSnapTests.ThePipeSnapsToTheFunnelOutletButNotItsWideInlet` (`TubeMouthSnapTests.cs:231`) |
| Flow-speed-driven rotor binding that stops with its source | present | `FanElementDeclaration.cs:85`; `FanIntegrationTests.DeclaredFlowBindingSamplesQueuedInteriorsAndStopsWithItsActualSource` (`FanIntegrationTests.cs:353`) |
| Flow **toward** the origin | missing | today the force is along +axis away from the origin, the flow speed must be non-negative and the axis a unit vector (`AirflowValues.cs:70`–`:86`), and occlusion is tested from the origin's plane to the sample along the axis (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:120`). Turning the axis round does not give suction: the blocked-inlet test would look on the wrong side of the cargo. Owner: `ENGINE-REACTION-THRUST`, clause (b) |
| Airflow receivers and occluders found without visiting every receiver and collider | missing | each source visits every receiver (`WorkshopPhysicsEngine.Airflow.cs:105`) and each sample's occlusion every collider (`:149`). Owner: `ENGINE-SPATIAL-QUERIES` (owner decision 3a) |

**The owning story.** Converging flow is not a story of its own: it edits the same airflow source descriptor and paired reaction as the ambient reaction, so both land in one change. The canonical text is in [EL-090, section 3](EL-090-steerable-blimp.md#3-engine-capabilities); its clause for this part reads:

> (b) **Flow sense.** A source declares a flow sense from a closed enum (Diverging, Converging). A diverging source behaves exactly as today. For a converging source the region is the same cone; the force on a sample is along the unit vector from the sample to the origin; the receiver's speed is measured along that vector; the occlusion segment runs from the sample to the origin, with the same internal-occluder exemptions; caps, payment, pairing with the source frame and atmosphere scaling are unchanged. A converging source has no ambient share: declaring both is refused. One consumer is recorded today, EL-100.

`ENGINE-GAS-NETWORK` does not fit: its text is stores, lines and valves, and this part has none.

## 4. Sources and legacy

- Requirement: "Supplied pressure/airflow draws responsive material through a finite opening"; outcome "Blocked inlet or missing supply prevents suction" ([element-100](../requirements.md#element-100)).
- Legacy search at `a6c914e` for "vacuum" finds only a gas-nozzle test's zero back-pressure case.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | A closed area or balanced pressure gives zero flow and zero force | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74` | Carry forward as "blocked inlet gives no suction" |
| L2 | Choked nozzle mass rate and thrust against back pressure | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L31-L47` | Do not carry forward: an f64 compressible model; the part uses the airflow law |
| L3 | Fan: solid obstacles block the direct jet | `parts/catalog/fan.tres@a6c914e:L11-L14` | Carry forward the occlusion rule |

**Files harvested:** `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`, `parts/catalog/fan.tres`.

## 5. Acceptance outline

Requirement row: [element-100](../requirements.md#element-100). Planned test class `VacuumNozzleTests`. The nozzle stands on the bench with a level Clear pipe snapped to its outlet; "drawn through" means the ball's centre crosses the outlet mouth plane. All rows fit the default 30 s Run.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a Battery on `PowerIn` and a Tennis ball at rest 1.5 m in front of the rim, then it is drawn through | `ASuppliedNozzleDrawsABallThroughItsThroat` | A2 | 6 s stall guard; speed at the outlet between 4 and 8 m/s (6.5 by hand; never above the flow speed) |
| A2 | Given the same scene with no wire, then the ball does not move and nothing is debited | `MissingSupplyGivesNoSuction` | A1 | speed under 0.05 m/s for 3 s; debit exactly zero (no payment is a ledger fact, not a simulated float) |
| A3 | Given a Wall between ball and intake, then the ball does not move; given the same Wall behind the ball, then it is drawn in | `ASolidInFrontOfTheInletBlocksSuction` | the Wall behind | as A2 for the blocked case |
| A4 | Given a Tennis ball with `radius` 0.7 nearest the intake and a default Tennis ball behind it, then the large ball lodges in the cone, does not pass the throat, and the ball behind is not drawn | `CargoWiderThanTheThroatLodgesAndBlocksTheInlet` | A1 | the large ball's centre stays in front of x = 0; the small ball's speed stays under 0.3 m/s for 5 s |
| A5 | Given a Basketball at the same place, then it is drawn through more slowly than the Tennis ball | `HeavierCargoIsDrawnMoreSlowly` | A1 | outlet speed 3 to 6.5 m/s (4.6 by hand) and below the Tennis ball's |
| A6 | Given a ball 3.0 m in front of the rim (outside the reach) and one 1.5 m to the side of the axis at the rim plane (outside the cone), then neither moves | `BodiesOutsideTheConeAreNotDrawn` | A1 | speed under 0.05 m/s for 3 s; both positions are at least 0.5 m outside the cone |
| A7 | Given A1, then the Battery's debit equals the airflow work; given A4's lodged ball held for 5 s, then the debit over that time is the standstill rate; and no tick debits more than 1 J at `force` 15 | `SuctionIsPaidAndBounded` | A2 | debit never below the work and above it by no more than the ledger residual; held: 96 W × 5 s = 480 J within 10 %; no tick debits more than the 1 J allowance (power_limit × dt) plus the ledger residual |
| A8 | Given the nozzle pointing down 1.5 m above a Tennis ball on the bench, then the ball is picked up and held inside the tube and does not leave by the outlet | `PointingDownItHoldsALightBall` | a Bowling ball stays on the bench | the ball's centre is within 0.4 m of the origin from 3 s to 8 s; the Bowling ball rises less than 0.02 m |
| A9 | Given a Domino in the cone, then it is not drawn | `ABodyWithoutAReceiverIsNotResponsive` | A1 | speed under 0.05 m/s for 3 s |
| A10 | A Signal output offered to `PowerIn` is refused; `force` −0.1 and 15.1 are refused; Reset restores every ball and the Battery; save and load keep `force`, pose and the wire | `ConnectionsAndForceAreValidatedAndStateRoundTrips` | a Battery `Supply` is accepted; 0 and 15 are accepted | generic: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

### Integration

- Works with: Battery and switched `Supply` outputs; Tennis ball, Basketball, Bowling ball, Balloon (all declare receivers; delivered); Wall as the blocker; Clear pipe, bends and Powered gate on the outlet; Basket or Receiver beyond. All delivered.
- Campaign: no chapter row named it; owner decision 24c gives it new level **27a** *Clean Sweep* (chapter 3): introduction 27a, practice 29, reuse 96 and 147 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)). A level that fixes one needs the lesson-fixture story (S8).
- Coverage binding: section 7.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a; of its stories this part leans on `ENGINE-SPATIAL-QUERIES` for airflow candidates and `ENGINE-SUPPLY-DEMAND` for the debit); then `ENGINE-REACTION-THRUST`, whose clause (b) is the converging flow; then S7.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (`PowerIn`, `TubeEnd`).
- **Files.** `elements/VacuumNozzle/` (project, declaration, `vacuum_nozzle.tscn`, `icon.svg`); `CuriousContraptions.tests/VacuumNozzleTests.cs`; one `CuriousContraptions.slnx` line; until S3, the remaining per-package lists.
- **Review tier.** Data-only once the engine story is published with tests; otherwise engine.
- **Disqualifiers the probe re-checks.** A converging sense delivered with axis-parallel occlusion (A3 would fail); a Frustum about X that cannot carry a mouth of bore radius 0.95; a ball that cannot roll over the 0.05 m lip with the pull computed above; whether a Domino declares an airflow receiver (A9 assumes it does not).

## 6. Open questions

All resolved.

1. **Airflow, not a pressure chamber.** The requirement allows either; airflow exists, is paid and is occluded by solids. No gas store.
2. **No canister.** Cargo leaves by the outlet mouth into whatever the player joins (D19); a canister would be a second container part.
3. **Throat is the standard bore, 1.30 m across**, so every delivered ball passes and the outlet joins the tube kit. Blocking is proven with a resized Tennis ball (D12's method).
4. **Flow speed 8 m/s, `force` 0 to 15 N, default 12**: **starting values** (D1), chosen so the rated power never exceeds the default Battery's 120 W.
5. **Reach and cone fixed** (D13).
6. **A held ball costs power.** That is the airflow law's payment, not a special case; a level that leaves cargo lodged for long sizes its Battery for it.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`) and owner decisions 1 and 3a.

1. **Declaration data only.** A static hollow body, one airflow source, one supply consumer, one port, one parameter, one binding. No capture rule, no teleport.
2. **Generic laws.** The conical airflow law with a converging sense, its solid occlusion and its payment rule; contact and friction in the cone and bore. The general capability is missing, with one owner; the part is its single consumer today, which is recorded in the story.
3. **Three layers.** Colliders: Frustum 1, Tube 1, Torus 1, Box 2, Sphere 1. Visuals: one mesh per collider; chevrons and rim band printed flat. Animation: the rotor, turned by the committed flow speed, enclosed and read-only. No other art node moves.
4. **Flow and typing.** One typed Electrical input; flow sense, `TubeEnd` and `ElectricalRole` are enums; one Real parameter in newtons.
5. **Flat state.** Bodies 1 (static); colliders 6; joints 0; drives 0; sensors 0; airflow sources 1; domain nodes: 1 supply consumer, 1 electrical node; stores 0; presentation bindings 1.
6. **Performance.** Six colliders: the foot, housing and socket are visible solids beside the three hollow shapes. The cone is a bounded volume whose candidates come from `ENGINE-SPATIAL-QUERIES`; the present pass over every receiver and collider is that story's to remove and nothing here is designed around it. A ball held in the airflow does not sleep (`ENGINE-ISLANDS-SLEEP`).
7. **Tests are scenarios.** Every row runs the production simulation with a delivered Battery, Wall and balls.

**Verdict: conforms** (general capabilities missing, each with one owning story).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `vacuum_nozzle` |
| Named identity | EL-100, `../invest/named-elements.md#element-100` |
| Acceptance | `todo-227`, `sequence-task-653` (`docs/planning/requirements.md:5197`) |
| Decision | S670, `../invest/scope-corrections.md#s670` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s670` block; and `todo-227` is a scope index shared by EL-097 to EL-106 that is not anchored beside `sequence-task-653`, which the reader's task-source rule requires. The sequence task itself exists.
