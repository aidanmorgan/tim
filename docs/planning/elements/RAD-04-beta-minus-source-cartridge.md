# RAD-04 · Beta-minus source cartridge — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-04](../requirements.md#radiation-04) (P2 potential). A fictional sealed toy cartridge in game units; never real-world source guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-04 · Beta-minus source cartridge |
| Type | Radiation (charged-particle source) |
| Anchor | [requirements.md#radiation-04](../requirements.md#radiation-04); [named-elements.md#radiation-04](../invest/named-elements.md#radiation-04) |
| Related identities | Distinct sibling [RAD-03 Alpha](RAD-03-alpha-source-cartridge.md) (separate inventory item); receiver [RAD-08](RAD-08-radiation-rate-meter.md) BetaMinus mode; [EL-126 Thin screen](EL-126-thin-screen-shield.md) passes it, [EL-127 Polymer](EL-127-polymer-shield.md) suppresses it; opposite bending with [RAD-14](RAD-14-magnetic-deflector.md). No CAT spec. |
| Proof owner | S621 |
| Roadmap story | unscheduled; campaign 116, 119 (research slots 91, 94) |
| Status | not started |

## 2. Declaration

The requirement fixes: negative charge; a range/material response different from alpha; a taught thin screen passes enough beta to detect while a polymer screen suppresses it; opposite bending is proven with the deflector. Values are proposals.

- **Bodies and shapes.** Static box 0.25 × 0.30 × 0.25 m with an outlet window 0.08 × 0.08 m flush on local +X (proposed: the same cartridge envelope as alpha so geometry is not the cue; the outlet stays unobstructed).
- **Mass and material.** Static, zero mass; static default contact material (1, 0.1 m/s, 0.3) (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** none.
- **Sensors and activation.** none.
- **Source declaration.** `RadiationKind.BetaMinus`, charge sign negative. Activity A = 16 game units/s; 10° half-angle cone from the outlet (proposed: matches alpha so only range and charge differ; narrow enough for the RAD-14 field-off control). Air range 3.0 m (proposed: through a 0.02 m thin screen, air-equivalent 2.2 m, a meter whose air path is up to 0.8 m still responds; 48 cells, inside the research range). Range model as in [RAD-03](RAD-03-alpha-source-cartridge.md#2-declaration): admitted while the air-equivalent length is within range, then zero; [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration) inside range.
- **Reference readings.** Meter 0.5 m away through a thin screen: air-equivalent 2.2 + 0.48 = 2.68 m ≤ 3.0 m, only the centre sample inside the cone, reading ≈ 7.1 (above On 4). Through a Standard polymer shield: 15 m air-equivalent, reading 0.
- **Magnetic response (for RAD-14).** k_BetaMinus = 4 m: curvature radius 1.0 m at default field strength 4, opposite sense to alpha (proposed: tighter than alpha's 1.5 m, yet clear of the 0.40 m-long region's poles — exit offset 0.0835 m, exit angle 23.6°).
- **Work and energy stores.** Emitted-quanta ledger; steady within a Run.
- **Parameters.** none.
- **Cosmetic curves and UI bindings.** Cap mark; selected-only dotted range arc ending at 3.0 m.
- **Art.** Cream `#fff8e9` cartridge, gold `#f7cb52` cap engraved "β⁻" with two dots, navy `#293954` outlet ring (proposed: two dots versus alpha's one).
- **Catalogue and inventory.** Id `beta_minus_source_cartridge`, title "Beta-minus cartridge", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** None in the row.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); air-equivalent and curved path queries: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); signed charged range model ([S605 beta-minus row](../invest/decisions.md#s605): "through a field the trail bends one way; reversed polarity bends the other; no field goes straight") |
| RadioactiveDecay | missing | unscheduled (S607-D/F); steady case |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; source state joins it with this element (unscheduled) |

Also used for the bending proof: FieldForce (missing; unscheduled, LAW-FIELD-D).

**Dependencies.** RAD-08 (BetaMinus mode), EL-126, EL-127; RAD-14 for the bending proof.

## 4. Sources and legacy

- [radiation-04](../requirements.md#radiation-04): "A taught thin screen passes enough beta to detect while a polymer screen suppresses it; opposite bending is proven with the deflector."
- [Named entry](../invest/named-elements.md#radiation-04), owner S621; map row; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) beta-minus row; research row ("passes paper, stopped by polymer, bent by a field"; slots 91 "Beyond the Paper", 94 "Opposite Ways").
- **Legacy.** None. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1721-L1751`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Cartridge facing a supplied BetaMinus-mode Rate meter 0.5 m away, contact → Powered gate; drag a Thin screen onto the path, then replace it with a Polymer shield.
- **Positive.** Thin screen: reading ≈ 7.1, meter on, gate releases.
- **Negative / controls.** Polymer shield: reading 0. Alpha cartridge in the same layout with the thin screen: 0 (2.2 m air-equivalent exceeds alpha's 2.0 m range). Photon-mode meter: 0. With RAD-14 at default strength, beta lands at +0.52 m while alpha lands at −0.331 m under Forward polarity; field off goes straight (see the [RAD-14 layouts](RAD-14-magnetic-deflector.md#5-acceptance-outline)).
- **Boundaries.** 3.0 m range edge in air; thin screen with a 0.8 m air path (admitted) versus 0.9 m (stopped); cone edge.
- **Run/Reset.** Pose and initial state restore.
- **Save/Load.** Pose round-trips.
- **Integrations.** Source partner in the cross-element task [radiation-05 integration (sequence-task-413)](../requirements.md#sequence-task-413) (thin screen passes, polymer suppresses) and in the RAD-14 and RAD-15 routing proofs; interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15) and [IX-16 Radioactive decay](../requirements.md#interaction-16) (steady case); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 111–120 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 12 of the [campaign plan](../requirements.md#campaign-plan)), reuse 121–125 and 136–150.

## 6. Open questions

1. Proposed range, stopping factors, cone and k_BetaMinus need S606-D and LAW-FIELD-D.
2. Should beta show a spectrum (tapered range) rather than one range — owner decision.
