# EL-126 · Thin-screen shield — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [element-126](../requirements.md#element-126). This is a fictional toy material: the simplified shield chart is never real-world shielding guidance ([radiation research](../../radiation-component-research.md#laws-at-puzzle-scale)).

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-126 · Thin-screen shield |
| Type | Radiation |
| Anchor | [requirements.md#element-126](../requirements.md#element-126); [named-elements.md#element-126](../invest/named-elements.md#element-126); scope source [radiation-05](../requirements.md#radiation-05) (umbrella, index only) |
| Related identities | Siblings [EL-127 Polymer shield](EL-127-polymer-shield.md), [EL-128 Dense shield](EL-128-dense-shield.md). Proving partners [RAD-03 Alpha](RAD-03-alpha-source-cartridge.md), [RAD-04 Beta-minus](RAD-04-beta-minus-source-cartridge.md), [RAD-01 Gamma](RAD-01-gamma-source-capsule.md), [RAD-08 Rate meter](RAD-08-radiation-rate-meter.md). No CAT spec: no catalogue element is refined or extended. |
| Proof owner | S610 |
| Roadmap story | unscheduled (no radiation epic in the [roadmap](../invest/vertical-delivery.md#rolling-playable-roadmap)); campaign chapter 11, levels 101–110 ([campaign plan](../requirements.md#campaign-plan)) |
| Status | not started |

## 2. Declaration

The requirement fixes the outcome ("gaps transmit; supported alpha/beta/photon responses follow shared transport coefficients") but no dimension or coefficient. Every number below is a design proposal.

- **Bodies and shapes.** One dynamic body with two box colliders: slab 0.80 × 0.80 × 0.02 m (proposed: same face as the other shields so material, not size, is the variable; 0.02 m keeps it above the 1 cm tunnelling floor of the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope)) and a foot 0.80 × 0.06 × 0.30 m centred under it (proposed: a wide foot lets a thin panel stand on the bench without a joint). Shapes use `ColliderDeclaration` boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`).
- **Mass and material.** 0.4 kg (proposed: a card-like toy panel lighter than the 1 kg Basketball, so a rolling ball can knock it aside; toy density, not real paper density). Contact material restitution 0.2, friction 0.5, rolling resistance 0 (proposed: a matte panel that does not bounce cargo; within `ContactMaterialDeclaration` bounds, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Radiation material** (`RadiationMaterialKind.ThinScreen`, typed name from [research typed boundaries](../../radiation-component-research.md#typed-boundaries)):
  - photon attenuation μ = 2 / 0.5 / 0.25 per metre for the Soft / Medium / Hard `PhotonEnergyBand` (proposed: nearly transparent to photons, about 1 % loss at Medium through 0.02 m; inside the research range 2⁻⁶–8 per cell, read as 0.25–128 per metre with a 1/16 m cell);
  - charged-particle air-equivalent stopping factor 110 (proposed: 0.02 m counts as 2.2 m of air — beyond the 2.0 m alpha range at any spacing, and inside the 3.0 m beta range while the remaining air path is at most 0.8 m, giving "stops alpha, passes beta");
  - neutron per-traversal fractions Fast 0.98 transmit / 0 moderate / 0.02 absorb, Slow 0.98 transmit / 0.02 absorb (proposed: a thin screen is not a neutron material).
  - The attenuating interval is exactly the collider volumes along each sampled path ([shared rate law](RAD-08-radiation-rate-meter.md#2-declaration)); empty space between two screens contributes nothing, so gaps transmit. Owner envelope and colliders never double-count.
- **Constraints.** none — a free dynamic body resting on contact.
- **Typed ports.** none — radiation crosses space, not cables; a wire to a shield is refused ([research network nodes](../../radiation-component-research.md#parameters-sensors-sources-stores-and-network-nodes)).
- **Sensors and activation.** none.
- **Work and energy stores.** none. Energy removed from a path is debited by the IonizingTransport ledger, not stored in the panel (thermal coupling is open question 3).
- **Parameters.** none player-editable; material and thickness are fixed by identity (the requirement names no thickness preset; thickness is taught by stacking, see open question 5).
- **Cosmetic curves and UI bindings.** No animation (static part in the [research table](../../radiation-component-research.md#per-element-declarations)). Selected-only info card: "Thin screen — stops alpha, passes most beta and photons" (proposed: the research's short contextual card pattern).
- **Art.** Beveled cream `#fff8e9` slab with one engraved horizontal groove (proposed: groove count 1/2/3 distinguishes thin/polymer/dense by shape, never colour alone), navy `#293954` foot, gold `#f7cb52` particle-symbol inlay; no radioactive green ([DESIGN.md colour system](../../../DESIGN.md#colour-system), [research presentation](../../radiation-component-research.md#presentation)).
- **Catalogue and inventory.** Id `thin_screen_shield`, title "Thin screen", category `Radiation` (proposed: snake_case catalogue id convention; Radiation group from the [grouped-menu requirement](../requirements.md#palette-type-groups)). Counted inventory via `PartAllowance` (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirement row names none. One fixed thickness; each supported radiation kind (Alpha, BetaMinus, Gamma/X-ray band, Fast/Slow neutron) is a separately proven response.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/element-02.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); deposited-energy accounting; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | dynamic AABB BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and sphere/box/plane narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); ordered material intervals along sampled paths: unscheduled (P0-004, S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); typed kinds, material table, charged range, neutron groups ([S605 rows](../invest/decisions.md#s605)) |
| SignalPropagation | missing | Story 8.1; listed by the map but the shield has no signal port — it belongs to the meter in the proof construction |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; the panel pose restores with Reset |

As a dynamic panel it also uses RigidBodyDynamics (exists now: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`, integration `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L216-L285`), ContactImpulse (exists now: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L700`) and SlidingFriction (exists now: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L726`). No radiation `WorkshopPartKind` exists (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Dependencies.** Needs at least one source (RAD-01, RAD-03 or RAD-04) and a receiver (RAD-08) to be observable.

## 4. Sources and legacy

- Requirement row [element-126](../requirements.md#element-126) and integration task under [radiation-05](../requirements.md#radiation-05): "equal geometry with different materials produces the declared readings; stacked thickness attenuates monotonically and gaps transmit. Repositioned panels restore."
- Named entry [element-126](../invest/named-elements.md#element-126), owner S610.
- Element map row: FiniteLedger, GeometryQuery, IonizingTransport, SignalPropagation; S605 and S257 decisions.
- Coverage binding: `docs/coverage/engine/element-02.json`, `FutureProduct`, proof owner S610, relation to radiation-05.
- Decision [S605](../invest/decisions.md#s605) photon and alpha rows (S606). The alpha row says "thin shield passes, thick shield stops", which conflicts with RAD-03's "a thin screen suppresses it" (open question 6).
- Research: "Material shield panel" row (lessons 77, 79, 90, 91 → 102, 104, 115, 116) and the transport laws.
- **Legacy.** Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` for radiation, shield, alpha, beta, gamma, neutron, attenuation and related terms. No part, law, test, level or lesson exists. Hits are coverage snapshots only: an older copy of the radiation-05 binding that lists this identity, identical to the current binding, `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1752-L1803`, and aggregate relation lists — no element knowledge; do not carry forward (superseded by `element-02.json`).

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

Authority: [element-126](../requirements.md#element-126) and the [radiation proof contract](../requirements.md#radiation-proof).

- **Construction (actual Chrome UI).** From the Radiation drawer place an Alpha cartridge, a Battery-supplied alpha-sensitive Rate meter 0.5 m away with its contact wired to a Powered gate (CAT-051) holding a ball, then drag a Thin screen between them with the move gizmo. No setters or numeric placement.
- **Positive.** Without the screen the meter reads ≈ 7.1 and the gate releases the ball; with it the alpha reading is zero (2.2 m air-equivalent ≥ the 2.0 m range) and the gate stays shut. With a Beta-minus cartridge in the same layout the meter stays on through the screen (2.2 + 0.48 = 2.68 m ≤ 3.0 m; reading ≈ 7.1).
- **Negative / controls.** Screen beside the path leaves the alpha reading unchanged. Two screens with a 0.1 m gap aligned with the beam axis transmit (meter on). Photon control: a Gamma capsule's reading at 1 m falls by about 1 % (15.79 → about 15.6), not to zero.
- **Boundaries.** Path grazing the slab edge; screen rotated 45° (longer path, still monotonic); two stacked screens never transmit more than one; beta with a 0.8 m air path through the screen (admitted) versus 0.9 m (stopped).
- **Run/Reset.** A screen knocked over during Run returns to its authored pose and the meter to its initial state.
- **Save/Load.** Pose and identity round-trip in the current schema.
- **Integrations.** Cross-element task [radiation-05 integration (sequence-task-413)](../requirements.md#sequence-task-413) (equal geometry with different materials, stacked thickness, gaps, restore); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15) and [IX-01 Contact impulse](../requirements.md#interaction-01) (the panel is a dynamic body); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 101–110 with the RAD-05 shields ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 11 of the [campaign plan](../requirements.md#campaign-plan)), reuse 111–120, 124–130 and 136–150.

## 6. Open questions

1. Static or dynamic: the row says "mass form a thin physical panel"; current static bodies must declare zero mass (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`). Dynamic is proposed — owner decision.
2. The proposed μ, stopping factor and neutron fractions need the S606-D model decision.
3. Whether deposited energy heats the panel (SensibleHeat coupling) — owner decision.
4. The research gives μ per "cell"; this spec reads one cell as the 1/16 m canonical body cell (`engine/gpu/CanonicalBody.cs@a6c914e:L8-L8`) — confirm.
5. Thickness: one fixed thickness plus stacking, or `ShieldThicknessPreset` options — owner decision.
6. **Conflict.** S605's alpha row ("thin shield passes, thick shield stops") contradicts the RAD-03 requirement ("a thin screen suppresses it"). This spec follows the requirement rows — owner decision to reconcile S605.
