# EL-235 · Jack-in-the-box — named-identity spec

**Current delivery (22 November 2026):** [EL-235 ledger entry](../invest/named-elements.md#element-235), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending. Reworked 22 November 2026 after the independent audit (`docs/planning/elements/readiness/audits/elements-new-identities.md`, F2): box, head, lid and stroke now close, and self-release is removed. Repaired 23 November 2026 against the owner decisions of 22 November 2026 ([readiness/owner-decisions-2026-11-22.md](readiness/owner-decisions-2026-11-22.md)), and again the same day after the second audit (`docs/planning/elements/readiness/audits/reaudit-group-1.md`, EL-235): the unwound release answers Released (J2), the head's and lid's rest is stated for the authored pose and for Run (item 1), the ball seats at 0.77 m and J6 pulses on the next tick.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-235 · Jack-in-the-box · Mechanical |
| Requirement anchor | [todo-195](../requirements.md#todo-195) (`sequence-task-381`) |
| Ledger entry / owner | [element-235](../invest/named-elements.md#element-235); slice `named-el-235`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a) and `ENGINE-JOINT-KINDS` (re-arm at rest within a declared distance, coordinator decision 31).** Run "timed and stored drives" in delivery batches 1b. Checked against Duplicate closure with the Wound spring (CAT-071), whose engine capabilities it reuses: **not** a duplicate, because it adds a hinged lid that carries the payload and is thrown open by the head. |
| Planned package | id `jack_in_the_box`, directory `elements/JackInTheBox/` |
| Related | [CAT-071 Wound spring](CAT-071-wound_spring.md) (shape), [EL-226 Hinged elastic catapult](EL-226-hinged-elastic-catapult.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP` (the wound head and the lid rest for long spells); then `ENGINE-JOINT-KINDS` (a released retention re-arming at rest within a declared re-arm distance, coordinator decision 31, 23 November 2026). The one untested row, whether two moving bodies of one package collide, stays a stated check (section 3) |

## 2. Declaration

Follows `elements/WoundSpring/WoundSpringElementDeclaration.cs:8` and adds one passive hinge.

- **Interfaces.** `ISliderElement`, `IHingeElement`, `IMechanicalTransmissionElement`, `IPrismaticRetentionElement` (`MechanicalContracts.cs:15`, `:20`, `:90`, `:85`), `ISignalElement` (`SignalContracts.cs:8`), `ISceneGeometryElement` (`SceneGeometry.cs:10`). `ISupplyElement` is not used.
- **Box (body 0, static).** Open-topped box 1.0 × 1.3 × 1.0 m, walls and floor 0.06 m (inner depth 1.24 m). Restitution 0, friction 0.3.
- **Head (body 1).** Sphere radius 0.3 m; mass 0.5 kg. Slider along +Y at the box centre, limits −`stroke` to 0, spring `stiffness` toward 0, no drive (`WoundSpringElementDeclaration.cs:30`). **At travel 0 the head's centre is level with the rim, so its top stands 0.3 m proud of the box.** Fully wound (−0.6 m) its top is 0.3 m below the rim and its underside 0.9 m below it, leaving 0.34 m above the floor for the coil.
- **Winding shaft (body 2).** Cylinder radius 0.26 m, width 0.1 m, mass 0.25 kg, on the outside of one box wall; continuous hinge. The crank handle is drawn on its face.
- **Lid (body 3).** Plate 1.0 × 0.05 × 1.0 m hinged along the back top edge, with a 0.05 m raised lip along its front and two side edges; mass 0.2 kg. Limited hinge 0 (closed, on the rim) to 1.92 rad (110°); friction torque 0.05 N·m; no spring.
- **Transmission and retention.** As the Wound spring: shaft to slider at `winding_lead`, `TransmissionConstraintKind.NonPositiveVelocity`; retention on the slider; a release request bound to `ActivationIn`. This is the delivered prismatic retention on a coordinate that a transmission winds, released by its own pulse; it needs no new hold or release capability. **Re-arm distance 0.1 m, re-arm rest speed v_r 0.05 m/s and settle time t_s 0.5 s** (declared values of `ENGINE-JOINT-KINDS`' re-arm at rest: the rule is coordinator decisions 33 and 34, the values coordinator decision 37, 23 November 2026): a released head re-arms, and its winding transmission re-engages, when it comes to rest (its speed below v_r for t_s) within 0.1 m of its hold position, travel 0, not only within the solver slop of travel 0 (coordinator decision 31, 23 November 2026; `ENGINE-JOINT-KINDS`). 0.1 m exceeds the largest rest it must catch, the 0.042 m sag under head and lid (item 1), by 0.058 m, and stays below the 0.3 m of winding that closes the lid, so a wound head is never taken for one at rest near its hold.
- **No self-release.** The latch releases on a pulse only (D19). An endpoint closes a supply contact, not a signal pulse (`SignalContactControl.cs:13`), so release at full winding is not declarable and is not part of this element.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `stiffness` | Real, N/m | 40–240 | 160 | Reject, build-only | The Wound spring's range and default |
| `stroke` | Real, m | 0.4–0.6 | 0.6 | Reject | The floor keeps the wound head at least 0.1 m under the closed lid (the head is 0.3 m proud unwound); the ceiling is the box depth. 28.8 J at the defaults |
| `winding_lead` | Real, m/rad | 0.025–0.2 | 0.1 | Reject | The Wound spring's. At the defaults full winding needs 160 × 0.6 × 0.1 = 9.6 N·m, under a default Motor's 20 N·m, and takes 6 rad (1 s at 6 rad/s) |

| Port | Domain | Direction, role | Position (m) |
| --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input, DriveIn; rim 0.26 m on the shaft | (−0.56, 0, 0.5) |
| `ActivationIn` | Signal | Input, ActivationIn, Pulse | (0.56, 0, 0.5) |

- **Presentation.** Head, shaft and lid body bindings; coil as a span under the head; latch from `CommittedRetentionReleased`; winding marks printed.
- **Palette.** Motion, "Jack-in-the-box"; order 129 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| Box walls and floor | Boxes, coral `#d96b58` with gold corner bands printed on | Solid, five box colliders on body 0 | nothing |
| Lid and its lips | Plate and three strips, navy `#1c2836` | Solid, four box colliders on body 3 | body pose |
| Head | Sphere 0.3 m, cream, face printed on | Solid, sphere collider on body 1 | body pose |
| Shaft and crank handle | Cylinder; handle drawn within the cylinder's outline | Solid, cylinder collider on body 2 | body pose |
| Coil | Spring under the head, inside the box | Decorative: enclosed | presentation span |
| Latch | Wedge inside the wall outline | Decorative: enclosed | presentation binding |

Icon: a box with a lid flung open and a head on a coil. It differs from the Wound spring by the box and lid.

### Behaviour and numbers

1. **At rest.** *Authored (Build and Reset):* the head at travel 0, its centre level with the rim, and the lid resting open on it with its underside tangent to the head; the hinge axis is the rim's top back edge, 0.5 m behind the head's axis, so sin φ = 0.3 ÷ 0.5 and the lid lies at **0.64 rad (37°)**. *In Run:* head weight (4.9 N) and the lid's share (about 1.8 N: the lid's 1.96 N weight at about 0.44 m from the hinge, carried by the head about 0.41 m along the lid) give a static sag of about 6.65 ÷ 160 = 0.042 m. The hold keeps the deepest point of the head's first drop from travel 0 (the retained bound follows inward travel and is the slider's upper stop, `CuriousContraptions.Simulation/SimPrismaticRetention.cs:78`, `PrismaticConstraintSolver.cs:78`), and the slider is undamped, so the head is held near twice the sag, about **0.083 m**, with ½ × 160 × 0.083² = 0.55 J stored (at most 2 × 6.65² ÷ 160 = 0.55 J). With the head's centre 0.083 m below the rim the hinge-to-centre line is √(0.5² + 0.083²) = 0.507 m long and 9.4° below the rim plane, so the lid lies at asin(0.3 ÷ 0.507) − 9.4° = 36.3° − 9.4° = **0.47 rad (27°)**; at the bare static sag it would be 0.56 rad.
2. **Winding.** A Mechanical link on `DriveIn` draws the head down. The lid follows it by gravity and closes on the rim when the head's top passes the rim (travel −0.3 m); winding continues to −`stroke`. The latch keeps the charge when the drive stops.
3. **Loading.** A ball rolls onto the closed lid from the hinge side and stops against the 0.05 m front lip. A Basketball (r 0.34 m) touches the lip's top inner corner (0.95 m from the hinge) √(0.34² − 0.29²) = 0.18 m short of it, so its centre is **0.77 m** from the hinge. It cannot be placed at Build, because the unwound lid is open.
4. **Release, hand calculation at the defaults** (k = 160 N/m, stroke 0.6 m, Basketball 1 kg at 0.77 m, treated as a point; head 0.5 kg meeting the lid 0.5 m from the hinge):
   - Stored: ½ × 160 × 0.36 = **28.8 J**.
   - Free run of the head from −0.6 to −0.3 m: spring ½ × 160 × (0.36 − 0.09) = 21.6 J, less lifting the head 0.5 × 9.81 × 0.3 = 1.5 J: 20.1 J, so it meets the lid at 9.0 m/s.
   - Blow about the hinge: head angular momentum 0.5 × 9.0 × 0.5 = 2.24 kg·m²/s; inertias head 0.125, lid ⅓ × 0.2 × 1² = 0.067, ball 1 × 0.77² = 0.593, total 0.785 kg·m². Dead blow: 2.86 rad/s (3.2 J).
   - The spring then pushes through the last 0.3 m (+7.2 J) while ball, head and lid rise (ball 0.77 × sin 37° = 0.46 m, −4.5 J; head −1.5 J; lid −0.6 J): 3.8 J left, 3.1 rad/s, ball speed **2.4 m/s**. A fully elastic blow gives 2 × 2.86 = 5.7 rad/s and 4.4 m/s. So the ball leaves at **2.4–4.4 m/s**, square to the lid: up and back over the hinge, about 37° from vertical. Counting the ball's centre 0.34 m above the lid (radius 0.84 m about the hinge) gives 2.5 m/s and a 0.69 m rise, inside the same band.
   - The head stops at travel 0 and the lid swings on, then falls back to rest on the head. Dead-blow rise of the ball above the closed-lid plane: 0.46 m at release plus (2.4 × cos 37°)² ÷ (2 × 9.81) = 0.19 m: 0.65 m.
5. No Mechanical link: nothing winds. In Run the head is held near 0.083 m (item 1), far beyond the 5e-4 m within which the delivered retention answers Empty (`SimPrismaticRetention.cs:44`), so a pulse reports **Released** with about 0.55 J stored. The released spring lifts head and lid back toward the 0.042 m equilibrium and, undamped, to about travel 0: lifting 6.65 N through 0.083 m takes the whole 0.55 J, so the head arrives there nearly at rest and nothing is launched (row J2). The head then swings about the 0.042 m equilibrium (amplitude 0.083 − 0.042 = 0.041 m), losing energy only to the lid's 0.05 N·m hinge friction: with the lid turning about 1 ÷ 0.41 = 2.4 rad per metre of head travel, that friction acts like 0.05 × 2.4 = 0.12 N on the head, so each cycle loses 4 × 0.12 ÷ 160 = 0.003 m of amplitude; about 14 cycles of 2π√(0.6 ÷ 160) = 0.38 s (head plus the lid's share, about 0.6 kg) bring it to rest within ±0.12 ÷ 160 = ±0.001 m of 0.042 m in about 5.5 s, where it re-arms (row J9; section 6 item 4).
6. Shortest stroke (0.4 m): 12.8 J stored, 5.6 J before the head meets the lid; a visibly weaker launch.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Winding through a Mechanical link, retention, release by pulse, rearm | present | `WoundSpringElementDeclaration.cs:52`–`:67` | `WoundSpringIntegrationTests.AuthoredTeachingWindsReleasesCapturesAndResets`; `WoundSpringControlTests.PackageTriggerReportsEmptyThenReleasedThenAlreadyReleasedAndRearms` |
| No launch without independent winding | present | same | `WoundSpringIntegrationTests.TeachingRequiresIndependentWindingAndReleaseRoutes` |
| Limited passive hinge with a stop | present | `MechanicalContractValues.cs:149` | `ImpactLeverTests.AngularLimitsBoundary_StopsArrestTravelWithoutPenetration` |
| A ball resting on and thrown by a hinged plate | present | contact solver | `ImpactLeverTests.AlignedBowlingBallImpact_FlingsPayloadUpward` |
| Two moving bodies of one package colliding with each other (head against lid) | present, untested | `BodiesCollide` governs only a moving body and its support (`MechanicalContractValues.cs:112`, `:160`) | none; the slice's probe must read the collision filter and add `JackInTheBoxTests.TheHeadThrowsTheLidOpen` |
| A released retention re-arming when its coordinate comes to rest within a declared re-arm distance of its hold position | missing | the delivered retention re-arms only within `ActuatorStopSlopMetres` (5e-4 m) of travel 0 (`CuriousContraptions.Simulation/SimPrismaticRetention.cs:85`) | `ENGINE-JOINT-KINDS` (coordinator decision 31, 23 November 2026) |

Counts: 4 present, 1 untested, 0 narrower, 1 missing. Beyond the programme (owner decision 3a), the one missing row is `ENGINE-JOINT-KINDS`'; the untested row is a stated check for the slice's probe.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only.** One static body, three moving bodies, a prismatic joint, two revolute joints, one transmission, one retention, one release request and two ports: each is a capability the Wound spring or Impact lever already declares. No element code, callback or engine branch.
- **Generic laws.** Winding: the one-way transmission law. Holding: the retention law. Lid opening and launch: contact between rigid bodies, the hinge limit and gravity; the spring law on the slider. Nothing says "when released, throw the lid": the head hits it.
- **Three layers.** Colliders (physics): 5 boxes (box), 1 sphere (head), 1 cylinder (shaft), 4 boxes (lid), as hidden `col_` proxies. Visuals: the art nodes in the table, none used as a collider. Animation: coil span and latch binding, read from committed state and never written back.
- **One-way flow and typing.** Physics → animation → renderer. Ports, roles and the retention id are typed; values f32.
- **What moves each art node.** As the art table's last column: box walls and floor, nothing (static body 0); lid and lips, head, and shaft with its handle, their body poses; coil, the presentation span; latch, a presentation binding from the committed retention state.
- **Flat state.** 4 bodies; 11 colliders (9 box, 1 sphere, 1 cylinder); 3 joints; 1 transmission; 1 retention; 0 drives; 0 sensors; 1 signal node; 0 stores; 3 body-pose bindings, 1 span, 1 driver binding.
- **Performance.** Six colliders more than the Wound spring: five for a box that must contain the head and four for a lid with lips that must carry a ball. No cosmetic collider. The box is a static body. Whether a resting wound head and lid may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. The 9 m/s head against a 0.05 m lid relies on the engine's speculative contacts ([numeric authority](../../gpu-f32-physics.md)), not on a check in this part.
- **Tests are scenarios.** Every row below runs the production simulation on the package declaration.

## 4. Sources and legacy

- **Requirement.** [todo-195](../requirements.md#todo-195): "Accumulate mechanical input before release. Show winding, lid opening and launch; verify no free launch without a drive connection."
- **Campaign.** Chapter 5 (41–50), "wound launcher, jack-in-the-box"; [sequence-task-277](../requirements.md#sequence-task-277) lists it with the wound spring among shaft consumers.
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: ball speed as a floor of 2 m/s (dead-blow estimate 2.4 m/s) and a rise floor of 0.5 m above the closed-lid plane (estimate 0.65 m); lid angle ±0.05 rad; energy bound + 0.5 J; stall guard 30 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| J1 | Motor → Mechanical link winds fully; the lid has closed; a Basketball rolls onto it from a Ramp and rests against the lip; one pulse | The lid opens past 0.6 rad; the ball leaves at 2 m/s or more and rises at least 0.5 m above the closed-lid plane | Control: no pulse, the lid stays shut 10 s with the ball on it | `JackInTheBoxTests.TheHeadThrowsTheLidOpen` |
| J2 | No Mechanical link; one pulse after 1 s of Run | The request reports Released with stored energy ≤ 2 × 6.65² ÷ 160 + 0.5 = 1.05 J (about 0.55 J, item 1; Empty is answered only within 5e-4 m of travel 0); nothing is launched, the head does not pass travel 0 and the lid does not open past 0.7 rad | Control: wound fully through a Mechanical link as in J1, the same pulse launches | `JackInTheBoxTests.ThereIsNoLaunchWithoutADrive` |
| J3 | Drive stopped half-wound (lid closed) | The charge is held 10 s; a pulse gives a lower ball rise than full winding | Boundary: `stroke` 0.4 and 0.6 | `JackInTheBoxTests.WindingAccumulatesAndIsRetained` |
| J4 | Any launch | Ball kinetic plus potential energy gain ≤ ½·k·x² + 0.5 J | — | `JackInTheBoxTests.TheLaunchIsPaidByTheWinding` |
| J5 | A Wall resting over the closed lid; one pulse | The lid cannot open; the head stops under it; no penetration | — | `JackInTheBoxTests.ABlockedLidHoldsTheHead` |
| J6 | A second pulse on the tick after a launch's pulse; then, once the head has stopped at travel 0, rewound | The second pulse reports AlreadyReleased (the head is still rising); the head re-arms on reaching travel 0, rewinding closes the lid again and the next pulse launches | — | `JackInTheBoxTests.ItRearmsOnlyByWinding` |
| J7 | Reset mid-wind; Save, Load | The authored pose (item 1): head at travel 0, lid resting on it at 0.64 ± 0.05 rad, uncharged; Mechanical link, wire, parameters restored | — | `JackInTheBoxTests.ResetAndReloadRestoreTheAuthoredBox` |
| J8 | Electrical wire into `ActivationIn`; `stroke` 0.39 and 0.61 | Refused with a typed reason | Boundary: 0.4 and 0.6 admitted | `JackInTheBoxTests.WrongKindWiresAndOutOfRangeValuesAreRefused` |
| J9 | Re-arm at rest (coordinator decisions 31, 33 and 34). Motor → Mechanical link connected, the Battery's `enabled` (a run control) Disabled; one pulse at 1 s (Released, as J2); at 16 s the Battery is enabled | The released head comes to rest at 0.042 ± 0.01 m (item 5: about 5.5 s) inside the 0.1 m re-arm distance, its speed staying below v_r 0.05 m/s for t_s 0.5 s (ω = 2π ÷ 0.38 = 16.5 rad/s, so the swing amplitude must fall below 0.05 ÷ 16.5 = 0.003 m, which at 0.003 m lost per cycle takes (0.041 − 0.003) ÷ 0.003 ≈ 13 cycles, about 4.8 s; re-armed by about 5.8 s, well before 16 s), and re-arms: once the Battery is enabled, the Mechanical link winds the head below −0.3 m and the lid closes within 5 s (0.3 m at the 0.1 m/rad lead is 3 rad, 0.5 s at the Motor's 6 rad/s). The delivered law, which re-arms only within 5e-4 m of travel 0, fails this row if the lid's friction stops the head short of it | Control: a test declaration with the same box and a re-arm distance of 0.03 m, below the 0.042 m rest: the head rests at the same place, the transmission stays disengaged, and after the Battery is enabled the head stays above −0.05 m for 5 s while the Motor turns | `JackInTheBoxTests.AReleasedHeadReArmsWhereItComesToRest` |

**Integration.** Motor, Clutch, Ramp, Impact switch, Delay (delivered); Mouse motor (EL-234, this batch). Levels need S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `jack_in_the_box`; Named identity EL-235; Acceptance todo-195 and sequence-task-381; owner slice `named-el-235`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP`; then `ENGINE-JOINT-KINDS` (re-arm at rest within a declared distance, row J9). Other prerequisites: the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: the `elements/JackInTheBox/` package, `CuriousContraptions.tests/JackInTheBoxTests.cs`, and the hand-edited lists until S3. Review tier: engine (untested capability). **Disqualified if** sibling moving bodies never collide; then the lid cannot be pushed by the head and "collision between two moving bodies of one package" becomes an engine slice first, stated for any package.

## 6. Open questions

1. *Is the lid real or drawn?* Resolved by the visible-solid rule: real, with a collider, because a ball rests on it.
2. *Self-release at full winding.* Resolved by D19: removed; not declarable without engine work and no lesson uses it.
3. *Where is the head when unwound?* Resolved under the standing delegation (audit F2, option a): proud of the rim with the lid resting open on it; winding closes the lid and the ball is loaded afterwards. An authored initial charge was rejected because the requirement asks for no launch without a drive.

4. *A pulse on the resting, unwound head (second audit X-2, 23 November 2026).* Under the delivered retention law it answers Released, not Empty (item 5); J2 asserts that answer. After such a release the delivered law keeps the winding transmission disengaged until the head comes within 5e-4 m of travel 0 (`SimPrismaticRetention.cs:85`), which the lid's hinge friction can prevent, leaving a box that cannot be wound until Reset. **Decided (coordinator decision 31, 23 November 2026):** a released coordinate re-arms when it comes to rest within a declared re-arm distance of its hold position (`ENGINE-JOINT-KINDS`); this box declares 0.1 m (section 2), and row J9 asserts the re-arm with a control outside the distance.

Needs the owner: nothing.
