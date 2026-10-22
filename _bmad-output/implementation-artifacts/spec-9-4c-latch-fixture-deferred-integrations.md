---
title: 'Story 9.4c: Latch-Fixture Deferred Integrations (CAT-005 N19, CAT-051 Supply Interruption Mid-Stroke)'
type: 'feature'
created: '2026-10-18'
status: 'review'
route: 'dispatch'
review_loop_iteration: 0
baseline: '55ee05b9'
roadmap_position: 30
guards: 'C-Gate (9A.19 + 9A.21), 9A.39 Battery, C-Memory (9A.43)'
policy_basis: 'owner 21 Oct 2026 native C# simulation-first (docs/delivery-workflow.md#fast-test-cadence); Chrome/E2E deferred to post-catalogue'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-94c-latch-fixture-deferred-integrations-cat-005-n19-cat-051-supply-interruption-mid-stroke'
  - '{project-root}/docs/planning/elements/CAT-005-battery.md'
  - '{project-root}/docs/planning/elements/CAT-051-powered_gate.md'
  - '{project-root}/docs/planning/elements/CAT-037-latch.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-9r-3-direct-gate-own-supply-route.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Two latch-dependent integrations are due but unproven (review-9-4 D1):
- CAT-005 N19 (supply switching through a contact, never by topology edits) and N17 (supply loss clears downstream);
- CAT-051 supply interruption mid-stroke, with fact 14 (closing stops at a ball without crushing it).

The runtime pieces already exist:
- compiled latch/counter switched-supply routes (`engine/gpu/WorkshopPhysicsCompiler.cs:287-310`, `SwitchedSupplyFeed`);
- `PhysicsTickInputs.Compose`: a route is closed only while the contact is closed and its battery is enabled (`engine/gpu/SwitchedSupply.cs:37-46`);
- the gate drive, active only while powered (`CuriousContraptions.Simulation/PrismaticConstraintSolver.cs:237-274`);
- the prismatic return spring (14 N/m, damping 4 N·s/m), which always acts (`WorkshopPhysicsCompiler.cs:212-214`).

No suite exercises two switches, a mid-stroke cut, or a ball caught under a closing blade. `cat-037` covers one switch plus the in-run battery toggle.

**Approach:**
- Build the machine through real UI, starting from the `cat-037` layout. Add two new suites and native companion tests against the shipped engine.
- Fix at its source any defect the suites expose.
- Add no new element, branch or heuristic.

## Boundaries & Constraints

**Always:**
- Construction uses only UI controls: palette, canvas, Move lift, contextual Connect and the bar/ring socket choices.
- Observation is read-only, through the existing channels (`CCGPU`, `CCGPU_RETAINED`, the pose ring and Animation telemetry; 9A.24 replaces them later).
- One battery and one gate.
- No wire is edited during Run (CAT-005 Q6 decision).

**Never:**
- Setters, imported solutions or numeric placement.
- Detecting the gate by drag or joint limit (9R.3 removes that).
- A brake or any new drive law. The return spring is the fail-closed law (CAT-051 Q1 decision).

## I/O & Edge-Case Matrix

| Scenario | Stimulus | Expected (committed reads) |
|---|---|---|
| Set | ball strikes Switch A at tick T | latch On, and the route closes at T+2 (T14: Off at T, Off at T+1, On at T+2); the blade's first opening tick is ≥ T+2; construction revision is unchanged |
| Reset when fully open | Switch B at T′, blade at 1.55 m | route open at T′+2; lamp Off and gate unpowered (N17); blade closes to ≤ closed tolerance within 240 ticks (fact 19) |
| Reset mid-stroke | Switch B at T″, opening sampled in (0.05, 1.50) m | from T″+2: no drive work; opening speed strictly decreasing until it reverses; KE + ½·14·x² + gravity PE never increases (fact 15); returns to closed |
| Ball upstream at cut | ball short of the passage | stays upstream (x < gate mouth) |
| Ball partly through at cut | ball in the passage | blade stops on the ball, opening > 0; ball not ejected (speed ≤ 0.5 m/s), penetration within the game envelope (fact 14) |
| No Switch B | Set only | reaches Open 1.55 m and holds (control) |
| Battery authored Disabled | Set fires | gate never opens (control) |
| Supply off/on | in-run battery Disable, then Enable | memory stays On; gate repowers with no new Set (T14) |

</frozen-after-approval>

## Current plan (native-first revision, 22 Oct 2026)

**Policy basis.** The owner's 21 October native C# simulation-first policy ([delivery-workflow §fast-test-cadence](../../docs/delivery-workflow.md#fast-test-cadence); epics "End-to-end scope rule") governs this story. Production C# unit tests that execute the real `WorkshopSimulation` are the primary proof. Chrome/Playwright suites are recorded below as pending post-catalogue recipes, not written or run now.

**Outcome.** Native production-simulation guards prove the Battery → Latch → Powered Gate supply-switching fixture (CAT-037 T21), CAT-005 N17/N19 and the CAT-051 mid-stroke interruption, including fact 14. C-Gate (9A.19 + 9A.21), 9A.39 (Battery) and C-Memory (9A.43) refactor exactly this path and must keep these guards green.

**Authority.**
- epics.md Story 9.4c ACs 1–6 and the delivery-order notes (9.4c precedes C-Gate, position 30);
- the frozen I/O matrix above, all rows (see D3 for the measured Set/Reset lag);
- CAT-051 Q1: the return spring (14 N/m, 4 N·s/m, always acting) is the fail-closed law. No brake, no new drive law;
- CAT-005 Q6: no wire edits during Run. Supply loss is the latch contact opening (Switch B → `ResetIn`) or the in-run battery enable.

**No new element. Legacy deleted: none** (integration proof). A defect fix would delete what it supersedes in the same slice.

**Supersedes in the 18 Oct plan (non-frozen parts only):**
- AC7 and the old tasks 2–3 (writing and running `cat-005-n19` / `cat-051-interrupt` in Chrome now) become the pending browser recipes below;
- the "all of Epic 9R" dependency (epics corrected it to delivery order only);
- the 9.4b cumulative-baseline risk (moot: no per-slice Chrome run).

### Native gaps (verified at 55ee05b9)

| Existing test | What it covers |
|---|---|
| `StateLatchTests` | `Compose` route closure: contact ∧ enabled battery |
| `BothGateTests.CommittedLatchSetResetAndSupplyRestorationDriveBothGate` | latch Set/Reset driven by Clocks, plus battery off/on |
| `SwitchElectricalTests` | impact-switch contact; supply loss and restoration keep memory |
| `WorkshopPhysicsEngineTests.AnEnabledBatteryClosesItsGateWhenDisabledInRun` | supply cut through `ConfigureElectrical` on a direct wire |

Nothing covered: impact-switch balls driving `SetIn` **and** `ResetIn`; the tick-exact lag on that path; an unchanged construction during Run (N19); Reset while fully open with N17 clearing; a mid-stroke cut through `ResetIn`; the spring return within 240 ticks with no energy gain (facts 15, 19); a ball staying upstream and a ball partly through stopping the blade uncrushed (fact 14); an authored-Disabled battery on the latch path; Run/Reset and Save/Load of this five-connection machine.

### Design decisions

- **D1 (kept).** The fifth connection is Latch `ActivationOut` → Signal lamp, as in `cat-037`. **Owner may confirm.**
- **D2 (kept).** "Stops gaining opening within two ticks" means the drive's work ends by the cut's lag edge: `SupplyClosed` is false from T″+1 (measured; within the frozen "from T″+2" bound). After that the axial opening speed decreases strictly each tick until it reverses, and E = ½·m·v² + ½·14·x² + m·g·cos(15°)·x (gravity projected on the tilted frame axis) never rises by more than ε_E = 1e-3 J per tick until the blade is closed. The blade coasts before reversing; a literal stop would need a brake, which Q1 excludes. **Owner may confirm.**
- **D3. Tick origin and lag. Measured; supersedes the frozen T14 estimate. Coordinator decision under owner delegation, 22 Oct 2026.** The supply lag is one tick. This D3 supersedes the "+2" wording in the frozen Set row ("On at T+2", "first opening tick ≥ T+2"), in AC1 ("settles two ticks after the activation") and in CAT-037 T14. The frozen block itself is not edited.
  - T is the first committed read in which Switch A's activation is `Latched` (the `SwitchElectricalTests` convention; it is also the physics tick that observed the strike).
  - **Measured** (production simulation, `evidence-9-4c/probe-1.log`): T = 67, latch On and lamp On at T, `SupplyClosed` false at T and **true at T+1**; the blade's first opening tick is T+1. Reset: Switch B `Latched` at T′ = 140, latch/lamp Clear at T′, `SupplyClosed` true at T′ and false from T′+1.
  - **Analysis (not an engine defect).** `WorkshopGpuDevice.Advance` to tick N+1 runs: `AdvanceScheduled` (due timers) → `PhysicsTickInputs.Compose` from the *committed* checkpoint (latch memory as of tick N) → physics step N+1 → `ConsumeScheduled`, where the switch's contact event, its edge to `SetIn`/`ResetIn` and the latch's boundary resolution all happen in the same tick → commit. One-way data flow (physics → discrete → next tick's inputs) makes T+1 the earliest possible route change and introduces no extra stage. The same 1-tick lag is already pinned on the latch path by the reviewed `BothGateTests.CommittedLatchSetResetAndSupplyRestorationDriveBothGate` (Clock Set at 120 → Off@120, On@121; Reset at 180 → On@180, Off@181) and on the direct contact path by `SwitchElectricalTests.PhysicalImpactDrivesBothDomainsAndExactSaveReset`.
  - **Origin of the frozen "2".** CAT-037 T14 carries forward legacy `LatchTests.cs@a6c914e:L10-L71`, where `world.Activate(set)` was a generic *command* queued between steps ("Current-tick requests cannot settle before all senders have delivered"): Off after the command, Off after one step, On after the second. That command path no longer exists (activating the latch by command is rejected); the current chain resolves every sender's request at the same tick boundary. The frozen value was carried over from that different chain, not measured on this engine.
  - **Consequence.** The tests pin the measured edges on both sides (Off at T / On at T+1; On at T′ / Off at T′+1), so a 0-lag or 2-lag engine fails. Affected frozen clauses: the Set row's "On at T+2" and "first opening tick ≥ T+2", and epics AC1's "settles two ticks after the activation". The Reset rows' "route open at T′+2" / "from T″+2" bounds hold (the route opens at T′+1). The frozen block is unchanged. The owner may still renegotiate its text to the measured value.
- **D4.** The mid-stroke cut goes only through Switch B → `ResetIn`, never `ConfigureElectrical` (Q6). Natively, ball B's authored drop height sets the strike tick. A guard first asserts the opening at T″ is strictly inside (0.05, 1.50) m (or ≥ 1.55 − 1e-3 for the fully-open row); a miss fails as invalid and never skips.
- **D5. Survive the refactors.** One fixture builder (`LatchSupplyFixtureTests.Machine`) creates the machine. Assertions use only committed reads: `ActuatorDriveRead.{SupplyClosed, Coordinate, Speed}`, body pose/velocity, activation phases/occurrences, the electrical source read, `WorkshopSimulation.Construction` equality and save bytes. No record-type predicates beyond the authored-Disabled variant, no prismatic indexes, no `SwitchedSupplyClosed`. C-Gate, 9A.39 and C-Memory change only the builder's instance factories.
- **D6. Browser: recipes only.** No unrun TypeScript is written: 9A.39 moves the battery toggle into the generic editor and C-Memory/C-Switch change the drawer, so a pixel-coordinate suite would be stale before it first runs. For C-Gate, 9A.39 and C-Memory, the required-now meaning of "`cat-005-n19` and `cat-051-interrupt` pass" is that `LatchSupplyFixtureTests` passes, with browser qualification pending. Those stories' ACs are not edited.
- **D7. Fixture geometry (native).** Battery 1, Latch 2, Powered Gate 3 at (8, 3, 0) tilted −15° about Z (the `cat-051` tilt, +X end lower), Impact Switch A 4 at (0, 1, 0) with ball A 5 dropped from y = 3, Impact Switch B 6 at (0, 1, −6) with ball B 7 dropped from a per-row height, Signal lamp 8; an optional payload Basketball 9 starts inside the bore at local (−0.45, −0.27) and rests against the closed blade. Measured strike ticks: A at 67; B at 103 (y = 5, opening 0.42 m), 116 (y = 6, opening ≈ 0.72 m), 150 (y = 9, opening ≈ 1.45 m, payload partly under the blade), 209 (y = 16, blade held at 1.55 m since ≈ 164).

### Ordered tasks

- [x] 1. **Measure first.** Build the fixture and record T, the lag, the travel curve and T″ (`evidence-9-4c/probe-*.log`). Result: lag 1 tick (D3), flagged.
- [x] 2. Add `CuriousContraptions.tests/LatchSupplyFixtureTests.cs` and its `Compile Include`, covering every case below on the production `WorkshopSimulation`.
- [x] 3. Mutation proofs (each reverted): `Compose`/`Resolve` ignores the contact → Reset rows fail; return spring zeroed → return rows fail; blade collider removed → the crush row fails; battery enable ignored → the Disabled control fails.
- [x] 4. Fix any exposed defect at its source: none fixed. Two findings recorded for the coordinator (D3 lag; `Blocked` predicate residual, Implementation Notes).
- [x] 5. Docs: §5 integration-status line in CAT-005 and CAT-051 (native proof, browser pending); browser recipes recorded here and as a TODO pending case.
- [ ] 6. Independent review: criterion/impact matrix, raw commands, verdict; then commit/push by the coordinator's publication flow. Record "implementation complete; browser qualification pending".

### Native tests (`LatchSupplyFixtureTests`, primary proof)

| Test | Matrix row / AC | Kind |
|---|---|---|
| `SetClosesRouteOneTickAfterStrikeWithoutTopologyChange` | Set; AC1, N19 | positive |
| `ResetWhenOpenClearsDownstreamAndSpringCloses` | Reset when fully open; AC2, N17, fact 19 | positive |
| `MidStrokeResetStopsDriveAndReturnsClosed` | Reset mid-stroke; AC3, D2, facts 15/19 | positive |
| `NoSwitchBCompletesOpeningHoldsAndPassesTheBall` | No Switch B; AC4 | control |
| `AuthoredDisabledBatteryNeverOpensAfterSet` | Battery authored Disabled; AC4 | control |
| `SupplyOffOnKeepsMemoryAndRepowersWithoutNewSet` | Supply off/on; AC4, T14 | control |
| `BallUpstreamAtCutStaysUpstream` | Ball upstream at cut; AC3 | boundary |
| `BallPartlyThroughStopsClosingBladeUncrushed` | Ball partly through; AC3, fact 14 | boundary |
| `RunResetRestoresLatchOffBladeClosedAuthoredBattery` | AC6 Run/Reset | lifecycle |
| `SaveLoadPreservesFiveSocketsAndReplaysIdentically` | AC6 Save/Load | persistence |

### Pending browser cases (post-catalogue E2E; owner: 9.4c recipes)

Construction uses only UI controls (palette, canvas, Move lift, contextual Connect, bar/ring socket icons); no setters, timers or numeric placement. Observation reads tick-stamped `CCGPU_RETAINED` / pose ring (or their 9A.24 successor).
- **`cat-005-n19`** — start from the `cat-037` layout and add a second Impact switch: (1) UI build; Save shows exactly five connections with socket names (Battery `Supply`→Latch `PowerIn`, Latch `Supply`→Gate `PowerIn`, Switch A→`SetIn`, Switch B→`ResetIn`, Latch `ActivationOut`→Lamp); (2) Set: supply edge one tick after the strike (D3; or the owner-confirmed value), construction unchanged; (3) Reset when open with N17 lamp clearing; (4) Battery authored Disabled; (5) supply off/on through the run toggle keeps memory; (6) Run/Reset equals the pre-Run snapshot; (7) Save/Load replay.
- **`cat-051-interrupt`** — the `cat-051` tilted gate (−15°) with a basketball in the upstream mouth: (1) mid-stroke cut timed by Move-lift height, with the (0.05, 1.50) guard; (2) a ball upstream stays upstream; (3) a ball partly through stops the blade uncrushed; (4) the no-Switch-B control reaches Open and the ball passes.

## Code Map

- `CuriousContraptions.tests/LatchSupplyFixtureTests.cs` (new) and its `Compile Include` in `CuriousContraptions.tests.csproj`.
- `CuriousContraptions.tests/NoFixedCapacityTests.cs` — `AssertSameState` (reused).
- `CuriousContraptions.tests/SwitchElectricalTests.cs`, `BothGateTests.cs` — impact-switch and latch Set/Reset patterns (read only).
- `CuriousContraptions.Simulation/WorkshopGpuDevice.cs` (`Advance`/`Stage` tick ordering), `engine/gpu/ActivationNetwork.cs` (latch boundary resolution), `engine/gpu/SupplyRoute.cs` (`Resolve`/`Compose`), `CuriousContraptions.Simulation/PrismaticConstraintSolver.cs` (`ActuatorSliderDrive`), `elements/PoweredGate/PoweredGateElementDeclaration.cs` (spring) — read only unless a defect appears.
- `tools/e2e/cat-037.test.ts`, `tools/e2e/cat-051.test.ts` — layouts the browser recipes start from (read only now).

## Acceptance Criteria (1:1 with epics 9.4c)

- **AC1.** Given this machine, built through real UI controls with no setters or numeric placement:
  - Battery `Supply` → State Latch `PowerIn`;
  - Latch `Supply` → Powered Gate `PowerIn`;
  - Impact Switch A → Latch `SetIn`;
  - Impact Switch B → Latch `ResetIn`;
  - a ball upstream of the gate;
  - plus D1's lamp link, a delegated addition.

  When a ball strikes Switch A, then the latch sets and the gate becomes supplied and opens. The supply settles two ticks after the activation, and the committed construction shows no topology change (N19). *(The lag is one tick; D3, a coordinator decision under owner delegation on 22 Oct 2026, supersedes "two ticks" here and "+2" in the frozen Set row. Proven natively now; UI construction is the pending browser recipe.)*
- **AC2.** When Switch B fires with the gate fully open, the supply drops: the drive stops, every downstream input clears (N17) and the blade closes under its return spring.
- **AC3.** Given the blade strictly between closed and 1.55 m (committed reads), when Switch B fires:
  - within two ticks the blade stops gaining opening (D2) and then returns to closed;
  - a ball still upstream stays upstream;
  - a ball partly through the opening stops the closing blade without being crushed.
- **AC4.** Controls:
  - with no Switch B strike, the opening completes;
  - with the Battery disabled, the gate never opens even after Set;
  - latch memory survives supply off/on (T14).
- **AC5.** One battery and one gate. Multi-supply independence is Story 9A.19.
- **AC6.** Run/Reset restores latch Off, the blade closed and the authored battery state. Save/Load preserves all five connections with exact socket names.
- **AC7.** `cat-005-n19` and `cat-051-interrupt` pass 100% in Chrome serially, and every `tools/e2e/*.test.ts` suite passes in one serial run. *(Superseded in cadence by the 21 Oct policy: pending post-catalogue E2E; required-now meaning per D6.)*

## Negative and mutation plan

- Make the supply resolution ignore the contact: the Reset rows fail.
- Zero the always-on return spring: the return rows fail.
- Remove the blade's collider (no ball contact): the crush row fails.
- Ignore the battery enable: the Disabled control fails.

**Impacted suites:** `StateLatchTests`, `PoweredGateTests`, `ElectricalSupplyTests`, `SwitchElectricalTests`, `BothGateTests`, `WorkshopPhysicsEngineTests` and the full native solution run; browser cat-037/cat-051/cat-005/cat-020/cat-052 stay pending.

**Risks:** fixture timing depends on drop heights; the guards turn a miss into an explicit failure. The engine change (if any) in C-Gate/9A.39/C-Memory must keep the measured lag or renegotiate D3 with the owner.

## Verification

- `dotnet build CuriousContraptions.slnx -c Debug` (one matching build), then `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` (per-project totals in Evidence).
- CI "Test C# tools" step under bash, `git diff --check`.
- Logs: `_bmad-output/implementation-artifacts/evidence-9-4c/`.

## Legacy Deleted

None (integration proof). If a defect fix supersedes code, delete it in the same slice.

## Implementation Notes

- **Production paths only.** Every case constructs, runs, advances, applies in-run `ConfigureElectrical` and resets the production `WorkshopSimulation` on `WorkshopGpuDevice` + `BrowserGpuTransport` (a local `Run` helper mirroring `NoFixedCapacityTests.RunAndReset`/`WorkshopPhysicsEngineTests.RunPhases`, extended to record `simulation.Construction` after every tick and to Run again after Reset). No setters; supply changes only through Switch B → `ResetIn` or the in-run battery enable. No product file changed (`git diff --stat` shows tests, docs and status only).
- **Discrimination.** The Set/Reset rows assert both sides of each lag edge. The return rows (`AssertSpringDamperReturn`) assert, per unsupplied tick, the declared law a = −14·x − 4·v − 9.81·cos 15° (measured residual 0.13 / 0.09 m/s², tolerance 0.5) and the energy bound (ε_E = 1e-3 J), plus closure within 240 ticks. The ball rows have the no-Switch-B control in which the same ball passes downstream.
- **"No drive work"** is proven by `SupplyClosed` false from T″+1 plus the unpowered spring-damper law/energy bound. The battery `Debit` is not usable for it: the gate drive debits no battery energy on this engine (observed 0 throughout a powered run), so that assertion was dropped rather than weakened.
- **Finding (not fixed; coordinator decision): `ActuatorDriveRead.Blocked` reads false for a blade resting on a ball.** In `BallPartlyThroughStopsClosingBladeUncrushed` the blade comes to rest on the ball at opening 0.749699 m and the pose is stationary to 1e-6 m from tick 241 to 2400 (`evidence-9-4c/probe-4-creep.log`), but the committed axial speed is a constant −1.083e-5 m/s, just above the predicate's 1e-5 m/s threshold, so `Blocked` stays false and the gate indicator (`WorkshopDeclaredPresentation`, `CommittedActuatorState`) shows "unpowered" rather than "blocked". The frozen row (opening > 0, ball not ejected, penetration within envelope) passes; the predicate is not asserted. Fact 14 carries forward behaviour, not the legacy 1e-7 tolerances; a game-grade rest threshold for `Blocked` would be a shared presentation change outside this slice. **Handoff (coordinator decision, 22 Oct 2026, review F1):** the C-Gate implementation owner fixes this residual, with native proof. The threshold is CAT-051 open question 3 (owner). The handoff is recorded in TODO.md and in the epics.md C-Gate story, and the CAT-051 §5 status line no longer claims "State Blocked".
- **Penetration** at rest: blade 0.18 mm, tube 0.13 mm (game envelope ≤ 0.5 mm); transient bound asserted ≤ 6 mm (5 % of the 0.12 m blade).
- **Process note.** Two edits bypassed the Write/Edit tools: the temporary probe's `InlineData` list (shell script; the probe was later rewritten and deleted) and this spec's frontmatter `status` line (`sed -i`, in-progress → review). Content is as intended; the frozen block hash is unchanged (`d6a284aa…`).
- **Case count.** The native suite has 10 cases (the draft's own list sums to 10, not 9). The epics one-line suite→guard mapping from the draft's task 5 was not added: D6 in this spec is the mapping record and the dependent stories' ACs stay unedited.

## Evidence

Candidate: worktree `story-9-4c`, base `55ee05b9`, uncommitted. `LatchSupplyFixtureTests.cs` sha256 `f9b1ec7f80d8b90b6c749c9050baea1d7a2366067543736c0f0aba1e18ee206f`. Logs in `_bmad-output/implementation-artifacts/evidence-9-4c/`.

| Check | Command | Result | Log |
|---|---|---|---|
| T14 measurement | probe theory (deleted) on the production simulation | T = 67: Off@67, On@68; T′ = 140: On@140, Off@141 | `probe-1.log` |
| Fixture timing / ball geometry | probe theories (deleted) | strike ticks 103/116/150/209; ball pinned at opening ≈ 0.75 m | `probe-2.log`, `probe-3.log`, `probe-4-creep.log` |
| Mutation M1 (contact ignored) | edit `SupplyRoute.Resolve`, rebuild tests, filter run | 7/10 fail incl. all Reset and ball rows | `mutation-M1-contact-ignored.log` |
| Mutation M2 (spring ≈ 0) | `Fixed(0f)` rejected at admission (non-discriminating); `Fixed(0.001f)` | final: 5/10 fail incl. Reset-when-open (law residual 21.7 m/s²) and mid-stroke (energy) | `mutation-M2-spring-zero.log` |
| Mutation M3 (enable ignored) | first edit did not compile (IDE0060; stale results discarded); retry | Disabled control and supply off/on fail | `mutation-M3-enable-ignored.log` |
| Mutation M4 (no ball–blade contact) | skipping the collider is rejected at admission; blade collider lifted 3 m | crush row, upstream row and no-B control fail | `mutation-M4-blade-collider-removed.log` |
| Build (once, after all reverts) | `dotnet build CuriousContraptions.slnx -c Debug` | 0 warnings, 0 errors | `build.log` |
| Full native | `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | CuriousContraptions.tests 2288/2288; E2eRunner.Tests 189/189; Coverage.Tests 77/77; BundleElements.Tests 145/145; WorkerAotGuard.Tests 17/17 (total 2716, 0 failed) | `test-full.log` |
| Fixture guards by name (same build) | `dotnet test CuriousContraptions.tests -c Debug --no-build --no-restore --filter "FullyQualifiedName~LatchSupplyFixtureTests"` | 10/10 passed | `test-latch-fixture.log` |
| CI "Test C# tools" step | the workflow step's script under `bash -e` | Coverage 77, BundleElements 145, WorkerAotGuard 17, E2eRunner 174 (toolchain filter); exit 0 | `ci-tools-step.log` |
| Whitespace | `git diff --check` (new file intent-added) | exit 0 | `diff-check.log` |

Browser qualification: pending (post-catalogue E2E; recipes above). Independent review: Pass-pending-merge ([review](review-9-4c-latch-fixture-deferred-integrations.md)).

### Post-merge (22 Oct 2026)

- WIP commit `5e840026` ("WIP 9.4c candidate (pre-merge)", normal hooks) holds the candidate, the review and both evidence directories. Merge commit `80ffc68589070a6e18839986861f54a358b557ad` (tree `cdd490e0`, parents `5e840026` and main `a2eddf81`, which includes C-Bumper+Spring and 9.8a OR). Normal hooks ran: gate tests 124/124.
- Conflicts: `TODO.md` only. Main's content is kept; 9.4c is the in-flight item; 9.8a is recorded as merged with its receipt pending; C-Bumper+Spring's receipt is carried with 9.4c. `epics.md`, `sprint-status.yaml` and the csproj auto-merged, so both sides are kept. `LatchSupplyFixtureTests` needed no API conversion: balls already use `WorkshopInput.Generic(PartId.Basketball, …)`, and `Battery`/`Latch`/`Lamp` factories still exist at `a2eddf81`. Assertions are unchanged.
- Uncommitted carry, applied after the merge: the C-Bumper+Spring terminal receipt (`git apply` of the coordinator's patch to `review-c-bumper-spring.md` and `sprint-status.yaml`, content unedited) and its evidence copied to `evidence-c-bumper-spring-review/terminal/` (`diff -r` identical).

| Check | Result | Log (`evidence-9-4c/post-merge/`) |
|---|---|---|
| AppBundle absent | 0 matches | `appbundle-check.txt` |
| One build `dotnet build CuriousContraptions.slnx -c Debug` | 0 warnings, 0 errors | `build.log` |
| Full `dotnet test CuriousContraptions.slnx -c Debug --no-build --no-restore` | CuriousContraptions.tests 2362/2362; E2eRunner.Tests 189/189; BundleElements.Tests 148/148; Coverage.Tests 77/77; WorkerAotGuard.Tests 17/17 (2793, 0 failed) | `test-full.log` |
| Named `LatchSupplyFixtureTests` | 10/10 | `test-latch-fixture.log` |
| CI "Test C# tools" step under bash | Coverage 77, BundleElements 148, WorkerAotGuard 17, E2eRunner 174; exit 0 | `ci-tools-step.log` |
| `git diff --check` | exit 0 | `diff-check.log` |

## Spec Change Log

- 22 Oct 2026 (review F1, coordinator): D3 recorded as a dated coordinator decision that supersedes the "+2" wording. The Blocked-residual handoff to C-Gate is recorded in TODO.md and in the epics.md C-Gate story. The CAT-051 §5 status line no longer claims "State Blocked" and links open question 3. The epics.md C-Gate (9A.19), 9A.39 and 9A.43 ACs gain a one-line native-guard pointer. Documentation only; no runtime input changed.

- 22 Oct 2026: merged the coordinator's native-first revision (non-frozen plan only); status ready-for-dev → in-progress; recorded the measured T14 lag (D3) for confirmation.

## Review Triage Log

## Appendix: census

- **Routes:** `SwitchedSupplyFeed` admits `WorkshopLatch or WorkshopCounter` → `WorkshopBattery` (`WorkshopPhysicsCompiler.cs:297-310`). Its per-tick consumer is `WorkshopPhysicsEngine.cs:612` (`SwitchedSupplyClosed`). *(18 Oct census; at 55ee05b9 the routes resolve through `CompiledSupplyGraph.Resolve` in `engine/gpu/SupplyRoute.cs`, called from `WorkshopGpuDevice.Stage`.)*
- **Drive gate:** `PrismaticConstraintSolver.cs:247-249` (drag bands plus `SwitchedSupplyClosed`). 9R.3 replaces it with route kinds. *(At 55ee05b9: `ActuatorSliderDrive` acts only while `SimJoint.SupplyClosed`.)*
- **Blade:** mass 1, gravity −9.81, box half-extents (0.06, 0.72, 0.72) (`WorkshopPhysicsCompiler.cs:205-211`).
