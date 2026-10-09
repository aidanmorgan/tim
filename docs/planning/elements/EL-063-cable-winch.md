# EL-063 · Cable winch declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-063 |
| Name | Cable winch |
| Type | Mechanical |
| Requirement anchor | [element-063](../requirements.md#element-063); scope index [todo-320](../requirements.md#todo-320) |
| Named entry | [named-elements.md#element-063](../invest/named-elements.md#element-063); proof owner S334 |
| CAT spec refined or extended | none. Related: [CAT-042 motor](CAT-042-motor.md) (shaft source), [CAT-058 rope anchor](CAT-058-rope_anchor.md) (rope route law, EL-205), [CAT-053 pulley](CAT-053-pulley.md), [CAT-067 weight](CAT-067-weight.md) (load) |
| Related identities | [EL-067 steel cable](EL-067-steel-cable.md), [EL-065 load hook](EL-065-load-hook.md), [EL-056 ratchet](EL-056-ratchet.md), [EL-055 brake](EL-055-mechanical-brake.md), [EL-057 escapement](EL-057-escapement.md); recipe-only "winch" record in [gap-historical-followthrough](../requirements.md#gap-historical-followthrough) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

- Static frame: box 0.9 × 0.7 × 0.6 m. **Proposed**: houses a drum of the default radius with bearing blocks, at the scale of the 1.25 m motor base.
- Dynamic drum shaft: cylinder r `drum_radius`, width 0.3 m, mass 0.5 kg. **Proposed**: half the 0.4 m pulley radius (`engine/MachineData.cs@a6c914e:L97-L97`), so a default 20 N·m motor pulls 100 N, enough for an 8 kg Weight (78 N).

### Mass and material

Frame: static default material (restitution 1, threshold 0.1 m/s, friction 0.3; `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). Cable mass: none (massless, as the legacy rope; see [CAT-058](CAT-058-rope_anchor.md) Open question 3).

### Constraints and joints

- Drum hinge to the frame.
- Spooled route: the cable route's maximum length = authored free length − `drum_radius` × drum angle (paid out positive, wound negative), clamped to [0, `cable_capacity`]. Length changes only through drum rotation, never instantaneously. Tension in the route produces a reaction torque on the drum (tension × radius), so a stalled or reverse-loaded drum debits work or is back-driven.

### Typed ports

| Socket | Domain | Direction | Local position (m) |
| --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input | (−0.45, 0, 0.3) |
| `Drive` | Mechanical | Output (pass-through) | (0.45, 0, 0.3) |
| `Tie` | Rope | Bidirectional (cable end leaves the drum) | drum tangent, (0, `drum_radius`, 0) |

Identities `engine/MachineData.cs@a6c914e:L104-L108`; positions **proposed**. Attachment role: a new `Spool` role (one end, like `Anchor`, but with variable length; **proposed**) — legacy roles are {None, Load, Anchor, Guide} (`engine/ConnectionPort.cs@a6c914e:L11-L11`).

### Sensors and activation

None. Committed wound length is a read for cosmetics.

### Work and energy stores

None inside the winch. Work flows from the shaft into lifted potential energy; a stalled motor delivers no net lift work; a reverse load back-drives the drum and returns energy upstream (or is stopped by a ratchet or brake).

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `drum_radius` | f32 | 0.1–0.4 | 0.2 | m | **Proposed** (see Bodies) |
| `cable_capacity` | f32 | 1–12 | 6 | m | **Proposed**: spans most of the 9.8 m bench depth or a tall lift while staying a few visible drum turns |

Closed parameter enum `WinchParameter { DrumRadius, CableCapacity }`. The free length at connect time follows the rope rule "length is set when you connect" (legacy fact R6 in [CAT-058](CAT-058-rope_anchor.md)).

### Cosmetic curves and UI bindings

Drum spokes follow the committed drum angle; wound cable layers on the drum follow committed wound length; free cable span is straight when taut and curved when slack (rope artwork rule, `DESIGN.md@a6c914e:L284-L284`).

### Art

Cream frame `#fff8e9`, navy foot `#293954`, ochre drum `#d69c47` with gold spokes `#f7cb52`, cable pale grey `#ccd9df` (steel) or warm-wood rope per the connected connector. Catalogue colour **proposed**: ochre (Pulley / rope anchor family, `DESIGN.md@a6c914e:L187-L187`).

### Catalogue and inventory entry

Id `cable_winch`, title "Winch", category "Ropes"; `WorkshopPartKind.CableWinch` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. One element is specified.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, TensionTransmission (map); coverage JSON adds StateTransaction. Neither lists ShaftTorque, although the row says "shaft work" (Open question 3).

**Exists now:** static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); gravity (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1278`).

**Missing**

- Rope domain, `Tie`, tension-only route and admission: Story 10.2; counterweight lifting: Story 10.4.
- Variable-length (spooled) route coupled to a hinge angle: no story; decision owner S334.
- Hinge: Story 10.3. `Mechanical` domain and shaft torque: Story 11.1.
- Cylinder collider and inertia (drum): no owning story; owner decision 9 Oct 2026: a new cylinder-collider story comes before the first shaft wheel.

**Dependencies.** CAT-042 Motor + CAT-005 Battery (11.1/8.1); a load (CAT-067 Weight, 10.4); EL-067 Steel cable or the EL-205 rope as connector.

## 4. Sources and legacy

- Requirements row: "Stall and reverse load debit work; no instantaneous rope shortening." No variants.
- GAP follow-through: recipe-only winch references become constituent or dedicated-piece specifications ([requirements.md#gap-historical-followthrough](../requirements.md#gap-historical-followthrough)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | A shaft drives a translating coordinate through a declared lead (m/rad) as a bidirectional coupling. | `parts/WoundSpringPart.cs@a6c914e:L89-L98`; `engine/physics/PhysicsTransmissionJoint.cs@a6c914e:L7-L13` | carry forward (law; the drum radius is the lead) | Gives finite-work winding with reaction torque. |
| 2 | With a supplied motor the plunger winds and stored energy ≤ 1.02 × accepted work + 0.01 J; without supply nothing moves and accepted work is 0. | `CuriousContraptions.tests/WoundSpringRuntimeTests.cs@a6c914e:L102-L141` | carry forward (acceptance pattern) | "Stall debits work"; energy bound. |
| 3 | Rope is tension-only, inactive while slack; branching rejected. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122`; `engine/RopeNetwork.cs@a6c914e:L69-L78` | carry forward | Cable route law. |
| 4 | Rope length bounds 0.05–200 m; connect-time length. | via [CAT-058](CAT-058-rope_anchor.md) R5–R6 | carry forward | Admission range of the free length. |
| 5 | Weight 0.25–8 kg, radius 0.32·∛m, tie at r + 0.08. | `parts/WeightPart.cs@a6c914e:L25-L26`; `engine/MachineData.cs@a6c914e:L99-L100` | carry forward via CAT-067 | Load scale used for drum sizing. |
| 6 | Rope cutting and moving-block ratios were future work. | `reference/P0-022-before/source-README.md@a6c914e:L99-L99` | carry forward as context | Winch winding was never implemented. |

**Files harvested:** `parts/WoundSpringPart.cs`, `parts/WeightPart.cs`, `engine/physics/PhysicsTransmissionJoint.cs`, `engine/physics/PhysicsRopeJoint.cs`, `engine/RopeNetwork.cs`, `engine/ConnectionPort.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/WoundSpringRuntimeTests.cs`, `reference/P0-022-before/source-README.md`. Searched with no hit for winch, spool, drum.

## 5. Acceptance outline

Requirement row: [element-063](../requirements.md#element-063).

- **Chrome UI recipe.** Place Battery, Motor and Winch on a high Wall ledge, a Weight on the bench below; wire Battery → Motor; belt Motor `Drive` → Winch `DriveIn`; connect Winch `Tie` → Weight `tie` (cable or rope). Verify placement and links.
- **Positive.** Run: the drum turns and the Weight rises by `drum_radius` × drum angle; motor supplied work ≥ the Weight's gained potential energy.
- **Negative/control.** No supply: the Weight stays (hanging from slack or resting) and the cable length is unchanged. Motor torque 5 N·m with an 8 kg Weight: the drum stalls, the Weight does not rise, and supplied work stops increasing. Supply cut mid-lift: the Weight back-drives the drum and descends (no ratchet).
- **Boundaries.** Cable fully wound (length 0) and fully paid out (`cable_capacity`) stop the drum; drum radius 0.1 and 0.4; reversed motor lowers; no step change in length in any tick.
- **Run/Reset and Save/Load.** Drum angle, wound length and Weight restore; parameters and links round-trip.
- **Integrations.** Cross-element task [sequence-task-371](../requirements.md#sequence-task-371) under [todo-320](../requirements.md#todo-320) (carrier and pickup obligations kept per named element); shared processes [IX-04 tension transmission](../requirements.md#interaction-04) and [IX-05 shaft torque transmission](../requirements.md#interaction-05).

## 6. Open questions

1. Does winding change the effective drum radius as layers build up? Proposed: no (constant radius). Owner decision.
2. Should a winch include an integral ratchet, or always rely on EL-056/EL-055? Proposed: rely on them. Owner decision.
3. Binding omits ShaftTorque despite the shaft input. Confirm the binding should add it. Owner decision.
