# CAT-024 · electrical_nand — declaration readiness spec

Story 7.0 Batch B declaration spec (CAT-024-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-024](../requirements.md#current-cat-024). All five supplied gates share one declaration: shared gate facts G1–G15 are in [CAT-013](CAT-013-both_gate.md#4-legacy-harvest) and network facts N1–N24 in [CAT-005](CAT-005-battery.md#4-legacy-harvest). This spec adds only what is specific to Nand.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-024 · `electrical_nand` (operation Nand) |
| Requirement anchor | [current-cat-024](../requirements.md#current-cat-024); retained behaviour [todo-362](../requirements.md#todo-362) |
| Mapped identities | [EL-137 Electrical NAND gate](../invest/named-elements.md#element-137). No TH/RAD/GAP identity. |
| Roadmap story | 9.7 "Electrical Logic Gates NAND & NOR" (Epic 9), shared with CAT-025 |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Same as CAT-013: static box 1.7 × 1.3 × 0.65 m and foot 1.85 × 0.16 × 0.85 m at y −0.75 ([CAT-013 §2](CAT-013-both_gate.md#2-declaration)). |
| Mass and material | Static; legacy static default material restitution 1, bounce threshold 0.1 m/s, friction 0.3 ([CAT-013 §2](CAT-013-both_gate.md#2-declaration)). |
| Constraints and joints | none |
| Typed sockets and ports | Same four electrical ports as CAT-013: `FirstIn`, `SecondIn`, separate `PowerIn` supply, `Supply` output. |
| Sensors and activation | none |
| Work and energy stores | none. A logically true Nand with no inputs (row 00) still needs real `PowerIn` supply; it never creates power ([todo-362](../requirements.md#todo-362)). |
| Parameters | `Operation = Nand` (enum value 4), fixed by catalogue kind (`parts/scenes/electrical_nand.tscn@a6c914e:L1-L5`). |
| Cosmetic curves and UI bindings | Same three lamps as CAT-013 (inputs follow committed availability; output follows truth ∧ supplied). |
| Art | Shared body, foot, cream face, input marks and port spheres (CAT-013). Nand shows its toolbox pictogram on the face and gold truth-row dots for rows 00, 01, 10 at x −0.21, −0.07, 0.07 (y −0.4, z 0.42) (G13). Toolbox icon (current, survives): `ui/WorkshopIcons.cs@a6c914e:L79-L79`. Colour `#66b8c9`. Design: [DESIGN.md · Supplied electrical logic](../../../DESIGN.md#supplied-electrical-logic). |
| Catalogue / inventory | Id `electrical_nand`, Title "Not both gate", Category Control, colour (0.4, 0.72, 0.79), no parameters; Description: "Passes the separate bottom supply when the numbered inputs are not both powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`parts/catalog/electrical_nand.tres@a6c914e:L1-L11`). No level places or grants it. |

## 3. Engine capabilities

Families ([map row](../general-engine-element-map.md)): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction (P0-022/023 shared evaluator/feedback registration).

- **Exists now:** as [CAT-013 §3](CAT-013-both_gate.md#3-engine-capabilities).
- **Missing:** supply network (Story 8.1); gate element, `FirstIn`/`SecondIn` sockets, electrical-input cosmetic source and nonmonotone-feedback rejection (Story 9.6, first gate). Story 9.7 adds only the Nand operation value, pictogram and truth dots on top of 9.6.
- **Dependencies:** CAT-005 Battery; CAT-013 Both gate declaration (Story 9.6) first; a supplied consumer (CAT-013 open question 1); input "power switches" need the CAT-063 Switch supply mode (open question 1 here).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| D1 | Nand truth rows (a, b) = 00 T, 01 T, 10 T, 11 F. | `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L14-L14` | carry forward |
| D2 | Evaluation Nand = ¬(a ∧ b). | `engine/LogicGate.cs@a6c914e:L17-L17` | carry forward |
| D3 | Scene sets `Operation = 4` (Nand). | `parts/scenes/electrical_nand.tscn@a6c914e:L1-L5` | carry forward the operation; do not carry forward the integer scene export (closed enum) |
| D4 | Catalogue entry, title and description. | `parts/catalog/electrical_nand.tres@a6c914e:L1-L11` | carry forward |
| D5 | The supplied load is a Powered gate (`powered_gate`, CAT-051) in the legacy tests (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L15-L15`). With supply absent every row, including the true 00 row, leaves the load unpowered; with supply present the load follows D1; reversed link order gives the same result. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L26-L62` | carry forward |
| D6 | Reconvergence: battery feeds Nand `FirstIn`; an Or gate feeds Nand `SecondIn`. Output starts powered; wiring the battery into the Or's input retracts the Nand output within the same solve, and it stays retracted. Both id orders. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L63-L90` | carry forward |
| D7 | A zero-delay loop Nand → Or → Nand rejects with the feedback error and no part receives power (no partial commit). | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L91-L109` | carry forward |
| D8 | Feedback through a potential (open) contact into a Nand is rejected at construction, before any solve; the error names the cycle nodes. | `CuriousContraptions.tests/BinaryCircuitTests.cs@a6c914e:L41-L50` | carry forward (covers "even through open switched routes") |
| D9 | Nand is one of the three operations forbidden in a cycle. | `engine/BinaryCircuit.cs@a6c914e:L104-L112` | carry forward |
| D10 | Truth-row dot positions derive from the evaluated truth table, so Nand shows three dots. | `parts/ElectricalLogicPart.cs@a6c914e:L67-L70` | carry forward |
| D11 | Committed lamp behaviour for every Nand row with and without supply. | `CuriousContraptions.tests/ElectricalAnimationTests.cs@a6c914e:L38-L100` | carry forward behaviour (CAT-013 G14) |

`reference/P0-022-before/docs/coverage/engine/*.json` list `electrical_nand` only as a catalogue id: no element knowledge.

### Files harvested

- `parts/ElectricalLogicPart.cs`
- `parts/catalog/electrical_nand.tres`
- `parts/scenes/electrical_nand.tscn`
- `engine/LogicGate.cs`
- `engine/BinaryCircuit.cs`
- `CuriousContraptions.tests/LogicGateTests.cs`
- `CuriousContraptions.tests/ElectricalLogicTests.cs`
- `CuriousContraptions.tests/BinaryCircuitTests.cs`
- `CuriousContraptions.tests/ElectricalAnimationTests.cs`

## 5. Acceptance outline

Authority: [CAT-024](../requirements.md#current-cat-024), [todo-362](../requirements.md#todo-362); Story 9.7 adds `tools/e2e/cat-024-025.test.ts`.

- **Construction (Chrome UI).** As CAT-013: battery to the bottom supply socket via the icon choice, condition sources to the one-mark/two-mark sockets, gate→consumer.
- **Positive.** Rows 00, 01, 10 with supply power the consumer; output lamp gold.
- **Negative / controls.** Row 11 with supply; row 00 without supply (true but unpowered: D5); output retraction on reconvergence (D6).
- **Boundaries.** A zero-delay Nand loop, including one closed only through an open switch, stays in build mode with the short bottom-status explanation (D7, D8); reordered placement and wiring.
- **Run/Reset; Save/Load.** As CAT-013.
- **Integrations.** Chained with an Or gate (D6); its own pictogram and three truth dots visible at projected size.

## 6. Open questions

1. **Input "power switches".** The Story 9.7 AC wires the gates "to input power switches"; the current Switch (CAT-063) is activation-only and its PowerIn→Supply mode is not scheduled before Epic 9. Whether 9.7 uses batteries or switched supplies for the conditions: unspecified — owner decision.
2. Consumer for the output: as [CAT-013 open question 1](CAT-013-both_gate.md#6-open-questions).
