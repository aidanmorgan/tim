# Palette allocation (23 November 2026)

Allocated by the cross-batch compatibility audit (`docs/planning/elements/readiness/audits/cross-batch.md`) and adopted by coordinator decision 36, 23 November 2026, with one change: orders 223 and 323 stay reserved while EL-223 is paused (owner decision 24g), so the Power block skips 323 (section 4). Sorted against the first-teaching levels of campaign coverage section 6 after the campaign-ordering repair of 23 November 2026 (coordinator decisions 35 and 36). Every undelivered placeable part's spec states its number from this file; each delivering slice writes that number into its package declaration. Owner decisions 24a and 24b (23 November 2026) govern it.

## 1. Rule

1. **Delivered orders are unchanged.** The 45 delivered packages keep the orders their declarations state (section 2).
2. **Grouped by palette category.** `ui/Workshop.cs:419` lists the palette grouped by `ElementCategory` in enum order, and inside a group in registry order, which is ascending `PaletteOrder` (`engine/PartRegistry.cs:95`). The bundle validator requires every order to be positive and unique across all packages (`CuriousContraptions.Bundle/BundleValidator.cs:25`, `:473`), so an order only decides a part's place inside its own category.
3. **New parts in campaign first-use order** (owner decision 24a), from the per-level allocation of [campaign coverage section 6](campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f). A lettered level sorts after its number (24a after 24, before 25). Ties at one level go by identity number, in ledger order CAT, EL, TH, RAD, GAP; the order in which a level lists its parts does not change a number.
4. **One block of numbers per category**, so a later identity can be appended to its category without renumbering any other: Motion 101–199, Goals 201–299, Power 301–399, Control 401–499, Structure 501–599, Optics 601–699, Radiation 701–799. Inside a block the numbers run consecutively in first-use order. The gaps among delivered orders (13, 14 and 43 to 63) are left unused: no document reserves them, and filling them would set new parts between delivered parts for no stated reason.
5. **Consequence, stated rather than hidden.** Because delivered orders are fixed and every new block is above 68, a new part always lists after every delivered part of its category, even when the campaign teaches it first (for example the Pool ball, first used at level 8, lists after the Baseball, order 68).
6. **One number per package.** A named identity that shares its CAT part's package takes the CAT number (EL-210 with CAT-029, EL-211 with CAT-059, EL-180 with CAT-061, EL-212 to EL-214 and EL-138 to EL-152 with their CAT parts).
7. **No number for a non-placeable entry** (owner decision 24b): companion packages (`sealed_can`, `toy_fish`, `glow_fish`, `toast_slice`, `balloon_cargo`, `breakable_block` and its fragments); connection kinds and materials (EL-038 hose, EL-048 acoustic duct, EL-067 steel cable, EL-197 wire, EL-200 drive belt, EL-201 drive chain, EL-205 rope); level data (EL-120 to EL-125); Duplicate closures and refinements of a delivered package (EL-133 to EL-137, EL-165, EL-187 to EL-196, EL-198, EL-199, EL-202 to EL-204, EL-206, EL-208, EL-209); a later criterion on another package (TH-05 on `mechanical_brake`). EL-064's reserved id `metal_loop_anchor` gets a number only if its closure check fails; it would then take the next free Motion number.

**Inputs read.** Delivered declarations under `elements/*/` at published main `25df3cd5`; `campaign-coverage.md` section 6 after the campaign-ordering repair of 23 November 2026, which states every first-teaching level (catalogue parts by name, named identities by id); the specs' declared categories. `elements/elements.bundle.json` is untracked build output and lacks the Baseball, so it was not used.

## 2. Delivered orders (unchanged)

| Category | Orders (package) |
| --- | --- |
| Motion | 1 `ball`, 7 `bumper`, 9 `domino`, 10 `bowling`, 12 `spring`, 15 `pipe`, 17 `pipe_bend_45`, 18 `pipe_bend_90`, 19 `funnel`, 20 `powered_gate`, 31 `weight`, 32 `rope_anchor`, 33 `pulley`, 34 `impact_lever`, 35 `trampoline`, 37 `conveyor`, 39 `reverse_transmission`, 41 `wound_spring`, 42 `cannon`, 64 `tennis`, 65 `balloon`, 68 `baseball` |
| Goals | 2 `basket`, 4 `lamp` |
| Power | 3 `switch`, 11 `battery`, 36 `motor`, 38 `clutch`, 40 `linear_pusher`, 66 `fan`, 67 `windmill` |
| Control | 6 `delay`, 16 `ball_detector`, 21 `pressure_plate`, 22 `counter`, 23 `clock`, 24 `latch`, 25 `hold_timer`, 26 `both_gate`, 27 `electrical_nand`, 28 `electrical_nor`, 29 `electrical_or`, 30 `electrical_xor` |
| Structure | 5 `wall`, 8 `ramp` |

## 3. Categories

`ElementCategory` today has Motion, Goals, Power, Control and Structure (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`). The Optics member is owned by `ENGINE-OPTICAL` ("the optical palette category and domain flag"; its *Deletes* line removes the closed set without optics). The Radiation member is owned by `ENGINE-RADIATION` ("the palette category and domain flag"). No story adds a Water, Air, Sound or Heat member, and the liquid, pneumatic, acoustic and thermal specs place their parts in the existing five by function; the light sources outside the laser family (Flashlight, Rope-operated light, Light-charge receiver, Solar panel) are in Power while the lasers are in Optics. Coordinator decision 36 keeps this: the existing function categories, with Optics and Radiation added by their engine stories; each spec's stated category is used.

Category corrections applied here: EL-185 is Structure (its catalogue line); CAT-007 takes 604, not the earlier "order 47".

**EL-223 is paused with its own order.** The session that was delivering it (worktree `tim-cluster2-after-baseball`, based on `fa86969f`, uncommitted) declares id `passive_impact_scoop`, directory `elements/PassiveImpactScoop/` and `PaletteOrder: 223`, Motion; the owner paused it until the engine programme lands (owner decision 24g). Orders 223 and 323 stay reserved while it is paused (coordinator decision 36), so no other package takes either: the Power block skips 323, and 223 lies in the Goals block's unused range. If EL-223 publishes with 223 it keeps it (a published order is not moved) and lists after every other new Motion part; order 130 below is then left unused. If its slice adopts 130 before it publishes, 130 is its number and 223 and 323 are released (323 then stays unused until a re-sort).

## 4. Allocation

269 placeable packages: 28 undelivered CAT parts, 240 named-identity packages and EL-242 (45° metal bend, package `metal_pipe_bend_45`, declared in [EL-242](../EL-242-45-metal-ball-pipe-bend.md)).

### Motion (80 new; orders 101 to 180)

| Order | Identity | Part | Package | First use |
| --- | --- | --- | --- | --- |
| 101 | [EL-076](../EL-076-programmable-ball.md) | Programmable ball | `programmable_ball` | 2b |
| 102 | [EL-077](../EL-077-soccer-ball.md) | Soccer ball | `soccer` | 2b |
| 103 | [EL-098](../EL-098-pool-ball.md) | Pool ball | `pool_ball` | 8 |
| 104 | [EL-215](../EL-215-damped-cushion.md) | Damped cushion | `cushion` | 8a |
| 105 | [EL-216](../EL-216-capture-cradle.md) | Capture cradle | `cradle` | 8a |
| 106 | [EL-065](../EL-065-load-hook.md) | Load hook | `load_hook` | 17 |
| 107 | [EL-207](../EL-207-moving-pulley.md) | Moving pulley | `moving_pulley` | 17 |
| 108 | [EL-069](../EL-069-moving-bucket.md) | Moving bucket (display name "Hanging bucket") | `moving_bucket` | 17a |
| 109 | [EL-070](../EL-070-moving-cage.md) | Moving cage | `moving_cage` | 17a |
| 110 | [EL-066](../EL-066-scissors.md) | Scissors | `scissors` | 17b |
| 111 | [EL-116](../EL-116-passive-wheel.md) | Passive wheel | `wheel` | 18 |
| 112 | [EL-102](../EL-102-large-bore-ball-pipe.md) | Large-bore ball pipe | `pipe_large` | 24a |
| 113 | [EL-162](../EL-162-fixed-ball-diverter.md) | Fixed ball diverter | `diverter_fixed` | 26a |
| 114 | [EL-241](../EL-241-ball-hopper.md) | Ball hopper with one-ball escapement | `ball_hopper` | 26a |
| 115 | [EL-163](../EL-163-powered-ball-diverter.md) | Powered ball diverter | `diverter_powered` | 27 |
| 116 | [EL-164](../EL-164-alternating-ball-diverter.md) | Alternating ball diverter | `diverter_alternating` | 27 |
| 117 | [EL-184](../EL-184-mechanical-ball-gate.md) | Mechanical ball gate | `mechanical_gate` | 28a |
| 118 | [EL-107](../EL-107-spiral-delay-tube.md) | Spiral gravity-delay tube | `spiral_tube` | 28b |
| 119 | [EL-109](../EL-109-speed-trapdoor.md) | Speed-sensitive trapdoor | `speed_trapdoor` | 28b |
| 120 | [EL-238](../EL-238-selective-trap-door.md) | Selective trap door | `trap_door` | 28c |
| 121 | [EL-232](../EL-232-gears.md) | Gears | `gear` | 33 |
| 122 | [EL-053](../EL-053-rotary-to-linear-converter.md) | Rotary-to-linear converter | `rotary_to_linear_converter` | 35 |
| 123 | [EL-054](../EL-054-linear-to-rotary-converter.md) | Linear-to-rotary converter | `linear_to_rotary_converter` | 35 |
| 124 | [EL-218](../EL-218-crank-slider.md) | Crank-slider | `crank_slider` | 36 |
| 125 | [EL-219](../EL-219-rack-and-pinion-slide.md) | Rack-and-pinion slide | `rack_slide` | 36 |
| 126 | [EL-156](../EL-156-single-lobe-cam.md) | Single-lobe cam | `cam_single` | 36a |
| 127 | [EL-157](../EL-157-double-lobe-cam.md) | Double-lobe cam | `cam_double` | 36a |
| 128 | [EL-158](../EL-158-rise-hold-fall-cam.md) | Rise-hold-fall cam | `cam_dwell` | 36a |
| 129 | [EL-235](../EL-235-jack-in-the-box.md) | Jack-in-the-box | `jack_in_the_box` | 42 |
| 130 | [EL-223](../EL-223-passive-impact-scoop.md) | Passive impact scoop (paused, owner decision 24g; its paused declaration holds 223; section 3) | `passive_impact_scoop` | 42a |
| 131 | [EL-224](../EL-224-counterweighted-scoop-catapult.md) | Counterweighted scoop catapult | `scoop_catapult` | 42a |
| 132 | [EL-225](../EL-225-latched-slingshot-cradle.md) | Latched slingshot cradle | `slingshot` | 44 |
| 133 | [EL-226](../EL-226-hinged-elastic-catapult.md) | Hinged elastic catapult | `elastic_catapult` | 44 |
| 134 | [EL-091](../EL-091-cannonball.md) | Cannonball | `cannonball` | 45 |
| 135 | [EL-217](../EL-217-reload-shuttle.md) | Reload shuttle with chamber interlock | `reload_shuttle` | 45a |
| 136 | [EL-227](../EL-227-magnetic-impulse-relay.md) | Magnetic impulse relay | `impulse_relay` | 46 |
| 137 | [EL-063](../EL-063-cable-winch.md) | Cable winch | `cable_winch` | 47 |
| 138 | [EL-220](../EL-220-docking-lift.md) | Docking lift | `docking_lift` | 47a |
| 139 | [EL-221](../EL-221-parallel-gripper.md) | Parallel gripper | `parallel_gripper` | 48 |
| 140 | [EL-228](../EL-228-zipline-signal-carriage.md) | Zipline signal carriage | `zipline` | 48b |
| 141 | [EL-071](../EL-071-straight-metal-ball-pipe.md) | Straight metal ball pipe | `metal_pipe` | 52 |
| 142 | [EL-072](../EL-072-curved-metal-ball-pipe.md) | 90° metal bend | `metal_pipe_bend_90` | 52 |
| 143 | EL-242 | 45° metal bend | `metal_pipe_bend_45` | 52 |
| 144 | [TH-03](../TH-03-combustible-block.md) | Combustible block | `combustible_block` | 58 |
| 145 | [TH-32](../TH-32-tinder-pad.md) | Tinder pad | `tinder_pad` | 58 |
| 146 | [EL-016](../EL-016-float.md) | Float | `float` | 63 |
| 147 | [EL-014](../EL-014-water-carrying-bucket.md) | Water-carrying bucket (display name "Water bucket") | `water_bucket` | 63a |
| 148 | [EL-015](../EL-015-leaky-bucket.md) | Leaky bucket | `leaky_bucket` | 63a |
| 149 | [EL-028](../EL-028-buoyant-platform.md) | Buoyant platform | `float_deck` | 66a |
| 150 | [TH-14](../TH-14-ice-block.md) | Ice block | `ice_block` | 67 |
| 151 | [EL-029](../EL-029-boat.md) | Boat | `boat` | 67a |
| 152 | [EL-169](../EL-169-cork-float.md) | Cork float | `cork_float` | 68a |
| 153 | [EL-170](../EL-170-raft.md) | Raft | `raft` | 68a |
| 154 | [EL-229](../EL-229-hydraulic-piston.md) | Hydraulic piston | `hydraulic_piston` | 69a |
| 155 | [TH-17](../TH-17-kettle.md) | Kettle | `kettle` | 69c |
| 156 | [EL-230](../EL-230-spring-return-pneumatic-piston.md) | Spring-return pneumatic piston | `pneumatic_piston` | 74b |
| 157 | [EL-231](../EL-231-double-acting-pneumatic-piston.md) | Double-acting pneumatic piston | `double_acting_piston` | 74b |
| 158 | [TH-19](../TH-19-steam-piston.md) | Steam piston | `steam_piston` | 75 |
| 159 | [TH-25](../TH-25-hot-air-balloon.md) | Hot-air balloon | `hot_air_balloon` | 79b |
| 160 | [EL-056](../EL-056-ratchet.md) | Ratchet | `ratchet` | 81 |
| 161 | [EL-057](../EL-057-escapement.md) | Escapement | `escapement` | 81 |
| 162 | [EL-061](../EL-061-indexed-carousel.md) | Indexed carousel | `indexed_carousel` | 82 |
| 163 | [EL-110](../EL-110-flywheel.md) | Flywheel | `flywheel` | 83 |
| 164 | [EL-111](../EL-111-centrifugal-governor.md) | Centrifugal governor | `governor` | 83 |
| 165 | [TH-35](../TH-35-coffee-pot-steam-vessel.md) | Coffee-pot steam vessel | `coffee_pot` | 86 |
| 166 | [EL-034](../EL-034-sponge.md) | Sponge | `sponge` | 87 |
| 167 | [EL-080](../EL-080-programmable-box.md) | Programmable box | `programmable_box` | 92 |
| 168 | [EL-096](../EL-096-tipsy-platform.md) | Tipsy platform | `tipsy_platform` | 92 |
| 169 | [EL-089](../EL-089-gravity-effect-pad.md) | Gravity-effect pad | `gravity_pad` | 93a |
| 170 | [EL-093](../EL-093-toy-dynamite-charge.md) | Toy dynamite charge | `toy_dynamite` | 95 |
| 171 | [EL-106](../EL-106-impact-sensitive-toy-charge.md) | Impact-sensitive toy charge | `shock_charge` | 95a |
| 172 | [EL-092](../EL-092-toy-rocket.md) | Toy rocket | `toy_rocket` | 96 |
| 173 | [EL-095](../EL-095-toy-revolver.md) | Toy revolver | `toy_revolver` | 96 |
| 174 | [EL-104](../EL-104-toy-firework.md) | Toy firework | `toy_firework` | 96a |
| 175 | [EL-082](../EL-082-lured-character.md) | Lured character | `lured_character` | 97 |
| 176 | [EL-083](../EL-083-escaping-character.md) | Escaping character | `escaping_character` | 97 |
| 177 | [EL-087](../EL-087-predator-character.md) | Predator character | `predator_character` | 97 |
| 178 | [EL-105](../EL-105-toy-missile.md) | Toy missile | `toy_missile` | 97a |
| 179 | [EL-086](../EL-086-obstacle-reversing-walker.md) | Obstacle-reversing walker | `walker` | 98 |
| 180 | [EL-090](../EL-090-steerable-blimp.md) | Steerable blimp | `blimp` | 98a |

### Goals (4 new; orders 201 to 204)

| Order | Identity | Part | Package | First use |
| --- | --- | --- | --- | --- |
| 201 | [EL-099](../EL-099-pool-pocket.md) | Pool pocket | `pool_pocket` | 8 |
| 202 | [TH-37](../TH-37-heat-sensitive-target.md) | Heat-sensitive target | `heat_target` | 15 |
| 203 | [EL-084](../EL-084-fragile-fish-bowl.md) | Fragile fish bowl | `fish_bowl` | 97b |
| 204 | [EL-088](../EL-088-fish-tank-lure.md) | Fish tank lure | `fish_tank` | 97b |

### Power (44 new; orders 301 to 322 and 324 to 345; 323 reserved while EL-223 is paused)

| Order | Identity | Part | Package | First use |
| --- | --- | --- | --- | --- |
| 301 | [TH-04](../TH-04-electrical-heating-plate.md) | Electrical heating plate | `heating_plate` | 14 |
| 302 | [EL-068](../EL-068-tin-snips.md) | Tin snips | `tin_snips` | 17b |
| 303 | [EL-100](../EL-100-vacuum-nozzle.md) | Vacuum nozzle | `vacuum_nozzle` | 27a |
| 304 | [EL-108](../EL-108-powered-airlift.md) | Powered airlift | `airlift` | 28c |
| 305 | [EL-055](../EL-055-mechanical-brake.md) | Mechanical brake (TH-05 is a criterion on it) | `mechanical_brake` | 33a |
| 306 | [EL-233](../EL-233-generator.md) | Generator | `generator` | 34 |
| 307 | [EL-234](../EL-234-mouse-motor.md) | Mouse motor | `mouse_motor` | 34a |
| 308 | [GAP-04](../GAP-04-driven-wheel.md) | Driven wheel | `driven_wheel` | 37 |
| 309 | [EL-237](../EL-237-boxing-glove.md) | Boxing glove | `boxing_glove` | 41b |
| 310 | [EL-222](../EL-222-pinball-flipper.md) | Supplied pinball flipper | `flipper` | 42 |
| 311 | [EL-097](../EL-097-pool-cue.md) | Pool cue | `pool_cue` | 44a |
| 312 | [EL-060](../EL-060-electromagnet.md) | Electromagnet | `electromagnet` | 46 |
| 313 | [EL-062](../EL-062-docking-ferry.md) | Docking ferry | `docking_ferry` | 47a |
| 314 | [EL-103](../EL-103-accelerator-tube.md) | Accelerator tube | `accelerator_tube` | 48a |
| 315 | [CAT-029](../CAT-029-flashlight.md) (also EL-210) | Flashlight | `flashlight` | 51 |
| 316 | [CAT-059](../CAT-059-solar_panel.md) (also EL-211) | Solar panel | `solar_panel` | 51a |
| 317 | [EL-155](../EL-155-rope-operated-light.md) | Rope-operated light | `pull_cord_light` | 51d |
| 318 | [EL-154](../EL-154-light-charge-receiver.md) | Light-charge receiver | `light_charge_receiver` | 53a |
| 319 | [TH-07](../TH-07-solar-absorber-plate.md) | Solar absorber plate | `solar_absorber` | 55 |
| 320 | [TH-01](../TH-01-candle.md) | Candle | `candle` | 56 |
| 321 | [TH-02](../TH-02-fire-bowl.md) | Fire bowl | `fire_bowl` | 57 |
| 322 | [TH-31](../TH-31-flint-striker.md) | Flint striker | `flint_striker` | 58 |
| 323 | — | reserved while EL-223 is paused (coordinator decision 36; section 3) | — | — |
| 324 | [EL-001](../EL-001-finite-reservoir.md) | Finite reservoir | `reservoir` | 61 |
| 325 | [EL-002](../EL-002-header-tank.md) | Header tank | `header_tank` | 61a |
| 326 | [EL-022](../EL-022-water-pump.md) | Water pump | `water_pump` | 65 |
| 327 | [EL-023](../EL-023-archimedes-screw.md) | Archimedes screw | `water_screw` | 65 |
| 328 | [EL-240](../EL-240-waterwheel.md) | Waterwheel | `waterwheel` | 65b |
| 329 | [EL-026](../EL-026-communicating-tank.md) | Communicating tank | `linked_tank` | 66a |
| 330 | [CAT-010](../CAT-010-bellows.md) | Bellows | `bellows` | 71 |
| 331 | [EL-039](../EL-039-air-reservoir.md) | Air reservoir | `air_reservoir` | 71 |
| 332 | [EL-042](../EL-042-air-nozzle.md) | Air nozzle | `air_nozzle` | 73a |
| 333 | [EL-037](../EL-037-air-compressor.md) | Air compressor | `air_compressor` | 74a |
| 334 | [TH-20](../TH-20-steam-turbine.md) | Steam turbine | `steam_turbine` | 76 |
| 335 | [TH-12](../TH-12-reversible-heat-pump.md) | Reversible heat pump | `heat_pump` | 78 |
| 336 | [EL-033](../EL-033-fluid-accumulator.md) | Fluid accumulator | `accumulator` | 84 |
| 337 | [TH-33](../TH-33-spring-mounted-match.md) | Spring-mounted match | `spring_match` | 85 |
| 338 | [TH-36](../TH-36-lamp-trigger-apparatus.md) | Lamp-trigger apparatus | `trigger_lamp` | 86 |
| 339 | [TH-30](../TH-30-thermoelectric-generator.md) | Thermoelectric generator | `thermoelectric_generator` | 87 |
| 340 | [TH-34](../TH-34-timed-toaster-ejector.md) | Timed toaster ejector | `toaster_ejector` | 91 |
| 341 | [EL-078](../EL-078-can-opener.md) | Can opener | `can_opener` | 93 |
| 342 | [EL-079](../EL-079-electric-mixer.md) | Electric mixer | `mixer` | 93a |
| 343 | [GAP-09](../GAP-09-fragmentation-station.md) | Fragmentation station | `fragmentation_station` | 95 |
| 344 | [EL-085](../EL-085-rope-driven-character-wheel.md) | Rope-driven character wheel | `character_wheel` | 98a |
| 345 | [RAD-20](../RAD-20-radioisotope-thermoelectric-generator.md) | Radioisotope thermoelectric generator | `radioisotope_thermoelectric_generator` | 131 |

### Control (37 new; orders 401 to 437)

| Order | Identity | Part | Package | First use |
| --- | --- | --- | --- | --- |
| 401 | [TH-21](../TH-21-temperature-sensor.md) | Temperature sensor | `temperature_sensor` | 15 |
| 402 | [EL-059](../EL-059-weight-tray.md) | Weight tray | `weight_tray` | 25 |
| 403 | [EL-183](../EL-183-resettable-counter.md) | Resettable counter | `reset_counter` | 26 |
| 404 | [EL-236](../EL-236-egg-timer.md) | Egg timer | `egg_timer` | 28 |
| 405 | [EL-181](../EL-181-rising-edge-detector.md) | Rising-edge detector | `rising_edge` | 32b |
| 406 | [EL-182](../EL-182-falling-edge-detector.md) | Falling-edge detector | `falling_edge` | 32b |
| 407 | [EL-003](../EL-003-tap.md) (also EL-165) | Tap | `tap` | 61 |
| 408 | [EL-017](../EL-017-mechanical-float-valve.md) | Mechanical float valve | `float_valve` | 63 |
| 409 | [EL-018](../EL-018-electronic-level-switch.md) | Electronic level switch | `level_switch` | 63 |
| 410 | [EL-166](../EL-166-mechanically-actuated-tap.md) | Mechanically actuated tap | `lever_tap` | 64 |
| 411 | [EL-167](../EL-167-solenoid-tap.md) | Solenoid tap | `solenoid_tap` | 64 |
| 412 | [EL-019](../EL-019-water-check-valve.md) | Water check valve | `check_valve` | 64a |
| 413 | [EL-020](../EL-020-water-diverter.md) | Water diverter | `water_diverter` | 64a |
| 414 | [EL-021](../EL-021-sluice-gate.md) | Sluice gate | `sluice_gate` | 64a |
| 415 | [EL-168](../EL-168-siphon-priming-bulb.md) | Siphon priming bulb | `priming_bulb` | 65a |
| 416 | [EL-025](../EL-025-tipping-bucket-water-clock.md) | Tipping-bucket water clock | `tipping_bucket` | 65b |
| 417 | [EL-030](../EL-030-flow-meter.md) | Flow meter | `flow_meter` | 69b |
| 418 | [EL-031](../EL-031-volume-meter.md) | Volume meter | `volume_meter` | 69b |
| 419 | [EL-032](../EL-032-pressure-meter.md) | Pressure meter | `pressure_meter` | 69b |
| 420 | [EL-040](../EL-040-pneumatic-release-valve.md) | Pneumatic release valve | `release_valve` | 72 |
| 421 | [EL-043](../EL-043-pneumatic-pressure-gauge.md) | Pneumatic pressure gauge | `pressure_gauge` | 72 |
| 422 | [EL-041](../EL-041-pneumatic-directional-valve.md) | Pneumatic directional valve | `directional_valve` | 74b |
| 423 | [CAT-009](../CAT-009-bell.md) | Bell | `bell` | 75a |
| 424 | [CAT-060](../CAT-060-sound_meter.md) | Sound meter | `sound_meter` | 75a |
| 425 | [CAT-061](../CAT-061-speaker.md) (also EL-180) | Speaker | `speaker` | 75b |
| 426 | [CAT-069](../CAT-069-wind_chimes.md) | Wind chimes | `wind_chimes` | 75b |
| 427 | [EL-044](../EL-044-tone-selective-sound-meter.md) | Tone-selective sound meter | `tuned_sound_meter` | 76a |
| 428 | [EL-179](../EL-179-continuous-tone-speaker.md) | Continuous-tone speaker | `tone_speaker` | 76a |
| 429 | [EL-045](../EL-045-air-whistle.md) | Air whistle | `air_whistle` | 77a |
| 430 | [EL-052](../EL-052-water-tuned-bottle.md) | Water-tuned bottle | `tuned_bottle` | 77a |
| 431 | [EL-049](../EL-049-acoustic-resonator.md) | Acoustic resonator | `acoustic_resonator` | 78a |
| 432 | [EL-046](../EL-046-listening-horn.md) | Listening horn | `listening_horn` | 79a |
| 433 | [EL-047](../EL-047-exit-horn.md) | Exit horn | `exit_horn` | 79a |
| 434 | [TH-22](../TH-22-bimetal-thermostat.md) | Bimetal thermostat | `bimetal_thermostat` | 83 |
| 435 | [EL-075](../EL-075-toy-pulse-emitter.md) | Toy pulse emitter | `pulse_emitter` | 91a |
| 436 | [EL-081](../EL-081-message-display.md) | Message display | `message_display` | 91a |
| 437 | [EL-094](../EL-094-detonation-plunger.md) | Detonation plunger | `plunger` | 95 |

### Structure (50 new; orders 501 to 550)

| Order | Identity | Part | Package | First use |
| --- | --- | --- | --- | --- |
| 501 | [EL-112](../EL-112-structural-beam.md) | Structural beam | `structural_beam` | 4 |
| 502 | [EL-073](../EL-073-brick-barrier.md) | Brick barrier | `brick_barrier` | 7a |
| 503 | [EL-074](../EL-074-wood-barrier.md) | Wood barrier | `wood_barrier` | 7a |
| 504 | [EL-114](../EL-114-placeable-pivot.md) | Placeable pivot | `pivot` | 9 |
| 505 | [EL-115](../EL-115-linkage-connector.md) | Linkage connector | `linkage` | 9 |
| 506 | [EL-113](../EL-113-structural-brace.md) | Structural brace | `structural_brace` | 9b |
| 507 | [EL-101](../EL-101-thumb-tack.md) | Thumb tack | `thumb_tack` | 13 |
| 508 | [TH-08](../TH-08-heat-conducting-bar.md) | Heat-conducting bar | `heat_bar` | 16 |
| 509 | [EL-117](../EL-117-axle.md) | Axle | `axle` | 18 |
| 510 | [TH-09](../TH-09-insulating-panel.md) | Insulating panel | `insulating_panel` | 19 |
| 511 | [EL-058](../EL-058-size-grate.md) | Size grate | `size_grate` | 28a |
| 512 | [TH-10](../TH-10-finned-heat-sink.md) | Finned heat sink | `heat_sink` | 39 |
| 513 | [EL-159](../EL-159-tension-limited-connector.md) | Tension-limited connector | `tension_connector` | 43 |
| 514 | [EL-160](../EL-160-shear-limited-connector.md) | Shear-limited connector | `shear_connector` | 43 |
| 515 | [EL-161](../EL-161-bending-limited-connector.md) | Bending-limited connector | `bending_connector` | 43 |
| 516 | [EL-185](../EL-185-releasable-assembly-joint.md) | Releasable assembly joint | `release_joint` | 48b |
| 517 | [EL-186](../EL-186-temporary-bridge.md) | Temporary bridge | `temporary_bridge` | 48b |
| 518 | [EL-004](../EL-004-catch-basin.md) | Catch basin | `catch_basin` | 61 |
| 519 | [EL-006](../EL-006-drain.md) | Drain | `drain` | 61a |
| 520 | [EL-005](../EL-005-liquid-funnel.md) | Liquid funnel | `liquid_funnel` | 61b |
| 521 | [EL-007](../EL-007-open-gutter.md) | Open gutter | `gutter` | 61b |
| 522 | [EL-008](../EL-008-straight-water-pipe.md) | Straight water pipe | `water_pipe` | 62 |
| 523 | [EL-009](../EL-009-water-elbow-45.md) | 45-degree water elbow | `water_elbow_45` | 62 |
| 524 | [EL-010](../EL-010-water-elbow-90.md) | 90-degree water elbow | `water_elbow_90` | 62 |
| 525 | [EL-011](../EL-011-water-t-junction.md) | Water T junction | `water_tee` | 62a |
| 526 | [EL-012](../EL-012-water-pipe-cap.md) | Water pipe cap | `water_cap` | 62a |
| 527 | [EL-013](../EL-013-water-nozzle.md) | Water nozzle | `water_nozzle` | 62a |
| 528 | [EL-024](../EL-024-primed-siphon.md) | Primed siphon | `siphon` | 65a |
| 529 | [TH-29](../TH-29-cold-pack.md) | Cold pack | `cold_pack` | 66 |
| 530 | [EL-027](../EL-027-canal-lock-chamber.md) | Canal lock chamber | `lock_chamber` | 67a |
| 531 | [TH-15](../TH-15-ice-plug.md) | Ice plug | `ice_plug` | 68 |
| 532 | [TH-11](../TH-11-heat-exchanger.md) | Heat exchanger | `heat_exchanger` | 69 |
| 533 | [TH-28](../TH-28-evaporative-cooling-pad.md) | Evaporative cooling pad | `evaporative_pad` | 73 |
| 534 | [TH-18](../TH-18-condenser.md) | Condenser | `condenser` | 74 |
| 535 | [TH-24](../TH-24-gas-expansion-bladder.md) | Gas expansion bladder | `gas_bladder` | 77 |
| 536 | [EL-050](../EL-050-acoustic-screen.md) | Acoustic screen | `sound_screen` | 78a |
| 537 | [EL-051](../EL-051-acoustic-dish.md) | Acoustic dish | `sound_dish` | 78a |
| 538 | [TH-13](../TH-13-freezing-mold.md) | Freezing mold | `freezing_mold` | 79 |
| 539 | [TH-26](../TH-26-thermal-storage-block.md) | Thermal storage block | `thermal_block` | 81 |
| 540 | [TH-23](../TH-23-expansion-rod.md) | Expansion rod | `expansion_rod` | 82 |
| 541 | [TH-27](../TH-27-phase-change-cartridge.md) | Phase-change storage cartridge | `phase_cartridge` | 84 |
| 542 | [TH-16](../TH-16-fusible-link.md) | Fusible link | `fusible_link` | 85 |
| 543 | [EL-171](../EL-171-squeeze-pad.md) | Squeeze pad | `squeeze_pad` | 87 |
| 544 | [EL-035](../EL-035-wick.md) | Wick | `wick` | 88 |
| 545 | [EL-036](../EL-036-sprinkler.md) | Sprinkler | `sprinkler` | 88 |
| 546 | [EL-172](../EL-172-rain-collector.md) | Rain collector | `rain_collector` | 88 |
| 547 | [GAP-06](../GAP-06-granular-dispenser.md) | Granular dispenser | `granular_dispenser` | 91 |
| 548 | [GAP-07](../GAP-07-granular-sieve.md) | Granular sieve | `granular_sieve` | 93 |
| 549 | [EL-118](../EL-118-coating-station.md) | Coating station | `coating_station` | 94 |
| 550 | [EL-119](../EL-119-dye-station.md) | Dye station | `dye_station` | 94 |

### Optics (29 new; orders 601 to 629; category member owned by ENGINE-OPTICAL)

| Order | Identity | Part | Package | First use |
| --- | --- | --- | --- | --- |
| 601 | [CAT-036](../CAT-036-laser.md) | Laser emitter | `laser` | 51 |
| 602 | [CAT-038](../CAT-038-light_receiver.md) (also EL-214) | Laser receiver | `light_receiver` | 51 |
| 603 | [CAT-041](../CAT-041-mirror.md) (also EL-212) | Flat mirror | `mirror` | 52a |
| 604 | [CAT-007](../CAT-007-beam_shutter.md) | Beam shutter | `beam_shutter` | 53 |
| 605 | [CAT-008](../CAT-008-beam_splitter.md) | Beam splitter | `beam_splitter` | 53 |
| 606 | [EL-153](../EL-153-general-light-receiver.md) | General-light receiver | `light_sensor` | 53a |
| 607 | [EL-176](../EL-176-red-laser-emitter.md) | Red laser emitter | `red_laser` | 54 |
| 608 | [EL-177](../EL-177-green-laser-emitter.md) | Green laser emitter | `green_laser` | 54 |
| 609 | [EL-178](../EL-178-blue-laser-emitter.md) | Blue laser emitter | `blue_laser` | 54 |
| 610 | [CAT-012](../CAT-012-blue_receiver.md) (also EL-148) | Blue receiver | `blue_receiver` | 54a |
| 611 | [CAT-032](../CAT-032-green_receiver.md) (also EL-147) | Green receiver | `green_receiver` | 54a |
| 612 | [CAT-056](../CAT-056-red_receiver.md) (also EL-146) | Red receiver | `red_receiver` | 54a |
| 613 | [CAT-011](../CAT-011-blue_filter.md) (also EL-145) | Blue filter | `blue_filter` | 54b |
| 614 | [CAT-031](../CAT-031-green_filter.md) (also EL-144) | Green filter | `green_filter` | 54b |
| 615 | [CAT-055](../CAT-055-red_filter.md) (also EL-143) | Red filter | `red_filter` | 54b |
| 616 | [CAT-006](../CAT-006-beam_combiner.md) (also EL-213) | Beam combiner | `beam_combiner` | 54c |
| 617 | [CAT-021](../CAT-021-cyan_receiver.md) (also EL-150) | Cyan receiver | `cyan_receiver` | 54c |
| 618 | [CAT-072](../CAT-072-yellow_receiver.md) (also EL-149) | Yellow receiver | `yellow_receiver` | 54c |
| 619 | [CAT-040](../CAT-040-magenta_receiver.md) (also EL-151) | Magenta receiver | `magenta_receiver` | 54d |
| 620 | [CAT-068](../CAT-068-white_receiver.md) (also EL-152) | White receiver | `white_receiver` | 54d |
| 621 | [CAT-043](../CAT-043-optical_and.md) (also EL-138) | Both light gate | `optical_and` | 55a |
| 622 | [CAT-046](../CAT-046-optical_or.md) (also EL-139) | Either light gate | `optical_or` | 55b |
| 623 | [CAT-047](../CAT-047-optical_xor.md) (also EL-140) | Exactly one light gate | `optical_xor` | 55c |
| 624 | [CAT-045](../CAT-045-optical_nor.md) (also EL-141) | Neither light gate | `optical_nor` | 55d |
| 625 | [CAT-044](../CAT-044-optical_nand.md) (also EL-142) | Not both light gate | `optical_nand` | 55e |
| 626 | [EL-173](../EL-173-diverging-lens.md) | Diverging lens | `diverging_lens` | 57a |
| 627 | [EL-175](../EL-175-optical-fibre.md) | Optical fibre | `optical_fibre` | 57a |
| 628 | [TH-06](../TH-06-converging-lens.md) | Converging lens | `converging_lens` | 59 |
| 629 | [EL-174](../EL-174-prism.md) | Prism | `prism` | 59a |

### Radiation (25 new; orders 701 to 725; category member owned by ENGINE-RADIATION)

| Order | Identity | Part | Package | First use |
| --- | --- | --- | --- | --- |
| 701 | [RAD-01](../RAD-01-gamma-source-capsule.md) | Gamma source capsule | `gamma_source_capsule` | 101 |
| 702 | [RAD-08](../RAD-08-radiation-rate-meter.md) | Radiation rate meter | `radiation_rate_meter` | 101 |
| 703 | [EL-126](../EL-126-thin-screen-shield.md) | Thin-screen shield | `thin_screen_shield` | 102 |
| 704 | [EL-128](../EL-128-dense-shield.md) | Dense shield | `dense_shield` | 102 |
| 705 | [EL-127](../EL-127-polymer-shield.md) | Polymer shield | `polymer_shield` | 104 |
| 706 | [RAD-06](../RAD-06-powered-radiation-shutter.md) | Powered radiation shutter | `radiation_shutter` | 105 |
| 707 | [RAD-07](../RAD-07-collimator-block.md) | Collimator block | `collimator_block` | 106 |
| 708 | [RAD-02](../RAD-02-powered-x-ray-emitter.md) | Powered X-ray emitter | `xray_emitter` | 107 |
| 709 | [RAD-09](../RAD-09-integrating-dosimeter.md) | Integrating dosimeter | `integrating_dosimeter` | 109 |
| 710 | [RAD-10](../RAD-10-exposure-sensitive-cargo-badge.md) | Exposure-sensitive cargo badge | `badged_parcel` | 111 |
| 711 | [EL-129](../EL-129-sheet-thickness-transmission-gauge.md) | Sheet-thickness transmission gauge | `sheet_thickness_gauge` | 113 |
| 712 | [RAD-03](../RAD-03-alpha-source-cartridge.md) | Alpha source cartridge | `alpha_source_cartridge` | 115 |
| 713 | [RAD-04](../RAD-04-beta-minus-source-cartridge.md) | Beta-minus source cartridge | `beta_minus_source_cartridge` | 116 |
| 714 | [RAD-12](../RAD-12-scintillator-tile.md) | Scintillator tile | `scintillator_tile` | 117 |
| 715 | [RAD-14](../RAD-14-magnetic-deflector.md) | Magnetic deflector | `magnetic_deflector` | 118 |
| 716 | [RAD-15](../RAD-15-track-chamber.md) | Track chamber | `track_chamber` | 119 |
| 717 | [RAD-13](../RAD-13-decay-clock-capsule.md) | Decay clock capsule | `decay_clock_capsule` | 123 |
| 718 | [EL-131](../EL-131-fast-neutron-detector.md) | Fast-neutron detector | `fast_neutron_detector` | 125 |
| 719 | [RAD-16](../RAD-16-neutron-source-module.md) | Neutron source module | `neutron_source_module` | 125 |
| 720 | [EL-132](../EL-132-slow-neutron-detector.md) | Slow-neutron detector | `slow_neutron_detector` | 126 |
| 721 | [RAD-17](../RAD-17-water-moderator-tank.md) | Water moderator tank | `moderator_tank` | 126 |
| 722 | [RAD-18](../RAD-18-neutron-absorber-panel.md) | Neutron absorber panel | `neutron_absorber_panel` | 127 |
| 723 | [EL-130](../EL-130-tank-level-transmission-gauge.md) | Tank-level transmission gauge | `tank_level_gauge` | 128 |
| 724 | [RAD-21](../RAD-21-radiation-responsive-material-latch.md) | Radiation-responsive material latch | `radiation_material_latch` | 133 |
| 725 | [RAD-22](../RAD-22-sealed-tracer-capsule.md) | Sealed tracer capsule | `sealed_tracer_capsule` | 135 |

## 5. Settled points and re-run trigger

- **Display names.** EL-014 is "Water bucket" and EL-069 is "Hanging bucket" (coordinator decision 36), so the Motion group shows no two identical rows.
- **Category rule** (section 3): function categories, with Optics and Radiation added by their engine stories (coordinator decision 36).
- **Delivered orders** are not renumbered (rule 1; coordinator decision 36).
- **Re-run trigger.** Any change to campaign coverage section 6, a new placeable identity (EL-242's spec, a 24c addition) or a category change re-sorts only its own category block; delivered numbers never move.
