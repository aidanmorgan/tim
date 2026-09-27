# Browser campaign playtest

## Spring activation feedback — 54-level draft

Campaign hash remains `2691ed54858909a421ef9b89f0c45596f7051f7bb07d19f1a8c84a2b3348e371`. The spring now has a continuous silver helix and a moving gold plate, with bounded compression/rebound and settling on each accepted impact. It retains the original instantaneous launch and collision proxy. Presentation runs independently of simulation so it can settle after success; Reset reconstructs the resting spring.

UI-only `L04-{balanced,forgiving,precise}-reference-spring-v1.json` win at ticks 277/265/302 respectively; `L07-balanced-reference-spring-v1.json` wins at tick 206. All four record zero browser errors, Run/result/Reset and passing audits. The level-4 Balanced run also records sixteen actual browser frames at roughly 90 ms intervals; reviewed frames 3/6/7/8/9 show rest, compression, extension and settling around the launched ball. The helix is partially hidden by the plate from the default camera. These sampled captures are not a frame-rate or complete continuous-fluidity measurement.

All 263 native tests pass, including new tests for three-axis orientation, unchanged launch/collision state, cooldown, overlapping impacts without pose jumps, active-animation Reset, settling after simulation stops and render-step agreement. The full repeated difficulty/error matrix remains incomplete.

## One-shot delay — 54-level draft

Campaign hash: `2691ed54858909a421ef9b89f0c45596f7051f7bb07d19f1a8c84a2b3348e371`. Lessons 23–24 introduce a one-second delayed activation command, first to a lamp and then to a torch/solar-panel/motor circuit. The C# module has typed Ready/Counting/Finished states, a clockwise countdown hand and Reset. It supplies commands, not electricity. Authors can set 0.1–12 seconds; player adjustment and rearming pulses are not implemented.

The initial UI batch exposed a goal-authoring defect: level 24 referred to the authored `delay_1` slot, but real placement created `delay_2`. All three reference difficulties timed out despite the working circuit. Those v1 records remain unchanged at historical hash `6ba2bb2bf38cdc577be650744e382e84d30f31fba7bc89a37d1e07d4b05338f7`. Goals now measure a minimum elapsed time from the fixed trigger, not the ID of a player-created part; native regression tests explicitly rename the placed timer.

Current UI-only `L23-{balanced,forgiving,precise}-reference-delay-v2.json` cases win at tick 207; the corresponding L24 cases win at tick 333. `L23-balanced-early-bypass-delay-v2.json` correctly times out at tick 3600. All seven record zero browser errors, a complete Run/result/Reset lifecycle and passing sampled-state/property/connection audits. Reviewed screenshots retain the palette and show the round timer and its icon; they do not prove continuous animation fluidity.

All 259 native tests and 26 UI-adapter tests pass. Native coverage includes exact deadlines, ignored repeat inputs, Reset, chained delays, invalid durations, arbitrary player IDs, missing links and early bypass rejection. These targeted checks are not the full repeated difficulty/placement-error matrix, nor verification of all 54 levels through the browser.

## Soft torch cone — current 52-level campaign

The straight axial marker is removed. A C# render-time mesh draws four translucent warm-cream shells, each sampled at 48 directions, using the physical emitter angle/range and the same opaque collision proxies. Source rotation affects both illumination and visible geometry. Adjacent sectors use their nearer sampled hit distance to limit connecting triangles across abrupt silhouette changes. This is approximate sampled clipping, not volumetric scattering; moving partial shadows and multi-source frame performance still need broader review.

Campaign hash remains `0121fc4a7704fac68d2b8652f71d50779ecd25ccdda68b00cd7f07b3efff0a43`. UI-only `L21-balanced-reference-cone-v1.json` and `L22-balanced-reference-cone-v1.json` win at tick 207. `L22-balanced-shaded-cone-v1.json` and `L22-balanced-missing-wire-cone-v1.json` correctly time out at 3600. All four have zero recorded browser errors, one Run/result/Reset lifecycle and passing audits. Reviewed screenshots show a soft widening cone reaching the lit panel or stopping at the wall, with the existing palette and no new UI.

All 240 native tests pass. New geometry checks cover cone angle/range, three source orientations, wall clipping, partial dynamic obstruction and restored unblocked rays when the blocker moves. These tests do not establish continuous rendered fluidity or complete optical fidelity. The earlier straight-marker captures and the solar Reset failure remain unchanged historical evidence.

## Flashlight and solar power — 52-level draft

Current campaign hash: `0121fc4a7704fac68d2b8652f71d50779ecd25ccdda68b00cd7f07b3efff0a43`. Lessons 21 (“A little sunshine”) and 22 (“Out of the shade”) introduce a self-contained impact-triggered flashlight, a directional solar panel and electrical supply to a motor. The second lesson places an opaque wall across the beam. Initial `solar-v1` Balanced references won, but their screenshots showed the panel's back and a hidden meter. Both lesson layouts were subsequently turned 180°; those earlier captures remain historical evidence of hash `f82820a110fca24fecb205541624ee047c307e502dbc52ae80e921951d646b2a`.

Eight current-layout UI-only attempts use real palette clicks, three-dimensional translation/rotation handles, contextual Connect, Run and Reset:

| Capture in `docs/playtest-results/` | Result |
| --- | --- |
| `L21-{forgiving,balanced,precise}-reference-solar-v2.json` | All won at tick 207 |
| `L22-{forgiving,balanced,precise}-reference-solar-v2.json` | All won at tick 207 |
| `L22-balanced-shaded-solar-v2.json` | Expected timeout at tick 3600 with panel beyond the wall |
| `L22-balanced-missing-wire-solar-v2.json` | Expected timeout at tick 3600 without panel-to-motor wiring |

All eight have zero recorded browser errors, one Run/result/Reset lifecycle and passing current sampled-motion/property/socket audits. The reviewed level-22 outcome now shows the blue panel cells, gold meter marks and flashlight lens on the camera-facing side of the wall, with the existing palette and no additional toolbar.

Six additional matched-error attempts use `L21-{forgiving,balanced,precise}-beam-edge-{102,110}-solar-v2.json`. Actual smaller-error starts are identical at [-1, 3, 1.0060782]; Forgiving ends at depth 0.75607824 and Balanced at 0.9060782, both winning at tick 207. Precise retains depth 1.0060782 and times out at 3600. Larger-error starts are identical at [-1, 3, 1.0860776]; Forgiving corrects to 0.8360776 and wins at 207, Balanced corrects to 0.98607755 but times out, and Precise stays unchanged and times out. These observations demonstrate distinct successful placement ranges for these fixtures, not complete coverage of all authored tolerances.

Five error records pass their sampled-motion/Reset audits. The larger-error Precise v2 record is **failed verification**: it records the expected timeout but the Reset click produced no Reset event before the observer deadline. Its screenshot still shows the unchanged timeout screen. A later separate UI click at the same control restored the scene and emitted `CCRESET`; the failed evidence was not amended. This does not establish the cause of the missed input or prove reliability. The separate `L21-precise-beam-edge-110-solar-v3.json` replay subsequently recorded the expected timeout and a complete Run/result/Reset lifecycle with zero browser errors; its audit passes. It does not erase the v2 failure.

All 236 native tests pass, including current 52-level references at three difficulties and six controlled solar-boundary cases. At an exact 1.02-unit depth error, Forgiving/Balanced win and Precise fails; at 1.1, only Forgiving wins. Impact thresholds remain .8 in every controlled case, isolating placement assistance rather than easier trigger physics. These are native boundary fixtures, not claims that UI drags land at exactly those offsets. Together with the 14 accepted current-layout UI captures and the retained Reset failure, they provide targeted evidence rather than full campaign acceptance.

The generator matches current content, and the diagnostic browser build was published locally. Visible torch light remains a temporary straight axial marker: the owner's widening, softly clipped cone is explicitly unfinished in TODO.md. The simulation samples nine points within a finite illumination cone; it has no calibrated lumens, reflection/refraction, storage or ambient-sky supply. Original TIM equivalence, full campaign difficulty coverage, continuous rendered-fluidity checks and browser Save/Load remain unproven. Changes are local, not deployed to Pages.

## Weights, fixed pulleys and ropes — 50-level draft

That stage’s campaign hash: `d4c81b09d5628d1fc0881f20bb3fc72ed1a17d5d9afaef16c00d60de550fd196`. New lessons 19 (“A helping weight”) and 20 (“Around the corner”) teach unequal counterweights and a pulley route extending into depth. The switch is mounted upside down so the rising load hits its button face. Earlier `ropes-v1` captures use the pre-rotation hash and remain historical evidence, not current acceptance.

The UI driver places parts with palette clicks and 3D handles and ties ropes using the contextual Connect action. Rope length is measured from actual socket positions when the player connects; recipe lengths do not set game state. Reset audits now compare explicit rope lengths and canonical undirected endpoints. The audit uses the shared C# connection-domain enum and rejects invalid domains, even when both Run and Reset contain the same invalid value.

| Capture in `docs/playtest-results/` | Result |
| --- | --- |
| `L19-{forgiving,balanced,precise}-reference-ropes-v2.json` | All won at tick 93 |
| `L20-{forgiving,balanced}-reference-ropes-v2.json` | Both won at tick 92 |
| `L20-precise-reference-ropes-v2.json` | Won at tick 93 |
| `L20-balanced-missing-rope-ropes-v2.json` | Expected timeout at tick 3600 with the middle pulley-to-pulley span omitted |

All seven captures contain one Run/result/Reset lifecycle, zero reported browser errors and passing current audits. The reviewed level-20 outcome screenshot shows cream/gold pulleys, blue banded weights and warm rope within the existing minimal interface and palette. Incomplete routes are drawn dashed and carry no tension.

Six additional UI-only captures, `L20-{forgiving,balanced,precise}-{near-positive,near-negative}-pulley-ropes-v2.json`, perturb the movable pulley through actual handles rather than moving the dynamic weight. Positive attempts win at ticks 94/94/93 and negative attempts at 91/91/93 (Forgiving/Balanced/Precise). All six have zero recorded browser errors, complete Run/result/Reset lifecycles and passing audits.

Positive attempts share the exact observed start [2.0926993, 5.9916, 2.043662], quaternion [0, 0, 0.018233724, 0.99983376]. Forgiving reaches [2, 6, 2] and effectively zero rotation; Balanced reaches [2.0025344, 5.99977, 2.0011938] with quaternion Z 0.000781438; Precise retains its starting transform. Negative starts are [1.9357867, 5.9815993, 1.9521961]; both assisted settings reach the authored position while Precise retains the error. This directly observes different bounded placement corrections. Since every attempt wins, it does **not** establish a wider successful placement region or isolate assistance from the rope length measured at connection time.

Important scope: the sampled correction audit observes **zero** parts in level 19 (the placed weight is dynamic) and **one** in level 20 (the movable pulley). It verifies connection/property/transform restoration for the complete setup, but it does not measure dynamic weight trajectories or continuous rendered fluidity. Reference wins at every difficulty alone do not establish an expanded successful placement region.

Native coverage includes unequal/equal counterweights, order independence, slack and inward motion, open routes, graph rejection, invalid masses, floor constraints, pendulum energy bounds and Reset. All 218 native and 26 adapter tests pass; the generator matches current content and the diagnostic web publish succeeds. The model uses ideal fixed point guides, not moving pulley blocks, obstacle wrapping, self-collision, cutting, friction or pulley inertia. Original TIM physics equivalence, browser persistence, full repeated difficulty coverage and the 75-level target remain unproven. These changes are local and not deployed to Pages.

## Mechanical relay and reversing transmission — 48-level draft

That stage’s campaign hash is `300dbcadd3e69e17e563d17a749032ce9136f01de2a7acf7b58fba7d1470e039`. Electrical lessons now precede the first conveyor: battery/motor 14, switched supply 15, conveyor 16, relay 17 and reversal 18. Old numbered captures remain immutable evidence of their own content hashes.

The driver now permits explicit `mechanical: drive -> drive_in` recipes. These use the ordinary contextual Connect icon and visible part clicks; no machine imports, variable setters or numeric placement menu. All records below contain one Run/result/Reset lifecycle, zero reported console errors and passing current sampled-motion/property/socket Reset audits.

| Capture in `docs/playtest-results/` | Result |
| --- | --- |
| `L16-balanced-reference-mechanical-v1.json` | Won at tick 281 |
| `L17-balanced-reference-mechanical-v1.json` | Won at tick 281; motor → fixed conveyor → placed conveyor |
| `L18-balanced-reference-mechanical-v1.json` | Won at tick 281; motor → reverse transmission → conveyor |
| `L17-balanced-reference-mechanical-animated-v2.json` | Won at tick 281 after adding moving belt artwork |
| `L18-balanced-reference-mechanical-animated-v2.json` | Won at tick 281; outcome screenshot reviewed |
| `L18-balanced-missing-drive-mechanical-v2.json` | Expected timeout at tick 3600 with transmission → conveyor omitted |
| `L18-balanced-reference-mechanical-final-v3.json` | Won at tick 281; final short-label/continuous-marker screenshot reviewed |

The reviewed reversal screenshot retains the established palette and minimal interface, with opposite-direction gear wheels and double-strand mechanical belts. It exposed an overlong inventory label, subsequently shortened to “Reverse gear” without changing the `reverse_transmission` part identity. A subsequent visual refinement makes witness marks traverse the closed belt ends continuously instead of wrapping along separate strands. The final-v3 replay passes its audit and the reviewed screenshot confirms the full “Reverse gear” label fits.

Native tests additionally cover all three difficulties, each required-link omission, wrong-direction bypass, source loss/coasting, two reversers restoring direction, ordering, fan-out rules, rejected loops/competing inputs, and Reset. The campaign generator matches the checked-in content. This does **not** complete repeated browser difficulty trials, frame-by-frame rendered-fluidity verification, current-schema persistence or the 75-level target. Shaft-speed transfer is ideal; torque, load sharing, belt slip and chain-specific mechanics remain unimplemented. Changes are local, not deployed to Pages.
## Switched circuits, power lessons and enum contracts — 27 September 2026

Current campaign: 46 levels, hash `9f0e32a24c249a8ac34febb16c458fecd649cf64d9c5f3a82f10ca849c5974a0`. Levels 15–16 teach a supplied motor, then ball-triggered supply through an impact switch. The motor must complete one shaft revolution. Level 16 additionally requires its first supplied-power event to occur after the switch trigger; native direct-wire bypass controls fail even though both the unrelated switch and motor activate.

Supply now traverses explicitly closed contacts from actual sources, with a per-tick snapshot and simultaneous assignment. Native tests cover reversed part/link ordering, multi-switch chains, cycles without source power, source loss/restoration, opened contacts, removed wires and Reset/replay. These are binary supply rules, not voltage/current, battery depletion or mechanical load simulation.

Connection domains and goal kinds now use C# enums with strict string-enum JSON conversion. Runtime events have typed kind/target/body keys, and built-in sockets use centralized identifiers. Unknown string names and numeric enum inputs are rejected; no old-name aliases were added. Resource/instance IDs remain extensible. All 173 native tests pass, the web publish succeeds, and the standalone generator matches current data after negative-zero normalisation.

Browser evidence:
- `L15-balanced-reference-direct-switched-supply-reset-enum-v3.json`: win at tick 146.
- `L16-balanced-reference-direct-switched-supply-reset-enum-v3.json`: win at tick 248.
- Both use real palette, movement, contextual Connect, Run and Reset; zero recorded browser errors, complete lifecycles, reviewed outcome screenshots and passing sampled-motion/property/socket Reset audits.
- Earlier `-v1` runs passed before the sequencing goal was strengthened; retain them as historical. `L15-...-v2` also won; `L16-...-v2` stopped during construction with `Timed out: requested puzzle` and no Run. Its failed record is preserved. The fresh enum-v3 attempt succeeds, but this does not establish a fix for intermittent selector navigation.

Full repeated difficulty coverage, browser bypass/missing-wire controls, continuous shaft-motion review, mechanical drive transmission and browser Save/Load remain outstanding. Goal observers now report selection of fixed fixtures too, allowing UI-only linking from the fixed battery without exposing writable game state.
## Battery/motor free-workshop probe — 27 September 2026

Added two real C# part scenes and icons without changing the existing colour scheme. Direct battery supply reaches the motor's electrical input; activation commands cannot replace it. The motor visibly indicates supply and accelerates/coasts its shaft. A mechanical output socket is declared, but downstream belts/chains and torque/load transmission are not implemented.

`workshop-battery-motor-direct-supply-reset-v1.json` uses only palette clicks, three-axis handles, contextual Connect, Run and Reset. It records one electrical edge from `battery_1:supply` to `motor_2:power_in`, preserved after Reset, with zero browser errors. The reviewed outcome screenshot shows the new icons, coral battery, teal motor, lit indicator and navy cable. Free workshop is currently selector entry 45; its 3600-tick diagnostic timeout is expected because it has no puzzle goal, **not** a successful level or a failed authored solution.

All 158 native tests pass, including four connected/enabled combinations, source-loss coast-down, restored supply, disconnect clearing and Reset. The enlarged toolbox correctly reaches its screen-height limit and scrolls overflow; its previous test incorrectly assumed every catalogue always fits without scrolling and now checks both content and viewport limits. All 19 JavaScript adapter tests pass.

The campaign remains 44 levels. Next: switched circuits, motor-driven mechanisms, progressive power lessons, real-browser unpowered controls and sampled shaft-motion review. Do not claim this single screenshot proves continuous fluidity, browser save persistence or full power-system acceptance.
## Forward-only connection schema — 27 September 2026

Per the owner's policy, remove compatibility paths instead of maintaining old save formats or implicit connection sockets. The 44-level draft now has explicit activation links; campaign hash is `717ead37ed63550b6193ca3d960d326dfc8633d3ce24ea6a92cb3f76d7f502cc`. Activation commands are separate from forthcoming electrical/signal/mechanical/rope domains. Version-1/2 and missing-version saves are rejected; current saves use version 3 and stable puzzle IDs.

UI-only `L02-balanced-reference-direct-explicit-sockets-reset.json` places the switch, links the lamp and wins at tick 116 with zero browser errors and one Run/result/Reset lifecycle. Reviewed outcome screenshot shows the lit lamp and unchanged approved colours. Both observed connection snapshots contain type `activation`, source socket `activation_out` and target socket `activation_in`. The independent sampled-motion/property/socket Reset audit passes.

The first audit rejected these valid sockets because it expected snake_case recipe keys instead of the browser observer's camelCase keys. Corrected the checker and its fixtures to use the actual diagnostic contract; re-audited the original unchanged capture successfully. No dual-name compatibility fallback was added.

Native forward-refactor suite: 153 passing tests, including all 44 reference solutions at three precisions, explicit socket validation/Reset and obsolete-save rejection. The campaign generator matches checked-in content after numeric negative-zero normalisation. New-family UI, electrical supply simulation, browser save persistence and full repeated difficulty coverage remain pending; this regression is not a completed campaign matrix.
In progress, 2026-09-27. All 40 levels have completed a direct-UI Balanced reference run; the full difficulty matrix remains incomplete.

**Revised user requirement:** direct 3D UI manipulation, no numeric placement/layer menus, and multiple attempts at every difficulty on all 40 levels. The 24 reference completions below do not satisfy that requirement. The replacement runner and 480-run minimum matrix are described in [tools/Playtest/README.md](../tools/Playtest/README.md).

Direct-run pilot evidence is stored in `docs/playtest-results/`. The first eleven direct-UI attempts recorded these outcomes: all nine level-1 reference/positive/negative cases won; level-1 Forgiving outside-window timed out; level-2 Balanced reference won at tick 116 (0.97 s), including actual wiring clicks. The outside-window ramp stayed at [0.60382605, 8.290041, 0] with an unchanged quaternion across ticks 0–120, while the eligible second ramp corrected. Screenshots accompany the reviewed pilot outcomes; per-case JSON records contain actual input actions and sampled transforms. This is partial evidence, not the full 480-case matrix or a completed bounds/easing/reset audit.

 Chrome through Playwright MCP, 1440×900, local published C# Godot/2dog build at http://127.0.0.1:8060. Default Balanced precision (0.45), surface-friction option off.

## Superseded reference method

Use actual palette, placement, move/rotate handles, optional front-view/layer controls, keyboard rotation shortcuts, wiring and Run/Reset. No injected game state, solution loading, simulated win events, or developer auto-solve. Reference placements guide this QA pass; it is not a blind puzzle-discovery test. A completed row requires visible “It works!” feedback. Native tests do not count as browser playthroughs.

## Reference-pass results (not difficulty-matrix evidence)

| Level | Puzzle | Browser result | Simulation time | Notes |
| --- | --- | --- | --- | --- |
| 1 | First principles | Completed | 2.95 s | Placed two ramps in 3D, tilted with E, lifted/lowered using green movement handle. |
| 2 | A little lightbulb moment | Completed | 0.97 s | Placed and lowered impact switch, connected to lamp; falling bowling ball activated it. |
| 3 | Air mail | Completed after retries | 1.86 s | Fan-only route. Height 3.795195 failed; front-view drag snapped to 3.8 and succeeded. See defect below. |
| 4 | Spring forward | Completed | 2.31 s | Front-view spring placement, four 5° tilts, successful launch into elevated basket. Spring lacks visible compression/rebound. |
| 5 | A different perspective | Completed | 1.18 s | Set depth −3, placed fan, quarter-turn around Y; moved ball from back to front receiver. Front view obscures depth; 3D view should be used when judging the route. |
| 6 | The domino effect | Completed | 1.06 s | Four placed dominoes transmitted the trigger to the end fixture. |
| 7 | Conveyor courier | Completed | 2.34 s | Conveyor delivered the ball into the receiver. |
| 8 | Spring-loaded signal | Completed | 1.72 s | Spring launched into a wired elevated impact switch; lamp lit. |
| 9 | A breath of electricity | Completed | 0.88 s | Fan drove tennis ball onto a wired switch; lamp lit. |
| 10 | Cold start | Completed | 1.86 s | Trigger ball powered initially-off fan through a placed/wired switch; receiver captured tennis ball. |
| 11 | Two deliveries | Completed | 2.95 s | Two separate depth lanes: ramp route and fan route. |
| 12 | Bounce and roll | Completed | 2.95 s | Ramp and spring deliveries. |
| 13 | Air and belt | Completed | 2.34 s | Fan and conveyor deliveries. |
| 14 | Catch and signal | Completed | 2.95 s | Ramp capture plus wired impact switch. |
| 15 | Wind and dominoes | Completed | 1.86 s | Fan capture plus four-domino chain. |
| 16 | Powered post | Completed | 2.95 s | Trigger-powered fan plus ramp delivery. |
| 17 | Spring and chain | Completed | 2.31 s | Spring delivery plus domino chain. |
| 18 | Two signals | Completed | 2.29 s | Two independently wired switches/lamps; wired in 3D to disambiguate depth. |
| 19 | Belt and signal | Completed | 2.34 s | Conveyor capture plus fan-triggered lamp. |
| 20 | Start and topple | Completed | 1.86 s | Trigger-powered fan plus domino chain. |
| 21 | Deep routes | Completed | 2.95 s | Four ramps rotated around Y into opposite depth directions. |
| 22 | Cross breezes | Completed | 1.86 s | Opposing depth-axis fans. |
| 23 | Deep springs | Completed | 2.31 s | Two depth-axis spring launches. |
| 24 | Spatial signals | Completed | 2.29 s | Depth-axis ramps/fan and two independently wired lamps. |
| 25–40 | Remaining campaign | Not yet played | — | Must not be counted as passed. |

Screenshots: `.playwright-mcp/level-01-solved.png`, `level-02-solved.png`, `level-03-solved.png`, `level-04-run.png`, `level-05-solved.png`. Failed level-3 attempt: `.playwright-mcp/level-03-run.png`.

## Findings

- **Fan tolerance — reproduced defect repaired:** the direct runner reproduced a Balanced timeout at fan height 3.7841127. Fan instances now author a 0.15 s correction instead of 0.4 s, retaining the same quintic easing, eligibility windows and correction caps; Precise has no correction. Identical UI placement now wins at tick 223. Samples at ticks 0/4/8/12/16/20 show heights 3.7841127/3.7853267/3.790415/3.7966657/3.7998166/3.8. Positive and negative position/orientation perturbations win at both Forgiving (tick 201) and Balanced (tick 223). Six native height errors at each assisted difficulty also win and restore their original placements on Reset; all 63 tests pass. Full browser Reset, bounds and campaign tolerance coverage remain outstanding.
- **Progression:** trying the level selector immediately after success did not change the level in the first attempt; Reset then explicit popup selection worked. Recheck before diagnosing a disabled-control bug.
- **Animation request:** springboard activation needs compression, rebound and settling animation synchronized with launch; tracked in TODO.md.
- **Direct-run navigation:** a level-2 attempt timed out because the first Down key focuses popup row 1; it does not advance to row 2. Updated the driver to send one Down per requested row and verified level-2 selection/wiring/completion. Later rows and scrolling still need verification.
- **Automation:** wait for game startup before input; separate pointer movement/down/up and allow frames between operations. Popup selection is most reliable with visible item coordinates. Fast synthetic actions can obscure input ordering.
- **Front view:** useful for exact height/sideways placement, but overlapping depth lanes require deliberate layer selection and 3D inspection.

## Fan timing regression evidence

The artifact directory now contains 17 records (15 wins, two timeouts), across two puzzle-data hashes. `L03-balanced-reference-direct.json` preserves the failing pre-change run; `L03-balanced-reference-direct-fan-timing.json` preserves the successful identical-input post-change run. Four `L03-*-near-*-direct.json` records cover post-change perturbations, with matching reviewed screenshots. Counts include the historical regression pair and must not be treated as current-build matrix completion. Fan timing is authored in both `content/puzzles.json` and its C# campaign generator; the physics solver and easing formula were not altered.

## Direct-UI Balanced reference pass — all 40 levels

All 40 levels have now been completed through palette clicks, movement arrows, rotation rings, wiring endpoints and Run, with reviewed success screenshots and no recorded browser errors in these successful runs. These are authored-reference QA runs, not blind puzzle solving. Level 3 uses the post-fix fan-timing record. Level 36 first failed during construction at an exact boundary click; the driver now stages boundary parts 0.1 units inside and uses the actual movement arrow to reach the edge, after which the level completed.

| Level | Puzzle | Time | Input/result record |
| --- | --- | --- | --- |
| 1 | First principles | 2.95 s | [JSON](playtest-results/L01-balanced-reference-direct.json) |
| 2 | A little lightbulb moment | 0.97 s | [JSON](playtest-results/L02-balanced-reference-direct.json) |
| 3 | Air mail | 1.86 s | [JSON](playtest-results/L03-balanced-reference-direct-fan-timing.json) |
| 4 | Spring forward | 2.31 s | [JSON](playtest-results/L04-balanced-reference-direct.json) |
| 5 | A different perspective | 1.18 s | [JSON](playtest-results/L05-balanced-reference-direct.json) |
| 6 | The domino effect | 1.06 s | [JSON](playtest-results/L06-balanced-reference-direct.json) |
| 7 | Conveyor courier | 2.34 s | [JSON](playtest-results/L07-balanced-reference-direct.json) |
| 8 | Spring-loaded signal | 1.72 s | [JSON](playtest-results/L08-balanced-reference-direct.json) |
| 9 | A breath of electricity | 0.88 s | [JSON](playtest-results/L09-balanced-reference-direct.json) |
| 10 | Cold start | 1.86 s | [JSON](playtest-results/L10-balanced-reference-direct.json) |
| 11 | Two deliveries | 2.95 s | [JSON](playtest-results/L11-balanced-reference-direct.json) |
| 12 | Bounce and roll | 2.95 s | [JSON](playtest-results/L12-balanced-reference-direct.json) |
| 13 | Air and belt | 2.34 s | [JSON](playtest-results/L13-balanced-reference-direct.json) |
| 14 | Catch and signal | 2.95 s | [JSON](playtest-results/L14-balanced-reference-direct.json) |
| 15 | Wind and dominoes | 1.86 s | [JSON](playtest-results/L15-balanced-reference-direct.json) |
| 16 | Powered post | 2.95 s | [JSON](playtest-results/L16-balanced-reference-direct.json) |
| 17 | Spring and chain | 2.31 s | [JSON](playtest-results/L17-balanced-reference-direct.json) |
| 18 | Two signals | 2.29 s | [JSON](playtest-results/L18-balanced-reference-direct.json) |
| 19 | Belt and signal | 2.34 s | [JSON](playtest-results/L19-balanced-reference-direct.json) |
| 20 | Start and topple | 1.86 s | [JSON](playtest-results/L20-balanced-reference-direct.json) |
| 21 | Deep routes | 2.95 s | [JSON](playtest-results/L21-balanced-reference-direct.json) |
| 22 | Cross breezes | 1.86 s | [JSON](playtest-results/L22-balanced-reference-direct.json) |
| 23 | Deep springs | 2.31 s | [JSON](playtest-results/L23-balanced-reference-direct.json) |
| 24 | Spatial signals | 2.29 s | [JSON](playtest-results/L24-balanced-reference-direct.json) |
| 25 | Three deliveries | 2.95 s | [JSON](playtest-results/L25-balanced-reference-direct.json) |
| 26 | Triple signal | 2.29 s | [JSON](playtest-results/L26-balanced-reference-direct.json) |
| 27 | Cold front | 2.95 s | [JSON](playtest-results/L27-balanced-reference-direct.json) |
| 28 | Chain mail | 2.34 s | [JSON](playtest-results/L28-balanced-reference-direct.json) |
| 29 | Bounce mail | 2.31 s | [JSON](playtest-results/L29-balanced-reference-direct.json) |
| 30 | Relay workshop | 2.29 s | [JSON](playtest-results/L30-balanced-reference-direct.json) |
| 31 | Double cold start | 2.34 s | [JSON](playtest-results/L31-balanced-reference-direct.json) |
| 32 | Triple chain | 1.06 s | [JSON](playtest-results/L32-balanced-reference-direct.json) |
| 33 | Signals and chain | 2.29 s | [JSON](playtest-results/L33-balanced-reference-direct.json) |
| 34 | Cold signals | 2.29 s | [JSON](playtest-results/L34-balanced-reference-direct.json) |
| 35 | Depth delivery office | 2.94 s | [JSON](playtest-results/L35-balanced-reference-direct.json) |
| 36 | Depth telegraph | 2.29 s | [JSON](playtest-results/L36-balanced-reference-direct.json) |
| 37 | Double bridge | 2.34 s | [JSON](playtest-results/L37-balanced-reference-direct.json) |
| 38 | Bridges and signal | 1.72 s | [JSON](playtest-results/L38-balanced-reference-direct.json) |
| 39 | Cold bridges | 1.86 s | [JSON](playtest-results/L39-balanced-reference-direct.json) |
| 40 | The grand contraption | 2.29 s | [JSON](playtest-results/L40-balanced-reference-direct.json) |

Matching screenshots are in `.playwright-mcp/<caseId>.png`. There are 54 outcome records overall (52 wins, two timeouts), including historical and regression runs; this is not 54 completed current-build matrix cells. The full difficulty matrix, independent correction-bounds/easing audit, and browser Reset checks are outstanding.

Playability observations from this pass:

- Foreground ramps and receivers can obscure deeper parts, notably in the combined-route levels. Successful scripted selection does not prove beginner-friendly visibility.
- Three-item inventories require scrolling in the compact palette; scrolled rows can show clipped labels. The runner successfully used the actual scroll control, but discoverability/readability need review.
- Later levels mostly combine independent familiar routes. More simultaneous objectives is not proof of increasing reasoning difficulty or variety.
- Exact build-boundary clicks can be rejected by pixel rounding. The driver workaround avoids this, but a player-facing edge-placement improvement remains desirable.
- On success, the scene stops with some balls still moving and dominoes mid-fall; a short post-success animation period would make the result easier to read.

## Browser Reset and sampled-motion audit

Three fresh level-1 near-positive runs at Forgiving/Balanced/Precise include actual Reset-button clicks and read-only restored transforms. All won and passed the independent C# audit (`dotnet run --project tools/Playtest -- --audit docs/playtest-results/*-reset.json`). Outcome screenshots were reviewed; the Balanced restored screenshot confirms build mode. The audit compares all restored part transforms to run start, including the ball and fixtures.

Implemented checks also cover complete sample cadence, correction caps, zero assistance on Precise/outside all same-kind position windows, and sampled easing. Deliberately corrupted stdin copies verify rejection of excess motion and incorrect Reset transforms; missing Reset is reported incomplete. See the runner README for conservative rotation-bound and assignment-coverage limitations. This remains partial verification, not full nudge-system acceptance. The earlier 54-record count is historical; there are now 57 records.

All 63 native tests remain passing. Normal builds omit the added diagnostic calls. Anvil's pre-write gate required authentication during these edits and returned its allow-with-warning fallback; this was reported while proceeding.

## Levels 2–3: four attempts at every difficulty

Each row covers reference, near-positive, near-negative and outside-window direct-UI attempts, followed by an actual Reset click. All 24 outcome screenshots were reviewed; no browser errors were recorded. Files are `docs/playtest-results/L0{2,3}-<difficulty>-<variant>-direct-reset.json`, with `-outcome.png` screenshots under `.playwright-mcp/`.

| Level | Difficulty | Reference | Positive error | Negative error | Outside window |
| --- | --- | --- | --- | --- | --- |
| 2 | Forgiving | Won, tick 116 | Won, tick 116 | Won, tick 116 | Timeout |
| 2 | Balanced | Won, tick 116 | Won, tick 116 | Won, tick 116 | Timeout |
| 2 | Precise | Won, tick 116 | Won, tick 116 | Won, tick 116 | Timeout |
| 3 | Forgiving | Won, tick 201 | Won, tick 201 | Won, tick 201 | Timeout |
| 3 | Balanced | Won, tick 223 | Won, tick 223 | Won, tick 223 | Timeout |
| 3 | Precise | Timeout | Timeout | Timeout | Timeout |

All 24 pass the implemented sampled-motion/Reset checks. This validates their recorded bounds/easing/ineligibility/Reset observations, not full assistance correctness or puzzle playability. The audit's broader assignment and rotation-eligibility limits remain documented in the runner README.

**Precise usability finding:** all three level-3 reference attempts started the fan at [-4, 3.7841127, 0]. Assisted runs corrected to [-4, 3.8, 0] and won; Precise retained the initial transform and timed out. Small free-drag errors are consequential on this puzzle. A discoverable practical Precise placement path still needs verification/improvement; no hidden Precise correction was added.

The artifact total is now 81 (70 wins, 11 timeouts), including historical/repeated cases. There are 27 Reset-equipped records passing the current audit; [saved audit output](difficulty-audit-2026-09-27.json). Full 480-cell coverage remains incomplete.

## Precise placement: tenth-unit Shift alignment

Movement-arrow Shift snapping now aligns only the active world coordinate to 0.1 units, replacing half-unit displacement snapping. An off-grid start can align to the grid, while other coordinates stay unchanged. The contextual drag status explains the modifier; no button or numeric menu was added. Free dragging, rotation snapping, difficulty settings and simulation physics are unchanged.

`L03-precise-reference-direct-snap-reset.json` records an actual Shift-held green-arrow drag: fan starts at [-4, 3.8, 0], stays unchanged across all recorded samples, and wins at tick 265 (2.21 s). Outcome screenshot reviewed; independent motion/Reset audit passes. The previous free-drag failures remain valid evidence and are not overwritten. This proves one practical keyboard-assisted solution path, not beginner discoverability or mobile precision usability.

All 66 native tests pass, including off-grid alignment on all three axes, preservation of the other coordinates, cancellation and existing undo behavior. There are now 82 outcome records; the full difficulty matrix is still incomplete.

## Level 4: spring difficulty matrix

All twelve level-4 variants now have direct-UI records ending `-direct-reset.json` and reviewed `-outcome.png` screenshots. Construction used palette placement, movement arrows and the rotation ring, with free dragging (no optional Shift movement snap). No game state was injected. All runs had zero recorded browser errors.

| Difficulty | Reference | Near-positive | Near-negative | Outside window |
| --- | ---: | ---: | ---: | ---: |
| Forgiving | Won, tick 265 | Won, tick 265 | Won, tick 265 | Timeout, tick 3600 |
| Balanced | Won, tick 277 | Won, tick 278 | Won, tick 280 | Timeout, tick 3600 |
| Precise | Won, tick 302 | Won, tick 314 | Won, tick 356 | Timeout, tick 3600 |

All three difficulties started the near-positive spring at approximately [-2.916413, 0.78961825, 0.030801335]. Forgiving settled at the authored [-3, 0.8, 0]; Balanced settled at [-2.9909742, 0.79887897, 0.003325997], reflecting its smaller correction cap; Precise retained the initial transform. The outside-window spring remained at [1.0030489, 4.784618, 0] for all difficulties. Actual starting transforms, not recipe offsets, are the evidence.

The small errors also succeeded on Precise, so these outcomes alone do not establish a larger successful placement region. The recorded corrections demonstrate assistance for this spring; the existing native receiver sweep and level-3 browser counterexample provide different, narrower tolerance evidence. None is a substitute for the remaining campaign matrix.

All twelve records pass the independent checker for sampled bounds/easing, Precise/outside-window non-correction, and transform restoration after clicking the real Reset control. Rechecking all Reset-equipped evidence gives 40 passing records. The checker's documented assignment, rotation-eligibility and between-sample rendering limitations still apply.

Screenshots show the expected ball in the basket for successful runs; outside-window runs leave the ball on the solid tabletop. The displaced spring visually overlaps the basket at that camera angle, reinforcing the existing depth-readability finding. The spring's activation animation remains a requested TODO, not something these endpoint screenshots verify.

Current totals: 94 outcome records (80 wins, 14 timeouts), including historical/repeated records; 40 include Reset. Levels 2, 3 and 4 each have their twelve matrix outcomes recorded, but level 3's free-drag Precise reference failed and its separate snapped success does not erase that finding. The full 480-cell matrix remains incomplete.

## Level 5: depth-axis fan matrix

All twelve level-5 variants are recorded as `L05-<difficulty>-<variant>-direct-reset.json`, with reviewed outcome screenshots. The UI-only driver rotated the fan around Y to direct airflow along the depth axis; no numeric placement menus or game-state setters were used. Free movement dragging was retained. All runs recorded zero browser errors.

| Difficulty | Reference | Near-positive | Near-negative | Outside window |
| --- | ---: | ---: | ---: | ---: |
| Forgiving | Won, tick 131 | Won, tick 131 | Won, tick 131 | Timeout, tick 3600 |
| Balanced | Won, tick 141 | Won, tick 141 | Won, tick 141 | Timeout, tick 3600 |
| Precise | Won, tick 154 | Won, tick 197 | Won, tick 154 | Timeout, tick 3600 |

The actual near-positive fan starts at [-0.9117979, 3.7986748, -2.9664342] for all difficulties. Forgiving and Balanced finish their positional correction at [-1, 3.8, -3]; Precise retains its initial position. The outside-window fan starts and remains at [3.0076642, 7.7936745, -3] on all difficulties. The complete position/quaternion samples pass the existing bounded-motion/easing/Reset checker, including zero correction for Precise and the distant placement.

Successful screenshots show the tennis ball at the basket; failed placements leave it on the tabletop. The scene remains readable at the default angle for this sparse arrangement, though the floating fan's height is not obvious from the outcome view alone. Endpoint screenshots do not verify animation fluidity.

As with level 4, all small-error Precise attempts also win. This establishes a playable local region, not a strict win/loss difference between difficulty settings. Faster assisted outcomes cannot by themselves be attributed solely to placement nudges because receiver assistance also changes with difficulty.

All 52 Reset-equipped records pass the implemented audit after this batch. There are 106 total outcome records (89 wins, 17 timeouts), including historical/repeated attempts. Levels 2–5 now each have twelve matrix outcome records; the full 480-case matrix, the audit's documented gaps, and original-physics calibration remain unfinished.

## Reset connection evidence

The diagnostic observer now records the actual directed, typed connection graph at Run and after the real Reset action. The independent C# audit compares those graphs without depending on edge order, rejects duplicates/malformed entries, and reports missing snapshots as incomplete. This adds verification only; gameplay and physics are unchanged. It does not prove that the player constructed the recipe's intended graph, only that Reset preserves the graph actually built.

New direct-UI Balanced reference runs:

- [Level 2](playtest-results/L02-balanced-reference-direct-connections-reset.json): won at tick 116; switch_1 → lamp power link preserved.
- [Level 18](playtest-results/L18-balanced-reference-direct-connections-reset.json): won at tick 275; lane1_receiver → lane1_lamp and lane2_receiver → lane2_lamp power links preserved.

Both outcome screenshots were reviewed and show lit goals with visible connecting wires. Neither run recorded browser errors. Both pass the stronger motion/transform/connection Reset audit. A modified stdin copy with a wrong restored target fails with exit 1; original evidence remains untouched.

All 89 native tests pass, including fourteen audit cases covering matching/empty/reordered graphs, changed sources/targets/types, reversed/missing/extra/duplicate links, absent snapshots, and malformed data.

Re-auditing all 54 Reset-equipped records returns **2 passed, 52 incomplete, 0 failed**. The older 52 still satisfy the previous checks but lack connection snapshots; earlier pass counts are historical and do not satisfy this stronger check. The archive now contains 108 outcomes (91 wins, 17 timeouts), including repeats. Full campaign difficulty coverage and original-physics fidelity remain incomplete.

## Level 6: partial matrix and placement interruption

The Forgiving reference, near-positive, and near-negative direct-UI attempts all win at tick 127. Their three `L06-forgiving-*-direct-reset.json` outcome records pass the connection-aware audit for four placed dominoes, including an explicitly empty connection graph. Screenshots reviewed: success freezes the last domino while still substantially upright, confirming the existing mid-animation success-freeze issue.

The next attempt, `L06-forgiving-outside-window-direct-reset`, stopped during construction with `Timed out: placed part`. It did not reach Run and has no outcome record. Do not count it as a timeout simulation or a completed matrix cell. Investigate the UI placement failure before resuming this recipe; no game variables were changed to bypass it. Balanced/Precise level-6 matrix attempts have not yet run.

Three new records bring the archive to 111 outcomes; five Reset records now pass the stronger audit, while the older 52 lack connection evidence. The full matrix remains incomplete.

A fresh-navigation retry of the failed Forgiving outside-window recipe completed construction with the same input gestures, then timed out at simulation tick 3600 as expected. [Retry evidence](playtest-results/L06-forgiving-outside-window-direct-placement-repro.json) passes the stronger audit for all four dominoes; the screenshot shows the misplaced domino above the chain and the ball on the deck. The earlier failed state had three movable dominoes and one inventory item remaining, with no browser errors. The placement interruption is therefore intermittent and remains unresolved; no game or driver source was changed to claim a fix. The retry brings the archive to 112 outcomes and six connection-aware audit passes (including this separately named retry).

## Level 6: remaining difficulty outcomes

Balanced and Precise now each have all four UI-only variants recorded with Reset. Together with the Forgiving records and separately named outside-window retry, level 6 has twelve distinct matrix outcomes. No construction interruption recurred in these eight attempts, but the earlier intermittent failure is not considered fixed.

| Difficulty | Reference | Near-positive | Near-negative | Outside window |
| --- | ---: | ---: | ---: | ---: |
| Forgiving | Won, tick 127 | Won, tick 127 | Won, tick 127 | Timeout, tick 3600 (retry) |
| Balanced | Won, tick 127 | Won, tick 127 | Won, tick 127 | Timeout, tick 3600 |
| Precise | Won, tick 127 | Won, tick 127 | Won, tick 127 | Timeout, tick 3600 |

All twelve outcomes have reviewed screenshots and no recorded browser errors. All twelve pass the current audit for four placed dominoes, sampled correction bounds/easing and actual Reset restoration, including explicitly empty connection arrays. These checks still do not establish exact target assignment or rendered fluidity between samples.

The near-positive first domino starts at [-2.3095338, 1.0008088, 0.032416027] on all difficulties. Forgiving corrects it to [-2.4, 1, 0]; Balanced ends at [-2.3848424, 1.0001355, 0.0054313447]; Precise leaves it unchanged. Outside-window Balanced/Precise attempts retain [1.6099286, 4.9958086, 0]. Identical success times across all small-error runs mean this batch alone does not demonstrate a larger successful placement region.

The screenshot review continues to show success freezing the final domino before its visible topple finishes. The approved visual style was not changed during testing; this behaviour remains an open playability issue.

Archive totals: 120 outcomes (100 wins, 20 timeouts), including repeats/historical records; 66 have Reset evidence. Auditing those 66 gives **14 passed, 52 incomplete, 0 failed** under the connection-aware checker. The incomplete records lack connection snapshots. The full 480-case campaign matrix and original-physics calibration remain unfinished.

## Level 7: outcomes and mixed-run evidence investigation

The twelve difficulty/placement cases now have passing motion/Reset audits when using the separate isolated retry for Forgiving outside-window. Gameplay, author data, visual style, and the UI driver were unchanged for that retry.

| Difficulty | Reference | Near-positive | Near-negative | Outside window |
| --- | ---: | ---: | ---: | ---: |
| Forgiving | Won, tick 268 | Won, tick 268 | Won, tick 268 | Timeout, tick 3600 (isolated retry) |
| Balanced | Won, tick 281 | Won, tick 281 | Won, tick 281 | Timeout, tick 3600 |
| Precise | Won, tick 300 | Won, tick 302 | Won, tick 311 | Timeout, tick 3600 |

All thirteen outcome screenshots (the twelve original records plus retry) were reviewed. Wins show the ball at the basket and success UI; outside-window runs show a conveyor far above the useful route, the ball on the solid deck, and timeout UI. All records report zero browser errors. Precise reference construction initially hit the intermittent `Timed out: placed part` failure before a fresh-navigation retry succeeded; this does not establish a placement fix.

The original `L07-forgiving-outside-window-direct-reset.json` remains **failed**: it contains 62 samples, two identical sequences of ticks 0..120, rather than one 31-sample sequence. The retained local browser log `.playwright-mcp/console-2026-09-27T02-31-06-916Z.log` establishes an actual Run at 6040414 ms, Reset at 6052029 ms, second Run at 6060268 ms, timeout at 6090265 ms, and final Reset at 6090636 ms. This explains the mixed trajectories; the source of the unexpected early Reset/restart is not established. Do not silently deduplicate or count the original record as a passing attempt.

A fresh-page, UI-only [isolated retry](playtest-results/L07-forgiving-outside-window-direct-isolated-retry.json) has exactly 31 ordered samples, times out at tick 3600, restores the build with the real Reset control, and passes the independent connection-aware audit. It is separate evidence, not a repair to the historical record. Add explicit run-lifecycle tracking and fail early with captured evidence if an unexpected Reset or second Run occurs during an attempt.

These outcomes do not demonstrate a wider successful placement region on easier settings: all three small-error variants win even on Precise. Rendered continuity between samples, exact target assignment, and original-physics equivalence remain outside the implemented checker.

Current local archive: 133 outcome records (109 wins, 24 timeouts), including repeats/historical records; 79 have Reset evidence. Current audit: **26 passed, 52 incomplete, 1 failed**. The incomplete records lack connection snapshots; the failed record is retained mixed-run evidence. Neither the archive count nor this level's twelve accepted cases establishes completion of the full 480-case campaign matrix.

## Runner lifecycle and failure-capture regression

The UI adapter now retains ordered Run/result/Reset events and aborts on unexpected lifecycle events instead of merging observations. Failed construction or interrupted simulation returns `failure`, partial observations, the action log, latest UI and a failure screenshot; callers must save the record and stop the batch. The independent C# audit reports such a record as failed before requiring a completed Run/outcome. No gameplay or diagnostic game code changed.

- Nine adapter-only unit tests pass: normal lifecycle, early Reset, repeated Run/result/Reset, malformed diagnostic JSON, missing Run, unavailable construction part, and screenshot-failure cleanup.
- All 89 native C# tests pass.
- [Level 8 Balanced reference](playtest-results/L08-balanced-reference-direct-lifecycle-reset.json) was played with the actual palette, lift/rotation handles, receiver-to-lamp wiring, Run and Reset. It wins at tick 206, records exactly Run → result → Reset, reports no browser errors, and passes the motion/connection-aware Reset audit. The reviewed screenshot shows a lit lamp, wire and success UI.
- [Intentional early-Reset negative control](playtest-results/L07-lifecycle-negative-control.json) uses the level-7 outside-window recipe, then clicks the visible Reset toolbar button during simulation. The runner stops with `Unexpected reset event during running`, retains 24 partial samples and a failure screenshot, and the C# audit returns exit 1. This intentionally interrupted check is **not** campaign coverage or a puzzle timeout. The screenshot shows the restored build and placement projections.

There are now 134 actual outcome records (110 wins, 24 timeouts), plus the separate intentional failure control with no outcome. This verifies the runner's normal and interrupted paths, not the full matrix, the source of the earlier unexpected interruption, or a fix for the intermittent placement issue. The published production game is unchanged.

## Level 8: complete base matrix and exploratory tolerance probes

All twelve planned reference/near-positive/near-negative/outside-window cases now have reviewed screenshots and pass the independent sampled-motion/Reset audit. Each records exactly one Run, result and Reset; no browser errors or construction interruptions occurred. Actual Reset snapshots preserve the directed receiver → lamp power link.

| Difficulty | Reference | Near-positive | Near-negative | Outside window |
| --- | ---: | ---: | ---: | ---: |
| Forgiving | Won 206 | Won 206 | Won 206 | Timeout 3600 |
| Balanced | Won 206 | Won 208 | Won 206 | Timeout 3600 |
| Precise | Won 207 | Won 211 | Won 203 | Timeout 3600 |

Numbers are simulation ticks. Near-positive starts at [-2.916413, 0.78961825, 0.030801335] at every difficulty. At tick 120, Forgiving reaches [-3, 0.8, 0], Balanced reaches [-2.9909742, 0.79887897, 0.003325997], and Precise remains unchanged. The small-error outcomes alone do not show expanded successful tolerance because all difficulties win.

Twelve additional UI-only exploratory probes requested a +0.18 X offset and +3°, +6°, +12°, then +9° Z rotation error relative to the reference spring placement. Each is a separate artifact, not a replacement or extra base-matrix cell; the chronological search and all failures are retained. Recipes describe requested gestures, not exact resulting transforms.

| Requested angular error | Forgiving | Balanced | Precise |
| --- | ---: | ---: | ---: |
| +3° (`larger-offset-probe`) | Won 208 | Won 214 | Won 219 |
| +6° (`six-degree-probe`) | Won 218 | Won 227 | Won 233 |
| +12° (`twelve-degree-probe`) | Timeout 3600 | Timeout 3600 | Timeout 3600 |
| +9° (`nine-degree-probe`) | Won 234 | Timeout 3600 | Timeout 3600 |

The three +9° records have identical actual starting transforms: position [-2.8148696, 0.78461814, 0], quaternion [0, 0, -0.089754276, 0.99596393], approximately -10.29894° Z (actual error +9.70106° from the authored -20°). Forgiving corrects position to [-3, 0.8, 0] and angle to -13.29894°; Balanced reaches [-2.894595, 0.79124224, 0] and -11.49894°; Precise changes neither. This directly observes 3° / 1.2° / 0° angular correction, respectively, within the authored caps.

The Forgiving +9° screenshot shows the goal lit, whereas Balanced/Precise show an unlit goal and the ball on the floor. This is a matched-placement example of a wider successful region for the **whole Forgiving difficulty profile**, not a causal isolation of placement nudging from trigger/capture assistance. Repeatability and a complete tolerance boundary are not established by one matched triplet.

All 24 level-8 records pass the audit, including the exploratory failures; all 24 screenshots were reviewed. The spring still has no compression/rebound animation (existing TODO). No physics, author curves, visual style or published build changed.

Local archive now contains 157 actual outcomes (125 wins, 32 timeouts), plus the intentional early-Reset negative control without an outcome. The 103 outcome records with Reset audit as **50 passed, 52 incomplete, 1 failed**; the historical mixed-run failure remains untouched. Full campaign difficulty coverage, stronger audit gaps, rendered fluidity and original-physics calibration remain unfinished.

## Level 9: fan-to-switch difficulty matrix

All twelve UI-only cases have reviewed outcome screenshots and pass the current sampled-motion/connection-aware Reset audit. Each records exactly Run → result → Reset with no browser errors or captured construction failures. The screenshots show a lit lamp for the nine wins, and an unlit lamp with the ball on the solid floor for the three outside-window attempts.

| Difficulty | Reference | Near-positive | Near-negative | Outside window |
| --- | ---: | ---: | ---: | ---: |
| Forgiving | Won 106 | Won 106 | Won 106 | Timeout 3600 |
| Balanced | Won 106 | Won 106 | Won 106 | Timeout 3600 |
| Precise | Won 107 | Won 107 | Won 107 | Timeout 3600 |

Numbers are simulation ticks. The near-positive fan starts at [-3.9104257, 3.7891128, 0.03292829] on all difficulties. Forgiving and Balanced reach [-4, 3.8, 0]; Precise retains the initial transform. Outside-window placements remain at [0.009037018, 7.784113, 0] throughout sampled motion at every difficulty. Actual Reset restores the unassisted placement and the directed receiver → lamp power link.

The reference free-drag height is 3.7841127 rather than the authored 3.8. All Precise near/reference attempts still win here; the earlier level-3 free-drag failure did not recur in this puzzle. Since all small-error cases win without assistance, these outcomes do not demonstrate expanded successful tolerance. No gameplay, author curves or visual changes were made.

The archive contains 169 actual outcomes (134 wins, 35 timeouts), plus the separate intentional early-Reset failure control. The 115 outcome records with Reset audit as **62 passed, 52 incomplete, 1 failed**. Counts include repeated and historical attempts, not 169 unique accepted matrix cells. The older missing connection snapshots, retained mixed-run failure, exact-assignment audit gaps, rendered fluidity, full 480-case matrix and original-game physics equivalence remain unresolved.

## Camera and toolbox interaction update

The user revised the campaign objective to 75 progressively taught levels and prioritised new mechanics over exhaustive legacy testing; see TODO.md. The already-running level-10 batch completed with twelve saved outcomes, but its remaining screenshot/audit review is explicitly left in the handoff rather than counted as finished review.

The local browser build now uses ground-plane, camera-heading-relative WASD movement and continuous Q/E camera turning. Q/E no longer tilts parts. Right-drag orbit, wheel zoom, three-axis part rotation and the approved visual style remain. Inventory rows are full-width click targets and their toolbox grows to fit, scrolling only at the available-height limit.

Verification: all **96 native tests pass**. Actual Chrome UI input at 1440×900 showed all 12 free-workshop rows visible without clipping; name-click placed a ramp, count-click placed a conveyor and icon-click placed a lamp. Each of W/S/A/D/Q/E moved the observed projected geometry, with zero subsequent drift after release. Reset camera restored the original projections exactly. A Shift-drag on the Z rotation ring produced the expected 30° quaternion [0, 0, 0.25881904, 0.9659258], preserved by Run/Reset. Right-drag still changed the view. Returning to level 1 shrank the toolbox; two name-click placements exhausted its inventory, and a third click on the disabled row added no part. No browser errors were observed.

Reviewed screenshots: .playwright-mcp/toolbox-all-parts.png, camera-fps-gizmo-orbit-v2.png and toolbox-exhausted-rows.png. Detailed local evidence: .playwright-mcp/camera-toolbox-controls.json. Earlier check attempts timed out on the script's Stop-button observation and selector-End assumptions; corrected UI gestures passed without changing game state through setters. These interaction checks are not campaign wins or evidence for the new 75-level campaign. The published GitHub Pages build remains unchanged.

## Pinball bumper: first expanded-campaign lessons

The local draft now contains **42 levels**. New levels 11–12 are `bumper_sidekick` (“A little sidekick”) and `bumper_depth` (“Bounce into depth”); original levels 11–40 shift to 13–42. Current puzzle SHA-256: `6f48ca28e0f5ebccbe94dc07f2ed8da57d9ca24590170ffc92b172e6f5fa91cf`. Earlier hashes and level numbers remain historical evidence, not verification of the expanded campaign.

The new C# bumper uses a spherical collision proxy, radial launch impulse preserving tangential motion, per-body cooldown and an overlapping impact-ring pulse. Native tests cover contacts from six directions, glancing/missed contacts, cooldown, animation settling after simulation stops, Reset and same-host replay. Save version 2 identifies puzzles by stable ID; version 1 resolves the original 40 indices through their original IDs. These compatibility checks are native tests, not browser persistence verification.

All **110 native tests** and **9 adapter-only tests** pass. The controlled native test `PlacementNudgingAloneRescuesTheSameImperfectBumper` freezes all non-bumper assistance at strict defaults. Starting at [-3.1104352, 1.4996231, 0.03465762], Forgiving and Balanced win while Precise fails. Gravity stays 9.81, Start does not teleport the part, first-step movement is at most 0.001, and Reset restores the initial placement. This isolates placement assistance for one configuration; it is not a complete tolerance boundary.

### UI-only difficulty outcomes

Each accepted case uses palette selection, movement/rotation handles, Run and actual Reset. Ticks below are simulation ticks; timeout is tick 3600.

| Lesson | Difficulty | Reference | Near-positive | Near-negative | Outside window |
| --- | --- | ---: | ---: | ---: | ---: |
| 11 | Forgiving | Won 323 | Won 323 | Won 323 | Timeout |
| 11 | Balanced | Won 335 | Won 335 | Won 335 | Timeout |
| 11 | Precise | Won 349 | Timeout | Timeout | Timeout |
| 12 | Forgiving | Won 323 | Won 323 | Won 323 | Timeout |
| 12 | Balanced | Won 335 | Won 334 | Won 335 | Timeout |
| 12 | Precise | Won 349 | Timeout | Timeout | Timeout |

All 24 accepted records have reviewed outcome screenshots, zero browser errors, ordered Run → result → Reset, and passing sampled-motion/connection-aware Reset audits. Wins show the ball in the basket and success text; timeouts show no success. A passing audit validates the recorded motion/reset contract, not that a puzzle was solved.

For level 12, near-positive starts at [0.09592436, 1.5004194, 3.239566] at every difficulty. By the last sampled frame, Forgiving reaches [0, 1.5, 3.2], Balanced reaches [0.0034803078, 1.5000153, 3.2014356], and Precise remains unchanged. Near-negative starts at [-0.060988147, 1.490419, 3.1667554]; assisted settings reach the authored target while Precise retains that placement. This matched browser evidence establishes expanded success for the whole assisted profile; the separate native probe isolates nudging.

Base artifacts end in `-direct-bumper-v1-lifecycle-reset.json`, except three accepted retries:
- L11 Precise near-positive: `-direct-bumper-v1-isolated-retry-reset.json`.
- L11 Precise near-negative: `-direct-bumper-v1-key-delay-retry-reset.json`.
- L12 Forgiving outside-window: `-direct-bumper-v1-fresh-tab-retry-reset.json`.

The three original records remain failed and excluded: L11 near-positive recorded a second Run; L11 near-negative timed out selecting the requested puzzle; L12 outside-window recorded a second Run for level 22 during level 12. Their source is not established. Explicit key-down delays and a fresh test tab produced clean retries but do not prove the root cause fixed. Audit totals for these lessons are **24 accepted passes, 3 retained failures**, plus one additional passing motion probe.

### Rendered impact feedback

`L11-balanced-reference-direct-bumper-motion-probe-reset.json` repeats the UI-only reference and wins at tick 335. Four reviewed images capture the resting cream ring, expanded gold ring after impact, and return to rest. Requested ticks 72/92/104/120 correspond to latest observed diagnostic ticks 72/96/104/120; screenshots occur asynchronously, not at exact simulation boundaries. No game-state setter was used. Sampled frames demonstrate visible feedback, not frame-by-frame fluidity or mobile performance. The earliest six bumper outcomes predate the final overlapping pulse refinement; this probe uses the final animation implementation.

The 75-level rewrite, other mechanism families, repeated matched-error trials, stronger assignment-specific auditing, browser save persistence, mobile support and original-game physics calibration remain unfinished. All changes remain local; GitHub Pages has not been updated.

## Physical wall and local-axis resize interaction

The free workshop now includes a physical wall (14 catalog parts total). It is not a reference-plane wall: it has a solid oriented box collider and warm wood/cream panel artwork. The contextual resize icon appears only for a movable selected wall; square X/Y/Z handles follow its rotated local axes. Resize preserves centre/rotation, clamps dimension limits, updates serialized properties, collider, selection radius and artwork-derived dashed projections, and participates in cancellation/Undo. No campaign level has been added for walls yet; the campaign remains 42 levels.

All **118 native tests** pass. New coverage includes three local resize axes on a rotated wall, snap/cancel, size limits/non-finite inputs, serialization/Reset, collision rebounds at 0°/45°/90° yaw, contextual visibility and one-drag/one-Undo. The initial contact tests incorrectly read a freed wall after reloading the world; fixing the test to retain the original centre resolved those failures without changing physics.

Actual Chrome input in `wall-local-axis-resize-ui-v2.json` places and rotates a wall, then drags each square handle. Dimensions change from [3, 2, 0.25] to approximately [3.6, 2.6, 0.9]; one Undo restores thickness to 0.25. A further drag grows width to 4.617939, and Escape restores it to 3.6000001. The test reselects the wall if Escape clears selection before reading dimensions. Zero browser errors were recorded. The first record `wall-local-axis-resize-ui-v1.json` is retained as a failed observation: it compared an empty dimensions array after deselection rather than reselecting the wall. No game-state setters were used.

The reviewed resized screenshot shows the warm panel, cream bands, square coloured handles, three unfilled projection boxes and contextual icon row. At 1440×900 all 14 items fit without selection controls; opening contextual controls appropriately enables inventory overflow scrolling. This check is not a campaign win, nor browser save/load or Run/Reset verification. Generic campaign audits currently check transforms/connections, not resized property values; expand that coverage before claiming wall-level acceptance. Remaining work is tracked in TODO.md. The deployment remains unchanged.
