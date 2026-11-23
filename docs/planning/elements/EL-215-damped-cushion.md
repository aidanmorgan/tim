# EL-215 · Damped cushion named-identity spec

**Current delivery (24 October 2026):** [EL-215 delivery slice](../invest/named-elements.md#element-215). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-215 |
| Name | Damped cushion |
| Type | Mechanical |
| Anchor | [requirements.md#element-215](../requirements.md#element-215); [named entry](../invest/named-elements.md#element-215); campaign 1–10, practice 41–50, reuse 96 and 149 ([campaign reservations](../invest/campaign-reservations.md)) |
| Proof owner | S381 |
| Refines / extends | No CAT spec. Same compliant-surface law as [CAT-065 trampoline](CAT-065-trampoline.md), tuned to dissipate rather than return energy |
| Related | EL-216 Capture cradle (same campaign slots), EL-194 Springboard, CAT-065 Trampoline |
| Roadmap story | [EL-215 delivery slice](../invest/named-elements.md#element-215); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (compliant-surface candidates) and ENGINE-ISLANDS-SLEEP; then S7. |

### Disposition (21 November 2026)

**To deliver as a single element** (the "Compliant surface" run of [delivery batches 1b](../invest/delivery-batches.md#1b-runs-each-its-own-slice-planning-reference-only)). It is not a variant of the Trampoline: same law, different frame, stroke and purpose, and its own catalogue entry (D8). Planned package id `cushion` (kept from this spec), directory `elements/Cushion/`, bound by this spec's final package-source-binding section once S7 widens that binding's preconditions (section 7); no ledger line binds it. Owner key S381. The 1b probe question, "whether the 0.8 damping-ratio ceiling dissipates rather than returns", is answered in section 3: it does.

## 2. Declaration

### Declaration to write (21 November 2026)

Closest delivered shape, to be followed line for line: `elements/Trampoline/TrampolineElementDeclaration.cs`.

- **Interfaces.** `ICompliantSurfaceElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:75`) and `ISceneGeometryElement`. Static (`KinematicMode.Static`), zero mass frame, no ports, no events, Mechanical domain.
- **Body and colliders (one static body, scene-owned).** Origin at the centre of the base plate's top face.
  - base plate: box 1.4 × 0.12 × 1.2 m, top face at y = 0. It is the stroke floor;
  - four rim walls 0.10 m thick and 0.20 m tall standing on the plate, tops at y = 0.20 (the rest plane), inner faces on the footprint edges: two of 1.4 m along X and two of 1.0 m along Z.
- **Compliant surface.** `CompliantSurfaceDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:228`): half extents (0.6, 0.5) m; rest height 0.20 m; maximum stroke 0.20 m; arrival depth 0.09 m (the contract requires it under half the stroke, `PhysicsDeclarationValues.cs:87`; 0.09 m is above the 0.046 m the Trampoline's tests measure for a landing, and with the closed rim nothing can enter under the pad from the side); tension from `stiffness`; damping ratio from `damping_ratio`.
- **Closed rim (difference from the Trampoline).** The Trampoline has open sides and derives its arrival depth from that opening. The cushion's rim is closed all round, so no body can enter under the pad from the side; the arrival depth only separates a landing from a body authored inside the pad.
- **Material.** Base and rim: restitution 0.4, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. Reason for 0.4 where the Trampoline frame has 0: a bottomed-out impact must be able to rebound from the hard base, so arrest is not guaranteed (the requirement's outcome). The pad itself is the normal-only surface law and has no friction.
- **Parameters.**

  | Name | Type | Unit | Default | Min | Max | Editor policy | Run edit |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | `stiffness` | Real (f32) | `NewtonPerMetre` | 300 | 120 | 1200 | `Reject` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:39`), as the Trampoline's `tension` | BuildOnly |
  | `damping_ratio` | Real (f32) | `None` | 0.8 | 0.5 | 0.8 | `Reject`, as the Trampoline's | BuildOnly |

  **Range:** the compliant-surface contract admits 0.08 to 0.8 only (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:90`; bundle check at `CuriousContraptions.Bundle/BundleValidator.cs:707`), so those values would be refused. 0.8 is enough (section 5, numbers) and needs no engine change. The floor of 0.5 keeps every setting a cushion: at 0.5 a 1 kg ball still does not rebound from a 1 m drop.
- **Stores.** None declared. The elastic energy is the surface's own state; there is no heat store (decision in section 6).
- **Presentation.** One binding, the Trampoline's: `Visual/Visual_Pad`, `LocalY`, driver `CommittedSurfaceDeflection`, from 0.19 to 0.19 − (0.20 − 0.001)/0.20 per metre of deflection, linear. Pick radius 1.0 m.
- **Catalogue.** Id `cushion`, title "Damped cushion", description "A soft pad that soaks up a landing instead of returning it.", category `ElementCategory.Motion`, palette order 104 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour Domino cream `#e8d4a6`.
- **Footprint.** Bodies 1, moving bodies 0.

### Art and solidity

| Node | Mesh | Class | Collider or reason |
| --- | --- | --- | --- |
| `Visual/Visual_Base` | navy `#293954` box 1.4 × 0.12 × 1.2 m | solid | base plate |
| `Visual/Visual_RimN`, `RimS`, `RimE`, `RimW` | cream `#fff8e9` boxes 0.10 m thick, 0.20 m tall | solid | the four rim walls |
| `Visual/Visual_Pad` | `#e8d4a6` slab 1.2 × 0.02 × 1.0 m, top face on the rest plane, with a quilted grid of stitched lines drawn flush | decorative | moved by a binding (the deflection binding); the load-bearing surface is the declared compliant surface, as the Trampoline's `Visual_Bed` |

No warm tint on absorption: the baseline's cosmetic tint needs a heat read that does not exist. Icon: a low tray with a quilted pad and a ball sunk into it. It differs from the Trampoline by shape: a closed low rim with no corner posts or open sides, and the quilt pattern.

### Harvest-era subsections — removed; the text is in git history.

## 3. Engine capabilities

Families: ContactImpulse, ElasticStorage, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

### Capability probe (21 November 2026)

Story 10.5 delivered the compliant surface. Every row was checked by opening the symbol and the test.

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Declared compliant surface on a static frame (footprint, rest height, stroke, arrival depth, tension, damping ratio from parameters) | present | `ICompliantSurfaceElement`; `CompliantSurfaceDescriptor` (`MechanicalContractValues.cs:228`); compiled at `engine/gpu/WorkshopPhysicsCompiler.cs:278` | `TrampolineTests.TheTrampolineIsAStaticFrameCarryingOneDeclaredCompliantSurface` (`CuriousContraptions.tests/TrampolineTests.cs:224`) |
| Unilateral spring-damper that never pulls, damping scaled by √(k·m) | present | `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:1418` | `TrampolineTests.EqualArrivalDropsReboundByTheClampedLawsFractionAndMoreDampingReboundsLower` (`:1803`) |
| Damping ratio up to 0.8 and tension 120 to 1200 N/m admitted; outside refused | present | `PhysicsDeclarationValues.cs:89`–`:90` | `TrampolineTests.AnOutOfRangeOrNonFiniteParameterIsRefusedAtEverySeamAndChangesNothing` (`:377`) |
| Bottoming out on the stroke floor without energy gain | present | the base collider as stroke floor (`TrampolineElementDeclaration.cs:33`–`:34`) | `TrampolineTests.AnOverloadBottomsOutOnTheBackPlateWithoutAddingEnergy` (`:1826`) |
| Normal-only response: a grazing body is not thrown or wedged | present | same law | `TrampolineTests.AGrazingBallReboundsAlongTheNormalWithoutThrowOrWedging` (`:723`) |
| A body authored inside the pad is never launched | present | arrival depth (`PhysicsDeclarationValues.cs:87`) | `TrampolineTests.ABodyAuthoredInsideTheBedIsNeverLaunched` (`:1012`) |
| Committed deflection read and its `LocalY` binding | present | `PresentationDriver.CommittedSurfaceDeflection` (`CuriousContraptions.Abstractions/ElementPresentation.cs:27`) | `TrampolineTests.TheCommittedDeflectionFollowsTheLoadAndTheBedVisualSagsByIt` (`:630`) |
| Reset mid-contact and save round trip | present | — | `TrampolineTests.ResetMidContactRestoresTheAuthoredStateAndTheReplayRepeatsTheRun` (`:2197`); `TrampolineTests.SaveRoundTripsPoseRotationTensionAndDampingExactly` (`:2226`) |
| A heat entry in a finite ledger for the dissipated work | not required now | none exists; see section 6 | ENGINE-THERMAL would own it if a thermal row ever names the cushion |

Counts: 8 present, 0 present-untested, 0 narrower, 0 missing. The cushion is declarations, art, one binding and tests. One caveat: the delivered tests simulate damping ratios up to 0.5; 0.8 is admitted by the contract but simulated by no delivered test, so A1 at the default is the first run of that value.

Recorded for the engine owners, not a prerequisite here: the 0.08 to 0.8 damping range and the 120 to 1200 N/m tension range sit in shared admission (`PhysicsDeclarationValues.cs:89`–`:90`) although they read as the Trampoline's product range. The design guide (D3) allows shared admission only the reusable contract's own validity limits. Owner: `ENGINE-COMPLIANT-FRICTION` (stability-bounded admission, per-sample deflection read and sag binding; coordinator decision 29). This spec stays inside the present range, so it does not depend on the answer; its slice re-checks A7's bounds against that story's admission once it lands.

**Dependencies.** Story 10.5 compliant surface.

## 4. Sources and legacy

- Requirement: "A finite compressible dissipative material surface absorbs impact work through the shared contact/deformation model"; outcome "Grazing or bottomed-out impact does not guarantee arrest; deformation/heat loss balances work" ([element-215](../requirements.md#element-215)). Campaign row lists "cushion/cradle" in the first block ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Compliant surface: frame, half X/Z, rest height, maximum stroke, stiffness, damping ratio, initial state; finite positive values | `engine/SceneCompliantSurface.cs@a6c914e:L9-L22` | carry forward (declaration shape) | The shared deformation model. |
| 2 | Force law: interval-mean spring force minus damping 2ζ√(k/m_eff)·v, clamped at zero | `engine/physics/CompliantContactLoad.cs@a6c914e:L42-L66` | carry forward (law), do not carry forward (CPU wrench evaluation) | Dissipation with no pull. |
| 3 | Engagement only from above, moving down, inside the footprint | `engine/physics/CompliantContactState.cs@a6c914e:L50-L68` | carry forward | Grazing and side entry do not engage. |
| 4 | Accepted: outside, separated and fast-separating contacts cannot attract | `CuriousContraptions.tests/CompliantContactTests.cs@a6c914e:L50-L66` | carry forward | No pull. |
| 5 | Accepted: compression work matches the potential across unloaded and saturated boundaries | `CuriousContraptions.tests/CompliantContactTests.cs@a6c914e:L67-L87` | carry forward | Energy balance at bottoming. |
| 6 | Accepted: a body falling onto the surface goes Ready → Engaged once, with one entry record (approach 1 m/s near 0.2 s); after it leaves upward the contact is Ready again and no new entry is recorded | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L32-L57` | carry forward (behaviour), do not carry forward (capture/restore replay, `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L46-L52`) | One engagement per real entry. |
| 7 | Accepted: removing and re-adding the declaration does not rearm a body already embedded below the rest height; it stays Unarmed | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L59-L75` | carry forward | An embedded body gets no spring force. |
| 8 | Accepted: while the body or the frame does not collide, the contact stays Unarmed, the body falls at unchanged speed and no entry is recorded; re-enabling it while embedded leaves it Unarmed | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L77-L94` | carry forward | A non-colliding body is never engaged. |
| 9 | Accepted: a Preloaded initial state pushes the body up at once but records no entry ("Preload is initial energy, not a new impact"); a preload must be declared explicitly | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L96-L107` | carry forward (rule); the cushion uses Unloaded | Preload is not an impact event. |
| 10 | Accepted: duplicate declarations and a preload on a changed initial condition reject atomically, leaving loads and contact states unchanged | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L109-L120` | carry forward (rejection) | Admission. |
| 11 | A failed step restores contact history, bodies and clock (exception-driven rollback) | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L122-L140` | do not carry forward | CPU transactional rollback; the worker never faults a tick. |

**Files harvested:**
- `parts/TrampolinePart.cs` (compliant-surface family values and membrane art)
- `engine/SceneCompliantSurface.cs`
- `engine/physics/CompliantContactLoad.cs`
- `engine/physics/CompliantContactState.cs`
- `CuriousContraptions.tests/CompliantContactTests.cs`
- `CuriousContraptions.tests/CompliantContactStateTests.cs`
- Searched with no hit for `cushion`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-215](../requirements.md#element-215) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place the cushion on the bench and a Basketball 1 m above it; Run. Repeat with a Bowling ball and with a ball rolled across the pad.
- **Positive.** The Basketball lands and stops on the pad with no visible rebound.
- **Negative/control.** A rolling or grazing ball crosses without arrest; a Bowling ball bottoms out and rebounds off the base; a trampoline at the same spot returns the ball; a ball placed already inside the pad volume gets no spring push; a ball passing a non-colliding cushion falls through unchanged.
- **Boundaries.** `stiffness` and `damping_ratio` endpoints; drop at the footprint edge; ledger shows heat equal to lost mechanical energy; a ball that bounces off the base and lands again counts as a new entry.
- **Run/Reset.** Pad uncompressed; heat ledger and contact phases cleared.
- **Save/Load.** Parameters and pose round-trip.
- **Integrations.** Element reservations 1–10, practice 41–50, reuse 96 and 149 ([element-215](../requirements.md#element-215)); campaign row 1 ("cushion/cradle") first use 1–10, reuse 21–30, 41–50, 91–100, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); paired with EL-216 capture cradle and CAT-065 trampoline.

### Behaviour and numbers (21 November 2026)

- **Law.** Per body over the footprint: normal force = max(0, k·d − c·v) with c = 2·ζ·√(k·m), d the compression and v its rate; never a pull. The clamp matters: on the way out the pad stops pushing as soon as the damper would have to pull, so at high ζ the body is simply let go at rest.
- **Estimates at the defaults (k = 300 N/m, ζ = 0.8).** They were computed for this spec from the law above for a point mass; the engine samples the surface at several points, so the slice treats them as expectations with the wide bounds of the acceptance rows, never as goldens.

  | Cargo and drop | Peak compression | Outcome |
  | --- | --- | --- |
  | Tennis ball, 0.35 kg, 1 m | about 0.07 m | arrested, no rebound |
  | Basketball, 1 kg, 1 m | about 0.12 m | arrested, no rebound; rests 0.033 m into the pad |
  | Basketball, 1 kg, 3 m | about 0.20 m | at the stroke limit |
  | Bowling ball, 4 kg, 0.5 m | 0.20 m (full stroke) | reaches the base at about 0.4 m/s |
  | Bowling ball, 4 kg, 1 m | 0.20 m (full stroke) | reaches the base at about 1.7 m/s and rebounds from it |

  For contrast the Trampoline at its default ζ = 0.12 returns roughly 0.4 of a 1 m drop. At the cushion's minimum ζ = 0.5 the Basketball is still arrested and the Tennis ball hops about 3 cm.
- **Why these values.** The cushion's lesson is "a soft landing for light cargo, not for heavy cargo": the Basketball and the Bowling ball, both delivered, fall on opposite sides of the 0.20 m stroke at a 1 m drop. A Bowling ball placed gently rests 0.13 m into the pad without touching the base.
- **Fit.** The 1.2 × 1.0 m pad takes the 0.68 m Basketball with margin and is narrower than the pipe's bore, 1.3 m across, so a pipe can drop cargo onto it.
- **Energy.** Kinetic plus gravitational plus elastic (½·k·d² from the committed deflection) never increases; what it loses is the damper's work. No heat is stored anywhere.

### Acceptance rows (21 November 2026)

Test class `CushionTests`, built like `TrampolineTests` on the Workshop integration harness. Tolerances: "no rebound" is an apex after first contact less than 0.05 m above the settled height (a player sees a 5 cm hop as a dead landing; the Trampoline control returns over 0.3 m); "reaches the base" is a lowest point within 0.01 m of the stroke floor, the bound the Trampoline's bottoming test uses; "does not reach" is at least 0.03 m above it; the energy envelope is the Trampoline suite's constant. Stall guards 8 game seconds.

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given a Basketball 1 m above the pad, when it lands, then it compresses the pad, does not rebound and settles; at the defaults, at `damping_ratio` 0.5 (stiffness 300) and at `stiffness` 1200 it is arrested clear of the base | `CushionTests.ADroppedBasketballIsArrestedWithoutRebound` | the same drop on a Trampoline rebounds more than 0.3 m | at `stiffness` 120 it does not rebound above 0.05 m; reaching the base is allowed there (by hand a 1 kg ball from 1 m uses the whole 0.20 m stroke at 120 N/m) |
| A2 | Given a Bowling ball 1 m above the pad, when it lands, then it runs the full stroke, strikes the base and leaves it moving upward | `CushionTests.AHeavyDropBottomsOutAndIsNotArrested` | the Basketball of A1 does not reach the base | a Bowling ball lowered onto the pad from 0.01 m rests on the pad, off the base |
| A3 | Given a Basketball arriving at 3 m/s along the pad and 1 m/s down, when it crosses, then it leaves the footprint with at least half its horizontal speed | `CushionTests.AGrazingBallIsNotArrested` | the vertical drop of A1 is arrested | a drop on a rim wall's top is met by the rigid rim and the pad stays undeflected |
| A4 | Over A1 and A2, total mechanical energy including the pad's elastic term never exceeds its starting value by more than the envelope; and for A1 at rest the mechanical energy lost equals m·g·(drop + static compression) − ½·k·(m·g/k)² within the suite's energy envelope (9.81·1.033 − 0.16 = 9.97 J for the Basketball at 300 N/m) | `CushionTests.NoTickGainsEnergyAndTheLandingLosesWhatTheBalanceSays` | — | 0.35 kg and 4 kg |
| A5 | A ball authored inside the pad volume is not pushed out | covered by the shared law (`TrampolineTests.ABodyAuthoredInsideTheBedIsNeverLaunched`); one cushion case `CushionTests.ABallAuthoredInThePadIsNotLaunched` because the stroke and arrival depth differ | — | — |
| A6 | Reset mid-compression restores the flat pad and the cargo; save and load round-trip pose and both parameters | `CushionTests.ResetAndSaveLoadRestoreTheCushion` | — | — |
| A7 | `stiffness` outside 120 to 1200, `damping_ratio` outside 0.5 to 0.8, or non-finite, is refused atomically | conformance suite row 2 (S4) once it lands; until then `CushionTests.OutOfRangeParametersAreRefused` | — | each bound is accepted |

No wrong-kind connection row: the part has no ports. Requirement rows preserved: "absorbs impact work" (A1, A4), "grazing or bottomed-out impact does not guarantee arrest" (A2, A3), "deformation/heat loss balances work" (A4, as an energy balance; see section 6).

### Integration

- **Works with.** Delivered: every ball, `weight`, `ramp`, `pipe` and bends, `trampoline` (the contrast lesson), `basket`. Planned: EL-216 Capture cradle (same campaign slots).
- **Campaign.** Row 1 of [campaign element coverage](../requirements.md#campaign-element-coverage) ("cushion/cradle"): first use 1–10, reuse 21–30, 41–50, 91–100, 136–150; the element's own reservations 1–10, practice 41–50, reuse 96 and 149. No level in `content/puzzles.json` places it today.
- **Coverage binding.** A final package-source-binding section in this spec, as EL-239 on published main; blocked on S7 (section 7).
- **Lesson fixtures (S8).** Not needed for the part; a lesson that locks a cushion under a drop uses it as an ordinary locked placement.

### Delivery

- **Shape.** Single element slice; no specific engine story beyond the engine programme (owner decision 3a).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES (the compliant surface's candidates) and ENGINE-ISLANDS-SLEEP; then S7 (the coverage follow-on, which widens the reader, owner decision 15). Preferably after S3 and S4, which remove hand lists and the hand-written baseline tests (A7 and half of A6).
- **Files.** `elements/Cushion/` (`Cushion.csproj`, `CushionElementDeclaration.cs`, `cushion.tscn`, `icon.svg`), `CuriousContraptions.tests/CushionTests.cs`, one line in `CuriousContraptions.slnx`, the art classification once ENGINE-ART-SOLIDITY lands, and the binding section of section 7 once S7 admits it (the test project compiles every top-level `*.cs` by one rule).
- **Review tier.** Data-only: every capability is present with a cited test and the owned diff is on the allow-list.
- **Disqualified if** the slice's probe finds the sampled surface law arrests the Bowling ball from 1 m at the defaults (then lower the stroke, not the law), finds that the Basketball rebounds visibly at ζ 0.8, or needs a damping ratio above 0.8: that last case is an engine slice on the compliant-surface contract's domain first, and the tier becomes engine.

## 6. Open questions

**Resolved under the delegation.**

1. Tangential friction on the pad: none. Reason: the delivered law is normal-only, and the requirement says a grazing impact must not be guaranteed arrest.
2. Does dissipated heat feed the thermal model: not now. "Deformation/heat loss balances work" is proven as an energy balance (A4). No thermal requirement row names the cushion; if one later does, it is a named later-partner criterion under ENGINE-THERMAL and this identity's row gains that test then. Reason: a heat store with no consumer would be a placeholder.
3. Damping range: 0.5 to 0.8, default 0.8, inside the delivered contract (correction in section 2).
4. All proposed sizes and values are starting values (D1); art and colour follow the proposal (D9); parameters are player-editable unless a level locks them (D11).
5. A4 asserts the balance, not only "never gains, ends lower" (22 November 2026, audit M24): the requirement says the loss balances the work (`docs/planning/requirements.md:6125`).

**Needs the owner.** None.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One static body, one declared compliant surface, two parameters and one deflection binding, following the Trampoline's declaration line for line. No solver branch, callback or engine test on this package.
2. **Generic laws.** The compliant-surface law (normal-only, declared tension and damping ratio, bounded stroke) and rigid contact with the base and rim. "Arrests a landing" is that law at a high damping ratio; "does not arrest a heavy or grazing one" is the same law running out of stroke or having no tangential term.
3. **Three layers.** Colliders: five boxes on the static body (base plate, four rim walls) and the declared compliant surface, which is a physics entity and not art. Visuals: base, rim and pad meshes; none is a collider. Base and rim are visible solids with their boxes; the pad is decorative over the declared surface. Animation: `Visual/Visual_Pad` is lowered by the `CommittedSurfaceDeflection` binding; it is decorative, reads the committed deflection and writes nothing back. The surface that bears the load is the declared one.
4. **Flow and typing.** No ports; two f32 Real parameters with declared units and `Reject` policy.
5. **Flat state.** Bodies 1 (static); colliders 5 (boxes); compliant surfaces 1; joints 0; drives 0; sensors 0; domain nodes 0; stores 0; presentation bindings 1 (deflection).
6. **Performance.** Five static boxes and one bounded rectangular surface, as the Trampoline. Nothing is cosmetic. Recorded for the engine owners, not designed around: the compliant surface's admitted ranges (0.08 to 0.8 damping, 120 to 1200 N/m) sit in shared admission and read as one part's product range, which design guide D3 does not allow (batch summary). Today the compliant surface tests every dynamic body against every collider at its samples, twice per substep, with engagement state kept for every surface and every dynamic body; ENGINE-SPATIAL-QUERIES supplies its candidates. The pad has no tangential friction (section 6), so ENGINE-COMPLIANT-FRICTION's friction term is not needed; that story's stability-bounded admission (coordinator decision 29) owns the shared-range point above. No island sleeping exists today; a resting ball on the pad costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
7. **Tests are scenarios.** Rows A1 to A7 run the production simulation and prove the shared law for this declaration.

**Verdict: conforms.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball on published main). The section is not written here: the reader parses every spec that carries that heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.** S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

| Field | Planned value |
| --- | --- |
| Package | `cushion` |
| Named identity | EL-215, `../invest/named-elements.md#element-215` |
| Acceptance | none yet: the requirement row `element-215` has no todo or sequence-task anchor |
| Decision | S381, `../invest/scope-corrections.md#s381` |
| Mode records | `Configuration=Adjustable` (`stiffness`, `damping_ratio`) |

What it lacks today: `scope-corrections.md` has no `s381` block on published main; and neither the requirement row (`docs/planning/requirements.md:6123`) nor the ledger entry has a `todo-` anchor with a co-anchored sequence task, which the reader requires.
