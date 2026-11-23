# Planning audit, second pass: `spec-elements-visible-solid-remainder-b2.md` (part B2: Tennis ball)

Audited 22 November 2026. Phase: planning. Nothing was built or run. Scope of this pass: the intent row T5, AC3, the owned scope and consumer list, the changed probe rows, and the move of T1/T2. The repaired contract (4,054 words) was read in full in the `spec-drafts` checkout; the first audit is `.tools/audit/audit-visible-solid-B2.md`.

## Verdict: Incomplete, on one clause of the read-only intent contract

B1 to B5 and N1 to N8 are applied correctly. One blocking defect remains, created by the repair itself: the fallback edit for the contact-spin test is not among the edits the intent row T5 permits. It is a one-clause fix. With R1 applied exactly as written below the verdict is **Scoped Pass** and no third audit is needed. R2 is a non-blocking contradiction to fix in the same edit.

## Remaining blocking finding

### R1. The fallback radius edit is outside what T5 allows

- Where: intent row T5 (`:39`), the owned scope (`:54–57`), AC3 (`:148`), against `:105` and the T5 table row (`:180`).
- Defect: `:105` and `:180` plan an edit to `TennisBallTests.ConfiguredRadiusChangesContactSpinAtFixedMass` (small case 0.125 m becomes 0.1875 m) if the scoped run fails it. T5 says the only edits to existing tests are three named kinds: the single-collider selections, the start spacing of head-on pairs, and the borrowed-sphere fixtures. AC3 allows only edits "the owned scope lists", and the owned scope does not list this one.
- Consequence: if the predicted risk happens, the implementer must either break T5 and AC3 or return to planning, and the intent contract is read-only by then.
- Fix, `:39`, Expected column: "Pass. The only edits to existing tests are those the Code Map lists: the two single-collider selections, the start spacing of head-on Tennis pairs, the borrowed-sphere fixtures, and, only if the scoped run fails it, the small radius of the contact-spin case."
- Fix, owned scope, new line after `:57`: "`CuriousContraptions.tests/TennisBallTests.cs:135` -- only if the scoped run fails the 0.125 m case: that `InlineData` becomes `.1875f`; the bounds at `:148–149` are unchanged."
- Classification: test.

## Non-blocking finding

### R2. The Baseball `Impact` line contradicts the "bounds unchanged" rule

- Where: `:56` ("the one-second route thresholds are re-derived from the 2 cm later contact, not fitted"), against the T5 table row (`:180`, "The masses, speeds and bounds of each edited test are unchanged") and the Never rule (`:29`, "A widened bound in an existing test").
- Defect: the thresholds do not need to change. With the striker at x = −0.53 the Baseball striker meets the target's seam after 22 mm of travel instead of 10 mm, 6 ms later at 2 m/s. After one second the target is about 9 mm short of where it was (1.526 m/s × 6 ms), against the test's 10 cm clearances (above 1.4 m, below 1.2 m). The mass-matched Tennis striker meets seam to seam after 14 mm, 4 ms before the Baseball striker does, so the two routes differ by about 6 mm against the 20 mm bound.
- Fix, `:56`: "`CuriousContraptions.tests/BaseballTests.cs` `Impact`, if the helper exists at the baseline -- the same start, x = −0.53. Contact is at most 6 ms later, which moves the target's one-second position by about 9 mm against 10 cm clearances; the thresholds are unchanged."
- Classification: test.

## What was re-checked and holds

- **T5 (`:39`) and AC3 (`:148`)** agree with each other and with task 5 (`:141`), apart from R1.
- **Owned scope (`:53–57`).** The head-on pair at `TennisBallTests.cs:121` moves to x = −0.53: seams 14 mm apart, felt 30 mm apart, contact within the 60-tick window; the expected speeds follow from the masses. The two borrowed-sphere fixtures take `Colliders = [original.Colliders[0]]`; `BundleEntry.Colliders` is settable through `with` (`FanAdmissionTests.cs:310` does it). The Baseball helper is handled for both publication orders (`:62`, `:200`).
- **Consumer list (`:64`)** matches what I found: three derived entries keep their bindings; 22 puzzles, all `Planned`.
- **Probe rows (`:73–75`, `:78`, `:80`).** `HollowContactQuery.Measure` is at `engine/gpu/HollowContactQuery.cs:40–55`, the Plane arm at `:49`, the hollow-pair arm at `:50–52`; the cited tests exist (`WorkshopPhysicsLawHollowTests.cs:1338`, `:1371`, `:1474`). The airflow row cites the skip at `WorkshopPhysicsEngine.Airflow.cs:151` and is recorded as narrower and accepted, with a deferred entry in task 6. The count line (`:68`) matches the rows: nine present, two narrower.
- **Intent text.** The package count is gone from `:23`; `:27` now says the sample points are unchanged and the seam is an obstacle to air; rolling on the bench is stated as accepted. AC1 reads "baseline + 1".
- **The seam decision (`:115–120`)** is recorded as a coordinator decision with its four visible consequences; no owner decision is open.
- **T1/T2 in `VisibleSolidColliderTests`.** The departure is sound: the file is already owned, `Meet` (`:682`) and `Penetration` (`:412`) are reused instead of copied, the turn's sign is stated and the −45° pose is named as the wrong-pose control. AC4's total (baseline + 5) still holds: one table case, two drop cases, two bound cases.
- **Fallback radius.** 0.1875 m gives 0.171 / 0.1875 = 52° of roll, short of the 69.5° at which the seam meets the bench, and still differs from the default 0.25 m lever arm by 25 per cent against a ±5 per cent band.
- **Filter (`:189`)** now includes `Baseball` and `WorkshopPhysicsLawHollowTests`.
- **Budget.** 4,054 words, inside 6,000.

## Evidence limits

- No build and no test run; the predicted refusals and the contact-spin risk are still unexecuted.
- I did not check whether a bundle entry carries other per-collider data that must shrink with `Colliders` in the two sphere fixtures; `BundleValidator.Validate` in each test will show it.
- The seam-to-seam head-on exchange at x = −0.53 relies on the hollow-pair kernel giving a central elastic exchange; the cited law tests cover that kernel, not this scene.
- A nit, no action needed: D4 calls 0.53 m "the smallest round spacing"; 0.52 m would also clear (4 mm). 0.53 m is fine.
