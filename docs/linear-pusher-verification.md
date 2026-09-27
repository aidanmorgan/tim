# Electric linear pusher — focused verification

27 September 2026. Focused native, real-UI and production-build verification passed. Evidence applies to the implementation introduced with this document (parent revision f0d91506ecf81b6acdfcc338e91711d517e50d07), including the loaded-speed correction. This is not completion of campaign, mobile or every possible mechanism combination.

## Current implementation

C# catalogue part, scene and original icon. Independent PowerIn, ExtendIn and RetractIn enum sockets; conflicting commands stop, absence of either command holds, and power loss engages an ideal self-locking brake. Electrical routes pass the independent supply to RetractedOut/ExtendedOut only at their respective endpoints. Electrical state is sampled once per fixed tick, so endpoint feedback is seen on the following network tick.

The cream spherical head follows bounded stroke/speed/acceleration along local +X; the gold rod and physical head proxy follow its actual extension. Finite-radius world sweeps stop the head at solid geometry. Contact with cargo applies a normal impulse bounded by authored force times substep and contact alignment, capped at target normal velocity. Cargo moves through normal physics integration, not positional teleportation. Positive kinetic-energy change from drive impulses is recorded as DeliveredWork. Binary electrical supply remains ideal: this does not model charge depletion, voltage/current, motor losses or piston inertia.

The rounded head can deflect off-centre cargo. The rod is a narrow solid box; the barrel and base are solid. A grey/cyan/gold indicator distinguishes idle/unpowered, motion and blocked/conflict. End-output connection choices use paired source and target pictograms, with presentation derived from enums rather than labels controlling behaviour.

## Evidence so far

- 28 native pusher cases: independent supply/command truth cases, full stroke and reversal, supplied endpoint outputs, missing supply, conflicting-command and interrupted-supply braking, three world orientations pushing cargo, per-step impulse/work bounds, thin static wall, pinned cargo and exact mid-stroke/full-cycle Reset. Additional cases cover invalid author parameters, bounded acceleration through reversal, gravity versus insufficient/sufficient force, 4/8-mass cargo, initial head overlap and exact current-JSON replay.
- Two connection-presentation tests check all 15 electrical source/target combinations have distinct labels and rendered icons, and reject invalid ports.
- Full native suite: 883 passed.
- JavaScript adapter unit suite: 63 passed, including all 15 supported electrical source/target selection paths. These are not browser evidence.
- Diagnostic web publish and production Release publish passed.
- No test assertion was relaxed to obtain these results.

## Retained browser attempts and failure diagnosis

- `pusher-cargo-v1`: actual palette/gizmo placement, battery Supply → PowerIn and Supply → ExtendIn. Screenshots show the gold rod extending and cargo moving along the horizontal wall platform (0.63, 1.60 and 4.88 seconds). Exact Run/Reset construction, no browser errors.
- `pusher-no-supply-v1`: identical layout but only ExtendIn wired. At 4.88 seconds the head remains retracted and cargo stays at the starting end of the platform. Exact Run/Reset, no browser errors.
- Both v1 attempts predate the loaded-speed correction below and therefore do not prove the corrected implementation.
- Actual v1 positions: pusher (-2,3,0), cargo (-0.4,3.095643,0), battery (-4,4.9870987,2), platform (1.5,2.546099,0), rotated 90° about X, dimensions (5.0277996,2,0.25). Placement differs from intended centres by at most 0.012902 world units; resizing uses the actual gizmo, not numeric inputs.
- `pusher-endpoints-v2` correctly failed required-link assertions: the test author selected signal lamps, which accept activation rather than electrical PowerIn. No endpoint wires were created. Keep the failed recipe/log/screenshot; use an electrically powered load.
- `pusher-endpoints-v3` used powered gates but failed with an unexpected Run event during construction, before the battery was placed. No completed proof is claimed. The cause is not established; production publishing overlapped this attempt. Retry after builds finish.
- `pusher-endpoints-v4`: corrected implementation, both outputs wired through the actual paired-icon controls to powered gates. All six part kinds and centre positions verified (maximum centre error 0.015567 world units); four typed links match exactly and Reset restores the construction. At 0.63/1.60 seconds the head and cargo advance; at 4.88 seconds the rod is fully extended, cargo has moved along the platform and the extended-output gate is open while the retracted-output gate is closed. No browser errors. This refreshes positive push/end-output proof but does not prove the retracted output opens its load during retraction.
- `pusher-no-supply-v2`: refreshed on the corrected implementation. One actual ExtendIn link, no PowerIn link; at 4.87 seconds head remains retracted and cargo remains at the start. Four correct parts, maximum centre error 0.012902, exact Reset, no browser errors.
- `pusher-conflict-v1`: five actual links, including simultaneous powered ExtendIn/RetractIn and both endpoint loads. Head and cargo remain at the start; home gate opens while extended gate stays closed. Six correct parts, maximum centre error 0.015567, exact Reset, no errors.
- `pusher-reversal-v1`: ten parts/nine actual links. A falling ball triggers a two-second hold timer; its output commands extension and suppresses a separately supplied NOR gate commanding retraction. At 0.62 seconds the pusher is home; 2.25 seconds shows extension and pushed cargo; 3.34 seconds shows return; 7.68 seconds shows fully retracted head and open home gate. Maximum centre error 0.015567, exact Reset, no errors.
- `pusher-interrupted-v1`: nine parts/five actual links. A lower dropped ball starts the supply timer before a higher dropped ball closes the independently supplied extend-command switch. Motion is visible at 2.24 seconds; the expired timer is off at 3.33 seconds, and the head holds the same partly extended pose through 7.64 seconds while the command switch remains on. Exact Reset, no errors.
- Local logs: `docs/playtest-results/<case>.json`; motion/holding/outcome/settled or failure captures: `.playwright-mcp/<case>-*.png`.

The new native vertical-lifting test first failed with force 40: cargo Y=5.5382996 from initial 5.45, extension=0.098087035 after 1.5 seconds. Contact with moving cargo had reset TravelSpeed to zero each substep, repeatedly restarting the acceleration ramp. The correction retains only commanded speed supported by the contacted cargo's actual velocity; rigid obstructions and brakes still stop immediately. Both sufficient-force lifting and insufficient-force non-lifting now pass, without relaxing the assertions. All earlier contact/force/work/pinned-cargo tests still pass.

## Reproduction and limits

The five successful current-build recipes are committed in [linear-pusher-recipes.json](linear-pusher-recipes.json). These describe UI actions, not machines to import: choose free workshop (current selector row 59), add each part through the palette, move/rotate/resize with the on-screen gizmos, connect the explicit sockets and use Run/Reset. The runner reads only CCUI/CCRUN/CCFRAME/CCRESET diagnostics. Actual links and complete Run/Reset construction are asserted; behavioural observations above come from inspected motion captures, not lifecycle success alone.

Browser run used the existing UI driver with workshop selector timing (1.5-second initial wait, Home then 59 Down actions), 40 palette-scroll attempts and held mouse gestures. Standard motion captures follow delays 450/200/200 ms, then a 450 ms hold capture; long timer cases use 450/500/500/500/500/500/500 ms. After holding, wait 2.75 seconds for the outcome image and another 450 ms for settled, then click Back to building. Screenshot overhead means visible simulation times differ slightly. Retain all earlier attempts, including failed v2/v3.

The plate is authored as a rotated wall, with actual resized dimensions approximately (5.0278,2,0.25); no numeric placement/property menu or gameplay-state setter is used. Upper endpoint sockets and lower input sockets are selected through contextual original pictograms. There are no additional persistent menus/tooltips. Build-mode selection is available after Reset; editing a still-running extended head is intentionally not allowed.

World sweeps currently enumerate all proxies, and tangent travel can require many small samples. These short browser runs are not sustained/mobile performance benchmarks. More crowded builds, ropes, simultaneous interacting actuators and downstream tube/conveyor integration need separate proof. Campaign lessons and 75-level progression remain open. This ideal actuator does not claim realistic motor torque, battery depletion, pneumatic pressure or measured original-game fidelity.

DESIGN.md records the current part and paired-icon style. Commit and push this verified element before beginning another; retain the open teaching/integration/performance tasks in TODO.md.
