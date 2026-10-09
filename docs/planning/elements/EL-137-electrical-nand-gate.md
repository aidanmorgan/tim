# EL-137 · Electrical NAND gate — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents. Shared gate facts E1–E10 are in [EL-133](EL-133-electrical-and-gate.md#shared-gate-facts-el-133el-137); the catalogue harvest is [CAT-024](CAT-024-electrical_nand.md) with shared facts in [CAT-013](CAT-013-both_gate.md) and the supplied network in [CAT-005](CAT-005-battery.md).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-137 · Electrical NAND gate |
| Type | Electrical |
| Anchor | [requirements.md#element-137](../requirements.md#element-137); existing record [todo-364](../requirements.md#todo-364); [named-elements entry](../invest/named-elements.md#element-137); owner S409 |
| Related | Refines [CAT-024 electrical_nand](CAT-024-electrical_nand.md) ("Not both gate", [current-cat-024](../requirements.md#current-cat-024)). Siblings [EL-133](EL-133-electrical-and-gate.md), [EL-134](EL-134-electrical-or-gate.md), [EL-135](EL-135-electrical-xor-gate.md), [EL-136](EL-136-electrical-nor-gate.md). |
| Roadmap story | 9.7 "Electrical Logic Gates NAND & NOR" (Epic 9) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Shared gate body: box 1.7 × 1.3 × 0.65 m at the origin, foot 1.85 × 0.16 × 0.85 m at (0, −0.75, 0) (`parts/ElectricalLogicPart.cs@a6c914e:L45-L46`). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | `FirstIn` (−0.94, 0.35, 0), `SecondIn` (−0.94, −0.35, 0), `PowerIn` (0, −0.75, 0.55) inputs; `Supply` (0.94, 0, 0) output, all Electrical (`parts/ElectricalLogicPart.cs@a6c914e:L28-L34`). |
| Sensors and activation | none. |
| Work and energy stores | none; output energy only from `PowerIn` (E4) — the true no-input rows still need the independent supply. |
| Parameters | Operation fixed `LogicGateKind.Nand` (scene `Operation = 4`, `parts/scenes/electrical_nand.tscn@a6c914e:L1-L5`; enum `engine/LogicGate.cs@a6c914e:L6-L6`). No numeric parameter. |
| Cosmetic curves and UI bindings | Shared three-lamp binding (E5; `parts/ElectricalLogicPart.cs@a6c914e:L72-L90`). |
| Art | Shared body; navy Nand pictogram on the face quad (`parts/ElectricalLogicPart.cs@a6c914e:L55-L66`; icon `ui/WorkshopIcons.cs@a6c914e:L79-L79`); gold truth-row dots for rows 00, 01, 10 at x −0.21, −0.07, 0.07, y −0.4 (`parts/ElectricalLogicPart.cs@a6c914e:L67-L70`). |
| Catalogue / inventory | Id `electrical_nand`, Title "Not both gate", Category Control, colour (0.4, 0.72, 0.79); Description "Passes the separate bottom supply when the numbered inputs are not both powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`parts/catalog/electrical_nand.tres@a6c914e:L6-L11`). |

**Variants.** None beyond the fixed operation (`Logic=Nand`, [current-cat-024](../requirements.md#current-cat-024)). Truth table (E6): 00 → 1, 01 → 1, 10 → 1, 11 → 0; output = `PowerIn` ∧ ¬(A ∧ B). Nonmonotone: the second input arriving (10 → 11) retracts the output in the same solve (E10); any zero-delay cycle through a Nand rejects, even through an open switched route (E8).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S409): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction).

**Exists now**
- Static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`); Electrical domain enum values, not admitted (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).

**Missing**
- Supplied network — Story 8.1; gate element and nonmonotone rejection — Story 9.6; this entry — Story 9.7; typed roles [S257](../invest/decisions.md#s257).

**Dependencies.** EL-133 / Story 9.6; Battery (CAT-005); condition sources; a supplied load; an Or gate (EL-134) for the reconvergence control.

## 4. Sources and legacy

- Requirement row [element-137](../requirements.md#element-137): "Separately supplied electrical output follows not both inputs present; A/B inputs carry conditions, not output energy"; outcome "Prove every input combination plus absent supply; logically true cannot create power". [current-cat-024](../requirements.md#current-cat-024): "A true no-input condition cannot create power. Reject zero-delay nonmonotone feedback atomically even through open switched routes; test reordering/reconvergence".
- Named entry [element-137](../invest/named-elements.md#element-137), owner S409; [electrical profile](../invest/profiles.md#electrical); recipe "Overflow Interlock" warns against deriving a command from the gate's own output ([component research](../../component-research.md#logic)).
- Legacy: E1–E10; Nand-specific: reconvergent retraction (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L63-L90`), feedback rejection (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L91-L109`), message (`engine/ElectricalNetwork.cs@a6c914e:L116-L122`).
- **Files harvested:** `parts/ElectricalLogicPart.cs`, `parts/catalog/electrical_nand.tres`, `parts/scenes/electrical_nand.tscn`, `engine/LogicGate.cs`, `engine/ElectricalNetwork.cs`, `CuriousContraptions.tests/ElectricalLogicTests.cs`.

## 5. Acceptance outline

Acceptance authority: [element-137](../requirements.md#element-137), [current-cat-024](../requirements.md#current-cat-024); Story 9.7 `tools/e2e/cat-024-025.test.ts`.

- **Construction (actual Chrome UI).** Battery → gate `PowerIn`; conditions → `FirstIn`/`SecondIn`; gate `Supply` → supplied load.
- **Positive.** Rows 00, 01, 10 with supply power the load.
- **Negative / control.** Row 00 with no supply: off (outcome). Row 11 with supply: off. A Nand loop is refused in build mode with the loop message; nothing is powered.
- **Boundaries.** All 4 rows × supply on/off; 10 → 11 through an upstream Or retracts the load the same tick; reversed wiring order.
- **Run/Reset.** Reset restores unpowered state exactly.
- **Save/Load.** Socket names round-trip; a saved Nand loop is rejected at load atomically.
- **Integrations.** Retained gate behaviour [todo-362](../requirements.md#todo-362) and the per-variant rule [todo-364](../requirements.md#todo-364); interaction processes [IX-06 electrical power transfer](../requirements.md#interaction-06) and [IX-07 signal propagation](../requirements.md#interaction-07) (with the 10 → 11 retraction control through an upstream Or, [EL-134](EL-134-electrical-or-gate.md)). Campaign: the logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "separately supplied electrical AND/OR/XOR/NOR/NAND", first use 21–40 (reuse 41–80, 101–150).

## 6. Open questions

none beyond [EL-133](EL-133-electrical-and-gate.md#6-open-questions).
