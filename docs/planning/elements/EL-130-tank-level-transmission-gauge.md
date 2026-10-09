# EL-130 · Tank-level transmission gauge — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [element-130](../requirements.md#element-130). A toy inspection instrument for fictional toy sources in game units; never real-world level-gauging guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-130 · Tank-level transmission gauge |
| Type | Radiation |
| Anchor | [requirements.md#element-130](../requirements.md#element-130); [named-elements.md#element-130](../invest/named-elements.md#element-130); scope source [radiation-11](../requirements.md#radiation-11) (umbrella, index only) |
| Related identities | Sibling [EL-129 Sheet gauge](EL-129-sheet-thickness-transmission-gauge.md); sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); vessels such as [EL-001 Finite reservoir](EL-001-finite-reservoir.md), [EL-026 Communicating tank](EL-026-communicating-tank.md) or [RAD-17](RAD-17-water-moderator-tank.md); controlled tap [EL-167 Solenoid tap](EL-167-solenoid-tap.md); shared detector defaults [RAD-08](RAD-08-radiation-rate-meter.md). No CAT spec. |
| Proof owner | S619 |
| Roadmap story | unscheduled; campaign chapter 12 (levels 111–120; research slot 89 → 114) |
| Status | not started |

## 2. Declaration

The requirement fixes: a supplied calibrated detector observes attenuation across the actual fill height; empty and full readings follow the material path; a disconnected source is invalid; a rising conserved waterline changes the level contact (radiation-11 task). Values are proposals.

- **Bodies and shapes.** Static box 0.50 × 0.60 × 0.30 m with a 0.30 × 0.30 m sensing face on local +X (proposed: RAD-08 envelope). Source placed on the far side of the vessel at the same height; the gauge height is the level being observed.
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** `PowerIn` (Electrical, Input) and `Supply` (Electrical, Output), existing sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`); switched route closed while the level reads AtOrAbove.
- **Calibration and reference path.** Same pairing rule as EL-129: strongest compatible photon source within ±5° of the face normal; R_ref is the empty-vessel reading under the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration) (vessel walls included, liquid excluded) (proposed: calibrating to "empty" makes "full" a pure material effect). No source, R_ref < 2.0, or misalignment → `Invalid`, contact open, never "full".
- **Sensors and activation.** `RadiationInstrumentState` plus `TransmissionGaugeMode.TankLevel` (fixed by identity). Level state enum `TankLevelState` (BelowPath, AtOrAbovePath, Invalid) (proposed: typed result). AtOrAbovePath when R ≤ 0.8 · R_ref, back to BelowPath when R ≥ 0.9 · R_ref (proposed: water's Medium μ 1.5/m over a 0.5 m wet chord gives T ≈ 0.47, well below 0.8; with the 3 × 3 receiver samples, a waterline that wets only the lowest sample row gives R ≈ 0.82 · R_ref, still BelowPath, and wetting two rows gives ≈ 0.65 · R_ref; the gap is hysteresis against ripple).
- **Work and energy stores.** none.
- **Parameters.** none player-editable beyond pose; the gauge height sets the observed level.
- **Cosmetic curves and UI bindings.** Dial ← committed transmission ratio R / R_ref; Invalid flag; level lamp.
- **Art.** Cream `#fff8e9` case, navy `#293954` dial, gold `#f7cb52` pointer, engraved wave-line mark (proposed: shape cue for the tank mode).
- **Catalogue and inventory.** Id `tank_level_gauge`, title "Level gauge", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** The row names none; empty, filling and full states are separately proven.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/element-02.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); liquid intervals bounded by the committed waterline: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F) |
| SignalPropagation | missing | Story 8.1 (supplied contacts); the current `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14` is activation-only |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; calibration recomputed each Run |

Also used: ElectricalPower (missing; Story 8.1); a conserved liquid store in the observed vessel (FluidAdvection, missing; unscheduled, S418-D).

**Dependencies.** A water vessel and supply (water family); a photon source; CAT-005 Battery; a controlled tap for the feedback lesson.

## 4. Sources and legacy

- [element-130](../requirements.md#element-130) and the [radiation-11](../requirements.md#radiation-11) integration task: "a rising conserved waterline changes the level contact. Missing source is invalid, not a valid heavy/full reading."
- [Named entry](../invest/named-elements.md#element-130), owner S619; map row; binding `docs/coverage/engine/element-02.json` (proof S619, relation radiation-11).
- [S605](../invest/decisions.md#s605) photon row; research "Inspection" law and slot 89 "The Hidden Waterline" (below-path water and a misaligned reference do not report a valid full state).
- **Legacy.** None. Hits are coverage snapshots only: older radiation-11 binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1964-L2008`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Gamma capsule and supplied gauge on opposite sides of a water vessel at the target height; a supplied solenoid tap filling the vessel; gauge `Supply` wired so the level contact closes the tap.
- **Positive.** Water rises through the path: R falls toward 0.47 · R_ref, state AtOrAbovePath, tap closes, water conserved.
- **Negative / controls.** Water below the path: BelowPath. Capsule removed or rotated off-axis: Invalid, not full; the tap is not closed by an invalid reading. Empty vessel reads R ≈ R_ref.
- **Boundaries.** Waterline wetting only the lowest receiver-sample row (≈ 0.82 · R_ref, still BelowPath); draining back below the path; hysteresis band.
- **Run/Reset.** State Unpowered; R_ref recomputed next Run.
- **Save/Load.** Pose and wires round-trip.
- **Integrations.** Cross-element task [radiation-11 integration (sequence-task-419)](../requirements.md#sequence-task-419) (a rising conserved waterline changes the level contact; missing source is invalid); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-08 Fluid advection](../requirements.md#interaction-08) (the observed vessel), [IX-06 Electrical power transfer](../requirements.md#interaction-06) and [IX-07 Signal propagation](../requirements.md#interaction-07); supplied-port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 111–120 as a RAD-11 gauge mode ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 12 of the [campaign plan](../requirements.md#campaign-plan)), reuse 121–125 and 136–150.

## 6. Open questions

1. Does the level gauge output a binary level or a continuous fill estimate — owner decision.
2. Pairing rule (automatic versus explicit link), shared with EL-129 — owner decision.
3. Proposed ratios and the sampled rate law need S606-D.
