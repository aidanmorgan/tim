# CAT-013 · both_gate — declaration readiness spec

Story 7.0 Batch B declaration spec (CAT-013-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-013](../requirements.md#current-cat-013). This spec holds the **facts shared by all five supplied electrical gates** (one declaration, five operations); [CAT-024](CAT-024-electrical_nand.md), [CAT-025](CAT-025-electrical_nor.md), [CAT-026](CAT-026-electrical_or.md) and [CAT-027](CAT-027-electrical_xor.md) add only their operation-specific facts. Network-wide facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-013 · `both_gate` (operation And) |
| Requirement anchor | [current-cat-013](../requirements.md#current-cat-013); retained behaviour [todo-362](../requirements.md#todo-362) |
| Mapped identities | [EL-133 Electrical AND gate](../invest/named-elements.md#element-133). No TH/RAD/GAP identity. |
| Roadmap story | 9.6 "Dual-Supply Both Gate" (Epic 9) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root box 1.7 × 1.3 × 0.65 m at the origin and a foot box 1.85 × 0.16 × 0.85 m centred at y −0.75 (`parts/ElectricalLogicPart.cs@a6c914e:L45-L46`). Both are collision boxes (legacy `AddBox`, see [CAT-005](CAT-005-battery.md#2-declaration)); all five gates share this geometry. |
| Mass and material | Static. Legacy static default material: restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`). |
| Constraints and joints | none |
| Typed sockets and ports | Four electrical ports (`parts/ElectricalLogicPart.cs@a6c914e:L28-L34`): `FirstIn` input (−0.94, 0.35, 0) — one raised mark; `SecondIn` input (−0.94, −0.35, 0) — two raised marks; `PowerIn` input (0, −0.75, 0.55) — the separate lower-front supply, not a third condition; `Supply` output (0.94, 0, 0). Gate rule = (operation, FirstIn, SecondIn, PowerIn, Supply) (`parts/ElectricalLogicPart.cs@a6c914e:L35-L36`). Current `WorkshopSocket` lacks `FirstIn`/`SecondIn` (`engine/gpu/WorkshopConnections.cs@a6c914e:L9-L9`). |
| Sensors and activation | none. No activation port; the gate is an electrical interlock, not a source or memory (`parts/ElectricalLogicPart.cs@a6c914e:L10-L10`). |
| Work and energy stores | none. Output energy comes only from `PowerIn`; the A/B inputs carry conditions, never output energy (`engine/BinaryCircuit.cs@a6c914e:L136-L138`). |
| Parameters | `Operation`: closed enum `LogicGateKind { And, Or, Xor, Nor, Nand }` (`engine/LogicGate.cs@a6c914e:L6-L6`), fixed per catalogue kind, not player-configurable; both_gate = And (scene sets no value, so the enum default 0 = And: `parts/scenes/both_gate.tscn@a6c914e:L1-L4`). No numeric parameter. |
| Cosmetic curves and UI bindings | Three lamps, each slate `#556573` → gold `#f7cb52`, 0.1 s SmoothStep presentation transition, endpoint-driven (`parts/ElectricalLogicPart.cs@a6c914e:L22-L27`, `parts/ElectricalLogicPart.cs@a6c914e:L72-L90`): FirstIn lamp at (−0.48, 0.3, 0.42) follows committed `FirstIn` availability; SecondIn lamp at (−0.48, −0.3, 0.42) follows `SecondIn` availability; output lamp at (0.48, 0, 0.42) follows owner active = truth AND supplied (`parts/ElectricalLogicPart.cs@a6c914e:L37-L41`). Lamps r 0.09. Current feedback sources lack an electrical-input source (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`). |
| Art | Body in catalogue colour `#66b8c9` (0.4, 0.72, 0.79); navy `#293954` foot; cream `#fff8e9` face 1.45 × 1.05 × 0.04 at z 0.35 (`parts/ElectricalLogicPart.cs@a6c914e:L45-L47`). And only: two navy converging path bars 0.65 × 0.035 × 0.025 at (−0.05, ±0.15, 0.395), rotated ∓27° about Z (`parts/ElectricalLogicPart.cs@a6c914e:L48-L54`). Input marks: one navy tick 0.025 × 0.08 × 0.025 at (−0.48, 0.47, 0.4) for FirstIn; two ticks at x −0.58 and −0.42, y −0.47 for SecondIn (`parts/ElectricalLogicPart.cs@a6c914e:L91-L93`). Gold port spheres r 0.075 at every socket (`parts/ElectricalLogicPart.cs@a6c914e:L94-L94`). Selection ring radius 1.1 (`parts/ElectricalLogicPart.cs@a6c914e:L44-L44`). Toolbox icon (current, survives): `ui/WorkshopIcons.cs@a6c914e:L61-L61`. Design: [DESIGN.md · Supplied electrical logic](../../../DESIGN.md#supplied-electrical-logic) and the "Both gate" row of [Motion and state feedback](../../../DESIGN.md#motion-and-state-feedback). |
| Catalogue / inventory | Id `both_gate`, Title "Both gate", Category Control, colour (0.4, 0.72, 0.79), no parameters; Description: "Both numbered controls must be powered to pass the separate bottom supply to the output. Connect a battery to the bottom socket and switched conditions to the one-mark and two-mark sockets. No supply means no output." (`parts/catalog/both_gate.tres@a6c914e:L6-L11`). No level places or grants it. |

## 3. Engine capabilities

Families ([map row](../general-engine-element-map.md)): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction. Extra map note: P0-022/023 shared evaluator/feedback registration.

**Exists now**
- Static box bodies/colliders/material: `engine/gpu/PhysicsDeclarations.cs`, `engine/gpu/WorkshopPhysicsCompiler.cs`.
- Electrical connection domain enum: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Declared cosmetic curve record (no electrical source yet): `engine/gpu/WorkshopCosmetic.cs@a6c914e:L16-L48`. It stores its duration as binary16 (`Half`); cosmetic durations are listed as remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L96-L98`), so extending it inherits that debt (open question 3).

**Missing**
- Binary supply network with sources/wires/contacts — Story 8.1 ([CAT-005](CAT-005-battery.md#3-engine-capabilities)).
- Gate elements in that network (typed operation, distinct condition/supply nodes, nonmonotone-feedback rejection at Run) and `FirstIn`/`SecondIn` sockets — Story 9.6 (first gate), reused by 9.7/9.8.
- Cosmetic feedback source for committed electrical input availability and supplied output — Story 9.6.
- Contextual one-mark/two-mark socket choice in the wiring drawer — Story 9.6.

**Element dependencies**: Battery (CAT-005) for supply and conditions; a supplied consumer to observe the output ([current-consumers](../invest/current-consumers.md#cat-013-i) names Motor, CAT-042, Story 11.1; the legacy tests used Powered gate, CAT-051, Story 8.2); switched conditions need the CAT-063 Switch PowerIn→Supply mode (open question 2).

## 4. Legacy harvest

### Shared gate facts (all five operations)

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| G1 | Truth evaluation: And = a∧b, Or = a∨b, Xor = a≠b, Nor = ¬a∧¬b, Nand = ¬(a∧b); an undefined operation rejects. | `engine/LogicGate.cs@a6c914e:L8-L20` | carry forward |
| G2 | Explicit truth table rows (a, b) = 00, 01, 10, 11: And F F F T; Or F T T T; Xor F T T F; Nor T F F F; Nand T T T F. | `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L5-L19` | carry forward (acceptance table) |
| G3 | A gate output is powered only when its supply node is powered AND the operation is true for the committed condition inputs. | `engine/BinaryCircuit.cs@a6c914e:L136-L138` | carry forward |
| G4 | Condition and supply must be three distinct nodes; undefined operation rejects. | `engine/BinaryCircuit.cs@a6c914e:L86-L96` | carry forward |
| G5 | Any cycle containing an Xor, Nor or Nand gate rejects at construction with the cycle's nodes; And/Or cycles are allowed and settle from the unpowered least fixed point. | `engine/BinaryCircuit.cs@a6c914e:L104-L112` | carry forward |
| G6 | Rejected feedback message: "Break the wire loop through <part ids>. XOR, NOR and NAND outputs cannot feed their own inputs." | `engine/ElectricalNetwork.cs@a6c914e:L116-L122` | carry forward the content; DESIGN keeps it in build mode on the bottom status line |
| G7 | All 5 × 4 truth rows × supply on/off × id order: the supplied load — a Powered gate (`powered_gate`, CAT-051) in the legacy tests — is powered iff supply ∧ truth; reconnecting links in reverse order gives the same result; removing every battery link leaves the load unpowered. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L26-L62`, `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L15-L15` and `CuriousContraptions.tests/BinaryCircuitTests.cs@a6c914e:L5-L21` | carry forward |
| G8 | Catalogue map used by all gate tests: And→`both_gate`, Or→`electrical_or`, Xor→`electrical_xor`, Nor→`electrical_nor`, Nand→`electrical_nand`. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L14-L21` | carry forward as a typed mapping |
| G9 | Ports, gate declaration, art, indicators as in section 2. | `parts/ElectricalLogicPart.cs@a6c914e:L22-L36` and `parts/ElectricalLogicPart.cs@a6c914e:L42-L95` | carry forward |
| G10 | Input state is a closed enum `Neither, FirstOnly, SecondOnly, Both` from committed availability. | `parts/ElectricalLogicPart.cs@a6c914e:L8-L8` and `parts/ElectricalLogicPart.cs@a6c914e:L18-L21` | carry forward |
| G11 | Owner active = truth ∧ PowerIn supplied; a `Powered` machine event is recorded the first tick it is active. | `parts/ElectricalLogicPart.cs@a6c914e:L37-L41` | carry forward the state; do not carry forward the per-part `PreparePhysics` update loop (the network commits it) |
| G12 | Operation is a Godot `[Export]` validated as a defined enum. | `parts/ElectricalLogicPart.cs@a6c914e:L13-L17` | carry forward the closed enum; do not carry forward the scene-export mechanism |
| G13 | Non-And gates show their toolbox pictogram (0.65 × 0.65 unshaded quad at z 0.41) and gold `#e8b764` truth-row dots r 0.035 at (−0.21 + 0.14·row, −0.4, 0.42) for each true row (row = 2a + b). | `parts/ElectricalLogicPart.cs@a6c914e:L55-L71` | carry forward |
| G14 | Lamps follow committed availability and output; a failed tick publishes nothing new; pause/hide/Reset/save restore the slate baseline; three electrical input reads exist per gate. | `CuriousContraptions.tests/ElectricalAnimationTests.cs@a6c914e:L38-L100` | carry forward behaviour; do not carry forward the CPU transaction/publication mechanism |
| G15 | An undefined input socket in an animation signal rejects at binding, before Run starts. | `CuriousContraptions.tests/ElectricalAnimationTests.cs@a6c914e:L157-L170` | carry forward |

### Both gate (And) specific

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| A1 | Battery→gate has three compatible pairs (FirstIn, SecondIn, PowerIn), so no suggestion is made and the player must choose. | `CuriousContraptions.tests/BothGateTests.cs@a6c914e:L30-L31` | carry forward |
| A2 | Two chained Both gates feeding a Powered gate load (`powered_gate`, CAT-051): output equals a∧b with no order delay in either id order; supply off (through the latch supply fixture) clears state to `Neither` and output; supply on restores; Reset gives `Neither`, inactive. | `CuriousContraptions.tests/BothGateTests.cs@a6c914e:L9-L55` | carry forward |
| A3 | Two Both gates feeding each other on all three inputs cannot retain power after the real source is removed (10 further ticks). | `CuriousContraptions.tests/BothGateTests.cs@a6c914e:L57-L82` | carry forward |
| A4 | And/Or monotone feedback is the least fixed point: dropping the external condition or opening the contact turns the loop off. | `CuriousContraptions.tests/BinaryCircuitTests.cs@a6c914e:L23-L39` and `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L131-L157` | carry forward |
| A5 | And variant art: converging navy path bars instead of a pictogram. | `parts/ElectricalLogicPart.cs@a6c914e:L48-L54` | carry forward |
| A6 | Catalogue entry and scene (no Operation override). | `parts/catalog/both_gate.tres@a6c914e:L1-L11` and `parts/scenes/both_gate.tscn@a6c914e:L1-L4` | carry forward the data |

`reference/P0-022-before/docs/coverage/engine/*.json` list `both_gate` only as a catalogue id: no element knowledge.

### Files harvested

- `parts/ElectricalLogicPart.cs`
- `parts/catalog/both_gate.tres`
- `parts/scenes/both_gate.tscn`
- `engine/LogicGate.cs`
- `engine/BinaryCircuit.cs`
- `engine/ElectricalNetwork.cs`
- `reference/cpu/MachinePart.cs`
- `CuriousContraptions.tests/BothGateTests.cs`
- `CuriousContraptions.tests/ElectricalLogicTests.cs`
- `CuriousContraptions.tests/LogicGateTests.cs`
- `CuriousContraptions.tests/BinaryCircuitTests.cs`
- `CuriousContraptions.tests/ElectricalAnimationTests.cs`

## 5. Acceptance outline

Authority: [CAT-013](../requirements.md#current-cat-013), [todo-362](../requirements.md#todo-362); Story 9.6 adds `tools/e2e/cat-013.test.ts`.

- **Construction (Chrome UI).** Place Battery(s), Both gate and the consumer; Connect battery→gate and pick the bottom socket from the one-mark/two-mark/supply icon choice (A1); wire the two conditions; Connect gate→consumer.
- **Positive.** Supply plus both conditions powers the consumer; output lamp gold.
- **Negative / controls.** Each other truth row (00, 01, 10); both conditions lit with supply absent leaves the output lamp slate and the consumer unpowered ([DESIGN.md](../../../DESIGN.md#supplied-electrical-logic) positive/negative pair); source-free And loop (A3).
- **Boundaries.** Reordered placement and wiring (A2, G7); reconvergence through a second gate; supply loss during Run through the latch supply fixture retracts the output on the next committed tick (A2; [CAT-005 N19](CAT-005-battery.md#4-legacy-harvest)).
- **Run/Reset; Save/Load.** Reset returns lamps to slate and state to `Neither`; the three wires and their exact socket names round-trip.
- **Integrations.** Chained gates; supply-loss control via the Latch (CAT-037) supply fixture.

## 6. Open questions

1. **Observable consumer.** Story 9.6 AC drives "an output lamp", but the Signal lamp is activation-only (CAT-035: "not a fabricated electrical PowerIn"). Candidate supplied consumers are Motor (CAT-042, Story 11.1) and Powered gate (CAT-051, Story 8.2, the legacy tests' load); the Powered gate precedes Epic 9 and the Motor follows it. Which supplied consumer proves Story 9.6: unspecified — owner decision.
2. **Condition sources.** "Two separate Battery circuits" can feed the conditions directly; switched conditions need the CAT-063 Switch supply mode, not yet scheduled before Epic 9. Whether 9.6 uses batteries only: unspecified — owner decision.
3. **binary16 activation and cosmetic lanes.** The current activation timers, activation times and cosmetic durations are binary16 (`docs/gpu-f32-physics.md@a6c914e:L96-L98`, remaining f32 migration). Whether the gate slice migrates the cosmetic lanes it extends to f32 first, or extends them as they are and leaves migration to the scheduled f32 work: unspecified — owner decision.
4. **"Disconnecting either battery" during Run.** The Story 9.6 AC says "disconnecting either battery extinguishes the lamp" (and Story 8.1 says "disconnecting the cable stops the motor immediately"). The legacy never edited wires during Run; supply loss was an activation command through the latch fixture, never a live topology edit ([CAT-005 N19](CAT-005-battery.md#4-legacy-harvest)), and the network compiles once per Run (N8). No legacy source covers mid-Run disconnection. Whether wire edits during Run are allowed, or the AC means supply loss through a contact or an edit between Runs: unspecified — owner decision.
