# EL-042 · Air nozzle — element readiness spec

**Current delivery (24 October 2026):** [EL-042 delivery slice](../invest/named-elements.md#element-042). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against `8d1d68aa`. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations of the form `path:line` are this checkout. Values marked *proposed* are starting values (decision D1). Box sizes are full extents. Shared sealed-gas facts P1–P20, the family contract F1–F11 and the capability rows G1–G14 are in [EL-039](EL-039-air-reservoir.md). Repaired on 22 November 2026 against the Batch H audit.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-042 · Air nozzle |
| Type | Pneumatic |
| Anchor | [requirements.md#element-042](../requirements.md#element-042); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-042); owner S477 |
| Related | Refines no CAT spec. The sealed-gas counterpart of the open-airflow emitters [CAT-028 Fan](CAT-028-fan.md) and [CAT-010 Bellows](CAT-010-bellows.md); fed by [EL-039](EL-039-air-reservoir.md) via [EL-040](EL-040-pneumatic-release-valve.md) and [EL-038](EL-038-pneumatic-hose.md); can drive [CAT-070 Windmill](../requirements.md#current-cat-070) and [CAT-069 Wind chimes](CAT-069-wind_chimes.md). The same nozzle law serves the [TH-17 Kettle](TH-17-kettle.md) spout and the [EL-092 Toy rocket](EL-092-toy-rocket.md). |
| Roadmap story | [EL-042 delivery slice](../invest/named-elements.md#element-042); canonical proof owner and prerequisite decisions are recorded there. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (airflow receiver and occluder candidates for the bounded cone; it deletes the scene-wide airflow scan) and `ENGINE-CAPABILITY-INSTANCES` (gas paths and airflow sources as declared lists with local ids); then `ENGINE-GAS-NETWORK` with the gas-fed airflow source and the nozzle law (owner decision 18). |
| Disposition | To deliver as a single element. Not a duplicate of the Fan: its source is a finite gas store and its strength follows pressure. |
| Planned package | id `air_nozzle`, directory `elements/AirNozzle/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |

## 2. Declaration

Closest delivered shape: the Fan's airflow source (`elements/Fan/FanElementDeclaration.cs:46`), with a gas store in place of its energy store.

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IAirflowSourceElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:40`) and the fluid port contract (G5).
- **Bodies and colliders.** One static scene body, mass 0. Mount box 0.3 × 0.35 × 0.35 m at (−0.15, 0, 0) *proposed*; barrel cylinder r 0.12 m, length 0.38 m, axis X, centre (0.19, 0, 0) *proposed* (a small fitting, sized for picking). A static mount takes the jet reaction; there is no recoil.
- **Material.** Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s.
- **Airflow source.** Local id 1, frame body 0, origin (0.38, 0, 0), axis +X, terminal radius from `width`, flow speed fixed 12 m/s (the family reference speed, as the Fan). Maximum force is min(T, 40 N), where T is the committed nozzle thrust and 40 N is the upper bound of the delivered Fan's `force` (`elements/Fan/FanElementDeclaration.cs:22`). Maximum power is that force × 12 m/s, so at most 480 W. Reach is `reach` × min(1, T / 20 N). Internal occluders: its own two colliders. Its input is the gas arriving at `GasIn`.
- **Stores.** None. The throat is a Vent boundary (F4): whenever the upstream pressure exceeds ambient, gas leaves. Control is upstream, at [EL-040](EL-040-pneumatic-release-valve.md).

| Parameter | Type | Unit | Default | Minimum | Maximum | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `throat_area` | Real f32 | m² | 5 × 10⁻⁵ | 1 × 10⁻⁵ | 2 × 10⁻⁴ | Reject; build only; player-editable unless the level locks it (D11) |
| `reach` | Real f32 | m | 4 | 0.5 | 8 | Reject; build only |
| `width` | Real f32 | m | 0.85 | 0.1 | 1 | Reject; build only |

| `PortId` | Role | Domain | Direction | Local position, normal |
| --- | --- | --- | --- | --- |
| `GasIn` | Bidirectional (fluid port, L5) | Fluid | Input (link-joined) | (−0.35, 0, 0), −X |

`GasIn` is a fluid port of the one record ([EL-039](EL-039-air-reservoir.md) F3; liquid L5): role Bidirectional, so flow follows pressure, and Input is only its link-joined socket's authoring end. `GasIn` is a new `PortId` member (`ENGINE-GAS-NETWORK`); no `PortRole` member is added for gas. The port declares no aperture area: the hose edge's throat is the connection kind's (EL-039 F10), and `throat_area` is the throat of the nozzle's declared Vent boundary (F4).

- **Presentation bindings.** `Visual/Puff`, `Visible` and `UniformScale`, driven by the committed mass rate; hidden at NoFlow (driver missing, G9). The selected-part readout shows the regime: NoFlow, Subsonic or Choked.
- **Palette.** `ElementCategory.Power`, palette order 332 (F8; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour gold `#f7cb52`. Title "Air nozzle". Description: "Turns stored air into a short directed jet. Weak pressure only reaches nearby objects."

### Art and solidity

| Node | Mesh and size | Class | Reason |
| --- | --- | --- | --- |
| `Visual/Mount` | Box 0.3 × 0.35 × 0.35 m, navy `#293954` | solid | its own box collider |
| `Visual/Barrel` | Cylinder r 0.12 m, length 0.38 m, gold `#f7cb52` | solid | its own cylinder collider |
| `Visual/Collar` | Cream `#fff8e9` band, flush on the barrel | decorative | printed flat |
| `Visual/Mouth` | Navy disc on the barrel end, flush | decorative | printed flat |
| `Visual/Puff` | Soft cream sprite beyond the mouth | decorative | glow, driven by a binding |
| `Loc_Port_GasIn` | Locator | none | not rendered |

Icon: a short barrel on a block with three jet lines. It differs from the Fan (a round guard with blades) by having no blades or ring, and from the release valve by its barrel outline.

### Behaviour and numbers

- **Law.** Converging nozzle (P6, oracle P20): mass rate, exit state and thrust T = momentum rate + pressure force, from the committed upstream state; NoFlow when upstream is at or below ambient. The jet's kinetic energy never exceeds the discharged enthalpy (P7): at the default, T × 12 m/s is 244 W against an enthalpy rate near 13.9 kW.
- **Force on a load.** The delivered cone law, unchanged (`ConicalAirflow.Contains` and `ConicalAirflow.Impulse`, `CuriousContraptions.Abstractions/ConicalAirflow.cs:9`, `:19`): a receiver inside the cone takes T × sample weight × slip, and one outside takes nothing. There is no second decay law.
- **Why reach follows thrust.** The outcome is "Insufficient pressure cannot move a distant load". With reach = `reach` × min(1, T / 20 N), a weak supply shortens the cone, so the result is zero force outside it and does not depend on rolling resistance holding a ball still. 20 N is the default peak thrust below.

**Peak thrust derivation** (Air γ 1.40, R 287 J/(kg·K), Cp 1005 J/(kg·K), stagnation at the ambient temperature of F2 (288 K), back pressure 101 325 Pa, `throat_area` 5 × 10⁻⁵ m²; P6 law). Thrust and exit pressure do not depend on the stagnation temperature; mass rate scales with its inverse square root:

| Upstream | Regime | Exit state | Mass rate | Thrust T | Effective reach (`reach` 4 m) |
| --- | --- | --- | --- | --- | --- |
| 400 000 Pa | Choked (critical upstream 191 800 Pa) | 211 320 Pa, 240.0 K, 310.5 m/s | 0.0476 kg/s | 14.79 N momentum + 5.50 N pressure = 20.3 N | 4.0 m |
| 150 000 Pa | Subsonic | 101 325 Pa, 257.5 K, 247.7 m/s, 1.371 kg/m³ | 0.0170 kg/s | 4.21 N momentum + 0 pressure = 4.2 N | 0.84 m |

These are peak values at valve opening; thrust and reach only fall as the tank drains (P17 d). A default 0.0484 kg charge lasts about a second. Across the parameter range thrust runs from about 4 N to about 130 N (throat 2 × 10⁻⁴ m² on a 600 000 Pa supply), which is why the force on loads is capped at 40 N: an uncapped 130 N on a 1 kg Ball is far outside the scale of every delivered airflow part.

- **Consistency.** `width` 0.85 m is the Fan's default terminal radius (`elements/Fan/FanElementDeclaration.cs:24`), so at 1 m of a 4 m reach the cone radius is 0.21 m and covers the centre samples of a Ball (`elements/Ball/BallElementDeclaration.cs:30`). `reach` stays inside the Fan's 12 m bound (`:23`). The Fan's default force is 9 N (`:22`); the nozzle's 20 N burst is stronger but lasts a second.
- **Tick order.** No signal read. Thrust for a tick comes from the upstream state committed in the previous gas step (F5).

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S477): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction). Under D14 the row is navigation; this probe (21 November 2026, `8d1d68aa`) is the authority. Evidence for G rows is in EL-039 section 3.

| Capability | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body with baked colliders (G1) | present | EL-039 G1 |
| Conical airflow source: cone bounds, occlusion, mass-dependent transfer | present | `AirflowSourceDescriptor` (`CuriousContraptions.Abstractions/AirflowValues.cs:45`); `AirflowRuntimeTests.FiniteConeTransfersRatedForceAccordingToMass` (`CuriousContraptions.tests/AirflowRuntimeTests.cs:52`); `ConicalAirflowTests.ConeHasOpenAxialAndRadialBoundaries` (`CuriousContraptions.tests/ConicalAirflowTests.cs:9`); `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` (`CuriousContraptions.tests/FanIntegrationTests.cs:481`) |
| Rotary capture of any airflow source (Windmill partner) | present | `IRotaryAirflowCaptureElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:55`); `WindmillIntegrationTests.RotorTravelEventPaidMotionAndLifecycleUseCommittedPhysicalState` (`CuriousContraptions.tests/WindmillIntegrationTests.cs:13`) |
| Wrong-kind refusal; authored-state Reset and save/load (G12) | present | EL-039 G12 |
| Fluid port contract (G5) | present, untested | EL-039 G5 |
| What may feed an airflow source | narrower | Inputs are an energy store, an electrical consumer or a shaft coordinate only (`CuriousContraptions.Abstractions/AirflowValues.cs:28`–`:30`). A gas store is not an input. `Reach` and `MaximumForce` are `MechanicalValue`s built from constants and parameters (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:166`), so neither can follow a committed thrust. Owner: `ENGINE-GAS-NETWORK`; its text does not name this, gap recorded in the batch summary |
| Gas outlet law | narrower | The delivered outlet is conductance × pressure difference with no thrust (`IsobaricGas.Discharge`, `CuriousContraptions.Abstractions/IsobaricGas.cs:56`; `GasRuntimeTests.FiniteOutflowChangesOneMassAndShapeWithoutThrustOrChangingDrySpinInertia`, `CuriousContraptions.tests/GasRuntimeTests.cs:151`). The converging-nozzle regimes and thrust of P6 are not in the tree. Owner: `ENGINE-GAS-NETWORK`; gap recorded |
| Gas connection kind and the gas `PortId` members (G6; role Bidirectional, no new `PortRole` member, EL-039 F3); lines (G7) | missing | `ENGINE-GAS-NETWORK` |
| Presentation from committed mass rate and regime (G9) | missing | `ENGINE-GAS-NETWORK` |
| Unit for m² (G11) | missing | `ENGINE-GAS-NETWORK` |

Counts: 4 present, 1 present but untested, 2 narrower, 3 missing. Nothing here needs element-specific engine code. Bellows: no `elements/Bellows` package and no `spec-12-4-pneumatic-bellows.md` exist in this checkout; Story 12.4 owns the bellows nozzle, and whatever nozzle record it delivers is reused here, not rebuilt.

**Dependencies.** EL-039, EL-040 and EL-038 upstream; a Ball or Tennis ball target (delivered).

### Engine conformance (22 November 2026)

Checked against the owner's conformance instruction of 22 November 2026 and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One static body, two colliders, one airflow source, one fluid port, one declared gas path to ambient, three parameters and two presentation bindings. No script, callback or loop; the engine may not branch on `air_nozzle`.
2. **Generic laws.** Two general laws compose: the converging-nozzle law gives mass rate and thrust from the committed upstream state (P6), and the delivered cone law applies a force to receivers inside a bounded cone. "Insufficient pressure cannot move a distant load" is the consequence of reach declared as a function of committed thrust: a receiver outside the cone takes nothing. The capability this needs, an airflow source fed by a gas path with force and reach as declared functions of committed thrust, is stated element-free and would serve any gas jet (the Kettle spout TH-17 uses the same nozzle law).
3. **Three layers.** *Colliders:* `col_` mount box and barrel cylinder on body 0. *Visuals:* the art table above; mount, barrel, collar, mouth and locator are moved by nothing. *Animation:* `Puff` visibility and scale from the committed mass rate; read-only, off the physics thread.
4. **One-way flow and typing.** Gas state → airflow source → receiver impulses inside the tick phases; committed state → animation → renderer. The regime is an enum; areas, forces and pressures are f32.
5. **Flat-state counts per instance.** Bodies 1 (static); colliders 2 (1 box, 1 cylinder); joints 0; drives 0; sensors 0; field regions 1 (the cone, bounded by `reach` ≤ 8 m and `width` ≤ 1 m); domain nodes 1 gas path row; ports 1; stores 0; bindings 2; parameters 3.
6. **Performance fitness.** Two primitive colliders, fewer than the Battery's three. Static body, so it joins no island. The cone is a bounded volume. **Engine gap, owned:** the delivered airflow pass tests every source against every receiver sample and sweeps every collider for occlusion (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:75`–`:158`), which is work proportional to the scene. `ENGINE-SPATIAL-QUERIES` owns the repair (owner decision 3a; engine design audit §6 item 2 deletes the airflow scan and names this part): receiver and occluder candidates for a source come from a query of its bounded cone, so work per tick is proportional to the bodies inside cones. This spec does not design around it.
7. **Tests are scenarios.** N-A1 to N-A10 author constructions and assert committed travel, mass rate, thrust, pressures and refusals on the production simulation.

## 4. Sources and legacy

- Requirement row [element-042](../requirements.md#element-042): "Expands supplied gas into a bounded directed jet"; outcome "Insufficient pressure cannot move a distant load". No variants listed.
- Named entry [element-042](../invest/named-elements.md#element-042), owner S477. Gas design: "Nozzle source | throat area, direction | Converts stored pressure into a jet that moves cargo | Exhaust puff ← committed regime"; nozzle law row "NoFlow, Subsonic and Choked regimes with mass continuity, stagnation enthalpy, momentum and pressure thrust".

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| N1 | Converging nozzle law and regimes; forward flow only; thrust = momentum + pressure force. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L87` | carry forward (EL-039 P6) |
| N2 | Airflow emitter = finite cylindrical field (position, direction, reach, width) with a conserved source and impedance material; sample weights must be positive and sum to one. | `engine/AirflowNetwork.cs@a6c914e:L11-L13`, `engine/AirflowNetwork.cs@a6c914e:L45-L46` | superseded by the delivered conical source; do not carry forward the per-step CPU network loop |
| N3 | Reference flow speed 12 m/s; fan force 0–40 N, reach (0, 12] m, width (0, 4] m. | `engine/AirflowNetwork.cs@a6c914e:L24-L24`, `parts/FanPart.cs@a6c914e:L28-L29` | carry forward as scale bounds; they match the delivered Fan |
| N4 | Nozzle oracle (fixture gas R 200, Cv 400; source 120 000 Pa, 300 K; area 0.01 m²): back 30 000 Pa is choked with exit 61 440 Pa, 240 K and √72 000 ≈ 268.3 m/s; the same mass rate and exit speed at vacuum, with larger thrust; doubling the area doubles mass and enthalpy rates. Same row as [TH-17 Kettle](TH-17-kettle.md) harvest row 6. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L31-L47` | carry forward as an acceptance oracle for this nozzle |
| N5 | A closed area or balanced pressure gives NoFlow with zero mass rate, enthalpy rate and thrust. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74` | carry forward (closed valve / empty tank control) |

- **Files harvested:** `engine/AirflowNetwork.cs` (emitter shape and constants), `parts/FanPart.cs` (bounds), `CuriousContraptions.tests/ConvergingGasNozzleTests.cs` (N4–N5); gas files as listed in EL-039.

## 5. Acceptance outline

Acceptance authority: [element-042](../requirements.md#element-042), [pneumatics profile](../invest/profiles.md#pneumatics). Pressure and mass tolerances and stall guards are EL-039 F6. Planned test class `AirNozzleTests`. The rig is tank → hose → release valve → hose → nozzle, aimed along a level floor at a resting Ball whose centre is on the jet axis. "Moves" means the ball travels more than 0.5 m along the axis within 10 s; "stays" means less than 0.02 m (a ball radius is 0.34 m, so neither can be mistaken for the other).

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| N-A1 | Given a 400 000 Pa tank and the ball 1 m from the exit, when the valve is triggered, then the ball moves | `AirNozzleTests.AChargedTankMovesANearBall` | N-A2 | — |
| N-A2 | Requirement outcome: given a 150 000 Pa tank and the same ball at 1 m (effective reach 0.84 m), then the ball stays | `AirNozzleTests.InsufficientPressureCannotMoveADistantLoad` | N-A1 is the same distance at 400 000 Pa | the 150 000 Pa ball at 0.3 m travels more than 0.1 m (the weak jet delivers about 0.4 N·s, which carries a 1 kg Ball about 0.2 m, so the 0.5 m "moves" bar does not apply to this case) |
| N-A3 | Given a 400 000 Pa tank and the ball 4.5 m away, behind a Wall, or with the nozzle turned away, then the ball stays | `AirNozzleTests.OutOfReachOccludedOrMisaimedLoadsStay` | N-A1 | `reach` at 0.5 m and 8 m |
| N-A4 | Given an ambient tank, or a valve never triggered, then the ball stays and the tank mass is unchanged | `AirNozzleTests.NoSupplyGivesNoJet` | N-A1 | upstream exactly ambient |
| N-A5 | Given the N-A1 run, then the tank ends at ambient and no longer pushes the ball; a nozzle hosed directly to a tank starts venting at Run | `AirNozzleTests.TheJetSpendsTheTankAndStops` | — | — |
| N-A6 | Given a nozzle hosed directly to a 400 000 Pa tank and, separately, to a 150 000 Pa tank (no valve; the hose starts at tank pressure, F9), then the first committed mass rate and thrust are 0.0476 kg/s and 20.3 N, and 0.0170 kg/s and 4.2 N, each within 5% (the two cases differ fivefold, so 5% cannot swap them) | `AirNozzleTests.ThrustAndMassRateFollowTheNozzleLaw` | equal pressures give zero | `throat_area` limits: doubling the area doubles the rate; at 2 × 10⁻⁴ m² on a 600 000 Pa tank the force on a receiver does not exceed 40 N |
| N-A7 | Given a Windmill in the jet, then its rotor turns while the tank lasts and coasts down after | `AirNozzleTests.AJetTurnsAWindmillWhileTheTankLasts` | no trigger: the rotor never turns | — |
| N-A8 | Given a run, when Reset is pressed, then the tank, valve and ball are at their authored state | `AirNozzleTests.ResetRestoresTankValveAndBall` | — | — |
| N-A9 | Given non-default parameters, a rotated pose and the gas link, when saved and loaded, then they are equal | `AirNozzleTests.SaveLoadKeepsParametersPoseAndLink` | — | every limit |
| N-A10 | Given a parameter outside its limits or non-finite, or a wire offered to `GasIn`, then it is refused with a typed reason | `AirNozzleTests.InvalidParametersAndWrongKindLinksAreRefused` | limits and the hose are admitted | next f32 beyond each limit |

Generic cover today: G12 for authored-state Reset and save/load; N-A8 to N-A10 stay package tests until the S4 conformance suite is delivered.

**Chrome recipe (pending the post-catalogue browser phase).** Charged Air tank → hose → Release valve → hose → Air nozzle aimed with the rotate gizmo at a Basketball on a ledge, Receiver below; a Switch triggers the valve. Ball positions are set with the move gizmo, never a numeric menu.

**Integrations.** Pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393) and the [todo-378](../requirements.md#todo-378) combinations; interaction processes [IX-08 fluid advection](../requirements.md#interaction-08), [IX-09 pressure work](../requirements.md#interaction-09), [IX-40 gas state evolution](../requirements.md#interaction-40) and [IX-11 aerodynamic drag](../requirements.md#interaction-11).

## 6. Open questions

1. **Jet decay law.** Resolved under the standing delegation: the quadratic distance factor is withdrawn. The jet uses the delivered cone law and its reach scales with thrust. Reason: a second force law for airflow would be a second rule for the same behaviour, and the cone already gives a clean in or out result.
2. **Mount recoil.** Resolved (D19): static mount only. Reaction thrust on a moving body is [EL-092 Toy rocket](EL-092-toy-rocket.md).
3. **Static hold.** Resolved by question 1: the weak-pressure control now places the ball outside the cone, so it no longer relies on rolling resistance at zero speed.
4. **Force cap and maximum power.** Resolved by coordinator decision under the owner's standing delegation (22 November 2026): the airflow force is min(T, 40 N) and the source's maximum power is that force × 12 m/s. Reason: the delivered airflow source needs a declared maximum power, and 40 N is the delivered Fan's upper bound, so the nozzle stays inside the scale the airflow law is already proven on. Reported thrust (N-A6) is uncapped.

Needs the owner: none.

## 7. Integration

- **Works with.** EL-039, EL-040, EL-038 (not delivered); Ball, Tennis ball, Balloon, Windmill and Wall (delivered); Wind chimes CAT-069 (not delivered). [EL-045 Air whistle](EL-045-air-whistle.md) is fed by gas through its own `GasIn`, not by this jet.
- **Port agreement.** `GasIn` is a Fluid input (F3).
- **Campaign.** The pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) did not name the nozzle, so owner decision 24c gives it new level **73a** *Jet Set* (chapter 8): introduction 73a, practice 75, reuse 99 and 143 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)). A level that places it needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7**, which also widens the reader for companion packages and non-`EL` identities (owner decision 15): the published form's preconditions are not met. This identity lacks its own `todo-NNN` and `sequence-task-NNN` pair co-anchored in `requirements.md` (the ledger entry links only the umbrella `todo-376`, which has no co-anchored sequence task and is shared by eight identities; the reader refuses two identities on one task source) and an `<a id="s477">` anchor for its ledger owner S477 in `docs/planning/invest/scope-corrections.md`.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` and `ENGINE-CAPABILITY-INSTANCES`; then `ENGINE-GAS-NETWORK` including the gas-fed airflow source and the converging-nozzle law, which the engine design audit (§5 "Nozzle", §6 item 13) gives to that story rather than to a separate nozzle slice under Story 12.4; EL-039; EL-038 acceptance; EL-040; S7.
- **Files.** Adds `elements/AirNozzle/AirNozzle.csproj`, `AirNozzleElementDeclaration.cs`, `air_nozzle.tscn`, `icon.svg` and `CuriousContraptions.tests/AirNozzleTests.cs` (compiled by the test project's one rule; no compile line). Edits one line in `CuriousContraptions.slnx` and this spec; the per-package lists S3 owns until S3 lands.
- **Review tier.** Data-only, if the probe finds every narrower and missing row delivered.
- **Disqualifiers.** An airflow source that cannot take a gas input; reach or force that cannot follow committed thrust; a Ball whose receiver samples fall outside a 0.21 m cone radius at 1 m (then `width` is retuned before planning). The first two return the work to `ENGINE-GAS-NETWORK`.
