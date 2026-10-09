# EL-183 · Resettable counter — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Box sizes are full extents (legacy `AddBox` takes the full size and stores half-extents, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). The base counter harvest is [CAT-020](CAT-020-counter.md); this spec adds the separately addressed reset input.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-183 · Resettable counter |
| Type | Control |
| Anchor | [requirements.md#element-183](../requirements.md#element-183); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("counter/reset input"); [named-elements entry](../invest/named-elements.md#element-183); owner S404 |
| Related | Extends [CAT-020 Counter](CAT-020-counter.md) (partial match recorded there: the legacy counter has no reset socket). Reset socket modelled on [CAT-037 Latch](../requirements.md#current-cat-037) `ResetIn`. Inputs from [EL-181](EL-181-rising-edge-detector.md)/[EL-182](EL-182-falling-edge-detector.md), [CAT-002 Ball detector](../requirements.md#current-cat-002), [CAT-063 Switch](CAT-063-switch.md). |
| Roadmap story | unscheduled (the base counter is Story 9.2; the reset input has no story) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static body box 1.65 × 1.25 × 0.65 m at the origin and foot box 1.8 × 0.16 × 0.85 m at (0, −0.72, 0) (`parts/CounterPart.cs@a6c914e:L56-L57`). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none. |
| Typed ports | From the counter: `ActivationIn` (Activation, Input) (0, 0.72, 0.1); `ActivationOut` (Activation, Output) (0, −0.72, 0.1); `PowerIn` (Electrical, Input) (−0.93, 0, 0); `Supply` (Electrical, Output) (0.93, 0, 0) (`parts/CounterPart.cs@a6c914e:L28-L34`). New: `ResetIn` (Activation, Input, command Reset) at (0.62, 0.72, 0.1) **proposed** (top face beside the count input, as the latch's top-right `ResetIn`, `parts/LatchPart.cs@a6c914e:L31-L31`). |
| Sensors and activation | Count: each distinct accepted occurrence on `ActivationIn` increments once; the one that reaches `target_count` emits one `ActivationOut`; further ones saturate (`engine/SimulationCounters.cs@a6c914e:L56-L62`). Reset: an occurrence on `ResetIn` sets count = 0 and phase Counting, opens the contact, and emits nothing; it is never counted as an arrival **proposed** (the outcome sentence). Same-tick count and reset resolve reset-dominant: count = 0 after the tick and the coinciding arrival is not counted **proposed** (the latch is reset-dominant, `parts/LatchPart.cs@a6c914e:L8-L9`). Distinct occurrence identities (emitter, tick ordinal) prevent duplicate counting (map composition). |
| Work and energy stores | none; the contact `PowerIn` → `Supply` is closed while the phase is Reached (`parts/CounterPart.cs@a6c914e:L35-L36`) and never generates supply. |
| Parameters | `target_count`: integer 1–9, default 3 (`parts/CounterPart.cs@a6c914e:L37-L42`, `parts/catalog/counter.tres@a6c914e:L14-L14`) — an integer, not a float with a floor check. |
| Cosmetic curves and UI bindings | One dot per target event, slate `#556573` → gold `#f7cb52` when committed count ≥ i + 1, 0.1 s SmoothStep (`parts/CounterPart.cs@a6c914e:L21-L25`, `parts/CounterPart.cs@a6c914e:L59-L68`); on reset all dots ease back to slate on the committed tick. |
| Art | Counter art (`parts/CounterPart.cs@a6c914e:L53-L71`: gold `#e8b764` housing, navy foot, cream face, dot lamps, gold port spheres) plus a hollow navy ring beside `ResetIn` — the latch's Reset mark (`parts/LatchPart.cs@a6c914e:L62-L62`) **proposed**. |
| Catalogue / inventory | Id `reset_counter`, Title "Reset counter", Category Control **proposed** (a separate entry keeps CAT-020's three-port counter unchanged). Description **proposed**: "Counts trigger events like the counter. A trigger on the ring socket clears the count without counting as an event." |

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S404): SignalPropagation. Map composition: "Discrete integer counter state and separately addressed reset; distinct occurrence IDs avoid duplicate counting."

**Exists now**
- Activation network with occurrence identity (emitter, ordinal, phase) and Reset clearing at Run/Reset: `engine/gpu/ActivationNetwork.cs@a6c914e:L14-L43`, `engine/gpu/ActivationNetwork.cs@a6c914e:L109-L114`.

**Missing**
- Counter node (counts every delivery, emits once at target, saturates) — Story 9.2 (CAT-020); the current network rejects a second emission and latches targets on the first input (`engine/gpu/ActivationNetwork.cs@a6c914e:L206-L216`).
- A `Reset`-command input on a node and reset-dominant same-tick ordering — Story 9.4 (Latch) builds the Set/Reset node; this identity reuses it.
- Supplied contact — Story 8.1.

**Dependencies.** CAT-020 (Story 9.2), CAT-037 reset input (Story 9.4); activation sources for counting and resetting; Battery for the contact.

## 4. Sources and legacy

- Requirement row [element-183](../requirements.md#element-183): "Counts distinct supported arrivals and accepts a separately addressed reset input"; outcome "Reset clears the count without manufacturing a new arrival". [current-cat-020](../requirements.md#current-cat-020): target 1–9/default 3, emits threshold once, latches a separately supplied contact until Reset; held/busy/extra triggers cannot count every tick.
- Named entry [element-183](../invest/named-elements.md#element-183), owner S404.

| # | Legacy fact | Source | Disposition |
| --- | --- | --- | --- |
| R1 | Counter phase is Counting until count = target, then Reached; results Accumulated, Reached, Saturated. | `engine/SimulationCounters.cs@a6c914e:L16-L22` | carry forward as enums |
| R2 | Each target 1, 3, 9 with/without supply: each delivery counts once (held input not re-sampled), Reached emits once, the contact powers a load only with supply, 20 extra deliveries change nothing, Reset restores count 0. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L39-L88` | carry forward |
| R3 | Three physical detector crossings are needed; replay is identical regardless of entity order. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L110-L152` | carry forward |
| R4 | The authored target round-trips; a partial run count is not persisted; invalid targets (−1, 10, 1.5, NaN, ∞) reject. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L153-L189` | carry forward |
| R5 | The legacy counter is cleared only by Workshop Reset; it has no reset socket. | `parts/CounterPart.cs@a6c914e:L8-L9`, `parts/CounterPart.cs@a6c914e:L28-L34` | the reset input is new in EL-183 |
| R6 | Latch `ResetIn` is a typed Activation input carrying the Reset command; commands settle at the next tick boundary; reset dominates. | `parts/LatchPart.cs@a6c914e:L8-L9`, `parts/LatchPart.cs@a6c914e:L31-L31` | carry forward |

- **Files harvested:** `parts/CounterPart.cs`, `parts/catalog/counter.tres`, `engine/SimulationCounters.cs`, `CuriousContraptions.tests/CounterTests.cs`, `parts/LatchPart.cs`, `reference/cpu/MachinePart.cs` (`AddBox` extents).

## 5. Acceptance outline

Acceptance authority: [element-183](../requirements.md#element-183), [current-cat-020](../requirements.md#current-cat-020).

- **Construction (actual Chrome UI).** Ball detector → Reset counter `ActivationIn`; a Switch → `ResetIn` (choose `ActivationOut → ResetIn`); counter `ActivationOut` → Signal lamp; Battery → `PowerIn`, `Supply` → supplied load.
- **Positive.** Three balls cross: dots light 1, 2, 3; the lamp lights once; the load runs.
- **Negative / control.** A ball presses the reset switch: count returns to 0, dots slate, contact opens, no lamp event, and the count stays 0 until a real arrival (outcome). Reset with count already 0 changes nothing. A held input counts once.
- **Boundaries.** Reset and arrival in the same tick → 0 (reset dominance); target 1 and 9; reset after saturation then three more arrivals reaches target again and emits a second time.
- **Run/Reset.** Workshop Reset restores count 0 exactly.
- **Save/Load.** `target_count` and all five connections round-trip; the run count is not persisted.
- **Integrations.** CAT-020 counter retained behaviour [todo-269](../requirements.md#todo-269) (distinct events, saturation, threshold once, supplied contact until Reset); interaction processes [IX-07 signal propagation](../requirements.md#interaction-07) (count and reset inputs) and [IX-06 electrical power transfer](../requirements.md#interaction-06) (supplied contact). Campaign: the logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "counter/reset input", first use 21–40 (reuse 41–80, 101–150); "a new rearm/reset mode needs its own objective", so the reset input needs a lesson of its own.

## 6. Open questions

1. **Separate entry or extended counter.** A new `reset_counter` entry (proposed) versus adding `ResetIn` to CAT-020: owner decision (also CAT-020 open question 2).
2. **Same-tick order.** Reset-dominant (proposed) versus applying the arrival after the reset (count 1): owner decision.
3. **binary16 activation and cosmetic lanes.** The activation times, occurrence phases and dot cosmetic curves this counter extends still hold binary16 values, listed as remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L96-L98`). Whether the slice migrates them to f32 first or extends them as they are: [CAT-020 open question 3](CAT-020-counter.md#6-open-questions), owner decision.
