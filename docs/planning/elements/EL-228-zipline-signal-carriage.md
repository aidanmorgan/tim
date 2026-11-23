# EL-228 · Zipline signal carriage — named-identity spec

**Current delivery (21 November 2026):** [EL-228 ledger entry](../invest/named-elements.md#element-228), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending. Propagated 23 November 2026 to the [element-spec decisions of 22 November 2026](readiness/owner-decisions-2026-11-22.md).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-228 · Zipline signal carriage · Mechanical |
| Requirement anchor | [todo-400](../requirements.md#todo-400) (`sequence-task-383`) |
| Ledger entry / owner | [element-228](../invest/named-elements.md#element-228); slice `named-el-228`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a) and `ENGINE-JOINT-KINDS` (the hold of a slider no transmission winds, and its release by a physical request bound to the pad's contact node; engine-stories-index C2)**: the hold-and-release only; the rest is declarable from general capabilities once the engine programme is delivered. Delivery batches lists it in 1b, run "drawn and guided carriages". |
| Planned package | id `zipline`, directory `elements/Zipline/` |
| Related | [EL-062 Docking ferry](EL-062-docking-ferry.md), [EL-225 Slingshot](EL-225-latched-slingshot-cradle.md) (same latch gap), [EL-063 Cable winch](EL-063-cable-winch.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CONTACT-OBSERVATIONS` (the trigger pad's contact node) and `ENGINE-ISLANDS-SLEEP` (a latched carriage at rest); then `ENGINE-JOINT-KINDS` (the hold of the unwound slider and its release by a physical request bound to the pad's contact node; engine-stories-index C2), and the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g). |

## 2. Declaration

**Decision on the ledger's probe question** ("a body guided along a tensile cable"): the cable is a straight, taut guide, declared as a prismatic slider between two posts. Sag is not simulated. Reason: a taut cable is a straight line, the slider exists and is tested, and a second guided-body rule would duplicate it. The cable is a solid static bar with its own box collider bound to `span`, part of body 0; it is not a strand and carries no tension, so owner decision 1 and `ENGINE-CONNECTION-MATERIALS` do not apply to it.

- **Interfaces.** `ISliderElement` (`MechanicalContracts.cs:15`), `IPrismaticRetentionElement` (`:85`), `ISignalElement` (`SignalContracts.cs:8`), `ISceneGeometryElement`, `IParameterizedGeometryElement` (`SceneGeometry.cs:10`, `:29`). The retention is declared in the form `ENGINE-JOINT-KINDS` delivers for a coordinate no transmission winds.
- **Posts (body 0, static).** Two posts 0.2 × 1.2 × 0.2 m, `span` apart along local X, the upper one carrying a trigger pad 0.5 × 0.08 × 0.5 m; end buffers 0.15 m thick at both posts; the cable, a bar 0.02 × 0.02 m between the post tops with its own box collider whose length is bound to `span`.
- **Carriage (body 1).** Trolley 0.3 × 0.15 × 0.2 m with a striker ball of radius 0.2 m hanging 0.4 m below on a rigid hanger; mass 2 kg (it must move a 1 kg ball at the far end); restitution 0.4.
- **Slider.** Along local X, limits 0 to `span`, no spring, no drive; gravity on the carriage. The player tilts the whole part in Build: tilted down, gravity pulls the carriage along the cable; level or uphill, it does not move.
- **Hold and release.** Two parts, both of `ENGINE-JOINT-KINDS` (holds on a coordinate and their release; engine-stories-index C2). The hold: a retention holds the carriage at 0 (`readiness/batch-i.md` section 5 G1; summary gap G2). The release: a contact node on the trigger pad (`SignalContractValues.cs:51`, threshold `trigger_threshold`) is wired by an internal edge to a physical release request (`:65`). The contact node is a contact trigger re-qualified by `ENGINE-CONTACT-OBSERVATIONS`.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `span` | Real, m | 2–10 | 5 | Reject, build-only | Crosses a gap wider than a ball can be thrown reliably |
| `trigger_threshold` | Real, m/s | 0–64 | 0.8 | Clamp | The Impact switch's parameter |

| Port | Domain | Direction, role | Position |
| --- | --- | --- | --- |
| `Tie` | Rope | Bidirectional, Tie, load attachment on the carriage | Uphill face of the trolley |

The rope port is declared `PortDirection.Bidirectional` as on every delivered rope part (`CuriousContraptions.Abstractions/PortDescriptor.cs:9`; `elements/ImpactLever/ImpactLeverElementDeclaration.cs:44`).

- **Presentation.** Carriage body binding; the drawn cable's length follows `span`, as its collider does.
- **Palette.** Motion, "Zipline"; order 140 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| Posts, pad, buffers | Boxes, cream `#fff8e9` (owner decision 4); pad coral `#d96b58` | Solid, body 0 | nothing |
| Trolley, hanger, striker ball | Box, bar 0.05 m, sphere 0.2 m, navy and gold | Solid, body 1 (three colliders) | body pose |
| Cable | 0.02 m square bar between post tops | Solid, box collider on body 0, length bound to `span` | nothing (length set in Build by the `span` parameter binding) |

Icon: a slanted line between two posts with a hanging ball. It differs from the Rope and Pulley by the rigid slant and striker.

### Behaviour and numbers

- An incoming ball strikes the pad; the carriage is released and runs down the cable. On a 15° tilt over 5 m it drops 1.3 m and arrives at about 5 m/s; a 2 kg carriage striking a resting 1 kg ball sends it off at about 4.7 m/s with the declared restitution 0.4 (1.4 × 2 ÷ 3 × 5; the elastic bound is 6.7 m/s). The initiating ball stays where it fell: it is never carried or moved to the far end.
- The carriage stops at the lower buffer. A rope on `Tie` returns it: a winch (EL-063, later), or with delivered parts a 1 kg Weight over a Pulley, held on a ledge with its rope just taut after the run and pushed off by a Linear pusher. The carriage's weight along a 15° cable is 2 × 9.81 × sin 15° = 5.1 N, so any load above 0.52 kg returns it, and the same load left on `Tie` stops it running again. **The return rope must be cut or paid out before the next run.** At 0 the retention re-engages. Returning it costs the work of lifting 2 kg through the 1.29 m drop (25.4 J at the defaults).
- A second strike on the pad while the carriage is away finds the retention already released and does nothing.
- **Ropes.** Ropes are solid strands under owner decision 1 once `ENGINE-CONNECTION-MATERIALS` (solid strand) is delivered; the delivered rope draws without a collider, a recorded visible-solid defect owned by that story. Every rope route in these rows runs clear of other colliders, so the rows hold either way. Route condition: a return rope laid along the cable line would cross the upper buffer, post and pad, so the rows stand the return Pulley uphill of and above the upper post, with its rim above the pad and buffer, and the rope rises from `Tie` clear of all three at every carriage position. The along-cable pull of the 1 kg Weight stays above the 5.1 N needed while the rope meets the cable at under 58° (9.81 × cos 58° = 5.2 N). The slice's scene confirms this geometry.
- Whether a latched carriage may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Passive slider on a tilted mount under gravity | present | `MechanicalContractValues.cs:104` | `DirectionalPrismaticTests.ZeroPassiveAxialCoefficientsLeaveAnUndrivenSliderFree`; `DeclaredPrismaticDriveTests.AnUnpoweredInvertedOrTiltedMountRestingInItsInteriorWithNothingInTheWayIsNotBlocked` |
| Slider limits bound to a parameter | present | `MechanicalValue.FromParameter` (`MechanicalContractValues.cs:171`) | `LinearPusherTests.ParameterEndpointsCompileAndProduceUsefulMotion` |
| Contact node on a static body feeding a request in one element | present; re-qualified on any body pair, found by index, by `ENGINE-CONTACT-OBSERVATIONS` | `SignalContractValues.cs:51`, `:65` | `WoundSpringControlTests.ActualImpactSwitchReleasesPartialChargeWithoutTimer` (across two parts); `ComposedPhysicalSignalTests.PhysicalSensorDelaysOnlyItsSelectedMemoryInOneElement` |
| A rope tied to a slider body | present | rope domain | `RopeTests.ASuppliedPrismaticDriveHeldByARopeHoldsShortOfItsTargetAndCommitsRest` |
| **Holding and releasing a slider that has no spring and no winding** | **narrower** | retention is for "a zero-rest compressive elastic coordinate" with a winding transmission (`PrismaticRetentionValues.cs:12`–`:15`) | Owner: the hold and its release by a physical request bound to the pad's contact node, `ENGINE-JOINT-KINDS` (holds on a coordinate and their release; engine-stories-index C2; summary gap G2, shared with EL-225) |
| A parameter-bound collider length on a static body (the cable) | present, untested here | `ColliderParameterBinding` (`SceneGeometry.cs:26`), delivered on the Wall and Ramp (`elements/Wall/WallElementDeclaration.cs`) | The slice's probe names the Wall's size test; none was opened for this spec. If the probe finds it narrower, the owner is `ENGINE-PARAMETER-BOUND-FRAMES` |

Counts: 4 present, 1 untested, 1 narrower, 0 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** Two static posts with a pad, buffers and a cable; one passive slider; one retention; one contact node wired to one release request; one rope port. The run is gravity along the slider axis; the strike is rigid contact with the declared restitution; the trigger is the contact-node rule feeding the retention law; the return is rope tension. The narrower capability is stated for any package: a hold on a prismatic coordinate that is not wound by a transmission, and its release by a physical request bound to a declared signal source, owned by `ENGINE-JOINT-KINDS` (engine-stories-index C2). No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 6 boxes on body 0 (2 posts, pad, 2 buffers, cable) and 2 boxes and 1 sphere on the carriage, as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: none beyond the body pose. Per art node: posts, pad, buffers and cable are moved by nothing (the cable's length is set in Build by its parameter binding); trolley, hanger and striker by body 1's pose.
- **Flat state.** 2 bodies; 9 colliders (8 box, 1 sphere); 1 prismatic joint; 1 retention; 0 drives; 1 contact sensor; 2 signal nodes and 1 internal edge; 0 stores; 1 body-pose binding, 1 parameter binding.
- **Performance.** Few primitives; the posts and cable are static bodies. Whether a latched carriage may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. The carriage reaches 5 m/s at the defaults; thin obstacles rely on the engine's speculative contacts.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-400](../requirements.md#todo-400): "Incoming ball releases a gravity-driven cable carriage that strikes a separate destination ball; it does not teleport or carry the initiating ball. Winch returns it."
- **Campaign.** Chapter 5 (41–50), "zipline carriage".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: carriage position ±0.03 m; "stalls" means it moves less than 0.05 m in 10 s; per-tick displacement bounded (no teleport); stall guard 30 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| Z1 | Tilted 15°; ball dropped on the pad; a second ball resting at the lower end beside a Bell stand-in (Impact switch and Lamp) | The carriage runs down, strikes the second ball, and that ball triggers the switch; the first ball is still within 1 m of the pad | Control: no ball on the pad, nothing moves for 10 s | `ZiplineTests.ABallOnThePadSendsTheCarriageToHitTheFarBall` |
| Z2 | Level, and tilted uphill 10° | Released, the carriage stalls | Boundary: 5° down runs | `ZiplineTests.AFlatOrUphillCableStalls` |
| Z3 | A Wall across the cable at mid-span | The carriage stops at it, no penetration | — | `ZiplineTests.AnObstructionStopsTheCarriage` |
| Z4 | No destination ball; destination blocked by a Wall | The carriage stops at the buffer; nothing else moves | — | `ZiplineTests.AnEmptyOrBlockedDestinationIsHarmless` |
| Z5 | A second pad strike while the carriage is away | Nothing happens | — | `ZiplineTests.APrematureRetriggerDoesNothing` |
| Z6 | After a run, a 1 kg Weight pushed off its ledge pulls `Tie` over a Pulley | The carriage returns and re-latches; work done on it ≥ m·g·drop − 1 J (24.4 J). After the rope is cut, a later strike runs it again | Control 1: no rope, it stays down. Control 2: rope not cut, a later strike releases the latch and the carriage does not run | `ZiplineTests.ReturningItCostsWork` |
| Z7 | Run, Reset; Save, Load | Carriage latched at 0; `span`, tilt and rope restored | Boundary: `span` 2 and 10 | `ZiplineTests.ResetAndReloadRestoreTheAuthoredZipline` |
| Z8 | An electrical or activation wire to `Tie`; `span` 1.9 | Refused with a typed reason | — | `ZiplineTests.WrongKindConnectionsAndOutOfRangeValuesAreRefused` |

The requirement's Bell (CAT-009) is after ENGINE-ACOUSTIC; Z1 is repeated with it there.

**Integration.** Impact switch, Lamp, Pulley, Weight, Linear pusher, Rope anchor (delivered); EL-063 winch (its winding clause is `ENGINE-TENSILE-EXTENSIONS`'), CAT-009 Bell (later). Levels need S8.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `zipline`; Named identity EL-228; Acceptance todo-400 and sequence-task-383; owner slice `named-el-228`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g).

**Delivery.** One element slice. Prerequisites: blocked on the engine programme (owner decision 3a), in particular `ENGINE-CONTACT-OBSERVATIONS` and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-JOINT-KINDS` (the hold of an unwound slider and its release by physical request), and the coverage reader admitting a slice-owned package (S7; EL-223 is paused, owner decision 24g); S3 if published (S2 is published at `25df3cd5`; no test-project list edit). Files: the `elements/Zipline/` package, `CuriousContraptions.tests/ZiplineTests.cs`, and the hand-edited lists until S3. Review tier: engine. **Disqualified if** a 10 m slider is outside the solver's stable range.

## 6. Open questions

Resolved: the cable has a 0.02 m box collider on body 0, length bound to `span`, so it is an ordinary visible solid and needs no owner decision (audit F15). Resolved: sag (not simulated, above); the trigger (a pad on the upper post, D19); the return load lets go by a cut rope (audit F3).

Needs the owner: nothing.
