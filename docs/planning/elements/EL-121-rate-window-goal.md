# EL-121 · Rate-window goal — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-121 · Rate-window goal · Goal |
| Anchor | [requirements.md#element-121](../requirements.md#element-121); [named-elements entry](../invest/named-elements.md#element-121); umbrella [gap-16](../requirements.md#gap-16) (index only) |
| Related identities | Siblings [EL-120 Quantity](EL-120-quantity-goal.md) (same delivery count, plus a time window), [EL-122](EL-122-ordered-events-goal.md), [EL-123](EL-123-protected-end-state-goal.md). Timing reuses the clock of the current `ActivatedAfter` goal. No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 29 "Going Steady", practice 30, reuse 97, 140 ([gap-16](../requirements.md#gap-16)). |
| Status | Not started (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L7-L7` has no rate kind). |

## 2. Declaration

The requirements row names no variants; this is the rate variant of the gap-16 family. Authored level data, not a placed part.

- **Bodies and shapes.** None; observes one Receiver as EL-120 does.
- **Mass and material.** None.
- **Constraints.** None.
- **Typed ports.** None; Receiver and candidates are typed `GpuBodyId`s.
- **Sensors and activation.** Per-(Receiver, body) residence latches as EL-120 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L66-L77`). Each latch carries its committed event time (ordinal and phase, `engine/gpu/CaptureLatch.cs@a6c914e:L8-L9`).
  - **Proposed** window rule: a delivery counts only if its latch time lies in the half-open simulation-time window [`start`, `start` + `length`). Solved once, at the commit where the in-window count first reaches `target`. A delivery before `start` never counts, even if the body stays in the Receiver — "output achieved before the valid interval" is rejected (`docs/physics-puzzle-gap-audit.md@a6c914e:L95-L95`).
  - Elapsed-time comparison uses the existing integer clock adapter (2^-36 of one 480 Hz step) so no time is rounded into a float (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L52-L76`).
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `receiver` | `GpuBodyId` | an authored Receiver | — | — | typed identity |
  | `target` | u32 | 1–9 | 3 | items | **proposed** — same bound as EL-120 |
  | `start` | `DurationSeconds` (f32) | 0–120 | 5 | s | range: current goal delay bound 0–120 s (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L19-L21`); default **proposed** — lets an early burst from the initial set-up fall outside the window |
  | `length` | `DurationSeconds` (f32) | 1–60 | 10 | s | **proposed** — long enough for a conveyor or dispenser to deliver three items at a steady pace, short enough to fit one Run |

- **Cosmetic curves and UI bindings.** Goal card shows a gold window bar advancing with committed simulation time, navy dots for in-window deliveries; deliveries outside the window show as hollow marks ([gap-16 visual style](../requirements.md#gap-16)).
- **Art.** Cream card `#fff8e9`, navy marks `#293954`, gold progress `#f7cb52` (`DESIGN.md@a6c914e:L147-L154`).
- **Catalogue and inventory.** Not a palette part. **Proposed** `WorkshopGoalKind.RateWindow` in the closed enum.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ObjectiveEvaluation, ReliablePublication, SignalPropagation, StateTransaction, TypedContracts.

- **Exists now.** Once-only latches with committed event time (`engine/gpu/CaptureLatch.cs@a6c914e:L5-L40`); integer-exact elapsed-time test (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L52-L76`); fixed 480 Hz canonical steps (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208-L208`).
- **Missing.** The `RateWindow` kind and window evaluation: decision owner LAW-GOALS-I ([S635](../invest/decisions.md#s635)). Several candidate balls per authored level (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L65-L73`): owner S675. A steady source of deliveries (Conveyor, Story 11.1; granular dispenser GAP-06).
- **Dependencies.** EL-120's count machinery; a repeating delivery mechanism.

## 4. Sources and legacy

- **Requirements.** Row: "Measures throughput over an explicit simulation-time interval"; outcome "An instantaneous burst outside the window fails" ([element-121](../requirements.md#element-121)). Integration: transient-only throughput is the near-miss control ([gap-16](../requirements.md#gap-16)).
- **Decisions.** [S635 goal-occurrences](../invest/decisions.md#s635): out-of-window routes do not solve.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Legacy timed goals compared first-occurrence ticks: `received − emitted ≥ ceil(delay / tick)`, delay 0–120 s. | `reference/cpu/MachineWorld.cs@a6c914e:L372-L378`, `reference/cpu/MachineWorld.cs@a6c914e:L934-L940` | Carry forward the 0–120 s bound and first-occurrence timing; do not carry forward tick rounding (the current clock is exact). |
| 2 | No legacy goal measured throughput. | `engine/MachineData.cs@a6c914e:L152-L152` | Recorded: the window rule is new. |

Files consulted: `reference/cpu/MachineWorld.cs`, `engine/MachineData.cs`.

## 5. Acceptance outline

Point of truth: [element-121](../requirements.md#element-121) and the [gap-16 integration](../requirements.md#gap-16).

- **Chrome recipe.** Authored rate level: window 5–15 s, target 3, a Conveyor feeding balls into a Receiver; the player places the release ramps through the palette.
- **Positive.** Three balls arrive between 5 s and 15 s; Solved shows once at the third arrival.
- **Negative or control.** All three balls dumped into the Receiver at 1 s (burst before the window): not solved even though three are retained. Two in-window and one at 16 s: not solved.
- **Boundaries.** A delivery exactly at `start` counts; one exactly at `start + length` does not. `length` 0 or above 60 rejected at the level boundary.
- **Run/Reset.** Reset clears latches, the in-window count and the window clock.
- **Save/Load.** Window parameters round-trip with the level.
- **Integrations.** Goal-variant integration task [sequence-task-549](../requirements.md#sequence-task-549) (gap-16: transient-only throughput is this variant's near-miss; reject premature success); interaction row IX-07 signal propagation ([interaction-07](../requirements.md#interaction-07)) for the typed event read. Campaign first use: GAP-16 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — rate introduction 29 "Going Steady" (after delay/clock), practice 30, reuse 97, 140.

## 6. Open questions

1. Fixed authored window (proposed) versus a sliding window ("any 10 s"). Unspecified — owner decision.
2. Whether a burst inside the window satisfies the goal or a minimum spacing between deliveries is required to express "steady".
3. Whether Solved fires at the target arrival (proposed) or only at window close.
