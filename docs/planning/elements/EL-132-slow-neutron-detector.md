# EL-132 · Slow-neutron detector — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [element-132](../requirements.md#element-132). Reads fictional toy sources in game units; never real-world detection guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-132 · Slow-neutron detector |
| Type | Radiation |
| Anchor | [requirements.md#element-132](../requirements.md#element-132); [named-elements.md#element-132](../invest/named-elements.md#element-132); scope source [radiation-19](../requirements.md#radiation-19) (umbrella, index only) |
| Related identities | Sibling [EL-131 Fast-neutron detector](EL-131-fast-neutron-detector.md); source [RAD-16](RAD-16-neutron-source-module.md); moderator [RAD-17](RAD-17-water-moderator-tank.md); absorber [RAD-18](RAD-18-neutron-absorber-panel.md); shared defaults [RAD-08](RAD-08-radiation-rate-meter.md). No CAT spec. |
| Proof owner | S630 |
| Roadmap story | unscheduled; campaign chapter 13 (levels 121–130; research slots 101, 102, 104) |
| Status | not started |

## 2. Declaration

The requirement fixes "supplied detector applies declared slow-group response" and "unmoderated fast flux does not silently count as slow". Values are proposals unless cited.

- **Bodies and shapes.** Static box 0.50 × 0.60 × 0.30 m with a 0.30 × 0.30 m sensing face on local +X (proposed: same envelope as RAD-08 and EL-131).
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** `PowerIn` (Electrical, Input) and `Supply` (Electrical, Output), existing sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`); switched route `PowerIn` → `Supply` closed while on.
- **Sensors and activation.** `RadiationInstrumentState`. Response weight w = 1 for `RadiationKind.Neutron` in `NeutronEnergyGroup.Slow`; w = 0 for Fast neutrons, photons and charged kinds (proposed: strict group selectivity; fast flux becomes slow only through a declared moderation fraction in a traversed material, never inside the detector). [Shared rate law](RAD-08-radiation-rate-meter.md#2-declaration), On 4.0 / Off 3.0, 0.05 s dwell, saturation 256 (proposed: RAD-08 defaults).
- **Work and energy stores.** none.
- **Parameters.** On/Off thresholds as RAD-08. Group fixed by identity.
- **Cosmetic curves and UI bindings.** Needle ← committed slow-group rate; contact lamp; optional muted-equivalent clicks.
- **Art.** Cream `#fff8e9` case, navy `#293954` face, gold `#f7cb52` needle, engraved "n" with one horizontal bar (proposed: bar means "slow"; EL-131 uses two chevrons).
- **Catalogue and inventory.** Id `slow_neutron_detector`, title "Slow-neutron detector", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** The row names none.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/element-02.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); sampled path queries: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); Fast → Slow moderation and slow-group receivers ([S605 neutron row](../invest/decisions.md#s605)) |
| SignalPropagation | missing | Story 8.1 (supplied contacts); the current `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14` is activation-only |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; detector state resets with Reset |

Also used: ElectricalPower (missing; Story 8.1).

**Dependencies.** RAD-16 source and RAD-17 moderator for any positive; RAD-18 for the absorption control; CAT-005 Battery.

## 4. Sources and legacy

- [element-132](../requirements.md#element-132) and the [radiation-19](../requirements.md#radiation-19) integration task (moderated flow drives Slow; photon source and missing supply are controls).
- [Named entry](../invest/named-elements.md#element-132), owner S630; map row; binding `docs/coverage/engine/element-02.json` (proof S630, relation radiation-19).
- [S605](../invest/decisions.md#s605) neutron row; research slots 101 "Slow Water", 102 "The Final Layer", 104 "One Speed at a Time".
- **Legacy.** None. Hits are coverage snapshots only, e.g. `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L14898-L14901`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance outline

Readings include the two RAD-17 thin-screen walls (neutron transmission 0.98 each).

- **Construction (actual Chrome UI).** Neutron source 1 m from the supplied Slow detector; a RAD-17 tank between them, filled through its supplied tap/pump; contact → Powered gate.
- **Positive.** Full tank: slow reading ≈ 8.3, gate releases.
- **Negative / controls.** Dry or drained tank: reading 0 (unmoderated fast flux never counts). RAD-18 panel after the tank: reading ≈ 0.42, gate shut. Gamma capsule: 0. Unsupplied: no contact.
- **Boundaries.** Waterline below the beam gives 0; a waterline rising through the receiver-sample rows raises the reading monotonically; On/Off crossings.
- **Run/Reset.** State Unpowered, contact open.
- **Save/Load.** Thresholds, pose and wires round-trip.
- **Integrations.** Cross-element task [radiation-19 integration (sequence-task-427)](../requirements.md#sequence-task-427) (moderated flow drives Slow while the Fast reading decreases; photon source and missing supply are controls); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-06 Electrical power transfer](../requirements.md#interaction-06) and [IX-07 Signal propagation](../requirements.md#interaction-07); supplied-port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 121–130 with RAD-19 Fast/Slow detection ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 13 of the [campaign plan](../requirements.md#campaign-plan)), reuse 136–150.

## 6. Open questions

1. One part with a group mode versus two parts (shared with EL-131) — owner decision.
2. Proposed thresholds need S606-D.
