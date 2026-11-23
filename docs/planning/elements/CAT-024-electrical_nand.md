# CAT-024 · electrical_nand — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-024 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch B declaration spec (CAT-024-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-024](../requirements.md#current-cat-024). All five supplied gates share one declaration: shared gate facts G1–G15 are in [CAT-013](CAT-013-both_gate.md#4-legacy-harvest) and network facts N1–N24 in [CAT-005](CAT-005-battery.md#4-legacy-harvest). This spec adds only what is specific to Nand.

**Colliders (owner, 25 October 2026, "visible = solid"):** since [`elements-visible-solid-sweep`](../../../TODO.md) the face panel, the three lamps and the four port sockets collide as drawn, as for every supplied gate ([CAT-013](CAT-013-both_gate.md)) (`elements/ElectricalNand/electrical_nand.tscn`; `VisibleSolidColliderTests` checks them against the art and drops a ball onto this gate's output lamp). The sockets count toward the bench edge as for every gate. The static markings are flat printing at z 0.37025, 0.25 mm above the cream face, without colliders. Original symbol layout and palette remain; native surface/shape proof does not claim post-catalogue browser legibility.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-024 · `electrical_nand` (operation Nand) |
| Requirement anchor | [current-cat-024](../requirements.md#current-cat-024); retained behaviour [todo-362](../requirements.md#todo-362) |
| Mapped identities | [EL-137 Electrical NAND gate](../invest/named-elements.md#element-137). No TH/RAD/GAP identity. |
| Roadmap story | 9.7 "Electrical Logic Gates NAND & NOR" (Epic 9), shared with CAT-025 Current [CAT-024 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | Story 9.7a candidate: package `elements/ElectricalNand`, shared `Nand` law, ordered supply resolve and typed `NonmonotoneFeedback` refusal implemented with native proof ([spec](../../../_bmad-output/implementation-artifacts/spec-9-7a-electrical-nand.md)); **delivered**, scoped native and publication Pass `b35be7`; browser qualification pending post-catalogue ("candidate" is the slice's wording at the time). Every residual slice, the EL-137 closure review included, is blocked on the engine programme (owner decision 3a, 22 November 2026); the engine gaps are those of [CAT-013](CAT-013-both_gate.md#3-engine-capabilities-and-conformance): `ENGINE-SIGNAL-NODES` (its self-edge rule row) and `ENGINE-CAPABILITY-INSTANCES` (audit E2 and E3). |

## 2. Declaration

The delivered declaration is `elements/ElectricalNand/ElectricalNandElementDeclaration.cs` with the scene `elements/ElectricalNand/electrical_nand.tscn` (read on `8d1d68aa`). It is the [CAT-013 section 2](CAT-013-both_gate.md#2-declaration) declaration with the values below changed. The body, material, ten colliders, four ports and nine lamp bindings are identical (file comparison), and so are the three layers: colliders from hidden proxies, art never used as a collider, bindings that change lamp colour only.

| Item | Value |
|---|---|
| Id and title | `electrical_nand`, "Not both gate" (`:12–14`) |
| Category and palette order | Control, 27 (`:13`, `:17`) |
| Operation | `LogicGateOperation.Nand` (`:35`), nonmonotone |
| Description | "Passes the separate bottom supply when the numbered inputs are not both powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`:15`) |
| Printed art (rendering graph) | `Visual/Pictogram/*`: navy strokes with a hollow inversion ring (`printed-ring.obj`). `Visual/TruthDot00`, `TruthDot01`, `TruthDot10`: gold discs (`printed-disc.obj`) at x −0.21, −0.07 and 0.07, y −0.4 (`electrical_nand.tscn:151–162`). All are decorative, printed flat at z 0.37025, and moved by nothing |
| Icon and coverage key | `elements/ElectricalNand/icon.svg`; `electrical_nand` |
| Compiled counts per instance | 1 static body; 10 colliders (3 boxes, 7 spheres); 0 joints; 0 signal nodes; 1 supply node; 0 stores; 4 ports; 9 bindings |

The pictogram with its ring and the three-dot truth row tell it from its siblings without colour.

## 3. Engine capabilities and conformance

The generic laws, the capability probe (6 present, 0 untested, 1 narrower, 0 missing), the conformance verdict (conforms with one fix, the unread `ElectricalNodes` ratings) and the performance fitness, with the reason for ten colliders, are those of [CAT-013 section 3](CAT-013-both_gate.md#3-engine-capabilities-and-conformance). The one difference is data: `Nand` is nonmonotone, so the supply network's cycle rule refuses any loop through this node before a solve. No engine code names this package.

**Dependencies.** Battery; a condition switch (each condition is a Battery through an Impact switch contact); a supplied consumer (Powered gate). All are delivered.

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
- **Integrations.** Chained with an Or gate (D6); its own pictogram and three truth dots visible at projected size. Native Or chaining (D6 reconvergence and the D7 Nand → Or → Nand refusal) is proved with the real Either gate since Story 9.8a; the browser case stays queued post-catalogue.

## 6. Open questions

1. **Input "power switches".** Resolved (Story 9.7a D5, after owner scheduling item b): each condition is a Battery → Impact Switch (CAT-063) `PowerIn`→`Supply` contact from Story 9.5b; battery-enabled conditions are supplemental proof only.
2. Consumer for the output: resolved; the Powered gate ([CAT-013 §6 decision 1](CAT-013-both_gate.md#6-current-delivery-decisions-22-october-2026)).

## 7. Delivered state, integration and residuals (22 November 2026)

Checked against `elements/ElectricalNand`, its scene and its tests on published main `8d1d68aa`. Nothing was built or run. The shared gate declaration, laws, capability probe and conformance are in CAT-013 [section 2](CAT-013-both_gate.md#2-declaration) and [section 3](CAT-013-both_gate.md#3-engine-capabilities-and-conformance); this part's values are in section 2 above.

- **Disposition.** Delivered CAT part: package `elements/ElectricalNand`, id `electrical_nand`, display name "Not both gate", category Control, palette order 27 (`elements/ElectricalNand/ElectricalNandElementDeclaration.cs:12–17`). Story 9.7a Pass `b35be7` (`sprint-status.yaml:279`). No slice is planned. [EL-137](EL-137-electrical-nand-gate.md) is a candidate Duplicate closure of this part.
- **Tests** (`ElectricalNandTests`). Truth and supply: `SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer`, `TrueRowWithoutSupplyWireNeverCreatesPower`. Retraction, save, load and Reset: `ClosingBothSwitchesRetractsTheOutputAndSaveLoadAndResetAreExact`. Order and reconvergence: `PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse`, `ReconvergentConditionRetractsOutputOnTheSameTick`. Feedback refusal: `ZeroDelayFeedbackThroughTheGateIsRefusedAtomicallyBeforeAnySolve`, `LongLoopRefusalNamesTheFirstPartsAndCountsTheRest`. Lamps and printing: `ElectricalNandPresentationTests.PictogramTruthDotsAndThreeLampsFollowCommittedRowsThroughReset`.
- **Residuals.** A wire from this gate's own `Supply` to its own input is refused as `InvalidConnection`, not with the loop message EL-137 requires; owner `ENGINE-SIGNAL-NODES` (its self-edge rule row); the EL-137 closure review is blocked on that row, after the engine programme (owner decision 3a). The Workbench connect tool refuses a same-part target (`ui/WorkshopConnections.cs:106`), so only a save or a command can carry this wire; it is not a player-reachable defect. The unread `ElectricalNodes` ratings (`:33`) are a required forward-only refactor, as for [CAT-013](CAT-013-both_gate.md#7-delivered-state-integration-and-residuals-22-november-2026). Requirement row CAT-024 stays unchecked until the browser phase.
- **Integration.** Condition sources: a Battery through an Impact switch contact. Consumer: Powered gate. No campaign level places it. Coverage binding: catalogue key `electrical_nand`.
