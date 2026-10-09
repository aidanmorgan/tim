# EL-196 · Battery — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents (legacy `AddBox` takes the full size and stores half-extents, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). The catalogue harvest (B1–B14, network N1–N21, levels L1–L5) is in [CAT-005](CAT-005-battery.md); this spec adds the finite store the named identity requires.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-196 · Battery |
| Type | Electrical |
| Anchor | [requirements.md#element-196](../requirements.md#element-196); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Battery, wire, switch, motor …", first use 11–20); [named-elements entry](../invest/named-elements.md#element-196); owner S302 |
| Related | Refines [CAT-005 Battery](CAT-005-battery.md) ([current-cat-005](../requirements.md#current-cat-005), retained behaviour [todo-146](../requirements.md#todo-146)). Connected by [EL-197 wire](EL-197-electrical-wire.md); switched by [EL-198](EL-198-switch.md); first consumer [CAT-042 Motor](CAT-042-motor.md). |
| Roadmap story | 8.1 "Battery DC Power Source & Network Graph" (Epic 8); the finite store waits on CAT-005 open question 1 |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static root box 0.85 × 1.05 × 0.75 m at the origin (`parts/BatteryPart.cs@a6c914e:L24-L24`). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`), through `ContactMaterialDeclaration` (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L45`). |
| Constraints | none. |
| Typed ports | `Supply` (Electrical, Output) at (0, 0.62, 0) (`parts/BatteryPart.cs@a6c914e:L16-L17`); `WorkshopSocket.Supply` already exists (`engine/gpu/WorkshopConnections.cs@a6c914e:L9-L9`). No activation input: activation cannot create supply (CAT-005 B6). |
| Sensors and activation | none. |
| Work and energy stores | One finite electrical store (FiniteLedger): charge Q (J), 0 ≤ Q ≤ `capacity`, starting at `initial_charge` × `capacity`. Each committed tick the network's supplied loads declare their power demand; the battery delivers P = min(Σ demand, `power_limit`, Q/dt) and debits P·dt; when Σ demand exceeds what it can deliver, every load's share is scaled by the same α = P/Σ demand (the envelope's proportional rule, [thermodynamic row](../../gpu-f32-physics.md#game-grade-envelope)). Availability at `Supply` = enabled ∧ Q > 0; at Q = 0 every downstream input becomes unavailable on that tick. No recharge source in this identity. Shared consumers never each spend the same balance ([finite stores](../../world-owned-energy-stores.md#reservoir-declaration-and-behaviour)). |
| Parameters | `enabled`: closed enum `BatteryEnable { Disabled, Enabled }`, default Enabled — legacy float `enabled` > 0.5, default 1.0, required (`parts/BatteryPart.cs@a6c914e:L12-L15`, `parts/catalog/battery.tres@a6c914e:L14-L14`), carried as an enum (CAT-005 B2). `capacity`: f32 60–14 400 J, default 3 600 J **proposed** (30 s of the default Motor's 120 W — longer than any ordinary lesson run, so only a deliberately small battery depletes; the upper bound is the Fan's 14 400 J construction reservoir, [finite-gas-foundation](../../finite-gas-foundation.md#sealed-gas)). `power_limit`: f32 10–480 W, default 120 W **proposed** (exactly the default Motor's 20 N·m × 6 rad/s, `parts/catalog/motor.tres@a6c914e:L14-L14`, so one motor runs at full speed and two share). `initial_charge`: f32 fraction 0–1, default 1 **proposed** (full by default; 0 gives the "depleted" control without a new part). |
| Cosmetic curves and UI bindings | Charge gauge: four slate `#556573` → gold `#f7cb52` marks on the cream band, mark i lit while Q ≥ (i + 1)/4 × capacity, 0.1 s SmoothStep — the Solar panel's four-mark meter pattern (`parts/SolarPanelPart.cs@a6c914e:L47-L53`) **proposed** (answers CAT-005's "show committed supply state"; readable without text). |
| Art | Body in catalogue colour `#de7059`; cream `#fff8e9` band 0.87 × 0.25 × 0.77 at y 0.3; gold `#f7cb52` terminal cylinder r 0.16, h 0.14 at y 0.59; navy `#293954` plus sign on the front (`parts/BatteryPart.cs@a6c914e:L24-L28`); selection ring 0.8 (`parts/BatteryPart.cs@a6c914e:L23-L23`). Toolbox icon `ui/WorkshopIcons.cs@a6c914e:L47-L47`. [DESIGN.md Motion and state feedback](../../../DESIGN.md#motion-and-state-feedback) battery paragraph. |
| Catalogue / inventory | Id `battery`, Title "Battery", Category Power, colour (0.87, 0.44, 0.35), Parameters `{"enabled": 1.0}`; Description "Supplies electricity through a wire; activation signals cannot replace a battery." (`parts/catalog/battery.tres@a6c914e:L6-L14`). Inventory: `battery_motor` grants 1; 12 levels place a locked battery (CAT-005 L4). |

**Variants** (CAT-005 configuration "enabled"):

| Variant | `enabled` | Behaviour |
| --- | --- | --- |
| Enabled | Enabled | Supplies while Q > 0, within `power_limit`. |
| Disabled | Disabled | Supplies nothing; Q unchanged. |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S302): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction). Map composition: "Finite ElectricalPower/FiniteLedger charge and power limits; no infinite source inferred from Boolean enabled."

**Exists now**
- Static box body/material: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L45`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`; `Supply`/`PowerIn` sockets and Electrical domain enum values (rejected by validation, `engine/gpu/WorkshopConnections.cs@a6c914e:L89-L92`).

**Missing**
- Binary supplied network — Story 8.1 ([CAT-005 §3](CAT-005-battery.md#3-engine-capabilities)).
- FiniteLedger electrical store with per-load power demand and proportional scaling — owner decision (CAT-005 open question 1), then a story; design owners S019/S020 ([finite stores](../../world-owned-energy-stores.md)).
- Charge-gauge cosmetic source (committed scalar thresholds) — new.
- Typed power role and capacities — [S257](../invest/decisions.md#s257) electrical-port ("a full-capacity attempt refuses the extra wire").

**Dependencies.** A supplied consumer with a declared power demand (Motor CAT-042, Story 11.1); EL-197 wires.

## 4. Sources and legacy

- Requirement row [element-196](../requirements.md#element-196): "Finite declared electrical store supplies connected loads through bounded energy and power"; outcome "Disconnected or depleted source cannot operate a motor". [current-cat-005](../requirements.md#current-cat-005): "Binary enable is not finite-energy qualification … must name capacity/power/work/depletion and affected consumers explicitly; no invented constant supply".
- Named entry [element-196](../invest/named-elements.md#element-196), owner S302.
- Legacy: binary availability only, no capacity or power (`engine/SceneElectricalSource.cs@a6c914e:L10-L10`); all other battery facts are CAT-005 B1–B14. A supplied motor's work allowance per tick is torque × target speed × dt (`parts/MotorPart.cs@a6c914e:L93-L97`) — carry forward as the motor's power demand; do not carry forward the per-part `PreparePhysics` loop.
- **Files harvested:** `parts/BatteryPart.cs`, `parts/catalog/battery.tres`, `engine/SceneElectricalSource.cs`, `parts/MotorPart.cs`, `parts/catalog/motor.tres`, `parts/SolarPanelPart.cs` (meter pattern), `reference/cpu/MachinePart.cs` (`AddBox` extents).

## 5. Acceptance outline

Acceptance authority: [element-196](../requirements.md#element-196), [current-cat-005](../requirements.md#current-cat-005); Story 8.1 `tools/e2e/cat-005.test.ts`.

- **Construction (actual Chrome UI).** Place Battery and Motor; select the battery, Connect, click the motor — the sole compatible pair `Supply → PowerIn` (CAT-005 B7); set `capacity`/`initial_charge` through the contextual configuration control.
- **Positive.** Enabled, charged battery: the motor turns; the gauge marks fall as charge is spent.
- **Negative / control.** No wire: motor still (outcome: disconnected). `initial_charge` = 0, or a small `capacity` that runs out mid-run: the motor stops on the depletion tick and never restarts (outcome: depleted). Disabled: motor still, charge unchanged. Two motors on one battery each get half power when demand exceeds `power_limit`.
- **Boundaries.** `capacity` 60 J and 14 400 J; `power_limit` 10 and 480 W; total energy delivered never exceeds the initial charge.
- **Run/Reset.** Reset restores the authored charge and enable exactly; replay is tick-identical.
- **Save/Load.** `enabled`, `capacity`, `power_limit`, `initial_charge` round-trip; runtime charge is not persisted.
- **Integrations.** CAT-005 retained behaviour [todo-146](../requirements.md#todo-146); switched supply through the switch contact [todo-147](../requirements.md#todo-147) (`switched_motor`); interaction process [IX-06 electrical power transfer](../requirements.md#interaction-06) ("Allocate finite supplied work among loads/storage; disconnected or exhausted sources deliver none", first lesson 11). Campaign: "Battery" is named in the power row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 11–20 (reuse 31–50, 61–80, 136–150).

## 6. Open questions

1. **Finite store values.** Capacity, power limit and initial-charge defaults are proposals answering CAT-005 open question 1: owner decision.
2. **Voltage label.** Story 8.1 says "12V"; the model is energy and power, not voltage (CAT-005 open question 3).
3. **Recharge.** Whether a battery can be recharged (e.g. by a generator or solar panel, EL-211) in a later identity: owner decision.
