# EL-145 · Blue optical filter — named-identity spec

**Current delivery (24 October 2026):** [EL-145 delivery slice](../invest/named-elements.md#element-145). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`. Lengths are metres. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions). [EL-143](EL-143-red-optical-filter.md) is the filter family lead for the probe, sources and conformance; this identity's declaration is CAT-011's (coordinator decision 27).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-145 |
| Name | Blue optical filter |
| Type | Optical |
| Anchor | [requirements.md#element-145](../requirements.md#element-145); named entry [named-elements.md#element-145](../invest/named-elements.md#element-145); source record [todo-297](../requirements.md#todo-297) |
| Owner | S500 |
| Disposition | Candidate **Duplicate closure** of [CAT-011 blue_filter](CAT-011-blue_filter.md) (`blue_filter`, "Blue filter"), closed inside the Colour filters Family slice. No delta from CAT-011. |
| Acceptance authority | The CAT-011 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "absent blue input cannot be recoloured into blue; opaque frame blocks rays", is CAT-011 rows **F4 and F5**; the ledger row cites the package and those tests. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-011's `blue_filter` (`elements/BlueFilter/`) |
| Roadmap story | Story 13.4 |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL, which follows ENGINE-SPATIAL-QUERIES |

## 2. Declaration

The `blue_filter` declaration, art and bindings are [CAT-011's declaration](CAT-011-blue_filter.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026).

## 3. Engine capabilities

The probe is [EL-143 section 3](EL-143-red-optical-filter.md#3-engine-capabilities) row for row, with the same citations: present R1, R2, R12; narrower R9 (owner ENGINE-SPATIAL-QUERIES); missing M2, M3, M5, M6 (owner ENGINE-OPTICAL). Counts 3 / 0 / 1 / 4.

## 4. Sources and legacy

**Sources.** Row [element-145](../requirements.md#element-145): passes only the existing blue channel with declared loss; absent blue input cannot be recoloured; opaque frame blocks rays. [todo-297](../requirements.md#todo-297). CAT row [current-cat-011](../requirements.md#current-cat-011). [todo-309](../requirements.md#todo-309). Refinement **optical-pairings** (S694). Campaign: filters first use 51–60 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

**Legacy** (shared optical facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); filter body and law harvested in full in [EL-143](EL-143-red-optical-filter.md#4-sources-and-legacy), unchanged here):
1. Aperture and Filter transmission = mask — `parts/ColourFilterPart.cs@a6c914e:L10-L12`; blue mask (0, 0, 1) — `engine/OpticalColour.cs@a6c914e:L16-L16`. Carry forward.
2. Channel validation; frame, pane and art — `parts/ColourFilterPart.cs@a6c914e:L15-L42`; blue ink `#5b9cdb` and three marks — `engine/OpticalColour.cs@a6c914e:L28-L28`, `engine/OpticalColour.cs@a6c914e:L61-L76`. Carry forward behaviour; not Godot materials.
3. Two-sided straight transmission — `engine/OpticalNetwork.cs@a6c914e:L94-L98`, `engine/OpticalNetwork.cs@a6c914e:L113-L126`. Carry forward.
4. Catalogue entry and scene (Colour = 3) — `parts/catalog/blue_filter.tres@a6c914e:L6-L11`, `parts/scenes/blue_filter.tscn@a6c914e:L1-L5`. Carry forward the entry.
5. Acceptance: front, back and rotated blue pass equals source × (0, 0, 1); a source lacking blue yields no reception — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L69-L103`; blue then green extinguishes, two blues do not amplify — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L104-L127`; frame and pane controls — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L169-L212`; blue beam ink stays visible without red (alpha 0.223–0.225 at power 0.32) — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L213-L230`. Carry forward.
6. Integration: blue-filtered laser feeds the combiner's third input — `CuriousContraptions.tests/BeamCombinerTests.cs@a6c914e:L39-L90`. Carry forward.

**Files harvested:** `parts/ColourFilterPart.cs`, `parts/catalog/blue_filter.tres`, `parts/scenes/blue_filter.tscn`, `engine/OpticalColour.cs`, `engine/OpticalNetwork.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`, `CuriousContraptions.tests/BeamCombinerTests.cs`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-011's rows](CAT-011-blue_filter.md#acceptance-outline) F1 to F10, with their fixtures, tolerances and stall guards. This identity's outcomes map to **F4** and **F5**; the ledger row cites those tests. No row is stated or inherited here.

**Behaviour and numbers.** P′ = (0, 0, 1) ∘ P. An amber beam (10, 7.8, 3.2) W leaves as (0, 0, 3.2) W; 17.8 W is absorbed (the declared loss, K-5). That 3.2 W is the weakest filtered amber channel: 28 % above a default blue receiver's 2.5 W, 2.88 W after a combiner (still on), 1.6 W after a 50/50 splitter (off), and as a gate control a mean of 3.2 ÷ 3 = 1.07 W (off). A blue laser's (0, 0, 10) W passes whole.

## 6. Open questions

None open: declared loss (K-5), white source (D18, K-9), absorbed heat (K-10) and the closure with CAT-011's rows as the single authority (coordinator decision 16) are decided as in [EL-143 section 6](EL-143-red-optical-filter.md#6-open-questions).

Needs the owner: none.

## 7. Integration

As [EL-143 section 7](EL-143-red-optical-filter.md#7-integration); channel partners are the blue laser [EL-178](EL-178-blue-laser-emitter.md) and the blue receiver [EL-148](EL-148-blue-selective-receiver.md); it feeds the combiner's third input in the white-light lesson. Coverage binding (planned, not yet live): catalogue key `blue_filter` (CAT-011). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).

## 8. Delivery

Closed inside the Colour filters Family slice. Prerequisites, in order: the engine programme (owner decision 3a) including ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL as corrected in the batch summary; a laser and a detector. Files (CAT slice): `elements/BlueFilter/`, one guarded solution line, rows in `ColourFilterTests` and `VisibleSolidColliderTests`, the ledger row. Review tier: data-only if the probe passes. Disqualifiers: as EL-143.

## 9. Engine conformance

Verdict: **Conforms**, on the seven points as [EL-143 section 9](EL-143-red-optical-filter.md#9-engine-conformance): six box colliders and one declared transmission vector (0, 0, 1); no engine code names the part; art, colliders and (absent) animation are separate layers; counts are fixed; its tests are CAT-011's scenarios.
