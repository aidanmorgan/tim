# EL-165 · Manual tap named-identity spec

This is the Story 7.0 full spec for named identity EL-165. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-165 |
| Name | Manual tap |
| Type | Water |
| Anchor | [requirements.md#element-165](../requirements.md#element-165); [named-elements.md#element-165](../invest/named-elements.md#element-165); scope source [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S458 |
| CAT spec refined | none. It refines [EL-003 Tap](EL-003-tap.md) (Batch F) as the manual-aperture variant identity |
| Related identities | [EL-166 Mechanically actuated tap](EL-166-mechanically-actuated-tap.md) and [EL-167 Solenoid tap](EL-167-solenoid-tap.md) (the other tap variants, each its own identity); EL-001 Finite reservoir (supply); EL-008 Straight water pipe (port standard) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

The three tap variants (EL-165–EL-167) share EL-003's tap body, port names and opening step (**proposed** cross-batch alignment): body 0.3 × 0.25 × 0.25 m, ports `WaterInlet` and `WaterOutlet` (the water family's names, as in Batch F), opening step 0.125. Each variant keeps its own catalogue id.

- **Bodies and shapes.** One static inline valve body (part root): Box 0.3 × 0.25 × 0.25 m with a quarter-turn handle on top (art). **Proposed**: matches a short pipe section so it drops into a route; the handle is large enough to read its angle.
- **Mass and material.** Static. The shared static water-vessel material: restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's water fixtures use).
- **Liquid.** Passage holds 0.001 m³ in the ledger (**proposed**). Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)).
- **Constraints.** None; the handle is not a physical joint.
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterInlet` | Water | Input | (−0.15, 0, 0) | Standard water mouth (EL-008 geometry, 0.10 m bore) |
  | `WaterOutlet` | Water | Output | (0.15, 0, 0) | Standard water mouth, or a spout emitting a free stream when unconnected |

  **Proposed** positions. No signal, mechanical or power port: the map row's inherited SignalPropagation and FiniteWorkActuation "are not extra powered-mode prerequisites" ([element map](../general-engine-element-map.md)).
- **Sensors and activation.** None.
- **Work and energy stores.** None. Flow follows EL-003's law and is set by supply head and aperture only: Q = Cd·s·A·√(2·g·Δh), s = `opening`, A = π·0.10² = 0.0314 m² (the standard bore), Cd 0.6, Δh the head difference across the valve ([EL-003](EL-003-tap.md); Cd and bore **proposed** there). At full opening and 1 m of head this is about 0.083 m³/s, the water family's reference tap flow. With no connected supply, or an empty one, Q = 0 at any opening.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `opening` | f32 | 0–1, step 0.125 | 0 | fraction of the bore area | range 0–1 sourced ([component research](../../component-research.md#water) tap row); step and closed default **proposed** (eighth-turns read on the handle and match EL-003; a new tap never leaks) |

  The value is chosen before Run and is fixed for the whole Run ([EL-165 row](../requirements.md#element-165)); the UI disables the control while running.
- **Cosmetic curves and UI bindings.** Handle angle = `opening` × 90° ("Handle angle ← committed opening", [component research](../../component-research.md#water)); it does not move during Run. A cyan stream at an unconnected `WaterOutlet` follows committed flow.
- **Art.** Cream valve body `#fff8e9`, gold quarter-turn handle `#f7cb52`, navy collar rings `#293954`, cyan flow window `#66b8c9`; a raised notch marks "closed" so state does not rely on colour ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `manual_tap`, title "Manual tap", category Water (**proposed**; variant-specific id). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** EL-165 is itself the manual variant of the tap family; its requirements row lists no further variants. Mechanical and solenoid taps are separate identities (EL-166, EL-167).

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-02.json) `element-165`): EnvironmentState, FiniteLedger, FiniteWorkActuation (inherited, not required), FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation (inherited, not required), StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Static Box collider: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`.
- Pre-Run part parameters held in the construction and saved by the canonical codec: `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`.

**Missing**
- Valve node with an aperture on a water route (FluidAdvection, S418) and head across it (S419): owner [S416](../invest/decisions.md#s416); unscheduled.
- Water ports and pipe snapping (TopologyTransaction): S416 with [S257](../invest/decisions.md#s257); unscheduled.
- A parameter control that is editable in build mode and locked during Run: unscheduled.

**Element dependencies.** A supply (EL-001 Finite reservoir, EL-002 Header tank) and a route (EL-008 pipes, EL-007 gutter); the shared tap declaration with EL-003.

## 4. Sources and legacy

- **Requirement row** ([element-165](../requirements.md#element-165)): "Player-selected aperture remains fixed during Run." Outcome: "Closed aperture stops flow; opening cannot supply water without a connected reservoir." No variants.
- **Map row**: "Source-specific composition: Manual aperture fixed during Run; inherited signal/actuator memberships are not extra powered-mode prerequisites."
- **Research** ([component research](../../component-research.md#water)): "Network valve node; mechanical lever or solenoid input variants"; parameter "opening 0–1"; "Quarter-turn handle regulates existing supply; actuated variants need their lever or supply, never free water". The first water slice "First Pour" is tank → tap → gutter → marked bucket, with "closed tap (nothing moves)" as its control.
- **Campaign**: "manual/mechanical/solenoid tap variants" first use 61–70 "with individually exercised construction objectives"; reuse 71–90, 114, 126–130, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "tap", "stopcock", "valve", "water": no element source ("tap" matches only unrelated words).

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-165 row](../requirements.md#element-165).
- **Chrome UI recipe.** In free play place a Finite reservoir (EL-001), the Manual tap on its outlet with real port snapping, an Open gutter (EL-007) and a Catch basin. Set `opening` to 0.5 with the real parameter control; Run.
- **Positive.** Liquid flows at a rate set by the opening and the falling head (about 0.042 m³/s at 1 m head for `opening` 0.5); the basin gains what the reservoir loses; the handle stays at 45° throughout.
- **Negative/control.** `opening` 0: nothing moves. A tap with no reservoir connected (or an empty one) at `opening` 1 emits nothing. The opening control cannot be changed during Run.
- **Boundaries.** `opening` 1 versus 0.125 (proportional rates); reversed head across the tap flows backwards only if the route allows it (no hidden check valve); reservoir emptying ends flow.
- **Run/Reset.** Volumes and in-route liquid restore exactly; `opening` is unchanged by Run.
- **Save/Load.** `opening`, pose and links round-trip; values off the 1/8 step or outside 0–1 reject.
- **Integrations.** "First Pour" recipe; EL-030 Flow meter.

## 6. Open questions

1. Whether EL-003 Tap ships as its own catalogue entry alongside the three variant entries (`manual_tap`, `lever_tap`, `solenoid_tap`), or only the variants ship: owner decision.
2. The shared tap body (0.3 × 0.25 × 0.25 m), port names (`WaterInlet`, `WaterOutlet`) and opening step (0.125) are a proposed cross-batch alignment with EL-003 (Batch F): owner decision.
3. Standard port bore (0.10 m) and Cd (0.6), owned by the EL-003/EL-008 specs (Batch F): owner decision.
4. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
5. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
