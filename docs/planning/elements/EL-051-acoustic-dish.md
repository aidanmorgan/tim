# EL-051 · Acoustic dish — element readiness spec

**Current delivery (24 October 2026):** [EL-051 delivery slice](../invest/named-elements.md#element-051). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against checkout `8d1d68aa`, and repaired on 22 November 2026 against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-h.md`). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the current checkout. Values marked *proposed* are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions). Box sizes are full extents. Shared acoustic facts A1–A13, laws L1–L7, the capability probe (C1–C13, N1–N13) and tolerances are in [EL-044](EL-044-tone-selective-sound-meter.md).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-051 · Acoustic dish |
| Type | Sound |
| Anchor | [requirements.md#element-051](../requirements.md#element-051); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-051); owner S538 |
| Disposition | **To deliver as a single element.** It refines no CAT part and duplicates nothing. Since the owner decided on 21 November 2026 that the [EL-050 Acoustic screen](EL-050-acoustic-screen.md) does not reflect, the dish is the only part that redirects sound. |
| Planned package | id `sound_dish`, directory `elements/SoundDish/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |
| Related | Listener is a [CAT-060](CAT-060-sound_meter.md)/[EL-044](EL-044-tone-selective-sound-meter.md) meter or an [EL-046](EL-046-listening-horn.md) horn; the optical analogue is the [CAT-041 Mirror](CAT-041-mirror.md). |
| Roadmap story | [EL-051 delivery slice](../invest/named-elements.md#element-051) `named-el-051`. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it); then Stories 14.0, 14.2, 14.1 and 14.3. |

## 2. Declaration

Closest delivered shape: a static scene-geometry part with no ports, as the Wall without its parameters (`elements/Wall/WallElementDeclaration.cs:8`).

**Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the redirecting-surface declaration Story 14.0 adds (N9).

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and colliders | One static body. Dish: cylinder r 0.75 m, length 0.2 m, axis X, centred at the origin, the concave face toward local +X *proposed* (1.5 m across matches a Speaker's height). Post: cylinder r 0.07 m (the admitted cylinder minimum is 1/16 = 0.0625 m, `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:241`), height 0.75 m, axis Y, at (−0.05, −0.45, 0). Foot box 0.6 × 0.14 × 0.6 m at (−0.05, −0.89, 0). Centre boss: sphere r 0.06 m at (0.1, 0, 0). |
| Mass and material | Static (mass 0). Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. Opaque to direct sound (transmission 0): what is behind the dish is shadowed. |
| Redirecting surface | Aperture disc r 0.75 m at local x = +0.1, axis +X; acceptance half-angle **70°**; output cone half-angle 10° from the dish centre (0.1, 0, 0) along +X; efficiency 0.8. All fixed declared values. |
| Constraints, sensors, stores | none. |
| Ports | none: passive geometry. |
| Parameters | none ([D13](../invest/delivery-batches.md#3-family-level-decisions): no requirement row names a setting; the player aims it with the rotate tool). |
| Footprint | 1 body, 0 moving bodies, 0 constraints. |

**Presentation bindings.** A gold glint `Visual/Glint` at the boss and forward rings from the dish ← the redirect event cue (`PresentationDriver.Event`, `CuriousContraptions.Abstractions/ElementPresentation.cs:21`, on the acoustic event kind, N12). A faint aiming line along the axis while selected is editor chrome, not simulation.

**Palette.** Category `ElementCategory.Structure`; palette order 537 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); title "Sound dish"; colour cream `#fff8e9`. Description: "Turns sound arriving at its face into a narrow beam along its axis. Aim it carefully: off-axis sound or a missed listener gets nothing, and it never makes sound louder."

**Variants.** None.

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Dish` | straight cylinder r 0.75 × 0.2 m, axis X, cream `#fff8e9`; the concavity is shading on the flat +X face (painted and normal-mapped, no geometry) | solid, cylinder collider of the same size |
| `Rim` | gold `#e8b764` edge band, flush with the bowl | decorative: printed flat |
| `Boss` | navy `#293954` sphere r 0.06 m at the centre | solid, sphere collider |
| `Post` | cylinder r 0.07 × 0.75 m, navy | solid, cylinder collider |
| `Foot` | box 0.6 × 0.14 × 0.6 m, navy | solid, box collider |
| `Glint`, rings | gold flash, translucent rings | decorative: glow |

Icon: a dish in profile with an arc arriving and a straight beam leaving. **At a glance** it differs from the Quiet screen (a flat quilted rectangle) by being a round bowl on a post.

### Behaviour and numbers

**Redirect rule** *proposed*. An admitted occurrence (L1–L3) whose straight, unoccluded path reaches the aperture disc from its +X side within 70° of the dish axis produces one child occurrence from the dish centre along +X, same tone, same kind (pulse or sustained), cone half-angle 10°. The child carries the source strength s × 0.8 and the source's path length: at a listener d₂ metres beyond the dish, with d₁ metres from source to dish,

  level = 0.8 · s / (1 + 0.08 · (d₁ + d₂)²), delay (d₁ + d₂) / 12 s, and 0 once d₁ + d₂ > 8 m.

Attenuation is applied once over the total path, never per leg. Arrivals that miss the disc, come from outside 70° or hit the back produce nothing. A child occurrence is never redirected again by the dish that made it; a second dish may redirect it, and total path still accumulates, so chains die at 8 m. The dish's own colliders (dish, post, foot and the boss sphere, which encloses the child's origin at (0.1, 0, 0)) are exempt from occlusion on both the incident path and the child path (L6: an instance's own colliders do not occlude it).

- **No gain, for every geometry.** The dish reading is exactly 0.8 × the direct reading at the same total path. Worked case (s = 1; dish at the origin facing +X; speaker 2 m from the dish centre and 60° off its axis, at about (1.1, 0, 1.73), aimed at the dish; meter 3 m along the axis): d₁ = 2 m, d₂ = 3 m; direct at 5 m = 1/(1 + 0.08·25) = 0.333; via the dish = 0.267, which still triggers a default meter (0.25). The speaker cabinet (1.25 m wide) is at least 0.8 m from the axis, where the 10° beam is under 0.2 m in radius, so it does not shadow the beam; the meter is 79° off the speaker's own axis (speaker → meter (2.0, 0, −1.73) against the axis (−0.5, 0, −0.866): cos = 0.189), outside its 35° cone, so it hears nothing directly. A per-leg rule would give 0.352 > 0.333 and is rejected for that reason.
- **Efficiency 0.8**: a smooth dish loses a little; below 1 by construction.
- **Acceptance 70°, output 10°.** The wide acceptance is what lets the dish turn a corner: a source up to 70° off the axis is redirected along the axis, a turn of up to 110°. (With the 20° first proposed the child beam returned within 30° of its source and, in the worked case, the speaker cabinet covered the axis.) Sound arriving nearly edge-on, beyond 70°, is not redirected. The output stays narrow: at d₂ = 3 m the 10° beam is 0.53 m in radius, about a Sound meter's half-width (0.8 m), so the meter must be near the axis and a 25° aiming error misses: "real coverage" in the row.
- **Reach at defaults.** With the source 2 m away, a default meter triggers out to d₂ = 3.2 m (0.8/(1 + 0.08·(2 + d₂)²) = 0.25), so a dish turns a corner over about a bench length.
- **Tick order.** Redirection is part of the Phase 2 acoustic pass (D7).

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S538): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction). Numbered rows are the shared probe in [EL-044 section 3](EL-044-tone-selective-sound-meter.md#shared-capability-probe).

| Capability this identity needs | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body with cylinder, sphere and box colliders (C1, C10) | present | EL-044 C1, C10 |
| Generic lifecycle and refusals (C9) | present | EL-044 C9 |
| Event cue presentation (C12) | narrower | no acoustic event kind; owned by N12 |
| Emission, propagation, occlusion, sustained occurrences (N1, N3) | missing | Story 14.0, with reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES` |
| Redirecting surface with child occurrences that carry total path length (N9) | missing | Story 14.0, gap 8 |
| Acoustic event kind and cue (N12) | missing | Story 14.0, gap 11 |

Counts: 2 present, 0 untested, 1 narrower, 3 missing. Nothing here needs element-specific engine code: the surface is a declared disc, two angles and an efficiency.

**Dependencies.** The engine programme (owner decision 3a: `ENGINE-SPATIAL-QUERIES`); Story 14.0; a sound source; a meter or horn listener; the rotate tool for aim (delivered).

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance instruction of 22 November 2026 and the documents named in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026).

1. **Declaration data only.** Four colliders and one redirecting surface (disc, axis, acceptance half-angle, output half-angle, efficiency). The engine applies the redirect rule to every declared redirecting surface; nothing branches on `sound_dish`.
2. **Generic laws.** Arrival L2, L3, L6 at the disc; the redirecting-surface rule (one child occurrence per incident occurrence, total-path attenuation, N9); then L1–L4 and L6 for the child. Contact, friction and restitution for balls. No parameters.
3. **Three layers.** *Colliders:* dish cylinder, post cylinder, foot box and boss sphere on one static body, static default material, all opaque to sound. *Visuals:* the art table above; the bowl is shading on a flat face and no art mesh is a collider. *Animation:* read-only bindings on the redirect event cue. Moved by a binding: `Glint`, rings. Moved by nothing: `Dish`, `Rim`, `Boss`, `Post`, `Foot`.
4. **One-way flow and typing.** Incident occurrence → child occurrence → receivers → presentation; nothing returns. Values are f32; there are no ports.
5. **Flat state.** 1 static body; 4 colliders (2 cylinder, 1 box, 1 sphere); 0 joints; 0 drives; 0 sensors; domain nodes: 1 redirecting surface; 0 stores; 2 presentation bindings.
6. **Performance fitness.** 4 colliders against the Wall's 1 (`elements/Wall/wall.tscn`): the post, foot and boss are separate proud solids of a free-standing part. Static body; it joins no island. One child per incident occurrence, and the total-path range ends every chain at 8 m. Incident and child occurrences follow the D5 bounds in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026) point 6 (tables sized at Run from declarations, no scene ceiling; arrival and occlusion candidates from `ENGINE-SPATIAL-QUERIES`).
7. **Scenarios.** Rows 1–7 run the real simulation; none depends on a code path that names this package.

## 4. Sources and legacy

- Requirement row [element-051](../requirements.md#element-051): "Curved surface redirects acoustic energy through real coverage"; outcome "Misalignment misses the listener and cannot amplify total energy".
- Named entry [element-051](../invest/named-elements.md#element-051), owner S538; component research row EL-050/051 "Occluder / redirector with explicit attenuation … no diffraction/interference claim" ([component research](../../component-research.md#sound)).
- Legacy: no dish. Reused facts: cone sampling and attenuation (A5, `engine/Acoustics.cs@a6c914e:L36-L46`), opaque blocking (A6, A7). The legacy collider kinds were sphere, box and plane (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`); the current engine also has cylinders (EL-044 C10), which this declaration uses. The legacy optical mirror is harvested by [CAT-041](CAT-041-mirror.md) and is not acoustic.
- **Files harvested:** acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-051](../requirements.md#element-051), [acoustic profile](../invest/profiles.md#acoustic). Planned test class `SoundDishTests`. Tolerances and stall guard: [EL-044 shared tolerances](EL-044-tone-selective-sound-meter.md#shared-tolerances-and-stall-guards).

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | Given a supplied Speaker 2 m from the dish centre and 60° off the dish axis, aimed at the dish, a Wall across the speaker's direct line to a supplied Sound meter 3 m along the dish axis (the Wall at least 0.5 m clear of the axis and clear of the speaker-to-dish line), when the speaker pulses, then the meter reads 0.267 within T-level and triggers once per pulse. | `SoundDishTests.AnAlignedDishTurnsSoundRoundAWall` | dish removed: 0 | total path just under and just over 8 m with a 0.05 threshold meter (at 8 m via the dish: 0.8/(1 + 0.08 × 64) = 0.131, above 0.05) |
| 2 | Given the dish turned 25° toward the speaker (the arrival is now 60° − 25° = 35° off its axis, still inside 70°, so it is still redirected), then the meter, now 25° off the dish axis and outside the 10° beam, reads 0 (requirement outcome: misalignment misses the listener). Turning away from the speaker is not used: the arrival would be 85°, beyond acceptance, and the meter would read 0 for the wrong reason. | `SoundDishTests.AMisalignedDishMissesTheListener` | row 1 | meter 8° and 12° off the dish axis (heard, not heard) |
| 3 | Given the speaker aimed at the dish back, or arriving 75° off the dish axis, then no child occurrence is produced. | `SoundDishTests.OffAxisOrRearArrivalsAreNotRedirected` | row 1 | arrival 68° (redirected) and 72° (not) off axis |
| 4 | Given the Wall removed and a second meter at (0.23, 0, −3.19): 5 m from the speaker at (1.1, 0, 1.73) (vector (−0.87, 0, −4.92), length 5.00), 20° off the speaker's axis in the horizontal plane (cos = 0.940), with a clear straight path (the line stays at x > 0.1, in front of the dish face) and about 88° off the dish axis, outside its beam; then it reads 0.333, above the 0.267 the first meter reads via the dish; for three other geometries the dish reading is 0.8 × the direct reading at the same total path within T-level (requirement outcome: cannot amplify). | `SoundDishTests.RedirectionNeverBeatsTheDirectPath` | — | d₁ + d₂ from 2 m to 8 m |
| 5 | Given a sustained source, then the redirected sound is one sustained occurrence and the meter triggers once. | `SoundDishTests.ASustainedToneIsRedirectedAsOne` | pulse source | — |
| 6 | Given a ball rolled at the dish, post and foot, then it stops at each collider; a meter directly behind the dish reads 0 (the dish shadows). | `SoundDishTests.TheDishIsSolidAndShadows` | dish removed | — |
| 7 | Reset clears in-flight and child occurrences; save then load round-trips the pose. No parameters and no ports: any link to it is refused. | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` plus `SoundDishTests.ResetClearsRedirectedSound` | — | Reset with a child occurrence in flight |

**Chrome recipe (pending, post-catalogue).** Battery → Speaker aimed at the dish 2 m away; a Wall blocks the speaker's direct line to a supplied Sound meter placed 3 m along the dish axis; aim the dish with the rotate gizmo.

## 6. Open questions

**Resolved.**

1. **Focus geometry.** The axis-beam redirect above, not a focal-point model: [D19](../invest/delivery-batches.md#3-family-level-decisions). It can be aimed by eye and its no-gain property holds for every geometry.
2. **Dish versus reflecting screen.** Moot: the screen does not reflect (owner decision, 21 November 2026). The redirect capability has one consumer, this part.
3. **Collider and art class for the dish.** A straight cylinder with the concavity drawn as shading, classed solid: the delivered art test admits solid art only as a box, a full sphere or a straight cylinder, and its arc class requires a torus-segment collider (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:206`–`:252`), so the earlier "arc chord" bowl would fail it.
4. **Coordinator decisions, 22 November 2026 (owner's standing delegation), applied in the audit repair.** (a) Acceptance half-angle 70°, axis beam and total-path law kept: a dish that cannot turn a corner does not teach its lesson, and no-gain still holds for every geometry. (b) Row 1 places the speaker 2 m away and 60° off axis; row 3 brackets the limit at 68° and 72°. (c) The dish art is a straight cylinder with shaded concavity (item 3). (d) Coverage is bound by the published section form and is blocked on S7 (section 7). (e) Palette order 537 (coordinator decision 36).

**Needs the owner.** None.

## 7. Integration

- **Works with:** Wall, Battery (delivered); Speaker, Bell, EL-179, Sound meter, EL-044 (planned before it); EL-046 (a horn placed on the beam captures it within its own 35° cone).
- **Campaign:** chapter 8, levels 71–80 ("reflecting dish"), first taught at **78a**, before the horn route (79a) (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)); reuse 81–100, 117–125, 136–150. [sequence-task-407](../requirements.md#sequence-task-407) ("Screens/dishes … each retain their later individual contract"); process [IX-12](../requirements.md#interaction-12).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7:** the published form's preconditions are not met. This identity lacks: (1) an acceptance pair of its own, since its ledger entry links only the umbrella `todo-352`, which has no co-anchored `sequence-task-NNN` in `requirements.md` and is shared by nine identities (the reader refuses two identities on one task source); (2) a decision anchor, since its ledger owner S538 has no `<a id="s538">` anchor in `docs/planning/invest/scope-corrections.md`. The delivering slice adds the section once S7 supplies both; it is not written earlier, because the reader throws on a malformed block. Mode records: `Configuration=Fixed`.
- **Lesson fixtures:** needed only where a level pre-places it. Such a level waits for [S8, lesson fixtures for any admitted package](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element, data-only.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception and occlusion candidates from it); then Story 14.0 amended with gaps 2, 8 and 11; Stories 14.2, 14.1 and 14.3; S7.
- **Files.** Adds `elements/SoundDish/` (`SoundDish.csproj`, `SoundDishElementDeclaration.cs`, `sound_dish.tscn`, `icon.svg`) and `CuriousContraptions.tests/SoundDishTests.cs`; registration is derived. The test file needs no `Compile` line (main compiles every top-level `*.cs` of the test project by one rule). Until S3 lands: `CuriousContraptions.slnx`, `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596`, `:653`. This spec gains its binding section when S7 allows (section 7).
- **Review tier.** Data-only.
- **Disqualified if** the probe finds no redirecting surface, or that child occurrences do not carry total path length (the no-gain proof then fails).
