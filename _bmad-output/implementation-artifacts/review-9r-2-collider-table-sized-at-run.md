# Review: Story 9R.2, Collider Table Sized at Run (Murdoch-8, round 1)

**Terminal verdict for round 1: PASS (scoped).** Every story AC is met on the final candidate `50ba7c72`, under the coordinator's AC5 and D2 decisions. No blocking finding is open.
- **F5 condition met:** the nine-scene trajectory hashes are byte-identical to base `8a77f9a9`.
- **No capacity-9r fault:** zero debt faults or close findings in my Tier 2 run and in the reused full run.
- **Everything below is filed.**

**Review identity**
- **Final candidate:** `50ba7c72`. That is `1f755119` (feat) plus four commits:
  - `26100c4c`: docs, the full-run evidence;
  - `2d09447e`: docs, spec risk wording;
  - `6194e9b2`: capacity-9r declared SerialAlone in `tools/E2eRunner/SuiteCatalogue.cs`, plus a README row;
  - `50ba7c72`: docs, coordinator decisions.

  Outside `_bmad-output/`, `git diff --name-only 1f755119 50ba7c72` lists only `tools/E2eRunner/SuiteCatalogue.cs` and `tools/e2e/README.md`.
- **Reviewer:** Murdoch-8, independent. I edited no deliverable. Mutants and probes ran only in my detached worktrees, `.claude/worktrees/murdoch8-9r2` (`1f755119`, then `50ba7c72`) and `.claude/worktrees/murdoch8-9r2-base` (`8a77f9a9`). Both were restored and are clean at the end.

**Ground rules**
- **Blocking rule (owner):** at most 2 rounds. Only a breach of the story's ACs, or a player-visible regression the story introduces, blocks. Everything else is filed; the default owner is Story F.4, "Collider Table Follow-ups".
- **Scope:** this verdict makes no global, runtime or performance qualification claim. Detailed performance stays at its named gate.

**Evidence**
- **Raw evidence:** `_bmad-output/implementation-artifacts/evidence-9r-2-review-murdoch8/`:
  - `tier1/`;
  - `probe-golden.cs.txt` (the probe), `golden-base.txt`, `golden-candidate.txt` and their logs;
  - `mutants.sh.txt` (exact commands), plus `mutants/` (each diff, log and failing tests, and `summary.txt`);
  - `chrome-tier2/` (summary and TAPs).

## Story AC matrix

| AC | Evidence (independent unless marked reused) | Status |
|---|---|---|
| 1. The collider table is sized from the compiled count, so every Build-admitted construction compiles | **Static:**<ul><li>`ColliderCapacity` is deleted, and the collider table is the buffer tail.</li><li>`Admission` allocates `ByteLength(scene.Colliders.Length)`.</li><li>Every consumer checks the buffer against its own header: `Header()`, `BrowserGpuTransport.Stage`, `WorkshopGpuDevice` (two checks) and `WorkshopPhysicsEngine.Advance`.</li><li>The hidden 888-pair cache ceiling is deleted along with the cache.</li></ul>**Runtime:** Tier 1 is green, and the property theory passes for all 22 Free kinds.<br>**Mutants:** R-HDR, R-RES and R-TAIL are killed. The implementer's M1 (2,048 re-added) and M2 (888 re-added) were also killed. | Met |
| 2. A native property test: 32 of each element (including 2,305-collider benches) and the 30-instance mixed case compile and step | **Test:** `EveryFreePaletteKindCompilesAndStepsAtTheLargestCountBuildAdmits`.<br>**D2 reading:** each kind at the largest count Build admits. It sits in the spec's frozen block, and the coordinator accepted it on 20 Oct.<br>**Coverage:**<ul><li>`WorkbenchFootprint.Of` has 22 kinds; all are in the budget table, which is bound to `Free`.</li><li>Tennis and Balloon have no footprint.</li><li>The mixed 30-instance bench steps 30 ticks.</li><li>The 0/28/29/32-bend boundaries pass.</li></ul> | Met (D2) |
| 3. A construction over the budget is rejected at Build with the existing message | `ThirtyThreeBendsAreRejectedAtBuildWithThePartTableMessage` rejects with the exact text "Workbench is full: part table". The implementer's M5 was killed. | Met |
| 4. In Chrome, `capacity-9r` places 32 `pipe_bend_90` through the real drawer, runs, resets exactly and save/loads, with no worker fault | **My Tier 2:** runner `20261019T214444Z-8565`.<ul><li>`sourceHead` `50ba7c72`, with the empty status digest `e3b0c442…`.</li><li>Bundle `6b15152c…` at both start and end.</li><li>capacity-9r ran **SerialAlone** and passed 3/3, with `debtFaults=0 closeFindings=0 recal=0`.</li><li>The smoke pair passed 5/5 and 1/1.</li><li>Run totals: 9/9 tests, 0 faults.</li></ul>**Reused full run:** capacity-9r passed 3/3 with all zeros (below).<br>**Test 1 coverage:** its Reset legs compare the committed read and the stored slot byte for byte, twice. Load restores each instance's id, cell and rotation.<br>**Limit:** the 32-bend bench has no moving body, so test 1's Reset compare is near-vacuous by construction. It proves the bench compiles, runs (the pose sequence advances and Pause is accepted) and save/loads without a fault. For large scenes, the exact Run/Reset and Save/Load proof is test 2, the 2,234-collider bench with one body, using the 9R.1 snapshot. The snapshot helper's body guard is filed to F.4 by the coordinator. | Met |
| 5. `dotnet build` reports 0 warnings, `dotnet test` passes 100%, and every e2e suite passes 100% in one Chrome run | **Mine, clean `1f755119`:**<ul><li>build: 0 warnings, 0 errors;</li><li>`dotnet test`: WorkerAotGuard 17, Coverage 78, E2eRunner 156 and CuriousContraptions 1729, all passing.</li></ul>**Mine, `50ba7c72`:** build 0 warnings, and E2eRunner.Tests 156/156.<br>**Reused full run** `20261019T210623Z-47887`:<ul><li>`sourceHead` `1f755119`, with an empty status digest;</li><li>bundle `314845a0…` at both start and end;</li><li>35 of 35 suite files, which equals `ls tools/e2e/*.test.ts`;</li><li>169/169 passed;</li><li>`workersUsed: 2`.</li></ul>**Coordinator decision:** the runner full run satisfies "one serial Chrome run" under the 19 Oct tiers. I agree it is acceptable for this engine change: timing suites run SerialAlone, every file has its own Chrome and preview, and capacity-9r was clean under Parallel there and SerialAlone here.<br>**Why the run still applies at `50ba7c72`:** only capacity-9r's phase changed, and my Tier 2 covers that phase. | Met (coordinator AC5 decision) |

## Coordinator design decisions

| # | Decision | Finding | Status |
|---|---|---|---|
| 1 | The dead ABI contact cache is deleted, header word 112 is zero, and nothing reads it | **Static (base):** the region was referenced only by `PhysicsGpuAbi*.cs` and `WorkshopWireTests`. The engine never read or wrote it.<br>**Static (candidate):** none of the deleted symbols is left (`rg`). `Header()` requires word 112 to be zero.<br>**Runtime:** the golden probe is byte-identical to base on 9 of 9 scenes, which proves the region and the offset shift had no physics effect. R-RES is killed by `GenericWorldLengthIsDerivedFromItsOwnColliderCount`. | Met |
| 2 | Non-vacuous proof: 31 bends plus a ball on the last bend (slots above 2,048), with a control where the ball falls | **R-SLOT:** it skips only the **static** colliders with an index above 2,048, so the ball's own collider stays. It kills exactly one test, `ABallRests…(withLastBend: True)`, with the height outside 2.5–2.9. The high slots therefore carry the contact.<br>**Chrome:** test 2 (rests in the cradle) and test 3 (control, falls) pass in my Tier 2 and in the full run. | Met |
| 3 | The transport reallocates only on a length change, and the 32 → 1 → 32 admit test exists | **Test:** `OneTransportAdmitsBenchesOfChangingColliderCount` covers 1, 32, 1, 32.<br>**Mutants:**<ul><li>R-CUR (current buffer only grows) is killed by that test, and the implementer's M3 and M3b were killed.</li><li>R-REUSE (always reallocate) **survives**, as predicted. That is a performance effect only (F3).</li></ul> | Met (F3 filed) |
| 4 | Boundaries: `Length >= HeaderBytes`, a checked u32 → int conversion, equal candidate and source counts, the u16 bound, and zero and maximum rows | **Static:** all are present, and the tests cover −1/+1 collider, ±1 byte, a `uint.MaxValue` count, a short header and a negative count. The plane-only bench (0 bends) and 32 bends pass. The u16 fit test passes.<br>**Mutants:** R-HDR is killed. R-LEN (drop the explicit length-equality check) **survives**: equal header bytes already force equal lengths, so it is redundant and harmless. | Met |
| 5 | Exact Run/Reset and Save/Load hold for large scenes | capacity-9r tests 1 and 2 pass in my Tier 2 and in the full run. Test 2 uses the 9R.1 snapshot (`bodies: 1`) over two Run/Reset cycles plus a Load. | Met |
| 6 | No player-visible regression: contacts and warm start still work without the ABI cache | **Golden probe:** each scene is hashed (SHA-256) over every committed read for 240 ticks, through `WorkshopSimulation`, `WorkshopGpuDevice` and `BrowserGpuTransport`. **Base `8a77f9a9` equals candidate `1f755119` on all nine scenes:** bumper, dominoes, gate, spring, receiver, bowling, balls16, dominoes16, springs16. All are non-vacuous (body 0 moved 0.01–5.18 m). This covers the ABI change and the broadphase change together.<br>**Further checks:**<ul><li>The base hashes also equal the implementer's post-change goldens.</li><li>The whole native suite (1729 tests) is green.</li><li>The WireContract, LifecycleContract and Ownership oracles pass, the wire oracle with `Failures: 0`.</li><li>R-PRUNE0 is killed (72 tests), so the pruning flag carries real behaviour.</li></ul> | Met |

## The F5 condition (coordinator)

The coordinator accepted the broadphase change, which reverses spec decision 6, on the condition that my hash probe matches base byte for byte on every scene.

- **Result:** 9 of 9 identical. `diff` of `golden-base.sorted` against `golden-candidate.sorted` is empty.
- **Excluded scenes:**
  - gates16 faults on base, because it exceeds the cache ceiling;
  - cradle has 2,234 colliders, over the base ceiling.

  Both are covered on the candidate by the native theory and the cradle test.
- **Limit:** the probe ran natively under Debug JIT. On the AOT side, the evidence is the Chrome cradle and control results in my Tier 2 and in the full run.
- **Condition: met.** The missing permanent pair-order test is filed to F.4 (F8).

## The capacity-9r first-fault run (F6 rule)

The run in `evidence-9r-2/chrome-capacity-9r-first-fault/` is runner `20261019T204832Z-23392`.

- **Its identity:** `sourceHead` `8a77f9a9`, with status digest `a4a552d6…`, so it was a dirty tree. It also predates the broadphase change, which the spec change log and the probe both place between that run and run `cap3`.
- **Classification:** a **superseded development state**, not the candidate code. Its 5 debt faults do not apply to `1f755119` or `50ba7c72`.
- **What does apply:**
  - the candidate's own full run (capacity-9r all zeros, Parallel);
  - my Tier 2 on `50ba7c72` (all zeros, SerialAlone).
- **Limit:** I cannot rebuild that dirty tree from its digest, so the attribution relies on the commit order and the probe record.
- **Status:** not blocking. The residual headroom concern stays filed (F6).

## Tier 1 (`tier1/`)

| Check | Result |
|---|---|
| Build on `1f755119` | 0 warnings, 0 errors |
| `dotnet test` on `1f755119` | 17 + 78 + 156 + 1729, all passing |
| Build on `50ba7c72` | 0 warnings; E2eRunner.Tests 156/156 |
| Node tests | `node --experimental-vm-modules --test tools/*.test.mjs`: 82/82 |
| Oracles | **Wire:** `--oracles` exits 0 with `Failures: 0`.<br>**Lifecycle:** exits 0.<br>**Ownership:** `--oracles` exits 0. The `--oracles .` form fails in a fresh worktree with `CS0006` (no Release reference assemblies); that is environmental, and the 9R.1 record used the form without the path. |
| Anvil | `anvil check --since 8a77f9a9`: no warnings (it checks only the one JS/TS extension file) |

## Mutants (`mutants/`, each applied alone to `1f755119`, native suite, then reverted)

| Mutant | Change | Result |
|---|---|---|
| R-SLOT | Skip BVH insertion for static colliders with an index above 2,048 | **Killed**, 1 test: `ABallRests…(True)` |
| R-RES | Drop the check that word 112 is zero | **Killed**, 1 test: `GenericWorldLengthIsDerivedFromItsOwnColliderCount` |
| R-LEN | Drop `candidate.Length != source.Length` | **Survives**, as predicted: redundant with the header equality |
| R-CUR | `Commit()` lets the current buffer only grow | **Killed**, 1 test: `OneTransportAdmitsBenchesOfChangingColliderCount` |
| R-REUSE | Always reallocate the candidate buffers | **Survives**, as predicted: performance only (F3) |
| R-TAIL | Compare only the first collider record of the tail | **Killed**, 1 test: `GenericCandidateRejectsAChangeAnywhereInTheColliderTail` |
| R-HDR | Drop `data.Length != ByteLength(data)` | **Killed**, 1 test: `GenericWorldLength…` |
| R-PRUNE0 | Flag every leaf static | **Killed**, 72 tests, including the cradle test and the contact-work laws |

## Other static checks

- **The physics world stays inside .NET.** No JS/TS code reads it: `worker.js` reads only the response wire, and `workshop-client.js` reads flatbuffers and the clock and pose rings.
- **No stale docs.** No active doc states the 2,048 ceiling or describes the contact cache.
- **No dangling `ref` locals.** In `WorkshopDynamicBvh`, no `ref` local survives an `AllocateNode` that can resize the array.
- **The SerialAlone commit is minimal.** `6194e9b2` replaces one `Parallel(...)` declaration with one `Serial(...)` declaration that names its owner, and adds a README row. The selection still resolves: `select` lists capacity-9r plus the smoke pair.

## Findings (none blocking; all filed)

- **F1 (F.4).** `WorkshopPhysicsEngine.cs:362` writes the owner collider slot with an unchecked `(ushort)colStatic.Index`. The u16 fit test covers the cast today; a checked cast would fail fast if a future footprint broke the fit.
- **F2 (F.4).** `ContactWorkOccurrence.Validate` no longer has its `Collider < 2048` clause.
  - A `Sequence == 0` row's collider field is now unbounded within u16.
  - Rows whose sequence has not changed are not compared at all; that gap predates this story.
  - Not player-visible.
- **F3 (F.4).** No test pins buffer reuse when the length is unchanged; R-REUSE survives. The cost is performance only.
- **F4 (F.4).** Past about 33M colliders, `ByteLength(int)` throws `OverflowException` instead of `ArgumentException`. Build cannot reach that size.
- **F5 (coordinator).** The condition is met; see above.
- **F6 (performance gate).** The remaining per-tick static re-insertion cost is filed. It was 2.79 ms natively for 32 bends, and the coordinator filed the static BVH built once to F.4. capacity-9r is now SerialAlone until calibration qualifies it.
- **F7 (closed).** The implementer's `chrome-capacity-9r/` evidence came from an uncommitted tree. It is superseded by the full run on `1f755119` and by my Tier 2 on `50ba7c72`.
- **F8 (F.4).** No permanent test pins pair order or trajectories. The goldens exist only as probes, hers and mine (`probe-golden.cs.txt`).
- **F9 (owner: Story F.1, clock recalibration).** In the reused full run, clock-master shows `wideOffset=1`.
  - **First observed on this candidate, unexplained.** I checked every clock-master summary on disk. The 9R.0a calibration and spin probe, the 9R.0b full run and its review Tier 2, and both 9R.1 full runs (serial and parallel) all show `wideOffset=0`.
  - Its other counters match the declared baseline: `closeFindings=1 unresponsive=1 recal=4 debtFaults=1`.
  - The suite passed 9/9, running SerialAlone first in the run, with no other Chrome.
  - Story 9R.2 changes no clock code.
  - It is filed as a clock observation. It is not a blocking 9R.2 regression, because no player-visible behaviour is affected and the suite passed.
- **F10 (admin).** The spec front matter still reads `status: 'ready-for-dev'`.
