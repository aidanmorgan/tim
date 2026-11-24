# Planning audit 2: `spec-policy-tool-fixes.md` (policy-tool and harness fixes, lane 4)

Independent planning auditor (context-free), 24 November 2026, against `0959e883`. No file edited.

**Verdict: Incomplete**

One new blocking finding (finding 1). The open owner decision O1 would not have blocked a Scoped Pass by itself: the contract says dispatch waits for it. The blocking defect is in text added to answer round-1 finding 4. Nothing breaks the forward-only rules or the element-free engine rules.

## Round-1 findings

| # | Subject | Status |
|---|---|---|
| 1 | P5 gitlink check passes on the unchanged tool | Resolved: git's line must appear inside the refusal line and exactly once |
| 2 | Allow-list widening is the owner's decision | Resolved: O1, default yes, dispatch waits |
| 3 | AC2(c) wrapper cannot fail on the old lookup | Resolved: wrapper calls `env` by bare name; control recorded |
| 4 | `DOTNET_ROOT` citation, requirement and refusal checks | Text resolved; the checks it added (AC2 e, f, g) are new finding 1 |
| 5 | No step reads `TODO.md` | Resolved: path-based allowance; AC8 consistency check |
| 6 | P9 does not prove its row | Resolved |
| 7 | P1 cannot tell D1 from a hand list | Resolved |
| 8 | AC5 measures itself | Resolved |
| 9 | AC7 pathspec | Resolved |
| 10 | P6 evidence printed before the scan | Resolved |
| 11 | Harness reports its own total | Resolved |
| 12 | Running beside other work | Resolved; D7 gives a recommended ruling |
| 13 | Capability probe | Resolved |
| 14 | Carry-forward stray-file item | Resolved |
| 15 | `SourcePaths` summary | Resolved |
| 16 | Link needs an anchor | Resolved |
| 17 | Anchors | Resolved except README `:146–149` (finding 3) |
| 18 | Exporting `GIT_DIR` reaches the outer build | Resolved: AC2(d) removed |

## New findings

1. **Blocking. AC2 (e), (f), (g), D2's refusal clauses, P2's error column and the Boundaries "Always" line never reach the harness's refusal code.** `dotnet run --file` starts the compiled apphost (run cache: `"Command":".../bin/debug/VerifyPolicy"`, `DefaultAppHostRuntimeIdentifier` `osx-arm64`; a Mach-O arm64 executable). The apphost finds hostfxr through the caller's `DOTNET_ROOT`, so with an empty directory (g) or one holding only the wrapper (f) the run fails inside the apphost, whose own message names `DOTNET_ROOT`: the ACs are met vacuously, the same way on the drafting revision. (e) cannot be met: the SDK supplies the launched app a `DOTNET_ROOT` when the caller has none (`GetTargetCommandForProject`/`SetRootVariableName` in SDK 10.0.403 `dotnet.dll`; not probed directly). Whenever harness code runs, its `DOTNET_ROOT` names the root its apphost resolved. Preferred fix: delete AC2 (e)–(g) and the refusal clauses in D2, P2 and Boundaries; keep D2's link to `$DOTNET_ROOT/dotnet` with the reason "`dotnet run` always supplies `DOTNET_ROOT` to the harness, naming the root its apphost resolved"; drop the stated requirement from D5, task 9, task 10 and "What this removes"; P2's controls become AC2 (a), (b) and the recorded drafting-revision control. Alternative: run (e)–(g) through the muxer against the built dll and require a harness-owned message prefix.

2. **Non-blocking.** AC2 lettering jumps from (c) to (e); Verification says "(a)–(g)". Reletter.

3. **Non-blocking.** `tools/anvil/README.md:146–149` (children have three variables cleared; the two-order run) goes stale under D1 and is outside the cited `:116–144`. Cite `:116–149`.

4. **Non-blocking.** D3's derivation would throw: `Harness.Execute` defaults to `requireSuccess: true` (`VerifyPolicy.cs:119,137`) and `git show :Module.cs` exits 128; `Result.Output` merges stdout and stderr (`:136`). Use `requireSuccess: false`, require a non-zero exit, and take the first non-empty line of the merged output (stdout is empty for this failure).

5. **Non-blocking.** The lettered-level slice expects its registration check "at the baseline, unchanged" (`spec-tool-lettered-level-ids.md:149`); this slice adds two classes. State that the later publisher re-measures.

6. **Non-blocking.** Boundaries overstate the harness cases: P1 runs git only and P9 runs neither git nor the hook. Reword.

## What was verified (against `0959e883`)

- `VerifyPolicy.cs` anchors `:72–85`, `:81–84`, `:86–88`, `:92–98`, `:99`, `:101–106`, `:108`, `:117–139`, `:127`, `:161–175`, `:190–244`, `:200–201`, `:204`, `:246–251`, `:265–269`, `:291–310`, `:296–298`, `:304–308`, `:312–337`, `:325–327`, `:339–350`, `:344–349`, `:353–361`, `:428–965`, `:966`.
- `CheckStagedTests.cs` anchors `:8`, `:35`, `:47–65`, `:102–132` (14 cases), `:170–172`, `:175–181`, `:181,187`, `:189`, `:205–207`, `:227–265`, `:238`, `:242–248`, `:250`, `:253`, `:274`.
- Document anchors: `delivery-workflow.md:34,58,64,67,70`; `bmad-loop-protocol.md:34,50,54–60,71,73,155,180,206`; `role-cards.md:33–40,64,73,90`; `bmad-build-auto.toml:21`; README `:52,94–102,114,116–149`; ledger OS-04 `:395`; `deferred-work.md:299–330`.
- Counts: 67 = 21 skip-rule checks + 46 staged-source, matching the 67 `PASS` lines in the S2 run log `:371`; +10 arithmetic right; 18 filter classes; 767/798 and the two classes confirmed at S2 run log `:395`; both classes exist.
- Discriminating power: P7 fails if an extension leaves the scanned set; P3 if the build-input set becomes case-sensitive; P4's unborn `HEAD` boundary holds; P8 catches git run from the subdirectory; P6 fails on a pass with no scan; P1's sentinel control and P9 can fail; the AC2(c) control holds on this host (`/bin/env` and `/bin/git` absent).
- The pinned `dotnet` is a native Mach-O binary; Homebrew's is a bash wrapper with absolute paths (the F2 setting).
- AC7 achievable; AC4 options exist; disjointness claims correct; F8 and F12 closures match their owner lines; 4,660 words; engine tier correct; no fallback; no engine, package, wire or presentation code touched.
