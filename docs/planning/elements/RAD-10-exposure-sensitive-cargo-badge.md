# RAD-10 · Exposure-sensitive cargo badge — element readiness spec

**Current delivery (24 October 2026):** [RAD-10 delivery slice](../invest/named-elements.md#radiation-10). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decision 12 and coordinator decisions 16 and 21, and on 23 November 2026 under coordinator decisions 26 and 30 (the one own-body rule, a sample per face read as their mean, accumulation per dt; section 5 re-derived). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-10](../requirements.md#radiation-10) (P1 potential). Lengths are metres. Fictional toy sources; never real-world dosimetry. Units, the receiver and accumulator laws, the one dose scale, the meaning of "exposed" and the tick order are the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-10 · Exposure-sensitive cargo badge |
| Type | Radiation (passive receiver on dynamic cargo, with a delivery goal) |
| Anchor | [requirements.md#radiation-10](../requirements.md#radiation-10); [named-elements.md#radiation-10](../invest/named-elements.md#radiation-10) |
| Owner | S617 |
| Disposition | To deliver as a single element: one parcel with its badge. It is not a duplicate of the [Dosimeter](RAD-09-integrating-dosimeter.md): it moves, has no port and feeds a goal, not a contact. |
| Planned package | id `badged_parcel`, directory `elements/BadgedParcel/`, class `BadgedParcelElementDeclaration` |
| Related identities | Supplied counterpart [RAD-09](RAD-09-integrating-dosimeter.md), on the same dose scale; sources [RAD-01](RAD-01-gamma-source-capsule.md); shields [EL-128](EL-128-dense-shield.md); transport [CAT-019 Conveyor](CAT-019-conveyor.md); dock [CAT-004 Basket](CAT-004-basket.md) / Receiver; goal family [EL-123 Protected-end-state goal](../invest/named-elements.md#element-123). No CAT spec refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL, ENGINE-RADIATION and the goal slices `engine-goal-evaluation` and `engine-goal-count-window-hold` (owner decision 12) |
| Run length needed | 30 s (the default); the longest row is A4 at 7 s plus the stall guard |

## 2. Declaration

Shape followed: the Domino for a dynamic box with a constant mass frame (`elements/Domino/DominoElementDeclaration.cs:47`–`:51`) and the Tennis ball for a capture target (`elements/TennisBall/TennisBallElementDeclaration.cs:45`). Items marked **(field)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), plus the receiver interface **(field)** |
| Body | One dynamic body, `PayloadPolicy.CaptureTarget`. Mass 2.0 kg, centre of mass at the origin. Principal moments of a uniform box with half-extents (0.25, 0.20, 0.25), by the Domino's rule (m ÷ 3)(a² + b²): (2 ÷ 3)(0.20² + 0.25²) = 0.0683, (2 ÷ 3)(0.25² + 0.25²) = 0.0833, 0.0683 kg·m². Gravity (0, −9.81, 0) m/s². |
| Material | Restitution 0.2, friction 0.5, rolling resistance 0, bounce threshold 0.1 m/s: a parcel that slides and settles, between the 1 kg Basketball and the 4 kg Bowling ball in mass. |
| Collider (hidden `col_` proxy) | One box 0.50 × 0.40 × 0.50 m at the origin; it declares no radiation row, so it takes the scene's default row. |
| Ports | None: a badge records without a socket. |
| Receiver **(field)** | Frame body 0; one **Uniform** receiver (radiation convention 7: facing 1 for every incident direction, the declared area an effective cross-section) of the standard area 0.09 m² (radiation convention 9). Its aperture rectangle, which only collimated segments use (credited when one crosses it from either side), is the 0.30 × 0.30 m square printed on the top face, centre (0, 0.20, 0), normal +Y. **Six samples, one at the centre of each face**, (±0.25, 0, 0), (0, ±0.20, 0) and (0, 0, ±0.25), weight 1/6 each, so the reading is their mean (coordinator decision 30, 23 November 2026: a receiver that must read the same whatever way it faces declares one aperture sample per face and reads their mean). Channel weights PhotonSoft, PhotonMedium, PhotonHard = 1, BetaMinus = 0.5, others 0. Own body by the one rule of optical convention 5 and radiation convention 4 (coordinator decision 26): each sample's shadow cast ends at that sample point, and the parcel's own box attenuates it like any body, so a sample on a face turned away from a source reads through the box's chord. No level. |
| Accumulator **(field)** | `dose`, joules: + the receiver's committed strength × dt each tick (dt the committed tick's duration, 1/120 s at the default cadence; radiation convention 9), never reduced within a Run; capacity 4.0 J; initial value 0. It commits one scalar with the tick, which a goal condition may name (section 5). |
| Goal use | None declared by the package. A level's goal names a capture of this parcel qualified by a band on its committed `dose` (section 5). |
| Parameters | None. The band is level data. |
| Presentation bindings | Four printed segments `Visual/Seg1` to `Visual/Seg4`, each albedo slate (85, 101, 115) → gold (247, 203, 82) when the accumulator's committed fraction of capacity reaches 0.25, 0.5, 0.75 and 1.0, that is 1, 2, 3 and 4 J **(field)**; twelve bindings (R, G, B each). |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 1, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domain Mechanical and the radiation flag **(field)** |
| Catalogue | Id `badged_parcel`, "Badged parcel", `ElementCategory.Radiation` **(field)**, palette order 710 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "A parcel whose badge adds up the radiation it meets on its way. Deliver it before the badge fills too far." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Parcel` | box 0.50 × 0.40 × 0.50 m, ochre `#e8b764` | solid (the box) | body pose |
| `Visual/Straps` | two navy `#293954` bands printed flat round the box | decorative: printed flat | body pose |
| `Visual/Badge` | square 0.30 m printed flat on the top face (the aperture), cream `#fff8e9` with a gold rim line | decorative: printed flat | body pose |
| `Visual/Seg1`–`Visual/Seg4` | four bars printed flat inside the badge, in a row | decorative: printed flat, recoloured by bindings | body pose (colour by bindings) |

Icon: a strapped box with a four-bar badge. It differs from any plain crate by the badge and its bars; the dose is read from how many bars are lit, a count and not a colour.

### Compiled counts

Bodies 1 (dynamic). Colliders 1 (box). Joints 0. Drives 0. Sensors 0. Signal nodes 0. Supply nodes 0. Receivers 1 (six samples, one per face). Stores 1 (accumulator). Presentation bindings 12.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport, ObjectiveEvaluation. Probe made on `8d1d68aa`: **4 present, 0 untested, 3 narrower, 4 missing**. ENGINE-SPATIAL-QUERIES is a story of owner decision 3a; the goal slices are those of owner decision 12.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | A dynamic body with a declared mass frame | present | `MassFrameDeclaration` (`elements/Domino/DominoElementDeclaration.cs:47`) | `WeightTests.TheWeightIsAMassBoundDynamicCylinderWithALoadTieAndNoPayloadRole` (`CuriousContraptions.tests/WeightTests.cs:39`) |
| R2 | A capture target taken by a capture part's residence sensor, with the `Captured` event and Reset replay | present | `ResidenceSensorDescriptor` with `TargetSelector.AllPayloads` (`elements/Receiver/ReceiverElementDeclaration.cs:12`–`:16`), event at `:64` | `TennisInteractionTests.ReceiverCapturesOnlyTheAlignedTennisPayload` (`CuriousContraptions.tests/TennisInteractionTests.cs:11`) |
| R3 | Every visible solid has a collider | present | per-package row of the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:206`) |
| R4 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids, moving ones resampled | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) is for airflow rows only and loops over every collider (`:149`) | — | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast: **ENGINE-SPATIAL-QUERIES** |
| N2 | A goal made of several conditions | narrower: one goal per level, of kind None, Captured, ActivatedAfter or IntegrityBroken (`engine/gpu/WorkshopPuzzle.cs:9`) | `WorkshopGoalKind` | `GoalSignalSelectorTests.AuthoredIntegrityGoalBindsTheDeclaredObservationAndCurrentSaveField` (`CuriousContraptions.tests/GoalSignalSelectorTests.cs:64`). Owner: goal slice **`engine-goal-evaluation`** |
| N3 | A binding driven by an accumulator's committed fraction | narrower: `PresentationDriver.CommittedStoredFraction` (`CuriousContraptions.Abstractions/ElementPresentation.cs:33`) reads only a mechanical energy store (`MechanicalPayload.EnergyStores`, `CuriousContraptions.Bundle/BundleValidator.cs:310`–`:312`), not an accumulator | `PresentationDriver` | `BothGateTests.ThreeNativeLampsFollowCommittedInputsAndSupplyThroughHideAndReset` (`CuriousContraptions.tests/BothGateTests.cs:259`) for the binding path. Owner of the accumulator driver: **ENGINE-RADIATION** |
| M1 | A declared receiver on a dynamic body, in the shared solve | **missing** | — | owner **ENGINE-OPTICAL** (dynamic receiver frames) |
| M2 | Radiation channels, weighted-sum strength, the Uniform facing rule, and an accumulator credited by a receiver and committed as a scalar | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | A scalar-band qualifier on a goal condition: "a condition may require a named committed scalar of its subject to lie in an authored closed band at the condition's occurrence; a failed qualifier leaves the condition unmet and reports the failed state" | **missing** | — | owner goal slice **`engine-goal-count-window-hold`** (owner decision 12). ENGINE-RADIATION only publishes the accumulator as a committed scalar. |
| M4 | `ElementCategory.Radiation` and the radiation `SimulationDomain` flag | **missing** | — | owner **ENGINE-RADIATION** |

**Dependencies.** A source (RAD-01, undelivered); a capture part and transport (Receiver, Ramp, Conveyor: delivered); shields (EL-128, undelivered; rows use law-scene Dense boxes until then). The Receiver admits the 0.50 m box: capture is a centre-point test in its local box (−0.66, −0.4, −0.66) to (0.66, 0.45, 0.66) with no size test (`elements/Receiver/ReceiverElementDeclaration.cs:12`–`:13`).

## 4. Sources and legacy

- [radiation-10](../requirements.md#radiation-10): "A shielded route delivers within budget; the same endpoint reached by an exposed route fails. Cargo retains its dose through stops and loses it only on Reset."
- [Named entry](../invest/named-elements.md#radiation-10), owner S617; map composition "bind dose integration to actual cargo BodyId/trajectory; cargo composition supplies RigidBodyDynamics/ContactImpulse, never an animated path"; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research row (slots 86 "The Sheltered Parcel", 87 "Briefly in the Open").
- **Legacy.** No badge, cargo-dose test or level. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1931-L1963`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions). Positions below are of the parcel's centre; each of the six samples is at its own distance dᵢ from the source.

- **Dose rate.** The mean of six Uniform face-centre samples on the standard area (convention 9; coordinator decision 30): strength = 0.09 × (1/6) Σᵢ Tᵢ·Eᵢ, with Eᵢ = I ÷ dᵢ² beyond 1 m and I ÷ 1 m² inside 1 m (convention 3), and Tᵢ = e^(−μ × chordᵢ) for the sample's chord through the parcel's own box on the default row (PhotonMedium μ = 0.15 /m), 1 for a sample on a face turned toward the source. For the Gamma capsule (10 W/sr, PhotonMedium):
  - *Centre 0.70 m away, a 0.40 × 0.50 m face toward it* (every sample within 0.70 + 0.25 = 0.95 m, so every Eᵢ is flat at 10 W/m²): near sample T 1; far sample chord 0.50 m, e^(−0.075) = 0.9277; top and bottom samples chord 0.260 m each, 0.9618; the other two sides chord 0.265 m each, 0.9610. Mean 5.7733 ÷ 6 = 0.9622; strength 0.900 × 0.9622 = **0.866 W**.
  - *Centre 0.70 m away, the 0.50 × 0.50 m top or bottom face toward it*: near 1; far chord 0.40 m, 0.9418; four sides chord 0.212 m, 0.9686. Mean 5.8162 ÷ 6 = 0.9694; strength **0.872 W**.
  - *Centre 2 m away, a 0.40 × 0.50 m face toward it*: samples at 1.75, 2.25, 2.010 (two) and 2.016 m (two) with T 1, 0.9277, 0.9630 and 0.9629: (1/6)(1 ÷ 3.0625 + 0.9277 ÷ 5.0625 + 2 × 0.9630 ÷ 4.04 + 2 × 0.9629 ÷ 4.0625) × 10 = 1.4606 ÷ 6 × 10 = 2.434 W/m²; strength **0.219 W**.
- **How far the reading depends on orientation** (coordinator decision 30 applied, with no second own-body rule). For a distant source the reading is the bare-sample value times the mean of the six Tᵢ. PhotonMedium: 0.963 when the source is square to a 0.40 × 0.50 m face (far chord 0.50 m, the four side samples grazed at 0.25 m), 0.973 on a body diagonal (three faces lit; hidden chords 0.433, 0.346 and 0.346 m), up to 0.990 when it is square to the top with the side samples grazed clear: under 3 % across every orientation. PhotonSoft (μ 0.4): 0.906 to 0.975, about 7 %. PhotonHard (μ 0.075): 0.982 to 0.995. BetaMinus reaches the badge only as a collimated ray (convention 3 makes every Alpha and BetaMinus source collimated, so the six diverging-source samples never read beta), credited where it crosses the top-face aperture; a ray that meets the box anywhere else first is absorbed in it (e^(−150 × 0.20) ≈ 10⁻¹³). So a beta beam is read only through the badge face, and the BetaMinus reading is not orientation-invariant; face samples cannot change that, because a collimated ray is credited at the aperture, not at samples. **This is a stated limit, not a defect against the requirement rows.** [radiation-10](../requirements.md#radiation-10) asks that a shielded route deliver within budget, that an exposed route to the same endpoint fail, and that the dose persist until Reset; it names no channel and no orientation, and every row here uses the PhotonMedium capsule, which reads within 3 % in every orientation (decision 30's purpose). A beam that strikes a side and gives no dose is the parcel's own 0.40 to 0.50 m box stopping beta under the one own-body rule (convention 4), not a second rule; the badge is printed on the top face, so a player sees which face counts. Lessons 111 and 112 use the photon capsule (the BetaMinus cartridge, RAD-04, is first taught at 116); a later level that pairs the parcel with a beta beam teaches top-face exposure as it stands.
- **Dose.** Each tick adds strength × dt joules (dt the committed tick's duration, 1/120 s by default). Three seconds with the centre 0.70 m from the capsule: 0.866 × 3 = 2.60 J. The same three seconds behind two Dense slabs (PhotonMedium 20 /m × 0.05 m each, e^−2 = 0.135): at most 0.866 × 0.135 × 3 = 0.352 J.
- **A route past the capsule.** The parcel travels along its local X axis on a straight path level with the capsule, its centre 1 m from the capsule at the nearest point, from 3 m before that point to 3 m after it, at speed v. At path coordinate s the strength is 0.900 × f(s) ÷ (1 + s²) W, f being the six-sample mean relative to a bare point at the centre, worked sample by sample as above: f = 0.876 at s = 0 (the near sample is 0.75 m away and reads flat), 0.900 at 0.25, 0.947 at 0.5, 0.981 at 1, 0.978 at 1.5, 0.975 at 2, 0.974 at 2.5 and 0.973 at 3. Simpson's rule (h = 0.25 on 0 to 0.5, h = 0.5 on 0.5 to 2.5, trapezium on 2.5 to 3) gives ∫₀³ f ÷ (1 + s²) ds = 0.418 + 0.706 + 0.058 = 1.183, against atan 3 = 1.249 for a bare point; so dose = 0.900 × 2 × 1.183 ÷ v = 2.13 ÷ v joules: 2.13 J at 1 m/s, 1.06 J at 2 m/s. With two Dense slabs between the capsule and the whole path the dose is at most 2.13 × 0.135 = 0.288 J (oblique rays cross more slab and receive less). The Receiver at the path's end is about 3.2 m from the capsule (0.088 W), so its default dwell (`capture_dwell` 0.35 s, `elements/Receiver/ReceiverElementDeclaration.cs:15`, `:46`) adds 0.088 × 0.35 = 0.03 J, inside the tolerance.
- **Lesson band.** A level authors the band; the taught default is 0 to 1.6 J. The exposed route at 1 m/s (2.13 J, 33 % above 1.6) fails; the shielded route (at most 0.288 J, 82 % below) passes. The same exposed route at 2 m/s gives 1.06 J, 33 % below the limit, and passes: lesson 112, "Briefly in the Open".
- **Stops.** The accumulator never falls. A parcel stopped where its strength is 0 keeps its dose unchanged; stopped in the field it keeps adding. Only Reset returns it to 0.
- **Goal.** The level's goal is a capture of this parcel by a named capture part, qualified by its committed `dose` lying in the band at the capture's occurrence (M3, slice `engine-goal-count-window-hold`). A capture outside the band leaves the condition unmet, so the level stays unsolved and the HUD (slice `engine-goal-progress-observation`) shows the condition unmet with its failed state. "At most" is a band from 0; "within" has a lower bound above 0.
- **Display.** The capacity of 4.0 J is a display scale: bars light at 1, 2, 3 and 4 J. The default band's limit, 1.6 J, sits between the first and second bar.
- **Tick order.** The field solve of tick n runs after the last substep on tick n's final pose, credits the accumulator and commits it with tick n. The capture observation of tick n commits with the same tick from the same final pose, so the qualifier reads the accumulator committed with the capture: capture and accumulator commit on one pose (radiation convention 11).
- **Energy.** The badge receives at most the power that crosses its effective cross-section; the accumulator is a measurement and supplies nothing.

### Rows

Planned class `BadgedParcelTests`. Fixture: a Gamma capsule, a Conveyor carrying the parcel along its local X axis on a straight path level with the capsule, its centre 1 m from the capsule at the nearest point, into a Receiver 3 m beyond that point, and a level goal of capture with the band 0 to 1.6 J. Doses ±3 % plus one tick of exposure (at most 0.0075 J at the default tick); the nearest decision margin is 33 %. Stall guard three times the travel time and at least 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given two law-scene Dense boxes between the capsule and the path, when the parcel is carried at 1 m/s and captured, then its dose is at most 0.288 J and the goal is solved (row: a shielded route delivers within budget) | `BadgedParcelTests.AShieldedRouteDeliversWithinTheBand` | A2 | dose at most 0.288 J + 3 %; solved |
| A2 | Given the same route with no boxes, then the parcel is captured by the same Receiver with a dose of 2.13 J and the goal is not solved (row: the same endpoint reached by an exposed route fails) | `BadgedParcelTests.AnExposedRouteToTheSameEndpointFails` | A1 | ±3 %; `Captured` occurs, goal unsolved for the stall guard |
| A3 | Given the parcel held with its centre 0.70 m from the capsule and a 0.40 × 0.50 m face toward it for 3 s, then its dose is 0.866 × 3 = 2.60 J; with its centre 2 m away, 0.219 × 3 = 0.657 J | `BadgedParcelTests.DoseIsRateTimesTime` | no capsule gives 0 | ±3 % |
| A4 | Given the parcel at rest with its centre 0.70 m from a law-scene 10 W/sr PhotonMedium emitter and a 0.40 × 0.50 m face toward it, the emitter on for 1 s, off for 5 s and on for 1 s, then the dose after the pause equals the dose before it (0.866 J) and the total is 1.732 J (row: cargo retains its dose through stops) | `BadgedParcelTests.DoseIsKeptThroughAStop` | one 2 s burst gives the same total | ±3 % |
| A5 | Given the capsule 0.70 m from the parcel's centre at the same height, with the parcel upright, upside down, on its side and with the badge facing away, then the dose rate is 0.866 W with a 0.40 × 0.50 m face toward the capsule and 0.872 W with the top or bottom face toward it (every face-centre sample within 0.70 + 0.25 = 0.95 m, inside the flat 1 m; the far samples read through the box's own chords, coordinator decision 30) | `BadgedParcelTests.TheBadgeReadsInEveryOrientation` | A3 | ±3 % of the pose's value; the poses agree within 1 % |
| A6 | Given the exposed route at 2 m/s, then the dose is 1.06 J and the goal is solved; with a band of 0.5 to 1.6 J the shielded route (at most 0.288 J, 42 % below 0.5) is not solved | `BadgedParcelTests.TheBandHasBothEnds` | A1, A2 | ±3 % |
| A7 | Given a level goal whose band has a negative, non-finite or inverted bound, or names a scalar its subject does not declare, then the content is refused naming the puzzle and field | `BadgedParcelTests.AnInvalidDoseBandIsRefused` | a valid band loads | typed refusal |
| A8 | Given a dose of 2.4 J, then two bars are lit; at 0.8 J none | `BadgedParcelTests.TheBarsFollowTheCommittedDose` | A3 | bar count; each value 20 % from the nearest bar |
| A9 | Given a Run with a dose above 0, when Reset, then the dose is 0, the bars are unlit and the parcel is at its authored pose; a saved and reloaded construction starts at 0 (row: it loses its dose only on Reset) | `BadgedParcelTests.OnlyResetClearsTheDose`; authored state by R4 | A3 | dose 0; pose within 0.01 m |

The declaration has no port and no parameter, so there is no connection or parameter-refusal row; the goal content refusal is A7. Chrome recipe (pending, post-catalogue): capsule, Conveyor, parcel, Receiver and two Dense shields; Run with and without the shields.

## 6. Open questions

None open.

1. **A separate attachable badge or this dedicated parcel — decided** (D15): the row says "a dedicated movable cargo piece"; one package, one body.
2. **Size, mass and band — decided** (D1, D16; coordinator decision 21 for the one dose scale): 0.50 × 0.40 × 0.50 m, 2.0 kg, the standard 0.09 m² area, taught band 0 to 1.6 J.
3. **Evaluated at dock entry or at level end — decided now**: at the capture's occurrence. The goal model is built from first-occurrence observations (`_bmad-output/implementation-artifacts/spec-engine-goal-evaluation.md`), and "arrival inside a taught exposure window" names the arrival.
4. **Facing rule — decided** (coordinator decisions 21 and 30): Uniform, radiation convention 7, with one sample at each face centre read as their mean. The own-body rule is the one rule of optical convention 5; the box's own chords cost a sample on a turned-away face 3 to 7 % of its PhotonMedium term, so the six-sample mean varies by under 3 % with orientation (section 5), and no second own-body rule is written. BetaMinus, whose sources are all collimated, is read only through the top-face aperture: a limit stated in section 5, which leaves every requirement row met because none names a channel or an orientation (checked 23 November 2026).

Needs the owner: none.

## 7. Integration

- **Collaborators.** Receiver, Conveyor, Ramp (delivered); RAD-01, RAD-02, EL-128, RAD-06 (undelivered). No port, so no port pair needs to agree.
- **Goal slices.** Owner decision 12 has five: `engine-goal-evaluation`, `engine-goal-supplied-occurrence`, `engine-goal-progress-observation`, `engine-goal-ordered-events` and `engine-goal-count-window-hold`. This part uses `engine-goal-evaluation` (several conditions, the `Captured` kind), `engine-goal-count-window-hold` (the scalar-band qualifier, M3, one field of the one condition model) and `engine-goal-progress-observation` (the unmet condition on the HUD).
- **Cross-element tasks.** None names it; processes [IX-15](../requirements.md#interaction-15), [IX-01](../requirements.md#interaction-01), [IX-02](../requirements.md#interaction-02).
- **Campaign.** Chapter 12, first use 111, then 112; reuse 121–125, 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity needs S7 to widen it (owner decision 15).
- **Lesson fixtures.** The parcel is usually level-placed and locked, so such a level needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL; ENGINE-RADIATION as amended (accumulator, Uniform facing); the goal slices `engine-goal-evaluation` and `engine-goal-count-window-hold` (the qualifier), in the slice order of owner decision 12; RAD-01; S7 for the binding. `engine-goal-progress-observation` is needed for the HUD and not for the rows. No dependency on ENGINE-THERMAL, as the [ENGINE-RADIATION story](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure) states.
- **Files.** `elements/BadgedParcel/BadgedParcel.csproj`, `BadgedParcelElementDeclaration.cs`, `badged_parcel.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/BadgedParcelTests.cs`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories and the two goal slices; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** No Uniform facing rule; a goal condition model that cannot carry a scalar-band qualifier; a different dose scale (re-derive section 5). In each case the slice stops and the owning story is amended; no second design is kept here.

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** One dynamic body, one box, one receiver, one accumulator and twelve bindings. The goal is level data on the engine's one condition model; no code path names the parcel or the badge.
2. **Generic laws.** Contact and friction for the body; the receiver and accumulator laws of the conventions; a goal qualifier over a named committed scalar. "The exposed route fails" is a capture observation whose qualifier is not met.
3. **Three layers.** Colliders: one box. Visuals: parcel, straps, badge and bars, all following the body pose and printed flat. Animation: bar colours from the committed accumulator; they write nothing back, and the goal never reads them.
4. **One-way flow and typing.** Physics pose → field solve → accumulator → goal evaluation and presentation. Channels and goal kinds are enums; doses are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** One primitive; six sample points, so six shadow casts per tick for each diverging segment whose cone query returns this receiver (radiation convention 12), each through the spatial query's segment cast. Tunnelling is excluded by the engine's general means; a 0.5 m box at conveyor speed moves under 0.01 m per tick.
7. **Tests are scenarios.** Rows A1 to A9 run the production simulation through placement, Run, capture and the committed accumulator and goal reads.
