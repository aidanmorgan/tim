# S4 planning audit, second pass: catalogue conformance suite

**Verdict: Scoped Pass (planning), conditional on owner decisions O1, O2 and O3.** Those three decisions are the only remaining gate. Nothing was built or run; planned tests are not executed proof.

Contract: `_bmad-output/implementation-artifacts/spec-s4-catalogue-conformance-suite.md` in the spec-drafts checkout (4,391 words, `8d1d68aa`). "contract:N" is a line in it.

## First-pass findings

All of B1–B6 and N1–N11 are repaired in the text, with the three stated differences accepted.

| Finding | Where repaired | Result |
|---|---|---|
| B1 three omitted tables | contract:38, 80, 126 | Fifteen tables; `Presentation` and `Motion` excluded with a reason |
| B2 tick assertion | contract:38, 126 | Tick and per-table count reported |
| B3 default probe | contract:34, 165 | Default is probed; the control names `length` |
| B4 deletion coverage | contract:34, 36, 155 | One-step neighbours and zero probe added; pipe's 3.6 is its default |
| B5 unclassified repeats | contract:53–61, 105–109, 128 | Five theories added to the deletions; kept tests listed with reasons |
| B6 store controls | contract:168, 175 | `EveryComparedTableReportsADifference` added; lone-run residual stated |
| N1–N4, N6–N10 | contract:26, 39, 124, 170, 134, 93, 182, 36, 193 | Applied as proposed |
| N5 partner search | contract:125, 137 | Now task 3, before R1b is written, with the return-to-planning clause in AC5. Acceptable: a port with no admissible pair stops the slice instead of being patched |
| N11 prerequisite name | contract:83, 115, 129 | `ENGINE-ART-SOLIDITY`; its story text is proposed for the planning documents |
| O3 reference | contract:74, 117 | Confirmed: `Workshop.ApplyDeclaredParametersAsync` at `ui/WorkshopConfiguration.cs:284`, refusing at 302 |

## The five newly added deletions

I opened each. All five are safe to delete; four are pure repeats of row 2 and one is weaker than a repeat.

| Theory | What it asserts | Covered by |
|---|---|---|
| `ConveyorTests.InvalidParameters_RefusedAtomically` (`ConveyorTests.cs:487–510`, 21 cases) | −1, 0, a value below the minimum, a value above the maximum, NaN and ±∞ refused at the collection and at placement, for each of three parameters | R2 (collection and placement seams; zero below a positive minimum). Pure repeat. `RefusedParameters` has no other user |
| `ImpactLeverTests.OutOfRangeBeamMass_IsRefused` and `OutOfRangeInitialAngle_IsRefused` (`ImpactLeverTests.cs:459–481`, 4 + 4 cases) | Placement throws `ArgumentException` | R2 placement seam. See the note below |
| `ClockGuardTests.AnIntervalOutsideItsAdmittedRangeIsRefusedAtConstruction` (`ClockGuardTests.cs:143–155`, 8 cases) | One ulp below 0.1 and above 12, 0, −1, 0.01, 13, NaN and +∞ refused at placement | R2 placement seam at one step outside each bound. Pure repeat. The `Ulps` helper stays in use at line 139 |
| `DeclaredOscillatorTests.AnIntervalOutsideItsRangeIsRefusedAtBothBoundaries` (`DeclaredOscillatorTests.cs:53–79`, 8 cases) | Same values refused at the save decode (`OutOfRange` or `NonFinite`) and at `MapInstance` (`OutOfRange`); 0.1 and 12 load bit for bit | R2 decode seams, R2 control and R1a at the bounds. Pure repeat. The `Save` helper stays in use |

Note on the two impact-lever theories: each passes a collection holding one of the part's two declared parameters, so placement refuses the incomplete set whatever the value (`engine/gpu/InstanceParameters.cs:41`). They could not fail for an in-range value. Deleting them loses nothing: the incomplete-set refusal stays proven by the kept `WeightTests.AMissingMassOrAnUnknownKeyIsRefusedWithoutDefaultFill` and `WorkshopWallTests.InvalidSubmittedParametersRejectAtomicallyWhileExactF32ValuesRoundTrip`.

None of the nine deleted theories is named by a tool, hook or current document. The only references outside the test project are historical records under `_bmad-output/`, which stay unchanged.

## Arithmetic and cited symbols

- 7 + 10 + 10 + 21 + 4 + 4 + 8 + 8 + 4 = 76 cases removed across nine theories; four data members. Correct.
- Full round: 4N + 5 − 76, which is +105 at N = 44 (7,351 from 7,246). Correct.
- Registration check: 5N + 5, which is +225 at N = 44, because the existing R3 theory joins the filter. Correct. None of the eight edited test classes is in the filter.
- Capability probe count (nine present, one untested, two narrower, one missing) matches its bullets. `DeclaredPhysics.Contains` is at `engine/gpu/DeclaredPhysics.cs:110`.
- The five charged-store tests named at contract:175 and the three kept tests added to D5 all exist.

## Remaining non-blocking notes

None of these changes the verdict; each is a one-line tidy.

1. **contract:60** — the Oscillator range is `53–79`, not `53–77`; the method closes at line 79.
2. **contract:155** — say the two impact-lever theories were refused for an incomplete parameter set, and name the two kept tests above as the cover for that, so AC3's record is accurate.
3. **contract:39 against contract:166** — "only the domain changed" and "the other endpoint being a delivered port that is valid for the wrong domain" cannot both hold, and the table says "one field changed". Suggested wording for contract:39: "its R1b link with the domain changed to each of the other three, a rope length added or removed as that domain requires, and the other endpoint replaced by a delivered port valid for that domain". Either reading gives a refused link, so the asserted behaviour is the same.
4. **contract:153, 155** — these two paragraphs under Design Notes run past the three-line limit of the contract budget rule. Move them to the run log and cite the entry before the first dispatch; this is record-keeping, not a defect.

## Owner decisions (the remaining gate)

- **O1** — defer row 4 behind `ENGINE-ART-SOLIDITY` (S4a now, S4b later). The contract assumes (a).
- **O2** — fate of the `PackageRuntimeParityTests` literal parameter table. The contract assumes (a), delete in a follow-up; nothing in this slice depends on it.
- **O3** — accept row 2 with a typed reason at two of five seams. The contract assumes (a), with the editor seam recorded as deferred work.

A different answer to O1 or O3 returns the contract to planning, as it says at contract:113.

## Evidence limits

- Nothing was built or run. The baseline total of 7,246, the "under a minute" registration check (it now adds 44 simulated cases) and the 30-second suite estimate are unverified.
- Whether every one of the 76 ports has an admissible single link is unknown until task 3 runs.
- The five charged-store tests were confirmed by title only, as the contract also says.
- `VisibleSolidColliderTests.Specs` coverage (33 of 44) was not recounted.
