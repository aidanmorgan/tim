# EL-026 · Communicating tank named-identity spec

This is the Story 7.0 full spec for named identity EL-026. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-026 |
| Name | Communicating tank |
| Type | Water |
| Anchor | [requirements.md#element-026](../requirements.md#element-026); [named-elements.md#element-026](../invest/named-elements.md#element-026); scope index [todo-339](../requirements.md#todo-339) |
| Proof owner | S447 |
| CAT spec refined | none |
| Related identities | EL-001 Finite reservoir (a single store; this tank adds low connecting ports), EL-008–EL-012 water pipe kit (the connection), [EL-027 Canal lock chamber](EL-027-canal-lock-chamber.md), [EL-032 Pressure meter](EL-032-pressure-meter.md) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** One static open-topped vessel (part root): interior `width` × `height` × `depth`, default 0.8 × 1.2 × 0.8 m, walls and floor 0.06 m thick, built from five Box colliders like the Receiver (`engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L17`). **Proposed**: close to the 1.5 m Receiver footprint, tall enough to show a level difference.
- **Mass and material.** Static. The shared static water-vessel material: restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's vessels use).
- **Liquid.** Finite store of capacity `width × height × depth` (0.768 m³ default) with a free surface; family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)), so a full default tank holds 12.3 kg. Overflow at the rim leaves as free-stream packets and stays in the ledger.
- **Constraints.** None.
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterMouthA` | Water | Bidirectional | (−0.43, −0.5, 0) | Low left wall mouth, 0.1 m above the interior floor |
  | `WaterMouthB` | Water | Bidirectional | (0.43, −0.5, 0) | Low right wall mouth at the same height |
  | `OpenMouth` | Water | Input | (0, 0.6, 0) | Open top; captures intersecting free-stream packets |

  **Proposed** positions; names follow the water family's port vocabulary (`WaterMouthA`/`WaterMouthB` for bidirectional mouths, `OpenMouth` for an open aperture, as in Batch F). A capped or unconnected low mouth is sealed. The `Water` domain does not exist yet (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`).
- **Sensors and activation.** None of its own. Its level and port pressure are observable by EL-032 and the level sensors of EL-016–EL-018.
- **Work and energy stores.** None. Exchange through a connected pipe is driven by the difference in hydraulic head at the two ports: head = free-surface elevation in world space, so tanks at different heights equalise to a common surface, not a common depth. Flow Q = k·(H₁ − H₂) with conductance k set by the connecting pipe (the S418/S419 decision), never copying volume.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `width` | f32 | 0.4–1.6, step 0.1 | 0.8 | m | **proposed**: bench depth 9.8 m allows several tanks side by side |
  | `height` | f32 | 0.4–2.0, step 0.1 | 1.2 | m | **proposed**: taller than the 1 m Receiver so level differences read |
  | `depth` | f32 | 0.4–1.6, step 0.1 | 0.8 | m | **proposed**: matches `width` for a square plan |
  | `initial_volume` | f32 | 0–capacity, step 1/16 | 0.25 | m³ | 1/16 m³ step is sourced ([component research](../../component-research.md#water) reservoir row); default **proposed** (about a third full, 4 kg) |

- **Cosmetic curves and UI bindings.** Waterline follows the committed volume ("Waterlines ← volumes", [component research](../../component-research.md#water)); etched fill marks every 1/16 m³. No easing.
- **Art.** Cream walls `#fff8e9` with a restrained cyan level window `#66b8c9` (the [common visual contract](../requirements.md#individual-element-register) asks water pieces to expose level through restrained windows), navy foot `#293954`, gold port collars `#f7cb52`. **Proposed**.
- **Catalogue and inventory entry.** Id `communicating_tank`, title "Communicating tank", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-026 is one declaration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-026`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Static Box colliders and the Receiver's five-box vessel precedent: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`, `engine/gpu/ReceiverGeometry.cs@a6c914e:L10-L17`.
- Counted inventory and save codec: `engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`.

**Missing**
- Liquid FiniteLedger and FluidAdvection between stores (S418), head/pressure relation (S419): owner [S416](../invest/decisions.md#s416); unscheduled. S416 advection's Chrome construction is "two containers and one branching route: the receiving levels rise by what the source loses; an empty source sends nothing".
- Water ports and pipe connections (TopologyTransaction): S416 with [S257](../invest/decisions.md#s257); unscheduled.

**Element dependencies.** A second communicating tank or any store with a water port; a pipe route from the EL-008–EL-012 kit.

## 4. Sources and legacy

- **Requirement row** ([element-026](../requirements.md#element-026)): "Connected vessel exchanges volume according to pressure and elevation." Outcome: "Disconnected tank does not equalise remotely." No variants.
- **Integration task** [sequence-task-390](../requirements.md#sequence-task-390): "Connected tank levels ... react to the actual fluid state". Refinement [S708 tank-levels](../invest/refinements.md#s708): "Connected tank levels follow actual conserved fluid."
- **Research** ([component research](../../component-research.md#water)): "Linked stores + sluice nodes"; "Low pipes equalise levels"; binding "Waterlines ← volumes". Sealed pipes carry head uphill; "a split shares supply, never copies it".
- **Family contract** [profiles#fluids](../invest/profiles.md#fluids): a full container holds its declared amount; reversed head does not flow uphill.
- **Campaign**: first use 61–70; distinct hull/load modes retain separate rows; reuse 81–100, 126–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "tank", "vessel", "communicating", "water", "liquid": no element source.

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-026 row](../requirements.md#element-026).
- **Chrome UI recipe.** In free play place two communicating tanks, the left with `initial_volume` 0.5 m³ and the right empty, set through the real parameter control. Join `WaterMouthB` of the left to `WaterMouthA` of the right with a Straight water pipe (EL-008) using the real placement and snapping. Run.
- **Positive.** The left level falls and the right rises until both free surfaces stand at the same world height; the total volume is unchanged.
- **Negative/control.** Without the pipe (or with a Water pipe cap, EL-012, on one mouth) the levels stay exactly as authored: no remote equalisation.
- **Boundaries.** Tanks at different base heights equalise to a common surface; a receiving tank filling above its rim spills conserved overflow; a mouth above the source free surface carries nothing; three tanks on a T junction share supply without copying.
- **Run/Reset.** Both volumes and in-pipe liquid restore exactly.
- **Save/Load.** Dimensions, `initial_volume`, pose and water links round-trip; out-of-range values reject.
- **Integrations.** EL-027 Canal lock chamber, EL-032 Pressure meter, EL-016 Float.

## 6. Open questions

1. Whether port conductance belongs to the tank mouth, the pipe, or both in series: S418/S419 decision.
2. Whether tank dimensions are player-resizable or fixed sizes in the palette: owner decision.
3. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
4. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
