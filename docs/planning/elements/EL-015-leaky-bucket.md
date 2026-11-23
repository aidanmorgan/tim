# EL-015 · Leaky bucket — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-015 delivery slice](../invest/named-elements.md#element-015). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md) and are cited by number. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-015 · Leaky bucket · Water |
| Anchors | [element-015](../requirements.md#element-015), co-anchored `sequence-task-568`; source record [todo-337](../requirements.md#todo-337); historical task [todo-212](../requirements.md#todo-212); integration [sequence-task-387](../requirements.md#sequence-task-387); ledger [element-015](../invest/named-elements.md#element-015), owner S436 |
| Disposition | To deliver as a single element, after [EL-014](EL-014-water-carrying-bucket.md). Not a duplicate of EL-014 and not a family member with it: it adds a seat, an outlet and a parameter, and it needs two capabilities EL-014 does not (W7 seat, W14 opening from a Choice) |
| Planned package | id `leaky_bucket`, directory `elements/LeakyBucket/`, title "Leaky bucket" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` and `ENGINE-THERMAL` |
| Related | [EL-014](EL-014-water-carrying-bucket.md), [EL-004](EL-004-catch-basin.md) (catches the leak) |

## Declaration

Closest delivered shape: as EL-014.

- **Interfaces, body, colliders, mass, inertia, material, tie port.** Exactly EL-014's: one dynamic body, eight boxes (the five shell slabs and the three-box bail; outer 0.58 × 0.56 × 0.58, interior 0.5 m cubed), shell 0.5 kg, Ix = Iz = 0.0271 and Iy = 0.0280 kg·m², family vessel material, `PortId.Tie` (Rope, Load). Tie at the top of the bail, (0, 0.49, 0); the same displacement envelope.
- **Store.** As EL-014: capacity 0.125 m³, plan area 0.25 m², floor datum (0, −0.22, 0), aperture 0.50 × 0.50 at y = +0.28. Its store is also a thermal node (owner decision 20; [TH-04 section 11](TH-04-electrical-heating-plate.md#thermal-conventions), T2), starting at the ambient 288 K (T1) unless a level authors another; inflow mixes by mass, outflow leaves at the store's temperature, and no row here reads temperature.
- **Leak seat and outlet.** One seat between store 1 and a declared outlet (not a mouth) at (0, −0.28, 0), normal −Y, on the underside of the base. The head across the seat is the depth above the floor datum; the 0.06 m of base is not counted. Seat area = opening × 0.011310 m² (the area of a 0.06 m radius hole), with one opening per option of `leak` (L14 a).

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_volume` | Real f32 | m³ (W13) | 0.125 | 0 | 0.125 | Reject; build only |
| `leak` | Choice | None | 1 (Medium) | 0 | 2 | Reject; build only; options `Small`, `Medium`, `Large` |

| `leak` | Hole radius | Opening | Seat area | K = 0.6·A·4.42945 | Time to empty from full, 2·A_b·√0.5 / K |
| --- | --- | --- | --- | --- | --- |
| Small | 0.03 m | 0.25 | 0.002827 m² | 0.007515 | 47.1 s |
| Medium | 0.04 m | 0.4444 | 0.005027 m² | 0.013359 | 26.5 s |
| Large | 0.06 m | 1 | 0.011310 m² | 0.030058 | 11.8 s |

A leaky bucket starts full so the delayed counterbalance plays at once. A Choice is used because the area is not a linear function of a radius and a parameter has no step (L2).

- **Presentation.** As EL-014, plus `Visual/Ring` `UniformScale` from `leak` through `PresentationParameterBinding`. The leak stream is drawn by the shared liquid presentation (W8).
- **Palette.** `ElementCategory.Motion`; palette order 148, next to EL-014's 147 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour blue.

### Art and solidity

As EL-014's table, plus:

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Ring` | Slate `#556573` ring with a dark centre on the underside and repeated on the front wall near the base, flush | decorative: printed flat | body pose; size by the parameter |
| `Loc_Outlet` | Locator on the underside | none | body pose |

Icon: the blue pail with a drip under it. It differs from the Water bucket (EL-014) by the drip ring on its front wall, not by colour.

### Behaviour and numbers

- **Leak law (L7, L8).** Q = K·√d through the one seat. The stream leaves downward at Q/(Cd·A) = √(2·g·d), 3.1 m/s at full depth, plus the bucket's own velocity at the outlet, so a swinging bucket lays its stream along its path.
- **It stops at empty.** Q falls to zero with the depth; the volume never goes below zero (L4).
- **Delayed reversal.** Against a 1.5 kg Weight the full bucket (2.5 kg) descends. It becomes lighter than the Weight when its water is below 1.0 kg, depth 0.25 m. Time from depth 0.5 to 0.25 m: (2·A_b / K)·(√0.5 − √0.25) = (0.5 / K) × 0.2071: **13.8 s** Small, **7.8 s** Medium, **3.4 s** Large.
- **Caught leakage equals lost inventory.** A basin under the outlet gains what the bucket loses, in the same step (L8); a stream that misses is in `Spilled`.
- **Tilt.** As EL-014 (L11). While tilted the leak's head is the L4 depth at the low edge.
- **Rope.** As EL-014: ropes are solid (owner decision, 22 November 2026); the delivered rope's missing collider is owned by `ENGINE-CONNECTION-MATERIALS`.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W21, W1 dynamic body and colliders; W20 rope tie | present | as EL-014 |
| W22, W23 authored state, refusals | present | both parameters |
| W4 fluid port contract | present, untested | store declaration |
| W11 contents add to body mass | narrower | `ENGINE-LIQUID` |
| W6, W9 store on a dynamic body, overflow, tilt | missing | `ENGINE-LIQUID` |
| W7 a seat on a store | missing | `ENGINE-LIQUID` |
| W8 outlet on a moving body; stream start velocity includes the body's; capture | missing | `ENGINE-LIQUID` (gap) |
| W14 seat opening per Choice option | missing | `ENGINE-LIQUID` (gap) |
| W12, W13 | missing | W12: `ENGINE-LIQUID`; W13's m³ unit: `ENGINE-GAS-NETWORK`, which lands first (EL-001 W13; owner decision 18, 22 November 2026) |

Counts: 5 present, 1 untested, 1 narrower, 7 missing; the missing rows are `ENGINE-LIQUID`'s, except the m³ unit, which is `ENGINE-GAS-NETWORK`'s.

### Engine conformance (22 November 2026)

1. **Declaration data only.** EL-014's declaration plus one seat, one outlet and one Choice. The countdown mass loss of the historical task is not used: mass falls because liquid leaves.
2. **Generic laws.** The leak is L7 on a declared seat; the changing load is L12 b; the catch is L8.
3. **Three layers.** As EL-014; the ring is a flush print.
4. **One-way flow and typing.** `leak` is a Choice held as a typed option index.
5. **Flat-state counts.** Bodies 1 (dynamic); colliders 8 boxes; joints 0; drives 0; sensors 0; liquid nodes 1 store; thermal nodes 1; seats 1; outlets 1; open apertures 1; ports 1; bindings 1 body + 2 liquid + 1 parameter; parameters 2.
6. **Performance fitness.** As EL-014, plus one stream path per substep while it leaks.
7. **Tests are scenarios.** Rows Y1 to Y7.

## Sources and legacy

- Requirement: "Finite aperture discharges actual contents and changes load"; outcome "Caught leakage equals lost inventory; empty bucket stops leaking." No variants.
- [todo-212](../requirements.md#todo-212): "Extend moving containers with conserved water outflow, changing load and visible fill level; catch leaks in other containers … countdown-only mass loss is no longer the proposed contract." Refinement [S706](../invest/refinements.md#s706) bucket-leak; component research [water](../../component-research.md#water) lines 32 and 51.
- **Legacy search.** EL-001's scope plus "leak": only solver "cannot leak" assertions; the countdown bucket was never implemented. Rope-load facts are in EL-014. **Files harvested:** none.

## Acceptance outline

Authority: [element-015](../requirements.md#element-015). Tolerances L17. Planned class `LeakyBucketTests`.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| Y1 | Given a full bucket standing on two Wall blocks under opposite base edges, 0.30 m apart, over a basin, `leak` Medium, when Run advances 10 s, then the basin's gain equals the bucket's loss within 0.004 m³ and the bucket holds 0.125 × (1 − 10/26.5)² = 0.048 m³ within 0.012 m³ | `LeakyBucketTests.CaughtLeakageEqualsLostInventory` | Y2 | basin moved aside: the loss is in `Spilled` |
| Y2 | Given `initial_volume` 0, when Run advances 10 s, then no stream leaves and the basin holds 0 | `LeakyBucketTests.AnEmptyBucketDoesNotLeak` | Y1 | — |
| Y3 | Given Y1 run until the bucket is empty (stall guard 80 s), then its volume is 0 and stays 0, the outflow reported is 0, and the basin holds 0.125 m³ (the level declares a Run length of 80 s, `ENGINE-RUN-LENGTH`, owner decisions 13 and 17) | `LeakyBucketTests.TheLeakStopsAtEmpty` | — | never negative at any sampled tick |
| Y4 | Given the rope rig against a 1.5 kg Weight with a full bucket, when Run advances, then the bucket descends first and later rises (stall guard 25 s for Medium) | `LeakyBucketTests.TheLeakReversesTheBalance` | an EL-014 bucket in the same rig never rises | — |
| Y5 | Given `leak` Small, Medium and Large in three lanes, then the volumes left after 5 s are in that order, largest first | `LeakyBucketTests.ALargerLeakDrainsFaster` | — | an option index outside 0 to 2 is refused whole |
| Y6 | Given the committed body mass sampled at full and at empty, then it is 2.5 kg and 0.5 kg within 0.07 kg | `LeakyBucketTests.TheLoadFollowsTheContents` | — | — |
| Y7 | Given a run, then Reset restores volume, pose and rope; save and load keep both parameters, the pose and the rope | `LeakyBucketTests.ResetAndSaveLoadKeepTheBucket` | — | — |

Generic cover: W22, W23. Chrome recipe (pending): anchor, pulley, a 1.5 kg Weight and a full Leaky bucket; a basin under the bucket's lowest point.

## Open questions

All resolved; none needs the owner.

1. Authored leak size: yes, three sizes as a Choice (reason above).
2. A pipe on the leak hole: no; it is an outlet, not a mouth (D19).
3. Variable mass: as EL-014.

## Integration

- **Works with.** Delivered: Rope anchor, Pulley, Weight. Planned: EL-004, EL-014.
- **Campaign.** Chapter 7, levels 61–70 (Borrowed Weight); reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-337`, shared by five identities; `sequence-task-568` not co-anchored with a todo; no `s436` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID`; `ENGINE-THERMAL`; EL-001; EL-004; EL-014; S7.
- **Files.** Adds `elements/LeakyBucket/LeakyBucket.csproj`, `LeakyBucketElementDeclaration.cs`, `leaky_bucket.tscn`, `icon.svg`, `CuriousContraptions.tests/LeakyBucketTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only.
- **Disqualifiers.** A seat that cannot sit on a store of a dynamic body; a stream whose path starts inside the owning body's own collider and is stopped by it; any EL-014 disqualifier.
