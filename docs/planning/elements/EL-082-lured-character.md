# EL-082 · Lured character — named-identity spec

**Current delivery (24 October 2026):** [EL-082 delivery slice](../invest/named-elements.md#element-082). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `T/` abbreviates `CuriousContraptions.tests/` and `A/` abbreviates `CuriousContraptions.Abstractions/`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32. Nothing here was built or simulated; every number is a hand calculation shown beside it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-082 · Lured character · Character |
| Anchor | [requirements.md#element-082](../requirements.md#element-082); [named entry](../invest/named-elements.md#element-082); scope index [todo-200](../requirements.md#todo-200) |
| Proof owner | S652 ([decision](../invest/scope-corrections.md#s652)) |
| Disposition | Member of the Family slice **"Sighted walkers"** with [EL-083](EL-083-escaping-character.md) and [EL-087](EL-087-predator-character.md). The three share one declaration shape (a dynamic chassis on two hinge-carried rollers, one optical receiver, one optical emitter, one internal supply source, one switched drive) and differ only in declared values: sizes, masses, which roller is driven and how fast, channels and art. This spec is the family's reference. Not a duplicate of anything delivered. |
| Planned package | id `lured_character`, directory `elements/LuredCharacter/`, class `LuredCharacterElementDeclaration` |
| Refines / extends | No CAT spec. Drive pattern of [CAT-042 Motor](CAT-042-motor.md); source pattern of [CAT-005 Battery](CAT-005-battery.md) |
| Status | not started; blocked on the engine programme (owner decision 3a), then the package-joints-on-a-dynamic-base story (coordinator decision 16) and ENGINE-OPTICAL, then S7 |

## 2. Declaration

**What the part is.** A wind-up toy on two rollers with one eye. There is no controller, state machine, perception query or update loop. The "decision" is three declared things in a row: an optical receiver (the eye) publishes a level while green light on it is above its threshold; that level closes a declared supply contact between the part's own finite source and its roller drive; the driven roller moves the toy by ordinary contact friction. Take the light away and the contact opens.

Interfaces: `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`A/SceneGeometry.cs:10`), plus the emitter and receiver contracts that ENGINE-OPTICAL adds. `KinematicMode.Dynamic`; `SupportedDomains = Mechanical | Electrical | Signal` and the optical domain once it exists. Footprint: 3 bodies, 3 moving, 2 revolute constraints. Closest delivered shape: the Motor (`elements/Motor/MotorElementDeclaration.cs:16`), with a dynamic base instead of a static one.

### Bodies and colliders

Local frame: origin at the chassis box centre, +X forward, +Y up. Placed at rest the origin is 0.41 m above the supporting surface.

| Body | Colliders (m) | Mass and inertia | Contact material | Optical response |
| --- | --- | --- | --- | --- |
| 0 chassis (dynamic base) | box half extents (0.25, 0.20, 0.20) at the origin; eye sphere r 0.05 at (0.30, 0.08, 0); crest sphere r 0.05 at (0, 0.25, 0) | 1.2 kg; box moments m/12·(b² + c²): (0.032, 0.041, 0.041) kg·m²; gravity (0, −9.81, 0) | restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s | box: absorb; eye and crest spheres: transmit every channel |
| 1 rear roller | cylinder r 0.10, half-width 0.19, axis along Z (collider turned Rx 90°, as the Motor rotor's frame, `MotorElementDeclaration.cs:99`) at (−0.18, −0.31, 0) | 0.15 kg; axial ½·m·r² = 0.00075; transverse m·(3r² + w²)/12 with w 0.38 = 0.0022 kg·m² | restitution 0.1, friction 0.8, rolling resistance 0, bounce threshold 0.1 m/s | absorb |
| 2 front roller | the same at (+0.18, −0.31, 0) | the same | the same | absorb |

Geometry check: roller tops are at y −0.21, 1 cm under the chassis underside at −0.20; roller bottoms at −0.41 are the only ground contact; the rollers (0.38 m wide) sit inside the 0.40 m chassis depth. Both spheres stand wholly outside the box (tangent to its front and top faces), so the receiver and emitter at their centres are not inside an absorbing collider. Eye height above the floor 0.49 m; crest height 0.66 m. Total mass 1.5 kg; overall 0.55 × 0.71 × 0.40 m. All starting values.

### Hinges and drive

Two `RevoluteHingeDescriptor` (`A/MechanicalContractValues.cs:149`), both `Support: new(0)`, axis the base frame's Z, `RevoluteTravel.Continuous`, stiffness and damping 0, `BodiesCollide` false, friction torque 0.05 N·m each (the brake that stops an unpowered toy).

Hinge 0 (rear roller, the trailing and loaded one when rolling forward) carries the drive, `RevoluteDriveDescriptor` (`A/MechanicalContractValues.cs:138`): `TargetSpeed` fixed −10 rad/s (rolling toward +X at 10 × 0.10 = 1.0 m/s), `MaxTorque` fixed 0.3 N·m. Hinge 1 is passive.

### Optics, signal and supply

Light follows the family conventions of [Batch K](readiness/batch-k.md) (owner decision 9 and coordinator decision 16, 22 November 2026): three ordinary channels Red, Green, Blue; power in watts per channel; one receiver law for all light.

- **Receiver `eye`** on body 0 at (0.30, 0.08, 0), aperture a disc r 0.05 m (area A = 0.00785 m²) facing +X, one sample point, required channel **Green**, threshold **4.0 × 10⁻⁵ W**, release ratio 0.9. Its reading from a diverging emitter of radiant intensity I at distance d and angle φ off the eye's axis is A·I·cos φ ÷ d² beyond 1 m, and flat A·I ÷ 1 m² inside 1 m for an emitter in front of the eye (none behind it), the one near-field rule of optical convention 1 (coordinator decisions 21 and 26); from a collimated beam it is the beam's whole power. Inside 1 m the fish's light therefore reads 0.00785 × 0.637 = 5.0 × 10⁻³ W from any direction in front of the eye, 125 times the threshold. It is published as a signal node whose committed level is high while the reading is at or above the threshold.
- **Emitter** on body 0 at the crest (0, 0.25, 0): all directions (half-angle 180°, Ω = 4π sr), range 5 m, radiant power 2 W **Blue**, electrical demand 2 W. I = 2 ÷ 4π = 0.159 W/sr. It lets EL-087 see this toy.
- **Supply nodes**, the internal-edge shape proven in `T/ComposableSupplyRoutingTests.cs:499`: `source` (`SupplySourceDescriptor`, `A/SupplyContractValues.cs:39`); `sight` (`SupplyContactDescriptor` controlled by the `eye` node's level through a signal-state contact control, the State latch's form, `elements/StateLatch/StateLatchElementDeclaration.cs:67`, with the level state ENGINE-OPTICAL adds, not `Latched`); consumers `drive` (target `SupplyDriveTarget(Revolute, 0)`) and `glow` (target: the emitter). Edges: `source.Output → sight.PowerIn`, `sight.Output → drive.PowerIn`, `source.Output → glow.PowerIn`.
- Electrical nodes (declared ratings, read by nothing): `drive`, `ElectricalRole.Consumer`, 12 V, 48 Ω, 0 F, `MaxPower` 3 W; `glow`, 12 V, 72 Ω, 0 F, 2 W.

### Ports

None. Stimuli are light, never wires. The toy carries its own source.

### Parameters and store

The source descriptor names four parameters of its own package (`A/ElectricalContractValues.cs:31`), as the Battery does (`elements/Battery/BatteryElementDeclaration.cs:42`).

| Key | Kind, unit | Min | Max | Default | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- |
| `capacity` | Real, Joule | 60 | 960 | 240 | Reject | glow 2 W plus drive at its 3 W peak for a 30 s Run is 150 J |
| `power_limit` | Real, Watt | 10 | 40 | 20 | Reject | above the 5 W peak; 20 W is 0.17 J per tick, under the 1 J per tick supply bound |
| `initial_charge` | Real, none | 0 | 1 | 1 | Reject | 0 gives the "run-down" control |
| `enabled` | Choice `Disabled`, `Enabled` | 0 | 1 | 1 | Reject | Build-only; no run control |

Store: one supply source. No drive speed, force or range parameter (D13).

### Metadata and bindings

Category `Motion`, display name "Curious critter", palette order 175 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour `#fff8e9`. Presentation: body bindings `Visual/RollerRear` → `PackageBodyIndex(1)`, `Visual/RollerFront` → `PackageBodyIndex(2)`; the root follows body 0. `Visual/EyeGlow` emission (three colour bindings) on `PresentationDriver.CommittedActivation` of node `eye` (`A/ElementPresentation.cs:23`); `Visual/ChargeMark` (three colour bindings) on `CommittedCharge` of `source`, the Battery's binding. The gait is the real roller rotation; there is no gait animation. The crest's blue glow is drawn by the optical story's shared renderer.

### Art and solidity

| Node | Mesh | Layer and class |
| --- | --- | --- |
| `Visual/Body` | cream `#fff8e9` chamfered box 0.50 × 0.40 × 0.40 | solid, box collider; follows body 0 |
| `Visual/Eye` | clear navy-tinted `#293954` dome r 0.05 on the front face | solid, sphere collider; body 0 |
| `Visual/EyeGlow` | emissive disc inside the dome | decorative: enclosed, recoloured by a binding |
| `Visual/Crest` | clear blue-tinted ball r 0.05 on the top face | solid, sphere collider; body 0 |
| `Visual/RollerRear`, `Visual/RollerFront` | ochre `#d69c47` cylinders with printed spokes | solid, cylinder colliders; follow bodies 1 and 2 |
| `Visual/ChargeMark` | flush printed bar on the back face | decorative: printed flat |

Icon: side view of the box on two wheels with one eye and one crest ball. It differs from EL-083 (two crest balls) and EL-087 (twice as long, a tail block, two front lenses) by silhouette, not colour.

### Behaviour and numbers

1. **Traction (contact and friction law).** Drive force at the rear contact (0.3 − 0.05)/0.10 = 2.5 N; front roller drag 0.05/0.10 = 0.5 N; net 2.0 N; acceleration 2.0/1.5 = 1.33 m/s², so 1.0 m/s in 0.75 s. Centre of mass height (1.2·0.41 + 0.3·0.10)/1.5 = 0.348 m; wheelbase 0.36 m; load transfer 1.5·1.33·0.348/0.36 = 1.93 N. The rear axle carries 7.36 + 1.93 = 9.29 N. The engine combines friction as a geometric mean (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119`) and the bench is 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs:101`): √(0.8 × 0.3) = 0.49, so the limit is 0.49·9.29 = 4.55 N, above the 2.5 N asked. No wheel slip.
2. **Slopes.** The 2.0 N net drive balances m·g·sin θ = 14.7·sin θ at 7.8°: it stalls on anything steeper.
3. **Stopping.** With the contact open the drive applies no torque (`T/RevoluteDriveTests.cs:259`); the two hinge frictions brake at 2·0.05/0.10 = 1.0 N, 0.67 m/s², so it stops from 1.0 m/s in 1.5 s over 0.75 m.
4. **Pushing and blocking.** Stalled push 2.5 N: it shoves a Tennis ball (0.35 kg) and cannot move a Wall; a stalled drive does no work (`T/RevoluteDriveTests.cs:307`). It presses on a barrier for as long as it is lit.
5. **Tipping.** It tips when pitched past atan(0.18/0.348) = 27°. Nothing holds it upright; knocked over, its rollers leave the ground and it goes nowhere.
6. **What lures it.** Any green light at or above 40 µW on the eye, inside the emitter's range and not blocked by an absorbing solid. For the EL-088 fish (8 W green, I = 0.637 W/sr): at 3 m on axis the reading is 0.00785·0.637/9 = 5.6 × 10⁻⁴ W, fourteen times the threshold; the threshold is reached at cos φ = 4.0 × 10⁻⁵·d²/(0.00785·0.637), which is 86° off axis at 3 m and 59° at 8 m. A lure behind the toy has cos φ ≤ 0 and is also behind the absorbing chassis. How far it is lured is the emitter's declared range (EL-088's `reach`), not a value of this part.
7. **Designed interplay.** The channels are the ordinary ones, so a green or white Flashlight (420 W/sr inside its beam, range 8 m) or a green laser on the eye lures it exactly as the fish does, and red or blue light does nothing. Its own 2 W blue glow reads at most 0.95·0.159 = 0.15 W on a delivered colour receiver's 0.95 m² disc within 1 m, under that receiver's 0.5 W minimum threshold, so it trips none.
8. **Power.** Drive peak 0.3 N·m × 10 rad/s = 3 W, cruise about (0.05 + 0.5·0.10) × 10 = 1 W; glow 2 W while the source lasts. A run-down toy neither moves nor glows.
9. **Tick order.** The optical solve runs after the last substep of a tick; its readings commit together and act from the next tick (coordinator decision 16). In that tick's supply solve the `eye → sight → drive` route closes at once and the drive acts in the same tick's physics. One tick (1/120 s) from light to torque.

## 3. Engine capabilities

| Capability | Status | Evidence |
| --- | --- | --- |
| Supplied revolute velocity drive within a torque bound | present | `A/MechanicalContractValues.cs:138`; `RevoluteDriveTests.ASuppliedDriveSpinsTheRotorUpAtItsTorqueBoundThenHoldsTheTargetSpeed` (`T/RevoluteDriveTests.cs:176`) |
| A drive whose base body is free reacts on it (no gravity, no ground) | present | `RevoluteDriveTests.ADriveOnAFreeBaseTurnsItTheOtherWayAndCountsWorkFromTheRelativeSpeed` (`T/RevoluteDriveTests.cs:503`) |
| Cylinder rolling by friction without slip | present | `WorkshopPhysicsLawCylinderTests.ACylinderRollsDownARampAtTwoThirdsGSinThetaWithoutSlipping` (`T/WorkshopPhysicsLawCylinderTests.cs:180`) |
| Package-internal supply edges from a source through a signal-controlled contact | present | `ComposableSupplyRoutingTests.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`T/ComposableSupplyRoutingTests.cs:530`) |
| Supply contact following a committed signal state | present | `StateLatchTests.Network_ContactClosedFollowsCommittedLatchMemoryAndCounterReach` (`T/StateLatchTests.cs:473`) |
| Disabled or empty source powers nothing | present | `ComposableSupplyRoutingTests.SourceUnavailableLeavesConsumersUnpowered` (`T/ComposableSupplyRoutingTests.cs:191`) |
| Driven hinges carried by a **dynamic base body of a package**, rolling on the ground | present, untested | the validator and compiler take any support index and any base mode (`CuriousContraptions.Bundle/BundleValidator.cs:617`, `engine/gpu/WorkshopPhysicsCompiler.cs:199`, `:245`); no package or test has one. Its qualification is **the package-joints-on-a-dynamic-base story** (coordinator decision 16), an unconditional prerequisite |
| Drive and emitter work debit the source; an exhausted source opens the route | missing | ENGINE-SUPPLY-DEMAND (owner decision 3a) |
| Optical emitter (range, power per channel, paid from a supply consumer), per-collider surface response, receiver with a watt threshold published as a level signal node that a signal-state contact reads | missing | ENGINE-OPTICAL (`engine-optical-propagation`) |
| Occlusion candidates for each emitter-to-receiver segment without a scene scan | missing | ENGINE-SPATIAL-QUERIES (owner decision 3a), from which ENGINE-OPTICAL takes its candidates |

The package-joints-on-a-dynamic-base story (coordinator decision 16; audit E0) is qualification of package joints on a dynamic base, with no agent, behaviour table or perception query. Its current text is [the story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#story-package-joints-on-a-dynamic-base) (key `engine-characters`).

## 4. Sources and legacy

- Requirement: "Original character chooses motion from generic perceptible stimulus and locomotion rules"; outcome "Occluded or unsupported stimulus does not trigger pursuit" ([element-082](../requirements.md#element-082)).
- [S652](../invest/scope-corrections.md#s652): in range it moves toward the lure, out of range or occluded it does not, with a missing lure it idles.
- Legacy search at `a6c914e` for "character", "lure", "mouse", "cat", "pursu" and "walker" found no character source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Visibility is a finite zero-thickness segment that is not a collision body | `engine/physics/SegmentOcclusionSweep.cs@a6c914e:L5-L22` | Carry forward the rule, delivered by ENGINE-OPTICAL. Do not carry forward the f64 CPU sweep. |
| L2 | Opaque pipe collars occlude a light trace; the bore and clear shell do not | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L82-L96` | Carry forward as "transmitting colliders pass, absorbing ones block" |

**Files harvested:** `engine/physics/SegmentOcclusionSweep.cs`, `CuriousContraptions.tests/PipeTests.cs` (occlusion test only).

## 5. Acceptance outline

Requirement row: [element-082](../requirements.md#element-082). Planned class `LuredCharacterTests`. "Lure" is an EL-088 tank at its default 4 m reach, or a declared fixture emitting 8 W green in all directions with range 4 m until EL-088 exists. Displacement is of the chassis origin along X. "Stays" means under 0.05 m over 5 s (a toy that drove for one tick would exceed it within 0.3 s).

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a lure 3 m ahead with a clear line, when run, then the toy rolls to it and rests against it | `AVisibleLureDrawsTheToyUntilItRestsAgainstIt` | no lure: stays | eye within 0.10 m of the tank glass and speed under 0.05 m/s, within a 15 s guard (about 4 s expected) |
| A2 | Given an opaque Wall between them, then it stays | `AnOpaqueWallBetweenThemStopsPursuit` | a transmitting pane in the same place: it moves at least 1 m | Wall 0.2 m thick |
| A3 | Given the lure beyond its range, then it stays | `ALureBeyondItsRangeIsNotFollowed` | 0.5 m inside the range: moves | 0.5 m either side of the 4 m range |
| A4 | Given the lure directly behind the toy, then it stays | `ALureBehindTheEyeIsNotSeen` | lure 60° off the eye's axis at 3 m: moves (threshold angle there is 86°) | — |
| A5 | Given the same fixture emitting red in the lure's place, then it stays | `LightOfAnotherChannelDoesNotTriggerPursuit` | A1 | — |
| A6 | Given `initial_charge` 0, or `enabled` Disabled, then it stays with the lure visible and its crest emits nothing | `ARunDownOrDisabledToyNeitherMovesNorGlows` (theory) | A1 | — |
| A7 | Given the toy resting belly-down on a block 0.25 m high so both rollers hang free, then the rollers spin and it stays | `WithoutGroundContactTheDriveMovesNothing` | A1 | block 0.12 m wide in the 0.16 m gap between the rollers, under a 0.21 m clearance |
| A8 | Given A1, then no committed tick moves the chassis more than 0.02 m | `PursuitIsContinuousMotion` | — | 1.0 m/s is 0.0083 m per tick; 0.02 m excludes a placed pose |
| A9 | Given a Wall 0.45 m high 1 m ahead and the lure visible over it, then it presses on the Wall without passing and the drive's committed work stops growing | `AWallStopsItAndAStalledDriveDoesNoWork` | A1 | drift under 0.02 m and work growth under 0.05 J over 3 s |
| A10 | After A1, Reset restores the authored pose, zero roller travel and the full store; save and load keep the four parameters | `ResetAndSaveLoadRestoreTheToy` | — | generic: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`T/CatalogueIntegrationTests.cs:273`) |
| A11 | A `capacity` of 30 J or 1200 J is refused; a connection naming this portless part is refused | `OutOfRangeStoreAndAnyWireAreRefused` | defaults accepted | typed refusal, nothing changed |
| A12 | Given A1, then the source's debit over the run equals the drive's committed work plus the emitter's paid energy | `TheSourcePaysForTheDriveAndTheGlow` | — | never below that total and above it by no more than the ledger residual |
| A13 | Given `initial_charge` 0.05 (12 J) and a lure 8 m ahead with range 8 m, then it stops short for good and its glow goes out | `AnExhaustedSourceStopsTheDrive` | full charge: it arrives | 12 J at about 3 W lasts 4 s: stopped having travelled between 2 and 6 m, within a 15 s guard; the route reads open |

### Integration

- Collaborators: EL-088 Fish tank lure (stimulus, undelivered), EL-087 (sees this toy's blue glow), CAT-066 Wall (occluder and barrier, delivered), the Flashlight and green laser of the optical catalogue (they lure it too).
- Campaign: chapter 10 "Oddly Satisfying", levels 91–100, "walker/lure/predator/fragile-target roles" (`docs/planning/requirements.md:2295`); coverage ledger row "Original cat/mouse lure/escape roles", reuse 136–150 (`:2411`).
- A level that fixes this toy or its lure in place needs the lesson-fixture story (S8).

### Delivery

- **Shape.** Family slice "Sighted walkers" (EL-082, EL-083, EL-087), EL-082 first in the slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS, ENGINE-ISLANDS-SLEEP, ENGINE-SUPPLY-DEMAND and ENGINE-CAPABILITY-INSTANCES; then the package-joints-on-a-dynamic-base story (coordinator decision 16) and ENGINE-OPTICAL; then S7. EL-088 or the emitter fixture for the lure rows.
- **Names.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/LuredCharacter/` (project, declaration, `lured_character.tscn`, `icon.svg`); `CuriousContraptions.tests/LuredCharacterTests.cs`; one `CuriousContraptions.slnx` line; the coverage binding once S7 admits it.
- **Review tier.** Engine tier (one *present, untested* row).
- **Disqualifiers the probe re-checks.** A receiver level that a supply contact cannot read; an emitter that cannot be a supply consumer's target; a source that cannot feed an internal contact; a rear roller that slips at 2.5 N.

## 6. Open questions

All resolved.

1. **Stimulus.** Green light on the ordinary Green channel (coordinator decision 16): occlusion, transmission and range are the optical law's, with no perception code. Lamps and lasers of that colour lure it too; that is designed interplay, not a defect.
2. **It cannot turn.** "Moves toward the lure" is met as "rolls along its own X while lit": the player aims the part, and a lure off its line is approached only as far as its line passes it. Decided under the standing delegation: steering would need a second drive and a comparison of two receivers, which no acceptance row asks for.
3. **Slope limit 7.8°.** It stalls on steeper ground; a level that needs a climb uses a Conveyor or a gentler ramp.
4. **It presses on barriers** for as long as it is lit; it does not route around them.
5. **No pursuit memory.** The drive is on exactly while the eye is lit (D19).
6. **Finite internal store**, not an external wire: a cable to a moving toy has no delivered form.
7. **No upright constraint.** A constraint that only this toy used would be element logic.
8. **No speed, force or range parameters** (D13).
9. **Name and art.** "Curious critter", an original toy (D9); part-body cream is `#fff8e9` (owner decision 4, 22 November 2026).

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** Bodies, colliders, two hinges, one drive, one receiver, one emitter, a four-node supply graph, four parameters and bindings. No controller, state machine, perception query, callback or engine test on this package.
2. **Generic laws.** Optical emission, transport and reception (ENGINE-OPTICAL); the supply-network rule closing a contact from a signal level; the revolute drive law; contact, friction and hinge friction.
3. **Three layers.** Colliders: one box, two spheres, two cylinders, each with a declared optical response. Visuals: one mesh per collider plus two flat or enclosed marks. Animation: the eye glow and charge mark are recoloured from committed state and push nothing. Body and rollers follow body poses.
4. **Flow and typing.** Light → observation → signal node → signal-state contact → drive → physics, each read from committed state; nothing returns from presentation. Channels, roles and the enable choice are enums.
5. **Flat state.** Bodies 3 (all dynamic); colliders 5 (1 box, 2 spheres, 2 cylinders); joints 2 revolute; drives 1; sensors 1 optical receiver; emitters 1; signal nodes 1; supply nodes 4; stores 1; presentation bindings 8 (2 body, 6 colour).
6. **Performance.** Five primitive colliders; the two spheres are the visible lenses. It is slow (1 m/s), so tunnelling does not arise. The receiver's occlusion candidates come from ENGINE-SPATIAL-QUERIES, not a scan of the scene. No sleeping exists today; an unlit toy at rest sleeps once ENGINE-ISLANDS-SLEEP exists, and nothing here depends on it.
7. **Tests are scenarios.** Every row runs the production simulation with placed parts; none needs a code path of this part.

**Verdict: conforms.** Not deliverable until the engine programme and the two named stories exist; each missing capability has one owning story.

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here because the reader fails the whole coverage run on a spec whose fields do not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `lured_character` |
| Named identity | EL-082, `../invest/named-elements.md#element-082` |
| Acceptance | `todo-200` and `sequence-task-635` (`docs/planning/requirements.md:5053`) |
| Decision | S652, `../invest/scope-corrections.md#s652` (the block exists) |
| Mode records | `Configuration=Fixed` |

What it lacks today: `todo-200` is a scope index shared by EL-082 to EL-085 (`docs/planning/requirements.md:4091`), and its sequence task sits beside the element row, not directly after the todo anchor as the reader requires.
