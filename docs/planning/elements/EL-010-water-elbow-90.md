# EL-010 · 90-degree water elbow — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-010 delivery slice](../invest/named-elements.md#element-010). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md); the pipe-kit values are in [EL-008](EL-008-straight-water-pipe.md). Sizes are in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-010 · 90-degree water elbow · Water |
| Anchors | [element-010](../requirements.md#element-010), co-anchored `sequence-task-563`; source record [todo-335](../requirements.md#todo-335); integration [sequence-task-385](../requirements.md#sequence-task-385); ledger [element-010](../invest/named-elements.md#element-010), owner S431 |
| Disposition | Member of the Family slice **"Water elbows"** with its one sibling [EL-009](EL-009-water-elbow-45.md). The two differ only in declared values: arc angle, mouth poses, chevron count, id and title. Not a duplicate of the delivered 90° ball bend ([CAT-050](../requirements.md#current-cat-050), `pipe_bend_90`) |
| Planned package | id `water_elbow_90`, directory `elements/WaterElbow90/`, title "90° water elbow" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | [EL-008](EL-008-straight-water-pipe.md), [EL-012](EL-012-water-pipe-cap.md) (blocks its outlet), [EL-002](EL-002-header-tank.md) (risers) |

## Declaration

Closest delivered shape: the 90° ball bend (`elements/PipeBend90/PipeBend90ElementDeclaration.cs:9`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid fitting declaration of `ENGINE-LIQUID` (W4, W7).
- **Body.** One static body, mass 0. The arc lies in the local XY plane, centre of curvature (0, −0.40, 0), origin at the centreline midpoint.
- **Collider (one).** Torus arc: centreline radius 0.40, bore 0.10, wall to 0.14, arc 90°.
- **Liquid node.** One fitting, zero volume, no seat.
- **Mouths (pipe bore, Bidirectional).** With half-angle 45°: `Start` at (−0.2828, −0.1172, 0), normal (−0.7071, −0.7071, 0); `End` at (+0.2828, −0.1172, 0), normal (+0.7071, −0.7071, 0). (x = ±0.40·sin 45°, y = −0.40·(1 − cos 45°).) Placed with one mouth horizontal, the other points straight down or up, 0.40 m away in each axis.
- **Parameters.** None.
- **Presentation.** None driven. **Palette.** `ElementCategory.Structure`; palette order 524 (kit sequence in EL-008; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cream.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Bend` | Torus arc as the collider, cream `#fff8e9` | solid, hollow; the Torus collider | body pose (static) |
| `Visual/Bands` | Navy `#293954` end bands, flush | decorative: printed flat | nothing |
| `Visual/Chevrons` | Two navy chevrons on the outside of the bend, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end` | Locators, bore 0.10 | none | nothing |

Icon: a cream quarter-turn with two chevrons. It differs from the 45° elbow by the quarter-turn outline and by two chevrons against one.

### Behaviour and numbers

- **Lossless turn (kit values).** The elbow changes direction and elevation only.
- **A drop adds head.** Tank → level pipe → elbow turned down → 1.0 m pipe → free end: the free end is 0.40 + 1.0 = 1.40 m below the tank floor (the elbow's own drop is 0.40 m). At 1.0 m depth Q = (K₀/√2)·√(1.0 + 1.40) = 0.05904 × 1.549 = **0.0915 m³/s**, against 0.0590 for a level route.
- **Blocking its outlet prevents through-flow.** With a cap on `End` the fitting has one opening only; the flows at a fitting sum to zero (L7), so nothing moves and the supply keeps its volume. Removing the cap while building leaves a free end again.
- **Riser.** Turned up, it starts the riser of [EL-002](EL-002-header-tank.md) rows H1 and H2.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Torus-arc collider | present | body |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | fitting |
| W3 tube mouth and snap | narrower | join not compiled to an edge |
| W7 fittings and flow; W8 free end | missing | `ENGINE-LIQUID` |

Counts: 3 present, 1 untested, 1 narrower, 2 missing; one owner.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, one collider, one fitting, two mouths.
2. **Generic laws.** The turn is L7 on a lossless fitting; the blocked outlet is the flow balance at a fitting with one opening.
3. **Three layers.** *Collider:* one Torus arc. *Visuals:* the table above; all marks flush. *Animation:* none.
4. **One-way flow and typing.** Mouth ids and roles are enums.
5. **Flat-state counts.** Bodies 1 (static); colliders 1 Torus arc; joints 0; drives 0; sensors 0; liquid nodes 1 fitting; mouths 2; bindings 0; parameters 0.
6. **Performance fitness.** One collider against the delivered bend's five. Merged with its neighbours at compile.
7. **Tests are scenarios.** Rows E1 to E6, this identity's theory rows of the family class.

## Sources and legacy

- Requirement: "Routes sealed flow through a right angle"; outcome "Blocking its outlet prevents through-flow." No variants.
- Component research [water](../../component-research.md#water) line 28; refinement [S705](../invest/refinements.md#s705) elbow90.
- **Legacy search.** No liquid elbow. The ball-bend facts are rows 1 to 4 of [EL-009](EL-009-water-elbow-45.md), plus:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 5 | The legacy default bend is 90°. | `parts/PipeBendPart.cs@a6c914e:L8-L8` | Recorded; each angle is its own package. |
| 6 | An occupied mouth is not a join candidate; deleting the occupant frees it. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L103-L121` | Carried forward; delivered (`TubeMouthSnapTests.AnOccupiedMouthIsNotACandidate`). The cap in row E2 occupies the outlet. |

**Files harvested:** as EL-009, plus `parts/catalog/pipe_bend_90.tres` (not carried; CAT-050 owns it).

## Acceptance outline

Authority: [element-010](../requirements.md#element-010). Tolerances L17. Planned class `WaterElbowTests` (family class; these are this identity's rows).

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| E1 | Given tank → level pipe → 90° elbow (lying flat) → pipe ending over a basin, when Run advances 10 s, then the basin's gain equals the tank's loss and the tank holds 0.497 m³ within 0.05 m³ | `WaterElbowTests.FlowTurnsThroughTheBendWithoutLoss` | E2 | — |
| E2 | Given a cap ([EL-012](EL-012-water-pipe-cap.md)) on the elbow's `End`, when Run advances 15 s, then the tank keeps 1.0 m³, no aperture reports flow and `Spilled` is 0 | `WaterElbowTests.ABlockedOutletPreventsThroughFlow` (a cross-part row owned by the EL-012 slice: the cap is taught at 62a, after the elbow at 62) | cap deleted while building: the free end discharges | — |
| E3 | Given the elbow turned down into a 1.0 m pipe, then the starting flow is 0.0915 m³/s within 10% | `WaterElbowTests.ADownwardTurnAddsItsDropToTheHead` | flat: 0.0590 m³/s | — |
| E4 | Given the elbow offered 40° off the pipe's facing, then it does not snap and the pipe end stays free | `WaterElbowTests.AWrongFacingElbowStaysDisconnected` | E1 | just inside 20° snaps |
| E5 | Given a delivered 90° ball bend brought to the water pipe end, then no join forms | `WaterElbowTests.ABallBendDoesNotJoinAWaterPipe` | the water elbow joins | — |
| E6 | Given a Tennis ball dropped on the bend, it rests on the wall; Reset restores the tank; save and load keep the pose and joins | `WaterElbowTests.TheBendIsSolidAndTheRouteSurvivesResetAndReload` | — | — |

The half-turn equivalence row is EL-009 E4. Generic cover: W22. Chrome recipe (pending): Header tank outlet → pipe → 90° elbow → pipe ending over a basin.

## Open questions

All resolved; none needs the owner.

1. Shared centreline radius 0.40 m: yes, so a 90° bend equals two 45° bends in footprint.
2. Head loss per fitting: none (kit values).

## Integration

- **Works with.** The pipe kit; EL-002 risers. None delivered.
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-335`; `sequence-task-563` not co-anchored with a todo; no `s431` anchor).

## Delivery

- **Slice shape.** Family slice "Water elbows" with EL-009; see that spec for files (`elements/WaterElbow90/` with `WaterElbow90.csproj`, `WaterElbow90ElementDeclaration.cs`, `water_elbow_90.tscn`, `icon.svg`).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID`; EL-001; EL-008; EL-004; S7. Row E2 runs in the EL-012 slice. The Water elbows slice also owns EL-005's cross-part row F5.
- **Review tier.** Data-only.
- **Disqualifiers.** As EL-009.
