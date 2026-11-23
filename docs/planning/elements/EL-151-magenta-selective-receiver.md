# EL-151 · Magenta selective receiver — named-identity spec

**Current delivery (24 October 2026):** [EL-151 delivery slice](../invest/named-elements.md#element-151). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`. Lengths are metres. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions). [EL-146](EL-146-red-selective-receiver.md) is the receiver family lead for the probe, sources and conformance; this identity's declaration is CAT-040's (coordinator decision 27).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-151 |
| Name | Magenta selective receiver |
| Type | Optical |
| Anchor | [requirements.md#element-151](../requirements.md#element-151); named entry [named-elements.md#element-151](../invest/named-elements.md#element-151); source record [todo-301](../requirements.md#todo-301) |
| Owner | S506 |
| Disposition | Candidate **Duplicate closure** of [CAT-040 magenta_receiver](CAT-040-magenta_receiver.md) (`magenta_receiver`, "Magenta receiver"), closed inside the Colour receivers Family slice. No delta from CAT-040. |
| Acceptance authority | The CAT-040 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "missing required channel, wrong channel and absent supply remain independent negative controls", is the red-only and blue-only cases of CAT-040 row **R2** (missing required channel), the green-only case of **R2** and row **R3** (wrong channel) and the **control of R1** (absent supply); the ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-040's `magenta_receiver` (`elements/MagentaReceiver/`) |
| Roadmap story | Story 13.6 |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES) and ENGINE-SIGNAL-NODES |

## 2. Declaration

The `magenta_receiver` declaration, art and bindings are [CAT-040's declaration](CAT-040-magenta_receiver.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026).

## 3. Engine capabilities

The probe is [EL-146 section 3](EL-146-red-selective-receiver.md#3-engine-capabilities) row for row, with the same citations: present R1, R2, R3, R4, R12, R13; present-untested R11; narrower R9 (owner ENGINE-SPATIAL-QUERIES) and R10 (ENGINE-OPTICAL); missing MS (ENGINE-SIGNAL-NODES) and M2, M4, M5, M6 (ENGINE-OPTICAL). Counts 6 / 1 / 2 / 5.

## 4. Sources and legacy

**Sources.** Row [element-151](../requirements.md#element-151): output requires red and blue under the declared thresholds and purity contract; three independent negative controls. [todo-301](../requirements.md#todo-301). CAT row [current-cat-040](../requirements.md#current-cat-040). Refinement **optical-pairings** (S694). Campaign first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); receiver body and shared tests in [EL-146](EL-146-red-selective-receiver.md#4-sources-and-legacy); mixed rule in [EL-149](EL-149-yellow-selective-receiver.md#4-sources-and-legacy)):
1. Receiver body, aperture, ports, route, threshold validation and indicator — `parts/LightReceiverPart.cs@a6c914e:L11-L59`. Carry forward behaviour; not the per-part callback or Godot materials.
2. Magenta mask (1, 0, 1); minimum-of-required strength; purity over the set — `engine/OpticalColour.cs@a6c914e:L10-L22`, `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Mark rows and pink beam ink — `engine/OpticalColour.cs@a6c914e:L49-L76`. Carry forward as presentation.
4. Catalogue entry and scene (Colour = 6) — `parts/catalog/magenta_receiver.tres@a6c914e:L6-L12`, `parts/scenes/magenta_receiver.tscn@a6c914e:L1-L5`. Carry forward the entry.
5. Acceptance: (0.3, 0, 0.3) accepted; R or B at 0 or 0.249 rejected; (1, 1, 1) rejected — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L118-L137`. Carry forward.
6. Acceptance: contact binding, indicator and rollback — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`, `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/magenta_receiver.tres`, `parts/scenes/magenta_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-040's rows](CAT-040-magenta_receiver.md#acceptance-outline) R1 to R12, with their fixtures, tolerances, thresholds and stall guards. This identity's outcomes map to **R2** (its red-only, blue-only and green-only cases), **R3** and the **control of R1**; the ledger row cites those tests. No row is stated or inherited here.

**Behaviour and numbers.** Strength is min(red, blue) when red + blue is at least 90 % of the total. A red and a blue laser through the combiner give (9, 0, 9) W: strength 9 W, on. Red alone gives strength 0: off. An unfiltered amber laser is (10 + 3.2) ÷ 21 = 62.9 % in the set: off. Two filtered amber lasers through the combiner give (9, 0, 2.88) W: strength 2.88 W, on at the default with a 15 % margin.

## 6. Open questions

None open: thresholds (K-6), hysteresis (K-4), coupling and the closure with CAT-040's rows as the single authority (coordinator decision 16) are decided as in [EL-146 section 6](EL-146-red-selective-receiver.md#6-open-questions).

Needs the owner: none.

## 7. Integration

As [EL-146 section 7](EL-146-red-selective-receiver.md#7-integration); it needs the combiner [EL-213](EL-213-optical-combiner.md) fed by [EL-176](EL-176-red-laser-emitter.md) and [EL-178](EL-178-blue-laser-emitter.md). Coverage binding (planned, not yet live): catalogue key `magenta_receiver` (CAT-040). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).

## 8. Delivery

Closed inside the Colour receivers Family slice. Prerequisites, in order: the engine programme (owner decision 3a) including ENGINE-SPATIAL-QUERIES; ENGINE-SIGNAL-NODES; ENGINE-OPTICAL as corrected in the batch summary; the combiner and two single-channel sources. Files (CAT slice): `elements/MagentaReceiver/`, one guarded solution line, rows in `SelectiveReceiverTests` and `VisibleSolidColliderTests`, the ledger row. Review tier: data-only if the probe passes. Disqualifiers: as EL-146.

## 9. Engine conformance

Verdict: **Conforms**, on the seven points as [EL-146 section 9](EL-146-red-selective-receiver.md#9-engine-conformance): the receiver family's declaration with the required set {Red, Blue}; no engine code names it; it couples through a signal node only; colliders, flush art and the disc binding are separate layers; counts are fixed; its tests are CAT-040's scenarios.
