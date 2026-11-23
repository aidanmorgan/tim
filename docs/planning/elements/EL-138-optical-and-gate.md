# EL-138 · Optical AND gate — named-identity spec

**Current delivery (24 October 2026):** [EL-138 delivery slice](../invest/named-elements.md#element-138). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. Local +X is the outlet direction. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here.

The five optical gates (EL-138 to EL-142) share one body, one aperture layout and one control law. They differ only in the declared boolean operation, catalogue entry, truth print and icon. Their declarations are CAT-043 to CAT-047's (coordinator decision 27); this spec leads the family's probe, sources and conformance sections.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-138 |
| Name | Optical AND gate |
| Type | Optical |
| Anchor | [requirements.md#element-138](../requirements.md#element-138); named entry [named-elements.md#element-138](../invest/named-elements.md#element-138); source record [todo-367](../requirements.md#todo-367) |
| Owner | S510 |
| Disposition | Candidate **Duplicate closure** of [CAT-043 optical_and](CAT-043-optical_and.md) (`optical_and`, "Both light gate"), closed inside that part's slice ([delivery-batches §2b](../invest/delivery-batches.md#2-duplicate-closures)). No delta: this identity adds no behaviour, parameter, port, mode or material to CAT-043. |
| Acceptance authority | The CAT-043 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "prove every input combination plus absent carrier; logically true cannot create power", is CAT-043 rows **G1 and G2**; the ledger row cites the package and those two tests. This spec carries no rows of its own. |
| Planned package | none of its own; the package is CAT-043's `optical_and` (`elements/OpticalAnd/`). The CAT spec is the single declaration (coordinator decision 27, 23 November 2026). |
| Roadmap story | [Story 13.8](../../../_bmad-output/planning-artifacts/epics.md#story-138-optical-logic-gates-and-nand-nor-or-xor-cat-043-through-cat-047) |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES and takes every candidate from its segment query) and ENGINE-SIGNAL-NODES |

## 2. Declaration

The `optical_and` declaration, art and bindings are [CAT-043's declaration](CAT-043-optical_and.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026).

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalTransport, SignalPropagation, StateTransaction; navigation only (D14). Probe on `8d1d68aa`: 3 present, 0 untested, 2 narrower, 7 missing. The CAT-043 probe is the slice's; this table records the same findings under the repaired conventions.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R9 | A segment cast against solid colliders | narrower: `AirflowLineClear` answers clear or blocked for airflow rows only, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`); the broadphase has only a pair-overlap query (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`) | `AirflowLineClear` | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing cast: **ENGINE-SPATIAL-QUERIES** |
| R11 | Presentation binding on a committed driver | narrower: a committed signal level can colour a lamp today (`PresentationDriver.CommittedActivation`, `CuriousContraptions.Abstractions/ElementPresentation.cs:22`); no driver reads committed outlet power | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`). Owner: **ENGINE-OPTICAL** |
| MS | A signal node "threshold over a committed scalar, with a release ratio" | **missing**: `SignalNodeKind` has no such node (`CuriousContraptions.Abstractions/SignalContractValues.cs:13`) | — | owner **ENGINE-SIGNAL-NODES** |
| MB | A boolean node over signal levels | **missing**: the two-input law `LogicGateOperation` (`CuriousContraptions.Abstractions/ElectricalContractValues.cs:25`) exists only on supply terminals (`SupplyLogicDescriptor`, `CuriousContraptions.Abstractions/SupplyContractValues.cs:41`) | — | owner **ENGINE-SIGNAL-NODES** (row proposed in the batch summary) |
| M2 | The optical solve: once per tick after the last substep, on ordered crossings from the spatial query | **missing** | — | owner **ENGINE-OPTICAL** |
| M3 | Declared surface responses, here a Relay whose enable names a signal level | **missing** | — | owner **ENGINE-OPTICAL** |
| M4 | Declared receivers committing a reading and a strength | **missing** | — | owner **ENGINE-OPTICAL** |
| M5 | Outlet-power driver, beam read model, build-mode aim preview | **missing** | — | owner **ENGINE-OPTICAL** |
| M6 | `ElementCategory.Optics` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) and the optical domain flag | **missing** | — | owner **ENGINE-OPTICAL** |

No logic is evaluated inside the optical solve: a second evaluator beside the signal network would be a second rule for the same behaviour. ENGINE-OPTICAL, ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES are stories of [the engine-first section of `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#engine-first-capability-stories-21-november-2026).

**Dependencies.** Up to three lasers (CAT-036 or [EL-176](EL-176-red-laser-emitter.md) to EL-178, undelivered), Battery (delivered), an outlet observer (CAT-038 / [EL-214](EL-214-broadband-beam-detector.md), undelivered), Wall (delivered).

## 4. Sources and legacy

**Sources.** Row [element-138](../requirements.md#element-138): carrier output follows both inputs present; prove every input combination plus absent carrier. Source record [todo-367](../requirements.md#todo-367): absorbing A/B inputs, independent carrier routed at 90% power, next-tick decisions, original icons, separate catalogue entries. CAT row [current-cat-043](../requirements.md#current-cat-043): 0.25 on / 0.225 off, transition, retraction, chaining, occlusion, rotation and preserved range/interaction budget. Refinement **optical-and** ([refinements](../invest/refinements.md)): independent A/B/carrier and occlusion controls. Campaign: optical logic first use 51–60, reuse 71–90, 111–125, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); teaching set-up "two-light-windows" teaches optical AND ([refinements](../invest/refinements.md)).

**Legacy.** The complete gate harvest (17 facts, including Boolean observations, lamp-source binding and parameter-ownership tests) is in [CAT-043-optical_and](CAT-043-optical_and.md#legacy-harvest) and applies unchanged to this identity; shared optical transport facts S1–S8 are in [CAT-036-laser](CAT-036-laser.md#legacy-harvest). The facts an EL-138 story needs are restated here:
1. Geometry, apertures, outlet, retention 0.9 and Route-when-open — `parts/OpticalLogicPart.cs@a6c914e:L13-L14`, `parts/OpticalLogicPart.cs@a6c914e:L51-L58`. Carry forward.
2. Controls use broadband strength on First/Second — `parts/OpticalLogicPart.cs@a6c914e:L59-L61`; strength rule `engine/OpticalColour.cs@a6c914e:L35-L42`. Carry forward.
3. Output power = power leaving the outlet; Active only when output > 0 — `parts/OpticalLogicPart.cs@a6c914e:L62-L66`; outlet accounting at the captured outlet pose — `engine/OpticalNetwork.cs@a6c914e:L171-L182`. Carry forward behaviour; not the CPU static solve.
4. Truth evaluation and closed operation enum; undefined operation rejected — `engine/LogicGate.cs@a6c914e:L6-L19`. Carry forward.
5. Hysteresis 0.25 on / 0.225 off; validation finite 0 ≤ off < on; sample validates both inputs before committing either; advance once; reset to evaluate(false, false) — `engine/LogicGate.cs@a6c914e:L44-L80`. Carry forward.
6. Advance runs before the networks each tick — `parts/OpticalLogicPart.cs@a6c914e:L50-L50`; legacy tick order hooks → cone light → optics → acoustics → electrical — `reference/cpu/MachineWorld.cs@a6c914e:L855-L861`. Do not carry forward the per-part hook or the CPU order; carry forward next-tick behaviour (section 6).
7. Control lamps and output lamp follow at rate 12; art and truth relief — `parts/OpticalLogicPart.cs@a6c914e:L38-L43`, `parts/OpticalLogicPart.cs@a6c914e:L67-L111`. Carry forward behaviour; not Godot materials.
8. Catalogue entry and scene (Operation = 0) — `parts/catalog/optical_and.tres@a6c914e:L6-L11`, `parts/scenes/optical_and.tscn@a6c914e:L1-L5`. Carry forward the entry.
9. Acceptance: for every truth row × carrier present/absent, controls are read on the first solve without changing the decision; after one advance the gate opens iff the row is true; output is (0.9, 0.9, 0.9) for a unit carrier only when open with carrier; all outgoing segments carry 0.9 power (controls never exit); removing the carrier clears the output on the next solve; reconfiguration restores A, B and the initial decision — `CuriousContraptions.tests/OpticalLogicTests.cs@a6c914e:L16-L69`. Carry forward.
10. Acceptance: truth rows are an explicit table — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L5-L19`; exact boundaries 0.25 on, 0.24 off, 0.23 held on, 0.225 releases; a bright A cannot impersonate B — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L56-L74`; NaN, ±∞ or negative samples are rejected with no partial commit — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L76-L90`; invalid thresholds and undefined operations rejected — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L92-L107`. Carry forward.
11. Acceptance: a failed tick restores pending inputs, decision and output; the retry commits — `CuriousContraptions.tests/OpticalControlCheckpointTests.cs@a6c914e:L45-L84`. Carry forward as a later (injected-fault) gate fact.
12. Acceptance: the output observation publishes all three channels atomically across rollback, pause, hide, Reset and Save/Load; output equals 0.9 × carrier (±1e-5); lamp follows 1 − e^(−1.2) per 0.1 s — `CuriousContraptions.tests/OpticalObservationTests.cs@a6c914e:L66-L117`. Carry forward.
13. Legacy beam ink and channel marks — `engine/OpticalColour.cs@a6c914e:L49-L76`. Carry forward as presentation rules.
14. Typed Boolean observation of {First, Second, IsOpen} over the control (closed enum) — `engine/LogicGate.cs@a6c914e:L5-L5`, `engine/LogicGate.cs@a6c914e:L56-L60`, `engine/SceneBooleanObservation.cs@a6c914e:L8-L22`; acceptance: every truth row publishes typed controls without inventing carrier energy; lamps ease 1 − e^(−1.2) per 0.1 s and ignore unpublished state; rollback, hide, Reset and Save/Load restore — `CuriousContraptions.tests/BooleanObservationTests.cs@a6c914e:L60-L111`. Carry forward the typed observation; not the Godot binding.
15. Acceptance: parameter validation cannot replace the captured optical control — `CuriousContraptions.tests/ParameterValidationOwnershipTests.cs@a6c914e:L64-L91`. Carry forward at the later fault gate.
16. Acceptance: unknown or foreign Boolean lamp sources reject before Run and a corrected retry succeeds — `CuriousContraptions.tests/BooleanColourBindingTests.cs@a6c914e:L34-L51`. Carry forward.

**Files harvested:** `parts/OpticalLogicPart.cs`, `parts/catalog/optical_and.tres`, `parts/scenes/optical_and.tscn`, `engine/LogicGate.cs`, `engine/SceneBooleanObservation.cs`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/OpticalLogicTests.cs`, `CuriousContraptions.tests/LogicGateTests.cs`, `CuriousContraptions.tests/OpticalControlCheckpointTests.cs`, `CuriousContraptions.tests/OpticalObservationTests.cs`, `CuriousContraptions.tests/BooleanObservationTests.cs`, `CuriousContraptions.tests/ParameterValidationOwnershipTests.cs`, `CuriousContraptions.tests/BooleanColourBindingTests.cs`. No level in `content/puzzles.json` or lesson in `tools/Campaign` uses an optical gate.

## 5. Acceptance

**Authority.** Acceptance is [CAT-043's rows](CAT-043-optical_and.md#acceptance-outline) G1 to G13, with their fixtures, tolerances and stall guards. This identity's outcomes map to **G1** (every input combination with the carrier) and **G2** (every combination without it: logically true cannot create power); the ledger row cites those tests. No row is stated here, so none can diverge.

**Behaviour and numbers** (the laws behind those rows; one legacy unit is 10 W, so the legacy 0.25 on / 0.225 off is 2.5 W / 2.25 W).

- **Control.** A control's strength is the mean of the three channels at its aperture. An amber laser gives (10 + 7.8 + 3.2) ÷ 3 = 7.0 W (on); a red laser 10 ÷ 3 = 3.33 W (on); the amber's blue channel alone, after a blue filter, 3.2 ÷ 3 = 1.07 W (off), the kept lesson that a weak filtered channel cannot switch a control (K-7).
- **Carrier.** A Relay with τ 0.9: an amber carrier leaves as (9.0, 7.02, 2.88) W, with its range reduced by 0.990 m and one interaction used. Closed, the inlet absorbs. With no carrier the outlet is dark in every truth row.
- **Supply check.** Three amber lasers demand 3 × 25 W = 75 W, under the Battery's default 120 W (1 J per tick).
- **Tick order.** Control strengths commit with tick n; the threshold and boolean nodes take them at the start of tick n + 1; the solve at the end of tick n + 1 reads the `open` level. A control change switches the carrier one tick later; a chain of k gates responds in k ticks. After Reset both thresholds are off, so And is closed.

Chrome recipe (pending, post-catalogue): CAT-043's.

## 6. Open questions

None open.

1. **Story 13.8 says gates "emit output laser beams" — decided** (D15, K-8): the requirement row governs; the carrier is separately supplied.
2. **One catalogue part satisfies EL-138 — decided**: Duplicate closure with CAT-043's rows as the single authority (coordinator decision 16).
3. **Broadband-mean control strength — decided** (D15, K-7): kept.
4. **Thresholds not authorable — decided** (D19, K-7).
5. **Tick order and coupling — decided** (coordinator decision 16): the solve runs after the last substep; receivers couple only through signal nodes; one tick from control to carrier.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Lasers and detectors (undelivered), filters before controls ([EL-143](EL-143-red-optical-filter.md)), mirrors, Wall, Battery. No ports, so no port domains to agree.
- **Campaign.** Optical logic first use 51–60; reuse 71–90, 111–125, 136–150.
- **Coverage binding (planned, not yet live).** Catalogue key `optical_and` (CAT-043). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22); a level that places the gate locked needs whatever it delivers.

## 8. Delivery

- **Slice shape.** Closed inside CAT-043's slice. The five gates differ only in declared values, so they qualify as one Family slice; `delivery-batches.md` §1c lists them as a run (proposed change K-17 in the batch summary).
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-SIGNAL-NODES (threshold and boolean nodes); ENGINE-OPTICAL as corrected in the batch summary; a laser (CAT-036) and a detector (CAT-038); then the gate slice.
- **Files (CAT slice).** `elements/OpticalAnd/` (project, declaration, scene, icon), one guarded `CuriousContraptions.slnx` line, `CuriousContraptions.tests/OpticalGateTests.cs`, a `VisibleSolidColliderTests` row, the ledger row for EL-138.
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise engine.
- **Disqualifiers.** A Relay whose enable cannot name a signal level; no threshold or boolean signal node; a different unit scale.

## 9. Engine conformance

Verdict: **Conforms.**

1. **Declaration data only.** Two receivers, two threshold nodes, one boolean node, one relay, nine bindings. No state machine in code and no logic inside the optical solve.
2. **Generic laws.** The receiver law, the signal network's threshold and boolean rules, the Relay law; "opens next tick" follows from the tick order.
3. **Three layers.** Colliders: two boxes. Visuals: flush prints and lamps; none is a collider. Animation: lamp colours from committed levels and outlet power.
4. **One-way flow and typing.** Readings → next tick's signal network → relay enable → segments; the operation is a closed enum.
5. **Flat state.** Counts in [CAT-043's declaration](CAT-043-optical_and.md#declaration).
6. **Performance fitness.** Two primitives, static; at most one extra segment per carrier.
7. **Tests are scenarios.** CAT-043's rows place real emitters and read committed state.
