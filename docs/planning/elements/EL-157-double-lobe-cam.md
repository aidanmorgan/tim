# EL-157 · Double-lobe cam named-identity spec

**Current delivery (24 October 2026):** [EL-157 delivery slice](../invest/named-elements.md#element-157). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-157 |
| Name | Double-lobe cam |
| Type | Mechanical |
| Anchor | [requirements.md#element-157](../requirements.md#element-157); [named entry](../invest/named-elements.md#element-157); scope indexes [todo-424](../requirements.md#todo-424) and [todo-375](../requirements.md#todo-375) |
| Proof owner | S345 |
| Refines / extends | No CAT spec. Same cam-unit layout as [EL-156](EL-156-single-lobe-cam.md); drive from [CAT-042 motor](CAT-042-motor.md); follower slider as [CAT-062 spring](CAT-062-spring.md) |
| Related | EL-156 Single-lobe cam, EL-158 Rise-hold-fall cam |
| Roadmap story | [EL-157 delivery slice](../invest/named-elements.md#element-157); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SUPPLY-DEMAND (the Battery and Motor train) and ENGINE-ISLANDS-SLEEP; then S7. |

## 2. Declaration

### Disposition and current declaration (21 November 2026)

**Disposition.** Member of the Family slice **Cam profiles** with siblings EL-156 and EL-158. Planned package id `cam_double`, directory `elements/CamDouble/`, class `CamDoubleElementDeclaration : ISliderElement, IHingeElement, ISceneGeometryElement`. Category Motion, palette order 127 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), display name "Double-lobe cam", description "Two lifts of its follower each turn." It has a behaviour delta from EL-156 (two lifts per revolution), so it is a family member, not a duplicate. The Status row above stays "not started".

Checked against this checkout (`8d1d68aa`); it is the whole declaration. The housing (body 0), follower slider (body 1), cam hinge, `DriveIn` port, materials and presentation bindings are those of the [EL-156 current declaration](EL-156-single-lobe-cam.md#current-declaration-21-november-2026) with no change; footprint 3 bodies, 2 moving, 1 prismatic, 1 revolute. Only the cam body differs:

| Item | Value |
| --- | --- |
| Colliders on body 2 | two cylinders, each radius 0.18, half-width 0.06, axis along hinge Z, centred (−0.12, 0, 0) and (+0.12, 0, 0) in the cam frame. They overlap; a flat follower rides their common outline |
| Mass frame | 0.5 kg, centre of mass on the axis (balanced), principal moments 0.00465 (along the line of centres), 0.01185 (across it) and 0.0153 (axial) kg·m² |
| Initial angle | 0: the line of centres is horizontal, so the follower starts at its lowest point |
| Follower rest | foot 0.18 m above the axis (centre at y 0.98, the hinge being at y 0.50 as in EL-156); slider travel −0.005 to +0.3 m as in EL-156. The cam sweeps to 0.30 m from the axle, 0.12 m clear of the base top; the follower contact is at most 0.12 m off the axis, inside the 0.22 m half-foot |

As for EL-156: there is no convex prism collider and none is needed; `lift` is fixed (0.12 m, D13); there is no `LinkageOut` socket; `CoordinatePerRadian` is +1; no parameters.

### Art and solidity (21 November 2026)

`Base`, `GuidePlate` and `Visual/Follower` as EL-156 (solid, same colliders). `Visual/Cam` is one mesh whose outline is the union of two discs of radius 0.18 m with centres 0.24 m apart, width 0.12 m: solid, matched by the two cylinders exactly. Two gold lobe marks and the axle cap are flush inlays (decorative, printed flat). Icon: a peanut-shaped cam under a flat-footed bar. It differs from EL-156 (one round disc) and EL-158 (disc with an arc band) by its two-lobed outline and two marks.

### Behaviour and numbers (21 November 2026)

- Follower height above rest: y(θ) = 0.12·|sin θ|. Two lifts of 0.12 m per revolution, 180° apart.
- At each valley the profile has a corner: the follower arrives at 0.12·ω m/s downward and leaves at the same speed upward (0.72 m/s at the Motor's default 6 rad/s). Restitution is 0, so this is a firm tap, not a bounce. It is the real behaviour of a two-disc cam and is why the lesson speed is 2 rad/s.
- Contact is kept while 0.12·ω² is below g + k·y/m = 9.81 + 8 = 17.8 m/s² at the top: up to about 12 rad/s.
- Peak shaft load about 0.65 N·m; the cam is balanced. A Motor at its 0.1 N·m floor stalls on the rise.
- No signal read; no tick-order rule.

### Harvest-era subsections — removed; the text is in git history.

The current declaration above is the whole declaration. Two separately observable lifts come from geometry alone; spring and follower potential energy are supplied only through `DriveIn`. Colours as EL-156: cam `#d69c47` with two gold `#f7cb52` lobe marks, one per lobe, follower cream `#fff8e9`, navy `#293954` base. One outcome in the row; no variants.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

### Capability probe (21 November 2026)

The harvest-era "Exists now" and "Missing" lists (baseline `a6c914e`) are removed; their text is in git history. Every row of the [EL-156 probe](EL-156-single-lobe-cam.md#capability-probe-21-november-2026) applies unchanged (6 present, 2 present-untested: a slider-carried box riding a hinge-carried cylinder, and an offset collider on a hinge-carried body). This identity adds one row:

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Several colliders on one moving body, each keeping its own contact | present | `MechanicalPayload.MovingBodyColliders`, `CuriousContraptions.Abstractions/BundleServices.cs:75` | `CompoundMassAndRollingTests.ManyChildSupportKeepsFiniteDissipativeMotionAndFrictionControl` (`:19`); `EveryDeclaredChildUsesItsCommittedMotionAndResetAndRefusesForeignSets` (`:1021`) |

Counts: 7 present, 2 present-untested, 0 narrower, 0 missing. Beyond the engine programme (owner decision 3a), no specific engine story is a prerequisite.

**Dependencies.** A shaft source such as CAT-042 (Story 11.1); shares every capability with EL-156, so it should follow it.

## 4. Sources and legacy

- Requirement: "Two geometric lobes drive two separately observable lifts per revolution"; outcome "One revolution cannot be recorded as a single-lobe cycle" ([element-157](../requirements.md#element-157)).
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
- Checked, no cam knowledge: `CuriousContraptions.tests/PrescribedBodyTests.cs` (a variable named `follower`), `reference/P0-022-before/docs/coverage/engine/task-017.json` (coverage text naming "wheel/cam/drop" lessons).
- Searched with no hit for `cam` or `lobe`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-157](../requirements.md#element-157) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place battery, motor and the double-lobe cam; connect `Supply` → `PowerIn` and `Drive` → `DriveIn`; feed balls onto the follower ledge. Run for one revolution.
- **Positive.** One revolution gives two follower lifts, each releasing one ball; the lift count per revolution is 2.
- **Negative/control.** Replacing it with EL-156 under the same drive gives one lift per revolution; an unpowered cam gives none; half a revolution gives exactly one lift.
- **Boundaries.** Shaft speeds below and above about 12 rad/s (follower keeps or loses contact); reverse rotation.
- **Run/Reset.** Cam angle and follower height return to authored values.
- **Save/Load.** Pose and Mechanical links round-trip.
- **Integrations.** Cam, crank, clutch/brake and ratchet integration task under [todo-375](../requirements.md#todo-375); campaign row "Gears ... each cam profile ...": first use 31–40, reuse 41–50, 61–90, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); paired with EL-156 as its single-lobe contrast.

### Acceptance rows (21 November 2026)

Native tests in the family class `CamTests`; measurement, tolerance (±0.02 m of lift) and lift-counting rule as in the [EL-156 acceptance rows](EL-156-single-lobe-cam.md#acceptance-rows-21-november-2026). The shared rows C3 (undriven), C5 (reverse), C6 (weak Motor), C7 (carries a ball), C8 (Reset and load), C9 (wrong connection, generic) and C10 (art and solidity) run for this package as theory rows. Rows of its own:

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| D1 | Given Battery → Motor (`speed` 2 rad/s) → cam, when the cam has turned three revolutions (guard 30 s), then exactly six lifts are counted, each peaking within 0.12 ± 0.02 m | `ADoubleLobeCamLiftsItsFollowerTwicePerRevolution` | D2 | — |
| D2 | Source outcome. Given the same rig with `cam_single` in place of `cam_double`, then three lifts are counted in three revolutions, so the two counts differ and one revolution of this cam is never one cycle | `TheSingleLobeCamGivesHalfAsManyLiftsInTheSameRig` | D1 | — |
| D3 | Given the shaft stopped (Motor `speed` set to 0) after half a revolution, then exactly one lift has been counted and the follower is back within 0.02 m of rest | `HalfARevolutionGivesExactlyOneLift` | D1 | half revolution |
| D4 | Given the shaft stopped anywhere on a rise, then the lift equals 0.12·|sin θ| for the committed angle within ±0.02 m | `APartialTurnLiftsOnlyTheProfileHeightAtThatAngle` (theory row) | D1 | — |
| D5 | Given `speed` 8 rad/s, then lifts still equal twice the revolutions; given 20 rad/s, then the follower stays inside its travel | `TheFollowerKeepsCountBelowTheFloatSpeedAndStaysBoundedAboveIt` (theory row) | D1 | either side of the 12 rad/s float speed |

No parameter, so no parameter refusal row.

### Integration and delivery (21 November 2026)

- Works with the same parts as EL-156. Campaign: "each cam profile", first use levels 31–40, reuse 41–50, 61–90, 136–150; it is taught against EL-156 as the contrast.
- Coverage binding: a final package-source-binding section in this spec, as EL-239 on published main; blocked on S7 (section 7). A level placing it as a fixture needs S8.
- Delivery: inside the Family slice `family-cam-profiles` described in the [EL-156 delivery section](EL-156-single-lobe-cam.md#delivery-21-november-2026); files `elements/CamDouble/CamDouble.csproj`, `CamDoubleElementDeclaration.cs`, `cam_double.tscn`, `icon.svg`, rows in `CamTests.cs`; engine review tier (two present-untested capabilities).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SUPPLY-DEMAND (the Motor's work debited from its Battery) and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 preferred.
- Disqualifies the plan, in addition to EL-156's items: the follower chatters or sticks at the valley corner at 2 rad/s (more than one counted lift per lobe), or resting on both cylinders at once produces drift along the slider. Either is an engine contact defect for its own slice.

## 6. Open questions

None open. Resolved 21 November 2026:

1. The two lobes are equal. D19 (the spec's proposal); unequal lobes are not asked for by any row.
2. Output: contact-only follower. D19.
3. `lift` is fixed at 0.12 m. D13.
4. Hinge at y 0.50 and the 0.44 m follower foot follow EL-156 (22 November 2026); this cam itself never reached the base.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026; the reasoning is that of [EL-156](EL-156-single-lobe-cam.md#engine-conformance-22-november-2026), with these differences.

1. **Declaration data only.** As EL-156, with two overlapping cylinders on the cam body. No profile function or lift count exists in code; two lifts per revolution are the outline of two discs.
2. **Generic laws.** As EL-156. y(θ) = 0.12·|sin θ| is a hand consequence; the valley corner is real contact between the follower and the second cylinder.
3. **Three layers.** Colliders: five (base, guide plate, follower box, two cam cylinders). Visuals: the cam mesh is the union of the two discs, matched exactly by the two cylinders; marks inlaid flush. Animation: none; both moving art nodes follow their bodies.
4. **Flow and typing.** As EL-156.
5. **Flat state.** Bodies 3 (1 static, 2 dynamic); colliders 5 (boxes 3, cylinders 2); joints 2 (1 prismatic, 1 revolute); drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 2 (body).
6. **Performance.** One more collider than EL-156, because the outline has two lobes. Nothing is cosmetic in physics. No island sleeping exists today; a resting cam and follower cost nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
7. **Tests are scenarios.** Rows D1 to D5 and the shared family rows run the production simulation.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `cam_double` |
| Named identity | EL-157, `../invest/named-elements.md#element-157` |
| Acceptance | `todo-424`; no sequence task yet |
| Decision | S345, `../invest/scope-corrections.md#s345` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s345` block on published main; `todo-424` is a scope-index row shared by EL-156, EL-157 and EL-158 with no sequence-task anchor directly after it, which the reader requires; and one task source can bind only one package.
