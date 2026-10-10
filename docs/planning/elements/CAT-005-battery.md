# CAT-005 · battery — declaration readiness spec

Story 7.0 Batch B declaration spec (CAT-005-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-005](../requirements.md#current-cat-005). This spec also records the **shared supplied-electrical network** facts that every supplied contact (CAT-013, 020, 024–027, 033, 037, 052) depends on; those specs point here instead of repeating them.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-005 · `battery` |
| Requirement anchor | [current-cat-005](../requirements.md#current-cat-005); retained behaviour [todo-146](../requirements.md#todo-146) |
| Mapped identities | [EL-196 Battery](../invest/named-elements.md#element-196) (finite store; see open question 1). Related, not owned: [EL-197 Electrical wire](../invest/named-elements.md#element-197) (typed electrical connection used by every supplied part). No TH/RAD/GAP identity. |
| Roadmap story | 8.1 "Battery DC Power Source & Network Graph" (Epic 8) |
| Status | direct finite-source prerequisite and root integration with Bumper Story6.2 have terminal scoped Pass at published60ddbbe; see [integration review](../../../_bmad-output/implementation-artifacts/review-6-2-bumper-battery-integration.md). Full Story8.1/Motor/network acceptance remains incomplete. |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | One static root box 0.85 × 1.05 × 0.75 m centred at the part origin (`parts/BatteryPart.cs@a6c914e:L24-L24`). Legacy `AddBox` registers a root-body collision box with half extents size/2 and draws it (`reference/cpu/MachinePart.cs@a6c914e:L352-L356`). Legacy world units were carried 1:1 into current metres (the Switch box 1.1 × 0.25 × 1 became half extents 0.55 × 0.125 × 0.5 m in `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L114`). |
| Mass and material | Static; no mass. Contact material is the legacy static default restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`), the same values the current compiler declares for static parts (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). Declare through `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L45`) as f32; that record still stores binary16, listed as remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L94-L94`). |
| Constraints and joints | none — a fixed static body. |
| Typed sockets and ports | One output: `Supply`, Electrical, Output, local (0, 0.62, 0) m (`parts/BatteryPart.cs@a6c914e:L16-L17`). Current enums already hold `WorkshopSocket.Supply = 7` and `WorkshopConnectionDomain.Electrical = 2` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`); the baseline `WorkshopPorts.For` had no battery row (`engine/gpu/WorkshopConnections.cs@a6c914e:L18-L26`). |
| Sensors and activation | none. No activation input: an activation command cannot create supply (harvest B6). |
| Work and energy stores | Legacy: none — binary availability only (`engine/SceneElectricalSource.cs@a6c914e:L10-L10`, `engine/bridge/ElectricalInputRead.cs@a6c914e:L10-L10`). Requirement: binary enable is not finite-energy qualification; capacity/power/work/depletion must be named before powered work. Owner decision (10 Oct 2026): energy/power model with default capacity 3,600 J, power limit 120 W, initially full and enabled; no infinite supply. See EL-196 for the declared finite-store law; runtime qualification remains outstanding. |
| Parameters | `enabled`: legacy float, Enabled when value > 0.5 (exclusive), default 1.0, unitless (`parts/BatteryPart.cs@a6c914e:L12-L15`, `parts/catalog/battery.tres@a6c914e:L14-L14`). Carry forward as a typed two-value enum (`Disabled`, `Enabled`), not a float compare. Required: a battery spec without it is rejected (harvest B11). |
| Cosmetic curves and UI bindings | none in legacy (no indicator). The requirement asks to "show committed supply state" — no legacy art for it (open question 5). Electrical cables are navy and thicker than gold activation links ([DESIGN.md](../../../DESIGN.md), battery/motor icon paragraph). |
| Art | Body box in catalogue colour `#de7059` (0.87, 0.44, 0.35); cream `#fff8e9` band 0.87 × 0.25 × 0.77 at y 0.3; gold `#f7cb52` terminal cylinder r 0.16, h 0.14 at y 0.59; navy `#293954` plus-sign bars 0.32 × 0.07 × 0.015 and 0.07 × 0.32 × 0.015 on the front face at z 0.385/0.395 (`parts/BatteryPart.cs@a6c914e:L24-L28`; primitive signatures `engine/PartArt.cs@a6c914e:L16-L22`). Selection ring radius 0.8 (`parts/BatteryPart.cs@a6c914e:L23-L23`; ring colour `#efffbd`, `reference/cpu/MachinePart.cs@a6c914e:L315-L315`). Toolbox icon (current, survives): `ui/WorkshopIcons.cs@a6c914e:L47-L47`. Palette row "Battery" in [DESIGN.md · Colour system](../../../DESIGN.md#colour-system). |
| Catalogue / inventory | Id `battery`, Title "Battery", Category Power, Description "Supplies electricity through a wire; activation signals cannot replace a battery." (`parts/catalog/battery.tres@a6c914e:L8-L14`). Inventory: `battery_motor` grants 1; 12 other levels place a locked battery (harvest L4). |

## 3. Engine capabilities

Families (from the [map row](../general-engine-element-map.md)): ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction.

**Exists now**
- Static box body, collider and material: `engine/gpu/PhysicsDeclarations.cs` (`RigidBodyDeclaration`, `ColliderDeclaration`, `ContactMaterialDeclaration`), compiled by `engine/gpu/WorkshopPhysicsCompiler.cs`.
- Typed connection storage with an Electrical domain and `Supply`/`PowerIn` sockets: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Discrete activation network (activation only, 8 nodes, no electrical solve): `engine/gpu/ActivationNetwork.cs@a6c914e:L70-L108`.

**Missing** (Story 8.1 builds all of these unless stated)
- ElectricalPower: a compiled per-Run binary supply network (sources, wires, contacts, gates) solved once per tick on the physics worker, with committed per-input availability published for presentation.
- Electrical port rows in `WorkshopPorts` for every supplied part, and the contextual Supply→PowerIn wiring UI.
- A typed electrical source declaration (enable state) and runtime enable commands, if adopted (open question 4).
- FiniteLedger: finite capacity/power/depletion — defaults approved 10 Oct 2026 (3,600 J, 120 W, full/enabled); implementation and powered-work verification remain outstanding.

**Element dependencies**
- Needs a supplied consumer to be observable. The roadmap and [current-consumers](../invest/current-consumers.md#cat-005-i) name Motor (CAT-042, Story 11.1); the Story 8.1 AC names Motor. The legacy tests used Powered gate (CAT-051, Story 8.2) as their supplied load (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L15-L15`). Both come after Story 8.1: the Powered gate directly after it in Epic 8, the Motor in Epic 11 (open question 2).
- Every supplied contact (CAT-013, 020, 024–027, 033, 037, 052) and the CAT-063 Switch PowerIn→Supply mode depend on this network.

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

## 6. Open questions

1. **Finite energy — defaults resolved 10 Oct 2026.** Owner approved 3,600 J capacity, 120 W power limit, initially full and enabled, using the energy/power model. This is an owner decision, not a legacy value or runtime completion claim. EL-196 declares depletion/accounting. The current bounded implementation admits capacity 60–14,400 J, maximum output 10–480 W, initial fraction 0–1 and a typed Enabled/Disabled setting. These are enforced implementation design bounds, distinct from the owner-approved defaults; they do not imply separate owner approval of every bound.
2. **First consumer.** Bumper PowerIn is the approved direct finite-store consumer for the Story6.2 recharge prerequisite. It does not complete the following Motor/network obligation: The Story 8.1 AC uses an Electric Motor "with continuous 12V supply", but Motor is Story 11.1 (Epic 11) and the Signal lamp is activation-only (CAT-035). The legacy tests' supplied load was the Powered gate (CAT-051), now Story 8.2, directly after the Battery in Epic 8. Candidate consumers are Motor and Powered gate. Which consumer qualifies Story 8.1, and whether it is pulled forward: unspecified — owner decision.
3. **"12V" — model resolved 10 Oct 2026.** Owner approved an energy/power model; the old voltage wording does not require a voltage simulation.
4. **Runtime enable toggle.** Current direct-source implementation exposes a contextual Enabled/Disabled control while Running or Paused. The next electrical phase commits the change; stored consumer work is preserved. Independent integrated and production-origin controls passed Running disable/re-enable, paused reversal, stored-work preservation and Reset under Story6.2.
5. **Committed supply indicator.** Current direct-source implementation uses a slate/gold supply terminal and four charge marks on the cream band, driven by committed state through generic animation. This is new implementation, not legacy evidence.
6. **"Disconnecting the cable" during Run.** The Story 8.1 AC says "disconnecting the cable stops the motor immediately". The legacy never edited wires during Run: supply loss was always an activation command through the latch fixture (N19). Whether wire edits during Run are allowed, or the AC means supply loss through a contact (or an edit between Runs): unspecified — owner decision.
