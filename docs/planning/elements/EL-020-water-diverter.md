# EL-020 · Water diverter — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-020 delivery slice](../invest/named-elements.md#element-020). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md); the pipe-kit values are in [EL-008](EL-008-straight-water-pipe.md). Sizes are in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-020 · Water diverter · Water |
| Anchors | [element-020](../requirements.md#element-020), co-anchored `sequence-task-573`; source record [todo-338](../requirements.md#todo-338); integration [sequence-task-389](../requirements.md#sequence-task-389); ledger [element-020](../invest/named-elements.md#element-020), owner S441 |
| Disposition | To deliver as a single element. It is the liquid route selector; the campaign's "fixed/powered/alternating diverter" is a ball-route part in another row |
| Planned package | id `water_diverter`, directory `elements/WaterDiverter/`, title "Water diverter" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-PARAMETER-BOUND-FRAMES` and `ENGINE-LIQUID` |
| Related | [EL-011](EL-011-water-t-junction.md) (shares instead of selecting), [EL-003](EL-003-tap.md), pipe kit, [EL-004](EL-004-catch-basin.md) |

## Declaration

Closest delivered shape: the Clear pipe for tubes and mouths; the Fan for a Choice that selects between declared alternatives (`elements/Fan/FanElementDeclaration.cs:14`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid fitting and seat declaration of `ENGINE-LIQUID` (W4, W7, W14).
- **Body.** One static body, mass 0. A Y lying in the local XZ plane; origin at the fork.
- **Colliders (three Tubes, bore 0.10, outer radius 0.14, each 0.30 long).** Stem from (−0.30, 0, 0) to the origin; leg A from the origin toward (cos 30°, 0, +sin 30°); leg B toward (cos 30°, 0, −sin 30°).
- **Liquid node.** One fitting with two seats, each of full bore area: seat A between the stem and `End`, seat B between the stem and `Branch`. Openings by option of `selected` (L14 a): Outlet A gives A = 1, B = 0; Outlet B gives A = 0, B = 1. The unselected seat's declared opening is 0: that is the explicitly modelled leakage, and it is none.
- **Mouths (pipe bore).** `Start` at (−0.30, 0, 0), normal −X, Intake. `End` (outlet A) at (0.2598, 0, +0.15), normal (0.866, 0, 0.5), Discharge. `Branch` (outlet B) at (0.2598, 0, −0.15), normal (0.866, 0, −0.5), Discharge. The two outlet centres are 0.30 m apart, so two 0.28 m wide pipes fit side by side and then diverge.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `selected` | Choice | None | 0 (Outlet A) | 0 | 1 | Reject; build only; options `Outlet A`, `Outlet B` |

- **Presentation.** `Visual/Pointer` `LocalRotationZ` from `selected` through `PresentationParameterBinding`: toward leg A at 0, toward leg B at 1.
- **Palette.** `ElementCategory.Control`; palette order 413 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cream.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Stem`, `Visual/LegA`, `Visual/LegB` | Three tubes 0.30 long, cream `#fff8e9`, navy flush end bands | solid, hollow; the three Tube colliders | body pose (static) |
| `Visual/Pointer` | Gold `#f7cb52` pointer on top of the fork, flush | decorative: printed flat, turned by a binding | parameter binding |
| `Visual/Pips` | One navy pip on leg A, two on leg B, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end`, `Loc_Port_mouth_branch` | Locators, bore 0.10 | none | nothing |

Icon: a cream Y with a pointer. It differs from the Water T (a right-angle stub, no pointer) by its Y shape and pointer; the selection reads from the pointer's direction and the pips.

### Behaviour and numbers

- **Selected route.** Tank → pipe → diverter → pipe → free end: three apertures (tank mouth join, the open seat, the free end), K = K₀/√3 = 0.04820, the same as a fully open Tap: 0.0482 m³/s at 1.0 m depth, a full Water tank empty in 41.5 s.
- **Unselected route.** A seat of zero area: zero flow at any head. The basin on that side receives nothing.
- **Selected outlet unjoined.** It is a free end and spills there; nothing is rerouted to the other outlet.
- **Fixed during a Run.** `selected` is a build-only parameter. A diverter that switches during a Run would take a signal memory node on the same seats (L14 c); no requirement or lesson asks this part for it.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Tube colliders | present | body |
| W22, W23 authored state, refusals | present | `selected` |
| W4 fluid port contract | present, untested | fitting and mouth roles |
| Parameter-driven art (`PresentationParameterBinding`) | present, untested | pointer; no test of a parameter-driven rotation was opened |
| W3 mouth sockets and snap | narrower | two mouths per part today; a third mouth socket is `ENGINE-CAPABILITY-INSTANCES`' (as EL-011) |
| W7 fitting with seats; W8 free end | missing | `ENGINE-LIQUID` |
| W14 seat opening per Choice option | missing | `ENGINE-LIQUID` (gap) |

Counts: 4 present, 2 untested, 1 narrower, 3 missing; one owner.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, three colliders, one fitting with two seats, three mouths, one Choice, one parameter binding.
2. **Generic laws.** Selection is two seat openings chosen by a declared option (L14 a) under L7. There is no routing rule.
3. **Three layers.** *Colliders:* three Tubes. *Visuals:* the table above; pointer and pips are flush. *Animation:* none in a Run.
4. **One-way flow and typing.** `selected` is a Choice held as a typed option index; mouth ids and roles are enums.
5. **Flat-state counts.** Bodies 1 (static); colliders 3 Tubes; joints 0; drives 0; sensors 0; liquid nodes 1 fitting; seats 2; mouths 3; bindings 1 parameter; parameters 1.
6. **Performance fitness.** Three colliders, as the Clear pipe's three. A closed seat removes its branch from the solve.
7. **Tests are scenarios.** Rows DV1 to DV6.

## Sources and legacy

- Requirement: "Selects one physically connected outlet"; outcome "Unselected route receives only explicitly modelled leakage." No variants.
- Refinement [S707](../invest/refinements.md#s707) diverter; [sequence-task-389](../requirements.md#sequence-task-389) "choose/gate a route … Do not substitute one generic timed valve for all variants"; component research [water](../../component-research.md#water) line 34.
- **Legacy search.** EL-001's scope plus "diverter" and "valve": no liquid or ball diverter at a6c914e. **Files harvested:** none.

## Acceptance outline

Authority: [element-020](../requirements.md#element-020). Tolerances L17. Planned class `WaterDiverterTests`. Rig: a full Water tank and pipe into `Start`; a pipe from each outlet ends over its own basin.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| DV1 | Given `selected` Outlet A, when Run advances 10 s, then basin A's gain equals the tank's loss within 0.004 m³ and basin B holds 0 | `WaterDiverterTests.TheSelectedOutletTakesAllTheFlow` | DV2 | tank volume after 10 s is (1 − 10/41.5)² = 0.576 m³ within 0.05 m³ |
| DV2 | Given `selected` Outlet B, then basin B gains and basin A holds 0 | `WaterDiverterTests.SelectingTheOtherOutletSwapsTheRoute` | DV1 | — |
| DV3 | Given `selected` Outlet A with outlet A's pipe removed, then liquid is spilled at the `End` mouth and basin B still holds 0 | `WaterDiverterTests.AnUnjoinedSelectedOutletSpillsAndIsNotRerouted` | DV1 | — |
| DV4 | Given the outlet B route's free end 1.0 m lower than A's, with `selected` Outlet A, then basin B still holds 0 (no head opens a closed seat) | `WaterDiverterTests.NoHeadOpensTheUnselectedSeat` | — | — |
| DV5 | Given an option index outside 0 to 1 or non-finite, then it is refused whole; the pointer art faces leg A at 0 and leg B at 1 | `WaterDiverterTests.SelectionIsAClosedChoiceAndShowsOnThePointer` | both options admitted | — |
| DV6 | Given a run, then Reset restores the tank and keeps the authored selection; save and load keep `selected`, the pose and all three joins | `WaterDiverterTests.ResetAndSaveLoadKeepTheSelection` | — | — |

Generic cover: W22, W23. Chrome recipe (pending): tank, tap, pipe into a Water diverter; pipes from both outlets over two basins; click the pointer to choose.

## Open questions

All resolved; none needs the owner.

1. Selection during a Run: no; build-only (reason above).
2. Non-zero leakage: none. The `leakage` parameter is removed: the modelled leakage is the unselected seat's declared opening, which is 0, and no lesson needs a leaking diverter (D19).

## Integration

- **Works with.** Planned: EL-001, EL-003, EL-008, EL-004, EL-011. None delivered.
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-338`; `sequence-task-573` not co-anchored with a todo; no `s441` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), including `ENGINE-CAPABILITY-INSTANCES` for the third mouth socket and `ENGINE-PARAMETER-BOUND-FRAMES` for choice-bound seat values, then: `ENGINE-LIQUID`; EL-001; EL-008; EL-004; S7.
- **Files.** Adds `elements/WaterDiverter/WaterDiverter.csproj`, `WaterDiverterElementDeclaration.cs`, `water_diverter.tscn`, `icon.svg`, `CuriousContraptions.tests/WaterDiverterTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** Two mouths per part only; a seat opening that cannot follow a Choice; Tube colliders that cannot be rotated 30° about the vertical within one body.
