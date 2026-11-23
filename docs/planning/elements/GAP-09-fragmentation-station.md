# GAP-09 · Fragmentation station — named-identity readiness spec

**Current delivery (24 October 2026):** [GAP-09 delivery slice](../invest/named-elements.md#gap-09). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | GAP-09 · Fragmentation station · Mechanical (P3 potential; adopted for implementation by the 24 October 2026 owner decision) |
| Anchor | [requirements.md#gap-09](../requirements.md#gap-09) (task `sequence-task-542`); [named-elements entry](../invest/named-elements.md#gap-09); decision block [S641](../invest/scope-corrections.md#s641) |
| Owner key | S641 |
| Related identities | [EL-159](EL-159-tension-limited-connector.md), [EL-160](EL-160-shear-limited-connector.md), [EL-161](EL-161-bending-limited-connector.md) (the same fracture story, on connections); [EL-118](EL-118-coating-station.md) and [EL-119](EL-119-dye-station.md) (fragments keep treated state); [EL-084 Fragile fish bowl](EL-084-fragile-fish-bowl.md); delivered Linear pusher (the press pattern) and Balloon (the integrity observation). No CAT refines it. |
| Status | Not started; blocked on the engine programme (owner decision 3a), in particular `ENGINE-CONTACT-OBSERVATIONS`, and on `ENGINE-FRACTURE` (section 3). |

### Disposition

**To deliver as a single element with one declared companion package**, in one slice after the engine programme (owner decision 3a) and one engine story. Planned packages: `fragmentation_station` (directory `elements/FragmentationStation/`, display name "Press") and its companion `breakable_block` (`elements/BreakableBlock/`, "Breakable block"), which is declared not placeable (owner decision 24b, 23 November 2026). One identity may own a main package and declared companions, with no new ledger identity (owner decision, 22 November 2026, item 15).

**The station breaks nothing by rule.** It is a supplied press: a head on a slider that pushes down with a bounded force. The block breaks because it declares a load limit and fragments, and any sufficient sustained load breaks it, whatever applies it. That is the element-free reading of "a supplied press or impact station breaks one input": the station supplies the load, and the general fracture law of `ENGINE-FRACTURE` does the rest.

## 2. Declaration

### Declaration to write

**Station `fragmentation_station`.** Follows the delivered Linear pusher (`elements/LinearPusher/LinearPusherElementDeclaration.cs:28-65`): a static frame, one head on a prismatic slider, a supplied directional drive that self-locks when unpowered, a clipped rod, and two electrical command inputs.

- **Interfaces.** `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static`.
- **Body 0 (static), four box colliders.** Origin at the centre of the base underside. Material: restitution 0, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s.

  | Collider | Centre | Half extents |
  | --- | --- | --- |
  | `col_tray` | (0, 0.05, 0) | (0.60, 0.05, 0.50) |
  | `col_post_a`, `col_post_b` | (±0.65, 0.875, 0) | (0.05, 0.875, 0.50) |
  | `col_lintel` | (0, 1.80, 0) | (0.70, 0.05, 0.50) |

  The tray top is at 0.10 m and is open along ±Z. Clear opening 1.20 m wide by 1.65 m high.
- **Body 1, the head (dynamic, on slider 0), two colliders.** Box `col_head`, half extents (0.30, 0.05, 0.30). `col_rod`, a box of half width 0.06 m rising from the head's top and clipped at the lintel's underside by a `ClippedExtrusionDescriptor`, exactly as the Linear pusher's rod (`:40-41`), so the drawn rod and its collider are one box at every head position. Mass 0.5 kg; box moments on half extents: X and Z 0.5 × (0.0025 + 0.09)/3 = 0.0154, Y 0.5 × (0.09 + 0.09)/3 = 0.030 kg·m². Material as body 0. Gravity (0, −9.81, 0).
- **Slider 0.** Positive travel is downward (base frame turned so its travel axis, the frame's local Y, points along −Y). Base frame at (0, 1.65, 0): raised, the head's underside is at 1.60 m and its top at 1.70 m, under the lintel's 1.75 m underside. Lower limit 0, upper limit 1.20 m (underside stops at 0.40 m). No spring. Drive: `PrismaticTargetSelection.Directional`, `PrismaticInactiveResponse.SelfLock` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:84-85`), maximum speed 0.5 m/s, stopping acceleration 4 m/s², effort from `force`, power `force` × 0.5.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `force` | Real | N | 150 | 20 | 200 | build-only; player-editable unless locked (D11) |

- **Ports.** All Electrical inputs, as the Linear pusher's (`:42-46`): `PowerIn` (role `PowerIn`), `PressIn` (role `FirstIn`, drives downward) and `RaiseIn` (role `SecondIn`), on the front face of post A at heights 0.5, 0.8 and 1.1 m. Supply graph: one logic node, the motor consumer targeting prismatic 0, and the two command consumers. The pusher's two end-position outputs are not declared (D19: no lesson uses them).
- **Stores.** None; work is supplied.
- **Presentation bindings.** `Visual/Mark` colour ← `PresentationDriver.CommittedPrismaticPhase` with state values (three channel bindings, as the pusher's indicator, `:73`); `Visual/Head` and `Visual/Rod` follow body 1 and the clipped extrusion.
- **Palette.** Category `ElementCategory.Power` (with the Linear pusher). Order: 343 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); the companion block has none.

**Companion `breakable_block`.** Follows the delivered Domino as a dynamic box (`elements/Domino/DominoElementDeclaration.cs:34-37`), with the load limit and fragments of the engine story.

- **Interfaces.** `IMechanicalElement`, `ISceneGeometryElement`, plus the load-limit and fragment contract `ENGINE-FRACTURE` delivers. `KinematicMode.Dynamic`; `PayloadPolicy.CaptureTarget`.
- **Parent body.** Box collider, half extents (0.25, 0.25, 0.25). Mass 1.6 kg; moments 1.6 × (0.0625 + 0.0625)/3 = 0.0667 kg·m² on each axis. Material: restitution 0.1, friction 0.5, rolling resistance 0, bounce threshold 0.1 m/s. Gravity (0, −9.81, 0).
- **Declared load limit.** Measure: contact normal, the largest normal load the body carries from any one other body (the sum over that pair's contact points). Limit 120 N; hold time 0.25 s: the block breaks when the measure has exceeded 120 N on every substep for 0.25 s (120 substeps). The settling transient at Run start does not count, and a strike shorter than the hold never qualifies.
- **Declared fragments.** Eight boxes of half extents (0.125, 0.125, 0.125) at offsets (±0.125, ±0.125, ±0.125) in the parent frame. Each 0.2 kg (8 × 0.2 = 1.6 kg, the parent's mass); moments 0.2 × (0.015625 + 0.015625)/3 = 0.00208 kg·m². Same material. Fragments declare no limit, so they never break again. Like the block, they have no palette entry: they appear only from their parent's break (owner decision 24b).
- **Event.** One `SimulationEventKind.IntegrityBroken` row with its integrity observation, the form the Balloon declares (`elements/Balloon/BalloonElementDeclaration.cs:53`, `:68`), so a level's `IntegrityBroken` goal condition reads the break.
- **Parameters, ports, stores.** None (D13: one material, fixed values).
- **Not placeable** (owner decision 24b, 23 November 2026). No palette entry, so no category or palette order (today every package declares a positive palette order, `CuriousContraptions.Abstractions/ElementMetadata.cs:16`, so the not-placeable form is S7's). The block appears only with its parent: each `fragmentation_station` instance carries one `breakable_block`, placed with the station at a seat pose the station declares and removed with it; it is never placed by itself and the player cannot move it off its seat, but the player may delete it alone in Build and restore it from its station, and a level may place a station without it; Reset restores the authored state either way (coordinator decisions 33 and 34). The seat pose puts the block's centre at (0, 0.35, 0) in the station's frame, unturned: resting on the tray (top 0.10 + half height 0.25 = 0.35) under the head's centre. The not-placeable declaration and the parent's seat pose are the capability S7 adds with the companion admission.

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tray` navy `#293954` | box 1.2 × 0.1 × 1.0 | solid, `col_tray` | nothing |
| `Visual/Post_A`, `Visual/Post_B`, `Visual/Lintel` cream `#fff8e9` | boxes as the colliders, corners rounded inside the collider | solid | nothing |
| `Visual/Head` cyan `#66b8c9` | box 0.6 × 0.1 × 0.6 | solid, `col_head` | body 1 pose |
| `Visual/Head/Face` gold `#f7cb52` | printed flush on the head's underside | decorative: printed flat | body 1 pose |
| `Visual/Rod` cream | box 0.12 m square from the head to the lintel | solid, `col_rod`; art and collider are the same clipped box, the visible-solid status the delivered Linear pusher's rod has (remainder part B, row B6) | body 1 pose and the clipped extrusion |
| `Visual/Mark` | arrow-shaped mark printed flush on post A | decorative: printed flat, coloured by a binding | presentation binding |
| block `Visual/Whole` cream `#e8d4a6` with a navy cross printed on each face | box 0.5 m | solid, the parent collider; shown while the parent is active | parent pose |
| block `Visual/Piece_0` to `Piece_7` | boxes 0.25 m | solid, one fragment collider each; shown while fragments are active | each fragment's pose |

The printed cross marks where the block will part, so it reads as breakable by pattern, not colour. Separation is the fragments' own motion: no sparks or debris art. The rod is rigid; no hose or duct is drawn. Icons: an arch with a descending head; a cube with a cross.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- |
| A vertical supplied prismatic drive bounded by a declared force | present | `PrismaticDriveDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:89` | `LinearPusherTests.VerticalPackageLiftsCargoOnlyWithSufficientDeclaredForce` |
| Self-lock when power is lost; resume on command | present | `PrismaticInactiveResponse.SelfLock`, `:85` | `LinearPusherTests.LosingPowerLocksTheIntermediatePositionAndRetractResumesFromIt` |
| An obstruction stops the head and adds no energy | present | same drive | `LinearPusherTests.ObstructionStopsTheHeadAndReportsBlockedWhileAnAdjacentWallDoesNot` |
| Static and dynamic box contact | present | `ISceneGeometryElement`, `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `WorkshopPhysicsLawFrictionTests.SlidingUsesTheSharedNormalLoadAndSpinSharesItsBudget` (face support case) |
| An integrity observation that publishes a broken event, read by a goal | narrower: bound to a gas store's outlet, and measured as one substep's approach impulse, so a resting or pressing load never qualifies | `ContactIntegrityDescriptor`, `CuriousContraptions.Abstractions/GasCouplingValues.cs:15`; `ContactIntegrity.Measure`, `CuriousContraptions.Abstractions/ContactIntegrity.cs:16` | `ContactIntegrityTests.RestingSupportAndApproachAtTheThresholdDoNotQualify`; `BalloonTests.AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie`; `GoalSignalSelectorTests.AuthoredIntegrityGoalBindsTheDeclaredObservationAndCurrentSaveField`; owner `ENGINE-FRACTURE` |
| A declared load limit on a free body, with a contact-normal measure and a hold time | **missing** | none | none; owner `ENGINE-FRACTURE` |
| Declared fragments that replace a body, the breaking substep re-solved with them | **missing** | none; a package's extra bodies exist only as slider or hinge bodies | none; owner `ENGINE-FRACTURE` |
| Fragments keeping the parent's rigid-body velocity at each fragment's centre, material values, payload role and per-body surface state | **missing** | none | none; owner `ENGINE-FRACTURE` |

Counts: present 4, untested 0, narrower 1, missing 3. One owner for the narrower row and all three missing rows: **`ENGINE-FRACTURE`** (`epics.md`: "a declared load limit on a connection or body, a one-way break that changes topology when the sustained load exceeds it, and declared fragments"). Its text needs the additions listed in the Batch M summary. The contact loads it reads come from `ENGINE-CONTACT-OBSERVATIONS`. Nothing needs code that names these parts.

## 4. Sources and legacy

- **Requirements.** "Insufficient impact leaves cargo intact; adequate supplied work creates finite fragments that can pass a smaller route. Blocked/unsupplied controls fail appropriately; fragments cannot create extra feed or lose inherited state on save/Reset" ([gap-09](../requirements.md#gap-09)). The element text adds: "Specify the fracture threshold and each supported material; fragments conserve mass and inherit explicit material state. Connect to existing broken-goal plans."
- **Decision block.** "Freeze one material's break threshold and fragment bounds: just below holds, just above breaks into bounded pieces, unsupported material is refused" ([S641](../invest/scope-corrections.md#s641)).
- **Audit.** "Breakable geometry, bounded fragment budget and conserved mass. Intact control fails a narrow delivery route; fragments remain physical and cannot multiply indefinitely" (`docs/physics-puzzle-gap-audit.md@a6c914e:L88-L88`).
- **Legacy.** None: no fracture or fragment code, test or level exists. The impact-scheduled joint and collider change (`CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L35-L54`) is the nearest precedent; only its atomicity carries forward.

## 5. Acceptance outline

Point of truth: [gap-09](../requirements.md#gap-09). Chrome proof through real UI controls is pending the post-catalogue phase.

### Behaviour and numbers

Laws: the supplied prismatic drive (force bounded by `force`), rigid contact, and the general load-limit law of `ENGINE-FRACTURE`.

- **Stroke.** The block's top is at 0.10 + 0.50 = 0.60 m; the raised head's underside is at 1.60 m, so the head travels 1.00 m before contact: 1.00 / 0.5 = 2.0 s, plus 0.5² / (2 × 4) = 0.03 m of ramp. The stroke ends with the underside at 0.40 m, 0.05 m above the fragments' tops (0.10 + 0.25 = 0.35 m), so fragments are free to be pushed off the tray.
- **Where the load is largest.** The block touches two bodies. Its top face carries the press; its lower face carries the press plus the block's own weight, so the tray contact is the largest and is what the measure reads. Weights: head 0.5 × 9.81 = 4.9 N; block 1.6 × 9.81 = 15.7 N.
- **Control, `force` 80 N.** The drive holds against the moving body's gravity within its force bound (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:82-83`), so the head delivers at most 80 + 4.9 N. Lower face: 80 + 4.9 + 15.7 = 100.6 N, which is 19.4 N or 16 % under the 120 N limit. If the bound is net of the head's weight it is 95.7 N. The block holds.
- **Positive, `force` 150 N.** Lower face: 150 + 4.9 + 15.7 = 170.6 N (165.7 N if net of the head's weight), 42 % over the limit. The block breaks 0.25 s after the load is established, about 2.3 s after the command.
- **Loads that must not break it.** A Bowling ball resting on the block: lower face 4 × 9.81 + 15.7 = 54.9 N. The same ball dropped on it from 1 m: a peak far above 120 N for a few substeps, then 54.9 N; the peak is shorter than the 0.25 s hold. The block alone on the tray: 15.7 N.
- **Why 120 N.** It sits 16 % above the 80 N control's load, 30 % below the 150 N default's, and more than twice the heaviest delivered ball at rest on the block.
- **Smaller route.** A fragment is 0.25 m across (0.354 m on a face diagonal) and passes a 0.40 m gap; the 0.50 m block does not.
- **Mass.** 8 × 0.2 kg = 1.6 kg exactly; the engine story refuses a declaration whose fragments do not sum to the parent.
- **Work.** The drive does at most `force` × stroke = 150 × 1.2 = 180 J in one press; a blocked head does none.
- **Fit.** A fragment weighs 0.2 kg: three press a default 0.5 kg Pressure plate (0.6 kg) and two do not (0.4 kg).
- **Tick order.** Supply and commands resolve in Phase 1; the drive and contacts are solved in Phase 3, where each substep's contact loads are compared with the limit. On the substep that completes the hold, the engine re-solves that substep with the fragment rows active in place of the parent (coordinator decision 16, 22 November 2026), so no committed tick shows the intact block still carrying the load ([solver model](../../gpu-f32-physics.md#solver-model)). `ENGINE-FRACTURE` confirms this once for every consumer.

### Acceptance rows

Planned classes `FragmentationStationTests` and `BreakableBlockTests`. Stall guard: 8 s of game time (more than three times the 2.3 s a press needs).

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| F1 | Given a block on the tray and the press supplied and commanded down at `force` 150, then the block is replaced by exactly eight fragments whose masses sum to 1.6 kg, and one broken event is published | `ASufficientPressBreaksTheBlockIntoItsEightFragments` | F2 | 80 N holds (100.6 N), 150 N breaks (170.6 N), around 120 N | exact counts and event count; mass sum within 0.001 kg |
| F2 | Given `force` 80, then the block is intact for the stall guard and the head rests on it | `AnInsufficientPressLeavesTheBlockIntact` | F1 | as F1 | exact state |
| F3 | Given no supply on `PowerIn` (command present), then the head moves less than 0.02 m and the block is intact | `AnUnsuppliedPressDoesNotMove` | F1 | none | 0.02 m |
| F4 | Given a Wall bridged across two supports above the block so the head lands on the Wall, then the drive reports Blocked and the block is intact | `ABlockedHeadLoadsNothingBeneathIt` | F1 | none | exact state |
| F5 | Given a Bowling ball resting on the block for 3 s, and the same ball dropped on it from 1 m, then the block is intact in both | `ARestingWeightAndABriefStrikeDoNotBreakTheBlock` | F1 | a load under the limit, and a load over it for less than the hold | exact state |
| F6 | Given the fragments of F1 swept off the tray by a Linear pusher onto a 0.40 m gap between two Walls over a Receiver, then at least six fragments end below the gap | `FragmentsPassARouteTheWholeBlockCannot` | the intact block of F2, swept the same way, stays above the gap | none | counts by position; a jammed pair of fragments is tolerated, hence six of eight |
| F7 | Given a level whose goal is the block's `IntegrityBroken` observation, then it is Solved in F1 and Waiting in F2 | `TheBrokenObservationSolvesABrokenGoal` | F2 | none | goal phase, one Solved occurrence |
| F8 | Given the fragments pressed again at 150 N for 3 s, then there are still eight bodies | `FragmentsNeverBreakAgain` | F1 | none | exact count |
| F9 | Reset after F1 restores one intact block at its authored pose, no fragment, and the raised head; a second Run repeats F1 | `ResetRestoresTheWholeBlockAndTheRaisedHead` | none | none | pose within 0.001 m; outcome repeated |
| F10 | Save and load keep `force`, the wires and the block's pose; a loaded construction starts intact | `SaveAndLoadKeepTheForceAndStartIntact` | none | none | exact |
| F11 | `force` of 19, 201 or a non-finite value is refused atomically | `InvalidForceIsRefused` (deleted when structural story S4 covers it) | 20 and 200 admitted | 20, 200 | typed refusal |
| F12 | An activation wire into `PowerIn`, `PressIn` or `RaiseIn` is refused | `ActivationWiresIntoElectricalInputsAreRefused` | electrical wires connect | none | typed refusal |
| F13 | A block declaration whose fragment masses do not sum to its mass, or whose limit or hold is non-finite or negative, is refused at bundle admission with a typed reason | `AnUnsupportedBreakableDeclarationIsRefused` | the delivered declaration is admitted | mass sum 1.6 kg against 1.5 kg | typed refusal, bundle unchanged |
| F14 | Given a block treated by a Coating or Dye station and then broken, then all eight fragments carry the treated value or tag, and Reset removes it | `FragmentsKeepTheParentsSurfaceState`, **later-partner row** (EL-118, EL-119) | an untreated block gives untreated fragments | none | exact state |

Collaborators the requirement names: supply (F1, F3), an insufficient strike (F5), a blocked control (F4), a smaller route (F6), the broken goal (F7), inherited state (F14).

### Integration

- **Works with.** Battery, Impact switch's electrical contact, Linear pusher, Wall, Receiver, Bowling ball, Pressure plate (delivered); EL-118 and EL-119 (F14).
- **Campaign.** "A Smashing Success": introduction 95, practice 96, reuse 99, 141, 150 ([campaign allocation](../requirements.md#campaign-gap-allocation)). Practice 96 "size and catch fragments with familiar sieve/limits" uses rigid openings (Walls, the Size grate), since fragments are bodies and the Granular sieve passes a granular inventory. A level that fixes the press (with its block) needs the lesson-fixture story (S8).
- **Goal.** F7 uses the `IntegrityBroken` observation kind that `engine-goal-evaluation` keeps; no new goal kind.
- **Coverage.** Section 7.

### Delivery

- **Shape.** One slice delivering a main package and its declared companion under one identity; engine tier (first consumer of the fracture capability on a body).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CONTACT-OBSERVATIONS` (the contact loads the limit measures) and `ENGINE-ISLANDS-SLEEP` (resting fragments); `ENGINE-FRACTURE` (package-declared limits after `ENGINE-JOINT-KINDS` and `ENGINE-CONTACT-OBSERVATIONS`; [engine stories index](readiness/engine-stories-index.md) C9); S7 widened to GAP identities and companion packages, with the not-placeable declaration and the parent's seat pose (owner decision 24b); S3 preferred. F14 after the Treatment stations slice.
- **Files.** `elements/FragmentationStation/` and `elements/BreakableBlock/` (each a project file, declaration, scene and icon), `CuriousContraptions.tests/FragmentationStationTests.cs`, `CuriousContraptions.tests/BreakableBlockTests.cs`, this spec, the ledger entry and `TODO.md`.
- **Disqualifies the plan.** `ENGINE-FRACTURE` delivers limits on connections only; it offers a body only the contact-impulse measure, or its contact-normal measure sums all of a body's contacts (the control would then read 80 + 4.9 + 100.6 = 185.5 N and break); fragments are created during a tick instead of compiled; or a clipped extrusion collider is not admitted on a downward slider.

## 6. Open questions

Resolved:

1. **Press or impact.** One mode, the supplied press. The row says "press or impact", S641 freezes one mode, and the press is the delivered Linear pusher capability with literal "supplied work". A strike never breaks this block: its limit has a 0.25 s hold, and a transient shorter than the hold does not qualify (row F5), which is the row's "insufficient impact leaves cargo intact".
2. **Fracture measure.** One measure, contact normal with a hold time, from the closed set `ENGINE-FRACTURE` defines. "Any one contact" is read as contact with one other body, summed over that pair's contact points: a face contact is several points and must not divide the load.
3. **Materials and "unsupported material is refused".** One supported material, with a 120 N limit (S641: "freeze one material's break threshold"). Refusal applies to a declaration: a breakable declaration the engine cannot honour is refused at admission (row F13). A body that declares no limit is not refused; it simply never breaks, as F5's Bowling ball shows under any press.
4. **Re-fracture.** No; fragments declare no limit.
5. **Block as its own package.** A declared companion package of this identity with its own catalogue entry (owner decision, 22 November 2026, item 15; D8), not placeable: it has no palette entry and comes with each station at the station's seat pose (owner decision 24b, 23 November 2026).
6. **Fragment count.** Eight, fixed at compile time.

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** The station is boxes, one slider with a declared drive, a clipped rod, three ports and bindings. The block is a box with a declared limit, eight declared fragments and one event. No code names either part; there is no "station breaks cargo" rule.
2. **Generic laws.** The supplied prismatic drive and rigid contact produce a load; the general load-limit law breaks any body that declares a limit below it. Both are parameterised by declared values (`force`, 120 N, 0.25 s).
3. **Three layers.** Colliders: four static boxes, a head box and a clipped rod box; one parent box and eight fragment boxes. Visuals: one mesh per collider; face and mark are printed flat. Animation: mark colour from committed state. The block's whole and piece art follow their bodies' poses and are shown by which bodies are active; nothing in physics reads art.
4. **Flow and typing.** Three Electrical inputs typed by `PortRole`; the broken event is the typed `SimulationEventKind.IntegrityBroken`; values are f32.
5. **Flat state.** Station: bodies 2; colliders 6 (box, one clipped); joints 1 prismatic; drives 1; supply nodes 4; electrical nodes 2; stores 0; bindings 3, moving roots 1. Block: body rows 9 (1 parent, 8 fragments, all compiled at Run, at most 8 active after the break); colliders 9 (box); load limits 1; integrity observations 1; events 1. The break toggles rows that already exist, so a tick allocates nothing.
6. **Performance.** Six primitive colliders on the station, as the Linear pusher's order. Nine box rows per block; inactive fragment rows take no part in the broadphase, sensors or the committed body set. The engine has no island sleeping today; eight resting fragments rely on `ENGINE-ISLANDS-SLEEP`, and contact loads and events come from `ENGINE-CONTACT-OBSERVATIONS`. A head at 0.5 m/s moves 0.5 / 480 = 0.001 m per substep, far below any collider's thickness here.
7. **Tests are scenarios.** F1 to F14 run the production simulation and goal evaluator.

**Verdict: conforms; blocked on the engine programme (owner decision 3a) and on `ENGINE-FRACTURE`, a general capability stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads. **A GAP identity cannot be bound at all today:** the reader lists only `EL-*.md` files and admits only an `EL-NNN` identity; it also refuses two packages on one identity. **Blocked on the coverage follow-on story S7**, which widens the reader to GAP identities and to a main package with declared companions (owner decision 15, 22 November 2026).

| Field | Planned value |
| --- | --- |
| Packages | `fragmentation_station`, with declared companion `breakable_block` |
| Named identity | GAP-09, `../invest/named-elements.md#gap-09` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-542` |
| Decision | S641, `../invest/scope-corrections.md#s641` (the block exists on published main) |
| Mode records | `Configuration=Adjustable` (`force`) for the station; `Configuration=Fixed` for the companion |

What it lacks: GAP admission; companion-package admission; a todo link in the ledger entry.
