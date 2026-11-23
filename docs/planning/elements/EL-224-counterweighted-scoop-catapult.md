# EL-224 · Counterweighted scoop catapult — named-identity spec

**Current delivery (21 November 2026):** [EL-224 ledger entry](../invest/named-elements.md#element-224), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending. Propagated 23 November 2026 to the [element-spec decisions of 22 November 2026](readiness/owner-decisions-2026-11-22.md).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-224 · Counterweighted scoop catapult · Mechanical |
| Requirement anchor | [todo-395](../requirements.md#todo-395) (`sequence-task-378`) |
| Ledger entry / owner | [element-224](../invest/named-elements.md#element-224); slice `named-el-224`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a) and `ENGINE-JOINT-KINDS` (the latch's hold on the hinge and its release by a physical request bound to `ActivationIn`; engine-stories-index C2).** Delivery batches lists it in run "hinged cups" (1b); the latch moves it to 1c. Not a duplicate of the Impact lever: it adds a cup, a latch and a release input. |
| Planned package | id `scoop_catapult`, directory `elements/ScoopCatapult/` |
| Related | [CAT-034 Impact lever](CAT-034-impact_lever.md) (shape and rope ports), [CAT-067 Weight](CAT-067-weight.md), [EL-063 Cable winch](EL-063-cable-winch.md), [EL-014 Water-carrying bucket](EL-014-water-carrying-bucket.md), [EL-226 Hinged elastic catapult](EL-226-hinged-elastic-catapult.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP` (the armed beam held at rest with its Weight hanging); then `ENGINE-JOINT-KINDS` (the hold on the revolute coordinate and its release by a physical request bound to `ActivationIn`; engine-stories-index C2), and the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g). |

## 2. Declaration

Follows `elements/ImpactLever/ImpactLeverElementDeclaration.cs:12`: a static stand, a limited hinge and rope ports on the beam.

- **Interfaces.** `IHingeElement` (`MechanicalContracts.cs:20`), `ISignalElement` (`SignalContracts.cs:8`), `ISceneGeometryElement` (`SceneGeometry.cs:10`), and the revolute hold that `ENGINE-JOINT-KINDS` delivers with its release by physical request (today's `IPrismaticRetentionElement`, `:85`, is prismatic only).
- **Stand (body 0, static).** Base 1.6 × 0.1 × 0.8 m and a fulcrum post 1.2 m tall.
- **Beam (body 1).** Bar 2.4 × 0.1 × 0.3 m pivoted 0.8 m from the weight end, with a cup at the long end: floor 0.8 × 0.05 × 0.8 m and 0.2 m walls on three sides, open toward the throw. Mass 1 kg; centre of mass 0.45 m toward the cup, so the unloaded beam rests armed, cup down.
- **Hinge.** Limited, −0.61 to 0.96 rad (armed −35°, fired +55°); no spring; friction torque 0.5 N·m; no drive.
- **Latch.** Two parts, both of `ENGINE-JOINT-KINDS` (holds on a coordinate and their release; engine-stories-index C2). The hold: a retention on the hinge that holds the armed angle against the weight and re-engages whenever the beam returns to it (`readiness/batch-i.md` section 5 G1). The release: a `SignalPhysicalRequestDescriptor` with `PhysicalRequestOperation.Release` (`SignalContractValues.cs:65`) bound to `ActivationIn`, as the Wound spring's is (`elements/WoundSpring/WoundSpringElementDeclaration.cs:66`).
- **Parameters, stores.** None. The energy is the attached load's fall.

| Port | Domain | Direction, role | Position (m, on the beam) |
| --- | --- | --- | --- |
| `TieA` | Rope | Bidirectional, TieA, load attachment | Weight end (−0.8, −0.05, 0) |
| `TieB` | Rope | Bidirectional, TieB, load attachment | Cup end (1.6, −0.05, 0) |
| `ActivationIn` | Signal | Input, ActivationIn, Pulse | On the stand (0, 0.3, 0.4) |

Rope ports are declared `PortDirection.Bidirectional` as on every delivered rope part (`CuriousContraptions.Abstractions/PortDescriptor.cs:9`; `elements/ImpactLever/ImpactLeverElementDeclaration.cs:44`).

- **Presentation.** Beam body binding; latch pawl rotation from `CommittedRetentionReleased`.
- **Palette.** Motion, "Scoop catapult"; order 131 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| Base and post | Boxes, cream `#fff8e9` (owner decision 4) | Solid, body 0 | nothing |
| Beam and cup | Boxes, sky blue `#7ebcd4`, gold cup rim | Solid, body 1 | body pose |
| Latch pawl | Small wedge inside the post's outline | Decorative: moved by a binding | presentation binding (rotation) |
| Rope eyes | Rings flush in the beam ends | Decorative: printed flat | body pose (with the beam) |

Icon: a tilted beam with a cup, a hanging weight and a trajectory arc. It differs from the Impact lever by the cup and from EL-223 by the weight.

### Behaviour and numbers

1. Armed: a Weight hangs by rope from `TieA`, held up by the latch; a ball sits in the cup.
2. A pulse releases the latch. With an 8 kg Weight the short arm gets 63 N·m against 16 N·m from a 1 kg ball; the weight end falls 1.1 m, the cup end rises 2.2 m.
3. Energy (hand calculation): 8 kg × 9.81 × 1.11 m = 87 J released; lifting the ball 2.2 m takes 21.6 J and the beam's centre of mass 0.63 m takes 6.2 J, leaving 59 J in 8.4 kg·m² about the pivot (weight 8 × 0.8² = 5.1, ball 1 × 1.6² = 2.6, beam 0.7): 3.75 rad/s, so the ball leaves at about **6 m/s** when the beam meets its fired stop, on a free trajectory. It is the same ball. 8 kg is the Weight's maximum (`elements/Weight/WeightElementDeclaration.cs:33`), so the principal row runs at that limit.
4. Spent: beam at the fired stop, weight down. To re-arm, the cup end must be pulled down by real work, which raises the weight again. With delivered parts: a 6 kg Weight held on a ledge, its rope to `TieB` just taut after the throw, is pushed off by a Linear pusher. Its 94 N·m at the cup end (6 × 9.81 × 1.6) beats the 63 N·m of the hanging 8 kg Weight, the cup end comes down, the 8 kg Weight rises and the latch re-engages. **The re-arming rope must then be cut** (or paid out by a winch, EL-063, later): left on `TieB` it holds the cup down and the next pulse throws nothing.
5. Inadequate counterweight (a 1 kg Weight, 7.8 N·m): the beam does not rise. Released empty: the beam swings and nothing is thrown.
6. **Ropes.** Ropes are solid strands under owner decision 1 once `ENGINE-CONNECTION-MATERIALS` (solid strand) is delivered; the delivered rope draws without a collider, a recorded visible-solid defect owned by that story. Every rope route in these rows runs clear of other colliders, so the rows hold either way: both ropes hang straight down from the beam's underside at its ends, the `TieA` rope to the counterweight and the `TieB` rope to the ledge Weight, and the scenarios hang the counterweight with more than its 1.1 m fall clear below it.
7. Whether the armed beam, its hanging Weight or a seated ball may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Limited hinge with stops | present | `MechanicalContractValues.cs:149` | `ImpactLeverTests.AngularLimitsBoundary_StopsArrestTravelWithoutPenetration` |
| Rope load ports on a hinged body; a falling weight lifts the arm | present | `ImpactLeverElementDeclaration.cs:41` | `ImpactLeverTests.RopeLiftingIntegration_FallingWeightLiftsArm` |
| A beam flinging a payload | present | same | `ImpactLeverTests.AlignedBowlingBallImpact_FlingsPayloadUpward` |
| Release request routed from a pulse | present | `SignalContractValues.cs:65` | `RetentionRequestTests.SimultaneousSourcesCoalesceByOccurrenceAndExactTargetThenExpire` |
| Rope cut and Reset of a rope | present | rope domain | `RopeTests.CutWaitsForCommittedStepAndSurvivesDiscardThenResetRestoresTheRope` |
| **Retention with a latched release on a revolute coordinate** | **missing** | `PrismaticRetentionDescriptor` is prismatic and needs a winding transmission (`PrismaticRetentionValues.cs:13`) | Owner: the hold and its release by a physical request bound to `ActivationIn`, `ENGINE-JOINT-KINDS` (holds on a coordinate and their release; engine-stories-index C2) |

Counts: 5 present, 0 untested, 0 narrower, 1 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static stand, one limited hinge, two rope ports, one retention and one release request. The throw is the rope tension law, gravity and the hinge limit; the ball leaves by rigid contact ending at the stop; holding and letting go are the retention law. The missing capability is stated for any package: a hold on a revolute coordinate that is loaded by a rope and not wound, and its release by a physical request bound to a declared signal input, owned by `ENGINE-JOINT-KINDS` (engine-stories-index C2). No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 7 boxes (base, post; beam, cup floor, 3 cup walls) as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: the latch pawl turns on a binding from the committed retention state; it holds nothing (the retention does). Per art node: base and post are moved by nothing; beam, cup and rope eyes by body 1's pose; the latch pawl by a presentation binding.
- **Flat state.** 2 bodies; 7 box colliders (0 of any other shape); 1 revolute joint; 1 retention; 0 drives; 0 sensors; 1 signal node; 0 stores; 1 body-pose and 1 driver binding.
- **Performance.** Two colliders more than the Impact lever's five: the cup adds four (floor and three walls) and the stand has no stop blocks, the hinge limits being the stops. The stand is static. Whether the armed beam may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. The cup end reaches 6 m/s; thin obstacles rely on the engine's speculative contacts.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-395](../requirements.md#todo-395): "Raised physical weight drives a loaded hinged cup; same ball leaves on a free trajectory. Rope/winch must raise the weight again. Show falling weight, swept arm and spent state."
- **Campaign.** Chapter 5 (41–50), "counterweighted scoop".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: launch speed as a range 4–8 m/s around the 6 m/s estimate (a third either way still clears a 1.5 m gap); angles ±0.05 rad; energy bound + 1 J; stall guard 20 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| W1 | 8 kg Weight (the Weight's maximum) on `TieA`, Basketball in the cup, one pulse | The weight falls, the beam reaches the fired stop and the same ball leaves at 4–8 m/s and lands in an upper Pipe mouth | Control: no pulse, nothing moves for 10 s | `ScoopCatapultTests.AReleasedWeightThrowsTheSeatedBall` |
| W2 | 1 kg Weight | The beam does not reach the fired stop; the ball stays in the cup | Boundary: 4 kg throws it | `ScoopCatapultTests.AnInadequateCounterweightDoesNotThrow` |
| W3 | Ball placed beside the cup | Not thrown | — | `ScoopCatapultTests.AMissedCupThrowsNothing` |
| W4 | A Wall over the arm | The beam stops at it, no penetration; the ball stays or rolls out physically | — | `ScoopCatapultTests.ABlockedArmStopsPhysically` |
| W5 | Before any pulse, for 10 s, with the weight hanging | The beam stays within 0.05 rad of armed (no premature release) | — | `ScoopCatapultTests.TheLatchHoldsUntilReleased` |
| W6 | Any throw | Ball kinetic plus potential energy gain ≤ the weight's potential energy loss + 1 J | — | `ScoopCatapultTests.TheThrowIsPaidByTheFallingWeight` |
| W7 | After a throw, a second pulse; then a 6 kg Weight pushed off its ledge pulls `TieB` down until the latch re-engages; its rope is cut; a third pulse | The second pulse throws nothing; the 8 kg Weight is back at its raised height within 0.05 m; the third pulse throws | Control: third pulse with the `TieB` rope not cut, nothing is thrown | `ScoopCatapultTests.ItMustBeRearmedByRaisingTheWeight` |
| W8 | Run, Reset; Save, Load | Beam armed, ropes and wire restored | — | `ScoopCatapultTests.ResetAndReloadRestoreTheArmedCatapult` |
| W9 | Electrical wire into `ActivationIn`; a wire into `TieA` | Refused with a typed reason | — | `ScoopCatapultTests.WrongKindConnectionsAreRefused` |

The requirement's "water-filled bucket" drive is EL-014 after `ENGINE-LIQUID`; it attaches to `TieA` like the Weight and is a later-partner row in that slice.

**Integration.** Weight, Rope anchor, Pulley, Linear pusher, Impact switch, Pipe (delivered); EL-063 winch (its winding clause is `ENGINE-TENSILE-EXTENSIONS`'), EL-014 bucket (later). Levels need S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `scoop_catapult`; Named identity EL-224; Acceptance todo-395 and sequence-task-378; owner slice `named-el-224`; Mode records `Configuration=Fixed`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** One element slice. Prerequisites: blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP`; then `ENGINE-JOINT-KINDS` (the revolute hold and its release by physical request), and the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: the `elements/ScoopCatapult/` package, `CuriousContraptions.tests/ScoopCatapultTests.cs`, and the hand-edited lists until S3. Review tier: engine if this is the first consumer of the revolute hold. **Disqualified if** `ENGINE-JOINT-KINDS`' hold needs a winding transmission, as today's prismatic retention does; this latch is loaded by a rope, not wound.

## 6. Open questions

1. *Built-in counterweight or an attached load?* Resolved under the standing delegation: attached, on a rope port. The requirement's lesson drives it with a filling bucket, and the Weight already exists.
2. *How is it re-armed?* Resolved (audit F3): by a second load on `TieB` that pulls the cup end down and raises the weight, after which that rope is cut or paid out. A load left on `TieB` holds the cup down; that is asserted as a control.

Needs the owner: nothing.
