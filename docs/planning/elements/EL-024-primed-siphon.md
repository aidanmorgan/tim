# EL-024 · Primed siphon named-identity spec

This is the Story 7.0 full spec for named identity EL-024. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-024 |
| Name | Primed siphon |
| Type | Water |
| Anchor | [requirements.md#element-024](../requirements.md#element-024); [named-elements.md#element-024](../invest/named-elements.md#element-024); scope index [todo-338](../requirements.md#todo-338) |
| Proof owner | S445 (with the S445-D priming/column decision named in the [element map](../general-engine-element-map.md)) |
| CAT spec refined | none |
| Related identities | [EL-168 Siphon priming bulb](EL-168-siphon-priming-bulb.md) (primer); EL-001 Finite reservoir and EL-004 Catch basin (source and receiver); EL-012 Water pipe cap (outlet seal); EL-022 Water pump (alternative primer, recipe "Prime Time") |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** One static sealed inverted-U tube (part root): an intake leg, a crest span and a discharge leg, inner radius 0.06 m, wall 0.02 m, built from Box colliders. Default intake leg 1.0 m, crest span 0.4 m, discharge leg 1.5 m, so the outlet sits 0.5 m below the intake mouth. Bore area 0.0113 m²; tube volume 0.0328 m³ at the default 2.9 m. All **proposed**: a 0.06 m bore keeps the tube visibly thinner than the 0.10 m standard water mouth and the ball pipe, and three default priming-bulb squeezes evacuate it.
- **Mass and material.** Static, no mass. The shared static water-vessel material: restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's vessels use).
- **Liquid.** Family density ρw = 16 kg/m³ (**proposed** water-family constant shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). Gravity 9.81 m/s² (`engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L123`).
- **Constraints.** None; the tube is static.
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterInlet` | Water | Input | (0, −1.0, 0) | Open intake mouth; must sit below the source free surface |
  | `WaterOutlet` | Water | Output | (0.4, −1.5, 0) | Connects to a water mouth, or emits a free stream when open in air |
  | `WaterPrime` | Water | Input | (0.2, 0.06, 0) | Crest port for EL-168 or a pump; sealed when unused |

  Positions follow the default leg lengths (**proposed**); names follow the water family's `Water*` port vocabulary (Batch F). The `Water` domain does not exist yet (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L10`).
- **Sensors and activation.** Committed siphon state is a closed enum `SiphonState { Dry, Priming, Flowing, Broken }` (sourced: `docs/component-research.md` siphon/prime row, [component research](../../component-research.md#water)). No signal output; meters observe it.
- **Priming rule.** Priming evacuates the gas in the tube through `WaterPrime`, so liquid rises into both legs. Both ends must be liquid-sealed while priming: the intake below the source surface, and the outlet either submerged below a receiver's free surface or sealed (an EL-012 cap, or a closed tap or valve downstream). With the outlet open in air, evacuation draws air in through the outlet, no column forms and the state stays Dry. The gas volume to evacuate is V_gas = A × (tube length − intake submergence − outlet submergence): with the defaults and both ends submerged 0.1 m, 0.0113 × 2.7 = 0.031 m³ (**proposed** geometry; three 0.012 m³ EL-168 squeezes give 0.036 m³). The state becomes Flowing when V_gas has been removed and the outlet is unsealed (submerged outlets flow at once).
- **Work and energy stores.** None. Flow is driven only by available head: Q = Cd·A·√(2·g·Δh) with Δh = source free surface − outlet elevation (or the receiver's free surface when the outlet is submerged), Cd 0.6 (the water-family orifice coefficient, **proposed** in Batch F's [EL-003](EL-003-tap.md)). The column is liquid in the finite ledger. The column breaks (state Broken, liquid in the legs drains under gravity to the lower ends) when the intake uncovers (air entry), when Δh ≤ 0, or when the crest stands more than `max_crest_lift` above the source surface.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `intake_leg` | f32 | 0.5–2.0, step 0.25 | 1.0 | m | **proposed**: reaches the floor of a 1 m tall reservoir |
  | `discharge_leg` | f32 | 0.5–3.0, step 0.25 | 1.5 | m | **proposed**: default outlet 0.5 m below the intake gives positive head |
  | `crest_span` | f32 | 0.2–1.0 | 0.4 | m | **proposed**: clears a 0.12 m reservoir wall with margin |
  | `max_crest_lift` | f32 | fixed per family | 1.5 | m | **proposed**: game-scale atmospheric limit; the real 10 m limit would never bind at bench scale |
  | `initial_prime` | enum `SiphonPrime { Dry, Primed }` | closed | Dry | — | **proposed**: lets a level author a pre-primed siphon; Primed adds the column volume to the initial inventory |

- **Cosmetic curves and UI bindings.** Column fill follows the committed state and in-tube volume ("Column fill ← state", [component research](../../component-research.md#water)). Flowing shows moving bands at committed flow rate; Broken shows an air gap at the crest. No easing curve.
- **Art.** Clear tube shell in Clear-pipe cyan `#66b8c9` at shell alpha 0.16 with cream collars `#fff8e9` (palette of the Clear pipe, [DESIGN colour system](../../../DESIGN.md#colour-system)); liquid cyan `#66b8c9` opaque; a small gold `#f7cb52` cap on the `WaterPrime` port. **Proposed** within the palette.
- **Catalogue and inventory entry.** Id `siphon`, title "Primed siphon", category Water (**proposed**; see [EL-023](EL-023-archimedes-screw.md)). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-024 is one declaration. `initial_prime` is a configuration of the same part. The priming bulb is a separate identity (EL-168).

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-024`): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Static Box colliders and materials: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`.
- Counted inventory and save codec: `engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`.

**Missing**
- FluidAdvection and liquid FiniteLedger (S418), PressureWork head relation (S419): owner [S416](../invest/decisions.md#s416); unscheduled.
- Siphon state machine, end-seal priming rule and column-break rule: S445-D (map row: "priming, continuous column and pressure/height break; not merely a connected flow edge"); unscheduled.
- EnvironmentState world atmosphere bounding `max_crest_lift`: [S470](../invest/decisions.md#s470) open-versus-sealed; unscheduled.
- Water ports and connections (TopologyTransaction): S416 with [S257](../invest/decisions.md#s257); unscheduled.

**Element dependencies.** A source store (EL-001 or EL-004) and a lower receiver that covers the outlet, or an outlet seal (EL-012, a closed EL-167 tap); a primer (EL-168 Siphon priming bulb or EL-022 Water pump) unless `initial_prime` = Primed.

## 4. Sources and legacy

- **Requirement row** ([element-024](../requirements.md#element-024)): "Continuous liquid column carries flow while the source and head permit." Outcome: "Breaking the column stops the siphon; it cannot lift indefinitely above available pressure." No variants.
- **Named entry** ([element-024](../invest/named-elements.md#element-024)): owner S445.
- **Integration task** [sequence-task-389](../requirements.md#sequence-task-389): "maintain/break a primed siphon". Refinement [S707 siphon](../invest/refinements.md#s707): "Require priming and continuous geometry."
- **Research** ([component research](../../component-research.md#water)): Siphon/prime state Dry, Priming, Flowing, Broken; "Air entry breaks the column; the receiving surface must be lower"; recipe 7 "Prime Time": a hold timer briefly powers a priming pump, then the siphon continues to a lower receiver; intake exposure stops it. Chapter 7 "Go with the Flow" includes the story title "Siphon of relief" in the requirements chapter table.
- **Engine design** `docs/general-engine-design.md` fluid rows: "siphon continuity and pressure limit need their own adopted model"; owners S416, S418-D–S421-D.
- **Campaign**: first use 61–70, reuse 71–90, 114, 126–130, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched `parts/`, `parts/catalog/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at `a6c914e` for "siphon", "prime", "water", "liquid": no element source exists.

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-024 row](../requirements.md#element-024).
- **Chrome UI recipe.** In free play place a full Finite reservoir (EL-001) on a raised block and a second Finite reservoir lower on the bench with InitialVolume 0.125 m³ (so 0.125 m of liquid covers its floor), then place the siphon with its intake in the upper reservoir and its outlet 0.1 m below the lower reservoir's surface, using the real gizmo. Place a Siphon priming bulb (EL-168) and connect its `WaterPrimeOut` to the siphon `WaterPrime` with the contextual connection UI. Run, then squeeze the bulb through its physical actuator (for example a pusher, CAT-039).
- **Positive.** After three full squeezes the state goes Dry → Priming → Flowing; the upper level falls and the lower rises by the same volume, continuing with no further priming.
- **Negative/control.** No priming: state stays Dry and nothing flows. Outlet open in air while priming: every squeeze draws air through the outlet, no column forms and the state stays Dry. Lower the source so its surface is below the receiver's: no flow. Raise the crest more than 1.5 m above the source surface: the column breaks.
- **Boundaries.** Source drawn down until the intake uncovers (Broken, flow stops, the leg liquid drains and stays counted); equal levels (flow tends to zero, no oscillating free energy); outlet sealed by an EL-012 cap while priming, then flowing once a downstream EL-167 tap opens; a pre-primed authored siphon with a dry intake breaks on the first tick.
- **Run/Reset.** State, column volume and both store levels restore exactly.
- **Save/Load.** Leg lengths, `crest_span`, `initial_prime`, pose and water links round-trip; unknown enum values reject atomically.
- **Integrations.** EL-168 bulb, EL-022 pump with a hold timer (CAT-033), EL-030 Flow meter on the outlet.

## 6. Open questions

1. The game-scale crest-lift limit (1.5 m proposed) versus a world atmospheric pressure from EnvironmentState: owner decision with S470.
2. Whether `initial_prime` = Primed is allowed in player inventory or only in authored fixtures: owner decision.
3. Whether a broken siphon can re-prime during the same Run from a re-covered intake without a primer: S445-D decision (proposed: no).
4. Whether a sealed-outlet prime (cap or closed tap) is required in the first lesson or a submerged outlet suffices: owner decision.
5. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
6. Shared static water-vessel material (restitution 0.12, friction 0.3, proposed): owner decision.
