# EL-127 · Polymer shield — element readiness spec

**Current delivery (24 October 2026):** [EL-127 delivery slice](../invest/named-elements.md#element-127). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`, findings C-1, C-4, C-7, M-6, X-1 to X-4 and section 4 item 3) and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [element-127](../requirements.md#element-127). A fictional toy material, never real-world shielding guidance. The shared body, probe and conformance points are in the family lead, [EL-126](EL-126-thin-screen-shield.md); the law and the rows are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions), cited by item. This spec states the polymer's own values, calculations and rows.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-127 · Polymer shield |
| Type | Radiation |
| Anchor | [requirements.md#element-127](../requirements.md#element-127); [named-elements.md#element-127](../invest/named-elements.md#element-127); scope source [radiation-05](../requirements.md#radiation-05) (umbrella, index only) |
| Owner | S611 |
| Disposition | Member of the Family slice **Shield materials** with [EL-126](EL-126-thin-screen-shield.md) (lead) and [EL-128](EL-128-dense-shield.md). It differs from its siblings only in declared values. [RAD-18](RAD-18-neutron-absorber-panel.md) shares the family's law but is its own slice at level 127 (coordinator decision 37). Not a duplicate (D8). |
| Planned package | id `polymer_shield`, directory `elements/PolymerShield/`, class `PolymerShieldElementDeclaration` |
| Related identities | Proving partners [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-08](RAD-08-radiation-rate-meter.md); [RAD-04](RAD-04-beta-minus-source-cartridge.md) (undelivered) is the planned part the law-scene beta beam used here stands in for; sample material for [EL-129](EL-129-sheet-thickness-transmission-gauge.md). No CAT part is refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); no row is longer than 2 s |

## 2. Declaration

The declaration is [EL-126's](EL-126-thin-screen-shield.md#2-declaration) with these values.

| Item | Value |
| --- | --- |
| Slab collider | box 0.05 × 0.80 × 0.80 m centred (0, 0.46, 0); foot as the lead |
| Mass and inertia | 1.0 kg, lighter than the 4 kg Bowling ball so it reads as plastic. Volumes 0.032 and 0.0144 m³ (density 1.0 ÷ 0.0464 = 21.55 kg/m³): slab 0.690 kg, foot 0.310 kg; centre of mass at y = (0.690 × 0.46 + 0.310 × 0.03) ÷ 1.0 = 0.327 m. Principal moments about it, by the lead's rule: X = 0.690 × (0.80² + 0.80²) ÷ 12 + 0.310 × (0.06² + 0.80²) ÷ 12 + 0.690 × 0.133² + 0.310 × 0.297² = 0.0736 + 0.0166 + 0.0123 + 0.0273 = 0.130 kg·m²; Y = 0.690 × (0.05² + 0.80²) ÷ 12 + 0.310 × (0.30² + 0.80²) ÷ 12 = 0.0369 + 0.0189 = 0.0558 kg·m²; Z = 0.690 × (0.05² + 0.80²) ÷ 12 + 0.310 × (0.30² + 0.06²) ÷ 12 + 0.0396 = 0.0369 + 0.0024 + 0.0396 = 0.0789 kg·m². Principal frame the identity. |
| Contact material | restitution 0.25, friction 0.45, rolling resistance 0 (smooth plastic) |
| Radiation row **(field)** | Polymer: 8, 3, 1.5, 400, 300, 2, 2 per metre (radiation convention 4; BetaMinus 300 by coordinator decision 21, 22 November 2026); no conversion. The row is declared on this package's colliders; nothing reads the palette category, so no dense value can reach it. |
| Ports, parameters, stores, bindings | None (the old `ShieldThicknessPreset` is dropped, D13) |
| Catalogue | id `polymer_shield`, "Polymer shield", `ElementCategory.Radiation` **(field)**, palette order 705 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "A plastic slab. It stops beta and alpha, and only weakens gamma." |

### Art and solidity

As the lead's table, with `Visual/Slab` 0.05 m thick in cyan `#66b8c9` and `Visual/Pattern` two printed groove lines. It differs from the thin screen by thickness and groove count and from the dense shield by two grooves against three.

### Compiled counts

As the lead: 1 dynamic body, 2 box colliders, 2 radiation rows, nothing else.

## 3. Engine capabilities

The family table in [EL-126 section 3](EL-126-thin-screen-shield.md#3-engine-capabilities) applies unchanged: **4 present** (R1 to R4), **0 untested**, **1 narrower** (N1 line of sight, owner ENGINE-SPATIAL-QUERIES), **3 missing** (M1 the shared solve, ENGINE-OPTICAL; M2 rows, surface τ and chords, and M3 category and flag, ENGINE-RADIATION). No capability is particular to this member.

**Dependencies.** RAD-01 and RAD-08. The beta row P3 uses a law-scene collimated emitter, so it does not need RAD-04.

## 4. Sources and legacy

- [element-127](../requirements.md#element-127) row and the [radiation-05](../requirements.md#radiation-05) integration task (equal geometry, stacked thickness, gaps, restore).
- [Named entry](../invest/named-elements.md#element-127), owner S611; element map row (S605, S257); binding `docs/coverage/engine/element-02.json` (proof S611).
- [S605](../invest/decisions.md#s605) photon, alpha and neutron rows; research "Material shield panel" row and RAD-04 lesson "polymer suppresses the selected beta profile" (slot 91 → 116).
- **Legacy.** Same search as the radiation family (parts, engine, tests, levels, Campaign, reference): no polymer part, coefficient, test or level exists. Hits are coverage snapshots only: the older radiation-05 binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1752-L1803`, identical to the current binding, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

Starting values (D1); chord 0.05 m square to the path; transmission per crossing as [radiation convention 4](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Photons.** Medium: e^(−3 × 0.05) = e^−0.15 = 0.861. Soft: e^(−8 × 0.05) = e^−0.4 = 0.670. Hard: e^(−1.5 × 0.05) = e^−0.075 = 0.928. A Gamma capsule 1 m from the standard meter reads 0.882 × 0.861 = 0.759 W behind one slab and 0.882 × e^−0.30 = 0.882 × 0.741 = 0.653 W behind two: weakened, never switched off (the mark is 0.2 W; 0.653 W is more than three times it).
- **Not a dense shield.** At equal geometry the dense slab reads 0.882 × e^−1 = 0.324 W. Polymer 0.759 W is 0.759 ÷ 0.324 = 2.34 times that. To match one dense slab would take 1 ÷ 0.15 = 6.7 polymer slabs.
- **Beta.** e^(−300 × 0.05) = e^−15 = 3.1 × 10⁻⁷: a 1 W Beta-minus beam leaves at 3.1 × 10⁻⁷ W, under the 0.001 W floor, and ends within ln 1000 ÷ 300 = 0.023 m of its entry. Polymer suppresses beta where the thin screen (0.670 W) passes it. A 0.05 m part on the default row (BetaMinus 150 /m) also stops beta (e^−7.5 = 0.00055 W) but needs ln 1000 ÷ 150 = 0.046 m to do it; the polymer needs half that thickness ([RAD-04](RAD-04-beta-minus-source-cartridge.md) section 5).
- **Alpha.** e^(−400 × 0.05) = e^−20: ends.
- **Neutrons.** e^(−2 × 0.05) = e^−0.1 = 0.905 for both groups, and no conversion: polymer does not moderate. Only the Liquid row declares a conversion (radiation convention 5), so the moderation lesson has one cause.
- **Body.** Centre of mass 0.327 m over a 0.30 m foot: tips beyond atan(0.15 ÷ 0.327) = 24.6°.

### Rows

Class `ShieldMaterialTests` (family), with the lead's fixture F and charged fixture. The family rows S1 to S4 and S8 to S11 of [EL-126 section 5](EL-126-thin-screen-shield.md#5-acceptance) run for this member with the values above (S1: 0.327 m; 0.130, 0.0558, 0.0789 kg·m²). Its own rows, all in this slice:

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| P1 | Given fixture F at 1 m with one polymer slab the reading is 0.759 W; with one dense slab in the same place it is 0.324 W; the polymer reading is more than twice the dense one (row: cannot inherit dense-shield performance) | `ShieldMaterialTests.PolymerIsNotADenseShield` | open path 0.882 W | ±3 % each; ratio above 2 (hand value 2.34) |
| P2 | Given one and two polymer slabs, the meter's level stays on (0.759, 0.653 W) | `ShieldMaterialTests.PolymerWeakensButDoesNotSilenceGamma` | two dense slabs: off (0.119 W) | ±3 % |
| P3 | Given the lead's Beta-minus charged fixture with the meter 1 m away and one polymer slab on the path, the strength is below 0.001 W and the level off; a thin screen in its place leaves 0.670 W and the level on (RAD-04's row; the form with the real parts is RAD-04 B7) | `ShieldMaterialTests.PolymerStopsBetaWhereAThinScreenPasses` | no slab: 1 W, on | below 0.001 W; ±3 % on 0.670 W |
| P4 | Given a law-scene NeutronFast source of 10 W/sr, paid by a declared steady inventory of 37,699 J (the Neutron module's, radiation convention 14), 1 m from a law-scene standard receiver weighted NeutronSlow only, with the slab between, the slow reading is 0 | `ShieldMaterialTests.PolymerDoesNotModerate` | the same receiver weighted NeutronFast reads 0.882 × 0.905 = 0.798 W | below 0.001 W; ±3 % |

## 6. Open questions

None open.

1. **Does polymer moderate — decided now**: no. The research names only water as the moderator, and one cause keeps lessons 126 to 128 readable.
2. **Coefficients — decided** (D1, D16; coordinator decision 21, 22 November 2026, for BetaMinus 300): the Polymer row.
3. **Heating — decided** (D19): as the lead, not part of this element; absorbed power is ledgered (radiation convention 10).
4. **Thickness presets — decided** (D13): none; stacking.

Needs the owner: none.

## 7. Integration

- **Collaborators.** RAD-01, RAD-08, EL-129 (undelivered). RAD-04 meets the polymer in its own row B7. No port.
- **Cross-element task.** [sequence-task-413](../requirements.md#sequence-task-413): family rows S2 to S4 and S9, and P1.
- **Campaign.** First use 101–110 with the shields; beta suppression taught at 116; reuse 111–120, 124–130, 136–150.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7, for the lead's reasons: the ledger entry for element-127 carries no `todo-N` link and S611 has no anchor in `docs/planning/invest/scope-corrections.md` (checked by search).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

Delivered in the Shield materials Family slice; prerequisites (blocked on the engine programme (owner decision 3a), with ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01 and RAD-08; S7; no dependency on ENGINE-THERMAL), review tier and disqualifiers as [EL-126 section 8](EL-126-thin-screen-shield.md#8-delivery). Files: `elements/PolymerShield/PolymerShield.csproj`, `PolymerShieldElementDeclaration.cs`, `polymer_shield.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; theory cases in `ShieldMaterialTests`; a row in `VisibleSolidColliderTests`. Own disqualifier: a sibling whose declaration differs in anything but values (then the family is not a family and the slices split).

## 9. Engine conformance

Verdict: **Conforms**, on the seven points of [EL-126 section 9](EL-126-thin-screen-shield.md#9-engine-conformance), which hold word for word: one dynamic body, two box colliders, a declared row, no binding, no port, counts fixed at compile time, a resting panel stepped every tick until ENGINE-ISLANDS-SLEEP (owner decision 3a) delivers island sleeping. "Cannot inherit from its category" is met structurally: the coefficients are this package's declared data and the engine has no material or category lookup.
