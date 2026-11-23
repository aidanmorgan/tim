# EL-128 · Dense shield — element readiness spec

**Current delivery (24 October 2026):** [EL-128 delivery slice](../invest/named-elements.md#element-128). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`, findings C-1, C-4, C-6, C-7, C-12, X-1 to X-4 and section 4 item 3) and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [element-128](../requirements.md#element-128). A fictional toy material, never real-world shielding guidance. The shared body, probe and conformance points are in the family lead, [EL-126](EL-126-thin-screen-shield.md); the law and the rows are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions), cited by item. This spec states the dense slab's own values, calculations and rows.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-128 · Dense shield |
| Type | Radiation |
| Anchor | [requirements.md#element-128](../requirements.md#element-128); [named-elements.md#element-128](../invest/named-elements.md#element-128); scope source [radiation-05](../requirements.md#radiation-05) (umbrella, index only) |
| Owner | S612 |
| Disposition | Member of the Family slice **Shield materials** with [EL-126](EL-126-thin-screen-shield.md) (lead) and [EL-127](EL-127-polymer-shield.md). It differs from its siblings only in declared values. [RAD-18](RAD-18-neutron-absorber-panel.md) shares the family's law but is its own slice at level 127 (coordinator decision 37). Not a duplicate (D8). |
| Planned package | id `dense_shield`, directory `elements/DenseShield/`, class `DenseShieldElementDeclaration` |
| Related identities | Proving partners [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md), [RAD-08](RAD-08-radiation-rate-meter.md). The Dense row is also declared by [RAD-06](RAD-06-powered-radiation-shutter.md) (blade), [RAD-07](RAD-07-collimator-block.md) (walls) and [RAD-14](RAD-14-magnetic-deflector.md) (poles); each package declares its own copy of the values. No CAT part is refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); no row is longer than 2 s |

## 2. Declaration

The declaration is [EL-126's](EL-126-thin-screen-shield.md#2-declaration) with these values.

| Item | Value |
| --- | --- |
| Slab collider | box 0.05 × 0.80 × 0.80 m centred (0, 0.46, 0); foot as the lead |
| Mass and inertia | 4.0 kg, the Bowling ball's mass (`elements/BowlingBall/BowlingBallElementDeclaration.cs:54`), so a rolling Tennis ball (0.35 kg by default, `elements/TennisBall/TennisBallElementDeclaration.cs:16`) does not move it and a Bowling ball does. Slab 2.76 kg, foot 1.24 kg (volumes 0.032 and 0.0144 m³); centre of mass at y = (2.76 × 0.46 + 1.24 × 0.03) ÷ 4.0 = 0.327 m. Principal moments about it, by the lead's rule: X = 2.76 × (0.80² + 0.80²) ÷ 12 + 1.24 × (0.06² + 0.80²) ÷ 12 + 2.76 × 0.133² + 1.24 × 0.297² = 0.2944 + 0.0665 + 0.0491 + 0.1091 = 0.519 kg·m²; Y = 2.76 × (0.05² + 0.80²) ÷ 12 + 1.24 × (0.30² + 0.80²) ÷ 12 = 0.1478 + 0.0754 = 0.223 kg·m²; Z = 2.76 × (0.05² + 0.80²) ÷ 12 + 1.24 × (0.30² + 0.06²) ÷ 12 + 0.1582 = 0.1478 + 0.0097 + 0.1582 = 0.316 kg·m². Principal frame the identity. |
| Contact material | restitution 0.1, friction 0.6, rolling resistance 0 (a heavy, dead slab) |
| Radiation row **(field)** | Dense: 64, 20, 10, 2000, 2000, 1.6, 2.8 per metre (radiation convention 4); no conversion |
| Ports, parameters, stores, bindings | None (the old `ShieldThicknessPreset` is dropped, D13: a "thick" shield is two slabs) |
| Catalogue | id `dense_shield`, "Dense shield", `ElementCategory.Radiation` **(field)**, palette order 704 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "A heavy slab. The strongest shield against gamma and X-rays; poor against neutrons." |

### Art and solidity

As the lead's table, with `Visual/Slab` 0.05 m thick in navy `#293954` with a cream `#fff8e9` printed edge band and `Visual/Pattern` three printed groove lines. It differs from the polymer shield by three grooves against two and by the edge band.

### Compiled counts

As the lead: 1 dynamic body, 2 box colliders, 2 radiation rows, nothing else.

## 3. Engine capabilities

The family table in [EL-126 section 3](EL-126-thin-screen-shield.md#3-engine-capabilities) applies unchanged: **4 present** (R1 to R4), **0 untested**, **1 narrower** (N1 line of sight, owner ENGINE-SPATIAL-QUERIES), **3 missing** (M1 the shared solve, ENGINE-OPTICAL; M2 rows, surface τ and chords, and M3 category and flag, ENGINE-RADIATION). No capability is particular to this member.

**Dependencies.** RAD-01 and RAD-08; RAD-02 for the band row D4 (queued). The neutron control D5 uses law-scene sources and receivers; its scene form with the real parts is [RAD-18](RAD-18-neutron-absorber-panel.md) row N4.

## 4. Sources and legacy

- [element-128](../requirements.md#element-128) and the [radiation-05](../requirements.md#radiation-05) integration task.
- [Named entry](../invest/named-elements.md#element-128), owner S612; element map row; binding `docs/coverage/engine/element-02.json`, proof S612.
- [S605](../invest/decisions.md#s605) photon row; research lessons 77 "Behind the Slab", 79 "Two Thin Walls" (→ 102, 104), 105 "Two Kinds of Shelter" (dense-only fails the neutron condition).
- **Legacy.** No dense-shield part, coefficient, test or level in `parts/`, `engine/`, tests, `content/puzzles.json`, `tools/Campaign` or `reference/`. Hits are coverage snapshots only: the older radiation-05 binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1752-L1803`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

Starting values (D1); chord 0.05 m square to the path; transmission per crossing as [radiation convention 4](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Medium band (the Gamma capsule).** One slab: e^(−20 × 0.05) = e^−1 = 0.368. Two: e^−2 = 0.135. Three: e^−3 = 0.0498. A capsule 1 m from the standard meter reads 0.882 × 0.368 = 0.324 W behind one slab (above the 0.2 W mark: one is too little) and 0.882 × 0.135 = 0.119 W behind two (below the 0.15 W release: two are enough). That is lesson 104.
- **The strongest shield.** The Dense row has the largest photon coefficients of the conventions' rows, and no ordinary part rivals one slab. A part with no row takes the default row (PhotonMedium 0.15 /m, radiation convention 4): a Wall passes e^(−0.15 × 0.25) = 0.963 across and e^(−0.15 × 3) = 0.638 end-on, against one Dense slab's 0.368 and four slabs' e^−4 = 0.018. In fixture F at 1 m no default-row chord can exceed about 1 m, so an ordinary part leaves at least 0.882 × e^(−0.15 × 1) = 0.882 × 0.861 = 0.759 W, nearly four times the mark; a Wall across the path leaves 0.882 × e^−0.0375 = 0.849 W. Lesson 104 therefore needs the slabs.
- **Other bands.** Soft: e^(−64 × 0.05) = e^−3.2 = 0.0408, reading 0.882 × 0.0408 = 0.036 W. Hard: e^(−10 × 0.05) = e^−0.5 = 0.607, reading 0.535 W. A Hard X-ray gets through a slab that stops Soft and halves Medium: penetration follows the band, not the source's name.
- **Thickness never helps the radiation.** For any μ ≥ 0 and added chord ΔL ≥ 0, exp(−μ(L + ΔL)) ≤ exp(−μL); radiation convention 4 has no buildup or scatter term, so nothing returns power to the path.
- **Actual path length.** A slab turned 45° about Y has a chord of 0.05 × √2 = 0.0707 m: Medium e^(−20 × 0.0707) = e^−1.414 = 0.243, reading 0.882 × 0.243 = 0.214 W, lower than 0.324 W square-on.
- **Charged beams.** e^(−2000 × 0.05) = e^−100: both end.
- **Neutrons.** Fast: e^(−1.6 × 0.05) = e^−0.08 = 0.923. Slow: e^(−2.8 × 0.05) = e^−0.14 = 0.869. Behind a full moderator tank, whose slow flux is converted by the Liquid row's declared conversion (radiation convention 5), a dense slab keeps 0.869 of the slow reading and the absorber panel only e^(−60 × 0.05) = 0.0498 of it; in RAD-18's layout A that is 0.283 × 0.869 = 0.246 W, still on, against 0.014 W, off (RAD-18 rows N3 and N4). Dense does not substitute for the absorber.
- **Body.** 4 kg with the centre of mass at 0.327 m: tips beyond atan(0.15 ÷ 0.327) = 24.6°.

### Rows

Class `ShieldMaterialTests` (family), with the lead's fixture F (axis 0.70 m above the bench). The family rows S1 to S4 and S8 to S11 of [EL-126 section 5](EL-126-thin-screen-shield.md#5-acceptance) run for this member (S1: 0.327 m; 0.519, 0.223, 0.316 kg·m²). Its own rows:

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| D1 | Given fixture F at 1 m, one slab leaves the meter on (0.324 W) and two turn it off (0.119 W); a delivered Wall across the path in their place leaves it on (0.849 W) (description: the strongest shield; an ordinary part is not one) | `ShieldMaterialTests.OneDenseSlabIsTooLittleAndTwoAreEnough` | open path 0.882 W | ±3 % |
| D2 | Given the capsule at 1.4 m and zero to three slabs, each reading is at most the one before it and the ratios to open are 0.368, 0.135, 0.0498 (row: increasing thickness cannot increase passive transmitted energy) | family row S3 for this member, `ShieldMaterialTests.StackedSlabsAttenuateMonotonically` | no slab | ±3 % on ratios; never greater |
| D3 | Given one slab turned 45°, the reading is 0.214 W, below the square-on 0.324 W (row: actual path length) | `ShieldMaterialTests.AnObliqueDenseSlabAttenuatesMore` | D1 | ±3 % |
| D4 | Given an X-ray emitter at 1 m on Soft, Medium and Hard through one slab, the readings are 0.036, 0.324 and 0.535 W: off, on, on | `ShieldMaterialTests.PenetrationFollowsTheBand` (theory; queued until RAD-02 is delivered) | no slab | ±3 % |
| D5 | Given a law-scene NeutronSlow source of 10 W/sr, paid by a declared steady inventory of 37,699 J (radiation convention 2), 1 m from a law-scene standard receiver weighted NeutronSlow only, one dense slab leaves 0.882 × 0.869 = 0.766 W (level on), while the absorber panel in the same place leaves 0.882 × 0.0498 = 0.044 W (off) | `ShieldMaterialTests.DenseDoesNotSubstituteForTheAbsorber` | open 0.882 W | ±3 % |
| D6 | Given a Tennis ball rolled into the slab at 2 m/s the slab stays within 0.05 m of its pose and the reading is unchanged; a Bowling ball at 2 m/s moves it | `ShieldMaterialTests.MassDecidesWhatMovesTheSlab` | — | 0.05 m; ±3 % |

D4 is the only queued row; the requirement row's behaviour (thickness and actual path length) is proven in this slice by D1 to D3. In fixture F both balls roll below the lowest sample ray (0.60 m), so D6's reading is not changed by the ball.

## 6. Open questions

None open.

1. **Coefficients — decided** (D1, D16): the Dense row.
2. **Heating — decided** (D19): as the lead, not part of this element; absorbed power is ledgered (radiation convention 10).
3. **Thickness presets — decided** (D13): none; a thick shield is two slabs, which is the stacking lesson itself.
4. **An ordinary part as a shield — decided** (coordinator decision 21, 22 November 2026): the default row is the photon coefficients ÷ 10, so the Wall and other delivered parts are not shields.

Needs the owner: none.

## 7. Integration

- **Collaborators.** RAD-01, RAD-02, RAD-08, RAD-18 (undelivered); Tennis ball, Bowling ball, Wall (delivered). No port.
- **Cross-element tasks.** [sequence-task-413](../requirements.md#sequence-task-413): family rows S2 to S4 and S9, and D1, D2. Non-substituting control in [sequence-task-427](../requirements.md#sequence-task-427): row D5, with its scene form in RAD-18 N4.
- **Campaign.** First use 101–110 (lessons 102, 104); neutron control at 130; reuse 111–120, 124–130, 136–150.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7, for the lead's reasons: the ledger entry for element-128 carries no `todo-N` link and S612 has no anchor in `docs/planning/invest/scope-corrections.md` (checked by search).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

Delivered in the Shield materials Family slice; prerequisites (blocked on the engine programme (owner decision 3a), with ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01 and RAD-08; S7; no dependency on ENGINE-THERMAL), review tier and disqualifiers as [EL-126 section 8](EL-126-thin-screen-shield.md#8-delivery). D4 runs when RAD-02 is delivered. Files: `elements/DenseShield/DenseShield.csproj`, `DenseShieldElementDeclaration.cs`, `dense_shield.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; theory cases in `ShieldMaterialTests`; a row in `VisibleSolidColliderTests`. Own disqualifier: ENGINE-RADIATION departing from radiation convention 4 (for example by a buildup or scatter term); the conventions are amended first and D2 is re-derived.

## 9. Engine conformance

Verdict: **Conforms**, on the seven points of [EL-126 section 9](EL-126-thin-screen-shield.md#9-engine-conformance), which hold word for word: one dynamic body, two box colliders, a declared row, no binding, no port, counts fixed at compile time, a resting slab stepped every tick until ENGINE-ISLANDS-SLEEP (owner decision 3a) delivers island sleeping. The Dense values reused by the shutter, collimator and deflector are each that package's own declared data; no shared "dense" type exists in the engine.
