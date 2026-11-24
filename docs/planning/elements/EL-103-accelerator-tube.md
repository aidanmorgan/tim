# EL-103 · Accelerator tube — named-identity spec

**Current delivery (24 October 2026):** [EL-103 delivery slice](../invest/named-elements.md#element-103). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`).** Sections 1–3, 5 and 6 are written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lstart-Lend` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Game values are canonical f32.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-103 · Accelerator tube · Specialist |
| Anchor | [requirements.md#element-103](../requirements.md#element-103) ([sequence-task-656](../requirements.md#sequence-task-656)); [named entry](../invest/named-elements.md#element-103); scope index [todo-227](../requirements.md#todo-227) |
| Proof owner | S363 |
| Disposition | To deliver as a single element. Not a duplicate of the Clear pipe (it adds a port, a supply consumer and a field) and not a Family member |
| Planned package | id `accelerator_tube`, directory `elements/AcceleratorTube/`, class `AcceleratorTubeElementDeclaration` |
| Refines / extends | [CAT-048 Clear pipe](CAT-048-pipe.md) for the bore, shell, collars and mouths |
| Related | EL-089 Gravity-effect pad (same engine story); EL-108 Powered airlift (airflow, a different law); EL-227 Magnetic impulse relay |
| Status | not started; blocked on the engine programme (owner decision 3a), then `ENGINE-FIELD-FORCE`, then S7 |

## 2. Declaration

A fixed-length Clear pipe that declares one supply-switched force field filling its bore. The package implements `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and the field-source interface that `ENGINE-FIELD-FORCE` adds. Closest delivered shapes: the Clear pipe for geometry (`elements/Pipe/PipeElementDeclaration.cs:13`) and the Fan's Electrical mode for the supply consumer (`elements/Fan/FanElementDeclaration.cs:72`–`:76`). `KinematicMode.Static`, `SupportedDomains = Mechanical | Electrical`. Footprint: 1 body, 0 moving, 0 constraints.

**Why a field and not airflow.** The delivered airflow source is a cone that is narrow at its origin (`CuriousContraptions.Abstractions/ConicalAirflow.cs:9`–`:15`) and acts on four samples half a radius from a ball's centre. A ball rolling through a level tube rides the wall with its centre 0.31 to 0.40 m off the axis, so its samples reach 0.48 to 0.53 m off the axis and lie inside a cone that ends at the bore radius of 0.65 m only over the last fifth of the tube. The work added would depend on how the ball happened to be turned. The requirement also says "coupling field". The Powered airlift (EL-108) can use the cone because its ball sits centred on a seat.

### Body and colliders

One static body, tube axis along local +X, length fixed at 3.6 m (the Clear pipe's default; D13). Dimensions in metres.

| Collider | Kind | Dimensions |
| --- | --- | --- |
| Shell | Tube | inner radius 0.65, outer 0.70, half-length 1.89 (reaches both mouths, as `PipeElementDeclaration.cs:9`–`:12`) |
| Collar, start and end | Torus, full sweep | centreline 0.715, tube radius 0.065, at x = ∓1.8 |
| Drive ring A, drive ring B | Torus, full sweep | centreline 0.74, tube radius 0.04 (spans 0.70 to 0.78), at x = −0.6 and x = +0.6 |
| Socket | Sphere | radius 0.06 at (0, 0.75, 0): it sits on the shell (outer radius 0.70), spanning 0.69 to 0.81 |

Material: restitution 0.15, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (`PipeElementDeclaration.cs:62`). Two mouths, `TubeEnd.Start` at x = −1.89 and `TubeEnd.End` at x = +1.89, bore radius 0.65: they snap to every delivered tube part.

### Field source (one, declared)

| Field | Value | Reason |
| --- | --- | --- |
| Frame | body 0 | |
| Region | cylinder about local X, radius 0.65, from x = −1.8 to x = +1.8 | the bore between the collars |
| Quantity | Force (N), no falloff | bounded work: force × length |
| Vector | (`force`, 0, 0) on each selected body whose centre of mass is inside | toward the End mouth |
| Selector | target selector, payload bodies (today's `BodyTargetKind.AllPayloads`, `CuriousContraptions.Abstractions/ContactWorkDeclarationValues.cs:31`) | cargo, as the Bumper and Cannon select it |
| Payment | PaidWork, power bound 120 W, through supply consumer `consumer`; enabled while its `PowerIn` route is closed | "explicit supply adds bounded work"; 120 W is the default Battery's `power_limit` (`elements/Battery/BatteryElementDeclaration.cs:44`): 1 J per tick at 120 Hz |

PaidWork, not StandingDraw: the requirement bounds the work added to cargo, and a tube with nothing in it should cost nothing.

### Port, supply and parameter

| `PortId` | Domain | Direction | Role | Local position |
| --- | --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | `PowerIn` | the socket, (0, 0.75, 0), normal +Y |

Supply graph: one `SupplyConsumerDescriptor` `consumer` whose target is the field (the target kind `ENGINE-FIELD-FORCE` adds beside `SupplyContinuousWorkTarget`, `CuriousContraptions.Abstractions/SupplyContractValues.cs:50`), bound to `PowerIn`. Electrical node: `consumer`, `ElectricalRole.Consumer`, 12 V, 1.2 Ω, 0 F, `MaxPower` 120 W; this is a declared rating that nothing reads today, and the field's own power bound is what limits the debit. No stores.

| Name | Type | Unit | Default | Min | Max | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `force` | Real | Newton | 5 | 1 | 10 | Reject | BuildOnly |

### Metadata and bindings

Category `Power`, display name "Accelerator tube", palette order 314 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.4, 0.72, 0.79). Presentation: `Visual/Ring_A` and `Visual/Ring_B` emission driven by `PresentationDriver.CommittedSupply` (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`; the Battery's terminal binding in form, `BatteryElementDeclaration.cs:12`–`:14`), slate `#556573` unsupplied to gold `#f7cb52` supplied.

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| `Visual/Visual_Shell` | clear cylinder, `#66b8c9` at alpha 0.16 | hollow, the Tube |
| `Visual/Visual_Collar_Start`, `_End` | cream `#fff8e9` tori | solid, Torus colliders |
| `Visual/Ring_A`, `Visual/Ring_B` | tori around the shell, recoloured by a binding | solid, Torus colliders (the binding changes colour only) |
| `Visual/Socket` | navy `#293954` sphere on top of the shell | solid, Sphere collider |
| `Visual/Chevrons` | three navy chevrons printed on the shell, pointing to +X | decorative: printed flat |

Icon: a pipe with two rings and a row of chevrons. It differs from the Clear pipe by the two rings and the chevrons.

### Behaviour and numbers

While supplied, each payload in the bore feels `force` along +X; friction with the wall spins it up. Work added per pass is at most `force` × 3.6 m: **18 J** at the default, 36 J at the maximum. The player reverses the tube by turning it round; both mouths are the same.

Exit speed from rest on a level tube, default 5 N. A ball rolls without slipping if the friction it needs, (2/7) × 5 = 1.43 N for a uniform sphere, is within 0.3 × m × 9.81; then 18 J = (7/10) m v².

| Ball | Mass | Friction available | Exit speed | Field power at exit |
| --- | --- | --- | --- | --- |
| Tennis ball | 0.35 kg | 1.03 N: it slips | between 8.6 m/s (rolling) and √(2 × 18 / 0.35) = 10.1 m/s (sliding) | 51 W |
| Basketball | 1 kg | 2.94 N: it rolls | √(18 / 0.7) = 5.07 m/s | 25 W |
| Bowling ball | 4 kg | 11.8 N: it rolls | √(18 / 2.8) = 2.54 m/s | 13 W |

A Tennis ball leaves faster than the Bumper's 8 m/s target; the Basketball does not. At `force` 10 the 120 W bound binds above 12 m/s, which only the Tennis ball approaches (√(72 / 0.35) = 14.3 m/s unbounded), so its last metre is power-limited. Two balls in the bore each feel `force`; if their summed power would pass 120 W, or what the supply can pay this tick, both forces are scaled down together. Unsupplied, the part is exactly a Clear pipe.

**Tick order.** Supply is resolved in the electrical phase and the field acts in the physics step of the same tick ([solver model](../../gpu-f32-physics.md#solver-model)).

## 3. Engine capabilities

Map row families: ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics (navigation only under D14; no joint).

| Capability | Status | Evidence |
| --- | --- | --- |
| Static hollow Tube with Torus collars; a ball rolls through on the wall | present | `HollowElementTests.ABallRollsThroughAStraightPipeOfEveryLengthOnItsWallWithoutCatching` (`CuriousContraptions.tests/HollowElementTests.cs:299`) |
| Mouths with bore 0.65 snap to tube parts | present | `TubeMouthSnapTests.BoresMustBeEqualWithin1EMinus6Metres` (`CuriousContraptions.tests/TubeMouthSnapTests.cs:269`) |
| A supply consumer that pays only for work delivered, from a wired Battery, and nothing when unwired | narrower: it exists for airflow work only | `SupplyContinuousWorkTarget` (`SupplyContractValues.cs:50`); `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`CuriousContraptions.tests/FanIntegrationTests.cs:540`): debit within [work, 1.01 × work], zero unwired |
| Several demands on one source scaled together within its bounds | narrower: airflow only | `ConicalAirflow.LimitSource` (`ConicalAirflow.cs:29`); `AirflowRuntimeTests.OpposingJetsUseOneCoupledVelocity` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:162`) |
| Target selection of payload bodies | present, untested here | `BodyTargetKind` (`ContactWorkDeclarationValues.cs:31`); no test body was opened for it. `ENGINE-CAPABILITY-INSTANCES` replaces the fixed kinds by participation masks |
| A supply-switched Force field acting on bodies inside a region, payment PaidWork | missing | [`ENGINE-FIELD-FORCE`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-field-force-the-one-uniform-field-law-and-declared-force-regions), in the single text below |
| Region membership candidates without visiting every body | missing | `ENGINE-SPATIAL-QUERIES` (owner decision 3a), which `ENGINE-FIELD-FORCE` takes its candidates from |
| Supply-driven emission on art nodes of a consumer | present, untested | `PresentationDriver.CommittedSupply`; the Battery binds it on a source (`BatteryElementDeclaration.cs:12`); no test body was opened for the binding |

The two *narrower* rows are widened by `ENGINE-FIELD-FORCE`: its PaidWork payment reuses the continuous-work payment and proportional scaling. `ENGINE-SUPPLY-DEMAND` (owner decision 3a) precedes every element; this part's debit is its field's paid work and nothing else.

**The story's text.** The audit of this batch (E5) replaces the five additions proposed across batches by one text, adopted here and in [EL-089](EL-089-gravity-effect-pad.md):

> A field source declares a bounded region (box or cylinder) on a frame body, or the whole scene; a selector (a responding property or a target selector); a constant vector whose quantity is a closed enum {Force (N), Acceleration (m/s²)}, optionally with falloff from a closed enum; membership by centre of mass, found through ENGINE-SPATIAL-QUERIES and waking sleepers; a payment kind from a closed enum {None, StandingDraw, PaidWork within a declared power bound, scaled proportionally}; the reaction on a dynamic frame; one work read per source. An Acceleration field is the one gravity law: a body's effective gravity is its declared gravity plus the vectors of the Acceleration fields it is inside, and every declared buoyant lift is scaled by |effective gravity| ÷ |declared gravity| and directed against the effective gravity.

This tube declares: cylinder region, target selector, Force, no falloff, payment PaidWork with a 120 W bound. Its acceptance needs from the story: the same body crossing the same region supplied gains kinetic energy of at most force × path length and the supply is debited at least that much; unsupplied it gains none; two bodies inside share one bound.

## 4. Sources and legacy

- Requirement: "Explicit supply adds bounded work through a declared coupling field"; outcome "Unpowered tube cannot increase cargo energy" ([element-103](../requirements.md#element-103)).
- Legacy search at `a6c914e` for "accelerator" and "booster" found nothing.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | Actuator work is bounded by available work; dissipated energy is never reused | `engine/physics/PoweredImpulse.cs@a6c914e:L7-L26` | Carry forward |
| L2 | Clear pipe: gravity carries balls through the open bore | `parts/catalog/pipe.tres@a6c914e:L17-L17` | Carry forward: unsupplied, the tube behaves as a Clear pipe |
| L3 | Jet force falls to zero at the flow speed | `engine/physics/JetTransferImpedance.cs@a6c914e:L26-L46` | Do not carry forward for this part: the field is a constant force bounded by power, not a slip law |

**Files harvested:** `engine/physics/JetTransferImpedance.cs`, `engine/physics/PoweredImpulse.cs`, `parts/catalog/pipe.tres`.

## 5. Acceptance outline

Requirement row: [element-103](../requirements.md#element-103). Planned test class `AcceleratorTubeTests`. The tube is level; "exit speed" is the ball's speed along the axis as its centre crosses the End mouth plane; balls enter rolling at 1 m/s from a level Clear pipe snapped to the Start mouth. All rows fit the default 30 s Run.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given a Battery on `PowerIn` and a Basketball entering, then it leaves faster | `ASuppliedTubeSpeedsTheBallUp` | A2 | exit speed 5.2 m/s within 0.6 (0.7 J + 18 J = 0.7 v² gives 5.17); 10 s stall guard |
| A2 | Given the same tube with no wire, then the ball leaves no faster than it entered | `AnUnpoweredTubeAddsNoEnergy` | A1 | exit kinetic energy at most entry kinetic energy + 0.05 J; this is the requirement's outcome |
| A3 | Given A1, then the ball's kinetic energy gain is at most `force` × 3.6 J and the Battery's debit is at least the gain | `WorkIsBoundedAndPaid` | — | gain ≤ 18 J × 1.02; debit within [gain, 18 J × 1.05] |
| A4 | Given a Tennis ball and a Bowling ball in turn, then the lighter leaves faster and both gains respect the same bound | `TheSameWorkMovesLightCargoFaster` (theory) | — | Tennis 8.0 to 10.5 m/s; Bowling 2.7 m/s within 0.4 (2.8 J + 18 J = 2.8 v²) |
| A5 | Given a Basketball resting on the bench beside the tube, 0.9 m from the axis, supplied, then it does not move | `ABodyOutsideTheBoreIsNotPushed` | A1 | speed stays under 0.05 m/s for 3 s |
| A6 | Given `force` 10 and a Tennis ball, then the supply is never debited more than 1 J in a tick and the ball still leaves | `ThePowerBoundLimitsTheFastestCargo` | — | no tick debits more than the 1 J allowance (power_limit × dt) plus the ledger residual; exit speed under 14.3 m/s |
| A7 | Given the Battery disabled mid-pass, then the ball gains nothing more | `LosingSupplyStopsTheFieldAtOnce` | A1 | speed 0.5 s after the loss is not above the speed at the loss + 0.1 m/s |
| A8 | Given a supplied tube with nothing in its bore for 5 s, then nothing is debited | `AnEmptyTubeCostsNothing` | A3 | debit exactly zero (a ledger fact) |
| A9 | Given a Signal output offered to `PowerIn`, then the connection is refused | `ASignalCannotPowerTheTube` | a Battery `Supply` is accepted | typed refusal; generic conformance row when S4 lands |
| A10 | `force` 0.9 and 10.1 are refused; Reset restores balls and the Battery; save and load keep `force`, pose and the wire | `ForceIsValidatedAndStateRoundTrips` | 1 and 10 accepted | generic: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |

### Integration

- Works with: Battery and every switched `Supply` output (Impact switch, State latch, Pressure plate, Counter, Hold timer); Clear pipe, both bends, Funnel, Powered gate by mouth; all balls; Bumper and Receiver downstream. All delivered.
- Campaign: no chapter row named it; owner decision 24c gives it new level **48a** *Up to Speed* (chapter 5): introduction 48a, practice 50, reuse 97 and 148 ([campaign coverage additions](../requirements.md#campaign-coverage-additions); brief in [campaign coverage](readiness/campaign-coverage.md#4-lesson-briefs)). A level that fixes one needs the lesson-fixture story (S8).
- Coverage binding: section 7.

### Delivery

- **Shape.** Single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a; of its stories this part leans on `ENGINE-SPATIAL-QUERIES` for membership candidates and `ENGINE-SUPPLY-DEMAND` for the debit); then `ENGINE-FIELD-FORCE` in the text of section 3; then S7.
- **Form.** Port roles, node arity, target masks and mouth identities are written in the form `ENGINE-CAPABILITY-INSTANCES` leaves; the names here are today's (`AllPayloads`, `TubeEnd`, `PowerIn`).
- **Files.** `elements/AcceleratorTube/` (project, declaration, `accelerator_tube.tscn`, `icon.svg`); `CuriousContraptions.tests/AcceleratorTubeTests.cs`; one `CuriousContraptions.slnx` line; until S3, the remaining per-package lists.
- **Review tier.** Data-only if the engine story publishes every missing row with tests and the two *present, untested* rows gain tests there; otherwise engine.
- **Disqualifiers the probe re-checks.** `ENGINE-FIELD-FORCE` delivered with property-only selection, without a cylinder region, or without PaidWork; membership found by a pass over every body.

## 6. Open questions

All resolved.

1. **A constant force bounded by power**, not a target speed with a slip law: "bounded work" is then force × length, which a player can reason about. `target_speed` and `power_rating` are dropped (D19); the power bound is the fixed 120 W.
2. **Payment is PaidWork** (the story's enum; the audit's owner item 8 asks only that the enum exist, and its recommended default is that it does).
3. **No `direction` parameter** (D19): the mouths are identical and the player turns the part.
4. **Fixed length 3.6 m** (D13), so the work budget is one number.
5. **Starting values** (D1): `force` 1 to 10 N, default 5.
6. **Region is the whole bore** (a cylinder), so cargo of any admitted radius that fits is pushed wherever it rides.

Needs the owner: none for this part.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`) and owner decision 3a and coordinator decision 16.

1. **Declaration data only.** A static tube, one field source, one supply consumer, one port, one parameter, bindings. Nothing sets a velocity or moves cargo by rule.
2. **Generic laws.** The one field law with the Force quantity and PaidWork payment, enabled by the supply-network rule; contact and friction in the bore. Missing general capability with one owner: `ENGINE-FIELD-FORCE`.
3. **Three layers.** Colliders: Tube 1, Torus 4, Sphere 1. Visuals: one mesh per collider; chevrons printed flat. Animation: emission bindings on the committed supply state, read-only. No art node moves.
4. **Flow and typing.** One typed Electrical input; quantity, payment, target set and `ElectricalRole` are closed enums; one Real parameter in newtons.
5. **Flat state.** Bodies 1 (static); colliders 6 (Tube 1, Torus 4, Sphere 1); joints 0; drives 0; sensors 0; field sources 1 (one work read); domain nodes: 1 supply consumer, 1 electrical node; stores 0; presentation bindings 6 (three emission channels on each ring, as the Battery declares one per channel).
6. **Performance.** Three colliders more than the Clear pipe: two visible rings and the visible socket, each a visible solid. One bounded cylinder region whose candidates come from `ENGINE-SPATIAL-QUERIES`; no state per source and body. Fast cargo (up to 14 m/s, 0.12 m per tick against a 0.25 m radius, inside the 64 m/s linear envelope) is kept in by the engine's general contact handling; no per-element check.
7. **Tests are scenarios.** Every row runs the production simulation with a delivered Battery and delivered balls.

**Verdict: conforms** (general capabilities missing, each with one owning story).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here, because the reader parses every spec that carries that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `accelerator_tube` |
| Named identity | EL-103, `../invest/named-elements.md#element-103` |
| Acceptance | `todo-227`, `sequence-task-656` (`docs/planning/requirements.md:5221`) |
| Decision | S363, `../invest/scope-corrections.md#s363` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s363` block; and `todo-227` is a scope index shared by EL-097 to EL-106 that is not anchored beside `sequence-task-656`, which the reader's task-source rule requires. The sequence task itself exists.
