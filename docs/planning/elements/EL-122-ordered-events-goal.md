# EL-122 · Ordered-events goal — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-122 · Ordered-events goal · Goal |
| Anchor | [requirements.md#element-122](../requirements.md#element-122); [named-elements entry](../invest/named-elements.md#element-122); umbrella [gap-16](../requirements.md#gap-16) (index only) |
| Related identities | Siblings [EL-120](EL-120-quantity-goal.md), [EL-121](EL-121-rate-window-goal.md), [EL-123](EL-123-protected-end-state-goal.md). The current two-event `ActivatedAfter` goal (used by `delayed_signal`, [CAT-022 Delay](CAT-022-delay.md)) is its two-step special case. No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 49 "First Things First", practice 50, reuse 100, 146 ([gap-16](../requirements.md#gap-16)). |
| Status | Not started as an N-step goal; the 2-step `ActivatedAfter` exists (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L7-L24`). |

## 2. Declaration

The requirements row names no variants; this is the order variant of the gap-16 family. Authored level data, not a placed part.

- **Bodies and shapes.** None.
- **Mass and material.** None.
- **Constraints.** None.
- **Typed ports.** None. Each step names a typed event source: an `ActivationNodeId` (Switch, Domino orientation source, Delay output; `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L9`) or a capture `GpuSensorId` (Receiver delivery; `engine/gpu/CaptureLatch.cs@a6c914e:L8-L9`).
- **Sensors and activation.** No new sensor; the goal reads the committed once-only latches of its named sources. Each latch has one occurrence time per Run.
  - **Proposed** order rule: the sequence is satisfied when every step has latched and each step's occurrence time is strictly later than the previous step's by at least that step's `minimum_gap`. Solved at the last step's occurrence.
  - A step that latches before its predecessor fails the sequence for the Run (latches never clear before Reset), so a wrong order cannot later be "repaired": "Wrong order does not satisfy the sequence" ([element-122](../requirements.md#element-122)).
  - **Proposed** equal occurrence times (same ordinal and phase) do not satisfy order — strict ordering avoids ambiguous ties inside one 480 Hz step.
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `steps` | ordered list of typed event sources | 2–4 entries, distinct | — | — | **proposed** maximum 4 — the activation network holds 8 nodes (`engine/gpu/ActivationNetwork.cs@a6c914e:L72-L72`); four steps read clearly on a card |
  | `minimum_gap[i]` | `DurationSeconds` (f32) per step after the first | 0–120 | 0 | s | range from the existing `ActivatedAfter` bound (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L19-L21`); default **proposed** 0 — pure order unless a level asks for a delay |

- **Cosmetic curves and UI bindings.** Goal card shows numbered navy order marks; each turns gold as its step latches in order; an out-of-order latch shows a crossed mark ([gap-16 visual style](../requirements.md#gap-16)).
- **Art.** Cream card `#fff8e9`, navy order marks `#293954`, gold progress `#f7cb52` (`DESIGN.md@a6c914e:L147-L154`).
- **Catalogue and inventory.** Not a palette part. **Proposed** `WorkshopGoalKind.OrderedEvents`.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ObjectiveEvaluation, ReliablePublication, SignalPropagation, StateTransaction, TypedContracts.

- **Exists now.** Two-step ordered evaluation with minimum delay over latched activation nodes, using an exact integer clock (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L36-L76`); typed activation nodes and capture latches (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`, `engine/gpu/CaptureLatch.cs@a6c914e:L5-L40`).
- **Missing.** N-step sequences, mixed activation/capture sources and the out-of-order failure state: decision owner LAW-GOALS-I ([S635](../invest/decisions.md#s635)); owner S676.
- **Dependencies.** At least two event sources (Switch CAT-063, Receiver CAT-004, Domino CAT-023).

## 4. Sources and legacy

- **Requirements.** Row: "Tracks declared event identities in a required sequence"; outcome "Wrong order does not satisfy the sequence" ([element-122](../requirements.md#element-122)). Integration: reversed order is the near-miss control ([gap-16](../requirements.md#gap-16)).
- **Decisions.** [S635 goal-occurrences](../invest/decisions.md#s635): wrong-order routes do not solve.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | `ActivatedAfter`: target activation tick − source tick ≥ ceil(delay / tick); `PoweredAfter` additionally required the powered tick strictly after the trigger. | `reference/cpu/MachineWorld.cs@a6c914e:L934-L940` | Carry forward strict order and minimum gap; tick rounding does not carry forward. |
| 2 | Goal delay finite and within 0–120 s, validated before world replacement. | `reference/cpu/MachineWorld.cs@a6c914e:L372-L378` | Carry forward. |
| 3 | Levels using order: `delayed_signal` (`activated_after`), `switched_motor` and `delayed_solar` (`powered_after`). | `content/puzzles.json@a6c914e:L6575-L6864`, `content/puzzles.json@a6c914e:L3580-L3873`, `content/puzzles.json@a6c914e:L6865-L7293` | Carry forward as lesson references; `powered_after` needs the electrical domain (Story 8.1). |

Files consulted: `reference/cpu/MachineWorld.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-122](../requirements.md#element-122) and the [gap-16 integration](../requirements.md#gap-16).

- **Chrome recipe.** Authored order level: step 1 Switch A, step 2 Receiver capture, step 3 Switch B; the player places Ramps and a Domino through the palette.
- **Positive.** The ball strikes A, lands in the Receiver, the Domino then trips B: three gold marks, Solved once.
- **Negative or control.** Reorder the ramps so B fires before the capture: the card shows a crossed mark and the level stays unsolved for the Run, even after A and the capture occur.
- **Boundaries.** Two simultaneous latches in one step do not satisfy order; `minimum_gap` 0 and 120 admitted, 121 rejected; duplicate step sources rejected at the level boundary.
- **Run/Reset.** Reset clears every latch and the failure state.
- **Save/Load.** Steps and gaps round-trip with the level.
- **Integrations.** Goal-variant integration task [sequence-task-549](../requirements.md#sequence-task-549) (gap-16: reversed order is this variant's near-miss); activation and signal connection audit [sequence-task-279](../requirements.md#sequence-task-279) for the switch/detector/delay event sources; interaction row IX-07 signal propagation ([interaction-07](../requirements.md#interaction-07)). Campaign first use: GAP-16 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — order introduction 49 "First Things First" (after loading), practice 50, reuse 100, 146.

## 6. Open questions

1. Whether `ActivatedAfter` is deleted and its levels re-expressed as a 2-step `OrderedEvents` goal (one current path, no alias). Unspecified — owner decision.
2. Whether an out-of-order step fails the Run (proposed) or is simply ignored until the correct order occurs later.
3. Maximum step count (proposed 4).
