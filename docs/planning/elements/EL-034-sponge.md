# EL-034 · Sponge named-identity spec

This is the Story 7.0 full spec for named identity EL-034. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-034 |
| Name | Sponge |
| Type | Water |
| Anchor | [requirements.md#element-034](../requirements.md#element-034); [named-elements.md#element-034](../invest/named-elements.md#element-034); scope index [todo-340](../requirements.md#todo-340) |
| Proof owner | S455 |
| CAT spec refined | none |
| Related identities | [EL-035 Wick](EL-035-wick.md) and [EL-171 Squeeze pad](EL-171-squeeze-pad.md) (porous siblings, separate identities); EL-004 Catch basin and EL-001 Finite reservoir (liquid sources); TH-28 Evaporative cooling pad (thermal porous body) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** One dynamic Box body 0.4 × 0.2 × 0.3 m with chamfered art (`RigidBodyDeclaration` + `ColliderDeclaration`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`). **Proposed**: a hand-sized block that sits in a basin or on a Ramp.
- **Mass and material.** Dry mass 0.1 kg (**proposed**: lighter than the 0.4 kg Domino, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`). Retained liquid adds mass up to 0.356 kg saturated. Material restitution 0.02, friction 0.8 (**proposed**: soft, grippy, no bounce).
- **Liquid.** A porous store of capacity 0.016 m³ (porosity 2/3 of the 0.024 m³ body, **proposed**), retaining 0.256 kg of liquid at the **proposed** family density ρw = 16 kg/m³ (shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)). Saturated density 0.356 / 0.024 = 14.8 kg/m³, just under ρw, so a full sponge still floats, about 93% immersed.
- **Buoyancy clamp.** Force regions clamp acceleration to ≤ 64 m/s² ([capability inventory](../../gpu-f32-physics.md#capability-inventory), Force Regions row). The largest buoyant acceleration is the dry sponge held fully under: 16 × 0.024 × 9.81 / 0.1 = 37.7 m/s², under the clamp.
- **Immersion damping.** Linear damping c = 1.0 1/s scaled by immersed fraction f_sub, the water-family constant shared with the flotation specs (**proposed**; see [EL-028](EL-028-buoyant-platform.md)). A floating sponge bobs, with amplitude decaying as e^(−c·f_sub·t/2), τ = 2 / (c·f_sub): dry (0.1 of 0.384 kg displacement, 26% immersed) τ ≈ 7.7 s, 5% in about 23 s; saturated (93% immersed) τ ≈ 2.15 s, 5% in about 6.5 s. Held fully under, it does not bob; its velocity decays with time constant 1/c = 1 s.
- **Constraints.** None.
- **Typed ports.** None. Absorption happens through geometry: immersion in a store or interception of free-stream packets.
- **Sensors and activation.** None. Retained volume (damp fraction) is observable.
- **Work and energy stores.** None. Absorption rate = `absorb_rate` × immersed fraction of the body, plus every intercepted packet up to remaining capacity; it stops exactly at capacity, and the rest stays in the source or runs off. Retained liquid leaves only under compression: a normal contact load above 5 N starts expelling, 40 N expels to 10% retention (**proposed** linear ramp: a resting Basketball, 9.8 N, wrings a little; a Bowling ball, 39 N, wrings almost all), as drips from the lower face that stay in the ledger.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `absorb_rate` | f32 | fixed per material | 0.008 | m³/s at full immersion | **proposed**: saturates in about 2 s when dunked, readable but not instant |
  | `initial_damp` | f32 | 0–1 | 0 | fraction of capacity | **proposed**: authored levels may start with a wet sponge as a finite water source |

- **Cosmetic curves and UI bindings.** Damp fraction tints and darkens the sponge ("Damp fraction ← volume", [component research](../../component-research.md#water)); visible pores fill as a pattern so wetness does not rely on colour alone ([common visual contract](../requirements.md#individual-element-register)). Pose follows the committed body. No easing.
- **Art.** Gold-cream sponge `#f5b354` dry (the Spring/Flashlight hue) shading toward cyan `#66b8c9` pore fill when wet, navy pore outlines `#293954` ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.
- **Catalogue and inventory entry.** Id `sponge`, title "Sponge", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-034 is one declaration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-034`): CapillaryTransport, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Dynamic Box body, contact normal impulses (from which load is measured): `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L1271-L1279`.

**Missing**
- Bounded porous absorption with a capacity limit (CapillaryTransport): [S416 capillary](../invest/decisions.md#s416) → S421 ("the wick wets up to its limit, a dry reservoir wets nothing, reversed head does not lift"); unscheduled.
- Liquid store carried by a dynamic body with mass coupling: S418; unscheduled.
- Compression-driven release from contact load (PressureWork): S419; unscheduled.
- Body mass changing during Run (current `RigidBodyDeclaration.Mass` is fixed per Run, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60`): unscheduled.
- Displaced-volume buoyancy and immersion damping for a floating sponge: S420; unscheduled.

**Element dependencies.** A liquid source (EL-001, EL-004, EL-013 Water nozzle); for release, a heavy body (CAT-014 Bowling ball) or EL-171 press.

## 4. Sources and legacy

- **Requirement row** ([element-034](../requirements.md#element-034)): "Absorbs available liquid up to material capacity." Outcome: "Saturated sponge cannot remove additional volume." No variants.
- **Integration task** [sequence-task-531](../requirements.md#sequence-task-531): "Finite absorption/storage ... can be observed before it produces an action. ... sponges ... remain individually tracked".
- **Research** ([component research](../../component-research.md#water)): "Absorbing store + bounded capillary transfer + compression input"; parameter "absorption capacity"; "Absorbs finite water (adds mass); compression releases it"; "Damp fraction ← volume".
- **Campaign**: first use 81–90, reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); chapter 9 keeps "porous-water lessons".
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "sponge", "porous", "absorb", "water": no element source.

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-034 row](../requirements.md#element-034).
- **Chrome UI recipe.** In free play place a Catch basin (EL-004) holding 0.03 m³ (filled by a reservoir and tap), drop the sponge into it with the real gizmo, and place an empty second basin beside it. Run; after it soaks, drop a Bowling ball onto the sponge resting in the second basin (or carry it there by a scoop).
- **Positive.** The basin level falls by exactly the volume the sponge gains; the sponge darkens and gets heavier (up to 0.356 kg); squeezing releases liquid into the second basin.
- **Negative/control.** With more liquid than capacity, the sponge saturates at 0.016 m³ (0.256 kg) and the rest stays in the basin. A dry basin leaves the sponge dry.
- **Boundaries.** Partial immersion absorbs proportionally slower; a saturated sponge floats lower; release never exceeds retained volume; total liquid is conserved across absorb and release.
- **Run/Reset.** Retained volume, mass and pose restore exactly.
- **Save/Load.** Pose and `initial_damp` round-trip; out-of-range values reject.
- **Integrations.** EL-171 Squeeze pad, EL-035 Wick, a water-carrying route.

## 6. Open questions

1. Whether a sponge releases under contact load (proposed) or only inside an EL-171 press: owner decision.
2. Whether retained liquid drips slowly under gravity with no load: owner decision (proposed no).
3. Whether a sponge's changing mass is a per-tick mass update in the solver or a bounded set of mass steps: S418/S421 decision.
4. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
5. Immersion damping c = 1.0 1/s (proposed family constant): owner decision under S420.
