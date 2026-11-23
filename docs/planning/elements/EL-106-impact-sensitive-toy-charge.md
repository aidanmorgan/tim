# EL-106 · Impact-sensitive toy charge — named-identity spec

**Current delivery (24 October 2026):** [EL-106 delivery slice](../invest/named-elements.md#element-106). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`).** Every section is written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lx-Ly` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). All game values are canonical f32. No number here was simulated; each is a hand calculation shown beside it.

**Safety boundary.** An abstract toy store of energy in joules with a declared shock threshold. Historical nitroglycerine is a reference for the puzzle role only; no substance, formulation, quantity or handling is modelled or described (requirement row).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-106 · Impact-sensitive toy charge · Specialist |
| Anchor | [requirements.md#element-106](../requirements.md#element-106); [named entry](../invest/named-elements.md#element-106); scope index [todo-227](../requirements.md#todo-227) |
| Proof owner | S379 |
| Disposition | To deliver as a single element after [EL-093 Toy dynamite charge](EL-093-toy-dynamite-charge.md). Not a Family member with it and not a duplicate: EL-093 releases on a signal and has a port; this part releases on a contact shock and has none. |
| Planned package | id `shock_charge`, directory `elements/ShockCharge/`, class `ShockChargeElementDeclaration` |
| Refines / extends | No CAT spec. Blast source of EL-093; shock law in the form of the Balloon's contact-integrity observer (`elements/Balloon/BalloonElementDeclaration.cs:68`) |
| Status | not started; blocked on the engine programme (owner decision 3a, which includes ENGINE-CONTACT-OBSERVATIONS), then ENGINE-IGNITION-BLAST, then S7 |

## 2. Declaration

The package implements `IEnergyStoreElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:35`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and the blast-source interface that ENGINE-IGNITION-BLAST adds. `KinematicMode.Dynamic`, `SupportedDomains = Mechanical`. Footprint: 1 body, no constraints. No ports, no signal nodes.

### Body and colliders

One dynamic body. Mass 0.4 kg (**starting value**); mass frame at the origin with the solid-sphere moment 0.4 × 0.4 × 0.15² = 0.0036 kg·m² on every axis; gravity (0, −9.81, 0); mass-frame `linearDrag` 0.04 /s (`CuriousContraptions.Abstractions/MassFrameValues.cs:39`). Material: restitution 0.3, friction 0.4, rolling resistance 0.03, bounce threshold 0.1 m/s (**starting values**: a glass-like toy flask).

| Collider | Shape | Dimensions (m) | Offset |
| --- | --- | --- | --- |
| Flask | Sphere | radius 0.15 | origin |
| Stopper | Cylinder about +Y | radius 0.05, half-length 0.04 | (0, 0.17, 0) |

The stopper stands 0.06 m proud, so the flask rolls with a wobble and comes to rest stopper-up or on its side.

### Store and blast source

- **Store** `1` (`EnergyStoreDescriptor`, `CuriousContraptions.Abstractions/EnergyStoreValues.cs:73`): capacity and initial energy both the parameter `energy`; charge power 0.
- **Blast source** `1` (missing capability): frame body 0, origin at the centre of mass; reference impulse 2.5 N·s at distance 0 (fixed, **starting value**); linear falloff to zero at the parameter `blast_radius`; paid from store 1; the reaction applied to this body; released by the one-way occurrence of an **integrity observer** on body 0 that names both colliders (flask and stopper): it fires for any contact pair of those colliders whose approach speed is strictly more than the parameter `shock_speed` and whose impulse within one substep is strictly more than 0.2 N·s (fixed), the delivered strict law (`CuriousContraptions.Abstractions/ContactIntegrity.cs:25`). These are the two fields of the delivered `ContactIntegrityDescriptor` (`Threshold`, `MinimumApproachSpeed`, `CuriousContraptions.Abstractions/GasCouplingValues.cs:15`), which today watches one collider, takes constants and can only open a gas outlet. ENGINE-CONTACT-OBSERVATIONS owns the general observer; the blast source consumes its occurrence.

### Ports

None. It cannot be commanded; a wire cannot be offered to it.

### Parameters

| Key | Kind | Unit | Default | Min | Max | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `shock_speed` | Real | MetrePerSecond | 2.5 | 0.5 | 8 | Reject | above the Impact switch's 0.8 m/s default, so careful rolling is safe; a 0.32 m drop reaches it |
| `energy` | Real | Joule | 25 | 5 | 60 | Reject | lighter than EL-093's 40 J |
| `blast_radius` | Real | Metre | 1.2 | 0.5 | 3 | Reject | a neighbour, not the bench |

Build-only (D11).

### Metadata and bindings

Category `Motion`, display name "Jumpy flask", palette order 171 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.40, 0.72, 0.79). Events: the blast story's release event. Presentation: `Visual/Liquid` `UniformScale` on `PresentationDriver.CommittedStoredFraction`, `ClampToEndpoints` (full to nothing); `Visual/Puff` `UniformScale` on `PresentationDriver.Event` for the release event, `SineSquaredPulse`, 0.4 s.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Flask` | translucent cyan `#66b8c9` sphere with a coral `#f06e54` band printed flat | solid, Flask collider | body pose |
| `Visual/Stopper` | cream `#fff8e9` cylinder | solid, Stopper collider | body pose |
| `Visual/Liquid` | gold `#f7cb52` sphere inside the flask | decorative: enclosed, scaled by a binding | binding |
| `Visual/Puff` | cream ring, hidden at scale 0 | decorative: moved by a binding | binding |

Icon: a navy round flask with a stopper and two short shake marks. It differs from the Toy charge (EL-093) by being round with a stopper and no socket.

### Behaviour and numbers

Laws: rigid-body dynamics and contact; a shock threshold on contact (approach speed and pair impulse); the one-shot blast of EL-093; the finite-store ledger.

- **Shock.** The approach speed is the relative normal speed of the two bodies at the contact, so striking and being struck count alike. Resting support has no approach and never qualifies. The release commits at the end of the substep in which the threshold is met, after that substep's contact solve, so the blast cannot push a body through the surface that set it off.
- **Drop heights.** v = √(2 × 9.81 × h): 0.20 m gives 1.98 m/s (safe), 0.32 m gives 2.5 m/s (the threshold), 0.50 m gives 3.13 m/s (releases). The pair impulse at 0.50 m is about 0.4 × 3.13 × 1.3 = 1.6 N·s, well over 0.2.
- **Rolling off a step.** A 0.10 m step gives 1.4 m/s: safe.
- **Struck by a Bowling ball (4 kg).** At 1 m/s: safe. At 4 m/s: approach 4 ≥ 2.5, and against a free flask the pair impulse is the reduced mass (4 × 0.4/4.4 = 0.364 kg) × 1.3 × 4 = 1.9 N·s: releases.
- **A very light striker.** A Tennis ball with `mass` 0.01 kg at 4 m/s delivers about 0.01 × 4 × 1.85 = 0.07 N·s, under 0.2: unspent. Speed alone is not enough.
- **Blast, `blast_radius` 1.2 m.** J(d) = 2.5 × (1 − d ÷ 1.2) N·s on each unshielded dynamic body, the negative sum on the flask, all scaled by one factor if the kinetic energy added would exceed `energy`. A Basketball resting 0.6 m away, centre to centre (the two radii sum to 0.49 m): 2.5 × (1 − 0.6/1.2) = 1.25 N·s, 1.25 m/s. A Tennis ball at 0.6 m: 3.6 m/s, under the Bumper's 8 m/s.
- **Chain.** A blast is not a contact, so one flask's blast does not release another directly; a flask thrown by a blast releases if it then lands or strikes faster than `shock_speed`. Two flasks meeting at 3 m/s both release.
- **Spent.** The store reads zero and the release cannot recur until Reset; the same body remains.
- **Fit and tunnelling.** 0.30 m across: it passes the pipe bore (1.3 m across) and is under the Cannon's 0.6 m minimum width, so it does not seat. Thrown at 6 m/s it moves 0.0125 m per substep against a 0.15 m radius; the engine's fattened bounds and speculative contacts cover it (`docs/gpu-f32-physics.md:195`, `:258`).

## 3. Engine capabilities

Map row families (navigation only, D14): ChemicalReaction, ContactImpulse, FiniteLedger, FiniteWorkActuation, GeometryQuery, RigidBodyDynamics, StateTransaction.

| Capability | Status | Evidence |
| --- | --- | --- |
| Free dynamic body with a declared mass frame and two colliders | present | `CompoundMassAndRollingTests.CollisionChildrenDoNotChangeDeclaredFreeFlight` (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:283`) |
| Finite store full at Run, never refilled | present | `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` (`CuriousContraptions.tests/FanIntegrationTests.cs:481`) |
| Store-fraction and event-cue presentation drivers | present | `CannonIntegrationTests.PhysicalLoadChargesAndFiresOnlyWithItsSeparateSupply` (`CuriousContraptions.tests/CannonIntegrationTests.cs:550`) asserts the store-fraction mapping |
| A contact-shock threshold on a dynamic body: strict pair impulse within one substep, with a minimum approach speed, never from resting support | narrower | the law exists (`ContactIntegrityTests.ThresholdIsStrictWithoutHiddenTolerance` and `.RestingSupportAndApproachAtTheThresholdDoNotQualify`, `CuriousContraptions.tests/ContactIntegrityTests.cs:40`, `:44`), but its only declared effect is opening a gas store's outlet (`GasCouplingValues.cs:15` requires a `LocalGasStoreId`) and its values are constants. A contact trigger cannot serve: its owner must be static (`ContactActivationTests.TriggerRequiresActualStaticOwnerDynamicTargetAndUniqueOwnership`, `CuriousContraptions.tests/ContactActivationTests.cs:77`, name only). Owner: **ENGINE-CONTACT-OBSERVATIONS** (owner decision 3a; audit E6 gives the integrity observer this one owner: it names a body or a set of colliders, takes constant or parameter-bound values, needs no gas store and publishes a one-way occurrence that a declared effect consumes) |
| **One-shot radial impulse with solid occlusion, paid from a finite store, spent until Reset** | missing | Owner: **ENGINE-IGNITION-BLAST** (`engine-ignition-blast`), as [EL-093 section 3](EL-093-toy-dynamite-charge.md#3-engine-capabilities); it consumes the observer's occurrence and has no shock clause of its own |

Counts: 3 present, 0 present-untested, 1 narrower, 1 missing; each gap has exactly one owner. Nothing needs element-specific engine code.

## 4. Sources and legacy

- Requirement: "Finite abstract stored energy reacts to a generic shock threshold"; outcome "Subthreshold contact leaves it unspent; historical nitroglycerine is a reference only" ([element-106](../requirements.md#element-106)).
- No legacy source for the part. The legacy impact-effect hook is the nearest record of the timing rules; its callback class is not carried forward, because an element-owned executable hook is not declaration data.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | A store debit never exceeds stored energy; release and debit commit together | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L47-L52`; `engine/physics/PhysicsWorld.cs@a6c914e:L1250-L1267` | carry forward the rule |
| L2 | The shock input is the contact approach speed | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L9-L19` | carry forward |
| L3 | Effect impulses apply after the ordinary contact response and cannot push through the contact that triggered them | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L21-L33`; `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L208-L216` | carry forward as a rule of the blast story |
| L4 | Clear motion without contact activates nothing | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L259-L264` | carry forward (row A3) |
| L5 | Simultaneous contacts notify every subscriber with the same approach speed | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L267-L280` | carry forward (row A8) |
| L6 | Persistent contact does not retrigger; release and recontact does | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L303-L315` | carry forward: a resting flask is never re-evaluated as a shock |

**Files harvested:** `engine/physics/PhysicsEnergyStore.cs`, `engine/physics/PhysicsWorld.cs` (energy-store release only), `engine/physics/PhysicsImpactEffects.cs`, `CuriousContraptions.tests/PhysicsImpactEffectTests.cs`.

## 5. Acceptance outline

Requirement row: [element-106](../requirements.md#element-106). Planned test class `ShockChargeTests`. Drop heights are from the flask's lowest point to the bench.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a flask dropped 0.50 m with a Basketball resting 0.6 m from where it lands, then the flask releases and the ball is thrown | `AHardLandingReleasesTheCharge` | A2 | ball speed 0.9–1.6 m/s on the tick after release (hand 1.25); store zero |
| A2 | Given a flask dropped 0.20 m, then it stays unspent and the ball does not move | `ASubthresholdLandingLeavesItUnspent` | A1 | store full; ball under 0.01 m/s. The 0.20 m and 0.50 m heights sit 20 % either side of the 2.5 m/s threshold in speed: closer rows would test the contact solver, not the lesson |
| A3 | Given a flask at rest, a ball passing 0.05 m clear, and a Bowling ball rolling into it at 1 m/s, then it stays unspent | `RestAMissAndAGentleBumpDoNotRelease` | A4 | store full after 3 s |
| A4 | Given a Bowling ball striking it at 4 m/s, then it releases | `AFastHeavyStrikeReleasesIt` | A3 | store zero within 0.1 s of first contact |
| A5 | Given a Tennis ball with `mass` 0.01 kg striking it at 4 m/s, then it stays unspent | `ALightStrikerCannotReleaseIt` | A4 | store full |
| A6 | Given a spent flask struck again at 4 m/s, then nothing is thrown | `ASpentFlaskCannotRepeat` | A4 | no neighbour gains more than the contact itself gives; store stays zero |
| A7 | Given a Wall between the landing point and a second ball, then that ball does not move | `ASolidWallShieldsTheBodyBehindIt` | A1's ball | under 0.01 m/s |
| A8 | Given two flasks meeting at a closing speed of 3 m/s, then both release | `TwoFlasksMeetingHardBothRelease` | closing at 1 m/s: neither | both stores zero |
| A9 | Given `shock_speed` 4 m/s and the 0.50 m drop, then it stays unspent | `TheThresholdIsTheDeclaredSpeed` | A1 at the default | store full |
| A10 | Given `shock_speed` 0.4 m/s or `energy` 70 J, then the edit is refused | `OutOfRangeValuesAreRefused` | defaults accepted | typed refusal |
| A11 | Reset restores pose, the full store and every thrown body; save and load keep the parameters | `ResetAndSaveLoadRestoreTheFlask` | — | also `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

No ports, so no connection refusal applies. The energy cap and the reaction are proven once, in EL-093 rows A6 and A7, on the same blast law.

### Integration

- Set off by any delivered ball, the Weight, a Wall or the bench; thrown by the Cannon, Bumper, Springboard or an EL-093 blast. Targets and shields as EL-093.
- Campaign: no ledger row names it. It falls under chapter 10's "toy rocket/ignition/destruction variants" (`docs/planning/requirements.md:2295`); owner decision 24c gives it new level **95a** *Gently Does It* (chapter 10): introduction 95a, practice 99, reuse 149 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)).
- A level that fixes a flask on a shelf as authored scenery needs S8.

### Delivery

- **Shape.** Single element slice after EL-093.
- **Prerequisites, in order.** The engine programme (owner decision 3a), of which this part leans on ENGINE-CONTACT-OBSERVATIONS (the integrity observer), ENGINE-SPATIAL-QUERIES (blast candidates and occlusion) and ENGINE-ISLANDS-SLEEP; then ENGINE-IGNITION-BLAST; then EL-093; then S7 (section 7). S3 optional. Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/ShockCharge/` (project, declaration, `shock_charge.tscn`, `icon.svg`); `CuriousContraptions.tests/ShockChargeTests.cs`; one `CuriousContraptions.slnx` line; until S3, the trim list and placement switch.
- **Review tier.** Data-only if the engine story publishes the shock release with tests; otherwise engine tier.
- **Disqualifiers the probe re-checks.** The integrity observer published without a parameter-bound approach speed or without a collider set; a soft contact that spreads a landing over so many substeps that no single substep exceeds 0.2 N·s (then the fixed impulse floor is re-chosen under D1 before the slice plans).

## 6. Open questions

All resolved.

1. **Shock measure: approach speed with a small impulse floor** (decided now): the speed is what a player can see and set; the floor stops a feather-light touch counting; both already exist as the two fields of the delivered contact-integrity law.
2. **Chain reaction only through real impacts** (decided now): a blast is not a contact, so there is one release rule.
3. **No command input** (D19): the commanded charge is EL-093.
4. **No liquid wobble binding** (D19): no committed driver exposes spin, and the cue teaches nothing.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One body, one store, one blast source with a declared contact-shock release, three parameters, two bindings. No impact callback.
2. **Generic laws.** "When hit hard it goes off" is the contact law reporting a pair's approach speed and impulse to a declared threshold, and the general blast law acting from a declared source; each is owned by one element-free story (ENGINE-CONTACT-OBSERVATIONS for the observer, ENGINE-IGNITION-BLAST for the blast) and usable by any package.
3. **Three layers.** Colliders: one sphere and one cylinder. Visuals: flask and stopper follow the body pose; the liquid is enclosed. Animation: liquid and puff read the committed store fraction and the release event and write nothing back.
4. **Flow and typing.** No ports; typed parameters with units.
5. **Flat state.** Bodies 1 (dynamic); colliders 2 (sphere 1, cylinder 1); joints 0; drives 0; blast sources 1; integrity observers 1 (over two colliders); signal nodes 0; stores 1; presentation bindings 2.
6. **Performance.** Two primitives. The observer reads only this body's own contact pairs. No sleeping exists today: resting cost is owned by ENGINE-ISLANDS-SLEEP, the contact pass that scales with the scene by ENGINE-CONTACT-OBSERVATIONS and blast candidates by ENGINE-SPATIAL-QUERIES (owner decision 3a). This part does not design around any of them.
7. **Tests are scenarios.** Every row runs the production simulation with delivered strikers.

**Verdict: conforms** (two general capabilities missing or narrower, one owner each; the audit fixes of 22 November 2026 are applied).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec carrying that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `shock_charge` |
| Named identity | EL-106, `../invest/named-elements.md#element-106` |
| Acceptance | `todo-227`, `sequence-task-659` |
| Decision | S379 |
| Mode records | `Configuration=Fixed` |

What it lacks today: `docs/planning/invest/scope-corrections.md` has no `s379` block; `todo-227` is a scope index shared by thirteen identities (`docs/planning/requirements.md:6389`–`:6391`) and is not co-anchored with this identity's own task, `sequence-task-659`, which sits beside `element-106` (`docs/planning/requirements.md:5245`).
