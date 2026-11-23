# RAD-04 · Beta-minus source cartridge — element readiness spec

**Current delivery (24 October 2026):** [RAD-04 delivery slice](../invest/named-elements.md#radiation-04). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and owner decisions 13 and 17 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the checkout. Requirement row: [radiation-04](../requirements.md#radiation-04) (P2 potential). Lengths are metres; local +X is the beam direction. A fictional sealed toy cartridge; never real-world source guidance. Units and laws are the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions). The shared shape, probe table and conformance points are in the family lead, [RAD-03](RAD-03-alpha-source-cartridge.md); this spec states its own values, calculations and rows.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-04 · Beta-minus source cartridge |
| Type | Radiation (charged-particle source) |
| Anchor | [requirements.md#radiation-04](../requirements.md#radiation-04); [named-elements.md#radiation-04](../invest/named-elements.md#radiation-04) |
| Owner | S621 |
| Disposition | To deliver as a member of the Family slice **Charged cartridges**, led by [RAD-03](RAD-03-alpha-source-cartridge.md). It differs from the lead only in declared values: channel BetaMinus, range 3.0 m, two printed dots, id, title, description and icon. It is a distinct inventory item with its own catalogue entry (D8), as the requirement asks. |
| Planned package | id `beta_minus_source_cartridge`, directory `elements/BetaMinusSourceCartridge/`, class `BetaMinusSourceCartridgeElementDeclaration` |
| Related identities | Sibling [RAD-03](RAD-03-alpha-source-cartridge.md); receiver [RAD-08](RAD-08-radiation-rate-meter.md) with `sensitivity` Beta-minus; [EL-126 Thin screen](EL-126-thin-screen-shield.md) passes it, [EL-127 Polymer](EL-127-polymer-shield.md) suppresses it; opposite bending with [RAD-14](RAD-14-magnetic-deflector.md). No CAT spec. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); the longest row is the shared 25 s theory A9 |

## 2. Declaration

The declaration is [RAD-03's](RAD-03-alpha-source-cartridge.md#2-declaration) with the values below changed; every other row is the same.

| Item | Value |
| --- | --- |
| Interfaces, body, colliders, ports, parameters, stores, footprint | As RAD-03: one static body, cartridge box 0.25 × 0.30 × 0.25 m at the origin, base box 0.35 × 0.06 × 0.35 m at (0, −0.18, 0), both with the scene's default row, no port, no parameter, one steady finite inventory (half-life none) of 1 W × 300 s = 300 J, restored on Reset. |
| Source **(field)** | Frame body 0; origin (0.125, 0, 0); axis +X; half-angle 0 (collimated); range 3.0 m; radiant power 1 W in BetaMinus, paid by the 300 J inventory; internal occluders = both colliders. |
| Presentation bindings | None during Run; the Build preview (ENGINE-OPTICAL's, radiation convention 13) draws the beam to 3.0 m. |
| Catalogue | Id `beta_minus_source_cartridge`, "Beta-minus cartridge", `ElementCategory.Radiation` **(field)**, palette order 713 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Sends a thin beam of beta particles from its ring. It passes a thin screen, a polymer shield stops it, and a magnet bends it the other way from alpha." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Cartridge` | box 0.25 × 0.30 × 0.25 m, cream `#fff8e9` | solid (cartridge box) | nothing |
| `Visual/Base` | box 0.35 × 0.06 × 0.35 m at y −0.18, navy `#293954` | solid (base box) | nothing |
| `Visual/OutletRing` | ring r 0.04 m printed flush on the +X face, navy | decorative: printed flat | nothing |
| `Visual/Mark` | "β⁻" and two dots printed flat on the top face, gold `#f7cb52` | decorative: printed flat | nothing |

Icon: a small box with a ring and two dots. It differs from the Alpha cartridge by the dot count (two against one).

### Compiled counts

As RAD-03: bodies 1 (static), colliders 2 (2 boxes), sources 1 (collimated), inventories 1 (steady, 300 J), everything else 0.

## 3. Engine capabilities

The probe is [RAD-03 section 3](RAD-03-alpha-source-cartridge.md#3-engine-capabilities), row for row: 3 present (R1 static body, R2 visible solids, R3 lifecycle), 0 untested, 1 narrower (N1 line of sight, owner ENGINE-SPATIAL-QUERIES, a story of owner decision 3a), 4 missing (M1 collimated emitter and shared solve, owner ENGINE-OPTICAL; M2 radiation channels, attenuated continuation with the default row, and a steady finite inventory, owner ENGINE-RADIATION; M3 category and domain flag, owner ENGINE-RADIATION; M4 Build preview, owner ENGINE-OPTICAL).

This identity adds no capability. Its queued bending row B5 reads the Magnetic deflector's relay, whose outlet masks list Alpha and BetaMinus and whose enable names the deflector's `energised` level; that capability is declared, probed and counted by [RAD-14](RAD-14-magnetic-deflector.md), not by this package. Counted for this identity: 3 present, 0 untested, 1 narrower, 4 missing, the same as its family lead, so the family needs no capability one member lacks.

**Dependencies.** RAD-01 and the family lead. Rows read a law-scene receiver until RAD-08 exists; screen rows use law-scene boxes with the Thin screen and Polymer rows until EL-126 and EL-127 exist. The bending row waits for RAD-14.

## 4. Sources and legacy

- [radiation-04](../requirements.md#radiation-04): "A taught thin screen passes enough beta to detect while a polymer screen suppresses it; opposite bending is proven with the deflector."
- [Named entry](../invest/named-elements.md#radiation-04), owner S621; map row; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) beta-minus row; research row ("passes paper, stopped by polymer, bent by a field"; slots 91 "Beyond the Paper", 94 "Opposite Ways").
- **Legacy.** None. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1721-L1751`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

- **Beam.** A zero-width ray of 1 W BetaMinus along +X; a receiver it ends on reads 1 W inside 3.0 m and 0 beyond. The range is half as long again as alpha's 2.0 m: a different range response, and room for a screen plus a deflector on one path. Every collider on the ray is crossed with surface transmittance 1 and multiplied by exp(−μ × chord) of its row (radiation convention 4).
- **Thin screen.** 0.02 m at 20 /m: 1 × e^−0.4 = 0.670 W. That is above the 0.2 W mark, so "enough beta to detect" passes. The same screen leaves alpha 0.000335 W.
- **Polymer.** 0.05 m at 300 /m: 1 × e^(−300 × 0.05) = e^−15 = 3.1 × 10⁻⁷ W, below the 0.001 W floor: the segment ends in the slab, within ln 1000 ÷ 300 = 0.023 m of its entry.
- **Default row.** BetaMinus 150 /m: a 0.05 m part with the default row also stops beta (e^−7.5 = 0.00055 W, below the floor), but needs ln 1000 ÷ 150 = 0.046 m to do it; the Polymer shield needs half that thickness, so it is the better beta shield.
- **Two thin screens.** 0.670² = 0.449 W, still detected; four give 0.670⁴ = 0.202 W, on the mark, and five 0.670⁵ = 0.135 W, below it. Stacking attenuates monotonically.
- **Dense.** 0.05 m at 2000 /m ends the beam within ln 1000 ÷ 2000 = 0.0035 m.
- **Opposite bending.** With a supplied deflector (its relay enabled by its `energised` level, RAD-14) in Forward polarity the beta beam leaves the exit pane 0.084 m to one side, turned 23.6°, at 0.8 W; alpha leaves 0.054 m to the other side, turned 15.5°. One metre beyond the exit pane along the chamber axis the beta beam is 0.084 + tan 23.58° = 0.084 + 0.436 = 0.520 m off axis; alpha is 0.054 + tan 15.47° = 0.054 + 0.277 = 0.331 m off axis on the other side. Range check for beta: 0.5 m to the inlet + 0.41 m internal path + 1 ÷ cos 23.58° = 1.091 m = 2.0 m, inside 3.0 m.
- **Constant strength, paid.** 1 W from a 300 J inventory, debited 1 ÷ 120 = 0.00833 J each tick and restored on Reset, exactly as [RAD-03](RAD-03-alpha-source-cartridge.md#5-acceptance); [IX-16](../requirements.md#interaction-16) holds as written. Own housing, aim, energy and tick order are as RAD-03.

### Rows

Planned class `ChargedCartridgeTests`, shared with RAD-03. Its theories A1 (whole power on a compatible receiver), A4 (a photon receiver rejects the beam), A5 (own housing), A6 (no width), A7 (Window pane 0.905 W), A8 (facing) and A9 (constant strength over 25 s with a 25 J debit, Reset to 300 J, no port, save and load) run for this package with BetaMinus weights. The rows below are this identity's own. Readings ±3 %; stall guard 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| B1 | Given a BetaMinus receiver at 2.85 m it reads 1 W; at 3.15 m it reads 0 | `ChargedCartridgeTests.TheBeamEndsAtItsDeclaredRange` (theory row for this package) | lead's A1 | 0.15 m either side of 3.0 m |
| B2 | Given a law-scene box 0.02 m thick with the Thin screen row on the axis, then the receiver reads 0.670 W, above the standard 0.2 W mark (row: a thin screen passes enough beta to detect; the real meter's level is B7) | `ChargedCartridgeTests.AThinScreenPassesBeta` | the Alpha cartridge behind the same box reads 0 | ±3 % |
| B3 | Given a law-scene box 0.05 m thick with the Polymer row on the axis, then the receiver reads 0 (3.1 × 10⁻⁷ W) (row: a polymer screen suppresses it) | `ChargedCartridgeTests.APolymerShieldStopsBeta` | B2 | below 0.001 W |
| B4 | Given one, two, four and five thin boxes, then the readings are 0.670, 0.449, 0.202 and 0.135 W, each lower than the last | `ChargedCartridgeTests.StackedScreensAttenuateMonotonically` | B2 | ±3 % each; strict order (each step is at least 33 % lower) |
| B5 | Given a supplied Magnetic deflector in Forward polarity and receivers 1 m beyond its exit pane at 0.520 m to the beta side and 0.331 m to the alpha side, then the beta beam reads 0.8 W on the first and 0 on the second; an Alpha cartridge in its place reads the reverse; Reverse polarity swaps both (row: opposite bending is proven with the deflector) | `ChargedCartridgeTests.BetaAndAlphaBendOppositeWays` (queued until RAD-14 is delivered) | coil unsupplied: both go straight | ±3 %; receiver centres within 0.05 m of the calculated points |
| B6 | Given the two cartridges in one palette, then they are two entries with different ids, titles and icons, and each emits only its own channel | `ChargedCartridgeTests.TheTwoCartridgesAreDistinctItems` | — | exact ids |
| B7 | Given a supplied Rate meter with `sensitivity` Beta-minus behind an EL-126 screen its load is supplied; behind an EL-127 shield it is not | `ChargedCartridgeTests.TheRealMeterAndShieldsAgree` (queued until RAD-08, EL-126 and EL-127 are delivered) | B2, B3 | load state |

There is no parameter, so no parameter-refusal row. Chrome recipe (pending, post-catalogue): cartridge aimed at a Beta-minus-set Rate meter 1 m away with a load; put a Thin screen between, then swap it for a Polymer shield.

## 6. Open questions

None open.

1. **Range, attenuation and curvature values — decided** (D1, D16): 3.0 m, the rows of the conventions (Polymer BetaMinus 300 /m, coordinator decision 21, 22 November 2026), and the deflector's declared outlets.
2. **A beta spectrum with a tapered range — decided** (D19): no. One hard range; the gradual part of the lesson is the exponential loss in screens (0.670 per thin screen), which a player can stack and read.
3. **Payment — decided by the owner** (21 November 2026, and coordinator decision 21, 22 November 2026): constant strength from a finite inventory of 300 J, restored on Reset.

Needs the owner: none.

## 7. Integration

- **Collaborators.** RAD-08, EL-126, EL-127, RAD-14, RAD-15 (all undelivered). No port, so no port pair needs to agree.
- **Cross-element tasks.** Source in [sequence-task-413](../requirements.md#sequence-task-413) (thin screen passes, polymer suppresses) and in the RAD-14 and RAD-15 routing proofs; processes [IX-15](../requirements.md#interaction-15) and [IX-16](../requirements.md#interaction-16) (steady inventory; holds as written).
- **Campaign.** Chapter 12, first use 116, then 119; reuse 121–125, 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader admits only an `EL-NNN` identity with an `element-NNN` anchor (`tools/Coverage/NamedPackageSources.cs:19`, `:57` on `origin/main`), so a `RAD-NN` identity needs S7 to widen it (owner decision 15).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Member of the Family slice "Charged cartridges" (two packages, one review, one candidate commit).
- **Prerequisites, in order.** As RAD-03: blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01; S7 for the binding; no dependency on ENGINE-THERMAL. Row B5 is owned here and runs in the RAD-14 slice, which records it; B7 runs when its parts exist.
- **Files.** `elements/BetaMinusSourceCartridge/BetaMinusSourceCartridge.csproj`, `BetaMinusSourceCartridgeElementDeclaration.cs`, `beta_minus_source_cartridge.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; rows in `CuriousContraptions.tests/ChargedCartridgeTests.cs`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** As the family: data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** As RAD-03; also a Thin screen BetaMinus coefficient that leaves less than 0.2 W (the "passes enough to detect" row would fail and the coefficient is re-chosen in the conventions), or a Polymer BetaMinus coefficient that leaves 0.001 W or more through 0.05 m (re-chosen likewise).

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story). The seven points are [RAD-03's](RAD-03-alpha-source-cartridge.md#9-engine-conformance) with these values: one static body, two boxes, one collimated source row in BetaMinus, one steady inventory; the same generic laws (range, attenuation along a chord, the floor, the inventory debit); marks printed flat and no animation; one-way flow with enum channels and f32 powers; fixed counts; one segment cast per tick, since a pass-through collimated ray is one cast returning every crossing (radiation convention 12); rows B1 to B7 and the shared theories run the production simulation. "It bends the other way from alpha" is the deflector relay's per-channel outlet data, not a rule in this package.
