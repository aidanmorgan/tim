# EL-004 · Catch basin — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-004 delivery slice](../invest/named-elements.md#element-004). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md) and are cited by number. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-004 · Catch basin · Water |
| Anchors | [element-004](../requirements.md#element-004), co-anchored `sequence-task-557`; source record [todo-334](../requirements.md#todo-334); integration [sequence-task-384](../requirements.md#sequence-task-384); ledger [element-004](../invest/named-elements.md#element-004), owner S425 |
| Disposition | To deliver as a single element. Not a duplicate of the ball Receiver ([CAT-004](../requirements.md#current-cat-004)): the Receiver captures balls and has no liquid store. Not a duplicate of EL-001: it has no mouth, a different capacity and area, and starts empty |
| Planned package | id `catch_basin`, directory `elements/CatchBasin/`, title "Catch basin" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` and `ENGINE-THERMAL` |
| Related | [EL-001](EL-001-finite-reservoir.md), [EL-003](EL-003-tap.md), [EL-005](EL-005-liquid-funnel.md), [EL-006](EL-006-drain.md), [EL-007](EL-007-open-gutter.md), [EL-016](EL-016-float.md) (floats in it), EL-031 |

## Declaration

Closest delivered shape: the Receiver's open five-box vessel (`elements/Receiver/basket.tscn`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, and the liquid store declaration of `ENGINE-LIQUID` (W4, W6).
- **Body.** One static body, mass 0. Origin at the centre of the outer box.
- **Colliders (five boxes).** Outer 1.49 × 0.55 × 1.24; interior 1.25 × 0.40 × 1.0. Floor 1.49 × 0.15 × 1.24 at (0, −0.2, 0); two walls 0.12 × 0.40 × 1.24 at (±0.685, 0.075, 0); two walls 1.25 × 0.40 × 0.12 at (0, 0.075, ±0.56).
- **Material.** Family vessel material (EL-001).
- **Store.** Local id 1. Capacity 0.5 m³ (8 marks, 8 kg), plan area 1.25 m², floor datum (0, −0.125, 0). Open aperture: rectangle 1.25 × 1.0 at y = +0.275, normal +Y. Overflow point (0, 0.275, 0.62). Starts empty. Its store is also a thermal node (owner decision 20; [TH-04 section 11](TH-04-electrical-heating-plate.md#thermal-conventions), T2), starting at the ambient 288 K (T1) unless a level authors another; inflow mixes by mass, outflow leaves at the store's temperature, and no row here reads temperature.
- **Mouths.** None; the Drain is [EL-006](EL-006-drain.md).
- **Parameters.** `initial_volume`: Real f32, m³ (W13), default 0, minimum 0, maximum 0.5, Reject, build only. It starts empty by default; rows in other specs author a depth through it. Capacity is fixed (D13).
- **Presentation.** `Visual/Water` scale and offset from the committed fill fraction (W12, missing).
- **Palette.** `ElementCategory.Structure`; palette order 518 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour teal (74/255, 171/255, 148/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Shell` | Open tray, five slabs, teal `#4aab94` with a cream `#fff8e9` rim face | solid; five box colliders | body pose (static) |
| `Visual/Water` | Box 1.25 × 0.40 × 1.0, cyan `#66b8c9`, alpha 0.16 | decorative: enclosed, moved by a binding | presentation binding |
| `Visual/Marks` | Gold lines on the inner back wall every 0.05 m (one mark = 0.0625 m³) | decorative: printed flat | nothing |

Icon: a low wide tray with a waterline. It differs from the Receiver (a taller basket with a goal rim) and from the Water tank (tall, with a port ring) by its low wide outline.

### Behaviour and numbers

- **Capture (L8, L9).** Only a stream whose path crosses the 1.25 × 1.0 rim rectangle from above enters. A stream that meets a wall's outer face or the bench first goes to `Spilled`. Nothing is captured at a distance.
- **Fill from a tank through an open tap.** The basin is full when the tank has given 0.5 m³, that is when the tank depth has fallen from 1.0 to 0.5 m: (2·A/K)·(√1.0 − √0.5) = (2 / 0.04820) × 0.2929 = **12.2 s**. The remaining 0.5 m³ arrives over the next 29.3 s and leaves over the rim (L9): the basin stays at 0.5 m³ and the overflow goes to `Spilled` unless something below catches it.
- **Where the stream lands.** From a tap `End` 0.5 m above the rim the stream lands at most 0.82 m out ([EL-003](EL-003-tap.md)), and nearer as the tank drains. A basin with its near inner wall under the tap and its 1.25 m length along the stream catches all of it.
- **Depth.** 0.5 m³ ÷ 1.25 m² = 0.40 m, the interior height. One mark is 0.05 m of depth.
- **Why these values.** Half a tank, so one full Water tank overflows it and the overflow lesson needs no second source; shallow, so the waterline is readable and a Float ([EL-016](EL-016-float.md), draught 0.16 m) has room.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body and baked colliders | present | tray |
| W22 authored state through Run, Reset, save, load | present | pose |
| W4 fluid port contract | present, untested | store declaration |
| W6 store with level | missing | `ENGINE-LIQUID` |
| W8 stream capture at an open aperture | missing | `ENGINE-LIQUID` (gap in its text) |
| W9 overflow and ledger | missing | `ENGINE-LIQUID` |
| W12 fill-fraction driver | missing | `ENGINE-LIQUID` |

Counts: 3 present (with W23 for the parameter), 1 untested, 0 narrower, 5 missing (with W13 for its unit); one owner. A fill-band goal reads the committed volume (L13) through the goal slice `engine-goal-progress-observation`; it is level data, not a parameter of this part.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, five colliders, one store with an aperture and an overflow point, one binding.
2. **Generic laws.** "A missed stream remains outside" is L8: the path meets something else first. "Overflow remains conserved" is L9 and L10.
3. **Three layers.** *Colliders:* five boxes. *Visuals:* the table above. *Animation:* the water box follows the committed fill.
4. **One-way flow and typing.** As EL-001.
5. **Flat-state counts.** Bodies 1 (static); colliders 5 boxes; joints 0; drives 0; sensors 0; liquid nodes 1 store; thermal nodes 1; mouths 0; open apertures 1; bindings 2; parameters 0.
6. **Performance fitness.** Five boxes, as the Receiver. The aperture is a bounded rectangle tested only against stream paths; the interior is one bounded region for immersion.
7. **Tests are scenarios.** Rows B1 to B7.

## Sources and legacy

- Requirement: "Open vessel collects intersecting liquid flow up to capacity"; outcome "A missed stream remains outside; overflow remains conserved." No variants.
- Component research [water](../../component-research.md#water) line 30 (finite store with capacity, explicit overflow) and line 15 (capture is a swept test, never a render-rate overlap); family boundary [fluids](../invest/profiles.md#fluids); refinement [S704](../invest/refinements.md#s704).
- **Legacy search.** As EL-001: no liquid collector at a6c914e. The finite-store rules are EL-001 rows 1 and 2; here too excess must spill, not be discarded. **Files harvested:** none.

## Acceptance outline

Authority: [element-004](../requirements.md#element-004). Tolerances L17. Planned class `CatchBasinTests`. Sources are a Water tank and Tap.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| B1 | Given a full tank with an open tap whose stream lands inside the rim, when Run advances 8 s, then the basin's gain equals the tank's loss within 0.004 m³ | `CatchBasinTests.TheBasinGainsWhatTheTankLoses` | B2 | — |
| B2 | Given the same construction with the basin moved 1.0 m sideways, when Run advances 8 s, then the basin holds 0 and `Spilled` equals the tank's loss | `CatchBasinTests.AMissedStreamStaysOutside` | B1 | a basin whose outer wall face, not its rim, is under the stream holds 0 |
| B3 | Given B1 run until the tank is empty (stall guard 125 s), then the basin holds 0.5 m³, `Spilled` holds 0.5 m³, and the ledger identity holds at every sampled tick (the level declares a Run length of 125 s, `ENGINE-RUN-LENGTH`, owner decisions 13 and 17) | `CatchBasinTests.OverflowIsConserved` | a tank authored at 0.5 m³ fills the basin and spills nothing | the basin reaches 0.5 m³ at 12.2 s within 10% |
| B4 | Given a second basin under the first basin's overflow point, then the lower basin receives the overflow | `CatchBasinTests.OverflowFallsFromTheDeclaredRimPoint` | lower basin moved aside: `Spilled` instead | — |
| B5 | Given a solid Wall between the tap and the basin, then the basin holds 0 (a stream does not pass a solid) | `CatchBasinTests.AStreamDoesNotPassASolid` | B1 | — |
| B6 | Given a run that filled the basin, then Reset restores `initial_volume` and zeroes `Spilled`; save and load keep the pose and `initial_volume`; a value outside 0 to 0.5 or non-finite is refused whole | `CatchBasinTests.ResetRestoresTheAuthoredVolume` | — | 0 and 0.5 admitted |
| B7 | **sequence-task-384:** given tank → tap → basin with a Drain ([EL-006](EL-006-drain.md)) under the basin's overflow point, when the tank empties (stall guard 125 s, as B3; the level declares a Run length of 125 s, `ENGINE-RUN-LENGTH`, owner decisions 13 and 17), then basin 0.5 m³ + `Drained` 0.5 m³ = the tank's 1.0 m³ and the water art shows the basin full | `CatchBasinTests.ATankEmptiesThroughATapIntoABasin` (a cross-part row owned by the EL-006 slice: the Drain is taught at 61a, after the basin at 61) | tap closed: nothing moves | — |

Generic cover: W22, W23. Chrome recipe (pending): place a Water tank and Tap, then a Catch basin under the spout; a second lane places the basin 1 m to the side.

## Open questions

All resolved; none needs the owner.

1. Capacity fixed or resizable: fixed (D13).
2. Fill-band goal: a goal that reads the committed volume, owned by `engine-goal-progress-observation`; no `TargetBand` parameter and no rim colour cue on the part, because a goal is level data and a part must not know it.
3. Spilled liquid: terminal in the `Spilled` account (L9). Puddles and floor run-off are not modelled; a Drain or a second vessel placed under the overflow point is how a player keeps it.

## Integration

- **Works with.** EL-001, EL-003, EL-005, EL-006, EL-007, EL-013, EL-014 to EL-018; none delivered.
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-334`; `sequence-task-557` not co-anchored with a todo; no `s425` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID`; `ENGINE-THERMAL`; EL-001; EL-003; S7. Row B7's drain term follows EL-006.
- **Files.** Adds `elements/CatchBasin/CatchBasin.csproj`, `CatchBasinElementDeclaration.cs`, `catch_basin.tscn`, `icon.svg`, `CuriousContraptions.tests/CatchBasinTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** Capture decided by anything but the stream path crossing the declared aperture; overflow that is discarded; a store that cannot start empty.
