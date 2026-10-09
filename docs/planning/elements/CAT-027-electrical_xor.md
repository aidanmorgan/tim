# CAT-027 · electrical_xor — declaration readiness spec

Story 7.0 Batch B declaration spec (CAT-027-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-027](../requirements.md#current-cat-027). All five supplied gates share one declaration: shared gate facts G1–G15 are in [CAT-013](CAT-013-both_gate.md#4-legacy-harvest) and network facts N1–N24 in [CAT-005](CAT-005-battery.md#4-legacy-harvest). This spec adds only what is specific to Xor.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-027 · `electrical_xor` (operation Xor) |
| Requirement anchor | [current-cat-027](../requirements.md#current-cat-027); retained behaviour [todo-362](../requirements.md#todo-362) |
| Mapped identities | [EL-135 Electrical XOR gate](../invest/named-elements.md#element-135). No TH/RAD/GAP identity. |
| Roadmap story | 9.8 "Electrical Logic Gates OR & XOR" (Epic 9), shared with CAT-026 |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Same as CAT-013: static box 1.7 × 1.3 × 0.65 m and foot 1.85 × 0.16 × 0.85 m at y −0.75 ([CAT-013 §2](CAT-013-both_gate.md#2-declaration)). |
| Mass and material | Static; legacy static default material restitution 1, bounce threshold 0.1 m/s, friction 0.3 ([CAT-013 §2](CAT-013-both_gate.md#2-declaration)). |
| Constraints and joints | none |
| Typed sockets and ports | Same four electrical ports as CAT-013: `FirstIn`, `SecondIn`, separate `PowerIn` supply, `Supply` output. |
| Sensors and activation | none |
| Work and energy stores | none; output energy only from `PowerIn`. |
| Parameters | `Operation = Xor` (enum value 2), fixed by catalogue kind (`parts/scenes/electrical_xor.tscn@a6c914e:L1-L5`). |
| Cosmetic curves and UI bindings | Same three lamps as CAT-013 (inputs follow committed availability; output follows truth ∧ supplied). |
| Art | Shared body, foot, cream face, input marks and port spheres (CAT-013). Xor shows its toolbox pictogram on the face and gold truth-row dots for rows 01 and 10 at x −0.07 and 0.07 (y −0.4, z 0.42) (G13). Toolbox icon (current, survives; shared with `optical_xor`): `ui/WorkshopIcons.cs@a6c914e:L77-L77`. Colour `#66b8c9`. Design: [DESIGN.md · Supplied electrical logic](../../../DESIGN.md#supplied-electrical-logic). |
| Catalogue / inventory | Id `electrical_xor`, Title "Exactly one gate", Category Control, colour (0.4, 0.72, 0.79), no parameters; Description: "Passes the separate bottom supply when exactly one numbered input is powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`parts/catalog/electrical_xor.tres@a6c914e:L1-L11`). No level places or grants it. |

## 3. Engine capabilities

Families ([map row](../general-engine-element-map.md)): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction (P0-022/023 shared evaluator/feedback registration).

- **Exists now:** as [CAT-013 §3](CAT-013-both_gate.md#3-engine-capabilities).
- **Missing:** supply network (Story 8.1); gate element, sockets, electrical-input cosmetic source and nonmonotone-feedback rejection at Run (Story 9.6). Story 9.8 adds only the Xor operation value, pictogram and truth dots.
- **Dependencies:** CAT-005 Battery; CAT-013 declaration (Story 9.6) first; a supplied consumer (CAT-013 open question 1); condition switches as [CAT-024 open question 1](CAT-024-electrical_nand.md#6-open-questions). The open-switch feedback control (X8) needs a part with an electrical contact (legacy used `switch`; CAT-063 supply mode).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| X1 | Xor truth rows (a, b) = 00 F, 01 T, 10 T, 11 F. | `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L12-L12` | carry forward |
| X2 | Evaluation Xor = a ≠ b. | `engine/LogicGate.cs@a6c914e:L15-L15` | carry forward |
| X3 | Scene sets `Operation = 2` (Xor). | `parts/scenes/electrical_xor.tscn@a6c914e:L1-L5` | carry forward the operation; do not carry forward the integer scene export (closed enum) |
| X4 | Catalogue entry, title and description. | `parts/catalog/electrical_xor.tres@a6c914e:L1-L11` | carry forward |
| X5 | The supplied load is a Powered gate (`powered_gate`, CAT-051) in the legacy tests (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L15-L15`). With supply absent no row powers the load; with supply present the load follows X1; reversed link order gives the same result; removing the battery links unpowers the load. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L26-L62` | carry forward |
| X6 | Reconvergence (chained controls): battery feeds Xor `FirstIn`; an Or gate feeds Xor `SecondIn`. Output starts powered; wiring the battery into the Or retracts the Xor output in the same solve, and it stays off. Both id orders. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L63-L90` | carry forward |
| X7 | Reconvergence settles before a nonmonotone consumer: one source reaching both Xor inputs by different wire paths gives an unpowered Xor output, whatever the wire order. | `CuriousContraptions.tests/BinaryCircuitTests.cs@a6c914e:L52-L63` | carry forward |
| X8 | An open switch cannot hide invalid feedback: Xor output → switch `PowerIn`, switch `Supply` → Xor `FirstIn` rejects at Start with an error naming both parts; the world stays in build mode and the saved machine is unchanged. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L110-L129` | carry forward |
| X9 | A zero-delay loop Xor → Or → Xor rejects with the feedback error and no part receives power. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L91-L109` | carry forward |
| X10 | Potential (open) contact feedback into a Xor rejects at construction with the cycle's node list. | `CuriousContraptions.tests/BinaryCircuitTests.cs@a6c914e:L41-L50` | carry forward |
| X11 | Truth-row dots derive from the evaluated table, so Xor shows two dots. | `parts/ElectricalLogicPart.cs@a6c914e:L67-L70` | carry forward |
| X12 | Committed lamp behaviour for every Xor row with and without supply. | `CuriousContraptions.tests/ElectricalAnimationTests.cs@a6c914e:L38-L100` | carry forward behaviour (CAT-013 G14) |

`reference/P0-022-before/docs/coverage/engine/*.json` list `electrical_xor` only as a catalogue id: no element knowledge.

### Files harvested

- `parts/ElectricalLogicPart.cs`
- `parts/catalog/electrical_xor.tres`
- `parts/scenes/electrical_xor.tscn`
- `engine/LogicGate.cs`
- `engine/BinaryCircuit.cs`
- `CuriousContraptions.tests/LogicGateTests.cs`
- `CuriousContraptions.tests/ElectricalLogicTests.cs`
- `CuriousContraptions.tests/BinaryCircuitTests.cs`
- `CuriousContraptions.tests/ElectricalAnimationTests.cs`

## 5. Acceptance outline

Authority: [CAT-027](../requirements.md#current-cat-027), [todo-362](../requirements.md#todo-362); Story 9.8 adds `tools/e2e/cat-026-027.test.ts`.

- **Construction (Chrome UI).** As CAT-013: battery to the bottom supply socket via the icon choice, conditions to the one-mark/two-mark sockets, gate→consumer.
- **Positive.** Rows 01 and 10 with supply power the consumer.
- **Negative / controls.** Rows 00 and 11 with supply (the Story 9.8 AC: "low when identical"); every row without supply (X5).
- **Boundaries.** Reconvergent and chained controls (X6, X7); zero-delay Xor loops, including one through an open switch, stay in build mode with the short bottom-status message (X8, X9).
- **Run/Reset; Save/Load.** As CAT-013; a rejected Run leaves the saved machine unchanged (X8).
- **Integrations.** Or feeding Xor (X6); its own committed indicator.

## 6. Open questions

1. Condition sources and output consumer: as [CAT-024 open question 1](CAT-024-electrical_nand.md#6-open-questions) and [CAT-013 open question 1](CAT-013-both_gate.md#6-open-questions). No Xor-specific question.
