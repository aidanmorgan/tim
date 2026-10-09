# EL-180 · Pulse speaker — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents (legacy `AddBox` takes the full size and stores half-extents, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). Shared acoustic facts A1–A13 are in [EL-044](EL-044-tone-selective-sound-meter.md#shared-acoustic-family-facts). This spec holds the **speaker facts K1–K10** that [EL-179](EL-179-continuous-tone-speaker.md) reuses.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-180 · Pulse speaker |
| Type | Sound |
| Anchor | [requirements.md#element-180](../requirements.md#element-180); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Speaker/pulse/continuous modes"); [named-elements entry](../invest/named-elements.md#element-180); owner S532 |
| Related | Refines [CAT-061 Speaker](CAT-061-speaker.md) (`speaker`, [current-cat-061](../requirements.md#current-cat-061)); sibling mode [EL-179 Continuous-tone speaker](EL-179-continuous-tone-speaker.md); supply [CAT-005](CAT-005-battery.md)/[EL-196](EL-196-battery.md); triggers from [CAT-017 Clock](CAT-017-clock.md), [CAT-063 Switch](CAT-063-switch.md); heard by [CAT-060](CAT-060-sound_meter.md)/[EL-044](EL-044-tone-selective-sound-meter.md). |
| Roadmap story | 14.3 "Audio Speaker Directional Pulse Emission" (Epic 14) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static cabinet box 1.25 × 1.5 × 1.25 m at the origin and foot box 1.6 × 0.18 × 1.55 m at (0, −0.85, 0) (`parts/SpeakerPart.cs@a6c914e:L93-L94`). Mouth (emission origin) at local (0.72, 0, 0), axis local +X (`parts/SpeakerPart.cs@a6c914e:L40-L40`, `parts/SpeakerPart.cs@a6c914e:L85-L85`). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | `PowerIn` (Electrical, Input) (−0.8, 0, 0) and `ActivationIn` (Activation, Input) (0, 0.85, 0) (`parts/SpeakerPart.cs@a6c914e:L57-L61`) (K1). |
| Sensors and activation | Only the Trigger command is accepted; Set/Reset reject (K2). Triggers received during tick t are coalesced into one request served at tick t + 1; if `PowerIn` is unpowered then, the request expires (no later replay); a pulse also needs ≥ 24 ticks since the last pulse (K3–K5). |
| Work and energy stores | Each emitted pulse debits a fixed pulse energy from the supplying store: 2 J **proposed** (with a 3 600 J default battery, [EL-196](EL-196-battery.md), that is 1 800 pulses — finite but never limiting in an ordinary lesson; depletion is observable with a small battery). No internal store: a pulse cannot be emitted without that debit (map composition: "One supplied trigger debits bounded pulse energy"). |
| Parameters | `tone`: closed enum `ToneBand { Low, Mid, High }`, default Mid (`parts/SpeakerPart.cs@a6c914e:L38-L38`; undefined rejects, `parts/SpeakerPart.cs@a6c914e:L62-L65`). Minimum interval 24 ticks (`parts/SpeakerPart.cs@a6c914e:L39-L39`) — carried as 0.2 s so it does not change with cadence **proposed** (24 ticks at the legacy 120 Hz; the current worker cadence is declared by `SimulationCadence`). Pulse constants A2: 12 m/s, 8 m, 0.15 s, 35° cone, strength 1. |
| Cosmetic curves and UI bindings | Diaphragm oscillation (amplitude 18 presentation units, angular rate 24π, damping 0.07 × 24π, presentation clock) on translation X, reversed (`parts/SpeakerPart.cs@a6c914e:L48-L50`); five forward translucent rings, shadow-free, 64 segments, uniform opacity, parameters (0, 0.5, 0.7, 0.012, 0.5) (`parts/SpeakerPart.cs@a6c914e:L55-L55`, `parts/SpeakerPart.cs@a6c914e:L104-L114`); active window = 0.15 s after a pulse (`parts/SpeakerPart.cs@a6c914e:L88-L88`). Audio voice is presentation-only (A11). |
| Art | Cream `#fff8e9` cabinet, navy `#293954` foot; gold `#e8b764` rim ring r 0.58, thickness 0.06; navy diaphragm r 0.49, depth 0.08; cyan `#66b8c9` centre sphere r 0.17 at (0.06, 0, 0) from the mouth; tone marks: tone+1 navy bars 0.13 × 0.06 × 0.02 at (−0.2 + 0.2·mark, 0.45, 0.635); gold `#f7cb52` port spheres r 0.075 (`parts/SpeakerPart.cs@a6c914e:L95-L103`). [DESIGN.md Sound speaker](../../../DESIGN.md#sound-speaker). Toolbox icon `ui/WorkshopIcons.cs@a6c914e:L38-L38`. |
| Catalogue / inventory | Id `speaker`, Title "Speaker", Category Sound, colour (1, 0.97, 0.91); Description "Supply electricity, then trigger a short forward sound pulse. Visible rings work even when muted. Needs a fresh trigger after losing power; at most five pulses per second. Aim toward a powered sound meter to trigger mechanisms; opaque obstacles block its direct reception." (`parts/catalog/speaker.tres@a6c914e:L6-L11`). |

**Variants** (CAT-061 modes `AcousticTone=Low, Mid, High`, [current-cat-061](../requirements.md#current-cat-061)):

| Variant | `tone` | Tone marks | Presentation frequency |
| --- | --- | --- | --- |
| Low | Low | 1 bar | 220 Hz (A11) |
| Mid | Mid | 2 bars | 440 Hz |
| High | High | 3 bars | 880 Hz |

### Speaker facts (shared with EL-179)

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| K1 | Ports PowerIn (−0.8, 0, 0) Electrical input, ActivationIn (0, 0.85, 0) Activation input. | `parts/SpeakerPart.cs@a6c914e:L57-L61` | carry forward |
| K2 | Trigger only; any other command rejects. | `parts/SpeakerPart.cs@a6c914e:L66-L71`, `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L95-L95` | carry forward |
| K3 | Requests from earlier ticks are consumed together (coalesced) at the next tick. | `parts/SpeakerPart.cs@a6c914e:L74-L81` | carry forward the next-tick rule; do not carry forward the per-part request set |
| K4 | Emission needs PowerIn powered and ≥ 24 ticks since the last pulse; an unpowered request is dropped, not deferred. | `parts/SpeakerPart.cs@a6c914e:L82-L87`, `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L69-L84` | carry forward |
| K5 | Retrigger storm (4 triggers per tick for 240 ticks): never more than 5 pulses in flight; 9–10 pulses total; after supply loss all pulses expire and the speaker goes inactive. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L85-L94` | carry forward |
| K6 | A clock trigger produces the pulse on the next tick regardless of part order; the pulse origin is the transformed mouth; Reset clears pulses, count and active state. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L41-L68` | carry forward |
| K7 | Muting or stopping audio leaves pulse count and activity unchanged. | `CuriousContraptions.tests/AcousticTests.cs@a6c914e:L99-L122` | carry forward |
| K8 | Speaker → meter only through an unblocked forward path with meter supply. | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L14-L54` | carry forward |
| K9 | The tone is an exported per-instance enum with default Mid. | `parts/SpeakerPart.cs@a6c914e:L38-L38` | carry forward as a typed configuration value |
| K10 | Runtime checkpoint class snapshots pulses, requests and counters per part. | `parts/SpeakerPart.cs@a6c914e:L11-L37` | do not carry forward (CPU transaction participant; the worker's committed state owns Reset) |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S532): AcousticPropagation, ElectricalPower, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction).

**Exists now**
- Static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`); `ActivationIn` socket and activation network with next-tick ordering (`engine/gpu/ActivationNetwork.cs@a6c914e:L69-L108`).

**Missing**
- Supplied network — Story 8.1; per-pulse energy debit — [EL-196](EL-196-battery.md) finite store (owner decision).
- AcousticPropagation (pulse records, cone, occlusion) — Story 14.3 (this), [S528](../invest/decisions.md#s528).
- Re-triggerable activation target (one pulse per accepted trigger, many per run) — the current network latches a target on its first input (`engine/gpu/ActivationNetwork.cs@a6c914e:L213-L216`).
- Diaphragm and wavefront cosmetic bindings driven by committed pulse occurrences — Story 14.3.

**Dependencies.** Battery (CAT-005, Story 8.1); a trigger source (Clock CAT-017, Switch CAT-063); a Sound meter (CAT-060, Story 14.1) to observe.

## 4. Sources and legacy

- Requirement row [element-180](../requirements.md#element-180): "Supplied trigger releases a bounded acoustic pulse"; outcome "Held input follows its edge contract and cannot create infinite pulse energy". [current-cat-061](../requirements.md#current-cat-061): next-tick coalesced trigger, independent supply, minimum interval 24 ticks bounds five in-flight pulses, Low/Mid/High tones, 12 unit/s, 8 unit reach, 0.15 s, 35° cone; mute cannot change gameplay.
- Named entry [element-180](../invest/named-elements.md#element-180), owner S532; map composition "One supplied trigger debits bounded pulse energy; no implicit indefinitely sustained tone"; component research "Pulse speaker (EL-180, CAT-061) | Electrical node + acoustic source (one pulse per rising trigger)".
- Legacy harvest K1–K10; also harvested by [CAT-061](CAT-061-speaker.md).
- **Files harvested:** `parts/SpeakerPart.cs`, `parts/catalog/speaker.tres`, `CuriousContraptions.tests/AcousticTests.cs`, `CuriousContraptions.tests/SoundMeterTests.cs`, `reference/cpu/MachinePart.cs` (`AddBox` extents); acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-180](../requirements.md#element-180), [current-cat-061](../requirements.md#current-cat-061); Story 14.3 `tools/e2e/cat-061.test.ts`.

- **Construction (actual Chrome UI).** Battery → Speaker `PowerIn`; Clock → Speaker `ActivationIn`; supplied Sound meter 3 m in front; `tone` chosen through the configuration control.
- **Positive.** Each clock pulse yields one speaker pulse on the next tick; rings expand; the meter triggers per pulse.
- **Negative / control.** Unpowered speaker: triggers expire, no pulse, no later replay when supply returns. Held input (edge contract, outcome): a Switch pressed by a ball that then rests on it emits one trigger occurrence, so the speaker emits exactly one pulse for the whole hold. Facing away, wall between, meter beyond 8 m: no reception.
- **Storm bound (separate test).** Four Clocks at the 0.1 s minimum interval ([CAT-017](CAT-017-clock.md)) all wired to `ActivationIn` request far faster than the 24-tick limit; over 240 ticks (2 s at 120 Hz) the speaker emits 9–10 pulses (K5), at most one per 0.2 s, and never more than 5 pulses are in flight (outcome: no infinite pulse energy); each pulse debits its 2 J.
- **Boundaries.** Each tone variant separately; triggers 23 and 24 ticks apart; battery with too little energy for another pulse emits none.
- **Run/Reset.** Reset clears pulses, count and pending requests.
- **Save/Load.** `tone` and connections round-trip; muted and audible runs identical (K7).
- **Integrations.** CAT-061 speaker retained behaviour [todo-345](../requirements.md#todo-345) and CAT-060 sound meter retained behaviour [todo-346](../requirements.md#todo-346) (speaker → supplied meter); acoustic integration task [sequence-task-407](../requirements.md#sequence-task-407); interaction processes [IX-06 electrical power transfer](../requirements.md#interaction-06) (per-pulse debit), [IX-07 signal propagation](../requirements.md#interaction-07) (trigger) and [IX-12 acoustic propagation](../requirements.md#interaction-12) (first lesson around 72). Campaign: the sound row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "Speaker/pulse/continuous modes", first use 71–80 (reuse 81–100, 117–125, 136–150).

## 6. Open questions

1. **Pulse energy.** 2 J per pulse is a proposal; whether pulse energy scales with strength or tone: owner decision.
2. **Interval unit.** Whether the 24-tick minimum stays tick-based or becomes 0.2 s at every cadence: owner decision.
