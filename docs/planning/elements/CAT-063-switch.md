# CAT-063 · switch (Impact switch) — declaration readiness spec

**Current delivery (24 October 2026):** [CAT-063 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Colliders (owner, 25 October 2026, "visible = solid"):** every part a player sees as solid collides with its true shape. Since [`elements-visible-solid`](../../../_bmad-output/planning-artifacts/epics.md#story-elements-visible-solid) the button collides as a cylinder (r 0.38, h 0.18 at y 0.06), with the base box (`elements/ImpactSwitch/switch.tscn`); the box (0.8 × 0.18 × 0.75 at y 0.05) whose corners reached 0.55 m from the axis is deleted. The contact trigger stays keyed to the whole body: a qualifying impact anywhere on the part (base, button, terminals or socket) fires it (decision, 25 October 2026; tested by a drop onto a terminal); a ball falling where only the old box's corner reached touches nothing and fires nothing (`VisibleSolidColliderTests`). The two terminals (spheres r 0.08 at x ±0.58, reaching about 0.11 m past the base) and the activation socket (sphere r 0.09 at (0.45, 0, 0.4)) collide as drawn (coordinator decision, 25 October 2026). Static restitution 1 is this doc's value and the neutral one under the product pair law, tested by a drop onto the button.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-063 · `switch` (display name Impact switch) |
| Requirement | [CAT-063](../requirements.md#current-cat-063) |
| Mapped identities | EL-198 Switch |
| Roadmap | [CAT-063-I and CAT-035-I](../invest/vertical-delivery.md#switch-lamp) (activation edge delivered); Story 4.2 (button cosmetic); electrical PowerIn → Supply pass-through needs Story 8.1 (Battery and network graph) |
| Status | ActivationOut has terminal native scoped Pass; separately supplied PowerIn → Supply is implemented with native proof in Story9.5b, pending publication review. Chrome qualification remains post-catalogue. |
| Levels | placed in 19 levels, inventory in 11; see [CAT-063-I consumers](../invest/current-consumers.md#cat-063-i) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Bodies and shapes | Harvest baseline: one static body, two static boxes: base centre (0, −0.15, 0) half (0.55, 0.125, 0.5); button centre (0, 0.05, 0) half (0.4, 0.09, 0.375) `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L111-L118`. Current: the base box and the button cylinder (Colliders above). |
| Material | restitution 1, bounce threshold 0.1 m/s, friction 0.3 (inherited static default) `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`. |
| Constraints and joints | none; the button does not move physically (its depression is cosmetic). |
| Sockets and ports | Declared ActivationOut (Signal Output Pulse) at (.45,0,.4), PowerIn (Electrical Input) at (−.58,0,0), Supply (Electrical Output) at (.58,0,0). Gold terminals and scene locators match the package ports. Source: elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs and switch.tscn. |
| Sensors and activation | One `ContactTriggerDeclaration` on the owner body, qualified by pre-response normal approach speed ≥ threshold `engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L7-L18`; targets all dynamic bodies in Free workshop, the named ball in a puzzle `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L144-L146`. Latches once per Run. |
| Work and energy stores | none. |
| Parameters | Current `trigger_threshold` is declared f32, 0–64 m/s, default 0.8 in `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs`; generic authoring/save/compiler use that parameter. Finite interactive editor values clamp; invalid external vectors reject atomically. Historical authored knots 0.2 / 0.47 / 0.8 at precision 0 / 0.45 / 1 `engine/gpu/WorkshopPuzzle.cs@a6c914e:L102-L105`. |
| Cosmetic curves and UI bindings | Activation source, smoothstep, 0.16 s `engine/gpu/WorkshopCosmetic.cs@a6c914e:L53-L53`; button local Y 0.06 → −0.02, albedo part colour → `#bff5b0` `parts/SwitchPart.cs@a6c914e:L17-L22`. Design rule `DESIGN.md@a6c914e:L276-L276`. |
| Art | `parts/SwitchPart.cs@a6c914e:L9-L23`: navy `#2c3a4c` base 1.1 × 0.25 × 1 at y −0.15, two `#293954` end studs r 0.08 at x ±0.58, button cylinder r 0.38 h 0.18 at y 0.06, gold `#ffd899` socket sphere r 0.09 at (0.45, 0, 0.4); pick radius 0.65. Scene `parts/scenes/switch.tscn`. Palette Switch `#f06e54` `DESIGN.md@a6c914e:L173-L173`. Icon `ui/WorkshopIcons.cs@a6c914e:L52-L52`. |
| Catalogue and inventory | `parts/catalog/switch.tres@a6c914e:L6-L14`: id `switch`, title Impact switch, category Power. |

Current tests: `CuriousContraptions.tests/ContactActivationTests.cs` (two boxes, trigger ownership, single latch, disconnected/absent contact, stale occurrence rejection), `ActivationTimerTests.cs`, `WorkshopActivationAnimationTests.cs`, `BasketballResourceTests.cs` (button binding); Chrome `tools/e2e/anim-1b.test.ts` (depression curve, unconnected lamp stays neutral).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, ElectricalPower, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SignalPropagation, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| ContactImpulse with approach-speed trigger | `engine/gpu/ContactTriggerDeclaration.cs`; `CuriousContraptions.Simulation/wwwroot/worker.js` |
| ElectricalPower: declared supplied contact, coherent source-seeded chains/cycles, supply loss/restoration without erasing activation memory | engine/gpu/SupplyRoute.cs; Story9.5b native SwitchElectricalTests |
| SignalPropagation (activation network) | `engine/gpu/ActivationNetwork.cs`, `engine/gpu/WorkshopActivationWire.cs` |

| Missing | Story that builds it |
| --- | --- |
| Motor/campaign electrical integration: shaft momentum on supply loss and switched_motor/gated_belt sequencing/direct-wire-bypass lesson | Named Motor/campaign integration; current9.5b native gate proof does not close these future-consumer outcomes |
| Remaining numeric lanes: compiled box/pose/material and physical event phase; lesson assistance knots | Story9A.44. C-Switch moves declared threshold and contact/cause/report speed to f32; no end-to-end f32 qualification claim. |

Dependencies: a dynamic body to strike it; Battery (CAT-005) for the electrical contract; Signal lamp (CAT-035), Delay (CAT-022) and others as activation targets.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | switched_motor: Battery Supply → switch PowerIn, switch Supply → motor PowerIn; goals Turned(motor) and PoweredAfter(motor, switch_1); Bowling striker at (−2, 5, 0) above the switch at (−2, 1, 0). | `tools/Campaign/Program.cs@a6c914e:L133-L149` | carry forward as the electrical pass-through lesson. |
| 2 | The switched lessons replay identically after Reset; a direct battery → motor bypass turns the motor and activates the switch but does not win (sequencing is checked); removing any wire fails and leaves the motor unpowered. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L42-L94` | carry forward (direct-wire-bypass lesson). |
| 3 | Closed relay chains (supply → switch 1 → switch 2 ↔ switch 1 → motor) propagate regardless of identity order; a loop alone never supplies; supply off clears both switches and the motor; supply back on restores it; Reset clears every part. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L137-L181` | carry forward (coherent network cycles). |
| 4 | A closed contact still needs upstream supply; supply loss clears power without deleting the motor's shaft momentum. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L183-L215` | carry forward. |
| 5 | Gated modules: switch at (−5.8, 1.6, 0) under a Bowling trigger at (−5.8, 2.4, 0); in gated_belt the switch is wired battery → switch → motor (electrical); in gated_air the switch's ActivationOut drives the fan. | `tools/Campaign/Program.cs@a6c914e:L64-L91` | carry forward (shows both domains on one part). |
| 6 | Signal modules replace the receiver with a switch and wire receiver ActivationOut → lamp ActivationIn; goal Activated(lamp). | `tools/Campaign/Program.cs@a6c914e:L53-L63` | carry forward. |
| 7 | Typed switch → lamp connection: a duplicate connection is rejected; activation lights the lamp and Reset clears it; ports are ActivationOut → ActivationIn; foreign parts and undefined sockets cannot connect; an invalid socket never activates. | `CuriousContraptions.tests/ConnectionPortTests.cs@a6c914e:L139-L185` | carry forward. |
| 8 | Contact probe fixtures: drop (ball from y 3 above the switch at y 1), miss (x 2), gentle (y 1.481), approach speed just below, equal to and just above the 0.8 m/s threshold, and a base-box-only hit at x 0.85. | `reference/switch-lamp/Program.cs@a6c914e:L20-L55` | carry forward the fixture matrix (threshold is inclusive at equal); do not carry forward the half-precision adjacent-bit construction of below/above speeds (f16 values). |
| 9 | Authoring defaults: switch rotation step 0° (no rotation correction); trigger threshold 0.2 / 0.47 / 0.8. | `tools/Campaign/Program.cs@a6c914e:L544-L562` | carry forward the values; do not carry forward the string `part.Kind` switch. |
| 10 | power_trip reference: Bowling at (−3, 6, 0), lamp at (3, 1, 0), switch_1 at (−3, 1, 0) with switch_1 ActivationOut → lamp ActivationIn; goal Activated(lamp). | `content/puzzles.json@a6c914e:L276-L494` | carry forward (content survives). |

Files harvested:
- `tools/Campaign/Program.cs`
- `CuriousContraptions.tests/ElectricalTests.cs`
- `CuriousContraptions.tests/ConnectionPortTests.cs`
- `reference/switch-lamp/` (Program.cs; inputs.json and csproj hold no further knowledge)

## 5. Acceptance outline

Acceptance: [CAT-063](../requirements.md#current-cat-063) and [retained behaviour](../requirements.md#todo-147).

- **Construction.** Place Impact switch and Signal lamp; Connect switch ActivationOut → lamp ActivationIn; drop a ball on the button. Electrical: Battery Supply → switch PowerIn, switch Supply → motor PowerIn.
- **Positive.** A qualifying impact latches once; lamp lights; with supply, the load is powered after the latch.
- **Negative/control.** Missed ball, gentle impact below threshold, duplicate impacts, disconnected wire, wrong domain, missing or lost supply, direct-wire bypass not winning.
- **Boundaries.** Approach speed just below / equal / above the threshold; base-box-only contact.
- **Run/Reset, Save/Load.** Reset restores the unpressed button, both domains and the activation cursor; connections persist through Save/Load.
- **Integrations.** Activation, signal and timed-logic connection audit [sequence-task-279](../requirements.md#sequence-task-279) (switch) and electrical routing [sequence-task-278](../requirements.md#sequence-task-278) (the switched pass-through, distinct from a source); [IX-01 contact impulse](../requirements.md#interaction-01), [IX-07 signal propagation](../requirements.md#interaction-07) and [IX-06 electrical power transfer](../requirements.md#interaction-06); campaign first use 11–20 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (battery, wire, switch row). Partners: Signal lamp (CAT-035), Delay (CAT-022), Battery (CAT-005, Story 8.1), Motor (CAT-042).
- **Remaining qualification/integration.** Chrome construction and visual qualification, power_trip / switched_motor and gated Motor/fan campaign integration; Motor shaft momentum and direct-wire-bypass goal semantics remain at their named deliveries. Current native gate tests prove supplied contact, chains/cycles, both domains, source loss/restoration and Reset/save/load.

## 6. Current decisions

- C-Switch preserves owner-body qualification: either base or button contact at or above threshold qualifies. Native production-engine controls prove base-only contact and adjacent f32 threshold outcomes.
- Activation and electrical modes use one generic declared instance. A qualifying impact latches once; missing/lost supply clears electrical power but not the activation/contact latch. Restoration powers a still-closed contact; Reset clears both.
- Declared SwitchedContact PowerIn admits fan-in for sourced cycles. Duplicate/wrong-domain links still reject; other consumers retain one immediate supplier. Cycles never create power. Finite-work storage keeps one direct source and explicitly rejects unsupported indirect/nonunique accounting; no invented voltage or MNA model.
- Native functional acceptance does not close post-catalogue Chrome qualification or unavailable Motor/campaign outcomes.
