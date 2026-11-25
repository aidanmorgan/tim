# EL-158 · Rise-hold-fall cam named-identity spec

**Current delivery (24 October 2026):** [EL-158 delivery slice](../invest/named-elements.md#element-158). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-158 |
| Name | Rise-hold-fall cam |
| Type | Mechanical |
| Anchor | [requirements.md#element-158](../requirements.md#element-158); [named entry](../invest/named-elements.md#element-158); scope indexes [todo-424](../requirements.md#todo-424) and [todo-375](../requirements.md#todo-375) |
| Proof owner | S346 |
| Refines / extends | No CAT spec. Same cam-unit layout as [EL-156](EL-156-single-lobe-cam.md); drive from [CAT-042 motor](CAT-042-motor.md); follower slider as [CAT-062 spring](CAT-062-spring.md) |
| Related | EL-156 Single-lobe cam, EL-157 Double-lobe cam; contrasts with timer elements such as CAT-033 Hold timer |
| Roadmap story | [EL-158 delivery slice](../invest/named-elements.md#element-158); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the Battery and Motor train) and ENGINE-ISLANDS-SLEEP; then ENGINE-PROFILE-COLLIDER if the fourteen-cylinder fixture fails (owner decision 3a places it before its first consumer, this part); then S7. |

## 2. Declaration

### Disposition and current declaration (21 November 2026)

**Disposition.** Member of the Family slice **Cam profiles** with siblings EL-156 and EL-157, on the condition in the Delivery section. Planned package id `cam_dwell`, directory `elements/CamDwell/`, class `CamDwellElementDeclaration : ISliderElement, IHingeElement, ISceneGeometryElement`. Category Motion, palette order 128 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), display name "Rise-hold-fall cam", description "Lifts its follower, holds it, then lets it down." It has a behaviour delta from both siblings (a dwell), so it is a family member, not a duplicate. The Status row above stays "not started".

Checked against this checkout (`8d1d68aa`); it is the whole declaration, and the harvest-era subsections that followed it are reduced to a stub below (their text is in git history). The housing (body 0), follower slider (body 1), cam hinge, `DriveIn` port, materials and presentation bindings are those of the [EL-156 current declaration](EL-156-single-lobe-cam.md#current-declaration-21-november-2026); footprint 3 bodies, 2 moving, 1 prismatic, 1 revolute. Only the cam body differs.

A dwell needs an arc of constant radius about the axle. No delivered collider is a sector or a convex prism (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:8`–`:11`), and the hollow kinds that do have arcs are static only. The dwell arc is therefore built from delivered cylinders, thirteen at 7.5° spacing. By hand: two neighbours meet in a cusp at radius 0.24·cos 3.75° + √(0.06² − (0.24·sin 3.75°)²) = 0.2974 m, so their common outline lies within 2.6 mm of a 0.30 m arc, and a flat follower riding their tops stays within 0.24·(1 − cos 3.75°) = 0.5 mm of constant height. The draft's seven cylinders at 15° left cusps at 0.2891 m, 10.9 mm inside the drawn arc (repair of 22 November 2026, audit H5).

| Item | Value |
| --- | --- |
| Colliders on body 2 | a base cylinder, radius 0.18, centred on the axle; and thirteen cylinders, radius 0.06, centred on a circle of radius 0.24 about the axle every 7.5° from −45° to +45° from straight down. All have half-width 0.06 and their axes along hinge Z |
| Mass frame | 0.5 kg, centre of mass (0, −0.06, 0), principal moments 0.008, 0.011 and 0.016 (axial) kg·m² (starting values; the slice derives them from the final outline) |
| Initial angle | 0: the arc is at the bottom, the follower rests on the base cylinder |
| Follower rest | foot 0.18 m above the axis (centre at y 0.98, the hinge being at y 0.50 as in EL-156); slider travel −0.005 to +0.3 m. The end cylinder picks the follower up 0.24·sin 60° = 0.208 m off the axis, on the flat of the family's 0.44 m foot |

`lift` (0.12 m) and the dwell sector (90°) are fixed: the requirement says the sector is declared, not player-set, and D13 applies. There is no `LinkageOut` socket and no parameter. `CoordinatePerRadian` is +1.

### Art and solidity (21 November 2026)

`Base`, `GuidePlate` and `Visual/Follower` as EL-156. `Visual/Cam` is fourteen cylinder meshes, each with the radius, half-width and pose of one cam collider (the 0.18 m disc and thirteen 0.06 m cylinders on the 0.24 m circle), classed Solid and matched one to one: the visible-solid check accepts as solid only a box, a full sphere or a straight cylinder (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:274`) and fails on any collider left unmatched (`:284`), so one scalloped mesh would fail it. Their union is the scalloped band: an outer edge at 0.30 m with twelve scallops 2.6 mm deep, rounded ends of radius 0.06 m, and flanks that follow the end cylinder down to the disc. The gold dwell band is printed flat on their outer faces (Decorative). Icon: a disc with a raised arc band under a flat-footed bar. It differs from EL-156 (plain off-centre disc) and EL-157 (peanut) by the flat-topped band.

### Behaviour and numbers (21 November 2026)

With θ the committed cam angle from its start:

| θ | Follower height above rest |
| --- | --- |
| 285° to 75° (150°) | 0, on the base cylinder |
| 75° to 135° | rise: 0.24·cos(135° − θ) − 0.12 |
| 135° to 225° (the dwell) | 0.12 m, constant to within 0.5 mm |
| 225° to 285° | fall: 0.24·cos(θ − 225°) − 0.12 |

- Dwell time is 1.571/ω seconds: 1.57 s at 1 rad/s, 0.79 s at 2 rad/s, 0.26 s at the Motor's default 6 rad/s. Nothing but the shaft angle holds the follower: a stopped shaft holds it for ever, and there is no timer.
- The rise starts with a corner: the follower is picked up at 0.21·ω m/s (0.42 m/s at 2 rad/s). Contact is kept on the flanks while 0.24·ω² is below 17.8 m/s², up to about 8.6 rad/s; above that the follower overshoots the dwell height.
- Peak shaft load about 1.0 N·m including the cam's 0.29 N·m imbalance. A Motor at its 0.1 N·m floor stalls on the rise.
- No signal read; no tick-order rule.

### Harvest-era subsections (Bodies and shapes to Variants)

Superseded by the current declaration (sections 2–3: no parameters, a fixed 90° dwell, the 0.44 m follower foot, `CoordinatePerRadian` +1, no `LinkageOut`); the harvest text is in git history.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

### Capability probe (21 November 2026)

The capability probe below is the whole engine probe (baseline `a6c914e` lists are reduced to a stub at the end of this section). Every row of the [EL-156 probe](EL-156-single-lobe-cam.md#capability-probe-21-november-2026) applies unchanged (6 present, 2 present-untested). This identity adds:

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Several colliders on one moving body, each keeping its own contact | present | `MechanicalPayload.MovingBodyColliders`, `CuriousContraptions.Abstractions/BundleServices.cs:75` | `CompoundMassAndRollingTests.ManyChildSupportKeepsFiniteDissipativeMotionAndFrictionControl` (`:19`) |
| A flat follower crossing from one small cylinder to the next without chatter (fourteen colliders on a turning body) | present, untested | same | none at this count on a hinge-carried body |
| The visible-solid check accepting an art arc matched by cylinders within a fit tolerance | not needed | — | `Visual/Cam` is fourteen cylinder meshes matched one to one with the fourteen cam colliders (Art and solidity above), so no fit tolerance is relied on |
| A sector or convex-prism collider on a moving body | not used; missing if the crossing row above fails | none | ENGINE-PROFILE-COLLIDER (owner decision 3a; before its first consumer, this part) |

Counts: 7 present, 3 present-untested (EL-156's two and the fourteen-collider crossing), 0 narrower, 0 missing. Beyond the engine programme (owner decision 3a), no specific engine story is a prerequisite while the cylinder outline carries the follower smoothly; otherwise ENGINE-PROFILE-COLLIDER is.

**Harvest-era lists (Exists now, Missing).** Superseded by the capability probe above; the harvest text is in git history.

**Dependencies.** A shaft source such as CAT-042 (delivered); shares every capability with EL-156.

## 4. Sources and legacy

- Requirement: "A declared dwell sector physically retains follower height"; outcome "Changing shaft speed changes dwell time; no hidden timer" ([element-158](../requirements.md#element-158)). Map: "Cam geometry/support retains follower during declared dwell sector; hold-animation is not a mechanical dwell."
- Integration under [todo-375](../requirements.md#todo-375); campaign "each cam profile", first use 31–40 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Finite-inertia shaft, hinge guide and convex-hull prism | `engine/SceneRotaryShaft.cs@a6c914e:L13-L42` | carry forward | Cam shaft declaration and shape class. |
| 2 | Slider joint with travel interval | `parts/SpringPart.cs@a6c914e:L35-L48` | carry forward | Follower guide. |

**Files harvested:**
- `engine/SceneRotaryShaft.cs`
- `parts/MotorPart.cs` (rotor radius and mass as scale references)
- `parts/SpringPart.cs` (slider pattern)
- `parts/ConveyorPart.cs` (mechanical input convention)
- Checked, no cam knowledge: `CuriousContraptions.tests/PrescribedBodyTests.cs` (a variable named `follower`), `reference/P0-022-before/docs/coverage/engine/task-017.json` (coverage text naming "wheel/cam/drop" lessons); the `dwell` hits in `engine/` and `CuriousContraptions.tests/` are timer and residence-sensor code.
- Searched with no hit for `cam`, `lobe` or `dwell cam`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-158](../requirements.md#element-158) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe (pending, post-catalogue).** Place battery, motor and the dwell cam; connect `Supply` → `PowerIn` and `Drive` → `DriveIn`; let the follower hold a gate or ball stop. Run at two motor speeds.
- **Outcomes, controls and boundaries** are rows H1–H5 and the shared family rows below. The cam has no parameter, so Save/Load round-trips its pose and links only (shared row C8).
- **Integrations.** Cam, crank, clutch/brake and ratchet integration task under [todo-375](../requirements.md#todo-375); campaign row "Gears ... each cam profile ...": first use 31–40, reuse 41–50, 61–90, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); a timed-hold contrast with CAT-033 Hold timer.

### Acceptance rows (21 November 2026)

Native tests in the family class `CamTests`; measurement and lift tolerance (±0.02 m) as in the [EL-156 acceptance rows](EL-156-single-lobe-cam.md#acceptance-rows-21-november-2026). The shared rows C3 (undriven), C5 (reverse; the profile is symmetric), C6 (weak Motor), C7 (carries a ball), C8 (Reset and load), C9 (wrong connection, generic) and C10 (art and solidity) run for this package as theory rows. Rows of its own:

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| H1 | Given Battery → Motor (`speed` 1 rad/s) → cam, when the committed cam angle is between 140° and 220°, then every sampled lift is within 0.12 ± 0.01 m. The ±0.01 m is contact slop (the scallop is 0.5 mm); a lobe with no dwell would vary by 0.05 m across the same 80° | `ADwellCamHoldsItsFollowerThroughTheDwellSector` | H4 | inside the sector's ends by 5° |
| H2 | Source outcome. Given `speed` 1 and then 2 rad/s, when the time with lift at or above 0.105 m is measured over one revolution, then it is 2.28 s and 1.14 s within ±10 %, and their ratio is 2.0 within ±0.2. This is a deliberate timing contract, so elapsed game time is asserted | `DwellTimeFollowsShaftSpeed` | — | two speeds |
| H3 | Source outcome (no hidden timer). Given the shaft stopped inside the dwell (Motor `speed` set to 0), when 10 s pass, then the lift stays at or above 0.11 m; when the speed is restored, the follower falls only after the cam has turned out of the sector | `AStoppedShaftHoldsTheFollowerIndefinitely` | H2 | — |
| H4 | Given one revolution at 1 rad/s, then exactly one rise, one hold and one fall are observed and the follower is on the base (lift under 0.02 m) between 290° and 70° | `OneRevolutionIsOneRiseHoldAndFall` | C3 | — |
| H5 | Given `speed` 6 rad/s, then the hold is still observed (lift at or above 0.105 m for 0.38 s within ±20 %); given 20 rad/s, then the follower stays inside its travel | `TheHoldSurvivesTheDefaultMotorSpeed` | H2 | either side of the 8.6 rad/s float speed |

No parameter, so no parameter refusal row. H1 to H3 are this identity's part of the todo-375 task. A timed hold made with the Hold timer (CAT-033) is the contrast lesson: that one does not change with shaft speed.

### Integration and delivery (21 November 2026)

- Works with the same parts as EL-156; Hold timer (delivered) as the contrast. Campaign: "each cam profile", first use levels 31–40, reuse 41–50, 61–90, 136–150.
- Coverage binding: a final package-source-binding section in this spec, as EL-239 on published main; blocked on S7 (section 7). A level placing it as a fixture needs S8.
- Delivery: inside the Family slice `family-cam-profiles` ([EL-156 delivery section](EL-156-single-lobe-cam.md#delivery-21-november-2026)); files `elements/CamDwell/CamDwell.csproj`, `CamDwellElementDeclaration.cs`, `cam_dwell.tscn`, `icon.svg`, rows in `CamTests.cs`; engine review tier.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND (the Motor's work debited from its Battery) and ENGINE-ISLANDS-SLEEP; then ENGINE-PROFILE-COLLIDER if the fourteen-cylinder fixture fails; then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 preferred.
- **Condition.** The family slice's probe runs a declared fourteen-cylinder fixture before any package is written. EL-158 stays in the slice only if the follower's height through the dwell varies by no more than 0.005 m at 1 and 2 rad/s (0.5 mm by geometry; the rest is contact behaviour at the cusps). The draft's second condition, the visible-solid check accepting a smooth arc, no longer applies: the cam is drawn as its fourteen collider cylinders. If the fixture fails, EL-158 leaves the slice, EL-156 and EL-157 proceed, and EL-158 waits for a sector or convex-prism collider on a moving body. That capability is ENGINE-PROFILE-COLLIDER (owner decision 3a), which the owner's order places before its first consumer, this part. A declared angle-to-height coupling is not a substitute, because the raised band would then be a visible solid with no collider.

## 6. Open questions

None open. Resolved 21 November 2026:

1. Rise and fall are fixed (60° each, from the end-cylinder geometry) and the dwell is fixed at 90°. D13; the requirement asks for a declared sector, not a player-set one.
2. Output: contact-only follower. D19.
3. `lift` is fixed at 0.12 m. D13.
4. Thirteen arc cylinders at 7.5°, and `Visual/Cam` drawn as the fourteen cylinder meshes of its colliders, matched one to one (22 November 2026, audit H5; art repaired 23 November 2026 after the second audit). Reason: seven at 15° left 10.9 mm of proud art; thirteen leave 2.6 mm, and one cylinder mesh per collider is the only form the visible-solid check accepts.
5. Hinge at y 0.50 and the 0.44 m follower foot follow EL-156 (22 November 2026).

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026; the reasoning is that of [EL-156](EL-156-single-lobe-cam.md#engine-conformance-22-november-2026), with these differences.

1. **Declaration data only.** As EL-156, with a base cylinder and thirteen arc cylinders on the cam body. There is no dwell timer, hold state or angle-to-height coupling: the follower is held because a constant-radius outline is under it.
2. **Generic laws.** As EL-156. Dwell time is the dwell angle divided by the actual shaft speed, by geometry; a stopped shaft holds for ever.
3. **Three layers.** Colliders: seventeen (base, guide plate, follower box, fourteen cam cylinders). Visuals: `Visual/Cam` is fourteen cylinder meshes matched one to one with the cam colliders, scallops included, so art and collider agree with no fit tolerance; the gold dwell band is printed flat. Animation: none; both moving art nodes follow their bodies.
4. **Flow and typing.** As EL-156.
5. **Flat state.** Bodies 3 (1 static, 2 dynamic); colliders 17 (boxes 3, cylinders 14); joints 2 (1 prismatic, 1 revolute); drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 2 (body). No island sleeping exists today; a resting cam and follower cost nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
6. **Performance. One recorded engine gap.** Fourteen colliders on one moving body where its siblings have one and two. Reason: the admitted shape set has no convex profile or sector that a dynamic body can carry, and a constant-radius arc is what a dwell is. Thirteen overlapping cylinders are data, not logic, but they cost fourteen narrowphase candidates against the follower for one smooth surface. The general capability that would replace them with one collider, **a convex extruded-profile (prism) collider admitted on a dynamic body**, is ENGINE-PROFILE-COLLIDER (owner decision 3a; before its first consumer, this part). When it exists this declaration is replaced forward with a single profile collider and the same acceptance rows; no second form is kept. Until then the count is accepted for a part of which a level places one or two.
7. **Tests are scenarios.** Rows H1 to H5 and the shared family rows run the production simulation; H2 asserts elapsed game time because shaft-speed-dependent dwell is the feature.

**Verdict: conforms, with one recorded performance gap whose engine owner is ENGINE-PROFILE-COLLIDER.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `cam_dwell` |
| Named identity | EL-158, `../invest/named-elements.md#element-158` |
| Acceptance | `todo-424`; no sequence task yet |
| Decision | S346, `../invest/scope-corrections.md#s346` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s346` block on published main; `todo-424` is a scope-index row shared by EL-156, EL-157 and EL-158 with no sequence-task anchor directly after it, which the reader requires; and one task source can bind only one package.
