# TH-23 · Expansion rod — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-23 delivery slice](../invest/named-elements.md#thermal-23). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal units, laws, scale, tick phase and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); shared probe rows are cited as "TH-04 R*n*".

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-23 · Expansion rod · Thermal |
| Anchors | [requirements.md#thermal-23](../requirements.md#thermal-23) (task `sequence-task-457`); [named-elements.md#thermal-23](../invest/named-elements.md#thermal-23) |
| Owner key / research | S586 · [TH-S06 thermal expansion](../../thermal-component-research.md#th-s06) |
| Related identities | [TH-22 Bimetal thermostat](TH-22-bimetal-thermostat.md) (same expansion binding); heated by [TH-04 Heating plate](TH-04-electrical-heating-plate.md); pushes delivered parts (Wall, Springboard, balls). No CAT refines it. |
| Campaign | Intro 82; practice 83; reuse 90, 146 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-CONTACT-OBSERVATIONS (the moving rod's touch link to the disc); then ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (Run length; a force limit that is reached; touch links). |

### Disposition

**To deliver as a single element; lead of the "Thermal expansion" specs** (TH-22, TH-24 and TH-25 cite its probe rows E1 to E5). Planned package id `expansion_rod`, directory `elements/ExpansionRod/`, display name "Expansion rod". Not a duplicate of anything delivered.

## 2. Declaration

Follows the delivered Springboard shape (a static base carrying one moving body on a prismatic slider, `elements/Springboard/SpringboardElementDeclaration.cs:41-42`, `:70`).

- **Interfaces.** `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the thermal-node and expansion-binding contracts ENGINE-THERMAL adds.
- **Bodies.** Body 0 static (the sleeve and the guide). Body 1 dynamic (the rod), carried on slider 0 whose axis is the part's local X. Origin on the floor under the middle of the rod.
- **Colliders (metres).**

  | Collider | Body | Shape | Centre | Half extents | Purpose |
  | --- | --- | --- | --- | --- | --- |
  | `col_sleeve` | 0 | box | (−0.40, 0.135, 0) | (0.10, 0.135, 0.06) | fixed end; the rod's tail slides inside it |
  | `col_guide` | 0 | box | (0.40, 0.135, 0) | (0.06, 0.135, 0.06) | free-end guide |
  | `col_rod` | 1 | box | (0, 0.24, 0) at rest | (0.50, 0.03, 0.03) | the rod; its +X end face is the push face; the thermal face |

  The slider declares `BodiesCollide: false`, so the rod passes through its own sleeve and guide. The two static blocks stand 0.60 m apart inside, so the part straddles a Heating plate (0.60 m wide) and the rod's underside (0.21 m) lies on the plate's disc (top at 0.21 m). The rod is held by its slider exactly at the disc's top, so the narrowphase may produce no contact there; the thermal link does not need one: it exists while the two face colliders touch within ENGINE-THERMAL's one touch tolerance (T3), found through ENGINE-SPATIAL-QUERIES and kept on begin- and end-touch (ENGINE-CONTACT-OBSERVATIONS).
- **Mass and material.** Rod 0.5 kg, box inertia; friction 0.3, restitution 0.1, rolling resistance 0 on all three colliders.
- **Slider 0.** Travel 0 to 0.20 m; no axial spring, no damping parameter; moving body the rod.
- **Thermal node** (T2). `rod`: 48 J/K (0.5 kg of copper at 96 J/(kg·K), T11); ambient conductance 0.6 W/K (2.5 × 0.24 m²); face `col_rod`, 64 W/K (metal).
- **Expansion binding** (T6). Node `rod` → slider 0: rest position x₀ = k·(T − 288 K), k = 0.001 m/K; stiffness 4000 N/m; force limit 300 N (reached at a rise of 75 K, which a 14 400 J Battery attains; the earlier 400 N needed 100 K, which no family source reaches in a Run). The bound drive needs no supply route. The drive's positive work in a tick is debited from `rod` and its negative work credited (T4, T13).
- **Ports, parameters, stores.** None (D13). Work leaves through contact at the push face.
- **Presentation bindings.** `Visual/Rod` follows body 1's pose. `Visual/LoadMark` albedo navy → gold ← committed load of the bound drive, 0 to 300 N.
- **Palette.** Category `ElementCategory.Structure` (TH-04 section 6, item 5). Order: 540 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Sleeve` cream `#fff8e9` | box 0.20 × 0.27 × 0.12 m with a printed bore ring | solid, `col_sleeve` | nothing |
| `Visual/Guide` cream `#fff8e9` | box 0.12 × 0.27 × 0.12 m | solid, `col_guide` | nothing |
| `Visual/Rod` gold `#f7cb52` | box 1.0 × 0.06 × 0.06 m | solid, `col_rod` | body 1's pose |
| `Visual/Marks` navy `#293954` | five reference ticks printed flush on the guide's top face, 0.01 m apart | decorative: printed flat | nothing |
| `Visual/LoadMark` | one bar printed flush on the sleeve | decorative: printed flat, recoloured by a binding | presentation binding |

The rod does not stretch: it slides out of the sleeve, which reads as lengthening and keeps the collider a fixed box. Icon: a bar between two blocks with an arrow at one end. At a glance it differs from the Heat-conducting bar (TH-08) by its two standing blocks and tick marks.

### Compiled counts

Bodies 2 (1 static, 1 dynamic); colliders 3 (box); joints 1 (prismatic); drives 1 (temperature-bound); sensors 0; thermal nodes 1; internal thermal links 0; ports 0; stores 0; presentation bindings 1 plus one body-carried art node. One collider more than the Springboard: the rod needs a guide at each end to read as a rod.

## 3. Engine capabilities

### Capability probe

Uses TH-04 R1, R9 to R13. Rows E1 to E5 are the expansion family's own.

| # | Capability | State | Symbol | Exercising test (body read unless marked) |
| --- | --- | --- | --- | --- |
| E1 | A moving body on a prismatic slider with limits | present | `PrismaticSliderDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:104` | `DeclaredPrismaticDriveTests.ASyntheticDriveReachesItsOwnTargetOnItsOwnProfileWithItsEffortAndPowerBinding` (`CuriousContraptions.tests/DeclaredPrismaticDriveTests.cs:127`) |
| E2 | A position drive on that slider, bounded in effort, that stops on its target without overshoot and is stopped by ordinary contact | present | `PrismaticDriveDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:89` | the same test (effort within 16 N, at rest on the 1.2 m target) |
| E3 | The drive's target bound to a node temperature, with a declared stiffness, active without a supply route | **missing**: the target is a constant or a parameter (`MechanicalValue`, `:170-173`) and the drive is enabled only while a `PowerIn` route is closed (`:79`); owner ENGINE-THERMAL (expansion binding) | none | none |
| E4 | The drive's mechanical work debited from, or credited to, the bound node | **missing**; owner ENGINE-THERMAL (expansion binding) | none | none |
| E5 | The committed load on a bound coordinate as an observation and presentation driver | narrower: drive phase and state are exposed (`PresentationDriver.CommittedActuatorDrive`, `CommittedActuatorState`, `CuriousContraptions.Abstractions/ElementPresentation.cs:23-24`), the effort is not; owner ENGINE-THERMAL (the expansion binding's committed load; coordinator decision 29) | — | not opened |

Counts for this part: present 3 (TH-04 R1, E1, E2), untested 0, narrower 1 (E5), missing 7 (TH-04 R9 to R13, E3, E4). Every missing and narrower row has one owner, **ENGINE-THERMAL** ([Story ENGINE-THERMAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-thermal-heat-storage-transfer-and-phase-change), "Expansion": the binding's stiffness and force limit, the work debit and credit, and the committed load as an observation and presentation driver; coordinator decision 29). Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-23](../requirements.md#thermal-23): thermal strain changes natural length; stiffness, constraints and load determine actual displacement and work; no variants.
- **Named entry** [thermal-23](../invest/named-elements.md#thermal-23): owner S586; "Insufficient temperature change cannot bridge the target gap; constrained heating produces accounted stress".
- **Research** per-element table: "expansion → slider constraint"; parameters coefficient, length; "pushes a lever as it warms"; rod length ← strain. Expansion coefficient row 2⁻¹⁰–2⁻⁴ with scale 2⁻¹⁰.
- **Element map** row TH-23: EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SensibleHeat, ThermalExpansion, TopologyTransaction. Navigation only (D14); the constrained case adds ThermalStress.
- **Processes.** [IX-31](../requirements.md#interaction-31), [IX-32](../requirements.md#interaction-32), [IX-03](../requirements.md#interaction-03).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for expansion, strain and rod terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787`.

## 5. Acceptance

Point of truth: [thermal-23](../requirements.md#thermal-23) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof through real UI controls is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Exchange Q̇ = G·ΔT (T3). Expansion (T6): the slider is driven toward x₀ = 0.001·(T − 288) with force min(4000·(x₀ − x), 300) N. Contact decides the actual travel.
- **Why 0.001 m/K.** A declared exaggeration (about 50 times brass over 1 m) so that 30 K gives a visible 0.03 m. The research table's range is narrower; the requirement row governs (D15).
- **Warm-up on a Heating plate.** Rod and disc touch through 64·64 ÷ (64 + 64) = 32 W/K and warm almost together: C = 48 + 15.25 = 63.25 J/K, loss 0.6 + 0.5 = 1.1 W/K, time constant 63.25 ÷ 1.1 = 57.5 s, final rise 120 ÷ 1.1 = 109.1 K. The rod lags the mean by about 0.6 K (the disc leads the rod by the rod's intake ÷ 32, about 2.6 K, weighted by 15.25 ÷ 63.25).
  - 0.03 m needs a rod rise of 30 K, a mean rise of 30.6 K: t = −57.5·ln(1 − 30.6 ÷ 109.1) = 57.5 × 0.329 = 18.9 s, costing 2270 J.
  - Default Battery empty at 30 s: mean rise 109.1 × (1 − e^(−0.5217)) = 44.3 K, rod 43.7 K, travel 0.044 m.
  - Battery `capacity` 1200 J (10 s): mean rise 109.1 × (1 − e^(−0.1739)) = 17.4 K, travel at most 0.017 m.
- **Blocked rod.** Push face against a Wall at zero gap, default Battery: load = min(4000 × 0.0437, 300) = 175 N at 30 s. The limit binds from a rise of 300 ÷ 4000 ÷ 0.001 = 75 K. With Battery `capacity` 14 400 J (120 s of heat) the rod's rise reaches 75 K at t = −57.5 × ln(1 − 75.6 ÷ 109.1) = 57.5 × 1.18 = 68 s and about 95 K by 120 s (109.1 × (1 − e^(−2.087)) = 95.6 K mean), so the load climbs to 300 N near 68 s and stays there.
- **Work.** Pushing a default Springboard laid on its side (400 N/m, `elements/Springboard/SpringboardElementDeclaration.cs:27`): 4000·(x₀ − x) = 400·x gives x = x₀ × 10 ÷ 11; at x₀ = 0.0437 m, x = 0.0397 m, force 15.9 N, work ½ × 400 × 0.0397² = 0.32 J, debited from the rod (0.32 ÷ 48 = 0.007 K). The earlier revision stored strain energy with no debit; that is corrected.
- **Cooling.** With the source empty, the pair cools with the same 57.5 s constant: 120 s later the rise is 43.7 × e^(−2.087) = 5.4 K, travel 0.005 m.
- **Tick order.** Temperature commits with tick N (T8); the drive of tick N + 1's substeps uses the rest position from it.
- **Run length these rows need: 150 s** (A5 observes 120 s after the default Battery empties at 30 s; A3's boundary runs a 14 400 J Battery for 120 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).
- **Tolerances** (T12): travel 0.003 m (a tenth of the lesson gap); load 15%; the 0.32 J work row uses 0.1 J, stated because T12's 5 J floor would hide it.

### Rows

Planned class `ExpansionRodTests` in `CuriousContraptions.tests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the rod on a wired Heating plate and a Wall 0.03 m from the push face, when Run, then the push face reaches the Wall | `AWarmedRodBridgesAThreeCentimetreGap` | Battery `capacity` 1200 J: travel stays under 0.02 m and the gap is never bridged | a 0.05 m gap is not bridged on a default Battery (0.044 m) | 0.003 m; stall guard 57 s |
| A2 | Given A1 without the Wall, then at every sample the travel is 0.001 × the rod's rise | `FreeTravelFollowsTheRodsTemperature` | an unheated rod stays at 0 | none | 0.003 m |
| A3 | Given the push face against a Wall at zero gap, when the default Battery is empty, then the travel is under 0.003 m and the committed load is 175 N | `ABlockedRodBuildsLoadInsteadOfMoving` | unheated: load under 1 N | `capacity` 14 400 J: the load reaches 300 N between 55 and 85 s and never exceeds it | 15%; the limit within 1 N |
| A4 | Given the push face against a sideways default Springboard, when the Battery is empty, then the plate has moved 0.040 m and the rod's work account equals the spring's stored 0.32 J | `ExpansionWorkIsPaidFromTheRodsHeat` | A2: no load, no work | none | 0.004 m; 0.1 J |
| A5 | Given A2, when 120 s pass after the source empties (observed at 150 s), then the travel is back under 0.008 m | `ACoolingRodReturns` | none | none | 0.003 m at the fixed observation time |
| A6 | Given A1 then Reset, then the rod is at 0 and 288 K; a second Run repeats A1 | `ResetRestoresTheRodAndItsTemperature` | none | none | as A1 |
| A7 | Given a placed rod, when saved and loaded, then its pose is kept and a fresh Run passes A1; no run temperature or travel is saved | `SaveAndLoadKeepThePoseAndNoRunState` | none | none | as A1 |

No ports and no parameters, so no wrong-kind or parameter-refusal row. The requirement's two outcome clauses are A1's control and boundary ("insufficient temperature change cannot bridge the target gap") and A3 ("constrained heating produces accounted stress").

## 6. Open questions

Resolved:

1. **Game scale.** D5, as restated in the thermal conventions.
2. **Stress in the map.** The constrained case is the bound drive's committed load (E5); the map is navigation (D14).
3. **Stiffness.** A declared 4000 N/m with a 300 N limit (lowered from 400 N so that a level's largest Battery reaches it; audit), decided now: a modulus × area ÷ length rule would be a second way to state one number.
4. **Failure.** The rod only pushes (D19); a breaking rod would be a declared load limit under ENGINE-FRACTURE, which no lesson uses.
5. **Exaggeration.** 0.001 m/K adopted (D1, D15).
6. **One end anchored.** Decided now: the rod slides out of a fixed sleeve; colliders cannot change length at run time, and a sliding fixed-size box needs no new capability.

Needs the owner: none.

## 7. Integration

- **Works with.** Heating plate (planned first); Wall, Springboard, balls (delivered); TH-22 (same binding).
- **Campaign.** Introduction 82, practice 83; reuse 90, 146. A level that fixes the rod in place needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element; the first consumer of the expansion binding.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS for the touch link); ENGINE-GAS-NETWORK and ENGINE-LIQUID (owner decision 18 places ENGINE-THERMAL after them); ENGINE-THERMAL as amended, including E3 to E5; ENGINE-RUN-LENGTH (A3's boundary, A5); TH-04; S7 for the coverage binding; S3 preferred.
- **Files.** `elements/ExpansionRod/ExpansionRod.csproj`, `ExpansionRodElementDeclaration.cs`, `expansion_rod.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); `CuriousContraptions.tests/ExpansionRodTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Engine tier on the first use of E3 to E5; data-only if ENGINE-THERMAL ships them tested.
- **Disqualifiers (the slice's probe re-checks).** ENGINE-THERMAL delivering expansion as a collider scale or a transform write instead of a coordinate drive; a binding with no stiffness (A3's 175 N becomes the 300 N limit: re-derive); no work debit (A4 fails); a slider whose moving body cannot rest on another part's collider while passing through its own base.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Three colliders, one slider, one thermal node, one expansion binding, one presentation binding. No update loop, handler or engine branch on this part.
2. **Generic laws.** Q̇ = G·ΔT; a coordinate drive toward a rest position that is a linear function of a node temperature; contact. "Pushes what is in front of it" is contact with a driven body.
3. **Three layers.** Colliders: sleeve, guide, rod. Visuals: three solid art boxes and two printed marks. Animation: the load mark's colour; the rod's art follows its body's pose. Nothing is written back.
4. **Flow and typing.** No ports; f32 values; typed node and coordinate references.
5. **Flat state.** The counts in section 2, fixed at compile time.
6. **Performance.** Three primitive colliders; one joint; no sensor. Thermal work per tick is one node, one binding and one link per body touching `col_rod` (T10). A cold rod at rest is a candidate for sleeping once ENGINE-ISLANDS-SLEEP exists; a temperature change that moves x₀ must wake it.
7. **Tests are scenarios.** A1 to A7 run the production simulation and pass or fail on the exchange, drive and contact laws.

**Verdict: Conforms; blocked on ENGINE-THERMAL, a general capability stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `expansion_rod` |
| Named identity | TH-23, `../invest/named-elements.md#thermal-23` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-457` and the ledger entry has no todo link |
| Decision | S586; `scope-corrections.md` on published main has no `s586` block |
| Mode records | `Configuration=Fixed` |
