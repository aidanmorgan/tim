# Planning audit: `spec-s5c-one-wire-schema-file.md` (S5c), fifth pass (bounded check of the audit 4 repairs)

Audited 25 November 2026, read-only, planning phase. Checkout `.claude/worktrees/plan-s5c-one-wire-schema` (HEAD `51d46f7b`, uncommitted planning edits: the untracked contract and audits 1 to 4, and eight edited planning documents). `.claude/worktrees/slice-s5b` was read for context only. Policy was read from this checkout: `AGENTS.md`, `docs/testing-guidance.md`, owner decision 24p with coordinator rulings (a) to (f) (`owner-decisions-2026-11-22.md:71-77`) and `_bmad/custom/bmad-loop-protocol.md`.

Scope, as bounded by the coordinator: the repairs of audit 4 findings A4-1 to A4-5 and their direct consequences, every `path:line` those repairs cite (re-read at `51d46f7b`), and whether the Code Map "Planning verdict" line and the Spec Change Log describe the audit history accurately. Settled design outside those repairs was not reopened. No build, test, probe or maintainability-gate run was made; the audit 4 probe (`.tools/s5c-audit4/probe.fbs:16`, `table SavedConstruction (authoring) {`) was re-read only.

## Verdict: Scoped Pass

- All five audit 4 findings are closed. A4-2 (medium) is closed: S3's boundary can now fail on the defect it names.
- No high or medium finding is open.
- Three new findings are low (A5-1 to A5-3). Under the review repair policy (AGENTS.md, 4 November 2026) each is still repaired in the contract, or by the implementation owner at dispatch, and re-checked before acceptance.

This is a scoped planning pass. It is not an implementation pass, and it is not build, browser or release qualification.

## Audit 4 dispositions

| Finding | Disposition | Evidence |
|---|---|---|
| A4-1 (low): compiler properties and their incremental input | **Closed** (consequences in A5-1) | Code Map `:185` moves `:23-41` (`FlatcVersion`, `FlatcCacheRoot`, `FlatcAsset`, both digests, `FlatcCacheDirectory`, `FlatcPath`) with `ProvisionFlatBuffersCompiler` (`:57`) and `DeleteFlatcTemporary` (`:114`); `GenerateWireAccessors`' Inputs (`:128`) gain the shared file; both pin-mismatch cases edit the digest in the shared file. Consistency checked: (1) import once: `Directory.Build.targets:41` imports `tools/runtime/RuntimeTypeScript.targets` unconditionally; the only nested Directory.Build file in the vendored directory is `Directory.Build.props` (`git ls-files`), and nothing sets `ImportDirectoryBuildTargets`, so the vendored project and Abstractions both receive the shared file through one importer, as `PinnedImports` (`CSharpStaticGateTests.cs:1368-1377`) will record. (2) Evaluation order: the shared file is imported after the project body, but every use of the moved properties is inside a target body or a target's Inputs, which expand at execution, and the body's only evaluation-time item (`WireSchema`, `:43`) reads none of them. (3) `RequireCachedCompiler` (`WorkshopWireBuildTests.cs:396-400`) and the redirect cases (`:248`, `:292`) read the properties with `dotnet msbuild -getProperty`, which reports final evaluated values including imports; the `-p:FlatcCacheRoot` redirect is a global property and still wins. This holds only if the copied tree contains the shared file (A5-1). (4) Static gate: the vendored project file stays pinned whole (`:116`), the shared file's two targets join `PinnedTargets` (Code Map `:188`), and none of the moved property lines matches the gate-line regex (`:1304-1310`). (5) CI: `.github/` names no compiler, digest or project-file hash, so nothing there reads the moved properties. |
| A4-2 (medium): S3's `file_identifier` boundary could not fail | **Closed** (wording conflict in A5-3) | Row S3 (`:139`) and its test row (`:516`) now assert that a codec-written save carries at `SizePrefixLength + 4` the bytes that the one conversion gives for `WireRootIdentifier.SavedConstruction`, and that the same save re-stamped with another member is refused by `ICommandCodec.ValidateSaveFrame`. A size-prefixed FlatBuffer is the 4-byte prefix (`FlatBufferConstants.SizePrefixLength`), the 4-byte root offset, then the identifier, so the offset is right. The expected bytes come from the enum declaration through the conversion; the actual bytes come from the finisher's `file_identifier` literal. A schema whose `file_identifier` differs from the member therefore fails the first assertion, and a `ValidateSaveFrame` that ignored the identifier fails the second. The check is independent of the finisher's literal. D2 (`:333`) and Always (`:69`) now name a check that can fail. |
| A4-3 (low): conversion domain and constant contexts | **Closed** | Always `:70` makes the one conversion total over `uint32`. `:74` gives the theory a `WireRootIdentifier` parameter with `(WireRootIdentifier)Foreign`, `Foreign` a named `uint` constant. An explicit enumeration conversion of a constant is a constant expression, so it is a valid `InlineData` argument, and the generated enum is public. `WorkshopSaveRejectionTests.cs:42` is the positional record `Save`; an enum member is a valid default for its `Identifier` parameter, and its one use is `:104` (`builder.FinishSizePrefixed(..., save.Identifier)`). The internal C# conversion is reachable from the tests through `CuriousContraptions.Core.csproj:20` (`InternalsVisibleTo CuriousContraptions.tests`), so no test needs a second conversion. `OldLayoutSaves.cs:45` and `ClutchGeneralityTests.cs:443` pass a `string` straight to the vendored API, which is not a conversion. One conversion per language holds. |
| A4-4 (low): S10's counting rule | **Closed** | Row S10 (`:146`): the admission is one source location (file and span), deduplicated across the replayed compiles; a second distinct location in any compile fails. D7 (`:359`) and the S10 test row (`:523`) agree. |
| A4-5 (low): polish | **Closed** (record gaps in A5-2) | Stale comments: `WorkshopSaveCodec.cs:44-47` (re-read: "then the refusal of any instance record field beyond the current table") is reworded (Code Map `:222`); `DeclaredOrientationTests.cs:154-156` (re-read: "the slots the rule walks reach the same instance tables") is reworded with H-T4 (`:223`). The `:61` anchor (re-read: `"table SavedConstruction {\n"`) becomes `table SavedConstruction (authoring) {` (`:253`), the syntax the audit 4 probe compiled. `OldLayoutSaves.cs:45` (re-read: `WorkshopWireFlatBuffers.CommandIdentifier`, inside the `:11-80` region) is named at `:254`. The two `WorkshopSaveCodec.cs` bullets were not merged; `:222` states that they describe one file and are applied together, which meets the finding's intent (no partial application), and the bullets agree. Design Notes 7 (`:479`) names the three `ICommandCodec` identifier properties. Always `:69` now admits "generated code: the generated `WireRootIdentifier`, or the root API that `root_type SavedConstruction` emits", which covers tests. |

## New findings

### A5-1 (low): the A4-1 move leaves the vendored build fixture and three texts behind

Direct consequence of A4-1. No hard rule is breached and nothing is unimplementable: the first fixture build fails at once.

Evidence:
- The vendored-project fixture copies only the vendored directory, the schemas and `SharedBuildFiles` (`WorkshopWireBuildTests.cs:31-32`: `Directory.Build.props`, `Directory.Build.targets`, `global.json`, `.editorconfig`, `tools/runtime/RuntimeTypeScript.targets`; S5b's `:36-38` is the same list). After S5c, `RuntimeTypeScript.targets` imports the shared file, so a copy without it fails evaluation with a missing import, in `RequireCachedCompiler` and every vendored build case, including the retargeted pin-mismatch edit. The contract never names the shared file's path or this list.
- `schemas/README.md:41` says the digests are "pinned in `CuriousContraptions.FlatBuffers.csproj`". The contract's README edit ranges (`:268`: `:3-9`, `:48-53`, `:55-78`) do not include it, and S11 does not scan `.md` files.
- Vendored project comments that the move makes false: `:5-8` ("the pinned compiler below", "its digests below"); `:55-56` ("Runs only from GenerateWireAccessors", which travels with the target and is no longer true once TypeScript generation calls it); `:124-126` (the generation inputs, which gain the shared file).

Repair: name the shared file's path in the Code Map; add it to `SharedBuildFiles`; add `schemas/README.md:39-46` to the README scope; reword the three comment blocks with the vendored project edit (its whole-file pin at `CSharpStaticGateTests.cs:116` changes anyway).

### A5-2 (low): the Spec Change Log's audit 4 entry is incomplete, and one causal claim has no traceable source

Evidence:
- The A4-5 entry (`:457`) lists "stale comments, the `:61` anchor, `OldLayoutSaves.cs:45`, and Design Notes 7". It omits the Always `:69` wording change and the `WorkshopSaveCodec.cs` bullet treatment (cross-referenced at `:222`, not merged as audit 4 asked).
- `:458` says the repairs await a re-check "because the four-round cap is reached (see the planning verdict in the Code Map)". No rule in this checkout sets a cap on planning audits: the loop protocol has the five-attempt `bad_spec` limit and the third-pass Re-review scope (`bmad-loop-protocol.md:124`, `:227`), and the Code Map line it cites (`:168`) does not mention a cap. The claim may come from a coordinator instruction outside the checkout; that is not established here.
- `:75` still says `"XXXX"` at `WorkshopSaveTests.cs:398` "stays as test data", while `:74` now spells it `(WireRootIdentifier)Foreign`. They mean the same foreign word, but the literal text no longer stays.
- `:253` restates the edit anchor only for `:61`. S5b's `AnAddedSchemaFieldIsATypeScriptAccessorAfterAnIncrementalBuild` (`slice-s5b/.../WorkshopWireBuildTests.cs:82`), merged into S13, uses the same stale anchor. The dispatch `rg -n workshop_authoring` finds the file name but not the anchor.

Repair: complete the A4-5 change-log entry; cite the source of the cap or drop the causal clause; say at `:75` that the foreign word stays as test data in its `Foreign` constant; apply the marked anchor to every case that edits `SavedConstruction`.

The Code Map "Planning verdict" line (`:168`) and the audit 1 to 4 entries otherwise match the record: audits 1 to 4 each have `## Verdict: Fail` (`audit-s5c-1.md:7`, `-2.md:7`, `-3.md:12`, `-4.md:13`), and audit 4's grading (A4-2 medium, four low) is stated correctly.

### A5-3 (low): the repaired S3 reads frame bytes, which the contract's own wording forbids

Evidence:
- S3's boundary (`:139`, `:516`) reads the four identifier bytes at `SizePrefixLength + 4` of a codec-encoded save, in `WireConformanceTests`.
- Always `:94`, about that conformance test: "The test never compares bytes or hashes." Never `:130`: "Hashes or byte comparisons of encoded output as acceptance".
- A reviewer applying the contract literally (Verification Gap or Forward-Only) would flag the S3 test.

Grade basis: low, not medium. The hard rule forbids hash or whole-output byte-identity acceptance; AGENTS.md (game fidelity) keeps "Exact IDs … and canonical serialized field values" valid, and the frame identifier is one such canonical field. The conflict is in the contract's wording, not a breach of the rule.

Repair, either:
- add a carve-out at `:94` and `:130` for the frame identifier field, a canonical serialized value read at its defined offset; or
- replace the byte read with "the codec-written save is admitted by `ICommandCodec.ValidateSaveFrame`". That still fails when the `file_identifier` differs from the member, because Core's frame boundary compares against the converted member, and it reads no bytes. Keep the re-stamp control.

## Checks that hold

| Question | Result |
|---|---|
| New contradictions from the repairs | None of high or medium grade. A5-3's wording conflict is the only one inside the contract. |
| Rulings (a) to (f) of 24p | Unaffected. Ruling (b) is now proven by an assertion that can fail. Ruling (f)'s reservation holds: Abstractions gains no reference. |
| Forward-only | The repairs add no fallback, alias, second conversion, second frame check or retained deprecated item. Moving the compiler properties leaves one definition of each. |
| Testing guidance | S3 and S10 now have discriminating assertions with named controls. No hash, mutant, skip or browser gate is added. |

## Notes for the coordinator

- The review repair policy still requires A5-1 to A5-3 to be repaired and re-checked before dispatch, in the contract or by the implementation owner at dispatch.
- The Code Map "Planning verdict" line (`:168`) becomes stale once this audit lands. Updating it is the coordinator's record update, not a finding.
