# EL-198 · Switch — element readiness spec

**Current delivery (24 October 2026):** [EL-198 delivery slice](../invest/named-elements.md#element-198). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, repaired on 22 November 2026 and reduced to a pointer on 23 November 2026: [CAT-063](CAT-063-switch.md) is the single declaration of the `switch` package and holds this identity's author-set contact variant, its acceptance and its delivery (coordinator decisions 27 and 31, 23 November 2026). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Supplied-network facts N1–N21 are in [CAT-005](CAT-005-battery.md#4-legacy-harvest).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-198 · Switch |
| Type | Electrical |
| Anchor | [requirements.md#element-198](../requirements.md#element-198) ("Physical or author-set contact state opens or closes an electrical path"; outcome "Closed contact cannot create supply energy"); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Battery, wire, switch …"); [named-elements entry](../invest/named-elements.md#element-198); owner S304 |
| Disposition | **A variant of [CAT-063 Switch](CAT-063-switch.md), declared there.** Not a Duplicate closure: the requirement row adds an author-set contact state, which is the `contact` parameter (Impact, Open, Closed) on the delivered `switch` (D8). It has no package, declaration or acceptance text of its own. It counts as implemented when CAT-063's variant slice passes [CAT-063 rows A1–A6](CAT-063-switch.md#5-acceptance-outline) beside CAT-063's delivered proof of the physical form. |
| Planned package | none of its own: the delivered `switch` (directory `elements/ImpactSwitch/`, palette order 3, `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:28`). Coverage is through CAT-063's catalogue key `switch`; there is no named binding. |
| Related | Supply from [CAT-005](CAT-005-battery.md)/[EL-196](EL-196-battery.md) via [EL-197](EL-197-electrical-wire.md); conditions for [EL-133](EL-133-electrical-and-gate.md)–[EL-137](EL-137-electrical-nand-gate.md) and [EL-181](EL-181-rising-edge-detector.md). |
| Roadmap story | CAT-063's variant slice ([CAT-063 section 8](CAT-063-switch.md#8-delivery)); [EL-198 ledger entry](../invest/named-elements.md#element-198). |
| Status | Physical contact delivered and proven natively (Story 9.5b). The author-set contact state is not started: blocked on the engine programme (owner decision 3a), in particular `ENGINE-CONTACT-OBSERVATIONS`, the contact-set-by-a-parameter row of `ENGINE-SIGNAL-NODES` and `ENGINE-PARAMETER-BOUND-FRAMES` (the impact node and event present by a Choice option); then CAT-063's variant slice. |

## 2. Declaration

The `switch` declaration, art, bindings and the author-set contact variant (the `contact` parameter, per-option controls, the Impact-only node and `TriggerActivated` event, port availability and the mode-edit wire rule, presentation, catalogue sentence and compiled counts) are [CAT-063 section 2](CAT-063-switch.md#author-set-contact-variant-el-198); this identity adds none (coordinator decisions 27 and 31, 23 November 2026). The earlier EL-198 declaration text is withdrawn.

## 3. Engine capabilities

[CAT-063 section 3](CAT-063-switch.md#3-engine-capabilities): the per-option contact control and the `CommittedContactClosed` driver are the contact-set-by-a-parameter row of `ENGINE-SIGNAL-NODES`; the impact node and event present by a Choice option are `ENGINE-PARAMETER-BOUND-FRAMES`'; indexed contact lookup is `ENGINE-CONTACT-OBSERVATIONS`'.

## 4. Sources and legacy

Named entry [element-198](../invest/named-elements.md#element-198), owner S304. These legacy facts are kept here because the [legacy disposition table](legacy-disposition.md) maps `engine/BinaryCircuit.cs` and `engine/SceneElectricalContact.cs` to this identity; CAT-063 section 4 holds the switch's other harvest.

| # | Legacy fact | Source | Disposition |
| --- | --- | --- | --- |
| S1 | A contact element (input → output, open or closed each tick) passes power only when closed and its input is powered. | `engine/BinaryCircuit.cs@a6c914e:L17-L17`, `engine/BinaryCircuit.cs@a6c914e:L134-L135` | carried forward (CAT-063's contact law) |
| S2 | Contact conditions are typed (owner active, boolean state, counter reached, latch on, …) and must bind a capability the owner declares. | `engine/SceneElectricalContact.cs@a6c914e:L8-L8` | carried forward (switch: owner active = latched) |
| S3 | `switched_motor`: switch under a falling ball; battery `supply` → switch `power_in`, switch `supply` → motor `power_in`; hint "A switch controls electricity but does not create it." | `content/puzzles.json@a6c914e:L3580-L3586`, `content/puzzles.json@a6c914e:L3857-L3872` | carried forward (level input) |
| S4 | Bypass control: wiring battery → motor directly turns it and the switch still activates, but the "powered after switch" goal fails; removing any solution wire leaves the motor unpowered. | `CuriousContraptions.tests/ElectricalTests.cs@a6c914e:L42-L94` | carried forward; the sequenced goal waits for `engine-goal-ordered-events` |
| S5 | The legacy `SwitchPart` holds art only; its ports and colliders already moved to the current declarations. | `parts/SwitchPart.cs@a6c914e:L6-L23` | carried forward |
| S6 | Legacy trigger settings: default 0.8 m/s, range 0–64 m/s; cosmetic curve SmoothStep 0.16 s; button Y 0.06 → −0.02, pressed colour `#bff5b0`. | `engine/gpu/WorkshopActivationParts.cs@a6c914e:L5-L10`, `engine/gpu/WorkshopCosmetic.cs@a6c914e:L53-L53`, `parts/SwitchPart.cs@a6c914e:L15-L22` | delivered; the button travel is deleted by CAT-063's button repair |

- **Files harvested:** `parts/SwitchPart.cs`, `parts/catalog/switch.tres`, `engine/BinaryCircuit.cs`, `engine/SceneElectricalContact.cs`, `CuriousContraptions.tests/ElectricalTests.cs`, `content/puzzles.json` (switched_motor).

## 5. Acceptance outline

[CAT-063 section 5](CAT-063-switch.md#5-acceptance-outline): the delivered `SwitchElectricalTests` rows for the physical form and rows A1–A6 for the author-set form. There is no second acceptance text.

## 6. Open questions

None. The decisions (author-set form, build-only `contact`, one three-option parameter, forward-only refusal of a save without `contact`, the Impact-only node and event) are recorded in [CAT-063 section 6](CAT-063-switch.md#6-open-questions). Needs the owner: none.

## 7. Integration

[CAT-063 section 7](CAT-063-switch.md#7-integration). [EL-181](EL-181-rising-edge-detector.md) and [EL-182](EL-182-falling-edge-detector.md) (planned) read its contact as a condition.

## 8. Delivery

CAT-063's variant slice ([CAT-063 section 8](CAT-063-switch.md#8-delivery)), after the engine programme (owner decision 3a), the contact-set-by-a-parameter row of `ENGINE-SIGNAL-NODES` and `ENGINE-PARAMETER-BOUND-FRAMES`. The EL-198 ledger entry cites CAT-063's rows and tests when the identity closes.
