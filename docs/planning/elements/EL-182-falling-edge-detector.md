# EL-182 · Falling-edge detector — element readiness spec

**Current delivery (24 October 2026):** [EL-182 delivery slice](../invest/named-elements.md#element-182). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification and is a starting value under decision D1. Box sizes are full extents. Edge-detector rules X1–X7 are in [EL-181](EL-181-rising-edge-detector.md#edge-detector-rules-shared-with-el-182).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-182 · Falling-edge detector |
| Type | Control |
| Anchor | [requirements.md#element-182](../requirements.md#element-182); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("edge detection"); [named-elements entry](../invest/named-elements.md#element-182); owner S403 |
| Related | Refines no CAT spec. Mirror of [EL-181 Rising-edge detector](EL-181-rising-edge-detector.md) — not a renamed rising detector (map composition). Typical inputs: [EL-198 Switch](EL-198-switch.md) contact, [CAT-060](CAT-060-sound_meter.md) meter contact going quiet, a ball leaving a pressure plate ([CAT-052](../requirements.md#current-cat-052)). |
| Roadmap story | [EL-182 delivery slice](../invest/named-elements.md#element-182); canonical proof owner and prerequisite decisions are recorded there. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular the edge row of `ENGINE-SIGNAL-NODES` (with its Falling polarity); then S7 and the Family slice "Edge detectors". |
| Disposition | **Member of the Family slice "Edge detectors"** with sibling [EL-181](EL-181-rising-edge-detector.md). It is its own element with its own package and acceptance, not a duplicate: its behaviour (emit on true → false) differs. It differs from EL-181 only in declared values. |
| Planned package | id `falling_edge`, directory `elements/FallingEdge/` (`FallingEdge.csproj`, `FallingEdgeElementDeclaration.cs`, `falling_edge.tscn`, `icon.svg`). Category Control; palette order 406 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). |

## 2. Declaration

Identical to [EL-181 §2](EL-181-rising-edge-detector.md#2-declaration) except the values in this table. Everything not listed (interfaces `ISignalElement`, `ISupplyElement`, `ISceneGeometryElement`; body 0.9 × 0.7 × 0.5 m, foot 1.0 × 0.12 × 0.6 m at (0, −0.41, 0), face plate, two lamp spheres r 0.07 m, two socket spheres r 0.075 m; static; bounce 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s; the no-target consumer `sense`; the `TriggerActivated` event; no parameter; no store; footprint one static body) is the same, so the two parts swap without rewiring.

| Item | EL-182 value | EL-181 value |
| --- | --- | --- |
| Edge node polarity | `EdgePolarity.Falling` | `EdgePolarity.Rising` |
| Id / directory | `falling_edge` / `elements/FallingEdge/` | `rising_edge` / `elements/RisingEdge/` |
| Display name | "Falling edge" | "Rising edge" |
| Description | "Sends one trigger when its input turns off. Staying off sends nothing more." | "Sends one trigger when its input turns on. Holding it on sends nothing more." |
| Palette order | 406 (one above EL-181's, so the pair sit together) | 405 |
| Scene, icon | `falling_edge.tscn`, step glyph falling left to right | `rising_edge.tscn`, step glyph rising |

Category `ElementCategory.Control`, colour (0.4, 0.72, 0.79). The edge node kind and `EdgePolarity` are **missing** today and come from the edge row of `ENGINE-SIGNAL-NODES` (§3).

**Ports** (same ids, roles and positions as EL-181):

| `PortId` | Role | Domain | Direction | Signal kind | Position (m) |
| --- | --- | --- | --- | --- | --- |
| `ConditionIn` (package-local id) | `PortRole.PowerIn` | Electrical | Input | — | (−0.55, 0, 0) |
| `ActivationOut` | `PortRole.ActivationOut` | Signal | Output | `PortSignalKind.Pulse` | (0.55, 0, 0) |

**Presentation bindings.** As EL-181: `Visual/InputLamp` ← `CommittedElectricalSocket` (`ConditionIn`), slate → gold, SmoothStep 0.1 s; `Visual/OutputLamp` ← `Event` (`TriggerActivated`, node `signal`), gold → slate over 0.2 s per occurrence. At a fall the input lamp goes slate as the output lamp flashes.

**Variants.** The requirements row names no variant; the base declaration is the only required mode.

### Art and solidity

As [EL-181](EL-181-rising-edge-detector.md#art-and-solidity): body, foot, face plate, two lamps and two socket spheres are solid with colliders; the navy step glyph is decorative, printed flat on the face. Icon: the falling step with one dot at the foot of the step. Told from EL-181 by shape, not colour: the step falls to the right.

### Behaviour and numbers

- **Law.** `emit = previous ∧ ¬current`; then `previous ← current`. `previous` starts false at admission and on Reset (X2), so a condition that is false from the start never emits, and a condition true at the start then falling emits at the fall.
- **Tick order, numbers and consistency.** As EL-181 rule X6: the node samples the supply resolution of the current tick, where oscillators do, and its occurrence is routed in that same tick (`engine/gpu/ActivationNetwork.cs:505`–`:512`); a Counter fed by it counts and can emit in that tick. A contact closed by that consumer passes supply from the next tick's electrical solve. Flash 0.2 s is presentation only; output Pulse fits the Counter, Delay box, Hold timer, State latch and Signal lamp inputs.
- **Supply loss is a fall.** Because the input is committed availability, a battery that is disabled or runs out on the condition route produces a falling edge. That is intended: the route turned off.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-02.json), proof owner S403): SignalPropagation. Map composition: "Reusable previous/current Boolean edge state emits once for true-to-false; not a renamed rising detector." The probe is [EL-181 §3](EL-181-rising-edge-detector.md#3-engine-capabilities), with the same marks: 6 present, 0 present-untested, 1 narrower, 1 missing.

| Capability | Mark | Evidence / owner |
| --- | --- | --- |
| Signal node reading a declared electrical input | narrower: oscillator only | `CuriousContraptions.Abstractions/SignalContractValues.cs:61`; `ComposableSignalNodesTests.BothOscillatorsUsePostPhysicsAvailabilityWhenTheirEnabledSourceDepletes` |
| Re-armable edge node with a typed polarity, Falling value | **missing** | no edge kind in `SignalNodeKind` (`CuriousContraptions.Abstractions/SignalContractValues.cs:13`); owner: the edge row of **`ENGINE-SIGNAL-NODES`** (owner decision 3a; row text proposed in `readiness/batch-h.md` section 4 under its earlier draft name), which must qualify one fixture per polarity |
| Consumer terminal with no target; pulse wiring; package-local port ids; socket lamp; event cue; scene colliders | present | rows 1 and 4–8 of the EL-181 table, each with its symbol and test |

**Dependencies.** The edge row of `ENGINE-SIGNAL-NODES`; a supplied condition that can turn off during Run (Battery `enabled` run control, Hold timer contact, Pressure plate level through a contact: delivered); an activation consumer.

### Engine conformance (22 November 2026)

Checked against [engine-conformance.md](readiness/engine-conformance.md); the seven points are written out in [EL-181](EL-181-rising-edge-detector.md#engine-conformance-22-november-2026) and hold here with `EdgePolarity.Falling`. Declaration data only; the law is the generic signal edge rule on a sampled supply availability. Three layers: colliders (seven on one static body: box 3, sphere 4), visuals (those solids plus a flat printed glyph) and animation (two lamps recoloured by bindings) are declared separately; what moves each art node: `InputLamp` and `OutputLamp`, a binding (colour only); every other node, nothing. Flat counts: bodies 1 (static); colliders 7 (box 3, sphere 4); joints 0; drives 0; sensors 0; domain nodes 2 (one signal edge node, one supply consumer); stores 0; presentation bindings 6 rows. No spatial query and no dynamic body (nothing from `ENGINE-SPATIAL-QUERIES` or `ENGINE-ISLANDS-SLEEP`). Acceptance rows F1–F7 are scenarios on the production simulation. Verdict: **Conforms** (the edge node with the Falling polarity is missing, with one owner: the edge row of `ENGINE-SIGNAL-NODES`).

## 4. Sources and legacy

- Requirement row [element-182](../requirements.md#element-182): "Observes a true-to-false transition and emits one typed event"; outcome "Held false input produces no repeated transitions".
- Named entry [element-182](../invest/named-elements.md#element-182), owner S403; component research edge-detector rows ([component research](../../component-research.md#logic)).
- Legacy: no falling-edge detection anywhere. The closest fact is the Sound meter's quiet re-arm: once the level drops below 0.9 × threshold the state turns off silently and a later rise triggers again (`parts/SoundMeterPart.cs@a6c914e:L64-L71`) — the falling transition itself emits nothing in the legacy; this element adds that event.
- **Files harvested:** `parts/SoundMeterPart.cs` (re-arm fact); rules as listed in EL-181.

## 5. Acceptance outline

Acceptance authority: [element-182](../requirements.md#element-182), [electrical profile](../invest/profiles.md#electrical). Planned class `EdgeDetectorTests` (shared file with EL-181; these rows are this identity's own). Fixture: Battery (run-controlled `enabled`) → detector `ConditionIn`; detector `ActivationOut` → Counter (target 9) and Signal lamp.

| Row | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| F1 | Given a true condition, when it turns off during Run, then exactly one pulse is emitted at the fall and the counter reads 1 | `EdgeDetectorTests.AFallingConditionEmitsExactlyOnePulse` | a condition that only rises: no pulse in 240 ticks | — |
| F2 | Given the condition held false after the fall, when 240 further ticks run, then no further pulse (requirement outcome) | `EdgeDetectorTests.AHeldFalseConditionEmitsNoFurtherPulses` | F1's single pulse | 240 ticks = 2 s |
| F3 | Given a condition false from Run start and staying false, then no pulse, including on the first committed tick (X2) | `EdgeDetectorTests.AConditionFalseFromTheStartNeverEmitsAFallingEdge` | — | first committed tick |
| F4 | Given a false condition, when it is true for exactly one committed tick and false again, then one pulse; repeated on/off cycles give one pulse per fall, each counted once | `EdgeDetectorTests.OneTickOnThenOffEmitsOnceAndEachFallIsCountedOnce` | zero ticks on: no pulse | one tick (X5) |
| F5 | Given both detectors on one condition, when it rises then falls, then the falling detector fires at the fall only (the two are distinct) | `EdgeDetectorTests.RisingAndFallingDetectorsFireOnOppositeTransitions` (shared with EL-181 row R5) | — | — |
| F6 | Given a run with one fall, when Reset then Run again, then `previous` is false and the same single pulse recurs | `EdgeDetectorTests.ResetClearsEdgeStateAndASupersededCandidateLeavesItUnchanged` (theory over both polarities) | — | — |
| F7 | Save and Load keep both connections; wrong-domain wires are refused; lamps follow the condition and flash once per fall | `EdgeDetectorTests.SaveLoadKeepsBothConnectionsAndNoRunState`, `WrongDomainWiresAreRefused`, `EdgeDetectorPresentationTests.InputLampFollowsConditionAndOutputFlashesOncePerPulse` (theories over both packages) | as EL-181 rows R7–R9 | — |

Tolerances, stall guards and generic coverage are as [EL-181 §5](EL-181-rising-edge-detector.md#5-acceptance-outline): counts, typed states and refusals only; 240-tick absence windows.

**Chrome recipe (pending, post-catalogue).** Battery → a contact that opens during Run → detector `ConditionIn`; detector `ActivationOut` → Counter. The condition goes true then false: exactly one flash, at the fall; the counter counts 1.

## 6. Open questions

All resolved; needs the owner: none.

1. **Start state — decided** with EL-181's first-tick decision (standing delegation): `previous` starts false at admission and on Reset. A false start never emits a falling edge; a true start then a fall emits one. Reason: one start rule for both polarities, matching the unpowered start of the network.
2. **Supply loss counts as a fall — decided** (standing delegation): yes, because the detector reads availability, and a player sees the input lamp go out either way.
3. **Coordinator decisions, 22 November 2026 (owner's standing delegation).** (a) Tick order follows the delivered signal network: same-tick routing, with the one-tick lag only between signal state and the next electrical solve; reason: the earlier "next tick boundary" wording contradicted `ActivationNetwork.cs:505`–`:512`. (b) Coverage uses the binding form published on main; reason: owner decision of 21 November 2026. (c) Palette order 406 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); reason: batches H, I and J chose overlapping numbers and the bundle refuses duplicates.

## 7. Integration

- **Collaborators.** As EL-181. Sources that fall during Run: Battery run control, Hold timer contact (opens when its time ends), a Pressure plate level routed through a contact; later the [CAT-060 Sound meter](CAT-060-sound_meter.md) contact going quiet ([todo-346](../requirements.md#todo-346); not delivered, not required here). Consumers: Counter, Delay box, Hold timer, State latch, Signal lamp, later [EL-183](EL-183-resettable-counter.md).
- **Campaign.** Logic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage): "edge detection", first use 21–40 (reuse 41–80, 101–150). No level places it today; its teaching level is its own level slice and needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Processes.** [IX-07 signal propagation](../requirements.md#interaction-07); counter retained behaviour [todo-269](../requirements.md#todo-269); switch contact [todo-147](../requirements.md#todo-147).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package `falling_edge`, Named identity, Acceptance, Decision, Mode records), added to this spec. **Blocked on the coverage follow-on story S7:** the published form's preconditions are not met. The EL-182 ledger entry has no `todo-NNN` link (the reader needs exactly one); no `todo`/`sequence-task` pair for this identity is co-anchored in `requirements.md`; and owner S403 has no `<a id="s403">` anchor in `docs/planning/invest/scope-corrections.md`. The section is not written until those exist, because the reader throws on a malformed block.

## 8. Delivery

- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular the edge row of `ENGINE-SIGNAL-NODES` with both polarities; then S7.

Delivered in the Family slice "Edge detectors" described in [EL-181 §8](EL-181-rising-edge-detector.md#8-delivery); files are the second package directory, the shared test file's rows and this spec's binding section; data-only review tier if the planning probe shows the edge node present with both polarities. Disqualifier specific to this member: the edge row delivering only a rising node, in which case this identity waits for the polarity and is not delivered as a renamed rising detector.
