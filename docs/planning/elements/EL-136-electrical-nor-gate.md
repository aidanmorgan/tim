# EL-136 · Electrical NOR gate — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents. Shared gate facts E1–E10 are in [EL-133](EL-133-electrical-and-gate.md#shared-gate-facts-el-133el-137); the catalogue harvest is [CAT-025](CAT-025-electrical_nor.md) with shared facts in [CAT-013](CAT-013-both_gate.md) and the supplied network in [CAT-005](CAT-005-battery.md).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-136 · Electrical NOR gate |
| Type | Electrical |
| Anchor | [requirements.md#element-136](../requirements.md#element-136); existing record [todo-364](../requirements.md#todo-364); [named-elements entry](../invest/named-elements.md#element-136); owner S408 |
| Related | Refines [CAT-025 electrical_nor](CAT-025-electrical_nor.md) ("Neither gate", [current-cat-025](../requirements.md#current-cat-025)). Siblings [EL-133](EL-133-electrical-and-gate.md), [EL-134](EL-134-electrical-or-gate.md), [EL-135](EL-135-electrical-xor-gate.md), [EL-137](EL-137-electrical-nand-gate.md). |
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
| Work and energy stores | none; output energy only from `PowerIn` (E4) — the "true with no inputs" row still needs the independent supply. |
| Parameters | Operation fixed `LogicGateKind.Nor` (scene `Operation = 3`, `parts/scenes/electrical_nor.tscn@a6c914e:L1-L5`; enum `engine/LogicGate.cs@a6c914e:L6-L6`). No numeric parameter. |
| Cosmetic curves and UI bindings | Shared three-lamp binding (E5; `parts/ElectricalLogicPart.cs@a6c914e:L72-L90`): with no inputs and supply present, both input lamps slate and the output lamp gold. |
| Art | Shared body; navy Nor pictogram on the face quad (`parts/ElectricalLogicPart.cs@a6c914e:L55-L66`; icon `ui/WorkshopIcons.cs@a6c914e:L78-L78`); one gold truth-row dot for row 00 at x −0.21, y −0.4 (`parts/ElectricalLogicPart.cs@a6c914e:L67-L70`). |
| Catalogue / inventory | Id `electrical_nor`, Title "Neither gate", Category Control, colour (0.4, 0.72, 0.79); Description "Passes the separate bottom supply when neither numbered input is powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`parts/catalog/electrical_nor.tres@a6c914e:L6-L11`). |

**Variants.** None beyond the fixed operation (`Logic=Nor`, [current-cat-025](../requirements.md#current-cat-025)). Truth table (E6): 00 → 1, 01 → 0, 10 → 0, 11 → 0; output = `PowerIn` ∧ ¬A ∧ ¬B. Nonmonotone: any zero-delay cycle through a Nor rejects (E8); an arriving input retracts the output in the same solve.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S408): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction).

**Exists now**
- Static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`); Electrical domain enum values, not admitted (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).

**Missing**
- Supplied network — Story 8.1; gate element and nonmonotone rejection — Story 9.6; this entry — Story 9.7; typed roles [S257](../invest/decisions.md#s257).

**Dependencies.** EL-133 / Story 9.6; Battery (CAT-005); condition sources; a supplied load.

## 4. Sources and legacy

- Requirement row [element-136](../requirements.md#element-136): "Separately supplied electrical output follows neither input present; A/B inputs carry conditions, not output energy"; outcome "Prove every input combination plus absent supply; logically true cannot create power". [current-cat-025](../requirements.md#current-cat-025): "True no-input state requires independent supply. Reject zero-delay nonmonotone feedback atomically including open switched routes".
- Named entry [element-136](../invest/named-elements.md#element-136), owner S408; [electrical profile](../invest/profiles.md#electrical); component research recipe "Quiet Lock: NOR between two sound detectors" ([component research](../../component-research.md#logic)).
- Legacy: E1–E10; Nor-specific: feedback rejection (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L91-L109`), rejection message (`engine/ElectricalNetwork.cs@a6c914e:L116-L122`).
- **Files harvested:** `parts/ElectricalLogicPart.cs`, `parts/catalog/electrical_nor.tres`, `parts/scenes/electrical_nor.tscn`, `engine/LogicGate.cs`, `engine/ElectricalNetwork.cs`, `CuriousContraptions.tests/ElectricalLogicTests.cs`.

## 5. Acceptance outline

Acceptance authority: [element-136](../requirements.md#element-136), [current-cat-025](../requirements.md#current-cat-025); Story 9.7 `tools/e2e/cat-024-025.test.ts`.

- **Construction (actual Chrome UI).** Battery → gate `PowerIn`; conditions → `FirstIn`/`SecondIn`; gate `Supply` → supplied load.
- **Positive.** Row 00 with supply: the load runs.
- **Negative / control.** Row 00 with no supply wire: the load stays off (outcome: "true" with no inputs cannot create power). Rows 01, 10, 11 with supply: off. A Nor output wired back into its own input is refused in build mode; nothing is powered.
- **Boundaries.** All 4 rows × supply on/off; an input arriving mid-run retracts the output on that tick; reversed wiring order.
- **Run/Reset.** Reset restores unpowered state exactly.
- **Save/Load.** Socket names round-trip; a saved Nor loop is rejected at load atomically.
- **Integrations.** Retained gate behaviour [todo-362](../requirements.md#todo-362) and the per-variant rule [todo-364](../requirements.md#todo-364); interaction processes [IX-06 electrical power transfer](../requirements.md#interaction-06) and [IX-07 signal propagation](../requirements.md#interaction-07) (the true no-input row still needs supply). Campaign: the logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "separately supplied electrical AND/OR/XOR/NOR/NAND", first use 21–40 (reuse 41–80, 101–150).

## 6. Open questions

none beyond [EL-133](EL-133-electrical-and-gate.md#6-open-questions).
