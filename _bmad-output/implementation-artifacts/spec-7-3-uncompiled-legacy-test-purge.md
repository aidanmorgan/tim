---
title: 'Story 7.3: Uncompiled legacy test purge'
type: 'chore'
created: '2026-10-10'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
source_commit: '90db56b54fd4d5320cc5ce07ec2d921828951bf2'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/planning/elements/legacy-disposition.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The test directory retains 416 uncompiled legacy C# files and a tracked merge backup after their only obsolete tool consumer was removed by Story 7.2.

**Approach:** Delete the exact 417 ledger paths while preserving every current test, fixture and compile input; execute the solution test suite and independently verify preservation.

## Boundaries & Constraints

**Always:** Match ledger §7.3 and fresh bytes before Anvil-gated deletion. Preserve all 26 local compiled sources, two linked Simulation sources, package-generated sources, project references and test data. Existing seven retained tools, ignored local archives, unrelated Battery work and review receipts stay untouched. Keep baseline citations resolvable through Git. Use the same independent reviewer; local scoped commit only after snapshot approval and committed-tree verification.

**Never:** Change test assertions, compilation membership, supported modes, runtime code or Story 7.4 sources. No cleanup of bin/obj, no compatibility path, no push.

## I/O & Edge-Case Matrix

| State | Action |
| --- | --- |
| Exact ledger paths outside evaluated Compile inputs | Delete the reviewed files |
| New consumer, unmatched path, symlink or source drift | Stop and reconcile before deletion |
| Existing test fails | Diagnose and resolve within actual impact; preserve failure evidence |

</frozen-after-approval>

## Code Map

- `legacy-disposition.md` §7.3 enumerates 417 exact files, 3,308,638 bytes: 416 C# files and `WoundSpringTests.cs.orig`.
- `CuriousContraptions.tests.csproj` disables default Compile discovery: 26 local sources plus two linked Simulation sources. Evaluated Release list has those 28 plus eight 2dog.xunit package sources. None is in the deletion set.
- `CuriousContraptions.slnx` still runs the current project; root game project excludes this directory. No remaining project/script consumer of deleted tests was found; Performance.Tests was removed in verified Story 7.2.
- Project comment naming the remaining sources “unshipped reference obligations” becomes current wording; Compile items remain byte-identical.
- TODO, sprint, epics, roadmap, inventory and ledger record the outcome. Current unit baseline: 643 passing tests.

## Tasks & Acceptance

**Execution:**
- [x] Freeze evaluated Compile inputs and exact deletion identities; independent entry review.
- [x] Delete only ledger paths; update stale comment and current status.
- [x] Run required solution tests; compare Compile inputs and current source preservation.
- [x] Freeze one scoped snapshot/diff, independent review, local commit and committed-tree check.

**Acceptance Criteria:**
- Given dead test files and obsolete native fixtures, when LEGACY-0c is applied, then all obsolete test files are deleted.
- `dotnet test CuriousContraptions.slnx` runs with 100% pass rate (zero failures).
- All remaining local C# test/fixture files belong to the active project; no current test is lost.

## Implementation Notes

Exact 417-path deletion completed after independent entry review, Anvil and fresh SHA equality. Exact membership check raw369ce1; MSBuild evaluated Compile output retained for before/after comparison. Story 7.2 is independently verified in local commit90db56b. No owner intent gap or untracked-source archive needed.

## Spec Change Log

## Review Triage Log

No implementation finding. Required solution test: 643 passed, zero failed/skipped; five pre-existing xUnit2013 warnings in unchanged WorkshopActivationAnimationTests remain. Complete raw output: `.anvil/story-7-3-unit.log` (raw5937b6/5528c7). Evaluated Compile objects remain exactly equal to `.anvil/story-7-3-compile-before.json`; all 26 remaining local C# sources match HEAD bytes (raw697f03). Original deletion manifest `.anvil/story-7-3-deletions.json` SHA-256 `8642b83bcbf3c6658007c723e2a34599632fc9ed60ab0ab02e8558e5f7c257a1`. Digest-bound inventory preview validation was partial; every file deletion separately passed Anvil. Project scan and diff check passed. Final snapshot and committed-tree approval remain.

## Verification

- `dotnet msbuild CuriousContraptions.tests/CuriousContraptions.tests.csproj -getItem:Compile -p:Configuration=Release --nologo` before/after: same 36 evaluated items.
- `dotnet test CuriousContraptions.slnx --nologo`: all current tests pass; no warning introduced (five known xUnit2013 warnings predate this slice).
- Anvil affected-source/document scan, exact deletion/kept-source comparison, active references and pinned-source checks; `git diff --check`.
- Reuse Story 7.1's exact production/Chrome proof only with independent relevant-input applicability; no runtime/driver change means no duplicate full Chrome run.
