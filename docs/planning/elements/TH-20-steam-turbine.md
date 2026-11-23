# TH-20 · Steam turbine — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-20 delivery slice](../invest/named-elements.md#thermal-20). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`), and repaired the same day against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (E1, N6) and owner decisions 17 to 20 and coordinator decisions 16 and 21, and on 23 November 2026 against the second audit `docs/planning/elements/readiness/audits/reaudit-group-3.md` (A2's Fan load, F4 vent). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal conventions T1 to T14 are owned by [TH-04](TH-04-electrical-heating-plate.md#thermal-conventions); steam values, the saturation table and probe rows W1 to W11 by [TH-17](TH-17-kettle.md); gas conventions by [EL-039](EL-039-air-reservoir.md) and [Batch H](readiness/batch-h.md).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-20 · Steam turbine · Thermal |
| Anchors | [requirements.md#thermal-20](../requirements.md#thermal-20) (task `sequence-task-454`); [named-elements.md#thermal-20](../invest/named-elements.md#thermal-20) |
| Owner key / research | S584 · [TH-S07 heat to mechanical work](../../thermal-component-research.md#th-s07) |
| Related identities | Shaft output as the delivered Windmill and Motor (`PortId.Drive`); vapour from [TH-17](TH-17-kettle.md); exhaust to [TH-18](TH-18-condenser.md). No CAT refines it. |
| Campaign | Intro 76; practice 77; reuse 87, 147 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026); ENGINE-GAS-NETWORK; ENGINE-CONNECTION-MATERIALS (solid strand: every row uses a hose, owner decision 1); ENGINE-RUN-LENGTH (rows need 140 s); and, for the rig only, ENGINE-LIQUID and ENGINE-THERMAL with TH-04 and TH-17. |

### Disposition

**To deliver as a single element**, the first consumer of a gas work target that turns a flow's enthalpy drop into shaft work. Planned package id `steam_turbine`, directory `elements/SteamTurbine/`, display name "Steam turbine". Not a duplicate of the Windmill: that part captures an open airflow cone; this one converts a ducted pressure drop.

## 2. Declaration

Follows the delivered Windmill for "a static frame, a rotor on a continuous hinge and a `Drive` output" (`elements/Windmill/WindmillElementDeclaration.cs:47`).

- **Interfaces.** `IHingeElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:20`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the path and work-target contracts of ENGINE-GAS-NETWORK.
- **Shell (body 0, static).** `col_back`: box, centre (0, 0.30, −0.07), half extents (0.30, 0.30, 0.03). `col_base`: box, centre (0, 0.03, 0), half extents (0.30, 0.03, 0.12).
- **Rotor (body 1, dynamic, 0.4 kg).** `col_rotor`: cylinder, axis Z, centre (0, 0.32, 0.02), radius 0.25, thickness 0.08; inertia about Z ½ × 0.4 × 0.25² = 0.0125 kg·m². Material friction 0.3, restitution 0.1.
- **Hinge.** Continuous revolute about Z at the rotor centre; friction torque 0.02 N·m; no drive.
- **Gas path with a work target.** `GasIn` → `GasOut` through a throat of 3.0 × 10⁻⁶ m². The work target on the hinge is an expander with efficiency η = 0.25 and free speed ω_free = 30 rad/s:
  - available power P = ṁ × Cp × T_in × (1 − (p_out ÷ p_in)^((γ−1)/γ)), the flow's isentropic enthalpy drop, with the declared gas of the flow (game steam, T11: Cp_v = 1153.2 J/(kg·K), γ = 1153.2 ÷ 691.7 = 1.667, (γ − 1) ÷ γ = 0.400; a carrier-gas mixture uses its mass-weighted values);
  - torque = (4·η·P ÷ ω_free) × (1 − ω ÷ ω_free), zero at and above ω_free;
  - the shaft work committed in a step is removed from the enthalpy the flow carries to `GasOut`.
  Shaft power is torque × ω, at most η·P (at ω_free ÷ 2) and zero at stall.
- **Ports.**

  | Port | Domain | Direction | Position (m) |
  | --- | --- | --- | --- |
  | `GasIn` | Fluid | Input | (−0.30, 0.50, 0), facing −X |
  | `GasOut` | Fluid | Output; declared Vent when unconnected (gas convention F4, `EL-039-air-reservoir.md:89`) | (0.30, 0.10, 0), facing +X |
  | `Drive` | Mechanical | Output, `PortRole.Drive`, bound to the hinge coordinate with `CoordinatePerRadian` 1, as the Motor's and the Windmill's (`elements/Motor/MotorElementDeclaration.cs:52-53`); the work target turns the coordinate positive | (0, 0.32, 0.10), facing +Z |
- **Thermal.** No node (T2): no condensation in the shell.
- **Parameters.** None (D13).
- **Presentation bindings.** The rotor follows body 1's pose. `Visual/Window` albedo ← committed mass rate of the path (0 to 1.0 g/s mapped cream to cyan).
- **Palette.** `ElementCategory.Power` (it produces shaft power; TH-04 section 6 item 5). Order: 334 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Back`, `Visual/Base` cream `#fff8e9` | boxes as the colliders; the "cutaway" is the open front | solid, `col_back`, `col_base` | nothing |
| `Visual/Rotor` gold `#f7cb52` | disc radius 0.25, thickness 0.08, with eight radial grooves printed on its face | solid, `col_rotor` (a plain cylinder: the grooves are printed, so nothing proud lacks a collider) | body 1 pose |
| `Visual/Window` cyan `#66b8c9` | flat arc printed on the back plate behind the rotor's rim | decorative: printed flat, recoloured by a binding | presentation binding |
| port marks | rings printed flush; ports are scene locators | decorative: printed flat | nothing |

Icon: a wheel with radial grooves in front of a square plate. It differs from the Windmill (separate blades on a mast) by the solid grooved disc and the plate.

### Compiled counts

Bodies 2 (1 static, 1 dynamic); colliders 3 (2 boxes, 1 cylinder); revolute joints 1; drives 0; sensors 0; gas paths 1 with 1 work target; ports 3; thermal nodes 0; presentation bindings 1.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol or owner | Test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R5 | Colliders from art; typed ports | present | as TH-04 | as TH-04 (bodies read) |
| U1 | A rotor on a continuous revolute hinge with a friction torque | present, untested here | `RevoluteHingeDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:149` | `RevoluteHingeFrictionTests` (not opened) |
| U2 | A mechanical `Drive` output bound to a revolute coordinate | present, untested here | `PortId.Drive` on the Windmill, `elements/Windmill/WindmillElementDeclaration.cs:47` | `FanIntegrationTests.MotorLinkUsesDeclaredInputSenseAndActualShaftTravel` (`CuriousContraptions.tests/FanIntegrationTests.cs:541`, not opened) |
| TH-17 W2 | One fluid port contract | narrower | as TH-17 | none for a network; owner ENGINE-GAS-NETWORK |
| TH-17 W4 | Lines, a path with a declared throat, a venting output | **missing** | ENGINE-GAS-NETWORK | — |
| U3 | A work target on a gas path that turns the flow's enthalpy drop into torque on a revolute coordinate by a declared efficiency and torque–speed law, and removes the committed shaft work from the flow | **missing** | ENGINE-GAS-NETWORK ("pressure doing work on a declared actuator … a declared prismatic or revolute coordinate"); the expanding direction and its law are a text gap (Batch N summary) | — |
| TH-17 W5 | Flow carrying carrier gas and vapour | **missing** | ENGINE-GAS-NETWORK (the mixture of pneumatic P1: flow carries each component by its mass fraction) | — |
| U4 | Committed mass rate as a presentation driver | **missing** | ENGINE-GAS-NETWORK (Batch H's additions name it) | — |
| TH-18 C2 | A colliding hose | **missing** | ENGINE-CONNECTION-MATERIALS | — |

Counts: present 2, untested 2, narrower 1, missing 5. The delivered rotary airflow capture (`IRotaryAirflowCaptureElement`, `MechanicalContracts.cs:55`) is not used: it captures an open cone, not a ducted flow. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-20](../requirements.md#thermal-20): fluid-work converter exchanges pressure/enthalpy drop for load-dependent shaft torque and exhaust; a supported constitutive model governs its range; no variants.
- **Named entry** [thermal-20](../invest/named-elements.md#thermal-20): owner S584; "no available pressure/enthalpy drop gives no useful work; stall cannot generate free power".
- **Research** per-element table: "nozzle + rotary capture"; parameters pitch, inertia; rotor ← hinge angle.
- **Element map** row TH-20: EnvironmentState, FiniteLedger, GasState, JointConstraint, PressureWork, RigidBodyDynamics, ShaftTorque; composition note "freeze the pressure/enthalpy-to-shaft constitutive model and exhaust flow; no prescribed speed" (D14).
- **Processes and scenarios.** [IX-09](../requirements.md#interaction-09), [IX-05](../requirements.md#interaction-05), [IX-08](../requirements.md#interaction-08); [TX-13](../requirements.md#thermal-scenario-13).
- **Legacy harvest** (converging nozzle; no part consumed it).

  | # | Fact | Citation | Disposition |
  | --- | --- | --- | --- |
  | 1 | Nozzle regimes NoFlow, Subsonic, Choked; thrust = momentum rate + pressure force; enthalpy power = ṁ·Cp·T₀. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L5-L37` | Carry forward the jet flux record. |
  | 2 | Fluxes conserve continuity, stagnation enthalpy (600·T + v²/2 = 180,000 in the oracle) and momentum. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L9-L29` | Carry forward as oracle. |
  | 3 | Closed area or balanced pressure: no mass, enthalpy or thrust. | `CuriousContraptions.tests/ConvergingGasNozzleTests.cs@a6c914e:L64-L74` | Carry forward: no pressure drop, no work. |
  | 4 | Reverse flow and downstream plume expansion are not inferred. | `engine/physics/ConvergingGasNozzle.cs@a6c914e:L39-L41` | Carry forward: exhaust needs its own declaration. |
  | 5 | Nozzle transit quadrature with evaluation budgets. | `engine/physics/GasNozzleTransit.cs@a6c914e:L5-L6` | Do not carry forward: proof-grade CPU integration. |

  Files harvested: `engine/physics/ConvergingGasNozzle.cs`, `engine/physics/GasNozzleTransit.cs`, `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`.

## 5. Acceptance

Point of truth: [thermal-20](../requirements.md#thermal-20), [TX-13](../requirements.md#thermal-scenario-13) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **The flow is subsonic.** For γ = 1.667 the critical ratio is (2 ÷ 2.667)^2.50 = 0.487, so the throat chokes only above 101 325 ÷ 0.487 = 208 000 Pa, beyond the kettle's 200 000 Pa relief. Mass flux ṁ ÷ A = p_in × √(2γ ÷ ((γ − 1)·R·T)) × √(r^(2/γ) − r^((γ+1)/γ)), r = p_out ÷ p_in, R = 461.5.
- **What the kettle can supply** (TH-17 chain 1, before fill and head-link losses). Boiling at T_b the water gets 346.3 − 0.792·T_b W, so ṁ_boil = (346.3 − 0.792·T_b) ÷ (107.2·T_b): 1.27 g/s at 373 K (T11's figure), 0.90 g/s at 389.5 K, 0.82 g/s at the relief's 393.4 K. The path passes only what the head holds, so the turbine's inflow never exceeds the boil rate, and every rate here is under T11's 3.0 g/s ceiling for 120 W.
- **Why a 3.0 × 10⁻⁶ m² throat.** At 200 000 Pa and 393.4 K the flux is 200 000 × 5.247 × 10⁻³ × 0.3246 = 340.6 kg/(s·m²). The earlier 1.0 × 10⁻⁶ m² throat would pass 0.34 g/s while the kettle makes 0.82 g/s, so the head would sit on its relief, venting the surplus, and the operating point would depend on the relief (this spec's own disqualifier). With 3.0 × 10⁻⁶ m²: at 178 000 Pa (T_sat 389.5 K, r = 0.569) the flux is 178 000 × 5.27 × 10⁻³ × 0.320 = 300 kg/(s·m²), 0.90 g/s, which equals the kettle's rate at 389.5 K. So the steady bound is 178 000 Pa, 22 000 Pa below the relief. Through the kettle's own 1.0 × 10⁻⁴ m² spout the same steam needs only 137 Pa (TH-17 A5), which is why the turbine's throat, not the spout, makes the pressure the work comes from.
- **Bound on available power.** At the steady bound: drop = 1153.2 × 389.5 × (1 − 0.569^0.400) = 449 171 × 0.2022 = 90 830 J/kg; P = 0.90 × 10⁻³ × 90 830 = 81.7 W.
- **The run on the plate** (Battery `capacity` 14 400 J). The 20 g charge, with the wetted link thinning and the head link's loss, is gone before that steady point is reached. Hand estimate of the peak: the throat passes 150 000 × 5.307 × 10⁻³ × 0.3012 × 3.0 × 10⁻⁶ = 0.72 g/s at 150 000 Pa (T_sat 384.5 K), about what a kettle one third full makes at that temperature (41.7 W spare, about 34 W into the water, 0.8 g/s); so the head peaks between 140 000 and 160 000 Pa. At 150 000 Pa the drop is 1153.2 × 384.5 × (1 − 0.6755^0.400) = 443 405 × 0.1453 = 64 430 J/kg and P = 0.72 × 10⁻³ × 64 430 = 46 W. The boil starts at 53 s; the head passes 110 000 Pa (P = 4 W) a few seconds later; the charge is nearly gone at about 85 to 90 s (the mean flow about 0.6 g/s over 33 s), at about 10.6 kJ.
- **Shaft.** At P = 46 W: stall torque 4 × 0.25 × 46 ÷ 30 = 1.54 N·m; best power η·P = 11.6 W at 15 rad/s; unloaded speed 30 × (1 − 0.02 ÷ 1.54) = 29.6 rad/s, reached with time constant I·ω_free ÷ τ_stall = 0.0125 × 30 ÷ 1.54 = 0.24 s. Unloaded speed exceeds 25 rad/s whenever the stall torque exceeds 0.12 N·m, i.e. P above 3.6 W: from about 110 000 Pa, which at 110 000 Pa and 375 K is 0.30 g/s and 4.2 W. Shaft power can never exceed η × 81.7 = 20.4 W on the plate.
- **Fan load (A2).** A delivered Fan in Shaft mode takes as shaft work exactly the airflow work it does on receivers in its cone, flow speed 12 m/s × the impulse it delivers, at most `force` × 12 W (`elements/Fan/FanElementDeclaration.cs:22`, `:47-51`; demand at `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Supply.cs:48`), and it blows only while its shaft turns in its declared sense (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.ContinuousPrepare.cs:54-57`). With nothing in its cone it takes no power and A2 could not fail, so A2 holds a Tennis ball (an airflow receiver, `elements/TennisBall/TennisBallElementDeclaration.cs:27`) in the cone against a Wall. At `force` 0.5 N (admitted 0 to 40 N) the load is at most 0.5 × 12 = 6 W; the share of that cap the ball receives is not derived here, so the figures below are the bound case. At P = 46 W, with the hinge friction, the rotor settles where 1.533·ω·(1 − ω ÷ 30) = 6 + 0.02·ω, that is 0.0511·ω² − 1.513·ω + 6 = 0: ω = 24.9 rad/s (the upper root; the lower, 4.7 rad/s, is unstable against a constant-power load), shaft power 1.533 × 24.9 × (1 − 24.9 ÷ 30) = 6.5 W, 14% of P and under η·P = 11.6 W. So A2's bound is tested with real power through the boil.
- **Energy check.** Shaft work is at most a quarter of the flow's isentropic drop and is taken out of the exhaust's enthalpy, so inlet enthalpy = exhaust enthalpy + shaft work. The bound 20.4 W is 17% of the plate's 120 W, below the Carnot fraction between 389.5 K and 288 K (26%); the expected best, 11.6 W, is 10%. Over the run the available energy is about 20 g × 50 kJ/kg ≈ 1.0 kJ, so at most about 250 J of shaft work against about 10.6 kJ of electrical input.
- **Why η = 0.25.** With game steam the isentropic drop per kilogram (90.8 kJ/kg at the bound) exceeds the latent heat paid at the boil (107.2 × 389.5 = 41.8 kJ/kg), because decision 19's latent heat is small while the saturation table keeps real water's slope; the exhaust leaves at 389.5 × 0.798 = 311 K, below its own dew point. Energy is conserved (the drop is paid from the vapour's sensible enthalpy, which the plate supplied), but with η = 0.5 the bound shaft power would be 41 W, 34% of the plate's input, above the Carnot fraction. η = 0.25 keeps the chain below it, and it is within the game-steam row's expander limit η_max = 0.25 (T11), which bundle validation checks from this declaration alone: at 0.25 every inlet state on the saturation table stays below the Carnot fraction (0.283 against 0.309 at 400 000 Pa, 0.342 against 0.351 at 800 000 Pa; T11 derives it). The earlier γ = 1.327 and Cp 1871.5 J/(kg·K) are withdrawn.
- **Stall and no drop.** At ω = 0 torque is 4ηP ÷ ω_free and power is zero; the flow exhausts with all its enthalpy. With no pressure difference ṁ = 0, so P = 0 and torque = 0.
- **Energy budget.** A default Battery (3600 J, `elements/Battery/BatteryElementDeclaration.cs:42`, 120 W limit at `:44`) never boils the kettle; every row names its `capacity`.
- **Tick order.** Gas flow and the work target run in the substeps with the hinge solve; the kettle's boil row adds vapour at the tick boundary (T8).
- **Tolerances.** Speed 10%; power 10% or 0.5 W; energy 2%; mass 0.2 g on the ledger.
- **Run length these rows need: 140 s** (A4's stall guard is 134 s; A1's 117 s; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).

### Rows

Planned class `SteamTurbineTests`. Rig: a TH-17 kettle on a TH-04 plate wired to a Battery of the stated `capacity`, a hose from the kettle's `GasOut` to `GasIn`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given the rig with Battery `capacity` 14 400 J and no shaft load, then the rotor turns faster than 25 rad/s by the stall guard (hand about 57 s) and never faster than 30 rad/s; it turns no faster than 1 rad/s before 45 s | `SteamSpinsTheRotorUpToItsFreeSpeed` | a default Battery (`capacity` 3600 J): no boiling, speed under 0.1 rad/s | none | as stated; stall guard 117 s |
| A2 | Given A1 with `Drive` linked to a delivered Fan in Shaft mode with `force` 0.5 N (airflow work at most 0.5 × 12 = 6 W, below the 11.6 W best shaft power), the Fan's axis through the centre of a default Tennis ball resting on the bench 2.0 m along its axis against a default Wall (the delivered `FanIntegrationTests.MotorLinkUsesDeclaredInputSenseAndActualShaftTravel`, `CuriousContraptions.tests/FanIntegrationTests.cs:541`, places its ball 2 m along the axis), while the kettle is boiling, then the Fan does airflow work on the ball (at most 6 W; with the full 6 W the rotor runs near 25 rad/s, faster for any smaller share the ball receives), its work never exceeds 6 W, and committed shaft power never exceeds a quarter of the committed available power, nor 20.4 W | `ShaftPowerNeverExceedsAQuarterOfTheEnthalpyDrop` | A1's control (a default Battery): zero shaft power and no airflow work on the ball | none | 10% or 0.5 W |
| A3 | Given A2 run until the kettle has at most 1 g left, then inlet enthalpy = exhaust enthalpy + shaft work, and mass in = mass out | `ExhaustCarriesWhatTheShaftDidNotTake` | none | none | 2%; 0.2 g |
| A4 | Given `GasOut` on a Sealed hose and Battery `capacity` 14 400 J, when the pressures equalise, then flow stops and the rotor slows below 1 rad/s | `ABlockedExhaustLeavesNoDropAndNoWork` | A1 | none | stall guard 134 s (the closed kettle reaches its relief at hand 74 s) |
| A5 | Given the rig with Battery `capacity` 14 400 J and a load that holds the rotor still, then committed shaft work is zero while vapour still flows | `AStalledRotorTakesNoPower` (run in the EL-055 brake's slice, the first part that can hold a shaft) | A2 | none | 0.05 J over 10 s |
| A6 | Reset restores rotor angle and speed zero and ambient lines; a second Run repeats A1 | `ResetRestoresRotorAndLines` | none | none | as A1 |
| A7 | Save and load keep pose, hoses and the shaft link; no run state | `SaveAndLoadKeepConnections` | none | none | canonical field values |
| A8 | A non-Fluid link to `GasIn`, or a Fluid link to `Drive`, is refused | generic: TH-04 R5 once Fluid links are validated | — | — | typed refusal |

No parameters, so no parameter-refusal row. The requirement's controls are A1's control and A4 (no drop, no work) and A5 with A2's bound (stall gives no free power).

## 6. Open questions

Resolved:

1. **Constitutive model.** A linear torque–speed law scaled by the flow's isentropic drop and a declared efficiency (decided now: it is bounded by construction, gives zero power at stall and at free speed, and needs only values the gas network already commits). The earlier rotary-capture law belonged to open airflow.
2. **Shaft power.** About 11.6 W at best from one kettle on one plate, never above 20.4 W. Accepted; lessons keep shaft loads light.
3. **Throat.** 3.0 × 10⁻⁶ m², decided now so that the operating point stays below the kettle's relief and the inflow never exceeds what the kettle supplies (section 5).
4. **Efficiency.** 0.25, decided now so that the chain stays below the Carnot fraction of the plate's input under decision 19's steam (section 5).
5. **Condensation in the shell.** None: no thermal node (D19).
6. **Game scale.** D5 and owner decision 19, TH-04 section 11.

Needs the owner: none.

## 7. Integration

- **Works with.** TH-17 (vapour); TH-18 (exhaust, TX-05); delivered shaft consumers through the rigid mechanical connection, which stays rigid (owner decision 2): Fan in Shaft mode (A2's load, section 5), Conveyor. Gas connections are hoses: solid strands (owner decision 1).
- **Campaign.** "Full Steam Ahead": introduction 76, practice 77; reuse 87, 147; each such level declares a Run length of at least 140 s and a 14 400 J Battery. A fixed turbine needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element; engine tier if it is the first user of the expander work target.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme; ENGINE-GAS-NETWORK as amended (U3, U4, W4, W5; owner of the one fluid port contract, coordinator decision 16); ENGINE-LIQUID and ENGINE-THERMAL with TH-04 and TH-17 (the rig); ENGINE-CONNECTION-MATERIALS; ENGINE-RUN-LENGTH; S7.
- **Files.** `elements/SteamTurbine/SteamTurbine.csproj`, `SteamTurbineElementDeclaration.cs`, `steam_turbine.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/SteamTurbineTests.cs`; a row in `VisibleSolidColliderTests`; the binding section once S7 allows it.
- **Review tier.** Engine unless the probe finds U3 present and tested.
- **Disqualifiers.** A gas network whose work target only compresses; a kettle relief or boil rate that moves the operating point to 200 000 Pa (re-derive the throat); a hose throat (2.0 × 10⁻⁴ m², F10) that the network treats as the limiting area in place of the declared one; a work target that uses fixed gas properties rather than the flow's declared ones.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Three colliders on two bodies, one hinge, one gas path with one work target, three ports, one binding. No update loop or handler.
2. **Generic laws.** Throat flow; an expander work target stated for any package; the hinge and friction rows. "Steam turns the wheel" is the work target acting on a declared coordinate.
3. **Three layers.** Colliders: back, base, rotor. Visuals: their art; printed grooves, window and port marks. Animation: the window colour from committed mass rate; the rotor is a body.
4. **Flow and typing.** Two Fluid ports and one Mechanical output; f32; enum roles.
5. **Flat state.** The counts in section 2.
6. **Performance.** Three primitive colliders; the rotor is a single cylinder, so its spin adds no contact work; no sensor; no scene-proportional work. No island sleeping is assumed; none exists in the engine today.
7. **Tests are scenarios.** A1 to A7.

**Verdict: Conforms; blocked on ENGINE-GAS-NETWORK, ENGINE-CONNECTION-MATERIALS and ENGINE-RUN-LENGTH, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Not written as main's binding section. **Blocked on the coverage follow-on story S7:** main's reader (`tools/Coverage/NamedPackageSources.cs`) lists only `EL-*.md` files and admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `steam_turbine` |
| Named identity | TH-20, `../invest/named-elements.md#thermal-20` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-454` and the ledger entry has no todo link |
| Decision | S584; `scope-corrections.md` on published main has no `s584` block |
| Mode records | `Configuration=Fixed` |
