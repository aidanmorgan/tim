# EL-051 · Acoustic dish — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared acoustic facts A1–A13 are in [EL-044](EL-044-tone-selective-sound-meter.md#shared-acoustic-family-facts).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-051 · Acoustic dish |
| Type | Sound |
| Anchor | [requirements.md#element-051](../requirements.md#element-051); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-051); owner S538 |
| Related | Refines no CAT spec. Redirecting sibling of [EL-050 Acoustic screen](EL-050-acoustic-screen.md) (Reflecting variant); listener is a [CAT-060](CAT-060-sound_meter.md)/[EL-044](EL-044-tone-selective-sound-meter.md) meter or an [EL-046](EL-046-listening-horn.md) horn; the optical analogue is the [CAT-041 Mirror](CAT-041-mirror.md). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root body: collider box 0.3 × 1.5 × 1.5 m at the origin bounding a shallow parabolic dish of aperture radius 0.75 m, concave face toward local +X **proposed** (current collider kinds are sphere, box and plane, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`; 1.5 m matches a Speaker's height). Navy post and foot 0.6 × 0.14 × 0.6 m at (−0.1, −0.82, 0) **proposed**. Dish centre point at local (0.1, 0, 0) **proposed**. |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`); opaque for direct sound (A7). |
| Constraints | none. |
| Typed ports | none — passive geometry. |
| Sensors and activation | none. |
| Work and energy stores | none. Redirect rule **proposed**: an admitted pulse (A5) whose straight path reaches the concave face inside the aperture disc and arrives within ±20° of the dish axis produces one child pulse from the dish centre along +X, same tone, cone half-angle 10°. The child carries the source strength s × `efficiency` and keeps the source's path bookkeeping: at a listener reached after d₂ metres beyond the dish, with d₁ metres from source to dish, its level is s·0.8 / (1 + 0.08·(d₁ + d₂)²), its arrival time uses d₁ + d₂ at 12 m/s, and it expires once d₁ + d₂ exceeds 8 m (A2, A5). The attenuation is applied once over the total path, never compounded per leg. Real coverage: arrivals that miss the disc, come from outside ±20° or hit the back produce nothing. |
| Parameters | `efficiency` fixed 0.8 **proposed** (a smooth dish loses some energy; < 1 so no gain). Acceptance ±20° **proposed** (wider than a listener's tolerance but clearly directional). Output cone 10° half-angle **proposed** (narrower than the 35° speaker cone, A2: the dish "focuses" by direction, not by strength). |
| Cosmetic curves and UI bindings | Static dish; a brief gold glint at the centre ← committed redirect occurrence and forward rings from the dish (speaker-style, [DESIGN.md Sound speaker](../../../DESIGN.md#sound-speaker)) **proposed**. A faint aiming line along the axis while selected **proposed** (helps alignment without numeric controls). |
| Art | Cream `#fff8e9` concave dish with a gold `#e8b764` rim and a small navy `#293954` centre boss; navy post and foot. Geometry **proposed**. |
| Catalogue / inventory | Id `sound_dish`, Title "Sound dish", Category Sound **proposed**. Description **proposed**: "Turns sound arriving at its face into a narrow beam along its axis. Aim it carefully: off-axis sound or a missed listener gets nothing, and it never makes sound louder." |

**No-gain check.** With the total-path rule the dish reading is exactly 0.8 × the direct reading at the same total path, for every geometry. Worked case (speaker strength 1, d₁ = 2 m, d₂ = 3 m): direct at 5 m = 1/(1 + 0.08·25) = 0.333; via the dish = 0.8/3 = 0.267 (≥ the default 0.25 meter threshold, so it still triggers). A per-leg rule (incident level 1/1.32 = 0.758, × 0.8, then ÷ (1 + 0.08·9) = 1.72) would give 0.352 > 0.333 and is rejected.

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S538): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction).

**Exists now**
- Static box body and rotation gizmo placement: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`.

**Missing**
- AcousticPropagation and reception — Stories 14.1–14.3; [S528](../invest/decisions.md#s528).
- Redirecting surface producing child occurrences that carry the source's accumulated path length — S528/S529 (same mechanism as the EL-050 Reflecting variant).

**Dependencies.** A sound source, a meter or horn listener, and the rotation gizmo for aim.

## 4. Sources and legacy

- Requirement row [element-051](../requirements.md#element-051): "Curved surface redirects acoustic energy through real coverage"; outcome "Misalignment misses the listener and cannot amplify total energy".
- Named entry [element-051](../invest/named-elements.md#element-051), owner S538; component research row EL-050/051 "Occluder / redirector with explicit attenuation … no diffraction/interference claim" ([component research](../../component-research.md#sound)).
- Legacy: no dish. Reused facts: cone sampling and attenuation (A5, `engine/Acoustics.cs@a6c914e:L36-L46`), opaque blocking (A6, A7). The legacy optical mirror is harvested by [CAT-041](CAT-041-mirror.md) and is not acoustic.
- **Files harvested:** acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-051](../requirements.md#element-051), [acoustic profile](../invest/profiles.md#acoustic).

- **Construction (actual Chrome UI).** Battery → Speaker aimed at the dish 2 m away; a Wall blocks the speaker's direct line to a supplied Sound meter placed 3 m along the dish axis; aim the dish with the rotate gizmo.
- **Positive.** Aligned: the meter reads 0.267 and triggers via the dish (one redirect per pulse).
- **Negative / control.** Rotate the dish 25° away: the beam misses the meter and it reads 0 (outcome); speaker aimed at the dish back or outside ±20°: nothing; with the Wall removed and the meter moved to 5 m on the direct line it reads 0.333, above the 0.267 via the dish (no amplification).
- **Boundaries.** Arrival at 19° and 21°; meter at 9° and 11° off axis; total path d₁ + d₂ just under and over 8 m.
- **Run/Reset.** Reset clears in-flight and child pulses.
- **Save/Load.** Pose round-trips.
- **Integrations.** Acoustic integration task [sequence-task-407](../requirements.md#sequence-task-407) ("Screens/dishes … each retain their later individual contract"); interaction process [IX-12 acoustic propagation](../requirements.md#interaction-12) (path, attenuation, occlusion). Campaign: "reflecting dish" is named in the sound row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 71–80 (reuse 81–100, 117–125, 136–150).

## 6. Open questions

1. **Focus geometry.** Axis-beam redirect (proposed) versus a true focal-point model where a listener at the focus collects off-dish arrivals: owner decision.
2. **Dish versus reflecting screen.** Whether EL-050's Reflecting variant and EL-051 share one surface material: owner decision.
