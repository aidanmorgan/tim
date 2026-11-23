# EL-141 · Optical NOR gate — named-identity spec

**Current delivery (24 October 2026):** [EL-141 delivery slice](../invest/named-elements.md#element-141). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`. Lengths are metres; local +X is the outlet direction. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions). [EL-138](EL-138-optical-and-gate.md) is the gate family lead for the probe, sources and conformance; this identity's declaration is CAT-045's (coordinator decision 27).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-141 |
| Name | Optical NOR gate |
| Type | Optical |
| Anchor | [requirements.md#element-141](../requirements.md#element-141); named entry [named-elements.md#element-141](../invest/named-elements.md#element-141); source record [todo-367](../requirements.md#todo-367) |
| Owner | S513 |
| Disposition | Candidate **Duplicate closure** of [CAT-045 optical_nor](CAT-045-optical_nor.md) (`optical_nor`, "Neither light gate"), closed inside that part's slice. No delta from CAT-045. |
| Acceptance authority | The CAT-045 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "prove every input combination plus absent carrier; logically true cannot create power", is CAT-045 rows **G1 and G2**; the ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-045's `optical_nor` (`elements/OpticalNor/`) |
| Roadmap story | Story 13.8 |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES) and ENGINE-SIGNAL-NODES |

## 2. Declaration

The `optical_nor` declaration, art and bindings are [CAT-045's declaration](CAT-045-optical_nor.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026).

## 3. Engine capabilities

The probe is [EL-138 section 3](EL-138-optical-and-gate.md#3-engine-capabilities) row for row, with the same citations: present R1, R2, R12; narrower R9 (owner ENGINE-SPATIAL-QUERIES) and R11 (ENGINE-OPTICAL); missing MS and MB (ENGINE-SIGNAL-NODES) and M2 to M6 (ENGINE-OPTICAL). Counts 3 / 0 / 2 / 7. Nor is not monotone (`CuriousContraptions.Abstractions/ElectricalContractValues.cs:23`–`:25`); an optical loop through this gate advances one step per tick, because a reading acts only from the next tick.

## 4. Sources and legacy

**Sources.** Row [element-141](../requirements.md#element-141): carrier output follows neither input present; prove every input combination plus absent carrier. [todo-367](../requirements.md#todo-367). CAT row [current-cat-045](../requirements.md#current-cat-045). Refinement **optical-nor** ([refinements](../invest/refinements.md)): those controls, and all ten logic gates retain feedback bounds and Reset. Campaign: optical logic first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy.** The complete gate harvest, including Boolean observations, lamp-source binding and parameter-ownership tests, is in [CAT-045-optical_nor](CAT-045-optical_nor.md#legacy-harvest) (shared gate facts from [CAT-043-optical_and](CAT-043-optical_and.md#legacy-harvest)); the shared gate body and control law are restated in full in [EL-138](EL-138-optical-and-gate.md#4-sources-and-legacy) items 1–16 and apply unchanged; shared optical facts S1–S8 are in [CAT-036-laser](CAT-036-laser.md#legacy-harvest). NOR-specific facts:
1. Apertures, outlet, retention, Route-when-open — `parts/OpticalLogicPart.cs@a6c914e:L13-L14`, `parts/OpticalLogicPart.cs@a6c914e:L51-L58`. Carry forward.
2. NOR truth `!first && !second` — `engine/LogicGate.cs@a6c914e:L16-L16`; reset sets open = evaluate(false, false), which is true for NOR — `engine/LogicGate.cs@a6c914e:L73-L77`; hysteresis and validation — `engine/LogicGate.cs@a6c914e:L44-L71`. Carry forward.
3. Catalogue entry and scene (Operation = 3) — `parts/catalog/optical_nor.tres@a6c914e:L6-L11`, `parts/scenes/optical_nor.tscn@a6c914e:L1-L5`. Carry forward the entry.
4. Acceptance: NOR rows 00→open, others closed, with and without carrier; reconfiguration restores the open initial decision — `CuriousContraptions.tests/LogicGateTests.cs@a6c914e:L13-L13`, `CuriousContraptions.tests/OpticalLogicTests.cs@a6c914e:L28-L69`. Carry forward.
5. Acceptance: failed-tick rollback and atomic output observation — `CuriousContraptions.tests/OpticalControlCheckpointTests.cs@a6c914e:L45-L84`, `CuriousContraptions.tests/OpticalObservationTests.cs@a6c914e:L66-L117`; typed Boolean control observations for every truth row — `CuriousContraptions.tests/BooleanObservationTests.cs@a6c914e:L60-L111`. Carry forward (rollback at the later fault gate).
6. Per-part advance hook — `parts/OpticalLogicPart.cs@a6c914e:L50-L50`. Do not carry forward the hook; keep next-tick ordering (section 6).

**Files harvested:** `parts/OpticalLogicPart.cs`, `parts/catalog/optical_nor.tres`, `parts/scenes/optical_nor.tscn`, `engine/LogicGate.cs`, `engine/SceneBooleanObservation.cs`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `reference/cpu/MachineWorld.cs`, `CuriousContraptions.tests/OpticalLogicTests.cs`, `CuriousContraptions.tests/LogicGateTests.cs`, `CuriousContraptions.tests/OpticalControlCheckpointTests.cs`, `CuriousContraptions.tests/OpticalObservationTests.cs`, `CuriousContraptions.tests/BooleanObservationTests.cs`, `CuriousContraptions.tests/ParameterValidationOwnershipTests.cs`, `CuriousContraptions.tests/BooleanColourBindingTests.cs`. No legacy level uses it.

## 5. Acceptance

**Authority.** Acceptance is [CAT-045's rows](CAT-045-optical_nor.md#acceptance-outline) G1 to G13, with their fixtures, tolerances and stall guards. This identity's outcomes map to **G1** and **G2**; the ledger row cites those tests. No row is stated or inherited here: the AND gate's expected values do not hold for Nor (it is open with both controls dark and closed with both lit), and CAT-045 states its own.

**Behaviour and numbers.** A control is on at a mean of 2.5 W (an amber laser gives 7.0 W) and releases below 2.25 W. An amber carrier leaves as (9.0, 7.02, 2.88) W while neither control is on. Three amber lasers demand 75 W of the Battery's 120 W. After Reset both controls are off, so Nor is **open**: a supplied carrier passes from the start of a Run until a control is lit, and the tick after that it is absorbed.

## 6. Open questions

None open. Story wording: the requirement row governs (D15, K-8). Duplicate closure with CAT-045's rows as the single authority (coordinator decision 16). Mean control strength kept (K-7). Tick order and coupling: coordinator decision 16.

Needs the owner: none.

## 7. Integration

As [EL-138 section 7](EL-138-optical-and-gate.md#7-integration). Coverage binding (planned, not yet live): catalogue key `optical_nor` (CAT-045). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).

## 8. Delivery

Closed inside CAT-045's slice, or inside one gate Family slice if proposal K-17 is accepted. Prerequisites, in order: the engine programme (owner decision 3a) including ENGINE-SPATIAL-QUERIES; ENGINE-SIGNAL-NODES; ENGINE-OPTICAL as corrected in the batch summary; a laser and a detector. Files (CAT slice): `elements/OpticalNor/`, one guarded solution line, rows in `OpticalGateTests` and `VisibleSolidColliderTests`, the ledger row. Review tier: data-only if the probe passes after the engine stories. Disqualifiers: as EL-138.

## 9. Engine conformance

Verdict: **Conforms**, on the seven points as [EL-138 section 9](EL-138-optical-and-gate.md#9-engine-conformance): two receivers, two threshold nodes, one boolean node (Nor) and one relay; no engine code names it and no logic runs inside the optical solve; colliders (two boxes), flush art and colour bindings are separate layers; counts are fixed at compile time; its tests are CAT-045's scenarios.
