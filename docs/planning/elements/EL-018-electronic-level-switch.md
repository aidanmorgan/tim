# EL-018 · Electronic level switch — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-018 delivery slice](../invest/named-elements.md#element-018). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md) and are cited by number. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-018 · Electronic level switch · Water |
| Anchors | [element-018](../requirements.md#element-018), co-anchored `sequence-task-571`; source record [todo-337](../requirements.md#todo-337); integration [sequence-task-387](../requirements.md#sequence-task-387); ledger [element-018](../invest/named-elements.md#element-018), owner S439 |
| Disposition | To deliver as a single element: a supplied contact closed by a liquid depth |
| Planned package | id `level_switch`, directory `elements/LevelSwitch/`, title "Level switch" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-SIGNAL-NODES` (threshold node and its committed phase), `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` (depth probe) |
| Related | [EL-017](EL-017-mechanical-float-valve.md) (unsupplied alternative), [EL-004](EL-004-catch-basin.md), [EL-022](EL-022-water-pump.md) (typical load), EL-167; delivered Battery ([CAT-005](CAT-005-battery.md)), Powered gate ([CAT-051](CAT-051-powered_gate.md), the test load), Pressure plate |

## Declaration

Closest delivered shape: the Pressure plate, a static part whose threshold node closes a route from `PowerIn` to `Supply` (`elements/PressurePlate/PressurePlateElementDeclaration.cs:45`).

- **Interfaces.** `ISceneGeometryElement`, `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), and the liquid depth probe declaration of `ENGINE-LIQUID`.
- **Body.** One static body, mass 0. Origin at the centre of the head.
- **Colliders (two boxes).** Head 0.30 × 0.25 × 0.25 at the origin; probe rod 0.06 × 1.0 × 0.06 at (0, −0.625, 0), hanging from the head. Probe tip at (0, −1.125, 0).
- **Material.** Delivered static default as the Battery (`elements/Battery/BatteryElementDeclaration.cs:63`): restitution 1, friction 0.3, bounce threshold 0.1 m/s.
- **Depth probe.** One declared probe point at the tip. Its committed reading is the depth of liquid above that point in the store whose interior contains it, and 0 when no store contains it. The probe adds no flow, force or demand.
- **Threshold node.** One signal node on that reading: on at `level`, off at `level` − 0.05 m, with a hold of 0.1 s before each change (the delivered contact-load hold, in seconds, D10).
- **Supply route.** `PowerIn` to `Supply`, closed while the node is on (the Pressure plate's form).

| `PortId` | Role | Domain | Direction | Local position, normal |
| --- | --- | --- | --- | --- |
| `PowerIn` | `PortRole.PowerIn` | `SimulationDomain.Electrical` | Input | (−0.15, 0, 0), −X |
| `Supply` | `PortRole.Supply` | `SimulationDomain.Electrical` | Output | (0.15, 0, 0), +X |

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `level` | Real f32 | `ParameterUnit.Metre` (depth above the probe tip) | 0.30 | 0.10 | 1.0 | Reject; build only |

The minimum 0.10 keeps the off level (0.05 m below) above zero.

- **Presentation.** `Visual/Needle` `LocalRotationZ` from the committed probe depth, 0 to 1.0 m, clamped (W12, missing); it moves whether or not the part is supplied. `Visual/Lamp` albedo from a `StateValues` binding naming the part's threshold signal node, slate when off and gold when on: the Pressure plate's form for its studs (`elements/PressurePlate/PressurePlateElementDeclaration.cs:14-25`). The committed phase of the general threshold node is owned by `ENGINE-SIGNAL-NODES`. Like the needle, the lamp shows what the switch senses, supplied or not. (A `CommittedSupply` driver on the `Supply` socket is refused by validation unless the binding names a `SupplySourceDescriptor` node, `CuriousContraptions.Bundle/SupplyBundleValidation.cs:183-185`; this part's node is a contact.)
- **Palette.** `ElementCategory.Control`; palette order 409 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cyan.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Head` | Box 0.30 × 0.25 × 0.25, cyan `#66b8c9` | solid; its box collider | body pose (static) |
| `Visual/Rod` | Box 0.06 × 1.0 × 0.06, navy `#293954`, with cream ticks every 0.1 m printed flat | solid; its box collider | body pose |
| `Visual/Dial` | Cream `#fff8e9` dial with navy ticks on the head's front face | decorative: printed flat | nothing |
| `Visual/Needle` | Gold `#f7cb52` needle on the dial, flush | decorative: printed flat, moved by a binding | presentation binding |
| `Visual/Lamp` | Flush disc on the head's front face | decorative: printed flat (glow) | presentation binding |
| `Loc_Port_PowerIn`, `Loc_Port_Supply` | Locators, no proud mesh | none | nothing |

Icon: a box with a dial and a rod hanging from it. It differs from the Sound meter and other dial boxes by the long rod.

### Behaviour and numbers

- **Sensing and switching are separate.** The needle follows the depth with or without power. The route passes power only when the node is on and `PowerIn` is supplied. So dry and powered, and wet and unpowered, both leave the output dead.
- **Thresholds.** With the default `level` and the tip on a basin's floor: on at 0.30 m of depth, off at 0.25 m, unchanged in between.
- **Timing.** A Catch basin under an open tap rises about 0.047 / 1.25 = 0.038 m/s, so it crosses 0.30 m after about 8 s; the route closes 0.1 s later, plus one tick before the electrical solve reads it (L16, D7). No lesson times this.
- **Which liquid.** Only the store whose interior holds the tip. A tip above a basin's rim, or outside it, reads 0.
- **Load.** The route passes whatever the source grants; a default Battery grants 120 W, at most 1 J per tick, which runs a Powered gate (its drive is bounded at 120 W, [CAT-051](CAT-051-powered_gate.md)) or the Water pump (30 W, [EL-022](EL-022-water-pump.md)). A Signal lamp is a signal load (`ActivationIn` only, `elements/SignalLamp/SignalLampElementDeclaration.cs:35`), so it cannot sit on `Supply`.
- **No activation output.** The requirement asks for a supplied contact. A pulse on the wet edge is what the planned Rising-edge detector (EL-181) makes from this contact.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body and colliders | present | head and rod |
| W17 switched supply route closed by a signal state | present | `PowerIn` to `Supply` |
| W22, W23 authored state, wire refusal, parameter refusal | present | ports and `level` |
| `StateValues` presentation binding on a signal node's committed phase, used by the Pressure plate's studs (`elements/PressurePlate/PressurePlateElementDeclaration.cs:14-25`) | present, untested | lamp; delivered for the contact-load node, no test asserting the stud colour was opened; for the general threshold node the committed phase is `ENGINE-SIGNAL-NODES` (with W16) |
| W16 threshold node on a committed scalar | narrower | delivered for contact load only; the general node is `ENGINE-SIGNAL-NODES` (owner decision 3a; coordinator decision 16) |
| Depth probe at a declared point (a liquid observation kind for W16) | missing | `ENGINE-LIQUID` ("level, flow and volume observations"; the story does not name a probe point, gap) |
| W12 driver for a committed probe depth | missing | `ENGINE-LIQUID` |

Counts: 3 present, 1 untested, 1 narrower, 2 missing. Two owners, one per capability: the threshold node (`ENGINE-SIGNAL-NODES`) and the liquid observation (`ENGINE-LIQUID`).

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, two colliders, one probe point, one threshold node, one supply route, two ports, one parameter, two bindings. No state enum in code: wet and dry are the node's two states.
2. **Generic laws.** The node thresholds a committed scalar with on and off levels and a hold, as the delivered contact-load node does for mass. The route is the delivered switched supply.
3. **Three layers.** *Colliders:* two boxes. *Visuals:* the table above. *Animation:* needle and lamp read committed state.
4. **One-way flow and typing.** Liquid → sensor → signal → next tick's electrical solve (L16). Port ids and roles are the delivered enums.
5. **Flat-state counts.** Bodies 1 (static); colliders 2 boxes; joints 0; drives 0; sensors 1 probe point; signal nodes 1; supply routes 1; ports 2; liquid nodes 0; bindings 2; parameters 1.
6. **Performance fitness.** Two colliders against the Pressure plate's ten. The probe is one point resolved to at most one store at Run compile for static stores; reading it is constant work.
7. **Tests are scenarios.** Rows S1 to S9.

## Sources and legacy

- Requirement: "Separately supplied contact observes actual local fluid height"; outcome "Dry and unpowered controls cannot switch the output." No variants.
- Refinement [S706](../invest/refinements.md#s706) level-switch; component research [water](../../component-research.md#water) lines 18 (separate thresholds, dwell), 33 and 59 (Dark at High Tide).
- **Legacy search.** No level switch. Supplied threshold-sensor analogue (Sound meter):

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Ports `PowerIn`, `Supply`, `ActivationOut`; one route PowerIn → Supply gated by the threshold state. | `parts/SoundMeterPart.cs@a6c914e:L50-L57` | Carry forward the gated route. The activation output is not carried (D19). |
| 2 | Hysteresis as a hidden 0.9 factor. | `parts/SoundMeterPart.cs@a6c914e:L58-L71` | Carry forward hysteresis as explicit on and off levels. |
| 3 | Active only when above threshold and powered; evaluated in the part's own per-substep loop. | `parts/SoundMeterPart.cs@a6c914e:L72-L80` | Carry forward the semantics. Not the per-part loop. |

**Files harvested:** `parts/SoundMeterPart.cs`.

## Acceptance outline

Authority: [element-018](../requirements.md#element-018). Tolerances L17. Planned class `LevelSwitchTests`. Rig: the switch with its tip on a Catch basin's floor; Battery `Supply` → `PowerIn`; switch `Supply` → Powered gate `PowerIn` ([CAT-051](CAT-051-powered_gate.md)); the basin fills from a Water tank and Tap. "The gate is supplied" means its committed actuator state is powered and its blade rises (opening 1.55 m, reached within 1.5 s at 2.8 m/s); "unsupplied" means it stays unpowered with its blade closed (opening 0).

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| S1 | Given the rig, when Run advances until the basin depth passes 0.30 m (stall guard 30 s), then the gate is supplied and its blade rises, and it was not supplied (blade closed) at any sampled tick while the depth was below 0.29 m | `LevelSwitchTests.TheContactClosesAtTheSetDepth` | S2, S3 | — |
| S2 | Given the tap closed (dry), when Run advances 10 s, then the gate is never supplied and its blade stays closed | `LevelSwitchTests.ADryProbeNeverSwitches` | S1 | — |
| S3 | Given the Battery wire removed, when the basin fills past 0.30 m, then the needle shows the depth and the gate is never supplied (blade closed) | `LevelSwitchTests.AnUnpoweredSwitchSensesButCannotSwitch` | S1 | a disabled Battery gives the same result |
| S4 | Given the probe tip outside the basin, when the basin fills, then the reading stays 0 and the gate's blade stays closed | `LevelSwitchTests.TheProbeReadsOnlyTheStoreItIsIn` | S1 | tip above the rim: 0 |
| S5 | Given a Water tank authored at 0.33 m of depth with the probe tip on its floor, draining through a Tap at `opening` 0.125, then the contact closes after the hold, stays closed while the depth passes 0.27 m and opens once it is below 0.25 m (about 14 s; stall guard 45 s; the level declares a Run length of 45 s, `ENGINE-RUN-LENGTH`, owner decisions 13 and 17) | `LevelSwitchTests.TheContactHoldsBetweenItsOnAndOffDepths` | a fresh tank authored at 0.27 m never closes it | 0.01 m either side of each level |
| S6 | Given a Header tank authored at 0.305 m of depth draining through an open Tap, so that the depth passes 0.30 m after about 0.05 s, then the contact never closes | `LevelSwitchTests.ARiseShorterThanTheHoldDoesNotSwitch` | authored at 0.35 m (0.45 s above the level) it closes | — |
| S7 | Given `level` 0.10 and 1.0, both are admitted; outside or non-finite is refused whole | `LevelSwitchTests.LevelIsBounded` | default | next f32 beyond each limit |
| S8 | Given a signal wire offered to `PowerIn` or `Supply`, or `Supply` to `Supply`, then it is refused | `LevelSwitchTests.OnlyElectricalWiresAreAdmitted` | Battery `Supply` → `PowerIn` is admitted | — |
| S9 | Given a run that closed the contact, then Reset opens it and zeroes the needle; save and load keep `level`, the pose and both wires | `LevelSwitchTests.ResetOpensTheContactAndSaveLoadKeepsTheWiring` | — | — |

Generic cover: W22 and W23 show the mechanism; S7 to S9 stay package tests until S4. Later-partner row: stopping a supplied pump (Dark at High Tide) is [EL-022](EL-022-water-pump.md) row M8. Chrome recipe (pending): a Level switch over a basin, Battery and Powered gate wired, the basin filling.

## Open questions

All resolved; none needs the owner.

1. Normally closed mode: no; not in the row (D19). "Pump until full" is built with a planned inverter or by placing the switch on the source vessel.
2. Rising-edge activation output: no (D19).
3. Which store a probe senses: the one whose interior holds the tip; there is no bracket and no binding to a vessel.
4. Two thresholds and a dwell in ticks: one `level` with a fixed 0.05 m band and a fixed 0.1 s hold. A parameter cannot be constrained by another parameter, and durations are seconds (D10).

## Integration

- **Works with.** Delivered: Battery, Powered gate, wires. Planned: EL-004, EL-001, EL-003, EL-022, EL-167.
- **Campaign.** Chapter 7, levels 61–70 (Dark at High Tide); reuse 71–90, 114, 126–130, 136–150. First taught at **63**, before the Solenoid tap (64), whose row E8 uses it (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)). Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-337`; `sequence-task-571` not co-anchored with a todo; no `s439` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-SIGNAL-NODES` for the threshold node and the committed phase its lamp binding reads; `ENGINE-GAS-NETWORK` (the one fluid port record, before `ENGINE-LIQUID`, owner decision 18); `ENGINE-LIQUID` with a probe observation; EL-001; EL-002; EL-003; EL-004; S7.
- **Files.** Adds `elements/LevelSwitch/LevelSwitch.csproj`, `LevelSwitchElementDeclaration.cs`, `level_switch.tscn`, `icon.svg`, `CuriousContraptions.tests/LevelSwitchTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** A threshold node that still accepts contact load only; a probe that reads a named vessel instead of a point; a reading that is not committed before the next tick's electrical solve.
