# Audit: element specs, Batch B (Epics 8 and 9; 11 delivered packages)

Independent read-only audit, 22 November 2026, checkout `spec-drafts` at `8d1d68aa` plus drafts; `origin/main` is `25df3cd5`. Nothing was built or run. Spec paths are under `docs/planning/elements/`.

Method. All eleven section 7s were read. Every one of the eleven declarations was opened and compared line by line with its spec (ids, palette orders, ports, parameters, signal and supply nodes, `ElectricalNodes`). Scenes were opened for the Battery (whole file), Counter, Pressure plate, Latch and Hold timer. Every test named in every section 7 was checked to exist in its class by script (none missing). Test bodies were read for the four owner questions and the items they rest on (listed in section 8).

## 1. Verdicts

Readiness: 0 Ready, 11 Ready with fixes, 0 Not ready. No section 7 claim was found false against the package. The fixes are wording, stale citations and the conformance items of section 5.

| Spec | Verdict | Reason |
|---|---|---|
| CAT-005 Battery | Ready with fixes | Proud marks confirmed (B-2); citation that moves on main (B-9); proposed repair buries 5 mm of the plus sign (B-7) |
| CAT-013 Both gate | Ready with fixes | Correct against code; residual should say the Workbench cannot make the self-wire (B-6) |
| CAT-017 Clock | Ready with fixes | "Nothing reads `ElectricalNodes`" is wrong for this part's `Role` (B-4) |
| CAT-020 Counter | Ready with fixes | Defect confirmed; the recommended repair is not data-only and half of it needs no owner (B-1) |
| CAT-024, 025, 026, 027 gates | Ready with fixes | Correct against code; B-6 |
| CAT-033 Hold timer | Ready with fixes | Correct against code; conformance items only |
| CAT-037 Latch | Ready with fixes | Correct against code; conformance items only |
| CAT-052 Pressure plate | Ready with fixes | Narrowing confirmed and untested as a control (B-3); Baseball sentence is wrong on main (B-8) |

## 2. The four questions asked

**(1) Counter count past the target: real defect, in what the player reads, not in the puzzle outcome.** `engine/gpu/ActivationMemory.cs:13` increments until `uint.MaxValue`; `:14` reports the crossing once. `engine/gpu/WorkshopDeclaredPresentation.cs:446` lights a lamp when `memory.Count >= d.Threshold`, and the nine bindings carry thresholds 1 to 9 (`elements/Counter/CounterElementDeclaration.cs:13-39`). Nothing reads `target_count` for display (searched `engine`, `ui`, all `CuriousContraptions.*` projects and the TypeScript runtime). `counter.tscn:71-105` draws nine lamps unconditionally. So a default counter shows nine dots, and events four to nine light dots four to nine. `MemoryGuardTests.cs:147` pins the over-count (12). `CounterTests.ActivationNetwork_CountingProgression_SingleEmission_AndSaturation` (`CounterTests.cs:303-312`) asserts only that the contact and lamp stay latched through 20 extra inputs; it does not assert the count, so its name overstates it. Emission and contact are correct and tested (one `ThresholdReached`, contact latched). The requirement rows say "saturates" and "shown with slate/gold dots" (`requirements.md:576`, `:6593`). Effect in play: the face no longer shows the target or that it was reached.

**(2) Pressure plate impact-only: real narrowing of the requirement row; small, bounded effect; no test of the control.** `requirements.md:770` lists an impact-only control and `:6585` says "no … impact command is created". As delivered, any qualifying impact closes the contact on its impact tick and for 11 more ticks (`PressurePlateElementDeclaration.cs:93`, release hold 0.1 s; `PressurePlateTests.cs:209-221` counts a pressed bounce contact; `:816-831` and `:842-851` fix the 11/12 boundary). A steady bounce keeps it closed (`:896-912`). A ball that ends at rest on the plate gives the same final state either way. The difference is a body that strikes once and leaves: it gives 0.1 s of supply to whatever the contact feeds. No test drives that case to a consumer, so the row's control is absent, not passing. The decision was a coordinator override (CAT-052 §6 item 3), not a cited owner decision; removing a listed control is a weakened criterion, so the owner item is real.

**(3) Battery marks and plus sign: confirmed; a visible = solid rule defect with slight play effect.** `battery.tscn:30-32` marks are 0.12 × 0.12 × 0.02 m at z 0.40 (`:69,74,79,84`), reaching 0.41 against the band face at 0.385: 2.5 cm proud. `:34-40`, `:89`, `:93`: plus bars 0.015 m deep at z 0.385 and 0.395, reaching 0.3925 and 0.4025 against the body face at 0.375: 1.75 and 2.75 cm proud. Colliders are only `col_0_0` to `col_0_2` (`:99-115`). `VisibleSolidColliderTests.cs:72-74` lists the six as Decorative, "printed on the body". They are static boxes, not flat, and their bindings are colour only. No repair contract names the Battery (searched `spec-engine-art-solidity.md` and the four `spec-elements-visible-solid-remainder*.md`). In play a ball against the +Z face sinks up to 2.75 cm into drawn solids.

**(4) `ElectricalNodes` ratings: confirmed unread, including the TypeScript runtime; one correction.** `RatedVoltage`, `Resistance`, `Capacitance` and `MaxPower` are only checked finite (`CuriousContraptions.Bundle/BundleValidator.cs:873-876`). No reader exists in `engine`, `ui`, any `CuriousContraptions.*` project, `schemas/workshop_wire.fbs`, or the twelve runtime `.ts` files under `CuriousContraptions.web`, `.Simulation` and `.Animation.Worker` (the only `max_power` on the wire is the prismatic drive's, `workshop_wire.fbs:724`). Two things are read: the list must be non-empty (`BundleValidator.cs:867`) and an oscillator requires a row with `Role == Consumer` (`SignalBundleValidation.cs:167`). Not a gameplay defect: the supply law uses `power_limit`, so the Battery's 100 W against 120 W changes nothing. It is dead declaration data in all eleven packages.

## 3. Findings, by severity

### High

**B-1. Counter repair is mis-scoped (`readiness/batch-b.md:14`, `:50`; `CAT-020-counter.md:139`).** The summary calls the repair "data-only or engine, by the owner's answer" and recommends "show only `target_count` dots". Neither half is data-only.
- Saturation is a change to the generic pulse-count law (`ActivationMemory.cs:13`) and to `MemoryGuardTests.cs:147`. The requirement already says "saturates", so this is a confirmed defect to repair, not an owner choice.
- Hiding dots needs art visibility and collider presence selected by a parameter. `PresentationProperty.Visible` exists but every `PresentationDriver` reads committed state, none a parameter (`CuriousContraptions.Abstractions/ElementPresentation.cs:12-37`), and the nine lamp colliders are baked (`counter.tscn:156-198`). Batch H audit S-17 found the same missing capability for band bars; it has no owner.

Replacement for owner item 1: "Repair now, engine tier: the pulse-count node holds its count at the target (`count = min(count + 1, target)`); `MemoryGuardTests.APulsedTargetEmitsOnItsOwnPulseAndNeverAfterSaturation` asserts the count equals the target after twelve pulses, and `CounterTests` asserts lamps `target` to 8 stay slate. Owner question, art only: keep nine fixed dots with `9 − target` never lit, or show `target` dots, which waits for a parameter-selected art and collider capability." Table row: tier "engine", ready "blocked on the saturation slice; owner for the dot count". EL-183 (`EL-183-resettable-counter.md:72`, `:76`) says "later ones saturate" and "Nine dots is what the face shows"; it inherits both.

**B-2. Battery proud marks (question 3).** The summary's repair text is sound and consistent with Batch H audit E-4. Add to it: change `VisibleSolidColliderTests.cs:72` comment only after the redraw; the row stays Decorative as printed flat.

**B-3. Pressure plate (question 2).** Keep owner item 2, and add the missing proof whichever way it is answered. If confirmed: reword the row as the summary proposes and add `PressurePlateTests.ASingleImpactClosesTheContactForItsHoldOnly` (a Basketball thrown across the plate so it strikes once and lands off it; the supplied contact is closed on no more than 12 consecutive ticks and open at the end; control: the same ball resting stays closed). If refused: a dwell qualifier on the generic contact-load node is an engine slice.

### Medium

- **B-4. `CAT-017-clock.md` residual, and gap 8 of the summary.** Replace "nothing was found that reads them" with: "the four rating values are unread; the oscillator rule requires this row's `Consumer` role (`SignalBundleValidation.cs:167`)". Gap 8's deferred-work entry must add: "deleting the record also removes `BundleValidator.cs:867` and re-keys the oscillator check to the declared `SupplyConsumerDescriptor`".
- **B-5. Summary gap 3 (story text for the self-edge).** It covers contract rows S1 and S2 only. `spec-engine-supply-self-edge-cycle-rule.md:49,53` also admit a contact self-edge on `PowerIn` (S4) and judge a package-declared internal self-edge (S8). Add to the proposed acceptance: "a contact feeding its own `PowerIn` is admitted and inert; a package-declared self-edge follows the same rule in the bundle validator; every other bad edge is still `InvalidConnection`." Neither this checkout nor `origin/main` has the story or a sprint key.
- **B-6. Gate residuals (`CAT-013:146`, `CAT-024` to `027` Residuals).** Add: "The Workbench connect tool refuses a same-part target (`ui/WorkshopConnections.cs:106`), so only a save or a command can carry this wire." Without it the residual reads as a player-reachable defect. It is not one.

### Low

- **B-7.** Summary gap 1: the plus sign's vertical bar spans y −0.14 to 0.18 and the band starts at y 0.175, so a flat print on the body face has its top 5 mm inside the band box. Shorten `Visual_PlusV` to 0.07 × 0.31 m or centre it at y 0.015.
- **B-8.** `CAT-052:158`: published main's Baseball is 0.65 kg (`origin/main:elements/Baseball/BaseballElementDeclaration.cs:22`), which presses a default plate alone. Replace the sentence with: "The Baseball is 0.65 kg on published main and presses the default plate alone; after its approved 0.15 kg repair it does not."
- **B-9.** `CAT-005:186` cites `deferred-work.md:211–213`; on main the entry starts at `:217`. Cite the anchor `#powered-part-demand-drain`.
- **B-10.** `CAT-020:145`: the unchecked item can be closed. The Impact switch's signal node latches and its description is "sends one activation" (`ImpactSwitchElementDeclaration.cs:26,72,79`), so one switch cannot send three triggers; summary gap 5's replacement text is right.
- **B-11.** `CAT-052:153`: accurate. The ring is touched in passing by the tilted roll-off (`PressurePlateTests.cs:529-531`), but no test rests a flat load (a Weight or a flat Domino) on the ring and expects Loaded; `OnlyDirectContactCountsForAStack` is a negative case that passes either way. Worth one positive row when the plate is next touched.
- **B-12.** `engine/gpu/WorkshopConnections.cs:150`: an engine refusal message names a product ("A level output can only light a Signal lamp."). The rule itself is generic (`LevelDrivesLevelInputOnly`). See section 5.

## 4. Gate closures against the contracts and the Batch H audit

- Identity mapping agrees everywhere: EL-133 Both, EL-134 Either (CAT-026), EL-135 Exactly one (CAT-027), EL-136 Neither (CAT-025), EL-137 Not both (CAT-024).
- The blocker is real: `engine/gpu/SupplyRoute.cs:50` refuses `from == to` before the cycle check at `:61-62`. Each gate has one supply node, so own-output to own-input is that case.
- The five declarations differ only in id, name, description, palette order (26 to 30), icon, scene and operation (`:35` in each); ports, material, `ElectricalNodes` and the nine lamp bindings are identical. This matches the contract's "True duplicate" table and Batch H audit's "correct against code".
- The CAT-013 probe (6 present, 1 narrower) agrees with the contract's (7 present, 1 narrower); the contract splits provenance and debit.
- Batch H audit E-13 (G6 runs once, through the Both gate) and E-14 (stale contract lines) are not contradicted by the Batch B specs and are not repeated in them; no Batch B edit is needed for either.
- Fixes: B-5 and B-6.

## 5. Engine conformance (owner addendum, 22 November 2026)

**Engine search.** Searched `engine`, `ui` and every non-test `CuriousContraptions.*` project for the eleven package ids as string literals, any `PartId("…")` literal, the package class and directory names, and the `CuriousContraptions.Elements` namespace: **no hit**. No engine code branches on any Batch B element. Two naming residues, neither a special case:
- `ActivationNodeKind.StateLatch` and `.Counter` (`CuriousContraptions.Abstractions/ActivationValues.cs:18`) are the generic memory and pulse-count node kinds, selected by `SignalMemoryDescriptor` and `SignalPulseCountDescriptor` (`engine/gpu/WorkshopActivationCompiler.cs:69`; mapped to `SignalNodeKind.Memory` at `ActivationNetwork.cs:166`). Required refactor, low: rename to `Memory` and `PulseCount`. No backlog item found.
- B-12 above. Rewrite: "A level output connects only to a level indicator input." No backlog item found.

**Verdict per spec: all eleven Conform with fixes.** The delivered packages meet points 1 to 5 and 7: each is declaration data with generic descriptors, static bodies, baked `col_` colliders separate from art, and presentation bindings that read committed state. The violations are in the spec text, plus B-1 and B-2.

| Spec | Violations |
|---|---|
| All eleven | **Forward-only rewrite.** Each section 7 opens "Where a value here differs from sections 1 to 6, this section is the delivered one" (for example `CAT-020:113`, `CAT-005:164`, `CAT-052:124`). The addendum requires the older text rewritten, not kept beside it. Known pairs: selection radius 1.1 against 1.2 (`CAT-020:31`, `CAT-037` §2); truth dots at z 0.42 (`CAT-024` to `027` §2); the plate's "Uncollided" paragraph and stud easing (`CAT-052:66,149`); legacy descriptions (`CAT-013` §2). Rewrite sections 2 and 3 to the delivered values and delete the override sentence. |
| All eleven | **Point 5.** No spec states its compiled counts. Add one line each, for example Counter: "1 static body; 15 colliders (3 boxes, 12 spheres); 0 joints; 1 signal node; 1 supply node; 0 stores; 27 bindings." |
| All eleven | **Point 6.** No reason is given for collider counts above the two of the original housings (Counter 15, gates 10, Plate 10, Latch 9, Clock 8, Hold timer 7). Cite the existing deferred-work entry from `spec-elements-visible-solid-sweep.md` ("Per-instance collider growth", evidence "Not measured") as the recorded engine gap. |
| CAT-020 | **Points 2 and 3.** B-1: the display the requirement asks for needs a parameter-selected art and collider capability that would today exist for one element. Proposed element-free story text: "A package may declare that an art node and its paired collider are present only when a declared whole-number or choice parameter meets a declared comparison; the selection is resolved at compile, so table counts stay fixed for the Run." Owner: none yet (Batch H audit S-17 needs the same). |
| CAT-005 | **Point 3.** B-2: six static proud visuals with no collider. |
| CAT-017, 033, 037 | **Point 3, wording.** Say for each art node which layer moves it. The pendulum (bob to z 0.54), the Latch rocker (to z 0.50, 13 cm proud) and the Hold timer bar (to z 0.4325) are moved by bindings and take no collider; that is allowed, and the specs say so. The remaining nodes need "nothing" stated. |

## 6. Is anything stale against `origin/main` (`25df3cd5`)?

Main's later changes are the Baseball package, the one-rule test compile glob, coverage tooling and planning text. None touches `elements/` for this batch, `engine/`, `VisibleSolidColliderTests.cs`, `PressurePlateTests.cs`, `CounterTests.cs` or `MemoryGuardTests.cs`, so every code finding stands. Stale items:
- B-8 (Baseball mass) and B-9 (deferred-work line).
- `spec-closure-review-electrical-gates.md:111,150` and the self-edge contract's `Compile`-line step are obsolete on main (already Batch H audit E-14).
- `sprint-status.yaml:267` and `:301` are unchanged on main; `:650-654` (the five `named-el` keys) are unchanged.
- `readiness/batch-b.md:96` can now read: "re-checked against `25df3cd5`; two sentences stale (CAT-052:158, CAT-005:186)".

## 7. Cross-batch, story gaps and owner items

**Cross-batch.**
- One slice has three names: `engine-supply-drive-demand` (Batch B and H), ENGINE-SUPPLY-DEMAND with key `powered-part-demand-drain` (Batch J), and the deferred-work anchor `powered-part-demand-drain`. Use the deferred-work key.
- Palette orders 11 and 21 to 30 are unique and clear of the other batches' bands.
- EL-196 must carry the Battery finding (Batch H audit E-4); EL-183 inherits B-1.

**Engine-story gaps.** `epics.md` has no story for the self-edge rule (text in summary gap 3, amended by B-5), for pulse-count saturation (B-1), or for parameter-selected art and colliders (section 5).

**For the owner.**
1. Counter dots: nine fixed with the surplus never lit, or `target` dots. Recommended: nine fixed now, after saturation; revisit when the parameter-selected art capability exists. Saturation itself needs no decision.
2. Pressure plate: confirm the 0.1 s impact pulse and reword the row. Recommended: confirm, with the single-impact test of B-3.

The summary's delegated decisions (Powered gate as native consumer, the Clock's clamp editor, immediate stud colour, EL-183 separate) are defensible and waive no gate.

## 8. Not verified

- Nothing was run; the Counter and plate behaviours are read from code and tests.
- Test bodies read: `CounterTests.ActivationNetwork_CountingProgression_SingleEmission_AndSaturation`; `MemoryGuardTests.APulsedTargetEmitsOnItsOwnPulseAndNeverAfterSaturation`; `PressurePlateTests` rows at `:198-232` and `:800-912`; `DeclaredContactLoadTests.TheDeclaredSensorCompilesItsRegionAndMembersForEveryPlate`; the dot-lamp assertions in `CounterTests.cs:41-70` and `DeclaredPresentationTests.cs:266-270`; the Underweight colour assertion at `ComposableAnimationMappingTests.cs:586-600`. Every other cited test was checked for existence only, including all gate, Clock, Hold timer and Latch tests.
- Scenes of the Clock and the five gates were not opened; their collider lists rest on the declarations and on `VisibleSolidColliderTests` rows.
- Legacy `a6c914e` citations, icons, and the `todo-146`, `-261`, `-267`, `-273`, `-362` anchors.
- Whether a backlog item exists for the two naming residues was searched only in `deferred-work.md`.
