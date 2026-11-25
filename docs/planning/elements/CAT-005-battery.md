# CAT-005 · battery — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-005 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Colliders (owner, 25 October 2026, "visible = solid"):** every part a player sees as solid collides with its true shape. Since [`elements-visible-solid`](../../../_bmad-output/planning-artifacts/epics.md#story-elements-visible-solid) the terminal collides as a cylinder (r 0.16, h 0.14 at y 0.59) and the cream band, which stands 1 cm proud of the body all round, as a 0.87 × 0.25 × 0.77 box at y 0.3, with the body box (`elements/Battery/battery.tscn`; `VisibleSolidColliderTests`). The four charge marks and the plus symbol have no collider; they are raised boxes, not flat printing (defect recorded in [section 7](#7-delivered-state-integration-and-residuals-22-november-2026)). Static restitution 1 is this doc's value and the neutral one under the product pair law, tested by a drop onto the terminal.

Story 7.0 Batch B declaration spec (CAT-005-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-005](../requirements.md#current-cat-005). This spec also records the **shared supplied-electrical network** facts that every supplied contact (CAT-013, 020, 024–027, 033, 037, 052) depends on; those specs point here instead of repeating them.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-005 · `battery` |
| Requirement anchor | [current-cat-005](../requirements.md#current-cat-005); retained behaviour [todo-146](../requirements.md#todo-146) |
| Mapped identities | [EL-196 Battery](../invest/named-elements.md#element-196) (finite store; see open question 1). Related, not owned: [EL-197 Electrical wire](../invest/named-elements.md#element-197) (typed electrical connection used by every supplied part). No TH/RAD/GAP identity. |
| Roadmap story | 8.1 "Battery DC Power Source & Network Graph" (Epic 8) Current [CAT-005 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | direct finite-source prerequisite and root integration with Bumper Story6.2 have terminal scoped Pass at published60ddbbe; see [integration review](../../../_bmad-output/implementation-artifacts/review-6-2-bumper-battery-integration.md). **Delivered** (22 November 2026 check): package `elements/Battery`, id `battery`; generic instance by Story 9A.39 (native Pass `54ffd9f0`), run control by `engine-run-control-table`, Motor network acceptance natively with Story 11.1a (`MotorTests`). Sprint key `8-1-battery-dc-power-source-network-graph` stays in-progress only for its Chrome clause. Residuals: [section 7](#7-delivered-state-integration-and-residuals-22-november-2026). Every residual slice (the flat-print repair of the marks and plus sign, the ratings refactor) is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gap, drive work debiting the store, is owned by `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`). |

## 2. Declaration

The delivered declaration is `elements/Battery/BatteryElementDeclaration.cs` with the scene `elements/Battery/battery.tscn` (read on `8d1d68aa`). It implements `ISupplyElement` and `ISceneGeometryElement` (`:8`). The legacy sources of the carried values are in section 4. Colliders, visuals and animation are declared separately, as the [numeric authority](../../gpu-f32-physics.md#compilation-model-and-tri-graph-architecture) requires.

**Body and material.** One static body with no mass and no joint (`:52`). Material: restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`:63`), all f32.

**Colliders (physics graph).** Baked from the hidden scene proxies `col_0_0` to `col_0_2` (`battery.tscn:42–50`, `:99–115`). Physics reads no art.

| # | Shape | Size (m) | Centre (m) |
|---|---|---|---|
| 0 | box | 0.85 × 1.05 × 0.75 | origin |
| 1 | box | 0.87 × 0.25 × 0.77 | (0, 0.3, 0) |
| 2 | cylinder, axis Y | r 0.16, h 0.14 | (0, 0.59, 0) |

**Visuals (rendering graph).** No art mesh is used as a collider.

| Art node | Mesh and colour | Solidity | Moved by |
|---|---|---|---|
| `Visual/Visual_Body` | box 0.85 × 1.05 × 0.75 m, coral `#de7059` | solid, collider 0 | nothing |
| `Visual/Visual_Stripe` | box 0.87 × 0.25 × 0.77 m at y 0.3, cream `#fff8e9` | solid, collider 1 | nothing |
| `Visual/Terminal` | cylinder r 0.16, h 0.14 m at y 0.59, slate to gold | solid, collider 2 | nothing (its binding changes colour only) |
| `Visual/Mark_0`–`3` | boxes 0.12 × 0.12 × 0.02 m at z 0.40, slate to gold | **defect:** 2.5 cm proud of the band face, no collider; to be redrawn as flat prints (section 7) | nothing (colour only) |
| `Visual/Visual_PlusH`, `Visual_PlusV` | navy `#293954` boxes 0.32 × 0.07 × 0.015 m at z 0.385 and 0.07 × 0.32 × 0.015 m at z 0.395 | **defect:** 1.75 cm and 2.75 cm proud of the body face, no collider; to be redrawn as flat prints (section 7) | nothing |

Selection radius 0.8 (`:10`). Icon `elements/Battery/icon.svg`. At a glance it is the coral box with a cream band and a round raised terminal on top; no other delivered part has that silhouette.

**Animation (animation graph).** Fifteen presentation bindings (`:12–26`). Each reads committed supply state of node `source` and writes one colour channel of one art node; none writes to physics, a sensor or a goal.

| Target | Property | Driver | Values |
|---|---|---|---|
| `Visual/Terminal` | albedo red, green, blue | `CommittedSupply` | slate (85, 101, 115)/255 to gold (247, 203, 82)/255, Linear, duration 0 |
| `Visual/Mark_0`–`3` | albedo red, green, blue | `CommittedCharge` | the same colours, at charge thresholds 0.25, 0.5, 0.75 and 1 |

**Port** (`:56`).

| `PortId` | Domain | Direction | Role | Position (m) | Facing |
|---|---|---|---|---|---|
| `Supply` | Electrical | Output | Supply | (0, 0.62, 0) | +Y |

**Parameters** (`:42–49`). All four are required; a save that misses, repeats, adds or mistypes one is refused.

| Name | Kind | Unit | Minimum | Maximum | Default | Editor policy | Run edit |
|---|---|---|---|---|---|---|---|
| `capacity` | Real | joule | 60 | 14,400 | 3,600 | Clamp | Build-only |
| `power_limit` | Real | watt | 10 | 480 | 120 | Clamp | Build-only |
| `initial_charge` | Real | fraction | 0 | 1 | 1 | Clamp | Build-only |
| `enabled` | Choice: Disabled, Enabled | none | — | — | Enabled | Reject (default) | `RunControl` |

The defaults are the owner's decision of 10 October 2026. The bounds are implementation design bounds.

**Supply node, store and event.** One supply source `source` whose store is bound to the four parameters, with `Supply` on its output terminal (`:70–74`). Event `ElectricalPowered` (`:66`). `ElectricalNodes` holds one `Source` row (`:76–79`); its ratings are unread (section 7).

**Compiled counts per instance.** 1 static body; 3 colliders (2 boxes, 1 cylinder); 0 joints; 0 drives; 0 sensors; 0 signal nodes; 1 supply node; 1 finite store; 1 run-control row; 1 port; 15 bindings. All are fixed at compile.

**Catalogue.** Id `battery`, title "Battery", category Power, palette order 11, colour (0.87, 0.44, 0.35), description "Supplies electricity through a wire; activation signals cannot replace a battery." (`:30–37`). Coverage key `battery`.

## 3. Engine capabilities and conformance

Map families, for navigation only (D14): ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction.

**Generic laws.** Every behaviour is a consequence of one general law and the declared values. The package holds no script, callback, update loop or state machine.

| Behaviour | General law | Declared values |
|---|---|---|
| A body rests on it or rebounds from it | rigid contact with the product pair law for restitution, and friction | three colliders; the material |
| It supplies wired loads | supply network: a source node makes its route available each committed tick and pays within its power limit from a finite store | `capacity`, `power_limit`, `initial_charge` |
| It is switched during Run | run-control table: one row for a parameter declared `RunControl`, applied at the next tick | `enabled` |
| It shows supply and charge | presentation bindings on committed supply state | the fifteen bindings |

**Capability probe.** 5 present, 0 untested, 1 narrower, 0 missing.

| Capability | Status | Symbol | Test |
|---|---|---|---|
| Finite source store with a power limit | present | `SupplySourceDescriptor` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:39`) | `SourceStoreInstanceTests.TheStorePaysWithinItsPowerLimitAndNeverOverdrawsItsCommittedBalance`; `ElectricalSupplyTests.RepeatedAllocationCannotOverfillOrSpendMoreThanInitialSource` |
| Typed electrical route and wrong-kind refusal | present | `SupplyTopology.ValidateEdges` (`engine/gpu/SupplyRoute.cs:45–62`) | `SourceStoreInstanceTests.NoWireAReversedWireOrAnActivationLinkGivesNoSupply` |
| Run-time enable control | present | `ParameterRunEdit.RunControl` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:44`) | `SourceStoreInstanceTests.ARunningControlIsAppliedOnTheNextTickAndLeavesTheAuthoredConstructionAlone`; `SourceStoreInstanceTests.ResetIsExactAndClearsTheControlAndASaveDuringRunKeepsTheAuthoredValue` |
| Supply and charge presentation drivers | present | `PresentationDriver.CommittedSupply`, `PresentationDriver.CommittedCharge` | `ComposableAnimationMappingTests` (found by symbol search; assertions not read) |
| Scene-baked static colliders, cylinder cap as a rest surface | present | `SceneGeometryExtractor.Bake` (`tools/BundleElements/SceneGeometryExtractor.cs:14`) | `VisibleSolidColliderTests` (terminal drop, `:465`); `PlacementPoseTests.ABallDroppedOverAnUprightCylinderRestsOnItsCap` |
| Powered work debits the store | narrower | allocation debits bindings to contact-work stores only | A prismatic or revolute drive's work is not debited. Owner: `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`; owner decision 3a), recorded in [deferred work](../../../_bmad-output/implementation-artifacts/deferred-work.md#engine-supply-demand) |

**Conformance (owner addendum, 22 November 2026): conforms with two fixes.**
- Declaration data only: holds. A search of `engine`, `ui` and every non-test `CuriousContraptions.*` project for the package id, class and namespace found no reference (`docs/planning/elements/readiness/audits/elements-batch-b.md` §5).
- Three layers: colliders and animation hold; the visual layer has the six proud marks without colliders (fix 1, section 7).
- One-way flow and typing: holds. Bindings read committed state only; the enable state is a typed Choice; values are f32.
- Flat state: holds, with the counts in section 2.
- Fix 2: the unread `ElectricalNodes` ratings are a required forward-only refactor (section 7).

**Performance fitness.** The body is static and is never integrated. Three primitive colliders is one more than the nearest delivered static housing (two): the terminal is a drawn solid a ball can rest on, so it has its own cylinder, and the band is 1 cm proud all round, so it has its own box. No collider is cosmetic. The supply node costs one row in the supply solve per tick; nothing scales with the scene. The authorities state no per-part budget, so none is claimed here.

**Dependencies.** A supplied consumer makes it observable: Powered gate, Motor and Bumper are delivered. Every supplied contact and gate of this batch (CAT-013, 017, 020, 024–027, 033, 037, 052) and the Impact switch's supply mode use this network.

## 4. Legacy harvest

### Battery part

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| B1 | Parameter enum has exactly one member, `Enabled`. | `parts/BatteryPart.cs@a6c914e:L6-L6` | carry forward |
| B2 | Enable is a per-part binary input whose initial state is Enabled iff `enabled` > 0.5. | `parts/BatteryPart.cs@a6c914e:L12-L15` | carry forward the binary meaning; do not carry forward the float threshold (a closed set must be an enum end to end) |
| B3 | Single port `Supply`, Electrical, Output at (0, 0.62, 0). | `parts/BatteryPart.cs@a6c914e:L16-L17` | carry forward |
| B4 | Source declaration: `Supply` is a network source driven by the enable input. | `parts/BatteryPart.cs@a6c914e:L18-L19` | carry forward |
| B5 | Geometry, art and selection radius as in section 2. | `parts/BatteryPart.cs@a6c914e:L21-L29` | carry forward |
| B6 | An activation command sent to a disabled battery does not create supply; the motor stays unpowered unless connected and enabled. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L96-L135` | carry forward (acceptance fact) |
| B7 | Connecting battery→motor with no explicit sockets resolves the sole compatible pair Electrical `Supply`→`PowerIn`; motor→battery is rejected. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L110-L118` | carry forward |
| B8 | Enable boundary: 0 and 0.5 are disabled, 0.50000006 and 1 enabled; a runtime enable command applies on the next committed tick and a failed tick leaves it unapplied; Reset restores the authored state; save/load round-trips. | `CuriousContraptions.tests/ElectricalSourceTests.cs@a6c914e:L49-L82` | carry forward behaviour; do not carry forward the float boundary (enum) or the CPU transaction rollback mechanism |
| B9 | A source declared on an undeclared, undefined or duplicate socket is rejected at network construction. | `CuriousContraptions.tests/ElectricalSourceTests.cs@a6c914e:L108-L123` | carry forward |
| B10 | An undefined enable-state value is rejected and leaves the previous settled supply unchanged. | `CuriousContraptions.tests/ElectricalSourceTests.cs@a6c914e:L125-L155` | carry forward |
| B11 | The battery's `enabled` parameter is required; Run then Reset restores the saved machine exactly. | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L80` | carry forward |
| B12 | A runtime "disable" command rolls back with a failed tick, then commits once with one acknowledged result. | `CuriousContraptions.tests/RuntimeBinaryInputTests.cs@a6c914e:L86-L108` | do not carry forward the command/result queue mechanism (CPU host transaction); runtime toggle is open question 4 |
| B13 | Catalogue text: "activation signals cannot replace a battery". | `parts/catalog/battery.tres@a6c914e:L8-L14` | carry forward |
| B14 | The scene binds only the script; no authored nodes. | `parts/scenes/battery.tscn@a6c914e:L1-L6` | do not carry forward the imperative binding. Current integration explicitly admits a minimal scene wrapper for the new declaration/art-only BatteryPart; the canonical resource supplies typed finite-source data. |

### Shared supplied-electrical network (used by all supplied parts)

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| N1 | The circuit is a binary-availability logical control law, not a voltage/current/energy model; every solve starts all nodes unpowered, including feedback loops, and the result is the least fixed point. | `engine/BinaryCircuit.cs@a6c914e:L29-L34` | carry forward |
| N2 | Element kinds: sources, wires (input→output), contacts (input→output, closed or open each tick) and two-condition gates with a separate supply node. | `engine/BinaryCircuit.cs@a6c914e:L16-L19` | carry forward |
| N3 | Duplicate source nodes and out-of-range nodes reject at construction. | `engine/BinaryCircuit.cs@a6c914e:L63-L72` | carry forward |
| N4 | A node is powered if any wire input is powered, or any closed contact has a powered input, or any gate has powered supply and a true condition; cyclic components iterate to a fixed point. | `engine/BinaryCircuit.cs@a6c914e:L115-L144` | carry forward the rule; do not carry forward the CPU solver loop (the worker owns the solve) |
| N5 | Cycles are found over potential topology, including open contacts, so closing a contact cannot create a new cycle; no recursion limit. | `engine/BinaryCircuit.cs@a6c914e:L79-L85` | carry forward |
| N6 | A 12 000-node chain solves without a stack limit and with zero per-solve allocation; an all-off source turns every node off. | `CuriousContraptions.tests/BinaryCircuitTests.cs@a6c914e:L65-L80` | carry forward as a boundary fact (allocation proof belongs to the Epic 16 gate) |
| N7 | Mismatched input/result lengths reject and leave the previous result unchanged; an empty topology is valid. | `CuriousContraptions.tests/BinaryCircuitTests.cs@a6c914e:L82-L123` | carry forward |
| N8 | The scene network compiles once per Run from every electrical port (each port is a node; every input port is published) and every electrical wire; each tick samples only source states and contact states. | `engine/ElectricalNetwork.cs@a6c914e:L10-L14` and `engine/ElectricalNetwork.cs@a6c914e:L60-L125` | carry forward (compile at Run into flat tables) |
| N9 | A source, contact or gate referencing a socket the part does not declare with the right domain and direction rejects. | `engine/ElectricalNetwork.cs@a6c914e:L80-L102` | carry forward |
| N10 | Observable power changes only after every source and contact is sampled and the solve succeeds; then all inputs are cleared and re-supplied together. | `engine/ElectricalNetwork.cs@a6c914e:L127-L137` | carry forward |
| N11 | Published per-input reads are binary `Unavailable`/`Available`, keyed by owner body and socket; no voltage, current or energy is implied. | `engine/bridge/ElectricalInputRead.cs@a6c914e:L6-L18` | carry forward |
| N12 | Source signal kinds: binary input, a contact condition, or a scalar ≥ threshold (solar). | `engine/SceneElectricalSource.cs@a6c914e:L8-L33` | carry forward as a typed enum |
| N13 | Contact condition kinds: owner active, boolean state, counter reached, latch on, timer counting, contact loaded, servo endpoint; each must bind a capability its owner declares. | `engine/SceneElectricalContact.cs@a6c914e:L8-L8` and `engine/SceneElectricalContact.cs@a6c914e:L56-L70` | carry forward as a typed enum |
| N14 | A contact reads its condition from owned committed state (counter phase Reached, latch On, timer Counting, contact load Loaded). | `engine/SceneElectricalContact.cs@a6c914e:L132-L142` | carry forward |
| N15 | A port is socket id, domain, direction, local position and (activation) command; a link resolves only with exactly one matching port at each end, the link's domain on both, output (or bidirectional) to input. | `engine/ConnectionPort.cs@a6c914e:L15-L21` and `engine/ConnectionPort.cs@a6c914e:L40-L67` | carry forward the rule; `Bidirectional` has no Batch B user |
| N16 | Serialized domain names are snake_case (`activation`, `electrical`, …); socket ids serialize as exact snake_case names (`supply`, `power_in`, `first_in`, `second_in`, `set_in`, `reset_in`, `activation_in`, `activation_out`) with no aliases. | `engine/MachineData.cs@a6c914e:L67-L70` and `engine/MachineData.cs@a6c914e:L103-L122` | carry forward at the serialization boundary only |
| N17 | A closed relay chain propagates in one tick regardless of id order; a wire loop with no source stays unpowered; supply loss clears every downstream input. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L137-L181` | carry forward |
| N18 | Supply loss clears motor power without erasing shaft momentum or adding work. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L183-L215` | carry forward (CAT-042 owns the motor half) |
| N19 | Test supply-switching fixture: battery → latch contact, two switches drive Set/Reset; a supply change settles two ticks after the command and never edits topology ("supply changes are activation commands, never live topology edits"). | `CuriousContraptions.tests/SupplyControl.cs@a6c914e:L6-L64` | carry forward as a construction recipe for supply-loss controls |
| N20 | The legacy tick was 1/120 s. | `reference/cpu/MachineWorld.cs@a6c914e:L20-L20` | do not carry forward the constant; current cadence comes from `SimulationCadence` |
| N21 | Port, gate and route declarations are captured when the network compiles and are never re-read during a solve (a solve that would poll them throws); a non-finite source reading rejects the solve and leaves every previous input power unchanged; an open contact unpowers its output while its own input stays powered. | `CuriousContraptions.tests/ElectricalPlanTests.cs@a6c914e:L52-L79` | carry forward the rules; do not carry forward the allocation measurement (Epic 16 gate) |
| N22 | Run, a failed tick, Reset and a saved reload each use a freshly compiled network: on the first step a switch input is powered while its load waits for activation; a failed tick leaves power unchanged; Reset leaves no input powered; a reloaded machine behaves identically. | `CuriousContraptions.tests/ElectricalPlanTests.cs@a6c914e:L81-L112` | carry forward the Run/Reset/reload behaviour; do not carry forward the CPU failure-injection rollback |
| N23 | Published input reads reject an undefined socket, undefined availability, an unknown or non-root owner, duplicates, wrong order and wrong count; a rejected publication leaves the previous one in place; reads of an undeclared key or undefined sample reject. | `CuriousContraptions.tests/CommittedElectricalTests.cs@a6c914e:L47-L94` | carry forward the rejection rules |
| N24 | Committed electrical reads are pinned per revision with previous/current samples, staged and discarded through a CPU pose buffer, with zero-allocation warm publication. | `CuriousContraptions.tests/CommittedElectricalTests.cs@a6c914e:L18-L45` and `CuriousContraptions.tests/CommittedElectricalTests.cs@a6c914e:L96-L120` | do not carry forward (CPU publication and rollback mechanism; allocation proof belongs to the Epic 16 gate) |

### Levels and lessons

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | `battery_motor` (14 / Electrical supply): inventory battery 1; locked motor at (3, 1, 0); solution battery at (−3, 1, 0) with one Electrical `supply`→`power_in` wire; goal motor `turned`. Hint: "Place a battery, select it and choose Connect. Click the motor, then Run." | `content/puzzles.json@a6c914e:L3424-L3579` | carry forward (Epic 15 campaign input) |
| L2 | `switched_motor` (15 / Switched supply): locked battery (−5, 1, 0), motor (3, 1, 0), bowling (−2, 5, 0); solution switch (−2, 1, 0); wires battery→switch→motor; goals `turned` and `powered_after` switch. | `content/puzzles.json@a6c914e:L3580-L3873` | carry forward |
| L3 | Bypass control: wiring battery directly to motor turns it and the switch activates, but `powered_after` fails; removing any one solution wire leaves the motor unpowered and the level unsolved; replays are tick-identical after Reset. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L42-L94` | carry forward |
| L4 | Locked battery fixtures (battery→motor wire) in `conveyor_courier`, `belt_relay`, `reverse_belt`, `air_and_belt`, `mixed_signals`, `chain_mail`, `double_cold_start`, `depth_delivery`, `double_bridge`, `grand_contraption`, `wind_then_release`, `saved_for_later`; typical position (−3.4, 0.4, lane z). | `content/puzzles.json@a6c914e:L3874-L4225`, `content/puzzles.json@a6c914e:L4226-L4647`, `content/puzzles.json@a6c914e:L4648-L5070`, `content/puzzles.json@a6c914e:L9143-L9689`, `content/puzzles.json@a6c914e:L12577-L13193`, `content/puzzles.json@a6c914e:L18225-L19155`, `content/puzzles.json@a6c914e:L21081-L22099`, `content/puzzles.json@a6c914e:L25205-L26009`, `content/puzzles.json@a6c914e:L26885-L28003`, `content/puzzles.json@a6c914e:L30172-L31305`, `content/puzzles.json@a6c914e:L31306-L31860`, `content/puzzles.json@a6c914e:L31861-L32564` | carry forward (each level belongs to its consumer's spec) |
| L5 | Campaign generator source for `battery_motor` and `switched_motor`, titles and hints. | `tools/Campaign/Program.cs@a6c914e:L125-L149` and `tools/Campaign/Program.cs@a6c914e:L391-L396` | carry forward the data only (Epic 15 rebuilds the tool) |

`reference/P0-022-before/docs/coverage/engine/*.json` and `reference/p054-guide/puzzles-candidate.json` mention `battery` only as a catalogue id or an older copy of the same levels: no additional element knowledge. Many uncompiled tests subclass `BatteryPart` as a generic probe source (for example `BooleanObservationTests.cs`, `EnumObservationTests.cs`, `PartRuntimeCheckpointTests.cs`, `NetworkSpatialOwnershipTests.cs`); they hold no battery behaviour beyond B3–B4.

### Files harvested

- `parts/BatteryPart.cs`
- `parts/catalog/battery.tres`
- `parts/scenes/battery.tscn`
- `engine/BinaryCircuit.cs`
- `engine/ElectricalNetwork.cs`
- `engine/SceneElectricalSource.cs`
- `engine/SceneElectricalContact.cs`
- `engine/ConnectionPort.cs`
- `engine/MachineData.cs`
- `engine/bridge/ElectricalInputRead.cs`
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `CuriousContraptions.tests/ElectricalTests.cs`
- `CuriousContraptions.tests/ElectricalSourceTests.cs`
- `CuriousContraptions.tests/ElectricalPlanTests.cs`
- `CuriousContraptions.tests/CommittedElectricalTests.cs`
- `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`
- `CuriousContraptions.tests/RuntimeBinaryInputTests.cs`
- `CuriousContraptions.tests/BinaryCircuitTests.cs`
- `CuriousContraptions.tests/SupplyControl.cs`
- `content/puzzles.json` (levels listed in L1–L4; the file survives)
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance authority: [CAT-005](../requirements.md#current-cat-005) and [todo-146](../requirements.md#todo-146); Story 8.1 adds `tools/e2e/cat-005.test.ts`.

- **Construction (actual Chrome UI).** Place Battery from the drawer, select it, Connect, click the consumer: the sole compatible pair `Supply`→`PowerIn` is chosen and a navy cable joins the actual sockets (B7). No numeric placement or setter.
- **Positive.** An enabled battery wired to the declared minimal consumer powers it on the first committed tick after Run (consumer per open question 2).
- **Negative / controls.** Disabled battery; no wire; wrong-domain link refused; consumer→battery refused; activation sent to the battery creates nothing (B6); a source-free wire loop stays unpowered (N17).
- **Boundaries.** Enum enable only (no float threshold); undeclared or duplicate source socket rejected (B9).
- **Run/Reset.** Reset restores the authored enable state and an unpowered network; replay is tick-identical (L3, N22).
- **Save/Load.** `enabled` and the electrical wire round-trip with exact socket names (N16).
- **Integrations.** Supply-loss control through the supply fixture (N19) once Latch exists; `battery_motor` and `switched_motor` replays once the Motor and Switch supply modes exist.
  - *Status (Story 9.4c, 22 Oct 2026):* the N19/N17 supply fixture is proven natively by `CuriousContraptions.tests/LatchSupplyFixtureTests.cs` (production simulation; measured route lag one tick, see [spec D3](../../../_bmad-output/implementation-artifacts/spec-9-4c-latch-fixture-deferred-integrations.md)). Browser case `cat-005-n19` is pending post-catalogue E2E.

## 6. Open questions

1. **Finite energy — defaults resolved 10 Oct 2026.** Owner approved 3,600 J capacity, 120 W power limit, initially full and enabled, using the energy/power model. This is an owner decision, not a legacy value or runtime completion claim. EL-196 declares depletion/accounting. The current bounded implementation admits capacity 60–14,400 J, maximum output 10–480 W, initial fraction 0–1 and a typed Enabled/Disabled setting. These are enforced implementation design bounds, distinct from the owner-approved defaults; they do not imply separate owner approval of every bound.
2. **First consumer.** Bumper PowerIn is the approved direct finite-store consumer for the Story6.2 recharge prerequisite. It does not complete the following Motor/network obligation: The Story 8.1 AC uses an Electric Motor "with continuous 12V supply", but Motor is Story 11.1 (Epic 11) and the Signal lamp is activation-only (CAT-035). The legacy tests' supplied load was the Powered gate (CAT-051), now Story 8.2, directly after the Battery in Epic 8. Candidate consumers are Motor and Powered gate. **Resolved by delivery:** the Powered gate is the supplied consumer of the native Battery proof (`SourceStoreInstanceTests.AnEnabledStoreSuppliesTheGateFromTheFirstCommittedTickAndADisabledOneNeverDoes`), and the Motor acceptance is delivered with Story 11.1a (`MotorTests.ASuppliedMotorSpinsUpToItsSpeedHoldsItAndPaysOnlyItsBearingLoss`, `MotorTests.EnablingTheBatteryDuringRunSpinsTheMotorUpFromRestAndLightsItsLamp`).
3. **"12V" — model resolved 10 Oct 2026.** Owner approved an energy/power model; the old voltage wording does not require a voltage simulation.
4. **Runtime enable toggle — declared (Story 9A.39).** The package declares its store's `enabled` parameter run-editable (`ParameterRunEdit.RunControl`); that declaration, not an element check, gives the generic editor its one contextual Enabled/Disabled control while Running or Paused and compiles one `SourceEnable` row of the run-control table for an unlocked owner, which admits the typed `ConfigureRunControl` command (engine-run-control-table). The next electrical phase commits the change; it never edits the authored construction (no revision, no Undo), so Reset and a save keep the authored value; stored consumer work is preserved. Native proof: `SourceStoreInstanceTests`, `PalettePlacementTests.TheRunControlToggle…`; browser re-run pending post-catalogue E2E.
5. **Committed supply indicator.** Current direct-source implementation uses a slate/gold supply terminal and four charge marks on the cream band, driven by committed state through generic animation. This is new implementation, not legacy evidence. *Story 9A.39 (coordinator decision, 22 Oct 2026):* the four-mark gauge, scaled by the instance's declared capacity, is the charge display; the former Running/Paused "Charge: X J" panel label is dropped (a visible UI change, queued for the post-catalogue browser re-run).
6. **"Disconnecting the cable" during Run.** The Story 8.1 AC says "disconnecting the cable stops the motor immediately". The legacy never edited wires during Run: supply loss was always an activation command through the latch fixture (N19). **Resolved (Story 9.6 decision 4, [CAT-013 §6](CAT-013-both_gate.md#6-current-delivery-decisions-22-october-2026)):** topology compiles once per Run. "Disconnecting" means source or contact loss during Run (the run-control toggle, or a Latch, Switch, Hold timer or Pressure plate contact) or an edit between Runs, never a mid-Run cable edit. Native proof: `MotorTests.CuttingTheSupplySpinsTheRotorDownUnderBearingFrictionWithNoFurtherWork`, `LatchSupplyFixtureTests.ResetWhenOpenClearsDownstreamAndSpringCloses`.

## 7. Delivered state, integration and residuals (22 November 2026)

Checked against `elements/Battery`, its scene and its tests on published main `8d1d68aa`, and against `origin/main` `25df3cd5`, which changes none of them. Nothing was built or run.

**Disposition.** Delivered CAT part: package `elements/Battery`, id `battery`, category Power, palette order 11 (`elements/Battery/BatteryElementDeclaration.cs:30–35`). No slice is planned for this identity. [EL-196](EL-196-battery.md) refines it and is planned separately (batch H readiness). Every residual below is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element.

**Visible-solid defect: repair decided (coordinator, 22 November 2026, under the owner's standing delegation).**
- The six nodes marked as defects in section 2 are static, proud and uncollided. `CuriousContraptions.tests/VisibleSolidColliderTests.cs:72–74` lists them Decorative as "printed on the body", but they are boxes, and a colour binding does not make art decorative (`spec-engine-art-solidity.md`, decision D5). In play a ball against the +Z face sinks up to 2.75 cm into drawn solids.
- Decision: redraw all six as flat `QuadMesh` prints 0.25 mm in front of their faces, as `spec-face-markings-flush.md` did for the logic parts. Reason: they are labels, and a print needs no collider, so the collider count stays three.
- The vertical plus bar spans y −0.14 to 0.18 and the band starts at y 0.175, so a print on the body face would be buried for 5 mm. It is shortened to 0.07 × 0.31 m (or centred at y 0.015) so it stays clear of the band.
- The marks keep their `CommittedCharge` colour bindings. The comment at `VisibleSolidColliderTests.cs:72` changes only after the redraw; the row stays Decorative, then truly printed flat. The repair-contract text is in [batch B readiness](readiness/batch-b.md) (gap 1). No repair contract names the Battery today; the repair is one small visible-solid slice after the engine programme, and EL-196 carries the same finding.

**Other residuals.**
- **Required forward-only refactor: unread ratings.** `ElectricalNodes` declares 12 V, 0.1 Ω and `MaxPower` 100 W (`BatteryElementDeclaration.cs:76–79`). The four rating values are only checked finite (`CuriousContraptions.Bundle/BundleValidator.cs:873–876`); no engine, runtime or presentation code reads them, and 100 W differs from the 120 W `power_limit` with no effect. Dead declaration data is removed, not kept; the deferred-work text is in batch B readiness.
- The drain residual in the section 3 probe: drive work debits the store, owned by `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`).
- Requirement row CAT-005 stays unchecked until the post-catalogue browser phase.

**Integration.**
- Delivered collaborators: Powered gate, Motor, Bumper, and the supplied contacts and gates of this batch (CAT-013, 017, 020, 024–027, 033, 037, 052).
- Campaign (`content/puzzles.json`): 8 levels place it as a locked fixture and 1 offers it in inventory (numbers are authored positions before decision 24d, each paired with its plan level in [campaign coverage, section 5](readiness/campaign-coverage.md#5-removed-and-converted-levels-owner-decision-24d)). Planned: 14 `battery_motor` (inventory only), 15 `switched_motor`, 16 `conveyor_courier`, 17 `belt_relay`, 18 `reverse_belt`, 49 `double_cold_start`, 58 `grand_contraption`. Playable: 59 `wind_then_release`, 60 `saved_for_later`. Level 14 needs the `turned` goal kind, which ENGINE-GOALS owns ([delivery batches §4](../invest/delivery-batches.md#4-early-level-proof)).
- Coverage binding: catalogue key `battery` in `docs/coverage/catalogue-elements.json`.
- The lesson-fixture story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22); a level that places a Battery as a locked fixture waits for it.
