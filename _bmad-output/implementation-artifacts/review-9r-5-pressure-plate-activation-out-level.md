# Review: Story 9R.5, Pressure Plate ActivationOut level lights a Signal lamp (0.1 s / 12-tick release hold)

- **Reviewer:** Murdoch-11 (independent adversarial reviewer; no deliverable edited).
- **Candidate:** worktree `.claude/worktrees/agent-ab1c08baf3533074d`, head `c5336092` (evidence only) over code commit `d6f5cacd`, on base `15b7431b` (Story 9R.4, merged to main as `37f04228`).
- **Main drift:** `git diff --name-only 15b7431b 37f04228` outside `_bmad-output/` is empty, so main carries no code change the candidate lacks. `git merge-tree 37f04228 c5336092` conflicts only in `_bmad-output/implementation-artifacts/sprint-status.yaml` (administrative; resolve at merge).
- **Owner rules:** only a breach of the story's own ACs (epics "### Story 9R.5"), or a player-visible regression it introduces, blocks. Everything else goes to Story F.7 "Plate Signal Follow-ups".
- **Round:** 1.

## Verdict: **Pass** (round 1; scoped to Story 9R.5 at code commit `d6f5cacd`, candidate head `c5336092`)

- No blocking finding. Every story AC is met with my own clean-tree evidence: Tier 1 is green, 7 of 8 mutants are killed (the survivor is an untested defensive rejection from spec D6, not an AC), and my serial Chrome run passes 25/25.
- Her Full run (38/38, 187/187) is reused for AC8 after identity checks: clean tree, same head, same suite source digest as my run.
- Four items are filed to Story F.7. Scoped Pass only; no global or runtime qualification is claimed.

## Criterion matrix

| AC (epics 9R.5) | Evidence | Result |
|---|---|---|
| 1. Plate ActivationOut wired to a lamp through the real wiring UI; a resting basketball lights it | Chrome cat-052-lamp test 1 (drawer row, text choice, bulb pixel lit, Memory cosmetic > 0.99); native `ABasketballRestingOnThePlateLightsItsLampOnItsFirstLoadedTick` (first high tick is a qualifying tick: no dwell) | Met |
| 2. Ball rolls off / is lifted: dark on the first tick after the 12-tick hold | Native `TheLevelIsHeldThroughElevenTicksAndDarkFromTwelve` (n = 1, 11 lit; 12, 13 dark), tilted roll-off live bench; Chrome test 2 trace (her run): last Q at tick 96, held 97–107 (n = 1..11), dark at 108 (n = 12) and after; explicit assertion `[n11, n12, n13] = [lit, dark, dark]`. Mutants H13, H11 killed | Met (roll-off; "lifted" is the same code path, no separate bench) |
| 3. Domino below minimum leaves the lamp dark; bounce stays lit for gaps < 0.1 s; dark 12 ticks after last contact; boundaries 11/12/13 | Native `AnUnderweightDominoLeavesThePlateLampDark`, `ABounceGapOfElevenStaysLitAndTwelveShowsOneDarkTick` (gap 11: 0 dark; gap 12: exactly 1 dark tick per gap), live 2 m bounce bench; Chrome test 3 (domino, no hold pending, bulb Slate) and test 4 (bounce trace: Q 75, H 76–82 = 7-tick gap, Q from 83, lit throughout). Mutant ONSET (hold counted from onset instead of last contact) killed | Met |
| 4. Plate → Counter / Delay / latch refused with "A level output can only light a Signal lamp."; latch refusal identical | Native `APlateActivationOutLightsOnlyASignalLamp` (Counter, Delay, SetIn, ResetIn, exact message), `StateLatchTests.Output_LevelDrivesOnlySignalLampsAndRejectsInvalidShapes` (latch, exact message, save codec rejects); Chrome test 5 reads each refusal from `CCGPU_UI_REJECTION` (one per attempt, exact message, icon-only Set/Reset choices), then saves: **no link stored**; the admitted plate → lamp control is stored with no extra refusal. UI path `ui/WorkshopConnections.cs:106-108` validates before `PushUndo`/submit, so a refused edit keeps nothing. Mutants REFUSE, ONELEVEL killed | Met |
| 5. The plate's supplied contact still switches a gate at the same time (both domains) | `ContactClosed` (ActivationNetwork.cs:204-211) and the lamp mirror (`:469-475`) both read the one `values[slot]` of the `ContactLoadSource` node: one committed level. Native `TheLampAndTheSuppliedContactShareOneHeldLevelOnEveryTick` asserts plate == lamp == next-tick route on every tick through the hold; Chrome test 1 opens the gate and passes the ball with the lamp lit. Mutant MIRROR killed (and her mutant 5, hold on the lamp only) | Met |
| 6. Run/Reset matches the snapshot (lamp Slate); Save/Load preserves the `activation_out` wire | Chrome every test: `expectResetMatches(s0)` (includes `read.activation_memory`, so the hold row's `due_tick` must return to 0), lamp phase Clear and bulb Slate after Reset; test 6 saves, reloads, `assertLoadRestores`, authored state bit-identical, wire preserved, loaded bench lights the lamp. Native `TheReleaseHoldIsCommittedCheckpointState` (Clear admits it released; a superseded candidate leaves it unchanged; a second network continuing the checkpoint releases on the same tick) | Met |
| 7. cat-052-lamp, cat-052, cat-037 100% in Chrome serially | My serial run `20261020T134105Z-44213` (`--workers 1`): cat-037 6/6, cat-052-lamp 6/6, cat-052 7/7, then the smoke pair, one after another | Met |
| 8. Build 0 warnings; `dotnet test` 100%; every e2e suite 100% in one serial run | My Tier 1 on clean `d6f5cacd`; her Full run reused after identity checks | Met (see below) |

Story-specific checks requested:

- **Hold uses the sized activation-memory table, no fixed slots.** `ActivationMemory.Holds` now includes `ContactLoadSource`; `_memorySlots = new int[_nodes.Length]` and `_memoryNodes` are built from the compiled nodes (ActivationNetwork.cs:108-115); the 28-node no-limit bench now asserts 10 + 13 memory rows. No new constant or fixed offset. Mutant HOLDS (plate takes no row) killed.
- **One held level** for contact and lamp: see AC5.
- **Element-keyed rule deleted.** No `PartId.Latch` / `is WorkshopLatch` remains in `WorkshopConnections.cs` or in the lamp-kind choice; the rule and the lamp kind read `WorkshopPorts.Signal(...)`. `LatchDrivesLampOnly` / `LampFollowsOneLatch` are renamed with no alias.

## Findings

### Blocking

None.

### Filed to Story F.7 "Plate Signal Follow-ups"

- **F7-1 (untested rejection, D6).** Mutant VALID deletes the `"Contact-load release hold differs from its committed level."` rejection in `CommitContactLoads` (ActivationNetwork.cs:508-511) and it survives the whole native project (1775/1775 pass, `mutants/VALID-full.log`), not only the 169-test filter. Spec D6 says `Consume` rejects a committed row that disagrees with the committed level; nothing proves it. Add a native row: a Latched level with `DueTick = 0`, a Clear level with a due tick, and a due tick beyond `boundary - 1 + hold`, each rejected. Not player-visible (a well-formed committed checkpoint never hits it).
- **F7-2 (formatting).** ActivationNetwork.cs:473 joins two statements on one line (`var target = Slot(edge.Target);            values[target] = ...`), a leftover of the mirror-loop edit. Cosmetic.
- **F7-3 (coverage).** AC2's "or is lifted" has no separate bench; it is the same non-Loaded path as roll-off (killed mutants H11/H13 apply), so the hold is proven but a lift bench would make it direct.
- **F7-4 (process, raised by the implementer).** Raw logs written with `tee`/redirection and the probe moved with `mv`; records only. No action beyond the note.

## Runtime evidence (mine, `evidence-9r-5-review/`)

### Tier 1 (`tier1/`, detached clean worktree `.claude/worktrees/murdoch11-9r5` at `d6f5cacd`, `git status --porcelain` empty)

- `dotnet build CuriousContraptions.slnx`: 0 Warning(s), 0 Error(s), exit 0 (`build.log`).
- `dotnet test CuriousContraptions.slnx`: WorkerAotGuard 17/17, Coverage 78/78, E2eRunner 156/156, CuriousContraptions 1775/1775, exit 0 (`test-full.log`). Matches her `tier1-dotnet-test.txt` counts.

### Mutants (`mutants/`, scratch worktree `.claude/worktrees/murdoch11-9r5-mut` at `d6f5cacd`, i.e. after the rebase onto `15b7431b`; each alone, then `git checkout -- .`; script `mutants.sh.txt`; each log starts with its diff)

| Mutant | Change | Result |
|---|---|---|
| H13 | release when `boundary > due` (n = 13) | Killed, 7 fail (n = 12/13 rows, shared-level, roll-off, gap 12, checkpoint, phase-follow) |
| H11 | due = boundary + hold − 1 (n = 11) | Killed, 9 fail |
| ONSET | hold refreshed only at onset, not per qualifying tick | Killed, 12+ fail (bounce rows, shared-level, 9R.4 plate rows) |
| MIRROR | lamp mirror follows State Latch only | Killed, 10 fail |
| HOLDS | plate takes no memory row | Killed, 12+ fail |
| REFUSE | level refusal keyed to `PartId.Counter` | Killed: plate refusal row and the latch regression row |
| ONELEVEL | a level-fed lamp admits a second signal | Killed: plate and latch rows |
| VALID | drop the committed-row consistency rejection | **Survives**: 169/169 filtered, and 1775/1775 on the unfiltered native project (`VALID-full.log`): F7-1 |

Her six mutants ran before the rebase; H11/H13/ONSET/MIRROR re-cover the same hold paths on the rebased code, and all are killed.

### Tier 2 (`tier2/serial/`, runner log `tier2-runner-1.log`)

- **Runner:** `20261020T134105Z-44213`, `run --changed serial-changed.txt --workers 1` (list: cat-052-lamp, cat-052, cat-037), from the clean worktree `murdoch11-9r5`. Mode Selected, Verdict **Pass**. It took the runner lock on the first attempt.
- **Identity:**
  - `sourceHead` is `d6f5cacd`.
  - `sourceStatusDigest` is `e3b0c442…`, the empty hash. `git status --porcelain` is still empty after Tier 1 (bin/obj are ignored).
  - `workersUsed` is 1.
  - The suite source digest `00f14b14…` is the same at start and end, and **equal to hers**.
  - Bundle `9fb135ae…` is the same at start and end; served checks pass (285 files). Her bundle on the same head is `870fa4d5…`. The publish is not byte-reproducible across runs, as already seen in 9R.4 (`a59e02ce…` vs `45ff83c8…` on `d7ab0e3e`). Each run is internally consistent.
- **Order, no overlap:**
  - cat-037 6/6 (13:41:06–13:42:29);
  - cat-052-lamp 6/6 (13:42:29–13:45:21);
  - cat-052 7/7 (13:45:21–13:47:08);
  - engine-core-2a1 5/5 and engine-core-2b1 1/1.
  - Totals: 5 files, 25/25 tests, with zero close findings, recalibrations or debt faults.
- **cat-052-lamp traces are identical to hers:**
  - Roll-off: the last qualifying tick is 96, the level is held 97–107 (n = 1..11) and dark from 108 (n = 12).
  - Bounce: Q at 75, held 76–82 (a 7-tick gap), Q again from 83, lit throughout.

### Reuse of the implementer's full run (`evidence-9r-5/full-run-1/`, in the candidate worktree)

- **Run:** `20261020T121826Z-82744`, verb Run (`--diff c5a1cdda` → Mode Full, Tier 3 required), Verdict **Pass**.
- **Identity:** `sourceHead` = `d6f5cacd8c36…` (the code commit under review); `sourceStatusDigest` = `e3b0c442…`, the empty-status hash, so the tree was clean; suite source digest `00f14b14…` equal at start and end; bundle `870fa4d5…` equal at start and end; served checks pass (285 files). Workers 2 (calibrated N = 2). `c5336092` adds only evidence and spec text on top, so the run applies to the candidate.
- **Result:** 38/38 files, 187/187 tests. cat-052-lamp 6/6, cat-052 7/7, cat-052-two-plates 4/4, cat-037 6/6, cat-051 5/5, cat-051-own-supply 6/6, capacity-9r 3/3, palette-placement 25/25. No suite other than clock-master has a recalibration, debt fault or close finding.
- **clock-master** 9/9 with closeFindings 1, unresponsive 1, recal 4, renewedLate 1, debtFaults 1, observation Clean: identical, field for field, to the 9R.3 baseline and both 9R.4 runs (review-9r-4 Tier 2), i.e. the suite's own injected faults. Not a regression.
- AC8's "one serial run" is met under the runner model (SerialAlone suites alone, Parallel on 2 workers), as accepted for 9R.3 and 9R.4.
