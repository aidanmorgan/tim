# EL-067 · Steel cable declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-067 |
| Name | Steel cable |
| Type | Mechanical (connector) |
| Requirement anchor | [element-067](../requirements.md#element-067); scope index [todo-210](../requirements.md#todo-210) (retained research reference: the TIM2 manual) |
| Named entry | [named-elements.md#element-067](../invest/named-elements.md#element-067); proof owner S336 |
| CAT spec refined or extended | Extends the rope connector law carried in [CAT-058 rope anchor](CAT-058-rope_anchor.md) (EL-205 rope facts R3–R22) with a second connector material. Related: [CAT-053 pulley](CAT-053-pulley.md), [CAT-067 weight](CAT-067-weight.md) |
| Related identities | EL-205 rope (Batch J), [EL-068 tin snips](EL-068-tin-snips.md) (can cut it), [EL-066 scissors](EL-066-scissors.md) (cannot, at default work), [EL-063 cable winch](EL-063-cable-winch.md), [EL-064 metal loop anchor](EL-064-metal-loop-anchor.md) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

None: a connection, not a body. Like the legacy rope it is massless and has no collision geometry (CAT-058 R14 and Open question 3). Visual radius 0.03 m (**proposed**: thinner than the warm-wood rope so the two connectors read apart).

### Mass and material

Connector material record, new enum `ConnectorMaterial { Rope, SteelCable }` (**proposed**), each declaring:

| Property | Rope (EL-205) | Steel cable | Unit | Source |
| --- | --- | --- | --- | --- |
| Tension-row natural frequency | owned by CAT-058 Open question 2 | 30 | Hz | **Proposed**: below the 60 Hz contact stiffness (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L222-L223`, itself ≤ substep rate / 4) and visibly stiffer than rope |
| Damping ratio | CAT-058 | 1 | — | **Proposed**: no bounce on a sudden load |
| `cutting_resistance` | 1 (see EL-066) | 25 | J | **Proposed**: above what a falling Bowling ball delivers through scissors from 0.5 m (≈20 J), within tin-snip work |

### Constraints and joints

Tension-only route constraint: inactive while slack, resists only lengthening when taut; compression cannot push a load (legacy law `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122`). Route rules (typed endpoints, no branching, loops rejected, complete-route requirement, connected-body collision enabled) are the rope rules (`engine/RopeNetwork.cs@a6c914e:L69-L78`; CAT-058 R4–R18).

### Typed ports

Endpoints use the existing rope socket `Tie` on anchors, loads, pulleys, winches and hooks (`engine/MachineData.cs@a6c914e:L104-L108`). The connection record gains a `ConnectorMaterial` field; the domain stays `Rope` (Open question 1).

### Sensors and activation

None. Committed route state {Open, Slack, Taut} (`engine/ConnectionPort.cs@a6c914e:L12-L12`) drives artwork.

### Work and energy stores

Elastic energy in the soft tension row only (returned on unloading). No external work.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| span length | f32 | 0.05–200 | connect-time distance | m | Legacy rope length rule (`engine/ConnectionPort.cs@a6c914e:L47-L52`; CAT-058 R5–R6) |

### Cosmetic curves and UI bindings

Straight when taut, length-matched curve when slack, dashed while unfinished (rope rule, `DESIGN.md@a6c914e:L284-L284`); steel cable drawn as a twisted pale grey strand.

### Art

Pale grey metal `#ccd9df` with navy twist marks `#293954`; endpoint ferrules gold `#f7cb52` (the gold endpoint knot language of ropes). **Proposed**.

### Catalogue and inventory entry

A connection material chosen in the wiring drawer, not a placed part. Inventory: counted connector allowance per level (Open question 2). Display name "Steel cable" (**proposed**).

### Variants

The requirements row names no variants or modes. One connector material is specified.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission (map); coverage JSON adds StateTransaction.

**Exists now:** TGS Soft parameterisation (`makeSoft`) usable for a soft tension row (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L230-L236`); typed connection storage (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`, Activation and Electrical only).

**Missing**

- Rope domain, `Tie`, tension-only route: Story 10.2.
- Connector material enum and per-material stiffness: no story; decision owner S336.
- Cutting resistance consumed by the fracture criterion: S563 (see EL-066/EL-068).

**Dependencies.** Same as rope: anchors, pulleys and loads (Stories 10.2/10.4).

## 4. Sources and legacy

- Requirements row: "Compression cannot push a load." No variants.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Rope supplies tension only; no equality row; inactive while current length < max − tolerance. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | carry forward (law) | Exactly the row outcome. |
| 2 | Rope link length finite, 0.05–200 m; non-rope links carry no length. | `engine/ConnectionPort.cs@a6c914e:L42-L66` | carry forward | Admission. |
| 3 | Rope state enum {Open, Slack, Taut}. | `engine/ConnectionPort.cs@a6c914e:L12-L12` | carry forward | Typed state. |
| 4 | A slack rope does not push: an upward 1 m/s impulse on the hanging load is not resisted (vertical speed stays > 0.9 m/s). | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L126-L156` | carry forward (acceptance) | "Compression cannot push". |
| 5 | Legacy rope had a single material; no stiffness or cutting property. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | carry forward as context | Both properties are new. |

**Files harvested:** `engine/physics/PhysicsRopeJoint.cs`, `engine/ConnectionPort.cs`, `engine/RopeNetwork.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/RopeTests.cs`. Searched for steel, cable, wire rope: only electrical wires, unrelated.

## 5. Acceptance outline

Requirement row: [element-067](../requirements.md#element-067).

- **Chrome UI recipe.** Place an Anchor and a Weight; connect them choosing "Steel cable" in the wiring drawer. Place a Bowling ball on a ramp aimed to strike the Weight upward. Verify connector material and length from the committed read.
- **Positive.** Run: the Weight hangs at the cable length with less stretch than the same Weight on rope.
- **Negative/control.** The Weight pushed toward the anchor (struck upward or placed closer): the cable goes slack and exerts no push. Tin snips cut it; scissors at default delivered work do not.
- **Boundaries.** Length 0.05 m and 200 m; taut/slack threshold; branch and loop rejection with cable; rope and cable mixed in one route rejected or admitted (Open question 3).
- **Run/Reset and Save/Load.** Lengths, endpoints and connector material round-trip; unknown material rejects.
- **Integrations.** The scope index [todo-210](../requirements.md#todo-210) defines no separate integration task; shared interactions use [IX-04 tension transmission](../requirements.md#interaction-04) and, when cut, [IX-37 structural fracture](../requirements.md#interaction-37). The [campaign coverage ledger](../requirements.md#campaign-element-coverage) reserves first use of rope and cable in levels 11–20.

## 6. Open questions

1. Same `Rope` domain with a material field (proposed), or a separate `Cable` domain? Owner decision.
2. Is cable an unlimited connection or a counted inventory item per level? Owner decision.
3. May one route mix rope and cable spans? Proposed: no. Owner decision.
