---
title: 'Story 9.4c: Latch-Fixture Deferred Integrations (CAT-005 N19, CAT-051 Supply Interruption Mid-Stroke)'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
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

## Code Map

- `tools/e2e/cat-037.test.ts:1-60`. Fixture to start from: placement points, socket constants, the `CHOICE_SET_BAR`/`CHOICE_RESET_RING` choices and `BATTERY_RUN_TOGGLE`.
- `tools/e2e/workshop-driver.ts`. Add helpers only if a recipe needs them: `selectTool`, `liftSelectedPart`, `run`, `reset`, `save`/`load` and `readLatestPose`. No setter helpers.
- `tools/e2e/cat-051.test.ts:1-40`. The tilted gate and an upstream rolling ball.
- `CuriousContraptions.tests/WorkshopPhysicsEngineTests.cs` ([R:9.4b] new) and `StateLatchTests.cs`. Native, tick-exact companions.
- New: `tools/e2e/cat-005-n19.test.ts` and `tools/e2e/cat-051-interrupt.test.ts`.
- New: `CuriousContraptions.tests/SupplyInterruptionTests.cs`, with its `Compile Include`.
- If a defect surfaces: `PrismaticConstraintSolver.cs`, `SwitchedSupply.cs`, `WorkshopPhysicsCompiler.cs`.

## Tasks & Acceptance

**Execution.** Depends on 9.4b's cumulative baseline and all of Epic 9R.
- [R:9R.3] refresh the route code: it adds a Direct route kind to the same table and deletes the drag bands.
- [R:9R.1] use its pre-Run snapshot helper for Reset.

1. [ ] **Native first.** `CuriousContraptions.tests/SupplyInterruptionTests.cs` steps the shipped engine and simulation through the Battery → Latch → Gate and Switch A/B → Set/Reset construction. It asserts every row of the matrix tick-exactly, with ticks from `WorkshopRead.Tick`. If a row fails because of an engine defect, it stays red until the fix.
2. [ ] `tools/e2e/cat-005-n19.test.ts`, with these tests:
   - (1) UI build, plus a check that Save shows exactly five connections with socket names;
   - (2) Set: tick T to T+2 from `CCGPU_RETAINED` reads, and the construction revision is unchanged;
   - (3) Reset when open, with N17 clearing;
   - (4) Battery authored Disabled;
   - (5) memory survives supply off/on;
   - (6) Run/Reset restores latch Off, the blade closed and the battery's authored state, equal to the pre-Run snapshot;
   - (7) Save/Load replay.
3. [ ] `tools/e2e/cat-051-interrupt.test.ts`, with these tests:
   - (1) a mid-stroke cut. A guard first asserts that the sampled opening is strictly inside (0.05, 1.50). If the timing misses, the test fails as invalid; it never skips.
   - (2) a ball upstream stays upstream;
   - (3) a ball partly through stops the blade without crushing;
   - (4) the no-Switch-B control reaches Open.
   
   Time Switch B's strike with Move-lift height. Do not use a timer setter.
4. [ ] Fix any defect at its source, with a mutation proof.
5. [ ] Update `tools/e2e/README.md` (suite list) and `docs/planning/elements/CAT-005-battery.md` §5 and `CAT-051-powered_gate.md` §5 (integration status).

**Acceptance Criteria (1:1 with epics 9.4c):**
- **AC1.** Given this machine, built through real UI controls with no setters or numeric placement:
  - Battery `Supply` → State Latch `PowerIn`;
  - Latch `Supply` → Powered Gate `PowerIn`;
  - Impact Switch A → Latch `SetIn`;
  - Impact Switch B → Latch `ResetIn`;
  - a ball upstream of the gate;
  - plus D1's lamp link, a delegated addition.
  
  When a ball strikes Switch A, then the latch sets and the gate becomes supplied and opens. The supply settles two ticks after the activation, and the committed construction shows no topology change (N19).
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
- **AC7.** `cat-005-n19` and `cat-051-interrupt` pass 100% in Chrome serially, and every `tools/e2e/*.test.ts` suite passes in one serial run.

## Design Notes

**Decisions (owner playability delegation, 18 Oct 2026):**
- **D1. The fifth connection is Latch `ActivationOut` → Signal lamp, as in `cat-037`.**
  - The AC says "all five connections" but lists four: Battery `Supply` → Latch `PowerIn`, Latch `Supply` → Gate `PowerIn`, Switch A → `SetIn`, Switch B → `ResetIn`.
  - Rationale: the player sees the latch memory, and N17 is visible when the lamp clears.
  - **Owner may confirm.**
- **D2. "Stops gaining opening within two ticks" means the drive's opening work ends by T+2.** Opening speed then decreases strictly each tick, and mechanical energy never increases (CAT-051 facts 15 and 19).
  - With blade mass 1 kg, up to 2.8 m/s and spring 14 N/m, the blade physically coasts about 0.1 m (about 10 ticks) before it reverses.
  - A literal "position stops rising within two ticks" would need a brake, which the declared fail-closed law excludes.
  - Rationale: the gate falls shut predictably, which is the source behaviour.
  - **Owner may confirm.**
- **D3. Tick exactness:**
  - the native companion proves each clause tick-exactly;
  - Chrome proves the same clauses through UI-built machines, using tick-stamped `CCGPU_RETAINED` reads, and the pose ring for blade samples.

**Negative and mutation plan:**
- Make `Compose` ignore `contactClosed`: the Reset rows fail.
- Drop the always-on spring: the return rows fail.
- Remove the ball contact with the blade: the crush row fails.
- Make the Battery disable ignored: AC4 fails.

**Impacted suites:** cat-037, cat-051, cat-005, cat-020 (counter route), cat-052, `StateLatchTests`, `PoweredGateTests`, `ElectricalSupplyTests`, and the full cumulative run.

**Risks:**
- Mid-stroke timing in Chrome depends on fall height and frame pacing. The guard turns a miss into an explicit failure; use the same lift recipe every run.
- `CCGPU_RETAINED` may not be emitted for every needed tick. If so, record it, keep the native proof as the tick authority, and assert ordering in Chrome. Escalate rather than add a channel.
- 9.4b AC4/AC10 (the cat-023b cumulative baseline) were unmet at spec time. This story's AC7 inherits that blocker.

## Verification

- `node --test --test-concurrency=1 tools/e2e/cat-005-n19.test.ts tools/e2e/cat-051-interrupt.test.ts` against `:8060`: 100%.
- `dotnet test CuriousContraptions.slnx --filter SupplyInterruptionTests`: pass.
- A cumulative serial run of `tools/e2e/*.test.ts`: 100%.

## Legacy Deleted

None (integration proof). If a defect fix supersedes code, delete it in the same slice.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Appendix: census

- **Routes:** `SwitchedSupplyFeed` admits `WorkshopLatch or WorkshopCounter` → `WorkshopBattery` (`WorkshopPhysicsCompiler.cs:297-310`). Its per-tick consumer is `WorkshopPhysicsEngine.cs:612` (`SwitchedSupplyClosed`).
- **Drive gate:** `PrismaticConstraintSolver.cs:247-249` (drag bands plus `SwitchedSupplyClosed`). 9R.3 replaces it with route kinds.
- **Blade:** mass 1, gravity −9.81, box half-extents (0.06, 0.72, 0.72) (`WorkshopPhysicsCompiler.cs:205-211`).
