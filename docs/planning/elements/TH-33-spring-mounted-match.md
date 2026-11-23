# TH-33 · Spring-mounted match — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-33 delivery slice](../invest/named-elements.md#thermal-33). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`); repaired the same day against the Batch N audit (`docs/planning/elements/readiness/audits/elements-batch-n.md`, findings A2, N2 and the coined story name). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here (steady values from the steady-state balance, transients from lumped estimates) and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal rules are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); burning follows the [combustion conventions](TH-01-candle.md#combustion-conventions) (F1 to F6). Neither is restated.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-33 · Spring-mounted match · Thermal |
| Anchors | [requirements.md#thermal-33](../requirements.md#thermal-33) (task `sequence-task-467`); [named-elements.md#thermal-33](../invest/named-elements.md#thermal-33) |
| Owner key / research | S595 · [TH-S03 combustion and suppression](../../thermal-component-research.md#th-s03) |
| Related identities | Spring and release follow the delivered [CAT-071 Wound spring](CAT-071-wound_spring.md) and [CAT-062 Springboard](CAT-062-spring.md); friction heat is the same source as [TH-31 Flint striker](TH-31-flint-striker.md); lights [TH-32](TH-32-tinder-pad.md), [TH-02](TH-02-fire-bowl.md) and the [TH-01](TH-01-candle.md) wick. No CAT refines it. |
| Campaign | Intro 85; practice 86; reuse 99, 149 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-CONTACT-OBSERVATIONS for the contact friction work and ENGINE-SPATIAL-QUERIES for the region query, and on ENGINE-JOINT-KINDS (the hold and its release by a physical request bound to `ActivationIn`: its authored-position retention, armed at Run start; holds on a coordinate and their release, engine-stories-index C2), ENGINE-THERMAL and ENGINE-IGNITION-BLAST. |

### Disposition

**To deliver as a single element.** Planned package id `spring_match`, directory `elements/SpringMatch/`, display name "Spring match". Not a duplicate of the Wound spring: it adds a reactive tip, a strike pad and a wound start.

## 2. Declaration

Follows the delivered Wound spring shape for a retained sprung slider released by a pulse (`elements/WoundSpring/WoundSpringElementDeclaration.cs:55-69`) and the Springboard for an authored starting compression (`elements/Springboard/SpringboardElementDeclaration.cs:29`, `:63`).

- **Interfaces.** `ISliderElement` and `IPrismaticRetentionElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`, `:85`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the thermal and fuel contracts. Footprint 3 bodies, 2 moving, 2 prismatic, 0 revolute. The match travels along local +X.
- **Body 0, the guide (static; boxes, metres).** `col_rail`: centre (0, 0.05, 0), half extents (0.30, 0.05, 0.05). `col_block`: centre (0.40, 0.11, 0), half extents (0.06, 0.11, 0.06), the housing behind the strike pad.
- **Body 1, the match (dynamic, 0.2 kg: a wood shaft with a brass collar).** `col_shaft`: box, half extents (0.15, 0.015, 0.015). `col_tip`: box at its front end, half extents (0.015, 0.02, 0.02); the face of node `tip`.
- **Slider 0** (guide to match, axis X): travel −0.15 to +0.02 m; stiffness 400 N/m; damping 0; starting compression 0.15 m; the match and the guide do not collide.
- **Body 2, the strike pad (dynamic, 0.05 kg).** `col_pad`: box, half extents (0.02, 0.06, 0.06), its face turned 15° from the travel direction so the arriving tip wedges along it. No thermal node.
- **Slider 1** (guide to pad, along the pad's normal): travel −0.07 to −0.05 m; stiffness 2000 N/m; so the pad rests on its upper stop pressed out with 2000 × 0.05 = 100 N and yields up to 0.02 m.
- **Retention.** On slider 0, holding the starting compression until released (the addition of section 3).
- **Material (f32).** Tip and pad: friction 0.8 (pair coefficient √(0.8 × 0.8) = 0.8), restitution 0.05. Other colliders: friction 0.3, restitution 0.1.
- **Thermal node.** `tip`, on body 1: 0.00425 J/K (10 mg of fibre), ambient 0.01 W/K, face `col_tip` 16 W/K, ignite row at 450 K, extinction 420 K.
- **Frictional source** (T4). Work dissipated by contact friction at `col_tip`, summed over all four substeps of the tick, credited to `tip`. Its share is 1.0 against any collider that owns no node, such as the pad.
- **Fuel store.** `match_fuel` on `tip`: 0.0105 kg × 15 625 = 164 J (the head and the front of the shaft); 12 W while lit, credited to `tip` (F1, a thin fuel); oxidiser 1.2/1024 kg of O₂ per kg.
- **Region link** (T3; audit A2). A box 0.20 × 0.20 × 0.20 m on body 0 centred at (0.30, 0.13, 0.20) (0.03 to 0.23 m above the guide's base, 0.10 to 0.30 m to the side of the rail), beside the strike point, axis +Y, fed by `tip`, G_r 0.06 W/K (was 0.1). A face collider's share is the area of overlap of its bounds with the box projected on the guide's XZ plane ÷ 0.04 m², granted nearest-first along +Y, the sum capped at 1. The match's own colliders never enter the box (it travels at z = 0).
- **Port.** `PortId.ActivationIn`, `SimulationDomain.Signal`, `PortDirection.Input`, `PortSignalKind.Pulse`, at (−0.30, 0.05, 0.06) facing +Z, bound to a physical request that releases the retention (the Wound spring's binding, `WoundSpringElementDeclaration.cs:67-69`).
- **Parameters.** None (D13).
- **Presentation bindings.** `Visual/Shaft` and `Visual/Pad` follow bodies 1 and 2; `Visual/Coil` stretches between the guide and the match (the Springboard's coil binding, `SpringboardElementDeclaration.cs:14`); `Visual/Latch` rotation ← `PresentationDriver.CommittedRetentionReleased` (`CuriousContraptions.Abstractions/ElementPresentation.cs:32`); `Visual/TipFlame` scale and emission ← committed burn power (0 to 12 W); `Visual/TipMark` albedo ← committed fuel fraction.
- **Palette.** Category `ElementCategory.Power` (TH-04 section 6, item 5). Order: 337 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Rail`, `Visual/Block` cream `#fff8e9` | boxes as the colliders | solid, `col_rail`, `col_block` | nothing |
| `Visual/Shaft` warm wood `#c28f52` with a navy `#293954` tip mark printed on its front block | boxes as `col_shaft` and `col_tip` | solid | body 1 pose |
| `Visual/Pad` navy, cross-hatched | box as `col_pad` | solid, `col_pad` | body 2 pose |
| `Visual/Coil` gold `#f7cb52` | a coil drawn inside the rail's top groove, behind the match | decorative: stretched by a binding; anything that pushes is the match body | presentation binding |
| `Visual/Latch` gold | a small hook printed on the rail side | decorative: turned by a binding | presentation binding |
| `Visual/TipFlame` gold | a teardrop at the tip | decorative: glow | body 1 pose; presentation binding |

Icon: a rail with a coil, a match and a hatched pad. At a glance it differs from the Wound spring by the match shaft and the hatched pad at the far end.

### Compiled counts

Bodies 3 (1 static, 2 dynamic); colliders 5 (boxes); prismatic joints 2; retentions 1; signal nodes 1; ports 1; thermal nodes 1; frictional sources 1; region links 1; fuel stores 1; ignite rows 1; presentation bindings 5 plus two body poses. Two sliders against the Wound spring's one: the pad must yield, or the tip would meet a rigid wedge and stop without sliding.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R5 | Colliders from art; typed port, wrong-kind refusal | present | as TH-04 | as TH-04 |
| M1 | A sprung slider compiled with an authored starting compression | present | `PrismaticSliderDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:104` | `WorkshopSpringboardTests.CompiledPreloadUsesActualF32PoseAndEightJoules` (`CuriousContraptions.tests/WorkshopSpringboardTests.cs:146`; body read) |
| M2 | A retention on a sprung slider, released by a signal request | present | `PrismaticRetentionDescriptor`, `CuriousContraptions.Abstractions/PrismaticRetentionValues.cs:13` | `PrismaticRetentionTests.PaidWindingRetainsAndIndependentReleaseReturnsToRest` (`CuriousContraptions.tests/PrismaticRetentionTests.cs:16`; body read) |
| M3 | A retention that is armed at Run start from the authored compression, with no winding | narrower: the descriptor requires a winding transmission and starts Idle (`PrismaticRetentionValues.cs:13-15`; M2's test asserts `RetentionPhase.Idle` at admission) | — | ENGINE-JOINT-KINDS (holds on a coordinate and their release; engine-stories-index C2): the hold and its release by a physical request bound to `ActivationIn` — its authored-position retention for prismatic and revolute coordinates, which holds the authored coordinate with no winding, is a physical-request target and releases once per Run ([Story ENGINE-JOINT-KINDS](../../../_bmad-output/planning-artifacts/epics.md#story-engine-joint-kinds-joint-kinds-holds-and-drives-on-a-coordinate), "Holds on a coordinate"; shared with RAD-21) |
| M4 | Two moving bodies of one package colliding with each other; a slider resting preloaded on its stop | present, untested: no delivered package does either | `PrismaticSliderDescriptor.BodiesCollide` covers only a slider's own pair | none |
| M5 | Contact friction work, summed over a tick's four substeps, credited to a thermal node by declared share | **missing** | none | ENGINE-THERMAL (T4, T8); the per-pair contact work comes through ENGINE-CONTACT-OBSERVATIONS |
| TH-04 R9, R11 to R13 | Thermal node, ambient, drivers, units | **missing** | none | ENGINE-THERMAL |
| TH-01 C1 | Store fraction as a driver | narrower | as TH-01 | ENGINE-IGNITION-BLAST |
| TH-01 C2 to C5 | Ignition and burn, oxidiser, spent solid, region link | **missing** | none | ENGINE-IGNITION-BLAST (C2 to C4), ENGINE-THERMAL (C5) |

Counts: present 4, untested 1 (M4), narrower 2 (M3, C1), missing 9. Each narrower or missing row has one owner. Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-33](../requirements.md#thermal-33): a finite reactive tip on a generic spring and contact assembly receives ignition energy from modelled striking work; its position and stored spring energy remain physical; no variants. Visual style: "Cream guide, gold visible spring and warm-wood shaft with a small original tip mark".
- **Named entry** [thermal-33](../invest/named-elements.md#thermal-33): owner S595; "No contact or spent reactive tip cannot ignite; winding the spring alone supplies no flame".
- **Research** per-element table: "spring + friction strike + ignition"; "releases, strikes, lights".
- **Element map** row TH-33: ChemicalReaction, ElasticStorage, EnvironmentState, FiniteLedger, GeometryQuery, Ignition, JointConstraint, RigidBodyDynamics, SensibleHeat, TopologyTransaction (D14; sliding friction is row M5).
- **Processes.** [IX-02](../requirements.md#interaction-02), [IX-29](../requirements.md#interaction-29), [IX-28](../requirements.md#interaction-28), [IX-03](../requirements.md#interaction-03).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for match, spring strike and ignition terms. Consulted, no element knowledge: `reference/P0-022-before/docs/coverage/engine/task-002.json@a6c914e:L2783-L2787`.

## 5. Acceptance

Point of truth: [thermal-33](../requirements.md#thermal-33) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Stored energy.** ½ × 400 × 0.15² = 4.5 J, held by the retention with 400 × 0.15 = 60 N.
- **Flight.** The tip meets the pad when the slider is at −0.04 m: kinetic energy ½ × 400 × (0.15² − 0.04²) = 4.18 J, speed √(2 × 4.18 ÷ 0.2) = 6.5 m/s, which is 6.5 ÷ 480 = 0.0135 m per substep against a 0.04 m thick pad.
- **The strike.** With 100 N on the pad the travel is resisted by 100 × (sin 15° + 0.8 × cos 15°) = 100 × (0.259 + 0.773) = 103 N; the spring still pushes with about 8 N; the match stops in 4.18 ÷ 95 = 0.044 m. Friction takes 0.773 ÷ 1.032 = 0.75 of the work: about 3.3 J. The rest, about 1.2 J, pushes the pad back 0.044 × sin 15° = 0.011 m against its spring and stays there as elastic energy, because a 15° wedge with friction 0.8 is self-locking (tan 15° = 0.27 < 0.8). Total 4.18 + 0.32 (the spring's last 0.044 m) = 4.5 J.
- **Where the friction work goes.** All of it, about 3.3 J summed over the strike's substeps, enters `tip`: the pad owns no node. The tip rises by 3.3 ÷ 0.00425 = 776 K in about 0.014 s, far past the 162 K it needs (0.69 J): it ignites.
- **Burn.** 164 ÷ 12 = 13.7 s. Free tip 288 + 12 ÷ 0.01 = 1488 K.
- **Covered tip** (audit A2). A 288 K thermal body whose bounds cover the box takes the full 0.06 W/K: the burning tip settles at 288 + 12 ÷ (0.01 + 0.06) = 459 K, 39 K above its 420 K extinction (with the earlier 0.1 W/K it was 288 + 12 ÷ 0.11 = 397 K and went out). Its committed temperature can sit about 8% of the rise below that, near 448 K, depending on where T8 applies a tick's credit; still above 420 K.
- **Lighting tinder beside it** (M-5: a Tinder pad lying on the bench with its wafer's bounds covering the box, 0.06 W/K; the unlit fibre loses 0.02 W/K). With a = T_tip − 288 and p = T_fibre − 288: 0.06·(a − p) = 0.02·p gives a = 1.333·p, and 12 = 0.01·a + 0.02·p gives p = 12 ÷ 0.0333 = 360: the fibre would settle at 648 K and the tip at 768 K. The fibre passes 450 K within a tenth of a second of the strike, while the struck tip is still near 1000 K.
- **Lighting a Fire bowl's bed beside it.** The guide stands beside the bowl, its block's side against the outside of a rim and its base 0.04 m above the floor (a static part may be placed off the floor with nothing under it, as the locked Wall "shade" of puzzle `solar_shadow` is, `content/puzzles.json:8118`), so that the box (0.07 to 0.27 m up, starting 0.01 m inside the rim) covers 0.95 of a 0.20 m strip of the bed and none of the bowl's base or rims: G = 0.06 × 0.95 = 0.057 W/K ([TH-02](TH-02-fire-bowl.md) gives the bowl's geometry). The bed surface (0.2125 J/K) loses 0.0196 W/K unlit. The tip at quasi-steady state is a = (12 + 0.057·b) ÷ 0.067, so the surface gains 0.057·(a − b) = 10.21 − 0.0085·b and keeps 10.21 − 0.0281·b, with b = T_surface − 288: it settles at 288 + 363 = 651 K with a time constant of 0.2125 ÷ 0.0281 = 7.6 s, and passes 573 K at 7.6 × ln(363 ÷ 78) = 11.6 s, inside the 13.7 s burn. The tip starts at 288 + 12 ÷ 0.067 = 467 K and only rises from there.
- **Lighting a candle.** Worked in [TH-01](TH-01-candle.md) section 5 (the guide mounted beside the column with its box over 0.95 of the dish).
- **Blocked short of the pad.** A solid in the 0.11 m gap stops the match head-on: no sliding, so no friction work and no heat.
- **Winding alone.** With no pulse nothing moves; a frictional source with no motion credits nothing.
- **Tunnelling.** At 0.0135 m per substep the match is inside the engine's general speculative margin, 0.004 m + relative speed × substep = 0.004 + 0.0135 = 0.0175 m (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:1117`; `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:33`). That non-sphere bodies meet the engine's tunnelling envelope is unproven (`docs/planning/elements/readiness/engine-design-gap.md:46`); if a box fails, the general fix is a swept test in ENGINE-SPATIAL-QUERIES' segment query (`docs/planning/elements/readiness/engine-design-gap.md:247`), not a rule of this part.
- **Tick order.** A pulse committed in tick N releases the retention in that tick's physics step, as for the Wound spring; the friction work of all four substeps of tick N is credited in its thermal step (T8).
- **Run length.** Every row fits in the default 30 s (M-1 burns out by 0.1 + 13.7 × 1.15 = 15.9 s).
- **Tolerances.** Friction heat 10% of the stored 4.5 J (the margin on ignition is a factor of four); positions 0.005 m; times 15%; stall guard 10 s (T12: three times a hand time under 3.3 s, raised to the 10 s minimum) and 30 s for burn-out (the hand time + 60 s is past the Run length, which caps it).

### Rows

Planned class `SpringMatchTests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| M-1 | Given a pulse on `ActivationIn`, then the match strikes the pad, the tip is burning within 10 s, its store pays 164 J and it goes out | `AReleasedMatchStrikesAndLights` | no pulse for 10 s: the slider stays within 0.005 m of −0.15 m, the tip stays at 288 K and nothing burns (winding alone supplies no flame) | none | stall guard 10 s to light, 30 s to burn out; 5 J |
| M-2 | Given a Wall across the gap before the pad and a pulse, then the match stops on the Wall, the heat credited to `tip` is under 0.3 J and nothing burns | `WithoutSlidingContactThereIsNoIgnition` | M-1 | none | as stated |
| M-3 | Given M-1, then the heat credited to `tip` equals the committed friction work at `col_tip` summed over every substep and is below 4.5 J; friction heat + pad spring energy + what the main spring still holds = 4.5 J | `StrikingWorkIsTheOnlyIgnitionEnergy` | M-1's control: all zero | none | 0.45 J |
| M-4 | Given M-1 after burn-out and a second pulse, then the request answers `AlreadyReleased`, the store is 0 and nothing burns | `ASpentTipCannotIgnite` | M-1 | none | exact |
| M-5 | Given a Tinder pad lying on the bench with its wafer covering the region box and M-1, then the pad is burning within 10 s | `TheMatchLightsTinderBesideIt` (needs TH-32) | the pad 0.5 m away stays unlit | none | stall guard 10 s |
| M-6 | Given M-1 then Reset, then the slider is back at −0.15 m and retained, the pad on its stop, the tip full and at 288 K | `ResetRewindsTheSpringAndRestoresTheTip` | none | none | 0.001 m; exact store |
| M-7 | Given a placed match with a wired `ActivationIn`, when saved and loaded, then pose and connection are kept and a fresh Run starts wound | `SaveAndLoadKeepPoseAndConnection` | none | none | exact |
| M-8 | An electrical output wired to `ActivationIn` is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (TH-04 R5) | — | — | typed refusal |

No parameters, so no parameter row. The requirement's outcome: "no contact" is M-2, "spent reactive tip" is M-4, "winding the spring alone" is M-1's control.

## 6. Open questions

Resolved:

1. **Game scale.** D5; stated in TH-04.
2. **Release method.** A pulse on `ActivationIn`, the Wound spring's delivered release (decided now: a loose pin would be a third moving body and an untested contact release; an Impact switch gives the same physical trigger).
3. **Map row.** Navigation only (D14); sliding friction is row M5.
4. **Re-arming.** None within a Run (D19); Reset rewinds it.
5. **Where the friction work goes.** All of it into the tip node; the earlier "half the friction work, capped at 4 J" discarded the other half without saying where it went and is withdrawn.
6. **Match mass.** 0.2 kg, not 0.02 kg (decided now: at 0.02 kg the tip would arrive at 21 m/s, 0.044 m per substep, wider than the pad).
7. **Region conductance** (audit A2). 0.06 W/K (decided now: a covered tip stays 39 K above extinction, and a bed beside it still lights inside the burn).

Needs the owner: none.

## 7. Integration

- **Works with.** Impact switch, Pressure plate and other pulse sources (delivered); TH-32, TH-02, TH-01 (lit by it); Wall (delivered) for M-2.
- **Campaign.** Introduction 85, practice 86; reuse 99, 149. Lesson fixtures need story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme, including ENGINE-SPATIAL-QUERIES and ENGINE-CONTACT-OBSERVATIONS; ENGINE-JOINT-KINDS with its authored-position retention (holds on a coordinate and their release; engine-stories-index C2); ENGINE-GAS-NETWORK, ENGINE-LIQUID and ENGINE-THERMAL as amended, in the order of owner decision 18; ENGINE-IGNITION-BLAST as amended; TH-32 for M-5; S7. This slice also owns TH-32's cross-part rows P5 and P9 (the pad, 58, and TH-26, 81, are taught before the match, 85).
- **Files.** `elements/SpringMatch/SpringMatch.csproj`, `SpringMatchElementDeclaration.cs`, `spring_match.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; `CuriousContraptions.tests/SpringMatchTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Engine tier unless M3 to M5 are present and tested when the slice starts.
- **Disqualifiers.** The probe finds that two moving bodies of one package cannot collide, that a slider cannot rest preloaded on its stop, or that the wedge rebounds instead of sliding (then the pad is redesigned before any value is tuned); contact friction heat split with no declared share, or read from one substep only; a box-on-box contact at 6.5 m/s that tunnels (then the swept test of ENGINE-SPATIAL-QUERIES comes first).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Five colliders on three bodies, two sliders, one retention, one signal request, one node, one frictional source, one fuel store, one region link, five bindings. No strike detector and no ignition script.
2. **Generic laws.** Spring and retention; contact with Coulomb friction; "dissipated friction work is heat"; threshold ignition; Q̇ = G·ΔT by T9's exchange.
3. **Three layers.** Colliders: rail, block, shaft, tip, pad. Visuals: the same solids; a coil, latch and flame that push nothing. Animation: coil stretch, latch, flame and tip mark from committed state.
4. **Flow and typing.** One Signal input of kind Pulse; enums for domain, role and request result; f32 values.
5. **Flat state.** The counts in section 2.
6. **Performance.** Two light dynamic bodies that rest before release and after the strike. Island sleeping is owned by ENGINE-ISLANDS-SLEEP (owner decision 3a); a resting match and pad are stepped every tick until then, and the tip node keeps exchanging while asleep (T3). The match is the family's fastest body, 6.5 m/s: 0.0135 m per substep against a 0.04 m pad, inside the general speculative margin; the unproven non-sphere case belongs to ENGINE-SPATIAL-QUERIES (section 5). One bounded region query per tick.
7. **Tests are scenarios.** M-1 to M-7 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, ENGINE-IGNITION-BLAST and ENGINE-JOINT-KINDS (general capabilities) and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader admits only an `EL-NNN` identity.

| Field | Planned value |
| --- | --- |
| Package | `spring_match` |
| Named identity | TH-33, `../invest/named-elements.md#thermal-33` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-467` and the ledger entry has no todo link |
| Decision | S595; `scope-corrections.md` on published main has no `s595` block |
| Mode records | `Configuration=Fixed` |
