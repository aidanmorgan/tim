# EL-155 · Rope-operated light — named-identity spec

**Current delivery (24 October 2026):** [EL-155 delivery slice](../invest/named-elements.md#element-155). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`). Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. Local +X is the beam direction; the pull knob travels along local −Y. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-155 |
| Name | Rope-operated light |
| Type | Optical |
| Anchor | [requirements.md#element-155](../requirements.md#element-155); named entry [named-elements.md#element-155](../invest/named-elements.md#element-155); scope index [todo-196](../requirements.md#todo-196) |
| Owner | S517 |
| Disposition | To deliver as a **single element**. It refines no catalogue part: it joins a rope-pulled slider, a supplied contact and a cone emitter in one package. |
| Planned package | id `pull_cord_light`, directory `elements/PullCordLight/`, class `PullCordLightElementDeclaration` |
| Roadmap story | [EL-155 delivery slice](../invest/named-elements.md#element-155), `named-el-155`; after ENGINE-OPTICAL and the Flashlight |
| Status | not started; no package; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES and takes every candidate from its segment query), on the Flashlight, and on ENGINE-CONNECTION-MATERIALS (solid strand) for its rope rows |

## 2. Declaration

Shape followed: the Linear pusher for a supply contact at a slider's endpoint (`elements/LinearPusher/LinearPusherElementDeclaration.cs:56`–`:57`), the Springboard for a sprung slider (`elements/Springboard/SpringboardElementDeclaration.cs:63`), the Impact lever for a rope port on a moving child body (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:41`–`:50`), and the Flashlight ([EL-210](EL-210-flashlight.md)) for the emitter.

| Item | Value |
| --- | --- |
| Interfaces | `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the optical emitter interface **(optics)** |
| Body 0 (static) | Colliders: housing box 0.9 × 0.6 × 0.6 m at the origin; collar cylinder, axis X, r 0.4 m, length 0.2 m at (0.55, 0, 0); foot box 0.6 × 0.12 × 0.8 m at (0.2, −0.36, 0); socket sphere r 0.075 m at the port. Material restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. |
| Body 1 (pull knob) | Rod cylinder, axis Y, r 0.04 m, length 0.2 m, centred (0, −0.1, 0) in the knob frame (it spans y 0 to −0.2), and a sphere r 0.06 m centred (0, −0.2, 0) at the rod's lower end; mass 0.05 kg; restitution 0.1, friction 0.3. Prismatic slider along Y based at (−0.25, −0.3, 0): lower limit −0.10 m, upper limit 0, stiffness 20 N/m, damping 0.6 N·s/m, bodies do not collide (the rod's upper part sits inside the housing). |
| Ports | `PortId.Tie`, Rope, Bidirectional, role `Tie`, `RopeAttachmentRole.Load`, on body 1 at the knob sphere's centre (0, −0.2, 0) in the knob's frame. `PortId.PowerIn`, Electrical, Input, on body 0 at (−0.45, 0, 0), normal −X. |
| Supply graph | `SupplyContactDescriptor` `pull` with `PrismaticContactControl(Prismatic 0, CoordinateEndpoint.Lower)` (`CuriousContraptions.Abstractions/SignalContactControl.cs:13`); `SupplyConsumerDescriptor` `emitter`, 100 W; `PowerIn` → `pull` → `emitter`. |
| Optical emitter **(optics)** | Frame body 0; origin (0.65, 0, 0), the collar's front face; axis +X; half-angle 15°; range 8 m; radiant power (30, 30, 30) W; demand 100 W from the consumer; no enable (it is optional in the conventions): the supply contact is its switch; internal occluders = housing and collar |
| Parameters | None. Travel and spring are fixed so the lesson is the cord, not a setting (D19). |
| Stores | None. Rope work moves the knob against its spring and never becomes light. |
| Presentation bindings | The knob art follows body 1's pose. `Visual/Lens` albedo slate (85, 101, 115) → warm cream (255, 240, 165), Linear over 0.06 s, from committed emission **(optics, M5)**. The cone is drawn by the shared renderer. |
| Footprint | `ElementFootprint(Bodies: 2, MovingBodies: 1, PrismaticConstraints: 1, RevoluteConstraints: 0)`; domains Mechanical, Electrical, Rope and the optical flag |
| Catalogue | Id `pull_cord_light`, "Pull-cord light", `ElementCategory.Power` (the Flashlight's category: both are light sources), colour (1, 0.97, 0.91), palette order 317 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Pulling its knob down switches on a light. Tie a rope to the gold knob and connect a separate supply; a slack rope turns it off." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Housing` | box 0.9 × 0.6 × 0.6 m, cream `#fff8e9` | solid (housing box) | nothing |
| `Visual/Collar` | cylinder r 0.4, length 0.2, gold `#f7cb52` | solid (collar cylinder) | nothing |
| `Visual/Lens` | disc drawn flush in the collar's front face | decorative: flush, recoloured by a binding | nothing (colour only) |
| `Visual/Foot` | box 0.6 × 0.12 × 0.8 m at (0.2, −0.36, 0), navy `#293954` | solid (foot box) | nothing |
| `Visual/Knob` | rod r 0.04, length 0.2, and sphere r 0.06, gold | solid (rod cylinder and knob sphere on body 1) | body 1's pose |
| Socket stud | sphere r 0.075, gold | solid (socket sphere) | nothing |
| Cone | translucent shells | decorative: light, drawn from the committed segment | the shared beam renderer |

No cord is drawn between the housing and the knob. The legacy proposal showed a short cord with no collider; a thick strand is solid (owner decision, 22 November 2026), so the pull is a rigid knob on a rod whose every visible part is a collider. The rope a player ties to the knob is a connection, drawn and collided by its declared material under ENGINE-CONNECTION-MATERIALS. Icon: an original navy outline of a lamp with a hanging knob. It differs from the Flashlight by a box body, no top button and the knob beneath.

### Compiled counts

Bodies 2 (1 static, 1 dynamic). Colliders 6 (2 boxes, 2 cylinders, 2 spheres). Prismatic joints 1. Supply nodes 2. Emitters 1. Presentation bindings 3 plus one body pose. Signal nodes, stores 0.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, OpticalTransport, RigidBodyDynamics, SignalPropagation, StateTransaction, TensionTransmission; navigation only (D14). Probe on `8d1d68aa`: 6 present, 2 present-untested, 3 narrower, 4 missing.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:237`) |
| R3 | Typed ports and wrong-kind refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R7 | Proportional sharing of a per-phase power allowance among compiled consumers | present, untested as used: the cited test proves the allocation function, not an emitter among other consumers; the first optical slice adds the row | `ElectricalSupply.Reserve` (`engine/gpu/ElectricalSupply.cs:11`) | `ElectricalSupplyTests.UnequalDemandSharesProportionallyAndFullLoadReceivesNothing` (`CuriousContraptions.tests/ElectricalSupplyTests.cs:207`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R16 | Prismatic slider with stiffness, damping and limits | present | `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`) | `WorkshopSpringboardTests.CompiledPreloadUsesActualF32PoseAndEightJoules` (`CuriousContraptions.tests/WorkshopSpringboardTests.cs:146`) |
| R17 | Rope port on a moving child body, pulled by a falling Weight | present | Impact lever `TieA` on body 1 (`ImpactLeverElementDeclaration.cs:41`–`:50`) | `ImpactLeverTests.RopeLiftingIntegration_FallingWeightLiftsArm` (`CuriousContraptions.tests/ImpactLeverTests.cs:414`) |
| R18 | Supply contact at the endpoint of a passive sprung slider | present, untested on a passive slider: every prismatic slot, driven or not, publishes an actuator row with its coordinate and limits (`engine/gpu/PhysicsPresentationRead.cs:25`–`:43`), which the endpoint contact reads (`CuriousContraptions.Simulation/WorkshopGpuDevice.cs:329`–`:346`), and the bundle admits the control for any declared slider (`CuriousContraptions.Bundle/SupplyBundleValidation.cs:78`–`:85`); the only executed use is on a driven slider | `PrismaticContactControl` | `LinearPusherTests.BothEndpointConsumersFollowDepartureReturnAndSupplyRemoval` (`CuriousContraptions.tests/LinearPusherTests.cs:65`); row P1 is the first proof on a passive one. If the probe fails, the owner is **ENGINE-SIGNAL-NODES** (threshold over a committed scalar, here the coordinate) |
| R6 | Supply consumer paying a continuous target | narrower: no emitter target (`CuriousContraptions.Abstractions/SupplyContractValues.cs:42`–`:54`) | `SupplyConsumerDescriptor` | `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`CuriousContraptions.tests/FanIntegrationTests.cs:540`). Owner: **ENGINE-OPTICAL** |
| R9 | Line-of-sight occlusion by solid colliders | narrower: airflow rows only, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`); the broadphase has only a pair-overlap query (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`) | `AirflowLineClear` | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing segment cast: **ENGINE-SPATIAL-QUERIES** |
| R11 | Presentation binding on a committed driver | narrower: no optical driver (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`) | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`). Owner: **ENGINE-OPTICAL** |
| M1 | A declared emitter as a supply consumer target, with grant scaling | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | The optical solve: once per tick after the last substep, with the diverging law and shadow casts from the spatial query | **missing** | — | owner **ENGINE-OPTICAL** |
| M5 | Committed-emission driver and cone read model | **missing** | — | owner **ENGINE-OPTICAL** |
| M6 | The optical `SimulationDomain` flag | **missing** | — | owner **ENGINE-OPTICAL** |

A colliding rope is a fifth dependency, outside this package: the delivered rope draws a strand with no collider, a recorded visible-solid defect owned by ENGINE-CONNECTION-MATERIALS (owner decision, 22 November 2026).

**Dependencies.** Flashlight ([EL-210](EL-210-flashlight.md), which proves the cone emitter first), Weight, Pulley and Rope anchor, Battery (delivered); an observer ([EL-153](EL-153-general-light-receiver.md) or a broadband detector).

## 4. Sources and legacy

**Sources.** Row [element-155](../requirements.md#element-155): real rope displacement actuates a separately supplied light source; slack rope or missing supply produces no light. [todo-196](../requirements.md#todo-196) scope index. Campaign: "drawstring source" first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage); [campaign-reservations](../invest/campaign-reservations.md)). S257 rope-port: a slack rope lifts nothing.

**Legacy** (no part; mechanisms only):
1. Torch cone values range 8, cosine 0.9659258, intensity 24; lens colour curve 0.06 s linear — `parts/FlashlightPart.cs@a6c914e:L10-L19`. Carry forward as the emitter.
2. Cone light law — `engine/LightNetwork.cs@a6c914e:L12-L52`. Carry forward the law; not the CPU solve.
3. Rope domain and attachment roles are closed enums — `engine/MachineData.cs@a6c914e:L68-L68`, `engine/ConnectionPort.cs@a6c914e:L11-L12`; loads accept one rope end; ropes cannot branch — `engine/RopeNetwork.cs@a6c914e:L69-L78`. Carry forward.
4. Rope route, length-at-connect, tension-only and slack/taut facts — recorded as R1–R19 in [CAT-058-rope_anchor](CAT-058-rope_anchor.md). Carry forward via that spec.
5. Lightest legacy weight 0.25 kg — `parts/WeightPart.cs@a6c914e:L22-L27` (recorded in [CAT-067-weight](CAT-067-weight.md)). Carry forward as the sizing basis for the spring.

**Files harvested:** `parts/FlashlightPart.cs`, `engine/LightNetwork.cs`, `engine/MachineData.cs`, `engine/ConnectionPort.cs`, `engine/RopeNetwork.cs`, `parts/WeightPart.cs`.

## 5. Acceptance

### Behaviour and numbers

Starting values (D1).

- **Knob at rest.** Weight 0.05 × 9.81 = 0.49 N against a 20 N/m spring: it hangs 0.49 ÷ 20 = 0.0245 m below the upper stop, 0.0755 m short of the lower one. The contact is open.
- **Pull.** At the lower stop the spring pushes up with 20 × 0.10 = 2.0 N, of which the knob's weight supplies 0.49 N: a rope must pull with more than 1.51 N. The lightest delivered Weight is 0.25 kg (`elements/Weight/WeightElementDeclaration.cs:33`), 2.45 N: it holds the knob on the stop with 0.94 N to spare. The heaviest, 8 kg, presses the stop with 78.5 − 1.51 = 77 N.
- **Contact.** Closed while the coordinate is within 0.5 mm of the lower limit (`PhysicalTolerance.ActuatorStopSlopMetres`, `CuriousContraptions.Abstractions/PhysicalTolerance.cs:14`). A slack rope pulls with nothing, the spring returns the knob, and the contact opens. Momentary: there is no latch (decision K-13).
- **Light.** As the Flashlight: (30, 30, 30) W over 0.2141 sr, I = 420.4 W/sr, 46.7 W/m² at 3 m, range 8 m. Demand 100 W = 0.833 J per tick, under the Battery's 1 J per tick. Light needs both the closed contact and a supplied `PowerIn`.
- **Tick order.** The knob's committed coordinate of tick n closes the contact at the start of tick n + 1; energy is granted in that tick's substeps and the cone is emitted by the solve after its last substep; a detector's contact follows one tick later.
- **Stability.** The knob is 160 times lighter than an 8 kg Weight hanging from it; the slice checks that it rests on the stop without drift under that load.

### Rows

Planned class `PullCordLightTests`. Fixture: the light; a Weight on a rope from the knob's `Tie`, resting on a Powered gate's blade or a ledge it can be pushed from; Battery `Supply` → `PowerIn`; a broadband detector (`threshold` 1.0 W) 3 m in front. Power tolerance ±2 %, the one optical set (coordinator decision 21); stall guard 600 ticks from the Weight's release.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| P1 | Given the fixture with a 0.25 kg Weight, when the Weight falls and the rope goes taut, then the knob reaches its lower stop, the cone exists and the detector reads a mean of 14.8 W | `PullCordLightTests.ATautRopePullsTheKnobAndLightsTheLamp` | P2, P3 | ±2 %; knob within 1 mm of −0.10 m |
| P2 | **Slack rope.** Given the Weight left resting with slack in the rope, then the knob stays within 5 mm of its 0.0245 m rest sag and no segment exists | `PullCordLightTests.ASlackRopeProducesNoLight` | P1 | 600 ticks |
| P3 | **Missing supply.** Given P1 with `PowerIn` unwired, then the knob reaches the stop and no segment exists | `PullCordLightTests.WithoutSupplyAPulledKnobProducesNoLight` | P1 | 600 ticks |
| P4 | Given P1 lit, when the Weight is lifted again (a Linear pusher raises it) so the rope slackens, then the knob returns and the light goes out; pulled again, it relights | `PullCordLightTests.TheLightFollowsTheRopeWithoutLatching` | P1 | light off within 60 ticks of slack |
| P5 | Given an 8 kg Weight, then the knob rests on the stop for 600 ticks with its coordinate inside the stop tolerance and the light steady | `PullCordLightTests.TheHeaviestWeightHoldsTheKnobSteadily` | P1 | coordinate within 1 mm; no reading drop |
| P6 | Given a Wall in the cone, then the detector reads nothing; given the detector at 8.5 m, nothing | `PullCordLightTests.SolidsAndRangeLimitTheCone` | P1 | 0.5 m beyond 8 m |
| P7 | Given a rope end offered to the housing, then no port accepts it; given an electrical wire offered to `Tie`, the edit is refused | `PullCordLightTests.OnlyTheKnobTakesARope`; generic R3 | — | typed refusal |
| P8 | Given a lit run, when Reset, then the knob is at its authored rest, no segment remains and the rope is as authored; a saved and reloaded construction keeps the rope and its length | `PullCordLightTests.ResetAndReloadRestoreTheAuthoredState`; authored state by R12 | P1 | knob within 1 mm of rest |

No parameter, so no parameter-refusal row. Chrome recipe (pending, post-catalogue): the light, a Pulley above a ledge, a Weight, a Battery and a light sensor in the beam; tie a rope from the Weight to the knob; Run.

## 6. Open questions

None open.

1. **Momentary or latching — decided** (K-13, D19): momentary; the row says displacement actuates it.
2. **Geometry, mass, spring and emitter values — decided** (D1): section 2, with the knob replacing the drawn cord.
3. **Cone or narrow beam — decided** (D19): a cone equal to the Flashlight's, so torch lessons transfer.
4. **Roadmap — resolved**: its own slice after the Flashlight.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Weight, Pulley, Rope anchor, Linear pusher, Battery (delivered); Flashlight, light sensor, light-charge receiver, Solar panel (undelivered). Port domains agree: `Tie` is Rope, Bidirectional, role Load, as the Weight's (`WeightElementDeclaration.cs:41`–`:45`); `PowerIn` is Electrical Input.
- **Campaign.** "Drawstring source", first use 51–60.
- **Coverage binding (planned, not yet live).** A named package is bound by the binding section that main's `tools/Coverage/NamedPackageSources.cs` reads; **blocked on the coverage follow-on story S7** (source [todo-196](../requirements.md#todo-196) is a scope index; decision S517 has no anchor in `docs/planning/invest/scope-corrections.md`). S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15); the section is not written here.
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL as corrected in the batch summary; the Flashlight; ENGINE-CONNECTION-MATERIALS for the rope's solid strand; S7 for the binding. No joint story is needed: the endpoint contact exists (row R18).
- **Files.** `elements/PullCordLight/PullCordLight.csproj`, `PullCordLightElementDeclaration.cs`, `pull_cord_light.tscn`, `icon.svg`; one guarded `CuriousContraptions.slnx` line; `CuriousContraptions.tests/PullCordLightTests.cs`; a `VisibleSolidColliderTests` row; this spec's binding section.
- **Review tier.** Data-only if the probe finds every row present after its engine prerequisites; otherwise engine.
- **Disqualifiers.** The endpoint contact failing on a passive slider (then ENGINE-SIGNAL-NODES' threshold over the committed coordinate is the owner, and the slice waits for it); the knob unstable under an 8 kg Weight (raise its mass and re-derive the spring: at 0.1 kg the rest sag is 0.049 m and the needed pull 1.02 N); a different unit scale.

## 9. Engine conformance

Verdict: **Conforms.**

1. **Declaration data only.** Two bodies, one slider, one rope port, one endpoint contact, one consumer, one emitter. No code turns "rope pulled" into "light on".
2. **Generic laws.** Tensile span, spring slider with stops, endpoint contact, supply reservation, the diverging segment law.
3. **Three layers.** Colliders: six primitives on two bodies. Visuals: housing, collar, foot, knob, stud; no cord. Animation: the lens colour from committed emission; the knob's art follows its body.
4. **One-way flow and typing.** Knob pose → contact → emitter → readings; ports and endpoint are enums.
5. **Flat state.** Counts in section 2.
6. **Performance fitness.** One small dynamic body, which will sleep at rest once `ENGINE-ISLANDS-SLEEP` lands (no island sleeping exists today); six primitives, each a visible solid; one diverging segment while lit.
7. **Tests are scenarios.** Rows P1 to P8.
