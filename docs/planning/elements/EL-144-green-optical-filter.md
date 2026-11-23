# EL-144 · Green optical filter — named-identity spec

**Current delivery (24 October 2026):** [EL-144 delivery slice](../invest/named-elements.md#element-144). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`. Lengths are metres. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions). [EL-143](EL-143-red-optical-filter.md) is the filter family lead for the probe, sources and conformance; this identity's declaration is CAT-031's (coordinator decision 27).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-144 |
| Name | Green optical filter |
| Type | Optical |
| Anchor | [requirements.md#element-144](../requirements.md#element-144); named entry [named-elements.md#element-144](../invest/named-elements.md#element-144); source record [todo-297](../requirements.md#todo-297) |
| Owner | S499 |
| Disposition | Candidate **Duplicate closure** of [CAT-031 green_filter](CAT-031-green_filter.md) (`green_filter`, "Green filter"), closed inside the Colour filters Family slice. No delta from CAT-031. |
| Acceptance authority | The CAT-031 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "absent green input cannot be recoloured into green; opaque frame blocks rays", is CAT-031 rows **F4 and F5**; the ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-031's `green_filter` (`elements/GreenFilter/`) |
| Roadmap story | Story 13.4 |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL, which follows ENGINE-SPATIAL-QUERIES |

## 2. Declaration

The `green_filter` declaration, art and bindings are [CAT-031's declaration](CAT-031-green_filter.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026).

## 3. Engine capabilities

The probe is [EL-143 section 3](EL-143-red-optical-filter.md#3-engine-capabilities) row for row, with the same citations: present R1, R2, R12; narrower R9 (owner ENGINE-SPATIAL-QUERIES); missing M2, M3, M5, M6 (owner ENGINE-OPTICAL). Counts 3 / 0 / 1 / 4.

## 4. Sources and legacy

**Sources.** Row [element-144](../requirements.md#element-144): passes only the existing green channel with declared loss; absent green input cannot be recoloured; opaque frame blocks rays. [todo-297](../requirements.md#todo-297). CAT row [current-cat-031](../requirements.md#current-cat-031). [todo-309](../requirements.md#todo-309). Refinement **optical-pairings** (S694). Campaign: filters first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); filter body and law harvested in full in [EL-143](EL-143-red-optical-filter.md#4-sources-and-legacy), unchanged here):
1. Aperture and Filter transmission = mask — `parts/ColourFilterPart.cs@a6c914e:L10-L12`; green mask (0, 1, 0) — `engine/OpticalColour.cs@a6c914e:L15-L15`. Carry forward.
2. Channel validation; frame, pane and art — `parts/ColourFilterPart.cs@a6c914e:L15-L42`; green ink `#62aa78` and two marks — `engine/OpticalColour.cs@a6c914e:L27-L27`, `engine/OpticalColour.cs@a6c914e:L61-L76`. Carry forward behaviour; not Godot materials.
3. Two-sided straight transmission — `engine/OpticalNetwork.cs@a6c914e:L94-L98`, `engine/OpticalNetwork.cs@a6c914e:L113-L126`. Carry forward.
4. Catalogue entry and scene (Colour = 2) — `parts/catalog/green_filter.tres@a6c914e:L6-L11`, `parts/scenes/green_filter.tscn@a6c914e:L1-L5`. Carry forward the entry.
5. Acceptance: front, back and rotated green pass equals source × (0, 1, 0); a source lacking green yields no reception — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L69-L103`; green then red extinguishes, two greens do not amplify — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L104-L127`; frame stops an off-centre ray and the pane bounces a ball — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L169-L212`; green beam ink stays visible without red — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L213-L230`. Carry forward.
6. Integration: green-filtered laser feeds the combiner's second input — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L39-L90`. Carry forward.

**Files harvested:** `parts/ColourFilterPart.cs`, `parts/catalog/green_filter.tres`, `parts/scenes/green_filter.tscn`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-031's rows](CAT-031-green_filter.md#acceptance-outline) F1 to F9, with their fixtures, tolerances and stall guards. This identity's outcomes map to **F4** and **F5**; the ledger row cites those tests. No row is stated or inherited here.

**Behaviour and numbers.** P′ = (0, 1, 0) ∘ P. An amber beam (10, 7.8, 3.2) W leaves as (0, 7.8, 0) W; 10 + 3.2 = 13.2 W is absorbed (the declared loss, K-5). A red laser's (10, 0, 0) W leaves as nothing. The 7.8 W green is 3.1 times the default 2.5 W green-receiver threshold.

## 6. Open questions

None open: declared loss (K-5), white source (D18, K-9), absorbed heat (K-10) and the closure with CAT-031's rows as the single authority (coordinator decision 16) are decided as in [EL-143 section 6](EL-143-red-optical-filter.md#6-open-questions).

Needs the owner: none.

## 7. Integration

As [EL-143 section 7](EL-143-red-optical-filter.md#7-integration); channel partners are the green laser [EL-177](EL-177-green-laser-emitter.md) and the green receiver [EL-147](EL-147-green-selective-receiver.md); it feeds the combiner's second input in the white-light lesson. Coverage binding (planned, not yet live): catalogue key `green_filter` (CAT-031). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).

## 8. Delivery

Closed inside the Colour filters Family slice. Prerequisites, in order: the engine programme (owner decision 3a) including ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL as corrected in the batch summary; a laser and a detector. Files (CAT slice): `elements/GreenFilter/`, one guarded solution line, rows in `ColourFilterTests` and `VisibleSolidColliderTests`, the ledger row. Review tier: data-only if the probe passes. Disqualifiers: as EL-143.

## 9. Engine conformance

Verdict: **Conforms**, on the seven points as [EL-143 section 9](EL-143-red-optical-filter.md#9-engine-conformance): six box colliders and one declared transmission vector (0, 1, 0); no engine code names the part; art, colliders and (absent) animation are separate layers; counts are fixed; its tests are CAT-031's scenarios.
