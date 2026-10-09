# EL-155 · Rope-operated light — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths in metres; local +X is the beam direction, local −Y the pull direction.

No legacy part implements this identity (the campaign calls it the "drawstring source"). Its light reuses the legacy torch cone and its cord reuses the legacy rope connection; every other value is **proposed** with a justification.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-155 |
| Name | Rope-operated light |
| Type | Optical |
| Anchor | [requirements.md#element-155](../requirements.md#element-155); named entry [named-elements.md#element-155](../invest/named-elements.md#element-155); scope index [todo-196](../requirements.md#todo-196) |
| Owner | S517 |
| Refines CAT | none. Combines the cone source of [CAT-029 flashlight](CAT-029-flashlight.md) with the rope connection of [CAT-058 rope_anchor](CAT-058-rope_anchor.md) (rope facts R1–R19) and the supply of [CAT-005 battery](CAT-005-battery.md). |
| Related | EL-210 flashlight (self-triggered torch); EL-205 rope; CAT-067 weight and CAT-053 pulley (pull sources); EL-153 and EL-154 (receivers) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

- **Bodies and shapes** (all **proposed**, sized like the torch so it reads as a light, not a winch):
  - Housing: static body, opaque box 0.9 × 0.6 × 0.6 m at the origin; lens collar box 0.2 × 0.8 × 0.8 m at (0.5, 0, 0); navy foot 0.6 × 0.12 × 0.8 m at (0.2, −0.42, 0), spanning x −0.1 to 0.5, so the cord exit at (−0.25, −0.3, 0), the bead and its whole pull path hang clear of the foot.
  - Pull handle: dynamic sphere radius 0.06 m, mass 0.05 kg, resting at (−0.25, −0.45, 0) (a small bead the rope tugs; light enough not to dominate any weight).
  - Types: `RigidBodyDeclaration` (static housing, dynamic handle) and box/sphere `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); handle mass within the dynamic bound.
- **Mass and material:** housing static; handle 0.05 kg (**proposed**), restitution 0.1, friction 0.3 (**proposed**: a soft toggle bead that does not bounce off its stop), via `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints** (**proposed**): handle on a prismatic joint along −Y, travel 0–0.10 m with hard stops; return spring force 1.0 N preload + 15 N/m × travel (2.5 N at full travel). At rest the 1.0 N preload holds the bead's own weight (0.05 kg × 9.81 = 0.49 N) with 0.51 N to spare. The lightest legacy weight (0.25 kg, 2.45 N) plus the bead's weight pulls with 2.94 N, which reaches the 0.10 m stop with 0.44 N to spare; a slack cord lets the bead return.
- **Typed ports:**
  - Rope `Tie` (domain Rope, bidirectional) on the handle at its centre, attachment role Load (accepts one rope end) (**proposed** placement; socket, domain and role are legacy enums — `engine/MachineData.cs@a6c914e:L68-L68`, `engine/ConnectionPort.cs@a6c914e:L11-L11`).
  - Electrical `PowerIn` (Input) at (−0.45, 0, 0) (**proposed**: rear, as the laser's supply socket).
  - Optical emitter at lens (0.6, 0, 0), direction +X.
- **Sensors and activation:** a displacement switch on the handle travel: closes at ≥ 0.06 m and opens below 0.04 m (**proposed**: hysteresis so a swinging weight does not flicker it). Momentary: no latch.
- **Work and energy stores:** none in the part; light needs `PowerIn` supplied **and** the switch closed. Rope work only moves the handle against the spring; it never powers the light.
- **Optical emitter:** cone range 8, half-angle 15° (cosine 0.9659258), intensity 24 game units (**proposed**: equal to the legacy torch so EL-153, EL-154 and solar thresholds apply unchanged).
- **Parameters:** none authorable (**proposed**: the single behaviour is the cord pull; travel and spring stay fixed for teaching).
- **Cosmetic curves and UI bindings:** lens slate `#556573` → warm cream `#fff0a5` over 0.06 s linear on committed emission (**proposed**: the torch's legacy curve); the cone presentation reuses the torch's four nested shells and never supplies optical authority; the handle and cord render from solved poses.
- **Art (DESIGN.md palette):** cream housing `#fff8e9`, gold lens collar `#f7cb52`, navy foot `#293954`, gold pull bead `#f7cb52`, dark cord as rope artwork (**proposed**: lamp-like cream body distinct from the gold torch).
- **Catalogue and inventory entry:** id `pull_cord_light`, title "Pull-cord light", category Optics (**proposed**). Description (**proposed**): "Pulling its cord switches on a light. Tie a rope to the gold bead and connect a separate supply; a slack cord turns it off."

### Variants

One. No variants are named. A latching (pull-on, pull-off) cord is not in the row; see Open questions.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, OpticalTransport, RigidBodyDynamics, SignalPropagation, StateTransaction, TensionTransmission ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now:** static and dynamic rigid bodies, sphere and box colliders, contact material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); `PowerIn` socket (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).

**Missing**
- TensionTransmission and the rope domain, `Tie` socket and route constraint — Story 10.2 (CAT-053, CAT-058); S257 rope-port row → S688.
- JointConstraint prismatic slider with stops and spring — exists for the springboard design ([docs/gpu-f32-physics.md](../../gpu-f32-physics.md#6-unified-compliant-soft-constraints-spring-damper-formulation), CAT-062a) but not yet as a declaration for this part.
- OpticalTransport cone emission — Story 13.1; S484 ([decisions](../invest/decisions.md#s484)): S486, S488.
- ElectricalPower supply gating — Story 8.1; S257.
- `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-058 rope anchor / EL-205 rope and CAT-067 weight or CAT-053 pulley (Epic 10), CAT-005 battery (Story 8.1), EL-153 or CAT-059 as an observer (Epic 13).

## 4. Sources and legacy

**Sources.** Row [element-155](../requirements.md#element-155): real rope displacement actuates a separately supplied light source; slack rope or missing supply produces no light. [todo-196](../requirements.md#todo-196) scope index. Campaign: "drawstring source" first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage); [campaign-reservations](../invest/campaign-reservations.md)). S257 rope-port: a slack rope lifts nothing.

**Legacy** (no part; mechanisms only):
1. Torch cone values range 8, cosine 0.9659258, intensity 24; lens colour curve 0.06 s linear — `parts/FlashlightPart.cs@a6c914e:L10-L19`. Carry forward as the proposed emitter.
2. Cone light law — `engine/LightNetwork.cs@a6c914e:L12-L52`. Carry forward the law; not the CPU solve.
3. Rope domain and attachment roles are closed enums — `engine/MachineData.cs@a6c914e:L68-L68`, `engine/ConnectionPort.cs@a6c914e:L11-L12`; loads accept one rope end; ropes cannot branch — `engine/RopeNetwork.cs@a6c914e:L69-L78`. Carry forward.
4. Rope route, length-at-connect, tension-only and slack/taut facts — recorded as R1–R19 in [CAT-058-rope_anchor](CAT-058-rope_anchor.md). Carry forward via that spec.
5. Lightest legacy weight 0.25 kg — `parts/WeightPart.cs@a6c914e:L22-L27` (recorded in [CAT-067-weight](CAT-067-weight.md)). Carry forward as the sizing basis for the proposed spring.

**Files harvested:** `parts/FlashlightPart.cs`, `engine/LightNetwork.cs`, `engine/MachineData.cs`, `engine/ConnectionPort.cs`, `engine/RopeNetwork.cs`, `parts/WeightPart.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place the pull-cord light, a rope anchor or pulley above a ramp, a weight, a battery and a solar panel or light sensor in the beam; connect a rope from the weight to the bead with the rope tool, and battery `Supply` → light `PowerIn`. Run; the weight rolls off and pulls the cord.
- **Positive:** the cord goes taut, the bead travels past 0.06 m, the lens and cone light, and the receiver responds.
- **Negative / controls:** slack rope (weight resting) — no light; supply unwired — bead moves, no light; rope tied to the housing instead of the bead is refused (no Tie socket there); an occluder in the cone.
- **Boundaries:** switch at 0.06 m on and 0.04 m off; travel stop at 0.10 m; a 0.25 kg weight (2.94 N with the bead) reaches the stop against the 2.5 N full-travel spring; at rest the bead stays up against its own weight; release returns the bead and extinguishes the light.
- **Run/Reset:** Reset returns the bead to rest, clears emission and rope state exactly. **Save/Load:** placement, rope connection (with length) and wiring round-trip.
- **Integrations:** EL-153, EL-154, CAT-059 solar; pulley routes. Binding criteria: [element-155](../requirements.md#element-155).

## 6. Open questions

1. Momentary (proposed) or latching pull-cord behaviour? The row says "rope displacement actuates"; owner decision.
2. All geometry, mass, spring, switch and emitter values are proposed; owner to confirm.
3. Should the emitter be a cone (proposed) or a narrow beam usable by laser receivers? Owner S484.
4. Roadmap: unscheduled; it needs Epic 10 ropes and Epic 13 cones, so it fits after Story 13.7.
