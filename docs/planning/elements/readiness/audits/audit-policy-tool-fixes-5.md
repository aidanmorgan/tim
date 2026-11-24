# Planning audit 5: `spec-policy-tool-fixes.md` (policy-tool and harness fixes, lane 4)

Independent planning auditor (context-free), 24 November 2026, against `0959e883` and the worktree (owner decisions 24m and 24n and the coordinator ruling after 24m). No file edited; probes only in the session scratchpad.

**Verdict: Scoped Pass**

No blocking finding. All six round-4 findings are resolved. The text changed since round 4 (D1 and the local git configuration, P1 and its control, P9, AC5, AC6, Design Note 7, the shared-documents paragraph and the B2 bullet) is consistent with 24m, 24n, the coordinator ruling and the code at `0959e883`. A probe shows the D1/P1 design is feasible and catches the leaks it targets. Four non-blocking findings, each a one-line text fix.

## Round-4 findings

| # | Subject | Status |
|---|---|---|
| 1 | Git's default excludes file reached the harness's children | Resolved: local `core.excludesFile` and `core.attributesFile` set to `/dev/null` at `VerifyPolicy.cs:43–45` (owned); P1 seeds `git/ignore` naming `Leaked.cs`, requires `ls-files` to list it, and its control passes the `GIT_CONFIG_COUNT` pair through `environment` |
| 2 | AC5 and AC6 assumed this slice publishes first | Resolved |
| 3 | B2's tier reason | Resolved: cites `bmad-loop-protocol.md:75`; B2 passed planning in `audit-visible-solid-B2-4.md` |
| 4 | `completion-ledger.md` not attributed | Resolved for lane 3; part D is new finding 1 |
| 5 | E5 wrongly listed as untouched | Resolved; `SourceKind` appears only at `CheckStagedTests.cs:8,175–180` |
| 6 | P9 claimed behaviour its check did not execute | Resolved; F10 checked by code order |

## New findings

1. **Non-blocking. The ledger's sharing list names only lane 3.** Part D (lane 1) also edits `completion-ledger.md`, rows ES-37 and OS-05 (`spec-elements-visible-solid-remainder-d.md:89`); no overlap with OS-04. Fix: "shares by row with lane 3 (OS-04 against CA-01 and OS-02) and part D in lane 1 (OS-04 against ES-37 and OS-05)", in D7 and the disjointness paragraph.

2. **Non-blocking. "Running beside other work" leaves out part D, the Baseball repair, S6 and S6b–S6g.** None edits `tools/anvil/`, `_bmad/custom/` or `docs/delivery-workflow.md`; S6f reads `delivery-workflow.md:46–51` only; the Baseball repair cites `TODO.md` as its only engine-tier reason (`spec-repair-baseball-el-239.md:96`), like B2. Fix: add a bullet for them.

3. **Non-blocking. The attributes-file part of the Boundaries promise has no check.** Without the setting, an XDG `git/attributes` seed reaches `git check-attr`; no tool query reads attributes, so tool decisions are not at risk. Fix: inside P1's existing check, the XDG directory also holds `git/attributes` containing `Leaked.cs foo=bar`, the child's `git check-attr -a Leaked.cs` prints nothing, and the control passes `GIT_CONFIG_KEY_0=core.attributesFile`, `GIT_CONFIG_VALUE_0=<the seeded attributes file>` through `environment` and prints `Leaked.cs: foo: bar` (AC1 +10 unchanged); or narrow Boundaries to the excludes file.

4. **Non-blocking. On a merged candidate, AC5 and task 8 can name different SHAs.** Fix task 8: "at the baseline and that SHA, or, when this slice publishes second, at the merged `origin/main` parent and that SHA (equal to the candidate's count, because this slice changes no test project)".

## What was verified

- 24m, 24n and the ruling (worktree `owner-decisions-2026-11-22.md:67–69`): D7, the 24n wording, the B2 bullet and P11 match; no owner decision is open; `delivery-batches.md` has no lane-4 row, so its absence from the Final diff scope is correct.
- P1 probe (git 2.56.1; `--local-env-vars` 15 names): with `GIT_CONFIG_GLOBAL=/dev/null`, `GIT_CONFIG_NOSYSTEM=1`, the XDG seeds and local `excludesFile`/`attributesFile` at `/dev/null`, `--show-origin --get-all core.hooksPath` prints exactly `file:.git/config\t.git-hooks` and `ls-files --others --exclude-standard` lists `Leaked.cs`; the hooks control prints `/nonexistent`; the excludes control omits `Leaked.cs`; unsetting the local `excludesFile` omits `Leaked.cs` and unsetting the local `attributesFile` lets the XDG attributes through. P1 is feasible and would fail on a three-name variable list or a missing local excludes setting.
- D1 and existing cases: the outside-repository case passes `GIT_CEILING_DIRECTORIES` through `environment`, applied after the `GIT_*` removal; `.git/info/exclude` (`:42`) is independent of `core.excludesFile`; `EmptyRepository` keeps `.git`, `.git-hooks` and `tools`.
- Counts: 67 PASS lines today (21 + 46); AC1's +10 is P1, P3, P4, P8, P9 one each plus five for P7; P9's mode-0500 subdirectory makes `Directory.Delete(true)` fail with an exception `Harness.Remove` catches.
- Both added registration classes exist and are not substrings of the 18 existing filter terms.
- Anchors at `0959e883`: `VerifyPolicy.cs:34–36,43–45,72–106,108,117–175,190–361`; `CheckStagedTests.cs:8,170–181,227–265,274`; README `:52,114,116–149`; `delivery-workflow.md:34,58,64,67,70`; protocol `:50–62,71,75,180`; role cards `:33–40,73,90`.
- F1–F19 cover every item at `deferred-work.md:306–328` plus OS-04; F8 and F12 closed with reasons; "Not taken" items match their owners.
- Disjointness: no listed slice has `tools/anvil/`, `_bmad/custom/` or `docs/delivery-workflow.md` in its owned scope; line edits in shared documents do not overlap; lane 3's contract records the same sharing.
- Engine tier correct; every probe entry has a symbol and a test; no fallback, compatibility path, output hash, mutant, browser run or skipped test; no engine, package, wire or presentation code; AC6 holds; 5,040 words.
