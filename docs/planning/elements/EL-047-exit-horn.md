# EL-047 · Exit horn — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared acoustic facts A1–A13 are in [EL-044](EL-044-tone-selective-sound-meter.md#shared-acoustic-family-facts); acoustic route rules R1–R6 in [EL-046](EL-046-listening-horn.md#acoustic-route-rules-shared-by-el-046-el-047-el-048).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-047 · Exit horn |
| Type | Sound |
| Anchor | [requirements.md#element-047](../requirements.md#element-047); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-047); owner S534 |
| Related | Refines no CAT spec. End of the route [EL-046](EL-046-listening-horn.md) → [EL-048](EL-048-acoustic-duct.md) → EL-047; its output is heard by [CAT-060](CAT-060-sound_meter.md) / [EL-044](EL-044-tone-selective-sound-meter.md) or caught again by another listening horn. Emission pattern matches [CAT-061 Speaker](CAT-061-speaker.md). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Same horn body as EL-046: static collider box 0.8 × 0.8 × 0.8 m centred at (0.1, 0.1, 0), navy foot 0.9 × 0.14 × 0.7 m at (0, −0.37, 0), mouth (emission origin) at local (0.5, 0.1, 0) facing +X **proposed** (one horn geometry for both ends keeps the pair readable; the role is shown by port and colour band). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`); opaque for sound (A7). |
| Constraints | none. |
| Typed ports | `SoundIn` (Acoustic, Input) at the throat, local (−0.4, 0.1, 0) **proposed**. No electrical port: the exit horn is passive. |
| Sensors and activation | none. It does not receive open-air sound at its mouth (that is the listening horn's role). |
| Work and energy stores | none. Release rule R4: every routed pulse arriving at `SoundIn` is re-emitted once as a cone pulse with the A2 constants (12 m/s, 8 m range, 0.15 s, 35° half-angle), tone unchanged, strength × `coupling`; with no routed pulse nothing is emitted. |
| Parameters | `coupling`: f32, 0.1–0.9, default 0.8 **proposed** (same finite loss as the listening horn; R2 forbids gain). Pattern fixed Cone **proposed** ("directed acoustic field" in the row; the speaker's cone is the precedent, `parts/SpeakerPart.cs@a6c914e:L85-L85`). |
| Cosmetic curves and UI bindings | Forward translucent wavefronts per released pulse ← committed child occurrence, as the speaker's shadow-free fading rings ([DESIGN.md Sound speaker](../../../DESIGN.md#sound-speaker); wavefront binding `parts/SpeakerPart.cs@a6c914e:L55-L55`); a gold dot arrives at the throat before release **proposed**. |
| Art | Cream `#fff8e9` flared bell, gold `#e8b764` rim, cyan `#66b8c9` band on the bell marking "out" **proposed** (distinguishes it from the listening horn without text), navy `#293954` throat and foot, gold `#f7cb52` port sphere r 0.075. |
| Catalogue / inventory | Id `exit_horn`, Title "Exit horn", Category Sound **proposed**. Description **proposed**: "Sends sound from a connected duct out of its mouth in a forward cone. With nothing connected it stays silent." |

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S534): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction).

**Exists now**
- Static boxes: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`.

**Missing**
- Cone pulse emission and reception — Story 14.3 (CAT-061 Speaker) and 14.1 (CAT-060); [S528](../invest/decisions.md#s528).
- Routed-pulse release (R4) and child occurrences — [S528](../invest/decisions.md#s528) event-routing (S529); acoustic ports [S257](../invest/decisions.md#s257).

**Dependencies.** EL-046 and EL-048 (no other source feeds `SoundIn`); a sound source upstream; a meter downstream.

## 4. Sources and legacy

- Requirement row [element-047](../requirements.md#element-047): "Converts incoming duct energy into a directed acoustic field"; outcome "Disconnected inlet emits nothing".
- Named entry [element-047](../invest/named-elements.md#element-047), owner S534; component research row EL-046–048 and recipe "Quiet Corridor" ([component research](../../component-research.md#sound)).
- Legacy: no exit horn. The speaker's emission facts are the precedent: pulse created at the transformed mouth along local +X with pattern Cone (`parts/SpeakerPart.cs@a6c914e:L84-L86`); five forward rings, 64 segments, shadow-free (`parts/SpeakerPart.cs@a6c914e:L104-L114`).
- **Files harvested:** `parts/SpeakerPart.cs` (emission and ring precedent); acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-047](../requirements.md#element-047), [acoustic profile](../invest/profiles.md#acoustic).

- **Construction (actual Chrome UI).** The EL-046 "Quiet Corridor" construction: speaker → listening horn → duct → exit horn → supplied Sound meter behind a Wall.
- **Positive.** After the duct delay, forward rings leave the exit horn and the meter triggers.
- **Negative / control.** Remove the duct (exit horn `SoundIn` unconnected): the exit horn never emits (outcome); exit horn turned away from the meter: meter silent; a Wall in front of the exit horn: meter silent; the exit horn never responds to a speaker aimed straight at its mouth.
- **Boundaries.** Meter at 7.9 m and 8.1 m from the exit horn (range); meter just inside and outside the 35° cone; reading below the reading the source would give directly at the same distance (R2).
- **Run/Reset.** Reset clears in-transit and released pulses.
- **Save/Load.** `coupling`, pose and duct connection round-trip.
- **Integrations.** Acoustic integration task [sequence-task-407](../requirements.md#sequence-task-407) ("connected horn/ducts carry bounded sound"), fed by [EL-046](EL-046-listening-horn.md) through [EL-048](EL-048-acoustic-duct.md); interaction process [IX-12 acoustic propagation](../requirements.md#interaction-12). Campaign: "exit horn" is named in the sound row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 71–80 (reuse 81–100, 117–125, 136–150).

## 6. Open questions

1. **Shared geometry.** One horn body for both roles (proposed) versus distinct shapes: owner decision.
2. **Pattern choice.** Whether an exit horn may be configured omnidirectional: owner decision.
