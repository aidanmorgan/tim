# Swept-flight browser regressions — 28 September 2026

Local diagnostic build based on HEAD a744199e46c239164afedac25e8b808d1cd9c91d plus the uncommitted cannon/shared-flight work. Diagnostic export succeeded before these runs. No game implementation changed in this set; the prior full native suite remains 1011 passing tests. These focused checks do not complete the exhaustive difficulty/campaign matrix.

Source SHA-256:
- WorldFlight: 2106d4d61dd3d01c63275821139683eaff38e50578c009195d34e812b49ac6e1
- MachineWorld: dd8a9d6ede8ca4445915582e94b2eacacc3ed5820dc155bf1994c1ac715bd8d9
- BasketPart: 8ee85d163735fd0005345ffb6a4926ccbef526c379fdd9ff995e148fa264392b

## Method

Playwright selected campaign levels via the puzzle dropdown, selected Forgiving via the visible difficulty control, created parts from the toolbox, moved/rotated them with actual 3D handles, ran the machine and reset through the visible button. No state setters, imported solutions, storage edits or numeric placement menus. CCUI/CCRUN/CCFRAME/CCRESULT/CCRESET are read-only observations.

[Recipes](flight-regression-recipes.json) retain every attempted configuration. The same component UI helper and capture timings described in [cannon verification](cannon-verification.md) were used. Its Reset action now selects “Build again” after a recorded win and “Back to building” while running. This is the current UI state transition, not an old-label compatibility path. Raw actions, actual transforms, body traces and results remain locally in docs/playtest-results/<caseId>.json; screenshots in .playwright-mcp/<caseId>-*.png.

## Results

All completed cases below use precision 0, have zero browser errors and exact Run/Reset construction equality. Negative observations extend through tick 948 (7.9 seconds), not the full 30-second timeout.

| Case | Observation |
| --- | --- |
| flight-spring-forgiving-v2 | Level 4 captured at tick 265. UI spring (-3,0.78461814,0), rotation approximately -19.4°; authored assistance smoothly reaches (-3,0.8,0), -20°. Ball visibly finishes in the basket. |
| flight-pipe-forgiving-v1 | Level 25 captured at tick 307. Actual pipe (0,3,0), -45°, length 3.6. Ball travels through the hollow route and finishes inside the receiver. |
| flight-bend-forgiving-v1 | Level 27 captured at tick 251. Bend reaches (0,4,0), -90°. Ball turns through the bend toward the left-hand basket. |
| flight-spring-wrong-aim-v1 | Spring requested +20° instead of -20°. Bounded assistance does not correct the large error; ball travels left and exits the play area near (-11.33,-5.00,0), without capture. |
| flight-pipe-missed-inlet-v1 | Pipe requested X=3; assistance moves it only to X=2.75. Ball misses the inlet and settles at (-1.3999939,-0.11999512,0), zero velocity, no capture. |
| flight-bend-missed-inlet-v1 | Bend at X=3 remains outside the falling path. Ball settles at (0.7028961,-0.11999512,0), zero velocity, no capture. |

## Retained unexpected results

- flight-spring-forgiving-v1 won at tick 265, then the helper timed out looking for “Back to building”; the actual winning-state button was “Build again”. The attempt has no Reset proof and is not counted as a complete pass. The full v2 repeat verifies the corrected automation path.
- flight-bend-wrong-mouth-v1 was intended as a negative control but **won** at tick 415 with rotation +90°. The 1.15-second capture shows the ball contacting the exterior of the inverted bend, then it reaches the receiver. Retain this as an alternative physical solution, not a failed game implementation or a valid negative. The displaced-inlet case above supplies the clear negative control.

## Moving pusher, trampoline and rope regressions

These additional cases use Balanced (0.45). Each completed with verified typed connections, zero errors and exact Run/Reset. Body traces retain the original identities.

| Case | Observation |
| --- | --- |
| flight-pusher-endpoints-v1 | Four electrical links: separate supply/extend command and both endpoint outputs to visible gates. Cargo moves from X=-0.4 to X=3.245346 on the platform; the pusher is extended, home gate closed and end gate open in the 7.09-second screenshot. |
| flight-pusher-no-supply-v1 | Only extend command is wired. Cargo stays at X=-0.3999939, velocity zero; pusher stays retracted. |
| flight-trampoline-receiver-v1 | Passive bed at (0,3,0), -30° Z sends the ball from Y=6.999069 into the receiver at (5.4,1.195014,0). At tick 948 it remains inside at (5.2551727,1.1600494,0), almost at rest; screenshot confirms containment. No connections or explicit workshop goal. |
| flight-trampoline-missed-v1 | Same bed/receiver, ball offset to Z=2. It misses the bed and settles on the floor at (0,-0.11999512,2), zero velocity, receiver empty. |
| flight-counterweight-v1 | Campaign level 19 wins at tick 93. The four-unit weight descends as the one-unit load rises into the switch, lighting the lamp. Three rope links and switch→lamp activation are verified. Actual rope lengths are 5, 4 and 0.8217652; the last is the UI-created length, not the recipe's reference 0.81203175. |
| flight-counterweight-no-rope-v1 | Final pulley→weight rope omitted. No goal event through tick 948; both weights settle on the floor and lamp remains dark. |

The first four runs observe approximately eight seconds, rather than the entire 30-second timeout. Positive trampoline containment is an observed physical outcome, not a fabricated campaign win.

## Verification scope

These focused regressions cover the previously affected spring/basket, pipe/bend, pusher, trampoline and pulley trajectories. Separate native tests now cover callback mutation/count and explicit non-convergence; the representative feeder timing check runs near wall-clock speed. See [cannon verification](cannon-verification.md) for final release evidence. Arbitrary stacks, very large machines, mobile performance and the exhaustive campaign/difficulty matrix remain project work; these checks do not establish those broader claims.
