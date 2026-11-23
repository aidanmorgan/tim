# RAD-07 · Collimator block — element readiness spec

**Current delivery (24 October 2026):** [RAD-07 delivery slice](../invest/named-elements.md#radiation-07). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-07](../requirements.md#radiation-07) (P1 potential). Lengths are metres; the channel runs along local X. Units, laws and the Dense row are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions); all values are game fiction.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-07 · Collimator block |
| Type | Radiation (passive absorbing geometry) |
| Anchor | [requirements.md#radiation-07](../requirements.md#radiation-07); [named-elements.md#radiation-07](../invest/named-elements.md#radiation-07) |
| Owner | S614 |
| Disposition | To deliver as a single element. It refines no CAT part and duplicates none: it is the only part whose radiation behaviour is the shape of a hole. It is not a lens ([TH-06](../invest/named-elements.md#thermal-06) is a different family). |
| Planned package | id `collimator_block`, directory `elements/CollimatorBlock/`, class `CollimatorBlockElementDeclaration` |
| Related identities | Wall row shared with [EL-128](EL-128-dense-shield.md); sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); receivers [RAD-08](RAD-08-radiation-rate-meter.md) |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), which includes ENGINE-PARAMETER-BOUND-FRAMES, and on ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); no row is longer than 2 s plus the stall guard |

## 2. Declaration

Shape followed: the Wall for a static body whose box sizes follow a parameter (`elements/Wall/WallElementDeclaration.cs:42`–`:44`). The collimator has no collimating law: it is four Dense boxes around a real square channel, and the general chord attenuation does the rest. Items marked **(field)** or **(frames)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IParameterizedGeometryElement` (`SceneGeometry.cs:29`) |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the static default, `elements/Battery/BatteryElementDeclaration.cs:61`–`:64`) |
| Colliders (hidden `col_` proxies) | Block envelope 0.40 × 0.60 × 0.60 m; channel width w from `aperture`. Top and bottom boxes 0.40 × (0.60 − w) ÷ 2 × 0.60 m at y = ±(0.15 + w ÷ 4). Left and right boxes 0.40 × w × (0.60 − w) ÷ 2 m at z = ±(0.15 + w ÷ 4). Four boxes that touch and do not overlap (radiation convention 4), all with the **Dense** row **(field)**. The bottom box rests on the bench; there is no separate base, so the channel axis is 0.30 m above the bench. |
| Wall thickness by option | Narrow (w 0.12): 0.24 m, centres ±0.18. Medium (w 0.16): 0.22 m, centres ±0.19. Wide (w 0.40): 0.10 m, centres ±0.25. |
| Ports | None. A wire to the block has nothing to attach to. |
| Parameters | `aperture`: Choice, unit None, options Narrow, Medium, Wide, default Medium, build only, player-editable unless the level locks it (D11). Each option carries one value in metres (0.12, 0.16, 0.40), bound affinely to the four boxes' half-extents and centre offsets **(frames, ENGINE-PARAMETER-BOUND-FRAMES)**. |
| Stores | None. Power removed in the walls is ledgered to them (radiation convention 10); none is re-emitted. |
| Presentation bindings | The four `Visual/Wall*` nodes take the same parameter-bound sizes and offsets as their colliders. `Visual/Notches` shows one, two or three printed notches by option **(frames)**. No animation. |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domains Mechanical and the radiation flag **(field)** |
| Catalogue | Id `collimator_block`, "Collimator", `ElementCategory.Radiation` **(field)**, palette order 707 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "A dense block with a hole through it. Radiation passes along the hole and is absorbed everywhere else." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/WallTop`, `Visual/WallBottom`, `Visual/WallLeft`, `Visual/WallRight` | boxes equal to their colliders, navy `#293954`, three grooves printed flat on the outer faces | solid (the four boxes) | nothing; sized by `aperture` at build |
| `Visual/Rim` | cream `#fff8e9` band printed flat around the channel mouth on both end faces | decorative: printed flat | nothing |
| `Visual/Notches` | one to three gold `#f7cb52` notches printed flat on the top face | decorative: printed flat | nothing |

No proud bezel is drawn. Icon: a square block with a square hole and an arrow through it. It differs from the Dense shield by the hole and the block depth, and the notch count tells the presets apart by shape.

### Compiled counts

Bodies 1 (static). Colliders 4 boxes. Joints 0. Drives 0. Sensors 0. Signal and supply nodes 0. Stores 0. Parameters 1. Geometry bindings 10 (top and bottom: one half-extent and one offset each; left and right: two half-extents and one offset each). Presentation bindings: the matching size bindings and one notch selector.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport. This probe, made on `8d1d68aa`, is the authority until the slice re-runs it: **4 present, 0 untested, 1 narrower, 4 missing**. ENGINE-SPATIAL-QUERIES and ENGINE-PARAMETER-BOUND-FRAMES are stories of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R3 | A box half-extent bound affinely to a Real parameter | present | `IParameterizedGeometryElement` (`SceneGeometry.cs:29`); the Wall's three bindings (`elements/Wall/WallElementDeclaration.cs:42`–`:44`) | `WorkshopWallTests.ThreeCanonicalDimensionsPoseAndIdentityRoundTripAtDefaultAndBothLimits` (`CuriousContraptions.tests/WorkshopWallTests.cs:15`): compiles the Wall at its default and both limits and asserts the box's half-extents are half of `width`, `height` and `thickness` (`:29`–`:30`) |
| R4 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) serves airflow rows only and loops over every collider (`:149`) | — | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast returning ordered crossings: **ENGINE-SPATIAL-QUERIES** |
| M1 | Choice-bound geometry: a Choice option that carries a value, bound to a child collider's half-extents and centre offset, and to the art node shown | **missing** (`docs/planning/elements/readiness/engine-design-gap.md:149`: "Parameters binding joint frames, ports and child colliders") | — | owner **ENGINE-PARAMETER-BOUND-FRAMES** (choice-bound values and parameter-bound geometry, coordinator decision 16) |
| M2 | The shared emitter, segment and receiver solve | **missing** | — | owner **ENGINE-OPTICAL** |
| M3 | Radiation channels and per-collider rows crossed by chord | **missing** | — | owner **ENGINE-RADIATION** |
| M4 | `ElementCategory.Radiation` and the radiation domain flag | **missing** (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) | — | owner **ENGINE-RADIATION** |

Nothing radiation-specific exists in `CuriousContraptions.Abstractions*`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle`, `engine/` or `elements/`. ENGINE-RADIATION ([Story ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure), sprint key `engine-radiation`) is the one owner of the radiation-specific rows.

**Dependencies.** A source and meters (RAD-01, RAD-08, undelivered); the Tennis ball (delivered) for the solidity row.

## 4. Sources and legacy

- [radiation-07](../requirements.md#radiation-07): "An aligned receiver responds while an off-axis control does not; a narrower opening never increases total transmitted power."
- [Named entry](../invest/named-elements.md#radiation-07), owner S614; map row; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research row ("addresses one receiver, excludes its neighbour"; slots 81 "The Narrow Door", 89).
- **Legacy.** None. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1839-L1868`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions). The Dense row is 20 /m for PhotonMedium, so a ray that runs L metres inside a wall keeps e^(−20 L): e^−0.5 = 0.61 at 0.025 m, e^−2.6 = 0.07 at 0.13 m, under e^−4.6 = 0.01 beyond 0.23 m.

**Reference layout.** Block at the origin (x from −0.20 to +0.20), resting on the bench, so the channel axis is 0.30 m above it. Gamma capsule centre at x = −0.40 (its 0.15 m radius clears the rear face by 0.05 m; its 0.40 m base box reaches x = −0.20 and touches the block); the capsule's centre is 0.26 m above the bench ([RAD-01](RAD-01-gamma-source-capsule.md) section 2: base 0.06 m under a 0.40 m cylinder), 0.04 m below the channel axis. Aligned standard meter with its face at x = +1.10, centred on the axis in z; its aperture centre is 0.36 m above the bench ([RAD-08](RAD-08-radiation-rate-meter.md) section 2: a 0.60 m case centred on its origin over a 0.06 m base, so 0.30 + 0.06 m), 0.06 m above the channel axis and 0.10 m above the capsule. Control meter with its face at x = +1.10, centred at z = +0.45 at the same height, also facing −X. The two meters are placed one per case: their 0.60 m-wide bases 0.45 m apart would overlap.

- **Aligned meter.** Nine samples at heights 0, 0.10 and 0.20 m above the capsule and z of 0 and ±0.10 m, 1.5 m along x: sample term 10 × 1.5 ÷ d³ with d² = 2.25 + y² + z². Terms 4.444, 4.415 twice; 4.415, 4.386 twice; 4.329, 4.300 twice; mean 39.39 ÷ 9 = 4.377 W/m²; R = 0.09 × 4.377 = 0.394 W (the axis formula gives 0.9 ÷ 1.5² = 0.400 W). Clearance: relative to the channel axis the capsule is at −0.04 m and the samples at −0.04, +0.06 and +0.16 m in height and 0 and ±0.10 m in z. At the block's exit (0.60 m of the 1.50 m from the source) the highest ray is at −0.04 + 0.20 × 0.60 ÷ 1.50 = +0.04 m and the widest at 0.10 × 0.60 ÷ 1.50 = 0.04 m in z; at the entry they are nearer the axis. Both are inside Narrow's half-width of 0.06 m, so all nine samples are clear for every preset: 0.394 W, on, for Narrow, Medium and Wide.
- **Control meter, open path.** Sample columns at z = 0.35, 0.45 and 0.55 and the same three heights: terms 4.105, 4.079, 4.003; 3.905, 3.882, 3.812; 3.678, 3.657, 3.594; mean 34.715 ÷ 9 = 3.857 W/m²; R = 0.347 W.
- **Control meter through the block, sample columns at z = 0.35, 0.45, 0.55** (slopes 0.233, 0.300, 0.367 per metre of x; a ray is z = slope × (x + 0.40)). The heights stay inside every channel's half-width, so only z decides.
  - Narrow (half-width 0.06). The rays reach z = 0.06 at x = −0.143, −0.200 and before the rear face. Chords inside the wall: 0.343 × 1.027 = 0.352 m, 0.400 × 1.044 = 0.418 m, 0.400 × 1.065 = 0.426 m. Transmissions e^−7.0 = 0.0009, e^−8.4 = 0.0002, e^−8.5 = 0.0002. Reading about 0.347 × 0.0004 = 0.0001 W: off.
  - Medium (0.08). Entry at x = −0.057, −0.133, −0.182. Chords 0.257 × 1.027 = 0.264 m, 0.333 × 1.044 = 0.348 m, 0.382 × 1.065 = 0.407 m. Transmissions 0.0051, 0.0010, 0.0003. Reading about 0.347 × 0.0021 = 0.0007 W: off.
  - Wide (0.20). At the exit the three columns are at z = 0.14, 0.18 and 0.22. The first two are clear. The third enters the wall at x = 0.20 ÷ 0.367 − 0.40 = 0.145: chord 0.055 × 1.065 = 0.058 m (0.0581, 0.0582 and 0.0585 m for its three heights), transmission e^(−20 × 0.058) = 0.313, 0.312 and 0.310. Reading 0.09 × (23.786 + 3.678 × 0.313 + 3.657 × 0.312 + 3.594 × 0.310) ÷ 9 = 0.09 × 27.19 ÷ 9 = 0.272 W: on.
- **Coverage for throughput.** Open areas are 0.0144, 0.0256 and 0.16 m². Widening only removes Dense from paths, so at every receiver position the reading can only rise from Narrow to Medium to Wide, and never above the open-path reading; what the walls absorb on those paths can only fall. The block is not a lens or amplifier: no row or relay creates power.
- **Solidity.** The channel is real geometry. A Tennis ball at its default 0.25 m radius (0.50 m across) does not fit even Wide (0.40 m) and stops at the face. Wide's channel floor is 0.10 m above the bench and its ceiling 0.50 m: a ball with `radius` 0.15 m set on that floor fits, with 0.05 m to each side wall and 0.10 m above it. No delivered ball at its default size fits.
- **Tick order.** The field solve of tick n runs after its last substep on this tick's final poses, here the static walls at their authored pose, and readings commit with tick n (radiation convention 11).

### Rows

Planned class `CollimatorBlockTests`. Fixture: the reference layout with one supplied meter per case, fed from a Battery. Readings ±3 %; stall guard 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| C1 | Given each preset in turn, then the aligned meter reads 0.394 W and its level is on (row: an aligned receiver responds) | `CollimatorBlockTests.TheAlignedReceiverRespondsThroughEveryAperture` (theory) | C2 | ±3 % |
| C2 | Given Narrow or Medium, then the control meter reads under 0.005 W and stays off for 240 ticks; given Wide, it reads 0.272 W and is on (row: an off-axis control does not; presets trade coverage) | `CollimatorBlockTests.OnlyTheWideApertureCoversTheNeighbour` (theory) | C1 | ±3 %; 0.005 W is 97 % under the 0.15 W release |
| C3 | Given a meter placed in turn at z = 0, 0.15, 0.30, 0.45 and 0.60 m, then at each the reading with Narrow ≤ Medium ≤ Wide ≤ the reading with the block removed; the sum over the five placements is non-decreasing and the power ledgered to the four walls non-increasing from Narrow to Wide (row: a narrower opening never increases total transmitted power; not an amplifier) | `CollimatorBlockTests.NarrowingNeverRaisesAnyReading` | block removed | each inequality within 3 % of the open reading |
| C4 | Given the block turned 90° about Y with the capsule at x = −0.50, then the aligned meter is off: the path now crosses 0.60 − w of Dense (Narrow e^(−20 × 0.48) = 7 × 10⁻⁵, Medium e^(−20 × 0.44) = 1.5 × 10⁻⁴) | `CollimatorBlockTests.TheChannelDirectionIsThePartsOrientation` | C1 | under 0.005 W for Narrow and Medium |
| C5 | Given a Tennis ball of `radius` 0.15 m set on the floor of the Wide channel at its rear mouth with the block tilted 10° about Z so the channel falls along +X (tilted, its low corner drops 0.20 × sin 10° + 0.30 × (1 − cos 10°) = 0.035 + 0.005 = 0.039 m, so the block is placed 0.04 m higher on a support), then the ball rolls out of the far mouth (centre beyond x = +0.20); the same ball against the Medium block's mouth, and a ball at the default radius against the Wide block's mouth, stay outside | `CollimatorBlockTests.TheChannelIsRealGeometry` | ball beside the block | ball centre beyond x = +0.20 or not |
| C6 | Given an option index outside 0 to 2, or a non-integer, then the edit is refused and nothing changes | `CollimatorBlockTests.AnUndefinedApertureIsRefused`; generic refusal of out-of-range Real values by `TennisBallTests.InvalidConfigurationCannotEnterConstruction` (`CuriousContraptions.tests/TennisBallTests.cs:188`) | C1 | typed refusal |
| C7 | Given each preset, then the four compiled boxes have the sizes and centres of section 2 and the art matches them | `CollimatorBlockTests.EachApertureCompilesItsFourWalls`; `VisibleSolidColliderTests` row | — | 0.001 m |
| C8 | Given a Run, when Reset, and after save and load, then `aperture` and the readings of C1 and C2 are as authored | `CollimatorBlockTests.ResetAndReloadKeepTheAperture`; authored state by R4 | C1 | ±3 % |

No port, so no connection-refusal row. Chrome recipe (pending, post-catalogue): capsule, collimator, two meters side by side each lighting a lamp; switch `aperture` between Narrow and Wide.

## 6. Open questions

None open.

1. **Widths, length and rate law — decided** (D1, D16): 0.12, 0.16 and 0.40 m in a 0.40 m long block; the law is the conventions'.
2. **Square or slit — decided** (D19): square. No row or lesson needs a one-dimensional selection.
3. **Preset as Choice — decided now**: the row says "aperture presets", so the setting is a closed Choice, each option carrying one width, bound to the four walls and the notch art by ENGINE-PARAMETER-BOUND-FRAMES (choice-bound values and parameter-bound geometry, coordinator decision 16).

Needs the owner: none.

## 7. Integration

- **Collaborators.** RAD-01, RAD-02, RAD-08 and the gauges EL-129 and EL-130 (undelivered); Tennis ball (delivered). No ports.
- **Processes.** [IX-15](../requirements.md#interaction-15) (finite aperture coverage).
- **Campaign.** Chapter 11, levels 106 and 114; reuse 111–120, 124–130, 136–150, including the inspection-post capstone with the gauges. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-PARAMETER-BOUND-FRAMES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01 and RAD-08; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story).
- **Files.** `elements/CollimatorBlock/CollimatorBlock.csproj`, `CollimatorBlockElementDeclaration.cs`, `collimator_block.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/CollimatorBlockTests.cs`; a row in `VisibleSolidColliderTests`. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** No choice-bound value for a child collider's half-extents and centre (the four walls cannot follow `aperture`; the slice stops and ENGINE-PARAMETER-BOUND-FRAMES is amended; no second design is kept here); a different unit scale (re-derive section 5); a segment cast that does not return every crossing on its segment (a path may cross two walls; there is no crossing cap, radiation convention 12 and coordinator decision 26).

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** One static body, four boxes, one parameter, ten geometry bindings, one radiation row. No code names the collimator.
2. **Generic laws.** Chord attenuation and contact. "Selects directions" is the consequence of Dense walls around a hole; there is no acceptance-cone rule.
3. **Three layers.** Colliders: four boxes. Visuals: four boxes of the same sizes, printed marks. Animation: none.
4. **One-way flow and typing.** Parameter → compiled geometry → field solve → readings. The preset is a closed Choice; widths are f32.
5. **Flat state.** Four colliders at every option; counts fixed at compile time.
6. **Performance fitness.** Static body; four primitives, the fewest that leave a real square hole; a path crosses at most two of them, and each shadow cast is one spatial-query segment cast returning its ordered crossings (radiation convention 12); a resting static part costs the broadphase nothing beyond its tree leaves.
7. **Tests are scenarios.** Rows C1 to C8 run the production simulation through placement, the parameter, Run and committed reads.
