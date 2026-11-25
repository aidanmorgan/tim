# Run log: visible-solid repair part A

Append-only. Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder.md`.

## Handled owner messages

- 24 November 2026: dispatch of part A on published main `0959e883`, publish with `git push origin HEAD:main` after approval, do not wait for CI (AGENTS.md owner instruction of 29 October 2026).

## E1 Session environment check (24 November 2026)

- Checkout `/Users/aidan/dev/personal/tim/.claude/worktrees/slice-visible-solid-a`, branch `slice-visible-solid-a` from `origin/main` = `0959e8834bacd5dd82226aa737c78ed3afb796e3`, tree clean.
- Pinned toolchain first on `PATH`: `node --version` = v26.12.1; `dotnet --version` = 10.0.403 (matches `global.json`); `dotnet workload list` shows `wasm-tools` 10.0.114/10.0.100.
- `--bmad-check --route Auto`: structural findings 0; three `when` conditions reported for the tiered layers (blind-hunter, edge-case-hunter, intent-alignment). Read against protocol Review tiers: the slice is engine tier (contract Code Map), so all six layers run on the first pass.
- Restore runs with the owner's first build.

## E2 Dispatch

- Baseline captured once: `baseline_revision` = `0959e8834bacd5dd82226aa737c78ed3afb796e3` (coordinator decision 38). Planning verdict Scoped Pass (`audit-visible-solid-A-4.md`). Tier: engine (Code Map).

## E3 Implementation owner: baseline and candidate evidence (24 November 2026)

Raw output is under `.tools/visible-solid-remainder-a-evidence/` (git-ignored).

- Baseline build at `0959e883` (incremental, 0 warnings, 0 errors; `baseline-build.txt`); `bundle-baseline.json` copied. Baseline count round (`baseline-full-round.txt`, run without compiler logs, so ten static-gate and float-guard tests fail there only for the missing logs): 17 + 287 + 115 + 256 + 6619 = 7294 tests, `Skipped: 0`. AC4 therefore expects 7309.
- Candidate incremental build 0 warnings, 0 errors. New and changed tests first run: 128 passed (`new-tests-second-run.txt`). One correction before it: the first run failed the two print facts because the scene text writes a `Transform3D` basis row by row and the quads faced down; the nine transforms were transposed (scene edit only, no contract change).
- AC2 comparison (`ac2-comparison.txt`, script kept outside the tree): exactly three entries differ: `rope_anchor` 1 to 2 colliders, `pulley` 2 to 4, `fan` 1 to 3 colliders on the base body and its airflow `InternalOccluders` 2 to 4 (`windmill`'s list is unchanged); every other entry including `impact_lever` and `trampoline` is equal.
- Drop lines recomputed: A1 the tube top is 0.2475 m above the mount top (the nearest other solid); A2 the mount's near edge is 0.398 m from the resting centre, 58 mm clear, and the wheel face 105 mm under the ball; A3 the wheel's edge is 0.39 m from the resting centre, 50 mm clear, and the post and mount are more than 0.13 m clear; A4 the hub is 0.83 m away. All clearances are at least 50 mm. The hub test's small ball turns the hub past 0.1 rad/s (test passes for `hitsHub: true`, and the other case does not move it).
- Compiler-proof capture (`capture-builds.txt`, logs in the scratchpad `vsa-logs`): five binary logs, 0 warnings, 0 errors. Maintainability gate (`maintainability-gate.txt`): PASS, both canary PASS lines, `Coupling dependencies: contract=4671, model=4, intrusive=0; violations=0`. Registration check (`registration-check.txt`): 17 + 67 + 192 + 97 + 394 = 767 passed, 0 failed, 0 skipped.
- Scoped affected tests (`scoped-affected-tests.txt`): 830 run, 829 passed, **1 failed**, 0 skipped.

## E4 Stop: a test outside the contract's expected changes fails (24 November 2026)

`BalloonTests.AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie` (`BalloonTests.cs:118`–`:137`) fails at its last assertion, "After losing lift, the empty carrier must fall below the ceiling" (`Y(Body(h)) < 1`). The Balloon (collider radius 0.36 m, more than the eye's 0.1725 m opening) is tied by a rope to a Rope anchor at (0, 1, −0.18), so its tie and the anchor's eye centre (0, 1, 0) lie on one vertical line. After the envelope breaks the carrier falls along that line onto the anchor's eye, which is now solid, and rests on it near y = 1 + √(0.3875² − 0.2²) = 1.33 m (derived, not measured). Before part A the eye had no collider and the carrier fell through it. The contract names `BalloonTests` as an unchanged retained-behaviour test (AC3) and says no test places a dynamic body over the new tori; this one does. Stopped per the dispatch instruction; nothing was changed in response. Remaining owner work not yet done: the lever and trampoline element documents, `deferred-work.md`, `delivery-batches.md`, `TODO.md`, the contract's status, Implementation Notes and table, `git diff --check`, and the full round.

## E6 Measurement step for the coordinator (24 November 2026)

Read-only: the E4 figure was wrong, and the corrected facts are below. Raw output: `balloon-probe.txt`, `full-round-1.txt`, `capture-builds-2.txt`.

- **Eye orientation.** The anchor in the test has identity rotation. `Visual_Eye`'s transform (basis written row by row, `1 0 0 / 0 0 1 / 0 -1 0`) turns the torus axis onto Z, so the eye is an upright hoop in the XY plane, centre world (0, 1, 0), centreline radius 0.2, tube top at y 1.2275. It is not the flat `Back` pose of the drop case. The mount lies behind it at z −0.38 to −0.22.
- **Probe** (a throwaway fact, scene of the test exactly: anchor (0, 1, −0.18), Balloon (0, 3, 0), mass 2, buoyancy 16, ceiling wall at y 5, 5 m rope; deleted afterwards). `IntegrityBroken` is at tick 86. The carrier's collider shrinks with its gas: it rests with its centre 0.0897 m above the hoop's centreline top, so its radius there is 0.0622 m, not 0.36 m (E4 assumed the full 0.36 m radius and was wrong).

| Tick | x, z | y | Speed |
|---|---|---|---|
| 300 | 0, 0 | 1.3640 | 0.629 m/s, falling |
| 400 | 0, 0 | 1.2897 | 0 |
| 500, 600, 700, 800, 900, 960, 1200, 1500, 1800 | 0, 0 | 1.2897 | 0 |

- **Outcome.** The carrier comes to rest by tick 400 and stays at rest to tick 1800, balanced exactly on the top of the hoop (x and z exactly 0, y 1.2897 = 1.2 + 0.0897), touching the eye's tube; it is 0.2205 m from the mount's box, clear of it. It does not roll off and does not fall through. The test's assertion `Y(Body(h)) < 1` therefore fails because the carrier stays 0.29 m above the anchor's centre.
- **Full local round on the current candidate** with compiler logs recaptured on the current sources (`capture-builds-2.txt`, five logs, 0 warnings, 0 errors): WorkerAotGuard.Tests 17, BundleElements.Tests 287, Coverage.Tests 115, E2eRunner.Tests 256, CuriousContraptions.tests 6634 (6633 passed, 1 failed); total 7309 = baseline 7294 + 15; `Skipped: 0` in every project. The one failure is `BalloonTests.AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie`, message "After losing lift, the empty carrier must fall below the ceiling." (`BalloonTests.cs:136`). It places a dynamic body (the Balloon carrier) over the Rope anchor's new eye. No test depends on the removed Fan `Arrow` and `Tip`, and no other test fails.

## E7 Handoff on the repaired candidate (24 November 2026)

Raw output under `.tools/visible-solid-remainder-a-evidence/`.

- Repair: `BalloonTests.cs:136` only, as the amended contract specifies (`Y(Body(h)) < 1.5875f`, with the derivation in a one-line comment and the message the contract names).
- Compiler-proof capture (`capture-builds-3.txt`): five binary logs, 0 warnings, 0 errors, pinned toolchain.
- Maintainability gate (`maintainability-gate-2.txt`): PASS, both canary PASS lines, `Coupling dependencies: contract=4671, model=4, intrusive=0; violations=0`.
- Registration check (`registration-check-2.txt`): 17 + 67 + 192 + 97 + 394 = 767 passed, 0 failed, 0 skipped.
- Scoped affected tests (`scoped-affected-tests-2.txt`): 830 passed, 0 failed, 0 skipped. Every table test appears: the visible-solid theory has 38 cases (33 + the five new rows), the four new drop cases, the anchor control fact, the lever print fact, the three stream cases, the Fan arrow fact, the four hub cases and the Balloon test.
- AC2 (`ac2-comparison-2.txt`): unchanged; exactly `rope_anchor` 1 to 2 colliders, `pulley` 2 to 4 and `fan` 1 to 3 colliders with occluders 2 to 4.
- The run log is the one new file; `git add -N` was run on it and `git diff --check 0959e8834bacd5dd82226aa737c78ed3afb796e3` printed nothing.
- Full local round under `caffeinate -i` (`full-round-2.txt`): WorkerAotGuard.Tests 17, BundleElements.Tests 287, Coverage.Tests 115, E2eRunner.Tests 256, CuriousContraptions.tests 6634; total 7309 = baseline 7294 + 15; 0 failed and `Skipped: 0` in every project.

## E8 Repair of review pass 1 (24 November 2026)

Raw output under `.tools/visible-solid-remainder-a-evidence/`. Tests and docs only; no product change.

- P1 and P6: `TennisBallTests.Ball` gained an optional last parameter `z` (existing callers unchanged); the drop theory's `SmallTennis` passes `at.Z`; `FanIntegrationTests.Tennis` is deleted and its three call sites use `TennisBallTests.Ball`. `DroppedBall.Penetration` and the class constant share one `PenetrationShare` (0.05).
- P2: `CAT-058` now says the Balloon test only bounds the emptied carrier and names the `AnchorEye` drop case and the A1 control as the proof of the eye's solidity.
- P3: the lever print fact holds extents and positions to 0.5 mm (`visible`, the print stand-off bound); its separate 10 micrometre flatness assertion was removed because the height check already keeps every vertex within 0.5 mm of the face. `CAT-034` and the A7a row say 0.5 mm.
- P4: `BalloonTests` now names `eyeY`, `eyeRadius`, `eyeTube` and `carrierRadius` and bounds the carrier at their sum (1.5875); the scene and message are unchanged, and the carrier radius is passed explicitly (0.36, the old default).
- P5: the test is `AirflowRuntimeTests.OnlyExactInternalCollidersAreExemptFromOcclusion`: `AirflowRuntimeTests` is a partial class whose files include `AirflowGeneralityTests.cs`, so the old filter term `~AirflowGeneralityTests` selected nothing. The contract (Capability probe, A9 row, Commands filter) and `CAT-028` are corrected, and the filter term is replaced by `~AirflowRuntimeTests`. In the scoped run it appears by name, 2 cases passed. `BaseballTests` has no case in the run (the Baseball repair has not published, as the A9 row allows).
- Compiler-proof capture `capture-builds-4.txt` (0 warnings, 0 errors); gate `maintainability-gate-3.txt` PASS with both canary lines and `Coupling dependencies: contract=4671, model=4, intrusive=0; violations=0`; registration check `registration-check-3.txt` 767 passed, 0 failed, 0 skipped; scoped run `scoped-affected-tests-3.txt` 900 passed (every table test by name: 38 table cases, 4 drop cases, anchor control, lever print, 3 stream cases, Fan arrow, 4 hub cases, the Balloon test); AC2 `ac2-comparison-3.txt` unchanged (three entries); `git diff --check 0959e883` prints nothing.
- `git fetch`: `origin/main` is still `0959e883`, so the merge of main is a no-op.
- Full local round (`full-round-3.txt`): 17 + 287 + 115 + 256 + 6634 = 7309, 0 failed, `Skipped: 0` in every project (no test added or removed).

## E9 Repair of review pass 2 (24 November 2026)

Raw output under `.tools/visible-solid-remainder-a-evidence/`. Tests and documentation only; no product change.

- R1: `CAT-058` and D7 now state only what is tested: a body falling onto the anchor's eye meets it; the eye's solidity is the `AnchorEye` drop case and the A1 control for a free ball; the Balloon scene was measured at rest on the hoop's top (E6) and the Balloon test only bounds the carrier at the eye's tube top plus its radius.
- R3: the `BalloonTests` comments say 0.36 is the declared (full) radius, a loose bound because the emptied carrier's collider shrinks with its gas, and that the eye constants mirror `rope_anchor.tscn`. The assertion is unchanged.
- R4: the lever print fact has two named bounds: `standoff` 0.5 mm for the height above the face (as `FaceMarkingTests`) and `lateral` 5 mm for position and extent, sized to the 35 mm wide marks, so it excludes a mark moved, resized or missing. The A7a row and `CAT-034` say so.
- R5: the contract's Implementation Notes no longer quote a file or line count.
- R2 and R6, by script outside the tree (`citation-plan.tsv`, `doc-edits.tsv`, `bare-citations.txt`): each citation of `FanIntegrationTests.cs`, `BalloonTests.cs` or `VisibleSolidColliderTests.cs` in the documents under `docs/` outside `readiness/audits/` was mapped from the baseline line to the current line through the diff of the test file (a line the slice changed would have been reported for manual repair; none was), and every `AirflowGeneralityTests.<Method>` citation of a method of the partial class `AirflowRuntimeTests` was renamed. Verified by re-running the checks: no old line number and no `AirflowGeneralityTests.<Method>` remains outside the audits. Two citations were already wrong at the baseline and are untouched (`CAT-064` `TennisBallTests` `:164`, `EL-125` `BalloonTests` `:78`). Left unchanged on purpose: `readiness/audits/` (historical evidence), this contract's own citations (they describe baseline lines), and the contracts of parts B and C, `spec-engine-art-solidity.md` and `spec-s3-remaining-per-package-lists.md`, which cite moved lines of `VisibleSolidColliderTests.cs` (`:241`, `:249`, `:162`, `:194`, `:195`, `:279`, `:294`) and `BalloonTests.cs` (`:151`, `:152`): their owners must refresh them.
- Files changed by R2 and R6, under `docs/planning/elements/`: CAT-001, 003, 006, 007, 008, 009, 010, 011, 012, 014, 018, 021, 029, 031, 032, 036, 038, 040, 041, 042, 043, 044, 045, 046, 047, 051, 055, 056, 057, 059, 067, 068, 069, 072; EL-001, 029, 037, 042, 044, 045, 051, 052, 070, 071, 072, 073, 074, 089, 090, 092, 093, 100, 103, 104, 105, 106, 108, 126, 138, 143, 146, 153, 154, 155, 158, 173, 174, 175, 176, 187, 188, 190, 191, 195, 199, 204, 209, 210, 212, 213, 216, 242-45; RAD-01, 02, 03, 06, 07, 08, 09, 10, 12, 13, 14, 15, 16, 17, 20, 21, 22; TH-04, 10, 12, 20; `readiness/batch-c.md`, `batch-g.md`, `batch-m.md`, `completion-ledger.md`; and, from earlier repairs, CAT-028, 034, 053, 058 and 065.
- Handoff on the changed candidate: capture `capture-builds-5.txt` (0 warnings, 0 errors); gate `maintainability-gate-4.txt` PASS with both canary lines and `Coupling dependencies: contract=4671, model=4, intrusive=0; violations=0`; registration check `registration-check-4.txt` 767 passed, 0 failed, 0 skipped; scoped run `scoped-affected-tests-4.txt` 900 passed, 0 failed; AC2 `ac2-comparison-4.txt` unchanged (three entries); `git diff --check 0959e883` prints nothing (`diff-check-4.txt`); `git fetch`: `origin/main` is still `0959e883` (`fetch-4.txt`).
- Full local round (`full-round-4.txt`): 17 + 287 + 115 + 256 + 6634 = 7309, 0 failed, `Skipped: 0` in every project; the count is unchanged.

## E5 Coordinator: stop (24 November 2026)

Implementation owner is agent `a6d62335ede1659a8` (fresh general-purpose subagent, no inherited history). It stopped at E4 under the dispatch rule that any test outside the contract's expected changes failing is a stop. The decisions file does not settle the Balloon test or a tie through the eye. The partial candidate stays uncommitted in this checkout; no review, full round, commit or push ran. The contract needs a planning decision: A9 and AC3 say `BalloonTests` passes unedited, and the Code Map says no test places a dynamic body over the new tori.

## E10 Coordinator: review (24 November 2026)

- Amendment: `bad_spec` (coordinator decision, Spec Change Log); planning re-audit agent `ad94e1664f5b39e18`, Incomplete on the first read (amendment not yet written; the lower-bound assertion fragile on an exact-symmetry balance), Scoped Pass after the single-bound assertion and the written edits. Preserved baseline and owner `a6d62335ede1659a8`.
- Tier engine; launches by `general-purpose` subagents with no inherited history (fresh context, inherited model). Pass 1 (inputs staged under `.tools/visible-solid-remainder-a-evidence/review-inputs/`; blind diff without `_bmad-output/` and `TODO.md`): Blind Hunter `a345641ba43bae1eb`, Edge Case Hunter `a220007c013d2f3a9`, Verification Gap `ace86ce6707528580`, Intent Alignment `a7f96d7eee8515cbf`, Forward-Only `a903bc4cb0c1ec54e`, Element-Free `ada1d52faece504b4`. Pass 2: Verification Gap `a96abaa8280931c31`, Forward-Only `ad90b63cc2164bb3f`, Element-Free `aea4bd2c37d9bf63f`. Pass 3: Verification Gap `ae9ddad7ac9c43b92`, Forward-Only `a277b4af7bd0feeff`, Element-Free `ab4d271bb66560356`; then Blind Hunter `ad4dbe0ddd174c68c` and Edge Case Hunter `a8a7154018c3b91fb` on the cumulative test and scene repair.
- Findings and routes are in the contract's Review Triage Log, one line per pass. Every confirmed finding was a patch (test or document); none was `bad_spec`; none deferred under Re-review scope.
- Stagnation note: the lever print tolerance was set to 2 micrometres, then 0.5 mm, then a 5 mm lateral and 0.5 mm stand-off split; the last form stays (it is the contract's same-extent-and-position requirement sized to the marks) and a later Blind Hunter objection to it was rejected with that reason.
- Coordinator housekeeping: two contract edits were first validated but not written (the amendment, then the triage log); both were found by the next read and written. No effect on the candidate.

## E11 Close note (24 November 2026)

- Recurring finding classes from earlier slices: stale cited line numbers and test-class names in planning documents (stale citations recurred across slices; here 107 documents were refreshed); a planning footprint search that named placements but not what a placed body does after an event.
- Unresolved or cause-unknown: none. Added to `deferred-work.md`: stale citations in the unpublished contracts of parts B and C, `spec-engine-art-solidity.md` and S3; the exact-symmetry Balloon balance on the eye.
- Capability found missing late: none; the one late discovery was a test outcome (a body falling onto the new eye), handled by the amendment.
- Card line for the next policy change: "At planning, search for every test that places a dynamic body anywhere near a new collider, including bodies that fall onto it after an event such as a rupture, and run the full round once before review to enumerate failures."

## E12 Publication receipt (coordinator, 24 November 2026)

SnapshotApproval Pass by an independent subagent on 37fbd3a48710306347b1660039d4ed142f0eeb3d, no blocking finding; full local round 7309/0/0, `Skipped: 0`; fast-forward push from 0959e883; remote build, artifact and deployment unverified (owner instruction of 29 October).
