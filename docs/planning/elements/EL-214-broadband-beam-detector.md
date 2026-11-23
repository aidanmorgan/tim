# EL-214 · Broadband beam detector — named-identity spec

**Current delivery (24 October 2026):** [EL-214 delivery slice](../invest/named-elements.md#element-214). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`. Lengths are metres; the sensitive face looks along local −X. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions). It shares the receiver family's body; [EL-146](EL-146-red-selective-receiver.md) is the family lead and this spec states what differs.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-214 |
| Name | Broadband beam detector |
| Type | Optical |
| Anchor | [requirements.md#element-214](../requirements.md#element-214); named entry [named-elements.md#element-214](../invest/named-elements.md#element-214); scope index [todo-215](../requirements.md#todo-215) |
| Owner | S497 |
| Disposition | Candidate **Duplicate closure** of [CAT-038 light_receiver](CAT-038-light_receiver.md) (`light_receiver`, "Laser receiver"), closed inside that part's slice. No delta from CAT-038. |
| Acceptance authority | The CAT-038 spec's acceptance rows are the single authority (coordinator decision 16, 22 November 2026). This identity's source outcome, "back-face, insufficient incident power and missing electrical supply are separate negative controls", is CAT-038 row **R2** (back face), rows **R4 and R4b** (insufficient incident power) and the **control of R1** (missing supply); the ledger row cites the package and those tests. Rows that need a later part (the splitter for R4b, the Flashlight for the cone row) run when that part is delivered, as CAT-038 states; the closure is recorded when the mapped rows have run. This spec carries no rows of its own. |
| Planned package | none of its own; CAT-038's `light_receiver` (`elements/LightReceiver/`). The CAT spec is the single declaration (coordinator decision 27); section 2 points to it. |
| Roadmap story | [Story 13.2](../../../_bmad-output/planning-artifacts/epics.md#story-132-broadband--pure-channel-optical-receivers-cat-038-cat-056-cat-032-cat-012) |
| Status | not started; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES) and ENGINE-SIGNAL-NODES |

## 2. Declaration

The `light_receiver` declaration, art, bindings and compiled counts are [CAT-038, Declaration](CAT-038-light_receiver.md#declaration); this identity adds none (coordinator decision 27, 23 November 2026). Palette: CAT-038's order 602 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); no order of its own.

## 3. Engine capabilities

The probe is [EL-146 section 3](EL-146-red-selective-receiver.md#3-engine-capabilities) row for row, with the same citations: present R1, R2, R3, R4, R12, R13; present-untested R11; narrower R9 (owner ENGINE-SPATIAL-QUERIES) and R10 (ENGINE-OPTICAL); missing MS (ENGINE-SIGNAL-NODES) and M2, M4, M5, M6 (ENGINE-OPTICAL). Counts 6 / 1 / 2 / 5. The mean is the WeightedSum value of the one declared strength rule, not a second code path chosen per part.

## 4. Sources and legacy

**Sources.** Row [element-214](../requirements.md#element-214): a finite sensing surface integrates supported optical channels and drives a separately supplied contact above its threshold; back face, insufficient incident power and missing electrical supply are separate negative controls. Campaign: first use 51–60; practice 71–80; reuse 110 and 142 ([campaign-reservations](../invest/campaign-reservations.md)). CAT row [current-cat-038](../requirements.md#current-cat-038): threshold 0.05–2 / default 0.25; half-power 0.35 positive and quarter-power 0.175 negative must survive canonical f32 admission.

**Legacy** (shared optical facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest); receiver body facts in [EL-146](EL-146-red-selective-receiver.md#4-sources-and-legacy)):
1. Default colour Broadband; threshold binding and 0.05–2 validation; aperture; ports and contact route; receive and match — `parts/LightReceiverPart.cs@a6c914e:L11-L46`. Carry forward behaviour; not the per-part callback.
2. Broadband strength = channel mean; broadband accepts without purity — `engine/OpticalColour.cs@a6c914e:L35-L48`. Carry forward.
3. Indicator and art — `parts/LightReceiverPart.cs@a6c914e:L22-L27`, `parts/LightReceiverPart.cs@a6c914e:L47-L59`. Carry forward behaviour; not Godot materials.
4. Catalogue entry and scene (no Colour override, so Broadband) — `parts/catalog/light_receiver.tres@a6c914e:L6-L12`, `parts/scenes/light_receiver.tscn@a6c914e:L1-L4`. Carry forward the entry.
5. Acceptance: all 8 combinations of laser enable × laser supply × receiver supply — the receiver lights iff the laser emits and the load is powered iff all three; received power equals the amber laser power; supply loss clears at the next snapshot — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L47-L94`; a reversed receiver (back face) and one 1 m off-axis stay dark under any rotation — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L96-L137`; nearest absorbing target wins with no double counting — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L176-L211`. Carry forward.
6. Acceptance: contact binding rolls back with the transaction — `CuriousContraptions.tests/ElectricalContactBindingTests.cs@a6c914e:L85-L111`; indicator across failure, pause and Reset — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L44-L102`; reception uses solved poses, and disabling participation removes it — `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs@a6c914e:L165-L199`. Carry forward.
7. DESIGN.md: default 0.25 accepts one half-power amber beam but not one quarter-power branch. Carry forward (mean of half amber = 0.35; quarter = 0.175).

**Files harvested:** `parts/LightReceiverPart.cs`, `parts/catalog/light_receiver.tres`, `parts/scenes/light_receiver.tscn`, `engine/OpticalColour.cs`, `engine/MachineData.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/OpticalTests.cs`, `CuriousContraptions.tests/ElectricalContactBindingTests.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs`.

## 5. Acceptance

**Authority.** Acceptance is [CAT-038's rows](CAT-038-light_receiver.md#acceptance-outline) R1 to R12, with their fixtures, tolerances and stall guards. This identity's outcomes map to **R2**, **R4 and R4b**, and the **control of R1**; the ledger row cites those tests. No row is stated here. The declared release ratio is exercised by CAT-038's R4 (from dark) and R12 (held while matched).

**Behaviour and numbers** (the laws behind those rows; one legacy unit is 10 W).

- **Rule.** Strength is (R_r + R_g + R_b) ÷ 3; `matched` turns on at `threshold` and off below 0.9 × `threshold`. A full amber beam: (10 + 7.8 + 3.2) ÷ 3 = 7.0 W. Half amber, after one 50/50 splitter: 3.5 W ≥ 2.5 W, on. Quarter amber, after two splitters: 1.75 W, off. These are the requirement row's 0.35 and 0.175 in watts; each sits 30 % or more from the threshold, so f32 admission cannot move either. A red laser: 3.33 W, on.
- **Any light, one law** (owner decision 9). A Flashlight 3 m in front gives 0.950 m² × 46.7 W/m² = 44.4 W, a mean of 14.8 W: on; the mean falls to 2.5 W at d = √(0.950 × 420.4 ÷ 3 ÷ 2.5) = 7.3 m. With several detectors in one cone the shares are scaled so that together they never receive more than the 90 W emitted.
- **Nearest solid.** A beam ends on the first absorbing collider it meets; a second detector behind the first reads nothing.
- **Contact and tick order.** The strength commits with the tick the light arrives; `matched` takes it at the start of the next tick and the contact closes in that tick.

Chrome recipe (pending, post-catalogue): CAT-038's.

## 6. Open questions

None open.

1. **"Integrates supported optical channels" — decided** (D15): the channel mean, which the CAT-038 row's values match.
2. **Torch cones — decided by the owner** (decision 9, 22 November 2026): one receiver law for all light; no "collimated only" attribute; `sequence-task-398` is reworded (text in the batch summary). Finite-width sampling with partial shadow remains [EL-153](EL-153-general-light-receiver.md)'s.
3. **Hysteresis — decided** (K-4): release ratio 0.9.
4. **One catalogue part satisfies EL-214 — decided**: Duplicate closure with CAT-038's rows as the single authority (coordinator decision 16).

Needs the owner: none.

## 7. Integration

- **Collaborators.** Lasers, splitter, mirror, combiner, gates (as their outlet observer), Beam shutter, Flashlight (undelivered); Battery, Powered gate, Motor, Wall (delivered). Port domains as EL-146. Behind an unpowered Beam shutter the detector is dark in any mounting (owner decision 6).
- **Campaign.** First use 51–60; practice 71–80; reuse 110 and 142.
- **Coverage binding (planned, not yet live).** Catalogue key `light_receiver` (CAT-038). A closed duplicate has no binding section of its own: it is covered by the package of the part that satisfies it and by the closure record, which the coverage follow-on story S7 must admit (owner decision 15 widens the reader).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Closed inside CAT-038's slice (the broadband member of the Colour receivers Family slice).
- **Prerequisites, in order.** The engine programme (owner decision 3a), including ENGINE-SPATIAL-QUERIES; ENGINE-SIGNAL-NODES; ENGINE-OPTICAL as corrected in the batch summary; the amber laser (CAT-036); the splitter (CAT-008) before row R4b can run.
- **Files (CAT slice).** `elements/LightReceiver/`, one guarded `CuriousContraptions.slnx` line, `CuriousContraptions.tests/LightReceiverTests.cs`, a `VisibleSolidColliderTests` row, the ledger row for EL-214.
- **Review tier.** Data-only if the probe finds every row present after the engine stories.
- **Disqualifiers.** A receiver with only one strength rule; no threshold node with a release ratio; a different unit scale.

## 9. Engine conformance

Verdict: **Conforms**, on the seven points as [EL-146 section 9](EL-146-red-selective-receiver.md#9-engine-conformance): the receiver family's declaration with the mean strength rule and no purity; no engine code names it; it couples through a signal node only; colliders (two boxes, two spheres), flush art and the disc binding are separate layers; counts are fixed; one sample point bounds its cost; its tests are CAT-038's scenarios.
