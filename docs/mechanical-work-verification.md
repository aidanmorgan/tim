# Mechanical work allowances — wound-spring prerequisite

28 September 2026; implementation based on `1f537d4`.
This forward-refactors the existing mechanical network. It does not complete the
wound-spring launcher, a dynamic torque/inertia solver, or the full component goal.

## Contract

Motor, conveyor and windmill parameter selectors are enums. `ReadParameter` converts them through validated `PartParameterName` only at the open resource dictionary boundary; the old string-constant selector classes are removed and callers/tests updated. This is not a completed repository-wide string/fallback audit.

Every `MechanicalSource` explicitly provides signed speed and nonnegative torque.
There is no old speed-only constructor or inferred torque fallback. The electric
motor's authored torque defaults to 20 (valid range greater than zero through 100).
Only active electrical supply provides that torque; its existing smooth spin-down
remains visible but supplies no work. The windmill's torque bound is absolute
sampled axial force times a 0.4-unit lever arm, only while wind and shaft agree in
direction above cut-in. Zero-wind and opposed-wind coast supply no work.

Consumers declare typed `MechanicalLoads` inputs. A reverse traversal identifies
live downstream consumers. At each socket, torque is shared equally between the
local consumer and each enabled branch with a downstream consumer. Open clutches
and dangling outputs do not take a share. This is equal *branch* allocation, not
equal allocation to every leaf in an arbitrarily shaped tree. Unused allocation
expires each substep; there is no redistribution by part iteration order.

A speed ratio transforms torque inversely to its absolute value, preserving the
work bound across reversal/gearing. Float speed rounding cannot increase the
transmitted power allowance. Each consumer gets at most absolute speed times
allocated torque times substep duration. Consumers debit actual accepted work;
overdrafts, negative/non-finite debits, invalid sources and overflow are explicit
errors. Graph solving computes and validates before committing any new drive
state, so failures do not refill an already consumed allowance.

Conveyors now bound traction by both authored force and available shaft torque.
All contacts in a substep share one traction-impulse allowance and one work
allowance, preventing repeated collision callbacks from multiplying drive force.
A zero-impulse contact cannot report a new Transported event, including cosmetic coast-down. Braking dissipates energy; crossing through zero cannot reuse dissipated kinetic
energy to pay for reverse acceleration. Shaft/tread artwork follows the same
speed as before, independently of whether a cargo is touching the surface.

The spring store now requires an explicit work allowance as well as travel,
torque and clearance. Winding cannot accept more than that allowance. Callers
must debit the returned accepted work from their mechanical input; a native
integration case exercises this pairing. Its physical plunger is still absent.

## Limits

This is an ideal regulated-speed network with bounded work, not a dynamic motor
stall curve, flywheel, elastic/slipping belt, battery depletion or aerodynamic
efficiency simulation. Fan airflow remains an ideal source. Work budgets do not
prove conservation of the entire game or old powered devices outside this
network. Static allocation can leave unused capacity while another branch is
busy. Do not claim energy from cosmetic coast-down or silently supply unlimited
torque to a new consumer.

## Automated evidence

`dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`: **1,083 passed**, including the new work, traction and diagnostic cases.

`node --test tools/Playtest/direct-ui.test.cjs`: **63 passed** (supplemental adapter checks).

Diagnostic and production Release exports passed. Production uses `PlaytestDiagnostics=false`.
New checks cover torque/work sharing in parallel and serial chains, connection
ordering, open clutches/dangling outputs, coasting supply loss, reversers, work
overdraft/reset, repeated contacts, authored parameter rejection, positive and
negative gear ratios, atomic invalid-source/overflow failure, finite traction
and dissipative reversal. Existing campaign reference assertions remain unchanged.
These are supplemental tests, not replacements for browser evidence.

## Real-UI Playwright regressions

Recipes: [mechanical-work-recipes.json](mechanical-work-recipes.json).
Actual palette, movement handles, connection controls, Run and Reset are used.
No imported solutions, game-state setters, numeric placement or storage changes.
Read-only construction/body diagnostics verify actual placement and typed links.

| Case | Observation |
| --- | --- |
| `mechanical-work-clutch-powered-v1` | Separately powered motor/clutch drive conveyor right; original cargo leaves its right end. By tick 960 it has fallen beyond the workbench at X=12.739563. |
| `mechanical-work-clutch-unpowered-v1` | Only clutch supply omitted; cargo remains at X=1, Y=3.4600067 with zero velocity through tick 960. |
| `mechanical-work-clutch-reversed-v1` | Reverse transmission between motor/clutch sends original cargo left; by tick 960 it has fallen beyond the workbench at X=-12.283936. |
| `mechanical-work-windmill-v1` | Fan drives windmill, mechanical belt and conveyor; screenshot at about 1.71 seconds shows original cargo leaving the right end. By tick 948 it is beyond the workbench at X=12.209793. |
| `mechanical-work-windmill-blocked-v1` | A rotated wall occludes the fan. Cargo stays at X=1, Y=3.4600067 with zero velocity through tick 948. |

All five complete attempts above have no browser errors and exact full Run/Reset
construction equality. The actual motor properties include its new torque key.
The blocked/unblocked windmill constructions use conveyor (2,3,0) and payload
(1,approximately 4.499,0); the wall is approximately (-2.5,5.9964,0), rotated 90
degrees about Y. Each attempt retains its actual coordinates.

Logs are in ignored `docs/playtest-results/<caseId>.json`; matching motion,
holding/outcome/reset captures are in `.playwright-mcp/`. These are workshop
behaviour checks, not campaign wins. Sampled motion is not a mobile or sustained
render-performance guarantee. The palette, geometry, icons and UI are unchanged.

### Timed motor power-loss proof

`mechanical-work-motor-coast-v1` uses a pre-wired hold timer to supply the motor
while the clutch has continuous battery supply. A dropped striker activates the
timer through a switch; no state-setting automation is used. All six typed links
are verified. The original conveyor cargo is transported during the supplied
interval. Exact full construction Reset and zero browser errors.

Read-only typed shaft diagnostics show the conveyor input at 6 rad/s, torque 20
and approximately 0.250000013 work allowance at ticks 312 and 324. At ticks
336/348/360 the motor, both clutch sockets and conveyor input still turn at
4.500004/2.7000084/0.9000094 rad/s respectively, but torque and available work
are zero throughout. By tick 960 all shafts have stopped. Thus the engaged
clutch passes cosmetic coast motion without inventing drive work.

This sixth attempt uses the final diagnostic schema. The first five predate the diagnostic addition and stricter invalid-conveyor-parameter validation. All six precede a final zero-impulse Transported-event guard, covered by the native coast-contact test, and a typed parameter-key refactor with explicit resource-name tests. These changes do not alter the measured trajectories or torque/work calculations; the guard prevents reporting transport for unmoved cargo.
The diagnostic emitter is read-only and compiled out of production.

### Final-source repeat

`mechanical-work-motor-coast-v2` repeats the complete eight-part, six-link timed
construction after the event guard and cached enum-parameter conversion.
It again observes the same powered-to-coasting torque/work transition at
ticks 324/336/348/360, with exact Run/Reset and no browser errors.
The final source is therefore exercised in the exported browser, not only
compiled. Eight simulated seconds elapsed in 7.975 wall seconds between the
read-only frame observations (about 1.003x real time for this construction).
This is not a render-FPS, mobile or large-machine performance guarantee.

## Retained failures

- The first open-clutch allocation test attempted to add an electrical wire
  during Run. `World.Connect` correctly refused. The corrected test wires a
  switch before Run, then operates its contact. No build/run restriction changed.
  The initial broad run had 1 failure and 1,068 passes.
- A new native graph probe tried calling internal `ClearMechanicalDrive` from the
  test assembly, causing CS0103. The probe now has an empty geometry build; the
  public network solve initializes drive state. Visibility was not broadened.

Anvil graph access worked but had no test mapping. Its write gate was unavailable
because credentials were missing; edits proceeded with its allow-with-warning
policy, not a successful validation scan.
