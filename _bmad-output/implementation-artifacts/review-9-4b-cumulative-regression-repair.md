# Adversarial Review Report: Story 9.4b — Cumulative Chrome Regression Repair

**Reviewer:** Murdoch-2 (`bmad_reviewer`). I am independent of the implementer (Amelia), and my pre-review probes were written separately from her work.
**Mandate:** terminal independent adversarial review (REQ-14). The owner asked for the review to "go harder".
**Story under review:** Story 9.4b ([`spec-9-4b-cumulative-regression-repair.md`](spec-9-4b-cumulative-regression-repair.md)), with source acceptance in `_bmad-output/planning-artifacts/epics.md` §Story 9.4b, including the widened coordinator ACs and the structural clock AC.
**Date:** 19 Oct 2026 (local). The log stamps are UTC 18 Oct, 16:40–18:20.
**Candidate:** the working tree on HEAD `8f682f8`.
- tracked diff sha256 `477b37bf…`; untracked sources hashed in [`review/source-identity.txt`](evidence-9-4b/review/source-identity.txt);
- `Simulation.wasm` `cc25176d…`;
- `simulation/worker.js` `33f45f53…`;
- `clock-master.js` `93f7d302…`;
- `workshop-client.js` `a59804ab…`.

**Raw evidence:** [`evidence-9-4b/review/`](evidence-9-4b/review/). I edited no deliverable file. Mutations ran in isolated copies (native tests) and in served copies of the bundle on :8061 (Chrome).

---

## 1. Executive summary and verdict

**Verdict: Fail.** The reason is one blocking finding, F1.

**F1: the cumulative baseline is not reproducible, so AC10 fails.**
- My first independent cumulative serial run failed 137/138 on `cat-051` test 4. My second passed 138/138.
- The failure is a false positive in the shared console filter `tools/workshop-console.mjs:5`. It treats any console line containing the substring `NaN` as a failure unless that line is a trace chunk.
- Ordinary `CCGPU <base64>` acknowledgements contain `NaN` by chance. Three did here, for example `…AYAAAANhNaN0CAAAAoI…`.
- I reproduced this deterministically. The exact logged line is classified as a failure; the same line with the substring changed is not (`review/console-filter-false-positive.txt`).
- Thirteen suites use the filter. AC10 asks for a 100% run "recorded as the new cumulative baseline that every Epic 9A story must keep", and a baseline with a known random false-failure source does not meet that.
- The filter code predates 9.4b. The deterministic baseline is still this story's deliverable.

**Required in the same fix round:**
- **F2 (Medium): the D18 ratchet has three proven holes.**
  - A camelCase element identifier evades the scan.
  - Deleting a pinned entry goes undetected.
  - The list can grow by adding an entry ("only shrinks" is not enforced).
- **F3 (Medium): a committed test is never compiled.** `CompiledMemoryLayoutTests.cs` has been outside the compile list since HEAD, against the widened AC that "every other test in the repository passes".
- **F4 (Medium, evidence): the anvil C# gate is vacuous.** The spec's "0 warnings" row carries no caveat, and the gate also misses `catch {}`.

**Everything else I attacked holds.**
- **Build and gates:**
  - identity reproduced;
  - 0 warnings across the slnx and 26 more projects;
  - `dotnet test` 1424/1424 and 78/78;
  - node 58/58;
  - Lifecycle 510/0, Wire 109/0, Ownership oracles positive.
- **Clock:**
  - all 28 clock-stress scenarios pass, with zero terminations and the D13 heartbeat boundary correct (800 ms survives, 1100 ms faults typed);
  - Save→reload→Load×3 with 9 cascades byte-exact.
- **Ledger:** L1–L9 hold, and replay after Reset is bit-identical.
- **Chrome repeats:**
  - cat-023b 12/12: 5 unloaded, 5 at load-8, and 2 at load-32 (twice the implementer's bar);
  - cat-005 5/5.
- **Mutations:** 16 of my mutations are killed. They include the coordinator's six: master back on the Simulation thread, offset assumed zero, dust threshold reverted, sphere support reverted, ratchet count edited, and heartbeat disabled (in node and in Chrome).
- **Amelia's two probe-defect claims are correct:** the hog workers and the ledger after-hook. I fixed my probes.

---

## 2. Identity

| Item | Result | Evidence |
| :-- | :-- | :-- |
| Source | HEAD `8f682f8` + tracked diff `477b37bf…` + 33 hashed untracked files | `review/source-identity.txt` |
| Rebuild + `dotnet publish CuriousContraptions.web` (exit 0, 0 warnings) vs `candidate-2/bundle-identity.txt` | 85/86 files byte-identical, including all wasm, js and dll. Only `godot.pck` differs, as the candidate recorded: a third distinct value across republishes, consistent with the non-deterministic Godot pack | `review/publish.log`, `appbundle-publish-vs-candidate2.diff`, `appbundle-full.sha256` (279 files) |
| Served bytes on :8060, start and end of all Chrome gates | Simulation.wasm `cc25176dcdab`, worker.js `33f45f530d02`, clock-master.js `93f7d3020c1d`, workshop-client.js `a59804abc67e` | `review/served-identity.txt`, `chrome-gates-summary.txt` |
| Probe runner | `workshop-driver.ts` `4aa077bb…`, `workshop-wire.ts` `2020a923…` and `workshop-console.mjs` `6652e11e…` are copies identical to the candidate's. The probes ran from scratch, not from `tools/e2e/` | `review/runner-identity.txt` |
| Environment | macOS, 16 logical CPUs, Chrome 155.0.8059.82, Node v24. The user's desktop apps were running (Chrome, Steam, Spotify) | logs |

## 3. Criterion and impact matrix

| # | Criterion (source) | Status | Evidence (own runs) |
| :-- | :-- | :-- | :-- |
| AC1 | CAT-015 ledger: no unearned debit, dust removed at source; cat-015a, sidekick and 015b 100% (epics) | **Pass** | Cumulative ×2: cat-015a 2/2, sidekick 3/3, 015b 4/4. Ledger probe 10/10 scenarios, L1–L9 hold (§6.3). Native mutation "dust threshold reverted" killed by 9 tests. |
| AC2 | cat-062b guide expectation matches the declared geometry and codec, source cited | **Pass** | The `maxAcceleration()` assertion is cited as pre-8.1 +140; cat-062b 7/7 in both runs. |
| AC3 | cat-062b test 5 fixed at source, with a Captured-goal codec test | **Pass (with C1 AC correction)** | Every Captured lesson declares Body = ball, Target = receiver (`BumperSidekick.cs:30`, `ClearPipe.cs:42`, `GentleBend.cs:46`, `BumperAdvanced.cs:41`). So the AC's premise was wrong and the test correction is the permitted alternative. Mutation "goal body/target swapped" is killed by 18 tests. |
| AC4 | cat-023b root-caused and structurally fixed; 3 consecutive runs (structural AC: 5; owner: 5 + load-8) | **Pass** | 5/5 unloaded, 5/5 at load-8 and 2/2 at load-32, each with 4/4 tests and 0 transport, unresponsive, rejected or recalibration lines (`chrome-gates-summary.txt`). |
| AC4-S | Structural: probes answered by a context the solver never blocks; a lapse recalibrates without terminating; a permanent injected-load test fails before and passes after | **Pass** | Clock-stress 28/28 (§6.1). Chrome mutant "master back on the Simulation event loop" fails `clock-master.test.ts` 6/6, test 1 on the expected Elapsed recalibrations. |
| AC5 | C2 conditional | **Pass (not triggered)** | The cause is not C2. No `ReliableStalled` stop occurred in any of the 28 scenarios, including the 600 ms main-thread block during a running chain. |
| AC6 | cat-005 on the production preview | **Pass** | 5/5 consecutive, each 7/7, plus cumulative ×2. |
| AC7 | IL2125 resolved without suppression; slnx 0 warnings | **Pass** | `build-slnx.log` 0/0; 26 other projects 0/0 (`build-projects.log`). Vendored sources refetched by me: 8/9 byte-identical, and `FlatBufferVerify.cs` differs only by BOM and trailing whitespace (`vendored-flatbuffers-refetch.txt`). IL2026 injection is a build **error** (VG2). |
| AC8 | Existing channels only | **Pass** | New lines are `CCGPU_*` typed records on the same console. `clockProbe` survives only as a negative control (`workshop-observation.test.mjs:148`). |
| AC9 | Harness deleted with references; coverage named and moved | **Pass** | `rg workshop-rigid-body` finds only history. Ported facts were checked by mutation (sphere support, velocity guard). The four unported facts are named with owners in spec §5. |
| AC10 | Every `tools/e2e/*.test.ts` 100% in one serial run, the baseline 9A keeps (owner: twice back to back) | **Fail** | Run 1: 137/138. `cat-051` test 4 failed on the console-filter false positive (F1). Run 2: 138/138. The TAP output shows all **33** files executed in both runs. |
| AC11 | All eight `tools/*.test.mjs` pass | **Pass** | 58/58 (`node-harnesses.log`). |
| AC12 | `tools/Coverage` passes and is run by `dotnet test` slnx | **Pass** | Coverage.Tests 78/78 inside `dotnet test CuriousContraptions.slnx`. |
| AC13 | Every test project in the slnx; contract executables run | **Pass for projects; F3 for files** | The only two tracked test projects are in the slnx. Lifecycle 510/0, Wire 109/0, Ownership `--oracles` and `--oracles .` positive (14 + 22 negatives, 545 headings). A committed `[Fact]` file is uncompiled (F3). |
| AC14 | Murdoch-2 pre-review items | **Pass** | Dust: mutation killed. Departure: D7/D13/D14 verified. Lapses: 28 scenarios. Stale contracts updated, `DeviceLost` gone, dead ABI gone. Decoder boundaries: 3 decoder mutations killed. Probes rerun (§6). |
| AC15 | Murdoch-2 mid-flight items (H1, H2, H3, M1, M2) | **Pass** | H1: heartbeat boundary in Chrome, and the mutant is killed in node and Chrome. H2/D14: the offset-zero mutation is killed. H3: the clock-master H3 test passes in both runs. M1: the slew/re-derive tests pass. M2: the 16-tick mutation is killed (5 tests), and supersedes are counted. |
| AC16 | Element-free solver ratchet "at full strength" with the pinned owned list | **Partial: F2** | It is at parity with the deleted harness scan (the same `\b` word patterns) and adds recognition patterns. A count edit and an unowned new literal are caught. Pin removal, camelCase identifiers and list growth are not. |
| Impact-clock | Every suite (startup qualification, pose mapping, `run()`, animation) | **Pass** | Both cumulative runs show 0 `CCGPU_CLOCK_RECALIBRATED`, 0 `CCGPU_CLOCK_REPLY_REJECTED` and 0 refused startups. 30 printed startup records: offset −7.5 to +9.8 µs, admitted width 31.4–36.5 µs. |
| Impact-transport | Heartbeat, liveness, yield loop | **Pass** | Clock-stress, the heartbeat and master mutants, the tick-loop mutation, and D13 boundaries 800 ms / 1100 ms. |
| Impact-codec | 27 suites with Save/Load; three decoders | **Pass** | Cumulative ×2, the cycling probe, and decoder mutations (zero default, fractional count, goal swap) all killed. |
| Impact-ledger | cat-005, cat-015* | **Pass** | As AC1 and AC6. |
| Impact-rolling (R13 changes every ball) | cat-002, 014, 015*, 017, 020, 023a/b, 030, 048a/b, 049, 050, 051, 052, 062, 062b, engine-core-2a*/2b*/2c, anim-* | **Pass** | All pass in run 2. In run 1 they all pass except cat-051 test 4, which failed on F1 and not on motion: cat-051 then passed 3/3 alone. The sphere-support revert is killed by 2 native rolling-law tests. No e2e numeric threshold was loosened (diff review of cat-014, 015b, 062b). |

## 4. Raw commands and results

All logs are in `evidence-9-4b/review/`.

| Command | Result | Log |
| :-- | :-- | :-- |
| `dotnet build CuriousContraptions.slnx --no-incremental` | exit 0; 0 warnings, 0 errors | `build-slnx.log` |
| `dotnet build <p> --no-incremental` for Simulation, Animation.Worker, Animation, Abstractions.{Electrical,Fluid,Mechanical,Signal}, 13 element packages, BundleElements, TraceAllocations, Preview, LifecycleContract, WireContract, Ownership | 26 × "0 Warning(s) 0 Error(s)" | `build-projects.log` |
| `dotnet publish CuriousContraptions.web` | exit 0, 0 warnings | `publish.log` |
| `dotnet test CuriousContraptions.slnx` | 1424/1424; Coverage.Tests 78/78; exit 0 | `dotnet-test.log` |
| `node --experimental-vm-modules --test tools/*.test.mjs` | 8 files; 58 tests, 58 pass | `node-harnesses.log` |
| `dotnet run --project tools/LifecycleContract` | Count 510, Failures 0 | `lifecyclecontract-contract.log` |
| `dotnet run --project tools/WireContract` | Count 109, Failures 0 (DesignOnly) | `wirecontract-contract.log` |
| `dotnet run --project tools/Ownership -- --oracles` / `--oracles .` | Positive; negatives 14 + 22 rejected; 545 headings | `ownership-contract.log`, `ownership-oracles-root.log` |
| `… Ownership -- --audit . docs/work-orders/P0-004/ownership/index.json` | "Source membership differs". The global audit was recorded as already failing and is owned by its named gate, not claimed here | `ownership-audit.log` |
| `anvil check --changed` | "13 changed files, no warnings": no `.cs` coverage | `anvil-cli.log` |
| MCP `anvil_check` on 74 changed sources | 0 warnings. **Control:** a C# file with an empty catch and a bare TODO also gives 0; a JS empty `catch (e) {}` gives AP-006 | `anvil-probe/` (F4) |
| Clock-stress probe (28 scenarios) | 16 pass in the first log; 12 duty scenarios failed on my probe's `__name` defect and pass in the rerun | `murdoch-clock-stress.log`, `murdoch-clock-stress-duty-rerun.log` |
| Save→reload→Load→Run×3 cycling | 3/3 loads, 9/9 runs | `murdoch-023b-save-load-cycles.log` |
| Bumper ledger | 10/10 | `murdoch-bumper-ledger.log` |
| cat-023b ×5 load0, ×5 load-8 (8 × `yes`), ×2 load-32 | 12 × 4/4; 0/0/0/0 counters | `e2e-cat-023b-*.log`, `chrome-gates-summary.txt` |
| cat-005 ×5 | 5 × 7/7 | `e2e-cat-005-run*.log` |
| Cumulative serial ×2 (spec + TAP reporters) | run 1 137/138 (F1); run 2 138/138; 33 files executed in each | `e2e-cumulative-run{1,2}.log` / `.tap` |
| cat-051 alone ×3 | 3 × 5/5, with no NaN-bearing `CCGPU` lines | `e2e-cat-051-isolated-*.txt/.log` |

## 5. What `unresponsive=2` means

In each cumulative run, the only `SimulationUnresponsive` lines are one adjacent pair: lines 759–760 and 761–762 in the candidate's two runs, and 791–792 in both of mine. Both lines of each pair belong to the deliberate H1 test, "A Simulation spin beyond the liveness bound faults the session as SimulationUnresponsive; a 600 ms block does not":

- line 791 is that test's own JSON summary (`firstFailureMs` 958 and 918), which quotes the typed failure;
- line 792 is its passing title.

The `transport=1` count is the same summary line. No other suite produced either string, so this is the injected-hang test working, not a defect.

## 6. Probes

### 6.1 Clock stress: 28 scenarios

The 26 planned scenarios plus two D13 boundaries.

**Zero terminations everywhere except the INFO 1100 ms Simulation block.** That block raises the typed `SimulationUnresponsive` 960 ms after it starts, as D13 contracts.

**Recalibrations:**
- Simulation-only load (duty 20 and 40 ms, blocks of 600 and 800 ms, and a 50% duty while running) gave **0 recalibrations**. This is the structural property.
- Animation or main-thread load gave exactly one recalibration, by the loaded requester's own role (1 or 3).

**Contracted Capacity debt stops** occurred in three scenarios and were allowed: R-chain-sim-duty20, R-ball-sim-block600, and R-chain-hogs-2x under 2× CPU oversubscription.

**Liveness after every load:** the session requalified and Run/Reset worked, and the chain solved where it was running.

**Probe defects of mine, now fixed (the evidence for both runs is kept):**
- **Hog workers.** The probe's blob hog workers were counted as closed game workers. Amelia was right, and I now filter to the game workers.
- **Hog debt.** R-chain-hogs-2x now allows the contracted Capacity stop.
- **Recalibration opt-in.** The driver now fails any session that recalibrates at `close()`. The probe opts in with `allowClockRecalibrations: true` and asserts its own expectations.
- **tsx keepNames.** tsx wraps named inner functions in `__name()`, which does not exist in the worker. The duty-cycle injection is now a source string.

### 6.2 Save→reload→Load→Run×3

- **Restoration:** every Load read at tick 0 equals the first load byte-for-byte, and every Reset restores those bodies byte-for-byte. A re-save after Load is authored-identical.
- **Cascades:** all nine runs topple in chain order 4,5,6,7,2, with exactly one CCGOAL_SOLVED each, and the lamp ramps without restarting.
- **Console:** the per-run slices and the `after()` total show 0 errors, 0 recalibrations and 0 terminations.

### 6.3 Bumper ledger

Basketball and Bowling at lifts of 60, 110, 150, 200 and 260 px, two Runs each.

**L1–L6, L8 and L9 hold.** Examples:
- Basketball pays 31.866867 J (target-limited) and then the exact remainder 0.133133 J (store-limited, ≥ the 1.22e-4 J minimum). After that the store is 0, with 4–5 unpaid contacts.
- Bowling pays 32 J (the whole store), then unpaid contacts.
- Ring pulses equal paid payouts, and replay after Reset is bit-identical in all 10 scenarios.

**L7:** the probe saw one extra occurrence per run. It was the tick-0 read's empty slot with sequence 0, an artefact of my probe. Excluding it, the observed occurrences equal `occurrenceCount` in all 10 scenarios.

**The ">= 3 paid payouts" coverage gate was unreachable (Amelia was right).**
- With bumper restitution below 1, a ~1 kg payload returns well below 8 m/s, so a top-up costs about 16 J or more. The 32 J store funds at most two payouts.
- The gate now requires the ledger-relevant sequence, a target-limited payout followed by a store-limited exhaust: met in 5 scenarios. Many small payouts are covered natively by `ContactWorkLedgerPropertyTests` T6.

## 7. Mutation checks (my mutations, not the implementer's tests' own evidence)

**Method.** Each native mutation was built in an isolated copy and compared against that copy's baseline. In the copy, 207 asset-dependent tests fail before any mutation, so a kill means a test that passes at baseline fails under the mutant (`mutations/run-mutation.sh`, `mutations/summary.txt`, per-mutant `.diff` and `.killed-by.txt`).

| Mutation | Result |
| :-- | :-- |
| **Clock master back on the Simulation thread.** Chrome mutant: the master answers Browser/Animation probes only after a Simulation event-loop timer advances a ring gate | **Killed.** `clock-master.test.ts` fails 6/6; test 1 (Simulation 20 ms duty) fails on Elapsed recalibrations of roles 3 and 1. *Caveat:* the mutant also delays the Simulation's own derivation check, so later tests also fail for that reason. |
| **Heartbeat disabled** (`beat(clock)` removed) | **Killed** in node (`workshop-observation.test.mjs` "every check turn also beats …"; the control copy passes). **Killed** in Chrome: `clock-master.test.ts` 0/6, every session `SimulationUnresponsive`. |
| **Offset assumed zero** (`StartupCorrection` → 0) | **Killed** by 3 tests (`AConstantOffset…`, `ADriftingTimeOriginOffset…` ×2). |
| **Dust threshold reverted** (`MinimumPayableFraction` = 0) | **Killed** by 9 tests (T1 ×5, `ARequestAFewUlpsBelowTheStore…`, …). |
| **Sphere support reverted** (always the material anchor) | **Killed** by 2 (`ABasketballRollingAt20…NeverRaisesItsSpin`, `…At5…DeceleratesOnEveryCommittedTick`). |
| **Velocity guard reverted** (overflow returns 0) | **Killed** by 1 (`TheEndOfSubstepGuardClampsAFiniteVelocityOfAnySize`). |
| **Ratchet list edited:** pinned count 8 → 9 | **Killed** (`TheShippedSolverNamesNoElement…`). |
| **Ratchet list edited:** a pinned entry removed ("0.5 kg threshold") while its code stays | **Survived** (F2). |
| **Ratchet growth:** a new solver violation plus a new pinned entry | **Survived** (F2). |
| **Ratchet:** camelCase `bumperBoost` in a solver file | **Survived** (F2); control: the whole-word `bumper` is **killed**. |
| **Ratchet:** an unowned `b.Drag > 0.3f` (control) | **Killed**. |
| Goal Body/Target swapped in the encoder | **Killed** by 18. |
| Decoder: counter zero → default 3 | **Killed** by 6. |
| Decoder: fractional counts rounded | **Killed** by 11. |
| `TicksPerTurn` = 16 | **Killed** by 5. |
| IL2026 injected into trimmable engine code | **Build error IL2026** (VG2 is real). |

## 8. Decisions D1–D18

| Decision | Assessment |
| :-- | :-- |
| **D1** `CCGPU_RETAINED` in production | Accepted under owner decision (h). I did not re-measure its cost; the claim of "within noise" is unverified by me and owned by P0-034. It adds base64 console volume, which widens the F1 false-positive surface; that amplification is unmeasured. |
| **D2** vendored FlatBuffers | Verified by an independent refetch. |
| **D3/D10** whole-store spend, minimum payable 2⁻¹⁸ × capacity | Verified (mutation, ledger probe). **Spec error:** D10 says "8 f32 ULPs of the 32 J preload". 2⁻¹⁸ × 32 J = 1.22e-4 J = **32** ULPs of 32 (ulp(32) = 2⁻¹⁸). The code comment's "32 to 64 ULPs" is right. |
| **D4** decoders reject | Verified (three mutations killed, three decoders). |
| **D5/D6** dead exports and device loss deleted | Verified by `rg`. Only explanatory comments remain. |
| **D7** clock master context | Verified (stress, mutant). |
| **D8** recalibrate and continue; suites fail on any recalibration | Verified. The driver's `close()` guard is load-bearing (the mutant was caught through it). |
| **D9** four ticks, one hop, supersedes counted | Verified (mutation). |
| **D11** coverage grammar | Not independently attacked. Coverage 78/78. |
| **D12** event-ring scaffold deleted | Accepted. The C# `EventRingBuffer` publisher remains live and unread (owner 9A.1). |
| **D13** heartbeat | Verified (boundaries; the mutant is killed twice). |
| **D14** startup offset derived | Verified (the offset-zero mutation is killed; 30 echoed startup records −7.5 to +9.8 µs, `startup-offset-sample.json`; no refusal and no "never became ready" in any review log). I checked my own concern about residual error: the residual is bounded by width/2 − bound plus quantization, inside the 15.8 µs share. Withdrawn. The > 0.1 ms refusal is unqualified beyond this Mac (device gate). |
| **D15** malformed reply counted | Not independently mutated. The implementer's mutation log is accepted as supplementary. |
| **D16** retired transport stops UI | Not independently attacked. Its tests pass. |
| **D17** R13 engine fixes | Verified (two mutations killed; rolling suites pass). |
| **D18** ratchet | F2. |

## 9. Process breaches (spec §7)

**Effect on the verdict: none.** Every gate in this verdict was rerun by me on the reproduced identity, so none of the implementer's contaminated or overlapped runs (the fork mutation window, the one-Chrome overlap, the `mv`/`cp` evidence moves) is relied on.

**Still open as process violations:**
- Repeated writes without anvil validation before the write (AGENTS: mandatory).
- The evidence claim in §9: "MCP `anvil_check` on every touched source: 0 warnings" is presented as a gate, but the C# side is vacuous (F4).
- The spec's "26 suites" in §9 is Node's count of `describe` blocks. 33 files ran (my TAP output confirms it).

**Repo hygiene before any commit:**
- the stray repo-root `simulation-unresponsive.png` and `.png.import` (Godot imported it);
- the untracked `scratch/`.

---

## 10. Findings

### F1 (High, blocking): the console filter randomly fails valid acknowledgements, so the cumulative baseline is not deterministic

- **Location:** `tools/workshop-console.mjs:5-13`. 13 suites call it (`cat-002`, `014`, `017`, `020`, `023b`, `030`, `037`, `048a`, `048b`, `049`, `050`, `051`, `052`).
- **Mechanism:** any `log` line containing the substring `NaN` counts as a failure, unless it is a 7-field `CCGPU_TRACE_CHUNK`. Base64 payloads of `CCGPU` acknowledgements and reads contain `NaN` at random.
- **Evidence:**
  - Cumulative run 1, `cat-051` test 4: "Zero errors expected, got: CCGPU NAAAA… (3 !== 0)". All three flagged lines are valid acknowledgements.
  - Deterministic: `isWorkshopConsoleFailure('log', <that line>)` = true, and with the `NaN` substring changed = false (`console-filter-false-positive.txt`).
  - cat-051 passes 3/3 alone when no line happens to contain the substring.
- **Required:** a real NaN in diagnostic prose must still fail, and a base64 envelope must never fail on its payload. Then:
  1. Classify by the typed envelope: every well-formed `CCGPU[_A-Z]* <base64>` line is exempt from the substring check, as trace chunks already are.
  2. Add a node test with an acknowledgement and a `CCGPU_RETAINED` line containing `NaN`, plus the prose control.
  3. Rerun the cumulative set twice.

### F2 (Medium): the D18 ratchet does not enforce its stated integrity

- **Location:** `CuriousContraptions.tests/WorkshopPhysicsLawTests.cs:518-581`.
- **(a) camelCase escapes the scan.** The element pattern `\b(…plate|gate…)\b` misses identifiers such as `currentPlateLoad` and `_plateLoaded`, which are present now, and `bumperBoost` (mutant survived).
- **(b) The pin protects nothing it cannot see.** The general scan cannot see `currentPlateLoad >= 0.5f`, so deleting its pinned entry passes (mutant survived), and owner 9R.4 would lose the debt silently.
- **(c) The list can grow.** "The ratchet only shrinks" is a comment only: a new violation plus a new entry passes (mutant survived).
- **Required:**
  1. Pin the list size and total count (for example a constant total, or a hash checked against sprint-status).
  2. Require every pinned match to also be matched by the general scan, or flag it explicitly as invisible.
  3. Match element names inside identifiers (`(?i)(domino|…|plate|gate|…)`), with a reviewed allow-list for generic words.

### F3 (Medium): a committed test is never compiled

- **Location:** `CuriousContraptions.tests/CompiledMemoryLayoutTests.cs`. It is not in the explicit `<Compile>` list; it has been tracked since HEAD and was never compiled.
- **Why it matters:** the widened AC says "every other test in the repository passes". This `[Fact]` never runs.
- **Required:** compile it and make it pass, or delete it with the coverage it claims named.

### F4 (Medium, evidence): the anvil C# gate is vacuous

- **Evidence:**
  - MCP `anvil_check` returns 0 warnings on a C# control with an empty `catch { }` and a bare `// TODO` (`anvil-probe/EmptyCatchControl.cs.txt`; checked as `.cs`, then renamed so that `CuriousContraptions.csproj`'s default compile glob, which does not exclude `_bmad-output`, never compiles it. No build ran while it was a `.cs`).
  - A JS control with `catch (e) {}` gets AP-006, but the binding-less `catch {}` form is not flagged. Touched `worker.js:195` keeps a `catch {}` around `FlushObservation`, already present at HEAD.
  - The spec presents "0 warnings on every touched source" without this caveat.
- **Owner:** the C# gate belongs to Story 9R.0. Here, the spec row must state the limitation.

### Low and info

- **L1. Spec claim errors:** "26 suites" (it is 33 files), and D10's "8 ULPs" (it is 32).
- **L2. Stray files:** the repo-root `simulation-unresponsive.png` and `.import`, and the untracked `scratch/`. They must not be committed.
- **L3. Pre-existing empty catches:** `worker.js:195` and `WorkshopTrace.cs:164` (commented) are present at HEAD.
- **I1. My probe defects** are corrected and recorded (§6), and Amelia's two refutations are accepted.
- **I2. Global audit:** the Ownership `--audit` membership difference belongs to its named gate.

## 11. Terminal verdict

**Fail.** F1 is a known violation of AC10. I reproduced it independently, and its cause is identified with a deterministic red test.

**Re-review conditions:**
- F1 fixed with its node test.
- F2 and F3 fixed.
- F4's spec row corrected.
- L1 and L2 corrected.
- Then two back-to-back cumulative serial runs on the frozen candidate with the TAP file accounting, plus cat-023b 5× and cat-005 5× if any runtime byte changes.

I will rerun my probes and the mutation set against the changed inputs only. Unchanged, independently verified results (§6, §7 except D18, and cat-023b/cat-005) carry over while their relevant inputs stay byte-identical.

The preview on :8060 and the mutant previews on :8061 are stopped.

---

# Round 2: candidate 3 terminal re-review (Murdoch-2, 19 Oct 2026)

**Scope:**
- every round-1 finding (F1–F4);
- every lens item in `lens-findings-9-4b-final.md` (BH1–BH14, E1–E4, V1–V5);
- the coordinator's decisions D19–D22;
- the startup-offset question and the BH10 residual.

**Method:** everything was checked against the actual tree, and every claimed test was attacked by my own mutation.

**Evidence:** [`evidence-9-4b/review-r2/`](evidence-9-4b/review-r2/).

**Not done:** I did not republish (godot.pck is not byte-deterministic). Chrome ran against Amelia's preview (pid 32348), one Chrome at a time. No deliverable file was edited; the tracked-diff hash is unchanged from start to end.

## R2-1. Verdict: **Fail**

Two findings block. A third is required by the coordinator.

**R2-F1 (High, blocking AC10): a second nondeterministic baseline failure.**
- My cumulative run 1 failed in `cat-017`. It was not a test failure: the file failed on a session-close finding, a `CCGPU_CLOCK_RECALIBRATED` record (Browser, `MappingLifetime`, from Ready).
- **Cause:** the Browser's mapping qualified and refreshed at 1.064, 1.320 and 1.570 s. Then the main thread stopped servicing until 2.236 s, during Godot's first-frame startup ("first frame 1004 ms after boot, 887 ms starting"). That is longer than the 500 ms mapping lifetime.
- **Why it fails a suite:** D8 makes a lapse a counted recalibration, and the driver's close policy fails every suite that has one.
- **Frequency:** 1 of about 556 sessions I observed, so roughly 0.2% per session and about 25% of cumulative runs at 157 sessions each.
- **Prior evidence:** this signature appears nowhere in round 1, candidate 2 or candidate 3. Her two clean cumulative runs do not establish a deterministic baseline.
- **Required:** an owner or coordinator decision on the fix. Either the Browser mapping must not lapse because of host startup work (for example, qualify after the first frame, or keep the refresh off the blocked thread), or the suite policy must classify a startup lapse explicitly. Then two clean back-to-back runs.

**R2-F2 (required by the coordinator; Medium): the BH10 residual is untested.**
- Four mutants survive the node harnesses (0 failures each):
  - deleting `fail()`'s ring detach (`workshop-client.js:78`);
  - deleting the Animation `closeOwned` detach;
  - dropping the late-reply guard after `fail()` (`workshop-client.js:394`);
  - dropping the late-reply guard after dispose (Animation `worker.js:43`).
- The code is correct today, but no test proves it.

**R2-F3 (coordinator decision; evidence below): the startup-offset refusal bound.**
- The outliers are real and environmental. They come neither from the suites nor from candidate 3's code.
- I found no loaded-machine refusal in 556 observed sessions, including 2× CPU oversubscription.
- The mechanism has no inherent bound, though: worker time origins carry their own host-clock reference. A host clock step or large slew between the creation of the two contexts would push a legitimate session past 0.1 ms, and it would be refused.
- The refusal adds no correctness. The offset is measured to a ≤ 36.6 µs window and corrected regardless.
- Recommendation: remove the refusal, or make it a counted record.

## R2-2. Identity

| Item | Result | Evidence |
| :-- | :-- | :-- |
| Source vs `candidate-3/source-identity.txt` (128 files) | Byte-identical, except the two repo-root PNG files the coordinator deleted. Rechecked unchanged at the end of the review | `review-r2` session (`c3-files` comparison); `now-hashes` identical start/end |
| Bundle vs `candidate-3/bundle-identity.txt` | 95/95 files identical, including godot.pck `a63b3974` | `appbundle-vs-candidate3.diff` (empty) |
| Served on :8060 (before and after every Chrome gate) | Simulation.wasm `fcf70711`, sim worker.js `b892a211`, clock-master.js `8af6087a`, workshop-client.js `9fdea5f8`, clock-ring.js `1c348f7a`, animation worker.js `b5b6cd9b`, Animation.Worker.wasm `94076e96`: MATCH | `served-identity-before.txt`, `chrome-gates-summary.txt` |
| Bundled scripts vs source | All six served scripts are byte-equal to their `wwwroot` sources | review session |

## R2-3. Gates (my runs)

| Gate | Result | Log |
| :-- | :-- | :-- |
| `dotnet build CuriousContraptions.slnx --no-incremental` | 0 warnings, 0 errors | `build-slnx.log` |
| The 26 projects outside the slnx (Simulation, Animation.Worker, Abstractions.*, 13 elements, tools) | 26 × exit 0, 0 warnings, 0 errors | `build-projects.log` |
| `dotnet test CuriousContraptions.slnx` | 1472/1472; Coverage.Tests 78/78 | `dotnet-test.log` |
| Node harnesses (9 files) | 67/67 | `node-harnesses.log` |
| LifecycleContract / WireContract / Ownership `--oracles .` | 510/0; 109/0; positive, 22 + 15 negatives rejected, 545 headings | `*-contract.log`, `ownership-oracles.log` |
| Murdoch clock stress (28 scenarios) | 28/28. 0 non-INFO terminations; 0 recalibrations under any Simulation-only load | `murdoch-clock-stress.log/.tap` |
| Save→reload→Load×3 cycles | 3/3 (9 cascades) | `murdoch-023b-save-load-cycles.*` |
| Bumper ledger L1–L9 | 10/10. **0** L7 notes: my probe now skips the tick-0 empty slot (sequence 0), the artefact I found in round 1. The "20 L7 Incomplete notes" in the candidate-3 summary come from my old probe | `murdoch-bumper-ledger.*` |
| clock-master ×5 | 5 × 6/6 | `clock-master-run*.tap` |
| cat-005 ×5 | 5 × 7/7 | `cat-005-run*.tap` |
| cat-023b ×5 unloaded, ×5 load-8 | 10 × 4/4; 0 recalibrations, 0 unresponsive | `cat-023b-load*-run*.tap` |
| **Cumulative ×2** (one process per file, per-file TAP and exit code) | **Run 1:** 32/33 files exit 0, 138/138 tests; `cat-017` exit 1 on the close finding (R2-F1). **Run 2:** 33/33 exit 0, 138/138, 0 close findings. `SimulationUnresponsive` only in clock-master (H1), 0 reply rejections in both runs | `cumulative-run{1,2}/` |

## R2-4. Round-1 findings

| Finding | Status | Proof (mine) |
| :-- | :-- | :-- |
| F1 console filter | **Resolved** | My exact failing line and 11 edge cases: 12/12 classified correctly (`f1-console-filter-probe.txt`). Prose NaN, a NaN after an envelope, malformed base64, multi-line text and non-`log` types are still caught. The filter-revert mutant is killed by 3 node tests. |
| F2 ratchet | **Resolved** | camelCase `bumperBoost`, deleting the pinned 0.5 kg entry, flipping its visibility, and adding a new violation with a new pinned entry are **all killed** (round 1: three of these survived). Residuals are in R2-F6. |
| F3 uncompiled test | **Resolved** | Compiled, and byte-identical to Story 9R.0's version (commit `16326220`, sha `8b872e2b…`). Its 4 tests pass in the trx. No other test file is uncompiled. |
| F4 anvil wording | **Resolved** | Spec §9 and `per-suite-counts-note.txt` now say anvil is write validation and the C# gate belongs to Story 9R.0. |
| L1 spec errors | **Resolved** | "33 files"; D10 is 2⁻¹³ J (32 ULPs). |
| L2 stray files | **Resolved** | The PNG and `.import` were deleted by the coordinator. The H1 screenshot now defaults to `evidence-9-4b/` (`clock-master.test.ts:15`). |

## R2-5. Lens items

Each item is checked against the diff; "killed" means my mutant (`review-r2/mutations/`).

| Item | Status | Proof |
| :-- | :-- | :-- |
| BH1 ABI literals | **Partial (R2-F4)** | The lens-named sites use constants. The same contact-work loop still has literals:<br>header counts `104`/`108` (`WorkshopPhysicsEngine.cs:329-330`, `:415`);<br>owner `+8`, kind `+12`, named `+28`;<br>occurrence target `+4`/`+8`;<br>strides `64`/`32`;<br>`kind == 1`.<br>`PhysicsGpuAbi.cs` header writes are also literal. |
| BH2 diagnostic epoch | Fixed | killed (copy 96 bytes) |
| BH3 JS wire cross-check | Fixed | killed (`ReplyBytes` 96; half-even tie rounding), both by `ClockRingScriptTests`. Her PascalCase disposition is accepted. |
| BH4 DeviceLost | Fixed | killed (TraceEnd 3 defined, by 1 test; rejection 6 defined, by 4 tests). The driver no longer has `DeviceLost`. |
| BH5 png | Fixed | see L2 |
| BH6 package versions | Fixed | Pinned 18.11.0 / 3.2.2 / 3.1.5 in both projects; the comment is true |
| BH7 `root = true` | Fixed | `.editorconfig:3` |
| BH8 classify first | Fixed | killed (epoch before classify) |
| BH9 / D22 outliers | Fixed | killed both ways: limit 16, and limit 0 (2 tests). D22 accepted: no outlier was set aside in any of my 556 observed sessions. |
| BH10 cleanup | **Partly proven (R2-F2)** | killed: no try/catch on `resumeTurn`; disposal leaving turns live; the unbounded reply wait (the harness hangs forever, so the test cannot finish). **Survived:** the ring detach in both requesters and the late-reply guards. |
| BH11 NaN lane | Fixed | killed (6 tests) |
| BH12 `derivedNow` | Fixed | `clock-master.js:23-24` computes the origin difference once |
| BH13 / D19 close findings | Fixed | My own fixture (`probes/d19-fixture.test.mjs`):<br>a passing test with a finding → `hookFailed`, failed;<br>a failing test keeps `OWN-ERROR-B` and the finding is still printed;<br>a finding in a suite's `after` hook fails the file (exit 1).<br>Removing either root hook is killed. Every suite gets the hooks through the driver import. Note R2-F6. |
| BH14 / D20 impossible store | Fixed, **closure proven** | See R2-7. Treating impossible as below-minimum is killed (11 tests); not reporting it is killed (3). |
| E1 startup stall | Fixed | killed (node). Bounded at 500 × 2 ms ≈ 1 s. |
| E2 turn wrap | Fixed | killed |
| E3 rederive without slots | Fixed | killed |
| E4 work-order suffix | Fixed | killed (Coverage) |
| V1 partly spent store | Fixed | killed (2 tests) |
| V2 ProfileRange | Fixed | killed |
| V3 epoch > 32 bits | Fixed | killed (2) |
| V4 deferral unit and call sites | Fixed | killed: ignoring master time, and overwriting the held control. Both requesters use the one `ReceiveEncoded` path. The Chrome duty20 test asserts ≥ 1 Animation recalibration and ≥ 1 control deferred while recalibrating (non-vacuous). It passed in all 5 of my clock-master runs and both cumulative runs. |
| V5 vtable branch | Fixed | `workshop-client.js:45-51` |

## R2-6. Startup offset spread: the coordinator's mandatory question

**Which suites?** Her 314 records come from two runs of 157 sessions, one process per file in alphabetical order (`files.txt`). My per-file derivation logs give the same 157 sessions per run (`session-index-map-run1.txt`). The six sessions beyond ±20 µs map as follows:

| Session (her jsonl) | Offset | Suite |
| :-- | :-- | :-- |
| run 1 #37, #38 | −55.0, −40.1 µs | cat-014 (its 3rd and 4th sessions) |
| run 1 #125 | −44.8 µs | cat-062b (7th) |
| run 2 #71, #72, #73 | +65.0, +30.1, +25.0 µs | cat-023b (4th), then cat-030 (1st, 2nd) |

The outliers cluster in time, across suite boundaries, and they are near multiples of Chrome's 5 µs cross-origin-isolated clamp. So they are time-correlated host behaviour, not something a suite does.

**Real or artefact?**
- **They are real per-session offsets.** Each was derived from 16 narrow exchanges whose admitted window is 31.6 µs, so it is measured, not noise.
- **Candidate 3's changes cannot produce them:**
  - BH12 only removes about 0.24 µs of double rounding.
  - D22 set aside 0 outliers in every session.
  - E1 changes only the attempt counts (17–19 here).

**Load does not widen it.**
- My 314 cumulative sessions: −15.0 to +15.1 µs.
- 30 sessions each at load 0, 8 and 32 (2× the 16 logical CPUs): −10.1 to +10.0 µs.
- 0 refusals and 0 outliers set aside. A refused session never reaches `close()` and so is missing from her distribution; my probe counts refusals explicitly.

**Mechanism (experiment):** `murdoch-time-origin-experiment`. Two cross-origin-isolated workers created 0–5000 ms apart ping-pong through Atomics, with bracket ≤ 2.4 µs.
- Their `timeOrigin + now()` agree to within ±13 µs.
- The disagreement drifts with the creation gap: the mean is −1.8 µs at 0 ms and +8.7 µs at 5 s, about 2 ppm.
- So each worker's time origin carries its own host-clock reference, not one shared monotonic origin. A host clock step or large slew between the creation of the Simulation and the master would therefore be a constant, unbounded per-session offset.

**Conclusion for the coordinator:**
- I found no loaded-machine refusal in 556 observed sessions, so this is not a demonstrated load-driven defect.
- A refusal of a legitimate session during a host clock adjustment (time sync, sleep and wake) is plausible, with no inherent margin; the observed worst case is 65% of the bound.
- The refusal gives no correctness benefit, because the offset is measured and corrected regardless.
- This is not proven Fail-level. Recommendation: drop the > 0.1 ms refusal (keep the empty-intersection and budget refusals) or make it counted telemetry. **Decision: coordinator.**

## R2-7. D20 closure proof (my probe, not the implementer's tests)

`probes/MurdochR2StoreClosureTests.cs.txt` was compiled in the isolated copy and passed 13/13 (`d20-closure-probe.log`).

1. **Exhaustive special-value product (more than 100k cases):**
   - capacities: 0, ε, 1e-38, 1e-30, 1e-7, 1, 2.5, 31.999998, 32, 128, 200;
   - available values: 0, −0, ε, min/2, min ± 1 ULP, cap/2, cap − 1 ULP, cap, and random;
   - speeds and inverse masses: NaN, ±Inf, ±0, ±ε, subnormal, 1e-30 … `MaxValue`, and negatives;
   - targets: NaN, Inf, −1, 0, 1e-7, 8, 20, `MaxValue`.

   No case reaches `Impossible`. Every result has remaining in [0, available], a finite non-negative debit and impulse, and closes exactly (remaining + debit = available).
2. **Chained payouts:** 20,000 trials × 60 chained payouts, with special values injected, never reach `Impossible`.
3. **Only impossible inputs** (NaN, negative or above-capacity available; NaN or negative capacity; positive available with 0 capacity) yield `Impossible`.
4. **Bad preloads rejected before Run:** declaration validation (`ContactWorkDeclaration.Validate`, run by scene validation at `PhysicsDeclarations.cs:278`, and `ContactWorkCalibration.Validate`) rejects NaN, ±Inf, −1, −1e-30, 200.00002 and `MaxValue`, and admits 0, 32 and 200 J.

**Writers of the store:** admission writes preload = available = the validated `InitialEnergy` (`PhysicsGpuAbi.cs:175-176`). The engine writes only `FromStore`'s `Remaining`. Commit validation pins the preload. So no numerical path writes an impossible store, and D20 is accepted as a fail-fast invariant only.

## R2-8. Findings

- **R2-F1 (High, blocking).** Startup Browser-mapping lapse → suite failure, so the cumulative baseline is nondeterministic (R2-1). Evidence: `cumulative-run1/cat-017.tap` lines 37–38 and 184–196.
- **R2-F2 (Medium, required).** No test for the BH10 ring detach or the late-reply-after-fail/dispose guards; 4 survivors (`mutations/J6–J9`).
- **R2-F3 (coordinator decision).** The startup-offset refusal can refuse a legitimate session under a host clock adjustment. It has no correctness value (R2-6).
- **R2-F4 (Low–Medium).** BH1 is incomplete: literal ABI offsets remain in the contact-work loop and the header writes. The spec's "contact work, occurrences … use the named constants" overstates this.
- **R2-F5 (Low).** The general ratchet scan names 14 element words. Spring, pipe, funnel, detector and clock families are not scanned.
- **R2-F6 (Low).**
  - The frozen ratchet list can still be rewritten together with its SHA constant; enforcement is a visible diff only.
  - A suite-level close finding is reported against `workshop-close-findings.mjs`, not the suite.
- **R2-F7 (Info).**
  - My own probe defects, fixed: tsx `__name` in `page.evaluate` (both experiments), and L7 sequence 0.
  - My first per-file counters read the empty `.log` files; I recounted from TAP.

## R2-9. Re-review conditions

- R2-F1 fixed, or its policy decided, followed by two clean back-to-back cumulative runs.
- R2-F2 tests added and mutation-killed.
- The R2-F3 decision recorded.
- R2-F4 completed, or its spec claim narrowed.

Carry-over: R2-3 to R2-7 results carry over for inputs that stay byte-identical.

---

# Round 3: candidate 4 terminal re-review (Murdoch-2, 19 Oct 2026)

**Method.**
- **Static review** while Amelia's gates ran: no builds, tests, Chrome or load.
- **Runtime checks** after the coordinator cleared them.
- **Mutations** ran in an isolated copy (`mut4`, now including the `parts/catalog` `.tres` files the ratchet reads), judged against that copy's own baseline.
- **Chrome** ran one session at a time against the :8060 preview.
- **Integrity:** no deliverable file was edited.
- **Evidence:** [`evidence-9-4b/review-r3/`](evidence-9-4b/review-r3/).

## R3-1. Verdict: **Fail**

**R3-F1 (High, blocking unless the coordinator accepts the risk in writing): D23 can renew across a genuine clock move.**
- **The weakness.** The renewal test is "the fresh exchange's admitted interval overlaps the old mapping's drift-widened interval". It puts no bound on the fresh interval's width, apart from the 250 ms probe lifetime. A slow renewing exchange is therefore uninformative, yet it still counts as consistent.
- **Proof** (my native probe, `probes/MurdochR3RenewalTests.cs.txt`, 2/2, `renewal-probe.log`):
  - A Ready mapping at +50 ms stalls for 700 ms.
  - The renewing probe is answered with a 200 ms round trip, while the true offset has moved to +110 ms (a 60 ms move).
  - Result: 1 late renewal, 0 recalibrations, and the display epoch kept (no reseed). Ordinary recovery then re-anchors on the wide renewal slot, so the narrow exchanges at the moved offset are not Disjoint, and the mapping follows +110 ms.
  - The same move answered by a narrow (1 ms) exchange is a recalibration, as intended.
- **The coordinator's acknowledged question,** whether a stall followed by a wide, consistent exchange can let a moved clock slip through recovery as consistent: **yes**.
- **Impact today.** I know of no source of a same-epoch move: derivation re-derives change the epoch, and slews are at most 5 µs per check. So nothing is observed in Chrome. It is still a soundness hole in the renewal contract, and stalls cluster, so a second stall during the renewing exchange makes a wide exchange plausible.
- **Fix.** Renew only on a narrow exchange: admitted width ≤ `MaximumMappedWidth` (2 ms; the observed post-stall widths were 1.75–1.83 ms), or apply the ordinary Ready-quality criterion. Otherwise recalibrate.

**R3-F2 (Medium, required): D23's "a lapse with a probe outstanding stays a recalibration" is untested where it matters.**
- Deleting the `Pending is not null` clause in `LapsedWhileStalled` survives every test (`mutations/R3-renew-with-probe-outstanding`).
- `ALapseWithAProbeOutstandingIsACountedRecalibration` passes for another reason: in Ready the outstanding refresh's due time (320 + 250 = 570 ms) exactly equals the lifetime end (70 + 500 ms), so the strict `<` fails first.
- The clause only matters in Recovering (10 ms spacing), and no test covers it.

**R3-F3 (Low): the renewing probe's "taken at once" spacing is not pinned.** The mutant `Renewing => RefreshSpacing` survives.

**R3-F4 (Low, fixed by the coordinator): stray files from a relative screenshot path.** A `private/tmp/…` tree inside the repo held 8 PNG and `.import` files that Godot imported. They came from pre-freeze runs that passed a relative screenshot path. The coordinator deleted them; see R3-2.

## R3-2. Identity

| Item | Result |
| :-- | :-- |
| Bundle | 95/95 entries of `candidate-4/bundle-identity.txt` match the AppBundle on disk. `968b6590…` is that identity file's own sha256 (97 lines). |
| Served, before and after all Chrome | Simulation.wasm `39e56adc`, sim worker.js `b892a211`, clock-master.js `8af6087a`, workshop-client.js `9fdea5f8`, clock-ring.js `1c348f7a`, animation worker.js `abf390c3`, Animation.Worker.wasm `27d8c398`, godot.pck `699ae97c`: MATCH, identical at start and end. All five served scripts are byte-equal to their sources. |
| Source vs `source-identity.txt` (156 files, `885a9f83…`) | **The coordinator's claims hold.** Before the deletion the tree matched all 156 files. Afterwards it differs by exactly 7 lines, all `deleted private/tmp/…/simulation-unresponsive*`, and by nothing else. No such path is in the bundle identity. `private/` and the `.godot/imported/simulation-unresponsive*` cache entries are gone. |

## R3-3. Gates (mine)

| Gate | Result |
| :-- | :-- |
| slnx build; the 26 other projects | 0 warnings, 0 errors (`build-slnx.log`, `build-projects.log`) |
| `dotnet test CuriousContraptions.slnx` | 1507/1507; Coverage.Tests 78/78 |
| Node harnesses (10 files) | 76/76 |
| LifecycleContract / WireContract / Ownership | 510/0; 109/0; positive, 22 + 15 negatives rejected |
| Murdoch probes | Clock stress 28/28; Save/Load cycles 3/3; ledger 10/10 (0 L7 notes) |
| Replay-can-fail probe | Passes: the control replays exactly, and a 20 px-lifted ball is rejected by her `assertReplaysExactly`. **The proof test's second half can fail** (it is copied verbatim; `murdoch-replay-can-fail.tap`). |
| **Cumulative ×2** (one process per file, per-file TAP) | **Run 1: 33/33 exit 0, 140/140. Run 2: 33/33 exit 0, 140/140.** No recalibration, renewal, unresponsive or rejected line outside clock-master. clock-master's `closeFindings=1` is the test-8 control: the finding prints between `ok 7` and `ok 8`, and the test asserts it with `assert.rejects(driver.closeAfterSuite(), …)`. Her `renewedLate=2` is two lines (the test's JSON summary and the driver's report) for one renewal. |
| Startup offsets (320 sessions) | −49.9 to **+115.1 µs**. Nine sessions are beyond ±20 µs, clustered: cat-002 #5–6, cat-062 #6–7, cat-015b #3–5 (115.1 → 67.4 → 30.1, decaying like a host slew), engine-core-2a1 #5, 2a2 #1. All were accepted (R2-F3), with 0 outliers set aside. **This confirms R2-F3 in practice:** the cat-015b #3 session, unloaded, would have been refused by candidate 3's 0.1 ms bound. |

## R3-4. Changes verified (my mutations)

| Change | Status |
| :-- | :-- |
| D23 renewal | Killed: no overlap check (3), renewing a running requester (1), staying Renewing (2), consumers reading while Renewing (6), renewal discarding the calibration (4). Consumers hold, because `IsQualified` is false while Renewing and both `TryMap` and `TryMasterNow` call `Observe` first. Classify still runs before mutation (BH8). An epoch change during the lapse recalibrates (`MasterRederived`) before renewal is considered. A stale or non-accepted reply while Renewing recalibrates: the safe side. Survivors: R3-F2 and R3-F3. Open: R3-F1. |
| R2-F2 (J6–J9) | **All four killed** by her new node tests. |
| R2-F3 wide offset | Killed: not recorded. Accepted and applied in Chrome (above). |
| R2-F4 | Killed: a header literal reintroduced (`NoLiteralAbiOffsetRemains(WorldHeader)`). The remaining records are owned by Story 9A.44. |
| R2-F5 | Killed: the catalogue word `funnelBoost` in a solver file, and removing the `fSpring` entry (2 tests). |
| R2-F6 | Killed: a suite `after` hook using `close()` (guard test). |
| Chrome forced-disagreement control | Asserts the driver failure inside test 8 (`assert.rejects`). It is not a stray finding. |

## R3-5. The allocation-test fix (coordinator's question)

**Judgement:** a plausible, statistically supported mitigation, but not deterministic by proof, and the root cause is not established.

**What is deterministic by construction:**
- The window now contains only the `RecordTickDuration` call between two `GC.GetAllocatedBytesForCurrentThread` reads.
- Both the method and the measuring test are `AggressiveOptimization`, so neither is call-counted, tier-promoted or on-stack-replaced. The method's callees inline.
- So, if the cause was runtime tiering on this thread, the fix removes it by construction.

**Why that is not a proof:** the hypothesis was never confirmed (0/45 JIT-delta runs, and the worker-timeout knob 0/12). Other candidates are not excluded, for example the allocation-context accounting being adjusted when another thread's GC suspends this one. The evidence is 0 failures in 115 runs against a pre-fix rate of about 2/66, which gives p ≈ 0.03 for "unchanged". That supports the fix, but does not prove it.

**What would establish the cause:**
1. An EventPipe trace (`dotnet-trace`, `Microsoft-Windows-DotNETRuntime` GC + JIT + TieredCompilation + `GCAllocationTick`/`AllocationSampled` at a low threshold) of a failing pre-fix run under the parallel full suite. It would name the allocated type and stack inside the window.
2. Or a deterministic red: force the hypothesised mechanism on the pre-fix form (for example `DOTNET_TC_CallCountThreshold`/`DOTNET_TC_CallCountingDelayMs=0`, `DOTNET_OSR_HitLimit=1`, `DOTNET_TieredPGO`), then show the fix removes it.
3. Or an ablation: revert only `AggressiveOptimization`, keep the timestamps outside the window, and run about 200 full suites to see whether the failures return.

The self-diagnosing failure message is good. Recommendation: accept the fix as a mitigation, and record the cause as open with an owner.

## R3-6. Re-review conditions

- R3-F1: bound the renewing exchange's width (with a native test of the 60 ms move and a mutation that removes the bound), or the coordinator accepts the residual in writing.
- R3-F2: a Recovering-state test with a probe outstanding, plus the mutation killed.
- R3-F3 pinned (optional).

Everything else in R3-2 to R3-4 carries over while its inputs stay byte-identical.

# Round 4: candidate 5 terminal re-review (Murdoch-2, 19 Oct 2026)

**Verdict: Fail.** One Medium finding (R4-F1), which blocks under the tiers. The fresh-exchange side of R3-F1 is fixed and well pinned. The retained-interval side of the same overlap check is unbounded, and a moved clock is confirmed in Chrome.

R4-F1 is not a candidate-5 regression. `TryRetained` and `Expand` are unchanged since candidate 4, so the hole was present in round 3, and my round-3 review missed it. Under the owner's verification tiers, Lows (R4-F2 to R4-F5) are recorded and do not block.

Evidence: `evidence-9-4b/review-r4/`. Plan per the owner decision of 19 Oct 2026: Amelia's two cumulative runs are reused after verification (R4-1). No reviewer cumulative run.

## R4-1. Identity and reuse of Amelia's runs

- **Source.** `candidate-5/source-identity.txt` sha256 `7fc5b065…` has 149 entries (148 hashes and 1 deletion).
  - Every hash matched the tree before my runs (05:5xZ) and after them (07:14Z).
  - The modified, added and untracked file set equals the listed set.
  - No `private/` tree remains.
- **Bundle.** `bundle-identity.txt` sha256 `fe6b832d…`: all 95 entries match `AppBundle` after my runs. The main-checkout `dotnet build` did not touch the bundle.
- **Served hashes on :8060.** My eight files, including `godot.pck`, are recorded at the start (06:54:01Z) and end (07:09:14Z) of my Chrome checks (`chrome-gates-summary.txt`). They equal Amelia's `served-identity.txt` (05:18Z, before her gates). Her runs therefore ran between two matching served checks.
- **Her cumulative runs:**
  - run 1: 05:44:15Z to 06:10:32Z; run 2: 06:10:32Z to 06:36:49Z;
  - each run has 33 per-file TAPs, all `exit=0`, 140/140, 0 `not ok`;
  - every file except clock-master shows 0 close findings, recalibrations, renewals, unresponsive, rejected and wide offsets;
  - the tree was unchanged during her gates.
- **Clock-event attribution,** in both runs and in my clock-master run, by record position between `ok` lines:
  - test 3 (duty20): role 3, `Elapsed`, stateBefore 5;
  - test 4 (spin): the 3 `SimulationUnresponsive` lines;
  - test 5 (skew): role 1 and role 3 `MasterRederived`, stateBefore 5;
  - test 7 (stall proof): 1 `CCGPU_CLOCK_RENEWED_LATE`, role 1, `exchanges` 2;
  - test 8 (control): 1 `MappingLifetime` record, stateBefore 6, which appears 3 times as the raw line, the driver echo and the one close finding.
  - Every event comes from a test that causes it on purpose. Her `recal=3` counts lines, not records.

## R4-2. Gates and Chrome (mine)

| Check | Result |
| :-- | :-- |
| `dotnet build CuriousContraptions.slnx --no-incremental`; 26 projects | 0 warnings, 0 errors |
| `dotnet test` slnx | 1516/1516 and Coverage 78/78 |
| node `tools/*.test.mjs` | 76/76 |
| LifecycleContract; WireContract; Ownership | 510/0; 109/0; exit 0 |
| clock-master ×1 | 8/8, exit 0; events as in R4-1 |
| cat-017 ×1 (R2-F1's file) | 5/5, exit 0; 0 close findings, 0 recalibrations, 0 renewals |
| Probes: clock stress, 023b save/load cycles, bumper ledger | 28/28, 3/3, 10/10 |
| Stall proof and control under load-8, ×5 | 10/10. Each proof renewed at `exchanges` 2 (lapse age 729–782 ms). Each control: `MappingLifetime`, stateBefore 6 |
| `murdoch-long-stall` (new, R4-F1) | 2/2 recorded (below) |

Native probe `review-r4/probes/MurdochR4RenewalTests.cs.txt`: all 7 cases (6 tests, one a 2-case theory) pass on candidate 5 (`native-probe-outcomes.txt`).

## R4-3. Findings, ranked

**R4-F1 (Medium, blocking; present since candidate 4): the renewal still confirms a moved clock, through the retained interval.**
- *Harm.* After a renewal, `TryBest` drops every old slot because each is past its lifetime. So `TryMap` and `TryMasterNow` use only the fresh slot, and mapped times are correct (the `R4_3` midpoint assertion shows this). The harm is two things:
  - The move leaves no recalibration record, which is the D8 detection this rule exists to keep.
  - The display epoch is kept across the move. `WorkshopPoseHistory._displayWall` stays monotonic, so a backward move holds presentation (`AwaitingHistory`) for up to the move size. I found no wrong pose.
  - That is why I rate it Medium, not High.
- *Mechanism.* `TryRetained` widens the old interval by 500 ppm of its age, and nothing bounds the stall. The tolerance therefore grows by about 1 ms for every 2 s of stall.
- *Chrome proof* (`murdoch-long-stall.log`): a 6 s Browser main-thread stall, then a +3 ms jump of the Browser clock (1.5 × `MaximumMappedWidth`).
  - Result: `CCGPU_CLOCK_RENEWED_LATE`, `exchanges` 2, old width 6.693 ms, new width 0.470 ms, centre shift −3.0125 ms. There were 0 recalibrations, and the display epoch and derivation were kept.
  - Control: the same jump after the 700 ms stall is `MappingLifetime` from Renewing.
- *Native proof:* a 4 ms move after a 10 s stall, and a 50 ms move after a 2 min stall, both renew (`R4_3`).
- *Why it is in scope.* The decision text (epics.md) renews only on agreement "within the uncertainty already admitted". The mapping never admits drift beyond its 500 ms lifetime (`TryBest`). The rationale "a wide exchange cannot see a clock move smaller than its own width" applies equally to a drift-widened old interval.
- *Reachability.* Demonstrated only by the injected Chrome stall and the native cases. Plausible natural routes have not been verified for this page:
  - Chrome's throttling of timers in a hidden tab, which would stall the 10 ms `setInterval` service for seconds or more;
  - tab freezing.
- *Suggested fix* (one predicate, no new path): conclude a renewal only when the retained interval, drift-widened to now, plus 2 × `TimestampUncertainty.Maximum`, also fits `MaximumMappedWidth`. Otherwise the lapse is a `MappingLifetime` recalibration.
  - At the observed base width (about 0.58 ms) this renews stalls up to about 1.2 s and recalibrates longer ones.
  - The 700 ms proof still renews (observed old widths 1.30–1.34 ms).
  - *Cost of the fix.* Every stall longer than about 1.2 s would recalibrate and fail its suite. That is the false-recalibration risk the coordinator asked about.
    - Every late renewal in the evidence (Amelia's 5 + 2, my 7) came from a deliberately injected stall. cat-017 and every other file renewed 0 times, so nothing observed is lost.
    - How long natural stalls last is unknown.
    - The alternative is for the coordinator to accept the long-stall residual in writing, and to say so in D23.
- *Tests:* invert `R4_3`, add a 0 / +1 ns boundary, and add a mutation that removes the bound.

**R4-F2 (Low).** While renewing, a reply that is not the pending probe's answer (`WrongIdentity`) is a `MappingLifetime` recalibration. In Ready it is ignored (blind hunter 8) (`R4_4`). This conforms to the spec ("an unaccepted renewing reply"), and it is unreachable through the one-slot ring in normal operation. Either exclude `WrongIdentity` from ending a renewal or say so in D23.

**R4-F3 (Low): allocation statistics are overstated.**
- The "p ≈ 0.002" compares 0/200 with a 2/66 rate. It omits the 57 pre-fix-window runs that also failed 0 times (`allocation-hunt-jit-delta` 0/45, `worker-timeout1` 0/12).
- Pooled pre-fix (2/123) against the ablation (0/200): Fisher p ≈ 0.14. Against all post-fix runs (0/315): p ≈ 0.08.
- So neither "the attribute is not the mitigation" nor "the timestamp move is" is established. The record should say only that the cause is open and owned by P0-034 / 16.1, the mitigation is unproven, and there has been no recurrence in 315 runs.
- Stale tiering comments (`ClockRecalibrationCause` summary, `WorkshopTrace.cs:89-90`, `WorkshopTraceTests.cs:514`) are acknowledged and assigned to 9R.0.

**R4-F4 (Low): test gaps and equivalent code** (20 mutants, `review-r4/probes/run-mutations-r4-batch.sh`; 16 killed).
- Survivors:
  - M4-14: the renewal inherits a Recovering episode's sends. This is untested (a budget-accounting gap).
  - M4-06: the Renewing send cap is equivalent. After 8 sends, every outcome of the 8th reply ends the renewal, so the cap is unreachable.
  - M4-15: `ClearRecovery` at conclusion is equivalent apart from a later record's `recoverySends` field.
  - M4-18: the `QualifiesThroughRefresh(fresh)` conjunct in `UpdateAvailability` is equivalent to `covered`. This is pre-existing: in Recovering, coverage can only come from the fresh slot.
- Amelia's red-green logs do not record the mutant diffs.

**R4-F5 (Low): stale wording.**
- `docs/shared-clock-cadence.md:13` says "renewed and checked by one fresh exchange", and the next sentence contradicts it.
- The `clock-master.test.ts:336` comment says "one fresh exchange".

## R4-4. Answers to the coordinator's attack list

- **Width or overlap confirming a moved clock.**
  - Fresh side: fixed. Mutants M4-01, 02, 03, 12, 16 and 19 are killed, and `R4_1` (my round-3 probe inverted) passes.
  - Retained side: not fixed (R4-F1).
- **Budget sized from evidence; is exhaustion a recalibration?**
  - Yes, and the evidence supports 8 sends in 250 ms. Every stall seen this round concluded at exchange 2 of 8: 5 + 2 of Amelia's, 1 + 5 (load-8) + 1 (6 s) of mine. That includes all five under 8 busy loops.
  - Exhaustion without a clock move was not observed, and I rate it implausible at this margin. The binding bound under starvation would be `Elapsed` at 250 ms, not the 8 sends.
  - The 8th inconclusive exchange is `ProbeLimit` (M4-04 and M4-20 killed). The time bound is `Elapsed` (M4-05 killed).
- **Consumers hold through a multi-exchange renewal.** Yes:
  - `IsQualified` is false for the whole Renewing state;
  - `Inconclusive` changes no state (M4-09 killed by 13 tests);
  - Animation holds as `Recalibrating` (label only).
- **Exactly one record per lapse.** Yes:
  - `RENEWED_LATE` is emitted only at conclusion (M4-08 killed);
  - a reply after the time bound gives one `Elapsed` record and then seeds the fresh calibration (`R4_5`).
- **duty20 `Elapsed` from Recovering.** This is the intended path, not a reclassification.
  - The Recovering code paths are unchanged from candidate 4 apart from extracting the predicate.
  - `RequireRecoveryTime` runs before the lifetime check, so a Recovering requester stalled past 250 ms is always `Elapsed` (`R4_6`).
  - Candidate 4 simply did not record the cause.
- **Dead code or a second path.** The candidate-4 Renewing timeout branch is deleted; there is one time bound. The remaining redundancies (R4-F4) are equivalent, not second paths.
- **R3-F2** is verified: M4-10 (no Pending guard) is killed only by `ALapseInRecoveryWithAProbeOutstandingIsACountedRecalibration`.
- **R3-F3** is verified: M4-11 (Renewing falls to `StartupSpacing`) is killed by 4 tests, and Amelia's `RefreshSpacing` mutant is killed by `TheRenewingProbeIsSentAtOnceOnLapseDetection`.

## R4-5. Re-review conditions

- R4-F1 is fixed with the inverted probe, a boundary test and a killed mutant, or the coordinator accepts the long-stall residual in writing.
- Re-run on the next candidate: native tests, the R4 mutants touching the changed predicate, clock-master, `murdoch-long-stall`, and cat-017 once.
- Everything else here carries over while its inputs stay byte-identical.

My last Chrome run ended at 07:09:14Z. No Chrome or `yes` load is running.

# Round 5: candidate 6 re-review of R4-F1 and the round-4 Lows (Murdoch-2, 19 Oct 2026)

**Verdict: Pass** (scoped to R4-F1, R4-F3, R4-F4 and R4-F5). No High or Medium finding. Evidence: `evidence-9-4b/review-r5/`.

## R5-1. Identity and diff

- **Source.** `source-identity.txt` sha256 `d7536a1c…` has 149 entries.
  - All match the tree, and the file set is equal, both before my runs and after them (after the builds).
  - Against candidate 5, exactly 5 sources differ: `WorkshopClockMapping.cs`, `WorkshopClockDerivationTests.cs`, `docs/engine-contracts.md`, `docs/shared-clock-cadence.md` and `tools/e2e/clock-master.test.ts`. No file was added or removed.
- **Bundle.** `bundle-identity.txt` sha256 `102e9f12…`: all 95 entries match `AppBundle`. The changed entries are the republished wasm, boot files and `godot.pck`.
- **Served hashes on :8060.** All 8 files, including `godot.pck` `359ce6c5…` and Simulation.wasm `689c0a83…`, are equal at 07:37:10Z and 07:39:25Z, and equal to `served-identity.txt`.

## R5-2. R4-F1 fix

- **The change.** `renewed` now also requires `WithinMappedWidth(retained)`, the one shared predicate (`widened.Width + 2 × TimestampUncertainty.Maximum ≤ MaximumMappedWidth`). `QualifiesThroughRefresh` and `HasOffsetBudget` use the same predicate. No second path was added.
- **The 0.2 ms allowance is justified, not arbitrary.**
  - `TryMap` admits a capture ± its uncertainty, which is validated ≤ `TimestampUncertainty.Maximum` = 100 µs. So a mapped interval is the offset width plus at most 200 µs.
  - The bound therefore reads: "the old mapping could still map any admissible capture within `MaximumMappedWidth`", which is exactly the uncertainty the mapping already admits. It is the pre-existing refresh-coverage bound in `docs/engine-contracts.md`.
  - The effective old-interval limit is 1.8 ms. That is about a 1.4 s lapse for a 0.4 ms slot, and about 1.2 s at the observed base width of about 0.58 ms.
- **Native.** The new boundary test is exact: a 400,000 ns slot at 1.4 s renews, and 1 ns later it does not. My `R4_3` vulnerability assertions now fail on candidate 6, as intended. My other R4 cases pass.
- **Mutants (5 of 6 killed):**
  - dropping the old bound (R5-01): 5 kills, the inverted `R4_3` cases;
  - the bound made strict (R5-03): 2 kills;
  - the old bound without the sample allowance (R5-04): 3 kills;
  - the old bound evaluated over the refresh horizon (R5-05): 1 kill;
  - the renewal inheriting recovery's sends (R5-06, R4-F4): killed by `ARenewalAfterRecoveryStartsAFreshBudget`;
  - R5-02, loosening the shared bound by 1 ns, survives. The ceilinged drift makes the boundary test's step 2 ns. This is a Low precision gap; an odd-width boundary case would pin it.
- **Chrome (mine, 07:37–07:39Z):**
  - clock-master 8/8. Events are attributed as in round 4: test 7 renewed once (`exchanges` 3, old width 1.295 ms) and test 8 had 1 `MappingLifetime` from Renewing.
  - `murdoch-long-stall` 2/2: the 6 s stall plus a 3 ms jump is now `MappingLifetime` from Renewing with 0 renewals, and the 700 ms stall plus a 3 ms jump likewise.
  - cat-017 5/5 with 0 clock events.
- **Amelia's evidence, re-derived:**
  - clock-master 5× 8/8, each renewal at `exchanges` 2 with old widths 1.225–1.356 ms. The margin to the 1.8 ms limit is about 0.44 ms, roughly 0.44 s more stall.
  - cat-017, cat-023b, engine-core-2b1 and engine-core-2a1: 0 clock events.
  - The tree was unchanged during her gates.
- **Gates:** slnx build 0 warnings and 0 errors; `dotnet test` 1523/1523 and Coverage 78/78; node 76/76.

## R5-3. Lows

- R4-F3 is corrected in spec §12, with the pooled figures and the cause stated as open.
- R4-F4 is closed by R5-06.
- R4-F5 is fixed in both places.
- **Remaining Lows (non-blocking):**
  - R5-02, the 1 ns bound precision.
  - R4-F2, the foreign reply while renewing, which is unchanged and was not in scope.
  - The coordinator-accepted telemetry point: a too-long stall shares `MappingLifetime` with a genuine disagreement. Owner: 9R.0.
  - The stale tiering comments, assigned to 9R.0.

My last Chrome run ended at 07:39:25Z. Chrome is free.
