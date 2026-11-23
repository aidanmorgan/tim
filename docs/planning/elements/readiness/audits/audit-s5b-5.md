# Planning audit: `spec-s5b-generate-typescript-wire-in-build.md` (S5b), fifth pass

Audited 24 November 2026, planning phase, read-only. Checkout `spec-drafts`, code at published main `25df3cd5`. Nothing was built or run; the contract is 2,726 words (`wc -w`). Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md` (owner 22, 24h, 24j, 24k; coordinator 37, 38, 40). The contract has not been audited before.

## Verdict: Fail

Five blocking text defects, none needs an owner decision, and S5b's design (generate the TypeScript in the build, pin the runtime, delete the committed generated part) is sound. The Fail is because two defects follow from what the pinned compiler actually does, so the contract's central object, "the generated entry module", would not exist as specified, and because AC1 contradicts the slice's own edit. With B1 to B5 applied the contract is a Scoped Pass, **with task 1's hard gate kept** (see "Evidence limits": the compiler was not run).

## Owner decision 24j: what stays hand-written and who owns it

After S5 and S5b the FlatBuffers byte-layout accessors are generated for C# (S5) and TypeScript (S5b). Still hand-written, per the ABI audit section 1.3 (`docs/planning/elements/readiness/audits/abi-interfaces.md:37-49`) and the contract's list (`:83-93`):

| Code | Owner named by the contract | Owner in the planning documents |
|---|---|---|
| W1 domain mapping codecs (about 3,470 lines) | `ENGINE-WIRE-ABI` (routes refusal onto generated accessors) | `epics.md:4655` "Present today", first bullet. Ordinary typed C#, not wire code (coordinator decision 38) |
| W2 vtable literals, partial unknown-field refusal (`WorkshopWireFlatBuffers.cs:45-53`, `:59-82`) | `ENGINE-WIRE-ABI` | Story contract bullet "Unknown-field refusal is derived from the generated accessors" |
| W3 `motion` blob | `ENGINE-WIRE-ABI` | "`motion: [uint8]` becomes schema structs" |
| W4 raw-integer enum fields | `ENGINE-WIRE-ABI` | "Enums: the C# enum is the single source" |
| W5 hand helpers and mock encoders in `workshop-wire.ts`, `SNAPSHOT_SCHEMA` | `ENGINE-WIRE-ABI` | Deletes: "the hand TypeScript schema copies of W5" |
| W6 TypeScript enum mirrors (`workshop-observation.ts:3-49`) | `ENGINE-CAPABILITY-INSTANCES` generator | `epics.md:4644`, coordinator decision 38 |
| W7 every non-FlatBuffers layout | `ENGINE-WIRE-ABI` | "Fixed records are FlatBuffers structs" |

The contract names `ENGINE-WIRE-ABI` (order 8a, owner decision 24k) as owner at `:85-93`, and the story exists with those deletes and with S5 and S5b as dependencies (`docs/planning/elements/readiness/engine-stories-index.md:21`, `epics.md:4655-4691`). The 24j question is closed at planning level for S5b. One forward note is non-blocking (N3).

## Blocking findings

### B1. `flatc` 25.2.10 with plain `--ts` does not write an entry module that exports every schema type (wrong specified behaviour; inside the intent contract and Code Map)

- **Where:** contract `:26` ("also runs the same ... compiler with `--ts`"), `:33`, `:34`, `:49` (T1), `:64`, `:66`, `:101`, `:122` (task 1); `schemas/README.md:46` runs plain `flatc --ts` and bypasses the entry module by joining per-type files.
- **Evidence (the pinned compiler's own source, tag v25.2.10, `src/idl_gen_ts.cpp` and `src/idl_parser.cpp`, fetched 24 November 2026):**
  - The generator emits one entry file per namespace by default (`generate()` at `:116` calls `generateEntry()` unless `--ts-omit-entrypoint`). Its path is `<out>/curious-contraptions/wire.ts` (`TrackNsDef`, `:175-199`), its lines are `export { Name } from './wire/<kebab-name>.js';` (`:243-296`), and it is written with `SaveFile` at `:311`, which overwrites.
  - Each schema file on the command line is a separate generator run with a fresh parser (`flatc.cpp:861-863`: "If we're processing multiple schemas, make sure to start each one from scratch", `parser.reset(new Parser(opts))`). `schemas/workshop_wire.fbs:1` and `schemas/workshop_authoring.fbs:2` declare the same namespace `CuriousContraptions.Wire`, so both runs write the same `wire.ts`. `workshop_authoring.fbs:1` includes the wire schema.
  - Definitions of included files are marked generated and skipped unless `--gen-all` (`idl_parser.cpp:3725` `if (!opts.generate_all) MarkGenerated();`; skipped at `idl_gen_ts.cpp:365` and `:1555`).
  - So with the README's command, the last run (authoring) overwrites `wire.ts` and it lists only the authoring schema's own types. The wire schema's types (every importer name in `:69`: `CommandKind`, `ResponseKind`, `InstanceRecord`, `Vec3f` and the rest) would not be exported from the entry module. T1 fails on the first real run, and the importers have nothing to import from.
- **Consequence:** an implementer following the contract reaches task 1's gate and has to stop, or invents a flag. The contract is not Sufficient.
- **Fix:**
  1. `:26` and `:64`: replace "with `--ts`" by "with `--ts --gen-all`, both schemas on one command line". An equivalent simpler option, if the author prefers one run: `--ts --gen-all schemas/workshop_authoring.fbs` alone, because it includes the wire schema and so yields one entry listing every type; either way task 1 records which and the entry's contents.
  2. `:101`: replace the "Not verified in planning" bullet by "Read from the compiler source (v25.2.10, `src/idl_gen_ts.cpp:116`, `:243-311`; `src/idl_parser.cpp:3725`): with `--ts --gen-all` each schema run writes every type it can reach, and the last run's namespace entry, `<out>/curious-contraptions/wire.ts`, re-exports every schema type (the authoring run reaches the wire types through its include). Per-type files are `<out>/curious-contraptions/wire/<kebab-name>.ts`. The compiler was not run in planning; task 1 confirms this."
  3. `:122` (task 1): add "record the entry module's path, and that it re-exports exactly the 136 `enum`, `struct` and `table` declarations of both schemas (`grep -hE '^(enum|struct|table) ' schemas/workshop_wire.fbs schemas/workshop_authoring.fbs | wc -l` is 136 at `25df3cd5`, equal to the 136 `export class`/`export enum` lines of the committed generated part). If the entry lacks any type, or the compiler writes a different entry path, stop and return to planning."
  4. T1 (`:49`, `:169`): name the entry as `tools/e2e/.wire/curious-contraptions/wire.ts` and require its export names to equal the schema type names derived from the schema text.
  5. Code Map: the specifier an importer uses is `./.wire/curious-contraptions/wire.ts` (the entry is a `.ts` file; the `.js` extensions inside the generated files resolve under `moduleResolution: bundler`, `tools/e2e/tsconfig.json:6`).
- **Classification:** wrong specified behaviour.

### B2. `GENERATED_SOURCE` cannot read "the generated module": the class bodies are in per-type files (wrong specified behaviour)

- **Where:** contract `:70`; `tools/e2e/unit/run-reset-snapshot.test.ts:19` and `:122-125`, and its namespace import at `:12` (`import * as generated from '../workshop-wire.ts'`).
- **Defect:** the test slices a class body out of one concatenated string (`GENERATED_SOURCE.indexOf('export class X {')`). In the compiler's layout the entry module holds only `export { X } from ...` lines (B1 evidence), so every `indexOf` returns -1 and the test's offsets check throws or is vacuous when the browser phase re-enables it. The contract's task 3 lists the file but not what to read, and `:12` is not mentioned.
- **Fix, `:70`:** "`tools/e2e/unit/run-reset-snapshot.test.ts:19` -- `GENERATED_SOURCE` is the concatenation, in schema declaration order, of every `curious-contraptions/wire/*.ts` file under `tools/e2e/.wire/` (the per-type files hold the class bodies; the entry module holds only re-exports); `:12` -- the namespace import reads the entry module." Add to the Code Map: "no other file reads generated source as text (`rg -n "workshop-wire" tools/e2e` lists 27 files; only this test slices source)".
- **Classification:** wrong specified behaviour (a deferred test, but broken by the slice's own change).

### B3. The one-writer guard is unstated, the cited pattern does not transfer, and nothing executes the check (wrong specified behaviour and test)

This is an unproven risk, not a confirmed race: MSBuild builds one project instance per distinct global-property set, and I did not establish that any build graph here builds the FlatBuffers project twice with an empty runtime identifier. The defect is that the contract requires "one writer per graph" (`:33`) but names no mechanism and no executed check, so an implementer cannot know what to build or prove.

- **Where:** contract `:33`, `:117`, task 2 (`:123`), Verification (`:152`).
- **Defect:** `tools/runtime/RuntimeTypeScript.targets:23-31` achieves one writer by `Condition="'$(MSBuildProjectName)' == 'CuriousContraptions.Abstractions'"`, a project built once per graph. The generation target lives in `CuriousContraptions.FlatBuffers.csproj`, whose project name is the same in every build of it, and S5 notes that the web host and CI's publish build the project for `browser-wasm` (S5 contract `:141`), a different invocation. The contract says "the condition pattern of `RuntimeTypeScript.targets`" without giving a condition, and relies on `Inputs`/`Outputs` alone, which do not stop two concurrent project instances in one MSBuild process both finding the outputs stale and both clearing and writing one fixed folder. T2 to T4 run in S5's copied trees (one build each) and cannot see it; "Task 2 confirms from the binlogs" is a manual look.
- **Consequence:** if any graph does build two instances, a race shows as a flaky build or a half-written `.wire`, with no test to see it.
- **Fix:**
  1. `:33` and task 2: say that task 2 reads `solution.binlog`, `playtest.binlog` and `web.binlog`, records how many instances of the FlatBuffers project each graph builds and with which global properties, and then names in the contract's Code Map the exact guard the target carries (a condition on the instance, or the statement that each graph builds the project once, with the log evidence), in addition to the stated `Inputs` and `Outputs`. No condition is prescribed here because none was checked against the project references.
  2. Add row **T8**: the full local round already requires the five matching binary logs through `CSHARP_STATIC_GATE_BINLOGS` (`CSharpStaticGateTests.cs:276-281` fails visibly without it; `docs/delivery-workflow.md:46-51`), so a test reading them runs in the required round and is not skipped; add `WorkshopWireTypeScriptTests.TheTypeScriptStepRunsAtMostOncePerBuildGraph`, which reads `solution.binlog`, `playtest.binlog` and `web.binlog` and asserts the generation target executed at most once in each. Control: a synthetic count of 2 is refused naming the log. Delta +1 (B4).
- **Classification:** wrong specified behaviour (condition) and missing test.

### B4. AC1 is false after the slice's own edit; AC2 states no delta (wrong specified behaviour)

- **Where:** contract `:129`, `:130`, `:176`.
- **Defect:** `tools/e2e/workshop-wire.ts` has 13,101 lines and the marker is line 12,732 (`grep -n "hand-written helpers"`), so `wc -l` minus 12,731 is 370 and includes the marker. The slice deletes the marker (`:66`) and adds import lines, so "370 lines remain" cannot be true; an exact line count is also a text pin that proves nothing about behaviour (T5 already proves "no schema type declared").
- **Fix, AC1:** "Given the candidate, when `git ls-files tools/e2e` is listed, then nothing under `tools/e2e/.wire/` is tracked, and `tools/e2e/workshop-wire.ts` has no `export class`, `export enum` or `export namespace` declaration of a schema type and no 'everything above is flatc output' marker (T5 executes this)." **Fix, AC2:** "the total is the baseline round + 7: the new facts T1, T2, T3, T4, T5, T7 and T8; T6 extends an existing S5 fact and adds none; the digest edit in `CSharpStaticGateTests.cs` adds none (S5's total at drafting is 7,300, so 7,307 if unchanged)". Update the filter-run expectation likewise.
- **Classification:** wrong specified behaviour.

### B5. The intent's measured history is not reproducible (wrong specified behaviour, inside the intent contract)

- **Where:** contract `:22` (and `delivery-batches.md` S5b "Now"): "changed in 13 commits (+4,161/-689 lines, measured on `8d1d68aa`)".
- **Measured:** over the commit range `66da1ae7..25df3cd5` (`66da1ae7`, 4 November 2026, is the last commit touching the file before 5 November), `git log --format=%h -- tools/e2e/workshop-wire.ts` lists 16 commits and `--numstat` sums to +4,535/-719; the range ending at `8d1d68aa` gives the same. Date-based counts (`--since`) vary with the boundary and time zone (I got 15 and 16), so they are not a reproducible basis. The author's boundary is unstated and the figures do not match. The intent contract becomes read-only, so a wrong figure stays.
- **Fix, `:22`:** replace the parenthesis by "(16 commits and +4,535/-719 lines over `66da1ae7..25df3cd5`, `git log --numstat 66da1ae7..25df3cd5 -- tools/e2e/workshop-wire.ts`)", or delete the figures; apply the same to the planning document's S5b "Now" line.
- **Classification:** wrong specified behaviour (a fact).

## Non-blocking findings

- **N1. Stale deferral reason text.** `tools/E2eRunner/BrowserDeferral.cs:42-44` says the run-reset-snapshot test "needs the tsx 4.23.15 loader and flatbuffers, the browser-suite toolchain, which no required run provisions". After S5b `flatbuffers` is installed by `npm ci` (`RuntimeTypeScript.targets:23-26`), though nothing runs the test. The contract says the list is "not changed" (`:42`); say that the reason text is edited to name only tsx, or record why not.
- **N2. `docs/planning/elements/readiness/engine-stories-index.md:5` gate list** reads "structural stories S3, S5 and S6", and owner decision 22 the same; decision 24h adds S5b and S6b to S6f. Documents only.
- **N3. Location versus `ENGINE-WIRE-ABI`.** D1 puts the generated TypeScript beside the e2e tools, "its only consumers". `ENGINE-WIRE-ABI` (`epics.md:4668`) needs TypeScript accessors for the production runtime scripts (`pose-ring.ts`, `event-ring-cursor.ts`, ...), which `tsconfig.runtime.json` builds. Add one sentence to D1: "`ENGINE-WIRE-ABI` adds or moves the output for production scripts; this slice does not".
- **N4. `include` and dot directories.** `tools/e2e/tsconfig.json:19` gains `.wire/**/*.ts`. The pattern names the dot directory explicitly so it should match, and imports pull the files in regardless; record the type-check result in the run log when it returns.
- **N5. Importer lines** at `:69` (`clock-master.test.ts:10`, `cat-005.test.ts:8`, `workshop-driver.ts:9`, `cat-050-joined.test.ts:20`, `cat-017.test.ts:27`, `cat-020.test.ts:18`, `cat-015a.test.ts:5`) all verified at `25df3cd5`; 27 files import `workshop-wire.ts` (`rg -l "workshop-wire\.ts'" tools/e2e`).

## What was checked and holds

- **Capability probe.** S5's compiler provisioning, generation target, cache path, `WorkshopWireBuildTests` and the `:112` digest are S5's (`spec-s5-generate-wire-accessors-in-build.md:30-35`, `:78-79`) and exist only after S5 publishes, so "prerequisite S5 published" is right. TypeScript generation is correctly *present, untested*. The npm package exists: `npm view flatbuffers@25.2.10` returns version 25.2.10. The root `package.json` has `typescript` and `@types/node` only and `package-lock.json` exists, so the exact devDependency is one new line.
- **Normalisation.** Both strings the contract rewrites exist in the 25.2.10 generator: the `eslint-disable ... @typescript-eslint/no-explicit-any ...` header (`idl_gen_ts.cpp:153`, `:259`, entry file included) and `optionalEncoding?:any` (`:1633`, `:1731`, `:1822`). The other `any` strings in the generator (`:546`, `:548`, `:789`) are object-API and union paths; neither schema has a union and the committed generated part contains no `any` today.
- **Direct consumers.** 27 importers; `run-reset-snapshot.ts:8`; `unit/run-reset-snapshot.test.ts:8-12`, `:19` (B2); `tools/e2e/tsconfig.json:15-19`; `TypeCheck.cs:31`; `ImpactRules.cs:116-117`; `SelectionTests.cs:212`; `ArtifactSource.cs` (untracked files under `tools/e2e/` are harness-only, so the generated folder does not trip it); `HalfPrecisionGuardTests` scans git-listed files, so the ignored folder is not scanned while the schemas it guards are. `RuntimeTypeScript.targets` reruns `npm ci` when `package-lock.json` changes, which is the intended provisioning.
- **Removed protections.** Stated at `:103-107`: reviewed generated lines (replaced by schema review and S5's digest pin), the in-tree reference (T1, T5), and the unproven type-check (residual stated and true: every importer is deferred).
- **Commit path.** `tools/e2e/.wire/` is ignored; the hook builds an index snapshot that writes its own folder, and `CheckStagedTests` refuses an intent-to-add C# file, covered by the stated `git add` of `WorkshopWireTypeScriptTests.cs` (`:117`).
- **Forward-only, element-free, typing.** The committed generated part and the README procedure are deleted; no re-export module; no TypeScript post-processor; no element identity anywhere; generated TypeScript is `any`-free by T1. Engine tier is right (project file, TypeScript, tests, policy file); one structural slice; 2,726 words.
- **Shared files.** With S5: the vendored project file, `WorkshopWireBuildTests.cs`, `CSharpStaticGateTests.cs`, `schemas/README.md`, `bmad-loop-protocol.md`; sequential by decision 37. No other pre-programme slice touches `tools/e2e/`.

## Decisions that need the owner

None. B1 to B5 are contract-text repairs within owner decisions 24h, 24j and 24k.

## Evidence limits

- The compiler was not run (running `flatc` was refused by the sandbox, and the installed Homebrew `flatc` is 25.12.19, not the pinned 25.2.10). B1 rests on reading `idl_gen_ts.cpp`, `flatc.cpp` and `idl_parser.cpp` at tag v25.2.10. Task 1 must stay as a hard gate; I verified the layout and the entry's behaviour, not the exact file name of each of the 136 types.
- Test names are planned, not run. `npm view` and the source fetch are the only network calls made.
