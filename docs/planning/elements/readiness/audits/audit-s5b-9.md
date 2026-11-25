# Planning audit: `spec-s5b-generate-typescript-wire-in-build.md` (S5b), ninth pass (bounded check of the R11 flag change)

Audited 25 November 2026, planning phase, read-only. Checkout `.claude/worktrees/slice-s5b` (branch `slice-s5b`) at published main `77e2b6aa` (baseline), with uncommitted planning edits: the contract, its run log (new), S5's run log, `TODO.md`, `audit-s5b-7.md` and `audit-s5b-8.md`. Scope: the eighth audit's R11 to R14 as applied by the planner, in particular R11 taken as plain `--ts` instead of `--ts --gen-all` in the same computed schema order (decision D5), which changes a flag inside `<intent-contract>` (Approach `:26`). Evidence the planner cites: run log E3. Parts of the contract not touched by these edits were not re-audited. Read: `_bmad/custom/role-cards.md` (Informed auditors), `_bmad/custom/bmad-loop-protocol.md` (Capability probe; Planning and repair transitions), AGENTS.md (forward-only gate; strict TypeScript), the contract (`git diff 77e2b6aa -- <contract>` and the full current text), `audit-s5b-8.md`, run log E3, `CuriousContraptions.FlatBuffers.csproj`, `CSharpStaticGateTests.cs:403-427`, `docs/delivery-workflow.md:64-72`, `tools/e2e/unit/run-reset-snapshot.test.ts`, `tools/e2e/workshop-wire.ts:1-12731`. The contract is 4,813 words (`wc -w`; budget 6,000).

## Verdict: Scoped Pass

Plain `--ts` in the computed order (`schemas/workshop_wire.fbs`, then `schemas/workshop_authoring.fbs`) meets every contract obligation checked, measured with the pinned compiler below; the reversed order still gives a short entry, so D5's ordering remains necessary. No `--gen-all` residue contradicts the switch. R12, R13 and R14 are applied. The intent-contract change is mechanism only; the outcome is unchanged and no owner decision opens. One required text fix (R15, T8's keying clause) does not change the verdict and needs no re-check. When recording this pass, the coordinator also adds `audit-s5b-9.md` to the Code Map status line (`:64`, which lists audits through `audit-s5b-8.md`). The contract is ready-for-dev on `77e2b6aa` once R15 is applied.

## 1. Plain `--ts` against the contract obligations (measured)

Compiler: `$HOME/Library/Application Support/CuriousContraptions/flatc/25.2.10/Mac.flatc.binary/flatc`, `flatc version 25.2.10`, SHA-256 `f004709c0334edb3cdc0430395b9de1dc071f3618fc4ba43674c3c61201037ac`, equal to the macOS `FlatcBinarySha256` pin (`CuriousContraptions.FlatBuffers.csproj:31`). Output written to `/private/tmp/claude-501/s5b-audit9-69112/` (subfolders `fwd`, `rev`, `wireonly`, `genall`); comparison lists in `/private/tmp/claude-501/s5b-audit9-69112-cmp/`. Nothing was written in the checkout.

| Invocation | Files | Entry exports | Entry names |
|---|---|---|---|
| `--ts schemas/workshop_wire.fbs schemas/workshop_authoring.fbs` (computed order) | 137 | 136 | equal to the 136 names from `grep -hE '^(enum\|struct\|table) ' schemas/*.fbs` |
| `--ts schemas/workshop_authoring.fbs schemas/workshop_wire.fbs` (reversed) | 137 | 127 | short: missing `AssistanceKnotRecord`, `CadenceSettingsRecord`, `ConnectionRecord`, `ConstructionRecord`, `InstanceRecord`, `IntegrityGoalRecord`, `ParameterRecord`, `PuzzleRecord`, `SavedConstruction` |
| `--ts schemas/workshop_wire.fbs` (T10's remaining schema) | 128 | 127 | equal to the 127 names of `workshop_wire.fbs`; no `SavedConstruction` export, no `saved-construction.ts` |
| `--ts --gen-all` in the computed order (comparison only) | 137 | 136 | equal to both schemas |

Obligation by obligation, for the computed order:

- **Always `:34`.** The entry is `curious-contraptions/wire.ts` with 136 `export { X } from './wire/<kebab>.js'` lines and nothing else exported; per-type files are `curious-contraptions/wire/<kebab>.ts`. `any` occurs only as 10 `optionalEncoding?:any` (in 6 files) and the eslint header `..., @typescript-eslint/no-explicit-any, ...`, identical in all 137 files. All 6 files with `optionalEncoding?:any` have `import * as flatbuffers from 'flatbuffers';`, so `flatbuffers.Encoding` resolves there. Applying the two literal replacements of owned scope `:67` to stdout (not to a file) leaves zero whole-word `any` in all 137 files.
- **T1 (`:49`).** All 136 per-type files declare exactly one `export class <Name> {` or `export enum <Name> {`; the declared names equal the 136 schema names; no other `export` form (function, union) occurs; every file, entry included, has balanced braces.
- **T4 (`:52`).** Unaffected by the flag: the target clears `tools/e2e/.wire/` before the one `Exec` (`:67`), so a removed table cannot survive; derived by reading.
- **T10 (`:58`).** The remaining-schema run gives exactly the wire schema's 127 types and no `SavedConstruction` (row above).
- **T9 (`:57`, `:187`).** The entry uses `'./wire/<name>.js'` (136); per-type files use `'../../curious-contraptions/wire/<name>.js'` (164) and `'flatbuffers'` (128). From inside `curious-contraptions/wire/` the `../../curious-contraptions/wire/` form resolves to a sibling `.js` whose file on disk is `.ts`, which is the boundary form `'../../m/wire/<name>.js'` T9 plans.
- **Run-reset-snapshot lookup (`:73`).** Because each per-type file holds exactly one declaration, "the file whose text declares `export class <Name> {`" is unique, and `generatedOffsets` (`run-reset-snapshot.test.ts:121-134`) slicing to the next `\nexport class ` reads to end of file. For `InstanceRecord`, `Vec3f` and `WorkshopResponse` the class text in the per-type file differs from the committed part only by the `optionalEncoding` annotation and the header line that the normalisation addresses, so the accessor regex sees the same lines. The `generated` namespace (`:12`, `:170`) reads the entry, which re-exports every struct class.
- **Member names versus the committed generated part (`tools/e2e/workshop-wire.ts:1-12731`).** With one extractor applied to both (class and enum members at column 0 or 2), plain `--ts` gives a sorted `Class.member` list identical to the committed part (2,964 lines each); `--gen-all` lacks exactly `WorkshopResponse.finishWorkshopResponseBuffer` and `WorkshopResponse.finishSizePrefixedWorkshopResponseBuffer`. Column-0 method lines: 2,302 committed, 2,302 plain `--ts`, 2,300 `--gen-all`. Plain `--ts` keeps both roots' helpers (`workshop-response.ts:727`, `:731`; `saved-construction.ts:59`, `:63`), which task 1 (`:129`) asks the implementer to record.
- **D5's ordering is still needed:** reversed order gives 127 under plain `--ts`, as under `--gen-all`.

## 2. Consistency after the edits

- **`--gen-all` residue.** Two occurrences, neither contradicting: probe `:106` attributes `--gen-all` to audit 7's measurement and audit 8's R11, then states the target uses plain `--ts`; D5 `:120` uses it as the reason ("as `--gen-all` loses them"). Approach `:26`, owned scope `:67` ("one `Exec`, the target's only one, of `--ts` over every `WireSchema`") and task 1 `:129` (`flatc --ts schemas/workshop_wire.fbs schemas/workshop_authoring.fbs`) all say plain `--ts`. The probe's audit-7 facts (137 files, 10 `optionalEncoding?:any`, one header per file, 136/127 by order) also hold for plain `--ts`, measured above. The matrix (`:49-58`), Concurrency, ACs and the acceptance table (`:177-189`) name no flag. The Spec Change Log's last entry (`:143`) lists exactly the edited places.
- **R12.** Applied at `:186` (`Exec` task starts whose `BuildEventContext` `(NodeId, ProjectContextId, TargetId)` matches a `TargetStartedEventArgs` with `TargetName` `GenerateWireTypeScript` and `ProjectFile` the FlatBuffers project) and `:67` ("the target's only one"). `:76` no longer names `TaskStartedEventArgs` as carrying a target name. Consequence of "only `Exec`": clearing `tools/e2e/.wire/` must use `RemoveDir`/`Delete`, not `Exec rm`; the owned scope implies that (clearing is listed separately from the one `Exec`), so it is not a finding. Keying by `TargetId` is also what excludes the provisioning `Exec`s (`csproj:103`, `:109`), which `CallTarget` runs under `ProvisionFlatBuffersCompiler`'s own target. One clause remains wrong: R15.
- **R13.** Applied at `:150` ("S5's pinned compiler, provisioning and inputs, reused by one more generation target").
- **R14.** Applied at `:164`. The filter at `docs/delivery-workflow.md:69` selects none of `WorkshopWireBuildTests`, `WorkshopWireTypeScriptTests` or `TimingDetectorTests` (`~` is a substring match; `SelectionTests` also selects `FastSelectionTests`, neither of which gains a test). `SelectionTests` and `TimingDeclarationTests` exercise the changed `ImportClosure` but their count is unchanged, so 821 holds.
- **Capability probe.** Present (`csproj:57`, `:127`, `:40`, with M1 and M5) and present-untested (TypeScript generation, which this slice tests through T1 to T5 and T7 to T9) entries remain cited; no missing capability; no owner decision open.

## 3. Intent contract

The only intent-contract change in this round is the flag in the Approach (`:26`): `--ts --gen-all` to `--ts`, in the same ordered single invocation. Build-generated TypeScript, the ignored output folder, the pinned runtime, no hand copy, the Never list and T1 to T10 are unchanged. The change keeps today's generated members (section 1) instead of losing two, so no gameplay, wire-byte or helper behaviour changes. D5 is a coordinator mechanism decision; no owner decision opens.

## Findings

- **R15. T8's keying clause contradicts its own tuple.** Classification: wrong specified behaviour (test specification); required text fix, not verdict-changing, no re-check needed.
  - Where: acceptance table T8 `:186`, "keyed as `ReplayCscRuns` keys its events".
  - Defect: `ReplayCscRuns` keys by `(context.NodeId, context.ProjectContextId, context.TaskId)` (`CuriousContraptions.tests/CSharpStaticGateTests.cs:412`), but T8's correlation needs `(NodeId, ProjectContextId, TargetId)`, which the same row names. A `TargetStartedEventArgs` has no valid `TaskId`, so following the clause literally matches no `Exec` start.
  - Consequence: an implementer who copies `ReplayCscRuns`' key gets 0 in every log; T8's "never zero" would refuse it in the first round, so this costs a repair loop, not a wrong acceptance.
  - Fix: replace the clause with "in a dictionary keyed by `(NodeId, ProjectContextId, TargetId)`, built the way `ReplayCscRuns` builds its `TaskId`-keyed one (`CSharpStaticGateTests.cs:403-427`)".

No blocking findings.

## Decisions that need the owner

None.

## Evidence limits

- The pinned compiler was run, with its digest checked against the pin, into `/private/tmp/claude-501/s5b-audit9-69112/` and compared in `/private/tmp/claude-501/s5b-audit9-69112-cmp/`, both outside the checkout. Generated output was compared by export, declaration and member names, not by bytes. Task 1 remains the implementer's gate.
- The probe's figure "2,290 member names" (`:106`, from run log E3) was not reproduced: this audit's extractors give 2,964 member lines, 2,302 column-0 method lines and 2,280 unique `Class.method` names, so the absolute figure depends on how members are counted. The equality claim it supports was reproduced under each extractor. No obligation rests on the number, so no fix is required.
- T4 and T10's incremental behaviour (clear, then regenerate after a schema-list change) and the MSBuild order computation were derived by reading, not built. T8's correlation was checked against `ReplayCscRuns`' source, not replayed.
- No build or test was run; planned test names are not executed proof. `git status --short` in the checkout before writing showed only the planning edits listed above.
