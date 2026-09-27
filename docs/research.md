# The Incredible Machine: research and design evidence

Research date: 27 September 2026. This is a design dossier, not a claim of an exact engine reconstruction. Sources are linked individually; inaccessible full pages are identified. No original artwork, level files, music, or code is included.

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
- Parts are standalone Godot scenes described by resources. Behavior, appearance, collision proxies, and connection sockets belong to each part. A registry discovers resources. Adding a normal new part should not require editing the main game controller.
- Scene hierarchy owns objects; a separate graph connects ports. A belt between unrelated parts is not parenthood.
- Store stable IDs, part versions, initial transforms, environment, inventory, connections, goals, and physics profile in level data.
- Author new teaching puzzles for each mechanic, then combine them. Preserve experimentation, humor, and alternate solutions.

## Physics realism and assistance

Proposed design, not historical claims:

A **Classic** profile targets observable TIM behavior: stable gravity, distinct ball masses/bounces, simplified buoyancy, understandable gadgets, repeatable runs. A **Physical** profile can expose more material friction, drag, angular response, and stricter constraints. Neither profile is an excuse to change the simulation timestep by difficulty.

Within each profile, a continuous **precision** setting selects author-defined, per-part assistance curves. Per the user's requirement, the nudge is automatic engine behavior, not a player-facing action. Authored placement/orientation windows bound correction toward a reference placement, with continuous easing rather than snapping. Separately, individual parts can specify receiver acceptance regions, activation thresholds, timing windows, and bounded lateral guidance. These values belong in puzzle data, not a universal physics relaxation rule. Never count an unrelated object or trigger a mechanism through an obstacle. Snapshot profile and assistance when starting a run; changing them requires a restart. See [implemented difficulty authoring](difficulty.md) for current behavior and limitations.

Keep authored puzzle complexity separate from this setting. Physics changes can invalidate layouts, so puzzle metadata must identify supported profiles. Do not claim that every realistic-profile solution automatically works in every assisted profile: test that property on the campaign.

Initial parameters to tune (new design values, not measured TIM constants):

| Parameter | Forgiving | Precise |
| --- | --- | --- |
| Receiver acceptance margin | 0.35 m | 0.02 m |
| Activation impulse threshold | 65% of authored threshold | 100% |
| Allowed timing window | 150% of authored window | 100% |
| Optional lateral capture force | Bounded, visible, receiver-local | Disabled |
| Simulation tick | Same fixed tick | Same fixed tick |

A fixed tick alone does not prove cross-platform determinism. A first implementation may quantize custom simulation state and establish native/browser replay comparisons, but the current quantized floating-point implementation has not established bit-identical behavior across platforms. A measured compatibility suite is required; merely replacing floats with integers would not prove equivalence either.

## Godot web constraints

The user requires C# wherever possible. Gameplay, simulation, UI, tests, and preview tooling now use C# with Godot Compatibility rendering. Godot 4 C# web export is not supported by the inspected official documentation, so the project uses the third-party 2dog browser host with pinned Godot/.NET packages. The published WebAssembly build has completed direct-UI Balanced reference playthroughs of all 40 levels, with reviewed outcome screenshots; see [browser evidence](browser-playtest.md). That verifies rendering and the exercised interactions, not complete persistence coverage, the full difficulty matrix, or original-physics fidelity. The browser bootstrap retains necessary HTML/JavaScript infrastructure. [Official web-export guide](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html); [2dog setup](https://2dog.dev/getting-started.html); [2dog web host](https://2dog.dev/hosts/web).

Use matching editor and export-template versions. Pin the toolchain so physics changes cannot arrive accidentally with an engine upgrade. [Official export guide](https://docs.godotengine.org/en/4.6/tutorials/export/exporting_projects.html).

## Fidelity evidence still needed

1. Inspect source gameplay frame by frame for fall times, rebound ratios, ramp motion, fan reach, balloon ascent, rope response, and activation delays.
2. Record edition, scene geometry, pixel scale, and uncertainty alongside every measurement. Relative trajectories are more meaningful than inventing a meters-to-pixels conversion.
3. Build isolated regression scenes for each measurement, then combined machines.
4. Compare repeated native and browser state traces, including restart, moving a whole machine, and changing render FPS.
5. Perturb known solutions over a placement/angle grid; verify lower precision increases the successful region without bypassing obstacles or accepting the wrong object.
6. Validate browser interaction, 3D depth placement, every campaign solution, save/reload, and adding an independent part.
7. Exact original-physics equivalence remains unproven until these comparisons pass. Documentation and plausible-looking bounces are not sufficient evidence.

## Isolated physics calibration baseline — 2026-09-27

[PhysicsCalibrationTests.cs](../CuriousContraptions.tests/PhysicsCalibrationTests.cs) now measures an isolated vertical drop for basketball, bowling ball, and tennis ball at pressure 0, 1, and 2. These are **our engine's native measurements, not measurements of TIM**. No campaign targets, assistance, fans, or other parts participate. Each starts at center (0, 6, 0), at rest, with gravity 9.81 and precision 1, above the solid deck at Y = -0.46.

Each case captures 481 position/velocity/visibility samples, including the initial state, over four simulation seconds. It restores the machine and checks exact equality of the second trace, plus restored position and velocity. At zero pressure, additional sanity checks compare fall time with constant-gravity motion and rebound-height ratio with squared restitution. Those analytical checks validate this isolated model, not historical compatibility.

| Part | Pressure | First impact tick | First apex tick | Rebound / initial clearance |
| --- | ---: | ---: | ---: | ---: |
| Basketball | 0 | 134 | 208 | 0.30120 |
| Basketball | 1 | 135 | 207 | 0.28771 |
| Basketball | 2 | 136 | 206 | 0.27484 |
| Bowling ball | 0 | 134 | 153 | 0.01930 |
| Bowling ball | 1 | 135 | 153 | 0.01866 |
| Bowling ball | 2 | 136 | 154 | 0.01802 |
| Tennis ball | 0 | 135 | 241 | 0.60657 |
| Tennis ball | 1 | 136 | 238 | 0.57528 |
| Tennis ball | 2 | 137 | 236 | 0.54590 |

Ticks are 1/120 second with four internal substeps. Impact/apex detection brackets a vertical-velocity sign change between adjacent tick samples; it does not identify the exact collision substep. Apex height uses the greater of the two bracketing sample heights. Clearance is measured from the sphere's center at floor contact, accounting for different radii. These ratios are not velocity restitution coefficients.

Reproduce measurements (JSON appears in detailed test output):

```sh
dotnet test CuriousContraptions.tests --nologo --filter FullyQualifiedName~PhysicsCalibrationTests --logger 'console;verbosity=detailed'
```

The full native suite passed 75 cases after adding these nine cases. Same-runtime replay equality does not establish browser/native equality.

### Concrete remaining calibration differences

Inspection of [MachineWorld.cs](../engine/MachineWorld.cs) identifies these current design choices without original-game measurement support:

| Current implementation | Comparison needed before claiming fidelity |
| --- | --- |
| Gravity 9.81; pressure scales buoyancy and linear drag | Original fall/ascent traces at multiple authored environment settings; establish unit and time conversion first |
| Sphere speed limited to 40 world units/s | Original long-fall or launch behavior; do not assume its terminal speed or cap |
| Box contact multiplies tangential velocity by 0.985 or 0.998 | Original ramp travel and horizontal slowdown; current `Realistic` flag only switches this friction factor |
| Low-speed upward-facing contact suppresses rebound below 0.45 | Original settling and low-height drops |
| State rounded to 1/65536 after each substep | Repeated and cross-platform traces; this is not the original fixed-point algorithm |

The level-format source confirms authored environment and ball-property fields, but not their conversion into our world units or integration equations. The interview-appendix PDF and original DOS manual both timed out on this research pass; their contents were not newly inspected. [Level-format source](https://moddingwiki.shikadi.net/wiki/The_Incredible_Machine_Level_Format).

Next reference experiment: identify an edition and capture an isolated ball drop onto a horizontal surface. Record the initial bottom-to-surface clearance, first impact, and first rebound apex in original pixels and video timestamps, with duplicate-frame/timing uncertainty. Compare the dimensionless rebound-height ratio before fitting any world-unit scale. Repeat with the same ball at another height and with the other ball types; keep some drops out of fitting as validation cases. Do not tune difficulty nudges to hide a base-physics mismatch. No original drop measurement has yet been recorded.
