# EL-025 · Tipping-bucket water clock — named-identity spec

**Current delivery (24 October 2026):** [EL-025 delivery slice](../invest/named-elements.md#element-025). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

**Repair (audit C-1, M-12).** The single counterweighted bucket could not tip (audit moment table: it leans to about 19° and weirs over its lip). The part is redesigned as the twin-chamber see-saw that coordinator decision 21 adopts, dimensioned below against its own moment table.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-025 · Tipping-bucket water clock · Water |
| Anchors | [element-025](../requirements.md#element-025), co-anchored `sequence-task-578`; source record [todo-338](../requirements.md#todo-338); integration [sequence-task-389](../requirements.md#sequence-task-389) ("count real tipping batches"); ledger [element-025](../invest/named-elements.md#element-025), owner S446 |
| Disposition | To deliver as a single element |
| Planned package | id `tipping_bucket`, directory `elements/TippingBucket/`, title "Tipping bucket" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID`, `ENGINE-THERMAL` and `ENGINE-SIGNAL-NODES` |
| Related | [EL-014](EL-014-water-carrying-bucket.md) (a store on a moving body), [EL-003](EL-003-tap.md) and [EL-005](EL-005-liquid-funnel.md) (the feed), [EL-004](EL-004-catch-basin.md) (receivers), Counter (delivered) |

## Declaration

Closest delivered shape: the Impact lever, a static stand carrying a hinged body with angular limits (`RevoluteHingeDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149`), with two of Batch F's open stores on the moving body.

- **Interfaces.** `ISceneGeometryElement`, `IHingeElement`, `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), and the liquid store declaration of `ENGINE-LIQUID` on the one fluid port record of `ENGINE-GAS-NETWORK` (W4, W6).
- **Frame.** Origin at the pivot. X runs left to right across the part, Y up, Z toward the viewer. Hinge angle a about +Z, positive counterclockwise seen from the front, so a positive angle raises the right (+X) chamber.
- **Body 0 (static stand), three boxes.** Base 0.36 × 0.06 × 0.74 at (0, −1.02, 0); two posts 0.06 × 0.99 × 0.06 at (0, −0.495, ±0.34). The pivot is 1.05 m above the base's underside. Family vessel material (EL-001).
- **Body 1 (dynamic rocker), eleven boxes.** Two open chambers in a Λ: each chamber's inner bottom corner is the apex (0, 0.24, 0), and its floor runs outward and down at 35° to the body frame. Chamber frame: u along the floor outward, v normal to it, z depth. Right chamber R: u = (0.8192, −0.5736, 0), v = (0.5736, 0.8192, 0), every box turned −35° about Z; left chamber L is the mirror (x → −x, turned +35°). Per chamber, in chamber coordinates (centre; size u × v × z): floor (0.30, −0.015, 0; 0.66 × 0.03 × 0.56); outer wall (0.615, 0.085, 0; 0.03 × 0.23 × 0.56); inner wall (−0.015, 0.085, 0; 0.03 × 0.23 × 0.56); front and back walls (0.30, 0.10, ±0.265; 0.60 × 0.20 × 0.03). In body coordinates R's boxes are centred at (0.2372, 0.0556, 0), (0.5526, −0.0432, 0), (0.0365, 0.3182, 0) and (0.3031, 0.1498, ±0.265); L's at the same points with x negated. Keel 0.06 × 0.44 × 0.50 at (0, 0.01, 0), unturned, holding the ballast below the chambers. The two inner walls form a V notch above the apex. Family vessel material.
- **Mass and inertia of body 1.** 1.5 kg; centre of mass declared at (0, 0.06, 0), 0.06 m above the pivot (the keel's ballast puts it there); principal moments by the box rule on the 1.26 × 0.64 × 0.56 bound: 0.090, 0.237 and 0.250 kg·m² about X, Y and Z. Store contents add ρ·V at the centroid of each chamber's liquid region (L4) with no added inertia (A5). Gravity (0, −9.81, 0).
- **Hinge.** Revolute about Z at the origin, `BodiesCollide` false. Limits −35° to +35° (the stops). Initial angle −35°: right chamber down, left chamber raised; an initial angle on a limit is admitted (`CuriousContraptions.Bundle/BundleValidator.cs:802-807`, inclusive, both values declared by the same f32 conversion). Friction torque 0.02 N·m; damping 0.05 N·m·s/rad; stiffness 0.
- **Stores.** Local ids 1 (L) and 2 (R) on body 1, each: capacity 0.06 m³ (0.96 kg); interior 0.60 (along u) × 0.20 (v) × 0.50 (z), the open aperture 0.60 × 0.50 at v = 0.20 extruded down to the floor; plan area 0.30 m²; floor datum points at the inner and outer bottom corners, R at (0, 0.24, 0) and (0.4915, −0.1042, 0), L mirrored; overflow point at the outer rim, R (0.6308, 0.0424, 0), L mirrored. Tilt retention by L11 with w = 0.60 and H = 0.20, the tilt measured from the chamber's own floor. Both start empty. No displacement envelope is declared: if the part is ever immersed, only its walls displace (L12 a).
- **Thermal.** Stores 1 and 2 are also thermal nodes (owner decision 20; [TH-04](TH-04-electrical-heating-plate.md) section 11, T2): heat capacity ρ·V × 1046 J/(kg·K), starting temperature declared ambient 288 K unless a level authors another; inflow mixes by mass and outflow leaves at the store's temperature. No row of this part reads temperature.
- **Signal nodes (two rows of `ENGINE-SIGNAL-NODES`).** `side`: a threshold node on the committed hinge angle, on above +10°, off below −10°, its initial state taken from the authored pose (off at −35°). `tip`: an edge node that emits one Pulse on each change of `side`, rising or falling, and none at Run start.

| `PortId` | Role | Domain | Direction | Signal kind | Position |
| --- | --- | --- | --- | --- | --- |
| `ActivationOut` | `PortRole.ActivationOut` | `SimulationDomain.Signal` | Output | `PortSignalKind.Pulse` | (0, −0.50, 0.37), on the front post; bound to `tip` |

- **Parameters.** None. The tip volume is fixed by the geometry (D13).
- **Presentation.** `Visual/Rocker` follows body 1's pose. `Visual/Rocker/WaterL` and `WaterR` scale from the committed fill fraction of stores 1 and 2 (W12, missing).
- **Palette.** `ElementCategory.Control`; palette order 416 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour blue (69/255, 99/255, 156/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Stand` | Base and two posts, cream `#fff8e9` | solid; the three box colliders of body 0 | body 0 pose (static) |
| `Visual/Rocker` | Two open chambers in a Λ, blue `#45639c`; keel navy `#293954` | solid; the eleven box colliders of body 1 | body 1 pose |
| `Visual/Rocker/WaterL`, `WaterR` | Cyan boxes inside each chamber | decorative: enclosed, moved by a binding | presentation binding |
| `Visual/Rocker/Marks` | Gold `#f7cb52` line at 0.10 m depth on each chamber's front wall | decorative: printed flat | body 1 pose |
| `Visual/Rocker/Hub` | Navy disc printed on the keel's front face at the pivot | decorative: printed flat | body 1 pose |
| `Loc_Port_ActivationOut` | Locator | none | nothing |

Icon: a Λ-shaped double trough on a two-post stand, one side lowered. It differs from the Water bucket (one pail hung on a bail) by its two chambers and its stand.

### Behaviour and numbers

All of this is rigid-body dynamics with the contents mass of A5 and the tilt retention of L11. With the rocker on a stop, the raised chamber's floor is level (35° − 35°) and the lowered chamber's floor is tilted 70°.

- **Feed.** A tap at `opening` 0.125 on a full Water tank passes 0.0103 m³/s (series K = 0.08349 / √(2 + 8²) = 0.01028, EL-003's three apertures, head 1.0 m; [EL-003](EL-003-tap.md)) into a Water funnel ([EL-005](EL-005-liquid-funnel.md)) whose free mouth stands on the pivot line 0.55 m above the pivot. The funnel's stream falls vertically with a contracted radius of √0.6 × 0.10 = 0.0775 m.
- **Where the stream lands.** At a = −35° the raised chamber's inner wall is vertical at x = 0.24 × sin 35° = 0.138 m, so the stream (|x| ≤ 0.0775) lands inside L's aperture, 0.06 m clear of the wall; by symmetry it lands in R at a = +35°. Its samples end inside the raised chamber's interior and are credited to it; mid-swing they meet the V notch and are spilled (L8).
- **Hold.** Empty, the rocker's weight holds it on its stop with 1.5 × 9.81 × 0.06 × sin 35° = **0.506 N·m**.
- **First tip.** The raised chamber's liquid centroid is at x = −(0.30 − 0.138) = −0.162 m whatever its depth, so each cubic metre adds 16 × 9.81 × 0.162 = 25.47 N·m. It tips when 25.47·V > 0.506 + 0.02 (hinge friction): **V = 0.0207 m³** (0.33 kg, depth 0.069 m).
- **It keeps falling.** Net moment toward the fall, hinge friction not included, for 0.0209 m³ in the falling chamber (computed with L4's liquid region at each angle; the chamber pours what exceeds C(θ) from its outer rim):

| Hinge angle a | −35° | −25° | −15° | −5° | +5° | +15° | +25° | +35° |
|---|---|---|---|---|---|---|---|---|
| Net moment (N·m) | +0.03 | +0.53 | +0.99 | +1.10 | +0.94 | +0.84 | +0.77 | +0.73 |
| Retained in the falling chamber (m³) | 0.0209 | 0.0209 | 0.0209 | 0.0173 | 0.0119 | 0.0084 | 0.0058 | 0.0036 |

  The moment grows as the rocker leaves the stop (its own weight passes over the pivot and the liquid's arm lengthens), stays above the 0.02 N·m friction through the whole swing, and holds the rocker on the opposite stop with 0.73 N·m. The tip runs away; a sub-threshold volume leaves the rocker on its stop.

  **Steady tips** (every tip after the first) start with 0.0296 m³ in the falling chamber while the rising chamber still holds its 0.0036 m³ residual, which opposes the fall from near its outer rim. At −25°: the falling chamber, tilted 10° outward, holds a trapezoid (depths 0.046 and 0.152 m, centroid 0.354 m along its floor) at x = −0.256 m, giving 0.0296 × 156.96 × 0.256 = 1.19 N·m; the rocker's own weight resists 1.5 × 9.81 × 0.06 × sin 25° = 0.373 N·m; the residual, a wedge in a chamber tilted 60° with its centroid at x = 0.432 m, resists 0.0036 × 156.96 × 0.432 = 0.244 N·m; net **+0.57 N·m**. At +25°: the falling chamber has poured down to 0.0058 m³ as in the first-tip table (+0.773 N·m), and the residual, now a 10° wedge centred at x = 0.399 m, resists 0.0036 × 156.96 × 0.399 = 0.225 N·m; net **+0.55 N·m**. On the far stop: 0.506 + 0.227 − 0.0036 × 156.96 × 0.162 = **+0.64 N·m**. The steady tip runs away too.
- **Discharge.** On the lowered stop the chamber is tilted 70°; tan 70° = 2.75 > H/w = 0.333, so L11's second branch gives C(70°) = 0.06 × 0.20 / (2 × 0.60 × 2.75) = **0.0036 m³**. The rest has left as pending overflow from the outer rim (L9, L11). It leaves as an L8 stream with no discharge speed of its own: its start velocity is the rocker's velocity at the rim (EL-001 L9), so the pour carries the chamber's motion. Most of the pour leaves during the swing, while the outer rim, 0.632 m from the pivot, moves at up to about 1.75 m/s (ω about 1.7 rad/s at −5°, 2.6 rad/s at +30° and 2.8 rad/s just before the +35° stop, from the moment table's work, 0.36 J, 0.90 J and about 1.0 J, into about 0.26 kg·m²; ω = √(2W/I)). The rim velocity is ω × (−y, x). Each pour falls to the basin rims' plane, 0.50 m below the pivot, in the time t that solves 4.905·t² + |v_y|·t = y_rim + 0.50:
  - at a = −5° the rim at (−0.625, +0.097) moves at (−0.16, −1.04) m/s; t = 0.259 s, so the pour crosses at x = −0.625 − 0.16 × 0.259, |x| = **0.67 m**;
  - at a = +30° the rim at (−0.568, −0.279) moves at (+0.73, −1.48) m/s, toward the pivot; t = 0.109 s, |x| = 0.568 − 0.73 × 0.109 = **0.49 m**;
  - just before the +35° stop the rim at (−0.541, −0.327) moves at (+0.92, −1.51) m/s; t = 0.089 s, |x| = 0.541 − 0.92 × 0.089 = **0.46 m**, the innermost case;
  - on the stop the rocker is at rest and the rest falls from |x| = **0.54 m**.

  Every pour lands between |x| = 0.46 and 0.67 m, inside the basin's aperture (|x| from 0.325 to 1.575).
- **Return.** The other chamber is now raised and level under the stream. It must also beat the lowered chamber's residual, whose centroid is 0.397 m out: 16 × 9.81 × 0.0036 × 0.397 = 0.227 N·m. Return tip: 25.47·V > 0.506 + 0.227 + 0.02, **V = 0.0296 m³**, about half the 0.06 m³ capacity, so the chamber never overflows before it tips. From then on each chamber starts its fill holding its own 0.0036 m³ residual and receives **0.026 m³ per tip**.
- **Tip event.** `side` changes state only when the rocker crosses ±10°, so one fall from stop to stop is exactly one change and one Pulse; a rebound off a stop would have to travel 45° against at least 0.73 N·m to count twice. A rocker that never leaves its stop emits nothing.
- **Period.** At 0.0103 m³/s the first Pulse comes after 0.0207 / 0.0103 = **2.0 s**, the second about 0.0296 / 0.0103 = 2.9 s later, then one every 0.026 / Q. Over 20 s the tank falls from 1.0 m to (1 − 20 × 0.0103 / 2)² = 0.80 m and the flow to 0.0092 m³/s, which gives about 1 + (20 − 2.0) / 2.7 ≈ 7 tips. The period lengthens as the tank's head falls: that is the head law, not a clock setting.
- **Clearances in the reference rig.** Two Catch basins (outer 1.49 × 0.55 × 1.24, rim 0.55 m above the bench; [EL-004](EL-004-catch-basin.md)) centred at x = ±0.95 m; their apertures span |x| = 0.325 to 1.575, so each pour, landing at |x| = 0.46 to 0.67 m (Discharge), is at least 0.135 m inside one. With the pivot 1.05 m above the bench the basin rims are 0.50 m below it; the rocker's lowest point on a stop is 0.406 m below it, 0.094 m clear. The stand's 0.36 m base fits between the basins' inner walls (|x| ≥ 0.205).
- **Tick order.** The hinge angle commits with the tick; `side` and `tip` are evaluated with the other sensors after the last substep and commit with the tick (L16); a Counter on `ActivationOut` counts the Pulse in the next tick's routing.
- **Streams exert no force (L8).** The rocker tips by weight alone.

## Engine capabilities

Map families (navigation only, D14): ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static stand; W21 dynamic body with declared mass, centre and inertia | present | bodies |
| W19 revolute hinge with angular limits; initial angle on a limit (`BundleValidator.cs:802-807`) | present | pivot and stops |
| G3 hinge friction torque | present | pivot |
| W22 authored state, wire refusal | present | pose, `ActivationOut` |
| W4 fluid port contract | present, untested | stores |
| G2 signal node on a body's turn | narrower | delivered as a one-shot orientation node (`SignalOrientationDescriptor`); a re-arming threshold on a committed angle and an edge node that pulses on both changes are rows of `ENGINE-SIGNAL-NODES` |
| W11 contents add to the carrying body's mass | narrower | `CenteredInventory` adds gas at the body's centre only; contents at the liquid centroid (A5) is a new mass-source kind of `ENGINE-LIQUID` |
| W6 stores; W8 stream credit into an interior that follows a body; W9 pending overflow and tilt retention | missing | `ENGINE-LIQUID` |
| W12 fill-fraction driver | missing | `ENGINE-LIQUID` |
| Liquid store as thermal node | missing | `ENGINE-THERMAL` |

Counts: 4 present, 1 untested, 2 narrower, 3 missing. Three owners, one per capability: liquid (`ENGINE-LIQUID`), the signal rows (`ENGINE-SIGNAL-NODES`), the stores' thermal nodes (`ENGINE-THERMAL`). Stream samples into a moving interior take their candidates from `ENGINE-SPATIAL-QUERIES`.

### Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, fourteen colliders, one hinge, two stores with apertures and overflow points, two signal nodes, one port, two bindings. No code names the part.
2. **Generic laws.** Tipping is torque from the contents mass at its centroid (A5) against the rocker's declared centre of mass and the hinge stops. Discharge is tilt retention (L11) and pending overflow (L9). The event is a threshold and an edge on a committed angle. No row says "when full, tip".
3. **Three layers.** *Colliders:* three static boxes and eleven on the rocker. *Visuals:* follow the two bodies. *Animation:* the two water boxes read the committed fills off the physics thread and write nothing back.
4. **One-way flow and typing.** Liquid → body mass → pose → sensor → signal, next tick. Port id, role, signal kind and store ids are typed; values are f32.
5. **Flat-state counts.** Bodies 2 (1 static, 1 dynamic); colliders 14 boxes; joints 1 revolute; drives 0; sensors 0; signal nodes 2; liquid nodes 2 stores; thermal nodes 2; open apertures 2; overflow points 2; sockets 0; ports 1; bindings 2; parameters 0.
6. **Performance fitness.** Eleven boxes on the rocker: two open vessels of five boxes each (EL-001's count for one) and the keel that carries the ballast. Each store costs constant work per substep. The rocker rests on a stop between tips; whether it may sleep there is `ENGINE-ISLANDS-SLEEP`'s, not assumed here.
7. **Tests are scenarios.** Rows C1 to C8.

## Sources and legacy

- Requirement: "Collected liquid shifts a pivoted bucket until it tips and discharges"; outcome "Subthreshold volume does not emit a tip event." No variants.
- Integration [sequence-task-389](../requirements.md#sequence-task-389) ("count real tipping batches"); refinement [S707](../invest/refinements.md#s707) ("Accumulate finite flow and reset exactly"). Component research [water](../../component-research.md#water): recipe Three Splashes.
- **Legacy.** None at `a6c914e` (searched for "tipping", "water clock", "bucket", "liquid"). **Files harvested:** none.

## Acceptance outline

Authority: [element-025](../requirements.md#element-025). Tolerances L17. Planned class `TippingBucketTests`. **Reference rig:** the part on the bench with the rocker on its −35° stop; the feed of Behaviour (a full Water tank, a Tap at `opening` 0.125, a Water funnel whose free mouth is 0.55 m above the pivot on the pivot line); two Catch basins centred at x = ±0.95 m. Every row fits the default 30 s Run.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| C1 | Given the reference rig, when Run advances until the first Pulse and then 1 s more (stall guard 10 s; at the Pulse the angle has only just crossed +10° and the chamber still holds about 0.01 m³, and the swing to the stop and the pour end about 0.2 s later), then the hinge angle is above +30°, the left chamber holds at most 0.0036 + 0.004 m³ and the left basin has gained what it poured; when Run advances until the second Pulse and then 1 s more (stall guard 10 s more), then the angle is below −30° and the right chamber holds at most 0.0076 m³ | `TippingBucketTests.AFilledChamberTipsDischargesAndTheOtherReturnsIt` | C2 | — |
| C2 | Given the rig with the tank authored at 0.016 m³ (0.77 × the 0.0207 m³ tip volume; tank and funnel drain completely through mouths at their floors), the Tap at `opening` 1 (K = 0.08349 / √3 = 0.0482, EL-003's three apertures, so the tank empties in 2 × √0.016 / 0.0482 = 5.3 s, and the boundary's 0.025 m³ in 2 × √0.025 / 0.0482 = 6.6 s, tipping once 0.0207 m³ has left, at 2 × (√0.025 − √0.0043) / 0.0482 = 3.8 s) and the funnel under the Tap's free end, when Run advances 20 s, then no Pulse occurs, the angle stays below −30° and the left chamber holds 0.016 m³ within 0.004 m³ | `TippingBucketTests.ASubthresholdVolumeEmitsNoTipEvent` | C1 | the tank authored at 0.025 m³ (1.21 × the tip volume): exactly one Pulse |
| C3 | Given the reference rig with `ActivationOut` wired to a Counter, when Run advances 20 s, then the Counter's count equals the number of stop-to-stop falls of the committed angle, and is at least 5 (hand estimate 7) | `TippingBucketTests.EachTipIsCountedOnce` | the same rig with the Tap's `opening` 0: count 0 | — |
| C4 | Given C3, then the two basins' gain plus `Spilled` plus the chambers' contents equals the tank's loss within 0.004 m³, and the ledger identity holds at every sampled tick | `TippingBucketTests.TheDumpsAreCaughtAndConserved` | — | — |
| C5 | Given the rig with a static Wall (0.4 × 1.21 × 0.4) centred at x = −0.46 m whose top is 0.01 m under the raised chamber's outer floor (that floor's underside is 1.217 m above the bench at a = −35°), and no left basin, when Run advances 15 s, then no Pulse occurs and the raised chamber holds its 0.06 m³ capacity within 0.004 m³ and overflows from its outer rim | `TippingBucketTests.ABlockedRockerDoesNotReportATip` | C1 | — |
| C6 | Given a run with two tips, when Reset is pressed, then both chambers are empty, the angle is −35°, `side` is off with no Pulse, and a second Run repeats C1 | `TippingBucketTests.ResetRestoresTheEmptyRocker` | — | — |
| C7 | Given the part saved and loaded, then its pose and its `ActivationOut` link are equal | `TippingBucketTests.SaveLoadKeepsPoseAndLink` | — | — |
| C8 | Given an electrical wire offered to `ActivationOut`, then it is refused | covered by W22 | — | — |

C2's margins (−23 % and +21 % of the tip volume) are wider than the 0.02 N·m friction's 4 % effect on it, so neither side depends on solver detail. Generic cover: W22 (authored state and wrong-domain refusal). The part has no parameter. Chrome recipe (pending): place a tank, a tap, a Water funnel over the pivot, the tipping bucket, a basin each side, and wire `ActivationOut` to a Counter.

## Open questions

All resolved; none needs the owner.

1. `tip_volume` parameter: none (D13). The tip volume follows from the declared geometry and mass; the gold line marks the return-tip depth.
2. Chime strike: not built in (D19). The Pulse output serves the Three Splashes lesson; a Bell under a lowered chamber can be struck as an ordinary contact.
3. Mechanism: the twin-chamber see-saw (coordinator decision 21). One counterweighted bucket cannot tip under L4 and A5 (audit C-1).
4. Stream force: none (L8).
5. Which way the first tip goes: the authored initial angle is −35°, so the left chamber fills first. A player who wants the other way mirrors the part.

## Integration

- **Works with.** Counter, Bell, Wall (delivered); EL-003, EL-005, EL-004, EL-035 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element, after EL-014.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples into a moving interior, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared store and node instances); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID` with Batch F's text and the L-additions (A5 in particular); then `ENGINE-THERMAL` (owner decisions 18 and 20); `ENGINE-SIGNAL-NODES` (threshold and edge rows); EL-001, EL-003, EL-004, EL-005, EL-014; S7.
- **Files.** Adds `elements/TippingBucket/TippingBucket.csproj`, `TippingBucketElementDeclaration.cs`, `tipping_bucket.tscn`, `icon.svg`, `CuriousContraptions.tests/TippingBucketTests.cs`; edits `CuriousContraptions.slnx` and this spec. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`); lists S3 owns need an edit until S3 lands.
- **Review tier.** Data-only if the probe finds every missing and narrower row delivered.
- **Disqualifiers the slice's probe must re-check.** Contents mass applied anywhere but the liquid centroid (with contents at the body's centre the raised chamber's moment is that of the rocker's own centre and the part never tips); no re-arming threshold or no edge on both changes; an aperture or interior that does not follow its body; a moment table that, rebuilt from the delivered L4 relation, falls below the hinge friction anywhere in the swing.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-338`; `sequence-task-578` co-anchored with `element-025`, not with a todo; no `s446` anchor.
