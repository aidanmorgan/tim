# TH-05 · Friction brake — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-05 delivery slice](../invest/named-elements.md#thermal-05). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Thermal rules are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14) and are not restated.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-05 · Friction brake · Thermal |
| Anchors | [requirements.md#thermal-05](../requirements.md#thermal-05) (task `sequence-task-439`); [named-elements.md#thermal-05](../invest/named-elements.md#thermal-05) |
| Owner key / research | S574 · [TH-S01 heat transfer and finite stores](../../thermal-component-research.md#th-s01) |
| Related identities | **A thermal criterion on [EL-055 Mechanical brake](EL-055-mechanical-brake.md)** (Batch I; package `mechanical_brake`). Shaft supply: [CAT-042 Motor](CAT-042-motor.md) (delivered). Heat receivers: [TH-26](TH-26-thermal-storage-block.md), [TH-08](TH-08-heat-conducting-bar.md). |
| Campaign | Intro 39; practice 40; reuse 81, 147 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), EL-055's slice and ENGINE-THERMAL (with ENGINE-SUPPLY-DEMAND for row H5). Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (status wording, the EL-055 file edit, friction summed over substeps, the visual clause). |

### Disposition

**Not a package, not a duplicate and not a family member: a later thermal criterion on the one brake package** ([delivery batches 2d](../invest/delivery-batches.md), line 152: "EL-055's slice delivers the brake; TH-05 is the later thermal criterion on the same package"). It is not a Duplicate closure because it has a delta: heat. Its slice edits `elements/MechanicalBrake/` to add the thermal declarations of section 2 and adds the rows of section 5. Bodies, hinge, drive, ports, parameter and art are EL-055's and are not repeated or changed here.

## 2. Declaration

What this criterion adds to EL-055's declaration ([EL-055 section 2](EL-055-mechanical-brake.md#2-declaration): static housing, body 0; drum and shaft, body 1, 0.25 kg, radius 0.28 m, on continuous hinge 0 with 0.02 N·m bearing friction and a supplied drive of target speed 0 bounded by `torque_capacity`).

- **Interfaces.** EL-055's, plus the thermal contract of ENGINE-THERMAL.
- **Thermal nodes.** `drum`, on body 1: 30.5 J/K (0.25 kg of steel × 122, T11), ambient 1.0 W/K, face the drum's cylinder collider, 64 W/K. `shoe`, on body 0: 21 J/K (0.1 kg of lining × 210), ambient 0.5 W/K, face the housing's box collider, 16 W/K. The foot declares no face.
- **Frictional sources** (T4). (a) The work hinge 0's drive row removes from the shaft in a tick (its torque opposing the motion × the angle turned, summed over all four substeps of the tick, T4 and T8): share 0.5 to `drum`, 0.5 to `shoe`. (b) The work hinge 0's bearing-friction row dissipates, summed the same way: share 1.0 to `drum`. Reading only the last substep would lose three quarters of the heat (audit N2).
- **No new body, collider, joint, port, parameter or store.**
- **Presentation bindings added.** `Visual/HeatMark_0`, `_1`, `_2` albedo navy → gold when the committed temperature of `drum` passes 300 K, 320 K and 340 K; `Visual/ShoeLeft` and `Visual/ShoeRight` emission ← committed heat flow of source (a), 0 to 180 W mapped 0 to 1.
- **Palette.** EL-055's entry (`mechanical_brake`, order 305, [palette allocation](readiness/palette-allocation.md)); no palette order of its own (not placeable: a criterion on `mechanical_brake`; coordinator decision 36) and no second catalogue entry (D8: a closed criterion gets none).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| EL-055's nodes (housing, foot, drum, shoes, pulleys, lamp) | as [EL-055](EL-055-mechanical-brake.md#art-and-solidity) | as EL-055 | as EL-055 |
| `Visual/HeatMark_0` to `_2` navy `#293954` | three short bars of rising length printed flush on the housing face above the window | decorative: printed flat, recoloured by a binding | presentation binding |

The requirement's "small shaped thermal indicator" is the three bars. The shoes' glow is a material property on art that EL-055 already classifies as decorative. The requirement's "gold rotor, cream shoe" differs from EL-055's pale grey drum and slate shoes: one identity has one art, EL-055 is its declaration authority, and the requirement clause is reworded to it (section 6, item 6).

### Compiled counts

Added to EL-055's counts: thermal nodes 2; frictional sources 2; presentation bindings 5. Bodies, colliders and joints unchanged.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| EL-055 rows | The brake itself: supplied drive with target speed 0 holds a shaft; unsupplied it is free | present | `RevoluteDriveDescriptor`, `CuriousContraptions.Abstractions/MechanicalContractValues.cs:138` | `RevoluteDriveTests.ASuppliedDriveWithZeroTargetSpeedHoldsItsShaftAsABrake` (`CuriousContraptions.tests/RevoluteDriveTests.cs:371`; body read) |
| F1 | A committed measure of the work a drive row removes from its shaft | narrower: the drive's committed `Work` only ever rises with work the drive adds; a resisting drive records none (`RevoluteDriveTests.AnOverloadedDriveIsBackDrivenAndAddsNoWork`, `:353`, body read: "committed work never exceeds 0.01 J") | — | ENGINE-THERMAL (T4 frictional source; addition in the Batch N summary) |
| F2 | Work dissipated by a joint's friction row credited to a node | narrower: the friction impulse is committed, no heat is derived ([TH-31 S3](TH-31-flint-striker.md#3-engine-capabilities)) | — | ENGINE-THERMAL |
| TH-04 R9 to R13 | Thermal nodes, contact conduction, ambient, drivers, units | **missing** | none | ENGINE-THERMAL |
| F3 | The supplying Motor's source debited by its committed shaft work | **missing** (a supplied drive is on or off and does not debit its source) | — | ENGINE-SUPPLY-DEMAND (owner decision 3a); needed by row H5 only |

Counts for this criterion: present 1, untested 0, narrower 2, missing 6. ENGINE-THERMAL owns F1, F2 and R9 to R13; ENGINE-SUPPLY-DEMAND owns F3. Nothing here needs code that names a brake.

## 4. Sources and legacy

- **Requirement row** [thermal-05](../requirements.md#thermal-05): extend the existing brake's generic resistive-contact and shaft model: removed mechanical work enters heat stores, with a visible mechanical cost; one brake identity with an added thermal proof. Visual style: "Gold rotor, cream shoe and a small shaped thermal indicator".
- **Named entry** [thermal-05](../invest/named-elements.md#thermal-05): owner S574; "A stationary brake makes no frictional heat; shaft energy loss balances thermal gain and declared losses".
- **EL-055 spec**: the brake is a pass-through shaft whose hinge carries a supplied drive with target speed 0; the earlier "carrier mode" clamp is not built, so this criterion has one mode.
- **Research** per-element table: "contact friction work → heat source"; pad glow ← power.
- **Element map** row TH-05: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SensibleHeat, SlidingFriction, ThermalConduction (D14).
- **Processes and scenarios.** [IX-02](../requirements.md#interaction-02), [IX-05](../requirements.md#interaction-05), [IX-18](../requirements.md#interaction-18); [TX-10](../requirements.md#thermal-scenario-10).
- **Legacy.** No friction-brake part, law or level. The "brake" hits in the legacy tests are motor and pusher supply-hold braking (`CuriousContraptions.tests/MotorPredictionTests.cs@a6c914e:L136-L136`, `CuriousContraptions.tests/LinearPusherTests.cs@a6c914e:L153-L153`): do not carry forward as heat sources; EL-055 harvests them for the mechanical law. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `content/puzzles.json`, `tools/Campaign` and `reference/`.

## 5. Acceptance

Point of truth: [thermal-05](../requirements.md#thermal-05), EL-055's rows and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Law.** While supplied, the drive row opposes rotation with at most `torque_capacity`. When the load exceeds it the shaft slips and the row removes capacity × slip speed watts. That removed work, and the bearing's, is the only heat (T4): half to `drum`, half to `shoe`.
- **Reference case.** A default Motor (20 N·m, 6 rad/s, `elements/Motor/MotorElementDeclaration.cs:41-44`) drives a brake set to 10 N·m: the Motor reaches its 6 rad/s, the brake removes 10 × 6 = 60 W and the bearing 0.02 × 6 = 0.12 W.
- **Drum.** 30 + 0.12 = 30.12 W into 30.5 J/K against 1.0 W/K: final 288 + 30.12 = 318.1 K, time constant 30.5 s. At 10 s: 288 + 30.12 × (1 − e^(−0.328)) = 288 + 30.12 × 0.2796 = 296.4 K.
- **Shoe.** 30 W into 21 J/K against 0.5 W/K: final 348 K, time constant 42 s. At 10 s: 288 + 60 × (1 − e^(−0.238)) = 288 + 60 × 0.212 = 300.7 K.
- **Balance at 10 s.** Shaft work removed 601.2 J. Stored: drum 30.5 × 8.4 = 256 J, shoe 21 × 12.7 = 267 J. To ambient: drum 30.12 × (10 − 30.5 × 0.2796) = 44 J, shoe 30 × (10 − 42 × 0.212) = 33 J. 256 + 267 + 44 + 33 = 600 J.
- **Stationary brake.** At the default 30 N·m the brake stalls the 20 N·m Motor: no slip, so source (a) is 0 × 30 = 0 W and source (b) is 0 W.
- **Released brake.** Unsupplied, only the bearing dissipates: 0.12 W, 1.2 J in 10 s, a drum rise of 0.04 K.
- **Mechanical cost.** The Motor must do 60 W more than against a released brake; once ENGINE-SUPPLY-DEMAND debits it, its Battery falls by 600 J more in 10 s (a default Battery lasts 3600 ÷ 60 = 60 s).
- **Onward conduction.** A ceramic body on the housing pairs at 16 × 16 ÷ 32 = 8 W/K with `shoe`; a metal body touching the drum pairs at 32 W/K.
- **Tick order.** The supply solve commits the supplied state; each of the tick's four substeps adds the row's work to the tick's sum; the thermal step of the same tick, after the last substep and the optical solve, credits the sum (T8).
- **Tolerances** (T12): temperature 2 K; energy 2% (12 J of 601 J); stall guard 30 s.
- **Run length.** Every row fits in the default 30 s (ENGINE-RUN-LENGTH is not needed).

### Rows

Planned class `MechanicalBrakeHeatTests`, added to the brake's package tests.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| H1 | Given a supplied default Motor joined by a Mechanical link (`Drive` → `DriveIn`) to a supplied brake with `torque_capacity` 10, when 10 s pass, then the drum reads 296.4 K and the shoe 300.7 K | `ASlippingBrakeHeatsDrumAndShoe` | H3 and H4 | none | ± 2 K |
| H2 | Given H1, then the heat credited to the two nodes plus their ambient loss equals the work the brake and bearing removed from the shaft (601 J), and does not exceed the Motor's committed work | `ShaftEnergyLossBalancesThermalGainAndDeclaredLosses` | H3: both sides zero | none | 12 J |
| H3 | Given the brake at its default 30 N·m, then the shaft does not turn and neither node gains more than 1 J in 10 s | `AStationaryBrakeMakesNoFrictionalHeat` | H1 | capacity 20 N·m, the Motor's own torque: the shaft creeps or holds, and heat stays below capacity × the angle actually turned | 1 J |
| H4 | Given the brake unsupplied, then the shaft runs at 6 rad/s and the drum rises less than 0.1 K in 10 s | `AReleasedBrakeAddsOnlyBearingHeat` | H1 | none | 0.1 K |
| H5 | Given H1 and H4 with finite Batteries, then the Motor's source falls by 600 J more in H1 than in H4 | `BrakingHasAVisibleMechanicalCost` (after ENGINE-SUPPLY-DEMAND) | H4 | none | 12 J |
| H6 | Given H1 and a Thermal storage block resting on the housing, then the block warms | `BrakeHeatConductsToATouchingBody` (TX-10; a cross-part row owned by the TH-26 slice: the block is taught at 81, after the brake at 39) | the same block 0.5 m away stays at 288 K | none | block rise over 1 K in 30 s |
| H7 | Given H1 then Reset, then both nodes are at 288 K and EL-055's state is restored | `ResetRestoresBrakeTemperatures` | none | none | 0.01 K |

Save and load, the wrong-kind connection refusal and the `torque_capacity` bounds are EL-055's rows and are not repeated. The requirement's outcome: "a stationary brake makes no frictional heat" is H3; "shaft energy loss balances thermal gain and declared losses" is H2.

## 6. Open questions

Resolved:

1. **Game scale.** D5; stated in TH-04.
2. **Art.** EL-055's art stands; this criterion adds three printed bars. The requirement row's "gold rotor, cream shoe" contradicts EL-055's pale grey drum and slate shoes for one identity; item 6 rewords it.
3. **Heat partition.** Half to the drum and half to the shoe, a declared share (T4; decided now: both stores then show the heat, and no row needs a material-dependent split).
4. **Indicator.** Three flush bars, the Heating plate's pattern.
5. **Carrier mode.** None: EL-055 did not build one.

6. **Requirement visual clause** (owner decision 24, 23 November 2026: the four thermal rows with visual wording are reworded to the declared presentation forms). [thermal-05](../requirements.md#thermal-05) asks for "gold rotor, cream shoe"; the brake identity's art is EL-055's (pale grey drum, slate shoes), and one package has one art. The clause is reworded to EL-055's form ("pale grey drum, slate shoes and a small shaped thermal indicator"), D9; the edit to `requirements.md` is the coordinator's ([Batch N summary](readiness/batch-n.md) section 7 item 7).

Needs the owner: none.

## 7. Integration

- **Works with.** Motor, Battery, the Mechanical link (delivered; coordinator decision 36); EL-055 (its package); TH-26, TH-08 (receivers, later).
- **Campaign.** "Brake for Warmth": introduction 39, practice 40; reuse 81, 147. Levels 39 and 40 precede the roadmap position of ENGINE-THERMAL, so they are authored after it. Lesson fixtures need story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** A criterion slice on a delivered package: declaration additions, art marks and tests.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-SUPPLY-DEMAND for H5); EL-055 delivered; ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (frictional sources from joint rows, summed over substeps; owner decision 18). Row H6 runs in the TH-26 slice.
- **Files.** Edits `elements/MechanicalBrake/MechanicalBrakeElementDeclaration.cs` and `mechanical_brake.tscn`; adds `CuriousContraptions.tests/MechanicalBrakeHeatTests.cs`; updates the brake's row in `VisibleSolidColliderTests` for the three printed marks; edits [EL-055's spec](EL-055-mechanical-brake.md) section 2 (its declaration gains the two nodes, the two frictional sources and the five bindings of this section, text in the [Batch N summary](readiness/batch-n.md)); this spec and the ledger entry.
- **Review tier.** Data-only if F1 and F2 are present and tested when the slice starts; otherwise the engine tier.
- **Disqualifiers.** ENGINE-THERMAL delivered with friction heat for contacts only (then the brake's lining would have to become a real contact pair, which is EL-055's redesign, not this slice's); EL-055 delivered with a different drum mass or hinge (re-derive section 5).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** Two nodes, two frictional sources and five bindings added to an existing declaration. No brake routine.
2. **Generic laws.** "Work removed by a joint row is heat, split by a declared share"; Q̇ = G·ΔT.
3. **Three layers.** Colliders: unchanged. Visuals: three printed bars added. Animation: bar colour and shoe glow from committed temperature and heat flow.
4. **Flow and typing.** No new port; f32 values.
5. **Flat state.** Two nodes and two sources added to fixed tables.
6. **Performance.** No collider or joint added; two nodes and two source terms per tick.
7. **Tests are scenarios.** H1 to H7 run the production simulation with a real Motor.

**Verdict: Conforms; blocked on the engine programme (owner decision 3a), EL-055's slice and ENGINE-THERMAL (a general capability).**

## 10. Coverage binding (planned, not yet live)

This identity has no package of its own. It closes when the `mechanical_brake` package, bound through EL-055's spec, carries the rows of section 5 and the ledger entry cites them. **Blocked on the coverage follow-on story S7**, which must also say how a second named identity is recorded against a package already bound to another (main's reader refuses a duplicate package and admits only `EL-NNN` identities).

| Field | Planned value |
| --- | --- |
| Package | none of its own; `mechanical_brake` (EL-055) |
| Named identity | TH-05, `../invest/named-elements.md#thermal-05` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-439` and the ledger entry has no todo link |
| Decision | S574; `scope-corrections.md` on published main has no `s574` block |
| Mode records | none (a criterion, no mode) |
