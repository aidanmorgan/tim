# EL-210 · Flashlight — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). Local +X is the beam direction.

EL-210 refines the catalogue torch [CAT-029 flashlight](CAT-029-flashlight.md), whose spec holds the full legacy harvest. This file restates the declaration and adds what the named identity requires: a *supplied* source where no supply means no emission. Legacy and the CAT-029 row describe a self-contained, unbounded battery with no store and no parameters, so how EL-210's supply is modelled is unspecified — owner decision (Open question 1). No value is invented for it here.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-210 |
| Name | Flashlight |
| Type | Optical |
| Anchor | [requirements.md#element-210](../requirements.md#element-210); named entry [named-elements.md#element-210](../invest/named-elements.md#element-210); source record [campaign-element-coverage](../requirements.md#campaign-element-coverage); scope index [todo-196](../requirements.md#todo-196) |
| Owner | S495 |
| Refines CAT | [CAT-029 flashlight](CAT-029-flashlight.md) (`flashlight`, "Flashlight"). One catalogue part satisfies both identities; not a second implementation. |
| Related | EL-153 general-light receiver, EL-154 light-charge receiver, EL-211 / CAT-059 solar panel (targets); EL-155 rope-operated light (rope-switched sibling); CAT-036 laser (narrow-ray contrast) |
| Roadmap story | 13.1 Flashlight Torch & Collimated Laser Emitters (CAT-029, CAT-036) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body with three opaque boxes: body 1.0 × 0.6 × 0.6 m at (−0.1, 0, 0); lens collar 0.25 × 0.86 × 0.86 m at (0.5, 0, 0) (half-extents 0.125, 0.43, 0.43); top button 0.4 × 0.14 × 0.35 m at (−0.15, 0.36, 0). Base plate 1.2 × 0.12 × 0.85 m at (0, −0.38, 0) is artwork only. Lens point (0.66, 0, 0); pick radius 0.9 m. Static `RigidBodyDeclaration`, box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass (kinematic only under placement assistance at precision 0); contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`).
- **Constraints and joints:** none.
- **Typed ports:** `ActivationIn` (Activation, Input) (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`); optical cone emitter at the lens, +X. No electrical port in legacy or CAT-029 (self-contained supply); whether EL-210 adds one is Open question 1.
- **Sensors and activation:** a contact trigger on the top button latches the torch on when approach speed ≥ the level's assistance trigger threshold and the local contact point has y > 0.3, |x + 0.15| < 0.45, |z| < 0.4; an activation command also latches it. On stays latched until Reset. Current types: `ContactTriggerDeclaration` and `ActivationNodeKind.ContactSource`/`Latch` (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L17`).
- **Work and energy stores:** none in legacy or CAT-029 (the built-in battery is unbounded). The finite supply that EL-210's "no supply means no emission" implies is unspecified — owner decision (Open question 1).
- **Optical emitter:** cone, range 8, half-angle 15° (cosine 0.9659258), intensity 24 game units; no reflection, refraction or ambient power.
- **Parameters:** none (legacy catalogue `Parameters = {}`; CAT-029 row: no enumerated selector, audit configuration rather than infer none).
- **Cosmetic curves and UI bindings:** button translates −0.06 on Y over 0.06 s linear (rest 0.36 → 0.30) and lens slate `#556573` → `#fff0a5` over 0.06 s linear, both on committed owner activity (endpoint drive); a translucent warm-cream cone of four nested shells × 48 directions clips against shared collision proxies and is presentation only.
- **Art (DESIGN.md palette):** body `#f5b354` (cylinder r 0.3, length 1.0), cream collar `#fff8e9`, slate lens, gold button `#f7cb52`, navy base `#293954`; DESIGN.md row "Light and solar".
- **Catalogue and inventory entry:** id `flashlight`, title "Flashlight", category Power, colour (0.96, 0.70, 0.33); description "Self-contained battery light: press its top button with a falling object or send an activation command. Its beam points out of the cream lens." Icon `ui/WorkshopIcons.cs@a6c914e:L93-L93` (current, kept). Legacy levels `solar_motor`, `solar_shadow`, `delayed_solar` place it locked.

### Variants

One element with two activation modes, each proved separately: (1) button contact by a falling body; (2) activation command over a wire (as in `delayed_solar`). No other variant is named.

## 3. Engine capabilities

Binding shard ([element-03.json](../../coverage/engine/element-03.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction; source-specific composition: add a finite supplied source declaration; inherited absorption/heat membership must not turn the flashlight into an unrelated heat gate).

**Exists now:** static body and colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`); contact trigger and activation latch (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L17`); cosmetic feedback from activation (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`).

**Missing**
- OpticalTransport cone emission with shared-proxy occlusion and committed readings — Story 13.1. Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S486, S488; refinements S522/S523.
- The finite supplied source declaration the map row names (FiniteLedger store or ElectricalPower input) — pending Open question 1; Story 13.1 once decided.
- Cone presentation shells — Story 13.1 (render-only).
- `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** a falling ball (delivered) or CAT-063 switch / CAT-022 delay (delivered) for activation; CAT-059 solar panel or EL-153 as observer (Epic 13); CAT-066 wall (delivered); CAT-005 battery (Story 8.1) only if Open question 1 chooses an external supply.

## 4. Sources and legacy

**Sources.** Row [element-210](../requirements.md#element-210): a supplied finite-aperture source emits a widening optical cone; occluding geometry blocks coverage and no supply means no emission. CAT row [current-cat-029](../requirements.md#current-cat-029): self-contained torch, 8-unit range, 15° half-angle, nine solar samples, four nested 48-direction shells; off, rotated, shadow and missing-wire controls. Campaign: flashlight first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (full harvest in [CAT-029-flashlight](CAT-029-flashlight.md#legacy-harvest); key facts):
1. Lens (0.66, 0, 0), range 8, cone cosine 0.9659258, intensity 24 — `parts/FlashlightPart.cs@a6c914e:L10-L13`. Carry forward.
2. Button and lens curves; light source only while active — `parts/FlashlightPart.cs@a6c914e:L18-L27`. Carry forward.
3. Geometry and colours — `parts/FlashlightPart.cs@a6c914e:L28-L44`. Carry forward.
4. Button contact predicate — `parts/FlashlightPart.cs@a6c914e:L45-L52`. Carry forward the predicate; not the per-part callback.
5. Cone law — `engine/LightNetwork.cs@a6c914e:L12-L52`. Carry forward; not the CPU solve.
6. Catalogue entry (description "Self-contained battery light") — `parts/catalog/flashlight.tres@a6c914e:L8-L14`. Carry forward.
7. Acceptance: button strike at 0° and 37° roll activates; underside strike and disabled collider do not — `CuriousContraptions.tests/FlashlightContactTests.cs@a6c914e:L19-L60`; reversed or out-of-range panel reads 0 — `CuriousContraptions.tests/LightTests.cs@a6c914e:L116-L166`; invalid cone declarations reject before Run — `CuriousContraptions.tests/LightConeBindingTests.cs@a6c914e:L11-L64`. Carry forward.
8. The torch's built-in supply is unbounded: the light source exists whenever the torch is active, with no supply check — `parts/FlashlightPart.cs@a6c914e:L26-L27`; the paired solar panel likewise has no storage or voltage model — `parts/SolarPanelPart.cs@a6c914e:L8-L9`. Disposition pending Open question 1 (it conflicts with EL-210's "no supply means no emission").
9. Lessons: torch locked at (2, 3, 0) facing −X with the trigger ball above the button; assistance thresholds 0.2 / 0.47 / 0.8 — `content/puzzles.json@a6c914e:L5944-L6225`, `content/puzzles.json@a6c914e:L5955-L5998`. Carry forward for Epic 15.

**Files harvested:** `parts/FlashlightPart.cs`, `parts/catalog/flashlight.tres`, `engine/LightNetwork.cs`, `parts/SolarPanelPart.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/FlashlightContactTests.cs`, `CuriousContraptions.tests/LightTests.cs`, `CuriousContraptions.tests/LightConeBindingTests.cs`, `content/puzzles.json`.

## 5. Acceptance outline

- **Chrome UI recipe:** in the actual Workshop place a flashlight, a ball above its button, a solar panel facing the lens 3 m away and a motor; wire panel `Supply` → motor `PowerIn`. Run. Mode 2: wire switch → delay → torch `ActivationIn`.
- **Positive:** the ball presses the button; lens lights, cone appears, panel meter fills, motor turns.
- **Negative / controls:** (named) a wall between torch and panel blocks coverage; (named) "no supply means no emission" — **pending Open question 1**: the control cannot be built until the supply model is chosen (an exhausted internal charge, or an unwired external `PowerIn`); also underside strike, rotated torch, panel beyond 8 m or outside 15°.
- **Boundaries:** cone edge 15°; range 8; partial shadow reduces rather than erases.
- **Run/Reset:** Reset clears the latch, cone, button and lens exactly. **Save/Load:** placement and wiring round-trip.
- **Integrations:** CAT-059, EL-153, EL-154; Story 13.1 suite `tools/e2e/cat-029-036.test.ts`. Binding criteria: [element-210](../requirements.md#element-210), [current-cat-029](../requirements.md#current-cat-029).

## 6. Open questions

1. Supply model. EL-210 says "supplied … no supply means no emission"; CAT-029 and legacy say "self-contained" with no store and no parameters. Options: (a) a finite internal charge (a FiniteLedger store, emission stops when it is exhausted, restored by Reset, possibly with an authored amount); (b) an external electrical `PowerIn` like the laser, with emission requiring supply (ElectricalPower, Story 8.1). Unspecified — owner decision (also CAT-029 question 4). The "no supply" negative control is pending this decision.
2. Story 13.1 says "35° cone"; the requirement rows and legacy say 15° half-angle (CAT-029 question 1).
3. Intensity 24 and max(1, d²) falloff are legacy only (CAT-029 question 2).
4. Confirm CAT-029 alone satisfies EL-210.
