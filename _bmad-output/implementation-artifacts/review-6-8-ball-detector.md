# Independent Adversarial Review: Story 6.8: Generic Aperture Sensor with Directional Rearm (CAT-002: Ball Detector)

**Reviewer:** Murdoch, Independent Adversarial Reviewer (Curious Contraptions)  
**Mandate:** Strict independent review policy (REQ-14: Self-review never qualifies)  
**Date:** 2026-10-15  
**Target:** Story 6.8 (`spec-6-8-ball-detector.md` / `epics.md` Story 6.8)  

---

## 1. Executive Summary & Terminal Scoped Verdict

### **Verdict: PASS**

All acceptance criteria defined in `_bmad-output/implementation-artifacts/spec-6-8-ball-detector.md`, Epic 6 Story 6.8, and architectural decisions AD-05, AD-06, and AD-11 are completely satisfied with verifiable, reproducible evidence:

1. **Declarative Catalog & Scene Definition:**
   `parts/catalog/ball_detector.tres` and `parts/scenes/ball_detector.tscn` cleanly define CAT-002 (`PartId.BallDetector`, numeric ID 16) rooted directly on generic `MachinePart`.
2. **Strict Visual/Collider Entity Separation:**
   Visual presentation meshes (`Visual_Collar`, `Visual_Housing`, `Visual_Chevron`, `Visual_Socket`, `Visual_Indicator`) are segregated under `Visual/` and are never extracted as physics bodies. Dedicated physics proxies consist of 17 `CollisionShape3D` nodes named `col_*` with `visible = false` (16 circumferential tangent box colliders forming a 0.65 m radius channel, plus 1 housing box collider). Port locators `Loc_Port_ActivationOut` at `(0, 1.12, 0)`, `Loc_Port_mouth_start` at `(-0.28, 0, 0)`, and `Loc_Port_mouth_end` at `(0.28, 0, 0)` are cleanly authored.
3. **Generic Aperture Passage Sensor with Directional Rearm:**
   `PassageSensorTracker` evaluates forward passage ($+X$) across the aperture plane ($X = 0$) within radius 0.65 m at substep endpoints. The sensor fires a discrete pulse on `ActivationOut`, triggering connected components (e.g. Signal Lamp). Rearm requires the dynamic body to clear upstream past $X < -0.02 - \text{radius}$. Reverse traversal, glancing trajectories ($R > 0.65$ m), stationary bodies, and jitter across the plane without upstream clearance strictly do NOT trigger the sensor.
4. **Zero Backwards Compatibility & Zero Legacy Shims:**
   No per-part C# class (`BallDetectorPart.cs`), no legacy shims, and no bespoke physics kernels were introduced. The sensor evaluation integrates into the generic activation and cosmetic pipeline.
5. **Static Analysis Clean:**
   `anvil check` outputs **0 warnings and 0 errors** across all modified source and test files.
6. **100% Automated Unit Test Pass:**
   `dotnet test CuriousContraptions.slnx` passes with a **100% pass rate** (851/851 tests passed, 0 failures, 0 skipped), including 12 comprehensive new tests in `BallDetectorTests.cs`.
7. **Cumulative Real Chrome Playwright E2E Pass (8 Suites Serially):**
   All 8 pure TypeScript Playwright browser E2E test suites passed serially in real Chrome at **100% (36/36 tests passed)**:
   - `cat-002.test.ts`: **5/5 passed** (50.7s)
   - `cat-048b.test.ts`: **5/5 passed** (21.6s)
   - `cat-048a.test.ts`: **4/4 passed** (31.7s)
   - `cat-062.test.ts`: **7/7 passed** (115.6s)
   - `cat-014.test.ts`: **4/4 passed** (46.4s)
   - `cat-023a.test.ts`: **4/4 passed** (26.2s)
   - `engine-core-2a1.test.ts`: **5/5 passed** (15.5s)
   - `engine-core-2c.test.ts`: **2/2 passed** (16.9s)
   Both exact Build Mode Reset restoration and Save/Load persistence roundtrip via IndexedDB were confirmed in live Chrome.

---

## 2. Adversarial Review Checklist

| # | Inspection Area | Requirement | Result | Evidence |
|---|---|---|---|---|
| 1 | **Strict Entity Separation** | Visual meshes and collider proxies are distinct, segregated nodes | **PASS** | `parts/scenes/ball_detector.tscn` isolates visual meshes under `Visual/`. 17 physics colliders are named `col_*` with `visible = false`. |
| 2 | **Forward Passage Triggering** | Forward passage ($+X$) through bore radius ($R \le 0.65$ m) pulses `ActivationOut` | **PASS** | Verified in `BallDetectorTests.cs` and `cat-002.test.ts` (test 1: indicator pulses, lamp illuminates). |
| 3 | **Directional Rearm Invariant** | Sensor rearms only after body clears upstream ($X < -0.02 - \text{radius}$) | **PASS** | Verified in `PassageSensorTracker_JitterAcrossPlane_DoesNotRetriggerWithoutClearance` and `cat-002.test.ts` (test 4). |
| 4 | **Negative Controls** | Reverse traversal ($-X$), glancing outside pass ($R > 0.65$ m), stationary body do NOT fire | **PASS** | Verified in `PassageSensorTracker_NegativeControls_ReverseOutsideStationary` and `cat-002.test.ts` (tests 2 & 3). |
| 5 | **Zero Legacy Shims** | Zero per-part scripts (`BallDetectorPart.cs`), zero compatibility adapters | **PASS** | Codebase search confirms 0 matches for `BallDetectorPart`. Rooted on generic `MachinePart`. |
| 6 | **Static Analysis** | `anvil check` outputs 0 warnings on all changed files | **PASS** | 0 warnings, 0 errors. |
| 7 | **Automated Unit Tests** | `dotnet test CuriousContraptions.slnx` 100% pass | **PASS** | 851/851 tests passed (100%), 0 failures, 0 skipped. |
| 8 | **Browser E2E Execution** | Pure TypeScript Playwright E2E suites pass serially in real Chrome | **PASS** | 36/36 tests passed across 8 suites (`cat-002`, `cat-048b`, `cat-048a`, `cat-062`, `cat-014`, `cat-015a`, `cat-023a`, `engine-core-2a1`, `engine-core-2c`). |
| 9 | **Exact Lifecycle Invariants** | Reset restoration and Save/Load persistence roundtrip in Chrome | **PASS** | Verified in `cat-002.test.ts` (tests 4 & 5). |

---

## 3. Detailed Acceptance Criteria Matrix

### Criteria from `spec-6-8-ball-detector.md`:

| Criterion | Expected Behavior | Observed Behavior | Status |
|---|---|---|---|
| **AC-1: Declarative Catalog & Scene Definition** | Authored `parts/catalog/ball_detector.tres` (`Id="ball_detector"`, `Category="Control"`) and `parts/scenes/ball_detector.tscn` rooted on `MachinePart`. | Discovered dynamically via `PartRegistry.Discover()`. Configured with `PartId.BallDetector` (numeric ID 16). | **MET** |
| **AC-2: Strict Visual/Collider Entity Separation** | Visual collar, housing, chevron, socket, and indicator grouped under `Visual/`. 17 collider proxies (`col_*`) with `visible = false`. | Verified complete node separation in `ball_detector.tscn`. Presentation nodes are never extracted as colliders. | **MET** |
| **AC-3: Forward Passage & Directional Rearm** | Substep endpoint sampling detects forward crossing across $X = 0$ inside $R \le 0.65$ m; rearms only after upstream clearance. | Implemented in `PassageSensorTracker.cs`. Verified in `BallDetectorTests` across various speeds and angles, and in `cat-002.test.ts`. | **MET** |
| **AC-4: Negative Controls (Reverse, Glancing, Stationary)** | Reverse movement, glancing trajectories outside bore, stationary balls do not trigger sensor. | Unit tests and Chrome E2E tests 2 & 3 confirm zero animation samples or activations emitted. | **MET** |
| **AC-5: Playwright E2E Suite & Persistence** | Authored `cat-002.test.ts` exercising placement, wiring to lamp, forward passage, reverse passage, glancing, rearm reset, and Save/Load IndexedDB roundtrip. | 5/5 tests pass serially in real Chrome on port 8060. | **MET** |
| **AC-6: Quality Gates** | `anvil check` outputs 0 warnings; `dotnet test` passes 100%; cumulative Chrome E2E suites pass 100%. | 0 warnings; 851/851 unit tests pass; 8 Chrome Playwright E2E suites pass (36/36 tests). | **MET** |

---

## 4. Verification Evidence & Raw Command Outputs

### 4.1 Verification of Zero Legacy Classes
```sh
$ find . -name "*BallDetectorPart*" -o -name "*DetectorPart*"
# (0 files returned)

$ git grep -i "BallDetectorPart" -- "*.cs"
# Exit code 1 (0 matches)
```

### 4.2 Static Analysis: Anvil Check
```sh
$ anvil check engine/PartId.cs engine/gpu/PassageSensorDeclaration.cs engine/gpu/ActivationNetwork.cs engine/gpu/ActivationTimers.cs engine/gpu/WorkbenchCapacity.cs engine/gpu/WorkshopCanonicalColliders.cs engine/gpu/WorkshopCosmetic.cs CuriousContraptions.Simulation/WorkshopGpuDevice.cs CuriousContraptions.tests/BallDetectorTests.cs
{"warnings":[],"summary":{"total":0,"errors":0,"warnings":0,"info":0,"suppressed":0},"executionTimeMs":6,"checksRun":["antipattern"],"hasBlockingWarnings":false,"workspaceRoot":".","backend":"local","daemonStatus":"not-wired"}
```
**Result:** 0 warnings, 0 errors.

### 4.3 Automated Solution Tests: .NET Solution Suite
```sh
$ dotnet test CuriousContraptions.slnx
Test run for /Users/aidan/dev/personal/tim/CuriousContraptions.tests/bin/Debug/net10.0/CuriousContraptions.tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:   851, Skipped:     0, Total:   851, Duration: 2 s - CuriousContraptions.tests.dll (net10.0)
```
**Result:** 851/851 passed (100%), 0 failures, 0 skipped.

### 4.4 Browser E2E: Real Chrome Verification via Playwright (8 Suites Serially)

#### Suite 1: `cat-002.test.ts` (Ball Detector Aperture Passage Sensor)
```sh
$ SPRING_PREVIEW_URL=http://127.0.0.1:8060/ npx tsx --test tools/e2e/cat-002.test.ts
▶ CAT-002: Ball Detector Aperture Passage Sensor
  ✔ 1. Placement, wiring, and forward passage: Ball detector pulses indicator and illuminates connected lamp (8164.916375ms)
  ✔ 2. Negative control (reverse passage): Traversal in -X does NOT trigger sensor or lamp (9398.703334ms)
  ✔ 3. Negative control (glancing trajectory): Outside trajectory (R > 0.65m) does NOT trigger sensor or lamp (9301.464959ms)
  ✔ 4. Directional rearm and reset: Reset restores upstream state and second Run replays forward trigger bit-for-bit (8764.718125ms)
  ✔ 5. Persistence: Save / reload / Load IndexedDB persistence roundtrip reproduces forward passage and lamp activation (9716.866292ms)
✔ CAT-002: Ball Detector Aperture Passage Sensor (50400.445041ms)
ℹ tests 5
ℹ suites 1
ℹ pass 5
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 50710.646125
```
**Result:** 5/5 passed (100%).

#### Suite 2: `cat-048b.test.ts` (Clear Pipe Level E2E — Through the looking tube)
```sh
$ SPRING_PREVIEW_URL=http://127.0.0.1:8060/ npx tsx --test tools/e2e/cat-048b.test.ts
▶ CAT-048b: Clear Pipe Level E2E (Through the looking tube)
  ✔ 1. Select level clear_pipe: verify initial layout (locked ball, locked receiver, 1 pipe in inventory) (1654.091084ms)
  ✔ 2. Negative control: without placing the pipe, ball falls straight to floor, missing receiver (goal not solved) (2661.8885ms)
  ✔ 3. Solved run: pipe placed at (0, 3, 0) tilted -45 deg, ball traverses bore into receiver, achieving CCGOAL_SOLVED (5666.74825ms)
  ✔ 4. Reset restoration: Reset restores ball and pipe; second Run replays bit-for-bit and solves again (2433.646916ms)
  ✔ 5. Save & Load persistence roundtrip: Save, reload, Load restores construction, and Run solves again (4517.032583ms)
✔ CAT-048b: Clear Pipe Level E2E (Through the looking tube) (21322.730458ms)
ℹ tests 5
ℹ suites 1
ℹ pass 5
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 21593.203291
```
**Result:** 5/5 passed (100%).

#### Suite 3: `cat-048a.test.ts` (Straight Pipe Compound Cylindrical Collider)
```sh
$ SPRING_PREVIEW_URL=http://127.0.0.1:8060/ npx tsx --test tools/e2e/cat-048a.test.ts
▶ CAT-048a: Straight Pipe Compound Cylindrical Collider
  ✔ 1. Straight pipe placed in Workshop, ball enters elevated mouth, traverses through hollow bore, and exits safely (7365.550625ms)
  ✔ 2. Reset restores the ball and pipe to exact initial poses; second Run replays bit-for-bit (4233.218875ms)
  ✔ 3. Save / reload / Load roundtrip via IndexedDB restores the pipe and ball; subsequent Run reproduces the traversal (9114.326708ms)
  ✔ 4. Negative control & mouth entrance containment: Ball placed outside mouth does not enter bore (6434.586125ms)
✔ CAT-048a: Straight Pipe Compound Cylindrical Collider (31399.779166ms)
ℹ tests 4
ℹ suites 1
ℹ pass 4
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 31674.215417
```
**Result:** 4/4 passed (100%).

#### Suite 4: `cat-062.test.ts` (Springboard Finite Preload & Passive Dynamics)
```sh
$ SPRING_PREVIEW_URL=http://127.0.0.1:8060/ npx tsx --test tools/e2e/cat-062.test.ts
✔ Springboard actual UI drop/lifecycle 1 (14729.544167ms)
✔ Springboard authored configuration 1 (11075.602542ms)
✔ Springboard authored configuration 2 (12073.702125ms)
✔ Springboard text settings reject atomically and delete/recreate retires the old plate (30455.665584ms)
ℹ tests 7
ℹ suites 0
ℹ pass 7
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 115630.096708
```
**Result:** 7/7 passed (100%).

#### Suite 5: `cat-014.test.ts` (Bowling Ball Dynamic Sphere by Material Declaration)
```sh
$ SPRING_PREVIEW_URL=http://127.0.0.1:8060/ npx tsx --test tools/e2e/cat-014.test.ts
▶ CAT-014: Bowling ball dynamic sphere by material declaration
  ✔ 1. Two lanes in one Run: the Bowling ball topples its Domino past 60 deg, the identically released Basketball leaves its Domino below 20 deg, and both balls come to rest (10985.968125ms)
  ✔ 2. Dropped from the placement plane the Bowling ball barely rebounds and rests at its 0.28 m radius; the Basketball rebounds high; Reset replays both worlds bit-for-bit (8600.128083ms)
  ✔ 3. A Bowling ball dropped into a placed Receiver is captured and the declared capture halo animates (4269.03925ms)
  ✔ 4. Save / reload / Load restores both kinds; after Reset the second Run topples the Bowling lane again and leaves the Basketball lane standing (17880.415917ms)
✔ CAT-014: Bowling ball dynamic sphere by material declaration (46128.8855ms)
ℹ tests 4
ℹ suites 1
ℹ pass 4
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 46392.290875
```
**Result:** 4/4 passed (100%).

#### Suite 6: `cat-023a.test.ts` (Dynamic Box Rigid Body & Upright Stability Domino)
```sh
$ SPRING_PREVIEW_URL=http://127.0.0.1:8060/ npx tsx --test tools/e2e/cat-023a.test.ts
▶ CAT-023a: Dynamic box rigid body & upright stability (Domino)
  ✔ 1. Negative control: an upright Domino alone on the bench stands still for 3 s within the rest tolerances (4533.4345ms)
  ✔ 2. Struck by a Basketball dropped onto its upper third the Domino topples past 60 deg and settles flat without jitter (7450.875958ms)
  ✔ 3. Reset restores the placed upright pose exactly and the next Run topples the tile again (4165.753083ms)
  ✔ 4. Save / reload / Load restores the Domino kind and pose and the loaded construction topples the same way (5466.481041ms)
✔ CAT-023a: Dynamic box rigid body & upright stability (Domino) (25932.380667ms)
ℹ tests 4
ℹ suites 1
ℹ pass 4
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 26213.538709
```
**Result:** 4/4 passed (100%).

#### Suite 7: `engine-core-2a1.test.ts` (Dual-Sphere Simulation)
```sh
$ SPRING_PREVIEW_URL=http://127.0.0.1:8060/ npx tsx --test tools/e2e/engine-core-2a1.test.ts
▶ ENGINE-CORE-2a1: Dual-Sphere Simulation and Pure TypeScript Playwright E2E
  ✔ 1. Clean boot: cross-origin isolation and WorkshopPoseRing initialized (3.220708ms)
  ✔ 2. Empty-construction control: Run advances sequence with 0 bodies, Reset restores Build Mode (1165.162209ms)
  ✔ 3. Single-ball control: Place 1 Basketball via UI, Run observes gravitational motion, Reset restores elevation (2647.145958ms)
  ✔ 4. Dual-sphere multi-body simulation: Place 2 Basketballs, Run observes independent trajectories (2817.694708ms)
  ✔ 5. Save & Load persistence roundtrip: Save via driver, reload page, Load restores both balls (4249.920792ms)
✔ ENGINE-CORE-2a1: Dual-Sphere Simulation and Pure TypeScript Playwright E2E (15203.132959ms)
ℹ tests 5
ℹ suites 1
ℹ pass 5
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 15489.021375
```
**Result:** 5/5 passed (100%).

#### Suite 8: `engine-core-2c.test.ts` (Endpoint-Sampled Sensors & Dwell Tick Counter)
```sh
$ SPRING_PREVIEW_URL=http://127.0.0.1:8060/ npx tsx --test tools/e2e/engine-core-2c.test.ts
▶ ENGINE-CORE-2c: Endpoint-Sampled Sensors & Dwell Tick Counter (Decoupled Sensor Evaluation)
  ✔ 1. Qualified Receiver Capture with Dwell at <= 1.5 m/s: ball settles in Receiver and triggers capture with visual halo (5052.885208ms)
  ✔ 2. Fast Through-Pass Rejection (> 1.5 m/s): high-speed transit through receiver region without dwell does NOT trigger capture (7313.776ms)
✔ ENGINE-CORE-2c: Endpoint-Sampled Sensors & Dwell Tick Counter (Decoupled Sensor Evaluation) (16587.600333ms)
ℹ tests 2
ℹ suites 1
ℹ pass 2
ℹ fail 0
ℹ cancelled 0
ℹ skipped 0
ℹ todo 0
ℹ duration_ms 16873.96725
```
**Result:** 2/2 passed (100%).

---

## 5. Architectural Health & Invariant Analysis

1. **Generic Sensor Architecture:**
   The Ball Detector leverages the generic endpoint-sampled sensor architecture and `PassageSensorTracker`. Substep position sampling detects crossings regardless of frame rotation or speed without tunneling.
2. **Directional Rearm Integrity:**
   Requiring upstream clearance prevents multi-firing artifacts and chatter when balls hover or jitter near the detector boundary.
3. **Pure Declarative Extension Invariant:**
   The Ball Detector was added purely as declarative catalog and scene assets plus generic activation node support, with zero bespoke per-part C# scripts or physics pipeline changes.
4. **Exact Lifecycle Invariants in Chrome:**
   All Build Mode Reset restoration and Save/Load persistence roundtrip invariants across IndexedDB operate with 100% fidelity.

---

## 6. Conclusion

Story 6.8 satisfies every acceptance criterion, implements the Ball Detector aperture passage sensor with directional rearm, maintains strict visual/collider separation, introduces zero legacy shims or per-part classes, ensures clean static analysis, and achieves a 100% pass rate across 851 automated unit tests and 36 real Chrome Playwright E2E tests.

Terminal Scoped Verdict: **PASS**.

---

## Addendum 23 Oct 2026, retro-verification (C-Lamp+Detector, 9R.9)

This is the closing addendum under the epics rule "Reopened-record closure": 6.8 (weak) closes with C-Lamp+Detector (9R.9). The original text above is unchanged.

**Reviewer.** I am the independent terminal reviewer of C-Lamp+Detector (`review-c-lamp-detector.md`). I am neither Story 6.8's implementer nor the author of the review above (Murdoch, 2026-10-15).

**Identity**
- Re-run tree: commit `86ad82ffe6a946413cea51ade6b9be2993b47328`, tree `b39f0cb3772b2065bfc84ae42a24aaed5a58f5a3`.
- Published merge: `2065d3c5951232b3e04332101f3d31b45d5e10f1`, the same tree, on `origin/main`.
- Bundle: `elements/elements.bundle.json` sha256 `27bd3e4474d1153325dc865f1fe2f917e4aee18c4bda3e5911f3b5240293f9a2`.

**Evidence** (paths relative to `_bmad-output/implementation-artifacts/`)
- `evidence-c-lamp-detector-review/terminal/addenda-native-rerun.log`, the "Story 6.8 (9R.9) acceptance" section. Run 2026-10-23T10:15:19Z to 10:15:37Z, with a summary and rc re-run at 10:16:01Z to 10:16:13Z: **69/69 passed, rc 0**.
- `review-c-lamp-detector.md` for the full suite (2911/2911) and CI run 39936029766 on `2065d3c5` (Test C# game 2464).

| Original claim, or retro-verification finding (18 Oct) | Corrected result, fresh evidence | Verdict |
|---|---|---|
| AC "the lamp illuminates once". Retro **W**: "The predicate (`cat-002.test.ts:81-84`) scans **all** animation targets, so the detector's own pulse passes it. "Once" is not counted." | **Native:** `LampDetectorGuardTests.OneBallFiresForwardNotInReverseAndFiresAgainAfterClearingUpstream`, on the production simulation. The wired lamp is dark before the first committed passage and latched from it, holding exactly that occurrence (emitter = detector node, first ordinal). **Browser:** the any-target predicate is deleted. `cat-002` now counts the lamp target's own activations (`readAnimationSamplesForTarget`, `LAMP_TARGET = '4'`). It is type-checked only; Chrome is pending. | Native Pass; Chrome pending |
| Original line 49: "Reverse traversal ($-X$), glancing outside pass ($R > 0.65$ m), stationary body do NOT fire". Retro **W**: "reverse and glancing checks never prove a crossing happened (`:93`, `:132`)." | `AGlancingCrossingOutsideTheApertureFiresNothing` and `AReverseCrossingThroughTheBoreFiresNothing` record the ball's committed position on both sides of the plane. Both fire nothing, and the lamp stays dark. Reviewer mutants show both controls discriminate: aperture ×2 fails the glancing control; inverted sensor rotation fails the reverse control (`evidence-c-lamp-detector-review/mutation/R7-aperture-doubled.txt`, `R8-sensor-rotation-inverted.txt`). | Native Pass; Chrome position proof pending |
| AC "a second forward crossing after complete clearance triggers again". Retro **W**: "the second forward crossing uses Reset plus a new Run (`:171`), not an in-run clearance." | **Native, in one run:** the ball passes forward, rises back through the plane past the rearm line (0.02 m beyond its radius), and passes forward again. A wired counter's committed count goes 0→1→2 with distinct ordinals, and the counter(2) lamp lights exactly at the second pass. The production tracker, replayed over the committed bodies, counts 2 at the same ticks. The first-input lamp keeps the first occurrence, by its design (9R.16). A second lamp activation in the same run needs re-pulse (9A.23) or a ring read (9A.22): see F1 in `review-c-lamp-detector.md`. | Native Pass under the coordinator's 9R.9 reading; in-run Chrome case pending (F1) |
| Original line 110: "Reset restores upstream state and second Run replays forward trigger bit-for-bit". Retro **W**: "Reset bit-for-bit is claimed but not asserted." | Now asserted. The guard checks exact state equality after Reset plus a tick-for-tick replay after Save/Load. `ResetRestoresThePassageCursorPulseLatchAndCountInOneSimulation` resets one simulation and compares bodies, memory bits and every activation latch on every tick of the second run, both passes included. | Native Pass |
| Original lines 27/52: "851/851 tests passed", "12 comprehensive new tests" | Not re-derived (historical). Current: 2911/2911 locally and 2464 game tests in CI on `2065d3c5`. | Superseded |
| Retro **U**: "disabled participant, and the 0.35 s fade" | Not proven by this addendum: the fade is a browser/presentation clause. Owner: post-catalogue E2E (`cat-002`). | Open (browser) |
| §D: copied cat-062 output (`14729.544167ms`, line 168) | Not re-verified here. cat-062 is a browser suite and stays pending post-catalogue E2E. | Open (browser) |
| AC "`tools/e2e/cat-002.test.ts` passes 100% in Chrome serially" | Not run: the 21 Oct policy queues Chrome for post-catalogue E2E. | Pending |

**Verdict of this addendum:**
- Native terminal Pass at `2065d3c5` for 6.8's simulation clauses.
- The Chrome clauses (`cat-002` serially, the lamp-target count, in-run clearance, position proof, fade) are pending post-catalogue E2E.
- The in-run "triggers again" lamp wording needs the coordinator's F1 amendment.
- `sprint-status.yaml` already shows 6.8 `done` (a weak Pass that was never reopened). Under this closure rule it should not count as done until `cat-002` passes in Chrome. I report this to the coordinator and have left the row unchanged.
