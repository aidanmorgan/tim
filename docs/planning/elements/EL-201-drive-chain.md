# EL-201 · Drive chain named-identity spec

**Current delivery (24 October 2026):** [EL-201 delivery slice](../invest/named-elements.md#element-201). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-201 |
| Name | Drive chain |
| Type | Mechanical |
| Anchor | [requirements.md#element-201](../requirements.md#element-201); [named entry](../invest/named-elements.md#element-201); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S358 |
| Refines / extends | No CAT spec; a connection, not a catalogue part. Shares the mechanical ports of [CAT-042 motor](CAT-042-motor.md) and [CAT-019 conveyor](CAT-019-conveyor.md) |
| Related | EL-200 Drive belt (must not be interchangeable), EL-199 Electric motor, EL-110 Flywheel |
| Roadmap story | [EL-201 delivery slice](../invest/named-elements.md#element-201); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (the solid chain loop as a query candidate) and ENGINE-ISLANDS-SLEEP; then ENGINE-MECHANICAL-LINKS (the chosen material, engagement classes, pitch radii) and ENGINE-CONNECTION-MATERIALS (solid strand: the visible loop is thick, owner decisions 1 and 2); then EL-200; then S7 if the material is a package |

## 2. Declaration

### Disposition and current declaration (21 November 2026)

**Disposition (owner decision 2, 22 November 2026).** A **new** declared connection material that the player chooses when drawing a mechanical connection, not a placed part: no body and no palette entry. The delivered rigid mechanical connection is unchanged and remains the default; the chain is a choice beside it, as the belt is. Whether its declaration lives in a package of its own is fixed by ENGINE-MECHANICAL-LINKS together with Batch I's `steel_cable`. It is not a Duplicate closure: its coupling is slip-free with the rigid link's coordinate ratio, but the typed toothed engagement, the refusal of an incompatible attachment and the solid link loop do not exist.

**What is delivered today** (checked on `8d1d68aa`). The slip-free law the chain needs is the law every mechanical connection has now: a `TransmissionConstraintKind.Bilateral` ratio coupling, ratio = output `CoordinatePerRadian` ÷ input `CoordinatePerRadian` (`engine/gpu/WorkshopPhysicsCompiler.cs:583`–`:599`). What is absent: a connection kind, an engagement type on a port, a refusal when they do not match, and link-by-link artwork.

**Declared values.**

| Where | Name | Type | Values | Policy |
| --- | --- | --- | --- | --- |
| Connection | material | typed id of a declared connection material ([EL-200 section 2, Carrier](EL-200-drive-belt.md#2-declaration)) | `drive_chain`: law `TransmissionConstraintKind.Bilateral`, speed-ratio source "pitch-radius ratio of its two ports" (declared data, coordinator decision 24l), no values, required engagement class Toothed, link presentation style "links", solid strand of declared thickness | chosen when the connection is drawn; Build only; an unknown id is refused |
| Each mechanical port | engagement | a typed set of engagement classes, proposed `MechanicalEngagement { Smooth, Toothed }` | per port, table below | each connection material declares the class it requires at both ends (`drive_chain`: Toothed; `drive_belt`: Smooth); the default rigid link requires no class and stays admitted at every mechanical port (owner decision 2); the engine compares classes and knows no material by name; otherwise the edit is refused with a typed reason and nothing changes |

A Chain carries no `tension` or `friction`; a Chain record that carries either is refused. The earlier `teeth_in` and `teeth_out` parameters are dropped. **Ratio (coordinator decision 24l, 23 November 2026).** The material declares, as data, that its speed ratio is the pitch-radius ratio of its two ports: ω_out = ω_in × r_in ÷ r_out, with r the ports' declared physical pitch radii ([EL-200 section 2](EL-200-drive-belt.md#2-declaration): Wound spring 0.26 m, Clutch 0.28 m, Motor 0.30 m, Reverse transmission 0.32 m). The chain therefore moves at one link speed around both rims, as a real chain does, and its drawn teeth turn with it; equal teeth on unequal rims are not used. The belt keeps the coordinate ratio (EL-200), and choosing a ratio by tooth count stays the lesson of EL-232 Gears (D19); owner decision 8 ("gear ratios follow tooth counts") applies to gears (EL-232, CAT-057), where it was raised. Pitch is a nominal 0.08 m, used for artwork only: a rim shows n = round(2π r ÷ 0.08) teeth (17 at 0.22 m, 24 at 0.30 m) spaced 2π r ÷ n apart, so every rim shows whole teeth passing at the link speed. Marks advance at the link speed, pitch radius × committed rotation, which is the same at both rims.

**Engagement data on delivered ports.** Adding an engagement value to a delivered port is declaration data only and changes no delivered connection: the default rigid link stays admitted at every port whatever its classes (owner decision 2). The table exists so that the required refusal has a real example among delivered parts; it no longer depends on any change to delivered links.

**Which ports are toothed** (decided now by game intent, so that the required incompatibility has a real example among delivered parts; every port stays `Smooth` as well unless stated):

| Part | Port | Engagement | Reason |
| --- | --- | --- | --- |
| Motor | `Drive` | Smooth, Toothed | the source of loads a belt cannot carry (up to 100 N·m) |
| Reverse transmission, Clutch | `DriveIn`, `Drive` | Smooth, Toothed | pass heavy drive through |
| Wound spring | `DriveIn` | Smooth, Toothed | winding is the heavy load |
| Flywheel, the three cams (planned) | all | Smooth, Toothed | heavy or jolting loads |
| Conveyor | `DriveIn`, `Drive` | Smooth only | a belt roller |
| Windmill | `Drive` | Smooth only | a light sail shaft |
| Fan | `DriveIn` (Shaft mode) | Smooth only | a light rotor on a 0.14 m pulley (`elements/Fan/FanElementDeclaration.cs:64-67`) |
| Governor (planned) | `DriveIn` | Smooth only | a light instrument |

**Bodies, stores, sensors, parameters of its own.** None. The chain is massless.

### Art and solidity (21 November 2026)

The chain is drawn by the link presentation as a closed loop of separate pale grey `#ccd8dc` links at the 0.08 m pitch instead of the belt's unbroken navy loop; toothed ports draw teeth on their rim. Links against a smooth loop is a shape difference, so belt and chain are told apart without colour. **The loop is solid:** a visible chain loop is thick and therefore solid (owner decisions 1 and 2); its collider is the material's declared strand (thickness 0.04 m, **proposed**, equal to the drawn links; segment rule of ENGINE-CONNECTION-MATERIALS), compiled from declarations. Blocked on ENGINE-CONNECTION-MATERIALS (solid strand).

### Behaviour and numbers (21 November 2026)

- ω_out = ω_in × r_in ÷ r_out at all times, r the two ports' declared pitch radii (coordinator decision 24l); there is no capacity and no slip. Torque is limited only by the source and the shafts, and the output torque is the input torque × r_out ÷ r_in.
- A stalled load stalls the source: with the driven shaft held, a supplied Motor is held too and does no work (the delivered behaviour of a held shaft, `MotorTests.AStalledMotorDoesNoWorkOnceItsShaftIsHeld`, `:732`).
- The contrast with the belt is numeric: the strongest belt passes 74.8 N·m at a 0.22 m pulley ([EL-200](EL-200-drive-belt.md#behaviour-and-numbers-21-november-2026)); a chain takes the Motor's full 100 N·m at the Motor shaft and never slips.
- No signal read; no tick-order rule.

### Harvest-era subsections (Bodies and shapes to Variants)

Superseded by the current declaration (section 2: a declared connection material with no values, engagement classes on the ports instead of a Sprocket socket kind, no `teeth_in` or `teeth_out`, a solid loop); the harvest text is in git history.

## 3. Engine capabilities

### Capability probe (21 November 2026)

The capability probe below is the whole engine probe; the baseline `a6c914e` lists are reduced to a stub at the end of this section.

| Capability | Status | Symbol | Exercising test or owner |
| --- | --- | --- | --- |
| Slip-free ratio coupling between two shafts, with load reaction | present | `engine/gpu/WorkshopPhysicsCompiler.cs:583`–`:599` | `MechanicalCoordinateBindingsTests.PositiveTransmission_DrivingCoordinateTransmitsProportionalAngularMotionToDrivenCoordinate` (`:499`); `ReverseTransmissionLoadTests.ADeclaredOutputLoadReactsThroughReverseToTheSource` (`:19`) |
| A held driven shaft holds the source, which then does no work | present | `RevoluteDriveDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:138` | `MotorTests.AStalledMotorDoesNoWorkOnceItsShaftIsHeld` (`:732`) |
| Typed refusal of a wrong mechanical connection, atomically, at edit and at load | present (for role, domain and occupancy) | `engine/gpu/WorkshopConnections.cs:170`–`:183` | `MechanicalCoordinateBindingsTests.WorkshopConnections_ValidateControls_RefuseInvalidMechanicalConnections` (`:456`); `ClutchGeneralityTests.OccupiedMechanicalInputIsRefusedWhenLoadingSavedConstruction` (`:632`) |
| Closed route presentation between rims | present | `MechanicalRimDescriptor`, `CuriousContraptions.Abstractions/MechanicalLinkPresentationValues.cs:10` | `MechanicalLinkPresentationTests.DeclaredRimsProduceClosedTangentRoutesAndCommittedMarkers` (`:29`) |
| A chosen material on the mechanical connection, beside the unchanged default rigid link, through authoring, save, wire and presentation | missing | — | ENGINE-MECHANICAL-LINKS (owner decision 3a; sprint key `engine-mechanical-link-kinds`, shared with EL-200) |
| An engagement type declared on a mechanical port, and refusal of a kind the port does not admit | missing | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) has no such field | same slice |
| Link-by-link route artwork and rim teeth selected by kind | missing | the route is one style today | same story |
| A solid strand: the visible loop's collider is a property of its declared material | missing | the link route is presentation only | ENGINE-CONNECTION-MATERIALS (solid strand), owner decisions 1 and 2 |
| A material whose speed ratio is the pitch-radius ratio of its two ports (declared data) | missing | the ratio is always the coordinate ratio (`engine/gpu/WorkshopPhysicsCompiler.cs:583`–`:599`) | ENGINE-MECHANICAL-LINKS (coordinator decision 24l) |

Counts: 4 present, 0 present-untested, 0 narrower, 5 missing. ENGINE-MECHANICAL-LINKS owns four missing items (the chosen material, the port engagement type, the link artwork and the pitch-radius ratio) and ENGINE-CONNECTION-MATERIALS (solid strand) the other; both stories have their text in `epics.md`.

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

**Harvest-era lists (Exists now, Missing).** Superseded by the capability probe above; the harvest text is in git history.

**Dependencies.** EL-200 for the incompatibility control and K1's belt control.

## 4. Sources and legacy

- Requirement: "Compatible sprockets exchange torque through typed toothed engagement"; outcome "Incompatible attachment fails rather than inheriting belt behaviour" ([element-201](../requirements.md#element-201)). Campaign: "chain", first use 11–20 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Rigid, always-engaged ratio coupling between two axial guides; ratio finite and nonzero; distinct guides | `engine/SceneTransmissionJoint.cs@a6c914e:L17-L26` | carry forward | The slip-free toothed law. |
| 2 | One authored link per input; matching mechanical sockets only | `engine/MechanicalNetwork.cs@a6c914e:L28-L38` | carry forward | Topology. |
| 3 | Accepted: wrong socket and wrong domain are refused | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L211` | carry forward | Typed refusal, extended to Chain versus Belt. |
| 4 | Historical status note lists "chain-drive variants" among future work alongside moving-block ratios and rope cutting | `reference/P0-022-before/source-README.md@a6c914e:L99-L99` | carry forward (as gap) | Confirms no legacy chain drive. |

**Files harvested:**
- `engine/SceneTransmissionJoint.cs`
- `engine/MechanicalNetwork.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `parts/ConveyorPart.cs` (port pulley scale)
- `reference/P0-022-before/source-README.md` (future-work note)
- Searched with no hit for `sprocket` or a chain drive (`chain` hits are domino-chain level text in `content/puzzles.json`): `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-201](../requirements.md#element-201) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe (pending, post-catalogue).** Place battery, motor and a Clutch; connect `Supply` → `PowerIn`; draw a mechanical connection from motor `Drive` to clutch `DriveIn` and choose the Chain material; then try the same from the motor to a Conveyor and see it refused. Run.
- **Outcomes, controls and boundaries** are rows K1–K11 below. Save/Load round-trips the endpoints and the material id `drive_chain` (K7).
- **Integrations.** Campaign row 2 ("... chain ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); mechanical-port incompatibility with EL-200 under [S257](../invest/decisions.md#s257); drives CAT-019 conveyor from CAT-042 motor.

### Acceptance rows (21 November 2026)

Native tests, class `DriveChainTests`. Heavy loads use a declared rotor package as described in [EL-200](EL-200-drive-belt.md#acceptance-rows-21-november-2026), here with a toothed port and pitch radius 0.22 m; K1 declares it at 10 kg·m² (the reason is in K1). "No slip" means the two shafts' committed travels, scaled by the ratio, differ by less than 0.02 rad over the whole run: that is solver residue, while a slipping belt in the same rig differs by more than 0.5 rad. Guards are game seconds. The earlier outline's "conveyor at half the motor speed" row is withdrawn with the tooth-count parameters, and the Conveyor is now the refused case.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| K1 | Given Battery (`power_limit` 480 W, its maximum) → Motor (`torque` 100 N·m, `speed` 4 rad/s; both inside the Motor's 0.1–100 N·m and 0–20 rad/s ranges) → Chain → declared toothed rotor of 10 kg·m², when the Motor spins up (guard 10 s), then the Motor reaches 4 rad/s and the rotor 4 × 0.30 ÷ 0.22 = 5.45 rad/s with no slip (pitch-radius ratio 15/11, coordinator decision 24l). By hand: the rotor reflected to the Motor shaft is 10 × (15/11)² = 18.60 kg·m², plus the Motor's 0.0225, so 100 N·m gives 5.37 rad/s² and 0.745 s to 4 rad/s; the peak demand is 100 × 4 = 400 W at the Motor shaft, under the 480 W allowance (4 J per tick), so the over-allowance limit of coordinator decision 27 never acts | `AChainTurnsBothShaftsTogetherUnderALoadThatSlipsABelt` | the same rig with a Belt at `tension` 200, `friction` 0.8 (which keeps the coordinate ratio, 1 here, EL-200) slips by more than 0.5 rad. By hand the belt passes 340 N × 0.22 m = 74.8 N·m, so the rotor accelerates at 7.48 rad/s² and takes 4 / 7.48 = 0.535 s to reach 4 rad/s while the Motor shaft (0.0225 kg·m²) is at 4 rad/s within a few ticks; the slip is 4 × 0.535 − ½ × 7.48 × 0.535² = 2.14 − 1.07 = 1.07 rad, and the drive asks at most 74.8 × 4 = 299 W. A chain that slipped like a belt, or a belt that never slipped, fails | the strongest belt. At 1 kg·m² the same belt would slip only 4 × 0.0535 − ½ × 74.8 × 0.0535² = 0.11 rad, under the control's 0.5 rad; hence the 10 kg·m² rotor |
| K2 | Source outcome. Given a Chain drawn from Motor `Drive` to Conveyor `DriveIn` (smooth only), then the edit is refused with the typed engagement reason and the construction is unchanged; the same connection in a saved construction is refused at load | `AChainToASmoothOnlyPortIsRefusedAndChangesNothing` | a Belt between the same ports is accepted | — |
| K3 | Source outcome ("rather than inheriting belt behaviour"). Given a Chain record that carries `tension` or `friction`, then it is refused; and a Chain between toothed ports under a load above any belt capacity does not slip (K1) | `AChainNeverTakesBeltValues` | K1 | — |
| K4 | Given the driven rotor held by a Wall-blocked arm, when the Motor is supplied, then both shafts stay within 0.05 rad/s of rest and the Motor's committed drive work stops rising | `AStalledChainStallsTheMotorWithoutSlip` | K1 | stall |
| K5 | Given a Reverse transmission in the train (toothed both sides), then the rotor turns the other way with no slip | `AChainThroughAReverserTurnsTheOtherWay` | K1 | sign |
| K6 | Given a removed chain or an unpowered Motor, then the rotor stays at rest | existing `ReverseTransmissionTests.AbsentDriveLeavesBothWheelsAtRest` (`:154`), plus one Chain row in `DriveChainTests.AnAbsentOrUnpoweredChainDrivesNothing` | — | — |
| K7 | Reset restores both shafts and the link marks; a saved construction loads with the material id `drive_chain` and both endpoints | `ResetAndLoadRestoreTheChain` | — | — |
| K8 | A Belt drawn between two toothed-and-smooth ports is still accepted, and each delivered port reports the engagement in the table above | `EveryMechanicalPortDeclaresItsEngagement` | — | — |
| K9 | The chain route is closed, tangent to both rims and its marks advance with the committed rotation; each rim shows a whole number of teeth and the drawn link speed (pitch radius × committed rotation) is the same at both rims | `AChainIsDrawnAsLinksAroundBothRims` | the belt style for the material `drive_belt` | unequal rims (Motor 0.30 m, Clutch 0.28 m) |
| K10 | Coordinator decision 24l and owner decision 2. Given Motor `Drive` (pitch radius 0.30 m) → Chain → Clutch `DriveIn` (0.28 m), then the Clutch input turns at 0.30 ÷ 0.28 = 15/14 ≈ 1.071 × the Motor's speed with no slip; the same pair joined by the default rigid link keeps the ports' coordinate ratio and is unchanged | `AChainTurnsAtItsPortsPitchRadiusRatio` | the default rigid link on the same pair; and a Chain between two ports of equal pitch radius (Motor `Drive` → a declared toothed rotor of 0.30 m) turns at ratio 1 | equal against unequal radii |
| K11 | The loop is solid (owner decisions 1 and 2). Given a horizontal Chain between two toothed shafts 1.5 m apart, when a Tennis ball is dropped 1 m onto the upper run, then over 3 s its centre, while within the strand's horizontal footprint, is never more than 0.01 m below the loop's top surface (it rests on the loop or rolls off its side) | `ABallDroppedOnAChainDoesNotPassThroughIt` | the same ball dropped 0.5 m beside the loop falls past it | 0.01 m: a ball passing through a drawn loop is the visible-solid failure |

There is no parameter of its own, so there is no parameter refusal row beyond K3.

### Integration (21 November 2026)

- Joins the toothed ports listed in section 2: Motor, Reverse transmission, Clutch, Wound spring (delivered); Flywheel and cams (planned). Refused at Conveyor, Windmill, Fan and Governor.
- Mechanical-port rule [S257](../invest/decisions.md#s257) ("chain and belt are not interchangeable") is satisfied by K2 and K3.
- Campaign: row 2 ("chain"), first use levels 11–20, load variants to 41–50, reuse 31–50, 61–80, 136–150. The natural lesson is winding a Wound spring: a belt slips, a chain does not.
- Coverage binding (planned, not yet live): none while the material has no package. If ENGINE-MECHANICAL-LINKS makes `drive_chain` a non-placeable material entry, the binding takes EL-067's planned form and waits for S7, which widens the reader (owner decision 15). S8 is needed only for a level that authors a chain as a locked fixture.

### Delivery (21 November 2026)

- **Slice shape.** `named-el-201`, after ENGINE-MECHANICAL-LINKS, ENGINE-CONNECTION-MATERIALS (solid strand) and `named-el-200` (K1's control needs the belt's slip).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-ISLANDS-SLEEP; then ENGINE-MECHANICAL-LINKS; then ENGINE-CONNECTION-MATERIALS (solid strand); then EL-200; then S7 (the coverage follow-on, which widens the reader, owner decision 15) only if the material is a package. No structural story.
- **Files.** `CuriousContraptions.tests/DriveChainTests.cs`; one engagement value on each mechanical port of `elements/Motor`, `elements/ReverseTransmission`, `elements/Clutch`, `elements/WoundSpring`, `elements/Conveyor`, `elements/Windmill` and `elements/Fan` (declaration data only); the EL-201 ledger entry; this spec. No new package.
- **Review tier.** Data-only if the engine slice left every capability present with a test; engine tier otherwise.
- **Disqualifies the plan:** the engine slice ships the kind without a port engagement type (then K2 has nothing to refuse and the slice is incomplete); the port table above makes a planned level unsolvable (then the table is revised at planning, by game intent, without adding a second rule).

## 6. Open questions

None open. Resolved 21 November 2026:

1. Which parts carry toothed ports is decided in section 2; a port may be both smooth and toothed. Decided under the standing delegation so that the required refusal has a delivered example (the Conveyor).
2. One fixed pitch, 0.08 m, used for artwork only. D19 (the spec's proposal).
3. The `teeth_in` and `teeth_out` connection parameters are dropped. D19: no requirement or campaign row sets a chain ratio by hand, and ratio by tooth count belongs to EL-232 Gears.
4. The Fan's `DriveIn` is smooth only (22 November 2026, audit H8): it was missing from the table, and it is the lightest pulley.
5. Owner decision 8 ("gear ratios follow tooth counts") applies to gears (EL-232, CAT-057), where it was raised. The chain's speed ratio is the pitch-radius ratio of its two ports, declared as data on the material (coordinator decision 24l, 23 November 2026), which closes the earlier flag that drawn teeth did not match the turning on unequal rims; rows K1, K9 and K10 are re-derived for it.
6. Delivered connections stay rigid and the chain is a new material the player chooses (owner decision 2); the toothed-port table is kept as declaration data and no longer depends on a change to delivered links.
7. The visible loop is solid (owner decisions 1 and 2), owned by ENGINE-CONNECTION-MATERIALS (solid strand); row K11 proves it.

**Needs the owner.** None: both earlier items are decided (owner decisions 1 and 2; items 6 and 7).

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md` D3; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** The chain is a declared connection material and one engagement value on each mechanical port. The engine holds no chain branch and no product enum (rewritten with EL-200 on 22 November 2026).
2. **Generic laws, stated element-free.** The delivered bilateral ratio coupling, selected by the material's declared law kind, with its ratio taken from the source the material declares (here the ports' pitch-radius ratio, coordinator decision 24l); and a general compatibility rule, missing today: ports declare engagement classes, a connection material declares the class it requires, and a mismatch is refused with a typed reason. A connection record that carries values its material does not declare is refused by the same general rule. One owner, ENGINE-MECHANICAL-LINKS (owner decision 3a).
3. **Three layers.** Colliders: the solid loop generated from the material's declared thickness and segment rule (ENGINE-CONNECTION-MATERIALS, solid strand; owner decisions 1 and 2); no package collider. Visuals: links along the closed route and teeth on toothed rims, drawn by the link presentation at the declared thickness and never used as a collider. Animation: the links advance from committed travel; nothing written back.
4. **Flow and typing.** Typed material id; engagement classes are a closed enum; no values.
5. **Flat state.** Per chain connection: one row in the existing transmission table and the loop's strand colliders fixed by the segment rule at compile. No body, sensor or store.
6. **Performance.** The refusal is an authoring-time check; at Run a chain is one bilateral row, as every mechanical connection today, and its loop colliders are bounded by compiled declarations and found through ENGINE-SPATIAL-QUERIES.
7. **Tests are scenarios.** Rows K1 to K11 run the production simulation and the authoring interface.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), then on ENGINE-MECHANICAL-LINKS and ENGINE-CONNECTION-MATERIALS (solid strand), both stated element-free. No owner item remains.**
