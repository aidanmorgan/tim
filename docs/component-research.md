# Component research: water, sound and cross-domain logic

Research date: 27 September 2026. Status: **proposed backlog, not implemented or verified**. Companion checklist: [TODO](../TODO.md). Fluid, sound and logic were researched by sub-agents; the consolidated priorities and puzzle recipes are project design proposals, not claims about historical TIM behaviour.

## Design contract and priority

P1 = next useful foundation; P2 = builds on those foundations; P3 = exploratory after the first playable family. Preserve DESIGN.md and its existing cream/navy/cyan/gold palette, toy-like forms, minimal contextual icons and touch-friendly controls. No CAD panels or permanent network overlays. Use C# enums for finite domains/states/operations and typed port/parameter identities. Forward-refactor current systems; no compatibility aliases or silent substitute behaviours.

Implement components before exhaustive difficulty testing. Each selected batch still needs native behaviour/Reset tests, authoring and inventory integration, original icons, animation and a real-UI browser smoke puzzle. These proposals expand the candidate catalogue, not the number of concepts that must be crowded into 75 levels. Teach each required mechanism before its capstone; reserve optional advanced pieces for the workshop or later content.

## Water: transport, storage and conversion

Use finite conserved water, bounded network transfers and visible free-stream capture rather than full CFD. Cosmetic particles never decide capture, success or water quantity. Existing ball tubes remain ball conduits; add explicitly typed fluid capabilities and distinguish sealed pressure pipes from open gutters.

| Priority | Candidate | Behaviour and puzzle decision |
| --- | --- | --- |
| P1 | Reservoir / header tank | Capacity, initial volume, outlet elevation and open catch mouth. Horizontal waterline and etched fill marks; dwindling head changes discharge. An external source must be explicitly authored and visually distinct. |
| P1 | Tap / stopcock | Regulates existing supply; build-set opening with a visible quarter-turn handle. Later mechanical lever and powered solenoid variants supply different actuation choices, not free water. |
| P1 | Water pipe kit | Straight length, 45° and 90° bends, T junction, cap, outlet nozzle. Compatible-mouth snapping and visible seated collars. A split shares supply rather than copying it. |
| P1 | Gutter / trough / aqueduct | Broad open collection and downhill flow with visible spill edges; cannot behave like a sealed uphill pressure pipe. |
| P1 | Catch basin / funnel / drain | Capacity-limited capture and explicit overflow. A drain is an accounted sink, not invisible cleanup anywhere on the floor. |
| P1 | Waterwheel | Stream position and direction create signed mechanical drive for belts/conveyors; the discharged water remains available underneath. Paddle filling/rotation must reflect actual flow. |
| P1 | Movable / leaky bucket | Extend the existing rope-container entry: total mass includes contents; tilt spills, leaks drain visibly into other containers. Do not substitute an unrelated countdown or silently remove mass. |
| P1 | Guided float / level switch | Water lifts a visible float. A mechanical float valve needs no battery; an electronic level contact needs supply. Distinct high/low thresholds prevent chatter. |
| P2 | Check valve / diverter / sluice | One-way flow, route selection and channel gating. Animate actual flap/blade state; closed valves and split branches must conserve volume. |
| P2 | Pump / Archimedes screw | Mechanical or electrical input lifts finite water against head. Dry intake is visibly idle. Exposed screw/bucket elevator makes conversion especially readable. |
| P2 | Siphon / priming bulb | Explicit dry, priming, flowing and broken states. Air entry breaks the column; the final receiving surface must allow gravity-driven transfer. |
| P2 | Tipping bucket / water clock | Real fill/load tips a hinged cup, spills a batch and returns. A physical strike can ring a chime or pulse a counter. Requires hinges and load support. |
| P2 | Communicating tanks / canal lock | Low connecting pipes equalize levels; controlled sluices lift a floating platform. Order and finite supply matter. |
| P2 | Cork / raft / floating platform | Displaced volume and load determine flotation. Start with constrained basins and simple hulls; rising water can deliver a ball onto a track. |
| P2 | Hydraulic piston | Pressure acts on a piston with finite stroke, displaced-water storage and return path; load resists motion. |
| P2 | Flow / volume / pressure meter | Separate moving-water, accumulated-volume and pressure measurements. Visible paddle or dial; threshold controls do not generate actuator power. |
| P3 | Accumulator | Finite stored hydraulic energy with a visible spring/bladder; requires load and energy accounting first. |
| P3 | Sponge / wick / squeeze pad | Finite absorption adds mass; compression releases water. Any capillary rule must be explicit and bounded. |
| P3 | Sprinkler / rain collector | Spatial distribution and collection with an authored finite source budget and selected-only footprint preview. |
| P3 | Ice plug / melt gate / kettle / condenser | Thermal-energy and phase-change extension; visible steam is not proof of a working thermal simulation. |

### Water puzzle recipes

1. **First Pour:** tank → tap → broad gutter → marked bucket. Teach height and finite supply in an almost planar arrangement.
2. **Run the Mill:** stream → waterwheel → belt → conveyor → ball goal. Wrong-side water reverses rotation; introduce reverse transmission only afterwards.
3. **Borrowed Weight:** fill a rope bucket until it lowers and lifts a ball platform. Follow with a leaking bucket that reverses the balance later.
4. **Every Drop Counts:** use wheel discharge to fill a second basin and raise a float. Two independent supplies should exceed the inventory budget; reuse is the insight.
5. **Two Gardens:** split a finite supply between two target fill bands without overflowing either.
6. **Up and Over:** battery/motor → screw pump → elevated header tank → gravity gutter. Stop the pump and observe stored gravitational energy.
7. **Prime Time:** a hold timer briefly powers a priming pump, then a siphon continues to a lower receiver; intake exposure stops it.
8. **Quietly Rising:** floating ball platform meets a track; high-level float closes the tap before overflow.
9. **Three Splashes:** tipping bucket strikes a chime once per dump → sound detector → counter → hatch after three pours.
10. **The Mill's Song:** wheel → cam/plucker → chimes → sound-controlled sluice → final basin.
11. **Dark at High Tide:** float physically interrupts a laser; receiver control stops a supplied pump.
12. **Canal Lift:** fill/equalize/drain a lock with two sluices to lift a toy boat or platform.
13. **Pressure, Not Plenty:** choose an elevated narrow tank rather than a large low tank to lift a loaded hydraulic piston.
14. **Catch the Escape:** rope lifts/tilts a bucket into a movable funnel; mistimed pours visibly spill to a collection tray.

### Water acceptance and simulation boundaries

- Account for initial volume + explicit external input = contained + in transit + sinks + explicit loss, within a documented numerical tolerance.
- Calculate transfers from a common tick snapshot, resolve shared supply/capacity, then commit. Construction/insertion order must not decide which branch receives all the water.
- Track gravity/head, priming and venting explicitly. Do not grant uphill motion just because mouths are connected.
- Current ideal mechanical drive lacks complete torque/load sharing. Forward-refactor power budgeting before claiming wheel → pump loops conserve energy; no perpetual fountains.
- Sweep streams/capture mouths so moving buckets and narrow mouths do not depend on rendered frame rate. Airborne packets may be bounded, but exceeding supported limits must fail authoring validation rather than delete water.
- Choose explicit floor handling: shallow catch tray, bounded puddles or visible drains. Include displaced water and filled-bucket load without double counting.
- Reset restores volumes, in-flight transfers, primes, valve states, wheel state, load and sensor hysteresis. Test empty/full/overflow, equal/reversed head, closed/backflow valves, branching, dry pumps, cycles, tilted capture, source exhaustion and pause/slow playback.
- Later authored nudging may ease valid alignment/catch tolerances; it must not invent supply, ignore a wall, bypass priming or create pumping energy.

### Water evidence

- [EPA EPANET](https://www.epa.gov/water-research/epanet): network vocabulary and modelling of tanks, reservoirs, pipes, pumps, valves, flows and pressure. Supports a bounded network abstraction, not a requirement to integrate EPANET.
- [DOE: How Hydropower Works](https://www.energy.gov/cmei/water/how-hydropower-works): head and flow determine available hydropower; turbine and generator are separate conversion stages.
- [NPS: Hopewell waterwheel](https://www.nps.gov/places/000/waterwheel.htm) and [Lowell water power](https://www.nps.gov/lowe/learn/photosmultimedia/water_power.htm): water-filled wheel buckets and transmission through shafts/gears/pulleys.
- [OpenStax: Buoyancy](https://openstax.org/books/university-physics-volume-1/pages/14-4-archimedes-principle-and-buoyancy): displaced-fluid weight underpins buoyancy.
- [Science Buddies: Straw siphon](https://www.sciencebuddies.org/stem-activities/straw-siphon): priming, air interruption and level constraints.
- [Factorio: Fluids 2.0](https://www.factorio.com/blog/post/fff-416) and [Drowning in fluids](https://www.factorio.com/blog/post/fff-430): developer lessons on predictable flow, readable connections and preserving meaningful constraints.
- [Disney: Where's My Water?](https://appsupport.disney.com/hc/en-us/articles/360000758626-Getting-Started-with-Where-s-My-Water) and [Nintendo: Fluidity—Spin Cycle](https://my.nintendo.com/rewards/54cbade66846a414): official water-routing puzzle precedents, not specifications for these proposed parts.

## Complementary mechanisms

These are original proposals; extend existing gears, bellows, converters and heat entries rather than duplicating them.

| Priority | Candidate | New decision and combination |
| --- | --- | --- |
| P1 | Cam and follower / crank | Rotation becomes visible periodic displacement; wheel-driven cam strikes a bell or opens a tap once per revolution. Cam profiles change dwell rather than merely appearance. |
| P1 | Clutch / brake | Engage a powered shaft or stop its load without deleting upstream energy; sound/level control starts a conveyor only when its destination is ready. |
| P2 | Ratchet / escapement | Ratchet holds gained motion; escapement releases stored weight/spring energy in steps. A dripping bucket can advance a lift without letting it roll back. |
| P2 | Bellows, air hose and pneumatic piston | Extend bellows with finite compression, routed air, valve and spring-return actuator. Wheel → cam → bellows → whistle or piston bridges mechanical, air and sound. |
| P2 | Air reservoir / release valve | Store a bounded charge and release a burst after the source stops. Show pressure and exhaust; don't conflate an open fan stream with sealed compressed air. |
| P2 | Electromagnet / magnetic pickup | Supplied coil attracts compatible material, releases on power loss; move a steel ball past a wooden one. Define force, material and range explicitly before puzzles depend on them. |
| P2 | Material/size sorter and escapement feeder | Physical apertures or a powered diverter sort actual bodies; one-at-a-time release prevents jams. No hidden ID-based success routing. |
| P3 | Bimetal thermostat / melt plug / expansion actuator | Slow heat accumulation and cooling offer delay without a timer; require a bounded thermal model and explicit energy source. |
| P3 | Flywheel / governor | Stored rotational energy bridges a short supply gap; overspeed control changes a valve. Depends on torque, inertia and loads, not the current ideal drive alone. |

Additional recipes: **Polite Factory** (clutch holds conveyor until a receiving bucket is ready); **One Breath** (finite air charge raises piston, then exhaust rings a whistle); **Sorting Office** (magnet removes steel ball, remaining ball reaches a different chute); **Clockwork Rain** (waterwheel cam meters individual drops into a counterweight); **Warm Welcome** (heat releases an ice plug, waterwheel rings a bell, detector opens final hatch).

Evidence: [KiwiCo automaton education](https://www.kiwico.com/explore/edu/automaton) describes different cam profiles producing different motion; [Festo pneumatic cylinders](https://www.festo.com/us/en/c/products/actuators-and-drives/pneumatic-cylinders-id_pim135) explains compressed-air actuation and single-acting spring/gravity return. These support the mechanical principles, not our exact game implementation.

## Sound: physical events, readable signals

Sound gameplay runs in the fixed-tick simulation; audio playback only presents it. No microphone permissions, musical knowledge or hearing requirement. Muting, browser audio suspension, camera position and audio voice limits must not change puzzle outcomes.

| Priority | Candidate | Behaviour and puzzle decision |
| --- | --- | --- |
| P1 | Powered speaker | Electrical supply plus activation input; one pulse per rising trigger while powered. Author-set Low/Mid/High band, visible cone direction and restrained expanding arcs. Continuous tone is a later explicit mode. |
| P1 | Sound meter | Needle measures simulated received strength; high threshold activates, lower threshold rearms. Separate sustained threshold state from one-shot crossing so a ringing bell does not count every tick. Switching an electrical load requires real supply. |
| P1 | Wind chimes | Air moves a suspended sail and clapper; actual tube contact emits sound. A ball may strike a tube too. Mere fan-volume overlap must not ring stationary chimes. |
| P1 | Struck bell | Ball, pendulum or shaft-driven striker produces a bounded impact pulse. Contact must separate before retriggering. |
| P2 | Tuned meter | Extends the meter with an explicit tone filter. Engraved shapes/labels distinguish bands without relying on colour or pitch. |
| P2 | Whistle | Airflow through a marked inlet sustains a tone above a threshold; lower release threshold prevents chatter. Distinguish open airflow from pressurized pneumatic tubing. |
| P2 | Listening horn / acoustic duct | Collects sound into typed ports; validated connected ducts carry attenuated, delayed pulses to an exit horn. Ball pipes do not silently acquire acoustic capability. |
| P2 | Resonator | Matching tone builds visible excitation which decays; threshold rings or triggers a contact. Passive resonance does not supply free actuator electricity. |
| P3 | Acoustic screen / reflecting dish | Explicit attenuation/redirection rules. Add only when placement creates a distinct decision beyond horn routing; do not claim full diffraction/interference simulation. |
| P3 | Water-tuned bottle | Fill level selects a marked resonance band; fan across the mouth excites it. Requires conserved fluid and boundary hysteresis. Blown bottles and struck glasses are not interchangeable physical models. |

### Sound puzzle recipes

1. **Across the Gap:** battery + switch → aimed speaker → meter → supplied release gate. Controls: no battery, wrong direction and excessive distance.
2. **Wind in the Tower:** fan swings chime sail → real clapper impact → meter → ball into pipe. The first lesson needs one reliable strike, not precise rhythm.
3. **Three O'Clock at the Mill:** tap → waterwheel → striker → bell → meter → counter. Third event latches the reward and closes the tap; account explicitly for coast-down strikes.
4. **Wait for the Bucket:** filling rope bucket opens an acoustic shutter; delayed speaker pulse must arrive afterwards. Check causal order, not a prescribed arrangement.
5. **Light Gives Wind a Voice:** reflected laser → receiver contact → powered fan → chimes → meter → release. Do not assume laser/solar compatibility unless implemented.
6. **Quiet Corridor:** horn → duct elbow → exit horn routes around a sound-blocking wall. Show actual pulse travel; teach the simplified blocking rule before relying on it.
7. **The Right Voice:** low bell is a distractor; high speaker/tuned meter controls the goal. Matching engraved symbols make the solution equally readable muted.
8. **Fill to Sing:** tap fills bottle to its mid-band; airflow excites it; tuned meter closes the tap and opens a hatch. A leak creates a later timing variant.

### Sound acceptance and evidence

- Typed events carry source, tone, emission tick, strength, duration and propagation identity. Proposed initial receiver rule: strongest arrival, not summed loudness. Add additive intensity only as a separately taught mechanic.
- Simulation owns directionality, distance attenuation, occlusion, transit time and thresholds; audio never feeds these calculations. Use bounded event lifetimes/relay depth and suppress repeat delivery of the same propagated event.
- Test threshold/rearm boundaries, sustained ringing, simultaneous arrivals, wrong tones, unpowered sources, occlusion and loops; Reset clears pending pulses, clapper motion and meter state. Compare muted and audible outcomes.
- Use cream housings/navy marks/gold needles and clappers, with the existing cyan accents. Animate the actual cause and response; optional captions identify tone/source/location. Avoid continuous screen-filling rings.

Sources: [Exploratorium Pipes of Pan](https://annex.exploratorium.edu/xref/exhibits/pipes_of_pan.html) supports tube-length resonance; [Resonant Rings](https://annex.exploratorium.edu/xref/exhibits/resonant_rings.html) shows visible speaker-driven resonance. [Baxter and Hagenbuch's wind-chime teaching experiment](https://leehite.org/documents/Wind_Chimes_Student_Project.pdf) concerns vibrating rods/tubes and suspension, not air-column whistles. [Minecraft Snapshot 23w12a](https://www.minecraft.net/en-us/article/minecraft-snapshot-23w12a) offers an official selective-vibration-sensor precedent; its event categories are not musical pitches. [Bottle resonance experiment](https://arxiv.org/abs/1805.04014) motivates water tuning, not the proposed discrete game bands. [Godot positional audio](https://docs.godotengine.org/en/stable/classes/class_audiostreamplayer3d.html) is a presentation facility; [Xbox Accessibility Guideline 103](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103) supports conveying critical sound through another sensory channel.

## Electrical and optical logic

P1: implement AND, OR, XOR, NOR and NAND in both domains with one pure C# `LogicGateKind` evaluator. Existing Both is AND: forward-refactor it and its callers/content/tests, never add a duplicate alias. Exactly two conditions initially; multi-input XOR parity would make “Exactly one” misleading.

| A | B | Both / AND | Either / OR | Exactly one / XOR | Neither / NOR | Not both / NAND |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 0 | 0 | 0 | 0 | 1 | 1 |
| 0 | 1 | 0 | 1 | 1 | 0 | 1 |
| 1 | 0 | 0 | 1 | 1 | 0 | 1 |
| 1 | 1 | 1 | 1 | 0 | 0 | 0 |

### Contracts and dependencies

- Electrical proposal: A/B condition inputs, separate supply P, switched output Y = P AND Boolean result. Inverted gates cannot create power when their conditions are absent.
- Current ElectricalNetwork accumulates reachable sockets. XOR/NAND/NOR can retract output when another input arrives, so they need a settled nonmonotone solver, not extra enqueue rules. Evaluate acyclic dependencies topologically; preserve source-reachable AND/OR strongly connected components using least fixed points. Explicitly reject zero-delay nonmonotone cycles. Latches/delays are intentional memory boundaries; arbitrary iteration caps must not select a truth result.
- Optical proposal: two independently addressed absorbing control apertures plus a separate carrier input/output. Truth opens the carrier path with documented transmission loss; no carrier means no output, even for NAND/NOR. Control beams never become free output energy.
- Current optical reception aggregates per part and supports one surface. Forward-refactor to typed aperture-addressed receptions; brighter A must never count as B. Update tracing, receivers, diagnostics and tests together.
- Sample fixed-tick conditions and commit settled electrical outputs together. Active optical changes affect the next optical tick; passive mirrors/splitters remain same-trace routing. Document this latency rather than recursively creating light in one trace.
- Proposed detector hysteresis uses separate On/Off thresholds (example 0.25/0.225 in current game units), explicit false initialization and complete Reset. This is a future behavioural change, not an existing feature.
- Electrical fanout remains binary availability, not modeled current/charge. Optical fanout retains finite power, loss, range and trace budgets. Conversion between domains requires an explicit supplied transducer.
- Author/inventory selects operation initially; a free operation dropdown must not bypass part budgets. Distinct cable sockets versus lenses, one/two A/B marks, rear supply/carrier and forward output arrow retain the current toy style. Contextual truth cards supplement icons; no permanent engineering panel.
- P2: explicit edge detector converts a held condition to one pulse. Do not increment counters every tick or replay missed events on power restoration.

### Logic puzzle recipes

1. **Two Banks, One Bridge:** waiting-ball plate AND high-water float permit a supplied bridge.
2. **Either Way Home:** generator condition OR light-detector condition enables a separately supplied exit.
3. **One Passenger Only:** XOR between occupied baskets; either may depart, both loaded fails.
4. **Quiet Lock:** NOR between two sound detectors; both visibly quiet permits a supplied actuator.
5. **Overflow Interlock:** NAND of independent pump-command and high-water condition inhibits the pump. Do not derive command from the gate's own immediate output and accidentally create a forbidden cycle.
6. **Two Light Windows:** shutters overlap A/B illumination; optical AND transmits a third carrier.
7. **Alternating Shadows:** rotating paddles interrupt offset beams; optical XOR passes the carrier only while one path is clear.
8. **Darkness Delivery:** a ball blocks both controls; optical NOR passes a separate carrier to start a hold timer.
9. **Duet Veto:** two tuned sound detectors drive NAND; a leaking bucket offsets one striker. Use explicit supplied sound-to-electrical/optical interfaces.
10. **Water Clock Sampler:** float readiness AND a clock window enables action while a moving mirror aligns.
11. **Light-to-Motion Relay:** optical XOR → supplied receiver → electrical AND with occupied dock → motor lifts shutter.
12. **Three-Lantern Finale:** optical OR permits alternate routes; AND confirms cargo; NOR confirms return lanes clear. Teach latch memory separately before using it here.

### Acceptance and evidence

Test four truth rows × five operations × both domains × supply/carrier present/absent. Include XOR/NAND 10→11 output retraction, reversed insertion/entity order, unequal paths, reconvergence, source removal, monotone cycles and rejected nonmonotone cycles. Optical cases: bright A-only, multiple arrivals at one aperture, independent occlusion, front/back/offset hits, nearest absorption, carrier loss and split thresholds. Reset at every truth row and during hysteresis/latency. Verify real-UI socket selection, Undo, mobile targets and muted play. Defer full difficulty sweeps.

Primary Boolean references: TI [AND](https://www.ti.com/product/SN74HC7001/part-details/SN74HC7001DR), [OR](https://www.ti.com/product/SN74HC32), [XOR](https://www.ti.com/product/SN74HC86/part-details/SN74HC86DT), [NOR](https://www.ti.com/product/SN74HC02), [NAND](https://www.ti.com/lit/ds/symlink/sn74hc00.pdf). Named functions/function tables matter: flattened equations can lose inversion bars. [TI Schmitt-trigger explanation](https://www.ti.com/document-viewer/lit/html/scea046) supports separate rising/falling thresholds.

[All-optical temporal logic research](https://www.nature.com/articles/s41566-024-01483-2) uses a pumped optical state and control interaction; it does not establish that passive mirrors alone implement arbitrary gates. Our carrier gate is a readable game abstraction. [Godot fixed/idle processing](https://docs.godotengine.org/en/stable/tutorials/scripting/idle_and_physics_processing.html) supports separating gameplay timing from visual easing.

[Zachtronics SHENZHEN I/O](https://www.zachtronics.com/shenzhen-io/) provides a logic/memory component and reference-manual precedent; [Factorio Combinators 2.0](https://www.factorio.com/blog/post/fff-384) motivates live visible values and approachable conditions. Borrow local clarity, not programming burden.
