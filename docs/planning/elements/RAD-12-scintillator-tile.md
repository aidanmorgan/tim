# RAD-12 · Scintillator tile — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-12](../requirements.md#radiation-12) (P2 potential). Toy conversion of fictional toy sources in game units; never real-world detector guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-12 · Scintillator tile |
| Type | Radiation → Optical converter |
| Anchor | [requirements.md#radiation-12](../requirements.md#radiation-12); [named-elements.md#radiation-12](../invest/named-elements.md#radiation-12) |
| Related identities | Optical receiver [CAT-038 light receiver](CAT-038-light_receiver.md) and [EL-153 General-light receiver](../invest/named-elements.md#element-153); sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); shield control [EL-128](EL-128-dense-shield.md). No CAT spec refined. |
| Proof owner | S622 |
| Roadmap story | unscheduled; campaign 117, 122 (research slots 92, 97) |
| Status | not started |

## 2. Declaration

The requirement fixes: absorbed ionizing radiation becomes a bounded amount of visible light at a declared optical aperture; a separate supplied optical receiver can control machinery; shielding prevents both; conversion loss is explicit and output cannot exceed absorbed energy. Values are proposals.

- **Bodies and shapes.** Static body: tile box 0.40 × 0.40 × 0.10 m on a foot box 0.40 × 0.06 × 0.25 m (proposed: smaller than a shield face; 0.10 m thick so it absorbs a useful fraction).
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). `RadiationMaterialKind.Scintillator`: photon μ = 16 / 6 / 3 per metre (Soft/Medium/Hard), charged stopping factor 300, neutron Fast/Slow transmit 0.98 (proposed: absorbs 1 − e^(−0.6) ≈ 45 % of Medium photons through 0.10 m, stops alpha and beta, ignores neutrons).
- **Constraints.** none.
- **Typed ports.** none — light leaves through the aperture and crosses space like any optical source; no wire carries it.
- **Sensors and activation.** Conversion each tick: the tile is a receiver under the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration) (radiation face 0.40 m, sampled 3 × 3); its absorbed contribution E_abs (incident rate × absorbed fraction, summed over kinds) → optical power P = η · κ · E_abs, with κ = 0.25 game optical power per absorbed rate unit and η = 0.25 (proposed: at 1 m from a Gamma capsule the 0.40 m face reads ≈ 15.6, so the tile deposits ≈ 15.6 × 0.45 ≈ 7.05, giving P ≈ 0.44, above the 0.25 default light-receiver threshold in [CAT-038](CAT-038-light_receiver.md#declaration)). The (1 − η) remainder is recorded as conversion loss in the ledger. Absorption resolves once: a second tile behind the first receives only the transmitted remainder.
- **Optical aperture.** 0.10 × 0.10 m on local +X face, emitting a broadband (white, equal R/G/B) cone of 20° half-angle (proposed: broadband so any broadband receiver can sample it; a narrow cone so alignment matters, lesson 92).
- **Work and energy stores.** none; output is instantaneous and never exceeds absorbed energy.
- **Parameters.** none player-editable (efficiency fixed by identity).
- **Cosmetic curves and UI bindings.** Glow ← committed absorbed energy (research row); aperture rim lights in proportion to P.
- **Art.** Cream `#fff8e9` tile with a cyan `#66b8c9` translucent face, gold `#f7cb52` aperture ring, navy `#293954` foot, engraved arrow from the radiation face to the aperture (proposed: the arrow shows the conversion direction by shape).
- **Catalogue and inventory.** Id `scintillator_tile`, title "Scintillator", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** None in the row.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); absorbed energy and conversion loss; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); sampled path queries: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); deposition per receiver and material |
| OpticalTransport | missing | Stories 13.1 (sources) and 13.2 (receivers); the coverage JSON's "Partial" basis is legacy `engine/OpticalNetwork.cs`, deleted by Epic 7. Map composition: "ionizing deposition debits energy before bounded OpticalTransport emission; conversion is not an unpowered light source" (S484) |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161` (no stored state of its own) |

**Dependencies.** CAT-038 light receiver (Story 13.2) or EL-153; CAT-005 Battery; a photon source.

## 4. Sources and legacy

- [radiation-12](../requirements.md#radiation-12): "Incident radiation produces observable light and a receiver response; shielding prevents both. Conversion loss is explicit and output cannot exceed absorbed energy."
- [Named entry](../invest/named-elements.md#radiation-12), owner S622; map row (S484, S605); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research "Conversion" law ("absorption resolves once so serial tiles cannot each claim the original energy") and slots 92 "Borrowed Light", 97.
- **Legacy.** No scintillator part or test. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2009-L2039`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Gamma capsule 1 m from the tile's radiation face; a supplied light receiver 0.5 m in front of the aperture, its contact → Powered gate.
- **Positive.** Tile glows, P ≈ 0.44, receiver closes, gate releases.
- **Negative / controls.** Thick dense slab between capsule and tile: E_abs ≈ 1.0, P ≈ 0.06 below 0.25, no response. Receiver rotated away from the aperture: no response. Receiver facing the capsule directly with no tile: no response (radiation is not light).
- **Boundaries.** Two tiles in series: the second absorbs only the ≈ 55 % remainder, so total optical output ≤ the absorbed-energy budget; aperture cone edge; output never exceeds η · κ · E_abs.
- **Run/Reset.** Glow and receiver state return to zero.
- **Save/Load.** Pose round-trips.
- **Integrations.** No row-level cross-element task names the tile; it integrates through interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15) and [IX-13 Optical transport](../requirements.md#interaction-13) (deposition debited before optical emission); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 111–120 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 12 of the [campaign plan](../requirements.md#campaign-plan)), reuse 121–125 and 136–150, including the radiation → light → sound capstone.

## 6. Open questions

1. The radiation-to-optical exchange κ and efficiency η need S606-D with S488-D — owner decision.
2. Broadband (white) output or a single coloured channel (for coloured receivers) — owner decision.
