# TH-14 · Ice block — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-14 delivery slice](../invest/named-elements.md#thermal-14). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal conventions T1 to T14 and probe rows R1 to R14 are owned by [TH-04](TH-04-electrical-heating-plate.md#thermal-conventions); liquid conventions L1 to L18 by [EL-001](EL-001-finite-reservoir.md). This spec leads the "Ice" specs (TH-15, TH-13, TH-16 cite its rows I1 to I6).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-14 · Ice block · Thermal |
| Anchors | [requirements.md#thermal-14](../requirements.md#thermal-14) (task `sequence-task-448`); [named-elements.md#thermal-14](../invest/named-elements.md#thermal-14) |
| Owner key / research | S578 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | Same water material as [TH-13 Freezing mold](TH-13-freezing-mold.md) contents and [TH-15 Ice plug](TH-15-ice-plug.md), which has a separate placement role. Meltwater joins the water family (Batches F and G; D4). No CAT refines it. |
| Campaign | Intro 67; practice 68; reuse 89, 139 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-CONTACT-OBSERVATIONS (the moving block's touch links); then ENGINE-LIQUID, ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (TH-14 items; its owner item 9 is decided by owner decision 24 of 23 November 2026). |

### Disposition

**To deliver as a single element; lead of the ice specs.** Planned package id `ice_block`, directory `elements/IceBlock/`, display name "Ice block". Not a duplicate of anything delivered.

## 2. Declaration

Follows the delivered Domino for a dynamic box cargo part (`elements/Domino/DominoElementDeclaration.cs`).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), and the node and phase-row contracts ENGINE-THERMAL adds.
- **Body.** One dynamic body: a 0.25 m cube built from four stacked layers. 0.25³ = 0.015625 m³, which at the liquid family's 16 kg/m³ (D2) is 0.25 kg, so ice and its meltwater have the same volume.
- **Colliders (metres).** `col_layer_0` to `col_layer_3`: boxes, half extents (0.125, 0.03125, 0.125), centres at y = 0.03125, 0.09375, 0.15625, 0.21875. Each layer is 0.0625 kg.
- **Material (f32).** Friction 0.05, restitution 0.1, rolling resistance 0 (wet ice).
- **Thermal node** `ice` (T2): heat capacity 131.25 J/K as a solid (0.25 kg at 525 J/(kg·K)); authored initial temperature 253 K; ambient conductance 0.9 W/K (2.5 W/(m²·K) × 0.375 m²); faces: all four layer colliders, 16 W/K.
- **Phase row** (T5): melt at 273 K; latent heat 0.25 kg × 20 875 J/kg = 5219 J; committed solid fraction f from 1 to 0. No freeze direction is declared: meltwater leaves the body.
- **Solid mass.** The body's mass is the node's solid mass, 0.25·f kg; its centre of mass and inertia are recomputed at a tick boundary from the active colliders, that mass spread uniformly over them (T5's mass-frame rule, ENGINE-THERMAL).
- **Collider activation.** `col_layer_3` is active while f > 0.75, `col_layer_2` while f > 0.5, `col_layer_1` while f > 0.25, `col_layer_0` while f > 0. A layer is deactivated at a step boundary, top first, so the base stays in contact with what heats it.
- **Meltwater.** A liquid free end at the base centre, facing −Y, fed by the melted mass ÷ 16 kg/m³: a stream captured by an open store below or sent to `Spilled` (L8). One liquid inventory (D4).
- **Ports, parameters.** None (D13): mass and initial temperature are fixed for this kind.
- **Presentation bindings.** `Visual/Layer_n` is shown over the same interval of f as its collider; `Visual/Edge` (a printed line on the block's front) local Y ← f.
- **Palette.** `ElementCategory.Motion` (melting cargo; TH-04 section 6 item 5). Order: 150 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Layer_0` to `Layer_3` frosted cyan `#66b8c9` | boxes 0.25 × 0.0625 × 0.25, facets printed | solid, `col_layer_n`; art and collider appear and vanish together | body pose; shown by a binding |
| `Visual/Edge` navy `#293954` | flat line printed on the front face | decorative: printed flat, moved by a binding | body pose; presentation binding |
| droplets | the liquid story's shared stream presentation at the free end | decorative: not this package's art | — |

Icon: a faceted cube with a waterline and one drop. It differs from the Domino (a plain slab) by the facets and the stacked-layer joints, and from the Cold pack (TH-29, a tiled block with a grip) by having no grip.

### Compiled counts

Bodies 1 (dynamic); colliders 4 (boxes); joints 0; sensors 0; thermal nodes 1; phase rows 1; liquid free ends 1; ports 0; presentation bindings 5. Four colliders against the Domino's one: the block's support must fall as it melts, and layers that switch off are the general way to do that without resizing a collider during a Run.

## 3. Engine capabilities

### Capability probe

Rows I1 to I6 are the shared ice probe.

| # | Capability | State | Symbol or owner | Test |
| --- | --- | --- | --- | --- |
| TH-04 R1 | Box colliders from package art, visible = solid | present | as TH-04 | as TH-04 (body read) |
| I1 | A small dynamic box body resting and carrying cargo | present, untested here | `ColliderShapeKind.Box`, `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12` | `WorkshopPhysicsLawCylinderTests.BoxesDroppedOntoAFaceStayPutAtRest` (not opened) |
| I2 | A thermal node with an authored initial temperature, faces and ambient exchange | **missing** | none (TH-04 R9 to R11) | ENGINE-THERMAL |
| I3 | A melt row: latent heat at a threshold with a committed solid fraction | **missing** | none | ENGINE-THERMAL ("phase thresholds (melt, freeze, boil, condense) as conserved transfers") |
| I4 | A body's mass and inertia following a node's solid fraction | **missing** | mass frames are fixed or parameter-bound at compile (`MassFrameDeclaration`, used at `elements/Fan/FanElementDeclaration.cs:30`) | ENGINE-THERMAL (T5; its story text is in the Batch N summary) |
| I5 | A collider and its art active over a declared interval of a committed phase scalar, switched at a step boundary, waking the islands it supported | **missing** | none | ENGINE-THERMAL alone (T5). The earlier "row-activation rule ENGINE-FRACTURE delivers" is withdrawn: that story's text has no such rule ([Story ENGINE-FRACTURE](../../../_bmad-output/planning-artifacts/epics.md#story-engine-fracture-load-limited-connections-and-irreversible-break)) |
| TH-04 R10 | Touch links between the moving block and the disc or another face | **missing** | none | ENGINE-THERMAL with ENGINE-CONTACT-OBSERVATIONS (begin- and end-touch, T3) |
| I6 | A liquid free end on a dynamic body fed by a phase row | **missing** | none | ENGINE-LIQUID (L3 source node, L8 stream) |
| TH-04 R12 | Phase-fraction presentation driver | **missing** | as TH-04 | ENGINE-THERMAL |

Counts: present 1, untested 1, narrower 0, missing 7. Each has one owner. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-14](../requirements.md#thermal-14): finite mass and enthalpy; phase evolution alters support and contact geometry and releases the same material as liquid; no variants.
- **Named entry** [thermal-14](../invest/named-elements.md#thermal-14): owner S578; "a cold control retains support; partially melted material cannot disappear or keep impossible full-block support".
- **Research** per-element table: "solid-phase node + body"; parameter mass; "melts into accounted water, shrinks"; scale ← solid fraction.
- **Element map** row TH-14: ContactImpulse, EnvironmentState, FiniteLedger, GasState, GeometryQuery, PhaseTopology, RigidBodyDynamics, SensibleHeat, SolidMelting, TopologyTransaction (D14).
- **Processes and scenarios.** [IX-21](../requirements.md#interaction-21), [IX-41](../requirements.md#interaction-41); [TX-07](../requirements.md#thermal-scenario-07).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for ice, melt, phase and water terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787` (historical coverage-scope rows).

## 5. Acceptance

Point of truth: [thermal-14](../requirements.md#thermal-14), [TX-07](../requirements.md#thermal-scenario-07) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Q̇ = G·ΔT on contact and ambient links (T3); the melt row (T5); contact support from whichever layers are active.
- **Budget.** Warming 253 → 273 K: 131.25 × 20 = 2625 J. Melting: 5219 J. Total 7844 J.
- **On the plate** (TH-04: 120 W, disc 15.25 J/K, 0.5 W/K). Pair 64·16 ÷ 80 = 12.8 W/K. The disc settles where 120 = 0.5·(T_d − 288) + 12.8·(T_d − T_i): T_d = (264 + 12.8·T_i) ÷ 13.3, so the disc gives 0.9624 × (264 − 0.5·T_i) W and the room gives 0.9 × (288 − T_i) W: 513.3 − 1.3812·T_i W in all.
  - Warming: time constant 131.25 ÷ 1.3812 = 95.0 s toward 371.6 K; 273 K at 95.0 × ln(118.6 ÷ 98.6) = 17.5 s.
  - Melting at 273 K: 513.3 − 377.1 = 136.2 W; 5219 ÷ 136.2 = 38.3 s. Layers go at 17.5 + 9.6 = 27.1 s, 36.7 s, 46.3 s and 55.8 s.
  - Battery debit 120 × 55.8 = 6.7 kJ: the level needs `capacity` ≥ 7.2 kJ.
- **Cold control** (plate unsupplied). Disc and room together give 397.8 − 1.3812·T_i W, toward 288 K with the same 95 s: 273 K only at 95 × ln(35 ÷ 15) = 80.5 s; the first layer then needs 1305 J at 20.7 W, 63 s more. All four layers hold for 140 s.
- **Partial melt.** Heat stopped at 40 s leaves f = 1 − (22.5 × 136.2 ÷ 5219) = 0.41: two layers gone, two left, cargo resting 0.125 m above the disc. The room and the cooling disc go on melting it slowly (the third layer needs another 851 J).
- **Mass.** Solid mass + 16 kg/m³ × (liquid captured + `Spilled`) = 0.25 kg at every tick. Because ice and water share one density, a floating block is neutrally buoyant (L12); accepted, no lesson floats ice.
- **Where meltwater goes.** The free end is at the base centre facing −Y, so on the plate (or any solid support under its centre) the stream meets the support at once and is counted `Spilled` (L8): A4's collected amount is zero there. To collect it, the block stands on two rails with a gap under its centre over an open store. On rails in the room alone (0.9 W/K): 273 K at 131.25 ÷ 0.9 × ln(35 ÷ 15) = 124 s; then 0.9 × 15 = 13.5 W melts (240 − 124) × 13.5 = 1566 J by 240 s, f = 1 − 1566 ÷ 5219 = 0.70, so about 0.075 kg of water has reached the store. The whole block would take about 510 s, past the 300 s maximum, so the row asserts collection and the ledger by 240 s, not completion (T12). Whether water landing on a solid support should count as `Spilled` is kept for the owner (section 6).
- **Tick order.** T8: f commits with the tick; a layer switches off at the following step boundary; meltwater moves from the next tick's substeps (L16).
- **Run length these rows need: 120 s** for A1 to A8 (A1's control and A6 hold for 120 s; A1's stall guard is the smaller of 3 × 55.8 and 55.8 + 60 s, 116 s), and **240 s** for A9 (ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).
- **Tolerances.** T12 (times 15%); mass 0.005 kg; positions 0.02 m.

### Rows

Planned class `IceBlockTests`. Heat source: a TH-04 plate on a Battery with `capacity` 14 400 J unless stated.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a block on the supplied plate, then all four layers are present at 15 s and none is left at 70 s | `AHeatedBlockMeltsLayerByLayer` | on an unsupplied plate all four are present at 120 s | none | stall guard 116 s |
| A2 | Given a Tennis ball resting on the block of A1, then by 70 s the ball rests on the disc, 0.25 m lower | `CargoDescendsAsTheBlockMelts` | A1's control: the ball is within 0.01 m of its start at 120 s | none | 0.02 m |
| A3 | Given a Battery `capacity` of 4800 J (heat for 40 s), then at 50 s exactly two layers are active and the ball rests 0.125 m above the disc | `APartlyMeltedBlockGivesPartSupport` | A1 | a layer is active while f is above its threshold, never at it | 0.02 m |
| A4 | Given A1, then at every sampled time solid mass + `Spilled` = 0.25 kg (the stream meets the disc under the block's centre) | `MeltedIceBecomesTheSameMassOfWater` | none | none | 0.005 kg |
| A5 | Given A1, then heat received = 2625 J + 5219 × (1 − f) + the enthalpy carried off by meltwater | `HeatReceivedBalancesSensibleAndLatent` | none | none | 2% |
| A6 | Given a plate heating a different body 1 m away, then the block keeps four layers for 120 s | `HeatingAnotherBodyMeltsNothing` | A1 | none | exact layer count |
| A7 | Reset restores four layers at 253 K, 0.25 kg and no meltwater; a second Run repeats A1 | `ResetRestoresTheWholeBlock` | none | none | as A1 |
| A8 | Save and load keep pose; no run state | `SaveAndLoadKeepThePose` | none | none | exact |
| A9 | Given the block on two Walls 0.10 m apart under its centre, above an open store, in the room, then by 240 s the store holds at least 0.04 kg of water and solid + store + `Spilled` = 0.25 kg at every sample (TX-07) | `MeltwaterFromARailedBlockIsCollected` | the same block on a Wall top under its centre: the store gains nothing | none | 0.005 kg on the ledger |

There are no ports and no parameters, so no connection or parameter refusal row. The requirement's clauses: the cold control is A1's control and A2's; "cannot disappear" is A4; "cannot keep impossible full-block support" is A2 and A3.

## 6. Open questions

Resolved:

1. **Shrink model.** Four layers that switch off, top first (decided now: resizing a collider during a Run is a capability nothing else needs, while switching compiled rows on and off at a step boundary is T5's general rule; support falls in 0.0625 m steps).
2. **Size and mass.** A 0.25 m cube of 0.25 kg, decided now from D2: the earlier 0.4 m cube of 0.25 kg was a quarter of water's density, so its meltwater would have filled a quarter of its volume.
3. **Meltwater.** The liquid family's inventory and free-end rule (D4).
4. **Ambient melting.** Accepted: the cold control holds for 140 s, longer than any lesson needs.
5. **Game scale.** D5, TH-04 section 11.
6. **Mass frame.** Total mass from the solid fraction, distribution from the active colliders (T5), so the ledger in A4 closes while support falls in steps.

7. **Meltwater on a solid support (audit owner item 9; owner decision 24, 23 November 2026).** Water from a free end that lands on a solid support is counted `Spilled` (L8 unchanged), and TX-07 is shown with the block on rails over an open store (A9).

Needs the owner: none.

## 7. Integration

- **Works with.** TH-04, TH-01 (heat); Tennis ball, Receiver (delivered cargo and goal); TH-09 (insulation protects it, lesson 67); the water family's open stores (TX-07).
- **Campaign.** "Thaw and Order": introduction 67, practice 68; reuse 89, 139. A block fixed in place needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element; engine tier as the first user of the melt row.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-CONTACT-OBSERVATIONS for the moving block's links; ENGINE-ISLANDS-SLEEP, if delivered, must wake an island when a layer switches off); ENGINE-GAS-NETWORK and ENGINE-LIQUID (I6), then ENGINE-THERMAL as amended (I2 to I5; owner decision 18); ENGINE-RUN-LENGTH; TH-04; S7.
- **Files.** `elements/IceBlock/IceBlock.csproj`, `IceBlockElementDeclaration.cs`, `ice_block.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/IceBlockTests.cs`; a row in `VisibleSolidColliderTests`; the binding section once S7 allows it.
- **Review tier.** Engine unless the probe finds I2 to I6 present and tested.
- **Disqualifiers.** Phase rows that cannot switch colliders; a mass frame that cannot follow a fraction; a free end that cannot sit on a dynamic body; `VisibleSolidColliderTests` unable to classify art that is hidden with its collider.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Four colliders, one node with a melt row, four activation intervals, one free end, five bindings. No update loop or handler.
2. **Generic laws.** Conduction, the melt row, contact. "Shrinks and lets the ball down" is colliders switching off as a committed fraction falls.
3. **Three layers.** Colliders: four layer boxes. Visuals: four layer meshes, each shown exactly while its collider is active; a printed edge. Animation: visibility and the edge from the committed fraction; nothing is written back.
4. **Flow and typing.** No ports; f32; no string in domain logic.
5. **Flat state.** The counts in section 2; inactive rows stay compiled, so nothing is allocated during a Run.
6. **Performance.** Four boxes on one body; one node, one row. A block at rest could sleep once ENGINE-ISLANDS-SLEEP exists, and a layer switching off must wake it; this spec does not assume sleeping.
7. **Tests are scenarios.** A1 to A8.

**Verdict: Conforms; blocked on ENGINE-THERMAL and ENGINE-LIQUID, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Not written as main's binding section. **Blocked on the coverage follow-on story S7:** main's reader (`tools/Coverage/NamedPackageSources.cs`) lists only `EL-*.md` files and admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `ice_block` |
| Named identity | TH-14, `../invest/named-elements.md#thermal-14` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-448` and the ledger entry has no todo link |
| Decision | S578; `scope-corrections.md` on published main has no `s578` block |
| Mode records | `Configuration=Fixed` |
