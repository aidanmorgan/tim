# EL-003 · Tap — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-003 delivery slice](../invest/named-elements.md#element-003). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md) and are cited by number. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-003 · Tap · Water |
| Anchors | [element-003](../requirements.md#element-003), co-anchored `sequence-task-556`; source record [todo-334](../requirements.md#todo-334); integration [sequence-task-384](../requirements.md#sequence-task-384); ledger [element-003](../invest/named-elements.md#element-003), owner S424 |
| Disposition | To deliver as a single element with its own catalogue entry (D8). It is the base seat declaration: [EL-166](EL-166-mechanically-actuated-tap.md) and [EL-167](EL-167-solenoid-tap.md) copy its body, mouths and seat and differ in how the opening is set (L14 b); they are variants delivered after it (the "Actuated taps" family). [EL-165](EL-165-manual-tap.md) is closed as a duplicate in this slice by rows T2, T4, T5, T7 and T9. |
| Planned package | id `tap`, directory `elements/Tap/`, title "Tap" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID`, then EL-001 (itself after `ENGINE-THERMAL`) |
| Related | Supplies [EL-001](EL-001-finite-reservoir.md), [EL-002](EL-002-header-tank.md); receivers [EL-004](EL-004-catch-basin.md), [EL-007](EL-007-open-gutter.md) |

## Declaration

Closest delivered shape: the Clear pipe, a static tube with two mouths (`elements/Pipe/PipeElementDeclaration.cs:13`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, and the liquid fitting and seat declaration of `ENGINE-LIQUID` (W4, W7, W14).
- **Body.** One static body, mass 0. Origin at the tube centre; the tube axis is local X.
- **Colliders (two).** Tube: bore 0.10, outer radius 0.14, length 0.30, axis X. Bonnet: box 0.12 × 0.10 × 0.12 at (0, 0.19, 0), standing on the tube.
- **Material.** Family vessel material (EL-001).
- **Liquid node.** One fitting (zero volume) with one seat between its two mouths. Seat area = `opening` × A₀ (L14 a).
- **Mouths.** `Start` at (−0.15, 0, 0), normal −X; `End` at (0.15, 0, 0), normal +X. Both pipe bore, Bidirectional. An unjoined `End` is a free end (L8).

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `opening` | Real f32 | `ParameterUnit.None` (fraction of bore area) | 0 | 0 | 1 | Reject; build only (`ParameterRunEdit.BuildOnly`); player-editable unless the level locks it |

A placed tap is closed until the player opens it. The opening is fixed during a Run.

- **Presentation.** `Visual/Lever` `LocalRotationZ` from `opening` through `PresentationParameterBinding`: upright at 0, along the pipe at 1. Streams from the free end are drawn by the shared liquid presentation (W8); the package declares no stream art.
- **Palette.** `ElementCategory.Control`; palette order 407, shared by EL-165, which closes against this package (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour gold (247/255, 203/255, 82/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Body` | Tube 0.30 long, outer radius 0.14, cream `#fff8e9` | solid; the Tube collider (true hollow) | body pose (static) |
| `Visual/Bonnet` | Box 0.12 × 0.10 × 0.12, cream | solid; its box collider | body pose |
| `Visual/Lever` | Gold `#f7cb52` lever glyph on the bonnet's front face | decorative: printed flat, and turned by a binding | parameter binding |
| `Visual/Bands` | Navy `#293954` bands at both ends, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end` | Locators, bore 0.10 | none | nothing |

Icon: a short pipe with a square bonnet and a lever. It differs from the Water pipe by the bonnet and lever, and from the Check valve (a round bulge with an arrow) by outline. Open and closed read from the lever's direction, not from colour.

### Behaviour and numbers

- **Law.** The tap adds one seat aperture in series (L7). For a tank joined to the tap with the tap's `End` free there are three apertures: the tank mouth join (K₀), the seat (s·K₀) and the free end (K₀). So K(s) = K₀ / √(2 + 1/s²).

| `opening` s | K | Flow at 1.0 m depth | Time to empty a full Water tank (2·A·√d₀ / K) |
| --- | --- | --- | --- |
| 1 | 0.08349 / √3 = 0.04820 | 0.0482 m³/s (0.77 kg/s) | 41.5 s |
| 0.5 | 0.08349 / √6 = 0.03409 | 0.0341 m³/s | 58.7 s |
| 0.125 | 0.08349 / √66 = 0.01028 | 0.0103 m³/s | 195 s |
| 0 | 0 | 0 | never |

- Half open passes 71% of fully open, not half, because the two fixed apertures share the loss. The lesson is "more open, more flow", which holds at every setting.
- **Stream.** At s = 1 and 1.0 m depth the stream leaves at Q/(Cd·A₀) = 0.0482 / 0.01885 = 2.56 m/s; from 0.5 m above a rim it lands 2.56 × √(2 × 0.5 / 9.81) = 0.82 m out, and nearer as the tank drains.
- **Unconnected.** With `Start` unjoined the fitting has no supply and nothing leaves at any opening. A dry supply gives the same result.
- **It never adds head.** The seat is a passive aperture.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Tube collider | present | body |
| W22, W23 authored state, refusals | present | `opening` |
| W4 fluid port contract | present, untested | fitting and mouths |
| Parameter-driven art (`PresentationParameterBinding`, `CuriousContraptions.Abstractions/ElementPresentation.cs:119`; used by `elements/Pipe/PipeElementDeclaration.cs:22`) | present, untested | lever; no test of a parameter-driven rotation was opened |
| W3 tube mouth and snap | narrower | joins are not compiled to edges |
| W7 fittings, apertures, flow | missing | `ENGINE-LIQUID` |
| W8 free end and stream | missing | `ENGINE-LIQUID` |
| W14 seat opening from a parameter | missing | `ENGINE-LIQUID` (gap in its text) |

Counts: 4 present, 2 untested, 1 narrower, 3 missing; one owner.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, two colliders, one fitting with one seat, two mouths, one parameter, one parameter binding.
2. **Generic laws.** "Closing stops routed flow" is a seat of zero area in L7. "An unconnected tap emits nothing" is a fitting with no wet source. No row says "when the tap is closed, do X".
3. **Three layers.** *Colliders:* a Tube and a box. *Visuals:* the table above. *Animation:* none during a Run; the lever follows the authored parameter.
4. **One-way flow and typing.** Mouth ids and roles are enums; `opening` is f32.
5. **Flat-state counts.** Bodies 1 (static); colliders 2 (1 Tube, 1 box); joints 0; drives 0; sensors 0; liquid nodes 1 fitting; seats 1; mouths 2; bindings 1 parameter; parameters 1.
6. **Performance fitness.** Two colliders, fewer than the Clear pipe's three. A fitting adds no unknown of its own when it is merged with its neighbours (L7); the seat is one aperture row.
7. **Tests are scenarios.** Rows T1 to T9.

## Sources and legacy

- Requirement: "A bounded aperture regulates flow from a connected supply"; outcome "Closing stops routed flow; an unconnected tap emits nothing." No variants.
- Component research [water](../../component-research.md#water) line 27: "regulates existing supply … never free water"; lesson First Pour (line 49). Family boundary [fluids](../invest/profiles.md#fluids); refinement [S704](../invest/refinements.md#s704).
- **Legacy search.** EL-001's scope plus "valve" and "stopcock": no liquid valve at a6c914e. The only valve legacy is gas-domain (`engine/physics/AdiabaticGasDischarge.cs`) and belongs to the pneumatic family. **Files harvested:** none.

## Acceptance outline

Authority: [element-003](../requirements.md#element-003). Tolerances L17. Planned class `TapTests`. Every row uses a Water tank (EL-001) as the supply.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| T1 | Given a full tank joined to a tap with `opening` 1 and a free `End`, when Run advances 12 s, then the tank holds (1 − 12/41.5)² = 0.505 m³ within 0.05 m³ and `Spilled` equals the loss | `TapTests.AnOpenTapPassesTheSeriesFlow` | T2 | — |
| T2 | Given `opening` 0, when Run advances 20 s, then the tank keeps 1.0 m³ and no aperture reports flow | `TapTests.AClosedTapStopsRoutedFlow` | T1 | `opening` 0.125 passes 0.0103 m³/s within 10% |
| T3 | Given `opening` 0.5 and 1 in two lanes, then the committed flows at equal depth are in the ratio 0.71 within 10% | `TapTests.OpeningScalesTheSeatNotTheWholeRoute` | — | — |
| T4 | Given a tap with `Start` unjoined at `opening` 1, when Run advances 10 s, then `Spilled` is 0 and no flow is reported | `TapTests.AnUnconnectedTapEmitsNothing` | T1 | a tap 0.3 m from the tank mouth (not joined): the tank spills, the tap passes nothing |
| T5 | Given an open tap on an empty tank, then nothing leaves | `TapTests.AnEmptySupplyGivesNoStream` | T1 | tank at 0.0625 m³ empties and stops |
| T6 | Given `opening` −0.01, 1.01 or non-finite, then it is refused whole with a typed reason | `TapTests.OpeningOutsideZeroToOneIsRefused` | 0 and 1 are admitted | next f32 beyond each limit |
| T7 | Given a run, then Reset restores the tank and the accounts; save and load keep `opening` and the pose, and the joined mouths are joined again | `TapTests.ResetAndSaveLoadKeepTheOpening` | — | — |
| T8 | Given `opening` 0 and 1, then the lever art is upright and along the pipe | `TapTests.TheLeverShowsTheOpening` | — | 0.5 is halfway |
| T9 | Given a tap at `opening` 0.5 on a full tank, when Run starts, then the compiled Run-control table has no row for the tap, and the committed seat opening equals the authored value 0.5 (a copied parameter, not a simulated float) at every sampled tick for 12 s while the tank drains | `TapTests.TheOpeningIsFixedDuringRun` | a Battery in the same construction has its `enabled` row | — |

Wrong-kind connection: the tap has no wire port (W22), and a mouth of another bore does not join (EL-001 R9, W3). Later-partner row: [sequence-task-384](../requirements.md#sequence-task-384) runs in the EL-004 slice. Chrome recipe (pending): place a Water tank, drag a Tap until its `Start` snaps to the tank's port, drag the lever, place a basin where the stream lands.

## Open questions

All resolved; none needs the owner.

1. Own entry: its own entry (D8). EL-166 and EL-167 are variants delivered after it; EL-165 is closed as a duplicate in this slice.
2. Stepped or continuous opening: continuous (L2); the lever shows it.
3. Valve characteristic: the seat area is linear in `opening`; the route flow is not (table above). Kept because it is the one aperture law.

## Integration

- **Works with.** EL-001, EL-002 (supply); EL-004, EL-005, EL-007, EL-014 (receivers); the pipe kit. None delivered.
- **Campaign.** Chapter 7, levels 61–70 (First Pour); reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15), and says how EL-165, closed here as a duplicate, is counted. **Blocked on the coverage follow-on story S7** (umbrella `todo-334`; `sequence-task-556` not co-anchored with a todo; no `s424` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` and `ENGINE-CAPABILITY-INSTANCES`; then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then EL-001, which waits for `ENGINE-THERMAL` (owner decisions 18 and 20); S7. The slice also records EL-165's closure (rows T2, T4, T5, T7, T9).
- **Files.** Adds `elements/Tap/Tap.csproj`, `TapElementDeclaration.cs`, `tap.tscn`, `icon.svg`, `CuriousContraptions.tests/TapTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** A seat whose opening cannot come from a parameter; a free end that discharges without a wet source; fitting interiors that add loss.
