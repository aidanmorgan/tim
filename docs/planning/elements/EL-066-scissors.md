# EL-066 · Scissors declaration readiness spec

**Current delivery (24 October 2026):** [EL-066 delivery slice](../invest/named-elements.md#element-066). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against published main `8d1d68aa`; their `path:line` citations are to that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-066 |
| Name | Scissors |
| Type | Mechanical |
| Requirement anchor | [element-066](../requirements.md#element-066); scope index [todo-193](../requirements.md#todo-193) |
| Named entry | [named-elements.md#element-066](../invest/named-elements.md#element-066); proof owner S335 |
| CAT spec refined or extended | none. Related: [CAT-058 rope anchor](CAT-058-rope_anchor.md) (rope route and topology), [CAT-067 weight](CAT-067-weight.md) (actuating and released loads), [CAT-034 impact lever](CAT-034-impact_lever.md) (hinged arm pattern) |
| Related identities | [EL-068 tin snips](EL-068-tin-snips.md) (powered cutter; same criterion), [EL-067 steel cable](EL-067-steel-cable.md) (resistant material), EL-205 rope (Batch J) |
| Roadmap story | [EL-066 delivery slice](../invest/named-elements.md#element-066); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (candidate spans for the blade pair) and ENGINE-ISLANDS-SLEEP; then ENGINE-CONNECTION-MATERIALS (the declared rope material with d and R, and the solid strand of owner decision 1) and ENGINE-TENSILE-EXTENSIONS (cutting clause: the one cutting law of section 2, coordinator decision 16); then S7. Story texts are in the batch summary |
| Disposition | To deliver as a single element. Not a family with the Tin snips: the two differ in ports, drive and supply, not only in declared values |
| Planned package | `scissors`, directory `elements/Scissors/`, class `ScissorsElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on the coverage follow-on story S7. No CAT number |
| delivery-batches class | Not listed in 1a, 1b or 1c of [delivery-batches](../invest/delivery-batches.md). Its map row names StructuralFracture and TopologyTransaction (`docs/planning/general-engine-element-map.md:171`), which `ENGINE-FRACTURE` unblocks (`_bmad-output/planning-artifacts/epics.md@25df3cd5:4472`). Code confirms a cut exists only as a player command. It belongs in 1c under "Joints and fracture" as a run "Cutters" with EL-068; the cut itself is the cutting clause of ENGINE-TENSILE-EXTENSIONS, not ENGINE-FRACTURE (section 3) |

## 2. Declaration

Follows the delivered **Impact lever** shape: a static base carrying one balanced bar on a limited hinge with a Tie port on the bar (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:12`, `:41`–`:62`, `:91`–`:119`).

**Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the cutter declaration the engine story adds (section 3). `KinematicMode.Static` root; domains Mechanical and Rope.

**Bodies and colliders** (pivot at the origin, hinge axis +Z, blades in the XY plane).

| Body | Collider | Box full size (m) | Centre (m) |
| --- | --- | --- | --- |
| 0 static | post | 0.20 × 0.60 × 0.10 | (0, −0.22, −0.08), behind the blades |
| 0 static | fixed upper blade | 0.45 × 0.04 × 0.06 | (0.265, 0.02, 0); its cutting edge is its underside, y = 0 |
| 1 bar (dynamic, hinged) | moving blade and handle, one straight bar | 0.98 × 0.04 × 0.06 | (0, −0.02, 0) in the bar's frame; blade on +X, handle on −X, cutting edge its top, y = 0 |

**Mass and inertia.** Base static. Bar 0.3 kg, centre of mass at its box centre (balanced about the pivot), principal moments of a uniform box m·(b² + c²)/12, m·(a² + c²)/12, m·(a² + b²)/12 with a, b, c = 0.98, 0.04, 0.06 m, gravity (0, −9.81, 0).

**Material** (restitution, friction, rolling resistance, bounce threshold): post and fixed blade `(0, 0.3, 0, 0.1)`; bar `(0, 0.3, 0, 0.1)` (the Impact lever beam's values, `ImpactLeverElementDeclaration.cs:70`).

**Hinge.** One `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): support body 0; base frame at the pivot, turned −35° about Z so that angle 0 is the open pose (blade drooped 35° below the fixed blade, handle end raised 0.27 m); `RevoluteTravel.Limited`, lower limit 0, upper limit 0.611 rad (closed: the two cutting edges meet); stiffness fixed 1 N·m/rad (return to open); damping fixed 0.05 N·m·s/rad; friction torque fixed 0.02 N·m; initial angle 0; `BodiesCollide: false`; no drive. Hinge spring values must be constants: a parameter is refused (`CuriousContraptions.tests/WoundSpringContractTests.cs:190`–`:191`).

**Parameters.** None. The cutting capacity is fixed per kind (D13).

**Port.**

| `PortId` | Role | Domain | Direction | Signal | Body | Position (m) | Attachment |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `Tie` | `PortRole.Tie` | Rope | Bidirectional | none | 1 | (−0.47, −0.04, 0), under the handle end, normal −Y | `RopeAttachmentRole.Load` |

The span to be cut is not connected to the scissors. It only passes between the blades.

**Cutter (declared to the engine capability of section 3).** Fixed edge: on body 0 from (0.10, 0, 0) to (0.49, 0, 0). Moving edge: on body 1 from (0.10, 0, 0) to (0.49, 0, 0), closing on revolute coordinate 0 toward its upper limit. Blade thickness in Z: ±0.03 m. Cutting capacity C = 4 J.

**Stores.** None. **Presentation.** `Bodies: [("Visual/Bar", PackageBodyIndex(1))]`. Pick radius 0.6 m. Footprint: 2 bodies, 1 moving, 0 prismatic, 1 revolute.

**Palette.** `ElementCategory.Motion`, palette order 110 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), id `scissors`, display name "Scissors", catalogue colour pale grey (metal detail, `DESIGN.md:248`; proposed `(0.80, 0.85, 0.87)`, D9).

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Visual_Post` | navy box 0.20 × 0.60 × 0.10 | solid, post collider |
| `Visual_FixedBlade` | pale grey box 0.45 × 0.04 × 0.06 | solid, fixed-blade collider |
| `Visual/Bar/Blade`, `Visual/Bar/Handle` | one bar 0.98 × 0.04 × 0.06, pale grey on the blade half, ochre on the handle half | solid, bar collider |
| `Visual/Bar/Pivot` | gold disc r 0.04 on the bar's front face, flush | decorative: printed flat |

Icon: an open pair of blades with a short strand between them. At a glance it differs from the Tin snips by its long bare handle with a rope knot and no housing; the snips are a box with a short jaw and a lamp.

### Behaviour and numbers

**Cutting contract (stated once, here; [EL-067](EL-067-steel-cable.md), [EL-068](EL-068-tin-snips.md) and [EL-078](EL-078-can-opener.md) cite it).** It is the one cutting-work law of the game (coordinator decision 16, 22 November 2026), owned by the cutting clause of ENGINE-TENSILE-EXTENSIONS (owner decision 3a), written without naming any element. The engine cannot do it today (section 3).

- *Material data.* A connection material declares a strand diameter d (m) and a cutting resistance R (J).
- *Cutter data.* A cutter declares a blade pair: a fixed straight edge on one body and a straight edge on a body moving on one revolute coordinate, both passing through the hinge axis; a blade thickness along that axis; and a capacity C (J, constant or parameter-bound).
- *Engagement geometry.* A span is a candidate while its straight segment crosses the plane of the blades within the blade thickness, between the two edges and inside their declared lengths. It engages when the perpendicular gap between the edges at the crossing point falls to d. At a crossing distance x from the pivot that is a blade angle asin(d/x) short of closed.
- *Force law.* While engaged and closing, the span opposes the moving edge with force R/d at the crossing point, a torque R·x/d on the coordinate. It never pushes the blades apart. If R ≤ C the route is severed when the gap reaches 0, the mechanism having delivered exactly R. If R > C the gap is held at d whatever the input.
- *Ledger.* The absorbed work leaves the mechanism and is a committed read on the cutter, so a cut never creates energy ([IX-37](../requirements.md#interaction-37)). Work absorbed by a stroke that stops short is not returned.
- *Severing* is the delivered route removal: every tensile row of the route is removed at that committed step, the read shows `RopeIntegrity.Cut` and `RopeState.Open`, a route is cut once, and Reset restores it (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:388`; `CuriousContraptions.tests/RopeTests.cs:151`).
- *Targets* (adopted from the can opener, `readiness/batch-l.md` section 4 item 11). A cutting target is a tensile span or an attachment seam: a declared straight segment with thickness d and resistance R (constant or parameter-bound) on a body held by a fixed joint (ENGINE-JOINT-KINDS). The same engagement, force, capacity and severing rules apply; severing a seam releases its fixed joint one-way and Reset restores it. Both edges load the target equally, so the target receives no net force.
- *Solid strands.* A rope or cable strand is solid (owner decision 1, through ENGINE-CONNECTION-MATERIALS). The blade-edge colliders do not collide with a target's strand or seam piece; the cutting force stands in for that contact, so a strand between the blades is not merely pinched. Every other collider of the cutter, and every other body, meets the strand as a solid.
- *Candidates* for a blade pair come from a bounded query through ENGINE-SPATIAL-QUERIES, not a scan of every route each tick.

The rule reads d, R, C and geometry only; no material or cutter is named in engine code.

| | Rope, d 0.04 m, R = 1 J (25 N) | Steel cable, d 0.04 m, R = 25 J (625 N) |
| --- | --- | --- |
| Scissors, C = 4 J | cut when the delivered work reaches 1 J | never cut |
| Tin snips, C = `cut_work` 5–40 J, default 30 | always cut | cut when `cut_work` ≥ 25 J and the crossing is within 0.154 m of the pivot ([EL-068](EL-068-tin-snips.md)) |

R = 1 J and d = 0.04 m for rope are the starting values this batch needs from EL-205, whose disposition here is "declared rope material" (D1; the conflict with Batch J's closure is in the batch summary).

**Closing the scissors.** A pull down on the handle end, or anything landing on the handle, turns the bar and raises the blade. The handle end falls 0.27 m over the stroke, so a mass m hung from the `Tie` delivers m·g·0.27 = 2.65 J per kilogram, less 0.19 J stored in the return spring (½·1·0.611²) and 0.01 J of bearing friction. Acceptance routes cross the blades 0.15 m from the pivot. There the rope engages asin(0.04/0.15) = 0.270 rad short of closed and resists with 25 N × 0.15 m = 3.75 N·m. Of the handle's fall, 0.144 m comes before engagement and 0.126 m during it.

- 0.25 kg (the lightest Weight, `elements/Weight/WeightElementDeclaration.cs:33`): 0.29 J at engagement and 0.31 J more during it, less 0.13 J of spring: 0.47 J against 1 J, and a static torque of 0.63 N·m against 3.75 N·m. It closes empty scissors but stops about 0.09 rad into the band, 0.18 rad short of closed. This is the row's "insufficient cutting work leaves it intact".
- 1 kg: 1.35 J at engagement and 1.24 J more, less 0.13 J: 2.45 J against 1 J; its static torque near closure (about 4.0 N·m net of the spring) also exceeds 3.75 N·m. Cuts a rope.
- 8 kg: 21 J. Cuts a rope. A steel cable (R 25 J above C 4 J) holds the gap at d, 0.270 rad short of closed, whatever lands on the handle.
- A strand farther from the pivot is harder to cut: at 0.30 m the rope needs 7.5 N·m, more than a hung 1 kg supplies statically. Lessons route the rope near the pivot.

**Return.** With the pull gone the spring reopens the blades to within 0.02 rad of open (friction 0.02 N·m over 1 N·m/rad).

**Tick order.** The cut is evaluated in the physics step and committed with it; the cut state is read by presentation from that committed tick, as the player's cut is.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Static base with one bar on a limited hinge; stops arrest travel | *present* | `RevoluteHingeDescriptor` `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149` | `CuriousContraptions.tests/ImpactLeverTests.cs:380` |
| Tie port (Load) on the hinged bar; a falling Weight turns it | *present* | `ImpactLeverElementDeclaration.cs:41`–`:51` | `CuriousContraptions.tests/ImpactLeverTests.cs:414` |
| A body landing on the bar turns it | *present* | same hinge | `CuriousContraptions.tests/ImpactLeverTests.cs:73` |
| Constant return spring on a limited hinge, declared by a package | *present, untested* | `RevoluteHingeDescriptor.Stiffness` `MechanicalContractValues.cs:156`; compiled at `engine/gpu/WorkshopPhysicsCompiler.cs:221`; admitted as a constant by `CuriousContraptions.Bundle/BundleValidator.cs:799` | Engine fixture only: `CuriousContraptions.tests/AirflowRuntimeTests.cs:14` (a sprung revolute pays out its stored energy). Every delivered hinge declares stiffness 0. No engine story expected; the slice's row X7 is the first package test |
| Removing a route's tensile rows at a committed step, Cut and Open in the read, Reset restores | *present* (player command only) | `WorkshopSimulation.CutRope` `engine/gpu/WorkshopSimulation.cs:11`; `engine/gpu/WorkshopCommandExecutor.cs:59` | `CuriousContraptions.tests/RopeTests.cs:151`, `:882` |
| A declared material on a tensile connection, carrying a strand diameter and a cutting resistance | *missing* | none: `WorkshopConnection` carries only a rope length (`engine/gpu/WorkshopConnections.cs:31`) | ENGINE-CONNECTION-MATERIALS (see [EL-067](EL-067-steel-cable.md) section 3; story text in the batch summary) |
| A solid strand: the collider of a rope or cable is a property of its declared material | *missing* (the delivered rope is drawn with no collider: a recorded visible-solid defect) | none | ENGINE-CONNECTION-MATERIALS (solid strand), owner decision 1 |
| **A declared blade pair that severs a crossing span under the cutting contract of section 2** (engagement at gap d, resisting force R/d, capacity C, committed cut-work read) | *missing* | none: no part, contact or signal can cut | ENGINE-TENSILE-EXTENSIONS, cutting clause (owner decision 3a; the one law, coordinator decision 16), after ENGINE-CONNECTION-MATERIALS. `ENGINE-FRACTURE` covers a load limit and a one-way break (`epics.md@25df3cd5:4470`), not a blade region or cutting work, and the cut needs neither that limit nor player-built joints; story text is in the batch summary |

Map row S543 and "Source D: cutting work and material criterion, no counterpart identity test" are met by the contract: only two declared numbers are compared.

**Element dependencies.** Delivered: Rope anchor, Weight, Pulley, Bowling ball. Not delivered: the rope's declared material (EL-205) is required; EL-067 steel cable is needed only for row X4.

### Engine conformance

- **Declaration data only.** One limited hinge with a constant spring, one Tie port, and one declared blade pair. The cut is not scissors logic: it is the general cutter capability of section 2, which any package may declare. No callback, script or engine branch on this part.
- **Generic laws.** Closing is hinge dynamics under a rope pull or contact; reopening is the hinge spring; severing is the cutting contract reading declared d, R and C; the route removal is the delivered topology step.
- **Layers.** Colliders: 2 static boxes (post, fixed blade) and 1 box on the hinged bar; the strand passing between the blades is a solid connection-material collider that the blade edges do not meet (section 2). Visuals: the art table, none used as a collider; every proud visible solid has its collider and the pivot disc is printed flush. Animation: `Visual/Bar` follows a body pose; the printed pivot disc is moved by nothing. No presentation binding.
- **One-way flow and typing.** Nothing is read back from art. Port role, domain and attachment role are enums; the material is a typed id on the connection; values are f32.
- **Flat state.** 2 bodies, 3 box colliders, 1 revolute joint, 0 drives, 1 cutter row, 0 sensors, 0 domain nodes, 0 stores, 1 body binding.
- **Performance.** The blade pair is a bounded region tested only against spans, not bodies; its candidate spans come from ENGINE-SPATIAL-QUERIES, not a scan of every route per tick, and no budget is invented here. Three primitive colliders, fewer than the Impact lever. No island sleeping exists today; a resting pair of scissors costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows X1 to X11 run the production simulation; none needs a scissors-specific path.

## 4. Sources and legacy

- Requirements row: "Wrong placement or insufficient cutting work leaves it intact." No variants.
- Current design note: "Scissors declare contact geometry, cutting work and material failure, updating a rope's topology only after the shared fracture criterion … EL-066/068 need LAW-CONTROLLER/S563 models, not … counterpart-type checks" (`docs/general-engine-design.md@a6c914e:L149-L149`).
- Decision S563 fracture: "Just above the break condition the object splits into bounded pieces; just below it holds; unsupported material refused" ([decisions.md](../invest/decisions.md#s543)).

No legacy implementation; legacy records rope cutting as future work (`reference/P0-022-before/source-README.md@a6c914e:L99-L99`). Searched `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: scissor, cut, snip, sever). Rope route facts: [CAT-058](CAT-058-rope_anchor.md) R11–R17.

**Files harvested:** `reference/P0-022-before/source-README.md`.

## 5. Acceptance outline

Requirement row: [element-066](../requirements.md#element-066).

- **Chrome UI recipe (pending, post-catalogue).** Place an Anchor high and a Weight A hanging from it by a rope that passes between the Scissors' blades; hang Weight B from the Scissors' handle `Tie`. Verify placement and both routes; Run; Reset.

All tests are planned, in `CuriousContraptions.tests/ScissorsTests.cs`, on the real simulation. In each, rope route 1 runs from a Rope anchor to a 4 kg Weight A and crosses the blades 0.15 m from the pivot. Stall guard: 5 s.

| Row | Given / when / then | Planned test | Control | Boundary | Tolerance and its gameplay reason |
| --- | --- | --- | --- | --- | --- |
| X1 cut | A 1 kg Weight B hangs from the handle `Tie`. Run: route 1 reads Cut and Open and Weight A falls | `ScissorsTests.AClosingBladeSeversTheRopeBetweenItsBlades` | X2 and X3 | 8 kg Weight B also cuts | Weight A more than 1 m below its start within the guard; before the cut it stays within 0.1 m (the `RopeTests.cs:21` band) |
| X2 wrong placement | Route 1 moved to pass 0.10 m beyond the blade tips. Same B: the blades close fully, route 1 stays Intact, A hangs for 5 s | `ScissorsTests.ARopeBesideTheBladesIsNotCut` | X1 | Route 1 crossing 0.08 m in front of the blades in Z (its 0.02 m strand radius clears the ±0.03 m blades by 0.03 m; at 0.05 m the strand would be tangent to the blade face) | Bar within 0.03 rad of closed; A within 0.1 m |
| X3 insufficient work | B = 0.25 kg (0.47 J against 1 J; 0.63 N·m against 3.75 N·m): the blade stops on the rope, route 1 Intact, A hangs for 5 s | `ScissorsTests.TooLittleCuttingWorkLeavesTheRopeIntact` | X1 (2.45 J) | The masses are 2.2 times below and 2.4 times above the threshold; no finer boundary is tested, because the threshold is a simulated work | Bar stays more than 0.05 rad short of closed (hand estimate 0.18 rad) |
| X4 resistant material | Route 1 made of steel cable, B = 8 kg hung, and again with B dropped 0.5 m onto a slack handle rope: Intact both times | `ScissorsTests.ScissorsNeverCutAMaterialAboveTheirCapacity` (after EL-067) | X1 with rope | none finer: R and C are declared numbers, 25 against 4 | A within 0.1 m for 5 s; the bar stays at least 0.2 rad short of closed (gap held at d: 0.27 rad) |
| X5 the cut costs its work | After X1 the cutter's committed cut-work read is 1 J, and the kinetic energy of B and the bar when the bar reaches its closed stop is lower than in a run with no rope | `ScissorsTests.TheCutRemovesItsWorkFromTheMechanism` | The no-rope run of the same scene | none | Read within 2 % of the declared 1 J; kinetic energy lower by 0.5 to 1.5 J (hinge damping also acts, so the read, not the difference, carries the precision) |
| X6 contact closing | A Bowling ball dropped 1 m onto the handle end cuts route 1 | `ScissorsTests.ABallLandingOnTheHandleCutsTheRope` | The ball dropped beside the handle: Intact | none | as X1 |
| X7 return spring | With B removed (handle rope cut by the player), the bar returns to within 0.05 rad of open in 2 s and stays | `ScissorsTests.TheBladesReopenWhenThePullIsGone` | none | Empty scissors with B = 0.25 kg close fully | 0.05 rad |
| X8 Reset | After X1, Reset: route 1 Intact with its authored length, bar open, both Weights at their built poses; a second Run cuts again | `ScissorsTests.ResetRestoresTheUncutRopeAndTheOpenBlades`; S4 row R3 once delivered | none | none | Authored values exactly |
| X9 save and load | The built scene round-trips with both routes; a save made in Build holds no cut state | S4 row R1b once delivered, else `ScissorsTests.ASavedConstructionLoadsUncut` | none | none | Field equality |
| X10 wrong-kind link | An electrical or activation link to `Tie`, and a second rope on it, are refused | S4 row R5 once delivered, else `ScissorsTests.AWrongKindOrSecondLinkIsRefused` | The handle rope is admitted | none | Typed refusal |
| X11 a slack rope is cut too | Route 1 slack (A resting on a shelf) is cut by X1's stroke; A stays on the shelf | `ScissorsTests.ASlackRopeBetweenTheBladesIsCut` | none | none | A moves less than 0.01 m |

No parameter refusal row: no parameter.

## 6. Open questions

**Resolved**

1. Contact can close the scissors as well as a rope pull. Decided now: the handle is a solid hinged bar, so this is ordinary contact and needs nothing extra; the map's missing ContactImpulse family is navigation only (D14).
2. Rope cutting resistance is 1 J. D1 (starting value). EL-205 owns the declaration; this batch records the value it depends on.
3. A cut span disappears from the tensile solve; frayed ends are artwork of the connection presentation only. D19 (the spec's proposal), and it is what the delivered cut does.
4. Decided now: the scissors have a fixed cutting capacity of 4 J, below the cable's 25 J. Reason: without a capacity a heavy Weight dropped on the handle could cut steel, and "scissors cut rope, snips cut cable" would not be a dependable lesson. The map row for EL-068 names "cutting capacity".
5. Decided now: blade and handle are one bar with one collider, and the lower blade is the moving one, so hanging a Weight on the handle closes the blades.
6. The compound-body prerequisite of the earlier draft is gone: the bar is one box, and multi-collider dynamic bodies exist in any case.

7. The player's Run-time "Cut rope" control is **kept**. Owner decision, 22 October 2026: Story 10.2 keeps runtime rope cutting (`_bmad-output/planning-artifacts/epics.md@25df3cd5:3923`–`:3945`). The earlier proposal to remove it is withdrawn. The player's cut stays a free command on any Rope-domain route, whatever its material, and `RopeTests.cs:151` and `:882` are untouched. A level may withhold it, like any other player control, as authored level data (coordinator decision 24l, 23 November 2026), so a cutter lesson can make the scissors the only way to cut; the control itself stays in the game.
8. Engagement geometry and force law: decided now as section 2 states (strand diameter d, engagement at gap d, force R/d). Reason: the earlier contract had no start angle, distance or resisting force, so rows X3 and X5 could not be implemented.
9. Engine owner: the cut is the cutting clause of ENGINE-TENSILE-EXTENSIONS (owner decision 3a), after ENGINE-CONNECTION-MATERIALS. It needs neither player-built joints nor a load limit, and cutter lessons start in levels 11–20. There is one cutting-work law, this contract (coordinator decision 16, 22 November 2026); the can opener (EL-078) uses it with a seam as target, and the earlier separate cutter story key is withdrawn.
10. A drawn rope or cable is solid (owner decision 1, 22 November 2026): its collider is a property of the declared connection material, owned by ENGINE-CONNECTION-MATERIALS (solid strand). The blade edges' exemption from that contact is part of the cutting clause (section 2).

**Needs the owner**

None. The strand question is decided (owner decision 1; resolved item 10).

## 7. Integration

- **Collaborators.** Rope anchor, Weight, Pulley, Bowling ball (delivered). EL-205 rope material and EL-067 steel cable (not delivered). EL-068 shares the criterion.
- **Campaign.** Scissors and tin snips first use in levels 11–20 (`docs/planning/invest/campaign-reservations.md:44`). Shared interactions [IX-04 tension transmission](../requirements.md#interaction-04) and [IX-37 structural fracture](../requirements.md#interaction-37).
- **Lesson fixture (S8).** Needed by any level that places scissors before this slice; likely, since first use is levels 11–20 and the engine stories come late.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (`origin/main`, `25df3cd5`; pattern EL-239 Baseball). That section is not written here: the reader parses every spec that carries the heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `scissors` |
| Named identity | EL-066, `../invest/named-elements.md#element-066` |
| Acceptance | `todo-193`, `sequence-task-619` |
| Decision | S335, `../invest/scope-corrections.md#s335` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s335` block, and `todo-193` is a scope index not immediately followed by `sequence-task-619` in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element slice. EL-068 follows as its own slice and reuses this probe.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES (candidate spans) and ENGINE-ISLANDS-SLEEP; then (1) ENGINE-CONNECTION-MATERIALS: the connection material with d and R and the solid strand, with the delivered rope converted to the first declared material (EL-205, R = 1 J, d = 0.04 m); (2) ENGINE-TENSILE-EXTENSIONS, cutting clause: the blade pair, engagement, force law, target forms, blade-edge exemption and cut-work read; (3) S7 (the coverage follow-on, which widens the reader, owner decision 15). EL-067 is needed only for X4, which may be added by whichever of the two lands second.
- **Files.** Adds `elements/Scissors/Scissors.csproj`, `ScissorsElementDeclaration.cs`, `scissors.tscn`, `icon.svg`, `CuriousContraptions.tests/ScissorsTests.cs`. No test-project edit (published main compiles every top-level `*.cs` by one rule). Edits, until S3 lands: `CuriousContraptions.slnx`, the trim list in `CuriousContraptions.tests/CSharpStaticGateTests.cs` (ends at `:703`) and the placement switches in `CuriousContraptions.tests/PackageRuntimeParityTests.cs` (`:641`; generic list `:653`). At delivery: the binding section of section 7. This spec and TODO.
- **Review tier.** Engine: the hinge return spring is *present, untested*. Data-only only if an earlier slice has tested a package-declared hinge spring.
- **Disqualifiers the probe must re-check.** The hinge spring's rest angle is not angle 0 of the base frame (then the open pose needs a different frame); the cutter slice delivers a cut without a capacity (then row X4 fails for a dropped Weight and decision 4 returns to planning); its force law is not R/d over the strand diameter (then the masses of X1 and X3 are re-derived, not the tolerances widened); the engine expresses resistance in a unit other than joules; a Tie on a hinged body of a package that also declares a cutter is refused.
