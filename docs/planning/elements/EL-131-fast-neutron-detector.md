# EL-131 · Fast-neutron detector — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [element-131](../requirements.md#element-131). Reads fictional toy sources in game units; never real-world detection guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-131 · Fast-neutron detector |
| Type | Radiation |
| Anchor | [requirements.md#element-131](../requirements.md#element-131); [named-elements.md#element-131](../invest/named-elements.md#element-131); scope source [radiation-19](../requirements.md#radiation-19) (umbrella "Neutron group detector", index only) |
| Related identities | Sibling [EL-132 Slow-neutron detector](EL-132-slow-neutron-detector.md); source [RAD-16](RAD-16-neutron-source-module.md); moderator [RAD-17](RAD-17-water-moderator-tank.md); absorber [RAD-18](RAD-18-neutron-absorber-panel.md); photon counterpart and shared defaults [RAD-08](RAD-08-radiation-rate-meter.md). No CAT spec. |
| Proof owner | S629 |
| Roadmap story | unscheduled; campaign chapter 13 (levels 121–130; research slots 100, 101, 104) |
| Status | not started |

## 2. Declaration

The requirement fixes "supplied detector applies declared fast-group response" and "slow-group and photon controls cannot masquerade as fast flux". Values are proposals unless cited.

- **Bodies and shapes.** Static box 0.50 × 0.60 × 0.30 m with a 0.30 × 0.30 m sensing face on local +X (proposed: identical envelope to RAD-08 so only the response differs).
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** `PowerIn` (Electrical, Input) and `Supply` (Electrical, Output), existing sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`); switched route `PowerIn` → `Supply` closed while on (as RAD-08).
- **Sensors and activation.** `RadiationInstrumentState` (Unpowered, Measuring, Saturated, Invalid). Response weight w = 1 for `RadiationKind.Neutron` in `NeutronEnergyGroup.Fast`; w = 0 for Slow neutrons and for every photon and charged kind (proposed: a strictly group-selective detector, the requirement's anti-masquerade rule). [Shared rate law](RAD-08-radiation-rate-meter.md#2-declaration), On 4.0 / Off 3.0 rate units, 0.05 s dwell, saturation 256 (proposed: the RAD-08 defaults so lessons transfer).
- **Work and energy stores.** none (rate only).
- **Parameters.** On/Off thresholds as RAD-08 (f32, 0.5–64 and 0.25 to On − 0.25). Group is fixed by identity (not a mode), because EL-131 and EL-132 are separate named identities.
- **Cosmetic curves and UI bindings.** Needle ← committed fast-group rate (research row "Needle ← rate"); contact lamp; optional clicks with identical muted outcomes.
- **Art.** Cream `#fff8e9` case, navy `#293954` face, gold `#f7cb52` needle, engraved "n" with two forward chevrons (proposed: chevrons mean "fast" by shape; the slow detector uses one bar).
- **Catalogue and inventory.** Id `fast_neutron_detector`, title "Fast-neutron detector", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** The row names none.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/element-02.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); sampled path queries: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); two-group neutron transport and group-selective receivers ([S605 neutron row](../invest/decisions.md#s605)) |
| SignalPropagation | missing | Story 8.1 (supplied contacts); the current `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14` is activation-only |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; detector state resets with Reset |

Also used: ElectricalPower (missing; Story 8.1; legacy `engine/ElectricalNetwork.cs` and `engine/BinaryCircuit.cs` are deleted by Epic 7).

**Dependencies.** RAD-16 source; CAT-005 Battery; an electrical load; RAD-17 for the moderation control.

## 4. Sources and legacy

- [element-131](../requirements.md#element-131) and the [radiation-19](../requirements.md#radiation-19) integration task: "Moderated flow drives Slow while the documented Fast reading decreases; a photon source and missing supply are negative controls."
- [Named entry](../invest/named-elements.md#element-131), owner S629; map row (S605, S257); binding `docs/coverage/engine/element-02.json` (proof S629, relation radiation-19).
- [S605](../invest/decisions.md#s605) neutron row; research "Neutron group detector" row (lessons 100, 101, 104 → 125, 126, 129).
- **Legacy.** None in parts, engine, tests, levels, Campaign or reference. Hits are coverage snapshots only, e.g. the FiniteLedger bound-source list `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L14894-L14897`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Neutron source 1 m from the supplied detector; contact → Powered gate.
- **Positive.** Reading ≈ 15.8 (Fast), gate releases.
- **Negative / controls.** Gamma capsule instead: 0. A full RAD-17 tank between (its two thin-screen walls included): fast reading falls to ≈ 4.6 while a Slow detector beside it switches on (≈ 8.3); the slow flux adds nothing to the fast reading (weight 0). Unsupplied: no contact.
- **Boundaries.** On/Off crossings (≈ 2.0 m and ≈ 2.3 m in open air); saturation; source removed reads zero.
- **Run/Reset.** State Unpowered, contact open.
- **Save/Load.** Thresholds, pose and wires round-trip.
- **Integrations.** Cross-element task [radiation-19 integration (sequence-task-427)](../requirements.md#sequence-task-427) (moderated flow drives Slow while the Fast reading decreases; photon source and missing supply are controls); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-06 Electrical power transfer](../requirements.md#interaction-06) and [IX-07 Signal propagation](../requirements.md#interaction-07); supplied-port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 121–130 with RAD-19 Fast/Slow detection ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 13 of the [campaign plan](../requirements.md#campaign-plan)), reuse 136–150.

## 6. Open questions

1. Whether Fast and Slow detectors should be one part with a typed group mode (the research's "group selection") or two parts as the named identities imply — owner decision.
2. Proposed thresholds and group weights need S606-D.
