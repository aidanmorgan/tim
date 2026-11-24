# Planning audit 4: `spec-policy-tool-fixes.md` (policy-tool and harness fixes, lane 4)

Independent planning auditor (context-free), 24 November 2026, against `0959e883` and the worktree (owner decisions 24m and 24n and the coordinator ruling after 24m). No file edited; probes only in the session scratchpad.

**Verdict: Incomplete**

One blocking finding (finding 1): D1 says `GIT_CONFIG_GLOBAL=/dev/null` keeps the caller's user and XDG git configuration from the harness's children, but git's default global excludes file still reaches them, so the Boundaries promise is false, F1 is only partly closed, and no planned check would notice. Everything else is non-blocking. The text added for 24n and the coordinator ruling is consistent with both decisions and the rest of the contract. Nothing breaks the forward-only or element-free engine rules.

## Round-3 findings

| # | Subject | Status |
|---|---|---|
| 1 | D2's premise | Resolved: D2 links `<root>/dotnet` from `RuntimeEnvironment.GetRuntimeDirectory()` and reads no environment variable; P2, the probe wording and "What this removes" match; AC2 gains (d) and (e) |
| 2 | Boundaries wording on the isolated hook | Resolved |
| 3 | D7 and `completion-ledger.md` | Resolved in substance; attribution loose (new finding 4) |

## New findings

1. **Blocking. D1's isolation claim does not hold for git's default excludes file.** Location: Boundaries "Always"; D1; matrix and acceptance-table row P1; tasks 1 and 10. When `core.excludesFile` is unset, git reads `$XDG_CONFIG_HOME/git/ignore`, or `$HOME/.config/git/ignore`, whatever `GIT_CONFIG_GLOBAL` says; D1 keeps `HOME`, and P1 reads only `core.hooksPath`. Probe (git 2.56.1, untracked `Foo.cs` and `Bar.cs`): with `GIT_CONFIG_GLOBAL=/dev/null GIT_CONFIG_NOSYSTEM=1 XDG_CONFIG_HOME=<dir>` and `<dir>/git/ignore` listing `Foo.cs`, `git ls-files --others --exclude-standard` prints only `Bar.cs`; with XDG unset and a fake `HOME` whose `.config/git/ignore` lists `Bar.cs`, it prints only `Foo.cs`. These queries feed the tool's Untracked and Ignored lists (`CheckStagedTests.cs:41,43`), so a caller's global ignore naming `nested/` or `fresh-dir/` would turn refusal cases (for example `VerifyPolicy.cs:198–199`) into passes. This host has `~/.config/git/ignore` with one harmless pattern, so the dependency is latent. Fix: the disposable repository's local configuration also sets `core.excludesFile` and `core.attributesFile` to `/dev/null` (`VerifyPolicy.cs:43–45`; add to owned scope); P1's input adds `git/ignore` naming `Leaked.cs` in the XDG directory; P1's expected output adds that a child's `git ls-files --others --exclude-standard` lists an untracked `Leaked.cs`; P1's control adds the same `ls-files` with `GIT_CONFIG_COUNT=1`, `GIT_CONFIG_KEY_0=core.excludesFile`, `GIT_CONFIG_VALUE_0=<the seeded ignore file>` passed through `environment`, which omits `Leaked.cs`. It stays inside P1's one check (AC1 +10 unchanged). Feasibility probed: with local `core.excludesFile=/dev/null` both seeded files are listed under the XDG seed and the fake `HOME`; the environment-pair control omits the file; `core.hooksPath` still prints exactly `file:.git/config\t.git-hooks`.

2. **Non-blocking. AC5 and AC6 assume this slice publishes first.** On a merged candidate (24m merge rule 3), S3 (+8), S5 (+7) or lane 3 (+11) change both totals. Fix: AC5 re-measured and restated on the merged candidate when this slice publishes second; AC6 the merged main's total on a merged candidate.

3. **Non-blocking. B2's tier reason is wrong.** B2 has not started; what holds it at engine tier is `bmad-loop-protocol.md:75` (a slice never moves from engine to data-only after planning). Fix accordingly; a slice whose planning passes after this slice publishes is tiered under 24n.

4. **Non-blocking. The coordinator ruling does not name `completion-ledger.md`.** It names `_bmad/custom/*.md` and `docs/delivery-workflow.md`; `deferred-work.md` and `TODO.md` come from 24m. Fix: attribute each; `completion-ledger.md` is a planning document, not code under 24m's admission test, shared by row with lane 3.

5. **Non-blocking. E5 is wrongly listed as untouched.** E5's trigger includes refactoring and requires deleted symbols to have no remaining reference; this slice deletes `SourceKind`, the three-variable list, the PATH lookup, the `/bin` branch and the second cleanup shape. Fix: E5 applies, proven by the compiler-proof capture of both file-based apps.

6. **Non-blocking. Matrix row P9 claims behaviour its check does not execute.** Fix: P9's expected output becomes "Removal attempts both, removes the second, and returns false"; keep F10 only in the acceptance table's review note.

## What was verified

- 24m, the coordinator ruling and 24n (worktree `owner-decisions-2026-11-22.md:67–69`): D7 matches the ruling; the 24n wording matches 24n and is path-based, consistent with `bmad-loop-protocol.md:71,206`; `role-cards.md:73,90` and `bmad-build-auto.toml:21` only refer to the allow-list; no owner decision is open.
- Probes: the excludes-file probe above; a file-based app beside the repository's `Directory.Build.props` and `.editorconfig` (warnings as errors, AOT analyzer) builds clean using `RuntimeEnvironment.GetRuntimeDirectory()` three levels up and a `static readonly FrozenSet<string>`, printing the pinned `<root>/dotnet` with and without `DOTNET_ROOT`.
- Feasibility: 67 PASS lines today (21 + 46), so AC1's line-count rule holds; the snapshot line is `Anvil snapshot <index>.ts: <path>` (`CheckStagedTests.cs:237,250`); git queries run at the top level with `:(top)` pathspecs (`:210–218`), so P8 needs no tool change; TEST-001 matches `test.skip(` and snapshots are `.ts`, so P7 is feasible; the checker's string requirements come from the TOML hooks (`VerifyPolicy.cs:700–715`), so AC4 is feasible; both added registration classes exist; AC7's grep hits exactly protocol `:180`, `delivery-workflow.md:58` and README `:114`; AC8's hits refer back to `:54–60`; no test reads the changed documents.
- Anchors checked against `0959e883`: `VerifyPolicy.cs:34–36,43–45,72–106,108,117–175,190–361,966`; `CheckStagedTests.cs:8,170–181,187,205–207,227–265,274`; README `:52,114,116–149`; `delivery-workflow.md:34,58,64,67,70`; protocol `:50,54–62,71,75,180,206`; role cards `:33–40,73,90`; ledger `:395`; `deferred-work.md:299–330`; the three tier citations in other contracts.
- F1–F19 cover every item at `deferred-work.md:306–328` plus OS-04; F8 and F12 closed per their owner lines.
- No sibling contract edits `tools/anvil/*`; shared-document lines do not overlap; lane 3's contract records the same sharing.
- No `sprint-status.yaml` key exists for this slice or its siblings.
- 4,835 words; engine tier correct; every probe entry has a symbol and test; no fallback, compatibility path, output hash, mutant or skipped test; no engine, package, wire or presentation code touched.
