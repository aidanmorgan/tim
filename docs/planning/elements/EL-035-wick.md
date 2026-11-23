# EL-035 · Wick — named-identity spec

**Current delivery (24 October 2026):** [EL-035 delivery slice](../invest/named-elements.md#element-035). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-035 · Wick · Water |
| Anchors | [element-035](../requirements.md#element-035), co-anchored `sequence-task-588`; source record [todo-340](../requirements.md#todo-340); integration [sequence-task-531](../requirements.md#sequence-task-531); ledger [element-035](../invest/named-elements.md#element-035), owner S456 |
| Disposition | To deliver as a single element. It declares the same crested fitting as the Siphon ([EL-024](EL-024-primed-siphon.md)) with a self-priming rate and a far smaller conductance. Not a duplicate: those two values, the size and the absence of a prime mouth are deltas |
| Planned package | id `wick`, directory `elements/Wick/`, title "Wick" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID` and `ENGINE-THERMAL` (Delivery) |
| Related | [EL-024](EL-024-primed-siphon.md), [EL-034](EL-034-sponge.md), [EL-171](EL-171-squeeze-pad.md), [EL-004](EL-004-catch-basin.md), [EL-025](EL-025-tipping-bucket-water-clock.md) (a slow feed makes a slow clock), [EL-167](EL-167-solenoid-tap.md) (row X6); TH-28 is the thermal porous body |

## Declaration

Closest shape: the Siphon. The wick is rigid: a stiff porous bar bent over a rim, not a flexible strand, so the owner decision on connection strands (22 November 2026) does not apply.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid fitting declaration of `ENGINE-LIQUID` (W4, W7) with a crest (L3, L7, A9).
- **Body.** One static body, mass 0. Origin at the centre of the bridge; crest datum y = 0.
- **Colliders (three boxes).** Intake leg 0.06 × 0.60 × 0.06 at (−0.25, −0.30, 0); bridge 0.56 × 0.06 × 0.06 at the origin; discharge leg 0.06 × 0.90 × 0.06 at (+0.25, −0.45, 0). Material restitution 0.05, friction 0.6, rolling resistance 0, bounce threshold 0.1 m/s.
- **Liquid node.** One crested fitting (L3, L7, A9): crest datum y = 0; prime (saturation) volume V_p = 0.0039 m³ (2⁻⁸ m³, 0.0625 kg); self-priming rate 0.001 m³/s while the `Start` mouth is wet. It declares **no lift limit** (A9, coordinator decision 21): the lift is bounded by the 0.60 m intake leg, because the intake uncovers first.
- **Mouths (both immersed, L5, A1).** An immersed mouth is a fluid port of the one record with no socket; its node is resolved by the innermost interior that contains its point (A1), the same query as a depth probe. `Start`: Intake, at (−0.25, −0.60, 0). `End`: Discharge, at (+0.25, −0.90, 0); in no interior it is a free end (L8). Each is an aperture of 0.002828 m², K = 0.6 × 0.002828 × 4.42945 = 0.007516; two in series give **K_w = 0.007516 / √2 = 0.005315**.
- **No store, so no thermal node.** Owner decision 20 attaches a thermal node to every liquid store; the wick declares none. Its 0.0039 m³ prime inventory is fitting inventory; no row reads temperature.
- **Parameters.** None (D13).
- **Presentation.** `Visual/Wick` albedo from the committed prime fraction, dry cream to wet cyan (driver missing, W12).
- **Palette.** `ElementCategory.Structure`; palette order 544 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour ochre (194/255, 143/255, 82/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Wick` | Thin inverted U, the three boxes above, cream `#fff8e9` with ochre `#c28f52` cross-hatching printed flat; tints cyan when wet | solid; the three box colliders | body pose (static); colour by a presentation binding |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end` | Immersed-port locators | none | nothing |

Icon: a thin hatched inverted U with a drop at the long end. It differs from the Siphon (thick, clear, gold ring) by thickness and hatching.

### Behaviour and numbers

**Worked placement (every row unless stated).** The source Catch basin ([EL-004](EL-004-catch-basin.md): interior 1.25 × 0.40 × 1.0, floor datum (0, −0.125, 0), plan area 1.25 m², rim 0.55 m above its base) stands on a Wall 1.49 × 0.70 × 1.24 inside its own footprint, so its floor is 0.85 m and its rim 1.25 m above the bench. The wick hangs over the basin's +X wall with its bridge centre over the wall's outer face: the intake leg is 0.10 m clear of the inner face, the intake point 0.05 m above the source floor (0.90 m above the bench), the crest 0.65 m above the floor (1.50 m) and the outlet 0.25 m below the floor (0.60 m above the bench). The lower Catch basin stands on the bench against the stand: its rim is 0.55 m, so the outlet is 0.05 m above its rim and its discharge leg 0.10 m clear of its near inner wall face. Without the stand the outlet would be 0.10 m under the bench.

- **It primes itself (L7, A9).** With `Start` wet, the saturation volume fills from the source at 0.001 m³/s: 0.0039 / 0.001 = **3.9 s**. Then the fitting is primed and conducts.
- **It carries by head only (L7).** With source depth d the head is d + 0.25 (intake 0.05 m above the floor, outlet 0.25 m below it). Full (d = 0.40): first flow 0.005315 × √0.65 = **0.0043 m³/s** (0.069 kg/s).
- **Whole drain.** Depth 0.40 → 0.05 m takes (2 × 1.25 / 0.005315) × (√0.65 − √0.30) = 470.4 × 0.2585 = **122 s** and moves 0.35 × 1.25 = 0.4375 m³. A level that uses the whole drain declares a Run length of at least 125 s (`ENGINE-RUN-LENGTH`); no row asserts it, because its stall guard (3 × 122 s) would pass the 300 s maximum.
- **It is slow on purpose.** One mark (0.0625 m³) takes 0.0625 / 0.0043 = about 15 s at first: fed into a Tipping bucket it makes a 15 s clock where a tap makes a 6 s one.
- **A dry reservoir stops it.** When the surface falls to the intake point (depth 0.05 m, 0.0625 m³ left) `Start` is dry: the column breaks, the 0.0039 m³ in the wick leaves through `End`, and nothing more moves. Every cubic metre is in a store, in the wick or in an account at every tick (L10).
- **The lift is the leg.** There is no lift limit (A9). Raised 0.10 m, the intake is 0.15 m above the source floor and uncovers there: transport stops with 0.15 × 1.25 = **0.1875 m³** left. Raised so its intake is above a full basin's surface (raised more than 0.35 m), `Start` is never wet and it never primes.
- **No uphill delivery.** With the outlet immersed in a second basin whose surface is level with the source's, the two heads are equal (L7): the wick wets and holds 0.0039 m³, and delivers nothing. Once the prime has drawn the source 0.0039 / 1.25 = 0.003 m below the second basin, nothing runs back: `End` is Discharge (outward only) and `Start` Intake (inward only, L5).
- **Re-wetting.** Unlike the Siphon, a wick whose intake is covered again primes itself again after 3.9 s.
- **The drip.** It leaves `End` at Q/(Cd·a) = 0.0043 / (0.6 × 0.002828) = 2.5 m/s straight down (L8). Each sample path that ends inside the lower basin's declared interior is credited to it; a sample that meets any other collider first is spilled (L8).

## Engine capabilities

Map families (navigation only, D14): CapillaryTransport, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body and colliders | present | body |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | fitting |
| W3 sockets and joins | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 0 sockets, 2 immersed ports, 0 outlets; the immersed-port locators are `ENGINE-CAPABILITY-INSTANCES` |
| W7 fittings, apertures, flow; W8 free end | missing | `ENGINE-LIQUID` |
| Immersed mouth resolved by the innermost interior (L5, A1) | missing | `ENGINE-LIQUID`; its point query is `ENGINE-SPATIAL-QUERIES` |
| Crested fitting with a self-priming rate and no lift limit (L3, L7, A9) | missing | `ENGINE-LIQUID` |
| W12 driver for the committed prime fraction | missing | `ENGINE-LIQUID` |

Counts: 2 present, 1 untested, 1 narrower, 4 missing. No store, so no thermal-node row.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, three colliders, one crested fitting, two immersed ports, one binding.
2. **Generic laws.** Transport is L7 with a small declared aperture. Wetting, the stop on a dry source and the bounded lift are the crest rule (L7, A9) with a declared self-priming rate; the Siphon uses the same rule with rate 0. There is no capillary routine for this part.
3. **Three layers.** *Colliders:* three boxes. *Visuals:* the bar. *Animation:* the tint reads the committed prime fraction.
4. **One-way flow and typing.** Liquid state → presentation; nothing flows back. Mouth ids and roles are enums; values f32.
5. **Flat-state counts.** Bodies 1 (static); colliders 3 boxes; joints 0; drives 0; sensors 0; signal nodes 0; liquid nodes 1 crested fitting; thermal nodes 0 (no store); sockets 0; immersed ports 2; outlets 0; bindings 1; parameters 0.
6. **Performance fitness.** Three thin boxes (0.06 m), each thicker than the 1 cm wall the tunnelling envelope names; the part is static. Two point queries per substep (A1) and, while `End` is free, one stream's fixed sample paths, all from `ENGINE-SPATIAL-QUERIES` (L18).
7. **Tests are scenarios.** Rows X1 to X8.

## Sources and legacy

- Requirement: "Transports liquid through a declared porous medium under bounded capillary rules"; outcome "Dry reservoir stops transport; mass remains accounted for." No variants.
- Integration [sequence-task-531](../requirements.md#sequence-task-531). Component research [water](../../component-research.md#water): "bounded capillary transfer"; "Damp fraction ← volume".
- **Legacy.** None at `a6c914e` (searched for "wick", "capillar", "porous", "water"). **Files harvested:** none.

## Acceptance outline

Authority: [element-035](../requirements.md#element-035). Tolerances L17. Planned class `WickTests`. Rows use the worked placement. The prime takes 0.0039 m³ (0.003 m of depth) from the source before any flow.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| X1 | Given a full source basin, when Run advances 20 s, then the lower basin has gained 0.067 m³ within 10% and the ledger identity holds at every sampled tick | `WickTests.AWetWickCarriesLiquidDownhill` | X2 | — |
| X2 | Given an empty source basin, when Run advances 20 s, then the prime fraction stays 0 and every committed flow is under 0.0005 m³/s (L17) | `WickTests.ADrySourceCarriesNothing` | X1 | — |
| X3 | Given a source authored at 0.10 m³ (depth 0.08 m), when Run advances until flow stops (stall guard 46 s; the level declares a Run length of 50 s (`ENGINE-RUN-LENGTH`)), then the source depth is 0.05 m within 0.01 m (0.0625 m³), the prime fraction is 0 and the lower basin holds the rest (source + lower basin = 0.10 m³ within 0.004 m³) | `WickTests.TransportStopsWhenTheIntakeUncovers` | X1 | — |
| X4 | Given the wick raised 0.10 m over a source authored at 0.25 m³ (depth 0.20 m), when Run advances until flow stops (stall guard 70 s; the level declares a Run length of 70 s (`ENGINE-RUN-LENGTH`)), then the source depth is 0.15 m within 0.01 m (0.1875 m³) | `WickTests.ARaisedWickLeavesMoreBehind` | X3 | raised 0.40 m over a full source (intake 0.05 m above the surface): the prime fraction stays 0 for 10 s |
| X5 | Given a source authored at 0.10 m³ (surface 0.93 m above the bench) and the lower basin on a Wall 0.40 m high (floor 0.55 m, rim 0.95 m) authored at 0.475 m³ (depth 0.38 m, surface 0.93 m), so the outlet is immersed 0.33 m below a surface level with the source's, when Run advances 20 s, then the prime fraction reaches 1 within 12 s, the lower basin keeps 0.475 m³ within 0.004 m³ and every committed flow through `End` is under 0.0005 m³/s | `WickTests.ItDoesNotDeliverUphill` | X1 | — |
| X6 | Given X3's rig and a Water tank ([EL-001](EL-001-finite-reservoir.md)) authored at 0.25 m³ on a Wall 1.30 m high against the source basin's back face, joined to a Solenoid tap ([EL-167](EL-167-solenoid-tap.md)) whose free `End` is 0.20 m above the source rim and 0.18 m inside it, with Battery A on `PowerIn` and Battery B on `FirstIn` authored Disabled, when flow has stopped as in X3 and Battery B is then enabled (its `enabled` is a Run control), then the intake is covered again and the lower basin gains again within 12 s of the intake being covered; the level declares a Run length of 60 s (`ENGINE-RUN-LENGTH`) | `WickTests.ARecoveredIntakeWetsAgain` | Battery B left Disabled: the lower basin gains nothing after the stop | — |
| X7 | Given the committed flow at source depth 0.40 m once wet, then it is 0.0043 m³/s within 10% | `WickTests.FlowFollowsHeadThroughTheSmallAperture` | — | — |
| X8 | Given a run, then Reset restores a dry wick and both basins; save and load keep the pose; the art is cream when dry and tinted when wet | `WickTests.ResetSaveLoadAndTint` | — | — |

Hand calculations. X1: after the 3.9 s prime the head is 0.647 m; after 16.1 s of flow √h = √0.647 − 0.005315 × 16.1 / 2.5 = 0.8043 − 0.0342 = 0.7701, h = 0.593 m, so the depth falls 0.054 m: 0.054 × 1.25 = 0.067 m³. X3: 3.9 s prime, then 470.4 × (√0.327 − √0.30) = 11.3 s; total 15.2 s, guard 3 × 15.2 = 46 s. X4: raised 0.10 m the head is d + 0.15; 3.9 s prime, then 470.4 × (√0.347 − √0.30) = 19.4 s; total 23.3 s, guard 70 s. X5: guard 3 × 3.9 = 12 s. X6: tap flow on a 0.25 m tank is 0.0482 × √0.25 = 0.0241 m³/s ([EL-003](EL-003-tap.md)), so 0.0125 m³ (0.01 m of source depth) arrives in 0.5 s after the 0.15 s opening; guard 3 × 3.9 = 12 s after the intake is covered; X3's 46 s + 0.65 s + 12 s = 58.7 s, Run length 60 s. The tap's tube (outer radius 0.14) passes 0.06 m over the source rim. X6 needs EL-001 and EL-167 and runs when they are delivered.

Generic cover: W22. No port, no parameter. Chrome recipe (pending): stand a basin on a Wall block, hang the wick over its wall, place a second basin under the long end.

## Open questions

All resolved; none needs the owner.

1. A flexible strand or a rigid bar: a rigid bar. A flexible wick would be a connection strand and wait for `ENGINE-CONNECTION-MATERIALS`; nothing in the requirement needs it to flex.
2. Evaporation from the wick: none here; TH-28 is the evaporative body.
3. Length and rate parameters: none (D13).
4. A separate capillary law: none. It is the crested fitting with a self-priming rate, so the family keeps one flow law.
5. A lift limit: none (A9, coordinator decision 21). The requirement's "bounded capillary rules" are the intake leg's length and the crest rule.

## Integration

- **Works with.** EL-004, EL-001, EL-025, EL-031, EL-167 (none delivered); Wall and Battery (delivered).
- **Campaign.** Chapter 9 "Stored Potential", levels 81–90; reuse 91–100, 131–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element, after EL-024 (which proves the immersed mouth and the crested fitting).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20), plus `ENGINE-RUN-LENGTH` (rows X3, X4, X6); then EL-004, EL-024, and for row X6 EL-001 and EL-167; then S7.
- **Files.** Adds `elements/Wick/Wick.csproj`, `WickElementDeclaration.cs`, `wick.tscn`, `icon.svg`, `CuriousContraptions.tests/WickTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** A crested fitting without a self-priming rate; an aperture area that cannot be declared apart from a bore; immersed-port locators not admitted by `ENGINE-CAPABILITY-INSTANCES`.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-340`; `sequence-task-588` co-anchored with `element-035`, not with a todo; no `s456` anchor.
