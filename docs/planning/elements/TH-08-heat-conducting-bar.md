# TH-08 · Heat-conducting bar — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-08 delivery slice](../invest/named-elements.md#thermal-08). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`), and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-n.md` and the owner decisions of 22 November 2026, and on 23 November 2026 against `docs/planning/elements/readiness/audits/reaudit-group-3.md` (the puck's touch). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Units, laws, scale and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); probe rows "TH-04 R<n>" are in [TH-04 section 3](TH-04-electrical-heating-plate.md#3-engine-capabilities).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-08 · Heat-conducting bar · Thermal |
| Anchors | [requirements.md#thermal-08](../requirements.md#thermal-08) (task `sequence-task-442`); [named-elements.md#thermal-08](../invest/named-elements.md#thermal-08) |
| Owner key / research | S570 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | No CAT refines it. Typical ends: [TH-04](TH-04-electrical-heating-plate.md), [TH-07](TH-07-solar-absorber-plate.md), [TH-26](TH-26-thermal-storage-block.md), [TH-37](TH-37-heat-sensitive-target.md). Lead of the "Conductors" specs: [TH-09](TH-09-insulating-panel.md) and [TH-10](TH-10-finned-heat-sink.md) cite its probe rows. |
| Campaign | Intro 16; practice 17; reuse 55, 82, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS (the touch link table); then ENGINE-THERMAL and ENGINE-RUN-LENGTH. |

### Disposition

**To deliver as a single element** after TH-04 and TH-37. Planned package id `heat_bar`, directory `elements/HeatBar/`, display name "Heat-conducting bar". Not a duplicate; not a Family member (TH-09 and TH-10 differ from it in bodies, nodes and parameters, not only in values).

## 2. Declaration

Follows the delivered Wall shape (one static part with a parameter-driven art binding, `elements/Wall/WallElementDeclaration.cs:11-13`, `:28-30`).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and the thermal-node contract of ENGINE-THERMAL. `KinematicMode.Static`; `PayloadPolicy.None`; zero static mass frame.
- **Body.** One static body, origin at its centre, long axis X. The player places it as a bridge, with each end touching the face it is to join. A contact link exists wherever a face touches another face within the touch tolerance, static pairs included (T3, coordinator decision 21), so a conducting fixture needs no weight to make its contact.
- **Colliders (metres; three coplanar boxes of one section).**

  | Collider | Centre | Half extents | Purpose |
  | --- | --- | --- | --- |
  | `col_end_a` | (−0.50, 0, 0) | (0.10, 0.04, 0.10) | contact end; thermal face of node `end_a` |
  | `col_grip` | (0, 0, 0) | (0.40, 0.04, 0.10) | insulated middle; no thermal face |
  | `col_end_b` | (0.50, 0, 0) | (0.10, 0.04, 0.10) | contact end; thermal face of node `end_b` |

  Three colliders against the Wall's one: a face is a collider (T2), so the two ends must be separate from the middle.
- **Material (f32).** Friction 0.5, restitution 0.1, rolling resistance 0 on all three (a ball may roll along the bar; the Ramp's order of friction).
- **Thermal nodes.** `end_a` and `end_b`: heat capacity 6 J/K each (declared directly, T1), ambient conductance 0.05 W/K each (0.02 m² of bare metal × 2.5), face conductance 64 W/K (metal).
- **Internal link.** `end_a` ↔ `end_b`, declared as a slab (T3): conductivity `conductivity`, section 0.2 × 0.08 = 0.016 m², length 1.2 m, so G = k × 0.016 ÷ 1.2 = 0.01333·k. Copper (400) gives 5.33 W/K; the minimum (5) gives 0.0667 W/K.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `conductivity` | `ParameterKind.Real` | W/(m·K) (a unit ENGINE-THERMAL adds, TH-04 R13) | 400 | 5 | 400 | `ParameterEditorPolicy.Clamp` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:39`); player-editable unless the level locks it (D11); build-only |

  Length and section are fixed (D13).
- **Ports.** None. Heat crosses real touching faces only; neither end names a counterpart.
- **Stores.** None beyond the two nodes.
- **Presentation bindings.** `Visual/Inlay` scale Z ← `conductivity` × (1 ÷ 400) (a parameter-driven binding, the Wall's pattern): a wide gold stripe is a good conductor, a hairline a poor one. `Visual/End_A` and `Visual/End_B` emission ← committed temperature of their node, 288 K to 528 K mapped 0 to 1 (driver of TH-04 R12).
- **Palette.** Category `ElementCategory.Structure`. Order: 508 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/End_A`, `Visual/End_B` gold `#f7cb52` | boxes 0.2 × 0.08 × 0.2 m with two engraved rings printed flush | solid, `col_end_a`, `col_end_b` | nothing |
| `Visual/Grip` cream `#fff8e9` | box 0.8 × 0.08 × 0.2 m, flush with the ends | solid, `col_grip` | nothing |
| `Visual/Inlay` gold `#f7cb52` | flat strip printed along the grip's top | decorative: printed flat, scaled by a parameter binding | width by the binding |

Icon: a long bar with two ringed ends and a centre stripe. At a glance it differs from the Insulating panel (a broad layered slab) by its long narrow form and ringed ends, and from a Ramp by its gold ringed ends.

### Compiled counts

Bodies 1 (static); colliders 3 (box); joints 0; drives 0; sensors 0; thermal nodes 2; faces 2; internal links 1; ports 0; stores 0; presentation bindings 3.

## 3. Engine capabilities

### Capability probe

Rows C2 and C3 are the Conductors rows that TH-09 and TH-10 cite.

| # | Capability | State | Symbol | Exercising test (body read) |
| --- | --- | --- | --- | --- |
| TH-04 R1 | Static box colliders from package art, visible = solid | present | as TH-04 | as TH-04 |
| TH-04 R6 | Parameter defaults and bounds enforced | present | as TH-04 | as TH-04 |
| C2 | A Real parameter drives an art scale; the value survives Reset, save and load | present | `ColliderParameterBinding` and `IParameterizedGeometryElement`, `CuriousContraptions.Abstractions/SceneGeometry.cs:27-33` | `WorkshopWallTests.RealSimulationResetAndSaveLoadRestoreGenericWallAndItsContact` (`CuriousContraptions.tests/WorkshopWallTests.cs:121`) |
| C3 | An internal thermal link declared as a slab (conductivity, area, length; each fixed or a declared Real parameter) | **missing** | none | none; owner ENGINE-THERMAL |
| TH-04 R9, R11 | Thermal nodes; ambient exchange | **missing** | as TH-04 | none; owner ENGINE-THERMAL |
| TH-04 R10 | Touch-based contact links. Static-on-static pairs this part uses: an end on the Heating plate's disc; an end on the Solar absorber's rail; an end on an Insulating panel; an end on a second bar's end. Pairs with a moving member: an end on a TH-37 target, a TH-26 block or a TH-29 pack | **missing** | as TH-04 | none; owner ENGINE-THERMAL |
| TH-04 R12, R13 | Temperature driver; thermal units, including W/(m·K) | **missing** | as TH-04 | none; owner ENGINE-THERMAL |

Counts: present 3, untested 0, narrower 0, missing 6 (C3, R9 to R13). All six have one owner, **ENGINE-THERMAL**; the link table draws its candidates from ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS (owner decision 3a). The story's text in `epics.md` names conduction through declared contacts but not the slab form, the touch rule or the unit; its complete proposed text is in the [Batch N summary](readiness/batch-n.md). Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-08](../requirements.md#thermal-08): conductivity, length, cross-section and actual contacts; no endpoint names a counterpart; no variants.
- **Named entry** [thermal-08](../invest/named-elements.md#thermal-08): owner S570; "An air gap removes solid contact conduction; a low-conductivity comparison transfers less under the same conditions".
- **Research** per-element table: "conduction link"; parameter conductance; "carries heat between touching nodes".
- **Element map** row TH-08: FiniteLedger, GeometryQuery, SensibleHeat, ThermalConduction. Navigation only (D14).
- **Processes.** [IX-18](../requirements.md#interaction-18), [IX-43](../requirements.md#interaction-43); conductor routing is IX-18's second objective (lesson 16).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for conduct, conductance and heat terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787`.

## 5. Acceptance

Point of truth: [thermal-08](../requirements.md#thermal-08) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof through real UI controls is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Contact links at each end (series rule, T3); one internal slab link; ambient at each node; the exact two-node exchange of T9. No rule names what the bar touches.
- **Reference layout (metres, plate origin at x = 0).** The Heating plate's disc top is at y = 0.21 and spans x from −0.20 to 0.20. The bar lies level with its underside at y = 0.21 (centre y = 0.25) and its centre at x = 0.50: `end_a` spans x from −0.10 to 0.10 and lies wholly on the disc (0.20 m of overlap, at least 0.10 m required); the grip passes 0.01 m above the plinth top and is no face; `end_b` spans x from 0.90 to 1.10. The TH-37 target (a puck 0.08 m high, radius 0.24 m) stands at x = 1.00 on a Wall laid flat with `thickness` 0.13 m (admitted from 0.12 m, `elements/Wall/WallElementDeclaration.cs:30`; `width` and `height` 0.6 m), so the puck's top is at 0.13 + 0.08 = 0.21 m and touches `end_b`'s underside at zero clearance, which is within ENGINE-THERMAL's one declared touch tolerance, so the pair is a contact link (T3; a gap wider than that tolerance would give none). The earlier layout, a loose bar sloping from the disc to a target on the floor, is withdrawn: the bar's underside met the plinth's edge and cleared the 0.01 m disc.
- **Reference chain.** A supplied plate (120 W, disc 0.5 W/K to ambient, T14); `end_a` on the disc (metal on metal, 64 × 64 ÷ 128 = 32 W/K); `end_b` on the target (metal on ceramic, 64 × 16 ÷ 80 = 12.8 W/K; target 10.5 J/K, 0.45 W/K to ambient). Write θ for the rise above 288 K. Steady state, copper:
  - target: 12.8·(θ_b − θ_t) = 0.45·θ_t, so θ_t = 12.8 ÷ 13.25·θ_b = 0.966·θ_b and the target takes 0.45 × 0.966 = 0.4347·θ_b;
  - `end_b`: 5.33·(θ_a − θ_b) = (0.05 + 0.4347)·θ_b, so θ_a = (1 + 0.4847 ÷ 5.33)·θ_b = 1.0909·θ_b;
  - `end_a`: 32·(θ_d − θ_a) = 0.05·θ_a + 0.4847·θ_b = 0.5392·θ_b, so θ_d = (1.0909 + 0.5392 ÷ 32)·θ_b = 1.1078·θ_b;
  - disc: 120 = 0.5·θ_d + 0.5392·θ_b = (0.5539 + 0.5392)·θ_b = 1.0931·θ_b, so θ_b = 109.8 K.
  Result: disc 409.6 K, `end_a` 407.8 K, `end_b` 397.8 K, target 394.1 K, with 0.45 × 106.1 = 47.7 W reaching the target. Check: 0.5 × 121.6 + 0.05 × 119.8 + 0.05 × 109.8 + 0.45 × 106.1 = 60.8 + 6.0 + 5.5 + 47.7 = 120 W.
- **Time.** Heat stored at steady state: 15.25 × 121.6 + 6 × 119.8 + 6 × 109.8 + 10.5 × 106.1 = 1854 + 719 + 659 + 1114 = 4346 J, so the lumped time constant is 4346 ÷ 120 = 36 s. The target passes 330 K (θ = 42) after about −36·ln(1 − 42 ÷ 106.1) = 36 × 0.504 = 18 s plus about 3 s of lag through the bar: about 21 s and 2.5 kJ, inside a default Battery. This is a one-time-constant estimate, so rows assert order and thresholds, not a temperature at a time.
- **Low conductivity.** `conductivity` 5 (G = 0.0667): θ_a = (1 + 0.4847 ÷ 0.0667)·θ_b = 8.267·θ_b; θ_d = θ_a + (0.05 × 8.267 + 0.4847) ÷ 32·θ_b = 8.295·θ_b; 120 = 0.5 × 8.295·θ_b + (0.4134 + 0.4847)·θ_b = 5.046·θ_b, so θ_b = 23.8 K and the target settles at 288 + 0.966 × 23.8 = 311 K with 0.45 × 23.0 = 10.3 W arriving. It never reaches 330 K.
- **Air gap.** With the target and its support moved to x = 1.39 the puck spans x from 1.15 to 1.63 and `end_b` ends at 1.10: a 0.05 m gap, far above any touch tolerance, so there is no link and the target stays at 288 K.
- **Why 6 J/K per end.** Declared small so the chain answers within a default Battery; 0.5 kg of copper per end (48 J/K) would need 18 kJ.
- **Tick order.** T8: the face pairs touching at tick N's last substep feed the thermal step of tick N.
- **Run length these rows need: 120 s** (row A3; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).
- **Tolerances.** T12. Stall guard 63 s (3 × 21 s).

### Rows

Planned class `HeatBarTests`. Rows A1 to A5 use the reference layout and a Battery with `capacity` 7200 J (60 s at 120 W).

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given plate, bar and target in the reference layout, when the plate is supplied, then the target passes 330 K within 63 s, and at that moment disc > `end_a` > `end_b` > target | `ABarCarriesHeatFromThePlateToTheTarget` | the same scene without the bar: the target stays at 288 K | `conductivity` 400 (the maximum) | control moves less than 0.01 K |
| A2 | Given A1 with the target 0.05 m beyond `end_b`, then after 63 s the target has not warmed | `AnAirGapCarriesNoHeat` | A1 | none | less than 0.01 K |
| A3 | Given A1 with `conductivity` 5 and a 120 s Run, then the target never reaches 330 K, and at 60 s its rise is less than half the copper bar's at 60 s | `ALowConductivityBarTransfersLess` | A1 | `conductivity` 5 (the minimum) | as stated |
| A4 | Given A1 with the bar turned end for end, then the target still passes 330 K within 63 s | `EitherEndConductsAndNeitherNamesACounterpart` | A1 | none | as A1 |
| A5 | Given A1 at 60 s, then the Battery's debit equals the heat stored in the four nodes plus the ambient account | `HeatThroughTheBarIsConserved` | an unsupplied scene: debit 0 J, every node at 288 K | none | 2% of the debit |
| A6 | Given A1 then Reset, then every node is at 288 K and a second Run repeats A1 | `ResetRestoresTheBarsNodes` | none | none | as A1 |
| A7 | Given a placed bar with `conductivity` 50, when saved and loaded, then the value and pose are kept and no run temperature is saved | `SaveAndLoadKeepConductivityAndNoRunState` | none | none | exact value |
| A8 | `conductivity` below 5 or above 400 is clamped by the editor (`ParameterEditorPolicy.Clamp`) and refused at admission | generic: `PackageRuntimeParityTests.EveryPackageDeclaresTheRuntimeParameterDefaultsAndBounds` (TH-04 R6) | 5 and 400 admitted | 5, 400 | typed refusal |

No port, so no wrong-kind connection row. The requirement's two controls are A2 (air gap) and A3 (low conductivity under the same conditions).

## 6. Open questions

Resolved:

1. **Game scale.** D5, restated once in TH-04 section 11.
2. **Comparison material.** A Real `conductivity` parameter on this part, decided now: the row asks for a comparison "under the same conditions", which only the same shape gives; a second package or TH-09 would change shape and contact.
3. **Dynamic or static.** Static (coordinator decision 21): thermal links come from the touch-based link table, so a conducting fixture is a static part placed with its faces touching. The earlier dynamic bar, made loose only to obtain a contact, is withdrawn.
4. **Length.** Fixed 1.2 m (D13): it spans a plate and a part a Basketball's width away.

Needs the owner: none.

## 7. Integration

- **Works with.** TH-04 and TH-37 (planned before it), TH-07 (its rail takes a bar end), TH-09, TH-26, TH-05 (brake heat carried onward); Battery and Wall (delivered).
- **Campaign.** "Conduct Yourself": introduction 16, practice 17; reuse 55, 82, 145. A level that shows a poor conductor failing declares a Run length of at least 120 s; the positive lesson fits in 63 s. A level that fixes the bar or its partners in place needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS for the link table); ENGINE-GAS-NETWORK, then ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18); ENGINE-RUN-LENGTH (row A3); TH-04; TH-37; S7; S3 preferred.
- **Files.** `elements/HeatBar/HeatBar.csproj`, `HeatBarElementDeclaration.cs`, `heat_bar.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); `CuriousContraptions.tests/HeatBarTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds C3 and R9 to R13 present and tested; otherwise the engine tier.
- **Disqualifiers.** ENGINE-THERMAL delivered without a parameter-bound slab link (then `conductivity` becomes a two-option Choice selecting one of two fixed links); without static-pair links (the bar on the disc would not conduct); with faces per body instead of per collider (the grip would conduct); a different scale (re-derive section 5); the Workbench refusing a Wall laid flat (then the reference layout uses another 0.13 m support).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** One static body, three boxes, two nodes, one slab link, one parameter, three bindings. No update loop or engine branch on this part.
2. **Generic laws.** Q̇ = G·ΔT on contact, internal and ambient links. "Carries heat from A to B" is the consequence of two faces and a link.
3. **Three layers.** Colliders: three `col_` boxes on the body. Visuals: end and grip art, each with its collider; a printed inlay. Animation: inlay width from the parameter, end glow from committed temperature; nothing is written back.
4. **Flow and typing.** No ports; one f32 parameter; typed node ids.
5. **Flat state.** The counts in section 2, fixed at compile time.
6. **Performance.** A static body with three primitive colliders, each needed as a face boundary; no sensor. Thermal work is two nodes, one internal link and one term per live pair on an end, held in the link table (T10); static pairs are resolved once at Run.
7. **Tests are scenarios.** A1 to A7 run the production simulation on the general exchange law.

**Verdict: Conforms; blocked on ENGINE-THERMAL, a general capability stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `heat_bar` |
| Named identity | TH-08, `../invest/named-elements.md#thermal-08` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-442` and the ledger entry has no todo link |
| Decision | S570; `scope-corrections.md` on published main has no `s570` block |
| Mode records | `Configuration=Fixed` |
