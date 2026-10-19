# Review: Story 9R.3, each directly wired Powered Gate follows its own battery

- **Reviewer:** Murdoch-9, the independent adversarial reviewer. I changed no deliverable.
- **Candidate:**
  - Code commit `bcc83118` on base `9b9d26e5`.
  - Final candidate `3414f836`. The two later commits only change scheduling: `e7f5b2b9` makes cat-051-own-supply SerialAlone and `3414f836` makes cat-062 SerialAlone. Apart from `SuiteCatalogue.cs`, the `tools/e2e/README.md` rows and the evidence and spec files, nothing differs from `bcc83118`.
- **Round:** 1 of at most 2.
- **Owner rules:** only a breach of the story's own ACs (epics "### Story 9R.3"), or a player-visible regression this story introduces, blocks. Everything else is filed to Story F.5 "Supply Route Follow-ups".
- **Evidence:** `_bmad-output/implementation-artifacts/evidence-9r-3-review/` (main checkout).

## Verdict: **Pass** (scoped to Story 9R.3; no blocking findings)

- No AC is breached.
- No player-visible regression was found: the probe shows that the ball ejection already happens on base (S1).
- Coordinator decision 2 is not met as written: mutant #4 does not fail `WorkshopSpringboardTests`. Its intent is met, and the deviation is filed.
- This is not a global or runtime qualification claim.

## Acceptance criteria

| AC | Evidence | Status |
|---|---|---|
| 1. Battery wired straight to a gate through real UI compiles a direct supply route row in the 9.4 route table, and its powered state is the committed `ConfigureElectrical` state | **Static:**<ul><li>`SupplyFeed` gives a Battery → `SupplyRouteKind.Direct` row.</li><li>Admission checks the switch per kind.</li><li>`Compose` reads `controls.SourceEnable[route.Source]`.</li></ul>**Native:** `PhysicsCompiler_DirectBatteryCompilesOneDirectRouteWhateverItsAuthoredEnable` (both authored enables) and `Compose_ClosesADirectRouteOnlyWhileItsOwnSourceIsEnabled`.<br>**Chrome:** cat-051-own-supply builds every bench through the UI and checks the committed enable with `committedEnable` after each toggle | Met |
| 2. Authored Disabled opens on in-run Enable supply; Enabled closes on in-run Disable | **Native:** `AnAuthoredDisabledBatteryOpensItsGateWhenEnabledInRun` and `AnEnabledBatteryClosesItsGateWhenDisabledInRun`, both through `ConfigureElectrical` with pre-asserts.<br>**Chrome:** own-supply tests 1–3, which include Enable while Paused and then Resume.<br>**Mutants:** mine (M4) and the implementer's 2, 3 and 6 are killed | Met. The ball does not pass on this bench (S1/F1); the epics AC does not require it |
| 3. Two batteries and two gates are independent | **Native:** `TwoCircuitsAreIndependent` (G2 open in every read), plus a compiler test with distinct sources.<br>**Chrome:** own-supply test 4, with `assertStaysOpen` taking the minimum over every sample.<br>**Mutant:** the implementer's mutant 1 ("any source enabled") is killed by 3 tests | Met |
| 4. An unpowered gate still blocks the ball | **Native:** `AnUnpoweredGateStaysClosed`, with an unwired battery enabled in the run.<br>**Chrome:** own-supply test 5, and cat-051 test 2 | Met |
| 5. Run/Reset matches the pre-Run snapshot; Save/Load keeps both wires | **Chrome:** `expectResetMatches(s0)` in every own-supply test, including the committed electrical enable and the blade's closed pose; test 6 checks the wires and replays the loaded bench.<br>**Native:** the D4 test (`ACommittedResetClearsInRunSupplyControlsAndACancelledOneKeepsThem`), with M5 killed | Met |
| 6. cat-051 and cat-051-own-supply pass 100% in Chrome serially | **My serial Tier 2:** runner `20261019T231737Z-70937` with `--workers 1` (`workersUsed: 1`), on clean `3414f836`. It passed cat-051-own-supply 6/6, then cat-051 5/5 (slot 0), with no suite overlapping in time. Both also passed in my 2-worker Tier 2 and in her full run | Met as written |
| 7. Build has 0 warnings, `dotnet test` is 100%, and every e2e suite passes 100% in one serial Chrome run | Tier 1 is green on `3414f836`. **The e2e pass follows the coordinator's composite instruction (20 Oct):** her full run passed 35 of 36 files on `bcc83118` (runner model: SerialAlone suites alone, Parallel suites on 2 workers), and my runs passed the 36th, cat-062, on `3414f836` (SerialAlone, 7/7, twice). This is not one all-serial run of every suite. `full --workers 1` exists if the coordinator wants the literal single serial run; I did not run it | Met per the coordinator's composite instruction |

## Coordinator decisions

| # | Decision | Finding |
|---|---|---|
| 1 | Ratchet: 9R3 rows deleted, 9A19 rows re-counted, SHA recomputed | **Verified.**<ul><li>4 rows deleted, among them the travel-limit recognition row, which 9A19 owned (its code is gone).</li><li>Literals 5 → 4 and identifiers 7 → 4, matching the solver text.</li><li>Totals 14 → 10 entries and 50 → 40 matches.</li><li>Tier 1 is green with the new SHA.</li></ul>**Guard mutants:**<ul><li>R1 re-adds the 0.03–0.08 band: killed by `TheShippedSolverNamesNoElementBeyondThePinnedOwnedViolations`.</li><li>R2 edits one pinned entry's text: killed by `TheKnownViolationListIsFrozenAndCanOnlyShrink`.</li></ul> |
| 2 | Mutation #4 means "drop the `SupplyClosed` check" and fails `WorkshopSpringboardTests`; a native unpowered-gate test exists | **Letter not met, intent met.** My M4 (`if (true)`) ran with `WorkshopSpringboardTests` in the filter. It is killed by 4 engine tests: `AnUnpoweredGateStaysClosed`, `TwoCircuitsAreIndependent`, `AnAuthoredDisabled…` and `AnEnabled…`. `WorkshopSpringboardTests` passes under it, as the Triage Log says. Filed (F2) |
| 3 | D4 native Reset test; controls clear only after `_device.Commit` | **Verified.** My M5 (skip the clear) is killed by the D4 test, and the implementer's 5b (clear before commit) was killed by it too |
| 4 | Tests go through `ConfigureElectrical` and Advance, with the pre-asserts, in native and Chrome | **Verified statically.** No input is hand-set. Pre-asserts: closed throughout before Enable; both open before B1 is disabled; G2 open throughout afterwards |
| 5 | Plate-fed drag band is a named interim exception owned by 9R.4/9A.19 | **Verified**, with an owner-label mismatch (F3). My M7 (drop the plate band) is killed **only** by the ratchet count. No native behavioural test covers the interim; only Chrome cat-052, which passed in her full run, does (F4) |
| 6 | Supply is enable only; the zero-charge claim is dropped | **Verified.** On base the deleted global flag also read only the enable word, so this is not a behaviour change. The element doc still lists it as an open question (F5) |
| 7 | `Compose` checks `Kind == Direct` before the contact | **Verified.** The implementer's mutant 6 is killed by `Compose_ClosesADirectRouteOnlyWhileItsOwnSourceIsEnabled` (its `NoContact` throws) and 3 engine tests |

## S1 probe: is the ball ejection new? (`probe/`)

- **Method.** The probe (`probe-ball-ejection.cs.txt`) runs through the production `WorkshopSimulation`, with in-run toggles sent only through `ConfigureElectrical`. I pasted it into scratch copies of base `9b9d26e5` and candidate `bcc83118`. Both runs: 6/6. Raw lines are in `probe/probe-results.txt`.
- **Bench.** Gate at (0, 3, 0) tilted 15° about Z, both signs tried. Basketball at (−0.08, 3, 0). Battery wired to the gate. Phase 0 lasts 180 ticks, phase 1 lasts 300.
- **Sign −1 is the cat-051 bench.** The held ball rests at x = −0.466, which matches cat-051's "blocked, px ≥ −0.5".

| Scenario (sign −1) | Base `9b9d26e5` | Candidate `bcc83118` |
|---|---|---|
| Enabled from the start | ball passes downstream, ends at x −2.748 | ball passes downstream, ends at x −2.749 |
| Enabled; Disable at tick 0 (ball held); then Enable | blade opens; ball **thrown back to x +2.009** | blade opens; ball **thrown back to x +2.012** |
| Authored Disabled; Enable in run | blade **never opens** (the defect); ball held at −0.466 | blade opens; ball thrown back to +2.012 |

- **Result.**
  - Base already throws back a held ball when its gate re-opens: the deleted global flag followed the in-run toggle while the blade still had drag 0.0625.
  - The candidate's ejection is the same to within 3 mm.
  - With the battery enabled from the start, the candidate matches base to within 1 mm, so dropping the 0.0625 drag causes no regression on the cat-051 path.
  - With sign +1 the ball runs downhill away from the blade on both, so it is not a held-ball case.
- **Classification:** pre-existing, so not blocking.

## Runtime evidence

### Tier 1 (`tier1/`)

| Check | Result |
|---|---|
| Clean `git archive` copy of `e7f5b2b9` | **Build:** 0 warnings, 0 errors.<br>**Tests:** WorkerAotGuard 17, Coverage 78 and CuriousContraptions 1740 all pass. E2eRunner.Tests had 3 environmental failures, because `git ls-files` needs a repository; it was re-run in the worktree (next row) |
| Detached worktree `.claude/worktrees/murdoch9-9r3` at final `3414f836` (clean) | **Build:** 0 warnings, 0 errors.<br>**Tests:** 17 + 78 + 156 + 1740, all passing. That includes the ratchet (new SHA), `ParallelSuitesHaveNoTimingDetectorHit` and `SharedConsoleModuleSelectsItsImporters`, which had failed in the implementer's development logs |
| Node tests | `node --experimental-vm-modules --test tools/*.test.mjs`: 82/82 |
| Oracles | **LifecycleContract:** Count 510, Failures 0.<br>**WireContract:** Count 109, Failures 0.<br>**Ownership `--oracles`:** Positive, 22 negatives all rejected |

### Mutants (`mutants/`, each applied alone to a scratch copy of `bcc83118`, then reverted)

| Mutant | Change | Result |
|---|---|---|
| M4 | Solver `if (joint.SupplyClosed)` → `if (true)` | **Killed**, 4 engine tests. `WorkshopSpringboardTests`, which was in the filter, survives (F2) |
| M5 | Skip the D4 clear in `Reset` | **Killed** by the D4 test |
| M7 | Plate band dropped (`gatePowered` always true) | **Killed only by the ratchet count**, with no behavioural test (F4) |
| R1 | Re-add the 0.03–0.08 direct band to the solver | **Killed** by the general scan |
| R2 | Edit one pinned entry's text | **Killed** by the frozen-list SHA |

- **The script:** `mutants.sh.txt` lists all five mutants.
- **Test counts include the probe:** the scratch copy also carried the S1 probe's 6 theory cases, which is why the M4 run reports 228 tests. Those probe cases wrote to a `/tmp/probe.txt` fallback, which I deleted.
- **Logs:** The full diff for every mutant is at the top of its `.log` file. The implementer's mutants 1, 2, 3, 5b and 6 kill as her logs claim, and the matrix above relies on them.

### Tier 2 (`tier2/`)

| Item | Value |
|---|---|
| Runner | `20261019T230802Z-53756`, `run --changed` with cat-051, cat-051-own-supply and cat-062, plus the smoke pair |
| Source identity | `sourceHead` `3414f836`. Status digest `e3b0c442…`, the empty-status hash, so the tree was clean |
| Bundle identity | `b755b194…` at start and end; the suite source digest is also equal at start and end |
| Result | **Pass**: 5/5 files, 24/24 tests, 0 recalibrations, 0 debt faults, 0 close findings |
| Per suite | cat-062 7/7 (SerialAlone); cat-051-own-supply 6/6 (SerialAlone); cat-051 5/5 (Parallel); engine-core-2a1 5/5; engine-core-2b1 1/1 |

**Serial re-run (`tier2-serial/`)** for AC6:

- **Runner:** `20261019T231737Z-70937`, the same change list, with `--workers 1` (`workersUsed: 1`).
- **Identity:** `sourceHead` `3414f836`, clean status. Bundle `85684620…` at start and end, and the suite source digest is unchanged.
- **Result:** **Pass**, 5/5 files and 24/24 tests, with 0 recalibrations, debt faults or close findings, and wideOffset 2.
- **Order:** every Parallel suite ran in slot 0, one after another: cat-051 from 23:21:21 to 23:22:19, then the smoke pair.

My first attempt (`tier2-env-fail-no-node-modules/`) ran from a worktree outside the main checkout and failed before any test with `Cannot find module 'flatbuffers'`. That failure is environmental: Node resolves `node_modules` from the main checkout. I moved the worktree under `.claude/worktrees/` and re-ran it.

### Reuse of the implementer's full run

The run is `20261019T222832Z-84166`, in `evidence-9r-3/full-run-1/`.

- **Identity:** `sourceHead` `bcc83118`. The status digest is `e3b0c442…`, so the tree was clean. Bundle `f9c096ab…` is the same at start and end, and so is the suite source digest.
- **Applicability:** between `bcc83118` and `3414f836` no runtime, bundle input or suite source changed, only scheduling declarations. Her results for the 35 passing files therefore still apply to the final candidate.
- **Result:** 35/36 passed. cat-051 passed 5/5 and cat-051-own-supply 6/6, both running Parallel at the time, and cat-052 (D3 interim) passed 5/5.
- **cat-062:** it was cancelled by a 120 s timeout on a Parallel worker with no failed assertion. My Tier 2 runs replace it, with SerialAlone 7/7 twice. The static reason the change cannot reach cat-062 (Springboard): springboard prismatics compile no supply route, so their `SupplyClosed` is false and the drive skips them, just as the travel-limit check did on base. (M4 shows only that the springboard tests cannot *detect* a stray drive; see F2.)
- **clock-master:** it passed 9/9, SerialAlone, with recal 4, 1 close finding and 1 debt fault. That is outside this story's dependency closure (a clock has no supply route, and the drive change does not touch the clock path). I did not investigate it. Noted, not blocking.

## Findings (none blocking; all filed to F.5 "Supply Route Follow-ups")

- **F1. The frozen spec matrix row "Authored Disabled, enabled in run → the ball passes" is not met on the cat-051 bench.**
  - The gate opens, but the rising blade throws the held ball back out of the entry mouth.
  - The same thing happens on base after Disable→Enable, so it is pre-existing.
  - The epics AC ("opens its gate") is met.
  - Chrome's `assertBallReleased` (moved more than 0.5 m) cannot tell "passes" from "thrown back". It should assert the direction once the stroke law is fixed.
  - The implementer proposed 9.4c/9A.19 as owners. The coordinator should decide whether the frozen row is renegotiated.
- **F2. Mutation #4 does not fail `WorkshopSpringboardTests`.** Driving every prismatic toward its Upper is invisible to the springboard suite, because a springboard's Upper is 0. A springboard test that would notice a foreign drive is missing.
- **F3. Ratchet owner label.** The solver's `b.Drag >= 0.08f && b.Drag < 0.11f` row is owned by `Story9A19`, but the code comments and decision 5 say Story 9R.4 deletes it.
- **F4. No native behavioural test covers the D3 plate interim.** M7 is caught only by the ratchet count. cat-052 in Chrome is the only behavioural guard.
- **F5. Stale open question.** `docs/planning/elements/CAT-051-powered_gate.md:182` still asks "finite energy or binary enable". The 20 Oct "enable only" decision is not recorded there.
- **F6. The Chrome D4 comment overstates what it proves.** Own-supply test 1 says the second cycle's closed pre-assert "proves Reset alone restored the authored Disabled enable". `Run` from Building already clears the controls (`WorkshopSimulation.cs:233`), so that leg would pass without D4. Only the native test proves it.
- **F7 (pre-existing).** `elements/PoweredGate/PoweredGateElementDeclaration.cs:59` still declares `LinearDrag: 0.05f`, inside the deleted 0.03–0.08 band, while the compiled blade drag is 0. Only `tools/BundleElements` consumes it.
- **F8. Wall-clock windows.** The own-supply suite's `sample(driver, ms)` bounds its windows with `Date.now()`, the same pattern cat-051 and cat-052 use. The SerialAlone declaration (`e7f5b2b9`) mitigates the risk. Requalifying it as Parallel should go through calibration.
- **F9 (note).** Two test method names in `StateLatchTests` still say "SwitchedSupply". They describe the Switched route kind and are not alias names, so there is nothing to fix unless the coordinator wants them renamed.
