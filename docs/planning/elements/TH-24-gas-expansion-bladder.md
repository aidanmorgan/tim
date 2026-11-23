# TH-24 · Gas expansion bladder — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-24 delivery slice](../invest/named-elements.md#thermal-24). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal units, laws, scale, tick phase and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); shared probe rows are cited as "TH-04 R*n*" and "[TH-23](TH-23-expansion-rod.md#capability-probe) E*n*". Gas units follow [Batch H](readiness/batch-h.md): pascals absolute, ambient 101 325 Pa.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-24 · Gas expansion bladder · Thermal |
| Anchors | [requirements.md#thermal-24](../requirements.md#thermal-24) (task `sequence-task-458`); [named-elements.md#thermal-24](../invest/named-elements.md#thermal-24) |
| Owner key / research | S587 · [TH-S06 thermal expansion](../../thermal-component-research.md#th-s06) |
| Related identities | [TH-25 Hot-air balloon](TH-25-hot-air-balloon.md) (the other heated gas store); [TH-23](TH-23-expansion-rod.md) (family lead); [CAT-010 Bellows](CAT-010-bellows.md) (a similar form, a different law); heated by [TH-04](TH-04-electrical-heating-plate.md). No CAT refines it. |
| Campaign | Intro 77; practice 78; reuse 90, 143 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-SPATIAL-QUERIES (the static neck on the static disc); then ENGINE-GAS-NETWORK, ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (static pair, Run length). |

### Disposition

**To deliver as a single element**, the first part that heats a gas store. Planned package id `gas_bladder`, directory `elements/GasBladder/`, display name "Gas bladder". Not a duplicate: the Balloon is a free isobaric envelope with no heat input, and the Bellows is an air pulse.

## 2. Declaration

Follows the delivered Springboard for the plate on its slider (`elements/Springboard/SpringboardElementDeclaration.cs:41-42`, `:70`) and the delivered Balloon for the gas store declaration (`elements/Balloon/BalloonElementDeclaration.cs:65-68`).

- **Interfaces.** `ISliderElement`, `IGasStoreElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`, `:109`), `ISceneGeometryElement`, and the chamber, line-end and thermal contracts of the two engine stories.
- **Bodies.** Body 0 static (neck and resting dome). Body 1 dynamic (top plate) on slider 0 along local Y, travel 0 to 0.08 m.
- **Colliders (metres).**

  | Collider | Body | Shape | Centre | Size | Purpose |
  | --- | --- | --- | --- | --- | --- |
  | `col_neck` | 0 | box | (0, 0.05, 0) | half extents (0.15, 0.05, 0.15) | base; the thermal face |
  | `col_dome` | 0 | cylinder, axis Y | (0, 0.20, 0) | radius 0.126, height 0.20 | the bellows at its rest height |
  | `col_plate` | 1 | box | (0, 0.32, 0) at rest | half extents (0.15, 0.02, 0.15) | load plate |

- **Mass and material.** Plate 0.2 kg, box inertia; friction 0.6, restitution 0.05 everywhere. Slider spring 200 N/m toward rest (the skin's return), no parameter.
- **Gas store.** Air: R 287, Cv 718 J/(kg·K) (T11), admitted 150 K to 600 K (pneumatic P14). Reference 0.010 m³ at 101 325 Pa and 288 K: mass 101 325 × 0.010 ÷ (287 × 288) = 0.01226 kg. Volume bound to slider 0: V = 0.010 + 0.05·q (chamber area 0.05 m², the dome's cross-section π × 0.126²). The store's pressure acts on the plate over that area; ambient pressure acts on the other side. Free end: `Sealed` or `Vent`, by the `vent` parameter.
- **Thermal nodes** (T2). `skin` on body 0: 25 J/K; ambient conductance 0.2 W/K; face `col_neck`, 16 W/K (soft). The gas store is a node through its own energy m·Cv·T. One wetted link `skin`–gas, 8 W/K.
- **Parameters.**

  | Name | Kind | Unit | Default | Options | Editor policy |
  | --- | --- | --- | --- | --- | --- |
  | `vent` | `ParameterKind.Choice` | none | 0 | `Sealed` (0), `Vented` (1) | player-editable unless the level locks it (D11); build-only |

- **Ports, stores.** No ports. The gas inventory is the one store.
- **Presentation bindings.** `Visual/Plate` follows body 1's pose; `Visual/Dome` scale Y ← committed coordinate of slider 0 (1.0 to 1.4); `Visual/VentCap` visible while `vent` is `Sealed`.
- **Palette.** Category `ElementCategory.Structure`. Order: 535 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Neck` cream `#fff8e9`, three gold `#f7cb52` volume marks printed flush | box 0.30 × 0.10 × 0.30 m | solid, `col_neck` | nothing |
| `Visual/Dome` cyan `#66b8c9`, ribbed | cylinder radius 0.126, height 0.20 m at rest | solid at its rest height, `col_dome`; the part stretched above 0.30 m is moved by a binding and is decorative | presentation binding (scale) |
| `Visual/Plate` cream | box 0.30 × 0.04 × 0.30 m | solid, `col_plate` | body 1's pose |
| `Visual/VentCap` gold | a flat disc printed flush on the neck's side | decorative: printed flat, shown or hidden by a binding | presentation binding |

Icon: a ribbed dome under a plate with an up arrow. It differs from the Bellows by standing upright under a plate and from the Springboard by the ribbed dome in place of a coil.

### Compiled counts

Bodies 2; colliders 3 (2 box, 1 cylinder); joints 1 (prismatic); drives 0; gas stores 1 with one chamber coupling; thermal nodes 1 plus the gas store; wetted links 1; ports 0; parameters 1; presentation bindings 2 plus one body-carried art node.

## 3. Engine capabilities

### Capability probe

Uses TH-04 R1, R6, R7, R9 to R13 and TH-23 E1. TH-04 R10 here is a static-on-static pair: the neck (body 0, static) placed on the Heating plate's static disc, resolved once at Run through ENGINE-SPATIAL-QUERIES (T3's list). Its own rows:

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| G1 | A sealed gas store whose volume follows a prismatic coordinate and whose pressure does work on it | **missing**: the delivered store is one isobaric sphere envelope per body (TH-04 R7; `GasShapeDescriptor`, `CuriousContraptions.Abstractions/GasCouplingValues.cs:11`) | none | none; owner ENGINE-GAS-NETWORK |
| G2 | A store free end that is sealed or vents to ambient, chosen by a parameter | **missing** | none | none; owner ENGINE-GAS-NETWORK |
| G3 | Heat entering and leaving a gas store's energy through a declared link | **missing** | none | none; owner ENGINE-THERMAL |
| G4 | Committed store pressure as an observation and presentation driver | **missing** (`PresentationDriver` has `CommittedGasRadius` only, `CuriousContraptions.Abstractions/ElementPresentation.cs:35`) | none | none; owner ENGINE-GAS-NETWORK |
| G5 | The gas law itself: mass, volume and energy with p = (R ÷ Cv)·U ÷ V | present | `IsobaricGas.Charge`, `CuriousContraptions.Abstractions/IsobaricGas.cs:9` | `GasCapabilityTests.PreparedSealedInventoryConservesMassAndAdiabaticEnergy` (`CuriousContraptions.tests/GasCapabilityTests.cs:13`, body read) |

Counts: present 4 (TH-04 R1, R6; E1; G5), untested 0, narrower 1 (TH-04 R7), missing 9 (TH-04 R9 to R13; G1 to G4). Owners: G1, G2 and G4 **ENGINE-GAS-NETWORK** (its text names pressure work on a prismatic coordinate, [Story ENGINE-GAS-NETWORK](../../../_bmad-output/planning-artifacts/epics.md#story-engine-gas-network-connected-finite-gas-stores-lines-and-valves); Batch H's additions name the sealed or vented line end and the pressure observation; the audit's amendment removes the sphere-only store); G3 and the TH-04 rows **ENGINE-THERMAL**. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-24](../requirements.md#thermal-24): a compliant sealed boundary contains finite gas whose equation of state couples heat, pressure, volume and mechanical work; no variants.
- **Named entry** [thermal-24](../invest/named-elements.md#thermal-24): owner S587; "A vented control cannot retain the same pressure; blocked expansion changes pressure rather than prescribing motion".
- **Research** per-element table ("sealed gas store + node"; inflates when heated; skin ← volume) and the [gas family](../../finite-gas-foundation.md) row.
- **Element map** row TH-24: ElasticStorage, EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, RigidBodyDynamics, SensibleHeat, ThermalExpansion, TopologyTransaction; composition note adds PressureWork. Navigation only (D14).
- **Processes.** [IX-40](../requirements.md#interaction-40), [IX-09](../requirements.md#interaction-09), [IX-31](../requirements.md#interaction-31).
- **Legacy harvest** (sealed-gas foundation; no part consumed it).

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | State T = U/(m·Cv), p = ρRT, derived only. | `engine/physics/SealedGasState.cs@a6c914e:L18-L41` | Carry forward. |
  | 2 | Adiabatic volume change U₂ = U₁(V₁/V₂)^(R/Cv); work on gas = ΔU. Oracle: V 0.01 → 0.0025 m³ gives U 4800 J, 600 K, 960 000 Pa, work +2400 J; → 0.04 m³ gives 1200 J, 150 K, 15 000 Pa, −1200 J. | `engine/physics/SealedGasState.cs@a6c914e:L43-L59`, `CuriousContraptions.tests/SealedGasStateTests.cs@a6c914e:L22-L41` | Carry forward law and oracle. |
  | 3 | Chamber V(q) = V₀ + A·(q − q₀) bound to an owned slider. | `engine/physics/AxialGasGeometry.cs@a6c914e:L25-L33`, `engine/physics/AxialGasLoad.cs@a6c914e:L27-L45` | Carry forward. |
  | 4 | A collision cuts compression work and both domains restore exactly. | `CuriousContraptions.tests/GasChamberWorldTests.cs@a6c914e:L67-L81` | Carry forward: blocked expansion changes pressure. |
  | 5 | Legacy gas had no heat input. | `engine/physics/IdealGasMaterial.cs@a6c914e:L5-L6` | Recorded: heat input is new. |
  | 6 | Out-of-envelope states threw. | `engine/physics/SealedGasState.cs@a6c914e:L29-L33` | Do not carry forward: clamp or continue at runtime. |

  Files harvested: `engine/physics/SealedGasState.cs`, `engine/physics/AxialGasGeometry.cs`, `engine/physics/AxialGasLoad.cs`, `engine/physics/IdealGasMaterial.cs`, `CuriousContraptions.tests/SealedGasStateTests.cs`, `CuriousContraptions.tests/GasChamberWorldTests.cs`.

## 5. Acceptance

Point of truth: [thermal-24](../requirements.md#thermal-24) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Ideal gas p·V = m·R·T with U = m·Cv·T; force on the plate (p − 101 325)·0.05 N against load, plate weight and the 200 N/m spring; heat by links (T3). No motion is prescribed.
- **Heat capacities.** Gas at constant volume 0.01226 × 718 = 8.80 J/K; at constant pressure 0.01226 × 1005 = 12.3 J/K.
- **Warm-up on a Heating plate.** Neck on disc: 64·16 ÷ 80 = 12.8 W/K. Lumped: C = 15.25 + 25 + 12.3 = 52.6 J/K, loss 0.5 + 0.2 = 0.7 W/K, time constant 75 s, final mean rise 171 K. At 30 s (default Battery empty) the mean rise is 171 × (1 − e^(−0.4)) = 56.5 K; the disc leads the skin by its 68 W through 12.8 W/K (5.3 K) and the gas trails the skin by 12.3 × 1.53 K/s ÷ 8 = 2.4 K, so the gas has risen about 52 K, to 340 K.
- **Free plate with a Tennis ball** (0.35 kg, `elements/TennisBall/TennisBallElementDeclaration.cs:16`). Load pressure (3.43 + 1.96 N) ÷ 0.05 = 108 Pa, negligible: V = 0.010 × 340 ÷ 288 ÷ 1.0012 = 0.01179 m³, travel (0.01179 − 0.010) ÷ 0.05 = 0.036 m. A Bowling ball (4 kg, 785 Pa) rises 0.034 m.
- **Energy.** Heat into the gas 12.3 × 52 = 641 J = internal energy 8.80 × 52 = 458 J + boundary work 101 433 × 0.00179 = 182 J. Of the work, 5.4 N × 0.036 m = 0.19 J lifts the load, ½ × 200 × 0.036² = 0.13 J winds the spring and the rest pushes back the atmosphere. All of it is debited from the gas.
- **Blocked plate** (a Wall resting on it): volume fixed, p = 101 325 × 340 ÷ 288 = 119 620 Pa, 18 300 Pa above ambient, 915 N on the Wall. Full travel would need 288 × 1.4 = 403 K.
- **Vented.** Pressure stays 101 325 Pa; mass falls to 0.01226 × 288 ÷ 340 = 0.01038 kg (1.9 g leaves to ambient, counted); the plate does not rise.
- **Tick order.** Heat enters the gas in the thermal step of tick N (T8); the gas law and the plate respond in tick N + 1's substeps, where the delivered gas advance runs (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:994`).
- **Run length these rows need: 260 s** (A5 observes at 30 + 225 = 255 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s). A1 to A4 fit in 30 s plus their stall guard (the smaller of 3 × 30 and 30 + 60 s, 90 s).
- **Tolerances.** Travel 25% (the lags above are estimates); pressure 3 000 Pa; mass 15%; energy 2% (T12).

### Rows

Planned class `GasBladderTests` in `CuriousContraptions.tests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the bladder on a wired Heating plate with a Tennis ball on its plate, when the default Battery is empty, then the plate has risen 0.036 m | `AHeatedSealedBladderLiftsItsLoad` | unwired: travel under 0.002 m | a Bowling ball rises within 0.006 m of the Tennis ball's rise | 0.009 m; stall guard 90 s |
| A2 | Given A1 with `vent` = `Vented`, then the plate rises less than 0.003 m, the store stays at ambient pressure and 1.9 g of gas has left | `AVentedBladderHoldsNoPressure` | A1 | none | 50 Pa; 0.3 g |
| A3 | Given A1 with a Wall resting across the plate, then the plate moves less than 0.003 m and the store reads 119 600 Pa | `ABlockedPlateRaisesPressureInsteadOfMoving` | A1 (pressure within 1 500 Pa of ambient) | none | 3 000 Pa |
| A4 | Given A1, then the Battery's debit equals the enthalpy gained by disc, skin and gas plus the ambient account plus the gas's boundary work | `HeatInEqualsStoredEnergyPlusWork` | none | none | 72 J (2% of 3600 J) |
| A5 | Given A1, when the pair has cooled for 225 s after the Battery empties (three time constants; observed at 255 s), then the plate is back within 0.004 m of rest | `ACooledBladderLowersItsLoad` | none | none | 0.004 m at the fixed observation time |
| A6 | Given A1 then Reset, then gas mass, 288 K, plate pose and Battery are restored; a second Run repeats A1 | `ResetRestoresTheGasAndThePlate` | none | none | as A1 |
| A7 | Given a placed bladder with `vent` = `Vented`, when saved and loaded, then the option is kept | `SaveAndLoadKeepTheVentOption` | none | none | exact |
| A8 | `vent` = 2 or −1 is refused | generic: `PackageRuntimeParityTests.EveryPackageDeclaresTheRuntimeParameterDefaultsAndBounds` (TH-04 R6) | 0, 1 admitted | −1, 0, 1, 2 | typed refusal |

No ports, so no wrong-kind row. Requirement clauses: the vented control (A2) and blocked expansion (A3).

## 6. Open questions

Resolved:

1. **Game scale.** D5 as restated in the thermal conventions; gas values are SI.
2. **Gas store schedule.** The sealed coordinate-bound store is ENGINE-GAS-NETWORK's (D16); this part waits for it.
3. **Vent.** A Choice on this part, decided now: the control must keep the same geometry, and the free-end kind is one declared value of the store.
4. **Map row.** PressureWork is added by the slice's probe (D14).
5. **Chamber size.** 0.010 m³ over 0.05 m², decided now: the earlier 0.004 m³ over 0.01 m² needed a 0.10 m plate, too small to carry a ball; the dome's collider and the chamber area now agree.

Needs the owner: none.

## 7. Integration

- **Works with.** Heating plate; Tennis ball, Bowling ball, Wall (delivered); TH-25; the pneumatic parts of Batch H share the store and line-end rules.
- **Campaign.** Introduction 77, practice 78; reuse 90, 143. A fixed placement needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SPATIAL-QUERIES for the static pair); ENGINE-GAS-NETWORK as amended (G1, G2, G4); ENGINE-LIQUID; ENGINE-THERMAL as amended (G3; owner decision 18); ENGINE-RUN-LENGTH (A5); TH-04; S7; S3 preferred.
- **Files.** `elements/GasBladder/GasBladder.csproj`, `GasBladderElementDeclaration.cs`, `gas_bladder.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/GasBladderTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if G1 to G4 are present and tested when the slice starts; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** A store that stays isobaric (A3 cannot pass); no coordinate-bound volume; gas heat as a separate temperature beside the store's energy (two copies, T2); a different ambient pressure.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Three colliders, one slider with a spring, one gas store with a chamber coupling and a free-end kind, one node, one wetted link, one parameter, two bindings.
2. **Generic laws.** The ideal-gas law; pressure × area on a coordinate; Q̇ = G·ΔT. "Inflates when heated" is their consequence and fails when vented or blocked.
3. **Three layers.** Colliders: neck, resting dome, plate. Visuals: their art; the dome's stretch above rest is animation only. Animation: dome scale and vent cap from committed state. Nothing is written back.
4. **Flow and typing.** No ports; one Choice; f32 values.
5. **Flat state.** The counts in section 2.
6. **Performance.** Three primitive colliders, one joint; one store, one node and one link per tick plus the links at `col_neck` (T10). No scan.
7. **Tests are scenarios.** A1 to A7 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-GAS-NETWORK and ENGINE-THERMAL, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `gas_bladder` |
| Named identity | TH-24, `../invest/named-elements.md#thermal-24` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-458` and the ledger entry has no todo link |
| Decision | S587; `scope-corrections.md` on published main has no `s587` block |
| Mode records | `Vent=Sealed`, `Vent=Vented` |
