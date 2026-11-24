# EL-037 · Air compressor — element readiness spec

**Current delivery (24 October 2026):** [EL-037 delivery slice](../invest/named-elements.md#element-037). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against `8d1d68aa`. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations of the form `path:line` are this checkout. Values marked *proposed* are starting values (decision D1). Box sizes are full extents. Shared sealed-gas facts P1–P20, the family contract F1–F11 and the capability rows G1–G14 are in [EL-039](EL-039-air-reservoir.md). Repaired on 22 November 2026 against the Batch H audit.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-037 · Air compressor |
| Type | Pneumatic |
| Anchor | [requirements.md#element-037](../requirements.md#element-037); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-037); owner S472 |
| Related | Refines no CAT spec. Electrical supply from [CAT-005 Battery](CAT-005-battery.md) / [EL-196](EL-196-battery.md) through [EL-197 wire](EL-197-electrical-wire.md); shaft supply from [CAT-042 Motor](CAT-042-motor.md); charges [EL-039 Air reservoir](EL-039-air-reservoir.md) through [EL-038 hose](EL-038-pneumatic-hose.md). |
| Roadmap story | [EL-037 delivery slice](../invest/named-elements.md#element-037); canonical proof owner and prerequisite decisions are recorded there. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`: the debit of a work target's granted energy, and the Motor's drain in Shaft mode) and `ENGINE-ISLANDS-SLEEP` (the pulley is a dynamic body); then `ENGINE-GAS-NETWORK` (owner decision 18). |
| Disposition | To deliver as a single element. Electrical and Shaft supply are one `supply` choice on one catalogue entry (D8), as on the Fan; the earlier separate `shaft_compressor` entry is withdrawn. |
| Planned package | id `air_compressor`, directory `elements/AirCompressor/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |

## 2. Declaration

Closest delivered shape: the Fan in its Electrical and Shaft modes (`elements/Fan/FanElementDeclaration.cs:14`, `:54`–`:55`, `:57`–`:77`), with compression into a gas line in place of airflow.

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`) for the input pulley, `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`) and the fluid port contract (G5).
- **Bodies and colliders.** Static housing box 1.2 × 0.8 × 0.7 m at (0, 0.15, 0) *proposed* (Counter scale; a Ball cannot pass through it); foot box 1.3 × 0.16 × 0.85 m at (0, −0.33, 0) *proposed*. One hinged pulley body: cylinder r 0.14 m, 0.08 m thick, axis X, at (−0.7, 0.15, 0), mass 0.15 kg, inertia 0.00147 kg·m² about its axis (½·m·r², a solid disc) and 0.00082 kg·m² across it, continuous travel; the hinge is declared in the shape of the Fan's hub (`elements/Fan/FanElementDeclaration.cs:36`–`:41`).
- **Material.** Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s.
- **Work conversion.** No store of its own. Each tick it accepts work W of at most `power_rating` × dt from the selected supply and delivers mass dm = η·W / w into `GasOut`, with w = Cp·T_amb·[(r)^((γ−1)/γ) − 1] and r = max(p_out, 1.05·p_amb) / p_amb. η = 0.7 *proposed* (a small reciprocating compressor; the other 30% is heat that is not tracked). The 1.05 floor *proposed* keeps the first tick's mass finite when the outlet is still at ambient. At p_out ≥ `rated_pressure`, or with nothing joined to `GasOut`, it accepts no work.
- **Outlet.** `GasOut` is non-return: gas never flows back through the compressor. Intake is the ambient boundary (F2), drawn as a grille, not a port.
- **Shaft load law (Shaft mode).** While it can deliver, the compressor resists its pulley with a torque τ = min(`power_rating` / |ω|, 15 N·m) opposing the rotation, and accepts the work τ·|ω|·dt, which never exceeds `power_rating`·dt. The 15 N·m cap *proposed* is below the default Motor's 20 N·m (`elements/Motor/MotorElementDeclaration.cs:44`), so a default Motor is loaded but never stalled. When it cannot deliver (rating reached, nothing joined) the torque is zero. In Electrical mode the pulley turns freely.
- **Electrical node.** One consumer, 12 V, maximum 240 W (the upper limit of `power_rating`), in the descriptor shape of the Fan's consumer (`elements/Fan/FanElementDeclaration.cs:76`, which declares 480 W).

| Parameter | Type | Unit | Default | Minimum | Maximum | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `supply` | Choice | none | 0 (Electrical) | 0 | 1 (Shaft) | Reject; build only |
| `power_rating` | Real f32 | W | 60 | 10 | 240 | Reject; build only; player-editable unless the level locks it (D11) |
| `rated_pressure` | Real f32 | Pa absolute | 600 000 | 150 000 | 600 000 | Reject; build only |

| `PortId` | Role | Domain | Direction | Local position, normal | Available |
| --- | --- | --- | --- | --- | --- |
| `PowerIn` | `PortRole.PowerIn` | Electrical | Input | (−0.66, −0.1, 0), −X | `supply` = Electrical |
| `DriveIn` | `PortRole.DriveIn` | Mechanical | Input | (−0.7, 0.15, 0), −X; revolute coordinate 0, rim radius 0.14 m | `supply` = Shaft |
| `GasOut` | Bidirectional (fluid port, L5) | Fluid | Output (link-joined) | (0.66, 0.15, 0), +X | always |

`GasOut` is a fluid port of the one record ([EL-039](EL-039-air-reservoir.md) F3; liquid L5): role Bidirectional, so flow follows pressure, and Output is only its link-joined socket's authoring end. `GasOut` is a new `PortId` member (`ENGINE-GAS-NETWORK`); no `PortRole` member is added for gas. The port declares no aperture area: a hose edge's throat is the connection kind's (EL-039 F10).

- **Presentation bindings.** `Visual/Piston`, `LocalY`, amplitude 0.12 m, one cycle per 0.0002 kg of committed delivered mass (5000 cycles per kg: about two strokes a second while charging near 300 000 Pa at 60 W), drawn at no more than 8 cycles per second (driver missing, G9). `Visual/Lamp`, albedo slate `#556573` to gold `#f7cb52`, driver `CommittedSupply`, Electrical mode. `Visual/Pulley`, `LocalRotationZ`, driver `CommittedRevoluteAngle`, as the Fan's blades in Shaft mode (`elements/Fan/FanElementDeclaration.cs:80`).
- **Palette.** `ElementCategory.Power`, palette order 333 (F8; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour cyan `#66b8c9`. Title "Air compressor". Description: "Uses supplied work to pump outside air into a connected tank. Without supply it stops; it never adds air on its own."

### Art and solidity

| Node | Mesh and size | Class | Reason |
| --- | --- | --- | --- |
| `Visual/Housing` | Box 1.2 × 0.8 × 0.7 m, cyan `#66b8c9` | solid | its own box collider |
| `Visual/Foot` | Box 1.3 × 0.16 × 0.85 m, navy `#293954` | solid | its own box collider |
| `Visual/Pulley` | Cylinder r 0.14 m, 0.08 m thick, cream `#fff8e9` | solid | its own cylinder collider on the hinged body |
| `Visual/Fins` | Navy stripes on the housing top, flush | decorative | printed flat (the earlier raised fins are redrawn flush) |
| `Visual/Grille` | Navy slots on the rear face, flush | decorative | printed flat |
| `Visual/Piston` | Cream block in a recessed window | decorative | moved by a presentation binding |
| `Visual/Lamp` | Flush disc | decorative | printed flat, glow |
| `Loc_Port_PowerIn`, `Loc_Port_DriveIn`, `Loc_Port_GasOut` | Locators | none | not rendered |

Icon: a box with a striped top, a side pulley and one outlet arrow. It differs from the Air tank (a lying cylinder with a dial) and the Battery (an upright striped box with a terminal) by its pulley and piston window.

### Behaviour and numbers

- **Charging a default tank (0.01 m³) from ambient at 60 W.** Ideal compression work is V/(γ − 1) × ∫(1 − (p/p_amb)^(−(γ−1)/γ)) dp; dividing by η gives the supplied work.

| Target | Supplied work | Time at 60 W | Share of a default Battery (3600 J, `elements/Battery/BatteryElementDeclaration.cs:42`) |
| --- | --- | --- | --- |
| 300 000 Pa (the gauge's default mark) | 1161 J | 19 s | 32% |
| 400 000 Pa (the tank's default charge) | 2222 J | 37 s | 62% |
| 600 000 Pa (the rating) | 4825 J | 80 s | more than the battery holds: a default battery stops near 510 000 Pa |

  A 0.002 m³ tank needs one fifth of each: 400 000 Pa in about 7 s. Lessons that teach charging should author the small tank; the default pairing is slow but inside the 90 s stall guard (F6), and a level that uses it declares a Run length of 90 s (`ENGINE-RUN-LENGTH`, owner decision 13). The work column is p·V work and does not depend on the ambient temperature. The table is for the tank alone. The rig's hose starts at the tank's pressure (F9) and adds its 4.0 × 10⁻⁴ m³ to the charged volume, so through one hose each figure is 4% higher for a default tank (1208 J, 2312 J, 5021 J) and 20% higher for the small tank (279 J to 300 000 Pa).
- **Stored gas energy.** The tank's internal energy rises by more than the supplied work, because the air drawn in brings its own ambient energy. So the test of "no free energy" is the outcome sentence itself (no supply, no rise) and the charge table above, not "battery loss ≥ gas energy gain".
- **Sharing.** 60 W is half a default Battery's 120 W limit (`BatteryElementDeclaration.cs:44`). Three default compressors ask for 180 W and each receives two thirds, by the delivered proportional rule.
- **Shaft mode.** At the default Motor speed of 6 rad/s (`elements/Motor/MotorElementDeclaration.cs:41`) 60 W is 10 N·m, inside the 15 N·m cap and the Motor's 20 N·m (`:44`), so the Motor holds speed and the compressor takes its full rating. At `power_rating` 240 W the law would ask 40 N·m; the cap gives 15 N·m, so at 6 rad/s the compressor takes 90 W and reaches its full 240 W only at 16 rad/s or more.
- **Tick order.** No signal read. The gas step runs once per 1/480 s substep, before the liquid step and the supply grants, in the [one combined tick order](EL-176-red-laser-emitter.md#combined-tick-order) (EL-039 F5; coordinator decision 26, 23 November 2026): the compression in a substep's gas step spends the grant made in the previous substep (at the first substep, tick n − 1's last), and a grant not spent when Reset is pressed is lost.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S472): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction); the map adds FiniteWorkActuation and electrical/shaft supply. Under D14 the row is navigation; this probe (21 November 2026, `8d1d68aa`) is the authority. Evidence for G rows is in EL-039 section 3.

| Capability | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body with baked colliders (G1) | present | EL-039 G1 |
| Hinged input pulley with a `DriveIn` rim joined to a Motor | present | `elements/Fan/FanElementDeclaration.cs:36`; `FanIntegrationTests.MotorLinkUsesDeclaredInputSenseAndActualShaftTravel` (`CuriousContraptions.tests/FanIntegrationTests.cs:574`) |
| Port and input selected by a choice parameter | present | `FanAdmissionTests.InactiveConnectionAndRetainedWireModeEditPreserveInstallation` (`CuriousContraptions.tests/FanAdmissionTests.cs:191`); `FanIntegrationTests.SelectedModeCompilesOneWorkInputAndOneBladeWriterWithoutHiddenStores` (`CuriousContraptions.tests/FanIntegrationTests.cs:485`) |
| Proportional sharing of one finite source | present | `ElectricalSupplyTests.UnequalDemandSharesProportionallyAndFullLoadReceivesNothing` (`CuriousContraptions.tests/ElectricalSupplyTests.cs:207`) |
| Lamp from committed supply; pulley from committed angle | present | `PresentationDriver.CommittedSupply` (`CuriousContraptions.Abstractions/ElementPresentation.cs:23`), `DeclaredPresentationTests.BatterySupplyAndEachChargeBoundaryUseActualCommittedRead` (`CuriousContraptions.tests/DeclaredPresentationTests.cs:490`); `FanIntegrationTests.NativeBladesUseCommittedPhaseAndPrivateArtworkRestoresOnReset` (`CuriousContraptions.tests/FanIntegrationTests.cs:689`) |
| Wrong-kind refusal; authored-state Reset and save/load (G12) | present | EL-039 G12 |
| Fluid port contract (G5) | present, untested | EL-039 G5 |
| Electrical consumer that pays continuous work | narrower | `SupplyContinuousWorkTarget` takes an airflow source only (`CuriousContraptions.Abstractions/SupplyContractValues.cs:50`); tested by `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`CuriousContraptions.tests/FanIntegrationTests.cs:540`). Paying compression work into a gas line is not a target. Owner: `ENGINE-GAS-NETWORK` |
| Shaft coordinate that pays continuous work | narrower | `CoordinateAirflowInput` (`CuriousContraptions.Abstractions/AirflowValues.cs:38`) pays airflow only. Owner: `ENGINE-GAS-NETWORK` |
| Compression: supplied work drawing ambient air into a line, with a declared efficiency, pressure rating, non-return outlet and, for a shaft input, a declared power-limited resisting torque with a cap | missing | `ENGINE-GAS-NETWORK`; its text names stores, lines, valves and pressure work on an actuator, but not work done **on** the gas. Proposed text in `readiness/batch-h.md` section 4 |
| A store that can gain mass (G3) | missing | `ENGINE-GAS-NETWORK` |
| Gas connection kind and the gas `PortId` members (G6; role Bidirectional, no new `PortRole` member, EL-039 F3); lines (G7) | missing | `ENGINE-GAS-NETWORK` |
| Presentation from committed mass rate (G9); unit for Pa (G11) | missing | `ENGINE-GAS-NETWORK` |

Counts: 6 present, 1 present but untested, 2 narrower, 4 missing. Nothing here needs element-specific engine code. Bellows: no `elements/Bellows` package and no `spec-12-4-pneumatic-bellows.md` exist in this checkout.

**Dependencies.** Battery or Motor (both delivered); EL-039 and EL-038 to store the charge; EL-043 to show it.

### Engine conformance (22 November 2026)

Checked against the owner's conformance instruction of 22 November 2026 and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** One static body, one hinged body, three colliders, one hinge, one electrical consumer, one declared work target, three ports, three parameters and three presentation bindings. No script, callback or loop; the engine may not branch on `air_compressor`. The piston is art.
2. **Generic laws.** A work target turns granted work into delivered gas mass by a declared efficiency and the adiabatic compression law; the grant comes from the delivered electrical sharing rule or from a shaft coordinate through the declared resisting-torque law; the outlet is a one-way path. "An unpowered compressor adds no gas energy" is the consequence of work being the only input. The capability is stated element-free: any package could declare a work target that compresses ambient gas into a line.
3. **Three layers.** *Colliders:* `col_` housing box and foot box on body 0, pulley cylinder on the hinged body. *Visuals:* the art table above. `Housing`, `Foot`, `Fins`, `Grille` and the locators are moved by nothing; `Pulley` follows its body's pose (the hinge); `Piston` and `Lamp` are driven by bindings. *Animation:* piston stroke from committed delivered mass, lamp from committed supply; read-only, off the physics thread. The piston pushes nothing, so it is not a body.
4. **One-way flow and typing.** Supply → work target → gas line inside the tick phases; committed state → animation → renderer. The `supply` choice, ports and roles are enums; power, pressure and torque are f32.
5. **Flat-state counts per instance.** Bodies 2 (1 static, 1 dynamic pulley); colliders 3 (2 boxes, 1 cylinder); joints 1 (revolute hinge); drives 0; sensors 0; domain nodes 2 (one electrical consumer, one gas work target with its one-way path); ports 3; stores 0; bindings 3; parameters 3.
6. **Performance fitness.** Three primitive colliders, as the Battery. The housing is static; the pulley is a dynamic body in both modes, as the Fan's hub is. A resting pulley is integrated every substep today; sleeping is `ENGINE-ISLANDS-SLEEP`. Its contact candidates come from `ENGINE-SPATIAL-QUERIES`. Constant work per instance per tick; nothing scales with the scene.
7. **Tests are scenarios.** C1 to C12 author constructions and assert committed pressures, battery balance, shaft speed and refusals on the production simulation.

## 4. Sources and legacy

- Requirement row [element-037](../requirements.md#element-037): "External work raises pressure in a connected finite gas volume"; outcome "Unpowered compressor cannot increase stored gas energy". No variants listed.
- Named entry [element-037](../invest/named-elements.md#element-037), owner S472.
- Element map composition: "Add FiniteWorkActuation and declared electrical/shaft supply to compression/PressureWork; power loss cannot raise stored gas energy."
- Gas family design: "Air compressor (EL-037) | Electrical node + nozzle source into a sealed store | watt rating, stroke | Charges a connected reservoir while supplied | Piston stroke ← committed charge rate" ([finite-gas-foundation](../../finite-gas-foundation.md#sealed-gas)).
- Decision owners: [S470](../invest/decisions.md#s470) gas-state and open-versus-sealed; [S257](../invest/decisions.md#s257) electrical-port and mechanical-port (decided by the delivering slice under D16).
- Legacy: no compressor part, level or lesson. Applicable facts: compression work law (EL-039 P3–P4), finite discharge (P5), a supplied motor's work allowance is torque × target speed per tick (`parts/MotorPart.cs@a6c914e:L93-L97`) — carry forward as the shaft-mode power reference; do not carry forward the per-part `PreparePhysics` update loop.
- **Files harvested:** `parts/MotorPart.cs` (power reference only), `parts/catalog/motor.tres`; gas files as listed in EL-039.

## 5. Acceptance outline

Acceptance authority: [element-037](../requirements.md#element-037), [pneumatics profile](../invest/profiles.md#pneumatics). Tolerances and stall guards are EL-039 F6. Planned test class `AirCompressorTests`. The rig is a default Battery, the compressor, a hose and an Air tank at ambient; "small tank" is `volume` 0.002 m³.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| C1 | Given the rig with a small tank, when Run advances (stall guard 90 s; the level declares a Run length of 90 s, `ENGINE-RUN-LENGTH`), then the tank passes 300 000 Pa and the battery balance has fallen | `AirCompressorTests.ASuppliedCompressorChargesTheTank` | C2 | — |
| C2 | Requirement outcome: given no wire, a disabled battery or an empty battery, when Run advances 20 s, then tank pressure, mass and energy are unchanged | `AirCompressorTests.AnUnpoweredCompressorAddsNoGasEnergy` | C1 | battery `initial_charge` 0 |
| C3 | Given a default tank through the rig's hose (0.0104 m³ charged), when the battery has supplied 1208 J, then the tank reads 300 000 Pa within 5% (5% of the 20 s wait is one second, which no lesson times) | `AirCompressorTests.ChargeFollowsTheSuppliedWork` | half the work gives a lower pressure | `power_rating` 10 W and 240 W reach the same pressure for the same work |
| C4 | Given `rated_pressure` 200 000 Pa, then the tank stops at 200 000 Pa and the battery balance does not change over a further 5 s | `AirCompressorTests.ChargingStopsAtTheRatingAndDrawsNoMoreWork` | the default rating keeps charging past 200 000 Pa | 150 000 and 600 000 Pa |
| C5 | Given a 60 J battery, when it empties mid-charge, then the tank keeps the pressure it reached for 20 s (non-return outlet) | `AirCompressorTests.LosingSupplyStopsTheRiseAndTheTankHolds` | — | — |
| C6 | Given nothing joined to `GasOut`, then the battery balance is unchanged after 5 s | `AirCompressorTests.AnUnconnectedOutletDrawsNoWork` | C1 | — |
| C7 | Given three compressors on one default battery, each with a small tank, then each takes at least 1.3 times as long to reach 300 000 Pa as one alone (the expected ratio is 1.5) | `AirCompressorTests.CompressorsShareOneBatteryInProportion` | two compressors (120 W) charge at the single rate | — |
| C8 | Given Shaft mode with a supplied default Motor (6 rad/s, 20 N·m) joined to `DriveIn` by a Mechanical link (the delivered rigid connection; coordinator decision 36), then the small tank passes 300 000 Pa and the Motor's settled speed stays between 5 and 6.2 rad/s (a shaft speed is read by eye; below 5 rad/s the load would be visibly dragging it); given an unsupplied Motor, then nothing changes | `AirCompressorTests.AShaftDrivenCompressorChargesOnlyWhileTheShaftTurns` | — | `power_rating` 240 W at the same Motor: the tank passes 300 000 Pa at least 1.3 times sooner than at 60 W (expected 1.5: 90 W against 60 W) and the Motor still does not stall |
| C9 | Given Shaft mode, when a wire is offered to `PowerIn` (or a Mechanical link to `DriveIn` in Electrical mode), or any activation link is offered, then it is refused | `AirCompressorTests.UnavailableAndWrongKindLinksAreRefused` | the available port admits its link | — |
| C10 | Given a run, when Reset is pressed, then tank, battery and pulley are at their authored state | `AirCompressorTests.ResetRestoresTankBatteryAndPulley` | — | — |
| C11 | Given non-default parameters, both modes and their links, when saved and loaded, then they are equal | `AirCompressorTests.SaveLoadKeepsModeParametersAndLinks` | — | both `supply` values |
| C12 | Given a parameter outside its limits, non-finite, or an undefined `supply`, then it is refused with a typed reason | `AirCompressorTests.InvalidParametersAreRefusedAtomically` | the limits are admitted | next f32 beyond each limit |

Generic cover today: G12 for authored-state Reset and save/load; C9–C12 stay package tests until the S4 conformance suite is delivered.

**Chrome recipe (pending the post-catalogue browser phase).** Place Battery, Air compressor and Air tank (`initial_pressure` = ambient); select the battery, Connect `Supply`, click the compressor, choose `Supply → PowerIn`; select the compressor, Connect `GasOut`, click the tank, choose `GasOut → GasIn`.

**Integrations.** Pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393), with the piston rows [todo-426](../requirements.md#todo-426) and [todo-427](../requirements.md#todo-427) and the [todo-378](../requirements.md#todo-378) combination "One Breath"; interaction processes [IX-06 electrical power transfer](../requirements.md#interaction-06), [IX-08 fluid advection](../requirements.md#interaction-08), [IX-09 pressure work](../requirements.md#interaction-09) and [IX-40 gas state evolution](../requirements.md#interaction-40).

## 6. Open questions

1. **Shaft variant scope.** Resolved (D8, D19): Shaft is a `supply` choice on the one entry. The element map names shaft supply and the Fan already shows the pattern as data.
2. **Efficiency and heat.** Resolved (D19): η is fixed at 0.7 and the lost heat is not tracked. `ENGINE-THERMAL` may claim it later if a lesson uses it.
3. **Intake model.** Resolved (D19): always ambient; no connectable inlet.
4. **Acoustic emission.** Resolved (D19): none. No requirement row or lesson asks for compressor sound.
5. **Shaft load law.** Resolved by coordinator decision under the owner's standing delegation (22 November 2026): τ = min(`power_rating` / |ω|, 15 N·m). Reason: the earlier text gave no torque law, so C8's "the Motor slows" could not be derived; a power-limited torque with a cap below the default Motor's torque gives a load that is visible on the shaft and never stalls the default pairing.
6. **Piston stroke and pulley inertia.** Resolved (same decision): 5000 cycles per kg, drawn at no more than 8 cycles per second; a solid-disc inertia for the pulley. Both are starting values (D1).

Needs the owner: none.

## 7. Integration

- **Works with.** Battery and Motor (delivered); EL-039, EL-038, EL-043 (not delivered). The Motor's electrical drain is `ENGINE-SUPPLY-DEMAND`'s, so a Motor and a compressor on one battery is not an acceptance case here.
- **Port agreement.** `PowerIn` and `DriveIn` match the Fan's roles, domains and rim; `GasOut` is a Fluid output (F3).
- **Campaign.** The pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) did not name the compressor, so owner decision 24c gives it new level **74a** *Pump It Up* (chapter 8): introduction 74a, practice 76, reuse 87 and 147 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)). A level that places it needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7**, which also widens the reader for companion packages and non-`EL` identities (owner decision 15): the published form's preconditions are not met. This identity lacks its own `todo-NNN` and `sequence-task-NNN` pair co-anchored in `requirements.md` (the ledger entry links only the umbrella `todo-376`, which has no co-anchored sequence task and is shared by eight identities; the reader refuses two identities on one task source) and an `<a id="s472">` anchor for its ledger owner S472 in `docs/planning/invest/scope-corrections.md`.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-GAS-NETWORK` including the compression work target; EL-039; EL-038 acceptance; S7. EL-043 is useful but not required.
- **Files.** Adds `elements/AirCompressor/AirCompressor.csproj`, `AirCompressorElementDeclaration.cs`, `air_compressor.tscn`, `icon.svg` and `CuriousContraptions.tests/AirCompressorTests.cs` (compiled by the test project's one rule; no compile line). Edits one line in `CuriousContraptions.slnx` and this spec; the per-package lists S3 owns until S3 lands.
- **Review tier.** Data-only, if the probe finds every narrower and missing row delivered.
- **Disqualifiers.** No work target that compresses gas; a store that cannot gain mass; no non-return path; work drawn with nothing joined. Each returns the work to `ENGINE-GAS-NETWORK`.
