# Run log: S5c-2 wire conformance and root pin

Append-only. Contract: `_bmad-output/implementation-artifacts/spec-s5c-2-wire-conformance-and-root-pin.md`.

## Handled owner messages

- 26 November 2026, top-level coordinator rulings on S5c audit 6 (recorded in the S5c-1 run log, handled messages and E4): S5c is split into S5c-1, S5c-2 and S5c-3, whole rows per slice; S5c-2 is a draft, re-audited at its own dispatch from the main that contains S5c-1 and lane 3's E2E runner process lifecycle slice; its rows are S2, S3, S4, S5, S6, S7, S12 and S13.

## E1 Planning: created by the split (26 November 2026, planner for lane 2)

- Source: `spec-s5c-one-wire-schema-file.md` as it stood at the dispatch re-audit (9,841 words), audits `audit-s5c-1.md` to `audit-s5c-6.md`, and the coordinator ruling of 26 November 2026. Citations were taken from audit 6's correction table (A6-11), read at `39aaba70`; S5c-1 and lane 3 move some of them, so the dispatch re-audit re-reads each one.
- Carried here: rows S2 to S7, S12 and S13; audit 6 A6-1 (runner schema record and staleness check), A6-3 (a) to (i), A6-4 (T1 object-API form), A6-11; A6-2 resolved by sequencing (S5c-2 starts from the main that contains lane 3's slice); D3 with audit 6's schema-path-once note; D4, D6, D8; the A6-3 (a) choice (D10: the stray-output and missing-entry refusals move with the target).
- **H-S3 placed here (planner's dependency placement, flagged to the coordinator).** The coordinator ruling lists "H-S1 to H-S6" for S5c-3, but its parenthesis names only H-S1, H-S2, H-S4, H-S5 and H-S6. S5c-2 creates `tools/runtime/wire-frame.ts` as "the one TypeScript frame check ... imported by both the conformance script and `tools/e2e/workshop-wire.ts`", and its `SourceIdentity.cs` change digests `wire-frame.ts` because the suites import it. If `workshop-wire.ts`'s `openFrame` and identifier constants (H-S3) stayed until S5c-3, S5c-2 would add a second rule for the same behaviour, which the Forward-Only Auditor fails (AGENTS.md review gate). H-S3 therefore moves with `wire-frame.ts`. The `CCWS` message at `workshop-wire.ts:79` and the mock finishers (`:316`, `:379`) that read the deleted constants move with it; the dump and mocks are otherwise S5c-3's.

### Moved bulk: D3, generation chain (from the S5c contract)

- **Why generation moves.** The runtime TypeScript compile runs in the Abstractions instance in every configuration (`tools/runtime/RuntimeTypeScript.targets:23-33`). `CuriousContraptions.FlatBuffers` and Abstractions have no build-order relation, and S5b generates TypeScript only in the Debug FlatBuffers instance. Left as it is, the generated module would be missing or stale for that compile, including in CI's Release graph. TypeScript generation therefore moves into the Abstractions chain, ahead of the compile.
- **One writer per graph.** Locally, the one-writer rule is the chain's existing project-name guard. CI's `dotnet publish` (`.github/workflows/pages.yml:43`) may build Abstractions twice: once in Release as the web host's reference, and once in Debug through `BuildWorkshopEditorMetadata` (`CuriousContraptions.web/CuriousContraptions.web.csproj:47-52`). The runtime compile already runs in each such instance today. The two instances are reached from different targets of the web project, so they run one after the other (one web project node, blocking `MSBuild` calls; audit 6 D3 table). Each regenerates the same files from the same compiler and schema, so the later write is identical.
- **Evidence limit.** Task 1(c) derives CI's instance count from the project references and the web host's MSBuild calls, as S5b's task 2 did, and records that the CI graph is not observed locally.
- **What is deleted.** S5b's Debug-only guard and its rationale; `OrderWireSchemas`; the schema record.
- **Rejected alternatives.**
  - Keeping generation in FlatBuffers with a separate `tsconfig` compiled there: it needs the root `node_modules` before Abstractions' `npm ci`, plus an order edge between the projects.
  - A build-order reference from Abstractions to FlatBuffers: CI builds several FlatBuffers instances.
- **Output location.** Unchanged at `tools/e2e/.wire/` (S5b D1), so the 27 importers stay the same. `ENGINE-WIRE-ABI` moves it when production scripts import it.
- **Schema path once (audit 6).** Define the schema path once, as a property in `FlatcCompiler.targets` read by both steps. A second `WireSchema` include in a file every project imports would double `@(WireSchema)` in the vendored project (`:43`), pass the file to `flatc` twice and trip S2.

### Moved bulk: D4, D6, D8

- **D4.** The planning probe and audit 1 confirmed `InstanceRecordT` with `unpack()` and `pack(builder)` at 25.2.10, compiling under strict `tsc` 5.9.3. Audit 6's probe (`.tools/s5c-evidence/audit6/ts-oapi/`) showed the entry form `export { ActivationMemoryRecord, ActivationMemoryRecordT } from './wire/activation-memory-record.js';` (all 140 lines) and the class forms `export class InstanceRecord implements flatbuffers.IUnpackableObject<InstanceRecordT> {` and `export class InstanceRecordT implements flatbuffers.IGeneratedObject {` (structs too), and no `any` beyond the 10 normalised `optionalEncoding?:any` sites and the suppression comment.
- **D6.** Decision B put the pin in `tools/e2e` because no build graph compiled or ran the generated TypeScript, so a root pin would have been test-only provisioning. S5c-2's conformance script and `wire-frame.ts` are compiled by the runtime TypeScript compile in every graph, as the existing `tools/runtime` harnesses already are in CI, and a required test runs that script. The runtime therefore belongs in the root manifest, with one pin and one source.
- **D8.** Record `Equals` is not used, because `WorkshopCompiledScene` is a class without `Equals`, so a command or response that carries a scene would compare by reference; `Assert.Equivalent(..., strict: true)` compares collections by element and handles the scene graph's by-ref-like properties (xUnit 3.2.2, audit 2). NaN bit patterns are not portable through JavaScript.

### Moved bulk: S5b build-test dispositions (A6-3 (j), A2-7, A1-5)

- S5c-1 retargets `WorkshopWireBuildTests.cs:68`, `:82`, `:99`, `:123` and rewrites `ASchemaSetWithoutOneRootReachingEverySchemaRefusesTheBuild` and `ADeletedSchemaFileLeavesNoGeneratedSource` around probe schema files, because `OrderWireSchemas` and the `WireSchema` glob stay live there. S5c-2 deletes those two cases with their targets.
- `AReleaseBuildWritesNoTypeScript`: its behaviour is removed because Release graphs now generate. S2 reads only the local Debug logs, so it is not cited as cover.
- T8 (`TheTypeScriptStepRunsExactlyOncePerBuildGraph`) is in `WorkshopWireTypeScriptTests.cs:487-557`, not in `WorkshopWireBuildTests` (A6-3 (g)); S2 covers the count.

### Word count

- Contract at creation: see the S5c-1 run log E4 (`wc -w`).
