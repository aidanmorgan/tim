# EL-181 · Rising-edge detector — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents. This spec holds the **edge-detector rules X1–X7** that [EL-182 Falling-edge detector](EL-182-falling-edge-detector.md) reuses.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-181 · Rising-edge detector |
| Type | Control |
| Anchor | [requirements.md#element-181](../requirements.md#element-181); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("edge detection"); [named-elements entry](../invest/named-elements.md#element-181); owner S402 |
| Related | Refines no CAT spec. Reads a supplied condition ([EL-198 Switch](EL-198-switch.md) contact, [EL-133](EL-133-electrical-and-gate.md)–[EL-137](EL-137-electrical-nand-gate.md) gate outputs, [CAT-060](CAT-060-sound_meter.md) meter contact); feeds activation consumers ([CAT-020 Counter](CAT-020-counter.md) / [EL-183](EL-183-resettable-counter.md), [CAT-035 lamp](../requirements.md#current-cat-035), [CAT-022 Delay](CAT-022-delay.md)). Mirror element [EL-182](EL-182-falling-edge-detector.md). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root body, collider box 0.9 × 0.7 × 0.5 m at the origin **proposed** (a small control module, about half the 1.65 m Counter, `parts/CounterPart.cs@a6c914e:L56-L56`); navy foot 1.0 × 0.12 × 0.6 m at (0, −0.41, 0) **proposed**. |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | `ConditionIn` (Electrical, Input) at (−0.55, 0, 0) and `ActivationOut` (Activation, Output) at (0.55, 0, 0) **proposed** (left-in/right-out convention of the gates and the Delay, `engine/gpu/WorkshopConnections.cs@a6c914e:L51-L52`). `ConditionIn` reads availability only; it never passes power onward (X1). |
| Sensors and activation | X2–X6: previous = false at admission; each committed tick after the electrical solve, current = committed availability of `ConditionIn`; previous false ∧ current true emits exactly one typed `ActivationOut` occurrence on that tick; then previous ← current. |
| Work and energy stores | none; the detector creates no electrical power and its event carries no energy. |
| Parameters | none. The edge polarity is fixed per catalogue entry by the closed enum `EdgePolarity { Rising, Falling }` = Rising. |
| Cosmetic curves and UI bindings | Output lamp flashes slate `#556573` → gold `#f7cb52` for 0.2 s on each committed occurrence, using the existing Activation feedback source (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`) **proposed** (a flash, not a held state, shows "one event"); an input lamp follows committed `ConditionIn` availability (gate input-lamp convention, `parts/ElectricalLogicPart.cs@a6c914e:L84-L85`). |
| Art | Cream `#fff8e9` face on a cyan `#66b8c9` body (control-module colours of the gates), navy `#293954` raised step glyph rising left-to-right, navy foot, gold `#f7cb52` electrical port sphere and gold `#e8b764` activation socket ring **proposed**. |
| Catalogue / inventory | Id `rising_edge`, Title "Rising edge", Category Control **proposed** (Control is the legacy category of logic modules, `parts/catalog/both_gate.tres@a6c914e:L8-L8`). Description **proposed**: "Sends one trigger when its input turns on. Holding it on sends nothing more." |

**Variants.** The requirements row names no variant; the base declaration is the only required mode (the falling polarity is its own identity, EL-182).

### Edge-detector rules (shared with EL-182)

All **proposed** except where a source is cited.

| # | Rule |
| --- | --- |
| X1 | The input is a condition: the detector samples committed availability and is a sink; no power flows from `ConditionIn` to any output (gate condition rule, `engine/BinaryCircuit.cs@a6c914e:L136-L138`). |
| X2 | `previous` starts false at admission and on Reset, matching the network's unpowered start (`engine/BinaryCircuit.cs@a6c914e:L29-L34`). A condition true on the first committed tick is therefore a rising edge (EL-181) and never a falling edge (EL-182). |
| X3 | Sampling happens once per committed tick, after the electrical solve, so every same-tick change participates and order of part ids cannot change the result. |
| X4 | Rising: false → true emits one occurrence; Falling: true → false emits one occurrence. |
| X5 | A held value emits nothing further; re-arming needs at least one committed tick in the opposite state (no debounce time beyond one tick). |
| X6 | An occurrence is emitted on the tick of the transition; downstream activation consumers apply it at their next tick boundary (latch rule, `parts/LatchPart.cs@a6c914e:L8-L9`). Missed transitions are never replayed (research: "missed events are not replayed on power restoration"). |
| X7 | Each occurrence has a distinct identity (emitter, tick ordinal) so a counter counts it once (EL-183 contract). |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S402): SignalPropagation. Map composition: "Reusable previous/current Boolean edge state emits once for false-to-true; ordering/Reset are explicit."

**Exists now**
- Activation output sockets, occurrence records with ordinal/phase timestamps and the bounded discrete network: `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L43`, `engine/gpu/ActivationNetwork.cs@a6c914e:L69-L108`.

**Missing**
- Electrical condition input — supplied network, Story 8.1; condition role [S257](../invest/decisions.md#s257).
- A re-armable edge source node that emits many occurrences per run: the current network emits each source once ("Every source emits once", `engine/gpu/ActivationNetwork.cs@a6c914e:L199-L208`) — new node kind, unscheduled.
- Activation network capacity beyond 8 nodes for larger logic (`engine/gpu/ActivationNetwork.cs@a6c914e:L72-L72`).

**Dependencies.** Battery and a switched condition (CAT-005, EL-198) or any supplied contact; an activation consumer to observe (lamp, counter).

## 4. Sources and legacy

- Requirement row [element-181](../requirements.md#element-181): "Observes a false-to-true transition and emits one typed event"; outcome "Held true input produces no repeated transitions". Campaign coverage reserves "edge detection" in 21–40.
- Named entry [element-181](../invest/named-elements.md#element-181), owner S402; component research: "Edge detector (EL-181, EL-182) | Rising/falling edge → one pulse | Counters never increment every tick; missed events are not replayed on power restoration" and "Electrical node + edge → pulse | One pulse per held-condition change | Lamp ← pulse" ([component research](../../component-research.md#logic)).

| # | Legacy fact | Source | Disposition |
| --- | --- | --- | --- |
| L1 | The Sound meter computes a rising crossing as `above ∧ ¬wasAbove` and emits one activation only on that tick and only if supplied; the pending flag is cleared afterwards, so a held level never re-emits. | `parts/SoundMeterPart.cs@a6c914e:L64-L71`, `parts/SoundMeterPart.cs@a6c914e:L72-L83` | carry forward the edge rule as a reusable node, not a per-part loop |
| L2 | A counter's threshold emits once and later deliveries saturate rather than re-emitting. | `engine/SimulationCounters.cs@a6c914e:L56-L62` | carry forward (one event per transition) |
| L3 | No standalone edge detector part, scene, level or lesson exists. | search of `parts/`, `content/puzzles.json`, `tools/Campaign` for "edge", "rising", "falling" | — |

- **Files harvested:** `parts/SoundMeterPart.cs`, `engine/SimulationCounters.cs`, `engine/BinaryCircuit.cs`, `parts/LatchPart.cs`.

## 5. Acceptance outline

Acceptance authority: [element-181](../requirements.md#element-181), [electrical profile](../invest/profiles.md#electrical).

- **Construction (actual Chrome UI).** Battery → Switch contact (EL-198) → detector `ConditionIn`; detector `ActivationOut` → Counter `ActivationIn` (choose the socket pair); a ball presses the switch.
- **Positive.** The switch closes: the detector lamp flashes once and the counter counts 1.
- **Negative / control.** The switch stays closed for the rest of the run: no further flashes, the counter stays at 1 (outcome). A condition that is never true: no event. Wiring a battery `Supply` straight to the detector emits exactly one event at the first tick (X2) and none after.
- **Boundaries.** Toggle off for exactly one tick then on: a second event (X5); two transitions in consecutive ticks give two events with distinct identities (X7).
- **Run/Reset.** Reset clears `previous` and the lamp; replay is tick-identical.
- **Save/Load.** Connections round-trip; `previous` is never persisted.
- **Integrations.** Interaction process [IX-07 signal propagation](../requirements.md#interaction-07) ("15 for a simple sensor signal; logic follows 21"); downstream counter retained behaviour [todo-269](../requirements.md#todo-269) ("Held/busy/extra triggers cannot count every tick"), which the edge detector feeds; upstream supplied conditions via the switch contact [todo-147](../requirements.md#todo-147) and the gates [todo-362](../requirements.md#todo-362). Campaign: the logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "edge detection", first use 21–40 (reuse 41–80, 101–150); "a new rearm/reset mode needs its own objective".

## 6. Open questions

1. **First-tick edge.** Whether a condition already true when Run starts counts as a rising edge (proposed, X2) or the detector arms silently: owner decision under S402.
2. **Input domain.** Electrical condition input (proposed) versus also accepting an activation input: owner decision.
3. **binary16 activation and cosmetic lanes.** The occurrence timestamps (ordinal and phase) and the flash cosmetic curve this detector emits through still hold binary16 values, listed as remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L96-L98`). Whether the edge-node slice migrates them to f32 first or extends them as they are: the same decision as [CAT-020 open question 3](CAT-020-counter.md#6-open-questions), owner decision. EL-182 inherits this question.
