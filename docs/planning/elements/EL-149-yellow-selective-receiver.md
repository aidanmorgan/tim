# EL-149 · Yellow selective receiver — named-identity spec

**Current delivery (24 October 2026):** [EL-149 delivery slice](../invest/named-elements.md#element-149). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`. Lengths are metres. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions). [EL-146](EL-146-red-selective-receiver.md) is the receiver family lead for the probe, sources and conformance; this identity's declaration is CAT-072's (coordinator decision 27). It is the first of the four mixed-channel receivers.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-149 |
| Name | Yellow selective receiver |
| Type | Optical |
| Anchor | [requirements.md#element-149](../requirements.md#element-149); named entry [named-elements.md#element-149](../invest/named-elements.md#element-149); source record [todo-301](../requirements.md#todo-301) |
| Owner | S504 |
| Disposition | Candidate **Duplicate closure** of [CAT-072 yellow_receiver](CAT-072-yellow_receiver.md) (`yellow_receiver`, "Yellow receiver"), closed inside the Colour receivers Family slice. No delta from CAT-072. |
| Acceptance authority | The CAT-072 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "missing required channel, wrong channel and absent supply remain independent negative controls", is the red-only and green-only cases of CAT-072 row **R2** (missing required channel), the blue-only case of **R2** and row **R3** (wrong channel) and the **control of R1** (absent supply); the ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-072's `yellow_receiver` (`elements/YellowReceiver/`) |
| Roadmap story | [Story 13.6](../../../_bmad-output/planning-artifacts/epics.md#story-136-secondary-spectral-receivers-cyan-magenta-yellow-white-cat-021-cat-040-cat-072-cat-068) |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES) and ENGINE-SIGNAL-NODES |

## 2. Declaration

The `yellow_receiver` declaration, art and bindings are [CAT-072's declaration](CAT-072-yellow_receiver.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026).

## 3. Engine capabilities

The probe is [EL-146 section 3](EL-146-red-selective-receiver.md#3-engine-capabilities) row for row, with the same citations: present R1, R2, R3, R4, R12, R13; present-untested R11; narrower R9 (owner ENGINE-SPATIAL-QUERIES) and R10 (ENGINE-OPTICAL); missing MS (ENGINE-SIGNAL-NODES) and M2, M4, M5, M6 (ENGINE-OPTICAL). Counts 6 / 1 / 2 / 5. The mixed rule is the same Minimum rule over a two-channel required set; it needs no capability of its own.

## 4. Sources and legacy

**Sources.** Row [element-149](../requirements.md#element-149): output requires red and green under the declared thresholds and channel-purity contract; missing required channel, wrong channel and absent supply are independent negative controls. [todo-301](../requirements.md#todo-301): every named channel at threshold and at least 90% of total power in that set. CAT row [current-cat-072](../requirements.md#current-cat-072): blue-only or either missing channel fails. Refinement **optical-pairings** (S694). Campaign: combiner/mixed receivers first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); receiver body, contact, indicator and shared tests in [EL-146](EL-146-red-selective-receiver.md#4-sources-and-legacy), unchanged here):
1. Receiver body, aperture, ports, route, threshold validation and indicator — `parts/LightReceiverPart.cs@a6c914e:L11-L59`. Carry forward behaviour; not the per-part callback or Godot materials.
2. Yellow mask (1, 1, 0); strength = minimum over masked channels; accept iff strength ≥ threshold and power·mask ≥ 0.9 × total — `engine/OpticalColour.cs@a6c914e:L10-L22`, `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Grouped mark rows (one row per required channel, bar count = channel index) — `engine/OpticalColour.cs@a6c914e:L61-L76`; yellow beam ink gold — `engine/OpticalColour.cs@a6c914e:L49-L55`. Carry forward as presentation.
4. Catalogue entry and scene (Colour = 4) — `parts/catalog/yellow_receiver.tres@a6c914e:L6-L12`, `parts/scenes/yellow_receiver.tscn@a6c914e:L1-L5`. Carry forward the entry.
5. Acceptance: (0.3, 0.3, 0) accepted at threshold 0.25; dropping R or G to 0, or to 0.249, rejects; (1, 1, 1) rejects (purity) — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L118-L137`. Carry forward.
6. Acceptance: contact binding, indicator and rollback per colour — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`, `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward.
7. Presentation: co-directed red and green paths from one origin sum to yellow ink over their overlap only; crossing beams stay separate — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L161-L177`. Carry forward.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/yellow_receiver.tres`, `parts/scenes/yellow_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-072's rows](CAT-072-yellow_receiver.md#acceptance-outline) R1 to R12, with their fixtures, tolerances, thresholds and stall guards. This identity's outcomes map to **R2** (its red-only, green-only and blue-only cases), **R3** and the **control of R1**; the ledger row cites those tests. No row is stated or inherited here; where an earlier draft of this spec gave a threshold case that differed from CAT-072's, CAT-072's stands.

**Behaviour and numbers.** Strength is min(red, green) when red + green is at least 90 % of the total. A red and a green laser through the combiner give (9, 9, 0) W: strength 9 W, on. Red alone gives strength 0: off. An unfiltered amber laser gives (10, 7.8, 3.2) W: 17.8 ÷ 21 = 84.8 % in the set, off, the intended lesson that yellow must be mixed (D18). A white Flashlight at 3 m gives 14.8 W per channel: 66.7 %, off. Two colour lasers demand 24 W of the Battery's 120 W.

## 6. Open questions

None open. One `threshold` applies to each required channel (K-6). Release ratio 0.9 (K-4). The amber laser failing yellow purity is intended (D18). Duplicate closure with CAT-072's rows as the single authority, and coupling through a signal node (coordinator decision 16).

Needs the owner: none.

## 7. Integration

As [EL-146 section 7](EL-146-red-selective-receiver.md#7-integration); it needs the combiner [EL-213](EL-213-optical-combiner.md) fed by [EL-176](EL-176-red-laser-emitter.md) and [EL-177](EL-177-green-laser-emitter.md), or by two filtered amber lasers ((9, 0, 0) + (0, 7.02, 0) W: strength 7.02 W, on). Coverage binding (planned, not yet live): catalogue key `yellow_receiver` (CAT-072). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).

## 8. Delivery

Closed inside the Colour receivers Family slice. Prerequisites, in order: the engine programme (owner decision 3a) including ENGINE-SPATIAL-QUERIES; ENGINE-SIGNAL-NODES; ENGINE-OPTICAL as corrected in the batch summary; the combiner (Story 13.5) and two single-channel sources. Files (CAT slice): `elements/YellowReceiver/`, one guarded solution line, rows in `SelectiveReceiverTests` and `VisibleSolidColliderTests`, the ledger row. Review tier: data-only if the probe passes. Disqualifiers: as EL-146.

## 9. Engine conformance

Verdict: **Conforms**, on the seven points as [EL-146 section 9](EL-146-red-selective-receiver.md#9-engine-conformance): the receiver family's declaration with the required set {Red, Green}; no engine code names it; it couples through a signal node only; colliders, flush art and the disc binding are separate layers; counts are fixed; its tests are CAT-072's scenarios.
