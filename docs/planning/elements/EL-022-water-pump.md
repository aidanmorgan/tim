# EL-022 · Water pump — named-identity spec

**Repair, 22 November 2026.** Repaired against the independent audit `docs/planning/elements/readiness/audits/elements-batch-f.md`, the conventions as restated in EL-001 and the owner decisions of that date. This part is blocked on the engine programme (owner decision 3a) before `ENGINE-LIQUID`.

**Current delivery (24 October 2026):** [EL-022 delivery slice](../invest/named-elements.md#element-022). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa`. The liquid conventions L1 to L18 and the capability probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md); the pipe-kit values are in [EL-008](EL-008-straight-water-pipe.md). Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-022 · Water pump · Water |
| Anchors | [element-022](../requirements.md#element-022), co-anchored `sequence-task-575`; source record [todo-338](../requirements.md#todo-338); integration [sequence-task-389](../requirements.md#sequence-task-389); ledger [element-022](../invest/named-elements.md#element-022), owner S443 |
| Disposition | To deliver as a single element with one `supply` Choice (Electrical or Shaft), the form the delivered Fan uses. The two supplies are modes of one entry (D8), each with its own acceptance rows. The Archimedes screw is the separate identity EL-023 |
| Planned package | id `water_pump`, directory `elements/WaterPump/`, title "Water pump" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-LIQUID` |
| Related | [EL-001](EL-001-finite-reservoir.md), [EL-002](EL-002-header-tank.md), [EL-019](EL-019-water-check-valve.md), [EL-018](EL-018-electronic-level-switch.md), [EL-013](EL-013-water-nozzle.md), EL-023, [EL-240](EL-240-waterwheel.md), [EL-229](EL-229-hydraulic-piston.md); delivered Battery, Motor |

## Declaration

Closest delivered shape: the Fan, a static housing with a hinged rotor, a `supply` Choice and a `DriveIn` available only in Shaft mode (`elements/Fan/FanElementDeclaration.cs:20`, `:53`–`:55`).

- **Interfaces.** `ISceneGeometryElement`, `IHingeElement`, `ISupplyElement`, the liquid fitting and work-target declaration of `ENGINE-LIQUID` (W4, W7, W15).
- **Body 0 (static housing).** One box 0.50 × 0.40 × 0.40 at the origin. Delivered static default material (restitution 1, friction 0.3, bounce threshold 0.1 m/s).
- **Body 1 (dynamic rotor).** A pulley disc: solid Cylinder radius 0.15, length 0.06, axis local Z, at (0, 0, 0.23) on the front face. 0.5 kg, solid-disc inertia ½·m·r² = 0.0056 kg·m² about Z. One revolute hinge about Z, no limits, friction torque 0.05 N·m.
- **Liquid node.** One fitting with one **work target** on the edge `Start` → `End` (L15), fixed values: maximum added head 3.0 m; rated input 30 W; efficiency 0.5. With no grant it adds no head and is an open, lossless edge (L15, coordinator decision 16).
- **Mouths (pipe bore).** `Start` at (−0.25, −0.05, 0), normal −X, Intake (low, left), 0.15 m above the housing's base (the kit rule). `End` at (+0.25, +0.15, 0), normal +X, Discharge (high, right).

| `PortId` | Role | Domain | Direction | Position, normal | Available when |
| --- | --- | --- | --- | --- | --- |
| `PowerIn` | `PortRole.PowerIn` | Electrical | Input | (0, 0.20, 0), +Y | `supply` = Electrical |
| `DriveIn` | `PortRole.DriveIn` | Mechanical | Input | rim of the rotor, radius 0.15 (`MechanicalRimDescriptor`) | `supply` = Shaft |

| Parameter | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `supply` | Choice | None | 0 (Electrical) | 0 | 1 | Reject; build only; options `Electrical`, `Shaft` |

- **Input side of the work target.** Electrical: a supply consumer on `PowerIn` (as the Powered gate's, `elements/PoweredGate/PoweredGateElementDeclaration.cs:88`) with demand 30 W. One rule for both supplies (L15): the energy taken is the hydraulic work done ÷ efficiency. Electrical debits it from the grant. Shaft: the rotor's revolute coordinate meets a resisting torque of that power ÷ |ω|, never above min(30 W / |ω|, 4 N·m); a dry or stalled pump therefore loads the shaft with nothing. The cap form is the one Batch H's compressor (EL-037) uses.
- **Presentation.** `Visual/Rotor` follows body 1's pose. `Visual/Impeller` rotation from the committed edge flow, integrated (`PresentationCurve.IntegratedRate`; the liquid driver is missing, W12). `Visual/Lamp` albedo from `PresentationDriver.CommittedSupply`, slate to gold.
- **Palette.** `ElementCategory.Power`; palette order 326 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour cyan.

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Housing` | Box 0.50 × 0.40 × 0.40, cyan `#66b8c9`, with a navy `#293954` band printed along its base | solid; the box collider | body 0 pose (static) |
| `Visual/Rotor` | Disc radius 0.15, thickness 0.06, cream `#fff8e9` | solid; the Cylinder collider | body 1 pose |
| `Visual/Impeller` | Three-blade glyph on a printed window on the top face, flush | decorative: printed flat, turned by a binding | presentation binding |
| `Visual/Lamp` | Flush disc on the top face | decorative: printed flat (glow) | presentation binding |
| `Visual/PortRings` | Navy rings round both mouths, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_start`, `Loc_Port_mouth_end`, `Loc_Port_PowerIn`, `Loc_Port_DriveIn` | Locators | none | body pose |

Icon: a box with a pulley disc and two port rings at different heights. It differs from the Motor (a housing with a shaft) by its two water ports and impeller glyph. Running reads from the impeller's motion, not from the lamp colour alone.

### Behaviour and numbers

- **Law (L15).** The edge adds head h_p with two limits: h_p ≤ 3.0 m, and hydraulic power ρ·g·Q·h_p ≤ 0.5 × input power. With 30 W of input the product Q·h_p is at most 15 / 156.96 = 0.0956 m³/s·m. The route then obeys L7 with the added head: Q = K·√(h_p − L), L being the lift from the intake surface to the delivery head.
- **Worked case (Electrical).** A full Water tank on the bench (surface 1.15 m) joined to `Start`; from `End` a pipe, elbow and riser to a free end at 2.25 m over a Header tank at `stand_height` 1.0 (rim 2.15 m). Two apertures: K = K₀/√2 = 0.05904.
  - Start, L = 1.10 m: 0.05904·√(h_p − 1.10)·h_p = 0.0956 gives h_p = 1.86 m, **Q = 0.0515 m³/s**.
  - After 0.25 m³ the tank surface is 0.25 m lower, L = 1.35 m: h_p = 2.00 m, Q = 0.0477 m³/s.
  - Mean 0.0496 m³/s: the Header tank's 0.25 m³ is delivered in **5.0 s**.
  - Energy: 30 W × 5.0 s = 151 J from the Battery. The liquid's gravitational energy rises by 156.96 × 0.25 × (1.65 − 1.025) = 24.5 J; the rest is lost at the apertures and in the pump. The bound "debit ≥ ρ·g·V·L / efficiency" is 156.96 × 0.25 × 1.10 / 0.5 = 86 J, and 151 J exceeds it.
- **Attainable from the delivered Battery.** 30 W is 0.25 J per tick; the default Battery grants 120 W, at most 1 J per tick (`elements/Battery/BatteryElementDeclaration.cs:44`), and its 3600 J run the pump for 120 s.
- **Stall.** With the free end 3.0 m or more above the intake surface, h_p cannot exceed L: Q = 0, no hydraulic work, no debit. Just below, at L = 2.9 m: Q = 0.05904 × √0.1 = 0.0187 m³/s, hydraulic power 8.8 W, head-limited.
- **Unpowered.** No grant, no added head: the pump is an open edge. Nothing rises, so "unpowered pump cannot sustain uphill flow" holds. Liquid already lifted runs back down through it unless a Check valve holds it, and a tank above it drains through it downhill.
- **Dry intake.** No wet source at `Start`: Q = 0, so the spent work and the debit are 0.
- **Shaft.** A Motor turning the rotor at 5 rad/s can give at most min(30/5, 4) = 4 N·m, 20 W: Q·h_p ≤ 10 / 156.96 = 0.0637, so at L = 1.10 m h_p = 1.57 m and Q = 0.0406 m³/s. Above 7.5 rad/s the input is the rated 30 W.
- **No perpetual loop.** A Waterwheel ([EL-240](EL-240-waterwheel.md)) gives at most 4.4 N·m at 2 rad/s, 8.8 W, from 12.6 W of falling water. The pump takes at most 4 N·m at 2 rad/s, 8 W, and returns at most 4 W of hydraulic power: under a third of what the wheel consumed.

## Engine capabilities

| Row (EL-001 probe) | Mark | Use here |
| --- | --- | --- |
| W1 static housing; W21 rotor body and Cylinder | present | bodies |
| W19 revolute hinge | present | rotor |
| W24 supply Choice with port availability, electrical consumer, shaft input | present | `supply`, `PowerIn`, `DriveIn` |
| W22, W23 authored state, wire refusal, parameter refusal | present | ports and `supply` |
| W4 fluid port contract; `CommittedSupply` lamp driver | present, untested | fitting; lamp |
| W3 tube mouth and snap | narrower | join not compiled |
| W7 fitting and flow; W8 free end | missing | `ENGINE-LIQUID` |
| W15 work target adding head on a liquid edge, from an electrical grant or shaft work | missing | `ENGINE-LIQUID` (gap in its text) |
| Debit of the energy actually spent, and a grant that scales with demand | missing | `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`); today drive supply is binary |
| W12 driver for a committed edge flow | missing | `ENGINE-LIQUID` |

Counts: 5 present, 2 untested, 1 narrower, 5 missing. Two owners, one per capability: the liquid work target and its observations (`ENGINE-LIQUID`); demand and debit (`ENGINE-SUPPLY-DEMAND`).

### Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, two colliders, one hinge, one fitting with one work target, two mouths, two ports, one Choice, three bindings. The earlier `PumpState { Unpowered, DryIdle, Pumping }` enum is gone: the three looks are the supply read and the flow read.
2. **Generic laws.** Lifting is L15 with L7. The earlier pump curve Q = Q_max·(1 − H/H_max) is not used: it was a second flow rule beside the aperture law. Supply is the delivered supply network; the shaft load is the general work-target input law.
3. **Three layers.** *Colliders:* housing box and rotor Cylinder. *Visuals:* the rotor art follows body 1. *Animation:* impeller and lamp read committed state.
4. **One-way flow and typing.** Supply → liquid work → next substep; the work target spends only energy already granted (L16). `supply` is a typed Choice; ports are the delivered enums.
5. **Flat-state counts.** Bodies 2 (1 static, 1 dynamic); colliders 2 (1 box, 1 Cylinder); joints 1 revolute; drives 0; sensors 0; liquid nodes 1 fitting; work targets 1; mouths 2; supply consumers 1; ports 2; bindings 1 body + 2; parameters 1.
6. **Performance fitness.** Two colliders, as the Fan's two (`elements/Fan/fan.tscn`). One edge row with a bounded solve for h_p.
7. **Tests are scenarios.** Rows M1 to M10.

## Sources and legacy

- Requirement: "Finite external work raises pressure and transports liquid"; outcome "Unpowered pump cannot sustain uphill flow." No variants.
- Refinement [S707](../invest/refinements.md#s707) pump; component research [water](../../component-research.md#water) lines 20 ("no perpetual fountains"), 35 ("a dry intake is visibly idle"), 54, 55 and 59; [todo-428](../requirements.md#todo-428) "no perpetual wheel/pump loop".
- **Legacy search.** No liquid pump. Finite-work analogues:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Requests on one finite store are scaled so total work never exceeds stored energy. | `engine/physics/StoredFlowSource.cs@a6c914e:L28-L73` | Carry forward the outcome (never more than granted). Not the f64 CPU allocator. |
| 2 | An empty store or a stationary source yields zero work; force and power ratings cap effort independently. | `CuriousContraptions.tests/StoredFlowSourceTests.cs@a6c914e:L42-L54` | Carry forward: dry or unpowered moves nothing; head and power are separate caps. |
| 3 | A store's debit never exceeds its contents. | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L52` | Carried forward; delivered in the supply network. |

**Files harvested:** none beyond EL-011's.

## Acceptance outline

Authority: [element-022](../requirements.md#element-022). Tolerances L17; energy within 10%. Planned class `WaterPumpTests`. Rig: the worked case above, with a Battery wired to `PowerIn`.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| M1 | Given the rig, when Run advances until the Header tank is full (stall guard 15 s), then it holds 0.25 m³, the Water tank has lost the same within 0.004 m³, and this took 5.0 s within 10% | `WaterPumpTests.ASuppliedPumpLiftsLiquidToAHigherTank` | M2 | — |
| M2 | Given the Battery wire removed, when Run advances 10 s, then the Header tank holds 0, the Water tank keeps 1.0 m³ and no aperture reports flow | `WaterPumpTests.AnUnpoweredPumpLiftsNothingAndPassesNothing` | M1 | a disabled Battery gives the same; the pump placed below a full tank with its `End` free and no power passes liquid downhill only, as an open pipe would |
| M3 | Given M1, then the Battery's charge has fallen by at least 86 J and by 151 J within 15%, and the stores' gravitational energy has risen by no more than half of that fall | `WaterPumpTests.TheLiftCostsAtLeastTheEnergyItStores` | — | — |
| M4 | Given the free end at 4.2 m (lift 3.05 m or more), when Run advances 10 s, then nothing is delivered and the Battery's charge is unchanged within 1 J | `WaterPumpTests.ThePumpStallsAtItsMaximumHead` | M1 | free end at 4.0 m delivers |
| M5 | Given an empty Water tank, then nothing moves and the Battery's charge is unchanged within 1 J | `WaterPumpTests.ADryIntakeDrawsNoEnergy` | M1 | — |
| M6 | Given `supply` Shaft and a Motor joined to `DriveIn` by a Mechanical link (the delivered rigid connection; coordinator decision 36) at 5 rad/s, then the starting flow is 0.0406 m³/s within 15%; with the Motor unsupplied nothing is lifted | `WaterPumpTests.AShaftDrivenPumpLiftsInProportionToShaftWork` | Motor unsupplied | — |
| M7 | Given `End` joined by pipe through a Check valve ([EL-019](EL-019-water-check-valve.md)) to the Header tank's mouth, and a Battery of `capacity` 60 J (2 s at 30 W), when Run advances 25 s, then the Header tank still holds what was lifted, at least 0.09 m³ (about 0.11 m³ is lifted in 2 s) | `WaterPumpTests.ACheckValveHoldsTheLiftWhenThePumpStops` | the same rig without the valve: the Header tank drains back through the stopped pump to under 0.01 m³ | — |
| M8 | Given a Level switch ([EL-018](EL-018-electronic-level-switch.md)) in the Header tank feeding a route that cuts the pump when wet (Dark at High Tide), then pumping stops within 0.5 s of the set depth | `WaterPumpTests.ALevelSwitchStopsThePump` | switch unwired: it pumps to the rim | — |
| M9 | Given `supply` Electrical, `DriveIn` refuses a Mechanical link; given Shaft, `PowerIn` refuses a wire; a signal wire to `PowerIn` is always refused; an option index outside 0 to 1 is refused whole | `WaterPumpTests.OnlyTheSelectedSupplyPortIsAvailable` | the selected port admits its link | — |
| M10 | Given a run, then Reset restores volumes, rotor and Battery charge; save and load keep `supply`, the pose, both joins and the wire | `WaterPumpTests.ResetAndSaveLoadKeepThePumpAndItsSupply` | — | — |

**Later-partner rows this slice runs.** "Cycling cannot create energy" for [EL-002](EL-002-header-tank.md): `WaterPumpTests.ALiftCycleSpendsAtLeastTheEnergyItStores` (the Header tank drains back to the Water tank and is pumped up again; total volume constant, Battery charge only falls). EL-240 row X6 and EL-229 row Y7 (wheel → Mechanical link → pump loops run down) run here if those parts are delivered, otherwise in the later of the two slices. Generic cover: W22, W23. Chrome recipe (pending): a Water tank, pump, riser and Header tank, with a Battery wired to the pump.

## Open questions

All resolved; none needs the owner.

1. Two inputs or two pump kinds: one part with a `supply` Choice, the delivered Fan's form; the unselected port is unavailable, so both can never be connected.
2. Curve, efficiency and rating: the head cap and power cap of L15 with 3.0 m, 0.5 and 30 W. The earlier 15 W rating with a linear curve is replaced; the Battery covers it (above).
3. Unpowered pump open or closed: open, adding no head (coordinator decision 16).
4. Shaft torque cap: 4 N·m, so that a Waterwheel can turn it and a Motor (20 N·m) is not loaded beyond 30 W.

## Integration

- **Works with.** Delivered: Battery, Motor, wires, mechanical connections. Planned: EL-001, EL-002, EL-008 to EL-010, EL-018, EL-019, EL-229, EL-240.
- **Campaign.** Chapter 7, levels 61–70 (Up and Over, Prime Time, Dark at High Tide); reuse 71–90, 114, 126–130, 136–150. Needs the lesson-fixture story (S8).
- **Coverage binding (planned, not yet live).** A binding section in the form main's coverage reader reads is written by the slice only once S7 allows, as EL-001 describes, with two mode records (Supply=Electrical, Supply=Shaft); S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15). **Blocked on the coverage follow-on story S7** (umbrella `todo-338`; `sequence-task-575` not co-anchored with a todo; no `s443` anchor).

## Delivery

- **Slice shape.** Single element, last of Batch F.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes `ENGINE-SUPPLY-DEMAND`, then: `ENGINE-LIQUID` including a work target; EL-001, EL-002, EL-008, the elbow family; EL-019 and EL-018 for M7 and M8; S7.
- **Files.** Adds `elements/WaterPump/WaterPump.csproj`, `WaterPumpElementDeclaration.cs`, `water_pump.tscn`, `icon.svg`, `CuriousContraptions.tests/WaterPumpTests.cs`; edits `CuriousContraptions.slnx` and this spec.
- **Review tier.** Data-only if both stories are delivered; otherwise the work target is an engine slice first.
- **Disqualifiers.** A work target that needs a pump-specific curve; a debit not tied to work done; a shaft input that only an airflow source can use; an ungranted edge that adds head or blocks flow.
