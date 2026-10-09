# EL-128 · Dense shield — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [element-128](../requirements.md#element-128). This is a fictional toy material: the simplified shield chart is never real-world shielding guidance ([radiation research](../../radiation-component-research.md#laws-at-puzzle-scale)).

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-128 · Dense shield |
| Type | Radiation |
| Anchor | [requirements.md#element-128](../requirements.md#element-128); [named-elements.md#element-128](../invest/named-elements.md#element-128); scope source [radiation-05](../requirements.md#radiation-05) (umbrella, index only) |
| Related identities | Siblings [EL-126](EL-126-thin-screen-shield.md), [EL-127](EL-127-polymer-shield.md). Proving partners [RAD-01 Gamma](RAD-01-gamma-source-capsule.md), [RAD-02 X-ray](RAD-02-powered-x-ray-emitter.md), [RAD-08 Rate meter](RAD-08-radiation-rate-meter.md). The dense material is reused by [RAD-06 shutter blade](RAD-06-powered-radiation-shutter.md), [RAD-07 collimator](RAD-07-collimator-block.md) and the [RAD-14](RAD-14-magnetic-deflector.md) poles and obstacle. No CAT spec. |
| Proof owner | S612 |
| Roadmap story | unscheduled; campaign chapter 11 (levels 101–110; "Behind the Slab" and "Two Thin Walls" at 102, 104) |
| Status | not started |

## 2. Declaration

The requirement fixes "dense material attenuates supported radiation through actual path length" and "increasing thickness cannot increase passive transmitted energy". Numbers are proposals.

- **Bodies and shapes.** Dynamic body: slab 0.80 × 0.80 × *t* m plus foot 0.80 × 0.06 × 0.30 m, `ColliderDeclaration` boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`) (proposed: shared shield face; *t* per variant).
- **Mass and material.** Restitution 0.1, friction 0.6, rolling resistance 0 (proposed: a heavy, dead slab that barely bounces cargo). Mass per variant.
- **Radiation material** (`RadiationMaterialKind.Dense`):
  - photon μ = 64 / 20 / 10 per metre for Soft / Medium / Hard (proposed: one Standard slab is "too little" and two are enough for a Gamma capsule at 1 m, which is exactly the lesson-104 stacking puzzle; all inside 0.25–128 per metre);
  - charged air-equivalent stopping factor 2000 (proposed: stops alpha and beta at any thickness);
  - neutron fractions per 0.05 m: Fast 0.92 transmit / 0 moderate / 0.08 absorb, Slow 0.87 transmit / 0.13 absorb (proposed: deliberately poor at neutrons so it cannot substitute for the RAD-18 absorber); compounding T(L) = T_ref^(L/0.05 m).
  - Transmission uses the actual traversed path through the slab for every sampled path of the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration) (a 45° slab path is √2 × *t*), never a nominal thickness.
- **Constraints.** none.
- **Typed ports.** none.
- **Sensors and activation.** none.
- **Work and energy stores.** none (ledger-debited deposition; open question 2).
- **Parameters.** `ShieldThicknessPreset` (Standard, Thick), inspector-selected before Run, default Standard (proposed: same presets as EL-127 for equal-geometry comparisons).
- **Cosmetic curves and UI bindings.** none animated. Selected-only card "Dense — strongest photon shield; poor against neutrons".
- **Art.** Thick beveled navy `#45639c` slab with three engraved grooves, cream `#fff8e9` edge band, navy `#293954` foot (proposed: the Bowling-ball/Weight navy reads as heavy; three grooves give the shape cue; the research asks for "thick beveled shield slabs").
- **Catalogue and inventory.** Id `dense_shield`, title "Dense shield", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)). `PartAllowance` counted (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variant: Standard (`ShieldThicknessPreset.Standard`)
- Thickness 0.05 m; mass 4.0 kg (proposed: equal to the Bowling ball, the heaviest current part).
- Medium transmission e^(−1.0) ≈ 0.37: a Gamma capsule at 1 m still reads ≈ 5.8 (above the default On 4).

### Variant: Thick (`ShieldThicknessPreset.Thick`)
- Thickness 0.10 m; mass 8.0 kg (proposed: twice the volume; inside the 1/1024–1024 kg admission bound, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`).
- Medium transmission e^(−2.0) ≈ 0.135: the same capsule reads ≈ 2.1 (below the default Off 3).

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/element-02.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); deposited-energy accounting; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); oriented-box path length along sampled paths: unscheduled (P0-004, S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); exponential photon attenuation over actual path intervals ([S605 photon row](../invest/decisions.md#s605): "thicker shield reads less; missing source or gap reads as declared") |
| SignalPropagation | missing | Story 8.1; no signal port on the shield — used by the meter in the proof |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; pose and preset restore with Reset |

As a dynamic panel it also uses RigidBodyDynamics, ContactImpulse and SlidingFriction (exist now: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L726`). The thickness enum selector is unscheduled with this element.

**Dependencies.** RAD-01 (or RAD-02) and RAD-08 to observe; EL-127 for the equal-geometry control.

## 4. Sources and legacy

- [element-128](../requirements.md#element-128) and the [radiation-05](../requirements.md#radiation-05) integration task.
- [Named entry](../invest/named-elements.md#element-128), owner S612; element map row; binding `docs/coverage/engine/element-02.json`, proof S612.
- [S605](../invest/decisions.md#s605) photon row; research lessons 77 "Behind the Slab", 79 "Two Thin Walls" (→ 102, 104), 105 "Two Kinds of Shelter" (dense-only fails the neutron condition).
- **Legacy.** No dense-shield part, coefficient, test or level in `parts/`, `engine/`, tests, `content/puzzles.json`, `tools/Campaign` or `reference/`. Hits are coverage snapshots only: the older radiation-05 binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1752-L1803`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Gamma capsule 1 m from a Battery-supplied photon Rate meter wired to a Powered gate; drag a Dense shield onto the path; choose Standard, then Thick, in the inspector; then stack two Standard slabs.
- **Positive.** Thick (or two Standard) drops the reading from ≈ 15.8 to ≈ 2.1, below Off, and the gate closes / stays shut; Standard alone (≈ 5.8) does not.
- **Negative / controls.** The same slab beside the path leaves the meter at ≈ 15.8. Neutron control: a Thick dense slab after a full water moderator leaves the slow detector at ≈ 6.3 (RAD-17 tank walls included), still on (RAD-18 owns absorption).
- **Boundaries.** Monotonic: Thick ≤ Standard ≤ open path at every band; 45° rotation lowers transmission; edge-grazing path (some receiver samples clear the slab, giving an intermediate reading); insertion order of stacked slabs does not change the result.
- **Run/Reset.** Pose and preset restore exactly.
- **Save/Load.** Preset and pose round-trip; undefined preset rejected.
- **Integrations.** Cross-element tasks [radiation-05 integration (sequence-task-413)](../requirements.md#sequence-task-413) (equal geometry with different materials, stacked thickness, gaps, restore) and, as the non-substituting control, [radiation-19 integration (sequence-task-427)](../requirements.md#sequence-task-427); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15) and [IX-01 Contact impulse](../requirements.md#interaction-01) (the panel is a dynamic body); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 101–110 with the RAD-05 shields ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 11 of the [campaign plan](../requirements.md#campaign-plan)), reuse 111–120, 124–130 and 136–150.

## 6. Open questions

1. Proposed μ, stopping factor and neutron fractions need the S606-D decision.
2. Whether deposited energy heats the slab — owner decision.
3. Shared with EL-126/EL-127: thickness presets versus stacking only — owner decision.
