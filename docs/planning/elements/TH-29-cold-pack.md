# TH-29 · Cold pack — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-29 delivery slice](../invest/named-elements.md#thermal-29). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Units, laws, scale and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14). This spec is a member of the "Sensible stores" family and cites its lead, [TH-26](TH-26-thermal-storage-block.md), for the declaration shape and probe table.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-29 · Cold pack · Thermal |
| Anchors | [requirements.md#thermal-29](../requirements.md#thermal-29) (task `sequence-task-463`); [named-elements.md#thermal-29](../invest/named-elements.md#thermal-29) |
| Owner key / research | S591 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) (row); [TH-S05](../../thermal-component-research.md#th-s05) ("a cold pack is a finite store, not a pump") |
| Related identities | Warm sibling and family lead [TH-26](TH-26-thermal-storage-block.md); contrast [TH-12 Reversible heat pump](TH-12-reversible-heat-pump.md) (active). Cold sink for [TH-13 Freezing mold](TH-13-freezing-mold.md) (TX-02) and [TH-18 Condenser](TH-18-condenser.md). No CAT refines it. |
| Campaign | Intro 66; practice 67; reuse 79, 139 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-CONTACT-OBSERVATIONS (dynamic-against-dynamic touch links); then ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (C2 values, the family's binding delta, Run length). |

### Disposition

**Member of the Family slice "Sensible stores"** with TH-26. It has the lead's declaration shape and differs only in declared values: size, mass, heat capacity, ambient conductance, the range and default of `initial_temperature`, and art. No behaviour, port, mode or capability differs, so it is a family member and not a duplicate (its values and its lesson are its own). Planned package id `cold_pack`, directory `elements/ColdPack/`, display name "Cold pack".

## 2. Declaration

The shape of [TH-26 section 2](TH-26-thermal-storage-block.md#2-declaration), with these values.

- **Interfaces.** `IMechanicalElement`, `ISceneGeometryElement`, and the thermal-node contract of ENGINE-THERMAL.
- **Body.** One dynamic body, 0.5 kg, origin at its centre. Box inertia for 0.4 × 0.16 × 0.3 m: Ixx = 0.5 × (0.16² + 0.3²) ÷ 12 = 0.0048, Iyy = 0.5 × (0.4² + 0.3²) ÷ 12 = 0.0104, Izz = 0.5 × (0.4² + 0.16²) ÷ 12 = 0.0077 kg·m².
- **Collider.** `col_pack`: box, half extents (0.20, 0.08, 0.15) m; the thermal face on every side, 16 W/K (soft face, T11).
- **Material (f32).** Friction 0.5, restitution 0.05, rolling resistance 0.
- **Thermal node.** `store`: heat capacity 437.5 J/K (0.5 kg of water-based gel at 875 J/(kg·K), which is 3500 × ¼ under T11's rule); initial temperature `initial_temperature`; ambient conductance 1.2 W/K (its 0.46 m² surface × 2.5). No phase row, no source, no sink: it is a store.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `initial_temperature` | `ParameterKind.Real` | K (TH-04 R13) | 255 (T14: a domestic freezer) | 233 | 288 | `ParameterEditorPolicy.Clamp`; player-editable unless the level locks it (D11); build-only |

- **Ports.** None. **Stores.** None beyond the node.
- **Presentation bindings.** The lead's pattern (TH-26): `Visual/Mark_0` to `Mark_3` albedo slate → cyan `#66b8c9` when the committed temperature of `store` falls below 281 K, 274 K, 267 K and 260 K (driver of TH-04 R12): four marks lit is a full cold charge, as a count and not only a colour. The earlier scale gauge and frost opacity, a different binding set from the lead's, are withdrawn so that the two members differ only in declared values (audit).
- **Palette.** Category `ElementCategory.Structure`. Order: 529 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Pack` cyan `#66b8c9` | box 0.4 × 0.16 × 0.3 m with a tile pattern printed flush | solid, `col_pack` | the body pose |
| `Visual/Grip` cream `#fff8e9` | a band printed flush across the middle | decorative: printed flat | the body pose |
| `Visual/Mark_0` to `Mark_3` slate / cyan | four graduated bars printed flush on the two long faces | decorative: printed flat, recoloured by a binding | the body pose; colour by the binding |

Icon: a flat tiled tile with one bar. At a glance it differs from the Thermal storage block by being flat and tiled with one gauge instead of four marks.

### Compiled counts

Bodies 1 (dynamic); colliders 1 (box); joints 0; drives 0; sensors 0; thermal nodes 1; faces 1; ports 0; stores 0; presentation bindings 4 (as TH-26).

## 3. Engine capabilities

The probe table of [TH-26 section 3](TH-26-thermal-storage-block.md#3-engine-capabilities) applies row for row: TH-04 R1 and R6 and TH-08 C1 present; H1 (initial temperature from a Real parameter) and TH-04 R9 to R13 missing. This part adds no row. Counts: present 3, untested 0, narrower 0, missing 6, all owned by **ENGINE-THERMAL**. An initial temperature below ambient needs nothing more than H1; the story's fixtures should include one (proposed in the [Batch N summary](readiness/batch-n.md)).

## 4. Sources and legacy

- **Requirement row** [thermal-29](../requirements.md#thermal-29): an authored initially cold finite thermal mass absorbs heat and warms; a store, not an active refrigerator; no variants.
- **Named entry** [thermal-29](../invest/named-elements.md#thermal-29): owner S591; "A warmed pack has depleted cooling capacity; no endless subambient boundary is attached secretly".
- **Research** [TH-S05](../../thermal-component-research.md#th-s05) and per-element table: "finite cold store"; "absorbs heat until spent"; frost ← remaining.
- **Element map** row TH-29: ChemicalReaction, FiniteLedger, GeometryQuery, SensibleHeat, TopologyTransaction, with the note that the inherited ChemicalReaction does not apply and an initially cold sensible store with shared conduction is used. Navigation only (D14).
- **Processes and scenarios.** [IX-43](../requirements.md#interaction-43), [IX-18](../requirements.md#interaction-18); [TX-02](../requirements.md#thermal-scenario-02).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for cold, pack, gel and freezer terms.

## 5. Acceptance

Point of truth: [thermal-29](../requirements.md#thermal-29) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Contact and ambient exchange (T3). The pack can absorb 437.5 × (288 − T) J before it reaches ambient and gives nothing below its own temperature.
- **Capacity.** At 255 K: 437.5 × 33 = 14.4 kJ in all, of which 437.5 × 18 = 7.9 kJ is below water's 273 K freezing point (the share that can freeze anything, TX-02; that chain is [TH-13](TH-13-freezing-mold.md)'s).
- **Alone.** Time constant 437.5 ÷ 1.2 = 365 s: after 60 s it reads 288 − 33 × e^(−60 ÷ 365) = 260.0 K.
- **Cooling a target.** A TH-37 target lying on the pack (8 W/K, 10.5 J/K, 0.45 W/K) follows with time constant 1.24 s toward 288 − 0.947 × 33 = 256.8 K; it is never colder than the pack.
- **Absorbing from a block.** The pack lying on a TH-26 block at 288 K (soft on ceramic, 8 W/K; block 420 J/K, 3.2 W/K to the room). Their difference decays with time constant (420 × 437.5 ÷ 857.5) ÷ 8 = 26.8 s, but the room is not negligible: both bodies are below 288 K and the block's 3.2 W/K and the pack's 1.2 W/K draw heat in throughout (the earlier "20 J" omitted that). Evaluated tick by tick with T9's exact step: at 20 s the block reads **280.0 K** and the pack **264.9 K**; the block has lost 3.36 kJ, the room has given 0.96 kJ, and the pack has gained 4.31 kJ = 3.36 + 0.96 (to rounding).
- **A warmer pack.** Authored at 280 K, the same evaluation gives the block 286.1 K at 20 s, 6.1 K warmer than above.
- **Spent.** On a TH-26 block at 400 K the pair heads for (420 × 400 + 437.5 × 255) ÷ 857.5 = 326 K with the 26.8 s time constant; by 80 s the pack is above 288 K and has no cooling capacity. A pack authored at 288 K cools nothing.
- **Tick order.** T8. **Tolerances.** T12. Stall guard 10 s for the target row, otherwise the stated time.
- **Run length these rows need: 80 s** (C4 observes at 80 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).

### Rows

Planned class `SensibleStoreTests` (shared with TH-26; this identity's rows are C1 to C8).

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| C1 | Given a pack at 255 K with a target lying on it, then within 10 s the target is below 260 K and is never colder than the pack | `AColdPackCoolsWhatRestsOnIt` | a pack authored at 288 K: the target stays at 288 K | `initial_temperature` 288 (the maximum) | control moves less than 0.01 K |
| C2 | Given the pack lying on a 288 K block, when 20 s pass, then the block reads 280 K and the pack 265 K, and the block's enthalpy drop plus the heat drawn from the room equals the pack's gain (about 3.36 + 0.96 = 4.31 kJ) | `AColdPackWarmsByWhatItAbsorbs` | two bodies at 288 K: no change | none | ± 2 K; 2% of 4.3 kJ |
| C3 | Given C2 with the pack authored at 280 K, when 20 s pass, then the block is at least 4 K warmer than in C2 | `AWarmerPackHasLessCapacity` | C2 | none | 4 K margin against a calculated 6.1 K |
| C4 | Given the pack lying on a 400 K block, when 80 s pass, then the pack is above 288 K, and no node in the scene was ever below 255 K | `AWarmedPackIsSpentAndHidesNoColdBoundary` | none | the minimum seen | 0.01 K |
| C5 | Given a pack alone, when 60 s pass, then it reads 260 K | `AnIdlePackWarmsSlowly` | none | none | ± 2 K |
| C6 | Given C2 then Reset, then the pack is at 255 K and the block at 288 K at their authored poses and a second Run repeats C2 | `ResetRestoresTheAuthoredColdStart` | none | none | as C2 |
| C7 | Given a placed pack with `initial_temperature` 240, when saved and loaded, then the value and pose are kept and no run temperature is saved | `SaveAndLoadKeepTheAuthoredColdStart` | none | 233 admitted | exact value |
| C8 | `initial_temperature` below 233 or above 288 is clamped (`ParameterEditorPolicy.Clamp`) | generic: TH-04 R6 | bounds admitted | 233, 288 | clamped value exact |

No port, so no wrong-kind connection row. Requirement clauses: absorbs heat and warms (C2); a warmed pack has depleted capacity (C3, C4); no endless sub-ambient boundary (C4). The TX-02 freezing row belongs to [TH-13](TH-13-freezing-mold.md).

## 6. Open questions

Resolved:

1. **Game scale.** D5, restated once in TH-04 section 11.
2. **Latent gel.** Sensible only (D19): the row says "thermal mass", and a melting fill is TH-27's lesson.
3. **Map row.** ChemicalReaction is not used; conduction is the shared contact link (D14).
4. **Initial temperature authoring.** A parameter, player-editable unless the level locks it (D11).

Needs the owner: none.

## 7. Integration

- **Works with.** TH-26 (family lead), TH-37, TH-13 (TX-02), TH-18, TH-09, TH-14.
- **Campaign.** "Thaw and Order": introduction 66, practice 67; reuse 79, 139. It is taught before TH-26 (81), so the family slice is scheduled by this identity's lesson. Fixed partners need the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Family slice "Sensible stores" with TH-26: two packages, one test class, one review.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-CONTACT-OBSERVATIONS for dynamic-against-dynamic touch links); ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18); ENGINE-RUN-LENGTH; TH-04; TH-37; S7; S3 preferred.
- **Files.** `elements/ColdPack/ColdPack.csproj`, `ColdPackElementDeclaration.cs`, `cold_pack.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); rows C1 to C8 in `CuriousContraptions.tests/SensibleStoreTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if the probe finds every row present and tested; otherwise the engine tier.
- **Disqualifiers.** As TH-26; in addition an engine that refuses an initial temperature below ambient.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** One dynamic body, one box, one node with one face, one parameter, four bindings. No update loop or engine branch on this part.
2. **Generic laws.** Q̇ = G·ΔT on contact and ambient links. "Cools until spent" is a node that starts below ambient.
3. **Three layers.** Collider `col_pack`. Visuals: the pack art with its collider; printed grip and four marks. Animation: mark colours from committed temperature; nothing is written back.
4. **Flow and typing.** No ports; one f32 parameter in kelvin.
5. **Flat state.** The counts in section 2.
6. **Performance.** One primitive collider; thermal work is one node and one term per contact (T10). It rests in use. Island sleeping is owned by ENGINE-ISLANDS-SLEEP (owner decision 3a); a resting pack is stepped every tick until then, and its node keeps exchanging while asleep (T3).
7. **Tests are scenarios.** C1 to C7 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, a general capability stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `cold_pack` |
| Named identity | TH-29, `../invest/named-elements.md#thermal-29` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-463` and the ledger entry has no todo link |
| Decision | S591; `scope-corrections.md` on published main has no `s591` block |
| Mode records | `Configuration=Fixed` |
