# EL-069 · Moving bucket declaration readiness spec

**Current delivery (24 October 2026):** [EL-069 delivery slice](../invest/named-elements.md#element-069). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against the current code (published main `8d1d68aa`); citations without `@a6c914e` are `path:line` in that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-069 |
| Name | Moving bucket |
| Type | Mechanical |
| Requirement anchor | [element-069](../requirements.md#element-069); scope index [todo-192](../requirements.md#todo-192) |
| Named entry | [named-elements.md#element-069](../invest/named-elements.md#element-069); proof owner S369. The entry also retains [CAT-004 acceptance](CAT-004-basket.md); read as the Receiver keeping its own acceptance: the bucket has no capture sensor (coordinator decision, section 6) |
| Disposition | To deliver as a single element. Not a duplicate of the Receiver: it is dynamic, rope-carried, round and has two rope ports |
| Planned package | id `moving_bucket`, directory `elements/MovingBucket/`, class `MovingBucketElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | [Section 1b](../invest/delivery-batches.md#1b-runs-each-its-own-slice-planning-reference-only), run "Tensile", open point "a rope-carried dynamic container". **Confirmed** as declaration data with no missing capability. **Corrected:** three compositions it relies on have no test (section 3), so the slice is engine tier |
| CAT spec related | [CAT-004 basket](CAT-004-basket.md) (static open container, `elements/Receiver/`); [CAT-067 weight](CAT-067-weight.md) (the dynamic rope load whose declaration shape this follows); [CAT-058 rope anchor](CAT-058-rope_anchor.md); [CAT-053 pulley](CAT-053-pulley.md) |
| Related identities | [EL-070 moving cage](EL-070-moving-cage.md), [EL-065 load hook](EL-065-load-hook.md), [EL-063 cable winch](EL-063-cable-winch.md); [EL-014 water-carrying bucket](EL-014-water-carrying-bucket.md), a separate identity with its own package |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-ISLANDS-SLEEP (a hung or resting bucket and its cargo) and ENGINE-SPATIAL-QUERIES (hollow-bore contact candidates on a moving body); then S7. No engine story of its own |

## 2. Declaration

The declaration follows the Weight, `elements/Weight/WeightElementDeclaration.cs:12`: one dynamic body with several scene colliders, a declared mass frame and a rope `Tie` with the Load role. Local frame: origin at the centre of the base, +Y up the axis.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). No parameterised geometry.
- **Body.** One dynamic body (`KinematicMode.Dynamic`), footprint 1 body and 1 moving, `PayloadPolicy.None`, supported domains Mechanical and Rope (`WeightElementDeclaration.cs:37`, `:83`–`:93`).

| # | Collider | Shape and dimensions (m) | Local pose |
| --- | --- | --- | --- |
| 0 | Floor | Cylinder, radius 0.65, half-height 0.03 | centre (0, 0.03, 0), axis +Y |
| 1 | Shell | Frustum, outer radius 0.65 at the bottom and 0.85 at the rim, wall 0.05, half-height 0.35 (bore radius 0.60 to 0.80, depth 0.70) | centre (0, 0.41, 0), axis +Y |
| 2 | Rim | Torus, centreline radius 0.825, solid tube radius 0.03, full sweep | centre (0, 0.76, 0), axis +Y |
| 3 | Bail | Torus, centreline radius 0.825, solid tube radius 0.03 (bar diameter 0.06), sweep 180° | centre (0, 0.76, 0), in the XY plane, feet on the rim at x ±0.825, apex centreline at (0, 1.585, 0) |
| 4 | Tip lug | Box 0.08 × 0.06 × 0.08 | centre (0, 0.03, 0.69) |

Hollow kinds on a dynamic body are admitted with an explicit mass frame (section 3). The coordinator's premise that they are static only is corrected; a box-walled bucket is not needed.

- **Mass frame** (`MassFrameDeclaration.Constant`, as `elements/Ball/BallElementDeclaration.cs:50`): mass 1.5 kg; centre of mass (0, 0.40, 0); principal moments (0.50, 0.68, 0.50) kg·m² in the local frame; linear drag 0; gravity (0, −9.81, 0). Collision geometry contributes no mass. The moments come from a thin-shell split (floor 0.4 kg, shell 0.9 kg, rim and bail 0.2 kg); they are starting values (D1) and the slice records the working in its run log.
- **Material** (restitution, friction, rolling resistance, bounce threshold, `CuriousContraptions.Abstractions/ColliderDescriptor.cs:21`): `new(0.2f, 0.5f, 0.03f, 0.1f)`.
- **Parameters.** None. Fixed size (D13).
- **Stores, sensors, events.** None.

| `PortId` | Role | Domain | Direction | Signal kind | Body | Position (m), normal | Attachment |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `Tie` | `PortRole.TieA` | Rope | Bidirectional | none | 0 | (0, 1.585, 0), +Y: the bail apex | `RopeAttachmentRole.Load` |
| `TipTie` | `PortRole.TieB` | Rope | Bidirectional | none | 0 | (0, 0.03, 0.73), +Z: the outer face of the tip lug | `RopeAttachmentRole.Load` |

`TipTie` is a package-minted `PortId` (as the Linear pusher mints its own, `elements/LinearPusher/LinearPusherElementDeclaration.cs:44`) carrying an existing role; no new role or domain. Two rope ports on one package take `TieA` and `TieB`, as the Impact lever does (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:45`, `:56`).

- **Presentation.** Pick radius 0.95 m, no resize axis, no bindings. The art is rigid with the body.
- **Metadata.** Id `moving_bucket`, display name "Hanging bucket" (coordinator decision 36: EL-014 is the "Water bucket", so no two Motion palette rows share a name), category `ElementCategory.Motion` (as the Weight and Rope anchor), palette order 108 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour `(0.29, 0.67, 0.58)` `#4aab94` (the Basket's, `DESIGN.md:229`, as the container cue), description "A pail that hangs from a rope by its bail. It carries what falls into it and spills when it tips."

### Art and solidity

| Node | Mesh and dimensions | Class |
| --- | --- | --- |
| `Visual/Visual_Floor` | Cylinder, radius 0.65, height 0.06 | Solid, collider 0 |
| `Visual/Visual_Shell` | Uncapped cone frustum, radius 0.65 to 0.85, height 0.70, drawn both sides | Hollow, collider 1 (Frustum, wall 0.05) |
| `Visual/Visual_Rim` | Torus, inner radius 0.795, outer 0.855 | Hollow class (Torus), collider 2 |
| `Visual/Visual_Bail` | Half torus, same section, standing on the rim | Hollow class (Torus segment), collider 3 |
| `Visual/Visual_Lug` | Box 0.08 × 0.06 × 0.08 | Solid, collider 4 |

Every drawn part is solid: rim, bail and lug each have a collider (owner rule, visible = solid). There is no decorative node; the earlier draft's cosmetic bail and hoops are withdrawn. Colours: shell and floor cream `#fff8e9`, rim ochre `#d69c47` (rope family, `DESIGN.md:246`), bail pale grey `#ccd9df` (`DESIGN.md:248`), lug gold `#f7cb52` (`DESIGN.md:80`). Icon: a flared pail with an arched bail. **At a glance:** round and flared with one arch over the opening; the Receiver is a square box with a halo and the cage is a barred box with a gate frame.

### Behaviour and numbers

- **Laws.** Rigid-body dynamics, contact (product restitution, geometric-mean friction, `docs/gpu-f32-physics.md:147`) and tension-only rope spans. Cargo is carried only by contact with the floor and wall, so its weight loads the rope through the bucket. No capture force, no guide, no sensor.
- **Rim bore 0.80 m radius, base bore 0.60 m, depth 0.70 m.** Chosen from the bail, not the cargo alone: the bail bar crosses the opening, so a ball falling straight in must fit in the 0.77 m between the bar and the rim. That admits the Basketball (0.70 m across with its band, `elements/Ball/ball.tscn:18`) with 0.07 m to spare, the Bowling ball (0.56 m) and the default Tennis ball (0.50 m). The earlier draft's 0.8 m square bucket predates the solid bail. A 0.70 m depth puts a resting Basketball wholly below the rim. The bail crosses the opening, so a Clear pipe or Funnel must discharge 0.41 m to the side of the bail, not over the axis.
- **Bail.** Bar diameter 0.06 m, the handle size agreed for the batch so the Load hook's 0.12 m throat takes it. Clearance under the apex is 0.795 m.
- **Mass 1.5 kg** (starting value). Lighter than a 4 kg Bowling ball so cargo dominates, heavier than a Basketball (1 kg, `BallElementDeclaration.cs:51`) so an empty bucket does not flutter. With a 3 kg counterweight the empty bucket rises and the bucket with a Bowling ball (5.5 kg) descends; every loaded mass stays inside the Weight's 0.25 to 8 kg range (`WeightElementDeclaration.cs:33`).
- **Hanging.** `Tie` is 1.185 m above the centre of mass, so the bucket hangs upright.
- **Tipping.** A rope from a fixed anchor to `TipTie` holds the base edge while the main rope pays out. Hanging from the lug, the bucket's axis is about 117° from upright (the lug is 0.73 m off axis and 0.37 m below the centre of mass). Cargo leaves once the axis passes 74° (90° less the wall's 16° flare), through the rim, beside the bail. Tipping needs the main rope near slack: against K1's 3 kg counterweight the main rope still carries the bucket, and a bucket holding only a Basketball (2.5 kg) rises instead of descending, so the tip rope never goes taut. Row K2 therefore uses a 0.25 kg counterweight (main-rope tension about 2·5.5·0.25/5.75·g = 4.7 N with a Bowling ball aboard).
- **Material.** Restitution 0.2 so a ball dropped in settles (0.11 of its approach speed for a Basketball); friction 0.5 so cargo does not skate as the bucket swings; rolling resistance 0.03, the Weight's value (`WeightElementDeclaration.cs:52`), so a bucket knocked onto its side comes to rest.
- **Liquid.** None in this identity. Carrying water is [EL-014](EL-014-water-carrying-bucket.md), its own package after `ENGINE-LIQUID` (D8).
- **Strands.** The ropes on `Tie` and `TipTie` become solid with ENGINE-CONNECTION-MATERIALS (solid strand, owner decision 1); the delivered rope's missing collider is a recorded defect owned by that story, not a prerequisite here. The slice's probe re-checks that a strand's collider does not snag the bail or the tip lug.
- **Tick order.** No signal, optical or acoustic read.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Dynamic body with several child colliders and a declared mass frame | *present* | `MassFrameDeclaration` `CuriousContraptions.Abstractions/MassFrameValues.cs:38`; `WeightElementDeclaration.cs:96` | `CuriousContraptions.tests/WeightTests.cs:139`; `CuriousContraptions.tests/CompoundMassAndRollingTests.cs:221`, `:19` |
| Tube, Torus and Frustum colliders on a dynamic body | *present* | `ColliderShapeKind` `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12` (its comment at `:9`–`:11` says static or dynamic bodies, corrected in `dbaacad1`); dynamic hollow support `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:161` | `CuriousContraptions.tests/WorkshopPhysicsLawHollowTests.cs:893` (needs an explicit mass frame), `:1338`, `:1371`; `CompoundMassAndRollingTests.cs:114` (hollow child of a compound body); delivered: the Weight's eye, `elements/Weight/weight.tscn:91`, tested at `CompoundMassAndRollingTests.cs:403` |
| A sphere enters a frustum bore and rests inside its wall | *present* | hollow narrowphase `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:1996` | `CuriousContraptions.tests/HollowElementTests.cs:437`; `WorkshopPhysicsLawHollowTests.cs:526` |
| Rope Load port on a dynamic body; tension-only span | *present* | `RopeAttachmentRole` `CuriousContraptions.Abstractions/PortDescriptor.cs:73`; `TensileSpanDeclaration` `CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:25` | `CuriousContraptions.tests/RopeTests.cs:21`, `:34`; `WeightTests.cs:1153` (counterweight over a pulley) |
| Per-package material | *present* | `SceneBodyGeometry` `SceneGeometry.cs:8` | `WeightTests.cs:139` (compiled material row) |
| Reset and save/load of a rope-hung dynamic body | *present* | — | `WeightTests.cs:1275`, `:1309`; `RopeTests.cs:905` |
| A Frustum extracted from a package scene on a **dynamic** body | *present, untested* | scene path as `elements/Funnel/funnel.tscn:88` (static); the only delivered dynamic hollow child is a Torus | none at package level |
| A dynamic sphere carried inside a moving hollow bore whose body hangs from a rope (cargo weight loading the span) | *present, untested* | composition of the rows above | nearest: `WeightTests.cs:585` (a stack on the bench), `RopeTests.cs:362` |
| Two rope ports on one free dynamic root body | *present, untested* | two Load ports on one body are declared by the Impact lever, `elements/ImpactLever/ImpactLeverElementDeclaration.cs:41`–`:56`, on a hinged body | `CuriousContraptions.tests/ImpactLeverTests.cs:414` exercises one of them on a hinged body only |

Counts: 6 present, 3 present-untested, 0 narrower, 0 missing. Not used and therefore not probed further: a capture sensor in a moving frame (open, delivery-batches 2c under EL-192) and any liquid capability.

**Element dependencies.** None undelivered: Rope anchor, Pulley, Weight and the balls are delivered. EL-063 Cable winch and EL-065 Load hook are optional collaborators delivered later.

### Engine conformance

- **Declaration data only.** One dynamic body with five colliders and two Tie ports. No capture force, sensor, guide, callback or engine branch.
- **Generic laws.** Carrying is contact inside a hollow bore; cargo weight reaches the rope through the body; tipping is rigid-body dynamics under two tension-only spans; spilling is geometry.
- **Layers.** Colliders: 1 Cylinder (floor), 1 Frustum (shell), 2 Torus (rim, bail), 1 Box (lug). Visuals: the art table, none used as a collider. Animation: all five art nodes follow the body pose; no presentation binding. Visible = solid: every drawn part has its collider and there is no decorative node (section 2).
- **One-way flow and typing.** Nothing is read back from art. Port roles, domain and attachment role are enums; values are f32.
- **Flat state.** 1 dynamic body, 5 colliders, 0 joints, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 0 bindings.
- **Performance.** Five primitives in place of the box-walled bucket's larger set; none is cosmetic. No island sleeping exists today; a resting bucket costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows K1 to K10 run the production simulation with delivered parts.

## 4. Sources and legacy

- Requirements row: "Tipping spills through the real opening; a missed object is not captured." No variants.
- Research recipe "Catch the Escape: rope lifts/tilts a bucket into a movable funnel; mistimed pours visibly spill" (`docs/component-research.md@a6c914e:L62-L62`).

No legacy bucket. Shared facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | The Receiver is five static boxes forming an open box 1.5 m wide (floor at y −0.45, walls 0.06 m half-thick, one lower front wall). | `engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L18` | carry forward (geometry pattern) | Same open-container construction, made dynamic and smaller. |
| 2 | Loads hang from a rope `Tie` with role `Load`, one end. | `parts/WeightPart.cs@a6c914e:L16-L21` | carry forward | Bail attachment. |

**Files harvested:** `parts/WeightPart.cs`; current `engine/gpu/ReceiverGeometry.cs` (survives). `parts/BasketPart.cs` checked (static basket, harvested by CAT-004). Searched for bucket, pail: only unrelated "request buckets" (`engine/SimulationLatches.cs@a6c914e:L32-L32`).

## 5. Acceptance outline

Requirement row: [element-069](../requirements.md#element-069). Fact 1's box pattern is superseded by the round shell (section 2); its "smaller" is superseded by the bail clearance.

- **Chrome UI recipe (pending, post-catalogue).** Hang a Hanging bucket from a Pulley with a Weight as counterweight; rope a Rope anchor to the bucket's `TipTie`; roll a Bowling ball off a Ramp into the bucket and aim a second ball to miss. Verify placement and both rope routes through real UI controls.

All tests are planned, in `CuriousContraptions.tests/MovingBucketTests.cs` unless named. "S4 row n" is the planned conformance theory; until it is delivered the named package test is written.

| # | Given / when / then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| K1 | **Carries cargo with its mass.** Given a bucket hung over a Pulley against a 3 kg Weight and a Bowling ball dropped in beside the bail, when Run advances, then the ball comes to rest inside and the bucket descends at least 1 m with the ball still inside | `MovingBucketTests.ACapturedBallLoadsTheRopeAndRidesDownInside` | The empty bucket (1.5 kg) rises at least 1 m against the same Weight | A default Tennis ball (0.35 kg): the bucket still rises, with the ball inside | "Inside" is the ball centre below the rim plane and within the bore radius at its height plus 0.01 m. Stall guard 10 s. Direction of travel, not speed, is asserted |
| K2 | **Tipping spills through the real opening.** Given a bucket hung over a Pulley against a 0.25 kg Weight, holding a Bowling ball, with a rope from an anchor to `TipTie` that goes taut after 1 m of descent, then the bucket's axis passes 74° from upright, the ball leaves through the rim and lands on the bench, and its centre never crosses the shell or the floor | `MovingBucketTests.ATautTipRopeInvertsTheBucketAndTheBallLeavesThroughTheRim` | No tip rope: the bucket reaches the bench upright with the ball inside | The Basketball, the largest delivered ball, also leaves (bucket and ball, 2.5 kg, still descend against 0.25 kg) | Wall crossing is judged with the 0.01 m allowance of `docs/gpu-f32-physics.md:375`. Stall guard 15 s. No tick or angle history is asserted |
| K3 | **A missed object is not captured.** Given a Bowling ball dropped 1.4 m from the axis (0.265 m clear of the 0.855 m rim), then it lands on the bench and the empty bucket still rises | `MovingBucketTests.ABallThatMissesTheRimIsNotCarried` | A ball dropped 0.42 m from the axis, beside the bail, is carried (K1) | — | The missed ball ends with its centre within 0.01 m of radius above the bench top. Stall guard 10 s |
| K4 | **Real opening and solid bail.** Given a Basketball, Bowling ball and Tennis ball each dropped 0.42 m to the side of the bail, each ends inside; given an 8 kg Weight (1.09 m across) dropped over the opening, it does not enter | `MovingBucketTests.EachDeliveredBallEntersBesideTheBailAndAnOversizedLoadDoesNot` | — | A Bowling ball dropped onto the bail apex is deflected and never passes through the bar | Solid crossing judged at 0.01 m. Stall guard 8 s |
| K5 | Given a bucket hung from a Rope anchor by `Tie`, then it settles with its axis within 3° of vertical | `MovingBucketTests.ItHangsUprightFromItsBail` | Unconnected, it falls to the bench | — | 3° is far inside the 74° spill angle; settled means speed under 0.05 m/s for 1 s. Stall guard 20 s (a light pendulum damps slowly) |
| K6 | Set down on the bench it rests on its base without drift; laid on its side it rolls and comes to rest | `MovingBucketTests.ItRestsOnItsBaseAndComesToRestOnItsSide` (pattern `WeightTests.cs:268`) | — | — | Resting drift under 0.01 m over 3 s; rest on its side within 20 s |
| K7 | Each drawn solid (floor, shell inside and outside, rim, bail, lug) stops a probe and the space beside it is empty; every art node is classified once | `MovingBucketArtTests.EveryDrawnSolidStopsAProbeAndAdjacentSpaceIsEmpty` (pattern `CompoundMassAndRollingTests.cs:403`); one entry beside `CuriousContraptions.tests/VisibleSolidColliderTests.cs:141` | The gap 0.01 m clear of each surface | — | As that pattern |
| K8 | Run, Reset and save/load restore the bucket, both rope routes and the cargo | S4 rows 1 and 3, else `MovingBucketTests.ResetAndSaveLoadRestoreTheBucketItsRopesAndCargo` (pattern `RopeTests.cs:905`) | — | — | Authored state exact |
| K9 | A connection from `Tie` or `TipTie` to a non-rope port is refused; a submitted parameter is refused (the package has none) | S4 row 5, else `MovingBucketTests.ItsRopePortsRefuseAWrongKindConnection` | A rope to a Rope anchor is accepted | — | Exact typed refusal |
| K10 | **Load hook (collaborator, with whichever of EL-065 and this part is delivered second).** A hook lowered onto the bail engages it by contact and lifts the bucket | `LoadHookTests.AHookLiftsABucketByItsBail` (owned by that slice) | A hook that misses the bail lifts nothing | — | — |

## 6. Open questions

**Resolved**

1. Liquid mode: **D8, none here.** EL-014 is its own identity and package; the capability-map note that this row "must add FluidAdvection" is navigation only (D14). The contradiction is reported in the batch summary.
2. May the bucket rest on the bench, not hung: **yes (D19, the draft's proposal).** It is a free dynamic body; row K6 covers it.
3. Fixed size or resizable: **D13, fixed.**
4. Second rope port `TipTie`: **kept (D19).** The requirement's own outcome is tipping, and a base rope is the one deterministic way to tip a bucket with a rigid bail. It moves from the rim to the base edge: a rim lug can turn the bucket only 45° (the angle from the bail apex to the rim), short of the 74° needed to spill.
5. Contents read for goals: **none declared (D19).** No requirement or lesson uses it, and a capture sensor in a moving frame is an open capability.
6. Bail: **solid, decided now** under the owner's visible = solid rule; it is rigid with the body, so no joint on a dynamic root is needed.
7. Round shell instead of five boxes: **decided now.** Hollow kinds are admitted on dynamic bodies (section 3), a pail is recognisable where a second square box beside the Receiver is not, and a frustum wall spills cleanly.

8. Goal capture: **none; coordinator decision under the standing delegation (22 November 2026).** The ledger's "also retains CAT-004 acceptance" is read as the Receiver keeping its own acceptance. The bucket declares no capture sensor: goal capture belongs to ENGINE-GOALS and its five slices (owner decision 12), not to a container package. The proposed ledger sentence is in the batch summary.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Delivered: Rope anchor, Pulley, Weight, Basketball, Bowling ball, Tennis ball, Ramp, Clear pipe and Funnel (discharging 0.41 m to the side of the bail). Not delivered: EL-063 Cable winch (lifts by `Tie`), EL-065 Load hook (engages the 0.06 m bail), EL-064 Metal loop anchor.
- **Campaign.** Bucket/cage first use levels 11–20, moving and load variants 41–50, reuse 31–50, 61–80 and 136–150 (`docs/planning/invest/campaign-reservations.md:44`). This slice authors no level; a level that places the bucket before it is playable needs the lesson-fixture story (S8).

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `moving_bucket` |
| Named identity | EL-069, `../invest/named-elements.md#element-069` |
| Acceptance | `todo-192` and `sequence-task-622` |
| Decision | S369; `scope-corrections.md` has no `s369` block |
| Mode records | `Configuration=Fixed` |

What it lacks today: an `s369` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-192 anchor is a scope index, not immediately followed by its sequence-task-622 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element slice. Deliver it before EL-070, which reuses its probe results for a rope-hung container.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-ISLANDS-SLEEP and ENGINE-SPATIAL-QUERIES; then S7 (the coverage follow-on, which widens the reader, owner decision 15). No engine story of its own and no other element.
- **Files added.** `elements/MovingBucket/MovingBucket.csproj`, `MovingBucketElementDeclaration.cs`, `moving_bucket.tscn`, `icon.svg`; `CuriousContraptions.tests/MovingBucketTests.cs`, `MovingBucketArtTests.cs`.
- **Files edited.** One entry beside `VisibleSolidColliderTests.cs:141`; until S3, `CuriousContraptions.slnx`, the trim list at `CSharpStaticGateTests.cs:687`–`:703` and the placement switch at `PackageRuntimeParityTests.cs:641` (generic list `:653`); the binding section of section 7; this spec and `TODO.md`. The comments at `PhysicsDeclarationValues.cs:9`–`:11` and `CuriousContraptions.Simulation/WorkshopCollisionKernels.cs:1996` already place hollow colliders on static or dynamic bodies (corrected in `dbaacad1`), so this slice does not edit them.
- **Review tier.** Engine tier: three capabilities are *present, untested*.
- **Disqualifiers (the slice's probe re-checks).** A Frustum on a dynamic package body extracts and compiles; a ball resting in the moving bore stays put while the bucket swings (if it does not, the defect is in the generic hollow contact law and is its own engine slice first); a package-minted rope `PortId` with role `TieB` is admitted on a free dynamic body (otherwise the delivered `PortId.TieB` is used); rolling resistance applies to a frustum rolling on its side; the bucket is a rope Load, never a Guide, so the tensile reaction on a dynamic guide body (ENGINE-TENSILE-EXTENSIONS, dynamic-guide clause) is not needed; if the probe shows it is, that story comes first; once the strands are solid, a strand's collider snagging the bail or the lug.
