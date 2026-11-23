# EL-240 · Waterwheel — named-identity spec

**Current delivery (21 November 2026):** [EL-240 ledger entry](../invest/named-elements.md#element-240), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-240 · Waterwheel · Water |
| Requirement anchor | [sequence-task-386](../requirements.md#sequence-task-386) |
| Ledger entry / owner | [element-240](../invest/named-elements.md#element-240); slice `named-el-240`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a), `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` (owner decision 18).** Delivery batches 1c, liquid run. It is the liquid counterpart of the delivered Windmill, not a duplicate: the working medium, its capture law and its conservation differ. |
| Planned package | id `waterwheel`, directory `elements/Waterwheel/` |
| Related | [CAT-070 Windmill](CAT-070-windmill.md) (shape), [EL-007 Open gutter](EL-007-open-gutter.md), [EL-003 Tap](EL-003-tap.md), [EL-022 Water pump](EL-022-water-pump.md), [EL-229 Hydraulic piston](EL-229-hydraulic-piston.md), [EL-233 Generator](EL-233-generator.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples ending in the capture region), `ENGINE-ISLANDS-SLEEP` (a dry wheel at rest; nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES`; then `ENGINE-GAS-NETWORK` (the one fluid port record, coordinator decision 16); then `ENGINE-LIQUID`, its capture-region row (EL-001 L12 c). |

## 2. Declaration

Follows `elements/Windmill/WindmillElementDeclaration.cs:8`: a stand, a continuous hinge carrying the wheel, a `Drive` port, and a declared capture of a flowing medium.

- **Interfaces.** `IHingeElement` (`MechanicalContracts.cs:20`), `ICoordinateTravelElement` (`:50`), `ISceneGeometryElement` (`SceneGeometry.cs:10`), and the rotary liquid capture interface `ENGINE-LIQUID` delivers (the Windmill's is `IRotaryAirflowCaptureElement`, `:55`, for air only).
- **Stand (body 0, static).** Two A-frames 0.15 m thick, 1.0 m apart, axle 1.0 m above the base.
- **Wheel (body 1).** Rim radius 0.8 m, width 0.5 m, with eight bucket paddles 0.25 × 0.04 × 0.5 m; mass 2 kg; inertia about the axle 0.9 kg·m². Continuous hinge, friction torque 0.1 N·m.
- **Capture.** A declared capture region (the capture-region row of `ENGINE-LIQUID`, [EL-001](EL-001-finite-reservoir.md) L12 c): liquid arriving in it (the upper half of the rim's swept volume; a stream sample that ends inside it is credited to it, EL-001 L8, which holds the stream-credit rule the Batch F audit proposed as its change E-5) is carried down by the buckets and released from a declared outlet below the axle. Work on the wheel per unit volume is at most 0.7 × ρ·g × the height it descends in the wheel (up to 1.6 m). Liquid arriving on the +X side of the axle turns the wheel one way; on the −X side, the other way: the output is signed.

| Port | Domain | Direction, role | Position (m) |
| --- | --- | --- | --- |
| `Drive` | Mechanical | Output, Drive; rim radius 0.3 m on the axle | (0, 1.0, 0.6) |

- **Parameters.** None (D13). Mode records: Fixed.
- **Presentation.** Wheel body binding; bucket fill from the capture's committed held volume.
- **Palette.** Power, "Waterwheel"; order 328 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md); order 323 stays reserved while EL-223 is paused).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| A-frames and base | Boxes, cream `#fff8e9` (owner decision 4) | Solid, body 0 |
| Rim (two rings), spokes, eight paddles | Torus rings and boxes, ochre `#d99e32` with sky blue paddles | Solid, body 1: ring colliders for the rims, boxes for the paddles and spokes |
| Water in the buckets | Small blue volumes | Decorative: moved by a binding (the liquid itself is the engine's) |

Icon: a spoked wheel with paddles under a falling drop. Paddles and the drop distinguish it from the Windmill's blades.

### Behaviour and numbers

- With the family density 16 kg/m³ (D2), a flow of 0.05 m³/s descending 1.6 m carries 12.6 W; the wheel receives at most 8.8 W. At 2 rad/s that is 4.4 N·m at the axle, enough to run a Conveyor slowly or a Generator (EL-233) at low power. Twice the flow gives twice the power.
- The wheel is load-aware: a heavier shaft load slows it and buckets spill early, so less of the head is used. With the shaft blocked the liquid still passes through.
- **Reusable discharge:** every unit of liquid captured leaves the bottom of the wheel and falls into whatever is below. Volume is conserved.
- **No energy-gaining loop:** a pump driven by the wheel raises less liquid than fell through it (0.7 capture efficiency, bearing loss and pump loss), so wheel and pump together run down.
- Tick order: liquid transport commits, then the mechanical solve reads the capture torque; nothing flows back from the wheel to the liquid other than the released volume.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Continuous hinge with a `Drive` rim driving Mechanical links and conveyors under load | present | `WindmillElementDeclaration.cs:47` | `WindmillIntegrationTests.PaidShaftTransportsRemoteCargoAndReverseStagesControlDirection`; `WindmillIntegrationTests.HeavyCargoLoadsTheFiniteShaftAndUnloadedRigAcceleratesAgain` |
| Signed rotary capture of a flowing medium, paid work only | narrower | air only (`RotaryAirflowValues.cs:14`) | `WindmillIntegrationTests.ActualSourcePoseAndSignedExposureDetermineDrive` |
| **Conserved liquid flow falling onto a part** | **missing** | no liquid solver | Owner: `ENGINE-LIQUID` |
| **Liquid doing paid work on a declared revolute coordinate and being released, volume conserved** | **missing** | the liquid conventions allow no other liquid force on a body except a declared capture region doing paid work on a coordinate, which is a row of `ENGINE-LIQUID` (EL-001 L12 c) | Owner: `ENGINE-LIQUID`, its capture-region row (EL-001 L12 c), in the story text (`_bmad-output/planning-artifacts/epics.md`, ENGINE-LIQUID, "Bodies"): "A declared capture region may do paid work on a declared coordinate, at most a declared fraction of ρ·g × descent per unit volume, and releases the volume from a declared outlet" |

Counts: 1 present, 0 untested, 1 narrower, 2 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static stand, one hinged wheel with a rim port and one declared liquid capture on that coordinate: the Windmill's declaration with liquid for air. Torque is the capture law, at most 0.7 × ρ·g × descent per unit volume, signed by the side the liquid arrives on; discharge is the liquid ledger releasing the same volume; load-awareness is the shaft's ordinary coupling. The missing capability is stated for any package: liquid doing paid work on a declared revolute coordinate and being released with its volume conserved, owned by `ENGINE-LIQUID` (its capture-region row, EL-001 L12 c). No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 5 boxes on the stand (two A-frames of two legs each, base); on the wheel 2 tori (rims), 8 boxes (paddles) and 4 boxes (spokes), as hidden `col_` proxies from the admitted shape set. Visuals: the art nodes, none a collider. Animation: bucket fill follows the capture's committed held volume and writes nothing back. What moves each art node: A-frames and base nothing (static body 0); rims, spokes and paddles the body 1 pose; the bucket water its presentation binding.
- **Flat state.** 2 bodies; 19 colliders (17 box, 2 torus); 1 revolute joint; 1 capture region; 0 drives; 0 sensors; 0 signal nodes; 1 body-pose and 1 driver binding.
- **Performance.** The wheel's 14 colliders are each a proud visible solid a ball can strike (visible = solid); none is cosmetic. The liquid is captured by one bounded region (the upper half of the rim's swept volume), not by paddle contacts, so the paddles add no per-tick liquid work. The stand is static. Whether a dry wheel at rest may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. If the slice finds the count costly, the art is redrawn with solid side plates and fewer parts; no element logic is added.
- **One-way flow, typing, tests.** Liquid → mechanics → animation → renderer in the engine's declared tick phases; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [sequence-task-386](../requirements.md#sequence-task-386): "waterwheel with signed mechanical output to belts/conveyors and reusable discharge. Refactor load/energy budgeting before claiming wheel/pump loops conserve energy."
- **Campaign.** Chapter 7 "Go with the Flow" (61–70) teaches the waterwheel at 65b; the lesson "Three O'Clock at the Mill" (waterwheel, bell, counter) belongs to chapter 8's sound lessons, after the Bell is taught at 75a (coordinator decisions 36 and 41); coverage row "Waterwheel; loaded/leaky bucket; …", reuse 71–90, 114, 126–130, 136–150.
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: power and work as inequalities with 5 %; volume and flow as the liquid conventions (EL-001 L17); "turns" means over 0.5 rad/s; stall guard 60 s. A row run to the 60 s guard declares a Run length of 60 s for its level (`ENGINE-RUN-LENGTH`, owner decisions 13 and 17).

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| X1 | A tap above the +X side of the rim, opened | The wheel turns one way and a Conveyor joined by a Mechanical link carries a ball | Control: tap shut, the wheel stays still | `WaterwheelTests.FallingWaterTurnsTheWheelAndDrivesAConveyor` |
| X2 | The tap moved to the −X side | The wheel and Conveyor run the other way | — | `WaterwheelTests.TheOutputIsSigned` |
| X3 | Flow doubled | Steady shaft power under the same load at least 1.6 times as large | — | `WaterwheelTests.MoreFlowGivesMorePower` |
| X4 | Any run | Shaft work ≤ 0.7 × ρ·g × volume passed × 1.6 m + 5 % | — | `WaterwheelTests.ShaftWorkIsPaidByTheFall` |
| X5 | A basin under the wheel | Basin volume gained equals the volume that left the tap, less any spill counted | — | `WaterwheelTests.TheDischargeIsReusableAndConserved` |
| X6 | Later-partner row, run in EL-022 Water pump's slice (it needs the pump): wheel → Mechanical link → pump → back to the tap's tank, started full | The loop runs down; the tank never exceeds its starting level. The pump is open when unpowered (coordinator decision 16), so a stopped loop drains back unless a check valve is placed (`readiness/batch-f.md` section 7) | — | `WaterwheelTests.AWheelAndPumpLoopGainsNothing` |
| X7 | Shaft blocked by an overloaded Conveyor | The wheel stalls and the liquid still passes through to the basin | — | `WaterwheelTests.AStalledWheelStillPassesWater` |
| X8 | Run, Reset; Save, Load; an electrical wire to `Drive` | Wheel at rest, Mechanical link restored; the wire refused with a typed reason | — | `WaterwheelTests.ResetReloadAndWrongKindRefusal` |

**Integration.** Conveyor, Clutch, Reverse transmission, Counter (delivered); tap, gutter, basin, pump, Generator, Bell (none delivered). No Weight meets liquid in these rows (coordinator decision 21 asks a spec to state its mass where one does).

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `waterwheel`; Named identity EL-240; Acceptance `sequence-task-386` alone (it has no `todo` anchor; a sequence-task-only acceptance anchor is proposed for the ledger in the [batch summary](readiness/new-identities.md)); owner slice `named-el-240`; Mode records `Configuration=Fixed`. A slice-owned package with a sequence-task-only anchor is admitted only once main's reader admits both (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g). Levels need S8.

**Delivery.** Prerequisites, in order: blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-ISLANDS-SLEEP` and `ENGINE-CAPABILITY-INSTANCES`; then `ENGINE-GAS-NETWORK` (fluid port record); then `ENGINE-LIQUID` with its capture-region row; a tap and a basin; the coverage reader admitting this binding. One element slice; row X6 follows in EL-022's slice. Files: the `elements/Waterwheel/` package, `CuriousContraptions.tests/WaterwheelTests.cs`, and the hand-edited lists until S3 (S2 is published at `25df3cd5`; no test-project list edit). Review tier: engine. **Disqualified if** `ENGINE-LIQUID` models flow without a volume that can be held and released by a moving part; then the capture is re-specified as a region law like the Windmill's.

## 6. Open questions

1. *Overshot or undershot?* Resolved under the standing delegation: overshot, fed from above on either side. It makes "signed output" a matter of where the player puts the tap, and uses the head, which is the chapter's lesson.
2. *The requirement's "refactor load/energy budgeting".* Resolved: that is `ENGINE-LIQUID`'s finite-ledger acceptance; rows X4 and X6 are the element's share.

Needs the owner: nothing.
