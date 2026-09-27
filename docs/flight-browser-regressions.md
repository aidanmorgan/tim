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

## Wound-spring shared-contact replay — 28 September 2026

Replayed through real UI controls on the uncommitted launcher/contact worktree over `0ede1e1bd72d7b5d929a4c7e26dee5e5a0c618c0`, after a fresh diagnostic export. Both attempts use Free workshop, Balanced difficulty and a 1440×900 viewport. No game-state setters or numeric placement menus were used.

- `wound-spring-regression-trampoline-v1`: the −30° bed sends the original ball into the basket. At tick 948 its position is (5.2551727, 1.1600494, 0) and velocity (−0.0036621094, 0, 0). The outcome screenshot visibly confirms containment. This is an observed workshop outcome, not a campaign win event.
- `wound-spring-regression-trampoline-missed-v1`: the same setup with the ball offset to Z=2 misses the bed and basket, settling at (0, −0.11999512, 2) with zero velocity through tick 948.

Each retains 100 diagnostic frames, zero console errors and exact Run/Reset construction equality. Recipes are appended to the linked recipe file; complete local logs and adapter source are retained under `docs/playtest-results/<caseId>.json`, with motion and outcome screenshot paths. These replays refresh trampoline/basket evidence only; other shared-flight browser regressions remain pending for this prototype.

Tested source SHA-256:

- WorldFlight: `2e97bc2ba402033564cace640371094887151621c171ce443f9ce1852f53e9d9`
- MachineWorld: `1cbedd73146b8db0dc16468493563c3e654b92d34d4f38036d9239e2195cfcf0`
- BodyContact: `fcb6267ce8c12ca2df6046b98c95e0e7bec7747b1ac3503edbe8707455eafbdd`
- BasketPart: `f671b9e8eec214d113e2d3347617a7316515cca66da9385de5a20da2d499756e`

Freshness note: the later rotated-launcher tests required a near-tangent roundoff correction in `MovingSphereSweep`. The browser replays above predate that correction and must be refreshed against the new diagnostic export; retain them as historical evidence.

## Counterweight replay after oblique-contact correction

On the uncommitted launcher worktree over `0ede1e1bd72d7b5d929a4c7e26dee5e5a0c618c0`, using MovingSphereSweep SHA-256 `e4d7c72b73997c62986266ce1c8006494adfa316c3c7a2262a648d62fc561774`:

- `wound-spring-regression-counterweight-v1`: level 19, Balanced. Actual palette placement and four verified links (three ropes plus switch → lamp) win at tick 93. The outcome screenshot confirms the raised load and lit lamp. Twenty-four diagnostic frames and exact Reset are retained.
- `wound-spring-regression-counterweight-no-rope-v1`: omits only the final pulley → weight rope. No goal event through tick 948. Both weights settle with zero velocity; the load is at (−2,−0.13999939,0.11999512), the placed weight at (2,0.04801941,0.1000061). One hundred frames and exact Reset are retained.

Both have zero console errors. Recipes are appended to the linked recipe file; local logs include actual actions, adapter source, configurations and screenshot paths. These refresh this pulley/weight/activation interaction only, not arbitrary rope networks or the other pending shared-flight cases.

## Springboard and pipe replays after oblique-contact correction

Same worktree/export and MovingSphereSweep hash as the preceding counterweight replay. Playwright selects Forgiving through the visible menu, then places/moves/rotates each part through the actual toolbox and gizmos. No state setters or solution imports.

| Case | Observed result |
| --- | --- |
| wound-spring-regression-springboard-v1 | Level 4 wins at tick 265; 43 diagnostic frames. |
| wound-spring-regression-pipe-v1 | Level 25 wins at tick 307; 46 frames. |
| wound-spring-regression-bend-v1 | Level 27 wins at tick 251; 41 frames. |
| wound-spring-regression-springboard-wrong-aim-v1 | +20° requested aim sends the ball left and out of bounds near (−11.329773,−5.0024567,0), with no goal event. |
| wound-spring-regression-pipe-missed-v1 | Pipe inlet displaced to X=3; ball misses and settles at (−1.3999939,−0.11999512,0), zero velocity, no goal event. |
| wound-spring-regression-bend-missed-v1 | Bend inlet displaced to X=3; ball settles at (0.7028961,−0.11999512,0), zero velocity, no goal event. |

The three negative attempts retain 100 frames each through tick 948, not the full timeout. All six have zero console errors and exact Run/Reset construction equality. Recipes and complete local logs (including adapter source, actual construction and screenshot paths) are retained under these case IDs. This refreshes these component trajectories, not repeated all-difficulty campaign coverage.

## Trampoline and pusher replays after oblique-contact correction

Same unchanged worktree/export and query hash as the counterweight/springboard/pipe replays above. All four attempts use actual UI controls in Free workshop, Balanced.

- `wound-spring-regression-trampoline-v2`: ball remains inside the basket at (5.2551727,1.1600494,0), velocity (−0.0036621094,0,0), at tick 948.
- `wound-spring-regression-trampoline-missed-v2`: ball offset to Z=2 misses the bed and settles at (0,−0.11999512,2), zero velocity, at tick 948.
- `wound-spring-regression-pusher-v1`: four typed electrical links verified. The cargo travels from X≈−0.4 to X=3.245346 on the platform. Outcome screenshot shows the rod extended, home-output gate closed and end-output gate open. 101 frames.
- `wound-spring-regression-pusher-no-supply-v1`: only the extend command is wired. Cargo stays at X=−0.3999939, with zero final velocity.

The other three attempts each retain 100 frames. All four have zero console errors and exact Run/Reset construction equality. Logs retain actual transforms, links, body traces, adapter source and screenshots; recipes are appended. These are focused regressions, not exhaustive supported-mode or difficulty tests.

## Cannon and conveyor replays after oblique-contact correction

Same worktree/export/query hash as the preceding current-query checks. All four use Free workshop, Balanced and real UI construction. Each retains 101 frames, zero console errors and exact Reset.

- `wound-spring-regression-cannon-v1`: six parts, three typed links. One accepted shot releases approximately 45 energy, sends the existing payload to sampled Y=7.251709, and later returns to ready under continued supply.
- `wound-spring-regression-cannon-no-supply-v1`: omits only battery → cannon. Two trigger links remain. Trigger result is uncharged, shot count and energy stay zero; payload settles in the cannon at Y=2.7900085.
- `wound-spring-regression-conveyor-v1`: five parts, four typed links through the powered clutch. Conveyor input reaches 6 rad/s and 20 torque; the ball is carried right and eventually leaves the workbench.
- `wound-spring-regression-conveyor-open-clutch-v1`: omits battery → clutch, retaining three links and a turning motor. Conveyor speed, torque and available work remain zero; ball settles at (1,3.4600067,0) with zero velocity.

Full logs retain adapter source, actions, actual links/transforms, frames and screenshot paths; recipes are appended. This completes the planned representative shared-flight replay set for the launcher change (springboard, pipe/bend, pulley, trampoline, pusher, cannon and conveyor with controls). It does not establish every supported mode, arbitrary assemblies, mobile performance or the full campaign/difficulty matrix.

## Evidence freshness after stacked-contact performance correction

The latest uncommitted shared-flight revision adds `BodyContactConvergence` and immutable geometry snapshots reused only during velocity-only, zero-time contacts. It retains full time consumption and invalidates snapshots after time advances, overlap repair, surface callbacks and motion-limit events. All **1,139 native tests** pass, including first/tiny impact and cache-invalidation controls.

Current browser proof is the single/stacked spring timing pair `wound-spring-single-timing-v3` / `wound-spring-stacked-timing-v3`: zero console errors and exact Reset; supported-stack simulation pacing improves from approximately 9 wall seconds per simulated second to approximately 1. See [launcher verification](wound-spring-verification.md) for retained intermediate failures, measured intervals, recipes and source hashes.

The preceding shared-flight positive/control replays were current for the earlier near-tangent query fix, **not yet refreshed for this convergence/snapshot revision**. Re-run the affected representative set (springboard, pipes/bends, pulley, trampoline, pusher, cannon and conveyor), plus affected launcher modes, before publishing the component as verified. Do not overwrite the historical artifacts or present native tests as replacement browser proof.

## Completed convergence/snapshot regression replay

Rebuilt the diagnostic export and replayed all **16 representative positive/control constructions** through actual UI controls on the unchanged uncommitted worktree over `0ede1e1bd72d7b5d929a4c7e26dee5e5a0c618c0`. The shared solver sources remained unchanged throughout:

- WorldFlight: `9f537623db965f933fcce15f45bfd4b0387bdd2c9d52fab0ff933491d6be73e9`
- WorldGeometry: `b7c9058ea536bc8e3fa1d572a10f3adb7bd5a27fefc81323d4b2d56f011465eb`
- BodyContactConvergence: `c24b7f23ba4bb66518a9460037adf5b25aaa30842f71b4dd9e011b42c1f697c3`

All 16 have **zero console errors and exact Run/Reset equality**. The actual Run construction in each is identical to its earlier `wound-spring-regression-*` baseline. At all **1,358 common diagnostic samples**, the complete bodies, mechanical and cannon fields also match exactly. This is sampled-state comparison, not proof of every intervening render frame.

Each case ID below is `contact-cache-regression-<suffix>-v1`. Full recipes are appended to [the recipe file](flight-regression-recipes.json).

| Positive suffix / control suffix | Positive observation | Control observation |
| --- | --- | --- |
| counterweight / counterweight-no-rope | Level 19, Balanced: win at tick 93; rising load, descending counterweight and lit lamp. Four verified links. | Three links, final pulley-to-weight rope absent; no win through tick 948, both loads settled on the floor. |
| springboard / springboard-wrong-aim | Level 4, Forgiving: win at tick 265. | Requested +20° instead of −20°; no win through tick 936, ball escapes left. |
| pipe / pipe-missed | Level 25, Forgiving: win at tick 307, ball in receiver. | Inlet displaced to X=3; no win through tick 948, ball rests at (−1.3999939,−0.11999512,0). |
| bend / bend-missed | Level 27, Forgiving: win at tick 251. | Bend displaced to X=3; no win through tick 948, ball rests at (0.7028961,−0.11999512,0). |
| trampoline / trampoline-missed | Workshop, Balanced: ball remains in receiver at (5.2551727,1.1600494,0), X velocity −0.0036621094, tick 948. | Z=2 drop misses bed and rests on the floor at (0,−0.11999512,2), zero velocity. |
| pusher / pusher-no-supply | Workshop, Balanced: four electrical links, cargo reaches X=3.245346; extended rod, closed home gate and open end gate verified visually. | Only extend command wired; no power supply. Cargo remains at X=−0.3999939 with zero velocity through tick 960. |
| cannon / cannon-no-supply | Workshop, Balanced: three links, one shot, released energy 44.9999966, sampled payload peak Y=7.251709, then recharge. | Trigger path intact but no supply: uncharged result, zero shots/energy, payload rests at Y=2.7900085. |
| conveyor / conveyor-open-clutch | Workshop, Balanced: four links, conveyor input 6 rad/s and 20 torque; payload transported right and eventually off the bench. | Three links, no clutch supply. Motor still turns at 6 rad/s but conveyor speed/torque/work remain zero; payload rests at (1,3.4600067,0). |

Frame counts in table order (positive/control): **24/100, 43/99, 46/100, 41/100, 100/100, 101/101, 101/101, 101/101**. Negative observations last approximately eight seconds, not the full timeout. Workshop physical outcomes are not fabricated campaign win events.

Complete logs retain adapter source, actual UI actions/configurations/links, frames, screenshot paths and source hashes at `docs/playtest-results/<caseId>.json`. The comparison audit is `docs/playtest-results/contact-cache-regression-comparison-v1.json`. Positive outcome/motion images were inspected for the pulley, springboard, pipe, bend, trampoline, pusher, cannon and conveyor. Historical logs remain unchanged.

This completes the **representative shared-flight** refresh after the convergence/snapshot changes. Launcher-mode replays, continuous launcher animation review and other component-specific acceptance work remain before publication. This does not prove all catalogue modes, arbitrary machines, mobile performance or the full 75-level/difficulty campaign.
