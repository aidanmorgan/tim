# EL-226 · Hinged elastic catapult — named-identity spec

**Current delivery (21 November 2026):** [EL-226 ledger entry](../invest/named-elements.md#element-226), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending. Propagated 23 November 2026 to the [element-spec decisions of 22 November 2026](readiness/owner-decisions-2026-11-22.md).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-226 · Hinged elastic catapult · Mechanical |
| Requirement anchor | [todo-397](../requirements.md#todo-397) (`sequence-task-380`) |
| Ledger entry / owner | [element-226](../invest/named-elements.md#element-226); slice `named-el-226`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a) and `ENGINE-JOINT-KINDS` (the hold on the arm hinge, its release by a physical request bound to `ActivationIn`, and the hinge spring's rest position; engine-stories-index C2).** Run "struck and sprung arms" in delivery batches 1c. Not a duplicate of the Wound spring: delivery is rotary, through a swung cup with a release angle. |
| Planned package | id `elastic_catapult`, directory `elements/ElasticCatapult/` |
| Related | [CAT-071 Wound spring](CAT-071-wound_spring.md) (winding, retention, release), [EL-224 Counterweighted scoop catapult](EL-224-counterweighted-scoop-catapult.md) (same hinge-latch gap), [EL-235 Jack-in-the-box](EL-235-jack-in-the-box.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP` (a wound arm held at rest); then `ENGINE-JOINT-KINDS` (the hold on the revolute coordinate, its release by a physical request bound to `ActivationIn`, and the hinge spring's declared rest position at the fired angle; engine-stories-index C2), and the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g). |

## 2. Declaration

Shape: the Wound spring (`elements/WoundSpring/WoundSpringElementDeclaration.cs:8`) with its sprung slider replaced by a sprung limited hinge.

- **Interfaces.** `IHingeElement` (`MechanicalContracts.cs:20`), `IMechanicalTransmissionElement` (`:90`), `ISignalElement` (`SignalContracts.cs:8`), `ISceneGeometryElement`, and the revolute hold that `ENGINE-JOINT-KINDS` delivers with its release by physical request.
- **Frame (body 0, static).** Base 1.8 × 0.15 × 0.8 m with a stop bar 0.9 m above the pivot.
- **Winding shaft (body 1).** Cylinder radius 0.26 m, width 0.1 m, mass 0.25 kg (the Wound spring's shaft); continuous hinge.
- **Arm (body 2).** Bar 1.4 × 0.08 × 0.2 m from the pivot with a cup at its tip (floor 0.8 × 0.05 × 0.8 m, 0.2 m walls on three sides); mass 0.6 kg. Limited hinge 0 to 1.31 rad (75°), measured from the fired position. **Frame:** fired (0), the arm stands 15° back from vertical against the stop bar (elevation 105° from the forward horizontal); wound 75° further back (1.31 rad) it lies horizontal to the rear.
- **Arm spring.** Constant stiffness 60 N·m/rad toward 0, damping 0.5 N·m·s/rad. Its rest position is the fired angle (hinge coordinate 0): a hinge spring with a declared rest position is `ENGINE-JOINT-KINDS`' (owner decision 6; coordinator decisions 21 and 25). Fully wound it stores ½·60·1.31² ≈ 51 J.
- **Transmission.** Shaft to arm, ratio `winding_ratio`, `TransmissionConstraintKind.NonPositiveVelocity` (winds only), as the Wound spring's (`:52`).
- **Retention and release.** Two parts, both of `ENGINE-JOINT-KINDS` (holds on a coordinate and their release; engine-stories-index C2). The hold: retention on the arm hinge (`readiness/batch-i.md` section 5 G1). The release: a physical release request bound to `ActivationIn`, as the Wound spring's is.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `winding_ratio` | Real, `ParameterUnit.None` (arm rad per shaft rad) | 0.02–0.2 | 0.05 | Reject, build-only | A default Motor (6 rad/s) winds fully in about 4.4 s |

| Port | Domain | Direction, role | Position (m) |
| --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input, DriveIn; rim 0.26 m on the shaft | (−0.6, 0.1, 0.45) |
| `ActivationIn` | Signal | Input, ActivationIn, Pulse | (0.6, 0.1, 0.45) |

- **Presentation.** Arm and shaft body bindings; latch from `CommittedRetentionReleased`; an elastic band as a span from the frame to the arm; charge marks on the base.
- **Palette.** Motion, "Elastic catapult"; order 133 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| Base and stop bar | Boxes, cream `#fff8e9` (owner decision 4) | Solid, body 0 | nothing |
| Shaft | Cylinder, navy `#1c2836` | Solid, body 1 | body pose |
| Arm and cup | Boxes, ochre `#d99e32` | Solid, body 2 | body pose |
| Band, latch | Band: a thin cord, 0.02 m, in the thin-wire class of owner decision 1 (non-solid; the hinge's declared spring carries the load), routed from the frame to the arm's underside, clear of the cup and the ball's path, which the slice confirms in its scene. Latch: a wedge | Decorative: moved by a binding | presentation binding (band span from frame to arm; latch from committed retention state) |
| Charge marks | Printed | Decorative: printed flat | nothing |

Icon: an arm with a cup swinging from a base, with an arc. It differs from EL-224 (a balanced beam and weight) by the single arm and band, and from the Wound spring by swinging.

### Behaviour and numbers

- Winding through a Mechanical link draws the arm back and the latch keeps the charge after the drive stops. A pulse releases; the spring swings the arm to the stop bar, where the arm stops and the ball leaves the open cup along the tangent to an arm 15° back from vertical: 15° above the forward horizontal for a level base.
- Hand calculation at full charge with a 1 kg ball at 1.4 m: stored ½ × 60 × 1.31² = 51.5 J; raising the ball 1.4 × sin 105° = 1.35 m takes 13.3 J and the arm's centre 0.68 m takes 4.0 J; 34.2 J remains in 2.35 kg·m² (ball 1.96, arm 0.39), so 5.4 rad/s and the tip moves at about **7.5 m/s**. Partial winding gives less (stored energy goes with the square of the wound angle).
- One cycle empties the store. Release unwound: nothing is thrown. A blocked sweep stops at the obstacle and keeps the remaining spring load against it.
- Whether a wound arm held by its latch, or a ball resting in its cup, may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| One-way winding transmission from a shaft driven through a Mechanical link, its ratio from a parameter | present | `MechanicalContractValues.cs:255`; ratio from a parameter as the Wound spring's `winding_lead` (`elements/WoundSpring/WoundSpringElementDeclaration.cs:52`–`:53`) | `WoundSpringIntegrationTests.AuthoredTeachingWindsReleasesCapturesAndResets` |
| Retention holds through reduced or reversed drive; release by pulse | present (prismatic) | `PrismaticRetentionValues.cs:13` | `WoundSpringControlTests.ChargedStoreRetainsThroughReverseOrReducedDrive` |
| Limited hinge with stops | present | `MechanicalContractValues.cs:149` | `ImpactLeverTests.AngularLimitsBoundary_StopsArrestTravelWithoutPenetration` |
| Constant axial spring on a limited hinge, its rest position at the fired angle (hinge coordinate 0) | present, untested (package level); whether a hinge spring's zero is its rest angle is not verified. If not, the declared spring rest position is `ENGINE-JOINT-KINDS`' (owner decision 6; coordinator decisions 21 and 25) | `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:681`; constant only (`CuriousContraptions.Bundle/BundleValidator.cs:749`) | Law level: `AirflowRuntimeTests.DeclaredElasticCoordinateFundsContinuousWorkWithoutAnInstantaneousDrive` |
| **Retention with a latched release on a revolute coordinate** | **missing** | retention names a prismatic, zero-rest compressive coordinate (`PrismaticRetentionValues.cs:12`) | Owner: the hold and its release by a physical request bound to `ActivationIn`, `ENGINE-JOINT-KINDS` (holds on a coordinate and their release; engine-stories-index C2) |

Counts: 3 present, 1 untested, 0 narrower, 1 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static base, a winding shaft, a limited sprung hinge carrying the arm, one one-way transmission, one retention and one release request: the Wound spring's declaration with a hinge in place of its slider. Winding is the transmission law; the swing is the hinge spring law, gravity and the hinge limit; the ball leaves by contact ending at the stop. The missing capability is stated for any package: a hold on a revolute coordinate and its release by a physical request bound to a declared signal input, owned by `ENGINE-JOINT-KINDS` (which also owns a hinge spring's declared rest position; engine-stories-index C2). No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 7 boxes (base, stop bar; arm, cup floor, 3 cup walls) and 1 cylinder (shaft) as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: the band is a presentation span and the latch a binding from committed retention state; neither carries load. Per art node: base, stop bar and charge marks are moved by nothing; the shaft by body 1's pose; arm and cup by body 2's pose; band and latch by presentation bindings.
- **Flat state.** 3 bodies; 8 colliders (7 box, 1 cylinder); 2 revolute joints; 1 transmission; 1 retention; 0 drives; 0 sensors; 1 signal node; 0 stores; 2 body-pose bindings, 1 span, 1 driver binding.
- **Performance.** Two colliders more than the Wound spring's six: the stop bar, arm and cup (six boxes) replace its two rails, sleeve and head. The base is static. Whether a wound arm may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. The cup reaches 7.5 m/s; thin obstacles rely on the engine's speculative contacts.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-397](../requirements.md#todo-397): "Distinct rotary delivery from the linear wound launcher: charged elastic arm swings a cup before releasing. Share finite-charge infrastructure, not an identical impulse with another mesh."
- **Campaign.** Chapter 5 (41–50), "hinged elastic catapult".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: release direction ±10° about the tangent at the stop; launch speed as a range 5.5–9.5 m/s at full charge (±25 % of the 7.5 m/s estimate); energy bound + 1 J; stall guard 30 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| T1 | Motor → Mechanical link winds fully; Delay → pulse; Basketball in the cup | The ball leaves at 5.5–9.5 m/s within 10° of the tangent (15° above the forward horizontal) and lands in a Receiver | Control: no pulse, the charge is held 10 s | `ElasticCatapultTests.AWoundArmThrowsOnRelease` |
| T2 | Pulse with no winding | Nothing is thrown | — | `ElasticCatapultTests.AnUnwoundReleaseThrowsNothing` |
| T3 | Winding continued past full | The arm stops at 75° and the shaft stalls | Boundary: `winding_ratio` 0.02 and 0.2 | `ElasticCatapultTests.MaximumChargeIsBounded` |
| T4 | A Wall in the sweep | The arm stops at it, no penetration | — | `ElasticCatapultTests.ABlockedSweepStops` |
| T5 | Two pulses after one winding | One throw; ball energy ≤ stored energy + 1 J | — | `ElasticCatapultTests.OneCycleEmptiesTheStore` |
| T6 | Half winding against full | The half-wound ball lands measurably shorter | — | `ElasticCatapultTests.ChargeSetsTheThrow` |
| T7 | Throw toward a Receiver riding a Conveyor | Captured only when the Delay matches the Receiver's arrival | — | `ElasticCatapultTests.ADelayedLatchHitsAMovingBasket` |
| T8 | Reset mid-wind; Save, Load | Arm at 0, uncharged; Mechanical link, wire and parameter restored | — | `ElasticCatapultTests.ResetAndReloadRestoreTheAuthoredCatapult` |
| T9 | Electrical wire into `ActivationIn`; `winding_ratio` 0.01 | Refused with a typed reason | — | `ElasticCatapultTests.WrongKindWiresAndOutOfRangeValuesAreRefused` |

**Integration.** Motor, Clutch, Delay, Conveyor, Receiver (delivered). Levels need S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `elastic_catapult`; Named identity EL-226; Acceptance todo-397 and sequence-task-380; owner slice `named-el-226`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** One element slice; deliver after EL-224 if both are queued. Prerequisites: blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP`; then `ENGINE-JOINT-KINDS` (the revolute hold, its release by physical request and the spring rest position), and the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: the `elements/ElasticCatapult/` package, `CuriousContraptions.tests/ElasticCatapultTests.cs`, and the hand-edited lists until S3. Review tier: engine. **Disqualified if** `ENGINE-JOINT-KINDS` cannot place the hinge spring's rest position at the fired angle; or a hinge spring must be parameter-bound to be retained (a parameter-bound hinge spring is `ENGINE-PARAMETER-BOUND-FRAMES`', coordinator decision 16, and this part does not plan one).

## 6. Open questions

1. *Stiffness as a parameter?* Resolved: fixed; charge is varied by how far it is wound. A hinge spring is admitted only as a constant today (`BundleValidator.cs:749`); a parameter-bound hinge spring would be `ENGINE-PARAMETER-BOUND-FRAMES`' (coordinator decision 16) and is not needed.
2. *Release angle.* Resolved by D13: fixed at the stop bar; the player aims by rotating the part in Build.

Needs the owner: nothing.
