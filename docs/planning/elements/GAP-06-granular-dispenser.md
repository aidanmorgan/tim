# GAP-06 · Granular dispenser — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | GAP-06 · Granular dispenser · Gravity / fluid-flow neighbour (P3 potential) |
| Anchor | [requirements.md#gap-06](../requirements.md#gap-06); [named-elements entry](../invest/named-elements.md#gap-06) |
| Related identities | [GAP-07 Granular sieve](GAP-07-granular-sieve.md) (depends on it); [EL-120 Quantity goal](EL-120-quantity-goal.md) (mass/count units); containers [EL-192 Receiving basket](EL-192-receiving-basket.md) and [EL-069 Moving bucket](../invest/named-elements.md#element-069); weighing [CAT-052 Pressure plate](CAT-052-pressure_plate.md); the funnel [CAT-030](CAT-030-funnel.md) is a routing neighbour, not a dispenser. No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 91 "Against the Grain", practice 92, reuse 96, 136, 141 ([gap-06](../requirements.md#gap-06)). |
| Status | Not started. Disposition potential/conditional ([element map](../general-engine-element-map.md)). |

## 2. Declaration

The row requires "grain-size presets, outlet control and performance limits before campaign authoring" ([gap-06](../requirements.md#gap-06)). Each grain-size preset is specified separately; there is one dispenser mode.

- **Bodies and shapes.** A static tapered hopper of boxes (two sloped side walls, front and back walls) with a bottom outlet, plus a finite feed of discrete grains.
  - **Proposed** hopper 1.0 m wide × 1.0 m tall × 0.6 m deep, side walls sloped 30° from vertical, outlet 0.5 m × 0.6 m — holds a 12-grain Coarse feed (about 0.16 m³ packed) and sits beside a 1.5 m Receiver.
  - Grains: discrete dynamic spheres, never liquid and never infinite: "Granules remain discrete conserved material, not liquid or infinite repeated balls" ([gap-06](../requirements.md#gap-06)).
- **Preset `Fine`.** **Proposed** radius 0.0625 m (the minimum admitted sphere radius, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L100-L104`), mass 0.02 kg — a full 12-grain Fine feed (0.24 kg) stays below the Pressure plate's 0.5 kg default, so Fine grains never trip it.
- **Preset `Coarse`.** **Proposed** radius 0.125 m, mass 0.16 kg (8× Fine by volume) — three Coarse grains (0.48 kg) do not press a 0.5 kg plate and four (0.64 kg) do, a clean weighing control; twice the Fine diameter, so a GAP-07 slot can separate them.
- **Mass and material.** **Proposed** grain friction 0.6, restitution 0.05, rolling resistance 0.1 (the admitted maximum, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L52-L52`) — high friction and rolling resistance let grains pile rather than roll like balls. Hopper static, friction 0.3.
- **Constraints.** None.
- **Typed ports.** **Proposed** one `ActivationIn` socket that opens the outlet gate (activation domain exists, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`) — reuses the taught Switch wiring for outlet control.
- **Sensors and activation.** Outlet gate state `OutletState { Closed, Open }`: a static box across the outlet when Closed, removed when Open (a TopologyTransaction on the gate collider). A Closed gate is the "blocked aperture": the feed rests on it and nothing flows; opening it mid-Run is the physical route opening that clears the jam.
- **Work and energy stores.** Finite feed ledger: integer grain count per preset. Discharged + retained = initial feed exactly (integer conservation; mass = count × grain mass): the "explicit representation tolerance" is zero grains.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `grain` | enum `GrainSize { Fine, Coarse }` | closed set | `Fine` | — | **proposed** (above) |
  | `feed` | u32 | 0–12 | 8 | grains | **proposed** — 0 is the authored empty control; 12 grains plus up to 4 other dynamic bodies fit the current 16-body table (section 3); 8 leaves room for a ball and a Weight |
  | `outlet` | enum `OutletState { Closed, Open }` | closed set | `Closed` | — | **proposed** initial state so the player chooses when to release |

- **Cosmetic curves and UI bindings.** Fill line in the cyan window ← committed retained count; quantity legible at mobile size ([gap-06 visual style](../requirements.md#gap-06)).
- **Art.** Cream tapered vessel `#fff8e9`, cyan inspection window `#66b8c9`, gold outlet lip `#f7cb52`; grains in existing material colours, Fine in wood `#c28f52`, Coarse in domino cream `#e8d4a6` (`DESIGN.md@a6c914e:L147-L174`).
- **Catalogue and inventory.** **Proposed** id `granular_dispenser`, title "Grain hopper", category Materials (a discrete-material element even beside fluid tools, [requirements](../requirements.md#gap-06) palette note); appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/gap-01.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, GranularTransport, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Dynamic spheres with friction, rolling resistance and restitution (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L622-L765`); dynamic AABB tree broadphase (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L203`).
- **Capacity (named prerequisite).** 16 dynamic bodies and 33 bodies per document (`engine/gpu/PhysicsBodyReadSet.cs@a6c914e:L30-L30`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L166-L169`) and 32 instances (`engine/gpu/WorkshopInstances.cs@a6c914e:L48-L48`). Grains are bodies of one dispenser instance, so they cost dynamic-body rows, not instance rows; the dispenser's workbench footprint must count `feed` dynamic bodies (`engine/gpu/WorkbenchCapacity.cs@a6c914e:L30-L43`). The proposed 12-grain cap fits today. Any larger feed, or a granular table, is a capacity increase that needs an owner decision before it is adopted (owner S639); the performance budget is measured at its named qualification gate.
- **Missing.**
  - GranularTransport: decision owner LAW-GRANULAR-I ("Finite feed passes a wide opening and jams a narrow one; the count delivered never exceeds the count fed", [S635](../invest/decisions.md#s635)); owner S639.
  - Outlet gate collider removal mid-Run (TopologyTransaction).
  - Pressure plate (Story 9.1) and its battery supply (Story 8.1) for the weighing integration.
- **Dependencies.** A Switch (CAT-063) for outlet control; a Receiver (EL-192) as bucket; a Pressure plate (CAT-052) as weighing control.

## 4. Sources and legacy

- **Requirements.** "Discharged plus retained material equals initial feed within the explicit representation tolerance. Empty feed stops, a blocked aperture jams, and opening the physical route clears it. Verify interaction with buckets and weighing controls" ([gap-06](../requirements.md#gap-06)).
- **Audit.** "Deterministic conserved quantity, collisions and a measured performance budget. Empty source stops; jam persists until a physical change clears it" (`docs/physics-puzzle-gap-audit.md@a6c914e:L85-L85`).
- **Weighing source.** Pressure plate default threshold 0.5 kg minimum mass (`parts/catalog/pressure_plate.tres@a6c914e:L14-L14`).
- **Legacy.** None: no granular, grain or hopper code, test or level exists in the legacy tree.

## 5. Acceptance outline

Point of truth: [gap-06](../requirements.md#gap-06).

- **Chrome recipe.** Place a Grain hopper (feed 8) above a Receiver; wire a Switch to its `ActivationIn`; roll a ball onto the Switch.
- **Positive.** The ball strikes the Switch, the gate opens and grains flow into the Receiver (the bucket); delivered + retained = 8.
- **Jam and clear.** Before the Switch is struck the Closed gate blocks the outlet: grains rest on it and nothing flows. Opening it mid-Run clears the jam. Static variant: a Wall placed 0.1 m under the outlet stops the flow once grains pile on it; after Reset, moving the Wall aside and running again lets the feed flow.
- **Weighing integration.** Coarse feed of 4 released onto a supplied Pressure plate (0.5 kg): the plate stays open after 3 grains (0.48 kg) and closes with the 4th (0.64 kg). A full Fine feed of 12 (0.24 kg) never closes it.
- **Negative or control.** Feed 0 (empty): nothing flows even with the gate open. No Switch strike: the outlet stays closed.
- **Boundaries.** Feed 0 and 12 admitted; 13 rejected at the boundary; grain count never increases.
- **Run/Reset.** Reset restores the full feed inside the hopper and the closed gate.
- **Save/Load.** Preset, feed and wiring survive save and Load.

## 6. Open questions

1. Grain representation: individual dynamic spheres (proposed) versus a dedicated granular table. Unspecified — owner decision.
2. Capacity: whether feeds above 12 are adopted, which requires raising the dynamic-body table (a named prerequisite).
3. Grain presets and sizes (proposed Fine 0.0625 m, Coarse 0.125 m), and whether a mixed feed is a third preset (needed by GAP-07).
4. Outlet control: activation-opened gate (proposed) or a fixed aperture only.
