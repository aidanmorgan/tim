# TH-22 · Bimetal thermostat — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-22 delivery slice](../invest/named-elements.md#thermal-22). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal units, laws, scale, tick phase and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); shared probe rows are cited as "TH-04 R*n*" and the expansion rows as "[TH-23](TH-23-expansion-rod.md#capability-probe) E*n*".

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-22 · Bimetal thermostat · Thermal |
| Anchors | [requirements.md#thermal-22](../requirements.md#thermal-22) (task `sequence-task-456`); [named-elements.md#thermal-22](../invest/named-elements.md#thermal-22) |
| Owner key / research | S585 · [TH-S06 thermal expansion](../../thermal-component-research.md#th-s06) |
| Related identities | [TH-23 Expansion rod](TH-23-expansion-rod.md) (lead; same expansion binding); [TH-21 Temperature sensor](TH-21-temperature-sensor.md) (signals a threshold; this part closes a physical supply contact instead); [CAT-005 Battery](CAT-005-battery.md), [CAT-042 Motor](CAT-042-motor.md) and [CAT-028 Fan](CAT-028-fan.md) (delivered) as the switched circuit; heated by [TH-04](TH-04-electrical-heating-plate.md). No CAT refines it. |
| Campaign | Intro 83; practice 84; reuse 90, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES (the static base on the static disc); then ENGINE-THERMAL, one row of ENGINE-SIGNAL-NODES and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (proud stalk, bend binding, Run length). |

### Disposition

**To deliver as a single element after TH-23.** It is not a Family member with the rod: it adds a supply contact, two ports and a parameter. Planned package id `bimetal_thermostat`, directory `elements/BimetalThermostat/`, display name "Bimetal thermostat".

## 2. Declaration

Follows the delivered Hold timer for the switched supply path (`PowerIn` and `Supply` joined by one contact node, `elements/HoldTimer/HoldTimerElementDeclaration.cs:27-28`, `:40-45`) and TH-23 for the moving tip.

- **Interfaces.** `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISupplyElement` (`CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs:11`), `ISceneGeometryElement`, and ENGINE-THERMAL's node and expansion-binding contracts.
- **Bodies.** Body 0 static (base and contact post). Body 1 dynamic (the strip's tip), on slider 0 along the part's local X. Origin at the centre of the base's underside.
- **Colliders (metres).**

  | Collider | Body | Shape | Centre | Half extents | Purpose |
  | --- | --- | --- | --- | --- | --- |
  | `col_base` | 0 | box | (0, 0.05, 0) | (0.15, 0.05, 0.10) | plinth; the thermal face |
  | `col_post` | 0 | box | (0.10, 0.30, 0) | (0.02, 0.20, 0.03) | contact post; its −X face is at x = 0.08 |
  | `col_tip` | 1 | box | (−0.065, 0.47, 0) at rest | (0.015, 0.03, 0.03) | the strip's tip; its +X face is at x = −0.05 |
  | `col_back` | 0 | box | (−0.02, 0.30, −0.045) | (0.13, 0.20, 0.015) | back plate behind the strip; its +Z face (z = −0.03) carries the printed strip |

  The visible gap between tip and post is 0.13 m. Slider 0 travels 0 to 0.13 m, so the tip's face meets the post's face exactly at the upper limit.
- **Mass and material.** Tip 0.02 kg, box inertia; friction 0.3, restitution 0.05 on all colliders.
- **Thermal node** (T2). `strip` on body 0: 4.8 J/K (0.05 kg of copper at 96 J/(kg·K)); ambient conductance 0.16 W/K (2.5 × 0.064 m²); face `col_base`, 64 W/K.
- **Expansion binding** (T6). Node `strip` → slider 0: x₀ = k·(T − 288 K) with k = 0.005 m/K × the `strip` option index (the delivered product value, `MechanicalValue.Multiply`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:173`); stiffness 200 N/m; force limit 5 N.
- **Supply.** One contact node `contact`, closed while slider 0 is at its upper endpoint (`PrismaticContactControl`, `CuriousContraptions.Abstractions/SignalContactControl.cs:13`) and released only after the tip has retreated a declared 0.012 m (release travel; section 3, C2). Electrical node: `ElectricalRole.Switch`, 12 V, 100 W, as the Hold timer's.
- **Ports.** `PortId.PowerIn` (Electrical, Input, `PortRole.PowerIn`) at (−0.15, 0.05, 0) facing −X; `PortId.Supply` (Electrical, Output, `PortRole.Supply`) at (0.15, 0.05, 0) facing +X. No signal port.
- **Parameters.**

  | Name | Kind | Unit | Default | Options | Editor policy |
  | --- | --- | --- | --- | --- | --- |
  | `strip` | `ParameterKind.Choice` | none | 1 | `Uniform` (0), `Bimetal` (1) | player-editable unless the level locks it (D11); build-only |

- **Presentation bindings.** `Visual/Tip` follows body 1's pose. `Visual/Strip` `PresentationProperty.LocalRotationZ` (`CuriousContraptions.Abstractions/ElementPresentation.cs:13`) ← committed coordinate of slider 0, 0 to 0.13 m mapped linearly to 0 to −0.339 rad about its root at (−0.065, 0.10, −0.03), so the printed strip leans toward the post as the tip travels (atan(0.13 ÷ 0.37) = 19.4° = 0.339 rad; the 0.37 m arm is the root-to-tip height). There is no "bend" property; the coordinate driver is probe row C6. `Visual/Layer_B` `Visible` only while `strip` is `Bimetal`.
- **Palette.** Category `ElementCategory.Control` (it is a switch). Order: 434 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Base` cream `#fff8e9` | box 0.30 × 0.10 × 0.20 m | solid, `col_base` | nothing |
| `Visual/Post` navy `#293954` with a gold `#f7cb52` pad | box 0.04 × 0.40 × 0.06 m | solid, `col_post` | nothing |
| `Visual/Tip` gold | box 0.03 × 0.06 × 0.06 m | solid, `col_tip` | body 1's pose |
| `Visual/Back` cream | box 0.26 × 0.40 × 0.03 m | solid, `col_back` | nothing |
| `Visual/Strip` (layers `Layer_A` gold, `Layer_B` cream), 0.40 m tall, 0.04 m wide | two bands printed flush on the back plate's +Z face, from the base to the tip | decorative: printed flat on a solid plate and turned in that plane by a binding; a turned print stays flush | presentation binding |
| `PowerIn`, `Supply` sockets | shared socket gizmos, flush on the base's end faces | decorative: shared socket art | nothing |

The strip is printed flush on a static back plate, so there is no proud art without a collider (audit: a 0.40 m proud stalk on a fixed foot is not "art moved by a binding"); the tip and post, which make the contact, are solid. Icon: a curved two-layer strip leaning toward a post. It differs from the Temperature sensor (a column with ticks) by the leaning strip and the gap; the `Uniform` option shows one layer instead of two.

### Compiled counts

Bodies 2 (1 static, 1 dynamic); colliders 4 (box); joints 1 (prismatic); drives 1 (temperature-bound); sensors 0; thermal nodes 1; supply nodes 1; electrical nodes 1; ports 2; stores 0; presentation bindings 2 plus one body-carried art node.

## 3. Engine capabilities

### Capability probe

Uses TH-04 R1, R5, R6, R9 to R13 and TH-23 E1 to E4. Its own rows:

| # | Capability | State | Symbol | Exercising test (body read unless marked) |
| --- | --- | --- | --- | --- |
| C1 | A supply contact closed while a prismatic coordinate is at a declared endpoint | present | `PrismaticContactControl`, `CuriousContraptions.Abstractions/SignalContactControl.cs:13`; closure rule `CuriousContraptions.Simulation/WorkshopGpuDevice.cs:341-344` | `DirectionalCompositionTests.UnequalDirectionalSlidersKeepReferencesOnARotatedMovingSupport` (`CuriousContraptions.tests/DirectionalCompositionTests.cs:122`; it asserts all four endpoint contacts as the sliders travel) |
| C2 | A declared release travel on that contact (closed at the endpoint, open only after retreating a declared distance) | narrower: one fixed slop decides both (`PhysicalTolerance.ActuatorStopSlopMetres`, same lines) | — | none; owner ENGINE-SIGNAL-NODES |
| C3 | The endpoint read for a slider whose drive is temperature-bound, not supplied | narrower: the read is taken from the actuator row of a supplied prismatic drive (`WorkshopGpuDevice.cs:338`) | — | none; owner ENGINE-THERMAL |
| C4 | A switched pass-through between `PowerIn` and `Supply` | present | `SupplyContactDescriptor`, `CuriousContraptions.Abstractions/SupplyContractValues.cs:40` | `HoldTimerTests.PhysicalTriggerHoldsExactWindowWithoutGeneratingSupply` (`CuriousContraptions.tests/HoldTimerTests.cs:88`; its unsupplied control is never powered) |
| C5 | A binding value that is a product of a Choice index and a constant | present, untested for a Choice operand (the Fan multiplies a Real parameter, `elements/Fan/FanElementDeclaration.cs:49-50`) | `MechanicalValue.Multiply` | not opened |
| C6 | A slider's committed coordinate as a continuous presentation driver | **missing**: the drivers include `CommittedPrismaticPhase` but no continuous coordinate (`CuriousContraptions.Abstractions/ElementPresentation.cs:31`); owner ENGINE-CAPABILITY-INSTANCES (coordinate drivers whatever the joint kind; coordinator decision 29) | — | none |

TH-04 R10 here is the static base on the Heating plate's static disc, a pair resolved once at Run (T3's list). Counts: present 7 (TH-04 R1, R5, R6; E1, E2; C1, C4), untested 1 (C5), narrower 2 (C2, C3), missing 8 (TH-04 R9 to R13, E3, E4, C6). Owners: ENGINE-THERMAL for the thermal and expansion rows and for C3 (the bound drive must publish the same actuator read); **ENGINE-CAPABILITY-INSTANCES** for C6 (a presentation driver that names a coordinate reads it continuously whatever the joint kind; coordinator decision 29, engine-stories-index section 2, "Capabilities formerly without an owner"); **ENGINE-SIGNAL-NODES** for C2 (it owns the rows of the compiled signal and contact network, owner decision 3a). A revolute strip was rejected: a revolute position drive (ENGINE-JOINT-KINDS) and a revolute endpoint contact are both absent, while the prismatic pair exists.

## 4. Sources and legacy

- **Requirement row** [thermal-22](../requirements.md#thermal-22): a bonded two-material strip bends by differential expansion and operates a real contact; temperature is not a magic switch instruction; no variants.
- **Named entry** [thermal-22](../invest/named-elements.md#thermal-22): owner S585; "A uniform-material control lacks the same curvature; verify contact opening/closing and hysteresis from declared geometry/material mechanics".
- **Research** [TH-S06](../../thermal-component-research.md#th-s06), expansion coupling row ("strain feeds a joint/contact row as stiffness-scaled displacement") and per-element table ("differential expansion → contact"; no battery needed to move).
- **Element map** row TH-22: EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SensibleHeat, SignalPropagation, ThermalExpansion, ThermalStress, TopologyTransaction; decision note S585-D (bonded differential expansion and contact hysteresis, not a temperature switch), which has no row in `decisions.md`. Navigation only (D14).
- **Processes and scenarios.** [IX-31](../requirements.md#interaction-31), [IX-32](../requirements.md#interaction-32), [IX-06](../requirements.md#interaction-06); [TX-06](../requirements.md#thermal-scenario-06).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for bimetal, thermostat, expansion and strain terms.

## 5. Acceptance

Point of truth: [thermal-22](../requirements.md#thermal-22), [TX-06](../requirements.md#thermal-scenario-06) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Exchange (T3); expansion binding (T6) on the tip's slider; the supply contact's endpoint rule (C1, C2). The contact is a consequence of where the tip is, never of the temperature directly: a tip held back by an obstacle does not close it however hot the strip is.
- **Tip travel.** 0.005 m/K (the earlier bimetal estimate, curvature 1.5·Δα·ΔT ÷ t over 0.40 m, gave 5.9 mm/K; rounded). Closes at 0.13 ÷ 0.005 = 26 K of rise, 314 K. Opens after 0.012 m of retreat: 0.012 ÷ 0.005 = 2.4 K lower, 311.6 K.
- **Warm-up on a Heating plate.** Pair 32 W/K; C = 4.8 + 15.25 = 20.05 J/K; loss 0.16 + 0.5 = 0.66 W/K; time constant 30.4 s; final rise 120 ÷ 0.66 = 181.8 K. Closing: t = −30.4·ln(1 − 26 ÷ 181.8) = 30.4 × 0.1543 = 4.7 s (563 J).
- **Insufficient heat.** Battery `capacity` 300 J (2.5 s): rise 181.8 × (1 − e^(−0.0822)) = 14.3 K, travel 0.072 m; the gap stays open.
- **Cooling.** From the default Battery's end (30 s, rise 181.8 × (1 − e^(−0.987)) = 114 K) the pair cools with the same constant; it opens at a rise of 23.6 K after 30.4 × ln(114 ÷ 23.6) = 47.9 s.
- **Uniform strip.** k = 0: the tip never moves.
- **Blocked tip.** A Wall between tip and post: the drive pushes with at most 5 N and the contact stays open.
- **Switched load.** The contact passes what the source grants; with no Battery on `PowerIn` a closed contact powers nothing (TX-06).
- **Tick order.** Temperature commits with tick N; the tip moves in tick N + 1's substeps; the endpoint is read after them and the supply solve of tick N + 2 sees the closed contact (T8, D7).
- **Run length these rows need: 150 s** (A3 opens at 30 + 47.9 = 78 s, stall guard the smaller of 3 × 78 and 78 + 60 s, 138 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).
- **Tolerances** (T12): temperatures 2 K; travel 0.003 m; times 15%.

### Rows

Planned class `BimetalThermostatTests` in `CuriousContraptions.tests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the thermostat on a wired Heating plate and a second Battery → `PowerIn`, `Supply` → Motor, when Run, then the contact closes with the strip at 314 K and the Motor turns | `AWarmedStripClosesTheContactAndTheLoadRuns` | `strip` = `Uniform`: the tip stays at 0, the Motor never turns | none | 2 K; stall guard 15 s |
| A2 | Given A1 with the plate's Battery at `capacity` 300 J, then the tip stops near 0.07 m and the Motor never turns | `InsufficientHeatLeavesTheGapOpen` | A1 | none | 0.01 m |
| A3 | Given A1, when the plate's Battery empties and the strip cools, then the contact opens at 311.6 K, below the 314 K at which it closed | `TheContactOpensBelowTheTemperatureAtWhichItClosed` | none | closing and opening temperatures differ by 2.4 K | 0.8 K on the difference; stall guard 138 s |
| A4 | Given A1 with no Battery on `PowerIn`, then the contact closes and the Motor never turns | `AClosedContactWithoutSupplyPowersNothing` | A1 | none | exact state |
| A5 | Given a Wall between tip and post, when the strip passes 314 K, then the contact stays open and the tip has not passed the Wall | `ABlockedTipDoesNotCloseTheContact` | A1 | none | 0.003 m |
| A6 | Given A1 then Reset, then the tip is at 0, the contact open, the strip at 288 K and both Batteries full | `ResetRestoresTheStripAndTheCircuit` | none | none | as A1 |
| A7 | Given a placed, wired thermostat with `strip` = `Uniform`, when saved and loaded, then the option and both connections are kept | `SaveAndLoadKeepTheOptionAndConnections` | none | none | exact |
| A8 | An activation output wired to `PowerIn`, or `Supply` wired to a supply output, is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (TH-04 R5) | — | — | typed refusal |
| A9 | `strip` = 2, −1 or 0.5 is refused | generic: `PackageRuntimeParityTests.EveryPackageDeclaresTheRuntimeParameterDefaultsAndBounds` (TH-04 R6) | 0 and 1 admitted | −1, 0, 1, 2 | typed refusal |

Requirement clauses: the uniform control (A1's control), opening, closing and hysteresis from declared geometry (A1, A3), insufficient temperature (A2), a downstream load still needs supply (A4).

## 6. Open questions

Resolved:

1. **Game scale.** D5, as restated in the thermal conventions.
2. **Control strip.** A `strip` Choice on this part, decided now: the control must keep identical geometry, and a second package would be a second catalogue entry for one identity (D8).
3. **S585-D.** The note's content is this spec's sections 2 and 5; the missing `decisions.md` row is recorded as a document gap in the summary (D16).
4. **Hysteresis mechanism.** The contact's declared release travel (0.012 m), decided now: it is the differential travel of a real snap contact, stated as geometry, and needs no second moving body.
5. **Exaggeration.** 0.005 m/K adopted (D1, D15).
6. **Strip as a slider.** Decided now: the tip is a body on a prismatic slider and the strip is printed on a solid back plate and turned by a binding, because the endpoint contact exists for prismatic coordinates only.
7. **Normally-closed mode.** None (D19): TX-06 and the lessons use a contact that closes when hot.

Needs the owner: none.

## 7. Integration

- **Works with.** Heating plate; Battery, Motor, Fan, Signal lamp (delivered); TH-23.
- **Campaign.** Introduction 83, practice 84; reuse 90, 145. A level that fixes it in place needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element, after TH-23.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES for the static pair), including ENGINE-SIGNAL-NODES with C2; ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (with C3 and C6; owner decision 18); ENGINE-RUN-LENGTH; TH-04; TH-23; S7; S3 preferred.
- **Files.** `elements/BimetalThermostat/BimetalThermostat.csproj`, `BimetalThermostatElementDeclaration.cs`, `bimetal_thermostat.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/BimetalThermostatTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if C2, C3 and C5 are present and tested when the slice starts; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** No release travel on the endpoint contact (A3 cannot pass; the story must add it, no element-side substitute is allowed); a Choice index refused as a product operand (then `strip` becomes a Real `bend` parameter and needs a metre-per-kelvin unit); the bound drive not publishing an actuator read.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Three colliders, one slider, one node, one expansion binding, one supply contact, two ports, one parameter. No handler or engine branch on this part.
2. **Generic laws.** Q̇ = G·ΔT; a temperature-bound coordinate drive; a supply contact closed by a coordinate at its endpoint. "Switches when hot" is the consequence of these three, and fails when any one is prevented (A2, A4, A5).
3. **Three layers.** Colliders: base, post, tip, back plate. Visuals: their four art boxes, the printed strip and two sockets. Animation: the printed strip's rotation from the committed coordinate. Nothing is written back.
4. **Flow and typing.** Two Electrical ports; the Choice is an enum-backed option list; f32 values.
5. **Flat state.** The counts in section 2.
6. **Performance.** Three primitive colliders, one joint, no sensor; thermal work is one node, one binding and the links at `col_base` (T10). The 0.02 kg tip moves at most 0.005 m/K × the heating rate (under 0.05 m/s), so it cannot tunnel the post.
7. **Tests are scenarios.** A1 to A7 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL and on one contact row of ENGINE-SIGNAL-NODES, both general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `bimetal_thermostat` |
| Named identity | TH-22, `../invest/named-elements.md#thermal-22` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-456` and the ledger entry has no todo link |
| Decision | S585; `scope-corrections.md` on published main has no `s585` block |
| Mode records | `Strip=Uniform`, `Strip=Bimetal` |
