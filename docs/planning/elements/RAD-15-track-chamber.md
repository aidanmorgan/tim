# RAD-15 · Track chamber — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-15](../requirements.md#radiation-15) (P3 potential). A toy readout for fictional toy sources in game units.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-15 · Track chamber |
| Type | Radiation (segmented charged-track detector, supplied contact) |
| Anchor | [requirements.md#radiation-15](../requirements.md#radiation-15); [named-elements.md#radiation-15](../invest/named-elements.md#radiation-15) |
| Related identities | Charged sources [RAD-03](RAD-03-alpha-source-cartridge.md), [RAD-04](RAD-04-beta-minus-source-cartridge.md); steering [RAD-14](RAD-14-magnetic-deflector.md); neutral control [RAD-01](RAD-01-gamma-source-capsule.md). No CAT spec refined. |
| Proof owner | S625 |
| Roadmap story | unscheduled; campaign 120, 121 (research slots 95, 96) |
| Status | not started |

## 2. Declaration

The requirement fixes: a readable volume shows persistent short trails from simulated charged trajectories; a supplied segmented readout selects an exit region; the selected segment triggers only for a traversing track; a decorative trail or a miss cannot trigger; neutral photons make no direct track. Values are proposals.

- **Bodies and shapes.** Static body: base box 1.20 × 0.10 × 0.60 m and a frame of thin box posts around a 1.20 × 0.70 × 0.60 m detector volume (proposed: long enough to separate RAD-14 routes at its exit). Entry face (local −X) and exit face (local +X) are open windows; the side walls are thin screens (EL-126 material).
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). The detector volume is air-equivalent (stopping factor 1) so tracks are not shortened.
- **Constraints.** none.
- **Typed ports.** `PowerIn` (Electrical, Input) and `Supply` (Electrical, Output), existing sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`); switched route `PowerIn` → `Supply` closed while a track crosses the selected exit segment.
- **Track definition.** Each charged source contributes one representative track: its emission-axis trajectory (straight outside field regions, bent inside RAD-14), cut off at the source's range (proposed: a beam source with a 10° cone otherwise spreads over every segment; one representative track is readable and deterministic).
- **Sensors and activation.** Exit face divided into three equal segments along local Z, 0.20 m each: Left [−0.30, −0.10], Centre [−0.10, 0.10], Right [0.10, 0.30] m (proposed: three readable choices on a 0.60 m face). A segment is "hit" while a representative track crosses its area at a substep endpoint; the contact stays closed for 0.10 s after the last crossing (proposed: long enough to drive a gate, short enough that a departed particle cannot sustain arrival pulses). Photon and neutron sources contribute no track. Unpowered: no readout.
- **Work and energy stores.** none.
- **Parameters.** `TrackSegment` enum (Left, Centre, Right), inspector-selected, default Centre (proposed: typed segment choice).
- **Cosmetic curves and UI bindings.** Trails ← committed track samples: a short line segment per sample that fades over 1.5 s (proposed: "persistent short trails" readable after the fact). The trail is presentation only and never feeds the readout. Selected segment outlined; segment lamp ← contact.
- **Art.** Cream `#fff8e9` frame, cyan `#66b8c9` translucent walls, gold `#f7cb52` trails, navy `#293954` segment dividers engraved 1/2/3 (proposed: numbered segments by shape).
- **Catalogue and inventory.** Id `track_chamber`, title "Track chamber", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

### Variant: Left segment (`TrackSegment.Left`)
### Variant: Centre segment (`TrackSegment.Centre`)
### Variant: Right segment (`TrackSegment.Right`)
- Each selected segment is separately proven with a positive (track through it) and a control (track through a neighbour).

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); track-versus-segment crossing queries: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); charged trajectory samples exposed to sensors |
| SignalPropagation | missing | Story 8.1 (supplied contacts); the current `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14` is activation-only |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; segment state joins it with this element (unscheduled) |

Also used: trail animation binding (AnimationEvaluation exists on the animation worker; map composition: "actual charged trajectory samples bind shared bounded trail animation, without controlling propagation or replaying stale tracks"); ElectricalPower (missing; Story 8.1).

**Dependencies.** RAD-03 or RAD-04; RAD-14 for curved routes; CAT-005 Battery; an electrical load.

## 4. Sources and legacy

- [radiation-15](../requirements.md#radiation-15): "The selected exit segment triggers only for a traversing track; a decorative trail or a miss cannot trigger it. Neutral photons make no direct charged track in the initial abstraction."
- [Named entry](../invest/named-elements.md#radiation-15), owner S625; map row (S605, S257); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) beta-minus row; research row ("only that segment's contact fires"; slots 95 "A Line in the Mist", 96 "Read the Turn").
- **Legacy.** None. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2103-L2133`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

Layout: cartridge outlet at the RAD-14 region entry, aimed along the axis; chamber entry face against the region exit; RAD-14 strength set to 1 (proposed: at strength 1 the routes stay inside the 0.60 m exit face). Track offsets at the chamber exit (1.20 m after the region): beta-minus r = 4.0 m gives 0.020 m at the region exit plus 1.20 × tan 5.7° = 0.141 m; alpha r = 6.0 m gives 0.013 + 1.20 × tan 3.8° = 0.094 m. Paths ≈ 1.6 m, within both ranges.

- **Construction (actual Chrome UI).** Place the cartridge, the deflector and the supplied chamber in line; select the segment in the inspector; chamber `Supply` → Powered gate.
- **Positive.** Beta, Forward: track exits at z = +0.141 m (Right); with Right selected, contact closes and the gate releases. Beta, Reverse: −0.141 m (Left) with Left selected. Field off: z = 0 (Centre) with Centre selected.
- **Negative / controls.** Each positive repeated with a neighbouring segment selected: trail visible, no trigger. Cartridge moved away while the old trail still fades: no new trigger. Gamma capsule aimed through: no track, no trigger. Chamber unsupplied: no trigger. Alpha at strength 1 stays in Centre (−0.094 m), showing its gentler curvature; at strength 2 it reaches Left (−0.188 m).
- **Boundaries.** A track exactly on a divider (±0.10 m) hits both adjacent segments (closed intervals); a very short crossing still registers. Sensitivity: the alpha strength-1 track sits only 6.5 mm inside the Centre/Left divider, so small changes to k_Alpha, the strength or the layout move it into Left; re-derive this case whenever S606-D or LAW-FIELD-D change the proposed values, and prefer the beta cases (41 mm clear of the dividers) for the segment proof.
- **Run/Reset.** Trails cleared; contact open; segment choice kept.
- **Save/Load.** Segment, pose and wires round-trip.
- **Integrations.** No row-level cross-element task names the chamber; it integrates through interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-06 Electrical power transfer](../requirements.md#interaction-06) and [IX-07 Signal propagation](../requirements.md#interaction-07); supplied-port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 111–120 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 12 of the [campaign plan](../requirements.md#campaign-plan)), track interpretation continued in 121–130 (chapter 13), reuse 121–125 and 136–150.

## 6. Open questions

1. Number of segments (three proposed) and whether the count is a preset — owner decision.
2. Contact hold time after a crossing, and how a track exactly on a divider is assigned — owner decision.
3. The single representative track per source is a proposed abstraction (S606-D): should a 10° beam instead light every segment its cone covers — owner decision.
