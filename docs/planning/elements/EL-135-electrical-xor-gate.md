# EL-135 · Electrical XOR gate — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents. Shared gate facts E1–E10 are in [EL-133](EL-133-electrical-and-gate.md#shared-gate-facts-el-133el-137); the catalogue harvest is [CAT-027](CAT-027-electrical_xor.md) with shared facts in [CAT-013](CAT-013-both_gate.md) and the supplied network in [CAT-005](CAT-005-battery.md).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-135 · Electrical XOR gate |
| Type | Electrical |
| Anchor | [requirements.md#element-135](../requirements.md#element-135); existing record [todo-364](../requirements.md#todo-364); [named-elements entry](../invest/named-elements.md#element-135); owner S407 |
| Related | Refines [CAT-027 electrical_xor](CAT-027-electrical_xor.md) ("Exactly one gate", [current-cat-027](../requirements.md#current-cat-027)). Siblings [EL-133](EL-133-electrical-and-gate.md), [EL-134](EL-134-electrical-or-gate.md), [EL-136](EL-136-electrical-nor-gate.md), [EL-137](EL-137-electrical-nand-gate.md). |
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
| Parameters | Operation fixed `LogicGateKind.Xor` (scene `Operation = 2`, `parts/scenes/electrical_xor.tscn@a6c914e:L1-L5`; enum `engine/LogicGate.cs@a6c914e:L6-L6`). Exactly two conditions: multi-input parity is not modelled ([component research](../../component-research.md#logic): "multi-input XOR parity would make 'Exactly one' misleading"). |
| Cosmetic curves and UI bindings | Shared three-lamp binding (E5; `parts/ElectricalLogicPart.cs@a6c914e:L72-L90`). |
| Art | Shared body; navy Xor pictogram on the face quad (`parts/ElectricalLogicPart.cs@a6c914e:L55-L66`; icon `ui/WorkshopIcons.cs@a6c914e:L77-L77`); gold truth-row dots for rows 01 and 10 at x −0.07 and 0.07, y −0.4 (`parts/ElectricalLogicPart.cs@a6c914e:L67-L70`). |
| Catalogue / inventory | Id `electrical_xor`, Title "Exactly one gate", Category Control, colour (0.4, 0.72, 0.79); Description "Passes the separate bottom supply when exactly one numbered input is powered. No supply means no output. The numbered inputs are conditions, not the source of output power." (`parts/catalog/electrical_xor.tres@a6c914e:L6-L11`). |

**Variants.** None beyond the fixed operation (`Logic=Xor`, [current-cat-027](../requirements.md#current-cat-027)). Truth table (E6): 00 → 0, 01 → 1, 10 → 1, 11 → 0; output = `PowerIn` ∧ (A ≠ B). Xor is nonmonotone: a second input arriving (10 → 11) retracts the output in the same solve (E10); any zero-delay cycle through an Xor rejects at construction or Run, even through an open switch (E8).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S407): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction).

**Exists now**
- Static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`); Electrical domain enum values, not admitted (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).

**Missing**
- Supplied network — Story 8.1; gate element and nonmonotone-cycle rejection with part-naming status message — Story 9.6; this entry — Story 9.8; typed roles [S257](../invest/decisions.md#s257).

**Dependencies.** EL-133 / Story 9.6; Battery (CAT-005); condition sources; a supplied load; an Or gate (EL-134) for the reconvergence control.

## 4. Sources and legacy

- Requirement row [element-135](../requirements.md#element-135): "Separately supplied electrical output follows exactly one input present; A/B inputs carry conditions, not output energy"; outcome "Prove every input combination plus absent supply; logically true cannot create power". [current-cat-027](../requirements.md#current-cat-027): "enumerate all four rows with/without supply and output retraction. Reject zero-delay nonmonotone feedback atomically".
- Named entry [element-135](../invest/named-elements.md#element-135), owner S407; [electrical profile](../invest/profiles.md#electrical).
- Legacy: E1–E10; Xor-specific: reconvergent retraction (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L63-L90`), feedback rejection and open-switch case (`CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L91-L129`), rejection message "Break the wire loop through <part ids>. XOR, NOR and NAND outputs cannot feed their own inputs." (`engine/ElectricalNetwork.cs@a6c914e:L116-L122`).
- **Files harvested:** `parts/ElectricalLogicPart.cs`, `parts/catalog/electrical_xor.tres`, `parts/scenes/electrical_xor.tscn`, `engine/LogicGate.cs`, `engine/BinaryCircuit.cs`, `engine/ElectricalNetwork.cs`, `CuriousContraptions.tests/ElectricalLogicTests.cs`.

## 5. Acceptance outline

Acceptance authority: [element-135](../requirements.md#element-135), [current-cat-027](../requirements.md#current-cat-027); Story 9.8 `tools/e2e/cat-026-027.test.ts`.

- **Construction (actual Chrome UI).** Battery → gate `PowerIn`; conditions → `FirstIn`/`SecondIn`; gate `Supply` → supplied load.
- **Positive.** Rows 01 and 10 with supply power the load.
- **Negative / control.** Rows 00 and 11 with supply: off; any row without supply: off (outcome). Wiring the gate's output back to its own input is refused in build mode with the short loop message on the bottom status line ([DESIGN.md](../../../DESIGN.md#supplied-electrical-logic)); nothing is powered and the construction is unchanged.
- **Boundaries.** 10 → 11 through an upstream Or retracts the load in the same tick (E10); reversed wiring order.
- **Run/Reset.** Reset restores unpowered state exactly.
- **Save/Load.** Socket names round-trip; a saved nonmonotone loop is rejected at load atomically.
- **Integrations.** Retained gate behaviour [todo-362](../requirements.md#todo-362) and the per-variant rule [todo-364](../requirements.md#todo-364); interaction processes [IX-06 electrical power transfer](../requirements.md#interaction-06) and [IX-07 signal propagation](../requirements.md#interaction-07) (with the 10 → 11 retraction control through an upstream Or, [EL-134](EL-134-electrical-or-gate.md)). Campaign: the logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "separately supplied electrical AND/OR/XOR/NOR/NAND", first use 21–40 (reuse 41–80, 101–150).

## 6. Open questions

none beyond [EL-133](EL-133-electrical-and-gate.md#6-open-questions).
