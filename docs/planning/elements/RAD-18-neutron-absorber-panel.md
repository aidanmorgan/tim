# RAD-18 · Neutron absorber panel — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-18](../requirements.md#radiation-18) (P3 potential). A **boron-inspired toy material**; capture products are deliberately omitted; never real-world shielding guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-18 · Neutron absorber panel |
| Type | Radiation (passive neutron absorber) |
| Anchor | [requirements.md#radiation-18](../requirements.md#radiation-18); [named-elements.md#radiation-18](../invest/named-elements.md#radiation-18) |
| Related identities | Moderator [RAD-17](RAD-17-water-moderator-tank.md); source [RAD-16](RAD-16-neutron-source-module.md); detectors [EL-132 Slow](EL-132-slow-neutron-detector.md), [EL-131 Fast](EL-131-fast-neutron-detector.md); photon shield that must not substitute [EL-128](EL-128-dense-shield.md); same panel form as [EL-126](EL-126-thin-screen-shield.md)/[EL-127](EL-127-polymer-shield.md). No CAT spec. |
| Proof owner | S628 |
| Roadmap story | unscheduled; campaign 127, 130 (research slots 102, 105) |
| Status | not started |

## 2. Declaration

The requirement fixes: a distinct boron-inspired material removes a declared fraction of slow-neutron flux; absorption is taught separately from moderation; omitted capture products are an explicit model limit; adding it after the moderator suppresses the slow detector; dense photon shielding does not substitute. Values are proposals.

- **Bodies and shapes.** Dynamic body: slab 0.80 × 0.80 × 0.05 m plus foot 0.80 × 0.06 × 0.30 m (proposed: the shared shield form so it is placed the same way as EL-126..EL-128).
- **Mass and material.** 1.5 kg; restitution 0.2, friction 0.5, rolling resistance 0 (proposed: between polymer and dense, a stiff composite board). `RadiationMaterialKind.NeutronAbsorber`: neutron fractions per 0.05 m Fast 0.90 transmit / 0 moderate / 0.10 absorb, Slow 0.05 transmit / 0.95 absorb (proposed: takes the ≈ 8.3 slow reading behind a full RAD-17 tank, walls included, to ≈ 0.42, below Off, while leaving fast flux mostly intact); photon μ 6 / 2 / 1 per metre and charged stopping factor 300 (proposed: an ordinary weak photon shield, so it is not a universal shield).
- **Model limit.** Absorbed neutrons produce no capture gamma or other secondaries; the info card states this explicitly (requirement wording).
- **Constraints.** none.
- **Typed ports.** none.
- **Sensors and activation.** none.
- **Work and energy stores.** none (absorbed counts are ledger-debited).
- **Parameters.** none (single fixed thickness; the requirement names no preset).
- **Cosmetic curves and UI bindings.** none animated (research row "None"); selected-only card "Absorbs slow neutrons; does not slow fast ones; capture products not modelled".
- **Art.** Cream `#e8d4a6` board with a navy `#293954` honeycomb engraving and a gold `#f7cb52` "n" with a crossed bar (proposed: honeycomb pattern distinguishes it from the grooved photon shields by shape).
- **Catalogue and inventory.** Id `neutron_absorber_panel`, title "Neutron absorber", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** None in the row.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); absorbed neutron counts; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); sampled path intervals: unscheduled (P0-004, S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); two-group absorption fractions ([S605 neutron row](../invest/decisions.md#s605)) |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; panel pose restores with Reset |

As a dynamic panel it also uses RigidBodyDynamics, ContactImpulse and SlidingFriction (exist now: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L726`).

**Dependencies.** RAD-16, RAD-17 and EL-132 for the positive; EL-128 for the substitution control.

## 4. Sources and legacy

- [radiation-18](../requirements.md#radiation-18): "Adding it after the moderator suppresses the slow detector; removing it restores response. Dense photon shielding does not substitute for its neutron contract."
- [Named entry](../invest/named-elements.md#radiation-18), owner S628; map row (S605); binding `radiation-01.json`.
- Research "Neutrons" law ("boron-inspired panels absorb; no multiplication, activation or decay chains") and slots 102 "The Final Layer", 105 "Two Kinds of Shelter".
- **Legacy.** None. Hits are coverage snapshots only, e.g. the FiniteLedger bound-source list `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L15430-L15433`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance outline

Readings include the two RAD-17 thin-screen walls (neutron transmission 0.98 each).

- **Construction (actual Chrome UI).** Neutron source, full RAD-17 tank, then the absorber panel, then a supplied Slow detector wired to a Powered gate.
- **Positive.** With the panel the slow reading drops to ≈ 0.42 and the protected gate stays shut.
- **Negative / controls.** Remove the panel: slow response returns (≈ 8.3). Replace it with a Thick dense shield: slow reading stays ≈ 6.3, above On (0.87² transmission). Panel before the moderator: fast flux mostly passes and is moderated afterwards, so the slow detector still responds (≈ 7.5).
- **Boundaries.** Panel edge-on to the path; two stacked panels; a gap beside the panel transmits.
- **Run/Reset.** Pose restores (it can be knocked over).
- **Save/Load.** Pose round-trips.
- **Integrations.** Absorber control in the cross-element task [radiation-19 integration (sequence-task-427)](../requirements.md#sequence-task-427); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15) and [IX-01 Contact impulse](../requirements.md#interaction-01) (the panel is a dynamic body); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 121–130 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 13 of the [campaign plan](../requirements.md#campaign-plan)), reuse 136–150.

## 6. Open questions

1. Proposed fractions need S606-D.
2. Whether a thickness preset is needed for absorption lessons — owner decision.
