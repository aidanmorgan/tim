# EL-046 · Listening horn — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared acoustic facts A1–A13 are in [EL-044](EL-044-tone-selective-sound-meter.md#shared-acoustic-family-facts). This spec defines the **acoustic route rules R1–R6** that [EL-047 Exit horn](EL-047-exit-horn.md) and [EL-048 Acoustic duct](EL-048-acoustic-duct.md) reuse.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-046 · Listening horn |
| Type | Sound |
| Anchor | [requirements.md#element-046](../requirements.md#element-046); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-046); owner S540 |
| Related | Refines no CAT spec. Route: listening horn → [EL-048 duct](EL-048-acoustic-duct.md) → [EL-047 exit horn](EL-047-exit-horn.md). Sources: [CAT-061 Speaker](CAT-061-speaker.md), [CAT-009 Bell](CAT-009-bell.md), [EL-045](EL-045-air-whistle.md). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root body, collider box 0.8 × 0.8 × 0.8 m centred at (0.1, 0.1, 0) **proposed** (bounds a flared horn with mouth radius 0.4 m and length 0.8 m — Speaker-scale, `parts/SpeakerPart.cs@a6c914e:L93-L93` cabinet 1.25 × 1.5 × 1.25 m); navy foot 0.9 × 0.14 × 0.7 m at (0, −0.37, 0) **proposed**. Mouth centre (aperture point) at local (0.5, 0.1, 0), facing +X **proposed**. |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`); opaque for sound (A7), so a horn also shadows what is behind it. |
| Constraints | none. |
| Typed ports | `SoundOut` (Acoustic, Output) at the throat, local (−0.4, 0.1, 0) **proposed**. Domain `Acoustic` is a new `WorkshopConnectionDomain` value **proposed** (current enum has Activation and Electrical only, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L8`). |
| Sensors and activation | Acoustic receiver at the aperture point with an acceptance cone (R1); no electrical supply needed (it is passive). |
| Work and energy stores | none; passive coupling cannot add energy (R2). |
| Parameters | `coupling`: f32, 0.1–0.9, default 0.8 **proposed** (a finite aperture loses some energy; below 1 so no route can amplify). Acceptance half-angle fixed 35° **proposed** (mirrors the speaker's emission cone, A2, so a facing pair couples and a side-on one does not). |
| Cosmetic curves and UI bindings | Thin translucent wavefronts shrink into the mouth when a pulse is captured; a gold pulse dot travels into the throat ← committed capture occurrence **proposed** (component research binding "Pulse travel ← committed transit", [component research](../../component-research.md#sound)). |
| Art | Cream `#fff8e9` flared bell with a gold `#e8b764` rim, navy `#293954` throat and foot, gold `#f7cb52` port sphere r 0.075 at `SoundOut` ([DESIGN.md Sound speaker](../../../DESIGN.md#sound-speaker) cream cabinet / gold rim language). Geometry **proposed**. |
| Catalogue / inventory | Id `listening_horn`, Title "Listening horn", Category Sound **proposed**. Description **proposed**: "Catches sound that reaches its open mouth and sends it down a connected duct. Sound from the side, behind or behind a wall is not caught." |

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

### Acoustic route rules (shared by EL-046, EL-047, EL-048)

All **proposed** (the legacy has direct paths only, A6; the bounded decision is [S528](../invest/decisions.md#s528) event-routing → S529):

| # | Rule |
| --- | --- |
| R1 | **Capture.** For each admitted pulse (A5) arriving at the aperture point within the 35° acceptance cone of the mouth axis and with an unobstructed opaque trace (A6, A7), the horn captures one routed pulse carrying the source occurrence identity, tone and strength = arrival level × `coupling`. Wrong-facing or occluded arrivals capture nothing. |
| R2 | **No gain.** Every coupling and loss factor is < 1, so a routed pulse is always weaker than the arrival that fed it. |
| R3 | **Transit.** A duct of length L delays a routed pulse by L / 12 m/s (A2 speed) and multiplies its strength by e^(−0.05·L) **proposed** (5% loss per metre: an 8 m duct keeps about two thirds). |
| R4 | **Release.** An exit horn re-emits each routed pulse that reaches it as a new cone pulse (A2 constants, origin at its mouth, along its axis) with strength × its `coupling`, recorded as a child occurrence of the source occurrence. |
| R5 | **Identity and loops.** A routed pulse never re-enters a route it has already traversed; relay depth is capped at 4 **proposed** (component research: "bounded lifetime and relay depth"); a later independent pulse from the same source routes again. |
| R6 | **Typed mouths only.** Only `SoundOut` → `SoundIn` duct connections carry sound; an unconnected mouth carries nothing, and ball pipes or hoses never carry sound (component research: "ball pipes do not silently gain acoustic capability"). |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S540): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction).

**Exists now**
- Static boxes: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`.
- Typed connection storage and contextual Connect UI (activation only): `engine/gpu/WorkshopConnections.cs@a6c914e:L65-L99`, `ui/WorkshopConnections.cs@a6c914e:L37-L72`.

**Missing**
- AcousticPropagation pulses, occlusion and reception — Stories 14.1–14.3; [S528](../invest/decisions.md#s528) (S530 arrival combination).
- Acoustic domain, aperture capture and routed occurrences (R1–R6) — this family, [S528](../invest/decisions.md#s528) event-routing (S529); typed ports [S257](../invest/decisions.md#s257).

**Dependencies.** A sound source (Speaker with Battery, or Bell), EL-048 duct and EL-047 exit horn to complete a route, a Sound meter to observe.

## 4. Sources and legacy

- Requirement row [element-046](../requirements.md#element-046): "Finite aperture couples incident acoustic energy into a duct"; outcome "Wrong-facing or occluded arrivals lose coupling". Scope index [todo-352](../requirements.md#todo-352): "connected horn/ducts carry bounded sound".
- Named entry [element-046](../invest/named-elements.md#element-046), owner S540. Component research: "Listening horn / Exit horn / Acoustic duct (EL-046–048) | Acoustic network nodes | attenuation, delay | Collects sound into typed ports and carries it to an exit around a blocking wall | Pulse travel ← committed transit"; recipe "Quiet Corridor: horn → duct elbow → exit horn routes around a sound-blocking wall".
- Legacy: no horn, duct or routed sound exists; legacy reception is direct-path only with opaque blocking (A6, `engine/Acoustics.cs@a6c914e:L49-L79`). The speaker's 35° cone (`engine/Acoustics.cs@a6c914e:L17-L17`) is reused for acceptance.
- **Files harvested:** `parts/SpeakerPart.cs` (scale reference); acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-046](../requirements.md#element-046), [acoustic profile](../invest/profiles.md#acoustic), S528 event-routing construction.

- **Construction (actual Chrome UI).** Speaker (Battery, Switch) facing a Listening horn 2 m away; a Wall blocks the direct line to a supplied Sound meter; Connect horn `SoundOut` → duct → Exit horn `SoundIn`; exit horn aimed at the meter.
- **Positive.** The pulse is caught, travels the duct (visible dot, delay L/12 s) and the meter triggers once after the delay.
- **Negative / control.** Horn rotated 90° or 180° (wrong-facing): nothing caught, meter silent; an opaque Wall between speaker and horn: nothing caught (outcome); horn without a duct: nothing reaches the meter.
- **Boundaries.** Speaker just inside and just outside the 35° acceptance cone; `coupling` 0.1 and 0.9; meter reading always below the direct-path reading at equal distance (R2).
- **Run/Reset.** Reset clears captured and in-transit pulses.
- **Save/Load.** `coupling`, pose and duct connection round-trip.
- **Integrations.** Acoustic integration task [sequence-task-407](../requirements.md#sequence-task-407) ("connected horn/ducts carry bounded sound"), with [EL-048](EL-048-acoustic-duct.md) and [EL-047](EL-047-exit-horn.md) completing the route; interaction process [IX-12 acoustic propagation](../requirements.md#interaction-12) ("an obstructed control receives the predicted diminished field"). Campaign: "listening horn" is named in the sound row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 71–80 (reuse 81–100, 117–125, 136–150).

## 6. Open questions

1. **Routing model.** R1–R6 are proposals; S529 must settle event identity, delay and loop rejection.
2. **Aperture size.** Whether the mouth radius scales capture (a larger horn catches weaker arrivals) or only `coupling` does: owner decision.
3. **Arrival combination.** If S530 adopts additive intensity, whether a horn captures every arrival or only the strongest: owner decision.
