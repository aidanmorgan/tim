# RAD-07 · Collimator block — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-07](../requirements.md#radiation-07) (P1 potential). Toy shielding geometry in game units; never real-world guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-07 · Collimator block |
| Type | Radiation (passive absorbing geometry) |
| Anchor | [requirements.md#radiation-07](../requirements.md#radiation-07); [named-elements.md#radiation-07](../invest/named-elements.md#radiation-07) |
| Related identities | Material from [EL-128 Dense shield](EL-128-dense-shield.md); sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); receivers [RAD-08](RAD-08-radiation-rate-meter.md). Not a lens: optical lenses ([TH-06](../invest/named-elements.md#thermal-06)) are a different family. No CAT spec. |
| Proof owner | S614 |
| Roadmap story | unscheduled; campaign 106, 114 (research slots 81, 89) |
| Status | not started |

## 2. Declaration

The requirement fixes: a dense block with a real aperture absorbs off-axis radiation; presets trade coverage for throughput; never a lens or amplifier; a narrower opening never increases total transmitted power. Values are proposals.

- **Bodies and shapes.** One static body 0.40 m long (local X) × 0.60 × 0.60 m, built from four box colliders surrounding a straight square channel along local +X (proposed: the aperture is real collider geometry, so the same intervals block cargo and radiation; 0.40 m keeps the geometry readable on the bench). Channel width per variant.
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`); `RadiationMaterialKind.Dense` (EL-128 coefficients, Medium μ 20 per metre). A ray that clips a wall for L metres keeps T = e^(−20·L): 0.61 at 0.025 m, 0.07 at 0.13 m, below 0.01 beyond 0.23 m.
- **Constraints.** none.
- **Typed ports.** none.
- **Sensors and activation.** none.
- **Work and energy stores.** none; absorbed contributions are debited, never re-emitted or concentrated.
- **Parameters.** `CollimatorAperture` enum (Narrow, Medium, Wide), inspector-selected, default Medium (proposed: research "aperture presets" as a typed closed set). Direction is the block orientation (rotate gizmo).
- **Cosmetic curves and UI bindings.** none animated (research row "None"); selected-only dotted acceptance cone.
- **Art.** Navy `#45639c` dense block with three grooves, cream `#fff8e9` aperture bezel whose engraved notch count shows the preset (1/2/3), navy `#293954` base (proposed: shape-coded preset).
- **Catalogue and inventory.** Id `collimator_block`, title "Collimator", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

### Variant: Narrow (`CollimatorAperture.Narrow`)
- Channel 0.12 × 0.12 m (proposed: just wide enough to pass all 9 receiver samples of an on-axis meter 0.50 m beyond the block, and effectively none of a neighbour 0.30 m to the side).

### Variant: Medium (`CollimatorAperture.Medium`)
- Channel 0.16 × 0.16 m (proposed: a step up in open area that still keeps the 0.30 m neighbour clearly off).

### Variant: Wide (`CollimatorAperture.Wide`)
- Channel 0.40 × 0.40 m (proposed: covers the 0.30 m neighbour as well).

Open area 0.0144 / 0.0256 / 0.16 m², so for any source behind the block the total transmitted power is Narrow ≤ Medium ≤ Wide.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | dynamic AABB BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and sphere/box/plane narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); sampled path intervals through a compound body without double counting: unscheduled (P0-004, S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); finite aperture coverage, absorbing rejected directions |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161` (static part; no Run state of its own) |

Also needed: the inspector enum selector (unscheduled with this element).

**Dependencies.** A source (RAD-01/RAD-02) and two receivers (RAD-08) for the aligned/off-axis control.

## 4. Sources and legacy

- [radiation-07](../requirements.md#radiation-07): "An aligned receiver responds while an off-axis control does not; a narrower opening never increases total transmitted power."
- [Named entry](../invest/named-elements.md#radiation-07), owner S614; map row; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research row ("addresses one receiver, excludes its neighbour"; slots 81 "The Narrow Door", 89).
- **Legacy.** None. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1839-L1868`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

Layout (distances from the capsule's emitter centre along the axis): Gamma capsule touching the back face (back face at 0.15 m, front face at 0.55 m); two photon Rate meters with faces at 1.05 m (0.50 m beyond the block), one on axis and one centred 0.30 m to the side, each wired to its own Powered gate. Readings use the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration) (point emitter, 3 × 3 receiver samples at ±0.10 m): a sample at lateral offset o passes the front face at 0.524·o.

| Preset | On-axis meter | Neighbour at 0.30 m (sample rows at 0.20 / 0.30 / 0.40 m reach 0.105 / 0.157 / 0.210 m at the front face) |
| --- | --- | --- |
| Narrow (half-width 0.06) | 9/9 open (0.052 ≤ 0.06); ≈ 14.3, on | 0/9 open; the 0.20 m row clips 0.235 m of wall (T ≈ 0.009); ≈ 0.04, off |
| Medium (half-width 0.08) | 9/9 open; ≈ 14.3, on | 0/9 open; the 0.20 m row clips 0.13 m of wall (T ≈ 0.07); ≈ 0.3, off |
| Wide (half-width 0.20) | 9/9 open; ≈ 14.3, on | 6/9 open; the 0.40 m row clips 0.025 m of wall (T ≈ 0.61); ≈ 11.5, on |

- **Construction (actual Chrome UI).** Place the capsule, the collimator and both meters as above through palette and gizmo; select each preset in the inspector.
- **Positive.** Narrow: only the aligned meter switches on.
- **Negative / controls.** Wide: both meters respond (coverage). Block turned 90° so a solid side faces the capsule: neither meter responds. A meter in a rejected direction never responds.
- **Boundaries.** Narrow sample at 0.052 m against the 0.06 m edge; total transmitted power Narrow ≤ Medium ≤ Wide; undefined preset rejected.
- **Run/Reset.** Preset and pose unchanged; meters reset.
- **Save/Load.** Preset and pose round-trip.
- **Integrations.** No row-level cross-element task names the collimator; it integrates through the interaction process [IX-15 Ionizing transport](../requirements.md#interaction-15) (finite aperture coverage); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 101–110 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 11 of the [campaign plan](../requirements.md#campaign-plan)), reuse 111–120, 124–130 and 136–150, including the inspection-post capstone with the gauges.

## 6. Open questions

1. Proposed widths, block length and the sampled rate law (owner question S606-D) — owner decision.
2. Square versus slit apertures (2-D versus 1-D selection) — owner decision.
