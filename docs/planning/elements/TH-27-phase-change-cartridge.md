# TH-27 · Phase-change storage cartridge — named-identity readiness spec

**Current delivery (24 October 2026):** [TH-27 delivery slice](../invest/named-elements.md#thermal-27). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Rewritten on 22 November 2026 as one current text against checkout `spec-drafts` (published main `8d1d68aa`). Code citations are `path:line` in that checkout; `path@a6c914e` citations appear only in section 4. Nothing was built or run: every number is a hand calculation shown here and a starting value under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). Units, laws, scale and tolerances are the [thermal conventions](TH-04-electrical-heating-plate.md#thermal-conventions) (T1 to T14); probe rows "TH-04 R<n>" and "TH-08 C<n>" are in [TH-04 section 3](TH-04-electrical-heating-plate.md#3-engine-capabilities) and [TH-08 section 3](TH-08-heat-conducting-bar.md#3-engine-capabilities).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | TH-27 · Phase-change storage cartridge · Thermal |
| Anchors | [requirements.md#thermal-27](../requirements.md#thermal-27) (task `sequence-task-461`); [named-elements.md#thermal-27](../invest/named-elements.md#thermal-27) |
| Owner key / research | S589 · [TH-S02 phase change](../../thermal-component-research.md#th-s02) |
| Related identities | Sensible sibling [TH-26](TH-26-thermal-storage-block.md); cold store [TH-29](TH-29-cold-pack.md). Charged by [TH-04](TH-04-electrical-heating-plate.md); discharges to [TH-37](TH-37-heat-sensitive-target.md) or any receiver. The first phase row with no change to the body; [TH-14 Ice block](TH-14-ice-block.md) adds one. No CAT refines it. |
| Campaign | Intro 84; practice 85; reuse 97, 147 ([thermal allocation](../requirements.md#thermal-campaign-allocation)) |
| Status | Not started. Blocked on the engine programme (owner decision 3a, 22 November 2026), in particular ENGINE-CONTACT-OBSERVATIONS (dynamic-against-dynamic touch links); then ENGINE-THERMAL and ENGINE-RUN-LENGTH. Repaired on 22 November 2026 against the audit `docs/planning/elements/readiness/audits/elements-batch-n.md` (Run length 115 s). |

### Disposition

**To deliver as a single element.** Planned package id `phase_cartridge`, directory `elements/PhaseCartridge/`, display name "Phase-change cartridge". Not a member of the "Sensible stores" family: it declares a phase row and a Choice of starting state, which those members do not.

## 2. Declaration

Follows the delivered Domino shape for a dynamic box part (`elements/Domino/DominoElementDeclaration.cs:9`, `:47`).

- **Interfaces.** `IMechanicalElement`, `ISceneGeometryElement`, and the thermal-node contract of ENGINE-THERMAL.
- **Body.** One dynamic body, 0.6 kg in every phase ("the container retains mass"), origin at its centre. Box inertia for 0.5 × 0.24 × 0.24 m: Ixx = 0.6 × (0.24² + 0.24²) ÷ 12 = 0.0058, Iyy = Izz = 0.6 × (0.5² + 0.24²) ÷ 12 = 0.0154 kg·m².
- **Collider.** `col_shell`: box, half extents (0.25, 0.12, 0.12) m; the thermal face, 64 W/K (metal shell). It does not change with phase.
- **Material (f32).** Friction 0.5, restitution 0.05, rolling resistance 0.
- **Thermal node.** `fill`: sensible heat capacity 70 J/K in both phases (0.1 kg of paraffin-type fill at 625 J/(kg·K), the wax value of T11, plus 7.5 J/K of shell, declared); ambient conductance 0.3 W/K.
- **Phase row** (T5). Melt and freeze at 303 K; latent heat 12 500 J/kg (paraffin, 200 000 × 1/16 under T11's rule) × 0.1 kg = 1250 J. No body change is declared: no mass moves, no collider changes. The committed phase fraction f is 0 when solid and 1 when liquid.
- **Parameters.**

  | Name | Kind | Unit | Default | Options | Editor policy |
  | --- | --- | --- | --- | --- | --- |
  | `initial_state` | `ParameterKind.Choice` | none | 0 | `Discharged` (288 K, f = 0), `Charged` (303 K, f = 1) | player-editable unless the level locks it (D11); build-only |

  The option selects the node's initial enthalpy: 0 J above ambient, or 70 × 15 + 1250 = 2300 J above it.
- **Ports.** None. **Stores.** None beyond the node and its latent amount.
- **Presentation bindings.** `Visual/Window_Fill` scale X ← committed phase fraction of `fill`, 0 to 1 (driver of TH-04 R12). A length, not a colour alone.
- **Palette.** Category `ElementCategory.Structure`. Order: 541 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Art node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Shell` cream `#fff8e9` | box 0.5 × 0.24 × 0.24 m, edges rounded inside the collider | solid, `col_shell` | the body pose |
| `Visual/Window` and `Visual/Window_Fill` cyan `#66b8c9` | a slot printed flush on the long side and a bar inside it | decorative: printed flat; the bar is scaled by a binding | the body pose; length by the binding |
| `Visual/Ends` navy `#293954` | a snowflake mark and a drop mark printed flush at the two ends of the slot | decorative: printed flat | the body pose |

Icon: a capsule with a half-filled slot between two marks. At a glance it differs from the Thermal storage block (four bars) and the Cold pack (tiles) by its single slot with two end symbols.

### Compiled counts

Bodies 1 (dynamic); colliders 1 (box); joints 0; drives 0; sensors 0; thermal nodes 1; faces 1; phase rows 1; ports 0; stores 0; presentation bindings 1.

## 3. Engine capabilities

### Capability probe

| # | Capability | State | Symbol | Exercising test |
| --- | --- | --- | --- | --- |
| TH-04 R1, R6 | Colliders from art; parameter bounds, including a Choice | present | as TH-04 | as TH-04 |
| TH-08 C1 | A dynamic body with a declared mass frame | present | as TH-08 | as TH-08 |
| L1 | A phase row on a node: a threshold temperature, a latent amount, a committed phase fraction, the temperature held while the fraction moves, and no body change required | **missing** | none | none; owner ENGINE-THERMAL |
| L2 | A node's initial temperature and phase fraction selected per option of a Choice parameter | **missing** | none; the selection pattern exists for other capabilities (`ChoicePredicate`, used at `elements/Fan/FanElementDeclaration.cs:14`) | none; owner ENGINE-THERMAL |
| TH-04 R9, R10, R11, R12, R13 | Node, contact conduction, ambient exchange, phase-fraction driver, thermal units | **missing** | as TH-04 | none; owner ENGINE-THERMAL |

Counts: present 3, untested 0, narrower 0, missing 7. All seven have one owner, **ENGINE-THERMAL**: its text names "phase thresholds ... as conserved transfers" and its acceptance already says a body held at its threshold changes phase only after the declared latent amount has been supplied. L2 is proposed text in the [Batch N summary](readiness/batch-n.md). Nothing here needs code that names this part.

## 4. Sources and legacy

- **Requirement row** [thermal-27](../requirements.md#thermal-27): an encapsulated material stores latent enthalpy while transitioning; the container retains mass and exposes a thermal interface; no variants.
- **Named entry** [thermal-27](../invest/named-elements.md#thermal-27): owner S589; "A fully transitioned cartridge cannot repeat a charge/discharge without reversing the energy transfer".
- **Research** per-element table: "node + latent store"; parameter latent heat; "holds temperature during a transition".
- **Element map** row TH-27: EnvironmentState, FiniteLedger, GasState, GeometryQuery, LiquidFreezing, PhaseStorage, PhaseTopology, SensibleHeat, SolidMelting, TopologyTransaction. Navigation only (D14); the sealed cartridge uses no gas state and changes no topology.
- **Processes.** [IX-35](../requirements.md#interaction-35), [IX-21](../requirements.md#interaction-21), [IX-22](../requirements.md#interaction-22), [IX-18](../requirements.md#interaction-18).
- **Legacy.** None found. Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e for phase, latent, paraffin and storage terms.

## 5. Acceptance

Point of truth: [thermal-27](../requirements.md#thermal-27) and [ELEMENT acceptance](../requirements.md#accept-element). Chrome proof is pending the post-catalogue phase.

### Behaviour and numbers

- **Laws.** Contact and ambient exchange (T3); one phase row (T5): at 303 K heat goes into or comes out of the latent amount and the temperature holds until f reaches 0 or 1.
- **Why 303 K and 0.1 kg.** The latent amount (1250 J) must exceed the sensible heat needed to reach the plateau (70 × 15 = 1050 J), or the part teaches nothing a block does not; under T11 paraffin's latent-to-sensible ratio is 12 500 ÷ 625 = 20 K, so the plateau must sit within 20 K of ambient. The whole charge, 2300 J, fits a default Battery.
- **Charging on a Heating plate** (T14; metal on metal, 32 W/K). θ is the rise above 288 K.
  - To the plateau: disc and cartridge together are 15.25 + 70 = 85.25 J/K losing 0.5 + 0.3 = 0.8 W/K, heading for 150 K of rise with time constant 106.6 s; 15 K takes −106.6·ln(1 − 15 ÷ 150) = 11.2 s.
  - On the plateau the cartridge holds 15 K of rise: 120 = 0.5·θ_d + 32·(θ_d − 15) gives θ_d = 600 ÷ 32.5 = 18.5 K; 32 × 3.46 = 110.8 W enter the cartridge, 0.3 × 15 = 4.5 W leave it, so 106.3 W melt the fill: 1250 ÷ 106.3 = 11.8 s.
  - Fully charged about 23 s after Run for about 2.8 kJ. After that the cartridge takes only sensible heat and climbs at about 106 ÷ 70 = 1.5 K/s.
- **Discharging into a target.** A `Charged` cartridge with a TH-37 target lying on it (metal on ceramic, 12.8 W/K; target 10.5 J/K, 0.45 W/K): the target heads for 12.8 × 15 ÷ 13.25 = 14.5 K of rise (302.5 K) and passes 300 K at −(10.5 ÷ 13.25)·ln(1 − 12 ÷ 14.5) = 1.4 s. The cartridge then gives 0.45 × 14.5 = 6.5 W to the target's losses and 4.5 W to its own: 11 W. After the 10.5 × 14.5 = 152 J that warm the target, the latent amount lasts (1250 − 152) ÷ 11 = 100 s at 303 K. Then f is 0, the temperature falls, and nothing more can be given without recharging.
- **Tick order.** T8. **Tolerances.** T12 (times 15%). Stall guard the smaller of three times each hand time and the hand time + 60 s, at least 10 s (T12).
- **Run length these rows need: 120 s** (A4's discharge ends between 85 and 115 s and is then observed falling below 302 K; ENGINE-RUN-LENGTH, owner decisions 13 and 17; default 30 s, maximum 300 s).

### Rows

Planned class `PhaseCartridgeTests`.

| # | Given / When / Then | Planned test | Control | Boundary | Tolerance |
| --- | --- | --- | --- | --- | --- |
| A1 | Given a `Discharged` cartridge on a supplied Heating plate, then it reaches 303 K within 34 s and stays within 1 K of 303 K for at least 8 s while f rises from 0 to 1 without falling | `ACartridgeHoldsItsMeltingPointWhileItCharges` | the plate unwired: 288 K and f = 0 throughout | f at 0 and at 1 | 1 K on the plateau; 8 s against a calculated 11.8 s |
| A2 | Given A1 at the tick f first reads 1, then the Battery's debit equals the disc's stored heat, the cartridge's 1050 J of sensible heat, its 1250 J of latent heat and the ambient account | `LatentHeatStoredEqualsHeatSupplied` | none | none | 2% of the debit |
| A3 | Given A1 continued 5 s past f = 1, then f is still 1 and the cartridge reads above 305 K | `AFullCartridgeTakesOnlySensibleHeat` | A1's plateau | f = 1 | as stated (calculated about 310 K) |
| A4 | Given a `Charged` cartridge with a target lying on it, then the target passes 300 K within 10 s; the cartridge stays within 1 K of 303 K until f reaches 0, which happens between 85 s and 115 s; afterwards f stays 0 and the cartridge falls below 302 K | `AChargedCartridgeGivesItsLatentHeatOnce` | a `Discharged` cartridge: the target stays at 288 K | f = 0 | times 15% of 100 s |
| A5 | Given A1 and A4, then the body's mass and collider are the same at f = 0 and f = 1 | `TheContainerKeepsItsMassAndShape` | none | none | exact |
| A6 | Given A1 then Reset, and A4 then Reset, then each cartridge is back at its authored state (288 K, f = 0; 303 K, f = 1) and pose, and a second Run repeats the row | `ResetRestoresTheAuthoredPhase` | none | both options | as A1, A4 |
| A7 | Given a placed cartridge set to `Charged`, when saved and loaded, then the option and pose are kept and no run state is saved | `SaveAndLoadKeepTheStartingState` | none | none | exact option |
| A8 | An `initial_state` index other than 0 or 1 is refused | generic: TH-04 R6 | 0 and 1 admitted | 0, 1 | typed refusal |

No port, so no wrong-kind connection row. Requirement clauses: latent storage while transitioning (A1, A2); the container retains mass (A5); a thermal interface (A1, A4); a fully transitioned cartridge cannot repeat (A3, A4).

## 6. Open questions

Resolved:

1. **Game scale.** D5, restated once in TH-04 section 11.
2. **Transition temperature.** 303 K, accepted (D1) for the reason in section 5.
3. **GasState in the map row.** Not used (D14).
4. **Charged start.** Allowed, as a Choice (D11): a lesson may start either way, and each option is proved (A1, A4).
5. **Fill mass.** 0.1 kg thermal fill inside a 0.6 kg body, decided now so that a default Battery completes one charge; the earlier 0.5 kg fill needed 11.3 kJ.

Needs the owner: none.

## 7. Integration

- **Works with.** TH-04 and TH-37 (planned before it), TH-21, TH-26 and TH-29 (contrast lessons), Conveyor (delivered) to carry it.
- **Campaign.** Introduction 84, practice 85; reuse 97, 147. Loose cargo; fixed partners need the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22).
- **Coverage.** Section 10.

## 8. Delivery

- **Slice shape.** Single element; the first consumer of a phase row.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which comes first: the programme (ENGINE-CONTACT-OBSERVATIONS for dynamic-against-dynamic touch links); ENGINE-GAS-NETWORK and ENGINE-LIQUID, then ENGINE-THERMAL as amended (owner decision 18); ENGINE-RUN-LENGTH; TH-04; TH-37; S7; S3 preferred.
- **Files.** `elements/PhaseCartridge/PhaseCartridge.csproj`, `PhaseCartridgeElementDeclaration.cs`, `phase_cartridge.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx` (`spec-s3-remaining-per-package-lists.md`, decision O1); `CuriousContraptions.tests/PhaseCartridgeTests.cs`; a row in `VisibleSolidColliderTests`; this spec's binding section once S7 allows it. Registration is derived.
- **Review tier.** Data-only if the probe finds every row present and tested; otherwise the engine tier.
- **Disqualifiers.** A phase row that requires a declared body change or a liquid store; no per-option initial state (the part then ships `Discharged` only and a lesson charges it in the Run); a different scale (re-derive section 5).

## 9. Engine conformance (22 November 2026)

1. **Declaration data only.** One dynamic body, one box, one node with one face and one phase row, one Choice parameter, one binding. No state machine in code: the phase fraction is the engine's general phase state.
2. **Generic laws.** Q̇ = G·ΔT; the enthalpy curve with one latent step. "Holds its temperature while it melts" is that step.
3. **Three layers.** Collider `col_shell`. Visuals: the shell art with its collider; printed slot and marks. Animation: the slot's bar from the committed phase fraction; nothing is written back.
4. **Flow and typing.** No ports; the starting state is a Choice (an enum end to end), f32 elsewhere.
5. **Flat state.** The counts in section 2.
6. **Performance.** One primitive collider; thermal work is one node, one phase row and one term per contact (T10). It rests in use. Island sleeping is owned by ENGINE-ISLANDS-SLEEP (owner decision 3a); a resting cartridge is stepped every tick until then, and its node keeps exchanging while asleep (T3).
7. **Tests are scenarios.** A1 to A7 run the production simulation.

**Verdict: Conforms; blocked on ENGINE-THERMAL, a general capability stated element-free, and on the engine programme (owner decision 3a).**

## 10. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball). That section is not written here. **Blocked on the coverage follow-on story S7:** main's reader lists only `EL-*.md` files and admits only an `EL-NNN` identity with an `element-NNN` anchor, so no thermal identity can be bound until S7 widens it.

| Field | Planned value |
| --- | --- |
| Package | `phase_cartridge` |
| Named identity | TH-27, `../invest/named-elements.md#thermal-27` |
| Acceptance | none resolvable today; the row's own task is `sequence-task-461` and the ledger entry has no todo link |
| Decision | S589; `scope-corrections.md` on published main has no `s589` block |
| Mode records | `InitialState=Discharged`, `InitialState=Charged` |
