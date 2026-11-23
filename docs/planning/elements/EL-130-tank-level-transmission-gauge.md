# EL-130 · Tank-level transmission gauge — element readiness spec

**Current delivery (24 October 2026):** [EL-130 delivery slice](../invest/named-elements.md#element-130). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`, findings H-1, C-1, M-6, X-1 to X-4 and the Low rounding note) and owner decision 18 and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [element-130](../requirements.md#element-130). A toy inspection instrument for fictional toy sources; never real-world level-gauging guidance. The shared declaration, probe, family rows and conformance are in the family lead, [EL-129](EL-129-sheet-thickness-transmission-gauge.md); the radiation law is in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions) and the liquid terms in the [liquid conventions](EL-001-finite-reservoir.md#liquid-family-conventions-stated-once-here). This spec states the level gauge's own values, calculations and rows.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-130 · Tank-level transmission gauge |
| Type | Radiation |
| Anchor | [requirements.md#element-130](../requirements.md#element-130); [named-elements.md#element-130](../invest/named-elements.md#element-130); scope source [radiation-11](../requirements.md#radiation-11) (umbrella, index only) |
| Owner | S619 |
| Disposition | Member of the Family slice **Transmission gauges** with [EL-129](EL-129-sheet-thickness-transmission-gauge.md) (lead). It differs only in declared values: the parameter is `level_threshold` with default 0.68 W, and the dial print, id, title and description. Not a duplicate (D8). The two parameter ids differ on purpose: each names what the player calibrates ("thin" against "level"). |
| Planned package | id `tank_level_gauge`, directory `elements/TankLevelGauge/`, class `TankLevelGaugeElementDeclaration` |
| Related identities | Sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); vessels [RAD-17](RAD-17-water-moderator-tank.md), [EL-001](EL-001-finite-reservoir.md), [EL-026](EL-026-communicating-tank.md); [EL-003 Tap](EL-003-tap.md); controlled tap [EL-167](EL-167-solenoid-tap.md). No CAT part is refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL, ENGINE-LIQUID (after ENGINE-GAS-NETWORK, owner decision 18) and ENGINE-RADIATION |
| Run length needed | 30 s (the default); the longest row (L2) fills and drains in under 10 s |

## 2. Declaration

The declaration is [EL-129's](EL-129-sheet-thickness-transmission-gauge.md#2-declaration): the same body, four colliders, two ports, one receiver, two threshold nodes over its committed strength (`valid` fixed at 0.05 W with release 0.75; `clear` at the parameter with release 0.97), one two-input boolean signal node `window` = `valid` Xor `clear` (equal to "`valid` and not `clear`" because `clear` implies `valid` while the parameter's minimum, 0.06 W, is at or above the 0.05 W mark; derivation in EL-129), one contact `relay` with `SignalStateContactControl` naming `window` (`PowerIn` → `relay` → `Supply`), one electrical node `relay_switch` (`ElectricalRole.Switch`, 12 V, 0.05 Ω, capacitance 0, maximum 100 W) and the same bindings. Its values:

| Item | Value |
| --- | --- |
| Parameters | `level_threshold`: Real, Watt, default 0.68, minimum 0.06, maximum 2.0, editor policy Clamp, build only (D11) |
| Catalogue | id `tank_level_gauge`, "Level gauge", `ElementCategory.Radiation` **(field)**, palette order 723 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Looks through a tank at a source. Its contact passes electricity when the liquid has risen to the gauge, and never when it sees no source." |

The gauge has no liquid port and no knowledge of a vessel. It is placed beside a vessel with a source on the far side; its height is the level it watches. `Supply` is live when the reading is valid and below the threshold, which is when liquid covers the path.

### Art and solidity

As the lead, with a wave line printed on the dial in place of the sheet-and-arrow, and the zones printed as a cross (invalid), a filled drop (at level) and an empty drop (below level). Every solid art node has the lead's collider.

### Compiled counts

As the lead: 1 static body, 4 colliders, 3 signal nodes (two threshold, one boolean), 1 supply node, 1 electrical node, 1 receiver (nine samples), 7 bindings.

## 3. Engine capabilities

The family table ([EL-129 section 3](EL-129-sheet-thickness-transmission-gauge.md#3-engine-capabilities)) applies, with one more missing row particular to this member. **5 present, 0 untested, 2 narrower, 5 missing.**

| # | Capability | Status | Owner |
| --- | --- | --- | --- |
| M5 | A liquid store's committed filled region crossed by a path as a volume with the liquid's attenuation row | **missing**: no liquid inventory exists (`CuriousContraptions.Abstractions.Fluid/FluidContracts.cs:5` declares a port contract that no solver consumes) and no path query reads one | **ENGINE-RADIATION**, after ENGINE-LIQUID (radiation convention 4). The liquid store, its surface and its conservation are ENGINE-LIQUID's and belong to the vessel's package, not to this one. |

**Dependencies.** Battery (delivered); RAD-08 (shape); RAD-01 (source); the RAD-17 tank (every row's vessel), the EL-001 reservoir (row L2's feed) and the EL-003 Tap (all undelivered, after ENGINE-LIQUID).

## 4. Sources and legacy

- [element-130](../requirements.md#element-130) and the [radiation-11](../requirements.md#radiation-11) integration task: "a rising conserved waterline changes the level contact. Missing source is invalid, not a valid heavy/full reading."
- [Named entry](../invest/named-elements.md#element-130), owner S619; map row; binding `docs/coverage/engine/element-02.json` (proof S619, relation radiation-11).
- [S605](../invest/decisions.md#s605) photon row; research "Inspection" law and slot 89 "The Hidden Waterline" (below-path water and a misaligned reference do not report a valid full state).
- **Legacy.** None. Hits are coverage snapshots only: older radiation-11 binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1964-L2008`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

Starting values (D1). Reference arrangement: a Gamma capsule (10 W/sr, PhotonMedium) 1 m from the face on its axis, with a [Moderator tank](RAD-17-water-moderator-tank.md) centred between them: two Window walls 0.02 m thick (PhotonMedium 0.5 /m) and a 0.50 m interior of plan area 0.25 m² and height 0.80 m, its mouth joined to a closed Tap. The Liquid row's PhotonMedium coefficient is 1.2 /m (convention 4). The gauge's face centre is at height h = 0.40 m above the tank floor, at least 0.15 m below the tank's 0.80 m rim; its three sample rows are at h − 0.10, h and h + 0.10 m.

- **Empty.** Two walls: 0.882 × e^(−0.5 × 0.02) × e^(−0.5 × 0.02) = 0.882 × 0.990 × 0.990 = 0.864 W. Above the 0.68 W mark by 27 %: `clear` on, `Supply` dead.
- **Full (surface above h + 0.10).** Wet chord 0.50 m in every sample path: e^(−1.2 × 0.5) = e^−0.6 = 0.549. Reading 0.864 × 0.549 = 0.474 W, 28 % under the release 0.97 × 0.68 = 0.660 W: `clear` off, `valid` on, `Supply` live.
- **Rising surface.** Surface between h − 0.10 and h: bottom row wet: (6 + 3 × 0.549) ÷ 9 = 0.850, reading 0.864 × 0.850 = 0.734 W, 7.9 % above the mark: still clear, dead. Surface between h and h + 0.10: two rows wet: (3 + 6 × 0.549) ÷ 9 = 0.699, reading 0.864 × 0.699 = 0.604 W, 8.4 % below the release 0.660 W: live. So the contact changes as the surface passes the gauge's centre height h.
- **Why 0.68 W.** The two neighbouring steps are 0.734 and 0.604 W. A mark T must satisfy 0.734 ≥ T and 0.604 < 0.97 T; equal margins on both sides give T² = 0.734 × 0.604 ÷ 0.97 = 0.457, T = 0.676, rounded to 0.68. Three sample rows cannot give a margin wider than about 8 %; it is more than twice the ±3 % reading tolerance.
- **Falling surface.** Draining back below h returns 0.734 W ≥ 0.68: dead again. Ripple at one row moves the reading between neighbouring steps that lie on the same side of the band, so it cannot chatter the contact.
- **Filling and draining times.** A Tap at opening 1 passes 0.0482 m³/s ([EL-003](EL-003-tap.md) flow table), raising the surface 0.0482 ÷ 0.25 = 0.19 m/s, so a fill from empty to h + 0.10 with h 0.40 m above the floor takes 0.50 ÷ 0.19 = 2.6 s. With the mouth's Tap opened the tank drains as [RAD-17](RAD-17-water-moderator-tank.md) states (about 5 s). Every row fits the 30 s default.
- **No source.** Reading 0: invalid, dead, whatever the fill. A full tank with no source is never "full".
- **Follows the material path.** The reading depends only on what the paths cross: the walls' row and length, the liquid's row and wet length. A wider vessel attenuates more: a 1.0 m wet chord gives e^−1.2 = 0.301, and the threshold is set lower to suit.
- **Conservation.** The gauge moves no liquid; the vessel's ledger (liquid conventions L10) is unchanged by its presence.
- **Tick order** (convention 11). The solve of tick n runs after the last substep and reads this tick's final liquid levels and poses; the levels and the contact follow in the signal network of tick n + 1, as the lead.

### Rows

Class `TransmissionGaugeTests` (family). The family rows G2, G5 and G7 to G9 of [EL-129 section 5](EL-129-sheet-thickness-transmission-gauge.md#5-acceptance) run for this member. Its own rows all run in the slice that delivers it (section 8); none is queued. Readings ±3 %.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| L1 | Given the reference arrangement with the tank empty, the strength is 0.864 W and `Supply` is dead; full, it is 0.474 W and `Supply` is live (row: empty and full readings follow the material path) | `TransmissionGaugeTests.EmptyAndFullFollowTheMaterialPath` | L3 | ±3 % |
| L2 | Given the tank filling from a Tap at opening 1 on an [EL-001](EL-001-finite-reservoir.md) reservoir pouring into its top, with its mouth joined to a closed Tap, `Supply` is dead while the committed surface is at or below h − 0.02 m and live once it is at or above h + 0.02 m, with one transition; opening the mouth's Tap drains it and reverses that with one transition; the tank's volume plus the world accounts stay equal to the supplied volume throughout (task 419: a rising conserved waterline changes the level contact) | `TransmissionGaugeTests.ARisingConservedWaterlineSwitchesAtTheGaugeHeight` | L1 | depth 0.02 m (twice the liquid depth tolerance L17); volume 0.004 m³; stall guard 10 s each way |
| L3 | Given the full tank and no source, or the capsule behind four Dense slabs so the reading is 0.474 × e^−4 = 0.0087 W, `Supply` is dead (row: disconnected source is invalid) | family row G2 for this member, `TransmissionGaugeTests.AMissingSourceIsNeverThickOrFull` | L1 | `valid` level off |
| L4 | Given a law-scene volume with the Liquid row whose wet chord (0.50 m) over the nine samples steps none → bottom row → two rows → all, the strengths are 0.882, 0.749, 0.617 and 0.484 W (0.882 × 1, × 0.8496, × 0.6992, × 0.5488); with `level_threshold` 0.70 W (release 0.679 W: 7.0 % and 9.1 % clear of the neighbouring steps, because this scene has no vessel walls) `Supply` goes live at the two-row step | `TransmissionGaugeTests.TheWetShareOfTheApertureSetsTheReading` | L1 | ±3 % |
| L5 | Given the tank filled to h + 0.05 m (0.45 m above the floor), `Supply` is live (two rows wet, 0.604 W); with the gauge and the capsule both raised 0.25 m (face centre 0.65 m above the floor, 0.15 m below the rim; samples at 0.55 to 0.75 m, all above the surface and all through both walls, so the reading is the empty 0.864 W) `Supply` is dead: the gauge's height is the level it reports. Raising the gauge alone would put the samples off the capsule's axis and change the reading for another reason | `TransmissionGaugeTests.TheGaugeHeightIsTheWatchedLevel` | L2 | ±3 % |

## 6. Open questions

None open.

1. **Binary level or continuous fill estimate — decided** (D19): a level contact, which is what the integration task names; the needle shows the reading continuously.
2. **Pairing rule — decided now**: none, as the lead: the gauge reads what reaches its face.
3. **Ratios and law — decided** (D1, D16): one absolute threshold in watts with release ratio 0.97; no stored empty-vessel reference. Default 0.68 W chosen for equal margins on the reference tank (section 5).

Needs the owner: none.

## 7. Integration

- **Collaborators.** Battery, Powered gate (delivered); RAD-01, RAD-17, EL-001, EL-003, EL-026, EL-167 (undelivered). Port domains agree: `PowerIn` Electrical Input, `Supply` Electrical Output; a solenoid tap's `PowerIn` takes the gauge's `Supply` to stop a fill at the level.
- **Cross-element task.** [sequence-task-419](../requirements.md#sequence-task-419): rows L1 to L3. Processes [IX-15](../requirements.md#interaction-15), [IX-08](../requirements.md#interaction-08), [IX-06](../requirements.md#interaction-06), [IX-07](../requirements.md#interaction-07).
- **Campaign.** Chapter 13, first use **128**, after the Water moderator tank (RAD-17, 126) that every row reads (coordinator decision 36; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)); reuse 129–130, 136–150. A level that teaches it joins the tank's mouth to a closed Tap, or the tank drains in about 5 s.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7, for the lead's reasons: the ledger entry for element-130 carries no `todo-N` link and S619 has no anchor in `docs/planning/invest/scope-corrections.md` (checked by search).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22); a level that pre-joins a Tap to the watched tank needs what S8 delivers.

## 8. Delivery

- **Slice shape.** Delivered in the Transmission gauges Family slice only once ENGINE-LIQUID, RAD-17, EL-001 and the EL-003 Tap are delivered, so that rows L1 to L5 run in the slice; otherwise EL-129 ships alone and this package follows in its own slice with the same shape. A requirement row of this identity is never queued out of the slice that delivers it.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES; ENGINE-GAS-NETWORK, then ENGINE-LIQUID (owner decision 18); ENGINE-OPTICAL; ENGINE-RADIATION as amended, including its liquid crossing; RAD-01, RAD-08, EL-129; RAD-17, EL-001 and EL-003; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names none).
- **Files.** `elements/TankLevelGauge/TankLevelGauge.csproj`, `TankLevelGaugeElementDeclaration.cs`, `tank_level_gauge.tscn`, `icon.svg`; one guarded line in `CuriousContraptions.slnx`; theory cases and rows L1 to L5 in `TransmissionGaugeTests`; a row in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** As the lead: data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** The lead's; ENGINE-RADIATION not crossing liquid volumes (then the gauge cannot see a level and waits); a liquid model without a flat committed surface per store (re-derive the row steps); vessel walls delivered with a row or thickness that puts the empty reading under the threshold (set the default from the delivered vessel).

## 9. Engine conformance

Verdict: **Conforms**, on the seven points of [EL-129 section 9](EL-129-sheet-thickness-transmission-gauge.md#9-engine-conformance), which hold unchanged: the package is the same data with other values. The level is seen by the general rule that a path crosses a liquid store's committed volume; the gauge names no vessel and no liquid, and the extra missing capability (M5) has one owner, ENGINE-RADIATION.
