# CAT-026 · electrical_or — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-026 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch B declaration spec (CAT-026-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-026](../requirements.md#current-cat-026). All five supplied gates share one declaration: shared gate facts G1–G15 are in [CAT-013](CAT-013-both_gate.md#4-legacy-harvest) and network facts N1–N24 in [CAT-005](CAT-005-battery.md#4-legacy-harvest). This spec adds only what is specific to Or.

**Colliders (owner, 25 October 2026, "visible = solid"):** since [`elements-visible-solid-sweep`](../../../TODO.md) the face panel, the three lamps and the four port sockets collide as drawn, as for every supplied gate ([CAT-013](CAT-013-both_gate.md)) (`elements/ElectricalOr/electrical_or.tscn`; `VisibleSolidColliderTests` checks them against the art). The sockets count toward the bench edge as for every gate. The static markings are flat printing at z 0.37025, 0.25 mm above the cream face, without colliders. Original symbol layout and palette remain; native surface/shape proof does not claim post-catalogue browser legibility.

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-026 · `electrical_or` (operation Or) |
| Requirement anchor | [current-cat-026](../requirements.md#current-cat-026); retained behaviour [todo-362](../requirements.md#todo-362) |
| Mapped identities | [EL-134 Electrical OR gate](../invest/named-elements.md#element-134). No TH/RAD/GAP identity. |
| Roadmap story | 9.8 "Electrical Logic Gates OR & XOR" (Epic 9), shared with CAT-027 Current [CAT-026 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). |
| Status | Story 9.8a candidate: package `elements/ElectricalOr` and shared monotone `Or` law on the Story 9.7a ordered supply resolve (Or-only cycles admitted at their least fixed point; mixed loops refused), implemented with native proof ([spec](../../../_bmad-output/implementation-artifacts/spec-9-8a-electrical-or.md)); **delivered**, scoped native and publication Pass `a2eddf`; browser qualification pending post-catalogue ("candidate" is the slice's wording at the time). Every residual slice, the EL-134 closure review included, is blocked on the engine programme (owner decision 3a, 22 November 2026); the engine gaps are those of [CAT-013](CAT-013-both_gate.md#3-engine-capabilities-and-conformance): `ENGINE-SIGNAL-NODES` (its self-edge rule row) and `ENGINE-CAPABILITY-INSTANCES` (audit E2 and E3). |

## 2. Declaration

The delivered declaration is `elements/ElectricalOr/ElectricalOrElementDeclaration.cs` with the scene `elements/ElectricalOr/electrical_or.tscn` (read on `8d1d68aa`). It is the [CAT-013 section 2](CAT-013-both_gate.md#2-declaration) declaration with the values below changed. The body, material, ten colliders, four ports and nine lamp bindings are identical (file comparison), and so are the three layers: colliders from hidden proxies, art never used as a collider, bindings that change lamp colour only.

| Item | Value |
|---|---|
| Id and title | `electrical_or`, "Either gate" (`:12–14`) |
| Category and palette order | Control, 29 (`:13`, `:17`) |
| Operation | `LogicGateOperation.Or` (`:35`), monotone |
| Description | "Passes the separate bottom supply when either numbered input is powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`:15`) |
| Printed art (rendering graph) | `Visual/Pictogram/*`: navy strokes (two input stubs, the curved body, an output stub), no ring. `Visual/TruthDot01`, `TruthDot10`, `TruthDot11`: gold discs (`printed-disc.obj`) at x −0.07, 0.07 and 0.21, y −0.4 (`electrical_or.tscn:172–182`). All are decorative, printed flat at z 0.37025, and moved by nothing |
| Icon and coverage key | `elements/ElectricalOr/icon.svg`; `electrical_or` |
| Compiled counts per instance | 1 static body; 10 colliders (3 boxes, 7 spheres); 0 joints; 0 signal nodes; 1 supply node; 0 stores; 4 ports; 9 bindings |

The curved-back pictogram without a ring and the right-hand three-dot row tell it from its siblings without colour.

## 3. Engine capabilities and conformance

The generic laws, the capability probe (6 present, 0 untested, 1 narrower, 0 missing), the conformance verdict (conforms with one fix, the unread `ElectricalNodes` ratings) and the performance fitness, with the reason for ten colliders, are those of [CAT-013 section 3](CAT-013-both_gate.md#3-engine-capabilities-and-conformance). The one difference is data: `Or` is monotone, so the supply network's cycle rule admits a loop of Or and And nodes and settles it from unpowered at every solve; a loop that also holds a nonmonotone node is refused. No engine code names this package.

**Dependencies.** Battery; a condition switch (each condition is a Battery through an Impact switch contact); a supplied consumer (Powered gate, Motor). All are delivered.

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

Authority: [CAT-026](../requirements.md#current-cat-026), [todo-362](../requirements.md#todo-362); Story 9.8a adds `tools/e2e/cat-026.test.ts` (Story 9.8 was split; its shared `cat-026-027.test.ts` is not used).

- **Construction (Chrome UI).** As CAT-013: battery to the bottom supply socket via the icon choice, conditions to the one-mark/two-mark sockets, gate→consumer.
- **Positive.** Rows 01, 10, 11 with supply power the consumer.
- **Negative / controls.** Row 00 with supply; every row without supply (O5); source-free Or loop forgets its condition (O7).
- **Boundaries.** Or loop restarts from unpowered every tick (O6); reordered placement and wiring; reconvergence into a downstream Xor (O9).
- **Run/Reset; Save/Load.** As CAT-013.
- **Integrations.** Or feeding Xor/Nand; actual committed lamps. The Xor forms (O9 reconvergence into a Xor and the O10 Xor → Or → Xor refusal) are proved natively with the real Exactly one gate since Story 9.8b; the browser case stays queued post-catalogue.

## 6. Open questions

1. Condition sources and output consumer: as [CAT-024 open question 1](CAT-024-electrical_nand.md#6-open-questions) and [CAT-013 §6 decision 1](CAT-013-both_gate.md#6-current-delivery-decisions-22-october-2026). Both are resolved: a Battery through an Impact switch contact for each condition, and the Powered gate as consumer. No Or-specific question.

## 7. Delivered state, integration and residuals (22 November 2026)

Checked against `elements/ElectricalOr`, its scene and its tests on published main `8d1d68aa`. Nothing was built or run. The shared gate declaration, laws, capability probe and conformance are in CAT-013 [section 2](CAT-013-both_gate.md#2-declaration) and [section 3](CAT-013-both_gate.md#3-engine-capabilities-and-conformance); this part's values are in section 2 above.

- **Disposition.** Delivered CAT part: package `elements/ElectricalOr`, id `electrical_or`, display name "Either gate", category Control, palette order 29 (`elements/ElectricalOr/ElectricalOrElementDeclaration.cs:12–17`). Story 9.8a Pass `a2eddf` (`sprint-status.yaml:301`). No slice is planned. [EL-134](EL-134-electrical-or-gate.md) is a candidate Duplicate closure of this part.
- **Tests** (`ElectricalOrTests`). Truth and supply: `SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer`, `TrueRowWithoutSupplyWireNeverCreatesPower`. Retraction, save, load and Reset: `ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact`. Order: `PlacementAndWiringOrderPreserveTruthTableAndConsumerResponse`. Monotone loops: `OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick`, `ContactLoopKeepsAnEitherGateOnThroughItsOtherInput`, `PureOrLoopIsAdmittedBySaveAndConstruct`. Mixed loops refused: `LoopsMixingANonmonotoneGateAreRefusedAtomicallyBeforeAnySolve`. Lamps and printing: `ElectricalOrPresentationTests.PictogramThreeTruthDotsAndThreeLampsFollowCommittedRowsThroughReset`. Motor: `MotorTests.AMotorSuppliedThroughAnEitherGateRunsOnlyWhileTheGateIsOn`.
- **Residuals.** The requirement admits Or cycles, but a wire from this gate's own `Supply` to its own input is refused as `InvalidConnection`; owner `ENGINE-SIGNAL-NODES` (its self-edge rule row); the EL-134 closure review is blocked on that row, after the engine programme (owner decision 3a). The Workbench connect tool refuses a same-part target (`ui/WorkshopConnections.cs:106`), so only a save or a command can carry this wire; it is not a player-reachable defect. The unread `ElectricalNodes` ratings (`:33`) are a required forward-only refactor, as for [CAT-013](CAT-013-both_gate.md#7-delivered-state-integration-and-residuals-22-november-2026). Requirement row CAT-026 stays unchecked until the browser phase.
- **Integration.** Condition sources: a Battery through an Impact switch contact. Consumers: Powered gate and Motor. No campaign level places it. Coverage binding: catalogue key `electrical_or`.
