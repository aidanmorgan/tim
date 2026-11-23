# EL-184 · Mechanical ball gate named-identity spec

**Current delivery (24 October 2026):** [EL-184 delivery slice](../invest/named-elements.md#element-184). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every `path@a6c914e:Lstart-Lend` citation resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

**Build-ready revision, 21 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 were rewritten against current code; a `path:line` citation without a commit is in this checkout. Section 4 is the unchanged harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-184 |
| Name | Mechanical ball gate |
| Type | Mechanical |
| Anchor | [requirements.md#element-184](../requirements.md#element-184); [named entry](../invest/named-elements.md#element-184); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("powered and mechanical gates") |
| Proof owner | S366 |
| Disposition | To deliver as a single element. Not a duplicate of the Powered gate (CAT-051, delivered): it has no electrical port and no drive, and adds a rope tie on the blade. Not a Family member with it either: the difference is ports and actuation, not declared values. |
| Planned package | id `mechanical_gate`, directory `elements/MechanicalGate/`, class `MechanicalGateElementDeclaration` |
| Refines / extends | No CAT spec of its own. Shares the conduit, blade and slider of [CAT-051 powered gate](CAT-051-powered_gate.md); rope input per [CAT-058 rope anchor](CAT-058-rope_anchor.md), [CAT-053 pulley](CAT-053-pulley.md) and [CAT-067 weight](CAT-067-weight.md) |
| Related | EL-205 Rope, EL-206/EL-207 pulleys, EL-216 Capture cradle (which this gate can release) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-ISLANDS-SLEEP; then ENGINE-CONNECTION-MATERIALS (solid strand) and ENGINE-JOINT-KINDS (spring rest position, owner decision 6 as extended to this blade by coordinator decision 25, 23 November 2026); then S7. |

## 2. Declaration

Follow the Powered gate (`elements/PoweredGate/PoweredGateElementDeclaration.cs:14`) with the drive, the supply graph and the electrical node removed and a rope port added as on the Impact lever (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:43`–`:51`). The package implements `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static` base, `SupportedDomains = Mechanical | Rope`. Footprint: 2 bodies, 1 moving, 1 prismatic constraint. No stores, no events, no parameters.

### Bodies and colliders

Sourced from the gate; box sizes are full extents in metres.

| Body | Collider | Dimensions and place |
| --- | --- | --- |
| Frame (static) | sleeve | Tube, outer radius 0.70, bore radius 0.65, half-length 0.4, axis X: the delivered gate's sleeve (`elements/PoweredGate/powered_gate.tscn:133`–`:150`, asserted at `CuriousContraptions.tests/PoweredGateTests.cs:63`–`:73`) |
| | collars (2) | Tubes, outer radius 0.78, bore 0.65, half-length 0.09, at x = ±0.4, reaching the mouths at ±0.49. The delivered gate's collars are already solid; the comment at `PoweredGateElementDeclaration.cs:10`–`:13` that says otherwise is stale |
| | header | box 0.8 × 0.65 × 1.8 at (0, 1.65, 0). No slot is drawn: the header is a solid box and the pull rod passes through it as through a bushing (blade and frame do not collide) |
| | posts (2) | boxes 0.24 × 1.7 × 0.12 at z = ±0.82 |
| Blade (dynamic, on the slider) | blade | box 0.12 × 1.44 × 1.44 across the bore |
| | pull rod | box 0.08 × 1.9 × 0.08 standing on the blade's top edge, so its eye is above the header at every blade position |

Blade and rod together 1 kg, moments of the blade box (`PoweredGateElementDeclaration.cs:80`–`:83`), gravity (0, −9.81, 0). Mouths `Start` and `End` at x = ∓0.49 with bore radius 0.65, as the gate's locators.

### Slider

One `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`) along the frame's +Y: limits 0 to 1.55 m, stiffness 14 N/m, damping 4 N·s/m, starting compression 0, `BodiesCollide` false: the gate's values (`PoweredGateElementDeclaration.cs:78`–`:79`). **Spring rest position 0.85 m below closed** (coordinate −0.85 m, outside the travel), the Powered gate's declared rest ([CAT-051](CAT-051-powered_gate.md)), so the closed blade is preloaded by 14 × 0.85 = 11.9 N toward closed, more than its 1 kg × 9.81 = 9.81 N weight: an unroped blade closes in every mounting orientation (owner decision 6; coordinator decision 25, 23 November 2026). The rest position is a missing capability of `ENGINE-JOINT-KINDS` (section 3). **Drive: null.**

### Port

| `PortId` | Domain | Direction | Role | Attachment | Body | Local position |
| --- | --- | --- | --- | --- | --- | --- |
| `Tie` | Rope | Bidirectional | `Tie` | `RopeAttachmentRole.Load` (`CuriousContraptions.Abstractions/PortDescriptor.cs:74`) | `PackageBodyIndex(1)`, the blade | top of the pull rod, (0, 2.62, 0) in the blade's frame, normal +Y |

Tube mouths are geometric. No electrical or signal port: the row requires "actual linkage work".

### Material, metadata and bindings

Restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s on both bodies (`PoweredGateElementDeclaration.cs:47`–`:48`). Category `Motion` (the closed category set has no "Ropes", `CuriousContraptions.Abstractions/ElementCategory.cs:5`), display name "Mechanical gate", palette order 117 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.97, 0.796, 0.322). Presentation: body binding `Visual/Blade` → `PackageBodyIndex(1)`. No state indicator: the blade and the rope are the cue, and the drive-state driver the Powered gate's indicator uses needs a drive.

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| `Visual/Sleeve` | clear `#66b8c9` shell, alpha 0.16 | hollow, the sleeve Tube |
| `Visual/Collar_Start`, `Visual/Collar_End` | cream `#fff8e9` rings | solid, the collar colliders |
| `Visual/Header`, `Visual/Posts` | navy `#293954` header, cream posts | solid, one box each |
| `Visual/Blade` | gold `#f7cb52` plate | solid, the blade box |
| `Visual/Blade/Rod`, `Visual/Blade/Eye` | cream rod with a gold eye, radius 0.07 | solid: the rod box covers both |

Icon: the gate's frame with a rope rising from the blade to a hook, and no lightning mark. It differs from the Powered gate by the pull rod and eye standing above the header and by having no socket or indicator.

### Behaviour and numbers

The blade is closed by its preloaded spring and, upright, by gravity. A rope tied to `Tie` lifts it only while taut. The pull needed at opening x, upright, is the blade's weight plus the spring from its rest 0.85 m below closed: 9.81 + 14·(x + 0.85) = 21.71 + 14·x newtons, 21.7 N to start and 21.71 + 21.7 = 43.4 N at the 1.55 m stop. A hanging Weight of mass m therefore holds the blade at x = (9.81·m − 21.71)/14 m: a Weight below 21.71/9.81 = 2.21 kg does not lift it; the Basketball (0.68 m, the opening under the blade being x − 0.07 m) passes only above x = 0.75 m, so above m = (21.71 + 14 × 0.75)/9.81 = 3.28 kg; the blade reaches its stop above 43.4/9.81 = 4.43 kg. So a 4 kg Weight (39.24 N, the Weight's default) opens it (39.24 − 21.71)/14 = 1.25 m, an opening of 1.18 m, enough for the Basketball; a 3 kg Weight opens it (29.43 − 21.71)/14 = 0.55 m, an opening of 0.48 m, too little for the Basketball; a 2 kg Weight (19.6 N) does not lift it. The Weight's range is 0.25–8 kg (`elements/Weight/WeightElementDeclaration.cs:33`). A rope with slack lets the Weight fall first and the blade starts only when the rope goes taut. If the rope is cut or the Weight is lifted, the blade closes at a rate set by gravity, spring and damping, and stops on a ball beneath it.

**Closure in every mounting orientation (owner decision 6; coordinator decision 25, 23 November 2026).** At the closed stop the spring pushes 11.9 N toward closed. Upright, gravity adds 9.81 N toward closed (21.7 N on the stop). On its side (conduit axis horizontal, blade travel horizontal), gravity is across the travel and the spring alone holds 11.9 N on the stop. Inverted, gravity pulls 9.81 N toward open and the net is 11.9 − 9.81 = 2.09 N toward closed on the stop; at any opening x the net closing force is 14·(x + 0.85) − 9.81 = 2.09 + 14·x N > 0, so a released inverted blade returns all the way to closed. Without the declared rest (spring resting at the closed stop), the inverted blade would hang open at 9.81/14 = 0.70 m, which row A8's control shows.

**The rope is a solid strand (owner decision 1).** Its collider is a property of the declared connection material (declared diameter, segment rule), compiled from declarations. Blocked on ENGINE-CONNECTION-MATERIALS (solid strand). The delivered rope draws a thick strand with no collider today: a recorded visible-solid defect owned by that story.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, StateTransaction (navigation only under D14; the part has no signal and no actuator).

| Capability | Status | Evidence |
| --- | --- | --- |
| Sprung prismatic slider with stops and no drive | present | `MechanicalContractValues.cs:104`; `DeclaredPrismaticDriveTests.ASuppliedBoxWithTheGatesBladeAndLimitsButNoDeclaredDriveNeverActuates` (`CuriousContraptions.tests/DeclaredPrismaticDriveTests.cs:244`); `SliderInstanceTests.ThePlateHoldsItsLowerAndUpperStops` (`CuriousContraptions.tests/SliderInstanceTests.cs:61`) |
| The gate's frame, blade and tube mouths | present | `PoweredGateTests.PhysicsCompiler_CompilesGateWithDynamicBladeAndPrismaticJoint` (`CuriousContraptions.tests/PoweredGateTests.cs:151`); `TubeMouthSnapTests.APoweredGateSnapsOntoAPipeMouth` (`CuriousContraptions.tests/TubeMouthSnapTests.cs:203`) |
| A closed blade stops a ball and an open one passes it | present | `DeclaredPrismaticDriveTests.ABallRestingAgainstTheClosedTiltedBladePassesDownstreamWhenEnabledDuringRun` (`DeclaredPrismaticDriveTests.cs:403`) |
| Tension-only rope: slack does not pull, taut holds | present | `RopeTests.SlackRopeDoesNotAccelerateTheFallingLoad` (`CuriousContraptions.tests/RopeTests.cs:34`), `ACompleteRopeSupportsItsLoadWhileAnUnconnectedLoadFalls` (`:21`) |
| A rope restraining a prismatic coordinate | present | `RopeTests.ASuppliedPrismaticDriveHeldByARopeHoldsShortOfItsTargetAndCommitsRest` (`RopeTests.cs:102`) |
| A falling Weight lifting a jointed body through a rope | present | `ImpactLeverTests.RopeLiftingIntegration_FallingWeightLiftsArm` (`CuriousContraptions.tests/ImpactLeverTests.cs:414`) |
| Rope over a pulley guide; rope cut and Reset | present | `RopeGuideTests.DynamicSimulationRotatesDistinctGuidesInInverseProportionToRadius` (`CuriousContraptions.tests/RopeGuideTests.cs:109`); `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` (`RopeTests.cs:151`) |
| Tube sleeve and Tube collars on a static frame | present | `PoweredGateTests.PoweredGateScene_ExtractsItsSleeveCollarsFrameSocketsAndThreePortLocators` (`CuriousContraptions.tests/PoweredGateTests.cs:56`, assertions `:63`–`:73`) |
| A **`Load` tie declared on a slider's moving body** by a package port | present, untested | the port's `Body` field exists (`PortDescriptor.cs:66`) and is used on a hinge body by the Impact lever; no delivered package puts a tie on a slider body |
| A slider body with **two colliders** (blade and rod) | present, untested | as EL-163 |
| A declared **spring rest position** on a prismatic coordinate, outside its travel (here 0.85 m below closed, a preload), so an unpowered spring-returned slider closes in every mounting orientation | missing | `PrismaticSliderDescriptor` has stiffness, damping and starting compression only (`MechanicalContractValues.cs:104`); a preload through the limits is refused (`CuriousContraptions.Bundle/MechanicalValueDomain.cs:78`–`:81`). Owner: `ENGINE-JOINT-KINDS`, spring rest position (owner decision 6; coordinator decisions 21 and 25) |

One capability is missing, the spring rest position, with one owner. Stale harvest rows ("slider with spring: Story 6.4", "hollow tube collider", "rope domain: Story 10.2", "slider-state cosmetic source") are superseded: the first three are delivered and the fourth is not needed.

## 4. Sources and legacy

- Requirement: "Actual linkage work moves a collision blade controlling a conduit opening"; outcome "Slack linkage or blocked blade prevents instantaneous release" ([element-184](../requirements.md#element-184)).
- Campaign: "powered and mechanical gates", first use 21–30 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Stroke 1.55 m; blade half extents (0.06, 0.72, 0.72); material (0.1, 0.1, 0.3); mouths at ±0.49 with the standard bore | `parts/PoweredGatePart.cs@a6c914e:L15-L30` | carry forward | Shared gate geometry. |
| 2 | Blade 1 kg, spring 14, damping 4; slider frame and stroke; connected collision disabled | `engine/SlidingBlade.cs@a6c914e:L12-L34` | carry forward (geometry and spring), do not carry forward (motor constants 60 N, 120 W) | No motor in the mechanical gate. |
| 3 | Gate states and Blocked classification | `engine/SlidingBlade.cs@a6c914e:L49-L53` | carry forward (states) | Observable jam. |
| 4 | Accepted: a horizontal gate supports settled cargo while closed and releases it when opened; Reset restores the construction | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L79-L117` | carry forward | Support then release. |
| 5 | Accepted: only an open gate lets the ball through, also rotated (20°, 30°, 40°) | `CuriousContraptions.tests/PoweredGateTests.cs@a6c914e:L152-L178` | carry forward | Positive and control. |
| 6 | Accepted: a slack rope does not push; a taut rope holds the load; motion toward the anchor is not resisted | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L126-L156` | carry forward | "Slack linkage" control. |

No legacy mechanical gate exists: the only gate part is the electrically powered one. Row 3's state classification is not presented by this part (no indicator); the blocked blade is proven by position.

**Files harvested:**
- `parts/PoweredGatePart.cs`
- `parts/catalog/powered_gate.tres`
- `engine/SlidingBlade.cs`
- `engine/TubeProxy.cs`
- `engine/ConnectionPort.cs` (rope link validation)
- `CuriousContraptions.tests/PoweredGateTests.cs`
- `CuriousContraptions.tests/RopeTests.cs`
- Searched with no hit for `mechanical gate` or a rope- or lever-driven gate: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Requirement row: [element-184](../requirements.md#element-184). Planned test class `MechanicalGateTests`. The fixture is a pipe run through the gate with a ball queued against the blade, a Pulley above the gate and a Weight on a ledge, roped `Tie` → Pulley → Weight; `x` is the committed blade coordinate.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a 4 kg Weight pushed off its ledge, when Run, then the blade rises (to about 1.25 m) and the queued Basketball passes the gate, and it does not pass before x exceeds 0.75 m (the opening under the blade is x − 0.07 m and the ball is 0.68 m): opening is never instantaneous | `AFallingWeightLiftsTheBladeAndTheQueuedBallPasses` (theory over Weight mass 4 and 6 kg; 6 kg, 58.9 N, opens it to the stop) | a 2 kg Weight (19.6 N, below the 21.7 N starting pull): the blade stays down and the ball stays queued. It fails if the spring's rest is not declared, because the 2 kg Weight would then lift the blade (19.6 − 9.81)/14 = 0.70 m | guard 10 s; "stays down" is x below 0.02 m |
| A2 | The requirement's blocked-blade case. Given a 3 kg Weight, then the blade settles partly open and the Basketball does not pass | `ABladeOpenLessThanTheBallStillBlocksIt` | A1 | x settles between 0.35 and 0.70 m (expected (29.43 − 21.71)/14 = 0.55 m; the band is wide because only "visibly open and less than a ball" matters: its top stays 0.05 m below the 0.75 m at which the Basketball passes) |
| A3 | Given 1 m of extra rope, then the Weight falls before the blade moves: the rope reads Taut no later than the first tick with x above 0.02 m | `ASlackRopeLetsTheWeightFallBeforeTheBladeMoves` | A1 with no slack | causal order only, no tick asserted |
| A4 | Given the rope cut while a ball is under the open blade, then the blade comes down onto the ball and stops, and the ball is not penetrated | `ARopeCutOverABallClosesTheBladeOntoItWithoutPenetration` | with no ball the blade returns to x below 0.02 m | overlap at most 0.02 m |
| A5 | Given the gate yawed 30°, then A1 holds | `ARotatedGateOpensTheSameWay` | — | — |
| A6 | An electrical or Signal output offered to `Tie` is refused; a rope from a Rope anchor is accepted | S4 row 5; until it lands `OnlyARopeConnectsToTheTie` | — | typed refusal |
| A7 | Reset returns the blade to closed and restores the rope and its length; save and load keep the rope links and lengths | `ResetAndSaveLoadRestoreTheGateAndItsRope` | — | generic rows: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`, S4 rows 1 and 3; rope lengths per `RopeTests.AuthoredLengthBoundariesRoundTrip` |
| A8 | Owner decision 6 (coordinator decision 25). Given an unroped gate mounted upright, on its side (rolled 90° about its conduit axis) and inverted, when Run for 5 s, then the blade stays closed (x below 0.02 m) in all three; and given the inverted gate's blade held open at 1 m by a rope that is then cut, it returns to x below 0.02 m within 5 s (net closing force 2.09 + 14·x N > 0 at every opening) | `TheUnropedBladeClosesInEveryMountingOrientation` (theory over the three orientations) | a test declaration with the same blade and spring but its rest at the closed stop (no preload), inverted: the blade hangs open at 9.81/14 = 0.70 m ± 0.05 m | the inverted blade's closing margin at the stop is 2.09 N; "closed" is x below 0.02 m, the A1 tolerance; 5 s is a stall guard far above the blade's settling time (ω = √(14/1) = 3.74 rad/s, ζ = 4/(2·3.74) = 0.53) |

No parameters, so no parameter refusal applies.

### Integration

- Works with CAT-058 Rope anchor, CAT-053 Pulley, CAT-067 Weight, CAT-034 Impact lever (its tie can pull the gate), the pipe kit and every delivered ball; all delivered. Later: EL-216 Capture cradle (released by this gate), EL-207 Moving pulley.
- Campaign: chapter 3 "Pipe Dreams", levels 21–30, "powered/mechanical gates" (`docs/planning/requirements.md:2288`); reuse 41–50, 111–120, 136–150 (`:2398`). Rope and pulley behaviour: [todo-140](../requirements.md#todo-140), [todo-141](../requirements.md#todo-141).
- Coverage binding: a final package-source-binding section in this spec, as EL-239 on published main; blocked on S7 (section 7).

### Delivery

- **Shape.** Single element slice. Not classified in delivery batches; it belongs in 1b "Tensile".
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-ISLANDS-SLEEP; then ENGINE-CONNECTION-MATERIALS (solid strand) and ENGINE-JOINT-KINDS (spring rest position; coordinator decision 25); then S7 (the coverage follow-on, which widens the reader, owner decision 15). S2–S4 optional.
- **Files.** `elements/MechanicalGate/` (project, declaration, `mechanical_gate.tscn`, `icon.svg`); `CuriousContraptions.tests/MechanicalGateTests.cs`; one `CuriousContraptions.slnx` line; the id in the tube-part rows of `TubeMouthSnapTests`; the binding section of section 7 once S7 admits it; until S3, the trim list and placement switch. The test project compiles every top-level `*.cs` by one rule.
- **Review tier.** Engine tier: two *present, untested* rows.
- **Disqualifiers the probe re-checks.** A tie port that cannot name a slider's moving body; a slider body limited to one collider (then draw the rod as part of one taller blade box); the rope route refusing a Load end that starts at its lower stop; the solid strand's collider snagging the pull rod, the eye or the header as the blade rises and falls.

## 6. Open questions

All resolved.

1. **Rope is the only linkage input** (D19): the row's "linkage" is satisfied by a rope, which every delivered puller (Weight, Impact lever, Linear pusher through cargo) can drive. A rigid `LinkageIn` would need a player-made joint, owned by `ENGINE-ASSEMBLY-JOINTS`; add it only when a lesson needs it.
2. **The blade travels vertically like the Powered gate**, so the two gates read as a pair and share geometry. No hinge.
3. **No parameters** (D13): blade weight, spring and stroke fix the pull, which is the lesson.
4. **Category Motion**: the closed category set has no rope group.
5. **No state indicator**: decided under the standing delegation, reason in section 2.
6. **Frame colliders are the delivered gate's Tubes, and no slot is drawn in the header** (22 November 2026, audit M13 and L4).
7. **The blade's spring declares a rest position 0.85 m below closed** (coordinator decision 25, 23 November 2026, extending owner decision 6 to every spring-returned blade that must close unpowered). The Powered gate's value is reused because the blade, spring and stroke are the Powered gate's; rows A1, A2 and A8 are sized to it (section 2).

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** A static frame, one sprung slider body, one typed rope port on that body and one body binding. No drive, no solver branch, callback or engine test on this package.
2. **Generic laws.** The prismatic joint with its declared spring, spring rest position and stops, gravity on the 1 kg blade, the tension-only rope constraint and contact. The blade opens because a rope does work on it; "opens when a weight falls" is a consequence, not a rule; "closes in every orientation" is the preload, not a rule.
3. **Three layers.** Colliders: eight. Static body: sleeve Tube, two collar Tubes, header box, two post boxes. Blade body: blade box and pull-rod box. Visuals: clear sleeve, collars, header, posts, blade, rod and eye; none is a collider, and each visible solid has its collider (the eye sits inside the rod box; the sleeve is hollow over its Tube). Animation: none. `Visual/Blade` and its rod and eye follow body 1's pose; the rope is the connection's own drawing, and its solid strand's collider belongs to the connection material (ENGINE-CONNECTION-MATERIALS), not to this part.
4. **Flow and typing.** One Rope-domain `Tie` port with `RopeAttachmentRole.Load`; no parameters; nothing flows back from presentation.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 8 (Tubes 3, boxes 5); joints 1 prismatic; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 1 (body).
6. **Performance.** Eight colliders, as the Powered gate's frame and blade. No island sleeping exists today; a resting closed blade costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part. Nothing is cosmetic.
7. **Tests are scenarios.** Rows A1 to A8 run the production simulation with a delivered Weight, Pulley and rope; none needs a code path of this part.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `mechanical_gate` |
| Named identity | EL-184, `../invest/named-elements.md#element-184` |
| Acceptance | none yet: the ledger entry cites `campaign-element-coverage`, not a todo and sequence-task pair |
| Decision | S366, `../invest/scope-corrections.md#s366` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s366` block on published main; and the ledger entry carries no `todo-` link, which the reader requires exactly once, with a sequence task co-anchored to it.
