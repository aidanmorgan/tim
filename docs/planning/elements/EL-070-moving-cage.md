# EL-070 · Moving cage declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-070 |
| Name | Moving cage |
| Type | Mechanical |
| Requirement anchor | [element-070](../requirements.md#element-070); scope index [todo-192](../requirements.md#todo-192) |
| Named entry | [named-elements.md#element-070](../invest/named-elements.md#element-070); proof owner S370 |
| CAT spec refined or extended | none. Related: [CAT-004 basket](CAT-004-basket.md) (container geometry), [CAT-058 rope anchor](CAT-058-rope_anchor.md) and [CAT-053 pulley](CAT-053-pulley.md) (rope carriage), [CAT-034 impact lever](CAT-034-impact_lever.md) (hinged member pattern) |
| Related identities | [EL-069 moving bucket](EL-069-moving-bucket.md), [EL-065 load hook](EL-065-load-hook.md), [EL-063 cable winch](EL-063-cable-winch.md); character elements EL-082..088 (Batch L) as likely cargo |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Cage frame: one dynamic compound body, outer 1.2 × 1.0 × 1.2 m: floor box 1.2 × 0.06 × 1.2 m, roof box 1.2 × 0.06 × 1.2 m, four corner posts 0.06 × 1.0 × 0.06 m, and three vertical bars 0.05 × 0.88 × 0.05 m on each of three sides at 0.3 m centres (9 bars; gaps 0.25 m between bars and about 0.22 m between a bar and a post). **Proposed**: every gap is narrower than the smallest current ball (Bowling ball, 0.56 m diameter), so nothing passes between bars.
- Door: a separate dynamic body on the fourth side, a barred panel 1.04 × 0.86 × 0.05 m, 0.4 kg, hinged on one vertical edge. **Proposed**: fits inside the 1.08 m clear gap between the corner posts (1.2 − 2 × 0.06) and the 0.88 m between floor and roof, with 0.02 m clearance each side.
- Collider count: 15 for the frame (floor, roof, 4 posts, 9 bars) plus 1 for the door = 16.
- Frame mass 2.5 kg. **Proposed**: comparable to a mid Weight so loaded and empty lifts differ visibly.

### Mass and material

Bars and floor: restitution 0.2, bounce threshold 0.1 m/s, friction 0.4, rolling resistance 0. **Proposed**.

### Constraints and joints

- Door hinge to the frame, travel [0°, 100°] (0° closed). **Proposed**: opens past square so a ball can roll out, stops before striking the side bars.
- `door_state` Closed: a latch row holds the door at 0° (rigid stop, holding torque 50 N·m, **proposed**). Open: no latch; the door swings freely within its travel.
- `ActivationIn` toggles the latch once per run (close an open door by releasing a gravity-closed door, or unlatch a closed door). **Proposed** (Open question 1).

### Typed ports

| Socket | Domain | Direction | Local position (m) |
| --- | --- | --- | --- |
| `Tie` (top eye) | Rope | Bidirectional | (0, 0.6, 0) |
| `ActivationIn` | Activation | Input | (0.6, 0.4, 0.6) frame corner |

Identities `engine/MachineData.cs@a6c914e:L104-L108`; positions **proposed**.

### Sensors and activation

Committed door angle and latch state are reads for cosmetics; contents through a residence sensor (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L122-L141`).

### Work and energy stores

None.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `door_state` | enum `CageDoorState { Closed, Open }` | — | Closed | — | **Proposed**: "declared closable opening" |

### Cosmetic curves and UI bindings

Frame and door at committed poses; a gold latch tab shows Closed versus Open by shape (raised vs dropped).

### Art

Pale grey bars `#ccd9df`, navy floor and roof `#293954`, cream posts `#fff8e9`, gold latch `#f7cb52`. Catalogue colour **proposed**: pale grey.

### Catalogue and inventory entry

Id `moving_cage`, title "Cage", category "Ropes"; `WorkshopPartKind.MovingCage` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. `door_state` is an authored parameter of one element; both values have acceptance below.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission (map); coverage JSON adds StateTransaction.

**Exists now:** dynamic boxes and contacts (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L345-L622`); activation input pattern (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`).

**Missing**

- Compound dynamic body (15 frame boxes): `engine/gpu/RigidMassProperties.cs@a6c914e:L26-L31` admits one primitive; one cage uses 16 of the scene's 64 colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L166-L172`), about a quarter (Open question 2). No story; decision owner S370.
- Hinge with limits and a latch row: Story 10.3 plus a latch (no story).
- Rope domain and lifting: Stories 10.2/10.4.

**Dependencies.** A rope lifter (EL-063 or CAT-053 + CAT-067); cargo (CAT-001/014 balls).

## 4. Sources and legacy

- Requirements row: "An open door allows escape through actual geometry." No variants.

No legacy cage, door or latch-on-hinge exists. Searched `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: cage, door, bars, enclosure). Shared facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Hinge travel bounds must be finite, ordered and strictly inside (−π, π). | `engine/physics/PhysicsJoint.cs@a6c914e:L104-L113`, `engine/physics/PhysicsJoint.cs@a6c914e:L137-L139` | carry forward | Door travel admission. |
| 2 | A hanging load uses one rope `Tie` with role `Load`. | `parts/WeightPart.cs@a6c914e:L16-L21` | carry forward | Cage suspension. |

**Files harvested:** `engine/physics/PhysicsJoint.cs`, `parts/WeightPart.cs`.

## 5. Acceptance outline

Requirement row: [element-070](../requirements.md#element-070).

- **Chrome UI recipe.** Place a Winch high and a Cage below it with a Basketball inside; rope Winch `Tie` → Cage `Tie`; set `door_state` Closed. Verify placement, door state and route.
- **Positive.** Run: the cage lifts with the ball inside; the ball cannot leave between bars or through the latched door.
- **Negative/control.** `door_state` Open, cage tilted or swung: the ball rolls out through the door opening. A ball placed outside the cage is not lifted.
- **Boundaries.** Ball pressed against bars at speed (no tunnelling, 0.05 m bars); door swung to its 100° limit; latch torque just below and above the ball's push; activation toggles once.
- **Run/Reset and Save/Load.** Cage, door angle, latch and ball restore; `door_state` round-trips.
- **Integrations.** The scope index [todo-192](../requirements.md#todo-192) defines no separate integration task; shared interactions use [IX-04 tension transmission](../requirements.md#interaction-04), [IX-01 contact impulse](../requirements.md#interaction-01) and [IX-03 joint constraint](../requirements.md#interaction-03) (door hinge and latch, released only through lifecycle state). The [campaign coverage ledger](../requirements.md#campaign-element-coverage) reserves first use of bucket/cage in levels 11–20.

## 6. Open questions

1. Is the door state only authored (build time), or also commanded during a run by `ActivationIn`? Owner decision.
2. Collider budget: 16 colliders per cage against 64 per scene. Accept, raise the budget, or use fewer, thicker bars? Owner decision.
3. Should the cage also stand on the bench as a static trap (no rope)? Proposed: yes, any pose. Owner decision.
