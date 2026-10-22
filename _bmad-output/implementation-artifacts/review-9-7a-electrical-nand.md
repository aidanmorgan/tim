# Independent review: 9.7a Electrical NAND Gate (CAT-024)

## Pre-merge review — 22 October 2026

The reviewer is independent: did not author or edit any source, test, tool or content file. The candidate is uncommitted on the `story-9-7a` worktree, base `9b324bbd6a161858b9342562cd0a572d67f9e62a`. The candidate identity manifest is the tracked diff plus the SHA-256 of each untracked file, excluding review evidence: `evidence-9-7a-review/candidate-identity-manifest.txt`, SHA-256 `4ba3858ab427ffd7fe54748e3a2ca4554035ea47b3b58d852904372e8692e1fd`. The same hash was taken before and after the review, so the candidate is byte-identical. Tracked patch `candidate.patch`: SHA-256 `0a8c6f7a46fe512580a7d9802690f3d24dd8384a18b5d178878d200b58b2743c`, 21 files, +188/−43, plus 7 untracked files. Mutation probes ran in a throwaway `git worktree` (base + `candidate.patch` + copied untracked files). That worktree was verified identical to the candidate after reverting each probe and was then removed.

This is not a SnapshotApproval. Approval waits for the corrective-slice merge from main and the tool-membership update (PM1/PM2 below).

### Criterion / impact matrix

| Criterion / impact | Expected outcome (derived) | Evidence | Disposition |
| --- | --- | --- | --- |
| Truth table via real power switches (Story 9.7a AC1, CAT-024 §5 Positive/Negative) | The real Powered Gate blade opens for 00/01/10 when Battery → ImpactSwitch contacts are the conditions and Battery 7 supplies the bottom socket; the blade does not move for 11. | `SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer` 8 rows, plus blade displacement > .2 m or < .01 m (`native-focused-named.log`) | Pass |
| No supply → nothing powered; conditions never lend energy (AC2, D5, todo-362) | Rows 00/01/10/11 with the supply disabled: Supplied=false, Active=false, consumer unpowered. A true row with the supply wire missing never powers. | Same theory supply=false rows; `TrueRowWithoutSupplyWireNeverCreatesPower`. Probe M8 (Nand active without PowerIn) → 10 tests red | Pass, discriminated |
| A3 latch-then-supply method | It must not hide transition defects. Dynamic transitions are proved separately. | `ClosingBothSwitches…` (00 → 11 while supplied, the blade really opens then closes); `CommittedReadsFollowEveryRow…` (00 → 10 → 11 → no supply). `Resolve` allocates `powered`/`contacts` on each call (SupplyRoute.cs:124–125), so 11→10 cannot latch | Pass, method accepted |
| D2 ordered Resolve: SCC condensation | Tarjan over predecessor lists emits components upstream-first. Singletons are evaluated once; monotone cycles iterate to the least fixed point. | Code review of `SupplyCycles.Components` (iterative, parent low-link via the stack top, correct emit order). M1b (old slot sweep restored) → `PlacementAndWiringOrder…` and `ReconvergentCondition…(gateFirst:True)` red. M1 (reverse order) → 22 red | Pass, discriminated |
| Least fixed point in monotone cycles; 9.6 regression | 9.6 Both Gate cycle/reconvergence and 9.5b contact cycles unchanged | BothGateTests 18/18 (+1 presentation) and SwitchElectricalTests 13/13 pass. M2 (single pass inside a cycle) → `BothGateTests.ChainedReconvergence…(cycle:True,reverse:True)` and `SwitchElectricalTests.TwoContactChains…(cycle:True,reverse:True)` red | Pass, discriminated (by 9.6/9.5b reverse variants only; see I1) |
| Placement, link order and same-tick retraction | Identical committed rows, power and blade across gate first/last × links forward/reversed. Reconvergent retraction happens on the first enable tick. | `PlacementAndWiringOrder…`; `ReconvergentConditionRetractsOutputOnTheSameTick` ×2 | Pass |
| D1 NonmonotoneFeedback: four loop shapes, ids named, wire-order independent | A typed `NonmonotoneFeedback` naming the SCC ids, ≤ 100 chars, for NAND→NAND, →Both Gate→, an open ImpactSwitch, and PowerIn entry | `ZeroDelayFeedback…` ×4. M3 (PowerIn edges ignored) → EnteringPowerIn and ThroughOpenSwitch red. M4 (contact-sourced edges ignored) → ThroughOpenSwitch red (native and chooser). M5 (no refusal) → 6 red. M6 (Nand declared monotone) → 8 red | Pass, discriminated |
| Refusal surface: before Undo, at save load, at Construct | Chooser: status line, no command, no Undo, construction unchanged. Save: `InvalidConnection` with inner `NonmonotoneFeedback`. `WorkshopSimulation.Construct`: Rejected/InvalidConstruction, construction kept. | `NotBothGateChooserRefusesALoopClosingWireOnTheStatusLine` ×2; the save/Construct asserts inside `ZeroDelayFeedback…`. Call-site trace: `WorkshopConstruction.Validate` (also run by `WorkshopPhysicsCompiler.Compile`) and `WorkshopSavePrevalidation:209` are the only callers. The edit-path `partOf(owner)!` is safe because earlier loop rejects null endpoints. | Pass |
| Compiler invariant only as invariant; no untyped player path | Constructing `CompiledSupplyGraph` with a nonmonotone cycle throws `ArgumentException`. Every player path validates first. | `CompiledGraphRejectsNonmonotoneCycleAsAnInvariant`. The construction/compile/save paths above all call `Connections.Validate` before any graph is built. | Pass |
| Monotone control | A source-free Both Gate loop is admitted and stays unpowered; the NAND it feeds settles after it | `MonotoneLoopIsAdmitted…` | Pass |
| G5/D9 scope of refusal (any socket) | "Any cycle containing … Nand rejects", so a PowerIn loop is included | CAT-013 G5, CAT-024 D7–D9, requirements.md:598 read directly | Matches the source |
| Closed sets / undefined rejection (D3, AGENTS enum rule) | Nand=1 is appended. `Evaluate`/`IsMonotone` are exhaustive switches; `(LogicGateOperation)2` and 255 throw. Socket 255 and the activation link are refused. The bundle serializes `"Operation": "Nand"` by name. | `LawMonotonicityAndUndefinedValuesAreClosed`; BundleValidatorTests (Nand admitted, 2/255 rejected). M7 (Nand→Nor law) → 5 red | Pass |
| Committed reads, lamps, neutral admission | Each row is committed, tick 0 is neutral, and an And-shaped row is refused at admission. Native lamps go slate/gold per row and return to slate through Reset. | `CommittedReadsFollowEveryRowAndAdmissionIsNeutral`; `PictogramTruthDotsAndThreeLampsFollowCommittedRowsThroughReset` (M7 turns both red) | Pass |
| Run/Reset (9R.1) and Save/Load typed sockets (AC3) | Reset state and presentation bits are exact; save bytes round-trip with the connections equal; saved replay is identical | `ClosingBothSwitchesRetractsTheOutputAndSaveLoadAndResetAreExact` | Pass |
| A1 pictogram / art (G13, DESIGN "Supplied electrical logic") | A navy pictogram matching the toolbox icon; gold `#e8b764` dots r .035 at x −.21/−.07/.07, y −.4, z .42 | The `icon.svg` path is byte-equal to `WorkshopIcons.cs@a6c914e:L79`. 13 navy (.161,.224,.329) strokes span x −.29….34 (≈ .65 m) at z .3875–.4125, in front of the face front at .37 and clear of the lamps and marks. Dot values verified in the scene and the native test. | Accepted deviation from "unshaded quad" (L2) |
| Package isolation / no identity branch | Declaration, art and icon only; no record type, PartId branch or cross-package reference | `static-scope-checks.log`: no `electrical_nand`, `"Nand"` or "Not both" in engine/Simulation/ui/Bundle/Abstractions; schemas, PartId, compiler, BothGate, ui and Simulation unchanged. No `res://elements/BothGate` reference. Godot UIDs `prdoguy3it27k` and `q8o27gu2yygj` are unique in the repo; the import hash equals md5 of the source path. | Pass |
| 9A.45a ratchet, isolation Known | No new entry | `KnownViolationsSha256` unchanged; full native pass includes the frozen-list and isolation tests | Pass |
| A8 solver-scan allow-list | Only exact, pre-existing shared tokens are exempted; element-specific names are still caught | The six tokens exist only as shared electrical source/binding layout and allocator names in `WorkshopPhysicsEngine.cs`, which this diff does not change. None of their sub-words (supply/enable/sources/bindings/offset) is a catalogue word. M9 inserted `electricalNandRow`, `nandTable`, `ElectricalSupplyNand` and `electricalOrRow` → all four flagged. The exemption is exact-token, so adding "electrical" to the scan makes it stricter. | Not a gate relaxation |
| Membership/count pins | Each pin is base +1: registry 25, SaveFixture 25, ids+undelivered 27, palette rows, capacity, parity, ports golden; ordinal 27 unique | Diff inspection, bundle `PaletteOrder` values unique, full native pass. Parity: excluded only from the frozen 9A.16 golden, like Hold/Both Gate. The per-package footprint/collider/material/parameter parity loops still include it. | Pass (re-pin after merge, PM1) |
| Worker / AOT | Release worker builds clean; AOT guard passes | `worker-release-noincremental.log` 0 W/0 E; WorkerAotGuard 17/17 | Pass |
| Tool membership (A5) | ImpactRules gains ElectricalNand; Coverage example → `electrical_nor` (CAT-025 exists) | Coverage 73/77 and E2eRunner 187/189 fail on the same named sets as base `9b324bbd`. The reviewer reproduced this independently in a fresh base worktree (`base-*.log`): BundleElements 140/141 (PortLocator), Coverage 73/77 (same four), E2eRunner 186/189 (same two plus `TypeCheckTests`, which is an environment artifact: no `node_modules` in the fresh worktree, so flatbuffers types are missing). | Unverified until merge (PM1) |

### Commands and results (raw under `evidence-9-7a-review/`)

| Command | Result |
| --- | --- |
| `dotnet build CuriousContraptions.slnx -c Debug -v:q` | 0 warnings, 0 errors, 6.3 s (`build-debug.log`) |
| `dotnet test CuriousContraptions.tests -c Debug --no-build --no-restore -v:minimal` | **2166/2166 passed**, 38 s (`native-full.log`) |
| same, `--filter` ElectricalNand / BothGate / NotBothGateChooser / SwitchElectrical / solver scan, normal verbosity | 73/73 passed, every test named (`native-focused-named.log`) |
| `dotnet test tools/BundleElements.Tests …` | 142/143. The only failure is `PortLocatorTests.AnUndefinedSocketIsRefused`; it is pre-existing and its file is untouched |
| `dotnet test tools/WorkerAotGuard.Tests …` | 17/17 |
| `dotnet test tools/Coverage.Tests …` | 73/77. Four `KeyNotFoundException` failures in `CurrentCatalogue.ValidateFixtures`, the same as base |
| `dotnet test tools/E2eRunner.Tests …` | 187/189: `ElementPackagesMatchDirectoriesAndBundle` (BothGate first missing) and `PartPreplacedByADeclaredLevel…`, the same as base |
| `dotnet build CuriousContraptions.Simulation -c Release --no-incremental -v:q` | 0 W / 0 E, 4.3 s |
| Mutation probes, throwaway worktree: rebuild, then the filtered native classes above (`mutations/M*.{mutation.diff.vs-base,build.log,test.log}`) | M0 control 152/152. M1 reverse order: 22 red. M1b old slot sweep: 2 red. M2 no cycle iteration: 2 red. M3 PowerIn ignored: 3 red. M4 contact edges ignored: 2 red. M5 no refusal: 6 red. M6 Nand monotone: 8 red. M7 Nand→Nor: 5 red. M8 no-supply Nand: 10 red. M9 nand/electrical names in solver: scan red, 4 hits |
| Identity before and after | manifest SHA-256 identical (`4ba3858a…`) |

### Findings

Pre-merge conditions. These block SnapshotApproval; they are not candidate defects.

- **PM1 — tool membership and pre-existing failures.**
  - *Criterion:* the scoped tool suites pass for the exact snapshot; membership is consistent.
  - *Risk:* the Coverage `electrical_nor` flip is unexecuted, because all four affected tests stop at `ValidateFixtures`. `ImpactRules.ElementPackages` still lacks BothGate and HoldTimer, and the corrective slice will probably edit the same line. PortLocator still pins `(PortSocket)5`.
  - *Why blocking:* a known-failing affected suite leaves A5 Incomplete.
  - *Smallest resolving check:* after merging main's corrective slice, resolve the `ImpactRules` line, do one rebuild, then run Coverage.Tests, E2eRunner.Tests and BundleElements.Tests fully green. Re-run the full native project for counts 25/27, ordinal 27 uniqueness and ports/parity.
- **PM2 — exact snapshot identity.**
  - *Criterion:* SnapshotApproval binds an exact tree.
  - *Risk:* the candidate is uncommitted, and the merge changes the inputs.
  - *Smallest check:* stage the merged candidate, record the tree and diff identity, and run a focused rerun of the ElectricalNand, BothGate, SwitchElectrical and chooser classes if the merge touches `SupplyRoute`, `WorkshopConnections` or electrical tests. Otherwise reuse this proof by identity. Also move the implementer's raw evidence out of the ephemeral `/private/tmp/…/scratchpad/9-7a/` and under `_bmad-output/implementation-artifacts/` before approval. That covers the red-before-green log, `green*.log`, the base logs and `anvil-check.log`, since the proof must remain durable.

Non-blocking:

- **L1 — G6 message wording.** The text is generic: "This gate's output cannot feed its own inputs." The legacy text named "XOR, NOR and NAND outputs". The "Break the wire loop through parts …" content is carried, and A7 justifies keeping it future-proof. Messages are unbounded for very large SCCs (> 100 chars) and rely on status-line wrap. Accept.
- **L2 — A1 pictogram.** The pictogram is shaded navy mesh strokes rather than G13's unshaded textured quad. The geometry and colour match the icon path, and DESIGN's requirement ("original navy operation pictogram … matching its toolbox icon") is met in substance. Readability at projected size and under lighting remains a queued browser case. Accept, pending post-catalogue E2E.
- **L3 — energy provenance.** There is no NAND-specific `UpstreamSources` assertion. It is covered structurally: `UpstreamSources` is unchanged and operation-agnostic (PowerIn only), and 9.6 `EnergyProvenanceIgnoresBothConditionSourcesAndUndefinedRulesReject` passes. Behavioural no-lend proof is above.
- **L4 — TODO.md current brief.** The candidate replaces base's C-Ramp+Receiver "In flight" and "Owner" lines with 9.7a. Main has since recorded C-Ramp's terminal Pass and an in-flight corrective forward fix. Reconcile at merge so the brief keeps every live outcome and its owner (AGENTS current-brief rule).
- **D1 — implementer process deviations.** The spec discloses writes that skipped Anvil pre-validation and three sed/perl edits. The reviewer independently ran `anvil check --no-tui` on all 29 candidate files: "No warnings found" (`anvil-check-candidate.log`). Content-level clearance is independent, so this is non-blocking.
- **I1 — fixed-point discrimination.** Least-fixed-point iteration is discriminated only by the reverse-order variants of the 9.6/9.5b suites, not by any test in `ElectricalNandTests`. Both suites run in the same native project, so this is adequate.
- **I2 — Story 9.7 Or chaining.** Or chaining (D6/D7) is substituted with Both Gate per spec D6. Or chaining stays pending under 9.8a, as the spec records.

No unresolved scoped unintended regression found in the candidate.

### Provisional verdict

**Pass-pending-merge.** All required-now 9.7a native criteria pass on the real C# simulation, and every key law, ordering, refusal and scan criterion is mutation-discriminated. SnapshotApproval is withheld until PM1 and PM2 are closed on the merged exact snapshot. This is not browser or release qualification: `tools/e2e/cat-024.test.ts` remains queued for post-catalogue E2E.

## Post-merge re-review — 22 October 2026

Same independent reviewer, with no source, test, tool or content edits. The raw output is in `evidence-9-7a-review/post-merge/`.

The branch is now: `1101069c` WIP (the reviewed candidate), then merge `411425ea` (parents `1101069c7798eb03881378ea10ee7387f68adb6c` and main `59ecb70b8d161045d08c1f3de802cdad89e23eef`), then an uncommitted 6-file delta (PM1, PM2 and L1).

| Check | Command / method | Result |
| --- | --- | --- |
| WIP commit = reviewed candidate | `git diff 9b324bbd 1101069c` for the reviewed paths, compared with `candidate.patch`; `git show 1101069c:<f>` SHA-256 for the 8 untracked files | **Pass.** Tracked diff byte-equal (`cmp`). All 8 hashes match `candidate-identity-manifest.txt`. |
| Merge `411425ea` loses or alters nothing | Every file changed `9b324bbd..411425ea` was classified as main-only (49), NAND-only (24) or both (5). Then `git diff --quiet 59ecb70b 411425ea` on the main-only files, `git diff --quiet 1101069c 411425ea` on the NAND-only files, and a two-sided diff of each both-touched file. | **Pass.** No file outside main ∪ NAND. Main-only files equal 59ecb70b and NAND-only files equal 1101069c. In the both-touched files, each side's diff to the merge is exactly the other side's change: `PalettePlacementTests` (NAND chooser test + main's ramp editor test), `CurrentCapabilityInputsTests` (NAND `electrical_nor` line + main's 1472/303 counts), `ImpactRules` (main's BothGate/HoldTimer + ElectricalNand), `sprint-status.yaml` (main's done rows + 9.7a review), `TODO.md` (main's brief + the 9.7a In flight/Owner lines, closing L4). |
| Uncommitted delta: PM1 | Diff inspection | **Pass.** The declaration now uses named `ElementMetadata` arguments: values unchanged, and all 25 package declarations use `Id: "…"`, the convention that the Coverage reader `CurrentCatalogue` (regex `\bId: "…"`) relies on. Admitted count 24→25, plus an explicit `electrical_nand` → declaration-path assertion. The planned-absent directory is now `elements/ElectricalNor`. No production engine change beyond L1. |
| Uncommitted delta: L1 | Diff; `LongLoopRefusalNamesTheFirstPartsAndCountsTheRest` (3/4/40 Both Gates, i.e. 4/5/41-member loops) | **Pass.** The message names the first 4 ascending ids plus " and N more" and asserts the exact text. The 120-char budget is justified from the 480 px, 12 px, two-line, word-wrapped label. The original 4 loop cases are kept. |
| Uncommitted delta: PM2 | `evidence-9-7a/{pre-merge,post-merge}` listing; spec Evidence links | **Pass.** The implementer's red/green, base, anvil and post-merge logs are durable in the repo. No scratch files. |
| One rebuild | `dotnet build CuriousContraptions.slnx -c Debug -v:q` (`build.log`, HEAD 411425ea + delta) | 0 W / 0 E |
| Full solution | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore -v:minimal` (`test-slnx.log`) | native **2173/2173**, Coverage **77/77**, BundleElements **143/143**, E2eRunner **189/189**, WorkerAotGuard **17/17** |
| Exact CI step "Test C# tools" | `pages.yml` lines 51–64 run verbatim with `bash -e` (`ci-tool-step.log`) | Coverage 77/77, BundleElements 143/143, WorkerAotGuard 17/17, E2eRunner 174/174 (toolchain-filtered); exit 0 |
| Focused NAND (named) | `--filter ElectricalNand|NotBothGateChooser`, normal verbosity (`native-nand-named.log`) | 27/27, including the 3 long-loop cases |
| Worker | `dotnet build CuriousContraptions.Simulation -c Release --no-incremental` | 0 W / 0 E |
| Pins / ordinal | bundle `PaletteOrder` | 25 entries, 0 duplicates; NAND 27. Native count pins pass inside 2173. |
| Content / whitespace | `anvil check --no-tui` on the 6 delta files; `git diff --check` | No warnings; clean |

Pre-merge findings, closed:

- **PM1:** closed. Tool suites and membership are green on the merged snapshot.
- **PM2:** closed. Evidence is durable, and the exact identity is recorded below.
- **L1:** closed.
- **L4:** closed. The TODO brief keeps main's C-Ramp corrective In-flight line and adds 9.7a.

Still open:

- **L2, L3, I1 and I2** stand as non-blocking, as recorded above.
- **I3:** package binding in the Coverage reader depends on the `Id:` named-argument source form. This is main's established convention, uniform across 25 packages. Info only.

Verdict: **Pass** for required-now scoped native/tool criteria on the merged candidate. SnapshotApproval follows.

## SnapshotApproval — 22 October 2026

**Approved snapshot**

- Staged tree: `3c2ea40a56b6f958220b4e27613f0aadaecf0c41`.
- SHA-256 of `git diff --cached --binary 411425ea`: `a973a24f1d105571af3e977f832eafc87733ff20f164c997d4c425e2523a3a9d`. The diff has 92 files.
- What is staged:
  - the 6-file slice delta (PM1, PM2, L1);
  - `evidence-9-7a/` (the implementer's pre-merge and post-merge evidence);
  - `evidence-9-7a-review/` (reviewer evidence);
  - this record, up to the end of the post-merge re-review.
- No scratch files are staged. Nothing staged is outside those paths.

**Parent chain**

- Merge `411425ea9fad3a78592dad34e611986bd9507d3f` has two parents:
  - WIP `1101069c7798eb03881378ea10ee7387f68adb6c`, whose parent is base `9b324bbd6a161858b9342562cd0a572d67f9e62a`;
  - main `59ecb70b8d161045d08c1f3de802cdad89e23eef`, whose parents are `9b324bbd` and `0b3607f5cdbe1c0739f690359c296f1b4fc0c1bc`.

**Staged whitespace**

- `git diff --cached --check` reports trailing whitespace only in the reviewer's raw `git diff` evidence: `candidate.patch`, `candidate-identity-manifest.txt` and `mutations/*.mutation.diff.vs-base`. In those files `+ ` is a diff context marker, and the files are kept byte-for-byte as raw output.
- Every other staged file is clean.
- The normal hook gate does not check whitespace.

**Exclusions** (scoped approval does not cover these)

- browser/Chrome qualification (cat-024 queued post-catalogue);
- Or chaining (9.8a);
- release/device/performance gates;
- common 9A.44 numerical conversion.

## Publication-dependent checks

Publication is:

1. commit the approved snapshot plus this SnapshotApproval section on `story-9-7a`, with normal hooks and no `--no-verify`;
2. `git merge --no-ff` into main;
3. push.

**Before pushing to main**, both of these must hold:

- **C-Ramp corrective has published.** TODO "Next" makes NAND publication wait for the C-Ramp+Receiver corrective forward fix. Its exact-SHA CI run `39704720283` for main `59ecb70b` was `in_progress` at approval. Confirm it succeeds and its terminal checks pass first.
- **Main has not moved.** If main has moved past `59ecb70b`, the new merge is a changed input: return it for an affected re-review before pushing.

**Terminal checks** (independent reviewer, after push):

- **Tree equivalence.** The published tree differs from `3c2ea40a` only by this record's SnapshotApproval and Publication-dependent sections, with the main-side merge parent `59ecb70b`. Confirm remote identity with `git fetch` and `git rev-parse origin/main`.
- **Normal hooks.** They succeed on the commit and the merge. If no hook log is kept, re-run the hook on the exact published tree.
- **Exact-SHA CI** "Build and deploy game" for the pushed main SHA succeeds, including:
  - the static gate;
  - "Test C# game" (native count ≥ 2173, no failures);
  - the **"Test C# tools"** step: Coverage 77, BundleElements 143, WorkerAotGuard 17, E2eRunner 174 filtered;
  - the production bundle publish;
  - the static deployment check.
- **Artifact.** The `github-pages` artifact for that run/SHA exists, has not expired, and its head_sha matches.
- **Deployment.** The github-pages deployment of that SHA reaches the **success** status and consumes that artifact.

Preserve any failed attempt unchanged and return concrete failures to the same implementation owner. Terminal scoped Pass is native/publication only, not browser or release qualification. No routine Chrome run or duplicate local campaign is required for publication.

## Terminal publication verification — 22 October 2026

Same independent reviewer, with no source changed. Raw outputs are in `evidence-9-7a-review/terminal/`: `identity.txt`, `ci-runs.txt`, `build-job.log`, `deploy-job.log`, `pages.txt`, `hook-rerun.log` and `c-ramp-precondition.txt`. The GitHub run/job logs are the original evidence.

| Check | Command (summarised) | Result |
| --- | --- | --- |
| Remote identity | `git fetch origin`; `git rev-parse origin/main` | `b35be7f444a199c2faaa8958d66c210947aad346`; tree `a36465b9e8a7bc7c655680f33c86f38b4b73ce84` |
| Parent chain | `git log --format='%H %P %T'` | **Main side:** b35be7f4 ← `59ecb70b8d161045d08c1f3de802cdad89e23eef` (main, `--no-ff` first parent). **Slice:** `608e3353edf1055618bfc08b31621b931a549187` (tree a36465b9) ← merge `411425ea9fad3a78592dad34e611986bd9507d3f` ← WIP `1101069c7798eb03881378ea10ee7387f68adb6c` and `59ecb70b`. 1101069c was verified byte-equal to the pre-merge reviewed candidate. |
| Tree equivalence | `git diff --stat 3c2ea40a a36465b9`; `git diff --stat a36465b9 origin/main^{tree}` | The approved tree `3c2ea40a` differs from the published tree in 1 file, this record (+60 lines). Those lines are only the "SnapshotApproval" and "Publication-dependent checks" sections (the receipt). The published main tree is identical to slice tree a36465b9. |
| Publication precondition | `gh run view 39704720283`; C-Ramp deployment statuses | Main `59ecb70b` CI completed **success** at 01:30:39Z, and Pages deployment 7241421567 succeeded, both before the NAND run started at 01:32:45Z. Main had not moved: the first parent is 59ecb70b. |
| Exact-SHA CI | `gh run list --commit b35be7f4…`; `gh run view 39706295965 --json jobs` | The only run for that SHA: "Build and deploy game", push, main, attempt 1, **success**. Job build 119587007687 succeeded, with every step successful: checkout of `b35be7f4…`, the C# static gate, Test C# game, Test C# tools, the production bundle publish, the static deployment check and upload-pages-artifact. Job deploy 119591218207 succeeded. |
| Native and tools totals | build job log | **Test C# game:** `Passed! - Failed: 0, Passed: 2173, Total: 2173` CuriousContraptions.tests. **Test C# tools:** Coverage 77/77, BundleElements 143/143, WorkerAotGuard 17/17, E2eRunner 174/174. The static gate's 3 builds have 0 warnings and 0 errors. |
| Pages artifact | `gh api …/actions/runs/39706295965/artifacts`; upload log | `github-pages` id **12395965745**, 79552970 bytes, digest `sha256:89759cc93112222ee2322fea7d8c28eba9e10a54917bf91cb6c8c4d32ce4e5d4`. workflow_run 39706295965 with head_sha b35be7f4; not expired. The upload step logged the same id and size. |
| Deployment | `gh api …/deployments?sha=b35be7f4…`; `…/deployments/7241638673/statuses`; deploy job log | Deployment **7241638673**, github-pages, ref main, sha b35be7f4. Statuses went waiting → queued → in_progress → **success**, with environment_url https://aidanmorgan.github.io/tim/. deploy-pages used `"artifact_id": 12395965745` and logged "Reported success!". This is the latest github-pages deployment; the prior one is 7241421567 for 59ecb70b. |
| Normal hooks | Index tree a36465b9 with nothing staged; `sh .githooks/pre-commit` | rc 0 in 42 s. The gate builds passed and the staged-test check passed 124/124. The index tree was unchanged afterwards. The implementer reports the hook passed on 608e3353. The merge commit ran no hook, because only pre-commit is installed (as in 9.6). The re-run on the exact published tree independently confirms the hook passes on this tree. |

No failed attempt occurred. The published tree differs from the approved tree only by the receipt.

One note: the C-Ramp corrective forward fix's own terminal receipt is not yet on main. Its CI and deployment preconditions held. That receipt is owned by the C-Ramp reviewer and is not a 9.7a input.

No unresolved scoped unintended regression.

Verdict: **Pass.** This is a scoped native/publication terminal Pass for 9.7a Electrical NAND Gate (CAT-024) at `b35be7f444a199c2faaa8958d66c210947aad346`. It is not browser or release qualification: cat-024 Chrome/Playwright proof remains queued for post-catalogue E2E, and the SnapshotApproval exclusions stand. Carry this receipt into the next normal publication.
