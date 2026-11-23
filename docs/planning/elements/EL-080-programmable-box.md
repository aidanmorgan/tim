# EL-080 · Programmable box — named-identity spec

**Current delivery (24 October 2026):** [EL-080 delivery slice](../invest/named-elements.md#element-080). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `path@a6c914e:L…` resolves in git history. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32 ([numeric contract](../../gpu-f32-physics.md#precision-status)).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-080 · Programmable box · Specialist |
| Anchors | [requirements.md#element-080](../requirements.md#element-080) (task `sequence-task-633`); [named entry](../invest/named-elements.md#element-080); scope index [todo-222](../requirements.md#todo-222) |
| Proof owner | S650 ([decision](../invest/scope-corrections.md#s650)) |
| Disposition | To deliver as a single element after the two general engine stories it shares with [EL-076 Programmable ball](EL-076-programmable-ball.md) (section 3). Not a duplicate of the Receiver (a static goal container) or the Domino (a solid block): it is a movable open container. |
| Planned package | id `programmable_box`, directory `elements/ProgrammableBox/`, class `ProgrammableBoxElementDeclaration` |
| Refines / extends | [CAT-023 Domino](CAT-023-domino.md) (dynamic box) and [CAT-004 Receiver](CAT-004-basket.md) (open container walls) |
| Status | not started; blocked on the engine programme (owner decision 3a), then `ENGINE-PARAMETER-BOUND-FRAMES` (its Choice clause) and `ENGINE-PARAMETER-COMBINATIONS`, then S7 |

## 2. Declaration

"Programmable" means preset admission, not a controller and not a numeric editor (S650): the author picks a size, a mass and a material in Build, and the box is then an ordinary rigid body with real walls. Contents stay in because the walls are colliders; nothing else holds them.

The package implements `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`) and `IParameterizedMaterialElement` (`CuriousContraptions.Abstractions/MaterialParameterBinding.cs:10`). The closest delivered shape is the Weight (`elements/Weight/WeightElementDeclaration.cs:12`): one dynamic body with five colliders, every one sized from a parameter. `KinematicMode.Dynamic`, `SupportedDomains = Mechanical`, `PayloadPolicy.None`, footprint one body, one moving.

### Parameters

| Key | Kind | Options (index order) | Default | Editor policy |
| --- | --- | --- | --- | --- |
| `size` | Choice | `Small`, `Medium`, `Large` | `Medium` | Reject, Build only |
| `mass` | Choice | `Light`, `Standard`, `Heavy` | `Standard` | Reject, Build only |
| `material` | Choice | `Wood`, `Metal`, `Rubber` | `Wood` | Reject, Build only |

Per-option values (all **starting values**):

| Choice | Option values | Reason |
| --- | --- | --- |
| `size` → side s | 0.8 m, 1.2 m, 1.6 m | Small holds a Tennis ball, Medium a Basketball, Large the largest ball |
| `mass` → mass | 0.5 kg, 2 kg, 6 kg | from lighter than a Basketball to heavier than a Bowling ball, inside the Weight's 0.25–8 kg |
| `material` → restitution | 0.1, 0.05, 0.5 | |
| `material` → friction | 0.6 (the Domino's), 0.3, 0.9 | wood grips, metal slides, rubber grips and bounces |

**Excluded combination**, declared as data: (`size` Large, `mass` Light): a 1.6 m, 0.5 kg box is shoved about by any ball and cannot carry a load readably. It excludes three of the 27 combinations (one per material); 24 are admitted, and the default is among them.

### Body and colliders

Every length is a fixed fraction of the side s, so the three sizes are the same shape scaled. The body origin is the centre of the box's underside, so a size change grows it upward. Outer size s × 0.75 s × s; floor and walls 0.1 s thick. Each collider is a hidden `col_` node.

| Collider (box) | Half extents | Centre |
| --- | --- | --- |
| Floor | (0.5 s, 0.05 s, 0.5 s) | (0, 0.05 s, 0) |
| Walls ±X | (0.05 s, 0.325 s, 0.5 s) | (±0.45 s, 0.425 s, 0) |
| Walls ±Z | (0.4 s, 0.325 s, 0.05 s) | (0, 0.425 s, ±0.45 s) |

That is 24 collider bindings on `size` (three half extents each, plus the non-zero centre components), each a pure multiplier, in the form of `WeightElementDeclaration.cs:57`–`:76`.

| Size | Outer (m) | Inner width (m) | Inner depth (m) | Wall (m) |
| --- | --- | --- | --- | --- |
| Small | 0.8 × 0.6 × 0.8 | 0.64 | 0.52 | 0.08 |
| Medium | 1.2 × 0.9 × 1.2 | 0.96 | 0.78 | 0.12 |
| Large | 1.6 × 1.2 × 1.6 | 1.28 | 1.04 | 0.16 |

**Mass frame.** Mass from `mass`. Treating the five boxes as one uniform solid (volumes 0.100, 2 × 0.065 and 2 × 0.052 of s³, total 0.334 s³):

- Centre of mass: (0, 0.313 s, 0), from (0.100 × 0.05 + 0.234 × 0.425) ÷ 0.334.
- Principal moments about it: (0.175, 0.241, 0.175) × m × s², by the parallel-axis sum over the five boxes. Declared with `PrincipalMomentOperation.ScaledMassLengthSquared` (`CuriousContraptions.Abstractions/PrincipalMomentDeclaration.cs:27`), whose coefficients are a vector and whose mass and length operands here are the `mass` and `size` option values.
- Default (Medium, Standard): 2 kg, centre of mass 0.375 m up, moments (0.504, 0.694, 0.504) kg·m².

Gravity (0, −9.81, 0). Material: restitution and friction from `material` (two material bindings; a friction binding exists as `MaterialComponent.Friction`, `MaterialParameterBinding.cs:5`, and no delivered package binds it); rolling resistance 0; bounce threshold 0.1 m/s. No ambient load, joints, drives, stores, sensors, ports or events.

### Metadata and bindings

Category `Motion`; display name "Programmable box"; description "An open box you set up before the run: pick its size, weight and material. It holds what fits inside it."; colour (0.76, 0.56, 0.32) (warm wood `#c28f52`); pick radius 0.9; palette order 167 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Presentation parameter bindings (`PresentationParameterBinding`, `CuriousContraptions.Abstractions/ElementPresentation.cs:119`), one value per option:

| Node | Property | Parameter | Values by option |
| --- | --- | --- | --- |
| `Visual` | `UniformScale` | `size` | 0.8, 1.2, 1.6 (the scene is authored at s = 1) |
| `Visual/Marks/MassPip2` | albedo red, green, blue | `mass` | wood, navy, navy |
| `Visual/Marks/MassPip3` | albedo red, green, blue | `mass` | wood, wood, navy |
| `Visual/Marks/Grain` | albedo red, green, blue | `material` | navy, wood, wood |
| `Visual/Marks/Rivets` | albedo red, green, blue | `material` | wood, navy, wood |
| `Visual/Marks/Ribs` | albedo red, green, blue | `material` | wood, wood, navy |

Sixteen bindings (one scale, five marks of three colour channels). No binding uses `Visible`: a mark that is "off" is drawn in the wall's own wood colour. Nothing here shows or hides a node or a collider; that is the business of the "Parameter-selected parts" row of `ENGINE-PARAMETER-BOUND-FRAMES`, which this part does not use.

### Art and solidity

No delivered package uses a texture, so the marks are geometry. The front wall's outer face is drawn as a flat main panel with six cut-outs, each filled by an inlay panel lying in the same plane. Panels never overlap and nothing is proud.

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Floor`, `Visual/WallLeft`, `Visual/WallRight`, `Visual/WallBack` | boxes of the collider sizes, warm wood `#c28f52` | solid, one box collider each | body pose; scaled by `size` |
| `Visual/WallFront` | the front wall's box, its outer face cut out for the marks | solid: with the inlays below it is the front wall's box collider | body pose; scaled by `size` |
| `Visual/Marks/MassPip1`, `MassPip2`, `MassPip3` | three square inlay panels in a row at a front corner; pip 1 is always navy `#293954` | solid: part of the front face, zero proud | body pose; pips 2 and 3 coloured by `mass` |
| `Visual/Marks/Grain`, `Rivets`, `Ribs` | three inlay panels shaped as wavy lines, a row of dots and upright bars | solid: part of the front face, zero proud | body pose; one coloured navy by `material` |

Edges are drawn square to the colliders (a chamfer would stand off them). Mass is read by pip count and material by which of three differently shaped marks is dark, not by colour alone. Icon: an open-topped box in three-quarter view with a ball inside. It differs from the Receiver at a glance by having four equal walls, no lowered lip and no halo, and by its pips.

### Behaviour and numbers

Laws: rigid-body dynamics of one compound body, gravity, contact with restitution (product of the two colliders' values, `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:117`) and friction (geometric mean, `:119`).

- **What fits.** Tennis ball 0.50 m (its seam is printed, owner decision 24i), Baseball 0.54 m across its band (repair values, radius 0.26 m with a 0.01 m proud band; coordinator decision 36), Soccer ball and Cannonball 0.66 m, Basketball 0.70 m across its band (stripe outer radius 0.35, `elements/Ball/ball.tscn:18`), Programmable ball Large 0.928 m across its band ([EL-076](EL-076-programmable-ball.md)). Small (0.64 m inside) takes the first two; Medium (0.96 m) takes all of them, the Large Programmable ball with (0.96 − 0.928) ÷ 2 = 16 mm a side; Large (1.28 m) takes all with room. A ball too wide for the opening rests on the rim.
- **Containment.** A Basketball resting in a Medium box has its centre 0.12 + 0.34 = 0.46 m above the box's underside and its top at 0.80 m, 0.10 m below the 0.90 m rim.
- **Shove.** A 1 kg Basketball rolling at 2 m/s into a Medium wooden box (contact restitution 0.55 × 0.1 = 0.055) gives the box (1 + 0.055) × 1 ÷ (1 + m) × 2 m/s: 1.41 m/s if Light, 0.70 if Standard, 0.30 if Heavy. Sliding on a surface of friction 0.3 (√(0.6 × 0.3) = 0.42) that first shove alone carries it v² ÷ (2 × 0.42 × 9.81): 0.24 m, 0.06 m and 0.011 m. But the ball, still rolling, strikes again: it keeps (1 − 0.055 m) ÷ (1 + m) × 2 m/s, 1.30 m/s against a Light box and 0.19 against a Heavy one, and its retained spin (ωr = 2 m/s) restores rolling at (5v + 2ωr) ÷ 7: 1.50 and 0.71 m/s. The friction work the box can absorb per metre is 0.42 × 9.81 × m: 2.1, 8.3 and 25 N, so a Light box goes furthest. Bounds: the ball's whole 2.8 J (0.7 × 1 × 2²) limits the Light box to 2.8 ÷ 2.1 = 1.35 m. For the Heavy box the re-strike is weak: the box (decelerating at 4.16 m/s²) and the ball (gaining 2.94 m/s² from its spin) meet again after 0.031 s, the box at 0.17 m/s and the ball at 0.28, and the second shove adds 1.055 ÷ 7 × 0.11 = 0.017 m/s, about 0.001 m more slide; the pressing ball then pushes with at most its bench friction, 0.3 × 9.81 = 2.9 N, against 25 N, so the Heavy box ends about 0.012–0.015 m from its start, against at least 0.24 m for the Light box. The ball strikes 0.34 m up, just under the 0.375 m centre of mass, so the box slides and does not tip.
- **Slope.** On a Ramp (friction 0.3, `elements/Ramp/RampElementDeclaration.cs:56`) the box slides when tan θ exceeds √(material friction × 0.3): Metal 0.30 (16.7°), Wood 0.42 (22.9°), Rubber 0.52 (27.5°). At 20° (tan 0.364) only Metal slides, at 9.81 × (sin 20° − 0.30 cos 20°) = 0.59 m/s², covering 0.3 m in about 1 s.
- **Stability.** The centre of mass is 0.313 s up and 0.5 s from an edge: it tips only past 58°.
- **Refusal.** As [EL-076](EL-076-programmable-ball.md): an excluded combination, an out-of-range or fractional option, a missing Choice or any added number is refused at the edit and at save and wire decode.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, TimedCommand. Under D14 the row is navigation: S650 excludes a controller, so only ContactImpulse, GeometryQuery, RigidBodyDynamics and friction are used. Tests marked † were opened for this revision.

| Capability | Status | Evidence |
| --- | --- | --- |
| One dynamic body with several colliders, each sized and placed from a declared parameter | present (for a Real parameter) | `WeightElementDeclaration.cs:57`–`:76`; `WeightTests.AMassEditGrowsABenchRestingWeightUpwardAndItsBaseStaysOnTheBench` † (`CuriousContraptions.tests/WeightTests.cs:286`) |
| Declared centre of mass and principal moments on a multi-collider body | present | `CompoundMassAndRollingTests.OffCentreImpactUsesDeclaredMassCentreAndRotatedPrincipalInertia` † (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:221`) |
| Many child colliders in simultaneous support, with friction | present | `CompoundMassAndRollingTests.ManyChildSupportKeepsFiniteDissipativeMotionAndFrictionControl` † (`:19`) |
| A material component bound to a parameter | present (restitution, for a Real parameter) | `elements/TennisBall/TennisBallElementDeclaration.cs:39`–`:40`; `TennisBallTests.InvalidConfigurationCannotEnterConstruction` † (`CuriousContraptions.tests/TennisBallTests.cs:188`) |
| A Choice parameter with closed options; bad index refused | present | `FanAdmissionTests.PackageScalarEndpointsAndClosedModeAreAdmittedAtomically` † (`CuriousContraptions.tests/FanAdmissionTests.cs:267`) |
| A free dynamic body made of five boxes carrying a loose ball inside it, at rest and while shoved; a friction material binding; a mass-frame centre that follows a parameter | present, untested | the delivered open containers (Receiver, Funnel) are static and the delivered multi-collider dynamic bodies carry nothing; `MaterialComponent.Friction` is bound by no package; no delivered mass-frame centre follows a parameter |
| **A Choice option carries the bound value** in collider components, material components, mass, centre of mass, principal-moment operands and presentation parameter bindings, one f32 per option | missing | bindings require a Real today (`CuriousContraptions.Bundle/BundleValidator.cs:425`, `:855`–`:863`). Owner: `ENGINE-PARAMETER-BOUND-FRAMES`, its Choice clause (owner decision 3a and coordinator decision 16; Batch O G5; clause text in the Batch L summary) |
| **Declared excluded combinations of Choice options**, refused atomically | missing | Owner: proposed `ENGINE-PARAMETER-COMBINATIONS` (`engine-parameter-combinations`), text in the Batch L summary |
| Coverage admits a named-identity package | narrower | owner: coverage follow-on story S7 (section 7) |

Of `ENGINE-PARAMETER-BOUND-FRAMES` this part needs only the Choice clause: its collider bindings are on the root body and of the delivered kind. Its "Parameter-selected parts" row is not needed: no art node or collider appears or disappears.

## 4. Sources and legacy

- Requirement: "Author-defined validated size, mass and material form a physical container"; outcome "Contents and collisions respond to its actual geometry" ([element-080](../requirements.md#element-080)).
- Decision [S650](../invest/scope-corrections.md#s650): "A contained load stays inside its actual walls, a collision deflects from them, an unsupported preset is refused … this is not a controller or a numeric solution editor."
- A search at `a6c914e` for "programmable", "crate" and "container" over the Epic 7 deletion paths found no programmable-box source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Weight mass must be finite and 0.25–8 kg | `parts/WeightPart.cs@a6c914e:L24-L26` | carry forward as the envelope of the three mass options |
| L2 | A part with physics parameters rejects a missing parameter; Run and Restore round-trip | `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs@a6c914e:L47-L81` | carry forward as "an incomplete or unknown preset is refused"; do not carry forward string keys |
| L3 | Dynamic bodies compiled mass properties from exactly one collider | `engine/gpu/RigidMassProperties.cs@a6c914e:L26-L30` | do not carry forward: multi-collider dynamic bodies with a declared mass frame are delivered (section 3) |

**Files harvested:** `parts/WeightPart.cs`, `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs`.

## 5. Acceptance outline

Requirement row: [element-080](../requirements.md#element-080). Planned test class `ProgrammableBoxTests`. Unless stated the box is Medium, Standard, Wood, standing on the bench.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given each of the 24 admitted combinations, when compiled, then the five colliders, mass, centre of mass, moments and material are that combination's declared values | `EveryAdmittedPresetCompilesItsDeclaredGeometryAndMass` (theory) | the default | authored constants compared exactly |
| A2 | Given a Basketball dropped from 1 m above the rim over the box's centre, then it ends at rest inside, its centre within the inner footprint and 0.46 m above the box's underside, and the box has moved less than 5 cm | `ADroppedBallStaysInsideTheWalls` | a ball dropped 0.2 m outside a wall lands on the bench beside the box | centre height within 2 cm; 5 s stall guard |
| A3 | Given a Basketball rolled at 2 m/s into a side wall, then the ball does not pass the wall, and a Light box slides at least 0.1 m and at least three times as far as a Heavy one | `ACollisionDeflectsFromTheWallAndALighterBoxIsShovedFurther` | the Heavy box | predicted first-impact box speeds 1.41 and 0.30 m/s; the rolling ball strikes again, so distances exceed 0.24 m and 0.011 m: Light between 0.24 and 1.35 m, Heavy about 0.012–0.015 m (Behaviour, Shove), so the threefold ratio holds with a factor of about 16 and fails if the mass preset is not applied (equal distances); ball–wall overlap at most 2 cm |
| A4 | Given a Basketball and a Tennis ball each dropped over a Small box, then the Tennis ball ends inside and the Basketball rests on the rim with its centre above it | `TheSizePresetDecidesWhatFits` | the same Basketball ends inside a Medium box | — |
| A5 | Given Metal, Wood and Rubber boxes each on a 20° Ramp, then the Metal box slides at least 0.3 m within 3 s and the other two move less than 2 cm | `TheMaterialPresetDecidesWhetherItSlides` | Wood | predicted thresholds 16.7°, 22.9° and 27.5°; 20° is 3° clear of the nearest |
| A6 | Given a box holding a Basketball and shoved 1 m along the bench by a Linear pusher, then the ball is still inside when the box stops | `ContentsTravelWithTheBox` | the same shove with the ball standing beside the box leaves the ball behind | ball centre within the inner footprint at the end |
| A7 | Given `mass` Light, when `size` is set to Large (or the reverse order), then the edit is refused with a typed reason and nothing changes | `TheExcludedCombinationIsRefusedAtTheEdit` | Large with Standard is admitted | all three materials |
| A8 | Given a save or wire record with the excluded combination, an option index of 3 or 0.5, a missing Choice or an added numeric size or mass, then it is refused atomically; during Run no parameter can be edited | `ASaveCannotCarryAnExcludedPresetOrAnyNumberAndNothingEditsDuringRun` | an admitted non-default combination round-trips | the run-control table has no row for this part |
| A9 | Given Run, Reset, save and load of a box with a ball inside, then box, ball and presets are restored and a second Run repeats A2 | `ResetSaveAndLoadRestoreTheBoxAndItsContents` | — | generic row: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| A10 | Given each size, then each drawn wall (the front wall with its inlays) has its collider, a probe lowered into the box reaches the floor and a probe at a wall is stopped; for each `mass` and `material` option the marks carry that option's colours | a `VisibleSolidColliderTests` row and `TheBoxIsHollowItsWallsAreSolidAndItsMarksFollowThePresets` | — | probe clearance 2 mm; mark colours are authored constants read through the presentation evaluator |

A2, A4 and A6 are "contents … respond to its actual geometry"; A3 and A5 are "collisions respond"; A1, A7 and A8 are "author-defined validated size, mass and material". No ports, so no connection refusal applies. Retained CAT-023 acceptance (a dynamic box rests, slides and is pushed by contact): A3 and A5. Retained CAT-004 acceptance (walls hold a ball): A2; this part has no capture sensor and captures nothing.

### Integration

- Works with every ball, Ramp, Wall, Linear pusher, Conveyor, Pressure plate (box plus contents load it), Trampoline (all delivered); EL-076 (Large in Large); later carriers and lifts.
- Campaign: the coverage ledger row "Size grate, weight tray/material sorter, … opener, mixer, box presets and contact display" reserves first use in levels 91–100 and reuse 111–120, 136–150 (`docs/planning/requirements.md:2410`); chapter 10 "Oddly Satisfying".
- A level that fixes a box in place or locks its presets needs the lesson-fixture story S8.

### Delivery

- **Shape.** Single element, after EL-076 (which exercises both engine stories on a simpler body).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element; then `ENGINE-PARAMETER-BOUND-FRAMES` (this part needs its Choice clause); then `ENGINE-PARAMETER-COMBINATIONS`; then S7.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (this part declares only `PayloadPolicy`).
- **Files.** `elements/ProgrammableBox/` (project, declaration and `.uid`, `programmable_box.tscn` with five wall and floor meshes, six inlay panels and five hidden colliders; no texture file; `icon.svg` and its import file); `CuriousContraptions.tests/ProgrammableBoxTests.cs`; one `CuriousContraptions.slnx` line; until S3, the hand lists a new package still edits; the ledger row; the binding section once S7 admits it.
- **Review tier.** Engine tier, because of the *present, untested* row; data-only if the probe finds exercising tests and both engine stories are published.
- **Disqualifiers the probe re-checks.** A ball inside a moving five-box body that jitters, creeps through a wall or gains energy; a compound body that sinks or drifts at rest on its floor collider; a mass frame whose centre cannot follow a parameter; a bench friction other than the 0.3 assumed in A3; a visible-solid check that cannot classify a wall mesh and its inlays against one box collider.

## 6. Open questions

All resolved.

1. **Preset membership and the exclusion:** the spec's proposal (D1, D19), with one change: Medium and Large heights become 0.9 m and 1.2 m and the wall thickness becomes 0.1 s, so the three sizes are one shape scaled and every collider and the inertia follow from one length.
2. **Open-top only** (D19): no requirement or lesson uses a lid.
3. **Fixing it in a level** is the lesson-fixture story's capability, not a parameter of this part.
4. **Not a payload** (`PayloadPolicy.None`): the box is a container, not something a Receiver captures.
5. **One frozen preset first?** S650 speaks of freezing "one validated … preset". That is met by the default combination's rows (A2, A3, A6, A9); the other combinations are the requirement row's "author-defined" choice and ship in the same slice.
6. **Marks:** inlaid panels whose colour is bound to the Choice, never shown or hidden; **owner of Choice-bound values:** `ENGINE-PARAMETER-BOUND-FRAMES` (coordinator decision 16).

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** One body, five colliders, three Choice parameters with per-option values, one excluded option pair and sixteen presentation parameter bindings. No controller, callback or engine test on this package; no containment rule.
2. **Generic laws.** Compound rigid-body dynamics, contact and friction. Containment is the walls' contact; the shove distance and the slope threshold follow from mass and friction.
3. **Three layers.** Colliders: five hidden boxes on the one body, sized from `size`. Visuals: five box meshes matching them at every size, the front one drawn with six flush inlay panels; nothing is proud. Animation: none during a Run; the presentation parameter bindings are resolved from authored parameters, and every node follows the body pose. No node is shown or hidden.
4. **Flow and typing.** Three closed Choices; option names only at the UI and save boundary; f32 values; no ports or events.
5. **Flat state.** Bodies 1 (dynamic); colliders 5 (boxes); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; presentation parameter bindings 16. Counts are the same for all 24 combinations.
6. **Performance.** Five primitives against the Domino's one: a hollow container cannot be fewer than a floor and four walls. Nothing is evaluated per tick for the presets. Nothing sleeps today; a box at rest, with its contents, will sleep as one island when `ENGINE-ISLANDS-SLEEP` lands.
7. **Tests are scenarios.** Rows A2–A6 run the production simulation with delivered parts; A1, A7 and A8 exercise the production admission path.

**Verdict: conforms, with two general capabilities missing, each owned by one element-free engine story** (the audit's fixes of 22 November 2026 are applied: one owner for Choice-bound values, no `Visible` by Choice, the mark mechanism).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `programmable_box` |
| Named identity | EL-080, `../invest/named-elements.md#element-080` |
| Acceptance | `todo-222`, `sequence-task-633` |
| Decision | S650, `../invest/scope-corrections.md#s650` |
| Mode records | one per Choice: Size (Small, Medium, Large), Mass (Light, Standard, Heavy), Material (Wood, Metal, Rubber); the exact record form for a parameterised part is S7's to fix |

What it has: the `s650` decision block (`docs/planning/invest/scope-corrections.md:50`, and on published main), and its sequence task, `sequence-task-633`, beside the `element-080` anchor (`docs/planning/requirements.md:5035`–`:5037`). What the reader lacks today: it wants the todo and the task adjacent in one row, while `todo-222` is a scope-index row shared with EL-081 (`:4087`) apart from the task.
