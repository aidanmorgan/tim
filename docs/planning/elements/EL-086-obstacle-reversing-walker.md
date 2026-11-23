# EL-086 · Obstacle-reversing walker — named-identity spec

**Current delivery (24 October 2026):** [EL-086 delivery slice](../invest/named-elements.md#element-086). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `T/` abbreviates `CuriousContraptions.tests/` and `A/` abbreviates `CuriousContraptions.Abstractions/`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32. Nothing here was built or simulated; every number is a hand calculation shown beside it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-086 · Obstacle-reversing walker · Character |
| Anchor | [requirements.md#element-086](../requirements.md#element-086); [named entry](../invest/named-elements.md#element-086); scope index [todo-221](../requirements.md#todo-221) |
| Proof owner | S656 |
| Disposition | To deliver as a single element. It shares the rolling chassis of the "Sighted walkers" family ([EL-082](EL-082-lured-character.md)) but is not a member: it has no eye, and it adds two sprung bumpers, a memory node and a second drive. Those are structure, not values. |
| Planned package | id `walker`, directory `elements/Walker/`, class `WalkerElementDeclaration` |
| Refines / extends | No CAT spec. Memory pattern of [CAT-037 State latch](CAT-037-latch.md); sprung slider of [CAT-062 Springboard](CAT-062-spring.md); endpoint contact of [CAT-039 Linear pusher](CAT-039-linear_pusher.md) |
| Status | not started; blocked on the engine programme (owner decision 3a), then the story "Package joints on a dynamic base" (key `engine-characters`, coordinator decision 16) and ENGINE-SIGNAL-NODES, then S7 |

## 2. Declaration

**What the part is.** A wind-up toy that rolls until one of its bumpers is pushed in, then rolls the other way. There is no controller, state machine in code, lookahead, timer or update loop. The "decision" is declared data: each bumper is a sprung slider with a declared end-of-stroke supply contact; a declared edge node turns each contact's closing into a pulse; the front pulse sets and the rear pulse resets one declared memory node; the memory's level chooses which of two ordinary roller drives is supplied from the toy's own source.

Interfaces: `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `IHingeElement` (`:20`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`A/SceneGeometry.cs:10`). `KinematicMode.Dynamic`; `SupportedDomains = Mechanical | Electrical | Signal`. Footprint: 5 bodies, 5 moving, 2 prismatic and 2 revolute constraints.

### Bodies and colliders

Local frame: origin at the chassis box centre, +X the starting direction. At rest the origin is 0.32 m above the supporting surface. Body indices follow the package rule (sliders, then hinges).

| Body | Colliders (m) | Mass and inertia | Material |
| --- | --- | --- | --- |
| 0 chassis (dynamic base) | box half extents (0.30, 0.15, 0.175) at the origin | 0.6 kg; box moments m/12·(b² + c²): (0.011, 0.024, 0.023) kg·m²; gravity (0, −9.81, 0) | restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s |
| 1 front bumper (slider 0), 2 rear bumper (slider 1) | box half extents (0.02, 0.205, 0.175), at rest centred (±0.34, −0.085, 0) | 0.05 kg each; moments (0.0012, 0.0005, 0.0007) kg·m²; gravity as the chassis | as the chassis |
| 3 rear roller (hinge 0) at (−0.20, −0.24, 0); 4 front roller (hinge 1) at (+0.20, −0.24, 0) | cylinders r 0.08, half-width 0.165, axis Z (collider turned Rx 90°) | 0.15 kg each; axial ½·m·r² = 0.00048; transverse m·(3r² + w²)/12 with w 0.33 = 0.0016 kg·m² | restitution 0.1, friction 0.8, rolling resistance 0, bounce threshold 0.1 m/s |

Geometry check: roller tops at −0.16, 1 cm under the chassis underside at −0.15; roller bottoms at −0.32. Each bumper plate reaches from 0.03 m above the floor (y −0.29) to y 0.12. At rest it spans x 0.32 to 0.36, 0.02 m clear of the chassis end face at 0.30; pushed in its full 0.02 m it lies against that face. It leads the rollers' outermost point (0.28), so anything taller than 0.03 m meets a bumper first. Total mass 1.0 kg; overall 0.72 × 0.47 × 0.35 m (**starting values**).

### Sliders, hinges and drives

Two `PrismaticSliderDescriptor` (`A/MechanicalContractValues.cs:104`), one per bumper, each with its travel axis pointing outward along ±X: lower limit −0.02 m (pushed in), upper limit 0 (rest), stiffness fixed 25 N/m, damping fixed 4 N·s/m (ζ = 4 ÷ (2√(25 × 0.05)) = 1.79, so the trailing bumper does not reach its stop when the toy arrives; behaviour row 3), compression 0, `BodiesCollide` false, no drive. Fully pushed in, the spring pushes back with 25 × 0.02 = 0.5 N.

Two continuous `RevoluteHingeDescriptor` (`A/MechanicalContractValues.cs:149`) on `Support: new(0)` about Z, stiffness and damping 0, `BodiesCollide` false, friction torque 0.03 N·m each. Each carries an ordinary `RevoluteDriveDescriptor` (`:138`), `MaxTorque` 0.18 N·m: hinge 0 (rear roller) `TargetSpeed` −10 rad/s, rolling the toy toward +X; hinge 1 (front roller) `TargetSpeed` +10 rad/s, rolling it toward −X. Each direction is therefore driven by its trailing, loaded roller, and an unsupplied drive applies no torque (`T/RevoluteDriveTests.cs:259`). 10 rad/s at r 0.08 is 0.8 m/s.

### Signal and supply graph

| Node | Kind | Wiring |
| --- | --- | --- |
| `source` | `SupplySourceDescriptor` (`A/SupplyContractValues.cs:39`) | feeds every node below |
| `frontPressed`, `rearPressed` | `SupplyContactDescriptor` with `PrismaticContactControl(Prismatic 0 or 1, CoordinateEndpoint.Lower)` (`A/SignalContactControl.cs:13`), the Linear pusher's endpoint contact | closed while that bumper is within the stop tolerance of fully pushed in |
| `frontEdge`, `rearEdge` | signal edge nodes, rising, reading `frontPressed.Output` and `rearPressed.Output` (ENGINE-SIGNAL-NODES) | `frontEdge.Output → heading.Set`; `rearEdge.Output → heading.Reset` |
| `heading` | `SignalMemoryDescriptor` (`A/SignalContractValues.cs:64`) | Off at Run start: the toy starts toward +X |
| `reverse` | `SupplyContactDescriptor` with `SignalStateContactControl(heading, Latched)`, the State latch's form (`elements/StateLatch/StateLatchElementDeclaration.cs:67`) | `reverse.Output → driveReverse.PowerIn` and `forward.SecondIn` |
| `forward` | `SupplyLogicDescriptor`, `LogicGateOperation.Xor` (`A/SupplyContractValues.cs:41`), `PowerIn` and `FirstIn` from `source` | `forward.Output → driveForward.PowerIn` |
| `driveForward`, `driveReverse` | `SupplyConsumerDescriptor` targeting `SupplyDriveTarget(Revolute, 0)` and `(Revolute, 1)` | — |

So `driveReverse` is supplied while `heading` is On and `driveForward` (source XOR reverse) while it is Off. A simultaneous Set and Reset resolves to Off, the memory node's own rule (`StateLatchElementDeclaration.cs:56`). Electrical ratings (read by nothing): each drive 12 V, 80 Ω, 1.8 W.

### Ports

None.

### Parameters and store

| Key | Kind, unit | Min | Max | Default | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- |
| `capacity` | Real, Joule | 20 | 240 | 60 | Reject | 60 J is 33 s at the 1.8 W peak |
| `power_limit` | Real, Watt | 5 | 20 | 10 | Reject | 0.083 J per tick, under the 1 J per tick supply bound |
| `initial_charge` | Real, none | 0 | 1 | 1 | Reject | 0 gives the "run-down" control |
| `enabled` | Choice `Disabled`, `Enabled` | 0 | 1 | 1 | Reject | Build-only |

Store: the one supply source. No sensitivity or starting-direction parameter (section 6).

### Metadata and bindings

Category `Motion`, display name "Wind-up walker", palette order 179 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour `#d69c47`. Presentation: body bindings `Visual/BumperFront` → `PackageBodyIndex(1)`, `Visual/BumperRear` → `(2)`, `Visual/RollerRear` → `(3)`, `Visual/RollerFront` → `(4)`; `Visual/KeyDisc` `LocalRotationZ` on `PresentationDriver.CommittedRevoluteAngle` of hinge 0 (`A/ElementPresentation.cs:30`); `Visual/EyesFront` and `Visual/EyesRear` albedo on `CommittedActivation` of `heading` (front eyes lit while Off, rear eyes while On).

### Art and solidity

| Node | Mesh | Layer and class |
| --- | --- | --- |
| `Visual/Body` | ochre `#d69c47` chamfered box 0.60 × 0.30 × 0.35 | solid, box collider; follows body 0 |
| `Visual/BumperFront`, `Visual/BumperRear` | cream `#fff8e9` plates 0.04 × 0.41 × 0.35 | solid, box colliders; follow bodies 1 and 2 |
| `Visual/EyesFront`, `Visual/EyesRear` | navy `#293954` dots printed on the bumper faces | decorative: printed flat, recoloured by a binding |
| `Visual/RollerRear`, `Visual/RollerFront` | navy cylinders with printed spokes | solid, cylinder colliders; follow bodies 3 and 4 |
| `Visual/KeyDisc` | gold `#f7cb52` disc with a printed key shape, flush in the +Z side face | decorative: flush, turned by a binding |

Icon: a low box between two end plates on two wheels, with a key mark. It differs from the sighted walkers by its two plates and its low shape, and has no eye or crest.

### Behaviour and numbers

1. **Traction.** Drive force (0.18 − 0.03)/0.08 = 1.875 N; other roller's drag 0.03/0.08 = 0.375 N; net 1.5 N; effective mass 1.0 + 2 × 0.00048/0.08² = 1.15 kg (the two rollers' spin), so acceleration 1.5/1.15 = 1.30 m/s² and 0.8 m/s in 0.62 s. Centre of mass height (0.6·0.32 + 0.1·0.235 + 0.3·0.08)/1.0 = 0.24 m; wheelbase 0.40 m; load transfer 1.0·1.30·0.24/0.40 = 0.78 N onto the trailing, driven axle: 4.9 + 0.78 = 5.7 N. Friction is the geometric mean with the bench, √(0.8 × 0.3) = 0.49 (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119`; bench 0.3 at `engine/gpu/WorkshopPhysicsCompiler.cs:101`): limit 0.49·5.7 = 2.79 N against 1.875 N, in either direction. Peak drive power 1.8 W is 0.015 J per tick, inside the toy's own 0.083 J per-tick allowance, so coordinator decision 27's limit never scales it.
2. **What reverses it (spring, slider stop and contact laws).** An obstruction is anything that pushes a bumper in to its stop, which takes 0.5 N. A Wall does: the toy keeps 1.5 N of net push behind the bumper, three times the spring. That holds whatever the arrival speed, so a toy started touching a Wall, or arriving at a crawl, reverses just as one arriving at 0.8 m/s does. A Wall it has not reached pushes nothing, so it cannot reverse early.
3. **What does not reverse it.** A body beside its path; a lip under 0.03 m; its own acceleration (the bumper's inertia is 0.05 × 1.30 = 0.065 N, moving it 0.065/25 = 2.6 mm of its 20 mm); a body so light that pushing it along never takes 0.5 N; the arrival stop: the trailing bumper's peak inward travel is 8.6 mm at 0.8 m/s (ζ 1.79). Hand calculation: when the chassis stops on the front bumper's stop, the trailing bumper (0.05 kg, 25 N/m, 4 N·s/m) starts inward at up to 0.8 m/s; its roots are (−4 ± √(16 − 5))/0.1 = −6.83 and −73.2 1/s, so its travel 0.8/66.3 × (e^(−6.83t) − e^(−73.2t)) peaks at t = ln(73.2/6.83)/66.3 = 0.036 s at 0.0121 × (0.783 − 0.073) = 8.6 mm, well short of its 20 mm stop. With 1 N·s/m (ζ 0.447) it would reach 0.8/22.4 × e^(−0.5 × 1.107) = 20.6 mm, close its contact about 7 ticks after the front one and reset the memory (row W14).
4. **Tick order.** The bumper reaches its stop in one tick's physics; the endpoint contact reads the committed coordinate in the next tick's supply solve (within 0.5 mm of the stop, `A/PhysicalTolerance.cs:14`); in that same solve the edge node pulses, the memory changes, `reverse` and `forward` swap and the other drive is supplied (signal and supply routing are same-tick). One tick from stop to reversed torque; it leaves at 1.30 m/s², the released bumper springs out, and the memory holds the new heading.
5. **Between two Walls 4 m apart** it travels about 3.3 m per leg: 0.8²/(2 × 1.30) = 0.25 m accelerating in 0.62 s, then 3.05 m at 0.8 m/s in 3.8 s, about 4.4 s per leg.
6. **Between two obstacles closer than its travel** it shuttles, reversing at each bumper press; both bumpers can be in together only if the obstacles are closer than 0.72 − 2 × 0.02 = 0.68 m, which a placed Wall cannot be (the toy is 0.72 m long at rest). With Walls 0.74 m apart each leg is 0.06 m: 0.04 m free (0.25 s at 1.30 m/s², reaching 0.32 m/s) and 0.02 m pressing the bumper in, which the 1.5 N drive completes against at most 0.5 N of spring and a damper that lets it creep in at (1.5 − 0.5)/4 = 0.25 m/s even from rest; so it reverses roughly every 0.3–0.4 s.
7. **Slopes.** 1.5 N balances 9.81·sin θ at 8.8°: it stalls on steeper ground. On an up-slope steeper than 5.8° a crawl arrival no longer trips the bumper (1.5 − 9.81·sin θ falls below 0.5 N); an arrival at speed still does. An up-ramp steeper than about 11° meets the bumper's lower edge (0.03 m up, 0.16 m ahead of the roller's contact) and reverses it.
8. **Edges.** It has no edge sensor: it rolls off a ledge and falls.
9. **Power.** Peak 0.18 × 10 = 1.8 W; cruise about 0.06 × 10 = 0.6 W.

## 3. Engine capabilities

| Capability | Status | Evidence |
| --- | --- | --- |
| Supplied revolute velocity drive, either sign | present | `RevoluteDriveTests.ASuppliedDriveSpinsTheRotorUpAtItsTorqueBoundThenHoldsTheTargetSpeed` at ±6 rad/s (`T/RevoluteDriveTests.cs:176`) |
| An unsupplied drive applies no torque | present | `RevoluteDriveTests.AnUnsuppliedDriveAppliesNoImpulseAndDoesNoWork` (`T/RevoluteDriveTests.cs:259`) |
| Sprung slider held between its stops | present | `SliderInstanceTests.ThePlateHoldsItsLowerAndUpperStops` (`T/SliderInstanceTests.cs:61`) |
| Supply contact closed while a slider is at a declared endpoint, read on the next electrical tick | present | `LinearPusherTests.BothEndpointConsumersFollowDepartureReturnAndSupplyRemoval` (`T/LinearPusherTests.cs:65`) |
| Memory node whose level closes a supply contact | present | `StateLatchTests.Network_ContactClosedFollowsCommittedLatchMemoryAndCounterReach` (`T/StateLatchTests.cs:473`) |
| Package-internal supply edges from a source through a contact | present | `ComposableSupplyRoutingTests.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`T/ComposableSupplyRoutingTests.cs:530`) |
| Cylinder rolling by friction | present | `WorkshopPhysicsLawCylinderTests.ACylinderRollsDownARampAtTwoThirdsGSinThetaWithoutSlipping` (`T/WorkshopPhysicsLawCylinderTests.cs:180`) |
| Hinges and sliders carried by a dynamic base body of a package | present, untested | admitted by `CuriousContraptions.Bundle/BundleValidator.cs:616`–`:619` and `engine/gpu/WorkshopPhysicsCompiler.cs:199`; no package or test uses it. Qualified by the story **"Package joints on a dynamic base"** (key `engine-characters`, coordinator decision 16), an unconditional prerequisite |
| Two driven hinges in one package, each with its own consumer | present, untested | one `RevoluteDriveDescriptor` per hinge and one consumer per driven hinge are the declared shape; no package has two |
| A supply logic gate and signal edges wired inside one package | present, untested | `SupplyLogicDescriptor` and `ISignalElement.SignalEdges` exist; the internal-edge tests cover a contact and a store only |
| A signal edge node reading a supply level inside its own package | missing | ENGINE-SIGNAL-NODES (edge; owner decision 3a) |
| Drive work debits the source; an exhausted source opens the route | missing | ENGINE-SUPPLY-DEMAND (owner decision 3a) |

Nothing is asked of a commanded-direction drive or of a contact-speed sensor: two ordinary drives and two sprung bumpers do the work. The package-joints-on-a-dynamic-base story is cited in its corrected form (coordinator decision 16; audit E0); its current text is [the story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#story-package-joints-on-a-dynamic-base) (key `engine-characters`).

## 4. Sources and legacy

- Requirement: "Locomotion reverses after sensed actual obstruction"; outcome "Distant obstacle does not reverse it early" ([element-086](../requirements.md#element-086)).
- Research reference: TIM2 manual ([todo-221](../requirements.md#todo-221)).
- Legacy search at `a6c914e` for "walker", "reverse", "obstacle" and "character" found no walker source; the reverse-transmission hits concern shaft direction.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Impact-switch threshold default 0.8 m/s approach speed, typed and range-checked 0–64 | `engine/gpu/WorkshopActivationParts.cs@a6c914e:L6-L10` | Not carried forward for this part: obstruction is sensed by force on a sprung bumper, so a slow arrival is sensed too |

**Files harvested:** none deleted (only a surviving file is cited).

## 5. Acceptance outline

Requirement row: [element-086](../requirements.md#element-086). Planned class `WalkerTests`. Velocity is the chassis's committed X velocity; "at a Wall" means the leading bumper face is within 0.02 m of the Wall face.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| W1 | Given Walls 4 m apart with the toy between them, when run, then it reverses at each Wall in turn | `ItReversesAtEachWallInTurn` | no Walls: its velocity never turns negative in 5 s | at least three reversals within a 25 s guard (about 13 s expected); every sign change happens at a Wall |
| W2 | Given a Wall 3 m ahead, then before its first contact its velocity is never negative | `ADistantWallDoesNotReverseItEarly` | — | checked on every tick before the bumper is first within 0.02 m of the Wall |
| W3 | Given a Wall standing 0.1 m beside its flank along its whole path, then it passes without reversing | `AWallBesideItsPathIsNotAnObstruction` | W1 | 5 s |
| W4 | Given W1's first arrival, then its velocity is negative soon after | `ReversalFollowsThePress` | — | negative within 0.5 s of the first tick the front bumper is at its stop |
| W5 | Given the toy started with its front bumper 0.01 m from a Wall, or touching it, then it still reverses | `ASlowArrivalStillReverses` (theory) | — | arrival speed √(2·1.30·0.01) = 0.16 m/s or zero; negative velocity within a 2 s guard |
| W6 | Given a Tennis ball rolled into its front bumper at 1.5 m/s closing speed, then it reverses; given a ball crossing 0.3 m ahead of it without touching, it does not | `OnlyAPushedBumperCountsAsAnObstruction` | — | hand check at 4 N·s/m: ball and bumper (0.40 kg) against the chassis (reduced mass 0.29 kg, ζ 0.74, ω 9.2 1/s) compress about 1.5/6.2 × 0.444 × 0.673 = 0.07 m, past the 0.02 m stroke, so the bumper reaches its stop |
| W7 | Given Walls 0.74 m apart with the toy between them, then it shuttles inside the gap: every velocity sign change happens with one bumper within 0.5 mm of its stop, and the chassis never leaves the gap | `BetweenCloseWallsItShuttles` | W1 | 5 s; at least four reversals (behaviour row 6 predicts one every 0.3–0.4 s); the row fails if the toy stalls with both bumpers in, chatters away from a stop or reverses without a press |
| W8 | Given a bench edge ahead and no Wall, then it rolls off and falls | `ItHasNoEdgeSense` | W1 | its height drops by more than 0.5 m within a 10 s guard |
| W9 | Given `initial_charge` 0 or `enabled` Disabled, then it stays | `ARunDownOrDisabledToyDoesNotMove` (theory) | W1 | under 0.05 m over 5 s |
| W10 | After two reversals, Reset restores the authored pose, both bumpers at rest, the memory Off, roller travel and store; save and load keep the four parameters | `ResetAndSaveLoadRestoreTheToy` | — | after Reset it sets off toward +X again; generic: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`T/CatalogueIntegrationTests.cs:273`) |
| W11 | A `capacity` of 10 J or 300 J is refused; a connection naming this portless part is refused | `OutOfRangeStoreAndAnyWireAreRefused` | defaults accepted | typed refusal, nothing changed |
| W12 | Given W1, then the source's debit over the run equals the two drives' committed work | `TheSourcePaysForTheDrives` | — | never below that work and above it by no more than the ledger residual |
| W13 | Given `initial_charge` 0.05 (3 J) between the Walls, then it stops for good | `AnExhaustedSourceStopsTheDrive` | full charge still walking at the same time | 3 J at about 0.6 W lasts 5 s: stopped within a 12 s guard; the route reads open |
| W14 | Given W1's first arrival, then the memory changes exactly once and the trailing bumper's travel stays under 0.015 m | `OneArrivalOneReversal` | W5's crawl arrival (0.16 m/s): also exactly once | over the 0.5 s after the front bumper first reaches its stop; predicted peak trailing travel 8.6 mm at 0.8 m/s and 1.7 mm at 0.16 m/s (behaviour row 3); at the earlier 1 N·s/m damping it reaches its stop (20.6 mm) and the memory changes twice, which this row catches |
| W15 | Given a standing Domino 2 m ahead, broad face to the toy, then the toy reverses at it | `ADominoIsAnObstruction` | no Domino: its velocity never turns negative in 5 s (W1's control) | velocity negative within 0.5 s of the bumper's first contact with the Domino; whether the Domino slides or topples is not asserted. Hand check: the damper passes up to 4 × 0.8 = 3.2 N at arrival, above the 1.1–2.1 N that topples the 0.4 kg Domino, so it may topple; but chassis against bumper and Domino (reduced mass 1.10 × 0.45 ÷ 1.55 = 0.32 kg, ζ 0.71, ω 8.85 1/s) compresses about 0.8/6.25 × 0.457 × 0.705 = 0.041 m before the relative motion ends, past the 0.02 m stroke, so the bumper reaches its stop in that first contact |

### Integration

- Collaborators: CAT-066 Wall, CAT-023 Domino, CAT-064 Tennis ball (all delivered) as obstacles; the bench edge.
- Campaign: chapter 10, levels 91–100, "walker/lure/predator/fragile-target roles" (`docs/planning/requirements.md:2295`); coverage ledger "walker", reuse 136–150 (`:2411`).
- A level that fixes it in place needs the lesson-fixture story (S8).

### Delivery

- **Shape.** Single element slice. It needs no optics, so it can precede the "Sighted walkers" family and prove the rolling chassis first.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS, ENGINE-ISLANDS-SLEEP, ENGINE-SUPPLY-DEMAND and ENGINE-CAPABILITY-INSTANCES; then the story "Package joints on a dynamic base" (key `engine-characters`, coordinator decision 16) and ENGINE-SIGNAL-NODES (edge node); then S7.
- **Names.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's (the two-input Xor with `FirstIn` and `SecondIn` becomes that story's N-input boolean node).
- **Files.** `elements/Walker/` (project, declaration, `walker.tscn`, `icon.svg`); `CuriousContraptions.tests/WalkerTests.cs`; one `CuriousContraptions.slnx` line; the coverage binding once S7 admits it.
- **Review tier.** Engine tier.
- **Disqualifiers the probe re-checks.** An edge node that cannot read an internal supply contact; a source output that cannot fan out to five inputs; a memory node that cannot be set and reset from inside its own package; a bumper spring that the solver's stop softness keeps more than 0.5 mm from its stop under 1.5 N; a trailing roller that slips at 1.875 N.

## 6. Open questions

All resolved.

1. **Obstruction is sensed by force, not by contact speed.** A sprung bumper with an end-of-stroke contact reverses the toy on any arrival, so it never presses forever on a slow one. The earlier contact-speed sensor and its `bump_speed` setting are dropped; so are the impulse threshold and the 45° cone. Decided under the standing delegation.
2. **Two ordinary drives, one per roller**, instead of a drive with a commanded direction (audit H2).
3. **It cannot turn.** It rolls along its local X only; the player turns the part. A yaw or upright lock for this toy alone would be element logic.
4. **Slope limits**: stalls above 8.8°; a crawl arrival is not sensed on an up-slope above 5.8°; a ramp steeper than about 11° counts as an obstruction.
5. **Between close obstacles it shuttles** (behaviour row 6): both bumpers can be in together only below 0.68 m, which placed Walls cannot reach.
6. **Edge behaviour.** It rolls off (D19): the row asks only for obstruction reversal.
7. **No refractory timer.** The edge node pulses once per press and a repeated Set changes nothing.
8. **No `initial_direction`.** The player turns the part (D19).
9. **The key is a flush printed disc** turned by a binding, not a proud key (audit M7).

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** Bodies, colliders, two sliders, two hinges, two drives, a memory node, two edge nodes, a seven-node supply graph, four parameters and bindings. No state machine in code, lookahead query, timer, callback or engine test on this package.
2. **Generic laws.** The slider's spring and stops; the endpoint supply contact; the signal network's edge and memory rules; the supply network's contact and XOR rules; the revolute drive law; contact and friction.
3. **Three layers.** Colliders: three boxes and two cylinders. Visuals: one mesh per collider. Animation: the flush key disc turns with the committed hinge angle and the eye dots follow the committed memory level; neither touches physics. Bumpers and rollers follow body poses.
4. **Flow and typing.** Physics → committed slider coordinate → supply contact → signal → supply → drive → physics on the next step; nothing returns from presentation. Logic operation, terminals, endpoints and the enable choice are enums.
5. **Flat state.** Bodies 5 (all dynamic); colliders 5 (3 boxes, 2 cylinders); joints 4 (2 prismatic, 2 revolute); drives 2; sensors 0; signal nodes 3; supply nodes 7; stores 1; presentation bindings 11 (4 body, 1 rotation, 6 colour).
6. **Performance.** Five primitive colliders. The two bumper sliders are the sensing mechanism and the visible end plates; they read no contact stream and add no scene pass. Slow body, no tunnelling case. No sleeping exists today; ENGINE-ISLANDS-SLEEP will let a run-down toy at rest sleep, and nothing here depends on it.
7. **Tests are scenarios.** Every row is a placed scene in the production simulation.

**Verdict: conforms.** Not deliverable until the engine programme and the two named stories exist; each missing capability has one owning story.

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here because the reader fails the whole coverage run on a spec whose fields do not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `walker` |
| Named identity | EL-086, `../invest/named-elements.md#element-086` |
| Acceptance | `todo-221` and `sequence-task-639` (`docs/planning/requirements.md:5085`) |
| Decision | S656; `scope-corrections.md` has no `s656` block |
| Mode records | `Configuration=Fixed` |

What it lacks today: a decision block for S656; `todo-221` is a scope index shared by EL-086, EL-087 and EL-088 (`docs/planning/requirements.md:4095`), and its sequence task sits beside the element row, not directly after the todo anchor as the reader requires.
