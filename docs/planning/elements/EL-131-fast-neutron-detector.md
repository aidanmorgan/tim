# EL-131 · Fast-neutron detector — element readiness spec

**Current delivery (24 October 2026):** [EL-131 delivery slice](../invest/named-elements.md#element-131). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`) and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e`; citations without `@` are `path:line` in the checkout. Requirement row: [element-131](../requirements.md#element-131). Reads fictional toy sources; never real-world detection guidance.

**This spec is the lead of the Family slice "Neutron detectors"** (EL-131 and [EL-132](EL-132-slow-neutron-detector.md)). Both are the [rate meter](RAD-08-radiation-rate-meter.md) with a fixed channel weight in place of its `sensitivity` setting; this spec states what the two share and EL-131's own values and rows. Units, the reading law, coupling and thresholds are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions).

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-131 · Fast-neutron detector |
| Type | Radiation |
| Anchor | [requirements.md#element-131](../requirements.md#element-131); [named-elements.md#element-131](../invest/named-elements.md#element-131); scope source [radiation-19](../requirements.md#radiation-19) (umbrella, index only) |
| Owner | S629 |
| Disposition | Member and lead of the Family slice **Neutron detectors** ([delivery-batches §1](../invest/delivery-batches.md)) with EL-132. The two differ only in declared values: the one non-zero channel weight, the printed mark, id, title and description. It is not a duplicate of RAD-08: it has no `sensitivity` setting, its own row and its own catalogue entry (D8). |
| Planned package | id `fast_neutron_detector`, directory `elements/FastNeutronDetector/`, class `FastNeutronDetectorElementDeclaration` |
| Related identities | Source [RAD-16](RAD-16-neutron-source-module.md); moderator [RAD-17](RAD-17-water-moderator-tank.md); absorber [RAD-18](RAD-18-neutron-absorber-panel.md); photon counterpart [RAD-08](RAD-08-radiation-rate-meter.md). No CAT part is refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default): every own row but the queued F5 and F6 takes at most 5 s; F5 and F6 run within a RAD-17 fill and drain, which fits 30 s (audit X-2) |

## 2. Declaration

The declaration is [RAD-08's](RAD-08-radiation-rate-meter.md#2-declaration) with the differences below. Everything not listed is the meter's: one static body, case box 0.30 × 0.60 × 0.50 m, base box 0.40 × 0.06 × 0.60 m at y −0.33, three socket spheres r 0.075 m, every collider on the scene's default row; ports `PowerIn` (−0.15, 0, −0.15), `Supply` (−0.15, 0, 0.15) and `ActivationOut` (0, 0.30, 0); the 0.30 × 0.30 m aperture on +X (the standard area 0.09 m²) with nine samples and Lambert facing; the threshold node `detect` over the receiver's committed strength (on at strength ≥ `threshold`, off below 0.75 × `threshold`, **(field, ENGINE-SIGNAL-NODES)**); `ActivationOut` is the pulse on each rise of `detect`; the contact `relay` with `SignalStateContactControl` naming `detect`, `PowerIn` → `relay` → `Supply`; needle, lamp and click bindings.

| Item | Value (family unless marked EL-131) |
| --- | --- |
| Receiver **(field)** | One channel-weight row, fixed, bound to no Choice. EL-131: NeutronFast = 1, every other weight 0. |
| Parameters | `threshold` only: Real, Watt, default 0.2, minimum 0.02, maximum 2.0, editor policy Clamp, build only (D11). No `sensitivity`: the group is the identity. |
| Catalogue | EL-131: id `fast_neutron_detector`, "Fast-neutron detector", `ElementCategory.Radiation` **(field)**, palette order 718 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "Reads fast neutrons only. Its contact passes electricity while the reading is at or above the mark." |

### Art and solidity

RAD-08's table, with `Visual/ChannelMark` replaced by a letter "n" printed flat beside the dial; EL-131 adds two forward chevrons after the letter, EL-132 one bar under it. Both differ from the rate meter by the "n" in place of a Greek letter and from each other by chevrons against bar. No art node is added, so no collider is added.

### Compiled counts

Bodies 1 (static). Colliders 5 (2 boxes, 3 spheres). Signal nodes 1 (threshold). Supply nodes 1. Receivers 1 (nine samples, one weight row). Stores 0. Presentation bindings 5. Joints, drives, sensors 0.

## 3. Engine capabilities

The table of [RAD-08 section 3](RAD-08-radiation-rate-meter.md#3-engine-capabilities) applies without row N2 (no Choice-bound value is declared): **5 present** (R1, R2, R3, R5, R6), **0 untested**, **2 narrower** (N1 line of sight, owner ENGINE-SPATIAL-QUERIES; N3 reading and level drivers for the needle and lamp, owner ENGINE-OPTICAL), **4 missing** (M1 declared receivers with weighted samples and a committed reading and strength, ENGINE-OPTICAL; M2 radiation channels, attenuation rows and chords, ENGINE-RADIATION; M3 a threshold node with a release ratio over a committed scalar, the contact state it presents to `SignalStateContactControl` and a pulse on its rise, ENGINE-SIGNAL-NODES; M4 the category and domain flag, ENGINE-RADIATION). The cited symbols and opened tests are the ones listed there; R3's record is `CuriousContraptions.Abstractions/PortDescriptor.cs:55`. This table is the family's.

**Dependencies.** Battery and Powered gate (delivered); RAD-08, which proves the detector shape once; a fast source, RAD-16; RAD-17 for the moderated rows.

## 4. Sources and legacy

- [element-131](../requirements.md#element-131) and the [radiation-19](../requirements.md#radiation-19) integration task: "Moderated flow drives Slow while the documented Fast reading decreases; a photon source and missing supply are negative controls."
- [Named entry](../invest/named-elements.md#element-131), owner S629; map row (S605, S257); binding `docs/coverage/engine/element-02.json` (proof S629, relation radiation-19).
- [S605](../invest/decisions.md#s605) neutron row; research "Neutron group detector" row (lessons 100, 101, 104 → 125, 126, 129).
- **Legacy.** None in parts, engine, tests, levels, Campaign or reference. Hits are coverage snapshots only, e.g. the FiniteLedger bound-source list `reference/P0-022-before/docs/coverage/engine/capabilities-02.json@a6c914e:L14894-L14897`, plus the aggregate task lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`

## 5. Acceptance

### Behaviour and numbers

Starting values (D1) under the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions). Strength = 1 × the NeutronFast reading; every other channel has weight 0.

- **Open path.** A Neutron module (10 W/sr NeutronFast) 1 m away on the axis: 0.09 × 9.80 = 0.882 W, the standard nine-sample value (radiation convention 14); 0.900 W anywhere inside 1 m (convention 3). On to about 2.12 m, off beyond about 2.45 m, as the rate meter.
- **Through a dry moderator tank.** Two Window walls (NeutronFast 1 /m × 0.02 m = 0.02 each): 0.882 × e^−0.02 × e^−0.02 = 0.882 × 0.980 × 0.980 = 0.847 W.
- **Through a full tank.** The liquid crossing (Liquid row NeutronFast 2.4 /m over 0.5 m) passes out_fast = in_fast × e^−1.2 (radiation convention 5): 0.882 × 0.980 × 0.301 × 0.980 = 0.255 W. The fast reading falls to 30 % of the dry value: that is the "documented Fast reading decreases" of the integration task. At the default 0.2 W mark the level stays on (0.255 W is 27 % above it).
- **A threshold that releases on filling.** With `threshold` 0.5 W the release is 0.75 × 0.5 = 0.375 W: dry 0.847 W is 69 % above the mark (on); full 0.255 W is 32 % below the release (off).
- **Slow flux does not count.** The 0.474 W of slow flux leaving the full tank ([EL-132](EL-132-slow-neutron-detector.md#5-acceptance)) has weight 0 here.
- **Photons do not count.** A Gamma capsule at 1 m gives 0.882 W of PhotonMedium and a strength of 0.
- **Dense slab.** Dense NeutronFast 1.6 /m × 0.05 m = 0.08: 0.882 × e^−0.08 = 0.882 × 0.923 = 0.814 W: a photon shield barely touches fast neutrons.
- **Tick order.** The field solve of tick n runs after the last substep on that tick's final poses and commits the strength with the tick; the signal network of tick n + 1 turns `detect` on, emits the pulse and closes `relay`, so the load is supplied one tick after the flux arrives (radiation convention 11).
- **Energy.** The detector absorbs what its case attenuates and adds none; its contact passes only what `PowerIn` receives.

### Rows

Planned class `NeutronDetectorTests`, one class for the family; rows marked "family" are theories over both members, each with its own group's source. Fixture: Battery `Supply` → detector `PowerIn`; detector `Supply` → Powered gate `PowerIn`. Readings ±3 % (the nearest decision margin is 27 %). Stall guard 2 s. Rows that need only a source use a law-scene source of the right channel until RAD-16 is delivered.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| F1 | Family: given a source of the member's own group at 1 m, the strength is 0.882 W, `detect` is on and the gate is supplied | `NeutronDetectorTests.ItsOwnGroupClosesTheSuppliedContact` | F2 | ±3 % |
| F2 | Given a slow-group source at 1 m, the fast detector's strength is 0 and `detect` stays off for 240 ticks (row: slow group cannot masquerade as fast) | `NeutronDetectorTests.TheFastDetectorIgnoresSlowFlux` | F1 | below 0.001 W |
| F3 | Family: given a Gamma capsule at 1 m, the strength is 0 and `detect` stays off (row: photon control; task 427) | `NeutronDetectorTests.PhotonsNeverCount` | F1 | below 0.001 W |
| F4 | Family: given no wire to `PowerIn` and the member's own source at 1 m, `detect` is on and `Supply` is never live (task 427: missing supply) | `NeutronDetectorTests.AnUnsuppliedDetectorSwitchesNothing` | F1 | gate unsupplied for 240 ticks |
| F5 | Given a Neutron module at 1 m through a Moderator tank, the fast strength is 0.847 W dry and 0.255 W full, and it never rises as the tank fills (task 427: the Fast reading decreases) | `NeutronDetectorTests.ModerationLowersTheFastReading` (queued until RAD-16 and RAD-17) | F1 | ±3 % at both ends; monotone within 3 % |
| F6 | Given F5 with `threshold` 0.5 W, `detect` is on dry and off full | `NeutronDetectorTests.AThresholdAboveTheModeratedReadingReleases` (queued as F5) | F5 | 0.255 W is 32 % below the 0.375 W release |
| F7 | Given a Dense slab on the open path, the fast strength is 0.814 W and `detect` stays on | `NeutronDetectorTests.ADenseSlabBarelyShieldsFastNeutrons` | two Dense slabs silence a photon meter | ±3 % |
| F8 | Family: `threshold` 0.02 and 2.0 are admitted; outside and non-finite values are refused with nothing changed | `NeutronDetectorTests.TheThresholdIsBounded` | — | typed refusal |
| F9 | Family: an electrical output wired to `ActivationOut` is refused | generic: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) | — | typed refusal |
| F10 | Family: after a lit Run, Reset leaves no reading or level; a saved and reloaded construction behaves as F1 with its `threshold` | `NeutronDetectorTests.ResetAndReloadRestoreTheAuthoredState`; authored state by `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) | F1 | strength 0 |
| F11 | Family: a source held in range for 600 ticks pulses `ActivationOut` once, on the rise of `detect` | `NeutronDetectorTests.AStationarySourceCountsOnce` | source moved out and back gives 2 | exact count |

Chrome recipe (pending, post-catalogue): Neutron module, Fast-neutron detector 1 m away, Battery, Powered gate; then a Gamma capsule in the module's place.

## 6. Open questions

None open.

1. **One part with a group mode, or two parts — decided** (D8): two packages, one per named identity; they are one Family slice.
2. **Thresholds and weights — decided** (D1, D11, D16): RAD-08's `threshold` and release ratio; weight 1 on the one group.
3. **Coupling — decided** (coordinator decision 16): strength → threshold node → signal-state contact, as RAD-08. No memory node is set by a receiver.

Needs the owner: none.

## 7. Integration

- **Collaborators.** Battery, Powered gate, Counter (delivered); RAD-16, RAD-17, RAD-18, EL-128, RAD-01 (undelivered). Port domains as RAD-08.
- **Cross-element task.** [sequence-task-427](../requirements.md#sequence-task-427): rows F3, F4, F5 here and S3 in [EL-132](EL-132-slow-neutron-detector.md). Processes [IX-15](../requirements.md#interaction-15), [IX-06](../requirements.md#interaction-06), [IX-07](../requirements.md#interaction-07).
- **Campaign.** Chapter 13, first use 121–130 (lessons 125, 126, 129); reuse 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader (`tools/Coverage/NamedPackageSources.cs` on `origin/main`) admits an `EL-NNN` identity only when its ledger entry carries exactly one `todo-N` link (`:96`) and its decision has an anchor; the entry for element-131 has no such link and S629 has no anchor in `docs/planning/invest/scope-corrections.md` (checked by search).
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Family slice of two packages, one review, one candidate commit.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES and ENGINE-SIGNAL-NODES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-08; S7 for the binding. RAD-16 and RAD-17 (with their own prerequisites, ENGINE-LIQUID among them) are needed only by the queued rows F5 and F6. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names none).
- **Files.** Per member `elements/<Dir>/<Dir>.csproj`, `<Dir>ElementDeclaration.cs`, `<id>.tscn`, `icon.svg` (here `FastNeutronDetector`, `fast_neutron_detector`); one guarded line each in `CuriousContraptions.slnx`; `CuriousContraptions.tests/NeutronDetectorTests.cs`; a row each in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories and RAD-08; otherwise the engine tier.
- **Disqualifiers (the slice's probe re-checks).** RAD-08 delivered in another shape than one package with `sensitivity` (the family follows the delivered shape; cite it); a neutron model without two group channels; a receiver row that cannot weight one channel alone; a threshold node that `SignalStateContactControl` cannot name. In each case the slice stops and the owning story is amended; no second design is kept here.

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story). The seven points of [RAD-08 section 9](RAD-08-radiation-rate-meter.md#9-engine-conformance) hold for both members with one change: a single fixed weight row compiles, so no Choice selects anything. Group selectivity is a row of seven declared weights, six of them zero; nothing in the engine knows a "fast detector". Coupling is strength → threshold node → signal-state contact. Static body, five primitives, nine shadow casts per diverging source in range, each through the spatial query's segment cast.
