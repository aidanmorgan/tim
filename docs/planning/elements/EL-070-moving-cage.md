# EL-070 · Moving cage declaration readiness spec

**Current delivery (24 October 2026):** [EL-070 delivery slice](../invest/named-elements.md#element-070). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against the current code (published main `8d1d68aa`); citations without `@a6c914e` are `path:line` in that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-070 |
| Name | Moving cage |
| Type | Mechanical |
| Requirement anchor | [element-070](../requirements.md#element-070); scope index [todo-192](../requirements.md#todo-192) |
| Named entry | [named-elements.md#element-070](../invest/named-elements.md#element-070); proof owner S370 |
| Disposition | To deliver as a single element |
| Planned package | id `moving_cage`, directory `elements/MovingCage/`, class `MovingCageElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | [Section 1b](../invest/delivery-batches.md#1b-runs-each-its-own-slice-planning-reference-only), run "Tensile", open point "a rope-carried dynamic container". **Confirmed** as declaration data with no missing capability, on the condition decided here that the door is set in Build. **Corrected:** two compositions it relies on have no test (section 3), so the slice is engine tier |
| CAT spec refined | None. Related: [CAT-067 weight](CAT-067-weight.md) (the dynamic rope load whose declaration shape this follows), [CAT-004 basket](CAT-004-basket.md), [CAT-058 rope anchor](CAT-058-rope_anchor.md), [CAT-053 pulley](CAT-053-pulley.md) |
| Related identities | [EL-069 moving bucket](EL-069-moving-bucket.md), [EL-065 load hook](EL-065-load-hook.md), [EL-063 cable winch](EL-063-cable-winch.md); character elements EL-082 to EL-088 as later cargo |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-ISLANDS-SLEEP (a hung or standing cage and its cargo) and ENGINE-SPATIAL-QUERIES (contact candidates for 23 children of one moving body); then S7. No engine story of its own |

## 2. Declaration

The declaration follows the Weight, `elements/Weight/WeightElementDeclaration.cs:12`: one dynamic body with scene colliders, a declared mass frame, a rope `Tie` with the Load role, and collider offsets bound to one real parameter. The door is a gate that slides up outside the front face; how far it is raised is the Build-only parameter `door_opening`. There is no hinge, latch or signal port. Local frame: origin at the centre of the base, +Y up, the gate on the +Z face.

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`).
- **Body.** One dynamic body, footprint 1 body and 1 moving, `PayloadPolicy.None`, supported domains Mechanical and Rope.

| # | Colliders | Shape and dimensions (m) | Local pose |
| --- | --- | --- | --- |
| 0 | Floor | Box 1.2 × 0.06 × 1.2 | centre (0, 0.03, 0) |
| 1 | Roof | Box 1.2 × 0.06 × 1.2 | centre (0, 0.97, 0) |
| 2–5 | Corner posts | Box 0.06 × 0.88 × 0.06 | (±0.57, 0.50, ±0.57) |
| 6–14 | Side bars, three on each of the −X, +X and −Z faces | Box 0.05 × 0.88 × 0.05 | on a face at ∓0.575, at −0.3, 0 and +0.3 along it, y 0.50 |
| 15–16 | Gate guides | Box 0.05 × 1.84 × 0.05 | (±0.585, 0.92, 0.63) |
| 17–18 | Gate rails, bottom and top | Box 1.12 × 0.05 × 0.05 | (0, 0.085 + `door_opening`, 0.63) and (0, 0.915 + `door_opening`, 0.63) |
| 19–21 | Gate bars | Box 0.05 × 0.78 × 0.05 | (−0.3 / 0 / +0.3, 0.50 + `door_opening`, 0.63) |
| 22 | Eye | Torus, centreline radius 0.14, solid tube radius 0.03 (bar diameter 0.06), full sweep, upright in the XY plane | centre (0, 1.17, 0) |

23 colliders. Interior clear space 1.08 × 0.88 × 1.08 m. Every gap between bars is 0.25 m, and 0.215 m between a bar and a post.

- **Collider bindings.** Five, one per gate collider (17 to 21): `GeometryComponent.OffsetY` ← 1·`door_opening` + its closed height, in the form of `WeightElementDeclaration.cs:57`–`:76`.
- **Mass frame** (`MassFrameDeclaration.Constant`, as `elements/Ball/BallElementDeclaration.cs:50`): mass 2.5 kg; centre of mass (0, 0.45, 0); principal moments (0.60, 0.75, 0.60) kg·m²; linear drag 0; gravity (0, −9.81, 0). The mass frame is declared, not derived from the colliders, and does not move with the gate: the floor is the heavy member, so the cage hangs level at any opening. Starting values (D1).
- **Material** (restitution, friction, rolling resistance, bounce threshold, `CuriousContraptions.Abstractions/ColliderDescriptor.cs:21`): `new(0.2f, 0.4f, 0f, 0.1f)`.
- **Stores, sensors, events.** None.

| Parameter | Kind | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `door_opening` | Real | Metre | 0 | 0 | 0.9 | Clamp (as the Weight's `mass`, `WeightElementDeclaration.cs:34`) | BuildOnly |

| `PortId` | Role | Domain | Direction | Signal kind | Body | Position (m), normal | Attachment |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `Tie` | `PortRole.Tie` | Rope | Bidirectional | none | 0 | (0, 1.17, 0), +Y: the eye's centre | `RopeAttachmentRole.Load` |

- **Presentation.** Pick radius 0.9 m, no resize axis. One parameter binding: `Visual/Visual_Gate` LocalY ← 1·`door_opening` + 0 (`PresentationProperty.LocalY`, `CuriousContraptions.Abstractions/ElementPresentation.cs:14`; the form of the Clear pipe's collar bindings, `elements/Pipe/PipeElementDeclaration.cs:23`). No run-time driver.
- **Metadata.** Id `moving_cage`, display name "Cage", category `ElementCategory.Motion`, palette order 109 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour pale grey `(0.80, 0.85, 0.875)` `#ccd9df`, description "A barred box that hangs from a rope. Raise its gate in Build to let cargo in or out; lowered, nothing its size escapes."

### Art and solidity

| Node | Mesh and dimensions | Class |
| --- | --- | --- |
| `Visual/Visual_Floor`, `Visual/Visual_Roof` | Boxes 1.2 × 0.06 × 1.2 | Solid, colliders 0 and 1 |
| `Visual/Visual_Post_0` … `3` | Boxes 0.06 × 0.88 × 0.06 | Solid, colliders 2 to 5 |
| `Visual/Visual_Bar_0` … `8` | Boxes 0.05 × 0.88 × 0.05 | Solid, colliders 6 to 14 |
| `Visual/Visual_Guide_0`, `1` | Boxes 0.05 × 1.84 × 0.05 | Solid, colliders 15 and 16 |
| `Visual/Visual_Gate/Rail_0`, `1`, `Visual/Visual_Gate/Bar_0` … `2` | As colliders 17 to 21 | Solid; moved by the same parameter as their colliders, so art and collider agree at every opening. This is a Build-time parameter binding, not a run-time presentation driver, so the "turned by a binding is decorative" allowance is not used |
| `Visual/Visual_Eye` | Torus, inner radius 0.11, outer 0.17 | Hollow class (Torus), collider 22 |

Every bar, rail, post, guide and the eye has a collider (owner rule, visible = solid). No decorative node; the earlier draft's latch tab is withdrawn with the latch. Colours: bars, guides and gate pale grey `#ccd9df` (`DESIGN.md:248`); floor and roof navy `#1c2836`; posts cream `#fff8e9`; eye gold `#f7cb52` (`DESIGN.md:80`). Icon: a barred box with a half-raised gate and a ring on top. **At a glance:** vertical bars and a tall two-post gate frame standing above the roof; the bucket is a round pail with an arch, the Receiver a solid-walled open box.

### Behaviour and numbers

- **Laws.** Rigid-body dynamics, contact and a tension-only rope. Cargo is held by the bars and floor only; no capture force or sensor.
- **Bar gap 0.25 m.** Half the default Tennis ball (0.50 m), the smallest delivered ball at its default, so nothing in the catalogue leaves between bars. A Tennis ball set below 0.125 m radius does pass, as the real geometry dictates. Bars are 0.05 m thick, five times the 1 cm tunnelling bound (`docs/gpu-f32-physics.md:375`).
- **Interior 1.08 × 0.88 × 1.08 m.** Holds a Basketball (0.70 m), a default Weight (0.70 m across, 0.56 m tall) or two Tennis balls.
- **Gate.** Closed at `door_opening` 0: its bottom rail sits on the floor line and its bars close the front face. Raised by h, the clear height under the bottom rail is h, up to the full 0.88 m at the maximum 0.9 m, where the gate stands above the roof between its guides. The clear width is 1.08 m. A partial opening passes only what is lower than it: 0.4 m holds every default ball; 0.6 m releases a Bowling ball (0.56 m) and holds a Basketball.
- **Mass 2.5 kg** (starting value). Empty it is lighter than a 3 kg counterweight and rises; with a Basketball (3.5 kg) it descends. With a Bowling ball it is 6.5 kg, inside the Weight's 8 kg maximum.
- **Eye.** Bar diameter 0.06 m, the handle size agreed for the batch; the ring's 0.22 m opening takes the Load hook. `Tie` is 0.72 m above the centre of mass, so the cage hangs upright.
- **Strand.** The rope on `Tie` becomes solid with ENGINE-CONNECTION-MATERIALS (solid strand, owner decision 1); the delivered rope's missing collider is a recorded defect owned by that story, not a prerequisite here. The slice's probe re-checks that the strand's collider does not snag the eye or the gate guides standing above the roof.
- **Material.** Restitution 0.2 so cargo settles; friction 0.4 so a cage set on a 15° slope stays put against a Wall-like 0.3 surface (pair friction 0.35 against tan 15° = 0.27).
- **Tick order.** No signal, optical or acoustic read.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Dynamic body with many child colliders and a declared mass frame | *present* | `MassFrameDeclaration` `CuriousContraptions.Abstractions/MassFrameValues.cs:38`; `WeightElementDeclaration.cs:96` | `CuriousContraptions.tests/CompoundMassAndRollingTests.cs:19` (many-child support), `:221`; `CuriousContraptions.tests/WeightTests.cs:139` |
| A real parameter bound to a collider offset on a dynamic body, refused when it evaluates out of range | *present* | `ColliderParameterBinding` `SceneGeometry.cs:27`; validation `CuriousContraptions.Bundle/BundleValidator.cs:411`–`:428` | `WeightTests.cs:139` (offset and extents follow the parameter), `:286` |
| Art moved by the same parameter as its collider | *present* | `PresentationProperty.LocalY` `ElementPresentation.cs:14` | `CuriousContraptions.tests/VisibleSolidColliderTests.cs:237`, which compares art with colliders at each bound parameter's minimum and maximum (`:227`–`:233`) |
| Torus collider on a dynamic body | *present* | `ColliderShapeKind` `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`; `elements/Weight/weight.tscn:91` | `CompoundMassAndRollingTests.cs:403` (the Weight's eye), `:114` |
| Rope Load port on a dynamic body; tension-only span | *present* | `RopeAttachmentRole` `CuriousContraptions.Abstractions/PortDescriptor.cs:73` | `CuriousContraptions.tests/RopeTests.cs:21`, `:34`; `WeightTests.cs:1153` |
| Reset and save/load of a rope-hung dynamic body and its parameter | *present* | — | `WeightTests.cs:1275`, `:1309`, `:805` |
| A dynamic sphere carried inside a cage of thin box children whose body hangs from a rope | *present, untested* | composition of the rows above | nearest: `WeightTests.cs:585`, `RopeTests.cs:362` |
| Thin (0.05 m) box children of a **dynamic** body stopping a ball at 8 to 20 m/s | *present, untested* | the tunnelling bound is stated for barriers (`docs/gpu-f32-physics.md:375`) | static only: `CuriousContraptions.tests/WorkshopWallTests.cs:88`; `VisibleSolidColliderTests.cs:1199` |

Counts: 6 present, 2 present-untested, 0 narrower, 0 missing.

**Considered and not used.** A hinged door needs a hinge child on a dynamic root, which the compiler accepts generically (`engine/gpu/WorkshopPhysicsCompiler.cs:245`–`:255`) but no package or test uses; the dynamic-support hinge belongs to ENGINE-JOINT-KINDS (owner decision 3a). A door commanded during a Run needs a hold on its revolute coordinate released by a physical request, or a revolute position drive, both *missing* and both ENGINE-JOINT-KINDS' (holds on a coordinate and their release; engine-stories-index C2; `ENGINE-ASSEMBLY-JOINTS`' commanded release removes only a connection or attachment, C33). Neither is required by the row.

**Element dependencies.** None undelivered: Rope anchor, Pulley, Weight and the balls are delivered.

### Engine conformance

- **Declaration data only.** One dynamic body, one Tie port and one geometry parameter. No hinge, latch, sensor, callback or engine branch.
- **Generic laws.** Holding cargo is contact against bars and floor; escape through the gate is geometry set by `door_opening`; hanging is the tension-only span law.
- **Layers.** Colliders: 22 boxes and 1 Torus on the dynamic body. Visuals: one art box per collider and the eye, none used as a collider. Animation: every art node follows the body pose; the gate's art and colliders follow `door_opening` through Build-time parameter bindings, not a run-time driver. Visible = solid: every bar, rail, post, guide and the eye has its collider; no decorative node (section 2).
- **One-way flow and typing.** Nothing is read back from art. `door_opening` is a typed f32 parameter in metres; port role and domain are enums.
- **Flat state.** 1 dynamic body, 23 colliders, 0 joints, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 5 collider parameter bindings and 1 presentation parameter binding.
- **Performance.** Twenty-three colliders is more than any delivered moving part. The reason is the behaviour: each bar blocks and each 0.25 m gap is real, so wall panels with drawn bars would break visible = solid. The engine documents state no per-body collider budget. If the slice's probe finds the narrowphase cost of many children on one dynamic body unacceptable for several cages, that is an engine gap with its own slice (ENGINE-SPATIAL-QUERIES and ENGINE-SOA-SIMD are the programme's owners of that cost), not a reason for invisible walls. No island sleeping exists today; a resting cage costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows C1 to C10 run the production simulation with delivered parts.

## 4. Sources and legacy

- Requirements row: "An open door allows escape through actual geometry." No variants.

No legacy cage, door or latch-on-hinge exists. Searched `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: cage, door, bars, enclosure). Shared facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Hinge travel bounds must be finite, ordered and strictly inside (−π, π). | `engine/physics/PhysicsJoint.cs@a6c914e:L104-L113`, `engine/physics/PhysicsJoint.cs@a6c914e:L137-L139` | carry forward | Door travel admission. |
| 2 | A hanging load uses one rope `Tie` with role `Load`. | `parts/WeightPart.cs@a6c914e:L16-L21` | carry forward | Cage suspension. |

**Files harvested:** `engine/physics/PhysicsJoint.cs`, `parts/WeightPart.cs`.

## 5. Acceptance outline

Requirement row: [element-070](../requirements.md#element-070): "Rigid barred enclosure carries cargo through a declared closable opening. An open door allows escape through actual geometry." Fact 1 no longer applies: the gate slides and has no hinge.

- **Chrome UI recipe (pending, post-catalogue).** Hang a Cage with a Basketball inside from a Pulley against a Weight; set `door_opening` to 0 and run; set it to 0.9, stand the cage on a tilted Wall and run. Verify placement, the gate setting and the rope route through real UI controls.

All tests are planned, in `CuriousContraptions.tests/MovingCageTests.cs` unless named. "S4 row n" is the planned conformance theory; until it is delivered the named package test is written.

| # | Given / when / then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| C1 | **Carries cargo.** Given a closed cage holding a Basketball, hung over a Pulley against a 3 kg Weight, when Run advances, then the cage descends at least 1 m and the ball is inside throughout | `MovingCageTests.AClosedCageCarriesItsCargoAndItsWeightLoadsTheRope` | The empty cage (2.5 kg) rises at least 1 m; a ball placed on the bench outside is not moved | — | "Inside" is the ball centre within the interior box shrunk by the ball radius, plus 0.01 m. Stall guard 10 s. Direction, not speed, is asserted |
| C2 | **An open door allows escape through actual geometry.** Given a cage standing on a panel tilted 15° toward its gate with a Bowling ball inside and `door_opening` 0.9, then the ball rolls out under the gate and its centre passes through the gate opening, never through a bar, post or rail | `MovingCageTests.ARaisedGateLetsTheBallOutThroughTheOpening` | `door_opening` 0: the ball is still inside after 5 s | `door_opening` 0.4: the Bowling ball (0.56 m) stays in; 0.6: it leaves and a Basketball (0.70 m) stays in | Bar crossing judged at 0.01 m. Stall guard 10 s |
| C3 | **Bars are real.** Given a default Tennis ball inside a closed, swinging cage, it never leaves; given a Tennis ball of radius 0.10 m, it leaves between two bars | `MovingCageTests.OnlyCargoNarrowerThanTheBarGapLeavesBetweenBars` | — | Radius 0.15 m (0.30 m across) stays in | Stall guard 10 s |
| C4 | Given a cage standing on the bench and a Bowling ball launched at 8 m/s at a barred side from outside, then the ball's centre, in the cage's frame, is never inside the interior box. The free 2.5 kg cage is shoved by the 4 kg ball; that is expected | `MovingCageTests.AFastBallStrikingTheBarsNeverEnters` | The same launch at the open gate enters | 20 m/s | Judged in the cage's frame with the 0.01 m allowance; no rebound speed or cage displacement is asserted |
| C5 | Hung from a Rope anchor by `Tie`, the cage settles within 3° of level at `door_opening` 0 and 0.9 | `MovingCageTests.ItHangsLevelFromItsEyeAtEitherGateSetting` | Unconnected, it falls | — | Settled means speed under 0.05 m/s for 1 s; stall guard 20 s |
| C6 | Every bar, rail, post, guide and the eye stops a probe and the gap beside it is empty, at `door_opening` 0 and 0.9; every art node is classified once | `MovingCageArtTests.EveryDrawnBarStopsAProbeAtBothGateSettings` (pattern `CompoundMassAndRollingTests.cs:403`); one entry beside `VisibleSolidColliderTests.cs:141` | The gap 0.01 m clear of each surface; the opening under a raised gate is empty | Both parameter limits | As that pattern |
| C7 | `door_opening` below 0, above 0.9 or non-finite is refused atomically at save and wire decode; the editor clamps | S4 row 2, else `MovingCageTests.AnInvalidGateOpeningIsRefusedAndChangesNothing` (pattern `WeightTests.cs:96`) | 0 and 0.9 are admitted | The next f32 beyond each limit | Exact typed refusal |
| C8 | Run, Reset and save/load restore the cage, its gate setting, the rope and the cargo | S4 rows 1 and 3, else `MovingCageTests.ResetAndSaveLoadRestoreTheCageItsGateAndCargo` (pattern `WeightTests.cs:805`, `RopeTests.cs:905`) | — | — | Authored state exact |
| C9 | A connection from `Tie` to a non-rope port is refused | S4 row 5, else `MovingCageTests.ItsRopePortRefusesAWrongKindConnection` | A rope to a Rope anchor is accepted | — | Exact typed refusal |
| C10 | **Load hook (collaborator, with whichever of EL-065 and this part is delivered second).** A hook through the eye lifts the cage | `LoadHookTests.AHookLiftsACageByItsEye` (owned by that slice) | A hook that misses lifts nothing | — | — |

## 6. Open questions

**Resolved**

1. Door authored in Build or commanded in a Run: **Build only (D19; confirmed as a coordinator decision under the standing delegation, 22 November 2026).** This reads the row's "closable opening" as closable at Build time. The row asks for a "declared closable opening" and for an open door to let cargo out through real geometry; a Build-set gate gives both, and no requirement row or campaign lesson uses a commanded door. The draft's `ActivationIn`, latch and holding torque are withdrawn. If a lesson later needs a door that opens during a Run, that is a new slice on this package after ENGINE-JOINT-KINDS (or the commanded release `ENGINE-ASSEMBLY-JOINTS` keeps), not an obligation of this one.
2. Hinged door or sliding gate: **sliding gate, decided now.** A real parameter bound to a collider offset is delivered and tested on a dynamic body; a hinge child on a dynamic root is untested and a latch is missing. A continuous opening also gives a size-selective exit for free.
3. Collider admission (the old 64-row limit): **no limit.** Scene tables are sized from the compiled scene (`CuriousContraptions.tests/StraightPipeTests.cs:146`); the cage's 23 colliders need no special admission.
4. May the cage stand on the bench as a static trap: **it may stand on the bench (D19, the draft's proposal), as a free dynamic body.** It is not static; rows C2 and C4 use it standing.
5. `door_state` enum: **replaced** by the real `door_opening`; no new enum.
6. Does the centre of mass follow the gate: **no, decided now.** A level-hanging cage at every setting is the plausible and readable behaviour; the declared mass frame is constant.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Delivered: Rope anchor, Pulley, Weight, Basketball, Bowling ball, Tennis ball, Wall, Ramp. Not delivered: EL-063 Cable winch (lifts by `Tie`), EL-065 Load hook (engages the eye), EL-069 Moving bucket (sibling, no dependency), the character elements EL-082 to EL-088.
- **Campaign.** Bucket/cage first use levels 11–20, moving and load variants 41–50, reuse 31–50, 61–80 and 136–150 (`docs/planning/invest/campaign-reservations.md:44`). This slice authors no level; a level that places the cage before it is playable needs the lesson-fixture story (S8).

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `moving_cage` |
| Named identity | EL-070, `../invest/named-elements.md#element-070` |
| Acceptance | `todo-192` and `sequence-task-623` |
| Decision | S370; `scope-corrections.md` has no `s370` block |
| Mode records | `Configuration=Adjustable` (it declares a parameter; confirmed at slice planning) |

What it lacks today: an `s370` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-192 anchor is a scope index, not immediately followed by its sequence-task-623 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element slice, after EL-069.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-ISLANDS-SLEEP and ENGINE-SPATIAL-QUERIES; then S7 (the coverage follow-on, which widens the reader, owner decision 15); EL-069 is recommended first (its slice proves cargo carried in a rope-hung container) but is not required.
- **Files added.** `elements/MovingCage/MovingCage.csproj`, `MovingCageElementDeclaration.cs`, `moving_cage.tscn`, `icon.svg`; `CuriousContraptions.tests/MovingCageTests.cs`, `MovingCageArtTests.cs`.
- **Files edited.** One entry beside `VisibleSolidColliderTests.cs:141`; until S3, `CuriousContraptions.slnx`, the trim list at `CSharpStaticGateTests.cs:687`–`:703` and the parameter and placement switches at `PackageRuntimeParityTests.cs:483` and `:641` (generic list `:653`); the binding section of section 7; this spec and `TODO.md`.
- **Review tier.** Engine tier: two capabilities are *present, untested*.
- **Disqualifiers (the slice's probe re-checks).** A parameter minimum of 0 is admitted for an offset binding (the validator rejects only non-positive extents and radii, `BundleValidator.cs:424`–`:427`); 23 colliders on one dynamic body stay stable under a resting ball; the cage is a rope Load, never a Guide, so ENGINE-TENSILE-EXTENSIONS (dynamic-guide clause) is not needed (if the probe shows it is, that story comes first); thin dynamic bars stop a 20 m/s ball (if not, the repair is a generic contact slice first); the requirement is still read as satisfied by a Build-set gate; once the strand is solid, its collider snagging the eye or the gate guides.
