---
title: 'Policy-tool and harness fixes deferred from S2 (lane 4)'
type: 'refactor'
created: '2026-11-24'
status: 'draft'
baseline_revision: 'captured at dispatch (the published main that contains owner decisions 24m and 24n)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/_bmad/custom/bmad-loop-protocol.md'
  - '{project-root}/tools/anvil/README.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** The S2 review deferred small gaps in the staged-source policy tool (`tools/anvil/CheckStagedTests.cs`), its no-argument verification harness (`tools/anvil/VerifyPolicy.cs`) and the loop documents to "whoever next changes a file under `tools/anvil/`" (`_bmad-output/implementation-artifacts/deferred-work.md:306–328`). The harness can inherit a caller's git state and configuration; its no-git case depends on how `dotnet` is installed; three refusal paths are proven only by fakes or not at all; some pass cases prove only an exit code; its cleanup and counts can misreport; the registration filter misses two classes main relies on; the role card omits a handoff step; and `TODO.md` is outside the data-only allow-list although it holds only the current brief, so every repair that edits it is reviewed at engine tier.

**Approach:** Close every deferred item that names this slice as owner, in the tool, its harness, its fixtures and the documents, each with an executed harness case or a recorded check; close the history-only items without editing history; and add `TODO.md` to the data-only allow-list (owner decision 24n).

## Boundaries & Constraints

**Always:** The staged-source rule's decisions are unchanged: every refusal and pass of `StagedSourceSelfCheck` (`CheckStagedTests.cs:102–132`, 14 cases) and of the existing harness cases still holds. Every refusal and pass case runs real git and the harness's isolated hook (which runs the real `CheckStagedTests.cs`; `VerifyPolicy.cs:34–36`) against a disposable repository outside the checkout; the P1 check runs real git and the P9 check real file-system removal, in temporary directories outside the checkout. A refusal names what it refuses and carries git's own text where git failed. The harness's child processes see no inherited `GIT_*` variable and no user, XDG or system git configuration, including git's default global excludes and attributes files. Policy changes apply from the next slice that starts (AGENTS.md Stable rules). Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** A change to what the hook refuses or scans, to `.githooks/pre-commit`, `tools/anvil/no-skipped-tests.json`, the maintainability or coupling gate (`VerifyPolicy.cs:966` onward) or the customization checker (`:428–965`). A test-only switch or environment variable read by `CheckStagedTests.cs`. Editing the published S2 contract or its run log, or `docs/delivery-workflow.md:34` (S5's). A fallback (a second way to find `dotnet`, a retry after a failed cleanup). Output hashes, mutants, browser runs or skipped tests.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| P1 | The harness process carries `GIT_DIR`, `GIT_CONFIG_PARAMETERS`, `GIT_CONFIG_COUNT`/`KEY_0`/`VALUE_0`, a `GIT_CONFIG_GLOBAL` file and an `XDG_CONFIG_HOME` whose `git/config`, each setting `core.hooksPath` to `/nonexistent`; the XDG directory also holds `git/ignore` naming `Leaked.cs` and `git/attributes` containing `Leaked.cs foo=bar` | A child's `git config --show-origin --get-all core.hooksPath` in the disposable repository is exactly `file:.git/config`, a tab, `.git-hooks`; a child's `git ls-files --others --exclude-standard` lists an untracked `Leaked.cs`; and a child's `git check-attr -a Leaked.cs` prints nothing | A leaked value fails the check, naming it |
| P2 | The `dotnet` first on `PATH` is a wrapper script that runs `env` by name; or `DOTNET_ROOT` is unset; or it names an empty directory | The no-git case runs the hook with `PATH` holding only a link to the `dotnet` at the root of the runtime the harness runs on, and refuses with `git could not be started` | If git starts all the same, the case fails for want of the refusal text |
| P3 | `APP.CSPROJ` staged, `NewTests.cs` untracked | Refused, listing `NewTests.cs` | Rule's refusal |
| P4 | `Kept.cs` staged, `NewTests.cs` untracked, real `git commit` through the hook | The commit exits non-zero, lists `NewTests.cs`, and no commit is created | Rule's refusal |
| P5 | A gitlink staged at `Module.cs` | Refused by one line naming the path and carrying git's own error text for `git show :Module.cs` | Tool's refusal |
| P6 | A skip-free staged test; an empty index | Pass, and the output shows the staged path was scanned (its `Anvil snapshot` line and the scanner's JSON report); the empty index passes with the self-check line and no snapshot line | A pass with no scan fails the check |
| P7 | A skip marker staged in `.js`, `.mjs`, `.cjs`, `.jsx` and `.tsx` files | Each is blocked by `TEST-001` | Skip rule |
| P8 | The hook run from a subdirectory, `NewTests.cs` untracked at the top level | Refused, listing `NewTests.cs` relative to the top level | Rule's refusal |
| P9 | Two temporary directories, the first undeletable | Removal attempts both, removes the second, and returns false | The cleanup failure is reported, never replacing an earlier failure |
| P10 | The registration filter at the baseline | It selects `CurrentCapabilityInputsTests` and `WorkshopIntegrationStartupTests` with the 18 existing classes | A class that selects nothing fails AC5 |
| P11 | A slice whose only path outside the allow-list was `TODO.md` (owner decision 24n) | Data-only tier under the protocol's Review tiers | Any other path keeps it engine tier |

</intent-contract>

## Code Map

Delivery shape: single structural tooling and policy slice (no element, family or closure). Review tier: **engine**, because `tools/anvil/`, `_bmad/custom/` and `docs/delivery-workflow.md` are tool, policy and workflow files outside the data-only allow-list; the capability probe is below. Baseline: captured at dispatch as the published main that contains owner decisions 24m and 24n and the coordinator ruling recorded after 24m (all uncommitted at drafting); every scope guard and `git diff` compares against it. Drafted and line-checked on `0959e883` (origin/main, 24 November 2026), which is the drafting revision only. Lane 4 of owner decision 24m.

**Status: draft.** Planning verdict: **Scoped Pass** (ready-for-dev) per the fifth audit (`docs/planning/elements/readiness/audits/audit-policy-tool-fixes-5.md`, 24 November 2026), whose four non-blocking findings are applied. Audits 1 to 4 (`audit-policy-tool-fixes-1.md` to `-4.md`) were each Incomplete; their findings (1–18, 1–6, 1–3, 1–6) are all applied. The owner decision is settled as owner decision 24n and lane admission by the coordinator ruling recorded after 24m (both 24 November 2026). Dispatch waits only for the publication of those planning records.

### Carry-forward: every deferred entry naming this slice

Source entries in `deferred-work.md` on `0959e883`. "In" items are this slice's acceptance; "Closed" and "Not taken" items get no change, with the reason.

| Id | Entry (`deferred-work.md`) | Item | Disposition |
|---|---|---|---|
| F1 | pass 8, `:325` | harness children clear only three git variables, not other `GIT_*` or user configuration | In: P1 |
| F2 | pass 8 | no-git case not exercised where `dotnet` is a wrapper script and `/bin` holds git | In: P2 |
| F3 | pass 8 | no case stages a non-scanned trigger extension in upper case | In: P3 |
| F4 | pass 8 | no staged-source refusal through a real `git commit` | In: P4 |
| F5 | pass 8 | the gitlink refusal names the path but not git's text | In: P5 |
| F6 | pass 8 | registration filter selects 767 where main's practice selects 798 | In: P10 |
| F7 | pass 8 | role card's handoff list lacks the policy-tool harness item | In: task 9 |
| F8 | pass 8 | the S2 contract's Code Map cites old line numbers | Closed, no edit: the entry's own owner line says "history and need no action", and AGENTS.md keeps historical artifacts unchanged. The dispatch brief listed it as a fix; this contract does not edit the published contract |
| F9 | pass 9 addendum, `:328` | cleanup-failure paths have no case | In: P9 |
| F10 | pass 9 | the passed-count line prints before a non-zero exit on cleanup failure | In: P9 |
| F11 | pass 9 | the cleanup result has two shapes | In: task 6 |
| F12 | pass 9 | run log E22 cites logs later overwritten | Closed, no edit: an append-only run log of a published slice |
| F13 | pass 7, `:318` | `.js`, `.mjs`, `.cjs`, `.jsx`, `.tsx` have no skip-content fixture | In: P7 |
| F14 | pass 7 | no case runs the tool from a subdirectory | In: P8 |
| F15 | pass 7 | `SourceKind` values in the scanned table are never read | In: task 4 |
| F16 | pass 7 | the harness-requirement sentence is repeated in three documents | In: task 9 |
| F17 | pass 6 BH10, `:312` | hand-copied check counts in `tools/anvil/README.md` and `TODO.md` | In: task 10 restates them from this slice's run |
| F18 | pass 6 EFE-o2, `:313` | Allow fixtures and the empty-index case assert exit 0 only | In: P6 (this slice changes the skip scan's refusal, so it is "the next slice that changes the skip check") |
| F19 | completion-ledger OS-04 (`completion-ledger.md:395`) | `TODO.md` absent from the data-only allow-list | In: P11 (owner decision 24n) |
| — | pass 6 EC3, EC4; the staged-source residual `:299–304`; pass 7's stray-file item (`:318`) | amend, merge and documents-only commits; stray `*.cs` files in the test project directory | Not taken: their recorded owner is each committer and the staged-source rule, with no slice queued; unchanged |

### Owned scope

- `tools/anvil/VerifyPolicy.cs` -- no-argument harness only: the disposable repository's local configuration `:43–45` (D1); fixture loop `:72–85` (Allow assertion `:81–84`); empty index `:86–88`; real-commit check `:92–98`; passed-count line `:99`; cleanup `:101–106`; `enum Source` `:108`; `Harness.Execute` `:117–139` (variables `:127`); `Harness.Remove` `:161–175`; `StagedSourceHarness` cases `:190–244`, `Verify` `:246–251`, `SkipScans` `:291–310` (gitlink case `:304–308`), `UnusableGit` `:312–337`, `PathWithoutGit` `:339–350` (comment `:339–341`), `RequireOutput` `:353–361`.
- `tools/anvil/CheckStagedTests.cs` -- `enum SourceKind` `:8` (deleted); the `SourcePaths` summary `:170–172`; `SourcePaths.Scanned` `:175–181` (a set); `SkipScan` `:227–265` (the `git show` at `:238` captures its error; the refusal at `:242–248` carries it).
- `tools/anvil/fixtures.json` -- one Block fixture for each of `.js`, `.mjs`, `.cjs`, `.jsx`, `.tsx`.
- `tools/anvil/README.md` -- tool self-check count `:52` (14, unchanged); the harness rule `:114` (linked, not restated); the inventory, counts and child-environment paragraph `:116–149`.
- `_bmad/custom/bmad-loop-protocol.md` -- handoff item 2 `:180`; the allow-list `:54–60` (owner decision 24n).
- `_bmad/custom/role-cards.md` -- implementation owner's "Before handoff" `:33–40`.
- `docs/delivery-workflow.md` -- an anchor `policy-tool-harness` before the harness rule `:58` (the one statement of the rule); the registration filter `:67`; the count sentence `:70`.
- `docs/planning/elements/readiness/completion-ledger.md` -- row OS-04 (`:395`) state column set to done.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- F1–F19 marked closed with this slice; any residual added with source, evidence and owner.
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

- **D1. All `GIT_*` variables are removed from every harness child,** then `GIT_CONFIG_NOSYSTEM=1`, `GIT_CONFIG_GLOBAL=/dev/null` and `LC_ALL=C` are set and the case's own `environment` applied. One rule covers `git rev-parse --local-env-vars` (15 names on git 2.56.1), the `GIT_CONFIG_KEY_n`/`VALUE_n` pairs it omits, and user and XDG configuration (`GIT_CONFIG_GLOBAL=/dev/null` suppresses the user and XDG configuration files). The disposable repository's local configuration also sets `core.excludesFile` and `core.attributesFile` to `/dev/null` (`VerifyPolicy.cs:43–45`), because git reads its default `$XDG_CONFIG_HOME/git/ignore` or `$HOME/.config/git/ignore` whenever `core.excludesFile` is unset, whatever `GIT_CONFIG_GLOBAL` says (probed on git 2.56.1, audit 4; record in the run log). `HOME` is kept, because the hook's `dotnet run` needs it.
- **D2. The no-git case links `<root>/dotnet`, runs `/bin/sh`, and sets `PATH` to that one directory,** where `<root>` is the directory three levels above `RuntimeEnvironment.GetRuntimeDirectory()` (`<root>/shared/Microsoft.NETCore.App/<version>/`), the runtime the harness runs on. No environment variable is read: `dotnet run` gives the app `DOTNET_ROOT_<ARCH>`, not `DOTNET_ROOT`, and passes a caller's `DOTNET_ROOT` through unchanged (probed on SDK 10.0.403, audit 3; record the probe in the run log), and `DOTNET_HOST_PATH` is absent whenever the caller sets `DOTNET_ROOT`. With `/bin` no longer on the child's `PATH`, the "`/bin` holds git" host condition is the one this host runs, so no seam is needed. If `dotnet run --file` cannot run in the hook with that `PATH` on this host, stop and return to planning.
- **D3. The gitlink case derives its expected text from real git.** The harness runs `git show :Module.cs` through `Harness.Execute` with `requireSuccess: false` in the same repository, requires a non-zero exit, and takes the first non-empty line of its output (stdout is empty for this failure, so the merged output is git's error); that line must appear inside the refusal line that starts `CheckStagedTests: the staged content of Module.cs could not be read`, and must occur exactly once in the hook's output, which proves the tool captured it instead of passing git's stderr through.
- **D4. One cleanup shape:** `Harness.RemoveAll(params DirectoryInfo[])` attempts every directory and returns whether all were removed; the main run and `UnusableGit` both use it. The passed-count line is printed only after `RemoveAll` returns true.
- **D5. The harness rule is stated once,** in `docs/delivery-workflow.md:58` under a new anchor `policy-tool-harness`; protocol handoff item 2, the role card's new item and `tools/anvil/README.md:114` link to `#policy-tool-harness`.
- **D6. `SourceKind` is deleted, not used.** The tool decides only "scanned or not"; the scanned set becomes `FrozenSet<string>`, and the `SourcePaths` summary (`:170–172`) drops "internal choices remain enum-typed". No closed set is lost: the enum's values were never read (`:189` reads keys only).
- **D7. Lane admission (coordinator ruling recorded after owner decision 24m, 24 November 2026).** Shared policy and workflow documents (`_bmad/custom/*.md`, `docs/delivery-workflow.md`) count as shared documents under the 24m merge rule, not lane code, so this lane runs beside the others; `tools/anvil/` code belongs to this lane alone, and each later publisher's full round on its merged commit runs the changed gates. This slice edits no C# file that another slice edits. Its shared documents are listed under "Running beside other work", together with `completion-ledger.md`, which it shares by row with lane 3 (OS-04 against CA-01 and OS-02) and part D (after the engine programme; not a running lane; OS-04 against ES-37 and OS-05).

### Owner decision

- **Owner decision 24n (24 November 2026; `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`).** `TODO.md` joins the data-only allow-list: it holds status text only, so editing it no longer raises a slice's review tier, and this slice makes the change. Wording: the last bullet of `bmad-loop-protocol.md:60` gains "and `TODO.md`, the current brief (outcome, blocker, next check and evidence links; owner decision 24n)", a path-based allowance needing no hunk reading. The owner decided it because Review tiers is an owner decision (`:50`) and the change removes Blind Hunter, Edge Case Hunter and Intent Alignment from slices that were engine tier only because of `TODO.md`. No owner decision is open.

### What this removes

The three-variable list at `VerifyPolicy.cs:127`; the first-`dotnet`-on-`PATH` lookup and the `/bin` condition in `PathWithoutGit`; the second cleanup shape (`List<bool>` in `UnusableGit`); the passed-count line before cleanup and its `+ 6` literal; `enum SourceKind`; the repeated harness-rule sentences in the protocol and the README; the stale `b5b9b233`/713 count sentence; the hand counts in `tools/anvil/README.md` (restated from this run).

## Tasks & Acceptance

**Execution:**

1. `VerifyPolicy.cs` `Harness.Execute` and the local configuration at `:43–45` -- D1; a check "an inherited git variable or configuration does not reach a child" -- P1.
2. `VerifyPolicy.cs` `PathWithoutGit`, `UnusableGit`, `RequireOutput` -- D2; every hook invocation in the harness uses `/bin/sh` -- P2.
3. `VerifyPolicy.cs` `StagedSourceHarness.Cases` -- add "an untracked C# file beside a staged `APP.CSPROJ` is refused" -- P3; after the real-commit skip check, add the real-commit staged-source refusal: stage `Kept.cs`, leave `NewTests.cs` untracked, `git commit`; require non-zero exit, `NewTests.cs` listed under "Not staged:", and `git rev-parse -q --verify HEAD` failing -- P4; add the subdirectory case: the hook run with working directory `nested/` (`/bin/sh ../.git-hooks/pre-commit`) -- P8.
4. `CheckStagedTests.cs` `SkipScan` -- capture `git show`'s standard error (read asynchronously, as `Git` does at `:274`) and append its trimmed text to the refusal line; `SourcePaths.Scanned` a `FrozenSet<string>`; delete `enum SourceKind`; update the summary at `:170–172` -- P5, D6. `VerifyPolicy.cs:304–308` -- D3.
5. `VerifyPolicy.cs` fixture loop and empty index -- an Allow fixture also requires the line `Anvil snapshot 0.ts: <path>` and the scanner's JSON report (`"summary"`) in the output; the empty index requires the self-check line and no `Anvil snapshot` line -- P6. `enum Source` gains `JavaScript`, `JavaScriptModule`, `JavaScriptCommon`, `JavaScriptReact`, `TypeScriptReact` with paths `sample.spec.js`, `.mjs`, `.cjs`, `.jsx`, `.tsx`; `fixtures.json` gains one Block fixture each (`test.skip("disabled", async () => {});`) -- P7.
6. `VerifyPolicy.cs` `Harness.Remove`, main cleanup, `UnusableGit` -- D4; a check "a directory the harness cannot delete is reported and fails the run": two fresh temporary directories outside the disposable repository, the first holding a mode-0500 subdirectory with a file; `RemoveAll(first, second)` returns false, the first still exists and the second is gone; the mode is restored in a `finally`, and `RemoveAll(first)` then returns true -- P9, F11.
7. The passed-count total is the number of checks that printed a `PASS` line (each check with its control counts once), printed after cleanup.
8. `docs/delivery-workflow.md:67` -- append `|FullyQualifiedName~CurrentCapabilityInputsTests|FullyQualifiedName~WorkshopIntegrationStartupTests`; `:70` -- replace the `b5b9b233`/713 sentence with the count this filter selects at the baseline and that SHA, or, when this slice publishes second, at the merged `origin/main` parent and that SHA (equal to the candidate's count, because this slice changes no test project) -- P10.
9. `docs/delivery-workflow.md:58` -- the anchor (D5); `_bmad/custom/role-cards.md:33–40` -- insert, as item 2, ``Policy-tool harness passes whenever a file under `tools/anvil/` changes ([rule](../../docs/delivery-workflow.md#policy-tool-harness))`` and renumber; `bmad-loop-protocol.md:180` and `tools/anvil/README.md:114` -- link to `#policy-tool-harness` instead of restating it -- F7, F16. `bmad-loop-protocol.md:60` -- the 24n wording above -- P11.
10. `tools/anvil/README.md:116–149` -- the inventory restated from this slice's harness run (cases, counts, D1's environment rule, D2's no-git case); `completion-ledger.md:395`; `deferred-work.md`; `TODO.md`.

**Acceptance Criteria:**

- AC1: Given the candidate, when `dotnet run --file tools/anvil/VerifyPolicy.cs` runs with no arguments, then it exits 0 and prints `Policy fixtures: Passed: N, Failed: 0, Skipped: 0.` with N = the baseline harness total + 10 (P1, P3, P4, P8, P9 one each; P7 five), re-measured at the baseline (67 at S2), and N equals the number of lines in that output beginning `PASS `.
- AC2: Given the candidate, when the same command runs (a) with the pinned toolchain first on `PATH`, (b) with a directory holding both `dotnet` and `git` first, and (c) with a scratch wrapper named `dotnet` (`#!/bin/sh` and `exec env "$DOTNET_ROOT/dotnet" "$@"`) first on `PATH` under the Verification environment, (d) with `DOTNET_ROOT` unset (`env -u DOTNET_ROOT`), and (e) with `DOTNET_ROOT` naming an empty scratch directory, then each passes with the AC1 total. Control for (c), recorded once in the run log: at the drafting revision's `PathWithoutGit`, the same wrapper makes the no-git case fail (exit 127, no refusal text).
- AC3: Given the candidate, when `dotnet run --file tools/anvil/CheckStagedTests.cs` runs in a clean checkout, then it reports its 14 self-check cases held.
- AC4: Given the candidate, when `dotnet run --file tools/anvil/VerifyPolicy.cs -- --bmad-check --self-test` runs, then `Fixtures: Passed: 37, Failed: 0, Skipped: 0.`; and `--bmad-check --project-root "$PWD"` reports `structural findings=0` for every route.
- AC5: Given the candidate, when the registration check in `docs/delivery-workflow.md` runs, then its count equals the count the `:70` sentence states (re-measured and restated on the merged candidate when this slice publishes second), with 0 failed and 0 skipped; and each of `CurrentCapabilityInputsTests` and `WorkshopIntegrationStartupTests`, filtered alone, selects more than zero tests, the total being the 18-class count plus those two counts.
- AC6: Given the candidate, when the full local round runs, then its total equals the baseline's, or on a merged candidate the merged main's total (this slice changes no test project), 0 failed, `Skipped: 0`.
- AC7: Given the candidate, when `git grep -n -e 'Whenever a file under .tools/anvil/. changes, run' -e 'The hook and CI build that harness but do not run it' -- AGENTS.md _bmad/custom tools/anvil docs/delivery-workflow.md docs/testing-guidance.md` runs, then it lists only `docs/delivery-workflow.md`.
- AC8: Given the candidate, when `git grep -n 'allow-list' -- AGENTS.md _bmad/custom docs/delivery-workflow.md` runs, then the list is defined only at `bmad-loop-protocol.md:54–60` and every other hit refers to it; the new bullet matches the 24n wording (read in review).

## Spec Change Log

## Review Triage Log

## Design Notes

1. **Behaviour and scope.** The policy tool refuses the same commits; its refusals carry git's text; the harness proves each refusal through real git, isolated from the caller; documents state each rule once. Source: `deferred-work.md:306–328`; completion-ledger OS-04.
2. **Reusable primitive.** None new; `Harness.RemoveAll` replaces two cleanup shapes.
3. **Declared composition.** `fixtures.json` stays the harness's data; the source-commit set stays held independently at `VerifyPolicy.cs:265–269`.
4. **Execution and ownership.** The harness writes only its own temporary directories; the tool writes only its temporary snapshot directory.
5. **Generality.** Each new case differs from an existing passing one in one cause (extension case, commit path, working directory, file kind).
6. **Forward-only.** Deletes `SourceKind`, the three-variable list, the PATH lookup and `/bin` condition, the second cleanup shape and the repeated sentences; no alias or fallback.
7. **Prerequisites.** Publication of owner decisions 24m and 24n and the coordinator ruling recorded after 24m. Engineering rules E2 (strict typing in tools), E5 (forward-only replacement: the deleted members have no remaining reference, proven by the compiler-proof capture of both file-based apps), E7, E8 and E9 apply (both files are file-based apps the maintainability gate scores); E1, E3, E4, E6 and the element-free design rules D1–D7 are untouched, because no engine, package, wire or presentation code changes.

## Verification

Executor for every command: the implementation owner. Raw output under `.tools/policy-tool-evidence/`; one line per result here, citing its run-log entry. A change to a file under `tools/anvil/` or `_bmad/custom/` invalidates the harness and checker results.

Environment: the pinned toolchain first on `PATH` (`PATH=$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403`); record the session environment check in the run log.

### Acceptance-to-test table

| Rows | Named discriminating check | Control and boundary | Run |
|---|---|---|---|
| P1 | Harness check "an inherited git variable or configuration does not reach a child": with P1's seeds set in the harness's own environment, the child's `git config --show-origin --get-all core.hooksPath` is exactly `file:.git/config`, a tab, `.git-hooks`, its `git ls-files --others --exclude-standard` lists an untracked `Leaked.cs`, and its `git check-attr -a Leaked.cs` prints nothing; the harness restores its environment in a `finally` | Control: only `GIT_CONFIG_GLOBAL=<the sentinel file>` passed through the case's `environment` argument shows `/nonexistent`, and the same `ls-files` with `GIT_CONFIG_COUNT=1`, `GIT_CONFIG_KEY_0=core.excludesFile`, `GIT_CONFIG_VALUE_0=<the seeded ignore file>` passed through `environment` omits `Leaked.cs`, and `git check-attr -a Leaked.cs` with `GIT_CONFIG_COUNT=1`, `GIT_CONFIG_KEY_0=core.attributesFile`, `GIT_CONFIG_VALUE_0=<the seeded attributes file>` passed through `environment` prints `Leaked.cs: foo: bar`, so the check can fail. A hand-kept three-name list fails the check, because the `GIT_CONFIG_COUNT` and `XDG_CONFIG_HOME` seeds would reach the child | planned |
| P2 | Existing "a git that cannot be started is a refusal, naming the query", under AC2 (c), (d), (e) | AC2 (a), (b); the recorded drafting-revision control | planned |
| P3 | "an untracked C# file beside a staged `APP.CSPROJ` is refused" | Control: "an untracked C# file beside a staged build input is refused" (`App.csproj`) | planned |
| P4 | "a real git commit with an unstaged new C# file is refused" | Control: the existing "real git commit rejected" (skip rule); boundary: `HEAD` stays unborn | planned |
| P5 | "a staged gitlink at a scanned path is a refusal naming the path and git's error" (D3) | `never: "Unhandled exception"` kept; git's line occurs exactly once, inside the refusal line | planned |
| P6 | Fixture loop Allow assertion; "empty index" | The Block fixtures still require `TEST-001` | planned |
| P7 | Five new Block fixtures, one per kind | Control: the existing "ordinary Playwright" Allow (`.ts`) | planned |
| P8 | "a new C# file is refused when the hook runs from a subdirectory" | Control: the same files with the hook at the top level (first existing case) | planned |
| P9 | "a directory the harness cannot delete is reported and fails the run" (task 6) | Control: the first directory after restoring its mode is removed. F10 (count printed only after `RemoveAll` returns true) is checked by code order in review | planned |
| P10, AC5 | Registration check command, the `:70` sentence and the two single-class runs | Count compared with the run's output | planned |
| P11, AC8 | The AC8 consistency check | Three contracts' tier reasons cited in the Capability probe | planned |
| AC3 | Tool self-check | 14 cases | planned |
| AC4 | Policy checker self-test and structural check | 37 fixtures; every route | planned |
| AC6 | Full local round | Baseline total | planned |
| AC7 | The AC7 `git grep` | `docs/delivery-workflow.md` is the one hit | planned |

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

- **S3**: shares `docs/delivery-workflow.md`, section "New-package registration check" (S3 rewrites `:64`; this slice edits `:58`, `:67` and `:70`), `deferred-work.md` and `TODO.md`. S3's AC3 counts the registration check at its baseline + 8; this slice changes that baseline (two classes added), so the later publisher re-measures on the merged candidate, and S3's own `docs/delivery-workflow.md` citations are re-read at that merge.
- **S5**: shares `_bmad/custom/bmad-loop-protocol.md` (S5 `:155`; this slice `:60` and `:180`), `_bmad/custom/role-cards.md` (S5 `:64`; this slice `:33–40`), `docs/delivery-workflow.md` (S5 `:34`; this slice `:58`, `:67`, `:70`), `deferred-work.md`, `TODO.md`. S5 cites `CheckStagedTests.cs:35,43,60` and `VerifyPolicy.cs:204,961,1092,1110` as checked-and-unchanged evidence; this slice moves those lines but keeps case `:204` ("a C# file under an ignored directory passes") and the gate code, and the moved citations are resolved at the later publisher's merge. S5's registration-check total (+7) shares this slice's baseline change.
- **S5b**: shares `_bmad/custom/bmad-loop-protocol.md` (its Maintainability gate paragraph, `spec-s5b-generate-typescript-wire-in-build.md:78`), `deferred-work.md`, `TODO.md`.
- **Visible-solid parts B, B2, C**: share `deferred-work.md` (B, C) and `TODO.md`; no code. B2 cites `TODO.md` as its only engine-tier reason; its tier was recorded at planning and stays engine (`bmad-loop-protocol.md:75`: a slice never moves from engine tier to data-only after planning); a slice whose planning passes after this slice publishes is tiered under 24n.
- **Part D (after the engine programme; not a running lane); the Baseball repair (lane 1); S6, S6b–S6g (lane 2)**: code disjoint (none edits `tools/anvil/`, `_bmad/custom/` or `docs/delivery-workflow.md`; S6f reads `delivery-workflow.md:46–51` only). Shared: `deferred-work.md`, `TODO.md`; part D also shares `completion-ledger.md` by row (ES-37, OS-05). The Baseball repair cites `TODO.md` as its only engine-tier reason (`spec-repair-baseball-el-239.md:96`); it is tiered as the B2 bullet states.
- **Lettered level ids (`spec-tool-lettered-level-ids.md`, lane 3)**: shares `completion-ledger.md` (this slice OS-04, that slice CA-01 and OS-02), `deferred-work.md`, `TODO.md`; no code. It cites `CheckStagedTests.cs:35`, which this slice moves (deleting `SourceKind` at `:8`); the citation is resolved at the later publisher's merge. Its new `Coverage.Tests` class is not in the registration filter, but its registration-check count shares this slice's baseline change (two classes added); the later publisher re-measures on the merged candidate (24m merge rule 3).

Lane admission follows D7: this lane runs beside lanes 1, 2 and 3. One publisher at a time under the 24m merge rule.
