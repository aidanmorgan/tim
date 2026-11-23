# EL-007 · Open gutter — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-007 delivery slice](../invest/named-elements.md#element-007). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md) and are cited by number. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-007 · Open gutter · Water |
| Anchors | [element-007](../requirements.md#element-007), co-anchored `sequence-task-560`; source record [todo-334](../requirements.md#todo-334); integration [sequence-task-384](../requirements.md#sequence-task-384); ledger [element-007](../invest/named-elements.md#element-007), owner S428 |
| Disposition | To deliver as a single element. It is a store with two channel-bore mouths: the general store law, not a channel solver of its own. "Trough" and "aqueduct" are names for it, not identities |
| Planned package | id `gutter`, directory `elements/Gutter/`, title "Open gutter" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK` (the one fluid port record, coordinator decision 16), `ENGINE-LIQUID` and `ENGINE-THERMAL` |
| Related | [EL-003](EL-003-tap.md), [EL-004](EL-004-catch-basin.md), [EL-006](EL-006-drain.md), [EL-021](EL-021-sluice-gate.md) (joins its ends), EL-025, EL-240 |

## Declaration

Closest delivered shapes: the Ramp for a long static part and the Clear pipe for a `length` parameter that drives colliders and mouths (`elements/Pipe/PipeElementDeclaration.cs:46`–`:55`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, `IParameterizedGeometryElement`, the liquid store declaration of `ENGINE-LIQUID` (W4, W6).
- **Body.** One static body, mass 0. The channel runs along local X; origin at the centre of the outer section. L is `length`.
- **Colliders (three boxes).** Floor L × 0.06 × 0.52 at (0, −0.10, 0); two walls L × 0.20 × 0.06 at (0, +0.03, ±0.23). Interior 0.40 wide × 0.20 deep; floor top at y = −0.07, rims at y = +0.13. Each box's X half-extent is bound to `length` (multiplier 0.5).
- **Material.** Family vessel material (EL-001).
- **Store.** Local id 1. Capacity 0.08·L m³ and plan area 0.40·L m², both bound to `length` (0.24 m³ and 1.2 m² at the default). Interior L × 0.20 × 0.40: in L4, w = L along the channel, b = 0.40, H = 0.20. Floor datums at both ends, (±L/2, −0.07, 0). Open aperture: rectangle L × 0.40 at y = +0.13, normal +Y. Overflow leaves at the lowest rim point (L11). Starts empty. Its store is also a thermal node (owner decision 20; [TH-04 section 11](TH-04-electrical-heating-plate.md#thermal-conventions), T2), starting at the ambient 288 K (T1) unless a level authors another; inflow mixes by mass, outflow leaves at the store's temperature, and no row here reads temperature.
- **Mouths.** `Start` at (−L/2, −0.07, 0), normal −X, and `End` at (+L/2, −0.07, 0), normal +X; channel bore 0.20, Bidirectional, into store 1, positions bound to `length`. Each is an aperture of 0.08 m² (K_c = 0.21261). Unjoined, an end is a free end at floor height. A channel mouth joins only another channel mouth (another gutter, or [EL-021](EL-021-sluice-gate.md)); it never joins a pipe-bore mouth (L6).

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `length` | Real f32 | `ParameterUnit.Metre` | 3.0 | 1.0 | 6.0 | Reject; build only; resize handle, as the pipe |

Slope is the part's pose (rotate gizmo), not a parameter.

- **Presentation.** `Visual/Water` scale and offset from the committed fill fraction (W12, missing); `Visual/Channel` and `Visual/Water` length from `length` through `PresentationParameterBinding`.
- **Palette.** `ElementCategory.Structure`; palette order 521 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour ochre (194/255, 143/255, 82/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Channel` | U-section, three slabs, ochre `#c28f52` outside, cream `#fff8e9` lining | solid; three box colliders | body pose (static); length by the parameter |
| `Visual/Water` | Box L × 0.20 × 0.40, cyan `#66b8c9`, alpha 0.16 | decorative: enclosed, moved by a binding | presentation binding |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end` | Locators, bore 0.20 | none | nothing |

Icon: a long open U-channel seen end-on with a waterline. It differs from the Ramp (a solid wedge with a rail) by its open U section, and from the Water pipe by being open and rectangular. A Basketball (radius 0.34 m) and a Tennis ball (0.25 m) are wider than the 0.40 m channel and ride on the rims.

### Behaviour and numbers

The gutter is a shallow store. Liquid leaves through whichever end mouth is wet, by L7; the surface is flat, so liquid collects at the low end (L4).

- **Sloped 5° down toward its `End`, default length, fed 0.0482 m³/s from an open tap.** The high end's datum is 3 × sin 5° = 0.26 m above the low end's, so only the low end is wet. The head at the low mouth settles at (Q / K_c)² = (0.0482 / 0.21261)² = **0.051 m**. By L4 the liquid is a wedge at the low end: depth d = 0.051 / cos 5° = 0.052 m, holding b·d² / (2·tan θ) = 0.40 × 0.052² / 0.175 = **0.006 m³**.
- **Carrying limit.** The gutter overtops when the surface reaches the rim at the low end, d = 0.20 m. The outflow is then 0.21261 × √(0.20 × cos 5°) = **0.095 m³/s**, for any slope that keeps the far end dry, and it holds C(θ) = 0.40 × 0.20² / 0.175 = 0.091 m³ (L11). One bare tank mouth at 1.0 m depth (0.0835 m³/s) stays inside that; two (0.167 m³/s) overtop at the low rim, conserved.
- **Closing an end.** Nothing in Batch F closes a channel mouth: an unjoined end is a free end. A gutter that must hold liquid is sloped toward a joined end ([EL-021](EL-021-sluice-gate.md)).
- **Adverse slope.** Tilted 5° the other way, the same feed leaves from the `Start` end. Nothing climbs: the high end stays dry.
- **Level.** Both ends are wet: depth (Q / 2K_c)² = (0.0482 / 0.42522)² = 0.013 m, and each end passes half.
- **Stream from an end.** Start speed Q/(Cd·A) = 0.0482 / 0.048 = 1.0 m/s along the end normal.
- **Two gutters joined end to end.** One aperture between two stores: the upstream depth sits (0.0482 / 0.21261)² = 0.051 m above the downstream depth.
- **Why 0.40 × 0.20.** Aperture area does not vary with submergence (L7), so the earlier 0.30 × 0.15 section needed 0.16 m of head to pass a tap's flow and overtopped. This section passes it at a quarter of its depth.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W5 parameter-bound colliders and mouths | present | channel |
| W22, W23 authored state, refusals | present | `length` |
| W4 fluid port contract | present, untested | store and mouths; the one fluid port record is `ENGINE-GAS-NETWORK`'s (coordinator decision 16) |
| W3 tube mouth, equal-bore rule | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20° are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID` | channel mouths |
| W6 store, with capacity and area bound to a parameter and two floor datums | missing | `ENGINE-LIQUID` (gap: parameter-bound store values) |
| W7, W8, W9 flow, streams, overflow and tilt | missing | `ENGINE-LIQUID` |
| W12 fill-fraction driver | missing | `ENGINE-LIQUID` |

Counts: 4 present, 1 untested, 1 narrower, 5 missing. Owners: `ENGINE-LIQUID`, with W3's socket form `ENGINE-CAPABILITY-INSTANCES` and W4's record `ENGINE-GAS-NETWORK`.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, three colliders, one store with two mouths and an aperture, one parameter.
2. **Generic laws.** "Carries liquid under gravity" is a tilted store draining through its lower mouth (L4, L7). "Adverse slope stalls or spills; no hidden uphill transport" follows because the higher mouth is dry. The earlier 0.25 m cell chain is gone: it was a transport routine for one part.
3. **Three layers.** *Colliders:* three boxes. *Visuals:* the table above. *Animation:* the water box follows the committed fill.
4. **One-way flow and typing.** As EL-001.
5. **Flat-state counts.** Bodies 1 (static); colliders 3 boxes; joints 0; drives 0; sensors 0; liquid nodes 1 store; thermal nodes 1; mouths 2; apertures 2; open apertures 1; bindings 2 liquid + 2 parameter; parameters 1.
6. **Performance fitness.** Three boxes, as the Clear pipe's three colliders. One store row whatever the length, where the cell design had 4 to 24.
7. **Tests are scenarios.** Rows G1 to G8.

## Sources and legacy

- Requirement: "Open channel carries liquid under gravity"; outcome "Adverse slope stalls or spills; no hidden uphill transport." No variants.
- Component research [water](../../component-research.md#water) lines 13 and 29 (downhill only, never an uphill pressure pipe), 49 (First Pour) and 66 (control: gutter sloped away).
- **Legacy search.** As EL-001: no channel element at a6c914e. **Files harvested:** none.

## Acceptance outline

Authority: [element-007](../requirements.md#element-007). Tolerances L17. Planned class `GutterTests`. The source is a Water tank and an open Tap over the gutter's high end.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| G1 | Given a gutter sloped 5° toward a basin under its low end, when Run advances 10 s, then the basin's gain plus the gutter's contents equal the tank's loss and `Spilled` is 0 | `GutterTests.ASlopedGutterCarriesLiquidToItsLowEnd` | G2 | at 10 s the contents are 0.002 m³ within 0.004 m³, not the 0.036 m³ a flat level would hold. Hand: the tap's flow falls with the tank's head, √h = 1 − 0.0482 × 10 / 2 = 0.759, so at 10 s it passes 0.0482 × 0.759 = 0.037 m³/s; low-mouth head (0.037 / 0.21261)² = 0.030 m; wedge 0.40 × 0.030² / 0.175 = 0.002 m³; flat level 1.2 × 0.030 = 0.036 m³ |
| G2 | Given the gutter sloped 5° away from the basin, then the basin holds 0 and the liquid leaves the other end to `Spilled` | `GutterTests.AnAdverseSlopeNeverDeliversUphill` | G1 | — |
| G3 | Given a level gutter fed at its middle with a basin under each end, then both basins gain equally within 10% | `GutterTests.ALevelGutterDrainsFromBothEnds` | G1 | — |
| G4 | Given two full Water tanks stacked on Wall blocks behind the 5° gutter's high end, turned so both bare mouths face +X on the channel's centre line, the lower mouth 0.30 m and the upper 1.45 m above the high end's rim (0.167 m³/s together at 1.0 m depth), then liquid leaves over the low rim as well as the end, and totals are conserved. Placement hand calculation: each stream leaves at 0.0835 / 0.01885 = 4.43 m/s; the rim falls 0.0875 m per metre, so the lower stream crosses the rim plane where 4.905t² = 0.30 + 0.0875 × 4.43t, t = 0.290 s, 1.29 m along the channel, and the upper where 4.905t² = 1.45 + 0.0875 × 4.43t, t = 0.585 s, 2.59 m along, both inside the 3 m channel. (Side by side the two mouths would be 1.24 m apart, wider than the 0.40 m channel.) | `GutterTests.InflowAboveTheCarryingRateOvertops` | the lower tank alone (0.0835 m³/s, head (0.0835 / 0.21261)² = 0.154 m < 0.199 m at the rim) does not overtop | — |
| G5 | Given a gutter end placed against a water pipe mouth or a clear ball pipe mouth, then no join forms | `GutterTests.AChannelMouthJoinsOnlyAChannelMouth` | two gutters end to end join and the second delivers | — |
| G6 | Given `length` 1.0 and 6.0, then both are admitted, the colliders and mouths are at ±L/2 and the capacity is 0.08·L; outside or non-finite is refused whole | `GutterTests.LengthIsBoundedAndDrivesTheChannel` | default 3.0 | next f32 beyond each limit |
| G7 | Given a Basketball rolled along the gutter, then it rides on the rims and is contained by neither wall face | `GutterTests.ABallRidesTheRims` | — | — |
| G8 | Given a run, then Reset empties the gutter; save and load keep `length` and the pose | `GutterTests.ResetEmptiesAndSaveLoadKeepsLengthAndPose` | — | — |

Generic cover: W22, W23. Later-partner rows: First Pour (sequence-task-384) uses G1 with EL-004; the sluice rows are in [EL-021](EL-021-sluice-gate.md). Chrome recipe (pending): a tank and tap, a gutter under the spout tilted 5° toward a basin and resized so its low end overhangs it.

## Open questions

All resolved; none needs the owner.

1. Cell length: no cells (one store, reason in conformance point 2).
2. A wide "trough" variant: no; the section is fixed (D13).
3. Balls inside the channel: no; they ride the rims.
4. Slope limit: none declared. A steep gutter retains almost nothing (L11) and simply behaves as a chute.

## Integration

- **Works with.** EL-001, EL-003, EL-004, EL-006, EL-021, EL-025, EL-240; none delivered.
- **Campaign.** Chapter 7, levels 61–70 (First Pour); reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-334`; `sequence-task-560` not co-anchored with a todo; no `s428` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-GAS-NETWORK` (the one fluid port record); `ENGINE-LIQUID`; `ENGINE-THERMAL`; EL-001; EL-003; EL-004; S7.
- **Files.** Adds `elements/Gutter/Gutter.csproj`, `GutterElementDeclaration.cs`, `gutter.tscn`, `icon.svg`, `CuriousContraptions.tests/GutterTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** Store values that cannot follow a parameter; a store with one floor datum only; a mouth's wet rule not evaluated at the committed pose; a second bore not admitted.
