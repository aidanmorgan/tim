---
title: 'S2: test project compile list becomes a glob'
type: 'chore'
created: '2026-11-21'
status: 'done'
baseline_revision: '7c019d64ee54a80bc0d31b82c90533985c2748ba'
review_loop_iteration: 2
followup_review_recommended: false
context:
  - '{project-root}/_bmad/custom/role-cards.md'
  - '{project-root}/docs/delivery-workflow.md'
warnings: []
deferred:
  - summary: >-
      Amending a source commit with nothing newly staged is not seen as staging source, so an untracked new C# file is not refused (review pass 6, EC3).
    evidence: |-
      The staged-source rule's trigger query is git diff --cached against HEAD (GitQuery.StagedInAnyStatus); no case covers git commit --amend. Recorded in deferred-work.md under "Deferred from the S2 review".
    location: >-
      tools/anvil/CheckStagedTests.cs
    severity: low
  - summary: >-
      A merge commit made through the hook counts merged-in source as staged, so an unrelated untracked C# file refuses it (review pass 6, EC4).
    evidence: |-
      The same trigger query lists every path a merge brings in; no case covers a merge. Recorded in deferred-work.md under "Deferred from the S2 review".
    location: >-
      tools/anvil/CheckStagedTests.cs
    severity: low
  - summary: >-
      Check counts in the policy README and TODO.md are hand-copied from a run and can drift from the harness (review pass 6, BH10).
    evidence: |-
      tools/anvil/README.md "Verified on" paragraph states counts that the harness computes and prints as "Policy fixtures: Passed: N". Recorded in deferred-work.md under "Deferred from the S2 review".
    location: >-
      tools/anvil/README.md
    severity: low
  - summary: >-
      The pre-existing skip-fixture Allow cases and the "empty index" case assert a zero exit only (review pass 6, EFE-o2).
    evidence: |-
      tools/anvil/VerifyPolicy.cs fixture loop and "empty index" check, unchanged from main 8d1d68aa. Recorded in deferred-work.md under "Deferred from the S2 review".
    location: >-
      tools/anvil/VerifyPolicy.cs
    severity: low
  - summary: >-
      Review pass 7 deferred candidates: no skip-content fixture for the scanned kinds other than .cs and .ts; no case runs the tool from a subdirectory; SourceKind values in the scanned table are unused; the harness sentence is repeated in three documents; stray files matching *.cs in the test directory would be compiled.
    evidence: |-
      tools/anvil/fixtures.json has CSharp and TypeScript sources only; every harness case runs at the disposable repository's top level; SourcePaths.Scanned values are never read. Recorded in deferred-work.md under "Deferred candidates from the S2 review (pass 7)".
    location: >-
      tools/anvil/CheckStagedTests.cs
    severity: low
  - summary: >-
      Review pass 8 deferred candidates: other inherited git variables and user git configuration are not isolated in harness children; a wrapper-script dotnet where /bin holds git; no upper-case case for the non-scanned trigger extensions; no staged-source refusal through a real git commit; the gitlink refusal omits git's own text; the documented registration filter selects 767 where main's practice selects 798; the role card's handoff list lacks the harness item; the Code Map names policy edits by old line numbers. Review pass 9 adds: the cleanup-failure paths of the harness have no case; a passed-count line prints before a non-zero exit on cleanup failure; the cleanup result has two shapes; run log E22 cites two logs later overwritten.
    evidence: |-
      tools/anvil/VerifyPolicy.cs Harness.Execute and PathWithoutGit; tools/anvil/CheckStagedTests.cs SkipScan; run log E21 (767 and 798); _bmad/custom/role-cards.md. Recorded in deferred-work.md under "Deferred candidates from the S2 review (pass 8)".
    location: >-
      tools/anvil/VerifyPolicy.cs
    severity: low
---

<intent-contract>

## Intent

**Problem:** `CuriousContraptions.tests/CuriousContraptions.tests.csproj` lists every local test source by hand (236 `Compile` items). Each slice that adds a test file must edit it, it is the most-edited source file since 5 November, and a forgotten line means a test file that never ran until the static gate fails the full round.

**Approach:** Replace the hand list of local sources with one rule that compiles every C# file directly in the test project directory. The two sources linked from other projects stay explicit. No test, product code or gameplay changes.

## Boundaries & Constraints

**Always:** The test project compiles exactly the same set of sources after the change as before it. The full local round keeps its test total with zero failed and zero skipped. One rule decides membership; a new top-level test file needs no project edit.

**Never:** No change to any test or production source. No second membership mechanism kept beside the rule (no leftover explicit local entries, no exclusion list, no condition). No change to other projects' source lists, to the static gate's rules or thresholds, or to registration tables owned by the registration-derivation slice. No subdirectory sources are pulled in (`Fixtures/` holds data, not tests).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| M1 Same sources | Build before and after the change | The compiler's source list for the test project is identical as a set (236 local + 2 linked) | No error expected |
| M2 New file | A new `*.cs` file placed directly in `CuriousContraptions.tests/` with no project edit | It is compiled into the test project and the static gate's "every repository C# file is compiled by a project" guard accepts it | No error expected |
| M3 Control: outside the rule | A `*.cs` file placed in a subdirectory of the test project | It is not compiled by the test project, and the static gate reports it as a C# file no project compiles | Gate failure names the file |

</intent-contract>

## Code Map

Original baseline (`baseline_revision`) `7c019d64ee54a80bc0d31b82c90533985c2748ba`, which was origin/main when the slice started; comparison base since the second merge (run log E21): published main `fa86969f1866368ae44da7d20f885453045a599a`; between the first merge (run log E17) and the second it was `8d1d68aa262bb3f89ee9d073bbbaf3a83b9f9322`. Checkout `.claude/worktrees/story-test-compile-glob`, branch `story-test-compile-glob`. Policy revision is the baseline; the owned policy delta is named in the scope list below. Implementation owner: recorded in the run log at first launch.

Owned scope list:

- `CuriousContraptions.tests/CuriousContraptions.tests.csproj` -- lines 72–309: comment plus one `ItemGroup` holding 2 linked `Compile` items (lines 74–75) and 236 local ones; `EnableDefaultCompileItems` is `false` at line 7. The only product change.
- `docs/delivery-workflow.md` -- line 62 says the static gate refuses "a test file missing from the explicit `Compile` list"; reword to the remaining case (a C# file outside any project's sources).
- `_bmad/custom/bmad-loop-protocol.md` -- line 46 names "the test project's `Compile` list" among files an Early level proof shares; remove that item only.
- `docs/planning/invest/delivery-batches.md` -- S2 story: mark delivered by this slice.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- line 264 S2 pointer: mark delivered. Line 258's correction sentence is history and stays.
- `TODO.md` -- current outcome.
- `tools/anvil/CheckStagedTests.cs` and `tools/anvil/README.md` (lines 7–10 and 25 describe what the hook checks) -- added to scope by review pass 2, replacing the pass-1 shell guard: one staged-source rule with executed fixtures. The hook already runs this tool from the real checkout against the real index (`.githooks/pre-commit:48`, pinned at `CSharpStaticGateTests.cs:1325`), so `.githooks/pre-commit` and `CuriousContraptions.tests/CSharpStaticGateTests.cs` return exactly to the baseline. Owned policy delta: these two files, `_bmad/custom/bmad-loop-protocol.md` line 46 and `docs/delivery-workflow.md`.
- `tools/anvil/VerifyPolicy.cs`, no-argument harness only. **Current rule: the harness holds the real-git cases that Spec Change Log entry 3 and the pass-4 triage rows list, and `tools/anvil/README.md` states the count from the actual run. The case list and the count of 22 in the rest of this line are the pass-2 plan, superseded.** (lines 21–86 at the baseline; the gate code is not touched) -- added to scope when the owner returned the amendment to planning: that harness is the documented verification of `CheckStagedTests.cs` and runs it inside a disposable repository where the tool's own copy is an untracked C# file, so the new rule would refuse every case. Changes: the harness copies the tool outside the disposable repository and runs it with the repository as working directory, as the real hook does from its snapshot; at the start of every case, after the index is emptied, the harness deletes the sample files of both source kinds, so the only source file on disk is the one that case stages (planning B1: otherwise the four TypeScript cases run with the previous `SampleTests.cs` untracked); `SampleTests.cs` is also absent for the "empty index" case, which stays a pass; one new case, empty index plus an untracked C# file, is refused with the rule's message and without `TEST-001`; the registry file `no-skipped-tests.json` stays inside the disposable repository; the check count becomes 22 and `tools/anvil/README.md` states it, with its "Verified on" line restated from the actual rerun. Planning verdict on this addition: Incomplete on B1 alone, amended here with the auditor's stated fix. Fixtures in `CheckStagedTests.cs` use a fake that answers only the exact query argument lists, held as independent literals, so a changed pathspec or a dropped `--exclude-standard` fails them; a git failure is a returned value, not a caught exception. Verification adds `dotnet run --file tools/anvil/VerifyPolicy.cs` with no arguments: every check passes.
- This contract and `_bmad-output/implementation-artifacts/run-logs/spec-s2-test-compile-glob.md`.

Read-only evidence:

- `CuriousContraptions.tests/CSharpStaticGateTests.cs:522` `EveryRepositoryCSharpFileIsCompiledByAProject` and `:510`, `:513` read the compiler logs named by `CSHARP_STATIC_GATE_BINLOGS`; nothing in the tree pins the explicit list itself (searched `Compile Include`, `EnableDefaultCompileItems`, `tests.csproj` across C#, project, props, targets, scripts, workflows and docs).
- `tools/Ownership/SourceInventory.cs:16,97` reads the test project's evaluated `Compile` items through MSBuild, so it follows the rule without change.
- `tools/anvil/VerifyPolicy.cs:958` inventories evaluated `Compile` items; test projects are excluded from the gate.
- Directory check on the baseline: 236 top-level `*.cs` files, equal to the listed local items; the only subdirectory is `Fixtures/` with no C# files.

**Capability probe.** Not dispatched: the slice uses no engine, abstraction, bundle, wire or presentation capability and adds no package; the protocol requires the scout for element slices and slices that plan to use an engine capability. Direct consumers are the three read-only items above. Open owner decisions: none.

**Delivery shape and review tier.** Single structural slice, no identity. Engine tier: the diff is build configuration and policy text, which the allow-list excludes.

**Design answers (E-rules).** Behaviour and scope: build membership only; retained behaviour is the identical source set (M1). Forward-only: the explicit local list is deleted, with no alias or second mechanism. Reusable primitive, declared composition, execution and ownership, generality: not applicable, no engine or element change. Prerequisites: none; registration-derivation and Bellows are in flight and do not own this file's list, though Bellows may add lines to it, which a merge resolves by dropping them.

**Planning verdict.** Scoped Pass on the review-pass-2 amendment (planning auditor `a5936bbe302620268`, pass 2 of the amendment, 21 November 2026; Spec Change Log second entry), with the `tools/anvil/VerifyPolicy.cs` harness addition and its B1 fix as recorded in the scope list. It follows the original Scoped Pass (planning audit pass 2; pass 1 Incomplete on F1 and F2; run log E3).

## Tasks & Acceptance

**Execution:**
- `CuriousContraptions.tests/CuriousContraptions.tests.csproj` -- replace the 236 local `Compile` items with exactly one `<Compile Include="*.cs" />`; keep `EnableDefaultCompileItems` false (the SDK default is recursive and would pull in subdirectories), keep the two linked items, and update the `ItemGroup` comment to state the rule -- one membership rule.
- Staged-source rule in `tools/anvil/CheckStagedTests.cs`. **Normative text: Spec Change Log entry 3 (a) to (g) with its pass-4 refinement. What follows in this line is the pass-2 plan, superseded where it differs (when the rule runs, the remedies, the fixture list); the removal of the pass-1 guard and pin and the README update stand.** Pass-2 plan: refuse the commit, naming each path, when any non-ignored C# file in the working tree is untracked or intent-to-add. Queries, run from the repository top level: `git ls-files -z --others --exclude-standard -- ':(top,glob,icase)**/*.cs'` and `git diff-files -z --name-only --diff-filter=A -- ':(top,glob,icase)**/*.cs'`. The rule runs before the tool's existing early return for "no staged source file", so a documents-only commit is checked too. The message offers three remedies: stage it, delete it, or move it out of the repository tree. It checks new files only; unstaged edits to a tracked file are not checked, as before this slice, and the wording says so. The tool runs its fixtures first on every invocation, with git's answers injected, and refuses if any fixture fails (as `VerifyPolicy` does with its canaries). Fixtures: untracked refused; intent-to-add refused; upper-case extension refused; nothing staged plus one untracked C# file refused; non-ASCII name fully staged passes; everything staged passes; a file under an ignored directory passes (git does not list it); an untracked non-C# file passes; a git failure is a named refusal, never a pass. Remove the pass-1 shell guard from `.githooks/pre-commit` and the pass-1 `InlineData` pin from `CuriousContraptions.tests/CSharpStaticGateTests.cs`. Update `tools/anvil/README.md` to describe the rule.
- `docs/delivery-workflow.md` line 62 -- state the rule as it now is (what the tool refuses and the one stated case it does not are those of Spec Change Log entry 3, not "every new C# file"; no standing check replaces the build's refusal of a deleted test file): a new test file is compiled without a project edit; it must be staged with `git add` before the candidate commit, which the hook's policy tool enforces for every new C# file; `git add -N` at handoff serves `git diff --check` only, and the committer replaces it with `git add` when staging the candidate; a new test file still gets the registration check; a deleted test file shows as a deleted path.
- `_bmad/custom/bmad-loop-protocol.md`, `docs/planning/invest/delivery-batches.md`, `_bmad-output/implementation-artifacts/deferred-work.md` -- the one-line edits named in the Code Map -- active documents state the current rule.
- Verification of M1–M3 as below. M2 and M3 use temporary probe files that are deleted before handoff and never committed.

**Acceptance Criteria:**
- Given published main's explicit list (`fa86969f`, the comparison base since the second merge) and the candidate's rule on the same sources, when the test project is built with a compiler log, then the two source lists are equal as sets.
- Given the candidate, when the full local round runs, then its listed test names equal those of main's explicit list on the same sources and its total equals main's recorded full round (7294), with zero failed and zero skipped.
- Given the candidate, when `git diff --stat fa86969f1866368ae44da7d20f885453045a599a` is read, then no file outside the owned scope list changed and no test, engine or element source changed.

## Spec Change Log

### 2026-11-21 — reading of the Never clause (review pass 1, no re-derivation)

- Trigger: the staging-gap repair adds a pre-commit guard, and the hook's protections are pinned by `CSharpStaticGateTests.TheHookKeepsEachProtection`. The intent contract's Never clause says no test source changes.
- Reading recorded, intent block unchanged: the source story (`delivery-batches.md` S2 acceptance) expects a static-gate test to assert the rule where one pins it, so the clause forbids changing test behaviour or any other test source; one added pin for the new hook guard in that existing fact is inside the intent. Nothing else in a test source may change.
- Known-bad state avoided: an unpinned hook guard that a later edit could drop silently.
- KEEP: the single `<Compile Include="*.cs" />` rule, `EnableDefaultCompileItems` false, the two linked items, the hook guard reading the directory rather than git's untracked list.

### 2026-11-21 — bad_spec: where the staged-source rule lives (review pass 2)

- Trigger: pass 2 confirmed that the pass-1 shell guard, which this contract specified, is wrong as designed: it compares `find` output with quoted git output (false refusal of a staged non-ASCII name), is case-sensitive where the build rule is not, covers one directory while four other test projects compile by glob, overstates what it checks, and has no executed test, only a text pin whose added theory case also broke the intent's unchanged test total.
- Amended: Code Map scope, Tasks and Verification now specify one staged-source rule in the policy tool the hook already runs, with executed fixtures; the shell guard and the pin are removed.
- Supersedes the entry above: with the pin removed, no reading of the Never or Always clause is needed; no test source changes and the round total stays 7232.
- Reading recorded for the Never clause: `tools/anvil/` is tool code, which the engineering contract's E9 distinguishes from production code; the clause's "test or production source" is untouched by a change there. Planning audit of this amendment: pass 1 Incomplete (A1 the tool named did not have fixtures the hook runs, A2 early return, A3 pathspec scope, A5 table), amended; A4 closed by evidence below; A6 and A7 adopted.
- Ignored files (pass-1 BH3, planning A4): the rule's set is non-ignored files, the set the static gate uses. Evidence that nothing hides a test source today: no tracked ignore pattern matches a `.cs` file (root `.gitignore`, `_bmad/custom/.gitignore`, `_bmad/render/.gitignore`), checked by the planning auditor on 21 November 2026; only a deliberate local exclude could.
- Known-bad state avoided: a commit-completeness guard proven only by its own text, and an acceptance total that the repair itself changes.
- KEEP: the single `<Compile Include="*.cs" />` rule, `EnableDefaultCompileItems` false, the two linked items; the corrected `docs/delivery-workflow.md` wording on registration, deletion and the hand-maintained tables; the past-tense story bullet; M1–M3 evidence (run log E5, E6), which the amendment does not touch.

### 2026-11-21 — bad_spec: when the staged-source rule runs, and what proves it (review pass 3)

- Trigger: pass 3 (first pass on the re-derived candidate) confirmed two things the amended Tasks specified wrongly. The rule ran on every commit, so a checkout holding another session's untracked C# files (the primary checkout has five) could not make even a documents-only commit, against the rule to preserve unrelated dirty work. And six of the nine in-tool fixtures prove only that a fake git answers a literal query, while the one real-git case covers an untracked file alone.
- Amended, superseding those parts of the Tasks line above: (a) the intent-to-add query runs on every commit, because an intent-to-add entry is the committer's own index state and `git diff --cached` does not list it; the untracked query runs only when the commit stages at least one source file the tool scans (C# or TypeScript). Remaining hazard, stated plainly: a wholly untracked new C# file in a commit that stages no other source is not caught by the hook; the handoff's `git status` check and the Final diff scope check cover it. (b) The message offers two remedies, stage the file or move it outside the repository tree, and says not to delete a file the committer does not own. (c) In-tool fixtures are the cases that exercise the rule's own decision: untracked refused; intent-to-add refused; nothing staged plus intent-to-add refused; both lists empty passes; no staged source with an untracked file passes; git failure is a named refusal. (d) Git's own behaviour is proven by real-git cases in the `VerifyPolicy.cs` harness: untracked refused; `git add -N` refused; nothing staged plus `git add -N` refused; a new file in a nested directory refused; upper-case extension refused (a name that does not collide case-insensitively with the sample file); a C# file under an ignored directory passes (ignore rule in the disposable repository's `.git/info/exclude`); a staged new C# file beside other staged source passes; a non-ASCII staged name passes; no staged source with an untracked C# file passes. Every refusal case stages a source file where the rule needs one, and asserts the rule's message and no `TEST-001`; per-case cleanup also removes the nested and ignored directories. (e) The tool's extension lookup is case-insensitive, so a staged `.CS` file is skip-scanned, with a harness case; no tracked file has a non-lowercase scanned extension today. (f) The self-check prints one line that does not imitate a test-run receipt.
- (g) Ignored files (planning D1, repairing EC2 in this slice): when a scanned source is staged, the tool also runs `git ls-files -z --others --ignored --exclude-standard --directory -- ':(top,glob,icase)**/*.cs'` and refuses every entry that does not end in `/`; collapsed ignored directories (build output, `.tools/`, other worktrees) end in `/` and are not refused. One more in-tool case (an individually ignored C# file is refused) and one more real-git harness case (a C# file ignored by name is refused; the ignored-directory case still passes), making seven in-tool and ten real-git cases. Planning verdict on entry 3 after this: the auditor's remaining finding D1 named this repair as its recommended fix; adopted verbatim.
- Pass-4 refinement of (a), a clarification of "stages source", not a new design, with the pass-5 set: the untracked and ignored-by-name queries run when the commit stages any path, in any status (added, copied, modified, renamed, type-changed or deleted), whose extension the tool scans (`.cs`, `.js`, `.mjs`, `.cjs`, `.jsx`, `.ts`, `.tsx`) or is one of `.csproj`, `.props`, `.targets`, `.slnx`, `.sln`, `.rsp`, `.editorconfig`, `.json`, `.tscn`. A deletion or type change triggers the queries but is not skip-scanned. A documents-only commit (`.md`, `.yaml`, `.toml`) does not trigger them. Planning verdict on entry 3 as amended with (g) and this refinement: recorded in run log E15.
- Planning audit of this entry: pass 1 Incomplete (C1 the "every candidate commit" premise was false for a commit whose only source is an intent-to-add or untracked test file; C2 the EC2 rejection cited a static-gate fact that tests paths, not git's file set; C3 Verification contradicted the entry); amended here and in the EC2 row and Verification.
- KEEP: everything in the previous entry's KEEP list; the tool placement, the two queries with `-z` and `:(top,glob,icase)**/*.cs`, the harness running the tool from outside the disposable repository, the per-case cleanup.
- Documents: `tools/anvil/README.md` states the hook's real order (snapshot and builds first, then this tool), names `dotnet run --file tools/anvil/CheckStagedTests.cs` as the cheap check before a candidate commit, and gives the new check count; `docs/delivery-workflow.md` says no standing check replaces the build's refusal of a deleted test file and that the diff and the AGENTS.md deletion rule cover it.

## Review Triage Log

### 2026-11-21 — Review pass 1 (engine tier, all six layers)
- verdicts: 23 findings — high 1, medium 7, low 9, false 6, maybe-false 0
- layers: Blind Hunter `a0f0b712b57aa20a1`, Edge Case Hunter `a9677708ffa9be673`, Verification Gap `afd86cc0850207293`, Intent Alignment `ae57c9e05358dc7a9`, Forward-Only `a5b6fb9473967380d` (no violation), Element-Free Engine `af6ee13c1a6776a2f` (tier confirmed, D1–D7 not applicable); all fresh, no history.
- findings: the 23 rows are in run log R1, verbatim.

### 2026-11-21 — Review pass 2 (re-review of the pass-1 repair; Blind Hunter, Edge Case Hunter, Verification Gap, both auditors)
- verdicts: 21 findings — high 0, medium 6, low 12, false 3, maybe-false 0
- layers: Blind Hunter `aab7ca116a552c06c`, Edge Case Hunter `ac74cd297d8a6a9fe`, Verification Gap `a1f479018fe6ece04`, Forward-Only `a946342dddc23ae98` (no violation), Element-Free Engine `a92057fe8d048d12d` (tier confirmed); all fresh.
- findings: the 21 rows are in run log R2, verbatim.
- Stagnation check (two repair attempts at the same finding): the shell-in-hook design was the cause; the next discriminating check is the tool's fixtures, which execute each refusal case.

### 2026-11-21 — Review pass 3 (first pass on the re-derived candidate; engine tier, all six layers)
- verdicts: 25 findings — high 0, medium 4, low 12, false 9, maybe-false 0
- layers: Blind Hunter `a6105a4325d168061`, Edge Case Hunter `aa5db9bfd1834f7db`, Verification Gap `af8706f429d89ae60`, Intent Alignment `a3ae070bbbee1a291`, Forward-Only `a86eaf4e0bf19718a` (no violation), Element-Free Engine `a27c1900d4c813ee1` (tier confirmed; every pass-2 amendment present); all fresh.
- findings: the 25 rows are in run log R3, verbatim.

### 2026-11-21 — Review pass 4 (first pass on the candidate re-derived after entry 3; engine tier, all six layers)
- verdicts: 33 findings — high 0, medium 3, low 16, false 13, maybe-false 1
- layers: Blind Hunter `afeaeb4c00824d6c7`, Edge Case Hunter `a807783cb4f35b596`, Verification Gap `a3ac78ee16c0691f5`, Intent Alignment `a06b55f794bf46f1f`, Forward-Only `af93b96e52dd46dad` (no violation), Element-Free Engine `ac39d1ac15f70c3b0` (tier confirmed; every pass-3 row and entry 3 (a) to (g) present); all fresh.
- findings: the 29 rows (33 findings; one row groups EC6 to EC10) are in run log R4, verbatim.

### 2026-11-21 — Review pass 5 (second pass after entry 3; Blind Hunter, Edge Case Hunter, Verification Gap, both auditors; candidate includes main `8d1d68aa`)
- verdicts: 19 findings — high 0, medium 1, low 16, false 1, maybe-false 1
- layers: Blind Hunter `adb0e3a2a8db6d072`, Edge Case Hunter `ad0b0275f6ff0faa2`, Verification Gap `ada58a28dfeed6aab`, Forward-Only `a289dbc9bd8ecec31` (no violation), Element-Free Engine `a02f28d046b252914` (Scoped Pass for its layer; every pass-4 patch row present; merge resolution keeps main's content; Final diff scope Pass on the pass-5 candidate, to be repeated after these patches).
- findings: the 19 rows are in run log R5, verbatim.
- Re-review scope from the next pass (the third after entry 3): a new finding outside this repair and what it affects that breaks no hard rule, required-now criterion or behaviour is deferred under the protocol.

### 2026-11-21 — Review pass 6 (third pass after entry 3; Re-review scope applies)
- verdicts: 24 findings — high 0, medium 1, low 21, false 2, maybe-false 0
- layers: Blind Hunter `a581b9d2a5cff1943`, Edge Case Hunter `a78f82c2f7b6209f1`, Verification Gap `a0744611cc7b35972`, Forward-Only `a2cf43e13656ed86a` (no violation, no finding), Element-Free Engine `a60b366956729bba9` (element-free gate Scoped Pass; maintainability evidence applicable; Final diff scope Pass on the pass-6 candidate, repeated after these patches).
- findings:
  - `[medium]` `[patch]` EC1 a staged type change (symlink to regular file) is a trigger but is not skip-scanned — directly affected by the repair and it weakens the hook's no-skip check: the scan filter adds `T`, with a real-git case.
  - `[low]` `[patch]` BH2, EFE-o1 same — EC1 root cause.
  - `[low]` `[patch]` VG1, BH1, EFE2 the repository-lookup refusal has no retained check on the final source — inside the repair: a harness case runs the tool outside any repository.
  - `[low]` `[patch]` VG2, BH3, EFE1 trigger extensions without a row, and in-tool cases use their own predicate — inside the repair: in-tool cases use the production predicates, and the harness derives one refusal row per member of the production trigger set, so the rows cannot fall behind the set.
  - `[low]` `[patch]` VG3, BH6 the real failure case does not assert git's error text — inside the repair: it requires a fragment git wrote.
  - `[low]` `[patch]` EC2 a git that cannot be started still throws — inside the repair: a named refusal.
  - `[low]` `[patch]` BH4 the "is not scanned" label claims more than the pass case asserts; BH5 the comment says the self-check line shows the rule ran — inside the repair: label and comment say what is asserted.
  - `[low]` `[patch]` BH7 the harness sentence is placed inside the gate item and between a command and its explanation — inside the repair: its own handoff item and paragraph.
  - `[low]` `[patch]` EFE3 two cause-unknown items from the close note (the refused gate run when MSBuild worker nodes exited; listed names 7,042 against 7,246 results) are not in `deferred-work.md` — required by the protocol's close-note rule.
  - `[low]` `[patch]` EFE4 the close note names classes that recurred within the slice, not from an earlier slice — stated explicitly.
  - `[low]` `[patch]` EFE5 `TODO.md` names steps now complete — restated for the commit.
  - `[false]` `[reject]` BH8 carried: README hook order — pass-4 BH3.
  - `[false]` `[reject]` BH9 the wider trigger refuses more commits in a checkout holding another session's untracked C# files — owner decision, 21 November 2026: keep the rule; the message says to stage or move, never delete.
  - `[low]` `[defer]` EC3 amending a source commit with nothing newly staged is not seen as staging source — outside the repair, breaks no rule or behaviour: recorded beside the stated residual in `deferred-work.md` (deferred under Re-review scope).
  - `[low]` `[defer]` EC4 a merge commit made through the hook counts merged-in source as staged, so an unrelated untracked C# file refuses it — outside the repair: recorded with EC3.
  - `[low]` `[defer]` BH10 counts in README and `TODO.md` are hand-copied and can drift — outside the repair: recorded with EC3.
  - `[low]` `[defer]` EFE-o2 the pre-existing skip-fixture Allow cases and "empty index" assert exit 0 only — outside the repair, unchanged from main: recorded with EC3.

**Final diff scope (pass-6 candidate):** Pass — eleven owned paths against main `8d1d68aa`, every hunk inspected by the Element-Free Engine auditor; nothing removed; no justified simplification beyond routing the in-tool cases through the production predicates, adopted in the pass-6 patch. Repeated on the patched candidate.

### 2026-11-21 — Review pass 7 (fourth pass after entry 3; Re-review scope applies)
- verdicts: 13 findings — high 0, medium 2, low 11, false 0, maybe-false 0
- layers: Blind Hunter `ada89a2b24dfccbaa`, Edge Case Hunter `ad15d9ef59f495471`, Verification Gap `a77b38a1e9aeeb316`, Forward-Only `a1db10ea6dfa309ea` (no violation), Element-Free Engine `a699ed1047a6dc9bb` (gate clean; every pass-6 row present; maintainability evidence applies; Final diff scope Pass on scope and simplification, Incomplete until these rows are repaired).
- findings (all inside the pass-6 repair or directly affected by it):
  - `[medium]` `[patch]` BH1, EC1, VG1, EFE1 the "git cannot be started" case depends on where `dotnet` and `git` are installed and fails on a host where they share a directory — a test that cannot run in a supported environment: the child gets a harness-made directory holding only a link to `dotnet`, and the case refuses clearly if git still resolves.
  - `[medium]` `[patch]` EC4 the harness does not clear inherited `GIT_DIR`, `GIT_WORK_TREE` or `GIT_INDEX_FILE`, so run from inside a hook its setup commands could act on the caller's repository — every child process clears them.
  - `[low]` `[patch]` BH2a, EC2 the outside-repository case assumes the temporary directory's parent is in no repository — a fresh directory with `GIT_CEILING_DIRECTORIES` set.
  - `[low]` `[patch]` BH2b, VG3 git's error fragments are matched in English without fixing the child's locale — children run with `LC_ALL=C`.
  - `[low]` `[patch]` BH3, VG2, EFE2 derived rows take their expected set from the tool, so a dropped member fails nothing; the self-check's expected scan set uses the production predicate — the harness holds the documented sixteen extensions as its own literal and requires the printed set to equal it; in-tool cases state literal expected scan paths. Stagnation check: third pass on trigger coverage; the independent literal is the discriminating check that ends it.
  - `[low]` `[patch]` BH4, FO-note, EFE3 the README's opening paragraph still says added, copied, modified and renamed for the skip scan — it names type changes too.
  - `[low]` `[patch]` EC3 a staged type change to a gitlink at a scanned path makes `git show` fail with an unlabelled exception — a named refusal that names the path.
- deferred candidates (outside the repair, breaking no rule; recorded in `deferred-work.md` as one entry): the JavaScript and TypeScript scanned kinds beyond `.ts` have no content fixture; no case runs the tool from a subdirectory; `SourceKind` values in the scanned table are unused; the harness sentence is repeated in three documents; stray editor or OS files matching `*.cs` in the test directory would be compiled (carried from pass 1).
- record-keeping: the contract is over the 8,000-word close budget; the finding rows of passes 1 to 5 move to the run log, each pass keeping its verdict line, layer line and one line citing the run-log entry.

### 2026-11-22 — Review pass 8 (fifth pass after entry 3; Re-review scope applies; candidate includes main `fa86969f`)
- verdicts: 4 findings — high 0, medium 0, low 4, false 0, maybe-false 0
- layers: Blind Hunter `a9d82ebf36b4037e6`, Edge Case Hunter `adb01f1a931fead51`, Verification Gap `a113858daac823490` (no in-scope finding; each pass-7 case shown to fail if its fix is reverted), Forward-Only `a5e57da130a9647ea` (no violation), Element-Free Engine `a825711e7a1a48307` (Scoped Pass for its layer; all seven pass-7 rows repaired; second merge keeps main's content; maintainability evidence applies; Final diff scope Pass).
- findings (all in the pass-7 harness code or its records; each fails closed and none can produce a false pass):
  - `[low]` `[patch]` EC1 the no-git case's `finally` deletes its two directories with raw calls, so a failed first delete leaks the second and masks the case's own failure — it uses the harness's `Remove`, as the other temporary directories do.
  - `[low]` `[patch]` EC2 a relative PATH entry holding a non-link `dotnet` makes a dangling link — the resolved path is made absolute.
  - `[low]` `[patch]` BH1 the "git still resolves" stop can never fire, because the PATH is built to exclude git — the unreachable guard is removed; the case still fails on the missing refusal text if git starts.
  - `[low]` `[patch]` EFE1 `TODO.md` names the pass-8 re-review as remaining and counts seven passes — restated for the commit.
- deferred candidates (outside the repair, breaking no rule; one `deferred-work.md` entry): other inherited git variables and user git configuration are not isolated in harness children; a wrapper-script `dotnet` where `/bin` holds git; no upper-case case for the non-scanned trigger extensions; no staged-source refusal through a real `git commit`; the gitlink refusal omits git's own text; the registration filter in `docs/delivery-workflow.md` selects 767 where main's practice selects 798; the role card's handoff list lacks the harness item; the Code Map names the policy edits by old line numbers.
- stagnation check: four consecutive passes found only defects in the previous pass's own harness additions; this patch removes code and adds none, and its re-review is limited to the layers its subject concerns (Verification Gap and both auditors).

### 2026-11-22 — Review pass 9 (sixth pass after entry 3; Verification Gap and both auditors)
- verdicts: 0 in-scope findings
- layers: Verification Gap `aab5a56c0c5c34dc6`, Forward-Only `a3b7d9f5ba42bd12d`, Element-Free Engine `a0baaedfc19df0593`.
- The acceptance auditor's four record items are applied here: this entry and the Final diff scope line, the frontmatter status and the Auto Run Result, `TODO.md`, and the run log's fetch result with entry E23.
- deferred candidates (added to the pass-8 entry in `deferred-work.md` and to its `deferred:` item): the cleanup-failure paths have no case; a passed-count line prints before a non-zero exit on cleanup failure; two shapes for the cleanup result; E22 cites two logs later overwritten.

## Verification

**Staged-source rule.** `dotnet run --file tools/anvil/CheckStagedTests.cs` runs its in-tool cases on every invocation -- expected: all fourteen pass. `dotnet run --file tools/anvil/VerifyPolicy.cs` with no arguments -- expected: every check passes; on the final candidate that is 67: 21 skip-rule checks and 46 staged-source checks (24 written real-git cases; 1 comparison of the set the tool prints with the sixteen extensions the harness holds as its own list, and 16 refusals, one per member; 3 skip scans, the third a staged gitlink refused by name; 2 unusable-git refusals). The harness is run twice, with the pinned toolchain directory first on `PATH` and with a directory holding both `dotnet` and `git` first. Real-tree positive and control, recorded under `.tools/s2-evidence/`: with one source file staged, a temporary untracked C# file and a temporary intent-to-add C# file are both refused by name; with nothing staged the intent-to-add file is still refused and the untracked one is not; with the probes removed the tool passes. `git diff fa86969f1866368ae44da7d20f885453045a599a --stat -- CuriousContraptions.tests/CSharpStaticGateTests.cs .githooks/pre-commit` -- expected: empty. The acceptance-to-test table carries a row for the rule and cites the runs made on this candidate.

Toolchain for every command since run log E22: `PATH=$HOME/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$HOME/.local/share/tim-toolchain/dotnet-10.0.403:$PATH`, `DOTNET_ROOT=$HOME/.local/share/tim-toolchain/dotnet-10.0.403` (SDK 10.0.403 with `wasm-tools`, Node 26.12.1; the earlier installs under `/private/tmp` were cleared by the system's temporary-file cleanup). Compiler logs: `$HOME/.local/share/tim-toolchain/logs/s2-13`. Executor: the implementation owner runs all of them, including the one full local round; raw output goes under `.tools/s2-evidence/` and each result is one line in the run log.

**Order.** Run the probe step first, then recapture the logs, so the gate, registration check and full round all use the final logs.

**Baseline total.** The comparison base is published main `fa86969f1866368ae44da7d20f885453045a599a` since the second merge (run log E21); `baseline_revision` stays the original `7c019d64`. Main's test project file is the original explicit list plus one line, `BaseballTests.cs`, and main's directory holds 237 C# files. The comparison is: the `Csc` source list and the `--list-tests` names from a build of main's explicit-list project file on the merged sources, against the candidate's; plus the round total against main's recorded full round, 7294. The listing counts 7090 names where the round reports 7294 results, on both sides. No second full round is run on main.

**Commands:**
- Source-list comparison (M1), authoritative, as run in E21 (and before it in E17 and E6): after the fast-forward the working tree holds main's explicit-list project file; it is built on the merged sources with `solution.binlog` and `playtest.binlog`, then the candidate's rule is written over it (and compared byte-for-byte with the saved candidate file) and built the same way; each log is replayed to text, and the `.cs` arguments of the test project's `Csc` command line are compared as repository-relative paths -- expected: equal sets in both directions on the full list, and 239 repository sources after filtering to in-repository, non-`obj` paths. `dotnet msbuild -getItem:Compile` is only a pre-check (it also returns package compile-in items).
- The five compiler-log builds from `docs/delivery-workflow.md#local-test-authority`, then `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: builds succeed with zero warnings; gate reports zero violations with the coupling summary and both canary PASS lines.
- New-package registration check from `docs/delivery-workflow.md#new-package-registration-check` -- expected: all pass, `Skipped: 0`.
- M2 and M3 probes in one build: one temporary top-level file with one passing fact, and one temporary file in a new non-ignored subdirectory `CuriousContraptions.tests/ProbeOutsideRule/`. Recapture the solution and playtest logs once, run the fact and `CSharpStaticGateTests` -- expected: the fact runs, and `EveryRepositoryCSharpFileIsCompiledByAProject` fails naming only the subdirectory file. These are the positive and control cases of the new build rule; no product or test source is mutated. Delete both probes and recapture the logs before the remaining checks.
- Before the full round, `git status --short` shows no untracked `CuriousContraptions.tests/*.cs`.
- `git add -N` on new files, then `git diff --check fa86969f1866368ae44da7d20f885453045a599a` (published main, the comparison base) -- expected: clean.
- One full local round under `caffeinate -i` -- expected: zero failed, `Skipped: 0`, total recorded.

**Acceptance-to-test table**

| Row | Proof | Control / boundary | Executed run |
|---|---|---|---|
| M1 | Source-list comparison of the test project's `Csc` arguments in `solution.binlog` and `playtest.binlog`: main's explicit list (`fa86969f`, which adds `BaseballTests.cs`) against the candidate's rule, both built on the merged sources; made before against `8d1d68aa` (run log E17) and the original baseline (run log E6) | Set equality, both directions, on the full list (267 arguments) and on the 239 repository sources (237 local + 2 linked); the candidate's 237 equal the directory listing; main's new explicit line is dropped | Pass, run log E21 |
| M2 | Top-level probe `S2ProbeTopLevelTests.ATopLevelFileIsCompiledWithoutAProjectEdit` compiles and passes with no project edit | The gate failure does not name it | Pass, run log E5 |
| M3 | Subdirectory probe `ProbeOutsideRule/S2ProbeOutsideRuleTests.cs` is refused by `CSharpStaticGateTests.EveryRepositoryCSharpFileIsCompiledByAProject`, and its fact is not discovered | Same build as M2; only location differs; it is the only file the failure names | Pass (expected failure observed), run log E5 |
| AC full round | Full local round on the final candidate (twice merged, with the pass-8 patches): total 7294, equal to main's recorded full round at `fa86969f`; the round before the pass-8 patches was also 7294 (run log E21) | Zero failed, `Skipped: 0` in all five projects; 7090 listed test names identical to those of main's explicit-list build (run log E21; no test source changed since) | Pass, run log E22 |
| AC scope | `git diff --stat fa86969f` | Eleven owned-scope paths; no test, engine or element source; `.githooks/pre-commit` and `CSharpStaticGateTests.cs` equal main | Pass, run log E22 |
| Maintainability gate | `VerifyPolicy.cs --maintainability` on the final candidate, after its five capture builds; it scores both changed tools | Both canary PASS lines and the coupling summary present | Pass, 0 violations, run log E22 |
| Registration check | The filter in `docs/delivery-workflow.md#new-package-registration-check` on the E22 logs: 767. Main's recorded 798 used that filter with `CurrentCapabilityInputsTests` and `WorkshopIntegrationStartupTests` appended, which main did not add to the document; run the same way here: 798 | `Skipped: 0` in all five projects in both runs | Pass, 767/0/0 and 798/0/0, run log E22 |
| Staged-source rule | Fourteen in-tool cases on every invocation, with literal expected refusals and scan paths; five are one failing query each, naming the query and git's error. Harness, all against real git, every child with the caller's git variables cleared and `LC_ALL=C`: 24 written cases (18 refusals listing exactly the expected files between `Not staged:` and `End of the list.`; one real git failure requiring the failing query and git's text "index file smaller than expected"); the set the tool prints equal to the harness's own list of sixteen, then 16 refusals, one per member, the bare extension being the staged file's whole name; a skip in a `.CS` file and in a regular file that replaced a symbolic link, both blocked with `TEST-001`; a staged gitlink at a scanned path refused by name with no unhandled exception; in a fresh directory with `GIT_CEILING_DIRECTORIES` set, and with a harness-made `PATH` holding only a link to `dotnet`, a refusal naming `git rev-parse --show-toplevel` and the error. Real tree: with one source staged an untracked and an intent-to-add probe are both refused by name | Five harness passes whose output carries the self-check line: ignored directory, fully staged new file, staged non-ASCII name, staged source deletion alone, untracked C# file when only documents (`.md`, `.yaml`, `.toml`) are staged; harness 67 of 67 in both `PATH` orders; `--bmad-check --self-test` 37 of 37. Real tree: with nothing staged only the intent-to-add probe is refused; with the probes removed and one source staged the tool passes | Pass, run log E22 |

**Executor and reuse.** Implementation owner `a5f2fbba28f1455a5` ran every command; Debug, `net10.0`, the five logs of run log E22 (final candidate, in `$HOME/.local/share/tim-toolchain/logs/s2-13`), which replace all earlier ones. Any change to a C# source, project, props or targets file, or to the SDK, invalidates the logs, the gate run and the round. All probes are deleted. The index holds only intent-to-add entries for this contract and its run log; the real-tree runs staged `tools/anvil/CheckStagedTests.cs` and unstaged it again. Whoever stages the candidate replaces the intent-to-add entries with `git add`.

**Final diff scope (pass 9):** Pass — eleven owned paths against published main `fa86969f`, every hunk inspected by the Element-Free Engine auditor `a0baaedfc19df0593`; nothing removed; no justified simplification; maintainability gate passing on this exact source state (`maintainability-gate-13.log`).

## Auto Run Result

**Summary.** The test project compiles every C# file directly in its directory through one rule, `<Compile Include="*.cs" />`, in place of 237 hand-listed files; the two linked sources stay explicit. Removing the list also removed an unstated protection (an unstaged new test file used to break the hook's snapshot build), so the pre-commit policy tool gained a staged-source rule: an intent-to-add C# file is refused on every commit, and an untracked or ignored-by-name C# file is refused when the commit stages source. The candidate includes published main `fa86969f` after two fast-forward merges.

**Files changed (eleven, against main `fa86969f`).**
- `CuriousContraptions.tests/CuriousContraptions.tests.csproj` — the one membership rule.
- `tools/anvil/CheckStagedTests.cs` — the staged-source rule, its fourteen self-check cases, named refusals for failed git queries and unreadable staged content, case-insensitive extensions.
- `tools/anvil/VerifyPolicy.cs` — the no-argument harness only: the tool runs from outside the disposable repository, 46 staged-source checks against real git, isolated child environment, safe cleanup.
- `tools/anvil/README.md` — the rule, the hook's real order, the cheap pre-commit check, the verified counts.
- `docs/delivery-workflow.md` — the test project's rule, the staging rule and its residual, the harness requirement.
- `_bmad/custom/bmad-loop-protocol.md` — handoff item 2 (policy-tool harness); the `Compile` list no longer named as a shared file.
- `docs/planning/invest/delivery-batches.md` — S2 status; S1 marked published.
- `_bmad-output/implementation-artifacts/deferred-work.md` — S2 delivered; the residual, deferred review rows and cause-unknown items.
- `TODO.md` — the current brief.
- This contract and its run log.

**Review findings by pass** (nine review passes; Review Triage Log; rows of passes 1 to 5 in run log R1 to R5).
- Pass 1: 23 — 17 patched, 6 rejected (evidence outside the blind diff; descriptive, or repaired under BH1).
- Pass 2: 21 — 18 repaired (17 through the bad_spec amendment, entry 2), 3 rejected (not separate defects).
- Pass 3: 25 — 16 repaired (entry 3), 9 rejected (carried or answered by executed evidence).
- Pass 4: 33 — 20 patched, 13 rejected (carried from earlier passes, or the rule as intended).
- Pass 5: 19 — 18 patched, 1 rejected (carried).
- Pass 6: 24 — 18 patched, 4 deferred under Re-review scope, 2 rejected (carried; owner decision on the wider trigger).
- Pass 7: 13 — 13 patched; five deferred candidates recorded as one entry.
- Pass 8: 4 — 4 patched; eight deferred candidates recorded as one entry.
- Pass 9: 0 in-scope findings; four deferred candidates added to the pass-8 entry. `followup_review_recommended` is false because the last three passes patched no finding above low and pass 9 found none.

**Verification on the final candidate (run log E22).** Five compiler-log builds, 0 warnings; maintainability gate 0 violations with both canaries; registration check 767/0/0 (documented filter) and 798/0/0 (main's practice); the tool's 14 self-check cases; the no-argument harness 67/0/0 in both `PATH` orders; `--bmad-check --self-test` 37/0/0 and `--route Auto` 0 structural findings; real-tree positive and control; `git diff --check` clean; the hook and `CSharpStaticGateTests.cs` equal main; full local round 7294/0/0, equal to main's recorded total. Source lists and 7090 listed test names equal those of main's explicit list (run log E21).

**Residual risks.** The whole pre-commit hook has not run on this candidate; its first run is the candidate commit. A wholly untracked new C# file in a commit that stages none of the trigger extensions is not caught by the hook, and no standing check replaces the build's refusal of a deleted test file. The deferred rows of passes 6 to 8 and two cause-unknown items are in `deferred-work.md`. Browser, remote build, artifact and deployment qualification are unverified.

## Implementation Notes

Owned-diff self-check (run log E9): the project change is the one rule plus its comment; one in-list comment about natively stepped worker sources went with the list it annotated. `deferred-work.md` line 262 ("None has started") was corrected beside the line-264 pointer, since it became false. No branch, condition, exclusion or second mechanism was added.

Amendment self-check (run log E13): the staged-source rule is one decision function in `tools/anvil/CheckStagedTests.cs`, called once, with no option or bypass; git failure is a returned value, not a caught exception. The pass-1 shell guard and pin are gone and both files equal the baseline. The `VerifyPolicy.cs` change stays inside the no-argument harness and one local helper.

Pass-3 self-check (run log E14): the rule is still one decision function; the untracked and ignored-by-name queries sit behind the single condition "a scanned source is staged", which is the rule, not a mode. The harness's shared helpers moved from top-level local functions into a `Harness` class to clear the maintainability gate; the gate, `--bmad-check` and `--self-test` code is untouched.
