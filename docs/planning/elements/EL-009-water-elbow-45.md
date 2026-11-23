# EL-009 · 45-degree water elbow — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-009 delivery slice](../invest/named-elements.md#element-009). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md); the pipe-kit values are in [EL-008](EL-008-straight-water-pipe.md). Sizes are in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-009 · 45-degree water elbow · Water |
| Anchors | [element-009](../requirements.md#element-009), co-anchored `sequence-task-562`; source record [todo-335](../requirements.md#todo-335); integration [sequence-task-385](../requirements.md#sequence-task-385); ledger [element-009](../invest/named-elements.md#element-009), owner S430 |
| Disposition | Member of the Family slice **"Water elbows"** with its one sibling [EL-010](EL-010-water-elbow-90.md) ([delivery batches 1a](../invest/delivery-batches.md#1a-families-one-slice-each-if-the-probe-passes)). The two differ only in declared values: arc angle, mouth poses, chevron count, id and title. Not a duplicate of the delivered 45° ball bend ([CAT-049](../requirements.md#current-cat-049), `pipe_bend_45`): different bore and a liquid fitting |
| Planned package | id `water_elbow_45`, directory `elements/WaterElbow45/`, title "45° water elbow" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | [EL-008](EL-008-straight-water-pipe.md), [EL-011](EL-011-water-t-junction.md), [EL-012](EL-012-water-pipe-cap.md), [EL-013](EL-013-water-nozzle.md) |

## Declaration

Closest delivered shape: the 45° ball bend, a static Torus-arc wall with two mouth locators (`elements/PipeBend90/PipeBend90ElementDeclaration.cs:9` describes the same form for 90°).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid fitting declaration of `ENGINE-LIQUID` (W4, W7).
- **Body.** One static body, mass 0. The arc lies in the local XY plane with its centre of curvature at (0, −0.40, 0); the origin is the midpoint of the centreline (the legacy bend's origin, row 2 below).
- **Collider (one).** Torus arc: centreline radius 0.40, bore 0.10, wall to 0.14, arc 45°.
- **Liquid node.** One fitting, zero volume, no seat.
- **Mouths (pipe bore, Bidirectional).** With half-angle 22.5°: `Start` at (−0.1531, −0.0304, 0), normal (−0.9239, −0.3827, 0); `End` at (+0.1531, −0.0304, 0), normal (+0.9239, −0.3827, 0). The normals are 45° apart in direction of travel. (Position: x = ±0.40·sin 22.5°, y = −0.40·(1 − cos 22.5°).)
- **Parameters.** None. The angle is the identity, not a setting.
- **Presentation.** None driven. **Palette.** `ElementCategory.Structure`; palette order 523 (kit sequence in EL-008; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cream.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Bend` | Torus arc as the collider, cream `#fff8e9` | solid, hollow; the Torus collider | body pose (static) |
| `Visual/Bands` | Navy `#293954` end bands, flush | decorative: printed flat | nothing |
| `Visual/Chevron` | One navy chevron on the outside of the bend, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end` | Locators, bore 0.10 | none | nothing |

Icon: a shallow cream bend with one chevron. It differs from the 90° elbow by its shallower arc and by one chevron against two, not by colour.

### Behaviour and numbers

- **Lossless turn (kit values).** A route through the elbow has the flow of the same route without it; only the elevations of its ends matter. Turned downward, the elbow's `End` datum is 0.40·(1 − cos 45°) = 0.117 m below its `Start`; any drop z between a route's first and last datum adds to the head, Q = (K₀/√2)·√(d + z). With both mouths at the same height (elbow lying flat), a full tank still empties in 33.9 s, as [EL-008](EL-008-straight-water-pipe.md).
- **Two 45° elbows equal one 90° elbow** in direction and, being lossless, in flow.
- **Wrong-facing or incompatible joins remain disconnected.** A mouth offered more than 20° off, or more than 0.20 m away, does not snap; two mouths count as joined only when coincident within 10⁻⁴ m and opposite within the delivered facing bound (L6); a mouth of another bore never joins. An unjoined neighbour stays a free end and spills there.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Torus-arc collider | present | body |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | fitting |
| W3 tube mouth, snap, facing and bore rules | narrower | join not compiled to an edge |
| W7 fittings and flow; W8 free end | missing | `ENGINE-LIQUID` |

Counts: 3 present, 1 untested, 1 narrower, 2 missing; one owner. The family needs no capability beyond EL-008's, which is what admits a Family slice.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, one collider, one fitting, two mouths.
2. **Generic laws.** Turning the flow is two mouths of one lossless fitting pointing different ways (L7). Refusing a wrong-facing join is the delivered snap rule (L6).
3. **Three layers.** *Collider:* one Torus arc. *Visuals:* the table above; bands and chevron are flush. *Animation:* none.
4. **One-way flow and typing.** Mouth ids and roles are enums.
5. **Flat-state counts.** Bodies 1 (static); colliders 1 Torus arc; joints 0; drives 0; sensors 0; liquid nodes 1 fitting; mouths 2; bindings 0; parameters 0.
6. **Performance fitness.** One collider against the delivered bend's five (arc, two stubs, two collars): no stubs or collars are drawn. Merged with its neighbours at compile (EL-008 point 6).
7. **Tests are scenarios.** Rows E1 to E5, each a theory row of the family class with this identity's own data.

## Sources and legacy

- Requirement: "Routes sealed flow through a 45-degree change"; outcome "Wrong-facing or incompatible joins remain disconnected." No variants.
- Component research [water](../../component-research.md#water) line 28; refinement [S705](../invest/refinements.md#s705) elbow45.
- **Legacy search.** No liquid elbow. Ball-bend facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Bend angle is a closed set {45, 90}; an undefined value is rejected. | `engine/MachineData.cs@a6c914e:L8-L8`; `parts/PipeBendPart.cs@a6c914e:L8-L8`, `:L29-L29` | Carried forward as two packages, not a parameter. |
| 2 | Origin at the centreline midpoint; mouths at the arc ends along the tangents. | `parts/PipeBendPart.cs@a6c914e:L9-L26` | Carried forward (without the collar offset: the kit has no collar). |
| 3 | Both angles snap mouth to mouth to straight pipes and to each other. | `CuriousContraptions.tests/PipeBendTests.cs@a6c914e:L195-L221` | Carried forward as row E4. |
| 4 | Snap refusals: distant, wrong-facing, occupied, locked, running. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L54-L121` | Carried forward; delivered (L6). |

**Files harvested:** `parts/PipeBendPart.cs`, `engine/MachineData.cs` (bend angle), `CuriousContraptions.tests/PipeBendTests.cs`.

## Acceptance outline

Authority: [element-009](../requirements.md#element-009). Tolerances L17. Planned class `WaterElbowTests` (one class for the family; every method is a theory with one row per identity, and the rows below are this identity's).

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| E1 | Given tank → level pipe → 45° elbow (lying flat) → pipe ending over a basin, when Run advances 10 s, then the basin's gain equals the tank's loss and the tank holds 0.497 m³ within 0.05 m³ (the EL-008 P1 value) | `WaterElbowTests.FlowTurnsThroughTheBendWithoutLoss` | E2 | — |
| E2 | Given the elbow offered to the pipe end 40° off its facing, then it does not snap; placed there anyway, the pipe end stays a free end, the liquid is spilled at it, and the basin beyond the elbow holds 0 | `WaterElbowTests.AWrongFacingElbowStaysDisconnected` | E1 | just inside 20° snaps |
| E3 | Given a delivered 45° ball bend (bore 0.65 m) brought to the water pipe end, then no join forms | `WaterElbowTests.ABallBendDoesNotJoinAWaterPipe` | the water elbow joins | — |
| E4 | Given two 45° elbows in series and one 90° elbow in a second lane, then the outlet directions agree within 1° and the tank volumes after 10 s agree within 0.01 m³ | `WaterElbowTests.TwoHalfTurnsEqualOneQuarterTurn` | — | — |
| E5 | Given a Tennis ball dropped on the bend, it rests on the wall (visible = solid); Reset restores the tank; save and load keep the pose and the joins | `WaterElbowTests.TheBendIsSolidAndTheRouteSurvivesResetAndReload` | — | — |

Generic cover: W22. The kit row of sequence-task-385 is in EL-008. This Family slice also owns EL-005's cross-part row F5 (the funnel, taught at 61b, cannot raise the head above its own rim through a pipe and elbows taught at 62). Chrome recipe (pending): tank, tap, pipe; drag a 45° elbow until it snaps; snap a second pipe; end over a basin.

## Open questions

All resolved; none needs the owner.

1. Centreline radius: 0.40 m, two bore diameters (D1).
2. Head loss per fitting: none (kit values).
3. A cue for a refused join: none on the part. A mouth that did not join is simply free; the Workbench's existing snap preview is the cue.

## Integration

- **Works with.** The pipe kit and every pipe-bore mouth; none delivered.
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-335`; `sequence-task-562` not co-anchored with a todo; no `s430` anchor).

## Delivery

- **Slice shape.** Family slice "Water elbows": EL-009 and EL-010 in one slice, each with its own rows, package and binding.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID`; EL-001; EL-008; EL-004; S7.
- **Files.** Adds `elements/WaterElbow45/` (`WaterElbow45.csproj`, `WaterElbow45ElementDeclaration.cs`, `water_elbow_45.tscn`, `icon.svg`), the sibling's directory, and one `CuriousContraptions.tests/WaterElbowTests.cs`; edits `CuriousContraptions.slnx` (two lines) and both specs.
- **Review tier.** Data-only.
- **Disqualifiers.** A Torus arc of 0.40 m centreline and 0.10 m bore refused by admission; the family probe finding any capability one sibling needs and the other does not (then two single slices).
