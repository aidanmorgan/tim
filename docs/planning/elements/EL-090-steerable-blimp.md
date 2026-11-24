# EL-090 · Steerable blimp — named-identity spec

**Current delivery (24 October 2026):** [EL-090 delivery slice](../invest/named-elements.md#element-090). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 are written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lstart-Lend` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32; pressure is pascals absolute with ambient 101 325 Pa.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-090 · Steerable blimp · Specialist |
| Anchor | [requirements.md#element-090](../requirements.md#element-090) ([sequence-task-643](../requirements.md#sequence-task-643)); [named entry](../invest/named-elements.md#element-090); scope index [todo-216](../requirements.md#todo-216) |
| Proof owner | S660 |
| Disposition | To deliver as a single element. Not a duplicate of the Balloon: it keeps every Balloon behaviour (the ledger says it "also retains CAT-003 acceptance", `docs/planning/invest/named-elements.md:840`) and adds a store, an airflow source, a signal input and three colliders |
| Planned package | id `blimp`, directory `elements/Blimp/`, class `BlimpElementDeclaration` |
| Refines / extends | [CAT-003 Balloon](CAT-003-balloon.md) (gas store, buoyancy, drag, airflow receiver, rupture, tie); the Fan's self-contained airflow source ([CAT-028](CAT-028-fan.md)) |
| Status | not started; blocked on the engine programme (owner decision 3a), then `ENGINE-REACTION-THRUST`, then S7. Its tie row is also blocked on `ENGINE-CONNECTION-MATERIALS` (solid strand) |

## 2. Declaration

A buoyant dynamic body that carries a finite energy store and one airflow source at its tail. The reaction of that jet is its drive. There is no steering rule in code: heading is the pose the player builds, the engine starts on a declared signal, and everything after that is buoyancy, drag, airflow and contact.

The package implements all the Balloon's interfaces (`elements/Balloon/BalloonElementDeclaration.cs:10`–`:12`): `IMechanicalElement`, `ISceneGeometryElement`, `IGasStoreElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:109`), `IGasShapeElement` (`:114`), `IContactIntegrityElement` (`:119`), `IAmbientLoadElement` (`:124`), `IAirflowReceiverElement` (`:45`); and from the Fan (`elements/Fan/FanElementDeclaration.cs:10`): `IEnergyStoreElement` (`:35`), `IAirflowSourceElement` (`:40`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`). `KinematicMode.Dynamic`, `SupportedDomains = Mechanical | Signal | Rope`, `PayloadPolicy.None`. Footprint: 1 body, 1 moving, 0 constraints.

### Body and colliders

One dynamic body; local +X is forward. Dimensions in metres.

| # | Collider | Kind | Dimensions and place |
| --- | --- | --- | --- |
| 0 | Envelope | Sphere | radius 0.45 in the Standard atmosphere, bound to the gas shape (below) |
| 1 | Gondola | Box | 0.5 × 0.2 × 0.25 centred (0, −0.55, 0): its top touches the envelope |
| 2 | Tail duct | Cylinder about X | radius 0.12, half-length 0.06, centred (−0.51, 0, 0) |
| 3 | Socket | Sphere | radius 0.05 at (0.25, −0.55, 0), the gondola's front face |

Material: restitution 0.2 (the Balloon's default bounce, `BalloonElementDeclaration.cs:22`), friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. Mass frame: the Balloon's centred-inventory source (`BalloonElementDeclaration.cs:58`–`:62`) on parameter `mass`, centre of mass at the origin, the Balloon's inertia rule, gravity (0, −9.81, 0).

**Gas.** One `GasStoreDescriptor` with the Balloon's values (`BalloonElementDeclaration.cs:63`–`:66`): gas material (2000, 3000, 1, 1000, 1, 200000), charge pressure **102 325 Pa** absolute (1 000 Pa above ambient), 300 K, and a charge volume of 4π/3 × (0.45³ − 0.0625³) m³, constant. One `GasShapeDescriptor` on collider 0 with dry radius 0.0625. Airflow receiver on collider 0 with the Balloon's four samples (`:40`–`:43`).

**Ambient load.** One `AmbientLoadDescriptor` (`:69`–`:71`): upward acceleration parameter `buoyancy`, linear drag parameter `drag`, tied to the gas store, and an **angular drag of 2 /s** (the general ambient angular drag that `ENGINE-REACTION-THRUST` adds, section 3). The Balloon's inertia rule gives this body very little spin inertia, so a glancing contact would otherwise leave it spinning and its heading lost; at 2 /s a spin dies to a third in half a second.

**Rupture (retained from the Balloon).** One `ContactIntegrityDescriptor` (`CuriousContraptions.Abstractions/GasCouplingValues.cs:15`; the Balloon's, `BalloonElementDeclaration.cs:68`) on collider 0 and the gas store: threshold **6 N·s** of one contact pair's accepted normal impulse, minimum approach speed 0.1 m/s. On rupture the store's outlet opens once, the gas leaves, the lift goes with it and the craft falls. The threshold is twice the Balloon's 3 N·s because this body is heavier and flies under power:

| Contact | Impulse (reduced mass × (1 + 0.2) × closing speed) | Result |
| --- | --- | --- |
| Blimp into a Wall at cruise, 3.33 m/s | 1.2 × 1.2 × 3.33 = 4.8 N·s | intact |
| Bowling ball (4 kg) dropped 0.5 m onto it, 3.13 m/s | 0.923 × 1.2 × 3.13 = 3.5 N·s | intact |
| Bowling ball dropped 3 m onto it, 7.67 m/s | 0.923 × 1.2 × 7.67 = 8.5 N·s | ruptures (threshold crossed at 5.4 m/s, a 1.5 m drop) |
| A Thumb tack (EL-101) | by that part's declared stress concentration | ruptures, as it does the Balloon |

**The thrust line passes through the centre of mass** (tail duct on the axis), so the drive adds no pitching moment. A gondola-mounted propeller 0.55 m below the centre would apply 12 × 0.55 = 6.6 N·m and tumble the craft.

### Store and airflow source

| Item | Value | Reason |
| --- | --- | --- |
| Energy store | `EnergyStoreDescriptor` id 1 on body 0, capacity and initial charge 3 600 J (the Fan's form, `FanElementDeclaration.cs:42`–`:44`) | "supplied": a finite on-board supply, the size of a default Battery (`elements/Battery/BatteryElementDeclaration.cs:42`) |
| Airflow source | frame body 0, origin (−0.57, 0, 0), axis −X, reach 2.0, terminal radius 0.5, flow speed 10 m/s, maximum force `thrust`, maximum power `thrust` × 10, flow sense Diverging, **ambient reaction declared** | the wash blows backward; its reaction pushes the craft forward |
| Internal occluders | colliders 0 and 2 | the jet starts inside the duct, behind the envelope |
| Input | one `StoredAirflowInput` on store 1 with `Enable = (powered, enable)` (`FanElementDeclaration.cs:53`) | off until started, or on from the start |

### Parameters and ports

| Name | Type | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `mass` | Real | Kilogram | 1.2 | 0.6 | 3 | Reject | BuildOnly |
| `buoyancy` | Real | MetrePerSecondSquared | 9.81 | 0 | 16 | Reject | BuildOnly |
| `drag` | Real | InverseSecond | 2 | 0 | 4 | Reject | BuildOnly |
| `thrust` | Real | Newton | 12 | 0 | 12 | Reject | BuildOnly |
| `powered` | Choice (Engine: Off, Engine: On) | None | 1 (On) | 0 | 1 | Reject | BuildOnly |

`buoyancy` and `drag` ranges are the Balloon's (`BalloonElementDeclaration.cs:24`–`:25`). At the default the craft is neutrally buoyant: it holds its height, so heading and thrust are the lesson.

| `PortId` | Domain | Direction | Role | Kind | Local position |
| --- | --- | --- | --- | --- | --- |
| `ActivationIn` | Signal | Input | `ActivationIn` | Pulse | the socket, (0.25, −0.55, 0), normal +X |
| `Tie` | Rope | Bidirectional | `Tie`, attachment role Load | — | the body origin, normal −Y (the Balloon's, `BalloonElementDeclaration.cs:37`–`:39`) |

Signal nodes: one `SignalMemoryDescriptor` `enable`; `ActivationIn` binds its Set terminal (the Fan's wiring exactly, `FanElementDeclaration.cs:58`–`:60`, `:69`–`:71`). A pulse from an Impact switch, Pressure plate or Ball detector starts the engine; it then runs until the store is empty. A signal wire is a thin wire and is drawn non-solid (owner decision 1, 22 November 2026).

**The tie** is the Balloon's rope port at the centre of mass, so a taut rope restrains the craft without turning it. A rope is a thick strand and therefore solid (owner decision 1): the collider of a rope belongs to its declared connection material, owned by `ENGINE-CONNECTION-MATERIALS`, and the delivered rope carries that story's recorded visible-solid defect until it lands. The blimp itself draws no string; the Balloon's thin-string exception is not extended to it.

### Metadata and bindings

Category `Motion`, display name "Blimp", palette order 180 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.93, 0.39, 0.47). Events: `SimulationEventKind.MemoryChanged` on `enable`; `SimulationEventKind.IntegrityBroken` on the observer (`BalloonElementDeclaration.cs:53`). Presentation: `Envelope` uniform scale on `PresentationDriver.CommittedGasRadius` (the Balloon's binding, `BalloonElementDeclaration.cs:29`–`:31`); `Visual/Duct/Blades` rotation on `PresentationDriver.CommittedFlowSpeed` with `PresentationCurve.IntegratedRate` (the Fan's, `FanElementDeclaration.cs:85`–`:88`).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| `Envelope` | balloon-pink `#ed6378` sphere with two cream `#fff8e9` bands printed flush | solid, collider 0; its scale follows the committed gas radius, as the collider does |
| `Visual/Gondola` | navy `#293954` box | solid, collider 1 |
| `Visual/Duct` | navy ring housing | solid, collider 2 |
| `Visual/Duct/Blades` | gold `#f7cb52` three-blade propeller inside the duct | decorative: enclosed, and moved by a binding |
| `Visual/Socket` | navy sphere | solid, collider 3 |
| `Visual/Arrow` | cream arrow printed on the gondola side, pointing forward | decorative: printed flat |

Icon: a round envelope with a gondola beneath, a ducted propeller behind and a forward arrow. It differs from the Balloon by the gondola and duct, and has no string.

### Behaviour and numbers

Laws: isobaric gas envelope, declared ambient lift (buoyancy in air is that one law, coordinator decision 28) and drag and contact-integrity rupture (the Balloon's), conical airflow with its ambient reaction, the tensile span on the tie, contact.

**Thrust sign.** The reaction factor is clamp((flow speed + origin velocity · axis) ÷ flow speed, 0, 1). The axis is −X and the craft moves at +v along X, so origin velocity · axis = −v and the factor is (10 − v) / 10: full thrust at rest, none at 10 m/s, and never more than `thrust` when the craft is blown backward. Hand calculations at the defaults (mass 1.2 kg, drag force 1.2 × 2 × v = 2.4 v newtons, thrust 12 (1 − v/10) newtons):

- **Still air, engine on.** 12 (1 − v/10) = 2.4 v gives a cruising speed of **3.33 m/s**; it reaches 63 % of that in 1 / (2 + 12 / (10 × 1.2)) = 0.33 s.
- **Power.** The jet is paid as airflow work is today, reaction impulse × flow speed: 12 × 10 = **120 W** (1 J per tick) at a standstill, 12 × 0.667 × 10 = 80 W at cruise. 3 600 J lasts 30 s held at a standstill and 45 s at cruise; then thrust is zero.
- **Against a Fan** (default 9 N at 12 m/s, `FanElementDeclaration.cs:22`, `:48`), head-on with the whole envelope in its cone. Engine on: 12 (1 − v/10) − 9 − 2.4 v = 0 gives **0.83 m/s forward**. Engine off: 9 (1 − u/12) = 2.4 u gives **2.86 m/s backward**. This is the requirement's outcome: unpowered, it cannot steer against a current.
- **Height.** Net vertical acceleration is `buoyancy` − 9.81 = 0; at `buoyancy` 11.5 (the Balloon's) it climbs at (11.5 − 9.81) / 2 = 0.85 m/s.
- **Moored.** With a 2 m rope from the tie to a Rope anchor and the engine on, it runs out to 2 m and holds there, the rope carrying the 12 N.
- **Vacuum atmosphere.** No lift, no drag and no jet (`AirflowSourceDeclaration.PressureScale`, `CuriousContraptions.Abstractions/AirflowValues.cs:72`): it falls.

A body behind the tail inside the wash is pushed back by the same jet, as by a small Fan.

**Run length (owner decision 13).** Every row but one fits the default 30 s Run (`CuriousContraptions.Abstractions/CadenceValues.cs:77`). The store-exhaustion row (A5) needs a level that declares a Run length of at least 60 s (`ENGINE-RUN-LENGTH`); a level that only wants the engine to run out inside 30 s moors the blimp, which empties the store in 30 s.

**Tick order.** A pulse on `ActivationIn` is routed in the tick it is emitted; the memory enables the source and the jet acts in that tick's physics step.

## 3. Engine capabilities

Map row families: AerodynamicDrag, Buoyancy, EnvironmentState, FiniteLedger, FiniteWorkActuation, GasState, GeometryQuery, JointConstraint, ProgrammableController, RigidBodyDynamics, SignalPropagation, TimedCommand (navigation only under D14; the part has no joint, controller or timer).

| Capability | Status | Evidence |
| --- | --- | --- |
| Sealed gas envelope that lifts in Standard and falls in Vacuum | present | `GasStoreDescriptor` (`BalloonElementDeclaration.cs:63`); `BalloonTests.StandardRisesAgainstHeavierControlAndVacuumFallsWithTheSameSealedInventory` (`CuriousContraptions.tests/BalloonTests.cs:22`) |
| Parameter-bound lift and linear drag | present | `AmbientLoadDescriptor` (`CuriousContraptions.Abstractions/GasCouplingValues.cs:19`); `BalloonTests.NondefaultLiftAndStrongDragChangeStandardMotion` (`BalloonTests.cs:144`) |
| Rope tie on a dynamic body: taut restrains, slack does not | present | `BalloonTests.TautTieRestrainsLiftWhileSlackAndDisconnectedControlsRise` (`BalloonTests.cs:95`) |
| Rupture by one contact pair's impulse empties the store and leaves the tie in place | present today on a gas store | `ContactIntegrityDescriptor` (`GasCouplingValues.cs:15`); `BalloonTests.AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie` (`BalloonTests.cs:118`). `ENGINE-CONTACT-OBSERVATIONS` (owner decision 3a; audit E6) becomes the observer's one owner and changes its form; the package is written in the form that story leaves |
| A Fan's cone pushes a body that declares a receiver; off or in Vacuum it does not | present | `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` (`CuriousContraptions.tests/FanIntegrationTests.cs:514`) |
| A self-contained store pays an airflow source, gated by a `powered` choice | present | the same test (`:481`): the store falls below 14 400 J only when powered |
| A dynamic source body receives the paired force and moment of its jet **on an exposed receiver** | present | `AirflowRuntimeTests.MovingNozzleReceivesPairedForceAndMoment` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:189`): momentum of nozzle and cargo sums to zero |
| A dynamic source body is pushed by its jet **when nothing is in the cone** | missing | a branch exists only per exposed receiver sample (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:105`–`:130`); a jet into open air applies nothing and pays nothing. The gas-outflow path explicitly has no thrust (`GasRuntimeTests.FiniteOutflowChangesOneMassAndShapeWithoutThrustOrChangingDrySpinInertia`, `CuriousContraptions.tests/GasRuntimeTests.cs:151`), and the axial gas chamber of `ENGINE-GAS-NETWORK` is a chamber on a prismatic coordinate. Owner: `ENGINE-REACTION-THRUST` |
| Ambient angular drag | missing | `ENGINE-REACTION-THRUST`, rider (c) (coordinator decision 16) |
| Airflow receivers and occluders found without visiting every receiver and collider | missing | each source visits every receiver (`WorkshopPhysicsEngine.Airflow.cs:105`) and each sample's occlusion every collider (`:149`). Owner: `ENGINE-SPATIAL-QUERIES` (owner decision 3a) |
| An airflow source, energy store, signal memory and Signal port declared on a **dynamic** package | present, untested | the law scene's nozzle is dynamic (`AirflowGeneralityTests.cs:192`); every delivered package with a source, store or signal port is static |
| A gas-shape sphere beside other colliders on one dynamic body | present, untested | `GasRuntimeTests.NonfirstBodyAndColliderOwnTheExactEnvelopeAndContact` (`GasRuntimeTests.cs:251`) by name; its body was not opened. Multi-collider dynamic bodies arrived with `engine-multi-collider-dynamic` |
| A constant gas charge volume (no size parameter) | present, untested | the Balloon's volume polynomial names a parameter (`BalloonElementDeclaration.cs:65`); whether it accepts all-zero coefficients is for the probe |
| A solid rope strand | missing | `ENGINE-CONNECTION-MATERIALS` (owner decision 1); blocks acceptance of the tie row only |

**Canonical story text** (the rocket, firework, missile and Vacuum nozzle specs cite this copy):

> ### Story ENGINE-REACTION-THRUST: Airflow source extensions: ambient reaction, converging flow, ambient angular drag
> *Status: Backlog. Sprint key: `engine-reaction-thrust`. Amends REQ-12. After ENGINE-SPATIAL-QUERIES; every receiver and occluder candidate of an airflow source comes from ENGINE-SPATIAL-QUERIES.*
>
> *Present today:* a conical airflow source on a dynamic frame receives the equal and opposite force and moment of what it delivers to each exposed receiver sample, paid from its store, supply or coordinate. *Missing, in one change to the airflow source descriptor, its wire row and its read:*
>
> (a) **Ambient reaction.** A source may declare an ambient reaction: a reaction impulse on the frame body at the source origin, opposite to the axis, equal to the delivered law evaluated for the still ambient medium: (granted limit − impulse granted to exposed receivers) × pressure scale × clamp((flow speed + origin velocity · axis) ÷ flow speed, 0, 1). The work paid is that impulse × flow speed from the same finite input, for every input kind (store, supply consumer, coordinate). A source that does not declare it behaves exactly as today. The committed airflow read reports the ambient share.
>
> (b) **Flow sense.** A source declares a flow sense from a closed enum (Diverging, Converging). A diverging source behaves exactly as today. For a converging source the region is the same cone; the force on a sample is along the unit vector from the sample to the origin; the receiver's speed is measured along that vector; the occlusion segment runs from the sample to the origin, with the same internal-occluder exemptions; caps, payment, pairing with the source frame and atmosphere scaling are unchanged. A converging source has no ambient share: declaring both is refused. One consumer is recorded today, EL-100.
>
> (c) **Ambient angular drag.** An ambient load may declare an angular drag (1/s) applied to the body's angular velocity as the linear drag is applied to its linear velocity, scaled by the atmosphere's pressure scale, zero by default.
>
> *Unblocks:* EL-090, EL-100, and the reaction-driven Specialist identities (rocket, firework, missile).
>
> **Specific acceptance:**
> - **Given** two differently parameterised free bodies, each carrying a declared source with an ambient reaction and a finite store, **when** enabled with nothing in the cone, **then** each accelerates opposite to its axis, never exceeds its flow speed, and stops accelerating when its store is empty; total debit equals work paid within the ledger residual. The same holds for a supply-consumer input and a coordinate input.
> - **And** with a receiver filling the cone, the reaction on the frame equals the force delivered to the receiver and the pair gains no net momentum, as today; in a vacuum atmosphere, or disabled, the body is not accelerated and nothing is debited.
> - **And** given two differently parameterised converging sources and a receiver inside each cone, each receiver accelerates toward its source's origin; a solid between the receiver and the origin stops the transfer while the same solid behind the receiver does not; an undefined sense is refused before any state changes.
> - **And** two differently parameterised free bodies given the same spin stop within the declared time constant ratio; a body declaring zero spins on.

## 4. Sources and legacy

- Requirement: "Buoyant body combines gas state with a bounded supplied directional drive"; outcome "Unpowered drive cannot steer against a current" ([element-090](../requirements.md#element-090)). The ledger entry also retains [CAT-003 acceptance](CAT-003-balloon.md): rise, tether and puncture with gas loss.
- Legacy search at `a6c914e` for "blimp" and "airship" returned nothing.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Balloon: bounce 0.2, drag 0.4, buoyancy 11.5 | `parts/catalog/balloon.tres@a6c914e:L11-L14` | Carry forward bounce; lift and drag are this part's own defaults (hover, and a readable cruise) |
| L2 | Legacy buoyant force = buoyancy × pressure × mass | `reference/cpu/MachineWorld.cs@a6c914e:L881-L882` | Carry forward the law as the ambient load. Do not carry forward the CPU loop |
| L3 | Fan default force 9, reach 5 | `parts/catalog/fan.tres@a6c914e:L14-L14` | Carry forward as the current used in the control (delivered: `FanElementDeclaration.cs:22`–`:23`) |

**Files harvested:** `parts/catalog/balloon.tres`, `parts/catalog/fan.tres`, `reference/cpu/MachineWorld.cs` (buoyancy and drag lines only).

## 5. Acceptance outline

Requirement row: [element-090](../requirements.md#element-090). Planned test class `BlimpTests`. The blimp is placed 6 m above the bench heading +X in the Standard atmosphere unless stated.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given `powered` On, when run for 3 s, then it moves forward at its cruising speed and holds its height; at `buoyancy` 11.5 it also climbs | `ThePoweredBlimpCruisesAlongItsHeading` | A2 | forward speed 3.3 m/s within 0.4; height change under 0.2 m; at 11.5, climb rate 0.85 m/s within 0.2 |
| A2 | Given `powered` Off, then it hovers in place and the store is untouched | `AnUnpoweredBlimpHovers` | A1 | displacement under 0.2 m over 5 s; store exactly 3 600 J (a ledger fact) |
| A3 | Given a Fan 4 m ahead blowing back along the heading: engine Off, it is carried backward; engine On, it advances | `OnlyThePoweredBlimpMakesWayAgainstACurrent` | the two cases against each other | Off: backward speed above 1.5 m/s after 3 s (2.86 by hand). On: forward speed above 0.3 m/s after 3 s (0.83 by hand). This is the requirement's outcome |
| A4 | Given `powered` Off and an Impact switch wired to `ActivationIn`, when a ball strikes the switch, then the blimp starts; with the wire removed it does not | `ASignalStartsTheEngine` | the unwired case | forward speed above 2 m/s within 2 s of the strike; a stall guard of 5 s for the strike |
| A5 | Given the engine running in a level whose Run length is 60 s, then the store falls, the debit equals the airflow work, no tick debits more than 1 J, and when the store is empty the blimp coasts to rest | `ThrustIsPaidFromTheFiniteStoreAndEndsWithIt` | A2 | debit never below the work and above it by no more than the ledger residual; no tick debits more than the 1 J allowance (power_limit × dt) plus the ledger residual; exhaustion between 40 and 55 s (45 by hand); speed under 0.3 m/s within 5 s of exhaustion |
| A6 | Given a Wall ahead, then the blimp stops against it by contact and does not rupture. Given a Tennis ball resting on the top edge of a Wall, level with the tail and 1 m behind it, and the blimp held by a 2 m tie, then the wash pushes the ball off the Wall backward | `ContactStopsItAndTheWashPushesWhatIsBehind` | engine Off: the ball stays on the Wall | overlap with the Wall under 0.02 m; integrity Intact; ball speed above 0.5 m/s backward within 2 s of the rope going taut |
| A7 | Given the Vacuum atmosphere, then it falls and the engine gives no thrust and no debit | `InAVacuumItNeitherFloatsNorDrives` | A1 | downward speed above 5 m/s after 1 s; forward speed under 0.05 m/s |
| A8 | Given heading +Z by placement, then it cruises along +Z | `HeadingIsThePlacedPose` | A1 | velocity direction within 10° of the heading |
| A9 | Given a 2 m rope from `Tie` to a Rope anchor and the engine On, then it runs out to the rope's length and holds; with an 8 m rope or none it is not yet restrained after the same time | `ATautTieRestrainsThePoweredBlimp` | slack and untied cases | distance from the anchor 2.0 m within 0.05 and speed under 0.1 m/s after 6 s; the untied craft is more than 8 m away. Retained CAT-003 row 4. Blocked on `ENGINE-CONNECTION-MATERIALS` for the solid strand |
| A10 | Given a Bowling ball dropped 3 m onto the envelope, then it ruptures once, the store empties, the craft falls and the tie's port does not move; dropped 0.5 m, it does not rupture | `AHardImpactRupturesTheEnvelopeOnce` | the 0.5 m drop | 8.5 and 3.5 N·s bracket the 6 N·s threshold; exactly one `IntegrityBroken` event; gas mass zero; height below the starting height by more than 2 m within 5 s. Retained CAT-003 row 5 |
| A11 | Given a glancing ball strike that sets the craft spinning, then the spin dies away | `AmbientAngularDragStopsASpin` | — | angular speed under a tenth of its peak within 2 s (time constant 0.5 s) |
| A12 | An Electrical output offered to `ActivationIn` and a Signal output offered to `Tie` are refused; each parameter one step outside its range is refused; Reset restores pose, store, gas state, integrity and `enable`; save and load keep parameters, pose, the wire and the rope | `ConnectionsAndParametersAreValidatedAndStateRoundTrips` | in-range values, a Signal output on `ActivationIn` and a rope on `Tie` are accepted | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`, as CAT-003 row 9); `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

Retained CAT-003 acceptance, by row of that spec: rise against a control (A1 at `buoyancy` 11.5), rests against a ceiling without rupture (A6's Wall), vacuum falls (A7), tether (A9), rupture (A10), wrong-kind connection (A12). None is dropped.

### Integration

- Works with: Fan (the current), Wall and Ramp (contact), Rope anchor and the rope connection (tie), Bowling ball and the planned Thumb tack (rupture), Impact switch, Pressure plate, Ball detector, Counter and Hold timer (signal sources), balls in its wash. All delivered except the Thumb tack.
- Campaign: chapter 10 "Oddly Satisfying", levels 91–100, "gravity pad/blimp" (`docs/planning/requirements.md:2295`); "drifting blimp", first use 91–100, reuse 136–150 (`:2411`). A level that fixes one needs the lesson-fixture story (S8); a level that runs the store out in free flight declares a Run length of 60 s.
- Coverage binding: section 7.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a; of its stories this part leans on `ENGINE-SPATIAL-QUERIES` for airflow candidates and `ENGINE-CONTACT-OBSERVATIONS` for the integrity observer); then `ENGINE-REACTION-THRUST`; then S7. `ENGINE-CONNECTION-MATERIALS` for acceptance of row A9; `ENGINE-RUN-LENGTH` for row A5.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (`ActivationIn`, `Tie`).
- **Files.** `elements/Blimp/` (project, declaration, `blimp.tscn`, `icon.svg`); `CuriousContraptions.tests/BlimpTests.cs`; one `CuriousContraptions.slnx` line; until S3, the remaining per-package lists.
- **Review tier.** Engine: three rows are *present, untested*.
- **Disqualifiers the probe re-checks.** A Signal connection that cannot follow a moving body; a store or airflow source refused on a dynamic package; a gas shape refused beside other colliders; a constant charge volume refused; an integrity observer that reads gondola or duct contacts as envelope contacts.

## 6. Open questions

All resolved.

1. **Supply is an on-board finite store**, not a power wire: the requirement's "supplied" and "unpowered" are both met by a store that can be off or empty, and a free-flying body then needs no power connection. Decided under the standing delegation.
2. **Neutral buoyancy by default**; the Balloon's rise is one parameter away.
3. **"Steerable" is the placed heading plus a signal start** (recorded decision). No reversible thrust, rudder or run-time control: `RunControlLane` has no lane for them (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:50`), and a rudder that turned the craft toward something would be a controller. The Fan's current and a tie are how a player redirects it in Run.
4. **Rupture and the tie are kept** from the Balloon, as the ledger requires. The threshold is 6 N·s so that flying into a Wall at cruise does not burst it.
5. **Fixed envelope size** (D13).
6. **Starting values** (D1): mass, drag, angular drag, thrust, flow speed, store, rupture threshold.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`) and owner decisions 1, 3a and 13 and coordinator decision 16.

1. **Declaration data only.** One body, four colliders, a gas store and shape, an integrity observer, an ambient load, a receiver, an energy store, an airflow source, a signal memory, two ports, five parameters, two bindings. No controller.
2. **Generic laws.** Isobaric gas, ambient lift and linear and angular drag; contact-integrity observation opening the store's outlet; conical airflow with its ambient reaction; the tensile span on a declared tie; the signal network's memory rule; contact. General capabilities are missing, each with one owner.
3. **Three layers.** Colliders: Sphere 2, Box 1, Cylinder 1, all on the one body. Visuals: one mesh per collider, all following the body pose; bands and arrow printed flat. Animation: envelope scale from the committed gas radius (the collider has the same radius) and blades from the committed flow speed; both read-only.
4. **Flow and typing.** One typed Signal input and one typed Rope port; `powered` is a Choice; units on every Real; enums for kinds, roles and flow sense.
5. **Flat state.** Bodies 1 (dynamic); colliders 4 (Sphere 2, Box 1, Cylinder 1); joints 0; drives 0; integrity observers 1; gas stores 1; gas shapes 1; ambient loads 1; airflow receivers 1 (4 samples); airflow sources 1; energy stores 1; domain nodes: 1 signal memory; presentation bindings 2.
6. **Performance.** Three colliders more than the Balloon, each a visible solid. It moves at under 4 m/s, far below any tunnelling concern. Airflow candidates come from `ENGINE-SPATIAL-QUERIES`; the present pass over every receiver and collider is that story's to remove and nothing here is designed around it. A body in an airflow or under thrust does not sleep (`ENGINE-ISLANDS-SLEEP`).
7. **Tests are scenarios.** Every row runs the production simulation with a delivered Fan, Wall, rope, switch and balls.

**Verdict: conforms** (general capabilities missing, each with one owning story).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `blimp` |
| Named identity | EL-090, `../invest/named-elements.md#element-090` |
| Acceptance | `todo-216`, `sequence-task-643` (`docs/planning/requirements.md:5117`) |
| Decision | S660, `../invest/scope-corrections.md#s660` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s660` block; and `todo-216` is a scope index shared with EL-089 (`docs/planning/requirements.md:4096`) that is not anchored beside `sequence-task-643`, which the reader's task-source rule requires. The sequence task itself exists.
