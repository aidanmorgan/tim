# EL-148 · Blue selective receiver — named-identity spec

**Current delivery (24 October 2026):** [EL-148 delivery slice](../invest/named-elements.md#element-148). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`. Lengths are metres. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions). [EL-146](EL-146-red-selective-receiver.md) is the receiver family lead for the probe, sources and conformance; this identity's declaration is CAT-012's (coordinator decision 27).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-148 |
| Name | Blue selective receiver |
| Type | Optical |
| Anchor | [requirements.md#element-148](../requirements.md#element-148); named entry [named-elements.md#element-148](../invest/named-elements.md#element-148); source record [todo-300](../requirements.md#todo-300) |
| Owner | S503 |
| Disposition | Candidate **Duplicate closure** of [CAT-012 blue_receiver](CAT-012-blue_receiver.md) (`blue_receiver`, "Blue receiver"), closed inside the Colour receivers Family slice. No delta from CAT-012. |
| Acceptance authority | The CAT-012 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "missing required channel, wrong channel and absent supply remain independent negative controls", is CAT-012 row **R2** (no blue in the beam), rows **R3 and R5** (blue present, other channels too strong) and the **control of R1** (absent supply); the ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-012's `blue_receiver` (`elements/BlueReceiver/`) |
| Roadmap story | Story 13.2 |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES) and ENGINE-SIGNAL-NODES |

## 2. Declaration

The `blue_receiver` declaration, art and bindings are [CAT-012's declaration](CAT-012-blue_receiver.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026).

## 3. Engine capabilities

The probe is [EL-146 section 3](EL-146-red-selective-receiver.md#3-engine-capabilities) row for row, with the same citations: present R1, R2, R3, R4, R12, R13; present-untested R11; narrower R9 (owner ENGINE-SPATIAL-QUERIES) and R10 (ENGINE-OPTICAL); missing MS (ENGINE-SIGNAL-NODES) and M2, M4, M5, M6 (ENGINE-OPTICAL). Counts 6 / 1 / 2 / 5.

## 4. Sources and legacy

**Sources.** Row [element-148](../requirements.md#element-148); [todo-300](../requirements.md#todo-300); CAT row [current-cat-012](../requirements.md#current-cat-012); refinement **optical-pairings** (S694); campaign first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); receiver body, contact, indicator and tests harvested in full in [EL-146](EL-146-red-selective-receiver.md#4-sources-and-legacy) and apply unchanged):
1. Receiver body, aperture, ports, route, threshold validation and indicator — `parts/LightReceiverPart.cs@a6c914e:L11-L59`. Carry forward behaviour; not the per-part callback or Godot materials.
2. Blue mask (0, 0, 1), strength = B, purity 0.9 — `engine/OpticalColour.cs@a6c914e:L10-L16`, `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Catalogue entry and scene (Colour = 3) — `parts/catalog/blue_receiver.tres@a6c914e:L6-L12`, `parts/scenes/blue_receiver.tscn@a6c914e:L1-L5`. Carry forward the entry.
4. Acceptance: purity and threshold boundaries for blue (0.249 off, 0.25 on, 0.9 + 0.051 off, 0.91 + 0.045 on), separate supply and Reset — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L128-L168`; contact binding — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`; indicator — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`; rollback — `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs@a6c914e:L30-L93`. Carry forward.

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/blue_receiver.tres`, `parts/scenes/blue_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-012's rows](CAT-012-blue_receiver.md#acceptance-outline) R1 to R12, with their fixtures, tolerances and stall guards. This identity's outcomes map to **R2**, **R3 and R5**, and the **control of R1**; the ledger row cites those tests. No row is stated or inherited here.

**Behaviour and numbers.** Strength is the blue reading when blue is at least 90 % of the total. A blue laser gives (0, 0, 10) W: on. The amber laser through a blue filter gives (0, 0, 3.2) W: on at the default 2.5 W with a 28 % margin, and off once a 50/50 splitter halves it to 1.6 W, which is the lesson that the amber beam is weak in blue. An unfiltered amber beam is 3.2 ÷ 21 = 15.2 % blue: off. The contact closes one tick after the strength commits.

## 6. Open questions

None open: thresholds (K-6), hysteresis (K-4), absorbed heat (K-10), coupling and the closure with CAT-012's rows as the single authority (coordinator decision 16) are decided as in [EL-146 section 6](EL-146-red-selective-receiver.md#6-open-questions).

Needs the owner: none.

## 7. Integration

As [EL-146 section 7](EL-146-red-selective-receiver.md#7-integration); channel partners are the blue laser [EL-178](EL-178-blue-laser-emitter.md) and the blue filter [EL-145](EL-145-blue-optical-filter.md). Coverage binding (planned, not yet live): catalogue key `blue_receiver` (CAT-012). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).

## 8. Delivery

Closed inside the Colour receivers Family slice. Prerequisites, in order: the engine programme (owner decision 3a) including ENGINE-SPATIAL-QUERIES; ENGINE-SIGNAL-NODES; ENGINE-OPTICAL as corrected in the batch summary; a laser. Files (CAT slice): `elements/BlueReceiver/`, one guarded solution line, rows in `SelectiveReceiverTests` and `VisibleSolidColliderTests`, the ledger row. Review tier: data-only if the probe passes. Disqualifiers: as EL-146.

## 9. Engine conformance

Verdict: **Conforms**, on the seven points as [EL-146 section 9](EL-146-red-selective-receiver.md#9-engine-conformance): the receiver family's declaration with the required set {Blue}; no engine code names it; it couples through a signal node only; colliders, flush art and the disc binding are separate layers; counts are fixed; its tests are CAT-012's scenarios.
