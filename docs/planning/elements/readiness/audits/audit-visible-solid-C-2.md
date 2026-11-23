# Planning audit, second pass: ELEMENTS-VISIBLE-SOLID-REMAINDER, part C

Auditor: `a7b5d5bd5a1ebb019` (planning auditor). Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-c.md` as repaired (4,966 words). Checkout `spec-drafts` at `8d1d68aa`. Phase: planning. Nothing was built or run.

## Verdict: Scoped Pass (blocked only on prerequisites), with one record edit required before dispatch

The verdict is given as it would stand once parts B and B2 are published. No blocking finding remains in the intent contract, the product repairs, the matcher changes or the planned tests. One required-now decision is still written as open in the contract (R1); it is a record edit outside the intent contract, not a design defect. The contract's own text requires a further planning audit at replanning (line 228), where line numbers, `Solid.Inner` and the theory's name are re-read.

## First-pass findings: all closed

| Finding | State |
|---|---|
| B1 ring rule admits a centred box | Closed. Line 119 adds the wall, four-circle, one-surface and centroid conditions; line 207 has three controls. I checked the centroid rule against real ring triangles (wall and end-cap centroids stay outside inner · cos Δ) and against a fan that closes the bore (at least one triangle falls inside). |
| B2 envelope proven by formula | Closed by the C8 production test (line 206), run first with a stop (task 1), and probe rows at lines 83–84. See the reading check below. |
| B3 three intent sentences | Closed: lines 22, 26, 42. The allowance is now per imported mesh (line 120). |
| B4 blocked on prerequisite | Closed: frontmatter `blocked`, line 53, line 228. |
| Eight smaller items | Closed: lines 109, 121, 160, 167, 172, 208, 218, 220. |

## The author's reading of the Balloon presentation path: verified at each cited point

- **The Animation worker advances whenever a schedule is installed, running or not.** Correct. `Service` returns early only when there is no schedule, a preparation is pending or master time is unavailable; it does not test playback (`CuriousContraptions.Animation.Worker/Program.cs:92–100`). It then emits every declared sample (`:109–114`).
- **A schedule is installed at the seed boundary in build mode.** Correct. `CommitSchedule` picks `PresentationBoundary.Seed` when playback is neither Running nor Completed, and sets `_schedule` (`engine/gpu/BrowserWorkshopClient.Schedule.cs:91–99`).
- **The evaluator takes the committed read at installation and holds the anchor when not running.** Correct. The constructor ends with `ApplyRead(endpoint.Read)` (`engine/gpu/WorkshopDeclaredPresentation.cs:154`); the scheduled `Advance` uses the anchor unless playback is Running (`:645–648`).
- **A sample carrying the committed radius is emitted on every advance.** Correct, and it is what makes the test meaningful: the inner `Advance` adds a sample for every visible track on every call, not only on change (`:665–689`), and the gas-radius input is `OuterRadius(dry, committed volume)` (`:402–405`).
- **The planned test is feasible.** The harness builds its presentation on the first response with a non-zero epoch, in build mode too (`CuriousContraptions.tests/WorkshopIntegrationHarness.cs:301–308`). `ResetPhysicalVisuals` and `ApplyDeclaredProperty` are already called by `CannonArtTests`.

So the 0.0625 m picture is what the part draws from construction or Reset until its first declared sample, as the contract now says. What reading cannot settle is how long that is, and whether the build-mode read carries the gas store; the test settles the second and stops the slice if it fails.

## Required before dispatch

**R1. The contract still records O1 as open (record).** Lines 53 ("awaiting owner decision O1"), 147–152 and 190 present it as awaiting the owner, and line 228 makes replanning wait on it. The coordinator has since decided it under the owner's standing delegation: answer (b) if C8's test shows the committed radius in build mode and after Reset, otherwise stop and repair first as an engine slice. The protocol does not allow planning to be ready while a required-now decision is written as open. Fix, all outside the intent contract:

- Line 53: delete "and **awaiting owner decision O1** (below)".
- Line 137: add "O1 is decided: (b) if C8's test shows the committed radius in build mode and after Reset; otherwise the slice stops and the projection is repaired first as an engine slice."
- Lines 147–152: retitle "Decision O1 (coordinator, 22 November 2026, under the owner's standing delegation)" and keep the two outcomes as the rule.
- Line 150 and task 7: the deferred-work entry names its owner as well as its destination slice.
- Line 190: drop "O1" from Uncertain. Line 228: drop "and O1 is answered" and "apply O1's answer to C8 and to the intent".

The intent contract needs no change: C8 (line 41) is already written for answer (b), and the stop in task 1 covers the other outcome.

One thing to put in front of the owner, since I cannot confirm consent from here: answer (b) leaves a known visible-is-not-solid picture in place and records it as deferred work. The delegation note I can see excludes waiving a gate. If the owner regards this deferral as one, it is theirs to confirm.

## Non-blocking (fix in the same edit)

1. **Line 127, Cannon reach.** "The Cannon's is unchanged" is not exact: the indicator tops out at y 0.72, above the collars' 0.65, so the bench-volume extent (`engine/PlacementPose.cs:113–116`) grows 7 cm in +y. Write: "the Cannon's reach grows from 0.65 m to 0.72 m in +y (the indicator); z is unchanged". No test places a Cannon or Windmill near a bench limit.
2. **Line 119, the angular gap.** Say "including the gap across the wrap-around". Without it a half ring has a small Δ. The C10 control would catch a wrong implementation, but the rule should say it.
3. **Line 83, what C8 proves.** The test exercises the evaluator and the part's application. The worker loop and the browser client's frame admission (`BrowserWorkshopClient.DeclaredPresentation.cs:133–147`) stay proven by reading only. Add "the worker and client legs are browser qualification, pending".
4. **Line 218.** `git add -N` the contract as well as its run log, if it is untracked at handoff.

## Rechecked and still correct

Classification of all 32 meshes; the four imported rings against their proxies; sleeve enclosure; Cannon colliders 5–8 and the chamber code; the Windmill occluder entry and its need; blade sweep and rim; both drops; AC4's count (3 + 2 + 3 + 2 = 10); AC1's 44 with order A, B, B2, C; engine tier; budget.

## Evidence limits

- Nothing was built or run. Imported vertex storage is unverified; `ImportedStep` covers 16-bit positions.
- I did not trace the simulation side that sends the schedule commit on Construct, nor the client's frame admission while stopped.
- Parts A, B and B2 are drafts. This verdict holds for the current text of part C and must be repeated at replanning.
