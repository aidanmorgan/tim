# EL-231 · Double-acting pneumatic piston — named-identity spec

**Current delivery (21 November 2026):** [EL-231 ledger entry](../invest/named-elements.md#element-231), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-231 · Double-acting pneumatic piston · Pneumatic |
| Requirement anchor | [todo-427](../requirements.md#todo-427) (`sequence-task-395`) |
| Ledger entry / owner | [element-231](../invest/named-elements.md#element-231); slice `named-el-231`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a), `ENGINE-GAS-NETWORK`, EL-041 and EL-230.** Not a Family slice with EL-230 and not a duplicate of it: it has two chamber ports, no spring, and is driven in both directions. |
| Planned package | id `double_acting_piston`, directory `elements/DoubleActingPiston/` |
| Related | [EL-230 Spring-return pneumatic piston](EL-230-spring-return-pneumatic-piston.md) (shares barrel, rod, area and units), [EL-041 Pneumatic directional valve](EL-041-pneumatic-directional-valve.md), [EL-039 Air reservoir](EL-039-air-reservoir.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SOA-SIMD` (the flat tables the chambers compile into), `ENGINE-CAPABILITY-INSTANCES` (declared port lists) and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-GAS-NETWORK` (owner decision 18; the one fluid port contract, coordinator decision 16; two chambers on one coordinate, `readiness/batch-n.md` section 6 item 4); then EL-039, EL-038 (itself blocked on ENGINE-CONNECTION-MATERIALS (solid strand), owner decision 1), EL-041 and EL-230. |

## 2. Declaration

As [EL-230](EL-230-spring-return-pneumatic-piston.md) section 2, with these differences only:

- **No spring.** The slider has zero stiffness; damping 8 N·s/m is kept.
- **Two chambers,** one each side of the piston: cap side with area 4 × 10⁻⁴ m², rod side with area 3.5 × 10⁻⁴ m² (the rod takes the difference). Each has a 2 × 10⁻⁵ m³ dead volume; their volumes change in opposite senses with travel.
- **Interfaces.** `ISliderElement` (`MechanicalContracts.cs:15`), `ISceneGeometryElement` (`SceneGeometry.cs:10`), and the gas chamber and port interfaces from `ENGINE-GAS-NETWORK`.
- **Bodies.** Barrel radius 0.15 m, length 0.9 m (body 0); rod of radius 0.04 m with a pusher disc of radius 0.2 m, mass 0.5 kg (body 1). Slider limits 0 to `stroke`.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `stroke` | Real, m | 0.2–0.8 | 0.6 | Reject, build-only | The same range as EL-230, so the two are interchangeable in a lane |

| Port | Domain | Direction | Position (m) | Meaning |
| --- | --- | --- | --- | --- |
| `ExtendIn` | Fluid | Input | (−0.45, 0.15, 0) | Cap-side chamber |
| `RetractIn` | Fluid | Input | (0.35, 0.15, 0) | Rod-side chamber |

Both are Fluid Inputs, so a hose joins a valve Output to each (EL-039 F3); flow direction follows pressure, so each hose both supplies and exhausts. The names are the ones the directional valve's spec already uses for this part (`EL-041-pneumatic-directional-valve.md:78`, `:150`: "hoses `PortA → ExtendIn` and `PortB → RetractIn`"; the delivered glyph set has Extend and Retract). EL-039 F3 names a part's one inlet `GasIn` and each inlet of a part with more than one by what it serves, as the valve has `PortA` and `PortB` for two outlets; this piston's two chamber inlets are therefore `ExtendIn` and `RetractIn`, and a single-inlet part such as EL-230 keeps `GasIn`.

The directional valve that swaps supply and exhaust between the two ports is EL-041 (RouteA: `GasIn` → `PortA` → `ExtendIn`, `RetractIn` → `PortB` → `Exhaust`; RouteB the reverse); a Clock and logic command the valve, not the piston. An unconnected chamber port is Sealed (EL-039 F4).

- **Presentation.** Rod body binding; two small chamber indicators whose fill follows each chamber's committed pressure.
- **Palette.** Motion, "Double-acting piston"; order 157 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Barrel and feet | Cylinder and boxes, cream `#fff8e9` (owner decision 4) with two sky blue bands printed on | Solid, body 0 |
| Rod and pusher disc | Cylinders, navy and gold | Solid, body 1 |
| Two port marks | Rings printed flush on the barrel, 0.8 m apart; the ports are scene locators with no proud mesh | Decorative: printed flat |
| Chamber indicators | Flush strips | Decorative: printed flat |

Icon: a cylinder with a rod and two port stubs with opposed arrows. Two port marks and no coil distinguish it from EL-230.

### Behaviour and numbers

- Law: rod force = p_cap × 4 × 10⁻⁴ − p_rod × 3.5 × 10⁻⁴ − p_ambient × 0.5 × 10⁻⁴, pressures in pascals absolute with ambient 101 325 Pa and the ambient temperature of the thermal conventions ([EL-039](EL-039-air-reservoir.md) F2, as EL-230). Both chambers start each Run at ambient. With the cap side at 400 000 Pa and the rod side vented (101 325 Pa): 160 − 35.5 − 5.1 = 119.5 N extending. Swapped: 40.5 − 140 − 5.1 = −104.5 N, retracting.
- Both chambers at the same pressure give a small net extending force from the area difference ((400 000 − 101 325) × 0.5 × 10⁻⁴ = 14.9 N at 400 000 Pa): with both pressurised the rod creeps out against light loads and is held by heavier ones. This is physical and is asserted.
- Mid-stroke reversal: swapping the valve reverses the force at once; the rod decelerates, stops and returns. No position is skipped.
- Each stroke in either direction takes gas from the supply and vents the other chamber; both are counted. A leaking or depleted supply slows and then stops the motion in both directions.
- With both ports shut the trapped gas acts as a spring: the rod can be pushed a little and comes back.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Passive slider with limits and a pusher body | present | `MechanicalContractValues.cs:104` | `LinearPusherTests.RotatedSphericalHeadPushesCargoAndLateralClearanceMisses` |
| A fluid port contract and one gas store per body | narrower | `FluidContractValues.cs:12`; `GasStoreDescriptor.cs:9` | none for a network |
| **Two chambers on one coordinate, each joined by a line to a valve, doing opposed pressure work paid from the store** | **missing** | — | Owner: `ENGINE-GAS-NETWORK`. Its acceptance asks for "at least two differently parameterised declared qualification fixtures"; one of them should be a two-chamber coordinate (batch summary G5; Batch N's proposed text already adds "two chambers on one coordinate", `readiness/batch-n.md` section 6 item 4) |

Counts: 1 present, 0 untested, 1 narrower, 1 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static barrel, one passive slider, two opposed gas chambers on that coordinate and two fluid ports. Motion either way is the pressure-work law on the two declared areas; the creep at equal pressures is the same law with the area difference; trapped gas acting as a spring is the gas state law. The missing capability is stated for any package: two chambers on one coordinate doing opposed paid work, owned by `ENGINE-GAS-NETWORK` (summary gap G5). No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 3 cylinders (barrel, rod, pusher disc) and 2 boxes (feet) as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: two chamber indicators filled by bindings from each chamber's committed pressure. What moves each art node: barrel, feet and port marks nothing (static body 0); rod and disc the body 1 pose; the indicators their presentation bindings.
- **Flat state.** 2 bodies; 5 colliders (3 cylinder, 2 box); 1 prismatic joint; 2 chambers; 0 drives; 0 sensors; 0 signal nodes; 1 body-pose and 2 driver bindings.
- **Performance.** As EL-230; the port collars were redrawn flush so they need no colliders. The barrel is static. Whether a piston with both ports shut may sleep at rest is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. There is no spring, so coordinator decision 25 (spring rest position) does not apply.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-427](../requirements.md#todo-427): "Two typed chamber ports and directional valve drive extension and retraction; distinct from spring return. Account for supply and exhaust in both directions."
- **Campaign.** Chapter 8 (71–80).
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances as EL-230: rod position ±0.02 m; pressure and network gas mass as [EL-039](EL-039-air-reservoir.md) F6; stall guard 20 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| V1 | Reservoir at 400 000 Pa → directional valve → both ports; Set (extend), then Reset (retract) | The rod extends fully, then retracts fully, with no spring | Control: the valve's `GasIn` unconnected, no motion | `DoubleActingPistonTests.PressureDrivesBothDirections` |
| V2 | Both chambers at 400 000 Pa, unloaded; then against a 2 kg load on a slope needing 20 N | Unloaded it creeps out; loaded it is held | — | `DoubleActingPistonTests.EqualPressuresLeaveOnlyTheAreaDifference` |
| V3 | Valve swapped at mid-stroke | The rod reverses without a jump: per-tick travel stays under peak speed × one tick + 0.01 m | — | `DoubleActingPistonTests.MidStrokeReversalIsContinuous` |
| V4 | A Wall in the stroke | The rod stops at it, no penetration | — | `DoubleActingPistonTests.ABlockedLoadStalls` |
| V5 | Repeated cycles from a small reservoir; a supply hose with a Vent end beside it (a leak, EL-038) | Motion slows and stops; it never continues on an empty supply | — | `DoubleActingPistonTests.DepletionAndLeaksStopBothDirections` |
| V6 | Any run | Gas in reservoir + lines + both chambers + vented total is constant, counted for both directions | — | `DoubleActingPistonTests.SupplyAndExhaustAreAccountedForBothWays` |
| V7 | Clock → logic → valve; the piston between two chutes (EL-041 D-A13) | Balls are pushed alternately into the two chutes; mid-stroke reversal reverses without a jump | Control: no clock link | `DoubleActingPistonTests.AClockAlternatesThePusherBetweenTwoChutes` |
| V8 | Run, Reset; Save, Load; an electrical wire to a port; `stroke` 0.9 | Restored; both refused with a typed reason | — | `DoubleActingPistonTests.ResetReloadAndRefusals` |
| V9 | The valve's `exhaust` Blocked, then Vent; the same command (EL-041 D-A5) | Blocked gives a slower or stalled stroke; Vent completes it | Boundary: equal chamber pressures hold still (EL-039 P10) | `DoubleActingPistonTests.ABlockedExhaustSlowsOrStallsTheStroke` |

**Integration.** EL-041 directional valve, EL-039 reservoir, EL-038 hose (none delivered); Clock and the electrical gates (delivered). EL-041 rows D-A5 and D-A13 run in this slice (V9, V7). Hoses are solid strands (owner decision 1); their collider is EL-038's, owned by ENGINE-CONNECTION-MATERIALS.

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `double_acting_piston`; Named identity EL-231; Acceptance todo-427 and sequence-task-395; owner slice `named-el-231`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g). Levels need S8.

**Delivery.** Prerequisites, in order: blocked on the engine programme (owner decision 3a), in particular `ENGINE-SOA-SIMD`, `ENGINE-CAPABILITY-INSTANCES` and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-GAS-NETWORK` (owner decision 18) with two chambers on one coordinate; then EL-039, EL-038, EL-041 and EL-230; the coverage reader admitting a slice-owned package; Batch H's F3 exception for two inlet ids. One element slice. Files: the `elements/DoubleActingPiston/` package, `CuriousContraptions.tests/DoubleActingPistonTests.cs`, and the hand-edited lists until S3 (S2 is published at `25df3cd5`; no test-project list edit). Review tier: engine if this is the first two-chamber consumer. **Disqualified if** the gas story allows only one chamber per coordinate.

## 6. Open questions

1. *Equal areas or a rod-side reduction?* Resolved under the standing delegation: a reduction, as in a real cylinder; it makes "both chambers pressurised" a visible, testable case instead of a dead balance.
2. *Valve inside the piston?* Resolved by D19: no; EL-041 is the valve.
3. *Port names and direction.* Resolved (hand-back in `readiness/batch-h.md` section 6 and `readiness/batch-n.md` item 8): two Fluid Inputs `ExtendIn` and `RetractIn`, the names EL-041 already uses; the earlier bidirectional `ExtendPort` and `RetractPort` are withdrawn: although `PortDirection` has a `Bidirectional` member (`CuriousContraptions.Abstractions/PortDescriptor.cs:9`, used by rope ports), the gas family joins one Output to one Input and lets pressure set the flow direction (EL-039 F3 and its open question 2). EL-039 F3 now states the rule these names follow (each inlet of a part with more than one is named by what it serves).

Needs the owner: nothing.
