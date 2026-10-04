# TIM research and current game interpretation

This research records reference observations, provenance and unresolved fidelity questions. Current architecture is [WGSL f16 with typed Half game values](gpu-f16-physics.md); intended behavior and current acceptance live in [requirements](planning/requirements.md). Reference algorithms and original-game constants are not instructions to maintain a second solver.

## Adopted light and rope behavior

The self-contained torch uses impact/activation latching, a finite 15° half-angle cone with range 8 and nine equally weighted front-face panel samples, incidence/inverse-square response with a close-range cap and actual opaque geometry. Panel output is supplied binary power, not charge storage; ambient lighting supplies nothing. Preserve the soft conical artwork without making render sampling optical authority.

Ropes constrain total length of complete unbranched routes through typed sockets, carry tension only, support mass-dependent counterbalance and visible slack, and reject missing/nonfinite/out-of-range span lengths. An unfinished route is dashed and carries no tension. Finite-radius sheaves, physical work and shared contact belong to the current rope criteria, not an ideal copied-speed model.

## Which game are we matching?

The original belongs to the early 1990s; the likely mid-decade reference is **The Incredible Machine 2 (1994)** or **Version 3.0 (1995)**. Version 3 reused TIM2's puzzles with interface/platform changes. Use TIM2/3 as the interaction reference and the original as the physical-behavior reference. Sources differ on the first game's 1992/1993 dating; that discrepancy does not affect this design. [Series history](https://en.wikipedia.org/wiki/The_Incredible_Machine), [original entry](https://en.wikipedia.org/wiki/The_Incredible_Machine_%281993_video_game%29).

The essential loop is: inspect a goal and immovable setup, arrange a finite inventory, start the machine, observe consequences, stop, and revise. Freeform construction is a second essential mode, not just a level selector. Multiple valid solutions must be accepted by outcome, not by comparing placements against the author's arrangement. [Original walkthrough](https://www.sierrachest.com/magazine/gfx/Publications/hintbooks/index.php?a=games&fld=walkthrough&id=228).

## What the primary evidence establishes

The TIM2 manual describes programmable parts, attachment rules, goal authoring, and the editor. Ropes connect supported attachment points and can pass through pulleys; belts connect rotating devices. Power supplies energize electrical parts. Lasers can be redirected and combined by color. Ball properties include mass, elasticity, density, and friction. An egg timer and a leaking bucket introduce delays. Gravity and pressure belong to the authored environment; players inspect them in puzzle mode and change them in the workshop. The manual distinguishes tutorial, easy, medium, hard, really hard, head-to-head, and homemade puzzle categories. These are **puzzle categories**, not evidence of a physics-forgiveness slider in the original. [TIM2 manual, printed pp. 12–21, 28–30](https://pexy.io/wp-content/uploads/2025/06/the-incredible-machine-2-manual.pdf).

Kevin Ryan's developer account explains why tiny numerical changes break large machines. TIM used integer physics; Contraption Maker also moved to integer arithmetic after floating-point issues. Stable part-processing order matters. Their regression approach compares machine-state hashes after many frames across platforms. This makes repeatability and compatibility part of the intended feel, not optional polish. It does **not** disclose TIM's full equations or constants. [Ryan, 2016](https://www.moddb.com/members/kevryan/blogs/the-butterfly-effect-deterministic-physics-in-the-incredible-machine-and-contraption-maker).

Schiffler's thesis records Ryan describing Newtonian interactions supplemented by intentionally simplified air-density and rope behavior. It also reports deterministic machines with randomness confined to things that do not affect physics. Thus maximum scientific realism and maximum TIM fidelity are different objectives. [Thesis, Kevin Ryan interview discussion](https://www.ferzkopp.net/PhD/Thesis/html/thesis-text.html).

## Puzzle taxonomy

This grouping is our synthesis of the manuals and walkthroughs, not the original menu hierarchy.

| Family | Representative goal or mechanism | Implementation consequence |
| --- | --- | --- |
| Transport / capture | A ball reaches a basket, cage, or pocket | Typed target regions; require correct object identity and residence |
| Momentum / impact | Heavy ball hits a lever or another ball | Relative mass, restitution, contact direction |
| Rebound / launch | Springboards, bumpers, and superballs cross gaps | Stable bounce response and swept collision tests |
| Air / buoyancy | Fan steers balloon; altered pressure changes ascent | Directional force fields; density/buoyancy independent of weight labels |
| Activation chains | Start a mixer, toaster, or motor | Discrete state transitions in addition to rigid motion |
| Mechanical transmission | Motor → belt → gear → conveyor | Explicit rotational ports and connection graph |
| Rope / load transfer | Lift a load, pull a switch, release a tether | Attachment sockets, pulley routing, rope length, cut state |
| Light / logic | Redirect laser into detector or matching plug | Ray paths, occlusion, color, powered-state propagation |
| Fire / destruction | Pop balloon, ignite fuse, break tank | Event goals and material/tag-specific interactions |
| Timing / ordering | Timer releases something when another part arrives | Simulation-tick timing and ordered-event predicates |
| Characters | Lure, transport, or protect an animal | Small deterministic state machines and sensing |
| Multi-object / conjunction | Collect several items or activate two devices | AND/OR/count predicates, not a single trigger |
| Environment puzzles | Changed gravity/pressure alter familiar behavior | Environment stored in level data and replay identity |
| Construction / sandbox | Build and share a machine with its own objective | Serialization, locked parts, inventory, configurable goals |

Concrete puzzle references:

- **Beach Ball Bonanza**: get the striped ball into a bucket; **Cat Bounce**: pop a balloon; **Laser Target**: direct light into a sensor; **Breakfast Buffet**: make coffee and toast. These demonstrate transport, animal/electrical chains, optical routing, and combined goals.
- **Pipe Mixer**: activate a mixer; **Fishing in Rome**: break a fish tank; **Bomb Baffle**: trigger both bombs; **Bowl Me Over**: put a bowling ball in a cage; **Mel in a Muddle**: bring a character home safely. The walkthrough also records alternatives simpler than the built-in solution. [TIM2 medium walkthrough; search-index excerpts reviewed, direct retrieval failed](https://sierrachest.com/index.php?a=games&fld=walkthrough&id=229&pid=102).
- **Poolin' Around** involves filling individual pockets; **Beam Scream** involves interrupting a laser. These show why collision accuracy and event predicates both matter. [TIM3 walkthrough; indexed excerpts reviewed, full retrieval restricted](https://gamefaqs.gamespot.com/pc/30270-the-incredible-machine-3/faqs/21076).
- Original-game walkthrough material describes mouse motors activated by impacts and linked by belts, plus fixed pieces and limited supplied parts. [Original walkthrough](https://www.sierrachest.com/magazine/gfx/Publications/hintbooks/index.php?a=games&fld=walkthrough&id=228).

These examples are references for original new puzzles, not a plan to copy their layouts.

## Reference catalogue

Status: **read** = substantive text inspected; **excerpt** = search-index material inspected; **located** = reference found, not yet fully inspected. Video descriptions are not gameplay measurements.

| Reference | Status | Why keep it |
| --- | --- | --- |
| [TIM2 manual](https://pexy.io/wp-content/uploads/2025/06/the-incredible-machine-2-manual.pdf) | Read | Main parts, editor, and interaction reference |
| [Original DOS manual](https://www.retrogames.cz/manualy/DOS/The_Incredible_Machine_-_DOS_-_Manual.pdf) | Located; image-only PDF | Original controls and diagrams; needs visual transcription |
| [Sierra Help manuals index](https://sierrahelp.com/Documents/Manuals.html) | Excerpt | Links to original, Even More, TIM2, TIM3, and Toons manuals |
| [Sierra Gamers TIM2](https://www.sierragamers.com/tim-2/) | Excerpt | Additional manual mirror |
| [Sierra Family Fun Pack manual](https://www.mocagh.org/sierra/sierrafamilypack-manual.pdf) | Located | Contemporary Even More documentation |
| [Sierra multimedia manual](https://mocagh.org/sierra/multimedia-manual.pdf) | Located | Additional TIM2 documentation |
| [Sierra Interaction, holiday 1994](https://mocagh.org/sierra/interaction-holiday94.pdf) | Located | Contemporary marketing and context |
| [Original walkthrough](https://www.sierrachest.com/magazine/gfx/Publications/hintbooks/index.php?a=games&fld=walkthrough&id=228) | Excerpt | Original gameplay loop and mechanisms |
| [TIM2 easy walkthrough](https://sierrachest.com/index.php?a=games&fld=walkthrough&id=229&pid=101) | Excerpt | Introductory puzzle progression |
| [TIM2 medium walkthrough](https://sierrachest.com/index.php?a=games&fld=walkthrough&id=229&pid=102) | Excerpt | Diverse goals and alternative solutions |
| [TIM3 walkthrough](https://www.sierrachest.com/index.php?a=games&fld=walkthrough&id=230&title=incredible-machine-3) | Excerpt | Parts-bin categories, interface, duplication |
| [Even More guide, Lord_Seth](https://gamefaqs.gamespot.com/pc/564703-the-even-more-incredible-machine/faqs/44732) | Excerpt | Original-family puzzle and interface reference |
| [Even More guide, Mike8787](https://gamefaqs.gamespot.com/pc/564703-the-even-more-incredible-machine/faqs/18514) | Excerpt | 160-puzzle solution catalogue; pressure experiments |
| [TIM3 guide, King_Kool](https://gamefaqs.gamespot.com/pc/30270-the-incredible-machine-3/faqs/21076) | Excerpt | Complex timing, pool, optical goals |
| [TIM2 screenshot archive](https://www.sierrachest.com/index.php?a=games&fld=screenshots&id=229&pid=2&title=incredible-machine-2) | Excerpt | Visual reference indexed by puzzle |
| [TIM2 tutorial video, Dosgamert](https://www.youtube.com/watch?v=3buQptEnLEk) | Description | Candidate for recording timings and interactions |
| [TIM2 tutorial video, Daniel's Game Vault](https://www.youtube.com/watch?v=GtdpYO4GNT0) | Description | Independent tutorial reference |
| [TIM2 easy puzzles video](https://www.youtube.com/watch?v=kyjYf9duVPA) | Description | Candidate reference trajectories |
| [Original parts catalogue](https://the-incredible-machine.fandom.com/wiki/The_Incredible_Machine_%28game%29/Parts) | Excerpt | Part names and qualitative bounce/mass differences |
| [Kevin Ryan on determinism](https://www.moddb.com/members/kevryan/blogs/the-butterfly-effect-deterministic-physics-in-the-incredible-machine-and-contraption-maker) | Read | Primary engine-design evidence |
| [Schiffler interview appendix](https://pure.plymouth.ac.uk/ws/portalfiles/portal/38457901/2012schiffler10048250phd-appendix.pdf) | Excerpt | Fixed-point scale and part/edge attributes |
| [knt47's TIM level-format analysis](https://moddingwiki.shikadi.net/wiki/The_Incredible_Machine_Level_Format) | Read | Internal physics and connection fields; credit retained |
| [Schiffler thesis](https://www.ferzkopp.net/PhD/Thesis/html/thesis-text.html) | Read, relevant excerpts | Interview evidence about simplified physics |
| [Kevin Ryan interview transcript mirror](https://app.blocktunes.net/%40badastroza/interesting-people-25-kevin-ryan-on-the-incredible-machine) | Excerpt | General physics combined with whimsical character rules |
| [Contraption Maker, creator's page](https://spotkin.itch.io/contraption-maker) | Read | Successor reference for sandbox and extensibility |
| [Ryan's Incredible Puzzle Pack post](https://www.reddit.com/r/Games/comments/1b598wd) | Excerpt | Creator's later reinterpretation of original puzzle set |
| [US5577185A](https://patents.google.com/patent/US5577185A/en) | Read, relevant excerpt | Historical object/rule architecture; primarily Toons, not a TIM solver specification |
| [Series overview](https://en.wikipedia.org/wiki/The_Incredible_Machine) | Read | Edition map; secondary context only |
| [Original game overview](https://en.wikipedia.org/wiki/The_Incredible_Machine_%281993_video_game%29) | Excerpt | Release and expansion context |

Some sources disagree on puzzle totals and edition dates. Do not merge tutorial, head-to-head, expansion, and campaign counts into a single number. The manual's wording is not evidence that a specific gameplay video covers every puzzle.


### Additional implementation evidence

Ryan's interview appendix describes a fixed-point representation with 1024 representing one unit, polygon outlines, per-part physical values, and attributes on individual boundary segments. This supports keeping collision behavior attached to parts/surfaces rather than relying on a universal material. It does not supply a complete compatible solver. [Original interview appendix, Plymouth repository](https://pure.plymouth.ac.uk/ws/portalfiles/portal/38457901/2012schiffler10048250phd-appendix.pdf).

knt47's first-hand reverse-engineering notes document TIM2/3 level fields for mass, elasticity, density, friction, gravity, pressure, and connection endpoints. The stored default gravity is 272 and pressure is 67. These are internal values, not SI units. The programmable ball's dialog values are not the stored values: one documented dialog setting (8, 3, 7, 3) maps to mass 201, elasticity 128, density 3000, and friction 16. Several fields remain uncertain, and the default programmable ball itself has values not reproduced exactly by the dialog. Do not copy these numbers into a metres/seconds solver without deriving the conversion and testing trajectories. Credit: knt47. [TIM level-format analysis](https://moddingwiki.shikadi.net/wiki/The_Incredible_Machine_Level_Format).

The new conveyor is an original implementation using finite top-surface contact, bounded traction, local-axis motion, and a power port. Its native tests establish those behaviors, not equivalence to TIM's conveyor speed or friction. Native campaign results are likewise not substitutes for the requested browser playthroughs.

## Translation into a new 3D game

Working title: **Curious Contraptions**. Original models, interface, text, and puzzle layouts.

- Real XYZ positions, depth-aware collisions, an orbitable camera, and editable work planes. The starter camera should make height and gravity obvious. Use spatial depth in later puzzles; do not quietly implement a flat 2D game with a decorative perspective.
- Fast edit → run → inspect → restore. Reset reconstructs the entire initial state, including timers, ports, velocities, event history, and consumed objects.
- Parts are resource-discovered definitions with presentation scenes, typed geometry/sockets and declarations of generic capabilities. Numerical laws execute in the shared WGSL engine and discrete state in the simulation host. Adding an ordinary part must not require a private solver or editing the main game controller.
- The browser scene hierarchy owns presentation resources; the simulation owns physical objects and typed connection graphs. A belt between parts is not scene parenthood.
- Store stable IDs, part versions, initial transforms, environment, inventory, connections, goals, and physics profile in level data.
- Author new teaching puzzles for each mechanic, then combine them. Preserve experimentation, humor, and alternate solutions.

## Physics, difficulty and browser delivery

Use the single current WGSL physical model with [Forgiving/Balanced/Precise assistance](difficulty.md). Authored precision curves bound automatic placement/orientation correction, receiver regions, activation/timing thresholds and permitted guidance; no user nudge action, global relaxed physics, obstacle bypass or wrong-object acceptance. Run captures the selected settings and changing them requires restart. Test alternative valid solutions and the campaign matrix; greater assistance is not assumed to preserve every outcome without proof.

Use C# for typed authoring, host/discrete logic, UI and tools, WGSL f16 for numerical physics, and the pinned browser host/build instructions in [README](../README.md). Matching toolchain/editor/export versions and actual Chrome behavior remain required. Cross-device repeatability uses declared physical bounds and exact discrete event identity/order; fixed tick alone is not proof of identical floating arithmetic.

## Fidelity evidence still needed

1. Inspect source gameplay frame by frame for fall times, rebound ratios, ramp motion, fan reach, balloon ascent, rope response, and activation delays.
2. Record edition, scene geometry, pixel scale, and uncertainty alongside every measurement. Relative trajectories are more meaningful than inventing a meters-to-pixels conversion.
3. Build isolated regression scenes for each measurement, then combined machines.
4. Compare repeated native and browser state traces, including restart, moving a whole machine, and changing render FPS.
5. Perturb known solutions over a placement/angle grid; verify lower precision increases the successful region without bypassing obstacles or accepting the wrong object.
6. Validate browser interaction, 3D depth placement, every campaign solution, save/reload, and adding an independent part.
7. Exact original-physics equivalence remains unproven until these comparisons pass. Documentation and plausible-looking bounces are not sufficient evidence.

## New primary reverse-engineering lead — OpenTIM

Inspected on 2026-09-27: [OpenTIM](https://github.com/mrfixit2001/OpenTIM), pinned to commit `83fac1af87651fb6bcb128c9470fad236133e3c8` (2020-10-25). The inspected history credits Danny Spencer / nukep. Its README describes an in-progress reconstruction aiming to retain original simulation quirks and requiring original assets from the user. This is primary evidence of the author's reverse-engineering work, **not** official released TIM source or proof that the reconstruction is complete.

[The reverse-engineering notes](https://github.com/mrfixit2001/OpenTIM/blob/83fac1af87651fb6bcb128c9470fad236133e3c8/reverse-engineering/README.md) identify Windows **The Even More! Incredible Machine**, with TEMIM.EXE SHA-256 `03d56a132ff7c987488c6d28cc6ba9c4a28b6f9d085c53a3c5a0bfdd14e49e35`. This narrows the edition provenance; it must not silently stand in for TIM2/3 or the original DOS executable. The project marks intentional original-game fixes with `VANILLAFIX`.

The [atmosphere module](https://github.com/mrfixit2001/OpenTIM/blob/83fac1af87651fb6bcb128c9470fad236133e3c8/src/atmosphere.rs) includes numerical fixtures the author says were obtained by inspecting the original TIMWIN Parts table in memory. Selected data and its provenance are recorded in [physics-reference-temim.json](physics-reference-temim.json); no upstream implementation code or game assets were imported.

### Concrete comparisons now available

The [part definitions](https://github.com/mrfixit2001/OpenTIM/blob/83fac1af87651fb6bcb128c9470fad236133e3c8/src/parts/mod.rs) report:

| Part | Internal mass | Density | Bounciness | Friction |
| --- | ---: | ---: | ---: | ---: |
| Bowling ball | 200 | 2832 | 128 | 16 |
| Basketball | 20 | 1322 | 192 | 16 |
| Tennis ball | 5 | 1322 | 192 | 16 |
| Balloon | 1 | 9 | 64 | 32 |

These raw reference values do not directly define our current authored mass/restitution. Compare relative mass and bounce behavior independently; do not equate source bounciness with a restitution coefficient or change current content without validated physical controls.

Selected reported atmosphere fixtures:

| Gravity setting | Pressure setting | Density | Signed acceleration | Terminal component limit |
| --- | ---: | ---: | ---: | ---: |
| 272 | 67 | 1322 | 266 | 9695 |
| 272 | 67 | 2832 | 269 | 9695 |
| 272 | 67 | 9 | -198 | 9695 |
| 272 | 0 | 1322 | 272 | 9728 |
| 0 | 67 | 1322 | 1 | 9695 |
| 272 | 128 | 1322 | -97 | 7680 |
| 272 | 128 | 2832 | 76 | 7680 |

Units are internal, not SI. Positive acceleration is downward in the referenced implementation. Notably, minimum gravity is not necessarily zero acceleration, and maximum pressure can reverse acceleration for some densities. Those observations conflict with assuming a linear SI gravity/pressure mapping. They have not been independently reproduced here.

### Solver details to validate, not silently port

The [movement and contact reconstruction](https://github.com/mrfixit2001/OpenTIM/blob/83fac1af87651fb6bcb128c9470fad236133e3c8/c_src/main.c) labels relevant routines with original addresses: velocity update `1090:01b0`, component-wise terminal clamp `1090:012d`, and bounce `1090:0644`. The inspected bounce path uses both contacting parts' material data, a low-speed loss, and special cases; our constant restitution alone is not equivalent. Position updates use 512 subunits per pixel in the inspected code, while Ryan's interview describes a 1024-based representation. This may reflect different quantities or editions; it rules out assuming one universal scale from that interview.

The [reconstruction's renderer update](https://github.com/mrfixit2001/OpenTIM/blob/83fac1af87651fb6bcb128c9470fad236133e3c8/src/nannou.rs) advances simulation on alternate update callbacks. That is **not** evidence of the original game's frame frequency. Establish simulation ticks versus wall-clock/video frames independently before converting accelerations or velocities.

OpenTIM is GPL-3.0. It remains an external research reference; no code dependency, port, license change, or publication of original assets was made. Any proposal to incorporate its implementation needs an explicit dependency/licensing decision. Numerical facts in our reference file retain source attribution and uncertainty rather than being presented as our measurements.

A second lead, [moralrecordings/breakfastmachine](https://github.com/moralrecordings/breakfastmachine/tree/1f1ffe920fb7e3c1ee7c742d9b760089890ba903), describes a TIM3 disassembly project. Its inspected file tree contains resource/graphics code; no physics implementation was established during this pass. Keep it as a located lead, not an independent physics confirmation.

### Next compatibility experiments

1. Use an identified, lawfully available original executable or reference capture matching the recorded edition/hash; independently reproduce the atmosphere fixtures where possible.
2. Capture isolated basketball, bowling and tennis drops at two heights onto the same surface. Check both dimensions and boundary shape; sprite sizes alone do not establish collision size.
3. Compare collision output across impact speeds, surface types and low-speed settling. A single bounce-height fit cannot recover additive losses or contact-specific rules.
4. Establish original step rate and each quantity's fixed-point scale. Compare relative mass ratios independently of the chosen world-unit scale.
5. Compare the single current physical model against independently measured reference behavior without using assistance to hide differences. Preserve approved visuals; any adopted behavior change follows the current source/design workflow and requalifies affected campaign cases. Do not add a Classic/Physical backend or retain an old solver.

The major change in evidence is that there are now pinned, edition-specific, upstream-reported numerical fixtures to investigate. They do not yet prove original-compatible physics, and no new original-game trajectory was measured in this pass.
