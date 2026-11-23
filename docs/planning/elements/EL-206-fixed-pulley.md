# EL-206 · Fixed pulley named-identity spec

**Current delivery (24 October 2026):** [EL-206 delivery slice](../invest/named-elements.md#element-206). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-206 |
| Name | Fixed pulley |
| Type | Mechanical |
| Anchor | [requirements.md#element-206](../requirements.md#element-206); [named entry](../invest/named-elements.md#element-206); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S308 |
| Refines / extends | Refines [CAT-053 pulley](CAT-053-pulley.md) ([requirement](../requirements.md#current-cat-053)); the full harvest is in that spec |
| Related | EL-205 Rope, EL-207 Moving pulley, CAT-058 Rope anchor, CAT-067 Weight |
| Roadmap story | [EL-206 delivery slice](../invest/named-elements.md#element-206); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Behaviour delivered by package `pulley` (CAT-053; slice records 10.2c and 10.4 are `done`). Closure review not yet run; blocked on the engine programme (owner decision 3a), which comes first and re-qualifies the delivered package (relying most on ENGINE-ISLANDS-SLEEP). The ledger row stays open until the closure check below is recorded |

### Disposition (21 November 2026)

**Candidate Duplicate closure of CAT-053 Pulley; no package, palette entry or package source binding of its own.** Checked against `elements/Pulley/PulleyElementDeclaration.cs:7`–`:34` and its tests: the identity names no behaviour, parameter, port, mode or material that the delivered part lacks. The part has no parameters (`:12`), one `Tie` port of role Guide with groove radius 0.4 m and rope-plane offset (0, 0, 0.12) m (`:21`–`:23`), and one static body (`:29`). It is in the "Rope parts" closure-review group of [delivery batches 2a](../invest/delivery-batches.md#2-duplicate-closures) with EL-204 and, per its own spec, EL-205.

**Closure check (exact).** The closure review records, against the published SHA, that these tests execute the ledger entry through the `pulley` package:

| Ledger criterion | Executed proof |
| --- | --- |
| Sheave redirects a routed rope and carries its load | `RopeTests.SinglePulleyRedirectionSupportsSuspendedWeight` (`CuriousContraptions.tests/RopeTests.cs:804`) |
| Actual tangent geometry (tangent legs plus wrapped arc) | `RopeGuideTests.BoundaryWrap180DegreeTurnAndSmallDeflection` (`CuriousContraptions.tests/RopeGuideTests.cs:265`); `RopeTests.TwoPulleyAtwoodAcceleratesUnequalWeightsWithTheoreticalAtwoodDynamics` (`RopeTests.cs:721`) |
| The sheave rotates with rope travel | `RopeTests.WorkshopDeclaredPresentationAdvancesPulleyWheelRotationAngle` (`:967`) |
| Disconnected or misrouted rope gives no coupling (source outcome) | `WeightTests.Counterweight_OpenRouteFallsFreelyToBench` (`CuriousContraptions.tests/WeightTests.cs:1232`); `RopeTests.MalformedGuideTopologyBranchingCycleAndGuideEndpointsRefuse` (`RopeTests.cs:949`) |
| Balanced control | `RopeTests.TwoPulleyAtwoodBalancesEqualWeightsAtRest` (`:699`); `WeightTests.Counterweight_EqualMassesOverPulleyRemainBalanced` (`WeightTests.cs:1179`) |
| Direction of wiring does not matter | `WeightTests.Counterweight_PulleyWiringDirectionInvariance` (`:1254`) |
| Run/Reset and save/load | `RopeTests.TwoPulleyRouteSurvivesResetAndSaveLoad` (`RopeTests.cs:905`); `RopeGuideTests.ResetRestoresInitialRouteStateAndZeroGuideObservations` (`RopeGuideTests.cs:356`) |

If any row is not accepted by the reviewer as executed through the delivered package, EL-206 is not closed and the missing proof is added in that review.

## 2. Declaration

The `pulley` declaration, art and bindings are [CAT-053 section 2](CAT-053-pulley.md#2-declaration); this identity adds none (coordinator decision 27, 23 November 2026). Palette: no order of its own (not placeable as a separate part: a Duplicate closure of CAT-053's `pulley`, order 33; coordinator decision 36).

**Residuals the ledger row cites (CAT-053's, not EL-206 deltas).** From the [art-solidity contract](../../../_bmad-output/implementation-artifacts/spec-engine-art-solidity.md) (table row `pulley`): **visible-solid defect: `Visual_Post` has no collider**, and the rim's treatment is settled by [visible-solid remainder part A](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder.md) (rows A2 and A3). The closure does not wait for them and does not hide them.

The load-attached sheave is EL-207.

## 3. Engine capabilities

Families: EnvironmentState, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

### Capability probe (21 November 2026)

Every capability the identity needs is present and exercised (Story 10.2 is delivered).

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Guide port with radius and offset on a static body | present | `PortDescriptor` guide fields used at `PulleyElementDeclaration.cs:21`–`:23`; `RopeGuideDeclaration` (`CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:12`) | `RopeGuideTests.StructuralValidationRefusesInvalidGuideDeclarations` (`RopeGuideTests.cs:290`) |
| Route length through finite-radius guides | present | `engine/gpu/RopeGeometry.cs` (`EvaluateRoute`); `CuriousContraptions.Simulation/TensileConstraintSolver.cs:29`–`:42` | `RopeGuideTests.EvaluateRouteSupportsMultipleGuidesWithDistinctRadiiAndOffsets` (`:72`) |
| Guide travel and wheel-angle read, and its presentation driver | present | `PresentationDriver.CommittedWheelAngle` (`CuriousContraptions.Abstractions/ElementPresentation.cs:25`) | `RopeTests.WorkshopDeclaredPresentationAdvancesPulleyWheelRotationAngle` (`RopeTests.cs:967`) |
| Open-route and guide-endpoint refusal | present | `engine/gpu/RopeTopology.cs:9` | `RopeTests.MalformedGuideTopologyBranchingCycleAndGuideEndpointsRefuse` (`:949`) |

**Dependencies.** EL-205 rope, CAT-058 anchor, CAT-067 weight (Stories 10.2 and 10.4).

## 4. Sources and legacy

- Requirement: "Supported rotating sheave redirects a routed rope through actual tangent geometry"; outcome "Disconnected or misrouted rope receives no invisible coupling" ([element-206](../requirements.md#element-206)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Fixed, frictionless point-guide pulley; no moving block or wheel inertia | `parts/PulleyPart.cs@a6c914e:L6-L6` | carry forward (fixed, frictionless); do not carry forward (point guide) | Tangent geometry supersedes the point guide. |
| 2 | Groove radius 0.4 m; tie socket depth 0.12 m | `engine/MachineData.cs@a6c914e:L95-L98` | carry forward | Geometry. |
| 3 | A route ending at a guide is incomplete and carries no tension | `engine/RopeNetwork.cs@a6c914e:L34-L38` | carry forward | "Misrouted rope receives no coupling." |
| 4 | Accepted: two fixed pulleys between masses (1, 4), (4, 1), (1, 1): rope taut at its length ±0.001 m; the wheel turns only for unequal masses; Reset returns wheels to 0 and replays | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L21-L68` | carry forward (re-freeze tolerances) | Positive and balanced control. |

**Files harvested:**
- `parts/PulleyPart.cs`
- `parts/catalog/pulley.tres`
- `engine/MachineData.cs` (pulley geometry constants)
- `engine/RopeNetwork.cs`
- `CuriousContraptions.tests/RopeTests.cs`
- The remaining pulley files are harvested in [CAT-053](CAT-053-pulley.md#4-legacy-harvest) (Batch C). Searched with no further EL-206 knowledge: `engine/bridge/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-206](../requirements.md#element-206) and the [CAT-053 outline](CAT-053-pulley.md#5-acceptance-outline).
- **Chrome UI recipe.** Place two pulleys high and Weights of 1 kg and 4 kg below; connect weight → pulley → pulley → weight with rope links. Run.
- **Positive.** The heavy Weight descends, the light one rises, both wheels turn with rope travel.
- **Negative/control.** Equal masses stay balanced and the wheels still; a route ending at a pulley is dashed, carries nothing, and the load falls.
- **Boundaries.** Reversed link order; depth route (`pulley_depth`); invalid branch or loop refused.
- **Run/Reset.** Weights, wheel angles and span lengths restored.
- **Save/Load.** Identities, `Tie` links and lengths round-trip.
- **Integrations.** Retained rope behaviour [todo-140](../requirements.md#todo-140) and qualification [todo-141](../requirements.md#todo-141); campaign row 2 ("... fixed/moving pulley roles ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); counterweight lessons with CAT-067 and the switch-and-lamp chain (CAT-063, CAT-035).

### Acceptance rows (21 November 2026)

All rows are executed by existing tests; the closure adds none unless its reading finds a gap. Tolerances are those the tests already carry (for example the 10 % band on Atwood acceleration at `RopeTests.cs:800`, which separates a working pulley from a 1:1 fall by a factor of three).

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given an anchor, a pulley and a Weight on one complete route, when Run starts, then the Weight hangs from the pulley | `RopeTests.SinglePulleyRedirectionSupportsSuspendedWeight` | the open route falls (`WeightTests.Counterweight_OpenRouteFallsFreelyToBench`) | a 180° wrap and a small deflection (`RopeGuideTests.BoundaryWrap180DegreeTurnAndSmallDeflection`) |
| A2 | Given unequal Weights over two pulleys, when Run starts, then the heavy one descends, the light one rises and the wheels turn with the travel | `RopeTests.TwoPulleyAtwoodAcceleratesUnequalWeightsWithTheoreticalAtwoodDynamics`; `RopeTests.WorkshopDeclaredPresentationAdvancesPulleyWheelRotationAngle` | equal Weights stay still (`TwoPulleyAtwoodBalancesEqualWeightsAtRest`) | reversed link order (`Counterweight_PulleyWiringDirectionInvariance`) |
| A3 | A route that ends at a pulley, branches or loops is refused or carries nothing | `RopeTests.MalformedGuideTopologyBranchingCycleAndGuideEndpointsRefuse` | a complete route is accepted | — |
| A4 | Reset restores Weights, wheel angles and lengths; save and load round-trip the route | `RopeTests.TwoPulleyRouteSurvivesResetAndSaveLoad` | — | — |

Parameter refusal does not apply (no parameters). A wrong-kind connection on `Tie` is the same check as [EL-205](EL-205-rope.md) row A5, which the "Rope parts" closure adds.

### Integration

- **Works with.** Delivered: rope connection, `rope_anchor`, `weight`. Planned: EL-207 Moving pulley, EL-065, EL-069, EL-070.
- **Campaign.** Row 2 of [campaign element coverage](../requirements.md#campaign-element-coverage): first use 11–20. Planned authored levels 19 `counterweight` (plan level 12b, where it is a fixture) and 20 `pulley_depth` (plan level 12c, where it is first taught) place it and are provable after the engine programme ([Early level proof](../invest/delivery-batches.md#4-early-level-proof), "Ropes").
- **Coverage binding.** Catalogue key CAT-053 of the satisfying part; EL-206 has no package of its own and so no package source binding.
- **Lesson fixtures (S8).** Not needed.

### Delivery

- **Shape.** Duplicate closure in the "Rope parts" closure-review slice. Owned diff: the `element-206` ledger entry (cite `pulley` and the tests above) and the closure record.
- **Prerequisites.** Blocked on the engine programme (owner decision 3a), which comes first; the closure review then re-runs its rows on the re-qualified package.
- **Review tier.** Data-only (ledger rows and the record; no test is planned).
- **Disqualified if** the reviewer finds the wheel must be a dynamic rotor with inertia to satisfy "rotating sheave". The delivered part shows rotation from committed rope travel; a dynamic rotor would be a behaviour delta and a slice of its own.
- **The rope is a solid strand (owner decision 1).** Its collider is a property of the declared connection material (declared diameter, segment rule), compiled from declarations, and becomes solid with ENGINE-CONNECTION-MATERIALS (solid strand). The delivered rope draws a thick strand with no collider today: a recorded visible-solid defect owned by that story, not by this closure, which does not wait for it. When that story lands, its probe re-checks that the strand's collider, riding the groove, does not snag the mount, the post or the wheel.

## 6. Open questions

**Resolved.** The CAT-053 questions this spec deferred to are settled by the delivered part: wheel collider and contact material are scene and declaration data (`PulleyElementDeclaration.cs:26`); the moving block is a separate identity (EL-207, D8). Decided here under the delegation: the presentation-driven wheel satisfies "rotating sheave" (reason: the ledger outcome is about rope coupling, and a frictionless massless wheel has no gameplay effect).

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026, with the package opened (`elements/Pulley/`) and the engine projects searched for the package id and name (no hit).

1. **Declaration data only.** A static body, one Rope-domain guide port with a declared groove radius and offset, and one wheel-angle binding. No engine code names the package and there is no pulley bonus anywhere: the route does the work.
2. **Generic laws.** Guide geometry in the tension-only route constraint. Travel and load ratios follow from the route.
3. **Three layers.** Colliders: the mount and wheel-body proxies on the static body. Visuals: mount, post, wheel, rim, spoke, marker; none is a collider. Animation: the wheel group is turned by the `CommittedWheelAngle` binding of the guide port, so rim, spoke and marker are decoration moved by a binding, and the wheel body's collider is the cylinder about the same axis. Recorded visible-solid defect, owned by visible-solid remainder part A: `Visual_Post` has no collider. The rope's solid strand is the connection material's collider (ENGINE-CONNECTION-MATERIALS), not this part's.
4. **Flow and typing.** One Rope-domain `Tie` port with attachment role Guide; no parameters.
5. **Flat state.** Bodies 1 (static); colliders 2 (box 1: mount; cylinder 1: wheel); joints 0; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 1 (wheel angle).
6. **Performance.** A static body that contributes guide geometry to a rope's one constraint row. No island sleeping exists today; a resting load hung from this pulley costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
7. **Tests are scenarios.** The cited rows run the production simulation.

**Verdict: conforms with one art fix already owned (the post's collider).**
