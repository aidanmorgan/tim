# CAT-025 · electrical_nor — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-025 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch B declaration spec (CAT-025-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-025](../requirements.md#current-cat-025). All five supplied gates share one declaration: shared gate facts G1–G15 are in [CAT-013](CAT-013-both_gate.md#4-legacy-harvest) and network facts N1–N24 in [CAT-005](CAT-005-battery.md#4-legacy-harvest). This spec adds only what is specific to Nor.

**Colliders (owner, 25 October 2026, "visible = solid"):** since [`elements-visible-solid-sweep`](../../../TODO.md) the face panel, the three lamps and the four port sockets collide as drawn, as for every supplied gate ([CAT-013](CAT-013-both_gate.md)) (`elements/ElectricalNor/electrical_nor.tscn`; `VisibleSolidColliderTests` checks them against the art). The sockets count toward the bench edge as for every gate. The static markings are flat printing at z 0.37025, 0.25 mm above the cream face, without colliders. Original symbol layout and palette remain; native surface/shape proof does not claim post-catalogue browser legibility.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-025 · `electrical_nor` (operation Nor) |
| Requirement anchor | [current-cat-025](../requirements.md#current-cat-025); retained behaviour [todo-362](../requirements.md#todo-362) |
| Mapped identities | [EL-136 Electrical NOR gate](../invest/named-elements.md#element-136). No TH/RAD/GAP identity. |
| Roadmap story | 9.7 "Electrical Logic Gates NAND & NOR" (Epic 9), shared with CAT-024 Current [CAT-025 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | Story 9.7b candidate: package `elements/ElectricalNor` and shared `Nor` law on the Story 9.7a ordered supply resolve and typed `NonmonotoneFeedback` refusal, implemented with native proof ([spec](../../../_bmad-output/implementation-artifacts/spec-9-7b-electrical-nor.md)); **delivered**, scoped native and publication Pass `55ee05`; browser qualification pending post-catalogue ("candidate" is the slice's wording at the time). Every residual slice, the EL-136 closure review included, is blocked on the engine programme (owner decision 3a, 22 November 2026); the engine gaps are those of [CAT-013](CAT-013-both_gate.md#3-engine-capabilities-and-conformance): `ENGINE-SIGNAL-NODES` (its self-edge rule row) and `ENGINE-CAPABILITY-INSTANCES` (audit E2 and E3). |

## 2. Declaration

The delivered declaration is `elements/ElectricalNor/ElectricalNorElementDeclaration.cs` with the scene `elements/ElectricalNor/electrical_nor.tscn` (read on `8d1d68aa`). It is the [CAT-013 section 2](CAT-013-both_gate.md#2-declaration) declaration with the values below changed. The body, material, ten colliders, four ports and nine lamp bindings are identical (file comparison), and so are the three layers: colliders from hidden proxies, art never used as a collider, bindings that change lamp colour only.

| Item | Value |
|---|---|
| Id and title | `electrical_nor`, "Neither gate" (`:12–14`) |
| Category and palette order | Control, 28 (`:13`, `:17`) |
| Operation | `LogicGateOperation.Nor` (`:35`), nonmonotone |
| Description | "Passes the separate bottom supply when neither numbered input is powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`:15`) |
| Printed art (rendering graph) | `Visual/Pictogram/*`: navy strokes (two input stubs, the curved body, an output stub) with a hollow inversion ring (`printed-ring.obj`). `Visual/TruthDot00`: one gold disc (`printed-disc.obj`) at (−0.21, −0.4) (`electrical_nor.tscn:179`). All are decorative, printed flat at z 0.37025, and moved by nothing |
| Icon and coverage key | `elements/ElectricalNor/icon.svg`; `electrical_nor` |
| Compiled counts per instance | 1 static body; 10 colliders (3 boxes, 7 spheres); 0 joints; 0 signal nodes; 1 supply node; 0 stores; 4 ports; 9 bindings |

The curved-back pictogram with its ring and the single dot tell it from its siblings without colour.

## 3. Engine capabilities and conformance

The generic laws, the capability probe (6 present, 0 untested, 1 narrower, 0 missing), the conformance verdict (conforms with one fix, the unread `ElectricalNodes` ratings) and the performance fitness, with the reason for ten colliders, are those of [CAT-013 section 3](CAT-013-both_gate.md#3-engine-capabilities-and-conformance). The one difference is data: `Nor` is nonmonotone, so the supply network's cycle rule refuses any loop through this node before a solve. No engine code names this package.

**Dependencies.** Battery; a condition switch (each condition is a Battery through an Impact switch contact); a supplied consumer (Powered gate). All are delivered.

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

Authority: [CAT-025](../requirements.md#current-cat-025), [todo-362](../requirements.md#todo-362); Story 9.7b adds `tools/e2e/cat-025.test.ts` (Story 9.7 was split; its shared `cat-024-025.test.ts` is not used).

- **Construction (Chrome UI).** As CAT-013: battery to the bottom supply socket via the icon choice, conditions to the one-mark/two-mark sockets, gate→consumer.
- **Positive.** Row 00 with supply powers the consumer.
- **Negative / controls.** Rows 01, 10, 11 with supply; row 00 without supply (R5).
- **Boundaries.** Zero-delay Nor loop, including through an open switch, stays in build mode (R6, R7); reordered placement and wiring; a reconvergent second input retracts the output in the same tick.
- **Run/Reset; Save/Load.** As CAT-013.
- **Integrations.** Its own icon, pictogram and single truth dot legible at projected size. Native Or chaining (the R6 Nor → Or → Nor refusal and reconvergence through an Either gate) is proved with the real Either gate since Story 9.8a; the browser case stays queued post-catalogue.

## 6. Open questions

1. Condition sources and output consumer: as [CAT-024 open question 1](CAT-024-electrical_nand.md#6-open-questions) and [CAT-013 §6 decision 1](CAT-013-both_gate.md#6-current-delivery-decisions-22-october-2026). Both are resolved: a Battery through an Impact switch contact for each condition, and the Powered gate as consumer. No Nor-specific question.

## 7. Delivered state, integration and residuals (22 November 2026)

Checked against `elements/ElectricalNor`, its scene and its tests on published main `8d1d68aa`. Nothing was built or run. The shared gate declaration, laws, capability probe and conformance are in CAT-013 [section 2](CAT-013-both_gate.md#2-declaration) and [section 3](CAT-013-both_gate.md#3-engine-capabilities-and-conformance); this part's values are in section 2 above.

- **Disposition.** Delivered CAT part: package `elements/ElectricalNor`, id `electrical_nor`, display name "Neither gate", category Control, palette order 28 (`elements/ElectricalNor/ElectricalNorElementDeclaration.cs:12–17`). Story 9.7b Pass `55ee05` (`sprint-status.yaml:291`). No slice is planned. [EL-136](EL-136-electrical-nor-gate.md) is a candidate Duplicate closure of this part.
- **Tests** (`ElectricalNorTests`). Truth and supply: `SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer`, `TrueRowWithoutSupplyWireNeverCreatesPower`. Retraction, save, load and Reset: `ClosingOneSwitchRetractsTheOutputAndSaveLoadAndResetAreExact`. Order and reconvergence: `PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse`, `ReconvergentConditionKeepsTheOutputOffFromTheFirstTick`. Feedback refusal: `ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve`. Lamps and printing: `ElectricalNorPresentationTests.PictogramSingleTruthDotAndThreeLampsFollowCommittedRowsThroughReset`.
- **Residuals.** A wire from this gate's own `Supply` to its own input is refused as `InvalidConnection`, not with the loop message EL-136 requires; owner `ENGINE-SIGNAL-NODES` (its self-edge rule row); the EL-136 closure review is blocked on that row, after the engine programme (owner decision 3a). The Workbench connect tool refuses a same-part target (`ui/WorkshopConnections.cs:106`), so only a save or a command can carry this wire; it is not a player-reachable defect. The unread `ElectricalNodes` ratings (`:33`) are a required forward-only refactor, as for [CAT-013](CAT-013-both_gate.md#7-delivered-state-integration-and-residuals-22-november-2026). Requirement row CAT-025 stays unchecked until the browser phase.
- **Later row owned here (coordinator decisions 36 and 41).** [EL-241](EL-241-ball-hopper.md) row H6 (`BallHopperTests.AClockFeedsTwoDestinationsAlternately`): Clock 1 (period T) pulses the hopper; Clock 2 (period 2T) supplies Powered gate A directly and gate B through an Electrical NOR, so successive balls meet opposite gates and alternate between two Receivers. It is a cross-part row owned by this slice, because the Repeating clock is taught at 28 and the NOR at 32, after the hopper at 26a; EL-241 keeps its control and variation.
- **Integration.** Condition sources: a Battery through an Impact switch contact. Consumer: Powered gate. No campaign level places it. Coverage binding: catalogue key `electrical_nor`.
