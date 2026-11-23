# EL-087 · Predator character — named-identity spec

**Current delivery (24 October 2026):** [EL-087 delivery slice](../invest/named-elements.md#element-087). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `T/` abbreviates `CuriousContraptions.tests/` and `A/` abbreviates `CuriousContraptions.Abstractions/`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32. Nothing here was built or simulated; every number is a hand calculation shown beside it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-087 · Predator character · Character |
| Anchor | [requirements.md#element-087](../requirements.md#element-087); [named entry](../invest/named-elements.md#element-087); scope index [todo-221](../requirements.md#todo-221) |
| Proof owner | S657 |
| Disposition | Member of the Family slice **"Sighted walkers"** with [EL-082](EL-082-lured-character.md) (the family's reference spec) and [EL-083](EL-083-escaping-character.md). Same declaration shape as EL-082; the differences are declared values only: larger and heavier bodies, drive values, the eye's channel (Blue) and threshold, the emitted channel (Red), a tail block, colours. Not a duplicate. |
| Planned package | id `predator_character`, directory `elements/PredatorCharacter/`, class `PredatorCharacterElementDeclaration` |
| Refines / extends | No CAT spec |
| Status | not started; blocked on the engine programme (owner decision 3a), then the package-joints-on-a-dynamic-base story (coordinator decision 16) and ENGINE-OPTICAL, then S7 |

## 2. Declaration

**What the part is.** A larger EL-082 toy whose eye accepts blue light and whose front lamp emits red. While blue light on its eye is above the threshold, a declared supply contact closes and its driven roller rolls it forward. It never addresses a target by name, id or kind: the only way to attract it is to put blue light on its eye. "Reachable routes" are physical: whatever stands in its way stops it by contact. No controller, state machine, path planning, perception query or update loop.

Interfaces, kinematic mode, domains and footprint: as [EL-082 section 2](EL-082-lured-character.md#2-declaration) (`IHingeElement`, `ISupplyElement`, `ISignalElement`, `ISceneGeometryElement`, the ENGINE-OPTICAL emitter and receiver contracts; `KinematicMode.Dynamic`; 3 bodies, 3 moving, 2 revolute constraints).

### Bodies and colliders

Local frame: origin at the chassis box centre, +X forward. At rest the origin is 0.50 m above the supporting surface.

| Body | Colliders (m) | Mass and inertia | Contact material | Optical response |
| --- | --- | --- | --- | --- |
| 0 chassis (dynamic base) | box half extents (0.40, 0.25, 0.225); eye sphere r 0.05 at (0.45, 0.16, 0); lamp sphere r 0.05 at (0.45, −0.05, 0); tail box half extents (0.05, 0.05, 0.10) at (−0.45, −0.10, 0) | 2.5 kg; box moments m/12·(b² + c²): (0.094, 0.176, 0.185) kg·m²; gravity (0, −9.81, 0) | restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s | box and tail: absorb; eye and lamp spheres: transmit every channel |
| 1 rear roller at (−0.28, −0.38, 0); 2 front roller at (+0.28, −0.38, 0) | cylinders r 0.12, half-width 0.21, axis Z | 0.25 kg each; axial ½·m·r² = 0.0018; transverse m·(3r² + w²)/12 with w 0.42 = 0.0046 kg·m² | restitution 0.1, friction 0.8, rolling resistance 0, bounce threshold 0.1 m/s | absorb |

Geometry check: roller tops at −0.26, 1 cm below the chassis underside at −0.25; roller bottoms at −0.50; rollers 0.42 m wide inside the 0.45 m chassis depth. Both spheres stand wholly outside the box, tangent to its front face. Total mass 3.0 kg (**starting value**: twice EL-082, so a light toy cannot push it aside); overall 1.00 × 0.75 × 0.45 m.

Sight-line check: the eye is 0.66 m above the floor, level with the prey toys' crest emitters (0.66 m), so the horizontal line between them clears a prey toy's absorbing box (top at 0.61 m) at every distance. The lamp is at 0.45 m, close to EL-083's eye (0.49 m), and in front of this part's own box, so nothing of this part shades it forward. The EL-088 fish (0.14 m) is seen downward through the tank's glass.

### Hinges and drive

Two continuous `RevoluteHingeDescriptor` on `Support: new(0)` about Z, friction torque 0.12 N·m each. Hinge 0 (rear roller, trailing and loaded when rolling forward) carries the `RevoluteDriveDescriptor` (`A/MechanicalContractValues.cs:138`): `TargetSpeed` fixed −10 rad/s (rolling toward +X at 10 × 0.12 = 1.2 m/s), `MaxTorque` fixed 0.72 N·m.

### Optics, signal and supply

Conventions as EL-082: ordinary channels, watts, one receiver law (owner decision 9 and coordinator decision 16, 22 November 2026).

- **Receiver `eye`** on body 0 at (0.45, 0.16, 0): disc r 0.05 m (A = 0.00785 m²) facing +X, required channel **Blue**, threshold **2.5 × 10⁻⁵ W**, release ratio 0.9; published as a level signal node.
- **Emitter** on body 0 at the lamp (0.45, −0.05, 0): all directions, range 2.5 m, 2 W **Red** (I = 0.159 W/sr), demand 2 W. EL-083 sees it.
- **Supply nodes** as EL-082: `source`; `sight` (contact on the `eye` level); consumers `drive` (target `SupplyDriveTarget(Revolute, 0)`) and `glow` (the emitter). Edges: `source → sight → drive`, `source → glow`.
- Electrical ratings (read by nothing): `drive` 12 V, 20 Ω, 7.2 W; `glow` 12 V, 72 Ω, 2 W.

### Ports

None. The earlier optional `ActivationOut` on a "catch" is dropped (section 6).

### Parameters and store

| Key | Kind, unit | Min | Max | Default | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- |
| `capacity` | Real, Joule | 120 | 1920 | 480 | Reject | glow 2 W plus the 7.2 W drive peak for a 30 s Run is 276 J |
| `power_limit` | Real, Watt | 10 | 40 | 20 | Reject | above the 9.2 W peak; 20 W is 0.17 J per tick, under the 1 J per tick bound |
| `initial_charge` | Real, none | 0 | 1 | 1 | Reject | 0 gives the "run-down" control |
| `enabled` | Choice `Disabled`, `Enabled` | 0 | 1 | 1 | Reject | Build-only |

### Metadata and bindings

Category `Motion`, display name "Prowler", palette order 177 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour `#45639c`. Bindings as EL-082: two roller body bindings, the eye glow on `CommittedActivation` of `eye`, the charge mark on `CommittedCharge` of `source`.

### Art and solidity

| Node | Mesh | Layer and class |
| --- | --- | --- |
| `Visual/Body` | blue `#45639c` chamfered box with a cream `#fff8e9` printed muzzle | solid, box collider; follows body 0 |
| `Visual/Eye` | clear gold-tinted `#f7cb52` dome r 0.05 on the front face | solid, sphere collider |
| `Visual/EyeGlow` | emissive disc inside the dome | decorative: enclosed, recoloured by a binding |
| `Visual/Lamp` | clear coral-tinted `#de7058` ball r 0.05 below the eye | solid, sphere collider |
| `Visual/Tail` | blue block 0.10 × 0.10 × 0.20 on the back face | solid, box collider |
| `Visual/RollerRear`, `Visual/RollerFront` | navy `#293954` cylinders with printed spokes | solid, cylinder colliders; follow bodies 1 and 2 |
| `Visual/ChargeMark` | flush printed bar | decorative: printed flat |

Friendly toy proportions, not menacing. Icon: a long box on two wheels with a tail block and two front lenses. It differs from EL-082 and EL-083 by its length, its tail and having no crest.

### Behaviour and numbers

1. **Traction.** Drive force at the rear contact (0.72 − 0.12)/0.12 = 5.0 N; front roller drag 0.12/0.12 = 1.0 N; net 4.0 N; acceleration 4.0/3.0 = 1.33 m/s²; 1.2 m/s in 0.9 s. Centre of mass height (2.5·0.50 + 0.5·0.12)/3.0 = 0.437 m; wheelbase 0.56 m; load transfer 3.0·1.33·0.437/0.56 = 3.1 N; the rear axle carries 14.7 + 3.1 = 17.8 N. Friction is the geometric mean with the bench, √(0.8 × 0.3) = 0.49 (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119`; bench 0.3 at `engine/gpu/WorkshopPhysicsCompiler.cs:101`): limit 0.49·17.8 = 8.7 N, above the 5.0 N asked.
2. **Slopes.** 4.0 N balances 29.4·sin θ at 7.8°; it stalls on anything steeper.
3. **Stopping.** Brake 2·0.12/0.12 = 2.0 N, 0.67 m/s²: from 1.2 m/s it stops in 1.8 s over 1.08 m.
4. **What it sees.** Blue light at or above 25 µW on the eye. A prey toy's crest (2 W blue, I = 0.159 W/sr, range 5 m) reads 0.00785·0.159/25 = 5.0 × 10⁻⁵ W on axis at 5 m, so the crest's range ends the sighting; the threshold angle is 60° off axis at 5 m. A prey toy whose source is disabled or spent emits nothing and is not seen.
5. **Barrier.** Stalled push 5.0 N. A Wall is static, so it stops there and the stalled drive does no work (`T/RevoluteDriveTests.cs:307`). It presses for as long as it is lit; it does not climb, path-plan or route around.
6. **Catch.** Reaching prey is ordinary contact: it bumps the prey and, still seeing it, pushes it at up to 5.0 N (EL-082 brakes with 1.0 N, so it is shoved along). Nothing is destroyed, scored or emitted.
7. **Glass.** Its chassis restitution 0.1 times glass 0.3 is 0.03 (the engine multiplies restitutions, `WorkshopConstraintSolver.cs:117`). Ramming the EL-088 tank at 1.2 m/s delivers at most 3.0·1.2·1.03 = 3.7 N·s, far below that tank's 16 N·s threshold: it presses on the glass and the fish stays inside.
8. **Slower than its prey.** 1.2 m/s against EL-083's 1.5 m/s, so an escape is possible.
9. **Designed interplay.** A blue or white Flashlight or a blue laser on its eye draws it as prey does. Its 2 W red lamp is too weak to trip a delivered colour receiver (0.15 W at 1 m against a 0.5 W minimum threshold).
10. **Power.** Drive peak 0.72 × 10 = 7.2 W, cruise about 0.24 N·m × 10 = 2.4 W; lamp 2 W.
11. **Tick order.** As EL-082: the optical solve runs after the last substep; readings act from the next tick.
12. **Tipping.** Pitch past atan(0.28/0.437) = 33° topples it; nothing holds it upright.

## 3. Engine capabilities

| Capability | Status | Evidence |
| --- | --- | --- |
| Supplied revolute velocity drive within a torque bound | present | `RevoluteDriveTests.ASuppliedDriveSpinsTheRotorUpAtItsTorqueBoundThenHoldsTheTargetSpeed` (`T/RevoluteDriveTests.cs:176`) |
| A stalled drive does no work | present | `RevoluteDriveTests.AStalledDriveDoesNoWorkWhileItsShaftIsBlocked` (`T/RevoluteDriveTests.cs:307`) |
| Cylinder rolling by friction | present | `WorkshopPhysicsLawCylinderTests.ACylinderRollsDownARampAtTwoThirdsGSinThetaWithoutSlipping` (`T/WorkshopPhysicsLawCylinderTests.cs:180`) |
| Package-internal source, signal-controlled contact and consumer | present | `ComposableSupplyRoutingTests.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`T/ComposableSupplyRoutingTests.cs:530`); `StateLatchTests.Network_ContactClosedFollowsCommittedLatchMemoryAndCounterReach` (`T/StateLatchTests.cs:473`) |
| Driven hinges carried by a dynamic base body of a package | present, untested | admitted by `CuriousContraptions.Bundle/BundleValidator.cs:617` and `engine/gpu/WorkshopPhysicsCompiler.cs:199`; no package or test uses it. Qualified by **the package-joints-on-a-dynamic-base story** (coordinator decision 16), an unconditional prerequisite |
| Drive and emitter work debit the source; an exhausted source opens the route | missing | ENGINE-SUPPLY-DEMAND (owner decision 3a) |
| Optical emitter, per-collider surface response, watt-threshold receiver published as a level signal node read by a signal-state contact | missing | ENGINE-OPTICAL (`engine-optical-propagation`) |
| Occlusion candidates without a scene scan | missing | ENGINE-SPATIAL-QUERIES (owner decision 3a) |

The package-joints-on-a-dynamic-base story is cited in its corrected form (coordinator decision 16; audit E0); its current text is [the story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#story-package-joints-on-a-dynamic-base) (key `engine-characters`).

## 4. Sources and legacy

- Requirement: "Original character responds to declared perceivable stimuli and reachable routes"; outcome "A barrier prevents traversal; target names cannot bypass sensing" ([element-087](../requirements.md#element-087)).
- Research reference: TIM2 manual ([todo-221](../requirements.md#todo-221)). The element-map note "predator perception, pursuit and contact policy" is met by the declared receiver, drive and plain contact; no policy table exists.
- Legacy search at `a6c914e` for "predator", "cat", "pursu", "prey" and "character" found no predator source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Visibility is a finite zero-thickness segment, not a collision body | `engine/physics/SegmentOcclusionSweep.cs@a6c914e:L5-L22` | Carry forward the rule through ENGINE-OPTICAL. Do not carry forward the CPU sweep. |
| L2 | Clear shells pass light while opaque collars block it | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L82-L96` | Carry forward as the occlusion rule |

**Files harvested:** `engine/physics/SegmentOcclusionSweep.cs`, `CuriousContraptions.tests/PipeTests.cs` (occlusion test only).

## 5. Acceptance outline

Requirement row: [element-087](../requirements.md#element-087). Planned class `PredatorCharacterTests`. "Prey" is an EL-082 at its defaults in a scene with no green light, so it stands still and glows blue. "Stays" means under 0.05 m over 5 s.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| P1 | Given prey 3 m ahead in clear view, when run, then it rolls to the prey and touches it | `VisiblePreyIsPursuedAndTouched` | no prey: stays | gap between its eye and the prey under 0.02 m at some tick within a 15 s guard (about 3.5 s expected) |
| P2 | Given an opaque Wall hiding the prey, then it stays | `HiddenPreyIsNotPursued` | P1 | Wall taller than both toys |
| P3 | Given a Wall 0.45 m high between them, over which the eye (0.66 m) sees the prey's crest (0.66 m), then it rolls to the Wall and is stopped there | `ABarrierItCanSeeOverStillStopsIt` | P1 | it never passes the Wall and never touches the prey in 10 s; drift under 0.02 m once stopped |
| P4 | Given an EL-088 tank 3 m ahead, then it sees the fish through the glass, rolls to the tank and presses on it; no pane breaks and the fish stays inside | `GlassPassesSightAndStopsTheBody` | an opaque Wall box around the tank: stays | every pane unbroken after 10 s |
| P5 | Given a Basketball where the prey stood, then it stays | `ABodyThatEmitsNoBlueLightIsIgnored` | P1 | — |
| P6 | Given the same prey toy enclosed by opaque Walls on every side the eye could see, then it stays; and given the same prey toy with `enabled` Disabled in clear view, then it stays | `PreyIsFoundOnlyByItsLightNeverByIdentity` (theory) | P1 | same part, same id, same position as P1 |
| P7 | Given prey directly behind it, then it stays | `PreyBehindTheEyeIsNotSeen` | P1 | — |
| P8 | Given an EL-083 2 m ahead, then the EL-083 backs away from this part's red lamp | `ItsLampDrivesTheEscaperAway` | this part behind an opaque Wall: the EL-083 stays | EL-083 moves at least 0.5 m away within 5 s |
| P9 | Given `initial_charge` 0 or `enabled` Disabled, then it stays with prey in view and its lamp emits nothing | `ARunDownOrDisabledToyNeitherMovesNorGlows` (theory) | P1 | — |
| P10 | Reset restores the authored pose, roller travel and store; save and load keep the four parameters | `ResetAndSaveLoadRestoreTheToy` | — | generic: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`T/CatalogueIntegrationTests.cs:273`) |
| P11 | A `capacity` of 60 J or 2400 J is refused; a connection naming this portless part is refused | `OutOfRangeStoreAndAnyWireAreRefused` | defaults accepted | typed refusal, nothing changed |
| P12 | Given P1, then the source's debit over the run equals the drive's committed work plus the emitter's paid energy | `TheSourcePaysForTheDriveAndTheLamp` | — | never below that total and above it by no more than the ledger residual |
| P13 | Given `initial_charge` 0.02 (9.6 J) and prey 4.5 m ahead, then it stops short for good and its lamp goes out | `AnExhaustedSourceStopsTheDrive` | full charge: it reaches the prey | 9.6 J at about 5 W lasts 2 s: stopped more than 1 m short of the prey, within a 15 s guard; the route reads open |

### Integration

- Collaborators: EL-082 and EL-083 (prey; EL-083 also reads this part's red lamp), EL-088 Fish tank lure (blue light behind glass), CAT-066 Wall (occluder and barrier, delivered), CAT-001 Basketball (non-prey control, delivered), the Flashlight and blue laser of the optical catalogue (they draw it too).
- Campaign: chapter 10, levels 91–100, "walker/lure/predator/fragile-target roles" (`docs/planning/requirements.md:2295`); coverage ledger, reuse 136–150 (`:2411`). First taught at **97**, with EL-082 and EL-083 in one slice and before the Fish tank lure (97b) that needs it (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)).
- A level that fixes it in place needs the lesson-fixture story (S8).

### Delivery

- **Shape.** Family slice "Sighted walkers", last in the slice (its rows use both siblings).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS, ENGINE-ISLANDS-SLEEP, ENGINE-SUPPLY-DEMAND and ENGINE-CAPABILITY-INSTANCES; then the package-joints-on-a-dynamic-base story (coordinator decision 16) and ENGINE-OPTICAL; then S7. EL-082 and EL-083 in the same slice; row P4 waits for EL-088.
- **Names.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/PredatorCharacter/` (project, declaration, `predator_character.tscn`, `icon.svg`); `CuriousContraptions.tests/PredatorCharacterTests.cs`; one `CuriousContraptions.slnx` line; the coverage binding once S7 admits it.
- **Review tier.** Engine tier (one *present, untested* row).
- **Disqualifiers the probe re-checks.** Those of EL-082.

## 6. Open questions

All resolved.

1. **Stimulus.** Blue light on the ordinary Blue channel (coordinator decision 16); blue lamps and lasers draw it too, by design.
2. **"Reachable routes" means stopped by contact.** It cannot turn, it stalls above 7.8°, and it presses on a barrier for as long as it is lit. It never routes around anything. Decided under the standing delegation: a route planner is the per-agent logic the owner forbids.
3. **Catch outcome.** Plain contact; no `Caught` occurrence, no `ActivationOut`, no rest state (D19). A level that must react to a catch places a contact sensor part; a level that must forbid one uses a goal of the goal engine slices.
4. **Sight through glass.** It sees the tank's fish and is stopped by the glass.
5. **Lamp range 2.5 m, prey crest range 5 m** (D1): prey is noticed from further than the hunter frightens.
6. **Name and art** "Prowler" (D9); part-body cream is `#fff8e9` (owner decision 4, 22 November 2026).

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** The EL-082 declaration with other values. No controller, state machine, path planner, target lookup or engine test on this package.
2. **Generic laws.** Optical reception (ENGINE-OPTICAL), the supply-network rule, the revolute drive law, hinge friction, contact and friction.
3. **Three layers.** Colliders: two boxes, two spheres, two cylinders, each with a declared optical response. Visuals: one mesh per collider and two flat or enclosed marks. Animation: eye glow and charge mark recoloured from committed state. Everything else follows a body pose.
4. **Flow and typing.** Light → observation → signal node → signal-state contact → drive → physics; nothing returns. Channels and the enable choice are enums; no string names a target.
5. **Flat state.** Bodies 3 (all dynamic); colliders 6 (2 boxes, 2 spheres, 2 cylinders); joints 2 revolute; drives 1; sensors 1 optical receiver; emitters 1; signal nodes 1; supply nodes 4; stores 1; presentation bindings 8.
6. **Performance.** Six primitive colliders: one more than EL-082, for the visible tail that makes the silhouette. Slow body; occlusion candidates from ENGINE-SPATIAL-QUERIES. No sleeping exists today; ENGINE-ISLANDS-SLEEP will let an unlit toy at rest sleep, and nothing here depends on it.
7. **Tests are scenarios.** Every row is a placed scene in the production simulation.

**Verdict: conforms.** Not deliverable until the engine programme and the two named stories exist; each missing capability has one owning story.

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here because the reader fails the whole coverage run on a spec whose fields do not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `predator_character` |
| Named identity | EL-087, `../invest/named-elements.md#element-087` |
| Acceptance | `todo-221` and `sequence-task-640` (`docs/planning/requirements.md:5093`) |
| Decision | S657; `scope-corrections.md` has no `s657` block |
| Mode records | `Configuration=Fixed` |

What it lacks today: a decision block for S657; `todo-221` is a scope index shared by EL-086, EL-087 and EL-088 (`docs/planning/requirements.md:4095`), and its sequence task sits beside the element row, not directly after the todo anchor as the reader requires.
