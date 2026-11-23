# EL-021 · Sluice gate — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-021 delivery slice](../invest/named-elements.md#element-021). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md); the channel values are in [EL-007](EL-007-open-gutter.md). Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-021 · Sluice gate · Water |
| Anchors | [element-021](../requirements.md#element-021), co-anchored `sequence-task-574`; source record [todo-338](../requirements.md#todo-338); integration [sequence-task-389](../requirements.md#sequence-task-389) and [sequence-task-390](../requirements.md#sequence-task-390); ledger [element-021](../invest/named-elements.md#element-021), owner S442 |
| Disposition | To deliver as a single element: a short gated channel section that joins gutters end to end. Not a duplicate of the Powered gate ([CAT-051](../requirements.md#current-cat-051)): no drive, a channel bore and a liquid seat |
| Planned package | id `sluice_gate`, directory `elements/SluiceGate/`, title "Sluice gate" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK` (the one fluid port record, coordinator decision 16), `ENGINE-LIQUID`, `ENGINE-JOINT-KINDS` and `ENGINE-CONNECTION-MATERIALS` (solid strand; owner decision 1, 22 November 2026) |
| Related | [EL-007](EL-007-open-gutter.md), [EL-003](EL-003-tap.md) (the sealed counterpart), EL-027, EL-028; delivered Rope anchor, Pulley, Weight, Linear pusher |

## Declaration

Closest delivered shape: the Powered gate, a static frame with a blade on a prismatic slider and a return spring (`elements/PoweredGate/PoweredGateElementDeclaration.cs:76`), without its drive.

- **Interfaces.** `ISceneGeometryElement`, `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), the liquid fitting and seat declaration of `ENGINE-LIQUID` (W4, W7, W14).
- **Body 0 (static frame), three boxes.** A 0.30 m length of channel with the gutter's section, its walls carried up as posts. Origin as the gutter's. Floor 0.30 × 0.06 × 0.52 at (0, −0.10, 0); two side posts 0.30 × 0.60 × 0.06 at (0, +0.23, ±0.23), which are the channel walls carried up, tops at 0.53 m; no top beam: the slider's 0.25 m limit is the upper stop and the tie's rope rises clear between the posts, 0.20 m from each post's inner face. Channel floor top at y = −0.07; interior width 0.40. Family vessel material.
- **Body 1 (dynamic blade), one box.** 0.04 × 0.30 × 0.39 (0.005 m clear of each wall), 1.0 kg, centred at (0, +0.08, 0) when closed, its lower edge on the channel floor. Inertia by the box rule: 0.0202, 0.0128 and 0.0076 kg·m² about X, Y and Z. Material restitution 0.1, friction 0.3, bounce threshold 0.1 m/s, as the Powered gate's blade (`PoweredGateElementDeclaration.cs:47`). Gravity (0, −9.81, 0).
- **Slider.** `PrismaticSliderDescriptor` along the frame's +Y, travel 0 to 0.25 m, return spring 14 N/m with damping 4 N·s/m (the Powered gate's values) and a declared **rest position of −0.75 m**, which preloads the spring to 10.5 N at closed. The preload exceeds the blade's 9.81 N weight, so the blade closes upright, on its side and inverted (owner decision 6, 22 November 2026; with the rest position at 0 an inverted blade would fall open). No drive. At full lift the blade's top is at 0.08 + 0.15 + 0.25 = 0.48 m, under the posts' tops at 0.53 m.
- **Port.** `PortId.Tie` on body 1 at the blade's top centre, `SimulationDomain.Rope`, Bidirectional, `PortRole.Tie`, `RopeAttachmentRole.Load`. The rope leaves the tie vertically to a Pulley directly above it; every Weight figure below assumes that vertical pull. The blade's top face is also an ordinary contact surface for a pusher.
- **Liquid node.** One fitting with one seat between its two mouths. Seat area = s × 0.08 m², with s = clamp(lift / 0.20, 0, 1) and lift the committed slider coordinate (L14 b): the opening under the blade, 0.40 m wide, up to the full channel depth.
- **Mouths (channel bore 0.20, Bidirectional).** `Start` at (−0.15, −0.07, 0), normal −X; `End` at (+0.15, −0.07, 0), normal +X. They join gutter ends only (L6).
- **Parameters.** None. The gate starts closed; a gate that should start open is held by its rope.
- **Presentation.** `Visual/Blade` follows body 1's pose (`PresentationBodyBinding`, as the Powered gate's blade, `PoweredGateElementDeclaration.cs:21`).
- **Palette.** `ElementCategory.Control`; palette order 414 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour ochre.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Floor`, `Visual/PostA`, `Visual/PostB` | The three frame slabs, ochre `#c28f52`, each post's top edge banded navy `#293954`, printed flush | solid; three box colliders | body 0 pose (static) |
| `Visual/Blade` | Box 0.04 × 0.30 × 0.39, gold `#f7cb52` | solid; the blade collider | body 1 pose |
| `Visual/Scale` | Gold ticks every 0.05 m on one post's inner face, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end`, `Loc_Port_Tie` | Locators | none | body pose |

Icon: two posts over a channel with a raised blade and a rope rising from it. It differs from the Powered gate (a round sleeve with a motor housing) by its open two-post frame over an open channel.

### Behaviour and numbers

- **Forces on the blade.** Weight 9.81 N and the spring, 14 N/m × (0.75 + lift): lifting needs 20.3 N at closed and 23.8 N at full lift. The liquid exerts no force on it (L12 c). Over a Pulley, a 2.5 kg Weight (24.5 N) lifts it fully; 2.25 kg (22.1 N) holds it at 0.126 m; 2.14 kg (21.0 N) at 0.05 m; 2.0 kg (19.6 N) cannot lift it.
- **Flow under the blade.** Between two gutters there are three apertures in series: the upstream gutter's mouth join (K_c = 0.21261), the seat (s·K_c) and the downstream mouth join (K_c). K = K_c / √(2 + 1/s²).

| Lift | s | K | Head needed to pass 0.0341 m³/s (a tap at `opening` 0.5), (Q/K)² |
| --- | --- | --- | --- |
| 0.20 m or more | 1 | 0.21261 / √3 = 0.1228 | 0.077 m |
| 0.126 m | 0.63 | 0.21261 / √4.52 = 0.1000 | 0.116 m |
| 0.05 m | 0.25 | 0.21261 / √18 = 0.0501 | 0.463 m: more than the upstream gutter can give (0.199 m of head at its rim plus the 0.026 m drop across the gate), so it passes only 0.0501 × √0.225 = 0.024 m³/s and the rest overtops upstream |
| 0 | 0 | 0 | nothing passes |

- **Closed gate retains upstream volume.** The upstream gutter is sloped 5° toward the gate, because a level gutter would drain from its unjoined far end and nothing in Batch F closes a channel mouth. With the gate closed it retains C(θ) = 0.091 m³ (L11), fills in 0.091 / 0.0341 = 2.7 s and then overflows at its low rim, conserved (L9). Nothing is deleted and nothing passes the blade's side clearances: they are collision clearance, not apertures.
- **Open, 0.0341 m³/s, both gutters sloped 5° along the flow.** The downstream gutter's joined mouth is its high end and is dry, so the head across the three apertures is the upstream head at the gate plus the 0.30 × sin 5° = 0.026 m drop across the gate: 0.077 m in all, so the upstream head is 0.051 m, well inside the channel. That is at Run start: the tap's flow falls with the tank's head (√h = 1 − 0.03409 × t / 2), to 0.027 m³/s at 12 s, when the three apertures need (0.027 / 0.1228)² = 0.048 m and the upstream head is 0.048 − 0.026 = 0.02 m.
- **Rope.** Ropes are solid (owner decision 1, 22 November 2026). The delivered rope's missing collider is a recorded visible-solid defect owned by `ENGINE-CONNECTION-MATERIALS`, and this part is blocked on ENGINE-CONNECTION-MATERIALS (solid strand), because SG1, SG3, SG4 and SG7 use the rope. With no beam, the rope's vertical run from the tie meets nothing of the frame.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static frame; W21 dynamic blade | present | bodies |
| W18 prismatic slider with limits and return spring | present | the blade (the spring's declared rest position is missing: `PrismaticSliderDescriptor` has no such field; it is `ENGINE-JOINT-KINDS` (owner decision 6; coordinator decision 21)) |
| W20 rope tie on a load, rope over a pulley | present | lifting; the rope's collider is missing (owner decision 1): `ENGINE-CONNECTION-MATERIALS` |
| W22 authored state | present | pose |
| A port on a child body (`PortDescriptor.Body`, `CuriousContraptions.Abstractions/PortDescriptor.cs:66`) | present, untested | the tie is on body 1; no test of a rope tie on a slider body was opened |
| W4 fluid port contract | present, untested | fitting; the one fluid port record is `ENGINE-GAS-NETWORK`'s (coordinator decision 16) |
| W3 tube mouth and snap, channel bore | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20° are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID` | channel mouths |
| W7 fitting, seat, flow between two stores | missing | `ENGINE-LIQUID` |
| W14 seat opening from a committed prismatic coordinate | missing | `ENGINE-LIQUID` (gap) |

Counts: 5 present, 2 untested, 1 narrower, 4 missing (two for `ENGINE-LIQUID`, the spring's rest position for `ENGINE-JOINT-KINDS`, the rope's collider for `ENGINE-CONNECTION-MATERIALS`). W3's socket form is `ENGINE-CAPABILITY-INSTANCES`'s and W4's record `ENGINE-GAS-NETWORK`'s.

### Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, four colliders, one slider, one rope tie, one fitting with one seat, two mouths, one body binding. The earlier "channel-gate region aligned with the gutter cell under the blade" is gone: the gate does not reach into another part; it is a fitting joined between two stores.
2. **Generic laws.** The aperture is a seat opened by a committed coordinate (L14 b) under L7; the blade moves by the delivered slider, spring, rope and contact laws. "Retains upstream volume without deleting inflow" is a closed seat plus L9 on the upstream store.
3. **Three layers.** *Colliders:* three frame boxes, one blade box. *Visuals:* the blade art follows body 1's pose. *Animation:* none.
4. **One-way flow and typing.** The liquid step reads the slider coordinate committed by the previous substep and writes nothing to it.
5. **Flat-state counts.** Bodies 2 (1 static, 1 dynamic); colliders 4 boxes; joints 1 prismatic; drives 0; sensors 0; liquid nodes 1 fitting; seats 1; mouths 2; ports 1; bindings 1 body; parameters 0.
6. **Performance fitness.** Four colliders against the Powered gate's nine. The posts are the channel walls carried up and the slider's limit is the upper stop, so the frame adds no box to the channel's section. No island sleeping exists today; a resting blade will sleep once `ENGINE-ISLANDS-SLEEP` lands.
7. **Tests are scenarios.** Rows SG1 to SG7.

## Sources and legacy

- Requirement: "A moving gate changes an open channel aperture"; outcome "Closed gate retains upstream volume without deleting inflow." No variants.
- Refinements [S707](../invest/refinements.md#s707) and [S708](../invest/refinements.md#s708) sluice; [sequence-task-390](../requirements.md#sequence-task-390) "sluice-controlled floating transport"; component research [water](../../component-research.md#water) lines 34, 58 and 60.
- **Legacy search.** No liquid gate. Powered-gate analogue:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Opening is read from the committed coordinate of a blade on a guide joint. | `parts/PoweredGatePart.cs@a6c914e:L10-L19` | Carry forward "aperture = committed slider coordinate". Not the ball dimensions or the electric drive. |
| 2 | Blade gold `#f7cb52`; contact material (0.1, 0.1, 0.3). | `parts/PoweredGatePart.cs@a6c914e:L22-L23`, `:L46-L47` | Carried forward (colour and material). |
| 3 | Blade motion prepared by the part's own per-substep routine. | `parts/PoweredGatePart.cs@a6c914e:L54-L55` | Not carried (no element update loop). |

**Files harvested:** `parts/PoweredGatePart.cs`.

## Acceptance outline

Authority: [element-021](../requirements.md#element-021). Tolerances L17. Planned class `SluiceGateTests`. Rig: two 3 m gutters joined through the gate, all three sloped 5° along the flow (the upstream gutter's low end at the gate; the downstream gutter's low end free over a basin); a Water tank and a Tap at `opening` 0.5 over the upstream gutter; a basin under the downstream gutter's free end. The blade's tie runs vertically to a Pulley directly above it and down to a Weight.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| SG1 | Given a 2.5 kg Weight (gate lifted), when Run advances 12 s, then the basin's gain rate at 12 s equals the tank's loss rate within 10% and `Spilled` is 0. Hand: the tap at `opening` 0.5 passes 0.0341 m³/s at 1.0 m and the tank's head falls, √h = 1 − 0.03409 × 12 / 2 = 0.795, so about 0.027 m³/s at 12 s; the downstream wedge then holds (0.027 / 0.21261)² = 0.016 m of head, 0.40 × 0.016² / 0.175 ≈ 0.0006 m³, under the 0.004 m³ tolerance, so the row compares rates rather than contents | `SluiceGateTests.ALiftedGatePassesTheChannelFlow` | SG2 (closed: the basin's gain rate is 0) | at 12 s the upstream head at the gate is 0.02 m within 0.01 m, under the 0.20 m rim: (0.027 / 0.1228)² = 0.048 m across the three apertures, less the 0.026 m drop across the gate |
| SG2 | Given no Weight (gate closed), when Run advances 12 s, then the basin and the downstream gutter hold 0, the upstream gutter holds 0.091 m³ within 0.01 m³, and the tank's further loss is in `Spilled`; the ledger identity holds at every sampled tick | `SluiceGateTests.AClosedGateRetainsUpstreamAndDeletesNothing` | SG1 | the upstream gutter is full by 2.7 s within 10% |
| SG3 | Given a 2.0 kg Weight, then the blade stays closed (lift under 0.01 m); given 2.25 kg, it settles at 0.126 m within 0.02 m | `SluiceGateTests.TheBladeLiftsOnlyAgainstItsWeightAndSpring` | SG1 | — |
| SG4 | Given a 2.14 kg Weight (lift 0.05 m within 0.02 m), then the basin gains less after 12 s than in SG1 and the upstream gutter overtops | `SluiceGateTests.ASmallOpeningPassesLess` | SG1 | — |
| SG5 | Given the gate mounted on its side and inverted with no rope, then the blade rests closed in each (lift under 0.01 m after 3 s) | `SluiceGateTests.TheBladeClosesInEveryOrientation` | upright | — |
| SG6 | Given a gate end offered to a water pipe mouth, then no join forms; offered to a gutter end, it snaps | `SluiceGateTests.TheGateJoinsOnlyChannelMouths` | — | — |
| SG7 | Given a run, then Reset restores blade, gutters and rope; save and load keep the pose, both joins and the rope connection | `SluiceGateTests.ResetAndSaveLoadKeepTheGateAndItsRope` | — | — |

Generic cover: W22; a wire offered to the tie is refused by the delivered rule. Later-partner row: sluice-controlled floating transport ([sequence-task-390](../requirements.md#sequence-task-390)) runs in the EL-027 or EL-028 slice (Batch G). It must be a lock raising a floating body: no convention gives a current that carries one, because immersion damping is to the world, not to the liquid. Chrome recipe (pending): tank and tap over a gutter joined through a Sluice gate to a second gutter over a basin; a rope from the blade over a Pulley to a Weight.

## Open questions

All resolved; none needs the owner.

1. Actuation: rope or pusher work only; no powered or signal input (D19).
2. A larger sluice for the canal lock: no second size here (D13); EL-027's spec states what it needs.
3. Water pressure on the blade: none (L12 c).
4. `initial_lift`: removed. The blade starts closed; fewer controls, and an authored lift would need the slider's start coordinate to follow a parameter.
5. How the gate meets the gutter: by joining channel mouths, as every water part joins (D3). It does not act on a region of another part.

## Integration

- **Works with.** Planned: EL-007, EL-001, EL-003, EL-004, EL-027, EL-028. Delivered: Rope anchor, Pulley, Weight, Linear pusher.
- **Campaign.** Chapter 7, levels 61–70 (Canal Lift; the lesson The Mill's Song uses the sound parts taught at 75a to 76a, so it is placed after them, coordinator decisions 36 and 41); reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-338`; `sequence-task-574` not co-anchored with a todo; no `s442` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-GAS-NETWORK` (the one fluid port record); `ENGINE-LIQUID` including a coordinate-driven seat; `ENGINE-JOINT-KINDS` (the declared spring rest position; owner decision 6, coordinator decision 21); `ENGINE-CONNECTION-MATERIALS` (solid strand; owner decision 1, 22 November 2026); EL-001; EL-003; EL-004; EL-007; S7.
- **Files.** Adds `elements/SluiceGate/SluiceGate.csproj`, `SluiceGateElementDeclaration.cs`, `sluice_gate.tscn`, `icon.svg`, `CuriousContraptions.tests/SluiceGateTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** A seat that cannot read a prismatic coordinate; a rope tie not admitted on a slider body; a channel-bore fitting between two stores counted as more than three apertures.
