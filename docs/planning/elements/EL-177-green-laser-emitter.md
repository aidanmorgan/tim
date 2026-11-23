# EL-177 · Green laser emitter — named-identity spec

**Current delivery (24 October 2026):** [EL-177 delivery slice](../invest/named-elements.md#element-177). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after its audit and the owner decisions of 22 November 2026. Legacy citations use `path@a6c914e:Lstart-Lend`. Lengths are metres; local +X is the beam direction. Optical rules are the [optical conventions](EL-176-red-laser-emitter.md#optical-conventions). [EL-176](EL-176-red-laser-emitter.md) is the lead of the Laser colours family; this spec states only what differs.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-177 |
| Name | Green laser emitter |
| Type | Optical |
| Anchor | [requirements.md#element-177](../requirements.md#element-177); named entry [named-elements.md#element-177](../invest/named-elements.md#element-177); source record [campaign-element-coverage](../requirements.md#campaign-element-coverage); scope index [todo-215](../requirements.md#todo-215) |
| Owner | S493 |
| Disposition | To deliver as a member of the Family slice **Laser colours** with [EL-176](EL-176-red-laser-emitter.md) and [EL-178](EL-178-blue-laser-emitter.md), after the amber laser [CAT-036](CAT-036-laser.md). It differs from its siblings only in declared values. Not a duplicate of CAT-036 (its own channel and catalogue entry, D8). |
| Planned package | id `green_laser`, directory `elements/GreenLaser/`, class `GreenLaserElementDeclaration` |
| Roadmap story | [Story 13.1c](../../../_bmad-output/planning-artifacts/epics.md#story-131c-green-laser-emitter-el-177) |
| Status | not started; no package; **blocked on the engine programme (owner decision 3a, 22 November 2026)**, then on ENGINE-OPTICAL (which follows ENGINE-SPATIAL-QUERIES) and on CAT-036 |

## 2. Declaration

Every row of [EL-176 section 2](EL-176-red-laser-emitter.md#2-declaration) applies (interfaces; one static body; housing box, base box at (0, −0.505, 0), collar cylinder and two socket spheres; `PowerIn` at (−0.7, 0, 0) and `ActivationIn` Pulse at (0, 0.5, 0); one `enable` memory node whose level enables the emitter; one supply consumer of 12 W; emitter origin (0.72, 0, 0), axis +X, collimated, range 16 m; no parameters or stores; the same art classes and counts), with these declared values changed:

| Item | Value |
| --- | --- |
| Radiant power | (0, 10, 0) W |
| Collar art | green `#62aa78`, with two cream bars printed flat |
| Lens binding | albedo slate (85, 101, 115) → green (98, 170, 120), ExponentialFollow 12 /s, from committed emission |
| Catalogue | Id `green_laser`, "Green laser", `ElementCategory.Optics` (missing, M6), colour (0.38, 0.67, 0.47), palette order 608 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "A trigger enables the green laser until Reset. Connect electricity separately. Its beam carries only green light." |

Compiled counts: bodies 1, colliders 5 (2 boxes, 1 cylinder, 2 spheres), signal nodes 1, supply nodes 1, emitters 1, bindings 3.

## 3. Engine capabilities

The probe is [EL-176 section 3](EL-176-red-laser-emitter.md#3-engine-capabilities) row for row, with the same citations: present R1, R2, R3, R12; present-untested R5 and R7 (proved first by CAT-036's slice); narrower R6 and R11 (owner ENGINE-OPTICAL) and R9 (owner ENGINE-SPATIAL-QUERIES); missing M1, M2, M5, M6 (owner ENGINE-OPTICAL). Counts 4 / 2 / 3 / 4.

## 4. Sources and legacy

**Sources.** Row [element-177](../requirements.md#element-177): a supplied, enabled emitter produces a bounded green optical channel; a wrong-channel receiver control remains inactive. [campaign-element-coverage](../requirements.md#campaign-element-coverage); scope index [todo-215](../requirements.md#todo-215); refinement **optical-pairings** (S694).

**Legacy** (shared facts S1–S8 in [CAT-036-laser](CAT-036-laser.md#legacy-harvest)):
1. Laser range, lens, enable/supply rule, trigger latch, ports, geometry and lens rate — `parts/LaserPart.cs@a6c914e:L12-L62`. Carry forward all but the amber power.
2. Single-channel fixture power = channel mask — `CuriousContraptions.tests/ReceiverAnimationTests.cs@a6c914e:L21-L25`. Carry forward as the basis for one full unit in one channel.
3. A primary receiver rejects light of another channel (purity 0.9; green strength 0 under red or blue) — `engine/OpticalColour.cs@a6c914e:L35-L48`; `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L128-L143`. Carry forward (the named wrong-channel control).
4. Enable × supply matrix, transforms, occlusion — `CuriousContraptions.tests/OpticalTests.cs@a6c914e:L47-L174`. Carry forward.
5. Green beam ink and visibility without red — `CuriousContraptions.tests/ColourOpticsTests.cs@a6c914e:L213-L230`. Carry forward.

**Files harvested:** `parts/LaserPart.cs`, `engine/OpticalColour.cs`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/ReceiverAnimationTests.cs`, `CuriousContraptions.tests/OpticalTests.cs`, `CuriousContraptions.tests/ColourOpticsTests.cs`.

## 5. Acceptance

**Behaviour and numbers.** As EL-176 section 5 with the channel changed: (0, 10, 0) W × (energy granted over the tick ÷ energy demanded); demand 12 W = 0.1 J per tick against the Battery's 1 J per tick at its 120 W default; 8.33 W at a 10 W `power_limit`; range 16 m. At a red receiver the beam's red share is 0 %, so that receiver's strength is zero however strong the beam is.

**Rows.** The family's rows are [EL-176's A1 to A11](EL-176-red-laser-emitter.md#rows), theories of `LaserColourTests` over the three packages, observed for this package with the family fixture's Laser receiver (CAT-038, taught at 51). This identity's ledger outcomes:

| Ledger outcome | Family row | Test |
| --- | --- | --- |
| A supplied, enabled emitter produces a bounded green channel | A1 (reading (0, 10, 0) W within ±2 %, red and blue below the floor), A2 (control: missing supply or enable), A5 (range) | `LaserColourTests.ASuppliedEnabledLaserDeliversOnlyItsOwnChannel`, `.MissingSupplyOrEnableEmitsNothing`, `.TheBeamEndsAtItsDeclaredRange` |
| A wrong-channel receiver control remains inactive | A10, for the green laser on a supplied red receiver and on a supplied blue receiver: each reads the 10 W in the green channel, its level stays off and its load is never supplied for 240 ticks (a cross-part row owned by the Colour receivers slice: those receivers are taught at 54a, after this laser at 54) | `LaserColourTests.AReceiverOfAnotherChannelStaysInactive` |

Chrome recipe (pending, post-catalogue): as EL-176, with a green receiver lit and a red receiver beside it dark.

## 6. Open questions

None open: channel power, collar, id and title (D1, D9, K-1), scheduling (Story 13.1c, one Family slice) and range (D19) are decided as in [EL-176 section 6](EL-176-red-laser-emitter.md#6-open-questions).

Needs the owner: none.

## 7. Integration

As [EL-176 section 7](EL-176-red-laser-emitter.md#7-integration); channel partners are the green filter [EL-144](EL-144-green-optical-filter.md), the green receiver [EL-147](EL-147-green-selective-receiver.md) and the yellow, cyan and white receivers through the combiner. Coverage binding (planned, not yet live): a named package is bound by the binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, **blocked on the coverage follow-on story S7** (source `campaign-element-coverage` is not a `todo-N` record; decision S493 has no anchor in `docs/planning/invest/scope-corrections.md`). S7 also widens the reader to admit companion packages and non-EL identities (owner decision 15); the section is not written here.

## 8. Delivery

Family slice with EL-176 and EL-178. Prerequisites, in order: the engine programme (owner decision 3a) including ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL as corrected in the batch summary; CAT-036; the Laser receiver (CAT-038) for the family fixture; S7 for the binding. Row A10 runs in the Colour receivers slice (54a). Files: `elements/GreenLaser/` (project, declaration, `green_laser.tscn`, `icon.svg`), one guarded `CuriousContraptions.slnx` line, rows in `LaserColourTests` and `VisibleSolidColliderTests`, this spec's binding section. Review tier: data-only if CAT-036 has delivered and tested rows R5 and R7 and the probe passes. Disqualifiers: as EL-176.

## 9. Engine conformance

Verdict: **Conforms**, on the seven points as [EL-176 section 9](EL-176-red-laser-emitter.md#9-engine-conformance): the family's declaration with radiant power (0, 10, 0) W; no engine code names it; five colliders, flush art and the lens binding are separate layers; counts are fixed; tests are scenarios.
