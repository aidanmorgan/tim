# RAD-16 · Neutron source module — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-16](../requirements.md#radiation-16) (P3 potential). A **distinct contained toy source**: no implied reactor, multiplication or gamma-shielding equivalence; game units only.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-16 · Neutron source module |
| Type | Radiation (neutron source, specialist transport domain) |
| Anchor | [requirements.md#radiation-16](../requirements.md#radiation-16); [named-elements.md#radiation-16](../invest/named-elements.md#radiation-16) |
| Related identities | Detectors [EL-131 Fast](EL-131-fast-neutron-detector.md), [EL-132 Slow](EL-132-slow-neutron-detector.md); moderator [RAD-17](RAD-17-water-moderator-tank.md); absorber [RAD-18](RAD-18-neutron-absorber-panel.md); photon-only control [RAD-08](RAD-08-radiation-rate-meter.md); field control [RAD-14](RAD-14-magnetic-deflector.md). No CAT spec. |
| Proof owner | S626 |
| Roadmap story | unscheduled; campaign 125, 130 (research slots 100, 105) |
| Status | not started |

## 2. Declaration

The requirement fixes: a distinct contained source of a declared fast-neutron group; a fast-sensitive neutron detector responds; a photon-only detector does not; magnetic deflection leaves the route unchanged. Values are proposals.

- **Bodies and shapes.** Static box 0.45 × 0.50 × 0.45 m with a 0.12 m radius emitting sphere at the centre (proposed: visibly bulkier than the gamma capsule, read as a sealed module; 0.24 m wide, so it is centre-sampled under the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration)).
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** none (no electrical switch).
- **Sensors and activation.** none; emits continuously during Run.
- **Source declaration.** `RadiationKind.Neutron`, `NeutronEnergyGroup.Fast`, isotropic, A = 16 game units/s (proposed: same strength as the other sources, so a fast detector at 1 m reads ≈ 15.8). Material interaction uses each material's two-group fractions (transmit, moderate Fast → Slow, absorb; outgoing sum ≤ incoming); no multiplication, activation or secondary photons (research "Neutrons" law). Charge zero: magnetic fields do not bend it.
- **Work and energy stores.** Emitted-quanta ledger; steady within a Run (RadioactiveDecay steady case).
- **Parameters.** none.
- **Cosmetic curves and UI bindings.** Selected-only field overlay (research row "Overlay"); no glow.
- **Art.** Cream `#fff8e9` module with navy `#293954` banding, gold `#f7cb52` cap engraved "n" with a fast-group chevron (proposed: distinct mark from γ/α/β; no radioactive green).
- **Catalogue and inventory.** Id `neutron_source_module`, title "Neutron source", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** None in the row (Fast group only).

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); emitted-quanta ledger; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); sampled path queries: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); two-group neutron channel ([S605 neutron row](../invest/decisions.md#s605): "with moderator the slow detector reads, without it the fast detector reads; absent source reads nothing") |
| RadioactiveDecay | missing | unscheduled (S607-D/F); steady case |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; pose and initial state restore with Reset |

**Dependencies.** EL-131 (positive), RAD-08 photon mode (control), RAD-14 (field control); RAD-17/RAD-18 for later lessons.

## 4. Sources and legacy

- [radiation-16](../requirements.md#radiation-16): "A fast-sensitive neutron detector responds; a photon-only detector does not. Ordinary magnetic deflection leaves the neutron route unchanged."
- [Named entry](../invest/named-elements.md#radiation-16), owner S626; map row (S605); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) neutron row; research "Neutrons" law and slots 100 "The Neutral Visitor", 105 "Two Kinds of Shelter".
- **Legacy.** None. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2134-L2164`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Neutron source 1 m from a supplied Fast-neutron detector (EL-131) wired to a Powered gate.
- **Positive.** Detector reads ≈ 15.8, contact closes, gate releases.
- **Negative / controls.** Photon-mode Rate meter at the same place: 0. A Gamma capsule replacing the source: the fast detector reads 0. Powered RAD-14 between source and detector, either polarity: reading unchanged. Source removed: nothing reads.
- **Boundaries.** On boundary at about 2.0 m; a Thick dense slab lowers the fast reading only to ≈ 13.4 (0.92² × 15.8), so it does not substitute for moderator or absorber.
- **Run/Reset.** Pose and initial state restore.
- **Save/Load.** Pose round-trips.
- **Integrations.** Source in the cross-element task [radiation-19 integration (sequence-task-427)](../requirements.md#sequence-task-427) (moderated flow drives Slow; photon source and missing supply are controls); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15) and [IX-16 Radioactive decay](../requirements.md#interaction-16) (steady case); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 121–130 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 13 of the [campaign plan](../requirements.md#campaign-plan)), reuse 136–150.

## 6. Open questions

1. Proposed activity and group fractions need the S606-D neutron decision.
2. Whether the neutron module also emits a weak photon component (real sources often do) — owner decision; this spec proposes none, as the requirement treats it as a distinct domain.
