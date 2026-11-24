# Planning audit 1: `spec-policy-tool-fixes.md` (policy-tool and harness fixes deferred from S2, lane 4)

Independent planning auditor (context-free), 24 November 2026, on the contract as drafted against `0959e883`.

**Verdict: Incomplete**

Four blocking findings: two planned checks cannot fail on the defects they target (P5 and AC2(c)), one decision belongs to the owner (D6), and one citation is false (D2). Each has a text fix. Nothing breaks the forward-only rules or the element-free engine rules.

## Findings

1. **Blocking. P5 / D3 / task 4: the gitlink check passes on the unchanged tool.** `SkipScan` starts `git show` with error capture off (`CheckStagedTests.cs:238`), so git's stderr goes straight through the hook's stderr, and `Harness.Execute` joins stdout and stderr (`VerifyPolicy.cs:133–136`). Real git 2.56.1 on this gitlink exits 128 and prints `fatal: bad object :Module.cs`, already in the harness output today. Fix: take the first non-empty line of git's own error output for `git show :Module.cs`; require it non-empty, inside the refusal line that starts `CheckStagedTests: the staged content of Module.cs could not be read`, and exactly once in the hook's output.

2. **Blocking. D6 and "No owner decision is open": widening the allow-list is the owner's decision.** Review tiers is an owner decision (`bmad-loop-protocol.md:50`). Adding `TODO.md` removes Blind Hunter, Edge Case Hunter and Intent Alignment from slices that are engine tier today; `bmad-loop-protocol.md:34` sends a gate-waiving decision to the owner. No owner decision on this exists (OS-04 and `TODO.md:7` record only the item). Fix: make it open owner decision O1 with a recommended default (yes); if declined, P11 is closed with no change.

3. **Blocking. AC2(c) and P2: the wrapper case cannot fail on the old lookup.** The planned wrapper (`#!/bin/sh` + `exec "$DOTNET_ROOT/dotnet" "$@"`) uses only absolute paths, so it passes under the old `PathWithoutGit` too. `/bin/git` and `/bin/env` do not exist on this host; `/usr/bin/git` and `/usr/bin/env` do. Fix: the wrapper runs `exec env "$DOTNET_ROOT/dotnet" "$@"` (bare `env`, found only through `PATH`); control: under the drafting revision's `PathWithoutGit` this wrapper makes the no-git case fail (exit 127, no refusal text), recorded once.

4. **Blocking. D2, P2, AC2(e): the `DOTNET_ROOT` citation is false, the requirement is undocumented, and refusal branches are untested.** `DOTNET_ROOT` is not in `docs/delivery-workflow.md#reproducible-local-verification` (only in audit reports and `VerifyPolicy.cs:341`). D2 changes the README "Install and verify" requirements (`tools/anvil/README.md:96–102`), not in owned scope. The `#!` and missing-file refusals have no check. Fix: state the requirement in the single harness rule and the README; add `README.md:96–102` to owned scope; do not edit `delivery-workflow.md:34` (S5's); add AC2(f) (wrapper as `$DOTNET_ROOT/dotnet`) and AC2(g) (empty directory).

5. **Non-blocking. D6 / P11: the content-dependent exception has no step that reads `TODO.md`, and P11 has no check.** Tier confirmation opens hunks only for list-line paths (`bmad-loop-protocol.md:71,206`, `role-cards.md:90`, `bmad-build-auto.toml:21`). Fix: widen those, or narrow D6 so it depends only on the path; replace P11's check with a consistency check that the allow-list is defined only at `bmad-loop-protocol.md:54–60`.

6. **Non-blocking. P9: the planned check does not prove the row's claim.** It does not exercise "attempts every directory" or F10. Fix: two directories, the first undeletable; `RemoveAll(first, second)` returns false, the first exists, the second is gone; mode restored in `finally`; F10 proven by code order in review.

7. **Non-blocking. P1: the check cannot tell D1 from a hand list, and the control is ambiguous.** Also seed `GIT_CONFIG_COUNT`/`KEY_0`/`VALUE_0` and `XDG_CONFIG_HOME` (verified: read unless `GIT_CONFIG_GLOBAL=/dev/null`). Control: only `GIT_CONFIG_GLOBAL=<sentinel file>` passed through `environment` shows `/nonexistent`. State that a case and its control count as one check.

8. **Non-blocking. AC5 measures itself.** Add: each of the two classes, filtered alone, selects more than zero tests, and the total equals the 18-class count plus those two.

9. **Non-blocking. AC7's pathspec is fragile and greps only the secondary sentence.** Grep both sentences over `AGENTS.md _bmad/custom tools/anvil docs/delivery-workflow.md docs/testing-guidance.md`.

10. **Non-blocking. P6's evidence is printed before the scan runs** (`CheckStagedTests.cs:250` before `:253`). Also require the scanner's JSON report (`"summary"`).

11. **Non-blocking. AC1 and task 7: the harness reports its own total.** N equals the number of lines beginning `PASS `; the total is a count of checks that printed `PASS`.

12. **Non-blocking. Running beside other work.** S5b edits `bmad-loop-protocol.md` (Maintainability gate paragraph, `spec-s5b-generate-typescript-wire-in-build.md:78`), not listed. The lettered-level slice cites `CheckStagedTests.cs:35`, which moves. Lane admission is left open while the contract says nothing is open; state a ruling.

13. **Non-blocking. Capability probe.** The allow-list is marked present with no exercising test; it is policy text. `spec-early-level-proof-batch-1.md:61` gives a second engine-tier reason.

14. **Non-blocking. Carry-forward.** The pass-7 stray-file item (`deferred-work.md:318`) is missing from the "Not taken" row.

15. **Non-blocking. D7.** The `SourcePaths` summary at `CheckStagedTests.cs:170–172` ("internal choices remain enum-typed") goes stale.

16. **Non-blocking. D5.** A link cannot target a line; add an anchor before `docs/delivery-workflow.md:58`.

17. **Non-blocking. Anchors.** Real-commit check `:92–98`; empty index `:86–88`; `Harness.Execute` `:117–139`; `Harness.Remove` `:161–175`; cases `:190–244`; `Verify` `:246–251`; `SkipScans` `:291–310`; `UnusableGit` `:312–337`; `PathWithoutGit` `:339–350`; source-commit set `:265–269`; README inventory `:116–144`.

18. **Non-blocking. AC2(d).** Exporting `GIT_DIR=/nonexistent` also reaches the outer `dotnet run --file` build; if the outer build fails, stop and return to planning, or run the outer command under `env -u`.

## Verified as correct

- All other anchors, including `CheckStagedTests.cs:8,47–65,175–181,189,205–207,227–265,238,274,285–296`, `VerifyPolicy.cs:99,101–106,108,127,200–201,296–298,304–308,344–349,353–361,428–965,966`, `delivery-workflow.md:58,64,67,70`, `bmad-loop-protocol.md:54–60,180`, `role-cards.md:33–40`, ledger OS-04 `:395`, `deferred-work.md:299–328`.
- Every deferred item owned by the next changer of `tools/anvil/` is in scope; the F8 and F12 closures match their owner lines.
- Real git 2.56.1: `--local-env-vars` gives 15 names; `GIT_CONFIG_GLOBAL=/dev/null` suppresses `~/.gitconfig` and XDG configuration; P1's expected output is exactly `file:.git/config<TAB>.git-hooks`; gitlink `git show` exits 128; `/bin/git` and `/bin/env` are absent on this host.
- Counts: 67 harness checks (15 + 6 + 46); +10 arithmetic right; 14 self-check cases; 18 filter classes; 767 and 798 in the S2 run log.
- P3, P4 (with the unborn `HEAD` boundary) and P7 each fail under a plausible defect; P8 catches git run from the subdirectory.
- D7 sound; engine tier correct; 4,133 words; policy self-test present; no fallback; no engine or element code touched.
