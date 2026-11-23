# TH-37 · Heat-sensitive target — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-37 delivery slice](../invest/named-elements.md#thermal-37). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Units, laws, scale and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); probe rows "TH-04 R<n>" are in [TH-04 section 3](TH-04-electrical-heating-plate.md#3-engine-capabilities).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-37 · Heat-sensitive target · Thermal |
| Anchors | [requirements.md#thermal-37](../requirements.md#thermal-37) (task `sequence-task-471`); [named-elements.md#thermal-37](../invest/named-elements.md#thermal-37); integration task [sequence-task-402](../requirements.md#sequence-task-402) (lens onto the heat-sensitive latch) |
| Owner key / research | S602 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | Historical record [todo-304](../requirements.md#todo-304) is shared with [TH-06 Converging lens](TH-06-converging-lens.md). Sensor sibling [TH-21](TH-21-temperature-sensor.md); goal sibling of the delivered Receiver. Receiver of heat in the [TH-01](TH-01-candle.md), [TH-08](TH-08-heat-conducting-bar.md), [TH-26](TH-26-thermal-storage-block.md), [TH-27](TH-27-phase-change-cartridge.md) and [TH-34](TH-34-timed-toaster-ejector.md) scenes. No CAT refines it. |
| Campaign | Intro 15; practice 16; reuse 59, 90, 145 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-CONTACT-OBSERVATIONS (the moving target's touch link); then ENGINE-SIGNAL-NODES, ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (dwell's one home, the Level output's consumers, the receiver law, story names). |

### Disposition

**To deliver as a single element**, second after TH-04. Planned package id `heat_target`, directory `elements/HeatTarget/`, display name "Heat-sensitive target". Not a duplicate of TH-21: that part is a probe that reads another body; this part is the body whose own temperature is the goal.

## 2. Declaration

Follows the delivered Domino shape (a dynamic part with one signal node and one activation output, `elements/Domino/DominoElementDeclaration.cs:9`, `:29`, `:59-63`).

- **Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the thermal-node contract of ENGINE-THERMAL.
- **Body.** One dynamic body, a puck: 0.3 kg, origin at its centre, axis Y. Solid-cylinder inertia: Iyy = ½ × 0.3 × 0.24² = 0.00864; Ixx = Izz = 0.3 × (3 × 0.24² + 0.08²) ÷ 12 = 0.00448 kg·m². Dynamic because it is cargo the player carries to a heat source; its links come from touch (T3), not from contact force.
- **Collider.** `col_disc`: cylinder, axis Y, radius 0.24 m, height 0.08 m. It is the thermal face (16 W/K, ceramic). Its optical response is Absorb (optical convention 7), and its top face carries a receiver aperture (optical convention 8: a disc of radius 0.24 m, area 0.181 m², one centre sample, all three channels at weight 1/3) whose reading observes the light the disc receives there. `col_disc` declares absorptance 0.9: 0.9 × the light the optical solve booked on its absorbed record in a tick (the receiver's collimated power and conserved diverging shares, and any collimated power ending elsewhere on the disc) moves to `body` in that tick's thermal step, and the rest stays on the record (T3 "Radiation"; booked once, coordinator decision 26). The reading credits nothing by itself.
- **Material (f32).** Friction 0.6, restitution 0.05, rolling resistance 0.
- **Thermal node.** `body`: heat capacity 10.5 J/K (0.05 kg of ceramic at 210 J/(kg·K), declared; the rigid mass is larger so the puck sits firmly), initial temperature ambient, ambient conductance 0.45 W/K (the 0.18 m² top × 2.5).
- **Signal node.** `reached`: a threshold node over the committed temperature of `body` (T7). On level `threshold`; off level `threshold` − 5 K; it asserts only after the temperature has stayed at or above the on level for `dwell` seconds without a dip, and releases at once below the off level.
- **Port.** `PortId.ActivationOut`, `SimulationDomain.Signal`, `PortDirection.Output`, `PortRole.ActivationOut`, `PortSignalKind.Level`, at (0, 0.04, 0) facing +Y. A signal, never power.
- **Goal.** A level's goal is a plain activation goal that names this instance's node `reached`, the way an activation goal names a signal node today (`goal.TargetNode`, read at `CuriousContraptions.tests/GoalSignalSelectorTests.cs:37`). The dwell has one home: the signal node's assert hold (ENGINE-SIGNAL-NODES). It is not a goal hold: hold goals belong to `engine-goal-count-window-hold` (owner decision 12), and this part needs none. No goal kind is added and the goal evaluator learns nothing about temperature.
- **Parameters.**

  | Name | Kind | Unit | Default | Min | Max | Editor policy |
  | --- | --- | --- | --- | --- | --- | --- |
  | `threshold` | `ParameterKind.Real` | K (TH-04 R13) | 330 | 300 | 600 | `ParameterEditorPolicy.Clamp`; editable unless the level locks it (D11); a goal part is locked in a level |
  | `dwell` | `ParameterKind.Real` | `ParameterUnit.Second` | 1.0 | 0.25 | 5.0 | `ParameterEditorPolicy.Clamp`; as above; seconds of game time (D10) |

- **Stores.** None beyond the node.
- **Presentation bindings.** `Visual/Indicator` albedo navy → gold ← committed level of `reached` (`PresentationDriver.CommittedActivation`, `CuriousContraptions.Abstractions/ElementPresentation.cs:22`); `Visual/Disc` emission ← committed temperature of `body` from 288 K to `threshold` (driver of TH-04 R12).
- **Palette.** Category `ElementCategory.Goals`. Order: 202 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Disc` cream `#fff8e9` | cylinder radius 0.24, height 0.08 m | solid, `col_disc` | the body pose |
| `Visual/Marks` navy `#293954` | twelve degree ticks printed flush on the top face | decorative: printed flat | the body pose |
| `Visual/Indicator` navy/gold `#f7cb52` | a flame-shaped mark printed flush at the centre | decorative: printed flat, recoloured by a binding | the body pose; colour by the binding |

Icon: a ticked disc with a centre flame mark. At a glance it differs from the Receiver (an open basket) and from the Pressure plate (a square pad with studs) by its round ticked face.

### Compiled counts

Bodies 1 (dynamic); colliders 1 (cylinder); joints 0; drives 0; sensors 0; thermal nodes 1; faces 1; optical responses 1; signal nodes 1; ports 1; stores 0; presentation bindings 2.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test (body read unless marked) |
| --- | --- | --- | --- | --- |
| TH-04 R1, R5, R6 | Colliders from art; typed ports and refusal; parameter bounds | present | as TH-04 | as TH-04 |
| TH-08 C1 | A dynamic body with a declared mass frame | present | as TH-08 | as TH-08 |
| G1 | A Level activation output lights a Signal lamp while the level holds | present | `PortSignalKind.Level`, `CuriousContraptions.Abstractions/PortDescriptor.cs:14-18` | `PressurePlateTests.ABasketballRestingOnThePlateLightsItsLampOnItsFirstLoadedTick` (`CuriousContraptions.tests/PressurePlateTests.cs:883`) |
| G2 | A goal bound to a declared signal node of one instance | present | `WorkshopPuzzle.Goal` (`SourceNode`, `TargetNode`) | `GoalSignalSelectorTests.SaveRefusesMissingSelectorAndPreservesTheAuthoredGoal` (`CuriousContraptions.tests/GoalSignalSelectorTests.cs:33`) |
| TH-04 R14 | A threshold signal node over a committed scalar, with on and off levels and an assert hold | narrower: contact load only | as TH-04 | not opened; owner ENGINE-SIGNAL-NODES |
| TH-04 R9, R10, R11, R12, R13 | Node, contact conduction, ambient, temperature driver and observation, kelvin unit | **missing** | as TH-04 | none; owner ENGINE-THERMAL |
| G3 | A face's declared absorptance share of the light booked on its collider's absorbed record, moved to the node that owns the face (T3 "Radiation") | **missing** | none (no optical or thermal engine) | none; owner ENGINE-THERMAL |
| G4 | A Level output wired to a signal input other than a lamp's (a latch's Set, a contact's control) | **narrower**: a Level output is exercised today only lighting a Signal lamp (`CuriousContraptions.tests/PressurePlateTests.cs:698`, `:883`) | `PortSignalKind.Level` | none for other consumers; owner ENGINE-SIGNAL-NODES |

Counts: present 6, untested 0, narrower 2 (R14, G4), missing 6. Owners, one each: R14 and G4 are **ENGINE-SIGNAL-NODES** ("threshold over a committed scalar" with an assert hold; "a Level output drives any typed signal input of matching kind"; proposed text in the [Batch N summary](readiness/batch-n.md)); R9 to R13 and G3 are **ENGINE-THERMAL** (G3 reads receivers that ENGINE-OPTICAL delivers; until then the absorbed power is only ledgered, Batch K decision K-10). A level with several goals needs `engine-goal-evaluation`, whose drafted contract already has an `Activated` observation on a signal node; this part adds nothing to it.

## 4. Sources and legacy

- **Requirement row** [thermal-37](../requirements.md#thermal-37): a finite thermal body exposes a typed temperature/phase goal observation with explicit threshold and dwell; sensing is separate from the heating source; no variants.
- **Named entry** [thermal-37](../invest/named-elements.md#thermal-37): owner S602; "Warm another disconnected target or remain below the target threshold and the goal stays false".
- **Research** per-element table: "goal node with threshold"; target T, dwell; "solved when held above temperature for the dwell"; tint ← T.
- **Element map** row TH-37: FiniteLedger, ObjectiveEvaluation, SensibleHeat, SignalPropagation, TemperatureSensing. Navigation only (D14).
- **Processes.** [IX-17](../requirements.md#interaction-17), [IX-18](../requirements.md#interaction-18), [IX-14](../requirements.md#interaction-14), [IX-43](../requirements.md#interaction-43).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for heat target, temperature goal and thermal terms. The goal evaluator and dwell-counted residence sensors the earlier revision cited (`engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L5-L8`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L123-L141`) are the harvest baseline only.

## 5. Acceptance

Point of truth: [thermal-37](../requirements.md#thermal-37), [sequence-task-402](../requirements.md#sequence-task-402) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Contact and ambient exchange (T3); a threshold with a hold over the committed temperature (T7); the existing activation goal.
- **On the Heating plate** (T14: 120 W, disc 15.25 J/K, 0.5 W/K). Pair: metal on ceramic, 64 × 16 ÷ 80 = 12.8 W/K. With θ the rise above 288 K, steady state: 12.8·(θ_d − θ_t) = 0.45·θ_t gives θ_d = 1.0352·θ_t; 120 = 0.5 × 1.0352·θ_t + 0.45·θ_t = 0.9676·θ_t, so θ_t = 124 K (412 K) and the disc 416 K. Stored heat 15.25 × 128.4 + 10.5 × 124 = 3260 J, time constant 3260 ÷ 120 = 27.2 s. The target passes 330 K at about −27.2·ln(1 − 42 ÷ 124) = 11.2 s plus 10.5 ÷ 12.8 = 0.8 s of lag: about 12 s; the goal solves about 13 s after Run, for about 1.6 kJ.
- **Supply cut early.** Battery `capacity` 840 J empties at 7 s with the target near 288 + 124 × (1 − e^(−6.2 ÷ 27.2)) = 313 K and the disc about 4 K hotter; they equalise near (15.25 × 29.1 + 10.5 × 25.3) ÷ 25.75 = 27.5 K of rise, 315.5 K. The goal stays false.
- **Short excursion.** `capacity` 1500 J empties at 12.5 s with the target just past 330 K; it peaks near 334 K and falls with the time constant 25.75 ÷ 0.95 = 27 s, staying above 330 K for about 27 × ln(45.7 ÷ 42) + 1 = 3 s. That is longer than a 1 s dwell and shorter than a 5 s dwell.
- **Another target.** Each target's node, signal node and goal binding are its own; heating one changes nothing in another.
- **Why 10.5 J/K, 330 K and 1 s.** A default Battery must solve the first lesson in well under its 30 s; 330 K is clear of ambient and far below any ignition threshold (T11); one second rejects a touch-and-go.
- **Light.** Under ENGINE-OPTICAL the face's receiver takes its conserved share of each diverging segment (optical convention 8), booked on `col_disc`'s record, and the node is credited 0.9 of that record, never more than arrives; the optical solve runs after the last substep and the record's share moves in the same tick's thermal step (T3, T8, [combined tick order](EL-176-red-laser-emitter.md#combined-tick-order)); the lens scene is TH-06's.
- **A horizontal beam.** The puck lies flat, so a level that lights it from the side leans it on a Wall or turns the beam with a mirror.
- **Tick order.** T8: the temperature of tick N commits with tick N; `reached` is evaluated with tick N's other observations; the lamp and the goal read it from tick N + 1 (D7).
- **Run length these rows need: 40 s** (A1's stall guard is the smaller of 3 × 12.2 s and 12.2 + 60 s, 37 s, where 12.2 s is the 11.2 s to 330 K plus the 1 s dwell; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).
- **Tolerances.** T12. Stall guard 39 s (3 × 13 s).

### Rows

Planned class `HeatTargetTests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a level whose goal names a target lying on a supplied Heating plate, when the plate runs, then the goal is Solved within 37 s, once, and the target reads at least 330 K at that moment | `ATargetHeldAboveItsThresholdSolvesTheGoal` | the plate unwired: Waiting for the whole guard | `threshold` 330 against a steady 412 K | goal phase; temperature ± 2 K |
| A2 | Given two targets, the goal's on the floor and another on the plate, then the other's node asserts and the goal stays Waiting | `WarmingAnotherTargetDoesNotSolve` | A1 | none | goal phase |
| A3 | Given A1 with Battery `capacity` 840 J, then the target peaks below 330 K and the goal stays Waiting | `BelowTheThresholdTheGoalStaysFalse` | A1 | peak about 316 K | peak below 328 K |
| A4 | Given `capacity` 1500 J, when the committed temperature is at or above 330 K for a span H with 1 s < H < 5 s (checked first, as a guard on the scene), then `dwell` 0.25 s solves and `dwell` 5 s does not | `AnExcursionShorterThanTheDwellDoesNotSolve` | the 0.25 s case | `dwell` at its minimum and maximum | goal phase |
| A5 | Given A1, then the Battery's debit equals the heat stored in disc and target plus the ambient account: observing adds no heat | `ObservingTheTargetAddsNoHeat` | none | none | 2% of the debit |
| A6 | Given A1 with a Signal lamp on `ActivationOut`, then the lamp lights when `reached` asserts and not before | `TheTargetsLevelOutputLightsALamp` | lamp unwired stays dark | none | causal order |
| A7 | Given A1 then Reset, then the node is at 288 K, `reached` is clear, the goal is Waiting and a second Run solves again | `ResetRestoresTemperatureLevelAndGoal` | none | none | as A1 |
| A8 | Given a placed target with `threshold` 350 and `dwell` 2, when saved and loaded, then both values, the pose and the connection are kept | `SaveAndLoadKeepThresholdAndDwell` | none | none | exact values |
| A9 | `ActivationOut` wired as an electrical link, or into an output, is refused; a Level output into a Pulse input is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (TH-04 R5) | — | — | typed refusal |
| A10 | `threshold` or `dwell` outside its range is clamped (`ParameterEditorPolicy.Clamp`) | generic: TH-04 R6 | bounds admitted | 300, 600, 0.25, 5 | clamped value exact |
| H1 | TH-04's row A5 (`HeatingPlateTests.OnlyABodyOnTheDiscIsHeated`): a target on the disc warms more than 5 K in 10 s; an identical one on the floor against the plinth's side warms less than 0.01 K | run in this slice | the second target | the plinth margin is 0.10 m wide, too narrow for a 0.48 m puck, so the control touches `col_plinth` at its side | as stated |

Requirement clauses: threshold and dwell (A1, A4), sensing separate from the source (A5), another target (A2), below threshold (A3).

## 6. Open questions

Resolved:

1. **Game scale.** D5, restated once in TH-04 section 11.
2. **Phase goals.** Not declared (D19): no lesson uses one; a phase fraction is an observation any part may threshold (T7).
3. **Dwell semantics.** Continuous: a dip below the on level restarts the hold. Decided now: "held above temperature for the dwell" (research table) and a cumulative count would need history the node does not keep.
4. **Activation output.** Kept, because sequence-task-402 actuates a latch from it. A Level output driving a latch is probe row G4, owned by ENGINE-SIGNAL-NODES; until that row is delivered the output is proven only on a Signal lamp (A6).
5. **Static or dynamic.** Dynamic, decided now: it is cargo the player moves; links come from touch (T3, coordinator decision 21), not from its being dynamic.
6. **Dwell's home.** The signal node's assert hold; the goal is a plain activation goal (owner decision 12 puts hold goals in `engine-goal-count-window-hold`, which this part does not use).
6. **Map extras** (ReliablePublication, TypedContracts): navigation only (D14).

Needs the owner: none.

## 7. Integration

- **Works with.** TH-04 (planned first), Signal lamp (delivered), TH-21, TH-08, TH-26, TH-27, TH-01, TH-06, TH-34.
- **Campaign.** "A Matter of Degrees": introduction 15, practice 16; reuse 59, 90, 145. A level locks the goal target; fixed partners need the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element; it also runs row H1 for TH-04.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme, including ENGINE-SIGNAL-NODES (before its first consumer; R14 and G4) and ENGINE-CONTACT-OBSERVATIONS; ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18); ENGINE-RUN-LENGTH; TH-04; S7; S3 preferred. Light rows wait for ENGINE-OPTICAL and belong to TH-06's slice.
- **Files.** `elements/HeatTarget/HeatTarget.csproj`, `HeatTargetElementDeclaration.cs`, `heat_target.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); `CuriousContraptions.tests/HeatTargetTests.cs`; one test added to `HeatingPlateTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if the probe finds every row present and tested; otherwise the engine tier.
- **Disqualifiers.** A threshold node without an assert hold (the dwell would then need a declared timer node in series); a goal that cannot bind a Level node; faces per body (harmless here); a different scale.

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** One dynamic body, one cylinder, one node, one threshold signal node, one port, two parameters, two bindings. The goal is level data naming the node. No handler or engine branch on this part.
2. **Generic laws.** Q̇ = G·ΔT; a threshold with hold over a committed scalar; the activation goal. "Solved when hot" is their composition.
3. **Three layers.** Collider `col_disc`. Visuals: disc art with its collider; printed marks. Animation: indicator colour and disc glow from committed state; nothing is written back.
4. **Flow and typing.** One Signal Level output; f32 parameters; typed node ids; enum port kinds.
5. **Flat state.** The counts in section 2.
6. **Performance.** One primitive collider; no sensor volume; thermal work is one node and one term per contact (T10). It rests in use. Island sleeping is owned by ENGINE-ISLANDS-SLEEP (owner decision 3a); a resting target is stepped every tick until then, and its node keeps exchanging while asleep (T3).
7. **Tests are scenarios.** A1 to A8 and H1 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL and ENGINE-SIGNAL-NODES, general capabilities stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `heat_target` |
| Named identity | TH-37, `../invest/named-elements.md#thermal-37` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-471` and the ledger entry has no todo link (`todo-304` in the requirements is a historical record shared with TH-06) |
| Decision | S602; `scope-corrections.md` on published main has no `s602` block |
| Mode records | `Configuration=Fixed` |
