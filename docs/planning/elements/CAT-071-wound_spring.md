# CAT-071 · wound_spring declaration readiness spec

Story 7.0 CAT-071-D readiness spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable after the Epic 7 purge. Current-engine files are cited at the same commit. This page records the declaration knowledge the legacy holds; it does not restate the requirement row. Visual authority is the [Latched wound-spring launcher](../../../DESIGN.md#latched-wound-spring-launcher) section of DESIGN.md.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID | CAT-071 |
| Kind | `wound_spring`, catalogue title "Wound spring", category "Motion" (`parts/catalog/wound_spring.tres@a6c914e:L6-L8`) |
| Requirement anchor | [CAT-071](../requirements.md#current-cat-071); retained behaviour [todo-394](../requirements.md#todo-394); DESIGN source record [todo-085](../requirements.md#todo-085) |
| Mapped identities | None. [EL-056 Ratchet](../invest/named-elements.md#element-056), [EL-063 Cable winch](../invest/named-elements.md#element-063) and [EL-194 Springboard](../invest/named-elements.md#element-194) are separate elements. Batch A confirms. |
| Roadmap story | [Story 11.4](../../../_bmad-output/planning-artifacts/epics.md) Wound Spring Potential Motor & Toy Cannon Launcher (CAT-071, CAT-016) |
| Status | Not started. `WorkshopPartKind` has no wound-spring member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

### Bodies and shapes
Positions in metres, part frame. The plunger axis is local +Y.
- **Static housing (root body).** Every legacy `AddBox` is a box collider (`reference/cpu/MachinePart.cs@a6c914e:L352-L356`). Navy base box at (0, −1, 0), size 1.55 × 0.2 × 1.35. Two cream guide rails at (±0.6, −0.05, 0), size 0.13 × 1.8 × 0.2 (`parts/WoundSpringPart.cs@a6c914e:L183-L185`).
- **Guide sleeve (collider).** A fixed open tube along local Y, centred at (0, 0.5, 0): half-length 0.9 (length 1.8), inner radius 0.41, outer radius 0.49, transparent (Opaque false), axis rotated X→Y. It holds an incoming ball above the head (`parts/WoundSpringPart.cs@a6c914e:L190-L193`; the tube proxy's second field is HalfLength, `engine/TubeProxy.cs@a6c914e:L6`). The current `ColliderShapeKind` has only Sphere, Box and Plane (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L7-L7`), so this needs the hollow tube collider of CAT-048.
- **Plunger head (internal dynamic body).** A solid sphere of radius 0.32 m. Its rest coordinate is 0.9 m along local Y; travel runs down to 0.9 − stroke (`parts/WoundSpringPart.cs@a6c914e:L41-L43`, `parts/WoundSpringPart.cs@a6c914e:L235-L250`). It is one internal body with the closed role `InternalBodyRole.Plunger` and a derived identity (`engine/MachineData.cs@a6c914e:L26-L45`).
- **Winding shaft (internal dynamic body).** A wheel of radius 0.26 m and width 0.1 m, hinged at (−0.62, −0.55, 0.5) (`parts/WoundSpringPart.cs@a6c914e:L71-L71`, `parts/WoundSpringPart.cs@a6c914e:L96-L96`, `parts/WoundSpringPart.cs@a6c914e:L195-L196`; `engine/SceneRotaryShaft.cs@a6c914e:L13-L13`).
- Current types: `RigidBodyDeclaration` and `ColliderDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`) cover the housing and head. The shaft wheel needs a cylinder or hull shape.

### Mass and material
- Head: 0.5 kg, bounce 0, drag 0 (`parts/WoundSpringPart.cs@a6c914e:L43-L43`, `parts/WoundSpringPart.cs@a6c914e:L247-L247`). Its sphere inertia fits `RigidMassProperties` (`engine/gpu/RigidMassProperties.cs@a6c914e:L17-L51`).
- Shaft: 0.25 kg (`parts/WoundSpringPart.cs@a6c914e:L71-L71`). A cylinder inertia law is missing.
- Housing, sleeve and shaft friction values are not recorded. Use `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`); see Open questions.

### Constraints and joints
- **Plunger guide.** A slider from the head to the housing along local Y, with head-to-housing collision disabled. Travel range [rest − stroke, rest]. The travel direction is Negative (inward winding only) while latched and Positive (outward only) while releasing; this is the ratchet (`parts/WoundSpringPart.cs@a6c914e:L81-L95`, `engine/physics/PhysicsWorld.cs@a6c914e:L1007-L1016`).
- **Shaft guide.** A hinge for the winding wheel (`parts/WoundSpringPart.cs@a6c914e:L96-L96`).
- **Winding transmission.** Shaft angle to guide coordinate, with ratio = `winding_lead` (m/rad). It is Engaged while latched and Open while releasing, so released energy cannot drive the input (`parts/WoundSpringPart.cs@a6c914e:L97-L98`, `engine/physics/PhysicsWorld.cs@a6c914e:L1007-L1016`).
- The mechanical input maps DriveIn to the shaft hinge with sign −1 (`parts/WoundSpringPart.cs@a6c914e:L74-L74`).

### Typed sockets and ports
- `DriveIn`: mechanical input at (−0.62, −0.55, 0.55).
- `ActivationIn`: activation input at (0.62, −0.55, 0.55) (`parts/WoundSpringPart.cs@a6c914e:L109-L113`).
- No electrical port. Supply reaches the spring only as shaft work from a Motor.
- The current `WorkshopConnectionDomain` has only Activation and Electrical (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`). A Mechanical domain and a `DriveIn` socket are missing.

### Sensors and activation
- `ActivationIn` accepts only the Trigger command. Repeated triggers coalesce into one request (`_triggerTick ??=`), which is applied at the start of the next tick (`parts/WoundSpringPart.cs@a6c914e:L126-L144`).
- **Trigger result.** `Released` if charged; `Empty` if not charged, which changes nothing, so the request expires; `AlreadyReleased` while releasing (`engine/physics/PhysicsWorld.cs@a6c914e:L988-L1005`).
- **Rearm.** The spring re-latches automatically when compression returns within the stop tolerance (`engine/physics/PhysicsWorld.cs@a6c914e:L1018-L1031`). The stop tolerance is the world position tolerance (`engine/physics/PhysicsWorld.cs@a6c914e:L370-L370`).
- **Observable phase.** A closed enum `Idle | Winding | Armed | Releasing | Blocked`, derived from the spring state each tick (`parts/WoundSpringPart.cs@a6c914e:L11-L11`, `parts/WoundSpringPart.cs@a6c914e:L145-L157`).
- The current `ActivationNetwork` (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`) delivers latched activation edges; it has no trigger-consumer node yet.

### Work and energy stores
- **Elastic store.** Stored energy is 0.5·k·q², where q is the compression (`engine/physics/PhysicsLatchedSpring.cs@a6c914e:L82-L87`). Full charge at the defaults is 0.5 × 160 × 0.8² = 51.2 J (`CuriousContraptions.tests/WoundSpringCampaignTests.cs@a6c914e:L49-L50`).
- **Accepted work.** It grows only by positive energy change while latched. **Released work** grows by the energy decrease while releasing (`engine/physics/PhysicsLatchedSpring.cs@a6c914e:L88-L100`).
- The law is pure potential with no damping term (`engine/physics/AxialElasticPotential.cs@a6c914e:L5-L41`).
- Current precedent: `Joules` and the finite reservoir in `ContactWorkDeclaration` (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L5`, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L22-L37`, bounded 0–200 J). A latched elastic store is a new declaration.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `stiffness` | f32 | 40–240 | 160 | N/m | `parts/WoundSpringPart.cs@a6c914e:L114-L125`, `parts/catalog/wound_spring.tres@a6c914e:L12-L12` |
| `stroke` | f32 | 0.2–0.8 | 0.8 | m | same |
| `winding_lead` | f32 | 0.025–0.2 | 0.1 | m/rad | same |

The parameter names are a closed enum `WoundSpringParameter { Stiffness, Stroke, WindingLead }` (`parts/WoundSpringPart.cs@a6c914e:L10-L10`).

### Cosmetic curves and UI bindings
- **Latch.** A gold latch rotates about Z toward −0.65 rad at a bounded 8 rad/s (linear, once) while releasing, and returns after rearm. It is presentation-clocked (`parts/WoundSpringPart.cs@a6c914e:L48-L56`). At 0.01 s the angle reads −0.08 rad (`CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L503-L538`).
- **Coil and charge index.** The silver helix (four turns, radius 0.22, height 1.38) and the gold charge index follow the committed plunger pose through axis-affine maps (`parts/WoundSpringPart.cs@a6c914e:L170-L179`, `parts/WoundSpringPart.cs@a6c914e:L210-L233`).
- **Marks.** Five navy compression marks start 0.1 below the rest and are spaced by stroke/4 (`parts/WoundSpringPart.cs@a6c914e:L186-L188`). DESIGN: the marks show compression, not a linear energy percentage.
- Pick radius 1.25 (`parts/WoundSpringPart.cs@a6c914e:L182-L182`). The current `CosmeticCurveDeclaration` (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L7-L16`) has no latch-state or pose-map source; both are new bindings.

### Art
- Scene `parts/scenes/wound_spring.tscn` is a bare `Node3D` with the script (`parts/scenes/wound_spring.tscn@a6c914e:L1-L4`); all geometry is procedural.
- Palette (`parts/WoundSpringPart.cs@a6c914e:L183-L202`, `parts/WoundSpringPart.cs@a6c914e:L248-L248`): navy base and marks `#293954`, cream rails, head and pulley `#fff8e9`, gold charge marker, pulley spoke and latch `#f7cb52`, cyan sleeve (0.4, 0.72, 0.79, alpha 0.18), silver coil `#ccd9df`, gold socket sphere at the ActivationIn position. Catalogue colour (0.4, 0.72, 0.79) (`parts/catalog/wound_spring.tres@a6c914e:L11-L11`).
- Latch: pivot node at (0.48, 0.25, 0.5) carrying a gold box 0.22 × 0.1 × 0.1 (`parts/WoundSpringPart.cs@a6c914e:L200-L201`).
- Charge marker: gold box 0.19 × 0.045 × 0.035 (`parts/WoundSpringPart.cs@a6c914e:L189`), placed by an axis map at (0.6, head coordinate − 0.1, 0.14), which is (0.6, rest − 0.1, 0.14) at rest (`parts/WoundSpringPart.cs@a6c914e:L176-L178`).
- Coil: silver mesh based at (0, −0.8, 0) (`parts/WoundSpringPart.cs@a6c914e:L194`); helix radius 0.22, height 1.38, four turns, tube radius 0.025 (`parts/WoundSpringPart.cs@a6c914e:L210-L233`).

### Catalogue and inventory entry
- Description: "A mechanical belt winds a finite spring. Its latch holds charge after power stops; a separate trigger releases the guided plunger. An empty spring cannot launch." (`parts/catalog/wound_spring.tres@a6c914e:L9-L9`).
- Inventory: one wound spring in `wind_then_release` (`content/puzzles.json@a6c914e:L31312-L31314`). It is placed and locked in `saved_for_later` (`content/puzzles.json@a6c914e:L32312-L32374`). Add `WorkshopPartKind.WoundSpring` and a counted `PartAllowance` (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

## 3. Engine capabilities

Capability families: the [CAT-071 row](../general-engine-element-map.md) and its binding in [catalogue-elements.json](../../coverage/catalogue-elements.json). They are not repeated here.

**Exists now**
- Static housing and dynamic sphere head: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`, `engine/gpu/RigidMassProperties.cs@a6c914e:L17-L51`.
- Head-to-payload contact through the TGS Soft solver: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L774-L791`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L878-L884`.
- Activation wiring to an `ActivationIn` socket, with switch and delay sources: `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`, `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14`.
- A finite `Joules` quantity: `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L5`.

**Missing**
- Prismatic slider and soft spring constraint: **Story 6.4**.
- Finite preload/elastic store: **Story 6.5**.
- Battery supply: **Story 8.1**.
- Shaft body, hinge, Mechanical connection domain, `DriveIn`/`Drive` sockets and motor shaft torque: **Story 11.1**.
- Ratio transmission with Engaged/Open engagement: **Story 11.2** (clutch and reverser coupling).
- One-way travel direction (ratchet), latch/trigger/rearm state, `Released`/`Empty`/`AlreadyReleased` results and the phase read: **Story 11.4**.
- Hollow guide sleeve collider: **Stories 6.6/6.7** (CAT-048).
- Cylinder shaft collider and inertia: Story 10.1 (ENGINE-CYLINDER).

**Element dependencies.** CAT-042 Motor and CAT-005 Battery supply winding. CAT-063 Switch and CAT-022 Delay trigger. CAT-001 Basketball is the payload; CAT-004 Receiver is the goal. CAT-033 Hold timer is used in `saved_for_later`. CAT-051 Powered gate is an obstruction control. CAT-066 Wall is a winding/obstruction stop.

## 4. Legacy harvest

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Stiffness 40–240 (160), stroke 0.2–0.8 (0.8), winding lead 0.025–0.2 m/rad (0.1); NaN rejects | `parts/WoundSpringPart.cs@a6c914e:L114-L125`, `parts/catalog/wound_spring.tres@a6c914e:L12-L12` | carry forward | Requirement names all three modes |
| 2 | Head radius 0.32 m, mass 0.5 kg, rest coordinate 0.9 m, bounce 0, drag 0 | `parts/WoundSpringPart.cs@a6c914e:L41-L43`, `parts/WoundSpringPart.cs@a6c914e:L245-L249` | carry forward | Finite guided plunger |
| 3 | Housing, rails, sleeve tube (half-length 0.9, length 1.8, inner 0.41, outer 0.49, transparent, axis rotated X→Y) and shaft wheel (0.25 kg, r 0.26, w 0.1) | `parts/WoundSpringPart.cs@a6c914e:L71-L71`, `parts/WoundSpringPart.cs@a6c914e:L180-L209` | carry forward | Functional geometry |
| 4 | Slider range [rest − stroke, rest]; Negative direction latched, Positive released; collision with housing disabled | `parts/WoundSpringPart.cs@a6c914e:L81-L95`, `CuriousContraptions.tests/ScenePlungerBindingTests.cs@a6c914e:L35-L77` | carry forward | Ratchet: inward winding only |
| 5 | Winding transmission ratio = lead, engaged while latched, open while released; DriveIn sign −1 | `parts/WoundSpringPart.cs@a6c914e:L74-L74`, `parts/WoundSpringPart.cs@a6c914e:L97-L98`, `engine/physics/PhysicsWorld.cs@a6c914e:L1007-L1016` | carry forward | Release disconnects winding |
| 6 | Released guide moves only outward and stops at rest; the latch restores | `CuriousContraptions.tests/ScenePlungerBindingTests.cs@a6c914e:L80-L117` | carry forward | Physical end and rearm |
| 7 | The ratchet stops a reverse impulse without advancing the lower bound | `CuriousContraptions.tests/ScenePlungerBindingTests.cs@a6c914e:L119-L143` | carry forward | Reverse freewheels; no false charge |
| 8 | A payload collision can wind the plunger through shared contacts and the shaft; kinetic energy never grows | `CuriousContraptions.tests/ScenePlungerBindingTests.cs@a6c914e:L146-L181` | carry forward | Contact recharge is legitimate but separately accounted |
| 9 | Energy 0.5·k·q²; accepted work counts only positive change while latched; released work counts the decrease while releasing | `engine/physics/PhysicsLatchedSpring.cs@a6c914e:L82-L100` | carry forward (ledger) | Requirement demands the separate accounting |
| 10 | Trigger results Released, Empty and AlreadyReleased; Empty changes nothing | `engine/physics/PhysicsWorld.cs@a6c914e:L988-L1005`, `CuriousContraptions.tests/PhysicsLatchedSpringTests.cs@a6c914e:L33-L76` | carry forward | Empty request expires |
| 11 | Re-latch when compression is within the stop tolerance (world position tolerance) | `engine/physics/PhysicsWorld.cs@a6c914e:L370-L370`, `engine/physics/PhysicsWorld.cs@a6c914e:L1018-L1031` | carry forward (rule) / do not carry forward (tolerance value) | The tolerance must be frozen under f32 |
| 12 | A trigger is coalesced and applied at the next tick; a failed tick restores or discards the pending trigger | `parts/WoundSpringPart.cs@a6c914e:L126-L144`, `CuriousContraptions.tests/MechanicalRuntimeCheckpointTests.cs@a6c914e:L78-L114` | carry forward; deferred to the injected-fault gate | The next-tick rule applies now; failed-tick rollback is proved at the injected-fault gate |
| 13 | Phases Idle/Winding/Armed/Releasing/Blocked; Blocked uses solver reaction observations (`OpposesWinding`) | `parts/WoundSpringPart.cs@a6c914e:L145-L157`, `engine/physics/PhysicsLatchedSpring.cs@a6c914e:L51-L79` | carry forward (phases) / do not carry forward (reaction certificate) | Proof-grade effort-resolution observation |
| 14 | Phase and trigger wire names are canonical lower-case; alternates and numbers reject; frame values compression 0.4 → 12.8 J | `CuriousContraptions.tests/WoundSpringDiagnosticTests.cs@a6c914e:L9-L103` | carry forward (typed enum sets and reject-unknown rule) / do not carry forward (Playtest wire strings) | The Playtest frame boundary is deleted with `tools/Playtest`; any new serialization boundary keeps typed enums and rejects unknown values (same rule as CAT-016 H29) |
| 15 | Supplied winding for 240 ticks stores 50–52 J; trigger lifts the ball > 1 m. Unsupplied: 0 J, no release | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L37-L75` | carry forward | Positive and negative |
| 16 | After power loss the charge is retained; release energy ≤ stored × 1.02 + 0.01; rise ≤ stored/(m·g) + 0.1 for masses 0.5, 1 and 4; a second trigger is Empty | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L76-L119` | carry forward | Stopped drive retains charge; no recharge |
| 17 | Empty release (no payload) spends 50–52 J and ends Idle with the head at rest | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L121-L142` | carry forward | Charge is spent on the internal mass |
| 18 | A late wall obstruction gives Blocked and retains 25–51.3 J indefinitely | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L144-L169` | carry forward | Blocked control |
| 19 | Rotated partial charges and repeated cycles restore exactly | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L171-L250` | carry forward | Rotated mounting and Reset |
| 20 | Same-tick power-off and release: later recharge is bounded by the coasting kinetic energy | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L253-L289` | carry forward | No energy creation |
| 21 | Rotated loaded release accounts for all mechanical energy; released 51.19–51.21 J | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L291-L392` | carry forward | Energy ledger |
| 22 | An impact switch triggers a partial charge (1–50 J); released = charge; ball peak 4.6–7 | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L394-L426` | carry forward | Partial-charge trigger |
| 23 | A wall at the housing limits winding to compression 0.455 m, 16.56 J; accepted = stored; no further acceptance | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L428-L455`, `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L318-L340` | carry forward | Clearance caps accepted travel |
| 24 | A powered gate closed after winding blocks the release at 37 J; reopening completes it, ending Idle | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L457-L501` | carry forward | Obstruction and resumption |
| 25 | Resting payload contact events ≤ 64 per step | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L542-L567` | do not carry forward | Legacy CPU event budget; performance is a later gate |
| 26 | Off-centre (+0.05), oversize radius 0.6, catalogue weight (r ≈ 0.508) and stacked loads stay finite, inside the sleeve, within energy × 1.02 | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L569-L668` | carry forward | Oversize/off-centre controls |
| 27 | An internal body ID collision rejects atomically; unknown roles reject; removing the part removes the head | `CuriousContraptions.tests/WoundSpringTests.cs@a6c914e:L670-L746` | carry forward | Atomic construction |
| 28 | Gravity alone can wind the passive latch without electrical work; zero gravity cannot | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L12-L52` | carry forward | Gravitational recharge is legitimate |
| 29 | Winding stays within the accepted-work budget × 1.02 + 0.01; unsupplied gives Empty | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L97-L144` | carry forward | Paid motor work bound |
| 30 | Power loss holds the coordinate within 1e-7; release adds no supplied work; head at rest after | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L146-L201` | carry forward (behaviour) | Tolerance values are legacy double; re-freeze for f32 |
| 31 | The released head launches the payload > 0.5 m via contacts; gravity rewind after relatch ≤ 2(m_head·g)²/k | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L228-L316` | carry forward | Bound on passive recharge |
| 32 | Presentation frames cannot move the head or alter energy; the coil follows committed poses | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L203-L226`, `CuriousContraptions.tests/ExplicitPoseReferenceTests.cs@a6c914e:L84-L129` | carry forward | One-way data flow |
| 33 | A raw world owns winding and release; rotated springs use the declared stops | `CuriousContraptions.tests/WoundSpringOwnershipTests.cs@a6c914e:L13-L102` | carry forward | No observer dependency |
| 34 | The motor→spring belt keeps total energy ≤ motor supplied work + gravity drop; power off adds no work | `CuriousContraptions.tests/MechanicalWorkTests.cs@a6c914e:L39-L96` | carry forward | Paid work accounting |
| 35 | Lesson `wind_then_release`: spring at (0,3,0) rotated −15° about Z; battery→motor (electrical), motor→spring (mechanical), switch→delay→spring (activation); ball at (0.4063459, 4.5165035, 0); basket at (3.6, 1.2, 0) | `tools/Campaign/WoundSpringLesson.cs@a6c914e:L56-L83`, `content/puzzles.json@a6c914e:L31306-L31860` | carry forward | Introductory fixture |
| 36 | Lesson `saved_for_later`: hold timer between battery and motor; 3 s release delay; the spring is locked in place | `tools/Campaign/WoundSpringLesson.cs@a6c914e:L84-L108`, `content/puzzles.json@a6c914e:L31861-L32564` | carry forward | Retained-charge fixture |
| 37 | Circuits: Complete wins with released 51.19–51.21 J; NoSupply and NoBelt accept 0; NoRelease holds 51.2 J. The stored lesson holds ≥ 30 unpowered ticks before release | `CuriousContraptions.tests/WoundSpringCampaignTests.cs@a6c914e:L14-L125` | carry forward | Lesson controls |
| 38 | UI fixture: without supply the resting payload still passively charges and releases once, but its peak is < 5.1; with supply the peak is > 6.5 | `CuriousContraptions.tests/WoundSpringBrowserWorkloadTests.cs@a6c914e:L22-L69` | carry forward | Gravity charge must not count as motor work |
| 39 | `ObservePhysics`, `BeforeNetworks` and `_Process` per-part callbacks | `parts/WoundSpringPart.cs@a6c914e:L137-L163` | do not carry forward | Per-element update loop |

### Files harvested
- `parts/WoundSpringPart.cs`, `parts/catalog/wound_spring.tres`, `parts/scenes/wound_spring.tscn`
- `engine/SceneLatchedSpringDeclaration.cs`, `engine/physics/PhysicsLatchedSpring.cs`, `engine/physics/AxialElasticLoad.cs`, `engine/physics/AxialElasticPotential.cs`, `engine/physics/PhysicsWorld.cs` (L356-L376, L988-L1031), `engine/SceneRotaryShaft.cs`, `engine/SceneTransmissionJoint.cs`, `engine/MachineData.cs` (`InternalBodyRole`)
- `engine/SpringParameter.cs` (checked, no element knowledge: springboard schema, CAT-062)
- `reference/cpu/MachinePart.cs` (`AddBox` collider semantics)
- `engine/TubeProxy.cs` (tube proxy field order only)
- `CuriousContraptions.tests/WoundSpringTests.cs`, `WoundSpringRuntimeTests.cs`, `WoundSpringCampaignTests.cs`, `WoundSpringDiagnosticTests.cs`, `WoundSpringOwnershipTests.cs`, `WoundSpringBrowserWorkloadTests.cs`, `PhysicsLatchedSpringTests.cs`, `ScenePlungerBindingTests.cs`, `MechanicalRuntimeCheckpointTests.cs`, `MechanicalWorkTests.cs`, `ExplicitPoseReferenceTests.cs`
- `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs`, `PreparedConfigurationTests.cs`, `BodyLocalBoxTests.cs`, `ConstructionLifecycleTests.cs`, `SceneBodyDynamicsTests.cs`, `BodyDiagnosticTests.cs` (checked, no element knowledge beyond rows 27 and 32: generic internal-body checkpoint and body-frame fixtures)
- `CuriousContraptions.tests/WoundSpringTests.cs.orig` (checked, no element knowledge: tracked merge leftover of `WoundSpringTests.cs`)
- `tools/Campaign/WoundSpringLesson.cs`, `tools/Campaign/Program.cs`
- `content/puzzles.json` (`wind_then_release`, `saved_for_later`)
- `reference/p054-guide/puzzles-candidate.json` (checked, no element knowledge: duplicate lesson levels)
- `tools/P0-007-actual-path-probe/inputs.json` (checked, no element knowledge: file list only)

## 5. Acceptance outline

Acceptance criteria are those of the [CAT-071 row](../requirements.md#current-cat-071), [todo-394](../requirements.md#todo-394) and Story 11.4.
- **Chrome UI recipe.** Open `wind_then_release`. Place the wound spring from the toolbox at (0, 3, 0) and rotate it −15° about Z. Wire battery Supply → motor PowerIn, motor Drive → spring DriveIn, switch ActivationOut → delay ActivationIn, and delay ActivationOut → spring ActivationIn with the contextual wiring UI. Run.
- **Positive.** The motor winds the spring to 51.2 J. The striker hits the switch; after the delay the latch releases and the ball is captured. Accepted motor work is reported separately from passive recharge.
- **Negative/control.** NoSupply and NoBelt accept no motor work. NoRelease retains 51.2 J. A trigger on an empty spring expires with no motion. A repeated trigger while releasing changes nothing.
- **Boundaries.** Parameter minimum and maximum values, with values outside rejected atomically. A wall clearance caps winding (0.455 m, 16.56 J at the defaults). Blocked release retains charge and resumes when cleared. Oversize, off-centre and stacked payloads.
- **Run/Reset.** Reset at any phase restores the uncharged latch, the head at rest coordinate 0.9 and the latch angle 0.
- **Save/Load.** Position, rotation, the three parameters and the DriveIn/ActivationIn connections round-trip through `WorkshopSaveCodec` (`engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`).
- **Integrations.** `saved_for_later` with Hold timer (Story 9.5). Powered gate obstruction (Story 8.2). Impact-switch partial charge.

## 6. Open questions

1. **Recharge ledger.** Legacy `AcceptedWork` counts motor, contact and gravity recharge alike (rows 8, 28, 38). The requirement asks for separate ledgers. The ledger split is unspecified — owner decision.
2. **Stop tolerance.** The charged/empty threshold is unspecified under f32 (the legacy used the world position tolerance). Owner decision.
3. **Blocked definition.** Define a game-grade Blocked rule without solver reaction observations (row 13).
4. **Drive sign.** Confirm the DriveIn sign convention (−1) and which motor rotation winds the spring.
5. **Materials.** Housing, sleeve and shaft friction is unrecorded. Rolling resistance is 0, the current value for non-sphere bodies (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L721-L726`).
6. **Story order.** Story 11.4 depends on the Story 11.2 transmission and the Story 6.6 tube collider. Confirm that order holds.
7. **Damping.** The spring has no damping parameter. Confirm that a pure-potential law is intended.
8. **Shaft collider.** Cylinder collider and cylinder inertia for the shaft wheel: Story 10.1 (ENGINE-CYLINDER), scheduled before the first shaft wheel by the owner on 9 Oct 2026.
