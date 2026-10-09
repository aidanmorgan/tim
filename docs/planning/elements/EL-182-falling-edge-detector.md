# EL-182 · Falling-edge detector — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents. Edge-detector rules X1–X7 are in [EL-181](EL-181-rising-edge-detector.md#edge-detector-rules-shared-with-el-182).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-182 · Falling-edge detector |
| Type | Control |
| Anchor | [requirements.md#element-182](../requirements.md#element-182); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("edge detection"); [named-elements entry](../invest/named-elements.md#element-182); owner S403 |
| Related | Refines no CAT spec. Mirror of [EL-181 Rising-edge detector](EL-181-rising-edge-detector.md) — not a renamed rising detector (map composition). Typical inputs: [EL-198 Switch](EL-198-switch.md) contact, [CAT-060](CAT-060-sound_meter.md) meter contact going quiet, a ball leaving a pressure plate ([CAT-052](../requirements.md#current-cat-052)). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Same module body as EL-181: collider box 0.9 × 0.7 × 0.5 m at the origin, navy foot 1.0 × 0.12 × 0.6 m at (0, −0.41, 0) **proposed** (one module geometry; the glyph tells the polarity). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | `ConditionIn` (Electrical, Input) at (−0.55, 0, 0) and `ActivationOut` (Activation, Output) at (0.55, 0, 0) **proposed** (identical to EL-181 so the two swap without rewiring). |
| Sensors and activation | X2–X6 with polarity Falling: previous = false at admission; each committed tick after the electrical solve, previous true ∧ current false emits exactly one `ActivationOut` occurrence; then previous ← current. Because previous starts false, no falling edge is emitted at Run start even when the input is false. |
| Work and energy stores | none. |
| Parameters | none; `EdgePolarity` fixed to Falling for this catalogue entry. |
| Cosmetic curves and UI bindings | Output lamp 0.2 s flash per committed occurrence (Activation feedback source, `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`) and an input-availability lamp, as EL-181 **proposed** (same flash convention for both polarities). |
| Art | EL-181 body, with the navy step glyph falling left-to-right **proposed** (mirror glyph distinguishes polarity by shape, not colour). |
| Catalogue / inventory | Id `falling_edge`, Title "Falling edge", Category Control **proposed**. Description **proposed**: "Sends one trigger when its input turns off. Staying off sends nothing more." |

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S403): SignalPropagation. Map composition: "Reusable previous/current Boolean edge state emits once for true-to-false; not a renamed rising detector."

**Exists now**
- Activation output and occurrence records: `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L43`.

**Missing**
- Electrical condition input — Story 8.1; [S257](../invest/decisions.md#s257).
- Re-armable edge node emitting on true → false (same node kind as EL-181 with a typed polarity; the current network emits each source once, `engine/gpu/ActivationNetwork.cs@a6c914e:L199-L208`).

**Dependencies.** EL-181's edge node; a supplied condition (CAT-005 + EL-198); an activation consumer.

## 4. Sources and legacy

- Requirement row [element-182](../requirements.md#element-182): "Observes a true-to-false transition and emits one typed event"; outcome "Held false input produces no repeated transitions".
- Named entry [element-182](../invest/named-elements.md#element-182), owner S403; component research edge-detector rows ([component research](../../component-research.md#logic)).
- Legacy: no falling-edge detection anywhere. The closest fact is the Sound meter's quiet re-arm: once the level drops below 0.9 × threshold the state turns off silently and a later rise triggers again (`parts/SoundMeterPart.cs@a6c914e:L64-L71`) — the falling transition itself emits nothing in the legacy; this element adds that event.
- **Files harvested:** `parts/SoundMeterPart.cs` (re-arm fact); rules as listed in EL-181.

## 5. Acceptance outline

Acceptance authority: [element-182](../requirements.md#element-182), [electrical profile](../invest/profiles.md#electrical).

- **Construction (actual Chrome UI).** Battery → a contact that opens during Run (e.g. a supplied pressure plate the ball rolls off, or a latch reset) → detector `ConditionIn`; detector `ActivationOut` → Counter.
- **Positive.** The condition goes true then false: exactly one flash, at the falling tick; the counter counts 1.
- **Negative / control.** A condition false from Run start and staying false: no event (outcome; X2); a condition that only rises: no event; EL-181 on the same input fires at the rise instead (the two are distinct).
- **Boundaries.** On for exactly one tick then off: one event; repeated on/off cycles: one event per fall.
- **Run/Reset.** Reset clears `previous`; replay is tick-identical.
- **Save/Load.** Connections round-trip.
- **Integrations.** Interaction process [IX-07 signal propagation](../requirements.md#interaction-07); downstream counter retained behaviour [todo-269](../requirements.md#todo-269), which the edge detector feeds; upstream supplied conditions such as the sound meter contact going quiet [todo-346](../requirements.md#todo-346) or the switch contact [todo-147](../requirements.md#todo-147). Campaign: the logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "edge detection", first use 21–40 (reuse 41–80, 101–150); "a new rearm/reset mode needs its own objective".

## 6. Open questions

1. **Start state.** Whether `previous` should instead start from the first committed sample (so a condition true at start then falling still emits, which X2 already allows, but a false start never emits): confirm with the EL-181 first-tick decision (S402/S403).
