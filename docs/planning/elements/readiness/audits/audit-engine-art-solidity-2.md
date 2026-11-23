# Planning audit, second pass: `spec-engine-art-solidity.md` (ENGINE-ART-SOLIDITY)

Auditor: planning auditor. Checkout `spec-drafts` at `8d1d68aa`. Phase: planning; nothing was built or run. The repaired contract was re-read in full (4,713 words, within budget) and its new cross-references were opened.

## Verdict: Scoped Pass, blocked only on the prerequisite

Scope of the pass: the planning design of this contract as it would stand once parts A, B and C of `ELEMENTS-VISIBLE-SOLID-REMAINDER` are published. No blocking finding remains. The contract's own "At replanning" step 5 requires another planning audit on the published revision, because line numbers, the 44-row table, the test totals and the matcher's three extensions are not yet real. This pass does not replace that audit and certifies no implementation.

## First-pass findings, checked against the repaired text

| Finding | Repair | Result |
|---|---|---|
| B1 vacuous AC1 | M1 (35) now reads "no entry gains, loses or changes a field"; AC1 (139) observes diff paths, the `BundleEntry` declaration, `.tscn` files and two existing tests; the bundle path is gone from the command (187) | Closed |
| B2 binding rule | M5 (39) requires a binding "that moves, turns, scales or hides" the node, or a span binding; D5 (111) lists the nine properties and excludes `Bodies`, `Parameters` and colour bindings; two refusal cases added (176); consequences for `wound_spring` recorded (101) | Closed; see N1 |
| B3 opt-out | Always (27) and task 2 (130) apply the rule to every discovered package; new fact at 172 | Closed |
| B4 lost independent check, wiring | Task 6 (134) keeps both assertions against the Godot-read art; task 3 (131) adds the internal `Generate` overload; M2's test runs through it (171); paragraph 160 states the independent reader | Closed |
| B5 codes and cases | M5's error column assigns each case; the theory has 13 cases with literal codes (176) | Closed |
| B6 unprovable reasons | Reviewer checklist text in task 7 (135); `docs/testing-guidance.md` has a "Reviewer checklist" heading at 149 to receive it; D7 makes the tether reason need a citation the generator checks; S4 owns the executed checks | Closed; see N3 |
| B7 second hand list | Never (29) narrowed to `VisibleSolidColliderTests`; `FaceMarkingTests` listed as a known second list (69); S4's contract owns it at its task 11 (`spec-s4-catalogue-conformance-suite.md:159`) | Closed |
| B8 proxy and hidden | Defined in Always (27); the generator's name rule is carried into the test (134); new fact for art under a hidden node (173) | Closed |
| N2 unsurveyed rows | Named in "At replanning" step 3 (200); the Conveyor wheels are in part B (`spec-elements-visible-solid-remainder.md:56`) | Closed |
| N3 fixture, N4 fit ceiling | Task 4 (132); D8 (114) | Closed; see N2 below |

Counts agree: 1 + 1 + 1 + 1 + 2 + 3 + 13 + 1 = 23 new cases, matching AC4 (142) and the commands at 188 and 191. The cross-references hold: the turned-art rule is decision D5 of the prerequisite contract (`spec-elements-visible-solid-remainder.md:123`).

## Remaining findings, none blocking

- **N1. "The node of a span binding" is looser than D5 (contract 39 against 111).** `PresentationSpanBinding` has two nodes (`CuriousContraptions.Abstractions/ElementPresentation.cs:118-119`). `MovingNode` is the body-bound end (`Visual/Head` in `linear_pusher` and `wound_spring`, `Visual/Plate` in `spring`); if it counted, every mesh on that body could be declared moved by a binding. D5 already says "span node", so the two do not contradict, but the intent freezes after planning. Fix at 39: replace "or the node of a span binding" with "or the span node of a span binding (not its moving node)". Optional fourteenth case at 176: `MovedByBinding` on a mesh under a span's moving node, `BUNDLE019`; the totals then become 24. Classification: wrong specified behaviour (wording).
- **N2. D4 contradicts D8 (contract 110 against 114).** D4 still calls 0.005 m "the matcher's existing mesh-error bound"; D8 says it is not. Fix at 110: "bounded at 0.005 m (D8)". Classification: wrong specified behaviour (wording).
- **N3. An empty citation.** M5 says "without an owner-decision citation" and the test says "no citation". Fix at 39 and 176: "null, empty or white space". Classification: test.
- **N4. The probe overstates the test's proxy rule (contract 77).** "The visible-solid test applies the same exclusion at 249" is not so today; the test checks the prefix only, as task 6 itself says. Fix at 77: "the visible-solid test applies the prefix part of it at 249; task 6 aligns it". Classification: wrong specified behaviour (record).
- **N5. Design Notes answer 3 (contract 153)** lists "node path, solidity and optional fit" and omits the reason and the citation. Fix: add them. Classification: record.
- **N6. Still no planning-document row.** `ENGINE-ART-SOLIDITY` and `ELEMENTS-VISIBLE-SOLID-REMAINDER` appear in no file under `docs/` or in `TODO.md`, so task 7's "mark the story delivered" has no row to mark. The coordinator adds both rows to section 5 of `docs/planning/invest/delivery-batches.md` before dispatch. Classification: record.
- **N7. A dependency for S4 to settle, not this contract.** S4's task 11 checks that an `Enclosed` mesh "lies inside the solid its entry names", but `SceneArt` (129) carries no such reference and S4's intent forbids an abstraction change (`spec-s4-catalogue-conformance-suite.md:28`). D9 here says a per-entry face reference would be needed. Decide before this slice is dispatched: either add the reference fields to `SceneArt` now, so the 44 declarations are written once, or record in D9 that S4 finds the face and the enclosing solid geometrically from the package's colliders with no new field. Recommended: the second, since it keeps the reason unprovable-by-assertion data out of the declaration. S4's replanning step 7 (`:136`) is the place it is checked.

## Decisions

The three decisions recorded under delegation are defensible and are not scope cuts or waived gates: the tether reason needs a cited owner decision on each use (D7), `FaceMarkingTests` moves to S4 (D9), and the Conveyor's pulley wheels are repaired in part B by precedent. Nothing new needs the owner. N7 is a coordinator choice between two designs with the same player-visible result.

## Evidence limits

- Nothing was built or run; all tests named are planned.
- Part B and part C have no contracts of their own; their content was read only from the summary in `spec-elements-visible-solid-remainder.md:56-58`.
- The measurement of the Conveyor wheels (200) was not repeated.
- The E1 to E9 applicability claim (158) and the pre-commit hook's generator run (192) were not traced further than on the first pass.
