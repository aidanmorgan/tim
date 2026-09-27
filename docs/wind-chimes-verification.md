# Wind chimes and shared airflow

27 September 2026. Focused component verification; the 75-level teaching progression and full difficulty matrix remain unfinished.

## Behaviour

The cyan sail belongs to a constrained 1.5-unit three-dimensional pendulum with a 0.35-unit effective mass. Gravity and the sampled airflow force move it at the existing four physics substeps per tick. The clapper follows at 60% of the pendulum length. Contact against one of four tube envelopes emits a sound only above 0.08 units/sec; separation by 0.025 units rearms that tube. Mere fan-volume overlap cannot ring stationary chimes. Damping and collision restitution allow the chime to coast after a fan stops and eventually settle. A steady jet can hold the clapper against a tube; that resting contact is not an automatic repeating clock.

Tubes have distinct lengths and typed Low/Mid/High bands. They are simplified fixed contact envelopes, not simulated elastic metal. An external moving ball can also strike the tube collision boxes, with an independent 0.8 units/sec threshold and separation rearm. The cream cap does not act as a sound trigger.

At most one acoustic pulse is emitted every 24 ticks, with five in flight. Same-tick strikes select the strongest, breaking ties by typed tube identity. The selected tone is played once on the following tick, after all same-tick collisions settle. Sound is omnidirectional and uses the shared distance/travel/blockage rules. There is no electrical input on the chime. A receiving sound meter still needs independent electricity to actuate another mechanism.

## Shared airflow

Fans now expose a typed finite cylindrical emitter. One snapshot per substep collects all forces before applying them to balls and sail targets, so part ordering cannot create a direct fan/receiver update-order dependency. Opposing jets add vectorially; world pressure scales the force; heavier balls accelerate less. The fan's existing force, reach and starting-running parameters are centralized and validated; its width is an explicit catalog parameter, not a hidden default lookup.

WorldGeometry.Trace replaces the old LightNetwork.Trace API. Every caller and test now selects a typed Light, Sound or Air medium; no compatibility wrapper remains. Light/sound retain the existing opaque-proxy rule. Air is blocked by transparent solid boxes and tube walls as well, including clear bends and funnels through signed-distance tracing. Open straight bores remain open. This is direct jet occlusion, not a sealed pneumatic network: wind is not automatically guided around bends, and there is no turbulence or pressure reservoir.

The existing fan is still a self-contained, activation-controlled drive. It does **not** yet accept external battery/shaft power. That separate forward-refactor and its campaign updates remain in TODO; this feature does not claim they are done.

## Native and driver evidence

- `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`: **687 passed**, including **26 new chime/airflow cases**.
- `node --test tools/Playtest/direct-ui.test.cjs`: **39 passed**.
- Cases cover actual fan→clapper→tube→sound-meter→counter operation, weak/zero/inactive/misaligned/blocked airflow, source ordering, four yaw orientations, a tilted 3D pendulum, constraint length, stop/coast/restart, direct ball strikes and misses, transparent straight/45°/90°/funnel walls, open bores, opposite jets, zero pressure, mass response, invalid inputs, simultaneous tone selection, audio independence, presentation independence and exact Reset/JSON construction replay, and build-mode pitch/roll/upside-down rest-pose consistency.
- The existing campaign and physics tests pass after the fan-force and geometry-query forward refactors. These reference checks are not the deferred full real-UI campaign/difficulty matrix.

Two initial compile mistakes were corrected: the geometry extraction omitted its epsilon constant, and the ring-axis switch needed parentheses around its modulo expression. A final editor review added three initially failing pitch/roll/upside-down pose checks: a stale world-space swing was being shown after editing the part's rotation, then corrected at Run. Build mode now displays the authored local rest pose; the tests pass without mutating pendulum physics during rendering. No native behavioural assertion was weakened.

## Browser evidence

- `wind-chimes-relay-v1`: retained failed setup. Run/Reset matched and no browser errors were reported, but the fan was actually placed at Y=0.5274403 instead of 5.4. Inspected screenshots show no chime actuation and a closed gate. This is **not** a positive component proof. The later retry records the intended upward gesture correctly; the original failure's cause remains unresolved.

- `wind-chimes-relay-v2`: fan drag captured before/after, actual fan Y=5.384113; inspected motion/holding frames show displaced clapper/sail, expanding wave, active timer and open gate. This uses the final next-tick audio selection implementation.
- `wind-chimes-blocked-v1`: same fan placement plus a separate wall across the jet, not intersecting the fan or chime. Inspected holding frame shows a resting sail, no wave, idle timer and closed gate.
- `wind-chimes-relay-v3`: final editor fix; inspected 90° pitch preview and return upright are correct. The subsequent setup missed the meter→timer activation wire and moved the battery unexpectedly during wiring. The closed gate is therefore a failed setup, not a second positive proof. Run/Reset still matched; no browser error was reported.
- `wind-chimes-rewired-v4`: repaired that missing wire through the real UI in the existing v3 scene, explicitly confirmed the activation connection in the Run record, and inspected the holding frame showing the timer active and gate open. The battery remains at its actual alternative location (4.6,1.9895903,4), not the originally requested location. No game-state setters or reload were used.
- v2, the blocked control and the v4 repair each report zero browser errors, one Run/Reset and byte-identical construction restoration. Neither v1 nor v3 is counted as a successful chain. The intermittent construction/connection failures remain unresolved UI work, not hidden retries.

Reproduce in the free workshop (currently selector row 59), Balanced: wind chimes (-1,6,0), fan (-4,5.4,0), sound meter (2,6,0), hold timer (1,3,2), powered gate (4,3,0) rotated 90° around Z, battery (-4,2,3). Connect meter activation→timer activation, battery supply→meter power, battery supply→timer power and timer supply→gate power. Use toolbox and real handles, then Run/Reset. The negative control adds a wall at (-2.5,5.4,0), rotated 90° around Y. Positions describe the intended assembly; records contain the actual UI placement.

Browser records are retained locally in `docs/playtest-results`, screenshots in `.playwright-mcp`. Only toolbox clicks, real 3D handles, wire choices and Run/Reset are permitted; diagnostics are read-only. Do not count lifecycle success as behavioural success.

## Production verification

`dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet` passed after the editor-pose fix. `git diff --check` passed. Anvil graph queries were ready, but its write gate was authentication-unavailable and proceeded under allow-with-warning; no passed gate is claimed.

## Limits

Auditory listening quality, browser audio suspension, mobile legibility and broad continuous-motion/performance testing remain pending. Cosmetic wavefronts and audible playback do not clip at blockers; meter reception is authoritative. New lessons, externally supplied fans, sealed pneumatic routing and original-game calibration remain separate outstanding requirements.
