# EL-132 · Slow-neutron detector — element readiness spec

**Current delivery (24 October 2026):** [EL-132 delivery slice](../invest/named-elements.md#element-132). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [element-132](../requirements.md#element-132). Reads fictional toy sources; never real-world detection guidance. The shared declaration, probe, family rows and conformance are in the family lead, [EL-131](EL-131-fast-neutron-detector.md), which in turn follows the [rate meter](RAD-08-radiation-rate-meter.md); the law is in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions). This spec states the slow detector's own values, calculations and rows.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-132 · Slow-neutron detector |
| Type | Radiation |
| Anchor | [requirements.md#element-132](../requirements.md#element-132); [named-elements.md#element-132](../invest/named-elements.md#element-132); scope source [radiation-19](../requirements.md#radiation-19) (umbrella, index only) |
| Owner | S630 |
| Disposition | Member of the Family slice **Neutron detectors** with [EL-131](EL-131-fast-neutron-detector.md) (lead). It differs only in declared values: the non-zero weight is NeutronSlow, and the mark, id, title and description. Not a duplicate (D8). |
| Planned package | id `slow_neutron_detector`, directory `elements/SlowNeutronDetector/`, class `SlowNeutronDetectorElementDeclaration` |
| Related identities | Source [RAD-16](RAD-16-neutron-source-module.md); moderator [RAD-17](RAD-17-water-moderator-tank.md); absorber [RAD-18](RAD-18-neutron-absorber-panel.md). No CAT part is refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default): rows S1 and S2 take at most 2 s; S3 to S5, cross-part rows owned by the RAD-17 slice (S3, S5) and the RAD-18 slice (S4), run within a RAD-17 fill and drain, which fits 30 s (audit X-2) |

## 2. Declaration

The declaration is [EL-131's](EL-131-fast-neutron-detector.md#2-declaration) (the rate meter's body, ports, aperture, threshold node `detect`, pulse on its rise, contact `relay` and bindings, with `threshold` as the only parameter) with these values.

| Item | Value |
| --- | --- |
| Receiver **(field)** | One fixed channel-weight row: NeutronSlow = 1, every other weight 0 |
| Catalogue | id `slow_neutron_detector`, "Slow-neutron detector", `ElementCategory.Radiation` **(field)**, palette order 720 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Reads slow neutrons only. Fast neutrons count only after something has slowed them." |

### Art and solidity

As the lead: the meter's art with an "n" and one bar under it printed flat beside the dial (the fast detector has two chevrons). No added node or collider.

### Compiled counts

As the lead: 1 static body, 5 colliders, 1 signal node (threshold), 1 supply node, 1 receiver, 5 bindings.

## 3. Engine capabilities

The family table ([EL-131 section 3](EL-131-fast-neutron-detector.md#3-engine-capabilities)) applies unchanged: 5 present, 0 untested, 2 narrower (N1 ENGINE-SPATIAL-QUERIES; N3 ENGINE-OPTICAL), 4 missing (M1 ENGINE-OPTICAL; M2 and M4 ENGINE-RADIATION; M3 ENGINE-SIGNAL-NODES). One point matters more here than for the lead: slow flux exists only where a crossed row declares a conversion ([radiation convention 5](RAD-08-radiation-rate-meter.md#radiation-conventions)), which is part of ENGINE-RADIATION's row model (M2), not of this package.

**Dependencies.** Battery and Powered gate (delivered); RAD-08; for the requirement's own scene RAD-16 and RAD-17 (which needs ENGINE-LIQUID).

## 4. Sources and legacy

- [element-132](../requirements.md#element-132) and the [radiation-19](../requirements.md#radiation-19) integration task (moderated flow drives Slow; photon source and missing supply are controls).
- [Named entry](../invest/named-elements.md#element-132), owner S630; map row; binding `docs/coverage/engine/element-02.json` (proof S630, relation radiation-19).
- [S605](../invest/decisions.md#s605) neutron row; research slots 101 "Slow Water", 102 "The Final Layer", 104 "One Speed at a Time".
- **Legacy.** None. Hits are coverage snapshots only, e.g. `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L14898-L14901`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance

### Behaviour and numbers

Starting values (D1). Strength = 1 × the NeutronSlow reading. Every conversion below is [radiation convention 5](RAD-08-radiation-rate-meter.md#radiation-conventions) on the Liquid row (NeutronFast 2.4 /m, NeutronSlow 0.3 /m, conversion NeutronFast → NeutronSlow, m = 0.8): per crossing of length L, out_fast = in_fast · e^(−2.4·L) and out_slow = in_slow · e^(−0.3·L) + 0.8 · in_fast · (1 − e^(−2.4·L)), the converted power not attenuated again inside that crossing.

- **Unmoderated fast flux.** A Neutron module 1 m away with nothing between gives 0.882 W of NeutronFast and 0 W of NeutronSlow: strength 0. The detector contains no conversion; fast flux never counts as slow by itself.
- **Through a full moderator tank** (0.5 m of liquid between two Window walls, each NeutronFast and NeutronSlow 1 /m × 0.02 m). Entering the liquid: in_fast = 0.882 × e^−0.02 = 0.882 × 0.980 = 0.8645 W, in_slow = 0. Leaving it: out_fast = 0.8645 × e^−1.2 = 0.8645 × 0.301 = 0.2604 W; out_slow = 0 + 0.8 × 0.8645 × (1 − 0.301) = 0.8 × 0.8645 × 0.699 = 0.4833 W. After the second wall: slow 0.4833 × 0.980 = 0.474 W at the detector, 137 % above the 0.2 W mark. Of the 0.6041 W of fast power the liquid removed, 0.4833 W continues as slow and 0.2 × 0.6041 = 0.1208 W is absorbed in the liquid: slowing is not absorbing, and both are accounted (out_fast + out_slow = 0.744 W ≤ in 0.8645 W).
- **Dry or drained tank.** No liquid on the path, no conversion: slow 0 W.
- **Part-filled.** With the waterline between the detector's middle and top sample rows, six of nine samples see liquid: 0.474 × 6 ÷ 9 = 0.316 W, 58 % above the mark, so `detect` is on. With only the bottom row wet: 0.474 × 3 ÷ 9 = 0.158 W, inside the 0.15 to 0.2 W band between release (0.75 × 0.2) and mark, so `detect` keeps its previous state; no row asserts the level at that fill. The reading rises with the fill.
- **Absorber after the tank.** The panel's 0.30 m foot needs room between tank and detector, so this case uses [RAD-18](RAD-18-neutron-absorber-panel.md)'s layout A (detector aperture 1.30 m from the module, 0.80 m from the tank centre): open 0.526 W, slow through the full tank 0.526 × 0.980 × 0.559 × 0.980 = 0.283 W. Absorber NeutronSlow 60 /m × 0.05 m = 3: 0.283 × e^−3 = 0.283 × 0.0498 = 0.014 W, 91 % below the 0.15 W release: off. Dense slab instead (NeutronSlow 2.8 /m × 0.05 m = 0.14): 0.283 × e^−0.14 = 0.283 × 0.869 = 0.246 W, 23 % above the mark: on.
- **Photons.** A Gamma capsule gives strength 0.
- **Energy.** Slow power at the detector (0.474 W) plus fast power at the same place (0.2604 × 0.980 = 0.255 W) is 0.729 W, less than the 0.847 W of fast power that arrives through the dry tank: conversion creates none.
- **Tick order.** As the lead: the field solve of tick n commits the strength on that tick's final poses and liquid levels; the signal network of tick n + 1 turns `detect` on and closes `relay`.

### Rows

Class `NeutronDetectorTests` (family). The family rows F1, F3, F4 and F8 to F11 of [EL-131 section 5](EL-131-fast-neutron-detector.md#5-acceptance) run for this member with a slow-group source. Readings ±3 %. Its own rows:

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| S1 | Given a fast-group source at 1 m with nothing between, the slow detector's strength is 0 and `detect` stays off for 240 ticks (row: unmoderated fast flux does not count as slow) | `NeutronDetectorTests.TheSlowDetectorIgnoresUnmoderatedFastFlux` | family row F1 with a slow source | below 0.001 W |
| S2 | Given a law-scene fast source of 10 W/sr, a law-scene volume with the Liquid row 0.5 m thick on the path, and the detector with its face 1 m from the source, the strength is 0.882 × 0.8 × (1 − e^−1.2) = 0.882 × 0.559 = 0.493 W and `detect` is on; with the volume removed it is 0 | `NeutronDetectorTests.ADeclaredConversionFeedsTheSlowDetector` | S1 | ±3 % |
| S3 | Given a Neutron module, a Moderator tank and the detector in line at 1 m, the strength is 0 dry, never falls during the fill, and is 0.474 W full; `detect` is on when full; a drained tank returns the strength to 0 and `detect` off (task 427: moderated flow drives Slow; RAD-17's row) | `NeutronDetectorTests.FillingTheModeratorDrivesTheSlowDetector` (a cross-part row owned by the RAD-17 slice: the tank follows this detector in level 126) | S1 | ±3 % at the dry, full and drained ends; non-decreasing within 3 % during the fill |
| S4 | Given S3 full in RAD-18's layout A with an Absorber panel after the tank `detect` is off (0.014 W); with a Dense slab instead it is on (0.246 W) | `NeutronDetectorTests.OnlyTheAbsorberSilencesModeratedFlux` (a cross-part row owned by the RAD-18 slice: the absorber is taught at 127) | no panel: 0.283 W | ±3 % on 0.246 W; 0.014 W below the 0.15 W release |
| S5 | Given S3 full, slow strength here plus fast strength at a Fast-neutron detector in the same place is at most the dry fast reading 0.847 W (0.474 + 0.255 = 0.729 W) | `NeutronDetectorTests.ModerationCreatesNoFlux` (a cross-part row owned by the RAD-17 slice, as S3) | — | sum ≤ 0.847 W + 3 % |

## 6. Open questions

None open.

1. **One part with a group mode, or two — decided** (D8): two packages in one Family slice, as the lead.
2. **Thresholds — decided** (D1, D11): RAD-08's `threshold`, default 0.2 W.
3. **Conversion law — decided** (coordinator decision 21): radiation convention 5; the Liquid row declares the conversion, the detector none.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Battery, Powered gate, Counter (delivered); RAD-16, RAD-17, RAD-18, EL-128, EL-131, RAD-01 (undelivered). Port domains as RAD-08.
- **Cross-element task.** [sequence-task-427](../requirements.md#sequence-task-427): row S3 with family rows F3 and F4; the Fast half is [EL-131](EL-131-fast-neutron-detector.md) row F5.
- **Campaign.** Chapter 13, first use 121–130 (lessons 126, 127, 129); reuse 136–150.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7, for the lead's reasons: the ledger entry for element-132 carries no `todo-N` link and S630 has no anchor in `docs/planning/invest/scope-corrections.md` (checked by search).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

Delivered in the Neutron detectors Family slice; prerequisites (blocked on the engine programme (owner decision 3a), with ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-08; S7; no dependency on ENGINE-THERMAL), review tier and disqualifiers as [EL-131 section 8](EL-131-fast-neutron-detector.md#8-delivery). Files: `elements/SlowNeutronDetector/SlowNeutronDetector.csproj`, `SlowNeutronDetectorElementDeclaration.cs`, `slow_neutron_detector.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; theory cases in `NeutronDetectorTests`; a row in `VisibleSolidColliderTests`. The slice's own positive for this member is row S2, which needs no other undelivered part; rows S3 to S5 are queued in the contracts of RAD-17 and RAD-18. Own disqualifier: ENGINE-RADIATION delivering no declared conversion (then no slow flux can exist; the slice stops and the story is amended).

## 9. Engine conformance

Verdict: **Conforms**, on the points of [EL-131 section 9](EL-131-fast-neutron-detector.md#9-engine-conformance): a static body, five primitives, one receiver with one fixed weight row, one threshold node, one contact, one parameter. "Fast does not count as slow" is a zero weight; "moderated flux counts" is a conversion declared on someone else's row. The detector holds no rule about either.
