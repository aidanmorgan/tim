# EL-118 · Coating station — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-118 · Coating station · Material |
| Anchor | [requirements.md#element-118](../requirements.md#element-118); [named-elements entry](../invest/named-elements.md#element-118); umbrella [gap-08](../requirements.md#gap-08) (index only) |
| Related identities | [EL-119 Dye station](EL-119-dye-station.md) (same arch; dye changes colour, not surface response); [GAP-09 Fragmentation station](GAP-09-fragmentation-station.md) (fragments inherit coating); thermal coating questions under S543. No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 94 "Coat of Many Colours", practice 95, reuse 98, 136, 150 ([gap-08](../requirements.md#gap-08)). |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The requirements row names no variants; there is one mode with one adopted coating material. Each further coating material is a later, separately specified mode.

- **Bodies and shapes.** One static body: an arch of three boxes over a shallow basin.
  - **Proposed** footprint 1.6 m (X, travel direction) × 1.2 m (Z); two side posts 0.15 × 1.0 × 1.2 m; top lintel 1.6 × 0.15 × 1.2 m; basin floor 1.6 × 0.06 × 1.2 m — a Basketball (0.68 m diameter) and a flat-lying Domino pass under it with clearance.
  - The application zone is the basin volume up to 0.25 m above the floor (the "application lip"), declared as a sensor volume in the station frame. The station may be rotated like any part, so its basin floor can itself be a slide.
- **Mass and material.** Static. **Proposed** basin friction 0.3, restitution 0.1 — the shared static-surface friction (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L90-L90`), damped so cargo does not bounce out of the basin.
- **Constraints.** None.
- **Typed ports.** None in this mode. **Proposed** no supply refill port — supply is finite per Run.
- **Sensors and activation.** A zone sensor on the application volume for any dynamic cargo whose material declares `Coatable`.
  - **Proposed** coverage rule: coverage accumulates with cumulative contact time inside the zone; full coat at 0.5 s cumulative contact; below that the cargo is `PartiallyCoated` and does not satisfy a "coated" predicate — a deliberate slide through the basin lasts over a second (section 5), while a corner graze does not.
- **Work and energy stores.** A finite coating supply ledger (FiniteLedger).
  - **Proposed** capacity 3 full coats; a partial coat debits its fraction — "Depletion prevents further treatment and application accounts for consumed supply" ([gap-08](../requirements.md#gap-08)) needs a small, countable number.
- **Material state.** Coated cargo carries `CoatingState { None, Partial, Full }` and `CoatingMaterial`. **Proposed** the single adopted material `Slick`, which sets the coated body's declared friction to 0.05.
  - Pair friction is the geometric mean (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L644-L644`). A Domino (friction 0.6, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`) on a Ramp or basin (0.3) has μ = √(0.6 × 0.3) = 0.424 and slides only above 23.0°; coated, μ = √(0.05 × 0.3) = 0.122 and it slides above 7.0°.
  - The cargo lies flat: broad 1.1 × 0.65 m face down, 0.25 m thickness vertical, long axis along the slope. Upright, with its 0.25 m thin axis along a slope, a Domino tips once tan θ > 0.125 / 0.55 = 0.227 (12.8°), so a 15° test would topple it rather than test friction. Lying flat it would need tan θ > 0.55 / 0.125 = 4.4 to tip, which never happens on a ramp.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `material` | enum `CoatingMaterial { Slick }` | closed set | `Slick` | — | **proposed** (see above) |
  | `supply` | u32 | 0–3 | 3 | full coats | **proposed** (see above) |

- **Cosmetic curves and UI bindings.** Fill line in the basin ← committed remaining supply; coated cargo shows a navy pattern stamp ← committed `CoatingState` (pattern, not colour alone) ([gap-08 visual style](../requirements.md#gap-08)).
- **Art.** Cream arch `#fff8e9`, cyan basin `#66b8c9`, gold application lip `#f7cb52`, navy pattern stamp `#293954` (`DESIGN.md@a6c914e:L147-L154`, `DESIGN.md@a6c914e:L172-L172`).
- **Catalogue and inventory.** **Proposed** id `coating_station`, title "Coating station", category Materials (a new palette category for material-state elements); appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): FiniteLedger, FluidAdvection, GeometryQuery, MaterialCoating, StateTransaction, TopologyTransaction.

- **Exists now.** Static box bodies; residence sensors with dwell in a static frame (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L122-L141`); a per-owner finite contact-work reservoir whose target set can be every dynamic body (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L10-L38`), used by the Bumper (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L119-L126`).
- **Missing.**
  - MaterialCoating: a per-body mutable material state and a runtime material replacement in the solver. Today materials are immutable per document (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L163-L192`). Decision owner S543 "coating" row (named next implementation S562, [decisions](../invest/decisions.md#s543)).
  - Residence sensors accept one named dynamic target per sensor (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L122-L141`); an any-cargo zone with per-body accumulation is missing. Owner S672.
  - FluidAdvection (coating as transported liquid): S416/S470 ([decisions](../invest/decisions.md#s416)); the proposal above treats supply as discrete coats instead.
- **Dependencies.** A material-state goal predicate ([EL-123](EL-123-protected-end-state-goal.md) or a coated-delivery goal); a Ramp (CAT-054) for the slide control; a Domino (CAT-023) as cargo.

## 4. Sources and legacy

- **Requirements.** Row: "Finite contact-transferred material forms a declared surface layer"; outcome "Absent supply or missed contact leaves cargo untreated" ([element-118](../requirements.md#element-118)). Integration: treated cargo satisfies a downstream material-state goal, untreated fails; depletion and partial contact without silently granting complete coverage ([gap-08](../requirements.md#gap-08)).
- **Audit.** "Finite material supply and explicit material-state transition, distinct from optical colour channels" (`docs/physics-puzzle-gap-audit.md@a6c914e:L87-L87`).
- **Element map.** "Conserved coating deposition updates surface response with material identity/revision and work/source limits" ([element map](../general-engine-element-map.md)).
- **Legacy.** None. The only "coating" in the legacy is the optical beam-splitter surface (`engine/OpticalNetwork.cs@a6c914e:L96-L96`), unrelated.

## 5. Acceptance outline

Point of truth: [element-118](../requirements.md#element-118) and the [gap-08 integration](../requirements.md#gap-08).

- **Chrome recipe.** Rotate a Coating station to a 30° tilt so its 1.6 m basin floor is the first slide; continue its lower lip with a 3 m Ramp at 15° ending at a Receiver; place a Domino lying flat at rest at the basin's upper end.
- **Positive.** On the 30° basin the box accelerates at 9.81 × (sin 30° − 0.424 cos 30°) = 1.30 m/s², taking 1.57 s to cross 1.6 m (≥ 0.5 s, full coat), and exits at 2.0 m/s. Coated (μ 0.122 < tan 15° = 0.268) it keeps accelerating down the 15° ramp into the Receiver.
- **Negative or control.** Supply 0, or the same box started on an adjacent 30° Ramp beside the station (missed contact): it arrives uncoated at the same 2.0 m/s, decelerates at 9.81 × (0.424 cos 15° − sin 15°) = 1.48 m/s² and stops after about 1.4 m, short of the Receiver. Static check: an uncoated box placed at rest on the 15° ramp stays; a coated one slides.
- **Boundaries.** Fourth cargo after three full coats is untreated; supply never negative; a box that grazes only a corner of the zone for under 0.5 s is `Partial` and fails the coated predicate.
- **Run/Reset.** Reset restores supply and every cargo's `CoatingState` to `None`.
- **Save/Load.** Supply and material survive save; coating state is runtime-only and starts `None`.
- **Integrations.** Coating/dye integration task [sequence-task-541](../requirements.md#sequence-task-541) (gap-08: treated cargo satisfies a downstream material-state goal, untreated fails, depletion and partial contact accounted); interaction rows IX-36 material coating ([interaction-36](../requirements.md#interaction-36)) and IX-02 sliding friction ([interaction-02](../requirements.md#interaction-02)) for the Slick effect. Campaign first use: GAP-08 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — introduction 94, practice 95, reuse 98, 136, 150.

## 6. Open questions

1. Coating material set and their physical effects (Slick only, or also Grip/thermal). Unspecified — owner decision.
2. Coverage model: time-in-zone (proposed) versus contact area.
3. Whether a coat wears off with sliding distance or persists for the Run.
4. Which cargo kinds are `Coatable`.
