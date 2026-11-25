---
title: 'Policy-tool and harness fixes deferred from S2 (lane 4)'
type: 'refactor'
created: '2026-11-24'
status: 'done'
baseline_revision: '5f1c4d4a170f5eceedf16b68378b2befa096651d'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad/custom/bmad-loop-protocol.md'
  - '{project-root}/tools/anvil/README.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** The S2 review deferred small gaps in the staged-source policy tool (`tools/anvil/CheckStagedTests.cs`), its no-argument verification harness (`tools/anvil/VerifyPolicy.cs`) and the loop documents to "whoever next changes a file under `tools/anvil/`" (`_bmad-output/implementation-artifacts/deferred-work.md:306–328`). The harness can inherit a caller's git state and configuration; its no-git case depends on how `dotnet` is installed; three refusal paths are proven only by fakes or not at all; some pass cases prove only an exit code; its cleanup and counts can misreport; the registration filter misses two classes main relies on; the role card omits a handoff step; and `TODO.md` is outside the data-only allow-list although it holds only the current brief, so every repair that edits it is reviewed at engine tier.

**Approach:** Close every deferred item that names this slice as owner, in the tool, its harness, its fixtures and the documents, each with an executed harness case or a recorded check; close the history-only items without editing history; and add `TODO.md` to the data-only allow-list (owner decision 24n).

## Boundaries & Constraints

**Always:** The staged-source rule's decisions are unchanged: every refusal and pass of `StagedSourceSelfCheck` (`CheckStagedTests.cs:102–132`, 14 cases) and of the existing harness cases still holds. Every refusal and pass case runs real git and the harness's isolated hook (which runs the real `CheckStagedTests.cs`; `VerifyPolicy.cs:34–36`) against a disposable repository outside the checkout; the P1 check runs real git and the P9 check real file-system removal, in temporary directories outside the checkout. A refusal names what it refuses and carries git's own text where git failed. The harness's child processes see no inherited `GIT_*` variable and no user, XDG or system git configuration, including git's default global excludes and attributes files. Policy changes apply from the next slice that starts (AGENTS.md Stable rules). Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** A change to what the hook refuses or scans (coordinator decision, 25 Nov 2026: except reading the staged blob by its explicit stage-0 form `:0:<path>`, so the scan reads the bytes of the file it names. What it scans and what it refuses are otherwise unchanged), to `.githooks/pre-commit`, `tools/anvil/no-skipped-tests.json`, the maintainability or coupling gate (`VerifyPolicy.cs:966` onward) or the customization checker (`:428–965`). A test-only switch or environment variable read by `CheckStagedTests.cs`. Editing the published S2 contract or its run log, or `docs/delivery-workflow.md:34` (S5's). A fallback (a second way to find `dotnet`, a retry after a failed cleanup). Output hashes, mutants, browser runs or skipped tests.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| P1 | The harness process carries `GIT_DIR`, `GIT_CONFIG_PARAMETERS`, `GIT_CONFIG_COUNT`/`KEY_0`/`VALUE_0`, a `GIT_CONFIG_GLOBAL` file and an `XDG_CONFIG_HOME` whose `git/config`, each setting `core.hooksPath` to `/nonexistent`; the XDG directory also holds `git/ignore` naming `Leaked.cs` and `git/attributes` containing `Leaked.cs foo=bar` | A child's `git config --show-origin --get-all core.hooksPath` in the disposable repository is exactly `file:.git/config`, a tab, `.git-hooks`; a child's `git ls-files --others --exclude-standard` lists an untracked `Leaked.cs`; and a child's `git check-attr -a Leaked.cs` prints nothing | A leaked value fails the check, naming it |
| P2 | The `dotnet` first on `PATH` is a wrapper script that runs `env` by name; or `DOTNET_ROOT` is unset; or it names an empty directory | The no-git case runs the hook with `PATH` holding only a link to the `dotnet` at the root of the runtime the harness runs on, and refuses with `git could not be started` | If git starts all the same, the case fails for want of the refusal text |
| P3 | `APP.CSPROJ` staged, `NewTests.cs` untracked | Refused, listing `NewTests.cs` | Rule's refusal |
| P4 | `Kept.cs` staged, `NewTests.cs` untracked, real `git commit` through the hook | The commit exits non-zero, lists `NewTests.cs`, and no commit is created | Rule's refusal |
| P5 | A gitlink staged at `Module.cs` | Refused by one line naming the path and carrying git's own error text for `git show :0:Module.cs` (coordinator decision, 25 Nov 2026) | Tool's refusal |
| P6 | A skip-free staged test; an empty index | Pass, and the output shows the staged path was scanned (its `Anvil snapshot` line and the scanner's JSON report); the empty index passes with the self-check line and no snapshot line | A pass with no scan fails the check |
| P7 | A skip marker staged in `.js`, `.mjs`, `.cjs`, `.jsx` and `.tsx` files | Each is blocked by `TEST-001` | Skip rule |
| P8 | The hook run from a subdirectory, `NewTests.cs` untracked at the top level | Refused, listing `NewTests.cs` relative to the top level | Rule's refusal |
| P9 | Two temporary directories, the first undeletable | Removal attempts both, removes the second, and returns false | The cleanup failure is reported, never replacing an earlier failure |
| P10 | The registration filter at the baseline | It selects `CurrentCapabilityInputsTests` and `WorkshopIntegrationStartupTests` with the 18 existing classes | A class that selects nothing fails AC5 |
| P11 | A slice whose only path outside the allow-list was `TODO.md` (owner decision 24n) | Data-only tier under the protocol's Review tiers | Any other path keeps it engine tier |
| P12 | A skip marker staged in `0:Foo.cs` beside a staged clean `Foo.cs`; the same pair without the skip (coordinator decision, 25 Nov 2026) | The first is blocked by `TEST-001`; the second passes | Skip rule |

</intent-contract>

## Code Map

Delivery shape: single structural tooling and policy slice (no element, family or closure). Review tier: **engine**, because `tools/anvil/`, `_bmad/custom/` and `docs/delivery-workflow.md` are tool, policy and workflow files outside the data-only allow-list; the capability probe is below. Baseline: captured at dispatch as the published main that contains owner decisions 24m and 24n and the coordinator ruling recorded after 24m (all uncommitted at drafting); every scope guard and `git diff` compares against it. Drafted and line-checked on `0959e883` (origin/main, 24 November 2026), which is the drafting revision only. Lane 4 of owner decision 24m.

**Status: reviewed candidate (passes 1–4 on `330d6bc9`; the pass-1 decision is the coordinator decision of 25 Nov 2026, Spec Change Log 1); merged with `51529148` as `52c2c643` (E15), whose SnapshotApproval found R6 and R7 (Spec Change Log 3); repaired (E16, E18) and re-reviewed in passes 5 and 6 (E19); the post-merge Final diff scope check is in the Review Triage Log.** Planning verdict: **Scoped Pass** (ready-for-dev) per the fifth audit (`docs/planning/elements/readiness/audits/audit-policy-tool-fixes-5.md`, 24 November 2026), whose four non-blocking findings are applied. Audits 1 to 4 (`audit-policy-tool-fixes-1.md` to `-4.md`) were each Incomplete; their findings (1–18, 1–6, 1–3, 1–6) are all applied. The owner decision is settled as owner decision 24n and lane admission by the coordinator ruling recorded after 24m (both 24 November 2026), published on the baseline.

**Dispatch (run log E1, E2).** Checkout `.claude/worktrees/slice-policy-tool-fixes`, branch `slice-policy-tool-fixes`; baseline `5f1c4d4a170f5eceedf16b68378b2befa096651d`. Owned code, policy and workflow files are unchanged from `0959e883`, so the anchors and probe hold; `deferred-work.md` lines moved (E2). Implementation owner: a fresh no-history subagent (Claude Opus 5.5, agent `a368b3287d3f6b1ff`), unavailable after the 24 November session stall; repairs go to a named replacement (E6).

### Carry-forward: every deferred entry naming this slice

F1–F19 (every `deferred-work.md` entry naming this slice on `0959e883`, each "In" with its matrix row or task, F8 and F12 "Closed, no edit", and the not-taken items) are moved verbatim to run log E17 (Contract budget). S3's residuals R6 and R7, which reached this slice through the merge, are Spec Change Log 3.

### Owned scope

- `tools/anvil/VerifyPolicy.cs` -- no-argument harness only: the disposable repository's local configuration `:43–45` (D1); fixture loop `:72–85` (Allow assertion `:81–84`); empty index `:86–88`; real-commit check `:92–98`; passed-count line `:99`; cleanup `:101–106`; `enum Source` `:108`; `Harness.Execute` `:117–139` (variables `:127`); `Harness.Remove` `:161–175`; `StagedSourceHarness` cases `:190–244`, `Verify` `:246–251`, `SkipScans` `:291–310` (gitlink case `:304–308`), `UnusableGit` `:312–337`, `PathWithoutGit` `:339–350` (comment `:339–341`), `RequireOutput` `:353–361`.
- `tools/anvil/CheckStagedTests.cs` -- `enum SourceKind` `:8` (deleted); the `SourcePaths` summary `:170–172`; `SourcePaths.Scanned` `:175–181` (a set); `SkipScan` `:227–265` (the `git show` at `:238` captures its error; the refusal at `:242–248` carries it).
- `tools/anvil/fixtures.json` -- one Block fixture for each of `.js`, `.mjs`, `.cjs`, `.jsx`, `.tsx`.
- `tools/anvil/README.md` -- tool self-check count `:52` (14, unchanged); the harness rule `:114` (linked, not restated); the inventory, counts and child-environment paragraph `:116–149`.
- `_bmad/custom/bmad-loop-protocol.md` -- handoff item 2 `:180`; the allow-list `:54–60` (owner decision 24n); after the merge, handoff item 3's R7 pointer (Spec Change Log 3).
- `_bmad/custom/role-cards.md` -- implementation owner's "Before handoff" `:33–40`; after the merge, its item 3 and the protocol's handoff item 3 gain the R7 pointer (Spec Change Log 3), and `deferred-work.md` closes R6 and R7.
- `docs/delivery-workflow.md` -- an anchor `policy-tool-harness` before the harness rule `:58` (the one statement of the rule); the registration filter `:67`; the count sentence `:70`; after the merge, `SolutionMembershipTests` in the filter and the count restated (R6, Spec Change Log 3).
- `docs/planning/elements/readiness/completion-ledger.md` -- row OS-04 (`:395`) state column set to done.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- F1–F19 marked closed with this slice; any residual added with source, evidence and owner; after the merge, S3 residuals R6 and R7 closed (Spec Change Log 3).
- `TODO.md`, this contract and `_bmad-output/implementation-artifacts/run-logs/spec-policy-tool-fixes.md`.

Read, not changed: `.githooks/pre-commit`; `tools/anvil/no-skipped-tests.json`; `tools/anvil/VerifyPolicy.cs:428–965` (`CustomizationCheck`, whose self-test reads `_bmad/custom` and must stay 37/0/0) and `:966` onward (gates); `_bmad/custom/bmad-loop-protocol.md:71,206`, `_bmad/custom/role-cards.md:90` and `_bmad/custom/bmad-build-auto.toml:21` (tier confirmation opens hunks of list-line paths only; the 24n allowance is path-based, so they need no edit); `AGENTS.md` ("Review tiers" defers to the protocol).

### Capability probe

Measured on `0959e883`. No solver, ABI, wire or presentation capability is involved. Harness total at S2's publication: 67 checks (15 fixtures + 6 + 46; `tools/anvil/README.md:116`; S2 TODO at `25df3cd5`); tool self-check 14; policy checker self-test 37 (same source).

- **Present:** staged-source decision with fake git. `StagedSourceRule.Decide` (`CheckStagedTests.cs:47–65`); exercised by `StagedSourceSelfCheck` (14 cases, run on every invocation, `:205–207`).
- **Present:** case-insensitive extension sets. `Scanned` and `SourceCommitInputs` use `OrdinalIgnoreCase` (`:181,187`); the scanned arm is exercised by the real case "a new C# file with an upper-case extension is refused" (`VerifyPolicy.cs:200–201`) and "a staged skip in a .CS file" (`:296–298`). **Present, untested:** the trigger arm in upper case (P3 adds it).
- **Present, untested:** refusal through a real commit. The real-commit check `:92–98` covers only the skip rule; P4 adds the staged-source one.
- **Narrower, owned here:** gitlink refusal text. `SkipScan` starts `git show` without capturing its error (`:238`), so git's line passes through the hook's own stderr and the refusal at `:245–246` does not carry it; exercised by the harness case `:304–308`. P5 changes it.
- **Narrower, owned here:** child isolation. `Harness.Execute` removes three variables (`:127`); P1 widens it and adds its check.
- **Narrower, owned here:** the no-git case. `PathWithoutGit` links the first `dotnet` on `PATH` (`:344–349`); a wrapper script that runs a command by name cannot run with `PATH` holding only that link; exercised by `UnusableGit` (`:325–327`). P2 links the runtime's own host instead.

Policy text, not a tested capability: the data-only allow-list (`bmad-loop-protocol.md:54–60`), applied in review by the coordinator and the Element-Free Engine Auditor (`role-cards.md:73`); no tool reads it. `TODO.md` is absent from it; two contracts cite it as their only engine-tier reason (`spec-elements-visible-solid-remainder-b2.md:45`, `spec-repair-baseball-el-239.md:96`) and one as one of two (`spec-early-level-proof-batch-1.md:61`).

### Decisions

- **D1. All `GIT_*` variables are removed from every harness child,** then `GIT_CONFIG_NOSYSTEM=1`, `GIT_CONFIG_GLOBAL=/dev/null` and `LC_ALL=C` are set and the case's own `environment` applied; the disposable repository sets `core.excludesFile` and `core.attributesFile` to `/dev/null`; `HOME` is kept. Reason and probes: run log E19 (full text), E3.
- **D2. The no-git case links `<root>/dotnet` (three levels above `RuntimeEnvironment.GetRuntimeDirectory()`), runs `/bin/sh`, and sets `PATH` to that one directory;** no environment variable is read. Reason, probes and stop condition: run log E19 (full text), E3.
- **D3. The gitlink case derives its expected text from real git:** git's first error line from `git show :0:Module.cs --` (Spec Change Log 2) must appear inside the refusal line, exactly once in the hook's output. Full text: run log E19.
- **D4. One cleanup shape:** `Harness.RemoveAll(params DirectoryInfo[])` attempts every directory and returns whether all were removed; the main run and `UnusableGit` both use it. The passed-count line is printed only after `RemoveAll` returns true.
- **D5. The harness rule is stated once,** in `docs/delivery-workflow.md:58` under a new anchor `policy-tool-harness`; protocol handoff item 2, the role card's new item and `tools/anvil/README.md:114` link to `#policy-tool-harness`.
- **D6. `SourceKind` is deleted, not used.** The tool decides only "scanned or not"; the scanned set becomes `FrozenSet<string>`, and the `SourcePaths` summary (`:170–172`) drops "internal choices remain enum-typed". No closed set is lost: the enum's values were never read (`:189` reads keys only).
- **D7. Lane admission (coordinator ruling recorded after owner decision 24m, 24 November 2026).** Shared policy and workflow documents (`_bmad/custom/*.md`, `docs/delivery-workflow.md`) count as shared documents under the 24m merge rule, not lane code, so this lane runs beside the others; `tools/anvil/` code belongs to this lane alone, and each later publisher's full round on its merged commit runs the changed gates. This slice edits no C# file that another slice edits. Its shared documents are listed under "Running beside other work", together with `completion-ledger.md`, which it shares by row with lane 3 (OS-04 against CA-01 and OS-02) and part D (after the engine programme; not a running lane; OS-04 against ES-37 and OS-05).

### Owner decision

- **Owner decision 24n (24 November 2026; `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`).** `TODO.md` joins the data-only allow-list: it holds status text only, so editing it no longer raises a slice's review tier, and this slice makes the change. Wording: the last bullet of `bmad-loop-protocol.md:60` gains "and `TODO.md`, the current brief (outcome, blocker, next check and evidence links; owner decision 24n)", a path-based allowance needing no hunk reading. The owner decided it because Review tiers is an owner decision (`:50`) and the change removes Blind Hunter, Edge Case Hunter and Intent Alignment from slices that were engine tier only because of `TODO.md`. No owner decision is open.

### What this removes

The three-name `GIT_*` list, the `PATH` lookup of `dotnet`, the second cleanup shape, the early count line, `enum SourceKind` and the repeated harness-rule sentences; the full list is in run log E20.

## Tasks & Acceptance

**Execution:**

1. `VerifyPolicy.cs` `Harness.Execute` and the local configuration at `:43–45` -- D1; a check "an inherited git variable or configuration does not reach a child" -- P1.
2. `VerifyPolicy.cs` `PathWithoutGit`, `UnusableGit`, `RequireOutput` -- D2; every hook invocation in the harness uses `/bin/sh` -- P2.
3. `VerifyPolicy.cs` `StagedSourceHarness.Cases` -- add "an untracked C# file beside a staged `APP.CSPROJ` is refused" -- P3; after the real-commit skip check, add the real-commit staged-source refusal: stage `Kept.cs`, leave `NewTests.cs` untracked, `git commit`; require non-zero exit, `NewTests.cs` listed under "Not staged:", and `git rev-parse -q --verify HEAD` failing -- P4; add the subdirectory case: the hook run with working directory `nested/` (`/bin/sh ../.git-hooks/pre-commit`) -- P8.
4. `CheckStagedTests.cs` `SkipScan` -- capture `git show`'s standard error (read asynchronously, as `Git` does at `:274`) and append its trimmed text to the refusal line; `SourcePaths.Scanned` a `FrozenSet<string>`; delete `enum SourceKind`; update the summary at `:170–172` -- P5, D6. `VerifyPolicy.cs:304–308` -- D3. `SkipScan` reads each staged path as `git show :0:<path>`; the harness adds the P12 case "a staged skip in a file named `0:Foo.cs` beside a clean `Foo.cs` is rejected" with its control -- P12 (coordinator decision, 25 Nov 2026).
5. `VerifyPolicy.cs` fixture loop and empty index -- an Allow fixture also requires the line `Anvil snapshot 0.ts: <path>` and the scanner's JSON report (`"summary"`) in the output; the empty index requires the self-check line and no `Anvil snapshot` line -- P6. `enum Source` gains `JavaScript`, `JavaScriptModule`, `JavaScriptCommon`, `JavaScriptReact`, `TypeScriptReact` with paths `sample.spec.js`, `.mjs`, `.cjs`, `.jsx`, `.tsx`; `fixtures.json` gains one Block fixture each (`test.skip("disabled", async () => {});`) -- P7.
6. `VerifyPolicy.cs` `Harness.Remove`, main cleanup, `UnusableGit` -- D4; a check "a directory the harness cannot delete is reported and fails the run": two fresh temporary directories outside the disposable repository, the first holding a mode-0500 subdirectory with a file; `RemoveAll(first, second)` returns false, the first still exists and the second is gone; the mode is restored in a `finally`, and `RemoveAll(first)` then returns true -- P9, F11.
7. The passed-count total is the number of checks that printed a `PASS` line (each check with its control counts once), printed after cleanup.
8. `docs/delivery-workflow.md:67` -- append `|FullyQualifiedName~CurrentCapabilityInputsTests|FullyQualifiedName~WorkshopIntegrationStartupTests`; `:70` -- replace the `b5b9b233`/713 sentence with the count this filter selects at the baseline and that SHA, or, when this slice publishes second, at the merged `origin/main` parent and that SHA (equal to the candidate's count, because this slice changes no test project) -- P10.
9. `docs/delivery-workflow.md:58` -- the anchor (D5); `_bmad/custom/role-cards.md:33–40` -- insert, as item 2, ``Policy-tool harness passes whenever a file under `tools/anvil/` changes ([rule](../../docs/delivery-workflow.md#policy-tool-harness))`` and renumber; `bmad-loop-protocol.md:180` and `tools/anvil/README.md:114` -- link to `#policy-tool-harness` instead of restating it -- F7, F16. `bmad-loop-protocol.md:60` -- the 24n wording above -- P11.
10. `tools/anvil/README.md:116–149` -- the inventory restated from this slice's harness run (cases, counts, D1's environment rule, D2's no-git case); `completion-ledger.md:395`; `deferred-work.md`; `TODO.md`.

**Acceptance Criteria:**

- AC1: Given the candidate, when `dotnet run --file tools/anvil/VerifyPolicy.cs` runs with no arguments, then it exits 0 and prints `Policy fixtures: Passed: N, Failed: 0, Skipped: 0.` with N = the baseline harness total + 11 (P1, P3, P4, P8, P9, P12 one each; P7 five), re-measured at the baseline (67 at S2), and N equals the number of lines in that output beginning `PASS `.
- AC2: Given the candidate, when the same command runs (a) with the pinned toolchain first on `PATH`, (b) with a directory holding both `dotnet` and `git` first, and (c) with a scratch wrapper named `dotnet` (`#!/bin/sh` and `exec env "$DOTNET_ROOT/dotnet" "$@"`) first on `PATH` under the Verification environment, (d) with `DOTNET_ROOT` unset (`env -u DOTNET_ROOT`), and (e) with `DOTNET_ROOT` naming an empty scratch directory, then each passes with the AC1 total. Control for (c), recorded once in the run log: at the drafting revision's `PathWithoutGit`, the same wrapper makes the no-git case fail (exit 127, no refusal text).
- AC3: Given the candidate, when `dotnet run --file tools/anvil/CheckStagedTests.cs` runs in a clean checkout, then it reports its 14 self-check cases held.
- AC4: Given the candidate, when `dotnet run --file tools/anvil/VerifyPolicy.cs -- --bmad-check --self-test` runs, then `Fixtures: Passed: 37, Failed: 0, Skipped: 0.`; and `--bmad-check --project-root "$PWD"` reports `structural findings=0` for every route.
- AC5: Given the candidate, when the registration check in `docs/delivery-workflow.md` runs, then its count equals the count the `:70` sentence states (re-measured and restated on the merged candidate when this slice publishes second), with 0 failed and 0 skipped; and each of `CurrentCapabilityInputsTests` and `WorkshopIntegrationStartupTests`, filtered alone, selects more than zero tests, the total being the 18-class count plus those two counts and, from Spec Change Log 3, the `SolutionMembershipTests` count.
- AC6: Given the candidate, when the full local round runs, then its total equals the baseline's, or on a merged candidate the merged main's total (this slice changes no test project), 0 failed, `Skipped: 0`.
- AC7: Given the candidate, when `git grep -n -e 'Whenever a file under .tools/anvil/. changes, run' -e 'The hook and CI build that harness but do not run it' -- AGENTS.md _bmad/custom tools/anvil docs/delivery-workflow.md docs/testing-guidance.md` runs, then it lists only `docs/delivery-workflow.md`.
- AC8: Given the candidate, when `git grep -n 'allow-list' -- AGENTS.md _bmad/custom docs/delivery-workflow.md` runs, then the list is defined only at `bmad-loop-protocol.md:54–60` and every other hit refers to it; the new bullet matches the 24n wording (read in review).

## Spec Change Log

1. 2026-11-25, review pass 1, coordinator decision, 25 Nov 2026 (option (a) of the pass-1 Auto Run Result). Trigger: the `intent_gap` row (a staged path named `0:Foo.cs` is read as stage 0 of `Foo.cs`). Amended: the Never clause gains the stage-0 read exception; P5's expected text and D3 use `git show :0:Module.cs`; matrix row P12 and its harness case; task 4; AC1 becomes baseline + 11 (78). Known-bad state avoided: the no-skip gate scanning another file's bytes. KEEP: every other decision of the staged-source and skip rules, D1–D7, the Final diff scope list and the pass-1 candidate.
2. 2026-11-25, review pass 2 (Edge Case Hunter, the `:0:<path>` work-tree row). The explicit stage-0 read of entry 1 ends its argument list with `--` (`git show :0:<path> --`), so git reads the argument only as the stage-0 revision even when a work-tree file has that literal name; this is the same read under the same coordinator decision, not a new rule. D3's reference read mirrors it (`git show :0:Module.cs --`; review pass 3). KEEP: entry 1 and the AC1 count (78); the proof sits inside the P12 control, with no new PASS line.
3. 2026-11-25, SnapshotApproval of merge `52c2c643` (Fail). The merge brought S3's residuals R6 and R7 (`deferred-work.md`, published in `3a80c6a8` after this slice's baseline), owned by "the next slice that edits the registration check" and "the next policy change to `_bmad/custom`", which this slice is. Added to owned scope: R6, `SolutionMembershipTests` joins the registration filter and the count is re-measured and restated; R7, a one-line pointer in the Implementation owner card's handoff item 3 and the protocol's handoff item 3 sends a slice that adds a slider declaration or makes a package a capture-target payload to the registration paragraph's slider and payload obligations, without routing it to the registration checks; both entries closed. KEEP: everything reviewed at `330d6bc9` and the merge resolution of `52c2c643`.

## Review Triage Log

### 2026-11-25 — Review pass

- Pass 1, engine tier, all six layers, each a fresh no-history subagent on the uncommitted candidate on `5f1c4d4a`; inputs, agents and probes in run log E7.
- verdicts: 17 findings — high 1, medium 1, low 9, false 6, maybe-false 0
- findings:
  - `[low]` `[patch]` BH: `Harness.VerifyCleanupFailure` leaks both temporary directories when it throws (its FAIL, the first `chmod`, or the control) — confirmed by reading `VerifyPolicy.cs:351–379`; siblings `ChildIsolation.Verify` and `UnusableGit` already clean up on failure. Fix: `catch { restore the mode; RemoveAll(undeletable, deletable); throw; }`, the sibling shape.
  - `[high]` `[intent_gap]` ECH: a staged scanned path named `<0–3>:<name>` makes `git show ":" + path` read stage N of `<name>`, so a skip in staged `0:Foo.cs` beside a tracked clean `Foo.cs` passes TEST-001, and a lone `0:Foo.cs` is refused as unreadable (pre-existing, `CheckStagedTests.cs:232`; both halves probed on git 2.56.1, E7). The direct fix (`":0:" + path`) changes what the hook scans and refuses and changes git's P5 error text to `:0:Module.cs`, both fixed by the intent contract (Never; P5). Owner decision raised (Auto Run Result).
  - `[low]` `[patch]` ECH: the gitlink refusal appends git's whole trimmed stderr, so a multi-line git error breaks P5's "one line" (`CheckStagedTests.cs:241–242`). Fix: collapse line breaks in the captured error to one line; checked by reading (git gives no multi-line error on demand).
  - `[low]` `[patch]` ECH: on a successful `git show`, its captured stderr (warnings) is now dropped where the baseline passed it through (`CheckStagedTests.cs:232–246`). Fix: write the captured stderr to the tool's stderr on success; the failure path keeps it only inside the refusal, so "exactly once" holds.
  - `[low]` `[patch]` ECH: as root or on a file system that ignores mode 0500, the undeletable directory is deleted, the restoring `chmod` then fails on a missing directory and hides the FAIL label (`VerifyPolicy.cs:358–377`). Fix: the mode restore does not throw when the directory is already gone, so the check's own FAIL surfaces; no user-name branch.
  - `[low]` `[patch]` ECH: the same leak as the first row (grouped with it).
  - `[medium]` `[patch]` VG: `GIT_CONFIG_NOSYSTEM=1` (`VerifyPolicy.cs:308`) has no check; deleting it fails nothing. Fix: inside `ChildIsolation`'s existing check (no new PASS line), `GIT_CONFIG_SYSTEM=<seed>` through `environment` still shows only `file:.git/config`, and its control with `GIT_CONFIG_NOSYSTEM=0` added shows `/nonexistent`; probe the control live first.
  - `[false]` `[reject]` IA: P1 seeds only around the isolation check and blocks XDG ignore and attributes by local configuration — D1 and the P1 table row specify exactly this design.
  - `[false]` `[reject]` IA: P2's caller conditions are proven by recorded runs, not a harness case — AC2 specifies the five recorded runs (E4).
  - `[low]` `[patch]` IA: P5's refusal can span lines and only git's first line is checked — grouped with the multi-line row above.
  - `[false]` `[reject]` IA: P9's "never replacing an earlier failure" is shown by structure — the P9 table row and task 7 specify the code-order check in review.
  - `[false]` `[reject]` IA: P10's selection proof is outside the diff — AC5 runs it; receipts `registration-*.txt` (E4).
  - `[false]` `[reject]` IA: the `SourceKind` change has no matrix row — task 4, D6 and F15 specify it.
  - `[false]` `[reject]` IA: the new role-card item and the 24n bullet carry no "applies from" qualifier — the AGENTS.md Stable rules and the Boundaries clause govern every policy change.
  - `[low]` `[patch]` EFE F-1: the cleanup leak (grouped with the first row).
  - `[low]` `[patch]` EFE F-2: records — the Code Map cited E6 before it existed, its Status line lagged the frontmatter, and `TODO.md` and the AC6 row cite the uncounted E5 round. Fix: E6 written (run log); Status line corrected; `TODO.md` and AC6 restated only from the counted final round.
  - `[low]` `[patch]` EFE observation: success-path stderr dropped (grouped with the ECH success-path row).
- Forward-Only: no findings. Element-Free Engine: tier confirmed engine; D1–D7 not applicable; Scoped Pass on the inspected scope pending the final gate and round.
- Cascade: the intent_gap halts this pass before patch dispatch; no code is reverted (protocol, Planning and repair transitions); the patch rows above go to the replacement owner in one repair once the owner decides.

### 2026-11-25 — Review pass

- Pass 2, engine tier, re-review of the pass-1 repair (E9): Verification Gap, Forward-Only, Element-Free Engine, Blind Hunter and Edge Case Hunter (the repair affects their subjects); no Intent Alignment. Inputs and agents in run log E10.
- verdicts: 16 findings — high 0, medium 3, low 13, false 0, maybe-false 0
- findings:
  - `[low]` `[patch]` VG: the `"; "` join of git's error lines (`CheckStagedTests.cs:243–246`) is never exercised; `GitlinkRefusal` checks only git's first line. Fix: inside `GitlinkRefusal` (no new PASS line) a second hook run with `GIT_TRACE=1` through `environment` makes the failing `git show` write a trace line and the `fatal:` line; require one refusal line holding both, joined, and no other line holding either.
  - `[low]` `[patch]` VG: the success-path pass-through of git's stderr (`CheckStagedTests.cs:249–250`) has no check; the layer proposed defer, but pass 2 confirms and repairs every finding (Re-review scope). Fix: inside the P12 control (no new PASS line), run the hook with `GIT_TRACE=1`; require the `git show :0:` trace line in the output.
  - `[low]` `[patch]` VG other: the failure path of `VerifyCleanupFailure` prints false "Cleanup failed" lines for directories already removed (`VerifyPolicy.cs:398–403`). Grouped as cleanup group A.
  - `[low]` `[patch]` BH: the same false cleanup reports (group A).
  - `[low]` `[patch]` BH: the join and the success pass-through are unguarded (grouped with the two VG test rows).
  - `[medium]` `[patch]` ECH: a work-tree file literally named `:0:<path>` makes `git show :0:<path>` ambiguous (git 2.56.1, exit 128), refusing a valid commit (`CheckStagedTests.cs:233`). Fix: end the argument list with `--` so git reads the argument only as the stage-0 revision (the same explicit stage-0 read of Spec Change Log 1; recorded as Spec Change Log 2); prove it inside the P12 control with a committed `:0:Foo.cs` beside the staged `Foo.cs` (no new PASS line).
  - `[low]` `[patch]` ECH: false cleanup reports (group A).
  - `[low]` `[patch]` ECH: a throwing mode restore inside the catch replaces the check's own exception and skips removal (`VerifyPolicy.cs:400`) (group A).
  - `[low]` `[patch]` ECH: the second temporary directory is created outside the `try`, so a failure there leaks the first (`VerifyPolicy.cs:369–370`) (group A).
  - `[low]` `[patch]` ECH claim: task 6 says the mode is restored in a `finally`; the repair restores it by explicit calls (group A; the fix uses a `finally`).
  - `[medium]` `[patch]` FO borderline, ruled a finding by the coordinator: when the control `RemoveAll(undeletable)` fails, the catch removes `undeletable` again, a retry after a failed cleanup that the contract's Never clause forbids (`VerifyPolicy.cs:395–402`) (group A). Group A fix: one `try`/`finally` in which the `finally` restores the mode (never throwing, never replacing the check's exception) and every directory is removed at most once, only if it still exists, with no removal after a failed one.
  - `[low]` `[patch]` EFE: the AC6 row still cited the uncounted E5 round — set to pending the counted final round (coordinator, this pass).
  - `[medium]` `[patch]` EFE: E9's record omitted that two run-log writes during the E8 reorder were not individually validated by Anvil, and that E8 was moved in an append-only log — recorded in E10 with an Anvil validation of the run log's current content (coordinator, this pass).
  - `[low]` `[patch]` EFE: E9's results have no raw output — the Verification line names the raw-output location from 25 November (`~/.local/share/tim-toolchain/logs/slice-policy-tool-fixes/`, outside the checkout); the pass-2 repair writes its raw results there, and the Element-Free Engine auditor's own reruns are cited in E10 (coordinator, this pass).
  - `[low]` `[patch]` EFE: the Code Map Status line, the Auto Run Result's opening status and `TODO.md` lagged the candidate — corrected (coordinator, this pass).
  - `[low]` `[patch]` EFE observation: false cleanup reports (group A).
- Forward-Only: no rule violation; the borderline existence check in `RestoreMode` is allowed (it never yields a PASS). Element-Free Engine: tier confirmed engine; the stage-0 read, P12, P5/D3, the system-configuration check and the README arithmetic verified; its own reruns gave 78/0/0 under AC2 (a)–(e), AC3 14, AC4 37/0/0.
- Route: the product and test rows go to the replacement owner in one repair (E10); the record rows are applied by the coordinator in this pass.

### 2026-11-25 — Review pass

- Pass 3, engine tier, re-review of the pass-2 repair (E11): Verification Gap, Forward-Only, Element-Free Engine, Blind Hunter and Edge Case Hunter; Re-review scope applies from this pass. Inputs and agents in run log E13.
- verdicts: 3 findings — high 0, medium 0, low 3, false 0, maybe-false 0
- findings:
  - `[low]` `[patch]` BH: `GitlinkRefusal` derives git's expected line from `git show :0:Module.cs` while the tool reads `git show :0:<path> --` (`VerifyPolicy.cs:629–631`; README `:161–162`); in the repair's direct impact. Fix: the reference read and the README name `git show :0:Module.cs --`; D3 updated (Spec Change Log 2). Sent to the replacement owner (E12).
  - `[low]` `[patch]` EFE: the Code Map Status line and `TODO.md` still said the pass-2 repair was with the owner — corrected (coordinator, this pass).
  - `[low]` `[patch]` EFE: the AC4 row cites E4 for the structural half; re-run on the final candidate and cited (coordinator, E13).
- Verification Gap and Edge Case Hunter: no findings. Forward-Only: no violation; its borderline item 2 (the control's second `RemoveAll(undeletable)`) is the check task 6 and P9 specify, run after the mode is restored, and is not the retry the pass-2 ruling forbids (that ruling concerns the cleanup path). Element-Free Engine: tier engine confirmed; Scoped Pass on the inspected scope; every pass-2 row verified in code; its reruns gave 78/0/0, AC3 14, AC4 structural 0 on seven routes, AC7 one hit.

### 2026-11-25 — Review pass

- Pass 4, engine tier, re-review of the pass-3 repair (E12): Verification Gap, Forward-Only, Element-Free Engine and Blind Hunter (run log E13).
- verdicts: 3 findings — high 0, medium 0, low 3, false 0, maybe-false 0
- findings:
  - `[low]` `[patch]` EFE: acceptance-to-test rows P5, P9, P12 and AC1/AC2 cited E11 after E12 changed `VerifyPolicy.cs`, and P5's control named the read without `--` — rows cite E12; P5 names `git show :0:Module.cs --` (coordinator).
  - `[low]` `[patch]` EFE: the pass-3 entry cited run log E13 before it existed — E13 written (coordinator).
  - `[low]` `[patch]` EFE: the Status line and `TODO.md` lagged again — restated from the final candidate (coordinator); recurrence noted in the close note (E14).
- Blind Hunter, Verification Gap and Forward-Only: no findings. Element-Free Engine: tier engine confirmed; the pass-3 repair correct; Scoped Pass on the inspected scope.

### Final diff scope

- Informed reviewer: the pass-4 Element-Free Engine auditor (`a2645eeadb12f4e25`), re-engaged after re-review, read-only, on `git diff 5f1c4d4a` (12 paths, all on the Final diff scope list; nothing outside it). Every hunk traced to a task, decision, closed deferred item or confirmed repair; nothing removed; simplification analysis: none justified (each option cosmetic or weakening a check). One record item (the AC3 row citing E11) applied. Verdict: **Pass**, conditional on later edits being records only, which they are (E14).
- Acceptance verdict (engineering gate): Scoped Pass for the policy-tool and harness scope; D1–D7 not engaged (no engine, package, wire or presentation code); E2, E5, E7, E8 and E9 applied; maintainability gate PASS on the final candidate (E13); full round 7309/0/0 (E14). Browser, release and remote qualification are not claimed.
- Invalidated by the post-merge R6/R7 repair (policy text changed): a fresh Final diff scope check and the maintainability gate run on the new final candidate (below).

### 2026-11-25 — Review pass

- Pass 5, engine tier, re-review of the post-merge R6/R7 repair (E16; Spec Change Log 3): Verification Gap, Forward-Only, Element-Free Engine (policy text only; run log E18).
- verdicts: 4 findings — high 1, medium 0, low 3, false 0, maybe-false 0
- findings:
  - `[high]` `[patch]` FO-1: the R7 sentence at `role-cards.md:37` and `bmad-loop-protocol.md:181` restated the registration paragraph's slider and payload obligations (a second statement already narrower in trigger and merging the remedies) — replaced by a pointer only (owner, E18).
  - `[low]` `[patch]` EFE F2: the same sentence's parenthetical ran the slider and payload remedies into one "or" list — removed with FO-1.
  - `[low]` `[patch]` EFE F1: `TODO.md`'s lane 4 line named the wrong commit as "this merge commit", omitted the failed SnapshotApproval and listed done checks as pending — restated for the repair commit (coordinator).
  - `[low]` `[patch]` EFE F3: the R6 and R7 closures cited line numbers without a revision under a `0959e883` header — line numbers replaced by the anchor and section headings (owner, E18).
- Verification Gap: no findings. The contract's Auto Run Result, owned scope and Final diff scope were refreshed for the repair (coordinator).

### 2026-11-25 — Review pass

- Pass 6, re-review of the pass-5 repair (E18): Verification Gap, Forward-Only, Element-Free Engine (run log E19).
- verdicts: 3 findings — high 0, medium 0, low 3, false 0, maybe-false 0
- findings:
  - `[low]` `[patch]` EFE: the pass-5 F3 row said "revision named" where the closures now cite the anchor and headings — corrected (coordinator).
  - `[low]` `[patch]` EFE: the Status line presented the invalidated Final diff scope Pass as current and omitted E18 and pass 6 — restated (coordinator); recurring class, close note E14.
  - `[low]` `[patch]` EFE: the owned-scope bullets for the protocol and `deferred-work.md` omitted their post-merge edits — added (coordinator).
- Verification Gap and Forward-Only: no findings (Forward-Only: the pointer's trigger and carve-out route only; no restated rule).

### Final diff scope (post-merge)

- Informed reviewer: the pass-6 Element-Free Engine auditor (`adf7cff433b7e8801`), read-only, on `git diff 51529148` (12 paths, all on the Final diff scope list, nothing untracked) and `git diff 52c2c643` (7 paths). `tools/anvil` is byte-identical to the reviewed `330d6bc9`, so the first check's analysis applies; every policy, workflow and record hunk traces to P10, P11, AC5, AC7, AC8, task 9, F7 or Spec Change Log 3; the lane 3 receipt in `TODO.md` is the merge's own resolution. Nothing removed; simplification analysis: none justified. Maintainability gate PASS on this candidate (`maintainability-postmerge.log`; violations 0, both canaries, coupling summary; C# unchanged since). Verdict: **Pass**.

## Design Notes

1. **Behaviour and scope.** The policy tool refuses the same commits; its refusals carry git's text; the harness proves each refusal through real git, isolated from the caller; documents state each rule once. Source: `deferred-work.md:306–328`; completion-ledger OS-04.
2. **Reusable primitive.** None new; `Harness.RemoveAll` replaces two cleanup shapes.
3. **Declared composition.** `fixtures.json` stays the harness's data; the source-commit set stays held independently at `VerifyPolicy.cs:265–269`.
4. **Execution and ownership.** The harness writes only its own temporary directories; the tool writes only its temporary snapshot directory.
5. **Generality.** Each new case differs from an existing passing one in one cause (extension case, commit path, working directory, file kind).
6. **Forward-only.** Deletes `SourceKind`, the three-variable list, the PATH lookup and `/bin` condition, the second cleanup shape and the repeated sentences; no alias or fallback.
7. **Prerequisites.** Publication of owner decisions 24m and 24n and the coordinator ruling recorded after 24m. Engineering rules E2 (strict typing in tools), E5 (forward-only replacement: the deleted members have no remaining reference, proven by the compiler-proof capture of both file-based apps), E7, E8 and E9 apply (both files are file-based apps the maintainability gate scores); E1, E3, E4, E6 and the element-free design rules D1–D7 are untouched, because no engine, package, wire or presentation code changes.

## Verification

Executor for every command: the implementation owner, except the final capture builds, maintainability gate and full round, which the coordinator runs on the final candidate (E6). Raw output under `.tools/policy-tool-evidence/` until 24 November and, from 25 November, under `~/.local/share/tim-toolchain/logs/slice-policy-tool-fixes/` (outside the checkout); one line per result here, citing its run-log entry. A change to a file under `tools/anvil/` or `_bmad/custom/` invalidates the harness and checker results.

Environment: the pinned toolchain first on `PATH` (`PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403`); record the session environment check in the run log.

### Acceptance-to-test table

| Rows | Named discriminating check | Control and boundary | Run |
|---|---|---|---|
| P1 | Harness check "an inherited git variable or configuration does not reach a child": with P1's seeds set in the harness's own environment, the child's `git config --show-origin --get-all core.hooksPath` is exactly `file:.git/config`, a tab, `.git-hooks`, its `git ls-files --others --exclude-standard` lists an untracked `Leaked.cs`, and its `git check-attr -a Leaked.cs` prints nothing; the harness restores its environment in a `finally` | Control: only `GIT_CONFIG_GLOBAL=<the sentinel file>` passed through the case's `environment` argument shows `/nonexistent`, and the same `ls-files` with `GIT_CONFIG_COUNT=1`, `GIT_CONFIG_KEY_0=core.excludesFile`, `GIT_CONFIG_VALUE_0=<the seeded ignore file>` passed through `environment` omits `Leaked.cs`, and `git check-attr -a Leaked.cs` with `GIT_CONFIG_COUNT=1`, `GIT_CONFIG_KEY_0=core.attributesFile`, `GIT_CONFIG_VALUE_0=<the seeded attributes file>` passed through `environment` prints `Leaked.cs: foo: bar`, so the check can fail. A hand-kept three-name list fails the check, because the `GIT_CONFIG_COUNT` and `XDG_CONFIG_HOME` seeds would reach the child. Within the same check, `GIT_CONFIG_SYSTEM=<the sentinel file>` passed through `environment` still shows exactly `file:.git/config`, a tab, `.git-hooks`; control: the same plus `GIT_CONFIG_NOSYSTEM=0` shows `/nonexistent` | ran, passed (E4; seeds probed live in E3); system-file seed and its control ran, passed (E9; control probed live in E9) |
| P2 | Existing "a git that cannot be started is a refusal, naming the query", under AC2 (c), (d), (e) | AC2 (a), (b); the recorded drafting-revision control | ran, passed in all five (E4); control E3 |
| P3 | "an untracked C# file beside a staged `APP.CSPROJ` is refused" | Control: "an untracked C# file beside a staged build input is refused" (`App.csproj`) | ran, passed (E4) |
| P4 | "a real git commit with an unstaged new C# file is refused" | Control: the existing "real git commit rejected" (skip rule); boundary: `HEAD` stays unborn | ran, passed (E4) |
| P5 | "a staged gitlink at a scanned path is a refusal naming the path and git's error" (D3) | `never: "Unhandled exception"` kept; git's line, from `git show :0:Module.cs --`, occurs exactly once, inside the refusal line; boundary, in the same check: with `GIT_TRACE=1` the refusal is still exactly one line, carrying git's trace line and its error joined by `; `, and no other line holds either | ran, passed (E12; git's line probed in E7, E9 and E11) |
| P6 | Fixture loop Allow assertion; "empty index" | The Block fixtures still require `TEST-001` | ran, passed (E4) |
| P7 | Five new Block fixtures, one per kind | Control: the existing "ordinary Playwright" Allow (`.ts`) | ran, passed (E4) |
| P8 | "a new C# file is refused when the hook runs from a subdirectory" | Control: the same files with the hook at the top level (first existing case) | ran, passed (E4) |
| P9 | "a directory the harness cannot delete is reported and fails the run" (task 6) | Control: the first directory after restoring its mode is removed. F10 (count printed only after `RemoveAll` returns true) is checked by code order in review. One `try`/`finally` holds all setup; the mode is restored in a `finally` that never throws, and the cleanup removes only directories no removal was yet attempted on, so no failed removal is retried and no directory already gone is reported (checked by reading) | ran, passed (E12) |
| P10, AC5 | Registration check command, the `:70` sentence and the two single-class runs | Count compared with the run's output | ran: 798 = 767 + 16 + 15, 0 failed, 0 skipped (E4); 798/0/0 again on the final candidate (E14); merged with `51529148`: 806 = 775 + 16 + 15, 0 failed, 0 skipped (E15); with `SolutionMembershipTests` (Spec Change Log 3): 809 = 806 + 3, 0 failed, 0 skipped, and the count sentence restated (E16) |
| P12 | "a staged skip in a file named `0:Foo.cs` beside a clean `Foo.cs` is rejected" (coordinator decision, 25 Nov 2026) | Control: the same pair without the skip, beside a committed file literally named `:0:Foo.cs` (Spec Change Log 2) and run with `GIT_TRACE=1`, passes with `Anvil snapshot 0.ts: 0:Foo.cs`, `Anvil snapshot 1.ts: Foo.cs`, the scanner's report and git's `trace: built-in: git show :0:Foo.cs --` line; no PASS line of its own | ran, passed (E12) |
| P11, AC8 | The AC8 consistency check | Three contracts' tier reasons cited in the Capability probe | ran (E4) |
| AC3 | Tool self-check | 14 cases | ran on the final candidate, 14 held (E14) |
| AC4 | Policy checker self-test and structural check | 37 fixtures; every route | ran on the final candidate: 37/0/0; all seven routes, findings 0 (E14) |
| AC6 | Full local round | Baseline total | ran on the final candidate, bracketed by HEAD and HEAD-AFTER: 7309 passed, 0 failed, `Skipped: 0`, equal to the baseline (E14; E5 not counted, E6) |
| AC7 | The AC7 `git grep` | `docs/delivery-workflow.md` is the one hit | ran (E4) |
| AC1, AC2 | Harness run, total and `PASS ` lines | AC2 (a)–(e) | ran: 78 = 67 + 11 and 78 `PASS ` lines in each of (a)–(e) (E12) |

No float is compared; every assertion is an exit status, a listed name or a message fragment.

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification` -- expected: five logs, zero warnings and errors (`CheckStagedTests.cs` and `VerifyPolicy.cs` are among the file-based apps compiled).
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: both canary `PASS` lines, the `Coupling dependencies:` summary, zero violations (both files are scored). Run as soon as the candidate compiles and again at handoff.
- AC1 and AC2 (a)–(e), each output saved; the AC2 (c) control once at the baseline, before the first edit.
- AC3, AC4, AC7 and AC8.
- The registration check and the two single-class runs -- AC5.
- `git diff --check <baseline_revision>` after `git add -N` on this contract and its run log -- expected: no output.
- One full local round under `caffeinate -i` -- AC6.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: passes; its static-gate selection at the baseline count.

### Running beside other work

Prerequisite: the coordinator's planning edit that records owner decisions 24m and 24n, the 24m merge rule and the coordinator ruling after it is published, and the baseline is captured from that main.

Final diff scope: `tools/anvil/VerifyPolicy.cs`, `tools/anvil/CheckStagedTests.cs`, `tools/anvil/fixtures.json`, `tools/anvil/README.md`, `_bmad/custom/bmad-loop-protocol.md`, `_bmad/custom/role-cards.md`, `docs/delivery-workflow.md`, `docs/planning/elements/readiness/completion-ledger.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, `TODO.md`, this contract and its run log. Nothing else.

Disjointness, checked against the owned-scope lists on `0959e883` (plus the worktree drafts). Code: `tools/anvil/` belongs to this lane alone (coordinator ruling after 24m), and no C# file this slice edits is edited by another listed slice. Shared documents: `_bmad/custom/*.md` and `docs/delivery-workflow.md` (coordinator ruling after 24m); `deferred-work.md` and `TODO.md` (24m); and `completion-ledger.md`, a planning document and not code under 24m's admission test ("disjoint from every other running lane's code"), shared by row with lane 3 (OS-04 against CA-01 and OS-02), as lane 3's contract also records, and with part D (after the engine programme; not a running lane; OS-04 against ES-37 and OS-05). A line citation that this slice moves in another contract, or that another slice moves in this one, is resolved at each later publisher's merge: that publisher re-reads the cited lines on the merged commit and its full round runs the changed gates (24m merge rule 3).

- Per-slice sharing with S3, S5, S5b, visible-solid parts B, B2 and C, part D, the Baseball repair, S6 and S6b–S6g, and lettered level ids (lane 3), with the citations each later publisher re-reads at its merge: moved verbatim to run log E13 (Contract budget).

Lane admission follows D7: this lane runs beside lanes 1, 2 and 3. One publisher at a time under the 24m merge rule.

## Auto Run Result

Status at review pass 1: blocked (resolved below; the current status is the frontmatter's).
Blocking condition: intent gap at review pass 1 (the `intent_gap` row); the options put to the coordinator are in run log E14 (moved verbatim, Contract budget).

Resolved 2026-11-25: coordinator decision, 25 Nov 2026, option (a) (Spec Change Log 1); blocked recovery resumes at `in-review` with the repair (run log E8).

Final result (2026-11-25): status `done`.
- Summary: the harness's children are isolated from inherited git variables and user, XDG and system configuration; the no-git case links the running runtime's own `dotnet`; the skip scan reads each staged path by its explicit stage-0 form `git show :0:<path> --` and carries git's error on one refusal line, passing git's warnings through on success; new harness cases cover P1, P3, P4, P7 (five), P8, P9 and P12, and pass cases prove their scan (P6); one cleanup shape; the count prints after cleanup; `SourceKind` deleted; the harness rule is stated once (`#policy-tool-harness`); `TODO.md` is on the data-only allow-list (24n); the registration filter gains two classes (798).
- Files: the twelve paths of the Final diff scope list, each with its change, in run log E20.
- Review: four passes; 39 findings in all (pass 1: 17, of which 1 intent_gap settled by coordinator decision and 6 false; pass 2: 16; pass 3: 3; pass 4: 3); every confirmed finding patched and re-reviewed; none deferred; rejected rows are pass 1's six false Intent Alignment readings, each refuted by the contract line cited in its row.
- Follow-up review recommendation: `false` (pass 4 patched three low record rows; no unverified risk named).
- Verification (E13, E14): capture builds 0 warnings and 0 errors; maintainability gate PASS (violations 0, both canaries, coupling summary); harness 78/0/0 with 78 `PASS ` lines; tool self-check 14; policy checker self-test 37/0/0 and `structural findings=0` on seven routes; registration check 798/0/0; AC7 one hit; AC8 eleven references to the one definition; `git diff --check` clean; full round 7309/0/0, `Skipped: 0`, bracketed by HEAD and HEAD-AFTER on an unchanged tree.
- Residual risks: the multi-line refusal and success pass-through are proven with `GIT_TRACE=1` on git 2.56.1 only; scratch probe directories under the host's temporary folder were left by denied `rm` calls (outside the checkout).
- Post-merge (2026-11-25): merged with `51529148` as `52c2c643` (E15; round 7339/0/0); its SnapshotApproval failed on S3 residuals R6 and R7, which the merge brought to this slice (Spec Change Log 3); repaired (E16, E18) and re-reviewed (passes 5 and 6, E19); the registration filter now has 21 classes and selects 809 (E16).
