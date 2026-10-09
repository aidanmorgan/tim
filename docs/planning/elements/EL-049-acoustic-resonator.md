# EL-049 · Acoustic resonator — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared acoustic facts A1–A13 are in [EL-044](EL-044-tone-selective-sound-meter.md#shared-acoustic-family-facts).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-049 · Acoustic resonator |
| Type | Sound |
| Anchor | [requirements.md#element-049](../requirements.md#element-049); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-049); owner S536 |
| Related | Refines no CAT spec. Receiver pattern of [CAT-060 Sound meter](CAT-060-sound_meter.md) and [EL-044](EL-044-tone-selective-sound-meter.md) (band, supplied contact), with a stored excitation; [EL-052 Water-tuned bottle](EL-052-water-tuned-bottle.md) is a resonator whose band follows its fill. |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root body, collider box 0.8 × 1.0 × 0.8 m centred at (0, 0.1, 0) **proposed** (a jar-like cavity on a stand, half a Speaker cabinet); navy foot 0.9 × 0.14 × 0.9 m at (0, −0.47, 0) **proposed**. Receiver point (cavity mouth) at local (0, 0.55, 0), omnidirectional **proposed** (a cavity mouth has no strong facing; occlusion still applies, A6). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | `PowerIn` (Electrical, Input) (−0.5, −0.3, 0), `Supply` (Electrical, Output) (0.5, −0.3, 0), `ActivationOut` (Activation, Output) (0.5, 0.35, 0) **proposed** (the Sound meter's port set and left/right/top layout, `parts/SoundMeterPart.cs@a6c914e:L50-L55`, scaled to this body). |
| Sensors and activation | Excitation E ∈ [0, 1], committed per tick. Each admitted arrival occurrence of tone = `band` adds `gain` × arrival level once (per occurrence identity, S529), capped at 1; off-band arrivals add 0 **proposed** (simplest form of "off-band pulses do not accumulate"). Between arrivals E decays as E·e^(−dt/`decay_time`). Threshold state on at E ≥ `threshold`, off at E ≤ 0.9 × `threshold` (meter hysteresis, `parts/SoundMeterPart.cs@a6c914e:L64-L71`); a rising crossing emits one `ActivationOut` only if `PowerIn` is supplied (no replay, `parts/SoundMeterPart.cs@a6c914e:L72-L83`); the contact `PowerIn` → `Supply` is closed while on. |
| Work and energy stores | Stored excitation E (dimensionless acoustic store, FiniteLedger). It only decays; it never supplies electricity and never re-radiates sound **proposed** (no emission keeps it a pure store and avoids horn feedback loops). |
| Parameters | `band`: `ToneBand` (A1), default Mid **proposed** (matches the default speaker). `gain`: f32 0.1–1, default 0.4 **proposed** (at the 2 m test distance a speaker pulse arrives at level 1/(1 + 0.08·2²) = 0.758 and adds 0.303; with 0.5 s spacing and `decay_time` 1.5 s each gap retains e^(−0.5/1.5) = 0.717, so E after each pulse is 0.303, 0.520, 0.676 — three pulses cross the 0.6 threshold, two do not; right at the mouth, level 1, two pulses would already reach 0.687). `decay_time`: f32 0.2–5 s, default 1.5 s **proposed** (visibly rings down over a second or two, like the 0.7 s bell voice ×2, A11). `threshold`: f32 0.05–1, default 0.6 **proposed** (above a single pulse's contribution). |
| Cosmetic curves and UI bindings | Cavity glow and a vibrating membrane ← committed E (component research binding "Excitation ← committed level", [component research](../../component-research.md#sound)); output lamp slate `#556573` → gold `#f7cb52` 0.1 s SmoothStep on supplied output (meter convention, `parts/SoundMeterPart.cs@a6c914e:L43-L47`). |
| Art | Cream `#fff8e9` jar with a cyan `#66b8c9` inner glow, gold `#e8b764` rim, navy `#293954` stand and foot; band marks one/two/three raised navy bars (`parts/SpeakerPart.cs@a6c914e:L101-L102`); gold port spheres r 0.075. Geometry **proposed**. |
| Catalogue / inventory | Id `acoustic_resonator`, Title "Resonator jar", Category Sound **proposed**. Description **proposed**: "Builds up when sounds of its own marks arrive and slowly dies away. Supply electricity to trigger once it rings strongly enough." |

**Variants** (campaign coverage: "each tuned band"):

| Variant | `band` | Builds from | Ignores |
| --- | --- | --- | --- |
| Low | Low | Low arrivals | Mid, High |
| Mid | Mid | Mid arrivals | Low, High |
| High | High | High arrivals | Low, Mid |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S536): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction).

**Exists now**
- Static boxes: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`; activation output and network: `engine/gpu/ActivationNetwork.cs@a6c914e:L69-L108`.

**Missing**
- AcousticPropagation and reception — Stories 14.1–14.3; [S528](../invest/decisions.md#s528) (S530 arrival combination: whether simultaneous arrivals add).
- Per-occurrence accumulation with decay (a scalar store updated by events) — new; S529 event identity so one pulse is counted once.
- Supplied contact and re-arming activation — Story 8.1 network; repeated emission per run (`engine/gpu/ActivationNetwork.cs@a6c914e:L206-L208` forbids it today).

**Dependencies.** A toned source (Speaker CAT-061/EL-180, EL-179, Bell CAT-009); Battery for the contact.

## 4. Sources and legacy

- Requirement row [element-049](../requirements.md#element-049): "Stores and dissipates bounded acoustic excitation around a declared band"; outcome "Off-band pulses do not accumulate the same response". Scope index [todo-352](../requirements.md#todo-352): "a resonator visibly builds/loses excitation".
- Named entry [element-049](../invest/named-elements.md#element-049), owner S536; component research: "Resonator | Excitation level, matching band, decay | Builds under a matching tone, decays otherwise; passive resonance supplies no electricity" and "Acoustic resonator (EL-049) | Resonator + contact output | band, threshold" ([component research](../../component-research.md#sound)); [acoustic profile](../invest/profiles.md#acoustic): "a loop does not amplify".
- Legacy: no resonator or accumulating receiver. Facts reused: pulse model and strongest arrival (A1–A13), meter contact and hysteresis (`parts/SoundMeterPart.cs@a6c914e:L50-L83`).
- **Files harvested:** `parts/SoundMeterPart.cs`, `parts/SpeakerPart.cs` (tone marks); acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-049](../requirements.md#element-049), [acoustic profile](../invest/profiles.md#acoustic).

- **Construction (actual Chrome UI).** Battery → Speaker; Clock (CAT-017) → Speaker activation at 0.5 s; Resonator 2 m in front with `band` chosen through its configuration control; Battery → resonator `PowerIn`; `ActivationOut` → Signal lamp.
- **Positive.** Matching tone: the glow builds pulse by pulse (E = 0.303, 0.520, 0.676) and the lamp lights on the third pulse; after the clock stops the glow decays and the contact opens once E falls to 0.54.
- **Negative / control.** The same pulse train in another tone: E stays 0 (outcome); one or two matching pulses at 2 m do not trigger; no supply: glow builds but nothing triggers; a Wall between: no build.
- **Boundaries.** Each band separately; `gain`, `decay_time` and `threshold` limits; E never exceeds 1; hysteresis at 0.9 × threshold.
- **Run/Reset.** Reset sets E = 0 and clears the contact.
- **Save/Load.** All parameters and connections round-trip; E is not persisted.
- **Integrations.** Acoustic integration task [sequence-task-407](../requirements.md#sequence-task-407) ("a resonator visibly builds/loses excitation"); interaction processes [IX-12 acoustic propagation](../requirements.md#interaction-12), [IX-07 signal propagation](../requirements.md#interaction-07) (threshold activation) and [IX-06 electrical power transfer](../requirements.md#interaction-06) (supplied contact). Campaign: "resonator" is named in the sound row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 71–80 (reuse 81–100, 117–125, 136–150).

## 6. Open questions

1. **Re-radiation.** Whether a ringing resonator emits its own (weaker) sound: owner decision under S528/S529 (loops must not amplify).
2. **Off-band response.** Zero contribution (proposed) versus a reduced contribution: owner decision.
3. **Simultaneous arrivals.** Whether two in-band arrivals in one tick both add (additive) or only the strongest (A6): S530.
