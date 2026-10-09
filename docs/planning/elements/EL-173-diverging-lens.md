# EL-173 · Diverging lens — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths in metres; the lens axis is local X and the lens works from either face.

No legacy part implements a lens; legacy optics were narrow rays plus a separate cone network, with no refraction ("Flashlight cones do not reflect or refract", [sequence-task-396](../requirements.md#sequence-task-396)). The frame reuses the legacy filter frame; every other value is **proposed** with a justification.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-173 |
| Name | Diverging lens |
| Type | Optical |
| Anchor | [requirements.md#element-173](../requirements.md#element-173); named entry [named-elements.md#element-173](../invest/named-elements.md#element-173); source record [todo-305](../requirements.md#todo-305) |
| Owner | S519 (refinement S524 divergent-lens) |
| Refines CAT | none. Frame shared with [CAT-055 red_filter](CAT-055-red_filter.md) family; finite-width output is read like the cone of [CAT-029 flashlight](CAT-029-flashlight.md). |
| Related | TH-06 converging lens (focus; separate thermal identity, scope index [todo-304](../requirements.md#todo-304)); EL-174 prism; EL-214 broadband detector and EL-153 general-light receiver (targets) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body (**proposed**: the legacy filter frame, so lenses and filters read as one optical family): opaque bars 0.18 × 0.18 × 1.65 m at (0, ±0.74, 0) and 0.18 × 1.3 × 0.18 m at (0, 0, ±0.74); base 0.9 × 0.2 × 1.65 m at (0, −1, 0); lens body collider 0.12 × 1.3 × 1.3 m at the origin that collides with bodies but does not occlude light (**proposed** 0.12 m thickness: visibly thicker at the rim than a 0.03 m filter pane).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports:** optical aperture `Main`, centre (0, 0, 0), normal −X, finite disc radius 0.65 (**proposed**: the filter and mirror aperture), interaction Diverge (**proposed** new member of the closed `OpticalInteraction` enum), two-sided. No electrical or activation ports.
- **Sensors and activation:** none.
- **Optical law (all proposed):**
  - Transmission retention 0.95 per channel (**proposed**: the mirror's legacy 0.95, so passive optics share one loss). Transmitted power P_t = 0.95 × P_in, conserved across the whole output footprint.
  - A narrow ray hitting the disc leaves as a cone from the hit point along the incident direction with half-angle 15° (**proposed**: the torch's legacy cone angle, so finite-width readers and visuals are shared).
  - A receiver aperture at distance d intercepts P_t × (intercepted area ÷ footprint area), footprint radius d × tan 15°. The sum over all receivers never exceeds P_t; partial occlusion removes the occluded share.
  - Remaining range and the interaction budget continue from the hit point (legacy rule).
- **Work and energy stores:** none; it never amplifies.
- **Parameters:** none authorable (**proposed**: a fixed spread keeps the teaching contrast with the converging lens).
- **Cosmetic curves and UI bindings:** no animation. Selected-only faint preview of the outgoing cone in idle construction, never activating receivers (**proposed**: the legacy preview convention for routing optics). Running output renders as a widening translucent beam whose opacity falls with footprint area.
- **Art (DESIGN.md palette):** cream frame `#fff8e9`, navy foot `#293954`, translucent cyan lens `#66b8c9` at alpha 0.3 with a thin ochre rim `#e8b764` and two inward-curving cream marks showing concavity (**proposed**: cyan is the established optic-face colour of the mirror and combiner).
- **Catalogue and inventory entry:** id `diverging_lens`, title "Diverging lens", category Optics (**proposed**). Description (**proposed**): "Spreads a beam into a wider cone. Several targets can share it, but each gets only part of the light."

### Variants

One. The row names no variants; the converging lens is a separate thermal identity.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static box body and colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).

**Missing**
- Finite-width beam coverage (narrow ray → cone with area-shared power) — no story; [todo-309](../requirements.md#todo-309) requires it before any lens. Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485 finite-colour allocation across receivers, S488 interval law with partial occlusion; refinement S524.
- Light-transparent solid collider — Story 13.4 (filters).
- OpticalAbsorption/SensibleHeat for the 5% loss — S489/S543, mode-specific.
- `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-036 laser or EL-176 to EL-178 (Story 13.1), CAT-005 battery (Story 8.1), two receivers (CAT-038 / EL-214, Story 13.2), CAT-066 wall.

## 4. Sources and legacy

**Sources.** Row [element-173](../requirements.md#element-173): finite-aperture refraction broadens illumination while conserving transmitted power; two receivers share the beam and neither obtains the unsplit full power. [todo-305](../requirements.md#todo-305): cover two low-threshold detectors with finite-width coverage and partial occlusion. Refinement S524 **divergent-lens** ([refinements](../invest/refinements.md)). [todo-309](../requirements.md#todo-309): splitting and focusing cannot create power; finite-width coverage first. Campaign: converging/diverging lens first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (no lens; mechanisms only):
1. Filter frame geometry and transparent pane collider — `parts/ColourFilterPart.cs@a6c914e:L20-L29`. Carry forward as the proposed frame.
2. Two-sided apertures and continued range — `engine/OpticalNetwork.cs@a6c914e:L94-L98`, `engine/OpticalNetwork.cs@a6c914e:L113-L127`. Carry forward.
3. Mirror retention 0.95 and the 16-interaction budget — `engine/OpticalNetwork.cs@a6c914e:L31-L33`. Carry forward as the proposed loss and budget.
4. Cone geometry (cosine 0.9659258) and the distance law — `parts/FlashlightPart.cs@a6c914e:L10-L13`, `engine/LightNetwork.cs@a6c914e:L12-L52`. Carry forward as the shape basis; the area-share law is new.

**Files harvested:** `parts/ColourFilterPart.cs`, `engine/OpticalNetwork.cs`, `parts/FlashlightPart.cs`, `engine/LightNetwork.cs`, `reference/cpu/MachinePart.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a battery, a triggered amber laser, the lens 2 m along the beam and two broadband receivers side by side 4 m beyond it, their centres ±0.8 m either side of the beam axis, both set to threshold 0.10 with the threshold control; wire each receiver to its own lamp. Run.
- **Positive:** both receivers light. The footprint radius at 4 m is 4 × tan 15° ≈ 1.07 m, and each 0.55 m disc intercepts ≈ 20% of it, so each reads ≈ 0.20 × 0.95 × the laser power (mean ≈ 0.13), far below the unsplit 0.7.
- **Negative / controls:** with the default 0.25 thresholds neither lights (spread power is too weak); removing only the lens leaves both off-axis receivers dark (each nearest aperture edge is 0.25 m from the narrow beam axis); as a separate direct-beam control, use the placement gizmo to centre one receiver on the axis and verify it receives the unsplit beam while the other stays dark; an occluder over half the footprint reduces only the covered share; a ray outside the 0.65 m disc hits the frame.
- **Boundaries:** sum of all receptions ≤ 0.95 × input; footprint growth with distance; range budget; interaction cap.
- **Run/Reset:** Reset clears beams and readings. **Save/Load:** placement and orientation round-trip.
- **Integrations:** EL-174 prism downstream, mirrors upstream. Binding criteria: [element-173](../requirements.md#element-173).

## 6. Open questions

1. The finite-width model (cone from the hit point with area-shared power) is proposed; S488 must adopt the interval/coverage law first.
2. Fixed 15° spread and 0.95 retention are proposed; should spread be authorable?
3. Does a diverging lens also act on torch cones (widen further), or only on narrow rays? Proposed: narrow rays only.
4. Roadmap: unscheduled; it depends on the finite-width capability, which no story builds yet.
