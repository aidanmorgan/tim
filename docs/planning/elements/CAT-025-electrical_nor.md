# CAT-025 · electrical_nor — declaration readiness spec

Story 7.0 Batch B declaration spec (CAT-025-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-025](../requirements.md#current-cat-025). All five supplied gates share one declaration: shared gate facts G1–G15 are in [CAT-013](CAT-013-both_gate.md#4-legacy-harvest) and network facts N1–N24 in [CAT-005](CAT-005-battery.md#4-legacy-harvest). This spec adds only what is specific to Nor.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-025 · `electrical_nor` (operation Nor) |
| Requirement anchor | [current-cat-025](../requirements.md#current-cat-025); retained behaviour [todo-362](../requirements.md#todo-362) |
| Mapped identities | [EL-136 Electrical NOR gate](../invest/named-elements.md#element-136). No TH/RAD/GAP identity. |
| Roadmap story | 9.7 "Electrical Logic Gates NAND & NOR" (Epic 9), shared with CAT-024 |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Same as CAT-013: static box 1.7 × 1.3 × 0.65 m and foot 1.85 × 0.16 × 0.85 m at y −0.75 ([CAT-013 §2](CAT-013-both_gate.md#2-declaration)). |
| Mass and material | Static; legacy static default material restitution 1, bounce threshold 0.1 m/s, friction 0.3 ([CAT-013 §2](CAT-013-both_gate.md#2-declaration)). |
| Constraints and joints | none |
| Typed sockets and ports | Same four electrical ports as CAT-013: `FirstIn`, `SecondIn`, separate `PowerIn` supply, `Supply` output. |
| Sensors and activation | none |
| Work and energy stores | none. Nor's only true row is 00 (no inputs); that state still requires independent `PowerIn` supply and never creates power ([todo-362](../requirements.md#todo-362)). |
| Parameters | `Operation = Nor` (enum value 3), fixed by catalogue kind (`parts/scenes/electrical_nor.tscn@a6c914e:L1-L5`). |
| Cosmetic curves and UI bindings | Same three lamps as CAT-013 (inputs follow committed availability; output follows truth ∧ supplied). |
| Art | Shared body, foot, cream face, input marks and port spheres (CAT-013). Nor shows its own toolbox pictogram on the face and one gold truth-row dot for row 00 at x −0.21 (y −0.4, z 0.42) (G13). Toolbox icon (current, survives): `ui/WorkshopIcons.cs@a6c914e:L78-L78`. Colour `#66b8c9`. Design: [DESIGN.md · Supplied electrical logic](../../../DESIGN.md#supplied-electrical-logic). |
| Catalogue / inventory | Id `electrical_nor`, Title "Neither gate", Category Control, colour (0.4, 0.72, 0.79), no parameters; Description: "Passes the separate bottom supply when neither numbered input is powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`parts/catalog/electrical_nor.tres@a6c914e:L1-L11`). No level places or grants it. |

## 3. Engine capabilities

Families ([map row](../general-engine-element-map.md)): AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction (P0-022/023 shared evaluator/feedback registration).

- **Exists now:** as [CAT-013 §3](CAT-013-both_gate.md#3-engine-capabilities).
- **Missing:** supply network (Story 8.1); gate element, `FirstIn`/`SecondIn` sockets, electrical-input cosmetic source and nonmonotone-feedback rejection (Story 9.6). Story 9.7 adds only the Nor operation value, pictogram and truth dot.
- **Dependencies:** CAT-005 Battery; CAT-013 declaration (Story 9.6) first; a supplied consumer (CAT-013 open question 1); condition switches as [CAT-024 open question 1](CAT-024-electrical_nand.md#6-open-questions).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| R1 | Nor truth rows (a, b) = 00 T, 01 F, 10 F, 11 F. | `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L13-L13` | carry forward |
| R2 | Evaluation Nor = ¬a ∧ ¬b. | `engine/LogicGate.cs@a6c914e:L16-L16` | carry forward |
| R3 | Scene sets `Operation = 3` (Nor). | `parts/scenes/electrical_nor.tscn@a6c914e:L1-L5` | carry forward the operation; do not carry forward the integer scene export (closed enum) |
| R4 | Catalogue entry, title and description. | `parts/catalog/electrical_nor.tres@a6c914e:L1-L11` | carry forward |
| R5 | The supplied load is a Powered gate (`powered_gate`, CAT-051) in the legacy tests (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L15-L15`). With supply absent the true 00 row leaves the load unpowered; with supply present the load follows R1; reversed link order gives the same result; removing the battery links unpowers the load. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L26-L62` | carry forward |
| R6 | A zero-delay loop Nor → Or → Nor rejects with the feedback error and no part receives power. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L91-L109` | carry forward |
| R7 | Feedback through a potential (open) contact into a Nor is rejected at construction, before any solve. | `CuriousContraptions.tests/BinaryCircuitTests.cs@a6c914e:L41-L50` | carry forward (covers "including open switched routes") |
| R8 | Nor is one of the three operations forbidden in a cycle. | `engine/BinaryCircuit.cs@a6c914e:L104-L112` | carry forward |
| R9 | Truth-row dots derive from the evaluated table, so Nor shows one dot. | `parts/ElectricalLogicPart.cs@a6c914e:L67-L70` | carry forward |
| R10 | Committed lamp behaviour for every Nor row with and without supply. | `CuriousContraptions.tests/ElectricalAnimationTests.cs@a6c914e:L38-L100` | carry forward behaviour (CAT-013 G14) |

The legacy has no Nor-specific reconvergence test (the reconvergence cases cover Xor and Nand only); the requirement's "reordering/reconvergence" check for Nor is new acceptance, built on the same rule (CAT-024 D6).

`reference/P0-022-before/docs/coverage/engine/*.json` list `electrical_nor` only as a catalogue id: no element knowledge.

### Files harvested

- `parts/ElectricalLogicPart.cs`
- `parts/catalog/electrical_nor.tres`
- `parts/scenes/electrical_nor.tscn`
- `engine/LogicGate.cs`
- `engine/BinaryCircuit.cs`
- `CuriousContraptions.tests/LogicGateTests.cs`
- `CuriousContraptions.tests/ElectricalLogicTests.cs`
- `CuriousContraptions.tests/BinaryCircuitTests.cs`
- `CuriousContraptions.tests/ElectricalAnimationTests.cs`

## 5. Acceptance outline

Authority: [CAT-025](../requirements.md#current-cat-025), [todo-362](../requirements.md#todo-362); Story 9.7 adds `tools/e2e/cat-024-025.test.ts`.

- **Construction (Chrome UI).** As CAT-013: battery to the bottom supply socket via the icon choice, conditions to the one-mark/two-mark sockets, gate→consumer.
- **Positive.** Row 00 with supply powers the consumer.
- **Negative / controls.** Rows 01, 10, 11 with supply; row 00 without supply (R5).
- **Boundaries.** Zero-delay Nor loop, including through an open switch, stays in build mode (R6, R7); reordered placement and wiring; a reconvergent second input retracts the output in the same tick.
- **Run/Reset; Save/Load.** As CAT-013.
- **Integrations.** Its own icon, pictogram and single truth dot legible at projected size.

## 6. Open questions

1. Condition sources and output consumer: as [CAT-024 open question 1](CAT-024-electrical_nand.md#6-open-questions) and [CAT-013 open question 1](CAT-013-both_gate.md#6-open-questions). No Nor-specific question.
