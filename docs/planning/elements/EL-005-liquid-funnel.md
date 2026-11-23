# EL-005 · Liquid funnel — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-005 delivery slice](../invest/named-elements.md#element-005). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md) and are cited by number. Sizes are in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-005 · Liquid funnel · Water |
| Anchors | [element-005](../requirements.md#element-005), co-anchored `sequence-task-558`; source record [todo-334](../requirements.md#todo-334); integration [sequence-task-384](../requirements.md#sequence-task-384) and [sequence-task-385](../requirements.md#sequence-task-385); ledger [element-005](../invest/named-elements.md#element-005), owner S426 |
| Disposition | To deliver as a single element. Not a duplicate of the delivered ball Funnel ([CAT-030](../requirements.md#current-cat-030), package `funnel`): different bore (bore radius 0.10 against 0.65 m), a liquid store and an open aperture, and the campaign lists "water funnel" separately |
| Planned package | id `liquid_funnel`, directory `elements/LiquidFunnel/`, title "Water funnel" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` and `ENGINE-THERMAL` |
| Related | [EL-004](EL-004-catch-basin.md), [EL-008](EL-008-straight-water-pipe.md), [EL-012](EL-012-water-pipe-cap.md), [EL-014](EL-014-water-carrying-bucket.md) (recipe "Catch the Escape") |

## Declaration

Closest delivered shape: the ball Funnel, a static Frustum wall with a Tube stub and mouth locators (`elements/Funnel/FunnelElementDeclaration.cs:10`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid store declaration of `ENGINE-LIQUID` (W4, W6).
- **Body.** One static body, mass 0. Axis local Y; origin at the middle of the cone.
- **Colliders (two, true hollow shapes).** Frustum wall: inner radius 0.50 at y = +0.30 tapering to 0.10 at y = −0.30, radial wall 0.04. Tube stub: bore 0.10, outer radius 0.14, from y = −0.30 to −0.36.
- **Material.** Family vessel material (EL-001).
- **Store.** Local id 1. Capacity = cone interior (π × 0.6 / 3) × (0.25 + 0.05 + 0.01) = **0.195 m³** (3.1 kg). Height from the mouth datum to the rim 0.66 m, so the equivalent plan area (L4) is 0.195 ÷ 0.66 = 0.295 m². Floor datum (0, −0.36, 0). Open aperture: disc radius 0.50 at y = +0.30, normal +Y. Overflow point (0.54, 0.30, 0). Starts empty. Its store is also a thermal node (owner decision 20; [TH-04 section 11](TH-04-electrical-heating-plate.md#thermal-conventions), T2), starting at the ambient 288 K (T1) unless a level authors another; inflow mixes by mass, outflow leaves at the store's temperature, and no row here reads temperature.
- **Mouth.** `End`, pipe bore, Discharge, into store 1, at (0, −0.36, 0), normal −Y. Unjoined, it is a free end; joined, it feeds the pipe.
- **Parameters.** None. Orientation by the rotate gizmo; a tilted funnel retains less (L11).
- **Presentation.** `Visual/Water` scale from the committed fill fraction (W12, missing).
- **Palette.** `ElementCategory.Structure`; palette order 520 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cream.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Cone` | Frustum shell as the collider, cream `#fff8e9` | solid, hollow; the Frustum collider | body pose (static) |
| `Visual/Stub` | Tube 0.06 long, outer radius 0.14, navy `#293954` | solid, hollow; the Tube collider | body pose |
| `Visual/RimBand` | Gold `#f7cb52` band at the rim, flush with the shell | decorative: printed flat | nothing |
| `Visual/Water` | Cone-shaped fill, cyan `#66b8c9`, alpha 0.16 | decorative: enclosed, moved by a binding | presentation binding |
| `Loc_Port_mouth_end` | Locator, bore 0.10 | none | nothing |

Icon: a cream cone with a narrow spout and a waterline. It differs from the ball Funnel (clear teal, lying on its side, far larger) by being opaque, upright and small.

### Behaviour and numbers

- **Level under a steady stream, mouth free (one aperture, K₀).** Depth settles where outflow equals inflow: d = (Q / K₀)².
  - Stream from a fully open tap at 1.0 m tank depth, 0.0482 m³/s: d = (0.0482 / 0.08349)² = **0.33 m**, below the 0.66 m rim: it passes everything.
  - Stream from a bare tank mouth at 1.0 m depth, 0.0835 m³/s: d = 1.0 m, above the rim: it overflows until the tank depth has fallen to 0.66 m. This is the "more than the outlet can pass" lesson.
- **Blocked outlet.** With a cap on the mouth nothing leaves; at 0.048 m³/s it fills its 0.195 m³ in about **4.1 s** and then overflows from the rim point (L9).
- **Head.** A pipe joined to the mouth is fed by the funnel's own surface, never by the tank above it: the funnel's free surface is the start of the head again.
- **No remote capture.** Only a stream path that crosses the 0.50 m disc from above is captured (L8).
- **Why these values.** A half-metre inlet catches a stream that wanders 0.8 m as a tank drains only if the player places it well; the pipe-bore outlet joins the kit.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Frustum and Tube colliders | present | body |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | store and mouth |
| W3 tube mouth and snap | narrower | the mouth; edges not compiled |
| W6, W7, W8, W9 store, flow, capture, overflow and tilt | missing | `ENGINE-LIQUID` |
| W12 fill-fraction driver | missing | `ENGINE-LIQUID` |

Counts: 3 present, 1 untested, 1 narrower, 5 missing; one owner.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, two hollow colliders, one store with a disc aperture, an overflow point and one mouth, one binding.
2. **Generic laws.** Guiding liquid to the outlet is a store draining through its mouth (L7). "Blocked outlet fills and spills" is an occupied mouth plus L9. "No remote capture" is L8.
3. **Three layers.** *Colliders:* Frustum and Tube. *Visuals:* the table above. *Animation:* the fill follows the committed fraction.
4. **One-way flow and typing.** As EL-001.
5. **Flat-state counts.** Bodies 1 (static); colliders 2 (1 Frustum, 1 Tube); joints 0; drives 0; sensors 0; liquid nodes 1 store; thermal nodes 1; mouths 1; apertures 1; open apertures 1; bindings 1; parameters 0.
6. **Performance fitness.** Two colliders against the delivered Funnel's larger set (its collars are separate solids; this part draws its rim band flush). The equivalent-area store costs the same as any store.
7. **Tests are scenarios.** Rows F1 to F7.

## Sources and legacy

- Requirement: "A tapered collector guides liquid toward a real outlet"; outcome "Blocked outlet fills and spills; no remote capture." No variants.
- Component research [water](../../component-research.md#water) lines 30 and 62 ("Catch the Escape").
- **Legacy search.** No liquid funnel at a6c914e. The ball funnel gives a geometric pattern only:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Ball funnel: half-length 0.9 m, inlet radius 1.3 m, shell 0.05 m; mouths on the axis beyond each collar; contact material (0.15, 0.1, 0.3). | `parts/FunnelPart.cs@a6c914e:L6-L17` | Not the dimensions (CAT-030 owns them). Carry forward the pattern: a wide inlet and a standard-bore outlet mouth. |
| 2 | Mouth record: id, position at the outside face, outward normal, bore radius. | `engine/TubeMouth.cs@a6c914e:L6-L14` | Carried forward; it is the delivered `TubeMouthDescriptor` (L5). |

**Files harvested:** `parts/FunnelPart.cs`, `engine/TubeMouth.cs`.

## Acceptance outline

Authority: [element-005](../requirements.md#element-005). Tolerances L17. Planned class `LiquidFunnelTests`. The source is a Water tank and an open Tap above the funnel.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| F1 | Given the tap stream entering the disc and a basin under the free mouth, when Run advances 8 s, then the basin's gain plus the funnel's contents equal the tank's loss, `Spilled` is 0, and the funnel's depth never passes 0.40 m | `LiquidFunnelTests.TheFunnelPassesAStreamToItsOutlet` | F3 | — |
| F2 | Given a cap on the mouth, when Run advances 15 s, then the funnel holds 0.195 m³, the basin holds 0, and the tank's remaining loss is in `Spilled` | `LiquidFunnelTests.ABlockedOutletFillsAndSpills` (a cross-part row owned by the EL-012 slice: the cap is taught at 62a, after the funnel at 61b) | F1 | the funnel is full by 4.1 s within 10% (stall guard 13 s) |
| F3 | Given the funnel moved 0.6 m off the stream, then the funnel and basin hold 0 and `Spilled` equals the tank's loss | `LiquidFunnelTests.NothingIsCapturedAtADistance` | F1 | a stream that meets the cone's outer wall, not the disc, is not captured |
| F4 | Given a bare tank mouth (no tap) at 1.0 m depth above the funnel, then the funnel overflows while the tank depth is above 0.66 m and stops overflowing below it; totals are conserved | `LiquidFunnelTests.InflowAboveTheOutletRateOverflows` | F1 | — |
| F5 | Given a water pipe joined to the mouth and rising to a free end 0.8 m above the funnel rim, then nothing leaves that free end (the funnel's surface, not the tank's, sets the head) | `LiquidFunnelTests.AFunnelRestartsTheHead` (a cross-part row owned by the "Water elbows" Family slice, EL-009 and EL-010: the pipe and elbows are taught at 62, after the funnel) | free end below the rim delivers | — |
| F6 | Given a funnel tilted 45°, then it retains less than its upright capacity and the difference leaves at the lowest rim point | `LiquidFunnelTests.ATiltedFunnelRetainsLess` | upright | — |
| F7 | Given a run, then Reset empties the funnel; save and load keep the pose and the mouth join | `LiquidFunnelTests.ResetEmptiesAndSaveLoadKeepsThePose` | — | — |

Generic cover: W22. Later-partner rows: the collector route of sequence-task-384 is EL-004 row B7, owned by the EL-006 slice; the kit join of sequence-task-385 runs in the slice that delivers the last kit piece (EL-008). Rows F2 and F5 are cross-part rows owned by the later parts' slices, as their test cells state, so no row of this slice needs a part the campaign teaches later. Chrome recipe (pending): place a tank and tap, a Water funnel under the spout, a pipe snapped to its outlet leading to a basin.

## Open questions

All resolved; none needs the owner.

1. Inlet radius: 0.5 m, not the ball funnel's 1.3 m (D1, D19).
2. Balls: a ball that fits may fall in; it is a body in a hollow collider and displaces liquid (L12). It does not pass the 0.10 m bore. Ball routing stays CAT-030's.
3. Head above its own rim: it cannot deliver it (row F5); confirmed as the teaching point.
4. Cone depth: the store is prismatic with an equivalent area (L4), decided now: one store shape in the engine, and no acceptance row depends on the cone's exact depth-to-volume curve.

## Integration

- **Works with.** EL-001, EL-003, EL-004, EL-008, EL-012, EL-014; none delivered.
- **Campaign.** Chapter 7, levels 61–70 ("water funnel"); reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-334`; `sequence-task-558` not co-anchored with a todo; no `s426` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID`; `ENGINE-THERMAL`; EL-001; EL-003; EL-004; S7. Rows F2 (EL-012 slice) and F5 (Water elbows slice) run in those later slices.
- **Files.** Adds `elements/LiquidFunnel/LiquidFunnel.csproj`, `LiquidFunnelElementDeclaration.cs`, `liquid_funnel.tscn`, `icon.svg`, `CuriousContraptions.tests/LiquidFunnelTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** A Frustum collider that the stream path does not treat as solid; a disc aperture not supported (only rectangles); tilt retention missing.
