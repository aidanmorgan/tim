# EL-050 · Acoustic screen — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared acoustic facts A1–A13 are in [EL-044](EL-044-tone-selective-sound-meter.md#shared-acoustic-family-facts).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-050 · Acoustic screen |
| Type | Sound |
| Anchor | [requirements.md#element-050](../requirements.md#element-050); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-050); owner S537 |
| Related | Refines no CAT spec. Physical sibling of the [CAT-066 Wall](../requirements.md#current-cat-066) (which fully blocks sound as an opaque body, A7); redirecting sibling [EL-051 Acoustic dish](EL-051-acoustic-dish.md). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | One static box panel, full size `width` × `height` × `thickness`, centred at the origin, face normal local +X **proposed** (a free-standing panel; its finite edges leave real gaps). Navy foot rail 0.3 × 0.1 × `width` at the base **proposed**. |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). The panel blocks balls like any static box. New acoustic surface material per variant (below) replaces the binary opaque rule for this collider only. |
| Constraints | none. |
| Typed ports | none — a passive surface; no electrical, activation or acoustic sockets. |
| Sensors and activation | none. |
| Work and energy stores | none. Acoustic interaction applies only to straight arrival paths that actually intersect the panel (A6 trace); paths that pass beside, over or under it are unaffected. Transmitted and reflected strengths are each ≤ the incident strength, and their sum ≤ incident (no gain). |
| Parameters | `width`: f32 0.5–4 m, default 1.5 m; `height`: f32 0.5–4 m, default 1.5 m; `thickness`: f32 0.05–0.3 m, default 0.1 m **proposed** (panel-scale counterpart of the Wall's width/height/thickness, [current-cat-066](../requirements.md#current-cat-066); a 1.5 m square hides a Speaker). `material`: closed enum `AcousticSurface { Absorbing, Reflecting }` (variants). |
| Cosmetic curves and UI bindings | Static. Wavefront art is clipped visually at the panel; the meter, not the decorative ring, is the authoritative feedback ([DESIGN.md Sound meter](../../../DESIGN.md#sound-meter)). Resize uses the Wall's three local-axis handles (one gesture, one Undo). |
| Art | Absorbing: cream `#fff8e9` quilted panel with navy `#293954` stitching dots. Reflecting: smooth cyan `#66b8c9` panel with a gold `#e8b764` edge. Navy foot rail. All **proposed** within the palette (texture, not text, distinguishes the materials). |
| Catalogue / inventory | Absorbing: Id `sound_screen`, Title "Quiet screen"; Reflecting: Id `sound_reflector`, Title "Echo board" **proposed**; Category Sound. Description **proposed**: "A finite panel that dampens (or bounces) sound that hits it. Sound going around its edges is unaffected." |

**Variants** (row: "attenuates or reflects according to its declared properties"):

| Variant | `material` | Transmitted through the panel | Reflected |
| --- | --- | --- | --- |
| Absorbing | Absorbing | strength × 0.2 **proposed** (clearly below a default 0.25 meter threshold at most distances, but not a perfect mute) | none |
| Reflecting | Reflecting | 0 | one child pulse from the hit point along the mirror direction about the face normal, same tone, carrying the source strength × 0.7 **proposed** (a hard board loses some energy); its level, arrival time and 8 m range use the total path source → board → listener, attenuated once as in the [EL-051](EL-051-acoustic-dish.md) total-path rule, so it always reads 0.7 × the direct reading at the same path length |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S537): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction).

**Exists now**
- Static box body, collider and resizable-box precedent (Wall): `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`; `WorkshopPartKind.Wall` (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Missing**
- AcousticPropagation with opaque blocking — Stories 14.1–14.3; [S528](../invest/decisions.md#s528).
- Per-collider acoustic surface material (partial transmission, specular reflection with child occurrences) — new under S528/S529; reflection identity and range bookkeeping are S529.

**Dependencies.** A sound source and a Sound meter; Wall resize handles (CAT-066) for the size controls.

## 4. Sources and legacy

- Requirement row [element-050](../requirements.md#element-050): "Material surface attenuates or reflects sound according to its declared properties"; outcome "A geometric gap permits transmission; no universal mute field".
- Named entry [element-050](../invest/named-elements.md#element-050), owner S537; component research: "Acoustic screen / Acoustic dish (EL-050, EL-051) | Occluder / redirector with explicit attenuation | attenuation | Blocks or redirects; no diffraction/interference claim" ([component research](../../component-research.md#sound)).

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| C1 | Sound traces test only opaque colliders; an opaque hit before the receiver discards the arrival entirely (binary). | `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79`, `engine/Acoustics.cs@a6c914e:L70-L73` | carry forward for ordinary bodies; the screen adds graded material behaviour |
| C2 | A wall between speaker and meter gives level 0; geometry only blocks the straight path. | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L14-L54` | carry forward (gap test: a path beside the panel is unaffected) |

- **Files harvested:** acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-050](../requirements.md#element-050), [acoustic profile](../invest/profiles.md#acoustic).

- **Construction (actual Chrome UI).** Battery → Speaker → supplied Sound meter 4 m away; place a Quiet screen between them with move/rotate tools; resize with its handles.
- **Positive.** Absorbing: the meter reading drops to about one fifth and no longer triggers at the default threshold. Reflecting: with the board at 45°, a meter placed on the mirror path triggers; the direct meter behind the board reads 0.
- **Negative / control.** Narrow the screen (or move it aside) so the straight path passes the edge: the meter reads exactly its unscreened value (outcome: a gap transmits; no mute field around the panel). A meter beside the screen but with a clear path is unaffected.
- **Boundaries.** Size limits; the reflected reading never exceeds 0.7 × the direct reading at the same total path length; the reflected pulse expires once the total path passes 8 m.
- **Run/Reset.** Reset clears in-flight and reflected pulses.
- **Save/Load.** Size, `material` and pose round-trip.
- **Integrations.** Acoustic integration task [sequence-task-407](../requirements.md#sequence-task-407) ("Screens/dishes and water-tuned bottles each retain their later individual contract"); interaction process [IX-12 acoustic propagation](../requirements.md#interaction-12) ("supported occlusion; an obstructed control receives the predicted diminished field"). Campaign: "acoustic screen" is named in the sound row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 71–80 (reuse 81–100, 117–125, 136–150).

## 6. Open questions

1. **Transmission value.** 0.2 for Absorbing is a proposal; owner decision.
2. **Reflection scope.** Whether reflection is required at all for EL-050 (it overlaps EL-051's redirecting role): owner decision.
3. **Ordinary walls.** Whether the CAT-066 Wall keeps binary opaque blocking (A7) once graded materials exist: owner decision.
