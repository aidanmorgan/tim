# EL-210 · Flashlight — named-identity spec

**Current delivery (24 October 2026):** [EL-210 delivery slice](../invest/named-elements.md#element-210). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. Local +X is the beam direction. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-210 |
| Name | Flashlight |
| Type | Optical |
| Anchor | [requirements.md#element-210](../requirements.md#element-210); named entry [named-elements.md#element-210](../invest/named-elements.md#element-210); source record [campaign-element-coverage](../requirements.md#campaign-element-coverage); scope index [todo-196](../requirements.md#todo-196) |
| Owner | S495 |
| Disposition | **Refinement of [CAT-029 flashlight](CAT-029-flashlight.md) delivered in that part's slice as the one package `flashlight`.** Not a duplicate: this identity requires "supplied … no supply means no emission", which CAT-029's row ("self-contained torch") does not, so the package has a `supply` choice and a `PowerIn` socket (decision K-11). No second package or catalogue entry. |
| Acceptance authority | The CAT-029 spec's acceptance rows are the single authority for the shared package, including this identity's outcomes. "A supplied finite-aperture source emits a widening cone" is CAT-029 rows **F1, F2 and F4**; "occluding geometry blocks coverage" is **F5**; "no supply means no emission" is **F12**, with **F13** (supply loss and return), **F10** (the supply choice governs its socket) and **F7** (light never exceeds energy paid). The ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | `flashlight`, directory `elements/Flashlight/`, class `FlashlightElementDeclaration` (CAT-029's). The CAT spec is the single declaration (coordinator decision 27); section 2 points to it. |
| Roadmap story | [Story 13.1](../../../_bmad-output/planning-artifacts/epics.md#story-131-flashlight-torch--collimated-laser-emitters-cat-029-cat-036) |
| Status | not started; no package; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, in which ENGINE-SPATIAL-QUERIES provides the segment cast and ENGINE-CONTACT-OBSERVATIONS the button's contact observations; then on ENGINE-JOINT-KINDS (the button spring's declared rest position, owner decision 6), ENGINE-RUN-LENGTH (CAT-029 row F8's 150 s Run) and ENGINE-OPTICAL, as CAT-029 is |

## 2. Declaration

The `flashlight` declaration, art, bindings and compiled counts are [CAT-029, Declaration](CAT-029-flashlight.md#declaration), including this identity's delta (the `supply` choice and the `PowerIn` socket, decision K-11); this identity adds none (coordinator decision 27, 23 November 2026). Palette: CAT-029's order 315 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); no order of its own.

## 3. Engine capabilities

Binding shard ([element-03.json](../../coverage/engine/element-03.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction; navigation only (D14). Probe on `8d1d68aa`: 6 present, 4 present-untested, 4 narrower, 5 missing. CAT-029's probe is the slice's. A row is "present" only where the cited test proves the capability as this part uses it.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:237`) |
| R3 | Typed ports and wrong-kind connection refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R16 | Prismatic slider with stiffness, damping and limits | present | `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`) | `WorkshopSpringboardTests.CompiledPreloadUsesActualF32PoseAndEightJoules` (`CuriousContraptions.tests/WorkshopSpringboardTests.cs:146`) |
| R19 | Choice parameter selecting one supply mode, with port availability | present | Fan (`elements/Fan/FanElementDeclaration.cs:13`–`:14`, `:20`, `:53`–`:55`, `:60`–`:63`) | `FanIntegrationTests.SelectedModeCompilesOneWorkInputAndOneBladeWriterWithoutHiddenStores` (`CuriousContraptions.tests/FanIntegrationTests.cs:485`) |
| R5 | A memory latch with no reset port that enables an electrically paid consumer and survives supply loss | present, untested as used: `SignalMemoryDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:64`) is exercised with a reset pulse (`StateLatchTests.cs:238`) and through supply loss on a contact (`SwitchElectricalTests.cs:72`); the Fan's latch exists only in its Self-contained mode | `SignalMemoryDescriptor` | CAT-029 rows F2, F12 and F13 are its first proof |
| R7 | Proportional sharing of a per-phase power allowance among compiled consumers | present, untested as used: `ElectricalSupply.Reserve` (`engine/gpu/ElectricalSupply.cs:11`) is proven as a pure function (`ElectricalSupplyTests.cs:207`), not for an emitter | `ElectricalSupply.Reserve` | CAT-029 row F7 is its first proof |
| R20 | Contact sensor node on a child body | present, untested: the descriptor takes a body index (`SignalContractValues.cs:51`), but the delivered use is body 0 (`elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:79`), where any qualifying contact on the part fires | `SignalContactDescriptor` | nearest: `VisibleSolidColliderTests.ABallFallingBesideTheButtonInsideTheOldBoxCornerPassesFreelyAndOnlyTheButtonFiresTheSwitch` (`VisibleSolidColliderTests.cs:1032`), on body 0 |
| R22 | A signal edge inside one package (contact output → memory set) | present, untested: `ISignalElement.SignalEdges` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:11`) is an empty list in every delivered declaration read for this batch | `SignalEdge` | none opened |
| R6 | Supply consumer paying a continuous target | narrower: no emitter target (`CuriousContraptions.Abstractions/SupplyContractValues.cs:42`–`:54`) | `SupplyConsumerDescriptor` | `FanIntegrationTests.ElectricalModePaysOnlyExposedWorkAndNeverUsesSelfContainedEnergy` (`FanIntegrationTests.cs:540`). Owner: **ENGINE-OPTICAL** |
| R9 | A segment cast against solid colliders | narrower: `AirflowLineClear` answers clear or blocked for airflow rows only, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`) | `AirflowLineClear` | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing cast: **ENGINE-SPATIAL-QUERIES** |
| R11 | Presentation binding on a committed driver | narrower: no optical driver (`CuriousContraptions.Abstractions/ElementPresentation.cs:18`–`:37`) | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`). Owner: **ENGINE-OPTICAL** |
| R21 | A continuous output paid from a declared store | narrower: airflow sources only (`StoredAirflowInput`, `CuriousContraptions.Abstractions/AirflowValues.cs:36`) | `StoredAirflowInput` | `FanIntegrationTests.SelectedModeCompilesOneWorkInputAndOneBladeWriterWithoutHiddenStores` (`FanIntegrationTests.cs:485`). Owner: **ENGINE-OPTICAL** |
| M1 | A declared emitter paid by a supply consumer or a store, enabled by a signal level, emitting in proportion to the energy granted over the tick | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | The optical solve: once per tick after the last substep, with the diverging law, shadow casts from the spatial query and conserved shares | **missing** | — | owner **ENGINE-OPTICAL** |
| M5 | Committed-emission driver and the cone read model | **missing** | — | owner **ENGINE-OPTICAL** |
| M6 | The optical `SimulationDomain` flag | **missing** | — | owner **ENGINE-OPTICAL** |
| J1 | A declared spring rest position on a prismatic joint, separate from its stops (here 0.02 m above the upper stop) | **missing** | — | owner **ENGINE-JOINT-KINDS** (owner decision 6; coordinator decision 21); CAT-029 row J1 |

ENGINE-OPTICAL is [its story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception); ENGINE-SPATIAL-QUERIES is named by owner decision 3a and has its story there too. The corrected story text is in the [batch summary](readiness/batch-k.md).

**Dependencies.** A falling ball, Impact switch, Delay box, Battery, Wall (delivered); an observer: the light sensor [EL-153](EL-153-general-light-receiver.md), the Solar panel [EL-211](EL-211-solar-panel.md) or a broadband detector (undelivered).

## 4. Sources and legacy

**Sources.** Row [element-210](../requirements.md#element-210): a supplied finite-aperture source emits a widening optical cone; occluding geometry blocks coverage and no supply means no emission. CAT row [current-cat-029](../requirements.md#current-cat-029): self-contained torch, 8-unit range, 15° half-angle, nine solar samples, four nested 48-direction shells; off, rotated, shadow and missing-wire controls. Campaign: flashlight first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (full harvest in [CAT-029-flashlight](CAT-029-flashlight.md#legacy-harvest); key facts):
1. Lens (0.66, 0, 0), range 8, cone cosine 0.9659258, intensity 24 — `parts/FlashlightPart.cs@a6c914e:L10-L13`. Carry forward.
2. Button and lens curves; light source only while active — `parts/FlashlightPart.cs@a6c914e:L18-L27`. Carry forward.
3. Geometry and colours — `parts/FlashlightPart.cs@a6c914e:L28-L44`. Carry forward.
4. Button contact predicate — `parts/FlashlightPart.cs@a6c914e:L45-L52`. Carry forward the predicate; not the per-part callback.
5. Cone law — `engine/LightNetwork.cs@a6c914e:L12-L52`. Carry forward; not the CPU solve.
6. Catalogue entry (description "Self-contained battery light") — `parts/catalog/flashlight.tres@a6c914e:L8-L14`. Carry forward.
7. Acceptance: button strike at 0° and 37° roll activates; underside strike and disabled collider do not — `CuriousContraptions.tests/FlashlightContactTests.cs@a6c914e:L19-L60`; reversed or out-of-range panel reads 0 — `CuriousContraptions.tests/LightTests.cs@a6c914e:L116-L166`; invalid cone declarations reject before Run — `CuriousContraptions.tests/LightConeBindingTests.cs@a6c914e:L11-L64`. Carry forward.
8. The torch's built-in supply is unbounded: the light source exists whenever the torch is active, with no supply check — `parts/FlashlightPart.cs@a6c914e:L26-L27`; the paired solar panel likewise has no storage or voltage model — `parts/SolarPanelPart.cs@a6c914e:L8-L9`. Do not carry forward: it conflicts with EL-210's "no supply means no emission" (decided in section 6).
9. Lessons: torch locked at (2, 3, 0) facing −X with the trigger ball above the button; assistance thresholds 0.2 / 0.47 / 0.8 — `content/puzzles.json@a6c914e:L5944-L6225`, `content/puzzles.json@a6c914e:L5955-L5998`. Carry forward for Epic 15.

**Files harvested:** `parts/FlashlightPart.cs`, `parts/catalog/flashlight.tres`, `engine/LightNetwork.cs`, `parts/SolarPanelPart.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/FlashlightContactTests.cs`, `CuriousContraptions.tests/LightTests.cs`, `CuriousContraptions.tests/LightConeBindingTests.cs`, `content/puzzles.json`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-029's rows](CAT-029-flashlight.md#acceptance-outline) F1 to F14, with their fixtures, tolerances and stall guards, in the class `FlashlightTests`. This identity's outcomes map to **F1, F2, F4** (a supplied source emits a widening cone, by button or by pulse), **F5** (occluding geometry blocks coverage), **F12** (no supply means no emission, in Electrical mode unwired and on a Battery that runs out), **F13**, **F10** and **F7**. The ledger row cites those tests. No row is stated here, so none can diverge. CAT-029 row F15 (two detectors staggered in depth in one cone) proves that receivers together never read more than is emitted.

**Behaviour and numbers** (the laws behind those rows).

- **Light.** White, (30, 30, 30) W = 90 W, spread over a 15° half-angle cone: Ω = 2π(1 − cos 15°) = 2π × 0.0340742 = 0.2141 sr, so I = 90 ÷ 0.2141 = 420.4 W/sr. Irradiance on the axis: 420.4 W/m² at 1 m, 420.4 ÷ 9 = 46.7 W/m² at 3 m, 420.4 ÷ 64 = 6.57 W/m² at the 8 m range. The cone's radius is d × tan 15° = 0.268 d: 0.80 m at 3 m. The legacy "intensity 24" becomes these watts (decision K-1): 46.7 ÷ 2.67 = 17.5 W/m² per legacy irradiance unit.
- **Conservation.** Receivers in the cone share its 90 W (conventions, item 8). In CAT-029 row F15 two detectors staggered in depth (2.3 m and 3.2 m from the lens face, 0.55 m and 0.80 m off the axis on opposite sides) take 69.5 W and 35.6 W uncapped, 105.1 W together; the solve scales every share by 90 ÷ 105.1, so they read 59.5 W and 30.5 W.
- **Energy.** Demand 100 W = 0.833 J per tick, under the Battery's 1 J per tick at its 120 W default; the 10 W not radiated is the declared loss. Electrical mode on a default 3,600 J Battery runs 36 s; Self-contained mode runs 14,400 ÷ 100 = 144 s and then goes dark until Reset. CAT-029's row F8 lets the store run out and so needs a Run of 150 s; under owner decisions 13 and 17 the level or test that runs it declares that Run length (`ENGINE-RUN-LENGTH`; the default is 30 s).
- **Button.** The spring, with its rest position 0.02 m above the upper stop, presses the 0.05 kg button onto that stop with 60 × 0.02 = 1.2 N against its 0.49 N weight, so it sits on its stop in every roll of the torch (owner decision 6; CAT-029). A body that reaches it at `trigger_threshold` (default 0.8 m/s) or faster fires the contact node, which sets the latch. Contact on any other collider of the torch does nothing, because the node is bound to body 1. The pulse on `ActivationIn` sets the same latch.
- **Shadows, filters and mirrors.** Any absorbing collider between the lens and a sample point removes that sample's share. A Transmit surface between them multiplies it: through a red filter the cone arrives red only. A Split multiplies it by its τ and absorbs its ρ share. Mirrors and relays do not redirect a cone (except a Relay inlet declared as a receiver aperture, which takes its conserved share); they block it, and a cone starts no further segment (conventions, item 3; decision K-3).
- **Tick order.** The latch and routing are resolved at the start of the tick; energy is granted in its substeps; the cone is emitted by the solve after the last substep; readings commit with the tick and act from the next.

Chrome recipe (pending, post-catalogue): CAT-029's.

## 6. Open questions

None open.

1. **Supply model — decided** (K-11): a `supply` choice of Self-contained (a finite 14,400 J store) or Electrical (`PowerIn`). Reason: the requirement says "supplied"; the Fan is the delivered shape; a store that never empties would break the energy ledger.
2. **"35° cone" in Story 13.1 — decided** (D15, K-8): 15° half-angle, as both requirement rows say.
3. **Intensity and falloff — decided** (K-1): 90 W over 0.2141 sr, inverse-square beyond 1 m.
4. **One catalogue part — decided**: one package, with CAT-029's rows as the single acceptance authority (section 1).

Needs the owner: none.

## 7. Integration

- **Collaborators.** Solar panel [EL-211](EL-211-solar-panel.md) (one package with CAT-059; at 3 m its 1.8 m² face absorbs about 81 W of this 90 W torch, irradiance being W/m² directly), light sensor [EL-153](EL-153-general-light-receiver.md), light-charge receiver [EL-154](EL-154-light-charge-receiver.md), every detector (one receiver law for all light, owner decision 9), filters; Impact switch, Delay box, Battery, Wall, balls (delivered). Port domains: `ActivationIn` Signal Pulse; `PowerIn` Electrical Input.
- **Campaign.** Chapter 6, slots 51–60. `content/puzzles.json` places it locked in `solar_motor`, `solar_shadow` and `delayed_solar`, the three planned levels blocked on this part and the Solar panel.
- **Coverage binding (planned, not yet live).** Catalogue key `flashlight` (CAT-029); this refinement is covered by that package and its mapped rows and has no binding section of its own, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).
- **Lesson fixtures.** The three levels place the torch locked; story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** One element slice (CAT-029 with this identity's outcomes). `delivery-batches.md` §2c still describes this identity as "two activation modes"; its replacement wording is in the batch summary.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS; ENGINE-JOINT-KINDS (button rest position); ENGINE-RUN-LENGTH (row F8); ENGINE-OPTICAL as corrected in the batch summary; an observer; S7 for counting the refinement.
- **Files.** `elements/Flashlight/` (project, declaration, `flashlight.tscn`, `icon.svg`), one guarded `CuriousContraptions.slnx` line, `CuriousContraptions.tests/FlashlightTests.cs`, a `VisibleSolidColliderTests` row, ledger rows for CAT-029 and EL-210.
- **Review tier.** Engine tier: four rows are present-untested (R5, R7, R20, R22), and a failing probe on any is engine work.
- **Disqualifiers.** A contact node that cannot be bound to a child body (then the button needs another general sensor, and the slice returns to planning); no store-paid emitter (Self-contained mode cannot be declared); a different unit scale.

## 9. Engine conformance

Verdict: **Conforms.**

1. **Declaration data only.** Two bodies, one slider with a declared spring rest position, a contact node, a memory node, a store, a consumer, an emitter, two parameters. "Press to light" is an edge between two declared signal nodes.
2. **Generic laws.** Contact threshold, spring slider, memory set, supply reservation or store debit, the diverging segment law with conserved shares.
3. **Three layers.** Colliders: six primitives on two bodies. Visuals: barrel, collar, base, button, studs; none is a collider. Animation: the lens colour from committed emission; the button's art follows its body; the cone is drawn from the committed segment.
4. **One-way flow and typing.** Contact → latch → emitter → readings; the supply choice is a typed Choice parameter.
5. **Flat state.** Counts in section 2.
6. **Performance fitness.** One small dynamic body, which will sleep at rest once ENGINE-ISLANDS-SLEEP lands (no island sleeping exists today); six primitives, the extra ones being the visible base and studs; one diverging segment costs one shadow cast per receiver sample in the scene, through the spatial query.
7. **Tests are scenarios.** CAT-029's rows.
