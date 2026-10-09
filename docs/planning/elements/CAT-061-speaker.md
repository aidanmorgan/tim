# CAT-061 · speaker — declaration readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Legacy citations are `path@a6c914e:Lstart-Lend` and stay retrievable from git history after Epic 7. Current-tree citations also use the a6c914e baseline. Legacy tick counts are at 120 Hz (`reference/cpu/MachineWorld.cs@a6c914e:L20-L21`): 24 ticks = 0.2 s.

## Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-061 · `speaker` |
| Requirement anchor | [CAT-061](../requirements.md#current-cat-061) and its [retained behaviour](../requirements.md#todo-345); D/I/V row [CAT-061-I](../invest/current-consumers.md#cat-061-i) |
| Modes | AcousticTone = Low, Mid, High |
| Mapped identities | [EL-180 Pulse speaker](../invest/named-elements.md#element-180) (the [acoustic family](../../component-research.md#sound) pairs EL-180 with CAT-061). [EL-179 Continuous-tone speaker](../invest/named-elements.md#element-179) is a distinct element, not merged. No TH, RAD or GAP identity. |
| Capability row | [general-engine-element-map, CAT-061](../general-engine-element-map.md) |
| Roadmap story | Epic 14, Story 14.3 "Audio Speaker Directional Pulse Emission" |
| Status | Not started. |

## Declaration

- **Bodies and shapes.** One static body: housing box size 1.25 × 1.5 × 1.25 at the origin and base box at (0, −0.85, 0) size 1.6 × 0.18 × 1.55 (half-extents = size/2) (`parts/SpeakerPart.cs@a6c914e:L93-L94`; `reference/cpu/MachinePart.cs@a6c914e:L352-L356`).
- **Mass and material.** Static; legacy static default material (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`).
- **Constraints and joints.** None.
- **Typed sockets and ports.** `PowerIn` (electrical, input) at (−0.8, 0, 0); `ActivationIn` (activation, input) at (0, 0.85, 0) (`parts/SpeakerPart.cs@a6c914e:L56-L61`).
- **Sensors and activation.** Accepts only the Trigger command; any other command rejects (`parts/SpeakerPart.cs@a6c914e:L66-L71`). Triggers are recorded by tick and consumed on the next tick (requests from earlier ticks are coalesced into one); the pulse is emitted only if `PowerIn` is supplied at that tick and at least 24 ticks (0.2 s) have passed since the last pulse. Unpowered requests are consumed and expire, with no delayed replay (`parts/SpeakerPart.cs@a6c914e:L72-L89`).
- **Acoustic source.** Cone pulse, strength 1, origin at the mouth (0.72, 0, 0) in part space, direction part +X, tone = declared band (`parts/SpeakerPart.cs@a6c914e:L40-L40`, `L84-L86`). Pulse law: speed 12 m/s, range 8 m, local duration 0.15 s, cone half-angle 35°, level strength / (1 + 0.08 d²) (`engine/Acoustics.cs@a6c914e:L11-L47`). Active while the last pulse is younger than 0.15 s (`parts/SpeakerPart.cs@a6c914e:L88-L88`).
- **Work and energy stores.** None (power comes through `PowerIn`).
- **Parameters.**

  | Parameter | Type | Range | Default | Unit |
  | --- | --- | --- | --- | --- |
  | tone | enum ToneBand | Low, Mid, High | Mid | — |

  `parts/SpeakerPart.cs@a6c914e:L38-L38`, `L62-L65`. No numeric catalogue parameters (`parts/catalog/speaker.tres@a6c914e:L5-L11`).
- **Cosmetic curves and UI bindings.**
  - Diaphragm (cone node) translation along X, reversed, per committed pulse: decay 18 /s, frequency 24π rad/s, velocity 0.07 × 24π per unit strength, presentation clock (`parts/SpeakerPart.cs@a6c914e:L48-L50`).
  - Wavefront rings, cone pattern (`parts/SpeakerPart.cs@a6c914e:L55-L55`). The surviving caller passes the positional tuple (0, 0.5, 0.7, 0.012, 0.5, Uniform, 64) after its ring/pattern arguments. Its record definition is absent at baseline a6c914e, so the full field mapping is a baseline gap (Open question 5); no earlier-revision definition is treated as baseline evidence. A visible ring's outer radius is initial radius + distance × radius per distance + thickness, and its centre travels along the pulse direction (`CuriousContraptions.tests/AcousticWavefrontTests.cs@a6c914e:L60-L71`). The art supplies 5 torus meshes of radius 1 and half-width 0.012, each set separately to 64 rings (`parts/SpeakerPart.cs@a6c914e:L104-L114`, `L109-L109`).
  - Audio: Speaker voice, a 0.15 s sine at 220/440/880 Hz with 10 ms attack, −15 dB, max distance 20, polyphony 2 (`parts/SpeakerPart.cs@a6c914e:L115-L117`; `engine/AcousticAudio.cs@a6c914e:L11-L31`).
- **Art.** Ring "thickness" values are torus half-widths: inner r − t, outer r + t (`engine/PartArt.cs@a6c914e:L22-L23`). Housing `#fff8e9`, base `#293954`; cone at the mouth: rim torus radius 0.58, half-width 0.06 (inner 0.52, outer 0.64), `#e8b764`; diaphragm cylinder radius 0.49, height 0.08, `#293954`; centre sphere of radius 0.17 `#66b8c9` at +0.06 X; tone marks: (tone index + 1) boxes 0.13 × 0.06 × 0.02 `#293954` at (−0.2 + 0.2 i, 0.45, 0.635); port spheres of radius 0.075 `#f7cb52`; pick radius 1.2 (`parts/SpeakerPart.cs@a6c914e:L90-L103`). Catalogue colour (1, 0.97, 0.91) (`parts/catalog/speaker.tres@a6c914e:L11-L11`); icon `ui/WorkshopIcons.cs@a6c914e:L38-L38`.
- **Catalogue and inventory entry.** Id `speaker`, title "Speaker", category Sound; description "Supply electricity, then trigger a short forward sound pulse. Visible rings work even when muted. Needs a fresh trigger after losing power; at most five pulses per second. Aim toward a powered sound meter to trigger mechanisms; opaque obstacles block its direct reception." (`parts/catalog/speaker.tres@a6c914e:L5-L11`).

## Engine capabilities

Families: the [CAT-061 row](../general-engine-element-map.md) and `docs/coverage/catalogue-elements.json` (AcousticPropagation, AnimationEvaluation, AnimationLifecycle, ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction).

- **Exists now.** Static boxes; activation input and wiring (`engine/gpu/ActivationNetwork.cs`; `WorkshopSocket.ActivationIn` and `PowerIn` in `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`); animation worker.
- **Missing.**
  - ElectricalPower (a supplied `PowerIn`): Story 8.1 (CAT-005); S257 typed power versus signal ([element map](../general-engine-element-map.md)).
  - Acoustic cone source with next-tick coalesced trigger and 0.2 s rearm: Story 14.3.
  - Propagation, occlusion and reception: Story 14.1 (CAT-060), decision owner S528 ([decisions](../invest/decisions.md#s528)).
  - Diaphragm oscillation, cone wavefronts and audio: Story 14.3.
- **Element dependencies.** Battery (CAT-005); a trigger source such as System clock (CAT-017) or Switch (CAT-063); Sound meter (CAT-060) as receiver; Counter (CAT-020) and Powered gate (CAT-051) in legacy chains.

## Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Ports, trigger handling, power gating, rearm, mouth and emission as in Declaration. | `parts/SpeakerPart.cs@a6c914e:L38-L89` | Carry forward as declaration data. The per-part `PreparePhysics` loop is not carried forward. |
| 2 | Acceptance: a cone pulse emitted at tick 10 is silent at 3 m on tick 39 and 0.58–0.59 on tick 41; 0 behind, 90° off-axis, at 9 m, after the 0.15 s window and at expiry; the same geometry rotated gives the same level. All three tones behave alike. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L10-L27` | Carry forward. |
| 3 | Zero direction, undefined tone, negative tick and non-finite sample points reject. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L28-L36` | Carry forward. |
| 4 | Acceptance: a supplied speaker wired to a supplied clock emits on the tick after the clock pulse, regardless of part order; origin = part transform × mouth; Reset clears pulses, count and Active. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L41-L68` | Carry forward. |
| 5 | Acceptance: a trigger while unpowered expires and supplying power later emits nothing; four triggers per tick for 240 ticks keep ≤ 5 pulses in flight and 9–10 pulses total; removing supply ends emission; a Set command rejects. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L69-L98` | Carry forward. |
| 6 | Acceptance: muting (−80 dB) or stopping playback does not change the pulse count or Active state; the waveform is 22,050 Hz 16-bit. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L99-L122` | Carry forward. |
| 7 | Acceptance: speaker at (−3, 6, 0), supplied meter at (1, 6, 0): one meter trigger per speaker pulse; a Wall at (−1, 6, 0), a speaker yawed 180° or an unsupplied meter give none (the backwards and walled cases also give zero level). | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L14-L54` | Carry forward (shared with CAT-060). |
| 8 | Emission uses the solved pose, not the rendered transform: origin = solved pose × mouth, direction = solved X. | `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L322-L354` | Carry forward. |
| 9 | Only committed waves render; one ring visible for a speaker pulse after 12 ticks, centred at origin + direction × distance travelled. | `CuriousContraptions.tests/AcousticWavefrontTests.cs@a6c914e:L27-L87` | Carry forward as presentation acceptance. |
| 10 | Committed diaphragm kicks only; multiple occurrences sum. | `CuriousContraptions.tests/AcousticMotionTests.cs@a6c914e:L22-L109` | Carry forward the committed-only rule; failed-tick cases are not carried forward. |
| 11 | Failed emission restores the request queue and cooldown. | `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs@a6c914e:L26-L92` | Do not carry forward: no faulting ticks. |
| 12 | No level uses the speaker. | `docs/coverage/catalogue-elements.json` (empty fixture list) | Recorded. |

**Files harvested:**
- `parts/SpeakerPart.cs`
- `parts/catalog/speaker.tres`
- `parts/scenes/speaker.tscn` (no element knowledge: script reference only)
- `engine/Acoustics.cs`
- `engine/AcousticAudio.cs`
- `engine/PartArt.cs` (ring half-width convention)
- `reference/cpu/MachinePart.cs`
- `reference/cpu/MachineWorld.cs`
- `CuriousContraptions.tests/AcousticTests.cs`
- `CuriousContraptions.tests/SoundMeterTests.cs`
- `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`
- `CuriousContraptions.tests/AcousticWavefrontTests.cs`
- `CuriousContraptions.tests/AcousticMotionTests.cs`
- `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs`

Baseline gap: the wavefront record definition was deleted before a6c914e; the surviving caller tuple and test assertions above are the harvested facts.

## Acceptance outline

Point of truth: [CAT-061](../requirements.md#current-cat-061) and [retained behaviour](../requirements.md#todo-345).

- **Chrome construction.** Through the real palette, place a Battery, a System clock, the Speaker and a supplied Sound meter in front of it; wire Battery → Speaker `PowerIn`, Battery → Clock and Clock → Speaker `ActivationIn` with the real socket UI; set the tone.
- **Positive.** Run: each clock pulse produces one forward pulse on the next tick; the meter triggers; a ring travels forward.
- **Negative or control.** Speaker unpowered (the trigger expires), speaker facing away, a Wall in between, and the meter beyond 8 m or outside the 35° cone each give no reception.
- **Boundaries.** ≤ 5 pulses in flight (0.2 s rearm); duplicate triggers in one tick coalesce; Low, Mid and High each qualified.
- **Run/Reset.** Reset clears pulses, requests and rings exactly.
- **Save/Load.** Tone, placement and wiring survive; the outcome repeats.
- **Integrations.** Clock, Battery, Sound meter; muted or suspended audio gives the same outcome.

## Open questions

1. **Cone angle.** Legacy and the requirement use a 35° half-angle; Story 14.3 says "radiate forward in a 35° cone". Confirm half-angle.
2. **Order with the meter.** Story 14.1 supplies propagation and the Sound meter before the Speaker in Story 14.3; reuse that admitted reception path.
3. **Rearm in seconds or ticks.** 24 ticks at 120 Hz versus the selectable 60/120/240 Hz cadence (`engine/gpu/WorkshopCadence.cs@a6c914e:L6-L6`). Owner decision.
4. **Supply semantics.** Whether the speaker draws finite electrical power per pulse is not specified in legacy. Owner decision with Story 8.1.

5. **Wavefront field mapping.** Unspecified — owner decision: confirm the intended declaration mapping for the preserved caller tuple because the baseline has no defining record. Preserve the independently sourced radius formula and art values; do not infer missing field names from positional values alone.
