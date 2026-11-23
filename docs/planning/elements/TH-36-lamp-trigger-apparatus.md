# TH-36 · Lamp-trigger apparatus — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-36 delivery slice](../invest/named-elements.md#thermal-36). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Heat follows the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); light follows the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions). Neither is restated.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-36 · Lamp-trigger apparatus · Thermal |
| Anchors | [requirements.md#thermal-36](../requirements.md#thermal-36) (task `sequence-task-470`); [named-elements.md#thermal-36](../invest/named-elements.md#thermal-36) |
| Owner key / research | S601 · [TH-S04 optical heating](../../thermal-component-research.md#th-s04) |
| Related identities | Nearest sibling: [EL-155 Rope-operated light](EL-155-rope-operated-light.md) (Batch K; a rope-pulled knob closing a supplied light's contact). Distinct from the delivered [CAT-035 Signal lamp](CAT-035-lamp.md), which is an activation-latched indicator and emits no light or heat. Optical neighbour [EL-210 Flashlight](EL-210-flashlight.md). Receivers: [TH-07](TH-07-solar-absorber-plate.md), [TH-37](TH-37-heat-sensitive-target.md), [TH-21](TH-21-temperature-sensor.md), light detectors. |
| Campaign | Intro 86; practice 87; reuse 100, 142 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES (the optical segment cast); then ENGINE-SIGNAL-NODES (the passive endpoint contact, P3), ENGINE-OPTICAL and ENGINE-THERMAL. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (palette and owner per the repaired Batch K, tick phase, convention numbers, visual clause). |

### Disposition

**To deliver as a single element.** It is not a duplicate of EL-155: the actuator is a pedal pressed by contact, not a rope pull, and the lamp is a declared heat source as well as a light. Planned package id `trigger_lamp`, directory `elements/TriggerLamp/`, display name "Pedal lamp". The requirement's "resolve the historical lamp's trigger before implementation" is resolved in section 6.

## 2. Declaration

Follows EL-155's shape ([EL-155 section 2](EL-155-rope-operated-light.md#2-declaration)): a sprung slider whose lower endpoint closes a supply contact in front of a supplied cone emitter. The slider follows the delivered Springboard (`elements/Springboard/SpringboardElementDeclaration.cs:63`) and the contact the delivered Linear pusher (`elements/LinearPusher/LinearPusherElementDeclaration.cs:56-57`).

- **Interfaces.** `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the emitter contract of ENGINE-OPTICAL and the thermal contract of ENGINE-THERMAL. Footprint 2 bodies, 1 moving, 1 prismatic, 0 revolute. The beam leaves along local +X.
- **Body 0, the lamp (static; metres).**

  | Collider | Shape | Centre | Size | Purpose |
  | --- | --- | --- | --- | --- |
  | `col_base` | box | (0, 0.10, 0) | half extents (0.425, 0.10, 0.425) | base |
  | `col_column` | cylinder, axis Y | (0, 0.35, 0) | radius 0.18, height 0.30 | brass collar; face of node `collar` |
  | `col_bulb` | sphere | (0, 0.93, 0) | radius 0.43 | lamp head |
  | `col_pedal_base` | box | (−0.65, 0.02, 0) | half extents (0.17, 0.02, 0.17) | plate under the pedal |

- **Body 1, the pedal (dynamic, 0.1 kg).** `col_pedal`: box, half extents (0.15, 0.02, 0.15).
- **Slider 0** (lamp to pedal, axis Y, based at (−0.65, 0.11, 0)): travel −0.05 to 0 m; stiffness 60 N/m; damping 0.6 N·s/m; the pedal and the lamp do not collide.
- **Material (f32).** Friction 0.6, restitution 0.1, rolling resistance 0.
- **Supply graph.** A supply contact `pedal` controlled by `PrismaticContactControl(Prismatic 0, CoordinateEndpoint.Lower)` (`CuriousContraptions.Abstractions/SignalContactControl.cs:13`); a consumer `lamp` with demand 100 W and two targets: the optical emitter (48 W at full grant) and a heat target on node `collar` (52 W at full grant). `PowerIn` → `pedal` → `lamp`. Electrical node: Consumer, 12 V, maximum 100 W.
- **Optical emitter.** Frame body 0; origin (0.43, 0.93, 0), the front of the head; axis +X; half-angle 15°; range 8 m; radiant (16, 16, 16) W at full grant, scaled by granted ÷ demanded; internal occluders: this part's colliders.
- **Thermal node.** `collar`: 9.5 J/K (0.1 kg of brass × 95), ambient 1.0 W/K, face `col_column` 64 W/K.
- **Port.** `PortId.PowerIn`, Electrical, Input, role `PowerIn`, at (0, 0.10, −0.425) facing −Z. No signal input: nothing but the pedal switches the lamp.
- **Parameters.** None. There is no trigger-design selector and no historical-name selector.
- **Presentation bindings.** `Visual/Pedal` follows body 1; `Visual/Aperture` emission ← `PresentationDriver.CommittedSupply` of `lamp` (`CuriousContraptions.Abstractions/ElementPresentation.cs:23`); `Visual/CollarMark_0`, `_1` albedo navy → gold when the committed temperature of `collar` passes 310 K and 330 K.
- **Palette.** Category `ElementCategory.Power` (`CuriousContraptions.Abstractions/ElementCategory.cs:9`), as the repaired Batch K places the pull-cord light EL-155 (`readiness/batch-k.md`, decision L-6: a supplied light source is a Power part). Order: 338 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Base` cream `#fff8e9` | box 0.85 × 0.2 × 0.85 m | solid, `col_base` | nothing |
| `Visual/Column` gold `#f7cb52` | cylinder radius 0.18, height 0.30 m | solid, `col_column` | nothing |
| `Visual/Bulb` cream | sphere radius 0.43 m | solid, `col_bulb` | nothing |
| `Visual/Aperture` | a disc of radius 0.15 m printed flush on the front of the head | decorative: printed flat, glows by a binding | presentation binding |
| `Visual/CollarMark_0`, `_1` navy `#293954` | two rings printed flush on the column | decorative: printed flat | presentation binding |
| `Visual/PedalBase` cream; `Visual/Pedal` gold | boxes as the colliders | solid, `col_pedal_base`, `col_pedal` | nothing; body 1 pose |

No cable is drawn between the pedal and the lamp: a thick strand would have to be solid (owner decision 1, 22 November 2026), and the two stand on one base line as one part. Icon: an original outline of a globe lamp on a column with a square pedal beside it. At a glance it differs from the Signal lamp by the pedal and the printed aperture, and from EL-155 by a pedal on the ground where EL-155 hangs a knob.

### Compiled counts

Bodies 2 (1 static, 1 dynamic); colliders 5 (3 boxes, 1 cylinder, 1 sphere); prismatic joints 1; supply nodes 2; electrical nodes 1; optical emitters 1; thermal nodes 1; ports 1; stores 0; presentation bindings 3 plus one body pose. One collider fewer than EL-155's six.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R2, R5 | Colliders from art; a supply consumer paid by a finite source; typed port and wrong-kind refusal | present | as TH-04 | as TH-04 (bodies read there) |
| P1 | A sprung prismatic slider with limits | present | `PrismaticSliderDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:104` | `WorkshopSpringboardTests.CompiledPreloadUsesActualF32PoseAndEightJoules` (`CuriousContraptions.tests/WorkshopSpringboardTests.cs:146`; body read) |
| P2 | A consumer with more than one target | present, untested: `SupplyConsumerDescriptor.Targets` is a list (`CuriousContraptions.Abstractions/SupplyContractValues.cs:42`); no delivered package declares two | — | none |
| P3 | A supply contact closed at the endpoint of a passive sprung slider | narrower: the endpoint is read from the actuator row of a supplied drive (EL-155's row R18) | `PrismaticContactControl` | `LinearPusherTests.BothEndpointConsumersFollowDepartureReturnAndSupplyRemoval` (`CuriousContraptions.tests/LinearPusherTests.cs:65`, not opened). Every prismatic slot already publishes an actuator row (`engine/gpu/PhysicsPresentationRead.cs:25`–`:43`, verified by Batch K); the contact rule reading it for a passive slider is owned by **ENGINE-SIGNAL-NODES** (the contact and signal network's rows; Batch K withdrew the ENGINE-ASSEMBLY-JOINTS blocker, `readiness/batch-k.md`, M-5) |
| TH-04 R8 | A supply target that turns granted work into heat | narrower | as TH-04 | ENGINE-THERMAL |
| TH-04 R9 to R13 | Thermal node, contact conduction, ambient, drivers, units | **missing** | none | ENGINE-THERMAL |
| P4 | A cone emitter as a supply consumer's target, scaled by the grant | **missing** | none | ENGINE-OPTICAL (optical convention 2) |
| P5 | A Power palette category | present | `ElementCategory.Power`, `CuriousContraptions.Abstractions/ElementCategory.cs:9` | used by the delivered Battery (`elements/Battery/BatteryElementDeclaration.cs:31`) |

Counts: present 5 (P5 now present: Power is a delivered category), untested 1 (P2), narrower 2 (P3, R8), missing 6. Each has one owner: P3 ENGINE-SIGNAL-NODES; the optical rows ENGINE-OPTICAL; the thermal rows ENGINE-THERMAL. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-36](../requirements.md#thermal-36): resolve the historical lamp's trigger before implementation; represent the selected original toy design as a finite supplied light and heat source and a generic contact or displacement actuator; no historical-name selector may choose its effect. Visual style: "Cream sculptural lamp, gold lever and restrained luminous aperture with original icon".
- **Named entry** [thermal-36](../invest/named-elements.md#thermal-36): owner S601; "An unactuated or unsupplied source produces no new output; demonstrate trigger displacement and optical/thermal energy independently".
- **Research** per-element table: "heat sensor ↔ lamp activation"; "lights a lamp when warm". This conflicts with the row; the row governs (D15).
- **Element map** row TH-36: FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat, SignalPropagation, TemperatureSensing (D14; temperature sensing fits the research reading, not the row, and is not declared).
- **Processes.** [IX-13](../requirements.md#interaction-13), [IX-39](../requirements.md#interaction-39), [IX-01](../requirements.md#interaction-01), [IX-03](../requirements.md#interaction-03).
- **Legacy (scale and emitter reference only).**

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Signal lamp art: base box 0.85 × 0.2 × 0.85 m, stem radius 0.18 m and height 0.3 m, bulb radius 0.43 m. | `parts/LampPart.cs@a6c914e:L11-L24`; `engine/PartArt.cs@a6c914e:L16-L21` | Carry forward as the lamp's scale only; CAT-035 owns that part and it has no trigger and no light or heat source. |
  | 2 | Torch emitter: range 8, cone 15°, intensity 24. | `parts/FlashlightPart.cs@a6c914e:L10-L13` | Carry forward range and angle; do not carry forward "intensity 24": power is in watts per channel. |

  Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for lamp, trigger, bulb and light terms; `reference/switch-lamp/` is a CAT-035 contact probe with no light or heat source.

## 5. Acceptance

Point of truth: [thermal-36](../requirements.md#thermal-36) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Contact and gravity press the pedal against its spring; at its lower endpoint the supply contact closes; the consumer's grant is split between light and heat in the declared 48 : 52 ratio; the collar exchanges heat by Q̇ = G·ΔT.
- **Pedal at rest.** Its weight, 0.1 × 9.81 = 0.98 N, against 60 N/m: it sits 0.98 ÷ 60 = 0.016 m below the upper stop, 0.034 m short of the lower one. The contact is open.
- **Pressing it.** At the lower stop the spring pushes up with 60 × 0.05 = 3.0 N, of which the pedal's weight supplies 0.98 N: a body must press with more than 2.02 N. The lightest delivered Weight, 0.25 kg (`elements/Weight/WeightElementDeclaration.cs:33`), presses with 2.45 N; a Basketball with 9.81 N. Momentary: there is no latch, as for EL-155.
- **Energy.** 100 W is 0.833 J per tick, under the default Battery's 1 J per tick; a default Battery lasts 36 s. Each joule granted becomes 0.48 J of light and 0.52 J of heat. Nothing else leaves the part, so light + heat = grant.
- **Light.** 48 W over 0.2141 sr: 224.2 W/sr; 224.2 ÷ 9 = 24.9 W/m² at 3 m; a default detector (0.950 m²) there reads 23.7 W. This is 0.53 of the Flashlight's 46.7 W/m².
- **Heat.** 52 W into 9.5 J/K against 1.0 W/K: final 288 + 52 = 340 K, time constant 9.5 s. At 10 s: 288 + 52 × (1 − e^(−1.053)) = 288 + 52 × 0.651 = 321.9 K (stored 322 J, to ambient 198 J, total 520 J). It passes a sensor's 330 K at −9.5 × ln(1 − 42 ÷ 52) = 15.7 s.
- **Shared source.** With a Heating plate on the same default Battery the demands are 100 + 120 = 220 W against 120 W: the lamp is granted 54.5 W, 26.2 W of light and 28.4 W of heat; the detector reads 12.9 W.
- **Why 48 W and 52 W.** Both outputs must be large enough to show separately: the light trips a default detector at 3 m and the collar passes a default sensor threshold inside one default Battery.
- **Tick order.** The pedal's committed coordinate of tick n closes the contact for tick n + 1, whose substeps grant the lamp's supply; the optical solve of tick n + 1 runs after its last substep and emits the cone from that grant (optical conventions 2 and 4); the heat is credited in that tick's thermal step, which follows the optical solve (T8); a detector's reading commits with tick n + 1 and acts from tick n + 2 (optical convention 4).
- **Tolerances.** Light: the one optical set (coordinator decision 21: power ± 2%, stall guard 1 s, direction 0.5°, end points ± 0.02 m); temperature 2 K; energy 2% (T12).
- **Run length.** Every row fits in the default 30 s (ENGINE-RUN-LENGTH is not needed).

### Rows

Planned class `TriggerLampTests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| T-1 | Given a default Battery wired to `PowerIn`, a Basketball resting on the pedal and a broadband detector 3 m in front, when 10 s pass, then the detector reads 23.7 W and the collar 321.9 K | `APressedSuppliedLampGivesLightAndHeat` | no ball: the pedal stays up, no segment exists, the collar is at 288 K and the Battery is whole (unactuated) | a 0.25 kg Weight also closes the contact | ± 5%; ± 2 K |
| T-2 | Given T-1 with `PowerIn` unwired, then the pedal reaches its stop and there is no light and no heat | `AnUnsuppliedLampProducesNoOutput` | T-1 | none | exact zero |
| T-3 | Given T-1 and a Wall between lamp and detector, then the detector reads 0 and the collar still reads 321.9 K; given T-2, the pedal's displacement is observed with neither | `DisplacementLightAndHeatAreObservedIndependently` | T-1 | none | ± 2 K |
| T-4 | Given T-1, then the Battery's debit (1000 J) = light emitted (480 J) + heat credited (520 J) | `LightPlusHeatEqualsTheGrant` | T-2: all zero | none | 20 J |
| T-5 | Given T-1, when the ball is pushed off the pedal, then the pedal rises, the light is gone within 60 ticks and the collar cools | `TheLampFollowsThePedalWithoutLatching` | T-1 | none | as stated |
| T-6 | Given T-1 with a Heating plate wired to the same Battery, then the detector reads 12.9 W | `AReducedGrantDimsLightAndHeatTogether` | T-1 | none | ± 5% |
| T-7 | Given T-1 then Reset, then the pedal is up, the collar at 288 K and the Battery full | `ResetRestoresPedalCollarAndSupply` | none | none | 0.001 m; 0.01 K |
| T-8 | Given a placed, wired lamp, when saved and loaded, then pose and connection are kept and a fresh Run passes T-1 | `SaveAndLoadKeepPoseAndConnection` | none | none | as T-1 |
| T-9 | An activation output wired to `PowerIn` is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (TH-04 R5) | — | — | typed refusal |

No parameters, so no parameter row. The requirement's outcome: "unactuated" is T-1's control, "unsupplied" is T-2, "demonstrate trigger displacement and optical/thermal energy independently" is T-3.

## 6. Open questions

Resolved:

1. **Game scale and optical unit.** D5 with TH-04's corrections; the earlier "24 intensity units = 24 W" is withdrawn.
2. **The trigger (recorded in the row as S601-D).** A pedal displaced by contact, closing a supply contact. Decided now under the standing delegation: the row asks for "a generic contact or displacement actuator", a pedal is both, it reuses EL-155's slider-and-contact shape, and it gives this identity an actuator EL-155 (rope pull) does not have. No selector between designs exists, so no historical name can choose an effect.
3. **Emitter and scale.** The Signal lamp's proportions and the Flashlight's cone, at 48 W of light and 52 W of heat (decided now: a lamp that is also a heat source must show both).
4. **Research conflict.** The row governs (D15): an actuated source, not a heat-driven lamp.
5. **Map row.** Navigation only (D14).
6. **Latching.** None (D19); a State latch part can hold a signal, and a resting body holds the pedal.

7. **Requirement visual clause** (owner decision 24, 23 November 2026: the four thermal rows with visual wording are reworded to the declared presentation forms). [thermal-36](../requirements.md#thermal-36) asks for a "gold lever"; this spec's actuator is a gold pedal displaced by contact (the row's "generic contact or displacement actuator"), because a lever would need a revolute endpoint contact that the engine does not have, while the prismatic endpoint exists. The clause is reworded to "gold pedal"; the edit to `requirements.md` is the coordinator's ([Batch N summary](readiness/batch-n.md) section 7 item 7).

Needs the owner: none.

## 7. Integration

- **Works with.** Battery, Weight, balls, Wall (delivered); light detectors and the Flashlight family (Batch K and Epic 13); TH-07, TH-37, TH-21, TH-04.
- **Campaign.** Introduction 86, practice 87; reuse 100, 142. Lesson fixtures need story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element, after EL-155 if that is delivered first (it proves the slider-and-contact shape).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES; ENGINE-SUPPLY-DEMAND for the lamp's demand); ENGINE-SIGNAL-NODES with the passive endpoint contact (P3); ENGINE-OPTICAL as amended; ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (heat target; owner decision 18); TH-04 for T-6; one detector; S7.
- **Files.** `elements/TriggerLamp/TriggerLamp.csproj`, `TriggerLampElementDeclaration.cs`, `trigger_lamp.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/TriggerLampTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if P2 to P5 and the thermal rows are present and tested when the slice starts; otherwise the engine tier.
- **Disqualifiers.** A consumer that cannot split one grant between two targets by declared watts (then the lamp declares two consumers behind the one contact, 48 W and 52 W); an emitter that does not scale with the grant; a contact that cannot read a passive slider's endpoint.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Five colliders, one sprung slider, one supply contact, one consumer with two targets, one emitter, one node, one port, three bindings. No trigger script and no mode.
2. **Generic laws.** Spring and contact; a coordinate endpoint closing a supply contact; proportional grant; the emitter law; "granted work credited to a node"; Q̇ = G·ΔT.
3. **Three layers.** Colliders: base, column, head, pedal plate, pedal. Visuals: the same solids; a printed aperture and two printed rings. Animation: aperture glow and ring colour from committed supply and temperature.
4. **Flow and typing.** One Electrical input; enums for domain, role and endpoint; f32 values.
5. **Flat state.** The counts in section 2.
6. **Performance.** One light dynamic body that rests; no sensor. One emitter inside the optical limits and work bound (optical conventions 10 and 11); one node and one ambient term per tick.
7. **Tests are scenarios.** T-1 to T-8 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-SIGNAL-NODES, ENGINE-OPTICAL and ENGINE-THERMAL (general capabilities) and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `trigger_lamp` |
| Named identity | TH-36, `../invest/named-elements.md#thermal-36` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-470` and the ledger entry has no todo link |
| Decision | S601; `scope-corrections.md` on published main has no `s601` block |
| Mode records | `Configuration=Fixed` |
