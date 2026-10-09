# EL-035 · Wick named-identity spec

This is the Story 7.0 full spec for named identity EL-035. The baseline is commit `a6c914e`; every citation uses `path@a6c914e:Lstart-Lend`. Values without a source are marked **proposed**, each with a one-line justification; the owner may revise them.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-035 |
| Name | Wick |
| Type | Water |
| Anchor | [requirements.md#element-035](../requirements.md#element-035); [named-elements.md#element-035](../invest/named-elements.md#element-035); scope index [todo-340](../requirements.md#todo-340) |
| Proof owner | S456 |
| CAT spec refined | none |
| Related identities | [EL-034 Sponge](EL-034-sponge.md), [EL-171 Squeeze pad](EL-171-squeeze-pad.md) (porous siblings); EL-001 Finite reservoir and EL-004 Catch basin (source and receiver); [EL-003 Tap](EL-003-tap.md) (reference flow); TH-28 Evaporative cooling pad (thermal wick body) |
| Roadmap story | unscheduled |
| Status | not started (no `WorkshopPartKind` member, `engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`) |

## 2. Declaration

- **Bodies and shapes.** One static porous strip (part root): a Box `length` × 0.04 × 0.15 m, bent at authoring into a source leg, a span and a delivery leg by its two end poses. Art is a braided cord; collision is the strip boxes. **Proposed**: thin enough to read as cloth, wide enough to see a wet front.
- **Mass and material.** Static. Restitution 0.0, friction 0.7 (**proposed**: soft fabric; deliberately not the shared hard water-vessel material, because the wick is cloth).
- **Liquid.** The wick retains liquid along its length: 0.004 m³ per metre (**proposed**), all of it in the ledger. Family density ρw = 16 kg/m³ (**proposed**, shared with Batch F; see [EL-023](EL-023-archimedes-screw.md)), so a saturated default wick holds 0.064 kg.
- **Reference flow.** EL-003's law Q = Cd·s·A·√(2·g·Δh) with Cd 0.6, the standard 0.10 m bore (A = 0.0314 m²), s = 1 and Δh = 1 m gives about 0.083 m³/s ([EL-003](EL-003-tap.md); values **proposed** there). The wick `rate` below is set against it.
- **Constraints.** None.
- **Typed ports.**

  | Port | Domain | Direction | Local position | Notes |
  | --- | --- | --- | --- | --- |
  | `WickInlet` | Water | Input | source-leg tip | Draws only while immersed below a store's free surface |
  | `DripOutlet` | Water | Output | delivery-leg tip | Non-joinable free-stream emitter; drips into whatever store or free space lies beneath |

  **Proposed**. These are geometric ends, not standard joinable mouths; they snap to nothing. Names follow the water family's style for non-joinable ends (Batch F's `LeakOutlet`, `JetOutlet`).
- **Sensors and activation.** None. Wet front position and retained volume are observable.
- **Work and energy stores.** None. Bounded capillary rule (**proposed**, for decision S421): the wet front advances from an immersed source end at up to `rate`; liquid reaches the delivery end only if the highest point of the wick stands no more than `max_rise` above the source free surface; transfer Q = min(`rate`, available source volume / Δt) once the wick is saturated, and only from source end to delivery end, never driven backwards by reversed head. When the source end leaves the liquid, transport stops; liquid already in the wick stays counted and drains only the saturated excess.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source / justification |
  | --- | --- | --- | --- | --- | --- |
  | `length` | f32 | 0.5–2.0, step 0.25 | 1.0 | m | **proposed**: spans a 0.12 m wall between neighbouring basins |
  | `max_rise` | f32 | fixed per material | 0.6 | m | **proposed**: bounded capillary lift, half a tank height, so a wick cannot replace a pump |
  | `rate` | f32 | fixed per material | 0.004 | m³/s | **proposed**: about one-twentieth of the 0.083 m³/s reference tap flow, so wicking reads as slow seepage; a 2⁻⁴ m³ step (1 kg) takes about 16 s |

- **Cosmetic curves and UI bindings.** Wet front and saturation follow committed retained volume along the length; a cyan drip at the delivery end while transferring. Pattern change (darkened weave) carries the wet state as well as colour. No easing.
- **Art.** Cream ribbed cord `#fff8e9` darkening toward cyan `#66b8c9` where wet, navy end ferrules `#293954` (TH-28's "cream ribbed wick above a cyan reservoir" is a compatible visual precedent, [requirements TH-28](../requirements.md#thermal-28)). **Proposed**.
- **Catalogue and inventory entry.** Id `wick`, title "Wick", category Water (**proposed**). Counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants; EL-035 is one declaration.

## 3. Engine capabilities

Families ([element-map row](../general-engine-element-map.md), [coverage binding](../../coverage/engine/element-01.json) `element-035`): CapillaryTransport, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction (coverage only), TopologyTransaction.

**Exists now**
- Static Box colliders: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`.
- Counted inventory and save codec: `engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`, `engine/gpu/WorkshopSaveCodec.cs@a6c914e:L6-L10`.

**Missing**
- Bounded capillary transport (CapillaryTransport): [S416 capillary](../invest/decisions.md#s416) → S421, whose Chrome construction is exactly "one reservoir and porous path: the wick wets up to its limit, a dry reservoir wets nothing, reversed head does not lift"; unscheduled.
- Liquid stores and immersion queries at the wick ends (S418, GeometryQuery): unscheduled.

**Element dependencies.** A source store (EL-001, EL-004, EL-026) and a receiver (EL-004).

## 4. Sources and legacy

- **Requirement row** ([element-035](../requirements.md#element-035)): "Transports liquid through a declared porous medium under bounded capillary rules." Outcome: "Dry reservoir stops transport; mass remains accounted for." No variants.
- **Decision** [S416 capillary](../invest/decisions.md#s416): "Which bounded wick model and capacity limit are adopted?"; next implementation S421.
- **Integration task** [sequence-task-531](../requirements.md#sequence-task-531): wicks remain individually tracked.
- **Research** ([component research](../../component-research.md#water)): "Absorbing store + bounded capillary transfer + compression input"; "Damp fraction ← volume".
- **Campaign**: first use 81–90, reuse 91–100, 131–140, 146–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- **Legacy.** None found. Searched the Epic 7 deletion scope at `a6c914e` for "wick", "capillary", "porous", "water": no element source.

**Files harvested:** none.

## 5. Acceptance outline

Follow the [EL-035 row](../requirements.md#element-035) and the S421 construction.
- **Chrome UI recipe.** In free play place a Catch basin with 0.1 m³ and an empty Catch basin beside it, then place the wick with the real gizmo so its source end dips into the full basin and its delivery end hangs over the empty one. Run.
- **Positive.** A wet front climbs the wick; once saturated it drips into the second basin at up to 0.004 m³/s; the first basin loses exactly what the second basin and the wick gain.
- **Negative/control.** Dry source basin: the wick stays dry and nothing is delivered. Crest more than 0.6 m above the source surface: the front stalls and nothing is delivered.
- **Boundaries.** The source drawn down until the end is exposed (transport stops; wick liquid stays counted); delivery end higher than the source end but within `max_rise` (slow transfer, never a reversed flow); a full receiver overflows conservatively.
- **Run/Reset.** Wet front, retained volume and both basins restore exactly.
- **Save/Load.** End poses, `length` and pose round-trip.
- **Integrations.** EL-034 Sponge (a wick feeding a sponge), a tipping bucket fed slowly by a wick (EL-025).

## 6. Open questions

1. Whether the delivery end may sit above the source end at all (proposed: yes, within `max_rise`), given S416's "reversed head does not lift": S421 decision.
2. Whether `max_rise` and `rate` are fixed material constants (proposed) or player parameters: owner decision.
3. Whether the wick may hang freely as a rope-like body: owner decision (static strip proposed).
4. Liquid density ρw = 16 kg/m³ (proposed) is one water-family constant shared with Batch F's EL-001–EL-022 specs: owner decision under S418/S420.
