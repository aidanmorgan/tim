# Campaign coverage: every part has a lesson, each mechanic is introduced once

Written 23 November 2026 for [owner decisions 24c to 24f](owner-decisions-2026-11-22.md) against the planning documents in this checkout. 24c: every placeable part and fixture is used by at least one campaign lesson, and a part no lesson uses gets one, in the chapter that teaches its domain and after the parts it depends on. 24d: the campaign may exceed 150 levels; a level that repeats a mechanic already introduced is removed; later levels combine and extend what is already taught. 24e: no level introduces more than two or three new parts; introductions move to earlier chapters where prerequisites allow, and levels are added for the rest. 24f: chapters 1 and 2 follow the plan, so the switch, lamp and fan first appear in chapter 2. This record lists what was found, what was added, removed and converted, and the level counts before and after. The allocations themselves are written into the campaign authorities: [requirements, campaign coverage additions](../../requirements.md#campaign-coverage-additions) (with a **Campaign:** clause on each affected element row), the [chapter reservations](../../invest/campaign-reservations.md), the [level scopes](../../invest/campaign.md) note and [Epic 15](../../../../_bmad-output/planning-artifacts/epics.md#epic-15-progressively-taught-campaign-at-least-150-levels).

## 1. Rule used

A part counts as used by the campaign if either holds:

1. an authored level in `content/puzzles.json` places it (cited by level id; an authored position is not its final slot), or
2. a campaign authority names it, by its own name or by a phrase that enumerates it ("each laser/receiver channel", "ramp/wall/material variants", "manual/mechanical/solenoid tap variants"). The campaign authorities are the [element coverage ledger](../../requirements.md#campaign-element-coverage), the [GAP allocation](../../requirements.md#campaign-gap-allocation), the [thermal allocation](../../requirements.md#thermal-campaign-allocation), the radiation allocations and an element row's **Campaign:** clause. Spec "Campaign" lines were read as evidence but are not the authority; they report coverage inconsistently.

A family phrase that a sibling part already satisfies does not count for a variant it does not name: "ball straight pipe, both bend angles" is met by the clear pipes, so it does not cover the metal pipes.

**Counted:** the 72 catalogue (CAT) parts and the 302 named identities with specs or ledger rows (242 EL, 37 TH, 19 RAD, 4 GAP parts).
**Not counted as separate parts:**
- declared companion packages (sealed can, toy fish, glow fish, toast slice, balloon cargo, fracture fragments): not placeable, owner decision 24b;
- umbrella and index entries (GAP-01, 02, 03, 05, 08, 16, 18; RAD-05, 11, 19), which forward to their children, each counted;
- GAP product features 10–15 and 17 (tools and UI, already allocated in the GAP table);
- EL-120 to EL-123 (goal kinds) and EL-124, EL-125 (environment presets): level data, not placeable; listed below as covered by GAP-16 and GAP-18;
- closed duplicates ([delivery batches §2a and §2b](../../invest/delivery-batches.md#2-duplicate-closures)): covered through their satisfying part, listed below with it.

**Fixtures.** Every fixture an authored level places is a catalogue part: ball, basket, bowling, tennis, lamp, switch, fan, domino, bumper, spring, motor, battery, conveyor, pulley, weight, wall, delay, pipe_bend_90, wound_spring, flashlight, balloon. Each also has a first-use reservation. Six appear in kept authored levels only as fixtures (ball, tennis, lamp, motor, flashlight, balloon), and the basket is placed by the player only once, in `ready_to_rebound`; under the pacing rule ("fixed background fixtures do not count as teaching") their reservation, not those levels, is their lesson. The CAT table marks fixture uses with ᶠ.

**Named ball packages (owner decision 24c).** The Baseball (EL-239), Soccer ball (EL-077) and Cannonball (EL-091) are lesson parts: the Baseball at 2a and the Soccer ball at 2b (section 6), and the Cannonball at 45, *Load Before Fire*. Each is a named package, not a catalogue part, so its lesson offers it in the player's inventory once S7 admits named packages. A teaching lesson has the player place the part (the pacing rule above), so none of the three lessons needs it as a fixture; a later level that places one as a locked fixture also needs S8, because today's Coverage fixture rule binds every fixture to a catalogue part (`tools/Coverage/CurrentCatalogue.cs:117`, [delivery batches, S8](../../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)).

## 2. Result

| Count | Before | After |
| --- | --- | --- |
| Planned campaign levels | 150 (15 chapters × 10) | **244**: the 150 slots + 94 lettered levels (9 under 24c, 82 under 24e and 24f, 2 under coordinator decision 35, 1 under coordinator decision 36); per chapter in section 6 |
| Most new parts in one level | not allocated per level | 3 (section 6) |
| Placeable parts with no lesson | 18 | 0 |
| Authored levels in `content/puzzles.json` | 63 (15 Playable, 48 Planned) | 34 kept (15 Playable, 19 Planned, 2 of them converted), all placed in their chapter and `domino_effect` re-authored (section 5); 29 removed |
| Planned lesson names that repeated a mechanic | 3 (in [todo-437](../../requirements.md#todo-437)) | 0: 2 converted, 1 removed |

The 18 parts with no lesson: 9 joined 6 existing planned lessons, and 9 got 9 new levels. Under 24e, 8 introductions moved to an earlier chapter and 82 more levels were added (21 of them hold a relocated authored level), then coordinator decision 35 added 2 so that no finale introduces a part; under 24f, the five authored levels that introduced the switch, lamp or fan leave chapter 1. Under coordinator decision 36 (prerequisites taught first), the introductions of 28 parts moved within the plan and 1 level (9b, the Structural brace) was added (section 6).

New levels keep the existing numbering: a new level takes the number of the slot it follows plus a letter (24a follows 24), so every existing slot number, exact reservation and `LEVEL-NNN` identity is unchanged. A new level's owner id is that slot's three digits plus one lowercase letter (`LEVEL-024a`, `LEVEL-069c`), so the ordinal order of the spelling is the campaign order (`LEVEL-069` < `LEVEL-069a` < `LEVEL-069b` < `LEVEL-070`). The ownership tools admit that form and refuse every other spelling (`Identity` in `tools/Ownership/CurrentOwnerMembership.cs` and the grouped-role pattern in `tools/Coverage/CurrentOwnerRelations.cs`; [lettered level ids slice](../../../../_bmad-output/implementation-artifacts/spec-tool-lettered-level-ids.md)). A new level's `LEVEL-` heading is added to `campaign.md` by the slice that authors it, which raises the heading census in `tools/Ownership/CurrentOwnerMembershipOracles.cs` (545) by one per heading.

`content/puzzles.json` is not changed here. The removals in section 5 are applied by the content housekeeping slice (completion-ledger OS-02), which also updates any test that counts or names levels; the two conversions are authored by their Early level proof slice (OS-08) and the `domino_effect` re-authoring by OS-06 (owner decision 24o(3)). Until the removals land, a removed level must not be Early-proven.

## 3. Parts that had no lesson, and where each now goes

| Part | Chapter · slot | Lesson | Joined an existing lesson, or new | Why there |
| --- | --- | --- | --- | --- |
| EL-098 Pool ball, EL-099 Pool pocket | 1 · **8** | *Bank the Bounce* | existing planned lesson ([todo-437](../../requirements.md#todo-437)), converted (section 5) | A pool ball is a ball preset and a pocket is a capture opening, chapter 1's domain; banking off a cushion into a pocket is what *Bank the Bounce* names, and without these parts it only repeated the wall rebound |
| EL-101 Thumb tack | 2 · **13** | the balloon tether/pop lesson | existing planned reservation ("balloon tether/pop", ledger row 2) | The tack's first target is the delivered Balloon; it extends the pop that `burst_of_motion` teaches with a dropped ball, with a new cause |
| EL-102 Large-bore ball pipe | 3 · **24a** | *Bore Necessities* | new level | A pipe variant that needs only the clear pipe and its joins; no planned pipe lesson teaches bore size |
| EL-100 Vacuum nozzle | 3 · **27a** | *Clean Sweep* | new level | Supplied airflow into a tube needs only the fan and supply (chapter 2) and pipes (chapter 3); placed in chapter 3 rather than with the airlift in 91–100 to keep chapter 10 from growing further |
| EL-097 Pool cue | 5 · **44a** | *On Cue* | new level | A supplied contact actuator modelled on the Linear pusher; chapter 5 teaches aiming and triggering |
| EL-091 Cannonball | 5 · **45** | *Load Before Fire* | existing planned lesson ([todo-437](../../requirements.md#todo-437)) | The Cannon's loading lesson; the cannonball is the heavy cargo the requirement separates from its launcher |
| EL-063 Cable winch | 5 · **47** | *Catch, Then Lift* | existing planned lesson ([todo-437](../../requirements.md#todo-437)) | Ledger row 2 sends rope and cable load variants to 41–50; a winch is the lifter in "catch, then lift" |
| EL-103 Accelerator tube | 5 · **48a** | *Up to Speed* | new level | A field force added to a pipe; chapter 5 teaches the electromagnet and launching |
| EL-071 Straight metal ball pipe, EL-072 90° metal bend, EL-242 45° metal bend | 6 · **52** | the light-interruption lesson ([sequence-task-340](../../requirements.md#sequence-task-340): "interrupt with a ball") | existing planned lesson | Their own behaviour is opacity: they route a ball like the clear pipes but hide it and block light, which only a light lesson can show |
| EL-043 Pneumatic pressure gauge | 8 · **72** | the air reservoir's first-use lesson | existing planned reservation (ledger row 11) | A container and its reading teach one causal relationship together (chapter structure rule) |
| EL-042 Air nozzle | 8 · **73a** | *Jet Set* | new level | Chapter 8's stated distinction is "fan airflow from stored pressure"; the nozzle is that contrast |
| EL-037 Air compressor | 8 · **74a** | *Pump It Up* | new level | Supplied work into stored pressure, after the gauge and nozzle |
| EL-106 Impact-sensitive toy charge | 10 · **95a** | *Gently Does It* | new level | Chapter 10's destruction variants; a shock trigger extends the dynamite's signal trigger |
| EL-104 Toy firework | 10 · **96a** | *Fuse Box* | new level | Chapter 10's ignition variants, after the toy rocket |
| EL-105 Toy missile | 10 · **97a** | *Follow the Light* | new level | Chapter 10's rocket variants; needs chapter 6's light sources and receivers |

## 4. Lesson briefs

Each brief gives id/slot, chapter, parts, concept, prerequisites, practice and reuse, and the puzzle intent. In-chapter order relied on: the chapter's earlier first uses named under *Prerequisites* take earlier slots of the same chapter (Wall, Ramp and Receiver before 8; Balloon before 13; clear pipe, bends and joins before 24a; Linear pusher and Boxing glove before 44a; Cannon before 45; Electromagnet before 48a; a light source and receiver before 52; Bellows, Pneumatic hose, Air reservoir and Release valve before 72; Toy dynamite and Detonation plunger before 95a; Toy rocket before 96a and 97a).

**8 · Bank the Bounce** (chapter 1; EL-098 Pool ball, EL-099 Pool pocket). *Concept:* a pocket counts a ball only when it actually drops through the opening; a cushion changes a rolling ball's direction. *Prerequisites:* Ramp, Wall, Receiving basket; the wall rebound (`wall_and_bumper`). *Practice* 10 (the chapter finale's alternative route may end in a pocket); *reuse* 49, 141. *Intent:* A pool ball waits at the top of a short ramp on a low table edged with Wall cushions; the pocket sits round a corner the ball cannot reach in a straight line. The player turns one cushion so the ball banks once and drops into the pocket. A ball that stops beside the opening is not pocketed, and a shot straight at the cushion comes back.

**13 · balloon pop lesson** (chapter 2; EL-101 Thumb tack). *Concept:* a sharp point pops the balloon only where the balloon actually meets it hard enough; a missed contact leaves it intact. *Prerequisites:* Balloon, Rope, Rope anchor, Weight; popping by a dropped ball (`burst_of_motion`). *Practice* 19 (pop the balloon to change a counterweight's balance); *reuse* 149. *Intent:* A rising balloon tied over a pulley holds a flap shut against a small weight. The player places the tack where the balloon will rise into it; the pop removes the lift, the weight drops the flap and the ball behind it rolls into the basket. A tack beside the balloon's path leaves it intact and the flap closed.

**24a · Bore Necessities** (chapter 3, new level; EL-102 Large-bore ball pipe). *Concept:* a pipe passes only cargo that fits its bore; a large-bore pipe passes a ball that jams the clear pipe, and its mouths do not join a clear pipe's. *Prerequisites:* clear pipe, 45° and 90° bends, joining mouths; a ball's `radius` setting (chapter 1 presets). *Practice* 30; *reuse* 141. *Intent:* Two balls must reach the basket through a gap in a wall, one ordinary and one oversized. A clear pipe already carries the small ball; the big one jams at its mouth. The player builds a second route of large-bore pipe; a join from large-bore to clear pipe is refused, so the big route must be large-bore throughout. Sending the oversized ball into the clear pipe leaves it stuck.

**27a · Clean Sweep** (chapter 3, new level; EL-100 Vacuum nozzle). *Concept:* supplied suction draws a loose ball into a tube through a finite opening; with no supply, or a blocked inlet, nothing moves. *Prerequisites:* Fan (airflow), Battery, wire and Switch, clear pipe and joins. *Practice* 29; *reuse* 96, 147. *Intent:* Balls roll to a stop on the floor below the route. The player sets the nozzle's mouth where they come to rest, joins its outlet to a pipe that runs to the basket and wires it to a battery. Unpowered, the nozzle draws nothing; a ball too big for the throat lodges in the cone and blocks the inlet.

**44a · On Cue** (chapter 5, new level; EL-097 Pool cue). *Concept:* the cue moves a ball only by touching it; a miss moves nothing; its aim sets the direction. *Prerequisites:* *Bank the Bounce* (8), Linear pusher, Boxing glove, Battery and Switch. *Practice* 49 (sink two pool balls in a set order, with the order goal); *reuse* 141. *Intent:* Three pool balls sit on a cushioned table. The player places and aims the cue and powers it through a switch that a falling ball strikes, to sink the named ball by a bank shot. A cue aimed past the ball moves nothing, and an unpowered cue does not strike.

**45 · Load Before Fire** (chapter 5; EL-091 Cannonball, with the Cannon's own lesson). *Concept:* the same launch gives a heavy cannonball far less speed than a light ball; the cargo, not the launcher, decides. *Prerequisites:* Cannon, Bowling and Tennis balls. *Practice* 46 (the counterweighted scoop throws the same cannonball); *reuse* 68 (heavy cargo a raft must carry), 146. *Intent:* The Cannon must land a load in a basket on a high ledge. A tennis ball overshoots; the player loads the cannonball and sets the aim so its heavier, slower shot lands. Firing an empty or unloaded cannon delivers nothing.

**47 · Catch, Then Lift** (chapter 5; EL-063 Cable winch). *Concept:* shaft work winds the cable and lifts the hooked load; with no drive there is no lift, and too heavy a load stalls the drum. *Prerequisites:* Motor and Battery, Steel cable, Load hook, Moving bucket, Pulley. *Practice* 48 (the winch lifts cargo onto the powered loading carrier); *reuse* 65, 138. *Intent:* A ball drops into a moving bucket on the floor. The player hooks the bucket to the winch cable, runs it over a pulley and drives the winch from a motor, so the bucket rises to a ledge and tips into the basket. An unpowered winch leaves the bucket on the floor; a bucket holding the bowling ball stalls it.

**48a · Up to Speed** (chapter 5, new level; EL-103 Accelerator tube). *Concept:* a supplied tube adds bounded work so the ball leaves faster; unpowered, it is an ordinary pipe. *Prerequisites:* clear pipe, Battery and Switch, Electromagnet (field force). *Practice* 50; *reuse* 97 (set against the spiral delay tube), 148. *Intent:* A ball rolling off a ramp cannot clear a gap to the basket. The player puts the accelerator tube in its path and supplies it, so the ball leaves fast enough to jump the gap. Unpowered, the ball drops into the gap.

**52 · light-interruption lesson** (chapter 6; EL-071, EL-072, EL-242 metal pipes). *Concept:* metal pipes route a ball exactly as clear pipes do, but they hide it and block light. *Prerequisites:* clear pipes and bends, a light source and a receiver. *Practice* 54; *reuse* 142. *Intent:* After the lesson's first objective (a passing ball interrupts a beam), a ball route must cross two beams. One receiver must stay lit while the ball passes, so that crossing must be clear pipe and the beam is broken only for the moment the ball is in it; the other receiver must stay dark, so that crossing must be a metal pipe or bend, which blocks its beam like a wall while still carrying the ball. Swapping the two materials fails both conditions.

**72 · air reservoir lesson** (chapter 8; EL-043 Pneumatic pressure gauge). *Concept:* the gauge shows the pressure stored in a reservoir, with no electrical connection; an empty reservoir reads ambient. *Prerequisites:* Bellows, Pneumatic hose, Air reservoir, Release valve. *Practice* 73; *reuse* 87, 147. *Intent:* A falling weight works the bellows through a lever, charging a reservoir; a release valve, opened by a ball striking its trigger, blows the stored air at a light ball at the foot of a ramp. The ball reaches the basket only if enough air is stored. The player sets the number of strokes and places the gauge to read the charge; too small a charge reads below the gauge's mark and the ball stops short.

**73a · Jet Set** (chapter 8, new level; EL-042 Air nozzle). *Concept:* stored pressure through a nozzle gives a strong, short jet that empties the reservoir; a fan gives a weak, steady stream for as long as it is supplied. *Prerequisites:* Fan, Air reservoir, gauge (72), Release valve. *Practice* 75; *reuse* 99, 143. *Intent:* A tennis ball and a basketball must each cross a gap. The fan moves the tennis ball but not the basketball. The player joins the charged reservoir through the release valve to a nozzle aimed at the basketball, which gets one strong push; the gauge falls as the reservoir empties, so a second push needs a recharge. An empty reservoir gives no jet, and the fan alone cannot move the basketball.

**74a · Pump It Up** (chapter 8, new level; EL-037 Air compressor). *Concept:* supplied electrical work raises a reservoir's pressure; an unpowered compressor adds nothing; the battery's energy limits the charge. *Prerequisites:* Battery and Switch, Air reservoir, Pneumatic hose, gauge (72), nozzle (73a). *Practice* 76; *reuse* 87, 147. *Intent:* Two nozzle shots are needed and the bellows from 72 cannot recharge in time. The player wires a compressor to a battery, watches the gauge rise to the charge the battery can pay for, and fires the nozzle twice. With the switch open the gauge stays at ambient. The level authors the reservoir at its smallest `volume`, 0.002 m³, as do the later charging levels (76, 87, 147): a default Battery (3600 J) cannot charge the default 0.01 m³ tank to its 600 000 Pa rating (4825 J), while the small tank reaches 400 000 Pa in about 7 s at 60 W (EL-037, supplied-work table).

**95a · Gently Does It** (chapter 10, new level; EL-106 Impact-sensitive toy charge). *Concept:* a shock above the charge's threshold releases its one blast; a gentle contact leaves it unspent, and a spent charge does not fire again. *Prerequisites:* Toy dynamite and Detonation plunger, Conveyor, Brick barrier. *Practice* 99; *reuse* 149. *Intent:* A brick barrier blocks the route; the toy charge waits at the far end of the room. The player carries it gently on a conveyor to the barrier, then drops a bowling ball on it to clear the barrier. Rolling it hard off a ramp sets it off early, in the wrong place.

**96a · Fuse Box** (chapter 10, new level; EL-104 Toy firework). *Concept:* a lit firework flies on its finite charge and bursts after its fuse delay, emitting one signal at the burst; a spent firework does not retrigger. *Prerequisites:* Toy rocket, Delay and Hold timer, Powered gate. *Practice* 99; *reuse* 148. *Intent:* A ball waits behind a powered gate that a short hold timer keeps open. The player launches the firework from an impact switch and sets its burst delay so the burst opens the gate after the ball has rolled up to it. Too short a delay opens the gate before the ball arrives, and it closes again.

**97a · Follow the Light** (chapter 10, new level; EL-105 Toy missile). *Concept:* two light eyes steer the dart toward the lit side; with no light it flies straight, and when its store is spent it stops pushing. *Prerequisites:* Toy rocket, Lamp or Flashlight, a receiver lesson, Mirror. *Practice* 100; *reuse* 142. *Intent:* A target basket sits behind a wall, beside a lamp. The player places the missile and a mirror so its eyes see the lamp and steer it round the wall into the basket. With the lamp off or blocked, it flies straight into the wall.

## 5. Removed and converted levels (owner decision 24d)

**Criterion.** A level is removed when it introduces nothing an earlier kept level does not: its goals are independent lanes, each repeating a mechanic or combination an earlier kept level already makes, so it adds count, not a new causal link or constraint. The first level that adds a second destination (`two_deliveries`) is kept, because a second destination changes the problem ([pacing rules](../../requirements.md#campaign-element-coverage), "Change the problem"). A level that holds a part's only use, a delivered or Playable level's proof, or a later combination's prerequisite is converted instead of removed. None of the removed levels is Playable, none holds a part's only use and none is a prerequisite of a kept level.

### Authored levels removed (29, all Planned)

| Position · id | Repeats | Reason |
| --- | --- | --- |
| 8 `wind_signal` | `spring_signal` (7), `cold_start` (9) | A taught mover striking the switch to light the lamp is introduced by `spring_signal`; fan and switch already meet in `cold_start`, which goes further (the switch powers the fan) |
| 12 `wall_return` | `wall_and_bumper` (13, Playable) | Same rebound off a player-built wall; `wall_and_bumper` becomes the Wall's introduction |
| 30 `bounce_and_roll` | `two_deliveries`; `first_principles`, `spring_forward` | Two independent delivery lanes |
| 31 `air_and_belt` | `two_deliveries`; `air_mail`, `conveyor_courier` | Two independent delivery lanes |
| 32 `catch_and_signal` | `first_principles`, `power_trip` | A catch lane beside a switch lane |
| 33 `wind_and_dominoes` | `air_mail`, `domino_effect` | Two independent lanes |
| 34 `powered_post` | `cold_start`, `first_principles` | `cold_start` plus a ramp lane |
| 35 `spring_and_chain` | `spring_forward`, `domino_effect` | Two independent lanes |
| 36 `two_signals` | `power_trip`, `spring_signal` | Two switch lanes |
| 37 `mixed_signals` | `conveyor_courier`, fan onto a switch (`cold_start`) | Two independent lanes |
| 38 `start_and_topple` | `cold_start`, `domino_effect` | `cold_start` plus a domino lane |
| 39 `deep_routes` | `first_principles`, depth (`third_dimension`, `bumper_depth`) | Two ramp lanes in depth |
| 40 `cross_breezes` | `third_dimension` | Two fan lanes in depth |
| 41 `deep_springs` | `spring_forward`, depth | Two springboard lanes in depth |
| 42 `spatial_signals` | `spring_signal`, depth | Two switch lanes in depth |
| 43 `three_deliveries` | `two_deliveries` | A third lane: count only |
| 44 `triple_signal` | `spring_signal` | Three switch lanes |
| 45 `cold_front` | `cold_start`, `first_principles`, `spring_forward` | `cold_start` plus two lanes |
| 46 `chain_mail` | `air_mail`, `conveyor_courier`, `domino_effect` | Three independent lanes |
| 47 `bounce_mail` | `spring_forward`, `spring_signal`, `domino_effect` | Three independent lanes |
| 48 `relay_workshop` | `cold_start`, `spring_signal`, `domino_effect` | `cold_start` plus two lanes |
| 50 `triple_chain` | `domino_effect` | Three domino chains |
| 51 `signals_and_chain` | `spring_signal`, `domino_effect` | Three independent lanes |
| 52 `cold_signals` | `cold_start`, `spring_signal` | `cold_start` plus two switch lanes |
| 53 `depth_delivery` | `first_principles`, `spring_forward`, `conveyor_courier`, depth | Three lanes in depth |
| 54 `depth_telegraph` | `spring_signal`, depth | Three switch lanes in depth |
| 55 `double_bridge` | `domino_effect`, `conveyor_courier` | Two domino chains and a conveyor lane |
| 56 `bridges_and_signal` | `domino_effect`, `spring_signal` | Two domino chains and a switch lane |
| 57 `cold_bridges` | `domino_effect`, `cold_start` | Two domino chains and `cold_start` |

### Authored levels converted (2, both Planned)

| Position · id | Was | Becomes | Why converted, not removed |
| --- | --- | --- | --- |
| 49 `double_cold_start` | Three lanes: a switch-started conveyor, a cold fan and a spring catch | One causal chain: a falling ball closes the switch that supplies the motor driving the conveyor, which carries a second ball to the basket; keeps its `powered_after` goal (the conveyor runs only after the strike) | It holds the only switch-started conveyor, which combines `switched_motor` with `conveyor_courier`; the fan and spring lanes are dropped |
| 58 `grand_contraption` | Three independent lanes (ramp signal, conveyor signal, domino chain) | Chapter 2's finale as one chain: a ball rolls down ramps onto a switch that powers the conveyor, which tips a ball onto the dominoes, whose last tile lights the lamp. Retitled *One Thing Leads to Another*, so *The Grand Contraption* stays with level 150 | It is the only authored finale combining ramp, switch, drive and dominoes; as lanes it introduced nothing |

### Planned lesson names (todo-437)

| Lesson | Repeats | Disposition |
| --- | --- | --- |
| *Bank the Bounce* | the wall rebound of `wall_and_bumper` | Converted: the pool build-up at slot 8 (section 4) |
| *One Ball at a Time* | the hopper's introduction, *Queue the applause* (single release from a queue) | Converted: chapter 5's build-up in which the hopper feeds the Cannon's reload shuttle one ball at a time (Ball hopper with Reload shuttle) |
| *Save the Spring* | `saved_for_later` (Playable: a wound spring keeps its charge after the motor stops) | Removed as a separate lesson; `saved_for_later` teaches it |

Kept although close to an earlier level, with the reason: `bumper_depth` (11, Playable) combines the bumper with depth; `ready_to_rebound` (62, Playable) reverses the springboard problem (place the catcher for a stored release); `spring_signal` (7) is the first level that joins a mover to a signal; `two_deliveries` (29) is the first second destination. Duplicate titles found: "Around the corner" is used by `pulley_depth` (20) and `quarter_bend` (27); the authoring slice renames one. *The Grand Contraption* is resolved by the retitle above.

**Early level proof.** [Delivery batches §4](../../invest/delivery-batches.md#4-early-level-proof) counted 63 levels as 15 Playable + 11 provable with single-goal evaluation + 34 after the goal slices + 3 blocked on parts (level 02 `power_trip` counts among the 34: its `activated` goal has no fixed predecessor). After the removals: 15 Playable + 9 single-goal provable (`wind_signal` and `wall_return` leave the 11) + 7 after the goal slices (02, 09, 14, 15, 29, 49, 58) + 3 blocked on parts (21, 22, 24) + 29 removed = 63. `domino_effect` is one of the 15 Playable both before and after its re-authoring (its goal stays Captured, coordinator decision 41), so no count moves.

### Chapter 1 re-authoring and relocated authored levels (owner decisions 24f and 24e)

Every kept authored level is placed at the plan level its parts allow (section 6). A relocated Playable level changes only its presentation order; `domino_effect` is the only Playable level whose content changes.

| Authored position · id | State | Plan level | Change |
| --- | --- | --- | --- |
| 1 `first_principles` | Playable | 1 | none |
| 2 `power_trip` | Planned | 11 | moves to chapter 2: it introduces the switch and lamp (24f) |
| 3 `air_mail` | Planned | 11a | moves to chapter 2: it introduces the fan (24f) |
| 4 `spring_forward` | Playable | 2 | none |
| 5 `third_dimension` | Planned | 11b | moves to chapter 2 as a build-up (fan through depth); chapter 1's depth level is `bumper_depth` |
| 6 `domino_effect` | Playable | 4 | **re-authored** (24f): the locked signal lamp (`fixture_3`) and the domino-to-lamp connection are removed, and the goal stays a Captured goal (coordinator decision 41): the last domino knocks a ball into a Receiving basket (level 1's two parts, so the level introduces no new part for it), a goal kind the engine evaluates today, so the level stays Playable and is provable without `ENGINE-GOALS`; the GAP-01 beam objective follows in the same slot (the brace's is at 9b, coordinator decision 36) |
| 7 `spring_signal` | Planned | 11c | moves to chapter 2 (switch and lamp) |
| 9 `cold_start` | Planned | 11d | moves to chapter 2 (switch and fan) |
| 10 `bumper_sidekick` | Playable | 5 | none |
| 11 `bumper_depth` | Playable | 6 | none; chapter 1's depth level |
| 13 `wall_and_bumper` | Playable | 7 | none; the Wall's introduction |
| 14 `battery_motor` | Planned | 12 | none |
| 15 `switched_motor` | Planned | 12a | none |
| 19 `counterweight` | Planned | 12b | moves before the pulley and tack levels |
| 20 `pulley_depth` | Planned | 12c | none |
| 16 `conveyor_courier` | Planned | 14a | none |
| 17 `belt_relay` | Planned | 14b | none |
| 18 `reverse_belt` | Planned | 14c | none |
| 29 `two_deliveries` | Planned | 19a | moves into chapter 2 (ramp and fan only) |
| 49 `double_cold_start` | Planned, converted | 19b | moves into chapter 2 |
| 58 `grand_contraption` | Planned, converted | 20 | chapter 2's finale |
| 25 `clear_pipe` | Playable | 21 | none |
| 26 `gentle_bend` | Playable | 21a | none |
| 27 `quarter_bend` | Playable | 22 | none |
| 28 `joined_pipe` | Playable | 22a | none |
| 23 `delayed_signal` | Playable | 23a | none |
| 59 `wind_then_release` | Playable | 41 | moves to chapter 5 (wound launcher) |
| 60 `saved_for_later` | Playable | 41a | moves to chapter 5 |
| 21 `solar_motor` | Planned | 51a | moves to chapter 6 (Flashlight and Solar panel) |
| 22 `solar_shadow` | Planned | 51b | moves to chapter 6 |
| 24 `delayed_solar` | Planned | 51c | moves to chapter 6 |
| 61 `a_gentle_rebound` | Planned | 3 | moves to chapter 1 (trampoline) |
| 62 `ready_to_rebound` | Playable | 3a | moves to chapter 1 |
| 63 `burst_of_motion` | Playable | 9a | moves to chapter 1; the balloon's introduction moves with it (24e) |

## 6. Per-level allocation and progression check (owner decisions 24e and 24f)

**Rule.** No level introduces more than three new parts (24e), and the switch, lamp and fan first appear in chapter 2 (24f). Introductions were first moved to an earlier chapter wherever every prerequisite is taught there; levels were then added for what remained. Each level below lists the parts it introduces; a part counts once, at the level that teaches it (where the player first places it from inventory); a part a level only fixes in place is not counted there, because a fixed fixture is not teaching (the pacing rule in section 1), so the Pulley fixed at 12b counts at 12c. Goal kinds, environment presets, tools (GAP-10 to GAP-17) and thermal scenarios are objectives, not parts, and are named only in the note column. A lettered level follows the slot with the same number (11a follows 11), so every existing slot number and exact reservation is unchanged.

**Moved introductions (24e).**

| Part | Was | Now | Why the prerequisites allow it |
| --- | --- | --- | --- |
| Balloon (CAT-003) | chapter 2 (11–20) | **9a**, chapter 1 | It needs no other part; the Playable `burst_of_motion` already pops it with a dropped ball |
| Weight tray (EL-059) | chapter 10 (91–100) | **25**, chapter 3 | A variant of the Pressure plate, taught beside it |
| Egg timer (EL-236) | chapter 10 | **28**, chapter 3 | A timer; the Delay and Hold timer come first (23a, 24) |
| Size grate (EL-058) | chapter 10 | **28a**, chapter 3 | Static geometry that sorts balls; needs only routes |
| Spiral delay tube (EL-107), Speed-sensitive trapdoor (EL-109) | chapter 10 (specialist routes) | **28b**, chapter 3 | Ball-routing parts; the pipes come first (21–22) |
| Powered airlift (EL-108), Selective trap door (EL-238) | chapter 10 (specialist routes) | **28c**, chapter 3 | Need the pipes and the fan and supply (chapter 2) |

Not moves but placements within the ledger's range: the electrical gates and edge detectors (ledger 21–40) are placed in chapter 4, as the chapter table names them there. Considered and not moved: the optical, water, pneumatic and acoustic parts each need their own chapter's first lessons; the thermal parts keep their exact allocation (owner decision 18) and already sit at most three to a level.

**Moved for prerequisite order (coordinator decision 36).** A part's campaign prerequisites are taught first. The cross-batch audit (`docs/planning/elements/readiness/audits/cross-batch.md`, check 9) and the re-run below found parts whose own spec needs a part the plan taught later. Each is fixed by moving one of the two within the rules of 24c to 24f and decision 35; only the brace needed a new level.

| Part | Was | Now | Prerequisite the spec states |
| --- | --- | --- | --- |
| Structural brace (EL-113) | 4 | **9b** (new level, chapter 1) | EL-114 Placeable pivot (9): the brace's frame stands on pivots (EL-113 Delivery). Level 4 was full, so the brace follows the pivot and linkage |
| Laser (CAT-036) | 51d | **51** | The Light receiver (CAT-038, 51) and its duplicate EL-214 need the Laser; the Solar panel (CAT-059 / EL-211, 51a) needs the Flashlight and the Laser |
| Red, Green and Blue receivers (CAT-056, CAT-032, CAT-012) | 54b | **54a** | The colour filters need the Story 13.2 receivers (CAT-055, CAT-031, CAT-011 Prerequisites (3)); the receivers need only the lasers (54) |
| Red, Green and Blue filters (CAT-055, CAT-031, CAT-011) | 54a | **54b** | As above |
| Float, Mechanical float valve, Electronic level switch (EL-016 to EL-018) | 64 | **63** | The Solenoid tap's row E8 needs the level switch (EL-167 Prerequisites) |
| Mechanically actuated tap, Solenoid tap (EL-166, EL-167) | 63 | **64** | As above |
| Buoyant platform (EL-028) | 67a | **66a** | The Canal lock needs it (EL-027 Prerequisites); the Boat needs the lock (EL-029 Prerequisites) |
| Canal lock chamber (EL-027) | 66a | **67a** | As above |
| Bell (CAT-009) | 76a | **75a** | The Sound meter's acceptance source is the Bell (CAT-060 Prerequisites, Story 14.2); the Speaker and Wind chimes name the Bell and meter as before them |
| Speaker (CAT-061) | 75a | **75b** | As above |
| Wind chimes (CAT-069) | 76a | **75b** | As above |
| Tone-selective meter, Continuous-tone speaker (EL-044, EL-179) | 75b | **76a** | EL-044 needs the Bell, the meter and the Speaker (EL-044 Prerequisites) |
| Acoustic resonator, screen and dish (EL-049 to EL-051) | 79a | **78a** | EL-046 follows EL-049 to EL-051 in its stated sound order (EL-046 Prerequisites) |
| Listening horn, Exit horn, Acoustic duct (EL-046 to EL-048) | 78a | **79a** | As above; the horn, duct and exit horn stay together |
| Predator character (EL-087) | 98 | **97** | EL-083's row B9 and EL-087 itself put EL-082, EL-083 and EL-087 in one slice; the Fish tank lure (97b) needs EL-082 and EL-087 (EL-088 Prerequisites) |
| Tank-level transmission gauge (EL-130) | chapter 12 (111–120) | **128**, chapter 13 | The Water moderator tank (RAD-17, 126) is every row's vessel (EL-130 Dependencies) |

**Totals.**

| Chapter | Levels | New levels in it | New parts | Most in one level |
| --- | --- | --- | --- | --- |
| 1 · On a Roll | 17 | 7 | 25 | 3 |
| 2 · A Helping Weight | 25 | 15 | 28 | 3 |
| 3 · Pipe Dreams | 20 | 10 | 27 | 2 |
| 4 · Gears of an Idea | 17 | 7 | 23 | 3 |
| 5 · Ready, Set, Throw! | 18 | 8 | 26 | 3 |
| 6 · A Bright Idea | 27 | 17 | 42 | 3 |
| 7 · Go with the Flow | 23 | 13 | 44 | 3 |
| 8 · Sounds Like a Plan | 20 | 10 | 32 | 3 |
| 9 · Stored Potential | 10 | 0 | 20 | 3 |
| 10 · Oddly Satisfying | 17 | 7 | 28 | 3 |
| 11 · A Ray of Possibility | 10 | 0 | 9 | 2 |
| 12 · Particle Manners | 10 | 0 | 7 | 1 |
| 13 · Half the Battle | 10 | 0 | 7 | 2 |
| 14 · Many Happy Returns | 10 | 0 | 3 | 1 |
| 15 · All Together Now | 10 | 0 | 0 | 0 |
| **Total** | **244** | **94** | **321** | **3** |


**Chapter 1 · On a Roll** (17 levels, 25 new parts)

| Level | New parts | Count | Authored level | Note |
| --- | --- | --- | --- | --- |
| 1 | Ramp, Basketball (CAT-001), Receiving basket | 3 | `first_principles` |  |
| 2 | Springboard | 1 | `spring_forward` | GAP-13 save |
| 2a | Bowling ball, Tennis ball, Baseball (EL-239) | 3 |  | new: ball materials |
| 2b | Soccer ball (EL-077), Programmable ball (EL-076) | 2 |  | new: presets |
| 3 | Trampoline | 1 | `a_gentle_rebound` (from 61) | GAP-14 |
| 3a | — | 0 | `ready_to_rebound` (from 62) | build-up: stored springboard, place the catcher |
| 4 | Domino, Structural beam (EL-112) | 2 | `domino_effect` (re-authored: no lamp; Captured goal into a Receiving basket) | GAP-01 beam; the brace moved to 9b (decision 36) |
| 5 | Pinball bumper | 1 | `bumper_sidekick` | GAP-01 practice |
| 6 | — | 0 | `bumper_depth` | depth (the chapter's depth introduction); GAP-17 |
| 7 | Wall | 1 | `wall_and_bumper` | GAP-15 |
| 7a | Brick barrier (EL-073), Wood barrier (EL-074) | 2 |  | new: wall materials |
| 8 | Pool ball (EL-098), Pool pocket (EL-099) | 2 |  | *Bank the Bounce* |
| 8a | Damped cushion (EL-215), Capture cradle (EL-216) | 2 |  | new: damped capture |
| 9 | Impact lever, Placeable pivot (EL-114), Linkage connector (EL-115) | 3 |  | GAP-02 |
| 9a | Balloon | 1 | `burst_of_motion` (from 63) | moved from chapter 2 (24e) |
| 9b | Structural brace (EL-113) | 1 |  | new: GAP-01 braced/unbraced frame on pivots (decision 36) |
| 10 | — | 0 |  | finale |

**Chapter 2 · A Helping Weight** (25 levels, 28 new parts)

| Level | New parts | Count | Authored level | Note |
| --- | --- | --- | --- | --- |
| 11 | Switch, Signal lamp | 2 | `power_trip` (from 2) | 24f |
| 11a | Fan | 1 | `air_mail` (from 3) | 24f |
| 11b | — | 0 | `third_dimension` (from 5) | build-up: fan through depth |
| 11c | — | 0 | `spring_signal` (from 7) | build-up: springboard to switch |
| 11d | — | 0 | `cold_start` (from 9) | build-up: switch supplies fan |
| 12 | Battery, Motor | 2 | `battery_motor` (from 14) |  |
| 12a | — | 0 | `switched_motor` (from 15) | build-up |
| 12b | Weight, Rope | 2 | `counterweight` (from 19) | pulley is a fixture here |
| 12c | Rope anchor, Pulley | 2 | `pulley_depth` (from 20) | anchor first (EL-206 Dependencies); the authored level ties Weights through Pulleys only and places no anchor, so `pulley_depth`'s Early level proof authors the anchor into it (completion-ledger OS-09, owner decision 24o(3); [CAT-058](../CAT-058-rope_anchor.md)) |
| 13 | Thumb tack (EL-101) | 1 |  | balloon pop lesson |
| 14 | Electrical heating plate (TH-04) | 1 |  |  |
| 14a | Conveyor | 1 | `conveyor_courier` (from 16) |  |
| 14b | — | 0 | `belt_relay` (from 17) | build-up: shaft relay |
| 14c | Reverse transmission | 1 | `reverse_belt` (from 18) |  |
| 14d | Drive belt (EL-200), Drive chain (EL-201) | 2 |  | new |
| 15 | Temperature sensor (TH-21), Heat-sensitive target (TH-37) | 2 |  | GAP-15 practice |
| 16 | Heat-conducting bar (TH-08) | 1 |  |  |
| 17 | Moving pulley (EL-207), Steel cable (EL-067), Load hook (EL-065) | 3 |  | GAP-18 gravity |
| 17a | Moving bucket (EL-069), Moving cage (EL-070) | 2 |  | new |
| 17b | Scissors (EL-066), Tin snips (EL-068) | 2 |  | new |
| 18 | Passive wheel (EL-116), Axle (EL-117) | 2 |  | GAP-03 |
| 19 | Insulating panel (TH-09) | 1 |  |  |
| 19a | — | 0 | `two_deliveries` (from 29) | build-up: second destination |
| 19b | — | 0 | `double_cold_start` (from 49, converted) | build-up: switch-started conveyor |
| 20 | — | 0 | `grand_contraption` (from 58, converted; *One Thing Leads to Another*) | finale; GAP-10 |

**Chapter 3 · Pipe Dreams** (20 levels, 27 new parts)

| Level | New parts | Count | Authored level | Note |
| --- | --- | --- | --- | --- |
| 21 | Clear pipe | 1 | `clear_pipe` (from 25) |  |
| 21a | 45° bend | 1 | `gentle_bend` (from 26) |  |
| 22 | 90° bend | 1 | `quarter_bend` (from 27) |  |
| 22a | — | 0 | `joined_pipe` (from 28) | joining mouths |
| 23 | Funnel, Ball detector | 2 |  |  |
| 23a | Delay | 1 | `delayed_signal` (from 23) |  |
| 24 | Powered gate, Hold timer | 2 |  |  |
| 24a | Large-bore ball pipe (EL-102) | 1 |  | *Bore Necessities* |
| 25 | Pressure plate, Weight tray (EL-059) | 2 |  | GAP-16 quantity; tray moved from 10 |
| 25a | State latch | 1 |  | new |
| 26 | Counter, Resettable counter (EL-183) | 2 |  | GAP-16 practice |
| 26a | Ball hopper (EL-241), Fixed ball diverter (EL-162) | 2 |  | new: *Queue the applause* |
| 27 | Powered ball diverter (EL-163), Alternating ball diverter (EL-164) | 2 |  |  |
| 27a | Vacuum nozzle (EL-100) | 1 |  | *Clean Sweep* |
| 28 | Repeating clock, Egg timer (EL-236) | 2 |  | egg timer moved from 10 |
| 28a | Mechanical ball gate (EL-184), Size grate (EL-058) | 2 |  | new; grate moved from 10 |
| 28b | Spiral delay tube (EL-107), Speed-sensitive trapdoor (EL-109) | 2 |  | new; moved from 10 |
| 28c | Powered airlift (EL-108), Selective trap door (EL-238) | 2 |  | new; moved from 10 |
| 29 | — | 0 |  | GAP-16 rate; practice |
| 30 | — | 0 |  | finale |

**Chapter 4 · Gears of an Idea** (17 levels, 23 new parts)

| Level | New parts | Count | Authored level | Note |
| --- | --- | --- | --- | --- |
| 31 | Electrical AND (Both gate) | 1 |  |  |
| 31a | Electrical OR | 1 |  | new |
| 31b | Electrical XOR | 1 |  | new |
| 32 | Electrical NOR | 1 |  |  |
| 32a | Electrical NAND | 1 |  | new |
| 32b | Rising-edge detector (EL-181), Falling-edge detector (EL-182) | 2 |  | new |
| 33 | Gears (EL-232) | 1 |  |  |
| 33a | Clutch, Mechanical brake (EL-055) | 2 |  | new |
| 34 | Generator (EL-233), Windmill | 2 |  |  |
| 34a | Mouse motor (EL-234) | 1 |  | new |
| 35 | Rotary-to-linear (EL-053), Linear-to-rotary (EL-054) | 2 |  |  |
| 36 | Crank-slider (EL-218), Rack-and-pinion (EL-219) | 2 |  |  |
| 36a | Single-lobe cam (EL-156), Double-lobe cam (EL-157), Rise-hold-fall cam (EL-158) | 3 |  | new |
| 37 | Driven wheel (GAP-04) | 1 |  |  |
| 38 | — | 0 |  | practice |
| 39 | Friction brake (TH-05), Finned heat sink (TH-10) | 2 |  | GAP-15; heat sink moved from 40 (decision 35) |
| 40 | — | 0 |  | finale |

**Chapter 5 · Ready, Set, Throw!** (18 levels, 26 new parts)

| Level | New parts | Count | Authored level | Note |
| --- | --- | --- | --- | --- |
| 41 | Wound launcher | 1 | `wind_then_release` (from 59) |  |
| 41a | — | 0 | `saved_for_later` (from 60) | build-up: retained charge |
| 41b | Linear pusher, Boxing glove (EL-237) | 2 |  | new |
| 42 | Supplied flipper (EL-222), Jack-in-the-box (EL-235) | 2 |  | GAP-17 |
| 42a | Passive impact scoop (EL-223), Counterweighted scoop (EL-224) | 2 |  | new |
| 43 | Tension-limited (EL-159), Shear-limited (EL-160), Bending-limited connector (EL-161) | 3 |  | GAP-05 |
| 44 | Latched slingshot (EL-225), Hinged catapult (EL-226) | 2 |  | GAP-05 practice |
| 44a | Pool cue (EL-097) | 1 |  | *On Cue* |
| 45 | Cannon, Cannonball (EL-091) | 2 |  | *Load Before Fire* |
| 45a | Reload shuttle (EL-217) | 1 |  | new: *One Ball at a Time* (hopper feeds the shuttle) |
| 46 | Electromagnet (EL-060), Magnetic impulse relay (EL-227) | 2 |  | GAP-02 reuse |
| 47 | Cable winch (EL-063) | 1 |  | *Catch, Then Lift* |
| 47a | Docking lift (EL-220), Docking ferry (EL-062) | 2 |  | new: *Dock Before Drop* |
| 48 | Parallel gripper (EL-221) | 1 |  | *Carry Without Iron*; GAP-04 reuse |
| 48a | Accelerator tube (EL-103) | 1 |  | *Up to Speed* |
| 48b | Zipline carriage (EL-228), Releasable joint (EL-185), Temporary bridge (EL-186) | 3 |  | new |
| 49 | — | 0 |  | GAP-16 order |
| 50 | — | 0 |  | finale |

**Chapter 6 · A Bright Idea** (27 levels, 42 new parts)

| Level | New parts | Count | Authored level | Note |
| --- | --- | --- | --- | --- |
| 51 | Flashlight, Laser, Light receiver | 3 |  | hit a receiver with either source (one receiver law, owner decision 9); Laser moved from 51d (decision 36) |
| 51a | Solar panel | 1 | `solar_motor` (from 21) |  |
| 51b | — | 0 | `solar_shadow` (from 22) | build-up: shade |
| 51c | — | 0 | `delayed_solar` (from 24) | build-up: delay |
| 51d | Rope-operated light (EL-155) | 1 |  | new |
| 52 | Straight metal pipe (EL-071), 90° metal bend (EL-072), 45° metal bend (EL-242) | 3 |  | interrupt with a ball |
| 52a | Mirror | 1 |  | new: reflect |
| 53 | Beam shutter, Beam splitter | 2 |  | finite-power splitting before the spectral lessons (chapter 6 progression) |
| 53a | General-light receiver (EL-153), Light-charge receiver (EL-154) | 2 |  | new: *Hold the Light* |
| 54 | Red laser (EL-176), Green laser (EL-177), Blue laser (EL-178) | 3 |  |  |
| 54a | Red receiver, Green receiver, Blue receiver | 3 |  | new; before the filters (decision 36) |
| 54b | Red filter, Green filter, Blue filter | 3 |  | new |
| 54c | Beam combiner, Yellow receiver, Cyan receiver | 3 |  | new |
| 54d | Magenta receiver, White receiver | 2 |  | new |
| 55 | Solar absorber plate (TH-07) | 1 |  |  |
| 55a | Optical AND | 1 |  | new |
| 55b | Optical OR | 1 |  | new |
| 55c | Optical XOR | 1 |  | new |
| 55d | Optical NOR | 1 |  | new |
| 55e | Optical NAND | 1 |  | new |
| 56 | Candle (TH-01) | 1 |  |  |
| 57 | Fire bowl (TH-02) | 1 |  |  |
| 57a | Diverging lens (EL-173), Optical fibre (EL-175) | 2 |  | new |
| 58 | Combustible block (TH-03), Tinder pad (TH-32), Flint striker (TH-31) | 3 |  | the striker lights a pad (TH-31 Prerequisites) |
| 59 | Converging lens (TH-06) | 1 |  |  |
| 59a | Prism (EL-174) | 1 |  | new |
| 60 | — | 0 |  | finale |

**Chapter 7 · Go with the Flow** (23 levels, 44 new parts)

| Level | New parts | Count | Authored level | Note |
| --- | --- | --- | --- | --- |
| 61 | Finite reservoir (EL-001), Tap (EL-003), Catch basin (EL-004) | 3 |  | source and container pair |
| 61a | Header tank (EL-002), Drain (EL-006) | 2 |  | new |
| 61b | Liquid funnel (EL-005), Open gutter (EL-007) | 2 |  | new |
| 62 | Straight water pipe (EL-008), 45° water elbow (EL-009), 90° water elbow (EL-010) | 3 |  |  |
| 62a | Water pipe cap (EL-012), Water T junction (EL-011), Water nozzle (EL-013) | 3 |  | new; cap first (EL-011 row J2) |
| 63 | Float (EL-016), Mechanical float valve (EL-017), Electronic level switch (EL-018) | 3 |  | moved from 64 (decision 36) |
| 63a | Water-carrying bucket (EL-014), Leaky bucket (EL-015) | 2 |  | new |
| 64 | Mechanically actuated tap (EL-166), Solenoid tap (EL-167) | 2 |  | moved from 63: the solenoid tap's row E8 uses the level switch (decision 36) |
| 64a | Check valve (EL-019), Water diverter (EL-020), Sluice gate (EL-021) | 3 |  | new |
| 65 | Water pump (EL-022), Archimedes screw (EL-023) | 2 |  |  |
| 65a | Primed siphon (EL-024), Siphon priming bulb (EL-168) | 2 |  | new; the bulb needs the siphon (EL-168 Prerequisites), and the siphon's primed rows run with the bulb |
| 65b | Tipping-bucket clock (EL-025), Waterwheel (EL-240) | 2 |  | new |
| 66 | Cold pack (TH-29) | 1 |  | GAP-15 fluid load |
| 66a | Communicating tank (EL-026), Buoyant platform (EL-028) | 2 |  | new; platform moved from 67a (decision 36) |
| 67 | Ice block (TH-14) | 1 |  |  |
| 67a | Canal lock (EL-027), Boat (EL-029) | 2 |  | new; the lock lifts the platform and the boat uses the lock (decision 36) |
| 68 | Ice plug (TH-15) | 1 |  |  |
| 68a | Cork float (EL-169), Raft (EL-170) | 2 |  | new |
| 69 | Heat exchanger (TH-11) | 1 |  |  |
| 69a | Hydraulic piston (EL-229) | 1 |  | new |
| 69b | Flow meter (EL-030), Volume meter (EL-031), Pressure meter (EL-032) | 3 |  | new |
| 69c | Kettle (TH-17) | 1 |  | new: moved from the finale 70 (decision 35) |
| 70 | — | 0 |  | finale |

**Chapter 8 · Sounds Like a Plan** (20 levels, 32 new parts)

| Level | New parts | Count | Authored level | Note |
| --- | --- | --- | --- | --- |
| 71 | Bellows, Pneumatic hose (EL-038), Air reservoir (EL-039) | 3 |  | within the level the hose is taught before the air reservoir (coordinator decisions 37 and 41); the level fixes two reservoirs as lesson fixtures (structural story S8; fixed fixtures are not teaching) so the player's first placement is the hose, and the reservoir is placed by the player in the level's next step |
| 72 | Release valve (EL-040), Pneumatic pressure gauge (EL-043) | 2 |  | air reservoir lesson |
| 73 | Evaporative cooling pad (TH-28) | 1 |  |  |
| 73a | Air nozzle (EL-042) | 1 |  | *Jet Set* |
| 74 | Condenser (TH-18) | 1 |  |  |
| 74a | Air compressor (EL-037) | 1 |  | *Pump It Up* |
| 74b | Directional valve (EL-041), Spring-return piston (EL-230), Double-acting piston (EL-231) | 3 |  | new |
| 75 | Steam piston (TH-19) | 1 |  |  |
| 75a | Bell, Sound meter | 2 |  | new: a struck bell heard across a gap; the Bell is the meter's acceptance source (decision 36) |
| 75b | Speaker (pulse), Wind chimes | 2 |  | new: *Across the Gap* (a powered pulse) and wind-struck chimes, after the Bell and meter (decision 36) |
| 76 | Steam turbine (TH-20) | 1 |  |  |
| 76a | Tone-selective meter (EL-044), Continuous-tone speaker (EL-179) | 2 |  | new; after the Bell, meter and Speaker (decision 36) |
| 77 | Gas expansion bladder (TH-24) | 1 |  |  |
| 77a | Air whistle (EL-045), Water-tuned bottle (EL-052) | 2 |  | new: *Fill to Sing* |
| 78 | Reversible heat pump (TH-12) | 1 |  |  |
| 78a | Acoustic resonator (EL-049), Acoustic screen (EL-050), Acoustic dish (EL-051) | 3 |  | new; before the horn route (decision 36) |
| 79 | Freezing mold (TH-13) | 1 |  | GAP-18 atmosphere |
| 79a | Listening horn (EL-046), Acoustic duct (EL-048), Exit horn (EL-047) | 3 |  | new; the route in its slice order (EL-048 Related, EL-047 Prerequisites) |
| 79b | Hot-air balloon (TH-25) | 1 |  | new: moved from the finale 80 (decision 35) |
| 80 | — | 0 |  | finale |

**Chapter 9 · Stored Potential** (10 levels, 20 new parts)

| Level | New parts | Count | Authored level | Note |
| --- | --- | --- | --- | --- |
| 81 | Thermal storage block (TH-26), Ratchet (EL-056), Escapement (EL-057) | 3 |  |  |
| 82 | Expansion rod (TH-23), Indexed carousel (EL-061) | 2 |  |  |
| 83 | Bimetal thermostat (TH-22), Flywheel (EL-110), Centrifugal governor (EL-111) | 3 |  |  |
| 84 | Phase-change cartridge (TH-27), Fluid accumulator (EL-033) | 2 |  |  |
| 85 | Fusible link (TH-16), Spring-mounted match (TH-33) | 2 |  |  |
| 86 | Coffee-pot steam vessel (TH-35), Lamp-trigger apparatus (TH-36) | 2 |  |  |
| 87 | Thermoelectric generator (TH-30), Sponge (EL-034), Squeeze pad (EL-171) | 3 |  |  |
| 88 | Wick (EL-035), Rain collector (EL-172), Sprinkler (EL-036) | 3 |  | the sprinkler needs the collector (EL-036 Prerequisites) |
| 89 | — | 0 |  | GAP-16 end state |
| 90 | — | 0 |  | finale |

**Chapter 10 · Oddly Satisfying** (17 levels, 28 new parts)

| Level | New parts | Count | Authored level | Note |
| --- | --- | --- | --- | --- |
| 91 | Granular dispenser (GAP-06), Timed toaster ejector (TH-34) | 2 |  |  |
| 91a | Toy pulse emitter (EL-075), Message display (EL-081) | 2 |  | new |
| 92 | Programmable box (EL-080), Tipsy platform (EL-096) | 2 |  |  |
| 93 | Granular sieve (GAP-07), Can opener (EL-078) | 2 |  |  |
| 93a | Electric mixer (EL-079), Gravity-effect pad (EL-089) | 2 |  | new |
| 94 | Coating station (EL-118), Dye station (EL-119) | 2 |  |  |
| 95 | Fragmentation station (GAP-09), Toy dynamite (EL-093), Detonation plunger (EL-094) | 3 |  |  |
| 95a | Impact-sensitive charge (EL-106) | 1 |  | *Gently Does It* |
| 96 | Toy rocket (EL-092), Toy revolver (EL-095) | 2 |  |  |
| 96a | Toy firework (EL-104) | 1 |  | *Fuse Box* |
| 97 | Lured character (EL-082), Escaping character (EL-083), Predator character (EL-087) | 3 |  | one slice (EL-083 row B9, EL-087 Prerequisites); predator moved from 98 (decision 36) |
| 97a | Toy missile (EL-105) | 1 |  | *Follow the Light* |
| 97b | Fragile fish bowl (EL-084), Fish tank lure (EL-088) | 2 |  | new; the lure follows EL-082, EL-084 and EL-087 |
| 98 | Obstacle-reversing walker (EL-086) | 1 |  | GAP-11 |
| 98a | Rope-driven character wheel (EL-085), Steerable blimp (EL-090) | 2 |  | new |
| 99 | — | 0 |  | practice |
| 100 | — | 0 |  | finale |

**Chapters 11 to 15** keep their ten slots each. The radiation parts take their exact allocation ([radiation records](../../requirements.md#radiation-01)); the shields, gauges and neutron detectors take the first lesson their spec's Campaign line names. A slot not listed introduces nothing.

| Chapter | Level | New parts | Count |
| --- | --- | --- | --- |
| 11 | 101 | Gamma source capsule (RAD-01), Radiation rate meter (RAD-08) | 2 |
| 11 | 102 | Thin-screen shield (EL-126), Dense shield (EL-128) | 2 |
| 11 | 104 | Polymer shield (EL-127) | 1 |
| 11 | 105 | Powered radiation shutter (RAD-06) | 1 |
| 11 | 106 | Collimator block (RAD-07) | 1 |
| 11 | 107 | Powered X-ray emitter (RAD-02) | 1 |
| 11 | 109 | Integrating dosimeter (RAD-09) | 1 |
| 12 | 111 | Exposure-sensitive cargo badge (RAD-10) | 1 |
| 12 | 113 | Sheet-thickness transmission gauge (EL-129) | 1 |
| 12 | 115 | Alpha source cartridge (RAD-03) | 1 |
| 12 | 116 | Beta-minus source cartridge (RAD-04) | 1 |
| 12 | 117 | Scintillator tile (RAD-12) | 1 |
| 12 | 118 | Magnetic deflector (RAD-14) | 1 |
| 12 | 119 | Track chamber (RAD-15) | 1 |
| 13 | 123 | Decay clock capsule (RAD-13) | 1 |
| 13 | 125 | Neutron source module (RAD-16), Fast-neutron detector (EL-131) | 2 |
| 13 | 126 | Slow-neutron detector (EL-132), Water moderator tank (RAD-17) | 2 |
| 13 | 127 | Neutron absorber panel (RAD-18) | 1 |
| 13 | 128 | Tank-level transmission gauge (EL-130) | 1 |
| 14 | 131 | Radioisotope thermoelectric generator (RAD-20) | 1 |
| 14 | 133 | Radiation-responsive material latch (RAD-21) | 1 |
| 14 | 135 | Sealed tracer capsule (RAD-22) | 1 |

Chapter 15 introduces nothing (136–150). EL-130 moved from chapter 12 to 128, after the moderator tank it reads (decision 36); 128 is the tank's practice level.

**Progression check, whole campaign.**
- **Prerequisites earlier (re-run 23 November 2026, coordinator decision 36).** This record's first pass claimed that every part's prerequisites were introduced earlier; the cross-batch audit (check 9) disproved it. The re-run extracted every part named on each spec's Prerequisites, Dependencies, Element dependencies and "planned before / in the same slice" lines (part ids, catalogue names and Story numbers) and compared its first use here with the spec's own, including order within a level. Result:
  - **Moved:** the 28 parts in the 16 rows of the decision-36 table above. In-level order was also corrected at 12c (anchor before pulley), 58 (pad before striker), 62a (cap before T junction), 71 (hose before reservoir, coordinator decisions 37 and 41), 79a (horn, duct, exit horn) and 88 (collector before sprinkler).
  - **Not ordering conflicts:** a list of alternatives with one already taught (EL-155's observer, EL-212's laser, EL-082's lure); collaborators called "later users", "optional" or "not blocking"; and rows the spec already runs in the later part's slice (CAT-038 R4b, CAT-070's Bellows row, EL-028 K5, EL-032 M6, EL-044 row 5, EL-128 D4, EL-131 F5 and F6, EL-179 row 2, EL-225, RAD-04 B5, RAD-16's later rows, TH-12 A6, TH-37's light rows).
  - **No test-partner exemption (coordinator decision 41, 24 November 2026).** A delivered part that the campaign teaches later is not exempt as "test data": a row that uses it moves to that part's slice. The independent plan review found rows that used the Weight, Rope and Rope anchor (taught at 12b and 12c) in the specs of the beam (4), pivot and linkage (9) and brace (9b); the hopper's clock and NOR row (26a); the cradle's Powered gate row (8a); and the Powered gate's Counter, Pressure plate, Cannon and Wound spring rows (24). Each of those rows now says "a cross-part row owned by the ... slice": EL-112 A2, EL-113 B1 and B2, EL-114 P1, P3, P5 and P6 (which also closes EL-112 A3), EL-115 L1 to L4, EL-241 H6, EL-216 A5 and the CAT-051 partner rows (facts 23 to 25; fact 22's Hold timer is taught with the gate at 24). Each receiving spec lists the rows it owns: CAT-067 and EL-204 (the Weight rows), EL-205 (L4), CAT-058 (A2), CAT-025 and EL-136 (H6), CAT-051 (A5), CAT-020, CAT-052, CAT-016 and CAT-071 (facts 23, 24 and 25).
  - **Moved to the later part's slice (row-level; no campaign move fits; each spec's row now says "a cross-part row owned by the ... slice", coordinator decisions 36 and 41):** EL-005 F2 and EL-010 E2 need the Water pipe cap (62a), which itself needs the straight pipe (62) and the 90° elbow (row C4); TH-01 A4, TH-02 B4, TH-05 H6, TH-12 A5 and TH-32 P5 and P9 need the Thermal storage block (81), TH-32 P6 the Converging lens (59) and P9 the Spring-mounted match (85), under the exact thermal allocation (owner decision 18); EL-176 to EL-178 rows A1 and A10 need the colour receivers (54a), which need the lasers first; EL-039's depletion rows need a valve feeding a nozzle or piston (72 to 74b), while the hose and valve need the reservoir first; RAD-17's two control rows need RAD-18 (127); the Beam splitter (53) is kept before the colour lessons by chapter 6's stated progression ("finite-power splitting before spectral and logical combinations"), so CAT-008's "Blue filter and Blue receiver (delivered by then)" and CAT-011's row F10 must run in the blue filter's slice (54b). Within one level, each needing the part listed after it, which in turn needs it: EL-024's primed rows and EL-168 (65a), EL-041's motion rows and EL-231 (74b), EL-132's scene rows and RAD-17 (126). Delivery wording outside the Campaign lines: EL-099 "last of the three pool identities" and EL-097 "before EL-099" (the pocket is taught at 8, the cue at 44a); EL-241 "deliver after EL-237 if both are queued" (the hopper is taught at 26a, the glove at 41b); and the "Colour receivers" family slice, whose members run from 51 (CAT-038) to 54d.
  - No first use is after slot 135.
- **Introduction to combination:** every chapter opens with introductions, places its build-up levels after the parts they combine, and ends on a combination finale that introduces no part (10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150). Coordinator decision 35 moved the four finale introductions: TH-10 Finned heat sink from 40 to 39 (39 now introduces 2 parts), TH-17 Kettle from 70 to the new level 69c (69b already introduces 3, so 69c introduces 1), TH-25 Hot-air balloon from 80 to the new level 79b (79a already introduces 3, so 79b introduces 1) and RAD-15 Track chamber from 120 to 119 (119 now introduces 1).
- **New parts per level:** at most 3 in every level; 188 levels introduce 1 to 3 parts and the rest are build-ups, practice or finales.
- **Authored levels:** all 34 kept levels sit in the chapter their parts allow (section 5); none introduces a part before its chapter.

## 7. Level counts

| Count | Before (this record's first pass) | After |
| --- | --- | --- |
| Planned campaign levels | 150 slots (then 159 with the 24c levels) | **244**: 150 slots + 94 lettered levels |
| Of those, already authored (kept) | — | 34 |
| Lettered levels holding a relocated authored level | — | 21 |
| Lettered levels to author new | 9 | 73 (9 under 24c, 61 under 24e, 2 under coordinator decision 35, 1 under coordinator decision 36) |
| Placeable parts placed in a level | none: each part had a chapter range only | all 321, each in one exact level |
| Most new parts in one level | unbounded (chapter 7: 44 first uses for 10 levels) | 3 |

## 8. Needs the owner

Nothing blocks the plan. One choice is the owner's to confirm and is stated in the last paragraph of this section: the thermal rows that wait for the Thermal storage block (taught at 81) run in its slice under coordinator decision 41, and the owner may instead move the block earlier.

Resolved by the owner on 23 November 2026: chapter density (24e), the chapter 1/2 boundary (24f) and the 150-level wording in `AGENTS.md` (now "at least 150"). Resolved by coordinator decision 35: the four finale introductions (section 6), and EL-098's variant B (a no-gravity pool table), which the EL-098 slice decides at its planning (owner decision of 21 November 2026); the lessons above use the gravity table. Resolved by coordinator decision 36: the prerequisite-order conflicts (section 6); the row-level items it lists moved to the later part's slice in each spec (coordinator decision 41, which also removed the "test partner" exemption). The owner may still choose differently for the thermal rows (TH-01 A4, TH-02 B4, TH-05 H6, TH-12 A5, TH-32 P5 and P9): owner decision 18 fixes the thermal allocation, so the Thermal storage block (TH-26) is taught at 81 and the rows that need it run in its slice; moving the block earlier would be an owner decision.

## 9. Where the changes are, and what is not done

- **Requirements** ([campaign plan](../../requirements.md#campaign-plan)): the campaign target and Total row (244), sequence-tasks 315, 322 and 344 (cite 24d and 24e), chapter rows 1, 3, 5, 6, 8 and 10, [todo-437](../../requirements.md#todo-437) annotations, one new ledger row, and the [campaign coverage additions](../../requirements.md#campaign-coverage-additions) section (the 18 parts, the moved introductions, the per-chapter level counts and a pointer to section 6 here); a **Campaign:** clause on element rows EL-037, 042, 043, 063, 071, 072, 091, 097 to 106. Coordinator decision 36 added: chapter rows 12 and 13 and the radiation ledger rows (EL-130 at 128), the GAP-01 row (brace at 9b), the prerequisite-order paragraph in the coverage additions, and a **Campaign:** clause on EL-113 and EL-130.
- **[Chapter reservations](../../invest/campaign-reservations.md)**: the decision oracle, one exact-reservation bullet per new allocation in each chapter it touches, and the moved introductions in chapters 1, 3 and 10; for coordinator decision 36, the GAP-01 bullets, the chapter 12 and 13 scopes, and one bullet in each chapter whose introductions moved.
- **[Level scopes](../../invest/campaign.md)**: the header note (63 authored, 244 planned); a placement paragraph in each of `LEVEL-001` to `LEVEL-062` saying where its authored level goes (or why it is removed or converted), which authored level the slot now holds and which lettered levels after it hold authored content; the exact reservations of `LEVEL-039`, `040`, `069`, `070`, `079`, `080`, `119` and `120` for coordinator decision 35; and, for decision 36, the 9b note in `LEVEL-009` and the EL-130 reservation in `LEVEL-128`. No `LEVEL-` heading is added for a lettered level (see *Not done*).
- **[Epic 15](../../../../_bmad-output/planning-artifacts/epics.md#epic-15-progressively-taught-campaign-at-least-150-levels)**: the title and goal (at least 150 levels, owner decisions 24c to 24f), the pointer line; each chapter story names its level count and points to section 6; Story 15.1's list is the re-authored chapter 1.
- **[Delivery batches §4](../../invest/delivery-batches.md#4-early-level-proof)**: one line with the reconciled counts.
- **[Early level proof batch 1](../../../../_bmad-output/implementation-artifacts/spec-early-level-proof-batch-1.md)** (this effort's draft): `wall_return` removed, three levels with their chapter placement, and marked waiting for the engine programme (owner decision 22).
- **Element specs**: the Campaign line of the 17 parts that had no lesson, of the 8 parts whose introduction moved (CAT-003 and EL-058, 059, 107, 108, 109, 236, 238), and of TH-10, TH-17, TH-25 and RAD-15 (coordinator decision 35, with the RAD-15 heading in the [named-identity ledger](../../invest/named-elements.md#radiation-15)); for decision 36, the Campaign line of each part in its table and EL-112's, and EL-099's Integration line (the Pool cue is taught later, at 44a).
- **Not done:** `content/puzzles.json` is unchanged; the removals and placements wait for the content housekeeping slice (completion-ledger OS-02), which also updates any test that counts or names levels; the conversions wait for OS-08, and the `domino_effect` re-authoring, with `DominoEffectTests` and the lamp fixture row in `docs/planning/invest/current-consumers.md`, for OS-06 (owner decision 24o(3)). No `LEVEL-` headings for the 94 lettered levels yet. The [lettered level ids slice](../../../../_bmad-output/implementation-artifacts/spec-tool-lettered-level-ids.md) made the tools admit their form, `LEVEL-[0-9]{3}[a-z]?` (one lowercase letter, such as `LEVEL-024a`; ordinal order of the spelling is campaign order), in `Identity` (`tools/Ownership/CurrentOwnerMembership.cs`) and the grouped-role pattern (`tools/Coverage/CurrentOwnerRelations.cs`), and added a lettered `LEVEL-` case to the oracle's synthetic scope. Each slice that adds lettered headings raises the fixed heading census in `tools/Ownership/CurrentOwnerMembershipOracles.cs:70` (545) by the number it adds (94 gives 639). The [palette allocation](palette-allocation.md) numbers new parts in first-teaching order within each category (coordinator decision 36), so the decision-36 moves re-number parts of its category blocks and the matching spec Palette lines; that is the palette repair's to apply. The planning pass that wrote this section ran no build or test.

## 10. Coverage tables

Column *Recorded in* cites the authority; "row N" is the Nth row of the [element coverage ledger](../../requirements.md#campaign-element-coverage). Slot ranges are first-use reservations; exact numbers are exact objectives. The exact level of every part is in section 6; for the 8 moved introductions of 24e and EL-130's move to chapter 13 (coordinator decision 36) it replaces the range below.

### 10a. Catalogue parts (72)

Authored uses list kept `content/puzzles.json` levels by id; ᶠ marks a fixture (placed and locked by the level, not by the player).

| CAT | Kind | First use | Later uses | Recorded in (ledger) | Kept authored levels | Note |
| --- | --- | --- | --- | --- | --- | --- |
| CAT-001 | `ball` | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | `first_principles`ᶠ, `spring_forward`ᶠ, `domino_effect`ᶠ, `spring_signal`ᶠ, `bumper_sidekick`ᶠ, `bumper_depth`ᶠ, `wall_and_bumper`ᶠ, `conveyor_courier`ᶠ, `belt_relay`ᶠ, `reverse_belt`ᶠ, `solar_motor`ᶠ, `solar_shadow`ᶠ, `delayed_signal`ᶠ, `delayed_solar`ᶠ, `clear_pipe`ᶠ, `gentle_bend`ᶠ, `quarter_bend`ᶠ, `joined_pipe`ᶠ, `two_deliveries`ᶠ, `double_cold_start` (converted)ᶠ, `grand_contraption` (converted)ᶠ, `wind_then_release`ᶠ, `saved_for_later`ᶠ, `a_gentle_rebound`ᶠ, `ready_to_rebound`ᶠ | fixture only in kept levels; 23 removed level(s) also used it |
| CAT-002 | `ball_detector` | 21–40 | 41–80, 101–150 | row 4 | none |  |
| CAT-003 | `balloon` | 9a (moved from 11–20) | 31–50, 61–80, 136–150 | row 2 | `burst_of_motion`ᶠ | fixture only in kept levels |
| CAT-004 | `basket` | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | `first_principles`ᶠ, `air_mail`ᶠ, `spring_forward`ᶠ, `third_dimension`ᶠ, `cold_start`ᶠ, `bumper_sidekick`ᶠ, `bumper_depth`ᶠ, `wall_and_bumper`ᶠ, `conveyor_courier`ᶠ, `belt_relay`ᶠ, `reverse_belt`ᶠ, `clear_pipe`ᶠ, `gentle_bend`ᶠ, `quarter_bend`ᶠ, `joined_pipe`ᶠ, `two_deliveries`ᶠ, `double_cold_start` (converted)ᶠ, `wind_then_release`ᶠ, `saved_for_later`ᶠ, `a_gentle_rebound`ᶠ, `ready_to_rebound` | 21 removed level(s) also used it |
| CAT-005 | `battery` | 11–20 | 31–50, 61–80, 136–150 | row 2 | `battery_motor`, `switched_motor`ᶠ, `conveyor_courier`ᶠ, `belt_relay`ᶠ, `reverse_belt`ᶠ, `double_cold_start` (converted)ᶠ, `grand_contraption` (converted)ᶠ, `wind_then_release`ᶠ, `saved_for_later`ᶠ | 5 removed level(s) also used it |
| CAT-006 | `beam_combiner` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-007 | `beam_shutter` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-008 | `beam_splitter` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-009 | `bell` | 71–80 | 81–100, 117–125, 136–150 | row 12 | none |  |
| CAT-010 | `bellows` | 71–80 | 91–100, 136–150 | row 11 | none |  |
| CAT-011 | `blue_filter` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-012 | `blue_receiver` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-013 | `both_gate` | 21–40 | 41–80, 101–150 | row 4 | none |  |
| CAT-014 | `bowling` | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | `power_trip`ᶠ, `cold_start`ᶠ, `switched_motor`ᶠ, `double_cold_start` (converted)ᶠ, `grand_contraption` (converted)ᶠ, `burst_of_motion` | 15 removed level(s) also used it |
| CAT-015 | `bumper` | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | `bumper_sidekick`, `bumper_depth`, `wall_and_bumper` | 1 removed level(s) also used it |
| CAT-016 | `cannon` | 41–50 | 61–100, 111–120, 136–150 | row 6 | none |  |
| CAT-017 | `clock` | 21–40 | 41–80, 101–150 | row 4 | none |  |
| CAT-018 | `clutch` | 31–40 | 41–50, 61–90, 136–150 | row 5 | none |  |
| CAT-019 | `conveyor` | 11–20 | 31–50, 61–80, 136–150 | row 2 | `conveyor_courier`, `belt_relay`, `reverse_belt`, `double_cold_start` (converted)ᶠ, `grand_contraption` (converted) | 5 removed level(s) also used it |
| CAT-020 | `counter` | 21–40 | 41–80, 101–150 | row 4 | none |  |
| CAT-021 | `cyan_receiver` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-022 | `delay` | 21–40 | 41–80, 101–150 | row 4 | `delayed_signal`, `delayed_solar`, `wind_then_release`ᶠ, `saved_for_later`ᶠ |  |
| CAT-023 | `domino` | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | `domino_effect`, `grand_contraption` (converted) | 11 removed level(s) also used it |
| CAT-024 | `electrical_nand` | 21–40 | 41–80, 101–150 | row 4 | none |  |
| CAT-025 | `electrical_nor` | 21–40 | 41–80, 101–150 | row 4 | none |  |
| CAT-026 | `electrical_or` | 21–40 | 41–80, 101–150 | row 4 | none |  |
| CAT-027 | `electrical_xor` | 21–40 | 41–80, 101–150 | row 4 | none |  |
| CAT-028 | `fan` | 11–20 | 31–50, 61–80, 136–150 | row 2 | `air_mail`, `third_dimension`, `cold_start`ᶠ, `two_deliveries`, `double_cold_start` (converted)ᶠ | 17 removed level(s) also used it |
| CAT-029 | `flashlight` | 51–60 | 71–90, 111–125, 136–150 | row 7 | `solar_motor`ᶠ, `solar_shadow`ᶠ, `delayed_solar`ᶠ | fixture only in kept levels |
| CAT-030 | `funnel` | 21–30 | 41–50, 111–120, 136–150 | row 3 | none |  |
| CAT-031 | `green_filter` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-032 | `green_receiver` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-033 | `hold_timer` | 21–40 | 41–80, 101–150 | row 4 | `saved_for_later` |  |
| CAT-034 | `impact_lever` | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | none |  |
| CAT-035 | `lamp` | 11 (signal lamp; light source 51–60) | 71–90, 111–125, 136–150 | row 7 | `power_trip`ᶠ, `spring_signal`ᶠ, `counterweight`ᶠ, `pulley_depth`ᶠ, `delayed_signal`ᶠ, `grand_contraption` (converted)ᶠ | fixture only in kept levels; 12 removed level(s) also used it |
| CAT-036 | `laser` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-037 | `latch` | 21–40 | 41–80, 101–150 | row 4 | none |  |
| CAT-038 | `light_receiver` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-039 | `linear_pusher` | 41–50 | 61–100, 111–120, 136–150 | row 6 | none |  |
| CAT-040 | `magenta_receiver` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-041 | `mirror` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-042 | `motor` | 11–20 | 31–50, 61–80, 136–150 | row 2 | `battery_motor`ᶠ, `switched_motor`ᶠ, `conveyor_courier`ᶠ, `belt_relay`ᶠ, `reverse_belt`ᶠ, `solar_motor`ᶠ, `solar_shadow`ᶠ, `delayed_solar`ᶠ, `double_cold_start` (converted)ᶠ, `grand_contraption` (converted)ᶠ, `wind_then_release`ᶠ, `saved_for_later`ᶠ | fixture only in kept levels; 5 removed level(s) also used it |
| CAT-043 | `optical_and` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-044 | `optical_nand` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-045 | `optical_nor` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-046 | `optical_or` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-047 | `optical_xor` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-048 | `pipe` | 21–30 | 41–50, 111–120, 136–150 | row 3 | `clear_pipe`, `joined_pipe` |  |
| CAT-049 | `pipe_bend_45` | 21–30 | 41–50, 111–120, 136–150 | row 3 | `gentle_bend` |  |
| CAT-050 | `pipe_bend_90` | 21–30 | 41–50, 111–120, 136–150 | row 3 | `quarter_bend`, `joined_pipe`ᶠ |  |
| CAT-051 | `powered_gate` | 21–30 | 41–50, 111–120, 136–150 | row 3 | none |  |
| CAT-052 | `pressure_plate` | 21–40 | 41–80, 101–150 | row 4 | none |  |
| CAT-053 | `pulley` | 11–20 | 31–50, 61–80, 136–150 | row 2 | `counterweight`ᶠ, `pulley_depth` |  |
| CAT-054 | `ramp` | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | `first_principles`, `air_mail`, `two_deliveries`, `grand_contraption` (converted) | 14 removed level(s) also used it |
| CAT-055 | `red_filter` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-056 | `red_receiver` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-057 | `reverse_transmission` | 11–20 | 31–50, 61–80, 136–150 | row 2 | `reverse_belt` |  |
| CAT-058 | `rope_anchor` | 11–20 | 31–50, 61–80, 136–150 | row 2 | none |  |
| CAT-059 | `solar_panel` | 51–60 | 71–90, 111–125, 136–150 | row 7 | `solar_motor`, `solar_shadow`, `delayed_solar` |  |
| CAT-060 | `sound_meter` | 71–80 | 81–100, 117–125, 136–150 | row 12 | none |  |
| CAT-061 | `speaker` | 71–80 | 81–100, 117–125, 136–150 | row 12 | none |  |
| CAT-062 | `spring` | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | `spring_forward`, `spring_signal`, `double_cold_start` (converted), `ready_to_rebound`ᶠ | 13 removed level(s) also used it |
| CAT-063 | `switch` | 11–20 | 31–50, 61–80, 136–150 | row 2 | `power_trip`, `spring_signal`ᶠ, `cold_start`, `switched_motor`, `counterweight`ᶠ, `pulley_depth`ᶠ, `delayed_signal`ᶠ, `delayed_solar`ᶠ, `double_cold_start` (converted), `grand_contraption` (converted)ᶠ, `wind_then_release`ᶠ, `saved_for_later`ᶠ | 16 removed level(s) also used it |
| CAT-064 | `tennis` | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | `air_mail`ᶠ, `third_dimension`ᶠ, `cold_start`ᶠ, `two_deliveries`ᶠ, `double_cold_start` (converted)ᶠ | fixture only in kept levels; 17 removed level(s) also used it |
| CAT-065 | `trampoline` | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | `a_gentle_rebound` |  |
| CAT-066 | `wall` | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | `wall_and_bumper`, `solar_shadow`ᶠ | 1 removed level(s) also used it |
| CAT-067 | `weight` | 11–20 | 31–50, 61–80, 136–150 | row 2 | `counterweight`, `pulley_depth` |  |
| CAT-068 | `white_receiver` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |
| CAT-069 | `wind_chimes` | 71–80 | 81–100, 117–125, 136–150 | row 12 | none |  |
| CAT-070 | `windmill` | 31–40 | 41–50, 61–90, 136–150 | row 5 | none |  |
| CAT-071 | `wound_spring` | 41–50 | 61–100, 111–120, 136–150 | row 6 | `wind_then_release`, `saved_for_later`ᶠ |  |
| CAT-072 | `yellow_receiver` | 51–60 | 71–90, 111–125, 136–150 | row 7 | none |  |

### 10b. Named identities (302)

Consecutive identities with the same allocation share a row. "24c additions" is the [campaign coverage additions](../../requirements.md#campaign-coverage-additions) table; "GAP allocation", "thermal allocation" and "radiation allocation" are the exact allocations in requirements; "element row" is the **Campaign:** clause on the identity's requirement row.

| Identities | First use | Later uses | Recorded in | Note |
| --- | --- | --- | --- | --- |
| EL-001 Finite reservoir, EL-002 Header tank, EL-003 Tap, EL-004 Catch basin, EL-005 Liquid funnel, EL-006 Drain, EL-007 Open gutter, EL-008 Straight water pipe, EL-009 45-degree water elbow, EL-010 90-degree water elbow, EL-011 Water T junction, EL-012 Water pipe cap, EL-013 Water nozzle | 61–70 | 71–90, 114, 126–130, 136–150 | row 8 |  |
| EL-014 Water-carrying bucket, EL-015 Leaky bucket, EL-016 Float, EL-017 Mechanical float valve, EL-018 Electronic level switch, EL-019 Water check valve, EL-020 Water diverter, EL-021 Sluice gate, EL-022 Water pump, EL-023 Archimedes screw, EL-024 Primed siphon, EL-025 Tipping-bucket water clock | 61–70 | 71–90, 114, 126–130, 136–150 | row 9 |  |
| EL-026 Communicating tank, EL-027 Canal lock chamber, EL-028 Buoyant platform, EL-029 Boat, EL-030 Flow meter, EL-031 Volume meter, EL-032 Pressure meter | 61–70 | 81–100, 126–140, 146–150 | row 10 |  |
| EL-033 Fluid accumulator, EL-034 Sponge, EL-035 Wick, EL-036 Sprinkler | 81–90 | 91–100, 131–140, 146–150 | row 13 |  |
| EL-037 Air compressor | **74a** (new level *Pump It Up*) | 76, 87, 147 | 24c additions | **had no lesson; added** |
| EL-038 Pneumatic hose, EL-039 Air reservoir, EL-040 Pneumatic release valve, EL-041 Pneumatic directional valve | 71–80 | 91–100, 136–150 | row 11 |  |
| EL-042 Air nozzle | **73a** (new level *Jet Set*) | 75, 99, 143 | 24c additions | **had no lesson; added** |
| EL-043 Pneumatic pressure gauge | **72** (air reservoir lesson) | 73, 87, 147 | 24c additions | **had no lesson; added** |
| EL-044 Tone-selective sound meter, EL-045 Air whistle, EL-046 Listening horn, EL-047 Exit horn, EL-048 Acoustic duct, EL-049 Acoustic resonator, EL-050 Acoustic screen, EL-051 Acoustic dish, EL-052 Water-tuned bottle | 71–80 | 81–100, 117–125, 136–150 | row 12 |  |
| EL-053 Rotary-to-linear converter, EL-054 Linear-to-rotary converter, EL-055 Mechanical brake | 31–40 | 41–50, 61–90, 136–150 | row 5 |  |
| EL-056 Ratchet, EL-057 Escapement | 81–90 | 91–100, 131–140, 146–150 | row 13 |  |
| EL-058 Size grate, EL-059 Weight tray | 28a, 25 (moved from 91–100) | 111–120, 136–150 | row 15 |  |
| EL-060 Electromagnet | 41–50 | 61–100, 111–120, 136–150 | row 6 |  |
| EL-061 Indexed carousel | 81–90 | 91–100, 131–140, 146–150 | row 13 |  |
| EL-062 Docking ferry | 41–50 | 61–100, 111–120, 136–150 | row 6 |  |
| EL-063 Cable winch | **47** (*Catch, Then Lift*) | 48, 65, 138 | 24c additions | **had no lesson; added** |
| EL-064 Metal loop anchor | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 | duplicate of CAT-058 |
| EL-065 Load hook, EL-066 Scissors, EL-067 Steel cable, EL-068 Tin snips, EL-069 Moving bucket, EL-070 Moving cage | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 |  |
| EL-071 Straight metal ball pipe | **52** (light-interruption lesson) | 54, 142 | 24c additions | **had no lesson; added** |
| EL-072 Curved metal ball pipe | **52** (light-interruption lesson) | 54, 142 | 24c additions | **had no lesson; added** |
| EL-073 Brick barrier, EL-074 Wood barrier | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 |  |
| EL-075 Toy pulse emitter | 91–100 | 111–120, 136–150 | row 15 |  |
| EL-076 Programmable ball, EL-077 Soccer ball | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 |  |
| EL-078 Can opener, EL-079 Electric mixer, EL-080 Programmable box, EL-081 Message display | 91–100 | 111–120, 136–150 | row 15 |  |
| EL-082 Lured character, EL-083 Escaping character, EL-084 Fragile fish bowl, EL-085 Rope-driven character wheel, EL-086 Obstacle-reversing walker, EL-087 Predator character, EL-088 Fish tank lure, EL-089 Gravity-effect pad, EL-090 Steerable blimp | 91–100 | 136–150 | row 16 |  |
| EL-091 Cannonball | **45** (*Load Before Fire*) | 46, 68, 146 | 24c additions | **had no lesson; added** |
| EL-092 Toy rocket, EL-093 Toy dynamite charge, EL-094 Detonation plunger, EL-095 Toy revolver, EL-096 Tipsy platform | 91–100 | 136–150 | row 16 |  |
| EL-097 Pool cue | **44a** (new level *On Cue*) | 49, 141 | 24c additions | **had no lesson; added** |
| EL-098 Pool ball | **8** (*Bank the Bounce*) | 10, 49, 141 | 24c additions | **had no lesson; added** |
| EL-099 Pool pocket | **8** (*Bank the Bounce*) | 10, 49, 141 | 24c additions | **had no lesson; added** |
| EL-100 Vacuum nozzle | **27a** (new level *Clean Sweep*) | 29, 96, 147 | 24c additions | **had no lesson; added** |
| EL-101 Thumb tack | **13** (balloon pop lesson) | 19, 149 | 24c additions | **had no lesson; added** |
| EL-102 Large-bore ball pipe | **24a** (new level *Bore Necessities*) | 30, 141 | 24c additions | **had no lesson; added** |
| EL-103 Accelerator tube | **48a** (new level *Up to Speed*) | 50, 97, 148 | 24c additions | **had no lesson; added** |
| EL-104 Toy firework | **96a** (new level *Fuse Box*) | 99, 148 | 24c additions | **had no lesson; added** |
| EL-105 Toy missile | **97a** (new level *Follow the Light*) | 100, 142 | 24c additions | **had no lesson; added** |
| EL-106 Impact-sensitive toy charge | **95a** (new level *Gently Does It*) | 99, 149 | 24c additions | **had no lesson; added** |
| EL-107 Spiral gravity-delay tube, EL-108 Powered airlift, EL-109 Speed-sensitive trapdoor | 28b, 28c, 28b (moved from 91–100) | 111–120, 136–150 | row 3 |  |
| EL-110 Flywheel, EL-111 Centrifugal governor | 81–90 | 91–100, 131–140, 146–150 | row 13 |  |
| EL-112 Structural beam | 4 (GAP-01) | 5, 18, 65, 138 | GAP allocation |  |
| EL-113 Structural brace | 9b (GAP-01, after the pivot; coordinator decision 36) | 10, 18, 65, 138 | GAP allocation |  |
| EL-114 Placeable pivot, EL-115 Linkage connector | 9 (GAP-02) | 10, 38, 46, 138 | GAP allocation |  |
| EL-116 Passive wheel, EL-117 Axle | 18 (GAP-03) | 19, 37, 68, 138 | GAP allocation |  |
| EL-118 Coating station, EL-119 Dye station | 94 (GAP-08) | 95, 98, 136, 150 | GAP allocation |  |
| EL-120 Quantity goal | 25 (GAP-16, level data) | 26, 96, 136 | GAP allocation | level data, not placeable |
| EL-121 Rate-window goal | 29 (GAP-16, level data) | 30, 97, 140 | GAP allocation | level data, not placeable |
| EL-122 Ordered-events goal | 49 (GAP-16, level data) | 50, 100, 146 | GAP allocation | level data, not placeable |
| EL-123 Protected-end-state goal | 89 (GAP-16, level data) | 90, 100, 150 | GAP allocation | level data, not placeable |
| EL-124 Authored gravity preset | 17 (GAP-18, level data) | 18, 97, 139 | GAP allocation | level data, not placeable |
| EL-125 Authored atmosphere preset | 79 (GAP-18, level data) | 80, 99, 143 | GAP allocation | level data, not placeable |
| EL-126 Thin-screen shield, EL-127 Polymer shield, EL-128 Dense shield | 101–110 | 111–120, 124–130, 136–150 | row 17 |  |
| EL-129 Sheet-thickness transmission gauge | 111–120 (113) | 121–125, 136–150 | row 18 |  |
| EL-130 Tank-level transmission gauge | 128 (after RAD-17; coordinator decision 36) | 129–130, 136–150 | row 18 |  |
| EL-131 Fast-neutron detector, EL-132 Slow-neutron detector | 121–130 | 136–150 | row 19 |  |
| EL-133 Electrical AND gate | 21–40 | 41–80, 101–150 | row 4 | duplicate of CAT-013 |
| EL-134 Electrical OR gate | 21–40 | 41–80, 101–150 | row 4 | duplicate of CAT-026 |
| EL-135 Electrical XOR gate | 21–40 | 41–80, 101–150 | row 4 | duplicate of CAT-027 |
| EL-136 Electrical NOR gate | 21–40 | 41–80, 101–150 | row 4 | duplicate of CAT-025 |
| EL-137 Electrical NAND gate | 21–40 | 41–80, 101–150 | row 4 | duplicate of CAT-024 |
| EL-138 Optical AND gate | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-043 |
| EL-139 Optical OR gate | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-046 |
| EL-140 Optical XOR gate | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-047 |
| EL-141 Optical NOR gate | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-045 |
| EL-142 Optical NAND gate | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-044 |
| EL-143 Red optical filter | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-055 |
| EL-144 Green optical filter | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-031 |
| EL-145 Blue optical filter | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-011 |
| EL-146 Red selective receiver | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-056 |
| EL-147 Green selective receiver | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-032 |
| EL-148 Blue selective receiver | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-012 |
| EL-149 Yellow selective receiver | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-072 |
| EL-150 Cyan selective receiver | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-021 |
| EL-151 Magenta selective receiver | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-040 |
| EL-152 White selective receiver | 51–60 | 71–90, 111–125, 136–150 | row 7 | duplicate of CAT-068 |
| EL-153 General-light receiver, EL-154 Light-charge receiver, EL-155 Rope-operated light | 51–60 | 71–90, 111–125, 136–150 | row 7 |  |
| EL-156 Single-lobe cam, EL-157 Double-lobe cam, EL-158 Rise-hold-fall cam | 31–40 | 41–50, 61–90, 136–150 | row 5 |  |
| EL-159 Tension-limited connector, EL-160 Shear-limited connector, EL-161 Bending-limited connector | 43 (GAP-05) | 44, 65, 96, 139 | GAP allocation |  |
| EL-162 Fixed ball diverter, EL-163 Powered ball diverter, EL-164 Alternating ball diverter | 21–30 | 41–50, 111–120, 136–150 | row 3 |  |
| EL-165 Manual tap | 61–70 | 71–90, 114, 126–130, 136–150 | row 8 | duplicate of EL-003 |
| EL-166 Mechanically actuated tap, EL-167 Solenoid tap | 61–70 | 71–90, 114, 126–130, 136–150 | row 8 |  |
| EL-168 Siphon priming bulb | 61–70 | 71–90, 114, 126–130, 136–150 | row 9 |  |
| EL-169 Cork float, EL-170 Raft | 61–70 | 81–100, 126–140, 146–150 | row 10 |  |
| EL-171 Squeeze pad, EL-172 Rain collector | 81–90 | 91–100, 131–140, 146–150 | row 13 |  |
| EL-173 Diverging lens, EL-174 Prism, EL-175 Optical fibre, EL-176 Red laser emitter, EL-177 Green laser emitter, EL-178 Blue laser emitter | 51–60 | 71–90, 111–125, 136–150 | row 7 |  |
| EL-179 Continuous-tone speaker | 71–80 | 81–100, 117–125, 136–150 | row 12 |  |
| EL-180 Pulse speaker | 71–80 | 81–100, 117–125, 136–150 | row 12 | duplicate of CAT-061 |
| EL-181 Rising-edge detector, EL-182 Falling-edge detector, EL-183 Resettable counter | 21–40 | 41–80, 101–150 | row 4 |  |
| EL-184 Mechanical ball gate | 21–30 | 41–50, 111–120, 136–150 | row 3 |  |
| EL-185 Releasable assembly joint, EL-186 Temporary bridge | 41–50 | 61–100, 111–120, 136–150 | row 6 |  |
| EL-187 Basketball, EL-188 Bowling ball | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 |  |
| EL-189 Tennis ball | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | duplicate of CAT-064 |
| EL-190 Ramp, EL-191 Wall, EL-192 Receiving basket, EL-193 Domino, EL-194 Springboard | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 |  |
| EL-195 Pinball bumper | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 | duplicate of CAT-015 |
| EL-196 Battery | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 |  |
| EL-197 Electrical wire | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 | duplicate of delivered electrical connection |
| EL-198 Switch, EL-199 Electric motor, EL-200 Drive belt, EL-201 Drive chain | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 |  |
| EL-202 Conveyor | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 | duplicate of CAT-019 |
| EL-203 Reverse transmission | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 | duplicate of CAT-057 |
| EL-204 Weight | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 | duplicate of CAT-067 |
| EL-205 Rope | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 |  |
| EL-206 Fixed pulley | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 | duplicate of CAT-053 |
| EL-207 Moving pulley | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 |  |
| EL-208 Balloon | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 | duplicate of CAT-003 |
| EL-209 Fan | 11–20 (load variants 41–50) | 31–50, 61–80, 136–150 | row 2 | duplicate of CAT-028 |
| EL-210 Flashlight, EL-211 Solar panel | 51–60 | 71–90, 111–125, 136–150 | row 7 |  |
| EL-212 Flat mirror | 51–60 | 61–70, 111–120, 142 | element row | duplicate of CAT-041 |
| EL-213 Optical combiner | 51–60 | 71–80, 117, 142 | element row | duplicate of CAT-006 |
| EL-214 Broadband beam detector | 51–60 | 71–80, 110, 142 | element row | duplicate of CAT-038 |
| EL-215 Damped cushion, EL-216 Capture cradle | 1–10 | 41–50, 96, 149 | element row |  |
| EL-217 Reload shuttle with chamber interlock | 41–50 (shuttle) | 111–120, 136–150 | row 3 |  |
| EL-218 Crank-slider, EL-219 Rack-and-pinion slide | 31–40 | 41–50, 61–90, 136–150 | row 5 |  |
| EL-220 Docking lift, EL-221 Parallel gripper, EL-222 Supplied pinball flipper, EL-223 Passive impact scoop, EL-224 Counterweighted scoop catapult, EL-225 Latched slingshot cradle, EL-226 Hinged elastic catapult, EL-227 Magnetic impulse relay, EL-228 Zipline signal carriage | 41–50 | 61–100, 111–120, 136–150 | row 6 |  |
| EL-229 Hydraulic piston | 61–70 | 81–100, 126–140, 146–150 | row 10 |  |
| EL-230 Spring-return pneumatic piston, EL-231 Double-acting pneumatic piston | 71–80 | 91–100, 136–150 | row 11 |  |
| EL-232 Gears, EL-233 Generator, EL-234 Mouse motor | 31–40 | 41–50, 61–90, 136–150 | row 5 |  |
| EL-235 Jack-in-the-box | 41–50 | 61–100, 111–120, 136–150 | row 6 |  |
| EL-236 Egg timer | 28 (moved from 91–100) | 111–120, 136–150 | row 15 |  |
| EL-237 Boxing glove | 41–50 | 61–100, 111–120, 136–150 | row 6 |  |
| EL-238 Selective trap door | 28c (moved from 91–100) | 111–120, 136–150 | row 3 |  |
| EL-239 Baseball | 1–10 | 21–30, 41–50, 91–100, 136–150 | row 1 |  |
| EL-240 Waterwheel | 61–70 | 71–90, 114, 126–130, 136–150 | row 9 |  |
| EL-241 Ball hopper with one-ball escapement | 21–30 | 41–50, 111–120, 136–150 | row 3 |  |
| EL-242 45° metal ball pipe bend | **52** (light-interruption lesson) | 54, 142 | 24c additions | **had no lesson; added** |
| GAP-04 Driven wheel | 37 | 38, 48, 68, 138 | GAP allocation |  |
| GAP-06 Granular dispenser | 91 | 92, 96, 136, 141 | GAP allocation |  |
| GAP-07 Granular sieve | 93 | 94, 97, 136, 141 | GAP allocation |  |
| GAP-09 Fragmentation station | 95 | 96, 99, 141, 150 | GAP allocation |  |
| RAD-01 Gamma source capsule | 101 | 103, 110 | radiation allocation |  |
| RAD-02 Powered X-ray emitter | 107 | 114 | radiation allocation |  |
| RAD-03 Alpha source cartridge | 115 | 118 | radiation allocation |  |
| RAD-04 Beta-minus source cartridge | 116 | 119 | radiation allocation |  |
| RAD-06 Powered radiation shutter | 105 | 110 | radiation allocation |  |
| RAD-07 Collimator block | 106 | 114 | radiation allocation |  |
| RAD-08 Radiation rate meter | 101 | 108, 115, 116 | radiation allocation |  |
| RAD-09 Integrating dosimeter | 109 | 110 | radiation allocation |  |
| RAD-10 Exposure-sensitive cargo badge | 111 | 112 | radiation allocation |  |
| RAD-12 Scintillator tile | 117 | 122 | radiation allocation |  |
| RAD-13 Decay clock capsule | 123 | 124 | radiation allocation |  |
| RAD-14 Magnetic deflector | 118 | 119 | radiation allocation |  |
| RAD-15 Track chamber | 119 | 121 | radiation allocation |  |
| RAD-16 Neutron source module | 125 | 130 | radiation allocation |  |
| RAD-17 Water moderator tank | 126 | 128 | radiation allocation |  |
| RAD-18 Neutron absorber panel | 127 | 130 | radiation allocation |  |
| RAD-20 Radioisotope thermoelectric generator | 131 | 132 | radiation allocation |  |
| RAD-21 Radiation-responsive material latch | 133 | 134 | radiation allocation |  |
| RAD-22 Sealed tracer capsule | 135 | 136 | radiation allocation |  |
| TH-01 Candle | 56 | 57, 83, 137 | thermal allocation |  |
| TH-02 Fire bowl | 57 | 58, 85, 140 | thermal allocation |  |
| TH-03 Combustible block | 58 | 59, 85, 149 | thermal allocation |  |
| TH-04 Electrical heating plate | 14 | 15, 55, 70, 147 | thermal allocation |  |
| TH-05 Friction brake | 39 | 40, 81, 147 | thermal allocation |  |
| TH-06 Converging lens | 59 | 60, 85, 142 | thermal allocation |  |
| TH-07 Solar absorber plate | 55 | 56, 69, 131 | thermal allocation |  |
| TH-08 Heat-conducting bar | 16 | 17, 55, 82, 145 | thermal allocation |  |
| TH-09 Insulating panel | 19 | 20, 67, 84, 139 | thermal allocation |  |
| TH-10 Finned heat sink | 39 | 41, 78, 87, 145 | thermal allocation |  |
| TH-11 Heat exchanger | 69 | 70, 87, 137 | thermal allocation |  |
| TH-12 Reversible heat pump | 78 | 79, 89, 145 | thermal allocation |  |
| TH-13 Freezing mold | 79 | 80, 89, 149 | thermal allocation |  |
| TH-14 Ice block | 67 | 68, 89, 139 | thermal allocation |  |
| TH-15 Ice plug | 68 | 69, 86, 137 | thermal allocation |  |
| TH-16 Fusible link | 85 | 86, 96, 149 | thermal allocation |  |
| TH-17 Kettle | 69c | 71, 86, 140 | thermal allocation |  |
| TH-18 Condenser | 74 | 75, 87, 137 | thermal allocation |  |
| TH-19 Steam piston | 75 | 76, 86, 143 | thermal allocation |  |
| TH-20 Steam turbine | 76 | 77, 87, 147 | thermal allocation |  |
| TH-21 Temperature sensor | 15 | 16, 70, 83, 145 | thermal allocation |  |
| TH-22 Bimetal thermostat | 83 | 84, 90, 145 | thermal allocation |  |
| TH-23 Expansion rod | 82 | 83, 90, 146 | thermal allocation |  |
| TH-24 Gas expansion bladder | 77 | 78, 90, 143 | thermal allocation |  |
| TH-25 Hot-air balloon | 79b | 81, 99, 149 | thermal allocation |  |
| TH-26 Thermal storage block | 81 | 82, 90, 140 | thermal allocation |  |
| TH-27 Phase-change storage cartridge | 84 | 85, 97, 147 | thermal allocation |  |
| TH-28 Evaporative cooling pad | 73 | 74, 88, 145 | thermal allocation |  |
| TH-29 Cold pack | 66 | 67, 79, 139 | thermal allocation |  |
| TH-30 Thermoelectric generator | 87 | 88, 131, 145 | thermal allocation |  |
| TH-31 Flint striker | 58 | 59, 85, 99 | thermal allocation |  |
| TH-32 Tinder pad | 58 | 59, 85, 149 | thermal allocation |  |
| TH-33 Spring-mounted match | 85 | 86, 99, 149 | thermal allocation |  |
| TH-34 Timed toaster ejector | 91 | 92, 99, 146 | thermal allocation |  |
| TH-35 Coffee-pot steam vessel | 86 | 87, 100, 140 | thermal allocation |  |
| TH-36 Lamp-trigger apparatus | 86 | 87, 100, 142 | thermal allocation |  |
| TH-37 Heat-sensitive target | 15 | 16, 59, 90, 145 | thermal allocation |  |
