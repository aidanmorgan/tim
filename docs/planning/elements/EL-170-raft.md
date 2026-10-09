# EL-170 · Raft named-identity spec

This is the Story 7.0 full spec for named identity EL-170. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-170 |
| Name | Raft |
| Type | Water |
| Anchor | [requirements.md#element-170](../requirements.md#element-170); [named-elements.md#element-170](../invest/named-elements.md#element-170); scope source [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S463 |
| CAT spec refined | none |
| Related identities | [EL-169 Cork float](EL-169-cork-float.md) (single buoyant body), [EL-028 Buoyant platform](EL-028-buoyant-platform.md) (single rigid deck), [EL-029 Boat](EL-029-boat.md) (hull); each a separate flotation identity |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** `members` dynamic log bodies, each a Box 1.2 × 0.15 × 0.25 m (0.045 m³), laid side by side along local Z; two dynamic cross battens, each a Box 0.06 × 0.04 × (0.25 × `members`) m, on top at x = ±0.45 m. Default 4 members give a 1.2 × 1.0 m deck. **Proposed**: comparable to the 1.2 m platform, but visibly made of parts.
- **Mass and material.** Member 0.2 kg (density 4.4 kg/m³), batten 0.1 kg; default raft 1.0 kg (**proposed**: displacement 0.18 m³ supports 2.88 kg gross at ρw, leaving 1.88 kg net payload, so a 1 kg Basketball rides and a 4 kg Bowling ball overloads it). Material restitution 0.1, friction 0.6 (**proposed**: wood-like, holds cargo).
- **Liquid.** Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). Each member receives its own buoyant force at its own centre of buoyancy, so stability emerges from the structure ("shared displacement and structure").
- **Buoyancy clamp.** Force regions clamp acceleration to ≤ 64 m/s² ([capability inventory](../../gpu-f32-physics.md#capability-inventory), Force Regions row). Per body, fully submerged: a member 16 × 0.045 × 9.81 / 0.2 = 35.3 m/s²; a default batten (0.0024 m³) 16 × 0.0024 × 9.81 / 0.1 = 3.8 m/s²; the whole default raft 2.88 × 9.81 / 1.0 = 28.3 m/s². All are under the clamp.
- **Constraints.** Each member is joined to each batten by a weld (fixed) joint: 2 × `members` joints, connected collision disabled. **Proposed**. No joint declaration exists in `engine/gpu` yet.
- **Typed ports.** None. A rope may tie to a batten end where rope attachments allow (CAT-058).
- **Sensors and activation.** None. Member poses are observable.
- **Work and energy stores.** None.
- **Immersion damping.** Proposed family c = 1.0 1/s acts per immersed body: F_i = −m_i·c·f_i·v_i (EL-028). Dry battens and dry cargo add inertia but no water damping. For the level four-member raft, total submerged log mass is 0.8 kg and log immersion f is 1/2.88 unloaded or 2/2.88 with a Basketball; battens on top remain dry at these draughts. Small-bob amplitude time constant τ = 2·m_total/(0.8·c·f) = 7.2 s in both cases, reaching 5% in about 22 s. Heeled/flooded configurations use each body's actual immersion and the coupled mass, not these level estimates. The S420 term is separate from the current 0–0.125 1/s drag bound (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L69-L69`).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `members` | u32 | 2–6 | 4 | logs | **proposed**: 2 is a narrow, tippy raft (1.44 kg gross, 0.84 kg net); 6 a wide, stable one (4.32 kg gross, 2.92 kg net); capacity scales with count |

- **Cosmetic curves and UI bindings.** Every member and batten follows its committed pose; rope lashings are drawn between member and batten anchors. No easing.
- **Art.** Logs in Ramp/Wall wood `#c28f52` with cream end caps `#fff8e9`, navy lashings `#293954`, gold batten bolts `#f7cb52` ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `raft`, title "Raft", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-170 is one declaration. `members` is configuration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-02.json) `element-170`): Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Dynamic Box bodies, contact impulses, gravity and linear drag: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`.

**Missing**
- Weld (fixed) joint: unscheduled (Story 6.4 brings the first prismatic joint, Story 10.3 the hinge).
- Displaced-volume buoyancy per body and immersion damping: [S416 buoyancy](../invest/decisions.md#s416) → S420; unscheduled.
- A multi-body catalogue part placed as one assembly: unscheduled.

**Element dependencies.** A liquid store large enough (EL-026, EL-027, EL-001); cargo (CAT-001 Basketball, CAT-014 Bowling ball as overload).

## 4. Sources and legacy

- **Requirement row** ([element-170](../requirements.md#element-170)): "Joined buoyant members support cargo through shared displacement and structure." Outcome: "Overload or separated members change stability." No variants.
- **Decision** [S416 buoyancy](../invest/decisions.md#s416): lighter floats at a visible waterline; water level conserved.
- **Research** ([component research](../../component-research.md#water)): cork/raft/platform/boat row "Dynamic body + displacement; hull volume, load".
- **Campaign**: "cork/raft/boat/floating platform" first use 61–70, distinct hull/load modes retain separate rows; reuse 81–100, 126–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Legacy lift = buoyancy × world pressure × mass on dynamic root bodies only | `reference/cpu/MachineWorld.cs@a6c914e:L875-L883` | do not carry forward | Mass-proportional gas model, root bodies only; a raft needs per-member displacement (S420) |
| 2 | An external lift equal to weight holds a body at rest without duplicating gravity | `CuriousContraptions.tests/PhysicsWrenchTests.cs@a6c914e:L33-L39` | carry forward (acceptance fact) | Buoyancy adds to declared gravity |

**Files harvested:** `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/PhysicsWrenchTests.cs`. Searched the Epic 7 deletion scope for "raft", "float", "water": no raft-specific legacy.

## 5. Acceptance outline

Follow the [EL-170 row](../requirements.md#element-170).
- **Chrome UI recipe.** In free play place a Canal lock chamber or a large Communicating tank with 1.0 m³, drop a 4-member Raft in with the real gizmo, and place a Basketball across the middle two members. Run.
- **Positive.** The raft floats level with the ball; every member shares the load; the raft rises with the water.
- **Negative/control.** Bowling ball cargo (4 kg > 1.88 kg): the raft submerges. Separated members: two 2-member rafts placed side by side with the ball across the gap tip apart and drop it, where the joined 4-member raft carried it.
- **Boundaries.** `members` = 2 is overloaded by the Basketball (1 kg > 0.84 kg net) and submerges; `members` = 6 carries it level; the total liquid volume is unchanged.
- **Run/Reset.** All member and batten poses and the store volume restore exactly.
- **Save/Load.** `members`, pose and any rope link round-trip; out-of-range values reject.
- **Integrations.** EL-027 Canal lock lift, rope tether (CAT-058).

## 6. Open questions

1. Whether lashings can break under load during Run (a breakable joint with a force limit) so members separate dynamically: owner decision (proposed: no; separation is shown by construction).
2. Whether members are EL-169 Cork floats joined together or a dedicated log body: owner decision.
3. Immersion damping c = 1.0 1/s scaled by immersion (proposed family constant) and any angular term: owner decision under S420.
4. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S420.
