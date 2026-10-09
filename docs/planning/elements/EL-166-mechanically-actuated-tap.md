# EL-166 · Mechanically actuated tap named-identity spec

This is the Story 7.0 full spec for named identity EL-166. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-166 |
| Name | Mechanically actuated tap |
| Type | Water |
| Anchor | [requirements.md#element-166](../requirements.md#element-166); [named-elements.md#element-166](../invest/named-elements.md#element-166); scope source [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S459 |
| CAT spec refined | none. It refines [EL-003 Tap](EL-003-tap.md) as the linkage-driven variant. Related: [CAT-034 Impact lever](CAT-034-impact_lever.md) (hinge), [CAT-058 Rope anchor](CAT-058-rope_anchor.md) and [CAT-053 Pulley](CAT-053-pulley.md) (rope linkage), [CAT-039 Linear pusher](CAT-039-linear_pusher.md) |
| Related identities | [EL-165 Manual tap](EL-165-manual-tap.md), [EL-167 Solenoid tap](EL-167-solenoid-tap.md); EL-017 Mechanical float valve (a float-driven valve, separate) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

The three tap variants (EL-165–EL-167) share EL-003's tap body, port names and opening step (**proposed** cross-batch alignment): body 0.3 × 0.25 × 0.25 m, ports `WaterInlet` and `WaterOutlet` (the water family's names, as in Batch F), opening step 0.125. Each variant keeps its own catalogue id.

- **Bodies and shapes.** A static inline valve body (part root): Box 0.3 × 0.25 × 0.25 m. A dynamic lever: Box 0.4 × 0.05 × 0.05 m pivoted at the top of the valve body, pointing along local +X when closed, with a 0.1 m contact pad at its tip. **Proposed**: a 0.4 m arm gives a clear lever and torque from a modest rope pull.
- **Mass and material.** Lever 0.2 kg (**proposed**: light next to the 1 kg Basketball so a rope weight or a ball strike moves it). Pad restitution 0.1, friction 0.5 (**proposed**). Valve body: the shared static water-vessel material, restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's water fixtures use).
- **Liquid.** Passage 0.001 m³ in the ledger; family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)).
- **Constraints.** One revolute hinge lever ↔ valve body about local Z, travel 0° (closed) to 90° (fully open), with a holding friction torque of 0.5 N·m (**proposed**: the lever stays where it is put, so only real linkage work changes the aperture; a 1 kg weight on a rope at the tip gives 3.9 N·m and opens it). Story 10.3 hinge with limits.
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterInlet` | Water | Input | (−0.15, 0, 0) | Standard water mouth (0.10 m bore) |
  | `WaterOutlet` | Water | Output | (0.15, 0, 0) | Standard water mouth or spout |
  | `LeverRope` | Rope | Input | lever tip, (0.4, 0.15, 0) closed | Rope attachment owning tension, not length ([S257 rope-port](../invest/decisions.md#s257)) |

  The lever pad also accepts contact pushes (pusher, ball, impact lever). **Proposed**. No signal or power port.
- **Sensors and activation.** None. Committed lever angle is the aperture state.
- **Work and energy stores.** None. Aperture `opening` = lever angle / 90°, computed from the committed hinge angle each tick (continuous during Run). Moving the lever costs work against the holding torque and lever weight; a slack rope or a stalled pusher delivers no work, so the aperture does not change. Flow follows EL-003's law as in [EL-165](EL-165-manual-tap.md): Q = Cd·s·A·√(2·g·Δh), Cd 0.6, A = 0.0314 m², about 0.083 m³/s fully open at 1 m head.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `initial_angle` | f32 | 0–90, step 11.25 | 0 | degrees | **proposed**: authored start aperture on the shared 0.125 opening step (11.25° per step), closed by default |

- **Cosmetic curves and UI bindings.** Lever art follows the committed hinge angle directly ("Handle angle ← committed opening", [component research](../../component-research.md#water)); a cyan stream follows committed flow. No easing.
- **Art.** Cream valve body `#fff8e9`, gold lever `#f7cb52` with a navy pad `#293954`, cyan flow window `#66b8c9`, navy angle marks at 0°, 45° and 90° ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `lever_tap`, title "Mechanically actuated tap", category Water (**proposed**; variant-specific id). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** EL-166 is the mechanical variant of the tap family; its requirements row lists no further variants.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-02.json) `element-166`): EnvironmentState, FiniteLedger, FiniteWorkActuation, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Dynamic and static Box bodies, contact impulses: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`.

**Missing**
- Revolute hinge with limits and a holding friction torque: Story 10.3 (friction torque term unscheduled if 10.2 lacks it).
- Rope attachment and tension: Story 10.2 ([S257 rope-port](../invest/decisions.md#s257) → S688).
- Aperture read from a committed joint coordinate (FiniteWorkActuation): unscheduled.
- Valve node and head (S418/S419): owner [S416](../invest/decisions.md#s416); unscheduled.

**Element dependencies.** A supply and route as EL-165; an actuator: CAT-058 Rope anchor with CAT-067 Weight over CAT-053 Pulley, CAT-039 Linear pusher, CAT-034 Impact lever or a ball strike.

## 4. Sources and legacy

- **Requirement row** ([element-166](../requirements.md#element-166)): "Physical linkage work changes the flow aperture." Outcome: "A slack or stalled linkage cannot change aperture." No variants.
- **Research** ([component research](../../component-research.md#water)): "mechanical lever or solenoid input variants"; "actuated variants need their lever or supply, never free water". A cam can "open a tap once per revolution" (mechanical table, cam and follower row).
- **Decisions**: [S257](../invest/decisions.md#s257) rope-port ("a slack rope lifts nothing") and mechanical-port.
- **Campaign**: first use 61–70 with individually exercised construction objectives; reuse 71–90, 114, 126–130, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "tap", "valve", "water": no element source. Hinge and rope legacy are harvested in [CAT-034](CAT-034-impact_lever.md) and [CAT-058](CAT-058-rope_anchor.md).

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-166 row](../requirements.md#element-166).
- **Chrome UI recipe.** In free play place a Finite reservoir, the mechanically actuated tap on its outlet, a gutter and a Catch basin. Place a Rope anchor and Pulley above the lever tip and a Weight; connect the rope from `LeverRope` over the pulley to the weight with the contextual rope UI. Hold the weight on a trapdoor or a pusher-released stop; Run and release it.
- **Positive.** The falling weight pulls the lever toward 90°; flow starts and rises with the angle; the basin gains what the reservoir loses.
- **Negative/control.** A slack rope (weight resting on the floor) or an unpowered pusher: the lever does not move and the tap stays closed. With the lever opened but no reservoir connected, nothing flows.
- **Boundaries.** Lever stopped halfway by a block (half aperture holds); lever driven past 90° (limit holds); a ball strike that opens it partly; the holding torque keeps the aperture after the load leaves.
- **Run/Reset.** Lever angle, volumes and rope state restore exactly.
- **Save/Load.** `initial_angle`, pose, water and rope links round-trip.
- **Integrations.** CAT-058/CAT-053/CAT-067 rope linkage, CAT-039 pusher, cam drive.

## 6. Open questions

1. Hold-in-place (proposed) versus a spring return to closed: owner decision.
2. Whether the lever also accepts a mechanical shaft input (S257 mechanical-port), for example from a cam: owner decision.
3. Holding friction torque value (0.5 N·m proposed) and whether hinges get a friction term: Story 10.3 decision.
4. Whether EL-003 Tap ships as its own catalogue entry alongside the variant entries, and the shared body/ports/step alignment with EL-003 (Batch F): owner decision.
5. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
6. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
