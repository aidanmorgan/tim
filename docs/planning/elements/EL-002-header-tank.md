# EL-002 · Header tank — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-002 delivery slice](../invest/named-elements.md#element-002). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md) and are cited by number. Citations `path@a6c914e:L…` are the legacy harvest; `path:line` is this checkout. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-002 · Header tank · Water |
| Anchors | [element-002](../requirements.md#element-002), co-anchored `sequence-task-555`; source record [todo-334](../requirements.md#todo-334); integration [sequence-task-384](../requirements.md#sequence-task-384) and [sequence-task-389](../requirements.md#sequence-task-389); ledger [element-002](../invest/named-elements.md#element-002), owner S423 |
| Disposition | To deliver as a single element. Not a duplicate of EL-001: it has a different capacity and plan area, a second parameter (`stand_height`) with parameter-bound geometry, and its own outcome (head from real height) |
| Planned package | id `header_tank`, directory `elements/HeaderTank/`, title "Header tank" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` and `ENGINE-THERMAL` |
| Related | [EL-001](EL-001-finite-reservoir.md) (the same store law at bench level), pipe kit [EL-008](EL-008-straight-water-pipe.md) to [EL-013](EL-013-water-nozzle.md), [EL-022](EL-022-water-pump.md) and EL-023 (fill it), EL-032, EL-229 |

## Declaration

Closest delivered shapes: the Receiver for the open box and the Clear pipe for geometry bound to a parameter (`elements/Pipe/PipeElementDeclaration.cs:50`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, `IParameterizedGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:29`) and the liquid store declaration of `ENGINE-LIQUID` (W4, W6).
- **Body.** One static body, mass 0. Origin at the centre of the tank's outer box.
- **Colliders (six boxes).** Tank outer 0.74 × 1.15 × 0.74, interior 0.5 × 1.0 × 0.5: floor 0.74 × 0.15 × 0.74 at (0, −0.5, 0); two walls 0.12 × 1.0 × 0.74 at (±0.31, 0.075, 0); two walls 0.5 × 1.0 × 0.12 at (0, 0.075, ±0.31). Pedestal: one box 0.30 × h × 0.30 at (0, −0.575 − h/2, 0), where h is `stand_height`; its half-height and offset are `ColliderParameterBinding` rows (multipliers 0.5 and −0.5, offset −0.575 on the centre).
- **Material.** Family vessel material (EL-001): restitution 0.12, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s.
- **Store.** Local id 1. Capacity 0.25 m³ (4 kg), plan area 0.25 m², floor datum (0, −0.425, 0). Open aperture 0.5 × 0.5 at y = +0.575. Overflow point (0, 0.575, 0.37). Its store is also a thermal node (owner decision 20; [TH-04 section 11](TH-04-electrical-heating-plate.md#thermal-conventions), T2), starting at the ambient 288 K (T1) unless a level authors another; inflow mixes by mass, outflow leaves at the store's temperature, and no row here reads temperature.
- **Mouth.** `End`, pipe bore, Bidirectional, into store 1, at (0, −0.425, 0.37), normal +Z, at interior-floor height. A riser is built with an elbow.
- **Heights above the pedestal base.** Floor and mouth datum h + 0.15; rim h + 1.15. At the default h = 2.0 the full surface is at 3.15 m.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_volume` | Real f32 | m³ (W13) | 0.25 | 0 | 0.25 | Reject; build only |
| `stand_height` | Real f32 | `ParameterUnit.Metre` | 2.0 | 0.5 | 4.0 | Reject; build only; resize handle on the pedestal, as the pipe's length handle |

The upper limit keeps the rim (5.15 m) under the Wall's 6 m maximum. Capacity is fixed (D13). No wire port, sensor, joint or drive.

- **Presentation.** `Visual/Water` scale and offset from the committed fill fraction (W12, missing). `Visual/Pedestal` `ScaleY` and `LocalY` from `stand_height` through `PresentationParameterBinding`, as the pipe shell (`elements/Pipe/PipeElementDeclaration.cs:22`).
- **Palette.** `ElementCategory.Power`; palette order 325 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cream.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Shell` | Open box, five slabs, cream `#fff8e9` | solid; five box colliders | body pose (static) |
| `Visual/Pedestal` | Box 0.30 × h × 0.30, navy `#293954` | solid; the pedestal collider, bound to the same parameter | parameter binding (authoring only) |
| `Visual/Scale` | Gold `#f7cb52` ticks every 0.5 m on the pedestal front | decorative: printed flat | nothing |
| `Visual/Water` | Box 0.5 × 1.0 × 0.5, cyan `#66b8c9`, alpha 0.16 | decorative: enclosed, moved by a binding | presentation binding |
| `Visual/OutletRing` | Navy ring, radius 0.14, front face | decorative: printed flat | nothing |
| `Loc_Port_mouth_end` | Locator, bore 0.10 | none | nothing |

Icon: a small tank on a tall pedestal. It differs from the Water tank by the pedestal and its narrow body, not by colour. The earlier four legs and cross-brace are one pedestal, because five extra colliders bought nothing.

### Behaviour and numbers

- **Head is real height (L7).** A sealed riser ending in a free end at height y_d delivers only while the tank surface is above y_d, and delivers (surface − y_d) × 0.25 m³ in total.
  - `stand_height` 2.0, full: surface 3.15 m. Free end at 2.55 m (a 90° elbow turned up on the mouth at 2.15 m ends there): 0.60 × 0.25 = **0.150 m³** is delivered. Two apertures in series (the tank mouth join and the free end): K = K₀/√2 = 0.05904, so the time is 2·A·√Δh₀ / K = 2 × 0.25 × √0.60 / 0.05904 = **6.6 s**.
  - `stand_height` 1.5: surface 2.65 m; 0.10 × 0.25 = 0.025 m³. The riser is rebuilt for each stand height, because the mouth moves with the tank: the elbow alone at 2.0, the elbow and a 0.5 m pipe at 1.5, the elbow and a 1.0 m pipe at 1.0; each ends at 2.55 m.
  - `stand_height` 1.0: surface 2.15 m, below 2.55 m: nothing is delivered and the riser holds liquid to 2.15 m. This is "lowering it reduces available lift", and it is the crest rule of L7.
- **No energy is created.** The gravitational energy of all stores, ρ·g·Σ V·(floor elevation + d/2), never rises in a network with no work target. Two header tanks joined by pipe, A at `stand_height` 2.0 and full, B at 1.0 and empty: A's floor (2.15 m) is B's rim, so all 0.25 m³ moves to B. Energy before 156.96 × 0.25 × (2.15 + 0.5) = 104.0 J; after 156.96 × 0.25 × (1.15 + 0.5) = 64.7 J. With a pump the cycle is checked in [EL-022](EL-022-water-pump.md).
- **Pressure at the mouth, full:** 101 325 + 156.96 = 101 482 Pa, independent of `stand_height`; the stand changes elevation, not depth.
- **Why these values.** A quarter of the Water tank's area makes the level fall four times faster, which shows head falling during one pour (recipe "Pressure, Not Plenty").

## Engine capabilities

Map families (navigation only, D14): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body and baked colliders | present | tank and pedestal |
| W5 parameter-bound geometry | present | pedestal collider follows `stand_height` |
| W22, W23 authored state, refusals | present | both parameters |
| W4 fluid port contract | present, untested | store and mouth declaration |
| W3 tube mouth and snap | narrower | the mouth; edges are not compiled |
| W6, W7, W8, W9 store, flow, stream, overflow | missing | `ENGINE-LIQUID` |
| W12 fill-fraction driver; W13 m³ unit | missing | W12: `ENGINE-LIQUID`; W13's m³ unit: `ENGINE-GAS-NETWORK`, which lands first (EL-001 W13; owner decision 18, 22 November 2026) |

Counts: 4 present, 1 untested, 1 narrower, 6 missing; two owners: `ENGINE-LIQUID`, and `ENGINE-GAS-NETWORK` for the m³ unit.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, six colliders, one store, one mouth, two parameters, parameter bindings and one liquid binding. No code path of its own.
2. **Generic laws.** Lift is L7: total head at a wet mouth is the surface elevation, which the pose and the stand set. Nothing reads "header tank".
3. **Three layers.** *Colliders:* six `col_` boxes, one bound to a parameter. *Visuals:* the table above. *Animation:* the water box follows the committed fill; the pedestal art follows the authored parameter and never moves in a Run.
4. **One-way flow and typing.** As EL-001.
5. **Flat-state counts.** Bodies 1 (static); colliders 6 boxes; joints 0; drives 0; sensors 0; liquid nodes 1 store; thermal nodes 1; mouths 1; apertures 1; open apertures 1; bindings 2 liquid + 2 parameter; parameters 2.
6. **Performance fitness.** Six boxes against the Receiver's five: the sixth is the pedestal, which is a visible solid. Static; constant work per substep.
7. **Tests are scenarios.** Rows H1 to H8.

## Sources and legacy

- Requirement: "Elevated storage supplies gravitational head from its real height"; outcome "Lowering it reduces available lift; cycling cannot create energy." No variants.
- Family boundary [fluids](../invest/profiles.md#fluids) ("reversed head does not flow uphill"); [envelope](../../gpu-f32-physics.md#game-grade-envelope) "no free energy"; component research [water](../../component-research.md#water) lines 26, 54 and 61 (Up and Over; Pressure, Not Plenty).
- **Legacy search.** As EL-001: no liquid element at a6c914e. The finite-store facts are EL-001 rows 1 and 2. **Files harvested:** none of its own.

## Acceptance outline

Authority: [element-002](../requirements.md#element-002). Tolerances L17. Planned class `HeaderTankTests`. Rows H1 to H4 need a pipe and an elbow, so they run after EL-008 and the elbow family are delivered; the slice order is in Delivery.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| H1 | Given a full tank at `stand_height` 2.0 feeding a riser whose free end is at 2.55 m over a basin, when Run advances until flow stops (stall guard 20 s), then the basin holds 0.150 m³ and the tank surface is at 2.55 m | `HeaderTankTests.ARiserDeliversDownToItsOutletHeight` | H2 | free end at 3.15 m: nothing |
| H2 | Given the same construction with `stand_height` 1.0, when Run advances 15 s, then the basin holds nothing and the tank keeps 0.25 m³ | `HeaderTankTests.ALoweredTankCannotReachTheOutlet` | H1 | `stand_height` 1.5 delivers 0.025 m³ |
| H3 | Given tank A (2.0, full) and tank B (1.0, empty) joined by pipe, when Run advances until flow stops (about 2 m of head moves 0.25 m³ in well under 10 s; stall guard 20 s, inside the default 30 s Run), then B holds 0.25 m³, A is empty, and the stores' gravitational energy never exceeds its starting 104.0 J by more than 0.7 J and ends at 64.7 J within 10% | `HeaderTankTests.LiquidOnlyRunsDownAndGainsNoEnergy` | A and B at equal height and equal volume: nothing moves | — |
| H4 | Given B full and A empty in the H3 construction, when Run advances 15 s, then nothing moves (the lower surface cannot feed the higher mouth) | `HeaderTankTests.ALowerTankDoesNotFeedAHigherOne` | H3 | — |
| H5 | Given a full tank under a stream, then it stays at 0.25 m³ and the excess is conserved (L9) | `HeaderTankTests.AFullTankOverflowsConserved` | a tank at 0.125 m³ gains | exactly full at start |
| H6 | Given `stand_height` 0.5 and 4.0, then both are admitted and the pedestal collider's top and bottom are at the tank floor and the bench; values outside, or non-finite, are refused whole | `HeaderTankTests.StandHeightIsBoundedAndMovesThePedestal` | default 2.0 | next f32 beyond each limit |
| H7 | Given a ball rolled at the pedestal, then it is stopped at the pedestal face (visible = solid) | `HeaderTankTests.ThePedestalIsSolid` | a ball rolled beside it passes | `stand_height` 4.0 |
| H8 | Given a run that moved liquid, then Reset restores `initial_volume`; a save and load keeps both parameters and the pose | `HeaderTankTests.ResetAndSaveLoadKeepTheAuthoredTank` | — | both limits of each parameter |

Later-partner rows: the pumped cycle ([sequence-task-389](../requirements.md#sequence-task-389)) is `WaterPumpTests.ALiftCycleSpendsAtLeastTheEnergyItStores` in the EL-022 slice; the tank → tap → basin route ([sequence-task-384](../requirements.md#sequence-task-384)) runs in the EL-004 slice. Generic cover as EL-001 (W22, W23). Chrome recipe (pending): place a Header tank, drag its pedestal handle, snap a riser of pipes and an elbow, end it over a basin on a shelf.

## Open questions

All resolved; none needs the owner.

1. `stand_height` continuous or stepped: continuous, because a parameter has no step (L2) and the resize handle already works that way for the pipe.
2. Head loss in long pipes: none; loss is at apertures only (L7), so a route's behaviour does not depend on how many pipe pieces build it.
3. Resizable capacity: no (D13).
4. Outlet through the floor: replaced by the front outlet at floor height, the same place as the Water tank, so both tanks join the kit the same way and the pedestal can sit under the centre.

## Integration

- **Works with.** EL-008 to EL-013, EL-003, EL-004, EL-022, EL-023, EL-032, EL-229; none delivered. The delivered Wall makes the shelf in H1.
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. A level that places it needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** for the same three reasons (umbrella `todo-334`; `sequence-task-555` co-anchored with the element, not a todo; no `s423` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID`; `ENGINE-THERMAL`; EL-001; EL-008 and the elbow family for rows H1 to H4 (if the tank is delivered before them, those rows are named later-partner rows of the elbow slice); S7.
- **Files.** Adds `elements/HeaderTank/HeaderTank.csproj`, `HeaderTankElementDeclaration.cs`, `header_tank.tscn`, `icon.svg`, `CuriousContraptions.tests/HeaderTankTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** A store whose head does not follow the body's pose; a parameter-bound collider that the liquid step does not see; any EL-001 disqualifier.
