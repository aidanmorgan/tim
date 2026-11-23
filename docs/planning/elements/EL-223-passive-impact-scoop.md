# EL-223 · Passive impact scoop — named-identity spec

> **Paused (owner decision 24g, 23 November 2026).** EL-223 is held until the engine programme lands (owner decision 22) and is blocked on the engine programme (owner decision 3a). The delivery a session had begun (worktree `tim-cluster2-after-baseball`, uncommitted) is paused; this spec is the one current text and adopts that declaration's package id, directory, display name and order. Audit finding F8 on this draft (in the [batch summary](readiness/new-identities.md), under "For other sessions") is applied in sections 2 and 5.

**Current delivery (21 November 2026):** [EL-223 ledger entry](../invest/named-elements.md#element-223), ledgered 21 November 2026; no earlier spec and no legacy to harvest. Code is cited as `path:line` in checkout `spec-drafts` at published main `25df3cd5`; abstraction files are cited by file name and live under `CuriousContraptions.Abstractions*/`. Values are f32; **proposed** values are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)). Tests are native; browser qualification is pending.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-223 · Passive impact scoop · Mechanical |
| Requirement anchor | [todo-412](../requirements.md#todo-412) (`sequence-task-377`) |
| Ledger entry / owner | [element-223](../invest/named-elements.md#element-223); slice `named-el-223`, no earlier S owner |
| Disposition | **To deliver as a single element.** Run "hinged cups" in delivery batches 1b. Not a family with EL-224: that part has a rope port, a latch and a trigger. Not a duplicate of the Impact lever: here the arriving ball is the one carried. |
| Planned package | id `passive_impact_scoop`, directory `elements/PassiveImpactScoop/` (the paused in-flight declaration) |
| Related | [CAT-034 Impact lever](CAT-034-impact_lever.md) (shape), [EL-224 Counterweighted scoop catapult](EL-224-counterweighted-scoop-catapult.md), [EL-215 Damped cushion](EL-215-damped-cushion.md) |
| Status | Paused (owner decision 24g, 23 November 2026). Blocked on the engine programme (owner decision 3a); then the coverage binding blocker (section 5) |

## 2. Declaration

Follows `elements/ImpactLever/ImpactLeverElementDeclaration.cs:12`: a static frame carrying one passive limited hinge.

- **Interfaces.** `IHingeElement` (`MechanicalContracts.cs:20`), `ISceneGeometryElement` (`SceneGeometry.cs:10`).
- **Frame (body 0, static).** Base 1.4 × 0.1 × 1.3 m and two posts 0.15 × 1.5 × 0.15 m at z = ±0.55 m carrying the pivot 1.4 m above the base. The swing passes between the posts. Restitution 0, friction 0.3.
- **Swing (body 1, dynamic).** Two arms 0.06 × 1.0 × 0.06 m from the pivot down to a shallow cup: floor 0.9 × 0.05 × 0.9 m, back wall 0.05 × 0.4 × 0.9 m, two side walls 0.9 × 0.4 × 0.05 m, a 0.05 m lip across the open front. Pivot to cup centre 1.0 m. At rest the cup floor is 0.4 m above the base, so a lesson supplies an approach deck (a ledge or a Ramp's end) at that height. Mass 0.2 kg, centre of mass 0.9 m below the pivot, inertia about the pivot 0.17 kg·m² (declared as principal moments about the centre of mass). Restitution 0.05, friction 0.5: a dead, grippy cup.
- **Hinge.** `RevoluteTravel.Limited`, −0.087 to 1.4 rad (−5° to 80°); no spring; friction torque 0.05 N·m; initial angle 0; no drive.
- **Parameters, ports, stores, signal nodes.** None.
- **Presentation.** Swing body binding only.
- **Palette.** Motion, "Passive impact scoop", order as its slice publishes (paused in-flight declaration: 223); otherwise 130 ([palette allocation](readiness/palette-allocation.md)); orders 223 and 323 stay reserved while EL-223 is paused (coordinator decisions 36 and 37).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Base and posts | Boxes, cream `#e8dccb` | Solid, colliders on body 0 |
| Arms | Boxes, navy `#1c2836` | Solid, colliders on body 1 |
| Cup floor, back, sides, lip | Boxes, sky blue `#7ebcd4`, gold lip | Solid, colliders on body 1 |
| Pivot caps | Discs flush on the posts | Decorative: printed flat |

Icon: a hanging cup on two arms with a curved up-arrow. It differs from the Impact lever (a balanced beam) by the hanging cup, and from EL-224 by having no weight.

### Behaviour and numbers

- A ball rolling in through the open front strikes the back wall and stays in the cup; the pair swings up about the pivot. Near the top of the swing the cup's open front faces down and the ball rolls out over the lip, about 0.8 m above where it entered. A mouth placed under the raised cup receives it. The empty swing falls back under gravity.
- No store, no drive, no counterweight: all the lift is the ball's own kinetic energy. For a 1 kg Basketball the capture keeps about 0.83 of its speed (ball 1 kg against 0.2 kg at the same radius); lifting 0.83 m needs 4.0 m/s after capture, so about **4.9 m/s on arrival**, the speed a ball rolling without slipping gains from a 1.7 m drop (v² = 10/7 × g × h, so h = 0.7 × 4.9² ÷ 9.81 = 1.71 m; 1.2 m is the figure for a sliding body). Below that the swing stalls and the ball rolls back out at the bottom.
- A ball wider than 0.85 m does not enter. A ball arriving from the back or the side meets a wall and is not carried.
- Supported cargo: one ball of 0.4–0.85 m; this covers every preset.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Limited passive hinge with stops | present | `MechanicalContractValues.cs:149` | `ImpactLeverTests.AngularLimitsBoundary_StopsArrestTravelWithoutPenetration` |
| Impact turning a hinged body | present | `MechanicalContracts.cs:20` | `ImpactLeverTests.AlignedBowlingBallImpact_FlingsPayloadUpward` |
| Hinge friction torque holding and resisting | present | `MechanicalContractValues.cs:158` | `RevoluteHingeFrictionTests.AHingedBarTurnsWithTheTorqueLeftAfterItsFrictionTorque` |
| Moving hinge body's colliders read from the scene | present | `ImpactLeverElementDeclaration.cs:70` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` |
| A concave compound (cup) on a hinged body carrying a ball through a swing | present, untested | same symbols | none; the slice adds `ImpactScoopTests.AFastBallIsCarriedUpAndReleased` |

Counts: 4 present, 1 untested, 0 narrower, 0 missing. No engine story beyond the programme (owner decision 3a) is needed (the ledger marks this identity "present").

## 4. Sources and legacy

- **Requirement.** [todo-412](../requirements.md#todo-412): "Arriving ball's momentum rotates a shallow hinged cup and carries the same ball upward before release. No raised counterweight or automatic boost; distinct from the counterweighted launcher above."
- **Campaign.** Chapter 5 (41–50), "passive impact scoop"; coverage row "… passive scoop, counterweighted scoop, …".
- **Legacy.** None.

## 5. Acceptance outline

Tolerances: heights ±0.05 m (the release height is asserted as a floor of 0.6 m above entry, against 0.8 m expected); speeds set by the release height of a Ramp, not asserted; "same ball" is the body identity, exact; stall guard 15 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| P1 | Basketball arriving at about 5.5 m/s from a Ramp run-up | The same ball rises at least 0.6 m above its entry height and is captured by a Receiver set under the raised cup | Control: no run-up (1 m/s), it is not lifted | `ImpactScoopTests.AFastBallIsCarriedUpAndReleased` |
| P2 | Arrival at about 3 m/s | The swing stalls below 60° and the ball leaves at the bottom (capture keeps 0.83 of the speed, about 2.5 m/s, a rise of 2.5² ÷ (2 × 9.81) = 0.32 m, which on the 1.0 m arm is acos(1 − 0.32) = 47°) | Boundary: the pair 3 m/s and 5.5 m/s brackets the minimum; the minimum itself is not pinned | `ImpactScoopTests.BelowTheMinimumEnergyTheSwingStalls` |
| P3 | Arrival from the back, from the side, and 0.5 m off centre in depth | The ball is not carried | Control: centred front arrival | `ImpactScoopTests.WrongSideAndOffCentreEntriesAreNotCarried` |
| P4 | A 0.9 m ball (Tennis ball with `radius` 0.45) | It does not enter | Boundary: a 0.8 m ball enters | `ImpactScoopTests.AnOversizeBallDoesNotEnter` |
| P5 | Two balls arriving 0.2 s apart | The first is carried; the second is not duplicated, deleted or moved without contact | — | `ImpactScoopTests.SimultaneousArrivalsStayPhysical` |
| P6 | Any run | The ball's position changes by less than 0.15 m between consecutive ticks (no teleport between mouths) and its height gain never exceeds its arrival kinetic energy ÷ (m g) + 0.05 m, the kinetic energy being translational plus rotational | — | `ImpactScoopTests.TheLiftIsPaidByTheArrival` |
| P7 | Ramp → scoop → elevated Pipe → Trampoline | The ball reaches the pipe's far end | Control: scoop removed | `ImpactScoopTests.ARunUpFeedsAnElevatedPipe` |
| P8 | Run, Reset; Save, Load | Swing at rest angle; second Run repeats P1 | — | `ImpactScoopTests.ResetAndReloadRestoreTheSwing` |

No ports, so wrong-kind connection refusal does not apply; no parameters, so a forged parameter in a save is refused (asserted in P8). The requirement's cushion collaborator (EL-215) is undelivered; P7 uses the Trampoline and is repeated with the cushion in its slice.

**Integration.** Ramp, Pipe, Receiver, Trampoline (delivered); EL-215 cushion (later). Coverage binding: as the delivered Baseball does it on main `25df3cd5` (`tools/Coverage/NamedPackageSources.cs`), the delivering slice adds a "Package source binding" section to this spec (Package `passive_impact_scoop`; Named identity EL-223; Acceptance todo-412 and sequence-task-377; Decision; Mode records `Configuration=Fixed`). It is not added now, because the reader parses every EL spec that carries that heading. **Blocker:** the reader requires an S owner in the ledger entry and EL-223 has none ([batch summary](readiness/new-identities.md)). A level that places it also needs S8.

**Delivery.** One element slice, paused (owner decision 24g). Prerequisites: blocked on the engine programme (owner decision 3a); the binding blocker; S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: `elements/PassiveImpactScoop/` (project, declaration and `.uid`, `passive_impact_scoop.tscn`, icon and import), `CuriousContraptions.tests/ImpactScoopTests.cs`, and the usual hand-edited lists until S3. Review tier: engine (one untested capability). **Disqualified if** the ball tunnels through or sticks in the cup during the swing, or cannot leave it at the top; the cup's wall heights and the 80° stop are the tuning values, and the outcome rows above do not change.

## 6. Open questions

1. *Release angle as a parameter?* Resolved: fixed. Hinge limits are constants in the declaration (`MechanicalContractValues.cs:153`), and D13 fixes size per kind.
2. *Exact minimum approach speed.* Resolved by D1: about 4.9 m/s is the starting estimate; P1 and P2 bracket it.

Needs the owner: nothing.
