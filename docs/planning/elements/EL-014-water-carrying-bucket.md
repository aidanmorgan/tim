# EL-014 · Water-carrying bucket — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-014 delivery slice](../invest/named-elements.md#element-014). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md) and are cited by number. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-014 · Water-carrying bucket · Water |
| Anchors | [element-014](../requirements.md#element-014), co-anchored `sequence-task-567`; source record [todo-337](../requirements.md#todo-337); integration [sequence-task-387](../requirements.md#sequence-task-387); ledger [element-014](../invest/named-elements.md#element-014), owner S435 |
| Disposition | To deliver as a single element. It is the first store on a moving body. [EL-015](EL-015-leaky-bucket.md) reuses its body and store and adds a leak; it is a separate identity delivered after this one |
| Planned package | id `water_bucket`, directory `elements/WaterBucket/`, title "Water bucket" (coordinator decision 36: EL-069 is the "Hanging bucket", so no two Motion palette rows share a name) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` and `ENGINE-THERMAL` |
| Related | [EL-015](EL-015-leaky-bucket.md), [EL-004](EL-004-catch-basin.md), [EL-005](EL-005-liquid-funnel.md), EL-025; delivered rope parts: Rope anchor ([CAT-058](CAT-058-rope_anchor.md)), Pulley ([CAT-053](CAT-053-pulley.md)), Weight (CAT-067) |

## Declaration

Closest delivered shapes: the Weight for a dynamic rope load with a tie (`elements/Weight/WeightElementDeclaration.cs:43`) and the Balloon for a body whose mass includes an inventory (`elements/Balloon/BalloonElementDeclaration.cs:63`).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid store declaration of `ENGINE-LIQUID` (W4, W6).
- **Body.** One dynamic body. Origin at the centre of the outer box.
- **Colliders (eight boxes).** Outer 0.58 × 0.56 × 0.58; interior 0.50 × 0.50 × 0.50. Base 0.58 × 0.06 × 0.58 at (0, −0.25, 0); two walls 0.04 × 0.50 × 0.58 at (±0.27, 0.03, 0); two walls 0.50 × 0.50 × 0.04 at (0, 0.03, ±0.27). Bail: two posts 0.04 × 0.18 × 0.04 at (±0.27, 0.37, 0) and a bar 0.58 × 0.03 × 0.03 at (0, 0.475, 0).
- **Mass and inertia.** Shell 0.5 kg. Principal moments by the solid-box rule on the outer box: Ix = Iz = 0.5 × (0.56² + 0.58²)/12 = 0.0271 kg·m², Iy = 0.5 × (0.58² + 0.58²)/12 = 0.0280 kg·m². Contents add ρ·V at the body's centre, translation only (L12 b): total 0.5 kg empty to 2.5 kg full. Gravity (0, −9.81, 0); linear drag 0, as the Weight.
- **Material.** Family vessel material (EL-001).
- **Store.** Local id 1 on body 0. Capacity 0.125 m³ (2 marks, 2 kg), plan area 0.25 m², floor datum (0, −0.22, 0). Open aperture 0.50 × 0.50 at y = +0.28, normal +Y (it moves with the body). Tilt retention by L11 with w = 0.5 and H = 0.5. Its store is also a thermal node (owner decision 20; [TH-04 section 11](TH-04-electrical-heating-plate.md#thermal-conventions), T2), starting at the ambient 288 K (T1) unless a level authors another; inflow mixes by mass, outflow leaves at the store's temperature, and no row here reads temperature.
- **Displacement envelope (L12 a).** One box, the outer 0.58 × 0.56 × 0.58 (0.188 m³), so the bucket floats by its outline: empty it draws 0.5 / (16 × 0.336) = 0.093 m, and full (2.5 kg) 0.46 m of its 0.56 m.
- **Port.** `PortId.Tie`, `SimulationDomain.Rope`, Bidirectional, `PortRole.Tie`, `RopeAttachmentRole.Load`, at (0, 0.49, 0), normal +Y: the top of the bail, above the centre of mass at every fill, so a hung bucket stays upright.
- **Mouths.** None.

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `initial_volume` | Real f32 | m³ (W13) | 0 | 0 | 0.125 | Reject; build only |

- **Presentation.** `Visual` follows body 0's pose (`PresentationBodyBinding`). `Visual/Water` scale from the committed fill fraction (W12, missing).
- **Palette.** `ElementCategory.Motion`; palette order 147 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour blue (69/255, 99/255, 156/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Shell` | Open box, five slabs, blue `#45639c`, cream `#fff8e9` rim face | solid; five box colliders | body pose (dynamic) |
| `Visual/Water` | Box 0.50 × 0.50 × 0.50, cyan `#66b8c9`, alpha 0.16 | decorative: enclosed, moved by a binding | body pose and presentation binding |
| `Visual/Marks` | Two gold `#f7cb52` lines inside at 0.25 m and 0.50 m | decorative: printed flat | body pose |
| `Visual/Bail` | Two posts and a bar as the three bail colliders, gold `#f7cb52` | solid; three box colliders | body pose |
| `Loc_Port_Tie` | Locator | none | body pose |

The bail is a solid. A stream aimed exactly at the bar loses the bar's shadow, about a fifth of a pipe-bore stream (a 0.03 m bar across a contracted jet 0.155 m wide), and nothing when off-centre (L8). Icon: an open blue pail with a bail and a waterline. It differs from the Weight (a solid cylinder with an eye) by its open box and from the Catch basin by being small, blue and dynamic.

### Behaviour and numbers

- **Rope balance.** Over a Pulley against a 1.5 kg Weight: empty (0.5 kg) the bucket rises; the balance point is 1.0 kg of water, one mark, depth 0.25 m; full (2.5 kg) it descends with (2.5 − 1.5) × 9.81 = 9.8 N net, 9.8 / 4.0 = 2.5 m/s² before pulley inertia.
- **Filling.** Under an open tap's stream (about 0.047 m³/s) it passes the balance point after 0.0625 / 0.047 = 1.3 s and is full at 2.7 s; more overflows from the rim (L9).
- **Tipping.** Tilted 45° it retains 0.125 × (1 − 0.5 × 1) = 0.0625 m³; at 90° nothing (L11). What leaves falls from the lowest rim point as a stream and can be caught.
- **No double counting.** The liquid is in the store's ledger; its weight reaches the body only through L12 b.
- **Rope.** Ropes are solid (owner decision, 22 November 2026). The delivered rope draws a strand with no collider; that is a recorded visible-solid defect owned by `ENGINE-CONNECTION-MATERIALS`, not by this part. A solid rope above the mouth costs a stream its shadow, as the bail does (L8); there is no per-material exemption.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W21 dynamic body with declared mass and inertia; W1 baked colliders | present | body |
| W20 rope tie on a load, rope over a pulley | present | tie and balance |
| W22, W23 authored state, refusals | present | `initial_volume` |
| W4 fluid port contract | present, untested | store declaration |
| W11 contents add to the body's mass | narrower | gas stores only today; liquid: `ENGINE-LIQUID` (gap) |
| W6 store on a dynamic body | missing | `ENGINE-LIQUID` |
| W8 capture at a moving aperture | missing | `ENGINE-LIQUID` |
| W9 overflow and tilt retention | missing | `ENGINE-LIQUID` (gap: tilt is not in its text) |
| W12 fill driver; W13 m³ unit | missing | W12: `ENGINE-LIQUID`; W13's m³ unit: `ENGINE-GAS-NETWORK`, which lands first (EL-001 W13; owner decision 18, 22 November 2026) |
| W10 displacement envelope | missing | `ENGINE-LIQUID` |

Counts: 5 present, 1 untested, 1 narrower, 6 missing; the missing rows are `ENGINE-LIQUID`'s, except the m³ unit, which is `ENGINE-GAS-NETWORK`'s.

### Engine conformance (22 November 2026)

1. **Declaration data only.** One dynamic body, eight colliders, one displacement envelope, one store with an aperture, one rope tie, one parameter, two bindings.
2. **Generic laws.** "Filled load changes rope balance" is body mass from a declared inventory (L12 b) under the delivered rope and pulley constraints. Spill is L9 and L11 evaluated at the committed pose.
3. **Three layers.** *Colliders:* five boxes on the dynamic body. *Visuals:* follow the body's pose. *Animation:* the water box's height follows the committed fill; it never writes to physics.
4. **One-way flow and typing.** As EL-001; the tie uses the delivered typed port.
5. **Flat-state counts.** Bodies 1 (dynamic); colliders 8 boxes; joints 0 (the rope is the rope part's); drives 0; sensors 0; liquid nodes 1 store; thermal nodes 1; mouths 0; open apertures 1; displacement shapes 1; ports 1; bindings 1 body + 2 liquid; parameters 1.
6. **Performance fitness.** Eight boxes against the Weight's five (`elements/Weight/weight.tscn`): the three extra are the bail, a visible solid that carries the tie. No island sleeping exists today; once `ENGINE-ISLANDS-SLEEP` lands a resting bucket sleeps, and a change of contents is a mass change the island solver must wake for, which `ENGINE-LIQUID` states. Nothing is fast or small enough to tunnel.
7. **Tests are scenarios.** Rows K1 to K8.

## Sources and legacy

- Requirement: "Movable container couples conserved contents to mass and spill geometry"; outcome "Filled load changes rope balance; empty control does not." No variants.
- [sequence-task-387](../requirements.md#sequence-task-387); refinement [S706](../invest/refinements.md#s706) bucket-balance, bucket-tip; component research [water](../../component-research.md#water) lines 16, 32, 51 and 62.
- **Legacy search.** No liquid bucket. Rope-load analogue:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The Weight is a dynamic rope load with one `Tie` socket, Rope domain, Load attachment, above the body. | `parts/WeightPart.cs@a6c914e:L13-L21`; `engine/ConnectionPort.cs@a6c914e:L11-L11` | Carried forward; delivered (`elements/Weight/WeightElementDeclaration.cs:43`). |
| 2 | Weight mass 0.25 to 8 kg, default 4 kg, drag 0. | `parts/WeightPart.cs@a6c914e:L22-L34` | Carried forward as the counterweight scale; delivered (`WeightElementDeclaration.cs:33`). 1.5 kg sits between the empty and full bucket. |
| 3 | No water connection domain ever existed. | `engine/MachineData.cs@a6c914e:L68-L68` | Recorded; none is added (L6). |

**Files harvested:** `parts/WeightPart.cs`, `engine/ConnectionPort.cs`.

## Acceptance outline

Authority: [element-014](../requirements.md#element-014). Tolerances L17. Planned class `WaterBucketTests`. Rig: Rope anchor, Pulley, a 1.5 kg Weight on one side and the bucket on the other.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| K1 | Given `initial_volume` 0.125, when Run advances 3 s, then the bucket has descended at least 0.5 m and the Weight has risen | `WaterBucketTests.AFullBucketOutweighsTheCounterweight` | K2 | — |
| K2 | Given `initial_volume` 0, when Run advances 3 s, then the bucket has risen and the Weight descended | `WaterBucketTests.AnEmptyBucketIsLifted` | K1 | `initial_volume` 0.0625 (1.5 kg total): neither side moves more than 0.05 m in 3 s |
| K3 | Given an empty bucket held on a shelf under an open tap, when Run advances 4 s, then its volume is 0.125 m³, the excess is conserved in `Spilled`, and the body's committed mass is 2.5 kg within 0.07 kg | `WaterBucketTests.FillingRaisesTheMassToCapacityAndNoFurther` | tap closed: mass stays 0.5 kg | — |
| K4 | Given the rig with an empty bucket under the tap's stream, when Run advances, then the bucket first rises or holds and then descends once its volume has passed 0.0625 m³ (stall guard 10 s) | `WaterBucketTests.FillingReversesTheBalance` | tap closed: it never descends | — |
| K5 | Given a full bucket standing with one base edge against a Wall stop 0.12 m high, and a Linear pusher pressing the opposite wall 0.40 m above the base until the body's committed tilt is 45° within 5°, then its volume is the L11 value for that tilt within 0.01 m³ (0.0625 m³ at 45°) and the rest left from the lowest rim point into a basin below | `WaterBucketTests.TippingSpillsWhatTheTiltCannotHold` | upright: nothing leaves | tipped past 90°: empty |
| K6 | Given a slack rope, then the bucket falls freely and lifts nothing | `WaterBucketTests.ASlackRopeCarriesNothing` | K1 | — |
| K7 | Given `initial_volume` below 0, above 0.125 or non-finite, then it is refused whole | `WaterBucketTests.AVolumeOutsideItsLimitsIsRefused` | limits admitted | next f32 beyond each limit |
| K8 | Given a run, then Reset restores pose, volume and rope; save and load keep `initial_volume`, the pose and the rope connection | `WaterBucketTests.ResetAndSaveLoadKeepTheBucketAndItsRope` | — | — |
| K9 | Given an empty and a full bucket lowered into a Water tank holding 0.5 m³, then both float upright, drawing 0.09 m and 0.46 m within 0.03 m, and the tank's depth rises by the displaced volume | `WaterBucketTests.TheBucketFloatsByItsEnvelope` | a bucket with a 4 kg Bowling ball in it (6.5 kg against 3.0 kg of displacement) sinks | — |

Wrong-kind connection: an electrical or signal wire offered to the tie is refused by the delivered rule (W22). Chrome recipe (pending): anchor, pulley, a 1.5 kg Weight and a Water bucket joined with the rope tool; a tank and tap above the bucket.

## Open questions

All resolved; none needs the owner.

1. Variable mass at run time: a mass update from the declared inventory, the form the gas inventory already uses (L12 b); not a second body.
2. Sloshing: none; the surface is level at once.
3. Empty mass 0.5 kg and capacity 0.125 m³: kept (D1). The interior is now exactly 0.5 m cubed.

## Integration

- **Works with.** Delivered: Rope anchor, Pulley, Weight, Linear pusher. Planned: EL-001, EL-003, EL-004, EL-005, EL-015.
- **Campaign.** Chapter 7, levels 61–70 (Borrowed Weight, Catch the Escape); reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes; S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-337`, shared by five identities; `sequence-task-567` not co-anchored with a todo; no `s435` anchor).

## Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), then: `ENGINE-LIQUID`, including a store on a dynamic body, its mass coupling and a displacement envelope; `ENGINE-THERMAL`; EL-001; EL-003; EL-004; S7.
- **Files.** Adds `elements/WaterBucket/WaterBucket.csproj`, `WaterBucketElementDeclaration.cs`, `water_bucket.tscn`, `icon.svg`, `CuriousContraptions.tests/WaterBucketTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only if the mass coupling is delivered; it is an engine change otherwise and belongs to `ENGINE-LIQUID`.
- **Disqualifiers.** A store that cannot sit on a dynamic body; contents that do not change the body's mass; an aperture that does not follow the body's pose; no tilt retention.
