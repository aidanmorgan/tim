# Review: Story 9R.4, each Pressure Plate honours its own minimum mass through its own compiled row

- **Reviewer:** Murdoch-10, the independent adversarial reviewer. I changed no deliverable.
- **Candidate:** worktree `.claude/worktrees/agent-af64068d5450f89f6`, commit `c5a1cdda` on base `647f5d72`.
- **Round:** 1 of at most 2.
- **Owner rules:** only a breach of the story's own ACs (epics "### Story 9R.4", release AC amended 20 Oct: release on the next tick, hold in 9R.5), or a player-visible regression this story introduces, blocks. Everything else is filed to Story F.6 "Contact-Load Follow-ups".
- **Status of this record:** static review complete. Builds, Tier 1, mutants and Chrome are **pending "CHROME CLEAR"** from the coordinator (her Tier 2/full run holds Chrome).

## Verdict: **Pass** (round 1; scoped to Story 9R.4 at `15b7431b`, reviewed on the clean evidence commit `d7ab0e3e`)

- No blocking finding remains. B1 was fixed in `6a9d54aa` and proven by mutant S1b. The P1 probe passes, and both branches are killed by existing tests.
- The owner's new first AC (no activation-slot limit) is met. Mutant C8 (restore the cap) is killed by 8 tests.
- **My runtime evidence, all on a clean tree:**
  - Tier 1 is green, with 0 warnings.
  - All 10 mutants are killed. Two did not compile on their first form (S1, RAT) and were replaced by S1b and RAT2.
  - My Tier 2 Full run passes 37/37 suites and 185 tests.
  - cat-052 and cat-052-two-plates pass serially with `--workers 1`.
- **Her full run** is supporting evidence only: its status digest was not clean.
- **AC10's "one serial Chrome run"** is met under the runner model, as accepted for 9R.3: SerialAlone suites run alone and Parallel suites run on 2 workers. I did not run `full --workers 1`.
- **Filed:** F-1, F-3, F-4 and F-6 to F-8 go to F.6. The fixed 32-instance worker response buffer goes to 9R.5b.
- This is not a global or runtime qualification claim.

## Update, 20 Oct 2026 (coordinator message: owner decision on activation slots)

- **New first AC (owner decision, 20 Oct 2026):** no limit on activation slots. The fixed 8-slot table must go; activation slots and memory are sized from the compiled scene at Run; Build refuses only at ordinary Free-palette part maxima; proven by a bench at the palette maximum of plates plus counters, clocks and latches that compiles, steps, Resets exactly, survives Save/Load, and whose last plate opens its own gate. My F-5 is promoted to this AC. **Status: delivered in `15b7431b`; verified statically below, runtime pending.** The static review covered: every `ActivationNetwork.Capacity` / `ContactLoadSensorCapacity` / fixed-offset consumer (ABI contact-load table, activation/timer/memory tables, `stackalloc` spans in the engine and device, `WorkbenchCapacity` activation check), the capacity tests, and the max-palette bench.
- **`de8e6c14` (SDK 10.0.402 gate re-pin), verified statically: exact pins only.** Six entries, each an exact relative path under the NuGet root including the version (`microsoft.netcore.app.ref/10.0.12/analyzers/dotnet/cs/<file>.dll`) and an exact SHA-256, admitted only in the three browser-wasm compiles (Simulation, Animation.Worker, web). No wildcard, prefix or version range; the new clause sits only in the NuGet-root branch of the ternary (the `?:` binds looser than `||`). I hashed all six local files under `~/.nuget/packages/microsoft.netcore.app.ref/10.0.12/analyzers/dotnet/cs/`: every SHA-256 matches its pin. Tier 1 (`CSharpStaticGateTests`) pending CHROME CLEAR.
- **Final candidate is `15b7431b`** (on `6a9d54aa` B1 and `de8e6c14` pin). The Impact Switch's 8-entry trigger table is out of scope (coordinator: Story 9R.5b), not a finding.
- **`15b7431b` (no activation-slot limit), verified statically:**
  - `ActivationNetwork.Capacity`, `WorkbenchTable.ActivationNodes`, the footprint field, `WorkbenchCapacity.ValidateConnected` and the Build activation check are deleted; `rg "ActivationNetwork\.Capacity|ActivationNodes"` finds nothing.
  - Network-local tables (`Clear*`, `Consume` values/timers/rows/pending/latch spans) are sized from the network's own population; the committed reads (`PhysicsActivationRead`, `PhysicsTimerRead`, `PhysicsMemoryRead`) are exact arrays; the device's passage span is sized from the declared sensors.
  - The remaining bound is `WorkshopInstances.Capacity` (32) for the wire sections, contact-load ABI rows and browser feedback arrays. The network constructor enforces one node per owner, so nodes ≤ instances and this never refuses what Build admitted. The `byte` `Count` holds 32.
  - Proof: `ABenchOfManyActivationPartsRunsResetsAndSavesWithNoActivationLimit` (13 plates + 5 counters + 5 clocks + 5 latches = 28 activation nodes, plus gate, battery and ball: 33 bodies with the plane, the body table's maximum). The 13th plate opens its own gate while the other 12 stay Clear; Reset equals the built state bit for bit; the save codec round-trip is identical and replays the same final state. `WorkbenchCapacityTests` lets every activation kind reach 32 alone. Chrome: cat-052-two-plates test 4 places 24 activation parts through the palette. "The palette maximum" is read as the 32-instance cap per kind, and for the combined bench, the most the fixed physics tables hold; neither is an activation limit.
  - Filed **F-7**: the committed reads now allocate three arrays per tick (formerly inline storage). The detailed allocation/performance gate belongs to the named qualification phase, so this is not blocking.
- **B1 fix `6a9d54aa`, verified statically.** The 2.0 kg rows of `TheInstanceMinimumMassDecidesItsOwnGate` now round-trip the bench through `WorkshopSaveCodec.Encode/Decode`, assert the reloaded `MinimumMass` bit-identical to 2.0f, and run the reloaded construction. B1 is resolved subject to Tier 1 and my own save-drop mutant.

## Static review: scope and deletions

| Deletion named by the story | Where | Status |
|---|---|---|
| Global `_plateLoaded` and its 0.5 kg literal | `WorkshopPhysicsEngine.cs` (field, Reset, detector, drive call) | Gone. `rg "_plateLoaded\|currentPlateLoad\|isPlatePowered\|0\.09375"` finds only the ratchet's own scanner fixture (`WorkshopPhysicsLawTests.cs:655`) and comments |
| Half-extent plate detection | `WorkshopPhysicsEngine.cs` old `:834-861` | Gone; replaced by `MeasureContactLoads` over the compiled rows |
| Plate drag band and 0.09375 encoding | `PrismaticConstraintSolver.ActuatorSliderDrive` (parameter `plateLoaded` deleted), `WorkshopPhysicsCompiler` blade drag now 0 | Gone |
| 9R.4 ratchet entries | `WorkshopPhysicsLawTests.KnownViolations` | Gone, with the `Story9R4` owner (see decision 1) |
| 9R.3 interim plate `Direct` row | `SupplyFeed` | Gone: every switch (latch, counter, plate) yields `Switched` via that switch |
| `Half` threshold fields | `ContactLoadSensorDeclaration` | Now f32 (`float MinimumAlignment`, `float MinimumMass`, plus `MaximumGap`) |

Live path (AC1): compiler emits one `ContactLoadSensorDeclaration` per plate from `plate.MinimumMass` (face collider `first + 2`) → `PhysicsGpuAbi` contact-load table (16-byte header + 8 × 64-byte rows; `CollidersOffset` moved after it) → engine reads each row once per tick, builds static-side `ContactPointRecord`s every substep and calls `ContactLoadSensorTracker.Measure` (D2 max, written over the copied state) → `WorkshopGpuDevice` reads every row and passes them to `ActivationNetwork.Consume` → `CommitContactLoads` sets the plate's `ContactLoadSource` level → `PhysicsTickInputs.Compose` closes that gate's `Switched` route at N+1. The device is the only production `Consume` caller (`rg "\.Consume\("`), so the new "every row must be read" throw cannot hit an unpatched production path.

No non-C# consumer of the world layout exists (`rg CollidersOffset|PrismaticRecordsOffset` outside `.cs`: none), so the 528-byte ABI shift has no TS/WGSL reader to break. The browser activation presenters (`BrowserWorkshopClient.Activation.cs`) index their per-slot arrays by the read's own slot and skip owners with no cosmetic declaration, so the plate's new slot only shifts indices consistently.

## Coordinator design decisions

| # | Decision | Finding |
|---|---|---|
| 1 | Ratchet: six 9R4 entries and the 9A19 drag-band entry deleted; `gatePowered\|plateLoaded` re-pinned; SHA and maxima updated | **Matches the spec change log, not the brief's wording.** The diff deletes the 6 `Story9R4` entries, the 9A19 drag-band entry **and** the `gatePowered\|plateLoaded` entry (its pattern now matches 0 times in `PrismaticConstraintSolver.cs`). The spec's 20 Oct change log item 1 says exactly that ("its count fell to 0, so it is deleted, not pinned at 0"). Remaining: `2.8f\|14f` = 4, `fSpring` = 2 → 2 entries / 6 matches, equal to the new maxima. SHA to be confirmed by Tier 1 (pending). Wording mismatch noted for the coordinator, not a finding |
| 2 | Release on the next tick | **Verified statically.** Engine overwrites each row's phase/mass every tick (`loadMass` reset per tick); `CommitContactLoads` clears the level on any non-Loaded phase; the route reads it at N+1. Native: `TheContactLoadLevelFollowsEachTicksPhaseAndItsRouteReadsItNextTick` (one-tick load closes then opens on the very next tick), `ALoadRolledOffATiltedPlate…` |
| 3 | Gap field: only gap ≤ d counts; 0.002 m above reads Empty | **Verified statically.** `contact.Gap > sensor.MaximumGap` skips; tracker test covers 0.002, d and the next f32 above d. Live path never exercises a lifted body (mutant m5 is killed only by the tracker test); acceptable as the decision names the tracker row |
| 4 | Region = face collider + upward normal + gap, not a y band; resting domino counts (Underweight), two sum | **Verified statically.** The y band is gone; `Face` + `MinimumAlignment` (1 − 10⁻⁶) + `MaximumGap` + x/z footprint remain. Two dominoes sum (`OneDominoIsUnderweightAndTwoDominoesSum(2)`); the single domino's Underweight is proven only indirectly (see F-3) |
| 5 | Measured and committed mass f32; minimum one f32 above the body mass reads Underweight | **Verified statically.** `PhaseOf(float)` compares raw f32; ABI stores f32 mass and rejects a phase that disagrees with it; `AMinimumOneF32AboveTheBodyMassStaysUnderweight` (live) and `ContactLoadPhase_IsTheInclusiveF32Comparison` |
| 6 | Matrix: side-by-side balls, stacked (direct contact only), box bridging two plates (both count, both gates open) | **Present:** `TheInstanceMinimumMassDecidesItsOwnGate(2, 2)`, `OnlyDirectContactCountsForAStack` (two flat dominoes; 0.4 < 0.5 < 0.8 discriminates), `ABoxBridgingTwoPlatesLoadsBoth` |
| 7 | PoweredGateTests pin → `Switched` with `Switch = plate`; no-battery → no route; plate-with-no-gate and no-supply controls | **Present:** `PhysicsCompiler_PlateChainCompilesASwitchedRowThroughThePlate` (also checks Compose closes only with enable ∧ contact), `PhysicsCompiler_APlateWithoutABatteryOrWithoutAGateCompilesNoRoute`, `ALoadedPlateWithoutSupplyLeavesItsGateClosed`, `APlateWithNoGateStillCommitsItsLevel` |
| 8 | Engine harness proves the live path; impostor next to a real 2.0 kg plate and its gate; ball on impostor leaves gate closed | **Present:** `ASizeImpostorNeverSwitchesAGate` (with a control that the ball rests on the impostor face). Red evidence `evidence-9r-4/red-native-live-path.txt` shows it failing on base |

## Acceptance criteria

**Final status at `15b7431b`, with the runtime evidence below:** every AC is **Met**.

- **AC0 (new, no activation-slot limit):**
  - static review is in the Update section above;
  - mutant C8 is killed;
  - cat-052-two-plates test 4 passes in both of my runs.
- **AC1–AC6 and AC8:** Tier 1 is green; the mutants are killed; Chrome cat-052 and cat-052-two-plates pass in both of my runs.
- **AC7:** met by `6a9d54aa` and proven by mutant S1b.
- **AC9:** met by my `--workers 1` run.
- **AC10:**
  - build has 0 warnings;
  - `dotnet test` is 100%;
  - every e2e suite passes in one Full run under the runner model, as accepted for 9R.3.

The table below is the round-1 static assessment, kept as written.

| AC | Static evidence | Status (round-1 static) |
|---|---|---|
| 1. Each plate emits its own compiled row; the tracker evaluates it live | Compiler/ABI/engine/device path above; `PhysicsCompiler_EmitsEachPlatesOwnRowAndASwitchedRouteThroughIt`, `PhysicsAbi_AdmitsEachRowAsF32WithAnEmptyCommittedState`, live-path benches | Met statically; runtime pending |
| 2. Plate contact is a switch in the switched-supply route table | `SupplyFeed` → `Switched` via plate; PoweredGate + PressurePlate compiler tests | Met statically |
| 3. Domino Underweight/gate closed; basketball closes; exactly-at-minimum closes; two bodies sum | Native benches; Chrome cat-052 tests 4 and 6 | Met statically; runtime pending; orientation coverage gap under probe (P1 below) |
| 4. Removal opens the contact on the next tick (hold in 9R.5) | Decision 2 | Met statically |
| 5. Two plates independent | `TwoPlatesSwitchOnlyTheirOwnGates`; Chrome cat-052-two-plates tests 1–3 | Met statically |
| 6. Size impostor never switches | Decision 8 | Met statically |
| 7. Native test with non-default `minimum_mass` 2.0 kg, **saved and loaded**, proves the instance value governs | The 2.0 kg live bench builds the instance directly; `RunPhases` → `WorkshopSimulation.Construct` → `_device.Admit` passes no codec. The codec round-trips use 3.5, 4.25 and 2.25 and never run a plate afterwards. No native test saves and loads a 2.0 kg plate and then shows it governs | **Not met as written — B1** |
| 8. Run/Reset matches the snapshot; Save/Load preserves `minimum_mass` | Chrome `expectResetMatches` in every cat-052 / two-plates test; two-plates test 3 compares saved minimums; cat-052 test 6 asserts 0.5 | Met statically; runtime pending |
| 9. cat-052 and cat-052-two-plates 100% in Chrome serially | Implementer's pre-commit manual two-plates run 3/3 | Pending my run |
| 10. Build 0 warnings, `dotnet test` 100%, every e2e suite 100% in one serial run | Implementer's Tier 1 log (1759/156/78 passed) | Pending |

## Findings

### Blocking

None open. B1 below was resolved by `6a9d54aa` (verified at runtime: my mutant S1b is killed by both 2.0 kg rows).

- **B1 (AC7), resolved.** No native test saves and loads a non-default 2.0 kg `minimum_mass` and then proves it governs. Fix (small, inside the story): in the 2.0 kg rows of `TheInstanceMinimumMassDecidesItsOwnGate`, round-trip the construction through the save codec (the FlatBuffers construction path the Workshop saves to IndexedDB), assert the decoded plate's `MinimumMass` is bit-identical 2.0, and run the **decoded** construction.

### Probe (blocks only if it fails)

- **P1 resolved: not blocking; both branches are covered.** Mutant P1a (A-static branch) is killed by the box–box rows (`ABoxBridgingTwoPlatesLoadsBoth`, two dominoes); P1b (B-static branch) is killed by 8 sphere rows. So the kernels already route sphere–box and box–box contacts through opposite branches. The load-before-plate probe (`evidence-9r-4-review/probe/`) passes 3/3 on the clean candidate: a basketball (id 1) on plate id 4 opens its gate; two dominoes (ids 1, 2) on plate id 5 sum and open it; one domino before the plate stays closed at 0.5 kg and opens at 0.3 kg (counted, not Empty).
- **P1 (AC3, player-visible), as raised.** `MeasureContactLoads` has two branches (static body on side A, or on side B) with opposite normal signs. In every native bench and both Chrome suites the load is placed after the plate (higher id), so one branch may never be exercised. To run after CHROME CLEAR: negate the normal in each branch separately; for any surviving mutant, probe a bench whose load id is lower than the plate's, with a basketball (sphere–box) and a standing domino (box–box).

### Filed to Story F.6 "Contact-Load Follow-ups"

- **F-1.** Placement: a domino placed through the palette onto a plate lands at the 3 m placement plane and straddles the face (y 3.08); two unlifted dominoes did not open the gate (`evidence-9r-4/manual-cat-052-pre-commit-domino-unlifted-fail.txt`). cat-052 now lifts them with the move gizmo. Pre-existing placement behaviour (no drop-onto-surface), not introduced here; a player stacking boxes on a plate through the palette alone will not load it.
- **F-2 (withdrawn after runtime).** `TheContactLoadLevelFollows…`'s final `Assert.Throws<ArgumentException>` calls `Consume` at boundary 5 after tick 6. My mutant F2 (delete the missing-read throw) is killed by exactly that test, so no stale-boundary rejection masks it.
- **F-7.** The committed activation/timer/memory reads now allocate three arrays per tick (formerly inline storage). The detailed allocation gate belongs to its named qualification phase.
- **F-8.** The face filter and the alignment threshold are killed only by tracker unit tests (mutants FACE, ALIGN), never by a live bench (no live bench rests a load against the base or on a tilted load face). Unit coverage satisfies the ACs; a live side/base contact bench would close it.
- **Filed to Story 9R.5b (coordinator, 20 Oct 2026), not F.6:** the response wire sections and the contact-load ABI rows are still sized to the fixed 32-instance worker response buffer; the owner ruled that physics/worker tables have no fixed capacities.
- **F-3.** The single-domino and stacked rows assert only "contact open, gate closed"; `ContactClosed` cannot distinguish Empty from Underweight, so Underweight is proven only indirectly (two-domino sum, mutant m4, Chrome `assertDominoesStand`). A read of the committed phase/mass would make it direct.
- **F-4.** `ContactLoadSensor(owner)` and `PassageSensor(owner)` share `PartIdentities(owner) + 6`. Safe today (a plate has no passage sensor; `Identity()` rejects any collision), but a future element with both would collide.
- **F-5 (promoted to the new first AC by the owner, 20 Oct 2026; no longer a filed item).** D3 capacity: each plate now takes one of the 8 activation nodes, so a bench with more than 8 plates (or plates plus lamps/counters/latches/clocks) that Built before is now refused with the activation-table message. Accepted by spec D3; recorded for the capacity follow-up.
- **F-6.** The alignment 1 − 10⁻⁶ (≈0.08°) means a box contact whose solver normal comes from a slightly tilted load face is not counted; CAT-052 §2 value, kept by design, but worth a qualification check when rotated/rocking loads are taught.

## Runtime evidence

Evidence: `_bmad-output/implementation-artifacts/evidence-9r-4-review/` (main checkout).

### Tier 1 (`tier1/`)

Detached clean worktree `.claude/worktrees/murdoch10-9r4` at `d7ab0e3e` (`git status --porcelain` empty; `d7ab0e3e` adds only evidence on top of `15b7431b`).

- `dotnet build CuriousContraptions.slnx`: 0 warnings, 0 errors.
- `dotnet test CuriousContraptions.slnx`: WorkerAotGuard 17, Coverage 78, E2eRunner 156 and CuriousContraptions 1759, all passing. That includes the ratchet with the new SHA and maxima (decision 1) and `CSharpStaticGateTests` with the SDK 10.0.402 pins.

### Mutants (`mutants/`, scratch worktree `murdoch10-9r4-mut` at `d7ab0e3e`, each alone, then `git checkout -- .`; script `mutants.sh.txt`, diff at the top of each log)

| Mutant | Change | Result |
|---|---|---|
| P1a | A-static branch: normal not negated | **Killed** by 2 (box–box: bridge, two dominoes) |
| P1b | B-static branch: normal negated | **Killed** by 8 (sphere rows, two plates, removal, no-gate, no-supply, many-parts bench) |
| F2 | Drop "every row must be read" | **Killed** by `TheContactLoadLevelFollows…` (F-2 withdrawn) |
| S1 | Save writes the placed default | Did not compile (IDE0059); replaced by S1b |
| S1b | Save writes `min(value, 0.5)` | **Killed** by both 2.0 kg rows (B1 fix proven) and the FlatBuffers round-trip |
| C8 | Restore an 8-node activation cap | **Killed** by the many-parts bench and all 7 activation kinds at 32 |
| FACE | Count any frame collider, not only the face | **Killed** by the tracker unit test only (F-8) |
| ALIGN | Alignment 0.7 instead of 1 − 10⁻⁶ | **Killed** by 2 tracker unit tests only (F-8) |
| RAT | Element-named drive parameter | Did not compile (IDE0060); replaced by RAT2 |
| RAT2 | Element-named local in the drive | **Killed** by `TheShippedSolverNamesNoElementBeyondThePinnedOwnedViolations` |

The implementer's 10 + 3 native mutants (`evidence-9r-4/mutants-native.txt`) cover the instance value, f32 rounding, the inclusive comparison, per-point counting, the gap, a global flag, size recognition, the clear and its timing, and the Direct route. I did not repeat them.

### P1 probe (`probe/`)

`ProbeP1LoadBeforePlate.cs.txt`, pasted into the scratch worktree's `PressurePlateTests` and run against the clean candidate: 3/3 pass (`probe-clean.log`).

### Tier 2 (`tier2/`)

`run --diff 647f5d72` selects **Full** ("Tier 3 required: yes"), because the solver, workers and wire changed. My first attempt was refused with `RunnerBusy` (`tier2-runner-busy-1.log`): another agent's 9R.5 full run held `.git/e2e-runner.lock`. The second, queued behind it, was refused with `OutputExists` (`tier2-runner-outputexists.log`). The third ran.

**Full run `tier2-full-1/`:**

- **Runner:** `20261020T125323Z-38472`, Verdict **Pass**.
- **Identity:**
  - `sourceHead` is `d7ab0e3e`.
  - The status digest is `e3b0c442…`, the empty-status hash, so the tree was clean.
  - The suite source digest `356c3634…` is the same at start and end, and it equals the implementer's.
  - Bundle `a59e02ce…` is the same at start and end, and both served checks pass (285 files).
- **Workers:** the runner model, 2 workers.
- **Result:** 37/37 suites, 185/185 tests.
  - anim-1c was observed "Contended" and re-run "Clean" by the runner. It passed both times.
  - clock-master passed 9/9 with recal 4, 1 debt fault, 1 close finding and Unresponsive 1. **Pre-existing and by design, not a regression.**
    - The clock *is* inside this story's closure since `15b7431b`, which rewrote the memory and timer reads and wires, the timer feedback arrays and the animation target capacity. So I checked against a baseline instead of relying on a closure argument.
    - The `Tap` block is identical, field for field, in three runs: the 9R.3 run before this story (`evidence-9r-3/full-run-1/summary.json`, on `bcc83118`), my run, and hers. In all three: CloseFindings 1, Unresponsive 1, Recalibrated 4, RenewedLate 1, DebtFaults 1, Observation Clean.
    - The observations are the suite's own injected faults: test 4 "A Simulation spin beyond the liveness bound faults the session as SimulationUnresponsive" and "A … ms Simulation block while Running takes the declared debt stop" (`tier2-full-1/clock-master.tap`).
  - No other suite had a recalibration, debt fault or close finding.
  - cat-052 7/7, cat-052-two-plates 4/4 (including test 4, the 24 activation parts), cat-037 6/6, cat-051 5/5, cat-051-own-supply 6/6, capacity-9r 3/3, palette-placement 25/25.

**Serial run `tier2-serial/` (AC9):**

- **Runner:** `20261020T132439Z-7653`, `run --changed serial-changed.txt --workers 1`, Pass.
- **Identity:** clean tree at `d7ab0e3e`; bundle `45ff83c8…` is the same at start and end.
- **Order:** one after another, with no overlap: cat-052-two-plates 4/4 (13:24:40–13:26:16), then cat-052 7/7 (13:26:16–13:28:03), then the smoke pair 5/5 and 1/1.
- An earlier attempt passed the list inline and was refused with `InvalidChangedPath`. Its directory holds only a publish log (`tier2-serial-attempt-invalid-path/`).

**Node:** `node --experimental-vm-modules --test tools/*.test.mjs`: 82/82 (`tier1/node.log`).

### Reuse of the implementer's full run (`evidence-9r-4/full-run`)

- **Run:** `20261020T114628Z-30179`, Full.
- **Identity:**
  - `sourceHead` is `15b7431b`.
  - The bundle `94a942c9…` and the suite source digest `356c3634…` are equal at start and end.
  - The served checks pass.
  - 37/37 files pass: cat-052 7/7, cat-052-two-plates 4/4, cat-037 6/6, cat-051 5/5, cat-051-own-supply 6/6.
  - clock-master passes 9/9, with recal 4, 1 debt fault and 1 unresponsive close finding. Its `Tap` block is identical to the 9R.3 baseline and to my run: the suite's own injected faults (see Tier 2).
- **Identity gap:** `sourceStatusDigest` is `4f463479…`, not the empty-status hash `e3b0c442…`, so her tree was **not clean** when the run started. I could not reconstruct what was dirty. Her tree is clean now at `d7ab0e3e`, and the suite source digest matches the committed tools. Her run is therefore supporting evidence only. AC10 rests on my clean Tier 2 Full run, which agrees with hers suite for suite, on the same 37 suite files.
