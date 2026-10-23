# Independent review: 9.8a Electrical OR Gate (CAT-026)

## Pre-merge review — 22 October 2026

The reviewer is independent, did not implement this story, and made no edits to source, test, tool or content files.

- **Candidate:** uncommitted on `story-9-8a`. The base is `55ee05b96f949a7d93241b5ed0608b32db6bb8a0` (main with 9.7b NOR merged).
- **Identity manifest:** the tracked diff plus the SHA-256 of each untracked file, excluding this review's evidence. Saved as `evidence-9-8a-review/candidate-identity-manifest.txt`, SHA-256 `110f5742060a85124a6d866f879c10c578cae64d136c0187b0c12aef1a9616dd`, identical before and after this review.
- **Mutation probes** ran in a throwaway `git worktree` built from the base, the candidate's binary patch and the copied untracked files. After every revert, that worktree's diff was byte-equal to the candidate patch. It was then removed.

This is not a SnapshotApproval. The branch must first merge main after C-Bumper+Spring publishes (PM1/PM2).

### Criterion / impact matrix

| Criterion / impact | Expected (derived) | Evidence | Disposition |
| --- | --- | --- | --- |
| Truth table via real power switches (9.8a AC1/AC2, CAT-026 O1/O2/O5) | Battery 7 supplies; the conditions are real ImpactSwitch contacts; the consumer is a real Powered Gate. Rows 01/10/11 open the blade. Row 00 stays still on every tick. No row powers without supply. | `SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer` 8 rows, with per-tick `AssertBladeStill` on false rows. P1 (`Or => first`) turns the 01 truth, order, reads, lamps, law, contact-loop and Or-loop tests red. Implementer M1 (And) and M2 (Xor) cover rows 01/10 and 11. | Pass, discriminated |
| No supply → nothing (todo-362) | A true row (11) with the supply wire missing never powers, with control evidence that both conditions really hold | `TrueRowWithoutSupplyWireNeverCreatesPower`; implementer M4 (Or without supply) → 14 red | Pass |
| Order independence | Bit-identical rows, power and blade across gate first/last × links forward/reversed, every row | `PlacementAndWiringOrderGiveIdenticalCommittedResults` ×4. Or is monotone, so the old slot sweep cannot change an Or row (G11, accepted). P4 (old sweep) still turns NAND order, NAND reconvergence (gate first, now fed through the real Either) and NOR order 01/10/11 red. That is the same discrimination as the 9.7a/9.7b reviews (2 + 3). | Pass |
| **Monotone cycles (O6/O7):** admitted; least fixed point; source loss retracts; no stale carry; id-order independent | Two Either gates feed each other's FirstIn, supplied by battery 2, with a condition on one SecondIn. The run must show: off from tick 0 with no self-latch; on in the same tick the condition arrives; off on the next tick after the condition goes, and again after supply loss; identical across id orders. | `OrLoopIsAdmittedAndSettlesAtItsLeastFixedPointEveryTick` ×2 (five phases). P2 (single pass in cycles, no fixed-point iteration) → `swapIds:True` red, which shows the id-order variant genuinely exercises intra-component order. P5 (previous-tick carry into cyclic components) → both Or-loop variants red, plus Both Gate/switch cycle tests. Implementer M5 (seeded powered) and M6 agree. | Pass, discriminated |
| Source-free pure Or loop (O7) | Save, `Construct` and validation admit it; it stays unpowered; Reset is exact | `PureOrLoopIsAdmittedBySaveAndConstruct`; chooser `OrToOr` (one Undo push, Undo restores); `CompiledGraphAdmitsAnOrCycleAndRejectsAMixedOne` | Pass |
| Contact loop (O8) | With an open contact closing the loop, Either stays on through its other input; Both Gate in the same place stays off | `ContactLoopKeepsAnEitherGateOnThroughItsOtherInput` ×2 (Either/Both control) | Pass |
| Mixed loops refused (O10): WithNand, WithNor, through an open switch, entering PowerIn | Typed `NonmonotoneFeedback` naming the ids; wire-order independent. Save gives InvalidConnection with inner NonmonotoneFeedback. `Construct` gives Rejected/InvalidConstruction with the construction kept. Chooser: status line, no command, no Undo. | `LoopsMixingANonmonotoneGateAreRefusedAtomicallyBeforeAnySolve` ×4; `EitherGateChooserAdmitsAnOrLoopAndRefusesAMixedOne` ×5. P6 (Or declared nonmonotone) turns the pure-loop admission, compiled-graph admission, contact loop, Or loop ×2 and chooser `OrToOr` red. | Pass, discriminated |
| Closing the 9.7a/9.7b Or substitutes (F5) | NAND D6 and NOR reconvergence route through a real Either fed on FirstIn only and assert `FirstOnly`/active. `ThroughOr` is added to the NAND and NOR feedback and chooser theories. `ThroughBothGate` is kept. | Diff review. P3 (Or as And) turns all four NAND/NOR reconvergence cases red, so the single-input Either proves the Or law. P4 shows the ordering discrimination is unchanged. No "undelivered Or" text remains. | Pass; changed tests still discriminate |
| Committed reads, lamps, neutral admission (O12) | Rows 00/10/01/11 supplied and 11 unsupplied are exact. Forged rows (supplied 01 inactive, supplied 00 active) are refused. Native lamps go slate/gold per row and back to slate through Reset. | `CommittedReadsFollowEveryRowAndAdmissionIsNeutral`; `PictogramThreeTruthDotsAndThreeLampsFollowCommittedRowsThroughReset`. P1 turns both red. | Pass |
| Run/Reset (9R.1), Save/Load typed sockets (AC3) | Reset exact with identical presentation bits; bytes round-trip with connections equal and the gate present; saved replay identical; the 00 row never moved the blade before the condition | `ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact` | Pass |
| Closed enum / undefined (F2/F3/F9) | `Or = 3` appended; exhaustive arms; the undefined pin moved 3→4 at every prior site (NAND ×2, NOR, BundleValidator); 255 kept; validator admits Or | Diff; law tests. P7 (`_ => false`) turns the NAND, NOR, Or and BothGate undefined-rule tests red. | Pass, discriminated |
| Art (CAT-026 §2/O11, DESIGN "Supplied electrical logic"; F6/G1 deviation) | Icon equals `WorkshopIcons.cs@a6c914e:L76`; navy strokes follow it; no bubble; gold dots 01/10/11 at x −.07/.07/.21 (y −.4, z .42) | `icon.svg` path is byte-equal to the legacy icon. The reviewer independently derived all 12 quadratic chord segments plus the stubs (len .1704 at x −.20313) and the output stub. Every mid-point, cos/sin and length matches the scene to 4–6 decimals. The dots, the absence of TruthDot00 and the 15-stroke count are pinned natively. | Accepted deviation (L2) |
| Package isolation / identity | Declaration, art and icon only; no PartId branch | `static-scope-checks.log`: the only production hits are the two `LogicGateOperation.Or` law arms in `SupplyRoute.cs`, which is the shared typed engine. Schemas, PartId, compiler, `WorkshopConnections`, the BothGate/NAND/NOR packages, ui and Simulation are unchanged. UIDs `00cjt3fjoi6p` and `dgjxmwg3f8fdk` are unique; the ctex hash equals md5(path). | Pass |
| 9A.45a ratchet / solver scan (F8) | The Known list, hash and `GenericIdentifiers` are unchanged; "or" is below the 3-letter floor; Or-specific names are caught through "electrical" | Diff adds only `("electricalOrRow", true)` and `("orRow", false)`; the full native run includes the frozen list and scan | Pass |
| Membership / pins (pre-merge) | Registry 27 (×2), SaveFixture 27, Coverage admitted 27, ids+undelivered 29, palette, capacity, parity, ports golden, csproj/slnx, ImpactRules. Planned example → `electrical_xor` (CAT-027 exists). | Independent derivation: 27 bundle entries, 27 package dirs, 2 undelivered, no `PaletteOrder` duplicates, `electrical_or` = 29; pin lines match | Pass pre-merge; re-derive after merge (PM1) |

### Commands and results (raw under `evidence-9-8a-review/`)

| Command | Result |
| --- | --- |
| `dotnet build CuriousContraptions.slnx -c Debug -v:q` | 0 W / 0 E |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2317/2317**, Coverage 77/77, BundleElements 147/147, E2eRunner 189/189, WorkerAotGuard 17/17 (`test-slnx.log`) |
| CI "Test C# tools", `pages.yml` lines 51–64 verbatim under `bash -e` | 77 / 147 / 17 / 174; exit 0 (`ci-tool-step.log`) |
| `dotnet build CuriousContraptions.Simulation -c Release --no-incremental` | 0 W / 0 E |
| Focused named run (Or, NAND, NOR, BothGate, all choosers, scan) | 141/141, including 32 Or/Either-chooser cases (`native-focused-named.log`) |
| Mutation probes (`mutations/P*`): rebuild, then the electrical, chooser, switch and scan classes (202 tests) | P0 control 202/202. P1 Or=`first`: 8 red. P2 single pass in cycles: 3 red. P3 Or=And: 15 red (all 4 NAND/NOR reconvergence included). P4 old slot sweep: 5 red (NAND/NOR ordering). P5 previous-tick carry in cycles: 6 red. P6 Or nonmonotone plus P7 undefined→false: 10 red. |
| `anvil check --no-tui` on the implementer's 32 changed files; `git diff --check`; AppBundle | No warnings; clean; AppBundle absent |

### Findings

Pre-merge conditions block SnapshotApproval. They are not candidate defects.

- **PM1 — merge with main after C-Bumper+Spring; re-derive the pins.**
  - *Risk:* C-Bumper+Spring touches the shared pin and membership lines, `ImpactRules` and the TODO/sprint-status files. The pins and `PaletteOrder` 29 are unproven for the merged inputs.
  - *Smallest resolving check:*
    - Prove the merge brings only main's changes plus resolutions: `git merge-tree` recomputation, with NOR-style per-file classification.
    - Re-derive every pin from the merged bundle.
    - One rebuild, then full slnx `--no-build --no-restore`.
    - The CI tools step under bash.
    - Release worker build and a named Or/NAND/NOR rerun.
- **PM2 — exact identity.** Stage the merged candidate and record the tree and diff identity. The implementer evidence is already durable in `evidence-9-8a/`.

Non-blocking:

- **L1 — TODO brief.** `TODO.md` appends 9.8a but still carries the base's 9.7b in-flight text. The 9.7b terminal receipt (Pass at `55ee05b9`) sits uncommitted in the `story-9-7b` worktree, awaiting carry. Reconcile at merge.
- **L2 — pictogram deviation (F6/G1).** The pictogram is shaded navy box-segment strokes, not the legacy unshaded textured quad. Geometry is verified exactly against the icon path. Readability at projected size is a queued browser case (`cat-026.test.ts`).
- **I1 — ThroughOr cases.** The `ThroughOr` cases in the NAND and NOR theories are refused by the NAND/NOR gate, whatever Or is declared. They prove the closed CAT-024 D7 and CAT-025 R6 forms, while Or's own admission and refusal behaviour is carried by the Or suite (P6).
- **I2 — Xor forms.** The Xor forms of O9/O10 remain pending under 9.8b, as the spec records.

No unresolved scoped unintended regression found in the candidate.

### Provisional verdict

**Pass-pending-merge.** All required-now 9.8a native criteria pass on the real C# simulation. The monotone-cycle semantics are mutation-discriminated: least fixed point, fresh state every tick, source and condition loss, and id order. So are the law rows, the mixed-loop refusal and undefined closure. The re-routed NAND/NOR tests keep their prior ordering discrimination and now also prove the Or law. SnapshotApproval is withheld until PM1 and PM2 close on the merged snapshot after C-Bumper+Spring. This is not browser or release qualification: `tools/e2e/cat-026.test.ts` remains queued post-catalogue.

## Post-merge re-review — 22 October 2026

Same independent reviewer, with no source, test, tool or content edits. Raw output: `evidence-9-8a-review/post-merge/`.

**Branch layout**

- WIP `8527aa3f7c84d8aeaa966d2948284f128bec7543`, with parent `55ee05b9`.
- Merge `71f7ca1295e592db219bbc8bf1d2fcd76110afef`, with parents `8527aa3f` and main `711568c473cf3143b60ea9145635ccd1a10acf64` (C-Bumper+Spring).
- On top: an uncommitted delta of the spec's post-merge section plus `evidence-9-8a/post-merge/`.

| Check | Command / method | Result |
| --- | --- | --- |
| WIP = reviewed candidate + review files only | Rebuilt the identity manifest from `git diff 55ee05b9 8527aa3f` over the 24 reviewed tracked paths, plus `git show 8527aa3f:<f>` hashes for the 42 untracked paths. Listed any other paths changed. | **Pass.** It rebuilds to `110f5742…`, identical to the pre-merge review. The only extra paths are this record and `evidence-9-8a-review/`, unchanged. |
| Merge brings only main + resolutions; OR not lost or altered | Classified files (main 222, OR 95, 9 both). Main-only files compared with 711568c4, OR-only files with 8527aa3f. Re-ran `git merge-tree --write-tree 711568c4 8527aa3f` (auto tree `288e02da`, conflicts `WorkbenchCapacityTests.cs` and `TODO.md`) and compared it with merge tree `71f7ca12`. | **Pass.** No file outside main ∪ OR. Main-only files equal 711568c4 and OR-only files equal 8527aa3f. The recorded merge equals git's auto-merge except the two conflict resolutions. (a) `WorkbenchCapacityTests`: main's generic Bumper/Springboard rows (`WorkshopInput.Generic(PartId.PinballBumper/Springboard, …)`) plus OR's `electrical_or` on the generic gate row and in the palette-row list. The two-sided diff shows exactly each side's change. (b) `TODO.md`: keeps main's Verified/Also-verified lines (C-Balls and the 9.7b receipt carried) and names 9.8a in flight, closing L1. `SupplyRoute`, `WorkshopConnections`, `ElectricalOrTests` and `elements/ElectricalOr` are unchanged from the WIP. |
| Pins / ordinal, independent | Merged bundle, `elements/` dirs, `UndeliveredParts` arms, pin lines | 27 entries, 27 package dirs, 2 undelivered, no `PaletteOrder` duplicates; `electrical_or` = 29. Registry 27 (×2), SaveFixture 27, Coverage admitted 27, ids+undelivered 29: all match, with no edit needed. (`pin-derivation.txt`) |
| Uncommitted delta | Diff; `anvil check` | Spec post-merge section (+32/−1) and implementer `evidence-9-8a/post-merge/` only. Its claims match the reviewer's results. Anvil: no warnings. |
| One rebuild | `dotnet build CuriousContraptions.slnx -c Debug -v:q` (HEAD 71f7ca12 + delta) | 0 W / 0 E |
| Full solution | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2352/2352**, Coverage 77/77, BundleElements 148/148, E2eRunner 189/189, WorkerAotGuard 17/17 |
| CI "Test C# tools" | `pages.yml` lines 51–64 verbatim, `bash -e` | Coverage 77, BundleElements 148, WorkerAotGuard 17, E2eRunner 174; exit 0 |
| Worker | `dotnet build CuriousContraptions.Simulation -c Release --no-incremental` | 0 W / 0 E |
| Named Or/NAND/NOR/chooser/scan | filtered, normal verbosity (`native-named.log`) | 122/122 |
| AppBundle | `ls -d CuriousContraptions.web/AppBundle` | absent |

Pre-merge findings:

- **PM1:** closed.
- **PM2:** closed. The exact identity is in the SnapshotApproval below.
- **L1:** closed.
- **L2, I1, I2:** non-blocking, as recorded.

Mutation discrimination from the pre-merge review still applies. The merge changes no engine file, no OR package file and no OR oracle. The re-routed NAND/NOR tests are unchanged from the WIP.

Verdict: **Pass** for required-now scoped native/tool criteria on the merged candidate. SnapshotApproval follows.

## SnapshotApproval — 22 October 2026

**Approved snapshot**

- Staged tree: `3389b9d09318f3c3420b505a3786d0f6c9063591`.
- SHA-256 of `git diff --cached --binary 71f7ca12`: `4358a043901e738a245b7475a59ae7301986563f789f1eba04303bfb93b5395a`. The diff has 19 paths.
- What is staged:
  - the spec's post-merge section;
  - `evidence-9-8a/post-merge/`;
  - `evidence-9-8a-review/post-merge/`;
  - this record, up to the end of the post-merge re-review.
- Nothing staged is outside those paths. No unstaged changes remain.

**Parent chain**

- Merge `71f7ca1295e592db219bbc8bf1d2fcd76110afef` has two parents:
  - WIP `8527aa3f7c84d8aeaa966d2948284f128bec7543`, whose parent is `55ee05b96f949a7d93241b5ed0608b32db6bb8a0` (main with 9.7b);
  - main `711568c473cf3143b60ea9145635ccd1a10acf64` (C-Bumper+Spring merge).

**Staged whitespace**

- `git diff --cached --check` reports trailing whitespace only in raw tool output (`evidence-9-8a/post-merge/build-debug.log`). That file is kept byte-for-byte.
- The spec and this record are clean.

**Other conditions**

- `CuriousContraptions.web/AppBundle` is absent.
- Exclusions (scoped approval does not cover these):
  - browser/Chrome qualification (`cat-026.test.ts` and the cat-024/025 Or-chaining browser cases queued post-catalogue);
  - Xor forms (9.8b);
  - release/device/performance gates;
  - common 9A.44 numerical conversion.

## Publication-dependent checks

Publication is:

1. commit the approved snapshot plus this SnapshotApproval section on `story-9-8a`, with normal hooks and no `--no-verify`;
2. `git merge --no-ff` into main;
3. push.

**Before pushing to main**, both of these must hold:

- **C-Bumper+Spring has published.** Main `711568c4` (C-Bumper+Spring) exact-SHA CI run `39720896408` was `in_progress` at approval. Confirm it succeeds and its Pages deployment succeeds before pushing.
- **Main has not moved.** If origin/main has moved past `711568c4`, the new merge is a changed input: return it for an affected re-review.

**Terminal checks** (independent reviewer, after push):

- **Tree equivalence.** The published tree differs from `3389b9d0` only by this record's SnapshotApproval and Publication-dependent sections, with first parent `711568c4`. Confirm remote identity with `git fetch` and `git rev-parse origin/main`.
- **Normal hooks.** They succeed on the slice commit. Re-run the hook on the exact published tree, since the merge commit runs none.
- **Exact-SHA CI** "Build and deploy game" succeeds, including:
  - the static gate;
  - "Test C# game" (native ≥ 2352, no failures);
  - the **"Test C# tools"** step: Coverage 77, BundleElements 148, WorkerAotGuard 17, E2eRunner 174 filtered;
  - the production bundle publish;
  - the static deployment check.
- **Artifact.** The `github-pages` artifact belongs to that run/SHA and has not expired.
- **Deployment.** The github-pages deployment of that SHA reaches the **success** status and consumes that artifact.

Preserve any failed attempt unchanged and return concrete failures to the same implementation owner. Terminal scoped Pass is native/publication only, not browser or release qualification.

## Terminal publication verification — 22 October 2026

Same independent reviewer, with no source changed. Raw outputs are in `evidence-9-8a-review/terminal/`: `identity-and-ci.txt`, `build-job.log`, `deploy-job.log`, `pages.txt`, `hook-rerun.log` and `anvil-slice-verification.txt`. The GitHub run/job logs are the original evidence.

| Check | Command (summarised) | Result |
| --- | --- | --- |
| Remote identity | `git fetch origin`; `git rev-parse origin/main` | `a2eddf811323d95819fef5eeeb50a76a63bf428f`; tree `3e7520ac002315d783d950850781604cbcae3aea` |
| Parent chain | `git log --format='%H %P %T'` | **Main side:** a2eddf81 ← `711568c473cf3143b60ea9145635ccd1a10acf64` (main, `--no-ff` first parent). **Slice:** `28a59b71e555debb1d620d51368f01e74686c1d0` (tree 3e7520ac) ← merge `71f7ca1295e592db219bbc8bf1d2fcd76110afef` ← WIP `8527aa3f7c84d8aeaa966d2948284f128bec7543` and `711568c4`. 8527aa3f was verified identical to the pre-merge reviewed candidate. |
| Tree equivalence | `git diff --stat 3389b9d0 3e7520ac`; `git diff --stat 3e7520ac origin/main^{tree}` | The approved tree `3389b9d0` differs from the published tree in 1 file, this record (+61 lines). Those lines are only the "SnapshotApproval" and "Publication-dependent checks" sections (the receipt). The published main tree is identical to slice tree 3e7520ac. |
| Publication precondition | `gh run view 39720896408`; deployment 7243705510 statuses | C-Bumper+Spring main `711568c4` CI completed **success** at 04:24:36Z, and Pages deployment 7243705510 succeeded, both before the OR run was created at 04:26:14Z. Main had not moved: the first parent is 711568c4. |
| Exact-SHA CI | `gh run list --commit a2eddf81…`; `gh run view 39722366290 --json jobs` | The only run for that SHA: "Build and deploy game", push, main, attempt 1, **success**. Job build 119638514681 succeeded, with every step successful: checkout of `a2eddf81…`, the C# static gate, Test C# game, Test C# tools, the production bundle publish, the static deployment check and upload-pages-artifact. Job deploy 119642039392 succeeded. |
| Native and tools totals | build job log | **Test C# game:** `Passed! - Failed: 0, Passed: 2352, Total: 2352` CuriousContraptions.tests. **Test C# tools:** Coverage 77/77, BundleElements 148/148, WorkerAotGuard 17/17, E2eRunner 174/174. The static gate's 3 builds have 0 warnings and 0 errors. |
| Pages artifact | `gh api …/actions/runs/39722366290/artifacts`; upload log | `github-pages` id **12402340730**, 79457188 bytes, digest `sha256:9f62652a98fa6d976a5ffab69a169d2da1fd0c67251fdaaec3d56394e7204ea1`. workflow_run 39722366290 with head_sha a2eddf81; not expired. The upload step logged the same id and size. |
| Deployment | `gh api …/deployments?sha=a2eddf81…`; `…/deployments/7243882125/statuses`; deploy job log | Deployment **7243882125**, github-pages, ref main, sha a2eddf81. Statuses went waiting → queued → in_progress → **success**, with environment_url https://aidanmorgan.github.io/tim/. deploy-pages used `"artifact_id": 12402340730` and logged "Reported success!". This is the latest github-pages deployment; the prior one is 7243705510 for 711568c4. |
| Normal hooks (gate) | Index tree 3e7520ac with nothing staged; `sh .githooks/pre-commit` | rc 0 in 40 s. Three gate builds have 0 W / 0 E; the staged-test check passed 124/124; the index tree was unchanged afterwards. The merge commit ran no hook (only pre-commit exists). |
| Normal hooks (anvil stage) | The implementer's hook log lacked the anvil line, so this was checked two ways. (1) `anvil check --no-tui` on the 19 slice files (`71f7ca12..28a59b71`). (2) A throwaway worktree at 71f7ca12 with the exact slice diff staged (staged tree = `3e7520ac`, matching the published slice tree), then `anvil hook pre-commit --no-tui`. | (1) "No warnings found". (2) rc 0 with no output. The hook is documented as "silent on pass, one line otherwise", so a missing summary line is the pass case. The throwaway worktree was removed. The only notice was `git apply`'s note of 2 trailing-whitespace lines inside raw build-log evidence, which is not a hook finding. |

No failed attempt occurred. The published tree differs from the approved tree only by the receipt.

No unresolved scoped unintended regression.

Verdict: **Pass.** This is a scoped native/publication terminal Pass for 9.8a Electrical OR Gate (CAT-026) at `a2eddf811323d95819fef5eeeb50a76a63bf428f`. It is not browser or release qualification: cat-026 Chrome/Playwright proof (and the cat-024/025 Or-chaining browser cases) remain queued for post-catalogue E2E, and the SnapshotApproval exclusions stand. Carry this receipt into the next normal publication.
