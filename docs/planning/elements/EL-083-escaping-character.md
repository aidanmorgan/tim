# EL-083 · Escaping character — named-identity spec

**Current delivery (24 October 2026):** [EL-083 delivery slice](../invest/named-elements.md#element-083). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`), repaired the same day against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-l.md`) and the owner decisions of 22 November 2026.** A `path:line` citation without a commit is in this checkout; `T/` abbreviates `CuriousContraptions.tests/` and `A/` abbreviates `CuriousContraptions.Abstractions/`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32. Nothing here was built or simulated; every number is a hand calculation shown beside it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-083 · Escaping character · Character |
| Anchor | [requirements.md#element-083](../requirements.md#element-083); [named entry](../invest/named-elements.md#element-083); scope index [todo-200](../requirements.md#todo-200) |
| Proof owner | S653 ([decision](../invest/scope-corrections.md#s653)) |
| Disposition | Member of the Family slice **"Sighted walkers"** with [EL-082](EL-082-lured-character.md) (the family's reference spec) and [EL-087](EL-087-predator-character.md). Same declaration shape as EL-082; the differences are declared values only: chassis mass, which roller carries the drive and its speed, the eye's channel and threshold, two crest balls instead of one, colours. Not a duplicate: the outcome (moving away) differs. |
| Planned package | id `escaping_character`, directory `elements/EscapingCharacter/`, class `EscapingCharacterElementDeclaration` |
| Refines / extends | No CAT spec |
| Status | not started; blocked on the engine programme (owner decision 3a), then the package-joints-on-a-dynamic-base story (coordinator decision 16) and ENGINE-OPTICAL, then S7 |

## 2. Declaration

**What the part is.** The EL-082 toy driven the other way: while its eye receives red light above its threshold, a declared supply contact closes and a roller turns so that the toy backs away from what it sees, still facing it. No controller, state machine, perception query, timer or update loop. When the light no longer reaches the eye (beyond the emitter's range, or hidden), the contact opens and hinge friction stops the toy. That is the whole "bounded avoidance route".

Interfaces, kinematic mode, domains and footprint: exactly as [EL-082 section 2](EL-082-lured-character.md#2-declaration) (`IHingeElement`, `ISupplyElement`, `ISignalElement`, `ISceneGeometryElement`, the ENGINE-OPTICAL emitter and receiver contracts; `KinematicMode.Dynamic`; 3 bodies, 3 moving, 2 revolute constraints).

### Bodies and colliders

Local frame as EL-082: origin at the chassis box centre, 0.41 m above the support at rest, +X the way the eye faces.

| Body | Colliders (m) | Mass and inertia | Contact material | Optical response |
| --- | --- | --- | --- | --- |
| 0 chassis (dynamic base) | box half extents (0.25, 0.20, 0.20); eye sphere r 0.05 at (0.30, 0.08, 0); two crest spheres r 0.05 at (0, 0.25, ±0.10) | 0.9 kg; box moments (0.024, 0.031, 0.031) kg·m² from m/12·(b² + c²); gravity (0, −9.81, 0) | restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s | box: absorb; eye and crest spheres: transmit every channel |
| 1 rear roller at (−0.18, −0.31, 0); 2 front roller at (+0.18, −0.31, 0) | cylinders r 0.10, half-width 0.19, axis Z | 0.15 kg each; axial 0.00075, transverse 0.0022 kg·m² | restitution 0.1, friction 0.8, rolling resistance 0, bounce threshold 0.1 m/s | absorb |

Total mass 1.2 kg (**starting value**: lighter than EL-082, so it is quicker). Geometry checks are EL-082's: rollers clear the chassis by 1 cm and are the only ground contact; the spheres stand wholly outside the box. Eye height 0.49 m, crest height 0.66 m.

### Hinges and drive

Two continuous `RevoluteHingeDescriptor` on `Support: new(0)` about Z, friction torque 0.05 N·m each, as EL-082. **Hinge 1** (the roller at x +0.18, which trails and is loaded when the toy backs away) carries the `RevoluteDriveDescriptor` (`A/MechanicalContractValues.cs:138`): `TargetSpeed` fixed **+15 rad/s** (rolling toward −X at 15 × 0.10 = 1.5 m/s), `MaxTorque` fixed 0.3 N·m. Hinge 0 is passive.

### Optics, signal and supply

Conventions as EL-082: ordinary channels, watts, one receiver law (owner decision 9 and coordinator decision 16, 22 November 2026).

- **Receiver `eye`** on body 0 at (0.30, 0.08, 0): disc r 0.05 m (A = 0.00785 m²) facing +X, required channel **Red**, threshold **1.0 × 10⁻⁴ W**, release ratio 0.9; published as a level signal node.
- **Emitter** on body 0 at the crest (0, 0.25, 0.10): all directions, range 5 m, 2 W **Blue** (I = 0.159 W/sr), demand 2 W. EL-087 sees it.
- **Supply nodes** as EL-082: `source`; `sight` (contact on the `eye` level); consumers `drive` (target `SupplyDriveTarget(Revolute, 1)`) and `glow` (the emitter). Edges: `source → sight → drive`, `source → glow`.
- Electrical ratings (read by nothing): `drive` 12 V, 32 Ω, 4.5 W; `glow` 12 V, 72 Ω, 2 W.

### Ports

None.

### Parameters and store

The four source parameters of EL-082, same ranges and defaults: `capacity` 60–960 J, default 240; `power_limit` 10–40 W, default 20; `initial_charge` 0–1, default 1; `enabled` Choice, default `Enabled`. All Build-only, policy Reject. Glow 2 W plus the 4.5 W drive peak for 30 s is 195 J. No distance, range, speed or force parameter (section 6).

### Metadata and bindings

Category `Motion`, display name "Skittish critter", palette order 176 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour `#bff5b0`. Bindings as EL-082: two body bindings for the rollers, the eye glow on `CommittedActivation` of `eye`, the charge mark on `CommittedCharge` of `source`.

### Art and solidity

| Node | Mesh | Layer and class |
| --- | --- | --- |
| `Visual/Body` | pale green `#bff5b0` chamfered box with a cream `#fff8e9` printed belly | solid, box collider; follows body 0 |
| `Visual/Eye` | clear navy-tinted `#293954` dome r 0.05 | solid, sphere collider |
| `Visual/EyeGlow` | emissive disc inside the dome | decorative: enclosed, recoloured by a binding |
| `Visual/CrestLeft`, `Visual/CrestRight` | clear blue-tinted balls r 0.05 | solid, sphere colliders |
| `Visual/RollerRear`, `Visual/RollerFront` | ochre `#d69c47` cylinders with printed spokes | solid, cylinder colliders; follow bodies 1 and 2 |
| `Visual/ChargeMark` | flush printed bar | decorative: printed flat |

Icon: the box on two wheels with two crest balls and a motion arrow pointing away from the eye. It differs from EL-082 by the second crest ball and from EL-087 by length and tail.

### Behaviour and numbers

1. **Traction.** Drive force (0.3 − 0.05)/0.10 = 2.5 N; other roller's drag 0.5 N; net 2.0 N; acceleration 2.0/1.2 = 1.67 m/s²; 1.5 m/s in 0.9 s. Centre of mass height (0.9·0.41 + 0.3·0.10)/1.2 = 0.333 m; load transfer 1.2·1.67·0.333/0.36 = 1.85 N onto the +X axle, which is the driven one: 5.89 + 1.85 = 7.74 N. Friction is the geometric mean with the bench, √(0.8 × 0.3) = 0.49 (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119`; bench 0.3 at `engine/gpu/WorkshopPhysicsCompiler.cs:101`): limit 0.49·7.74 = 3.79 N, above the 2.5 N asked. (Driving hinge 0 instead would leave 4.04 N and a 1.98 N limit: it would slip.)
2. **Slopes.** 2.0 N balances 11.8·sin θ at 9.8°; it cannot back up anything steeper.
3. **What scares it.** Red light at or above 100 µW on the eye. EL-087's lamp (2 W red, I = 0.159 W/sr, range 2.5 m) reads 0.00785·0.159/2.5² = 2.0 × 10⁻⁴ W on axis at 2.5 m, so the lamp's range, not the threshold, ends the fright; the threshold angle is 60° off axis at 2.5 m and 80° at 1.5 m.
4. **The route is bounded by the hazard's range.** A fixed hazard of range R is lost when the eye is R away; the toy then brakes at 2·0.05/0.10 = 1.0 N, 0.83 m/s², stopping from 1.5 m/s in 1.8 s over 1.35 m. It rests between R and R + 1.5 m from the hazard: 2.5 to 4.0 m for EL-087's lamp.
5. **Against a pursuer.** EL-087 rolls at 1.2 m/s and this toy at 1.5 m/s, so on open ground the gap grows until the lamp is out of range; the toy stops, the pursuer closes, and it retreats again until its store or a Wall ends it. Each leg is the same contact opening and closing; there is no recover timer.
6. **A Wall behind it** stops it by contact; the stalled drive does no work (`T/RevoluteDriveTests.cs:307`). It presses there while it is lit and does not route around.
7. **Designed interplay.** A red or white Flashlight or a red laser on its eye frightens it from up to that light's range, just as EL-087 does. Its own 2 W blue glow is too weak to trip a delivered colour receiver (0.15 W against a 0.5 W minimum, as EL-082).
8. **Power.** Drive peak 0.3 × 15 = 4.5 W, cruise about 0.1 N·m × 15 = 1.5 W; glow 2 W.
9. **Tick order.** As EL-082: the optical solve runs after the last substep; readings act from the next tick; the route closes within that tick's supply solve.

## 3. Engine capabilities

| Capability | Status | Evidence |
| --- | --- | --- |
| Supplied revolute velocity drive, either sign, within a torque bound | present | `RevoluteDriveTests.ASuppliedDriveSpinsTheRotorUpAtItsTorqueBoundThenHoldsTheTargetSpeed`, run at +6 and −6 rad/s (`T/RevoluteDriveTests.cs:176`) |
| Unsupplied drive applies no torque | present | `RevoluteDriveTests.AnUnsuppliedDriveAppliesNoImpulseAndDoesNoWork` (`T/RevoluteDriveTests.cs:259`) |
| Hinge friction as the brake | present | `RevoluteHingeFrictionTests.AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque` (`T/RevoluteHingeFrictionTests.cs:59`) |
| Package-internal source, signal-controlled contact and consumer | present | `ComposableSupplyRoutingTests.SimulationAdvancesPowerAcrossPackageInternalEdgesToRechargeStore` (`T/ComposableSupplyRoutingTests.cs:530`); `StateLatchTests.Network_ContactClosedFollowsCommittedLatchMemoryAndCounterReach` (`T/StateLatchTests.cs:473`) |
| Driven hinges carried by a dynamic base body of a package | present, untested | admitted by `CuriousContraptions.Bundle/BundleValidator.cs:617` and `engine/gpu/WorkshopPhysicsCompiler.cs:199`; no package or test uses it. Qualified by **the package-joints-on-a-dynamic-base story** (coordinator decision 16), an unconditional prerequisite |
| Drive and emitter work debit the source; an exhausted source opens the route | missing | ENGINE-SUPPLY-DEMAND (owner decision 3a) |
| Optical emitter, per-collider surface response, watt-threshold receiver published as a level signal node read by a signal-state contact | missing | ENGINE-OPTICAL (`engine-optical-propagation`) |
| Occlusion candidates without a scene scan | missing | ENGINE-SPATIAL-QUERIES (owner decision 3a) |

The package-joints-on-a-dynamic-base story is cited in its corrected form (coordinator decision 16; audit E0); its current text is [the story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#story-package-joints-on-a-dynamic-base) (key `engine-characters`).

## 4. Sources and legacy

- Requirement: "Original character chooses a bounded avoidance route from perceived hazards"; outcome "Absent or blocked perception does not trigger a hidden scripted escape" ([element-083](../requirements.md#element-083)).
- [S653](../invest/scope-corrections.md#s653): a visible hazard produces the declared move-away; an absent hazard or blocked perception produces none; no scripted escape or new safe-arrival goal.
- Legacy search at `a6c914e` for "escape", "flee", "hazard" and "character" found no character source. The `Escaped` machine event of `CuriousContraptions.tests/FloorTests.cs@a6c914e:L48-L48` is a body leaving the world and is unrelated.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Visibility is a finite zero-thickness segment, not a collision body | `engine/physics/SegmentOcclusionSweep.cs@a6c914e:L5-L22` | Carry forward the rule through ENGINE-OPTICAL. Do not carry forward the CPU sweep. |
| L2 | Clear shells pass light while opaque collars block it | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L82-L96` | Carry forward as the occlusion rule |

**Files harvested:** `engine/physics/SegmentOcclusionSweep.cs`, `CuriousContraptions.tests/PipeTests.cs` (occlusion test only), `CuriousContraptions.tests/FloorTests.cs` (checked, unrelated).

## 5. Acceptance outline

Requirement row: [element-083](../requirements.md#element-083). Planned class `EscapingCharacterTests`. "Hazard" is a declared fixture emitting 2 W red in all directions with range 2.5 m, or an EL-087. Distances are from the eye to the emitter. "Stays" means under 0.05 m over 5 s.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| B1 | Given a hazard 1.5 m in front of the eye, when run, then the toy backs away and comes to rest | `AVisibleHazardDrivesItAwayUntilTheHazardIsOutOfRange` | no hazard: stays | rest distance between 2.4 and 4.1 m (R to R + 1.5 m, 0.1 m allowance), within a 15 s guard |
| B2 | Given an opaque Wall between them, then it stays | `ABlockedViewCausesNoEscape` | a transmitting pane in the same place: it backs away at least 0.5 m | — |
| B3 | Given the hazard 3.0 m away, beyond its range, then it stays | `AHazardBeyondItsRangeCausesNoEscape` | 2.0 m: backs away | 0.5 m either side of the 2.5 m range |
| B4 | Given the hazard directly behind the toy, then it stays | `AHazardItCannotSeeCausesNoEscape` | B1 | hazard on the −X side at 1.5 m |
| B5 | Given the same fixture emitting green in the hazard's place, then it stays | `LightOfAnotherChannelCausesNoEscape` | B1 | — |
| B6 | Given B1, then the chassis never moves toward the hazard | `ItOnlyEverMovesAway` | — | X never rises more than 0.02 m above its start |
| B7 | Given a Wall 0.5 m behind the toy and a hazard in view, then it presses on the Wall and does not pass | `AWallBehindItEndsTheRoute` | B1 | drift under 0.02 m and drive work growth under 0.05 J over 3 s once stopped |
| B8 | Given `initial_charge` 0 or `enabled` Disabled, then it stays with the hazard in view and its crest emits nothing | `ARunDownOrDisabledToyNeitherMovesNorGlows` (theory) | B1 | — |
| B9 | Given an EL-087 starting 2 m in front of it on open ground, then the gap never closes | `ItOutrunsThePursuer` | with this toy disabled the pursuer does not see it and stays | gap never under 1.5 m over 8 s (1.5 against 1.2 m/s) |
| B10 | Reset restores the authored pose, roller travel and store; save and load keep the four parameters | `ResetAndSaveLoadRestoreTheToy` | — | generic: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`T/CatalogueIntegrationTests.cs:273`) |
| B11 | A `capacity` of 30 J or 1200 J is refused; a connection naming this portless part is refused | `OutOfRangeStoreAndAnyWireAreRefused` | defaults accepted | typed refusal, nothing changed |
| B12 | Given B1, then the source's debit over the run equals the drive's committed work plus the emitter's paid energy | `TheSourcePaysForTheDriveAndTheGlow` | — | never below that total and above it by no more than the ledger residual |
| B13 | Given `initial_charge` 0.05 (12 J) and a fixture emitting 30 W red with range 8 m placed 1.5 m in front, then it stops for good while the hazard is still in range | `AnExhaustedSourceStopsTheDrive` | full charge: it backs away beyond 8 m | 12 J at about 3.5 W lasts 3.4 s: stopped within 7 m of the hazard, within a 15 s guard; the route reads open |

### Integration

- Collaborators: EL-087 Predator (hazard lamp; sees this toy's blue glow), CAT-066 Wall (occluder and barrier, delivered), the Flashlight and red laser of the optical catalogue (they frighten it too).
- Campaign: chapter 10, levels 91–100 (`docs/planning/requirements.md:2295`); coverage ledger row "Original cat/mouse lure/escape roles", reuse 136–150 (`:2411`).
- A level that fixes it in place needs the lesson-fixture story (S8).

### Delivery

- **Shape.** Family slice "Sighted walkers", after EL-082 inside the slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-CONTACT-OBSERVATIONS, ENGINE-ISLANDS-SLEEP, ENGINE-SUPPLY-DEMAND and ENGINE-CAPABILITY-INSTANCES; then the package-joints-on-a-dynamic-base story (coordinator decision 16) and ENGINE-OPTICAL; then S7. Row B9 needs EL-087 in the same slice.
- **Names.** Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/EscapingCharacter/` (project, declaration, `escaping_character.tscn`, `icon.svg`); `CuriousContraptions.tests/EscapingCharacterTests.cs`; one `CuriousContraptions.slnx` line; the coverage binding once S7 admits it.
- **Review tier.** Engine tier (one *present, untested* row).
- **Disqualifiers the probe re-checks.** Those of EL-082, and a driven trailing roller that slips at 2.5 N.

## 6. Open questions

All resolved.

1. **Hazard definition.** A hazard is red light on the ordinary Red channel and nothing else (coordinator decision 16). The earlier idea that any body closing faster than 1.5 m/s counts is dropped: it would be a scene-wide query written for this part. Red lamps and lasers frighten it too, by design.
2. **No `escape_distance`, no recover timer** (the S653 correction allows it). The route is bounded by the hazard's range plus the 1.35 m stopping distance, and by the store.
3. **It cannot turn**, and it only backs away from what it faces; a hazard behind it is unseen, which is the requirement's "absent perception" control.
4. **Slope limit 9.8°**; **it presses on a barrier behind it** while lit.
5. **Speed 1.5 m/s** against the pursuer's 1.2 m/s so escape is possible (D1).
6. **Name and art** "Skittish critter" (D9); part-body cream is `#fff8e9` (owner decision 4, 22 November 2026).

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** The EL-082 declaration with other values. No controller, state machine, timer, perception query or engine test on this package.
2. **Generic laws.** Optical reception (ENGINE-OPTICAL), the supply-network rule, the revolute drive law, hinge friction, contact and friction.
3. **Three layers.** Colliders: one box, three spheres, two cylinders, each with a declared optical response. Visuals: one mesh per collider and two flat or enclosed marks. Animation: eye glow and charge mark recoloured from committed state. Everything else follows a body pose.
4. **Flow and typing.** Light → observation → signal node → signal-state contact → drive → physics; nothing returns. Channels and the enable choice are enums.
5. **Flat state.** Bodies 3 (all dynamic); colliders 6 (1 box, 3 spheres, 2 cylinders); joints 2 revolute; drives 1; sensors 1 optical receiver; emitters 1; signal nodes 1; supply nodes 4; stores 1; presentation bindings 8.
6. **Performance.** One sphere more than EL-082, for the visible second crest ball that tells the two apart. Slow body; occlusion candidates from ENGINE-SPATIAL-QUERIES. No sleeping exists today; ENGINE-ISLANDS-SLEEP will let an unlit toy at rest sleep, and nothing here depends on it.
7. **Tests are scenarios.** Every row is a placed scene in the production simulation.

**Verdict: conforms.** Not deliverable until the engine programme and the two named stories exist; each missing capability has one owning story.

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here because the reader fails the whole coverage run on a spec whose fields do not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `escaping_character` |
| Named identity | EL-083, `../invest/named-elements.md#element-083` |
| Acceptance | `todo-200` and `sequence-task-636` (`docs/planning/requirements.md:5061`) |
| Decision | S653, `../invest/scope-corrections.md#s653` (the block exists) |
| Mode records | `Configuration=Fixed` |

What it lacks today: `todo-200` is a scope index shared by EL-082 to EL-085 (`docs/planning/requirements.md:4091`), and its sequence task sits beside the element row, not directly after the todo anchor as the reader requires.
