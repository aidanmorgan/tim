# EL-133 · Electrical AND gate — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents (legacy `AddBox` takes the full size and stores half-extents, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). This spec holds the **gate facts shared by EL-133–EL-137 (E1–E10)**; [EL-134](EL-134-electrical-or-gate.md), [EL-135](EL-135-electrical-xor-gate.md), [EL-136](EL-136-electrical-nor-gate.md) and [EL-137](EL-137-electrical-nand-gate.md) add only their operation. The catalogue-level harvest is in [CAT-013](CAT-013-both_gate.md) (G1–G8) and the supplied-network facts in [CAT-005](CAT-005-battery.md) (N1–N20).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-133 · Electrical AND gate |
| Type | Electrical |
| Anchor | [requirements.md#element-133](../requirements.md#element-133); existing record [todo-364](../requirements.md#todo-364); [named-elements entry](../invest/named-elements.md#element-133); owner S405 |
| Related | Refines [CAT-013 Both gate](CAT-013-both_gate.md) (`both_gate`, operation And; requirement [current-cat-013](../requirements.md#current-cat-013)). Supply from [CAT-005 Battery](CAT-005-battery.md) / [EL-196](EL-196-battery.md) through [EL-197 wires](EL-197-electrical-wire.md); conditions typically from [EL-198 Switch](EL-198-switch.md) contacts. |
| Roadmap story | 9.6 "Dual-Supply Both Gate" (Epic 9) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static body box 1.7 × 1.3 × 0.65 m at the origin and foot box 1.85 × 0.16 × 0.85 m at (0, −0.75, 0) (`parts/ElectricalLogicPart.cs@a6c914e:L45-L46`); shared by all five gates (E1). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 — the static default (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | `FirstIn` (Electrical, Input) (−0.94, 0.35, 0) — one raised mark; `SecondIn` (Electrical, Input) (−0.94, −0.35, 0) — two raised marks; `PowerIn` (Electrical, Input) (0, −0.75, 0.55) — the separate supply; `Supply` (Electrical, Output) (0.94, 0, 0) (`parts/ElectricalLogicPart.cs@a6c914e:L28-L34`) (E2). |
| Sensors and activation | none; no activation port. A/B are conditions read from committed electrical availability (E3). |
| Work and energy stores | none. Output energy comes only from `PowerIn`; when the supply is a finite store ([EL-196](EL-196-battery.md)), downstream demand debits that store, never the gate (E4). |
| Parameters | Operation = closed enum `LogicGateKind { And, Or, Xor, Nor, Nand }` (`engine/LogicGate.cs@a6c914e:L6-L6`), fixed to **And** for this catalogue entry, not player-editable (scene sets no value, enum default And: `parts/scenes/both_gate.tscn@a6c914e:L1-L4`). No numeric parameter. |
| Cosmetic curves and UI bindings | Three lamps slate `#556573` → gold `#f7cb52`, 0.1 s SmoothStep presentation transition, endpoint-driven: `FirstIn` lamp (−0.48, 0.3, 0.42) and `SecondIn` lamp (−0.48, −0.3, 0.42) follow committed input availability; output lamp (0.48, 0, 0.42) follows truth ∧ supplied (`parts/ElectricalLogicPart.cs@a6c914e:L22-L27`, `parts/ElectricalLogicPart.cs@a6c914e:L72-L90`, `parts/ElectricalLogicPart.cs@a6c914e:L37-L41`) (E5). |
| Art | Cyan `#66b8c9` body, navy `#293954` foot, cream `#fff8e9` face 1.45 × 1.05 × 0.04 at z 0.35; And-only: two navy converging path bars 0.65 × 0.035 × 0.025 at (−0.05, ±0.15, 0.395) rotated ∓27° (`parts/ElectricalLogicPart.cs@a6c914e:L45-L54`); input tick marks (`parts/ElectricalLogicPart.cs@a6c914e:L91-L93`); gold port spheres r 0.075 (`parts/ElectricalLogicPart.cs@a6c914e:L94-L94`). Toolbox icon `ui/WorkshopIcons.cs@a6c914e:L61-L61`. [DESIGN.md Supplied electrical logic](../../../DESIGN.md#supplied-electrical-logic). |
| Catalogue / inventory | Id `both_gate`, Title "Both gate", Category Control, colour (0.4, 0.72, 0.79); Description "Both numbered controls must be powered to pass the separate bottom supply to the output. Connect a battery to the bottom socket and switched conditions to the one-mark and two-mark sockets. No supply means no output." (`parts/catalog/both_gate.tres@a6c914e:L6-L11`). |

**Variants.** The requirements row names none beyond the fixed operation (`Logic=And` in [current-cat-013](../requirements.md#current-cat-013)). Truth table (E6): A B = 00 → 0, 01 → 0, 10 → 0, 11 → 1; output = `PowerIn` ∧ (A ∧ B).

### Shared gate facts (EL-133–EL-137)

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| E1 | All five gates share one body, foot and port layout; only the operation and face art differ. | `parts/ElectricalLogicPart.cs@a6c914e:L11-L46` | carry forward as one declaration with an operation enum |
| E2 | Ports FirstIn/SecondIn/PowerIn inputs and Supply output; gate rule (operation, FirstIn, SecondIn, PowerIn, Supply). | `parts/ElectricalLogicPart.cs@a6c914e:L28-L36` | carry forward |
| E3 | Truth reads committed availability of FirstIn and SecondIn; the state enum is Neither/FirstOnly/SecondOnly/Both. | `parts/ElectricalLogicPart.cs@a6c914e:L8-L8`, `parts/ElectricalLogicPart.cs@a6c914e:L18-L21` | carry forward |
| E4 | A gate output node is powered only if its supply node is powered and the operation is true; conditions and supply must be distinct nodes. | `engine/BinaryCircuit.cs@a6c914e:L86-L96`, `engine/BinaryCircuit.cs@a6c914e:L136-L138` | carry forward |
| E5 | Owner active = truth ∧ PowerIn supplied; that drives the output lamp. | `parts/ElectricalLogicPart.cs@a6c914e:L37-L41` | carry forward; do not carry forward the per-part `PreparePhysics` loop |
| E6 | Explicit truth table, rows 00, 01, 10, 11: And F F F T; Or F T T T; Xor F T T F; Nor T F F F; Nand T T T F. | `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L5-L19`, `engine/LogicGate.cs@a6c914e:L11-L19` | carry forward (acceptance table) |
| E7 | Every operation × truth row × supply on/off × reversed ids: load powered iff supply ∧ truth; reconnecting in reverse order gives the same result; removing every battery link leaves the load unpowered. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L26-L62` | carry forward |
| E8 | Any cycle containing Xor, Nor or Nand rejects at construction (and at Run even through an open switch), naming the parts; nothing is partially powered and the snapshot is unchanged. And/Or cycles settle from the unpowered least fixed point. | `engine/BinaryCircuit.cs@a6c914e:L104-L112`, `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L91-L129` | carry forward |
| E9 | Monotone (And/Or) feedback cannot remember a condition after its source is removed. | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L131-L157` | carry forward |
| E10 | A reconvergent second input retracts an Xor/Nand output within the same solve (10 → 11). | `CuriousContraptions.tests/ElectricalLogicTests.cs@a6c914e:L63-L90` | carry forward (EL-135, EL-137) |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S405): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction).

**Exists now**
- Static boxes: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`.
- `WorkshopConnectionDomain.Electrical`, `Supply`, `PowerIn` enum values (rejected by validation): `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`, `engine/gpu/WorkshopConnections.cs@a6c914e:L89-L92`.

**Missing**
- Supplied binary network (sources, wires, contacts) compiled at Run and solved on the worker — Story 8.1 ([CAT-005](CAT-005-battery.md#3-engine-capabilities)); typed power versus condition roles — [S257](../invest/decisions.md#s257) electrical-port.
- Gate elements, `FirstIn`/`SecondIn` sockets, nonmonotone-cycle rejection — Story 9.6 (this identity), reused by 9.7/9.8.
- Input-availability cosmetic source for the lamps — Story 9.6 (current sources: `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`).
- FiniteLedger supply accounting — [EL-196](EL-196-battery.md) (owner decision).

**Dependencies.** Battery (CAT-005) for supply and conditions; switched conditions need the [EL-198](EL-198-switch.md) contact; a supplied load (Motor CAT-042 or Powered gate CAT-051) to observe the output.

## 4. Sources and legacy

- Requirement row [element-133](../requirements.md#element-133): "Separately supplied electrical output follows both inputs present; A/B inputs carry conditions, not output energy"; outcome "Prove every input combination plus absent supply; logically true cannot create power". [todo-364](../requirements.md#todo-364): each gate variant has its own playable entry, icon and separately supplied output.
- Named entry [element-133](../invest/named-elements.md#element-133), owner S405; [electrical profile](../invest/profiles.md#electrical); component research logic family ([component research](../../component-research.md#logic)).
- Legacy harvest: E1–E10 above; CAT-013 G1–G8 and CAT-005 N1–N20 cover the same files in more depth.
- **Files harvested:** `parts/ElectricalLogicPart.cs`, `parts/catalog/both_gate.tres`, `parts/scenes/both_gate.tscn`, `engine/LogicGate.cs`, `engine/BinaryCircuit.cs`, `CuriousContraptions.tests/LogicGateTests.cs`, `CuriousContraptions.tests/ElectricalLogicTests.cs`, `reference/cpu/MachinePart.cs` (`AddBox` extents).

## 5. Acceptance outline

Acceptance authority: [element-133](../requirements.md#element-133), [current-cat-013](../requirements.md#current-cat-013); Story 9.6 `tools/e2e/cat-013.test.ts`.

- **Construction (actual Chrome UI).** Place Battery, Both gate, two condition sources (Switch contacts or second battery) and a supplied load; Connect battery `Supply` → gate, choose `Supply → PowerIn`; condition sources → `FirstIn` and `SecondIn` through the socket-choice buttons; gate `Supply` → load `PowerIn`.
- **Positive.** Row 11 with supply: output lamp gold, load runs.
- **Negative / control.** Rows 00, 01, 10 with supply: output slate, load off. Row 11 without supply: both input lamps gold, output slate, load off (outcome: logical truth cannot create power). Wiring a battery into `FirstIn` alone never powers the output.
- **Boundaries.** Every row × supply on/off (8 cases); reversed connection order gives the same result (E7); And/Or loop settles and forgets a removed source (E9).
- **Run/Reset.** Reset restores unpowered lamps and the authored wiring exactly.
- **Save/Load.** Wires round-trip with exact socket names (`first_in`, `second_in`, `power_in`, `supply`, CAT-005 N16).
- **Integrations.** Retained gate behaviour [todo-362](../requirements.md#todo-362) and the per-variant rule [todo-364](../requirements.md#todo-364) ("own playable catalogue entry, toolbox/face icon and separately supplied output"); interaction processes [IX-06 electrical power transfer](../requirements.md#interaction-06) (supply to the load) and [IX-07 signal propagation](../requirements.md#interaction-07) ("a true signal alone cannot power a load"). Campaign: the logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "separately supplied electrical AND/OR/XOR/NOR/NAND", first use 21–40 (reuse 41–80, 101–150).

## 6. Open questions

These apply to all five gates (EL-133–EL-137).

1. **Observable consumer.** Story 9.6's AC drives "an output lamp", but the Signal lamp is activation-only; which supplied consumer proves the gate (Motor, Story 11.1, or Powered gate, Story 8.2, Powered gate before Epic 9, Motor after it): [CAT-013 open question 1](CAT-013-both_gate.md#6-open-questions), owner decision.
2. **Condition sources.** Story 9.6's AC uses "two separate Battery circuits"; switched conditions need the EL-198 contact mode, which is not scheduled before 9.6: owner decision on the first condition source ([CAT-013 open question 2](CAT-013-both_gate.md#6-open-questions)).
3. **binary16 activation and cosmetic lanes.** The three-lamp binding extends cosmetic lanes and activation times that are still binary16, listed as remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L96-L98`). Whether Story 9.6 migrates them to f32 first or extends them as they are: [CAT-013 open question 3](CAT-013-both_gate.md#6-open-questions), owner decision.
4. **Finite supply.** Whether gate outputs carry a power limit from a finite battery (EL-196) at Story 9.6 or later: owner decision.
