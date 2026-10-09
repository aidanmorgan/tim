# CAT-016 · cannon declaration readiness spec

This is the Story 7.0 CAT-016-D declaration readiness spec for the Toy cannon, written against baseline commit `a6c914e` before Epic 7 deletes the legacy that holds its knowledge. Legacy citations use `path@a6c914e:Lstart-Lend` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; current-engine files are cited at the same commit so the line ranges stay stable. The [requirement row](../requirements.md#current-cat-016) remains the acceptance authority; this spec records the concrete declaration knowledge the legacy holds.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-016 |
| Kind | `cannon`, catalogue title "Toy cannon" (`parts/catalog/cannon.tres@a6c914e:L6-L7`) |
| Requirement anchor | [CAT-016](../requirements.md#current-cat-016); retained behaviour [todo-392](../requirements.md#todo-392); related rows [sequence-task-082](../requirements.md#sequence-task-082) (compound chamber eligibility), [sequence-task-084](../requirements.md#sequence-task-084) (compound swept queries), [sequence-task-100](../requirements.md#sequence-task-100) (physical reload), [sequence-task-102](../requirements.md#sequence-task-102) (coupled finite-work actuation), [sequence-task-154](../requirements.md#sequence-task-154) (owned gameplay impulses), [sequence-task-164](../requirements.md#sequence-task-164) (loading boundaries), [sequence-task-205](../requirements.md#sequence-task-205) (physical cannon energy) |
| Mapped identities | None found. [EL-095 Toy revolver](../invest/named-elements.md#element-095) and [EL-091 Cannonball](../invest/named-elements.md#element-091) are separate launcher and payload identities, not this part — not mapped; Batch A confirms. |
| Roadmap story | [11.4 Wound Spring Potential Motor & Toy Cannon Launcher](../../../_bmad-output/planning-artifacts/epics.md) |
| Status | Not started: `cannon` has no `WorkshopPartKind` (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3`) |

## 2. Declaration

### Bodies and shapes

One static body in the part frame; the barrel axis is local +X. All values are legacy authored metres (harvest H1–H5).
- **Jacket:** an open annular tube, half-length 0.55, inner (bore) radius 0.48, outer radius 0.58, transparent.
- **Collars:** two opaque annular rings at x = ±0.5, half-length 0.05, inner radius 0.48, outer radius 0.65.
- **Breech:** a solid box centred at (−0.65, 0, 0), size 1.16 × 1.16 in Y × Z and 0.2 in X (half extents 0.1, 0.58, 0.58). Its inner face at x = −0.55 closes the chamber.
- **Foot:** a box centred at (0, −0.75, 0), size 1.7 × 0.2 × 1.4 (half extents 0.85, 0.1, 0.7).
- **Chamber region:** the cylinder of half-length 0.55 and radius 0.48 along local X. It is a query region, not a collider.

The breech and foot use `ColliderDeclaration` boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`). The jacket and collars need the annular collider that Stories 6.6/6.7 add for CAT-048; the parked precedent is `AnnularProfile` and `PipeDimensions` (`engine/gpu/AnnularProfile.cs@a6c914e:L6-L25`, `engine/gpu/WorkshopPipe.cs@a6c914e:L6-L11`). The legacy bore radius is 0.48, while the pipe bore is 0.65, so the cannon declares its own radius.

### Mass and material

- **Mass:** none. The cannon is a static body (`RigidMotionKind.Static`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`) and has no recoiling barrel (H30).
- **Material:** restitution 0, bounce threshold 0.1 m/s, friction 0.3 (H6), declared as a `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Payload:** the payload keeps its own declared material. The Basketball is 0.34 m / 1 kg (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L51`).

### Constraints and joints

None. The cannon is static and holds no joint. Legacy tests attach a slider joint to the payload only to prove the coupled release response (H24); joints arrive in Epic 10.

### Typed sockets and ports

| Socket | Domain | Direction | Local position (m) |
| --- | --- | --- | --- |
| `PowerIn` | Electrical | Input | (−0.45, −0.55, 0.55) |
| `ActivationIn` | Activation | Input | (0.15, −0.55, 0.55) |

Source: H7. Both sockets already exist in `WorkshopSocket` and `WorkshopConnectionDomain` (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`). The cannon has no output port.

### Sensors and activation

- **Trigger:** one activation input (`Trigger`), coalesced to the next fixed-tick boundary (H10, H11).
- **Chamber sensor:** every tick, it inspects which dynamic bodies overlap the chamber region (H12–H16).

`ResidenceSensorDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L123-L141`) watches only an open box against one named target, so it cannot express "exactly one of any dynamic body, fully contained in a cylinder". Story 11.4 must add a chamber-containment sensor, which may generalise the Story 6.8 aperture sensor. Activation edges use `ActivationNetwork` (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`, `engine/gpu/ActivationNetwork.cs@a6c914e:L70`); a trigger consumer node kind does not exist yet.

### Work and energy stores

One finite store, owned by the cannon:
- **Capacity:** the `capacity` parameter.
- **Initial energy:** 0 (H17).
- **Charging:** while `PowerIn` is supplied, the store accepts `charge_power × dt`, clipped at capacity (H18).
- **Release:** the stored energy is the work budget for one axial impulse on the payload (H22).

`ContactWorkDeclaration` (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L36`) and `BumperWork` (`engine/gpu/WorkshopBumper.cs@a6c914e:L6-L29`) are the current finite-store precedents. However, the bumper is contact-triggered and preloaded, not supply-charged and trigger-released, so Story 11.4 must add a supplied charge store.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `capacity` | f32 energy | 1–100 | 45 | J (legacy "game energy", equal to ½mv² in tests) | H8, H9 |
| `charge_power` | f32 power | 1–100 | 30 | W (J/s) | H8, H9 |

At the defaults, a full charge takes 1.5 s (45 J ÷ 30 W).

### Cosmetic curves and UI bindings

- **Charge bar:** X-scale follows the store fill fraction (0–1), with a minimum scale of 0.001, and is always visible (H27).
- **Ready flag:** rotates about Z to 0° in `Ready` and to −70° in every other phase (H28).
- **Indicator sphere:** slate `#556573` in Empty, Loading, Firing and Jammed; cyan `#66b8c9` in Charging; gold `#f7cb52` in Ready (H28).
- **Recoil sleeve:** a translation of up to −0.16 along X driven by the shot occurrence. It uses a smooth rise and fall, 0.08 s compression and 0.45 s return, and a tanh-sum overlap (H30, H31).

The current carrier is `CosmeticCurveDeclaration` (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L17`). Its `AnimationFeedbackSource` has no shot-occurrence or energy-store source yet; Story 11.4 adds them.

### Art

- **Scene:** `parts/scenes/cannon.tscn` binds only the script (H32). The geometry is code-built (H33).
- **Palette:** follows [DESIGN.md "Reloadable toy cannon"](../../../DESIGN.md#reloadable-toy-cannon): navy foot `#293954`; cream breech and collars `#fff8e9`; transparent cyan jacket RGBA (0.4, 0.72, 0.79, 0.22); slate sleeve `#556573`; gold charge bar `#e8b764` over a navy track; gold flag `#f7cb52`; port markers `#e8b764`.
- **Catalogue colour:** `#66b8c9`.
- **Pictogram:** the original navy cannon pictogram is kept.

### Catalogue and inventory entry

- **Id and title:** `cannon`, "Toy cannon".
- **Category:** "Motion".
- **Colour:** (0.4, 0.72, 0.79).
- **Defaults:** `capacity` 45, `charge_power` 30.
- **Description:** "Load a ball through the open muzzle. A separate electrical supply charges the finite store; an activation fires only a fully charged, seated single ball through a clear muzzle. Empty or obstructed attempts retain charge. It never creates ammunition." (H34)

No level in `content/puzzles.json` places the cannon or stocks it in an inventory (`git grep '"cannon"' a6c914e -- content/puzzles.json` returns nothing). Inventory admission needs a `WorkshopPartKind` and `PartInventory` entry (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

## 3. Engine capabilities

The capability families are listed in the [general-engine element map](../general-engine-element-map.md) CAT-016 row and in [catalogue-elements.json](../../coverage/catalogue-elements.json); they are not duplicated here.

**Exists now**
- **Dynamic sphere payloads and static box colliders:** `RigidBodyDeclaration` and `ColliderDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`), with the Basketball material (`engine/gpu/WorkshopConstruction.cs@a6c914e:L41-L53`).
- **Box2D v3 TGS Soft contact solve at 480 Hz substeps:** `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L208`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236` (`makeSoft`), `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623` (`prepareContact`), `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L774` (`solveNormalRow`).
- **Typed Activation and Electrical connection domains, with `PowerIn` and `ActivationIn` sockets:** `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.
- **Activation network with typed nodes and edges:** `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`, `engine/gpu/ActivationNetwork.cs@a6c914e:L70`.
- **Finite paid-work store pattern:** `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L36`.
- **Cosmetic impulse curves forwarded to the animation worker:** `engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L17`.
- **Residence or capture latch over a committed sensor candidate:** `engine/gpu/CaptureLatch.cs@a6c914e:L5-L8`.

**Missing**
- **Annular jacket and collar colliders with an open bore:** Stories 6.6/6.7 (CAT-048 pipe).
- **Electrical supply source and powered-consumer evaluation:** Story 8.1 (CAT-005 Battery).
- **Supply-charged finite store, chamber containment sensor, coalesced trigger consumer, muzzle swept-clearance query, axial release impulse and typed phase/shot reads:** Story 11.4.
- **Coupled release response against a jointed payload** (H24): needs joints from Epic 10 (Stories 10.3/10.2) before that control can run.
- **Compound (multi-child) payload bodies,** which the requirement's full compound containment needs (H15): no current story named; see Open questions.

**Element dependencies**
- **Required for the first proof:** CAT-005 Battery (supply), CAT-001 Basketball (payload) and an activation source such as CAT-063 Switch.
- **Needed for the feeder integration:** CAT-017 Clock, CAT-022 Delay, CAT-037 Latch and CAT-051 Powered gate (H38).
- **Needed for the obstruction control:** CAT-066 Wall (H21).

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

The requirement is the [CAT-016 row](../requirements.md#current-cat-016) and Story 11.4.

**Chrome UI construction recipe.** Use actual Chrome UI only.
1. From the toolbox, place a Toy cannon and rotate it with the gizmo to point up (rotation (0, 0, 90)).
2. Place a Basketball above the muzzle so it falls into the bore (recipe `cannon-powered-v1`: cannon (0, 3, 0), ball (0, 5, 0)).
3. Place a Battery and wire its `Supply` to the cannon's `PowerIn`.
4. Place a Switch with a striker ball and wire its `ActivationOut` to the cannon's `ActivationIn`.
5. Verify the placed configuration and the typed connections, then Run.

**Positive.** After the full charge (1.5 s at defaults), the trigger launches the same ball along the bore. The muzzle speed equals the frozen value (see Open questions), the transverse velocity is preserved, the part count is unchanged, the ball does not teleport, and the recoil plays once.

**Negative and control.**
- Missing supply wire: no launch.
- Empty chamber: no launch, and the charge is kept.
- Blocked muzzle (wall): no launch, the charge and payload identity are kept, and only a fresh trigger after clearing fires.
- Wrong-size or protruding payload: `Unseated`.
- Two balls: `Ambiguous`.

**Boundaries.**
- Parameter minimum and maximum, plus atomic rejection just outside 1–100.
- Payload widths at the 0.6/0.84 edges.
- The frozen f32 seating clearance and speed.
- Arrival exactly at the following boundary (H11).
- Oblique mounting (0, 30, 75) and (30, 45, 60).

**Run/Reset.** Reset restores the exact construction, the store to 0, the shot count to 0, no payload identity and a cleared recoil.

**Save/Load.** Round-trips `capacity`, `charge_power`, pose and both connections. The replayed shot is identical.

- **Integrations.** Clock + Delay + Latch + Powered gate feeder (H38), return reload under gravity (H37) and a rebound from a wall (H26).

## 6. Open questions

1. **Muzzle speed — unspecified, owner decision.** Story 11.4 expects 12 m/s. The legacy default (45 J into the 1 kg Basketball) yields 9.48–9.50 m/s, with a 16 m/s release cap. Choose between: a default capacity of 72 J (½·1·12²); a 12 m/s target-speed cap; or keeping 45 J and amending the story.
2. **Release cap and incoming-speed rejection — unspecified, owner decision.** Keep the 16 m/s target and `SpeedLimited` rejection (H22) under the f32 envelope, or replace them?
3. **Seating values — unspecified, owner decision.** The f32 seating clearance (legacy 0.002 m) and relative-speed limit (legacy 1 m/s) must be frozen, as the requirement asks.
4. **Bowling ball eligibility — unspecified, owner decision.** The Bowling ball (radius 0.28 m, width 0.56) falls below the 0.6 m minimum width and can never seat. Is that intended? The Tennis ball (CAT-064) width is undeclared.
5. **Muzzle sweep overshoot — unspecified, owner decision.** Keep the 0.02 m distance past the half-length (H20)?
6. **Compound payloads — unspecified, owner decision.** Full compound containment (H15) needs multi-child payload bodies, which the current engine lacks (single-primitive mass properties only). Which story adds them?
7. **Energy units — unspecified, owner decision.** Confirm `capacity` in J and `charge_power` in W. The legacy observation unit was "game energy".
8. **Shot-result set — unspecified, owner decision.** Do `SpeedLimited` and `NoResponse` stay in the closed set, given that `NoResponse` needs Epic 10 joints to test?
9. **Recoil overflow policy — unspecified, owner decision.** H43 is not carried forward, so what happens to cosmetic overflow?
10. **Recipe file — unspecified, owner decision.** `docs/cannon-recipes.json` survives Epic 7, but its Playtest consumer is deleted. Retain it as the Story 11.4 recipe source or retire it?
