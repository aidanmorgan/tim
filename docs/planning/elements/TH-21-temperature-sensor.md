# TH-21 · Temperature sensor — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-21 delivery slice](../invest/named-elements.md#thermal-21). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Units, laws, scale and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); probe rows "TH-04 R<n>" are in [TH-04 section 3](TH-04-electrical-heating-plate.md#3-engine-capabilities).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-21 · Temperature sensor · Thermal |
| Anchors | [requirements.md#thermal-21](../requirements.md#thermal-21) (task `sequence-task-455`); [named-elements.md#thermal-21](../invest/named-elements.md#thermal-21) |
| Owner key / research | S568 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | First heat lesson with [TH-04](TH-04-electrical-heating-plate.md) and [CAT-035 Signal lamp](CAT-035-lamp.md) (delivered). Sibling sensors: [TH-22 Bimetal thermostat](TH-22-bimetal-thermostat.md) (a mechanical contact), [TH-37](TH-37-heat-sensitive-target.md) (a goal body). The same threshold node kind serves the pressure gauge [EL-043](../invest/named-elements.md#element-043) and the sound meter. No CAT refines it. |
| Campaign | Intro 15; practice 16; reuse 70, 83, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-CONTACT-OBSERVATIONS (the moving arm's touch link); then ENGINE-SIGNAL-NODES, ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (Run length, contact rating, the logic parts' row). |

### Disposition

**To deliver as a single element** after TH-04. Planned package id `temperature_sensor`, directory `elements/TemperatureSensor/`, display name "Temperature sensor". Not a duplicate.

## 2. Declaration

Follows the delivered Pressure plate for the signal and supply side (one signal node, a Level output and a supply contact the node controls, `elements/PressurePlate/PressurePlateElementDeclaration.cs:47-49`, `:79`, `:93`) and the Impact lever for a free hinged arm (`elements/ImpactLever/ImpactLeverElementDeclaration.cs:91`).

- **Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement`, and the thermal-node contract of ENGINE-THERMAL.
- **Bodies.** A static column (origin at the centre of its underside) and one moving body, the probe arm, 0.05 kg, inertia about its centre 0.05 × 0.43² ÷ 12 = 0.00077 kg·m².
- **Hinge.** Revolute, axis Z, at (0.08, 0.60, 0) on the column; limits −80° to +60° from horizontal; no drive, spring or friction. The arm falls under gravity until its tip rests on whatever is below, so the probe makes a real contact (T3) without exact placement; with nothing in reach it hangs on the −80° stop, its tip 0.18 m above the floor (0.60 − 0.40 × sin 80° − 0.03).
- **Colliders (metres).**

  | Collider | Body | Centre | Half extents | Purpose |
  | --- | --- | --- | --- | --- |
  | `col_column` | static | (0, 0.40, 0) | (0.08, 0.40, 0.08) | the thermometer column; no thermal face |
  | `col_arm` | arm, from the hinge along the arm | (0.20, 0, 0) | (0.20, 0.015, 0.015) | the probe rod; no thermal face |
  | `col_tip` | arm | (0.40, 0, 0) | (0.03, 0.03, 0.03) | the probe tip; the thermal face |

  With the column 0.38 m from a Heating plate's centre the tip rests on the 0.21 m high disc at −64° (sin⁻¹(0.36 ÷ 0.40)), 0.08 + 0.40 × cos 64° = 0.254 m out from the column and so 0.126 m from the disc's centre, inside its 0.20 m radius.
- **Material (f32).** Friction 0.5, restitution 0.05 on all three.
- **Thermal node.** `probe`, on the arm: heat capacity 1.9 J/K (0.02 kg of brass at 95 J/(kg·K)); ambient conductance 0.01 W/K; face `col_tip`, 0.5 W/K (probe tip, T11).
- **Signal node.** `reading`: a threshold node over the committed temperature of `probe` (T7). On level `on_temperature`; off level `on_temperature` − 5 K; it asserts after 0.25 s at or above the on level and releases after 0.25 s at or below the off level.
- **Supply.** One contact node `contact`, closed while `reading` is asserted (`SupplyContactDescriptor`, `CuriousContraptions.Abstractions/SupplyContractValues.cs:40`); electrical node `ElectricalRole.Switch`, 12 V, 480 W (the Battery's largest `power_limit`, `elements/Battery/BatteryElementDeclaration.cs:44`), so the contact never rates below what it switches; the Pressure plate's 100 W would sit under the 120 W Heating plate that A6 switches. The sensor passes an upstream source's power; it supplies none.
- **Ports.**

  | `PortId` | Domain | Direction / role | Position, facing |
  | --- | --- | --- | --- |
  | `ActivationOut` | Signal, `PortSignalKind.Level` | Output / `ActivationOut` | (0, 0.82, 0), +Y |
  | `PowerIn` | Electrical | Input / `PowerIn` | (−0.08, 0.10, 0), −X |
  | `Supply` | Electrical | Output / `Supply` | (0, 0.10, −0.08), −Z |

- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `on_temperature` | `ParameterKind.Real` | K (TH-04 R13) | 330 | 300 | 600 | player-editable unless the level locks it (D11); build-only |

- **Stores.** None beyond the node.
- **Presentation bindings.** `Visual/Needle` local position Y ← committed temperature of `probe`, 288 K to 600 K mapped to 0 to 0.6 m (driver of TH-04 R12); `Visual/Indicator` albedo navy → gold ← committed level of `reading` (`PresentationDriver.CommittedActivation`). The arm art follows the arm body's pose.
- **Palette.** Category `ElementCategory.Control`. Order: 401 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Column` cream `#fff8e9` | box 0.16 × 0.8 × 0.16 m | solid, `col_column` | nothing |
| `Visual/Ticks` navy `#293954` | scale marks printed flush on the front | decorative: printed flat | nothing |
| `Visual/Needle` gold `#f7cb52` | a flat pointer in a printed slot | decorative: printed flat, moved by a binding | presentation binding |
| `Visual/Indicator` | a round mark printed flush on the cap | decorative: printed flat, recoloured by a binding | presentation binding |
| `Visual/Arm`, `Visual/Tip` gold | rod 0.40 × 0.03 × 0.03 m and a 0.06 m cube | solid, `col_arm`, `col_tip` | the arm body's pose |

Icon: a ticked column with a drooping arm. At a glance it differs from the Signal lamp (a bulb on a post) by its scale and its hinged arm.

### Compiled counts

Bodies 2 (1 static, 1 moving); colliders 3 (box); joints 1 (revolute); drives 0; sensors 0; thermal nodes 1; faces 1; signal nodes 1; supply nodes 1; electrical nodes 1; ports 3; stores 0; presentation bindings 2. Three colliders against the Pressure plate's base and pad: the tip must be its own collider to be the only face.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test (body read unless marked) |
| --- | --- | --- | --- | --- |
| TH-04 R1, R5, R6 | Colliders from art; typed ports and refusal; parameter bounds | present | as TH-04 | as TH-04 |
| S1 | A revolute hinge carrying a moving body with its own colliders | present | `IHingeElement`; `RevoluteHingeDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149` | `PackageRuntimeParityTests.EveryHingePackageDeclaresTheCompiledRevoluteJointAndCarriesItsMovingBody` (`CuriousContraptions.tests/PackageRuntimeParityTests.cs:219`) |
| S2 | A free, limit-stopped arm coming to rest under gravity on another part's collider | present, untested | the same hinge with no drive | none found |
| S3 | A supply contact closed by a signal node's state; an unsupplied part still commits its level | present | `SupplyContactDescriptor`, `CuriousContraptions.Abstractions/SupplyContractValues.cs:40`; `SignalStateContactControl`, `CuriousContraptions.Abstractions/SignalContactControl.cs:12` | `PressurePlateTests.ALoadedPlateWithoutSupplyLeavesItsGateClosed` (`CuriousContraptions.tests/PressurePlateTests.cs:679`) |
| TH-37 G1 | A Level output lights a Signal lamp while the level holds | present | as TH-37 | as TH-37 |
| TH-04 R14 | A threshold signal node over a committed scalar with on and off levels and assert and release holds | narrower: contact load only, with a release hold | as TH-04 | not opened; owner ENGINE-SIGNAL-NODES |
| TH-04 R9, R10, R11, R12, R13 | Node (here on a hinge's moving body), contact conduction, ambient, temperature observation and driver, kelvin unit | **missing** | as TH-04 | none; owner ENGINE-THERMAL |

Counts: present 6, untested 1, narrower 1, missing 5. Owners, one each: R14 is **ENGINE-SIGNAL-NODES** ("threshold over a committed scalar"); R9 to R13 are **ENGINE-THERMAL**, which adds only the temperature observation to that node kind. The contact control's state names (`SignalContactState` has `Latched` and `TimerCounting`, `CuriousContraptions.Abstractions/SignalContactControl.cs:5`) gain the threshold node's asserted state in ENGINE-SIGNAL-NODES. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-21](../requirements.md#thermal-21): read local state through a probe/contact; emit a supplied typed threshold signal with defined hysteresis and response; no variants.
- **Named entry** [thermal-21](../invest/named-elements.md#thermal-21): owner S568; "An unattached probe or unmet threshold does not assert the goal; sensing supplies no heat or electrical work".
- **Research** parameter table (sensor thresholds, dwell; "On > Off by ≥ 2 K"), per-element table ("thermal sensor + electrical output"; needle ← T) and the [first heat slice](../../thermal-component-research.md).
- **Element map** row TH-21: FiniteLedger, SensibleHeat, SignalPropagation, TemperatureSensing; "probe binding, response, hysteresis and supplied output are separate from thermal state observation". Navigation only (D14).
- **Processes.** [IX-17](../requirements.md#interaction-17), [IX-07](../requirements.md#interaction-07), [IX-18](../requirements.md#interaction-18).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for thermometer, temperature sensor and threshold terms. The legacy activation kinds (`engine/gpu/ActivationNetwork.cs@a6c914e:L8-L8`) had no level source; that is the harvest baseline only.

## 5. Acceptance

Point of truth: [thermal-21](../requirements.md#thermal-21) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Contact and ambient exchange at the tip (T3); a threshold with holds over the committed probe temperature (T7); a supply contact closed by that node's state.
- **Probe response.** Tip on a metal face: 64 × 0.5 ÷ 64.5 = 0.496 W/K. Time constant 1.9 ÷ (0.496 + 0.01) = 3.75 s. In steady state the probe's rise is 0.496 ÷ 0.506 = 0.98 of the body's: it reads 2% low, because of its own small loss.
- **On the Heating plate** (T14). The disc rises as 240·(1 − e^(−t/30.5)). The probe reaches 330 K (a rise of 42 K) when the disc, 3.75 s earlier, was at 42 ÷ 0.98 = 42.9 K of rise: t = −30.5·ln(1 − 42.9 ÷ 240) + 3.75 = 6.0 + 3.75 = 9.8 s. With the 0.25 s hold the output asserts about 10 s after Run, for 1200 J of a default Battery.
- **Heat taken.** At most 0.496 W/K × the few kelvin the probe lags: under 3 W of the plate's 120 W, and all of it accounted to the probe node and ambient.
- **Release.** Battery `capacity` 1500 J empties at 12.5 s with the disc at 240 × (1 − e^(−0.41)) = 80.7 K of rise. The probe falls to 325 K (37 K of rise) when the disc has cooled to 37.8 K: 30.5 × ln(80.7 ÷ 37.8) + 3.75 = 27 s after the cut, about 39 s after Run.
- **Why 5 K and 0.25 s.** 5 K exceeds the research rule (on above off by at least 2 K) and stops chatter; 0.25 s rejects a single-tick spike. Both are seconds and kelvin, never ticks (D10).
- **Unattached.** The arm hangs on its stop; the tip touches nothing; the probe stays at 288 K.
- **Tick order.** T8: the probe temperature of tick N commits with tick N; `reading` is evaluated with tick N's other observations; the lamp and the supply solve read it in tick N + 1 (D7).
- **Tolerances.** T12. Stall guard 30 s for assertion (the smaller of 3 × 10 s and 10 + 60 s) and 99 s for release (the smaller of 3 × 39 s and 39 + 60 s).
- **Run length these rows need: 100 s** (A4's release guard of 99 s; A3 observes for 60 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).

### Rows

Planned class `TemperatureSensorTests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a supplied Heating plate, the sensor beside it with its tip on the disc and a Signal lamp on `ActivationOut`, then the output asserts within 30 s, the probe reads at least 330 K when it does, and the lamp is dark before and lit after | `AProbeOnAHotPlateAssertsAndLightsTheLamp` | the plate unwired: never asserts | none | probe ± 2 K; causal order |
| A2 | Given A1 with the sensor moved 1 m away so the arm hangs free, then the probe stays at 288 K and the output never asserts | `AnUnattachedProbeNeverAsserts` | A1 | none | less than 0.01 K |
| A3 | Given A1 with `on_temperature` 600 and a 7200 J Battery, then after 60 s the output has not asserted (the disc cannot pass 528 K) | `AnUnmetThresholdNeverAsserts` | A1 | `on_temperature` at its maximum | level stays clear |
| A4 | Given A1 with Battery `capacity` 1500 J, then the output asserts once and releases once within 99 s; at release the probe reads between 323 K and 325.5 K | `TheOutputReleasesFiveKelvinBelowItsThreshold` | a 7200 J Battery: no release in 60 s | off level = on level − 5 K | as stated |
| A5 | Given A1 at 20 s, then the Battery's debit equals the heat stored in disc and probe plus the ambient account, and nothing is debited for the sensor | `SensingAddsNoHeatAndDrawsNoPower` | none | none | 2% of the debit |
| A6 | Given a second Heating plate fed through the sensor's `PowerIn` and `Supply`, then its disc stays at 288 K until the output asserts and warms afterwards | `TheSupplyContactPassesPowerOnlyWhileAsserted` | the second plate wired straight to the Battery warms from Run | none | less than 0.01 K before; more than 2 K within 10 s after |
| A7 | Given A1 then Reset, then the probe is at 288 K, the arm at its authored angle, the output clear and a second Run repeats A1 | `ResetRestoresProbeArmAndOutput` | none | none | as A1 |
| A8 | Given a placed, wired sensor with `on_temperature` 350, when saved and loaded, then the value, pose and three connections are kept | `SaveAndLoadKeepThresholdAndConnections` | none | none | exact |
| A9 | The Level output into an electrical input or a Pulse input, or an output into an output, is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (TH-04 R5) | — | — | typed refusal |
| A10 | `on_temperature` outside 300 to 600 is clamped (`ParameterEditorPolicy.Clamp`) | generic: TH-04 R6 | bounds admitted | 300, 600 | clamped value exact |

Requirement clauses: probe contact (A1, A2), supplied typed threshold signal (A1, A6), hysteresis and response (A4), no heat or electrical work (A5).

## 6. Open questions

Resolved:

1. **Game scale.** D5, restated once in TH-04 section 11.
2. **Latching lamp.** The output is a Level signal, the delivered Pressure plate's kind; a Signal lamp on a Level output follows it (TH-37 G1), so the lamp goes dark below the off level. No new consumer is needed.
3. **"Supplied" output.** The delivered pattern: the Level output needs no supply, and a supply contact passes an upstream source while asserted. Decided now from the row's word "supplied" and the plate's precedent.
4. **Level signals.** Exist today (`PortSignalKind.Level`).
5. **Probe form.** A free hinged arm, decided now: it reads bodies of any height from 0.21 m to 0.9 m. Under T3 a static probe placed touching a static plate would also link (coordinator decision 21), but it could read only one height; the earlier reason ("no narrowphase contact") no longer applies.
6. **Hysteresis.** Fixed 5 K (D13); one threshold parameter.

Needs the owner: none.

## 7. Integration

- **Works with.** TH-04 (planned first), Signal lamp and Battery (delivered), TH-37, TH-07 (its rail), TH-09, TH-26, TH-27. The logic parts and latches read its Level output only once ENGINE-SIGNAL-NODES delivers its row "a Level output drives any typed signal input of matching kind" (today a Level output is proven only on a Signal lamp, `CuriousContraptions.tests/PressurePlateTests.cs:698`).
- **Campaign.** "A Matter of Degrees": introduction 15, practice 16; reuse 70, 83, 145. A level that fixes the sensor in place needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme, including ENGINE-SIGNAL-NODES and ENGINE-CONTACT-OBSERVATIONS; ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18); ENGINE-RUN-LENGTH; TH-04; S7; S3 preferred.
- **Files.** `elements/TemperatureSensor/TemperatureSensor.csproj`, `TemperatureSensorElementDeclaration.cs`, `temperature_sensor.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); `CuriousContraptions.tests/TemperatureSensorTests.cs`; a row in `VisibleSolidColliderTests`; the hinge package list at `CuriousContraptions.tests/PackageRuntimeParityTests.cs:222` until S3 derives it; this spec's binding section once S7 allows it.
- **Review tier.** Data-only if the probe finds every row present and tested (S2 gains its test in this slice); otherwise the engine tier.
- **Disqualifiers.** A thermal node that cannot sit on a hinge's moving body; a threshold node without separate on and off levels or holds; a contact control that cannot follow the threshold node; the free arm not settling (S2), in which case the arm gets a declared hinge friction.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Two bodies, three boxes, one hinge, one node with one face, one threshold signal node, one supply contact, three ports, one parameter, two bindings. No update loop or engine branch on this part.
2. **Generic laws.** Gravity and contact on a free hinge; Q̇ = G·ΔT; a threshold with holds over a committed scalar; a contact closed by a signal state.
3. **Three layers.** Colliders: column, arm and tip. Visuals: column, arm and tip art each with its collider; printed ticks, needle and indicator. Animation: needle height and indicator colour from committed state; the arm art follows its body; nothing is written back.
4. **Flow and typing.** Signal Level output, Electrical input and output; f32 parameter; enum kinds and roles.
5. **Flat state.** The counts in section 2.
6. **Performance.** Three primitive colliders, one light moving body on one joint; no sensor volume; thermal work is one node and one term per tip contact (T10). The arm rests in use. Island sleeping is owned by ENGINE-ISLANDS-SLEEP (owner decision 3a); a resting arm is stepped every tick until then, and its node keeps exchanging while asleep (T3).
7. **Tests are scenarios.** A1 to A8 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL and ENGINE-SIGNAL-NODES, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `temperature_sensor` |
| Named identity | TH-21, `../invest/named-elements.md#thermal-21` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-455` and the ledger entry has no todo link |
| Decision | S568; `scope-corrections.md` on published main has no `s568` block |
| Mode records | `Configuration=Fixed` |
