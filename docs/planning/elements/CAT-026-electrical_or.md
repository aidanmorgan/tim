# CAT-026 · electrical_or — declaration readiness spec

Story 7.0 Batch B declaration spec (CAT-026-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-026](../requirements.md#current-cat-026). All five supplied gates share one declaration: shared gate facts G1–G15 are in [CAT-013](CAT-013-both_gate.md#4-legacy-harvest) and network facts N1–N24 in [CAT-005](CAT-005-battery.md#4-legacy-harvest). This spec adds only what is specific to Or.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-026 · `electrical_or` (operation Or) |
| Requirement anchor | [current-cat-026](../requirements.md#current-cat-026); retained behaviour [todo-362](../requirements.md#todo-362) |
| Mapped identities | [EL-134 Electrical OR gate](../invest/named-elements.md#element-134). No TH/RAD/GAP identity. |
| Roadmap story | 9.8 "Electrical Logic Gates OR & XOR" (Epic 9), shared with CAT-027 |
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
| Parameters | `Operation = Or` (enum value 1), fixed by catalogue kind (`parts/scenes/electrical_or.tscn@a6c914e:L1-L5`). |
| Cosmetic curves and UI bindings | Same three lamps as CAT-013 (inputs follow committed availability; output follows truth ∧ supplied). |
| Art | Shared body, foot, cream face, input marks and port spheres (CAT-013). Or shows its toolbox pictogram on the face and gold truth-row dots for rows 01, 10, 11 at x −0.07, 0.07, 0.21 (y −0.4, z 0.42) (G13). Toolbox icon (current, survives; shared with `optical_or`): `ui/WorkshopIcons.cs@a6c914e:L76-L76`. Colour `#66b8c9`. Design: [DESIGN.md · Supplied electrical logic](../../../DESIGN.md#supplied-electrical-logic). |
| Catalogue / inventory | Id `electrical_or`, Title "Either gate", Category Control, colour (0.4, 0.72, 0.79), no parameters; Description: "Passes the separate bottom supply when either numbered input is powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`parts/catalog/electrical_or.tres@a6c914e:L1-L11`). No level places or grants it. |

## 3. Engine capabilities

Families ([map row](../general-engine-element-map.md)): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction (P0-022/023 shared evaluator/feedback registration).

- **Exists now:** as [CAT-013 §3](CAT-013-both_gate.md#3-engine-capabilities).
- **Missing:** supply network (Story 8.1); gate element, sockets, electrical-input cosmetic source and cycle rules (Story 9.6). Story 9.8 adds only the Or operation value, pictogram and truth dots.
- **Dependencies:** CAT-005 Battery; CAT-013 declaration (Story 9.6) first; a supplied consumer (CAT-013 open question 1); condition switches as [CAT-024 open question 1](CAT-024-electrical_nand.md#6-open-questions).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| O1 | Or truth rows (a, b) = 00 F, 01 T, 10 T, 11 T. | `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L11-L11` | carry forward |
| O2 | Evaluation Or = a ∨ b. | `engine/LogicGate.cs@a6c914e:L14-L14` | carry forward |
| O3 | Scene sets `Operation = 1` (Or). | `parts/scenes/electrical_or.tscn@a6c914e:L1-L5` | carry forward the operation; do not carry forward the integer scene export (closed enum) |
| O4 | Catalogue entry, title and description. | `parts/catalog/electrical_or.tres@a6c914e:L1-L11` | carry forward |
| O5 | The supplied load is a Powered gate (`powered_gate`, CAT-051) in the legacy tests (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L15-L15`). With supply absent no row powers the load; with supply present the load follows O1; reversed link order gives the same result; removing the battery links unpowers the load. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L26-L62` | carry forward |
| O6 | Or (with And) may sit in a feedback cycle; it is not rejected. Each solve restarts the cycle from unpowered (least fixed point). | `engine/BinaryCircuit.cs@a6c914e:L104-L112` and `engine/BinaryCircuit.cs@a6c914e:L115-L144` | carry forward |
| O7 | Source-free Or loop: two Or gates feeding each other's `FirstIn` stay powered only while the battery feeds them; after the battery links are removed both inputs read unpowered. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L131-L157` | carry forward |
| O8 | Monotone loop with a contact: with sources on and the contact open, an Or gate's output stays on through its other input while the loop nodes go off (an And would go off). | `CuriousContraptions.tests/BinaryCircuitTests.cs@a6c914e:L23-L39` | carry forward |
| O9 | Or is the upstream gate in the reconvergence case: its output feeding a Xor/Nand second input retracts that output in the same solve. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L63-L90` | carry forward |
| O10 | An Or gate in a loop with a Xor/Nor/Nand is still rejected, because the nonmonotone gate is in the cycle. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L91-L109` | carry forward |
| O11 | Truth-row dots derive from the evaluated table, so Or shows three dots. | `parts/ElectricalLogicPart.cs@a6c914e:L67-L70` | carry forward |
| O12 | Committed lamp behaviour for every Or row with and without supply. | `CuriousContraptions.tests/ElectricalAnimationTests.cs@a6c914e:L38-L100` | carry forward behaviour (CAT-013 G14) |

`reference/P0-022-before/docs/coverage/engine/*.json` list `electrical_or` only as a catalogue id: no element knowledge.

### Files harvested

- `parts/ElectricalLogicPart.cs`
- `parts/catalog/electrical_or.tres`
- `parts/scenes/electrical_or.tscn`
- `engine/LogicGate.cs`
- `engine/BinaryCircuit.cs`
- `CuriousContraptions.tests/LogicGateTests.cs`
- `CuriousContraptions.tests/ElectricalLogicTests.cs`
- `CuriousContraptions.tests/BinaryCircuitTests.cs`
- `CuriousContraptions.tests/ElectricalAnimationTests.cs`

## 5. Acceptance outline

Authority: [CAT-026](../requirements.md#current-cat-026), [todo-362](../requirements.md#todo-362); Story 9.8 adds `tools/e2e/cat-026-027.test.ts`.

- **Construction (Chrome UI).** As CAT-013: battery to the bottom supply socket via the icon choice, conditions to the one-mark/two-mark sockets, gate→consumer.
- **Positive.** Rows 01, 10, 11 with supply power the consumer.
- **Negative / controls.** Row 00 with supply; every row without supply (O5); source-free Or loop forgets its condition (O7).
- **Boundaries.** Or loop restarts from unpowered every tick (O6); reordered placement and wiring; reconvergence into a downstream Xor (O9).
- **Run/Reset; Save/Load.** As CAT-013.
- **Integrations.** Or feeding Xor/Nand; actual committed lamps.

## 6. Open questions

1. Condition sources and output consumer: as [CAT-024 open question 1](CAT-024-electrical_nand.md#6-open-questions) and [CAT-013 open question 1](CAT-013-both_gate.md#6-open-questions). No Or-specific question.
