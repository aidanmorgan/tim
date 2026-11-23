# EL-023 · Archimedes screw — named-identity spec

**Current delivery (24 October 2026):** [EL-023 delivery slice](../invest/named-elements.md#element-023). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

**Repair (audit C-4, H-1, H-7).** The rotor disc covered the outlet and the post went into the bench. The outlet now sits at the tip of a spout ledge below the upper end, clear of tube and rotor; the post stands on the bench; the edge is open when the screw is not driven, with no one-way seat (coordinator decision 21); the worked placement and every number below are recomputed.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-023 · Archimedes screw · Water |
| Anchors | [element-023](../requirements.md#element-023), co-anchored `sequence-task-576`; source record [todo-338](../requirements.md#todo-338); integration [sequence-task-389](../requirements.md#sequence-task-389); ledger [element-023](../invest/named-elements.md#element-023), owner S444 |
| Disposition | To deliver as a single element. Not a duplicate of the Water pump ([EL-022](EL-022-water-pump.md)): it has no sockets to join, draws from any open store through an immersed intake, pours from a declared outlet, takes a shaft only, and lifts in one sense of rotation only |
| Planned package | id `water_screw`, directory `elements/WaterScrew/`, title "Water screw" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` |
| Related | [EL-004](EL-004-catch-basin.md) (the stores it draws from and pours into), [EL-022](EL-022-water-pump.md) (the same work-target law), Motor (delivered), [EL-240](EL-240-waterwheel.md) (alternative drive) |

## Declaration

Closest delivered shape: the Fan in Shaft mode, a static housing with a hinged rotor, a `DriveIn` rim and a signed coordinate input (`CoordinateAirflowInput(…, Sense)`, `CuriousContraptions.Abstractions/AirflowValues.cs:38`, declared by the Fan with sense −1 at `elements/Fan/FanElementDeclaration.cs:55`), and Batch F's pump.

- **Interfaces.** `ISceneGeometryElement`, `IHingeElement`, and the liquid fitting and work-target declaration of `ENGINE-LIQUID` on the one fluid port record of `ENGINE-GAS-NETWORK` (W4, W7, W15).
- **Body 0 (static housing).** Origin at the tube centre. The tube axis is u = (0.866, 0.5, 0): it rises 30° toward +X.
- **Colliders on body 0 (three).** Tube: inner radius 0.15, outer radius 0.19, length 2.0, axis u (a Tube turned off the body axes is delivered: `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:193-195`, `elements/PipeBend45/pipe_bend_45.tscn:119-121`). Post: box 0.12 × 0.89 × 0.40 at (0.60, −0.385, 0), its foot 0.83 m below the origin and its top 0.0325 m under the tube. Spout ledge: box 0.44 × 0.04 × 0.20 at (0.88, 0.04, 0), from the post's right face to x = 1.10. Family vessel material (EL-001).
- **Body 1 (dynamic rotor).** A pulley disc: solid Cylinder radius 0.15, length 0.06, axis u, on the upper end face at (0.892, 0.515, 0). 0.5 kg; solid-disc inertia ½·m·r² = 0.0056 kg·m² about its axis. One revolute hinge about u (its base frame turned 30°, `HingeFrame`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:118-120`), no limits, friction torque 0.05 N·m.
- **Liquid node.** One fitting with one work target on the edge intake → outlet (L15), fixed values: maximum added head 1.25 m; rated input 20 W; efficiency 0.5; input is the rotor's revolute coordinate in a declared positive sense. The energy taken is the hydraulic work ÷ 0.5: resisting torque = (ρ·g·Q·h_p ÷ 0.5) ÷ |ω|, never above min(20 W / |ω|, 4 N·m) (L15). Undriven, or driven the other way, the work target adds no head and the edge is open, as any ungranted work target is (coordinator decision 16). No one-way seat is declared (coordinator decision 21).
- **Intake.** One immersed port, Intake role, aperture area A₀, at the lowest interior point of the lower end: (−0.791, −0.630, 0). An immersed mouth is a fluid port of the one record with no socket; its node is resolved by the innermost interior that contains its point (A1), the same query as a depth probe.
- **Outlet.** A declared free end that is not a mouth (L8), aperture area A₀, at the spout ledge's tip, (1.10, 0.17, 0), normal +X. Its contracted stream (radius √0.6 × 0.10 = 0.0775 m) starts 0.0325 m above the ledge, 0.0875 m below the tube's lowest outer point (0.961, 0.335, 0) and further still from the rotor's lowest point (0.941, 0.370, 0) (the rim's lowest point (0.967, 0.385) less half the disc's 0.06 m thickness along u). The outlet is 0.80 m above the intake.

| `PortId` | Role | Domain | Direction | Position |
| --- | --- | --- | --- | --- |
| `DriveIn` | `PortRole.DriveIn` | `SimulationDomain.Mechanical` | Input | rim of the rotor, radius 0.15 (`MechanicalRimDescriptor`) |

- **Fluid ports.** 0 sockets, 1 immersed port, 1 outlet that is not joinable (counts and locators are `ENGINE-CAPABILITY-INSTANCES`').
- **Parameters.** None. Size and incline are fixed (D13); the part's pose sets where it reaches.
- **Presentation.** `Visual/Rotor` follows body 1's pose; the helix is a child of it. `Visual/Water` and `Visual/Spout/Water` scale from the committed edge flow (W12, missing).
- **Palette.** `ElementCategory.Power`; palette order 327 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cyan (102/255, 184/255, 201/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tube` | Clear tube, outer radius 0.19, length 2.0, cyan `#66b8c9` at alpha 0.16 with cream `#fff8e9` end bands printed flush | solid; the Tube collider (true hollow) | body 0 pose (static) |
| `Visual/Post` | Box 0.12 × 0.89 × 0.40, cream | solid; its box collider | body 0 pose |
| `Visual/Spout` | Ledge 0.44 × 0.04 × 0.20, cream with a navy `#293954` lip line printed flat | solid; its box collider | body 0 pose |
| `Visual/Rotor` | Disc radius 0.15, thickness 0.06, gold `#f7cb52` | solid; the Cylinder collider | body 1 pose |
| `Visual/Rotor/Helix` | Helix radius 0.14, length 1.9, navy, inside the tube | decorative: enclosed by the Tube collider | body 1 pose |
| `Visual/Water` | Cyan band along the tube floor | decorative: enclosed, moved by a binding | presentation binding |
| `Visual/Spout/Water` | Cyan sheet from the tube's end band to the outlet | decorative: drawn liquid, not a body, moved by a binding | presentation binding |
| `Loc_Port_DriveIn` | Locator | none | body 1 pose |

Icon: a slanted clear tube with a helix, a pulley at the top and a ledge under it. It differs from the Water pump (a box with two port rings) by its slant and helix, not by colour.

### Behaviour and numbers

- **Law (L15 with L7 and A9).** Two apertures in series, intake and outlet, each K₀: K = K₀/√2 = 0.05904. A granted work target adds head h_p ≤ 1.25 m with ρ·g·Q·h_p ≤ 0.5 × absorbed shaft power; then Q = K·√(h_p − L), L being the lift from the supply surface to the outlet. The crest rule counts the granted head (A9), so liquid passes the outlet datum though it is above the supply surface.
- **Worked placement.** The supply is a full Catch basin on the bench (interior floor 0.15 m, rim 0.55 m above the bench) whose right outer face is at x = 0.20 from the part's origin; the intake point stands 0.05 m above the basin floor, 0.20 m above the bench, which puts the origin 0.83 m up and the post's foot on the bench. The tube's underside crosses the basin's right wall 0.107 m above its rim (underside y = 0.5774·x − 0.2193 relative to the origin). The receiver is a second Catch basin on the bench, its near outer face at x = 0.70 (aperture from 0.82 to 2.07), clear of the post by 0.04 m and under the spout. The outlet is 1.00 m above the bench, so L = 1.00 − (0.15 + d) = 0.85 − d at basin depth d.
- **Rate.** At a rotor speed of 5 rad/s or more the shaft can give the rated 20 W, so the hydraulic power is at most 10 W: Q·h_p ≤ 10 / 156.96 = 0.0637. At d = 0.40 (L = 0.45) the head cap would give 0.05904 × √0.80 = 0.0528 m³/s, 10.4 W, so power binds: h_p√(h_p − 0.45) = 0.0637 / 0.05904 gives h_p = 1.225 m and **Q = 0.0520 m³/s**; the shaft gives 20 W (4.0 N·m at 5 rad/s). Below d = 0.345 m the head cap binds: at d = 0.20, Q = 0.05904 × √0.60 = 0.0457 m³/s, hydraulic 8.97 W, shaft 17.9 W (3.59 N·m at 5 rad/s).
- **Lift.** Depth 0.40 → 0.05 m moves 1.25 × 0.35 = 0.4375 m³ in **9.5 s** (integrated over the two regimes) and takes 85.7 J of hydraulic work, so **171 J** of shaft work (the head-cap regime alone would be 156.96 × 1.25 × 0.4375 / 0.5 = 172 J). At d = 0.05 m the surface reaches the intake point and the intake is dry; flow ends at 0.0396 m³/s.
- **Slow shaft.** At 2 rad/s the cap is 4 N·m × 2 = 8 W, so Q·h_p ≤ 4 / 156.96 = 0.0255: at L = 0.45, h_p = 0.767 m and Q = **0.0332 m³/s**.
- **Undriven or reversed.** A negative coordinate rate is not input: no head is added and the edge is open. The outlet datum is 0.45 m or more above the supply surface, so nothing passes (L7 crest rule). The rotor meets only its 0.05 N·m bearing friction. Tilted so its outlet were below the supply surface, the part would conduct downhill undriven, as a pipe would and as the unpowered pump does; but the straight tube must cross the supply's rim, which stands at or above the surface, and the outlet lies at most √(0.139² + 0.165²) = 0.22 m below the tube's lowest upper-end point (0.961, 0.335), so no practical placement puts the outlet below an open supply's surface. No row relies on that pose; the open ungranted edge is the general L15 law that [EL-022](EL-022-water-pump.md) row M2 proves.
- **Stall.** An outlet 1.25 m or more above the supply surface gives Q = 0 and no shaft load beyond friction (reachable only by pitching the part, A6: upright the outlet is 0.80 m above the intake, which must be under the surface).
- **Pour.** The stream leaves at Q/(0.6·A₀) = 0.0520 / 0.01885 = 2.76 m/s along +X at first and 0.0396 / 0.01885 = 2.10 m/s at the end of the lift (L8). Falling 0.45 m to the receiver's rim takes 0.303 s, so it crosses the rim plane between x = 1.10 + 2.76 × 0.303 = 1.94 and 1.10 (a trickle), all inside the receiver's aperture (0.82 to 2.07). Every sample path ends inside the receiver's interior and is credited to it (L8).
- **Attainable drive.** A delivered Motor (default 6 rad/s, 20 N·m, `elements/Motor/MotorElementDeclaration.cs:41`–`:44`) exceeds the 4 N·m cap; 20 W is 0.17 J per tick against the default Battery's 120 W and 1 J per tick.
- **Why these values.** A 0.80 m lift carries a basin into a second basin beside it on the same bench; the 1.25 m head cap binds only when the part is pitched steeper than its 30° (A6); upright, the intake leg bounds the lift.

## Engine capabilities

Map families (navigation only, D14): ElectricalPower, EnvironmentState, FiniteLedger, FiniteWorkActuation, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, ShaftTorque, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Tube collider (off the body axes as delivered); W21 rotor body | present | bodies |
| W19 revolute hinge | present | rotor |
| W24 shaft input on a `DriveIn` rim; signed coordinate sense as `CoordinateAirflowInput` | present | drive |
| W22 authored state, wire refusal | present | pose, `DriveIn` |
| W4 fluid port contract | present, untested | fitting, immersed port, outlet |
| A hinge whose base frame is turned 30° off the body axes | present, untested | `HingeFrame` carries a rotation (`MechanicalContractValues.cs:118-120`); no delivered test turns a hinge off a body axis. A8 is its first proof |
| W3 sockets and locators | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK`. This part: 0 sockets, 1 immersed port, 1 outlet | intake, outlet |
| W7 fitting, flow and the crest rule with granted head (A9); W8 free end and stream | missing | `ENGINE-LIQUID` |
| W15 work target on a liquid edge from shaft work, in a declared sense; open when ungranted | missing | `ENGINE-LIQUID` |
| Immersed port (L5) | missing | `ENGINE-LIQUID` |
| W12 driver for a committed edge flow | missing | `ENGINE-LIQUID` |

Counts: 4 present, 2 untested, 1 narrower, 4 missing. Owners: `ENGINE-LIQUID`, with W3's socket form `ENGINE-CAPABILITY-INSTANCES` and W4's record `ENGINE-GAS-NETWORK`. The fitting holds no volume, so the part has no store and no thermal node.

### Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, four colliders, one hinge, one fitting with one work target, one immersed port, one outlet, one port, two bindings. No screw routine exists or is asked for.
2. **Generic laws.** Lifting is the work-target law L15 on the aperture law L7 with the crest rule of A9. "Wrong rotation delivers nothing" is the declared input sense with the crest rule: an ungranted target adds no head. "Dry intake delivers nothing" is the wet rule of L5 applied to an immersed port.
3. **Three layers.** *Colliders:* Tube, post box, spout box, rotor Cylinder. *Visuals:* the table above; the helix is enclosed art on the rotor body. *Animation:* the water band and spout sheet read committed flow and write nothing back.
4. **One-way flow and typing.** Shaft work → liquid edge in the next substep (L16). `DriveIn` and its role are the delivered enums.
5. **Flat-state counts.** Bodies 2 (1 static, 1 dynamic); colliders 4 (1 Tube, 2 boxes, 1 Cylinder); joints 1 revolute; drives 0; sensors 0; liquid nodes 1 fitting; work targets 1; stores 0; thermal nodes 0; sockets 0; immersed ports 1; outlets 1; wire ports 1; bindings 2; parameters 0.
6. **Performance fitness.** Four colliders against the pump's two: the post carries the raised end and the ledge is the visible lip under the stream. One edge row; a fixed number of sample paths for the outlet; the immersed port resolves by one point query, with candidates from `ENGINE-SPATIAL-QUERIES` (L18).
7. **Tests are scenarios.** Rows A1 to A8.

## Sources and legacy

- Requirement: "Rotating bounded screw lifts fluid through its real intake and discharge"; outcome "Wrong rotation or a dry intake cannot deliver the target volume." No variants.
- Integration [sequence-task-389](../requirements.md#sequence-task-389) ("lift water with power"); refinement [S707](../invest/refinements.md#s707) ("Convert finite supplied work to head"). Component research [water](../../component-research.md#water): "a dry intake is visibly idle"; recipe Up and Over.
- **Legacy.** None at `a6c914e` (searched `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/` for "archimedes", "screw", "water", "liquid"). **Files harvested:** none.

## Acceptance outline

Authority: [element-023](../requirements.md#element-023). Tolerances and stall guards L17. Planned class `WaterScrewTests`. Each row uses the worked placement (supply basin with `initial_volume` 0.5 m³, empty receiver basin) unless stated; every row fits the default 30 s Run.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| A1 | Given the rotor driven forward at 5 rad/s or more, when Run advances until the intake is dry (stall guard 28.5 s, three times 9.5 s), then the supply basin holds 0.0625 m³ within 0.02 m³, the receiver gained what the supply lost within 0.004 m³ and `Spilled` is under 0.004 m³ | `WaterScrewTests.ForwardRotationLiftsTheBasinIntoTheReceiver` | A2, A3 | — |
| A2 | Given the same construction with the rotor driven backward, when Run advances 15 s, then every committed flow is under 0.0005 m³/s and both basins are unchanged within 0.004 m³ | `WaterScrewTests.BackwardRotationDeliversNothing` | A1 | — |
| A3 | Given the supply basin authored at 0.05 m³ (depth 0.04 m, below the intake point), when the rotor turns forward for 15 s, then nothing moves | `WaterScrewTests.ADryIntakeDeliversNothing` | A1 | supply at 0.08 m³ (depth 0.064 m) delivers until depth 0.05 m and stops |
| A4 | Given the rotor not driven, when Run advances 15 s, then every committed flow is under 0.0005 m³/s and both basins are unchanged | `WaterScrewTests.AnUndrivenScrewAddsNoHead` | A1 | — (no practical pose puts the outlet below an open supply's surface; see Undriven or reversed. The open ungranted edge is proved by EL-022 row M2) |
| A5 | Given the committed flow sampled at supply depth 0.40 m and 0.20 m, then it is 0.0520 and 0.0457 m³/s within 10%; at a rotor speed of 2 rad/s and depth 0.40 m it is 0.0332 m³/s within 10% | `WaterScrewTests.FlowFollowsHeadCapAndShaftPower` | — | — |
| A6 | Given the part turned a further +20° about Z (tube at 50°), placed so its intake is 0.10 m below the surface of the full supply basin with the basin's inner +X wall face 0.23 m to the +X side of the intake point (outlet 1.30 m above that surface), and the rotor driven forward at 5 rad/s, when Run advances 10 s, then every committed flow is under 0.0005 m³/s. Hand: turning by φ moves the outlet relative to the intake (1.891 m along X and 0.800 m above it in the part frame) to Δy(φ) = 1.891·sin φ + 0.800·cos φ above it: 0.647 + 0.752 = 1.399 m at 20°, 0.534 + 0.767 = 1.301 m at 16.4°. Without the head cap the 10 W bound alone would give h_p·√(h_p − 1.30) = 1.079, h_p = 1.70 m and Q = 0.037 m³/s, so the row fails. Placement: the tube's underside crosses the basin's wall 0.11 m above its rim at 20° (0.08 m at 16.4°); the post and spout stand clear on the +X side of the basin; the part is static and needs no support (its post's foot is 0.72 m above the basin's base, 0.63 m at 16.4°); the tube's lowest point is 0.27 m above the basin floor | `WaterScrewTests.ALiftAboveTheHeadCapStalls` | A1 | turned +16.4° instead (tube at 46.4°, outlet 1.20 m above the surface): the committed flow 0.25 s after Run starts is 0.0132 m³/s within 10% (Q = 0.05904 × √(1.25 − 1.20) = 0.0132 m³/s, 2.6 W hydraulic, inside the 10 W bound; by 0.25 s the surface has fallen 0.0132 × 0.25 / 1.25 = 0.003 m, lowering Q 3 %) |
| A7 | Given A1, then every sampled tick satisfies the ledger identity (L10), the shaft work done is at least the hydraulic work ÷ 0.5 within 10%, Reset restores both basins, and save and load keep the pose and the drive connection | `WaterScrewTests.TheLedgerHoldsAndResetRestores` | — | — |
| A8 | Given the rotor turned a quarter turn about u, then the helix art has turned with it about u | `WaterScrewTests.TheHelixFollowsTheRotor` | — | — |

Generic cover: W22 covers authored state and the refusal of an electrical wire on `DriveIn`. The part has no parameter. Chrome recipe (pending, post-catalogue): place a basin, lower the screw's foot into it, connect a Motor to the pulley, place a second basin under the spout.

## Open questions

All resolved; none needs the owner.

1. `incline` and `length` parameters: removed (D13). The pose does the aiming.
2. Electrical mode: none (D19). A Motor drives the pulley; the pump is the electrical lifter.
3. Positive displacement (flow proportional to speed): not used. The one work-target law of L15 applies, so the family has one lifting rule.
4. Rotor shape: a pulley disc outside the tube; the helix is enclosed art, because a helix is not an admitted collider shape.
5. Back-flow: the open edge, no one-way seat (coordinator decision 21). Upright over an open vessel nothing can run back, because the outlet is above the supply; tilted so its outlet were below the supply it would conduct downhill like a pipe, but the tube's crossing of the supply's rim leaves no practical pose for that (Behaviour, Undriven or reversed).
6. Where the liquid leaves: a declared outlet at the tip of a spout ledge, clear of the rotor (coordinator decision 21).

## Integration

- **Works with.** Motor, Battery (delivered); EL-004, EL-001, EL-002, EL-007, EL-014 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150 ([campaign coverage](../requirements.md#campaign-element-coverage)). A level that places it needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples and the immersed port's point query, L18), `ENGINE-ISLANDS-SLEEP` (a change of level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (the immersed-port locator and the non-joinable outlet); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID` with Batch F's text and A1, A9; EL-004; S7 for the binding.
- **Files.** Adds `elements/WaterScrew/WaterScrew.csproj`, `WaterScrewElementDeclaration.cs`, `water_screw.tscn`, `icon.svg`, `CuriousContraptions.tests/WaterScrewTests.cs`; edits `CuriousContraptions.slnx` and this spec. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`); the lists S3 owns need an edit until S3 lands.
- **Review tier.** Data-only if the probe finds every missing and narrower row delivered.
- **Disqualifiers the slice's probe must re-check.** A hinge cannot be declared about an axis turned off the body axes; a work target cannot take a signed shaft input or does not open when ungranted; the crest rule does not count granted head; an immersed port is not available; a free end cannot be declared apart from a mouth; any stream sample from the outlet meets the tube, rotor or ledge.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The identity will be bound by the named-identity binding section in the form published for EL-239. It cannot be written yet: the ledger entry links only the umbrella `todo-338`; `sequence-task-576` is co-anchored with `element-023`, not with a todo; and `docs/planning/invest/scope-corrections.md` has no `s444` anchor.
