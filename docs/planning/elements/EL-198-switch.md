# EL-198 · Switch — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy and current citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and may be revised by the owner. Current collider sizes below are half-extents, as the current compiler stores them. Supplied-network facts N1–N21 are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-198 · Switch |
| Type | Electrical |
| Anchor | [requirements.md#element-198](../requirements.md#element-198); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Battery, wire, switch …"); [named-elements entry](../invest/named-elements.md#element-198); owner S304 |
| Related | Refines [CAT-063 Switch](CAT-063-switch.md) ([current-cat-063](../requirements.md#current-cat-063): "ActivationOut and separately supplied PowerIn→Supply remain distinct contracts"); supply from [CAT-005](CAT-005-battery.md)/[EL-196](EL-196-battery.md) via [EL-197](EL-197-electrical-wire.md); conditions for [EL-133](EL-133-electrical-and-gate.md)–[EL-137](EL-137-electrical-nand-gate.md) and [EL-181](EL-181-rising-edge-detector.md). |
| Roadmap story | unscheduled as its own story: the activation half is delivered (CAT-063); the electrical contact is first needed by Story 9.7 ("gates wired to input power switches") |
| Status | partial — impact switch with `ActivationOut` delivered; electrical contact not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Current (delivered): static body with two box colliders, half-extents (0.55, 0.125, 0.5) at (0, −0.15, 0) and (0.4, 0.09, 0.375) at (0, 0.05, 0) (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L111-L115`). |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). |
| Constraints | none; the button travel is cosmetic. |
| Typed ports | Current: `ActivationOut` (Activation, Output) at (0.45, 0, 0.4) (`engine/gpu/WorkshopConnections.cs@a6c914e:L32-L35`, `engine/gpu/WorkshopConnections.cs@a6c914e:L49-L49`). New: `PowerIn` (Electrical, Input) at (−0.58, 0, 0) and `Supply` (Electrical, Output) at (0.58, 0, 0) **proposed** — exactly at the two navy terminal spheres the art already has (`parts/SwitchPart.cs@a6c914e:L12-L13`; [DESIGN.md](../../../DESIGN.md#motion-and-state-feedback): "two navy supply terminals"). |
| Sensors and activation | Physical variant: a contact trigger latches once per run when an approaching body exceeds the authored threshold (`ContactTriggerSettings`, default 0.8 m/s, range 0–64 m/s, `engine/gpu/WorkshopActivationParts.cs@a6c914e:L5-L10`), emitting one `ActivationOut` (`engine/gpu/ActivationNetwork.cs@a6c914e:L135-L157`). Contact state = closed iff latched (variant table). |
| Work and energy stores | none. The contact passes upstream supply `PowerIn` → `Supply` only while closed; a closed contact with no upstream supply passes nothing (outcome; legacy contact rule `engine/BinaryCircuit.cs@a6c914e:L134-L135`). |
| Parameters | `trigger` threshold (current, `engine/gpu/WorkshopActivationParts.cs@a6c914e:L6-L9`). `mode`: closed enum `SwitchMode { Impact, Fixed }`, default Impact **proposed** (keeps the delivered behaviour as default). `fixed_state`: closed enum `ContactState { Open, Closed }`, default Open, used only when `mode` = Fixed **proposed** (an author-set contact for lessons that need a known open/closed path). |
| Cosmetic curves and UI bindings | Current curve: Activation source, SmoothStep, 0.16 s (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L53-L53`); button LocalY 0.06 → −0.02 and albedo catalogue colour → pressed `#bff5b0` (`parts/SwitchPart.cs@a6c914e:L15-L22`). The Fixed variant shows its authored state statically (pressed colour when Closed) **proposed**. |
| Art | Navy `#293954` terminal spheres r 0.08 at (±0.58, 0, 0); base 1.1 × 0.25 × 1 in `#2c3a4c` at y −0.15; button cylinder r 0.38, h 0.18 in catalogue colour `#f06e54`; small lamp sphere `#ffd899` r 0.09 at (0.45, 0, 0.4) (`parts/SwitchPart.cs@a6c914e:L11-L16`). Toolbox icon `ui/WorkshopIcons.cs@a6c914e:L52-L52`. |
| Catalogue / inventory | Id `switch`, Title "Impact switch", Category Power, colour (0.94, 0.43, 0.33), no parameters; Description "A qualifying impact presses the button and sends one activation to connected signal parts." (`parts/catalog/switch.tres@a6c914e:L8-L14`). The description gains the electrical contact once delivered; `switched_motor` grants 1 switch (`content/puzzles.json@a6c914e:L3580-L3590`). |

**Variants** (row: "Physical or author-set contact state"):

| Variant | `mode` | Contact closed when | Activation |
| --- | --- | --- | --- |
| Physical | Impact | from the tick the impact latch fires until Reset | one `ActivationOut` at the latch |
| Author-set | Fixed | `fixed_state` = Closed for the whole run (Open: never) | none (no impact sensing) **proposed** |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S304): ElectricalPower, FiniteLedger, SignalPropagation (+ StateTransaction).

**Exists now**
- Impact switch body, contact trigger, activation latch and cosmetic curve: `engine/gpu/WorkshopActivationParts.cs@a6c914e:L12-L22`, `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L111-L117`, `engine/gpu/ActivationNetwork.cs@a6c914e:L135-L157`.

**Missing**
- Electrical sockets on the switch and a contact element whose closed state is the committed activation latch — Story 8.1 network plus a switch slice before Story 9.7; typed roles [S257](../invest/decisions.md#s257).
- `SwitchMode`/`ContactState` configuration and UI — this identity.

**Dependencies.** Battery (CAT-005/EL-196), wires (EL-197), a supplied load (Motor CAT-042).

## 4. Sources and legacy

- Requirement row [element-198](../requirements.md#element-198): "Physical or author-set contact state opens or closes an electrical path"; outcome "Closed contact cannot create supply energy". [current-cat-063](../requirements.md#current-cat-063): missed/gentle/duplicate impacts, lost/missing supply, coherent network cycles, direct-wire-bypass lesson; Reset restores unpressed state and both domains.
- Named entry [element-198](../invest/named-elements.md#element-198), owner S304.

| # | Legacy fact | Source | Disposition |
| --- | --- | --- | --- |
| S1 | A contact element (input → output, open or closed each tick) passes power only when closed and its input is powered. | `engine/BinaryCircuit.cs@a6c914e:L17-L17`, `engine/BinaryCircuit.cs@a6c914e:L134-L135` | carry forward |
| S2 | Contact conditions are typed (owner active, boolean state, counter reached, latch on, …) and must bind a capability the owner declares. | `engine/SceneElectricalContact.cs@a6c914e:L8-L8` | carry forward (switch: owner active = latched) |
| S3 | `switched_motor`: switch under a falling ball; battery `supply` → switch `power_in`, switch `supply` → motor `power_in`; hint "A switch controls electricity but does not create it." | `content/puzzles.json@a6c914e:L3580-L3586`, `content/puzzles.json@a6c914e:L3857-L3872` | carry forward (Epic 15 input) |
| S4 | Bypass control: wiring battery → motor directly turns it and the switch still activates, but the "powered after switch" goal fails; removing any solution wire leaves the motor unpowered. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L42-L94` | carry forward |
| S5 | The legacy `SwitchPart` holds art only; its ports and colliders already moved to the current declarations. | `parts/SwitchPart.cs@a6c914e:L6-L23` | carry forward the art; geometry is current |

- **Files harvested:** `parts/SwitchPart.cs`, `parts/catalog/switch.tres`, `engine/BinaryCircuit.cs`, `engine/SceneElectricalContact.cs`, `CuriousContraptions.tests/ElectricalTests.cs`, `content/puzzles.json` (switched_motor).

## 5. Acceptance outline

Acceptance authority: [element-198](../requirements.md#element-198), [current-cat-063](../requirements.md#current-cat-063).

- **Construction (actual Chrome UI).** Battery → switch `PowerIn`; switch `Supply` → Motor `PowerIn`; a ball above the switch (the `switched_motor` layout).
- **Positive.** Physical: the ball strikes above threshold, the button latches and the motor turns from that tick. Author-set Closed: the motor turns from tick 1.
- **Negative / control.** No battery wire: the latched switch powers nothing (outcome). Gentle impact below threshold, missed ball: contact stays open. Author-set Open: never powers. Direct battery → motor bypass fails the sequenced goal (S4).
- **Boundaries.** Threshold exactly at and just below 0.8 m/s; duplicate impacts latch once; supply lost after latching clears the motor's power.
- **Run/Reset.** Reset restores the unpressed button, open contact and unpowered network exactly.
- **Save/Load.** `mode`, `fixed_state`, threshold and both electrical wires round-trip.
- **Integrations.** CAT-063 retained behaviour [todo-147](../requirements.md#todo-147) (`switched_motor`, direct-wire-bypass control) and battery retained behaviour [todo-146](../requirements.md#todo-146); the contact feeds gate conditions [todo-362](../requirements.md#todo-362) and edge detectors ([EL-181](EL-181-rising-edge-detector.md)); interaction processes [IX-06 electrical power transfer](../requirements.md#interaction-06) (passes, never creates, supply) and [IX-07 signal propagation](../requirements.md#interaction-07) (`ActivationOut`). Campaign: "switch" is named in the power row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 11–20 (reuse 31–50, 61–80, 136–150).

## 6. Open questions

1. **Author-set form.** A `mode` on the impact switch (proposed) versus a separate toggle-switch catalogue entry: owner decision.
2. **Runtime toggling.** Whether players may flip an author-set switch during Run: owner decision (see CAT-005 open question 4).
