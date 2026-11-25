# Planning audit: `spec-s5-generate-wire-accessors-in-build.md` (S5, fifth audit: task 8 amendment)

Audited 24 November 2026, read-only, planning phase: a bounded re-check of one `bad_spec` amendment. Checkout `.claude/worktrees/slice-s5` (branch `slice-s5`), baseline `3a80c6a8ec230948eeef29594e8b72d9fa3ed5e7`, working tree uncommitted. Policy was read from the same checkout: the Informed auditors card (`_bmad/custom/role-cards.md`), the loop protocol's "Planning and repair transitions", "Reviewer duties" and "Routing by what is wrong", the project engineering contract and both element-free engine guides.

## Scope

In scope:

- Task 8's amended stop condition (Tasks & Acceptance item 8).
- The first Spec Change Log entry ("coordinator amendment 1").

The rest of the contract was not re-audited. Its planning verdict stays the Scoped Pass of `audit-s5-4.md`. The other changes in the working tree's contract diff are status and dispatch records (the baseline, Code Map line numbers, lanes, M-row results and the Auto Run Result), not planning changes. Review tier: engine, unchanged.

## Verdict: Scoped Pass

The amended task 8 still guards the risk named by `audit-s5-4.md` N-1: two evaluations inside one build invocation racing on the generation folder. Excluding NuGet's restore-only evaluation is justified by the binary logs. The intent contract is unchanged, and no acceptance criterion or owner rule is weakened. The text is executable with the existing query output. Two non-blocking items follow; neither changes the planning verdict.

## Checks

| Question | Result | Evidence |
|---|---|---|
| Intent contract unchanged | Holds | The `<intent-contract>` block extracted from the baseline and from the working tree compares identical (`diff` empty). The diff's hunks fall outside it. |
| The guarded risk is still covered | Holds | A race on `$(IntermediateOutputPath)WireAccessors/` needs two configurations that each write there (`GenerateWireAccessors`: `RemoveDir`, `Exec flatc`, `Touch`, vendored project `:98-102`) or compile from it (`CompileWireAccessors`, `:106-109`, adds the folder's `Compile` items). The amended condition fires on exactly that pair: two evaluations with different global properties that each run `GenerateWireAccessors` or a compile target. Within one configuration MSBuild runs each target at most once, so dropping same-globals pairs loses nothing (unchanged from N-1). |
| "A compile target" is executable | Holds | `CompileWireAccessors` has no `Inputs`/`Outputs`, is hooked `BeforeTargets="BeforeCompile"` and has `DependsOnTargets="GenerateWireAccessors"`. It therefore runs in every evaluation that compiles, under any reading of "compile target", and the existing query (`.tools/s5-evidence/task1-build/BinlogQuery/Program.cs`) records its finished or skipped events. |
| The restore-only exclusion is justified | Holds | Independent replay of `binlogs-c1/web.binlog`: the vendored project's restore contexts (7:16 to 7:20, invoked by `CuriousContraptions.csproj` and `CuriousContraptions.Core.csproj`) run only NuGet restore-graph targets (`_GenerateRestoreProjectPathWalk`, `_IsProjectRestoreSupported`, `_GenerateRestoreGraphProjectEntry`, `_GenerateProjectRestoreGraph` and their `_GetRestore*`/`Collect*` dependencies). Their tasks are NuGet item tasks, `ConvertToAbsolutePath`, `RemoveDuplicates`, `ResolveAppHosts`, `ProcessFrameworkReferences` and `Message`: no `Exec`, `RemoveDir`, `Touch`, `Csc`, `Copy` or file write. `GenerateWireAccessors`, `CompileWireAccessors` and `CoreCompile` occur only in context 7:7 (the default-targets build). |
| The restore pair predates S5 | Holds | Independent replay of lane 1's `slice-visible-solid-b/web.binlog`: there are no S5 targets, and the same five restore-graph contexts (8:16 to 8:20) are present. |
| Timing (corroboration only) | Consistent | Generation in c1's web log ran at 20:48:28.308 to 28.376, and the default build ended at 28.668. The restore walk ran at 20:48:33.648 to 33.781. The amended rule does not depend on timing. |
| An `ExcludeRestorePackageImports` evaluation that compiles is not excused | Holds | The exclusion is defined as "restore-graph targets only", and the first sentence already admits any evaluation that runs generation or a compile target. So no loophole opens. |
| Other constraints and owner rules | Hold | The amendment adds no fallback, second path, test, hash, byte comparison or skip. Acceptance criteria AC1 to AC5 and rows M1 to M8 are unchanged by it. Element-free engine: not engaged (build configuration only). |
| Change Log entry accuracy | Holds | It cites web evaluations 27 and 38 (`task8.log`) and 23 and 39 (`task8-pre-s5-web.log`), matching the logs. It records the trigger, what is kept and this re-check, and is routed `bad_spec` outside the intent contract per the loop protocol. |

## Non-blocking findings

- **N-1. The query's summary line still computes the pre-amendment rule** (test class, patch).
  - `BinlogQuery` prints `STOP CONDITION: MET` whenever two evaluations with different globals share an `IntermediateOutputPath`. It ignores which targets each evaluation ran, so it will print `MET` on every web capture.
  - The Verification section invalidates capture set c1 on any test or build-configuration change, so task 8 will be re-recorded on fresh logs after resume.
  - When it is re-recorded, decide the amended condition from the per-context target-attribution lines, not from that summary line, or update the query's verdict to the amended rule. Either way, record which was done in the run log.
- **N-2. Status lines are stale after the amendment** (stale status lines, patch; the coordinator's job when recording this verdict).
  - The Code Map reads "Status: ready-for-dev" while the frontmatter reads `draft`.
  - The "Planning verdict" paragraph says no further re-audit is needed and does not cite this re-check.
  - The Auto Run Result still reads blocked although its retry condition is met (run log E5).

## Decisions that need the owner

None.

## Evidence inspected

- `git diff 3a80c6a8 -- _bmad-output/implementation-artifacts/spec-s5-generate-wire-accessors-in-build.md` and `git show 3a80c6a8:` of the same file (item 8 before and after).
- `CuriousContraptions.FlatBuffers/CuriousContraptions.FlatBuffers.csproj:50-109` (working tree).
- `~/.local/share/tim-toolchain/logs/slice-s5/task8.log` and `task8-pre-s5-web.log`.
- The `BinlogQuery` program source.
- Diagnostic replays (`dotnet msbuild <binlog> -v:diag`, pinned SDK 10.0.403, console output only) of `binlogs-c1/web.binlog` and `~/.local/share/tim-toolchain/logs/slice-visible-solid-b/web.binlog`.
- Run log entries E3, E4 and E5, treated as claims; they match the replays.
- `audit-s5-4.md` N-1.

## Evidence limits

- `solution.binlog` and `playtest.binlog` were not replayed independently. Their single evaluation of the vendored project rests on `task8.log`.
- The NuGet `Restore` task's own writes under `obj/` (for example `project.assets.json`) are outside the guarded generation folder and were not assessed.
- Nothing was built or run. Capture set c1 will be superseded after resume; this verdict covers the amended rule, not those future captures.
