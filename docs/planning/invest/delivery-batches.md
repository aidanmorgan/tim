# Delivery batches — family slices, closures, decisions and structural work

Planning document, written 21 November 2026 against published main `4f193bb2` (44 element packages, Windmill 12.3 included). It records work the next coordinator would otherwise rediscover: which identities can share a slice, which are closed by another part, which family-level questions are already decided, which planned levels can be proven now, and which structural stories make an element slice cheaper.

It adds no delivery rule. The rules it relies on are **Family slice**, **Duplicate closure** and **Early level proof**, stated in [AGENTS.md, Delivery slices](../../../AGENTS.md#delivery-slices-and-independent-review) and defined in the [loop protocol, Slice shapes](../../../_bmad/custom/bmad-loop-protocol.md#slice-shapes) (owner, 21 November 2026), together with that protocol's [capability probe](../../../_bmad/custom/bmad-loop-protocol.md#capability-probe) and the [named-identity ledger](named-elements.md). On any conflict those documents win. Section 3 records decisions made under the owner's standing delegation; they are decisions, not rules. Thermal and radiation identities stay in the first release (owner, 21 November 2026).

**How to read the confidence marks.** *Confirmed* means the reused capability was read in this checkout and is cited as `path:line`. *Needs capability probe* means the identity looks like declaration data from its requirement row and spec, but nobody has checked the code; the slice's planning scout decides. The element specs under `docs/planning/elements/` were written against harvest baseline `a6c914e`, so their "Missing" and "Status" rows are often stale: never classify from them alone.

**Family or run.** A family is variants of one part: presets, materials, colours or sizes. Only a family may share a slice. A *run* is a list of unrelated parts that reuse the same capability; each is its own slice, and they are listed together only because doing them consecutively lets one planner reuse the same probe.

## 1. Family slices and runs

### 1a. Confirmed from code (3 identities, one family slice)

| Family | Identities | Reuses | Disqualified if |
|---|---|---|---|
| Ball presets | [EL-077 Soccer ball](../elements/EL-077-soccer-ball.md), [EL-091 Cannonball](../elements/EL-091-cannonball.md), [EL-239 Baseball](../elements/EL-239-baseball.md) ("an additional ball type"; native verification in progress) | The whole Tennis ball declaration shape: generic interfaces only (`elements/TennisBall/TennisBallElementDeclaration.cs:9`), five typed parameters (`:16`–`:20`), radius-bound collider (`:33`–`:38`), bounce-bound material (`:39`–`:40`), declared mass frame (`:48`–`:53`) and ambient loads (`:55`–`:57`) | The Cannon's chamber does not admit the cannonball's radius |

Tennis (12.1a) used exactly this shape and still touched 58 files, almost all registration tables. The saving from batching is one registration pass and one review for three packages; it grows once [registration derivation](#5-structural-stories) lands.

### 1b. Candidates today — needs capability probe (42 identities)

**Families (8 identities, 4 family slices if the probe passes).**

| Family | Identities | Likely reuse (read, not proven sufficient) | What the probe must settle |
|---|---|---|---|
| Ball presets, continued | EL-098 Pool ball, EL-076 Programmable ball | The ball shape in 1a | EL-098's row requires planar-table constraints: whether table contact satisfies it or a planar guide (`IPlanarGuideElement`, `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:30`) is needed; ball-to-ball friction different from ball-to-cloth needs per-pair materials, which do not exist; variant B is an owner question (section 3). EL-076 needs validated preset combinations and their refusal. Either one joins the 1a slice only if its probe passes before that slice is planned |
| Barrier materials | EL-073 Brick barrier, EL-074 Wood barrier | Static resizable box (`elements/Wall/WallElementDeclaration.cs:8`, `:26`–`:30`) | "Strength" (EL-073) and "combustible" (EL-074) have no engine family yet. If the row requires them now, these are not data-only; if only contact behaviour is required now, the rest is a named later-partner criterion and the identity stays open until it runs |
| Rope materials | EL-205 Rope, EL-067 Steel cable | Tensile span capability from Epic 10 | Whether the named rope acceptance is already executed (then EL-205 is a closure candidate); a second connector material |
| Edge detectors | EL-181 Rising-edge detector, EL-182 Falling-edge detector | Signal nodes and pulse ports | **Not data-only today:** `SignalNodeKind` has no edge node (`CuriousContraptions.Abstractions/SignalContractValues.cs:13`). One small signal slice first, then this family |

**Runs (34 identities, each its own slice).**

| Run | Identities | Likely reuse | What each probe must settle |
|---|---|---|---|
| Static geometry | EL-058 Size grate; EL-162 Fixed ball diverter; EL-216 Capture cradle | Static boxes and multi-body scene geometry as in Wall, Funnel and Receiver | A player-set blade angle that moves one collider (EL-162) |
| Compliant surface | EL-215 Damped cushion | Declared tension and damping ratio (`elements/Trampoline/TrampolineElementDeclaration.cs:14`, `:27`–`:28`, `:89`–`:95`) | Bottoming-out at finite travel; whether the 0.8 damping-ratio ceiling dissipates rather than returns |
| Authored box | EL-080 Programmable box | Typed, bounded parameters | Validated preset combinations; larger cargo against existing containers |
| Metal ball pipes | EL-071 Straight metal ball pipe; EL-072 Curved metal ball pipe | Pipe and both bends, under decision D12 | Opaque shell art against the existing collider; the oversized-cargo control in D12 |
| Rotary | EL-110 Flywheel; EL-200 Drive belt; EL-201 Drive chain; EL-232 Gears | Mechanical transmission and link presentation used by Motor, Conveyor, Reverse transmission and Clutch | A free inertia with no drive; belt and chain are connections, not parts; a chain of more than two meshed gears |
| Tensile | EL-065 Load hook; EL-207 Moving pulley; EL-069 Moving bucket; EL-070 Moving cage | Tensile span, pulley and anchor capabilities | A dynamic pulley; a rope-carried dynamic container |
| Two-body parts | EL-096 Tipsy platform; EL-116 Passive wheel | Multi-collider dynamic bodies; hinge (`IHingeElement`) | A static ridge and dynamic platform in one placed part; a rolling cylinder collider |
| Hinged cups | EL-223 Passive impact scoop; EL-224 Counterweighted scoop catapult | Hinge with stops as in Impact lever; Weight | A hinged container that carries and releases cargo; a weight driving the hinge |
| Supplied sliders | EL-217 Reload shuttle with chamber interlock; EL-220 Docking lift; EL-221 Parallel gripper; EL-237 Boxing glove | Supplied prismatic drive and retention as in Linear pusher (`PrismaticDriveKind`, `CuriousContraptions.Abstractions/PrismaticConstraintValues.cs:32`) | Interlock and dock outputs; a holding brake; two opposed jaws with an object-held output; a cooldown before the next punch |
| Timed and stored drives | EL-234 Mouse motor; EL-235 Jack-in-the-box; EL-236 Egg timer | Motor, Impact switch, Hold timer, Delay box and Wound spring | Whether an impact-started timed run, a belt-wound store and a readable countdown are declarable from existing signal and energy-store capabilities |
| Selective passage | EL-238 Selective trap door; EL-241 Ball hopper with one-ball escapement | Hinge with stops; Powered gate; Ball detector | What selects (size, mass or speed); a visible queue that releases exactly one ball per trigger |
| Drawn and guided carriages | EL-225 Latched slingshot cradle; EL-228 Zipline signal carriage | Finite elastic energy store and tensile rope (the ledger marks both present for EL-225); prismatic guide | A latch release (EL-225); a body guided along a tensile cable (EL-228) |
| Shaft-paid supply | EL-233 Generator | Electrical supply and shaft coordinates, which exist separately | Whether a supply whose output is paid by shaft work is declarable. If not, `ENGINE-ASSEMBLY-JOINTS` owns shaft-to-electrical conversion in its Missing list |
| Already-built behaviour | EL-197 Electrical wire; EL-209 Fan | The electrical connection Battery and Motor already use; Fan 12.2 | Whether the named acceptance is already executed. If the identity has no behaviour of its own it is a duplicate closure; otherwise a small slice |
| Counter | EL-183 Resettable counter | Pulse-count node | **Not data-only today:** `Reset` is a pulse terminal only on a memory node (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:19`) |

Correction to the 21 November review: it estimated 60–65 identities as data-only today. Code confirms 3. The 42 above are candidates, and only 8 of them could share a slice. Most variant savings arrive only after each domain's engine slice (1c).

### 1c. Open after an engine slice (55 identities, not available today)

Listed so the engine slice's planner can design for the whole family at once. Membership is from requirement rows and spec identity tables; each needs its own probe once the capability exists.

The engine slices are the engine-first stories in `_bmad-output/planning-artifacts/epics.md` ("Engine-first capability stories (21 November 2026)"): Optics is `ENGINE-OPTICAL`, Liquid `ENGINE-LIQUID`, Joints `ENGINE-ASSEMBLY-JOINTS`, fracture `ENGINE-FRACTURE`, Field force `ENGINE-FIELD-FORCE`, Gas network `ENGINE-GAS-NETWORK`, Goals `ENGINE-GOALS` and Radiation `ENGINE-RADIATION`.

**Families (21 parts, 6 family slices).**

| After this engine slice | Family | Identities |
|---|---|---|
| Optics | Colour filters | CAT-055, CAT-031, CAT-011 |
| Optics | Colour receivers | CAT-056, CAT-032, CAT-012, CAT-072, CAT-021, CAT-040, CAT-068, CAT-038 (broadband) |
| Optics | Laser colours | EL-176, EL-177, EL-178 (after CAT-036) |
| Liquid | Water elbows | EL-009 (45°), EL-010 (90°) |
| Radiation | Shield materials | EL-126, EL-127, EL-128 |
| Radiation | Neutron detectors | EL-131, EL-132 |

**Runs (34 parts, each its own slice).**

| After this engine slice | Run | Identities |
|---|---|---|
| Optics | Optical gates | CAT-043 to CAT-047 |
| Liquid | Water pipe kit | EL-008, EL-011, EL-012, EL-013 |
| Liquid | Taps | EL-165, EL-166, EL-167 (after EL-003) |
| Liquid | Meters | EL-030, EL-031, EL-032 |
| Liquid | Hydraulic piston | EL-229 |
| Liquid | Waterwheel | EL-240 |
| Joints and fracture | Limited connectors | EL-159, EL-160, EL-161 |
| Joints | Cams | EL-156, EL-157, EL-158 |
| Joints | Linkages | EL-218 Crank-slider; EL-219 Rack-and-pinion slide |
| Joints (`ENGINE-ASSEMBLY-JOINTS`, whose Missing list holds the revolute items) | Struck and sprung arms | EL-222 Supplied pinball flipper; EL-226 Hinged elastic catapult. The revolute drive is velocity-only (`RevoluteDriveKind { None, Supplied }`, `CuriousContraptions.Abstractions/RevoluteConstraintValues.cs:22`) and only prismatic retention exists |
| Field force | Magnetic relay | EL-227 Magnetic impulse relay |
| Gas network | Pneumatic pistons | EL-230 Spring-return; EL-231 Double-acting |
| Goals | Goal kinds (level data, not parts) | EL-120 to EL-123 |

### Identities ledgered after EL-241 — NOT YET CLASSIFIED

EL-217 to EL-241 (25 campaign-taught parts ledgered on 21 November 2026) are classified above from their one-line ledger rows only; Baseball now has an [element spec](../elements/EL-239-baseball.md), while the other 24 remain probe candidates without specs. If the ledger gains identities after EL-241, none of them is classified here: classify each into 1a, 1b or 1c before planning from this document, and update the totals at the end.

## 2. Duplicate closures

Forty-four named identities say in their own spec that they refine one catalogue part (found from the `Refines`, `Refines CAT`, `Refines / extends`, `Related` and `CAT spec refined` rows of every `EL-`, `TH-`, `RAD-` and `GAP-` spec). Under the Duplicate closure rule an identity with any behaviour, parameter or mode of its own is a variant or an element, not a duplicate. On that test 27 are candidate duplicates and 17 are not closures until checked.

**Every row in 2a and 2b is a candidate until its closure check passes with executed tests.** The first review of this list found that 3 of 14 sampled rows had behaviour of their own, so the classification method is recorded: each candidate's spec was scanned for added parameters, ports, modes, materials, supply and stated deltas, and its declared identifiers were compared with the satisfying part's spec. That is a screen, not a full reading; the planner of each closure reads the whole spec.

A closed duplicate gets no package and no catalogue or palette entry. **Closure check:** executed proof of the identity's ledger entry (exact source outcome, negative/control, Reset and save criteria) through the satisfying part, with the ledger row citing the part and the test. A row with no executed test is not closed.

### 2a. Candidate duplicates of delivered parts (9) — closure-review slices

Each group is one ordinary closure-review slice (one record, one candidate commit, one approval) whose owned diff is tests and ledger rows.

| Group | Identity → satisfying part |
|---|---|
| Electrical gates | EL-133 → CAT-013 Both gate; EL-134 → CAT-026; EL-135 → CAT-027; EL-136 → CAT-025; EL-137 → CAT-024 |
| Drive train | EL-202 Conveyor → CAT-019 ("this EL adds no new value") |
| Rope parts | EL-204 Weight → CAT-067; EL-206 Fixed pulley → CAT-053 ("this EL adds no new value") |
| Cargo | EL-189 Tennis ball → CAT-064 |

### 2b. Candidate duplicates of undelivered parts (18) — closed inside that part's slice

Each spec states "One catalogue part satisfies both identities; not a second implementation." The closure is acceptance rows and named tests inside the part's slice; it has no record of its own.

| Identity | Closed inside the slice for |
|---|---|
| EL-138 to EL-142 Optical gates | CAT-043 AND, CAT-046 OR, CAT-047 XOR, CAT-045 NOR, CAT-044 NAND (in that order) |
| EL-143, EL-144, EL-145 Colour filters | CAT-055 red, CAT-031 green, CAT-011 blue |
| EL-146 to EL-152 Selective receivers | CAT-056 red, CAT-032 green, CAT-012 blue, CAT-072 yellow, CAT-021 cyan, CAT-040 magenta, CAT-068 white |
| EL-212 Flat mirror | CAT-041 |
| EL-213 Optical combiner | CAT-006 Beam combiner |
| EL-214 Broadband receiver | CAT-038 Light receiver |

### 2c. Refining identities with behaviour or a residual of their own (17) — not closures until checked

Each needs a planning check against current code. If the residual is already delivered and tested, the identity becomes a candidate duplicate and joins a 2a group (or, for an undelivered part, that part's slice). If not, it is a variant or a small slice of its own, and the ledger row stays open.

| Identity | Refines | Behaviour or residual its spec names |
|---|---|---|
| EL-187 Basketball | CAT-001 Ball | Buoyancy and pressure modes; the retained CAT-001 R-N3 item |
| EL-188 Bowling ball | CAT-014 | Lever loading; pressure modes; use as a named goal body |
| EL-191 Wall | CAT-066 | Optical and acoustic occlusion by the same box (waits for those engines) |
| EL-192 Receiving basket | CAT-004 Receiver | Residence and guide in a moving frame; a second Receiver while balls exist |
| EL-193 Domino | CAT-023 | Offset centre of mass |
| EL-194 Springboard | CAT-062 | Preload and `spring_forward` |
| EL-195 Pinball bumper | CAT-015 | A supplied variant with its own `PowerIn` |
| EL-196 Battery | CAT-005 | Network acceptance with the Motor |
| EL-198 Switch | CAT-063 Impact switch | A separately supplied electrical contact |
| EL-208 Balloon | CAT-003 | Its spec adds three behaviours: buoyant rise, tether, puncture with gas loss |
| EL-180 Pulse speaker | CAT-061 Speaker (undelivered) | Stated refinement with a sibling mode (EL-179); check for a mode of its own when the Speaker is planned |
| EL-211 Solar panel | CAT-059 (undelivered) | Supplies loads through wires like a battery and needs network acceptance with a first consumer (the Motor), beyond the panel's own light sampling |
| EL-064 Metal loop anchor | CAT-058 Rope anchor | Steel-cable admission is its delta over the anchor and needs a connector-material enum; depends on EL-067 |
| EL-190 Ramp | CAT-054 | Supported-dimension controls and remaining fixtures are open |
| EL-199 Electric motor | CAT-042 | Declared losses and an `efficiency` parameter; Motor electrical drain is deferred |
| EL-203 Reverse transmission | CAT-057 | Open question whether the row's "gear/belt arrangement" needs a separate crossed-belt reverser |
| EL-210 Flashlight | CAT-029 (undelivered) | A supplied source with two activation modes |

### 2d. Related but never closures

- **EL-071, EL-072** are classified in 1b as variants with their own catalogue entries (D12). They are mentioned here only because CAT-048 to CAT-050 name them as mapped identities.
- **TH-05 Friction brake** extends [EL-055 Mechanical brake](../elements/EL-055-mechanical-brake.md) ("one brake identity with an added thermal proof"). EL-055's slice delivers the brake; TH-05 is the later thermal criterion on the same package.

## 3. Family-level decisions

The element specs hold 1,141 list items under their "Open questions" headings; 689 mention an owner decision (counted 21 November 2026 by scanning every spec for list items under a heading containing "open questions" or "open decisions"). Most repeat a handful of family questions. Under the owner's standing delegation (decide by game intent, a playable physics puzzle; ask only when cutting scope or waiving a gate) the following are decided. A slice that finds a decision causes a gameplay defect records the defect and decides again at planning.

The line counts below were measured by keyword on an earlier index of 1,005 of those items, so they are lower bounds and they overlap; on that index the patterns together match 657 items. The rest are single-element questions, covered by D19.

| # | Decision | Reason | Lines it resolves |
|---|---|---|---|
| D1 | Every value a spec marks **proposed** is the starting value. No owner confirmation per value. | Outcomes, not numbers, are the acceptance target (28 October policy) | 307 items containing "proposed", from `EL-003-tap.md:85` to `GAP-09-fragmentation-station.md:96` |
| D2 | Liquid density is 16 kg/m³ for the whole liquid family. | The Basketball floats and the Bowling ball sinks, which is the lesson the buoyancy row asks for | 46 items, `EL-001-finite-reservoir.md:84` onward |
| D3 | Liquid mouths join the way clear ball-pipe mouths join today; no second join mechanism. | One current path (forward-only rule) | `EL-001-finite-reservoir.md:86` and the family rows that defer to it |
| D4 | One liquid inventory representation, owned by the liquid engine slice. Meltwater, condensate and moderator water exchange with it. | A second liquid model would be a second rule for the same behaviour | `TH-14-ice-block.md:74`, `EL-001-finite-reservoir.md:87`, `RAD-17-water-moderator-tank.md:81` |
| D5 | The thermal "Batch N game scale" is adopted as written, with its declared saturation curve for boiling. | One consistent scale keeps heating times within a puzzle's length | 37 items, one per `TH-` spec |
| D6 | Combustion reads oxygen from a finite gas inventory, not a boolean flag. | The requirement row (TX-14) asks for it and the gas store capability exists | 4 items: `TH-01-candle.md:76`, `TH-03-combustible-block.md:79` and two more |
| D7 | Tick order follows [the numeric authority](../../gpu-f32-physics.md#solver-model): electrical in Phase 1, optics and acoustics in Phase 2. All receiver readings of a tick commit together and are read by the electrical solve of the **next** tick. | See the note under this table | 22 items headed "Tick order", `EL-138-optical-and-gate.md:108` to `CAT-072-yellow_receiver.md:89` |
| D8 | Each identity that is **not** a duplicate (section 2) gets its own catalogue entry. A mode inside one identity is a parameter of that entry (EL-044's band is one entry with a `band` setting). A closed duplicate gets none. | The owner requires every named element; players should not see two entries for one part | 21 items, `EL-003-tap.md:83` to `EL-207-moving-pulley.md:105` |
| D9 | Art, icon and colour follow [DESIGN.md](../../../DESIGN.md) and the spec's proposal. No per-part confirmation. | The approved palette already governs | 20 items |
| D10 | Authored durations are seconds of game time, never ticks. | Cadence is selectable; a tick count would change behaviour with it | `CAT-009-bell.md:116`, `CAT-061-speaker.md:105`, `CAT-069-wind_chimes.md:121` |
| D11 | A declared parameter is player-editable in the Workbench unless the level locks it. | Matches every delivered part | 7 items, `EL-058-size-grate.md:116` to `RAD-09-integrating-dosimeter.md:83` |
| D12 | Metal ball pipes keep the 1.3 m bore and join clear pipes. They differ in material and in hiding the cargo. The required outcome "oversized cargo jams rather than traversing" is kept and proven with a ball whose `radius` parameter is set above the 0.65 m bore radius (the Tennis ball admits up to 16 m, `elements/TennisBall/TennisBallElementDeclaration.cs:18`). | A 0.62 m gauge would not pass the Basketball (0.68 m across). The spec's objection that this option "needs a new larger body" predates parameterised ball radius | `EL-071-straight-metal-ball-pipe.md:114`, `EL-072-curved-metal-ball-pipe.md:111` |
| D13 | Capacity and size are fixed per kind unless the requirement row names a size parameter. | Fewer controls per part; resizing is the Wall's lesson | 28 items, `EL-001-finite-reservoir.md:85` onward |
| D14 | Capability-family map rows are navigation. The slice's capability probe is the authority; a missing family is added in the Code Map without owner sign-off. | The map predates the current engine | 27 items, `EL-027-canal-lock-chamber.md:93` to `TH-37-heat-sensitive-target.md:77` |
| D15 | Where legacy behaviour or a research table conflicts with the requirement row, the row governs. | Archives record past results, not requirements | The conflict questions among 110 items mentioning legacy or historical behaviour, and `TH-36-lamp-trigger-apparatus.md:86` |
| D16 | A question addressed to a source owner ("owner S418") is decided by the delivering slice at planning and recorded in its Code Map. | The S owner is the slice; no separate decision stage exists | 304 items naming an `S` owner |
| D17 | A continuous sound source is one sustained emission, not back-to-back pulses. | Pulses would retrigger pulse listeners | `EL-179-continuous-tone-speaker.md:79` |
| D18 | Amber stands in for white light in filter and receiver tests until a white source exists; the amber laser failing a purity test is the intended lesson. | The amber laser carries all three channels | `EL-143-red-optical-filter.md:85`, `CAT-055-red_filter.md:89`, `EL-149-yellow-selective-receiver.md:78` |
| D19 | Default for every single-element question not listed: take the spec's proposal; add an optional extra socket, output or mode only when a requirement row or a campaign lesson uses it. | Keeps each part to what a puzzle needs | The remaining items |

**Note on D7.** The requirement says receiver readings "commit … together before the electrical solve" that reads them (`docs/planning/requirements.md:686`, `:6617`), and `CAT-072-yellow_receiver.md:89` flags the order for the owner. This document reads that as: together, and before the solve that consumes them, which is the next tick's. Both parts of the requirement hold: no solve ever sees a half-updated set of readings, and no solve reads a value that has not been committed. The cost is one tick (1/120 s) between light arriving and a supplied part responding, which a player cannot see and which no lesson times. The alternative, running optics before the electrical phase, would also have to let an electrically switched laser light in the same tick, which is the two-way flow the authority forbids. If the optics engine slice finds a lesson that needs same-tick response, that is a gameplay defect and it goes to the owner.

### Needs the owner

| Question | Why it is not delegated |
|---|---|
| RAD-01 (`RAD-01-gamma-source-capsule.md:80`): may a radiation source keep a fixed strength for a whole Run, when the requirement says a spent inventory cannot emit indefinitely? | Accepting it narrows a stated requirement |
| EL-098 variant B (`EL-098-pool-ball.md:84`): adopt the no-gravity pool table? | The ledger says it needs "a separate explicitly fictional field rule"; adopting or dropping it is scope |
| EL-050 (`EL-050-acoustic-screen.md:80`): is reflection required of the acoustic screen? | Dropping it removes a behaviour; decide with the acoustics engine slice |

## 4. Early level proof

`content/puzzles.json` holds 63 levels: 15 `Playable` and 48 `Planned`.

- **45** planned levels use only delivered parts (part kinds in `inventory`, `parts` and `solution` against the 44 delivered package ids). The other three (21 `solar_motor`, 22 `solar_shadow`, 24 `delayed_solar`) need the Flashlight and Solar panel.
- **12 of the 45 can be proven now.** The engine evaluates one goal per level, of kind `None`, `Captured`, `ActivatedAfter` or `IntegrityBroken` (`engine/gpu/WorkshopPuzzle.cs:9`, `:75`). Twelve planned levels have a single `captured` or `activated` goal.
- **33 wait for the goal-evaluation engine story** (`ENGINE-GOALS`; the goals domain, EL-120 to EL-123): 32 have two to five goals (14 with two, 13 with three, 4 with four, 1 with five), and level 14 `battery_motor` has a single `turned` goal, a kind the engine does not have. `powered_after` also appears (level 49 among these 45).

**Every level must first be authored into the playable shape.** Planned entries use a different content shape (`goals` array, `parts`) that the engine indexes but never constructs (`engine/gpu/PuzzleCatalogue.cs:10`; `engine/gpu/PuzzleDefinition.cs:100`–`:101` refuses any entry that is not `Playable`). A playable entry has `goal`, `placements`, `profiles`, `presentation` and `connections_allowed`. So each level is an authoring task plus a proof, not a test over existing data.
**Proven natively** means what the existing level tests already do (`CuriousContraptions.tests/DominoEffectTests.cs` is the pattern), under [testing guidance](../../testing-guidance.md):

1. The level loads through `PuzzleCatalogue` with its authored fixtures, inventory and goal.
2. The intended solution, placed through the production edit interface, runs in the real C# simulation and reaches the goal within a generous stall guard.
3. A control fails: the unsolved start, or the solution with one required part removed, does not reach the goal.
4. Reset restores the authored state, and save/load of the solved construction round-trips.

No arrival tick, trajectory or hash is asserted. Browser proof stays pending. A level changes to `Playable` only in the slice that proves it.

**Provable now (12), in campaign order.** Group levels that share parts into one slice.

| Group | Levels (number, id, goal) |
|---|---|
| Signals | 02 `power_trip` (activated), 07 `spring_signal` (activated), 08 `wind_signal` (activated) |
| Air and capture | 03 `air_mail`, 05 `third_dimension`, 12 `wall_return`, 61 `a_gentle_rebound` (all captured) |
| Drive | 16 `conveyor_courier`, 17 `belt_relay`, 18 `reverse_belt` (all captured) |
| Ropes | 19 `counterweight`, 20 `pulley_depth` (both activated) |

**After the goal-evaluation story (33).** 09, 14, 15, 29 to 58. Level 14 needs the `turned` kind; 15 and 49 also use `turned` or `powered_after`.

These levels were authored against the legacy engine. Expect some solutions to need re-tuning on the current solver; that is the useful finding, and it is cheaper now than at the browser phase.

## 5. Structural stories

Each removes hand edits from every later element slice. None changes gameplay. Each is its own slice through the normal loop.

### S1. Derive new-package registration from declarations — in flight

Started 21 November 2026 in another session (`spec-registration-derivation.md`, baseline `4f193bb2`). Its owned scope covers the registry, bundle, capacity, catalogue, palette, coverage-membership and impact tables. It does **not** cover the lists in S2 and S3 below or the capability requirement tables under `tools/Coverage/`.

### S2. Test project compile list becomes a glob

- **Outcome.** A new test file compiles without editing `CuriousContraptions.tests/CuriousContraptions.tests.csproj`.
- **Now.** `EnableDefaultCompileItems` is false (`CuriousContraptions.tests/CuriousContraptions.tests.csproj:7`) and every test file is listed by hand: 236 local files plus 2 linked `RuntimeProviders.cs` files. The local list equals the directory's 236 `.cs` files, none missing and none extra (checked 21 November 2026). The file changed in 20 commits since 5 November.
- **Acceptance.** The project compiles exactly the same test sources before and after (compare the compiler logs' source lists). The two linked files stay explicit. Any static-gate test that pins the list is rewritten to assert the rule instead. The full round's test total is unchanged, with zero failed and zero skipped.
- **Removes from an element slice.** One file, and one class of first-round failure (a test file that never ran).

### S3. Remaining per-package lists

- **Outcome.** Adding a package edits no shared list.
- **Now.** Not owned by S1: the project list in `CuriousContraptions.tests/CSharpStaticGateTests.cs:703` (trim analysis), the placement switches in `CuriousContraptions.tests/PackageRuntimeParityTests.cs:596` and `:653`, and `CuriousContraptions.slnx`.
- **Acceptance.** Each list is derived from the discovered packages (`TestBundle.Packages`, `CuriousContraptions.tests/BundlePortsTests.cs:19`) or from the `elements/` directory, with one control proving an unregistered package is detected. A package that needs special placement declares it; the test does not switch on its id.
- **Removes.** Three files per slice.

### S4. One catalogue conformance suite

- **Outcome.** Every package gets the same baseline proof without a hand-written test per package.
- **Design.** One theory over `TestBundle.Packages`, each row run through the production interfaces:
  1. save and load of a placed instance round-trips its typed parameters and connections;
  2. every declared parameter refuses a value below its minimum, above its maximum and non-finite, atomically and with a typed reason;
  3. Run then Reset restores the authored state of every body and store the package declares;
  4. every visible solid in the package art has a collider, and no collider lacks art;
  5. every declared port refuses a connection of the wrong kind;
  6. the catalogue entry's metadata (id, title, category, icon, scene path) resolves;
  7. the package compiles into the physics graph with only generic capability families.
- **Acceptance.** The suite passes for all delivered packages. Per-package tests that only repeat a row are deleted, and the record names each deleted test and the row that covers it. A deliberately broken fixture package fails each row.
- **Removes.** Three to six hand-written tests per element slice, and the largest review finding class (missing or weak baseline tests).

### S5. Generate the wire accessors in the build

- **Now.** `engine/gpu/WorkshopWireGenerated.cs` (6,872 lines) and `engine/gpu/WorkshopAuthoringGenerated.cs` (407 lines) are committed FlatBuffers output for `schemas/workshop_wire.fbs` and `schemas/workshop_authoring.fbs`. The wire file changed in 14 commits since 5 November (+2,428 / −752 lines), and every one of those lines went through review. Windmill added `tools/WorkshopWireGeneration`, which post-processes compiler output into the one supported accessor surface and is covered by `CuriousContraptions.tests/WorkshopWireGenerationTests.cs`. It is referenced only by the test project; no build step calls it.
- **Outcome.** A schema change is reviewed as a schema change. Generated C# is build output.
- **Acceptance.** The normal build runs the pinned FlatBuffers compiler and `WorkshopWireGeneration` before compiling consumers; the generated files leave the repository; CI builds from a clean checkout; a schema edit with stale output cannot compile. The first step is deciding how the pinned compiler is provisioned on a developer machine and in CI. If it cannot be provisioned without a new prerequisite, the lesser acceptance is: keep the files committed, add a test that regenerates and compares them semantically, and exclude them from blind reviewers' diffs.
- **Removes.** 500 to 1,500 generated lines from each capability slice's diff and reviews.

### S6. Table-driven capability plumbing

- **Now.** Adding a capability extends a branch chain in several places, and those methods sit at the complexity limit, so the maintainability gate fails on the next addition. Recent slices recorded `WorkshopDeclaredPresentation.Query` (`engine/gpu/WorkshopDeclaredPresentation.cs:373`) at 28, `BundleValidator.PresentationBindingCapabilities` (`CuriousContraptions.Bundle/BundleValidator.cs:258`) at 27, and three methods at 26 including `BundleSchema.Entry` (`CuriousContraptions.Bundle/BundleSchema.cs:18`); each was repaired by extraction inside the slice that tripped it.
- **First step.** Measure. The gate prints only violations, so run the metrics once with the threshold lowered in a scratch copy and list every production member at 20 or above. The three methods named here are from slice records, not a current measurement.
- **Outcome.** Each per-capability chain becomes a table keyed by the closed capability enum, with one entry per capability and a test that every enum value has an entry.
- **Acceptance.** No listed member is above 15 afterwards; behaviour is unchanged (the full round passes with the same totals); adding a capability in a follow-up fixture touches one table row per chain.
- **Removes.** The maintainability repair that recent engine slices each paid, and its re-review.

## Counts and how they reconcile

- **Ledger at the time of writing:** 301 named identities with specs or ledger rows: 241 EL (EL-001 to EL-241, of which EL-217 to EL-241 were ledgered on 21 November 2026 and have ledger rows but no spec), 37 TH, 19 RAD and 4 GAP, plus 72 CAT specs. Identities ledgered after EL-241 are not counted or classified here.
- **Delivered:** 44 packages under `elements/`, each matching one CAT spec; 28 CAT parts remain.
- **Section 1:** 1a 3; 1b 42 (8 in families, 34 in runs); 1c 55 (16 CAT and 39 EL; 21 in families, 34 in runs).
- **Section 2:** 44 refining identities, all EL: 27 candidate duplicates (9 of delivered parts, 18 of undelivered parts) and 17 not closures until checked.
- No identity is classified in more than one of 1a, 1b, 1c, 2a, 2b and 2c. Section 2d only explains why EL-071, EL-072 (in 1b) and TH-05 are not closures. All 25 of EL-217 to EL-241 are classified: one in 1a, fifteen in 1b, nine in 1c.
- **Not classified here:** the remaining EL, TH, RAD and GAP identities each need their domain's engine slice and are single elements, not variants; the [roadmap](vertical-delivery.md) orders them.
- **Section 4:** 63 levels = 15 playable + 12 provable now + 33 after goal evaluation + 3 blocked on parts.
