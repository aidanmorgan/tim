# EL-101 · Thumb tack — named-identity spec

**Current delivery (24 October 2026):** [EL-101 delivery slice](../invest/named-elements.md#element-101). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout, and `path@a6c914e:L…` is the legacy harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Every number is a hand calculation; nothing was simulated. Game values are canonical f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-101 · Thumb tack · Specialist |
| Anchor | [requirements.md#element-101](../requirements.md#element-101); [named entry](../invest/named-elements.md#element-101); scope index [todo-227](../requirements.md#todo-227); [sequence-task-654](../requirements.md#sequence-task-654) |
| Proof owner | S671 |
| Disposition | To deliver as a single element. Not a duplicate: no delivered part is a sharp point. |
| Planned package | id `thumb_tack`, directory `elements/ThumbTack/`, class `ThumbTackElementDeclaration` |
| Refines / extends | No CAT spec. Its first target is the delivered Balloon ([CAT-003](CAT-003-balloon.md), `elements/Balloon/`). Declaration shape of the Wall (`elements/Wall/WallElementDeclaration.cs:8`): a static part that is geometry only |
| Status | not started; blocked on the engine programme (owner decision 3a), within it ENGINE-CONTACT-OBSERVATIONS with the stress-concentration clause (section 3), then S7 |

## 2. Declaration

The package implements `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Static`, `SupportedDomains = Mechanical`, footprint 1 body, 0 moving, no constraints. It has no ports, parameters, stores, sensors or events. The point lies along local +Y; the player turns the part with the rotation gizmo and it stays where it is placed, as a Wall does.

### Colliders (one static body)

| Collider | Shape and size (m) | Centre (m) | Stress concentration |
| --- | --- | --- | --- |
| head | cylinder radius 0.18, half-length 0.02, axis Y | (0, 0.02, 0) | 1 |
| pin | box 0.06 × 0.30 × 0.06 | (0, 0.19, 0) | 1 |
| tip | sphere radius 0.03125 | (0, 0.34, 0) | 16 |

The admitted minimum radii are 1/32 m for a sphere and 1/16 m for a cylinder (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:223`, `:240`), so the tip is the smallest admitted sphere and the pin is a box, not a thin cylinder. The pin spans y 0.04 to 0.34; the tip's top is at 0.34 + 0.03125 = 0.371 m. The tip sphere is 0.0625 m across against the pin's 0.06 m, so it stands 1.25 mm proud of the pin's faces and is the first thing a body meets from above.

Material for all three: restitution 0.3, friction 0.4, rolling resistance 0, bounce threshold 0.1 m/s (**starting values**: metal). "Stress concentration" is the one declared value this part needs that the engine does not yet have (section 3): a dimensionless factor of at least 1 on a collider, default 1. The tack declares 16 on its tip (**starting value**, derived below and proven or changed by the engine slice's two-fixture test). Overall height 0.371 m, head diameter 0.36 m (**starting values**: readable at bench scale, smaller than a Tennis ball).

### Metadata and bindings

Category `Structure`, display name "Thumb tack", palette order 507 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.97, 0.80, 0.32). No presentation bindings: nothing on the tack moves.

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Head` | gold `#f7cb52` disc, the collider's size | solid, cylinder collider | nothing |
| `Visual/Pin` | slate `#556573` square rod, the collider's size | solid, box collider | nothing |
| `Visual/Tip` | slate rounded point, the sphere's size, with a cream `#fff8e9` highlight printed on it | solid, sphere collider; no art is proud of it | nothing |

Icon: a round head seen at three-quarter view with a short pin and a small four-ray "sharp" mark at the point. It differs from every other static part by its shape: a disc on a spike.

The pop puff, the shrinking envelope and the limp balloon are the target's own bindings on its committed gas state (`elements/Balloon/BalloonElementDeclaration.cs:29`–`:34`); the tack draws none of it.

### Behaviour and numbers

**What breaks a Balloon today.** The Balloon declares one contact-integrity observer with threshold 3 N·s and minimum approach speed 0.1 m/s (`BalloonElementDeclaration.cs:68`; `ContactIntegrityDescriptor`, `CuriousContraptions.Abstractions/GasCouplingValues.cs:15`). In each accepted substep the engine sums the normal impulse between the observed collider and each other collider whose approach speed is above the minimum; if the largest pair sum exceeds the threshold the observer breaks, once, and its gas store opens (`CuriousContraptions.Abstractions/ContactIntegrity.cs:16`–`:43`). The measure is impulse only. It cannot tell a point from a wall: a default Balloon (0.5 kg, bounce 0.2) rising into a ceiling at its terminal speed of (11.5 − 9.81)/0.4 = 4.2 m/s delivers at most 0.5 × 1.3 × 4.2 = 2.7 N·s and survives, which a delivered test confirms.

**The general law this part needs.** The observer's pair measure becomes the pair impulse multiplied by the other collider's declared stress concentration. Nothing names the tack: any package may declare the factor on any collider, and any package with an integrity observer responds.

**Numbers.** The impulse of a Balloon of mass m meeting a static collider at speed v is 1.06·m·v (pair restitution 0.2 × 0.3 = 0.06, the product law, `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:117`). With factor 16 the tip breaks the default Balloon when 16 × 1.06 × 0.5 × v > 3, that is at an approach speed above 3/(16 × 0.53) = 0.354 m/s. Approach speeds include the Balloon's linear drag 0.4 1/s (`BalloonElementDeclaration.cs:25`): v·dv/dx = 1.69 − 0.4·v, integrated over the gap.

| Case | Approach speed | Measure (N·s) | Result |
| --- | --- | --- | --- |
| Balloon released 0.5 m below the tip: 1.69 m/s² net lift less drag gives v = 1.17 m/s (1.30 without drag) | 1.17 | 16 × 0.53 × 1.17 = 9.9 | breaks (3.3 times the threshold) |
| Balloon released 0.01 m below the tip | 0.18 | 16 × 0.53 × 0.18 = 1.54 | intact (about half the threshold); it then rests on the tip, and a resting contact never qualifies |
| Balloon at terminal speed (11.5 − 9.81)/0.4 = 4.2 m/s onto the head | 4.2 | 1 × 0.53 × 4.2 = 2.24 | intact |
| Basketball dropped 2 m onto the tip | 6.3 | not measured | intact: a ball declares no integrity observer |

**One-substep assumption.** These figures assume the whole collision impulse arrives in one substep. The measure is per substep, so a soft contact that spreads the impulse over several substeps lowers each sample and the factor must rise. That is why 16 is a starting value and the engine slice's two-fixture test (just below and just above the scaled threshold) is its proof.

**Puncture-resistant material** is declaration data on the other body: no integrity observer (every delivered ball), or a threshold above 16 times the impulses it will meet (row A5b: the same rising Balloon's 9.9 N·s holds against a 20 N·s threshold and breaks a 6 N·s one). **Missed contact** is no contact with the tip collider: the head and pin carry factor 1.

**Tick order.** The observer reads the final contact impulses of the substep in which they occur and the store opens at the next step boundary, as today; the tack adds no ordering.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, StructuralFracture, TopologyTransaction (navigation only, D14).

| Capability | Status | Evidence |
| --- | --- | --- |
| Contact-integrity observer: per-pair impulse in one substep against a strict threshold, approach speed above a minimum | present | `ContactIntegrity.cs:16`; `ContactIntegrityTests.MultipointImpulseSumsWithinOneUnorderedPairOnly`, `.ThresholdIsStrictWithoutHiddenTolerance`, `.RestingSupportAndApproachAtTheThresholdDoNotQualify` (`CuriousContraptions.tests/ContactIntegrityTests.cs:8`, `:40`, `:44`) |
| A broken observer opens its gas store once; the carrier loses lift | present | `IContactIntegrityElement` (`MechanicalContracts.cs:119`); `BalloonTests.AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie` (`CuriousContraptions.tests/BalloonTests.cs:118`) |
| An ordinary blunt contact does not break the default envelope | present | `BalloonTests.OrdinaryCeilingContactSupportsTheDefaultEnvelopeWithoutRupture` (`BalloonTests.cs:41`) |
| Small admitted features give contact and leave the space beside them empty | present | sphere minimum 1/32 m (`PhysicsDeclarationValues.cs:223`); `CompoundMassAndRollingTests.AdmittedSmallFeaturesGiveContactAndEmptySpaceControls` (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:176`) |
| A declared stress concentration on a collider that scales the integrity measure | **missing** | ENGINE-CONTACT-OBSERVATIONS (owner decision 3a; `docs/planning/elements/readiness/engine-design-gap.md`, section 6 item 3), with the audit's clause E6: the pair measure may be multiplied by the other collider's declared stress concentration (at least 1, default 1). The same story makes the observer work on any pair of bodies and found by index |

The earlier draft's `PunctureClass` material field, per-feature sharp flag and `puncture_force` parameter are not used: the delivered measure is an impulse with an approach-speed gate, and one factor on the pointed collider is enough. The factor is not part of ENGINE-FRACTURE.

## 4. Sources and legacy

- **Requirement row.** [element-101](../requirements.md#element-101): sharp-contact geometry punctures material through a generic failure threshold; missed contact or puncture-resistant material remains intact.
- **Legacy search.** `git grep -i` at a6c914e for "tack", "puncture", "pop", "burst" and "sharp" found no puncture source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Balloon: mass 0.5, bounce 0.2, radius 0.36, buoyancy 11.5, drag 0.4 | `parts/catalog/balloon.tres@a6c914e:L11-L14` | Carry forward; these are the delivered defaults (`BalloonElementDeclaration.cs:21`–`:25`) |
| L2 | An effect cannot push through the contact that triggered it | `engine/physics/PhysicsImpactEffects.cs@a6c914e:L21-L33` | Carry forward as an outcome: a broken balloon never passes through the tip. Do not carry forward the callback class |
| L3 | Clear motion without contact activates nothing | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L259-L264` | Carry forward as the missed-contact control |
| L4 | Persistent contact does not retrigger | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L303-L315` | Carry forward: one break per target per Run. The delivered observer is one-way |

The legacy impact-effect hook (an element-owned callback) is not carried: the current mechanism is the declared observer.

**Files harvested:** `parts/catalog/balloon.tres`, `engine/physics/PhysicsImpactEffects.cs`, `CuriousContraptions.tests/PhysicsImpactEffectTests.cs`.

## 5. Acceptance outline

Requirement row: [element-101](../requirements.md#element-101). Planned test class `ThumbTackTests`. "Breaks" means the Balloon's observer reads `IntegrityPhase.Broken`, exactly one `IntegrityBroken` event is published and its gas store mass is 0; "intact" means phase `Intact` and no event, the assertions of the delivered Balloon tests.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a tack pointing down and a default Balloon released 0.5 m below its tip, when the Balloon rises into the tip, then it breaks, loses its lift and falls | `ARisingBalloonBreaksOnTheTip` | A2, A3 | break within a 3 s guard; afterwards the Balloon's centre is below its start within 10 s |
| A2 | Given the same Balloon rising under the tack's head (tack pointing up, head down), then it stays intact and rests against the head | `TheBluntHeadDoesNotBreakTheBalloon` | A1 | intact for the Run limit |
| A3 | Given the Balloon rising 0.5 m to one side of the tack, then it passes and stays intact | `AMissLeavesTheBalloonIntact` | A1 | no contact with any tack collider |
| A4 | Given the Balloon released 0.01 m below the tip, then it touches, rests on the tip and stays intact | `AGentleTouchOnTheTipDoesNotBreakIt` | A1 | intact for the Run limit; the measure is at most two thirds of the threshold |
| A5 | Given a Basketball dropped 2 m onto an upward tip, then it bounces off unchanged; and given a Tennis ball, the same | `ABallIsNotPunctured` (theory) | A1 | no integrity event; the ball's radius and mass are unchanged |
| A5b | Given a test fixture package that is the Balloon's declaration with its integrity observer's threshold set to 20 N·s, released 0.5 m below the tip as in A1, then it stays intact; with the threshold set to 6 N·s it breaks | `APunctureResistantTargetHolds` (theory) | A1 (the delivered 3 N·s Balloon breaks) | measure 16 × 0.53 × 1.17 = 9.9 N·s: under 20 by a factor of 2 and over 6 by 1.65, margins wide enough for the one-substep assumption of section 2; the row fails if the target's own threshold is not the one compared |
| A6 | Given the broken Balloon of A1, then it never passes through the tack | `ABrokenBalloonDoesNotPassThroughTheTip` | — | overlap of the Balloon's collider and the tip at most 0.02 m |
| A7 | Given two tacks touched by one Balloon, then it breaks once | `ABalloonBreaksOnlyOnce` | — | exactly one event |
| A8 | Reset restores the Balloon's gas, radius and lift and the tack's pose; save and load keep the tack's pose | `ResetRestoresTheBalloonAndSaveLoadKeepsTheTack` | — | generic row: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

The tack has no ports and no parameters, so wrong-kind connection and parameter refusals do not apply. The threshold boundary itself (just below and just above the scaled measure, two differently parameterised fixtures) belongs to the engine story's own tests. Every row fits the default 30 s Run length.

### Integration

- Target: Balloon (delivered). Movers: Fan and Rope anchor (delivered). Later targets declare their own observers: EL-090 Steerable blimp and EL-084 Fragile fish bowl (this batch).
- Campaign: no campaign coverage row names the tack (`docs/planning/requirements.md:2400`–`:2420` were searched); it is in the [todo-227](../requirements.md#todo-227) scope index with the pool and toy parts. Owner decision 24c places it in chapter 2's balloon pop lesson at **13**, a build-up on the pop that `burst_of_motion` teaches: introduction 13, practice 19, reuse 149 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)).
- A level that fixes a tack in place needs the lesson-fixture story S8.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), whose ENGINE-CONTACT-OBSERVATIONS must carry the stress-concentration clause; then S7.
- **Files.** `elements/ThumbTack/` (project, declaration, `thumb_tack.tscn`, `icon.svg`); `CuriousContraptions.tests/ThumbTackTests.cs`; one `CuriousContraptions.slnx` line; the binding section once S7 admits it; until S3, the remaining per-package lists.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's. This part has none; the factor is declared wherever that story and ENGINE-CONTACT-OBSERVATIONS put per-collider values.
- **Review tier.** Data-only if the engine slice publishes the factor with tests; otherwise engine.
- **Disqualifiers the probe re-checks.** Whether a collision's impulse arrives in one substep (the factor follows); the pair restitution rule; whether a 0.03125 m static sphere beside a 0.06 m box gives the first contact from above.

## 6. Open questions

All resolved.

1. **Measure.** Impulse scaled by a declared factor, not a force threshold or a pressure: it extends the one delivered measure instead of adding a second rule for the same behaviour. Decided under the standing delegation; its owner is ENGINE-CONTACT-OBSERVATIONS (coordinator decision 16 on the audit's E6).
2. **Static only.** The tack is pinned where it is placed. A loose tack that sticks into things is not asked for (D19).
3. **Which materials puncture.** Those whose package declares an integrity observer; today the Balloon. Each later target decides its own threshold (D16).
4. **No `puncture_force` parameter** (D13): the factor is fixed per kind.
5. **Sizes, material and the factor 16** are starting values (D1). The pin and tip are the audit's replacements for two colliders below the admitted minimum.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One static body with three colliders and their materials. No solver branch, callback, sensor or engine test on this package; the target is never named.
2. **Generic laws.** Rigid contact; the contact-integrity measure with a declared per-collider factor. The missing factor is stated element-free and has one owner, ENGINE-CONTACT-OBSERVATIONS.
3. **Three layers.** Colliders: a cylinder, a box and a sphere, all admitted sizes. Visuals: one mesh per collider, none proud. Animation: none.
4. **Flow and typing.** No ports or parameters. The factor is an f32 collider value.
5. **Flat state.** Bodies 1 (static); colliders 3 (cylinder 1, box 1, sphere 1); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 0.
6. **Performance.** Three static primitives. The tip is small, but it is static and its targets are large slow envelopes, so tunnelling does not arise; a fast small body meeting it relies on the engine's general continuous-collision means, not on this part. Today contact events are found by scanning rows per contact and only for a static body against a dynamic one; ENGINE-CONTACT-OBSERVATIONS replaces that with lookup by handle on any pair, ahead of this part (owner decision 3a). The spec adds no per-element work to it.
7. **Tests are scenarios.** Every row runs the production simulation with the delivered Balloon and balls.

**Verdict: conforms** (one general capability missing, with one owner).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `thumb_tack` |
| Named identity | EL-101, `../invest/named-elements.md#element-101` |
| Acceptance | `todo-227`, `sequence-task-654` (`docs/planning/requirements.md:5205`) |
| Decision | S671, `../invest/scope-corrections.md#s671` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s671` block; `todo-227` is a scope index shared by EL-097 to EL-106, and `sequence-task-654` is anchored beside the element row, not beside that todo, which is the pairing the reader expects.
