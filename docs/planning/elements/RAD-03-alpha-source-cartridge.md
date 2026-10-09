# RAD-03 · Alpha source cartridge — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-03](../requirements.md#radiation-03) (P2 potential). A fictional sealed toy cartridge in game units; never real-world source guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-03 · Alpha source cartridge |
| Type | Radiation (charged-particle source) |
| Anchor | [requirements.md#radiation-03](../requirements.md#radiation-03); [named-elements.md#radiation-03](../invest/named-elements.md#radiation-03) |
| Related identities | Distinct sibling [RAD-04 Beta-minus](RAD-04-beta-minus-source-cartridge.md); receiver [RAD-08](RAD-08-radiation-rate-meter.md) Alpha mode; stopper [EL-126 Thin screen](EL-126-thin-screen-shield.md); later steering [RAD-14](RAD-14-magnetic-deflector.md), [RAD-15](RAD-15-track-chamber.md). No CAT spec. |
| Proof owner | S620 |
| Roadmap story | unscheduled; campaign 115, 118 (research slots 90, 93) |
| Status | not started |

## 2. Declaration

The requirement fixes: short range, positive charge, an explicitly modelled emission window that its own enclosure does not block; a close compatible detector responds; separation or a thin screen suppresses it; gamma-only reception rejects it. Values are proposals.

- **Bodies and shapes.** Static box 0.25 × 0.30 × 0.25 m (proposed: a small cartridge, smaller than the gamma capsule). Outlet window 0.08 × 0.08 m on local +X, flush with the face (proposed: the emitting surface sits on the outer face so the body's own collider is never on the path; centre-sampled under the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration)).
- **Mass and material.** Static, zero mass; static default contact material (1, 0.1 m/s, 0.3) (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** none (no wire; emission is continuous during Run).
- **Sensors and activation.** none.
- **Source declaration.** `RadiationKind.Alpha`, charge sign positive. Activity A = 16 game units/s (proposed: same as the other sources so range, not strength, is the lesson). Emission window: a 10° half-angle cone about local +X (proposed: a narrow beam so the RAD-14 field-off control misses a meter placed on the bent route). Air range 2.0 m (proposed: long enough for the RAD-14 opposite-charge layout, path ≈ 1.74 m; 32 cells at 1/16 m, inside the research range of 1/4–64 cells). Range model: a path is admitted while its air-equivalent length (air metres plus material thickness × stopping factor) stays within the range, else T = 0 (proposed: a readable step, not a tail).
- **Reference readings** (shared law): a meter face 0.5 m away has only its centre sample inside the 10° cone, reading 16 / 0.25 / 9 ≈ 7.1 (above On 4); at 1.0 m every sample is inside the cone, reading ≈ 15.8; beyond a 2.0 m path the reading is 0.
- **Magnetic response (for RAD-14).** k_Alpha = 6 m: curvature radius 1.5 m at the deflector's default strength 4 (proposed: gentler than beta-minus's 1.0 m, as the requirements forbid equal radii).
- **Work and energy stores.** Emitted-quanta ledger; steady activity within a Run.
- **Parameters.** none.
- **Cosmetic curves and UI bindings.** Cap mark (research row); selected-only dotted range arc ending at 2.0 m.
- **Art.** Cream `#fff8e9` cartridge with a gold `#f7cb52` brass-like cap engraved "α" and one dot, navy `#293954` outlet ring (proposed: the dot count separates alpha (1) from beta (2) by shape).
- **Catalogue and inventory.** Id `alpha_source_cartridge`, title "Alpha cartridge", category `Radiation`, a distinct inventory item from beta-minus (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** None in the row.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); air-equivalent path accumulation: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); charged range-loss model, not relabelled photon attenuation |
| RadioactiveDecay | missing | unscheduled (S607-D/F); steady case |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; source state joins it with this element (unscheduled) |

**Dependencies.** RAD-08 in Alpha mode; EL-126 for the screen control.

## 4. Sources and legacy

- [radiation-03](../requirements.md#radiation-03): "A close compatible detector responds; extra separation or a thin screen suppresses it. Gamma-only reception rejects it."
- [Named entry](../invest/named-elements.md#radiation-03), owner S620; map row (S605); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) alpha row ("thin shield passes, thick shield stops" — conflicts with this row; open question 1); research row ("reaches only a close meter; a thin screen stops it"; slots 90 "The Short Journey", 93).
- **Legacy.** None in parts, engine, tests, levels, Campaign or reference. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1690-L1720`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Place the cartridge with its outlet facing a Battery-supplied Rate meter (Alpha mode) 0.5 m away; meter contact → Powered gate.
- **Positive.** Reading ≈ 7.1; meter on; gate releases.
- **Negative / controls.** Meter moved so the path exceeds 2.0 m (2.2 m): reading 0. Thin screen on the path at any spacing: air-equivalent ≥ 2.2 m, reading 0. Photon-mode meter at 0.5 m: 0 (gamma-only reception rejects alpha). Meter behind the cartridge (outside the cone): 0.
- **Boundaries.** Exactly 2.0 m path (admitted); cone edge at 10°; a 0.1 m gap between two screens transmits; no optical receiver ever responds.
- **Run/Reset.** Pose and initial state restore.
- **Save/Load.** Pose round-trips.
- **Integrations.** Source partner in the cross-element task [radiation-05 integration (sequence-task-413)](../requirements.md#sequence-task-413) (equal geometry with different materials) and in the RAD-14 opposite-charge proof; interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15) and [IX-16 Radioactive decay](../requirements.md#interaction-16) (steady case); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 111–120 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 12 of the [campaign plan](../requirements.md#campaign-plan)), reuse 121–125 and 136–150.

## 6. Open questions

1. **Conflict.** The S605 alpha row says "thin shield passes, thick shield stops", but this row and the research say a thin screen suppresses alpha. This spec follows the requirement row (thin screen stops alpha); S605 needs the owner to reconcile — owner decision.
2. Range model shape (step at the range versus a tapered tail) and the proposed 2.0 m range, 10° cone and k_Alpha — S606-D and LAW-FIELD-D.
3. With a 2.0 m range, the default inverse-square response region (about 2 m) and the range limit coincide; whether alpha should instead keep a shorter range and stay out of the deflector proof — owner decision.
