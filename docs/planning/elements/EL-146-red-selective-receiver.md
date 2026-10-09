# EL-146 · Red selective receiver — named-identity spec

Story 7.0 full spec for a named puzzle-element identity. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Lengths are legacy scene units, which the current engine treats as metres (`MetreVector`). The sensitive face looks along local −X.

All seven selective receivers (EL-146 to EL-152) and the broadband detector (EL-214) share one legacy body, one absorbing aperture and one switched electrical contact; they differ in the channel acceptance rule, marks and icon. Each is specified in full in its own file.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-146 |
| Name | Red selective receiver |
| Type | Optical |
| Anchor | [requirements.md#element-146](../requirements.md#element-146); named entry [named-elements.md#element-146](../invest/named-elements.md#element-146); source record [todo-300](../requirements.md#todo-300) |
| Owner | S501 |
| Refines CAT | [CAT-056 red_receiver](CAT-056-red_receiver.md) (`red_receiver`, "Red receiver"). One catalogue part satisfies both identities; not a second implementation. |
| Related | EL-147, EL-148 (sibling primary channels); EL-149 to EL-152 (mixed); EL-214 broadband; EL-143 red filter and EL-176 red laser |
| Roadmap story | 13.2 Broadband & Pure Channel Optical Receivers (CAT-038, CAT-056, CAT-032, CAT-012) |
| Status | not started |

## 2. Declaration

- **Bodies and shapes:** one static rigid body: opaque cream target plate 0.26 × 1.4 × 1.4 m at the origin; opaque navy base 0.85 × 0.18 × 1.6 m at (0, −0.88, 0). Pick radius 1.1 m. Types: static `RigidBodyDeclaration` and two box `ColliderDeclaration`s (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`).
- **Mass and material:** static, zero mass. Contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`) via `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints and joints:** none.
- **Typed ports:**
  - Optical aperture `Main` (`OpticalPortId`): centre (−0.18, 0, 0), normal −X, finite disc radius 0.55, interaction Absorb, transmission (1, 1, 1); front incidence only. The plate back and frame are opaque and never detect.
  - Electrical `PowerIn` (Electrical, Input) at (0, −0.6, 0.65) and `Supply` (Electrical, Output) at (0, −0.6, −0.65). Both sockets already exist in the current enum (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`).
- **Sensors and activation:** an optical acceptance sensor. With received power (R, G, B) summed over all emitters in one snapshot, the receiver matches when R ≥ threshold **and** R ≥ 0.9 × (R + G + B). While matched, the contact routes `PowerIn` → `Supply`; light never creates electricity. Readings commit together after all emitters trace; the electrical consequence follows the documented optical tick. No hysteresis.
- **Work and energy stores:** none.
- **Parameters:** `threshold` — f32, range 0.05–2 (finite; others reject before Run), default 0.25, unit game optical power (wire name `threshold`, enum `ReceiverParameter.Threshold`). Fixed constant: channel purity 0.9. Channel fixed at `Red` (`OpticalColour`). Requirement mode: "threshold. OpticalChannel=Red".
- **Cosmetic curves and UI bindings:** target disc eases slate `#556573` → gold `#f7cb52` over 0.125 s smoothstep, driven by committed owner activity (endpoint drive); "correct channel" (red marks) and "currently active" (gold disc) stay separate cues ([DESIGN.md](../../../DESIGN.md#colour-optics)).
- **Art (DESIGN.md palette):** cream plate `#fff8e9`; navy foot `#293954`; disc r 0.55, thickness 0.035 at (−0.18, 0, 0); cream concentric ring r 0.33 (tube 0.025) at (−0.21, 0, 0); gold centre sphere r 0.08 at (−0.23, 0, 0); one red bar mark `#de7058` at (−0.15, 0.6, 0); gold port studs r 0.075.
- **Catalogue and inventory entry:** id `red_receiver`, title "Red receiver", category Optics, colour (1, 0.97, 0.91), parameters `{"threshold": 0.25}`. Description: "Requires at least 90% red light and sufficient channel power. Switches a separate electrical supply." Icon `ui/WorkshopIcons.cs@a6c914e:L71-L71` (current, kept). No legacy level places it.

### Variants

One. The row names no variants; green and blue receivers are separate identities. The three independent negative controls (missing red, wrong channel, absent supply) are proof cases, not variants.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction ([element-map row](../general-engine-element-map.md) omits StateTransaction).

**Exists now**
- Static box body, colliders and contact material: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L60`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L91`.
- `PowerIn`/`Supply` socket identities and Electrical domain enum: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`.

**Missing**
- OpticalTransport Absorb aperture with snapshot-aggregated readings — Story 13.2 (emission Story 13.1). Decision owner **S484** ([decisions](../invest/decisions.md#s484)): S485 finite-colour allocation, S486 optical-commit, S488 interval law.
- ElectricalPower switched contact (`PowerIn` → `Supply` route gated by an optical match) — Story 8.1 network plus Story 13.2; S257.
- OpticalAbsorption and SensibleHeat (absorbed power to heat) — S489/S543, mode-specific.
- No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`).

**Element dependencies:** a red-carrying source (EL-176 red laser, or CAT-036 amber laser through EL-143 red filter; Stories 13.1/13.4), CAT-005 battery (Story 8.1), a load such as CAT-035 lamp (delivered) or CAT-051 powered gate, CAT-066 wall (delivered).

## 4. Sources and legacy

**Sources.** Row [element-146](../requirements.md#element-146): separately supplied output requires red under the declared thresholds and channel-purity contract; missing required channel, wrong channel and absent supply are independent negative controls. [todo-300](../requirements.md#todo-300): selected channel at threshold, default 0.25, and at least 90% of total RGB; switches independent supplied power. CAT row [current-cat-056](../requirements.md#current-cat-056): below threshold, backside/blocked and no-supply controls; coherent threshold indication. Refinement **optical-pairings** (S694). Campaign: each receiver channel first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S7 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest)):
1. Aperture `Main` (−0.18, 0, 0), −X, radius 0.55, Absorb — `parts/LightReceiverPart.cs@a6c914e:L21-L21`. Carry forward.
2. Threshold binding, validation 0.05–2 finite, undefined colour rejected — `parts/LightReceiverPart.cs@a6c914e:L11-L14`, `parts/LightReceiverPart.cs@a6c914e:L40-L46`; enum `ReceiverParameter { Threshold }` — `engine/MachineData.cs@a6c914e:L80-L80`. Carry forward.
3. Receive: store received RGB and match; Active = matched — `parts/LightReceiverPart.cs@a6c914e:L35-L39`. Carry forward behaviour; not the per-part callback.
4. Ports and contact route `PowerIn` → `Supply` gated by the match — `parts/LightReceiverPart.cs@a6c914e:L28-L34`. Carry forward.
5. Indicator 0.125 s smoothstep slate → gold, endpoint drive; art — `parts/LightReceiverPart.cs@a6c914e:L22-L27`, `parts/LightReceiverPart.cs@a6c914e:L47-L59`. Carry forward behaviour; not Godot materials.
6. Acceptance rule: red strength = R; accept iff strength ≥ threshold and power·mask ≥ 0.9 × total — `engine/OpticalColour.cs@a6c914e:L10-L10`, `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
7. Catalogue entry and scene (Colour = 1) — `parts/catalog/red_receiver.tres@a6c914e:L6-L12`, `parts/scenes/red_receiver.tscn@a6c914e:L1-L5`. Carry forward the entry.
8. Acceptance: (1, 1, 1) rejected (purity); R 0.249 rejected; R 0.25 accepted; R 0.9 with 0.051 on each other channel rejected; R 0.91 with 0.045 accepted; a laser through a red filter lights the receiver, but the downstream powered gate gets no supply until the receiver's `PowerIn` is wired to a battery; wiring is refused during Run; Reset restores colour, inactive and zero reading — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L128-L168`. Carry forward.
9. Acceptance: the contact reads an owned, checkpointed acceptance result, independent of the presentation `Active` flag, and rolls back with the transaction — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`. Carry forward.
10. Acceptance: indicator follows committed feedback across failure, pause and Reset (half-blend at 0.0625 s; returns to slate after supply loss); supply disable at the battery opens the downstream gate — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`. Carry forward.
11. Acceptance: a failed tick restores every reading and the retry commits — `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward at the later fault gate.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/red_receiver.tres`, `parts/scenes/red_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`.

## 5. Acceptance outline

- **Chrome UI recipe:** from the actual drawer place a battery, a triggered amber laser, a red filter, the red receiver and a lamp; wire battery `Supply` → laser `PowerIn`, battery `Supply` → receiver `PowerIn`, receiver `Supply` → lamp `PowerIn` with the connection UI; aim with the gizmo. Run.
- **Positive:** red light ≥ 0.25 at ≥ 90% purity lights the disc and the lamp.
- **Negative / controls (independent):** (a) missing required channel — no red arrives (beam blocked, or red below 0.25); (b) wrong channel — a strong green-only or blue-only beam (EL-177/EL-178, or amber through a green filter); (c) absent supply — receiver `PowerIn` unwired: disc lights, lamp stays off. Also purity (unfiltered amber: R is only 48% of total), back-face incidence and an occluder.
- **Boundaries:** threshold 0.25 / 0.249; purity 0.91 / 0.898; threshold edits at 0.05 and 2 accepted, outside rejected; finite disc edge 0.55 m.
- **Run/Reset:** Reset clears reading, match, contact and indicator; threshold persists. **Save/Load:** threshold, placement and wiring round-trip.
- **Integrations:** Story 13.2 suite `tools/e2e/cat-receivers-primary.test.ts`. Binding criteria: [element-146](../requirements.md#element-146), [current-cat-056](../requirements.md#current-cat-056).

## 6. Open questions

1. The row says "declared thresholds" (plural); legacy has one authorable threshold and a fixed 0.9 purity. Confirm purity is not authorable.
2. No hysteresis: a reading at the threshold can toggle the contact tick to tick. Confirm, or adopt the gates' 0.225 off threshold. Owner S486.
3. Heating from absorbed power (OpticalAbsorption, SensibleHeat) has no legacy behaviour; owner S489/S543.
4. Confirm CAT-056 alone satisfies EL-146.
