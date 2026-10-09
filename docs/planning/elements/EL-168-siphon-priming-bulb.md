# EL-168 · Siphon priming bulb named-identity spec

This is the Story 7.0 full spec for named identity EL-168. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-168 |
| Name | Siphon priming bulb |
| Type | Water |
| Anchor | [requirements.md#element-168](../requirements.md#element-168); [named-elements.md#element-168](../invest/named-elements.md#element-168); scope source [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S461 |
| CAT spec refined | none. Related: [CAT-010 Bellows](CAT-010-bellows.md) (compressed-chamber precedent, Story 12.4), [CAT-062 Spring](CAT-062-spring.md) (slider and return spring, Story 6.4) |
| Related identities | [EL-024 Primed siphon](EL-024-primed-siphon.md) (the only consumer); EL-022 Water pump (alternative primer); [EL-039 Air reservoir](EL-039-air-reservoir.md) and the EL-037–EL-043 pneumatic family (sealed-gas siblings) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** A static cradle (part root): Box 0.4 × 0.08 × 0.3 m. A dynamic press plate: Box 0.3 × 0.04 × 0.25 m resting on the bulb, 0.15 m above the cradle when the bulb is full. The bulb itself is a gas chamber whose volume follows the plate height (art: a rounded cyan bulb). **Proposed**: a hand-pump silhouette a falling ball or pusher can press.
- **Mass and material.** Plate 0.1 kg (**proposed**: light, so the bulb spring returns it). Plate restitution 0.1, friction 0.6 (**proposed**). Cradle: the shared static water-vessel material, restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's water fixtures use).
- **Gas and liquid.** Bulb volume 0.012 m³ full (**proposed**: three full squeezes, 0.036 m³, exceed the 0.031 m³ of gas in the default [EL-024](EL-024-primed-siphon.md) tube with both ends submerged 0.1 m). Liquid drawn into the siphon is liquid at the **proposed** family density ρw = 16 kg/m³ (shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)).
- **Constraints.** One prismatic joint plate ↔ cradle along local Y, travel 0–0.15 m, with a return spring of 200 N/m toward full (**proposed**: a resting Basketball, 9.8 N, presses about a third; a Bowling ball, 39 N, presses fully; a full squeeze costs 2.25 J). Story 6.4 slider and spring.
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterPrimeOut` | Water | Output | (0.2, 0, 0) | Connects by tube to EL-024 `WaterPrime`; the only connection |

  **Proposed**; named in the water family's `Water*` style (Batch F). The press plate accepts contact load (falling ball, pusher, lever). No signal or power port.
- **Sensors and activation.** None. Committed bulb volume and the connected siphon's state are observable.
- **Work and energy stores.** Two internal one-way valves (**proposed**): pressing expels bulb gas to the atmosphere; release re-expands the bulb, drawing the same gas volume out of the connected siphon tube, so liquid rises in the legs by that volume. Evacuation only builds a column while both siphon ends are liquid-sealed: the intake below the source surface and the outlet submerged in a receiver or sealed (an EL-012 cap or a closed downstream tap). With the siphon outlet open in air, each release draws air in through the outlet instead and no column forms. The gas volume to evacuate is the siphon tube's gas volume, V_gas = A × (tube length − both submerged lengths): 0.0113 m² × 2.7 m = 0.031 m³ for the default tube with both ends submerged 0.1 m, i.e. three full squeezes. Until V_gas is removed the partial column is held by the valves and stays in the ledger. Every squeeze costs real compression work; nothing fills the siphon remotely or automatically.
- **Parameters.** None configurable (**proposed**: one bulb size keeps "count the squeezes" teachable).
- **Cosmetic curves and UI bindings.** Bulb shape follows the committed plate position; three navy tick marks on the cradle show squeezes of a default siphon. No easing.
- **Art.** Cyan rubber bulb `#66b8c9`, cream press plate `#fff8e9`, gold valve caps `#f7cb52`, navy cradle `#293954` ([DESIGN bellows](../../../DESIGN.md#bellows) uses the same cyan body, cream plate and gold accents). **Proposed**.
- **Catalogue and inventory entry.** Id `priming_bulb`, title "Siphon priming bulb", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-168 is one declaration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-02.json) `element-168`): EnvironmentState, FiniteLedger, FiniteWorkActuation, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Static and dynamic Box bodies, contact normal impulses: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`.

**Missing**
- Sealed gas chamber whose volume follows a slider (GasState): [S470 gas-state](../invest/decisions.md#s470) → S471; unscheduled.
- Prismatic slider with spring: Story 6.4.
- Coupling of drawn gas volume to the siphon column, with the end-seal rule (S445-D) and the liquid/gas boundary ([S416](../invest/decisions.md#s416)/S470): unscheduled.
- Connection between the bulb and the siphon `WaterPrime` port (TopologyTransaction): S416 with [S257](../invest/decisions.md#s257); unscheduled.

**Element dependencies.** EL-024 Primed siphon (required consumer) with a submerged or sealed outlet; a presser: CAT-039 Linear pusher (Story 11.3), a falling ball, CAT-034 Impact lever.

## 4. Sources and legacy

- **Requirement row** ([element-168](../requirements.md#element-168)): "Finite mechanical compression expels gas or transfers liquid to establish a supported liquid column." Outcome: "Insufficient priming leaves the siphon broken; no remote automatic fill." No variants.
- **Map row** decision owners: S416/S470 liquid/gas boundary and S470 finite atmosphere/gas and force admission.
- **Research** ([component research](../../component-research.md#water)): "Primed siphon / Priming bulb (EL-024, EL-168) — Siphon state + network nodes; column height; Dry → Priming → Flowing → Broken; air entry breaks it". Bypassing priming is forbidden even for later nudging ("never ... bypasses priming").
- **Integration task** [sequence-task-389](../requirements.md#sequence-task-389): "maintain/break a primed siphon".
- **Campaign**: "siphon/priming bulb" first use 61–70, reuse 71–90, 114, 126–130, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** No bulb- or siphon-specific source. Searched the Epic 7 deletion scope at `a6c914e` for "priming", "bulb", "siphon", "water". The legacy sealed-gas primitives the GasState decision (S471) starts from are already accounted for elsewhere: `engine/physics/AxialGasGeometry.cs@a6c914e:L5-L12` (chamber geometry; "SignedArea is dV/dq", the volume-per-stroke relation a bulb needs) is listed in [CAT-010](CAT-010-bellows.md), and `engine/physics/SealedGasState.cs@a6c914e:L5-L18` (homogeneous mass, volume and internal energy, with pressure derived) is harvested in [CAT-003](CAT-003-balloon.md) and [EL-039](EL-039-air-reservoir.md). Both are CPU double-precision classes: their state variables carry forward as S471 input, the classes do not.

**Files harvested:** none new (pointers above to `engine/physics/AxialGasGeometry.cs` and `engine/physics/SealedGasState.cs`, harvested by CAT-010, CAT-003 and EL-039).

## 5. Acceptance outline

Follow the [EL-168 row](../requirements.md#element-168).
- **Chrome UI recipe.** Build the [EL-024](EL-024-primed-siphon.md) siphon construction with its outlet submerged 0.1 m in the lower reservoir, place the priming bulb and connect `WaterPrimeOut` to the siphon `WaterPrime` with the contextual connection UI; place a Linear pusher above the plate with a Clock (CAT-017) pulsing its command and a Battery supply. Run.
- **Positive.** Each press and release raises liquid in the legs; after the third full squeeze (0.036 m³ ≥ 0.031 m³) the siphon reaches Flowing and keeps running with the pusher stopped.
- **Negative/control.** Two squeezes only (0.024 m³): the column stays incomplete and nothing flows. The siphon outlet open in air: squeezes draw air through the outlet and the state stays Dry. An unconnected bulb squeezed any number of times changes no siphon. With the source empty the bulb draws only air and the siphon stays Dry.
- **Boundaries.** Partial presses (a Basketball resting on the plate) draw proportionally less; an outlet sealed by an EL-012 cap primes the same way; a siphon that breaks later needs priming again; squeeze work is paid by the presser every time.
- **Run/Reset.** Plate position, bulb volume, drawn column and siphon state restore exactly.
- **Save/Load.** Pose and the `WaterPrimeOut` link round-trip.
- **Integrations.** EL-024 siphon, CAT-039 pusher, recipe "Prime Time".

## 6. Open questions

1. Whether the bulb–siphon link is a Water-domain connection (proposed) or a pneumatic hose (EL-038): owner decision under S470/S257.
2. Whether a held partial column leaks back over time: S445-D decision (proposed: held while connected).
3. Whether the bulb can also prime by pushing liquid (the row says "expels gas or transfers liquid"): owner decision; this spec covers the gas-evacuation route.
4. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
5. Shared static water-vessel material for the cradle (restitution 0.12, friction 0.3, proposed): owner decision.
