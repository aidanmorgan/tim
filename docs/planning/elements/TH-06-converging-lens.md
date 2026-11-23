# TH-06 · Converging lens — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-06 delivery slice](../invest/named-elements.md#thermal-06). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Light follows the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions); heat in what the light lands on follows the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions). Neither is restated.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-06 · Converging lens · Thermal (an optical part whose lesson is heating) |
| Anchors | [requirements.md#thermal-06](../requirements.md#thermal-06) (task `sequence-task-440`); [named-elements.md#thermal-06](../invest/named-elements.md#thermal-06); integration task [sequence-task-402](../requirements.md#sequence-task-402) |
| Owner key / research | S518 · [TH-S04 optical heating](../../thermal-component-research.md#th-s04) |
| Related identities | Sibling of [EL-173 Diverging lens](EL-173-diverging-lens.md) (Batch K), whose frame it shares; frame family of [EL-143](EL-143-red-optical-filter.md) and [CAT-055](CAT-055-red_filter.md). Historical records [todo-304](../requirements.md#todo-304) (shared with TH-37) and [todo-196](../requirements.md#todo-196). Sources: [EL-210 Flashlight](EL-210-flashlight.md). Targets: [TH-32](TH-32-tinder-pad.md), [TH-07](TH-07-solar-absorber-plate.md), [TH-37](TH-37-heat-sensitive-target.md). |
| Campaign | Intro 59; practice 60; reuse 85, 142 ([thermal allocation](../requirements.md#thermal-campaign-allocation)); the optical lens lessons 51 to 60 are retained |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES (the optical segment cast), and on ENGINE-OPTICAL with the Relay-inlet addition below. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (C4: the converging segment is withdrawn; the lens is a Relay whose inlet is a receiver aperture) and coordinator decisions 16 and 21. |

### Disposition

**To deliver as a single element; a variant in the lens family, not a duplicate and not a criterion on another package.** "Refines the existing lens identity" in the requirement means the lens family of EL-173: no converging lens package exists or is planned elsewhere (EL-173 states "The converging lens is a separate thermal identity (TH-06)"). It differs from EL-173 in one declared surface response (a Relay that gathers light into a ray where EL-173 declares a spread), which is a behaviour delta, so it cannot be closed against EL-173. If ENGINE-OPTICAL delivers both responses, the two may share one Family slice "Lenses"; each keeps its own rows. Planned package id `converging_lens`, directory `elements/ConvergingLens/`, display name "Converging lens".

The part itself declares no thermal node: the requirement says "heating remains absorption in the receiving material". Its thermal rows are run with the parts that absorb (TH-32 P6, TH-37, TH-07).

## 2. Declaration

Follows the Diverging lens ([EL-173 section 2](EL-173-diverging-lens.md#2-declaration)), which follows the delivered Wall for a static body that is nothing but colliders (`elements/Wall/WallElementDeclaration.cs:8`).

- **Interfaces.** `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) plus the optical surface contract of ENGINE-OPTICAL. `KinematicMode.Static`; footprint 1 body.
- **Body.** One static body; material restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (EL-173's values). Light travels along local X.
- **Colliders (six boxes, full sizes, metres; EL-173's).** Two frame bars 0.18 × 0.18 × 1.65 at (0, ±0.74, 0); two frame bars 0.18 × 1.3 × 0.18 at (0, 0, ±0.74); base 0.9 × 0.2 × 1.65 at (0, −0.93, 0); lens slab `col_lens` 0.12 × 1.3 × 1.3 at the origin.
- **Optical response** (optical convention 7, Relay, with the inlet addition of section 3, row L2). Two Relays, one per face, so the lens works from either side. Each inlet is a receiver aperture on one large face of the slab (optical convention 8): a rectangle 1.3 × 1.3 m, area 1.69 m², one centre sample at weight 1, all three channels. It receives the conserved share of every diverging segment that reaches it, and every collimated segment ending on it. Each inlet feeds one outlet: a collimated segment leaving the centre of the opposite face along the lens axis, τ = (0.95, 0.95, 0.95), channel mask all, range 3.0 m (the declared focal reach). The slab's own response for light not landing on an inlet is Absorb, so a diverging cone is never transmitted past the slab: everything that reaches the glass is gathered into the ray. Bars and base: Absorb (the default). The 5% not relayed is absorbed light in the optical ledger; with no node it heats nothing.
- **Ports, parameters, stores, thermal nodes.** None. The reach is fixed (D13); the player focuses by moving the lens so that its axis meets the target within 3.0 m.
- **Presentation bindings.** None. The ray and its bright end point are drawn by the shared beam renderer from the committed segment; the build-mode aim preview is the same solve (optical convention 12).
- **Palette.** Category `ElementCategory.Optics`, which ENGINE-OPTICAL adds (the enum has five members today, `CuriousContraptions.Abstractions/ElementCategory.cs:7-12`). Order: 628 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| Four frame bars cream `#fff8e9` | boxes as the colliders | solid | nothing |
| Base navy `#293954` | box as the collider, touching the bars | solid | nothing |
| `Visual/Lens` cyan `#66b8c9`, translucent | slab 0.12 × 1.3 × 1.3 m with two convex arcs printed flush on each face | solid, `col_lens`: balls bounce off it; light passes by its declared response | nothing |

The requirement's "faint selected-only finite focus footprint" is the beam renderer's drawing of the relayed ray and its end point in the aim preview; no presentation binding can be gated on selection today, so nothing else is declared. Icon: a frame with a lens whose arcs bow outward and two rays meeting. At a glance it differs from the Diverging lens by arcs that bow outward where EL-173's bow inward, and from the colour filters by the printed arcs.

### Compiled counts

Bodies 1 (static); colliders 6 (boxes), the same as EL-173; joints 0; sensors 0; optical relays 2, each with one receiver-aperture inlet (one sample) and one outlet; thermal nodes 0; ports 0; presentation bindings 0. Per tick the lens adds at most two outlet segments, inside the 128-segment limit (optical convention 10).

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Owner or test |
| --- | --- | --- | --- | --- |
| TH-04 R1 | Static box colliders from art, visible = solid | present | `ISceneGeometryElement` | test named in [TH-04](TH-04-electrical-heating-plate.md#3-engine-capabilities) |
| L1 | A declared Relay response with collimated outlets on a collider face | **missing** | none: the engine has no beam, ray or optical contract ([Story ENGINE-OPTICAL](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception)) | ENGINE-OPTICAL (convention 7) |
| L2 | A Relay inlet that is a receiver aperture: the conserved share it receives from diverging segments feeds its outlets | **missing**, and an addition to the optical conventions, which apply Relay to collimated segments only (convention 3) | none | ENGINE-OPTICAL (exact text in the [Batch N summary](readiness/batch-n.md); no new segment kind) |
| L3 | Receivers taking conserved shares of a diverging segment, and the whole power of a collimated segment ending on them | **missing** | none | ENGINE-OPTICAL (convention 8) |
| L4 | An Optics palette category | **missing** | `ElementCategory` | ENGINE-OPTICAL (EL-173's row M6) |

Counts: present 1, untested 0, narrower 0, missing 4, all ENGINE-OPTICAL's. Absorbed light becoming heat in a target is that target's row (ENGINE-THERMAL), not this part's. Nothing here needs code that names a lens.

## 4. Sources and legacy

- **Requirement row** [thermal-06](../requirements.md#thermal-06): refine the existing lens identity: finite-width refraction concentrates irradiance on real surfaces without multiplying incident power; heating remains absorption in the receiving material. Visual style: "Translucent cyan disc in a cream sculptural frame; faint selected-only finite focus footprint".
- **Named entry** [thermal-06](../invest/named-elements.md#thermal-06): owner S518; "Defocus, occlusion or inadequate source power fails to reach ignition conditions".
- **Integration task** [sequence-task-402](../requirements.md#sequence-task-402): moving the lens places a finite bright region on the target; a correct focus can actuate the heat-sensitive latch while an unfocused or occluded beam cannot gain free energy.
- **Research** [TH-S04](../../thermal-component-research.md#th-s04): "optical refraction + irradiance concentration"; parameter focal length.
- **Element map** row TH-06: ChemicalReaction, FiniteLedger, GeometryQuery, Ignition, OpticalAbsorption, OpticalTransport, SensibleHeat, TopologyTransaction; the map notes that ignition and reaction belong to the target (D14).
- **Legacy (no lens; frame and mechanisms only).**

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Filter frame geometry, transparent pane collider and aperture radius 0.65. | `parts/ColourFilterPart.cs@a6c914e:L11-L12`, `parts/ColourFilterPart.cs@a6c914e:L20-L29` | Carry forward the frame; the aperture is now the whole slab, as EL-173. |
  | 2 | Closed enums `OpticalPortId` and `OpticalInteraction` (no refraction). | `engine/OpticalNetwork.cs@a6c914e:L8-L9` | Carry forward the closed-set pattern as a declared response; do not carry forward the enum. |
  | 3 | Mirror retention 0.95; passive branches divide rather than duplicate power. | `engine/OpticalNetwork.cs@a6c914e:L26-L27`, `engine/OpticalNetwork.cs@a6c914e:L33-L33` | Carry forward 0.95 as τ; do not carry forward the CPU ray queue. |
  | 4 | Legacy cone light had no refraction; flashlight range 8, cone 15°, intensity 24. | `engine/LightNetwork.cs@a6c914e:L12-L13`, `parts/FlashlightPart.cs@a6c914e:L10-L13` | Carry forward range and angle; do not carry forward "intensity 24" as watts: the Flashlight radiates (30, 30, 30) W (optical convention 2). |

  Files harvested: `parts/ColourFilterPart.cs`, `engine/OpticalNetwork.cs`, `engine/LightNetwork.cs`, `parts/FlashlightPart.cs`, `reference/cpu/MachinePart.cs`. No lens term was found in `engine/physics/`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` or `reference/`.

## 5. Acceptance

Point of truth: [thermal-06](../requirements.md#thermal-06), [sequence-task-402](../requirements.md#sequence-task-402) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Law, declared as its result.** Light that reaches a face of the slab ends there. The face's inlet aperture takes its conserved share of each diverging segment (A × Σ wᵢEᵢ, scaled down with every other receiver of that segment so that received ≤ emitted, optical convention 8) and the whole of any collimated segment ending on it. The relay sends that power × τ out as one collimated ray from the centre of the other face, along the lens axis, for 3.0 m. A collimated ray carries its whole power to what it ends on (optical convention 3). No index of refraction is solved and no segment kind is added.
- **Reference case.** Flashlight (30, 30, 30) W over a 15° half-angle: Ω = 2π(1 − cos 15°) = 0.2141 sr, 420.4 W/sr, range 8 m (optical convention 2). Lens 1.5 m in front of it, on its axis: the inlet's centre sample sees 420.4 ÷ 1.5² = 186.8 W/m², an uncapped share of 1.69 × 186.8 = 316 W, capped to the segment's 90 W (the lens is the only receiver it reaches). The ray carries 0.95 × 90 = 85.5 W; 4.5 W is absorbed in the lens.
- **Never multiplied.** The largest reading any receiver can take from the ray is 85.5 W, less than the 90 W that left the source. Concentration puts all of it on one point instead of spreading it (the bare cone gives 420.4 ÷ 4.5² = 20.8 W/m² at 4.5 m from the source).
- **On a default detector** (0.950 m² disc) on the axis, from just behind the lens to 3.0 m: the ray ends on it, reading 85.5 W. Without the lens, at 4.5 m from the source: 0.950 × 20.8 = 19.7 W. Beyond the 3.0 m reach, or off the axis, the detector gets nothing from the lens; the slab absorbs or relays the whole cone, so no direct light passes it either.
- **A farther source.** The Flashlight 3.0 m from the lens: centre sample 420.4 ÷ 9 = 46.7 W/m², share 1.69 × 46.7 = 79.0 W (under the 90 W cap; the cone, 0.80 m in radius there, is wider than the slab), ray 0.95 × 79.0 = 75.0 W.
- **On tinder** (TX-03, row [TH-32 P6](TH-32-tinder-pad.md#5-acceptance)): a pad whose wafer the ray ends on books 85.5 W on the wafer's absorbed record, of which its absorptance moves 0.9 × 85.5 = 77 W to the fibre (thermal T3), and ignites; a pad clear of the ray or beyond 3.0 m receives nothing from the lens.
- **On an absorber** ([TH-07](TH-07-solar-absorber-plate.md)): the ray ends on its receiver aperture; the plate absorbs its own absorptance × 85.5 W.
- **Inadequate source.** A Candle's 3 W radiated over all directions ([TH-01](TH-01-candle.md)), 0.239 W/sr, 1.5 m from the lens: share 1.69 × 0.239 ÷ 2.25 = 0.18 W, ray 0.17 W, 0.15 W absorbed by a tinder pad, far below what it loses (TH-32).
- **Occlusion.** Any collider between source and lens or lens and target ends the segment (optical convention 6).
- **Why τ = 0.95 and a 3.0 m reach.** τ is the legacy mirror retention (section 4, fact 3). The reach is three times the old focal length: far enough that a lesson can place tinder on a bench beside the lens, short enough that "move the lens closer" is a lesson the player can see.
- **Tick order.** One optical solve per tick, after the last substep and before commit (optical convention 4); the inlet's share and the outlet ray are produced in the same solve; readings commit with the tick and act from the next tick. Light absorbed by a thermal face in tick N is credited in the thermal step of tick N (T8).
- **Tolerances.** The one optical tolerance set (coordinator decision 21): power ± 2%, stall guard 1 s, direction 0.5°, end points ± 0.02 m.
- **Run length.** Every row fits in the default 30 s (ENGINE-RUN-LENGTH is not needed).

### Rows

Planned class `ConvergingLensTests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| L-1 | Given a Flashlight, the lens 1.5 m in front of it and a broadband detector on the axis 2.0 m behind the lens, then the detector's committed reading is 85.5 W and the ray ends on the detector's face | `ALensGathersTheConeIntoARay` | the same detector with no lens, 4.5 m from the source, reads 19.7 W | none | ± 2%; end point ± 0.02 m |
| L-2 | Given L-1 with the detector at 0.5, 2.0 and 2.9 m behind the lens, then every reading is 85.5 W, none exceeds it, and relayed + absorbed in the lens = 90 W | `ALensNeverMultipliesPower` | none | 2.9 m, inside the 3.0 m reach | ± 2% |
| L-3 | Given L-1 with the Flashlight 3.0 m from the lens (the cone is 0.80 m in radius there, wider than the slab), then the reading is 75.0 W | `AFartherSourceGivesTheLensLess` | L-1 | none | ± 2% |
| L-4 | Given L-1 and a Wall between lens and detector, then the reading is 0 | `AnOccludedBeamDeliversNothing` | L-1 | none | exact zero |
| L-5 | Given L-1 with the detector 3.1 m behind the lens on the axis, or 2.0 m behind it and 0.5 m off the axis, then the reading is 0 | `OffTheAxisOrBeyondTheReachNothingArrives` | L-1 | 2.9 m (L-2) against 3.1 m | exact zero |
| L-6 | Given a Basketball rolled at the slab, then it is stopped by it | `TheLensIsSolidToBodies` | none | none | the ball's centre never passes x = 0 |
| L-7 | Given a placed lens, when saved and loaded and after Reset, then its pose is kept and L-1 repeats | `SaveLoadAndResetKeepThePose` | none | none | pose 0.001 m |

No ports and no parameters, so no wrong-kind or parameter rows. The requirement's outcome, "defocus, occlusion or inadequate source power fails to reach ignition conditions", is TH-32 P6 (defocus), L-4 with a pad in place of the detector (occlusion) and the Candle case above (inadequate source), all run when TH-32 is delivered. Sequence-task-402 is run with TH-37 in its slice.

## 6. Open questions

Resolved:

1. **Game scale and optical unit.** D5 with TH-04's correction: watts per channel as the optical conventions state. The earlier "24 W flashlight, 22.8 W transmitted, 1700 W/m²" figures are withdrawn.
2. **Refraction decision.** Declared as its result (optical convention 7): Relay outlets fed by a receiver-aperture inlet (audit C4, coordinator decision 16). The earlier "terminal converging segment" with a focal length and a waist is withdrawn: it was a second segment kind beside conventions 3 and 7. The cost is that the focus is a ray, not a spot with a size: what the ray ends on receives all of it, and a target off the axis or beyond the reach receives none.
3. **Map row.** Navigation only (D14): ignition and reaction are the target's.
4. **Focal presets.** One fixed reach (D13); the research table's "parameter focal length" is not declared.
5. **Lens heating.** The lens declares no thermal node (decided now: the requirement places heating in the receiving material, and a node would add ENGINE-THERMAL as a prerequisite for a 4.5 W effect no lesson uses).
6. **Aperture.** The whole slab, as EL-173, not the legacy 0.65 m disc: what is drawn as glass is what transmits.

Needs the owner: none.

## 7. Integration

- **Works with.** Flashlight, Wall, detectors (Epic 13 and Batch K, undelivered); EL-173 (sibling); TH-32, TH-07, TH-37 (thermal targets); TH-01 (weak source).
- **Campaign.** "Focus Pocus": introduction 59, practice 60; reuse 85, 142. Lesson fixtures need story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element, or the Family slice "Lenses" with EL-173 if the probe finds both responses present.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES for ray candidates); ENGINE-OPTICAL as amended (row L2); the Flashlight and one detector; S7. This slice owns the cross-part rows of the thermal targets taught before the lens (59): TH-32 P6 (pad, 58) and TH-37's light rows (heat target, 15); ENGINE-THERMAL and ENGINE-IGNITION-BLAST are needed only for those rows.
- **Files.** `elements/ConvergingLens/ConvergingLens.csproj`, `ConvergingLensElementDeclaration.cs`, `converging_lens.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/ConvergingLensTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if ENGINE-OPTICAL leaves L1 to L4 present and tested; otherwise the engine tier.
- **Disqualifiers.** ENGINE-OPTICAL delivered with Relay inlets for collimated segments only (the part cannot be declared; the story is amended first with L2); a relay whose outlet can exceed its inlet's share; a different unit scale (re-derive section 5 and TH-32 P6).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Six static boxes and two Relay responses, each a receiver-aperture inlet and one collimated outlet with τ and a range.
2. **Generic laws.** The Relay, the conserved receiver share and the collimated-segment laws of the optical conventions; contact for bodies. "Lights tinder" is the target's absorbed watts exceeding its loss.
3. **Three layers.** Colliders: six boxes. Visuals: the same six solids with printed arcs. Animation: none; the beam is the shared renderer's.
4. **Flow and typing.** No ports; channels are a closed set; f32 values.
5. **Flat state.** The counts in section 2.
6. **Performance.** Static body; the collider count of the sibling lens. At most one outlet segment per face per tick, inside the optical limits (optical conventions 10 and 11).
7. **Tests are scenarios.** L-1 to L-7 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-OPTICAL, a general capability stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `converging_lens` |
| Named identity | TH-06, `../invest/named-elements.md#thermal-06` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-440` and the ledger entry has no todo link (todo-304 is a shared historical record) |
| Decision | S518; `scope-corrections.md` on published main has no `s518` block |
| Mode records | `Configuration=Fixed` |
