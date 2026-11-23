# EL-096 · Tipsy platform — named-identity spec

**Current delivery (24 October 2026):** [EL-096 delivery slice](../invest/named-elements.md#element-096). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout, and `path@a6c914e:L…` is the legacy harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Every number below is a hand calculation; nothing was simulated. Game values are canonical f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-096 · Tipsy platform · Specialist |
| Anchor | [requirements.md#element-096](../requirements.md#element-096); [named entry](../invest/named-elements.md#element-096); scope index [todo-219](../requirements.md#todo-219) (shared with TH-33, TH-35, TH-36); [sequence-task-649](../requirements.md#sequence-task-649) |
| Proof owner | S666 |
| Disposition | To deliver as a single element. Not a duplicate of the delivered Impact lever (`impact_lever`): it has no rope ports and no parameters, a different deck, pivot friction and travel, and a centre of mass above the pivot, so a tipped deck stays tipped. Not a Family member: no sibling shares its shape. |
| Planned package | id `tipsy_platform`, directory `elements/TipsyPlatform/`, class `TipsyPlatformElementDeclaration` |
| Refines / extends | No CAT spec. Declaration shape of [CAT-034 Impact lever](CAT-034-impact_lever.md) (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:12`) |
| Status | not started; blocked on the engine programme (owner decision 3a), then S7 |

## 2. Declaration

The package implements `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), as the Impact lever does. `KinematicMode.Static` base, `SupportedDomains = Mechanical`, footprint 2 bodies, 1 moving, 1 revolute constraint. The part origin is on the bench under the ridge; +X runs along the deck.

### Bodies and colliders

| Body | Collider | Shape and size (m) | Centre (m) | Material (restitution, friction, rolling resistance, bounce threshold) |
| --- | --- | --- | --- | --- |
| 0, static | ridge | box 0.2 × 0.55 × 0.8 | (0, 0.275, 0) | 1, 0.3, 0, 0.1 (the Impact lever's base, `ImpactLeverElementDeclaration.cs:69`) |
| 0, static | pivot cap | cylinder radius 0.0625, half-length 0.4, axis Z | (0, 0.60, 0) | as the ridge |
| 1, dynamic | deck | box 2.4 × 0.12 × 0.8 | (0, 0.7225, 0) at rest | 0.05, 0.6, 0, 0.1 (**starting value**: a wooden plank; grippy so a resting ball does not slide) |

The cap's radius is the smallest admitted cylinder radius, 1/16 m (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:240`). Its axis is the hinge axis, so the deck's underside stays tangent to the cap at every angle and nothing drawn interpenetrates; the cap's lower half is sunk in the ridge's top. Deck mass 1.5 kg (**starting value**); centre of mass at the deck centre, 0.0625 + 0.06 = 0.1225 m above the pivot; principal moments (box law) 1.5·(0.12² + 0.8²)/12 = 0.082, 1.5·(2.4² + 0.8²)/12 = 0.80 and 1.5·(2.4² + 0.12²)/12 = 0.722 kg·m² about X, Y and Z; gravity (0, −9.81, 0).

### Hinge

One `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): support body 0; base frame at (0, 0.60, 0), identity rotation (axis local Z); moving frame at (0, −0.1225, 0) in the deck's frame; `RevoluteTravel.Limited` from −0.349 to +0.349 rad (±20°); stiffness 0; damping 0; `FrictionTorque` 2.5 N·m; initial angle 0; `BodiesCollide` false; no drive.

No ports, no parameters, no stores, no sensors, no events. The earlier draft's `ridge_offset` parameter is dropped (section 6).

### Metadata and bindings

Category `Motion`, display name "Tipsy platform", palette order 168 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.76, 0.56, 0.32). Presentation: one body binding `Visual/Deck` → `PackageBodyIndex(1)` (`CuriousContraptions.Abstractions/ElementPresentation.cs:114`). No animation bindings.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Ridge` | navy `#293954` box, the collider's size | solid, box collider | nothing |
| `Visual/Cap` | cream `#fff8e9` cylinder, the collider's size | solid, cylinder collider | nothing |
| `Visual/Deck` | wood `#c28f52` plank, the collider's size | solid, box collider | body 1 pose |
| `Visual/Deck/CentreMark` | gold `#f7cb52` stripe across the deck top over the pivot | decorative: printed flat | body 1 pose (child of the deck) |

Icon: a plank balanced on a narrow post with a small curved arrow at one end. It differs from the Impact lever by the single narrow post under a plain plank (the lever has a fulcrum block, two end stops and tie studs), and from the Ramp by standing level on a post.

### Behaviour and numbers

All behaviour is the revolute joint law with Coulomb friction, joint limits and contact; nothing is timed.

- **Tipping threshold.** A load of mass m resting at offset x on the level deck applies m·g·x about the pivot. The pivot holds while that is below 2.5 N·m and turns above it. Threshold offsets: Bowling ball (4 kg) 2.5/(4 × 9.81) = 0.064 m; Basketball (1 kg) 0.255 m; Tennis ball (0.35 kg, `elements/TennisBall/TennisBallElementDeclaration.cs:16`) 0.728 m. 2.5 N·m is a **starting value** chosen so a Basketball holds at 0.20 m (1.96 N·m, 22 % under) and tips at 0.30 m (2.94 N·m, 18 % over). The audit re-derived these thresholds and they hold.
- **Why "tipsy".** Once the deck turns by θ, the deck's own weight adds 1.5 × 9.81 × 0.1225 × sin θ = 1.80·sin θ N·m and a ball on it adds more as it rolls outward, so a deck that starts to tip runs to its stop. Unloaded at the stop the deck's own torque is 1.80 × sin 20° = 0.62 N·m, below the friction torque, so it stays tipped until Reset. A lever balanced on its pivot does neither.
- **A dropped load tips more easily than a resting one.** A Basketball dropped from 1 m lands at 4.4 m/s; at 0.20 m off centre its angular impulse of about 1 N·m·s gives the deck (0.722 + 1.5 × 0.1225² = 0.745 kg·m² about the pivot) about 1.4 rad/s, which friction cannot stop before the ball rolls. The threshold rows therefore start with the ball at rest on the deck; the drop row uses a centred ball, whose angular impulse is zero.
- **Stops.** At ±20° the deck's end is 1.2 × sin 20° = 0.41 m lower; its lowest corner is about 0.25 m above the bench, so the joint limit, not the bench, stops it.
- **Constrained.** A Wall under the loaded end blocks the turn by contact. The part has no lock of its own.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction (navigation only, D14).

| Capability | Status | Evidence |
| --- | --- | --- |
| Passive limited hinge on a static base, turned by a resting load | present | `MechanicalContractValues.cs:149`; `ImpactLeverTests.AsymmetricLoad_TurnsTheBeamToItsStopAboutTheAuthoredPivot` (`CuriousContraptions.tests/ImpactLeverTests.cs:165`) |
| Pivot friction holds below its torque and yields above it, in a scene | present | `ImpactLeverTests.ASmallLoadIsHeldBelowTheFrictionTorqueAndTipsTheBeamAboveIt` (`ImpactLeverTests.cs:275`) |
| The same law at engine level, energy-removing only | present | `RevoluteHingeFrictionTests.AHingedBarIsHeldWhileItsGravityTorqueIsBelowTheFrictionTorque` (`CuriousContraptions.tests/RevoluteHingeFrictionTests.cs:77`) |
| Joint limits arrest a hard impact | present | `ImpactLeverTests.AngularLimitsBoundary_StopsArrestTravelWithoutPenetration` (`ImpactLeverTests.cs:380`) |
| Offset hinge frames (centre of mass off the pivot) | present | `HingeFrame` (`MechanicalContractValues.cs:118`); `JointFrameTests.RotatedHingeMovingFrame_ChildOrientationMatchesInverseMovingFrame` (`CuriousContraptions.tests/JointFrameTests.cs:130`) |
| Art follows the hinge body's committed pose | present | `ElementPresentation.cs:114`; `MotorTests.TheRotorArtFollowsTheCommittedRotorPoseAndResetsToItsScenePose` (`CuriousContraptions.tests/MotorTests.cs:1075`) |
| Reset restores the authored hinge state | present | `ImpactLeverTests.RunResetRestoration_RestoresAuthoredStateExactly` (`ImpactLeverTests.cs:484`) |

Nothing is missing, narrower or untested for this part. A free plank resting on a ridge with no joint, the earlier draft's design, is not available: a package's moving bodies are carried by its sliders and hinges (`ElementFootprint`, `ImpactLeverElementDeclaration.cs:37`), and this design does not need it.

## 4. Sources and legacy

- **Requirement row.** [element-096](../requirements.md#element-096): an offset supported load changes equilibrium and causes physical tipping; a balanced or constrained platform does not tip on a timer.
- **Research reference.** The walkthrough cited by [todo-219](../requirements.md#todo-219); intent, not constants.
- **Legacy search.** `git grep -i` at a6c914e for "tipsy", "teeter", "seesaw", "balance" and "tip" found no tipping-platform source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Impact lever: a passive hinged beam; a falling load turns one end; fixed stops limit its swing | `parts/catalog/impact_lever.tres@a6c914e:L9-L12` | Carry forward the passive limited hinge as the mechanism. The lever's balance on its pivot and its tie ports are not carried. |
| L2 | The historical scripted Domino tilt is not a physical authority; show the committed pose | `DESIGN.md@a6c914e:L275-L275` | Carry forward: no scripted tipping |

**Files harvested:** `parts/catalog/impact_lever.tres`.

## 5. Acceptance outline

Requirement row: [element-096](../requirements.md#element-096). Planned test class `TipsyPlatformTests`; θ is the committed deck angle. Angle tolerances follow the Impact lever's delivered tests: 0.01 rad for "held", 0.05 rad at a stop.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a Basketball at rest on the deck 0.30 m off centre, when Run starts, then the deck turns to its stop on that side and the ball leaves the deck | `AnOffsetRestingLoadTipsTheDeckToItsStop` | A2 | θ within 0.05 rad of ±0.349 inside a 5 s guard |
| A2 | Given the same ball 0.20 m off centre, then the deck stays level for the whole Run limit | `ALoadInsideTheThresholdNeverTipsTheDeck` | A1 | max abs θ ≤ 0.01 rad at every tick; this is the "not on a timer" criterion |
| A3 | Given a Basketball dropped from 1 m onto the deck centre, then the deck stays level and the ball rests | `ACentredDroppedLoadLeavesTheDeckLevel` | the same drop 0.6 m off centre tips it | max abs θ ≤ 0.02 rad for the Run limit |
| A4 | Given A1 with a Wall standing under the loaded end, then the deck does not tip | `APropUnderTheLoadedEndPreventsTipping` | A1 | max abs θ ≤ 0.02 rad (the prop's contact clearance) |
| A5 | Given each load at rest, then mass × offset decides: a Tennis ball holds at 0.60 m and tips at 0.90 m; a Bowling ball tips at 0.10 m | `MassTimesOffsetDecidesTipping` (theory) | each row's opposite | each case is at least 18 % from 2.5 N·m |
| A6 | Given a Bowling ball dropped from 2 m onto the deck end, then the deck stops at its limit | `TheStopsArrestAHardImpact` | — | abs θ never above 0.349 + 0.05 rad |
| A7 | Given A1 after the ball has left, then the deck stays tipped | `ATippedDeckStaysTipped` | — | θ within 0.05 rad of the stop for 5 s |
| A8 | Reset returns θ to 0 and the deck to its authored pose; save and load keep the placement | `ResetAndSaveLoadRestoreTheLevelDeck` | — | generic row: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

The part has no ports and no parameters, so wrong-kind connection and parameter refusals do not apply; the conformance suite (S4) covers "no port accepts a connection" generically when it lands. Every row fits the default 30 s Run length.

### Integration

- Loads: Basketball, Tennis ball, Bowling ball, Weight (delivered). Prop: Wall (delivered). No collaborator is undelivered.
- Campaign: chapter 10 "Oddly Satisfying", levels 91–100, "tipsy/unstable platform" (`docs/planning/requirements.md:2411`, chapter row `:2295`); reuse 136–150.
- The thermal interactions of the shared scope index (TH-33, TH-35, TH-36) are proved by those identities.
- A level that fixes the platform in place needs the lesson-fixture story S8; a level that gives it as inventory does not.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a); no further engine story; then S7. S3 preferred.
- **Files.** `elements/TipsyPlatform/` (project, declaration, `tipsy_platform.tscn`, `icon.svg`); `CuriousContraptions.tests/TipsyPlatformTests.cs` (compiled by the test project's one `*.cs` rule); one `CuriousContraptions.slnx` line; the binding section once S7 admits it; until S3, the remaining per-package lists.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's. This part has none of them; its hinge and presentation follow whatever form that story leaves for declared joint frames.
- **Review tier.** Data-only: every capability is present and tested.
- **Disqualifiers the probe re-checks.** The Basketball's delivered mass; a resting ball that creeps on a level deck (then the threshold rows use a Weight); a hinge body whose centre of mass may not sit off the pivot.

## 6. Open questions

All resolved.

1. **Hinge, not a free plank on a ridge.** The free plank needs a second unjointed body in one package, which does not exist; the hinge is delivered and tested. The pivot sits under the deck so the deck is top-heavy and stays tipped. Decided under the standing delegation.
2. **The ridge is part of the element.** One placed part.
3. **`ridge_offset` dropped** (D13, D19): a parameter-bound hinge frame does not exist (ENGINE-PARAMETER-BOUND-FRAMES), and no requirement or lesson asks for a pre-biased platform. A player biases it with a second load.
4. **Friction torque, mass, sizes and travel** are starting values (D1).
5. **Thermal interactions** of the shared scope index belong to TH-33, TH-35 and TH-36 (D16).

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** A static base, one hinge-carried body and one body binding. No solver branch, callback, timer or engine test on this package.
2. **Generic laws.** The revolute joint with Coulomb friction torque and limits; rigid contact and friction between loads and deck. Tipping is the torque balance m·g·x against the declared friction torque.
3. **Three layers.** Colliders: ridge box and cap cylinder on the static body, deck box on the hinge body. Visuals: one mesh per collider and a flat printed mark. Animation: none; `Visual/Deck` follows body 1's pose.
4. **Flow and typing.** No ports, parameters or signals. `RevoluteTravel` is an enum.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 3 (boxes 2, cylinder 1); joints 1 revolute; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 1 (body).
6. **Performance.** Three primitive colliders, fewer than the Impact lever. No sleeping exists today; a held deck costs nothing only once ENGINE-ISLANDS-SLEEP is delivered, which the engine programme schedules ahead of this part (owner decision 3a). The part adds no scene-proportional work.
7. **Tests are scenarios.** Every row runs the production simulation with delivered loads.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `tipsy_platform` |
| Named identity | EL-096, `../invest/named-elements.md#element-096` |
| Acceptance | `todo-219`, `sequence-task-649` (`docs/planning/requirements.md:5165`) |
| Decision | S666, `../invest/scope-corrections.md#s666` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s666` block; `todo-219` is a scope index shared by EL-096 and three thermal identities, and `sequence-task-649` is anchored beside the element row, not beside that todo, which is the pairing the reader expects.
