# EL-029 · Boat — named-identity spec

**Current delivery (24 October 2026):** [EL-029 delivery slice](../invest/named-elements.md#element-029). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

**Repair (audit C-2, C-3, M-12, L-3).** Cargo in the bilge displaces bilge liquid, so a leaky boat with a Basketball stays afloat; the sinking row now carries a 2.0 kg Weight, which sinks in the bilge. Nesting, envelope shielding and the buoyancy reaction are A1, A2 and A4. The sail has its own envelope box.

**Repair after the second audit (23 November 2026, `docs/planning/elements/readiness/audits/reaudit-group-2.md`, EL-029).** Two High findings. (1) Every boat started keel-at-surface, so a 3.0 kg load dipped about 0.52 m on its first downswing, past the 0.35 m gunwale, and V5 and its control failed; each row now places the boat at the sound draught of its authored load, at rest. (2) The leak port at (0, −0.185, 0) lay below the keel, so a boat resting on a floor put it below the floor datum, in no interior, a case the port rule left undefined; the port now sits just outside the hull's floor slab, 0.025 m above the keel, where it is inside the surrounding store's interior whether the boat floats or rests on the floor.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-029 · Boat · Water |
| Anchors | [element-029](../requirements.md#element-029), co-anchored `sequence-task-582`; source record [todo-339](../requirements.md#todo-339); ledger [element-029](../invest/named-elements.md#element-029), owner S450 |
| Disposition | To deliver as a single element. Not a member of the "Floating bodies" family: it is a hollow hull, so it needs a displacement envelope (L12 a), a bilge store and a leak seat |
| Planned package | id `boat`, directory `elements/Boat/`, title "Boat" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID`, `ENGINE-THERMAL` and `ENGINE-PARAMETER-BOUND-FRAMES` |
| Related | [EL-028](EL-028-buoyant-platform.md), [EL-169](EL-169-cork-float.md), [EL-170](EL-170-raft.md), [EL-015](EL-015-leaky-bucket.md) (a leak as a Choice), [EL-027](EL-027-canal-lock-chamber.md), Fan, Weight (delivered) |

## Declaration

Closest delivered shapes: the Tennis ball for a dynamic body with an airflow receiver (`elements/TennisBall/TennisBallElementDeclaration.cs:27`) and Batch F's bucket ([EL-014](EL-014-water-carrying-bucket.md)) for a store on a dynamic body.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, `IAirflowReceiverElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:45`), and the liquid store and fluid port declarations of `ENGINE-LIQUID` on the one fluid port record of `ENGINE-GAS-NETWORK` (W4, W6).
- **Body.** One dynamic body. `PayloadPolicy.None`. Origin at the centre of the hull's outer box, 0.90 × 0.35 × 0.80; the keel is at y = −0.175.
- **Colliders (six boxes).** Floor 0.90 × 0.05 × 0.80 at (0, −0.15, 0); two sides 0.90 × 0.30 × 0.04 at (0, 0.025, ±0.38); two ends 0.04 × 0.30 × 0.72 at (±0.43, 0.025, 0); sail 0.04 × 0.50 × 0.60 at (0.43, 0.425, 0), standing on the bow end.
- **Mass and inertia.** 1.0 kg; centre of mass declared at (0, −0.10, 0), 0.075 m above the keel. Box rule on the outer hull: 0.0635, 0.1208 and 0.0777 kg·m² about X, Y and Z. Bilge contents add ρ·V at the centroid of the bilge's liquid region with no added inertia (A5). Gravity (0, −9.81, 0).
- **Material (f32).** Restitution 0.2, friction 0.5, rolling resistance 0.03, bounce threshold 0.1 m/s.
- **Displacement envelope (L12 a).** Two boxes: the outer hull 0.90 × 0.35 × 0.80 at the origin (0.252 m³, waterplane 0.72 m²) and the sail 0.04 × 0.50 × 0.60 at (0.43, 0.425, 0) (0.012 m³). The envelope replaces all of the body's colliders for immersion; the region inside it is excluded from any surrounding store's interior, so cargo inside the hull is not immersed in the outside liquid (A2).
- **Bilge store.** Local id 1 on the body. Interior 0.82 × 0.30 × 0.72 (the hull's inside), floor datum (0, −0.125, 0); open aperture 0.82 × 0.72 at y = +0.175, moving with the body; capacity 0.177 m³; plan area 0.59 m²; tilt retention by L11 with w = 0.72 (across) or 0.82 (along) and H = 0.30; overflow point at the stern rim (−0.41, 0.175, 0). Starts empty. Its interior lies inside the envelope, so it is the innermost interior for any point in it (A1): cargo in the hull is immersed in the bilge, and buoyancy on that cargo is reacted on the hull (A4). Store 1 is also a thermal node (owner decision 20; [TH-04](TH-04-electrical-heating-plate.md) section 11, T2): heat capacity ρ·V × 1046 J/(kg·K), starting temperature declared ambient 288 K unless a level authors another; inflow mixes by mass and outflow leaves at the store's temperature. No row of this part reads temperature.
- **Leak.** One seat of area 0.0201 m² (radius 0.08 m, K = 0.6 × 0.0201 × 4.42945 = 0.0534) between the bilge store and one immersed port, Bidirectional, at (0, −0.15, 0.405): 0.005 m outside the starboard (+Z) face of the hull's floor slab (the floor spans z ±0.40 and y −0.175 to −0.125) and 0.025 m above the keel, so outside the envelope and inside the surrounding store's interior whether the boat floats (it is 0.062 m under the surface at the empty draught) or rests on that store's floor (it is then 0.025 m above the floor datum). The bilge's end of the seat is 0.025 m below the bilge's floor datum (A7, a mouth below the floor datum: the bilge's pressure there counts that drop, so the head across the seat stays the outside surface above the bilge surface). An immersed mouth is a fluid port of the one record with no socket; its node is resolved by the innermost interior that contains its point (A1), the same query as a depth probe. While its point lies inside no interior (the boat on the bench, or lifted clear of every store) it is sealed: it passes nothing in either direction, and the bilge keeps its contents (EL-001 L5; it is never a free end, so a beached or sunk hull does not spill its bilge to `Spilled`). The seat's opening is one value per option of `hull` (L14 a): 0 for Sound, 1 for Leaky.
- **Airflow receiver.** One, on the sail collider, sampled as the Tennis ball's is (`AirflowSampling.ColliderNormalized`).

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `hull` | Choice | None | 0 (Sound) | 0 | 1 | Reject; build only; options `Sound`, `Leaky` |

- **Ports.** No wire port. Fluid: 0 sockets, 1 immersed port, 0 outlets (socket and locator counts are `ENGINE-CAPABILITY-INSTANCES`').
- **Presentation.** `Visual` follows the body's pose. `Visual/Bilge` scale from the committed fill fraction of store 1 (W12, missing). `Visual/Plug` visibility from `hull` through `PresentationParameterBinding`.
- **Palette.** `ElementCategory.Motion`; palette order 151 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour blue (69/255, 99/255, 156/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Hull` | Open box, the five hull slabs, blue `#45639c` outside, cream `#fff8e9` inside | solid; the five hull box colliders | body pose (dynamic) |
| `Visual/Sail` | Board 0.04 × 0.50 × 0.60, cream with a coral `#f06e54` stripe printed flat | solid; the sail box collider | body pose |
| `Visual/Bilge` | Cyan box inside the hull | decorative: enclosed, moved by a binding | presentation binding |
| `Visual/Plug` | Gold `#f7cb52` disc, flush in the starboard face of the floor slab at the leak port (0, −0.15, 0.40); hidden when Leaky, where a navy hole is printed | decorative: printed flat | parameter binding |
| `Visual/Line` | Navy waterline band 0.087 m above the keel, flush | decorative: printed flat | body pose |

Icon: an open blue hull with a square sail at the bow. It differs from the Floating deck (a slab) and the Raft (logs) by its hollow hull and sail.

### Behaviour and numbers

Flotation is L12 a on the envelope; the bilge is a store whose contents weigh at their centroid (A5); the leak is L7 through one seat; cargo in the hull is immersed in the bilge only (A1, A2) and its buoyancy is reacted on the hull (A4), so the hull always carries boat + cargo + bilge liquid. No route is authored: the boat moves only by forces. Water is 16 kg/m³, so 16 × 0.72 = 11.52 kg per metre of draught.

- **Draught.** D = (1.0 + m_cargo + 16·V_b) / 11.52. Empty and sound: 1.0 / 11.52 = **0.087 m**. With a Basketball (1.0 kg): 0.174 m. With a 2.0 kg Weight: 3.0 / 11.52 = **0.260 m**.
- **Overload.** The hull envelope lifts at most 16 × 0.252 = 4.03 kg at the gunwale, 4.22 kg with the sail under too. A Bowling ball (4 kg) makes 5.0 kg: the boat sinks.
- **The delivered Weight in liquid.** A delivered Weight sinks between about 0.5 and 4.9 kg and floats outside that range (linear size law, `elements/Weight/WeightElementDeclaration.cs:59-62`); a row that immerses one states its mass. The 2.0 kg Weight is a cylinder of radius 0.04808 × 2 + 0.15933 = 0.2555 m and height 2 × (0.03818 × 2 + 0.12653) = 0.406 m: 24.0 kg/m³, so it rests on the bilge floor. Its cross-section, 0.2051 m², stands through the 0.30 m bilge; its flange and bands overlap the cylinder and add under 0.001 m³ (A3).
- **Stability with cargo.** Sound, with the 2.0 kg Weight centred: KB = 0.130 m, BM = (0.90 × 0.80³ / 12) / (3.0 / 16) = 0.205 m, KG = (1.0 × 0.075 + 2.0 × 0.253) / 3.0 = 0.194 m, GM = **0.141 m**. With a Basketball (radius 0.34 m, centre 0.39 m above the keel): KG = 0.2325 m, GM = 0.087 + 0.307 − 0.2325 = 0.16 m across and 0.24 m along; the ball can roll only 0.02 m across and 0.07 m along before it meets a wall, a heel of at most 0.07 / (2 × 0.24) = 0.146 rad (8.4°), which lowers an end by 0.066 m against 0.176 m of freeboard.
- **Leak, no cargo.** The head across the leak is the outside surface above the bilge surface: h = (D − 0.05) − V_b / 0.59 = 0.0368 − 0.305·V_b. It closes at V_b = **0.120 m³**, after 2·√0.0368 / (0.305 × 0.0534) = **23.6 s**; the boat then rides 0.254 m deep with 0.096 m of freeboard. With the bilge's free surface its metacentric height is 0.127 + 0.210 − 0.126 − 0.139 = **0.072 m**: still stable.
- **Leak, with a Basketball.** The ball sits in the bilge and displaces bilge liquid (cap volume π·y²·(1.02 − y)/3 at bilge depth y), so the bilge rises faster than its volume alone. Solving y + 0.05 = (2.0 + 16·(0.59·y − cap(y))) / 11.52 gives y = 0.263 m, V_b = 0.100 m³, draught 0.313 m: the leak closes with 0.037 m of freeboard and the boat stays afloat. The ball would float in the bilge only above y = 0.285 m. This is not an acceptance row: 0.037 m of freeboard is inside the end drop a rolling ball causes.
- **Leak, with a 2.0 kg Weight.** The Weight takes 0.2051 m² of the bilge's 0.5904, so the bilge depth is V_b / 0.3853 and h = (3.0 + 16·V_b) / 11.52 − 0.05 − V_b / 0.3853 = 0.2104 − 1.206·V_b. Starting at rest at its sound draught 0.260 m (V_b = 0, h = 0.2104 m, a first flow of 0.0534 × √0.2104 = 0.0245 m³/s), the gunwale goes under when D = 0.35, at V_b = (0.35 × 11.52 − 3.0) / 16 = **0.0645 m³** (4.03 kg in all), after 2·(√0.2104 − √0.1326) / (1.206 × 0.0534) = **2.9 s**, with 0.13 m of head still driving the leak. It sinks: the bilge keeps filling toward 0.3853 × 0.30 = 0.116 m³ (4.85 kg in all), above the 4.22 kg the whole envelope lifts. A sound boat carries the same Weight at 0.260 m.
- **Rain.** A stream sample that ends inside the bilge's interior is credited to the bilge (A1, L8); a sample that meets the sail first is spilled. The bilge's overflow point moves with the hull, and its pending overflow leaves as an L8 stream whose start velocity is the hull's velocity at that point ([EL-001](EL-001-finite-reservoir.md) L9). No row here overflows the bilge. A full bilge (0.177 m³, 2.83 kg) leaves an empty sound boat afloat at 3.83 / 11.52 = 0.333 m of draught with 0.017 m of freeboard.
- **No flooding over the gunwale.** Liquid enters only through the leak seat or as a credited stream sample, because the region inside the envelope is not part of the surrounding store (A2). An overloaded boat sinks because its mass exceeds the envelope's lift. A game simplification, stated once here.
- **Transport.** A Fan's airflow on the sail pushes the boat; immersion damping (L12 a) slows it. Any force moves a floating body, so V7's 0.20 m in 10 s needs only about 0.01 N. c = 1.0 s⁻¹ is a starting value that `ENGINE-LIQUID` sets for a damping ratio near 0.5 (L12 a).
- **Fit.** 0.90 × 0.80 fits the Lock chamber (1.0 × 1.0) and the Catch basin (1.25 × 1.0).

## Engine capabilities

Map families (navigation only, D14): Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W21 dynamic body with declared mass, centre and inertia | present | hull |
| W22, W23 authored state, refusals | present | pose, `hull` |
| G5 airflow receiver on a body | present for spheres (`AirflowGeneralityTests.OneOfFourOccludedSamplesRemovesItsOwnQuarterOfDemand`, `CuriousContraptions.tests/AirflowGeneralityTests.cs:138`; sample extent from the collider's support function, `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:113`, `:133-139`); untested on a box, V7 is its first proof | sail |
| W4 fluid port contract | present, untested | bilge store, immersed port |
| W3 sockets and locators | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK`. This part: 0 sockets, 1 immersed port | leak |
| W11 contents add to the carrying body's mass | narrower | gas at the body's centre only; liquid at its centroid (A5): `ENGINE-LIQUID` |
| W6 store; W8 stream credit into an interior on a body; W9 tilt retention | missing | `ENGINE-LIQUID` |
| W10 immersion with a displacement envelope; nesting, shielding, union and reaction (A1 to A4) | missing | `ENGINE-LIQUID` |
| W14 seat opening (L14 a); immersed port (L5) | missing | `ENGINE-LIQUID` |
| Seat value per Choice option | missing | `ENGINE-PARAMETER-BOUND-FRAMES` |
| W12 fill-fraction driver | missing | `ENGINE-LIQUID` |
| Liquid store as thermal node | missing | `ENGINE-THERMAL` |

Counts: 2 present, 2 untested, 2 narrower, 7 missing. Owners: `ENGINE-LIQUID`, `ENGINE-PARAMETER-BOUND-FRAMES`, `ENGINE-THERMAL`, `ENGINE-CAPABILITY-INSTANCES` (W3), one per capability.

### Engine conformance (22 November 2026)

1. **Declaration data only.** One dynamic body, six colliders, two envelope boxes, one store with an aperture, one seat with an immersed port, one airflow receiver, one Choice, two bindings. No route, no script.
2. **Generic laws.** Floating is L12 a on a declared envelope. "A leak changes flotation" is L7 into a store whose contents weigh at their centroid (A5). "Overload changes flotation" is mass against the envelope's lift. Cargo in the hull meets the bilge by A1, A2 and A4. Propulsion is the delivered airflow law on a declared receiver.
3. **Three layers.** *Colliders:* six boxes. *Visuals:* follow the body. *Animation:* the bilge box reads the committed fill; the plug reads the authored Choice.
4. **One-way flow and typing.** `hull` is a typed Choice; values f32.
5. **Flat-state counts.** Bodies 1 (dynamic); colliders 6 boxes; envelope boxes 2; joints 0; drives 0; sensors 0; airflow receivers 1; liquid nodes 1 store; thermal nodes 1; seats 1; sockets 0; immersed ports 1; outlets 0; open apertures 1; overflow points 1; wire ports 0; bindings 2; parameters 1.
6. **Performance fitness.** Six boxes against the Water bucket's five; the sixth is the sail. One point query for the immersed port and one interior query for immersion, with candidates from `ENGINE-SPATIAL-QUERIES` (L18). A floating boat is never at rest in the sleeping sense while liquid moves; nothing assumes sleeping exists.
7. **Tests are scenarios.** Rows V1 to V9.

## Sources and legacy

- Requirement: "Hull displaces liquid and transports its actual cargo"; outcome "Leak or overload changes flotation; no authored route animation." The campaign row keeps "distinct hull/load modes" as separate rows: rows V1 to V5.
- Component research [water](../../component-research.md#water): "Dynamic body + displacement"; recipe Canal Lift.
- **Legacy.** None at `a6c914e` (searched for "boat", "hull", "float", "water"). **Files harvested:** none.

## Acceptance outline

Authority: [element-029](../requirements.md#element-029). Tolerances L17. Planned class `BoatTests`. The vessel is a Lock chamber with `initial_volume` 0.80 m³ (depth 0.80 m) and a closed Sluice gate (no Weight, [EL-021](EL-021-sluice-gate.md)) on each of its two sockets, so that neither socket is a free end, unless stated. The boat is placed at the sound draught of its authored load, with its cargo set at rest, at Run start: 0.087 m empty, (1.0 + 1.0) / 11.52 = 0.174 m with a Basketball, (1.0 + 2.0) / 11.52 = 0.260 m with the 2.0 kg Weight, and (1.0 + 0.35) / 11.52 = 0.117 m with V7's Tennis ball; it therefore starts at rest at equilibrium and does not bob (the keel-at-surface start dropped a 3.0 kg load about 0.52 m on its first downswing: k = 156.96 × 0.72 = 113 N/m, ω = √(113 / 3.0) = 6.1 rad/s, a 0.26 m step overshooting to about twice the step). V3's overloaded boat has no sound draught (5.0 kg against the envelope's 4.22 kg), so it is placed with its gunwale at the surface. Its surface does not move as the bilge fills: the chamber loses V_b and the displaced volume gains V_b.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| V1 | Given a sound boat on the water, when Run advances 20 s, then its keel's depth below the surface, averaged over the last 2 s (more than three bobbing periods), is 0.087 m within 0.01 m | `BoatTests.TheHullFloatsAtItsDraught` | V6 | — |
| V2 | Given a Basketball set in a sound boat at rest, when Run advances 20 s, then the ball is still in the hull, the draught averaged over the last 2 s is 0.174 m within 0.02 m and the heel is under 15° | `BoatTests.TheHullCarriesItsCargo` | V3 | — |
| V3 | Given a Bowling ball set in a sound boat at rest, when Run advances 10 s, then the gunwale is below the surface and the boat rests on the chamber floor | `BoatTests.AnOverloadSinksIt` | V2 | — |
| V4 | Given a leaky boat with no cargo, when Run advances 71 s (three times 23.6 s; the level declares a Run length of 75 s, `ENGINE-RUN-LENGTH`), then the bilge holds 0.120 m³ within 0.015 m³, the draught is 0.254 m within 0.02 m, and the gunwale is above the surface | `BoatTests.ALeakLowersTheBoat` | the sound boat's bilge stays empty: every committed leak flow is under 0.0005 m³/s | — |
| V5 | Given a leaky boat with a 2.0 kg Weight (24 kg/m³) set centred on its floor, when Run advances until the gunwale is under (stall guard 10 s, hand 2.9 s), then the bilge holds 0.0645 m³ within 0.015 m³ at that tick and by 15 s the boat rests on the chamber floor | `BoatTests.ALeakWithCargoSinksIt` | a sound boat with the same Weight, its draught averaged over the last 2 s of 15 s, floats at 0.260 m within 0.02 m and its gunwale stays above the surface | a leaky boat with no cargo resting on the bench outside every store, its bilge filled by a tap stream (EL-003 at `opening` 0.125, 0.0103 m³/s, for 2 s, about 0.021 m³, 0.036 m deep) landing inside the bilge: once the tap closes, the bilge volume stays constant within 0.004 m³ for 5 s and no leak flow is committed, because the immersed port lies in no interior and is sealed (EL-001 L5); a free end would empty it to the floor datum in 2 × (√0.061 − √0.025) × 0.59 ÷ 0.0534 ≈ 1.9 s (head measured from the port, 0.025 m below the floor datum) |
| V6 | Given an empty chamber, then the boat rests on the floor and every committed leak flow is under 0.0005 m³/s | `BoatTests.NoWaterNoFlotation` | V1 | — |
| V7 | Given a powered Fan 1.5 m from the sail in a Catch basin at 0.375 m³, the boat against the upwind wall, when Run advances 10 s, then the boat and the Tennis ball in it have moved at least 0.20 m downwind | `BoatTests.AFanMovesTheBoatAndItsCargo` | Fan unpowered: under 0.02 m | — |
| V8 | Given V4, then the chamber, bilge and accounts satisfy the ledger identity at every sampled tick | `BoatTests.LeakedLiquidIsConserved` | — | — |
| V9 | Given a run, then Reset restores the pose and an empty bilge; save and load keep `hull` and the pose; an unknown `hull` option is refused | `BoatTests.ResetAndSaveLoadKeepTheHull` | — | both options |

Generic cover: W22, W23. No wire port. Chrome recipe (pending): fill a chamber, float the boat, choose Leaky, set a Weight in it, point a Fan at the sail.

## Open questions

All resolved; none needs the owner.

1. Leak as a Choice of two, not a size: yes (D19), as the Leaky bucket's Choice.
2. Propulsion: a sail with a delivered airflow receiver. No tow port (D19).
3. Flooding over the gunwale: not modelled (A2); stated above.
4. Hull-volume parameter: none (D13).
5. Cargo in the bilge: immersed in the bilge, reacted on the hull (coordinator decision 21: nested interiors, hull-envelope shielding and buoyancy reaction adopted).

## Integration

- **Works with.** Fan, Weight, delivered balls; EL-004, EL-027, EL-013, EL-036 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 81–100, 126–140, 146–150. Needs the lesson-fixture story (S8). A level that uses V4's slow leak declares a Run length of at least 75 s.

## Delivery

- **Slice shape.** Single element, after EL-014 and the "Floating bodies" family.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and the immersed port's point query, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (the immersed-port locator); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID` with Batch F's text and the L-additions A1 to A5; then `ENGINE-THERMAL` (owner decisions 18 and 20); `ENGINE-PARAMETER-BOUND-FRAMES` (seat value per Choice option); `ENGINE-JOINT-KINDS` (the rest coordinate of the rig's closed Sluice gates, EL-021); `ENGINE-RUN-LENGTH` (V4); EL-004, EL-014, EL-016, EL-021, EL-027; S7.
- **Files.** Adds `elements/Boat/Boat.csproj`, `BoatElementDeclaration.cs`, `boat.tscn`, `icon.svg`, `CuriousContraptions.tests/BoatTests.cs`; edits `CuriousContraptions.slnx` and this spec. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`); lists S3 owns need an edit until S3 lands.
- **Review tier.** Data-only if the probe finds every missing and narrower row delivered.
- **Disqualifiers the slice's probe must re-check.** No displacement envelope (a hull of thin boxes would displace only its walls and sink); cargo inside the hull buoyed by the outside liquid (no shielding) or its bilge buoyancy not reacted on the hull; bilge contents applied at the body's centre; an airflow receiver that cannot sit on a box collider.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-339`; `sequence-task-582` co-anchored with `element-029`, not with a todo; no `s450` anchor. It will need two mode records (Sound, Leaky).
