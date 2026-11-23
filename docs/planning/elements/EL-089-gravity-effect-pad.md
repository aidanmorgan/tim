# EL-089 · Gravity-effect pad — named-identity spec

**Current delivery (24 October 2026):** [EL-089 delivery slice](../invest/named-elements.md#element-089). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 are written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lstart-Lend` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-089 · Gravity-effect pad · Specialist |
| Anchor | [requirements.md#element-089](../requirements.md#element-089) ([sequence-task-642](../requirements.md#sequence-task-642)); [named entry](../invest/named-elements.md#element-089); scope index [todo-216](../requirements.md#todo-216) |
| Proof owner | S659 |
| Disposition | To deliver as a single element. Not a duplicate and not a Family member: no delivered or planned part declares an acceleration region |
| Planned package | id `gravity_pad`, directory `elements/GravityPad/`, class `GravityPadElementDeclaration` |
| Refines / extends | No CAT spec |
| Related | EL-124 Authored gravity preset (the same field law, scene-wide); EL-098 Pool ball (its variant B would reuse it); EL-103 Accelerator tube (the same engine story) |
| Status | not started; blocked on the engine programme (owner decision 3a), then `ENGINE-FIELD-FORCE`, then S7 |

## 2. Declaration

The pad is a static plate that declares one bounded acceleration field above it. The field is an instance of the general field law; the package holds no rule of its own. It implements `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and the field-source interface that `ENGINE-FIELD-FORCE` adds. Closest delivered shape: the Wall as a static box (`elements/Wall/`). `KinematicMode.Static`, `SupportedDomains = Mechanical`, zero mass frame. Footprint: 1 body, 0 moving, 0 constraints.

### Body and collider

One static body, dimensions in metres, local +Y up, origin at the centre of the plate.

| Collider | Kind | Dimensions |
| --- | --- | --- |
| Plate | Box | 1.6 × 0.12 × 1.6 (half extents 0.8, 0.06, 0.8) |

Material: restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (**starting value**: a dull landing surface with ordinary grip). The plate is one Receiver footprint, a placeable floor tile.

### Field source (one, declared)

| Field | Value | Reason |
| --- | --- | --- |
| Frame | body 0 | turning the pad turns the region and the vector |
| Region | box from (−0.8, 0.06, −0.8) to (0.8, 3.06, 0.8) | the plate's footprint, 3 m tall: enough height for a visibly slow fall or a rise. Fixed (D13) |
| Quantity | Acceleration (m/s²), no falloff | "changes acceleration inside its declared volume" |
| Vector | (0, `lift`, 0) in the frame | added to each body's own declared gravity |
| Selector | target selector, every dynamic body (today's `BodyTargetKind.AllDynamic`, `CuriousContraptions.Abstractions/ContactWorkDeclarationValues.cs:31`) | gravity acts on everything |
| Membership | centre of mass inside the box; binary | outside bodies keep exactly their own gravity |
| Payment | None | the requirement names no supply; the field is labelled fiction |

### Parameter

| Name | Type | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `lift` | Real | MetrePerSecondSquared | 7.36 | −4.9 | 14.7 | Reject | BuildOnly |

With the pad upright and world gravity 9.81 m/s² downward, the effective gravity inside is (9.81 − `lift`) downward: 2.45 m/s² (a quarter of gravity) at the default, 14.71 (one and a half times) at the minimum, and 4.89 m/s² **upward** at the maximum. `lift` = 9.81 is weightless. The magnitude stays under the 16 m/s² that the Balloon's lift parameter already admits (`elements/Balloon/BalloonElementDeclaration.cs:24`).

### Ports, stores, metadata

No ports, stores, sensors, signal or supply nodes. Category `Motion`, display name "Gravity pad (fiction)", description "Game fiction, not physics: bodies above this pad fall with a different gravity. Outside the column, gravity is normal.", palette order 169 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.97, 0.80, 0.32). The display name and description are where the requirement's label is shown: the palette entry, the part's tooltip and its settings panel all read them. A star glyph printed on the plate marks the part itself.

### Art and solidity

| Node | Mesh | Layer and class |
| --- | --- | --- |
| `Visual/Plate` | cream `#fff8e9` box 1.6 × 0.12 × 1.6 with a navy `#293954` rim drawn flush | solid, the Box collider; static |
| `Visual/Star` | gold `#f7cb52` five-point star printed on the top face, flush | decorative: printed flat |
| `Visual/Column` | pale green `#efffbd` box 1.6 × 3.0 × 1.6 at low alpha, unlit, with four thin edge lines | decorative: glow. It is a drawn light volume, not a surface; nothing rests on it, and it is drawn the way a beam is. Static |
| `Visual/Arrows` | three upward chevrons drawn inside the column | decorative: glow. One parameter binding sets their `ScaleY` to `lift` × (1 / 9.81) − 1: positive and upright when bodies would rise, negative and so pointing down when they fall, zero (invisible) at weightless. A parameter binding is multiplier × value + offset only (`CuriousContraptions.Abstractions/ElementPresentation.cs:119`), which this is |

If a negative scale is not admitted by the presentation layer, the arrows are left out and the column alone marks the region; no threshold logic is added for them. Icon: a tile with a star and a dashed column holding a floating ball. No delivered part has a drawn column, so it is distinct by shape.

### Behaviour and numbers

Gravity is one uniform-field law (coordinator decision 16, 22 November 2026; `ENGINE-FIELD-FORCE`, section 3): the field at a body is the sum of the fields whose region contains its centre of mass, so in the Standard scene a body's effective gravity is its own declared gravity plus the vector of each acceleration field it is inside, and buoyant lift is scaled with it. Contact, friction and every other law are unchanged. Hand calculations with the pad upright:

- **Slow fall (default).** A ball released 2.5 m above the plate's top falls in t = √(2 × 2.5 / 2.4525) = 1.43 s and lands at 3.50 m/s; the same ball outside falls in √(5 / 9.81) = 0.71 s and lands at 7.00 m/s. Twice the time, half the speed, for any mass.
- **Rise (`lift` 14.7).** A Basketball resting on the plate (centre at 0.06 + 0.34 = 0.40) accelerates upward at 4.89 m/s² for 2.66 m to the region's top, leaving at √(2 × 4.89 × 2.66) = 5.10 m/s. Outside, world gravity returns: it rises another 5.10² / (2 × 9.81) = 1.33 m to about y = 4.39, falls back, re-enters at 5.10 m/s and is slowed to rest near the plate. With no losses it bobs; restitution and drag end it.
- **Heavier (`lift` −4.9).** Effective gravity 14.71 m/s²: the 2.5 m fall takes 0.58 s.
- **A Balloon inside (buoyancy scaled with the field).** The default Balloon declares 11.5 m/s² of lift against 9.81 of gravity (`BalloonElementDeclaration.cs:24`): outside it rises at 1.69 m/s², reaching 1.69 / 0.4 = 4.2 m/s. Inside at the default, effective gravity is 0.25 of normal, so its lift is 11.5 × 0.25 = 2.875 and its net rise is 2.875 − 2.4525 = **0.42 m/s²**, reaching 1.06 m/s: it rises four times more slowly, as it would on a small moon. At `lift` 14.7 effective gravity points up at 4.89, the buoyant lift (11.5 × 4.89 / 9.81 = 5.73) points down, and the Balloon **sinks** at 0.84 m/s² while a ball beside it rises. Had the lift not been scaled, the Balloon would shoot up at 9.05 m/s², which is why the law scales it.
- **Tilted pad.** The vector turns with the pad, so a pad on its side pushes bodies sideways at `lift` while gravity still pulls them down.

A body that leaves the column sideways while rising keeps the height it gained: the field does net work on it. That is the stated fiction; the source's one committed work read keeps it observable and no store pays for it (section 6).

## 3. Engine capabilities

Map row families: EnvironmentState, GeometryQuery, RigidBodyDynamics.

| Capability | Status | Evidence |
| --- | --- | --- |
| Static box body with a declared material | present | `ColliderDescriptor.cs:8`; `WorkshopWallTests.ThreeCanonicalDimensionsPoseAndIdentityRoundTripAtDefaultAndBothLimits` (`CuriousContraptions.tests/WorkshopWallTests.cs:15`) asserts the compiled static body and its one Box |
| A constant world acceleration per body, declared once and fixed for the run | present | `WorldAccelerationDeclaration` (`CuriousContraptions.Abstractions/WorldAccelerationDeclaration.cs:8`); `BalloonTests.NondefaultLiftAndStrongDragChangeStandardMotion` (`CuriousContraptions.tests/BalloonTests.cs:140`) proves a per-body upward acceleration of 14 m/s² against gravity |
| A body-carried ambient load (lift and drag on the declaring body only) | narrower | `IAmbientLoadElement` (`MechanicalContracts.cs:124`), `AmbientLoadDescriptor` (`CuriousContraptions.Abstractions/GasCouplingValues.cs:19`): it acts on the body that declares it, never on other bodies in a region, and its lift is not tied to gravity |
| A bounded region that changes the acceleration of bodies inside it, with buoyancy scaled consistently | missing | [`ENGINE-FIELD-FORCE`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-field-force-the-one-uniform-field-law-and-declared-force-regions), in the single text below |
| Region membership candidates without visiting every body | missing | `ENGINE-SPATIAL-QUERIES` (owner decision 3a), which `ENGINE-FIELD-FORCE` takes its candidates from |
| A body at rest on the plate costs nothing until the field or a contact moves it | missing | `ENGINE-ISLANDS-SLEEP` (owner decision 3a): no sleeping exists today; its contract wakes an island on a force region |
| Parameter-bound art scale, including a negative value | present, untested | `PresentationParameterBinding` (`ElementPresentation.cs:119`); delivered uses bind positive scale and position |

**The story's text.** `ENGINE-FIELD-FORCE` today describes a source acting on "bodies carrying a declared responding material property" with a falloff and a reaction, and lists the Gravity-type identities under *Unblocks* without a rule for them. Gravity is the one uniform-field law of coordinator decision 16, owned by `ENGINE-FIELD-FORCE`. Its text is stated once, with [EL-124](EL-124-authored-gravity-preset.md) (the unbounded, scene-wide region of the same law), in `readiness/batch-m.md` section 4 item 7, and this spec cites it rather than restating it: a uniform acceleration field declares a vector and a region, unbounded (the scene) or a bounded volume; the field at a body is the sum of the fields whose region contains its centre of mass; a body's declared gravity is its response to the Standard field; a declared buoyant ambient load scales with the same field, inside a region and in the scene alike; bounded regions take candidates from `ENGINE-SPATIAL-QUERIES`, and their work is bounded by the bodies inside; the story sets the bound on a summed field. This pad is a bounded region of that law.

The clauses this batch adds to the same story are additions, not a second law (this batch's audit E5; EL-103 needs the Force quantity and paid work): a bounded region is a box or cylinder on a frame body; a selector (a responding property or a target selector); the vector's quantity is a closed enum {Force (N), Acceleration (m/s²)}, optionally with falloff from a closed enum; membership by centre of mass, waking sleepers; a payment kind from a closed enum {None, StandingDraw, PaidWork within a declared power bound, scaled proportionally}; the reaction on a dynamic frame; one work read per source; a declared buoyant lift directed against the summed field and scaled by its magnitude ÷ the Standard field's.

This pad declares: box region, target selector, Acceleration, no falloff, payment None. Under a non-Standard scene field (EL-124) the pad's vector adds to it: in the Light preset (4.905 m/s²) the default pad leaves 2.455 m/s² upward. **Summed-field bound.** This pad needs the story to admit a summed field of magnitude up to 14.71 m/s² (Standard scene, `lift` −4.9) and pointing upward (4.89 m/s² at `lift` 14.7, rows A4 and A7). If the story sets a lower bound or refuses an upward sum, the range of `lift` is narrowed to it at slice planning, a declared-value change; a level that combines a scene field above 11.1 m/s² with a negative `lift` meets the story's bound and its refusal applies.

## 4. Sources and legacy

- Requirement: "Bounded authored field changes acceleration inside its declared volume"; outcome "Outside bodies retain world gravity; label the field as deliberate game fiction" ([element-089](../requirements.md#element-089)).
- Legacy search at `a6c914e` for "gravity pad", "antigrav" and "gravity scale" found no pad source.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | World gravity 9.81 was an authored machine field | `engine/MachineData.cs@a6c914e:L170-L171` | Carry forward the world default; today it is each body's declared gravity (`elements/Ball/BallElementDeclaration.cs:56`) |
| L2 | World gravity could change only while idle | `reference/cpu/MachineWorld.cs@a6c914e:L185-L189` | Carry forward as "`lift` is Build-only" |
| L3 | Legacy test worlds set gravity 0 to isolate a behaviour | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L65-L70` | Do not carry forward as a player feature: weightlessness exists only inside this labelled field |

**Files harvested:** `engine/MachineData.cs`, `reference/cpu/MachineWorld.cs` (gravity property only), `CuriousContraptions.tests/PipeTests.cs` (test set-up only).

## 5. Acceptance outline

Requirement row: [element-089](../requirements.md#element-089). Planned test class `GravityPadTests`. All rows use an upright pad unless stated, and all fit the default 30 s Run.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a Basketball released 2.5 m above the plate inside the column and another at the same height 2 m beside it, when run, then the inside ball lands later and slower | `ABallInsideTheColumnFallsSlowerThanOneBeside` | the outside ball | inside landing time 1.43 s within 0.15 s and speed 3.5 m/s within 0.4; outside 0.71 s within 0.1 s. A dead field gives 0.71 s, far outside the band |
| A2 | Given the same construction without the pad, then the outside ball's fall is the same as in A1 | `ABodyOutsideKeepsWorldGravity` | A1's inside ball | landing time within 0.02 s and landing speed within 0.1 m/s of the no-pad run |
| A3 | Given a Bowling ball (4 kg) and a Tennis ball (0.35 kg, drag set to 0) released together inside, then they land together | `TheFieldIsAnAccelerationNotAForce` | — | landing times within 0.05 s of each other; a Force-quantity field would separate them by more than 0.5 s |
| A4 | Given `lift` 14.7 and a Basketball resting on the plate, then it rises, leaves the top of the column, and comes back down under world gravity | `AStrongFieldLiftsABallWhichFallsBackFromOutside` | `lift` 7.36: the ball stays on the plate | peak height between 3.9 and 4.6 m (4.39 by hand; the band covers drag); it is above 3.06 m and descending within a 5 s guard |
| A5 | Given a ball whose centre is 0.05 m outside the column's side face and one 0.05 m inside, then only the second falls slowly | `MembershipFollowsTheCentreOfMassAtTheBoundary` | — | 0.05 m each side of the face: well above contact slop, well below a ball radius |
| A6 | Given the pad turned 90° about Z with `lift` 7.36, then a ball in the region gains sideways speed along the pad's local +Y while still falling | `TurningThePadTurnsTheField` | upright pad: no sideways speed | sideways speed after 0.5 s is 3.68 m/s within 0.4 |
| A7 | Given a default Balloon inside the column and one outside, then the inside one rises more slowly; given `lift` 14.7, the inside Balloon sinks while a Basketball beside it in the column rises | `BuoyancyIsScaledWithTheField` | the outside Balloon | upward speed after 1 s: inside under 0.6 m/s (0.35 by hand), outside above 1.1 m/s (1.39 by hand). At 14.7: Balloon moving down and ball moving up after 1 s. An unscaled lift gives the inside Balloon over 3 m/s upward |
| A8 | The catalogue entry's name contains "fiction" and its description states that the field is game fiction | `TheEntryLabelsTheFieldAsFiction` | — | exact metadata strings at the bundle boundary |
| A9 | `lift` 14.8 and −5.0 are refused with the prior construction unchanged; a `lift` edit during Run is refused; Reset restores every body; save and load keep `lift` and pose | `LiftIsValidatedAndStateRoundTrips` | 14.7 and −4.9 are accepted | generic: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

The part has no ports, so no connection refusal applies. A7's hand values: speed after 1 s is (a / 0.4)(1 − e^−0.4) with the Balloon's drag 0.4 /s, giving 0.35 m/s for a = 0.42 and 1.39 m/s for a = 1.69.

### Integration

- Works with every dynamic part: Basketball, Bowling ball, Tennis ball, Balloon, Domino, Weight (all delivered); Ramp and Wall as supports. EL-124's scene-wide gravity and EL-098 Pool ball variant B, if adopted, are other declarations of the same field law.
- Campaign: chapter 10 "Oddly Satisfying", levels 91–100, "gravity pad/blimp" (`docs/planning/requirements.md:2295`); first use 91–100, reuse 136–150 (`:2411`). A level that fixes a pad in place needs the lesson-fixture story (S8).
- Coverage binding: section 7.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a; of its stories this part leans on `ENGINE-SPATIAL-QUERIES` for membership candidates and `ENGINE-ISLANDS-SLEEP` for resting bodies); then `ENGINE-FIELD-FORCE` in the text of section 3; then S7.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (`AllDynamic`).
- **Files.** `elements/GravityPad/` (project, declaration, `gravity_pad.tscn`, `icon.svg`); `CuriousContraptions.tests/GravityPadTests.cs`; one `CuriousContraptions.slnx` line; until S3, the remaining per-package lists.
- **Review tier.** Data-only if the engine story publishes every row of section 3 with tests and a negative art scale is admitted or the arrows are dropped; otherwise engine.
- **Disqualifiers the probe re-checks.** `ENGINE-FIELD-FORCE` delivered without the Acceleration quantity, without a target selector, or without buoyancy scaling (A7 fails); membership found by a pass over every body; a resting body that the field does not wake.

## 6. Open questions

All resolved.

1. **Binary membership by centre of mass**, no blending (the story's rule).
2. **Direction follows the pad's orientation**; there is no separate direction parameter (D19).
3. **One parameter in m/s²**, not a scale of gravity: a scale is ambiguous once the pad is tilted, and the field adds a vector.
4. **Fixed column height 3 m** (D13).
5. **Energy.** A field that lifts a body does work no store pays for (payment None). The requirement authorises exactly this by requiring the label; the source's work read keeps it observable. Decided under the standing delegation.
6. **No on/off input** (D19): no requirement or lesson uses one.
7. **Buoyancy is scaled with the field** (coordinator decision 16): a Balloon inside rises more slowly at the default and sinks in a reversed field.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`), owner decision 3a and coordinator decision 16.

1. **Declaration data only.** A static plate, one field source, one parameter, art. No handler, no per-tick callback, no test on bodies in package code.
2. **Generic laws.** The one uniform-field law with the Acceleration quantity (declared region, vector, selector, payment) and ordinary contact. The capability is general and missing; its one owner is `ENGINE-FIELD-FORCE`.
3. **Three layers.** Colliders: one Box. Visuals: plate (solid), star (printed), column and arrows (glow). Animation: none in Run; one parameter binding sets the arrows' scale in Build. No art node is moved by a body pose or a run-time binding, and nothing drawn feeds physics.
4. **Flow and typing.** One Real parameter with a unit; quantity, payment and target set are closed enums; no ports.
5. **Flat state.** Bodies 1 (static); colliders 1 (Box); joints 0; drives 0; sensors 0; field sources 1 (one work read); domain nodes 0; stores 0; presentation bindings 1 (parameter).
6. **Performance.** One primitive collider; one bounded box region whose candidates come from `ENGINE-SPATIAL-QUERIES`; no state per source and body. Nothing sleeps today; resting bodies sleep and are woken by the field once `ENGINE-ISLANDS-SLEEP` lands. Nothing here is designed around either gap.
7. **Tests are scenarios.** Every row runs the production simulation with delivered balls and Balloons and proves the field law's consequence for these values.

**Verdict: conforms** (general capabilities missing, each with one owning story).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `gravity_pad` |
| Named identity | EL-089, `../invest/named-elements.md#element-089` |
| Acceptance | `todo-216`, `sequence-task-642` (`docs/planning/requirements.md:5109`) |
| Decision | S659, `../invest/scope-corrections.md#s659` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s659` block; and `todo-216` is a scope index shared with EL-090 (`docs/planning/requirements.md:4096`) that is not anchored beside `sequence-task-642`, which the reader's task-source rule requires. The sequence task itself exists.
