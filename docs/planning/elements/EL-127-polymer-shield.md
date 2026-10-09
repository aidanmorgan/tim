# EL-127 · Polymer shield — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [element-127](../requirements.md#element-127). This is a fictional toy material: the simplified shield chart is never real-world shielding guidance ([radiation research](../../radiation-component-research.md#laws-at-puzzle-scale)).

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-127 · Polymer shield |
| Type | Radiation |
| Anchor | [requirements.md#element-127](../requirements.md#element-127); [named-elements.md#element-127](../invest/named-elements.md#element-127); scope source [radiation-05](../requirements.md#radiation-05) (umbrella, index only) |
| Related identities | Siblings [EL-126 Thin screen](EL-126-thin-screen-shield.md), [EL-128 Dense shield](EL-128-dense-shield.md). Proving partners [RAD-04 Beta-minus](RAD-04-beta-minus-source-cartridge.md), [RAD-01 Gamma](RAD-01-gamma-source-capsule.md), [RAD-08 Rate meter](RAD-08-radiation-rate-meter.md). No CAT spec. |
| Proof owner | S611 |
| Roadmap story | unscheduled; campaign chapters 11–12 (levels 101–120, beta suppression at 116) |
| Status | not started |

## 2. Declaration

The requirement says only "distinct material coefficients and density govern radiation transport" and "it cannot inherit dense-shield performance from its category". All numbers are proposals.

- **Bodies and shapes.** One dynamic body: slab 0.80 × 0.80 × *t* m plus a foot 0.80 × 0.06 × 0.30 m, both `ColliderDeclaration` boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`). *t* is set by the variant below (proposed: same 0.80 m face as every shield so equal-geometry comparisons isolate the material, as the radiation-05 integration task requires).
- **Mass and material.** Contact restitution 0.25, friction 0.45, rolling resistance 0 (proposed: a smooth plastic slab, slightly slicker than the thin screen). Mass per variant below.
- **Radiation material** (`RadiationMaterialKind.Polymer`):
  - photon μ = 8 / 3 / 1.5 per metre for Soft / Medium / Hard (proposed: clearly weaker than Dense at every band, so a polymer slab never matches a dense slab of equal thickness);
  - charged air-equivalent stopping factor 300 (proposed: 0.05 m counts as 15 m of air, far beyond the 3.0 m beta range, so polymer suppresses beta as RAD-04 requires);
  - neutron fractions per 0.05 m: Fast 0.90 transmit / 0.05 moderate / 0.05 absorb, Slow 0.90 transmit / 0.10 absorb (proposed: a weak hydrogenous moderator, well below water, kept explicit rather than zero); thicker paths compound transmission as T(L) = T_ref^(L/0.05 m).
  - Own coefficients only: the material is looked up by `RadiationMaterialKind`, never by the Radiation palette category, so no dense value can leak in.
- **Constraints.** none.
- **Typed ports.** none — no wire to a shield.
- **Sensors and activation.** none.
- **Work and energy stores.** none (deposited energy is ledger-debited; see open question 3).
- **Parameters.** `ShieldThicknessPreset` enum, player-selected in the part inspector before Run, default Standard (proposed: the research names this typed preset; two values keep stacking lessons readable).
- **Cosmetic curves and UI bindings.** none animated. Selected-only info card "Polymer — stops beta; weak against photons".
- **Art.** Matte cyan `#66b8c9` slab with two engraved grooves, navy `#293954` foot, beveled edges (proposed: cyan reuses an approved palette value and two grooves give a shape cue).
- **Catalogue and inventory.** Id `polymer_shield`, title "Polymer shield", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)). `PartAllowance` counted (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variant: Standard (`ShieldThicknessPreset.Standard`)
- Thickness 0.05 m; mass 1.0 kg (proposed: lighter than the 4 kg Bowling ball so a polymer slab reads as light plastic).
- Medium-band transmission e^(−0.15) ≈ 0.86: a Gamma capsule at 1 m reads ≈ 13.6 behind it.

### Variant: Thick (`ShieldThicknessPreset.Thick`)
- Thickness 0.10 m; mass 2.0 kg (proposed: mass scales with volume at the same toy density).
- Medium-band transmission e^(−0.30) ≈ 0.74 (≈ 11.7 at 1 m); never higher than Standard.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/element-02.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); deposited-energy accounting; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); ordered material intervals along sampled paths: unscheduled (P0-004, S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); enum-indexed material table ([S605 rows](../invest/decisions.md#s605)) |
| SignalPropagation | missing | Story 8.1; no signal port on the shield — used by the meter in the proof |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; pose and preset restore with Reset |

As a dynamic panel it also uses RigidBodyDynamics, ContactImpulse and SlidingFriction (exist now: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L726`). The inspector enum selector for `ShieldThicknessPreset` is unscheduled with this element.

**Dependencies.** RAD-04 and RAD-08 for the beta control; RAD-01 for the photon comparison; EL-128 for the equal-geometry control.

## 4. Sources and legacy

- [element-127](../requirements.md#element-127) row and the [radiation-05](../requirements.md#radiation-05) integration task (equal geometry, stacked thickness, gaps, restore).
- [Named entry](../invest/named-elements.md#element-127), owner S611; element map row (S605, S257); binding `docs/coverage/engine/element-02.json` (proof S611).
- [S605](../invest/decisions.md#s605) photon, alpha and neutron rows; research "Material shield panel" row and RAD-04 lesson "polymer suppresses the selected beta profile" (slot 91 → 116).
- **Legacy.** Same search as the radiation family (parts, engine, tests, levels, Campaign, reference): no polymer part, coefficient, test or level exists. Hits are coverage snapshots only: the older radiation-05 binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1752-L1803`, identical to the current binding, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Place a Beta-minus cartridge and a Battery-supplied beta-sensitive Rate meter 0.5 m apart, wire the contact to a Powered gate (CAT-051); drag a Polymer shield onto the path; select each thickness preset in the inspector.
- **Positive.** Beta reading falls from ≈ 7.1 to zero with either preset; the gate stays shut.
- **Negative / controls.** A Thin screen in the same place passes beta (≈ 7.1, meter on). Equal-geometry control: a Standard Polymer (≈ 13.6) and a Standard Dense shield (≈ 5.8) at the same pose in front of a Gamma capsule 1 m from the meter give different readings, polymer higher.
- **Boundaries.** Thick never transmits more than Standard; two stacked Standard slabs equal one Thick within f32 rounding; slab edge grazing; a 0.1 m gap between two slabs transmits.
- **Run/Reset.** Pose (it can be knocked over) and selected preset restore exactly.
- **Save/Load.** Preset and pose round-trip; an undefined preset value is rejected at load.
- **Integrations.** Cross-element task [radiation-05 integration (sequence-task-413)](../requirements.md#sequence-task-413) (equal geometry with different materials, stacked thickness, gaps, restore); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15) and [IX-01 Contact impulse](../requirements.md#interaction-01) (the panel is a dynamic body); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 101–110 with the RAD-05 shields ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 11 of the [campaign plan](../requirements.md#campaign-plan)), beta suppression taught at 116, reuse 111–120, 124–130 and 136–150.

## 6. Open questions

1. Whether polymer moderates neutrons at all in the first abstraction (the research names only water as moderator) — owner decision.
2. Proposed coefficients need the S606-D decision.
3. Whether deposited energy heats the slab — owner decision.
4. Whether thickness presets exist or only stacking teaches thickness (shared with EL-126/EL-128) — owner decision.
