# GAP-06 · Granular dispenser — named-identity readiness spec

**Current delivery (24 October 2026):** [GAP-06 delivery slice](../invest/named-elements.md#gap-06). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | GAP-06 · Granular dispenser · Gravity / fluid-flow neighbour (P3 potential; adopted for implementation by the 24 October 2026 owner decision) |
| Anchor | [requirements.md#gap-06](../requirements.md#gap-06) (task `sequence-task-539`); [named-elements entry](../invest/named-elements.md#gap-06); decision block [S639](../invest/scope-corrections.md#s639); model question [S635](../invest/decisions.md#s635) |
| Owner key | S639 |
| Related identities | [GAP-07 Granular sieve](GAP-07-granular-sieve.md) (takes this part's feed); [EL-121 Rate-window goal](EL-121-rate-window-goal.md); delivered Receiver, Pressure plate, Powered gate (the blade pattern). No CAT refines it. |
| Status | Not started; blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` and `ENGINE-CONTACT-OBSERVATIONS`, on the 3a story `ENGINE-JOINT-KINDS` (the spring rest position) and on `ENGINE-GRANULAR` (owner decision 14: sand waits for the granular engine). |

### Disposition

**To deliver as a single element**, one package: `granular_dispenser`, directory `elements/GranularDispenser/`, display name "Grain hopper". There is no grain part: grain is a declared granular inventory held by the hopper's store and conserved by the engine's ledger, never a set of placed bodies (owner decision, 22 November 2026, item 14). The identity is blocked on the engine programme (owner decision 3a) and then on `ENGINE-GRANULAR`, which itself follows `ENGINE-LIQUID` ([epics](../../../_bmad-output/planning-artifacts/epics.md), "Story ENGINE-GRANULAR").

## 2. Declaration

### Declaration to write

Follows the delivered Powered gate for the blade (`elements/PoweredGate/PoweredGateElementDeclaration.cs:74-93`): a static frame carrying one blade on a prismatic slider with a return spring and a supplied drive. The store and aperture use the contracts `ENGINE-GRANULAR` delivers.

- **Interfaces.** `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the granular store and aperture contracts of the engine story. `KinematicMode.Static`.
- **Body 0 (static), four box colliders.** Origin at the centre of the outlet, on the blade's top plane. Material: restitution 0.05, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s.

  | Collider | Centre | Half extents | Turn |
  | --- | --- | --- | --- |
  | `col_side_a`, `col_side_b` | (±0.395, 0.53, 0) | (0.02, 0.487, 0.30) | ∓14.9° about Z (0.25 m run over 0.94 m rise) |
  | `col_front`, `col_back` | (0, 0.53, ±0.32) | (0.54, 0.47, 0.02) | none |

  Inner mouth 1.0 × 0.6 m at the top, outlet 0.5 × 0.6 m. Inner volume: (0.5 + 1.0)/2 × 0.94 × 0.6 = 0.423 m³.
- **Body 1 (dynamic, on slider 0), one box collider.** `col_blade`, half extents (0.28, 0.02, 0.33), centred on the outlet when closed. Mass 0.5 kg; box moments m(b² + c²)/3 on half extents: X 0.5 × (0.0004 + 0.1089)/3 = 0.0182, Y 0.5 × (0.0784 + 0.1089)/3 = 0.0312, Z 0.5 × (0.0784 + 0.0004)/3 = 0.0131 kg·m². Material: restitution 0.05, friction 0.1 (a polished slide, so the feed's drag stays under the closing force; section 5), rolling resistance 0, bounce threshold 0.1 m/s. Gravity (0, −9.81, 0).
- **Slider 0.** A slider travels along its base frame's local Y (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:100-101`); the base frame is turned −90° about Z so travel lies along the part's +X. Lower limit 0 (closed), upper 0.56 m (clear of the outlet). Return spring 14 N/m, damping 4 N·s/m, **declared rest position −0.50 m** (0.50 m beyond closed), so an unpowered blade closes in every mounting orientation (owner decision, 22 November 2026, item 6). Drive: target 0.56 m, maximum speed 1.5 m/s, stopping acceleration 14 m/s², effort 60 N, power 90 W (60 × 1.5), `PrismaticInactiveResponse.Free` so the spring returns it.
- **Granular store (engine story's descriptor).** Interior: the vessel's inner volume. Two declared fractions, conserved separately:

  | Fraction | Grain diameter | Initial amount |
  | --- | --- | --- |
  | Fine | 0.05 m | `fine_feed` |
  | Coarse | 0.10 m | `coarse_feed` |

  Bulk density 6 kg/m³ for both (the game scale at which a Basketball, 1 kg in 0.165 m³, is 6.1 kg/m³). Grain friction 0.6 for both. Capacity 2.5 kg: 2.5 / 6 = 0.417 m³, under the 0.423 m³ interior. Repose limit 30°.
- **Aperture (engine story's descriptor).** The outlet rectangle, clear width 0.5 m along X when unobstructed; full-width rate 0.25 kg/s, independent of fill height; passage ratio 3 (a fraction passes only while the clear width is at least three of its grain diameters: 0.15 m Fine, 0.30 m Coarse); occlusion reach 0.10 m, the larger declared grain diameter. A solid narrows the clear width over the part of the outlet beneath which its top lies within the reach of the outlet plane: the closed blade (at 0 m) and the Wall of rows G4 and G5 (at 0.06 m) do; a solid lower than that does not, and material falls onto it as a pile. No row brings a pile within the reach: the nearest, G7's, tops out 0.28 m below the outlet. There is no command input to the aperture.
- **Ports.** `PowerIn`, Electrical, Input, role `PowerIn`, at (−0.50, 0.20, 0.34) on the front plate. One supply consumer targeting prismatic 0; one consumer node, 12 V, 90 W.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `fine_feed` | Real | kg | 1.0 | 0 | 1.25 | build-only; player-editable unless locked (D11) |
  | `coarse_feed` | Real | kg | 0 | 0 | 1.25 | build-only; player-editable unless locked (D11) |

  The two maxima sum to the 2.5 kg capacity, so no cross-parameter rule is needed. Rate, grain diameters, passage ratio, repose and density are fixed for this kind (D13).
- **Presentation bindings.** `Visual/Fill` height ← the store's committed surface height above the outlet plane; `Visual/Stream` visibility ← the aperture's committed flow; `Visual/Mark` colour slate to gold ← `PresentationDriver.CommittedActuatorDrive` (three channel bindings); `Visual/Blade` follows body 1's pose.
- **Palette.** Category `ElementCategory.Structure`. Order: 547 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Side_A`, `Visual/Side_B` cream `#fff8e9` | boxes as the colliders | solid, `col_side_*` | nothing |
| `Visual/Front` cyan inspection window `#66b8c9`, `Visual/Back` cream | boxes 1.08 × 0.94 × 0.04 | solid, `col_front`, `col_back`; the window is a transparent material, still a collider | nothing |
| `Visual/Lip` gold `#f7cb52` | band printed flush round the outlet on the side and front faces | decorative: printed flat | nothing |
| `Visual/Blade` gold | box 0.56 × 0.04 × 0.66 | solid, `col_blade` | body 1 pose |
| `Visual/Fill` wood `#c28f52` body with a navy `#293954` fill line along its top | the store's contents, seen through the window | the inventory's own picture: its surface supports rigid bodies under the engine story (visible = solid), so it needs no package collider | presentation binding (committed surface height) |
| `Visual/Stream` | a narrow column of grain dots under the outlet | decorative: material in flight, drawn by a binding | presentation binding (committed flow) |
| `Visual/Mark` | bolt-shaped mark printed flush on the front plate | decorative: printed flat, coloured by a binding | presentation binding |

Coarse grain is drawn with larger dots than Fine (size, not colour alone). No hose, chute or duct is drawn. Icon: a tapered vessel with a fill line and a gold bar under it.

## 3. Engine capabilities

### Capability probe

| Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- |
| Static box colliders from package art | present | `ISceneGeometryElement`, `CuriousContraptions.Abstractions/SceneGeometry.cs:10` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` |
| A supplied prismatic drive that an obstruction stops without adding energy | present | `PrismaticDriveDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:89` | `LinearPusherTests.ObstructionStopsTheHeadAndReportsBlockedWhileAnAdjacentWallDoesNot` |
| A rigid body's mass counted by a declared contact-load sensor | present | `SignalContactLoadDescriptor`, `elements/PressurePlate/PressurePlateElementDeclaration.cs:93` | `PressurePlateTests.ContactLoadSensorTracker_CountsEachBodyOnceAndSumsBodies` |
| A slider return spring with a declared rest position | narrower: the spring rests where the two frames coincide (`MechanicalContractValues.cs:100`); `Compression` sets only the starting position | `PrismaticSliderDescriptor`, `:104-114` | none for a rest position; the field is the general repair of owner decision 6, owned by `ENGINE-JOINT-KINDS` (spring rest position, owner decision 6; coordinator decisions 21 and 25) and first consumed by the Powered gate |
| A finite granular store with separately conserved fractions | **missing** | none | none; owner `ENGINE-GRANULAR` |
| An aperture with a declared rate independent of fill height | **missing** | none | none; owner `ENGINE-GRANULAR` |
| Passage by declared grain diameter against the aperture's clear width (jam) | **missing** | none | none; owner `ENGINE-GRANULAR` |
| An aperture's clear width reduced by any solid whose top lies within the aperture's declared occlusion reach | **missing** | none | none; owner `ENGINE-GRANULAR` |
| A free pile on solids, bounded by a repose limit, in the same ledger | **missing** | none | none; owner `ENGINE-GRANULAR` |
| A pile's mass counted by contact-load sensors | **missing** | none | none; owner `ENGINE-GRANULAR` |
| A store or pile surface that supports rigid bodies | **missing** | none | none; owner `ENGINE-GRANULAR` |
| A committed inventory read and the store's surface height as a presentation driver | **missing** | none | none; owner `ENGINE-GRANULAR` |

Counts: present 3, untested 0, narrower 1, missing 8. All eight missing rows have one owner, **`ENGINE-GRANULAR`**; its text in `epics.md` covers the first two and the load of a vessel by its contents, and needs the additions in the Batch M summary for the rest. Candidates for occlusion come from `ENGINE-SPATIAL-QUERIES` and loads are reported through `ENGINE-CONTACT-OBSERVATIONS`. Nothing needs code that names this part.

## 4. Sources and legacy

- **Requirements.** "A finite hopper releases simulated grains that pile, flow and jam. Granules remain discrete conserved material, not liquid or infinite repeated balls. Specify grain-size presets, outlet control and performance limits before campaign authoring." Visual: "a cyan inspection window, gold outlet lip and calm visible fill line". Outcome: "Discharged plus retained material equals initial feed within the explicit representation tolerance. Empty feed stops, a blocked aperture jams, and opening the physical route clears it. Verify interaction with buckets and weighing controls" ([gap-06](../requirements.md#gap-06)).
- **Decision blocks.** "One finite feed through a passable and a jammed opening fixes the supported preset and count limit; delivered grains never exceed fed grains" ([S639](../invest/scope-corrections.md#s639)). "Finite feed passes a wide opening and jams a narrow one; the count delivered never exceeds the count fed" ([S635](../invest/decisions.md#s635)).
- **Audit.** "Deterministic conserved quantity, collisions and a measured performance budget. Empty source stops; jam persists until a physical change clears it" (`docs/physics-puzzle-gap-audit.md@a6c914e:L85-L85`).
- **Element map.** The row names GranularTransport and FiniteLedger ([element map](../general-engine-element-map.md)).
- **Weighing source.** Pressure plate default threshold 0.5 kg (`elements/PressurePlate/PressurePlateElementDeclaration.cs:39`).
- **Legacy.** None: no granular, grain or hopper code, test or level exists in the legacy tree.

## 5. Acceptance outline

Point of truth: [gap-06](../requirements.md#gap-06). Chrome proof through real UI controls is pending the post-catalogue phase.

### Behaviour and numbers

Laws: the granular inventory law of `ENGINE-GRANULAR` (conserved transfer through an aperture at a declared rate, repose, load by mass), the supplied prismatic drive and its spring, and rigid contact.

- **What "discrete" means here.** The material has a declared grain diameter, so it jams, piles at a repose angle and never levels or equalises like a liquid; it is a finite ledger, so it cannot be an endless supply of balls.
- **Fill line.** Inner width at height h above the outlet is 0.5 + 0.532 h (0.5 m gained over 0.94 m). Volume to height h: (0.5 h + 0.266 h²) × 0.6. The default 1.0 kg is 1.0 / 6 = 0.167 m³: 0.5 h + 0.266 h² = 0.278, so h = (−0.5 + √(0.25 + 4 × 0.266 × 0.278)) / 0.532 = 0.45 m. Half of it, 0.5 kg (0.0833 m³): 0.5 h + 0.266 h² = 0.139, h = (−0.5 + √(0.25 + 4 × 0.266 × 0.139)) / 0.532 = 0.25 m. A full 2.5 kg (0.417 m³) stands at 0.93 m, under the 0.94 m rim.
- **Bucket fit.** The Receiver's floor top is 0.375 m below its origin and its tallest walls' tops 0.50 m above it (`elements/Receiver/basket.tscn`), 0.875 m above the floor. With the outlet 1.0 m above the floor, the blade's underside (0.04 m below the outlet) clears the wall tops by 1.0 − 0.04 − 0.875 = 0.085 m along its whole 0.56 m stroke. The 1.0 kg pile is 0.167 m³, a 30° cone of radius (0.167 / 0.605)^(1/3) = 0.65 m and height 0.65 × tan 30° = 0.38 m: inside the floor's 0.69 m inner half-width and 0.62 m below the outlet, beyond the occlusion reach.
- **A ball in the vessel.** A Tennis ball (`radius` 0.25 m, `elements/TennisBall/TennisBallElementDeclaration.cs:18`) fits: the inner depth is 0.60 m, and resting on the closed blade its centre is 0.25 m above the outlet, where the inner half-width is 0.25 + 0.266 × 0.25 = 0.317 m and the tapered side lies 0.317 / √(1 + 0.266²) = 0.306 m from the centre. A Basketball (0.68 m across) is wider than the 0.60 m inner depth and cannot enter.
- **Emptying time.** 1.0 kg at 0.25 kg/s takes 4.0 s; 2.5 kg takes 10.0 s. Every row fits the default 30 s Run (`CuriousContraptions.Abstractions/CadenceValues.cs:77`); a level that wants a slower pour declares its Run length under `ENGINE-RUN-LENGTH` (owner decision 13).
- **Partial opening.** The rate scales with the clear fraction: a 0.35 m clear width passes 0.25 × 0.35 / 0.5 = 0.175 kg/s; a 0.20 m width passes Fine at 0.10 kg/s and no Coarse (0.20 < 0.30).
- **Blade forces.** The spring's closing force is 14 × (x + 0.50): 7.0 N at closed and 14 × 1.06 = 14.8 N at the 0.56 m stroke. The blade weighs 0.5 × 9.81 = 4.9 N.
  - *On its side, travel axis pointing down:* gravity pulls the blade open with 4.9 N against 7.0 N, so it still closes with 2.1 N to spare. *Travel axis up:* gravity helps. *Upright and inverted:* gravity is across the travel.
  - *Upright under a full feed:* at most the whole 2.5 × 9.81 = 24.5 N rests on the blade; grain on blade friction is √(0.6 × 0.1) = 0.245 with the grain's declared 0.6, so drag is at most 0.245 × 24.5 = 6.0 N, under the 7.0 N closing force.
  - *Opening:* the 60 N drive works against at most 14.8 + 6.0 = 20.8 N (upright, full) or 14.8 + 4.9 = 19.7 N (on its side).
  - Whether an inventory drags a moving occluder at all is fixed by the engine story; the margins hold either way.
- **Weighing.** A pile on a supplied Pressure plate presses it when its mass passes the 0.5 kg default: 0.4 kg leaves it off, 0.6 kg turns it on, about 0.5 / 0.25 = 2.0 s after flow starts. A 0.6 kg pile is 0.1 m³; a cone at 30° has volume 0.605 r³, so r = (0.1 / 0.605)^(1/3) = 0.55 m and height 0.55 × tan 30° = 0.32 m. It fits the plate's 1.8 m face (`elements/PressurePlate/pressure_plate.tscn`); the outlet stands 0.6 m above the plate so the pile does not reach it.
- **Representation tolerance.** 0.01 kg on any sum of amounts: 2 % of the plate threshold and a twentieth of the 0.2 kg between the weighing row's two feeds. Each transfer debits and credits the same amount in one tick, so the tolerance covers only f32 accumulation over a Run.
- **Performance limits.** The supported number of stores, apertures and piles in a scene and their tick cost are measured by `ENGINE-GRANULAR` on its fixed reference scene, before and after (owner decision 3a's criterion). No number is set here; these rows use at most two stores, one aperture and one pile.
- **Tick order.** The supply route is resolved in Phase 1; the blade drive and contacts are solved in Phase 3; the plate's load is evaluated in Phase 5 and read by the next tick's Phase 1 ([solver model](../../gpu-f32-physics.md#solver-model)). The phase in which the inventory transfer and its load commit is fixed once by `ENGINE-GRANULAR` for every consumer.

### Acceptance rows

Planned class `GranularDispenserTests`. Amounts are read from the committed inventory read. Stall guard: 20 s of game time (twice the longest emptying time).

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| G1 | Given a hopper with `fine_feed` 1.0 centred over a Receiver, its outlet 1.0 m above the Receiver's floor, and a Battery on `PowerIn`, when Run, then the Receiver's pile holds 1.0 kg and the store 0 | `ASuppliedHopperEmptiesItsFiniteFeedIntoABucket` | with no wire the store still holds 1.0 kg after the stall guard | at every sampled tick store + in flight + pile = 1.0 kg, and the pile never exceeds what the store has lost | 0.01 kg |
| G2 | Given both feeds 0 and supply, then nothing leaves and the Receiver holds nothing | `AnEmptyFeedDeliversNothing` | G1 | none | exact zero |
| G3 | Given the Battery disabled 2 s into G1, then the blade closes, flow stops and the amounts on both sides stay fixed; when it is enabled again, the remainder is delivered | `ClosingTheOutletStopsTheFlowAndReopeningClearsIt` | G1 | none | 0.01 kg; no change over 3 s while closed |
| G4 | Given a Wall under the whole outlet whose top is 0.02 m below the blade's underside (0.06 m below the outlet plane, inside the 0.10 m occlusion reach), then the blade opens over it and nothing leaves for the stall guard; after Reset with the Wall moved aside, G1's outcome holds | `ABlockedOutletJamsUntilTheRouteIsOpened` | G1 | none | 0.01 kg |
| G5 | Given `coarse_feed` 1.0 and a Wall at G4's height leaving 0.20 m of the outlet clear, then nothing leaves; with 0.35 m clear the whole feed is delivered | `ANarrowOutletJamsAndAWideOnePasses` | `fine_feed` 1.0 through the same 0.20 m gap is delivered | 0.20 m against 0.35 m around Coarse's 0.30 m | 0.01 kg |
| G6 | Given `fine_feed` 1.25 and `coarse_feed` 0.75, then the amount delivered between 1 s and 2 s equals the amount delivered between 5 s and 6 s | `TheRateDoesNotChangeWithFillHeight` | none | none | each 0.25 kg within 0.01 kg |
| G7 | Given a hopper 0.6 m over a supplied Pressure plate at its default, with `fine_feed` 0.6, then the plate's level turns on | `APileOfSixTenthsOfAKilogramPressesAHalfKilogramPlate` | `fine_feed` 0.4 leaves it off for the stall guard | 0.4 kg against 0.6 kg around 0.5 kg | level state |
| G8 | Given a Tennis ball (`radius` 0.25 m) dropped into a hopper holding 1.0 kg, then it comes to rest on the fill surface, above the blade | `TheFillSurfaceSupportsARigidBody` | in an empty hopper it rests on the blade, its centre 0.25 m above the outlet plane | none | rest height within 0.05 m of the fill line plus the ball's radius (0.45 + 0.25 = 0.70 m above the outlet plane) |
| G9 | Given the fill at 1.0 kg and after delivering half, then `Visual/Fill`'s height reads 0.45 m and 0.25 m above the outlet plane | `TheFillLineFollowsTheCommittedInventory` | none | none | 0.01 m |
| G10 | An activation wire into `PowerIn` is refused and changes nothing | `AnActivationWireIntoThePowerInputIsRefused` | G1's wire connects | none | typed refusal |
| G11 | `fine_feed` or `coarse_feed` of −0.1, 1.3 or a non-finite value is refused atomically | `InvalidFeedIsRefused` (deleted when structural story S4 covers it) | 0 and 1.25 admitted | 0, 1.25 | typed refusal |
| G12 | Reset after G1 restores the store to 1.0 kg, removes every pile and closes the blade; a second Run repeats G1 | `ResetRestoresTheFeedAndTheClosedOutlet` | none | none | 0.01 kg; blade within 0.01 m of closed |
| G13 | Save and load keep both feeds and the wire; a loaded construction starts with its authored feed | `SaveAndLoadKeepTheFeedAndWiring` | none | none | exact parameter values |
| G14 | Given an empty, unpowered hopper mounted upright, on its side (travel axis down) and inverted, then the blade is closed in each | `AnUnpoweredBladeClosesInEveryOrientation` | supplied, it opens in each | three orientations | blade within 0.01 m of closed |
| G15 | Given a scene at the limit `ENGINE-GRANULAR` publishes, then G1's outcome is unchanged; one declaration beyond it is refused with a typed reason | `TheHopperHoldsAtThePublishedLimitAndRefusesBeyondIt` | none | at and one beyond the limit | set by that story's measured reference scene |

Collaborators the requirement names: a bucket (G1, the Receiver; the Moving bucket EL-069 adds its own row when delivered) and a weighing control (G7). Every row except G10, G11, G13 and G14 is executed on capabilities owned by `ENGINE-GRANULAR`.

### Integration

- **Works with.** Battery, Receiver, Pressure plate, Wall, Tennis ball (delivered); GAP-07; EL-069 (undelivered).
- **Campaign.** "Against the Grain": introduction 91, practice 92, reuse 96, 136, 141 ([campaign allocation](../requirements.md#campaign-gap-allocation)). An objective over a delivered amount needs the committed amount-reached observation of the engine story, selected by the goal list like any other observation. A level that fixes a filled hopper needs the lesson-fixture story (S8).
- **Coverage.** Section 7.

### Delivery

- **Shape.** Single element, one package, engine tier (first consumer of the granular capability).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (occlusion candidates) and `ENGINE-CONTACT-OBSERVATIONS` (contact loads); `ENGINE-JOINT-KINDS` (spring rest position, owner decision 6; coordinator decisions 21 and 25); `ENGINE-LIQUID`; `ENGINE-GRANULAR` with the additions in the Batch M summary; S7 widened to GAP identities; S3 preferred.
- **Files.** `elements/GranularDispenser/` (`GranularDispenser.csproj`, `GranularDispenserElementDeclaration.cs`, `granular_dispenser.tscn`, `icon.svg`), `CuriousContraptions.tests/GranularDispenserTests.cs`, this spec, the ledger entry and `TODO.md`.
- **Disqualifies the plan.** The engine story delivers no passage rule by grain diameter, no occlusion by solids, or no free pile outside a store interior; its inventory is not readable per fraction; or the spring rest position is not a declared field.

## 6. Open questions

Resolved:

1. **Grain representation.** A declared granular inventory on `ENGINE-GRANULAR`; no grain bodies and no grain packages (owner decision, 22 November 2026, item 14).
2. **Grain-size presets.** Two declared fractions, Fine 0.05 m and Coarse 0.10 m, each with its own feed parameter; a mixed feed is both (decided now: the sieve needs two sizes and S639 keeps other presets as distinct children).
3. **Outlet control.** A supply-driven blade that occludes the aperture, the delivered Powered gate behaviour; the aperture has no command input (one current way to gate a flow).
4. **Capacity and rate.** 2.5 kg and 0.25 kg/s, fixed (D13), from the arithmetic in section 5.
5. **Count limit.** S639's "count limit" is the engine story's measured limit of stores, apertures and piles (row G15), not a number chosen here.
6. **Catalogue entry.** One entry (D8), category Structure.

Needs the owner: none.

## Engine conformance (22 November 2026)

1. **Declaration data only.** A static vessel, a blade on a declared slider with a declared drive and spring, one declared store with two fractions, one declared aperture, one port, two parameters, bindings. No update loop, spawn rule or engine branch on this part.
2. **Generic laws.** The granular inventory law, the supplied prismatic drive and spring, rigid contact. Flow, jam, conservation and weighing are consequences of declared values; no row is "when grain meets the outlet, do Y". The missing capabilities are stated element-free with one owner, `ENGINE-GRANULAR`.
3. **Three layers.** Colliders: four static boxes and one blade box. Visuals: one mesh per collider, a printed lip and mark, and the inventory's fill body and stream. Animation: fill height, stream and mark from committed state; nothing writes back. Physics reads no art.
4. **Flow and typing.** One Electrical `PowerIn`; fractions are typed local ids; amounts are f32 kilograms.
5. **Flat state.** Bodies 2 (1 static, 1 dynamic); colliders 5 (box); joints 1 prismatic; drives 1; sensors 0; electrical nodes 1; supply consumers 1; granular stores 1 (2 fractions); apertures 1; presentation bindings 5, moving roots 1. All counts are fixed at compile; a tick creates nothing.
6. **Performance.** Five box colliders, as the Receiver. The inventory costs one store and one aperture however much grain it holds; the aperture's occlusion test takes candidates from `ENGINE-SPATIAL-QUERIES`. The blade moves at most 1.5 / 480 = 0.003 m per substep. The engine has no island sleeping today; a resting blade and pile rely on `ENGINE-ISLANDS-SLEEP`. The measured budget is row G15.
7. **Tests are scenarios.** G1 to G15 run the production simulation.

**Verdict: conforms; blocked on the engine programme (owner decision 3a) and on `ENGINE-GRANULAR`, a general capability stated element-free.**

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads. **A GAP identity cannot be bound at all today:** the reader lists only `EL-*.md` files and admits only an `EL-NNN` identity. **Blocked on the coverage follow-on story S7**, which widens the reader to GAP identities (owner decision 15, 22 November 2026).

| Field | Planned value |
| --- | --- |
| Package | `granular_dispenser` |
| Named identity | GAP-06, `../invest/named-elements.md#gap-06` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-539` |
| Decision | S639, `../invest/scope-corrections.md#s639` (the block exists on published main) |
| Mode records | `Configuration=Adjustable` (`fine_feed`, `coarse_feed`) |

What it lacks: GAP admission in the reader; a todo link in the ledger entry.
