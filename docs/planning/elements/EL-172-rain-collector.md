# EL-172 · Rain collector — named-identity spec

**Current delivery (24 October 2026):** [EL-172 delivery slice](../invest/named-elements.md#element-172). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-172 · Rain collector · Water |
| Anchors | [element-172](../requirements.md#element-172), co-anchored `sequence-task-725`; source record [campaign coverage](../requirements.md#campaign-element-coverage) and scope index [todo-340](../requirements.md#todo-340); integration [sequence-task-531](../requirements.md#sequence-task-531); ledger [element-172](../invest/named-elements.md#element-172), owner S465 |
| Disposition | To deliver as a single element. Not a duplicate of the Catch basin ([EL-004](EL-004-catch-basin.md)) or the Liquid funnel ([EL-005](EL-005-liquid-funnel.md)): it is the widest and shallowest catcher (1.56 m² over 0.125 m³) and it has a down-pipe mouth |
| Planned package | id `rain_collector`, directory `elements/RainCollector/`, title "Rain collector" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID` and `ENGINE-THERMAL` (Delivery) |
| Related | [EL-036](EL-036-sprinkler.md) and [EL-013](EL-013-water-nozzle.md) (what it catches), [EL-004](EL-004-catch-basin.md), [EL-005](EL-005-liquid-funnel.md), [EL-012](EL-012-water-pipe-cap.md), pipe kit |

## Declaration

Closest delivered shape: the Receiver's open five-box vessel; closest planned shape: the Catch basin with the Liquid funnel's down-pipe.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid store declaration of `ENGINE-LIQUID` (W4, W6).
- **Body.** One static body, mass 0. Origin at the centre of the tray's outer box, 1.33 × 0.14 × 1.33. As a static part it stays where it is placed, at any height.
- **Colliders (six).** Floor 1.33 × 0.06 × 1.33 at (0, −0.04, 0); two walls 0.04 × 0.08 × 1.33 at (±0.645, 0.03, 0); two walls 1.25 × 0.08 × 0.04 at (0, 0.03, ±0.645); down-pipe Tube, bore radius 0.10, outer radius 0.14, length 0.20, axis Y, at (0, −0.17, 0). Family vessel material (EL-001: restitution 0.12, friction 0.3, rolling resistance 0).
- **Store.** Local id 1. Capacity 0.125 m³ (2 marks, 2 kg); plan area 1.5625 m²; floor datum (0, −0.01, 0); **interior 1.25 × 0.08 × 1.25** (the aperture extruded to the floor datum, L4), from y = −0.01 to the rim at y = +0.07. Open aperture 1.25 × 1.25 at y = +0.07, normal +Y. Overflow point (0, 0.07, 0.665). Starts empty.
- **Store 1 is also a thermal node** (owner decision 20; [TH-04](TH-04-electrical-heating-plate.md) section 11, T2): heat capacity ρ·V × 1046 J/(kg·K), starting temperature declared ambient 288 K unless a level authors another; inflow mixes by mass and outflow leaves at the store's temperature. No row of this part reads temperature.
- **Mouth (one socket), below the floor datum (A7).** `End`, pipe bore, Discharge, capture 0.20 m and 20° (L5), into store 1, at the foot of the down-pipe (0, −0.27, 0), normal −Y: 0.26 m below the floor datum. Its head is the store's surface elevation while the store holds liquid and its own datum when the store is empty (A7). Unjoined it is a free end (L8).
- **Parameters.** None. Capacity is fixed (D13).
- **Presentation.** `Visual/Water` `ScaleY` and `LocalY` from the committed fill fraction (W12, missing).
- **Palette.** `ElementCategory.Structure`; order 546 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour teal (74/255, 171/255, 148/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tray` | Wide shallow tray, the five slabs above, teal `#4aab94` | solid; the five box colliders | body pose (static) |
| `Visual/Pipe` | Tube 0.20 long, outer radius 0.14, cream `#fff8e9`, under the centre | solid; the Tube collider (true hollow) | body pose |
| `Visual/Water` | Sheet 1.25 × 0.08 × 1.25, cyan `#66b8c9` at alpha 0.16 | decorative: enclosed, moved by a binding | presentation binding |
| `Visual/Drain` | Navy `#293954` ring on the tray floor at the centre, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_end` | Socket locator, bore 0.10 | none | nothing |

Icon: a wide flat tray with a short pipe under its centre. It differs from the Catch basin (deeper, no pipe) and the Liquid funnel (a cone) by its flat, wide outline.

### Behaviour and numbers

- **Only what lands in the opening is gathered (L8, L9).** Each sample path that ends inside the store's declared interior is credited to it; a sample that meets any other collider first is spilled (L8). A stream that passes beside the tray is not gathered.
- **Occlusion.** A Wall, a board or a ball above the tray takes the stream first: that liquid goes to `Spilled` (L8). Nothing is collected through a solid.
- **Open down-pipe.** With `End` free the store drains through one aperture with the head from surface to pipe foot: Q = K₀·√(0.26 + d). Empty it can pass 0.08349 × √0.26 = 0.0426 m³/s, nine times one Sprinkler stream (0.00461 m³/s, [EL-036](EL-036-sprinkler.md)): under a sprinkler it holds no visible depth and delivers a single stream straight down from the pipe. Once empty its head is the pipe foot's own datum, so nothing more leaves (A7).
- **Stated limit, a full tap.** Fed 0.0482 m³/s ([EL-003](EL-003-tap.md)) the depth would settle where K₀·√(0.26 + d) = 0.0482: √(0.26 + d) = 0.5773, d = 0.333 − 0.26 = 0.073 m, inside the 0.08 m tray by 0.007 m. A feed 10% higher (0.0530 m³/s) settles at (0.0530 / 0.08349)² − 0.26 = 0.143 m and overflows. This is the part's limit, not an acceptance row.
- **Finite storage.** With `End` capped ([EL-012](EL-012-water-pipe-cap.md)) or joined to a closed tap, it fills: one Sprinkler stream fills 0.125 m³ in 0.125 / 0.00461 = **27.1 s**; a tap at `opening` 0.125 (0.0103 m³/s) in 12.1 s; a tap at `opening` 0.5 (0.0341 m³/s) in 3.7 s. More is pending overflow and leaves at the overflow point in the next substep (L9), conserved.
- **Routed.** Joined to pipes, what it gathers follows L7 like any store.
- **Why these values.** An opening of 1.56 m² is the largest catcher in the family, big enough to take a Sprinkler stream as its landing point moves 0.5 m ([EL-036](EL-036-sprinkler.md)); two marks of storage is one Water bucket.

**Acceptance rig (rows N1 to N8).** A full Water tank ([EL-001](EL-001-finite-reservoir.md)) stands on a Wall 1.24 × 1.35 × 1.24, so its mouth and the Tap joined to it are 1.50 m above the bench. The collector is static and is placed with its rim at 1.00 m, 0.5 m under the Tap's free `End`, the stream landing inside its opening; its tray underside is then at 0.86 m and its pipe foot at 0.66 m. `End` is capped unless stated. Where a row frees `End`, a Catch basin ([EL-004](EL-004-catch-basin.md): interior 1.25 × 0.40 × 1.0, rim 0.55 m) stands on the bench under the pipe foot, 0.11 m below it.

## Engine capabilities

Map families (navigation only, D14): FiniteLedger, FluidAdvection, GeometryQuery, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body and colliders; W2 Tube collider | present | tray and pipe |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | store and mouth |
| W3 sockets and joins | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 1 socket (`End`), 0 immersed ports, 0 outlets |
| W6 store; W7 joins and flow | missing | `ENGINE-LIQUID` |
| W8 sample-path credit inside a declared interior; a sample that meets another collider first is spilled | missing | `ENGINE-LIQUID`; path candidates from `ENGINE-SPATIAL-QUERIES` |
| W9 pending overflow and ledger; W12 fill-fraction driver | missing | `ENGINE-LIQUID` |
| A store mouth below its floor datum (L5, A7) | missing | `ENGINE-LIQUID` |
| Liquid store as thermal node | missing | `ENGINE-THERMAL` |

Counts (by row): 2 present, 1 untested, 1 narrower, 5 missing.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, six colliders, one store with an interior, an aperture, an overflow point and one mouth, two bindings.
2. **Generic laws.** Collection is stream credit inside a declared interior (L8, L9). "An occluded or missed stream does not fill it" is that a sample meeting another collider first is spilled (L8). Drainage is L7 with the mouth-below-datum head rule (A7).
3. **Three layers.** *Colliders:* five boxes and a Tube. *Visuals:* the table above. *Animation:* the water sheet reads the committed fill.
4. **One-way flow and typing.** Liquid state → presentation; nothing flows back. Mouth id and role are enums; volumes f32.
5. **Flat-state counts.** Bodies 1 (static); colliders 6 (5 boxes, 1 Tube); joints 0; drives 0; sensors 0; signal nodes 0; liquid nodes 1 store; thermal nodes 1, one per store; sockets 1; immersed ports 0; outlets 0; open apertures 1; overflow points 1; bindings 2; parameters 0.
6. **Performance fitness.** Six colliders against the Catch basin's five; the sixth is the down-pipe. The collector runs no query of its own: capture is decided by each stream's sample paths (L18). A free `End` adds one stream's fixed sample paths.
7. **Tests are scenarios.** Rows N1 to N8.

## Sources and legacy

- Requirement: "Open catch surface gathers only intersecting droplets into finite storage"; outcome "An occluded or missed stream does not fill it." No variants. "Droplets" are streams in this family (L8, decided in EL-001).
- Integration [sequence-task-531](../requirements.md#sequence-task-531). Component research [water](../../component-research.md#water): "collection is accounted"; "capture never depends on render rate".
- **Legacy.** None at `a6c914e` (searched for "rain", "collector", "droplet", "water"). **Files harvested:** none.

## Acceptance outline

Authority: [element-172](../requirements.md#element-172). Tolerances L17. Planned class `RainCollectorTests`. Every row uses the acceptance rig; the Tap's `opening` is 0.125 unless stated, fixed for the Run.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| N1 | Given the stream landing inside the opening, when Run advances 8 s, then the collector holds what the tank lost within 0.004 m³ | `RainCollectorTests.AnIntersectingStreamIsGathered` | N2, N3 | — |
| N2 | Given the collector moved 1.5 m aside, when Run advances 8 s, then it holds nothing (under 0.004 m³) and `Spilled` holds the tank's loss | `RainCollectorTests.AMissedStreamDoesNotFillIt` | N1 | stream landing 0.05 m outside the rim: nothing |
| N3 | Given a Wall board 0.2 m above the opening under the stream, when Run advances 8 s, then the collector holds nothing and `Spilled` holds the tank's loss | `RainCollectorTests.AnOccludedStreamDoesNotFillIt` | N1 | board covering half the landing zone as the stream walks across it: the collector gains only while the landing point is uncovered |
| N4 | Given the Tap at `opening` 0.5, when Run advances until the tank has lost 0.20 m³ (stall guard 19 s), then the collector holds 0.125 m³ within 0.004 m³ and the other 0.075 m³ has left at the overflow point | `RainCollectorTests.StorageIsFinite` | N1 (8 s at `opening` 0.125: not yet full) | — |
| N5 | Given `End` free over the Catch basin, when Run advances 20 s, then the collector's depth stays under 0.01 m and the basin has gained the tank's loss within 0.01 m³ | `RainCollectorTests.AnOpenDownPipePassesItOn` | N1 | — |
| N6 | Given `End` free over the Catch basin and the Tap at `opening` 0.5 (0.0341 m³/s at first, under the empty collector's 0.0426 m³/s), when Run advances 20 s, then the collector's depth stays under 0.02 m and nothing leaves over the rim | `RainCollectorTests.TheDownPipeKeepsUpWithAHalfOpenTap` | N4 (`End` capped: it fills and overflows) | — |
| N7 | Given any of these runs, then the ledger identity holds at every sampled tick | `RainCollectorTests.GatheredLiquidIsConserved` | — | — |
| N8 | Given a run, then Reset restores an empty collector; save and load keep the pose and the join | `RainCollectorTests.ResetAndSaveLoadRestore` | — | — |

Hand calculations. N1: the Tap at 0.125 passes 0.0103 m³/s ([EL-003](EL-003-tap.md)), about 0.08 m³ in 8 s, under the 0.125 m³ capacity; its stream leaves at 0.0103 / 0.01885 = 0.55 m/s and lands 0.55 × √(2 × 0.5 / 9.81) = 0.18 m out. N4: the Tap at 0.5 has K = 0.03409; the tank loses 0.20 m³ in 2 × 1.0 × (1 − √0.8) / 0.03409 = 6.2 s, stall guard 3 × 6.2 = 19 s; its stream leaves at 1.81 m/s and lands 0.58 m out, inside the 1.25 m opening. N5: 20 s at the Tap's 0.125 opening takes the tank to (1 − 0.01028 × 20 / 2)² = 0.805 m³, a loss of 0.195 m³, under the basin's 0.5 m³; the inflow 0.0103 m³/s is under the 0.0426 m³/s the empty collector passes, so no depth builds. N6: a depth of 0.02 m would pass 0.08349 × √0.28 = 0.0442 m³/s, above the 0.0341 m³/s supplied, so the depth stays under 0.02 m.

Generic cover: W22. No wire port, no parameter. Later-partner rows with the Sprinkler are EL-036 S1, S2 and S5. Chrome recipe (pending): place the collector under a tap's stream, cap its pipe, watch it fill; slide a board over it.

## Open questions

All resolved; none needs the owner.

1. A rain source: none in this part. The requirement describes a catcher. Liquid arrives as streams from parts that have a supply; nothing in this batch declares a Source node, and `Supplied` stays zero.
2. Droplets as separate bodies: no (L8).
3. Pedestal: none. A static part is placed at any height.
4. Capacity parameter: none (D13).
5. A mouth below the floor datum: its head is the surface while the store holds liquid and its own datum when empty (A7), so the down-pipe adds 0.26 m of head and an empty collector passes nothing.

## Integration

- **Works with.** Wall (delivered) as an occluder and stand; EL-001, EL-003, EL-004, EL-008, EL-012, EL-013, EL-036 (not delivered).
- **Campaign.** Chapter 9 "Stored Potential", levels 81–90; reuse 91–100, 131–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element, before EL-036 (whose rows use it).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20); then EL-001, EL-003, EL-004, EL-012; then S7.
- **Files.** Adds `elements/RainCollector/RainCollector.csproj`, `RainCollectorElementDeclaration.cs`, `rain_collector.tscn`, `icon.svg`, `CuriousContraptions.tests/RainCollectorTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** A store mouth that cannot sit below the store's floor datum with the A7 head rule; a stream sample that ignores solids above an aperture.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: the ledger entry links only the campaign coverage row; `sequence-task-725` is co-anchored with `element-172`, not with a todo; no `s465` anchor.
