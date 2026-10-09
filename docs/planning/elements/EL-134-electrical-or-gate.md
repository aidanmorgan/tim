# EL-134 · Electrical OR gate — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents. Shared gate facts E1–E10 are in [EL-133](EL-133-electrical-and-gate.md#shared-gate-facts-el-133el-137); the catalogue harvest is [CAT-026](CAT-026-electrical_or.md) with shared facts in [CAT-013](CAT-013-both_gate.md) and the supplied network in [CAT-005](CAT-005-battery.md).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-134 · Electrical OR gate |
| Type | Electrical |
| Anchor | [requirements.md#element-134](../requirements.md#element-134); existing record [todo-364](../requirements.md#todo-364); [named-elements entry](../invest/named-elements.md#element-134); owner S406 |
| Related | Refines [CAT-026 electrical_or](CAT-026-electrical_or.md) ("Either gate", [current-cat-026](../requirements.md#current-cat-026)). Siblings [EL-133](EL-133-electrical-and-gate.md), [EL-135](EL-135-electrical-xor-gate.md), [EL-136](EL-136-electrical-nor-gate.md), [EL-137](EL-137-electrical-nand-gate.md). |
| Roadmap story | 9.8 "Electrical Logic Gates OR & XOR" (Epic 9) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Shared gate body: box 1.7 × 1.3 × 0.65 m at the origin, foot 1.85 × 0.16 × 0.85 m at (0, −0.75, 0) (`parts/ElectricalLogicPart.cs@a6c914e:L45-L46`). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | `FirstIn` (−0.94, 0.35, 0), `SecondIn` (−0.94, −0.35, 0), `PowerIn` (0, −0.75, 0.55) inputs; `Supply` (0.94, 0, 0) output, all Electrical (`parts/ElectricalLogicPart.cs@a6c914e:L28-L34`). |
| Sensors and activation | none. |
| Work and energy stores | none; output energy only from `PowerIn` (E4). |
| Parameters | Operation fixed `LogicGateKind.Or` (scene `Operation = 1`, `parts/scenes/electrical_or.tscn@a6c914e:L1-L5`; enum `engine/LogicGate.cs@a6c914e:L6-L6`). No numeric parameter. |
| Cosmetic curves and UI bindings | Shared three-lamp binding (E5; `parts/ElectricalLogicPart.cs@a6c914e:L72-L90`). |
| Art | Shared cyan/navy/cream body (`parts/ElectricalLogicPart.cs@a6c914e:L45-L47`); non-And face: navy operation pictogram on a 0.65 × 0.65 quad at z 0.41 matching the toolbox icon (`parts/ElectricalLogicPart.cs@a6c914e:L55-L66`; icon `ui/WorkshopIcons.cs@a6c914e:L76-L76`); raised gold `#e8b764` truth-row dots r 0.035 at y −0.4, x = −0.21 + 0.14·row for each true row — for Or rows 01, 10, 11 at x −0.07, 0.07, 0.21 (`parts/ElectricalLogicPart.cs@a6c914e:L67-L70`). |
| Catalogue / inventory | Id `electrical_or`, Title "Either gate", Category Control, colour (0.4, 0.72, 0.79); Description "Passes the separate bottom supply when either numbered input is powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`parts/catalog/electrical_or.tres@a6c914e:L6-L11`). |

**Variants.** None beyond the fixed operation (`Logic=Or`, [current-cat-026](../requirements.md#current-cat-026)). Truth table (E6): 00 → 0, 01 → 1, 10 → 1, 11 → 1; output = `PowerIn` ∧ (A ∨ B). Or is monotone: an Or cycle is admitted and settles from the unpowered least fixed point each solve (E8, E9).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S406): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction).

**Exists now**
- Static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`); Electrical domain and `Supply`/`PowerIn` enum values, not admitted (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).

**Missing**
- Supplied network — Story 8.1; gate element and condition sockets — Story 9.6; this operation's entry, icon face and truth rows — Story 9.8; typed roles [S257](../invest/decisions.md#s257).

**Dependencies.** EL-133 / Story 9.6 gate element; Battery (CAT-005); condition sources (EL-198 contacts or batteries); a supplied load.

## 4. Sources and legacy

- Requirement row [element-134](../requirements.md#element-134): "Separately supplied electrical output follows at least one input present; A/B inputs carry conditions, not output energy"; outcome "Prove every input combination plus absent supply; logically true cannot create power". [current-cat-026](../requirements.md#current-cat-026): "And/Or strongly connected components restart from unpowered least fixed point each solve. Test source-free loop, missing supply, reorder/reconvergence".
- Named entry [element-134](../invest/named-elements.md#element-134), owner S406; [electrical profile](../invest/profiles.md#electrical).
- Legacy: E1–E10 ([EL-133](EL-133-electrical-and-gate.md#shared-gate-facts-el-133el-137)); Or-specific: the operation value and catalogue text above; Or participates in monotone-feedback tests (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L131-L157`) and as the upstream gate in the reconvergence test (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L63-L90`).
- **Files harvested:** `parts/ElectricalLogicPart.cs`, `parts/catalog/electrical_or.tres`, `parts/scenes/electrical_or.tscn`, `engine/LogicGate.cs`, `CuriousContraptions.tests/ElectricalLogicTests.cs`.

## 5. Acceptance outline

Acceptance authority: [element-134](../requirements.md#element-134), [current-cat-026](../requirements.md#current-cat-026); Story 9.8 `tools/e2e/cat-026-027.test.ts`.

- **Construction (actual Chrome UI).** Battery → gate `PowerIn`; condition sources → `FirstIn`/`SecondIn` via the socket-choice buttons; gate `Supply` → supplied load.
- **Positive.** Rows 01, 10 and 11 with supply power the load; the matching input lamps light.
- **Negative / control.** Row 00 with supply: off. Any row without supply: output off although input lamps may be lit (outcome). A source-free Or loop stays unpowered.
- **Boundaries.** All 4 rows × supply on/off; reversed wiring order; Or loop forgets a removed source (E9).
- **Run/Reset.** Reset restores unpowered state exactly.
- **Save/Load.** Socket names round-trip.
- **Integrations.** Retained gate behaviour [todo-362](../requirements.md#todo-362) and the per-variant rule [todo-364](../requirements.md#todo-364); interaction processes [IX-06 electrical power transfer](../requirements.md#interaction-06) and [IX-07 signal propagation](../requirements.md#interaction-07). Campaign: the logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "separately supplied electrical AND/OR/XOR/NOR/NAND", first use 21–40 (reuse 41–80, 101–150).

## 6. Open questions

none beyond [EL-133](EL-133-electrical-and-gate.md#6-open-questions) (observable consumer, condition sources, binary16 lanes, finite supply).
