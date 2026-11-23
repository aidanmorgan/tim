# RAD-18 · Neutron absorber panel — element readiness spec

**Current delivery (24 October 2026):** [RAD-18 delivery slice](../invest/named-elements.md#radiation-18). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`, findings C-4, C-6, M-6, X-1 to X-4 and section 4 item 3) and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-18](../requirements.md#radiation-18) (P3 potential). A boron-inspired toy material; capture products are deliberately not modelled; never real-world shielding guidance. The panel has the body of the shield family, so the shared declaration, probe and conformance points are in [EL-126](EL-126-thin-screen-shield.md); the law and the rows are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions), cited by item.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-18 · Neutron absorber panel |
| Type | Radiation (passive neutron absorber) |
| Anchor | [requirements.md#radiation-18](../requirements.md#radiation-18); [named-elements.md#radiation-18](../invest/named-elements.md#radiation-18) |
| Owner | S628 |
| Disposition | **Its own element slice, delivered at level 127, its teaching level** (coordinator decision 37, 23 November 2026: a family member whose acceptance rows need a part the campaign teaches later is delivered as its own slice at its own teaching level; this panel's scene rows need RAD-16, EL-132 and RAD-17, taught at 125 and 126). It is split from the Family slice **Shield materials** ([EL-126](EL-126-thin-screen-shield.md) lead, [EL-127](EL-127-polymer-shield.md), [EL-128](EL-128-dense-shield.md); taught at 102 and 104), from which it differs only in declared values (mass, contact material, row, printed pattern, id, title), and reuses that family's law, fixture F and test class. Not a duplicate (D8). |
| Planned package | id `neutron_absorber_panel`, directory `elements/NeutronAbsorberPanel/`, class `NeutronAbsorberPanelElementDeclaration` |
| Related identities | Moderator [RAD-17](RAD-17-water-moderator-tank.md); source [RAD-16](RAD-16-neutron-source-module.md); detectors [EL-132](EL-132-slow-neutron-detector.md), [EL-131](EL-131-fast-neutron-detector.md); the photon shield that must not substitute, [EL-128](EL-128-dense-shield.md). No CAT part is refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION. The [radiation-18](../requirements.md#radiation-18) ledger row stays open until the scene rows N3 and N4 pass. |
| Run length needed | 30 s (the default); no row is longer than 2 s |

## 2. Declaration

The declaration is [EL-126's](EL-126-thin-screen-shield.md#2-declaration) with these values.

| Item | Value |
| --- | --- |
| Slab collider | box 0.05 × 0.80 × 0.80 m centred (0, 0.46, 0); foot as the lead |
| Mass and inertia | 1.5 kg, between polymer (1.0) and dense (4.0): a stiff composite board. Slab 1.034 kg, foot 0.466 kg (volumes 0.032 and 0.0144 m³); centre of mass at y = (1.034 × 0.46 + 0.466 × 0.03) ÷ 1.5 = 0.327 m. Principal moments about it, by the lead's rule: X = 1.034 × (0.80² + 0.80²) ÷ 12 + 0.466 × (0.06² + 0.80²) ÷ 12 + 1.034 × 0.133² + 0.466 × 0.297² = 0.1103 + 0.0250 + 0.0183 + 0.0411 = 0.195 kg·m²; Y = 1.034 × (0.05² + 0.80²) ÷ 12 + 0.466 × (0.30² + 0.80²) ÷ 12 = 0.0554 + 0.0283 = 0.0837 kg·m²; Z = 1.034 × (0.05² + 0.80²) ÷ 12 + 0.466 × (0.30² + 0.06²) ÷ 12 + 0.0594 = 0.0554 + 0.0036 + 0.0594 = 0.118 kg·m². Principal frame the identity. |
| Contact material | restitution 0.2, friction 0.5, rolling resistance 0 |
| Radiation row **(field)** | Absorber: 6, 2, 1, 400, 150, 2, 60 per metre (Soft, Medium, Hard, Alpha, BetaMinus, NeutronFast, NeutronSlow; radiation convention 4); no conversion. The NeutronSlow coefficient is the part; the rest make it an ordinary weak shield. |
| Ports, parameters, stores, bindings | None |
| Catalogue | id `neutron_absorber_panel`, "Neutron absorber", `ElementCategory.Radiation` **(field)**, palette order 722 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Absorbs slow neutrons. It does not slow fast ones, and what the capture gives off is not modelled." |

### Art and solidity

As the lead's table, with `Visual/Slab` 0.05 m thick in ochre `#e8b764`, `Visual/Pattern` a navy honeycomb printed flat on both faces, and `Visual/Mark` a gold "n" with a bar through it, printed flat. The honeycomb separates it from the grooved photon shields by pattern, not colour alone.

### Compiled counts

As the lead: 1 dynamic body, 2 box colliders, 2 radiation rows, nothing else.

## 3. Engine capabilities

The family table in [EL-126 section 3](EL-126-thin-screen-shield.md#3-engine-capabilities) applies unchanged: **4 present** (R1 to R4), **0 untested**, **1 narrower** (N1 line of sight, owner ENGINE-SPATIAL-QUERIES, proposed), **3 missing** (M1 the shared solve, ENGINE-OPTICAL; M2 rows, surface τ and chords, and M3 category and flag, ENGINE-RADIATION). No capability is particular to this member: a neutron channel is one of the seven coefficients of every row.

**Dependencies.** For the requirement's own scene (rows N3, N4, N6): RAD-16, RAD-17 (which needs ENGINE-GAS-NETWORK, ENGINE-LIQUID and a closed Tap, EL-003) and EL-132. Rows N1, N2 and N5 need none of them: they use law-scene sources and receivers, or the family's fixture F.

## 4. Sources and legacy

- [radiation-18](../requirements.md#radiation-18): "Adding it after the moderator suppresses the slow detector; removing it restores response. Dense photon shielding does not substitute for its neutron contract."
- [Named entry](../invest/named-elements.md#radiation-18), owner S628; map row (S605); binding `radiation-01.json`.
- Research "Neutrons" law ("boron-inspired panels absorb; no multiplication, activation or decay chains") and slots 102 "The Final Layer", 105 "Two Kinds of Shelter".
- **Legacy.** None. Hits are coverage snapshots only, e.g. the FiniteLedger bound-source list `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L15430-L15433`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance

### Behaviour and numbers

Starting values (D1); chord 0.05 m square to the path; transmission per crossing as [radiation convention 4](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Slow neutrons.** e^(−60 × 0.05) = e^−3 = 0.0498: the panel removes 95 % of slow flux.
- **Fast neutrons.** e^(−2 × 0.05) = e^−0.1 = 0.905, and no conversion: it neither stops nor slows fast flux. Absorption and moderation are separate causes; only the Liquid row declares a conversion (radiation convention 5).
- **Where the panel can stand.** Its foot is 0.30 m deep. RAD-17's reference layout (module and detector 1 m apart, the tank centred) leaves about 0.10 m of bench between the tank's base (to 0.35 m from the tank centre) and the detector's base (from 0.45 m), so the panel cannot stand there. **Layout A** is that layout with the detector moved out until its aperture face is 0.80 m from the tank centre: module to aperture 1.30 m, axis 0.45 m above the bench as in RAD-17; the panel's slab stands 0.55 m from the tank centre, its foot spanning 0.40 to 0.70 m, 0.05 m clear of the tank base and of the detector's base (from 0.75 m). **Layout B** moves the module back instead, its centre 1.05 m from the tank centre (base edge at 0.775 m) with the detector where RAD-17 puts it: module to aperture 1.55 m; the panel's slab stands 0.55 m before the tank centre, its foot spanning 0.40 to 0.70 m.
- **After the moderator (layout A).** Open nine-sample reading at 1.30 m: centre 10 ÷ 1.69 = 5.917 W/m², edges 10 × 0.99705 ÷ 1.70 = 5.865, corners 10 × 0.99413 ÷ 1.71 = 5.814, mean 5.848 W/m², R = 0.09 × 5.848 = 0.526 W. Through a full tank by the conversion law (radiation convention 5; [RAD-17](RAD-17-water-moderator-tank.md) section 5): slow 0.526 × 0.980 × 0.559 × 0.980 = 0.283 W, 41 % above the 0.2 W mark. With the panel between tank and detector: 0.283 × 0.0498 = 0.014 W, 91 % below the 0.15 W release, so the detector turns off. Removing the panel restores 0.283 W.
- **Dense does not substitute (layout A).** A dense slab in the panel's place leaves 0.283 × e^(−2.8 × 0.05) = 0.283 × 0.869 = 0.246 W: still on, 23 % above the mark.
- **Before the moderator it does little (layout B).** In front of the tank it meets only fast flux, so the slow reading keeps 0.905 of its no-panel value, against 0.0498 after the tank. Order matters, which is lesson 127. In layout B the no-panel slow reading is 0.372 × 0.537 = 0.200 W and with the panel 0.181 W, both inside the 0.15 to 0.2 W hysteresis band, so the row asserts the ratio, not the level.
- **Photons.** Medium e^(−2 × 0.05) = 0.905: a Gamma capsule 1 m from the standard meter reads 0.882 × 0.905 = 0.798 W. It is not a photon shield.
- **Model limit.** Absorbed neutron power is ledgered to the panel (radiation convention 10) and nothing else happens: no capture gamma, no secondary particle, no activation. The catalogue description and the selected-part card say so, as the row requires.

### Rows

Class `ShieldMaterialTests` (family). The family rows S1 to S4 and S8 to S11 of [EL-126 section 5](EL-126-thin-screen-shield.md#5-acceptance) run for this member (S1: 0.327 m; 0.195, 0.0837, 0.118 kg·m²). Law-scene neutron sources are 10 W/sr in one group, paid by a declared steady inventory of 37,699 J (the Neutron module's, radiation conventions 2 and 14), 1 m from a law-scene standard receiver weighted for one group. The scene rows use a full tank (`initial_volume` 0.20 m³) with its mouth joined to a closed Tap, as RAD-17's rows do. Its own rows:

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| N1 | Given a slow-group law-scene source and a slow receiver, the panel on the path leaves 0.882 × 0.0498 = 0.044 W; a fast-group source and fast receiver in the same scene leave 0.882 × 0.905 = 0.798 W | `ShieldMaterialTests.TheAbsorberRemovesSlowAndPassesFast` | open 0.882 W | ±3 % (slow: ±0.005 W; what matters is that it stays far under the 0.15 W release) |
| N2 | Given the fast source of N1 and a slow receiver behind the panel, the slow reading is 0 (it does not moderate) | `ShieldMaterialTests.TheAbsorberDoesNotModerate` | N1 | below 0.001 W |
| N3 | Given a Neutron module, a full Moderator tank and a Slow-neutron detector in layout A, when the panel stands between tank and detector the detector reads 0.0498 of its no-panel reading (0.014 W) and its level is off; when the panel is removed from the path it reads 0.283 W and is on (row: adding suppresses, removing restores) | `ShieldMaterialTests.TheAbsorberAfterTheModeratorSilencesTheSlowDetector` (queued until RAD-16, RAD-17 and EL-132) | no panel | ratio 0.0498 ± 0.005; ±3 % on 0.283 W; the levels hold in layout A only |
| N4 | Given the scene of N3 with a dense slab in the panel's place, the detector reads 0.869 of its no-panel reading (0.246 W) and stays on (row: dense does not substitute) | `ShieldMaterialTests.DenseAfterTheModeratorLeavesTheSlowDetectorOn` (the scene form of EL-128's row D5; queued as N3) | N3 | ±3 % on the ratio; level in layout A only |
| N5 | Given fixture F with a Gamma capsule at 1 m, the panel leaves 0.798 W and the photon meter stays on | family row S2 for this member | two dense slabs: off | ±3 % |
| N6 | Given a Neutron module, a full Moderator tank and a Slow-neutron detector in layout B with the panel in front of the tank, the slow reading is 0.905 of the no-panel reading in the same layout (against N3's 0.0498 after the tank) | `ShieldMaterialTests.AnAbsorberBeforeTheModeratorDoesLittle` (queued as N3) | N3 | ±3 % on the ratio; no level asserted |
| N7 | Given any row above, the power received never exceeds the open-path value and no reading appears in a channel the source lacks except through the tank's declared conversion | asserted inside N1 to N6 | — | never greater by more than 3 % |

Rows N1, N2 and N5 run in this slice and prove the row's coefficients. Rows N3, N4 and N6 run when RAD-16, RAD-17 and EL-132 are delivered; until N3 and N4 pass, the radiation-18 ledger row stays open.

## 6. Open questions

None open.

1. **Coefficients — decided** (D1, D16): the Absorber row.
2. **Thickness preset — decided** (D13): none; the row names none and stacking works as for every slab.
3. **Capture products — decided** (requirement row): not modelled, stated as a model limit above and in the description.
4. **Scene layout — decided now**: layouts A and B above, because the panel's foot does not fit in RAD-17's 1 m reference layout. The ratios of N3, N4 and N6 hold in any layout; the levels are asserted only where the margins of section 5 hold.

Needs the owner: none.

## 7. Integration

- **Collaborators.** RAD-16, RAD-17, EL-131, EL-132, EL-128, EL-003 Tap (all undelivered). No port.
- **Cross-element task.** Absorber control in [sequence-task-427](../requirements.md#sequence-task-427): rows N3, N4. Processes [IX-15](../requirements.md#interaction-15) and [IX-01](../requirements.md#interaction-01).
- **Campaign.** Chapter 13, first use 121–130 (lessons 127, 130); reuse 136–150. No level in `content/puzzles.json` places it. A lesson that puts the panel after the tank uses layout A's spacing or wider.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader lists only `EL-*.md` specs and admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity cannot be bound until S7 widens it.
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Its own element slice at level 127, after the Shield materials Family slice (EL-126, EL-127, EL-128) and after RAD-16, EL-132 and RAD-17 (coordinator decision 37); its cases join the family's theory in `ShieldMaterialTests`.
- **Prerequisites, in order.** As [EL-126 section 8](EL-126-thin-screen-shield.md#8-delivery): blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01 and RAD-08; S7 for the binding; no dependency on ENGINE-THERMAL. The scene rows N3, N4 and N6 run in this slice with N1, N2 and N5, after ENGINE-GAS-NETWORK, ENGINE-LIQUID, EL-003, RAD-16, EL-132 and RAD-17 (taught at 61 and 125 to 126, before the absorber at 127). This slice also owns the absorber controls of parts taught before it: RAD-17 W6 and EL-132 S4.
- **Files.** `elements/NeutronAbsorberPanel/NeutronAbsorberPanel.csproj`, `NeutronAbsorberPanelElementDeclaration.cs`, `neutron_absorber_panel.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; theory cases in `ShieldMaterialTests`; a row in `VisibleSolidColliderTests`.
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers.** A two-group neutron model that is not two channels of the one row (then the panel needs whatever replaces it; the slice stops and ENGINE-RADIATION is amended); the Shield materials Family slice not yet published (this slice reuses its law, fixture F and test class, so it stops until that slice publishes).

## 9. Engine conformance

Verdict: **Conforms**, on the seven points of [EL-126 section 9](EL-126-thin-screen-shield.md#9-engine-conformance), which hold word for word: one dynamic body, two box colliders, a declared row, no binding, no port, counts fixed at compile time, a resting panel stepped every tick until ENGINE-ISLANDS-SLEEP (owner decision 3a) delivers island sleeping. "Suppresses the slow detector" is e^−3 on one channel of a declared row; no rule names neutrons, boron or the moderator.
