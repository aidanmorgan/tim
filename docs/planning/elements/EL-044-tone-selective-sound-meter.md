# EL-044 · Tone-selective sound meter — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. This spec also holds the **shared acoustic family facts (A1–A13)** that the other sound identities ([EL-045](EL-045-air-whistle.md)–[EL-052](EL-052-water-tuned-bottle.md), [EL-179](EL-179-continuous-tone-speaker.md), [EL-180](EL-180-pulse-speaker.md)) point to instead of repeating.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-044 · Tone-selective sound meter |
| Type | Sound |
| Anchor | [requirements.md#element-044](../requirements.md#element-044); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-044); owner S541 |
| Related | Extends [CAT-060 Sound meter](CAT-060-sound_meter.md) (same cabinet, ports and threshold law; adds a band filter — CAT-060 already requires "all admitted tone filtering", [current-cat-060](../requirements.md#current-cat-060)). Sources: [CAT-061 Speaker](CAT-061-speaker.md) / [EL-180](EL-180-pulse-speaker.md), [CAT-009 Bell](CAT-009-bell.md), [CAT-069 Wind chimes](CAT-069-wind_chimes.md), [EL-045](EL-045-air-whistle.md). |
| Roadmap story | unscheduled (CAT-060 is Story 14.1; the band filter has no story) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static cabinet box of full size 1.6 × 1.5 × 0.65 m (collision half-extents 0.8 × 0.75 × 0.325) at the origin and foot box of full size 1.8 × 0.18 × 0.85 m at (0, −0.85, 0) (`parts/SoundMeterPart.cs@a6c914e:L87-L88`; legacy `AddBox` takes the full size and stores half-extents, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | `PowerIn` (Electrical, Input) (−0.92, −0.5, 0); `Supply` (Electrical, Output) (0.92, −0.5, 0); `ActivationOut` (Activation, Output) (0.92, 0.45, 0) (`parts/SoundMeterPart.cs@a6c914e:L50-L55`). Contact route `PowerIn` → `Supply` closed while the threshold state is on (`parts/SoundMeterPart.cs@a6c914e:L56-L57`). |
| Sensors and activation | Acoustic receiver at local origin (`parts/SoundMeterPart.cs@a6c914e:L48-L48`). Band level = strongest admissible arrival (A6) among pulses whose tone equals `band`; pulses of another tone contribute 0 **proposed** (simplest discrete selectivity: an equal-strength wrong-tone pulse reads 0, so the outcome holds at every threshold). Threshold state: on at level ≥ `threshold`, held while level > 0.9 × `threshold` (`parts/SoundMeterPart.cs@a6c914e:L64-L71`). A rising crossing emits one `ActivationOut` only if `PowerIn` is supplied on that tick; an unpowered crossing is discarded, never replayed when supply returns (`parts/SoundMeterPart.cs@a6c914e:L72-L83`). |
| Work and energy stores | none; the contact never creates supply. |
| Parameters | `threshold`: f32 dimensionless 0.05–1, default 0.25 (`parts/SoundMeterPart.cs@a6c914e:L58-L63`; `parts/catalog/sound_meter.tres@a6c914e:L12-L12`). `band`: closed enum `ToneBand { Low, Mid, High }` (A1), default Mid **proposed** (the Speaker's default tone, `parts/SpeakerPart.cs@a6c914e:L38-L38`, so an untouched pair works). |
| Cosmetic curves and UI bindings | Needle ← committed band level (0–1) with eased response; output lamp slate `#556573` → gold `#f7cb52`, 0.1 s SmoothStep, on owner active = above threshold ∧ supplied (`parts/SoundMeterPart.cs@a6c914e:L40-L47`, `parts/SoundMeterPart.cs@a6c914e:L76-L76`). The needle shows only in-band level. |
| Art | Cyan `#66b8c9` cabinet, navy `#293954` foot, cream `#fff8e9` dial 1.35 × 1.15 with seven navy ticks, gold `#e8b764` needle, slate lamp, gold `#f7cb52` port spheres r 0.075 (`parts/SoundMeterPart.cs@a6c914e:L84-L100`). Band marks: one, two or three raised navy bars for Low, Mid, High, the Speaker's tone-mark rule (`parts/SpeakerPart.cs@a6c914e:L101-L102`), plus an engraved tuning-fork glyph on the cabinet top **proposed** (marks the meter as tuned, readable muted — sound recipe "The Right Voice", [component research](../../component-research.md#sound)). [DESIGN.md Sound meter](../../../DESIGN.md#sound-meter). |
| Catalogue / inventory | Id `tuned_sound_meter`, Title "Tuned sound meter", Category Sound **proposed** (Sound is the legacy category of the meter, `parts/catalog/sound_meter.tres@a6c914e:L8-L8`). Description **proposed**: "Like the sound meter, but it listens only to sounds whose marks match its own. Supply electricity to trigger and pass power." |

**Variants** (campaign coverage requires "each tuned band", [campaign-element-coverage](../requirements.md#campaign-element-coverage)):

| Variant | `band` | Accepts | Rejects (reads 0) | Presentation frequency |
| --- | --- | --- | --- | --- |
| Low | Low | Low pulses | Mid, High | 220 Hz (A11) |
| Mid | Mid | Mid pulses | Low, High | 440 Hz (A11) |
| High | High | High pulses | Low, Mid | 880 Hz (A11) |

### Shared acoustic family facts

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| A1 | Closed sets: `ToneBand { Low, Mid, High }`, `AcousticPattern { Cone, Omnidirectional }`. | `engine/Acoustics.cs@a6c914e:L8-L9` | carry forward as enums end to end |
| A2 | Pulse constants: speed 12 m/s, range 8 m, local duration 0.15 s, cone half-angle 35° (cos 0.819152). | `engine/Acoustics.cs@a6c914e:L14-L17` | carry forward as f32 declaration data (not compiler constants) |
| A3 | A pulse expires at emission + ⌈(range/speed + duration)/tick⌉ ticks. | `engine/Acoustics.cs@a6c914e:L24-L24` | carry forward the rule; the tick comes from the simulation cadence |
| A4 | A pulse needs a finite origin, non-zero direction, defined tone and pattern, non-negative emission tick and strength in (0, 1]. | `engine/Acoustics.cs@a6c914e:L26-L34` | carry forward as admission rejection |
| A5 | Sample: 0 beyond range, before emission or after expiry, or outside the cone; local age = elapsed − distance/12 must lie in [0, 0.15 s); level = strength / (1 + 0.08·d²). | `engine/Acoustics.cs@a6c914e:L36-L46` | carry forward |
| A6 | Reception: the strongest direct arrival wins (never summed); a source never hears itself; disabled parts neither emit nor receive; opaque geometry on the straight path blocks the arrival. | `engine/Acoustics.cs@a6c914e:L49-L79` | carry forward the rule (S528 arrival-combination row confirms or replaces it); do not carry forward the CPU per-tick solve over scene parts ordered by string id |
| A7 | Sound and light traces test only opaque colliders; air traces test all geometry. | `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79`, `engine/physics/WorldQueryContracts.cs@a6c914e:L4-L4` | carry forward |
| A8 | Pulse at origin, +X, tick 10: at 3 m reads 0 at tick 39 and 0.58–0.59 at tick 41; behind, beside, at 9 m and at tick 60 read 0; a rotated, translated pulse samples identically. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L10-L27` | carry forward (acceptance table at 120 Hz) |
| A9 | Zero direction, undefined tone, negative tick and non-finite sample points reject. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L28-L36` | carry forward |
| A10 | Two equal simultaneous sources read 1, not 2; a hidden meter reads 0. | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L72-L95` | carry forward |
| A11 | Audio is presentation-only: tones synthesized at Low 220, Mid 440, High 880 Hz; speaker voice 0.15 s, bell 0.7 s; muting or stopping playback never changes pulses or counts. | `engine/AcousticAudio.cs@a6c914e:L8-L15`, `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L99-L122` | carry forward |
| A12 | Speaker → meter triggers only through an unblocked forward path with meter supply; a wall or a reversed speaker gives level 0; without supply the needle still reads but nothing triggers; Reset clears level, count and state. | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L14-L54` | carry forward |
| A13 | The legacy used Godot float vectors, a `MachineWorld.Tick` constant and per-part update hooks. | `engine/Acoustics.cs@a6c914e:L24-L24`, `engine/Acoustics.cs@a6c914e:L52-L56` | do not carry forward (canonical f32 records, worker cadence, no per-element loop) |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S541): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction). Map composition: "Band-selective numerical acoustic observation plus separately supplied contact; do not treat raw event arrival as adequate power."

**Exists now**
- Static boxes: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`; activation output socket and network: `engine/gpu/WorkshopConnections.cs@a6c914e:L9-L9`, `engine/gpu/ActivationNetwork.cs@a6c914e:L69-L108`.

**Missing**
- AcousticPropagation (pulse records, propagation, opaque occlusion, strongest arrival) — Story 14.2/14.3/14.1 (CAT-009/061/060); decision owner [S528](../invest/decisions.md#s528) (S530 arrival combination, S529 event routing).
- Band filter on the receiver — this identity (unscheduled).
- Supplied contact and rising-edge activation that may re-arm — Story 8.1 network; repeated emission per run is not supported by the current activation network (`engine/gpu/ActivationNetwork.cs@a6c914e:L206-L208`).
- Scalar needle cosmetic source — current sources are discrete (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`).

**Dependencies.** CAT-060 Sound meter (Story 14.1); a toned source (CAT-061 Speaker with Battery CAT-005, or CAT-009 Bell); Battery for the contact.

## 4. Sources and legacy

- Requirement row [element-044](../requirements.md#element-044): "Separately supplied sensor selects a declared frequency band"; outcome "Equal-strength wrong-tone pulse does not close the contact". Scope index [todo-352](../requirements.md#todo-352): "A tuned meter distinguishes the intended tone".
- Named entry [element-044](../invest/named-elements.md#element-044), owner S541; [acoustic profile](../invest/profiles.md#acoustic): "silence/wrong tone/no power does not trigger the meter … each tone separately".
- Component research: "Tone-selective sound meter (EL-044) | Receiver + tone filter | band, thresholds | Responds only to its engraved band | Needle ← strength" ([component research](../../component-research.md#sound)).
- Legacy: the legacy Sound meter has no tone filter (it takes the strongest pulse of any tone, `engine/Acoustics.cs@a6c914e:L65-L74`); cabinet, ports, threshold and hysteresis facts are carried from it (also harvested by [CAT-060](CAT-060-sound_meter.md)).
- **Files harvested:** `engine/Acoustics.cs`, `engine/AcousticAudio.cs`, `engine/physics/BodyQueryGeometry.cs` (trace medium lines), `engine/physics/WorldQueryContracts.cs`, `parts/SoundMeterPart.cs`, `parts/catalog/sound_meter.tres`, `parts/SpeakerPart.cs` (tone marks, default tone), `reference/cpu/MachinePart.cs` (`AddBox` extents), `CuriousContraptions.tests/AcousticTests.cs`, `CuriousContraptions.tests/SoundMeterTests.cs`.

## 5. Acceptance outline

Acceptance authority: [element-044](../requirements.md#element-044), [acoustic profile](../invest/profiles.md#acoustic).

- **Construction (actual Chrome UI).** Battery → Speaker (`Supply → PowerIn`), Switch → Speaker activation; Tuned sound meter facing the speaker 3 m away with `band` chosen through its configuration control; Battery → meter `PowerIn`; meter `Supply` → supplied load or meter `ActivationOut` → Signal lamp.
- **Positive.** Matching tone: the needle rises, the lamp lights, the load runs (one activation per rising crossing).
- **Negative / control.** Same speaker strength set to a different tone: needle stays at 0 and the contact stays open (outcome). No meter supply: needle reads, no trigger. Wall between: 0. Speaker reversed: 0.
- **Boundaries.** Each band separately (three variants × matching and non-matching tones); threshold 0.05 and 1; hysteresis at 0.9 × threshold.
- **Run/Reset.** Reset clears level, threshold state and pending pulses exactly.
- **Save/Load.** `band`, `threshold` and connections round-trip; muted and audible runs give identical outcomes.
- **Integrations.** Acoustic integration task [sequence-task-407](../requirements.md#sequence-task-407) ("A tuned meter distinguishes the intended tone"); combination [todo-378](../requirements.md#todo-378) ("The Mill's Song" sound-controlled sluice); CAT-060 retained behaviour [todo-346](../requirements.md#todo-346) (threshold, hysteresis, tone filtering); interaction processes [IX-12 acoustic propagation](../requirements.md#interaction-12), [IX-07 signal propagation](../requirements.md#interaction-07) and [IX-06 electrical power transfer](../requirements.md#interaction-06) (supplied contact). Campaign: the sound row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "sound meter" and "each tuned band", first use 71–80 (reuse 81–100, 117–125, 136–150).

## 6. Open questions

1. **Selectivity shape.** Discrete exact-band acceptance (proposed) versus partial response to neighbouring bands: owner decision under S528.
2. **One entry or three.** A single catalogue entry with a `band` setting (proposed) versus three catalogue entries, one per band: owner decision.
