# Planning audit: `spec-s5b-generate-typescript-wire-in-build.md` (S5b), eighth pass (bounded re-check)

Audited 25 November 2026, planning phase, read-only. Checkout `.claude/worktrees/slice-s5b` (branch `slice-s5b`) at published main `77e2b6aa`, with uncommitted planning edits: the contract, its run log (new), S5's run log (E32), `TODO.md` and `audit-s5b-7.md`. Scope: the seventh audit's B1, B2, R1 to R10 and stale-citation table, as applied by the planner (`git diff 77e2b6aa -- <contract>`, run log E2, decision D5 Option A), and whatever those edits touch. Parts of the contract that held at audit 7 and were not edited were not re-audited. Nothing was built or tested. Read: the contract, `audit-s5b-7.md`, `audit-s5b-5.md` (B1), the run log, S5's run log E3 to E5 and E32, `CuriousContraptions.FlatBuffers.csproj`, `WorkshopWireBuildTests.cs`, `CSharpStaticGateTests.cs:403-427`, `docs/delivery-workflow.md:40-72`, `schemas/README.md:55-78`, the `Microsoft.Build.Framework` 17.5.0 reference documentation. The contract is 4,652 words (`wc -w`; budget 6,000).

## Verdict: Scoped Pass

B1 and B2 are resolved: the ordered invocation is concrete, computable in MSBuild without a script, keeps S5's `ADeletedSchemaFileLeavesNoGeneratedSource` green, and T10's expectation is confirmed with the pinned compiler; the TypeScript target has S5's dependency and Inputs, no `FileWrites` for `tools/e2e/.wire/`, and T8 counts the one compiler `Exec`, which runs exactly once in each `--no-incremental` capture build. R1 to R10 and every stale citation are applied. The intent-contract edits change mechanism and evidence only. Four required text fixes below (R11 to R14) are wording and recorded evidence; none changes the verdict or needs another re-check. The contract is ready-for-dev on `77e2b6aa` once they are applied.

## Seventh-audit findings: status

| Seventh-audit finding | Status | Evidence at the current tree |
|---|---|---|
| B1 fixed root-schema invocation | Resolved | Approach `:26`; owned scope `:67` ("one `Exec` of `--ts --gen-all` over every `WireSchema` ... the schemas whose file name appears in another schema's `include` line first and the rest last (computed with property functions over the schema text, no script ...)"); task 1 `:129`; D5 `:120`; matrix T10 `:58`; table row `:187`; `:75` (T10 extends `ADeletedSchemaFileLeavesNoGeneratedSource`). See "B1 re-check" below. |
| B2 `Clean`/`FileWrites` basis, T8 event | Resolved | Always `:31` (S5's `DependsOnTargets="ListWireSchemas"` and Inputs, cited `csproj:127-128`, which hold); `:32` (outside `$(OutDir)` and `$(IntermediateOutputPath)`, no `FileWrites` entry); `:33` (Outputs the entry module; T8 counts the compiler `Exec`); `:56`; `:67` ("No `FileWrites` entry"); `:76`; `:185`. "The pinned digest" removed from the input lists. See "B2 re-check". |
| R1 AC2 arithmetic | Applied | `:137`: 7,363 (S5 run log E32, which records "7363 passed, 0 failed, `Skipped: 0`"), +8, 7,371; one `[Fact]` per new row. |
| R2 provisioning by `CallTarget`, hook, task 2 | Applied | `:67` (`<CallTarget Targets="ProvisionFlatBuffersCompiler" />` first, as `csproj:129`; `BeforeTargets="BeforeCompile"`; comment `csproj:55-56` names both targets); task 2 `:130` "beside S5's `GenerateWireAccessors`". Design Note 2 still says otherwise: R13. |
| R3 problem statement | Applied | `:22` cites `schemas/README.md:55-75` (heading `:55`, procedure through `:75`). |
| R4 README and delivery-batches sentences | Applied | `:80` (`:50-53` clause deleted, `:77-78` kept); `:82` (`delivery-batches.md:298`). |
| R5 capability probe | Applied | `:104` (`csproj:57`, `:127`, `:40`, M1 and M5, M6 in `CSharpStaticGateTests`); `:106` (observed counts). R11 adds one observed member difference. |
| R6 T9 control and boundary | Applied | `:186` (`.js` with neither file inside the new fact; `./gone.ts`, `TimingDetectorTests.cs:134-136`, second control; sibling form `'../../m/wire/<name>.js'`, which matches the per-type files' `../../curious-contraptions/wire/<name>.js`). |
| R7 T2, T3 controls and boundaries | Applied | `:179`, `:180` (touch in the same copy, S5's M3 pattern; `--no-incremental` boundary through the schema list). |
| R8 one-writer evidence and CI references | Applied | `:33` (S5 E3 to E5: solution 28, playtest 23, web 27, web 38 restore-only, as S5 run log E5 `:77` records; `web.csproj:33` through `CuriousContraptions.csproj:26`, `Simulation.csproj:43`, `web.csproj:51`, `:69`). |
| R9 verification line | Applied | `:161`. |
| R10 Code Map status | Applied | `:64` (baseline `77e2b6aa`, citations re-read by audit 7, audits listed through `audit-s5b-8.md`). |
| Stale-citation table (11 rows) | Applied | README `:22`, `:80`, `:105` (`:64`); `CSharpStaticGateTests.cs:116` `:78` (equals `shasum -a 256` of the vendored project, `23bb9021…`); `:403-427` `:76`, `:185`; `workshop-wire.ts:12740-12746`, `:12916-13062` `:69`; `Selection.cs:192-195`, `SelectionTests.cs:212`, `ImpactRules.cs:127-131` `:84`; `:134-136` `:186`; web references `:33`; 7,363 and 821 `:137`, `:163`; M1 to M5 and M7 `:104`; observed run `:106`. |

## B1 re-check

- **Concrete and correct.** The rule is a two-bucket order: every schema named in another schema's `include` line, then the rest. The last schema's run writes the entry (audit 7), so the order is correct whenever exactly one schema is not included by another; with two such schemas the entry is short and T1 fails naming the missing types (`:67`), which the contract states. Order inside the first bucket does not matter, because a later, not-included schema overwrites the entry. Today: `schemas/workshop_authoring.fbs:1` `include "workshop_wire.fbs";`, so the computed order is wire, authoring, the order task 1 runs (`:129`).
- **Computable without a script.** Target-scoped item batching does it: an item per `WireSchema` whose condition tests the concatenated schema text (`$([System.IO.File]::ReadAllText('%(FullPath)'))`, accumulated in a target property) for `include "%(Filename)%(Extension)"`, then the included items before the rest in one `Exec` argument list. That is property functions and items in the vendored project file: not a TypeScript-side post-processor script and not a second generation path (Never `:41`). Read, not run.
- **Coherent with S5's deleted-file test and T10.** With the TypeScript target on S5's Inputs (`:31`), deleting `schemas/workshop_authoring.fbs` changes `WireSchemas.list` (`csproj:120-122`, `WriteOnlyWhenDifferent`), the target runs, `.wire` is cleared (`:67`), and the one remaining schema, included by none, is compiled alone; `flatc` no longer receives a missing file, so the build succeeds and S5's assertions (`WorkshopWireBuildTests.cs:104-117`) are unaffected.
- **T10's expectation, measured** with the pinned binary (digest `f004709c0334edb3…`, equal to `csproj:31`) into `/private/tmp/claude-501/s5b-audit8-61340/`, outside the checkout:

| Invocation | Files | Entry exports | Entry names |
|---|---|---|---|
| `--ts --gen-all workshop_wire.fbs workshop_authoring.fbs` (computed order) | 137 | 136 | equal to the 136 declarations of both schemas |
| `--ts --gen-all workshop_authoring.fbs workshop_wire.fbs` | 137 | 127 | short |
| `--ts --gen-all workshop_wire.fbs` (T10's remaining schema) | 128 | 127 | equal to the 127 `enum`/`struct`/`table` names of `workshop_wire.fbs`; no `SavedConstruction` export or `saved-construction.ts` |
| `--ts workshop_wire.fbs workshop_authoring.fbs` (no `--gen-all`) | 137 | 136 | equal to both schemas' names |
| `--ts workshop_authoring.fbs workshop_wire.fbs` (no `--gen-all`) | – | 127 | short |

  `SavedConstruction` is declared only in `workshop_authoring.fbs:88`, so T10's control and its "exactly the remaining schema's types, derived from its text" (`:58`) are correct, and its boundary ("the remaining schema includes nothing", `:187`) holds.
- **Observed member difference (R11).** Under the computed order with `--gen-all`, the authoring run rewrites every reachable type, so `wire/workshop-response.ts` lacks the two root-type helpers of the wire schema's `root_type WorkshopResponse` (`schemas/workshop_wire.fbs:1122`): `finishWorkshopResponseBuffer` and `finishSizePrefixedWorkshopResponseBuffer`. Both are in today's committed generated part (`tools/e2e/workshop-wire.ts:1950`, `:1954`), because the README procedure runs plain `--ts` (`schemas/README.md:64`). Every other per-type file, including `saved-construction.ts` with its two `finish…Buffer` helpers and `WorkshopResponse.getRootAsWorkshopResponse`, has the same members in both orders. No current source uses either missing helper: `rg` over the tree outside `_bmad-output/` finds only their definitions; the hand-written helpers use `getRootAsWorkshopResponse` (`:12752`) and the `start`/`add`/`end` builders (`:12919-13035`); the one other hit is a historical evidence copy (`_bmad-output/implementation-artifacts/evidence-9r-0c/...`). No contract statement promises member parity (T1 and the probe speak of export names), no gameplay or wire-byte behaviour changes, and no Never item is breached, so this is recorded, not blocking.

## B2 re-check

- **Inputs, dependency, Outputs.** `:31` and `:67` give exactly S5's `DependsOnTargets="ListWireSchemas"` and `Inputs="@(WireSchema);$(IntermediateOutputPath)WireSchemas.list;$(MSBuildProjectFullPath)"`, which `csproj:127-128` hold; Outputs the entry module (`:33`, `:67`).
- **No `FileWrites` for `tools/e2e/.wire/`.** `:32` and `:67` say so, with the correct `FindUnderPath` basis.
- **Exactly once per log in the three captures.** The capture commands (`docs/delivery-workflow.md:46`, `:47`, `:49`) are `--no-incremental`, that is `Rebuild` (`Clean` then `Build`; the web project's `Clean` cleans referenced projects). `Clean` deletes `WireSchemas.list`, which S5 records in `FileWrites` under the intermediate directory (`csproj:147`); `ListWireSchemas` rewrites it, so it is newer than the entry module and the target runs. It runs in one instance per log on S5's evidence (E5: one building evaluation of the project per log). The only `Exec` the owned scope places in the target is the compiler run (`:67`: `CallTarget`, guard, clearing, one `Exec`, replacements by property function and `WriteLinesToFile`); the provisioning `chmod` and `mv` `Exec`s (`csproj:103`, `:109`) run inside `ProvisionFlatBuffersCompiler`, a separate target reached by `CallTarget`, and do not run on a warm cache. A target skipped as up to date starts no task, so T8's "never zero" now discriminates. Derived from the targets and S5's logs; not exercised (R12 makes the counted event and "only `Exec`" explicit).

## Contract consistency after the edits

- **Intent contract.** `:22` (citation), `:26` (invocation order), `:31-33` (Inputs, cleaning basis, T8's counted event), T8 `:56` and T10 `:58` (a test row protecting S5's accepted behaviour) change mechanism and evidence; the outcome (build-generated TypeScript, pinned runtime, no hand copy) is unchanged, as the Spec Change Log `:142` states.
- **Elsewhere.** Approach, Always, Never, matrix, Code Map, tasks 1 and 2 (`:129`, `:130` list T10), D5, Concurrency, Verification (`:164` filter includes `WorkshopWireBuildTests`), ACs and the table agree, except Design Note 2 (R13) and the registration parenthetical (R14).
- **AC2.** New facts T1, T5, T7, T8 (`WorkshopWireTypeScriptTests`), T2, T3, T4 (`WorkshopWireBuildTests`, names distinct from S5's facts at `:40-167`) and T9 (`TimingDetectorTests`, in `CuriousContraptions.slnx:90`, so in the round): 8. T6 and T10 extend S5 facts. 7,363 + 8 = 7,371 holds.
- **Budget, tier, decisions.** 4,652 words (≤ 6,000). Engine tier (`:64`). D5 is a coordinator mechanism decision; no owner decision is open.

## Blocking findings

None.

## Required text fixes (not verdict-changing; no re-check needed)

- **R11. Record the member difference.** Probe `:106` and task 1 `:129`: add "With `--gen-all` in the computed order, the authoring run rewrites `wire/workshop-response.ts` without the wire schema's root-type helpers `finishWorkshopResponseBuffer` and `finishSizePrefixedWorkshopResponseBuffer` (committed today at `tools/e2e/workshop-wire.ts:1950`, `:1954`); every other generated member is unchanged, and no current source uses them (`rg` outside `_bmad-output/`). Task 1 records the member lists of `workshop-response.ts` and `saved-construction.ts`." Planner's alternative, not the default: plain `--ts` in the same computed order (measured: 137 files, entry names equal to both schemas, helpers of both roots kept, reversed order 127, so D5's ordering still applies); that is a flag change inside `<intent-contract>` (`:26`) and would need its own bounded check.
- **R12. T8's counted event.** `:76` and `:185` ("`TaskStartedEventArgs` with that target name and task name `Exec`"): `TaskStartedEventArgs` carries `TaskName` and `ProjectFile` but, in the 17.5.0 reference read here, no target name. Replace with: "`Exec` task starts whose `BuildEventContext` (`NodeId`, `ProjectContextId`, `TargetId`) matches a `TargetStartedEventArgs` with `TargetName` `GenerateWireTypeScript` and `ProjectFile` the FlatBuffers project file, keyed as `ReplayCscRuns` keys its events (`CSharpStaticGateTests.cs:403-427`)". In `:67`, say "the target's only `Exec`", so T8's count and the owned scope agree.
- **R13. Design Note 2.** `:149` "S5's one generation target, one more compiler output" contradicts the separate target `GenerateWireTypeScript` (`:67`, `:130`). Replace with "S5's pinned compiler, provisioning and inputs, reused by one more generation target".
- **R14. Registration expectation.** `:163` "unless the slice adds a class to the filter (a test file is added)" reads as if the new test file changes the filter. Replace with "821: the filter (`docs/delivery-workflow.md:66-72`) selects no class this slice adds or changes beyond the `CSharpStaticGateTests` digest edit, which adds no test".

## Decisions that need the owner

None.

## Evidence limits

- The pinned `flatc` 25.2.10 was run (digest checked against the pin) into `/private/tmp/claude-501/s5b-audit8-61340/`, outside the checkout; generated output was compared by export names and member names, not by bytes. Task 1 remains the implementer's gate.
- Audit 5's B1 premise (from the compiler source, not run) that plain `--ts` on both schemas writes an entry with only the authoring types is contradicted here: in the computed order plain `--ts` writes an entry with all 136 names. This does not affect D5, which is needed under either flag (the reversed order gives 127 under both).
- The MSBuild ordering computation was derived by reading, not built. The `--no-incremental` rerun and the single `Exec` per log were derived from the SDK targets read at audit 7, `csproj:120-147` and S5's E3 to E5, not exercised.
- `TaskStartedEventArgs` members were read from the `Microsoft.Build.Framework` 17.5.0 reference documentation, not the SDK 10.0.403 assembly the tests compile against; R12's correlation works on either.
- No build or test was run; planned test names are not executed proof.
