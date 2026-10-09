# EL-179 · Continuous-tone speaker — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared acoustic facts A1–A13 are in [EL-044](EL-044-tone-selective-sound-meter.md#shared-acoustic-family-facts); speaker facts K1–K10 in [EL-180](EL-180-pulse-speaker.md#speaker-facts-shared-with-el-179); sustained emission S1 in [EL-045](EL-045-air-whistle.md).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-179 · Continuous-tone speaker |
| Type | Sound |
| Anchor | [requirements.md#element-179](../requirements.md#element-179); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Speaker/pulse/continuous modes"); [named-elements entry](../invest/named-elements.md#element-179); owner S531 |
| Related | Extends [CAT-061 Speaker](CAT-061-speaker.md) with a continuous mode distinct from [EL-180 Pulse speaker](EL-180-pulse-speaker.md) (map: "pulse and continuous speakers stay distinct"). Supply [CAT-005](CAT-005-battery.md)/[EL-196](EL-196-battery.md), switched by [EL-198](EL-198-switch.md); listeners [CAT-060](CAT-060-sound_meter.md), [EL-044](EL-044-tone-selective-sound-meter.md), [EL-049 Resonator](EL-049-acoustic-resonator.md). |
| Roadmap story | unscheduled (CAT-061 pulse mode is Story 14.3) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | The speaker cabinet: box 1.25 × 1.5 × 1.25 m at the origin, foot 1.6 × 0.18 × 1.55 m at (0, −0.85, 0); mouth (0.72, 0, 0), axis +X (`parts/SpeakerPart.cs@a6c914e:L40-L40`, `parts/SpeakerPart.cs@a6c914e:L93-L94`). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | `PowerIn` (Electrical, Input) (−0.8, 0, 0) (K1). No `ActivationIn` **proposed** (the tone follows supply and the authored enable, so an activation port would suggest a pulse contract it does not have). |
| Sensors and activation | Phase closed enum `ToneSpeakerPhase { Silent, Sounding }`: Sounding exactly while `PowerIn` is supplied with available power and `enabled` = Enabled. On power loss it stops emitting new pulses on that committed tick; pulses already in flight expire by their own lifetime (A3), and a downstream resonator decays by its own state (EL-049) — the speaker fabricates no tail. |
| Work and energy stores | No internal store. While Sounding it draws a continuous `power` from the supplying finite store each tick (FiniteLedger); if the store cannot supply it the speaker is Silent (map: "Continuous finite supplied acoustic power and emission lifetime"). |
| Parameters | `tone`: `ToneBand` (A1), default Mid (the speaker default, `parts/SpeakerPart.cs@a6c914e:L38-L38`). `enabled`: closed enum `ToneEnable { Disabled, Enabled }`, default Enabled **proposed** (an author-set mute that never needs a float compare, as CAT-005's enable). `power`: f32 1–40 W, default 10 W **proposed** (a 3 600 J default battery sustains it for 6 minutes — finite, observable with a small battery, well below the 120 W default Motor). Strength fixed 1 and pattern Cone (A2, as the pulse speaker). |
| Cosmetic curves and UI bindings | Diaphragm oscillation (`parts/SpeakerPart.cs@a6c914e:L48-L50`) runs continuously while Sounding and stops when Silent; forward rings per S1 pulse (`parts/SpeakerPart.cs@a6c914e:L55-L55`); a cream sine-wave glyph on the cabinet top lights gold while Sounding **proposed** (tells the continuous speaker from the pulse speaker without text). |
| Art | The speaker art (`parts/SpeakerPart.cs@a6c914e:L95-L103`: cream cabinet, navy foot, gold rim, navy diaphragm, cyan centre, tone bars) plus the sine-wave glyph. [DESIGN.md Sound speaker](../../../DESIGN.md#sound-speaker). |
| Catalogue / inventory | Id `tone_speaker`, Title "Tone speaker", Category Sound **proposed**. Description **proposed**: "Holds a steady tone for as long as it has electricity. Cut the power and it stops at once." |

**Emission.** While Sounding the speaker emits back-to-back cone pulses per S1 (one 0.15 s pulse per pulse window, A2 constants), so meters see a continuous level and trigger once on the rising edge.

**Variants** (CAT-061 tone modes; campaign coverage "each tuned band"):

| Variant | `tone` | Tone marks | Presentation frequency |
| --- | --- | --- | --- |
| Low | Low | 1 bar | 220 Hz (A11) |
| Mid | Mid | 2 bars | 440 Hz |
| High | High | 3 bars | 880 Hz |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S531): AcousticPropagation, ElectricalPower, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction).

**Exists now**
- Static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`); `PowerIn` enum value, not admitted (`engine/gpu/WorkshopConnections.cs@a6c914e:L9-L9`).

**Missing**
- Supplied network — Story 8.1; continuous power draw on a finite store — [EL-196](EL-196-battery.md).
- AcousticPropagation — Story 14.3; sustained emission (S1) — [S528](../invest/decisions.md#s528) (S529 identity of a held source).
- Continuous diaphragm binding driven by committed phase.

**Dependencies.** Battery (CAT-005/EL-196); a supply switch (EL-198) to show power loss; a Sound meter or resonator.

## 4. Sources and legacy

- Requirement row [element-179](../requirements.md#element-179): "Finite supplied power sustains a declared acoustic tone while enabled"; outcome "Power loss stops excitation; stored resonance may decay only by its own state".
- Named entry [element-179](../invest/named-elements.md#element-179), owner S531; map composition "Continuous finite supplied acoustic power and emission lifetime differ from pulse occurrence admission"; component research "Continuous-tone speaker (EL-179) | Electrical node + sustained acoustic source | band, strength | Holds a tone while powered and enabled".
- Legacy: only the pulse speaker exists (K1–K10); no continuous mode. Reused: cabinet, port, tone, art and diaphragm facts.
- **Files harvested:** `parts/SpeakerPart.cs`; acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-179](../requirements.md#element-179), [acoustic profile](../invest/profiles.md#acoustic).

- **Construction (actual Chrome UI).** Battery → Switch contact (EL-198) → Tone speaker `PowerIn`; supplied Sound meter and a Resonator (EL-049) in front.
- **Positive.** Supply closed: the diaphragm vibrates, rings stream, the meter triggers once and stays above threshold; the resonator builds.
- **Negative / control.** Open the supply: emission stops on that tick; the meter falls after the last pulse's lifetime; the resonator decays at its own rate, not faster or slower (outcome). `enabled` = Disabled with supply: silent. Depleted battery: silent.
- **Boundaries.** Each tone variant; `power` limits; a battery with exactly enough energy for n seconds sounds for n seconds.
- **Run/Reset.** Reset silences and restores the battery ledger.
- **Save/Load.** `tone`, `enabled`, `power` and connections round-trip.
- **Integrations.** CAT-061 speaker retained behaviour [todo-345](../requirements.md#todo-345) (supply, tones, cone, mute independence); acoustic integration task [sequence-task-407](../requirements.md#sequence-task-407) (tuned meter, resonator that builds and loses excitation); interaction processes [IX-06 electrical power transfer](../requirements.md#interaction-06) (finite continuous draw) and [IX-12 acoustic propagation](../requirements.md#interaction-12). Campaign: the sound row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "Speaker/pulse/continuous modes", first use 71–80 (reuse 81–100, 117–125, 136–150).

## 6. Open questions

1. **Control input.** Supply-only control (proposed) versus an `ActivationIn` Set/Reset enable: owner decision.
2. **Sustained representation.** Back-to-back pulses (S1) versus one long occurrence: S529.
3. **Mode or element.** Whether the continuous speaker is a separate catalogue entry (proposed) or a `mode` of the CAT-061 speaker: owner decision.
