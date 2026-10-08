# Water, acoustic, electrical and optical capability families

Each domain below is one generic capability family inside the single WASM SIMD f32 solver ([capability inventory](gpu-f16-physics.md#capability-inventory), [general data-driven engines](engine-contracts.md#general-data-driven-engines)), added in the slice that first needs it. Every element is declaration data over those records (which capabilities it instantiates, parameters, art and animation bindings); no element has its own solver, kernel branch, equation table or update loop. Individual element obligations are in [named-elements](planning/invest/named-elements.md) and [requirements](planning/requirements.md); companion families: [gas/airflow](finite-gas-foundation.md), [heat](thermal-component-research.md), [radiation](radiation-component-research.md). Slices follow the [roadmap order](planning/invest/vertical-delivery.md#rolling-playable-roadmap): ENGINE-CORE-1/2 precede new elements; P0-035 is the engine-closure release gate at LEGACY-0. Preserve [DESIGN.md](../DESIGN.md): cream/navy/cyan/gold palette, toy-like forms, minimal contextual icons, touch-friendly controls, no CAD panels or permanent network overlays. Closed sets are C# enums; port/parameter identities are typed. These families expand the candidate catalogue; teach each mechanism before its capstone within the 150 progressively taught levels and reserve optional advanced pieces for the workshop.

<a id="water"></a>
## Water capability family

### State variables and laws at puzzle scale

| Record | State | Law |
| --- | --- | --- |
| Finite water store | Volume V (m³), capacity, outlet elevation, open mouth geometry | Conserved: initial volume + explicit external input = contained + in transit + sinks + explicit loss |
| Water network node | Typed port list (mouth, inlet, outlet, cap), sealed/open flag, valve opening, check direction | Sealed pipes carry head uphill; open gutters flow downhill only; a split shares supply, never copies it |
| Head/flow transfer | Elevation difference, opening, conductance | Discharge from a common substep snapshot; shared supply/capacity resolved before commit, so insertion order never selects a branch |
| Free-stream packet | Bounded airborne volume, velocity, capture sweep | Swept against moving mouths so capture never depends on render rate; exceeding the packet budget rejects at authoring, never deletes water |
| Buoyancy/displacement | Displaced volume, hull geometry | Displaced-fluid weight lifts; carried water adds body mass without double counting |
| Siphon/prime state | Dry, Priming, Flowing, Broken | Air entry breaks the column; the receiving surface must be lower |
| Level sensor | High/Low thresholds (separate), dwell in ticks | Float position sampled at substep endpoints |

Floor handling is explicit: shallow catch tray, bounded puddles or visible drains. Mechanical drive from a waterwheel shares the finite power/torque budget of the mechanical family; wheel → pump loops conserve energy, so there are no perpetual fountains. Cosmetic particles never decide capture, success or quantity. Later nudging may ease catch tolerances but never invents supply, ignores a wall, bypasses priming or creates pumping energy. All quantities are canonical IEEE-754 f32 with declared scales; residuals are clamp-or-continue under the [game-grade envelope](gpu-f16-physics.md#game-grade-envelope).

### Per-element declarations

| Element | Capabilities instantiated | Parameters (f32) | Player-observable behaviour | Animation binding |
| --- | --- | --- | --- | --- |
| Finite reservoir / Header tank (EL-001, EL-002) | Finite store + outlet port + open mouth | capacity, initial volume (m³, scale 2⁻⁴), outlet height | Level waterline against etched fill marks; discharge weakens as head drops | Waterline ← committed volume |
| Tap / stopcock (EL-003, EL-165–167) | Network valve node; mechanical lever or solenoid input variants | opening 0–1 | Quarter-turn handle regulates existing supply; actuated variants need their lever or supply, never free water | Handle angle ← committed opening |
| Water pipe kit (EL-008–013) | Sealed network nodes: straight, 45°, 90°, T, cap, nozzle | length | Compatible-mouth snapping with seated collars; a T shares supply | None (static) |
| Open gutter / trough / aqueduct (EL-007) | Open network node with spill edges | length, slope | Downhill collection and flow; cannot act as an uphill pressure pipe | Spill ← overflow |
| Catch basin / Liquid funnel / Drain (EL-004–006) | Finite store with capacity + accounted sink | capacity | Capture up to capacity, explicit overflow; a drain is an accounted sink | Fill ← volume |
| Waterwheel | Hinge rotor + stream receiver + mechanical output port | inertia, paddle count | Stream position/direction gives signed drive to belts/conveyors; discharged water stays available underneath | Paddles ← committed hinge angle |
| Water-carrying / Leaky bucket (EL-014, EL-015) | Dynamic container body + finite store + leak port | capacity, leak rate | Contents add mass; tilt spills; leaks drain visibly into other containers | Tilt/fill ← committed pose and volume |
| Float / Mechanical float valve / Electronic level switch (EL-016–018) | Buoyant body + level sensor (+ valve or electrical output) | High/Low thresholds | Water lifts a visible float; the mechanical valve needs no battery, the electronic contact needs supply | Float ← committed height |
| Check valve / Diverter / Sluice gate (EL-019–021) | Network node with direction, route selection or gating input | opening | One-way flow, route choice, channel gating; closed valves and split branches conserve volume | Flap/blade ← committed state |
| Water pump / Archimedes screw (EL-022, EL-023) | Mechanical or electrical input → head transfer | lift head, rate | Lifts finite water against head; a dry intake is visibly idle | Screw/impeller ← committed rate |
| Primed siphon / Priming bulb (EL-024, EL-168) | Siphon state + network nodes | column height | Dry → Priming → Flowing → Broken; air entry breaks it | Column fill ← state |
| Tipping-bucket water clock (EL-025) | Hinged cup body + finite store + contact trigger | tip volume | Fills, tips a batch, strikes a chime or pulses a counter, returns | Cup ← committed hinge angle |
| Communicating tank / Canal lock (EL-026, EL-027) | Linked stores + sluice nodes + buoyant platform | capacities | Low pipes equalise levels; sluices lift a floating platform in order | Waterlines ← volumes |
| Cork float / Raft / Buoyant platform / Boat (EL-169, EL-170, EL-028, EL-029) | Dynamic body + displacement | hull volume, load | Flotation from displaced volume and load; rising water delivers a ball onto a track | Pose ← committed body |
| Hydraulic piston | Chamber + slider body + displaced-water store | stroke, area | Pressure extends a finite stroke against load; return path required | Rod ← committed slider |
| Flow / Volume / Pressure meter (EL-030–032) | Sensors on a network node | thresholds | Separate moving-water, accumulated-volume and pressure readings; threshold controls do not power actuators | Needle/dial ← committed reading |
| Fluid accumulator (EL-033) | Finite hydraulic energy store with spring/bladder | capacity | Stores and returns bounded hydraulic energy | Bladder ← stored energy |
| Sponge / Wick / Squeeze pad (EL-034, EL-035, EL-171) | Absorbing store + bounded capillary transfer + compression input | absorption capacity | Absorbs finite water (adds mass); compression releases it | Damp fraction ← volume |
| Sprinkler / Rain collector (EL-036, EL-172) | Distributed packet source with finite budget + collection mouth | budget, footprint | Selected-only footprint preview; collection is accounted | Spray ← committed rate |
| Ice plug / Melt gate / Kettle / Condenser | Water store + [heat family](thermal-component-research.md) phase change (TH-15, TH-17, TH-18) | see heat family | Visible steam is not proof of thermal state | See heat bindings |

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

### Chrome-observable acceptance for the first water slice

**First Pour** introduces the family (ELEMENT-n in roadmap order). Through actual palette, gizmo and socket controls in Chrome/Playwright: place tank, tap, gutter and marked bucket; open the tap; Run: the waterline falls in the tank, water flows along the gutter and the bucket fills to its mark (Solved). Controls: closed tap (nothing moves); gutter sloped away (water spills to the tray, bucket stays empty); tank initial volume below the bucket mark (bucket never reaches it, no invented supply). Reset restores volumes, in-flight packets, prime and valve states, wheel state, load and sensor hysteresis exactly; Save/Load where supported restores identical canonical bits. Later slices add empty/full/overflow, equal/reversed head, closed/backflow valves, branching, dry pumps, cycles, tilted capture, source exhaustion and pause/slow playback checks as their elements arrive.

Evidence for the principles: [EPA EPANET](https://www.epa.gov/water-research/epanet) (network vocabulary; not a requirement to integrate it), [DOE: How Hydropower Works](https://www.energy.gov/cmei/water/how-hydropower-works), [NPS: Hopewell waterwheel](https://www.nps.gov/places/000/waterwheel.htm), [NPS: Lowell water power](https://www.nps.gov/lowe/learn/photosmultimedia/water_power.htm), [OpenStax: Buoyancy](https://openstax.org/books/university-physics-volume-1/pages/14-4-archimedes-principle-and-buoyancy), [Science Buddies: Straw siphon](https://www.sciencebuddies.org/stem-activities/straw-siphon), [Factorio: Fluids 2.0](https://www.factorio.com/blog/post/fff-416) and [Drowning in fluids](https://www.factorio.com/blog/post/fff-430), [Disney: Where's My Water?](https://appsupport.disney.com/hc/en-us/articles/360000758626-Getting-Started-with-Where-s-My-Water), [Nintendo: Fluidity—Spin Cycle](https://my.nintendo.com/rewards/54cbade66846a414).

## Complementary mechanical declarations

These compose existing mechanical records (hinges, sliders, springs, finite stores, contact triggers) rather than duplicating gears, bellows, converters or heat entries.

| Element | Capabilities instantiated | Parameters (f32) | Player-observable behaviour | Animation binding |
| --- | --- | --- | --- | --- |
| Cam and follower / Crank | Hinge body with profile collider + slider follower | profile, dwell | Rotation becomes periodic displacement; strikes a bell or opens a tap once per revolution; profile changes dwell, not just looks | Follower ← committed contact |
| Clutch / Brake (EL-055) | Engagement constraint between shafts, controller input | close duration | Engages a powered shaft or stops its load without deleting upstream energy | Plate gap ← committed engagement |
| Ratchet / Escapement (EL-056, EL-057) | One-way hinge limit; stepped release of a stored load | tooth count | Holds gained motion; releases stored weight/spring energy in steps | Pawl ← committed state |
| Bellows, air hose, pneumatic piston | See [gas family](finite-gas-foundation.md) | — | Wheel → cam → bellows → whistle or piston | See gas bindings |
| Air reservoir / Release valve (EL-039, EL-040) | Sealed gas store + valve node | capacity | Stores a bounded charge; bursts after the source stops; open fan stream ≠ sealed compressed air | Gauge/flap ← committed state |
| Electromagnet / Magnetic pickup (EL-060) | Electrical node + force region on compatible material | force, range | Attracts steel while supplied, releases on loss; wooden ball unaffected | Coil lamp ← supply |
| Material/size sorter and escapement feeder | Aperture geometry or powered diverter + one-at-a-time release | aperture | Physical apertures sort actual bodies; no hidden ID-based routing | Gate ← committed state |
| Bimetal thermostat / Melt plug / Expansion actuator | [Heat family](thermal-component-research.md) (TH-22, TH-15, TH-23) | — | Slow accumulation gives delay without a timer | See heat bindings |
| Flywheel / Governor (EL-110) | High-inertia hinge body + speed sensor → valve | inertia, thresholds | Bridges a short supply gap; overspeed changes a valve | Wheel ← committed angle |

Additional recipes: **Polite Factory** (clutch holds conveyor until a receiving bucket is ready); **One Breath** (finite air charge raises piston, then exhaust rings a whistle); **Sorting Office** (magnet removes steel ball, remaining ball reaches a different chute); **Clockwork Rain** (waterwheel cam meters individual drops into a counterweight); **Warm Welcome** (heat releases an ice plug, waterwheel rings a bell, detector opens final hatch). Evidence: [KiwiCo automaton education](https://www.kiwico.com/explore/edu/automaton), [Festo pneumatic cylinders](https://www.festo.com/us/en/c/products/actuators-and-drives/pneumatic-cylinders-id_pim135).

<a id="sound"></a>
## Acoustic capability family

### State variables and laws at puzzle scale

| Record | State | Law |
| --- | --- | --- |
| Acoustic event | Source identity, tone band (Low, Mid, High), emission tick, strength, duration, propagation identity | Typed occurrence emitted by a contact trigger or a powered source; bounded lifetime and relay depth; the same propagated event is never delivered twice |
| Propagation | Direction, distance attenuation, occlusion by bodies, transit time | Computed in the solver from committed geometry; audio playback, mute, camera and browser audio suspension never feed it |
| Acoustic receiver | Received strength, tone filter, On/Off thresholds (separate), sustained versus one-shot mode | Strongest arrival, not summed loudness (additive intensity is a later taught mechanic); a ringing bell does not count every tick |
| Acoustic network node | Horn/duct typed ports | Validated connected ducts carry attenuated, delayed pulses; ball pipes do not silently gain acoustic capability |
| Resonator | Excitation level, matching band, decay | Builds under a matching tone, decays otherwise; passive resonance supplies no electricity |

No microphone permission, musical knowledge or hearing is required: muted and audible outcomes are identical, and captions can identify tone/source/location. All quantities are canonical IEEE-754 f32; residuals clamp-or-continue.

### Per-element declarations

| Element | Capabilities instantiated | Parameters (f32) | Player-observable behaviour | Animation binding |
| --- | --- | --- | --- | --- |
| Pulse speaker (EL-180, CAT-061) | Electrical node + acoustic source (one pulse per rising trigger) | band, strength, cone direction | Pulses while powered on each trigger; restrained expanding arcs | Cone ← committed emission tick |
| Continuous-tone speaker (EL-179) | Electrical node + sustained acoustic source | band, strength | Holds a tone while powered and enabled | Cone ← committed state |
| Sound meter | Electrical node + acoustic receiver | On/Off thresholds | Needle reads received strength; switches a supplied load above On, rearms below Off | Needle ← committed strength |
| Tone-selective sound meter (EL-044) | Receiver + tone filter | band, thresholds | Responds only to its engraved band | Needle ← strength |
| Wind chimes | Suspended sail body + clapper contacts + acoustic source | tube bands | Airflow swings the sail; actual tube contact emits; a ball may strike a tube; mere fan overlap stays silent | Tubes/sail ← committed bodies |
| Struck bell (CAT-009) | Static body + contact trigger + acoustic source | threshold speed | Ball, pendulum or striker impact emits one bounded pulse; contact must separate before re-trigger | Bell swing ← occurrence |
| Air whistle (EL-045) | Airflow receiver + sustained acoustic source | flow threshold, release threshold | Sounds above the flow threshold; open airflow ≠ pressurised tubing | Reed ← committed flow |
| Listening horn / Exit horn / Acoustic duct (EL-046–048) | Acoustic network nodes | attenuation, delay | Collects sound into typed ports and carries it to an exit around a blocking wall | Pulse travel ← committed transit |
| Acoustic resonator (EL-049) | Resonator + contact output | band, threshold | Visible excitation builds under a matching tone then decays; threshold triggers a contact | Excitation ← committed level |
| Acoustic screen / Acoustic dish (EL-050, EL-051) | Occluder / redirector with explicit attenuation | attenuation | Blocks or redirects; no diffraction/interference claim | None |
| Water-tuned bottle (EL-052) | Water store + resonator with level-selected band | fill marks | Fill level selects a marked band; airflow across the mouth excites it | Waterline ← volume |

### Sound puzzle recipes

1. **Across the Gap:** battery + switch → aimed speaker → meter → supplied release gate. Controls: no battery, wrong direction and excessive distance.
2. **Wind in the Tower:** fan swings chime sail → real clapper impact → meter → ball into pipe. The first lesson needs one reliable strike, not precise rhythm.
3. **Three O'Clock at the Mill:** tap → waterwheel → striker → bell → meter → counter. Third event latches the reward and closes the tap; account explicitly for coast-down strikes.
4. **Wait for the Bucket:** filling rope bucket opens an acoustic shutter; delayed speaker pulse must arrive afterwards. Check causal order, not a prescribed arrangement.
5. **Light Gives Wind a Voice:** reflected laser → receiver contact → powered fan → chimes → meter → release. Do not assume laser/solar compatibility unless implemented.
6. **Quiet Corridor:** horn → duct elbow → exit horn routes around a sound-blocking wall. Show actual pulse travel; teach the simplified blocking rule before relying on it.
7. **The Right Voice:** low bell is a distractor; high speaker/tuned meter controls the goal. Matching engraved symbols make the solution equally readable muted.
8. **Fill to Sing:** tap fills bottle to its mid-band; airflow excites it; tuned meter closes the tap and opens a hatch. A leak creates a later timing variant.

### Chrome-observable acceptance for the first acoustic slice

**Across the Gap** introduces the family. Through actual controls in Chrome/Playwright: Battery → Switch → Pulse speaker aimed at a supplied Sound meter → release gate; Run: the meter needle rises on the pulse, the gate opens, the ball reaches the Receiver (Solved). Controls: no battery; speaker facing away; meter beyond range; a Wall between them. The same construction played muted and audible gives identical outcomes. Reset clears pending pulses, clapper motion and meter state; Save/Load where supported restores identical canonical bits. Later slices add threshold/rearm boundaries, sustained ringing, simultaneous arrivals, wrong tones, unpowered sources, occlusion and loops.

Evidence: [Exploratorium Pipes of Pan](https://annex.exploratorium.edu/xref/exhibits/pipes_of_pan.html), [Resonant Rings](https://annex.exploratorium.edu/xref/exhibits/resonant_rings.html), [Baxter and Hagenbuch wind-chime experiment](https://leehite.org/documents/Wind_Chimes_Student_Project.pdf) (vibrating tubes, not air-column whistles), [Minecraft Snapshot 23w12a](https://www.minecraft.net/en-us/article/minecraft-snapshot-23w12a) (selective vibration sensor precedent), [Bottle resonance experiment](https://arxiv.org/abs/1805.04014), [Godot positional audio](https://docs.godotengine.org/en/stable/classes/class_audiostreamplayer3d.html) (presentation only), [Xbox Accessibility Guideline 103](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/103).

<a id="logic"></a>
## Electrical and optical logic families

### State variables and laws at puzzle scale

| Record | State | Law |
| --- | --- | --- |
| Electrical network node | Supply reachability (binary), condition inputs A/B, output Y | Fanout is binary availability, not modelled current; conversion to another domain needs an explicit supplied transducer |
| Logic gate (`LogicGateKind`: And, Or, Xor, Nor, Nand) | Two condition inputs, separate supply P | Y = P AND f(A, B); inverted gates cannot create power when their conditions are absent; Both is And, forward-refactored with its callers, never aliased |
| Network settlement | Acyclic dependencies evaluated topologically; source-reachable And/Or strongly connected components by least fixed point | Xor/Nand/Nor retract output when another input arrives (10 → 11), so the compiled network solves to a settled state each tick; zero-delay nonmonotone cycles reject at compile; latches/delays are the only memory boundaries |
| Optical gate | Two independently addressed absorbing control apertures + separate carrier input/output | Truth opens the carrier path with declared transmission loss; no carrier means no output even for Nand/Nor; control beams never become output energy; brighter A never counts as B |
| Optical latency | Active changes affect the next optical tick; passive mirrors/splitters route within the same trace | Documented one-tick latency instead of recursive light in one trace |
| Detector hysteresis | Separate On/Off thresholds (example 0.25/0.225 game units), explicit false initialisation, complete Reset | Sampled at substep endpoints |
| Edge detector (EL-181, EL-182) | Rising/falling edge → one pulse | Counters never increment every tick; missed events are not replayed on power restoration |

| A | B | Both / AND | Either / OR | Exactly one / XOR | Neither / NOR | Not both / NAND |
| --- | --- | --- | --- | --- | --- | --- |
| 0 | 0 | 0 | 0 | 0 | 1 | 1 |
| 0 | 1 | 0 | 1 | 1 | 0 | 1 |
| 1 | 0 | 0 | 1 | 1 | 0 | 1 |
| 1 | 1 | 1 | 1 | 0 | 0 | 0 |

Exactly two conditions initially; multi-input XOR parity would make "Exactly one" misleading. Optical fanout retains finite power, loss, range and trace budgets. Author/inventory selects the operation; a free dropdown cannot bypass part budgets.

### Per-element declarations

| Element | Capabilities instantiated | Parameters (f32) | Player-observable behaviour | Animation binding |
| --- | --- | --- | --- | --- |
| Electrical AND/OR/XOR/NOR/NAND gate (EL-133–137) | Electrical node (A, B, supply P, output Y) + `LogicGateKind` | kind | Distinct cable sockets, one/two A/B marks, rear supply, forward output arrow; contextual truth card | Output lamp ← committed Y |
| Optical AND/OR/XOR/NOR/NAND gate (EL-138–142) | Two control apertures + carrier aperture + `LogicGateKind` | kind, transmission loss | Lenses for controls, rear carrier, forward output; carrier passes only on truth | Output glow ← committed carrier |
| Rising-edge / Falling-edge detector (EL-181, EL-182) | Electrical node + edge → pulse | — | One pulse per held-condition change | Lamp ← pulse |
| Optical combiner (EL-213) | Passive same-trace routing with typed aperture lists and per-port snapshot commit | — | Combines independent arrivals without one counting as another | None |

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

### Chrome-observable acceptance for the first logic slice

**Two Banks, One Bridge** introduces the electrical gate family. Through actual socket selection in Chrome/Playwright: wire a plate condition and a float condition into an AND gate's A/B, a Battery into its supply, and its output into a bridge; Run: the bridge lowers only when both conditions hold and the ball crosses (Solved). Controls: either condition alone (bridge stays up); supply removed with both conditions true (no output); reversed socket insertion order gives the same result. Reset at every truth row restores the exact construction; Undo, mobile targets and muted play work. Later slices cover four truth rows × five operations × both domains × supply/carrier present/absent, 10 → 11 output retraction, unequal paths, reconvergence, source removal, monotone cycles and compile-time rejection of nonmonotone cycles, plus optical bright-A-only, multiple arrivals, independent occlusion, front/back/offset hits, nearest absorption, carrier loss and split thresholds.

Evidence: TI [AND](https://www.ti.com/product/SN74HC7001/part-details/SN74HC7001DR), [OR](https://www.ti.com/product/SN74HC32), [XOR](https://www.ti.com/product/SN74HC86/part-details/SN74HC86DT), [NOR](https://www.ti.com/product/SN74HC02), [NAND](https://www.ti.com/lit/ds/symlink/sn74hc00.pdf) function tables (flattened equations can lose inversion bars); [TI Schmitt-trigger explanation](https://www.ti.com/document-viewer/lit/html/scea046) for separate rising/falling thresholds; [all-optical temporal logic research](https://www.nature.com/articles/s41566-024-01483-2) (passive mirrors alone do not implement arbitrary gates; the carrier gate is a game abstraction); [Godot fixed/idle processing](https://docs.godotengine.org/en/stable/tutorials/scripting/idle_and_physics_processing.html); [Zachtronics SHENZHEN I/O](https://www.zachtronics.com/shenzhen-io/) and [Factorio Combinators 2.0](https://www.factorio.com/blog/post/fff-384) for local clarity without programming burden.
