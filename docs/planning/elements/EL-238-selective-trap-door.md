# EL-238 · Selective trap door — named-identity spec

**Current delivery (21 November 2026):** [EL-238 ledger entry](../invest/named-elements.md#element-238), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending. Repaired 23 November 2026 against the owner decisions of 22 November 2026 ([readiness/owner-decisions-2026-11-22.md](readiness/owner-decisions-2026-11-22.md)).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-238 · Selective trap door · Mechanical |
| Requirement anchor | [todo-220](../requirements.md#todo-220) (`sequence-task-354`) |
| Ledger entry / owner | [element-238](../invest/named-elements.md#element-238); owner **S391** (selection rule); slice `named-el-238` |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a).** Run "selective passage" in delivery batches 1b. Separate from [EL-109 Speed-sensitive trapdoor](EL-109-speed-trapdoor.md) (selects by speed with a sensor), [EL-058 Size grate](EL-058-size-grate.md) (size) and [EL-059 Weight tray](EL-059-weight-tray.md) (reports a weight as a signal; it does not divert). |
| Planned package | id `trap_door`, directory `elements/TrapDoor/` |
| Related | [CAT-034 Impact lever](CAT-034-impact_lever.md) (shape: a limited hinge with a parameter-bound mass) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP` (the closed door rests on its stop between bodies) |

## 2. Declaration

**S391 selection rule, decided here under D16:** the door selects by **weight**. It is a counterweighted flap: a light body rolls across; a heavy body tips it and falls through. Reason: the requirement itself says to test "heavy/light bodies" and warns against assuming a size filter; weight needs no sensor or power; and speed, size and material already have their own identities.

Follows `elements/ImpactLever/ImpactLeverElementDeclaration.cs:12`.

- **Interfaces.** `IHingeElement` (`MechanicalContracts.cs:20`), `ISceneGeometryElement` (`SceneGeometry.cs:10`).
- **Frame (body 0, static).** Two sills 0.3 × 0.1 × 1.0 m, 1.2 m apart, level with the closed door, joined by side rails 0.08 m wide and 0.15 m tall so a ball crossing cannot leave sideways; a closed-position stop under the free edge. The entry sill is a 0.1 m bridge with clear space beneath it for the counterweight.
- **Door (body 1).** Plate 1.2 × 0.06 × 0.9 m hinged at the entry sill, with a counterweight block 0.3 × 0.2 × 0.9 m hung 0.3 m behind and 0.25 m below the hinge line, under the entry sill and out of the lane. Mass is the `counterweight` parameter (as the lever's `beam_mass`, `ImpactLeverElementDeclaration.cs:110`); the centre of mass is declared constant at (−0.3, −0.25) m from the hinge; inertia scales with the mass. The closed-door threshold is unchanged: the closing torque depends on the 0.3 m horizontal offset only.
- **Hinge.** Limited, −1.4 rad (open, hanging 80° down) to 0 (closed against the stop); no spring; friction torque 0.3 N·m; no drive.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `counterweight` | Real, kg | 1–12 | 6 | Reject, build-only | Closing torque is counterweight × g × 0.3 m. A body at the free edge (1.1 m out) tips the door when its mass exceeds 0.27 × counterweight: 1.64 kg at the default, so the Basketball (1 kg) crosses and the Bowling ball (4 kg) drops |

- **Ports, stores, signal nodes.** None.
- **Presentation.** Door body binding; a scale mark on the counterweight from `counterweight`.
- **Palette.** Motion, "Trap door"; order 120 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Sills, rails, stop | Boxes, cream `#fff8e9` (owner decision 4) | Solid, body 0 |
| Door plate | Box, ochre `#d99e32` with navy hazard stripes printed on | Solid, body 1 |
| Counterweight block and arm | Boxes, navy `#1c2836` | Solid, body 1 |
| Scale mark | Printed | Decorative: printed flat |

Icon: a flap hinged at one side, hanging half open, with a small weight behind the hinge. It differs from EL-109 (a door with a sensor eye) by the counterweight.

### Behaviour and numbers

| Ball | Mass | Torque at the free edge | Default door (17.7 N·m to open) |
| --- | --- | --- | --- |
| Baseball (the Baseball repair's 0.15 kg, radius 0.26 m, coordinator decision 36; published main declares 0.65 kg until the repair publishes, 0.65 × 9.81 × 1.1 = 7.0 N·m, which also crosses) | 0.15 kg | 0.15 × 9.81 × 1.1 = 1.6 N·m | crosses |
| Tennis ball | 0.35 kg | 3.8 N·m | crosses |
| Basketball | 1 kg | 10.8 N·m | crosses |
| Bowling ball | 4 kg | 43 N·m | drops |
| Cannonball (planned) | 6 kg | 65 N·m | drops |

- The door starts to open only when the body has rolled far enough out for its torque to exceed the closing torque, so a heavy body drops near the middle of the opening, not at the hinge.
- When the body has gone the counterweight closes the door again (closing torque 17.7 N·m against 0.3 N·m of friction). It is reusable within a Run.
- **The threshold is static.** A fast body can cross above it: the door needs time to turn. A 2.0 kg ball starts to tip the default door only 0.92 m out (17.7 ÷ (2 × 9.81)); at 1 m/s it has 0.29 s left, the door turns about 2° and the ball crosses. Lessons and tests therefore feed the door slowly.
- Two light bodies on the door at once add their torques and may open it together; that is physical and is a lesson, not a defect.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Limited hinge with stops | present | `MechanicalContractValues.cs:149` | `ImpactLeverTests.AngularLimitsBoundary_StopsArrestTravelWithoutPenetration` |
| A load turning a hinged body only above a torque threshold | present | `MechanicalContractValues.cs:158` | `ImpactLeverTests.ASmallLoadIsHeldBelowTheFrictionTorqueAndTipsTheBeamAboveIt` |
| Hinged-body mass bound to a parameter, with range refusal | present | `ImpactLeverElementDeclaration.cs:110` | `ImpactLeverTests.OutOfRangeBeamMass_IsRefused` |
| A ball rolling across a hinged plate with moving collision geometry | present | contact solver | `ImpactLeverTests.UnequalBallLoads_AreHeldByThePivotFriction` |
| A hinged body whose declared centre of mass is offset from its hinge, held closed by gravity | present, untested | `MassVector.Constant` (`MassFrameValues.cs`); every delivered hinged body is balanced | none; the slice adds `TrapDoorTests.TheCounterweightHoldsTheDoorShut` |

Counts: 4 present, 1 untested, 0 narrower, 0 missing. No engine story beyond the programme (owner decision 3a) is named.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static frame and one limited passive hinge whose body has a declared mass and an offset centre of mass. Selection is the balance of gravity torques about the hinge with hinge friction; dropping and crossing are rigid contact on a moving plate; closing again is gravity on the counterweight. Nothing says "if the ball is heavy, open": the door has no sensor and no rule. No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 5 boxes on body 0 (2 sills, 2 rails, stop) and 3 on the door (plate, counterweight block, arm), as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: none beyond the body pose; the scale mark follows `counterweight`. What moves each art node: sills, rails and stop, nothing (static body 0); door plate with its printed stripes, counterweight block and arm, body 1's pose; scale mark, printed on the counterweight and set by the `counterweight` parameter binding.
- **Flat state.** 2 bodies; 8 box colliders; 1 revolute joint; 0 drives; 0 sensors; 0 nodes; 0 stores; 1 body-pose and 1 parameter binding.
- **Performance.** Five colliders more than the Impact lever: sills and rails that guide the ball and the counterweight a ball could meet from below. The frame is a static body. Whether the closed door resting on its stop may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-220](../requirements.md#todo-220): "Add a hinged/selective passage. Verify its selection rule rather than guessing 'size filter'; test thresholds, heavy/light bodies and moving collision geometry."
- **Campaign.** Chapter 10 "Oddly Satisfying" (91–100), "speed-sensitive/selective trapdoor"; coverage row "… spiral delay, airlift and trapdoor variants", specialist routes 91–100. Owner decision 24e moves the first use to level **28c** in chapter 3 ([per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)).
- **Legacy.** None. The original game's rule is not used as a source (D15).
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: door angle ±0.05 rad; outcomes are captures in one of two Receivers (exact); stall guard 20 s. The threshold is tested by bracketing, with a Tennis ball whose `mass` parameter is set either side of it.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| D1 | Basketball and Bowling ball released in turn from rest on a 5° Ramp (under 1 m/s at the hinge), a Receiver beyond the door and one below | The Basketball reaches the far Receiver; the Bowling ball reaches the lower one | Control: door replaced by a Wall section, both cross | `TrapDoorTests.LightCrossesAndHeavyDrops` |
| D2 | Balls released from rest on a 5° Ramp (under 1 m/s at the hinge): 1.2 kg and 3 kg at the default (threshold 1.64 kg) | The 1.2 kg ball crosses, the 3 kg ball drops | Boundary: `counterweight` 1 and 12 move the threshold to 0.27 kg and 3.3 kg, bracketed at 0.2 and 0.6 kg, and at 2.4 and 6 kg | `TrapDoorTests.TheThresholdFollowsTheCounterweight` |
| D3 | No load for 10 s | The door stays within 0.05 rad of closed | — | `TrapDoorTests.TheCounterweightHoldsTheDoorShut` |
| D4 | A heavy ball, then a light one 3 s later | The door has closed again and the light ball crosses | — | `TrapDoorTests.ItClosesAgainAfterAHeavyBody` |
| D5 | A ball dropped onto the opening door; a ball arriving while the door swings back | No penetration of the plate; each ball ends in a Receiver | — | `TrapDoorTests.MovingDoorGeometryStaysSolid` |
| D6 | A same-size pair: Tennis ball with `mass` 0.35 and with `mass` 4, both radius 0.25 m | Only the heavy one drops: the rule is weight, not size | — | `TrapDoorTests.SelectionIsByWeightNotSize` |
| D7 | Run, Reset; Save, Load | Door closed, `counterweight` restored | — | `TrapDoorTests.ResetAndReloadRestoreTheAuthoredDoor` |
| D8 | `counterweight` 0.9, 12.1, NaN | Refused with a typed reason | — | `TrapDoorTests.OutOfRangeCounterweightIsRefused` |

No ports, so wrong-kind connection refusal does not apply.

**Integration.** Ramp, Conveyor, Receiver, Pipe, every ball (delivered); EL-058, EL-059, EL-060, EL-109 as the other sorters (later). Levels need S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `trap_door`; Named identity EL-238; Acceptance todo-220 and sequence-task-354; Decision S391; Mode records `Configuration=Adjustable`. Its S owner anchor S391 is resolvable on main (`docs/planning/invest/scope-corrections.md`), and binding still waits for this identity's slice.

**Delivery.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP`. Other prerequisites: S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: the `elements/TrapDoor/` package, `CuriousContraptions.tests/TrapDoorTests.cs`, and the hand-edited lists until S3. Review tier: engine (one untested capability). **Disqualified if** an offset centre of mass on a hinged body is not honoured by the solver (CAT-023 records "offset centre of mass open" for the Domino).

## 6. Open questions

1. *S391 selection rule.* Resolved above under D16: weight.
2. *A latch that keeps it open after one heavy body?* Resolved by D19: no; no lesson asks for it.
3. *A declared spring rest position?* Coordinator decision 25, 22 November 2026, was checked and does not apply: the door has no spring; the counterweight closes it.

Needs the owner: nothing. For the owner's information: the selection rule was decided by game intent, not by checking the 1994 game.
