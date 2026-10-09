# EL-062 · Docking ferry declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-062 |
| Name | Docking ferry |
| Type | Mechanical |
| Requirement anchor | [element-062](../requirements.md#element-062); scope index [todo-320](../requirements.md#todo-320) |
| Named entry | [named-elements.md#element-062](../invest/named-elements.md#element-062); proof owner S372 |
| CAT spec refined or extended | none. Related: [CAT-058 rope anchor](CAT-058-rope_anchor.md) and [CAT-053 pulley](CAT-053-pulley.md) (rope law, Story 10.2), [CAT-067 weight](CAT-067-weight.md) (pulling load), [CAT-051 powered gate](CAT-051-powered_gate.md) (loader gated by the interlock) |
| Related identities | [EL-063 cable winch](EL-063-cable-winch.md) (drive), [EL-055 brake](EL-055-mechanical-brake.md) (carrier mode), [EL-061 indexed carousel](EL-061-indexed-carousel.md); todo-429 docking lift (requirements row, separate) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Static rail: box `rail_length` × 0.12 × 0.3 m on two cream end posts (docks) of 0.3 × 0.8 × 0.6 m. **Proposed**.
- Dynamic carriage (ferry deck): box 1.4 × 0.12 × 1.4 m with a 0.15 m lip on all four sides, mass 2 kg. **Proposed**: holds one Basketball (0.68 m) with clearance; the lip is real collision geometry so cargo is carried by support, not attachment.

### Mass and material

Deck material: restitution 0.1, bounce threshold 0.1 m/s, friction 0.5, rolling resistance 0. **Proposed**: cargo settles quickly on the deck.

### Constraints and joints

- Carriage slider along the rail with travel [0, `rail_length` − 1.4] m and end stops at each dock.
- No cargo joint: cargo rides on deck contact (support) only.

### Typed ports

| Socket | Domain | Direction | Body | Local position (m) |
| --- | --- | --- | --- | --- |
| `Tie` (A end) | Rope | Bidirectional | carriage | (−0.7, 0.1, 0) |
| `Tie` (B end) | Rope | Bidirectional | carriage | (0.7, 0.1, 0) |
| `DockedAOut` | Activation | Output | rail | dock A post top |
| `DockedBOut` | Activation | Output | rail | dock B post top |

Rope socket identity: `engine/MachineData.cs@a6c914e:L104-L108`; two distinct tie sockets and the two dock outputs need new enum members (**proposed**). The legacy weight is a single-end `Load` (`parts/WeightPart.cs@a6c914e:L16-L21`); a carriage with two ends is a new attachment role (Open question 1).

### Sensors and activation

- Docked at A (or B): carriage within 0.02 m of that end stop and slower than 0.05 m/s. **Proposed** tolerance (game-grade resting threshold).
- Each dock output emits an occurrence on arrival; the interlock is informational: it may gate a loader (for example a Powered gate) but it never supports or moves cargo.

### Work and energy stores

None. Motion work comes only through rope tension from a winch or a falling weight.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `rail_length` | f32 | 2.5–8 | 4 | m | **Proposed**: 8 m matches the wall maximum width (`engine/gpu/WorkshopWall.cs@a6c914e:L8-L10`) |

Closed parameter enum `FerryParameter { RailLength }`.

### Cosmetic curves and UI bindings

Deck and cargo at committed poses; each dock lamp slate when empty, gold when docked.

### Art

Cream deck `#fff8e9` with ochre lip `#d69c47`, navy rail `#293954`, pale grey wheels `#ccd9df` (cosmetic), gold dock lamps `#f7cb52`. Catalogue colour **proposed**: ochre.

### Catalogue and inventory entry

Id `docking_ferry`, title "Ferry", category "Ropes"; `WorkshopPartKind.DockingFerry` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. One element is specified.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission (map); coverage JSON adds StateTransaction.

**Exists now:** box bodies, contact, friction and support (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L766`); residence-style box sensor against one body (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L122-L141`); activation outputs (`engine/gpu/ActivationNetwork.cs@a6c914e:L7-L12`).

**Missing**

- Compound dynamic body (deck + lip): one primitive per dynamic body today (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L31`); no story.
- Slider with end stops: Story 6.4.
- Rope domain, `Tie`, tension-only route: Story 10.2; counterweight lifting: Story 10.4.
- Residence sensor relative to a moving body's travel (dock state from joint coordinate): no story.

**Dependencies.** A rope drive: EL-063 Winch, or CAT-067 Weight over CAT-053 Pulley (10.2/10.4).

## 4. Sources and legacy

- Requirements row: "Undocked loading interlock does not bypass actual support." No variants.
- todo-320 integration: "cargo can transfer only when the carrier is actually docked".
- todo-429 docking lift (separate P2 row): "Cargo stays physically supported through travel … test edge cargo, acceleration, blocked travel, power loss and unloading between docks" ([requirements.md#todo-429](../requirements.md#todo-429)) — the same support rule, recorded for consistency.

No legacy implementation. Shared rope facts (route completeness, slack carries no tension, branch rejection) are harvested in [CAT-058](CAT-058-rope_anchor.md) R4–R17 and apply unchanged:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Loads and anchors accept one rope end; guides two; branching rejected. | `engine/RopeNetwork.cs@a6c914e:L69-L78` | carry forward; extend with a two-end carriage role | A ferry needs a rope at each end. |
| 2 | Rope supplies tension only; inactive while slack. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | carry forward (law) | Slack rope cannot push the ferry. |

**Files harvested:** `engine/RopeNetwork.cs`, `engine/physics/PhysicsRopeJoint.cs`, `parts/WeightPart.cs`. Searched with no hit: `parts/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (terms: ferry, dock, carriage, shuttle).

## 5. Acceptance outline

Requirement row: [element-062](../requirements.md#element-062).

- **Chrome UI recipe.** Place a Ferry, a Pulley beyond dock B and a Weight hanging from it; rope Ferry `Tie` (B end) → Pulley → Weight; place a Basketball on the deck at dock A, a Basket below dock B's far side, and a Lamp linked to `DockedBOut`. Verify placement, rail length and the rope route.
- **Positive.** Run: the Weight pulls the ferry to dock B carrying the ball on its deck; `DockedBOut` lights the Lamp on arrival.
- **Negative/control.** A ball dropped where the deck would be while the ferry is away from that dock falls to the bench (the interlock or any signal does not support it). No rope: the ferry stays at A and no dock-B occurrence.
- **Boundaries.** Ferry stopped 0.03 m short of the dock (not docked); ball near the lip edge during acceleration; rail length 2.5 and 8; slack rope; out-of-range rejected.
- **Run/Reset and Save/Load.** Carriage pose, cargo and dock states restore; rail length and rope links round-trip.
- **Integrations.** Cross-element task [sequence-task-371](../requirements.md#sequence-task-371) under [todo-320](../requirements.md#todo-320): "cargo can transfer only when the carrier is actually docked".

## 6. Open questions

1. Rope attachment role for a two-ended carrier (new `Carrier` role accepting two ends) or two separate load sockets? Owner decision.
2. Shaft drive alternative (a `DriveIn` on the rail with a belt) in addition to ropes? Owner decision.
3. Should the ferry move on liquid (a water-family boat) in a later mode? Not in the row. Owner decision.
