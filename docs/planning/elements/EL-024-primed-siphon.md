# EL-024 · Primed siphon — named-identity spec

**Current delivery (24 October 2026):** [EL-024 delivery slice](../invest/named-elements.md#element-024). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-024 · Primed siphon · Water |
| Anchors | [element-024](../requirements.md#element-024), co-anchored `sequence-task-577`; source record [todo-338](../requirements.md#todo-338); integration [sequence-task-389](../requirements.md#sequence-task-389); ledger [element-024](../invest/named-elements.md#element-024), owner S445 |
| Disposition | To deliver as a single element. It is the first consumer of the crested fitting (L3, L7 with A9) and of immersed mouths at both ends (L5, A1) |
| Planned package | id `siphon`, directory `elements/Siphon/`, title "Siphon" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` |
| Related | [EL-168](EL-168-siphon-priming-bulb.md) (primer), [EL-022](EL-022-water-pump.md) (alternative primer), [EL-001](EL-001-finite-reservoir.md), [EL-004](EL-004-catch-basin.md), [EL-012](EL-012-water-pipe-cap.md) (caps the tank mouth in every rig), [EL-167](EL-167-solenoid-tap.md) (row P4), [EL-035](EL-035-wick.md) (the same fitting, self-priming) |

## Declaration

Closest delivered shape: the Wall, a static part of plain boxes; no delivered part is an over-the-rim conduit.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid fitting declaration of `ENGINE-LIQUID` on the one fluid port record of `ENGINE-GAS-NETWORK` (W4, W7), with a crest (L3, L7).
- **Body.** One static body, mass 0. Origin at the centre of the bridge; the crest datum is y = 0.
- **Colliders (three boxes).** Intake leg 0.20 × 1.20 × 0.20 at (−0.30, −0.60, 0); bridge 0.80 × 0.20 × 0.20 at the origin; discharge leg 0.20 × 1.70 × 0.20 at (+0.30, −0.85, 0). Family vessel material (EL-001: restitution 0.12, friction 0.3, rolling resistance 0). The legs are 0.60 m apart centre to centre, which straddles a 0.12 m vessel wall with 0.14 m clear on each side.
- **Liquid node.** One crested fitting (L3, L7 with A9): crest datum y = 0; prime volume V_p = 0.0125 m³ (0.2 kg); self-priming rate 0. It declares **no lift limit** (A9, coordinator decision 21): the lift is bounded by the intake leg, because the intake uncovers first. It declares no store.
- **Immersed mouths.** `Start`: immersed (L5), Intake, area A₀, at (−0.30, −1.20, 0). `End`: immersed (L5), Discharge, area A₀, at (+0.30, −1.70, 0); in no store it is a free end (L8). An immersed mouth is a fluid port of the one record with no socket; its node is resolved by the innermost interior that contains its point (A1), the same query as a depth probe.
- **Socket.** `Branch`: a declared snap socket (L5), pipe bore, Intake, opening into the prime inventory, at (0, 0.10, 0), normal +Y, capture distance 0.20 m and angle 20°.
- **Parameters.** None. Leg lengths are fixed (D13).
- **Presentation.** `Visual/Column` `ScaleY` from the committed prime fraction (an observation of L13; driver missing, W12).
- **Palette.** `ElementCategory.Structure`; palette order 528 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cyan (102/255, 184/255, 201/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Tube` | Square-section inverted U, the three boxes above, cyan `#66b8c9` at alpha 0.16 | solid; the three box colliders | body pose (static) |
| `Visual/Column` | Opaque cyan core inside the tube | decorative: enclosed, moved by a binding | presentation binding |
| `Visual/PrimeRing` | Gold `#f7cb52` ring round the `Branch` socket, flush on the bridge top | decorative: printed flat | nothing |
| `Visual/EndBands` | Navy `#293954` bands at both leg ends, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end`, `Loc_Port_mouth_branch` | Locators (two immersed-port locators, one socket) | none | nothing |

Icon: an inverted U with one long leg and a gold ring on top. It differs from the Wick (thin, ochre, no ring) by thickness and the ring.

### Behaviour and numbers

- **Unprimed, it conducts nothing.** This is L7's crest rule (A9 wording): liquid passes the crest datum only while the highest total head among the wet stores feeding the fitting is at or above it, or the crested fitting's prime inventory is full. In every placement over an open rim the supply surface is below the crest, so with less than V_p in the prime inventory nothing passes, however far the outlet is below the source.
- **Priming.** Liquid delivered into `Branch` fills the prime inventory. Liquid beyond V_p passes on down the discharge leg. With the press damping of 40 N·s/m (audit H-2), a full Priming bulb under a 2.0 kg Weight set on its cap at rest travels (2.0 × 9.81 + 1.96)/100 − 0.03 = 0.186 m and delivers 0.02 × 0.186/0.25 = 0.0149 m³ ≥ 0.0125 m³; damping ratio 40/(2·√(100 × 2.2)) = 1.35, so there is no overshoot, and 84% of the travel (the 0.156 m that delivers 0.0125 m³) is reached in about 0.7 s.
- **Primed, it obeys L7.** Two apertures in series (the two immersed mouths), K = K₀/√2 = 0.08349/1.41421 = 0.05904. **Worked placement:** a full Water tank stands on Wall blocks 0.90 m high, all within its 1.24 × 1.24 footprint, its `End` mouth capped ([EL-012](EL-012-water-pipe-cap.md)); the intake is 0.05 m above its floor (0.20 m above the tank's base), so the crest is 1.25 m above the floor and 0.25 m above the rim, and the bridge's underside is 0.15 m above the rim. The outlet is 0.50 m below the intake and free: 0.90 + 0.20 − 0.50 = **0.60 m above the bench**, 0.05 m above the rim of a Catch basin that stands on the bench against the Wall blocks, the outlet 0.24 m from the tank's outer face and so over the basin's interior (inner wall face 0.12 m out). With tank depth d the head is d + 0.45: initial flow 0.05904 × √1.45 = **0.0711 m³/s**; depth 1.0 → 0.05 m takes (2 × 1.0 / 0.05904) × (√1.45 − √0.50) = 33.88 × 0.4971 = **16.8 s** and moves 0.95 m³. The basin holds 0.5 m³; the rest leaves its rim as pending overflow and is spilled (L9), which the ledger counts (L10).
- **Air entry breaks the column.** When the upstream surface falls to the `Start` point (depth 0.05 m here) the mouth is dry: the column breaks, the 0.0125 m³ prime inventory leaves through `End` in that step, and the edge is closed again. Refilling the tank does not restart it.
- **It cannot lift indefinitely.** The lift is crest minus supply surface, and the intake is wet only while the surface is above it, so the lift never exceeds the 1.20 m intake leg. Raised 0.30 m, the intake stands 0.35 m above the floor: it uncovers at depth 0.35 m and 0.35 m³ stays. Head d + 0.15 (outlet 0.15 m below the floor); time 1.0 → 0.35 m = 33.88 × (√1.15 − √0.50) = 33.88 × 0.3653 = **12.4 s**.
- **Reversed head does nothing.** With the outlet immersed in a receiver whose surface is at or above the source surface, L7 gives no flow; equal levels stop it.
- **Flooded crest.** A fitting whose supply head is at or above its crest passes liquid by L7 without a primer and fills its prime inventory. Over an open rim this cannot happen.
- **Insufficient priming.** Under a 1.5 kg Weight set on the cap at rest the bulb travels (1.5 × 9.81 + 1.96)/100 − 0.03 = 0.137 m (damping ratio 40/(2·√(100 × 1.7)) = 1.53: no overshoot) and delivers 0.02 × 0.137/0.25 = **0.0109 m³**, under V_p: delivering 0.0125 m³ needs 0.156 m of travel, 0.019 m more.
- **Thermal.** The fitting declares no store and so no thermal node (owner decision 20 covers stores). Whether the 0.0125 m³ prime inventory carries a temperature is put to the coordinator (Open question 6).
- **Why these values.** Legs of 1.20 and 1.70 m reach the floor of a Water tank over its rim and end below its base. The intake leg sets the greatest lift; raising the part one step shows it.

## Engine capabilities

Map families (navigation only, D14): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body and colliders | present | body |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | fitting, immersed ports and socket |
| W3 snap sockets | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 1 socket (`Branch`), 2 immersed ports, 0 outlets |
| W7 fittings, apertures, flow; W8 free end and stream | missing | `ENGINE-LIQUID` |
| Immersed mouth resolved by the innermost interior (L5, A1) | missing | `ENGINE-LIQUID`; the point query is `ENGINE-SPATIAL-QUERIES` |
| Crested fitting: prime inventory, crest rule, break on a dry intake (L3, L7 with A9) | missing | `ENGINE-LIQUID` |
| W12 driver for the committed prime fraction | missing | `ENGINE-LIQUID` |

Counts, one per capability: 2 present (W1, W22), 1 untested (W4), 1 narrower (W3), 5 missing (W7, W8, immersed mouth, crested fitting, W12). No thermal row: the part declares no store.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, three colliders, one crested fitting, one socket, two immersed ports, one binding. There is no siphon state enum: what a player sees is the prime inventory (empty, part full, full) and the edge flow.
2. **Generic laws.** Flow is L7. Priming, the closed edge and the break are the crested-fitting rule (L3, L7 with A9), which any package may declare (the Wick declares it with a self-priming rate). The greatest lift is geometry (the intake leg), not a declared limit. Nothing reads "when a bulb is attached".
3. **Three layers.** *Colliders:* three boxes. *Visuals:* the table above. *Animation:* the column reads the committed prime fraction.
4. **One-way flow and typing.** Socket and port ids are package-local typed ids, roles are enums; volumes f32.
5. **Flat-state counts.** Bodies 1 (static); colliders 3 boxes; joints 0; drives 0; sensors 0; signal nodes 0; liquid nodes 1 crested fitting (one prime inventory row); thermal nodes 0 (no store); sockets 1; immersed ports 2; outlets 0; wire ports 0; bindings 1; parameters 0.
6. **Performance fitness.** Three boxes, the Wall's order of cost. Each immersed port is one point query, resolved once at Run while the part and its store are static (`ENGINE-SPATIAL-QUERIES`); a free `End` adds a fixed number of stream sample paths (L18).
7. **Tests are scenarios.** Rows P1 to P9 run the production simulation on built rigs.

## Sources and legacy

- Requirement: "Continuous liquid column carries flow while the source and head permit"; outcome "Breaking the column stops the siphon; it cannot lift indefinitely above available pressure." No variants. "Cannot lift indefinitely" is met by the intake leg (row P5).
- Integration [sequence-task-389](../requirements.md#sequence-task-389) ("maintain/break a primed siphon"); refinement [S707](../invest/refinements.md#s707) ("Require priming and continuous geometry"). Component research [water](../../component-research.md#water): "Air entry breaks the column; the receiving surface must be lower"; recipe Prime Time.
- **Legacy.** None at `a6c914e` (searched for "siphon", "prime", "water", "liquid"). **Files harvested:** none.

## Acceptance outline

Authority: [element-024](../requirements.md#element-024). Tolerances L17. Planned class `SiphonTests`. Rows use the worked placement (tank on 0.90 m Wall blocks, its mouth capped, a Catch basin under the outlet) and a Priming bulb joined to `Branch` with a 2.0 kg Weight set on its cap at rest, unless stated. Residual volumes are asserted as depths, because the intake's datum sets them; L17's depth tolerance (0.01 m) is the quantity's own.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| P1 | Given a full tank and the bulb pressed, when Run advances until every committed flow is under 0.0005 m³/s (hand time 0.7 + 16.8 = 17.5 s; stall guard 53 s; the level declares a Run length of 55 s (`ENGINE-RUN-LENGTH`)), then the tank's depth is 0.05 m within 0.01 m and the ledger identity holds at every sampled tick | `SiphonTests.APrimedSiphonDrainsTheTankToItsIntake` | P2 | — |
| P2 | Given the same construction with no bulb (`Branch` unjoined), when Run advances 20 s, then the tank keeps 1.0 m³ within 0.004 m³ and every committed flow is under 0.0005 m³/s (L17) | `SiphonTests.AnUnprimedSiphonCarriesNothing` | P1 | — |
| P3 | Given a 1.5 kg Weight set on the bulb's cap at rest (it delivers 0.0109 m³), when Run advances 10 s, then the prime inventory holds 0.0109 m³ within 0.004 m³, the tank keeps 1.0 m³ within 0.004 m³ and every committed flow through the siphon is under 0.0005 m³/s | `SiphonTests.InsufficientPrimingLeavesItClosed` | P1 | — |
| P4 | Given P1's construction plus a second Water tank, full, on Wall blocks 2.55 m high beside the source, with a Solenoid tap ([EL-167](EL-167-solenoid-tap.md)) open-ended over the source's aperture, its `PowerIn` fed by an enabled Battery and its `FirstIn` by a second Battery authored Disabled; when P1's flow has stopped, the command Battery is enabled through its Run control (`enabled` is `ParameterRunEdit.RunControl`, `elements/Battery/BatteryElementDeclaration.cs:48`–`:49`; lane `RunControlLane.SourceEnable`, switched during Run by `ClutchTests.EqualInertiaDeclaredClosure_SplitsSpinHalfAndNeverCreatesKineticEnergy`, `CuriousContraptions.tests/ClutchTests.cs:317`, command at `:408`) and Run advances 10 s more (Run length 55 + 10 = 65 s, `ENGINE-RUN-LENGTH`), then the source has gained 0.42 m³ within 10% and the Catch basin's volume changes by under 0.004 m³ over those 10 s | `SiphonTests.ABrokenColumnDoesNotRestart` | the command Battery left Disabled: the source keeps its 0.05 m depth | — |
| P5 | Given the siphon raised 0.30 m and primed, when Run advances until every committed flow is under 0.0005 m³/s (hand time 0.7 + 12.4 = 13.1 s; stall guard 40 s; the level declares a Run length of 40 s (`ENGINE-RUN-LENGTH`)), then the tank's depth is 0.35 m within 0.01 m (0.35 m³ stays) | `SiphonTests.ARaisedSiphonLeavesMoreBehind` | P1 | raised 0.10 m: the depth ends at 0.15 m within 0.01 m |
| P6 | Given the source authored at 0.40 m³ and the outlet immersed in a second Water tank (mouth capped) whose floor is 0.50 m lower, authored at 0.90 m³, so both surfaces stand at the same elevation; when primed and Run advances 20 s, then neither volume changes by more than 0.004 m³ (the bulb's surplus 0.0149 − 0.0125 = 0.0024 m³ apart) | `SiphonTests.EqualLevelsDoNotFlow` | P1 | the receiver authored at 0.80 m³ (surface 0.10 m lower): within 20 s they level at 0.35 and 0.85 m³ within 0.004 m³ (levelling time √0.10 / 0.05904 = 5.4 s) |
| P7 | Given P1, then the first committed flow after the prime is full is 0.0711 m³/s within 10% | `SiphonTests.FlowFollowsHead` | — | — |
| P8 | Given a run that primed and flowed, then Reset empties the prime inventory and restores every store and account; save and load keep the pose and the `Branch` join | `SiphonTests.ResetEmptiesThePrime` | the state before Reset differs | — |
| P9 | Given prime fractions 0, 0.5 and 1, then the column art is empty, half and full | `SiphonTests.TheColumnShowsThePrime` | — | — |

Rig arithmetic. **P4:** the upper tank's base is 2.55 m up, so its tap's free end is 2.70 m up, 0.65 m above the source rim (0.90 + 1.15 = 2.05 m). The open tap passes 0.0482 m³/s from a full tank ([EL-003](EL-003-tap.md), K = 0.04820); its stream lands 2.56 × √(2 × 0.65 / 9.81) = 0.93 m out at first and, after 10 s at depth (1 − 10/41.5)² = 0.576 m, 0.93 × √0.576 = 0.71 m out. The source's aperture is placed 0.50 to 1.50 m out, with the siphon on its far wall (intake leg 1.16 to 1.36 m out), so every sample lands inside it (L8). The source gains 1 − 0.576 = 0.42 m³ and its surface rises above the intake, but the prime inventory is empty and the crest rule keeps the edge shut. **P6:** the outlet is 0.45 m below the source floor, so the receiver's floor is 0.50 m lower and the outlet 0.05 m above it; the receiver abuts the source, so the discharge leg (0.14 to 0.34 m from the source's outer face) is 0.02 m clear of the receiver's inner wall face (0.12 m). Source surface = floor + 0.40; receiver surface = (floor − 0.50) + 0.90: equal.

Generic cover: W22 (authored state; the part has no wire port, so any wire naming it is refused). A mouth of another bore does not join `Branch` (EL-001 R9). Later-partner row: the pump as primer runs in this slice as `SiphonTests.APumpDeliveringToBranchPrimes` once EL-022 is delivered (an unpowered pump is open, L15, so the pump's own supply tank must stand below the crest). Chrome recipe (pending): place a tank on blocks, cap its port, hang the siphon over its rim, snap a bulb on top, set a Weight on the bulb.

## Open questions

All resolved but one, which is put to the coordinator (6).

1. Crest limit: none (A9, coordinator decision 21). The intake leg bounds the lift; at the family density of 16 kg/m³ (D2) ambient pressure would allow 645 m, which never binds.
2. `initial_prime`: removed (D19). No requirement row or lesson asks for a pre-primed siphon.
3. Re-priming in the same Run: allowed by priming again; never by itself.
4. Sealed-outlet priming: not needed. Priming is liquid delivered at the crest, so an open outlet does not defeat it.
5. Leg-length parameters: removed (D13).
6. **For the coordinator:** owner decision 20 makes every store a thermal node; this part has only a fitting with a 0.0125 m³ prime inventory. Recommended default: a fitting's inventory carries no temperature of its own and the liquid it passes keeps the temperature of the store it left (a rule for the liquid and thermal conventions, not this part). No row of this spec reads temperature, so nothing here is blocked.

## Integration

- **Works with.** EL-168, EL-022, EL-001, EL-004, EL-012, EL-167, EL-030 (none delivered); Weight, Wall and Battery (delivered). The Weight presses the bulb and never meets liquid here.
- **Campaign.** Chapter 7, levels 61–70 ("Siphon of relief"); reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element; EL-168 follows it directly.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20); `ENGINE-RUN-LENGTH` (rows P1, P4, P5). Then EL-001, EL-004, EL-012; EL-003 and EL-167 (row P4, taught at 61 and 64); S7. The primed rows need the bulb, which follows this siphon in level 65a, so they run in the EL-168 slice.
- **Files.** Adds `elements/Siphon/Siphon.csproj`, `SiphonElementDeclaration.cs`, `siphon.tscn`, `icon.svg`, `CuriousContraptions.tests/SiphonTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only if the probe finds every missing row delivered. Rows P1 and P3 to P7 are cross-part rows owned by the EL-168 slice (the bulb is taught after this siphon in level 65a).
- **Disqualifiers.** No crested fitting; no immersed port resolved by the innermost interior; no declared `Branch` snap socket.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: the ledger entry links only the umbrella `todo-338`; `sequence-task-577` is co-anchored with `element-024`, not with a todo; `scope-corrections.md` has no `s445` anchor.
