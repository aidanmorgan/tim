# CAT-027 · electrical_xor — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-027 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch B declaration spec (CAT-027-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-027](../requirements.md#current-cat-027). All five supplied gates share one declaration: shared gate facts G1–G15 are in [CAT-013](CAT-013-both_gate.md#4-legacy-harvest) and network facts N1–N24 in [CAT-005](CAT-005-battery.md#4-legacy-harvest). This spec adds only what is specific to Xor.

**Colliders (owner, 25 October 2026, "visible = solid"):** since [`elements-visible-solid-sweep`](../../../TODO.md) the face panel, the three lamps and the four port sockets collide as drawn, as for every supplied gate ([CAT-013](CAT-013-both_gate.md)) (`elements/ElectricalXor/electrical_xor.tscn`; `VisibleSolidColliderTests` checks them against the art). The sockets count toward the bench edge as for every gate. The static markings are flat printing at z 0.37025, 0.25 mm above the cream face, without colliders. Original symbol layout and palette remain; native surface/shape proof does not claim post-catalogue browser legibility.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-027 · `electrical_xor` (operation Xor) |
| Requirement anchor | [current-cat-027](../requirements.md#current-cat-027); retained behaviour [todo-362](../requirements.md#todo-362) |
| Mapped identities | [EL-135 Electrical XOR gate](../invest/named-elements.md#element-135). No TH/RAD/GAP identity. |
| Roadmap story | 9.8 "Electrical Logic Gates OR & XOR" (Epic 9), shared with CAT-026 Current [CAT-027 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | Story 9.8b candidate: package `elements/ElectricalXor` and shared nonmonotone `Xor` law on the Story 9.7a ordered supply resolve; every loop containing the gate is refused as `NonmonotoneFeedback`. Implemented with native proof ([spec](../../../_bmad-output/implementation-artifacts/spec-9-8b-electrical-xor.md)); **delivered**, scoped native and publication Pass `8bfddc`; browser qualification pending post-catalogue ("candidate" is the slice's wording at the time). Every residual slice, the EL-135 closure review included, is blocked on the engine programme (owner decision 3a, 22 November 2026); the engine gaps are those of [CAT-013](CAT-013-both_gate.md#3-engine-capabilities-and-conformance): `ENGINE-SIGNAL-NODES` (its self-edge rule row) and `ENGINE-CAPABILITY-INSTANCES` (audit E2 and E3). |

## 2. Declaration

The delivered declaration is `elements/ElectricalXor/ElectricalXorElementDeclaration.cs` with the scene `elements/ElectricalXor/electrical_xor.tscn` (read on `8d1d68aa`). It is the [CAT-013 section 2](CAT-013-both_gate.md#2-declaration) declaration with the values below changed. The body, material, ten colliders, four ports and nine lamp bindings are identical (file comparison), and so are the three layers: colliders from hidden proxies, art never used as a collider, bindings that change lamp colour only.

| Item | Value |
|---|---|
| Id and title | `electrical_xor`, "Exactly one gate" (`:12–14`) |
| Category and palette order | Control, 30 (`:13`, `:17`) |
| Operation | `LogicGateOperation.Xor` (`:35`), nonmonotone |
| Description | "Passes the separate bottom supply when exactly one numbered input is powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`:15`) |
| Printed art (rendering graph) | `Visual/Pictogram/*`: navy strokes (a doubled back curve, the curved body, an output stub), no ring. `Visual/TruthDot01`, `TruthDot10`: gold discs (`printed-disc.obj`) at x −0.07 and 0.07, y −0.4 (`electrical_xor.tscn:176–181`). All are decorative, printed flat at z 0.37025, and moved by nothing |
| Icon and coverage key | `elements/ElectricalXor/icon.svg`; `electrical_xor` |
| Compiled counts per instance | 1 static body; 10 colliders (3 boxes, 7 spheres); 0 joints; 0 signal nodes; 1 supply node; 0 stores; 4 ports; 9 bindings |

The doubled back curve and the centred two-dot row tell it from its siblings without colour.

## 3. Engine capabilities and conformance

The generic laws, the capability probe (6 present, 0 untested, 1 narrower, 0 missing), the conformance verdict (conforms with one fix, the unread `ElectricalNodes` ratings) and the performance fitness, with the reason for ten colliders, are those of [CAT-013 section 3](CAT-013-both_gate.md#3-engine-capabilities-and-conformance). The one difference is data: `Xor` is nonmonotone, so the supply network's cycle rule refuses any loop through this node before a solve. No engine code names this package.

**Dependencies.** Battery; a condition switch (each condition is a Battery through an Impact switch contact, which also gives the open-switch feedback control); a supplied consumer (Powered gate). All are delivered.

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

Authority: [CAT-027](../requirements.md#current-cat-027), [todo-362](../requirements.md#todo-362); Story 9.8b adds `tools/e2e/cat-027.test.ts` (Story 9.8 was split; its shared `cat-026-027.test.ts` is not used).

- **Construction (Chrome UI).** As CAT-013: battery to the bottom supply socket via the icon choice, conditions to the one-mark/two-mark sockets, gate→consumer.
- **Positive.** Rows 01 and 10 with supply power the consumer.
- **Negative / controls.** Rows 00 and 11 with supply (the Story 9.8 AC: "low when identical"); every row without supply (X5).
- **Boundaries.** Reconvergent and chained controls (X6, X7); zero-delay Xor loops, including one through an open switch, stay in build mode with the short bottom-status message (X8, X9).
- **Run/Reset; Save/Load.** As CAT-013; a rejected Run leaves the saved machine unchanged (X8).
- **Integrations.** Or feeding Xor (X6); its own committed indicator.

## 6. Open questions

1. Condition sources and output consumer: as [CAT-024 open question 1](CAT-024-electrical_nand.md#6-open-questions) and [CAT-013 §6 decision 1](CAT-013-both_gate.md#6-current-delivery-decisions-22-october-2026). Both are resolved: a Battery through an Impact switch contact for each condition, and the Powered gate as consumer. No Xor-specific question.

## 7. Delivered state, integration and residuals (22 November 2026)

Checked against `elements/ElectricalXor`, its scene and its tests on published main `8d1d68aa`. Nothing was built or run. The shared gate declaration, laws, capability probe and conformance are in CAT-013 [section 2](CAT-013-both_gate.md#2-declaration) and [section 3](CAT-013-both_gate.md#3-engine-capabilities-and-conformance); this part's values are in section 2 above.

- **Disposition.** Delivered CAT part: package `elements/ElectricalXor`, id `electrical_xor`, display name "Exactly one gate", category Control, palette order 30 (`elements/ElectricalXor/ElectricalXorElementDeclaration.cs:12–17`). Story 9.8b Pass `8bfddc` (`sprint-status.yaml:309`). No slice is planned. [EL-135](EL-135-electrical-xor-gate.md) is a candidate Duplicate closure of this part.
- **Tests** (`ElectricalXorTests`). Truth and supply: `SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer`, `RemovingTheSupplyOrConditionLinksUnpowersATrueRow`. Retraction, save, load and Reset: `ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact`. Order and reconvergence: `PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse`, `ReconvergenceThroughAnEitherGateKeepsTheOutputOffFromTheFirstTick`, `OrLoopFeedingTheGateIsAdmittedAndPowersIt`. Feedback refusal: `ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve`. Lamps and printing: `ElectricalXorPresentationTests.PictogramTwoTruthDotsAndThreeLampsFollowCommittedRowsThroughReset`.
- **Residuals.** A wire from this gate's own `Supply` to its own input is refused as `InvalidConnection`, not with the loop message EL-135 requires; owner `ENGINE-SIGNAL-NODES` (its self-edge rule row); the EL-135 closure review is blocked on that row, after the engine programme (owner decision 3a). The Workbench connect tool refuses a same-part target (`ui/WorkshopConnections.cs:106`), so only a save or a command can carry this wire; it is not a player-reachable defect. The unread `ElectricalNodes` ratings (`:33`) are a required forward-only refactor, as for [CAT-013](CAT-013-both_gate.md#7-delivered-state-integration-and-residuals-22-november-2026). Requirement row CAT-027 stays unchecked until the browser phase.
- **Integration.** Condition sources: a Battery through an Impact switch contact. Consumer: Powered gate. No campaign level places it. Coverage binding: catalogue key `electrical_xor`.
