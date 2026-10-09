# EL-174 · Prism — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths in metres; light enters along local +X and the three channel paths fan out in the local XY plane.

No legacy part implements a prism. It reuses the legacy routing mechanism (an input aperture routed to a directed outlet, counting internal travel) and the legacy channel masks; every other value is **proposed** with a justification. The dispersion is an explicit simplified spectral rule, not physical refraction.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-174 |
| Name | Prism |
| Type | Optical |
| Anchor | [requirements.md#element-174](../requirements.md#element-174); named entry [named-elements.md#element-174](../invest/named-elements.md#element-174); source record [todo-306](../requirements.md#todo-306) |
| Owner | S520 (refinement S525 prism) |
| Refines CAT | none. Inverse of [CAT-006 beam_combiner](CAT-006-beam_combiner.md) / EL-213; channel rule shared with the filters ([CAT-055](CAT-055-red_filter.md), EL-143 to EL-145). |
| Related | EL-213 combiner (merges); EL-143 to EL-145 filters (absorb instead of separate); EL-146 to EL-148 receivers (targets); EL-173 diverging lens |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: a triangular glass block, equilateral cross-section of side 1.0 m in the XY plane and depth 1.0 m along Z (**proposed**: the size of the 1.0 m torch body, large enough to aim at); collision proxy one box 0.87 × 1.0 × 1.0 m at the origin (**proposed**: no wedge collider exists, so the bounding box stands in); the glass collides with bodies but does not occlude light. Navy foot 1.2 × 0.12 × 1.2 m at (0, −0.56, 0) (**proposed**: the base style of the other optics).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports (optical):**
  - Input aperture `Main` at (−0.29, 0, 0), normal −X, finite disc radius 0.43, front only (**proposed**: the combiner and gate port radius).
  - Three directed outlets from (0.29, 0, 0): red at −20° about Z from +X, green along +X, blue at +20° (**proposed**: at ≥ 5 m beyond the exit, adjacent paths are ≥ 5 × tan 20° ≈ 1.82 m apart, so each 0.55 m receiver disc can be hit alone).
  - Interaction Disperse (**proposed** new member of the closed `OpticalInteraction` enum), needing multiple outlets per part. No electrical or activation ports.
- **Sensors and activation:** none.
- **Optical law (proposed):** for incident power (R, G, B), the red outlet emits (0.9 R, 0, 0), green (0, 0.9 G, 0), blue (0, 0, 0.9 B); a zero channel emits no path. Retention 0.9 (**proposed**: the legacy combiner and gate routing retention, so every routing body loses the same 10%). Internal travel from input to outlet counts against the remaining range, and each outlet ray consumes one interaction.
- **Work and energy stores:** none; output total ≤ 0.9 × input; a missing channel is never created.
- **Parameters:** none authorable (**proposed**: fixed fan angles keep puzzles predictable).
- **Cosmetic curves and UI bindings:** selected-only faint preview of the three outgoing paths in idle construction, never activating (**proposed**: the legacy combiner/splitter preview convention). Output beams use the channel inks red `#de7058`, green `#62aa78`, blue `#5b9cdb`.
- **Art (DESIGN.md palette):** translucent cyan glass `#66b8c9` at alpha 0.3 with ochre edges `#e8b764`; navy foot `#293954`; on the exit face three small bars in the channel accents in fan order, plus one/two/three raised cream bars so order does not rely on hue (**proposed**, following the filters' bar convention).
- **Catalogue and inventory entry:** id `prism`, title "Prism", category Optics (**proposed**). Description (**proposed**): "Splits a beam into its red, green and blue parts. It cannot add a colour the beam does not already carry."

### Variants

One; no variants are named.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static box body and colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).

**Missing**
- OpticalTransport routed aperture with **multiple** directed outlets and per-outlet channel masks — no story (Story 13.5 builds single-outlet routing for the combiner). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485 finite-colour allocation, S488 interval law; refinement S525 ([refinements](../invest/refinements.md)).
- Light-transparent solid collider — Story 13.4; a wedge collider shape (only Sphere, Box, Plane exist: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`).
- OpticalAbsorption/SensibleHeat for the 10% loss — S489/S543, mode-specific.
- `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** an all-channel source (CAT-036 amber laser; Story 13.1), CAT-005 battery (Story 8.1), primary receivers EL-146 to EL-148 (Story 13.2), CAT-066 wall.

## 4. Sources and legacy

**Sources.** Row [element-174](../requirements.md#element-174): declared dispersion separates only spectral channels present in incident light; red-only input creates no green or blue output. [todo-306](../requirements.md#todo-306): an explicit simplified spectral rule; pure red must not create green or blue; each path can be deliberately blocked. Refinement S525 **prism**; teaching set-up "Separate colours with a prism" ([refinements](../invest/refinements.md)). Campaign: prism first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (no prism; mechanisms only):
1. Single directed outlet record and Route interaction: routed ray leaves the outlet with power × transmission and remaining range minus internal travel — `engine/OpticalNetwork.cs@a6c914e:L11-L13`, `engine/OpticalNetwork.cs@a6c914e:L119-L125`. Carry forward, generalised to several outlets.
2. Route requires a finite directed outlet or the declaration is rejected — `engine/OpticalNetwork.cs@a6c914e:L58-L62`. Carry forward per outlet.
3. Routing retention 0.9 and the input/outlet layout — `parts/BeamCombinerPart.cs@a6c914e:L12-L20`. Carry forward as the proposed retention.
4. Internal travel counts against range: total path = range ± 0.001 — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L91-L117`. Carry forward.
5. Channel masks are closed enums; filters never create channels — `engine/OpticalColour.cs@a6c914e:L6-L22`; `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L69-L103`. Carry forward.

**Files harvested:** `engine/OpticalNetwork.cs`, `parts/BeamCombinerPart.cs`, `engine/OpticalColour.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a battery, a triggered amber laser aimed at the prism input, and red, green and blue receivers at least 5 m beyond the exit on the three fan directions (adjacent receivers ≈ 1.82 m apart); wire each receiver to a lamp. Run.
- **Positive:** red, green and blue receivers each read 0.9 × the matching laser channel (0.9, 0.702, 0.288) and light.
- **Negative / controls:** a red-only input (EL-176, or amber through EL-143) produces only the red path — green and blue receivers stay dark; blocking one path darkens only its receiver; light on the prism's back or sides does not enter.
- **Boundaries:** output sum ≤ 0.9 × input; internal travel consumes range; interaction budget; fan angle ±20°.
- **Run/Reset:** Reset clears paths and readings. **Save/Load:** placement and orientation round-trip.
- **Integrations:** prism → combiner (EL-213) recombination; prism → white receiver negative. Binding criteria: [element-174](../requirements.md#element-174).

## 6. Open questions

1. Fan angles, retention, size and the box collision proxy are proposed; owner to confirm, and whether a wedge collider is required.
2. Does each outlet ray count as one interaction (proposed) or share the incoming ray's count?
3. Should a prism accept torch cones (white-ish light) as well as narrow rays? Proposed: narrow rays only.
4. Roadmap: unscheduled; multi-outlet routing is not built by any story.
