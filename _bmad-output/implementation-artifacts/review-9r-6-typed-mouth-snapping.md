# Review: Story 9R.6, Typed Mouth Snapping for Tubes

- **Reviewer:** Murdoch-13 (independent adversarial reviewer; no deliverable edited).
- **Candidate:** worktree `.claude/worktrees/agent-adbfa64630a43398c`, commit `63ba232c`, rebased on `e4afb855` (Story 9R.5b final).
- **Review identity:** clean detached worktree `.claude/worktrees/murdoch13-9r6`. It holds `63ba232c` merged with main `e25082c8`, giving local merge commit `c36c7e08`, so 9R.5b's Ownership fix `a5a6c494` applies. The merge was clean, and `git status --porcelain` was empty before and after every run.
- **Identity of reused evidence:**
  - `git range-diff 007a65ed..2e7cfb48 e4afb855..3b4669ff` shows every code commit patch-identical; only the evidence commit differs.
  - `3b4669ff..63ba232c` touches only `_bmad-output/`.
  - `e4afb855..e25082c8` changes only `tools/Ownership/OwnershipPolicy.cs` outside `_bmad-output/`. That is a standalone analyser; it is not bundled and no suite reads it.
  - My full run's suite digest `68815d8d…` equals her `3b4669ff` run's.
- **Owner rules:**
  - Only a breach of the story's own ACs (epics "### Story 9R.6"), or a player-visible regression the story introduces, blocks.
  - Everything else goes to Story F.9 "Tube Snapping Follow-ups".
- **Round:** 1.

## Static review

- **`engine/TubeMouthSnap.cs`.** `Find` is a pure query: it mutates nothing and creates no network edge.
  - The mover's committed pose is excluded through `instance.Id == mover.Body`. `MoveSnap` passes `part.AuthoredId`, which is the same identity as the instance id (`MachineWorld.Gpu.cs:327`).
  - A target is skipped when its mouth is occupied: another mouth within 1e-4 m with dot < −0.9999.
  - Bores must be equal within 1e-6 m. The capture limits are ≤ 0.45 m and ≤ 20° against the target's opposed axis.
  - A shortest-arc turn keeps the mover's roll.
  - A locked mover returns null. A locked target stays a candidate, as CAT-048 fact 21 requires: "with a locked fixed pipe … placing a new pipe … snaps it to x 1.78".
  - So the AC's "a locked part" refusal reads as the locked mover. I agree with I7.
- **Extractor.** `mouth_start` and `mouth_end` are parsed into the `TubeMouthEnd` enum at the scene boundary. Any other `mouth_*` name, a missing bore, a non-float bore and a non-finite or non-positive bore are all rejected. This follows the enum rule.
- **Scenes.**
  - Every `mouth_start` now faces outward (−X) on the pipe, both bends, the funnel, the powered gate and the ball detector.
  - Every mouth declares `bore_radius` 0.65, except the funnel's wide inlet at 1.3.
  - On the four parts I measured (pipe, both bends and funnel), each mouth sits 0.09 m beyond its rim collider along the outward axis: pipe 1.8 → 1.89, bend 45 −0.918 → −1.008, bend 90 −1.697 → −1.787, funnel 0.9 → 0.99. That consistent offset is CAT-048 fact 21's 3.78 m joined spacing. The powered gate and the ball detector have no rim colliders to compare against.
  - No engine or UI consumer reads `mouth_*` except `TubeMouthSnap`, so the ball detector's flip has no physical effect.
- **UI.**
  - The preview and the click share `PlacementPose`, so the click commits exactly the pose the preview shows.
  - `CanEdit` gates the preview, the click and `MoveSnap`.
  - The release snap runs only on drag or lift release, before the one `CommitSelected`, so it stays one Undo.
  - The gold ring is presentation only.
- **Changed tests.** In `cat-030` test 5, only the construction and the ball id changed; its trajectory assertions are unchanged. The story's "no snap" deletion is done.

## Runtime evidence (mine, `evidence-9r-6-review/`)

### Tier 1 (`tier1/`, clean `c36c7e08`)

- `dotnet build CuriousContraptions.slnx`: 0 Warning(s), 0 Error(s). The Release build is also 0 warnings and 0 errors.
- `dotnet test CuriousContraptions.slnx`, exit 0:
  - WorkerAotGuard 17/17;
  - Coverage 78/78;
  - E2eRunner 156/156;
  - CuriousContraptions 1807/1807, which includes TubeMouthSnapTests.
- Node harnesses: 83/83.
- Ownership `--oracles .`: **exit 0**. The failure she reported was 9R.5b's, and it is fixed on the merge.
- LifecycleContract: 510/0. WireContract: 109/0.

### Tier 2 / Tier 3 (`tier2/`)

- **Selection.** `select --diff e25082c8` gives **Mode: Full, Tier 3 required**.
- **My full run** (`tier2/full-run-c36c7e08/`, run `20261020T172314Z-61593`): **verdict Pass, 41/41 files, 209/209 tests**.
  - Source: sourceHead `c36c7e08`, status digest `e3b0c442…` (clean).
  - Bundle `f8ab7c72…`, unchanged from start to end. Suite digest `68815d8d…`, unchanged.
  - The run used the runner default (SerialAlone files alone; Parallel files on N = 2 workers). That is the same mode as the full runs accepted for 9R.5b and 9R.5.
  - Suite results: tube-snapping 13/13 (Clean), cat-030 7/7, cat-048a 4/4, cat-048b 5/5, cat-049 5/5, cat-050 5/5, cat-005 7/7.
  - clock-master's counters (closeFindings 1, unresponsive 1, recal 4, renewedLate 1, debtFaults 1) are identical to its 9R.4, 9R.5 and 9R.5b baseline.
  - Chrome transit, minimum speed over every observed publication from entry to 0.4 m past the far mouth:
    - straight→45°: 3.813 m/s;
    - 45°→45°: 4.617 m/s;
    - straight→90°: 3.491 m/s;
    - Funnel outlet→pipe: 3.652 m/s.
    - There was no fallback in chord progress.
- **Her runs, reused as corroboration:**
  - `3b4669ff` cat-005 plus the tube suites: 9/9 files, 52/52 tests.
  - `2e7cfb48` full run: 39/40. The miss was the inherited cat-005 wire version 20, fixed by `0978b2a7`; cat-005 passes 7/7 in my full run.

### Mutants (`mutants/`, scratch worktree `.claude/worktrees/murdoch13-9r6-mut` at `c36c7e08`)

Each mutant was applied alone with the Edit tool and then reverted with `git checkout -- .`. The porcelain was empty after each.

| Mutant | Change | Result |
|---|---|---|
| N1 | Ball detector `mouth_start` left as identity (unflipped) | **Killed**: `EveryTubePartDeclaresAStartAndEndMouthWithABoreFacingOutward` |
| N2 | Funnel inlet `bore_radius` 1.3 → 0.65 | **Killed**: the outward/bore test and `ThePipeSnapsToTheFunnelOutletButNotItsWideInlet` |
| N3 | No shortest-arc alignment (`aligned = rotation`) | **Killed**: 12 rows (every-part joins, the 19° row, roll, all four transit joints) |
| N4 | `Precedes`: the farthest candidate wins | **Survives** (100/100). The D5 ordering is tested only for ties. **F9-2** |
| C1 (Chrome) | Bore check dropped | **Killed**: tube-snapping test 5, "a refused snap shows no gold joint ring (753 gold pixels)". Failed both the contended first attempt and the Clean rerun |
| C2 (Chrome) | Occupied check removed outright | Did not compile (IDE0051, unused member); not a result |
| C2b (Chrome) | Occupied check ignored (`&& false`) | **Killed**: test 4 (Occupied), in both attempts |
| C3 (Chrome) | Capture angle 20° → 41° | **Killed**: test 3 (Angled 40°) |
| C4 (Chrome) | Release snap disabled (`&& false`) | Did not compile (IDE0059); not a result |
| C4b (Chrome) | Release snap disabled (`&& snap.Position.X > 1e9f`) | **Killed**: test 8 (moved tube, D4) |

Each Chrome mutant ran with `run --changed tools/e2e/tube-snapping.test.ts --workers 1` (tube-snapping plus the smoke pair). My Chrome mutants show that each Chrome negative control (wrong bore, occupied, angle) fails for its own reason. The implementer's Chrome mutants already covered the preview command (UI-6) and the grid-after-snap coincidence (UI-5).

## Criterion matrix (epics "### Story 9R.6")

| AC | Evidence | Result |
|---|---|---|
| Declared mouth sockets (position, outward axis, bore) on the Pipe, both bends, the Funnel outlet and the Powered Gate | Scene diff. Native `EveryTubePartDeclares…`, `APipeSnapsOntoTheEndMouthOfEveryTubePart` (6 parts), `APoweredGateSnapsOntoAPipeMouth`. N1 and N2 killed | Met |
| Snap within 0.45 m and 20°: the preview shows it without mutating | Chrome test 1: the gold ring, plus `hoverCanvas`'s `expectNoCommand` (no CCGPU acknowledgement while hovering). Native `TheQueryNeverMutatesTheConstruction`. Her UI-6 killed | Met |
| On release, mouths coincide within 1e-4 m and dot < −0.9999 | Chrome `assertJoined` (1e-4, −0.9999) on the saved construction, in tests 1–5 and 7–13 and cat-030 test 5. Native `AssertJoined`. N3 killed; her UI-5 killed (0.02 m) | Met |
| One Undo removes only the new part | Chrome test 1: the construction equals the pre-placement snapshot. Test 8: one Undo returns the moved pipe | Met |
| Refused: 0.72 m or 40° | Chrome test 2 (0.72 m and 0.52 m refused, 0.42 m snaps) and test 3 (40° refused, 15° snaps; I checked that the 40° offer's mouth is about 0.04 m from the target, so only the angle refuses it). Native boundaries 0.44/0.46 m and 19/21/40°. C3 killed | Met |
| Refused: an occupied mouth | Chrome test 4 (0.32 m away, refused only by occupancy). Native. C2b killed | Met |
| Refused: a locked part | Locked mover: native `ALockedMoverNeverSnaps…` (her mutant 5 killed). Chrome test 6 shows the UI gate refuses to drag the level's locked fixture. A Chrome locked-target join waits for 9R.7's level (I7) | Met (locked target is native only; F9-6) |
| Refused: the Funnel's wide inlet (wrong bore) | Chrome test 5. Native. C1 and N2 killed | Met |
| Unavailable while Running or Paused; returns after Reset | Chrome test 7: no command is issued while Running or Paused, and the snap works after Reset. `CanEdit` gates every snap path | Met |
| A ball crosses straight→45°, 45°→45°, straight→90° and Funnel outlet→pipe continuously | Chrome tests 10–13 (minimum speeds 3.49–4.62 m/s, no progress fallback). Native per-tick probe: minimum 3.41 m/s (hers). N3 kills the native transit rows | Met |
| The joined placement survives Reset and Save/Load | Chrome test 9: exact Reset snapshot, then reload + Load with `assertLoadRestores`, still joined | Met |
| tube-snapping, cat-049, cat-050 and cat-030 pass 100% in Chrome | My full run: 13/13, 5/5, 5/5, 7/7 | Met |
| Build 0 warnings, `dotnet test` 100%, every suite 100% in one Chrome run against the production preview | Tier 1 above. My full run is 41/41 files and 209/209 tests on clean `c36c7e08` | Met |

**Player-visible regressions:** none found.
- cat-030, cat-048a/b, cat-049 and cat-050 all pass in the full run.
- Snapping changes only a placement or release within 0.45 m and 20° of a free equal-bore mouth.
- The tube levels (clear_pipe, gentle_bend, quarter_bend) are covered by cat-048a/b, cat-049 and cat-050, which pass. I did not audit every level's layout for an unintended capture.

## Findings

### Blocking

None.

### Filed to Story F.9 "Tube Snapping Follow-ups"

- **F9-1 (formatting).** Two statements share one line in three places:
  - `ui/Workshop.cs:534` (`… = PlacementPose(chosen, at);            var placedId = _nextId;`);
  - `ui/WorkshopGuidance.cs:336` (`… = PlacementPose(tool, position);        }`);
  - `tools/e2e/tube-snapping.test.ts:89` (`goldPixels(...);        if (snaps) …`).
- **F9-2 (untested D5 ordering).** Mutant N4 (the farthest candidate wins) survives the native suite. Add a two-candidate test at different distances, plus one at different angles.
- **F9-3 (bounds).** The snapped pose is not re-checked against `PlacementInside` or the bench bounds. Only the pointer point is checked, so a snap near the edge can commit a centre outside the bench (a translation of up to 0.45 m plus the alignment turn).
- **F9-4 (gizmo moves).** DESIGN.md says "a move snaps on release". The pointer drag and lift release snap, but gizmo arrow moves, rotate steps and Height steps never do (I5, by decision). Confirm the intent, or extend the snap to gizmo moves.
- **F9-5 (allocation, performance gate).** `TubeMouthSnap.Find` allocates two lists per preview frame (`others`, `Mouths`). It belongs to the named performance gate.
- **F9-6 (locked target in Chrome; doc wording).**
  - The locked-target join (CAT-048 fact 21) is proven only natively until 9R.7's `joined_pipe` level.
  - CAT-048 fact 11 says "a locked part [is] not a candidate", which conflicts with fact 21. Reword fact 11 to "a locked part never moves to snap".
- **F9-7 (visual seam).** Joined collars leave about a 0.05 m gap.
  - The collar torus is centred at ±1.8, with a tube radius of 0.065, so its face is at 1.865. The mouth is at 1.89, which comes from fact 21's 3.78 m spacing.
  - DESIGN.md says "Collar faces meet". This is cosmetic only; transit is continuous.
- **F9-8 (weak outward oracle).** The native outward check is only `outward_start · (start − end) > 0`. A per-mouth axis against the collar normal would be stronger. Chrome transit covers the real alignment for now.
- **F9-9 (not in this AC).** CAT-048 fact 10 ("a part from another world never snaps") and the §5 "Snapping" summary's "separation and rejoin" (line 113) have no test here.

### Process disclosures

- My evidence used two runner verbs: `full` for Tier 3, and `run --changed` with a list file written by the Write tool for the mutants.
- The Tier 1 and mutant scripts are kept in `evidence-9r-6-review/scripts.sh.txt`. Each mutant's effective diff and result are recorded in its `mutants/*.txt` file.
- Join tolerance: the Chrome and native checks assert gap ≤ 1e-4 m and dot < −0.9999 and pass. I did not measure the actual gap margins.
- Two mutant forms (C2, C4) failed the analyser build and were rewritten as C2b and C4b. Both are recorded.

## Terminal verdict: **Pass** (round 1; scoped to Story 9R.6 at candidate `63ba232c`, verified merged with main `e25082c8` as `c36c7e08`)

- Every AC is met on my own clean-tree evidence: Tier 1, a 41/41-file full Chrome run, and 4 native plus 4 Chrome mutants, of which 7 were killed and 1 was filed.
- There are no blocking findings, and 9 items are filed to F.9.
- This is a scoped Pass only. It claims no global, performance or runtime qualification.
- The scratch worktrees `murdoch13-9r6` and `murdoch13-9r6-mut` are review-only.
