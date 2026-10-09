# EL-197 · Electrical wire — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Supplied-network facts N1–N21 are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-197 · Electrical wire |
| Type | Electrical |
| Anchor | [requirements.md#element-197](../requirements.md#element-197); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Battery, wire, switch …", first use 11–20); [named-elements entry](../invest/named-elements.md#element-197); owner S303 |
| Related | No CAT kind of its own: it is the Electrical connection every supplied part uses ([CAT-005](CAT-005-battery.md) names it "related, not owned"). Connection-kind siblings: activation links (current), [EL-038 hose](EL-038-pneumatic-hose.md), [EL-048 duct](EL-048-acoustic-duct.md). |
| Roadmap story | 8.1 "Battery DC Power Source & Network Graph" (Epic 8) — the first electrical connection |
| Status | not started (the Electrical domain exists as an enum value but validation rejects every non-activation link) |

## 2. Declaration

The wire is an authored typed connection, not a body.

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | none: a wire has no collider and no spatial query, so two wires that cross in space never connect and never touch balls (outcome). Rendered as a sagging cable between the two sockets' world positions. |
| Mass and material | none. |
| Constraints | none; endpoints follow the named local sockets under each part transform ([DESIGN.md](../../../DESIGN.md#motion-and-state-feedback) battery/motor paragraph). |
| Typed ports | One Electrical Output socket (`Supply`) → one Electrical Input socket of the role-specific kinds below. Resolution: the link's domain on both ends, exactly one matching port at each end, output → input; undefined domains or sockets reject (harvest W1, W2). Record type: `WorkshopConnection(Source, Output, Target, Input, Domain)` (`engine/gpu/WorkshopConnections.cs@a6c914e:L12-L13`). |
| Sensors and activation | none; a wire is not a source, contact or memory. |
| Work and energy stores | none: in the network a wire makes its output node powered whenever its input node is powered (harvest W4); it carries availability (and, with [EL-196](EL-196-battery.md), the load's share of finite power) without loss. |
| Parameters | none. Per-socket wire capacity: unspecified (see open question 1); the current construction caps all connections at 8 (`engine/gpu/WorkshopConnections.cs@a6c914e:L67-L67`). |
| Cosmetic curves and UI bindings | Static cable; optional supply tint is not required. Contextual linking highlights compatible targets and never guesses between several compatible socket pairs ([DESIGN.md](../../../DESIGN.md#motion-and-state-feedback)); when exactly one pair fits it is chosen (harvest W5). |
| Art | Navy `#293954` cable, thicker than the gold `#e8b764` activation links ([DESIGN.md](../../../DESIGN.md#motion-and-state-feedback) "Electrical cables are navy and thicker than gold activation links"); visual diameter 0.05 m **proposed** (visibly thicker than a thin activation link while staying lighter than the 0.18 m sound duct). |
| Catalogue / inventory | Not a drawer part: created by the Connect control from an Electrical output socket. Each socket pair has a distinct label and pictogram (harvest W6). Level budgets for wires: none in the legacy (wires are free) — kept free **proposed** (the legacy levels list no wire inventory; limiting wires would add an untaught rule). |

**Variants** (row: "carry power or control according to their distinct typed contracts"):

| Variant | Target sockets | Network meaning |
| --- | --- | --- |
| Power wire | `PowerIn` of a consumer, gate supply or contact input | Feeds supply; with a finite battery the consumer's demand debits the source (EL-196). |
| Condition wire | `FirstIn` / `SecondIn` of a gate ([EL-133](EL-133-electrical-and-gate.md)–[EL-137](EL-137-electrical-nand-gate.md)), `ConditionIn` of an edge detector ([EL-181](EL-181-rising-edge-detector.md)) | Carries a condition only; the target never passes this energy onward (gate rule `engine/BinaryCircuit.cs@a6c914e:L136-L138`). |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S303): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction).

**Exists now**
- Connection record, domain enum `Electrical = 2`, sockets `Supply = 7`, `PowerIn = 8` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`); construction validation that currently rejects any non-activation domain (`engine/gpu/WorkshopConnections.cs@a6c914e:L89-L92`).
- Contextual Connect UI (select part → "Connect <socket>" → click target → choose socket pair; "Disconnect signal"), which labels electrical sockets "(unsupported)" today (`ui/WorkshopConnections.cs@a6c914e:L20-L26`, `ui/WorkshopConnections.cs@a6c914e:L37-L72`).

**Missing**
- Admitting the Electrical domain, `FirstIn`/`SecondIn`/`ConditionIn` sockets, and the compiled network — Story 8.1 (power wires), Story 9.6 (condition wires); roles and capacities [S257](../invest/decisions.md#s257) electrical-port.
- Cable rendering distinct from activation links — Story 8.1.

**Dependencies.** Battery (CAT-005/EL-196) and at least one supplied part.

## 4. Sources and legacy

- Requirement row [element-197](../requirements.md#element-197): "Compatible terminals carry power or control according to their distinct typed contracts"; outcome "Geometric crossing alone cannot connect wires".
- Named entry [element-197](../invest/named-elements.md#element-197), owner S303; [S257](../invest/decisions.md#s257) electrical-port: "a reversed or incompatible wire is refused at connection and the construction is unchanged; a full-capacity attempt refuses the extra wire".

| # | Legacy fact | Source | Disposition |
| --- | --- | --- | --- |
| W1 | A link resolves only with exactly one matching port per end, same domain, output (or bidirectional) → input; unknown domains, missing or undefined socket ids and self-links reject. | `engine/ConnectionPort.cs@a6c914e:L40-L67` | carry forward |
| W2 | Domain names serialize snake_case (`electrical`); socket ids serialize as exact snake_case names with no aliases. | `engine/MachineData.cs@a6c914e:L67-L70`, `engine/MachineData.cs@a6c914e:L103-L122` | carry forward at the save boundary only |
| W3 | Typed sockets need matching domain, direction and identity; duplicate source ports or a missing socket reject. | `CuriousContraptions.tests/ConnectionPortTests.cs@a6c914e:L18-L38` | carry forward |
| W4 | A wire is (input node → output node); a node is powered if any wire into it is powered; a source-free wire loop stays unpowered; supply loss clears every downstream input. | `engine/BinaryCircuit.cs@a6c914e:L16-L16`, `engine/BinaryCircuit.cs@a6c914e:L73-L78`, `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L137-L181` | carry forward |
| W5 | Connecting battery → motor with no explicit sockets picks the sole compatible pair `Supply → PowerIn`; motor → battery is rejected. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L110-L118` | carry forward |
| W6 | Every electrical socket pair (outputs Supply/ExtendedOut/RetractedOut × inputs PowerIn/FirstIn/SecondIn/ExtendIn/RetractIn) has a distinct label and an actual pictogram (15 pairs); non-electrical pairs reject. | `CuriousContraptions.tests/ConnectionChoiceTests.cs@a6c914e:L9-L34` | carry forward |
| W7 | Removing any one solution wire leaves the motor unpowered and the level unsolved. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L82-L91` | carry forward (control) |
| W8 | `switched_motor` wiring: battery `supply` → switch `power_in`, switch `supply` → motor `power_in`. | `content/puzzles.json@a6c914e:L3857-L3872` | carry forward (Epic 15 input; the file survives) |

- **Files harvested:** `engine/ConnectionPort.cs`, `engine/MachineData.cs`, `engine/BinaryCircuit.cs`, `CuriousContraptions.tests/ConnectionPortTests.cs`, `CuriousContraptions.tests/ConnectionChoiceTests.cs`, `CuriousContraptions.tests/ElectricalTests.cs`, `content/puzzles.json` (switched_motor wiring).

## 5. Acceptance outline

Acceptance authority: [element-197](../requirements.md#element-197), [S257](../invest/decisions.md#s257) electrical-port construction.

- **Construction (actual Chrome UI).** Battery and Motor: select the battery, Connect `Supply`, click the motor — the single pair is offered and a navy cable appears between the actual sockets. For a gate, the socket-choice buttons offer `Supply → PowerIn`, `Supply → FirstIn`, `Supply → SecondIn` with distinct icons.
- **Positive.** Power wire: the motor runs. Condition wire into a gate with supply: the output follows the truth table.
- **Negative / control.** Two wires drawn so they cross on screen between two unrelated circuits: neither circuit gains power (outcome). Motor → battery, electrical → activation socket, output → output: refused, construction unchanged. Delete the power wire: the motor stops.
- **Boundaries.** Connection cap (8 today, or the S257 capacity) refuses the extra wire atomically; a source-free loop of wires stays unpowered.
- **Run/Reset.** Wires are construction data; Reset never changes them.
- **Save/Load.** Both endpoints and exact socket names round-trip; an unknown socket name rejects the whole load.
- **Integrations.** Battery retained behaviour [todo-146](../requirements.md#todo-146) (wired supply), switch retained behaviour [todo-147](../requirements.md#todo-147) (battery → switch → motor wiring) and gate behaviour [todo-362](../requirements.md#todo-362) (condition wires); interaction processes [IX-06 electrical power transfer](../requirements.md#interaction-06) (power wires) and [IX-07 signal propagation](../requirements.md#interaction-07) (condition wires carry no output energy). Campaign: "wire" is named in the power row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 11–20 (reuse 31–50, 61–80, 136–150).

## 6. Open questions

1. **Per-socket capacity.** Whether an input socket accepts several wires (legacy wire-OR) or exactly one, and output fan-out limits: owner decision under S257.
2. **Wire budget.** Free wires (proposed) versus a per-level wire inventory: owner decision.
