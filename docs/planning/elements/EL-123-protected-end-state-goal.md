# EL-123 · Protected-end-state goal — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-123 · Protected-end-state goal · Goal |
| Anchor | [requirements.md#element-123](../requirements.md#element-123); [named-elements entry](../invest/named-elements.md#element-123); umbrella [gap-16](../requirements.md#gap-16) (index only) |
| Related identities | Siblings [EL-120](EL-120-quantity-goal.md), [EL-121](EL-121-rate-window-goal.md), [EL-122](EL-122-ordered-events-goal.md). Reuses the Domino orientation sensor ([CAT-023](CAT-023-domino.md)) and Receiver residence ([EL-192](EL-192-receiving-basket.md)); later consumers include fragile cargo (EL-084) and coated/dyed cargo ([EL-118](EL-118-coating-station.md), [EL-119](EL-119-dye-station.md)). No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 89 "Leave It Lovely", practice 90, reuse 100, 150 ([gap-16](../requirements.md#gap-16)). |
| Status | Not started (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L7-L7`). |

## 2. Declaration

The requirements row names no variants; this is the end-state variant of the gap-16 family. It is a constraint combined with the level's delivery goals: the level solves only when every delivery goal is met and every protected predicate holds once the machine has settled.

- **Bodies and shapes.** None of its own. Each region predicate declares an authored axis-aligned box in a static frame.
- **Mass and material.** None.
- **Constraints.** None.
- **Typed ports.** None; protected bodies are typed `GpuBodyId`s.
- **Sensors and activation.** One predicate per protected body, from a closed set:
  - `InsideRegion`: body centre inside an authored box — reuses `ResidenceSensorDeclaration` bounds (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L122-L141`).
  - `Upright`: body within `tilt_limit` of its admitted orientation — reuses the orientation threshold |⟨q, q₀⟩| ≤ cos(θ/2) (`engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L7-L27`), read as "not exceeded".
  - `NotEscaped`: body never left the bench volume.
  - **Settle rule (proposed).** The machine is settled when every dynamic body's speed stays below 0.05 m/s for 1.0 s — 0.05 m/s is the legacy settle bound a ball on the bench must reach (`CuriousContraptions.tests/FloorTests.cs@a6c914e:L47-L47`); 1 s outlasts a Domino wobble. Predicates are evaluated at the first settled commit after all delivery goals latch.
  - "Delivery alone cannot bypass an unsafe or unfinished terminal state" ([element-123](../requirements.md#element-123)): if a predicate fails at settle, the level stays unsolved for the Run.
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `predicates` | list of (`GpuBodyId`, enum `ProtectedPredicate { InsideRegion, Upright, NotEscaped }`) | 1–4 | — | — | **proposed** — three predicates cover "unsafe" (fallen, escaped) and "unfinished" (not in place); 4 keeps the card readable |
  | `region_min`, `region_max` | `MetreVector` (f32) | within ±16 m, min < max | — | m | bounds rule from residence sensors (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L134-L137`) |
  | `tilt_limit` | f32 | 1°–179° | 30° | degrees | **proposed** range and default — under f32 the half-angle cosine resolves small angles, so 1°–179° replaces the binary16 4°–179° bound (fact 4); 30° sits below the Domino's 45° tip threshold, so a toppled piece fails |
  | `settle_speed`, `settle_time` | f32 | fixed | 0.05, 1.0 | m/s, s | speed from `CuriousContraptions.tests/FloorTests.cs@a6c914e:L47-L47`; time **proposed** (see settle rule) |

- **Cosmetic curves and UI bindings.** Goal card shows a small on-object gauge per protected body: navy outline while unsettled, gold when its predicate holds at settle; a failed predicate shows a shaped warning mark, not colour only ([gap-16 visual style](../requirements.md#gap-16)).
- **Art.** Cream card `#fff8e9`, navy marks `#293954`, gold progress `#f7cb52` (`DESIGN.md@a6c914e:L147-L154`).
- **Catalogue and inventory.** Not a palette part. **Proposed** `WorkshopGoalKind.ProtectedEndState`.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ObjectiveEvaluation, ReliablePublication, SignalPropagation, StateTransaction, TypedContracts.

- **Exists now.** Residence-region sensors and orientation-threshold sensors evaluated at substep endpoints (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L122-L141`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1370-L1372`); committed f32 body velocities are published every tick.
- **Missing.**
  - A settle detector and an end-state goal combinator (all delivery goals AND all predicates at settle): owner LAW-GOALS-I ([S635](../invest/decisions.md#s635)), S677.
  - Orientation sensors exist only for wired Dominoes (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L53-L55`); goal-owned sensors on any protected body are missing.
  - An escape volume: the current engine has no world bounds event. Owner S677.
- **Dependencies.** At least one delivery goal (Captured, EL-120) and a protected body (Domino, ball).

## 4. Sources and legacy

- **Requirements.** Row: "Requires the observed final machine state to remain within declared bounds"; outcome "Delivery alone cannot bypass an unsafe or unfinished terminal state" ([element-123](../requirements.md#element-123)). Integration: an unsafe final state is the near-miss control; reject premature success ([gap-16](../requirements.md#gap-16)).
- **Audit.** "protected end-state constraints as typed goal variants. Reject transient false wins" (`docs/physics-puzzle-gap-audit.md@a6c914e:L95-L95`).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Legacy escape bounds: a body with centre y < −5 or y > 20, |x| > 18 or |z| > 12 was hidden, its collider disabled, and an `Escaped` event recorded once. | `reference/cpu/MachineWorld.cs@a6c914e:L903-L916` | Carry forward the bounds as the `NotEscaped` volume; the per-tick CPU loop and hiding do not carry forward. |
| 2 | A ball, Bowling ball or Tennis ball dropped on the bench bounces, settles below 0.05 m/s and never raises `Escaped`; bodies placed off the finite bench (x = 10 or z = 6) fall and raise `Escaped`. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L51`, `CuriousContraptions.tests/FloorTests.cs@a6c914e:L53-L67` | Carry forward as boundary acceptance. |
| 3 | Legacy `Won` latched at the first tick when all goals were met and stopped the run; nothing checked the state afterwards. | `reference/cpu/MachineWorld.cs@a6c914e:L921-L925` | Do not carry forward: this is the "premature success" the end-state goal forbids. |
| 4 | The orientation threshold admits only 4°–179° because the binary16 rounding of cos(θ/2) cannot represent smaller angles. | `engine/gpu/OrientationSensorDeclaration.cs@a6c914e:L11-L12` | Do not carry forward: a binary16 artefact; the f32 range is proposed above. |

Files consulted: `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/FloorTests.cs`.

## 5. Acceptance outline

Point of truth: [element-123](../requirements.md#element-123) and the [gap-16 integration](../requirements.md#gap-16).

- **Chrome recipe.** Authored level: deliver a Basketball to a Receiver while a protected Domino stands beside the route (`Upright`) and a Bowling ball must stay on a shelf (`InsideRegion`); the player places Ramps.
- **Positive.** The ball is captured, the Domino stays upright, the Bowling ball stays put; after the settle period Solved shows once.
- **Negative or control.** A route that captures the ball but knocks the Domino over: delivery latches, the level stays unsolved. A route that pushes the Bowling ball off the bench fails `NotEscaped`.
- **Boundaries.** A Domino tilted 29° at settle passes, 31° fails (`tilt_limit` 30°); a machine still moving never reaches the settle check.
- **Run/Reset.** Reset clears settle timers, predicate results and goal state.
- **Save/Load.** Predicates and regions round-trip with the level.
- **Integrations.** Goal-variant integration task [sequence-task-549](../requirements.md#sequence-task-549) (gap-16: an unsafe final state is this variant's near-miss; reject premature success); outcome-predicate task [sequence-task-361](../requirements.md#sequence-task-361) (broken, arrived-safely and related predicates follow actual events); interaction row IX-07 signal propagation ([interaction-07](../requirements.md#interaction-07)). Campaign first use: GAP-16 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — end-state introduction 89 "Leave It Lovely" (after thermal control), practice 90, reuse 100, 150.

## 6. Open questions

1. Settle rule (proposed 0.05 m/s for 1 s) versus an authored end time. Unspecified — owner decision.
2. Whether `NotEscaped` uses the legacy bounds or the bench footprint (16.8 × 9.8 m).
3. Whether material-state predicates (coated, dyed, intact) join this goal or get their own goal kind.
