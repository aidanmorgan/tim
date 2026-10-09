# EL-119 · Dye station — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-119 · Dye station · Material |
| Anchor | [requirements.md#element-119](../requirements.md#element-119); [named-elements entry](../invest/named-elements.md#element-119); umbrella [gap-08](../requirements.md#gap-08) (index only) |
| Related identities | [EL-118 Coating station](EL-118-coating-station.md) (same arch, surface response instead of colour); the colour filters [CAT-055](CAT-055-red_filter.md)/[CAT-031](CAT-031-green_filter.md)/[CAT-011](CAT-011-blue_filter.md) are optical channels, explicitly not dye. No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 94 "Coat of Many Colours", practice 95, reuse 98, 136, 150 ([gap-08](../requirements.md#gap-08)). |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Declaration

The requirements row names no variants. The pigment colour is a typed parameter of the one mode; each colour is a value, not a separate element.

- **Bodies and shapes.** Static arch over a shallow basin, identical geometry to EL-118 so the two read as one family.
  - **Proposed** 1.6 × 1.2 m footprint, posts 0.15 × 1.0 × 1.2 m, lintel 1.6 × 0.15 × 1.2 m, basin 1.6 × 0.06 × 1.2 m, application zone 0.25 m deep — shared with EL-118 for one art kit.
- **Mass and material.** Static. **Proposed** basin friction 0.3, restitution 0.1 — as EL-118.
- **Constraints.** None.
- **Typed ports.** None.
- **Sensors and activation.** Zone sensor on the basin for any dynamic cargo whose material declares `Dyeable`.
  - **Proposed** full colour change after 0.5 s cumulative contact; less leaves the previous colour state — same rule as EL-118 so lessons teach one dwell idea.
  - Illumination by a Flashlight or Laser never changes colour state: "optical illumination is not dye" ([element-119](../requirements.md#element-119)).
- **Work and energy stores.** Finite pigment ledger. **Proposed** capacity 3 doses — matches EL-118 so both stations deplete on the same count.
- **Material state.** Dyed cargo carries `DyeColour`. It changes declared appearance and any colour-reading goal predicate; it does not change friction, restitution or mass ("do not assume structural coating/fracture changes", [element map](../general-engine-element-map.md)).
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `pigment` | enum `DyeColour { Red, Green, Blue }` | closed set | `Red` | — | **proposed** — reuses the established red/green/blue accents (`DESIGN.md@a6c914e:L158-L158`, the filters reuse them, `DESIGN.md@a6c914e:L343-L343`), so no new palette is introduced |
  | `supply` | u32 | 0–3 | 3 | doses | **proposed** (see above) |

- **Supported materials.** **Proposed** `Dyeable` only on wooden/ceramic cargo (Domino-type boxes); balls reject dye and pass unchanged — balls already carry fixed catalogue identity colours (`DESIGN.md@a6c914e:L165-L168`) that dye must not overwrite.
- **Cosmetic curves and UI bindings.** Basin fill ← committed pigment remaining; cargo tint ← committed `DyeColour` plus a navy pattern stamp per colour (one, two or three bars) so state is not colour-only ([gap-08 visual style](../requirements.md#gap-08)).
- **Art.** Cream arch `#fff8e9`, cyan basin `#66b8c9`, gold lip `#f7cb52`, navy stamp `#293954`; pigment tints coral `#de7058`, green `#62aa78`, blue `#5b9cdb` (`DESIGN.md@a6c914e:L147-L158`, `DESIGN.md@a6c914e:L172-L172`).
- **Catalogue and inventory.** **Proposed** id `dye_station`, title "Dye station", category Materials; appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): FiniteLedger, FluidAdvection, GeometryQuery, MaterialCoating, StateTransaction, TopologyTransaction.

- **Exists now.** Static bodies, residence sensors (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L122-L141`), finite per-owner reservoirs (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L10-L38`).
- **Missing.**
  - Per-body mutable appearance/material state committed by the worker and read by the renderer and goals: owner S673; the S543 coating row covers surface response, not colour ([decisions](../invest/decisions.md#s543)).
  - Any-cargo zone sensor with per-body accumulation (shared with EL-118): owner S673.
  - FluidAdvection for pigment as liquid: S416 ([decisions](../invest/decisions.md#s416)); proposal uses discrete doses.
- **Dependencies.** A colour-reading goal predicate (material-state delivery goal); Domino-type cargo (CAT-023).

## 4. Sources and legacy

- **Requirements.** Row: "Finite pigment transport changes a supported material's colour state"; outcome "No pigment means no colour change; optical illumination is not dye" ([element-119](../requirements.md#element-119)). Integration as EL-118 ([gap-08](../requirements.md#gap-08)).
- **Audit.** "explicit material-state transition, distinct from optical colour channels" (`docs/physics-puzzle-gap-audit.md@a6c914e:L87-L87`).
- **Legacy.** None. Legacy colour handling was optical only (`engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`); no material colour state existed. Not harvested: no element knowledge.

## 5. Acceptance outline

Point of truth: [element-119](../requirements.md#element-119) and the [gap-08 integration](../requirements.md#gap-08).

- **Chrome recipe.** Rotate a Dye station (pigment Blue) to a 30° tilt so its basin is a slide; continue it with a 30° Ramp ending at a Receiver whose goal requires blue cargo; place a Domino lying flat (broad face down, long axis along the slope, as in [EL-118](EL-118-coating-station.md)) at rest at the basin's upper end.
- **Positive.** The Domino slides through the basin in about 1.57 s (≥ 0.5 s; pair friction 0.424 < tan 30°), turns blue with a three-bar stamp, continues down the ramp and satisfies the blue-cargo goal.
- **Negative or control.** Supply 0: cargo arrives undyed and fails the goal. A blue Laser shone on an undyed Domino leaves it undyed. A Basketball through the basin stays orange.
- **Boundaries.** Fourth cargo after three doses is unchanged; pigment outside the enum rejected at the boundary.
- **Run/Reset.** Reset restores supply and every cargo's colour to undyed.
- **Save/Load.** Pigment and supply survive save; dye state is runtime-only.
- **Integrations.** Coating/dye integration task [sequence-task-541](../requirements.md#sequence-task-541) (gap-08: treated cargo satisfies a downstream material-state goal, untreated fails, depletion accounted); interaction row IX-36 material coating ([interaction-36](../requirements.md#interaction-36)) for the conserved deposition accounting (dye changes appearance only); the optical control uses IX-13 optical transport ([interaction-13](../requirements.md#interaction-13)). Campaign first use: GAP-08 row of the [campaign allocation](../requirements.md#campaign-gap-allocation) — introduction 94 (dye has its own objective), practice 95, reuse 98, 136, 150.

## 6. Open questions

1. Pigment set (proposed Red/Green/Blue) and whether dyes mix (red over blue). Unspecified — owner decision.
2. Which cargo materials are `Dyeable`.
3. Whether a dyed surface changes optical absorption/reflection (interaction with Epic 13 receivers).
