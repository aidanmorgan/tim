# EL-230 · Spring-return pneumatic piston — named-identity spec

**Current delivery (21 November 2026; propagation repair 23 November 2026):** [EL-230 ledger entry](../invest/named-elements.md#element-230), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-230 · Spring-return pneumatic piston · Pneumatic |
| Requirement anchor | [todo-426](../requirements.md#todo-426) (`sequence-task-394`) |
| Ledger entry / owner | [element-230](../invest/named-elements.md#element-230); slice `named-el-230`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a) and `ENGINE-GAS-NETWORK`.** Delivery batches 1c, "pneumatic pistons", after EL-039, EL-038, EL-040 and EL-041 (`readiness/batch-h.md` order). **Not a Family slice with EL-231:** the two differ in ports (one chamber port against two) and in a spring, not only in declared values. They are two consecutive slices sharing one probe. |
| Planned package | id `pneumatic_piston`, directory `elements/PneumaticPiston/` |
| Related | [EL-231 Double-acting pneumatic piston](EL-231-double-acting-pneumatic-piston.md), [EL-039 Air reservoir](EL-039-air-reservoir.md) (the pneumatic conventions F1–F11), [EL-038 Pneumatic hose](EL-038-pneumatic-hose.md), [EL-041 Directional valve](EL-041-pneumatic-directional-valve.md), [EL-040 Release valve](EL-040-pneumatic-release-valve.md), [EL-045 Air whistle](EL-045-air-whistle.md), [CAT-010 Bellows](CAT-010-bellows.md), [TH-19 Steam piston](TH-19-steam-piston.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SOA-SIMD` (the flat tables the chamber compiles into), `ENGINE-CAPABILITY-INSTANCES` (declared port lists) and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-GAS-NETWORK` (owner decision 18; it owns the one fluid port contract, coordinator decision 16, and the axial chamber, engine design audit §6 item 13); then EL-039, EL-038 (itself blocked on ENGINE-CONNECTION-MATERIALS (solid strand), owner decision 1) and EL-041. |

## 2. Declaration

Nearest delivered shape: the Powered gate's sprung slider (`elements/PoweredGate/PoweredGateElementDeclaration.cs:76`), with the supplied drive replaced by chamber pressure.

- **Interfaces.** `ISliderElement` (`MechanicalContracts.cs:15`), `ISceneGeometryElement` (`SceneGeometry.cs:10`), and the chamber and port interfaces `ENGINE-GAS-NETWORK` delivers (it reshapes or deletes `IFluidElement`, `FluidContracts.cs:5`; `readiness/batch-f.md` section 5).
- **Barrel (body 0, static).** Cylinder radius 0.15 m, length 0.9 m, on two feet.
- **Rod and head (body 1).** Rod of radius 0.04 m ending in a pusher disc of radius 0.2 m, thickness 0.06 m; mass 0.5 kg. Slider along the barrel axis, limits 0 to `stroke`; return spring 100 N/m toward 0, damping 8 N·s/m; no drive.
- **Chamber.** One gas chamber behind the piston with effective area 4 × 10⁻⁴ m². Its volume is area × travel plus a 2 × 10⁻⁵ m³ dead volume. At Run start and after Reset it holds air at ambient pressure and the ambient temperature ([EL-039](EL-039-air-reservoir.md) F2).

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `stroke` | Real, m | 0.2–0.8 | 0.6 | Reject, build-only | Diverts a ball across one lane |

| Port | Domain | Direction | Position (m) | Meaning |
| --- | --- | --- | --- | --- |
| `GasIn` | Fluid | Input | (−0.45, 0.15, 0) | The chamber. A hose joins a valve's Output to it (EL-039 F3); flow direction follows pressure, so the same hose supplies and exhausts |

The valve is a separate part. With the directional valve ([EL-041](EL-041-pneumatic-directional-valve.md), its "spring-return use"): `PortA` → `GasIn`, `PortB` unconnected and so Sealed; Set feeds the chamber; Reset joins the chamber to the valve's `Exhaust` socket. The release valve ([EL-040](EL-040-pneumatic-release-valve.md)) latches open and gives one stroke only (EL-039 A9). The piston has no electrical or signal port.

- **Presentation.** Rod body binding; coil as a span between barrel and disc (visible spring); stroke marks on the rod. Reservoir state is shown by the reservoir's own gauge.
- **Palette.** Motion, "Pneumatic piston"; order 156 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| Barrel and feet | Cylinder and boxes, cream `#fff8e9` (owner decision 4) with a sky blue band printed on | Solid, body 0 | nothing (static) |
| Rod and pusher disc | Cylinders, navy `#1c2836` and gold | Solid, body 1 | body 1 pose |
| Stroke marks | Printed flush on the rod | Decorative: printed flat | body 1 pose |
| Return spring | Coil around the exposed rod, within the disc's radius | Decorative: moved by a binding | presentation span |
| Port mark | Ring printed flush on the barrel; the port is a scene locator `Loc_Port_GasIn` with no proud mesh (the pneumatic batch's convention) | Decorative: printed flat | nothing |

Icon: a cylinder with a rod and a coil around it. The coil is what distinguishes it from the double-acting piston (two port marks, no coil).

### Behaviour and numbers

- Units follow the pneumatic conventions ([EL-039](EL-039-air-reservoir.md) F1–F2): pressure is in pascals absolute, ambient 101 325 Pa, the ambient temperature that of the thermal conventions, and a 600 000 Pa reservoir rating.
- Law: rod force = (p − 101 325 Pa) × A − spring. At full stroke the spring gives 60 N, so the rod completes its stroke only above **251 325 Pa absolute** (150 000 Pa over ambient × 4 × 10⁻⁴ m² = 60 N). At 400 000 Pa it pushes with (400 000 − 101 325) × 4 × 10⁻⁴ = 119.5 N less the spring. Pressure is a quantity, not an on/off flag: at 200 000 Pa the force is 39.5 N and the rod stops where the spring matches it, 0.39 m out.
- Each full stroke takes about 2.4 × 10⁻⁴ m³ of chamber volume from the reservoir and does at most p × ΔV of work (about 60 J at 251 325 Pa), so a small reservoir is visibly depleted over repeated strokes.
- Exhaust: when the valve vents the chamber (EL-041 Reset), the spring returns the rod and the gas leaves through the valve's `Exhaust` socket; a hose from `Exhaust` to an [EL-045](EL-045-air-whistle.md) `GasIn` sounds the whistle on each return. An unconnected `GasIn` is Sealed (EL-039 F4): the chamber keeps its ambient start pressure and nothing moves.
- Spring rest position: coordinator decision 25 (the declared rest position of owner decision 6) was checked and does not apply: this is a piston rod, not a blade, hatch or gate that must close, and every row mounts it horizontally beside a lane.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Sprung slider that returns when unloaded | present | `PoweredGateElementDeclaration.cs:76` | `DeclaredPrismaticDriveTests.ThePoweredGateOpensToItsTargetWithinItsDeclaredSpeedEffortAndPowerOnEverySubstep` |
| A fluid port contract and one gas store per body | narrower | ports declared with no consumer (`FluidContractValues.cs:12`); one isobaric store per body (`GasStoreDescriptor.cs:9`), not joined to others | none for a network |
| **Stores joined by lines, valves, and chamber pressure doing work on a prismatic coordinate paid from the store** | **missing** | — | Owner: `ENGINE-GAS-NETWORK` (its acceptance: "a declared chamber drives a declared prismatic or revolute coordinate through pressure work paid from the store"; it also takes the axial chamber, engine design audit §6 item 13) |

Counts: 1 present, 0 untested, 1 narrower, 1 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static barrel, one sprung passive slider, one gas chamber on that coordinate and one fluid port. Extension is the pressure-work law (p − ambient) × A on the declared area; the return is the spring law; depletion is the gas network's conserved transfer. The missing capability is stated for any package: stores joined by lines and valves, and chamber pressure doing paid work on a declared coordinate, owned by `ENGINE-GAS-NETWORK`. No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 3 cylinders (barrel, rod, pusher disc) and 2 boxes (feet) as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: the coil is a presentation span between barrel and disc; the slider's declared spring, not the coil, carries the load. What moves each art node is in the art table.
- **Flat state.** 2 bodies; 5 colliders (3 cylinder, 2 box); 1 prismatic joint; 1 chamber; 1 fluid port; 0 drives; 0 sensors; 0 signal nodes; 1 body-pose binding, 1 span.
- **Performance.** The Powered gate's primitive count or fewer; the port collar was redrawn flush so it needs no collider. The barrel is static. Whether a vented piston at rest may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-426](../requirements.md#todo-426): "Finite compressed-air supply and valve extend rod; visible spring returns it on exhaust. Show stroke and reservoir state. Do not equate pressure with an electrical on/off flag."
- **Campaign.** Chapter 8 (71–80); the ledger records first use 71–80.
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: rod position ±0.02 m; pressure and network gas mass as [EL-039](EL-039-air-reservoir.md) F6 (pressure within 2000 Pa or 2 %, mass within 0.5 %); stall guard 20 s. Every row is a build of a tank (EL-039), hoses (EL-038) and the directional valve (EL-041) as in section 2.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| U1 | Reservoir at 400 000 Pa → valve → piston beside a ball lane; Set | The rod extends its full stroke and diverts the ball | Control: no Set, no motion | `SpringReturnPistonTests.PressureExtendsTheRod` |
| U2 | Reservoir at 200 000 Pa | The rod stops part-way, within 0.03 m of the balance point (0.39 m) | Boundary: 260 000 Pa completes the stroke; 240 000 Pa stops short of it | `SpringReturnPistonTests.InadequatePressureGivesAPartialStroke` |
| U3 | After U1, Reset (EL-041 D-A15) | The spring returns the rod; the gas leaves through `Exhaust`; with `Exhaust` hosed to a Whistle, each return sounds it | Control: no Reset, the rod stays out; no exhaust hose, no whistle; `exhaust` Blocked, the rod does not return | `SpringReturnPistonTests.ResetVentsTheChamberAndTheSpringReturnsTheRod`; the whistle clause, `SpringReturnPistonTests.TheExhaustSoundsAWhistleOnEachReturn`, is a cross-part row owned by the EL-045 slice (the whistle is taught at 77a) |
| U4 | A Wall in the stroke | The rod stops at it, no penetration; pressure holds | — | `SpringReturnPistonTests.ABlockedStrokeStalls` |
| U5 | Set and Reset alternate from a small reservoir (EL-039 A9, EL-041 D-A15) | Each stroke lowers the reservoir pressure; strokes stop completing below 251 325 Pa | Control: an ambient tank moves nothing | `SpringReturnPistonTests.RepeatedStrokesDepleteTheTankUntilTheLoadIsNotReached` |
| U6 | `GasIn` unconnected; Set | No stroke (a Sealed port) | — | `SpringReturnPistonTests.AnUnconnectedChamberGivesNoStroke` |
| U7 | Any run | Gas in reservoir + lines + chamber + vented total is constant | — | `SpringReturnPistonTests.GasIsAccountedFor` |
| U8 | Run, Reset; Save, Load; an electrical wire to `GasIn`; a hose from `GasIn` to another Input; `stroke` 0.1 | Restored; the three refused with a typed reason | Boundary: `stroke` 0.2 and 0.8 admitted | `SpringReturnPistonTests.ResetReloadAndRefusals` |

**Integration.** Bellows (CAT-010, in flight), EL-037 to EL-043 pneumatic kit, EL-045 whistle (none delivered); EL-041 rows D-A15 run in this slice and are U3 and U5. Hoses are solid strands (owner decision 1); their collider is EL-038's, owned by ENGINE-CONNECTION-MATERIALS.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `pneumatic_piston`; Named identity EL-230; Acceptance todo-426 and sequence-task-394; owner slice `named-el-230`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g). Levels need S8.

**Delivery.** Prerequisites, in order: blocked on the engine programme (owner decision 3a), in particular `ENGINE-SOA-SIMD`, `ENGINE-CAPABILITY-INSTANCES` and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-GAS-NETWORK` (owner decision 18); then EL-039, EL-038 (after ENGINE-CONNECTION-MATERIALS (solid strand)) and EL-041; the coverage reader admitting a slice-owned package. One element slice. Files: the `elements/PneumaticPiston/` package, `CuriousContraptions.tests/SpringReturnPistonTests.cs` (the class name EL-039 and EL-041 already cite), and the hand-edited lists until S3 (S2 is published at `25df3cd5`; no test-project list edit). Review tier: engine. **Disqualified if** the gas story fixes a different pressure scale; the area is then re-chosen so the stroke completes at about 40 % of the reservoir rating.

## 6. Open questions

1. *Family slice with EL-231?* Resolved: no (ports and spring differ).
2. *Built-in valve.* Resolved by D19: none; the pneumatic valves are EL-040 and EL-041.
3. *Pressure units.* Resolved by the coordinator under the standing delegation (22 November 2026): pascals absolute with ambient 101 325 Pa everywhere, as the pneumatic batch (H) fixed in EL-039 F2; the earlier figures on another scale with a 100 000 Pa ambient are replaced.
4. *Port name and direction.* Resolved by the pneumatic conventions (EL-039 F3; hand-back in `readiness/batch-h.md` section 6 and `readiness/batch-n.md` item 8): one Fluid Input `GasIn`; the earlier bidirectional `ChamberPort` is withdrawn: although `PortDirection` has a `Bidirectional` member (`CuriousContraptions.Abstractions/PortDescriptor.cs:9`, used by rope ports), the gas family joins one Output to one Input and lets pressure set the flow direction (EL-039 F3 and its open question 2).

Needs the owner: nothing.
