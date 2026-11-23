# TH-15 · Ice plug — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-15 delivery slice](../invest/named-elements.md#thermal-15). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal conventions T1 to T14 are owned by [TH-04](TH-04-electrical-heating-plate.md#thermal-conventions); ice values and probe rows I1 to I6 by [TH-14](TH-14-ice-block.md); liquid conventions L1 to L18 by [EL-001](EL-001-finite-reservoir.md).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-15 · Ice plug · Thermal |
| Anchors | [requirements.md#thermal-15](../requirements.md#thermal-15) (task `sequence-task-449`); [named-elements.md#thermal-15](../invest/named-elements.md#thermal-15) |
| Owner key / research | S579 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | Same water material as [TH-14 Ice block](TH-14-ice-block.md), with a separate placement role and shape (requirement row). Joins the water family's mouths, for example [EL-001 Finite reservoir](EL-001-finite-reservoir.md). No CAT refines it. |
| Campaign | Intro 68; practice 69; reuse 86, 137 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES (the static tab-on-disc pair); then ENGINE-LIQUID, ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (D1) and owner decisions 13 and 17 and coordinator decision 21, and on 23 November 2026 against `docs/planning/elements/readiness/audits/reaudit-group-3.md` (the rig's reservoir). |

### Disposition

**To deliver as a single element, after TH-14.** It is not a family member of the Ice block: it adds a liquid passage, two mouths and a seat, which the block does not have. Planned package id `ice_plug`, directory `elements/IcePlug/`, display name "Ice plug". Not a duplicate of anything delivered.

## 2. Declaration

A static pipe fitting in the liquid family's shape (two mouths and a seat, as the tap of [EL-003](EL-003-tap.md)) whose seat is closed by ice.

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and the fitting, mouth, seat, node and phase-row contracts of ENGINE-LIQUID and ENGINE-THERMAL. `KinematicMode.Static`.
- **Body.** One static body; origin on the bench under the tab.
- **Colliders (metres).**

  | Collider | Shape | Centre | Size | Purpose |
  | --- | --- | --- | --- | --- |
  | `col_tab` | box | (0, 0.02, 0) | half extents (0.10, 0.02, 0.10) | the gold heat tab; the thermal face |
  | `col_collar` | box | (0, 0.24, 0) | half extents (0.15, 0.20, 0.15) | the cream aperture block |
  | `col_mouth_a`, `col_mouth_b` | tubes, axis X | (∓0.18, 0.25, 0) | outer radius 0.14, bore radius 0.10, length 0.06 | the two pipe mouths (L5) |

- **Material (f32).** Friction 0.5, restitution 0.05.
- **Liquid.** A fitting (zero volume, L3) between mouths `Start` at (−0.21, 0.25, 0) facing −X and `End` at (0.21, 0.25, 0) facing +X, both pipe bore, role Bidirectional. A seat between `Start` and the fitting, with opening = 1 − f (below). Aperture constant when fully open: the pipe bore's 0.08349 (L7).
- **Thermal node** `plug` (T2): 26.25 J/K (0.05 kg of ice; the bore's 0.10 m radius × 0.10 m length is 0.00314 m³, 0.05 kg at 16 kg/m³); authored initial temperature 253 K; ambient conductance 0.05 W/K (enclosed); face `col_tab`, 64 W/K. The collar declares no node.
- **Phase row** (T5): melt at 273 K; latent heat 0.05 × 20 875 = 1044 J; committed solid fraction f. The melted volume (up to 0.003125 m³) enters the fitting as a source and leaves with the flow.
- **Seat rule.** The seat's open area fraction is 1 − f: the solid thins across the bore, so what is left still obstructs in proportion.
- **Ports, parameters.** No wire ports; no parameters (D13).
- **Presentation bindings.** `Visual/Insert` scale (Y and Z) ← √f, so its drawn cross-section is f of the bore.
- **Palette.** `ElementCategory.Structure` (a pipe fitting). Order: 531 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tab` gold `#f7cb52` | box 0.20 × 0.04 × 0.20 | solid, `col_tab` | nothing |
| `Visual/Collar` cream `#fff8e9` | box 0.30 × 0.40 × 0.30 with a round window recessed in its front | solid, `col_collar` | nothing |
| `Visual/Mouth_A`, `Visual/Mouth_B` cream | open tubes as the colliders | hollow, `col_mouth_*` | nothing |
| `Visual/Insert` cyan `#66b8c9`, translucent | cylinder radius 0.10, length 0.10, inside the window | decorative: enclosed in the collar, scaled by a binding | presentation binding |

Icon: a block with a pipe stub each side and a disc in a round window. It differs from the tap (a handle on top) by the window and from the Ice block (a free cube) by the pipe stubs.

### Compiled counts

Bodies 1 (static); colliders 4 (2 boxes, 2 tubes); joints 0; sensors 0; thermal nodes 1; phase rows 1; liquid fittings 1; mouths 2; seats 1; liquid sources 1; presentation bindings 1.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Owner or evidence |
| --- | --- | --- | --- |
| TH-04 R1 | Static box and tube colliders from art | present | as TH-04 (body read); tube is an admitted shape (`ColliderShapeKind`, `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) |
| G1 | A liquid fitting with two snapping mouths and a seat | **missing** | ENGINE-LIQUID (L3, L5, L6, L14) |
| G2 | A seat whose opening follows a committed scalar of another capability (here a phase fraction), by a declared clamped linear map | **missing** | ENGINE-LIQUID (liquid convention L14(d): "any committed scalar observation through a clamped linear map") |
| TH-04 R10 | The static tab touching the static disc conducts | **missing** | ENGINE-THERMAL with ENGINE-SPATIAL-QUERIES (static pairs resolved once at Run, T3) |
| TH-14 I2 | Node with an authored initial temperature, a face and ambient exchange | **missing** | ENGINE-THERMAL |
| TH-14 I3 | Melt row with a committed solid fraction | **missing** | ENGINE-THERMAL |
| TH-14 I6 | A liquid source fed by a phase row | **missing** | ENGINE-LIQUID |
| TH-04 R12 | Phase-fraction presentation driver | **missing** | ENGINE-THERMAL |

Counts: present 1, untested 0, narrower 0, missing 7. Each has one owner. I6 rests on L3's declared exchange (a phase change moving volume into another capability's ledger); its delivery into a zero-volume fitting at the row's committed melt rate, exempt from L17's zero-flow threshold, is an ENGINE-LIQUID text addition in the [Batch N summary](readiness/batch-n.md). Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-15](../requirements.md#thermal-15): a solid-water insert blocks an aperture until melting changes the occupied geometry; separate placement and shape from the ice-block cargo role; no variants.
- **Named entry** [thermal-15](../invest/named-elements.md#thermal-15): owner S579; "heating an unrelated body does not open it; residual solid continues to obstruct as geometry dictates".
- **Research** per-element table: "ice block as a gate"; parameter mass; "releases a path when melted"; scale ← f.
- **Element map** row TH-15: EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, PhaseTopology, PressureWork, RigidBodyDynamics, SensibleHeat, SolidMelting, TopologyTransaction (D14).
- **Processes and scenarios.** [IX-21](../requirements.md#interaction-21), [IX-41](../requirements.md#interaction-41), [IX-09](../requirements.md#interaction-09); [TX-07](../requirements.md#thermal-scenario-07).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for plug, ice, melt and aperture terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance

Point of truth: [thermal-15](../requirements.md#thermal-15) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Q̇ = G·ΔT (T3); the melt row (T5); the liquid aperture law Q = K·√Δh with K scaled by the seat opening (L7, L14).
- **On the plate** (TH-04: 120 W, disc 15.25 J/K starting at 288 K, 0.5 W/K; tab and disc are a static-on-static touch pair, metal to metal 64 × 64 ÷ 128 = 32 W/K, T3). The disc starts 35 K above the plug's 253 K and holds 15.25 × 35 = 534 J above it, so the first seconds are not quasi-steady (audit D1; the earlier 4.0 s ignored that store). Evaluated tick by tick with T9's exact step and a 1 J grant per tick (default Battery):
  - the plug reaches 273 K and starts to melt at **2.6 s** (the disc gives up its stored heat at once: the pair alone would settle at (15.25 × 288 + 26.25 × 253) ÷ 41.5 = 265.9 K within about 0.5 s, and the 120 W grant does the rest);
  - f = 0.96 at 3 s, 0.37 at 8 s, 0.13 at 10 s; **fully open at 11.1 s**, for about 1.33 kJ of the Battery's 3600 J.
- **Unheated plate.** A plug on an unsupplied disc: the pair settles at 265.9 K, then the disc's and plug's 0.55 W/K to the room warm 41.5 J/K to 273 K in 75.5 × ln(22.1 ÷ 15) = 29 s; melting starts at about 30 s and, at about 0.55 × 15 = 8.3 W, finishes after about 155 s (f = 0.30 at 120 s). An unsupplied plate is therefore not a "shut" control; the control is a plug on the bench.
- **On the bench.** Only the plug's own 0.05 W/K acts: time constant 525 s; 273 K at 525 × ln(35 ÷ 15) = 445 s. The plug stays shut for more than seven minutes.
- **Residual solid.** A Battery `capacity` of 600 J stops the heat at 5 s; the disc's remaining heat and the room keep melting slowly: f = 0.66 at 8 s, 0.64 at 10 s, 0.56 at 20 s, 0.48 at 30 s. At 10 s the opening is 1 − 0.64 = 0.36; in series with one joined store mouth (L7) the constant falls from 0.08349 ÷ √2 = 0.0590 (open) to 0.08349 ÷ √(1 + 1 ÷ 0.36²) = 0.08349 ÷ 2.952 = 0.0283: 48% of the open flow. (With 960 J, the earlier boundary, the plug is fully open by 48 s: f = 0.37 at 8 s, 0.14 at 30 s.)
- **Held head.** A seat is a seat: upstream head does not push partly melted ice out (decided, section 6).
- **Mass.** Upstream loss + melted volume = downstream gain + `Spilled`.
- **Tick order.** T8: f commits with the tick; the seat opening is read by the liquid step of the next tick's substeps (L16).
- **Run length these rows need: 120 s** (A2 observes for 120 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s). A1 and A3 fit in 30 s.
- **Tolerances.** T12; opening as each row states; liquid moved by the phase row is checked as mass on the ledgers, in grams (T12; the plug's own water is 50 g, far below L17's 0.004 m³ = 64 g volume tolerance); flow 10%.

### Rows

Planned class `IcePlugTests`. Rig: an EL-001 reservoir with `initial_volume` 0.5 m³, placed so that its `End` mouth joins `Start` (its interior floor level with the bore; on its 1.0 m² plan area the surface stands 0.5 m above it), an open store below `End`, the plug standing on a TH-04 plate wired to a default Battery.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the rig (default Battery, 3600 J), then no liquid passes in the first 2 s, and by 20 s the seat is fully open and the lower store is filling | `AMeltedPlugOpensThePath` | A2 | none | stall guard 33 s (the smaller of 3 × 11 and 11 + 60 s) |
| A2 | Given the plug standing on the bench in the same plumbing and the supplied plate 1 m away heating another body, then no liquid passes and f stays 1 for 120 s | `HeatingAnotherBodyDoesNotOpenIt` | A1 | none | exact: zero volume moved |
| A3 | Given a Battery `capacity` of 600 J, when 10 s have passed, then the opening is between 0.2 and 0.8, and the flow is the open flow × the series factor of L7 at the committed opening (48% at the hand value 0.36) | `ResidualIceStillObstructs` | A1 | none | opening band as stated; flow ± 10% of the open flow |
| A4 | Given A1 when the reservoir is empty, then reservoir loss + 50 g of meltwater = lower store + `Spilled`, by mass on the stores' ledgers | `MeltwaterJoinsTheFlow` | none | none | 2 g; stall guard 95 s (hand 35 s: the 0.5 m head drains through the open series constant 0.0590 in 2 × √0.5 ÷ 0.0590 = 24 s after the seat opens at 11.1 s; the smaller of 3 × 35 and 35 + 60) |
| A5 | Reset restores f = 1 at 253 K, a shut seat and the reservoir; a second Run repeats A1 | `ResetRefreezesThePlug` | none | none | as A1 |
| A6 | Save and load keep pose and both joins; no run state | `SaveAndLoadKeepTheJoins` | none | none | exact |
| A7 | A mouth of another bore does not join | generic: the liquid family's join test, delivered with ENGINE-LIQUID (L6) | — | — | typed refusal |

No wire ports and no parameters, so no wire-connection or parameter refusal row. The requirement's clauses are A2 (unrelated heating) and A3 (residual solid).

## 6. Open questions

Resolved:

1. **Seat relation.** The plug is its own pipe fitting with two mouths (decided now: a "host aperture" relation between two packages would be a new placement capability; the liquid family's mouth snap already joins it to any pipe-bore mouth, D3).
2. **What it blocks.** Liquid in a pipe-bore passage. The delivered ball pipe's bore, 1.3 m across, is not a target: an insert that size would hold 1.7 m³ of ice.
3. **Shrink model.** Open area fraction 1 − f (radial thinning).
4. **Held pressure.** No ejection (decided now: a seat has no body to push; D19).
5. **Game scale.** D5, TH-04 section 11.

Needs the owner: none.

## 7. Integration

- **Works with.** The water family's stores and fittings through the mouth snap (no hose is drawn); TH-04, TH-01, TH-08 (heat to the tab).
- **Campaign.** "Thaw and Order": introduction 68, practice 69; reuse 86, 137. A fixed plug needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES for the static pair); ENGINE-GAS-NETWORK, ENGINE-LIQUID as amended (G1, G2, I6) with EL-001, then ENGINE-THERMAL (owner decision 18); ENGINE-RUN-LENGTH (A2); TH-04; TH-14 (proves the melt row); S7.
- **Files.** `elements/IcePlug/IcePlug.csproj`, `IcePlugElementDeclaration.cs`, `ice_plug.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/IcePlugTests.cs`; a row in `VisibleSolidColliderTests`; the binding section once S7 allows it.
- **Review tier.** Data-only if TH-14 proved the melt row and ENGINE-LIQUID shipped G2 tested; otherwise engine.
- **Disqualifiers.** Seats that cannot read a thermal observation; a fitting that cannot take a source; mouths that need a store behind them.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Four static colliders, one fitting with two mouths, one seat, one node with a melt row, one source, one binding.
2. **Generic laws.** Conduction, the melt row, the aperture law. "Opens when melted" is a seat opening declared as a function of a committed fraction.
3. **Three layers.** Colliders: tab, collar, two mouth tubes. Visuals: their art; the insert is enclosed and decorative. Animation: the insert's scale from the committed fraction.
4. **Flow and typing.** Two liquid mouths with enum roles; f32.
5. **Flat state.** The counts in section 2.
6. **Performance.** Static; four primitive colliders; one node, one row and one seat per tick.
7. **Tests are scenarios.** A1 to A6.

**Verdict: Conforms; blocked on ENGINE-LIQUID and ENGINE-THERMAL, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Not written as main's binding section. **Blocked on the coverage follow-on story S7:** main's reader (`tools/Coverage/NamedPackageSources.cs`) lists only `EL-*.md` files and admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `ice_plug` |
| Named identity | TH-15, `../invest/named-elements.md#thermal-15` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-449` and the ledger entry has no todo link |
| Decision | S579; `scope-corrections.md` on published main has no `s579` block |
| Mode records | `Configuration=Fixed` |
