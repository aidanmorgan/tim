# Planning audit: `spec-s3-remaining-per-package-lists.md` (S3, third audit)

Audited 23 November 2026. Phase: planning. Nothing was built or run. The contract is the uncommitted draft in the `spec-drafts` checkout (3,858 words, baseline field `25df3cd5`). Code was read only with `git show 25df3cd5:<path>`; "main" below means that revision. Policy was read from this checkout. Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md` (owner decisions 22 and 24g; coordinator 16, 21, 25–36). The earlier Scoped Pass (auditor `aa26de8e9f5eea107`, 21 November, on `8d1d68aa`) has no report on disk (completion ledger DC-55) and is not carried over.

## Verdict: Incomplete

The design is sound. Every code citation I opened at `25df3cd5` is correct or off by one line. The derivations work against the real code. The test deltas are right. The planned controls discriminate. Four text defects block Ready for Development:
- a missing serialisation prerequisite (the unpublished planning work);
- a wrong shared-file statement (S5);
- a whitespace check that misses the slice's new files;
- one removed protection that the contract records as covered when it is not.

None needs a redesign or an owner decision. With B1 to B4 applied it is a Scoped Pass. The fixes do not touch `<intent-contract>`.

## What was checked and holds

- **Citations at `25df3cd5`.** All of the following were opened and match:
  - `CSharpStaticGateTests.cs`:
    - `Files` at 199 and `RepositoryFiles()` at 201–206;
    - `PackageAnalyzers` at 558–566, with the two ILLink rows at 562–563 taking `TrimLinkedCompiles()`;
    - `TrimLinkedCompiles()` at 612–613 and `RequiredAnalyzerSwitches` at 660–668;
    - `TrimAnalyzedProjects()` at 670–705: eleven fixed projects at 672–680, 45 element entries at 681–704, the Baseball's at 701;
    - the caller at 792, `GeneratedConfigViolations` at 806–828, `TheSolutionListsEveryCSharpProject` at 1312.
  - `PackageRuntimeParityTests.cs`:
    - name pins at 149, 177 and 223;
    - slider rows at 383–399 and the count pin at 400;
    - `Parameters(id)` at 468–638 and the Windmill branch at 291;
    - `Placed(PartId)` at 641–662 and `Placed(PartId, Dictionary)` at 665–673.
  - Elsewhere:
    - `F32BehaviourIdentityTests.cs:39`, `:71`;
    - `BasketballResourceTests.cs:34`–`35` (registry at 32–33, reused by `registry.Create` further down);
    - `EventRingProducerTests.cs:780`;
    - `WorkshopConstruction.cs:31`–`39`;
    - `InstanceParameters.cs:34` (`Defaults`) and `:41` ("Missing or unknown declared parameters.");
    - `DeclaredPhysics.cs:41` (`Scoped`) and `:146` (`Of`, `InvalidOperationException`);
    - `ElementPortCatalogue.cs:89` (`Extend`) and `:320` (`Scoped`);
    - `BundlePortsTests.cs:19`–`27` and `PartRegistryTests.cs:17`;
    - `.githooks/pre-commit:23`, `:36`–`53`;
    - `BundleElements.csproj:26`, `SolutionMembershipTests.cs:32`–`38`, `BuildImpact.cs:9`–`33`.
  - The two disagreeing citations are in N1.
- **Path rule.** All 45 package projects at main are exactly `elements/<Name>/<Name>.csproj` (three segments; `git ls-tree -r --name-only 25df3cd5 elements | grep csproj`). No other `.csproj` lives under `elements/`. The eleven fixed entries are the only non-package trim entries. None of them is `tools/WorkshopWireGeneration`, so S5's deletion of that tool does not touch the trim list.
- **Static initialisation order.** `Root` (185), `OutputDirectoryNames` (196) and `Files` (199) are initialised before `PackageAnalyzers` (558) and `RequiredAnalyzerSwitches` (660). The class is not partial (`:22`), so deriving `TrimAnalyzedProjects()` from `Files` is safe in both file sources: `git ls-files --cached --others --exclude-standard` in a checkout, `Walk(Root)` in the hook snapshot.
- **Placement.**
  - Every `Placed(PartId)` arm is `WorkshopInput.Generic` at (0, 3, 0) with identity rotation, or a `PlacedParts` helper that is the same generic instance with the declared defaults: Battery 3600/120/1/Enabled, Plate 0.5, Counter 3, Clock 1 s, Latch and Domino with none.
  - `Placed(PartId, Dictionary)` is `private` and has one caller (`:183`). That caller passes every declared key at its maximum (`:182`), so the generic path's full-set check (`InstanceParameters.cs:41`) is always satisfied.
  - The `powered_gate` arm discards the dictionary, but Powered gate declares no parameters (`PoweredGateElementDeclaration.cs:32`), so the generic replacement is identical.
- **M3/M4 constructions.**
  - Ramp defaults: `length` 3 and `width` 1.3 (`RampElementDeclaration.cs:27`–`28`).
  - Springboard has three parameters: `stiffness` 120–1200, `damping` 0–8, `initial_compression` 0–0.2 (`SpringboardElementDeclaration.cs:27`–`29`).
  - The re-identification pattern is the one `BundleElementIdTests.AnUnfamiliarDeclarationPassesSaveAndWorkerAdmission` already uses (`:113`–`117`).
  - The unregistered-identity control reaches `DeclaredPhysics.Of`, either through `InstanceParameterCollection.Defaults` or through `Parameters.Validate(PartId)` (`WorkshopInstances.cs`, `WorkshopGenericInstance.Validate`). An omitted key gives `ArgumentException` at `InstanceParameters.cs:41`.
  - xUnit runs serially (`xunit.runner.json`: `parallelizeTestCollections: false`), and `PackageRuntimeParityTests` is in `NativeSceneCollectionDefinition` (`:15`). A scoped install therefore cannot leak into a concurrent test.
- **Palette derivation.** `ElementCategory` is `Motion = 1`, `Goals = 2`, `Power = 3`, `Control = 4`, `Structure = 5` (`CuriousContraptions.Abstractions/ElementCategory.cs:7`–`11`), which is the order of the literal headings. The literal is 5 headings plus 45 identities (`WorkshopIntegrationHarnessTests.cs:691`–`698`).
- **M1 message.** `GeneratedConfigViolations` defaults an absent key to `""` and yields `build_property.{key} = {value} (must be true here)` (`:825`–`826`). The contract's expected text `build_property.EnableTrimAnalyzer =  (must be true here)`, with two spaces, is exact.
- **Removed protections (other rows).**
  - Footprint is authored literally in each declaration (for example `PoweredGateElementDeclaration.cs:34`, `FanElementDeclaration.cs:25`). `EveryPackageDeclaresTheCompiledFootprintCollidersAndMaterial` (`:84`–`100`) compares it with the compiled prismatic and revolute counts. A package that drops `ISliderElement` or `IHingeElement` therefore fails there.
  - A package whose project is lost is still caught by its kept solution line (D1).
  - A wrong derivation that left element projects out of `TrimAnalyzedProjects()` fails `EveryLoggedCompileReceivedThePinnedCompilerInputs` as an unpinned ILLink analyzer (`:766`).
- **Test deltas.**
  - New tests: the M1/M2 theory has 5 cases; the M1 config fact, the M3 fact and the M4 fact add 1 each. That is +8 in the full round.
  - All 8 are in `CSharpStaticGateTests` (6) and `PackageRuntimeParityTests` (2), and both classes are in the registration filter (`docs/delivery-workflow.md` registration-check command at main), so the registration check is also +8.
  - The hook's filter selects `CSharpStaticGateTests` and `WorkshopUiFailureTests` (`.githooks/pre-commit:53`), so the hook is +6.
  - No test is deleted. `BasketballResourceTests.TheBundleAndSceneUseDeclaredNumericMaterials` loses one assertion only.
- **Rename.** `EveryCurrentLessonAndElementHasAnExecutedScenario` is named only at `F32BehaviourIdentityTests.cs:36` and in the historical record `spec-11-4a-wound-spring.md:817`. It is not named in `tools/anvil/no-skipped-tests.json` (a regex pattern registry with no test names), coverage, `TODO.md`, docs, CI or the hook.
- **Tier and paths.** Engine tier is right: shared test builders and the static gate are off the data-only allow-list. AC4's path set matches the owned scope.
- **M7.** `SolutionMembershipTests`' "generator" is `tools/BundleElements` (`SolutionMembershipTests.cs:25`–`26`), not the wire generator, so S5 does not affect M7.
- **Budget and size.** 3,858 words; seven matrix rows; one coherent slice. Decisions D1–D4, O1 and O2 are defensible. O1 and O2 are recorded as owner answers, so none cuts scope or waives a gate without the owner.

## Blocking findings

### B1. The planning work this contract cites is not on main, and the contract does not name it as a prerequisite (wrong specified behaviour)

- Where: contract `:49` ("runs before the engine programme (owner decision 22)"), `:208`, task 7 (`:148`).
- Evidence:
  - `docs/planning/elements/readiness/` (which holds owner decision 22 and completion-ledger DC-55) is untracked: it is not in the index and not in `25df3cd5`.
  - `delivery-batches.md` section 0 and the S3 "When." line exist only in the uncommitted planning edits. Main's S3 section has no "When." line (`git show 25df3cd5:docs/planning/invest/delivery-batches.md`, lines 239–244).
  - Task 7 rewrites the same S3 section and the `deferred-work.md` "Structural stories queued" block that the planning work also edits.
- Consequence:
  - An implementer starting from published main cannot resolve the contract's authority for running now.
  - Whichever of the two publishes second gets a textual conflict in `delivery-batches.md` S3.
- Fix: add a bullet at the head of "Running beside other work" (`:208`): "Prerequisite: the planning work of 22–23 November 2026 (`docs/planning/elements/readiness/`, including `owner-decisions-2026-11-22.md`; `delivery-batches.md` sections 0 and 5; `named-elements.md`, `requirements.md`, `vertical-delivery.md`, `sprint-status.yaml`, `deferred-work.md`) is published before this slice starts, and the coordinator re-captures the baseline as that revision. Task 7 then edits the S3 section as published there (it keeps the **When.** line)."

### B2. "Running beside other work" omits the two files S3 shares with S5 (wrong specified behaviour)

- Where: contract `:208`.
- Evidence: S5 (`spec-s5-generate-wire-accessors-in-build.md:77`, `:82`, `:128`) edits `CuriousContraptions.tests/CSharpStaticGateTests.cs`:
  - `GeneratedWire` at 28–29;
  - the vendored digest at 112;
  - `IsGeneratedFor` at 474–481;
  - `TheBuildOutputSourcesAreOnlyTheSdkGeneratedOnes` at 851;
  - the exemptions at 1082 and 1376;
  - new tests.

  It also edits `docs/delivery-workflow.md:34`. S3 owns the trim list, `GeneratedConfigViolations` and two new tests in the same class, and the paragraph at `:64` of the same document. S5 names the overlap; S3's list ("`docs/delivery-workflow.md`, `deferred-work.md`, `TODO.md`, `delivery-batches.md`") does not name `CSharpStaticGateTests.cs` at all, and does not name S5 for the workflow document.
- Consequence: the coordinator may run the two slices without serialising them, which risks a conflicting merge in the static gate class. That class is the hook's own guard.
- Fix: in `:208`, replace "Shared files touched: `docs/delivery-workflow.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, `TODO.md`, `docs/planning/invest/delivery-batches.md`." with: "Shared files touched: `CuriousContraptions.tests/CSharpStaticGateTests.cs` (with S5, which edits its generated-source rule, vendored digest and exemptions; different members, same class), `docs/delivery-workflow.md` (with S5, which edits `:34`), `_bmad-output/implementation-artifacts/deferred-work.md`, `TODO.md` and `docs/planning/invest/delivery-batches.md` (with every slice allowed before the programme). One publisher at a time; the later publisher merges main once, re-reads every `CSharpStaticGateTests.cs` line cited here and repeats the full local round."

### B3. The whitespace check skips the slice's new files (test)

- Where: contract `:198`.
- Defect:
  - The contract and `run-logs/spec-s3-remaining-per-package-lists.md` (owned at `:61`) are new and untracked on the implementation checkout.
  - `git diff --check` ignores untracked files.
  - The protocol's handoff step 5 requires `git add -N` first.
- Fix, `:198`: "`git add -N _bmad-output/implementation-artifacts/spec-s3-remaining-per-package-lists.md _bmad-output/implementation-artifacts/run-logs/spec-s3-remaining-per-package-lists.md`, then `git diff --check <baseline_revision>` -- expected: no output."

### B4. The payload name pin's protection is recorded as covered, but nothing replaces it (wrong specified behaviour)

- Where: contract `:130` ("payload membership by `PartRegistryTests.Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder` … payload is tied per package at `PartRegistryTests.cs:51–53`").
- Evidence: `PartRegistryTests.cs:51`–`53` asserts that a package's declared `Payload` equals `DeclaredPhysics.IsPayload` for the same package, which is consistency between the declaration and its bundle entry. Both sides come from the same declaration.
  - With the literal pin at `:149` gone, a package whose `Payload` changes from `CaptureTarget` to something else silently leaves `EveryPayloadPackageCompilesItsDeclaredMaterial`'s loop, and every remaining test passes.
  - For sliders and hinges the contract's footprint argument holds. For payloads there is no independently authored counterpart.
- Consequence: a lost protection is recorded as replaced. Lesson 1 requires the residual to be stated plainly.
- Fix, `:130`: replace "payload membership by `PartRegistryTests.Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder`;" with "payload membership: **residual** (R5 below);". Delete "payload is tied per package at `PartRegistryTests.cs:51–53`". Then add to the residual list at `:136`: "**R5** a package whose declared `Payload` policy changes leaves the payload parity loop without failing it; `PartRegistryTests.cs:51–53` checks only that the declaration and its bundle entry agree. Its own capture tests and the visible declaration diff cover it (owner: S4, whose conformance suite runs per package)." Task 7 adds R5 with R2–R4.

## Non-blocking findings (apply with the above)

- **N1. Line ranges of the palette literal.** The array is `WorkshopIntegrationHarnessTests.cs:691`–`698`; `:700` is the assertion. The contract says 691–700 (`:57`), 691–699 (`:85`, `:147`) and "50 rows" (`:147`). Write "the literal at 691–698 (asserted at 700)" in all three places.
- **N2. Intent wording "every trimmable project"** (`:27`). `CuriousContraptions.FlatBuffers` is `IsTrimmable` (`CuriousContraptions.FlatBuffers.csproj:12`) but is not in the trim list today, because it is the vendored analyzer opt-out. The sentence reads as "keeps", so behaviour is unchanged. To avoid a literal reading, the coordinator may record in the Code Map's Decisions: "'every trimmable project' in the intent means the gate's trim list: the eleven fixed projects and every package project; the vendored project stays outside it as today."
- **N3. The M1 config fact bypasses the derivation** (`:180`). The table it passes is hand-built from the synthetic path, so the composition `Files.Where(IsElementProject)` → `TrimAnalyzedProjects()` → `RequiredAnalyzerSwitches` is proven only on the 45 real packages. That proof does exist: a missed concatenation fails `EveryLoggedCompileReceivedThePinnedCompilerInputs` with an unpinned ILLink analyzer. Better: give `TrimAnalyzedProjects` a file-list parameter (`TrimAnalyzedProjects(IEnumerable<string> files)`, with the static use passing `Files`), and build the fact's table by calling it with `["elements/Unfamiliar/Unfamiliar.csproj", "tools/Preview/Preview.csproj"]`. The same fact then proves the derivation and the check together.
- **N4. D3 wording** (`:116`). "Every arm of both switches builds the same generic instance … with the declared defaults" is true of the first switch. In the second switch the `powered_gate` arm discards the given values, which is harmless only because Powered gate declares no parameters (`PoweredGateElementDeclaration.cs:32`). Add that clause, so a reviewer does not read M4 as a behaviour change for Powered gate.
- **N5. M3's construction is a step beyond its probe evidence** (`:71`, `:182`). The cited exercising test re-identifies the zero-parameter `ball`. M3 re-identifies `ramp`, whose colliders are parameter-bound (`RampElementDeclaration.cs:42`–`49`). It is a planned test, and a failure is a capability discovery to return to planning, not an in-slice repair. Either state that, or re-identify a zero-parameter static part. The latter would not exercise "with exactly its declared default parameters", so keeping `ramp` and stating the rule is preferred.
- **N6. Session environment** (the same note is in all four reports). On this host the default `PATH` has Homebrew node v26.13.1 and Homebrew dotnet 10.0.403, which has no `wasm-tools` workload. The pinned toolchain is `~/.local/share/tim-toolchain` (`dotnet-10.0.403` with the wasm packs; `node-v26.12.1-darwin-arm64`). The capture block in `docs/delivery-workflow.md` (`test "$(node --version)" = v26.12.1`, and the web build) therefore fails unless the pinned directories come first. Neither the contract nor the workflow says so. Add to Verification: "Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`; record the session environment check in the run log."

## Decisions

- None needs the owner. D1–D4 are defensible, and O1 and O2 are owner answers of 21 November 2026.
- For the coordinator: the order of S3 against S5 (B2) and against the planning publication (B1).

## Evidence limits

- Nothing was built or run. The equality of the derived palette rows with the literal is the implementer's to re-check against the baseline's generated bundle, as task 6 says. The bundle is git-ignored and was not regenerated here.
- That the compiler compiles a re-identified, parameter-bound `ramp` entry under the scoped seams was not executed (N5).
- The uncommitted planning work and the other drafted contracts were read only for the files and sections they share with S3.
