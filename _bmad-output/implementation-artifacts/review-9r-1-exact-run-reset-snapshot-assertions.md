# Review: Story 9R.1, Exact Run/Reset Snapshot Assertions (Murdoch-7, round 1)

**Terminal verdict for round 1: PASS (scoped).** Every story AC is met on the final candidate `b1390cd9`. No blocking finding is open. The shipped counter/clock memory change introduces no player-visible regression. Probes P1 and P2 are green and not vacuous; they reset while Running, mid-count and mid-interval. Everything below is filed.

- **Final candidate:** `b1390cd9`. It is `042a1de0` (feat) plus three commits:
  - `5015326d`: docs, the serial full-run evidence and the D2 Load-leg amendment;
  - `24e192f9`: docs, the Parallel full run and the cat-017 repeat evidence;
  - `b1390cd9`: the cat-020 SerialAlone demotion in `tools/E2eRunner/SuiteCatalogue.cs` and the README.

  Outside `_bmad-output/`, `git diff --name-only 042a1de0 b1390cd9` lists only those two files. Engine, driver and suite code is byte-identical to `042a1de0`.
- **Reviewer:** Murdoch-7, independent. I edited no deliverable. I applied mutants and probes only to my own detached worktree, `.claude/worktrees/murdoch7-9r1`, and restored it after each one; its status is clean at the end.
- **Blocking rule (owner):** at most 2 rounds. Only a breach of the story's ACs, or a player-visible regression it introduces, blocks.
- **Scope:** this review covers the snapshot helper, the ten listed suites, the D3 counter/clock memory change in shipped engine code, and the cat-020 timing demotion. It makes no global or runtime qualification claim; detailed performance stays at its named gate.
- **Raw evidence:** `_bmad-output/implementation-artifacts/evidence-9r-1-review-murdoch7/`:
  - `tier1/`
  - `mutants/` (each diff plus its TAP)
  - `chrome/tier2`, `chrome/run-alone`, `chrome/probes-v2`, `chrome/probes-v1-vacuity-check-defect`, `chrome/c1`
  - `chrome-queue.sh.txt`, the exact commands
  - `mutants.diff.txt`, the static-phase plan

## Story AC matrix

| AC | Evidence (independent unless marked reused) | Status |
|---|---|---|
| 1. A shared helper captures the committed construction (poses as f32 bits, parameters, connections, goal) through the committed read, and asserts bit-equality naming the first differing field | **Static:** `captureSnapshot`, `expectResetMatches` and `run-reset-snapshot.ts`; the design-review table below has the detail.<br>**Runtime:** my Tier 2 run (below) and the reused serial full run exercise it in cat-020 and cat-049.<br>**Mutants:**<ul><li>U1–U6, which make the compare vacuous, are all killed by the unit suite.</li><li>The implementer's M1b and M4 TAPs show Chrome failures at `Reset: read.bodies[id=…]…` (read).</li></ul> | Met |
| 2. The negative control: one perturbed f32 bit fails and names the field | My run on clean `042a1de0`, `tier1/unit-green.tap`: 24/24. It includes `read.bodies[id=1].local.y 0x40400000≠0x40400001`, -0/+0 and two NaN payloads. U3, which decodes floats to numbers, makes 4 tests red. | Met |
| 3. Each listed suite asserts the snapshot after Reset, and after a second Run then Reset | A static trace of all ten suites. Each has at least one test with two cycles against one S0: 048a t2, 048b t2/t4, 049 t2/t4, 050 t2/t4, 030 t6, 051 t2/t3, 052 t2/t4, 017 t3, 002 t4, 020 t3. Every `reset()` is preceded in its cycle by `expectRunAdvanced`, through a helper. All pass in the reused serial full run. | Met |
| 4. Each test in cat-048a/048b/049/050 builds its own bench and passes when run alone | **My re-run, `chrome/run-alone/`:** 19/19, each test in its own `node --test --test-name-pattern` process, against my own preview (:8297).<ul><li>Head `042a1de0`.</li><li>Bundle manifest `5edbe8df…`, which equals my Tier 2 runner's `bundleIdentity` and was unchanged at the end.</li></ul> | Met |
| 5. cat-051 asserts the blade's closed pose after Reset; cat-017 asserts clock phase 0; cat-020 asserts count 0 and an open contact | <ul><li>**cat-017:** `assertClockStopped` checks phase/due_tick/count = 0/0/0 and the counter at 0.</li><li>**cat-020:** `assertCounterCleared` checks count 0 and the counter activation `Clear`.</li><li>**cat-051:** the blade entries are deep-equal to S0. That matches the spec task ("equals its authored closed pose"), but it only repeats the full compare (F1).</li><li>In cat-051 t3, blade samples are non-empty: `assert.ok(bladeSamples.length > 0)` passes in the full run.</li></ul> | Met per spec wording (F1 filed) |
| 6. `dotnet build` 0 warnings, `dotnet test` 100%, and every `tools/e2e/*.test.ts` 100% in one serial Chrome run against the production preview | **Mine, clean `042a1de0`:** build 0 warnings / 0 errors, and `dotnet test` 17 + 78 + 1696 + 156, all passing.<br>**Mine, `b1390cd9`:** build 0 warnings, and E2eRunner.Tests 156/156.<br>**Reused serial full run** `20261019T181945Z-91929`:<ul><li>`sourceHead` `042a1de0`;</li><li>`sourceStatusDigest` `e3b0c442…`, the empty-status hash, so the tree was clean;</li><li>bundle `e7797f1b…` at both start and end;</li><li>`workersUsed: 1`;</li><li>Pass, 34 files, 166/166.</li></ul>`clock-master`'s line (`closeFindings=1 unresponsive=1 recal=4 debtFaults=1`, Pass) matches the 9R.0b baseline exactly, in `evidence-9r-0b/full-run/summary.txt`; it is the declared 600 ms block fault.<br>**Why this run still applies at `b1390cd9`:** after `042a1de0` only cat-020's phase classification changed. The reused run had `workersUsed: 1`, and every file runs in its own process with its own Chrome and preview, so 166/166 at `042a1de0` holds.<br>**Bundle hashes:** publishing the same source gives a new bundle hash each time (hers `e7797f1b…`, mine `5edbe8df…`). Runs are therefore matched across publishes by `sourceHead` plus a clean status. The summary is in the coordinator's scratchpad `full-run/summary.txt`, and in-repo through the implementer's commit `5015326d`. | Met |

## Coordinator design-review requirements

| # | Requirement | Finding | Status |
|---|---|---|---|
| 1 | Exclusions include `cadence` and `projection`, matched by full top-level path | `EXCLUDED_PATHS.has(\`${prefix}.${field}\`)` is an exact full-path check. Nested `read.occurrences[*].sequence` and `saved.construction.revision` stay compared, and a unit test covers both. U1, which excludes every top-level `read.*` path, makes 8 tests red. | Met |
| 2 | No vacuous pass | **Mid-run read:** `expectRunAdvanced` uses the Pause acknowledgement, which must have tick > 0 and differ from S0 outside `tick`/`phase`. Both acknowledgements have kind `Acknowledgement`, so `read.kind` is not trivially different. U5 makes 2 tests red.<br>**Bodies:** empty or wrong-count bodies are rejected; U6 makes 1 test red.<br>**Path sets:** a path-set difference fails; U4 makes 1 test red.<br>**Read half:** dropping it, U2, makes 8 tests red.<br>**Schema listing:** the explicit listing has a field-count unit test. Weakness: a field with a default value escapes both the listing and the test's parser. U7 **survives** (F5). | Met (F5 filed) |
| 3 | Goal state means the authored `goal_*` fields are equal and no `CCGOAL_SOLVED` appears after Reset | `PuzzleRecord.goal_*` is compared bit for bit, the unit test "a differing authored goal fails" covers it, and `capturedCount === solvedAtReset` is checked. | Met |
| 4 | S0 and S1 are bound to the consumed Save acknowledgement | `command()` returns `entry.bytes` of the entry at `mark`. It must be Applied, newer in sequence and revision, in phase `Building`, and the only arrival. Save carries `Simulation.Committed` (`Program.cs:270-271`), and Reset sets `Committed = read` from a fresh `Admit`. | Met |
| 5 | Raw u32 float bits, with a unit test on two NaN payloads | `hex32(bb.readUint32(...))` is used for every float, and the unit tests have `0x7fc00000≠0x7fc00001`. U3 makes 4 tests red. | Met |
| 6 | Motion is named as unobservable | In the Spec Change Log, D2 amended. | Met |

## D3 engine change (shipped code): scope and regression

- **Scope:** the coordinator accepted D3 as in scope (F4). AC5 cannot be observed without it.
- **Static analysis:**
  - The oscillator's `DueTick` 0 sentinel is safe, because `IntervalTicks == 0` is rejected when the network is constructed (`ActivationNetwork.cs:128`).
  - The counter emits once per world either way: the count is monotonic, and the Latched gate replaces `_counterEmitted`.
  - All three production `new WorkshopRead(` sites carry `Memory`, and admission guards it with `HasSameBits(ClearMemory())`.
  - The removed getters had no production caller.
  - B1 grows to 26960 B, and `ResponseAbi()` reads that dynamically.
  - The one behaviour change is a fix: a superseded or discarded candidate no longer advances the count or clock.
- **Runtime, the Reset-from-Running coverage gap (F2):** every listed Reset is now made from Paused, so I ran probes, `chrome/probes-v2`, runner `20261019T200609Z-52922`, `042a1de0` plus the probe diffs, Pass, 18/18. Each probe resets **while Running**, at 1500 ms and at 800 ms. Before each Reset it checks the pose sequence advanced and a body moved.
  - **P1 (cat-020):** the detector balls fall from 3.855 to −0.120 in the 1500 ms cycle, and to 0.351 at 800 ms. That is mid-count. After both Resets the exact compare passes, with count 0 and the contact Clear.
  - **P2 (cat-017):** the ball moves from −0.207 to −0.466, with one pulse at 1.0 s in the 1500 ms cycle. After both Resets the exact compare passes, with the clock Stopped, due 0 and count 0.
  - **No player-visible regression.**
  - The v1 probes in `chrome/probes-v1-vacuity-check-defect` failed on **my own** non-vacuity check, before any snapshot compare. It compared against a Build-mode pose read. The probable cause is that this read does not carry the running bodies (Build mode publishes nothing). v1 logged no ids, so that cause is not verified. The defect is in the probe, not in the product, and v2 is the authoritative probe.
- **Mutant C1:** Reset keeps the paused bodies, with the main-thread guard bypassed (`chrome/c1`). It was **killed** in cat-002, cat-030 and cat-052: 19 of 23 tests went red, at `reset (Space): command Faulted (InvalidRead)`. The kill came from a worker-side admission validation before any snapshot ran, so this mutant does not show the snapshot path on those suites. The implementer's M1b (cat-049) and M4 (cat-051) do show it (F14).

## Tier 2 and the Parallel question

- **My Tier 2:** runner `20261019T193756Z-11823`, `--changed` cat-020 and cat-049, plus the smoke pair, `workersUsed: 2`.
  - `sourceHead` `042a1de0`, clean status, bundle `5edbe8df…` at start and end.
  - Pass, 4/4 files, 16/16 tests, and 0 recalibrations; cat-020 ran Parallel.
- **The implementer's answer:** the full run was serial because AC6 requires a serial run and `run` has no `--workers` option.
- **Her Parallel full run** `20261019T190207Z-61276` is on `5015326d`, identical code. 33/34 passed. cat-020 passed all 5 tests but closed with one clock recalibration (`Elapsed`) about 2 s after boot, before any snapshot code runs.
  - cat-020 had shown recalibration failures in the 9R.0a calibration trials even serially. Trials 6 and 8 recalibrated; trial 4 failed on a different close finding, a debt fault, with `recal=0`. So this is the known clock-lapse family. It is not a 9R.1 regression.
  - The coordinator's demotion of cat-020 to SerialAlone (`b1390cd9`) is correct and minimal. My review of that diff found one `Serial(...)` declaration that replaces the `Parallel(...)` one, with an owner reason, and a README row. E2eRunner.Tests are 156/156 on `b1390cd9`, and the selection still resolves.
  - The suite edits do not otherwise invalidate Parallel qualification. Each suite gets its own browser and context, so IndexedDB is isolated.

## Tier 1 on clean `042a1de0` (`tier1/`)

| Check | Result |
|---|---|
| Build | `dotnet build CuriousContraptions.slnx`: 0 warnings, 0 errors |
| Tests | `dotnet test`: WorkerAotGuard 17, Coverage 78, CuriousContraptions 1696, E2eRunner 156, all passing |
| Snapshot unit suite | 24/24 |
| Node tests | `node --experimental-vm-modules --test tools/*.test.mjs`: 82/82. Without the flag, 6 fail with `vm.SourceTextModule is not a constructor`; that is environmental, the flag is required |
| Oracles | WireContract, LifecycleContract and Ownership `--oracles`: exit 0, wire `Failures: 0` |
| Anvil | `anvil check --since e36467b8`: 14 changed files, 0 warnings |

## Findings

**None of the findings is blocking.**

**F1, owner Amelia-6, Low.** cat-051's closed-pose check only repeats the full compare, and the run-time blade-height check in `assertBlocked` is conditional.

- **Fix:** anchor S0's blade read to an independent closed reference, and make the check unconditional.

**F2, owner Amelia-6, Low.** No listed suite resets from Running any more.

- P1 and P2 show the path is correct.
- **Follow-up:** adopt one Reset-from-Running cycle, the probe diffs in `mutants/P1-*` and `mutants/P2-*`, into cat-017 or cat-020.

**F3, resolved.** The coordinator amended D2 so the Load leg excludes `saved.construction.revision` by full path, and `5015326d` records it in the spec.

- I verified that Load legitimately advances the revision: `MachineWorld.Gpu.cs:148` assigns every install, Load included, `Construction.Revision + 1` from the page's own counter.

**F4, resolved.** The coordinator accepted D3 as in scope.

**F5, owner Amelia-6, Low.** The schema parser skips fields that have defaults or attributes; U7 survives.

- **Fix:** fail on any unparsed body line.

**F6, owner the Story 9R.0a runner owner, Low.** `tools/e2e/unit/*` runs under no gate.

**F7, owner Amelia-6, Low.** `PhysicsMemoryRead … = default` parameters exist on `ActivationCheckpoint`, `Consume` and `WorkshopRead`. That is a fallback-shaped API.

- **Fix:** make them required.

**F8, owner Amelia-6, Low.** The TypeScript enums `MemoryKind` and `ActivationPhase` are string constants.

**F9, owner Amelia-6, cosmetic.** `WorkshopGpuDevice.cs:155` has two statements on one line.

**F10, owner Amelia-6, partly resolved.** She stated identities for her serial and run-alone TAPs. The mutant diffs for M1b, M2, M3, M4 and M6 are still not retained.

- So the M2 kill (`activation_memory[0].count`) cannot be attributed to a specific mutation.

**F11, owner Amelia-6, Low.** `solvedAtReset` is marked after the settle frames, which makes the post-Reset goal check weak by construction.

**F12, owner Amelia-6, Low.** Every snapshot adds a menu Save to the flow under test.

**F13, owner the Story F.1 owner, resolved for 9R.1.** cat-020's Parallel clock recalibration is handled by the `b1390cd9` demotion. Whole-set recalibration stays in Story F.1.

**F14, owner Amelia-6, Low.** C1 is killed by the worker's Reset validation before the snapshot runs.

- The snapshot's own sensitivity to body changes is shown by the unit mutants and by the implementer's M1b/M4 on cat-049/051, not on cat-002/030/052.
- **Fix:** add a guard-bypassed body mutant there.
