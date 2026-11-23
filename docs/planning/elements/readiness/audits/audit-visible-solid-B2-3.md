# Planning audit 3: ELEMENTS-VISIBLE-SOLID-REMAINDER part B2 (Tennis ball)

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-b2.md` (4,343 words). Checkout `spec-drafts` at published main `25df3cd5` plus uncommitted planning work. Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`. Phase: planning; read-only; nothing built or run. Audited independently and as one serial set with parts A, B, C and the Baseball repair; earlier passes (`audit-visible-solid-B2.md`, `-B2-2.md`) were not relied on.

## Verdict: Fail

Two defects sit inside `<intent-contract>`: the Never clause forbids what S1 and S2 do (F1), and T5 leaves one test edit to the outcome of a run (F2). Both have exact text fixes. The seam geometry, the bindings, admission at both radius bounds, the matcher arithmetic and every cited line hold at `25df3cd5`. After F1, F2 and R1–R4 the contract is ready for development, with line anchors re-read on the main that publishes part B.

## What was verified at `25df3cd5`

- `tennis.tscn` 16–19 (seam 0.245/0.253), 31 (45° about X), 35 (`col_0_0`); `TennisBallElementDeclaration.cs:18` (radius 1/16 to 16, default 0.25), `:24` (Visual scale 4 · radius), `:33–38` (bindings).
- Redrawn seam 31/128 and 33/128 scaled by 4r: centreline r, tube r/32. At 1/16 m: tube 1/512, centreline 1/16, both on the inclusive floors (`PhysicsDeclarationValues.cs:261–267`, `Range` `:323–329`); at 16 m: centreline on the ceiling. Both binding products are exact in f32. `BundleValidator.cs:419–427` and `BundleGeometry.cs:28–46` behave as cited.
- Matcher: `Tol` `:33`, `AxisTol` `:37`, mesh-error bound `:206`, torus arm `:288–290`, `Meet` `:682`. At 16 m the seam's Y and Z basis columns differ from 64 by f32 rounding, so S1 is needed; the X column is exact, so the compared centreline and tube (16, 0.5) are exact. Default 64-ring seam at 16 m: 22 mm faceting against S2's 33 mm (0.2 % of 16.5 m).
- Refusal premise: `ValidateHollowPlacement` (`WorkshopPhysicsCompiler.cs:110`, `:498–523`), `PlacementSlop` 1e-4 (`HollowContactQuery.cs:14`). Seams reach 0.2578 m along X, so the 0.51 m pairs (`TennisBallTests.cs:121`; `BaseballTests.cs:88` in `Impact` `:83`, called from `:64`) overlap by 5.6 mm; 0.53 m clears. The Baseball striker clears by 2.2 mm.
- Borrowed-sphere fixtures `GasRuntimeTests.cs:20–24` and `ParameterizedBodyIntegrationTests.cs:20–26` drop or replace the bindings, as stated; `FanIntegrationTests.cs:134`, `FanAdmissionTests.cs:295`, `WindmillAdmissionTests.cs:179` keep them. `TennisArtTests.cs:25`, `:34`; `TennisBallTests.cs:45`, `:135`, `:148–149`.
- Other multi-Tennis placements: `TennisInteractionTests.cs:35` (0.8 m apart), `FanAdmissionTests.cs:134–135` (2 m apart): clear.
- T1/T2: a 2 m ball turned +45° about X stands the seam upright (rest 2.0625 above the Wall top); unturned, the seam's lowest point is 1.48 m below the centre; bands 45 mm apart. Restitution 0.85 × 1.
- Delta +5 (1 row, 2 drops, 2 bound cases); AC2 as stated.
- Decisions: no conflict (Tennis is not a strand; runs before the programme under owner 22).

## Blocking findings

### F1. The Never clause forbids the slice's own matcher change

- Where: line 30, "A widened bound in an existing test"; against lines 103–106 (S1 relaxes the torus uniformity guard to `Tol · max(1, s.X)`; S2 raises the mesh-error bound to 0.2 % of the drawn radius above 2.5 m), both in the existing `VisibleSolidColliderTests`.
- Consequence: the read-only intent contradicts its Code Map; a reviewer applying the Never clause literally fails the slice.
- Fix: replace "A widened bound in an existing test." with "A widened bound in any existing test, except the two size-relative matcher bounds S1 and S2 of `VisibleSolidColliderTests`, which are numerically unchanged below a scale of 1 and a drawn radius of 2.5 m."
- Classification: wrong specified behaviour (intent text).

### F2. T5 decides a test edit by whether the test fails

- Where: line 40 (T5, "and, only if the scoped run fails it, the small radius of the contact-spin case"); also lines 63, 111 and 188.
- Defect: the contract's own geometry settles the case. `TennisBallTests.ConfiguredRadiusChangesContactSpinAtFixedMass(.125f)` rolls about 79° in its 30 ticks and the seam presses on the bench from 69.5°, so the case no longer measures a smooth sphere's contact lever arm, whatever its outcome. Editing only on failure is choosing the input to fit observed output (`docs/testing-guidance.md`, float and effort rules), and the read-only intent would carry an unresolved condition.
- Fix: T5, replace "and, only if the scoped run fails it, the small radius of the contact-spin case" with "and the small radius of the contact-spin case, 0.125 m to 0.1875 m (it rolls about 55° in its window, before the seam meets the bench at 69.5°)". Line 63: drop "only if the scoped run fails the 0.125 m case:". Line 111: replace "If the scoped run fails it, the small case becomes" with "The small case becomes". Line 188: move the case from "At risk, unedited unless the scoped run fails it" to the edited list. Bounds at `:148–149` unchanged.
- Classification: test.

## Required fixes (outside the intent)

### R1. Name part A's Tennis probes

- Where: row T5 (line 189), which names only "part A's planned Fan stream tests".
- Fix: list `VisibleSolidColliderTests.ASmallBallPassesThroughTheAnchorEyeOntoTheMount` (radius 0.1: seam outer 0.103 m against the 0.1725 m opening; lands on felt, seam's lowest point 0.074 m below the centre), `FanIntegrationTests.PassiveHubReceivesExternalContactWithoutEmittingUnpaidFlow` as re-authored by part A (radius 0.1; the hub contact direction is about 70° from the seam's plane) and `FanIntegrationTests.TheFansOwnRingAndPostDoNotShadowItsStream` (radius 0.0625). The filter already matches them.
- Classification: test.

### R2. Baseline precondition; the cited deferred entry is not on main

- Where: lines 48–50, 64, 86, 148 cite the Repairs table, owner decisions 22 and 24g and `deferred-work.md#airflow-receiver-self-shadowing`; at `25df3cd5` none exists (the entry is uncommitted, `deferred-work.md:255–259` in this checkout).
- Fix: add to **Baseline**: "Precondition: the planning commit that adds `docs/planning/elements/readiness/`, the Repairs table and the `airflow-receiver-self-shadowing` entry is on main, and part B is published; the coordinator captures the baseline at or after both."
- Classification: wrong specified behaviour (Code Map).

### R3. Stale record lines

- Line 52: replace with "Planning audit 3 (`.tools/audit/audit-visible-solid-B2-3.md`, 23 November 2026): Fail on F1 and F2; ready after F1, F2 and R1–R4, line anchors re-read on part B's published main."
- Line 50: "unchanged ... except `BaseballTests.cs`" is false for `FanGuidanceTests.cs`, `PackageRuntimeParityTests.cs` and `WorkshopIntegrationHarnessTests.cs` (Baseball rows at `fa86969f`), all named in T5; say "every file cited with a line number".

### R4. The airflow consequence on the main Fan payload

- Where: line 114 and the "Narrower, accepted" probe item (line 86).
- Defect: stated as "a turned ball can briefly lose part of a fan's push". The Tennis ball is the payload of 22 planned Fan lessons (23 records, `content/puzzles.json`), and the redrawn seam is 2.6 times prouder than today's. The deferred entry names no lesson-level check.
- Fix: add to line 114: "Each early level proof of a Fan lesson that carries a Tennis ball through a stream re-checks its delivery margin against this loss; the entry `airflow-receiver-self-shadowing` names that check." and add the same sentence to that entry in task 6.
- Classification: test (recorded consequence).

## Serialisation

After parts A and B, before part C; serialised with the Baseball repair in either order. The Baseball repair's text matches this contract: if it publishes first, the `BaseballTests` edit is void; if this part publishes first, the repair deletes the edited helper and re-reads R3–R5 against the seamed ball (R4's authored pose clears the sample rays by 0.354 r − r/32; R5's Tennis pair is 1 m apart). Rows 40 → 41. Part A's Tennis probes are in R1. No overlap with S3, S5, S6 beyond `TODO.md` and `delivery-batches.md`.

## Decisions for the owner

Surface, not blocking: the coordinator's seam redraw changes the look of a delivered part (7.8 mm proud at the default size, 0.5 m at 16 m) and, with R4, slightly weakens Fan transport of a turned Tennis ball in every planned Fan lesson. The contract lists it "for the owner's information"; it should be put to the owner with the alternative (lowering the engine's torus floor) before the slice starts.

## Evidence limits

- Nothing was built or run. The predicted refusals, the 16 m axis agreement and the clear-bore Windmill probe (2 mm wider across the seam at 1/16 m) are unexecuted.
- Whether `HollowContactQuery.Overlaps` refuses seam-to-seam starts (hollow partner, distance-only contacts) was read, not run; the 0.53 m spacing holds either way.
