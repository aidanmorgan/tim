# EL-012 · Water pipe cap — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-012 delivery slice](../invest/named-elements.md#element-012). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md); the pipe-kit values are in [EL-008](EL-008-straight-water-pipe.md). Sizes are in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-012 · Water pipe cap · Water |
| Anchors | [element-012](../requirements.md#element-012), co-anchored `sequence-task-565`; source record [todo-335](../requirements.md#todo-335); integration [sequence-task-385](../requirements.md#sequence-task-385); ledger [element-012](../invest/named-elements.md#element-012), owner S433 |
| Disposition | To deliver as a single element in the "Water pipe kit" run. It is the blocked-outlet control for most water parts, so it should be delivered early in the run |
| Planned package | id `water_cap`, directory `elements/WaterCap/`, title "Pipe cap" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | Every pipe-bore mouth: [EL-001](EL-001-finite-reservoir.md), [EL-002](EL-002-header-tank.md), [EL-003](EL-003-tap.md), [EL-005](EL-005-liquid-funnel.md), [EL-008](EL-008-straight-water-pipe.md) to [EL-011](EL-011-water-t-junction.md) |

## Declaration

Closest delivered shape: a one-collider static part; the solid Cylinder is the Weight's collider kind (`WeightTests.TheCompiledMassSizeCentreAndInertiaFollowTheMassParameter`, `CuriousContraptions.tests/WeightTests.cs:139`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid fitting declaration of `ENGINE-LIQUID` (W4, W7).
- **Body.** One static body, mass 0. Axis local X; origin at the centre.
- **Collider (one).** Solid Cylinder, radius 0.15, length 0.10, axis X. It is wider than the 0.14 m pipe, so it visibly covers the mouth, and no wider than the kit rule allows (nothing reaches more than 0.15 m below the axis).
- **Liquid node.** One fitting, zero volume, with a single mouth and no other opening.
- **Mouth.** `Start` at (−0.05, 0, 0), normal −X, pipe bore, Intake. Joined, it occupies its neighbour's mouth, so that mouth is no longer a free end; the cap's fitting has one opening, and flows at a fitting sum to zero, so nothing passes. Unjoined, an Intake mouth passes nothing (L8). There is no leak value: a cap is the "nothing passes" control.
- **Parameters.** None.
- **Presentation.** None driven. **Palette.** `ElementCategory.Structure`; palette order 526 (kit sequence in EL-008; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour navy.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Cap` | Cylinder radius 0.15, length 0.10, navy `#293954` | solid; the Cylinder collider | body pose (static) |
| `Visual/Band` | Cream `#fff8e9` band round the rim, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_start` | Locator, bore 0.10 | none | nothing |

Icon: a dark round cap with a pale band. It differs from every other kit piece by being a closed dark disc; "closed" reads from shape and contrast.

### Behaviour and numbers

- **Capped flow stops.** Tank → pipe → cap: the tank keeps its volume at any head. With the Header tank at `stand_height` 4.0 and full, the pressure at a cap 5.0 m below the surface is 101 325 + 156.96 × 5.0 = 102 110 Pa and still nothing passes.
- **Deleting the cap restores a real opening.** The pipe's end is unjoined again and is a free end: the EL-008 flow (tank empties in 33.9 s through a level pipe).
- **Not seated, not closed.** A cap 0.3 m from the mouth is not joined (L6); the stream leaves the free end and meets the cap's solid face or passes it, and is spilled or caught below.
- **Authoring only.** Joins are fixed for a Run (L6); a cap cannot be pulled during a Run.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W21 solid Cylinder collider | present | body |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | fitting and mouth |
| W3 tube mouth, snap and occupied-mouth rule | narrower | the occupancy that makes a cap work while building is delivered and tested (`TubeMouthSnapTests.AnOccupiedMouthIsNotACandidate`); a part with a single mouth socket is `ENGINE-CAPABILITY-INSTANCES`'; the compiled join is `ENGINE-LIQUID`'s |
| W7 fitting flow balance | missing | `ENGINE-LIQUID` |

Counts: 3 present, 1 untested, 1 narrower, 1 missing; one owner.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, one collider, one fitting with one mouth.
2. **Generic laws.** Closure is the zero-sum rule at a fitting with one opening. There is no "cap" state and no closed flag.
3. **Three layers.** *Collider:* one Cylinder. *Visuals:* the table above. *Animation:* none.
4. **One-way flow and typing.** Mouth id and role are enums.
5. **Flat-state counts.** Bodies 1 (static); colliders 1 Cylinder; joints 0; drives 0; sensors 0; liquid nodes 1 fitting; mouths 1; bindings 0; parameters 0.
6. **Performance fitness.** One primitive. Merged into its neighbour at compile, a cap adds no unknown.
7. **Tests are scenarios.** Rows C1 to C6.

## Sources and legacy

- Requirement: "Closes a compatible fluid mouth"; outcome "Capped flow stops; deleting the cap restores a real opening." No variants.
- Component research [water](../../component-research.md#water) line 28; refinement [S705](../invest/refinements.md#s705) "Seal an end"; [sequence-task-385](../requirements.md#sequence-task-385) "a capped route cannot leak through an invisible connection".
- **Legacy search.** No liquid cap. Join facts:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | An occupied mouth is not a join candidate; removing the occupant frees it. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L103-L121` | Carried forward; delivered. |
| 2 | Joins are refused while running and restored by Reset. | `TubePlacementSnapTests.cs@a6c914e:L54-L85` | Carried forward: caps are placed and removed while building. |
| 3 | Mouth record (id, position, outward normal, bore radius). | `engine/TubeMouth.cs@a6c914e:L6-L14` | Carried forward (L5). |

**Files harvested:** none beyond EL-008's.

## Acceptance outline

Authority: [element-012](../requirements.md#element-012). Tolerances L17. Planned class `WaterCapTests`. Supply: a full Water tank with a level pipe; a basin under the pipe's far end.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| C1 | Given a cap joined to the pipe's far mouth, when Run advances 20 s, then the tank keeps 1.0 m³, the basin holds 0 and `Spilled` is 0 | `WaterCapTests.ACappedRouteHoldsItsVolume` | C2 | — |
| C2 | Given the same construction with the cap deleted, then the basin gains what the tank loses (0.503 m³ after 10 s within 0.05 m³) | `WaterCapTests.DeletingTheCapRestoresARealOpening` | C1 | — |
| C3 | Given the cap 0.3 m beyond the mouth, not joined, then the tank drains as in C2 | `WaterCapTests.AnUnseatedCapClosesNothing` | C1 | a cap just inside the snap distance seats and closes |
| C4 | Given a full Header tank at `stand_height` 4.0 with a riser down to a capped mouth, when Run advances 20 s, then nothing passes | `WaterCapTests.ACapHoldsAHighHead` | cap deleted: discharge | — |
| C5 | Given a mouth already joined to a pipe, then a cap offered to it does not snap | `WaterCapTests.AnOccupiedMouthRefusesACap` | a free mouth accepts it | — |
| C6 | Given a ball dropped on the cap, it rests on it; Reset and save and load keep the cap seated | `WaterCapTests.TheCapIsSolidAndStaysSeatedThroughResetAndReload` | — | — |

Generic cover: W22. It also serves as the control in EL-001 R1 and EL-011 J2. This slice owns two cross-part rows of earlier-taught parts and runs them with its own: EL-005 F2 (the funnel, 61b) and EL-010 E2 (the 90° elbow, 62). Chrome recipe (pending): tank, tap, pipe over a basin; drag a Pipe cap onto the open mouth until it seats; Reset, delete it, Run again.

## Open questions

All resolved; none needs the owner.

1. A cap for the ball-pipe bore: no; water only (D19).
2. Removable during Run: no; joins are fixed for a Run (L6). A plug that can be pulled would be a valve, which the Tap family is.
3. Leakage: zero; there is no leak parameter.

## Integration

- **Works with.** Every pipe-bore mouth; none delivered.
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-335`; `sequence-task-565` not co-anchored with a todo; no `s433` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), including `ENGINE-CAPABILITY-INSTANCES` for a single mouth socket, then: `ENGINE-LIQUID`; EL-001; EL-008; EL-004; S7. C4 needs EL-002 and the 90° elbow.
- **Files.** Adds `elements/WaterCap/WaterCap.csproj`, `WaterCapElementDeclaration.cs`, `water_cap.tscn`, `icon.svg`, `CuriousContraptions.tests/WaterCapTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** A part with a single mouth refused by the bundle; a joined mouth still treated as a free end.
