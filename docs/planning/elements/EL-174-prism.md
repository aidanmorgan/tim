# EL-174 · Prism — named-identity spec

**Current delivery (24 October 2026):** [EL-174 delivery slice](../invest/named-elements.md#element-174). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`). Legacy citations use `path@a6c914e:Lstart-Lend`; citations without `@` are `path:line` in the checkout. Lengths are metres. The prism's long axis is local Z; light enters from −X and leaves toward +X. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) and are not restated here.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-174 |
| Name | Prism |
| Type | Optical |
| Anchor | [requirements.md#element-174](../requirements.md#element-174); named entry [named-elements.md#element-174](../invest/named-elements.md#element-174); source record [todo-306](../requirements.md#todo-306) |
| Owner | S520 (refinement S525 prism) |
| Disposition | To deliver as a **single element**. It refines no catalogue part. It is the inverse of the combiner ([EL-213](EL-213-optical-combiner.md)): one Relay inlet and three outlets, each passing one channel. |
| Planned package | id `prism`, directory `elements/Prism/`, class `PrismElementDeclaration` |
| Roadmap story | [EL-174 delivery slice](../invest/named-elements.md#element-174), `named-el-174`; after ENGINE-OPTICAL, the amber laser and the primary receivers |
| Status | not started; no package; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL, which follows ENGINE-SPATIAL-QUERIES and takes every candidate from its segment query |

## 2. Declaration

Shape followed: the combiner ([EL-213 section 2](EL-213-optical-combiner.md#2-declaration)), a static body with a Relay.

| Item | Value |
| --- | --- |
| Interfaces | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) plus the optical element interface **(optics)** |
| Body | One static scene body; material restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s |
| Cross-section | An equilateral triangle of side 1.0 m in the XY plane, centroid at the origin: apex (0, 0.577), base corners (±0.5, −0.289); face midpoints left (−0.25, 0.144), right (0.25, 0.144), bottom (0, −0.289) |
| Colliders | Three pane boxes 1.0 × 0.04 × 1.0 m (face width × thickness × length along Z), one centred on each face midpoint with its thin axis along that face's normal; two end-cap cylinders, axis Z, radius 0.577 m, length 0.04 m at z = ±0.52; foot box 1.2 × 0.12 × 1.2 m at (0, −0.637, 0), its top at y −0.577 where the two caps rest. Six colliders. |
| Relay **(optics)** | Inlet: disc aperture r 0.43 m on the left pane's outer face, 0.02 m out from the pane's mid-plane along its normal: centre (−0.267, 0.154, 0), normal (−0.866, 0.5, 0), front face only. Three outlets at the right pane's outer face centre (0.267, 0.154, 0), half-angle 0, τ 0.9 each: red, mask (1, 0, 0), direction +X turned −20° about Z; green, mask (0, 1, 0), direction +X; blue, mask (0, 0, 1), direction +X turned +20° about Z. Each channel has one outlet at 0.9, so no channel's outputs exceed its input. Internal path 0.534 m, the distance between the two centres. No enable: always open. A segment leaving an outlet ignores the pane face it leaves from. |
| Other faces | Absorb (the default): light on the right or bottom pane, the caps, the foot, or the left pane outside the inlet disc does not enter. |
| Ports, parameters, stores, bindings | None. Fan angles are fixed so puzzles stay predictable (D19). |
| Footprint | `ElementFootprint(1, 0, 0, 0)`; domain: the optical flag **(optics, M6)** |
| Catalogue | Id `prism`, "Prism", `ElementCategory.Optics` **(optics, M6)**, colour (0.40, 0.72, 0.79), palette order 629 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Splits a beam into its red, green and blue parts. It cannot add a colour the beam does not already carry." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/PaneLeft`, `PaneRight`, `PaneBottom` | boxes as the colliders, cyan `#66b8c9` at alpha 0.3 with ochre `#e8b764` edge lines | solid (three pane boxes) | nothing |
| `Visual/CapFront`, `CapBack` | discs r 0.577, thickness 0.04, cream `#fff8e9` | solid (two cap cylinders) | nothing |
| `Visual/Foot` | box 1.2 × 0.12 × 1.2 m at y −0.637, navy `#293954`; the caps, radius 0.577 m, stand on it and the bottom pane hangs 0.27 m above it between them | solid (foot box) | nothing |
| Inlet ring | cream ring r 0.43 printed flat on the left pane | decorative: printed flat | nothing |
| Exit marks | on the right pane, one, two and three cream bars beside small red `#de7058`, green `#62aa78` and blue `#5b9cdb` bars, in fan order, printed flat | decorative: printed flat | nothing |

The prism is a column of three flat glass panes closed by two round caps (decision K-15). The engine admits no wedge collider (`ColliderShapeKind`, `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`: Sphere, Box, Plane, Cylinder, Tube, Torus, Frustum), so a solid glass wedge could not be both drawn and collided; three boxes and two cylinders are drawn exactly as they collide, and the caps keep a ball from entering the hollow. Icon: an original navy outline of a triangle with one line in and three fanning out. It differs from the combiner's cube and the lens's flat frame by its triangular column.

### Compiled counts

Bodies 1 (static). Colliders 6 (4 boxes, 2 cylinders), as the filter's six. Relays 1 with 3 outlets. Everything else 0.

## 3. Engine capabilities

Binding shard ([element-02.json](../../coverage/engine/element-02.json)): FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, StateTransaction; navigation only (D14). Probe on `8d1d68aa`: 3 present, 0 untested, 1 narrower, 4 missing.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | the package's row in `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:206`) |
| R12 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R9 | Line-of-sight occlusion by solid colliders | narrower: airflow rows only, by a scan of every collider (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`, `:149`); the broadphase has only a pair-overlap query (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`); no crossing points | `AirflowLineClear` | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the ordered-crossing segment cast: **ENGINE-SPATIAL-QUERIES** |
| M2 | The optical solve: once per tick after the last substep, on ordered crossings from the spatial query, with range and interaction limits | **missing** | — | owner **ENGINE-OPTICAL** |
| M3 | Declared surface responses, here a Relay with several outlets, each with its own channel mask and direction | **missing** | — | owner **ENGINE-OPTICAL** |
| M5 | Beam read model and build-mode aim preview | **missing** | — | owner **ENGINE-OPTICAL** |
| M6 | `ElementCategory.Optics` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) and the optical domain flag | **missing** | — | owner **ENGINE-OPTICAL** |

ENGINE-OPTICAL is [its story in `epics.md`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-optical-beam-emission-transport-surface-interaction-and-reception). A Relay with one outlet serves the combiner and the gates; several outlets with masks serve the prism and the beam splitter's two branches. It is one capability with a declared outlet count, proposed in the [batch summary](readiness/batch-k.md).

**Dependencies.** The amber laser (CAT-036, an all-channel source), a colour laser for the control, Battery, the primary receivers ([EL-146](EL-146-red-selective-receiver.md) to EL-148), Wall.

## 4. Sources and legacy

**Sources.** Row [element-174](../requirements.md#element-174): declared dispersion separates only spectral channels present in incident light; red-only input creates no green or blue output. [todo-306](../requirements.md#todo-306): an explicit simplified spectral rule; pure red must not create green or blue; each path can be deliberately blocked. Refinement S525 **prism**; teaching set-up "Separate colours with a prism" ([refinements](../invest/refinements.md)). Campaign: prism first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (no prism; mechanisms only):
1. Single directed outlet record and Route interaction: routed ray leaves the outlet with power × transmission and remaining range minus internal travel — `engine/OpticalNetwork.cs@a6c914e:L11-L13`, `engine/OpticalNetwork.cs@a6c914e:L119-L125`. Carry forward, generalised to several outlets.
2. Route requires a finite directed outlet or the declaration is rejected — `engine/OpticalNetwork.cs@a6c914e:L58-L62`. Carry forward per outlet.
3. Routing retention 0.9 and the input/outlet layout — `parts/BeamCombinerPart.cs@a6c914e:L12-L20`. Carry forward as the retention.
4. Internal travel counts against range: total path = range ± 0.001 — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L91-L117`. Carry forward.
5. Channel masks are closed enums; filters never create channels — `engine/OpticalColour.cs@a6c914e:L6-L22`; `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L69-L103`. Carry forward.

**Files harvested:** `engine/OpticalNetwork.cs`, `parts/BeamCombinerPart.cs`, `engine/OpticalColour.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`.

## 5. Acceptance

### Behaviour and numbers

Starting values (D1).

- **Law.** A collimated segment ending on the inlet starts one segment per outlet whose masked power is not zero: P′ = 0.9 × mask ∘ P. An amber beam (10, 7.8, 3.2) W leaves as red (9, 0, 0) W at −20°, green (0, 7.02, 0) W straight on, and blue (0, 0, 2.88) W at +20°: 18.9 W out of 21 W in, the rest absorbed. A red laser's (10, 0, 0) W leaves as one red segment of 9 W and nothing else: a channel that is absent produces no segment. This is the declared dispersion: fixed directions by channel, not angles solved from indices.
- **Reaching the receivers.** 5 m from the outlet, adjacent paths are 2 × 5 × sin 10° = 1.74 m apart, so three 0.55 m receiver discs stand 0.64 m clear of one another. The blue 2.88 W is 15 % above the default 2.5 W threshold; the red and green have factors of 3.6 and 2.8.
- **Range and limits.** Each outlet segment has the incoming segment's remaining range less 0.534 m and one more interaction. A beam entering after 10 m leaves with 16 − 10 − 0.534 = 5.47 m.
- **What does not enter.** A beam on any face but the inlet disc is absorbed. A diverging segment (torch light, or a diverging lens's output) on the inlet is absorbed: only collimated segments are relayed (decision K-3).
- **Bodies.** Panes and caps are ordinary solids; a ball rebounds from them and cannot get inside.

### Rows

Planned class `PrismTests`. Fixture: Battery; a pulsed amber laser 3 m from the prism, its ray at the inlet centre; supplied red, green and blue receivers 5 m from the outlet on the three fan directions, each facing the outlet. Power tolerance ±2 %; stall guard 1 s (the family's laser values, as [CAT-036](CAT-036-laser.md)).

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| X1 | Given the fixture, then the red, green and blue receivers read (9, 0, 0), (0, 7.02, 0) and (0, 0, 2.88) W and all three levels are on | `PrismTests.AnAllChannelBeamFansIntoItsThreeChannels` | X2 | ±2 % per reading; the three sum to 18.9 W, at most 0.9 × the 21 W input |
| X2 | **Red-only input.** Given a red laser instead, then the red receiver reads 9 W and exactly one segment leaves the prism; the green and blue receivers read nothing (ledger outcome) | `PrismTests.AnAbsentChannelCreatesNoOutput` | X1 | outlet segment count 1 |
| X3 | Given a Wall across the green path, then only the green receiver goes dark | `PrismTests.EachPathCanBeBlockedAlone` | X1 | red and blue unchanged within 2 % (coordinator decision 21) |
| X4 | Given the beam on the right pane, the bottom pane, a cap, or the left pane 0.46 m from the inlet centre, then nothing leaves any outlet; given torch light on the inlet, nothing leaves | `PrismTests.OnlyCollimatedLightOnTheInletIsDispersed` | X1 | 0.03 m outside the 0.43 m disc |
| X5 | Given the laser 10 m from the inlet and the green receiver 5 m beyond the outlet, lit; at 6 m, dark | `PrismTests.DispersedBeamsKeepTheRemainingRange` | X1 | about 0.5 m either side of the 5.47 m remainder |
| X6 | Given the three outputs returned by mirrors into a combiner, then a detector on its outlet reads 0.9 × 0.95 × 0.9 of each channel: (7.70, 6.00, 2.46) W | `PrismTests.APrismAndACombinerRecombineWithBothLosses` | X1 | ±2 % |
| X7 | Given a Tennis ball thrown at a pane and at a cap, then it rebounds and its centre never enters the column | `PrismTests.ThePrismIsSolidToBodies` | — | contact within 5 mm of the drawn surface |
| X8 | Given a lit run, when Reset, then no segment or reading remains; a saved and reloaded construction keeps the prism's rotation and behaves as X1 | `PrismTests.ResetAndReloadRestoreTheDarkState`; authored state by R12 | X1 | — |

No port and no parameter, so no refusal rows. Chrome recipe (pending, post-catalogue): Battery, triggered amber laser aimed at the ringed face, three colour receivers on the fan 5 m beyond, each with a lamp circuit; Run.

## 6. Open questions

None open.

1. **Fan angles, retention, size and collider — decided** (D1, K-15): ±20° and straight on; 0.9; side 1.0 m; three panes and two caps, because no wedge collider is admitted and none is needed.
2. **Interaction count — decided** (D19): each outlet segment carries the incoming count plus one.
3. **Torch cones — decided** (K-3): not dispersed; absorbed at the inlet.
4. **Roadmap — resolved**: its own slice; the multi-outlet Relay is ENGINE-OPTICAL's.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Amber laser, colour lasers, primary receivers, combiner, mirrors, white receiver [EL-152](EL-152-white-selective-receiver.md) (as a negative: after the prism no single path satisfies it) (undelivered); Wall, balls, Battery (delivered). No ports.
- **Campaign.** Prism first use 51–60; teaching set-up "Separate colours with a prism".
- **Coverage binding (planned, not yet live).** A named package is bound by the binding section that main's `tools/Coverage/NamedPackageSources.cs` reads; **blocked on the coverage follow-on story S7** (decision S520 has no anchor in `docs/planning/invest/scope-corrections.md`). S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15); the section is not written here.
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL as corrected in the batch summary (Relay with several masked outlets); the amber laser; the three primary receivers; the mirror and combiner for row X6 (queued until delivered); S7 for the binding.
- **Files.** `elements/Prism/Prism.csproj`, `PrismElementDeclaration.cs`, `prism.tscn`, `icon.svg`; one guarded `CuriousContraptions.slnx` line; `CuriousContraptions.tests/PrismTests.cs`; a `VisibleSolidColliderTests` row; this spec's binding section.
- **Review tier.** Data-only if the probe finds every row present after ENGINE-OPTICAL.
- **Disqualifiers.** A Relay limited to one outlet or without a per-outlet channel mask; an aperture that cannot lie on a rotated box face; a different unit scale.

## 9. Engine conformance

Verdict: **Conforms.**

1. **Declaration data only.** Six colliders and one Relay with three outlet rows.
2. **Generic laws.** The Relay law with channel masks; contact for bodies. Dispersion is declared outlet data.
3. **Three layers.** Colliders: four boxes and two cylinders. Visuals: the same six solids and flat prints. Animation: none.
4. **One-way flow and typing.** One segment in, up to three out; channels are the closed set.
5. **Flat state.** Counts in section 2.
6. **Performance fitness.** Static; six primitives, the least that draw a closed triangular column from admitted shapes; at most three new segments per arriving segment, inside the per-emitter limit.
7. **Tests are scenarios.** Rows X1 to X8.
