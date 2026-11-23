# EL-165 · Manual tap — named-identity spec

**Current delivery (24 October 2026):** [EL-165 delivery slice](../invest/named-elements.md#element-165). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-165 · Manual tap · Water |
| Anchors | [element-165](../requirements.md#element-165), co-anchored `sequence-task-718`; source record [campaign coverage](../requirements.md#campaign-element-coverage); ledger [element-165](../invest/named-elements.md#element-165), owner S458 |
| Disposition | **Duplicate closure of the Tap ([EL-003](EL-003-tap.md), planned package `tap`).** It gets no package, entry or slice of its own (D8); it is closed by the check below, run in the EL-003 slice, and the ledger row then cites that slice |
| Planned package | none. The satisfying package is `tap` |
| Status | Not started; closes with EL-003. Blocked, as EL-003 is, on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` (Delivery) |
| Related | [EL-003](EL-003-tap.md), [EL-166](EL-166-mechanically-actuated-tap.md), [EL-167](EL-167-solenoid-tap.md) |

## Declaration

None of its own. The requirement row reads: "Player-selected aperture remains fixed during Run", with the outcome "Closed aperture stops flow; opening cannot supply water without a connected reservoir."

The Tap, as specified in [EL-003](EL-003-tap.md), is exactly that:

| EL-165 asks for | EL-003 declares | Delta |
| --- | --- | --- |
| A player-selected aperture | `opening`, Real f32, 0 to 1, default 0, player-editable unless the level locks it (`EL-003-tap.md:33`) | none |
| Fixed during Run | `ParameterRunEdit.BuildOnly` (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:44`); the seat reads the authored parameter (L14 a); "The opening is fixed during a Run" (`EL-003-tap.md:35`) | none |
| A closed aperture stops flow | seat area `opening` × A₀; row T2 | none |
| Opening cannot supply water without a connected reservoir | a fitting with no wet source passes nothing; rows T4 and T5 | none |
| Body, mouths, material, art | one static Tube and bonnet, mouths `Start` and `End`, family vessel material, a printed lever that shows the opening | none |

No behaviour, parameter, port, mode, material or acceptance delta exists, so it is a duplicate and not a variant. The opening is continuous, because a Real parameter has no step (L2); the requirement asks for no steps.

**What would make it a variant instead.** Any one of: an opening the player can change while the machine runs (that would be `ParameterRunEdit.RunControl`, which the row excludes); a second body; a wire port. None is asked for. The campaign row (`requirements.md`, campaign coverage) lists "manual/mechanical/solenoid tap variants" and no plain tap, so a level that teaches the manual variant places a `tap`.

## Engine capabilities

It adds none. EL-003's table applies, with the counts stated there. One delivered fact carries the "fixed during Run" check:

| Capability | Mark | Evidence |
| --- | --- | --- |
| A build-only parameter compiles to no Run control | present | `ParameterRunEdit` (`ParameterDescriptor.cs:44`); `RunControlTableTests.RunCompilesOneRowPerDeclaredRunControlOfAnUnlockedPart` (`CuriousContraptions.tests/RunControlTableTests.cs:35`), whose table holds rows only for `RunControl` parameters |

### Engine conformance (22 November 2026)

Conforms by reference: the satisfying declaration is EL-003's, which is declaration data only (a static body, two colliders, one fitting with one seat, two mouths, one parameter, one parameter binding), uses the one aperture law (L7), keeps colliders, visuals and animation apart, has no store and so no thermal node, and compiles to the flat counts stated there. This identity adds no state, no layer and no cost.

## Sources and legacy

- Requirement and outcome: quoted above. No variants.
- Campaign coverage row: "manual/mechanical/solenoid tap variants … with individually exercised construction objectives". The manual variant's objective is exercised with the `tap` package.
- Component research [water](../../component-research.md#water): "Quarter-turn handle regulates existing supply".
- **Legacy.** None at `a6c914e`. **Files harvested:** none.

## Acceptance outline

Authority: [element-165](../requirements.md#element-165). **The closure check** is these rows, all in `TapTests`, all run in the EL-003 slice. D1 to D4 are EL-003's rows T2, T4, T5 and T7; D5 is EL-003's row T9, which the EL-003 slice adds (Delivery).

| # | Given / When / Then | Test | Control | Boundary |
| --- | --- | --- | --- | --- |
| D1 | A closed tap on a full tank passes nothing for 20 s: every committed flow is under 0.0005 m³/s (L17) | `TapTests.AClosedTapStopsRoutedFlow` (EL-003 T2) | T1 | `opening` 0.125 passes 0.0103 m³/s within 10% |
| D2 | An open tap with `Start` unjoined emits nothing | `TapTests.AnUnconnectedTapEmitsNothing` (EL-003 T4) | T1 | a tap 0.3 m from the tank mouth |
| D3 | An open tap on an empty tank emits nothing | `TapTests.AnEmptySupplyGivesNoStream` (EL-003 T5) | T1 | — |
| D4 | Reset and save and load keep `opening` | `TapTests.ResetAndSaveLoadKeepTheOpening` (EL-003 T7) | — | — |
| D5 | Given a tap at `opening` 0.5 on a full tank, when Run starts, then the compiled Run-control table has no row for the tap, and the committed seat opening equals the authored value 0.5 (a copied parameter, not a simulated float) at every sampled tick for 12 s while the tank drains | `TapTests.TheOpeningIsFixedDuringRun` (EL-003 T9) | a Battery in the same construction has its `enabled` row | — |

When D1 to D5 pass in the EL-003 slice, that slice's review record lists EL-165 as closed, and the ledger entry for EL-165 cites it.

## Open questions

All resolved; none needs the owner.

1. Own package or closure: closure. EL-003 has a build-only, player-selected opening. A second package with the same declaration would put two identical entries in the palette, which D8 excludes. The palette therefore shows one Tap, not a Tap and a Manual tap; the identity is still accounted for by name (Duplicate closure, approved 21 November 2026).
2. A tap turned during Run: not asked for. The row says the aperture "remains fixed during Run"; a Run-editable tap would be a new variant with its own slice (Disqualifiers).

## Integration

- **Works with.** Everything EL-003 works with.
- **Campaign.** Chapter 7, levels 61–70; reuse 71–90, 114, 126–130, 136–150. A level that teaches the manual tap places a `tap`.

## Delivery

- **Slice shape.** Duplicate closure inside the EL-003 slice.
- **Prerequisites, in order.** Those of EL-003: blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20); then EL-001; then S7, which must also say how a closed duplicate is counted.
- **Files.** None of its own. The EL-003 slice adds test D5 (`TapTests.TheOpeningIsFixedDuringRun`) to `CuriousContraptions.tests/TapTests.cs` and records the closure; the ledger entry is updated to cite it.
- **Edits to other documents, as proposed text (made with the plan-document updates, not by this spec).**
  - `EL-003-tap.md` line 15, the Disposition cell, becomes: "To deliver as a single element with its own catalogue entry (D8). It is the base seat declaration: [EL-166](EL-166-mechanically-actuated-tap.md) and [EL-167](EL-167-solenoid-tap.md) copy its body, mouths and seat and differ in how the opening is set (L14 b); they are variants delivered after it (the "Actuated taps" family). [EL-165](EL-165-manual-tap.md) is closed as a duplicate in this slice by rows T2, T4, T5, T7 and T9."
  - `EL-003-tap.md` line 120 becomes: "1. Own entry: its own entry (D8). EL-166 and EL-167 are variants delivered after it; EL-165 is closed as a duplicate in this slice."
  - `EL-003-tap.md` acceptance table gains row "| T9 | Given a tap at `opening` 0.5 on a full tank, when Run starts, then the compiled Run-control table has no row for the tap, and the committed seat opening equals the authored value 0.5 (a copied parameter, not a simulated float) at every sampled tick for 12 s while the tank drains | `TapTests.TheOpeningIsFixedDuringRun` | a Battery in the same construction has its `enabled` row | — |", and its conformance point 7 reads "Rows T1 to T9".
  - `docs/planning/invest/delivery-batches.md` line 77 becomes "| Liquid | Actuated taps | EL-166, EL-167 (after EL-003) |".
  - `docs/planning/invest/delivery-batches.md` section 2b gains the row "| EL-165 Manual tap | EL-003 Tap; check: `TapTests` rows T2, T4, T5, T7 and T9 (`TapTests.TheOpeningIsFixedDuringRun`) |", and its heading count rises by one.
- **Review tier.** That of the EL-003 slice.
- **Disqualifiers.** EL-003 delivered with a Run-editable opening, or the owner asking for a manual tap that can be turned during Run: either makes this a variant with its own slice.

## Coverage binding (planned, not yet live)

None of its own: a closed duplicate has no package to bind. Coverage of the identity is the closure record in the EL-003 slice. The coverage follow-on story S7 must say how a closed duplicate is counted; the proposed text is in `readiness/batch-g.md`.
