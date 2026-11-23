# RAD-01 · Gamma source capsule — element readiness spec

**Current delivery (24 October 2026):** [RAD-01 delivery slice](../invest/named-elements.md#radiation-01). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-01](../requirements.md#radiation-01) (P1 potential). Lengths are metres. This is a **constant-strength sealed toy source paid from a finite inventory**; every value is game fiction, never real-world source or shielding guidance. Units, sources, the propagation law and tick order are the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions); this spec states only its own values.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-01 · Gamma source capsule |
| Type | Radiation (photon source) |
| Anchor | [requirements.md#radiation-01](../requirements.md#radiation-01); [named-elements.md#radiation-01](../invest/named-elements.md#radiation-01) |
| Owner | S608 |
| Disposition | To deliver as a single element, the first radiation element slice. It refines no CAT part and duplicates none. [RAD-13](RAD-13-decay-clock-capsule.md) reuses its body with a decaying inventory and is its own element. |
| Planned package | id `gamma_source_capsule`, directory `elements/GammaSourceCapsule/`, class `GammaSourceCapsuleElementDeclaration` |
| Related identities | Receiver [RAD-08](RAD-08-radiation-rate-meter.md); shields [EL-126](EL-126-thin-screen-shield.md), [EL-127](EL-127-polymer-shield.md), [EL-128](EL-128-dense-shield.md); controllable alternative [RAD-02](RAD-02-powered-x-ray-emitter.md); decaying variant [RAD-13](RAD-13-decay-clock-capsule.md). No CAT spec. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); the longest row is 25 s |

## 2. Declaration

Shape followed: the Battery for a static part with a base (`elements/Battery/BatteryElementDeclaration.cs:60`–`:64`). Items marked **(field)** use contracts that do not exist yet; the slice's capability probe restates them.

| Item | Value |
| --- | --- |
| Interfaces | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the emitter interface **(field)** |
| Body | One static scene body; restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the static default, `elements/Battery/BatteryElementDeclaration.cs:62`) |
| Colliders (hidden `col_` proxies) | Capsule cylinder, axis Y, radius 0.15 m, height 0.40 m at the origin. Base box 0.40 × 0.06 × 0.40 m at (0, −0.23, 0), touching the capsule. Two colliders; neither declares a radiation row, so both take the scene's default row. |
| Ports | None. The requirement gives the source no electrical off switch; with no declared port, no connection to the capsule can be made. |
| Source **(field)** | Frame body 0; origin (0, 0, 0); isotropic (half-angle 180°, Ω = 4π); range 16 m; radiant power 125.66 W in PhotonMedium (4π × 10 W/sr); paid by a steady finite inventory (half-life none) of 125.66 W × 300 s = 37,699 J; no enable; internal occluders = both colliders. |
| Signal and supply graphs | None. |
| Parameters | None: constant strength. Pose is set in Build. |
| Stores | The steady inventory above (radiation convention 2). Each tick debits 125.66 ÷ 120 = 1.047 J. It holds exactly the longest admissible Run, 300 s (ENGINE-RUN-LENGTH, the story of owner decisions 13 and 17; it only sizes this declared inventory and is not a prerequisite of this slice), so the strength is constant throughout any Run; Reset restores 37,699 J (owner decision of 21 November 2026; coordinator decision 21, 22 November 2026). |
| Presentation bindings | None during Run: no glow, strobe or particles. The selected-part Build preview is ENGINE-OPTICAL's shared one (radiation convention 13). |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 0, PrismaticConstraints: 0, RevoluteConstraints: 0)`; the radiation domain flag **(field)** |
| Catalogue | Id `gamma_source_capsule`, "Gamma capsule", `ElementCategory.Radiation` **(field)**, palette order 701 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "A sealed source that shines the same in every direction for the whole Run. Nothing switches it off: move it away or shield it." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Capsule` | cylinder r 0.15 m, height 0.40 m, cream `#fff8e9` | solid (capsule cylinder) | nothing |
| `Visual/Base` | box 0.40 × 0.06 × 0.40 m at y −0.23, navy `#293954` | solid (base box) | nothing |
| `Visual/CapBand` | gold `#f7cb52` band printed flat round the top 0.06 m of the cylinder | decorative: printed flat | nothing |
| `Visual/Mark` | γ printed flat on the side | decorative: printed flat | nothing |

Icon: an upright capsule with a γ. It differs from the Decay capsule by a plain side (that one prints an hourglass band and tick marks) and from the Neutron module by its round section (that one is a box).

### Compiled counts

Bodies 1 (static). Colliders 2 (1 cylinder, 1 box), fewer than the Battery. Joints 0. Drives 0. Sensors 0. Signal nodes 0. Supply nodes 0. Sources 1. Inventories 1 (steady, 37,699 J). Presentation bindings 0.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport, RadioactiveDecay. This probe, made on `8d1d68aa`, is the authority until the slice re-runs it. 4 present, 0 untested, 1 narrower, 4 missing. ENGINE-SPATIAL-QUERIES is a story of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | A cylinder collider | present | `ColliderShapeKind.Cylinder` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `WeightTests.TheWeightIsAMassBoundDynamicCylinderWithALoadTieAndNoPayloadRole` (`CuriousContraptions.tests/WeightTests.cs:39`) |
| R3 | Every visible solid has a collider | present | per-package row of the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R4 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) answers clear or blocked for airflow rows only and loops over every collider (`:149`); the broadphase offers only `QueryOverlaps` (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`) | — | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast returning ordered crossings: **ENGINE-SPATIAL-QUERIES** |
| M1 | A declared emitter and the shared segment and receiver solve | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels, a steady finite inventory that pays an emitter, per-collider attenuation rows with the scene's default row, and chords | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | `ElementCategory.Radiation` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12` has five members) and the radiation `SimulationDomain` flag | **missing** | — | owner **ENGINE-RADIATION** |
| M4 | The selected-part Build preview that runs the same solve and commits nothing | **missing** | — | owner **ENGINE-OPTICAL** (optical convention 12; radiation convention 13) |

Nothing radiation-specific exists in the engine or the packages (searched 22 November 2026; see [RAD-08 section 3](RAD-08-radiation-rate-meter.md#3-engine-capabilities)). ENGINE-RADIATION ([Story ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure), sprint key `engine-radiation`) is the one owner of the radiation-specific rows.

**Dependencies.** None delivered is needed to place it. Its rows read a receiver declared in a law-scene fixture of the production engine, the way `AirflowGeneralityTests` builds a `LawScene`, because the Rate meter is delivered after it.

## 4. Sources and legacy

- [radiation-01](../requirements.md#radiation-01): "Moving the capsule farther away lowers the meter reading; disconnecting a nearby battery does not stop emission. Source pose and initial state restore exactly."
- [Named entry](../invest/named-elements.md#radiation-01), owner S608; map row (S605); binding `radiation-01.json` (proof S608).
- [S605](../invest/decisions.md#s605) photon and decay rows; research per-element row and [first-slice acceptance](../../radiation-component-research.md#chrome-observable-acceptance-for-the-first-radiation-slice) (slot 76 "The Quiet Beacon").
- **Legacy.** No gamma source in `parts/`, engine, tests, `content/puzzles.json`, `tools/Campaign` or `reference/`. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1628-L1658`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Strength.** 125.66 W over 4π sr is 10 W/sr. The number is chosen so the standard detector reads close to 1 W at arm's length and loses its 0.2 W mark at about 2 m, a region that suits the 16.8 m bench.
- **Distance.** The standard detector (0.09 m², nine samples) reads 0.882 W at 1 m (derivation in [RAD-08 section 5](RAD-08-radiation-rate-meter.md#5-acceptance)). At 2 m: centre sample 10 ÷ 4 = 2.5 W/m²; edge d² = 4.01, facing 0.9988, 2.491; corner d² = 4.02, facing 0.9975, 2.481; mean 2.488; R = 0.09 × 2.488 = 0.224 W. At 3 m the axis formula gives 0.9 ÷ 9 = 0.100 W. So 0.882 → 0.224 → 0.100 W: farther is lower.
- **Inside 1 m.** For a source in front of the face every sample term is I ÷ 1 m² = 10 W/m² with facing 1 (radiation convention 3; coordinator decision 21, 22 November 2026), so the standard detector reads 0.09 × 10 = 0.900 W anywhere inside 1 m, touching the capsule included, and never more. The reading never rises as the capsule moves away: 0.900 W inside 1 m, 0.882 W at 1 m (2 % lower, inside the ±3 % tolerance, so no row orders these two), then 0.224 and 0.100 W.
- **Shielding.** One Dense slab (0.05 m, 20 /m) at 1 m: 0.882 × e^−1 = 0.882 × 0.368 = 0.324 W. Two: 0.882 × e^−2 = 0.882 × 0.135 = 0.119 W.
- **No switch.** The source has no supply node and no signal node, so no electrical state reaches it. A Battery beside it, enabled or not, changes nothing.
- **Constant strength, paid.** The capsule emits 125.66 W at every tick of a Run from its 37,699 J inventory; each tick debits 125.66 ÷ 120 = 1.047 J. After 25 s the inventory holds 37,699 − 125.66 × 25 = 37,699 − 3,142 = 34,557 J. It reaches 0 only at the end of a 300 s Run, the longest ENGINE-RUN-LENGTH admits, so the power never falls within a Run. Reset restores 37,699 J. This is the owner decision of 21 November 2026 (constant strength) with coordinator decision 21 of 22 November 2026 (a finite inventory sized for the 300 s maximum Run). [IX-16](../requirements.md#interaction-16) ("spent inventory cannot emit indefinitely") holds as written. The decaying case is [RAD-13](RAD-13-decay-clock-capsule.md).
- **Own housing.** Both colliders are internal occluders of the capsule's own emission, so it does not shield itself. They take the default row and attenuate other sources' rays like any collider: through the 0.30 m cylinder PhotonMedium keeps e^(−0.15 × 0.30) = e^−0.045 = 0.956.
- **Energy.** The capsule emits 125.66 J each second, paid from its inventory. A 0.09 m² face at 1 m subtends about 0.09 sr of 4π, so it can receive at most 125.66 × 0.09 ÷ 12.566 = 0.9 W; the 0.882 W reading is inside that. Inside 1 m the flat rule credits 0.900 W per standard face; should receivers ever ask more than the segment's power in total, every share is scaled by power ÷ sum (radiation convention 7), so received never exceeds emitted.
- **Tick order.** The field solve of the first tick of Run runs after that tick's last substep, on its final poses; it debits the tick's 1.047 J and commits the reading with the tick (radiation convention 11). A contact that reads it acts from the next tick.

### Rows

Planned class `GammaSourceCapsuleTests`. Fixture: the capsule in a production law scene with one receiver row of the standard aperture (0.09 m², nine samples, Lambert), PhotonMedium weight 1, facing the capsule. Readings and energies ±3 %; stall guard 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given the receiver 1 m from the capsule, when Run starts, then the committed reading is 0.882 W from the first committed tick | `GammaSourceCapsuleTests.TheCapsuleEmitsFromTheFirstTickOfRun` | no capsule reads 0 | ±3 % |
| A2 | Given the capsule at 1 m, 2 m and 3 m, then the readings are 0.882, 0.224 and 0.100 W, each lower than the last (row: moving it farther lowers the reading) | `GammaSourceCapsuleTests.MovingTheCapsuleAwayLowersTheReading` | A1 | ±3 % each; strict order (each step is at least 55 % lower) |
| A3 | Given a Battery placed beside the capsule and a second Run with that Battery disabled by its run control at tick 120, then the reading is 0.882 W before and after (row: disconnecting a nearby battery does not stop emission) | `GammaSourceCapsuleTests.ANearbyBatteryNeverChangesEmission` | A1 | ±3 % |
| A4 | Given the declaration, then it has no port; an edit that connects a Battery's `Supply` to the capsule is refused and the construction is unchanged | `GammaSourceCapsuleTests.TheCapsuleDeclaresNoPortAndRefusesEveryConnection` | a Battery to a Powered gate is accepted | typed refusal |
| A5 | Given a Run of 25 s, then the reading at 1 m is 0.882 W at 1 s and at 25 s, and the committed inventory has been debited 125.66 × 25 = 3,142 J, leaving 34,557 J | `GammaSourceCapsuleTests.StrengthIsConstantAndPaidForTheWholeRun` | RAD-13 halves (its own row) | ±3 % reading; debit ±3 % |
| A6 | Given receivers 1 m away along +X, −X, +Z, −Z and +Y, then all read the same | `GammaSourceCapsuleTests.EmissionIsTheSameInEveryDirection` | A1 | ±3 % |
| A7 | Given a law-scene box 0.05 m thick with the Dense row on the path, the reading is 0.324 W; two give 0.119 W; the box beside the path leaves 0.882 W | `GammaSourceCapsuleTests.AShieldOnThePathLowersTheReading` | A1 | ±3 % |
| A8 | Given the receiver touching the capsule (aperture 0.15 m from its axis) and at 0.5 m, then both read 0.900 W and never above it (row: moving farther never raises the reading) | `GammaSourceCapsuleTests.TheReadingIsFlatInsideOneMetre` | A1 (0.882 W at 1 m) | ±3 %; never above 0.900 W |
| A9 | Given a Run, when Reset, then no reading remains, the inventory is 37,699 J again and the next Run reads as A1; the pose is the authored pose; a saved and reloaded construction behaves as A1 (row: pose and initial state restore) | `GammaSourceCapsuleTests.ResetAndReloadRestoreTheAuthoredSource`; authored state by R4 | A1 | pose within 0.01 m; inventory ±3 % |
| A10 | Given the Build preview of a selected capsule, then no reading is committed and the inventory stays 37,699 J | `GammaSourceCapsuleTests.TheBuildPreviewCommitsNothing` | A1 | reading 0 |

There is no parameter, so no parameter-refusal row. The Rate meter's rows A1, A2 and A5 repeat these readings through the real instrument once it is delivered. Chrome recipe (pending, post-catalogue): Battery, capsule 1 m from a Rate meter, Powered gate over a Receiver; Run; move the capsule to 3 m; disable a second Battery beside it.

## 6. Open questions

None open.

1. **Activity, band and rate law — decided** (D1, D16): 125.66 W PhotonMedium under the conventions.
2. **Constant strength against "spent inventory cannot emit indefinitely" — decided by the owner** (21 November 2026, and coordinator decision 21, 22 November 2026): constant within a Run, paid from a finite inventory of P₀ × 300 s = 37,699 J, restored on Reset. IX-16 holds as written.
3. **Static or dynamic — decided now**: static. Players control exposure by where they place it in Build and by shielding; the mobile source is [RAD-22](RAD-22-sealed-tracer-capsule.md).
4. **Reading close to the capsule — decided by the owner** (coordinator decision 21, 22 November 2026): flat at 0.900 W inside 1 m.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Battery, Powered gate (delivered; fixtures only). Rate meter RAD-08, shields EL-126 to EL-128, shutter RAD-06, collimator RAD-07, gauges EL-129 and EL-130, badge RAD-10, latch RAD-21 (undelivered). It has no port, so no port pair needs to agree.
- **Cross-element tasks.** Source in [sequence-task-413](../requirements.md#sequence-task-413) and [sequence-task-419](../requirements.md#sequence-task-419); photon control in [sequence-task-427](../requirements.md#sequence-task-427); processes [IX-15](../requirements.md#interaction-15) and [IX-16](../requirements.md#interaction-16) (steady inventory).
- **Campaign.** Chapter 11, first use 101, then 103, 110; reuse 111–120, 124–130, 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22); a level that places the capsule locked needs what S8 delivers.

## 8. Delivery

- **Slice shape.** Single element; the first radiation element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story).
- **Files.** `elements/GammaSourceCapsule/GammaSourceCapsule.csproj`, `GammaSourceCapsuleElementDeclaration.cs`, `gamma_source_capsule.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/GammaSourceCapsuleTests.cs`; a row in `VisibleSolidColliderTests`. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** No steady finite inventory that pays an emitter (then the source needs another payment row, an engine slice); a different unit scale, distance law or rule inside 1 m (re-derive section 5); no law-scene receiver row to read (then this slice waits for RAD-08 and the two are planned together).

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** A static body, two colliders, one source row and one steady inventory. No code path names the capsule.
2. **Generic laws.** The line-of-sight field law of the conventions and contact for the body. "It cannot be switched off" is the absence of a supply or signal node, not a rule; "it is paid" is the inventory debit every source obeys.
3. **Three layers.** Colliders: a cylinder and a box. Visuals: the art table; band and mark are printed flat. Animation: none.
4. **One-way flow and typing.** Inventory → source → segments → readings; nothing flows back to the source. The channel is an enum; power and energy are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** Static body; two primitives; one diverging source costs one shadow cast per receiver sample point in the scene (the D × K term of radiation convention 12), each through the spatial query's segment cast; nothing scans the scene.
7. **Tests are scenarios.** Rows A1 to A10 run the production simulation through placement, Run and the committed read.
