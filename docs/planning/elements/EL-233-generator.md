# EL-233 · Generator — named-identity spec

**Current delivery (21 November 2026):** [EL-233 ledger entry](../invest/named-elements.md#element-233), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending. Repaired 23 November 2026 against the owner decisions of 22 November 2026 ([readiness/owner-decisions-2026-11-22.md](readiness/owner-decisions-2026-11-22.md)).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-233 · Generator · Electrical |
| Requirement anchor | [todo-189](../requirements.md#todo-189) (`sequence-task-366`) |
| Ledger entry / owner | [element-233](../invest/named-elements.md#element-233); slice `named-el-233`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a), whose `ENGINE-SUPPLY-DEMAND` delivers the shaft-paid source.** Delivery batches lists it in 1b as "shaft-paid supply"; the probe confirms the capability is missing, so it is 1c. |
| Planned package | id `generator`, directory `elements/Generator/` |
| Related | [CAT-042 Motor](CAT-042-motor.md) (its inverse), [CAT-005 Battery](CAT-005-battery.md) (source shape), [EL-211 Solar panel](EL-211-solar-panel.md), TH-30 and RAD-20 (other sources) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` (a supply source paid by shaft work, and a Motor's drive work drawn from its source; section 3) and `ENGINE-ISLANDS-SLEEP` (a stopped rotor rests). The Windmill and Motor collaborators are re-qualified by the same programme |

## 2. Declaration

Shape: the Motor's housing and rotor (`elements/Motor/MotorElementDeclaration.cs:16`) with the Battery's `Supply` port (`elements/Battery/BatteryElementDeclaration.cs:56`), and a source whose energy is shaft work instead of a store.

- **Interfaces.** `IHingeElement` (`MechanicalContracts.cs:20`), `ISupplyElement` (`ElectricalContracts.cs:11`), `ISceneGeometryElement` (`SceneGeometry.cs:10`).
- **Housing (body 0, static)** 1.25 × 0.8 × 0.5 m; **rotor (body 1)** cylinder radius 0.3 m, width 0.13 m, mass 0.5 kg, solid-disc inertia; continuous hinge, friction torque 0.05 N·m. These are the Motor's.
- **Source.** A supply source whose available power each tick is paid by braking the rotor: electrical energy delivered = 0.8 × shaft work absorbed. It holds no energy of its own. Electrical node: rated 12 V, as every part.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `power_limit` | Real, W | 10–240 | 60 | Reject, build-only | Half the Battery's default 120 W, so one generator runs a lamp or a gate and struggles with a Motor |
| `max_torque` | Real, N·m | 1–20 | 10 | Reject | The largest braking torque it applies; half a default Motor's 20 N·m, so a Motor can turn it |

| Port | Domain | Direction, role | Position (m) |
| --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input, DriveIn; rim radius 0.3 m on the rotor | (0, 0, 0.65) |
| `Supply` | Electrical | Output, Supply | (0.55, 0, 0) |

- **Presentation.** Rotor body binding; output lamp from `CommittedSupply` (slate to gold).
- **Palette.** Power, "Generator"; order 306 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Housing with cooling fins | Box; fins drawn within the box outline, ochre `#d99e32` | Solid, body 0 |
| Rotor | Cylinder, navy `#1c2836` with a gold stripe printed on | Solid, body 1 |
| Output lamp | Flush disc | Decorative: printed flat |

Icon: a rotor with a lightning bolt leaving it. It differs from the Motor (bolt entering, lamp on the housing) by the bolt's direction and the fins.

### Behaviour and numbers

- Law: with loads drawing P watts (P ≤ `power_limit`), the rotor is braked with torque τ = P ÷ (0.8 × |ω|), limited to `max_torque`. If the limit binds, less power is delivered. Below 1 rad/s the output is off.
- It generates for either direction of rotation; output has no polarity.
- At the Motor's default 6 rad/s and `max_torque` 10 N·m the shaft can absorb 60 W and deliver 48 W.
- Source loss: when the shaft stops, dependent devices lose supply in the same electrical tick's solve after the rotor speed commits (next tick, as D7 treats every committed reading).
- No perpetual loop: Motor → Mechanical link → Generator → wire → the same Motor returns at most 0.8 of the work each pass and bearings take more, so the pair runs down and stops. A Battery in the loop is drained, never recharged above what the shaft paid. Both depend on a Motor's drive work being drawn from its source, which is `ENGINE-SUPPLY-DEMAND`'s (drive supply is binary today, `_bmad-output/implementation-artifacts/deferred-work.md:211`): until then a Motor on this source would place no load on the shaft.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Continuous hinge with a `DriveIn` rim | present | `PortDescriptor.cs:20` | `ConveyorTests.ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` |
| A back-driven shaft doing work on a load | present | `MechanicalContractValues.cs:138` | `WindmillIntegrationTests.ConnectedExternalDriveBackdrivesActualWindmillAboveAerodynamicTarget`; `AirflowRuntimeTests.CoordinateLoadPaysFromFiniteRotorEnergy` |
| Finite source allocation among loads | present | `ElectricalContractValues.cs:31` | `ElectricalSupplyTests.UnequalDemandSharesProportionallyAndFullLoadReceivesNothing` |
| Shaft work paying another capability (airflow) | present | Fan shaft mode (`elements/Fan/FanElementDeclaration.cs:20`) | `AirflowSupplyIntegrationTests` (class) |
| **A supply source paid by shaft work** | **missing** | `SupplySourceDescriptor` takes only a parameter-declared store (`SupplyContractValues.cs:39`, `ElectricalContractValues.cs:31`) | Owner: `ENGINE-SUPPLY-DEMAND` (owner decision 3a; the inverse of its work-target clause: a source whose available power is at most a declared efficiency times the work absorbed on a declared coordinate) |
| **A supplied drive's committed work drawn from its source** (a Motor fed by the Generator in Q4 and Q7; the Battery drain in Q4's control) | **missing** | drive supply is binary (`_bmad-output/implementation-artifacts/deferred-work.md:211`) | Owner: `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`) |

Counts: 4 present, 0 untested, 0 narrower, 2 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static housing, one hinged rotor with a rim port, one supply source and one electrical port. Output is the supply network's allocation law with a source whose available power is paid by shaft work; the braking torque P ÷ (0.8 × |ω|) is that source's declared conversion; run-down is hinge friction. The two missing capabilities are stated for any package and are owned by one supply-graph story, `ENGINE-SUPPLY-DEMAND`: a supply source paid by work absorbed on a declared coordinate, delivering no more than its declared efficiency times that work; and a supplied drive's committed work drawn from its source. No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 1 box (housing) and 1 cylinder (rotor) as hidden `col_` proxies; fins are drawn inside the box. Visuals: the art nodes, none a collider. Animation: one lamp binding from committed supply. What moves each art node: housing and fins, nothing (static body 0); rotor and its printed stripe, body 1's pose; output lamp, the driver binding only.
- **Flat state.** 2 bodies; 2 colliders (1 box, 1 cylinder); 1 revolute joint; 0 drives; 0 sensors; 0 signal nodes; 1 supply source; 0 stores; 1 body-pose and 1 driver binding.
- **Performance.** The Motor's primitive count. The housing is a static body. Whether a stopped rotor may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. No region or scan.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-189](../requirements.md#todo-189): "Couple a driven shaft to electrical output. Verify source loss stops dependent devices; teach motor/generator conversion without perpetual-power loops." [sequence-task-278](../requirements.md#sequence-task-278): "ensure each intended generator/solar source obeys its specified energy model. No feedback loop creates free power."
- **Campaign.** Chapter 4 (31–40); the end-to-end lesson "windmill → belt → generator → wire → motor → belt → conveyor".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: energy inequalities with + 1 J; "stops" is below 0.1 rad/s; lamp state exact; stall guard 30 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| Q1 | Windmill (Fan blowing) → Mechanical link → Generator → wire → Lamp | The lamp is on while the rotor turns | Control: Mechanical link removed, the lamp stays off | `GeneratorTests.ADrivenShaftLightsALamp` |
| Q2 | Fan switched off | The rotor runs down and the lamp goes off | — | `GeneratorTests.SourceLossStopsDependentDevices` |
| Q3 | Any run | Electrical energy delivered ≤ 0.8 × shaft work absorbed + 1 J | Boundary: `power_limit` 10 and 240 | `GeneratorTests.OutputIsPaidByShaftWork` |
| Q4 | Motor → Mechanical link → Generator. The Generator's `Supply` and a Battery's `Supply` are both wired to the Motor's `PowerIn` (two sources on one input). The Battery starts the Motor and is then disabled | The pair stops within the stall guard; total energy never rises | Control: Battery left on, it keeps running and the Battery drains | `GeneratorTests.AMotorGeneratorLoopRunsDown` |
| Q5 | Load connected against load disconnected at the same drive | The loaded rotor turns slower (the load is felt on the shaft) | — | `GeneratorTests.ElectricalLoadBrakesTheShaft` |
| Q6 | Shaft reversed | The lamp is on as before | — | `GeneratorTests.EitherDirectionGenerates` |
| Q7 | Windmill → Mechanical link → Generator → wire → Motor → Mechanical link → Conveyor carrying a ball | The ball is delivered to a Receiver | Control: wire removed | `GeneratorTests.WindCarriesABallThroughTwoConversions` |
| Q8 | Run, Reset; Save, Load | Rotor at rest, parameters, Mechanical link and wire restored | — | `GeneratorTests.ResetAndReloadRestoreTheAuthoredGenerator` |
| Q9 | Activation wire to `Supply`; Mechanical link to `Supply`; `power_limit` 9 | Refused with a typed reason | — | `GeneratorTests.WrongKindWiresAndOutOfRangeValuesAreRefused` |

**Integration.** Windmill, Motor, Fan, Lamp, Battery, Conveyor, Powered gate, every supplied part (delivered; each re-qualified by the engine programme, the Fan's airflow candidates coming from `ENGINE-SPATIAL-QUERIES` and every Motor's and gate's draw from `ENGINE-SUPPLY-DEMAND`); Gears, Mouse motor, Waterwheel (this batch). Levels need S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `generator`; Named identity EL-233; Acceptance todo-189 and sequence-task-366; owner slice `named-el-233`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SUPPLY-DEMAND` delivering the shaft-paid source and the drive's draw on its source, and `ENGINE-ISLANDS-SLEEP`. Other prerequisites: the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: the `elements/Generator/` package, `CuriousContraptions.tests/GeneratorTests.cs`, and the hand-edited lists until S3. Review tier: engine. **Disqualified if** the conversion is delivered as a store charged by the shaft (a battery with a crank); then `power_limit` and Q2 change, because stored energy would keep devices running after the shaft stops. Also to probe: whether two sources may feed one input (Q4); if not, Q4's Battery supplies a second Motor joined by a Mechanical link to the same shaft.

## 6. Open questions

1. *Efficiency.* Resolved by D1: 0.8, a starting value; any value below 1 makes a loop run down.
2. *A store inside the generator?* Resolved under the standing delegation: none. The requirement says losing the shaft source stops dependent devices.

Needs the owner: nothing.
