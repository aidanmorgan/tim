# EL-028 · Buoyant platform — named-identity spec

**Current delivery (24 October 2026):** [EL-028 delivery slice](../invest/named-elements.md#element-028). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

**Repair (audit H-4, L-4).** A Basketball on a flat deck rolled to a rail and heeled it under. The deck is now 0.30 m thick, its rails 0.15 m high, and it carries a centre seat that holds a ball on the deck's axis; the stability below is computed with the deck edge flooding, not the small-angle estimate.

**Repair after the second audit (23 November 2026, `docs/planning/elements/readiness/audits/reaudit-group-2.md`, EL-028).** The seat walls were 0.04 m high, so a Basketball on their edges would have its bottom at 0.125 m, below the deck top at 0.15: it rested on the deck instead, with 0.04 m of play, and heeled the deck about 10.5°, failing B2. The seat walls are now 0.12 m high; every number below is recomputed for them, including the large-angle table (union of the nine boxes clipped at each heel).

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-028 · Buoyant platform · Water |
| Anchors | [element-028](../requirements.md#element-028), co-anchored `sequence-task-581`; source record [todo-339](../requirements.md#todo-339); integration [sequence-task-390](../requirements.md#sequence-task-390); ledger [element-028](../invest/named-elements.md#element-028), owner S449 |
| Disposition | Member of the Family slice "Floating bodies" with [EL-169](EL-169-cork-float.md) (Cork float). Both are one solid dynamic body with declared mass and shape and need no capability beyond the Float's ([EL-016](EL-016-float.md)). Not a duplicate of either: a square railed deck of 0.81 m² with a ball seat that carries a Basketball |
| Planned package | id `float_deck`, directory `elements/FloatDeck/`, title "Floating deck" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | [EL-016](EL-016-float.md), [EL-169](EL-169-cork-float.md), [EL-170](EL-170-raft.md), [EL-029](EL-029-boat.md), [EL-027](EL-027-canal-lock-chamber.md), [EL-004](EL-004-catch-basin.md) |

## Declaration

Closest delivered shape: the Weight, a dynamic body with declared mass and inertia (`elements/Weight/WeightElementDeclaration.cs`), without its tie.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`. No liquid interface: immersion acts on any dynamic body (L12 a).
- **Body.** One dynamic body. `PayloadPolicy.None`. Origin at the centre of the deck slab; its underside is at y = −0.15 and its top at +0.15.
- **Colliders (nine boxes).** Deck 0.90 × 0.30 × 0.90 at the origin. Four rails on its top edges, 0.15 m high: two 0.90 × 0.15 × 0.04 at (0, 0.225, ±0.43) and two 0.04 × 0.15 × 0.82 at (±0.43, 0.225, 0). A centre seat of four boxes 0.12 m high forming a square pocket 0.40 m across: two 0.04 × 0.12 × 0.48 at (±0.22, 0.21, 0) and two 0.40 × 0.12 × 0.04 at (0, 0.21, ±0.22), standing on the deck top (y 0.15 to 0.27). None overlap (the first pair spans |x| 0.20 to 0.24, the second |x| ≤ 0.20); displaced volume is their union (A3): deck 0.243, rails 2 × 0.0054 + 2 × 0.00492 = 0.0206, seat 2 × 0.00230 + 2 × 0.00192 = 0.0084; 0.243 + 0.0206 + 0.0084 = 0.2721 m³.
- **Mass and inertia.** 1.0 kg; centre of mass declared at (0, 0.024, 0) (uniform density over the nine boxes: (0.0206 × 0.225 + 0.0084 × 0.21) / 0.2721 = 0.0236), so 0.174 m above the underside. Box rule on the deck: Ix = Iz = 1.0 × (0.30² + 0.90²) / 12 = 0.075 kg·m²; Iy = 1.0 × (0.90² + 0.90²) / 12 = 0.135 kg·m². Mean density 1.0 / 0.2721 = 3.7 kg/m³. Gravity (0, −9.81, 0); linear drag 0.
- **Material (f32).** Restitution 0.2, friction 0.5, rolling resistance 0.03, bounce threshold 0.1 m/s: a matte deck that cargo does not skate on.
- **Ports, stores, parameters.** None. No store, so no thermal node.
- **Presentation.** `Visual` follows the body's pose.
- **Palette.** `ElementCategory.Motion`; palette order 149 (the Cork float, EL-169, is 152; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour coral (240/255, 110/255, 84/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Deck` | Slab 0.90 × 0.30 × 0.90, cream `#fff8e9` top, coral `#f06e54` sides | solid; the deck box collider | body pose (dynamic) |
| `Visual/Rails` | Four rails 0.15 m high, navy `#293954` | solid; the four rail box colliders | body pose |
| `Visual/Seat` | Square collar 0.48 m across, 0.12 m high, gold `#f7cb52` | solid; the four seat box colliders | body pose |
| `Visual/Line` | Navy band on the sides at the unloaded waterline, 0.077 m above the underside, flush | decorative: printed flat | body pose |

Icon: a square slab with a tall rim and a small square collar in the middle. It differs from the Cork float (a round disc) and the Raft (three logs) by its square railed outline.

### Behaviour and numbers

All of this is L12 a applied to nine boxes; there is no platform rule. Water is 16 kg/m³, so the 0.81 m² deck sinks 1 / (16 × 0.81) = 0.0772 m per kilogram.

- **Draught.** Unloaded 1.0 / 12.96 = **0.077 m**; with a Tennis ball (0.35 kg) 0.104 m; with a Basketball (1.0 kg) **0.154 m**, leaving 0.146 m of freeboard to the deck top.
- **Displacement limit.** Fully under water the deck, rails and seat lift 16 × 0.2721 = 4.35 kg: it carries at most **3.35 kg** of cargo clear of the water. A Bowling ball (4 kg, radius 0.28 m, 0.092 m³) makes 5.0 kg: the deck goes under. In deep water the pair floats with 5.0 / 16 = 0.3125 m³ submerged: the whole deck union (0.2721 m³) and a 0.0404 m³ cap of the seated ball, π·h²·(0.84 − h)/3 = 0.0404 at h = 0.257 m; the ball's bottom is 0.336 m above the underside, so the surface stands 0.593 m above the underside and the deck's top 0.29 m under it. In a vessel shallower than 0.59 m, such as a Catch basin, the deck rests on the floor.
- **The seat.** A ball resting on the seat touches its four inner top edges, 0.20 m from the axis and 0.27 m above the slab centre (0.42 m above the underside). A Basketball (radius 0.34 m) sits with its centre √(0.34² − 0.20²) = 0.275 m above the edges, at 0.545 in the body frame and 0.695 m above the underside; its bottom is 0.545 − 0.34 = 0.205, **0.055 m clear of the deck top** (0.15), so it bears only on the four edges. A Tennis ball (radius 0.25 m) sits √(0.25² − 0.20²) = 0.15 m above the edges, centre 0.57 m above the underside, bottom 0.02 m clear of the deck. A Bowling ball (radius 0.28 m) sits √(0.28² − 0.20²) = 0.196 m above them, centre at 0.466 in the body frame, bottom 0.036 m clear. A seated ball stays on the axis until the deck heels past atan(0.20 / 0.275) = **36°** (Basketball; 53° for a Tennis ball).
- **Stability with cargo.** Small angle, KB + BM − KG, with I = 0.90 × 0.90³ / 12 = 0.0547 m⁴ and the deck's KG 0.174 m:
  - Unloaded: 0.039 + 0.0547 / 0.0625 − 0.174 = 0.039 + 0.875 − 0.174 = **0.74 m**.
  - Tennis ball seated: KB 0.052, BM 0.0547 / 0.0844 = 0.648, KG (0.174 + 0.35 × 0.57) / 1.35 = 0.277: **0.42 m**.
  - Basketball seated: KB 0.077, BM 0.0547 / 0.125 = 0.437, KG (0.174 + 0.695) / 2.0 = 0.4345: **0.080 m**.
  - Large angle, Basketball seated, with the submerged volume of the nine boxes computed at each heel (the deck edge goes under at atan(0.146 / 0.45) = 18°): righting moment 0.06 N·m at 2° (small-angle check 2.0 × 9.81 × 0.080 × sin 2° = 0.055), 0.30 at 10°, 0.49 at 15°, 0.72 at 20°, 0.73 at 25° (the greatest), 0.53 at 30°, 0.24 at 35°, zero near 38°. The ball leaves the seat at 36°, inside that range. At rest the seated ball adds no heel.
  - A ball set off the seat cannot reach the deck: between the seat wall's outer face (0.24 m) and a rail's inner face (0.41 m) there is 0.17 m, less than a Tennis ball's, Bowling ball's or Basketball's diameter, so it bridges the seat edge (0.24, 0.27) and the rail edge (0.41, 0.30). A Basketball there has its centre 0.268 m off the axis: 1.0 × 9.81 × 0.268 = 2.6 N·m of heel, more than the 0.73 N·m greatest righting moment, so the deck capsizes. That is why the seat exists: it is the one place a Basketball rides.
- **Lost water removes support.** With no liquid there is no submerged volume: the deck rests on the vessel floor, its top 0.30 m above it. It floats once the depth passes its draught.
- **Displacement raises the level (L4).** Unloaded it displaces 0.0625 m³: 0.05 m in a Catch basin (1.25 m²), 0.0625 m in a Lock chamber (1.0 m²).
- **Rising water lifts cargo.** In a Lock chamber the deck top rises with the surface ([EL-027](EL-027-canal-lock-chamber.md)): the lesson "rising water delivers a ball onto a track".
- **Bobbing.** Stiffness 156.96 × 0.81 = 127 N/m; unloaded frequency √(127 / 1.0) = 11.3 rad/s; immersion damping 1.0 × 0.0625 / 0.2721 = 0.230 s⁻¹ (L12 a), a damping ratio of 0.230 / (2 × 11.3) = 0.010. c = 1.0 s⁻¹ is a starting value that `ENGINE-LIQUID` sets for a damping ratio near 0.5 at a floating draught (L12 a); at the starting value a 0.077 m bob decays to 0.01 m in ln 7.7 / 0.115 = 18 s. Loaded with a Basketball: √(127 / 2.0) = 8.0 rad/s, damping 1.0 × 0.125 / 0.2721 = 0.46 s⁻¹, ratio 0.029; a ball set in the seat of a floating deck starts a 0.077 m bob that is 0.077 × e^(−0.23 × 10) = 0.008 m at 10 s, so B2 reads its draught averaged over the last 2 s.
- **Fit.** 0.90 × 0.90 fits the Lock chamber (1.0 × 1.0) and the Catch basin (1.25 × 1.0).

## Engine capabilities

Map families (navigation only, D14): Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W21 dynamic body with declared mass and inertia | present | the deck |
| W22 authored state | present | pose |
| W10 immersion: buoyancy and damping from displaced volume (union of shapes, A3); displaced volume raises the level | missing | `ENGINE-LIQUID` |

Counts: 2 present, 0 untested, 0 narrower, 1 missing; one owner. The package could be declared today; its acceptance cannot run before W10 and a store to float in.

### Engine conformance (22 November 2026)

1. **Declaration data only.** One dynamic body, nine box colliders, one material.
2. **Generic laws.** Floating, the load limit and grounding are L12 a with the declared mass and boxes. Cargo rides by contact and friction; the seat holds a ball by contact.
3. **Three layers.** *Colliders:* nine boxes. *Visuals:* follow the body. *Animation:* none.
4. **One-way flow and typing.** Values are f32.
5. **Flat-state counts.** Bodies 1 (dynamic); colliders 9 boxes; joints 0; drives 0; sensors 0; liquid nodes 0; thermal nodes 0; ports 0; bindings 0 (the art follows the body pose); parameters 0.
6. **Performance fitness.** Nine boxes against the Float's one sphere: the rails keep cargo aboard, the seat keeps a ball on the axis (without it the deck capsizes), and all are visible solids. Immersion is evaluated only for bodies whose candidates `ENGINE-SPATIAL-QUERIES` returns for a store interior (L18). Whether a floating body may sleep is `ENGINE-ISLANDS-SLEEP`'s; nothing here assumes it.
7. **Tests are scenarios.** Rows B1 to B7.

## Sources and legacy

- Requirement: "Loaded floating deck carries physical cargo with displacement limits"; outcome "Overload or lost water removes support." No variants; the campaign row keeps "distinct hull/load modes" as separate rows, which are this part's load cases B1 to B3.
- Integration [sequence-task-390](../requirements.md#sequence-task-390). Component research [water](../../component-research.md#water): "Dynamic body + displacement"; recipe Quietly Rising.
- **Legacy.** None at `a6c914e` (searched for "buoyan", "platform", "float", "water"). **Files harvested:** none.

## Acceptance outline

Authority: [element-028](../requirements.md#element-028). Tolerances L17. Planned class `FloatDeckTests`. The vessel is a Catch basin with `initial_volume` 0.3125 m³ unless stated: with a seated Basketball aboard it stands at (0.3125 + 0.125) / 1.25 = 0.35 m, under the 0.40 m rim with no overflow.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| B1 | Given the deck placed with its underside on the water, when Run advances 20 s, then its underside's depth below the surface, averaged over the last 2 s (more than three bobbing periods of 0.56 s, so an undamped remainder averages out), is 0.077 m within 0.01 m | `FloatDeckTests.TheDeckFloatsAtItsDraught` | B4 | — |
| B2 | Given a Basketball set in the seat of a floating deck, when Run advances 10 s, then the ball is still in the seat, the heel is under 5° and the draught averaged over the last 2 s is 0.154 m within 0.02 m | `FloatDeckTests.ALoadedDeckRidesLower` | B1 | a Tennis ball in the seat: 0.104 m |
| B3 | Given a Bowling ball (4 kg, radius 0.28 m) set in the seat of a floating deck, when Run advances 10 s, then the deck rests on the basin floor with its top below the surface (floating, deck and ball would need 0.59 m of depth; aground at the 0.40 m rim the deck, the rails and seat to 0.40 m and a 0.064 m cap of the ball displace 0.243 + 0.0138 + 0.0070 + 0.0033 = 0.267 m³, a lift of 4.27 kg against 5.0 kg, so it stays down; the displaced volume takes the basin to its rim and the rest overflows, so the surface stands 0.10 m above the deck's 0.30 m top) | `FloatDeckTests.AnOverloadTakesTheDeckUnder` | B2 | — |
| B4 | Given an empty basin, then the deck rests on the floor with its top 0.30 m above it within 0.01 m, loaded or not | `FloatDeckTests.NoWaterNoSupport` | B1 | basin at depth 0.05 m (buoyancy 16 × 0.81 × 0.05 = 0.65 kg, under 1.0 kg): still aground |
| B5 | Given a basin with a Drain opened under a floating loaded deck, when the basin empties, then the deck and its cargo come to rest on the floor | `FloatDeckTests.LostWaterRemovesSupport` | — | — |
| B6 | Given B1, then the committed level is 0.05 m higher within 0.01 m than the basin's volume alone gives, and the liquid total is unchanged | `FloatDeckTests.DisplacementRaisesTheLevel` | — | — |
| B7 | Given a run, then Reset restores the authored pose and save and load keep it | covered by W22 | — | — |

Generic cover: W22. No port, no parameter. B5 needs EL-006. Later-partner row: lifting in a lock runs as EL-027 row K5. Chrome recipe (pending): fill a basin from a tap, drop the deck in, set a ball in its seat.

## Open questions

All resolved; none needs the owner.

1. Hull-volume or load parameters: none (D13). Load is the cargo placed on it.
2. Tie port for towing: none (D19). The Boat is the towed and blown craft.
3. Family with EL-016: planned with EL-169 because EL-016 is the first proof of immersion and lands first.
4. Cargo stability: a centre seat 0.12 m high and 0.15 m rails (decision under the standing delegation, audit H-4; seat height re-chosen after the second audit so a Basketball bears only on the seat's edges, 0.055 m clear of the deck): a ball anywhere else capsizes the deck by the calculation above.

## Integration

- **Works with.** Delivered balls; EL-004, EL-006, EL-027 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 81–100, 126–140, 146–150. First taught at **66a**, before the Canal lock chamber (67a) that lifts it (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)). Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Family slice "Floating bodies" with EL-169, after EL-016.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (immersion candidates, L18), `ENGINE-ISLANDS-SLEEP` (a change of level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES`; then `ENGINE-GAS-NETWORK`; then `ENGINE-LIQUID`; EL-004, EL-016; S7. Every row uses a Catch basin, so the part does not wait for EL-027; lifting in a lock runs later as EL-027 row K5.
- **Files.** Adds `elements/FloatDeck/FloatDeck.csproj`, `FloatDeckElementDeclaration.cs`, `float_deck.tscn`, `icon.svg`, `CuriousContraptions.tests/FloatDeckTests.cs`; edits `CuriousContraptions.slnx` and this spec. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`); lists S3 owns need an edit until S3 lands.
- **Review tier.** Data-only.
- **Disqualifiers the slice's probe must re-check.** Buoyancy applied at the body centre instead of the centroid of the submerged part (the deck would not right itself); submerged volume not computed per box, or overlapping shapes counted twice.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-339`; `sequence-task-581` co-anchored with `element-028`, not with a todo; no `s449` anchor.
