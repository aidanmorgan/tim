# EL-069 · Moving bucket declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-069 |
| Name | Moving bucket |
| Type | Mechanical |
| Requirement anchor | [element-069](../requirements.md#element-069); scope index [todo-192](../requirements.md#todo-192) |
| Named entry | [named-elements.md#element-069](../invest/named-elements.md#element-069); proof owner S369 |
| CAT spec refined or extended | Extends the static open container of [CAT-004 basket](CAT-004-basket.md) (current `ReceiverGeometry`) into a dynamic, rope-carried container. Related: [CAT-058 rope anchor](CAT-058-rope_anchor.md), [CAT-053 pulley](CAT-053-pulley.md), [CAT-067 weight](CAT-067-weight.md) |
| Related identities | [EL-070 moving cage](EL-070-moving-cage.md), [EL-065 load hook](EL-065-load-hook.md), [EL-063 cable winch](EL-063-cable-winch.md); [EL-014 water-carrying bucket](EL-014-water-carrying-bucket.md) (separate water identity, not folded in) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- One dynamic compound body: floor box 0.9 × 0.06 × 0.9 m and four wall boxes 0.9 × 0.7 × 0.05 m (outer 0.9 m, inner 0.8 m, wall height 0.7 m, so 0.64 m inner depth above the 0.06 m floor), open top. **Proposed**: the inner width holds one Basketball (0.68 m) with 0.06 m clearance per side, about half the static Receiver's 1.5 m width (`engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L18`).
- Bail: a cosmetic arc; its pivot is the rope attachment at (0, 0.9, 0) above the rim, so the bucket hangs upright and can tip about the bail.
- Mass 0.8 kg (empty). **Proposed**: lighter than one Basketball so cargo dominates the carried mass.

### Mass and material

Bucket material: restitution 0.2, bounce threshold 0.1 m/s, friction 0.5, rolling resistance 0. **Proposed**: a ball dropped in settles rather than bouncing out.

### Constraints and joints

None owned. The bucket hangs from a rope or cable route through its bail `Tie`; tipping is the body's own rotation about the bail point. Captured cargo is carried only by floor and wall contact, so its mass and inertia load the rope.

### Typed ports

| Socket | Domain | Direction | Local position (m) |
| --- | --- | --- | --- |
| `Tie` (bail) | Rope | Bidirectional | (0, 0.9, 0) |
| `TipTie` | Rope | Bidirectional | (0, −0.1, 0.45) lug under the rim |

Identity `Tie` exists in legacy (`engine/MachineData.cs@a6c914e:L104-L108`); `TipTie` and positions **proposed**. Attachment role `Load` for each (one end each).

### Sensors and activation

None required. A committed contents read (bodies inside the inner volume) is available to goals through the existing residence sensor pattern (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L122-L141`).

### Work and energy stores

None.

### Parameters

None. **Proposed**: fixed size.

### Cosmetic curves and UI bindings

Bucket at committed pose; bail arc follows the rope direction (presentation only).

### Art

Cream body `#fff8e9` with two ochre hoops `#d69c47`, pale grey bail `#ccd9df`, gold lug `#f7cb52`. Catalogue colour **proposed**: the Basket colour `#4aab94` (`DESIGN.md@a6c914e:L170-L170`) as a container family cue.

### Catalogue and inventory entry

Id `moving_bucket`, title "Bucket", category "Ropes"; `WorkshopPartKind.MovingBucket` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants. The element map records a required liquid mode ("Moving-container collider/contents bindings must add FluidAdvection and carried mass/inertia when its required liquid mode is admitted"). Two modes, each specified separately:

- **Solid cargo mode (now):** carries rigid bodies by contact; tipping spills them through the open top.
- **Liquid contents mode (water family):** holds a finite liquid volume below the rim. The inner volume above the 0.06 m floor is 0.8 × 0.8 × 0.64 m = 0.41 m³. Fill limit 0.375 m³, which is 6 volume steps of 2⁻⁴ m³ and weighs 6 kg at the water-family liquid density of 16 kg/m³ (both constants as proposed in [EL-014](EL-014-water-carrying-bucket.md) §2). **Proposed** fill limit: the largest whole step count that fits the 0.41 m³ inner volume (7 steps, 0.4375 m³, would overflow the rim). Liquid mass adds to the carried mass and inertia; tipping pours through the rim at the real lip height. Depends on FluidAdvection (S416 advection decision, [decisions.md#s416](../invest/decisions.md#s416)). [EL-014 water-carrying bucket](EL-014-water-carrying-bucket.md) remains its own identity with its own spec; it is not folded into this mode.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission (map); coverage JSON adds StateTransaction. Liquid mode adds FluidAdvection (map note).

**Exists now:** dynamic boxes and box–sphere/box–box contacts (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L345-L622`); static open-box container geometry pattern (`engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L18`).

**Missing**

- Compound dynamic body (floor + 4 walls) with combined mass properties: `engine/gpu/RigidMassProperties.cs@a6c914e:L26-L31` admits one primitive; no story. Decision owner S369.
- Rope domain and `Load` attachments: Story 10.2; lifting: Story 10.4.
- Liquid contents: water family (S416 → S418); no story in this programme.

**Dependencies.** A rope lifter (EL-063 Winch or CAT-053 Pulley with CAT-067 Weight); cargo (CAT-001/014 balls).

## 4. Sources and legacy

- Requirements row: "Tipping spills through the real opening; a missed object is not captured." No variants.
- Research recipe "Catch the Escape: rope lifts/tilts a bucket into a movable funnel; mistimed pours visibly spill" (`docs/component-research.md@a6c914e:L62-L62`).

No legacy bucket. Shared facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | The Receiver is five static boxes forming an open box 1.5 m wide (floor at y −0.45, walls 0.06 m half-thick, one lower front wall). | `engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L18` | carry forward (geometry pattern) | Same open-container construction, made dynamic and smaller. |
| 2 | Loads hang from a rope `Tie` with role `Load`, one end. | `parts/WeightPart.cs@a6c914e:L16-L21` | carry forward | Bail attachment. |

**Files harvested:** `parts/WeightPart.cs`; current `engine/gpu/ReceiverGeometry.cs` (survives). `parts/BasketPart.cs` checked (static basket, harvested by CAT-004). Searched for bucket, pail: only unrelated "request buckets" (`engine/SimulationLatches.cs@a6c914e:L32-L32`).

## 5. Acceptance outline

Requirement row: [element-069](../requirements.md#element-069).

- **Chrome UI recipe.** Place a Winch high, rope Winch `Tie` → Bucket `Tie`; place a Ramp so a Basketball rolls into the bucket and a Bowling ball aimed to miss it; a short rope from `TipTie` to an Anchor so lifting tips the bucket. Verify placement and routes.
- **Positive.** The Basketball lands inside and is lifted with the bucket; rope tension reflects bucket plus ball mass; when the tip rope goes taut the bucket tips and the ball spills over the rim.
- **Negative/control.** The Bowling ball that misses stays on the bench. Without the tip rope the bucket rises upright and keeps the ball.
- **Boundaries.** Ball at the rim edge; fast lift; tipping angle just before and after spill; liquid mode (when admitted) filled to 0.375 m³ (6 kg) and an overfill attempt rejected or overflowing at the rim.
- **Run/Reset and Save/Load.** Bucket, cargo and routes restore; links round-trip.
- **Integrations.** The scope index [todo-192](../requirements.md#todo-192) defines no separate integration task; shared interactions use [IX-04 tension transmission](../requirements.md#interaction-04) and [IX-01 contact impulse](../requirements.md#interaction-01), plus [IX-08 fluid advection](../requirements.md#interaction-08) in liquid mode. The [campaign coverage ledger](../requirements.md#campaign-element-coverage) reserves first use of bucket/cage in levels 11–20.

## 6. Open questions

1. Is liquid mode a separate catalogue entry or a property of the same bucket? Owner decision.
2. Should the bucket be authorable as resting on the bench (not hung)? Proposed: yes, any pose. Owner decision.
3. Fixed size (proposed) or resizable? Owner decision.
