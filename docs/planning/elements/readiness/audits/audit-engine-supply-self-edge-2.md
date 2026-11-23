# Planning audit, second pass: spec-engine-supply-self-edge-cycle-rule.md

Auditor: informed engineering auditor, bounded planning check. Phase: planning. Checkout: `.claude/worktrees/spec-drafts` at `8d1d68aa`; product code unchanged since the first pass (no modified file under `engine/`, `CuriousContraptions.Bundle/`, `CuriousContraptions.Abstractions/` or `ui/`). Contract re-read in full: 3,394 words (budget 6,000). Read-only: nothing built, no test run. Planned tests are not executed proof.

## Verdict: Scoped Pass (planning)

Scope: the contract's design, intent, capability probe, owned scope, planned acceptance and verification, for the stage "ready for development". It certifies no implementation. No blocking finding remains. Three small text fixes are listed below; none touches the intent contract, and none needs another audit.

Deferred gates, unchanged: executed tests, the full local round and hooks, implementation review at the engine tier (six layers), and browser qualification at the post-catalogue E2E phase.

## First-pass findings: disposition

| Finding | State | Evidence (contract line) |
|---|---|---|
| B1 row S8 | Fixed | line 53: logic monotone on any input terminal or contact on `PowerIn` admitted; nonmonotone `NonmonotoneCycle`; source, consumer, contact condition terminal and duplicate `Edge.Terminal`. Matches `SupplyContractValues.cs:58`–`:69` |
| B2 connect tool | Fixed | lines 63, 99, 125 (decision 6), 158 (answer 8), 193 (no self-wire browser case queued) |
| B3 synthetic settle proof | Fixed | line 186 law row in three declaration orders; line 156 |
| B4 surviving refusals pinned | Fixed | line 188 (source and consumer self-edge on `PowerIn`, contact self-edge on both condition terminals); line 189 (bundle layer) |
| N1 controls | Fixed | lines 182, 184, 187 |
| N2 citations | Fixed | lines 81, 82 |
| N3 worker admission note | Fixed | line 185 |
| N4 unreachable branch | Fixed, verified below | lines 36, 90, 157 |
| N5 every terminal | Fixed | line 121 |
| N6 ordering | Decided by the coordinator and recorded | lines 59, 63 |
| N7 public entry point | Fixed | line 189 |

## The deleted ownerless loop branch is dead code, and deleting it preserves behaviour

Checked in `engine/gpu/SupplyRoute.cs`:

1. `ValidateEdges` (`:44`) is `private static` and has exactly one caller, `Validate` at `:41`. `Validate` is private and is called only from the constructor (`:18`). The only constructions of `SupplyTopology` are `CoreSupplyQueries.cs:10`, `PhysicsGpuAbi.Airflow.cs:43` and `SupplyRoute.cs:115`; all go through that constructor.
2. Before `:41` runs, the loop at `:26`–`:40` has visited every node and thrown `ArgumentException` at `:29`–`:30` if any has `node.Owner.Value == 0`. So every node that reaches `ValidateEdges` has a nonzero owner.
3. `SupplyCycles.Components` never emits an empty component (each is closed by popping at least its root, `SupplyCycles.cs:56`–`:60`). So at `:63` `component.Select(slot => nodes[slot].Owner)` is non-empty and every element is nonzero: the `Where(owner => owner.Value != 0)` filter removes nothing and `loop.Length > 0` at `:64` is always true.
4. Therefore `:72`–`:73` ("Break the wire loop through the nonmonotone supply capability.") cannot execute. No test, tool or document outside the slice records mentions that text (repository search: only `SupplyRoute.cs:73`).

Deleting the filter, the `if (loop.Length > 0)` wrapper and the second `throw` leaves the reachable message, its part ordering (`Distinct().OrderBy`), the four-part cap and the typed reason exactly as they are. It is behaviour-preserving, removes a second path for one refusal, and is consistent with the intent's "no reachable message text changes" (line 36). Limit: this is by reading; the full round is the executed check.

## Remaining findings (non-blocking; apply before dispatch)

### R1. Task 2 omits the deletion the owned scope list adds

- Where: contract line 137 against line 90.
- Defect: the task says "make the two condition removals and the comment correction"; the scope list now also deletes the unreachable branch in the same file. An implementer working from the task list would skip it, and the final diff scope check would then find the scope list and the diff disagreeing.
- Fix: replace line 137 with: `2. engine/gpu/SupplyRoute.cs -- make the two condition removals, delete the unreachable ownerless loop branch and its filter (:63–:73, keeping the reachable message, its Distinct/OrderBy ordering and the four-part cap), and correct the comment -- the resolved-graph rule.`
- Classification: wrong specified behaviour (task text).

### R2. Decision 5 still says message text is unchanged without qualification

- Where: contract line 124 against lines 36 and 157.
- Defect: a message string is deleted from the source; the decision reads as if none is.
- Fix: replace line 124 with: `5. **Reachable message text is unchanged**, including "parts N." for one part. The ownerless loop message is deleted because it cannot be produced (owner 0 is refused at SupplyRoute.cs:29).`
- Classification: wrong specified behaviour (record consistency).

### R3. The law row does not name the observation, and the table's lead sentence and verdict line are stale

- Where: contract lines 186, 177, 61.
- Defect: (a) a synthetic consumer with no drive target has no drive slot, so "powers the consumer" must be read from the resolution's per-node output; the row does not say so and an implementer may reach for an internal. (b) Line 177 says law-level tests build through `ISupplyQueries.CreateTopology`, which is true of S1 and S7 only. (c) Line 61 records "awaiting second audit".
- Fix: (a) append to the line 186 test cell: `Powered state is read per node from the resolution's Outputs (SupplyOutputRead.IsPowered, CuriousContraptions.Abstractions/SupplyValues.cs:61); source nodes are matched by synthetic ElectricalSourceDeclaration rows and a PhysicsElectricalRead with remaining charge.` (b) in line 177 replace "Law-level tests build nodes and edges through" with "The admission tests (S1, S7) build nodes and edges through". (c) replace line 61 with: `**Planning verdict:** Scoped Pass on second audit (planning auditor, 21 November 2026, .tools/audit/audit-engine-supply-self-edge-2.md); first audit Fail, repaired.`
- Classification: test.

## Checks repeated on the repaired text

- Intent contract (lines 17–55): every row agrees with the code as read on the first pass; S8 now agrees with the terminal rule. Nothing inside it contradicts the Code Map, tasks, acceptance or verification.
- Capability probe: rows 1–8 each cite a symbol and, where marked present, a test that exercises it. Row 3 is present-untested and the slice adds the test; row 4 is narrower, a validator case, owned here. Engine tier with six layers is correct.
- Planned tests: each matrix row has a named test with a control that can fail independently and a boundary. The law row is constructible from public types (`CompiledSupplyGraph` constructor, `PhysicsElectricalRead`'s public constructor, `ICompiledSupplyGraph.Resolve`). Its case (a) distinguishes the least from the greatest fixed point, which the catalogue cases cannot. No tolerance, hash, fixed arrival tick, skip or mutant.
- Owner decisions 1–6 are defaultable from the game's intent; none cuts scope, waives a gate or adds a dependency. Needs the owner: none.
- Concurrency: unchanged from the first pass (shares the test project file, sprint status and `TODO.md`; publishes serially).

## Evidence limits

- Nothing was built or run.
- Product code was not re-read in full; the first pass's reading stands because those paths are unmodified, and only the dead-branch claim and the law row's constructibility were re-traced.
- The dependent closure contract was not re-audited here.
