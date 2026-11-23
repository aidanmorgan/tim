# EL-108 · Powered airlift named-identity spec

**Current delivery (24 October 2026):** [EL-108 delivery slice](../invest/named-elements.md#element-108). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every `path@a6c914e:Lstart-Lend` citation resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

**Build-ready revision, 21 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 were rewritten against current code; a `path:line` citation without a commit is in this checkout. Section 4 is the unchanged harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-108 |
| Name | Powered airlift |
| Type | Mechanical |
| Anchor | [requirements.md#element-108](../requirements.md#element-108); [named entry](../invest/named-elements.md#element-108); scope index [todo-248](../requirements.md#todo-248) |
| Proof owner | S480 |
| Disposition | To deliver as a single element. Not a duplicate of the Fan (CAT-028, delivered 12.2): it adds a confined vertical passage, a seat and tube mouths, and has one supply mode. |
| Planned package | id `airlift`, directory `elements/Airlift/`, class `AirliftElementDeclaration` |
| Refines / extends | No CAT spec. Reuses the Fan's airflow source ([CAT-028](CAT-028-fan.md)) and the tube kit's bore ([CAT-048](CAT-048-pipe.md)) |
| Related | EL-209 Fan, EL-107, EL-109, CAT-005 Battery |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (airflow candidates) and ENGINE-SUPPLY-DEMAND (the supply debit); then S7. |

## 2. Declaration

The package implements `IAirflowSourceElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:40`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). Closest delivered shape: the Fan in its Electrical mode (`elements/Fan/FanElementDeclaration.cs:10`, `:46`–`:56`, `:72`–`:76`), without the hub hinge, the self-contained store, the signal latch and the shaft input. `KinematicMode.Static`, `SupportedDomains = Mechanical | Electrical`. Footprint: 1 body, 0 moving, 0 constraints.

### Body and colliders

One static body; local +Y is up and the base sits on the bench at y = 0. Dimensions in metres.

| Collider | Kind | Dimensions and place |
| --- | --- | --- |
| Blower base | box | 1.6 × 1.6 × 1.6, y 0 to 1.6 |
| Seat | Frustum, axis +Y | y 1.6 to 1.95; inner radius 0.10 at the bottom, 0.65 at the top; wall 0.05. Its throat is narrower than every ball's contact circle, so each ball rests on the cone, centred on the axis |
| Inlet chamber | back wall box 0.05 × 1.7 × 1.5 at x = +0.725 and two side wall boxes 1.5 × 1.7 × 0.05 at z = ±0.725, all y 1.6 to 3.3; a sill box 0.05 × 0.35 × 1.5 at x = −0.725, y 1.6 to 1.95 | the −X side above the sill is open and filled by the `Inlet` mouth: centre (−0.79, 2.60, 0), bore radius 0.65, a Tube stub (inner 0.65, outer 0.70, length 0.09) and a Torus collar ring as the bend's `Mesh_Collar` (inner radius 0.65, outer 0.78, `elements/PipeBend90/pipe_bend_90.tscn:36`–`:39`). The inlet's bore floor is at y 1.95, level with the seat's rim, so a ball rolls over the rim and down into the cone |
| Riser | Tube, axis +Y | inner 0.65, outer 0.70, y 3.3 to 4.3 |
| Outlet hood | back wall box at x = −0.725 and two side wall boxes at z = ±0.725, sized as the chamber's, y 4.3 to 5.7; a roof box 1.5 × 0.05 × 1.5, y 5.70 to 5.75; a deflector box 1.3 × 0.05 × 1.3 centred (0, 5.2, 0), pitched 45° with its high edge toward +X | the +X side is open and filled by the `Outlet` mouth: centre (0.79, 5.00, 0), with stub and collar as the inlet's |

The part lifts a ball 2.40 m, from a level pipe at 2.60 m to a level pipe at 5.00 m, and is 5.75 m tall. Mouth locators follow the bend (`elements/PipeBend90/pipe_bend_90.tscn:128`). Height is fixed (section 6).

Material for every collider: restitution 0.15, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s, the tube kit's (`elements/PipeBend90/PipeBend90ElementDeclaration.cs:35`).

### Airflow source

One `AirflowSourceDescriptor` (`CuriousContraptions.Abstractions/AirflowValues.cs:45`):

| Field | Value | Reason |
| --- | --- | --- |
| Frame, origin, axis | body 0, (0, 0.05, 0), +Y | the cone's apex is inside the base |
| Reach | 5.45 m | ends at y 5.5, 0.2 m below the hood roof, so a ball that has left by the outlet is no longer pushed |
| Terminal radius | 0.65 m | the bore radius; the cone stays inside the tube at every height |
| Flow speed | 12 m/s | the Fan's constant |
| Maximum force | parameter `force` | |
| Maximum power | `force` × 12 m/s | the Fan's product (`FanElementDeclaration.cs:50`–`:51`) |
| Internal occluders | the base box and the seat | the cone starts inside them; every other collider and every other body occludes |
| Input | one `ElectricalAirflowInput` (`AirflowValues.cs:37`) on supply consumer `consumer`, no selection, no enable latch | "supplied airflow": supply alone enables it |

**Seat and cone (hand calculation, repair of 22 November 2026, audit H3).** The engine's cone has radius `terminal radius × axial distance / reach` (`CuriousContraptions.Abstractions/ConicalAirflow.cs:9`) and tests each receiver sample along a line parallel to the axis (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:115`–`:121`). A ball's four samples sit half a radius from its centre and turn with it (`elements/Ball/BallElementDeclaration.cs:30`–`:33`), so all four are inside the cone in every orientation when the cone's radius at (centre height − r/2) is at least r/2.

| Ball | Radius | Rests on the cone at (contact circle r·cos 57.5°) | Centre height | Cone radius at centre − r/2 | Needed |
| --- | --- | --- | --- | --- | --- |
| Basketball | 0.34 | 0.183 m, y 1.653 | 1.939 | 0.65 × 1.719 / 5.45 = 0.205 | 0.170 |
| Bowling ball | 0.28 | 0.150 m, y 1.632 | 1.868 | 0.65 × 1.678 / 5.45 = 0.200 | 0.140 |
| Tennis ball | 0.25 | 0.134 m, y 1.622 | 1.833 | 0.65 × 1.658 / 5.45 = 0.198 | 0.125 |

The seat wall makes 57.5° with the axis (0.55 m of radius over 0.35 m of height), so a ball of radius r touches it on a circle of radius r·cos 57.5° and its centre is r·sin 57.5° above that circle. The draft's seat (inner radius 0.25 at the bottom) let a Basketball rest on the flat base with ±0.15 m of play, where the cone was only 0.184 m wide and two of the four samples could fall outside it: 15 N then delivered 7.5 N against 9.81 N of weight. This is why the base is 1.6 m tall.

### Parameters

| Name | Type | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `force` | Real | Newton | 15 | 0 | 40 | Reject | BuildOnly |

Range from the Fan (`FanElementDeclaration.cs:22`). Default 15 N lifts the 1 kg Basketball (9.81 N) and the 0.35 kg Tennis ball and leaves the 4 kg Bowling ball (39.2 N) on the seat.

### Port, supply and bindings

| `PortId` | Domain | Direction | Role | Local position |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | `PowerIn` | base face, (0, 0.5, −0.8), normal −Z |

Supply graph: one consumer with `SupplyContinuousWorkTarget(source 1)`, bound to `PowerIn`; electrical node 12 V, 0.3 Ω, 480 W (the Fan's, `FanElementDeclaration.cs:76`). No stores. Metadata: category `Power`, display name "Powered airlift", palette order 304 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.4, 0.72, 0.79). Presentation: `Visual/Rotor` turned by `PresentationDriver.CommittedFlowSpeed` with `PresentationCurve.IntegratedRate` (the Fan's blade binding, `FanElementDeclaration.cs:85`–`:88`).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| `Visual/Base` | dark slate `#263d4b` box with a Fan-blue `#66b8c9` band | solid, the base collider |
| `Visual/Seat` | cream `#fff8e9` cone | hollow, the Frustum |
| `Visual/Chamber`, `Visual/Hood`, `Visual/Deflector` | clear `#66b8c9` panels, alpha 0.16, cream frames | solid, one box each |
| `Visual/Riser`, stubs | clear tube | hollow, Tube colliders |
| `Visual/Collar_In`, `Visual/Collar_Out` | cream rings | solid, collar colliders |
| `Visual/Rotor` | four-blade rotor behind a grille drawn on the base face | decorative: enclosed, and moved by a binding |
| `Visual/Streaks` | pale `#a9e7e0` flow lines in the riser | decorative: glow |
| `Visual/Socket` | socket sphere on the base | solid, sphere collider |

Icon: a vertical tube on a block with an upward arrow and a ball. It differs from the Fan by the tube and the two side mouths, and from a pipe by the block.

### Behaviour and numbers

Each exposed sample receives `force × weight × (1 − v/12)` along +Y, where v is its upward speed (`ConicalAirflow.cs:19`); the source's total is capped by `force` and by power (`:30`), and only work actually delivered is paid from the supply. At 15 N the Basketball rises with about 5 m/s² at first, tops out near 4 m/s, meets the deflector and rolls out of the outlet; at 9 N it cannot leave the seat. Without supply nothing is emitted and nothing is drawn. A second ball on the seat is shadowed by the first until the first has left the column. A ball that drifts off the axis low in the riser leaves the narrow part of the cone, falls back to the seat and is re-centred.

## 3. Engine capabilities

Map row families: AerodynamicDrag, EnvironmentState, FiniteLedger, FiniteWorkActuation, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction (navigation only under D14; the part uses no gas store and no pressure work).

| Capability | Status | Evidence |
| --- | --- | --- |
| Conical airflow source with internal occluders | present | `AirflowValues.cs:45`; `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` (`CuriousContraptions.tests/FanIntegrationTests.cs:481`) |
| Electrical input that pays only exposed work | present | `AirflowValues.cs:37`; `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`FanIntegrationTests.cs:507`) |
| Ball receivers with distributed samples | present | `elements/Ball/BallElementDeclaration.cs:30`; `FanIntegrationTests.ExposedPayloadLifecyclePreservesPaidStateAndRestoresAuthoredInputs` (`FanIntegrationTests.cs:25`) |
| Flow-speed presentation binding | present | `FanIntegrationTests.DeclaredFlowBindingSamplesQueuedInteriorsAndStopsWithItsActualSource` (`FanIntegrationTests.cs:353`) |
| Tube and Frustum colliders, tube mouths | present | `HollowElementTests.ABallDroppedIntoTheFunnelsInletLeavesThroughItsThroatInsideTheWall` (`CuriousContraptions.tests/HollowElementTests.cs:437`); `TubeMouthSnapTests.APipeSnapsOntoTheEndMouthOfEveryTubePart` (`CuriousContraptions.tests/TubeMouthSnapTests.cs:184`) |
| `force` endpoints and refusal | present | `FanAdmissionTests.PackageScalarEndpointsAndClosedModeAreAdmittedAtomically` (`CuriousContraptions.tests/FanAdmissionTests.cs:267`) |
| A cone running **inside a hollow bore**, its lines clear of the tube wall | present, untested | the line test traces hollow walls (`WorkshopPhysicsEngine.Airflow.cs:141`–`:147`); no test places a source inside a Tube |
| A source with one input and **no selection or enable** | present, untested | both are nullable (`AirflowValues.cs:27`–`:37`); the Fan always declares three selected inputs |
| Sustained lift against gravity to a declared height | present, untested | Fan tests prove impulse and payment, not a vertical lift |

Nothing is missing. Stale harvest rows ("open airflow field: Story 12.2", "electrical supply: Story 8.1", "hollow tube collider: Stories 6.6–6.10") are superseded: all three are delivered.

## 4. Sources and legacy

- Requirement: "Supplied airflow lifts compatible cargo through a physical passage"; outcome "Insufficient airflow cannot lift the load" ([element-108](../requirements.md#element-108)). Map decision: S416/S470.
- The air-jet facts below match the [CAT-028 harvest](CAT-028-fan.md#appendix-historical-evidence) (its facts 23, 24, 27 and 29).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Fan parameters: force 0–40, reach (0, 12], width (0, 4], powered 0/1; non-finite rejected | `parts/FanPart.cs@a6c914e:L24-L31` | carry forward (force range) | Bounded field strength. |
| 2 | Catalogue defaults force 9, reach 5, width 0.85 | `parts/catalog/fan.tres@a6c914e:L14-L14` | do not carry forward as airlift defaults | Fan geometry, not a confined lift. |
| 3 | Field is a finite cylinder with an explicit conserved source and impedance; reference flow speed 12 m/s | `engine/AirflowNetwork.cs@a6c914e:L11-L13`, `engine/AirflowNetwork.cs@a6c914e:L24-L24` | carry forward | Same open-field family. |
| 4 | Bodies owned by the source or target are excluded; other solid bodies occlude the jet | `engine/AirflowNetwork.cs@a6c914e:L62-L74` | carry forward (occlusion), do not carry forward (per-step CPU network) | Per-element step loop is banned. |
| 5 | Accepted: jet acceleration is 6/mass for 1 kg and 4 kg; a blocker gives zero acceleration and releases no energy; kinetic energy ≤ released energy | `CuriousContraptions.tests/AirJetWorldTests.cs@a6c914e:L18-L66` | carry forward | Mass-scaled lift and the blocked control. |
| 6 | Accepted: a blocker moving through the jet cuts the force only while it overlaps the jet (receiver speed 0.4 m/s for one blocker, 0.3 m/s for a two-part blocker over 0.6 s); released energy equals 4 W × exposed time; only exposed work is committed and the rest is dissipated | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L13-L40` | carry forward (behaviour at substep resolution) | Exposed-only work. |
| 7 | Accepted: exposure and occlusion neither need nor create supply; an unpowered load has zero demand and changes no body; excluded bodies do not occlude | `CuriousContraptions.tests/AirJetGeometryTests.cs@a6c914e:L8-L32` | carry forward | Exposure never creates supply. |
| 8 | Accepted: a stationary blocker occludes a moving receiver only while its collider participates; a non-colliding blocker never occludes | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L48-L72` | carry forward | Non-colliding parts do not block. |
| 9 | Accepted: inlet, outlet and rim crossings in both directions switch exposure on and off; a source whose collider is disabled emits no field | `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L16-L45`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L126-L135` | carry forward (force starts and stops at the field boundary) | Field edges. |
| 10 | Continuous root-finding sweeps for boundary and occlusion times (tolerance 1e-7, tangency and initially-on-surface handling, sweep validation), and whole-world capture/restore replay | `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L41-L45`, `CuriousContraptions.tests/AirJetOcclusionTests.cs@a6c914e:L74-L90`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L102-L124`, `CuriousContraptions.tests/AirJetBoundaryTests.cs@a6c914e:L136-L143` | do not carry forward | CPU continuous-sweep solver path and rollback; the worker samples at 480 Hz substeps and never rolls back a tick. |
| 11 | Self-contained supply 14,400 J | `parts/FanPart.cs@a6c914e:L12-L16` | do not carry forward | The airlift is externally supplied. |

The current field is the delivered cone, not the legacy cylinder of row 3; rows 5–9 are proven for the Fan by its delivered tests and are not repeated here.

**Files harvested:**
- `parts/FanPart.cs`
- `parts/catalog/fan.tres`
- `parts/PipeBendPart.cs` (tube material)
- `engine/AirflowNetwork.cs`
- `CuriousContraptions.tests/AirJetWorldTests.cs`
- `CuriousContraptions.tests/AirJetOcclusionTests.cs`
- `CuriousContraptions.tests/AirJetGeometryTests.cs`
- `CuriousContraptions.tests/AirJetBoundaryTests.cs`
- Searched with no hit for `airlift` or `lift tube`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Requirement row: [element-108](../requirements.md#element-108). Planned test class `AirliftTests`. "Lifted" means the ball's centre passes x = 0.79 at a height above 4.3 m; "on the seat" means its height changes by less than 0.05 m.

**Supply used by every row.** Each row's Battery sets `power_limit` 240 W, so the Battery never bounds the airlift. The default 15 N × 12 m/s is 180 W rated; a default Battery (`power_limit` 120 W, admitted 10–480 W, `elements/Battery/BatteryElementDeclaration.cs:44`) bounds the source's rated power below that. This is a setting of the supply, not a rule of this part: the airlift's power cap stays `force` × 12 m/s, and whichever of the source's limit and the part's cap is lower bounds the delivered work.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a battery on `PowerIn` and a Basketball rolled into `Inlet`, when Run, then it is lifted and leaves by `Outlet` | `ASuppliedAirliftRaisesABasketballFromInletToOutlet` (theory `wired`) | unwired: the ball stays on the seat for 10 s | guard 15 s |
| A2 | Given `force` 9 N and 15 N, then the Basketball stays on the seat at 9 and is lifted at 15, whatever orientation it came to rest in (two entry spins) | `AirflowBelowTheBallsWeightCannotLiftIt` | A1 | the two values straddle the 9.81 N weight by more than 8 %; all four samples are inside the cone by the table in section 2 |
| A3 | Given a Bowling ball at the default force, then it stays on the seat | `ABowlingBallIsTooHeavyForTheDefaultAirflow` | a Tennis ball is lifted | 39.2 N against 15 N |
| A4 | Given a battery that empties while the ball is in the riser, then the ball falls back to the seat and the battery reads empty | `WhenTheSupplyRunsOutTheBallFallsBack` | a full battery completes A1 | the ball ends within 0.1 m of its seated height; guard 15 s |
| A5 | Given supply and no cargo for 5 s, then the battery has paid nothing | `AnEmptyColumnDrawsNoEnergy` | A1 pays a positive amount | intentional zero, asserted exactly on the decoded balance |
| A6 | Given a Basketball beside the riser at mid height, then it receives no force | `CargoOutsideTheTubeIsNotBlown` | the same ball inside the riser rises | sideways and upward speed gained under 0.05 m/s in 2 s |
| A7 | Given pipes snapped to both mouths, then a ball rolls in and out without a step | `BothMouthsJoinThePipeKit` | — | 0.02 m height change across a joint |
| A8 | `force` −1, 41 and NaN are refused; 0 and 40 are admitted | `ForceOutsideZeroToFortyIsRefused` | — | exact limits; S4 row 2 replaces it |
| A9 | A Signal output offered to `PowerIn` is refused | S4 row 5; until it lands `ASignalOutputCannotSupplyTheAirlift` | a battery `Supply` is accepted | typed refusal |
| A10 | Reset restores cargo and battery; save and load keep `force` and the wire and repeat A1 | `ResetAndSaveLoadRepeatTheLift` | — | generic rows: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`, S4 rows 1 and 3 |

### Integration

- Works with CAT-005 Battery, the switched supplies (Impact switch, latch, plate, timer), CAT-048/049/050 pipes and every delivered ball; all delivered.
- Campaign: coverage row "spiral delay, airlift and trapdoor variants", specialist routes 91–100, reuse 41–50, 111–120, 136–150 (`docs/planning/requirements.md:2398`). Owner decision 24e moves the first use to level **28c** in chapter 3 ([per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)).
- Coverage binding: a final package-source-binding section in this spec (Package, Named identity, Acceptance, Decision, Mode records), as EL-239 on published main; blocked on S7 (section 7).

### Delivery

- **Shape.** Single element slice. Not classified in delivery batches; it belongs in 1b beside "Already-built behaviour" as a Fan-capability reuse.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES (the airflow source's receiver and occluder candidates) and ENGINE-SUPPLY-DEMAND (the supply debit, sprint key `engine-supply-drive-demand`); then S7 (the coverage follow-on, which widens the reader, owner decision 15). S2–S4 optional.
- **Files.** `elements/Airlift/` (project, declaration, `airlift.tscn`, `icon.svg`); `CuriousContraptions.tests/AirliftTests.cs` (compiled by the test project's one `*.cs` rule); one `CuriousContraptions.slnx` line; the id in the tube-part rows of `TubeMouthSnapTests`; the binding section of section 7 once S7 admits it; until S3, the trim list and placement switch.
- **Review tier.** Engine tier: three *present, untested* rows.
- **Disqualifiers the probe re-checks.** A seated ball whose samples fall outside the cone, so the lift depends on the ball's orientation (then raise the base or shorten the reach; numbers only); the tube wall occluding the column; a ball oscillating under the deflector instead of leaving; an unselected single input refused by the bundle.

## 6. Open questions

All resolved.

1. **Variants.** One identity; "airlift … variants" in the campaign row are not separate parts (D8).
2. **Supply is external electrical only.** The row says "supplied airflow"; the Fan's self-contained store is not carried.
3. **Compatibility is bore and weight.** Any ball that fits the bore (radius 0.65 m) and weighs less than the force is lifted. The Balloon is not a target: it does not fit a tube mouth usefully and no lesson asks for it (D19).
4. **No field outside the tube.** The cone ends inside the hood and stays within the bore; acceptance A6 proves it.
5. **`height` parameter dropped** (D13): a longer riser narrows the cone at the seat, so height is not a free number.
6. **Default force 15 N and the dimensions**: starting values under D1.
7. **Seat throat 0.10 m, base 1.6 m, reach 5.45 m** (22 November 2026, audit H3): the hand table in section 2. The inlet floor is level with the seat's rim; the draft's rim stood 0.2 m above the inlet floor, a step a rolling ball could not climb.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One static body, one declared airflow source with an electrical input, one supply consumer, one parameter and one presentation binding. No solver branch, callback or engine test on this package.
2. **Generic laws.** The conical airflow law and its paid-work supply rule (`ConicalAirflow`, the same descriptors the Fan declares), contact, friction and gravity. "Too heavy to lift" is force against weight, not a rule about ball kinds.
3. **Three layers.** Colliders: seventeen hidden proxies on the static body (boxes 10: base, three chamber walls and a sill, three hood walls, roof, deflector; Frustum 1; Tube 3; Torus collar rings 2; socket sphere 1). Visuals: base, clear panels, riser, collars, drawn grille; none is a collider, and each visible solid has its proxy by the art table in section 2. Animation: `Visual/Rotor` is turned by a `CommittedFlowSpeed` binding and `Visual/Streaks` is a glow; both are enclosed decoration that reads committed state and writes nothing back.
4. **Flow and typing.** `PowerIn` is a typed Electrical input; `force` is an f32 Real in newtons; nothing flows from presentation to physics.
5. **Flat state.** Bodies 1 (static); colliders 17; joints 0; drives 0; airflow sources 1; sensors 0; domain nodes 1 (electrical consumer); stores 0; presentation bindings 1.
6. **Performance.** Seventeen colliders against the Fan's handful. Reason: the chamber and hood are open-sided enclosures and the admitted shape set has no box with a circular opening, so each is built from wall boxes; all are static and none is cosmetic. The airflow region is the bounded declared cone, sampled only at declared receiver samples. Today the airflow pass sweeps every receiver sample against every collider; its candidates come from ENGINE-SPATIAL-QUERIES, which bounds the work by the bodies inside the cone. No island sleeping exists today; a resting ball on the unsupplied seat costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
7. **Tests are scenarios.** Rows A1 to A10 run the production simulation; none needs a code path of this part.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `airlift` |
| Named identity | EL-108, `../invest/named-elements.md#element-108` |
| Acceptance | `todo-248`; no sequence task yet |
| Decision | S480, `../invest/scope-corrections.md#s480` |
| Mode records | `Configuration=Adjustable` (`force`) |

What it lacks today: `scope-corrections.md` has no `s480` block on published main; `todo-248` is a scope index shared by EL-107, EL-108 and EL-109 with no sequence-task anchor directly after it, which the reader requires; and one task source can bind only one package.
