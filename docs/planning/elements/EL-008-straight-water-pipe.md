# EL-008 · Straight water pipe — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-008 delivery slice](../invest/named-elements.md#element-008). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md) and are cited by number. This spec holds the **pipe-kit values** that [EL-009](EL-009-water-elbow-45.md) to [EL-013](EL-013-water-nozzle.md) cite. Sizes are in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-008 · Straight water pipe · Water |
| Anchors | [element-008](../requirements.md#element-008), co-anchored `sequence-task-561`; source record [todo-335](../requirements.md#todo-335); integration [sequence-task-385](../requirements.md#sequence-task-385); ledger [element-008](../invest/named-elements.md#element-008), owner S429 |
| Disposition | To deliver as a single element, first of the "Water pipe kit" run ([delivery batches 1b](../invest/delivery-batches.md#1b-runs-each-its-own-slice-planning-reference-only)). Not a duplicate of the Clear pipe ([CAT-048](../requirements.md#current-cat-048), `pipe`): different bore, opaque, and a liquid fitting; balls do not pass it |
| Planned package | id `water_pipe`, directory `elements/WaterPipe/`, title "Water pipe" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | Kit: [EL-009](EL-009-water-elbow-45.md), [EL-010](EL-010-water-elbow-90.md), [EL-011](EL-011-water-t-junction.md), [EL-012](EL-012-water-pipe-cap.md), [EL-013](EL-013-water-nozzle.md); supplies [EL-001](EL-001-finite-reservoir.md), [EL-002](EL-002-header-tank.md) |

## Declaration

### Pipe-kit values (shared by EL-008 to EL-013, EL-003, EL-019, EL-020, EL-022)

- Pipe bore radius 0.10 m, outer radius 0.14 m (L5). Every kit collider is a true hollow shape from the admitted set.
- A kit mouth sits on the end face of the tube. The earlier 0.06 m proud collar is drawn as a flush navy band, so a kit part needs no collar collider.
- Fitting interiors are lossless (L7). A route's flow depends on its apertures and elevations, never on how many kit pieces build it.
- Kit colour: opaque cream `#fff8e9` with navy `#293954` end bands. The Clear ball pipe is transparent cyan and 6.5 times wider.
- Kit material: the family vessel material (EL-001).
- A pipe-bore mouth axis is 0.15 m above its part's base when the part rests on the bench, and no kit part reaches more than 0.15 m below its axis. Kit parts therefore sit on the bench beside a bench tank, whose mouth is at 0.15 m.
- Every kit mouth socket declares capture distance 0.20 m and capture angle 20° (L5). The ball pipe's 0.45 m was sized for a bore radius of 0.65 m; the Tap is only 0.30 m long.
- A kit route obeys the crest rule (L7): it cannot carry liquid over a point above its supply surface. Only a crested fitting with a prime (the Siphon, Batch G) can.

### Element declaration

Closest delivered shape: the Clear pipe (`elements/Pipe/PipeElementDeclaration.cs:13`), copied in form.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, `IParameterizedGeometryElement`, the liquid fitting declaration of `ENGINE-LIQUID` (W4, W7).
- **Body.** One static body, mass 0. Axis local X; origin at the centre. L is `length`.
- **Collider (one).** Tube: bore 0.10, outer radius 0.14, length L; its half-length is bound to `length` (multiplier 0.5), as the Clear pipe's shell (`PipeElementDeclaration.cs:52`).
- **Liquid node.** One fitting, zero volume, no seat.
- **Mouths.** `Start` at (−L/2, 0, 0), normal −X; `End` at (+L/2, 0, 0), normal +X; pipe bore, Bidirectional; positions bound to `length` (`TubeMouthParameterBinding`, as `PipeElementDeclaration.cs:46`). Either end may be upstream.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `length` | Real f32 | `ParameterUnit.Metre` | 2.0 | 0.5 | 6.0 | Reject; build only; resize handle |

- **Presentation.** `Visual/Tube` `ScaleY` and the two bands' `LocalX` from `length` through `PresentationParameterBinding`, as the Clear pipe (`PipeElementDeclaration.cs:22`–`:24`). No liquid-driven binding: the pipe is opaque.
- **Palette.** `ElementCategory.Structure`; palette order 522; the kit takes six consecutive orders: pipe 522, 45° elbow 523, 90° elbow 524, T 525, cap 526, nozzle 527 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cream.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tube` | Tube, outer radius 0.14, bore 0.10, length L, cream | solid, hollow; the Tube collider | body pose (static); length by the parameter |
| `Visual/BandStart`, `Visual/BandEnd` | Navy bands 0.06 wide at each end, flush | decorative: printed flat | parameter binding (position) |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end` | Locators, bore 0.10 | none | nothing |

Icon: a short cream tube with two dark end bands. It differs from the Clear pipe by being opaque and thin, and from the Tap by having no bonnet.

### Behaviour and numbers

- **A pipe changes where liquid leaves, not how hard it is to push.** A full Water tank with a pipe on its mouth and the pipe's far end free has two apertures (the tank mouth join and the free end): K = K₀/√2 = 0.05904. With the pipe level at tank-floor height the tank empties in 2·A·√d₀ / K = 2 / 0.05904 = **33.9 s**, the same for a 0.5 m pipe and a 6 m pipe.
- **A falling pipe adds head.** If the free end is z below the tank floor, Q = K·√(d + z): with z = 1.0 m and d = 1.0 m, Q = 0.05904 × 1.414 = 0.0835 m³/s.
- **A rising pipe delivers only below the surface.** A free end above the supply surface passes nothing ([EL-002](EL-002-header-tank.md) rows H1, H2).
- **A disconnected mouth cannot feed its neighbour.** A pipe mouth is not an open aperture (L9): a stream that reaches it from outside meets the tube wall or the bench and is spilled. Liquid enters a pipe only through a join.
- **Joins.** The authoring snap with the kit's declared capture values: within 0.20 m and 20° a free mouth of equal bore snaps; an occupied mouth takes no second pipe; a mover that is locked never snaps (L6). The harvest's 0.15 m and 10° and the ball pipe's 0.45 m are not used.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Tube collider; W5 parameter-bound collider and mouths | present | the whole body |
| W22, W23 authored state, refusals | present | `length` |
| W4 fluid port contract | present, untested | fitting |
| W3 mouth sockets, snap, equal bore, occupied mouth | narrower | the equal-bore and occupied-mouth rules are delivered; declared capture values and socket ids are `ENGINE-CAPABILITY-INSTANCES`'; compiling the join to an edge is `ENGINE-LIQUID`'s |
| W7 fittings, apertures and flow | missing | `ENGINE-LIQUID` |
| W8 free end and stream | missing | `ENGINE-LIQUID` |

Counts: 5 present, 1 untested, 1 narrower, 2 missing; one owner. Every geometric capability this part needs is delivered; only the liquid network is missing.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, one collider, one fitting, two mouths, one parameter.
2. **Generic laws.** Through-flow is L7 across a lossless fitting. "A disconnected mouth cannot feed its neighbour" is L6 (no join, no edge) and L9 (a mouth is not an aperture).
3. **Three layers.** *Collider:* one Tube. *Visuals:* the table above. *Animation:* none in a Run.
4. **One-way flow and typing.** Mouth ids and roles are enums.
5. **Flat-state counts.** Bodies 1 (static); colliders 1 Tube; joints 0; drives 0; sensors 0; liquid nodes 1 fitting; mouths 2; seats 0; bindings 3 parameter; parameters 1.
6. **Performance fitness.** One collider against the Clear pipe's three, because the bands are flush. Joined fittings merge into one network node at compile, so a long run of pipes costs the solve nothing more than one.
7. **Tests are scenarios.** Rows P1 to P10.

## Sources and legacy

- Requirement: "Sealed oriented conduit joins compatible fluid mouths"; outcome "A disconnected mouth cannot feed its neighbour." No variants.
- Component research [water](../../component-research.md#water) line 28; refinement [S705](../invest/refinements.md#s705); family boundary [fluids](../invest/profiles.md#fluids).
- **Legacy search.** No liquid pipe. The ball pipe's snap tests give the join rules, all of which the delivered snap already has:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A pipe placed near the joined position snaps to coincident mouths (< 0.0001 m, normals dot < −0.9999); the query does not change the part; once joined it reports no further snap. | `CuriousContraptions.tests/TubePlacementSnapTests.cs@a6c914e:L15-L52` | Carried forward; delivered (`engine/TubeMouthSnap.cs:27`). |
| 2 | Snapping is refused while running and works again after Reset. | `TubePlacementSnapTests.cs@a6c914e:L54-L85` | Carried forward: joins are authoring only (L6). |
| 3 | Distant or wrong-facing mouths do not snap. | `TubePlacementSnapTests.cs@a6c914e:L87-L101` | Carried forward with the kit's declared 0.20 m and 20°. |
| 4 | An occupied mouth and a locked part are not candidates; deleting the occupant frees the mouth. | `TubePlacementSnapTests.cs@a6c914e:L103-L121` | Carried forward; delivered. |
| 5 | Ball-pipe catalogue values (3.6 m, "Clear pipe"). | `parts/catalog/pipe.tres@a6c914e:L8-L20` | Not carried (CAT-048 owns them). |

**Files harvested:** `CuriousContraptions.tests/TubePlacementSnapTests.cs`, `parts/catalog/pipe.tres`.

## Acceptance outline

Authority: [element-008](../requirements.md#element-008). Tolerances L17. Planned class `WaterPipeTests`. The supply is a Water tank.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| P1 | Given a full tank with a level pipe joined to its mouth and a basin under the pipe's free end, when Run advances 10 s, then the tank holds (1 − 10/33.9)² = 0.497 m³ within 0.05 m³ and the basin holds the loss | `WaterPipeTests.AJoinedPipeCarriesTheSeriesFlow` | P2 | — |
| P2 | Given the pipe moved 0.3 m from the tank mouth, then the pipe carries nothing: the basin under its far end holds 0 and the tank's loss is in `Spilled` | `WaterPipeTests.ADisconnectedMouthCarriesNothing` | P1 | — |
| P3 | Given a second pipe lying 0.3 m beyond the first pipe's free end and in line with it, then it carries nothing and the stream is spilled or caught by what lies below | `WaterPipeTests.AStreamDoesNotEnterAPipeMouth` | joined end to end, both carry | — |
| P4 | Given pipes of `length` 0.5 and 6.0 in two lanes, then the tank volumes after 10 s are equal within 0.01 m³ | `WaterPipeTests.LengthDoesNotChangeTheFlow` | — | both limits |
| P5 | Given a pipe falling 1.0 m to its free end, then the starting flow is 0.0835 m³/s within 10% | `WaterPipeTests.AFallingPipeAddsItsDropToTheHead` | level pipe: 0.0590 m³/s | — |
| P6 | Given a water pipe offered to a tank mouth within 0.20 m and 20°, then it snaps; beyond either, or to an occupied mouth, or to a clear ball pipe mouth, it does not | `WaterPipeTests.WaterMouthsSnapByTheirDeclaredCapture` | — | just inside and just outside each limit |
| P7 | Given `length` outside 0.5 to 6.0 or non-finite, then it is refused whole; inside, collider and mouths follow it | `WaterPipeTests.LengthIsBoundedAndDrivesTheTube` | default | next f32 beyond each limit |
| P8 | Given a Tennis ball dropped on the pipe, then it rests on the tube (visible = solid) | `WaterPipeTests.TheTubeIsSolidOutside` | — | — |
| P9 | Given a run, then Reset restores the tank; save and load keep `length` and pose and the same mouths are joined | `WaterPipeTests.ResetAndSaveLoadKeepTheRoute` | — | — |
| P10 | Given a full tank and a route of elbows and pipes that rises 0.5 m above the tank's surface and then falls to a free end below the tank's floor, when Run advances 10 s, then nothing flows (crest rule) | `WaterPipeTests.AKitRouteDoesNotSiphonOverACrest` | the same route with its highest datum 0.2 m below the surface flows | the tank draining until its surface falls below the crest stops the flow |

**Kit integration ([sequence-task-385](../requirements.md#sequence-task-385)).** "Kit pieces join by compatible water ports; a capped route cannot leak through an invisible connection" is one cross-kit row, `WaterPipeKitTests.EveryKitPieceJoinsAndACappedRouteHoldsItsVolume`, run in the slice that delivers the last kit piece. Chrome recipe (pending): a tank and tap, a Water pipe dragged until it snaps, resized, ending over a basin.

## Open questions

All resolved; none needs the owner.

1. Head loss per length: none (kit values; L7).
2. Join tolerance: 0.20 m and 20°, declared on the sockets (one join mechanism, D3).
3. Capacity of the network: no fixed edge count is carried from the legacy; `ENGINE-LIQUID` states its table capacities and the typed refusal (L18).
4. Hollow so balls can pass: the collider is a true hollow Tube because the art is hollow, but nothing in the catalogue fits a 0.10 m bore. Ball routing stays the Clear pipe's.

## Integration

- **Works with.** Every part with a pipe-bore mouth; none delivered. It does not join the delivered Clear pipe family (different bore).
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-335`, shared by six identities; `sequence-task-561` not co-anchored with a todo; no `s429` anchor).

## Delivery

- **Slice shape.** Single element; the kit run is EL-008, the elbow Family slice, EL-011, EL-012, EL-013.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID`; EL-001; EL-004 for the receiver; S7. Row P10 needs the elbow family.
- **Files.** Adds `elements/WaterPipe/WaterPipe.csproj`, `WaterPipeElementDeclaration.cs`, `water_pipe.tscn`, `icon.svg`, `CuriousContraptions.tests/WaterPipeTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** A Tube collider with a 0.10 m bore refused by admission; fitting-to-store joins that add an aperture each side instead of one; a second bore class not kept apart by the snap.
