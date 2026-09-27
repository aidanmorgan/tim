# Latched wound-spring launcher — verification

28 September 2026. **Incomplete, uncommitted component prototype.** The finite spring primitive and mechanical work allocation are committed. The current worktree adds a catalogue/scene/icon, belt-driven winding and a finite-mass guided plunger. Initial powered/unpowered, reverse-drive, empty-release and retained-charge real-UI Playwright attempts now pass; the remaining acceptance cases are unverified. This is not a completed or published element.
Do not confuse this with the existing always-ready springboard or passive trampoline.

## Model and limits

`LatchedSpringStore` represents a linear spring compressed by an ideal ratcheted
screw. Stored energy is one half stiffness times compression squared; force is
stiffness times compression. These relationships follow
[OpenStax's spring-work derivation](https://openstax.org/books/university-physics-volume-1/pages/7-1-work).
The screw lead is compression per shaft radian, so ideal required input torque is
spring force times lead. The screw/ratchet, latch rules and future part geometry
are our design, not verified historical TIM behaviour.

The caller must supply actual positive shaft travel, available torque and
collision-cleared compression travel. Winding is capped by stroke, clearance
and available torque. Only the accepted travel adds work; excess offered travel
does not charge the spring. Reverse rotation freewheels. Zero drive preserves
charge. A reduced torque limit never unwinds an already latched spring.

A trigger releases the latch but contributes no energy and does not jump the
plunger or a payload. Empty triggers are not queued. Once released, winding is
disconnected until the physical stroke ends and the caller rearms the latch.
Only actual permitted extension debits spring potential energy. Returned work
must become plunger/load motion or explicitly accounted dissipation in the
future physical part. A blocked extension consumes nothing. Repeated triggers
cannot release twice, nor can a partially compressed release be relatched.
Reset clears compression, accounting and latch state.

States/results are enums. Invalid/non-finite inputs are rejected before state
changes. Double internal calculations keep products of finite float inputs
representable. Sub-precision movement adds/releases no work.

This is **not** a collision solver, dynamic screw/gear model, motor inertia
simulation or shared torque/power allocator. The mechanical network now supplies explicit torque and per-substep work
allowances shared across live branches; see [mechanical work verification](mechanical-work-verification.md).
Winding requires the allocated work explicitly and cannot exceed it. The physical
part must debit actual accepted work from its typed mechanical input. Do not
infer energy from nonzero speed or add a torque fallback. Shaft inertia, dynamic
load feedback and motor stall curves remain separate unfinished work.

## Native verification

Initial groundwork: 26 focused cases cover:

- Rising work/force with compression, stroke limits and collision-clearance limits.
- Zero/insufficient torque, capped winding and accepted work versus input torque/travel.
- Stopped and reverse drive retaining latched charge.
- Partial/full release, blocked release, winding interlock and explicit rearming.
- Empty/no-queued triggers, repeated releases and 1,000 partial charge/release cycles.
- Step partitioning, sub-precision movement, Reset after an interrupted release.
- Invalid configuration/input atomicity and 27 combinations of extreme finite float scales.

`dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`: **1,040 passed**, including all 26 focused spring cases (40-second test run).

`dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet`: **passed**.

These native checks are prerequisites, **not** the mandatory Playwright evidence
for a completed puzzle element. No gameplay caller has changed in this groundwork.

## Current worktree native evidence

`dotnet test CuriousContraptions.tests --no-restore --verbosity quiet --logger 'console;verbosity=normal' --filter FullyQualifiedName~WoundSpringTests`: **11 passed** on 28 September 2026. These tests exercise the actual part/world integration:

- Powered launch and an unpowered control, with exact construction Reset.
- Retained charge after supply loss for payload masses 0.5, 1 and 4; no additional accepted work, finite stroke and an energy upper bound with a 2% numerical allowance (not exact energy conservation).
- Empty release moves the existing internal plunger and spends its charge.
- A late native-test obstruction stops the plunger and preserves remaining charge. This test inserts an obstacle during simulation; it is not a player-UI construction proof.
- Three partial-charge/release cycles at each of three orientations: horizontal, compound rotation and upside down. These empty-stroke tests disable gravity to isolate the guide, verify axial confinement and exact Reset, and do not prove rotated loaded shots under gravity.
- Removing the assembly removes its internal physics body.

The full current native suite, including five diagnostic serialization cases and four direct ratchet-contact regressions, also passes: **1,107 tests**, 43-second reported test duration. Both the diagnostic web publish and the current prototype's production Release export (`dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet`) pass, and `git diff --check` is clean. The production export overwrites the local AppBundle; rebuild with diagnostics before continuing frame-based browser verification. These are worktree results, not a published revision.

Shared collision changes, visual polish, all supported interactions and the remaining browser proof remain unfinished. The following checklist is acceptance work, not a claim that the prototype has passed it.

## Initial real-UI browser evidence

Tested the uncommitted prototype on top of `0ede1e1bd72d7b5d929a4c7e26dee5e5a0c618c0`, using a fresh diagnostic web export, localhost port 8060, a 1440×900 viewport, Free workshop (row 59), Balanced difficulty. [Recipes](wound-spring-recipes.json) record the exact intended construction. The actual-UI adapter used palette clicks, placement/movement handles and connection controls, then Run/Reset; no game-state setters or imported construction were used.

- `wound-spring-powered-v1`: seven authored parts, four verified typed connections (battery → motor → spring; falling ball → switch → delay → spring). The payload settled near Y=3.760, then rose to a sampled peak Y=7.167; sampled upward speed reached 7.164. The motor supplied 6 rad/s and 20 torque at full speed. The plunger physically extended and rewound while supply remained connected.
- `wound-spring-unpowered-v1`: identical parts and trigger route, omitting only battery → motor. All three expected connections were present. The motor/drive had zero speed, torque and work; after tick 228 the payload stayed at Y=4.560 with zero velocity and the plunger remained at Y=3.900. No launch occurred.
- Both attempts recorded 101 diagnostic frames, no browser console errors, and exact equality of the Run construction and Reset snapshot.
- Local raw logs: `docs/playtest-results/wound-spring-{powered,unpowered}-v1.json`. Each log includes actual action coordinates, construction, connections, frames and screenshot paths. Motion/holding/outcome/settled screenshots remain under ignored `.playwright-mcp/`. The powered motion capture was visually inspected: continuous helix and rounded plunger are visible within the transparent guide, with the existing palette retained.

Two further attempts used the same prototype and diagnostic export:

- `wound-spring-reverse-v1`: added a reversing gearbox, with five verified typed connections. The motor and gearbox input ran at +6 rad/s; gearbox output and spring input ran at −6 rad/s, with 20 torque available. The spring head remained at Y=3.900 and the payload stayed at Y=4.560 after settling; the trigger did not cause a launch. This proves the reverse-drive freewheel control, not retention of previously wound charge under reverse drive.
- `wound-spring-empty-v1`: omitted the payload, retaining all four drive/trigger connections. The same internal plunger moved from Y=3.100 at tick 228 to Y=3.241 with upward velocity 7.730 at tick 240, then returned to winding by tick 252. Every frame contained exactly the striker ball and existing plunger; no ammunition/body was created. This proves an empty mechanical stroke, not charge accounting (the current browser frames do not report spring energy).
- Both additional attempts recorded 101 frames, exact construction Reset and no browser console errors. Their complete logs are retained locally as `docs/playtest-results/wound-spring-{reverse,empty}-v1.json`, with screenshot paths and actual actions; both recipes are included in the linked recipe file.

### Retained-charge launch and failed recipe

Read-only C# diagnostics now emit typed spring phase and nullable trigger result, compression, stored energy, accepted/released work and release count. These fields add observation only; no state-changing hooks are exposed. Five serialization cases preserve all fields (including a null trigger) and reject numeric/unknown enum inputs and undefined enum outputs. The focused launcher/diagnostic run passes **16 tests**.

- `wound-spring-retained-charge-v1` **failed its intended acceptance condition**, despite an error-free Run/Reset. The bouncing striker retriggered the two-second hold timer. Drive work disappeared briefly at tick 360 but returned by tick 372, before release at tick 480. Total accepted work later increased to 75.0711; this is not a stored-charge-only launch. Preserve this recipe and its raw evidence, not as a passing case.
- `wound-spring-retained-charge-v2` routes the timer through a one-shot delay and adds another delay before release. Eleven UI-placed parts and nine typed connections were verified. At ticks 516–588 the motor was stopped, torque/work were zero, and spring energy stayed at 51.2000015 with no release. Release began by tick 600; the payload reached a sampled Y=7.16687. Accepted work stayed at 51.2000015 throughout release and afterward; released work reached the same value and stored energy reached zero by tick 624. Release count remained one. This demonstrates a launch after power loss, not a launch powered by shaft coasting.
- Both attempts recorded 102 frames, exact construction Reset and no browser console errors. Logs remain locally at `docs/playtest-results/wound-spring-retained-charge-v{1,2}.json`; recipes include both versions. The successful version's captured spring and mechanical values were explicitly checked together.

### Partial-charge failure, collision correction and rerun

- `wound-spring-partial-v1` removes the delay and connects the impact switch directly to the spring. It **failed**: repeated small releases produced `Flight contacts did not converge`, severe slowdown and 6,962 captured console-error entries (including secondary runtime errors while printing the original exception). Reset eventually restored the construction exactly, which does not make this a passing test. The complete failed log and recipe are retained.
- Native regression `ImpactSwitchTriggersPartialChargeThroughTheActivationNetwork` reproduced the failure. Additional exception context identified a stationary ratcheted plunger touching a payload with Y velocity −1E−45. The solver allocated inward motion to the plunger, then its one-way constraint removed that motion, leaving repeated zero-time contact.
- The correction makes the resting ratchet's inward inverse-mass response zero and queries the second body's response using its actual, opposite impulse direction. It does not discard remaining simulation time, suppress exceptions, increase the iteration limit or add a substitute physics path.
- `wound-spring-partial-v2` repeats the same UI recipe after the correction: six parts, three verified typed connections, 101 frames, zero console errors, exact Reset. First release had accepted 18.480027 energy units; it released the same amount before the second trigger. Its first-flight sampled peak was Y=5.282562 (the earlier fully charged recipe reached Y=7.16687). The impact ball caused five releases while the motor stayed powered. At the final frame a very weak release was stalled under its payload, retaining 0.128634 energy; this is not proof of emptying every partial charge or repeated triggering without recharge.
- Local raw logs are `docs/playtest-results/wound-spring-partial-v{1,2}.json`; both recipes are retained. The failed screenshot showing stalled simulation was inspected. All **17 focused launcher/diagnostic cases** and the full **1,100-test native suite** pass after the correction.

### Direct ratchet-contact regression

Four additional native cases use the actual released plunger, explicitly brought to rest, and an incoming payload. They cover both argument orders and speeds of 4 and `float.Epsilon`. Each resolves contact once, asserts zero velocity for both bodies, asserts no backward plunger displacement, and confirms that the next continuous sphere query reports clear rather than another zero-time collision. All four pass. The full suite now passes **1,104 tests** (43 seconds); the 63 supplemental UI-adapter tests also pass. Adapter tests are not real-browser behavioral proof.

The first test setup failed its release-state precondition in all four cases: one step had only queued the deferred activation. The corrected setup advances the following fixed tick before testing contact. No production behavior was changed to accommodate the test.

### Browser replays after the ratchet-contact correction

- `wound-spring-retained-repeat-v1`: the retained-charge construction plus one extra one-shot delay, all placed and linked through the UI. Twelve authored parts and eleven typed connections were verified. At ticks 516–588 the stopped motor supplied zero torque/work while the spring held 51.2000015 energy. It then launched the payload to sampled Y=7.16687, exhausting that energy without accepting more work. A second delayed trigger reported `empty` by tick 720; release count stayed one and stored energy stayed zero through tick 984. There were 103 frames, zero console errors and exact Run/Reset construction equality.
- `wound-spring-reverse-v2`: repeats the gearbox control after the correction with eight parts and five typed connections. The spring received −6 rad/s and 20 torque, but accepted/released work, stored energy and release count stayed zero throughout all 101 frames. The trigger reported `empty`, the payload remained at rest after settling, Reset was exact and there were no console errors.
- Complete local logs are `docs/playtest-results/wound-spring-retained-repeat-v1.json` and `docs/playtest-results/wound-spring-reverse-v2.json`; both recipes and screenshot paths are retained.

### Winding obstruction and clear control

- Native `SolidPanelLimitsWindingTravelAndAcceptedWork` places a horizontal panel before Run and verifies the head stops at its upper face plus plunger radius: Y=3.445, compression 0.455, stored/accepted energy approximately 16.562. A further 240 ticks with the motor powered add no work.
- `wound-spring-winding-obstacle-v1` builds the same arrangement through UI controls: four parts, two verified typed connections. The wall is rotated 90° about X, then moved from its staging position (3,3,3) to approximately (0.000525,3,0.002124) using gizmo drags after wiring. The head stops at Y=3.445; phase is `blocked`. All frames from tick 240 onward retain exactly 16.5620012 stored/accepted energy, zero released work and zero releases despite the 6 rad/s powered shaft.
- `wound-spring-winding-clear-v1` leaves the same panel at (3,3,3). The head reaches Y=3.100, compression 0.8 and `armed` state. Stored/accepted energy plateaus at 51.2000015, with no excess work accepted after full winding.
- Both browser attempts have exact Reset and no console errors (100 and 101 frames respectively). These tests use no payload or activation input and prove winding obstruction/charge caps, not blocked release. The obstacle deliberately intersects the static assembly frame; this is a collision-travel test, not proof of authoring overlap prevention.
- Both recipes are retained. Local logs `docs/playtest-results/wound-spring-winding-{obstacle,clear}-v1.json` include the complete adapter source: the final-movement block runs after wiring, before Run, exclusively using UI drags. The obstructed holding screenshot was visually inspected.

### Timed blocked release and open-shutter control

- Native `TimedGateClosesAfterWindingAndBlocksTheReleasedPlunger` authors the spring, shutter and hold timer before Run. The timer opens the shutter, allowing full winding, then lets it close before release. The plunger stops against the blade and retains its unused spring energy for a further 240 ticks. No late object insertion is used.
- `wound-spring-blocked-release-v1` constructs the sequence through the UI with eleven parts and ten verified typed links. A one-shot delay powers the hold timer; further delays release the spring after the timer expires and the shutter closes. The gate is rotated 90° about Z and positioned via gizmo drags before Run at approximately (0.008885,3.596894,0.002953). The spring first reaches 51.2000015 energy. Release stops the head at Y=3.216893, retaining 37.3307573 energy and recording 13.8692443 released work. From tick 624 through 984, stored energy and accepted work remain unchanged with release count one, despite the still-powered motor.
- `wound-spring-open-release-v1` differs only in the gate's supply source: battery rather than timed contact. The shutter stays open, the plunger completes its empty stroke and releases all 51.2000015 energy. It then accepts a second 51.2000015 charge from the powered motor (total accepted 102.4000031); release count remains one.
- Both runs retain exactly two internal/dynamic bodies (the striker and existing plunger), have exact construction Reset and no console errors; frame counts are 103 and 102. Neither test contains a payload in the spring. The blocked outcome screenshot was visually inspected; motion captures and full adapter source are included/referenced in the local logs `docs/playtest-results/wound-spring-{blocked,open}-release-v1.json`.
- This establishes blocked release versus a clear stroke using real timed puzzle components, and refreshes the empty-release check after the contact correction. Resumption after reopening is verified separately below.

### Resuming the same release after obstruction clears

- The native timed-gate regression now also stops the motor, reopens the shutter and verifies completion of the existing release with no additional accepted work, no second release, zero remaining charge and a stationary plunger.
- `wound-spring-resumed-release-v1` uses thirteen UI-authored parts and fourteen verified typed connections. Separate hold timers control the motor and shutter. A later delay reopens only the shutter; it cannot restart the motor. No placement, numeric parameter or game-state edits occur during Run.
- After full winding and motor shutdown, the first trigger releases the spring against the closed blade. Sampled ticks 600–756 report `blocked`, with 37.3307573 energy remaining. When the shutter clears, the same release completes by sampled tick 780. Release count remains one; accepted work stays at 51.2000015 and motor work stays zero throughout release/resumption. Final stored energy is zero and released work equals accepted work.
- The attempt records 102 frames, exact construction Reset and no console errors. The recipe and complete local log (`docs/playtest-results/wound-spring-resumed-release-v1.json`, including adapter source and screenshot paths) are retained. This is an empty-plunger resumption test, not a loaded shot through a moving shutter.

The partial-charge rerun, reverse-drive rerun, winding-obstruction/control pair, timed blocked/open/resumed-release cases and retained-charge/repeated-empty-trigger construction are current browser evidence after the shared contact correction. Other earlier browser cases remain historical until replayed. Remaining gaps include rotated loaded launches, downstream integration, shared-flight browser regressions, mobile performance and fully fluid latch animation.

### Catalogue payload comparison

Two new actual-UI constructions use the same seven-part powered-launch layout and four typed links, replacing only the payload selected from the catalogue. No mass, radius or bounce values were edited through a menu or game-state hook.

| Attempt | Catalogue mass / radius / bounce | Settled Y before release | Sampled peak Y | Rise | Sampled peak upward speed |
| --- | --- | --- | --- | --- | --- |
| `wound-spring-tennis-v1` | 0.35 / 0.25 / 0.78 | 3.669998 | 9.656113 | 5.986115 | 10.08551 |
| `wound-spring-bowling-v1` | 4 / 0.38 / 0.14 | 3.800003 | 4.949326 | 1.149323 | 3.062454 |

Both release exactly the same 51.2000015 spring energy in one release, then rewind under continued motor power. Each retains 101 frames, verifies all four typed links and the actual catalogue properties, has zero console errors and restores the construction exactly after Reset. This comparison establishes different playable payload responses; because mass, radius and bounce all differ, it is not an isolated mass experiment. The earlier native equal-size mass sweep supplies that separate evidence.

Both recipes and local raw logs `docs/playtest-results/wound-spring-{tennis,bowling}-v1.json` are retained. Motion screenshots are referenced in the logs. These are post-contact-fix browser results. They do not establish off-centre loading, oversized payload rejection or rotated loading.

### Compression marks and latch presentation

`wound-spring-visual-v1` repeats the powered construction using real UI controls with a closer camera. Seven authored parts and four typed links were verified. All 102 frames are retained, with zero console errors and exact construction Reset. The identified payload reaches sampled Y=7.16687; released spring work reaches 51.2000015. Motion captures show the navy rail ticks, gold compression index and continuous silver helix. These sampled images do not establish continuous latch readability or mobile performance.

`LatchPresentationMovesContinuouslyWithoutChangingPhysics` verifies bounded latch movement in both directions, settling, exact Reset and unchanged plunger/payload motion and energy under render-only updates. The current focused launcher/diagnostic run passes **24 cases** (19 part/contact/presentation and five diagnostic cases). The production Release export also passed after this presentation change. The marker represents compression, not a linear energy gauge. The complete local log is `docs/playtest-results/wound-spring-visual-v1.json`, including adapter source, assertions and screenshot paths; its recipe is retained alongside earlier attempts.

### Shared-contact browser regression progress

The existing trampoline-to-basket construction and its missed-bed control have been replayed on this worktree through real UI actions. Each retained 100 frames, zero console errors and exact Reset; the positive ball remains in the basket and the offset control settles on the floor. See [current shared-contact replay evidence](flight-browser-regressions.md#wound-spring-shared-contact-replay--28-september-2026). This refreshes trampoline/basket coverage only; the other shared-flight replays and rotated loaded-launch proof remain incomplete.

### Rotated loaded shots exposed oblique-contact roundoff

Four new native cases keep normal gravity enabled, place a real payload in the rotated guide before Run, wind fully, remove supply, wait for motor coast-down and trigger from retained charge. Orientations are Z=−15°, Z=15°, X=15° and compound (15°,30°,−15°). They assert a loaded settled position, upward and aimed horizontal travel, axial plunger confinement/stroke limits, unchanged accepted work, one release, exhausted charge and exact construction Reset.

All four initially **failed** with zero-time body-contact nonconvergence during winding (ticks 19–20). Example: plunger (0,3.8460832,0.2267073), payload (0,4.4737387,0.43078774), payload velocity (0,−0.13901666,0.42754984). The double-precision contact query still detected a minute inward component after the float impulse response had resolved the normal motion. An initial bound based only on relative velocity fixed winding but all four still failed at release tick 361: subtracting nearly equal moving-body velocities also carries their individual float rounding.

The current query uses four float-relative-resolution units times the sum of absolute position-difference × individual-velocity terms, only for already-touching spheres. It distinguishes resolvable approach from near-tangent cancellation; it does not advance/discard time, add impulses, change restitution or raise the contact iteration cap. There is no fixed minimum speed. Six direct cases cover both body orders, three tangent velocity scales with genuine inward controls, and axial impacts down to `float.Epsilon`.

All four loaded orientations now pass; the combined launcher/diagnostic/moving-sphere run passes **54 cases**, and the full native suite passes **1,117 tests** (44 seconds). This remains native evidence, not real-UI angled-launch proof. **All earlier browser captures predate this query correction.** Re-export and replay affected browser cases before treating the prototype as verified; the immediately preceding trampoline pair is retained historical evidence, not proof of the updated query.

### Angled player-built launch and downstream receiver

Three real-UI Playwright attempts now exercise the diagnostic export **after** the oblique-contact correction, with `MovingSphereSweep.cs` SHA-256 `e4d7c72b73997c62986266ce1c8006494adfa316c3c7a2262a648d62fc561774`. Free workshop, Balanced, 1440×900; palette selection, 3D movement/rotation handles and contextual connections only. No game-state setters, imported constructions or numeric placement menus.

- `wound-spring-angled-v1`: seven authored parts/four verified typed links. The spring is rotated −15° about Z; actual payload placement is (0.4,4.5017104,0). It settles near (0.263275,3.712402,0), then launches rightward to sampled peak Y=6.844635. There is no receiver, and the ball eventually leaves the workbench. This is a successful launch observation, not a solved puzzle.
- `wound-spring-angled-receiver-v1`: adds a basket at actual (3.6000001,1.1882488,0), preserving the launch and four links. At tick 972 the original payload remains inside at (3.833435,1.1533356,0), velocity (−0.0036621094,0,0). The outcome screenshot visibly confirms containment. One release spends 51.2000015 energy; continued motor power then rewinds, so this is not a supply-loss test.
- `wound-spring-angled-unpowered-v1`: identical eight-part arrangement, omitting only battery → motor (three verified links). Drive speed/torque/work and all spring energy/work/count values remain zero. The trigger reports `empty`. At tick 972 the payload remains in the guide near (0.503525,4.47139,0), with small residual contact velocity; the basket is visibly empty. Do not describe this as exact static rest.

Each attempt retains 102 frames, zero console errors and exact Run/Reset construction equality. Complete logs, adapter source and screenshot paths are retained locally as `docs/playtest-results/<caseId>.json`; all three recipes are included in the recipe file. This proves one UI-authored rotated loaded launch and downstream basket integration with an unpowered control, not all orientations, difficulty settings or mobile performance. Remaining shared-flight and launcher-mode replays still require refresh after the query correction.

### Current-query partial, reverse and retained-charge replays

These three actual-UI Playwright replays use the same post-correction diagnostic export/source hash as the angled cases above. No production code changed between them. All use Free workshop, Balanced and actual palette/gizmo/connection controls; recipes, full local logs with adapter source and screenshot paths are retained under their case IDs.

- `wound-spring-partial-v3`: six parts, three typed links, 101 frames. First release spends 18.48002697 energy and reaches sampled Y=5.2825623. Continued power and switch impacts produce five releases; the final weak release remains blocked under its payload with 0.128633858 energy. No contact-loop errors occur.
- `wound-spring-reverse-v3`: eight parts, five typed links, 101 frames. Spring input reaches −6 rad/s with 20 torque available, but accepted/released work, stored energy and release count remain zero; the trigger reports empty. The payload settles at Y=4.5599976.
- `wound-spring-retained-repeat-v2`: twelve parts, eleven typed links, 103 frames. Seven sampled frames at ticks 516–588 show 51.2000015 stored energy while motor/spring speed, torque and available work are all zero. The subsequent launch reaches sampled Y=7.16687. Accepted work remains 51.2000015 from tick 600 onward; final released work equals it, stored energy is zero and release count remains one. The second trigger reports empty.

All three have zero console errors and exact Run/Reset construction equality. These refresh the three named modes only; winding obstruction, blocked/resumed release, payload comparisons and remaining shared-flight browser regressions still require post-correction replay.

### Current-query obstruction and resumption replays

Three further real-UI attempts use the unchanged post-correction export/source hash recorded above. The obstacle adapter performs its final gizmo movements after wiring and before Run; all parts and connections are constructed using visible controls.

- `wound-spring-winding-obstacle-v2`: four parts, two typed links, 100 frames. All 60 sampled frames from tick 240 onward retain exactly 16.5620012 stored/accepted energy, compression 0.455 and no releases. The still-powered motor cannot wind through the horizontal panel.
- `wound-spring-winding-clear-v2`: same parts/links, leaving the panel at its staging location. All 60 sampled frames from tick 240 onward retain 51.2000015 stored/accepted energy and full 0.8 compression. Charge cannot exceed the stroke cap. This also has 100 frames.
- `wound-spring-resumed-release-v2`: thirteen parts, fourteen typed links, 103 frames. Eleven sampled frames at ticks 624–744 retain 37.3307573 energy against the closed shutter. After reopening, the same release completes by sampled tick 780. From tick 600 onward available mechanical work stays zero and accepted work stays 51.2000015; final released work equals it, stored energy is zero and release count remains one. This is an empty-plunger resumption test, not a loaded shutter shot.

Each has zero console errors and exact Run/Reset construction equality. Full local logs include the adapter source and screenshot paths; recipes are retained. These refresh blocked winding, full-charge limiting, blocked release and empty-stroke resumption after the near-tangent correction. Payload comparisons, other shared-flight replays and remaining visual review are still incomplete.

### Current-query catalogue payload and pulley checks

`wound-spring-tennis-v2` and `wound-spring-bowling-v2` repeat the seven-part/four-link launch through actual UI controls against the post-correction export. The tennis payload (mass 0.35, radius 0.25, bounce 0.78) rises from Y=3.6699982 to sampled Y=9.656113. The bowling payload (mass 4, radius 0.38, bounce 0.14) rises from Y=3.800003 to sampled post-release Y=4.9493256; its initial placement at Y=4.999069 is not counted as a launch peak. Both release 51.2000015 energy in one shot and then rewind under continued power. They retain 101 and 102 frames respectively, zero console errors and exact Reset. This is a catalogue-payload comparison, not an isolated mass experiment.

The existing level 19 counterweight puzzle also passes its positive/no-final-rope UI pair under the corrected query; see [shared-flight evidence](flight-browser-regressions.md). All four logs include the exact adapter source and screenshot paths; both recipe files retain the new attempts. Remaining shared-flight replays and visual review are still required before publishing the launcher.

### Additional current-query shared-flight checks

Springboard (level 4), straight pipe (25) and bend pipe (27) now pass their actual-UI positive/negative pairs under the corrected query: intended wins at ticks 265, 307 and 251, and wrong-aim/missed-inlet controls with no goal event through tick 948. All six runs have zero console errors and exact Reset. See [shared-flight evidence](flight-browser-regressions.md). Cannon, conveyor, pusher and trampoline replays and remaining visual review are still pending.

### Current-query trampoline and pusher replays

The trampoline/basket positive and missed-bed control now pass after the oblique-contact correction, as do the powered-pusher endpoint-output and no-supply control. All four use actual UI construction, have zero errors and exact Reset, and are recorded in [shared-flight evidence](flight-browser-regressions.md). Cannon/conveyor replay and remaining visual review are still pending before publication.

### Current-query cannon/conveyor replays and release audit

Powered/unpowered cannon and powered/open-clutch conveyor pairs now pass through actual UI controls, each with 101 frames, zero console errors and exact Reset. This completes the planned representative shared-flight replay set; see [evidence](flight-browser-regressions.md). Earlier sections retain their chronological pending statements, not the current status.

Before publishing this component, finish these specific checks:

- Review continuous latch/helix presentation, not only isolated screenshots; retain mobile limitations explicitly.
- Cover off-centre, oversized and multiple-payload loading, including finite motion and energy/stroke constraints.
- Internal-body identity collision checks are now implemented and covered natively; recheck real-UI creation/Reset after this authoring-validation change (details below).
- Re-run the production Release export after the near-tangent query correction, review changed code for typed selectors, then commit/push only when component acceptance is satisfied.
- Add progressive launcher lessons as part of the still-incomplete 75-level campaign.

The earlier checklist below is the original implementation contract; implemented items are evidenced above, not all outstanding work.

### Internal-body identity validation

Internal body roles are now declared with `InternalBodyRole` before scene construction. The launcher declares `Plunger`; only the explicit scene/diagnostic ID boundary converts that enum to its serialized suffix. Unknown or undeclared roles throw. Existing valid body IDs remain unchanged, so this does not alter the solver, body ordering or established valid trajectories.

`MachineWorld` validates authored and declared internal IDs together before adding a node, and validates all candidates before replacing a loaded machine. Collisions are rejected, not renamed or mapped through an alias. Five new native cases cover both insertion orders, both load orders with exact preservation of the previous machine/body references, and invalid/undeclared role rejection. The focused launcher/diagnostic suite passes **33 cases**. The production Release export passes after this change. The first full native run had 1,121 passes and one failure: `OpticalPortsTests.RotatedPortGeometryAndOcclusionDoNotLeakBetweenInputs` directly inserted three probes with identical empty IDs. An isolated rerun confirmed `Duplicate instance ID` at the new validator. The fixture now uses enum-declared roles converted to distinct IDs at its scene-configuration boundary; production validation was not weakened. The corrected full run passes **1,122 tests** (44 seconds), and a fresh diagnostic export also passes. A real-UI creation/Reset replay is still required against a new diagnostic export after this change; earlier trajectory evidence remains evidence for the unchanged solver.

### Loading edges and post-identity-change UI proof

Three new native cases cover a 0.05-unit off-centre ball, a radius-0.6 ball too large for the 0.41-radius guide, and two stacked balls. All verify finite positions/velocities and constant body count while winding and releasing, plunger axis/stroke limits, no extra work after supply loss, one complete release and exact Reset. Total stored/kinetic/gravitational energy after shutdown remains below the initial budget plus the existing 2% numerical allowance. Off-centre and stacked loads rise; the oversized ball does not rise more than 0.01 while the plunger completes its empty stroke. These are three sampled edge configurations, not proof of arbitrary stacks or offsets. The full suite now passes **1,125 tests** (43 seconds).

`wound-spring-stacked-v1` adds a second catalogue ball through the real UI to the standard powered construction (eight parts, four typed links). The original lower ball settles near Y=3.7599945 and reaches sampled Y=5.819626; the upper ball settles near Y=4.439987 and reaches Y=6.500366. A motion capture visibly shows both balls above the extended launcher. One release spends 51.2000015 energy, then continued supply rewinds the spring. The four body identities remain the two payloads, striker and existing plunger. There are 87 retained frames through tick 792, zero console errors and exact Run/Reset construction equality.

This browser attempt uses the diagnostic export after enum-role/identity validation, refreshing real-UI creation and Reset after that change. Source hashes: MachineWorld `72e3adebea686ea556c996e82826e38c4185cdd7a1991ad5cc7d2f14908016bc`; WoundSpringPart `c6878b347d5552b3649a8c60016321d19f07a8a46d40a3be962ef6777a06d93e`. Full local log, adapter source and screenshot paths are retained; recipe is appended.

The capture advances fewer simulation ticks than the single-ball runs using the same nominal observation waits. That does not establish its cause or frame rate; measure stack runtime and continuous latch/helix motion before claiming fluid performance. Off-centre and oversized checks remain native-only. No production behavior changed for these loading tests.

### Stacked-load performance acceptance failure

Paired actual-UI runs use the same current diagnostic export and capture schedule. The adapter records host `Date.now()` when each read-only CCFRAME message arrives. This measures observed simulation progress, not rendering frame rate or isolated CPU cost.

| Tick interval (one simulated second) | Single ball wall ms | Two stacked balls wall ms |
| --- | ---: | ---: |
| 0–120 | 963 | 977 |
| 120–240 | 1,001 | 4,785 |
| 240–360 | 999 | 935 |
| 360–480 | 1,001 | 4,632 |
| 480–600 | 999 | 9,007 |
| 600–720 | 1,001 | 9,010 |

`wound-spring-single-timing-v1` retains 101 frames; `wound-spring-stacked-timing-v1` retains 87. Both have zero console errors and exact Reset, but the stacked attempt **fails performance acceptance**. The launch interval remains close to wall time while supported-stack intervals slow substantially. Repeated resting-contact resolution is a hypothesis to profile, not yet a proven cause. Do not publish this element as fluid or complete on the strength of correct final positions/native tests.

Both recipes and complete local logs (including timestamped observations, exact adapter source and screenshot paths) are retained. No production code changed in these measurements. Next work is to instrument/profile contact iteration cost, correct the cause without discarding simulation time or adding fallback physics, and repeat the timed pair.

## Original implementation and acceptance contract

- Integrate the supplied winding drive with the new mechanical work allowance; debit accepted work and keep its physical shaft/plunger poses honest.
- Add a finite-mass plunger with collision-swept movement and real payload contact.
  Account released work into plunger/load kinetic energy, gravity and dissipation;
  blocked geometry must never grant a free impulse. Payload mass must matter.
- Render a continuous helix, moving plate, gold latch and readable charge marks
  using the existing cream/navy/cyan/gold palette. Physical and visual poses agree.
  Add an original icon and scene/catalogue entry without a new toolbar.
- Define fixed-tick trigger ordering, empty stroke and obstruction behaviour.
  Support rotated placement and exact Run/Reset/current-schema restoration.
- Real-UI Playwright constructions must cover winding/release, no drive, reverse
  drive, retained charge after drive loss, partial charge, repeated trigger without
  recharge, blocked winding/release, empty shot and different physical payloads.
  Verify actual typed links and configurations; retain failed attempts.
- Prove integration with motor/belt and a receiving puzzle part, review continuous
  spring/latch motion, then commit and push the individually verified element.
- Add progressive lessons within the 75-level campaign; keep mobile and broader
  interacting-load coverage explicit.

### Native contact-loop measurement (unresolved performance regression)

Added a read-only `MaximumFlightIterationsThisStep` counter: it resets at each running tick and records the largest sweep-loop count across that tick's substeps, including the final clear sweep. It does not alter impulses, time advancement or the existing convergence guard.

`RestingPayloadContactIterationsRemainBounded` runs the same powered launcher with either one ball or two stacked balls for 360 ticks. The single-ball control passes a 64-iteration-per-substep performance budget. The stacked case **fails**: maximum **142**, sum of per-tick maxima **24,443**, mean per-tick maximum **67.8972**. This mean is not the average over every substep. The stack test took approximately 730 ms versus 121 ms for the control in this native run; these are test durations, not browser frame-rate measurements.

Command: `dotnet test CuriousContraptions.tests --no-build --no-restore --logger 'console;verbosity=normal' --filter FullyQualifiedName~RestingPayloadContactIterationsRemainBounded` after building/running the same filter. Result: **1 passed, 1 failed**. The failing regression is intentionally retained while the solver correction is pending. The 64-iteration budget is a test assertion, not a new runtime cutoff; the engine still consumes the entire substep or reports convergence failure.

This confirms excessive repeated sweep work in the stack construction alongside the existing browser slowdown. It does not yet isolate each contact's contribution or prove the precise numerical cause. Next: examine the zero-time contact sequence and convergence scale, implement a principled correction, retain tiny-impact and energy invariants, and repeat native and real-UI timed browser controls. The launcher remains incomplete and unpublished.

### Repeated-contact convergence and geometry reuse

A temporary trace at settled tick 340 confirms the numerical cause. At zero elapsed time, the same stack repeatedly exchanges diminishing inward velocity: approximately 0.020435799 initially, 3.020193e-8 at iteration 20, 1.00429275e-14 at 40, 3.3395333e-21 at 60, and 1.2279e-40 at 120. The four substeps repeat this pattern. Raw failed-test output is retained locally at `docs/playtest-results/wound-spring-native-contact-trace-v1.txt`; temporary runtime tracing was removed.

`BodyContactConvergence` records an already-resolved pair's peak approach speed at a single simulation instant. A repeated contact with the exact same normal can converge below four float-relative precision units of that speed, provided its remaining closing displacement is also within the existing geometric contact tolerance. First encounters, changed normals, overlaps and future contacts are not suppressed. History clears whenever time advances and at every substep; the solver still consumes all remaining time, with its original nonconvergence guard intact.

Eight direct tests cover subnormal/tiny first impacts, unchanged impacts, relative scaling, larger renewed approaches, peak-scale retention, changed normals, overlaps, future contacts and displacement bounds. The existing stack regression now passes: maximum **22**, sum of per-tick maxima **4,145**, mean **11.5139** (previously 142 / 24,443 / 67.8972). Single-ball values are **2 / 548 / 1.5222**.

The first browser replays after that correction (`wound-spring-single-timing-v2` and `wound-spring-stacked-timing-v2`) preserved zero console errors, launch/rewind and exact Reset, but the stack still **failed** real-time performance: the two settled one-second intervals took **1,805 ms** each. The single-ball intervals remained about 1,000 ms. These intermediate failures and adapter source are retained, not replaced by later passes.

The flight loop now captures solid geometry once while iterating velocity-only contacts at the same instant. Capture preserves proxy order, rigid-transform validation and query semantics. It is invalidated on positive time advancement, overlap repair, surface callbacks and motion-limit events. Zero-time contacts no longer perform redundant zero-distance scene-node writes. Three snapshot tests cover move/hide/proxy removal, and a world test proves that a zero-time callback opening a gate invalidates the cached path for another body too. The full native suite passes **1,139 tests** (46 seconds), and the diagnostic export passes.

The post-snapshot stacked replay (`wound-spring-stacked-timing-v3`) retains 107 frames, zero console errors and exact Reset. Tick intervals 0–120, 120–240, 240–360, 360–480, 480–600 and 600–720 take **962, 1005, 986, 1039, 985 and 1001 ms** respectively. This sampled desktop construction now keeps pace with simulation time; console-arrival timestamps are not a render-frame-rate measurement or mobile proof. Final same-build single-ball control and release export verification are recorded below when complete.

This changes shared flight behavior and geometry-query implementation: earlier representative shared-flight and launcher-mode browser proofs need affected-case replay before release. No component completion or publishing claim is made.

#### Final same-build control and export

`wound-spring-single-timing-v3` retains 101 frames, zero console errors and exact Reset. Its corresponding six one-second intervals take **968, 1000, 999, 1002, 1000 and 1000 ms**. The stacked run verifies eight authored parts/four typed links, a single release of **51.2000015** energy and subsequent rewind. Its two payloads reach sampled post-release peaks **5.819626** and **6.500366**, unchanged from the earlier stacked proof. The inspected motion capture shows both balls above the launcher. Both v3 attempts pass sampled desktop simulation-pacing acceptance; continuous rendering FPS and mobile performance are still unproven.

Full logs, timing samples and exact adapter source are retained locally as `docs/playtest-results/wound-spring-single-timing-v3.json` and `docs/playtest-results/wound-spring-stacked-timing-v3.json`. The source hashes for this uncommitted revision are:

- `engine/WorldFlight.cs`: `9f537623db965f933fcce15f45bfd4b0387bdd2c9d52fab0ff933491d6be73e9`
- `engine/WorldGeometry.cs`: `b7c9058ea536bc8e3fa1d572a10f3adb7bd5a27fefc81323d4b2d56f011465eb`
- `engine/BodyContactConvergence.cs`: `c24b7f23ba4bb66518a9460037adf5b25aaa30842f71b4dd9e011b42c1f697c3`

`dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet` completes successfully. This last export leaves the local AppBundle in production mode; rebuild with diagnostics before the next evidence replay. No commit/push has been made for this incomplete launcher. Remaining acceptance work and refreshed shared-flight browser evidence still gate its publication.

### Shared-flight refresh after convergence/snapshot changes complete

All 16 representative shared-flight positive/control UI replays now pass on the same source revision as the v3 spring timing pair. They cover pulley/counterweight, springboard, straight pipe, 90° bend, trampoline, linear pusher, cannon and conveyor. Each has zero console errors and exact Reset; all actual constructions and 1,358 common body/mechanical/cannon samples match the earlier baselines exactly. See [the completed replay table](flight-browser-regressions.md#completed-convergencesnapshot-regression-replay) for case IDs, outcomes, source hashes and retained evidence.

No production source changed during these replays. The existing 1,139-test native pass and production Release build remain applicable; the current local AppBundle has been rebuilt with diagnostics for further UI verification. The representative shared-flight replay gate is cleared. Launcher-specific modes still need refreshed evidence after the shared-physics change, followed by continuous latch/helix review and the remaining acceptance/release work. The launcher is still incomplete and uncommitted.

### Launcher controls refreshed after convergence/snapshot changes

Five actual-UI replays now refresh the no-power, empty-stroke, reverse-drive, partial-charge and retained-charge/repeated-trigger cases on the unchanged convergence/snapshot revision. They use Free workshop, Balanced and the existing palette/gizmo/connection adapter. **All five have zero console errors and exact Reset.** Actual constructions match the earlier baselines. All **507 common body/mechanical samples** match exactly; spring fields also match wherever the historical baseline already emitted them. The earliest no-power/empty logs predate spring-energy diagnostics, so their new energy assertions come from the current traces, not missing historical fields.

| Case | Current observation |
| --- | --- |
| `contact-cache-spring-unpowered-v1` | 101 frames. Spring/motor work and stored/released energy remain zero; no release, empty trigger. Payload rests at Y=4.5599976; plunger remains at Y=3.9. |
| `contact-cache-spring-empty-v1` | 101 frames. Same striker and plunger IDs in every frame; no payload created. Plunger rises from Y=3.1 to Y=3.2408347 at sampled tick 240 with upward speed 7.7303395, completes one 51.2000015-energy release, then rewinds under continued supply. |
| `contact-cache-spring-reverse-v1` | 101 frames. Five links verified. Spring input −6 rad/s with 20 torque available, but accepted/stored/released work remains zero; no launch. |
| `contact-cache-spring-partial-v1` | 101 frames. First release accepts 18.480027 energy and reaches sampled payload Y=5.2825623. Five releases occur; final weak release remains blocked under the load with 0.128633858 energy retained. No recurrence of the old contact-loop error. |
| `contact-cache-spring-retained-repeat-v1` | 103 frames, twelve parts and eleven links. Ticks 516–588 retain 51.2000015 energy with zero motor speed/torque/work. Launch starts by tick 600, reaches sampled payload Y=7.16687, and completes by tick 624. Accepted work stays unchanged after shutdown. The next trigger reports empty by tick 720; release count stays one. |

All recipes are appended to [the recipe file](wound-spring-recipes.json). Complete local logs include exact adapter source, UI actions, configurations/links, frames, screenshots and the same three solver source hashes recorded above. Explicit energy/control checks and baseline comparisons are retained at `docs/playtest-results/contact-cache-spring-first-mode-audit-v1.json`. Empty-stroke and partial-charge motion captures were inspected; this is not yet the continuous latch/helix visual review.

Current freshness: the powered single/stack pair, these five launcher cases and all 16 representative shared-flight cases are refreshed. Still refresh winding obstruction/clear control, timed blocked/open/resumed release, rotated launch/receiver/control and tennis/bowling payload comparisons. Off-centre/oversized UI coverage and continuous animation review also remain before deciding component completion. No production code changed in this replay batch; no commit/push yet.

### Obstruction and resumed-release refresh complete

Five more real-UI attempts pass on the same unchanged solver revision, in Free workshop/Balanced. Obstacles are staged away while wiring, then moved into place with actual handles before Run. Every attempt has zero console errors and exact Reset.

| Case | Frames | Verified outcome |
| --- | --- | --- |
| `contact-cache-spring-winding-obstacle-v1` | 101 | Four parts/two links. Panel limits compression to 0.455 and plunger Y to 3.445. Stored/accepted work stabilizes at 16.5620012, without accepting excess drive work. |
| `contact-cache-spring-winding-clear-v1` | 101 | Same construction with panel outside the stroke. Compression reaches 0.8 and accepted work stops at 51.2000015. |
| `contact-cache-spring-blocked-release-v1` | 102 | Eleven parts/ten links. Closed shutter stops the released plunger at Y=3.2168934, retaining 37.3307573 energy. Release count remains one. |
| `contact-cache-spring-open-release-v1` | 102 | Eleven parts/ten links, clear stroke. One release spends 51.2000015 energy and continued supply rewinds the spring. |
| `contact-cache-spring-resumed-release-v1` | 102 | Thirteen parts/fourteen links. Ticks 600–756 retain 37.3307573 energy while blocked. Reopening allows extension at sampled tick 768 and completion by tick 780. Motor speed/torque/work stay zero; accepted work stays 51.2000015 and release count remains one. |

The blocked-release screenshot was inspected and shows the shutter across the physical stroke. Complete local logs, adapter source, actual connections/configurations and screenshots are retained under these case IDs. Explicit energy/work/Reset checks are saved in `docs/playtest-results/contact-cache-spring-obstruction-audit-v1.json`; recipes are appended to the tracked recipe file. Rotated integration, payload comparisons and the remaining visual/loading review are still pending; this does not mark the component complete.

### Rotated integration and catalogue payload refresh complete

Five final planned mode replays pass through actual UI controls on the unchanged convergence/snapshot revision. Every run has zero console errors and exact Reset. The actual constructions match their earlier baselines, and all **506 common body/mechanical/spring diagnostic samples** match exactly.

| Case | Frames | Verified outcome |
| --- | --- | --- |
| `contact-cache-spring-angled-v1` | 101 | Actual −15° Z rotation verified; payload starts at (0.4,4.5017104,0), launches once with 51.2000015 released energy and reaches sampled Y=6.844635. Without a receiver it eventually leaves the bench. |
| `contact-cache-spring-angled-receiver-v1` | 101 | Same launch, eight parts/four links. Basket retains the original ball at (3.833435,1.1533356,0), X velocity −0.0036621094. Outcome screenshot visibly confirms containment. |
| `contact-cache-spring-angled-unpowered-v1` | 102 | Only motor supply link omitted. Accepted/stored/released energy stays zero, release count stays zero, trigger reports empty. Ball remains in the tilted guide near (0.5035248,4.47139,0), with small residual motion; no claim of exact rest. |
| `contact-cache-spring-tennis-v1` | 101 | Actual catalogue mass 0.35, radius 0.25, bounce 0.78. One 51.2000015-energy release reaches sampled post-release Y=9.656113. |
| `contact-cache-spring-bowling-v1` | 101 | Actual catalogue mass 4, radius 0.38, bounce 0.14. Equal released energy reaches sampled post-release Y=4.9493256; the initial drop height is not counted as launch height. |

The payload comparison changes mass, radius and restitution together; it is not an isolated mass experiment. Full logs and adapter source are retained under the listed case IDs, recipes are appended, and explicit checks/comparisons are at `docs/playtest-results/contact-cache-spring-aim-payload-audit-v1.json`. The three solver source hashes are unchanged from the v3 timing pair and preceding shared-flight replays.

**Current release status:** the planned launcher-mode replays and all 16 representative shared-flight replays are refreshed after the performance fix. Single/stacked desktop pacing is also current. Remaining work is off-centre/oversized player-facing loading coverage, continuous latch/helix visual review, final changed-code/typed-selector review and progressive launcher lessons. Native oversized coverage already exists, but no current oversized catalogue ball has been identified: the basketball is catalogue `ball` (not a separate `basketball` resource), and the bowling radius is 0.38, below the 0.41 guide radius. Do not claim a bowling shot proves oversized exclusion. Keep mobile and broader campaign/difficulty coverage explicitly incomplete. No production source changed during these replays; no commit/push yet.

### First campaign lesson: Wind, then release

The campaign now contains **59 draft levels**. The new lesson is appended at 59 while the final 75-level progression is still being assembled; Free workshop is now row 60. Historical workshop recipes with row 59 remain unchanged evidence of their earlier campaign revision, not current recipes.

The lesson has seven locked fixtures and one placeable wound-spring launcher. The player positions and tilts the launcher toward the basket, supplies the motor electrically, belts its shaft to the spring, and connects the impact switch through the one-shot delay to the latch. The fixed payload begins above the tilted guide. Only the original payload captured by the basket satisfies the goal. The timer signal cannot create launch energy.

Authoring lives in `tools/Campaign/WoundSpringLesson.cs`, with enum-typed roles/catalog choices and explicit JSON boundary mappings, typed connection domains/sockets and typed goals. Its part-local assistance bounds full correction to 0.2 units/5 degrees on Forgiving and 0.1 units/2 degrees on Balanced, with continuous 0.4-second blending; Precise supplies no placement correction. These authored limits are not yet verified as a monotonic difficulty success region. The campaign generator and checked-in JSON match after canonical JSON normalization (SHA-256 `8daacba5656dbc0aec8eeb4cc1ef7aa7fa6b78b43cc38938a84af8f6ade51342`).

Six new native cases prove reference success at all three difficulties, missing electrical supply, missing mechanical belt, missing activation wiring, appropriate accepted/stored/released energy, and exact Reset. An initial test compile referred to a nonexistent `ReleasedEnergy` property; it was corrected to the existing `ReleasedWork` API before execution. No production solver change was needed. The full native suite passes **1,145 tests** (48 seconds). Diagnostic and production Release exports both pass. The current local AppBundle is the production export; rebuild with diagnostics enabled before the next diagnostic UI attempt.

Actual-UI Playwright evidence on Balanced:

| Case | Evidence |
| --- | --- |
| `wound-spring-lesson-reference-v1` | Palette placement, −15° Z gizmo rotation, four UI-created typed links. Eight actual parts; win at tick 498 (4.15 seconds), 62 diagnostic frames, one release spending 51.2000015 energy, zero errors and exact Reset. The inspected outcome image shows the payload inside the basket. |
| `wound-spring-lesson-no-supply-v1` | Identical placed parts with only the electrical link omitted. Three verified links; 101 frames through tick 960, no win, accepted/stored/released energy all zero, zero releases and an empty trigger. Zero errors and exact Reset. This is an eight-second observation, not a campaign timeout claim. |

Recipes are appended to `docs/wound-spring-recipes.json`; complete local results, UI actions, adapter source, source hashes and explicit audit checks are in `docs/playtest-results/` under those case IDs. Tested base HEAD remains `0ede1e1bd72d7b5d929a4c7e26dee5e5a0c618c0` plus the recorded uncommitted changes. The campaign file SHA-256 is `8d9bb28b9aec3f0ed937a8b4475591613028a4d74835583067b700293b788ab4`. The solver hashes remain unchanged from the refreshed component/shared-flight proofs.

This completes the first introductory lesson's focused positive/control proof, not the whole launcher release gate. Off-centre/oversized player-facing coverage, continuous latch/helix review, final typed-selector audit, further progressive combinations and final campaign ordering remain open. Prototype title remains; no commit/push as a verified element yet.

### Off-centre loading, oversized catalogue weight and continuous desktop motion

The remaining representative loading edges now have actual-UI proof on the unchanged production solver. These recipes use Free workshop **row 60** of the 59-level campaign, Balanced difficulty, palette placement, gizmo movement and four UI-created typed connections. Both have seven actual parts, finite sampled body states, one release spending 51.2000015 energy, zero browser errors and exact Reset.

- `wound-spring-off-centre-video-v2`: requested 0.05 X offset becomes an actual 0.061009478-unit offset through the handle. The payload reaches sampled Y=7.1043854; 101 diagnostic frames run through tick 960. Continuous 1440×900 VP8 video is retained at `.playwright-mcp/wound-spring-off-centre-video-v2.webm` (57.08 seconds, 25 fps; SHA-256 `c4d6db5be8b03a1fcea8c2895b4d9cf20372d366df1b885f7b427eb428f32f52`).
- `wound-spring-oversized-weight-v1`: catalogue Weight has mass 4 and a spherical collision envelope radius of approximately 0.507968, larger than the guide's 0.41 inner radius. Its centre must stay above Y=4.699886 for the centred annular-rim geometry; the minimum observed Y is 4.69989. It never loads into the guide. The rounded plunger's terminal envelope can nevertheless tap its underside: sampled post-release peak is Y=4.8177795, followed by return to the rim. This is **oversized exclusion, not zero movement**. 101 frames run through tick 960; the outcome image was inspected.

The first added native catalogue-weight test incorrectly reused the radius-0.6 ball's “rise ≤0.01” expectation. It failed with a 0.118270874 rise; the detailed failure is retained at `docs/playtest-results/wound-spring-oversized-weight-native-v1-failure.txt`. The smaller weight can contact the rounded head while resting outside the mouth, unlike the larger sphere. The corrected enum-selected case checks independent rim-clearance geometry every tick, finite bodies, constrained plunger travel, unchanged accepted work after supply loss, whole-system energy, one release and exact Reset. It expects the physical tap and bounds rise by available energy rather than an arbitrary immobility threshold. All four loading cases pass. A misplaced test-only check briefly caused compilation errors and was moved into the intended loading helper; no production physics was changed.

`wound-spring-close-motion-v1` repeats the off-centre setup with actual E-key orbit and wheel zoom before Run. It has 102 diagnostic frames, zero errors, one release and exact Reset. Its continuous video is `.playwright-mcp/wound-spring-close-motion-v1.webm` (59.56 seconds, 25 fps; SHA-256 `272e37c36be796b50dfda1e1f7e9182de478411a24dab7072e829a0af111c515`). The winding interval around video seconds 50.4–51.65 and every consecutive 40-ms recorded frame around 51.9–52.7 were inspected. The helix compresses as the head winds down, extends with the physical release, and compresses on recharge; the gold latch visibly opens through intermediate angles and returns. No cosmetic teleport or detached coil was observed. This closes the focused desktop latch/helix readability review, not a 60-fps guarantee, mobile validation or a large-machine performance benchmark.

The first video attempt completed the browser workflow but failed to return evidence because `video.path()` is unavailable for a remote connection. Preserve `wound-spring-off-centre-video-v1-capture-failure.json`; it is not counted as behavioural proof. The successful replay uses Playwright's `video.saveAs()` after closing the recording context. No game evaluation, storage edits or transform setters are used.

Complete local results, exact recording adapters, source hashes, actual configurations, audits and native output are retained under these case IDs in `docs/playtest-results/`; tracked recipes are appended. The prototype remains incomplete pending final changed-code/typed-selector audit and the remaining progressive teaching/release decision. Mobile and the full campaign difficulty matrix remain open. No commit/push in this verification step.

Final regression for this step: `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet` passes **1,146 tests**, zero failures/skips, in 48 seconds. `git diff --check` passes. Only tests and evidence changed in this step; the production build from the preceding lesson remains applicable. The local AppBundle is now diagnostic again for continuing UI verification.

### Changed-code audit: exact diagnostic enum boundary

Review covered the new launcher parameter/state/activation selectors, guided-body ownership and identity handling, changed swept-contact/convergence/snapshot paths, their focused regression tests, diagnostic serialization and lesson authoring. Internal launcher choices use enums; catalog/resource/instance names remain at the existing registry/scene boundaries. This is not a completed repository-wide typed-identifier audit.

The review found a concrete boundary violation: the default string-enum diagnostic converters accepted case variants, padded names and comma-combined non-flags values. Twelve rejection cases exposed **ten failures**; retain the pre-fix output at `docs/playtest-results/wound-spring-enum-boundary-v1-failure.txt`. Numeric strings were already rejected.

The two launcher converters now use an exact ordinal mapping generated from their enum members solely at the serialization boundary. Unknown names, aliases, non-string values and undefined output members throw `JsonException`. No fallback, compatibility alias or case folding is present. Five phase and three trigger canonical-wire round trips supplement the rejection tests and existing typed-frame/null round trips: all **25 focused diagnostic cases pass**. All launcher runtime properties remain enum-typed. This forward refactor does not change the canonical JSON names or solver behavior.

The full native suite passes **1,166 tests** (48 seconds). Diagnostic export passes. A fresh real-UI level-59/Balanced replay, `wound-spring-exact-enum-reference-v1`, places/rotates the launcher and builds its four links. It wins at tick 498 with 62 diagnostic frames, zero errors and exact Reset. Its actual configuration and all 62 common sampled frames match the previous lesson reference exactly. Complete local result/adapter/source hashes/audit and the tracked recipe are retained. The solver hashes remain unchanged; earlier shared-flight and continuous visual evidence is still applicable.

The design document now reflects the completed focused 25-fps continuous latch/helix review, while retaining explicit mobile/large-machine limitations. Final progressive teaching/release review remains before publishing the launcher as a verified element; the broader repository enum audit stays open. No commit/push in this step.

Post-audit production Release export also passes. `git diff --check` is clean. The current AppBundle is the production export; enable diagnostics again before subsequent diagnostic browser tests.

### Second lesson and component release decision

**60 draft campaign levels** now exist toward the separate 75-level target. Level 60, “Saved for later”, follows “Wind, then release”. The player places a hold timer and makes seven connections: two supply cables, one winding belt and four activation links. A one-shot input delay starts the two-second supply contact and a fixed three-second release delay. The motor stops before the latter releases the already-wound spring. The objective is physical basket capture; alternative deliveries are allowed, so this lesson does not impose a hidden exact-wiring requirement or require the motor-off pattern as a separate victory predicate.

Five new native cases cover the reference at all three difficulties plus missing supply/release. They require at least 30 ticks of zero-input retained charge before launch, unchanged accepted work thereafter, one fully spent release, successful basket capture and exact Reset. All eleven launcher campaign tests pass, and the full suite passes **1,171 tests** in 49 seconds. The C# generator matches checked-in content after canonical normalization (SHA-256 `205fa24192ea6f170993e2061db3eb41293c09ba3c9efe86887cc243fdb121a1`). The raw campaign SHA-256 is `b754ff36a5599357fe29de073b4c9dabd62c15f5f1abd344581fd9a8ae34b685`.

Actual-UI Balanced proof:
- `wound-spring-retained-lesson-reference-v2`: ten parts/seven verified links, 92 diagnostic frames, win at tick **858** (7.15 seconds). Ticks 516–588 show 51.2000015 retained energy with zero shaft speed, torque and available work. Release is sampled at tick 600 and completes by 624; accepted work never increases after shutdown. The outcome image visibly shows the captured payload, stopped motor and success state.
- `wound-spring-retained-lesson-no-release-v1`: same placement with only the last latch-trigger link omitted. Ten parts/six links, 111 frames through tick 1080, no release/no win; the spring remains armed with 51.2000015 stored energy after the motor stops.
Both have zero errors and exact Reset.

The first reference attempt also won at tick 858 according to its lifecycle record, but its top-level outcome and screenshot were copied before that event. Retain `wound-spring-retained-lesson-reference-v1.json` as an instrumentation defect. The corrected adapter records the final outcome after observation/Reset and waits an extra second before the outcome image. No gameplay state is set. Complete recipes, adapter and audits are retained. Free workshop is now row **61**; historical recipes retain their original revision's row numbers.

The component's focused release gate is now satisfied: finite-work winding/latching, no/reverse/partial/full supply, empty and repeated triggers, retained-power launch, obstruction/resumption, mass-dependent and off-centre/stacked/oversized interactions, exact Reset and identity validation, real-UI integration/control proofs, continuous desktop visual review, changed-code boundary review, and two gradually taught lessons. The prototype label is removed. Diagnostic and production Release web exports pass; the final production export includes the new title and level 60. `git diff --check` passes.

This is a verified **wound-spring launcher**, not completion of every backlog component, measured TIM physics equivalence, mobile support, every possible mechanism combination, the final 75-level ordering or the deferred exhaustive difficulty matrix. The remaining project requirements stay open in TODO.md. Commit/push/deployment are recorded by Git and the ensuing delivery record.
