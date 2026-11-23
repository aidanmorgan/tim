# EL-147 · Green selective receiver — named-identity spec

**Current delivery (24 October 2026):** [EL-147 delivery slice](../invest/named-elements.md#element-147). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`. Lengths are metres. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions). [EL-146](EL-146-red-selective-receiver.md) is the receiver family lead for the probe, sources and conformance; this identity's declaration is CAT-032's (coordinator decision 27).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-147 |
| Name | Green selective receiver |
| Type | Optical |
| Anchor | [requirements.md#element-147](../requirements.md#element-147); named entry [named-elements.md#element-147](../invest/named-elements.md#element-147); source record [todo-300](../requirements.md#todo-300) |
| Owner | S502 |
| Disposition | Candidate **Duplicate closure** of [CAT-032 green_receiver](CAT-032-green_receiver.md) (`green_receiver`, "Green receiver"), closed inside the Colour receivers Family slice. No delta from CAT-032. |
| Acceptance authority | The CAT-032 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "missing required channel, wrong channel and absent supply remain independent negative controls", is CAT-032 row **R2** (no green in the beam), rows **R3 and R5** (green present, other channels too strong) and the **control of R1** (absent supply); the ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-032's `green_receiver` (`elements/GreenReceiver/`) |
| Roadmap story | Story 13.2 |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES) and ENGINE-SIGNAL-NODES |

## 2. Declaration

The `green_receiver` declaration, art and bindings are [CAT-032's declaration](CAT-032-green_receiver.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026).

## 3. Engine capabilities

The probe is [EL-146 section 3](EL-146-red-selective-receiver.md#3-engine-capabilities) row for row, with the same citations: present R1, R2, R3, R4, R12, R13; present-untested R11; narrower R9 (owner ENGINE-SPATIAL-QUERIES) and R10 (ENGINE-OPTICAL); missing MS (ENGINE-SIGNAL-NODES) and M2, M4, M5, M6 (ENGINE-OPTICAL). Counts 6 / 1 / 2 / 5.

## 4. Sources and legacy

**Sources.** Row [element-147](../requirements.md#element-147); [todo-300](../requirements.md#todo-300); CAT row [current-cat-032](../requirements.md#current-cat-032); refinement **optical-pairings** (S694); campaign first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); receiver body, contact, indicator and tests harvested in full in [EL-146](EL-146-red-selective-receiver.md#4-sources-and-legacy) and apply unchanged):
1. Receiver body, aperture, ports, route, threshold validation and indicator — `parts/LightReceiverPart.cs@a6c914e:L11-L59`. Carry forward behaviour; not the per-part callback or Godot materials.
2. Green mask (0, 1, 0), strength = G, purity 0.9 — `engine/OpticalColour.cs@a6c914e:L10-L15`, `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Catalogue entry and scene (Colour = 2) — `parts/catalog/green_receiver.tres@a6c914e:L6-L12`, `parts/scenes/green_receiver.tscn@a6c914e:L1-L5`. Carry forward the entry.
4. Acceptance: purity and threshold boundaries for green (0.249 off, 0.25 on, 0.9 + 0.051 off, 0.91 + 0.045 on), separate supply and Reset — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L128-L168`; contact binding — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`; indicator — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`; rollback — `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/green_receiver.tres`, `parts/scenes/green_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-032's rows](CAT-032-green_receiver.md#acceptance-outline) R1 to R12, with their fixtures, tolerances and stall guards. This identity's outcomes map to **R2**, **R3 and R5**, and the **control of R1**; the ledger row cites those tests. No row is stated or inherited here; in particular the purity cases use a contaminant that is not green, as CAT-032 states them.

**Behaviour and numbers.** Strength is the green reading when green is at least 90 % of the total. A green laser gives (0, 10, 0) W: on. The amber laser through a green filter gives (0, 7.8, 0) W: on, 3.1 times the default 2.5 W. An unfiltered amber beam is 7.8 ÷ 21 = 37.1 % green: off. A red laser gives no green: off. The contact closes one tick after the strength commits.

## 6. Open questions

None open: thresholds (K-6), hysteresis (K-4), absorbed heat (K-10), coupling and the closure with CAT-032's rows as the single authority (coordinator decision 16) are decided as in [EL-146 section 6](EL-146-red-selective-receiver.md#6-open-questions).

Needs the owner: none.

## 7. Integration

As [EL-146 section 7](EL-146-red-selective-receiver.md#7-integration); channel partners are the green laser [EL-177](EL-177-green-laser-emitter.md) and the green filter [EL-144](EL-144-green-optical-filter.md). Coverage binding (planned, not yet live): catalogue key `green_receiver` (CAT-032). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).

## 8. Delivery

Closed inside the Colour receivers Family slice. Prerequisites, in order: the engine programme (owner decision 3a) including ENGINE-SPATIAL-QUERIES; ENGINE-SIGNAL-NODES; ENGINE-OPTICAL as corrected in the batch summary; a laser. Files (CAT slice): `elements/GreenReceiver/`, one guarded solution line, rows in `SelectiveReceiverTests` and `VisibleSolidColliderTests`, the ledger row. Review tier: data-only if the probe passes. Disqualifiers: as EL-146.

## 9. Engine conformance

Verdict: **Conforms**, on the seven points as [EL-146 section 9](EL-146-red-selective-receiver.md#9-engine-conformance): the receiver family's declaration with the required set {Green}; no engine code names it; it couples through a signal node only; colliders, flush art and the disc binding are separate layers; counts are fixed; its tests are CAT-032's scenarios.
