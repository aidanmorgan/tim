# RAD-06 · Powered radiation shutter — element readiness spec

**Current delivery (24 October 2026):** [RAD-06 delivery slice](../invest/named-elements.md#radiation-06). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 6, 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-06](../requirements.md#radiation-06) (P1 potential). Lengths are metres. Radiation crosses the window along local X; the blade slides along local +Y. Units, laws, the default row and the Dense row are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions); all values are game fiction.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-06 · Powered radiation shutter |
| Type | Radiation (supplied actuator moving a shield) |
| Anchor | [requirements.md#radiation-06](../requirements.md#radiation-06); [named-elements.md#radiation-06](../invest/named-elements.md#radiation-06) |
| Owner | S613 |
| Disposition | To deliver as a single element. It follows the Powered gate's declaration shape but is not a duplicate of it or of the Beam shutter ([CAT-007](CAT-007-beam_shutter.md)): its blade has its own size, mass and Dense radiation row, and its frame is an open window, not a tube sleeve. |
| Planned package | id `radiation_shutter`, directory `elements/RadiationShutter/`, class `RadiationShutterElementDeclaration` |
| Related identities | Blade row shared with [EL-128](EL-128-dense-shield.md); sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); observers [RAD-08](RAD-08-radiation-rate-meter.md), [RAD-09](RAD-09-integrating-dosimeter.md) |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), which includes ENGINE-JOINT-KINDS for the spring's rest position, and on ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); no row is longer than 2 s plus the stall guard |

## 2. Declaration

Shape followed: the Powered gate (`elements/PoweredGate/PoweredGateElementDeclaration.cs:14`): a static frame, one dynamic blade on a prismatic slider with a return spring (`:76`–`:86`), and a supply consumer whose target is the slider's drive (`:88`–`:90`). Supplied, the drive opens the blade; unsupplied, the drive is free (`PrismaticInactiveResponse.Free`, the default, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:85`) and the spring closes it at physical speed. Items marked **(field)** or **(rest)** use contracts that do not exist yet.

| Item | Value |
| --- | --- |
| Interfaces | `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) |
| Body 0, frame | Static; restitution 0.1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s (the gate's values, `PoweredGateElementDeclaration.cs:47`) |
| Frame colliders (hidden `col_` proxies) | Sill box 0.20 × 0.10 × 0.80 m at (0, −0.35, 0). Two jamb boxes 0.20 × 1.30 × 0.10 m at (0, 0.35, ±0.35). Header box 0.20 × 0.10 × 0.80 m at (0, 1.05, 0). One socket sphere radius 0.075 m at the port. The window between sill and jambs that the closed blade fills is 0.60 × 0.60 m, centred on the origin. No radiation row: the scene's default row. The boxes touch and do not overlap; the socket sphere's centre lies on the header's +Z face, so half of it is inside the header, and that overlapped chord counts once (radiation convention 4). |
| Body 1, blade | Dynamic box 0.10 × 0.60 × 0.60 m, 4.5 kg, constant mass frame, principal moments (0.270, 0.139, 0.139) kg·m² (m(b² + c²) ÷ 12), same contact material, gravity −9.81 m/s². **Dense** radiation row **(field)**. Closed, it fills the window. |
| Slider | Along frame +Y; lower limit 0 (closed), upper limit 0.70 m (open: the blade's lower edge is at y = +0.40, 0.10 m above the window); stiffness 120 N/m; damping 20 N·s/m; spring rest position −0.50 m, below the lower limit, so the closed blade is preloaded against it **(rest, ENGINE-JOINT-KINDS)**; connected bodies do not collide |
| Drive | Target 0.70 m, maximum speed 0.5 m/s, stopping acceleration 5 m/s², maximum effort 240 N, maximum power 120 W |
| Ports | `PortId.PowerIn`, Electrical, Input, role `PowerIn`, at (0, 1.05, 0.40), normal +Z |
| Supply graph | One `SupplyConsumerDescriptor` `consumer` with a `SupplyDriveTarget` on the prismatic coordinate; `PowerIn` → (`consumer`, `SupplyTerminal.PowerIn`). `ElectricalNodes`: one Consumer row, 12 V, maximum power 120 W |
| Parameters | None. The old travel-speed setting is dropped (D13, D19: no row or lesson needs it). |
| Stores | None |
| Presentation bindings | `Visual/Blade` follows body 1's pose. `Visual/Indicator` albedo from `PresentationDriver.CommittedActuatorState`, the gate's three bindings (`PoweredGateElementDeclaration.cs:18`–`:20`) |
| Footprint | `ElementFootprint(Bodies: 2, MovingBodies: 1, PrismaticConstraints: 1, RevoluteConstraints: 0)`; domains Mechanical, Electrical and the radiation flag **(field)** |
| Catalogue | Id `radiation_shutter`, "Radiation shutter", `ElementCategory.Radiation` **(field)**, palette order 706 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Electricity lifts the dense blade out of the window. Without power a spring closes it." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Sill`, `Visual/Header` | boxes 0.20 × 0.10 × 0.80 m, cream `#fff8e9` | solid (sill and header boxes) | nothing |
| `Visual/JambLeft`, `Visual/JambRight` | boxes 0.20 × 1.30 × 0.10 m, cream, with a printed position scale | solid (jamb boxes) | nothing |
| `Visual/Blade` | box 0.10 × 0.60 × 0.60 m, navy `#293954`, three grooves printed flat | solid (blade box) | body 1's pose |
| `Visual/Indicator` | disc r 0.03 m flush in the header | decorative: flush, recoloured by a binding | nothing (colour only) |
| `Visual/PowerStud` | sphere r 0.075 m, gold `#f7cb52` | solid (socket sphere) | nothing |

No actuator rod is drawn: a proud rod would need a collider and a second moving body. Icon: a window frame with a half-raised grooved blade. It differs from the Powered gate by the square open frame (the gate is a round sleeve) and from the Dense shield by the frame around the grooved slab.

### Compiled counts

Bodies 2 (1 static, 1 dynamic). Colliders 6 (5 boxes, 1 sphere), fewer than the Powered gate's ring of 16. Joints 1 prismatic. Drives 1. Sensors 0. Supply nodes 1. Stores 0. Presentation bindings 3 and one body-pose node.

## 3. Engine capabilities

Map row families (navigation, D14): ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, IonizingTransport, JointConstraint, RigidBodyDynamics. This probe, made on `8d1d68aa`, is the authority until the slice re-runs it: **5 present, 0 untested, 2 narrower, 4 missing**. ENGINE-SPATIAL-QUERIES, ENGINE-JOINT-KINDS and ENGINE-SUPPLY-DEMAND are stories of owner decision 3a.

| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Static scene body with colliders from `col_` nodes | present | `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) | `StateLatchTests.Compiler_DeclaresStaticLatchBodyWithCanonicalColliders` (`CuriousContraptions.tests/StateLatchTests.cs:503`) |
| R2 | Every visible solid has a collider | present | per-package row | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:186`) |
| R3 | Dynamic blade on a prismatic slider with a spring and a supplied drive | present | `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`) | `PoweredGateTests.PhysicsCompiler_CompilesGateWithDynamicBladeAndPrismaticJoint` (`CuriousContraptions.tests/PoweredGateTests.cs:151`) |
| R4 | Typed ports and wrong-kind refusal | present | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| R5 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| N1 | Line of sight through solids at this tick's final poses of moving bodies | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) serves airflow rows only and loops over every collider (`:149`) | — | `AirflowGeneralityTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast returning ordered crossings: **ENGINE-SPATIAL-QUERIES** |
| N2 | A spring on a prismatic or revolute coordinate declares a rest position that may lie outside the travel limits, so the coordinate is preloaded against a limit | narrower: the slider spring's rest is the coordinate where the two frames coincide (`MechanicalContractValues.cs:100`–`:103`); `Compression` (`:111`) sets only the starting coordinate (−Compression, `CuriousContraptions.Bundle/MechanicalValueDomain.cs:80`); a rest outside the limits (`lower > 0` or `upper < 0`) and a start outside them are refused (`MechanicalValueDomain.cs:81`) | `PrismaticSliderDescriptor` | the R3 test. Owner: **ENGINE-JOINT-KINDS** (owner decision 6 and coordinator decision 21) |
| M1 | A drive's committed work debits its source | **missing** (`docs/planning/elements/readiness/engine-design-gap.md:138`) | — | owner **ENGINE-SUPPLY-DEMAND** |
| M2 | The shared emitter, segment and receiver solve | **missing** | — | owner **ENGINE-OPTICAL** |
| M3 | Radiation channels and per-collider rows crossed by chord, on a dynamic body's collider | **missing** | — | owner **ENGINE-RADIATION** |
| M4 | `ElementCategory.Radiation` and the radiation domain flag | **missing** (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12`) | — | owner **ENGINE-RADIATION** |

Nothing radiation-specific exists in `CuriousContraptions.Abstractions*`, `CuriousContraptions.Simulation`, `CuriousContraptions.Bundle`, `engine/` or `elements/`. ENGINE-RADIATION ([Story ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure), sprint key `engine-radiation`) is the one owner of the radiation-specific rows.

**Dependencies.** Battery (delivered); a source and a meter (RAD-01, RAD-08, undelivered); the Tennis ball (delivered) for the obstruction row.

## 4. Sources and legacy

- [radiation-06](../requirements.md#radiation-06): "Closed blade reduces exposure below the taught target threshold; an obstructed blade leaks according to geometry. Power loss cannot teleport the blade closed."
- [Named entry](../invest/named-elements.md#radiation-06), owner S613; map row (S605, S257); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research row (blade pose ← committed slider; "a jammed blade stays open"; slots 80 "Closing Time", 85).
- **Legacy.** No radiation shutter part, test or level. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1804-L1838`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Spring.** Closing force 120 × (p + 0.50) N at travel p: 60 N closed, 144 N open. The blade weighs 4.5 × 9.81 = 44.1 N. Damping ratio 20 ÷ (2 × √(120 × 4.5)) = 20 ÷ 46.5 = 0.43.
- **Mounting (owner decision 6).** Upright, gravity and spring both close: 60 + 44.1 = 104.1 N at closed. On its side (rolled 90° about local X, so the blade slides horizontally), the spring alone: 60 N closed to 144 N open. Inverted, gravity opens with 44.1 N and the spring closes with at least 60 N: 60 − 44.1 = 15.9 N net at closed, 144 − 44.1 = 99.9 N at open. With the rest at the closed position instead, an inverted blade would hang open at 44.1 ÷ 120 = 0.37 m.
- **Drive.** Holding open upright needs 144 + 44.1 = 188.1 N; moving at 0.5 m/s near the top it needs 188.1 + 20 × 0.5 = 198.1 N, under the 240 N limit, and 198.1 × 0.5 = 99 W, under 120 W, the delivered Battery's default limit. Opening takes at least 0.70 ÷ 0.5 = 1.4 s. Mechanical work of one upright opening against the spring and gravity: spring 120 × (0.70² ÷ 2 + 0.50 × 0.70) = 120 × 0.595 = 71.4 J plus lift 44.1 × 0.70 = 30.9 J, 102.3 J; the damper takes about 20 × 0.5 × 0.70 = 7.0 J more at full speed.
- **Power loss.** The drive goes free and the spring closes the blade. The largest closing force is 188.1 N, so acceleration is at most 188.1 ÷ 4.5 = 41.8 m/s² and closing takes at least √(2 × 0.70 ÷ 41.8) = 0.18 s (22 ticks). The blade is never closed in the tick the supply is lost.
- **Closed.** A standard meter 1 m from the Gamma capsule, the blade between: 0.882 × e^(−20 × 0.10) = 0.882 × 0.135 = 0.119 W, 21 % under the 0.15 W release mark.
- **Partial travel.** With the blade 0.15 m in front of the meter face, the meter's three sample rows (at 0 and ±0.10 m) cross the blade plane at 0 and ±0.085 m. The blade's lower edge is at y = −0.30 + p. At p = 0.26 the edge is at −0.04 and the bottom row passes under it: 0.882 × (3 + 6 × 0.135) ÷ 9 = 0.882 × 0.423 = 0.373 W (on). At p = 0.34 the edge is at +0.04 and two rows pass: 0.882 × (6 + 3 × 0.135) ÷ 9 = 0.882 × 0.712 = 0.628 W.
- **Obstruction.** A Tennis ball with `radius` 0.12 m on the sill under the blade stops it at p = 0.24 (edge at −0.06). The bottom row passes; its centre ray passes 0.095 m from the ball's centre and crosses a chord of 2 × √(0.12² − 0.095²) = 0.147 m of the ball, which takes the default row: e^(−0.15 × 0.147) = e^−0.022 = 0.978. The two outer rays of that row pass √(0.095² + 0.085²) = 0.127 m from the centre and miss it. Reading 0.882 × (2 + 0.978 + 6 × 0.135) ÷ 9 = 0.882 × 0.421 = 0.371 W: the shutter leaks through the gap it actually leaves.
- **Thickness.** A path crossing the closed blade at 45° has a chord of 0.10 ÷ cos 45° = 0.141 m: e^(−20 × 0.141) = e^−2.83 = 0.059 against 0.135 head on, a factor e^−0.83 = 0.44. The meter facing the capsule along that path reads 0.882 × 0.059 = 0.052 W.
- **Tick order.** The field solve of tick n runs after its last substep, on this tick's final blade pose, and the reading commits with tick n (radiation convention 11).

### Rows

Planned class `RadiationShutterTests`. Fixture: Gamma capsule, shutter, standard meter on one axis, capsule to meter face 1 m, blade 0.15 m before the face; Battery `Supply` → shutter `PowerIn`. Readings ±3 %; positions 0.01 m; stall guard 5 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| S1 | Given no supply, then the blade rests closed, the reading is 0.119 W and the meter's level is off (row: closed blade reduces exposure below the threshold) | `RadiationShutterTests.AClosedBladeQuietsTheMeter` | S2 | ±3 %; 21 % under 0.15 W |
| S2 | Given supply, then the blade reaches 0.70 m in no less than 1.4 s and the reading is 0.882 W | `RadiationShutterTests.SupplyOpensTheWindow` | S1 | ±3 % |
| S3 | Given a fixture declaration with drive target 0.26 m, then the reading is 0.373 W; with 0.34 m, 0.628 W (row: radiation follows actual blade position) | `RadiationShutterTests.PartialTravelLeaksByGeometry` (theory) | S1 | ±3 % |
| S4 | Given a Tennis ball of radius 0.12 m on the sill, when supply is removed, then the blade stops on the ball, the reading is 0.371 W and the level stays on (row: an obstructed blade leaks) | `RadiationShutterTests.AnObstructedBladeLeaksThroughItsGap` | S5 | blade travel 0.24 ± 0.02 m; reading ±3 % |
| S5 | Given an open blade, when the Battery is disabled, then the blade is not closed in that tick, its travel lies strictly between 0.6 and 0.1 m on at least five ticks, it closes no sooner than 0.18 s and within 2 s, and the reading ends at 0.119 W (row: power loss cannot teleport the blade) | `RadiationShutterTests.PowerLossClosesAtPhysicalSpeed` | S2 | time lower bound 0.18 s; reading ±3 % |
| S6 | Given the shutter upright, on its side (rolled 90° about local X) and inverted, then supplied it opens fully and unsupplied it closes and stays closed for 2 s (owner decision 6) | `RadiationShutterTests.TheBladeOpensAndClosesInEveryMounting` (theory, three mountings) | a fixture declaration with the rest at 0: the inverted blade hangs open at 0.37 m | travel 0 ± 0.01 m when closed; inverted closing force 15.9 N net |
| S7 | Given the closed shutter at the origin with its blade normal along X, the capsule at (−0.354, 0, −0.354) and the meter's aperture centre at (0.354, 0, 0.354) facing the capsule (1 m apart along a line 45° off the blade normal, the blade midway; each sample ray's chord depends only on its angle, so the blade's place on the path does not change the reading; the meter's base, turned 45°, has its nearest corner at x = 0.107, clear of the frame's x ≤ 0.10), then the reading is 0.052 W, against 0.119 W head on (row: actual thickness) | `RadiationShutterTests.AnObliquePathCrossesMoreBlade` | S1 | ±3 % |
| S8 | Given one upright opening, then in every tick the Battery's debit is at least the drive's committed work, and the committed work of the opening is at least 102 J | `RadiationShutterTests.OpeningIsPaidFromSupply` | unsupplied: no debit | two inequalities, no tolerance: the work against spring and gravity is 102.3 J and the damper adds about 7 J above it |
| S9 | Given an electrical output wired to another output, or a signal output wired to `PowerIn`, then the edit is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (R4) | — | typed refusal |
| S10 | Given an open Run, when Reset, then the blade is closed at rest with no reading; a saved and reloaded construction behaves as S1 and S2 | `RadiationShutterTests.ResetAndReloadRestoreTheClosedBlade`; authored state by R5 | S2 | travel 0 ± 0.01 m |

No parameter, so no parameter-refusal row. Chrome recipe (pending, post-catalogue): Battery with its run control, shutter, capsule, meter and a lamp on the meter's `Supply`; Run, then switch the Battery off.

## 6. Open questions

None open.

1. **Open or closed when unpowered — decided now**: closed. It matches the Powered gate and the Beam shutter, and "cut the power to shield" is lesson 110.
2. **Spring return or friction hold — decided** (owner decision 6, 22 November 2026): a return spring with a declared rest position outside the travel limits, owned by ENGINE-JOINT-KINDS (coordinator decision 21); the blade moves at physical speed, which is what the row requires. No orientation limit.
3. **Force, speed and leak law — decided** (D1, D16): the values above; the leak is the receiver's nine samples under the conventions.
4. **Command input — decided** (D19): none. The supply is the command, as on the Powered gate; no row or lesson names a separate signal input.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Battery, Tennis ball (delivered); RAD-01, RAD-02, RAD-08, RAD-09 (undelivered). `PowerIn` is Electrical Input against the Battery's `Supply`.
- **Processes.** [IX-15](../requirements.md#interaction-15), [IX-03](../requirements.md#interaction-03), [IX-06](../requirements.md#interaction-06).
- **Campaign.** Chapter 11, levels 105 and 110; reuse 111–120, 124–130, 136–150, including the dosimeter lesson with RAD-09. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES, ENGINE-SUPPLY-DEMAND and ENGINE-JOINT-KINDS (the rest-position row); ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01 and RAD-08; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names no thermal story).
- **Files.** `elements/RadiationShutter/RadiationShutter.csproj`, `RadiationShutterElementDeclaration.cs`, `radiation_shutter.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/RadiationShutterTests.cs`; a row in `VisibleSolidColliderTests`. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** ENGINE-JOINT-KINDS delivered without a spring rest position outside the travel limits (the inverted row fails; the slice stops and that story is amended; no orientation limit is added); a 4.5 kg blade against a static frame proving unstable at the delivered solver settings (the slice stops and section 5 is re-derived; no alternative is kept here); radiation rows not readable from a dynamic body's collider; a different unit scale (re-derive section 5).

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story).

1. **Declaration data only.** Two bodies, six colliders, one slider with a spring, a rest position and a drive, one supply consumer, one port, one radiation row. No code names the shutter.
2. **Generic laws.** Prismatic joint, spring and drive laws; supply reservation; contact; chord attenuation. "The blade blocks radiation" is a Dense collider at this tick's final pose on the path. "A jam leaks" is contact stopping the blade.
3. **Three layers.** Colliders: six primitives on two bodies. Visuals: the art table; grooves and scale are printed. Animation: the blade art follows the body pose; the indicator is a colour binding.
4. **One-way flow and typing.** Supply → drive → pose → this tick's field solve → reading committed with the tick; presentation reads committed state only. Values are f32.
5. **Flat state.** The counts in section 2 are fixed at compile time.
6. **Performance fitness.** One moving box; no sensor; nine shadow casts per source in range, each through the spatial query's segment cast (radiation convention 12); the blade is slow (at most a few m/s) and 0.10 m thick, so tunnelling does not arise. Island sleeping does not exist today; a resting blade would sleep only once ENGINE-ISLANDS-SLEEP is delivered.
7. **Tests are scenarios.** Rows S1 to S10 run the production simulation; S3 and S6's control use fixture declarations of the same general capabilities.
