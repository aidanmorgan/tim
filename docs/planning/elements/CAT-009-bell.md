# CAT-009 · bell — declaration readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Legacy citations are `path@a6c914e:Lstart-Lend` and stay retrievable from git history after Epic 7. Current-tree citations also use the a6c914e baseline. Legacy tick counts are at 120 Hz (`reference/cpu/MachineWorld.cs@a6c914e:L20-L21`): 24 ticks = 0.2 s.

## Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-009 · `bell` |
| Requirement anchor | [CAT-009](../requirements.md#current-cat-009) and its [retained behaviour](../requirements.md#todo-347); D/I/V row [CAT-009-I](../invest/current-consumers.md#cat-009-i) |
| Modes | AcousticTone = Low, Mid, High (each qualified separately) |
| Mapped identities | None. No EL, TH, RAD or GAP identity names the bell; the [acoustic family](../../component-research.md#sound) lists it as "Struck bell (CAT-009)". |
| Capability row | [general-engine-element-map, CAT-009](../general-engine-element-map.md) |
| Roadmap story | Epic 14, Story 14.2 "Service Bell Percussion & Resonant Ringing"; roadmap family "Acoustic emission and reception" ([vertical-delivery](../invest/vertical-delivery.md#existing-element-order)) |
| Status | Not started. |

## Declaration

- **Bodies and shapes.** One static body with a sphere collider of radius 0.75 m at the origin (`parts/BellPart.cs@a6c914e:L32-L32`, `L89-L89`). The bell shell is artwork.
- **Mass and material.** Static. Contact material: the legacy static default restitution 1, threshold 0.1, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`); the ordinary solver alone supplies rebound and the bell adds no velocity (`parts/BellPart.cs@a6c914e:L84-L84`).
- **Constraints and joints.** None.
- **Typed sockets and ports.** None; no electrical supply, no activation input (`parts/BellPart.cs@a6c914e:L8-L8`; `CuriousContraptions.tests/BellTests.cs@a6c914e:L56-L56`).
- **Sensors and activation.** Impact trigger on its own collider: an impact counts when the other body has mass > 0 and approach speed ≥ 0.8 m/s (`parts/BellPart.cs@a6c914e:L33-L33`, `L59-L65`).
  - Strength = clamp(other mass × approach speed / 6, 0.05, 1) (`parts/BellPart.cs@a6c914e:L66-L67`).
  - Rearm: at least 24 ticks (0.2 s) between pulses; impacts in the same tick coalesce to the strongest, independent of body order (`parts/BellPart.cs@a6c914e:L34-L34`, `L68-L80`).
  - Emits one omnidirectional acoustic pulse at the bell's committed position, direction = bell up, tone = the declared band (`parts/BellPart.cs@a6c914e:L81-L83`).
  - Active while a pulse is younger than 0.15 s (`parts/BellPart.cs@a6c914e:L54-L58`).
- **Acoustic pulse law (shared with CAT-060, 061, 069).** Speed 12 m/s, range 8 m, local duration 0.15 s, cone half-angle 35° (cos 0.819152) for cones. At a point d metres away the pulse is non-zero only while 0 ≤ (t − emission) − d/12 < 0.15 s and d ≤ 8, with level strength / (1 + 0.08 d²). It expires ⌈(8/12 + 0.15)/tick⌉ ticks after emission (`engine/Acoustics.cs@a6c914e:L8-L47`).
- **Work and energy stores.** None.
- **Parameters.**

  | Parameter | Type | Range | Default | Unit |
  | --- | --- | --- | --- | --- |
  | tone | enum ToneBand | Low, Mid, High | Mid | — |

  `parts/BellPart.cs@a6c914e:L35-L35`, `L50-L53`; `engine/Acoustics.cs@a6c914e:L8-L8`. The catalogue declares no numeric parameters (`parts/catalog/bell.tres@a6c914e:L5-L11`).
- **Cosmetic curves and UI bindings.**
  - Wobble of the bell body about Z on each committed pulse: underdamped oscillation with decay 7 /s, frequency 48 rad/s, velocity 5 per unit strength, presentation clock; pose = (5/48) e^(−7t) sin(48t) × strength, summed over occurrences (`parts/BellPart.cs@a6c914e:L46-L48`; `CuriousContraptions.tests/AcousticMotionTests.cs@a6c914e:L22-L35`).
  - Wavefront rings, omnidirectional (`parts/BellPart.cs@a6c914e:L41-L41`). The surviving caller passes the positional tuple (0.75, 0, 1, 0.009, 0.35, Strength, 64) after its ring/pattern arguments. Its record definition is absent at baseline a6c914e, so the full field mapping is a baseline gap (Open question 6); no earlier-revision definition is treated as baseline evidence. A visible ring's outer radius is initial radius + distance × radius per distance + thickness (`CuriousContraptions.tests/AcousticWavefrontTests.cs@a6c914e:L63-L68`). The art supplies 15 torus meshes (5 × 3 axes) of radius 1 and half-width 0.009, each mesh set separately to 64 rings (`parts/BellPart.cs@a6c914e:L98-L109`, `L104-L104`). The current visual contract is in [presentation bindings](../../presentation-bindings.md) (0 → 8 m over 8/12 s).
  - Audio: synthesized Bell voice, 0.7 s, −15 dB, max distance 20, polyphony 2 (`parts/BellPart.cs@a6c914e:L110-L112`). Tone frequencies Low 220, Mid 440, High 880 Hz at 22,050 Hz mono 16-bit; partials ×2.76 (decay 8 /s) and ×5.4 (decay 12 /s); 3 ms attack (`engine/AcousticAudio.cs@a6c914e:L11-L31`). Playback never feeds simulation.
- **Art.** Ring "thickness" values are torus half-widths: inner radius r − t, outer r + t (`engine/PartArt.cs@a6c914e:L22-L23`). Shell cylinder (top radius 0.25, bottom 0.59, height 0.7) `#e8b764` at (0, 0.025, 0); crown sphere of radius 0.26 scaled 0.6 in Y at (0, 0.32, 0); rim torus radius 0.6, half-width 0.035 (inner 0.565, outer 0.635), `#fff8e9` at (0, −0.325, 0); clapper rod radius 0.06, height 0.24, `#293954` at (0, −0.37, 0); clapper ball of radius 0.1 `#66b8c9` at (0, −0.49, 0); fixed hanger radius 0.07, height 0.22, `#293954` at (0, 0.55, 0); pick radius 0.85 (`parts/BellPart.cs@a6c914e:L86-L97`). Catalogue colour (0.91, 0.72, 0.39) (`parts/catalog/bell.tres@a6c914e:L11-L11`); icon `ui/WorkshopIcons.cs@a6c914e:L36-L36`.
- **Catalogue and inventory entry.** Id `bell`, title "Bell", category Sound; description "Strike with a moving ball or weight to ring in every direction. Harder, heavier impacts sound louder. Needs separation before another strike, at most five times per second; gentle resting contact stays silent. No battery needed. A powered sound meter can turn the sound into a signal; opaque obstacles block reception." (`parts/catalog/bell.tres@a6c914e:L5-L11`).

## Engine capabilities

Families: the [CAT-009 row](../general-engine-element-map.md) and `docs/coverage/catalogue-elements.json` (AcousticPropagation, AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction).

- **Exists now.** Static sphere colliders and contact; `ContactTriggerDeclaration` qualifies a real impact by pre-response normal approach speed, independent of catalogue kind, and reads back approach speed per occurrence (`engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L7-L22`); animation worker and cosmetic curves (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L48`).
- **Missing.**
  - Acoustic occurrence record (tone, pattern, strength from mass × speed, 0.2 s rearm, same-tick strongest coalescing): Story 14.2.
  - Acoustic propagation, occlusion and reception: Story 14.1 (CAT-060), under decision owner S528 ([decisions](../invest/decisions.md#s528)): S530 arrival combination, S529 event routing.
  - Occurrence-driven oscillation cosmetic, wavefront rings and tone-mapped audio (the current feedback sources are Activation, Timer, ContactWork, Capture): Story 14.2.
- **Element dependencies.** A striking ball (Basketball CAT-001 or other); Sound meter (CAT-060) for the meter integration; Battery (CAT-005), Hold timer (CAT-033) and Powered gate (CAT-051) for the legacy chain; Wall (CAT-066) as occluder.

## Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Collider, threshold, strength, rearm, coalescing and emission as in Declaration. | `parts/BellPart.cs@a6c914e:L32-L85` | Carry forward as declaration data. The per-part `ObserveContact` callback is not carried forward. |
| 2 | Acoustic pulse law and expiry. | `engine/Acoustics.cs@a6c914e:L11-L47` | Carry forward. |
| 3 | Reception: strongest direct arrival wins (not additive); a source never hears itself; a disabled source or receiver neither emits nor hears; opaque colliders on the straight path block it; acoustic solve runs before the electrical solve each tick. | `engine/Acoustics.cs@a6c914e:L49-L80`; `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79`; `reference/cpu/MachineWorld.cs@a6c914e:L855-L861` | Carry forward pending S530. The string-ordinal source ordering is not carried forward. |
| 4 | At most 1024 acoustic events per tick. | `reference/cpu/MachineWorld.cs@a6c914e:L22-L22` | Carry forward as a capacity bound, subject to Epic 16 qualification. |
| 5 | Acceptance: a 1 kg ball at 4 m/s from any of six axes rings once against a rotated bell; its rebound speed is 4 × bounce ± 0.001 (no energy added); no penetration; strength 0.666–0.667; the level 3 m away on every axis at emission + 31 ticks is 0.387–0.388. | `CuriousContraptions.tests/BellTests.cs@a6c914e:L31-L59` | Carry forward. |
| 6 | Acceptance chain: bell (−3, 6, 0), ball dropped from (−3, 9, 0), meter (1, 6, 0) → hold timer → powered gate. Rings only with a ball; the meter hears only without the Wall at (−1, 6, 0); the gate opens only when the meter is also supplied; meter triggers iff the gate opened. Reset clears pulses, count and wobble; Save/Load reproduces the outcome; swapping part identities changes nothing. | `CuriousContraptions.tests/BellTests.cs@a6c914e:L60-L102` | Carry forward. |
| 7 | Acceptance: gentle resting contact stays silent; an impulse of 6 m/s × mass rings once; separation rearms and a second impulse rings again. | `CuriousContraptions.tests/BellTests.cs@a6c914e:L103-L125` | Carry forward. |
| 8 | Acceptance: 0.7 m/s does not ring; 2 m/s with 0.1 kg rings at the 0.05 floor; 2 m/s with 4 kg rings at the cap 1. | `CuriousContraptions.tests/BellTests.cs@a6c914e:L126-L143` | Carry forward. |
| 9 | Acceptance: rapid real strikes keep ≤ 5 pulses in flight and give 9–10 pulses in 240 ticks; the Bell waveform is 30,870 bytes; presentation never changes physics and the wobble decays to rest. | `CuriousContraptions.tests/BellTests.cs@a6c914e:L144-L175` | Carry forward. |
| 10 | Acceptance: two simultaneous impacts (0.1 kg and 1 kg at 6 m/s) give one pulse of strength 1 regardless of order; wobble is 0.07–0.08 rad 0.02 s later. | `CuriousContraptions.tests/BellTests.cs@a6c914e:L176-L199` | Carry forward. |
| 11 | Undefined pattern, tone or voice and strengths 0, −1, 1.1, NaN, ∞ reject; samples beyond range or at expiry are 0. | `CuriousContraptions.tests/BellTests.cs@a6c914e:L200-L223` | Carry forward; enums end to end, no string names. |
| 12 | Hidden rendering still rings; a disabled payload or disabled bell does not. | `CuriousContraptions.tests/ContactParticipationTests.cs@a6c914e:L20-L75` | Carry forward. |
| 13 | Only committed waves render: 3 rings visible for an omnidirectional pulse after 12 ticks, centred on the origin with radius initial + distance × radius per distance + thickness; a 0.01 m/s contact emits nothing; hiding hides rings; Reset restores ring transforms and radii; Load starts with none. | `CuriousContraptions.tests/AcousticWavefrontTests.cs@a6c914e:L27-L87` | Carry forward as presentation acceptance. |
| 14 | Committed kicks only; a failed tick and live activity do not kick; multiple occurrences before rendering all contribute. | `CuriousContraptions.tests/AcousticMotionTests.cs@a6c914e:L36-L109` | Carry forward the committed-only rule; the failed-tick part is not carried forward (no faulting ticks). |
| 15 | Playback volume of a strength-0.5 pulse is −21.02 dB, consistent with −15 dB + 20 log10(strength); staging is silent until publish. | `CuriousContraptions.tests/SceneAcousticRunTests.cs@a6c914e:L50-L70` | Carry forward as audio mapping. |
| 16 | Failed emission restores queue, cooldown and retry. | `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs@a6c914e:L26-L92` | Do not carry forward: no faulting ticks under clamp-or-continue. |
| 17 | No level uses the bell. | `docs/coverage/catalogue-elements.json` (empty fixture list) | Recorded. |

**Files harvested:**
- `parts/BellPart.cs`
- `parts/catalog/bell.tres`
- `parts/scenes/bell.tscn` (no element knowledge: script reference only)
- `engine/Acoustics.cs`
- `engine/AcousticAudio.cs`
- `engine/PartArt.cs` (ring half-width convention)
- `engine/physics/BodyQueryGeometry.cs`
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `CuriousContraptions.tests/BellTests.cs`
- `CuriousContraptions.tests/ContactParticipationTests.cs`
- `CuriousContraptions.tests/AcousticWavefrontTests.cs`
- `CuriousContraptions.tests/AcousticMotionTests.cs`
- `CuriousContraptions.tests/SceneAcousticRunTests.cs`
- `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs`
- `CuriousContraptions.tests/AcousticWavefrontBindingTests.cs` (presentation-binding validation for any acoustic source: capacity, hidden frames, invalid declarations; no bell-specific values)
- `CuriousContraptions.tests/AcousticMotionBindingTests.cs` (fan-out preflight and invalid occurrence rejection; no bell-specific values)

Baseline gap: the wavefront record definition was deleted before a6c914e; the surviving caller tuple and test assertions above are the harvested facts.

## Acceptance outline

Point of truth: [CAT-009](../requirements.md#current-cat-009) and [retained behaviour](../requirements.md#todo-347).

- **Chrome construction.** Through the real palette, place a Bell and a ball above it with the move gizmo; set the tone with the real configuration control; for the integration, add a Battery-supplied Sound meter facing the bell.
- **Positive.** Run: the ball strikes the bell, one pulse is emitted (wavefront and wobble visible), the meter needle rises.
- **Negative or control.** A ball resting gently on the bell, a 0.7 m/s nudge, a depth miss and a Wall between bell and meter each give no reception; rapid strikes are capped.
- **Boundaries.** Threshold 0.8 m/s; strength floor 0.05 and cap 1; 0.2 s rearm; ≤ 5 pulses in flight; Low, Mid and High each qualified.
- **Run/Reset.** Reset clears pulses, count, rings and wobble exactly.
- **Save/Load.** Tone and placement survive; the outcome repeats.
- **Integrations.** Sound meter → hold timer → powered gate; muted audio leaves the outcome identical.

## Open questions

1. **Threshold quantity.** The requirement and legacy use approach speed ≥ 0.8 m/s; Story 14.2 says "kinetic energy exceeding threshold". Owner decision.
2. **Story order.** CAT-009 requires meter integration. After the 9 Oct 2026 reorder, propagation and reception land with the Sound meter in Story 14.1, before the Bell (Story 14.2), so the meter is available for the Bell's integration proof. The meter's own acceptance uses the Bell as its source, which is open scheduling item (c) in `epics.md`.
3. **Rearm in seconds or ticks.** Legacy is 24 ticks at 120 Hz; the current engine runs 60, 120 or 240 Hz (`engine/gpu/WorkshopCadence.cs@a6c914e:L6-L6`). Decide whether the declaration is 0.2 s.
4. **Static contact material.** No bell-specific restitution or friction. Owner decision.
5. **"Service bell".** Story 14.2 calls it a service bell; legacy art is a hanging bell. Confirm the art.

6. **Wavefront field mapping.** Unspecified — owner decision: confirm the intended declaration mapping for the preserved caller tuple because the baseline has no defining record. Preserve the independently sourced radius formula and art values; do not infer missing field names from positional values alone.
