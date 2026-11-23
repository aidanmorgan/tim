# EL-219 · Rack-and-pinion slide — named-identity spec

**Current delivery (21 November 2026):** [EL-219 ledger entry](../invest/named-elements.md#element-219), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending. Propagated 23 November 2026 to the [element-spec decisions of 22 November 2026](readiness/owner-decisions-2026-11-22.md).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-219 · Rack-and-pinion slide · Mechanical |
| Requirement anchor | [todo-425](../requirements.md#todo-425) (`sequence-task-369`) |
| Ledger entry / owner | [element-219](../invest/named-elements.md#element-219); slice `named-el-219`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a) and `ENGINE-ASSEMBLY-JOINTS`** (for the attachment carriage only; the drive is declarable from general capabilities once the engine programme is delivered). **Duplicate-closure candidate against [EL-053 Rotary-to-linear converter](EL-053-rotary-to-linear-converter.md)** if the attachment carriage (`ENGINE-ASSEMBLY-JOINTS`) is its only delta over EL-053 (EL-053 section 6 item 8 and section 7; Batch I hand-back, `readiness/batch-i.md` section 5); decided at EL-219's capability probe, which then makes it a variant of the EL-053 package or closes it as a Duplicate closure. This draft names the tray and the long parameter-bound rail as further candidate deltas for that probe to confirm or reject. The two share one coupling law and are planned together. |
| Planned package | id `rack_slide`, directory `elements/RackSlide/` |
| Related | [EL-218 Crank-slider](EL-218-crank-slider.md), [CAT-071 Wound spring](CAT-071-wound_spring.md), [EL-221 Parallel gripper](EL-221-parallel-gripper.md) (carried by it) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP` (a stopped carriage and its cargo at rest); then `ENGINE-ASSEMBLY-JOINTS` (the attachment socket: a player-made connection at a typed socket) and the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g). |

## 2. Declaration

Shape: the Wound spring's hinge, slider and ratio transmission (`elements/WoundSpring/WoundSpringElementDeclaration.cs:30`–`:54`), made bilateral.

- **Interfaces.** `IHingeElement` (`MechanicalContracts.cs:20`), `ISliderElement` (`:15`), `IMechanicalTransmissionElement` (`:90`), `ISceneGeometryElement`, `IParameterizedGeometryElement` (`SceneGeometry.cs:10`, `:29`).
- **Rail bed (body 0, static).** Box (`travel` + 0.8) × 0.15 × 0.5 m, length bound to `travel`; end stops 0.1 × 0.3 × 0.5 m.
- **Pinion (body 1).** Cylinder pitch radius 0.2 m, width 0.12 m, mass 0.25 kg; continuous hinge, friction torque 0.05 N·m.
- **Carriage (body 2).** Rack bar 0.8 × 0.1 × 0.3 m with a tray 0.6 × 0.06 × 0.5 m and 0.08 m lips; mass 0.6 kg. Slider along the rail, limits 0 to `travel`, no spring.
- **Transmission.** Pinion to carriage, ratio fixed 0.2 m/rad, `TransmissionConstraintKind.Bilateral` (`MechanicalContractValues.cs:255`): 1.257 m of travel per turn, and a stopped carriage stops the shaft.
- **Attachment socket.** One typed socket on the carriage top where the player fixes another part (a mirror, a gripper). Declared with the socket contract `ENGINE-ASSEMBLY-JOINTS` delivers (player-made connections between separately placed parts at typed sockets).

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `travel` | Real, m | 0.5–3 | 1.5 | Reject, build-only | A little over one turn at the default; positions a mirror across a beam lane |

| Port | Domain | Direction, role | Position (m) |
| --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input, DriveIn; rim radius 0.2 m on the pinion | (0, 0.2, 0.3) |

- **Presentation.** Pinion and carriage body bindings; rail length from `travel`.
- **Palette.** Motion, "Rack-and-pinion slide"; order 125 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| Rail bed and end stops | Boxes, cream `#fff8e9` (owner decision 4) | Solid, body 0 | nothing (bed length set in Build by the `travel` parameter binding) |
| Pinion | Cylinder at the pitch radius; teeth drawn inside the 0.2 m cylinder, navy `#1c2836` | Solid, cylinder collider | body pose |
| Rack bar, tray and lips | Boxes, sky blue `#7ebcd4`; teeth drawn within the bar's box | Solid, body 2 | body pose |
| Witness marks every 0.25 m on the bed | Printed | Decorative: printed flat | nothing |

Teeth are drawn inside their colliders, so nothing visible lacks solidity. Icon: a toothed bar under a small gear. It differs from the crank-slider by the straight toothed bar.

### Behaviour and numbers

- Load-aware only: the carriage moves because the transmission couples it to the shaft, so a load on the carriage is felt by the Motor, and at an end stop the shaft stalls. No prescribed-speed mode exists (one path).
- At the Motor's default 6 rad/s the carriage moves 1.2 m/s; the default 20 N·m gives up to 100 N at the rack.
- Reversing the shaft reverses the carriage. With no Mechanical link the carriage is free and stays where friction leaves it.
- Whether a stopped carriage, or cargo resting on its tray, may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Bilateral revolute-to-prismatic ratio coupling, admitted and committed | present | `MechanicalContractValues.cs:258` | `RetentionBoundaryTests.CanonicalBilateralCoordinatePairsSurviveAdmissionAndCommittedAbi` |
| A prismatic load reacting on its winding shaft | present | same | `PrismaticRetentionTests.ElasticLoadReactsOnATorqueLimitedWindingShaft` |
| Bilateral coupling stalling the shaft at a slider end stop and under carried load | present, untested | same | none (the Wound spring is one-way); the slice adds `RackSlideTests.TheEndStopStallsTheShaft` |
| Rotated mount keeps domains and sign | present | `PortDescriptor.cs:43` | `ClutchTests.RotatedMounts_PreserveDomainsAndSign_ClosedLoopStaysAtRest_AndDuplicateDriveInRejected` |
| Collider length bound to a parameter | present | `SceneGeometry.cs:29` | `HollowElementTests.ABallRollsThroughAStraightPipeOfEveryLengthOnItsWallWithoutCatching` |
| Cargo carried on a moving slider body | present | `MechanicalContractValues.cs:104` | `LinearPusherTests.VerticalPackageLiftsCargoOnlyWithSufficientDeclaredForce` |
| **A part fixed to the carriage at a typed socket** | **missing** | no attachment socket contract exists | Owner: `ENGINE-ASSEMBLY-JOINTS` ("connections the player makes between two separately placed parts at typed attachment sockets") |

Counts: 5 present, 1 untested, 0 narrower, 1 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static rail bed, a hinged pinion, a sliding carriage, one bilateral ratio transmission and one attachment socket. Travel per turn is the transmission law at the declared 0.2 m/rad; a stopped carriage stopping the shaft is the same bilateral constraint; carrying is rigid contact. The missing capability is stated for any package: a typed attachment socket on a moving body where the player fixes a separately placed part, owned by `ENGINE-ASSEMBLY-JOINTS` (player-made connections). No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 9 boxes (bed, 2 end stops; rack bar, tray, 4 lips) and 1 cylinder (pinion) as hidden `col_` proxies; teeth are drawn inside them. Visuals: the art nodes, none a collider. Animation: none beyond body poses; the bed's drawn length follows `travel` as its collider does. Per art node: bed, end stops and witness marks are moved by nothing; the pinion by body 1's pose; rack bar, tray and lips by body 2's pose.
- **Flat state.** 3 bodies; 10 colliders (9 box, 1 cylinder); 2 joints (1 revolute, 1 prismatic); 1 transmission; 1 socket; 0 drives; 0 sensors; 0 nodes; 0 stores; 2 body-pose bindings, 1 parameter binding.
- **Performance.** Four colliders more than the Wound spring's six; the tray and its four lips carry cargo. The bed is static. Whether a stopped carriage may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. Carriage speed is 0.2 m/rad × shaft speed, 1.2 m/s at a default Motor.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-425](../requirements.md#todo-425): "Signed shaft drives finite toothed rail with attachment carriage and end stops. Teeth/witness marks explain translation. Explicitly distinguish prescribed-speed prototype from load-aware final behaviour."
- **Campaign.** Chapter 4 (31–40); coverage row "… rack-and-pinion".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: travel per turn 1.257 m ± 0.03 m; position ±0.02 m; "stalled" is below 0.1 rad/s; stall guard 20 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| K1 | Motor → Mechanical link → pinion, one turn | The carriage moves 1.257 m | Control: no Mechanical link, the carriage does not move | `RackSlideTests.OneTurnMovesOnePitchCircumference` |
| K2 | Shaft reversed | The carriage returns | — | `RackSlideTests.ReversalReturnsTheCarriage` |
| K3 | Driven into an end stop | The carriage stops at `travel`, no penetration, and the shaft stalls | Boundary: `travel` 0.5 and 3 | `RackSlideTests.TheEndStopStallsTheShaft` |
| K4 | Basketball on the tray; a Wall in its path | The ball rides the tray; at the Wall the ball is stopped and the carriage stalls or the ball is pushed off, never passed through | Control: no Wall | `RackSlideTests.CarriedCargoCollidesPhysically` |
| K5 | The slide yawed 90° and pitched 30° | K1 holds along the rotated rail | — | `RackSlideTests.ARotatedSlideKeepsItsTravelPerTurn` |
| K6 | A Wall fixed to the attachment socket | It moves with the carriage and blocks a ball lane only at the commanded position | Control: unattached Wall stays put | `RackSlideTests.AnAttachedPartMovesWithTheCarriage` |
| K7 | Run, Reset; Save, Load | Authored carriage position, `travel`, Mechanical link and attachment restored | — | `RackSlideTests.ResetAndReloadRestoreTheAuthoredSlide` |
| K8 | Electrical wire to `DriveIn`; `travel` 0.4, 3.1 | Refused with a typed reason | — | `RackSlideTests.WrongKindWiresAndOutOfRangeValuesAreRefused` |

The requirement's lesson "Solar → motor → rack positions mirror → receiver controls supplied gate" is a named later-partner row in the Mirror's slice, with K6 as its mechanical half; it needs the Solar panel, Mirror and a light receiver, all after `ENGINE-OPTICAL`. Its optical half follows the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions) (EL-176 section 10) and is not restated here: the Solar panel supplies the Motor only through the store-credit form of coordinator decision 21 (a declared fraction of absorbed energy credited into a finite store that an ordinary source draws from); the receiver responds under the one receiver law for all light (owner decision 9) and couples to the gate only through observation → signal node → signal-state contact.

**Integration.** Motor, Conveyor, Clutch, Reverse transmission (delivered); EL-221 gripper, CAT-041 Mirror, EL-211 Solar panel (later). Levels need S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `rack_slide`; Named identity EL-219; Acceptance todo-425 and sequence-task-369; owner slice `named-el-219`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** One element slice. Prerequisites: blocked on the engine programme (owner decision 3a), in particular `ENGINE-ISLANDS-SLEEP`; then `ENGINE-ASSEMBLY-JOINTS` (attachment sockets) and the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: the `elements/RackSlide/` package, `CuriousContraptions.tests/RackSlideTests.cs`, and the hand-edited lists until S3. Review tier: engine. **Disqualified if** bilateral coupling at an end stop injects energy or jitters (then that is an engine defect to fix first); or the probe finds no delta over EL-053 beyond the attachment carriage (then it is a variant of the EL-053 package or a Duplicate closure candidate, as the Disposition states).

## 6. Open questions

1. *Prescribed-speed prototype.* Resolved: not built. The forward-only rule allows one behaviour, and the requirement names load-aware as final.
2. *Deliver the drive now and the socket later?* Resolved under the standing delegation: no. The requirement names the attachment carriage, and a part without it would be EL-053.

Needs the owner: nothing.
