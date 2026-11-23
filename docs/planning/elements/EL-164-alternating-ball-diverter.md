# EL-164 · Alternating ball diverter named-identity spec

**Current delivery (24 October 2026):** [EL-164 delivery slice](../invest/named-elements.md#element-164). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every `path@a6c914e:Lstart-Lend` citation resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

**Build-ready revision, 21 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 were rewritten against current code; a `path:line` citation without a commit is in this checkout. Section 4 is the unchanged harvest. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-164 |
| Name | Alternating ball diverter |
| Type | Mechanical |
| Anchor | [requirements.md#element-164](../requirements.md#element-164); [named entry](../invest/named-elements.md#element-164); scope index [todo-246](../requirements.md#todo-246); combination [todo-319](../requirements.md#todo-319) |
| Proof owner | S375 |
| Disposition | To deliver as a single element, third of the diverter run, after [EL-162](EL-162-fixed-ball-diverter.md) and [EL-163](EL-163-powered-ball-diverter.md). Not a duplicate of EL-163: it has no command ports and adds two passage sensors and two memories. |
| Planned package | id `diverter_alternating`, directory `elements/DiverterAlternating/`, class `DiverterAlternatingElementDeclaration` |
| Refines / extends | No CAT spec. Junction and drive of EL-163; passage sensing of [CAT-002 ball detector](CAT-002-ball_detector.md); memory and switched supply of [CAT-037 latch](CAT-037-latch.md) |
| Related | CAT-020 Counter (the todo-319 combination) |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SENSOR-VOLUMES (two passage sensors on one element, in the tick), ENGINE-CAPABILITY-INSTANCES (one-sensor-per-element slots removed) and ENGINE-SUPPLY-DEMAND (the supplied directional drive); then EL-162 and EL-163; then S7. |

## 2. Declaration

The package implements `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). Closest delivered shapes: the Linear pusher for the drive (`elements/LinearPusher/LinearPusherElementDeclaration.cs:8`), the Ball detector for the sensors (`elements/BallDetector/BallDetectorElementDeclaration.cs:70`) and the State latch for a memory that closes a supply contact (`elements/StateLatch/StateLatchElementDeclaration.cs:57`–`:67`). `SupportedDomains = Mechanical | Electrical | Signal`. Footprint: 2 bodies, 1 moving, 1 prismatic constraint.

### Bodies, slider and drive

Exactly [EL-163 section 2](EL-163-powered-ball-diverter.md#2-declaration): the EL-162 junction frame with bridge and housing; the 0.5 kg wedge on a 0–1.2 m slider along +X; the directional, self-locking drive (3 m/s, 12 m/s², 40 N, 120 W). Electrical nodes as EL-163: `motor` (Consumer, 12 V, 24 Ω, 0 F, 400 W) and `distribution` (LogicGate, 12 V, 0.05 Ω, 0 F, 400 W). Added to the static base: two **exit hoops**, one across each lane at z = 1.3, each of two posts 0.06 × 0.9 × 0.06 on the lane walls and a lintel 1.26 × 0.06 × 0.06 at y = 0.9.

### Sensors, memories and internal wiring

| Node | Descriptor | Values |
| --- | --- | --- |
| `exit_a` | `SignalPassageDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:54`) on body 0 | local position (0.6, 0.4, 1.3), aperture axis +Z, aperture radius 0.6, rearm clearance 0.02 |
| `exit_b` | same | local position (−0.6, 0.4, 1.3) |
| `to_b` | `SignalMemoryDescriptor` (`SignalContractValues.cs:64`) | set by `exit_a`, reset by `exit_b` |
| `to_a` | `SignalMemoryDescriptor` | set by `exit_b`, reset by `exit_a` |

Internal signal edges: `exit_a.Output → to_b.Set`, `exit_a.Output → to_a.Reset`, `exit_b.Output → to_a.Set`, `exit_b.Output → to_b.Reset`. Supply graph: `PowerIn` feeds the drive consumer and two contacts; contact `cmd_b` is `SignalStateContactControl(to_b, Latched)` (`CuriousContraptions.Abstractions/SignalContactControl.cs:12`) and feeds the drive's `positive` node; contact `cmd_a` on `to_a` feeds `negative`. Events: `SimulationEventKind.Passage` for each exit node. The sensors sit 0.7 m past the end of the wedge's sweep, so a ball counts only after it has left the wedge. The rearm clearance is the Ball detector's.

### Ports and parameters

| `PortId` | Domain | Direction | Role | Local position |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | `PowerIn` | housing front face, (0, 1.13, 0.1), normal −Z |

No command input and no parameter: alternation is driven only by observed passages, and the wedge starts at lane A.

### Metadata and bindings

Category `Motion`, display name "Alternating diverter", palette order 116 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.76, 0.56, 0.32). Presentation: wedge body binding and phase indicator as EL-163; each hoop lintel flashes slate to pale green `#bdf4bd` for 0.35 s on its own `Passage` event (the detector's binding, `BallDetectorElementDeclaration.cs:18`–`:20`, selected by `SignalNode`).

### Art and solidity

As EL-163, without the two select sockets, plus:

| Node | Mesh | Class |
| --- | --- | --- |
| `Visual/Hoop_A`, `Visual/Hoop_B` | cream `#fff8e9` posts and lintel | solid, one box collider per post and lintel |

Icon: the Y with two curved arrows chasing each other over the chevron. It differs from EL-163 by the two exit hoops and the single socket.

### Behaviour and numbers

At Run both memories are clear, so the drive has no command and the wedge is self-locked at lane A. The first ball leaves through `exit_a`: that passage sets `to_b` and clears `to_a`, `cmd_b` closes and, if `PowerIn` is supplied, the wedge crosses to lane B in about 0.5 s and holds. The next ball leaves through `exit_b` and the roles swap. A passage node fires once per forward crossing and rearms only when the ball has cleared the upstream side, so a ball parked in a hoop, or rolling back through it, requests nothing further. Tick order: a passage is observed in the physics step, the memories commit with that tick, and the electrical solve of the next tick reads them (decision D7); the one-tick delay is far below the 0.5 s stroke. If balls leave both exits in the same tick each memory receives Set and Reset together and resolves clear, so the wedge holds; this is the latch's delivered rule. Balls should arrive at least 1.5 s apart (stroke plus travel to the hoop); a closer ball meets a moving wedge and is turned or blocked, never passed through.

## 3. Engine capabilities

Map row families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, StateTransaction. Map: "Actual completed passage occurrence drives alternating target state; separately supplied actuator moves physical blade."

| Capability | Status | Evidence |
| --- | --- | --- |
| Directional self-locking supplied slider | present | as EL-163: `LinearPusherTests.IndependentElectricalInputsHoldWithoutDeliveringWork` (`CuriousContraptions.tests/LinearPusherTests.cs:38`), `LosingPowerLocksTheIntermediatePositionAndRetractResumesFromIt` (`:104`) |
| Directional forward-only passage with rearm clearance | present | `SignalContractValues.cs:54`; `BallDetectorTests.PassageSensorTracker_NegativeControls_ReverseOutsideStationary` (`CuriousContraptions.tests/BallDetectorTests.cs:257`), `PassageSensorTracker_JitterAcrossPlane_DoesNotRetriggerWithoutClearance` (`:339`) |
| Memory with Set and Reset, same-tick conflict resolving clear | present | `StateLatchTests.Network_SetHoldsResetClearsAndSimultaneousRequestsResolveOff` (`CuriousContraptions.tests/StateLatchTests.cs:238`) |
| A memory closing a supply contact | present | `SignalContactControl.cs:12`; `StateLatchTests.Network_ContactClosedFollowsCommittedLatchMemoryAndCounterReach` (`StateLatchTests.cs:473`) |
| Package-internal supply edges | present | `ComposableSupplyRoutingTests.PackageInternalSupplyEdgesAdmittedByValidationAndCompileToTopology` (`CuriousContraptions.tests/ComposableSupplyRoutingTests.cs:499`) |
| Package-internal signal edges without an instantaneous cycle | present | `ComposableSignalNodesTests.PublicAliasesOfAnInternalDeliveryCountOnce` (`CuriousContraptions.tests/ComposableSignalNodesTests.cs:191`), `InternalAndPublicEdgesCannotCloseAnInstantaneousCycle` (`:502`) |
| Two passage nodes on one owner, each with its own event cue | present, untested | `ComposableSignalNodesTests.SameKindEventCuesSelectTheirExactSiblingAndReset` (`:718`) covers sibling cues; no delivered package declares two passage nodes |
| One passage output fanned out to a Set and a Reset of two memories | present, untested | admitted by `SignalTerminals.CanConnect` (`SignalContracts.cs:29`); no delivered package has an internal signal edge |
| Signal-state contacts feeding the drive's `positive` and `negative` command nodes | present, untested | `DirectionalCompositionTests.UnresolvedAndConflictingDirectionalReferencesRefuseBeforeInstallation` (`CuriousContraptions.tests/DirectionalCompositionTests.cs:84`) covers refusals only; the pusher feeds those nodes from ports |
| A slider body with two yawed box colliders | present, untested | as EL-163 |

Nothing is missing. Stale harvest rows ("directional aperture passage sensor: Story 6.8", "repeating occurrences: Story 9.2", "hinge, servo") are superseded: the sensor and counter family are delivered, and no hinge or servo is used.

Two passage sensors on one element, each with its own local id and evaluated inside the physics tick, are ENGINE-SENSOR-VOLUMES (owner decision 3a; gap audit story 4), with the one-sensor-per-element slots removed by ENGINE-CAPABILITY-INSTANCES. This part asks nothing of those stories beyond what they state.

## 4. Sources and legacy

- Requirement: "Observed completed passage requests the next supplied blade position"; outcome "Held arrival signal cannot alternate repeatedly without new passages" ([element-164](../requirements.md#element-164)).
- [todo-319](../requirements.md#todo-319): dispenser + alternating junction + counter distribute six balls into two groups of three; withholding an arrival prevents the goal.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Passage sensor declaration: frame, aperture radius, rearm clearance | `engine/ScenePassageSensorDeclaration.cs@a6c914e:L3-L4` | carry forward (data shape) | Exit sensors. |
| 2 | Accepted: outside-aperture passage disarms without counting; reverse or stationary motion publishes nothing | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L96-L107` | carry forward | Only completed forward passages count. |
| 3 | Accepted: re-enabling requires physical upstream clearance; one forward pass counts exactly once | `CuriousContraptions.tests/PhysicsPassageSensorTests.cs@a6c914e:L109-L124` | carry forward | "Held arrival cannot alternate repeatedly." |
| 4 | Explicit typed command sockets rather than a toggle | `parts/LatchPart.cs@a6c914e:L28-L33` | carry forward (typing) | Target state is a closed enum. |

No legacy alternating diverter exists.

**Files harvested:**
- `engine/ScenePassageSensorDeclaration.cs`
- `CuriousContraptions.tests/PhysicsPassageSensorTests.cs`
- `engine/physics/PhysicsServo.cs`
- `parts/LatchPart.cs` (typed command sockets)
- `parts/BasketPart.cs` (halo colour)
- Searched with no hit for `diverter`, `alternating` or `junction`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Requirement row: [element-164](../requirements.md#element-164). Planned test class `DiverterAlternatingTests`. Lane membership as in EL-162; `q` is the committed slider coordinate.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given supply and six balls released 2 s apart, when Run, then three leave by each lane, in the order A, B, A, B, A, B | `SixBallsSplitThreeAndThreeInAlternation` | A3 | counts exact; guard 30 s; the order is causal, no tick is asserted |
| A2 | Given a ball stopped inside hoop A after counting once, when 5 s pass, then the wedge stays at lane B and `exit_a` has counted once | `ABallParkedInAnExitHoopRequestsNothingMore` | A1 | q stays above 1.15 m; passed count exactly 1 |
| A3 | Given no supply, when six balls run, then all six take lane A | `WithoutSupplyEveryBallTakesTheStartingLane` | A1 | counts 6 and 0; q below 0.01 m throughout |
| A4 | Given a ball that rolls back through a hoop, then no further switch occurs | `ABallRollingBackThroughAHoopDoesNotSwitch` | a forward crossing switches | q changes by less than 0.02 m after the reverse crossing |
| A5 | Given a second ball arriving while the wedge is mid-stroke, then it is turned or held and never passes through the wedge; every ball ends in a lane or in the delta | `ABallMeetingTheMovingWedgeIsNeverPassedThrough` | 2 s spacing gives strict alternation | overlap at most 0.02 m; ball count conserved exactly |
| A6 | Given a ball pinned against the wedge, then the drive reports Blocked and completes when the ball leaves | `APinnedBallBlocksTheWedgeUntilItLeaves` | — | as EL-163 A4 |
| A7 | Reset clears both memories and returns q to 0, so the first ball of the next Run takes lane A; save and load keep the supply wire | `ResetStartsAgainAtLaneAAndSaveLoadKeepsTheWire` | — | generic rows: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`, S4 rows 1 and 3 |
| A8 | Given a Signal output offered to `PowerIn`, then the connection is refused | S4 row 5; until it lands `ASignalOutputCannotSupplyTheDiverter` | a battery `Supply` is accepted | typed refusal |
| A9 | todo-319: a ball source, this part and two Ball detectors feeding Counters with target 3 reach both counts; withholding the sixth ball leaves one Counter short; changing the route does too | `SixBallsReachTwoCountersOfThreeAndFiveDoNot` | five balls; and, with `PowerIn` unwired, six balls take one Counter to 3 and leave the other at 0 | counts exact |

No parameters, so no parameter refusal applies.

### Integration

- Supplied by CAT-005 Battery; observed by CAT-002 Ball detector and CAT-020 Counter on pipes after each lane (todo-319); all delivered. The "dispenser" of todo-319 is EL-241 Ball hopper, not delivered: A9 uses timed releases until it is, and the campaign level waits for it.
- Campaign: chapter 3 "Pipe Dreams", levels 21–30, "distribute counted arrivals" (`docs/planning/requirements.md:2288`); reuse 41–50, 111–120, 136–150 (`:2398`).
- Coverage binding: a final package-source-binding section in this spec, as EL-239 on published main; blocked on S7 (section 7).

### Delivery

- **Shape.** Single element slice after EL-163. Not classified in delivery batches; it belongs in 1b beside "Selective passage".
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SENSOR-VOLUMES, ENGINE-CAPABILITY-INSTANCES and ENGINE-SUPPLY-DEMAND (the drive's work debited from its source); then EL-162 and EL-163; then S7 (the coverage follow-on, which widens the reader, owner decision 15). S2–S4 optional.
- **Files.** `elements/DiverterAlternating/` (project, declaration, `diverter_alternating.tscn`, `icon.svg`); `CuriousContraptions.tests/DiverterAlternatingTests.cs` (compiled by the test project's one `*.cs` rule); one `CuriousContraptions.slnx` line; the binding section of section 7 once S7 admits it; until S3, the trim list and placement switch.
- **Review tier.** Engine tier: four *present, untested* compositions.
- **Disqualifiers the probe re-checks.** A passage pulse that cannot fan out to two memory terminals; a signal-state contact that cannot feed a directional command node (then the generic supply routing needs a small engine slice first, and this element waits for it); a passage node that needs a tube collar rather than a free aperture.

## 6. Open questions

All resolved.

1. **No `ActivationOut` per exit** (D19): the todo-319 Counters read Ball detectors placed after the lanes. Add outputs only when a lesson needs them.
2. **A ball arriving mid-stroke is the player's problem**: the wedge does not lock while the delta is occupied. Spacing the balls is the lesson.
3. **`initial_branch` dropped** (D13, D19), as in EL-163.
4. **Aperture and hoop positions**: starting values under D1.
5. **A9 also proves the changed route** (22 November 2026, audit M12): the requirement says "changing a route or withholding an arrival prevents the goal" (`docs/planning/requirements.md:2734`), so the unsupplied run is a second control.

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** The EL-163 frame, slider and drive, two declared passage sensors, two declared memories, four internal signal edges and two memory-closed supply contacts. The alternation is that declared network; there is no toggle, counter or state machine in code and no engine test on this package.
2. **Generic laws.** The passage-sensor rule (one pulse per forward crossing, rearm on clearance), the signal memory's set and reset rule, the signal-state contact in the supply network, and the prismatic drive law. Each is the rule the Ball detector, State latch and Linear pusher already declare.
3. **Three layers.** Colliders: twenty-four primitives. Static body: the EL-163 static set less two socket spheres (fifteen) and six hoop boxes. Wedge body: three boxes. Visuals: one mesh per collider. Animation: the phase indicator, the span-bound rod and the two lintel flashes on their own `Passage` events; all read committed state and write nothing back. `Visual/Wedge` follows body 1's pose.
4. **Flow and typing.** One typed Electrical input; sensor, memory and contact ids are typed nodes; no parameters. The passage sensors are bounded apertures (radius 0.6 m).
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 24 (boxes 22, spheres 2); joints 1 prismatic; drives 1; sensors 2; signal memories 2; internal signal edges 4; supply contacts 2 with the drive's supply nodes; electrical nodes 2; stores 0; presentation bindings 5 (body, phase colour, span, two flashes).
6. **Performance.** Twenty-four colliders, twenty-one of them static; four more than EL-163 for the two visible hoops. Two bounded sensors. Today passage crossings are tracked outside the tick over every participant; ENGINE-SENSOR-VOLUMES evaluates them inside the tick with candidates from ENGINE-SPATIAL-QUERIES. The tick order is the engine's declared phase order; the part adds none. No island sleeping exists today; a resting self-locked wedge costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
7. **Tests are scenarios.** Rows A1 to A9 run the production simulation; none needs a code path of this part.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `diverter_alternating` |
| Named identity | EL-164, `../invest/named-elements.md#element-164` |
| Acceptance | `todo-246`; no sequence task yet |
| Decision | S375, `../invest/scope-corrections.md#s375` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s375` block on published main; `todo-246` is a scope index shared by EL-162, EL-163 and EL-164 with no sequence-task anchor directly after it, which the reader requires; and one task source can bind only one package.
