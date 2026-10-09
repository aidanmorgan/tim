# EL-214 · Broadband beam detector — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). The sensitive face looks along local −X.

EL-214 refines the catalogue laser receiver [CAT-038 light_receiver](CAT-038-light_receiver.md). It shares the selective receivers' body and contact ([EL-146](EL-146-red-selective-receiver.md)) but integrates all channels by their mean, with no purity test. Appearance (the eased disc) does not establish detection; the numeric reading does.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-214 |
| Name | Broadband beam detector |
| Type | Optical |
| Anchor | [requirements.md#element-214](../requirements.md#element-214); named entry [named-elements.md#element-214](../invest/named-elements.md#element-214); scope index [todo-215](../requirements.md#todo-215) |
| Owner | S497 |
| Refines CAT | [CAT-038 light_receiver](CAT-038-light_receiver.md) (`light_receiver`, "Laser receiver", OpticalChannel=Broadband). One catalogue part satisfies both identities; not a second implementation. |
| Related | EL-146 to EL-152 selective receivers; EL-152 white (each-channel rule, contrast); EL-153 general-light receiver (cone light); CAT-036 laser |
| Roadmap story | 13.2 Broadband & Pure Channel Optical Receivers (CAT-038, CAT-056, CAT-032, CAT-012) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque cream target plate 0.26 × 1.4 × 1.4 m at the origin (half-extents 0.13, 0.7, 0.7); opaque navy base 0.85 × 0.18 × 1.6 m at (0, −0.88, 0). Pick radius 1.1 m. Static `RigidBodyDeclaration`, two box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass; contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports:** optical `Main` at (−0.18, 0, 0), normal −X, finite disc radius 0.55, Absorb, front only; electrical `PowerIn` (Input) at (0, −0.6, 0.65) and `Supply` (Output) at (0, −0.6, −0.65) (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).
- **Sensors and activation:** broadband strength S = (R + G + B) ÷ 3 of the power summed over all emitters in one snapshot. Matches when S ≥ threshold; no purity requirement. While matched the contact routes `PowerIn` → `Supply`. All emitters trace before any reading commits; the electrical solve follows the documented optical tick. No hysteresis; light never creates electricity.
- **Work and energy stores:** none.
- **Parameters:** `threshold` — f32, 0.05–2 (finite; others reject before Run), default 0.25, game optical power (`ReceiverParameter.Threshold`). Channel fixed at `Broadband` (`OpticalColour`). Requirement mode: "threshold. OpticalChannel=Broadband".
- **Cosmetic curves and UI bindings:** disc eases slate `#556573` → gold `#f7cb52` over 0.125 s smoothstep from committed activity (endpoint drive) while the contact responds at the fixed tick. No channel marks (broadband).
- **Art (DESIGN.md palette):** cream plate `#fff8e9`, navy foot `#293954`, slate/gold disc r 0.55 (thickness 0.035), cream concentric ring r 0.33, gold centre sphere r 0.08, two lower gold sockets r 0.075; DESIGN.md row "Laser receiver".
- **Catalogue and inventory entry:** id `light_receiver`, title "Laser receiver", category Optics, colour (1, 0.97, 0.91), parameters `{"threshold": 0.25}`; description "The front target detects a laser beam and closes an electrical contact while lit. Connect a separate battery supply; the beam does not create electricity." Icon `ui/WorkshopIcons.cs@a6c914e:L63-L63` (current, kept).

### Variants

One. The three named negative controls (back face, insufficient power, missing supply) are proof cases.

## 3. Engine capabilities

Binding shard ([element-03.json](../../coverage/engine/element-03.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction; source-specific composition: numerical broadband sensing plus a separately supplied contact; appearance does not establish detection).

**Exists now:** static body, colliders and material (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); `PowerIn`/`Supply` sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`).

**Missing**
- OpticalTransport Absorb aperture with snapshot readings — Story 13.2 (emission Story 13.1). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485, S486, S488.
- ElectricalPower switched contact — Story 8.1 plus 13.2; S257.
- OpticalAbsorption/SensibleHeat — S489/S543, mode-specific.
- `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** CAT-036 laser or EL-176 to EL-178 (Story 13.1), CAT-005 battery (Story 8.1), a load (CAT-035 lamp, delivered, or CAT-051 powered gate), CAT-066 wall.

## 4. Sources and legacy

**Sources.** Row [element-214](../requirements.md#element-214): a finite sensing surface integrates supported optical channels and drives a separately supplied contact above its threshold; back face, insufficient incident power and missing electrical supply are separate negative controls. Campaign: first use 51–60; practice 71–80; reuse 110 and 142 ([campaign-reservations](../invest/campaign-reservations.md)). CAT row [current-cat-038](../requirements.md#current-cat-038): threshold 0.05–2 / default 0.25; half-power 0.35 positive and quarter-power 0.175 negative must survive canonical f32 admission.

**Legacy** (shared optical facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); receiver body facts in [EL-146](EL-146-red-selective-receiver.md#4-sources-and-legacy)):
1. Default colour Broadband; threshold binding and 0.05–2 validation; aperture; ports and contact route; receive and match — `parts/LightReceiverPart.cs@a6c914e:L11-L46`. Carry forward behaviour; not the per-part callback.
2. Broadband strength = channel mean; broadband accepts without purity — `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Indicator and art — `parts/LightReceiverPart.cs@a6c914e:L22-L27`, `parts/LightReceiverPart.cs@a6c914e:L47-L59`. Carry forward behaviour; not Godot materials.
4. Catalogue entry and scene (no Colour override, so Broadband) — `parts/catalog/light_receiver.tres@a6c914e:L6-L12`, `parts/scenes/light_receiver.tscn@a6c914e:L1-L4`. Carry forward the entry.
5. Acceptance: all 8 combinations of laser enable × laser supply × receiver supply — the receiver lights iff the laser emits and the load is powered iff all three; received power equals the amber laser power; supply loss clears at the next snapshot — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L47-L94`; a reversed receiver (back face) and one 1 m off-axis stay dark under any rotation — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L96-L137`; nearest absorbing target wins with no double counting — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L176-L211`. Carry forward.
6. Acceptance: contact binding rolls back with the transaction — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`; indicator across failure, pause and Reset — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`; reception uses solved poses, and disabling participation removes it — `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L165-L199`. Carry forward.
7. DESIGN.md: default 0.25 accepts one half-power amber beam but not one quarter-power branch. Carry forward (mean of half amber = 0.35; quarter = 0.175).

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/light_receiver.tres`, `parts/scenes/light_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/OpticalTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place a battery, a switch with a falling ball, an amber laser, a beam splitter and the detector on one split branch, plus a lamp; wire battery → laser and receiver `PowerIn`, switch → laser `ActivationIn`, receiver `Supply` → lamp. Run.
- **Positive:** a half-power amber branch (mean 0.35) lights the disc and lamp.
- **Negative / controls (independent):** (a) back face — the detector rotated 180° stays dark; (b) insufficient incident power — a quarter-power branch (mean 0.175, after two splits) stays below 0.25; (c) missing supply — receiver `PowerIn` unwired: disc lights, lamp off. Also an occluder and an off-axis miss.
- **Boundaries:** 0.35 positive and 0.175 negative after f32 admission; threshold edits 0.05 and 2; finite disc edge 0.55 m; nearest target absorbs.
- **Run/Reset:** Reset clears reading, contact and indicator. **Save/Load:** threshold, placement and wiring round-trip.
- **Integrations:** practice 71–80, reuse 110 and 142; gates, mirrors, combiner outputs. Story 13.2 suite `tools/e2e/cat-receivers-primary.test.ts`. Binding criteria: [element-214](../requirements.md#element-214), [current-cat-038](../requirements.md#current-cat-038).

## 6. Open questions

1. Does "integrates supported optical channels" mean the channel mean (legacy) or the channel sum? The mean is presumed binding (CAT-038 row values match it).
2. Should the detector also respond to torch cones? Legacy: laser only; cone sensing is EL-153 (CAT-029 question 3).
3. No hysteresis (as EL-146 question 2); owner S486.
4. Confirm CAT-038 alone satisfies EL-214.
