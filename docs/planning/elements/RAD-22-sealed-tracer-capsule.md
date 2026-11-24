# RAD-22 · Sealed tracer capsule — element readiness spec

**Current delivery (24 October 2026):** [RAD-22 delivery slice](../invest/named-elements.md#radiation-22). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 11, 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-22](../requirements.md#radiation-22) (P2 potential). Lengths are metres. A **marked mobile sealed toy source** tracked as a discrete body, not dissolved radioactive fluid. Units, sources, the propagation law and tick order are the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-22 · Sealed tracer capsule |
| Type | Radiation + Gravity (dynamic ball-compatible photon source) |
| Anchor | [requirements.md#radiation-22](../requirements.md#radiation-22); [named-elements.md#radiation-22](../invest/named-elements.md#radiation-22) |
| Owner | S633 |
| Disposition | To deliver as a single element. It is a ball that carries a source: not a duplicate of the Tennis ball (it emits, and has no settings) nor of the [Gamma capsule](RAD-01-gamma-source-capsule.md) (it moves). |
| Planned package | id `sealed_tracer_capsule`, directory `elements/SealedTracerCapsule/`, class `SealedTracerCapsuleElementDeclaration` |
| Related identities | Receivers [RAD-08](RAD-08-radiation-rate-meter.md); ball transport [CAT-048 Pipe](CAT-048-pipe.md), [CAT-054 Ramp](CAT-054-ramp.md), [CAT-019 Conveyor](CAT-019-conveyor.md); branching via [EL-163 Powered ball diverter](../invest/named-elements.md#element-163); counting via [CAT-020 Counter](CAT-020-counter.md); ball scale from [CAT-064 Tennis](CAT-064-tennis.md). No CAT spec refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL, ENGINE-RADIATION and RAD-08 |
| Run length needed | 30 s (the default); the longest row is a 5 s roll with a 15 s stall guard |

## 2. Declaration

Shape followed: the Tennis ball for a dynamic sphere that is a capture target and rides ball transport (`elements/TennisBall/TennisBallElementDeclaration.cs:10`–`:57`). Items marked **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IAmbientLoadElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:124`), `IAirflowReceiverElement` (`MechanicalContracts.cs:45`), plus the emitter interface **(field)** |
| Body | One dynamic body (`KinematicMode.Dynamic`, as `TennisBallElementDeclaration.cs:44`), `PayloadPolicy.CaptureTarget` (as `:45`). Mass 0.6 kg, constant. Inertia: solid sphere, 0.4 × m × r² = 0.4 × 0.6 × 0.33² = 0.0261 kg·m² on each axis (the Tennis ball's 0.4 factor, `:50`–`:51`), declared with a constant mass frame (the Domino's form, `elements/Domino/DominoElementDeclaration.cs:47`). Gravity (0, −9.81, 0) m/s² (as `:53`). |
| Material | Restitution 0.5, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. Friction, rolling resistance and bounce threshold are the Tennis ball's (`:32`); restitution is lower than its 0.85 so a capsule dropped beside a meter settles in the meter's zone and does not bounce out. |
| Collider (hidden `col_` proxy) | One sphere, radius 0.33 m. It declares no radiation row, so it takes the scene's default row: a ray through its 0.66 m diameter keeps e^(−0.15 × 0.66) = e^−0.099 = 0.906 of PhotonMedium. |
| Ambient load | Linear drag 0.04 /s and buoyancy 0, the Tennis ball's defaults (`:19`–`:20`, `:55`–`:57`), as constants. |
| Airflow receiver | Four samples at (0, ±0.5, 0) and (0, 0, ±0.5) in collider-normalised coordinates, weight 0.25 each, as the Tennis ball (`:27`–`:30`), so a Fan moves it like any ball. |
| Ports | None. |
| Source **(field)** | Frame body 0 (it moves with the ball); origin (0, 0, 0); isotropic; range 16 m; radiant power 62.83 W in PhotonMedium (4π × 5 W/sr); paid by a steady finite inventory (half-life none) of 62.83 W × 300 s = 18,850 J; internal occluder = its own sphere. |
| Parameters | None (D13). Size and mass are fixed so every lesson reads the same. |
| Stores | The steady inventory above (radiation convention 2): each tick debits 62.83 ÷ 120 = 0.524 J; it holds the 300 s maximum Run of ENGINE-RUN-LENGTH (the story of owner decisions 13 and 17; it only sizes this declared inventory and is not a prerequisite of this slice), so the strength is constant throughout any Run; Reset restores 18,850 J (owner decision of 21 November 2026; coordinator decision 21, 22 November 2026). |
| Presentation bindings | None; the art follows the body's pose. The selected-part Build preview is ENGINE-OPTICAL's shared one (radiation convention 13). |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 1, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domain Mechanical and the radiation flag **(field)** |
| Catalogue | Id `sealed_tracer_capsule`, "Tracer capsule", `ElementCategory.Radiation` **(field)**, palette order 725 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "A ball with a sealed source inside. Meters notice it as it rolls past, even through a pipe." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Shell` | sphere r 0.33 m, cream `#fff8e9` | solid (the sphere) | body pose |
| `Visual/Mark` | γ printed flat at each pole, navy `#293954` | decorative: printed flat | body pose |

The shell is one smooth sphere carrying only two printed marks, so it needs no second collider; it has no band, because the banded-ball rule admits a flat printed band only on the Pool ball (owner decision 11). Icon: a cream ball with a γ. It differs from the delivered balls by its cream shell and the γ at the poles.

### Compiled counts

Bodies 1 (dynamic). Colliders 1 (sphere), as the plain Tennis ball. Joints 0. Drives 0. Sensors 0. Airflow receivers 1 (four samples). Ambient loads 1. Signal nodes 0. Supply nodes 0. Sources 1. Inventories 1 (steady, 18,850 J). Presentation bindings 0.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport, RadioactiveDecay, FluidAdvection; the map's composition note already replaces FluidAdvection with rigid-body, contact and friction families, and under D14 the probe is the authority. 4 present, 0 untested, 1 narrower, 4 missing. ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES are stories of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | A dynamic sphere with declared mass, drag and gravity | present | `MassFrameDeclaration` (`elements/TennisBall/TennisBallElementDeclaration.cs:48`) | `TennisBallTests.IndependentMassAndRadiusCompileCorrectInertiaAndPersist` (`CuriousContraptions.tests/TennisBallTests.cs:29`) |
| R2 | A capture target taken by a delivered capture part, with Reset replay | present | `PayloadPolicy.CaptureTarget` (`TennisBallElementDeclaration.cs:45`) | `TennisInteractionTests.ReceiverCapturesOnlyTheAlignedTennisPayload` (`CuriousContraptions.tests/TennisInteractionTests.cs:11`) |
| R3 | Every visible solid has a collider | present | per-package row of the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:206`) |
| R4 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids, moving ones resampled | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) is for airflow rows only, gives no crossing lengths and loops over every collider (`:149`) | — | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast returning ordered crossings: **ENGINE-SPATIAL-QUERIES** |
| M1 | A declared emitter whose frame body is dynamic, emitting from this tick's final pose, and the shared segment and receiver solve | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | Radiation channels, a steady finite inventory that pays an emitter, attenuation along chords of hollow shapes (a Tube wall) with the scene's default row | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | `ElementCategory.Radiation` and the radiation `SimulationDomain` flag | **missing** | — | owner **ENGINE-RADIATION** |
| M4 | The selected-part Build preview that runs the same solve and commits nothing | **missing** | — | owner **ENGINE-OPTICAL** (optical convention 12; radiation convention 13) |

Capture does not stop emission and needs no capability: a capture is a residence latch over the payload's centre point (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:929`–`:976`), and collision participation is read from the compiled declaration (`:523`) and written nowhere at run time, so a captured capsule stays in place, colliding and emitting.

**Dependencies.** RAD-08 (two meters, undelivered, first); Pipe, Bumper, Counter, Powered gate, Battery, Pressure plate, Cannon, Receiver (delivered); EL-128 (undelivered; row A7); EL-163 diverter (undelivered; row A6 waits for it).

## 4. Sources and legacy

- [radiation-22](../requirements.md#radiation-22): "Two detectors observe the capsule in causal order and drive a gate/counter; a stationary capsule cannot repeatedly count as new arrivals. Branches move one capsule without copying it."
- [Named entry](../invest/named-elements.md#radiation-22), owner S633; map composition note (discrete sealed cargo, not dissolved tracer); binding `radiation-01.json`.
- Research row ("meters along opaque transport sequence a diverter") and slot 110 "The Traveling Beacon".
- **Legacy.** No tracer part. Hits are coverage snapshots only, e.g. `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L15446-L15449`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Fit with delivered parts.** Diameter 0.66 m is inside the Cannon's 0.6–0.84 m window (`elements/Cannon/CannonElementDeclaration.cs:32`) and the hopper's 0.62–0.78 m, and under the Pipe's bore, 1.3 m across (bore radius 0.65 m, `elements/Pipe/PipeElementDeclaration.cs:10`). Resting in the bore, its centre lies 0.65 − 0.33 = 0.32 m below the pipe axis. At 0.6 kg one capsule loads a Pressure plate set to its 0.5 kg default (`elements/PressurePlate/PressurePlateElementDeclaration.cs:39`).
- **Strength.** 62.83 W over 4π sr is 5 W/sr, half the Gamma capsule: a meter notices the tracer only when it is near, so two meters along a route have separate zones.
- **Head-on.** With the capsule on a meter's axis the reading is half the Gamma capsule's: 0.441 W at 1 m with the nine samples (0.882 ÷ 2), 0.450 W anywhere inside 1 m (0.900 ÷ 2, radiation convention 3), and by the axis formula 0.45 ÷ d² beyond 1 m: on within √(0.45 ÷ 0.2) = 1.50 m, off beyond √(0.45 ÷ 0.15) = 1.73 m.
- **Through a clear Pipe.** The shell is a Tube between bore radius 0.65 m and outer radius 0.70 m. Radiation crosses it with surface transmittance 1 and the default row (PhotonMedium 0.15 /m); its optical Transmit response does not act on radiation (radiation convention 4). A horizontal ray from the capsule's centre, 0.32 m below the axis, crosses the wall between x = √(0.65² − 0.32²) = 0.566 m and x = √(0.70² − 0.32²) = 0.623 m: a chord of 0.0568 m, so the wall passes e^(−0.15 × 0.0568) = e^−0.0085 = 0.9915.
- **Passing a meter beside the pipe.** Meter aperture centre level with the capsule's centre and 1 m from it horizontally at the nearest point, facing the pipe; capsule at distance s along the pipe from that point. Then d = √(1 + s²), facing = 1 ÷ d, the wall chord is 0.0568 × d and, by the axis formula, R ≈ 0.45 × e^(−0.0085 d) ÷ d³ (the nine-sample mean is 1 to 2 % lower). At s = 0: 0.441 × 0.9915 = 0.437 W. On mark 0.2 W at d = 1.306 (d³ = 2.226, e^−0.0111 = 0.989, 0.45 × 0.989 ÷ 2.226 = 0.200), s = √(1.306² − 1) = 0.84 m. Off mark 0.15 W at d = 1.436 (d³ = 2.963, e^−0.0122 = 0.988, 0.45 × 0.988 ÷ 2.963 = 0.150), s = √(1.436² − 1) = 1.03 m. A passing capsule turns a meter on 0.84 m before its nearest point and off 1.03 m after it: 1.87 m of pipe. Two meters 4 m apart never see it together: A turns off at s_A = 1.03 m and B turns on at s_A = 4 − 0.84 = 3.16 m, 2.13 m later.
- **Speed.** A Bumper at its default `strength` of 8 m/s (`elements/Bumper/BumperElementDeclaration.cs:36`) needs no supply for one kick: its store starts preloaded with ½ × 1 × 8² = 32 J (`:42`–`:45`), more than the ½ × 0.6 × 8² = 19.2 J the capsule takes. It sends the capsule through a zone in 1.87 ÷ 8 = 0.234 s, 28 ticks, moving 8 ÷ 120 = 0.067 m per tick. The solve reads this tick's final pose every tick, so a pass cannot fall between samples.
- **Hidden by a shield.** Two Dense slabs (EL-128, 0.05 m at 20 /m each) between pipe and meter at s = 0 leave 0.437 × e^−2 = 0.437 × 0.135 = 0.059 W, 70 % below the on mark: that meter does not see the pass. One slab leaves 0.437 × e^−1 = 0.161 W, 19.6 % below the mark, under the 20 % decision margin, so no row uses it.
- **One capsule.** The source is one row on one body. A branch moves the body; nothing copies the row.
- **After capture.** A Receiver's capture latches the event and leaves the capsule in place and colliding (section 3), so it goes on emitting from the box; a lesson that wants it hidden shields the Receiver.
- **Constant strength, paid.** 62.83 W from an 18,850 J inventory, debited 0.524 J each tick; after 10 s the debit is 628 J; Reset restores 18,850 J. The inventory holds the 300 s maximum Run, so the strength never falls within a Run (owner decision of 21 November 2026; coordinator decision 21, 22 November 2026). [IX-16](../requirements.md#interaction-16) holds as written.
- **Tick order.** The field solve of tick n runs after its last substep and emits from the capsule's final pose of tick n; readings commit with that tick and a meter's level follows in the signal network of tick n + 1 (radiation convention 11).

### Rows

Planned class `SealedTracerCapsuleTests`. Fixture: an 8 m clear Pipe sloping down at 5°; two Rate meters at the default threshold, A 2 m and B 6 m from the upper mouth along the pipe (4 m apart), each with its aperture centre level with the capsule's centre as it rolls past and 1 m from it horizontally, facing the pipe; each meter `PowerIn` from its own Battery's `Supply`; meter A's `ActivationOut` → a Counter's `ActivationIn`; meter B's `Supply` → a Powered gate's `PowerIn`. The capsule is released at rest 0.4 m inside the upper mouth, 1.6 m before A and outside its zone; it rolls at (5/7) × 9.81 × sin 5° = 0.61 m/s² and leaves the lower mouth after √(2 × 7.6 ÷ 0.61) = 5.0 s. Readings and energies ±3 %; positions 0.15 m either side of a calculated mark; stall guard three times the rolling time and at least 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given the capsule released in the fixture, when it rolls through, then meter A's level rises before meter B's, the Counter reads 1, the gate is supplied while the capsule is in B's zone, and the scene compiles one source (row: two detectors observe it in causal order and drive a gate and a counter) | `SealedTracerCapsuleTests.TwoMetersSeeThePassInOrder` | a Tennis ball through the same pipe fires neither | A's first-on tick before B's; exact count |
| A2 | Given the capsule resting on a level bench 1 m in front of meter A's face for 600 ticks, then the Counter reads 1 (row: a stationary capsule cannot repeatedly count as new arrivals) | `SealedTracerCapsuleTests.ARestingCapsuleCountsOnce` | rolled out of the zone and back gives 2 | exact count |
| A3 | Given the capsule resting on the bench 1 m in front of a meter's face, the reading is 0.441 W; resting inside a level Pipe beside the meter at s = 0, it is 0.437 W | `SealedTracerCapsuleTests.ThePipeWallBarelyDimsTheCapsule` | no pipe | ±3 % |
| A4 | Given the capsule resting inside a level Pipe at s = 0.69 m the level is on (0.45 × 0.990 ÷ 1.215³ = 0.248 W, 24 % above the mark); at s = 1.20 m it is off (0.45 × 0.987 ÷ 1.562³ = 0.117 W, 42 % below the mark) | `SealedTracerCapsuleTests.EachMeterHasItsOwnZone` | A3 | 0.15 m inside the 0.84 m on mark and 0.17 m beyond the 1.03 m off mark |
| A5 | Given the capsule launched into the upper mouth by a Bumper at its default 8 m/s `strength`, with a second Counter on meter B's `ActivationOut`, then each Counter reads exactly 1 | `SealedTracerCapsuleTests.AFastPassIsNotMissed` | A1 | exact count |
| A6 | Given an EL-163 powered ball diverter feeding two pipes, each with one meter and a Counter, and the capsule sent down one, then only that pipe's meter fires, the scene compiles one source and the two Counters sum to 1 (row: branches move one capsule without copying it) | `SealedTracerCapsuleTests.ABranchMovesOneCapsule` (queued until EL-163 is delivered; no delivered part forks a ball path) | the diverter switched: the other pipe's meter fires | exact counts |
| A7 | Given two Dense slabs between the pipe and meter A, then A does not fire and B does | `SealedTracerCapsuleTests.AShieldHidesThePassFromOneMeter` (EL-128 slabs, or law-scene boxes with the Dense row until EL-128 is delivered) | A1 | level states |
| A8 | Given the capsule dropped on a Pressure plate at its 0.5 kg default, the plate is loaded; placed in a Cannon it is accepted; dropped into a Receiver it is captured, and a meter 1 m from its resting point whose level was on before capture is still on 120 ticks after the Captured event | `SealedTracerCapsuleTests.TheCapsuleBehavesAsABallForDeliveredParts` | a 0.35 kg Tennis ball does not load the plate | event kinds and states |
| A9 | Given a Run of 10 s, then the inventory has been debited 628 J; when Reset, the capsule is at its authored pose, no reading or count remains, the inventory is 18,850 J and the next Run repeats A1; the declaration has no port and no parameter; a saved and reloaded construction behaves as A1 | `SealedTracerCapsuleTests.ResetAndReloadRestoreTheAuthoredCapsule`; authored state by R4 | A1 | pose within 0.01 m; energies ±3 % |

There is no parameter, so no parameter-refusal row. Chrome recipe (pending, post-catalogue): two Batteries, Pipe, two Rate meters beside it, a Counter and a Powered gate; drop the capsule in and Run.

## 6. Open questions

None open.

1. **Size, mass, activity and material — decided** (D1, D13): 0.33 m, 0.6 kg, 62.83 W, the Tennis ball's friction and drag with a lower bounce. It does not share the Tennis ball's settings: a fixed capsule keeps the meters' zones the same in every level.
2. **Rate meters or a dedicated passage sensor — decided** (D19): Rate meters. The requirement says the capsule is "sensed through its enclosure" by detectors; a second sensing part would duplicate RAD-08.
3. **Coverage families — resolved** (D14): the probe drops FluidAdvection and lists rigid-body capabilities; the map row is corrected with the plan-document updates (see the [batch summary](readiness/batch-o.md)).
4. **Marks — decided** (owner decision 11): no band; two γ marks printed flat at the poles.
5. **Payment — decided by the owner** (21 November 2026, and coordinator decision 21, 22 November 2026): constant strength from a finite inventory of 18,850 J, restored on Reset.
6. **Emission after capture — decided now**: it continues, because capture leaves the body in place and colliding; no part-specific rule hides a captured source.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Pipe, Pipe bends, Ramp, Conveyor, Bumper, Cannon, Pressure plate, Receiver, Counter, Powered gate, Battery, Fan (delivered); RAD-08, EL-128, EL-163 (undelivered). No port, so no port pair needs to agree; the meters' ports are RAD-08's.
- **Cross-element tasks.** None names it; processes [IX-15](../requirements.md#interaction-15), [IX-16](../requirements.md#interaction-16) (steady inventory; holds as written), [IX-01](../requirements.md#interaction-01) and [IX-02](../requirements.md#interaction-02).
- **Campaign.** Chapter 14, first use 135, then 136; reuse 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity needs S7 to widen it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES (the meter's threshold node and pulse); ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01; RAD-08; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story). Row A6 runs when EL-163 exists.
- **Files.** `elements/SealedTracerCapsule/SealedTracerCapsule.csproj`, `SealedTracerCapsuleElementDeclaration.cs`, `sealed_tracer_capsule.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/SealedTracerCapsuleTests.cs`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** An emitter that cannot sit on a dynamic body or does not emit from this tick's final pose; chords not computed for a Tube wall (then a pipe would read as solid or as empty and row A3 fails); the Pipe's wall or bore changed (re-derive the zone); the Tennis ball's material or drag changed by a repair slice (re-check the mirrored values); no steady finite inventory that pays an emitter (an engine slice first).

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** One dynamic body, one sphere, one ambient load, one airflow receiver, one source row and one steady inventory. No tracking code: "where is the capsule" is the body's pose.
2. **Generic laws.** Contact, friction, restitution, drag and gravity for the ball; the field law of the conventions and the inventory debit for the source. "Counts once" is a meter level that rises once.
3. **Three layers.** Colliders: one sphere. Visuals: shell and marks, following the body pose; marks printed flat. Animation: none.
4. **One-way flow and typing.** Physics pose → source → readings → next tick's signals; nothing flows back to the ball. The channel is an enum; values are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** One primitive; island sleeping does not exist today (ENGINE-ISLANDS-SLEEP, owner decision 3a), so a resting capsule is stepped every tick; once it exists, a sleeping capsule still emits from its committed pose. Tunnelling of the body is excluded by the engine's general means for fast spheres; detection cannot be skipped because a zone is 1.87 m and a tick's travel at 8 m/s is 0.067 m. One shadow cast per receiver sample point in the scene (the D × K term of radiation convention 12), each through the spatial query's segment cast.
7. **Tests are scenarios.** Rows A1 to A9 run the production simulation through placement, wiring, Run and committed reads.
