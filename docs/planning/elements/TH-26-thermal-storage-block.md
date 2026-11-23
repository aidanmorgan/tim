# TH-26 · Thermal storage block — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-26 delivery slice](../invest/named-elements.md#thermal-26). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Units, laws, scale and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); probe rows "TH-04 R<n>" and "TH-08 C<n>" are in [TH-04 section 3](TH-04-electrical-heating-plate.md#3-engine-capabilities) and [TH-08 section 3](TH-08-heat-conducting-bar.md#3-engine-capabilities).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-26 · Thermal storage block · Thermal |
| Anchors | [requirements.md#thermal-26](../requirements.md#thermal-26) (task `sequence-task-460`); [named-elements.md#thermal-26](../invest/named-elements.md#thermal-26) |
| Owner key / research | S569 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | Cold sibling [TH-29 Cold pack](TH-29-cold-pack.md) (same family); latent sibling [TH-27](TH-27-phase-change-cartridge.md). The "stored-hot block" source of TX-01 with [TH-17 Kettle](TH-17-kettle.md); the noncombustible control for [TH-03](TH-03-combustible-block.md); the hot store of TX-11 with [TH-30](TH-30-thermoelectric-generator.md). No CAT refines it. |
| Campaign | Intro 81; practice 82; reuse 90, 140 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-CONTACT-OBSERVATIONS (dynamic-against-dynamic touch links); then ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (Run length; the family's binding pattern, now shared by TH-29). |

### Disposition

**Lead of the Family slice "Sensible stores"** with TH-29. The two members are one declaration shape (a dynamic box, one node with one face, one `initial_temperature` parameter, four temperature-threshold marks) and differ only in declared values: size, mass, heat capacity, ambient conductance, the parameter's range and default, the four mark thresholds and colours, and art. Planned package id `thermal_block`, directory `elements/ThermalBlock/`, display name "Thermal storage block". Not a duplicate.

## 2. Declaration

Follows the delivered Domino shape for a dynamic box part (`elements/Domino/DominoElementDeclaration.cs:9`, `:47`) and the Battery's threshold marks (`elements/Battery/BatteryElementDeclaration.cs:15-26`).

- **Interfaces.** `IMechanicalElement`, `ISceneGeometryElement`, and the thermal-node contract of ENGINE-THERMAL.
- **Body.** One dynamic body, 2.0 kg ("heavy": between the 1 kg Basketball and the 4 kg Bowling ball), origin at its centre. Box inertia for 0.6 × 0.4 × 0.4 m: Ixx = 2 × (0.4² + 0.4²) ÷ 12 = 0.0533, Iyy = Izz = 2 × (0.6² + 0.4²) ÷ 12 = 0.0867 kg·m².
- **Collider.** `col_block`: box, half extents (0.30, 0.20, 0.20) m; the thermal face on every side, 16 W/K (ceramic).
- **Material (f32).** Friction 0.6, restitution 0.05, rolling resistance 0.
- **Thermal node.** `store`: heat capacity 420 J/K (2 kg of ceramic at 210 J/(kg·K), T14); initial temperature `initial_temperature`; ambient conductance 3.2 W/K (its 1.28 m² surface × 2.5).
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `initial_temperature` | `ParameterKind.Real` | K (TH-04 R13) | 288 | 288 | 500 | `ParameterEditorPolicy.Clamp`; player-editable unless the level locks it (D11); build-only. 500 K is below wood's 573 K ignition |

- **Ports.** None; heat enters and leaves through contacts. **Stores.** None beyond the node.
- **Presentation bindings.** `Visual/Mark_0` to `Mark_3` albedo navy → gold when the committed temperature of `store` passes 316 K, 344 K, 372 K and 400 K (driver of TH-04 R12): four marks lit is a full charge, as a count and not only a colour.
- **Palette.** Category `ElementCategory.Structure`. Order: 539 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Block` cream `#fff8e9` | box 0.6 × 0.4 × 0.4 m, edges chamfered inside the collider | solid, `col_block` | the body pose |
| `Visual/Mark_0` to `Mark_3` navy / gold `#f7cb52` | four graduated bars printed flush on the two long faces | decorative: printed flat, recoloured by a binding | the body pose; colour by the binding |

Icon: a prism with four graduated bars. At a glance it differs from the Cold pack (a flat tiled tile with one gauge) by its tall prism and four bars, and from the Domino by its bulk.

### Compiled counts

Bodies 1 (dynamic); colliders 1 (box); joints 0; drives 0; sensors 0; thermal nodes 1; faces 1; ports 0; stores 0; presentation bindings 4.

## 3. Engine capabilities

### Capability probe

Row H1 is the family's own row; TH-29 cites this table.

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R6 | Colliders from art; parameter bounds | present | as TH-04 | as TH-04 |
| TH-08 C1 | A dynamic body with a declared mass frame | present | as TH-08 | as TH-08 |
| H1 | A node's initial temperature bound to a declared Real parameter and restored on Reset | **missing** | none | none; owner ENGINE-THERMAL |
| TH-04 R9, R10, R11, R12, R13 | Node, contact conduction, ambient exchange, temperature driver, kelvin unit | **missing** | as TH-04 | none; owner ENGINE-THERMAL |

Counts: present 3, untested 0, narrower 0, missing 6. All six have one owner, **ENGINE-THERMAL** ("declared heat capacity and temperature per body"; the parameter binding of H1 is proposed text in the [Batch N summary](readiness/batch-n.md)). Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-26](../requirements.md#thermal-26): a high-heat-capacity body stores sensible enthalpy and can be transported to release it through ordinary heat exchange; no variants.
- **Named entry** [thermal-26](../invest/named-elements.md#thermal-26): owner S569; "An equal-temperature block supplies no net heat; repeated deliveries deplete its stored difference".
- **Research** per-element table: "high-capacity node"; parameters mass, c; "stays warm after the source stops"; tint ← T.
- **Element map** row TH-26: FiniteLedger, GeometryQuery, SensibleHeat, ThermalConduction. Navigation only (D14).
- **Processes and scenarios.** [IX-43](../requirements.md#interaction-43), [IX-18](../requirements.md#interaction-18); [TX-01](../requirements.md#thermal-scenario-01), [TX-11](../requirements.md#thermal-scenario-11).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for storage, enthalpy and heat terms.

## 5. Acceptance

Point of truth: [thermal-26](../requirements.md#thermal-26) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Contact and ambient exchange (T3). The block holds H = 420 × (T − 288) J above ambient and gives it up only down a temperature difference.
- **Stored heat.** Authored at 400 K: 420 × 112 = 47 kJ.
- **Alone.** Time constant 420 ÷ 3.2 = 131 s: from 400 K it reads 288 + 112 × e^(−30 ÷ 131) = 377 K after 30 s. It "stays warm".
- **Why the hot start is authored.** On a 120 W Heating plate (pair 12.8 W/K): 12.8·(θ_d − θ_b) = 3.2·θ_b gives θ_d = 1.25·θ_b; 120 = 0.5 × 1.25·θ_b + 3.2·θ_b = 3.825·θ_b, so the block can only reach 288 + 31.4 = 319 K, slowly. A stored-hot block is therefore a level setting.
- **Warming a target.** A TH-37 target lying on the block (ceramic on ceramic, 8 W/K; 10.5 J/K; 0.45 W/K): it follows with time constant 10.5 ÷ 8.45 = 1.24 s toward 8 ÷ 8.45 = 0.947 of the block's rise, and passes 330 K at −1.24·ln(1 − 42 ÷ 106) = 0.63 s. It stays above 330 K until the block's rise falls to 42 ÷ 0.947 = 44.4 K: 420 ÷ (3.2 + 0.426) × ln(112 ÷ 44.4) = 107 s.
- **Depletion.** A block at 400 K resting on an identical block at 288 K (8 W/K). Their difference decays with time constant 420 ÷ (2 × 8) = 26.25 s and their mean with 131 s. At 10 s: mean rise 56 × e^(−10 ÷ 131) = 51.9 K, difference 112 × e^(−10 ÷ 26.25) = 76.5 K, so the hot block reads 378.2 K and the cold one 301.6 K. The cold block gained 420 × 13.6 = 5.7 kJ; the hot one lost 420 × 21.8 = 9.2 kJ; the 3.5 kJ between them went to ambient (3.2 × 2 × 56 × 131 × (1 − e^(−0.0762)) = 3.45 kJ). A block already depleted to 344 K gives the cold block half as much: 294.8 K at 10 s.
- **Equal temperatures.** A block at 288 K against anything at 288 K moves no heat.
- **Tick order.** T8. **Tolerances.** T12. Stall guard 10 s for the target row, otherwise the stated time.
- **Run length these rows need: 60 s** (B6 observes for 60 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s). The 107 s "stays above 330 K" figure is a lesson statement, not a row.

### Rows

Planned class `SensibleStoreTests` (shared with TH-29; this identity's rows are B1 to B8).

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| B1 | Given a block authored at 400 K with a target lying on it, then the target passes 330 K within 10 s and is never hotter than the block | `AHotBlockWarmsWhatRestsOnIt` | a block at 288 K: the target stays at 288 K | `initial_temperature` 288 (the minimum) | control moves less than 0.01 K |
| B2 | Given a 400 K block resting on a 288 K block, when 10 s pass, then they read 378 K and 302 K | `HeatFlowsFromTheHotBlockToTheColdOne` | two blocks at 288 K: no change | none | ± 2 K |
| B3 | Given B2 with the upper block authored at 344 K, when 10 s pass, then the lower block reads 295 K, about half the gain of B2 | `ADepletedBlockDeliversLess` | B2 | none | ± 2 K |
| B4 | Given B2 at 10 s, then the hot block's enthalpy drop equals the cold block's gain plus the ambient account | `DeliveredHeatIsConserved` | none | none | 2% of 9.2 kJ |
| B5 | Given a 400 K block alone, when 30 s pass, then it reads 377 K | `AStoredBlockStaysWarm` | none | none | ± 2 K |
| B6 | Given B2 over 60 s, then the cold block is never above the hot one and neither is below 288 K | `ABlockNeverCoolsBelowItsColdestNeighbour` | none | the minimum seen | 0.01 K |
| B7 | Given B2 then Reset, then the blocks are at 400 K and 288 K at their authored poses and a second Run repeats B2 | `ResetRestoresAuthoredTemperatures` | none | none | as B2 |
| B8 | Given a placed block with `initial_temperature` 450, when saved and loaded, then the value and pose are kept and no run temperature is saved | `SaveAndLoadKeepTheAuthoredTemperature` | none | 500 admitted | exact value |
| B9 | `initial_temperature` below 288 or above 500 is clamped (`ParameterEditorPolicy.Clamp`) | generic: TH-04 R6 | bounds admitted | 288, 500 | clamped value exact |

No port, so no wrong-kind connection row. Requirement clauses: equal temperature gives nothing (B1 and B2 controls); repeated deliveries deplete (B3); transport and ordinary exchange (B1, B2). The TX-01 kettle row is in [TH-17](TH-17-kettle.md#batch-n-chains).

## 6. Open questions

Resolved:

1. **Game scale.** D5, restated once in TH-04 section 11.
2. **Authored hot start.** Yes, by the `initial_temperature` parameter (D11), decided now: no attainable source charges it within a Run (319 K at most on the plate).
3. **Surface loss.** Modelled as the node's ambient conductance (T3); the map row's omission is navigation only (D14).

Needs the owner: none.

## 7. Integration

- **Works with.** TH-37 (planned before it), TH-29 (sibling), TH-17 (TX-01), TH-30 (TX-11), TH-10, TH-09; Conveyor (delivered) to carry it.
- **Campaign.** Introduction 81, practice 82; reuse 90, 140. A level authors the temperature and may lock it; fixed partners need the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Family slice "Sensible stores" (TH-26, TH-29): two packages, one test class, one review.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-CONTACT-OBSERVATIONS for dynamic-against-dynamic touch links); ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18); ENGINE-RUN-LENGTH; TH-04; TH-37; S7; S3 preferred. This slice also owns the cross-part rows of parts taught before the block (81) that use it as a heat receiver: TH-01 A4, TH-02 B4, TH-05 H6 and TH-12 A5.
- **Files.** `elements/ThermalBlock/ThermalBlock.csproj`, `ThermalBlockElementDeclaration.cs`, `thermal_block.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); `CuriousContraptions.tests/SensibleStoreTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if the probe finds every row present and tested; otherwise the engine tier.
- **Disqualifiers.** No parameter-bound initial temperature (the hot start then becomes a two-option Choice of fixed temperatures); a different scale (re-derive section 5); a member found to need a capability the other does not (it then leaves the family).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** One dynamic body, one box, one node with one face, one parameter, four bindings. No update loop or engine branch on this part.
2. **Generic laws.** Q̇ = G·ΔT on contact and ambient links. "Stores heat and releases it" is a large declared capacity.
3. **Three layers.** Collider `col_block`. Visuals: the block art with its collider; printed marks. Animation: mark colour from committed temperature; nothing is written back.
4. **Flow and typing.** No ports; one f32 parameter in kelvin.
5. **Flat state.** The counts in section 2.
6. **Performance.** One primitive collider; thermal work is one node and one term per contact (T10). It rests in use. Island sleeping is owned by ENGINE-ISLANDS-SLEEP (owner decision 3a); a resting block is stepped every tick until then, and its node keeps exchanging while asleep (T3).
7. **Tests are scenarios.** B1 to B8 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, a general capability stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `thermal_block` |
| Named identity | TH-26, `../invest/named-elements.md#thermal-26` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-460` and the ledger entry has no todo link |
| Decision | S569; `scope-corrections.md` on published main has no `s569` block |
| Mode records | `Configuration=Fixed` |
