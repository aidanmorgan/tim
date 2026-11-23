# EL-013 · Water nozzle — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-013 delivery slice](../invest/named-elements.md#element-013). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md); the pipe-kit values are in [EL-008](EL-008-straight-water-pipe.md). Sizes are in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-013 · Water nozzle · Water |
| Anchors | [element-013](../requirements.md#element-013), co-anchored `sequence-task-566`; source record [todo-335](../requirements.md#todo-335); integration [sequence-task-385](../requirements.md#sequence-task-385); ledger [element-013](../invest/named-elements.md#element-013), owner S434 |
| Disposition | To deliver as a single element, last of the "Water pipe kit" run |
| Planned package | id `water_nozzle`, directory `elements/WaterNozzle/`, title "Water nozzle" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | [EL-008](EL-008-straight-water-pipe.md), [EL-002](EL-002-header-tank.md) (head), [EL-022](EL-022-water-pump.md) (pumped head), [EL-004](EL-004-catch-basin.md) (target), EL-036 |

## Declaration

Closest delivered shape: the ball Funnel's Frustum wall (`elements/Funnel/FunnelElementDeclaration.cs:10`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid fitting and seat declaration of `ENGINE-LIQUID` (W4, W7).
- **Body.** One static body, mass 0. Axis local X; origin at the centre.
- **Collider (one).** Frustum wall 0.30 long: inner radius 0.10 at x = −0.15 tapering to 0.04 at x = +0.15, radial wall 0.04 (outer 0.14 to 0.08).
- **Liquid node.** One fitting, zero volume, with one fixed seat at the exit: area A_n = π × 0.04² = 0.005027 m², so K_n = 0.6 × 0.005027 × 4.42945 = **0.01336**.
- **Mouth.** `Start` at (−0.15, 0, 0), normal −X, pipe bore, Intake.
- **Outlet (not a mouth).** A declared free end at (+0.15, 0, 0), normal +X, behind the seat. Nothing joins it; it only launches a stream (L8).
- **Parameters.** None. Aim is the part's pose.
- **Presentation.** None driven; the stream is drawn by the shared liquid presentation (W8). **Palette.** `ElementCategory.Structure`; palette order 527 (kit sequence in EL-008; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour gold.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Cone` | Frustum as the collider, gold `#f7cb52` | solid, hollow; the Frustum collider | body pose (static) |
| `Visual/TipBand` | Navy `#293954` band at the tip, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_start` | Locator, bore 0.10 | none | nothing |
| `Loc_Outlet` | Locator at the exit | none | nothing |

Icon: a gold cone narrowing to a dark tip. It differs from the Water funnel (wide, cream, open on top) by being a small closed cone on a pipe axis.

### Behaviour and numbers

- **Law (L7, L8).** Supplied by a store through the nozzle there are two apertures in series, the store's mouth join (K₀) and the exit seat (K_n): K = 1/√(1/K₀² + 1/K_n²) = 0.01336 / √(1 + 0.0256) = **0.01319**. The stream leaves at v = Q / (Cd·A_n) = K·√h / 0.003016, so v² = 0.975 × 2·g·h: 97.5% of the head becomes jet speed, never more than all of it.
- **Rated head 2.0 m** (a full Header tank at `stand_height` 1.36, surface 2.51 m, above a nozzle whose exit is 0.50 m above the bench): Q = 0.01319 × 1.414 = 0.01866 m³/s (0.30 kg/s); v = 6.19 m/s. Thrown at 45° over level ground the stream returns to exit height v²/g = **3.90 m** away. This is the rated reach.
- **Reach follows head.** v²/g = 1.95 × h for a 45° throw: 1.0 m of head reaches 1.95 m, 0.5 m reaches 0.98 m. As the tank drains the reach shortens. No head, no jet.
- **A thin jet drains slowly.** The Header tank falls from 1.0 m to 0.68 m of depth (head 2.01 to 1.69 m) in (2·A/K)·(√2.01 − √1.69) = (0.5 / 0.01319) × 0.118 = 4.5 s.
- **No force on bodies.** The stream is a transfer path (L8, L12). What it meets first receives the liquid or spills it.

**Acceptance geometry (used by rows N1 to N3).** A nozzle joins only in line with its pipe, so it is aimed with a 45° elbow: tank mouth → pipe → 90° elbow down → pipe → 90° elbow → pipe along the bench → 45° elbow up → nozzle. Its exit is then 0.50 m above the bench at x = 0, aimed 45° up along +X. With `stand_height` 1.36 the head is 2.01 m, v = 6.20 m/s and the path is y = 0.50 + x − 0.2551·x². It crosses the height of a Catch basin's rim (0.55 m) on the way down at x = 3.87 m. Place the basin with its long axis along X and its centre at x = 3.87: rim opening from 3.245 to 4.495 m, near outer wall at 3.125 m, where the stream is 1.13 m up and clears the 0.55 m wall. The stream keeps landing in the opening while the downward crossing stays beyond 3.245 m, that is while v ≥ 5.69 m/s, head ≥ 1.69 m: the first 0.32 m of tank depth, 0.080 m³. With `stand_height` 0.5 and `initial_volume` 0.125 (surface 1.15 m, head 0.65 m), v = 3.53 m/s and the stream reaches the bench at x = 1.65 m, short of the basin's near wall.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Frustum collider | present | body |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | fitting and mouth |
| W3 tube mouth and snap | narrower | join not compiled |
| W7 a fixed seat with its own area in series | missing | `ENGINE-LIQUID` |
| W8 a free end that is not a mouth, launching a stream at Q/(Cd·A) along its normal | missing | `ENGINE-LIQUID` (gap in its text) |

Counts: 3 present, 1 untested, 1 narrower, 2 missing; one owner.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, one collider, one fitting with one fixed seat, one mouth, one outlet.
2. **Generic laws.** "Trades supplied pressure for a bounded directed jet" is a small aperture in series (L7) and the stream start speed rule (L8), which every free end obeys; the nozzle only declares a smaller area. "Insufficient head cannot produce the rated reach" follows from v² being proportional to h.
3. **Three layers.** *Collider:* one Frustum. *Visuals:* the table above. *Animation:* none of its own.
4. **One-way flow and typing.** Mouth id and role are enums; the seat area is f32 data.
5. **Flat-state counts.** Bodies 1 (static); colliders 1 Frustum; joints 0; drives 0; sensors 0; liquid nodes 1 fitting; seats 1; mouths 1; outlets 1; bindings 0; parameters 0.
6. **Performance fitness.** One collider. One stream path per substep, bounded as L18 requires; a fast thin stream is still one path, so there is nothing to tunnel.
7. **Tests are scenarios.** Rows N1 to N6.

## Sources and legacy

- Requirement: "Trades supplied pressure for a bounded directed jet"; outcome "Insufficient head cannot produce the rated reach." No variants.
- Refinement [S705](../invest/refinements.md#s705) nozzle; component research [water](../../component-research.md#water) lines 15, 20 and 28.
- **Legacy search.** No liquid nozzle. Gas-nozzle analogue:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Zero area, or equal upstream and back pressure, gives no flow. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L57` | Carry forward: no head, no jet. |
| 2 | Back pressure above source pressure throws. | `ConvergingGasNozzle.cs@a6c914e:L54-L55` | Not carried: reverse head simply gives no jet (clamp or continue). |
| 3 | Mass rate = area × density × speed; momentum rate = mass rate × speed. | `ConvergingGasNozzle.cs@a6c914e:L76-L81` | Carry forward the mass rate. The momentum rate is not applied to bodies (L12). Not the compressible law. |

**Files harvested:** `engine/physics/ConvergingGasNozzle.cs` (analogue only).

## Acceptance outline

Authority: [element-013](../requirements.md#element-013). Tolerances L17. Planned class `WaterNozzleTests`. Constructions use the acceptance geometry above.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| N1 | Given `stand_height` 1.36 and a full tank, when Run advances 3 s, then the basin at 3.87 m gains what the tank loses within 0.004 m³ and `Spilled` is 0 | `WaterNozzleTests.RatedHeadReachesTheBasin` | N2 | — |
| N2 | Given `stand_height` 0.5 and `initial_volume` 0.125 (head 0.65 m), when Run advances 3 s, then the basin holds 0 and the tank's loss is in `Spilled` | `WaterNozzleTests.InsufficientHeadFallsShort` | N1 | — |
| N3 | Given N1 run on until the tank's depth is below 0.6 m (head 1.61 m, reached at about (0.5 ÷ 0.01319) × (√2.01 − √1.61) = 5.6 s), then the basin has stopped gaining while the tank is still losing, and the basin's total is 0.080 m³ within 0.02 m³ | `WaterNozzleTests.ReachShortensAsTheHeadFalls` | — | stall guard 15 s for the depth to fall below 0.6 m (inside the default 30 s Run) |
| N4 | Given the nozzle's committed flow at head 2.01 m, then it is 0.0187 m³/s within 10%, about a fifth of what the same route passes without the nozzle (0.0837 m³/s) | `WaterNozzleTests.TheNozzleThrottlesTheRoute` | the bare pipe end | — |
| N5 | Given an empty tank, or a cap in place of the supply, then no stream leaves | `WaterNozzleTests.NoHeadNoJet` | N1 | — |
| N6 | Given a solid Wall across the stream's path, then the basin holds 0; Reset restores the tank; save and load keep the aim and the join | `WaterNozzleTests.AWallStopsTheStreamAndTheAimSurvivesReload` | N1 | — |

Generic cover: W22. Chrome recipe (pending): Header tank at stand height 1.36, pipes and elbows down to the bench, a 45° elbow and a nozzle, a basin 3.87 m away.

## Open questions

All resolved; none needs the owner.

1. Does the jet push bodies: no (D19; no requirement row or lesson uses it). A general stream force would be an `ENGINE-LIQUID` addition if a later part needs it.
2. Exit radius 0.04 m: kept (D1). The velocity coefficient 0.95 is replaced by the one family Cd, which gives 97.5% of the head as speed in this route.
3. Rated reach shown to players: as a selected-part preview arc at the current head, which is Workbench presentation and reads committed head (L13); not a parameter.

## Integration

- **Works with.** EL-002, EL-008 to EL-010, EL-004, EL-022; none delivered. The delivered Wall blocks the stream in N6.
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-335`; `sequence-task-566` not co-anchored with a todo; no `s434` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID`; EL-001; EL-002; EL-008; the elbow family; EL-004; S7.
- **Files.** Adds `elements/WaterNozzle/WaterNozzle.csproj`, `WaterNozzleElementDeclaration.cs`, `water_nozzle.tscn`, `icon.svg`, `CuriousContraptions.tests/WaterNozzleTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** A stream that always starts at zero speed; an outlet that must be a joinable mouth; a seat area that cannot be fixed data.
