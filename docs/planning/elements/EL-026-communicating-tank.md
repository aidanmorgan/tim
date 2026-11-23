# EL-026 · Communicating tank — named-identity spec

**Current delivery (24 October 2026):** [EL-026 delivery slice](../invest/named-elements.md#element-026). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-026 · Communicating tank · Water |
| Anchors | [element-026](../requirements.md#element-026), co-anchored `sequence-task-579`; source record [todo-339](../requirements.md#todo-339); integration [sequence-task-390](../requirements.md#sequence-task-390); ledger [element-026](../invest/named-elements.md#element-026), owner S447 |
| Disposition | To deliver as a single element. Not a duplicate of the Water tank ([EL-001](EL-001-finite-reservoir.md)): it has two sockets on opposite faces so tanks chain, a quarter of the plan area, and a quarter of the capacity |
| Planned package | id `linked_tank`, directory `elements/LinkedTank/`, title "Linked tank" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID` and `ENGINE-THERMAL` |
| Related | [EL-001](EL-001-finite-reservoir.md), pipe kit [EL-008](EL-008-straight-water-pipe.md), [EL-010](EL-010-water-elbow-90.md) and [EL-012](EL-012-water-pipe-cap.md), [EL-027](EL-027-canal-lock-chamber.md), [EL-032](EL-032-pressure-meter.md) |

## Declaration

Closest delivered shape: the Receiver's open five-box vessel (`elements/Receiver/basket.tscn`); closest planned shape: the Water tank.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid store declaration of `ENGINE-LIQUID` on the one fluid port record of `ENGINE-GAS-NETWORK` (W4, W6).
- **Body.** One static body, mass 0. Origin at the centre of the outer box.
- **Colliders (five boxes).** Outer 0.74 × 1.15 × 0.74; interior 0.50 × 1.0 × 0.50. Floor 0.74 × 0.15 × 0.74 at (0, −0.5, 0); two walls 0.12 × 1.0 × 0.74 at (±0.31, 0.075, 0); two walls 0.50 × 1.0 × 0.12 at (0, 0.075, ±0.31). Family vessel material (EL-001: restitution 0.12, friction 0.3, rolling resistance 0).
- **Store.** Local id 1. Capacity 0.25 m³ (4 marks, 4 kg); plan area 0.25 m²; interior 0.50 × 1.0 × 0.50 (H = 1.0 m) above the floor datum (0, −0.425, 0). Open aperture 0.50 × 0.50 at y = +0.575, normal +Y, the top of the interior. Overflow point (0, 0.575, 0.37), the front rim. Streams: each sample path that ends inside the store's declared interior is credited to it; a sample that meets any other collider first is spilled (L8). Store 1 is also a thermal node (owner decision 20; [TH-04](TH-04-electrical-heating-plate.md) section 11, T2): heat capacity ρ·V × 1046 J/(kg·K), starting temperature declared ambient 288 K unless a level authors another; inflow mixes by mass and outflow leaves at the store's temperature. No row of this part reads temperature.
- **Sockets (pipe bore, Bidirectional, into store 1).** Declared snap sockets (L5), capture distance 0.20 m and angle 20°: `Start` at (−0.37, −0.425, 0), normal −X; `End` at (+0.37, −0.425, 0), normal +X. Both are at interior-floor height, 0.15 m above the part's base (the kit rule), so head equals depth. Two tanks placed side by side join `End` to `Start` with their centres 0.74 m apart.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_volume` | Real f32 | m³ (unit member missing, W13) | 0 | 0 | 0.25 | Reject; build only; player-editable unless the level locks it (D11) |

- **Presentation.** `Visual/Water` `ScaleY` and `LocalY` from the committed fill fraction (W12, missing).
- **Palette.** `ElementCategory.Power`; palette order 329 (the Water tank, EL-001, is 324; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cream (1.0, 248/255, 233/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Shell` | Open box, the five slabs above, cream `#fff8e9` | solid; the five box colliders | body pose (static) |
| `Visual/Water` | Box 0.50 × 1.0 × 0.50, cyan `#66b8c9` at alpha 0.16 | decorative: enclosed, moved by a binding | presentation binding |
| `Visual/Marks` | Gold `#f7cb52` lines on the inner back wall every 0.25 m | decorative: printed flat | nothing |
| `Visual/PortRings` | Navy `#293954` rings, radius 0.14, on the two side faces | decorative: printed flat | nothing |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end` | Socket locators, bore 0.10 | none | nothing |

Icon: a narrow tall tank with a port ring low on each side. It differs from the Water tank (square, one ring on the front) by its narrow outline and two side rings.

### Behaviour and numbers

Everything here is L4, L6 and L7 applied to stores with two sockets. There is no equalising rule.

- **Two tanks side by side.** One joined pair is one aperture, K₀ = 0.08349. With 0.25 m³ and 0, the level difference Δ obeys dΔ/dt = −K₀·√Δ·(1/A + 1/A) = −2·K₀·√Δ / A, so it closes in A·√Δ₀ / K₀ = 0.25 × 1 / 0.08349 = **3.0 s**; each ends at 0.125 m³ (depth 0.5 m).
- **Through a straight pipe.** Two apertures in series, K₀/√2 = 0.05904: 0.25 / 0.05904 = 4.2 s.
- **Three in a row.** 0.25 m³ in the first ends as 0.25 / 3 = 0.0833 m³ in each. Hand estimate: about two pair times, 6 s.
- **Different floor heights.** The second tank raised 0.80 m and joined by two 90° elbows ([EL-010](EL-010-water-elbow-90.md): each turns through 0.40 m in each axis, so two rise 0.80 m with no pipe between). Surfaces level in elevation, not depth: h₁ = h₂ + 0.80 and 0.25·(h₁ + h₂) = 0.25 give h₁ = **0.90 m** and h₂ = **0.10 m**. The route has two apertures (K₀/√2); the starting difference is 1.0 − 0.80 = 0.20 m, closing in 2·√0.20 / (2 × 0.05904 / 0.25) = 0.894 / 0.4723 = 1.9 s. Raised 1.30 m (a 0.5 m pipe, the kit minimum, between the elbows) the upper socket's datum is above the 1.0 m supply surface, so by L7's crest rule (A9) nothing passes.
- **Unequal areas.** Joined to a Water tank (1.0 m²) holding 1.0 m³, both surfaces settle at 1.0 / 1.25 = 0.80 m: this tank holds 0.20 m³. Time: dΔ/dt = −K₀·√Δ·(1/1 + 1/0.25), so 2 × 1 / (5 × 0.08349) = 4.8 s.
- **An open socket is a free end (L8).** A tank with water and an unjoined socket drains through it: one free socket empties a full tank in 2·A·√d₀ / K₀ = 0.5 / 0.08349 = 6.0 s; both free in 3.0 s. An end tank therefore carries a Water pipe cap ([EL-012](EL-012-water-pipe-cap.md)) on its outer socket.
- **Disconnected tanks do not equalise.** Two tanks 0.30 m apart share no edge (L6): each behaves alone.
- **Pressure.** At the sockets, 101 325 + 156.96 × depth: 101 482 Pa when full.
- **Levelling stays inside L7's clamp (b):** a transfer between two stores never passes the amount that would level them.
- **Why these values.** A quarter-area tank moves its level four times as far as a Water tank for the same volume, so equalising reads at a glance and finishes in seconds.

## Engine capabilities

Map families (navigation only, D14): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body and colliders | present | shell |
| W22, W23 authored state, refusals | present | `initial_volume` |
| W4 fluid port contract | present, untested | store and sockets |
| W3 snap sockets | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 2 sockets, 0 immersed ports, 0 outlets |
| W6 store; W7 joins, apertures, flow; W8 free end; W9 overflow and ledger | missing | `ENGINE-LIQUID` |
| W12 fill-fraction driver; W13 m³ unit | missing | `ENGINE-LIQUID` |
| Liquid store as thermal node | missing | `ENGINE-THERMAL` |

Counts: 3 present, 1 untested, 1 narrower, 7 missing.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, five colliders, one store with an interior, an aperture, an overflow point and two sockets, one parameter, two bindings.
2. **Generic laws.** Levelling is L7 across a joined pair. "Disconnected does not equalise" is the absence of an edge (L6). The raised boundary is the crest rule (L7, A9).
3. **Three layers.** *Colliders:* five boxes. *Visuals:* the table above. *Animation:* the water box reads the committed fill.
4. **One-way flow and typing.** Socket ids are package-local typed ids, roles are enums; volumes f32.
5. **Flat-state counts.** Bodies 1 (static); colliders 5 boxes; joints 0; drives 0; sensors 0; signal nodes 0; liquid nodes 1 store; thermal nodes 1, one per store; sockets 2; immersed ports 0; outlets 0; open apertures 1; overflow points 1; wire ports 0; bindings 2; parameters 1.
6. **Performance fitness.** Five boxes, as the Receiver. Static. Constant work per store and per aperture; stream candidates come from `ENGINE-SPATIAL-QUERIES` (L18).
7. **Tests are scenarios.** Rows T1 to T8.

## Sources and legacy

- Requirement: "Connected vessel exchanges volume according to pressure and elevation"; outcome "Disconnected tank does not equalise remotely." No variants.
- Integration [sequence-task-390](../requirements.md#sequence-task-390) ("Connected tank levels … react to the actual fluid state"); refinement [S708](../invest/refinements.md#s708). Component research [water](../../component-research.md#water): "Low pipes equalise levels"; "a split shares supply, never copies it".
- **Legacy.** None at `a6c914e` (searched for "communicating", "tank", "water", "liquid"). **Files harvested:** none.

## Acceptance outline

Authority: [element-026](../requirements.md#element-026). Tolerances L17. Planned class `LinkedTankTests`. Outer sockets are capped (EL-012) unless stated.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| T1 | Given two tanks joined side by side holding 0.25 m³ and 0, when Run advances until their depths differ by less than 0.01 m (hand time 3.0 s; stall guard 10 s), then each holds 0.125 m³ within 0.004 m³ and the total is unchanged within 0.004 m³ | `LinkedTankTests.JoinedTanksLevel` | T2 | both authored at 0.125 m³: every committed flow is under 0.0005 m³/s (L17) |
| T2 | Given the same tanks 0.30 m apart with all four sockets capped, when Run advances 10 s, then both volumes are unchanged within 0.004 m³ and every committed flow is under 0.0005 m³/s | `LinkedTankTests.DisconnectedTanksDoNotEqualise` | T1 | — |
| T3 | Given three tanks in a row with 0.25 m³ in the first, when Run advances until every depth differs from the mean by less than 0.01 m (hand estimate 6 s; stall guard 18 s), then each holds 0.0833 m³ within 0.004 m³ | `LinkedTankTests.AChainSharesOneLevel` | — | — |
| T4 | Given a second tank raised 0.80 m and joined by two 90° elbows, with 0.25 m³ in the lower, when Run advances until every committed flow is under 0.0005 m³/s (hand time 1.9 s; stall guard 10 s), then the depths end at 0.90 m and 0.10 m within 0.01 m | `LinkedTankTests.SurfacesLevelInElevationNotDepth` | T1 | raised 1.30 m (a 0.5 m pipe between the elbows): the upper tank stays empty within 0.004 m³ for 10 s |
| T5 | Given a tank joined to a full Water tank (its own other socket capped, the Water tank's front socket joined to it), when Run advances until every committed flow is under 0.0005 m³/s (hand time 4.8 s; stall guard 15 s), then both surfaces end at 0.80 m within 0.01 m | `LinkedTankTests.UnequalAreasShareOneSurface` | — | — |
| T6 | Given a full tank with one socket capped and one free, when Run advances until empty (hand time 6.0 s; stall guard 18 s), then `Spilled` holds 0.25 m³ within 0.004 m³ | `LinkedTankTests.AnOpenSocketDrains` | both capped: unchanged within 0.004 m³ | — |
| T7 | Given `initial_volume` below 0, above 0.25 or non-finite, then it is refused with a typed reason | `LinkedTankTests.AVolumeOutsideItsLimitsIsRefused` | limits admitted | next f32 beyond each limit |
| T8 | Given a run that moved liquid, then Reset restores both authored volumes and zeroes the accounts; save and load keep `initial_volume`, the poses and the join | `LinkedTankTests.ResetAndSaveLoadKeepTheAuthoredState` | the state before Reset differs | 0 and 0.25 |

Generic cover: W22 and W23. The part has no wire port, so any wire naming it is refused. T4 needs EL-010 and EL-008 (boundary); T5 needs EL-001. Chrome recipe (pending): place two Linked tanks side by side until they snap, cap the outer ports, fill one.

## Open questions

All resolved; none needs the owner.

1. Separate identity or the Water tank with a second port: separate (D8); the second socket and the size are deltas.
2. Built-in link valve: none (D19). A Tap between two tanks is the valve.
3. Capacity: fixed (D13).

## Integration

- **Works with.** EL-001, EL-003, EL-008 to EL-012, EL-018, EL-032 (none delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 81–100, 126–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element, after EL-012.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20). Then EL-001, EL-008, EL-010, EL-012; S7.
- **Files.** Adds `elements/LinkedTank/LinkedTank.csproj`, `LinkedTankElementDeclaration.cs`, `linked_tank.tscn`, `icon.svg`, `CuriousContraptions.tests/LinkedTankTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** A store that cannot declare two sockets; a transfer step that overshoots the levelling amount (L7).

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-339`; `sequence-task-579` co-anchored with `element-026`, not with a todo; no `s447` anchor.
