# TH-07 · Solar absorber plate — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-07 delivery slice](../invest/named-elements.md#thermal-07). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Units, laws, scale and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); light follows the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions); probe rows "TH-04 R<n>" and "TH-08 C<n>" are in [TH-04 section 3](TH-04-electrical-heating-plate.md#3-engine-capabilities) and [TH-08 section 3](TH-08-heat-conducting-bar.md#3-engine-capabilities).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-07 · Solar absorber plate · Thermal |
| Anchors | [requirements.md#thermal-07](../requirements.md#thermal-07) (task `sequence-task-441`); [named-elements.md#thermal-07](../invest/named-elements.md#thermal-07) |
| Owner key / research | S573 · [TH-S04 optical heating](../../thermal-component-research.md#th-s04) |
| Related identities | Distinct from the Solar panel ([CAT-059](CAT-059-solar_panel.md), [EL-211](../invest/named-elements.md#element-211)), which turns light into electrical supply; this plate turns it into heat. Sources: [CAT-029 Flashlight](CAT-029-flashlight.md), [CAT-036 Laser](CAT-036-laser.md), [TH-06 Converging lens](TH-06-converging-lens.md). Onward routing by [TH-08](TH-08-heat-conducting-bar.md); readout by [TH-21](TH-21-temperature-sensor.md). No CAT refines it. |
| Campaign | Intro 55; practice 56; reuse 69, 131 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES (the optical segment cast and static pairs); then ENGINE-OPTICAL, ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (the unrepaired receiver law, tick phase, convention numbers, the visual clause) and coordinator decisions 16 and 21. |

### Disposition

**To deliver as a single element** after ENGINE-OPTICAL, the Flashlight and TH-04. Planned package id `solar_absorber`, directory `elements/SolarAbsorber/`, display name "Solar absorber plate". Not a duplicate of the Solar panel: it has no electrical port and its output is heat at a thermal face.

## 2. Declaration

Follows the delivered Wall for a static part with a parameter-driven art binding (`elements/Wall/WallElementDeclaration.cs:8`, `:11-13`); its light side follows the receiver shape ENGINE-OPTICAL delivers (optical convention 8).

- **Interfaces.** `ISceneGeometryElement`, the optical receiver contract of ENGINE-OPTICAL and the thermal-node contract of ENGINE-THERMAL. `KinematicMode.Static`; zero static mass frame.
- **Body.** One static body; origin at the centre of the base's underside; the absorbing face looks along +X.
- **Colliders (metres).**

  | Collider | Centre | Half extents | Purpose |
  | --- | --- | --- | --- |
  | `col_base` | (0, 0.02, 0) | (0.12, 0.02, 0.15) | the foot; no thermal face |
  | `col_slab` | (0, 0.19, 0) | (0.04, 0.15, 0.15) | the upright slab; its +X face is the optical aperture; a radiant face of `plate` that conducts nothing (face conductance 0, absorptance `absorptance`) |
  | `col_rail` | (0, 0.35, 0) | (0.06, 0.01, 0.15) | a gold rail along the top, 0.36 m high; the thermal face |

  The rail is where a Heat-conducting bar's end or a Temperature sensor's probe rests: loose parts make real contacts on it (T3). Three boxes against the Wall's one, because only the rail may conduct.
- **Material (f32).** Friction 0.6, restitution 0.05, rolling resistance 0.
- **Optical receiver.** Aperture: the +X face of `col_slab`, 0.30 × 0.30 m, area 0.09 m²; five sample points of weight 0.2 each (the centre and four points offset 0.075 m both ways, 0.106 m from it); every channel counted; no threshold. Its committed reading R, in watts, is the receiver law's (optical convention 8): collimated power ending on the aperture plus its conserved share of each diverging segment, the uncapped share A × Σ wᵢ·Eᵢ scaled down, with every other receiver of that segment, so that the receivers together never take more than the segment's power (coordinator decision 16). The reading can never exceed what the source emits. The earlier "min(P, A × Σ wᵢ·Eᵢ)" per receiver is withdrawn: two receivers in one cone could each take P.
- **Thermal node.** `plate`: heat capacity 22.5 J/K (0.1 kg of aluminium at 225 J/(kg·K)); ambient conductance 0.3 W/K (0.12 m² of exposed front and rail × 2.5; the back is insulated); faces `col_rail`, 64 W/K (metal), and `col_slab`, 0 W/K with absorptance `absorptance` (a radiant face only: a contact link through it moves nothing, so only the rail conducts). Each tick `absorptance` × the light the optical solve booked on `col_slab`'s absorbed record moves to the node (T3, radiation; booked once, coordinator decision 26). The receiver's aperture is on the slab's Absorb face, so the record holds what the receiver received (optical convention 8), and in every row here that is the reading R: a diverging segment books only through receivers. The remaining (1 − `absorptance`) × R stays on the record and is not heat; the reading itself credits nothing.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `absorptance` | `ParameterKind.Real` | `ParameterUnit.None` (a fraction) | 0.95 | 0.05 | 0.95 | player-editable unless the level locks it (D11); build-only |

- **Ports.** None: the aperture is spatial and the thermal face is a contact. **Stores.** None beyond the node.
- **Presentation bindings.** `Visual/Sheen` scale Y ← 1 − `absorptance` (parameter-driven, the Wall's pattern): a tall bank of gold chevrons marks a reflective plate, a sliver a matte one. `Visual/Mark_0` to `Mark_2` albedo navy → gold when the committed temperature of `plate` passes 300 K, 330 K and 360 K (driver of TH-04 R12).
- **Palette.** Category `ElementCategory.Power` (a converter, as the Solar panel is). Order: 319 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Base`, `Visual/Slab` cream `#fff8e9` | boxes as the colliders | solid, `col_base`, `col_slab` | nothing |
| `Visual/Rail` gold `#f7cb52` | box 0.12 × 0.02 × 0.30 m | solid, `col_rail` | nothing |
| `Visual/Inset` navy `#293954` with a gold rim | a square printed flush on the +X face | decorative: printed flat | nothing |
| `Visual/Sheen` gold | chevrons printed flush across the inset | decorative: printed flat, scaled by a parameter binding | the binding |
| `Visual/Mark_0` to `Mark_2` | three bars engraved flush on the slab's side | decorative: printed flat, recoloured by a binding | the binding |

Icon: an upright square with a dark inset and a top rail. At a glance it differs from the Solar panel by its top rail and three side bars and by having no socket.

### Compiled counts

Bodies 1 (static); colliders 3 (box); joints 0; drives 0; sensors 0; optical receivers 1 (5 sample points); thermal nodes 1; faces 2 (one conducting, one radiant); ports 0; stores 0; presentation bindings 4.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R6 | Colliders from art; parameter bounds | present | as TH-04 | as TH-04 |
| TH-08 C2 | A Real parameter drives an art scale | present | as TH-08 | as TH-08 |
| O1 | An optical receiver on a collider face with weighted sample points and a continuous committed reading in watts, bounded by the emitted power, zero when occluded | **missing** | none: no beam, ray or optical contract exists in the engine projects | none; owner ENGINE-OPTICAL |
| O2 | A face's declared (fixed or parameter-bound) absorptance share of the light booked on its collider's absorbed record, moved to the face's node each tick (T3, radiation) | **missing** | none | none; owner ENGINE-THERMAL |
| TH-04 R9, R10, R11, R12 | Node, contact conduction at the rail, ambient exchange, temperature driver | **missing** | as TH-04 | none; owner ENGINE-THERMAL |

Counts: present 3, untested 0, narrower 0, missing 6. Owners, one each: O1 is **ENGINE-OPTICAL** (the receiver with sample points and a continuous reading is in Batch H's and Batch K's proposed additions to that story); O2 and R9 to R12 are **ENGINE-THERMAL** (O2 is proposed text in the [Batch N summary](readiness/batch-n.md); Batch K's decision K-10 leaves absorbed light as a ledger entry until then). Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-07](../requirements.md#thermal-07): absorbed optical flux becomes internal energy according to surface absorptance and illuminated area; a thermal port can carry it onward; no variants.
- **Named entry** [thermal-07](../invest/named-elements.md#thermal-07): owner S573; "A reflective comparison absorbs less; shading removes the input without immediately deleting stored heat".
- **Research** per-element table: "node with absorptance + optical receiver"; "warms under a Flashlight/laser or lens spot"; tint ← T.
- **Element map** row TH-07: FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, ThermalConduction. Navigation only (D14).
- **Processes and scenarios.** [IX-13](../requirements.md#interaction-13), [IX-14](../requirements.md#interaction-14), [IX-18](../requirements.md#interaction-18), [IX-20](../requirements.md#interaction-20); [TX-03](../requirements.md#thermal-scenario-03) source side.
- **Legacy (consulted, no absorber).** Legacy optics recorded "Absorb" as a reception of power with no heat (`engine/OpticalNetwork.cs@a6c914e:L109-L111`): carry forward only that absorbed power ends the ray; do not carry forward the CPU trace. The legacy flashlight cone (range 8, 15° half-angle, intensity 24, `parts/FlashlightPart.cs@a6c914e:L10-L13`) is superseded by the optical conventions' 90 W; the earlier reading of it as 24 W is withdrawn.

## 5. Acceptance

Point of truth: [thermal-07](../requirements.md#thermal-07) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** The optical receiver law (optical convention 8), which books what the receiver receives once on `col_slab`'s record; `absorptance` × that record moved to the node (T3, T4); ambient and contact exchange (T3). Heat is never more than the light received, and light never more than the source paid for (optical convention 2).
- **Flashlight at 1 m, square on** (T14: 90 W over 15°, radiant intensity 420.4 W/sr; demand 100 W). Irradiance 420.4 ÷ 1² = 420.4 W/m² (at 1 m the flat near-field rule and the d² law agree, optical convention 1). The cone is tan 15° × 1 = 0.268 m in radius, so all five samples (the farthest 0.106 m from the axis) are lit. The uncapped share is 0.09 × 420.4 = 37.8 W, under the 90 W of the segment, and the plate is its only receiver, so R = 37.8 W is booked on `col_slab`'s record; 0.95 × 37.8 = 35.9 W moves to the node and 1.9 W stays on the record.
- **Warm-up.** Time constant 22.5 ÷ 0.3 = 75 s; final rise 35.9 ÷ 0.3 = 119.7 K (408 K, below every ignition threshold of T11). At 20 s: 119.7 × (1 − e^(−20 ÷ 75)) = 28.0 K, so 316 K; absorbed 35.9 × 20 = 718 J = stored 22.5 × 28.0 = 630 J + ambient 88 J. It passes 300 K at −75·ln(1 − 12 ÷ 119.7) = 7.9 s. A default Battery runs the Flashlight for 3600 ÷ 100 = 36 s.
- **Distance.** At 3 m the irradiance is 46.7 W/m²: R = 4.2 W, absorbed 4.0 W, final temperature 288 + 13.3 = 301 K. Bringing the light closer, or concentrating it with [TH-06](TH-06-converging-lens.md), is what pays.
- **Reflective comparison.** `absorptance` 0.05: absorbed 1.9 W, final rise 6.3 K; at 20 s the rise is 6.3 × 0.234 = 1.5 K against 28 K.
- **Shading.** A Wall between light and plate from the start: R = 0 and the plate stays at 288 K. A Wall covering half the face blocks the samples behind it, so R is between two and three fifths of the open value. When the light stops (a 2000 J Battery empties at 20 s) the plate is at 316 K and 10 s later still at 288 + 28 × e^(−10 ÷ 75) = 312.5 K: the input ends, the stored heat does not.
- **Carried onward.** A TH-08 bar from the rail (metal on metal, 32 W/K) to a TH-37 target: with TH-08's chain algebra, the bar and target take 0.5392·θ_b and the plate sits at 1.1078·θ_b; 35.9 = 0.3 × 1.1078·θ_b + 0.5392·θ_b = 0.8715·θ_b, so θ_b = 41.2 K and the target settles at 288 + 0.966 × 41.2 = 328 K. Stored heat at that state is 22.5 × 45.6 + 6 × 44.9 + 6 × 41.2 + 10.5 × 39.8 = 1960 J, a time constant of 1960 ÷ 35.9 = 55 s; at 30 s the target has risen about 39.8 × (1 − e^(−27 ÷ 55)) = 15 K.
- **Tick order.** The optical solve of tick N runs after the last substep, books R on `col_slab`'s record and commits the reading ([combined tick order](EL-176-red-laser-emitter.md#combined-tick-order)); the thermal step of tick N, which follows it, moves `absorptance` × that record to the node (T3, T8). **Tolerances.** T12 for temperatures and energy; the optical set of coordinator decision 21 (power ± 2%) for readings. Rows are fixed-time observations.
- **Run length these rows need: 35 s** (A5 observes 10 s after a 20 s light, A7 at 30 s; a level showing the full default Battery's 36 s of light declares more; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).

### Rows

Planned class `SolarAbsorberTests`. The Flashlight is 1 m from the face, square on, fed by a default Battery unless stated.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the lit plate, when 20 s pass, then it reads 316 K | `ALitPlateWarmsByTheLightItAbsorbs` | the Flashlight off: 288 K | none | ± 2 K; control within 0.01 K |
| A2 | Given A1, then the heat stored plus the ambient account equals `absorptance` × the light booked on `col_slab`'s record (here the summed reading), the rest stays on that record, and the summed reading is at most the Flashlight's 90 W × 20 s | `AbsorbedHeatNeverExceedsReceivedLight` | none | reading against emitted power | 2% of 718 J |
| A3 | Given A1 with `absorptance` 0.05, when 20 s pass, then the rise is less than 3 K | `AReflectivePlateAbsorbsLess` | A1 (28 K) | `absorptance` at its minimum and maximum | as stated (calculated 1.5 K) |
| A4 | Given A1 with a Wall between light and plate, then the plate stays at 288 K; with the Wall covering half the face its rise at 20 s is between 30% and 70% of A1's | `ShadingRemovesTheInput` | A1 | half shade | 0.01 K; the stated band |
| A5 | Given A1 with a 2000 J Battery, when the light has been out for 10 s, then the plate still reads above 310 K and is falling | `StoredHeatOutlastsTheLight` | none | none | calculated 312.5 K |
| A6 | Given A1 with the Flashlight 3 m away, when 20 s pass, then the rise is less than a fifth of A1's | `AFartherLightHeatsLess` | A1 | none | calculated ratio 0.11 |
| A7 | Given A1 with a bar from the rail to a target, when 30 s pass, then the target has risen more than 6 K; without the bar it has not moved | `TheRailCarriesHeatOnward` | no bar | none | 6 K against a calculated 15 K; 0.01 K |
| A8 | Given A1 then Reset, then the plate is at 288 K, the Battery full, and a second Run repeats A1 | `ResetRestoresThePlate` | none | none | as A1 |
| A9 | Given a placed plate with `absorptance` 0.5, when saved and loaded, then the value and pose are kept and no run temperature is saved | `SaveAndLoadKeepAbsorptance` | none | none | exact value |
| A10 | `absorptance` outside 0.05 to 0.95 is clamped (`ParameterEditorPolicy.Clamp`) | generic: TH-04 R6 | bounds admitted | 0.05, 0.95 | clamped value exact |

No port, so no wrong-kind connection row. Requirement clauses: absorptance and illuminated area (A1, A3, A4); a thermal port carries it onward (A7); a reflective comparison absorbs less (A3); shading removes the input without deleting stored heat (A4, A5).

## 6. Open questions

Resolved:

1. **Game scale.** D5, restated once in TH-04 section 11.
2. **Flashlight power.** Settled by the optical conventions (90 W radiant, 100 W demand); the old 24 W reading is withdrawn. The heat here is 40% of the light emitted and is paid for by the Battery.
3. **Reflective comparison.** A Real `absorptance` parameter on this part, decided now: one cause changes, and a mirror in its place would also change shape and node.
4. **Thermal port.** A top rail that loose parts rest on, decided now: a contact link needs a narrowphase contact (T3), which two static parts never make.
5. **Unabsorbed light.** Not traced further from a diverging source (optical convention 3: a diverging segment is terminal); it is the reflected share in the ledger.

6. **Requirement visual clause** (owner decision 24, 23 November 2026: the four thermal rows with visual wording are reworded to the declared presentation forms). [thermal-07](../requirements.md#thermal-07) asks for "raised temperature marks"; this spec prints them flush, because a raised mark on a static part is a proud visible solid that would need its own collider (visible = solid) and would catch balls rolled across the face. The clause is reworded to "flush printed temperature marks"; the edit to `requirements.md` is the coordinator's ([Batch N summary](readiness/batch-n.md) section 7 item 7).

Needs the owner: none.

## 7. Integration

- **Works with.** Flashlight and Laser (Epic 13, not delivered), Battery and Wall (delivered), TH-06, TH-08, TH-21, TH-37.
- **Campaign.** "Focus Pocus": introduction 55, practice 56; reuse 69, 131. A level that fixes the plate or the light needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES); ENGINE-OPTICAL as amended, then CAT-029 Flashlight; ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18); ENGINE-RUN-LENGTH; TH-04, TH-37 and TH-08 (for A7); S7; S3 preferred.
- **Files.** `elements/SolarAbsorber/SolarAbsorber.csproj`, `SolarAbsorberElementDeclaration.cs`, `solar_absorber.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); `CuriousContraptions.tests/SolarAbsorberTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if the probe finds every row present and tested; otherwise the engine tier.
- **Disqualifiers.** ENGINE-OPTICAL delivered with a different unit scale or receiver law (re-derive section 5); a receiver that must carry a threshold or a port; no parameter-bound absorptance (the comparison then becomes a two-option Choice of fixed values); the Flashlight delivered with other power or cone values.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Three static boxes, one optical receiver, one node with one face, one parameter, four bindings. No update loop or engine branch on this part.
2. **Generic laws.** The receiver law; "a declared fraction of a reading is credited to a node"; Q̇ = G·ΔT. "Warms in the light" is their composition, for any emitter.
3. **Three layers.** Colliders: base, slab and rail. Visuals: the same three as art, each with its collider; printed inset, sheen and marks. Animation: sheen height from the parameter, mark colour from committed temperature; nothing is written back.
4. **Flow and typing.** No ports; one f32 parameter; light is watts per channel, heat is joules.
5. **Flat state.** The counts in section 2.
6. **Performance.** Static body; three primitive colliders; five sample shadow casts per diverging segment per tick, each through the spatial query's segment cast (optical conventions 5 and 11); thermal work is one node and one term per contact on the rail (T10).
7. **Tests are scenarios.** A1 to A9 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-OPTICAL and ENGINE-THERMAL, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `solar_absorber` |
| Named identity | TH-07, `../invest/named-elements.md#thermal-07` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-441` and the ledger entry has no todo link |
| Decision | S573; `scope-corrections.md` on published main has no `s573` block |
| Mode records | `Configuration=Fixed` |
