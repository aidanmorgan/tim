# EL-167 · Solenoid tap — named-identity spec

**Current delivery (24 October 2026):** [EL-167 delivery slice](../invest/named-elements.md#element-167). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. The tap tube, mouths and seat are in [EL-003](EL-003-tap.md). All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-167 · Solenoid tap · Water |
| Anchors | [element-167](../requirements.md#element-167), co-anchored `sequence-task-720`; source record [campaign coverage](../requirements.md#campaign-element-coverage); ledger [element-167](../invest/named-elements.md#element-167), owner S460 |
| Disposition | Member of the Family slice "Actuated taps" with [EL-166](EL-166-mechanically-actuated-tap.md), after [EL-003](EL-003-tap.md). A variant of the Tap, not a duplicate: it adds a driven plunger body, a supply input and a control input, and its seat follows the plunger's committed travel (L14 b) |
| Planned package | id `solenoid_tap`, directory `elements/SolenoidTap/`, title "Solenoid tap" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-JOINT-KINDS` (spring rest coordinate), `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` (Delivery) |
| Related | [EL-003](EL-003-tap.md), [EL-166](EL-166-mechanically-actuated-tap.md), [EL-018](EL-018-electronic-level-switch.md) and the meters [EL-030](EL-030-flow-meter.md) to [EL-032](EL-032-pressure-meter.md) (typical command sources), Powered gate, Both gate, Battery, Hold timer (delivered); [EL-035](EL-035-wick.md) and the meters use it to switch a supply during Run |

## Declaration

Closest delivered shapes: the Powered gate for a spring-returned body opened by a supplied prismatic drive (`elements/PoweredGate/PoweredGateElementDeclaration.cs:76`–`:90`), and the Both gate for a control input that carries a condition and funds no work (`elements/BothGate/BothGateElementDeclaration.cs:34`–`:41`).

- **Interfaces.** `ISceneGeometryElement`, `ISliderElement`, `ISupplyElement`, the liquid fitting and seat declaration of `ENGINE-LIQUID` (W4, W7, W14).
- **Body 0 (static), two colliders.** The Tap's Tube (bore radius 0.10, outer radius 0.14, length 0.30, axis X) and a coil housing box 0.16 × 0.16 × 0.16 at (0, 0.22, 0). Family vessel material, the material of every pipe-bore tube (EL-001: restitution 0.12, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s).
- **Body 1 (dynamic plunger pin), one box.** 0.04 × 0.12 × 0.04, 0.1 kg, centred at (0, 0.30, 0) when closed: 0.06 m stands above the housing top. Inertia by the box rule: 0.000133, 0.000027 and 0.000133 kg·m² about X, Y and Z. Material restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. Gravity (0, −9.81, 0). The pin and housing do not collide with each other.
- **Slider.** `PrismaticSliderDescriptor` along the body's +Y, travel 0 to 0.05 m, starting offset 0 (the pin starts on its closed stop). Return spring 60 N/m, damping 2 N·s/m, spring rest coordinate −0.05 m, outside the travel: the spring presses the pin shut with 60 × 0.05 = 3 N at travel 0 and 60 × 0.10 = 6 N at full travel. A prismatic slider declares its spring rest coordinate, which may lie outside its travel; `SliderTravel` admits it; the starting offset stays a separate value (owner `ENGINE-JOINT-KINDS`, owner decision 6 and coordinator decision 21). Today this is refused, not merely absent: `CuriousContraptions.Bundle/MechanicalValueDomain.cs:81` returns false when `lower > 0f || upper < 0f`, so the spring's rest (coordinate 0) must lie inside the travel; `Compression` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:111`) is the starting offset (`MechanicalValueDomain.cs:80`, `engine/gpu/WorkshopPhysicsCompiler.cs:159`), not a rest position.
- **Drive.** A supplied prismatic drive on that slider: target 0.05 m, maximum speed 0.5 m/s, stopping acceleration 10 m/s², maximum effort 10 N, maximum power 5 W.
- **Supply graph.** One logic node `LogicGateOperation.Or` and one consumer whose target is the drive. `PowerIn` binds the logic node's power terminal; `FirstIn` binds its first condition; its second condition is unbound and so never live. One package-internal edge joins the logic output to the consumer. The drive is therefore supplied only while `PowerIn` is supplied **and** `FirstIn` is live, and only the `PowerIn` source pays. A logic node with one unbound condition terminal is admitted today: validation checks only that the operation is defined (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:63`–`:65`) and that every Electrical port is bound (`:151`), and the solver reads an unbound terminal as not live (`engine/gpu/SupplyRoute.cs:370`–`:376`).
- **Liquid node.** The Tap's fitting with one seat: s = clamp(travel / 0.05, 0, 1) from the committed slider coordinate (L14 b).
- **Mouths (two sockets).** As the Tap: `Start`, `End`, pipe bore, Bidirectional, capture 0.20 m and 20° (L5).
- **No store, so no thermal node.** Owner decision 20 attaches a thermal node to every liquid store; the fitting holds none. No row reads temperature.

| `PortId` | Role | Domain | Direction | Local position, normal |
| --- | --- | --- | --- | --- |
| `PowerIn` | `PortRole.PowerIn` | `SimulationDomain.Electrical` | Input | (0, 0.22, 0.08), +Z |
| `FirstIn` | `PortRole.FirstIn` (label "Command") | `SimulationDomain.Electrical` | Input | (−0.08, 0.22, 0), −X |

- **Parameters.** None. It is closed without power (D13, D19).
- **Presentation.** `Visual/Pin` follows body 1's pose. `Visual/CommandLamp` albedo from `PresentationDriver.CommittedElectricalSocket` on `FirstIn`; `Visual/PowerLamp` from the same driver on `PowerIn`, slate to gold, as the Both gate's lamps. Validation admits this driver only on a socket bound to a logic node (`SupplyBundleValidation.cs:186`–`:196`), which both sockets are.
- **Palette.** `ElementCategory.Control`; order 411 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour gold.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Body` | Tube 0.30 long, outer radius 0.14, cream `#fff8e9` | solid; the Tube collider | body 0 pose (static) |
| `Visual/Coil` | Box 0.16 × 0.16 × 0.16, navy `#293954`, with gold windings printed flat | solid; its box collider | body 0 pose |
| `Visual/Pin` | Box 0.04 × 0.12 × 0.04, gold `#f7cb52` | solid; the pin box collider | body 1 pose |
| `Visual/CommandLamp`, `Visual/PowerLamp` | Flush discs beside their sockets | decorative: printed flat (glow) | presentation binding |
| Locators | two mouths, `PowerIn`, `FirstIn` | none | nothing |

Icon: the Tap with a dark cube on top, a pin rising from it and two sockets. Open and closed read from the pin's height, not from a lamp colour. It differs from the Lever tap by the cube and pin.

### Behaviour and numbers

- **Opening.** With supply and command, the drive lifts the pin. Upright it must overcome 0.98 N of weight and 3 to 6 N of spring: at most 6.98 N, under the 10 N effort. At 0.5 m/s that is 6.98 × 0.5 = 3.5 W, under the 5 W bound. The 0.05 m travel on the declared profile takes 0.05 s to speed up (0.5 / 10), 0.05 s at speed and 0.05 s to stop: **0.15 s**.
- **Closing.** With the supply or the command removed the drive is unsupplied; spring and weight shut the pin in under 0.1 s (at least 3.98 N on 0.1 kg upright).
- **Every orientation.** Upright the weight adds to the spring: 3 + 0.98 = 3.98 N holds it shut. On its side the spring alone gives 3 N. Inverted the weight opposes the spring: 3 − 0.98 = 2.02 N still presses it shut. Powered and inverted, opening needs at most 6 − 0.98 = 5.02 N. That is why the spring's rest coordinate lies beyond the stop (owner decision 6).
- **A true command without supply cannot open it.** The logic node passes no energy from `FirstIn` (G1): the consumer is unsupplied and the pin stays shut.
- **Supply without command** leaves it shut for the same reason.
- **Flow.** The Tap's law with s from the travel ([EL-003](EL-003-tap.md)): open on a full Water tank, 0.0482 m³/s.
- **Energy.** Holding open costs the drive's committed work, zero once the pin rests on its stop; opening costs about 6 N × 0.05 m = 0.3 J. 5 W is 0.042 J per tick against the default Battery's 1 J per tick. Debiting what was spent needs `ENGINE-SUPPLY-DEMAND`; today a supplied drive is not debited.
- **A blocked pin.** A Wall block over the pin stops it: the seat stays shut and the drive reports Blocked.
- **Liquid exerts no force on the pin (L12 c).**
- **Tick order.** The electrical solve of a tick sets the drive's supply; the drive acts in that tick's substeps; the seat reads the travel committed by the previous substep (L16). A command from a Level switch or a meter is read one tick after it commits (D7).
- **Switching a supply during Run.** A Tap's opening is fixed during Run (`EL-003-tap.md:35`); a level that must start or stop a flow mid-Run uses this part, commanded by a Battery whose `enabled` Run control is changed, or by a Hold timer's supplied contact.

## Engine capabilities

Map families (navigation only, D14): ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Tube; W21 pin body | present | bodies |
| W18 prismatic slider with limits; supplied prismatic drive within declared speed, effort and power | present | pin |
| G1 supply logic node with a package-internal edge to a consumer; a condition input funds no work | present | `PowerIn`, `FirstIn` |
| W22 authored state, wire refusal | present | pose, ports |
| W4 fluid port contract | present, untested | fitting |
| `CommittedElectricalSocket` lamp on a socket bound to a logic node (`SupplyBundleValidation.cs:186`–`:196`) | present, untested | lamps (no test of this lamp form on a part other than a gate was opened) |
| A logic node with one unbound condition terminal (`SupplyBundleValidation.cs:63`–`:65`, `:151`; `engine/gpu/SupplyRoute.cs:370`–`:376`) | present, untested | `SecondIn` unbound; no test covers a one-condition node, rows E1 to E3 and E9 are its first |
| W3 sockets and joins | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 2 sockets (`Start`, `End`), 0 immersed ports, 0 outlets |
| G8 spring rest coordinate outside the travel | narrower: refused today (`MechanicalValueDomain.cs:81`) | `ENGINE-JOINT-KINDS` |
| W7 fitting, seat, flow; W8 free end | missing | `ENGINE-LIQUID` |
| W14 seat opening from a committed prismatic coordinate | missing | `ENGINE-LIQUID` |
| Debit of the drive's committed work | missing | `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`) |

Counts: 4 present, 3 untested, 2 narrower, 3 missing. No store, so no thermal-node row. Each missing or narrower capability has one owner: liquid (`ENGINE-LIQUID`), sockets (`ENGINE-CAPABILITY-INSTANCES`), debit (`ENGINE-SUPPLY-DEMAND`), spring rest coordinate (`ENGINE-JOINT-KINDS`).

### Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, three colliders, one slider with a drive, one supply graph of two nodes and one edge, one fitting with one seat, two sockets, two ports, one body binding and two lamp bindings. No valve state in code.
2. **Generic laws.** Opening is the delivered supplied drive against a declared spring. "Command without supply cannot open" is the delivered supply logic. The seat is a clamped linear map of a committed coordinate. Closing in every orientation is the spring's declared rest coordinate, a general slider value.
3. **Three layers.** *Colliders:* Tube, housing, pin. *Visuals:* the pin art follows its body. *Animation:* two lamps read committed sockets.
4. **One-way flow and typing.** Electrical → drive → coordinate → seat; nothing flows back. Port ids, roles and the logic operation are the delivered enums.
5. **Flat-state counts.** Bodies 2 (1 static, 1 dynamic); colliders 3 (1 Tube, 2 boxes); joints 1 prismatic; drives 1; sensors 0; signal nodes 0; supply nodes 2 (1 logic, 1 consumer); liquid nodes 1 fitting; seats 1; thermal nodes 0 (no store); sockets 2; immersed ports 0; outlets 0; ports 2; bindings 1 body + 2 lamps; parameters 0.
6. **Performance fitness.** Three colliders against the Powered gate's far larger set. The pin is 0.04 m thick and moves at most 0.5 m/s: 4 mm per tick. A free `End` adds one stream's fixed sample paths (L18).
7. **Tests are scenarios.** Rows E1 to E9.

## Sources and legacy

- Requirement: "Independent supply moves a valve on a control input"; outcome "A true command without supply cannot open the valve." No variants.
- Campaign coverage row as EL-166. Component research [water](../../component-research.md#water): "actuated variants need their lever or supply, never free water"; recipe Dark at High Tide.
- **Legacy.** None at `a6c914e`; the only valve legacy is gas-domain. **Files harvested:** none.

## Acceptance outline

Authority: [element-167](../requirements.md#element-167). Tolerances L17. Planned class `SolenoidTapTests`. The supply is a full Water tank joined to `Start`; `End` is free. Battery A feeds `PowerIn`; Battery B feeds `FirstIn`.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| E1 | Given both Batteries enabled, when Run advances 1 s, then the pin travel is 0.05 m within 0.005 m and the committed flow is 0.047 m³/s within 10% | `SolenoidTapTests.SupplyAndCommandOpenTheValve` | E2, E3 | — |
| E2 | Given Battery B only, when Run advances 10 s, then the travel stays under 0.002 m and the tank keeps 1.0 m³ within 0.004 m³ | `SolenoidTapTests.ACommandWithoutSupplyCannotOpenIt` | E1 | — |
| E3 | Given Battery A only, when Run advances 10 s, then the same | `SolenoidTapTests.SupplyWithoutACommandLeavesItShut` | E1 | — |
| E4 | Given E1, when Battery B is disabled during Run (its `enabled` Run control), then the travel is under 0.002 m within the stall guard 10 s and every committed flow is then under 0.0005 m³/s (L17) | `SolenoidTapTests.RemovingTheCommandShutsIt` | — | disabling Battery A instead: the same |
| E5 | Given E1 run 10 s, then Battery B's remaining charge equals its capacity | `SolenoidTapTests.TheCommandSourceIsNotCharged` | Battery A is the bound source | — |
| E6 | Given the part mounted upright, on its side and inverted, unpowered, then the travel stays under 0.002 m in each; powered and commanded, it opens to 0.05 m within 0.005 m in each | `SolenoidTapTests.ItClosesInEveryOrientation` | — | inverted is the least margin (2.02 N) |
| E7 | Given a Wall block resting over the pin and both Batteries connected, then the travel stays under 0.005 m and every committed flow is under 0.0005 m³/s | `SolenoidTapTests.ABlockedPinStaysShut` | E1 | — |
| E8 | Given a Catch basin fed by this tap's stream, with a Level switch in it whose contact feeds `FirstIn` through a delivered inverting gate, when the basin reaches the switch level, then the tap shuts and the basin stops within 0.03 m³ of that level | `SolenoidTapTests.ALevelSwitchShutsTheTap` | switch unwired: the basin overflows | — |
| E9 | Given a run, then Reset restores a closed pin and a full tank; save and load keep the pose, joins and both wires; the bundle admits the logic node with `SecondIn` unbound; a signal wire offered to `FirstIn` is refused | `SolenoidTapTests.ResetSaveLoadAndWireRefusal` | — | — |

Hand calculations. E1: the pin opens in 0.15 s; by 1 s the tank has fallen to (1 − 1/41.5)² = 0.952 m and the flow is 0.0482 × √0.952 = 0.047 m³/s. E4: closing takes under 0.1 s; 10 s is the 10 s minimum guard.

Generic cover: W22. No parameter. E8 needs EL-018 and EL-004 and runs when they are delivered. A row asserting the energy debited for an opening (0.3 J within 50%) is added with `ENGINE-SUPPLY-DEMAND`. Chrome recipe (pending): snap it to a tank, wire one Battery to Power and another through a switch to Command.

## Open questions

All resolved; none needs the owner.

1. The control input's domain: Electrical, as the gates' condition inputs. A level-like command is what a Level switch, a meter contact or a Hold timer supplies.
2. Normally open variant: none (D19). A delivered inverting gate on the command gives it.
3. Proportional opening: none; the drive has one target.
4. Hold power: the drive's committed work only; no separate holding drain is invented here.
5. Spring rest position: a declared rest coordinate beyond the stop, owned by `ENGINE-JOINT-KINDS` (owner decision 6 and coordinator decision 21); no orientation limit.

## Integration

- **Works with.** Battery, Both gate and the other electrical gates, Hold timer, Signal lamp (delivered); EL-001, EL-003, EL-018, EL-030 to EL-032, EL-035 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. First taught at **64**, after the Electronic level switch (63) that row E8 uses (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)). Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Family slice "Actuated taps" (EL-166, EL-167), after EL-003.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20), plus `ENGINE-JOINT-KINDS` (the spring rest coordinate) and `ENGINE-SUPPLY-DEMAND` (in the programme; the debit row only); then EL-001, EL-003, and for row E8 EL-004 and EL-018 (which needs `ENGINE-SIGNAL-NODES`); then S7.
- **Files.** Adds `elements/SolenoidTap/SolenoidTap.csproj`, `SolenoidTapElementDeclaration.cs`, `solenoid_tap.tscn`, `icon.svg`, `CuriousContraptions.tests/SolenoidTapTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only if the probe finds the disqualifiers clear.
- **Disqualifiers the slice's probe must re-check.** A logic node with one unbound condition terminal no longer admitted (today it is, `SupplyBundleValidation.cs:63`–`:65`, `:151`); a seat that cannot read a prismatic coordinate; a slider spring rest coordinate outside the travel still refused (`MechanicalValueDomain.cs:81`) after `ENGINE-JOINT-KINDS`.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: the ledger entry links only the campaign coverage row; `sequence-task-720` is co-anchored with `element-167`, not with a todo; no `s460` anchor.
