# CAT-037 · latch — declaration readiness spec

Story 7.0 Batch B declaration spec (CAT-037-D). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [CAT-037](../requirements.md#current-cat-037). Supplied-network facts (N1–N24) are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

## 1. Identity

| Item | Value |
| --- | --- |
| CAT ID / kind | CAT-037 · `latch` ("Set/Reset latch") |
| Requirement anchor | [current-cat-037](../requirements.md#current-cat-037); retained behaviour [todo-267](../requirements.md#todo-267) |
| Mapped identities | none owned. Related: [EL-109 Speed-sensitive trapdoor](../invest/named-elements.md#element-109) mentions a "supplied latch" as part of its own mechanism; its requirement row stands as the source. |
| Roadmap story | 9.4 "State Latch Bistable Memory" (Epic 9) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root box 1.65 × 1.25 × 0.65 m at the origin; foot box 1.8 × 0.16 × 0.85 m centred at y −0.72 (`parts/LatchPart.cs@a6c914e:L56-L57`; legacy `AddBox` = collision box, [CAT-005 §2](CAT-005-battery.md#2-declaration)). |
| Mass and material | Static. Legacy static default material restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`). |
| Constraints and joints | none (the rocker is cosmetic). |
| Typed sockets and ports | `SetIn` Activation Input at (−0.45, 0.72, 0), command Set; `ResetIn` Activation Input at (0.45, 0.72, 0), command Reset; `PowerIn` Electrical Input (−0.93, 0, 0); `Supply` Electrical Output (0.93, 0, 0) (`parts/LatchPart.cs@a6c914e:L28-L34`). Electrical route `PowerIn`→`Supply` closed while memory is On (`parts/LatchPart.cs@a6c914e:L35-L36`). No activation output. Current `WorkshopSocket` lacks `SetIn`/`ResetIn` (legacy values 2 and 3: `engine/MachineData.cs@a6c914e:L103-L107`; current `engine/gpu/WorkshopConnections.cs@a6c914e:L9-L9`). |
| Sensors and activation | Two typed command inputs; a generic `Trigger` is rejected (`parts/LatchPart.cs@a6c914e:L37-L47`). Commands delivered on tick t settle at boundary t + 1; Reset dominates a same-tick Set. |
| Work and energy stores | Memory only: one bit (`Off`/`On`) that survives supply loss; the contact never generates supply (`parts/LatchPart.cs@a6c914e:L8-L9`, `parts/catalog/latch.tres@a6c914e:L11-L11`). |
| Parameters | none — no authored parameters (`parts/catalog/latch.tres@a6c914e:L1-L13` has no `Parameters`). |
| Cosmetic curves and UI bindings | (1) Rocker: navy bar rotates about Z by 0.5 rad between its two angles (rest −0.25 rad, On +0.25 rad) over 0.125 s SmoothStep, endpoint-driven by committed memory (`parts/LatchPart.cs@a6c914e:L19-L24`, `parts/LatchPart.cs@a6c914e:L59-L60`). (2) Lamp slate `#556573` → gold `#f7cb52` over 0.125 s SmoothStep, mirrors committed memory (`parts/LatchPart.cs@a6c914e:L18-L18`, `parts/LatchPart.cs@a6c914e:L21-L26`). Current cosmetic sources lack a latch-memory source (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L7`). |
| Art | Housing in catalogue colour `#66b8c9` (0.4, 0.72, 0.79); navy `#293954` foot; cream `#fff8e9` face 1.4 × 1.02 × 0.04 at z 0.35; navy rocker 0.65 × 0.28 × 0.12 at (0, 0, 0.44); lamp r 0.09 at (0, −0.33, 0.42); Set marker = raised navy bar 0.055 × 0.20 × 0.04 at (−0.45, 0.34, 0.4); Reset marker = hollow ring of 12 navy beads r 0.025 on radius 0.1 centred (0.45, 0.34, 0.4) — marks distinguish the sockets without text or colour; gold port spheres r 0.075 (`parts/LatchPart.cs@a6c914e:L53-L70`). Selection ring 1.1 (`parts/LatchPart.cs@a6c914e:L55-L55`). Toolbox icon (current, survives): `ui/WorkshopIcons.cs@a6c914e:L57-L57`. Design: "Set/Reset latch" row of [DESIGN.md · Motion and state feedback](../../../DESIGN.md#motion-and-state-feedback). |
| Catalogue / inventory | Id `latch`, Title "Set/Reset latch", Category Control, colour (0.4, 0.72, 0.79), no parameters; Description: "Set closes the electrical contact; Reset opens it. Reset wins simultaneous triggers. Remembers its state without power, but needs a connected supply to power another device." (`parts/catalog/latch.tres@a6c914e:L8-L13`). No level places or grants it. |

## 3. Engine capabilities

Families ([map row](../general-engine-element-map.md)): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction, TimedCommand (P0-022/023 shared evaluator/feedback registration).

**Exists now**
- Discrete activation network and its node kinds: `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`. Note: the current `ActivationNodeKind.Latch` is the Signal lamp's sticky "first input wins" node, not this Set/Reset memory; the two must not be conflated or share a name.
- Activation sockets and connection storage: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- Note: the network's `ActivationTime`/`ActivationLatch` and the cosmetic curve record store times and durations as binary16 (`Half`), listed as remaining f32 migration (`docs/gpu-f32-physics.md@a6c914e:L96-L98`). Extending them for the latch inherits that debt (open question 2).

**Missing**
- A Set/Reset memory node: typed `SetIn`/`ResetIn` inputs with per-socket commands, two request buckets (current boundary and next), next-boundary settlement with Reset dominance, memory persisting without new commands — Story 9.4.
- `SetIn`/`ResetIn` socket values and the icon-only socket choice in the wiring drawer — Story 9.4.
- Supplied contact `PowerIn`→`Supply` closed while On — Story 8.1 network (open question 1).
- Latch-memory cosmetic source for rocker and lamp — Story 9.4.

**Element dependencies**: activation sources for Set and Reset (Switch CAT-063); Battery (CAT-005) and a supplied consumer for any observable output ([current-consumers](../invest/current-consumers.md#cat-037-i) names Motor, CAT-042; the legacy tests used Powered gate, CAT-051). The latch is also the legacy supply-switching fixture for every supply-loss control ([CAT-005 N19](CAT-005-battery.md#4-legacy-harvest)).

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| T1 | Reset-dominant memory contact; commands settle at the next tick boundary so every delivery in a tick participates before the electrical solve. | `parts/LatchPart.cs@a6c914e:L8-L9` | carry forward |
| T2 | One memory slot; phase `Off`/`On`; reads `Off` before Run. | `parts/LatchPart.cs@a6c914e:L12-L15` | carry forward |
| T3 | Rocker and lamp bindings as in section 2. | `parts/LatchPart.cs@a6c914e:L16-L26` | carry forward |
| T4 | Ports with per-socket commands and the contact route, as in section 2. | `parts/LatchPart.cs@a6c914e:L27-L36` | carry forward |
| T5 | Set → Set request, Reset → Reset request, anything else rejects ("Latch requires an explicit Set or Reset input"); commands never act immediately. | `parts/LatchPart.cs@a6c914e:L37-L47` | carry forward the rule; do not carry forward the per-part `HandleActivation` callback |
| T6 | The part is active while On and records an `Activated` event. | `parts/LatchPart.cs@a6c914e:L48-L52` | carry forward the state; do not carry forward the per-part `BeforeNetworks` update loop |
| T7 | Geometry and art as in section 2. | `parts/LatchPart.cs@a6c914e:L53-L70` | carry forward |
| T8 | Requests are a flag set {None, Set, Reset}; state = (phase, current requests, next requests). | `engine/SimulationLatches.cs@a6c914e:L16-L21` | carry forward as typed state |
| T9 | Submit: a delivery on the current boundary tick goes to the current bucket, on the next tick to the next bucket; earlier or later deliveries reject. | `engine/SimulationLatches.cs@a6c914e:L56-L72` | carry forward |
| T10 | Advance (consecutive ticks only): Reset in the current bucket → Off; else Set → On; else keep; the next bucket becomes current. | `engine/SimulationLatches.cs@a6c914e:L73-L85` | carry forward |
| T11 | Set then Reset (or Reset then Set) delivered on the same tick, on either side of the boundary, settle Off; memory then persists for many ticks. | `CuriousContraptions.tests/SimulationLatchesTests.cs@a6c914e:L5-L23` | carry forward |
| T12 | Commands on adjacent ticks stay separate (Set at t then Reset at t + 1 gives On then Off); duplicate Sets are harmless; memory persists with no commands for 97 ticks. | `CuriousContraptions.tests/SimulationLatchesTests.cs@a6c914e:L25-L43` | carry forward |
| T13 | Unknown command, unknown id, negative or non-adjacent delivery ticks and non-consecutive advances reject without changing state. | `CuriousContraptions.tests/SimulationLatchesTests.cs@a6c914e:L68-L91` | carry forward |
| T14 | The supplied load is a Powered gate (`powered_gate`, CAT-051; `CuriousContraptions.tests/LatchTests.cs@a6c914e:L26-L26`). With and without supply, either entity order: Set reads Off after the command, Off after one step, On after the second step; the Powered gate is powered only with supply; supply off/on keeps memory and repowers the gate; same-tick Set+Reset in either order ends Off; Reset clears a pending Set; activating the latch with a generic command is rejected. Switch→latch has two compatible pairs (Set, Reset), so no suggestion. | `CuriousContraptions.tests/LatchTests.cs@a6c914e:L10-L71` | carry forward |
| T15 | One source wired to both `SetIn` and `ResetIn` delivers both commands (no per-part deduplication), so the result is Off. | `CuriousContraptions.tests/LatchTests.cs@a6c914e:L73-L88` | carry forward |
| T16 | Commands before Run are rejected; a pending Set is part of restorable state; `Trigger` is rejected; Workshop Reset gives Off. | `CuriousContraptions.tests/LatchTests.cs@a6c914e:L90-L120` | carry forward |
| T17 | Rocker and lamp follow only committed memory: at 0.0625 s the rocker has turned 0.25 rad and the lamp is half-way; a failed tick leaves them unchanged; supplied or not. | `CuriousContraptions.tests/LatchAnimationTests.cs@a6c914e:L35-L70` | carry forward behaviour; do not carry forward the CPU failure-injection mechanism |
| T18 | The contact follows committed memory, not the transient active flag; a delivered Set reads open until the following boundary; reads open after Reset. | `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L38-L83` | carry forward |
| T19 | Diagnostic wire names: phase `off`/`on`; requests `none`, `set`, `reset`, `set_and_reset`. | `CuriousContraptions.tests/LatchDiagnosticTests.cs@a6c914e:L7-L38` | carry forward at the diagnostics boundary only |
| T20 | Declaration requires owner and slot. | `engine/SceneLatchDeclaration.cs@a6c914e:L5-L14` | carry forward |
| T21 | Latch used as the supply-switching fixture (battery → latch contact; switches on Set and Reset). | `CuriousContraptions.tests/SupplyControl.cs@a6c914e:L22-L43` | carry forward as a construction recipe |
| T22 | Catalogue entry and scene. | `parts/catalog/latch.tres@a6c914e:L1-L13` and `parts/scenes/latch.tscn@a6c914e:L1-L6` | carry forward the data |

`reference/P0-022-before/docs/coverage/engine/*.json` list `latch` only as a catalogue id: no element knowledge. `engine/SceneLatchedSpringDeclaration.cs` and `engine/physics/PhysicsLatchedSpring.cs` belong to the wound spring (CAT-071), not this element.

### Files harvested

- `parts/LatchPart.cs`
- `parts/catalog/latch.tres`
- `parts/scenes/latch.tscn`
- `engine/SceneLatchDeclaration.cs`
- `engine/SimulationLatches.cs`
- `engine/MachineData.cs`
- `reference/cpu/MachinePart.cs`
- `CuriousContraptions.tests/LatchTests.cs`
- `CuriousContraptions.tests/SimulationLatchesTests.cs`
- `CuriousContraptions.tests/LatchAnimationTests.cs`
- `CuriousContraptions.tests/LatchDiagnosticTests.cs`
- `CuriousContraptions.tests/ElectricalContactBindingTests.cs`
- `CuriousContraptions.tests/SupplyControl.cs`

## 5. Acceptance outline

Authority: [CAT-037](../requirements.md#current-cat-037), [todo-267](../requirements.md#todo-267); Story 9.4 adds `tools/e2e/cat-037.test.ts`.

- **Construction (Chrome UI).** Place two Switches, Latch, Battery and a supplied consumer; Connect each switch to the latch choosing the raised-bar (Set) or ring (Reset) icon (T14); battery→latch `PowerIn`; latch `Supply`→consumer.
- **Positive.** A Set impact turns the memory On at the following boundary and it stays On indefinitely; rocker and lamp follow.
- **Negative / controls.** Reset impact turns it Off; simultaneous Set and Reset resolve Off (Reset dominance; T11, T15); memory On with supply absent leaves the consumer off, and restoring supply repowers it without a new command (T14).
- **Boundaries.** Commands on adjacent ticks stay separate (T12); a duplicate Set has no extra effect; generic trigger refused (T16).
- **Run/Reset.** Workshop Reset clears memory and any pending command (T14, T16).
- **Save/Load.** Both typed command links round-trip with exact socket names `set_in`/`reset_in`.
- **Integrations.** Used as the supply-loss control fixture for CAT-005/013/020/033/052 (T21).

## 6. Open questions

1. **Output domain in Story 9.4.** The Story 9.4 AC says "the latch output transitions to high", but the legacy and the requirement give the latch only a supplied electrical contact (no activation output); the supply network (Story 8.1) now precedes Story 9.4. Whether 9.4 proves the output through the Story 8.1 supplied contact, or adds an output the requirement does not define: unspecified — owner decision.
2. **binary16 activation and cosmetic lanes.** The activation network and cosmetic curves the latch extends still hold binary16 values (`docs/gpu-f32-physics.md@a6c914e:L96-L98`, remaining f32 migration). Whether Story 9.4 migrates those lanes to f32 before extending them, or extends them as they are and leaves migration to the scheduled f32 work: unspecified — owner decision.
