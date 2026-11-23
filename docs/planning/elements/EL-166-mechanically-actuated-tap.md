# EL-166 · Mechanically actuated tap — named-identity spec

**Current delivery (24 October 2026):** [EL-166 delivery slice](../invest/named-elements.md#element-166). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. The tap body, mouths and seat are in [EL-003](EL-003-tap.md). All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-166 · Mechanically actuated tap · Water |
| Anchors | [element-166](../requirements.md#element-166), co-anchored `sequence-task-719`; source record [campaign coverage](../requirements.md#campaign-element-coverage); ledger [element-166](../invest/named-elements.md#element-166), owner S459 |
| Disposition | Member of the Family slice "Actuated taps" with [EL-167](EL-167-solenoid-tap.md), after [EL-003](EL-003-tap.md). A variant of the Tap, not a duplicate: it adds a hinged lever body and a rope tie, and its seat follows the lever's committed angle (L14 b) |
| Planned package | id `lever_tap`, directory `elements/LeverTap/`, title "Lever tap" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` (Delivery) |
| Related | [EL-003](EL-003-tap.md), [EL-165](EL-165-manual-tap.md), [EL-167](EL-167-solenoid-tap.md), [EL-017](EL-017-mechanical-float-valve.md) (a seat on a float arm), rope parts and Weight (delivered) |

## Declaration

Closest delivered shape: the Impact lever, a static base carrying a hinged arm with rope ties, limits and pivot friction.

- **Interfaces.** `ISceneGeometryElement`, `IHingeElement`, the liquid fitting and seat declaration of `ENGINE-LIQUID` (W4, W7, W14).
- **Body 0 (static), two colliders.** The Tap's Tube (bore radius 0.10, outer radius 0.14, length 0.30, axis X) and bonnet box 0.12 × 0.10 × 0.12 at (0, 0.19, 0). Family vessel material (EL-001: restitution 0.12, friction 0.3, rolling resistance 0).
- **Body 1 (dynamic lever), one box.** 0.30 × 0.04 × 0.04, centred at (0.15, 0.26, 0) when closed: it lies along +X from a pivot at (0, 0.26, 0) on the bonnet top. 0.1 kg; inertia about its centre m·L²/12 = 0.00075 kg·m² about Y and Z, 0.00003 about X. Material restitution 0.1, friction 0.4, rolling resistance 0, bounce threshold 0.1 m/s. Gravity (0, −9.81, 0).
- **Hinge.** Revolute about Z at the pivot. Angle 0° is closed (lever level); positive raises the tip. **Limits 0° to 60°.** Friction torque 0.20 N·m. No spring, no drive.
- **Liquid node.** The Tap's fitting with one seat between `Start` and `End`: seat area s × A₀ with **s = clamp(angle / 60°, 0, 1)** from the committed hinge coordinate (L14 b).
- **Mouths (two sockets).** As the Tap: `Start` at (−0.15, 0, 0), `End` at (+0.15, 0, 0), pipe bore, Bidirectional, capture 0.20 m and 20° (L5).
- **No store, so no thermal node.** Owner decision 20 attaches a thermal node to every liquid store; the fitting holds none. No row reads temperature.

| `PortId` | Role | Domain | Direction | Position |
| --- | --- | --- | --- | --- |
| `Tie` | `PortRole.Tie` (`RopeAttachmentRole.Load`) | `SimulationDomain.Rope` | Bidirectional | lever tip, (0.30, 0.26, 0) on body 1 |

The lever's faces are also ordinary contact surfaces: a cam, a pusher or a falling ball moves it.

- **Parameters.** None. The lever starts closed; a tap that should start open is pulled open by its rope at Run start.
- **Presentation.** `Visual/Lever` follows body 1's pose.
- **Palette.** `ElementCategory.Control`; order 410 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour gold (247/255, 203/255, 82/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Body` | Tube 0.30 long, outer radius 0.14, cream `#fff8e9` | solid; the Tube collider | body 0 pose (static) |
| `Visual/Bonnet` | Box 0.12 × 0.10 × 0.12, cream | solid; its box collider | body 0 pose |
| `Visual/Lever` | Bar 0.30 × 0.04 × 0.04, gold `#f7cb52`, with a navy `#293954` eye printed at its tip | solid; the lever box collider | body 1 pose |
| `Visual/Bands` | Navy bands at both tube ends, flush | decorative: printed flat | nothing |
| Locators | two mouths, `Tie` | none | body pose |

Icon: the Tap with a long bar standing out from the bonnet and an eye at its tip. It differs from the Tap (a printed lever glyph) by the real bar, and from the Solenoid tap (a pin and two sockets) by the bar and eye.

### Behaviour and numbers

- **The lever stays where it is put.** Its own weight gives at most 0.1 × 9.81 × 0.15 = 0.147 N·m, under the 0.20 N·m pivot friction: it holds any angle with nothing acting on it.
- **Work moves it.** A rope from the tip up over a Pulley to a Weight of mass m pulls with m·g on a 0.30 m arm: net torque (2.943·m − 0.147) × cos θ. It starts to turn when that exceeds 0.20 N·m at θ = 0: m > (0.20 + 0.147) / 2.943 = **0.118 kg**. A slowly loaded lever would stall where cos θ = 0.20 / (2.943·m − 0.147); that angle is beyond 60° for every m above (0.20/0.5 + 0.147) / 2.943 = 0.186 kg. The lightest delivered Weight is 0.25 kg (`elements/Weight/WeightElementDeclaration.cs:33`), so **every delivered Weight drives the lever onto its 60° stop**, fully open, released or lowered slowly; a 0.118 kg pull cannot be built from delivered Weights.

| Weight | Start torque (2.943·m − 0.147) | Slow-load stall angle | Ends at | Opening s | Route K = K₀/√(2 + 1/s²) |
| --- | --- | --- | --- | --- | --- |
| 1.0 kg | 2.80 N·m | 85.9° (beyond the stop) | 60° stop | 1 | 0.04820 (a fully open Tap) |
| 0.25 kg (the minimum) | 0.589 N·m | 70.2° (beyond the stop) | 60° stop | 1 | 0.04820 |
| none (slack rope) | 0 | — | 0° | 0 | 0 |

- **A slack linkage changes nothing.** A rope with its Weight resting on a ledge applies no tension: the angle and the opening stay as they were.
- **A stalled linkage changes nothing.** With a Wall block over the lever tip the Weight hangs taut and the angle cannot rise: the seat stays shut. Against the 60° stop, more pull opens it no further.
- **A partial opening holds.** If the Weight's fall is stopped by a ledge 0.15 m below its start, the rope goes slack when the tip has risen 0.15 m (0.30·sin θ = 0.15, θ = 30°); the lever coasts on against friction and gravity and then holds wherever it stops, between 30° and 60°.
- **Closing.** A push on the tip shuts the lever when its torque beats the rope's pull and the friction. At the 60° stop the tip's horizontal arm is 0.30 × cos 60° = 0.15 m. Against friction alone, a Tennis ball (0.35 kg) on the tip gives 0.35 × 9.81 × 0.15 = **0.515 N·m**, plus the lever's own 0.1 × 9.81 × 0.075 = 0.074 N·m, against 0.20 N·m: it would ride the lever shut. But a rope that is still attached is taut again as soon as the lever turns back: a 1.0 kg Weight holds 1.0 × 9.81 × 0.15 = 1.47 N·m upward at the stop, and a Weight resting on a ledge must be lifted, so a Tennis ball cannot shut a roped lever. A Bowling ball (4 kg, radius 0.28 m, `elements/BowlingBall/BowlingBallElementDeclaration.cs:54`–`:56`) on the tip gives 4 × 9.81 × 0.15 = **5.89 N·m** against 1.47 + 0.20 − 0.074 = 1.60 N·m, so it shuts it; once the push is gone the taut Weight opens it again. Paying out the rope alone does not shut it: the friction holds it open.
- **Flow.** The Tap's law with s from the angle ([EL-003](EL-003-tap.md)): on a full Water tank at s = 1, 0.0482 m³/s.
- **Liquid exerts no force on the lever (L12 c).** Opening against a full tank costs the same as against an empty one.
- **Tick order.** The seat reads the angle committed by the previous substep (L16).
- **Rope.** Ropes are solid (owner decision, 22 November 2026); the delivered rope's missing collider is a defect owned by `ENGINE-CONNECTION-MATERIALS`, not by this part.

## Engine capabilities

Map families (navigation only, D14): EnvironmentState, FiniteLedger, FiniteWorkActuation, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body; W2 Tube; W21 lever body | present | bodies |
| W19 revolute hinge with angular limits | present | lever |
| G3 hinge friction holds a load below it and yields above it | present | lever |
| G4 rope tie on a hinged arm; W20 rope over a pulley | present | `Tie` |
| W22 authored state, wire refusal | present | pose, `Tie` |
| W4 fluid port contract | present, untested | fitting |
| W3 sockets and joins | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 2 sockets (`Start`, `End`), 0 immersed ports, 0 outlets |
| W7 fitting, seat, flow; W8 free end | missing | `ENGINE-LIQUID` |
| W14 seat opening from a committed revolute coordinate | missing | `ENGINE-LIQUID` |

Counts: 5 present, 1 untested, 1 narrower, 2 missing. No store, so no thermal-node row.

### Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, three colliders, one hinge, one fitting with one seat, two sockets, one port, one body binding.
2. **Generic laws.** The lever moves by rope tension and contact against declared pivot friction and limits. The seat is a clamped linear map of a committed coordinate (L14 b). "Slack or stalled cannot change the aperture" is that the coordinate did not change.
3. **Three layers.** *Colliders:* Tube, bonnet, lever. *Visuals:* the lever art follows its body. *Animation:* none.
4. **One-way flow and typing.** Mechanics → coordinate → seat, next substep; nothing flows back. Port id and role are the delivered enums.
5. **Flat-state counts.** Bodies 2 (1 static, 1 dynamic); colliders 3 (1 Tube, 2 boxes); joints 1 revolute; drives 0; sensors 0; signal nodes 0; liquid nodes 1 fitting; seats 1; thermal nodes 0 (no store); sockets 2; immersed ports 0; outlets 0; ports 1; bindings 1 body; parameters 0.
6. **Performance fitness.** Three colliders against the Tap's two; the third is the lever, which must push and be pushed. The lever is 0.04 m thick, four times the 1 cm wall of the tunnelling envelope. A free `End` adds one stream's fixed sample paths (L18).
7. **Tests are scenarios.** Rows L1 to L8.

## Sources and legacy

- Requirement: "Physical linkage work changes the flow aperture"; outcome "A slack or stalled linkage cannot change aperture." No variants.
- Campaign coverage row: "manual/mechanical/solenoid tap variants … with individually exercised construction objectives". Component research [water](../../component-research.md#water): "actuated variants need their lever or supply, never free water"; a cam can "open a tap once per revolution".
- **Legacy.** None at `a6c914e`. **Files harvested:** none.

## Acceptance outline

Authority: [element-166](../requirements.md#element-166). Tolerances L17; angles within 3°. Planned class `LeverTapTests`. The supply is a full Water tank joined to `Start`; `End` is free. The rope runs from `Tie` up over a Pulley to a Weight hanging clear of everything for at least 0.30 m of fall (the tip rises 0.30 × sin 60° = 0.26 m to the stop).

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| L1 | Given a 1.0 kg Weight on the rope, when Run advances 1 s, then the lever is on its 60° stop within 3° and the committed flow is 0.047 m³/s within 10% | `LeverTapTests.AFallingWeightOpensTheTap` | L2 | the lightest delivered Weight, 0.25 kg: also on the 60° stop within 3° |
| L2 | Given the same rope with its Weight resting on a ledge, when Run advances 10 s, then the angle stays under 1° and the tank keeps 1.0 m³ within 0.004 m³ | `LeverTapTests.ASlackLinkageChangesNothing` | L1 | — |
| L3 | Given L1 with a Wall block resting over the lever tip, when Run advances 10 s, then the angle stays under 3° and every committed flow is under 0.0005 m³/s (L17) | `LeverTapTests.AStalledLinkageChangesNothing` | L1 | — |
| L4 | Given a 1.0 kg Weight whose fall is stopped by a Wall ledge 0.15 m below its start, when Run advances 10 s after the Weight lands, then from 1 s after landing the angle stays within 1° of its value then (between 30° and 60°) and the committed seat opening (L13) stays within 1/60 of its value then | `LeverTapTests.TheLeverHoldsItsPosition` | L1 (taut rope: on the stop) | — |
| L5 | Given L1's rig and a Bowling ball released from rest at Run start with its centre 1.5 m above the tip's position at the stop, when it lands on the tip (about 0.50 s, after the lever reaches the stop at about 0.3 s), then the committed seat opening falls under 0.05 (angle under 3°) within 1 s of contact and every committed flow is under 0.0005 m³/s while it stays there | `LeverTapTests.APushShutsIt` | L1 (no ball: it stays on the stop) | a Tennis ball released the same way does not shut it (the taut Weight wins: 0.515 + 0.074 N·m against 1.47 + 0.20 N·m) |
| L6 | Given `Start` unjoined and the lever pulled onto its stop as L1, then nothing leaves `End` | `LeverTapTests.AnOpenTapWithNoSupplyEmitsNothing` | L1 | — |
| L7 | Given a run that opened it, then Reset restores a closed lever and a full tank; save and load keep the pose, the joins and the rope | `LeverTapTests.ResetAndSaveLoadRestore` | — | — |
| L8 | Given an electrical wire offered to `Tie`, then it is refused | covered by W22 | — | — |

Hand calculations. L1: the lever reaches the stop in about 0.3 s (2.6 N·m net on about 0.093 kg·m² of lever and Weight referred to the tip); by 1 s the tank has fallen to (1 − 1/41.5)² = 0.952 m, so the flow is 0.0482 × √0.952 = 0.047 m³/s. L4: the lever's own kinetic energy when the rope goes slack at 30° is about 0.06 J against at least 0.20 + 0.147 × cos 60° = 0.27 N·m of retarding torque, so it coasts at most about 0.2 rad (12°) and stops between 30° and 60°; 1° of angle is 1/60 of opening. L5: the tip at the stop is at (0.15, 0.52, 0); the ball's centre starts at (0.15, 2.02, 0) and meets the tip after falling 1.5 − 0.28 = 1.22 m, t = √(2 × 1.22 / 9.81) = 0.50 s; it meets the tip first because the bar runs down and inward from it; net closing torque 5.89 + 0.074 − 1.47 − 0.20 = 4.29 N·m at 60°, and both the ball's and the Weight's torques scale with cos θ as the lever falls.

Generic cover: W22. No parameter. Chrome recipe (pending): snap the lever tap to a tank, tie a rope from its eye over a pulley to a Weight, release the Weight.

## Open questions

All resolved; none needs the owner.

1. Return spring: none. A tap that closes itself would hide the "slack linkage" outcome; pivot friction holds the last position. Owner decision 6 concerns spring-returned blades and does not apply to an unpowered lever, which holds in every orientation because 0.147 N·m is under the 0.20 N·m friction.
2. Shaft input (`DriveIn`): none (D19). Rope and contact are the linkages the row and the research name.
3. Hydraulic load on the lever: none (L12 c).
4. Travel: 0° to 60°, so that any delivered Weight opens it fully and a push on the tip at the stop still has a 0.15 m arm.

## Integration

- **Works with.** Rope anchor, Pulley, Weight, balls, Wall (delivered); EL-001, EL-003, EL-004 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. First taught at **64**, with the Solenoid tap, after the float and level-switch lesson (63) (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)). Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Family slice "Actuated taps" (EL-166, EL-167), after EL-003.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20); then EL-001, EL-003; then S7.
- **Files.** Adds `elements/LeverTap/LeverTap.csproj`, `LeverTapElementDeclaration.cs`, `lever_tap.tscn`, `icon.svg`, `CuriousContraptions.tests/LeverTapTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** A seat that cannot read a revolute coordinate; pivot friction that does not hold a static load in every orientation.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: the ledger entry links only the campaign coverage row; `sequence-task-719` is co-anchored with `element-166`, not with a todo; no `s459` anchor.
