# Planning audit: `spec-s3-remaining-per-package-lists.md` (S3, fourth audit)

Audited 23 November 2026. Phase: planning. Nothing was built or run. The tests are planned and have not been executed. Contract lines below are from the snapshot `/private/tmp/claude-501/-Users-aidan-dev-personal-tim/37b1f527-b6f7-4082-bfd2-686d62e814b3/scratchpad/audit4-snap/spec-s3-remaining-per-package-lists.md`, taken at 21:33. That draft has 4,247 words and baseline field `25df3cd5`. Code was read at HEAD `25df3cd5` with `git show HEAD:<path>`; the worktree has no uncommitted change under `CuriousContraptions.tests/`, `engine/`, `tools/`, `ui/` or `.githooks/`. Policy was read from this checkout. Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md` (owner 22, 24h; coordinator 37). `.tools/audit/` citation strings are out of scope (the decision 24l re-pointing is in progress).

## Verdict: Incomplete (one blocking text fix, B-1; with it applied, Scoped Pass (ready-for-dev))

All four blocking findings of the third audit (B1–B4) are resolved in substance, and so are its six non-blocking findings (N1–N6). I re-opened every cited code line at `25df3cd5`. The derivations, controls, deltas, tier, commit-path reasoning and serialisation all hold on published main. That includes:
- the one compile rule at `CuriousContraptions.tests.csproj:77`;
- derived registration (`06ac9aa2`);
- the staged-source rule (`tools/anvil/CheckStagedTests.cs`, published by S2 in `25df3cd5`);
- the pinned toolchain under `$HOME/.local/share/tim-toolchain`.

One blocking finding (B-1, the frontmatter baseline; its evidence and fix are under N1) and three non-blocking findings (N2–N4) can be applied by the coordinator before dispatch. None touches `<intent-contract>`.

## Third-audit blocking findings

| Finding | Status | Where it is resolved, and how |
|---|---|---|
| B1: the unpublished planning work is not named as a prerequisite | Resolved | `:213`. The planning work of 22–23 November (readiness folder, `delivery-batches.md` sections 0 and 5, `named-elements.md`, `requirements.md`, `vertical-delivery.md`, `sprint-status.yaml`, `deferred-work.md`) publishes first, and the baseline is re-captured at that revision. Task 7 edits the S3 section as published (keeping the **When.** line). This is repeated in Design Notes 7 (`:172`) and in the Code Map status (`:49`). |
| B2: the files shared with S5 are omitted | Resolved | `:215` names `CSharpStaticGateTests.cs` (S5 edits other members of the same class) and `docs/delivery-workflow.md` (S5 edits `:34`), with one publisher at a time and a merge-and-repeat rule. S5's own statement agrees (`spec-s5-generate-wire-accessors-in-build.md:135`). |
| B3: the whitespace check skips new files | Resolved | `:203` runs `git add -N` on the contract and the run log before `git diff --check <baseline_revision>`. |
| B4: payload-pin protection recorded as covered | Resolved | `:133` now reads "payload membership: **residual** (R5 below)". `:139` adds R5, owned by S4, with the reason `PartRegistryTests.cs:51–53` is not independent. Task 7 (`:151`) records R2–R5 in `deferred-work.md`. |

Non-blocking N1–N6 of the third audit are applied:

| Finding | Where it is applied |
|---|---|
| N1 | Palette literal "691–698 (asserted at 700)" at `:59`, `:87`, `:150` |
| N2 | D5 at `:119` |
| N3 | `TrimAnalyzedProjects(IEnumerable<string> files)` at `:145`; the M1 fact builds its table through it at `:185` |
| N4 | D3 at `:118` |
| N5 | Capability-discovery rule for the re-identified `ramp` at `:187` |
| N6 | Environment at `:178` |

## What was re-verified and holds

- **Citations at `25df3cd5`.**
  - `CSharpStaticGateTests.cs`:
    - `Files` 199 and `RepositoryFiles()` 201–206 (`git ls-files --cached --others --exclude-standard` in a checkout; `Walk(Root)` in the hook snapshot, skipping `bin`/`obj`/`.godot`/`node_modules`/`AppBundle`/`TestResults`);
    - `PackageAnalyzers` 558–566 (ILLink rows 562–563 take `TrimLinkedCompiles()`);
    - `TrimLinkedCompiles()` 612–613, `RequiredAnalyzerSwitches` 660–668;
    - `TrimAnalyzedProjects()` 670–705 (eleven fixed entries at 672–680, 45 package entries at 681–704, Baseball at 701);
    - the single `GeneratedConfigViolations` caller at 792 and its body 806–828 (the absent key yields `build_property.EnableTrimAnalyzer =  (must be true here)` with two spaces).
    - `TrimAnalyzedProjects` has exactly one caller (`:613`); `TrimLinkedCompiles` is called only from the two field initialisers. Both are initialised after `Files` (199), so passing `Files` is safe.
  - `PackageRuntimeParityTests.cs`:
    - footprint test 84–100;
    - name pins 149, 177, 223;
    - slider rows 383–399 and count pin 400;
    - `Parameters(id)` 468–638 and the Windmill branch 291;
    - `Placed` 641–662 and 665–673.
  - Elsewhere:
    - `F32BehaviourIdentityTests.cs:36–39`, `:71`;
    - `BasketballResourceTests.cs:32–35`;
    - `WorkshopIntegrationHarnessTests.cs:691–700`;
    - `EventRingProducerTests.cs:780`;
    - `WorkshopConstruction.cs:31–39`;
    - `InstanceParameters.cs:34`, `:41`;
    - `DeclaredPhysics.cs:41`, `:146–148` (`InvalidOperationException`);
    - `BundleElementIdTests.cs:111–131` (the re-identification pattern the M3/M4 facts copy);
    - `BundleElements.csproj:26` (`elements/*/*.csproj`, the same three-segment rule as `IsElementProject`).
- **Placement derivation.** Every `PlacedParts` helper the first switch uses is `WorkshopInput.Generic` at (0, 3, 0) with the declared defaults:
  - Battery 3600/120/1/Enabled (`BatteryElementDeclaration.cs:42–48`);
  - Plate 0.5 (`PressurePlateElementDeclaration.cs:39`);
  - Counter 3 (`CounterElementDeclaration.cs:53`);
  - Clock 1 s (`SystemClockElementDeclaration.cs:32`);
  - Latch and Domino with none.

  `PlacedParts.Springboard` is the generic instance carrying the three given values (`PlacedParts.cs:34–37`). The two `Placed` switches are therefore replaceable by one generic placement with no behaviour change, as D3 says. `SaveFixture.Elements` is already derived from the bundle (`SaveFixture.cs:13`).
- **No re-derivation of earlier slices.** `06ac9aa2` derived registry, palette, save, port/footprint, lifecycle, capacity and E2eRunner membership (`docs/delivery-workflow.md:64`). `25df3cd5` derived the test-project compile list. S3's five lists are none of those. Each was still hand-edited by the Baseball slice (`git show fa86969f`: `CSharpStaticGateTests.cs` +1, `PackageRuntimeParityTests.cs` two lines, `WorkshopIntegrationHarnessTests.cs` +1, `BasketballResourceTests.cs`).
- **Direct consumers.** I ran an independent count of how many of the 45 identities each tracked `*.cs`/`*.ts`/`*.json` file names (outside `elements/` and `_bmad*`):
  - 45 of 45 only in the three files the contract derives or deletes (`WorkshopIntegrationHarnessTests.cs`, `PackageRuntimeParityTests.cs`, `BasketballResourceTests.cs`; the static gate names paths, not ids).
  - 44 of 45 in `tools/Coverage/Capability*Requirements*.cs` and historical `docs/coverage/*.json`, which are keyed by planned identities and were not edited by the Baseball slice.
  - 33 of 45 in `VisibleSolidColliderTests.cs` (S4 and the repairs).

  `FanGuidanceTests` gained Baseball rows in `fa86969f`, but that is a behaviour theory, not a membership list. `TrimAnalyzedProjects`, the two `No placement` messages and the renamed test name appear nowhere else in the tree (outside `_bmad-output`): no hook, CI workflow, `VerifyPolicy.cs`, coverage map or `no-skipped-tests.json` names them. No consumer is missed.
- **Removed protections against the commit path.**
  - A package project that only exists untracked is in the working-tree file list but not the hook snapshot (`.githooks/pre-commit:23`). Its `.cs` files are untracked too, so `CheckStagedTests.cs` refuses the commit whenever it stages source (`CheckStagedTests.cs:57–61`). A `.csproj` is a source-staging extension there (`docs/delivery-workflow.md:64`). An intent-to-add `.cs` is refused on every commit.
  - So D2's "no new divergence" holds, and the derivation is no weaker than `TheSolutionListsEveryCSharpProject`, which reads the same list.
  - The 45 trim entries are replaced by the same checks over path-discovered projects. A derivation that dropped element projects would fail `EveryLoggedCompileReceivedThePinnedCompilerInputs` as an unpinned ILLink analyzer (`:766`).
  - The removed vacuous loop is covered by the theory at `F32BehaviourIdentityTests.cs:66–78`, which places every element through `Placed`.
- **Deltas.** New tests: +5 (theory) +1 +1 +1 = +8 in the full round. All of them are in `CSharpStaticGateTests` or `PackageRuntimeParityTests`, which are both in the registration filter (`docs/delivery-workflow.md:67`), so the registration check is also +8. The hook selects `CSharpStaticGateTests|WorkshopUiFailureTests` (`.githooks/pre-commit:53`), so the hook is +6. The contract measures each at the slice's re-captured baseline, against main's 7,294 at drafting (`:157`, `:204`). Nothing is deleted and one test is renamed.
- **Tier and shape.** Engine tier, single structural slice (`:47`): the static gate and shared test builders are off the data-only allow-list. AC4's paths match the owned scope. `.tools/s3-evidence/` is git-ignored (`.gitignore:6`), so raw evidence stays out of the diff.
- **Verification order** (`:197–205`) follows the handoff order: compiler-log builds, maintainability gate, policy harness (not required, since `tools/anvil/` is untouched, but harmless), registration check, affected tests, `git add -N` plus `diff --check`, the full round under `caffeinate -i`, the hook at commit.
- **Serialisation** (`:211–215`).
  - It publishes after the planning work (coordinator 37) and one at a time with S5 (coordinator 37).
  - It is allowed before the programme (owner 22, 24h).
  - The Baseball repair cites `PackageRuntimeParityTests.cs:149/:653`, `BasketballResourceTests.cs:34`, `WorkshopIntegrationHarnessTests.cs:694` and `CSharpStaticGateTests.cs:701` as read-only (`spec-repair-baseball-el-239.md:108`). Parts A, B, B2 and C only run `PackageRuntimeParityTests` in their filters.
  - S6 shares only `docs/delivery-workflow.md` by reference, and `TODO.md`/`deferred-work.md`.
  - The test project file, `content/puzzles.json`, coverage and impact tables are untouched. The run-beside claim is correct.
- **Budget.** 4,247 words, seven matrix rows, one coherent slice. Owner answers O1 and O2 are recorded (`:122–125`). D1–D5 are defensible.

## Blocking findings

### B-1. The frontmatter pins a baseline that the contract says will be replaced (wrong specified behaviour, coherence)

This was graded non-blocking as N1 in this audit's first draft. The coordinator, after reading all four fourth audits, graded it blocking. The reason is that the stated consequence (AC4 fails on a correct candidate) is a predictable acceptance failure, and the Baseball-repair and S5 fourth audits grade the same defect blocking. The evidence and the fix are unchanged; they are under N1 below. With B-1 applied, the contract is a Scoped Pass (ready-for-dev), with no further re-audit needed.

## Non-blocking findings

### N1 (blocking: B-1's evidence and fix). The frontmatter pins a baseline that the contract says will be replaced (wrong specified behaviour, coherence)

- **Where.** `:6` (`baseline_revision: '25df3cd5…'`) against `:47` ("the coordinator confirms or re-captures it") and `:213` ("re-captures the baseline as that revision").
- **Consequence.** The planning work modifies `AGENTS.md` and `DESIGN.md` (both modified in this checkout). If the frontmatter value survives, `git diff --check <baseline_revision>` and AC4's path list are taken against `25df3cd5`, AC4 then fails on those files, and the protocol forbids overwriting a captured baseline. The drafting brief also says not to set this field.
- **Fix.** Delete the `baseline_revision:` line from the frontmatter. Change `:47`'s "Baseline: published main `25df3cd5` (drafting revision; the coordinator confirms or re-captures it at start, before first implementation)" to "Baseline: captured by the coordinator at start as the published planning revision (Running beside other work); drafted and line-checked on `25df3cd5`".

### N2. The M4 matrix names an out-of-range refusal that no planned control exercises (test)

- **Where.** `:38` (M4 error handling: "A missing or out-of-range value is refused") against `:188`, which plans only the missing-key and undeclared-identity controls.
- **Fix.** Append to the M4 row's controls at `:188`: "the same call with one value just above its declared maximum (`stiffness` 1200 → the next f32 above) throws `ArgumentException` ("Invalid declared parameter value.", `engine/gpu/InstanceParameters.cs:47`)". Add +0 to the counts: it is an assertion inside the same fact.

### N3. The M6 derivation's handling of an empty category is unstated (test)

- **Where.** Task 6, `:150`: "for each `ElementCategory` in ascending value its literal heading … a category with no package is not expected today".
- **Evidence.** Production emits a heading only for categories that have parts (`ui/Workshop.cs:419`, `GroupBy(... .Category).OrderBy(group => group.Key)`). Coordinator decision 36 adds Optics and Radiation categories through their engine stories, possibly before their first package.
- **Fix.** Replace "for each `ElementCategory` in ascending value its literal heading" with "for each `ElementCategory` that at least one discovered package declares, in ascending value, its literal heading". Delete "a category with no package is not expected today".

### N4. A planning-document conflict with owner answer O1, outside this contract (wrong specified behaviour, planning documents)

- **Where.** `docs/planning/elements/readiness/engine-design-gap.md:79` (E14, "Hand-maintained package list `CuriousContraptions.slnx:16-59` — Derive from discovery"), assigned to story 8 by `engine-stories-index.md:120`. This contradicts O1 at `:124` ("Keep one guarded solution line per package … No second S3 slice follows") and D1's measured MSB4014 result (`:113`).
- **Fix (coordinator, planning documents, not S3's owned scope).** In `engine-stories-index.md:120` change "E1 to E9, E11 to E14" to "E1 to E9, E11 to E13", and append "E14 → closed by S3 owner answer O1 (21 November 2026): one guarded solution line per package; `.slnx` has no wildcard". In `engine-design-gap.md:79`, add "Closed: S3 O1" to the E14 row.

## Decisions that need the owner

None. O1 and O2 are recorded owner answers of 21 November 2026, and D1–D5 follow from the game's intent and the code. N4 is coordinator record-keeping.

## Evidence limits

- Contract text is from the 21:33 snapshot named above. Later edits to the live file (the `.tools/audit/` citation re-pointing) were not re-read.
- Nothing was built or run. Not executed here:
  - the equality of the derived palette rows with the literal against the baseline's generated bundle (task 6);
  - the compile of a re-identified, parameter-bound `ramp` under the scoped seams (the M3 row states the return-to-planning rule);
  - the claim that an intent-to-add `.csproj` is skipped by `git checkout-index --all`.
- O1 and O2 are recorded in this contract and reflected in element specs ("one guarded line in `CuriousContraptions.slnx`", for example `TH-02-fire-bowl.md:164`). I found no separate owner record on disk.
- The earlier Scoped Pass (auditor `aa26de8e9f5eea107`) has no report on disk (completion ledger DC-55) and is not relied on.
