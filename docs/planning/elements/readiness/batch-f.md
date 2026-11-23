# Batch F readiness: Water 1 (22)

> **Current pointers (cross-batch pass, 23 November 2026).** This summary is a dated record. Retired story names used below map to their current stories in the [engine stories index](engine-stories-index.md#3-retired-and-renamed-names) section 3; palette orders are the [palette allocation](palette-allocation.md)'s (coordinator decision 36); `epics.md:NNNN` citations are as of this summary's date (the specs cite stories by heading).

Written 22 November 2026 in checkout `spec-drafts` (`8d1d68aa` plus uncommitted planning drafts) and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the engine design audit `docs/planning/elements/readiness/engine-design-gap.md`, the decisions of 22 November 2026 (owner items 1 to 15 with 3a, coordinator item 16; section 11 records the propagation of 23 November 2026) and Batch G's summary. All 22 specs were changed in the repair. No product, test, ledger, requirement, epic or contract file was changed. Nothing was built or run.

[EL-001](../EL-001-finite-reservoir.md) owns the liquid conventions L1 to L18 and the shared capability probe W1 to W24; [EL-008](../EL-008-straight-water-pipe.md) owns the pipe-kit values. Batch G's additions LG1 to LG8 are adopted into L3 to L15, so both water batches read one text.

## 1. Readiness table

Counts are present / present, untested / narrower / missing. Palette order is "allocated by the cross-batch pass" everywhere. **Every row is blocked on the engine programme (owner decision 3a) first**; the Prerequisites column lists what follows it. **Conforms** means the declaration needs no element logic and every missing capability has exactly one owning story, stated element-free.

| Identity | Name | Disposition | Package id | Category | Counts | Owning stories after the programme | Prerequisites after the programme | Collaborators | Tier | Ready to plan a slice | Conformance |
|---|---|---|---|---|---|---|---|---|---|---|---|
| EL-001 | Finite reservoir | single element; owns L1–L18, W1–W25 | `reservoir` | Power | 3/1/1/7 | ENGINE-LIQUID; ENGINE-THERMAL (the store's thermal node, W25) | ENGINE-GAS-NETWORK (port record), ENGINE-LIQUID, ENGINE-THERMAL, S7 | every water part; TH-11 (authored `liquid_temperature`) | data-only | blocked on the engine programme, then ENGINE-GAS-NETWORK, ENGINE-LIQUID, ENGINE-THERMAL | Conforms |
| EL-002 | Header tank | single element | `header_tank` | Power | 4/1/1/6 | ENGINE-LIQUID | EL-001, EL-008, elbows, S7 | pipe kit, EL-022 | data-only | blocked, as EL-001 | Conforms |
| EL-003 | Tap | single element; base seat for EL-165 to EL-167 | `tap` | Control | 4/2/1/3 | ENGINE-LIQUID | EL-001, S7 | EL-001, EL-004 | data-only | blocked, as EL-001 | Conforms |
| EL-004 | Catch basin | single element; now has `initial_volume` | `catch_basin` | Structure | 3/1/0/5 | ENGINE-LIQUID | EL-001, EL-003, S7 | EL-006, EL-016 | data-only | blocked, as EL-001 | Conforms |
| EL-005 | Liquid funnel | single element | `liquid_funnel` | Structure | 3/1/1/5 | ENGINE-LIQUID | EL-001, EL-003, EL-004, EL-012, S7 | EL-008 | data-only | blocked, as EL-001 | Conforms |
| EL-006 | Drain | single element; the only sink | `drain` | Structure | 2/1/0/3 | ENGINE-LIQUID | EL-001, EL-003, EL-004, S7 | EL-004 | data-only | blocked, as EL-001 | Conforms |
| EL-007 | Open gutter | single element; a store with two channel mouths | `gutter` | Structure | 4/1/1/5 | ENGINE-LIQUID | EL-001, EL-003, EL-004, S7 | EL-021 | data-only | blocked, as EL-001 | Conforms |
| EL-008 | Straight water pipe | single element; owns kit values | `water_pipe` | Structure | 5/1/1/2 | ENGINE-LIQUID | EL-001, EL-004, S7 | kit | data-only | blocked, as EL-001 | Conforms |
| EL-009 | 45° water elbow | Family slice "Water elbows" with EL-010 | `water_elbow_45` | Structure | 3/1/1/2 | ENGINE-LIQUID | EL-008, S7 | kit | data-only | blocked, as EL-001 | Conforms |
| EL-010 | 90° water elbow | Family slice "Water elbows" with EL-009 | `water_elbow_90` | Structure | 3/1/1/2 | ENGINE-LIQUID | EL-008, S7 | kit, EL-012 | data-only | blocked, as EL-001 | Conforms |
| EL-011 | Water T junction | single element; first three-mouth part | `water_tee` | Structure | 3/1/1/2 | ENGINE-LIQUID (third socket: ENGINE-CAPABILITY-INSTANCES, in the programme) | EL-008, EL-012, S7 | kit | data-only | blocked, as EL-001 | Conforms |
| EL-012 | Water pipe cap | single element; radius now 0.15 m | `water_cap` | Structure | 3/1/1/1 | ENGINE-LIQUID | EL-008, S7 | every pipe-bore mouth | data-only | blocked, as EL-001 | Conforms |
| EL-013 | Water nozzle | single element; acceptance geometry rebuilt | `water_nozzle` | Structure | 3/1/1/2 | ENGINE-LIQUID | EL-002, EL-008, elbows, EL-004, S7 | EL-002, EL-022 | data-only | blocked, as EL-001 | Conforms |
| EL-014 | Water-carrying bucket | single element; bail restored; displacement envelope | `water_bucket` | Motion | 5/1/1/6 | ENGINE-LIQUID | EL-001, EL-003, EL-004, S7 | Rope anchor, Pulley, Weight (delivered) | data-only | blocked, as EL-001 | Conforms |
| EL-015 | Leaky bucket | single element after EL-014 | `leaky_bucket` | Motion | 5/1/1/7 | ENGINE-LIQUID (choice-bound values: ENGINE-PARAMETER-BOUND-FRAMES) | EL-014, EL-004, S7 | rope parts | data-only | blocked, as EL-001 | Conforms |
| EL-016 | Float | single element; first proof of immersion | `float` | Motion | 2/0/0/1 | ENGINE-LIQUID | EL-004, EL-001, S7 | delivered balls, Wall | data-only | blocked, as EL-001 | Conforms |
| EL-017 | Mechanical float valve | single element; reworked geometry | `float_valve` | Control | 6/1/1/5 | ENGINE-LIQUID | EL-001, EL-008, elbows, EL-004, EL-016, S7 | Wall (delivered) | data-only | blocked, as EL-001 | Conforms |
| EL-018 | Electronic level switch | single element | `level_switch` | Control | 3/1/1/2 | ENGINE-SIGNAL-NODES (threshold node), ENGINE-LIQUID (depth probe) | both, EL-002, EL-004, S7 | Battery, Signal lamp (delivered), EL-022 | data-only | blocked on the programme, then both | Conforms |
| EL-019 | Water check valve | single element; bulge now 0.15 m | `check_valve` | Control | 3/1/1/3 | ENGINE-LIQUID | EL-001, EL-008, S7 | EL-022 | data-only | blocked, as EL-001 | Conforms |
| EL-020 | Water diverter | single element | `water_diverter` | Control | 4/2/1/3 | ENGINE-LIQUID | EL-001, EL-008, EL-004, S7 | kit | data-only | blocked, as EL-001 | Conforms |
| EL-021 | Sluice gate | single element; preloaded blade; sloped rig | `sluice_gate` | Control | 5/2/1/4 | ENGINE-LIQUID; ENGINE-JOINT-KINDS (spring rest position; owner decision 6, coordinator decision 21); ENGINE-CONNECTION-MATERIALS (solid rope strand, owner decision 1) | EL-007, that repair, S7 | rope parts, EL-027, EL-028 | data-only | blocked on the programme, then ENGINE-GAS-NETWORK, ENGINE-LIQUID, ENGINE-JOINT-KINDS and ENGINE-CONNECTION-MATERIALS | Conforms |
| EL-022 | Water pump | single element; `supply` Choice; open when unpowered | `water_pump` | Power | 5/2/1/5 | ENGINE-LIQUID (work target); ENGINE-SUPPLY-DEMAND (in the programme) | EL-001, EL-002, EL-008, elbows, EL-018, EL-019, S7 | Battery, Motor (delivered), EL-240, EL-229 | data-only | blocked, as EL-001 | Conforms |

**Totals.** Dispositions: 20 single elements; 2 members of one Family slice; 0 duplicate closures, umbrellas or delivered parts. Readiness: 0 ready; 22 blocked on the engine programme and then on ENGINE-LIQUID, with the store parts (EL-001, EL-002, EL-004, EL-005, EL-007, EL-014, EL-015) also on ENGINE-THERMAL (owner decisions 18 and 20). Conformance: 22 Conform. The audit's shared finding G-1 and its part findings are applied (section 9 lists where the repair differs).

**Suggested order after ENGINE-LIQUID.** EL-001, EL-003, EL-004, EL-008, EL-012, the elbow family, EL-002, EL-006, EL-005, EL-007, EL-011, EL-013, EL-016, EL-014, EL-015, EL-019, EL-020, EL-017, EL-021, EL-018, EL-022.

## 2. Liquid conventions for other batches

Stated once in [EL-001](../EL-001-finite-reservoir.md) (L1 to L18, probe rows W1 to W25); cite it by number. The summary that stood in this section until 23 November 2026 was a second home and had gone stale, so it is removed. Changes made in EL-001 on 23 November 2026, so that readers of the earlier summary can find them:

- **Batch G's A1 to A9** (adopted by coordinator decision 21; exact text from `readiness/batch-g.md` section 2): nesting resolves to the innermost interior (A1; L4, L5, L8); envelope shielding (A2) and overlapping shapes counted once (A3) in L12 a; buoyancy reacted on a carrying body (A4, new L12 d); store contents at the centroid of the liquid region, a new mass-source kind (A5, L12 b, replacing "at the body's centre"); porous and sealed interiors (A6, L4); a mouth below the floor datum and the immersed port's node (A7, L5); the relief mouth (A8, L9); the crest rule counting granted work-target head, and no crest lift limit (A9, L3 and L7).
- **Thermal node (owner decision 20; Batch N section 7 item 1).** Every liquid store is also a thermal node with an authored starting temperature (L2; TH-04 section 11, T2); EL-001 declares it with the parameter `liquid_temperature` (273 to 368 K, default 288, clamp, build-only), new probe row W25 (`ENGINE-THERMAL`) and acceptance row R12. Every Batch F store part now names `ENGINE-THERMAL` after `ENGINE-LIQUID` (owner decision 18).
- **Delivered Weight.** It sinks between about 0.5 and 4.9 kg and floats outside that range (linear size law); a row that immerses one states its mass (coordinator decision 21).
- **Pressure** in L15's example is 400 000 Pa absolute (no kPa in a declared value).
- **Spring rest position** (W18, EL-021): `ENGINE-JOINT-KINDS` (owner decision 6; coordinator decision 21), not an unnamed "repair".

Gravity is the one uniform-field law (coordinator decision 16) under `ENGINE-FIELD-FORCE`; decisions 16 and 21 are coordinator decisions, and the specs now cite them so.

## 3. What Batch G must change to follow

1. **LG4:** "the excess leaves in the same step" becomes pending overflow leaving in the next substep (L9).
2. **LG2 and its contradiction note:** the crest rule is now L7 for every fitting, so a kit route does not siphon; delete the note "a pipe-kit route over a crest conducts unprimed".
3. **LG6 and EL-023:** "the edge stays closed" conflicts with coordinator decision 16 (an ungranted work target is an open edge that adds no head). The screw either accepts that liquid can run back down it when stopped, or declares a one-way seat in series.
4. **Story-text ownership:** "a part may declare a single mouth" and "several outlets" belong to `ENGINE-CAPABILITY-INSTANCES` (declared socket lists), not ENGINE-LIQUID; the port record belongs to `ENGINE-GAS-NETWORK`.
5. **Streams:** any part designed round "the first solid spills the stream" is re-read against sampled paths credited inside an interior; every store and sink declares an interior.
6. **Tilted stores:** every head or volume read on a tilted store (EL-025 and the Boat's bilge in particular) is recomputed with the one relation of L4.
7. **Values that moved:** capture distance 0.20 m; pipe-bore mouth axis 0.15 m above a part's base; Catch basin has `initial_volume`; Level switch `level` minimum 0.10; cap and check-valve radius 0.15; pump mouths at −0.05 and +0.15; Bucket has a three-box bail, tie at (0, 0.49, 0) and a displacement envelope; Float valve is a vertical tube with limits −45° to +5° and shut-off −25° to −5°; Sluice blade has a −0.75 m spring rest position and lifts with 2.5 kg (2.25 kg holds 0.126 m; 2.0 kg cannot); a gutter that must hold liquid is sloped toward a joined end.
8. **EL-003:** its opening is fixed during a Run; the sentence about EL-165 is removed.
9. **Wording:** every spec says "blocked on the engine programme (owner decision 3a)"; the threshold node is `ENGINE-SIGNAL-NODES`.
10. The earlier note that a rain source is a Source node is withdrawn; no water part declares a Source.

## 4. Decisions made under the standing delegation

Decisions of the first pass that stand: mouths as snap-joined scene mouths; one aperture law with lossless fittings; no packets or in-flight inventory; no jet or hydrostatic force on bodies; prismatic stores; categories by role; rim overflow; Header tank on a pedestal; build-only tap opening; no `TargetBand`; unlimited drain; gutter as one store; flush kit bands; T split by fitting head; three leak sizes as a Choice; one `level` with a fixed band and hold; stateless check valve; diverter without leakage; sluice as a gated channel fitting; pump with head and power caps. Decisions of the repair:

| Where | Decision | Reason |
|---|---|---|
| EL-001 L4, L11 | One level relation for every pose; retained capacity is that relation at the rim | Audit C-1: depth and tilt retention used two geometries |
| EL-001 L7 | Crest rule on every fitting, keyed to the supply surface | Batch G: kit pipes and the Siphon must obey one law |
| EL-001 L7, L9 | One pass per substep; pending overflow; inflow clamp in the wedge regime; outflows scaled to inflows; stability bound written down | Audit C-2, C-3, C-9 |
| EL-001 L8 | Sampled stream paths credited inside a declared interior; no exemption by kind | Coordinator decision 16; audit C-4 |
| EL-001 L12 | Displacement envelope for hollow vessels; damping value a starting value; contents at the body's centre, replaced on 23 November 2026 by contents at the liquid centroid (A5, coordinator decision 21) | Batch G LG7; audit M-7, H-3 |
| EL-001 L17 | A flow under 0.0005 m³/s counts as none | Audit C-9: "no flow" at a dynamic balance was an exact zero |
| EL-008 | Capture 0.20 m and 20°; mouth axis 0.15 m above a part's base | Audit C-6, M-2 |
| EL-014 | Bail restored as three boxes (bar 0.03 m); tie on the bail | The stream rule no longer forces art changes |
| EL-017 | Vertical housing with a downward outlet; limits −45° to +5°; shut-off −25° to −5°, default −20°, band 15°; reference rig with the hinge 0.60 m above a basin floor | Audit H-1; the vertical tube removes the stream's throw so the float and the stream fit one basin |
| EL-021 | Spring rest position −0.75 m (preload 10.5 N); upstream gutter sloped 5° toward the gate | Audit H-2a, H-2b |
| EL-022 | One input rule for both supplies; open when unpowered; row M7 has a control that fails without the valve | Coordinator decision 16; audit H-4, M-1 |

## 5. Needs the owner

None. The audit's five items are settled by coordinator decision 16 (one fluid port contract with two socket kinds; a head-capped gas-to-liquid edge; the unpowered pump open; the stream rule; the threshold node's owner) or adopted as stated (aperture area does not vary with submergence).

## 6. `ENGINE-LIQUID`: complete proposed text (for `epics.md:4489`; not edited here)

*Depends on the engine programme of owner decision 3a, in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-ISLANDS-SLEEP`, `ENGINE-CAPABILITY-INSTANCES` (declared snap sockets), `ENGINE-SUPPLY-DEMAND`, `ENGINE-SIGNAL-NODES` and `ENGINE-PARAMETER-BOUND-FRAMES`, and on `ENGINE-GAS-NETWORK` for the one fluid port record.*

*Missing (replaces the present list):*

"A liquid network compiled at Run from declared nodes: stores, zero-volume fittings, sinks and sources, with world accounts for spilled, drained and supplied volume. The liquid is one declared material row (density, immersion damping, discharge coefficient) to which other capabilities attach their rows. Liquid ports use the one fluid port record and its mouth-joined socket kind; each pair of joined snap sockets of equal bore compiles to one edge, and fittings joined to fittings merge into one node. (E-1: socket counts and ids are not this story's.)

A store declares capacity, plan area, interior height, floor datum points and its interior region, any of them optionally bound to real parameters. Level follows volume, displaced volume and committed tilt by one relation, and retained capacity is that relation at the rim (E-2). A store may be sealed and compliant, porous, or have its capacity bound to a committed coordinate.

Head is lost only at declared apertures (a store mouth, a free end, a seat or outlet with its declared area) by one orifice law; fitting flows sum to zero. Liquid passes a mouth datum only while the highest total head among the wet stores feeding that fitting, plus the head added by granted work targets between that store and the datum, is at or above it, unless the fitting declares a crest and its prime inventory is full (A9). Each substep is one pass: aperture flows from committed heads, then stream credits. Volume above retained capacity, counting displaced volume, is held as pending overflow and leaves from the declared overflow point in the next substep (E-3). A store never gives more than it holds, a store in the wedge regime never receives more than raises its surface to the supplying head, and a transfer between two stores never passes levelling. A fitting never stores or creates volume: after the declared sweeps its outflows are scaled to its inflows (E-4).

A free end, including a declared outlet, discharges a stream whose start velocity is the contracted-jet speed along its normal plus the owning body's velocity. A stream is a fixed, declared number of sample paths across its contracted section, each carrying an equal share; a sample that ends inside a store's or sink's declared interior is credited to it in the same step, and a sample that meets any other collider is spilled; no collider is exempt by kind (E-5). No in-flight inventory exists.

A dynamic body's displacement shapes (its colliders, or a declared envelope) inside a store's interior receive buoyancy and damping from the submerged volume, which also raises the level. A store on a dynamic body adds its contents to that body's mass at the centroid of the liquid region at the committed pose, with no added inertia (a new mass-source kind, A5), and captures at an aperture that follows the body. A declared capture region on a body may do paid work on a declared coordinate, at most a declared fraction of ρ·g × descent per unit volume, and releases the same volume from a declared outlet (E-7).

A seat's opening follows a parameter (including one value per option of a choice), a committed revolute or prismatic coordinate, a signal memory node or any committed scalar observation through a clamped linear map; a seat may be one-way with a cracking head. A declared exchange moves volume between a store and another capability's ledger (phase change), counted in the supplied and drained accounts (E-8). The liquid is one declared material row (E-9).

A work target adds head on a declared edge, limited by a declared maximum head and by declared efficiency times its input power. One input rule serves both supplies: the energy taken is the hydraulic work done divided by the efficiency; an electrical input debits that from its grant; a shaft input meets a resisting torque of that power divided by the shaft speed, never above a declared cap, in a declared sense (E-10). An ungranted work target adds no head and passes flow as an open edge. A gas node meets a liquid node only through a declared edge with its own head cap in metres.

A depth probe at a declared point reads the store whose interior contains it. Committed observations (store volume, depth, fraction; aperture flow; node pressure; account totals; seat opening; probe depth; prime fraction; cumulative edge volume) are readable by presentation drivers, threshold signal nodes and goals. Parameter units for cubic metres, cubic metres per second and pascals. Stores accumulate with the engine's bulk and remainder pair.

Candidates for stream samples and immersion come from the spatial query interface; a change of contents or level wakes the islands it loads; the step runs once per substep and is not repeated by a solver trial replay (E-6). Stores, apertures, fittings, seats, stream samples, open apertures, probes, envelopes and work targets compile to fixed tables sized at Run from the compiled declarations, with no scene capacity and no refusal for exceeding one (design contract D5; coordinator decision 26). Work per substep is bounded by those counts and by a declared sweep count and sample count; no pass is proportional to the scene."

*Specific acceptance (added to the two present lines):*

- "**And** a route's flow is unchanged when a lossless fitting is inserted, and two equal apertures in series pass 1/√2 of one."
- "**And** a route with no granted work target whose highest mouth datum is above its supply surface passes nothing however low its outlet; a crested fitting with a full prime passes the aperture law's flow."
- "**And** a tilted store's reported depth and its overflow point agree (E-11)."
- "**And** a cascade of three full vessels conserves volume at every committed tick (E-11)."
- "**And** a body lowered into a full store spills its displaced volume (E-11)."
- "**And** a stream sample that ends inside a store's interior adds exactly its share; one that meets another collider first adds to the spilled account; a body floating inside a store does not spill the stream that lands on it."
- "**And** a hollow body with a declared envelope floats at the draught its total mass requires."
- "**And** a seat driven by a held coordinate passes its held opening; a one-way seat passes nothing against its direction at any head."
- "**And** a work target with no grant adds no head and passes flow downhill; the energy taken is never less than the hydraulic work done divided by the declared efficiency, and a dry or stalled work target takes none."
- "**And** after two stores joined by one aperture have levelled, their level difference stays within the stated overshoot bound, and the reported flow stays under the stated no-flow allowance."
- "**And** each performance-relevant pass states a measured before and after on the fixed reference scene (owner decision 3a)."

*Unblocks* should name the depth probe's first consumer (EL-018), the first work target (EL-022), the first store on a dynamic body (EL-014), the first crested fitting (EL-024) and the paid-work capture region (EL-229, EL-240).

**Other stories.** `ENGINE-GAS-NETWORK`: "A fluid port is one record: a node, a role, an aperture area and a socket. A socket is either a scene mouth joined by adjacency or a link terminal joined by a connection of a fluid connection material. Both compile to the same network edge; no join rule depends on the medium." It reshapes or deletes `IFluidElement`. `ENGINE-CAPABILITY-INSTANCES`: its snap-socket list must admit one, two, three or four sockets per part, declared outlets that are not joinable, and per-socket bore, capture distance and angle. `ENGINE-SIGNAL-NODES`: the threshold node reads any committed scalar observation, with on and off levels and a hold in seconds. `ENGINE-SUPPLY-DEMAND`: "a work target is a supplied consumer: it declares a rated demand, receives a grant, and is debited the energy it took, never more than the grant". `ENGINE-JOINT-KINDS` (owner decision 6; coordinator decision 21): "a prismatic return spring declares its rest position, which may lie outside the travel (a preload)". No liquid clause is proposed for `ENGINE-CONNECTION-MATERIALS`: the earlier per-material stream flag is withdrawn.

## 7. Other proposed changes (files this batch may not edit)

- **`delivery-batches.md`** (not edited here; Batch G's section 6 edits closing EL-165 are listed with this batch's because the closure runs in the EL-003 slice). Line 77, 1c Liquid: "| Liquid | Actuated taps | EL-166, EL-167 (after EL-003) |" in place of "Taps | EL-165, EL-166, EL-167 (after EL-003)"; section 2b gains "| EL-165 Manual tap | EL-003 Tap; check: `TapTests` rows T2, T4, T5, T7 and T9 (`TapTests.TheOpeningIsFixedDuringRun`) |" and its heading count rises by one. Also D3 to read "no second way to join two mouths; gas and liquid share one fluid port record with two socket kinds". Under Liquid in 1c add a run "Liquid core" (EL-001, EL-003, EL-004, EL-002, EL-006, EL-005, EL-007) and a run "Bodies and controls" (EL-016, EL-014, EL-015, EL-019, EL-020, EL-017, EL-021, EL-018, EL-022); move EL-012 to directly after EL-008.
- **S7.** No Batch F identity meets the published binding's preconditions and no spec contains the binding heading. Each lacks its own `todo` co-anchored with its `sequence-task` (the ledger links the umbrellas `todo-334`, `-335`, `-337`, `-338`, shared by 7, 6, 5 and 7 identities; `sequence-task-554` to `-575` are co-anchored with the `element-NNN` anchors) and an owner anchor (`s422` to `s443` are absent from `scope-corrections.md`). EL-022 needs two mode records.
- **README "How to read a spec"** says six sections; these specs add Integration and Delivery, as Batch H's do.
- **[EL-052](../EL-052-water-tuned-bottle.md).** Its "liquid in flight" term is always zero; its volume-step disqualifier should name the family mark; its mouth is an open aperture over a declared interior.
- **[EL-229](../EL-229-hydraulic-piston.md), [EL-240](../EL-240-waterwheel.md).** Their paid-work capture is in the story text above (E-7). Their loop rows are named in EL-022; with the pump open when unpowered, a stopped loop drains back unless a check valve is placed.
- **Batch N, Batch O.** The scalar-driven seat and phase exchange (E-8), the material row (E-9) and the store interior are in the text above. Every liquid store is a thermal node (owner decision 20); EL-001 declares its starting temperature as the parameter `liquid_temperature`, which TH-11's warm feed uses (Batch N section 7 item 1).

## 8. Cross-batch items and contradictions

- **Palette needs (22 orders; none chosen).** Power 3: `reservoir`, `header_tank`, `water_pump`. Control 6: `tap`, `float_valve`, `level_switch`, `check_valve`, `water_diverter`, `sluice_gate`. Structure 10: `catch_basin`, `liquid_funnel`, `drain`, `gutter`, then six consecutive for the kit (`water_pipe`, `water_elbow_45`, `water_elbow_90`, `water_tee`, `water_cap`, `water_nozzle`). Motion 3: `float`, `water_bucket`, `leaky_bucket`.
- **The delivered Weight.** It sinks between about 0.5 and 4.9 kg and floats outside that range (linear size law, `elements/Weight/WeightElementDeclaration.cs:59-62`); a row that immerses one states its mass (coordinator decision 21). No Batch F row puts one in liquid.
- **Two things called buoyancy.** Settled: gravity is one uniform-field law with buoyancy scaled consistently (coordinator decision 16), and for the ball identities "buoyancy" means the declared ambient lift (owner decision 24); liquid immersion (L12) is a separate law that a level may add.
- **The bail costs a centred stream about a fifth.** This follows from the stream rule with no exemption by kind. A player fills a hung bucket off-centre or accepts the loss.
- **Aperture area ignores submergence**, so a shallow channel passes as much as a full one at equal head. Stated in L7 as a game simplification.
- **Lesson-fixture story S8** is now defined in [`delivery-batches.md`](../../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) and lands after the engine programme (owner decision 22).
- **Slider rest position.** Owner decision 6 presumes a field that `PrismaticSliderDescriptor` does not have; it is `ENGINE-JOINT-KINDS`' (coordinator decision 21), with a preload as EL-021 declares.

## 9. Where the repair differs from the audit

- **H-1 (float valve).** The audit's limits, shut-off range, band and 0.60 m rig are adopted. The housing is turned vertical with a downward outlet, which the audit did not propose: with a horizontal tube at hinge height the stream's throw plus the float's 1.08 m reach did not fit one basin. The audit's V4 (a Water tank drained by a tap) is replaced by two authored basin volumes, because the arm does not fit a 1.0 m tank.
- **C-4 consequence.** The audit said a bail "costs at most its shadow"; the spec states the size of that cost (about a fifth of a centred pipe-bore stream for a 0.03 m bar).
- **C-3.** Scaling outflows to inflows is adopted for conservation; the spec also says convergence within the declared sweeps is the story's to measure, since scaling can move a split away from the head law by more than the audit's "within 10%" if sweeps are too few.
- **Crest rule.** Keyed to the supply surface, as Batch G proposed, not to the fitting's own head: a fitting's head is below the supply surface whenever liquid moves, and a rule on it would stop ordinary routes that rise and fall below the surface.
- **G4 (gutter).** Kept, fed by two tank mouths, rather than deleted.
- **E-1.** The sentence about mouth counts is removed from ENGINE-LIQUID, and the need is restated under `ENGINE-CAPABILITY-INSTANCES` so it has an owner.
- **H-3.** Adopted with the Wall thickness stated (0.12 m, the delivered minimum), without which the 0.60 m well does not fit a 1.0 m tank.

## 10. Not verified or not done

- **Anvil.** EL-001, EL-017 and this file were validated with their full content before writing. The other 20 specs were validated twice: the added lines of each edit set before the edits, and the whole post-edit file (through a patch against the file on disk) before the final insertion. Two edit sets carried a few lines meant for a neighbouring spec in the same folder; the text scanned was the text written.
- No simulation was run. The level relation, the bounded step and the crest rule are checked by hand calculation; `ENGINE-LIQUID` planning must confirm them or change L4, L7 and L9 in EL-001.
- The sample count for streams, the sweep count for fittings and all table capacities are not stated anywhere; the specs name the story that must state them.
- Not re-opened in the repair: the form `ENGINE-CAPABILITY-INSTANCES` will give snap sockets (the specs still name mouth locators as `Loc_Port_mouth_*`); admission limits for a 0.10 m bore Tube, Torus arc or Frustum and for Tube colliders at 30° in one body; whether a rope tie is admitted on a slider body; a test of a parameter-driven art rotation; `DESIGN.md` colours; legacy `a6c914e` citations.
- EL-013's route (four elbows and three pipes ending 0.50 m above the bench) and EL-017's reference rig are dimensioned by arithmetic, not built.
- EL-022 row M7's lifted volume (about 0.11 m³ in 2 s) is an estimate from two operating points; the row asserts at least 0.09 m³.
- Batch G's specs were not edited; section 3 lists what they must change. Its LG6 conflict with the open unpowered work target is not resolved here.
- No spec was re-read end to end after the repair.

## 11. Propagation of 23 November 2026

Hand-backs and the checklist of the propagation brief, applied to the 22 specs. Planning only; nothing built or run. Anvil: most edits were validated before writing as the exact search and replace. Some (EL-001's W11, W18 and conformance edits, EL-003, EL-018, and this summary, whose section 2 replacement was validated with a placeholder hunk) were validated before writing as hand-written patches that did not apply exactly, so their added text was scanned but not as a patch that applies; after the last edit each such file was validated in full on its post-image (a no-change patch at the full tier). Every decision was `allow`.

**Hand-backs applied.**
- **EL-001** (Batch G section 2; Batch N section 7 item 1; owner decision 20; coordinator decision 21): A1 to A9 written into L3, L4, L5, L7, L8, L9 and L12 at the places Batch G names, with a mapping paragraph so Batch G's short names resolve; L12 b now puts contents at the liquid centroid (a new mass-source kind, W11); new L12 d (buoyancy reaction). Every store is a thermal node (L2); EL-001 declares `liquid_temperature` (Batch N's exact text: kelvin, 273 to 368, default 288, clamp, build-only), new probe row W25 (`ENGINE-THERMAL`), Reset and save row R12, counts updated; Status and Prerequisites add `ENGINE-GAS-NETWORK` and `ENGINE-THERMAL` (owner decision 18 order). The Weight sentence is the mass-dependent one. Also: "400 kPa" became 400 000 Pa; W18's rest position is `ENGINE-JOINT-KINDS`; R1 and R5 state their Run lengths; the Prerequisites cross-reference points to section 6 here and section 6 of Batch G.
- **EL-003** (Batch G section 6, via EL-165's proposed text): Disposition and open question 1 replaced; row T9 `TapTests.TheOpeningIsFixedDuringRun` added; conformance point 7 reads T1 to T9. EL-165's closure rows T2, T4, T5, T7 and T9 all exist.
- **`delivery-batches.md`** (not edited): Batch G's line-77 and section 2b edits for EL-165 are listed in section 7 above.
- **EL-018** (Batch G section 6): the lamp is a `StateValues` binding on the part's threshold signal node, the Pressure plate's form, with the node's committed phase under `ENGINE-SIGNAL-NODES`; the refused `CommittedSupply` driver is gone. No Batch F text names `ENGINE-GAS-NETWORK` as the threshold node's owner.
- **This summary:** section 2 no longer restates the conventions (it lists what changed in EL-001); the readiness table, section 6's rest-position owner, section 7's thermal and `delivery-batches.md` bullets and section 8's Weight, buoyancy and slider bullets are updated.

**Checklist (before → after, over the 22 specs).** Status rows with "blocked on the engine programme (owner decision 3a)": 2 → 22 (Prerequisites already had it). "Coverage binding (planned, not yet live)" with S7 widening the reader: 0 → 22. Store parts naming `ENGINE-THERMAL` and declaring the store a thermal node: 0 → 7 (EL-001, EL-002, EL-004, EL-005, EL-007, EL-014, EL-015). Present-tense sleep claims: 4 → 0 (EL-014, EL-016, EL-017, EL-021). kPa in a declared value: 1 → 0. Coordinator decisions 16 and 21 cited as owner decisions: 11 → 0. The unnamed "owner-decision-6 repair": 4 → 0 (now `ENGINE-JOINT-KINDS`). Rows over 30 s without a Run length: at least 6 → 0 (EL-001 R5, EL-004 B3 and B7, EL-015 Y3, EL-017 V2, EL-019 CV1; EL-002 H3 now states that it fits the default Run). Retired names, non-solid tether, 293.15, live binding heading: 0 → 0.

**Not done.** EL-013 row N3 ("run on until the tank's depth is below 0.6 m") gives no time or guard; by the spec's own nozzle figures it is a few seconds, but it was not re-derived. Citations of decisions 16 and 21 earlier in this summary were corrected where they carry an argument; the record of the first pass is otherwise unchanged.

## 12. Second-audit repair (23 November 2026)

Repaired against `docs/planning/elements/readiness/audits/reaudit-group-2.md` (group 2). Planning only; nothing built or run; Anvil allowed every write. The second audit found EL-007, EL-017 and EL-021 Ready with fixes; after this repair each is **Ready** (still blocked on the engine programme and the stories in its row).

| Spec | What changed |
|---|---|
| EL-007 | W3 and W4 owners named (`ENGINE-CAPABILITY-INSTANCES` for the socket form, `ENGINE-GAS-NETWORK` for the one fluid port record, coordinator decision 16). G4's rig: the audit's "two tanks side by side" puts the mouths 1.24 m apart over a 0.40 m channel, so the two tanks are stacked on Wall blocks on the channel's centre line, landing 1.29 m and 2.59 m along it; G1's contents check re-derived for the draining tap |
| EL-017 | Initial angle −34°; V4 runs 20 s and checks the last 10 s (the float's settling swing reopens the seat in the first seconds); stall guards three times the hand times with declared Run lengths; `ENGINE-GAS-NETWORK` named |
| EL-021 | No top beam (it blocked the solid rope): three frame boxes, four colliders; blocked on `ENGINE-CONNECTION-MATERIALS` (solid strand, owner decision 1) as well; SG1 compares the basin's gain rate with the tank's loss rate (the tap's flow falls as the tank drains) |

Cross-spec item 1 of the second audit (fluid port record owner) is resolved in all three.
