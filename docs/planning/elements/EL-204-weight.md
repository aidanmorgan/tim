# EL-204 · Weight — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-204 · Weight · Gravity |
| Anchor | [requirements.md#element-204](../requirements.md#element-204); [named-elements entry](../invest/named-elements.md#element-204); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Refines | [CAT-067 weight](CAT-067-weight.md) ([requirement](../requirements.md#current-cat-067)); the CAT spec holds the full rope/pulley harvest. Partners: rope [EL-205](../invest/named-elements.md#element-205), pulleys [EL-206](../invest/named-elements.md#element-206)/[EL-207](../invest/named-elements.md#element-207), [CAT-053 Pulley](CAT-053-pulley.md), [CAT-058 Rope anchor](CAT-058-rope_anchor.md). |
| Roadmap story | Story 10.4 "Counterweight & Multi-Body Pendulum Lifting" (CAT-067), after Story 10.2 ropes ([epics](../../../_bmad-output/planning-artifacts/epics.md)). Element map disposition: potential/conditional ([element map](../general-engine-element-map.md)). |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

Configuration: mass, no enumerated selector ([CAT-067](../requirements.md#current-cat-067)); one mode.

- **Bodies and shapes.** One dynamic sphere collider ("translating load with a spherical collision envelope", `parts/WeightPart.cs@a6c914e:L7-L15`). Radius = 0.32 · ∛mass m (`engine/MachineData.cs@a6c914e:L99-L99`): 0.508 m at the 4 kg default, 0.32 m at 1 kg. The drawn body is a cylinder; the collider is the sphere.
- **Mass and material.**
  - Mass parameter, default 4 kg (`parts/catalog/weight.tres@a6c914e:L14-L14`); solid-sphere inertia (`parts/WeightPart.cs@a6c914e:L14-L15`).
  - Restitution 0.08, drag 0 (`parts/WeightPart.cs@a6c914e:L33-L34`); bounce threshold 0.1 m/s and friction 0.3 from the legacy dynamic default (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`).
  - **Proposed** rolling resistance 0.05 — a dropped weight is drawn as a cylinder and should not roll away like a ball; still within 0–0.1 (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`).
- **Constraints.** None of its own; the rope (EL-205) attaches at the tie and is tension-only.
- **Typed ports.** One `Tie` socket, rope domain, bidirectional, at (0, radius + 0.08, 0) (`parts/WeightPart.cs@a6c914e:L16-L21`; `engine/MachineData.cs@a6c914e:L100-L100`). Today no rope domain exists (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L9`); Story 10.2 adds it.
- **Sensors and activation.** None owned.
- **Work and energy stores.** Gravitational potential only: "Raising it requires actual work" ([element-204](../requirements.md#element-204)); the weight never supplies work it did not receive.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `mass` | `Kilograms` (f32) | 0.25–8 | 4 | kg | `parts/WeightPart.cs@a6c914e:L22-L27`; `parts/catalog/weight.tres@a6c914e:L14-L14` |

  Fixture loads in the counterweight lessons use the fixed fixture mass 1, not the default 4 ([CAT-067](../requirements.md#current-cat-067)).
- **Cosmetic curves and UI bindings.** None; art follows the committed pose. Countable bands = ⌈mass⌉ communicate heavier loads (`parts/WeightPart.cs@a6c914e:L40-L46`).
- **Art.** Cylinder radius 0.85 r, height 1.35 r in catalogue navy `#45639c`; cream band `#fff8e9` near the base; gold eye ring `#f7cb52` (radius 0.14 m) at 0.72 r (`parts/WeightPart.cs@a6c914e:L36-L39`; `DESIGN.md@a6c914e:L184-L184`). Pick radius r + 0.22 m (`parts/WeightPart.cs@a6c914e:L35-L35`).
- **Catalogue and inventory.** Id `weight`, title "Weight", category Ropes, "A rope-linked load. Larger weights have more bands and pull harder. Connect through pulleys to lift a lighter load." (`parts/catalog/weight.tres@a6c914e:L7-L14`). `WorkshopPartKind.Weight` appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-03.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, StateTransaction; source-specific "Add TensionTransmission binding for supported rope load".

- **Exists now.** Dynamic sphere with mass, contact, friction, gravity (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`). Mass range 1/1024–1024 kg admits 0.25–8 (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L78-L78`). Today only `BallMaterial` kinds compile as dynamic spheres; a weight needs its own declaration record with a mass parameter.
- **Missing.** Rope domain, tie socket and tension-only rope row (TensionTransmission): Story 10.2 (S257 rope-port row, [decisions](../invest/decisions.md#s257)). Pendulum suspension: Story 10.4.
- **Dependencies.** Rope (EL-205) with Pulley (CAT-053) or Rope anchor (CAT-058); a Switch and Lamp for the counterweight lesson.

## 4. Sources and legacy

- **Requirements.** Row: "Movable load exchanges gravitational potential and tension through declared sockets"; outcome "Raising it requires actual work" ([element-204](../requirements.md#element-204)). CAT-067: equal/unequal ratios, slack release, floor contact, energy/length bounds.

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Two weights over two fixed pulleys accelerate at g (m₂ − m₁)/(m₁ + m₂) within 0.015 m/s after 30 ticks; heights stay complementary; rope taut at its length; replay identical. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L21-L68` | Carry forward. |
| 2 | Invalid masses −1, 0, 20 and NaN reject before an instance is added. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L110-L124` | Carry forward. |
| 3 | Slack rope does not push; a taut tether stops extension. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L126-L156` | Carry forward. |
| 4 | An unfinished pulley route cannot lift. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L158-L189` | Carry forward. |
| 5 | A 2 kg 3D pendulum stays within its rope (±2 mm) and never gains energy (+0.5 J tolerance). | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L191-L215` | Carry forward behaviour; the 2 mm/0.5 J tolerances are game-grade checks, re-derive against the envelope. |
| 6 | Counterweight contacts cannot pull bodies through the floor. | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L217-L241` | Carry forward. |
| 7 | Lessons `counterweight` (load at (−2, 1, 0.12), pulleys at (±2, 6, 0), solution weight at (2, 5, 0.12), rope spans 5 / 4 / 0.812 m) and `pulley_depth`. | `content/puzzles.json@a6c914e:L5071-L5506`, `content/puzzles.json@a6c914e:L5507-L5943` | Carry forward as Story 10.4 lesson set-ups. |
| 8 | Parameter keys bound through a string dictionary. | `parts/WeightPart.cs@a6c914e:L11-L12` | Do not carry forward: typed parameter record. |

Files consulted: `parts/WeightPart.cs`, `parts/catalog/weight.tres`, `parts/scenes/weight.tscn` (script reference only), `engine/MachineData.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/RopeTests.cs`, `content/puzzles.json`.

## 5. Acceptance outline

Point of truth: [element-204](../requirements.md#element-204), [CAT-067](../requirements.md#current-cat-067).

- **Chrome recipe.** Place two Pulleys high, a 1 kg Weight and a 4 kg Weight below them; connect Weight → Pulley → Pulley → Weight with the rope tool.
- **Positive.** The 4 kg weight descends and lifts the 1 kg weight at the predicted acceleration.
- **Negative or control.** Equal masses stay balanced. With no rope, lifting the light weight requires an external push: it never rises by itself.
- **Boundaries.** Mass 0.25 and 8 admitted; 0.24 and 8.01 rejected; weights never sink into the bench.
- **Run/Reset.** Reset restores weights, rope length and pulleys exactly.
- **Save/Load.** Mass and rope connections survive save and Load.
- **Integrations.** Rope/weight connection audit [sequence-task-275](../requirements.md#sequence-task-275) (weights exchange tension through pulleys and anchors); CAT-067 retained behaviour [todo-141](../requirements.md#todo-141); interaction rows IX-04 tension transmission ([interaction-04](../requirements.md#interaction-04)) and IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)). Campaign first use: the "Battery … weight, rope, cable, fixed/moving pulley roles …" row of the [campaign element coverage](../requirements.md#campaign-element-coverage) — first use 11–20, moving/load variants 41–50; combination and spaced reuse 31–50, 61–80, 136–150.

## 6. Open questions

1. Sphere collider under a cylinder drawing: keep (legacy) or adopt a box/cylinder collider. Unspecified — owner decision.
2. Rolling resistance (proposed 0.05).
