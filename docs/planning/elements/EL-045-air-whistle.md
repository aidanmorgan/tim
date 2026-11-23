# EL-045 · Air whistle — element readiness spec

**Current delivery (24 October 2026):** [EL-045 delivery slice](../invest/named-elements.md#element-045). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against checkout `8d1d68aa`, and repaired on 22 November 2026 against the independent audit (`docs/planning/elements/readiness/audits/elements-batch-h.md`). Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the current checkout. Values marked *proposed* are starting values under [D1](../invest/delivery-batches.md#3-family-level-decisions). Box sizes are full extents. Shared acoustic facts A1–A13, laws L1–L7, the capability probe (C1–C13, N1–N13) and tolerances are in [EL-044](EL-044-tone-selective-sound-meter.md); shared sealed-gas facts P1–P20 in [EL-039](EL-039-air-reservoir.md#shared-sealed-gas-family-facts). This spec defines the **flow-excitation rule (S1)** that [EL-052](EL-052-water-tuned-bottle.md) reuses.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-045 · Air whistle |
| Type | Sound |
| Anchor | [requirements.md#element-045](../requirements.md#element-045); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-045); owner S533 |
| Disposition | **To deliver as a single element** with one catalogue entry and an `inlet` setting ([D8](../invest/delivery-batches.md#3-family-level-decisions): a mode is a parameter). It refines no CAT part and duplicates nothing. The Airflow inlet is delivered first; the Gas inlet is a later criterion on the same package, after ENGINE-GAS-NETWORK. The identity counts as implemented only when both inlets pass. |
| Planned package | id `air_whistle`, directory `elements/AirWhistle/`. Coverage binding: section 7 (blocked on the coverage follow-on story S7). |
| Related | Airflow sources [CAT-028 Fan](CAT-028-fan.md) (delivered), [CAT-010 Bellows](CAT-010-bellows.md) (in flight), [EL-042 Air nozzle](EL-042-air-nozzle.md); gas source: a `GasOut` on [EL-037](EL-037-air-compressor.md), [EL-039](EL-039-air-reservoir.md) or [EL-040](EL-040-pneumatic-release-valve.md), or `PortA`, `PortB` or the `Exhaust` Fluid Output of [EL-041](EL-041-pneumatic-directional-valve.md), joined by an [EL-038](EL-038-pneumatic-hose.md) hose (the `Exhaust` link keeps the piston's "exhaust → whistle", [todo-426](../requirements.md#todo-426)); heard by [CAT-060](CAT-060-sound_meter.md)/[EL-044](EL-044-tone-selective-sound-meter.md). Airflow-excited sibling: [CAT-069 Wind chimes](CAT-069-wind_chimes.md). |
| Roadmap story | [EL-045 delivery slice](../invest/named-elements.md#element-045) `named-el-045`. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception, occlusion and probe candidates from it), the threshold row of `ENGINE-SIGNAL-NODES` (the S1 on and off levels) and `ENGINE-PARAMETER-BOUND-FRAMES` (tone bars); then Stories 14.0, 14.2, 14.1 and 14.3 for the Airflow inlet, and `ENGINE-GAS-NETWORK` for the Gas inlet. |

## 2. Declaration

Closest delivered shapes: the Fan for a `Choice` that selects ports and inputs (`elements/Fan/FanElementDeclaration.cs:13`–`:14`, `:20`, `:53`–`:68`), and the Fan's airflow source for the stream an observation probe reads (`IAirflowSourceElement`, `CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:40`). No delivered part observes flow without taking force.

**Interfaces.** `IMechanicalElement` (`MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), the observation-probe and acoustic emitter declarations of Story 14.0 (N6, N1), and for the Gas inlet the single fluid port contract `IFluidElement` (`CuriousContraptions.Abstractions.Fluid/FluidContracts.cs:5`) as ENGINE-GAS-NETWORK leaves it. It does **not** implement `IAirflowReceiverElement`: a receiver on body 0 of a static element is refused ("Airflow receiver requires a dynamic body", `CuriousContraptions.Bundle/AirflowDomain.cs:33`–`:34`).

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and colliders | One static body. Whistle tube: cylinder r 0.15 m, length 0.7 m along X at the origin *proposed* (large enough to pick, smaller than a Basketball); foot box 0.8 × 0.1 × 0.4 m at (0, −0.2, 0); mouthpiece cylinder r 0.1 m, length 0.12 m at (−0.41, 0, 0); in Gas mode a `Loc_Port_GasIn` locator at the mouthpiece's open end (delivered ports are `Loc_Port_*` locators, `elements/ImpactSwitch/switch.tscn:76`); no separate socket solid. |
| Mass and material | Static (mass 0). Restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s. |
| Constraints | none. |
| Airflow probe | One observation-only probe (N6; missing today) at the mouthpiece, four weighted samples on a 0.1 m square at local (−0.47, 0, 0) facing −X, each weight 0.25. It reads the committed flow speed of a declared airflow source at its unoccluded samples; it takes no force and adds no demand. The samples lie on the mouthpiece's end face (the mouthpiece spans x −0.47 to −0.35), so the whistle's own colliders are excluded from its sample lines (the receiver-exclusion form of `AirflowLineClear`, `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`–`:161`, which EL-044 N6 carries over); without it the delivered sweep, which tests every collider not on an exclusion list, would occlude every sample and the whistle would never sound. Available in Airflow mode. |
| Emitter | One emitter at local (0.1, 0.15, 0) (the window slot): kind Sustained, pattern Omnidirectional, tone = `tone`, strength by rule S1. No electrical or signal input: only flow excites it. |
| Stores | none. |
| Footprint | 1 body, 0 moving bodies, 0 constraints. |

**Parameters**

| Name | Type | Unit | Default | Min | Max | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `inlet` | Choice | None | 0 (Airflow) | 0 | 1 | Reject; `Airflow`, `Gas`. The slice that delivers only the Airflow inlet declares the single option; the Gas slice adds the second. |
| `tone` | Choice | None | 2 (High) | 0 | 2 | Reject; `Low`, `Mid`, `High`. High because a small whistle is high-pitched and differs from the default Mid speaker. |
| `on_flow` | Real f32 | MetrePerSecond | 4 | 1 | 12 | Clamp; BuildOnly; used in Airflow mode. |

The Gas inlet's thresholds are fixed declared values (no `ParameterUnit` for kg/s exists, `CuriousContraptions.Abstractions/ParameterDescriptor.cs:15`; [D13](../invest/delivery-batches.md#3-family-level-decisions)).

**Ports**

| `PortId` | Role | Domain | Direction | Position (m) | Available |
| --- | --- | --- | --- | --- | --- |
| `GasIn` (new id) | Bidirectional (fluid port, L5) | Fluid | Input (link-joined) | (−0.47, 0, 0), facing −X | `inlet` = Gas |

Airflow mode has no port. The other end of a `GasIn` link is a Fluid Output: `GasOut` on the compressor ([EL-037](EL-037-air-compressor.md)), reservoir ([EL-039](EL-039-air-reservoir.md)) or release valve ([EL-040](EL-040-pneumatic-release-valve.md)), or `PortA`, `PortB` or `Exhaust` on the directional valve ([EL-041](EL-041-pneumatic-directional-valve.md)). The hose ([EL-038](EL-038-pneumatic-hose.md)) is the connection, not an outlet, and the nozzle has no outlet. A Fluid Output takes several hoses, so a whistle can share an outlet with another consumer. `GasIn` is a fluid port of the one record (EL-039 F3; liquid L5): role Bidirectional, so flow follows pressure, and Input is only its link-joined socket's authoring end. `PortId` has no gas member today (`CuriousContraptions.Abstractions/PortId.cs:21`–`:33`), so ENGINE-GAS-NETWORK adds `GasIn`; no `PortRole` member is added for gas (`PortDescriptor.cs:20`–`:35`). The port declares no aperture area: the whistle's 2 × 10⁻⁵ m² throat is the throat of its declared vent boundary on `GasIn` (EL-039 F4). Pressures are pascals absolute with ambient 101 325 Pa.

**Presentation bindings.** Reed `Visual/Reed` local rotation flutter ← committed excitation measure (new committed driver from N6); translucent wavefronts while Sounding ← committed emitter state (N12). Audio voice is presentation-only (A11).

**Palette.** Category `ElementCategory.Control`; palette order 429 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); title "Air whistle"; colour gold `#f7cb52`. Description: "Sounds its tone only while enough air actually flows through it. Blocked or still air stays silent."

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Tube` | cylinder r 0.15 × 0.7 m, gold `#f7cb52` | solid, cylinder collider |
| `Mouthpiece` | cylinder r 0.1 × 0.12 m, cream `#fff8e9` | solid, cylinder collider |
| `Foot` | box 0.8 × 0.1 × 0.4 m, navy `#293954` | solid, box collider |
| `Window` | navy slot 0.12 × 0.05 m in the top of the tube, drawn flush | decorative: printed flat |
| `Reed` | cream tab inside the window slot | decorative: enclosed and moved by a binding |
| `ToneBars` | three flush nodes of one, two and three navy bars on the tube side; one is visible, chosen by `tone` (needs EL-044 C13) | decorative: printed flat |
| Wavefronts | translucent rings | decorative: glow |
| `Loc_Port_GasIn` (Gas mode) | locator at the mouthpiece end, no mesh | not art |

Icon: a side view of the tube with its window and one ring above. **At a glance** it differs from the Air nozzle (a tapered frustum) by its straight tube with a top window, and from the Speaker by having no cabinet.

### Behaviour and numbers

**S1 — flow-excitation rule.** Let m be the committed excitation measure and F the full-strength measure. The emitter starts one sustained occurrence when m ≥ on, and stops it when m ≤ 0.9 × on (the Sound meter's hysteresis ratio, `parts/SoundMeterPart.cs@a6c914e:L64-L71`); the on and off state is a threshold node over m, the threshold row of `ENGINE-SIGNAL-NODES`, and Story 14.0 adds only the observation kind and the strength map (EL-044 gap 5). While Sounding, strength = min(1, m / F). This replaces the back-to-back pulses this spec first proposed: a sustained source is one occurrence ([D17](../invest/delivery-batches.md#3-family-level-decisions)).

| Inlet | Measure m | on | F | Reason |
| --- | --- | --- | --- | --- |
| Airflow | committed source flow speed × the summed weight of unoccluded samples (m/s) | `on_flow`, default 4 m/s | 12 m/s | The Fan blows at a fixed 12 m/s (`elements/Fan/FanElementDeclaration.cs:48`), so m takes the values 0, 3, 6, 9, 12 m/s as samples are uncovered. The default sounds with two or more of four samples in the stream and is silent with one: a half-shadowed whistle still sounds, a grazed one does not. |
| Gas | committed mass rate through `GasIn`'s declared vent throat to ambient (kg/s), throat 2 × 10⁻⁵ m² (EL-039 F4, P6) | 0.002 kg/s | 0.01 kg/s | With Air at the ambient temperature per [TH-04 §11 T1](TH-04-electrical-heating-plate.md#11-thermal-conventions-owned-here-every-thermal-spec-cites-this-section) (288 K) the subsonic rate reaches 0.002 kg/s at about 105 400 Pa absolute (about 4 100 Pa above ambient) and falls to the 0.0018 kg/s off level near 104 600 Pa, so a tank sounds it until nearly empty; full strength needs about 210 000 Pa (recomputed for 288 K: at a fixed throat and pressure ratio the rate scales with the inverse square root of temperature). These depend on the gas-network law and are re-derived in that story's Code Map. |

- **Laws.** L1 (Sustained, Omnidirectional), L2–L4, L6, L7. At full strength a default meter hears it to 6.1 m.
- **No sound without flow.** A Wall between Fan and whistle zeroes every sample weight (m = 0); a closed valve or an empty tank gives zero mass rate. Both give no emission at all, not a quiet one (requirement outcome).
- **No demand on the source.** The probe is observation-only, so it adds no branch to the Fan's demand and the Fan's finite store is charged exactly as it would be without the whistle (row 8). A delivered airflow receiver could not give this: every receiver branch is charged flow speed × impulse (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Supply.cs:48`), largest for a receiver at rest. In Gas mode the gas that passes is the upstream store's own loss; the whistle adds none.
- **Tick order.** The gas mass rate is committed by the gas step, which runs once per 1/480 s substep (EL-039 F5), and airflow exposure commits with the tick's observations. The acoustic phase at the start of tick n reads the measure committed in tick n − 1 ([one combined tick order](EL-176-red-laser-emitter.md#combined-tick-order), step 1), and receivers respond per D7.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S533): AcousticPropagation, AerodynamicDrag, EnvironmentState, FiniteLedger, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, SignalPropagation (+ StateTransaction). Numbered rows are the shared probe in [EL-044 section 3](EL-044-tone-selective-sound-meter.md#shared-capability-probe).

| Capability this identity needs | Mark | Evidence or owner |
| --- | --- | --- |
| Static scene body with cylinder colliders (C1, C10) | present | EL-044 C1, C10 |
| Conical airflow source with per-sample solid occlusion (C8) | present | `AirflowGeneralityTests.OneOfFourOccludedSamplesRemovesItsOwnQuarterOfDemand` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:138`) |
| Art visibility chosen by a `Choice` (tone bars; C13) | narrower | EL-044 C13; owned by `ENGINE-PARAMETER-BOUND-FRAMES` (choice-bound values, coordinator decision 16) |
| `Choice` selecting a port and an input (C7) | present, untested | Fan pattern; test not opened |
| A fluid port contract | present, untested | `IFluidElement`, `FluidPortDescriptor` (`CuriousContraptions.Abstractions.Fluid/FluidContracts.cs:5`) are declared and validated but no solver or element consumes them (`epics.md` ENGINE-GAS-NETWORK, "Present today") |
| Generic lifecycle and refusals (C9) | present | EL-044 C9 |
| Emission, propagation, occlusion, tone, sustained emission (N1–N3) | missing | Story 14.0 (gaps 1, 2), with reception and occlusion candidates from `ENGINE-SPATIAL-QUERIES` |
| Emission excited by a committed scalar observation, and the observation-only airflow probe on a static body (N6) | missing | Story 14.0, gap 5 (probe and strength map; probe candidates from `ENGINE-SPATIAL-QUERIES`); the on and off levels are the threshold row of `ENGINE-SIGNAL-NODES`. The delivered `AirflowReceiverDescriptor` is refused on a static body and adds demand (EL-044 C8) |
| Gas flow through a typed port to ambient with a committed mass-rate observation; the `GasIn` `PortId` member (role Bidirectional, no new `PortRole` member, EL-039 F3) | missing | ENGINE-GAS-NETWORK (proposed story text is in `readiness/batch-h.md` section 4) |
| Committed excitation driver, wavefront cue, presentation-only audio (N12) | missing | Story 14.0, gap 11 |

Counts: 3 present, 2 untested, 1 narrower, 4 missing. Nothing here needs element-specific engine code. The `GasIn` vent throat follows gas convention F4, which admits a vent on any Fluid port, Input or Output end, and names this part's `GasIn` (`EL-039-air-reservoir.md`, F4). **Gap for the summary:** ENGINE-GAS-NETWORK's story text still names vents on an Output only; proposed story sentence: "a vent throat to ambient may be declared on any Fluid port, Input or Output, with a committed mass-rate observation."

**Dependencies.** Both inlets: the engine programme (owner decision 3a: `ENGINE-SPATIAL-QUERIES`, the threshold row of `ENGINE-SIGNAL-NODES`, `ENGINE-PARAMETER-BOUND-FRAMES`). Airflow inlet: Story 14.0, the Fan (delivered), Story 14.3 and [EL-044](EL-044-tone-selective-sound-meter.md) (row 1 listens with a High-band meter). Gas inlet: ENGINE-GAS-NETWORK, [EL-039](EL-039-air-reservoir.md), [EL-038](EL-038-pneumatic-hose.md), [EL-040](EL-040-pneumatic-release-valve.md); the exhaust row also [EL-041](EL-041-pneumatic-directional-valve.md).

### Engine conformance (22 November 2026)

Checked against the owner's engine-conformance instruction of 22 November 2026 and the documents named in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026).

1. **Declaration data only.** One observation probe, one sustained emitter, one optional fluid port, and rule S1 as declared values (observation kind, on level, 0.9 off ratio, full-strength value). The engine evaluates S1 for any emitter that declares an excitation; the package has no update loop or handler and no engine code may branch on `air_whistle`.
2. **Generic laws.** The delivered conical airflow law with per-sample solid occlusion, read by a probe (N6); the gas network's mass rate through a venting inlet (ENGINE-GAS-NETWORK); excitation by a committed scalar observation (N6), its on and off state the threshold row of `ENGINE-SIGNAL-NODES`; acoustic laws L1–L6. "Blocked flow is silent" is the occlusion of the airflow samples, not a rule about walls.
3. **Three layers.** *Colliders:* tube cylinder, mouthpiece cylinder and foot box on one static body, static default material. *Visuals:* the art table above; no art mesh is a collider. *Animation:* read-only bindings. Moved by a binding: `Reed` (flutter), `ToneBars` nodes (visibility), wavefronts. Moved by nothing: `Tube`, `Mouthpiece`, `Foot`, `Window`.
4. **One-way flow and typing.** Committed airflow or gas rate → emitter → receivers → presentation; nothing returns. `inlet` and `tone` are enums (`ToneBand` for the tone); values are f32.
5. **Flat state.** 1 static body; 3 colliders (2 cylinder, 1 box); 0 joints; 0 drives; 0 sensors; domain nodes: 1 airflow probe of 4 samples, 1 acoustic emitter, 1 threshold signal node (S1 on and off), 0 electrical nodes, 1 fluid port in Gas mode; 0 stores; 5 presentation bindings (reed, three tone-bar visibilities, wavefront).
6. **Performance fitness.** 3 colliders against the Fan's 2 (`elements/Fan/fan.tscn`): the mouthpiece is a separate proud solid. Static body; it joins no island. The probe has a fixed four samples and is bounded by its source's reach; its source and occluder candidates come from `ENGINE-SPATIAL-QUERIES`, which deletes the delivered airflow pass's scan over every collider. A sustained tone is one occurrence, not a pulse per tick. Occurrence tables and reception work follow the D5 bounds in [EL-044](EL-044-tone-selective-sound-meter.md#engine-conformance-22-november-2026) point 6 (sized at Run from declarations, no scene ceiling, candidates from `ENGINE-SPATIAL-QUERIES`).
7. **Scenarios.** Rows 1–8 run the real simulation through the production edit interface; none depends on a code path that names this package.

## 4. Sources and legacy

- Requirement row [element-045](../requirements.md#element-045): "Actual gas flow excites an acoustic source"; outcome "Blocked or absent flow produces no sound event". Scope index [todo-352](../requirements.md#todo-352): "a whistle needs airflow"; [todo-378](../requirements.md#todo-378) "One Breath (stored air/piston/whistle)".
- Named entry [element-045](../invest/named-elements.md#element-045), owner S533. Component research: "Airflow receiver + sustained acoustic source | flow threshold, release threshold | Sounds above the flow threshold; open airflow ≠ pressurised tubing | Reed ← committed flow".
- Legacy: no whistle part, level or lesson. Applicable facts: acoustic model A1–A13; airflow emitter shape and 12 m/s reference flow (`engine/AirflowNetwork.cs@a6c914e:L11-L13`, `L24-L24`); hysteresis pattern (`parts/SoundMeterPart.cs@a6c914e:L64-L71`). The legacy Wind chimes ring only on real tube contact, not on airflow overlap (harvested by [CAT-069](CAT-069-wind_chimes.md)); the whistle differs by sounding from flow itself.
- **Files harvested:** `engine/AirflowNetwork.cs`, `parts/SoundMeterPart.cs`, `parts/SpeakerPart.cs` (tone marks); acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-045](../requirements.md#element-045), [acoustic profile](../invest/profiles.md#acoustic), [pneumatics profile](../invest/profiles.md#pneumatics). Planned test class `AirWhistleTests`. Tolerances and stall guard: [EL-044 shared tolerances](EL-044-tone-selective-sound-meter.md#shared-tolerances-and-stall-guards).

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| 1 | Given a powered Fan 2 m from the mouth, aimed at it, and a supplied High-band meter 3 m from the whistle, when Run starts, then the whistle sounds at strength 1 and the meter reads 0.581 within T-level and triggers once. | `AirWhistleTests.FanAirflowSoundsTheWhistle` | Fan switched off: no emission occurrence at all, meter 0 | `tone` each band against a matching meter |
| 2 | Given a Wall between Fan and whistle, the Fan facing away, or the whistle beyond the Fan's `reach`, then there is no emission occurrence (requirement outcome). A meter beside the running Fan with no whistle reads 0. | `AirWhistleTests.BlockedOrAbsentFlowMakesNoSoundEvent` | row 1 | Wall slid until two of four samples are uncovered: sounds at strength 0.5 |
| 3 | Given the mouth half-shadowed so m = 6 m/s, then it sounds at 0.5; with one sample exposed (3 m/s) it is silent at the default `on_flow`; with `on_flow` 3 it sounds. Once sounding at `on_flow` 6, covering one more sample (m = 3 ≤ 5.4) silences it. | `AirWhistleTests.TheFlowThresholdAndItsHysteresisGateTheSound` | — | `on_flow` at 1 and 12 |
| 4 | (Gas inlet) Given a charged tank → release valve → hose → `GasIn`, when the valve opens, then the whistle sounds, and it falls silent when the mass rate drops to 0.0018 kg/s as the tank drains; total gas is conserved (tank loss = vented amount). | `AirWhistleTests.ReleasedGasSoundsItUntilTheTankIsNearlyEmpty` | valve never opened, or tank at ambient: no emission occurrence | tank pressure giving a rate just above and just below 0.002 kg/s |
| 5 | Reset silences it, clears the emission and restores the flow source; save then load round-trips `inlet`, `tone`, `on_flow` and (Gas) the connection; muted and audible runs are identical. | generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` plus `AirWhistleTests.ResetSilencesTheWhistle` | — | Reset while sounding |
| 6 | `on_flow` below 1, above 12 or non-finite, or a Choice outside its range, is refused atomically with a typed reason; an Electrical or Signal output linked to `GasIn` is refused; in Airflow mode `GasIn` does not exist and a saved link to it is refused. | `AirWhistleTests.OutOfRangeParametersAndForeignLinksAreRefused` | limits accepted | first f32 beyond each limit |
| 7 | (Gas inlet; runs in the later of the EL-041 and Gas-inlet slices.) Given a directional valve whose `Exhaust` is hosed to `GasIn` and whose working port holds a charged volume, when the valve switches so that volume vents, then the whistle sounds while the exhaust mass rate is at or above 0.002 kg/s and is silent before the switch and after the volume reaches ambient ([todo-426](../requirements.md#todo-426): "exhaust → whistle"). | `AirWhistleTests.ValveExhaustSoundsTheWhistle` | `Exhaust` left unconnected: the volume still vents, no emission occurrence | a volume charged just above and just below the pressure that gives 0.002 kg/s |
| 8 | Given a powered Fan run for 5 s with the whistle in its stream and again without it, then the Fan's store debit is equal within T-energy (the probe adds no demand). | `AirWhistleTests.TheWhistleTakesNothingFromTheFan` | a Windmill in the same place: the debit rises | — |

**Chrome recipe (pending, post-catalogue).** Airflow: Fan aimed at the whistle mouth 2 m away; supplied Sound meter 3 m from the whistle. Gas: charged tank → release valve → hose → `GasIn`; Switch → valve.

## 6. Open questions

**Resolved.**

1. **Inlet contract.** Both inlets are required (the row says "gas flow", [todo-352](../requirements.md#todo-352) says "airflow", [todo-426](../requirements.md#todo-426) says "exhaust → whistle"), as one entry with an `inlet` setting: [D8](../invest/delivery-batches.md#3-family-level-decisions). The earlier `pipe_whistle` id is dropped.
2. **Sustained emission.** One sustained occurrence: [D17](../invest/delivery-batches.md#3-family-level-decisions).
3. **Flow drain.** None, by construction: the observation-only probe (Story 14.0, N6) adds no demand. The earlier text rested this on a stationary airflow receiver doing zero work; that was wrong on both counts (the receiver is refused on a static body, and a receiver at rest is charged the most).
4. **Coordinator decisions, 22 November 2026 (owner's standing delegation), applied in the audit repair.** (a) Nothing declares an airflow receiver on a static body; the flow observer is the missing observation-only probe with one owner, Story 14.0, because the code admits no alternative. (b) Pressure is pascals absolute, ambient 101 325 Pa, Fluid links run Output to Input and an Output takes several hoses: one convention for the pneumatic contract, owned by [EL-039](EL-039-air-reservoir.md). (c) `GasIn` is also fed by EL-041's `Exhaust` output, so [todo-426](../requirements.md#todo-426) "exhaust → whistle" is kept in full. (d) Coverage is bound by the published section form and is blocked on S7 (section 7). (e) Palette order 429 (coordinator decision 36).
5. **Propagation repair, 22 November 2026.** The Gas inlet's thresholds are recomputed at the thermal conventions' ambient, 288 K (TH-04 §11 T1); rows 4 and 7 are stated in kg/s and are unchanged. The S1 on and off levels are the threshold row of `ENGINE-SIGNAL-NODES`, tone bars are `ENGINE-PARAMETER-BOUND-FRAMES`' (coordinator decision 16), and probe and reception candidates come from `ENGINE-SPATIAL-QUERIES` (owner decision 3a).

**Needs the owner.** None.

## 7. Integration

- **Works with:** Fan (delivered); Bellows (in flight; there is no `spec-12-4-pneumatic-bellows.md` in this checkout); EL-042, EL-038, EL-039, EL-040 (planned); CAT-060, EL-044 (planned). [EL-052](EL-052-water-tuned-bottle.md) reuses S1 and is planned after it.
- **Campaign:** chapter 8, levels 71–80 ("whistle"); reuse 81–100, 117–125, 136–150. [sequence-task-407](../requirements.md#sequence-task-407); [todo-426](../requirements.md#todo-426) under [sequence-task-393](../requirements.md#sequence-task-393); [todo-378](../requirements.md#todo-378) "One Breath"; processes [IX-12](../requirements.md#interaction-12), [IX-11](../requirements.md#interaction-11), [IX-08](../requirements.md#interaction-08).
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239 (Package, Named identity, Acceptance, Decision, Mode records). **Blocked on the coverage follow-on story S7:** the published form's preconditions are not met. This identity lacks: (1) an acceptance pair of its own, since its ledger entry links only the umbrella `todo-352`, which has no co-anchored `sequence-task-NNN` in `requirements.md` and is shared by nine identities (the reader refuses two identities on one task source); (2) a decision anchor, since its ledger owner S533 has no `<a id="s533">` anchor in `docs/planning/invest/scope-corrections.md`. The delivering slice adds the section once S7 supplies both; it is not written earlier, because the reader throws on a malformed block. Mode records: `Inlet=Airflow`, `Inlet=Gas`.
- **Lesson fixtures:** needed only where a level pre-places it. Such a level waits for [S8, lesson fixtures for any admitted package](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Single element, two data-only slices on one package: (a) Airflow inlet; (b) Gas inlet, adding the second `inlet` option, the port and row 4. The ledger row stays open until (b) passes.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (Story 14.0 takes its reception, occlusion and probe candidates from it), the threshold row of `ENGINE-SIGNAL-NODES` and `ENGINE-PARAMETER-BOUND-FRAMES`; then (a) Story 14.0 amended with gaps 1, 2, 5 and 11; Stories 14.2, 14.1 and 14.3; EL-044 (the High-band meter of row 1); S7. (b) `ENGINE-GAS-NETWORK`, then EL-039, EL-038 and EL-040; row 7 also needs EL-041. Slice (b) also owns the whistle clause of the spring-return piston's exhaust (EL-230 U3 and EL-041 D-A15, `SpringReturnPistonTests.TheExhaustSoundsAWhistleOnEachReturn`), since the piston and valve (74b) are taught before the whistle (77a).
- **Files.** Adds `elements/AirWhistle/` (`AirWhistle.csproj`, `AirWhistleElementDeclaration.cs`, `air_whistle.tscn`, `icon.svg`) and `CuriousContraptions.tests/AirWhistleTests.cs`; registration is derived. The test file needs no `Compile` line (main compiles every top-level `*.cs` of the test project by one rule). Until S3 lands: `CuriousContraptions.slnx`, `CuriousContraptions.tests/CSharpStaticGateTests.cs:703`, `PackageRuntimeParityTests.cs:596`, `:653`. This spec gains its binding section when S7 allows (section 7).
- **Review tier.** Data-only for both slices.
- **Disqualified if** the probe finds no observation-only airflow probe that an emitter can read (N6 absent; then that engine slice comes first), or that the gas vent needs a rule naming this package. The Fan's flow speed is uniform inside its cone (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:76`–`:120`, confirmed by the audit), so row 3's values stand.
