# EL-200 · Drive belt named-identity spec

**Current delivery (24 October 2026):** [EL-200 delivery slice](../invest/named-elements.md#element-200). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-200 |
| Name | Drive belt |
| Type | Mechanical |
| Anchor | [requirements.md#element-200](../requirements.md#element-200); [named entry](../invest/named-elements.md#element-200); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S310 |
| Refines / extends | No CAT spec of its own; the belt is a connection, not a catalogue part. [CAT-019 conveyor](CAT-019-conveyor.md) and [CAT-057 reverse transmission](CAT-057-reverse_transmission.md) list it as a candidate mapping for the legacy mechanical link |
| Related | EL-201 Drive chain (must not be interchangeable), EL-199 Electric motor, EL-202 Conveyor, EL-203 Reverse transmission |
| Roadmap story | [EL-200 delivery slice](../invest/named-elements.md#element-200); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (the solid belt loop as a query candidate) and ENGINE-ISLANDS-SLEEP; then ENGINE-MECHANICAL-LINKS (the declared material, force-bounded coupling, pitch radius and engagement class) and ENGINE-CONNECTION-MATERIALS (solid strand: the visible loop is thick, owner decisions 1 and 2); then S7 if the material is a package |

## 2. Declaration

### Disposition and current declaration (21 November 2026)

**Disposition (owner decision 2, 22 November 2026).** A **new** declared connection material that the player chooses when drawing a mechanical connection, not a placed part: no body and no palette order (not placeable: a connection material; coordinator decision 36). **The delivered rigid mechanical connection, named "Mechanical link" (coordinator decision 36), is unchanged and remains the default**: no delivered connection becomes a slipping belt, and every delivered part, test and level keeps its rigid coupling. Whether the belt's declaration lives in a package of its own, and so needs a package source binding, is fixed by ENGINE-MECHANICAL-LINKS together with Batch I's `steel_cable`. It is **not** a Duplicate closure, because the requirement row adds behaviour the rigid connection does not have (slip when tension is missing).

**What is delivered today** (checked on `8d1d68aa`). A `Drive` → `DriveIn` connection, the Mechanical link, already exists and is already drawn as a closed loop (its schematic drawing; "belt" names only this new material, coordinator decision 36):

- It compiles to a rigid `TransmissionConstraintKind.Bilateral` ratio coupling between the two hinges, ratio = output `CoordinatePerRadian` ÷ input `CoordinatePerRadian` (`engine/gpu/WorkshopPhysicsCompiler.cs:583`–`:599`). It cannot slip.
- It is drawn as a closed tangent route around the two ports' rims with travelling markers (`engine/gpu/MechanicalLinkCompiler.cs:9`; `CuriousContraptions.Abstractions/MechanicalLinkPresentationValues.cs:10`, `:30`).
- One connection per output coordinate and one per input coordinate (`engine/gpu/WorkshopConnections.cs:182`). The legacy "an output may fan out" does not hold.
- A mechanical connection carries no values of its own; only a rope connection does (`RopeLength`, `WorkshopConnections.cs:166`).

So the delta EL-200 adds is exactly: a material the player may choose instead of the default rigid link, carrying a declared tension and friction, a transmitted force bounded by them, slip when the load exceeds it, and a solid visible loop.

**Declared values on a Belt connection** (starting values, D1):

| Name | Type | Unit | Default | Minimum | Maximum | Edited | Policy |
| --- | --- | --- | --- | --- | --- | --- | --- |
| material | typed id of a declared connection material; this identity is the material `drive_belt` | — | the delivered rigid link (no belt); `drive_belt` only when the player chooses it | — | — | chosen when the connection is drawn; Build only | an unknown id is refused |
| `tension` | Real f32 | N | 160 (the worked value for this new material only, owner decision 2) | 0 | 200 | Build only | out-of-range and non-finite refused |
| `friction` | Real f32 | coefficient | 0.4 | 0.1 | 0.8 | Build only | out-of-range and non-finite refused |

**Ports.** None of its own. It joins a `PortRole.Drive` output to a `PortRole.DriveIn` input, both Mechanical, as today. Each delivered port declares a physical pitch radius equal to its rim radius (below); physics never reads the cosmetic rim, which stays presentation only ("Physical transmission does not require cosmetics", `MechanicalLinkPresentationValues.cs:9`).

**Pitch radii (repair of 22 November 2026, audit H8).** The pitch radius of each delivered port is its declared rim radius (`MechanicalRimDescriptor.Radius`): Fan `DriveIn` 0.14 m (`elements/Fan/FanElementDeclaration.cs:64-67`), Conveyor 0.22, Windmill 0.24, Wound spring 0.26, Clutch 0.28, Motor 0.30, Reverse transmission 0.32. Capacity torque = F_max × the smaller of the two pitch radii. The speed ratio stays the ports' `CoordinatePerRadian` ratio and is not derived from pitch radii.

**Default link (owner decision 2).** How ENGINE-MECHANICAL-LINKS represents the default rigid link (as the absence of a chosen material or as a declared default) is that story's choice, on one condition: no delivered connection, save, test or level changes behaviour, and the rigid coupling stays the one law for a link that is not a chosen belt or chain.

**Carrier (rewritten 22 November 2026 under the owner's engine-conformance addendum).** The first draft put a closed enum `MechanicalLinkKind { Belt, Chain }` with default values on the engine's connection. That names products in the engine and gives it product defaults, which the element-free engine gate and design guide D3 do not allow, so it is withdrawn. The belt is a **declared connection material**: declaration data that names a general coupling law, the values that law takes with their ranges and defaults, the engagement class it requires of both ports, and a link presentation style. This is the mechanism Batch I specifies for `steel_cable` (a material declared with no body), so the two batches now use one mechanism (audit C3). The engine knows the law and the classes; it knows no material by name.

**Bodies, stores, sensors.** None. The belt is massless.

### Art and solidity (21 November 2026)

The belt is drawn by the link presentation: a closed loop around both rims with two travelling marks, navy `#293954` with cream marks, distinguished from a chain by a smooth, unbroken loop (the chain is drawn as separate links, EL-201). During slip the marks follow the belt's own travel, which ENGINE-MECHANICAL-LINKS provides as a committed value. **The loop is solid.** A visible belt loop is thick and therefore solid (owner decisions 1 and 2): its collider is the material's declared strand (thickness 0.03 m, **proposed**, equal to the drawn loop; segment rule of ENGINE-CONNECTION-MATERIALS), compiled from declarations, with no per-element code. Blocked on ENGINE-CONNECTION-MATERIALS (solid strand). The delivered rigid link keeps its schematic drawing and stays non-solid, like a thin wire (coordinator decision 25, 23 November 2026, under owner decision 2; `MechanicalLinkPresentationTests.RemovingCosmeticsKeepsTheSamePhysicalReversal`, `:81`, shows that drawing is cosmetic); only the belt and chain materials are solid.

### Behaviour and numbers (21 November 2026)

- Capacity (capstan, wrap angle π at both pulleys): F_max = 2·T·(e^{μπ} − 1)/(e^{μπ} + 1), in newtons at the belt. The torque that can pass at a pulley is F_max × its pitch radius, so the smaller pulley slips first.
- While the load is below capacity the two rims move together exactly as today. Above it, the belt passes F_max and no more; the driven shaft lags and the difference is lost as heat (a loss entry in the finite ledger, never a gain).
- `tension` 0 passes nothing: the source outcome "missing tension … prevent[s] full drive transfer".

| `tension` (N) | `friction` | F_max (N) | Torque at 0.14 m (Fan) | at 0.22 m (Conveyor) | at 0.30 m (Motor) |
| --- | --- | --- | --- | --- | --- |
| 0 | any | 0 | 0 | 0 | 0 |
| 20 | 0.4 | 22.3 | 3.1 N·m | 4.9 N·m | 6.7 N·m |
| 90 | 0.4 | 100.2 | 14.0 N·m | 22.1 N·m | 30.1 N·m |
| 130 | 0.4 | 144.8 | 20.3 N·m | 31.9 N·m | 43.4 N·m |
| 160 (default) | 0.4 (default) | 178.2 | 24.9 N·m | 39.2 N·m | 53.5 N·m |
| 200 | 0.8 | 340 | 47.6 N·m | 74.8 N·m | 102 N·m |

F_max = 1.1138·T at μ = 0.4 (e^{0.4π} = 3.514) and 1.7003·T at μ = 0.8.

**Why 160 N for the new material (hand calculation).** Delivered connections stay rigid (owner decision 2), so no delivered test or level depends on this value; it is chosen so that a belt a player draws between delivered parts carries their ordinary loads. 90 N passes only 14.0 N·m at the Fan's 0.14 m port, below the default Motor's 20 N·m (`elements/Motor/MotorElementDeclaration.cs:44`), and 22.1 N·m at the Conveyor, below a 30 N·m load such as `ConveyorTests.SaveLoadRoundtrip_PreservesParametersAndMechanicalConnections` (`:453`) uses. 130 N clears the Fan by 1.4 % only (20.3 against 20) and does not carry the 38 N·m the audit reports for a Wound spring at maximum settings (37.6 N·m at 0.26 m). 160 N passes 24.9 N·m at the Fan (25 % over), 39.2 at the Conveyor, 46.3 at the Wound spring. No belt setting carries the Motor's 100 N·m maximum at a 0.22 m pulley (340 N × 0.22 = 74.8 N·m); that is the reason the chain exists. No signal read is involved, so no tick-order rule applies.

### Harvest-era subsections (Bodies and shapes to Variants)

Superseded by the current declaration (section 2: a declared connection material, pitch radius = rim radius on each port, no fan-out, no sheave of its own); the harvest text is in git history.

## 3. Engine capabilities

### Capability probe (21 November 2026)

The capability probe below is the whole engine probe; the baseline `a6c914e` lists are reduced to a stub at the end of this section.

| Capability | Status | Symbol | Exercising test or owner |
| --- | --- | --- | --- |
| Mechanical connection as a ratio coupling with load reaction | present | `engine/gpu/WorkshopPhysicsCompiler.cs:583`–`:599` | `ConveyorTests.ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` (`:417`); `ReverseTransmissionLoadTests.ADeclaredOutputLoadReactsThroughReverseToTheSource` (`:19`) |
| A disconnected or unpowered train transfers nothing | present | same | `ReverseTransmissionTests.AbsentDriveLeavesBothWheelsAtRest` (`:154`) |
| Mechanical link route and markers between two rims | present | `MechanicalRimDescriptor`, `MechanicalLinkPresentationValues.cs:10` | `MechanicalLinkPresentationTests.DeclaredRimsProduceClosedTangentRoutesAndCommittedMarkers` (`:29`) |
| Typed values carried by a connection | narrower | only `RopeLength` on rope connections, `engine/gpu/WorkshopConnections.cs:166` | a mechanical connection has no fields; owner `ENGINE-CONNECTION-MATERIALS` (typed values on a connection; engine-stories-index section 2) |
| A chosen material on the mechanical connection, beside the unchanged default rigid link, through authoring, save, wire and presentation | missing | — | ENGINE-MECHANICAL-LINKS |
| A coupling whose transmitted force is bounded and which slips beyond the bound | missing | `TransmissionConstraintKind { Bilateral, NonPositiveVelocity }` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:255`) has no bounded kind; the Clutch's `CloseSeconds` is engagement over time, not a force bound | ENGINE-MECHANICAL-LINKS |
| A physical pitch radius declared on a mechanical port | missing | the rim radius is cosmetic | ENGINE-MECHANICAL-LINKS |
| Slip recorded as a ledger loss and readable as the belt's own travel | missing | — | ENGINE-MECHANICAL-LINKS |
| A solid strand: the visible loop's collider is a property of its declared material | missing | the link route is presentation only | ENGINE-CONNECTION-MATERIALS (solid strand), owner decisions 1 and 2 |

Counts: 3 present, 0 present-untested, 1 narrower, 5 missing. The first four non-present items are ENGINE-MECHANICAL-LINKS (owner decision 3a; sprint key `engine-mechanical-link-kinds`), shared with EL-201; it needs none of the player-built joints of ENGINE-ASSEMBLY-JOINTS. The solid loop is ENGINE-CONNECTION-MATERIALS (solid strand), the one strand-collider capability for ropes, cables, belts and chains.

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Harvest-era lists (Exists now, Missing).** Superseded by the capability probe above; the harvest text is in git history.

**Dependencies.** CAT-042 motor and CAT-019 conveyor (delivered) as the first drive pair.

## 4. Sources and legacy

- Requirement: "Connected pulleys exchange torque through declared friction and tension"; outcome "Missing tension or disconnected endpoints prevent full drive transfer" ([element-200](../requirements.md#element-200)). Campaign: "belt", first use 11–20 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | A mechanical input accepts one authored belt; links resolve only between matching mechanical sockets | `engine/MechanicalNetwork.cs@a6c914e:L28-L38` | carry forward | Typed topology. |
| 2 | Each link compiles to an always-engaged rigid ratio joint | `engine/MechanicalNetwork.cs@a6c914e:L46-L49`; `engine/SceneTransmissionJoint.cs@a6c914e:L17-L26` | do not carry forward (rigid, slip-free coupling) | The EL row requires friction and tension; a rigid ratio cannot slip. |
| 3 | Accepted: wrong socket or wrong domain refused; a source-free loop refused; a second driver into one input refused; explicit fan-out allowed | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L211` | carry forward | Admission rules. |
| 4 | Belt artwork: four lines and two marks following socket transforms; marks move only while running | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L17-L60` | carry forward | Presentation contract (the `MechanicalBeltVisual` source was already deleted before `a6c914e`). |

**Files harvested:**
- `engine/MechanicalNetwork.cs`
- `engine/SceneTransmissionJoint.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `parts/ConveyorPart.cs` (port pulley geometry and art)
- `parts/catalog/motor.tres` (default torque reference)
- `reference/cpu/MachineWorld.cs` (connect-time length rule)
- Searched with no hit for a friction- or tension-limited belt (`belt` hits are the rigid link above, conveyor tests and level text): `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-200](../requirements.md#element-200) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe (pending, post-catalogue).** Place battery, motor and conveyor; connect `Supply` → `PowerIn`; draw a mechanical connection from motor `Drive` to conveyor `DriveIn` and choose the Belt material; set `tension`. Run with a ball on the conveyor.
- **Outcomes, controls and boundaries** are rows B1–B13 below; a second belt on an occupied coordinate is refused (B10). Save/Load round-trips the endpoints, the material id `drive_belt`, `tension` and `friction` (B8).
- **Integrations.** Retained belt behaviour [todo-151](../requirements.md#todo-151) (signed, load-aware motion through conveyor input/output and the −1 reverser); campaign row 2 ("... belt ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); couples CAT-042, CAT-019 and CAT-057; contrast with EL-201 chain.

### Acceptance rows (21 November 2026)

Native tests, class `DriveBeltTests`. Where a heavy load is needed the test declares an unfamiliar rotor package of inertia 1 kg·m² and pitch radius 0.22 m through the scoped declaration seam the transmission tests already use, so no undelivered part is required. Speeds are compared with ±0.1 rad/s (the coupled-speed allowance of `MechanicalCoordinateBindingsTests`); accelerations with ±20 %, because a slip bound that is wrong by a pulley radius or by the capstan factor is off by 35 % or more. Guards are game seconds.

**Supply power (coordinator decision 27, 23 November 2026).** Rows B1–B5 use a Battery at its default `power_limit` 120 W and the Motor at its default 20 N·m and 6 rad/s. The Motor's velocity drive asks for at most 20 N·m × 6 rad/s = 120 W, which is 1 J per tick at 120 Hz: exactly the default allowance, reached only at full torque and full speed together. So the over-allowance limit never reduces the Motor's torque in these rows, and no row states a raised `power_limit` (a 480 W Battery would change nothing and hide that fact). The Battery's electrical node `MaxPower` 100 W (`elements/Battery/BatteryElementDeclaration.cs:76`) is only checked to be finite and caps nothing.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| B1 | Given Battery → Motor → default Belt → Conveyor with a Tennis ball, when the Motor reaches its speed (guard 10 s), then roller and rotor speeds agree for 5 s and the ball is carried at least 1 m | `ADefaultBeltTurnsTheConveyorWithTheMotor` | B2 | the default Motor (20 N·m) through a default Belt into a Fan in Shaft mode (0.14 m, capacity 24.9 N·m): relative travel stays under 0.05 rad |
| B2 | Source outcome. Given the same construction with `tension` 0, then the roller turns less than 0.05 rad in 5 s while the Motor reaches its speed | `ABeltWithNoTensionTransmitsNothing` | B1 | parameter minimum |
| B3 | Source outcome. Given Motor (20 N·m) → Belt with `tension` 20 → declared 1 kg·m² rotor, then the rotor accelerates at 4.9 rad/s² ± 20 %, lags the Motor by more than 1 rad/s while it does, and ends at the Motor's speed (guard 15 s) | `ASlackBeltSlipsAndPassesOnlyItsCapacity` | the default belt accelerates the same rotor at more than 15 rad/s² | — |
| B4 | Given `tension` 70 (capacity 17.2 N·m at 0.22 m) and 110 (27.0 N·m) against the 20 N·m Motor, then the first slips during spin-up (relative travel above 0.5 rad) and the second does not (below 0.05 rad) | `ABeltSlipsOnlyAboveItsCapacity` | — | either side of the capacity |
| B5 | Given `friction` 0.1 and 0.8 at `tension` 90, then the rotor's acceleration is 6.2 rad/s² ± 20 % (28.0 N × 0.22 m) and at least 15 rad/s² (motor-limited) | `LowerFrictionLowersTheCapacity` | — | parameter minimum and maximum |
| B6 | In B3, the rotor's kinetic energy never exceeds the Motor's committed drive work, and the ledger loss is positive only while slipping | `SlipLosesEnergyAndNeverCreatesIt` | B1 has zero loss | — |
| B7 | Given a belt with no connected far end, or a removed belt, then the driven part stays at rest | existing `ReverseTransmissionTests.AbsentDriveLeavesBothWheelsAtRest` (`:154`) | — | — |
| B8 | Reset restores both shafts and the marks; a saved construction loads with the connection's material id, `tension` and `friction` | `ResetAndLoadRestoreTheBeltAndItsValues` | — | 0 and 200 N round-trip |
| B9 | `tension` −0.01, 200.01, NaN, +∞ and `friction` 0.09, 0.81, NaN are refused at the editor, input and save decode with nothing changed; an unknown material id is refused | `AnInvalidBeltValueIsRefusedAtEverySeam` | bounds admitted | — |
| B10 | A belt drawn to an electrical or activation socket, or a second belt on an occupied coordinate, is refused | existing `MechanicalCoordinateBindingsTests.WorkshopConnections_ValidateControls_RefuseInvalidMechanicalConnections` (`:456`), `AliasesCannotReuseEitherCoordinateRole` (`:874`) | — | — |
| B11 | Through a Reverse transmission the driven sense is reversed and B1 still holds | existing `ReverseTransmissionTests.ReverseTransportsCargoOppositeTheZeroReverserBypass` (`:134`), rerun unchanged | — | sign |
| B12 | Owner decision 2. Given the construction of B3 with the default rigid link instead of a Belt, then the rotor follows the Motor with no slip (relative travel below 0.02 rad); every delivered mechanical test and levels 16–18 keep their outcomes unchanged | `TheDefaultLinkStaysRigidBesideTheBelt` | B3 | the delivered suites rerun unchanged |
| B13 | The loop is solid (owner decisions 1 and 2). Given a horizontal Belt between two shafts 1.5 m apart, when a Tennis ball is dropped 1 m onto the upper run, then over 3 s its centre, while within the strand's horizontal footprint, is never more than 0.01 m below the loop's top surface (it rests on the loop or rolls off its side) | `ABallDroppedOnABeltDoesNotPassThroughIt` | the same ball dropped 0.5 m beside the loop falls past it | 0.01 m: a ball passing through a drawn loop is the visible-solid failure |

The retained belt behaviour [todo-151](../requirements.md#todo-151) (signed, load-aware motion through the Conveyor and the −1 reverser) is already executed by the delivered tests cited in B7, B10 and B11 and must stay green.

### Integration (21 November 2026)

- Joins every delivered part with a mechanical port: Motor, Conveyor, Reverse transmission, Clutch, Windmill, Wound spring and the Fan's `DriveIn`; and the planned Flywheel, Governor and cams.
- Campaign: row 2 ("belt"), first use levels 11–20, load variants to 41–50, reuse 31–50, 61–80, 136–150. Planned levels 16 `conveyor_courier`, 17 `belt_relay` and 18 `reverse_belt` use the delivered rigid connection, which is unchanged (owner decision 2); they are in the Early level proof "Drive" group. A level that teaches slip asks the player to choose the Belt.
- Coverage binding (planned, not yet live): none while the material has no package. If ENGINE-MECHANICAL-LINKS makes `drive_belt` a non-placeable material entry, the binding takes EL-067's planned form and waits for S7, which widens the reader (owner decision 15).
- The lesson-fixture story (S8) is needed only if a level authors a belt with non-default values as a locked fixture.

### Delivery (21 November 2026)

- **Slice shape.** Blocked on the engine programme (owner decision 3a). Then ENGINE-MECHANICAL-LINKS (sprint key `engine-mechanical-link-kinds`: chosen connection material and values, bounded coupling, port pitch radius, engagement class, slip read; wire schema, TypeScript callers and presentation read model included; at least two differently parameterised qualification fixtures) and ENGINE-CONNECTION-MATERIALS (solid strand). Then `named-el-200`: the acceptance tests above and the ledger row. EL-201 follows as its own slice on the same stories.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-ISLANDS-SLEEP; then ENGINE-MECHANICAL-LINKS; then ENGINE-CONNECTION-MATERIALS (solid strand); then S7 (the coverage follow-on, which widens the reader, owner decision 15) only if the material is a package.
- **Forward-only and owner decision 2.** The delivered rigid link is not converted, migrated or given belt values: it stays the default and its saves load as today. The belt is a second declared material with a different law (force-bounded), chosen by the player, not a second path for the rigid link's behaviour.
- **Files (identity slice).** `CuriousContraptions.tests/DriveBeltTests.cs`, the EL-200 ledger entry, this spec. No element package and no registration. The physical pitch radius on the seven delivered mechanical ports is declaration data added by ENGINE-MECHANICAL-LINKS: `elements/Fan/FanElementDeclaration.cs`, `elements/Conveyor/ConveyorElementDeclaration.cs`, `elements/Windmill/WindmillElementDeclaration.cs`, `elements/WoundSpring/WoundSpringElementDeclaration.cs`, `elements/Clutch/ClutchElementDeclaration.cs`, `elements/Motor/MotorElementDeclaration.cs` and `elements/ReverseTransmission/ReverseTransmissionElementDeclaration.cs`.
- **Review tier.** Engine slice: engine tier. Identity slice: data-only if every capability is then present with a test; otherwise engine tier.
- **Disqualifies the plan** (re-checked by the engine slice's probe): any delivered mechanical test, save or level changes outcome when the belt lands (owner decision 2 forbids it); a bounded row that cannot share the solver's existing transmission table without a second code path.

## 6. Open questions

None open. Resolved 21 November 2026:

1. EL-200 is a new connection material the player chooses; it is not the identity of the delivered mechanical connection. Owner decision 2 (22 November 2026): no delivered connection becomes a slipping belt, the existing rigid connection is unchanged and remains the default, and drive belt and drive chain are new declared materials. The earlier disposition, which converted every delivered connection to a belt, is withdrawn.
2. Belts are unlimited connections, like wires and ropes today. D19.
3. The capstan law with a fixed wrap angle of π is kept. D19 (the spec's proposal).
4. Default `tension` 160 N for this material only (owner decision 2 names 160 N as the worked value) and pitch radius = declared rim radius (audit H8). Reason: the hand table in section 2.
5. The visible loop is solid. Owner decisions 1 and 2: a belt loop is thick and therefore solid; its collider is a property of the declared material, owned by ENGINE-CONNECTION-MATERIALS (solid strand). Row B13 proves it.

**Needs the owner.**

None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md` D3; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** The belt is a declared connection material, chosen by the player, and two values on a connection. The engine holds no belt branch, product enum or product default (section 2, Carrier); the default rigid link is unchanged (owner decision 2).
2. **Generic laws, stated element-free.** Four general capabilities, all missing today, all in one story, ENGINE-MECHANICAL-LINKS (owner decision 3a; text in the batch summary): (a) a mechanical connection carries a typed material id and the typed values its material declares, through authoring, save, wire and presentation; (b) a force-bounded ratio coupling, a third `TransmissionConstraintKind` beside `Bilateral` and `NonPositiveVelocity`, whose transmitted force is bounded by a capacity computed from declared tension, friction coefficient and wrap angle (the capstan relation), with the excess appearing as relative travel and as a ledger loss that is never a gain; (c) a physical pitch radius declared on each mechanical port, the capacity torque using the smaller of the pair; (d) a declared engagement class on ports and a required class on a material, with a typed refusal when they differ. Any package or material may declare them.
3. **Three layers.** Colliders: the solid loop generated from the material's declared thickness and segment rule (ENGINE-CONNECTION-MATERIALS, solid strand; owner decisions 1 and 2); no package collider. Visuals: the closed route and its travelling marks, drawn by the link presentation at the declared thickness and never used as a collider. Animation: the marks advance from committed travel (during slip, from the link's own committed travel) and write nothing back.
4. **Flow and typing.** The material is a typed id, engagement classes are a closed enum, `tension` and `friction` are f32 Reals with units and ranges declared by the material.
5. **Flat state.** Per belt connection: one row in the existing transmission table with its law kind, ratio, capacity inputs and slip state, fixed at compile time, and the loop's strand colliders fixed by the segment rule at compile. No body, sensor or store.
6. **Performance.** One coupling row per connection, solved with the existing transmission rows; the plan is disqualified if the bounded row needs a second code path (Delivery). The loop's colliders are bounded by compiled declarations and found through ENGINE-SPATIAL-QUERIES; nothing scans the scene.
7. **Tests are scenarios.** Rows B1 to B13 run the production simulation; the heavy-load rows use a declared qualification rotor, so they prove the law and not a part.

**Verdict: conforms; blocked on the engine programme (owner decision 3a), then on ENGINE-MECHANICAL-LINKS and ENGINE-CONNECTION-MATERIALS (solid strand), both stated element-free. No owner item remains.**
