# Role cards — BMAD customization

One short card per role. A child agent reads its own card alongside the documents its prompt names. The cards restate nothing new: each line points at the rule it summarises, and the linked authority wins on any conflict. They exist because every agent was re-reading about 550 lines of the same rules, and because the same avoidable defects recurred across consecutive slices (7–21 November 2026 review).

All paths are relative to the checkout the agent is working in. Never read a policy file from another checkout. A card adds to the documents your prompt names; it does not remove any of them.

## Implementation owner

Read: the documents your prompt names, this card, the slice **contract** (the native spec file) fully including its Review Triage Log and Spec Change Log, the files in its `context:` list, and the [protocol's handoff section](bmad-loop-protocol.md#implementation-handoff-and-verification-ownership). Read the run log only for an entry the contract cites.

**Before the first edit**

1. Check the contract's Capability probe (in its Code Map) against the code. If a capability it marks *present* is missing or behaves differently, stop and return it to planning; do not build it inside this slice unless the contract already owns it.
2. Name every direct consumer you will edit (callers, wire schema, TypeScript, tables, fixtures) in the contract's Code Map before editing it.
3. Files you write go inside this checkout; Anvil refuses paths outside it. Transient review inputs go under `.tools/<slice>-evidence/`. Evidence a record cites after the worktree is gone is committed under `_bmad-output/implementation-artifacts/`. Compiler binary logs stay where the [local verification commands](../../docs/delivery-workflow.md#reproducible-local-verification) put them.

**Tests** ([testing guidance](../../docs/testing-guidance.md), [float rules](../../docs/testing-guidance.md#float-comparison-and-effort-rules))

- One named discriminating test per matrix and acceptance row, with its control and boundary case. Fill the contract's acceptance-to-test table as you go.
- Choose every tolerance and observation window before running the test, from the quantity and its gameplay consequence. Never fit a bound to observed output.
- No exact-equality assertion on a simulated float unless the value is exact by contract (an intentional zero, authored Reset state, an admission threshold); no per-tick monotonic trajectory; no whole-buffer, hash or byte-identity comparison; no fixed arrival tick unless the feature is a timer.
- Assert the changed result directly through the production interface, so the plausible defect fails the test. A test that only proves the scenario ran is a finding.
- A failing numerical assertion: state the gameplay defect before a second repair attempt, or correct the test contract.

**Code**

- No fallback, default identity, second path, alias, migration or retained old field. Refuse unsupported input with a typed reason.
- No element identity, kind or name in engine code. A new element is declarations, art, bindings and tests.
- Closed sets stay enums end to end; f32 game values.
- Keep each new or changed member within cyclomatic complexity 25 and maintainability index 20 as you write it. Run the [maintainability gate](bmad-loop-protocol.md#maintainability-gate) as soon as the candidate compiles, not only at handoff.

**Before handoff, in this order**

1. Maintainability gate passes; then repeat the matching non-incremental compiler-log builds (the gate's injected build is not verification evidence).
2. [New-package registration check](../../docs/delivery-workflow.md#new-package-registration-check) passes, whenever the slice adds or renames a package, port, parameter, wire field, test file or tool.
3. Affected tests pass, and the acceptance-to-test table names a test that ran for every row.
4. `git add -N` your new files, then `git diff --check <baseline_revision>` is clean.
5. Inspect the full owned diff for needless branches, duplicate rules and unrelated churn.
6. One full local round, zero failures, `Skipped: 0`.

Report: what changed, each command with its result and raw output location, the filled table, and remaining risks.

## Planning scout (capability probe)

Read: this card, the draft contract, the element's requirement and named-element rows, and the [protocol's capability probe](bmad-loop-protocol.md#capability-probe). Read-only; edit nothing.

Return three lists, each entry cited as `path:line` or a test name in the current checkout:

1. **Capabilities.** Every engine, abstraction, bundle, wire or presentation capability the planned behaviour needs, marked *present* (cite the symbol and a test that exercises it), *present, untested* (cite the symbol; the slice adds the test), *narrower* (say how), or *missing*.
2. **Direct consumers.** Every table, list, guard, schema, generated file, TypeScript caller and fixture that must change when this package or field is added.
3. **Open decisions.** Every question only the owner can answer, each with the default you would choose from the game's intent.

Do not infer a capability from a type name.

## Blind reviewers (Blind Hunter, Edge Case Hunter, Verification Gap, Intent Alignment)

Read: this card, your layer's instruction, the diff you were given, and the [reviewer duties](bmad-loop-protocol.md#reviewer-duties). Do not load the engineering contract or engine guides.

- This is a physics puzzle game. Judge outcomes, causal order, stability and lifecycle; do not demand exact ticks, incidental contact sequences or tighter precision. Unjustified precision in a test is itself a finding.
- Follow your layer's instruction on what you may run. No mutants, no browser gate, no finding quota. Report evidence limits and unreadable inputs.
- For each finding give the file and line, the concrete defect, and its gameplay or interface consequence, in your layer's own output format. Where you can tell, add whether the fix is a test change or a product change; the coordinator classifies and routes. Intent Alignment stays descriptive and adds no classification.
- On a re-review, your input is the repair diff. Report defects in the repair and in what it directly affects.

## Informed auditors (Forward-Only, Element-Free Engine, acceptance, planning)

Read: this card, your layer's instruction, the header naming phase, candidate identity and evidence paths, and the [reviewer duties](bmad-loop-protocol.md#reviewer-duties). The Element-Free Engine and planning auditors also read the [engineering contract](project-engineering-contract.md) and both engine guides.

- Planned tests are not executed proof; test source is not evidence it ran. Missing required-now proof is Incomplete.
- A planning audit also checks the capability probe: every capability marked *present* has a cited symbol and test, every *missing* capability has an owning slice, and no required-now owner decision is open.
- Give each finding a classification the coordinator can route on: test, product defect or wrong specified behaviour.

## Coordinator

Read: this card and the [loop protocol](bmad-loop-protocol.md) fully.

- Wait for a child or a long command with the host's blocking tool call in the same turn. Do not poll on a timer, do not end the turn to await a child, and do not post a status message while nothing has changed.
- Update `TODO.md` and the sprint status only when the slice's outcome, blocker or candidate changes.
- After a context compaction, re-read the run log's handled-messages list before acting. Never act again on an owner message that list records as handled.
- Ask the owner every open decision in one batch at planning, each with a recommended default. Do not surface them one at a time during implementation.
- Route each confirmed finding by its classification: a missing or weak test is a patch.
- Apply the protocol's Re-review scope from a slice's third pass: only a finding outside the repair diff and what it affects, breaking no rule, criterion or behaviour, is deferred.
- Keep each native log row, receipt and identity in the contract to one line that cites its run-log entry; bulk goes in the run log.
- Write the close note and deferred-work additions before the candidate commit.
