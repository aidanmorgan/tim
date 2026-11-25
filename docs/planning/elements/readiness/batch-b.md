# Batch B readiness: Epics 8 and 9 (11)

Written 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa` plus uncommitted planning drafts) and re-checked against `origin/main` `25df3cd5`, which changes none of these packages, their tests or the engine. Revised the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-b.md`) and the owner's engine-conformance addendum, and brought up to the [decisions of 22 November 2026](owner-decisions-2026-11-22.md) (owner decisions 3a and 8; coordinator decision 16). All eleven identities are delivered packages. Each spec is now one current text: section 2 is the delivered declaration in three layers (colliders, visuals, animation) with compiled counts, section 3 gives the generic law for each behaviour, the capability probe, the conformance verdict and the performance fitness, and section 7 holds integration and residuals. No section overrides another. No product, test, ledger, requirement or contract file was changed. Nothing was built or run.

## 1. Readiness table

Counts are present / present, untested / narrower / missing. The four sibling gates share the Both gate's probe and conformance.

Every residual slice and closure review below is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element. The "Owning engine stories" column names the 3a stories that own a real gap of that part.

| Identity | Name | Disposition | Package id (palette) | Counts | Colliders (reason: visible = solid) | Conformance | Owning engine stories | Readiness |
|---|---|---|---|---|---|---|---|---|
| CAT-005 | Battery | delivered | `battery` (Power 11) | 5/0/1/0 | 3 | Conforms with fixes: six proud marks to redraw flat; unread ratings | `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`; drain residual) | blocked on the engine programme (owner decision 3a); then the art repair (gap 1) |
| CAT-013 | Both gate | delivered; EL-133 candidate Duplicate closure | `both_gate` (Control 26) | 6/0/1/0 | 10: face, 3 lamps, 4 sockets | Conforms with fixes: unread ratings | `ENGINE-SIGNAL-NODES` (self-edge rule row); `ENGINE-CAPABILITY-INSTANCES` (E2, E3) | blocked on the engine programme (owner decision 3a); closure after the self-edge row |
| CAT-017 | Repeating clock | delivered | `clock` (Control 23) | 6/1/0/0 | 8: face, recess, indicator, 2 sockets, pivot cap | Conforms with fixes: unread ratings | none specific | blocked on the engine programme (owner decision 3a); no slice needed beyond the ratings refactor |
| CAT-020 | Counter | delivered | `counter` (Control 22) | 5/0/1/1 | 15: face, 9 dots, 3 sockets | Conforms with fixes: saturation is repaired in the law; node-kind rename; unread ratings | `ENGINE-SIGNAL-NODES` (saturating-count row); `ENGINE-CAPABILITY-INSTANCES` (E1); `ENGINE-PARAMETER-BOUND-FRAMES` (not needed for the decided display) | blocked on the engine programme (owner decision 3a) and the saturating-count row |
| CAT-024 | Not both gate | delivered; EL-137 candidate Duplicate closure | `electrical_nand` (Control 27) | as CAT-013 | 10 | Conforms with fixes: unread ratings | as CAT-013 | blocked on the engine programme (owner decision 3a); closure after the self-edge row |
| CAT-025 | Neither gate | delivered; EL-136 candidate Duplicate closure | `electrical_nor` (Control 28) | as CAT-013 | 10 | Conforms with fixes: unread ratings | as CAT-013 | blocked on the engine programme (owner decision 3a); closure after the self-edge row |
| CAT-026 | Either gate | delivered; EL-134 candidate Duplicate closure | `electrical_or` (Control 29) | as CAT-013 | 10 | Conforms with fixes: unread ratings | as CAT-013 | blocked on the engine programme (owner decision 3a); closure after the self-edge row |
| CAT-027 | Exactly one gate | delivered; EL-135 candidate Duplicate closure | `electrical_xor` (Control 30) | as CAT-013 | 10 | Conforms with fixes: unread ratings | as CAT-013 | blocked on the engine programme (owner decision 3a); closure after the self-edge row |
| CAT-033 | Hold timer | delivered | `hold_timer` (Control 25) | 6/0/0/0 | 7: face, indicator, 3 pads | Conforms with fixes: unread ratings | none specific | blocked on the engine programme (owner decision 3a); no slice needed beyond the ratings refactor |
| CAT-037 | Set/Reset latch | delivered | `latch` (Control 24) | 7/0/0/0 | 9: face, lamp, 5 sockets | Conforms with fixes: node-kind rename; socket glyphs; refusal wording; unread ratings | `ENGINE-CAPABILITY-INSTANCES` (E1, E3) | blocked on the engine programme (owner decision 3a); no slice needed beyond the refactors |
| CAT-052 | Pressure plate | delivered | `pressure_plate` (Control 21) | 7/0/0/0 | 10: 3 sockets, 4 studs, ring torus | Conforms with fixes: single-impact test; refusal wording; unread ratings | `ENGINE-CONTACT-OBSERVATIONS` (contact-load rows scanned per contact, static owner only) | blocked on the engine programme (owner decision 3a); then the single-impact and ring tests |

**Totals.** Specs changed: 11. Dispositions: 11 delivered, 5 of them the satisfying part of a candidate Duplicate closure. Conformance: 0 Conform, 11 Conform with fixes, 0 Do not conform. No engine code names any Batch B element (audit search of `engine`, `ui` and every non-test `CuriousContraptions.*` project). Readiness: every residual is blocked on the engine programme (owner decision 3a); after it, CAT-005 has an art repair, CAT-020 waits for the saturating-count row of `ENGINE-SIGNAL-NODES`, CAT-052 needs two tests, and the rest need only the forward-only refactors. The five closures (EL-133 to EL-137, batch H) are blocked on `ENGINE-SIGNAL-NODES` (self-edge rule row) after the programme.

**Collider counts.** Every count above the two boxes of an original housing is caused by visible = solid: each proud face panel, lamp, dot, socket, stud and ring is drawn standing clear of the housing and has its own primitive, and merging them would collide where nothing is drawn. No collider is cosmetic, and art moved by a binding (pendulum, rocker, remaining bar) has none. The cost for many instances is the recorded, unmeasured engine gap "Per-instance collider growth from visible = solid" (`_bmad-output/implementation-artifacts/deferred-work.md:177–179`), owned by the performance qualification gate P0-034. The authorities state no per-part budget, so the specs claim none.

**Batch compatibility checks.**
- Package ids and palette orders are unique: 11, then 21 to 30, and clear of the other batches' bands.
- Port domains and roles agree at both ends: Electrical `Supply` outputs feed `PowerIn`, `FirstIn` and `SecondIn` inputs; Pulse outputs (Clock, Counter) feed Pulse inputs (Counter, Hold timer, Latch); Level outputs (Latch, Pressure plate) are accepted only by a level indicator.
- Units agree: seconds for both timers (0.1 to 12), kilograms for the plate (0.1 to 16), joules and watts for the Battery.
- The five gates differ only in declared values. The four siblings were compared file against file.
- Each narrower or missing capability has one owner: the self-edge and saturating-count rows of `ENGINE-SIGNAL-NODES`, `ENGINE-SUPPLY-DEMAND`, `ENGINE-PARAMETER-BOUND-FRAMES`, `ENGINE-CONTACT-OBSERVATIONS` and `ENGINE-CAPABILITY-INSTANCES`.

## 2. Decisions

**Owner decisions of 22 November 2026 applied in the specs.**

| Decision | Applied |
|---|---|
| 3a (engine programme) | Every spec's status and section 7 say every residual is blocked on the engine programme and name the specific stories. The drive-demand story is `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`); the self-edge and saturating-count stories are rows of `ENGINE-SIGNAL-NODES` |
| 8 (approved defaults) | CAT-052: the 0.1 s single-impact pulse is confirmed, the requirement row reworded ("a single impact closes the contact for no longer than the 0.1 s release hold"), and the single-impact test added |
| Coordinator 16 | Parameter-selected art and colliders belong to `ENGINE-PARAMETER-BOUND-FRAMES` |

**Coordinator decisions of 22 November 2026, under the owner's standing delegation.**

| # | Decision | Reason |
|---|---|---|
| 1 | Counter: saturation at the target is repaired in the generic pulse-count law, the saturating-count row of `ENGINE-SIGNAL-NODES`. It needs no owner decision. The display keeps nine fixed dots; after the repair the dots past the target never light. Art and colliders selected by a parameter is a named missing general capability of `ENGINE-PARAMETER-BOUND-FRAMES`, not a prerequisite. EL-183 inherits both | The requirement already says "saturates"; a capability that would serve one element today is not built for it |
| 2 | Battery: the four charge marks and the plus sign are redrawn as flat prints, with the vertical plus bar shortened or shifted clear of the band | They are labels; a print needs no collider, so the count stays three |
| 3 | Pressure plate: a single-impact test is added; the 0.1 s impact pulse is confirmed by owner decision 8 | The requirement's impact-only control has no test today |
| 4 | The self-edge row covers contract rows S4 and S8, and the gate specs state that the Workbench refuses a same-part target | Without it the residual reads as player-reachable; it is not |
| 5 | The drive-demand story is named `ENGINE-SUPPLY-DEMAND` (sprint key `engine-supply-drive-demand`) everywhere, by owner decision 3a; the existing deferred-work anchor stays only as a link target | One slice had three names; the owner chose one |
| 6 | The node-kind names, the socket glyphs, the refusal message that names a product, and the unread `ElectricalNodes` ratings are required forward-only refactors; the node-kind names and glyphs are owned by `ENGINE-CAPABILITY-INSTANCES` (audit E1, E3), the gates' two-input boolean node by the same story (E2) | Forward-only rule: one current representation, no dead data |

**Earlier decisions under the same delegation.**

| Identity | Decision | Reason |
|---|---|---|
| CAT-005 | The first consumer is settled by delivery: Powered gate natively, Motor with Story 11.1a | Both are tested |
| CAT-005 | "Disconnecting" means source or contact loss during Run or an edit between Runs, never a mid-Run cable edit | Story 9.6 decision 4: one topology per Run |
| CAT-013, 037, 052 | The native consumer stays the Powered gate; the Motor recipe of `current-consumers.md` is kept for the browser phase | A consumer reads one route law whatever part it is |
| CAT-017 | The hidden catch-up bound stays 64 impulses per lamp; saturation proof stays at its named gate | D1 and D19; `spec-c-clock.md:62` |
| CAT-017 | The Clock's interval editor clamps while the Hold timer and Delay box refuse; left as delivered | Editor convenience with no effect on a Run |
| CAT-020 | EL-183 is a separate element; this Counter keeps four sockets | D8 and D19 |
| CAT-052 | The studs change colour at once; the legacy easing is not carried | D15 |

## 3. Needs the owner

Nothing. The one item this section held (CAT-052, the 0.1 s single-impact pulse) is decided by owner decision 8 (22 November 2026) and applied in the spec. The `requirements.md` rewording belongs to that file's owner.

## 4. Gaps, with proposed text

**1. Battery repair contract (decision 2).** No repair contract names the Battery. Proposed addition to `spec-elements-visible-solid-remainder-c.md`, or one small repair slice before `spec-engine-art-solidity.md`, after the engine programme (owner decision 3a):

> `battery`: redraw `Visual/Mark_0`–`3` (0.12 × 0.12 m) as `QuadMesh` prints 0.25 mm in front of the band face (z 0.38525). Redraw `Visual/Visual_PlusH` (0.32 × 0.07 m) and `Visual_PlusV` as `QuadMesh` prints 0.25 mm in front of the body face (z 0.37525). `Visual_PlusV` is shortened to 0.07 × 0.31 m, or kept at 0.32 m and centred at y 0.015, because today it spans y −0.14 to 0.18 and the band starts at y 0.175, which would bury its top 5 mm. The marks keep their `CommittedCharge` colour bindings; no collider is added or removed. Test: `BatteryArtTests.TheChargeMarksAndPlusSignArePrintedFlatOnTheirFaces` (each node is a `QuadMesh` facing +Z, its plane between 0 and 0.5 mm in front of its face, its extent inside the face and clear of the band box). Control: the old 0.02 m box fails the plane bound. The comment at `VisibleSolidColliderTests.cs:87` is corrected in the same slice; the row stays Decorative. Existing `SourceStoreInstanceTests` stay green unedited. EL-196 carries the same finding.

In `spec-engine-art-solidity.md`, task 3: add `battery` `Mark_0`–`3`, `Visual_PlusH` and `Visual_PlusV` to the rows that fit no reason today, repaired by the prerequisite.

**2. `spec-engine-art-solidity.md`, task 3.** Two rows it lists as unmeasured are printed flat: `latch` `SetMarker`, `ResetMarker/*`, `OutMarker/*` (z 0.37025) and `hold_timer` `Track` (z 0.37275). Proposed edit: move both to `PrintedFlat`.

**3. `ENGINE-SIGNAL-NODES`: the self-edge rule row (decision 4).** Owner decision 3a places the self-edge rule in `ENGINE-SIGNAL-NODES`; no separate story or sprint key is made. Proposed row text for that story:

> *Present today:* directed supply cycles are judged by the monotone rule (`engine/gpu/SupplyRoute.cs:59–61`). *Narrower:* an edge from a node to itself is refused as `InvalidConnection` before that rule runs (`:50–54`). *Unblocks:* the closure of EL-133 to EL-137. **Specific acceptance:** a self-edge on a monotone logic node is admitted and never powers itself; a self-edge on a nonmonotone logic node is refused as `NonmonotoneFeedback` with the loop message, at construction and at load; a contact feeding its own `PowerIn` is admitted and inert; a package-declared self-edge follows the same rule in the bundle validator; every other bad edge is still `InvalidConnection`.

**4. `ENGINE-SIGNAL-NODES`: the saturating-count row (decision 1).** Owner decision 3a places pulse-count saturation in `ENGINE-SIGNAL-NODES`. Proposed row text:

> *Present today:* a pulse-count node counts distinct occurrences and emits once on reaching its declared target (`engine/gpu/ActivationMemory.cs:13–14`). *Narrower:* the committed count keeps rising to `uint.MaxValue`, so every reader of the count sees values above the target. *Unblocks:* every requirement row that says a count saturates. **Specific acceptance:** given a node with target N, when it receives more than N distinct occurrences, then its committed count is N after each of them (`count = min(count + 1, target)`), it has emitted exactly once and its latched state is unchanged; Reset returns the count to 0; a save keeps the authored target and no run count. A count-threshold presentation binding above the target stays at its rest value. Tests: `MemoryGuardTests.APulsedTargetEmitsOnItsOwnPulseAndNeverAfterSaturation` asserts the count equals the target after twelve pulses (it pins 12 today); the same for a second target value; a declaration with nine count-threshold bindings keeps bindings `target + 1` to 9 at rest. No element package changes.

**5. `ENGINE-PARAMETER-BOUND-FRAMES`: art and its collider present by parameter value (decision 1).** Coordinator decision 16 gives choice-bound values and parameter-bound parts to `ENGINE-PARAMETER-BOUND-FRAMES`; the Batch H audit (S-17) needs the same capability. Proposed acceptance text for that story:

> *Present today:* `PresentationProperty.Visible` driven by committed state; colliders baked unconditionally. *Missing:* a package may declare that an art node and its paired collider are present only when a declared whole-number or choice parameter meets a declared comparison; the selection is resolved at compile, so table counts stay fixed for the Run. *Unblocks:* no delivered part waits for it. **Specific acceptance:** for two values of the parameter the compiled collider count and the drawn nodes differ as declared; a body passes where a deselected node would stand; save and load keep the parameter; a comparison on an undeclared parameter is refused when the bundle loads.

**6. Pressure plate tests (decision 3; owner decision 8).** Rows for the next slice that touches the plate, after the engine programme:

> `PressurePlateTests.ASingleImpactClosesTheContactForItsHoldOnly`: a Basketball thrown across the plate so that it strikes once and lands off it; the supplied contact to a Powered gate is closed on no more than 12 consecutive ticks and open at the end. Control: the same ball coming to rest on the plate stays closed.
> `PressurePlateTests.AFlatLoadRestingOnTheRingLoadsThePlate`: a Weight or a flat Domino resting on the ring alone reads Loaded at a threshold at or below its mass. Control: the same load beside the plate reads Empty.

The requirement row's impact-only control is reworded to "a single impact closes the contact for no longer than the 0.1 s release hold" (owner decision 8).

**7. `delivery-batches.md` §2c, EL-196 row.** "Network acceptance with the Motor" is delivered (`MotorTests`). Proposed text: "Drive work does not debit the store (`ENGINE-SUPPLY-DEMAND`, sprint key `engine-supply-drive-demand`); every other row criterion is delivered; after the engine programme (owner decision 3a)." Use the story name `ENGINE-SUPPLY-DEMAND` wherever the drain slice is named (decision 5).

**8. `current-consumers.md`.** `cat-017-i`: a Signal lamp latches on the first pulse, so it cannot show repetition. Proposed text: "Battery powers Repeating clock; ActivationOut drives a Counter, whose dots light one per interval." `cat-020-i`: one Impact switch cannot send three triggers (its signal node latches; `elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs:26`, `:72`, `:79`). Proposed text: "Three balls cross a Ball detector wired to the Counter; the third emits once to a Signal lamp and closes the supplied contact."

**9. `docs/planning/elements/README.md`, "How to read a spec".** Proposed addition: "A delivered part's section 2 is its delivered declaration in three layers with compiled counts, its section 3 adds the generic laws, conformance verdict and performance fitness, and it ends with section 7, integration and residuals."

**10. Required forward-only refactors (decision 6).**

> - The product-named node kinds `ActivationNodeKind.StateLatch` and `.Counter` (`CuriousContraptions.Abstractions/ActivationValues.cs:18`), the two-input boolean supply node and the shared socket-glyph enum (`First`, `Second`, `Set`, `Reset`) are audit findings E1, E2 and E3, owned by `ENGINE-CAPABILITY-INSTANCES`; no separate deferred-work entry is made.
> - summary: An engine refusal names a product. `engine/gpu/WorkshopConnections.cs:150` says "A level output can only light a Signal lamp." for the generic rule `LevelDrivesLevelInputOnly`. destination: `ENGINE-CAPABILITY-INSTANCES` (audit E4) rewords it to "A level output connects only to an input that admits a level." and updates the tests that pin the text; CAT-035, CAT-037 and CAT-052 cite that one wording.
> - summary: `ElectricalNodeDescriptor` ratings are dead declaration data. `RatedVoltage`, `Resistance`, `Capacitance` and `MaxPower` are only checked finite (`CuriousContraptions.Bundle/BundleValidator.cs:873–876`); no engine, wire or runtime code reads them, in every supplied package. destination: delete the four fields, or the record; deleting the record also removes the non-empty check at `BundleValidator.cs:867` and re-keys the oscillator check (`SignalBundleValidation.cs:167`) to the declared `SupplyConsumerDescriptor`.

**11. Stale comments in a test file** (for the next slice that edits it): `CuriousContraptions.tests/VisibleSolidColliderTests.cs:145` (studs and ring) and `:177` (pivot cap). Both parts are in the Solid or Hollow list of their rows. `CounterTests.ActivationNetwork_CountingProgression_SingleEmission_AndSaturation` does not assert the count; the saturating-count row adds that assertion.

**12. Cross-batch.** EL-183 inherits the saturation repair and the nine-dot face. EL-196 carries the Battery marks finding and the `ENGINE-SUPPLY-DEMAND` residual. The EL-133 to EL-137 closures are blocked on `ENGINE-SIGNAL-NODES` (self-edge rule row) after the engine programme (Batch H hand-back). `readiness/batch-h.md` still names the self-edge story by its retired sprint key; Batch H's repair applies the story name.

## 5. Contradictions found

All of these were corrected in the specs unless marked otherwise.

- **Specs against code.** Four specs named capability interfaces that do not exist; the capabilities are descriptors in `ISignalElement.SignalNodes`. Selection radius was 1.1 for the Counter and the Latch; both declare 1.2. Three catalogue descriptions were the legacy text.
- **Specs against scenes.** The Clock's pivot cap and the plate's studs and ring were called uncollided; all collide since `engine-multi-collider-dynamic` (`28a2d302`). The Battery's marks and plus sign were called printed; they are raised boxes. The sibling gates' truth dots were given at z 0.42; they are flat at z 0.37025.
- **Specs against the engine.** The performance fitness of every static part said it "lives in the static tree of the broadphase and needs no sleeping"; there is no persistent static tree yet (`ENGINE-SPATIAL-QUERIES`) and no island sleeping (`ENGINE-ISLANDS-SLEEP`). The specs now say the static body is never integrated. The plate's sensor was said to cost work in proportion to the bodies touching it; the engine scans every contact for every contact-load row (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:220–237`), owned by `ENGINE-CONTACT-OBSERVATIONS`.
- **Status rows.** Five specs said "not started" and four said publication was pending; `sprint-status.yaml` records every one delivered. The Hold timer's lesson was called Planned; it is Playable.
- **Sections against each other.** The first revision's section 7 overrode sections 1 to 6. Sections 2 and 3 are now the delivered text and the override sentence is gone.
- **Requirement against code.** CAT-020 "saturates" (decision 1) and CAT-052 "impact-only" (owner decision 8; row reworded).
- **Stale against main.** The plate spec's Baseball sentence (0.65 kg on main, 0.15 kg after its approved repair) and the Battery spec's deferred-work line number (now the deferred-work anchor, kept as a link target only; the prose names `ENGINE-SUPPLY-DEMAND`).
- **Not changed.** `sprint-status.yaml` keeps 8.1, 9.1, 9.2 and 9.3 `in-progress` for a Chrome clause while 9.4 to 9.8 are `done` with "browser pending", and several contract front matters disagree with it.

## 6. Not verified

- Nothing was built or run. Every behaviour statement is read from declarations, scenes and tests.
- Test bodies were read for the Counter progression test, the pulsed-target count at `MemoryGuardTests.cs:147`, the Clock flash test and the Both gate save and Reset part. The audit read the plate rows at `PressurePlateTests.cs:198–232` and `:800–912`, the dot-lamp assertions and the Underweight colour assertion. Every other cited test is cited by its method name; the audit confirmed each exists.
- The Clock's bench reach of z +0.495 is derived from the collider list, not from a test.
- Icons (`icon.svg`) were not opened. The retained-behaviour anchors (`todo-146`, `-261`, `-265`, `-267`, `-269`, `-273`, `-362`) and the legacy `a6c914e` citations of section 4 were not re-read.
- Whether a backlog item exists for the naming residues was searched only in `deferred-work.md`.
- The brief's lesson-fixture story S8 is now defined in [`delivery-batches.md`](../../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) and lands after the engine programme (owner decision 22).
