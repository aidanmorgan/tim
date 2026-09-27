# Browser campaign playtest

## Joined tubes — 58-level draft; difficulty work now deferred

Campaign hash: `a800fa3f065bee0ac9de85183125d8bee37b06ca7d281892b78c01e5b2d7df56`. Lesson 28, “Meet in the middle”, requires a shortened straight tube feeding a fixed 90° bend. The adapter supports validated final axis-handle moves after rotation/resizing, so the join is made through an ordinary mouse gesture. These are editor actions, not runtime nudging controls.

The initial `L28-balanced-reference-joined-v1.json` is failed evidence: Run appeared during construction before the script requested it. Its cause remains unknown. The isolated v2 retry and the subsequent eleven v2 cases completed with zero browser errors and full Run/result/Reset. Reference wins are Balanced 250, Forgiving 237 and Precise 265 ticks. The snapped pipe has measured length approximately 2.0073; its position aligns the actual collar face rather than forcing the nominal length-2 centre, and Reset preserves it.

At approximately 0.48 Z error, Forgiving/Balanced win and Precise times out. At approximately 0.58, only Forgiving wins. Missing-tube cases time out at all three settings. Native fixtures remove receiver assistance and reproduce the two distinct authored windows. These runs were already in flight when the user redirected priority to component coverage; further difficulty sweeps are deferred until the component set is implemented.

The native suite passes 326 tests, including straight-to-bend and bend-to-bend passage at 6 and 40 units/s with bounded displacement and no added energy, plus gravity-driven joined passage. An initial landing estimate was corrected from the measured trajectory; the two-bend test's exit check was strengthened to require proximity to the outlet axis, because a half-plane alone also included the inlet. Adapter unit tests pass 37. Broader multi-ball queues, blocked outlets, all orientations/speeds and continuous frame-rate checks remain unfinished.


## 45° and 90° bends — 57-level draft

Current campaign hash: `0ae028b3a743782143bb95a9c6a7ffbcf68bd508edaa39b2bf61b411d4f42edc`. Lessons 26–27 introduce a gentle bend and a quarter turn after the straight-pipe lesson. Typed angles and opening IDs, a continuous hollow circular collision surface, common-bore collars, transparent cyan artwork and angle-specific icons extend the existing C# scene graph and sphere solver. Native tests align both bends with straight tubes and with each other in rotated 3D poses. There is no hidden path-following, added speed or automatic transport-network connection.

The first six `reference-bend-v1` browser runs passed on hash `f5d951bc481f63ef24f465d1a9f4bc41a6571904d6ee682b6098d93750c67a93`. Native matched-error trials then exposed a poor generic assistance profile: partial correction could turn a successful 45° placement into failure, and did not rescue the 90° offset. An initial test also accidentally shared its perturbed solution with the reference target; cloning the reference fixed that test-fixture error before evaluating the real profile issue. The authoring generator now gives each bend an explicit bounded full-alignment window: Forgiving 0.6 units / 5°, Balanced 0.5 / 2°, Precise zero. Quintic easing remains 0.4 seconds; the physics engine is unchanged. This is a specific authored policy, not proof that all future error patterns are monotonic.

Fresh `L{26,27}-{balanced,forgiving,precise}-reference-bend-v2.json` all win. L26 ticks are 185/173/201; L27 ticks are 262/251/275. Matched `depth-error-bend-v2` trials use the same approximately 0.48017 Z error. Both easier modes align the bend to Z=0 and repeat their reference win ticks. L26 Precise leaves the error untouched and still wins at 239: this is a valid alternate placement, not evidence of expanded tolerance. L27 Precise leaves it untouched and times out at 3600. Native tests remove fixture assistance and reproduce these results for exact 0.48 errors.

`L27-balanced-reference-bend-motion-v3.json` repeats tick 262 with sixteen screenshots. Reviewed frames 3/6/9 show entry at 0.53 seconds, the ball inside the curve at 0.90 and exit at 1.28. These are sampled frames, not continuous frame-rate measurements. All thirteen current-version records have zero browser errors, complete Run/result/Reset, and passing audits with one sampled part each. Native tests cover ball trajectories separately.

All 315 native tests pass, including all 57 campaign reference solutions at three difficulties, missing-part failures, six rotated bend passages without added energy, gravity-only traversal, nearest-surface checks, mouth alignment and isolated placement assistance. Blocked flow, queues, high-speed curved collisions, gravity-driven multi-piece seams, broader error sweeps and the full 75-level UI matrix remain unfinished.


## Straight-tube mouth snapping — editor checks

Campaign remains 55 levels with hash `9386ca71bb8b61253c8d9c67da823ed4f859cb012107632026b5534b8ecc8f72`. Typed Start/End mouths sit at the outer collar faces. Editor queries require matching bores, opposing normals within 20°, distance at most 0.45 units and an unoccupied destination mouth. They preserve roll through the shortest alignment rotation, leave runtime assistance untouched and create no transport-network edge or rigid assembly.

`sandbox-tube-snap-v1.json` places two tubes through the palette in the free workshop (selector row 56, not a new campaign level): the second requested centre X=1.9 snaps to X=1.78 against the first at X=-2. Screen-coordinate verification agrees within a pixel; reviewed screenshot shows adjacent collar faces. One Undo removes only the second tube. `sandbox-tube-snap-move-v1.json` additionally pulls it one unit away, moves it back into range, verifies rejoining and undoes each movement independently. Both records have zero browser errors. These are editor checks, not Run/result/Reset campaign records.

`L25-balanced-tube-snap-regression-v1.json` resizes to 3.2, wins at tick 262, resets and passes the campaign audit with no errors. All 297 native tests pass, including three-axis mouth alignment, non-mutating queries, continuous zero-gravity ball passage through both joined tubes, constant speed, Reset, distant/wrong-facing rejection, occupied mouths, locked parts and running-state rejection. The initial new UI test incorrectly used height 2 instead of the workshop's height-3 placement plane; its fixture was corrected. Production publish passes. Gravity-driven seam combinations, incompatible-bore fixtures, blocked flow, corner joins and a full current campaign browser matrix remain to verify.

## Straight-pipe resizing — 55-level draft

Campaign hash: `9386ca71bb8b61253c8d9c67da823ed4f859cb012107632026b5534b8ecc8f72`. Pipes resize from 1–8 units with a fixed 1.3-unit bore; a typed resize capability exposes only the local length handle while walls keep three. Native tests cover geometry, collars, optical bounds, serialization/Reset, invalid dimensions, rotated dragging, Cancel and one-gesture Undo.

Six `L25-{balanced,forgiving,precise}-length-{32,42}-resize-v2.json` attempts use real placement/rotation/resize drags. Length 3.2 wins at ticks 262/216/266; length 4.2 wins at 283/250/296. The `L12-balanced-wall-resize-regression-v1.json` three-axis wall resize wins at 515. All seven have zero browser errors, complete Run/result/Reset and passing audits. These alternative lengths are not a new isolated difficulty-nudging comparison.

The six resize-v1 records are retained as failed verification despite winning outcomes: startup reported an unsupported basket length. An ambiguously anchored content edit had put the new pipe property on the first basket. The content was corrected, regenerated and checked against the generator; no compatibility exception was added. A stale variable in the new parameterized UI test was also corrected. The fresh native suite passes all 290 tests; adapter tests pass 31. Elbows, mouth snapping and the broader campaign matrix remain unfinished.

## Clear gravity tube — 55-level draft

Campaign hash: `ffc48e5fb25b24fe323d457e4419c9021543db686ff22901a643f502a966ef61`. Lesson 25, “Through the looking tube”, introduces a fixed-length clear pipe. The C# solver collides spheres against finite hollow cylinders and annular ends, including transformed pipes and rope-load contact passes. It does not capture/teleport a ball or prescribe travel velocity. Clear walls transmit traced light; opaque cream collars use the same hollow shape for optical occlusion. This is a modern hollow conduit, not a claim of calibrated original TIM pipe physics.

`L25-{balanced,forgiving,precise}-reference-pipe-v1.json` win at ticks 255/212/262. The matched `depth-error` variants place the pipe at approximately Z=0.48017. Balanced corrects to 0.38017 and wins at 268; Forgiving corrects to 0.23017 and wins at 195; Precise leaves it unchanged and times out at 3600. Native fixtures remove basket assistance and reproduce the successful Forgiving/Balanced versus failed Precise outcome at an exact 0.48 error. An initial native expectation incorrectly predicted Balanced failure; the observed native/browser agreement corrected that assertion without changing the mechanism.

A fresh `L25-balanced-reference-pipe-v2.json` repeats the win at 255 and captures sixteen browser motion frames. Reviewed frames 6/9/12 show the ball travelling inside the transparent tube and leaving toward the basket; the clear shell, cream collars, navy rails and original pipe icon retain the palette. All seven records have zero browser errors, complete Run/result/Reset and passing sampled-state/connection audits. Screenshots sample motion, not continuous frame-rate performance.

All 282 native tests pass, including all 55 campaign references at three difficulties, continuous rotated bore passage, side-wall rejection, maximum-speed passage, an oversized ball stopped at the mouth, light through the bore/shell versus collar occlusion, isolated placement assistance, missing-pipe failure and deterministic Reset. Straight resizing, elbows, funnels, explicit mouth snapping, pipe networks, seams, queues and blocked-outlet combinations are unfinished; the full repeated 75-level matrix is still incomplete.

## Pulley rim artwork — 54-level draft

Campaign hash remains `2691ed54858909a421ef9b89f0c45596f7051f7bb07d19f1a8c84a2b3348e371`. Rope presentation now uses tangent entry/exit legs and 32-segment contact arcs outside each intervening pulley rim. Adjacent-wheel tangencies are iterated, endpoint knots remain, and the former pulley knots are hidden. Winding is retained during motion and chosen to favour the wheel's local upper side.

The first six `rim-v1` UI captures all won, but screenshot review found an unwanted almost-full loop on the right pulley: its nearby starting weight selected the underside. Those records remain unchanged. The winding selection was corrected and a close-starting-weight native regression was added.

All six `L{19,20}-{forgiving,balanced,precise}-reference-rim-v2.json` cases win with zero browser errors and complete Run/result/Reset. Level 19 wins at tick 93 for all three; level 20 wins at 92/92/93. Reviewed Balanced screenshots show both same-plane and depth-separated ropes using the outer rims without the extra loop. All audits pass, but their sampled transform coverage is zero parts for L19 and one static pulley for L20; they do not measure weight trajectories or arc motion. Native tests cover those geometry and simulation-isolation concerns separately.

All 270 native tests pass. New coverage includes three-axis rotation, depth-offset tangency, rim clearance, symmetric upper threading, retained winding under small motion, reversed traversal, close starting loads and finite artwork during both campaign simulations. The dynamics still use ideal fixed point guides: finite-radius displayed length is not reconciled with the rope constraint, and projections inside the rim use radial contact rather than true groove tangency. Slack/wheel-travel reconciliation, near-axis routes, close-up continuous animation and performance remain unfinished; this stage does not complete finite-wheel rope physics.

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

## Powered gate — focused component smoke (27 September 2026)

The component-first priority defers difficulty sweeps. This batch leaves the campaign at 58 draft levels.

- Native: all 332 tests pass, including six gate checks for electrical socket compatibility, bounded continuous opening, Reset, powered/unpowered travel in two orientations and obstruction-safe closing/resumption.
- Browser: local diagnostic build, real palette selection, move/rotate handles, battery-to-gate wiring, Run and Reset. Accepted local records are `powered-gate-freeworkshop-v6.json` and `powered-gate-unwired-v7.json`. Both have one intended Run and one intended Reset, no console errors, and identical before/after part/connection snapshots. Screenshots show the powered blade retracted with the ball below the gate, versus the unwired ball retained above the gold blade. These are free-workshop controls, not campaign wins or difficulty evidence.
- Earlier attempts are retained locally: v1 screenshots/console log, v2–v4 JSON and v5 diagnostic trace. The Reset lookup timeout was caused by read-only UI diagnostics suppressing every snapshot while simulation ran: the driver still saw the pre-run button. Running controls now emit once at the transition; ongoing moving-body snapshots remain suppressed. The separate unexpected Run/Reset events in earlier attempts are not explained or claimed fixed by this change.
- Anvil graph checks were available; write validation reported authentication-required and allowed proceeding with warning. No diagnostic input tracing remains in production code.

## Hold timer and explicit connection choice (27 September 2026)

- 346 native tests pass, including 14 new checks: exact expiry before network evaluation in both entity orders at three durations; ignored busy triggers and rearming; Reset and saved duration; no-source/power-loss/restoration behaviour; delay-to-hold-to-gate composition; ambiguous socket rejection and explicit typed save/load; invalid author durations. The first power-restoration fixture incorrectly tried editor wiring while Run was active; corrected the native fixture to restore its saved supply link rather than weakening the editor lock.
- UI-only local records `hold-timer-freeworkshop-v1.json` and `hold-timer-electrical-choice-v2.json` each have exactly one intended Run/Reset, zero console errors, and identical start/Reset part and connection snapshots. Six parts are placed using palette/gizmos; a falling ball activates a switch. Explicitly choosing the trigger icon for switch→timer makes its gold bar count down, opens the powered gate and releases a second ball. Reviewed captures at ~1.65 s and ~4.23 s show the bar draining/open gate, then spent timer/closed gate.
- The second record chooses the electrical icon for the same switch→timer pair. Captured connection domain is electrical; without a trigger the timer remains idle and the second ball remains on the closed gate. The contextual choice screenshot shows two icons within the existing drawer, with no floating inspector. Ordinary unambiguous battery/timer/gate wires remain one-click links.
- A third UI record, `hold-timer-choice-cancel-undo-v3.json`, verifies Escape dismisses the contextual choices, selecting a trigger adds a link and one Undo removes it. The following Run/Reset retains exactly the original three electrical links and reports no console errors.
- Campaign remains 58 drafts; introductory lessons and the complete difficulty matrix are still pending. Previous gate commit's Pages workflow 36309710645 completed successfully; these local browser observations are not a Pages-origin smoke check.

## Tube-mounted ball detector (27 September 2026)

- 356 native tests pass, including ten detector checks. Swept centre-plane intersection handles supported fast travel and arbitrary part orientation; whole-ball clearance upstream rearms it, avoiding repeated triggers from centre-plane jitter. Reverse, outside-aperture, stationary and invisible bodies do not trigger. Two separated balls each trigger once. No extra propulsion or path following is introduced. Reset clears the detector count/pulse and downstream timer state.
- The initial 120-unit/s fixture used an unrealistically short integration interval despite the world's existing 40-unit/s safety cap. Corrected the expected integration speed/time, retained the cap and reran the entire suite; no production change was needed for that failure. The isolated swept-aperture/jitter test also exercises a complete crossing between samples.
- Local UI-only `ball-detector-forward-v1.json` and `ball-detector-reverse-v2.json`: palette placement, rotation handles, detector→hold-timer trigger wire, battery supply and timer→gate wire. Forward/downward orientation activates the timer, visibly drains its bar and opens the gate; later capture shows expiration and gate closure. Reversing the collar still permits physical ball passage but leaves timer idle and gate closed. Both attempts have one intended Run/Reset, exact starting/restored snapshots and zero console errors. These are focused component controls, not campaign difficulty trials.
- Visuals retain the approved cream/cyan/navy/gold palette. No new global controls. Campaign remains 58 drafts; detector/hold/gate teaching levels remain on the backlog.

## Funnel inlet and joined tube (27 September 2026)

- All 365 native tests pass. Nine new cases cover centre/±0.75-unit offset gravity drops, unpowered momentum-preserving central travel at ordinary/maximum supported speed and arbitrary orientation, analytic signed shell/end normals, matched narrow versus mismatched wide mouth snapping, exterior collision, transparent-shell light passage, Reset and a complete gravity-fed funnel→tube exit. Normal and rope-load collision passes both include the new continuous annular-frustum proxy; specific rope-load funnel trials remain pending.
- Local `funnel-joined-ui-v1.json` uses only the palette, 3D move/rotation handles, a final move-release snap, Run and Reset. The measured ball starts at X=0.9/Z=0.1; funnel centre Y=4.992545 and tube centre Y=2.1125455 give the intended 2.88-unit separation, with both rotated −90° about Z. No console errors; exactly one intended Run/Reset with identical snapshots.
- Reviewed motion captures at ~0.50, 0.83, 1.22 and 2.25 s show the off-centre ball entering the wide rim, passing the throat and moving down the joined transparent tube. The tube in this browser setup ends near the solid floor, so the final image shows the ball bouncing within its lower section; this is not a claim of a clear browser outlet exit. The elevated native joined fixture separately verifies complete exit.
- Palette, translucent cyan glass, cream rims and navy rails match the current design. Campaign remains 58 draft levels; no new difficulty sweeps were run. Prior ball-detector Pages workflow 36310466632 completed successfully.

## Continuous pressure plate (27 September 2026)

- All 380 native tests pass, including 15 new checks for basketball/weight loading, single underweight versus two combined tennis balls, missing supply, contact release and gate closure, rotated top-face detection, rejection of hovering/side/underside/invisible bodies, threshold save/load, typed socket rejection, invalid parameters and Reset.
- The sensing rule is deliberately explicit: sum masses of visible dynamic spheres directly touching the local top face, with their centres projected inside it and a 0.025-unit contact skin. It is an occupied-load switch, not a force solver; stacked loads and rope-transmitted forces are not counted. Electrical routing is sampled after this state update. No activation output or implicit electricity is added.
- UI-only records `pressure-plate-loaded-v1.json`, `pressure-plate-underweight-v2.json` and `pressure-plate-rolloff-v3.json` use palette placement, the 3D gizmo, real electrical wiring, Run and Reset. Each has zero console errors, one intended Run/Reset and identical initial/restored snapshots.
- Reviewed screenshots: basketball remains on the plate and holds the gate open at ~4.26 s; a tennis ball remains too light and leaves the gate closed at ~4.25 s. On the −15° tilted plate the basketball slides off: the gate is open near ~1.01 s, then closed and plate lights slate near ~3.59 s with the ball on the floor. This demonstrates continuous release rather than latching.
- Existing palette and minimal UI are retained. Campaign remains 58 drafts and difficulty sweeps remain deferred.

## Saturating event counter (27 September 2026)

- 395 native tests pass, including 15 counter checks: targets 1/3/9 with and without supply; exact event counting without per-tick accumulation; saturation and one-shot output even after a downstream hold timer rearms; latched real electrical supply; physical three-ball detector chains in both entity orders; target save/load, fresh runtime count on restore; invalid/non-integral target rejection. Workshop Reset clears count, active state and lights; no dedicated reset-signal input is implemented yet.
- Local UI-only `counter-three-balls-v1.json` and `counter-two-balls-v2.json` place a downward detector, stacked starting balls, counter, battery and gate with palette/gizmos; wire the three explicit typed links; then Run and Reset. Both have zero console errors, one intended Run/Reset and identical starting/restored snapshots.
- Reviewed three-ball captures at ~1.68 and ~4.27 s show three gold dots and a gate that remains open. The two-ball control at ~4.25 s shows two gold dots, one slate dot and a closed gate. The native tests separately verify extra input events do not re-emit or exceed the target. Dot colour easing is presentation-only and continues when physics stops after success.
- The component uses the approved ochre/cream/navy/gold palette and existing wiring controls. Campaign remains 58 drafts, with counter teaching levels and comprehensive difficulty trials pending.

## Reset-dominant memory latch (27 September 2026)

- All 400 native tests pass, including five new cases: both entity/delivery orders with/without supply, next-tick command settlement, memory retention during source loss/restoration, repeated Set/Reset, pending-command cancellation through workshop Reset, explicit-input rejection, and both sockets reached in one activation fan-out. Activation delivery now carries an enum command resolved from the actual destination socket; loop protection distinguishes commands on the same part.
- All 37 browser-adapter tests pass. The UI driver accepts explicit set_in/reset_in targets and clicks the corresponding contextual icon. Existing activation handlers were forward-refactored to the typed signature; unsupported commands are rejected.
- Local UI-only records `latch-set-reset-v1.json` and `latch-simultaneous-v1.json` use palette placement, movement/rotation handles and explicit wiring. Both have one intended Run/Reset, identical starting/restored snapshots and zero console errors.
- First setup: detector sends Set immediately and Reset through a one-second delay. Reviewed captures at ~1.46 s show a lit latch and open gate; at ~4.25 s the latch is off and the gate closed. Second setup: detector fans out to both Set and Reset; the gate remains closed. Reviewed construction capture confirms distinct rendered bar/ring socket-choice icons in the existing drawer.
- These are focused component controls, not campaign/difficulty coverage. Campaign remains 58 drafts; teaching levels and full 75-level balancing remain pending.

## Powered repeating clock (27 September 2026)

- All 413 native tests pass. Thirteen clock checks cover 0.1/1/12-second intervals with clock ordered before/after the counter, no startup pulse, four exact intervals without duplicate substep events, power interruption partway through a cycle, a complete fresh interval after restoration, no catch-up events, invalid interval rejection and workshop Reset.
- Initial test runs exposed fixture errors: an omitted instance ID caused a Godot empty-name error in invalid-input cases; changing IDs to exercise actual sorted simulation order required updating the post-Reset lookup too. Both fixtures were corrected, with no weakening of timing assertions. The full suite subsequently passed.
- Local UI-only `clock-powered-v1.json` and `clock-unpowered-v1.json` place the clock, counter, battery and gate using palette/gizmos, wire explicit sockets, then Run/Reset. Both have one intended Run/Reset, identical starting/restored snapshots and zero console errors.
- Reviewed powered captures at ~1.45 s show one lit counter dot and closed gate; at ~4.23 s all three dots are lit and the gate is open. Pendulum poses differ across the captures. Without the battery→clock wire, the pendulum rests vertically, all counter dots remain slate and the gate remains closed at ~4.24 s despite the counter's separate electrical supply.
- All 37 adapter tests and production Release web publish pass. No campaign difficulty sweeps were run; 58 draft levels remain toward the target of 75. Latch Pages workflow 36312026359 completed successfully; these clock captures use the local diagnostic build, not the public Pages origin.

## Continuous Both interlock (27 September 2026)

- All 422 native tests pass. Nine new cases cover every two-input combination with chained gates in both sorted entity orders, same-solve propagation, supply removal/restoration, ambiguous socket selection, workshop Reset and previously powered feedback cycles after all real sources are disconnected.
- Electrical conjunctions are explicit typed rules. Reachability starts afresh from actual sources each solve; the output becomes reachable only after both required inputs. No previous-tick input state drives the rule, no implicit power is created, and no extra per-gate tick delay is introduced. This remains the existing binary supply model, not a voltage/current solver.
- Local UI-only `both-timed-input-v1.json` connects battery→first input, battery→hold timer→second input, detector→timer and Both→powered gate. Reviewed ~1.45 s capture shows all three lamps lit and the hatch open. At ~4.24 s only the first lamp remains lit and the hatch is closed. The construction capture confirms distinct one/two-mark contextual socket icons render in the existing drawer.
- `both-missing-first-v1.json` omits the first supply. At ~1.45 s the timer and second input are on, but output remains off and hatch closed. Both attempts use palette/gizmos/wiring/Run/Reset only; each records one intended Run/Reset, identical initial/restored snapshots and zero console errors.
- All 37 adapter tests pass. The preceding clock Pages workflow 36312298721 completed successfully. These are local component checks; the campaign remains 58 drafts and the full 75-level difficulty matrix is deferred.

## Initial laser emitter and receiver (27 September 2026)

- All 438 native tests pass. Sixteen new optics checks cover enable/source/receiver-supply combinations, one-tick electrical-to-optical sampling, power loss, Reset, four arbitrary three-axis orientations, front/back/finite-disc targeting, wall/ball/opaque-pipe-collar interruption and clearance, finite range and nearest absorbing target without duplicated delivery.
- Emitters carry linear RGB game power, fixed amber output and a 16-unit range. The first implementation traces a centre ray; visible thickness is artwork, not finite-width energy coverage. Receivers sum absorbed contributions and close separately supplied electrical contacts at a fixed threshold. No reflection, refraction, spectral filtering, beam branching or flashlight detection is claimed yet.
- `laser-receiver-clear-v1.json` uses real palette/gizmo/wiring controls to connect a battery-powered clock, triggered laser, receiver and powered hatch. Reviewed ~4.04 s capture shows the beam reaching the gold receiver and hatch open.
- Retained `laser-receiver-blocked-v1.json` is an ineffective blocking fixture, not an accepted negative control: actual wall Z=0.2 with thickness 0.25 left the Z≈0 beam behind it. The wall visually overlapped from the camera but the beam correctly remained unobstructed.
- Corrected `laser-receiver-blocked-v2.json` rotates the wall 90° about Y through the UI. At ~4.26 s the beam stops at the wall, receiver is slate and hatch is closed. Both accepted controls have one intended Run/Reset and no console errors.
- Reset audit: all positions, part identities, flags, properties and typed connections match exactly. The two 180° rotations differ by approximately 0.00001725° after Euler serialization/reconstruction, below the existing 0.002° audit limit; snapshots are not byte-identical. The ineffective v1 fixture is retained unchanged.
- All 37 adapter tests pass. Both-gate Pages workflow 36312581330 completed successfully. Evidence is local; the campaign remains 58 drafts and component-first work continues before full difficulty sweeps.

## Flat mirrors and multi-segment optical paths (27 September 2026)

- All 444 native tests pass. Six mirror cases cover the reflection law before/after arbitrary 3D rotation, non-activating geometry previews, two-mirror routing with multiplicative 95% reflectivity, one total range budget, opaque rear/frame and downstream-wall absorption, bounded facing-mirror loops, Reset and placement ghosts outside MachineWorld.
- Forward-refactored single beam lengths into world-space optical segments; simulation and editor preview share the pure trace query. Reflected rays no longer ignore their original emitter or the previous mirror's mount. At most 16 reflections occur, each attenuating power, and only the final absorbing receiver receives a contribution. This is still centre-ray optics, not finite-width coverage or refraction.
- The old exact range assertion saw 15.999999 instead of 16 when reconstructed from endpoint coordinates. It now checks a 0.00001-unit geometry tolerance; the separate multi-reflection test verifies the shared range budget.
- Retained `mirror-reflection-v1.json` has console errors and is not accepted: the placement ghost is parented by Workshop, but MirrorPart assumed a MachineWorld parent. Preview tracing is now explicitly restricted to selected, placed mirrors in a non-running/non-won world. A native ghost regression covers this editor context.
- Accepted UI-only `mirror-reflection-v2.json` uses palette placement, the three-axis rotation gizmo, real wiring and Run/Reset. The selected-mirror construction capture shows the outgoing aim guide while the receiver stays dark; the ~4.26 s run capture shows the 45° mirror routing the beam downward, receiver gold and hatch open.
- `mirror-wrong-angle-v3.json` leaves the mirror at 0°. The beam returns toward the emitter instead of reaching the lower receiver; the ~4.24 s capture shows receiver slate and hatch closed. Both accepted records have zero console errors, one intended Run/Reset and byte-identical starting/restored construction snapshots.
- All 37 adapter tests and production Release web publish pass. Laser Pages workflow 36313049427 completed successfully. These remain local component checks, not final campaign/difficulty coverage.

## 50:50 splitter and bounded optical branching (27 September 2026)

- Twelve new focused cases cover splitting from both sides with/without one blocked branch, equal RGB power, independent reception, cascading to half/quarter/quarter with conserved total power, default half-power acceptance versus quarter-power rejection, transparent-pane ball collision, opaque frame clipping, deterministic mirror/splitter loops and invalid receiver thresholds.
- Forward-refactored optical surfaces to an interaction enum and traces to a list of absorbing receiver contributions; no singular-result compatibility API remains. Every split partitions each RGB component equally. Paths retain the parent's remaining range; traces stop after 16 surface interactions per path or 128 total segments per emitter, discarding remaining work without reallocating its power. This is still bounded centre-ray tracing, not finite-width lens physics.
- Receiver sensitivity is now an author parameter (0.05–2, default 0.25), intentionally changed from the initial fixed 0.5. A split of the current amber emitter delivers 0.35 mean RGB power and can operate two separately supplied receivers; a further split delivers 0.175 and cannot operate the default receiver. Historical earlier screenshots keep their recorded settings.
- UI-only `splitter-two-receivers-v1.json` places/rotates/wires one splitter, two receivers, clock-enabled laser, battery, Both gate and powered hatch. Reviewed ~4.24 s capture shows both receiver discs gold, all Both indicators lit and hatch open. The build capture shows faint two-path aim guidance with receivers still off.
- `splitter-one-branch-blocked-v2.json` adds a resized horizontal wall using actual rotation/resize handles. The ~4.24 s capture shows the reflected branch ending at the wall, lower receiver slate, straight-through receiver gold, only one Both input lit and hatch closed.
- Both attempts have one intended Run/Reset and zero console errors. Positions, identities, flags, properties and typed links restore exactly. The 180° orientations differ by ~0.00001725° through the existing Euler round trip, within the 0.002° Reset audit tolerance rather than byte-identical quaternion data.
- All 37 adapter tests pass. Mirror Pages workflow 36313557064 completed successfully. The 58-level draft campaign is unchanged; final difficulty testing remains deferred until component coverage is complete.

## Beam shutter — 27 September 2026

- Added standalone electrical beam shutter and shared accelerated blade motion with the existing tube gate. Six new native cases verify unrotated/rotated actual-ray clipping, partial-opening transmission, collider/visible pose equality, power-loss closure, safe ball obstruction/resumption, physical blocking/pass-through and Reset. Existing six tube-gate checks remain green; 462 native checks pass.
- UI-only `beam-shutter-timed-v2.json` constructs eight parts through the palette/gizmos and seven links through actual connection controls. Ball detector enables the laser and a two-second hold timer; timer powers the shutter. The 1.46 s capture shows retracted blade, beam reaching the gold receiver and open downstream tube gate. At 4.25 s, timer expiry has closed the shutter, ray ends at its blade, receiver is slate and downstream gate is closed.
- `beam-shutter-unwired-v1.json` omits only timer→shutter power. At 1.46 s the timer is active but the blade remains closed, receiver slate and downstream gate closed. This checks missing supply rather than a different optical arrangement.
- Both accepted attempts have one Run and one Reset, no console errors and no synthetic success. Free workshop has no win outcome. IDs, positions, properties and links restore exactly; largest quaternion round-trip deviation is 0.000017303 degrees, below the existing 0.002-degree allowance.
- Retained failed `beam-shutter-timed-v1.json`: driver scrolled down before trying to return to the clipped basketball and exhausted its search. Changed driver to scroll toward the observed target row; above/below regression tests bring adapter checks to 39. No storage mutation, game setters or numeric placement menus.
- Reviewed screenshots: `.playwright-mcp/beam-shutter-timed-v2-holding.png`, `beam-shutter-timed-v2-outcome.png`, and `beam-shutter-unwired-v1-holding.png`. Captures establish sampled visual states, not uninterrupted animation fluidity. Campaign lesson, rope actuation, mobile/performance and exhaustive difficulty tests remain pending.

## Primary-colour filters and powered receivers — 27 September 2026

- Added three channel-filter and three matching-receiver catalog scenes, sharing typed C# colour behaviour. Two-sided finite filters multiply channel power by an RGB mask; they absorb other components without changing direction or adding power. Frames remain opaque and panes collide with balls.
- Receivers require selected-channel power >= author threshold (default 0.25), plus >=90% purity; broadband receivers retain mean-RGB threshold behaviour. Supply remains independent. The source remains the existing amber emitter with game-power RGB (1,0.78,0.32); filtering does not pretend that it has equal white power.
- Fourteen new native cases cover front/back and arbitrary filter rotation, missing-channel extinction, unlike/identical filter stacks, purity and threshold boundaries, separate supply, Reset, frame clipping, physical pane collision and non-red beam opacity. Full suite: 476 passed. Adapter suite: 39 passed.
- Three initial native supply tests failed because the fixture attempted to Connect during Run. That operation correctly returns false; tests now assert the rejection, Restore, connect in build mode and Run again. No runtime editing bypass was introduced.
- Real-UI records: `colour-blue-match-v1.json`, `colour-green-match-v1.json`, `colour-red-match-v3.json` and `colour-wrong-channel-v1.json`. All construct six parts/five links using actual palette, movement/rotation and connection controls. Matching filtered beam lights receiver and opens downstream gate; blue beam hitting red receiver leaves it slate and gate closed. Screenshots reviewed at ~4.25 s.
- All four accepted cases have no console errors, one Run/Reset and exact IDs, positions, properties and links after Reset. Maximum quaternion round-trip difference is 0.000017303 degrees, below existing 0.002-degree audit tolerance. Free workshop is a smoke fixture with no win outcome, not campaign completion.
- Retain `colour-red-match-v1/v2.json`: selector timed out before construction. Inspection showed delayed selector response; v3 adds explicit startup/popup waits and slower key events. This is a separate deliberate attempt, not an automatic fallback; root cause remains pending.
- Current palette retained, with one/two/three channel marks and distinct icons. Low-power blue beam is visible but faint against sky. Full contrast/mobile/performance checks, mixed-colour receivers, combiner, lens physics, campaign teaching and exhaustive difficulty sweeps remain pending.
