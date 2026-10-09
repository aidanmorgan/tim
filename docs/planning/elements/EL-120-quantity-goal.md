# EL-120 · Quantity goal — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-120 · Quantity goal · Goal |
| Anchor | [requirements.md#element-120](../requirements.md#element-120); [named-elements entry](../invest/named-elements.md#element-120); umbrella [gap-16](../requirements.md#gap-16) (index only) |
| Related identities | Siblings [EL-121](EL-121-rate-window-goal.md), [EL-122](EL-122-ordered-events-goal.md), [EL-123](EL-123-protected-end-state-goal.md). Delivery sensing comes from the Receiver ([EL-192](EL-192-receiving-basket.md), [CAT-004 basket](CAT-004-basket.md)); counting generalises the [CAT-020 Counter](CAT-020-counter.md). It extends the current `Captured` goal; no CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 25 "A Steady Job", practice 26, reuse 96, 136 ([gap-16](../requirements.md#gap-16)). |
| Status | Not started. Current goal kinds are `None`, `Captured` and `ActivatedAfter` (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L7-L7`). |

## 2. Declaration

The requirements row names no variants; this identity is the quantity variant of the gap-16 goal family. A goal is authored level data, not a placed part.

- **Bodies and shapes.** None. The goal observes deliveries into one named Receiver (existing geometry, `engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L18`).
- **Mass and material.** None.
- **Constraints.** None.
- **Typed ports.** None. The goal names its delivery source by typed identity (`GpuBodyId` of the Receiver), never by string.
- **Sensors and activation.** One residence sensor per (Receiver, candidate body) pair, as Free play already compiles (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L66-L77`). Each sensor's `CaptureLatch` latches once per Run (`engine/gpu/CaptureLatch.cs@a6c914e:L17-L30`).
  - Count = number of distinct candidate bodies whose latch for the named Receiver is `Latched`. A body that bounces out and back in, or rests and is sensed repeatedly, counts once: "Repeated sensing of one retained item cannot inflate totals" ([element-120](../requirements.md#element-120)).
  - Solved once, at the commit where the count first reaches the target; the occurrence time is that latch's event time.
- **Work and energy stores.** None.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `receiver` | `GpuBodyId` | an authored Receiver | — | — | typed identity rule ([AGENTS.md](../../../AGENTS.md)) |
  | `target` | u32 | 1–9 | 3 | items | **proposed** — 9 is the most dots the Counter face shows (`DESIGN.md@a6c914e:L292-L292`); 3 is the smallest count that clearly is not "one or two" |
  | `unit` | enum `GoalUnit { Item }` | closed set | `Item` | — | **proposed** — "explicit units"; mass or volume units arrive with granular/fluid goals |
  | `candidates` | set of `GpuBodyId` | 1–9 dynamic bodies | all authored balls | — | **proposed** — explicit candidate list so an unintended body cannot count |

- **Cosmetic curves and UI bindings.** Goal card shows navy quantity dots filling gold as count rises ← committed count; calm completion feedback ([gap-16 visual style](../requirements.md#gap-16)).
- **Art.** Cream goal card `#fff8e9`, navy dots `#293954`, gold progress `#f7cb52` (`DESIGN.md@a6c914e:L147-L154`).
- **Catalogue and inventory.** Not a palette part. **Proposed** `WorkshopGoalKind.Quantity` added to the closed enum (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L7-L7`) and the goal wire/save codec.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ObjectiveEvaluation, ReliablePublication, SignalPropagation, StateTransaction, TypedContracts.

- **Exists now.** Per-body residence sensors and once-only capture latches (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L66-L77`; `engine/gpu/CaptureLatch.cs@a6c914e:L5-L40`); a goal evaluator reading only committed events (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L7-L51`).
- **Missing.**
  - A `Quantity` goal kind with count over latches: decision owner LAW-GOALS-I ([S635 goal-occurrences](../invest/decisions.md#s635)).
  - Authored puzzles admit exactly one named Basketball (`engine/gpu/WorkshopPuzzle.cs@a6c914e:L65-L73`); a quantity level needs several candidates. Owner S674.
  - Capacity: 16 residence sensors per document (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L166-L169`), and a second Receiver is refused while any ball exists (`engine/gpu/WorkbenchCapacity.cs@a6c914e:L67-L72`).
- **Dependencies.** Receiver (CAT-004); several balls; optionally the CAT-020 Counter for an in-scene readout.

## 4. Sources and legacy

- **Requirements.** Row: "Counts distinct supported deliveries in explicit units"; outcome "Repeated sensing of one retained item cannot inflate totals" ([element-120](../requirements.md#element-120)). Integration: positive and near-miss (insufficient quantity); reject double counting and premature success; goals stay outcome-based ([gap-16](../requirements.md#gap-16)).
- **Audit.** "Individually specify quantity ... Reject transient false wins, double-counting" (`docs/physics-puzzle-gap-audit.md@a6c914e:L95-L95`).
- **Decisions.** [S635 goal-occurrences](../invest/decisions.md#s635): one level per variant; duplicate routes do not solve; Solved shows once.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Legacy events were a dictionary keyed by (kind, target, body) filled with `TryAdd`, so each event counted once at its first tick. | `reference/cpu/MachineWorld.cs@a6c914e:L915-L915`, `reference/cpu/MachineWorld.cs@a6c914e:L928-L942` | Carry forward the once-per-identity rule. |
| 2 | Legacy had no count goal; multi-delivery levels listed one `captured` goal per basket and required all. | `reference/cpu/MachineWorld.cs@a6c914e:L921-L942`; `content/puzzles.json@a6c914e:L8205-L8673` (`two_deliveries`) | Carry forward as context; a quantity goal is new. |
| 3 | Unknown goal kinds reject before the world is replaced; goals are immutable and round-trip through save. | `CuriousContraptions.tests/ObjectiveOwnershipTests.cs@a6c914e:L18-L83` | Carry forward. |
| 4 | Goal kinds were a string-converted enum (`GoalKindJsonConverter`). | `engine/MachineData.cs@a6c914e:L151-L154` | Do not carry forward the JSON string form; the enum stays typed end-to-end with conversion only at the save boundary. |

Files consulted: `reference/cpu/MachineWorld.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/ObjectiveOwnershipTests.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-120](../requirements.md#element-120) and the [gap-16 integration](../requirements.md#gap-16).

- **Chrome recipe.** Select an authored quantity level (target 2) with two locked Basketballs and one Receiver; place Ramps from the inventory through the palette and rotate ring.
- **Positive.** Both balls settle in the Receiver; the card fills two dots and Solved shows once.
- **Negative or control.** One ball delivered and the other missing: count 1, not solved. A ball that rattles in and out repeatedly still counts 1.
- **Boundaries.** Target 1 and 9 admitted; 0 and 10 rejected at the level boundary; a non-candidate body in the Receiver does not count.
- **Run/Reset.** Reset clears every latch and the count to 0.
- **Save/Load.** Goal kind, target and candidates round-trip with the level; save does not store the runtime count.
- **Integrations.** Goal-variant integration task [sequence-task-549](../requirements.md#sequence-task-549) (gap-16: positive and near-miss per variant — insufficient quantity here; reject double counting and premature success); interaction row IX-07 signal propagation ([interaction-07](../requirements.md#interaction-07)) for the typed event read. Campaign first use: GAP-16 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — quantity introduction 25 "A Steady Job", practice 26, reuse 96, 136.

## 6. Open questions

1. Whether a delivered body that later leaves the Receiver still counts (latch semantics, proposed yes) or the count is of currently retained bodies. Unspecified — owner decision.
2. Whether quantity can span several Receivers (one goal over a set of targets).
3. Units beyond `Item` (mass for granular GAP-06, volume for water).
