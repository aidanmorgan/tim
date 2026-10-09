# EL-171 · Squeeze pad named-identity spec

This is the Story 7.0 full spec for named identity EL-171. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-171 |
| Name | Squeeze pad |
| Type | Water |
| Anchor | [requirements.md#element-171](../requirements.md#element-171); [named-elements.md#element-171](../invest/named-elements.md#element-171); scope index [todo-340](../requirements.md#todo-340) |
| Proof owner | S464 |
| CAT spec refined | none. Related: [CAT-052 Pressure plate](CAT-052-pressure_plate.md) (load-on-plate precedent), [CAT-062 Spring](CAT-062-spring.md) (slider and spring, Story 6.4) |
| Related identities | [EL-034 Sponge](EL-034-sponge.md) and [EL-035 Wick](EL-035-wick.md) (porous siblings, separate); [EL-036 Sprinkler](EL-036-sprinkler.md) and EL-013 Water nozzle (wetting sources); EL-004 Catch basin (receiver) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** A static tray frame (part root): floor Box 0.7 × 0.06 × 0.7 m with four rim boxes 0.2 m high and a drip outlet in the floor. A porous pad (static store, art and a Box sensor volume 0.6 × 0.15 × 0.6 m) sits in the tray. A dynamic press plate: Box 0.6 × 0.04 × 0.6 m resting on the pad. **Proposed**: a 0.6 m plate is wide enough for a falling Basketball or Bowling ball to land on.
- **Mass and material.** Press plate 0.3 kg (**proposed**: light, barely pre-squeezes the pad). Plate restitution 0.05, friction 0.5 (**proposed**). Tray: the shared static water-vessel material, restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0 (**proposed** reuse of the Receiver material, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`, as Batch F's vessels use).
- **Resting position.** Compression x is measured downward from the plate's resting position: the plate lying on the uncompressed pad under its own weight only (the 2.9 N plate weight settles 0.007 m into the 400 N/m pad spring, and that settled pose is x = 0). Capacity, travel and expelled volume below are all referred to this rest pose.
- **Liquid.** Pad porous capacity at rest C(0) = 0.04 m³ (porosity 0.74 of the 0.054 m³ pad, **proposed**), i.e. 0.64 kg of liquid at the **proposed** family density ρw = 16 kg/m³ (shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). Retained capacity under compression x: C(x) = 0.04 × (1 − 0.9·x / 0.12) m³ (**proposed**: a full squeeze leaves 10% in the pad). Any retained volume above C(x) is expelled through the drip outlet.
- **Constraints.** One prismatic joint plate ↔ tray along local Y, travel x = 0–0.12 m below the resting position, with a 400 N/m spring precompressed by 0.0073575 m at x = 0, giving upward force 2.943 + 400x N to balance the 0.3 kg plate's gravity before a presser arrives. Proposed damping 100 N·s/m dissipates slider motion (overdamped for the plate plus a 4 kg Bowling ball); this prevents oscillatory pumping under a slowly applied load (**proposed**: a Bowling ball, 39 N, compresses x = 0.098 m and expels about 0.029 m³ from a full pad; a Basketball, 9.8 N, compresses 0.025 m and wrings a little). Story 6.4 slider and spring.
- **Typed ports.**

  | Port | Domain | Direction | Local position (m) | Notes |
  | --- | --- | --- | --- | --- |
  | `WaterOutlet` | Water | Output | (0, −0.06, 0) | Drip outlet; joins a standard water mouth or emits a free stream |

  **Proposed**; named with the water family's `WaterOutlet` (Batch F). Wetting enters through the open top (intercepted packets, a sponge or nozzle above). No signal or power port.
- **Sensors and activation.** None. Retained volume and plate position are observable.
- **Work and energy stores.** The spring holds ½·400·(x + 0.0073575)² J, including its proposed 0.0108 J admission preload. Its increase from x = 0 is 2.943x + 200x² J; the plate loses 2.943x J of gravitational potential, so net quasistatic presser work is 200x² J (2.88 J at full stroke). Dynamic pressing also dissipates the declared damper work; release can return only the remaining stored energy. Expelled liquid is never created: it is only ever the retained excess. On release the pad's capacity recovers but it reabsorbs only liquid that reaches it.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `initial_wet` | f32 | 0–1 | 0 | fraction of capacity at rest | **proposed**: authored levels may supply a wet pad as a finite water source (up to 0.64 kg) |

- **Cosmetic curves and UI bindings.** Plate follows the committed slider position; pad damp fraction tints and patterns the pad ("Damp fraction ← volume", [component research](../../component-research.md#water)); a cyan drip at the outlet while expelling. No easing.
- **Art.** Cream tray `#fff8e9`, gold-cream pad `#f5b354` shading to cyan `#66b8c9` when wet, navy press plate `#293954` with a gold grip `#f7cb52` ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `squeeze_pad`, title "Squeeze pad", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-171 is one declaration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-02.json) `element-171`): CapillaryTransport, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Static and dynamic Box bodies, contact normal impulses: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`.

**Missing**
- Prismatic slider with spring: Story 6.4.
- Porous store with compression-dependent capacity (CapillaryTransport + PressureWork): [S416](../invest/decisions.md#s416) capillary → S421 and pressure-work → S419; map row: "Mechanical compression and pressure/porous store coupling expel conserved liquid; capacity alone is insufficient". Unscheduled.
- Outlet free stream (S418): unscheduled.

**Element dependencies.** A wetting source (EL-036 Sprinkler, EL-013 nozzle, EL-034 Sponge); a presser (CAT-014 Bowling ball, CAT-039 Linear pusher, CAT-067 Weight); a receiver (EL-004).

## 4. Sources and legacy

- **Requirement row** ([element-171](../requirements.md#element-171)): "Mechanical compression expels retained liquid from a porous store." Outcome: "A dry pad cannot produce water and squeezing consumes work." No variants.
- **Map row**: "Source-specific composition: Mechanical compression and pressure/porous store coupling expel conserved liquid; capacity alone is insufficient."
- **Research** ([component research](../../component-research.md#water)): "Absorbing store + bounded capillary transfer + compression input"; "compression releases it".
- **Integration task** [sequence-task-531](../requirements.md#sequence-task-531): finite absorption/storage observable before it acts.
- **Campaign**: first use 81–90, reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "squeeze", "pad", "porous", "water": no element source. Pressure-plate legacy is harvested in [CAT-052](CAT-052-pressure_plate.md).

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-171 row](../requirements.md#element-171).
- **Chrome UI recipe.** In free play place the squeeze pad with `initial_wet` 1 set by the real parameter control, a Catch basin under its outlet, and a Ramp that rolls a Bowling ball onto the plate. Run.
- **Positive.** The ball lands, the plate settles about 0.1 m below its resting position, liquid drips into the basin by exactly the volume the pad loses. Quasistatic loading expels about 0.029 m³; a rolling impact can compress farther before settling, so compare yield to the maximum observed compression: 0.3 × x_max m³, bounded above by 0.036 m³ at the travel stop, and the ball's kinetic and potential energy pays the compression.
- **Negative/control.** A dry pad (`initial_wet` 0) squeezed by the same ball compresses identically and produces no liquid. A pad with no presser stays at its resting position and keeps its liquid.
- **Boundaries.** Basketball (0.025 m below rest, small yield); repeated presses yield nothing more once retained volume is below C(x); release lets the plate return to its resting position without drawing liquid back from the basin.
- **Run/Reset.** Plate position, retained volume and basin volume restore exactly.
- **Save/Load.** `initial_wet`, pose and the outlet link round-trip; out-of-range values reject.
- **Integrations.** EL-036 Sprinkler wetting the pad, EL-025 tipping bucket fed by the drip.

## 6. Open questions

1. Whether the pad reabsorbs liquid standing in its tray after release: S421 decision (proposed: only liquid that reaches the pad surface).
2. Whether the press can be driven through a mechanical shaft (crank) as well as contact load: owner decision.
3. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
4. Shared static water-vessel material for the tray (restitution 0.12, friction 0.3, proposed): owner decision.
