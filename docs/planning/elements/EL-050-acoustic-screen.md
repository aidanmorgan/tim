# EL-050 · Acoustic screen — element readiness spec

**Current delivery (24 October 2026):** [EL-050 delivery slice](../invest/named-elements.md#element-050). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against checkout `8d1d68aa`, and repaired on 22 November 2026 against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-h.md`). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the current checkout. Values marked *proposed* are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions). Box sizes are full extents. Shared acoustic facts A1–A13, laws L1–L7, the capability probe (C1–C13, N1–N13) and tolerances are in [EL-044](EL-044-tone-selective-sound-meter.md).

**Owner decision, 21 November 2026:** an acoustic screen blocks and attenuates only; it does not reflect. The Reflecting variant and the `sound_reflector` entry this spec once proposed are removed. Redirection belongs to [EL-051 Acoustic dish](EL-051-acoustic-dish.md) alone.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-050 · Acoustic screen |
| Type | Sound |
| Anchor | [requirements.md#element-050](../requirements.md#element-050); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-050); owner S537 |
| Disposition | **To deliver as a single element.** It refines no CAT part. It is not a duplicate or a material variant of the [CAT-066 Wall](../requirements.md#current-cat-066): the Wall is opaque to sound (transmission 0) and the screen passes a declared fraction, a behaviour delta. It has one mode. |
| Planned package | id `sound_screen`, directory `elements/SoundScreen/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |
| Related | Physical sibling of the Wall (`elements/Wall/`); redirecting sibling [EL-051](EL-051-acoustic-dish.md). |
| Roadmap story | [EL-050 delivery slice](../invest/named-elements.md#element-050) `named-el-050`. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it); then Stories 14.0, 14.2, 14.1 and 14.3. |

## 2. Declaration

Closest delivered shape: the Wall, a static box resized by three typed parameters (`elements/Wall/WallElementDeclaration.cs:8`, parameters `:26`–`:30`, collider bindings `:40`–`:45`).

**Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`), plus the acoustic surface declaration Story 14.0 adds (N8): a transmission factor on a named collider.

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and colliders | One static body. Panel box `thickness` × `height` × `width` (X × Y × Z) centred at the origin, face normal local +X, bound to the three parameters as the Wall binds its box. Foot rail box 0.3 × 0.1 × `width` m at (0, −`height`/2 − 0.05, 0), its length bound to `width`. |
| Mass and material | Static (mass 0). Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. The panel stops balls like any static box. |
| Acoustic surface | Panel collider: transmission factor **0.2**. Foot rail: opaque (default 0). |
| Constraints, sensors, stores | none. |
| Ports | none: a passive surface. |
| Footprint | 1 body, 0 moving bodies, 0 constraints. |

**Parameters**

| Name | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `width` | Real f32 | Metre | 1.5 | 0.5 | 4 | Clamp; BuildOnly; resize handle |
| `height` | Real f32 | Metre | 1.5 | 0.5 | 4 | Clamp; BuildOnly; resize handle |
| `thickness` | Real f32 | Metre | 0.1 | 0.05 | 0.3 | Clamp; BuildOnly; resize handle |

The requirement row names "declared properties", and the outcome needs a real edge, so the size parameters stay ([D13](../invest/delivery-batches.md#3-family-level-decisions) keeps size fixed only where no row needs it; here the gap is the lesson). Resize uses the Wall's three local-axis handles (one gesture, one Undo).

**Presentation bindings.** None driven by simulation. Wavefront art is not clipped by the engine; the meter, not a decorative ring, is the authoritative feedback ([DESIGN.md Sound meter](../../../DESIGN.md#sound-meter)).

**Palette.** Category `ElementCategory.Structure` (as the Wall); palette order 536 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); title "Quiet screen"; colour cream `#fff8e9`. Description: "A finite panel that dampens sound passing through it. Sound going around its edges is unaffected."

**Variants.** None (owner decision above).

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Panel` | box `thickness` × `height` × `width`, cream `#fff8e9` with a quilted relief no deeper than the collider face (normal-mapped, not geometry) | solid, box collider |
| `Stitching` | navy `#293954` dots on both faces | decorative: printed flat |
| `FootRail` | box 0.3 × 0.1 × `width` m, navy | solid, box collider |

No static proud visible solid lacks a collider. Icon: a quilted panel with a wave on one side and a smaller wave on the other. **At a glance** it differs from the Wall by its quilted, stitched surface and its foot rail (pattern and shape).

### Behaviour and numbers

- **Law (L6).** For each straight path from an emission to a receiver, every screen panel the path crosses multiplies the arriving level by 0.2; a path that misses the panel is unaffected, and there is no field around it (requirement outcome: "a geometric gap permits transmission; no universal mute field"). Two screens in line give 0.04. Any opaque collider on the path still gives 0. The screen never emits: there is no reflected occurrence.
- **Why 0.2.** Behind a screen the most a full-strength source can read is 0.2 at zero distance, below the default meter threshold of 0.25: **a default meter never triggers through a screen**. A meter set to its most sensitive 0.05 still triggers through it out to 6.1 m (0.2/(1 + 0.08·d²) = 0.05 at d = 6.12 m). So the screen is a clear "quiet" for the default part and a threshold puzzle for a tuned one, and it is not a perfect mute, which keeps it distinct from the Wall.
- **Edges.** The path test is geometric: with the default 1.5 m square 2 m in front of a source, a receiver 2 m behind it is shadowed within about 1.5 m of the axis and hears full level outside that.
- **Tick order.** Attenuation is part of the Phase 2 acoustic pass (D7).
- **Consistency.** The default panel hides a Speaker cabinet (1.25 × 1.5 m face). It also blocks a Fan's cone as any solid does (delivered airflow occlusion).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S537): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction). Numbered rows are the shared probe in [EL-044 section 3](EL-044-tone-selective-sound-meter.md#shared-capability-probe).

| Capability this identity needs | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body and colliders (C1) | present | EL-044 C1 |
| Box resized by typed parameters with handles (C6) | present | `IParameterizedGeometryElement`; `WorkshopWallTests.ThreeCanonicalDimensionsPoseAndIdentityRoundTripAtDefaultAndBothLimits` (`CuriousContraptions.tests/WorkshopWallTests.cs:15`) and `WorkshopWallTests.RealSimulationRotatedWallBlocksMovingBallWhileAbsentWallDoesNot` (`:88`) |
| Generic lifecycle and refusals (C9) | present | EL-044 C9 |
| Emission, propagation, binary occlusion (N1) | missing | Story 14.0, with reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES` |
| Per-collider acoustic transmission factor (N8) | missing | Story 14.0, gap 7 (its text says a solid collider gives zero) |

Counts: 3 present, 0 untested, 0 narrower, 2 missing. Nothing here needs element-specific engine code: the factor is a value on a collider, and the engine treats every collider's factor the same way.

**Dependencies.** The engine programme (owner decision 3a: `ENGINE-SPATIAL-QUERIES`); Story 14.0; a sound source and a Sound meter.

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance instruction of 22 November 2026 and the documents named in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026).

1. **Declaration data only.** Two colliders and one value: an acoustic transmission factor on the panel collider. The engine treats every collider's factor the same way (default 0, opaque); nothing branches on `sound_screen`, and "a screen dampens sound" is not a rule about screens.
2. **Generic laws.** Occlusion L6: each collider a straight path crosses multiplies the arrival by that collider's declared factor (N8). Contact, friction and restitution for balls; the delivered airflow occlusion for a Fan's stream. The three size parameters bind collider extents as the Wall's do.
3. **Three layers.** *Colliders:* panel box and foot-rail box on one static body, static default material, panel factor 0.2. *Visuals:* the art table above; the quilt relief is a normal map, not geometry. *Animation:* none. Every art node is moved by nothing (static body pose; sizes follow the parameter bindings at build).
4. **One-way flow and typing.** The surface only attenuates paths the engine already traces; nothing returns. Sizes are f32 metres through `ParameterUnit.Metre`.
5. **Flat state.** 1 static body; 2 colliders (2 box); 0 joints; 0 drives; 0 sensors; 0 domain nodes (one acoustic transmission factor on the panel collider); 0 stores; 0 presentation bindings; 3 collider parameter bindings.
6. **Performance fitness.** 2 colliders against the Wall's 1 (`elements/Wall/wall.tscn`): the foot rail is a proud solid and the shape cue against the Wall. Static body; it joins no island. Attenuation adds no query: it reuses the occlusion test each path already makes, whose candidates come from `ENGINE-SPATIAL-QUERIES`. Occurrences follow the D5 bounds in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026) point 6 (tables sized at Run from declarations, no scene ceiling).
7. **Scenarios.** Rows 1–7 run the real simulation; none depends on a code path that names this package.

## 4. Sources and legacy

- Requirement row [element-050](../requirements.md#element-050): "Material surface attenuates or reflects sound according to its declared properties"; outcome "A geometric gap permits transmission; no universal mute field". The owner decision of 21 November 2026 narrows "attenuates or reflects" to "attenuates"; the outcome is unchanged. Proposed row wording is in the batch summary.
- Named entry [element-050](../invest/named-elements.md#element-050), owner S537; component research: "Acoustic screen / Acoustic dish (EL-050, EL-051) | Occluder / redirector with explicit attenuation | attenuation | Blocks or redirects; no diffraction/interference claim" ([component research](../../component-research.md#sound)).

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| C1 | Sound traces test only opaque colliders; an opaque hit before the receiver discards the arrival entirely (binary). | `engine/physics/BodyQueryGeometry.cs@a6c914e:L74-L79`, `engine/Acoustics.cs@a6c914e:L70-L73` | carry forward for ordinary bodies; the screen adds a graded factor |
| C2 | A wall between speaker and meter gives level 0; geometry only blocks the straight path. | `CuriousContraptions.tests/SoundMeterTests.cs@a6c914e:L14-L54` | carry forward (gap test: a path beside the panel is unaffected) |

- **Files harvested:** acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-050](../requirements.md#element-050), [acoustic profile](../invest/profiles.md#acoustic). Planned test class `SoundScreenTests`. Tolerances and stall guard: [EL-044 shared tolerances](EL-044-tone-selective-sound-meter.md#shared-tolerances-and-stall-guards).

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | Given a supplied Speaker and a supplied Sound meter 4 m apart with a default screen midway on the line, when the speaker pulses, then the meter reads 0.2 × 0.439 = 0.088 within T-level and does not trigger at the default threshold. | `SoundScreenTests.AScreenOnThePathPassesOneFifth` | screen removed: 0.439, triggers | meter threshold 0.05: triggers through the screen |
| 2 | Given the screen narrowed or slid aside so the straight path passes its edge, then the meter reads its unscreened 0.439; a second meter beside the screen with a clear path reads its unscreened value while the first is shadowed (requirement outcome: a gap transmits; no mute field). | `SoundScreenTests.APathPastTheEdgeIsUnaffected` | row 1 | path just inside and just outside the edge, 0.1 m either side |
| 3 | Given two screens in line, then the meter reads 0.04 × the open level; given a Wall and a screen in line, then 0. | `SoundScreenTests.ScreensMultiplyAndAWallStillBlocks` | row 1 | — |
| 4 | Given any geometry, then no receiver on the source's side of the screen reads more than it does with the screen removed (it does not reflect; owner decision). | `SoundScreenTests.NothingIsReflected` | — | meter at the mirror position of the source |
| 5 | Given a ball rolled at the panel, then it stops against the drawn face and rests; a Fan's stream is blocked behind it. | `SoundScreenTests.ThePanelIsSolidToBallsAndAir` | panel removed: the ball passes | panel at `thickness` 0.05 m |
| 6 | Reset restores the authored size and pose; save then load round-trips the three sizes and pose. | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` | — | sizes at both limits |
| 7 | Each size below its minimum, above its maximum or non-finite is refused atomically with a typed reason; the part has no ports, so any link to it is refused. | `SoundScreenTests.OutOfRangeSizesAreRefused` (after `WorkshopWallTests.InvalidSubmittedParametersRejectAtomicallyWhileExactF32ValuesRoundTrip`) | limits accepted | first f32 beyond each limit |

**Chrome recipe (pending, post-catalogue).** Battery → Speaker → supplied Sound meter 4 m away; place a Quiet screen between them with the move and rotate tools; resize with its handles.

## 6. Open questions

**Resolved.**

1. **Transmission value.** 0.2: [D1](../invest/delivery-batches.md#3-family-level-decisions), with the gameplay reason in section 2.
2. **Reflection scope.** Not required and not built: owner decision, 21 November 2026 (an acoustic screen blocks and attenuates only).
3. **Ordinary walls.** The Wall and every other collider stay fully opaque (factor 0) once graded surfaces exist: decided under the standing delegation, because "visible = solid = sound-blocking" must stay predictable and only this part declares a factor.
4. **Coordinator decisions, 22 November 2026 (owner's standing delegation), applied in the audit repair.** (a) Coverage is bound by the published section form and is blocked on S7 (section 7). (b) Palette order 536 (coordinator decision 36). (c) `docs/planning/invest/delivery-batches.md:190` still lists reflection as open; the owner decision of 21 November 2026 closed it, and the correction is proposed in the batch summary (this spec does not edit that file).

**Needs the owner.** None.

## 7. Integration

- **Works with:** Wall, Battery, Fan, balls (delivered); Speaker, Bell, Sound meter, EL-044 (planned before it); EL-046 route (a screen between source and horn weakens what is captured).
- **Campaign:** chapter 8, levels 71–80 ("acoustic screen"), first taught at **78a**, before the horn route (79a) (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)); reuse 81–100, 117–125, 136–150. [sequence-task-407](../requirements.md#sequence-task-407) ("Screens/dishes … each retain their later individual contract"); process [IX-12](../requirements.md#interaction-12) ("an obstructed control receives the predicted diminished field").
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7:** the published form's preconditions are not met. This identity lacks: (1) an acceptance pair of its own, since its ledger entry links only the umbrella `todo-352`, which has no co-anchored `sequence-task-NNN` in `requirements.md` and is shared by nine identities (the reader refuses two identities on one task source); (2) a decision anchor, since its ledger owner S537 has no `<a id="s537">` anchor in `docs/planning/invest/scope-corrections.md`. The delivering slice adds the section once S7 supplies both; it is not written earlier, because the reader throws on a malformed block. Mode records: `Configuration=Fixed`.
- **Lesson fixtures (S8):** needed only where a level pre-places it.

## 8. Delivery

- **Slice shape.** Single element, data-only.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it); then Story 14.0 amended with gap 7; Stories 14.2, 14.1 and 14.3 (a meter and a source); S7.
- **Files.** Adds `elements/SoundScreen/` (`SoundScreen.csproj`, `SoundScreenElementDeclaration.cs`, `sound_screen.tscn`, `icon.svg`) and `CuriousContraptions.tests/SoundScreenTests.cs`; registration is derived. The test file needs no `Compile` line (main compiles every top-level `*.cs` of the test project by one rule). Until S3 lands: `CuriousContraptions.slnx`, `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596`, `:653`. This spec gains its binding section when S7 allows (section 7).
- **Review tier.** Data-only.
- **Disqualified if** the probe finds occlusion is binary only (no per-collider factor), or that the factor would be read by package id rather than from the collider's declaration.
