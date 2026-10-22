# Review: Story 9.4c Latch-Fixture Deferred Integrations

Independent adversarial review (pre-merge). The reviewer did not write the candidate and edited no candidate file.

- **Candidate:** worktree `story-9-4c`, base `55ee05b9`, uncommitted. `LatchSupplyFixtureTests.cs` sha256 `f9b1ec7f…ee206f`; the other identities are in `evidence-9-4c-review/identity.txt`.
- **Verdict: Incomplete.** The native proof passes and discriminates. One small documentation finding (F1) blocks it. After F1 is resolved and re-reviewed, the verdict becomes Pass-pending-merge. That is a pre-merge verdict only: no SnapshotApproval, because main will move (C-Bumper+Spring, OR) and must be merged first.
- **Browser qualification:** pending, by recipe only (post-catalogue E2E).

## Commands and results (evidence-9-4c-review/)

| Check | Result | Log |
|---|---|---|
| `dotnet build CuriousContraptions.slnx -c Debug` | 0 warnings, 0 errors | build.log |
| `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | tests 2288, E2eRunner 189, Coverage 77, BundleElements 145, WorkerAotGuard 17; 0 failed | test-full.log |
| CI "Test C# tools" step (pages.yml:51-64) under `bash -e` | 77 / 145 / 17 / 174 passed, exit 0 | ci-tools-step.log |
| `git diff --check` (new file intent-added, index restored afterwards) | exit 0 | diff-check.log |
| Throwaway-worktree baseline (fixture filter) | 10/10 passed | mutation-R0-baseline.log |

## Mutations (throwaway worktree, each reverted; worktree removed and pruned)

| ID | Mutation | Killed by | Log |
|---|---|---|---|
| R1 | `Resolve` ignores the latch contact | 7/10 fail: Set, both Reset rows, both ball rows, the no-B control and off/on | mutation-R1-contact-ignored.log |
| R2 | Battery enable ignored | Authored-Disabled control and supply off/on | mutation-R2-enable-ignored.log |
| R3 | Latch `ResetIn` ignored | Reset-open, mid-stroke and both ball rows | mutation-R3-latch-reset-ignored.log |
| R4 | Route reads the latch one tick later (engine lag +1) | Set, Reset-open and mid-stroke. Run/Reset also fails, partly because of a stale mutation field | mutation-R4-lag-plus-one.log |
| T0 / T2 | Test-side `SupplyLagTicks` set to 0 / 2 against the real engine | T0: Set, Reset-open, mid-stroke. T2: Set only (the Reset rows' far edge is pinned only at lag 1; R4 covers the engine side) | mutation-T0/T2-*.log |
| R5 | Return spring 14 → 0.001 N/m | 5/10, including Reset-open, mid-stroke and both ball rows | mutation-R5-spring-disabled.log |
| R6 | Blade collider moved 3 m sideways | 5/10. The crush row is killed by its **guard** (the ball has already passed), not by the fact-14 assertions | mutation-R6-blade-collider-removed.log |
| R7 | Spring 3000 N/m | 9/10, by guards and opening (the blade never opens), not by the crush assertions | mutation-R7-crushing-spring.log |
| R8 | Contact softness 60 → 1 Hz (crush proxy) | Only the crush row fails, at its assertion "temporary penetration ≤ 5% of the blade, tick 221". This is the real fact-14 discriminator | mutation-R8-soft-contacts-crush.log |

## Criterion and impact matrix

| Criterion | Evidence | Status |
|---|---|---|
| AC1 Set; N19 no topology change | `SetCloses…`: edges Off@T and On@T+1, first opening tick T+1. `Construction` has value equality (`WorkshopConstruction` record struct, `WorkshopConnections : IEquatable`), compared after every tick and after Reset | Pass. Lag value: see D-T14 |
| AC2 Reset when open; N17; spring return | Latch and lamp clear at T′, unsupplied from T′+1, spring-damper law residual ≤ 0.5 m/s², energy bound, closed within 240 ticks | Pass |
| AC3 mid-stroke; upstream ball; ball partly through (fact 14) | Guards (0.05, 1.50) m never skip; coasting until reversal follows the frozen row "speed strictly decreasing until it reverses"; R8 kills the crush row | Pass. The "State Blocked" part of fact 14 is not proven: F1 |
| AC4 controls | No-B (the ball passes downstream), authored Disabled, off/on with memory and occurrence unchanged | Pass |
| AC5 one battery, one gate | Builder | Pass |
| AC6 Run/Reset and Save/Load | `AssertSameState` and presentation bits Built==Reset, construction equality, tick-for-tick replay after Reset. Real `WorkshopSaveCodec` round trip with byte re-encode equality, five typed connections, replay identical | Pass |
| AC7 Chrome | Superseded in cadence by the 21 Oct policy. Recipes are in the spec and TODO | Pending (recorded) |
| Production path | Real `WorkshopSimulation` on `WorkshopGpuDevice` + `BrowserGpuTransport`. Only the clock and installation doubles are used (the existing `WorkshopPhysicsEngineTests` pattern). No setters; supply changes only through `ResetIn` or the in-run enable | Pass |
| Durability | Committed reads only. The record factories (`WorkshopInput.Battery/Latch/Lamp`) are confined to `Machine` | Pass. See N2 |

## Coordinator decisions evaluated

- **D-T14 (1-tick lag): correct.**
  - In `WorkshopGpuDevice.cs:120-123`, `AdvanceScheduled`, then `Compose`, read the committed checkpoint before the physics step.
  - The strike's edge and latch resolution happen only in `ConsumeScheduled` (`:185`), after the step.
  - `ContactClosed` reads `Activations` (`ActivationNetwork.cs:243-253`). Presentation `SupplyClosed` is that tick's composed input.
  - So T+1 is the earliest possible route change. `BothGateTests:150,154` pins the same lag (120→121, 180→181).
  - The "+2" came from the legacy command queue: `git show a6c914e:CuriousContraptions.tests/LatchTests.cs` L40-45 show `world.Activate(set)`, then Off, `Step`, Off, `Step`, On.
  - The tests pin both sides of the Set edge and of each Reset edge (On@T′ / Off@T′+1).
  - Required record (non-blocking): AC1, the frozen Set row and the TODO "awaiting confirmation" still read +2. Add a dated line recording the coordinator decision, with owner renegotiation of the frozen text still open.
- **D-Blocked residual: observation confirmed; no 9.4c acceptance requires it.**
  - `probe-4-creep.log` shows v = −1.083e-5 m/s, constant from tick 241 to 2400.
  - `PhysicsPresentationRead.cs:29` requires |v| < 1e-5, so the indicator shows "unpowered" instead of "Blocked" (`WorkshopDeclaredPresentation.cs:282`).
  - The frozen row does not require the state, but see F1.

## Findings

**F1 (blocking, documentation and ownership).**
- *Criterion:* current documents must be accurate, and a known defect stays explicit with an owner (AGENTS.md).
- *Problem:* the new CAT-051 §5 status line says fact 14 is "proven natively". Fact 14 (row 14) includes "State Blocked", and that state reads false on this engine. The "assigned to C-Gate" handoff is recorded only in the spec's Implementation Notes: the C-Gate, 9A.19 and 9A.21 ACs and TODO do not mention it. The threshold is also CAT-051 open question 3 (owner).
- *Risk:* C-Gate, or the post-catalogue E2E, inherits an overclaim and loses the defect.
- *Smallest resolving check:*
  - amend the CAT-051 status line to exclude the Blocked classification and link open question 3;
  - add a TODO (or C-Gate) note naming the owner (C-Gate, threshold decision owner Q3);
  - the reviewer re-reads both lines.

**N1 (non-blocking).** D6's mapping, under which "`cat-005-n19`/`cat-051-interrupt` pass" means `LatchSupplyFixtureTests` passes, exists only in this spec. The C-Gate, 9A.39 and 9A.43 ACs do not mention it. Consider a one-line pointer next to those ACs.

**N2 (non-blocking).** C-Lamp+Detector (position 36) deletes `WorkshopInput.Lamp`, so it must also edit `Machine`. D5 lists only C-Gate, 9A.39 and C-Memory.

**N3 (non-blocking).** The gate drive debits 0 battery energy. CAT-005 already lists powered-work verification as outstanding (line 43); 9A.39 or the owner should keep it explicit.

**N4 (non-blocking process).** The implementer self-reported two edits made outside the Write and Edit tools (the probe file, since deleted, and the spec's `status` line). The content is as intended.

## Re-review scope after F1

Documentation only: CAT-051 §5, plus TODO or C-Gate. No runtime input changes, so this native evidence is reused. Before SnapshotApproval, merge main, then run one matching build, the full `--no-build` test run and the fixture filter again.

## Re-review 1 (22 Oct 2026): F1 fixes, documentation only

**Verdict: Pass-pending-merge** (pre-merge). This supersedes the Incomplete verdict above. No SnapshotApproval: main must be merged first, and then the post-merge checks listed above apply.

- **F1: resolved.**
  - The CAT-051 §5 status line now says the "State Blocked" part of fact 14 is **not** proven. It cites the −1.083e-5 m/s residual against the 1e-5 predicate, links `#6-open-questions` (that heading exists), and names the C-Gate assignment.
  - TODO.md records the dated handoff, with the C-Gate implementation owner, the native-proof requirement and OQ3.
  - The epics.md C-Gate section has the same dated coordinator decision.
- **D-T14 record: resolved.** Spec D3 is dated as a coordinator decision under owner delegation and supersedes the "+2" wording in the frozen Set row, AC1 and CAT-037 T14. The AC1 note and the Spec Change Log agree. The frozen block is byte-identical to HEAD: the awk-extracted block hashes to sha256 `47bd45a8…4a83` in both HEAD and the worktree.
- **N1: resolved.** A native-guard pointer line now sits under the suite ACs of 9A.19 (C-Gate), 9A.39 and 9A.43, and keeps the browser suites pending.
- **No runtime input changed.**
  - `LatchSupplyFixtureTests.cs` sha256 is still `f9b1ec7f…ee206f`.
  - `git diff --stat` over engine, elements, Simulation, ui and tests shows only the earlier csproj Compile Include.
  - So the build, test, CI and mutation evidence above is reused unchanged.
  - `git diff --check`: exit 0 (re-run).
- **Residual (non-blocking):**
  - CAT-037 T14 itself is not annotated. D3 and TODO record that it is superseded.
  - N2 and N3 stay open as notes.
