# CAT-020 · counter — declaration readiness spec

Story 7.0 Batch B declaration spec (CAT-020-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-020](../requirements.md#current-cat-020). Supplied-network facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-020 · `counter` |
| Requirement anchor | [current-cat-020](../requirements.md#current-cat-020); retained behaviour [todo-269](../requirements.md#todo-269) |
| Mapped identities | [EL-183 Resettable counter](../invest/named-elements.md#element-183) — partial match: EL-183 needs a separately addressed reset input, which the legacy counter lacks (open question 2). No TH/RAD/GAP identity. |
| Roadmap story | 9.2 "Pulse Counter Event Accumulator" (Epic 9) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root box 1.65 × 1.25 × 0.65 m at the origin; foot box 1.8 × 0.16 × 0.85 m centred at y −0.72 (`parts/CounterPart.cs@a6c914e:L56-L57`; legacy `AddBox` = collision box, [CAT-005 §2](CAT-005-battery.md#2-declaration)). |
| Mass and material | Static. Legacy static default material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`). |
| Constraints and joints | none |
| Typed sockets and ports | `ActivationIn` (0, 0.72, 0.1) and `ActivationOut` (0, −0.72, 0.1), Activation; `PowerIn` Electrical Input (−0.93, 0, 0); `Supply` Electrical Output (0.93, 0, 0) (`parts/CounterPart.cs@a6c914e:L28-L34`). Electrical route `PowerIn`→`Supply` closed while the count is Reached (`parts/CounterPart.cs@a6c914e:L35-L36`). |
| Sensors and activation | Receives Trigger activations; each accepted delivery increments once; the delivery that reaches the target emits one activation through `ActivationOut`; further deliveries are absorbed (`parts/CounterPart.cs@a6c914e:L43-L52`). |
| Work and energy stores | none; the contact never generates supply (`parts/CounterPart.cs@a6c914e:L8-L9`). |
| Parameters | `target_count`: integer 1–9 inclusive, default 3, unitless (`parts/CounterPart.cs@a6c914e:L37-L42`, `parts/catalog/counter.tres@a6c914e:L14-L14`). Carry forward as an integer parameter (not a float with a floor check). |
| Cosmetic curves and UI bindings | One dot lamp per target event, r 0.10, rows of three; lamp i turns slate `#556573` → gold `#f7cb52` when the committed count ≥ i + 1, 0.1 s SmoothStep presentation transition, endpoint-driven (`parts/CounterPart.cs@a6c914e:L21-L25`, `parts/CounterPart.cs@a6c914e:L59-L68`). Dot layout for lamp i (0-based): row = ⌊i / 3⌋; rows = ⌊(target + 2) / 3⌋; cols = min(3, target − 3·row) (`parts/CounterPart.cs@a6c914e:L59-L63`); x = (i mod 3 − (cols − 1)/2) · 0.36, y = ((rows − 1)/2 − row) · 0.3, z = 0.4 (`parts/CounterPart.cs@a6c914e:L64-L64`). Current cosmetic sources lack a counter-threshold source (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`). |
| Art | Housing in `#e8b764` (catalogue colour 0.91, 0.72, 0.39); navy `#293954` foot; cream `#fff8e9` face 1.4 × 1.02 × 0.04 at z 0.35; dot lamps; gold port spheres r 0.075 (`parts/CounterPart.cs@a6c914e:L53-L71`). Selection ring 1.1 (`parts/CounterPart.cs@a6c914e:L55-L55`). Toolbox icon (current, survives): `ui/WorkshopIcons.cs@a6c914e:L83-L83`. Design: "Counter" row of [DESIGN.md · Motion and state feedback](../../../DESIGN.md#motion-and-state-feedback). |
| Catalogue / inventory | Id `counter`, Title "Counter", Category Control, colour (0.91, 0.72, 0.39), Parameters `{"target_count": 3.0}`; Description: "Counts trigger events, lighting one dot per event. At three it emits once and keeps its electrical contact closed until Reset. Connect a supply to power downstream devices." (`parts/catalog/counter.tres@a6c914e:L8-L14`). No level places or grants it. |

## 3. Engine capabilities

Families ([map row](../general-engine-element-map.md)): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction, TimedCommand (P0-022/023 shared evaluator/feedback registration).

**Exists now**
- Discrete activation network: contact sources (Switch), orientation sources (Domino), sticky lamp nodes and delay timers; one emission per source per world; fan-out by edge; capacity 8 nodes: `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12` and `engine/gpu/ActivationNetwork.cs@a6c914e:L70-L231`.
- Activation sockets: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Note: the network's `ActivationTime`/`ActivationLatch` and the cosmetic curve record store times and durations as binary16 (`Half`), listed as remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L96-L98`). Extending them for the counter inherits that debt (open question 3).

**Missing**
- A counter node kind: accepts every delivery (not only the first), counts to an integer target, emits once at the target, saturates — Story 9.2. The current network rejects a second emission from any source ("Duplicate source emission", `engine/gpu/ActivationNetwork.cs@a6c914e:L206-L208`) and latches targets on the first input (`engine/gpu/ActivationNetwork.cs@a6c914e:L213-L216`), so counting needs a new node rule.
- Supplied contact (`PowerIn`→`Supply` closed while Reached) — Story 8.1 network (open question 1).
- Counter-threshold cosmetic source driving one lamp per target — Story 9.2.

**Element dependencies**: an activation source that can deliver several events in one world. The current Switch (CAT-063) cannot: once its contact source latches, later contact occurrences are ignored (`engine/gpu/ActivationNetwork.cs@a6c914e:L150-L156`). The legacy integration used the Ball detector (CAT-002, Story 6.8), one event per ball crossing (K12); it too needs repeated emission in the current network. Downstream Signal lamp (CAT-035) for the activation output; Battery (CAT-005) and a supplied consumer for the contact mode (the legacy tests used Powered gate, CAT-051).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| K1 | Counts activation deliveries, emits once at its target and latches an electrical contact; Workshop Reset clears the count; never generates supply. | `parts/CounterPart.cs@a6c914e:L8-L9` | carry forward |
| K2 | Readable state: count, target, phase (`Counting`/`Reached`). | `parts/CounterPart.cs@a6c914e:L14-L20` | carry forward |
| K3 | Ports and contact route as in section 2. | `parts/CounterPart.cs@a6c914e:L26-L36` | carry forward |
| K4 | Target must be a finite integer 1–9. | `parts/CounterPart.cs@a6c914e:L37-L42` | carry forward |
| K5 | Only `Trigger` commands are accepted; Reached → emit immediately; Accumulated or Saturated → no emission. | `parts/CounterPart.cs@a6c914e:L43-L52` | carry forward the rule; do not carry forward the per-part `HandleActivation` callback (declaration data on the shared network) |
| K6 | Dot-lamp layout and art. | `parts/CounterPart.cs@a6c914e:L53-L71` | carry forward |
| K7 | Phase is `Reached` exactly when count = target. Increment: at target → `Saturated` (unchanged); else count + 1 → `Reached` if now equal, else `Accumulated`. | `engine/SimulationCounters.cs@a6c914e:L16-L22` and `engine/SimulationCounters.cs@a6c914e:L56-L62` | carry forward |
| K8 | Declaration requires owner, slot and target ≥ 1. | `engine/SceneCounterDeclaration.cs@a6c914e:L5-L14` | carry forward |
| K9 | For targets 1, 3, 9: every delivery counts once; only the target-th returns Reached; 20 more deliveries are Saturated and leave the state unchanged. | `CuriousContraptions.tests/SimulationCountersTests.cs@a6c914e:L5-L20` | carry forward |
| K10 | Parameter wire name `target_count`; activating the counter before Run is rejected; restores to earlier counts are exact. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L17-L37` and `engine/MachineData.cs@a6c914e:L84-L84` | carry forward |
| K11 | Targets 1, 3, 9 with and without supply; the supplied load is a Powered gate (`powered_gate`, CAT-051; `CuriousContraptions.tests/CounterTests.cs@a6c914e:L54-L54`). One activation then 4 ticks counts exactly once (a held input is not extra events); active only at the target; the contact powers the Powered gate only when supplied and Reached; the activation output starts a downstream hold timer exactly once; 20 extra activations change nothing; Reset gives count 0, Counting, inactive, authored target. Counter→hold timer has two compatible pairs, so no suggestion. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L39-L88` | carry forward |
| K12 | Physical integration: ball detector → counter → lamp with 2 or 3 balls crossing the detector: 3 crossings reach the target and light the lamp, 2 do not; replay after Reset is identical, either id order. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L89-L152` | carry forward |
| K13 | Save/load keeps the authored target and never persists a partial run count. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L153-L171` | carry forward |
| K14 | Targets 0, −1, 10, 1.5, NaN and +∞ are rejected and the part is not added. | `CuriousContraptions.tests/CounterTests.cs@a6c914e:L172-L189` | carry forward |
| K15 | Every target 1–9 has exactly that many lamps; lamp i is keyed to threshold i + 1; a live (uncommitted) count does not light a lamp; after one tick the newest lamp is half-way at 0.05 s and fully gold at 0.1 s; supply does not affect the lamps. | `CuriousContraptions.tests/CounterAnimationTests.cs@a6c914e:L20-L60` | carry forward |
| K16 | The contact follows the committed counter state, not the part's transient active flag, and reads open again after Reset. | `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L38-L83` | carry forward |
| K17 | Diagnostic wire names `counting`/`reached`. | `CuriousContraptions.tests/CounterDiagnosticTests.cs@a6c914e:L7-L21` | carry forward at the diagnostics boundary only |
| K18 | Counter publication/rollback/allocation tests. | `CuriousContraptions.tests/CommittedCounterTests.cs@a6c914e:L11-L173` | do not carry forward (CPU transaction/publication mechanism; allocation proof belongs to the Epic 16 gate) |
| K19 | Catalogue entry and scene. | `parts/catalog/counter.tres@a6c914e:L1-L14` and `parts/scenes/counter.tscn@a6c914e:L1-L6` | carry forward the data |

`reference/P0-022-before/docs/coverage/engine/*.json` list `counter` only as a catalogue id: no element knowledge.

### Files harvested

- `parts/CounterPart.cs`
- `parts/catalog/counter.tres`
- `parts/scenes/counter.tscn`
- `engine/SceneCounterDeclaration.cs`
- `engine/SimulationCounters.cs`
- `engine/MachineData.cs`
- `reference/cpu/MachinePart.cs`
- `CuriousContraptions.tests/CounterTests.cs`
- `CuriousContraptions.tests/SimulationCountersTests.cs`
- `CuriousContraptions.tests/CounterAnimationTests.cs`
- `CuriousContraptions.tests/CounterDiagnosticTests.cs`
- `CuriousContraptions.tests/CommittedCounterTests.cs`
- `CuriousContraptions.tests/ElectricalContactBindingTests.cs`

## 5. Acceptance outline

Authority: [CAT-020](../requirements.md#current-cat-020), [todo-269](../requirements.md#todo-269); Story 9.2 adds `tools/e2e/cat-020.test.ts`.

- **Construction (Chrome UI).** Place a Ball detector (CAT-002, Story 6.8), Counter (target 3, the default) and Signal lamp, with balls that roll through the detector; Connect detector→counter `ActivationIn` and counter `ActivationOut`→lamp; pick the activation choice where the drawer offers trigger/power icons. Do not use a Switch as the source: it emits once per world (section 3).
- **Positive.** Three distinct balls cross the detector: the third crossing emits once and lights the lamp; three gold dots (K12).
- **Negative / controls.** Two crossings do not emit (K12); a held/resting trigger does not count every tick (K11); extra triggers after the target do not re-emit.
- **Boundaries.** Targets 1 and 9; invalid targets rejected (K14).
- **Run/Reset.** Reset clears the count to zero and the dots to slate (Story 9.2 AC; K11).
- **Save/Load.** `target_count` round-trips; partial counts are not saved (K13).
- **Integrations.** Supplied contact closes at the target and stays closed until Reset (needs Battery and a consumer); Clock→Counter (CAT-017 C17).

## 6. Open questions

1. **Supplied contact in Story 9.2.** The requirement and legacy include a supplied contact that latches until Reset; the supply network (Story 8.1) now precedes Story 9.2. Whether 9.2 delivers the contact mode with the activation-only mode or defers it, per the existing-element order row ("supply-consuming modes additionally require the next row's source/contact contract"): unspecified — owner decision.
2. **Separately addressed reset input (EL-183).** EL-183 requires a reset input; the legacy counter is cleared only by Workshop Reset and has no `ResetIn` socket. Whether the counter gains a reset socket, or EL-183 is a separate element: unspecified — owner decision.
3. **binary16 activation and cosmetic lanes.** The activation network and cosmetic curves the counter extends still hold binary16 values (`docs/gpu-f32-physics.md@a6c914e:L96-L98`, remaining f32 migration). Whether Story 9.2 migrates those lanes to f32 before extending them, or extends them as they are and leaves migration to the scheduled f32 work: unspecified — owner decision.
