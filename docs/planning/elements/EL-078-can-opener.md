# EL-078 · Can opener — named-identity spec

**Current delivery (24 October 2026):** [EL-078 delivery slice](../invest/named-elements.md#element-078). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout, and `path@a6c914e:L…` is the legacy harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Every number is a hand calculation; nothing was simulated. Game values are canonical f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-078 · Can opener · Specialist |
| Anchor | [requirements.md#element-078](../requirements.md#element-078); [named entry](../invest/named-elements.md#element-078); scope index [todo-218](../requirements.md#todo-218) (shared with EL-079); [sequence-task-631](../requirements.md#sequence-task-631) |
| Proof owner | S648 |
| Disposition | To deliver as a single element with one declared companion package, the sealed can, which is the requirement row's "compatible container". A companion package belongs to this identity and is not a new ledger identity (owner decision 15, 22 November 2026). Not a duplicate of any part. |
| Planned packages | main `can_opener`, directory `elements/CanOpener/`, class `CanOpenerElementDeclaration`; companion `sealed_can`, directory `elements/SealedCan/`, class `SealedCanElementDeclaration`, declared not placeable (owner decision 24b, 23 November 2026) |
| Refines / extends | No CAT spec. Drive shape of the Motor (`elements/Motor/MotorElementDeclaration.cs:16`). Cutting law of Batch I's blade pair ([EL-066 Scissors](EL-066-scissors.md), [EL-068 Tin snips](EL-068-tin-snips.md)) |
| Status | not started; blocked on the engine programme (owner decision 3a), then ENGINE-JOINT-KINDS (fixed joint) and ENGINE-TENSILE-EXTENSIONS (cutting clause, with a seam as target), then S7 |

## 2. Declaration

**One cutting-work law.** There is one cutting law in the game (coordinator decision 16, 22 November 2026): Batch I's blade pair (`docs/planning/elements/readiness/batch-i.md:96`). A fixed straight edge and an edge on a body turning about a hinge, both through the hinge axis, close on a target. When the gap between the edges at the target falls to the target's declared thickness d, the target opposes the moving edge with force R/d, R being its cutting resistance in joules. If R is at most the cutter's capacity C the target is severed when the gap reaches zero, having absorbed R; otherwise the gap is held at d. The opener is such a blade pair with a supplied drive. Its target is not a rope but the can's seam: a declared straight segment on the lid, joined to the can by a fixed joint. That is the only extension the law needs: "a target that is a span or an attachment seam" (section 3). Tin snips with enough capacity open the same tin by the same law, and this blade pair cuts a rope laid through it.

### `can_opener`: bodies and colliders

Implements `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static` base, `SupportedDomains = Mechanical | Electrical`, footprint 2 bodies, 1 moving, 1 revolute constraint. Origin on the bench; a seated can's axis is at x = 0.40.

| Body | Collider | Shape and size (m) | Centre (m) |
| --- | --- | --- | --- |
| 0, static | seat | box 1.1 × 0.2 × 0.8 | (0.25, 0.10, 0) |
| 0 | stop | box 0.1 × 0.3 × 0.8 | (0.75, 0.35, 0) |
| 0 | column | box 0.2 × 0.9 × 0.3 | (−0.20, 0.65, 0) |
| 0 | fixed jaw | box 0.18 × 0.03 × 0.04 | (−0.01, 0.655, 0); its top face, y = 0.67, is the fixed edge |
| 0 | lamp | sphere radius 0.06 | (−0.20, 1.13, 0) |
| 0 | socket | sphere radius 0.075 | (−0.30, 0.90, 0) |
| 1, blade | moving jaw | box 0.21 × 0.03 × 0.04 | from the pivot along the blade, centre (0.105, 0.015, 0) in the blade frame, so its lower face passes through the hinge axis; that lower face is the moving edge |

Static material: restitution 0, friction 0.4, rolling resistance 0, bounce threshold 0.1 m/s. Blade: 0.3 kg (**starting value**); moment about the pivot 0.3 × 0.21²/3 = 0.0044 kg·m²; friction 0.3. All boxes are above the admitted minimum half-extent.

### `can_opener`: hinge, drive and blade pair

- **Hinge.** One `RevoluteHingeDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`): support body 0; base frame at (−0.13, 0.67, 0), axis Z; `RevoluteTravel.Limited` from 0 to 0.60 rad; initial angle 0.55 rad (open); `FrictionTorque` 1.0 N·m; `BodiesCollide` false.
- **Drive.** `RevoluteDriveDescriptor` (`MechanicalContractValues.cs:138`): target speed −0.125 rad/s (closing), maximum torque 56 N·m, both fixed.
- **Blade pair** (the cutting clause's declaration): fixed edge on body 0 from the pivot along +X, 0.21 m long; moving edge on body 1, 0.21 m long; blade thickness 0.04 m along Z; capacity C = 15 J.

One port: `PowerIn` (Electrical, Input, role `PowerIn`, glyph Power) at (−0.30, 0.90, 0), normal −X. Supply graph: one consumer with `SupplyDriveTarget(Revolute, 0)` bound to `PowerIn`, the Motor's form (`MotorElementDeclaration.cs:120`). Electrical node: Consumer, 12 V, `MaxPower` 10 W: a declared rating, read by nothing. The real debit is ENGINE-SUPPLY-DEMAND's: the source pays the drive's committed work each tick and an exhausted source stops the blade (rows A9 and A10). No parameters (D13), no stores.

Metadata: category `Power`, display name "Can opener", palette order 341 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md); the companion `sealed_can` takes none), colour (1.0, 0.97, 0.91). Presentation: body binding `Visual/Blade` → body 1; `Visual/Lamp` albedo slate `#556573` to gold `#f7cb52` on `PresentationDriver.CommittedActuatorDrive` (the Motor's binding, `MotorElementDeclaration.cs:25`).

### `sealed_can` (companion): bodies, seam and parameter

A dynamic part, the Balloon's kind of package (`elements/Balloon/BalloonElementDeclaration.cs:10`): `KinematicMode.Dynamic`, `SupportedDomains = Mechanical`, 2 bodies. Origin at the centre of the can's base.

**Not placeable** (owner decision 24b, 23 November 2026): the can has no palette entry and appears only with its parent. Each `can_opener` instance carries one `sealed_can`, placed with the opener at a seat pose the opener declares and removed with it; it stays on its seat (the player cannot move it off) and is never placed by itself; the player may delete it alone in Build and restore it from its opener, and a level may place an opener without it; Reset restores the authored state either way (coordinator decisions 33 and 34). The seat pose puts the can's origin at (0.40, 0.20, 0) in the opener's frame, unturned: its base on the seat top (y 0.20), its wall's outer face touching the stop's inner face (0.40 + 0.30 = 0.70), and its tab toward the jaws (x 0.40 − 0.35 = 0.05, the seam line of "Geometry on the seat"). Its `seam_work` is edited on the can as on any part. The not-placeable declaration and the parent's seat pose are the capability S7 adds with the companion admission (owner decision 24b).

| Body | Collider | Shape and size (m) | Centre (m) |
| --- | --- | --- | --- |
| 0, can | wall | Tube: outer radius 0.30, inner 0.27, half-length 0.25, axis Y | (0, 0.25, 0) |
| 0 | bottom | cylinder radius 0.27, half-length 0.015, axis Y | (0, 0.015, 0) |
| 1, lid | disc | cylinder radius 0.24, half-length 0.02, axis Y | (0, 0.52, 0) |
| 1 | rim | Tube: outer radius 0.30, inner 0.24, half-length 0.03, axis Y | (0, 0.53, 0) |
| 1 | seam tab | box 0.10 × 0.05 × 0.20 | (−0.35, 0.50, 0) |

Can 0.5 kg, lid with tab 0.06 kg (**starting values**), moments by shape; restitution 0.2, friction 0.4. The rim rests on the top of the wall, so a loose lid sits on the can and does not fall in. The tab stands 0.10 m out from the can's side across the line where lid meets can. The inside is 0.54 m across and 0.47 m high: it holds a Balloon at its smallest radius or a small Tennis ball, placed by a level.

**Seam.** One fixed joint between lid and can (ENGINE-JOINT-KINDS). Its attachment seam is the segment from (−0.35, 0.50, −0.10) to (−0.35, 0.50, +0.10), the tab's centre line, with thickness d = 0.05 m (the tab's height) and cutting resistance R bound to `seam_work`.

| Key | Type, unit | Minimum | Maximum | Default | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- |
| `seam_work` | Real, joule | 3 | 24 | 12 | Reject | the opener's capacity is 15 J, so the range spans tins it opens and tins it cannot |

Metadata: display name "Sealed can", colour (0.78, 0.80, 0.82); no palette entry, so no category or palette order (owner decision 24b; today every package declares a positive palette order, `CuriousContraptions.Abstractions/ElementMetadata.cs:16`, so the not-placeable form is S7's). Presentation: body binding `Visual/Lid` → body 1.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| opener `Visual/Seat`, `Stop`, `Column` | cream `#fff8e9` seat and column, navy `#293954` stop | solid, box colliders | nothing |
| opener `Visual/FixedJaw` | slate `#556573` bar | solid, box | nothing |
| opener `Visual/Blade` | slate bar with a gold `#f7cb52` edge line printed on it | solid, box on body 1 | body 1 pose |
| opener `Visual/Lamp`, `Visual/Socket` | spheres | solid | lamp colour by binding |
| can `Visual/Can` | pale grey shell with a cream label band printed on it | solid, Tube and bottom | body 0 pose |
| can `Visual/Lid` (disc, rim, tab) | grey disc with a raised rim and a gold tab | solid: cylinder, Tube and box | body 1 pose |

Icons: opener, an open pair of jaws on a post beside a seat; can, a closed tin with a side tab. The opener differs from the Scissors and Tin snips by its seat and post (it is a station, not a hand tool); the can differs from every ball and box by being a lidded cylinder with a tab.

### Behaviour and numbers

Laws: the revolute joint with friction and limits; the supplied velocity drive; the blade-pair cutting law; the fixed joint; rigid contact.

- **Geometry on the seat.** Seat top y = 0.20. A can pushed against the stop (inner face x = 0.70) has its axis at x = 0.40 and its near wall at x = 0.10. Its tab spans x 0.00 to 0.10, y 0.675 to 0.725, z ±0.10; the seam segment is at x = 0.05, y = 0.70, running along Z through the blade plane z = 0. The jaws reach from the column to x = 0.08, short of the can's wall by 0.02 m. The seam crosses the blades 0.05 − (−0.13) = 0.18 m from the pivot, inside their 0.21 m length.
- **Angles.** Gap at the seam = 0.18 × tan θ. Open, θ = 0.55 rad: gap 0.110 m, twice the tab's height, so a can slides in. Engagement at gap d = 0.05 m: θ = atan(0.05/0.18) = 0.271 rad. Severed at θ = 0.
- **Force and torque.** Default R = 12 J: the seam opposes the moving edge with R/d = 12/0.05 = 240 N at 0.18 m, 43.2 N·m, inside the drive's 56 N·m. The torque bound covers the capacity plus the bearing: 15 J / 0.05 m × 0.18 m + 1.0 N·m = 55 N·m ≤ 56 N·m (the blade's weight, 0.3 × 9.81 × 0.105 × cos θ = 0.30 N·m during the cut, helps the closing and is left out as margin). So every seam the law severs (R ≤ 15 J) the drive can also drive through, and above 15 J the law holds the gap at d whatever the drive offers: at 16 J the drive would need 16/0.05 × 0.18 + 1.0 − 0.30 = 58.3 N·m and stalls in any case; at 14 J it needs 51.1 N·m.
- **Time.** At 0.125 rad/s the blade takes (0.55 − 0.271)/0.125 = 2.2 s to reach the tab and 0.271/0.125 = 2.2 s to cut through it: 4.4 s in all, whatever the resistance, since the drive holds its speed. Acceptance guard 12 s.
- **Power and work.** During the cut 43.2 × 0.125 = 5.4 W, 0.045 J per tick; at the torque bound 56 × 0.125 = 7.0 W, 0.058 J per tick. Both are far inside the Battery's 120 W and 1 J per tick, so the per-tick allowance limit of coordinator decision 27 (power_limit × dt) never scales this drive. Work: the seam absorbs R = 12 J (240 N over 0.05 m); the bearing takes 1.0 × 0.55 = 0.55 J; the blade's own weight returns 0.3 × 9.81 × 0.105 × sin 0.55 = 0.16 J. The drive's committed work is about 12.4 J, which is what the source is debited.
- **No net force on the can.** The tab is squeezed between the two edges, so the can is not pushed along the seat. The cutting clause must say that both edges load the target equally (section 3).
- **Missing supply.** An unsupplied drive applies no torque. The blade's weight gives 0.3 × 9.81 × 0.105 = 0.31 N·m, below the 1.0 N·m bearing friction, so the blade stays open; even resting on the tab it could not cut (0.31 against 43.2 N·m). The seal stays intact.
- **Misalignment.** A can 0.2 m off in Z has its seam at z from 0.10 to 0.30, outside the 0.04 m blade thickness: the blades close on nothing. A can turned so the tab faces away presents its wall 0.02 m beyond the jaws' tips. In both cases the cutter's cut-work read stays 0.
- **A tin too tough.** With `seam_work` above 15 J the gap is held at d, the drive stalls and does no further work, and the seal stays intact.
- **Interrupted cut.** If supply is lost mid-cut the blade stops where it is (the law never pushes the blades apart) and resumes when supply returns.
- **After the cut** the lid is a free body resting on the can. A default-buoyancy Balloon inside (0.5 kg, net lift (11.5 − 9.81) × 0.5 = 0.85 N) lifts the 0.59 N lid; tipping the can spills a ball. Before the cut neither can happen.

## 3. Engine capabilities

Map row families: ContactImpulse, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, StructuralFracture, TopologyTransaction, TypedContracts (navigation only, D14; the row's ProgrammableController and TimedCommand are not used).

| Capability | Status | Evidence |
| --- | --- | --- |
| Supplied revolute drive on a limited hinge; it holds its speed within a torque bound, and does no work when stalled at a stop or unsupplied | present | `MechanicalContractValues.cs:138`; `RevoluteDriveTests.AStalledDriveDoesNoWorkWhileItsShaftIsBlocked` (`CuriousContraptions.tests/RevoluteDriveTests.cs:307`, a driven Limited hinge against its stop); `.ASuppliedDriveSpinsTheRotorUpAtItsTorqueBoundThenHoldsTheTargetSpeed` (`:176`); `MotorTests.AnUnpoweredMotorStaysStillDoesNoWorkAndKeepsItsLampSlate` (`CuriousContraptions.tests/MotorTests.cs:294`) |
| Hinge friction holds a body whose gravity torque is below it | present | `RevoluteHingeFrictionTests.AHingedBarIsHeldWhileItsGravityTorqueIsBelowTheFrictionTorque` (`CuriousContraptions.tests/RevoluteHingeFrictionTests.cs:77`) |
| Tube colliders as children of a dynamic body | present | `ColliderShapeKind.Tube` (`CuriousContraptions.Abstractions/ColliderDescriptor.cs:8`); `CompoundMassAndRollingTests.HollowChildLocalPoseAndDeclaredCentreGiveRealContactAndEmptySpace` (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:114`) |
| A fixed joint between a package's dynamic base and a second body of the same package | **missing** | ENGINE-JOINT-KINDS (owner decision 3a and coordinator decision 16; `docs/planning/elements/readiness/engine-design-gap.md`, section 6 item 9) |
| A blade pair that cuts a target by declared cutting work against a capacity | **missing** | ENGINE-TENSILE-EXTENSIONS, cutting clause (owner decision 3a; Batch I's contract, `docs/planning/elements/readiness/batch-i.md:96`) |
| The target may be an attachment seam as well as a span | **missing** | the same story and clause; the one-line generalisation is in the batch summary: a cutting target is a tensile span or an attachment seam, a declared segment with thickness and resistance on a body held by a fixed joint; severing it releases the joint one-way; both edges load the target equally; blade edges do not collide with a target's strand or seam piece |
| Drive work debits its source; an exhausted source stops the drive | **missing** | ENGINE-SUPPLY-DEMAND (owner decision 3a); it precedes every element |

No friction "work limit" is asked of ENGINE-FRACTURE, and no contact work on a moving owner is needed, so ENGINE-CONTACT-OBSERVATIONS is not a prerequisite of this part beyond its place in the engine programme.

## 4. Sources and legacy

- **Requirement row.** [element-078](../requirements.md#element-078): a supplied tool progressively cuts a compatible container seam using generic cutting work; misalignment and missing supply leave the seal intact.
- **Research reference.** The inventory cited by [todo-218](../requirements.md#todo-218).
- **Legacy search.** `git grep -i` at a6c914e for "can opener", "cut", "sever", "seam" and "blade" over every Epic 7 deletion path found no cutting source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | A debit never exceeds stored energy; a charge never exceeds power × dt | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L52` | Carry forward as "cut work never exceeds the drive's work". Do not carry forward the CPU ledger |

**Files harvested:** `engine/physics/PhysicsEnergyStore.cs` (generic finite-work rule only).

## 5. Acceptance outline

Requirement row: [element-078](../requirements.md#element-078). Planned test class `CanOpenerTests`. "Sealed" means the fixed joint's committed state is intact and the lid's pose relative to the can is unchanged within 0.01 m; "open" means it is released. "Cut work" is the cutter's committed cut-work read.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a can seated against the stop with its tab between the jaws and a Battery on `PowerIn`, when Run proceeds, then the blade closes, the cut work rises to `seam_work` and the can is open | `ASuppliedBladeCutsTheSeam` | A2, A3 | open inside a 12 s guard; cut work never decreases and ends within 1 % of 12 J |
| A2 | Given the same with no supply (no battery, disabled, unwired), then the blade stays open and the can stays sealed | `WithoutSupplyTheSealStaysIntact` (theory) | A1 | blade angle within 0.02 rad of 0.55 and cut work 0 for 10 s |
| A3 | Given the can 0.2 m off in Z, or turned so its tab faces away, with supply, then the blades close and the can stays sealed | `AMisalignedCanIsNotCut` (theory) | A1 | cut work 0 for 10 s |
| A4 | Given supply removed while the cut work is between 3 and 9 J and restored 2 s later, then the blade holds while unsupplied and the cut completes afterwards | `AnInterruptedCutResumes` | A1 | blade angle changes by at most 0.02 rad while unsupplied |
| A5 | Given `seam_work` 14 and then 16, then only the first opens; the second holds the blade at the tab with the drive stalled | `ATinTougherThanTheCutterStaysSealed` | each other | at 16 J the cut work stays 0 and the drive's work stops growing |
| A6 | Given a sealed can holding a Balloon, then the lid stays on; given the opened can, the Balloon lifts the lid and leaves | `OnlyAnOpenedCanReleasesItsContents` | the sealed case | the Balloon's centre rises above the rim within 5 s only when open |
| A7 | `seam_work` 3 and 24 are admitted; 2.9, 24.1 and non-finite values are refused at every seam | `TheSeamWorkBoundsAreAdmitted` (theory) | — | exact bounds |
| A8 | A Signal output offered to `PowerIn` is refused | `AnActivationWireIntoThePowerInputIsRefused` | an electrical `Supply` is accepted | typed `InvalidConnection`; the Motor's delivered row (`MotorTests.cs:316`) proves the shape |
| A9 | Given A1, then the Battery's debit equals the drive's committed work, and the cut work is no more than it | `TheBatteryPaysForTheCut` | A2 debits nothing | debit never below the committed work and above it by no more than the ledger residual; expected about 12.4 J |
| A10 | Given a 60 J Battery (its minimum capacity, `elements/Battery/BatteryElementDeclaration.cs:42`) at `initial_charge` 1/12 (5 J, less than the 12.4 J the cut needs), then the blade stops when the source is exhausted and the can stays sealed | `AnExhaustedSourceStopsTheBlade` | A1 | cut work below `seam_work`; the seal intact for 10 s after the stop |
| A11 | Reset restores the sealed can, zero cut work, the open blade and the source's charge; save and load keep both parts, the parameter and the wire | `ResetAndSaveLoadRestoreTheSealedCan` | — | generic row: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

The threshold of the law itself (torque just below and just above R·x/d, two differently parameterised fixtures) is the engine story's own test. Every row fits the default 30 s Run length.

### Integration

- Supply: Battery and any switched supply. Contents and movers: Balloon, Tennis ball, Fan, Linear pusher (to seat a can against the stop). All delivered.
- The same law in other parts: when EL-068 Tin snips is delivered, one row is added there or here: snips whose capacity is at least `seam_work` open the tin, and a rope laid through this opener's jaws is cut if its material's resistance is at most 15 J.
- Campaign: chapter 10 "Oddly Satisfying", levels 91–100, "opener" (`docs/planning/requirements.md:2410`, chapter row `:2295`); reuse 111–120, 136–150.
- A level that fixes the opener (with its can), or ships the can with contents already inside, needs the lesson-fixture story S8.

### Delivery

- **Shape.** Single element slice that delivers the main package and its companion, each with its own acceptance rows.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND; then ENGINE-JOINT-KINDS (fixed joint); ENGINE-TENSILE-EXTENSIONS (cutting clause, with the seam target); then S7, widened to admit companion packages and to declare them not placeable with the parent's seat pose (owner decision 24b).
- **Files.** `elements/CanOpener/` and `elements/SealedCan/` (project, declaration, scene, icon each); `CuriousContraptions.tests/CanOpenerTests.cs`; two `CuriousContraptions.slnx` lines; the binding section once S7 admits it; until S3, the remaining per-package lists.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's. The rule that blade edges do not collide with a seam piece is written as whatever participation mask that story leaves.
- **Review tier.** Engine tier unless the engine slices publish every row above with tests.
- **Disqualifiers the probe re-checks.** The cutting clause not admitting a seam as target; a severed tab that collides with the closed blade (the lid is then pushed up 0.03 m, which is harmless, but the art must be checked); whether a level can place a body inside the can's Tube; an opener placement that does not put its can at the seat pose with the jaws' 0.02 m clearance to the can wall.

## 6. Open questions

All resolved.

1. **The can is a declared companion package** of this identity, delivered in this slice, with its own catalogue entry (D8). It is not a new ledger identity (owner decision 15, 22 November 2026). It is not placeable: it has no palette entry and comes with each opener at the opener's seat pose (owner decision 24b, 23 November 2026, answering the question raised in the second-audit repair; stated once in [EL-084](EL-084-fragile-fish-bowl.md) section 6 item 7). S7 adds the capability with the companion admission.
2. **Cutting law: the one law.** The opener is a driven blade pair under Batch I's span-cutter contract, with a seam as its target (coordinator decision 16). The earlier friction-work "work limit" on ENGINE-FRACTURE is withdrawn: it was a second rule for the same behaviour.
3. **"Progressively"** is the cut work rising from 0 to R over about 2.2 s while the blade visibly closes through the tab. An interrupted cut holds and resumes.
4. **One cut per Run.** The drive only closes; Reset reopens the blade. A reopening stroke is not asked for (D19).
5. **No opener parameters** (D13); `seam_work` is on the can. All values are starting values (D1).

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** Two packages of bodies, colliders, one hinge with a drive, one blade pair, one fixed joint with a declared seam, one port, one parameter and bindings. Neither names the other; no opening routine, alignment check or handler exists.
2. **Generic laws.** The revolute joint and supplied drive; the one blade-pair cutting law; the fixed joint; contact; the supply ledger. The missing capabilities are stated element-free, each with one owner, and the cutting law is the same one the Scissors and Tin snips use.
3. **Three layers.** Colliders: opener 7 (boxes 5, spheres 2); can 5 (Tubes 2, cylinders 2, box 1). Visuals: one mesh per collider with flat prints. Animation: the lamp from the committed drive state; blade, can and lid follow body poses.
4. **Flow and typing.** One typed Electrical input; `RevoluteTravel` is an enum; `seam_work` is an f32 in joules; the cut-work read flows to tests and presentation only.
5. **Flat state.** Opener: bodies 2 (1 static, 1 dynamic); colliders 7; joints 1 revolute; drives 1; blade pairs 1; domain nodes 1 supply consumer and 1 electrical; stores 0; bindings 4. Can: bodies 2 (dynamic); colliders 5; fixed joints 1 with 1 seam; bindings 1. Severing changes the compiled constraint set at a step boundary with no allocation in a tick.
6. **Performance.** Few primitive colliders; the base is static. Candidate targets for a blade pair come from a bounded query (ENGINE-SPATIAL-QUERIES), not a scan of every seam and route each tick, as the cutting clause already requires for spans. No sleeping exists today; an untouched can rests at no cost only once ENGINE-ISLANDS-SLEEP is delivered, ahead of this part.
7. **Tests are scenarios.** Every row runs the production simulation with delivered supply and cargo.

**Verdict: conforms** (general capabilities missing, each with one owner; one cutting law).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `can_opener`; companion `sealed_can` (S7 widens the reader to admit companion packages, owner decision 15) |
| Named identity | EL-078, `../invest/named-elements.md#element-078` |
| Acceptance | `todo-218`, `sequence-task-631` (`docs/planning/requirements.md:5021`) |
| Decision | S648, `../invest/scope-corrections.md#s648` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s648` block; `todo-218` is a scope index shared with EL-079, and `sequence-task-631` is anchored beside the element row, not beside that todo, which is the pairing the reader expects; the reader binds one package per identity and does not yet admit a companion.
