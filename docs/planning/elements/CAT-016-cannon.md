# CAT-016 · cannon declaration readiness spec

**Current delivery:** [CAT-016 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Current contract for a delivered part, repaired on 22 November 2026 after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-c.md`). Checked against `elements/Cannon/` on checkout `spec-drafts` (`8d1d68aa`); published main is `25df3cd5`, and its two later commits touch neither this package nor its tests. Sections 1–3 and 5–10 are current. Section 4 is the legacy harvest at `a6c914e`, kept verbatim as evidence; where it differs from the delivered declaration, the declaration governs. The [requirement row](../requirements.md#current-cat-016) remains the acceptance authority.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-016 |
| Kind | `cannon`, catalogue title "Toy cannon" |
| Requirement anchor | [CAT-016](../requirements.md#current-cat-016); retained behaviour [todo-392](../requirements.md#todo-392); related rows [sequence-task-082](../requirements.md#sequence-task-082) (compound chamber eligibility), [sequence-task-084](../requirements.md#sequence-task-084) (compound swept queries), [sequence-task-100](../requirements.md#sequence-task-100) (physical reload), [sequence-task-102](../requirements.md#sequence-task-102) (coupled finite-work actuation), [sequence-task-154](../requirements.md#sequence-task-154) (owned gameplay impulses), [sequence-task-164](../requirements.md#sequence-task-164) (loading boundaries), [sequence-task-205](../requirements.md#sequence-task-205) (physical cannon energy) |
| Mapped identities | None found. [EL-095 Toy revolver](../invest/named-elements.md#element-095) and [EL-091 Cannonball](../invest/named-elements.md#element-091) are separate launcher and payload identities, not this part. |
| Roadmap story | [11.4b Cannon](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership); retain every CAT-016 mode and mapped named-identity obligation. |
| Status | **Delivered.** Package id `cannon`, directory `elements/Cannon`. Slice record [11.4b](../../../_bmad-output/implementation-artifacts/spec-11-4b-cannon.md) (`done`, commit `90fe6fca`). Browser case pending. Every residual slice (section 9) is blocked on the engine programme (owner decision 3a, 22 November 2026); the stories that own this part's gaps are `ENGINE-SPATIAL-QUERIES` (chamber occupancy and muzzle-clearance sweeps) and `ENGINE-OPTICAL` (the jacket's Transmit response). |
| Disposition | Delivered catalogue part; no mapped named identity. EL-091 Cannonball is its planned payload (Ball presets family); EL-095 Toy revolver is a separate launcher. |

## 2. Declaration

The current declaration is `elements/Cannon/CannonElementDeclaration.cs`; the scene is `elements/Cannon/cannon.tscn`.

| Item | Delivered value | Source |
| --- | --- | --- |
| Interfaces | `IEnergyStoreElement`, `IChamberRegionElement`, `IFiniteWorkActuatorElement`, `ISupplyElement`, `ISignalElement`, `ISceneGeometryElement` | `:11`–`:12` |
| Body | one static body (footprint 1 body, 0 moving, no constraint); material restitution 0, friction 0.3, rolling 0, threshold 0.1 m/s | `:19`, `:26` |
| Colliders | jacket and two collar tubes, breech and foot boxes, from hidden proxies `col_Jacket`, `col_RearCollar`, `col_MuzzleCollar`, `col_Breech`, `col_Foot` | `cannon.tscn:132`–`:155` |
| Energy store | capacity `capacity`, initial energy 0, charge power `charge_power` | `:27`–`:29` |
| Chamber region | along the barrel (local +X): radius 0.48 m, half-length 0.55 m, admitted transverse widths 0.6–0.84 m, containment allowance 0.001 m, maximum relative arrival speed 1 m/s | `:30`–`:32` |
| Finite-work actuator | one release from store 1 on chamber 1, speed cap 12 m/s, muzzle sweep padding 0.02 m | `:33`–`:34` |
| Parameters | `capacity` J, default 45, 1–100; `charge_power` W, default 30, 1–100. Real, editor policy Reject, Build only (descriptor defaults) | `:17`–`:18` |
| Ports | `PowerIn`: Electrical, Input, role PowerIn, (−0.45, −0.55, 0.55). `ActivationIn`: Signal, Input, role ActivationIn, pulse, (0.15, −0.55, 0.55). Normals +Z; no output | `:35`–`:41` |
| Supply and signal | `PowerIn` → supply consumer `charge` → the energy store; `ActivationIn` → a physical release request on actuator 1. Electrical node `charge`: Consumer, 12 V, 10 Ω, capacitance 0, maximum 100 W, in the field order (NodeId, Role, RatedVoltage, Resistance, Capacitance, MaxPower) (`CuriousContraptions.Abstractions/ElectricalContractValues.cs:15`–`:21`). The node is validated as finite and read by no law (only `Role` is read): unread dead data, to be consumed by the `ENGINE-SUPPLY-DEMAND` story or removed forward-only. Charging itself is governed by `charge_power` and debits the source | `:42`–`:51` |
| Event | `FiniteWorkReleased` for actuator 1 | `:52` |
| Presentation | `ChargeBar` X scale by `CommittedStoredFraction` (0.001–1); `ReadyFlag` Z rotation 0° when ready, −70° otherwise; `Indicator` albedo by `CommittedFiniteWorkState` (slate, cyan, gold); `Sleeve` local X recoil from −0.22 to −0.38 on the release event, smooth rise and fall over 0.53 s, tanh-sum overlap; pick radius 1.1 m | `:53`–`:75` |
| Palette | category Motion, order 42, colour (0.4, 0.72, 0.79), display name "Toy cannon" | `:14`–`:16` |

At the defaults a full charge takes 1.5 s (45 J ÷ 30 W). The connection kind for `ActivationIn` is Activation; its domain is `SimulationDomain.Signal`.

### Art and solidity (delivered scene)

| Node | Mesh and pose | Layer and class |
| --- | --- | --- |
| `Visual/Jacket` with `col_Jacket` | translucent cyan ring along the barrel: outer radius 0.58 m, bore radius 0.48 m, half-length 0.55 m | visual with a separate hidden tube proxy; hollow |
| `Visual/RearCollar`, `Visual/MuzzleCollar` with `col_RearCollar`, `col_MuzzleCollar` | cream rings at x −0.5 and +0.5: outer radius 0.65 m, bore radius 0.48 m, half-length 0.05 m | visuals with hidden tube proxies; hollow |
| `Visual/Breech` with `col_Breech` | cream box 0.2 × 1.16 × 1.16 m at (−0.65, 0, 0); its inner face closes the chamber | visual with a hidden box proxy; solid |
| `Visual/Foot` with `col_Foot` | navy box 1.7 × 0.2 × 1.4 m at (0, −0.75, 0) | visual with a hidden box proxy; solid |
| `Visual/Sleeve` | slate ring, outer radius 0.565 m, 0.14 m long, at x −0.22 | decorative: moved by the recoil presentation binding, inside the jacket wall |
| `Visual/ChargeTrack` | navy box 0.8 × 0.11 × 0.07 m at (0, −0.55, 0.62) | **defect:** drawn solid, no collider. Part C (row C2) appends a box |
| `Visual/ChargeBar` | gold box 0.8 × 0.07 × 0.075 m at (0, −0.55, 0.66) | decorative: scaled by a presentation binding |
| `Visual/ReadyFlag` | gold box 0.28 × 0.14 × 0.04 m | decorative: turned by a presentation binding |
| `Visual/Indicator` | sphere r 0.07 m at (−0.66, 0.65, 0.4) | **defect:** drawn solid, no collider. Part C (row C1) appends a sphere. Colour moved by a presentation binding |
| `Visual/PowerIn`, `Visual/ActivationIn` | spheres r 0.07 m at the two port positions | **defect:** drawn solid, no collider. Part C (row C2) appends spheres. Sockets keep their colliders (owner decision, 21 November 2026) |

Floating art, recorded and not repaired by part C: both sockets hang 3 cm above the foot and 12.8 cm off the jacket with nothing drawn joining them to the body. Part C is [visible-solid remainder part C](../../../_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-c.md), status **blocked on parts B and B2 being published**; it also teaches the matcher to read a ring mesh from its vertices and triangles (row C10), which is what confirms that the drawn jacket, collars and sleeve match their tubes. The classification source is [`spec-engine-art-solidity.md`](../../../_bmad-output/implementation-artifacts/spec-engine-art-solidity.md), row `cannon`. Palette follows [DESIGN.md "Reloadable toy cannon"](../../../DESIGN.md#reloadable-toy-cannon). No level in `content/puzzles.json` places or stocks the part.

## 3. Engine capabilities

The capability families are listed in the [general-engine element map](../general-engine-element-map.md) CAT-016 row and in [catalogue-elements.json](../../coverage/catalogue-elements.json); they are not duplicated here.

**Capability probe (22 November 2026).** Tests were found by name and line; none was run. Counts: 8 present, 0 present-untested, 0 narrower, 0 missing. The store, chamber and release are generic declared capabilities; the engine holds no cannon code.

| Capability | Status | Symbol | Exercising test |
| --- | --- | --- | --- |
| Supply-charged finite energy store | present | `IEnergyStoreElement`, `EnergyStoreDescriptor`, `SupplyEnergyStoreTarget` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:35`; `CuriousContraptions.Abstractions/EnergyStoreValues.cs:73`; `SupplyContractValues.cs:51`) | `CannonIntegrationTests.DefaultChargeTakesOneAndAHalfSecondsAndPowerLossRetainsPartialEnergy` (`CuriousContraptions.tests/CannonIntegrationTests.cs:53`); generic `EnergyStoreContractTests` |
| Chamber region: exactly one fully contained, slow occupant within declared widths | present | `IChamberRegionElement`, `ChamberRegionDescriptor` (`MechanicalContracts.cs:60`; `CuriousContraptions.Abstractions/ChamberRegionValues.cs:13`) | `CannonIntegrationTests.TwoPhysicalOccupantsPresentUnavailableAndResetToEmpty` (`:103`); generic `ChamberOccupancyTests`, `ChamberContractTests` |
| Finite-work release with a speed cap and swept muzzle clearance | present | `IFiniteWorkActuatorElement`, `FiniteWorkActuatorDescriptor` (`MechanicalContracts.cs:65`; `CuriousContraptions.Abstractions/FiniteWorkActuatorValues.cs:26`) | `CannonIntegrationTests.AuthoredMountAimsAlongItsBarrelAndTenMillimetreMuzzleWallRefuses` (`:336`); generic `FiniteWorkResponseTests`, `FiniteWorkGeometryIntegrationTests` |
| Signal pulse as a physical release request that expires when refused | present | `SignalPhysicalRequestDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:65`) | `CannonIntegrationTests.AnExpiredAttemptDoesNotWaitForChargeOrArrival` (`:135`), `OpeningTheMuzzleGateDoesNotReplayAnExpiredRequest` (`:230`) |
| Open tube colliders | present | `ColliderShapeKind.Tube` (`CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12`) | `CannonIntegrationTests.PhysicalLoadChargesAndFiresOnlyWithItsSeparateSupply` (`:550`) loads a ball through the muzzle |
| Store-fraction and finite-work-state presentation drivers | present | `PresentationDriver.CommittedStoredFraction`, `CommittedFiniteWorkState` (`CuriousContraptions.Abstractions/ElementPresentation.cs:33`, `:34`) | `CannonArtTests.NativeAnnuliAndPrivateCuesUseTheManagedFactoryAndRestoreAuthoredArt` (`CuriousContraptions.tests/CannonArtTests.cs:11`) |
| Event-driven impulse presentation (recoil) | present | `PresentationDriver.Event` with `SimulationEventKind.FiniteWorkReleased` (`elements/Cannon/CannonElementDeclaration.cs:66`–`:69`) | `CannonIntegrationTests.HiddenReleaseRetainsOneVisibleRecoilFrameAndResetCancelsIt` (`:157`) |
| Pause, Step, Run/Reset and save/load | present | generic lifecycle | `CannonIntegrationTests.TwentyRunResetsAndFiveSaveLoadsRestoreOnlyAuthoredConstruction` (`:482`), `PauseRetainsTheClockRequestUntilStepAndResumeDoesNotReplayIt` (`:183`) |

**Engine gaps this part relies on (stated, not designed around).**

- **Chamber and clearance queries scan the scene.** Inside the substep loop (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:1002`) the chamber pass refreshes every collider (`WorkshopPhysicsEngine.Chambers.cs:15`), loops every dynamic body (`:86`) and every collider for each region (`:102`), and sweeps the payload against every collider for muzzle clearance (`:156`–`:167`). The broadphase is not used. That is work proportional to the scene every substep. Engine gap owned by `ENGINE-SPATIAL-QUERIES` (owner decision 3a, 22 November 2026): regions and sweeps take their candidates from its region and shape queries, bounded by their declared volume.
- **Optical response of the jacket.** Every collider absorbs light by default under the optical conventions ([EL-176](EL-176-red-laser-emitter.md) section 10, item 6). The clear jacket's collider `col_Jacket` declares Transmit (1, 1, 1), a criterion of `ENGINE-OPTICAL`, not of a Cannon slice: a beam through the jacket reaches a detector, and a ball in the chamber blocks it.
- **No island sleeping.** Island sleeping is not implemented; a resting body is still stepped (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP` (owner decision 3a, 22 November 2026). The Cannon's body is static, so the gap concerns a payload resting in the chamber, not the part.
- **Generality residue, recorded only.** A finite-work release needs a full store (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.FiniteWork.cs:106`), one occupant, a cylinder region and an axial release, so a multi-shot launcher (EL-095 Toy revolver, EL-225 Latched slingshot cradle) cannot be declared from it. Proposed line for the owning story: "a finite-work actuator declares its minimum releasable store energy (default: capacity)".

**Element dependencies.** Battery; a Signal output for the trigger; Basketball as the payload; System clock, Delay box, State latch and Powered gate for the feeder; Wall for the obstruction control. All delivered.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| H1 | Barrel half-length 0.55 m, bore radius 0.48 m; chamber region uses the same cylinder | `parts/CannonPart.cs@a6c914e:L42-L43`, `parts/CannonPart.cs@a6c914e:L142` | carry forward | authored geometry |
| H2 | Jacket tube (half-length 0.55, inner 0.48, outer 0.58, transparent); collars at x = ±0.5 (half-length 0.05, inner 0.48, outer 0.65, opaque) | `parts/CannonPart.cs@a6c914e:L218-L229`; field order `engine/TubeProxy.cs@a6c914e:L6` | carry forward | authored geometry |
| H3 | Foot box at (0, −0.75, 0) size 1.7×0.2×1.4; breech box at (−0.65, 0, 0) size 0.2×1.16×1.16; `AddBox` takes full size, collider half extents = size/2 | `parts/CannonPart.cs@a6c914e:L216-L217`; `reference/cpu/MachinePart.cs@a6c914e:L352-L356` | carry forward | authored geometry |
| H4 | Pick radius 1.1 m | `parts/CannonPart.cs@a6c914e:L215` | carry forward | selection affordance |
| H5 | Seated Basketball rests against the breech at local x −0.215 to −0.205 | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L283-L300` | carry forward | acceptance fact for seating |
| H6 | Cannon contact material: restitution 0, bounce threshold 0.1, friction 0.3 (constructor order) | `parts/CannonPart.cs@a6c914e:L111`; `engine/physics/PersistentContactPair.cs@a6c914e:L18-L29` | carry forward | material values; the legacy record type is not carried |
| H7 | Ports: `PowerIn` electrical input at (−0.45, −0.55, 0.55); `ActivationIn` activation input at (0.15, −0.55, 0.55) | `parts/CannonPart.cs@a6c914e:L112-L116` | carry forward | typed port layout |
| H8 | Parameters `capacity` and `charge_power` each finite in 1–100; out-of-range or NaN rejects construction | `parts/CannonPart.cs@a6c914e:L117-L122`; `CuriousContraptions.tests/CannonTests.cs@a6c914e:L397-L411` | carry forward | range and atomic rejection |
| H9 | Catalogue defaults `capacity` 45, `charge_power` 30 | `parts/catalog/cannon.tres@a6c914e:L12` | carry forward | defaults |
| H10 | Trigger is the only activation command; pulses coalesce to the next fixed tick and never wait for a later load or charge | `parts/CannonPart.cs@a6c914e:L123-L138` | carry forward | behaviour; per-part `BeforeNetworks` hook is a per-element update loop and is not carried |
| H11 | Two activations in one tick fire once; a payload arriving within the following boundary fires, one arriving after it is `Unseated` and needs a fresh trigger | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L351-L395` | carry forward | acceptance fact |
| H12 | Chamber inspection: disabled cannon collider means Empty; departing payload still overlapping means Busy; zero candidates Empty; more than one Ambiguous | `parts/CannonPart.cs@a6c914e:L139-L157` | carry forward | eligibility outcomes |
| H13 | Seating requires transverse widths Y and Z within 0.6–0.84 m | `parts/CannonPart.cs@a6c914e:L44-L45`, `parts/CannonPart.cs@a6c914e:L160-L163` | carry forward | requirement row restates 0.6–0.84 |
| H14 | Seating requires containment within the chamber with a 0.002 m tolerance and payload speed relative to the cannon at most 1 m/s | `parts/CannonPart.cs@a6c914e:L164-L166`; `engine/physics/CylindricalRegion.cs@a6c914e:L63-L75` | do not carry forward (values) | requirement row says the 0.002 CPU allowance is not current; f32 seating clearance and speed must be frozen anew |
| H15 | Every collider child of a compound payload must be contained; a protruding child leaves the cannon in Loading and the shot `Unseated`; an obstructing child blocks the muzzle | `CuriousContraptions.tests/CannonChamberGeometryTests.cs@a6c914e:L12-L53`; `CuriousContraptions.tests/CompoundWorldSweepTests.cs@a6c914e:L91-L142` | carry forward | full compound containment |
| H16 | Radii 0.29 and 0.43 (widths 0.58, 0.86) are `Unseated`, keep charge and are not launched | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L413-L431` | carry forward | boundary controls |
| H17 | One store per cannon, capacity from parameter, initial energy 0; initial energy may be installed only before the first step | `parts/CannonPart.cs@a6c914e:L82-L83`; `engine/physics/PhysicsWorld.cs@a6c914e:L1193-L1209` | carry forward | store starts empty |
| H18 | Charging accepts `min(capacity − energy, power × dt)` only while `PowerIn` is supplied; overflow is not stored or counted | `parts/CannonPart.cs@a6c914e:L197-L200`; `engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L46` | carry forward | finite charge law; the CPU double ledger is not carried |
| H19 | Firing needs full charge (`energy == capacity`); partial charge returns `Uncharged` without spending; supply loss retains charge | `parts/CannonPart.cs@a6c914e:L171-L172`; `CuriousContraptions.tests/CannonTests.cs@a6c914e:L335-L349`, `CuriousContraptions.tests/CannonTests.cs@a6c914e:L500-L533` | carry forward | acceptance facts |
| H20 | Muzzle clearance sweeps the payload geometry along the axis by `0.55 + 0.02 − rear`, excluding only cannon and payload; not clear means `Obstructed` | `parts/CannonPart.cs@a6c914e:L176-L187` | carry forward | the requirement keeps "excludes only cannon and payload"; the 0.02 overshoot is an open question |
| H21 | A wall of half-thickness 0.005 m (0.01 m thick) at x 0.8 blocks the shot: charge 45 kept, payload velocity zero, identity kept | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L195-L210` | carry forward | negative control |
| H22 | Release: incoming axial speed ≥ 16 m/s returns `SpeedLimited`; otherwise one axial impulse at the payload centre targets 16 m/s with unbounded impulse, limited by stored work; zero impulse returns `NoResponse` | `parts/CannonPart.cs@a6c914e:L46`, `parts/CannonPart.cs@a6c914e:L188-L192` | carry forward (behaviour) | the 16 m/s cap versus Story 11.4's 12 m/s is an open question |
| H23 | Release is atomic: failure restores the world and the store debit equals supplied work | `engine/physics/PhysicsWorld.cs@a6c914e:L1250-L1267`; `engine/physics/PhysicsEnergyStore.cs@a6c914e:L47-L52` | carry forward | transactional release; the CPU `ApplyPoweredImpulse` path is not carried |
| H24 | With a slider-coupled partner both move at `sqrt(2·45/(m1+m2))` and kinetic energy equals released energy; a payload fixed to the cannon returns `NoResponse` with charge kept | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L65-L122` | carry forward | coupled-response acceptance (needs Epic 10 joints) |
| H25 | Default shot of a 1 kg Basketball: axial speed 9.48–9.50 m/s, transverse speed ≤ 0.001, released 44.99–45 J, payload moves under 0.1 m in the trigger ticks (no teleport) at rotations (0,0,0), (0,90,0), (0,0,90), (30,45,60) | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L38-L63`; `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L380-L416` | carry forward | positive acceptance; launch axis uses solved pose, not presentation |
| H26 | Same payload rebounds from a thin wall (half-thickness 0.001 m, 0.002 m thick) at x 2 and never exceeds released energy | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L124-L143` | carry forward | integration fact |
| H27 | Charge bar X-scale shows fill fraction with minimum scale 0.001; bar updates on the next presented frame after the release | `parts/CannonPart.cs@a6c914e:L87-L92`; `CuriousContraptions.tests/EnergyObservationTests.cs@a6c914e:L103-L125` | carry forward | cosmetic binding |
| H28 | Phases `Empty, Loading, Charging, Ready, Firing, Jammed` and results `None, Fired, Empty, Uncharged, Unseated, Obstructed, Ambiguous, Busy, SpeedLimited, NoResponse` as enums; phase mapping from chamber state and charge; flag and indicator maps | `parts/CannonPart.cs@a6c914e:L10-L11`, `parts/CannonPart.cs@a6c914e:L95-L109`, `parts/CannonPart.cs@a6c914e:L201-L210` | carry forward | closed sets stay enums |
| H29 | Diagnostic wire names (`"speed_limited"`, `"no_response"` …) reject numbers, case changes and unknown values | `CuriousContraptions.tests/CannonDiagnosticTests.cs@a6c914e:L29-L84` | carry forward (typed enum sets and reject-unknown rule) / do not carry forward (Playtest wire strings) | The Playtest frame boundary is deleted with `tools/Playtest`; any new serialization boundary keeps typed enums and rejects unknown values (same rule as CAT-071 row 14) |
| H30 | Recoil: maximum 0.16, 0.08 s compression, 0.45 s return, tanh-sum overlap; hidden shot catches up to −0.16·tanh(1); rejected shots never recoil; recoil never changes physics state | `parts/CannonPart.cs@a6c914e:L47-L59`; `CuriousContraptions.tests/CannonTests.cs@a6c914e:L578-L621`; `CuriousContraptions.tests/CannonRuntimeCheckpointTests.cs@a6c914e:L162-L205` | carry forward | cosmetic contract (`DESIGN.md@a6c914e:L125-L131`) |
| H31 | Sleeve rest at x −0.22, sleeve tube half-length 0.07, radii 0.50–0.565, inside the jacket | `parts/CannonPart.cs@a6c914e:L49`, `parts/CannonPart.cs@a6c914e:L221-L223` | carry forward | art geometry |
| H32 | Scene binds only the script | `parts/scenes/cannon.tscn@a6c914e:L1-L4` | do not carry forward | Godot script binding is legacy |
| H33 | Art boxes: charge track 0.8×0.11×0.07 navy at (0, −0.55, 0.62); bar 0.8×0.07×0.075 at (0, −0.55, 0.66); flag 0.28×0.14×0.04 at (−0.55, 0.75, 0); indicator sphere r 0.07 at (−0.66, 0.65, 0.4); port spheres r 0.07 | `parts/CannonPart.cs@a6c914e:L230-L235` | carry forward | art layout |
| H34 | Catalogue entry: title, category "Motion", description, colour | `parts/catalog/cannon.tres@a6c914e:L6-L12` | carry forward | catalogue data |
| H35 | Empty or uncharged trigger is not queued for a later arrival; after charge the arrived ball needs a new trigger | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L145-L193`, `CuriousContraptions.tests/CannonTests.cs@a6c914e:L302-L333` | carry forward | acceptance fact |
| H36 | Trigger while the payload departs returns `Busy`; shot count stays 1 | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L270-L281` | carry forward | departing guard |
| H37 | Reload: a distinct arriving ball fires second (shot 2); without arrival result is `Empty`; returning same ball under gravity reseats and fires again, total release 89.99–90.01 J; part count never changes | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L212-L268`, `CuriousContraptions.tests/CannonTests.cs@a6c914e:L555-L576` | carry forward | reload acceptance |
| H38 | Timed feeder: cannon at 75°, Clock → cannon, Clock → Delay → Latch powering a Powered gate releases a second ball; 2 shots and 90 J with gate supplied, 1 shot and 45 J without | `CuriousContraptions.tests/CannonFeederTests.cs@a6c914e:L17-L71` | carry forward | integration fact |
| H39 | Clearing a closed Powered gate needs a fresh trigger and uses retained charge | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L452-L498` | carry forward | blocked-to-clear control |
| H40 | Reset restores the exact saved construction, store 0, shot count 0, no last payload; JSON reload replays an identical shot and flight | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L535-L553`, `CuriousContraptions.tests/CannonTests.cs@a6c914e:L623-L637`; `CuriousContraptions.tests/CannonEnergyOwnershipTests.cs@a6c914e:L12-L70` | carry forward | Run/Reset and save/load |
| H41 | Gizmo-edited rotations survive 20 Run/Reset cycles and 5 save/load cycles exactly | `CuriousContraptions.tests/ConstructionResetTests.cs@a6c914e:L30-L64`; `CuriousContraptions.tests/AuthoredOrientationSceneTests.cs@a6c914e:L33-L58` | carry forward | pose restoration |
| H42 | A failed tick rolls back shot result, count, payload identity, store, events and recoil occurrence together | `CuriousContraptions.tests/CannonRuntimeCheckpointTests.cs@a6c914e:L35-L119` | carry forward | requirement: events roll back together; the CPU checkpoint participant mechanism is not carried |
| H43 | A full recoil occurrence queue (64) rejects the physical shot until drained | `parts/CannonPart.cs@a6c914e:L53-L56`; `CuriousContraptions.tests/CannonRuntimeCheckpointTests.cs@a6c914e:L121-L160` | do not carry forward | physics must not depend on animation feedback (one-way flow) |
| H44 | Clock-fed workload: 3600 ticks give 14 shots and 630 J with supply, 0 without | `CuriousContraptions.tests/CannonWorkloadTests.cs@a6c914e:L17-L52` | do not carry forward | CPU timing and allocation workload; performance stays at its named gate |
| H45 | Seventeen named Chrome recipe cases (powered, no supply, blocked, empty, oblique (0,30,75), return reload, distinct feed, recoil) with cannon at (0,3,0), rotation (0,0,90) and ball at (0,5,0) | `docs/cannon-recipes.json@a6c914e:L1-L1596` | carry forward | surviving doc; recipes seed the acceptance outline |
| H46 | Two balls at chamber-local x −0.21 and 0.47 make the shot Ambiguous; charge 45 J and shot count 0 are kept; both identities stay visible | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L433-L450` | carry forward | ambiguous-load control |
| H47 | Recoil presentation: the per-frame offset step is ≤ 0.02 and the minimum reaches < −0.1 before returning to 0; physics state, boxes and tubes are unchanged | `CuriousContraptions.tests/CannonTests.cs@a6c914e:L578-L603` | carry forward | cosmetic bounds; one-way flow |
| H48 | Feeder without a supplied gate: the unfed second ball stays visible and rests at y 8.39–8.41; one shot and 45 J | `CuriousContraptions.tests/CannonFeederTests.cs@a6c914e:L57-L66` | carry forward | negative feeder control |

### Files harvested

- `parts/CannonPart.cs`
- `parts/catalog/cannon.tres`
- `parts/scenes/cannon.tscn`
- `engine/physics/PhysicsEnergyStore.cs`
- `engine/physics/PhysicsWorld.cs` (energy-store install, charge and release only)
- `engine/physics/CylindricalRegion.cs` (containment tolerance only)
- `engine/physics/PersistentContactPair.cs` (contact-material constructor order only)
- `engine/SceneEnergyStoreDeclaration.cs`
- `engine/TubeProxy.cs`
- `reference/cpu/MachinePart.cs` (`AddBox` size convention only)
- `CuriousContraptions.tests/CannonTests.cs`
- `CuriousContraptions.tests/CannonChamberGeometryTests.cs`
- `CuriousContraptions.tests/CannonDiagnosticTests.cs`
- `CuriousContraptions.tests/CannonEnergyOwnershipTests.cs`
- `CuriousContraptions.tests/CannonFeederTests.cs`
- `CuriousContraptions.tests/CannonRuntimeCheckpointTests.cs`
- `CuriousContraptions.tests/CannonWorkloadTests.cs`
- `CuriousContraptions.tests/AuthoredOrientationSceneTests.cs` (cannon fixture only)
- `CuriousContraptions.tests/CompoundWorldSweepTests.cs` (cannon test only)
- `CuriousContraptions.tests/ConstructionResetTests.cs` (cannon fixture only)
- `CuriousContraptions.tests/EnergyObservationTests.cs` (cannon reservoir only)
- `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs` (cannon test only)
- `CuriousContraptions.tests/EnumPresentationTests.cs` (checked, no element knowledge: generic enum-binding test reusing `CannonPhase`)
- `content/puzzles.json` (checked, no element knowledge: no cannon level or inventory)
- `reference/P0-022-before/docs/coverage/engine/*.json` (checked, no element knowledge: coverage records naming `cannon`)
- `reference/P0-022-candidate-r1/status/ownership-reconciliation.md` (checked, no element knowledge)
- `tools/P0-007-actual-path-probe/inputs.json` (checked, no element knowledge: probe input list)
- `docs/cannon-recipes.json` (surviving doc, not deleted by Epic 7)

## 5. Acceptance outline

Requirement: the [CAT-016 row](../requirements.md#current-cat-016). The Chrome recipe (place a Toy cannon pointing up, a Basketball above the muzzle, a Battery wired to `PowerIn` and a Switch with a striker wired to `ActivationIn`; verify the construction; Run) is queued for the post-catalogue browser phase, with `docs/cannon-recipes.json` as its input.

**Native tests that exist per requirement criterion (22 November 2026; none was run for this review).** In `CuriousContraptions.tests/CannonIntegrationTests.cs`. Store starts empty, full charge in 1.5 s, power loss keeps partial energy: `:53`. Separate supply: `:550`. Ambiguous occupancy: `:103`. Rejected trigger expires (empty, partial, early): `:135`. Blocked muzzle, and blocked-to-clear needs a fresh trigger: `:336`, `:230`. Rotated bore: `SuppliedAuthoredMountLaunchesOnItsClockRequest` (`:284`). Same-ball reload: `GravityReturnsTheSameBallForAnotherPaidShot` (`:516`). Distinct-ball timed feeder: `ClockDelayLatchAndGateFeedADistinctSecondBall` (`:407`). Recoil as cosmetic feedback, none on rejection: `:157`. Rollback and lifecycle: `:482`, `:183`, `:13`. Wrong-kind connection, generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`BundlePortsTests.cs:262`). Compound containment, both widths, moving occupant and the coupled response are in the generic suites `ChamberOccupancyTests`, `ChamberContractTests`, `FiniteWorkResponseTests` and `OwnedFiniteWorkTransactionTests`; this review listed those files and did not read their rows.

**Residual acceptance rows.**

| Row | Given / When / Then | Planned test | Control | Boundary | Tolerance and stall guard |
| --- | --- | --- | --- | --- | --- |
| CN-R1 parameter refusal | Given a saved or edited cannon whose `capacity` or `charge_power` is outside 1–100, NaN or infinite, When admitted, Then it is refused with a typed reason and nothing changes | `CannonIntegrationTests.AnOutOfRangeOrNonFiniteParameterIsRefusedAndChangesNothing`; superseded by row R2 of the conformance suite (S4) if that lands first. The refusal code is one generic seam (`engine/gpu/InstanceParameters.cs:37`–`:49`) | the corners (1 J, 1 W) and (100 J, 100 W) are admitted and each charges in 1 s | one f32 step outside each bound | exact refusal; charge time within one simulation tick of 1 s, since charging is a timed contract |
| CN-R2 width window through real balls | Given a Basketball and a Tennis ball at radius 0.35 m, 0.25 m and 0.45 m, each loaded in a charged cannon, When triggered, Then the Basketball and the 0.35 m Tennis ball fire, and the 0.25 m and 0.45 m balls are refused and keep the charge | `CannonIntegrationTests.OnlyBallsInsideTheDeclaredWidthWindowFire` | the Basketball | none here: the exact edges are `ChamberOccupancyTests.WidthAndRelativeArrivalAdmissionIncludeTheirDeclaredBoundaries` (`CuriousContraptions.tests/ChamberOccupancyTests.cs:21`) | fired means muzzle exit within a 3 s stall guard; refused means the ball's speed stays under 0.1 m/s (the bounce threshold) and stored energy is unchanged |
| CN-R3 indicator, track and sockets solid; rings match their tubes | rows C1–C4 and C10 of visible-solid remainder part C | owned by that contract | — | — | — |

## 6. Decisions

The harvest raised ten planning questions; all are decided. The owner's decision of 18 November 2026 and the Story 11.4b resolution in section 7 settle muzzle speed, the release cap, seating values, the sweep padding, compound payloads, units, the shot-result set and recoil overflow. Two were decided by this review under the standing delegation: the Bowling ball stays ineligible by the declared width window, with no identity exception; and `docs/cannon-recipes.json` is kept as input for the post-catalogue browser phase. No item needs the owner.

## 7. Current Story 11.4b resolution (19 November 2026)

Owner decision, 18 November: retain default capacity **45 J**, charging **30 W**, and set release cap to **12 m/s**. The stationary free 1 kg payload is energy-limited to sqrt(90), approximately **9.49 m/s**; the old exact-12 m/s story expectation is superseded. Retain SpeedLimited at the cap and NoResponse for a fixed/zero-response target, with no debit on either rejection. Energy and power are J and W.

The [native Cannon plan](../../../_bmad-output/implementation-artifacts/spec-11-4b-cannon.md) records independently approved package admission controls of **0.001f m** containment allowance, **1f m/s** maximum relative arrival speed and **0.02f m** muzzle sweep padding. The slice is published (`90fe6fca`). The 1 mm allowance permits seated contact without treating visible protrusion as fully contained; the admitted maximum spherical width leaves 60 mm radial clearance. Relative motion at 1 m/s spans at most 16.7 mm at the slowest admitted committed simulation tick profile (60 Hz). Canonical operation remains 120 Hz simulation/480 Hz physical substeps and independent 60 Hz animation; `CuriousContraptions.Abstractions/CadenceValues.cs:49–73` admits 60/120/240 Hz simulation with 8/4/2 substeps. The bound uses that supported simulation profile, not the animation clock. The 20 mm sweep pad places the payload's rear support beyond the muzzle and its immediate exterior. These are authored gameplay thresholds, not numerical-test epsilons, and must not be tuned to observed results. Compound radial/cap containment still checks actual material, not merely transverse widths.

Both transverse widths remain inclusively **0.6–0.84 m**. Consequently the current 0.56 m Bowling ball is Unseated; no element identity exception is added. Future payloads qualify by their declared geometry. Multi-child bodies already exist in the published engine; bounded full-material containment/clearance queries and coupled finite-work release belong to this consuming slice. Harvest statements that compounds, joints, supply or annular contacts are missing are historical, not current prerequisites.

Use exact declared store/region/actuator/body references, including multiple instances on one owner. A generic finite reservoir separates capacity, initial energy and charging from contact/trigger release. The constrained release solves contacts, joints and carrier reaction with one finite source debit; payload-mass-only release is forbidden. Typed owned Idle transactions remain a simulation boundary, not a new direct part impulse API or runtime ammunition insertion.

Current availability must follow current chamber, clearance and charge, not a historical failed request. LastResult remains diagnostic: after the muzzle physically clears, a fully charged seated payload returns to Ready without an automatic shot, debit or recoil; a fresh trigger is still required. The review-pass-1 native amendment defines generic exact-actuator committed clearance, including typed uncertainty and Reset. This repairs the earlier persistent-Obstructed presentation rule; H28's distinction between current phase and shot result remains authoritative.

Recoil explicitly authors independent generic choices: SmoothRiseFall, HyperbolicTangentSum, DeferUntilVisible, EventTimeRetainFirstFrame, .53 s duration and .08/.53 peak. Other admitted impulse curves may compose their supported overlap/visibility/timing independently; the shared engine must not select a Cannon recipe from its curve. Recoil uses the existing demand-grown occurrence/animation ownership. Cosmetic queue occupancy never refuses a physical shot; explicit ring retention/lapping rules remain. Preserve `docs/cannon-recipes.json` as historical recipe input for the deferred Chrome phase, not current executable proof. Native artwork has no script bindings. H25/H37/H48 historical numeric windows and whole replay identity are replaced by independent semantic gameplay/work/clearance assertions under current testing guidance; retained reload, feeder, rebound and exact authored Reset/save behavior remain required. Failed-tick atomicity stays a design obligation; injected-fault qualification stays at its named later gate.

## 8. Integration

- **Collaborators, all delivered:** Battery (Supply, Electrical Output → `PowerIn`, Electrical Input), Impact switch, System clock and Delay box (Signal Output → `ActivationIn`, Signal Input), State latch, Powered gate, Wall, Basketball, Receiver.
- **Which balls seat (widths 0.6–0.84 m in both cross directions, from `:32`).** Basketball, 0.68–0.70 m with its bands: yes. Bowling ball, 0.56 m: no (Unseated). Tennis ball, a plain sphere whose seam is printed (owner decision 24i): seats for radius 0.30–0.42 m (width 2 × radius in 0.6–0.84 m) at any turn; at its default radius 0.25 m it does not seat. Baseball after its approved repair, 0.52–0.54 m with its band: no. Planned EL-091 Cannonball and EL-077 Soccer ball, both proposed at radius 0.32 m, 0.64–0.66 m with a band: yes, so the Ball presets family is not disqualified by the chamber. A change of either proposed radius must stay inside the window.
- **Energy.** 45 J into a free 1 kg ball gives about 9.49 m/s, under the 12 m/s cap and above a Bumper's 8 m/s target; a heavier ball leaves slower by the square root of the mass ratio.
- **Campaign.** No level in `content/puzzles.json` places a Cannon. S8 is not needed.
- **Coverage binding.** Catalogue key `cannon` (CAT-016).

## 9. Delivery and residuals

- **Delivered** by 11.4b.
- **Blocked on the engine programme (owner decision 3a, 22 November 2026).** Every residual slice below waits for it, the tests-only rows included. The stories that own this part's gaps are `ENGINE-SPATIAL-QUERIES` and `ENGINE-OPTICAL` (section 3).
- **Residual slices.** (1) Visible-solid remainder part C, blocked on parts B and B2; engine tier (it changes the shared matcher). (2) CN-R1 and CN-R2: tests only, data-only tier, CN-R1 absorbed by S4 if that lands first. (3) Supply drain: none outstanding. Charging debits the source through the supply-to-store binding (`CannonIntegrationTests.cs:83`).
- **Engine work this part waits on for conformance, not for play:** `ENGINE-SPATIAL-QUERIES` for the chamber and sweep queries, and `ENGINE-OPTICAL` for the jacket's Transmit response (owner decision 3a, 22 November 2026: the engine programme precedes further element delivery).
- **Probe re-check for part C:** the four new colliders are appended after `col_Foot` and lie outside the bore, so the chamber and sweep, which exclude only the cannon's own body and the payload, are unaffected.

## 10. Engine conformance (owner instruction, 22 November 2026)

**Verdict: Conforms with fixes.** The package is declaration data only. Fixes: the four missing colliders of section 2 (visible-solid remainder part C); the unread electrical node values (point 1); the scene-proportional chamber and clearance queries the part relies on (point 6, `ENGINE-SPATIAL-QUERIES`); and the jacket's optical response (point 3, `ENGINE-OPTICAL`). No engine, bundle, abstraction or `ui/` source names this element; the only hits are coverage and browser-tool tables under `tools/`.

1. **Declaration data only.** One static body, five scene colliders, one energy store, one chamber region, one finite-work actuator, two parameters, two ports, one electrical consumer node, one signal request node, one event row and six presentation bindings. The phases and shot results are the engine's finite-work state and request-result enums for any actuator. Fix: the electrical node's four numbers are read by no law (section 2).
2. **Generic laws.** Charging: the supply network delivers power to a declared energy store, bounded by `charge_power` and `capacity`. Eligibility: a chamber region query (one fully contained occupant, declared widths, declared maximum relative speed). Firing: a signal pulse delivered as a release request to a finite-work actuator, which solves one bounded axial impulse with contacts, joints and the carrier reaction and debits the store once; a refused request expires. Clearance: a swept query along the region axis that excludes only the actuator's own body and the payload. Loading and reload: ordinary contact in a tube. Nothing creates or teleports a body.
3. **Three layers.** Colliders: `col_Jacket`, `col_RearCollar`, `col_MuzzleCollar` (tubes), `col_Breech`, `col_Foot` (boxes), all hidden proxies; part C appends a box for the charge track and spheres for the indicator and two sockets. `col_Jacket` declares the optical response Transmit (1, 1, 1) under `ENGINE-OPTICAL` (section 3). Visuals: imported jacket and collar meshes, breech, foot, sleeve, charge track and bar, flag, indicator, sockets. Animation: charge bar scale from `CommittedStoredFraction`; flag and indicator from `CommittedFiniteWorkState`; sleeve recoil from the `FiniteWorkReleased` event. The sleeve, bar and flag are decorative; the recoil pushes nothing.
4. **One-way flow and typing.** Physics and domain state → committed reads and one event → presentation. The cosmetic queue never refuses a shot (section 7). Typed ids for store, region and actuator; `SimulationEventKind`, `PhysicalRequestOperation`; J and W as f32.
5. **Flat-state counts.** 1 static body; 5 colliders (3 tubes, 2 boxes), 9 after part C; 0 joints and drives; 1 energy store; 1 chamber region; 1 finite-work actuator; 1 electrical node; 1 supply consumer; 1 signal node; 1 event row; 2 ports; 2 parameters; 6 presentation bindings.
6. **Performance fitness.** Five static colliders, nine after part C. A fired ball leaves at up to 12 m/s; tunnelling through thin parts is excluded by the engine's speculative contacts, not by a cannon check. Stated truthfully: the chamber region and the muzzle sweep are bounded volumes in the declaration, but the engine evaluates them by refreshing every collider, looping every dynamic body and every collider per region and sweeping the payload against every collider, inside the substep loop (`WorkshopPhysicsEngine.Chambers.cs:15`, `:86`, `:102`, `:156`–`:167`; `WorkshopPhysicsEngine.cs:1002`). That is work proportional to the scene. Engine gap owned by `ENGINE-SPATIAL-QUERIES` (owner decision 3a, 22 November 2026). Island sleeping is not implemented; a resting body is still stepped (no sleep or deactivation code in `CuriousContraptions.Simulation`). Owned by `ENGINE-ISLANDS-SLEEP`; the Cannon's body is static, so it concerns a resting payload, not the part.
7. **Scenario tests.** `CannonIntegrationTests` runs real constructions; region, response and transaction laws have their own generic suites.
