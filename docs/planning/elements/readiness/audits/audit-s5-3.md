# Planning audit: `spec-s5-generate-wire-accessors-in-build.md` (S5, third audit)

Audited 23 November 2026. Phase: planning; nothing was built or run. The contract was read in the `spec-drafts` checkout as an uncommitted draft (4,195 words). Code was read only through `git show 25df3cd5:<path>`, so "main" below means `25df3cd5`. Policy was read from the same checkout. No earlier audit report exists for S5 (completion ledger DC-55), so nothing is carried over from earlier passes.

## Verdict: Incomplete

The design is sound and implements the owner's approvals exactly:

- O1: a pinned 25.2.10 download, archive and binary digests, a per-user cache, placement by rename.
- O2: macOS arm64 and x86-64, and Linux x86-64, with an error naming any other host.
- O3: one CI check of the slice's commit.

Every cited `path:line` holds at `25df3cd5`. The contract is not Ready for Development, for five reasons:

- It does not name the unpublished planning work as a prerequisite.
- It missed one consumer, `GameFloatGuardTests`. That miss leaves five dead allow-list rows, which is a forward-only Fail, and an unlisted lost protection.
- AC3 is false as written.
- AC4 gives no number.
- The planned build tests share state across test methods.

All five are text fixes. None needs a redesign or a new owner decision for S5 itself. With B1 to B5 applied, the contract is a Scoped Pass.

## What was checked and holds

- **Citations (all at `25df3cd5`).**
  - `CuriousContraptions.FlatBuffers.csproj`: `:4-6` (comment), `:16-19` (linked items).
  - `CuriousContraptions.csproj:67-68`; `CuriousContraptions.slnx:63`; `CuriousContraptions.tests.csproj:16` (tool reference) and `:77` (S2 glob).
  - `CSharpStaticGateTests.cs`: `:29` (`GeneratedWire`), `:112` (vendored csproj digest), `:475-481` (`IsGeneratedFor`), `:851` (`TheBuildOutputSourcesAreOnlyTheSdkGeneratedOnes`), `:1082` (header exemption), `:1376` (catch-scan exemption).
  - `WorkshopFormatTests.cs:54-55`; `TypedTransportCommandTests.cs:397`.
  - `schemas/README.md`: `:22-24`, `:26-39`, `:41-59`; `FlatBufferConstants.cs:35`.
  - `tools/anvil/CheckStagedTests.cs`: `:35`, `:43`, `:60`; `tools/anvil/VerifyPolicy.cs:204` (real-git case "a C# file under an ignored directory passes").
  - `tools/E2eRunner/ImpactRules.cs:131`; `tools/E2eRunner/BrowserDeferral.cs:42`.
  - `_bmad/custom/bmad-loop-protocol.md:155`; `_bmad/custom/role-cards.md:64`; `docs/delivery-workflow.md:34`.
- **Consumer search by name.** `git grep -e WorkshopWireGenerat -e WorkshopAuthoringGenerat -e WorkshopWireGeneration 25df3cd5`, outside `_bmad-output/` and `docs/archive/`, finds 13 files. All but `docs/planning/invest/delivery-batches.md` are in the owned scope. That file is B3.
- **Files with no named consumer.** None of these names a generated file or the tool:
  - `CuriousContraptions.Core/RuntimeSources.props`;
  - `tools/anvil/VerifyPolicy.cs`;
  - `tools/Ownership/*`;
  - `tools/E2eRunner/*`;
  - `.github/workflows/pages.yml`;
  - `tools/anvil/no-skipped-tests.json`.
  So no `tools/anvil/` edit follows, and omitting the policy-tool harness command is correct.
- **Project lists derive from the solution.**
  - The static gate's expected compiles (`CSharpStaticGateTests.cs:240-251`, `ExpectedCompiles` and `ExpectedLogCompiles`) come from `CuriousContraptions.slnx`, so deleting the tool's line removes its expected compile with no further edit.
  - The trim list (`:670-705`) does not contain the tool.
  - The coupling gate classifies `tools/*` by path (`VerifyPolicy.cs:1252`) and keeps `CuriousContraptions.FlatBuffers` as `WireContract` (`:1215`), which is unchanged.
- **`SolutionMembershipTests.TheSolutionContainsEveryPackageTheDomainAbstractionsAndTheGenerator`.** Its "generator" is `tools/BundleElements/BundleElements.csproj` (`tools/BundleElements.Tests/SolutionMembershipTests.cs:25`), not the wire tool. S5 breaks no guard it does not own.
- **Vendored identity tests do not see `obj/`.**
  - `TheVendoredDirectoryIsExactlyItsPinnedFiles` (`CSharpStaticGateTests.cs:1125`) reads `Files`. That is git's tracked plus untracked-not-ignored list, or in the hook snapshot a walk that skips `obj` and `bin` (`:195`, `:199-214`).
  - `VendoredFlatBuffersIdentityTests.TheVendoredSetHasNoOtherSource` lists `*.cs` in the top directory only.
  - The vendored csproj is pinned once, at `CSharpStaticGateTests.cs:112`; task 5 updates it. `VendoredFlatBuffersIdentityTests.Pinned` does not include the csproj.
- **The source scan skips the new location.** `SourcesToScan()` drops every in-repository vendored path (`:501`), so generated files under `CuriousContraptions.FlatBuffers/obj/` are not scanned. M6 is their only check, as the contract says.
- **Staged-source rule.**
  - `.gitignore:12` (`**/obj/`) makes generated output an ignored directory. `CheckStagedTests.cs:43`, `:60` collapses it to one entry ending in `/` and drops it.
  - The new `WorkshopWireBuildTests.cs` must be `git add`ed. Verification says so (contract `:175`), and the deletions and csproj edits make the commit a source commit, so any stray untracked `.cs` file is refused.
- **Hook and CI.**
  - The hook builds `git checkout-index --all` from scratch with `--no-incremental` (`.githooks/pre-commit:23`, `:35-46`), so it generates from the cache.
  - CI runs the same builds and then `dotnet publish -c Release` on `ubuntu-24.04` (`pages.yml:13`, `:35-43`). That needs the Linux asset, which is covered by O3.
  - The maintainability gate builds into `--artifacts-path` (`VerifyPolicy.cs:1100-1110`), so generation runs there into the gate's temporary folder. The gate already excludes the vendored project (analyzers off) and `<auto-generated>` output (`:958-962`), so its text stays true.
- **Deleted tests.** `WorkshopWireGenerationTests` has 7 cases at `25df3cd5`: `GeneratedAccessorsHaveOneSupportedSurface` (1), `MalformedInputDoesNotReplaceExistingOutput` (5 `InlineData`) and `PinnedGenerationJoinsTypesInOneNamespaceAndRefusesAnotherRuntimeStamp` (1).
  - The allow-list edits in `WorkshopFormatTests.cs:54-55` and `TypedTransportCommandTests.cs:397` are dictionary and array entries, not test cases, so no case is lost.
  - Both scans skip build output: `TypedTransportCommandTests.IsBuildOutput` excludes `obj`, and `WorkshopFormatTests`' `SourceTrees` and `SourceProjects` do not include the vendored directory.
- **Copy list (task 7) against the import closure.** The import chain is:
  - the vendored `Directory.Build.props` imports the root one through `GetPathOfFileAbove`;
  - the root `Directory.Build.props` has no `Import`;
  - the root `Directory.Build.targets` imports only `tools/runtime/RuntimeTypeScript.targets`, whose targets are conditioned on other project names;
  - `global.json` selects 10.0.403.

  The vendored project builds with `CSharpStaticGate=false`, so `TreatWarningsAsErrors` is not set for it. Only the trim warnings are errors, through `WarningsAsErrors`. The listed files are therefore sufficient; see N2 for `.editorconfig`.
- **Tier and shape.** The engine tier is right: build configuration, a tool project, test-harness code and policy files are all outside the data-only allow-list. It is one structural slice. 4,195 words is within budget.
- **Element-free engine.** No engine behaviour, solver law, worker ABI or wire byte changes.
- **Owner approvals.**
  - O1: built-in `DownloadFile`, `VerifyFileHash` and `Unzip`; digests pinned in the project file; no NuGet or npm dependency.
  - O2: the asset table plus an error naming any other host.
  - O3: Verification's single CI check.
  - The contract implements each exactly. It adds no new external dependency beyond O1.
- **Serialisation with S3.** S5 correctly names the shared `CSharpStaticGateTests.cs` (S3 edits the trim list and `GeneratedConfigViolations`; S5 edits `:29`, `:112`, `:475-481`, `:851`, `:1082`, `:1376`) and `docs/delivery-workflow.md` (S3 `:64`, S5 `:34`). Their regions do not overlap.
- **Serialisation with the repair parts.** None of parts A, B, B2 or C, the Baseball repair or S6 edits `schemas/*.fbs` or the two generated files.

## Blocking findings

### B1. The unpublished planning work is not named as a prerequisite (wrong specified behaviour)

- **Where:** contract `:66` (Status), `:128` (Concurrency), `:142` (task 10), `:123`–`:124` (O4 and Order).
- **Evidence:**
  - Owner decision 22 is the contract's authority for running before the engine programme. It lives in `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`, and `git ls-files docs/planning/elements/readiness/` lists nothing: the folder is untracked.
  - Main's `delivery-batches.md` S5 section (`:260-265`) has no "When" line and no section 0. Both exist only in the uncommitted planning work, where the S5 section moved to `:291-297`.
  - Task 10 edits that same S5 section and `deferred-work.md`.
- **Defect:** a slice started from published main cannot resolve its own authority. Its task 10 edits text that the uncommitted planning work also rewrites.
- **Consequence:** a conflict on `delivery-batches.md`, and a status line that cites a document absent from the baseline.
- **Fix:** add as the first bullet of Concurrency (`:128`): "Prerequisite: the planning work of 22–23 November 2026 publishes first. That work is `docs/planning/elements/readiness/` (owner decisions 0–36), `docs/planning/invest/delivery-batches.md` sections 0 and 5, `named-elements.md`, `requirements.md`, `sprint-status.yaml`, `deferred-work.md` and `AGENTS.md`. This slice starts from the published main that contains it, and the coordinator re-reads the S5 section and the `deferred-work.md` 'S5:' item on that baseline before dispatch."

### B2. Missed consumer: `GameFloatGuardTests` leaves five dead allow-list rows and an unlisted lost protection (wrong specified behaviour)

- **Where:** contract `:77`–`:85` (owned scope and "checked and unchanged"), `:102`–`:109` (protections), `:138` (task 6).
- **Evidence:**
  - `CuriousContraptions.tests/GameFloatGuardTests.cs:39-43` lists five adapters: `CuriousContraptions.Wire.CaptureObservation.Milliseconds`, `GetMillisecondsBytes`, `GetMillisecondsArray`, `CreateMillisecondsVector` and `CreateMillisecondsVectorBlock`.
  - They exist because the guard scans `engine/gpu/WorkshopWireGenerated.cs`. `Included` (`:64-70`) admits `engine/` and `.cs` files.
  - They exempt the generated declarations of the schema's one `double` field, `milliseconds:[double]` (`schemas/workshop_wire.fbs:383`).
  - The only hand-written references are inside members that are themselves adapters: `WorkshopWireFlatBuffers.EncodeDurations` and `DecodeDurations` (`engine/gpu/WorkshopWireFlatBuffers.Observation.cs:168-171`, exempt through `GameFloatGuardTests.cs:109`).
  - After S5 the generated source sits under `CuriousContraptions.FlatBuffers/obj/`, which `Included` excludes twice: by the `CuriousContraptions.FlatBuffers/` prefix and by `obj`.
- **Defect, part 1:** the five rows become entries that admit nothing. That is a retained allow-list row for removed input, a Forward-Only Fail under AGENTS.md, and task 6 itself promises "no dead allow-list rows".
- **Defect, part 2:** the f32 guard over generated accessors is a removed protection that the protections list (`:106`) omits.
- **Fix, owned scope (`:78`):** add "`CuriousContraptions.tests/GameFloatGuardTests.cs:39-43` -- the five `CuriousContraptions.Wire.CaptureObservation.*` adapter rows, deleted (they admitted only generated declarations; the hand-written callers `EncodeDurations` and `DecodeDurations` are adapters themselves)."
- **Fix, task 6 (`:138`):** add `CuriousContraptions.tests/GameFloatGuardTests.cs` to its file list.
- **Fix, protections list (`:106`):** add "`GameFloatGuardTests` scanned the generated accessors and admitted the one `double` field by five adapter rows. Afterwards the generated code is not scanned. A new `double` schema field is still refused wherever hand-written game code reads or writes it (the guard flags the expression's type), unless the caller is a listed adapter. Residual: an unused `double` accessor is not refused."
- **Fix, Verification filter (`:173`):** add `|FullyQualifiedName~GameFloatGuardTests`.

### B3. AC3 is false as written (wrong specified behaviour)

- **Where:** contract `:147`, `:142`, `:196`.
- **Evidence:** main's `docs/planning/invest/delivery-batches.md:262` (S5 "Now") names both generated files, and `:264` (S5 "Acceptance") names `WorkshopWireGeneration`. Task 10 rewrites only the acceptance sentence, and closing S5 will describe what was removed. The planning work holds the same lines at `:294` and `:296`.
- **Defect:** "no tracked file outside `_bmad-output/` and `docs/archive/` names them" fails on the slice's own record. The AC3 test row has no command that checks the "names them" clause.
- **Fix, AC3:** "Given the candidate, when `git ls-files` is searched for `WorkshopWireGenerated`, `WorkshopAuthoringGenerated` and `tools/WorkshopWireGeneration`, then nothing is listed; and `git grep -n -e WorkshopWireGenerat -e WorkshopAuthoringGenerat -e WorkshopWireGeneration -- . ':(exclude)_bmad-output' ':(exclude)docs/archive'` matches only the S5 section of `docs/planning/invest/delivery-batches.md`, which records what was removed."
- **Fix, AC3 table row (`:196`):** add that `git grep` command as its second check.

### B4. AC4 states no number, and the M6 theory's case count is open (test)

- **Where:** contract `:148`, `:192`, `:172`.
- **Defect:** "minus 7 plus the cases this slice adds" leaves the expected total for the run log to fill in. Lesson 3 of the drafting brief requires the delta to be stated, and the registration check and hook selection have no expected delta at all.
- **Fix, row M6 (`:192`):** "`CSharpStaticGateTests.TheGeneratedWireRuleAdmitsOnlySchemaTypes`, a theory of exactly six cases: the schema-equal set (passes); one extra type; one missing schema type; a file outside the generation folder; a file compiled by another project; a define list containing `ENABLE_SPAN_T`. Plus the fact `TheVendoredCompileDefinesNoSpanAccessorSymbol` on the real binary logs, and the existing `TheBuildOutputSourcesAreOnlyTheSdkGeneratedOnes`, rewritten."
- **Fix, AC4:** "...the total is the current full local round at the baseline + 6 (−7 `WorkshopWireGenerationTests` cases, +6 `WorkshopWireBuildTests` facts, +7 `CSharpStaticGateTests` cases), re-measured at start; main's at drafting: 7,294, so 7,300 if unchanged."
- **Fix, Verification:**
  - registration check: "the current registration check at the baseline + 7";
  - hook: "its `CSharpStaticGateTests|WorkshopUiFailureTests` selection at the baseline + 7".

### B5. The planned build tests share state and controls across methods (test)

- **Where:** contract `:139` (task 7), `:189` (M3), `:193` (M7).
- **Defect:** task 7 copies "into a temporary directory" without saying whose copy it is. M3's control is "M2's edit and M7's build", which are other test methods. xUnit gives no order within a class. With one shared copy, M2's added field and M4's removed type leak into M3, M7 and M1. A control in another method proves nothing about the method that relies on it.
- **Fix, task 7:** "Each test method copies the listed files into its own new temporary directory, builds there, and deletes it at the end. No copy, build or state is shared between methods."
- **Fix, row M3 control:** "In the same copy, after the unchanged second build, touch `schemas/workshop_authoring.fbs`. A third build changes the generated sources' write time."
- **Fix, row M7 control:** "In the same copy, an ordinary second build before the `--no-incremental` build leaves the write time unchanged."

## Non-blocking findings (apply with the above)

- **N1. Session toolchain** (the same note is in all four reports). On this host the default `PATH` has Homebrew Node v26.13.1 and Homebrew dotnet 10.0.403 without the wasm-tools workload. The pinned toolchain at `~/.local/share/tim-toolchain` has `dotnet-10.0.403` with wasm-tools and `node-v26.12.1-darwin-arm64`. The capture block in `docs/delivery-workflow.md:41-43` (`test "$(node --version)" = v26.12.1`) and the web build fail unless the pinned directories come first. Neither the contract nor the workflow says so.
  - Add to Verification: "Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`; record the session environment check in the run log."
  - For S5 in particular, add to task 7: "Each child keeps `PATH`, `DOTNET_ROOT`, `HOME`, `TMPDIR` and `NUGET_PACKAGES`, and runs the `dotnet` executable that `DOTNET_HOST_PATH` names. It clears only `MSBUILD*`, `MSBuildSDKsPath`, `MSBuildExtensionsPath`, `VSTEST_*` and `TESTINGPLATFORM_*`."
- **N2. Copy list** (`:139`).
  - Add `.editorconfig`, so the copied compile sees the same `[CuriousContraptions.FlatBuffers/**.cs] generated_code = true` section and no `.editorconfig` above the temporary folder applies.
  - Say how "tracked files" is enumerated: `git ls-files -- CuriousContraptions.FlatBuffers` in the test's checkout. That way no `obj/` or `bin/` content is copied, which would break M1's control.
  - Record in the run log that the copy's implicit restore, including the SDK's ILLink package for an `IsTrimmable` project, resolves from the global packages folder without network.
- **N3. Host key** (`:134`, task 2). State that the asset table is keyed by the build host: `$([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)` with `$([MSBuild]::IsOSPlatform('OSX'))` and `IsOSPlatform('Linux')`, never `$(RuntimeIdentifier)`. The web host builds the vendored project for `browser-wasm`, and CI's Release publish does the same. The macOS universal binary serves both arm64 and x86-64.
- **N4. Writability** (task 2). Add: "If the cache path is not writable from the pre-commit hook or the agent sandbox, stop and return to planning; there is no alternative location."
- **N5. Evidence limit worth recording.** The two-argument `GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.DoNotVerify)` form was not shown to evaluate inside MSBuild; only the one-argument value was measured. Task 2 should record the evaluated path on macOS. Linux is proven by the O3 CI check.
- **N6. Protection of the new build logic** (`:104`). The new targets live in the gate's one opt-out project. The per-target pins (`RepositoryMsBuildLogicIsOnlyThePinnedTargetsAndImports`) and the MSBuild-line rule skip vendored files (`CSharpStaticGateTests.cs:1154`). The whole-file digest at `:112` is the one replacement. Add to the protections list: "The provisioning and generation targets are protected by the vendored project file's whole-file digest (`CSharpStaticGateTests.cs:112`) and by review of the project-file diff; the per-target pins do not cover the vendored directory."
- **N7. Task 8.** If two builds share one intermediate directory, the stop-and-replan route is right. Name the binlog query used, for example the `IntermediateOutputPath` of each `CuriousContraptions.FlatBuffers` project evaluation.
- **N8. Story wording.** D2 departs from the story's acceptance, which says "runs ... `WorkshopWireGeneration`". It is defensible: the tool existed only to make the output committable. Task 10 records the change and it is not a scope cut.

## Decisions that need the owner

- **S5b timing against owner decision 22.** O4 (21 November) schedules S5b "straight after S5". Owner decision 22 (23 November) allows only S3, S5 and S6 before the engine programme, and S5b appears in no planning document, on main or in the uncommitted work.
  - Question: does S5b run before the programme, as part of S5, or after it?
  - Recommended default, from the game's intent (TypeScript wire drift matters to the browser E2E phase, not to native play): after the programme, before the post-catalogue browser phase.
  - Until the owner answers, task 10 records S5b in `deferred-work.md` as "queued: generate the TypeScript wire module in the build (S5b); timing open: owner decision 22 lists only S5; owner to confirm before or after the engine programme", with its numbers and owner. It does not record "straight after S5".
  - This does not block S5.

## Evidence limits

- Nothing was built or run. Generation, provisioning and the build tests are planned only. Task 1's equivalence of committed and pinned-compiler output is not established.
- The Linux asset was not executed (as the contract states), and nothing confirms the cache's writability from the hook or the sandbox.
- No digest was re-downloaded; the pins are the contract's record.
- The import closure was read for MSBuild `Import` elements only. SDK-implicit package references were inferred, not restored.
