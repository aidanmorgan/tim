# Independent review: 9.8b Electrical XOR Gate (CAT-027)

## Pre-merge review — 22 October 2026

The reviewer is independent, did not implement this story, and made no edits to candidate files.

- **Candidate:** WIP commit `1e6d707e11577064cb97175a32594566c6e5f5e0` (tree `724636af1f8555d89c2d6fb8faeea12a26d5ab3e`), parent `a2eddf811323d95819fef5eeeb50a76a63bf428f` (main with 9.8a OR).
- **Candidate identity:** SHA-256 of `git diff --binary a2eddf81 1e6d707e` is `d3dbc9749babce40701681e0bef1d4ef3e81315feca1008a36c7974f8c9912ed`.
- **Where the work ran:** the build, suites and probes all ran in a throwaway `git worktree` checked out at the WIP commit, as the coordinator instructed. After the probes, the reviewer's mutations and temporary probe test were removed: the tree's tracked diff was empty before removal. The throwaway tree was then removed, and the evidence copied here.

### Criterion / impact matrix

| Criterion / impact | Expected (derived) | Evidence | Disposition |
| --- | --- | --- | --- |
| Truth table via real power switches (9.8b AC1, X1/X2/X5; Story 9.8 "low when identical") | Battery 7 supplies; real ImpactSwitch contacts are the conditions; the consumer is a real Powered Gate. Rows 01/10 open the blade. Rows 00/11 stay still on every tick. No row powers without supply. | `SwitchedConditionsAndSeparateSupplyDriveTheActualConsumer` 8 rows. Q2 (Xor as Or) turns truth/order row 11, reads, lamps, law and every reconvergence case red. Implementer M1–M3 agree. | Pass, discriminated |
| No supply / links removed (AC2, X5) | A true row (10) with either the supply wire or both condition battery links removed never powers | `RemovingTheSupplyOrConditionLinksUnpowersATrueRow` ×2. Q4 (Xor without PowerIn) → 9 red, including the supply-wire case. | Pass, discriminated |
| Order (X5 reversed links; J6) | Bit-identical rows for gate first/last × links forward/reversed, every row | `PlacementAndWiringOrderGiveIdenticalCommittedResults` ×4. J6 is accepted: in this layout a gate visited first sees 00, which Xor treats as false, so the old sweep cannot glitch the row-11 case. Ordering is carried by the reconvergence and the reviewer's dynamic probe (below). | Pass (with J6 limit) |
| **Reconvergence X6/X7: deviation J2** ("off from tick 0" across two runs) | X7: one source reaching both inputs gives an unpowered Xor, whatever the order. X6: starts powered; wiring the battery into the Or retracts the output in the same solve. | `ReconvergenceThroughAnEitherGateKeepsTheOutputOffFromTheFirstTick` ×4 (gate first/last × reversed links): the control run has row 10 open, and the wired run has the Xor off and the blade still on every tick with Reset exact. Q1 (old slot sweep) turns both gate-first cases red. **Reviewer dynamic probe** (`probes/ReviewerXorDynamicProbe.cs.txt`, not a deliverable): fixed wiring, battery 1 → Xor FirstIn and → ImpactSwitch → Either → Xor SecondIn, with a ball closing the contact mid-run. It shows the same source reaching both inputs mid-run retracts the Xor in that very solve (every tick, Xor active == ¬Either active), for both placement orders, passing 2/2. Under Q1 the gate-first variant fails; under Q2 both fail. | **Adequate.** X7 is proved directly. X6's literal "rewire during Run" is not an available interaction. Each solve restarts from a fresh state (proved by the 9.8a P5/M6 carry probes), so a per-tick off assertion is the same solve-level claim. The reviewer probe independently confirms the mid-run transition with fixed wiring. Optional strengthening (L3). |
| Nonmonotone refusal (X8/X9/X10): XorToXor, WithOr, WithNand, through an open switch, entering PowerIn | Typed `NonmonotoneFeedback` naming the ids; wire-order independent. Save gives InvalidConnection with inner NonmonotoneFeedback. `Construct` gives Rejected/InvalidConstruction with the simulation construction unchanged. Chooser: no command, no Undo, construction unchanged, status line. | `ZeroDelayFeedback…` ×5; `ExactlyOneGateChooserRefusesALoopThroughTheGateAndAdmitsAnOrLoopBeside` (refusals, plus the admitted Or loop with one Undo push). Q3 (Xor monotone) turns the 4 Xor-only loops native and chooser, Or `WithXor` native and chooser, the invariant and the law red. `WithNand` stays refused through NAND, as expected. | Pass, discriminated (L2 on one tautological assertion) |
| Monotone control (J3) | An Or↔Or loop beside the Xor, feeding it off the cycle, is admitted by validate, save, Construct and the chooser. It powers the Xor on the condition and retracts when the condition goes. | `OrLoopFeedingTheGateIsAdmittedAndPowersIt`; chooser admitted case. Implementer M6 (refuse every cycle) and M7 (seeded powered) agree. | Pass |
| 9.8a deferral closed (H8/J8) | `WithXor` is added to the Or loops and the Either chooser. The Or → Xor reconvergence goes through a real Either. In the 9.8a spec, the F5 "Still pending" line is replaced (1 line). | Diff: the 9.8a spec changes +1/−1, and Or tests add `WithXor`. Q3 turns them red. | Pass |
| Committed reads / lamps (X12) | Each row is committed and tick 0 is neutral; contradicting rows are refused; native lamps go slate/gold per row through Reset | `CommittedReadsFollowEveryRowAndAdmissionIsNeutral`; `PictogramTwoTruthDotsAndThreeLampsFollowCommittedRowsThroughReset`. Q2 and Q4 turn both red. | Pass |
| Run/Reset, Save/Load (AC3) | Reset exact; bytes round-trip; saved replay identical | `ClosingOneSwitchOpensTheOutputAndSaveLoadAndResetAreExact` | Pass |
| Closed enum / undefined (H2/H3/H9) | `Xor = 4` appended; exhaustive arms; the undefined pin moves 4→5 at every prior site (NAND ×2, NOR, Or, BundleValidator); 255 kept; validator admits Xor | Diff; Q5 (`_ => false`) turns the NAND, NOR, Or, Xor and BothGate undefined tests red | Pass, discriminated |
| Art (CAT-027 §2/X11; H5/J1) | Icon equals `WorkshopIcons.cs@a6c914e:L77`; 17 navy strokes; dots 01/10 at x −.07/.07 | `icon.svg` path is byte-equal to the legacy icon. The reviewer independently derived all 16 quadratic chord segments (the extra back curve plus OR's body) and the output stub. Every mid-point, cos/sin and length matches the scene to 4–6 decimals. No stubs or bubble; dots 01/10 only. | Accepted deviation (as 9.8a L2) |
| Package isolation / identity | Declaration, art and icon only | Production hits are only the two `LogicGateOperation.Xor` law arms. Schemas, PartId, compiler, `WorkshopConnections`, the other gate packages, ui and Simulation are unchanged. UIDs `b2yf83y2a7m8a` and `d1qgg1pd1thmb` are unique; the ctex hash equals md5(path). | Pass |
| Ratchet / solver scan (H6) | "xor" becomes an element word; the Known list, hash and `GenericIdentifiers` are unchanged | Diff adds `("electricalXorRow", true)` and `("xorRow", true)`. Q5 inserted `xorRow` and `exclusiveXorTable` into `WorkshopPhysicsEngine.cs` and both were flagged. | Pass |
| Coverage planned example (H7/J7) | `pulley` is a Catalogue key with a capability row; `elements/Pulley` is absent | `CapabilityCatalogueRequirements.cs:61` lists `pulley`; the directory is absent; Coverage 77/77 | Pass |

Pre-merge commands (raw files under `evidence-9-8b-review/`):

| Command | Result |
| --- | --- |
| Build | Debug build 0 W / 0 E |
| Full slnx | native **2392/2392**, Coverage 77, BundleElements 150, WorkerAotGuard 17. E2eRunner was 188/189 in the fresh throwaway tree: only `TypeCheckTests` failed, because it had no `node_modules` (flatbuffers). Re-run with `node_modules` linked, it was 189/189 (`e2erunner-with-node-modules.log`). |
| CI tools step, bash | 77/150/17/174; exit 0 |
| Release worker | 0 W / 0 E |
| Reviewer probes (`mutations/Q*`; 243-test filter including the reviewer dynamic probe) | Q0 243/243. Q1 old sweep: 8 red (NAND/NOR order, Xor reconvergence gate-first ×2, reviewer probe gate-first). Q2 Xor=Or: 11 red. Q3 Xor monotone: 12 red. Q4 no-supply Xor: 9 red. Q5 undefined→false plus solver names: 6 red. |

### Findings

- **L1 — TODO / status receipts.**
  - The 9.8a terminal receipt (Pass at `a2eddf81`) and its sprint-status `done` line are still uncommitted in the `story-9-8a` worktree. Main `a598a09e` still shows `9-8a-electrical-or-gate: review`, and TODO has no 9.8a verified line.
  - TODO "In flight" says "Pre-merge review queued".
  - Carry the 9.8a receipt in the next publication, per the coordinator's practice. Non-blocking.
- **L2 — tautological X8 assertion.**
  - In `ZeroDelayFeedback…`, `Assert.Equal(saved, WorkshopSaveCodec.Encode(new WorkshopSavedConstruction(open,…)))` re-encodes the same immutable value. It cannot fail, so spec J4's "saved bytes re-encoded identically after the refused Construct" overstates it.
  - The substantive X8 property is proved by `Assert.Equal(before, simulation.Construction)` and by the chooser test (`Assert.Equal(open, scene.World.Construction)`, no command, no Undo).
  - Non-blocking. Smallest fix: compare against the bytes of the world's construction after the refusal, or drop the line.
- **L3 — X6 dynamic form.**
  - The candidate proves X6/X7 as "off from tick 0" over two runs (J2).
  - The reviewer's fixed-wiring probe (contact closes mid-run) proves the same-solve transition and discriminates ordering. Consider adopting it as a candidate test in a later slice.
  - Non-blocking.
- **I1 — row-11 order cases (J6).** Under the old sweep, the row-11 order cases are non-discriminating by construction. Ordering is covered by the reconvergence gate-first cases and the probe.

No unresolved scoped unintended regression found in the candidate.

Provisional verdict: **Pass** for required-now scoped native criteria on the candidate (pre-merge). The merge re-review follows.

## Post-merge re-review — 22 October 2026

Raw output: `evidence-9-8b-review/post-merge/`.

**Branch layout**

- Merge `746d6582fc6be629f5c3714d85ff79ee5a7a8107` (tree `e6080ad66c25ae2c172a5fe7d16e4612587eed57`), with parents WIP `1e6d707e` and main `a598a09e2b91d58135b3343a2beeea6b88578013` (9.4c).
- On top: an uncommitted delta of the spec's post-merge section plus `evidence-9-8b/post-merge/`.

| Check | Command / method | Result |
| --- | --- | --- |
| Candidate = reviewed WIP | The pre-merge review ran directly on commit `1e6d707e` | Identical by construction (tree `724636af`) |
| Merge brings only main + resolutions; Xor not lost or altered | Classified files (main 70, Xor 71, 3 both). Main-only files compared with a598a09e, Xor-only files with 1e6d707e. Re-ran `git merge-tree --write-tree a598a09e 1e6d707e` (auto tree `035cf1d5`, sole conflict `TODO.md`) and compared it with merge tree e6080ad6. | **Pass.** No file outside main ∪ Xor. Main-only files equal a598a09e and Xor-only files equal 1e6d707e. The recorded merge equals git's auto-merge except `TODO.md`, which keeps main's Verified/Also-verified and 9.4c receipt-pending lines and names 9.8b in flight. The csproj and sprint-status auto-merged with both sides kept. |
| Pins / ordinal, independent | Merged bundle, dirs, `UndeliveredParts`, pin lines | 28 entries, 28 package dirs, 2 undelivered, no `PaletteOrder` duplicates; `electrical_xor` = 30. Registry 28 (×2), SaveFixture 28, Coverage admitted 28, ids+undelivered 30: all match, with no edit needed. |
| Delta | Diff; `anvil check` (the 34 candidate files plus the spec) | Spec post-merge section and implementer `evidence-9-8b/post-merge/` only; anvil reports no warnings |
| One rebuild | Debug build of the merged tree + delta | 0 W / 0 E |
| Full slnx | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | native **2402/2402** (2392 + 9.4c's 10), Coverage 77, BundleElements 150, E2eRunner 189, WorkerAotGuard 17 |
| CI tools step | `pages.yml` lines 51–64 verbatim, `bash -e` | 77/150/17/174; exit 0 |
| Worker | Release `--no-incremental` | 0 W / 0 E |
| Named electrical, chooser, scan and `LatchSupplyFixture` tests | filtered | 190/190 |
| AppBundle | `ls` | absent |
| Static scope | `pin-and-static.txt` | No forbidden-scope change since a2eddf81; Known list unchanged; `git diff --check` clean on the working tree |

Verdict: **Pass** for required-now scoped native/tool criteria on the merged candidate. SnapshotApproval follows.

## SnapshotApproval — 22 October 2026

**Approved snapshot**

- Staged tree: `40afc9e01303dd5e206b2106db985110d3f8b47b`.
- SHA-256 of `git diff --cached --binary 746d6582`: `9e587f5d5d50f84ac6b6844a16ccda532b5e2cea2127d477c9eda50beaa8bdd1`. The diff has 47 paths.
- What is staged:
  - the spec's post-merge section;
  - `evidence-9-8b/post-merge/`;
  - `evidence-9-8b-review/` (pre-merge, probes, mutations, post-merge);
  - this record, up to the end of the post-merge re-review.
- Nothing staged is outside those paths. No unstaged changes remain.

**Parent chain**

- Merge `746d6582fc6be629f5c3714d85ff79ee5a7a8107` has two parents:
  - WIP `1e6d707e11577064cb97175a32594566c6e5f5e0`, whose parent is `a2eddf811323d95819fef5eeeb50a76a63bf428f` (main with 9.8a);
  - main `a598a09e2b91d58135b3343a2beeea6b88578013` (9.4c).

**Staged whitespace**

- `git diff --cached --check` reports trailing whitespace only in raw tool output: two reviewer mutation diffs and `evidence-9-8b/post-merge/build-debug.log`. Those files are kept byte-for-byte.
- This record, the spec and the probe source are clean.

**Other conditions**

- `CuriousContraptions.web/AppBundle` is absent.
- Exclusions (scoped approval does not cover these):
  - browser/Chrome qualification (`cat-027.test.ts` queued post-catalogue);
  - release/device/performance gates;
  - common 9A.44 numerical conversion.
- L1–L3 and I1 are non-blocking.

## Publication-dependent checks

Publication is:

1. commit the approved snapshot plus this SnapshotApproval section on `story-9-8b`, with normal hooks and no `--no-verify`;
2. `git merge --no-ff` into main;
3. push.

**Before pushing to main**, both of these must hold:

- **9.4c has published.** 9.4c's exact-SHA CI run `39723690498` for main `a598a09e` was `in_progress` at approval. Confirm it succeeds and its Pages deployment succeeds before pushing.
- **Main has not moved.** If origin/main has moved past `a598a09e`, the new merge is a changed input: return it for an affected re-review.

**Terminal checks** (independent reviewer, after push):

- **Tree equivalence.** The published tree differs from `40afc9e0` only by this record's SnapshotApproval and Publication-dependent sections, with first parent `a598a09e`. Confirm remote identity with `git fetch` and `git rev-parse origin/main`.
- **Normal hooks.** The gate and anvil stages succeed on the slice commit. Re-run them on the exact published tree, including `anvil hook pre-commit` with the slice diff staged, since the merge commit runs no hook.
- **Exact-SHA CI** "Build and deploy game" succeeds, including:
  - the static gate;
  - "Test C# game" (native ≥ 2402, no failures);
  - the **"Test C# tools"** step: Coverage 77, BundleElements 150, WorkerAotGuard 17, E2eRunner 174 filtered;
  - the production bundle publish;
  - the static deployment check.
- **Artifact.** The `github-pages` artifact belongs to that run/SHA and has not expired.
- **Deployment.** The github-pages deployment of that SHA reaches the **success** status and consumes that artifact.

Preserve any failed attempt unchanged and return concrete failures to the same implementation owner. Terminal scoped Pass is native/publication only, not browser or release qualification.
