# RAD-01 · Gamma source capsule — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-01](../requirements.md#radiation-01) (P1 potential). This is a **fixed-strength sealed toy source**; every value is game fiction, never real-world source or shielding guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-01 · Gamma source capsule |
| Type | Radiation (photon source) |
| Anchor | [requirements.md#radiation-01](../requirements.md#radiation-01); [named-elements.md#radiation-01](../invest/named-elements.md#radiation-01) |
| Related identities | Receiver [RAD-08](RAD-08-radiation-rate-meter.md); shields [EL-126](EL-126-thin-screen-shield.md), [EL-127](EL-127-polymer-shield.md), [EL-128](EL-128-dense-shield.md); controllable alternative [RAD-02](RAD-02-powered-x-ray-emitter.md); decaying variant concept [RAD-13](RAD-13-decay-clock-capsule.md). No CAT spec. |
| Proof owner | S608 |
| Roadmap story | unscheduled; campaign 101, 103, 110 (research slots 76, 78, 85) |
| Status | not started |

## 2. Declaration

The requirement fixes: continuous emission during Run, exposure controlled only by moving the capsule or shielding, no electrical off switch, exact restore of pose and initial state. All values are proposals.

- **Bodies and shapes.** One static body with a box collider 0.30 × 0.40 × 0.30 m (proposed: a hand-sized capsule at the catalogue's ~1 m part scale; current shapes are sphere, box and plane only, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L6-L7`). Emitting surface: a 0.10 m radius sphere at the body centre, centre-sampled under the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration) because it is under 0.25 m wide (proposed: a finite emitter inside the 0.25 m distance floor). The capsule's own collider is not counted as shielding of its own emission.
- **Mass and material.** Static, zero mass. Contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (current static default, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** none. Any Connect attempt to the capsule is refused (proposed: "no electrical off switch for radioactive decay" made structural, not a disabled toggle).
- **Sensors and activation.** none; emission starts at the first committed tick of Run and never stops within the Run.
- **Source declaration.** `RadiationKind.Gamma`, `PhotonEnergyBand.Medium`, isotropic, activity A = 16 game units/s (proposed: a default meter reads ≈ 15.8 at 1 m, the On boundary lies at 1.997 m and Off at 2.307 m on the 16.8 m bench, and one Thick dense slab quiets it at 1 m). No decay within a Run: half-life "none" (the research row "steady gamma uses none").
- **Work and energy stores.** Emitted-quanta ledger only (FiniteLedger); no player-visible store.
- **Parameters.** none — fixed strength. Pose is set with the move/rotate gizmo in Build only.
- **Cosmetic curves and UI bindings.** Selected-only dotted field-path overlay showing the response region; no glow, strobe or particles; Build preview shows the initial field without advancing state ([research presentation](../../radiation-component-research.md#presentation)).
- **Art.** Sealed ceramic cream `#fff8e9` capsule body with a gold `#f7cb52` brass-like cap and an engraved γ mark, small navy `#293954` cradle (proposed: the research's "sealed ceramic/brass-like capsules" in approved palette values; no radioactive green).
- **Catalogue and inventory.** Id `gamma_source_capsule`, title "Gamma capsule", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)); counted `PartAllowance` (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** None in the requirement row (one fixed-strength capsule).

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); emitted-quanta ledger; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | dynamic AABB BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and sphere/box/plane narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); sampled source-to-receiver path intervals: unscheduled (P0-004, S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); photon source and per-receiver contributions ([S605 photon row](../invest/decisions.md#s605)) |
| RadioactiveDecay | missing | unscheduled (S607-D/F); steady case (S605 decay row: "a steady source does not fade") |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; pose and initial state restore with Reset |

Also needed: the selected-only field overlay (presentation; unscheduled with this element). Selection and gizmo placement exist in `engine/gpu/WorkshopUi.cs`.

**Dependencies.** RAD-08 rate meter to observe; CAT-005 Battery and CAT-051 Powered gate for the first-slice construction.

## 4. Sources and legacy

- [radiation-01](../requirements.md#radiation-01): "Moving the capsule farther away lowers the meter reading; disconnecting a nearby battery does not stop emission. Source pose and initial state restore exactly."
- [Named entry](../invest/named-elements.md#radiation-01), owner S608; map row (S605); binding `radiation-01.json` (proof S608).
- [S605](../invest/decisions.md#s605) photon and decay rows; research per-element row and [first-slice acceptance](../../radiation-component-research.md#chrome-observable-acceptance-for-the-first-radiation-slice) (slot 76 "The Quiet Beacon").
- **Legacy.** No gamma source in `parts/`, engine, tests, `content/puzzles.json`, `tools/Campaign` or `reference/`. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1628-L1658`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Place the capsule 1 m from a Battery-supplied photon Rate meter whose contact drives a Powered gate holding a ball (all via palette, gizmo and Connect).
- **Positive.** Reading ≈ 15.8, above On; gate releases, ball reaches the Receiver.
- **Negative / controls.** Move the capsule to 3 m: reading ≈ 1.8, below Off, gate shut. Disconnect a second Battery placed beside the capsule: emission is unchanged. A Connect attempt from a Battery to the capsule is refused and the construction is unchanged.
- **Boundaries.** Capsule touching the meter (samples clamp at the 0.25 m floor); On crossing at 1.997 m and Off at 2.307 m; Thick dense slab on versus beside the path (≈ 2.1 versus ≈ 15.8); insertion order and camera position do not change the result; the overlay appears only while selected.
- **Run/Reset.** Pose and the initial (non-decayed) state restore bit-exactly.
- **Save/Load.** Pose round-trips in the current schema.
- **Integrations.** Source partner in the cross-element tasks [radiation-05 integration (sequence-task-413)](../requirements.md#sequence-task-413) and [radiation-11 integration (sequence-task-419)](../requirements.md#sequence-task-419), and the photon control in [radiation-19 integration (sequence-task-427)](../requirements.md#sequence-task-427); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15) and [IX-16 Radioactive decay](../requirements.md#interaction-16) (steady case); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 101–110 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 11 of the [campaign plan](../requirements.md#campaign-plan)), reuse 111–120, 124–130 and 136–150.

## 6. Open questions

1. Proposed activity, band and emitter size, and the sampled rate law, need the S606-D decision.
2. RadioactiveDecay is bound but the source is "fixed-strength": confirm that a steady population with no in-Run depletion satisfies "spent inventory cannot emit indefinitely" — owner decision (S607-D).
3. Whether the capsule is static, or a dynamic movable object during Run — owner decision.
