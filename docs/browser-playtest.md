# Browser campaign playtest

In progress, 2026-09-27. All 40 levels have completed a direct-UI Balanced reference run; the full difficulty matrix remains incomplete.

**Revised user requirement:** direct 3D UI manipulation, no numeric placement/layer menus, and multiple attempts at every difficulty on all 40 levels. The 24 reference completions below do not satisfy that requirement. The replacement runner and 480-run minimum matrix are described in [tools/Playtest/README.md](../tools/Playtest/README.md).

Direct-run pilot evidence is stored in `docs/playtest-results/`. The first eleven direct-UI attempts recorded these outcomes: all nine level-1 reference/positive/negative cases won; level-1 Forgiving outside-window timed out; level-2 Balanced reference won at tick 116 (0.97 s), including actual wiring clicks. The outside-window ramp stayed at [0.60382605, 8.290041, 0] with an unchanged quaternion across ticks 0–120, while the eligible second ramp corrected. Screenshots accompany the reviewed pilot outcomes; per-case JSON records contain actual input actions and sampled transforms. This is partial evidence, not the full 480-case matrix or a completed bounds/easing/reset audit.

 Chrome through Playwright MCP, 1440×900, local published C# Godot/2dog build at http://127.0.0.1:8060. Default Balanced precision (0.45), surface-friction option off.

## Superseded reference method

Use actual palette, placement, move/rotate handles, optional front-view/layer controls, keyboard rotation shortcuts, wiring and Run/Reset. No injected game state, solution loading, simulated win events, or developer auto-solve. Reference placements guide this QA pass; it is not a blind puzzle-discovery test. A completed row requires visible “It works!” feedback. Native tests do not count as browser playthroughs.

## Reference-pass results (not difficulty-matrix evidence)

| Level | Puzzle | Browser result | Simulation time | Notes |
| --- | --- | --- | --- | --- |
| 1 | First principles | Completed | 2.95 s | Placed two ramps in 3D, tilted with E, lifted/lowered using green movement handle. |
| 2 | A little lightbulb moment | Completed | 0.97 s | Placed and lowered impact switch, connected to lamp; falling bowling ball activated it. |
| 3 | Air mail | Completed after retries | 1.86 s | Fan-only route. Height 3.795195 failed; front-view drag snapped to 3.8 and succeeded. See defect below. |
| 4 | Spring forward | Completed | 2.31 s | Front-view spring placement, four 5° tilts, successful launch into elevated basket. Spring lacks visible compression/rebound. |
| 5 | A different perspective | Completed | 1.18 s | Set depth −3, placed fan, quarter-turn around Y; moved ball from back to front receiver. Front view obscures depth; 3D view should be used when judging the route. |
| 6 | The domino effect | Completed | 1.06 s | Four placed dominoes transmitted the trigger to the end fixture. |
| 7 | Conveyor courier | Completed | 2.34 s | Conveyor delivered the ball into the receiver. |
| 8 | Spring-loaded signal | Completed | 1.72 s | Spring launched into a wired elevated impact switch; lamp lit. |
| 9 | A breath of electricity | Completed | 0.88 s | Fan drove tennis ball onto a wired switch; lamp lit. |
| 10 | Cold start | Completed | 1.86 s | Trigger ball powered initially-off fan through a placed/wired switch; receiver captured tennis ball. |
| 11 | Two deliveries | Completed | 2.95 s | Two separate depth lanes: ramp route and fan route. |
| 12 | Bounce and roll | Completed | 2.95 s | Ramp and spring deliveries. |
| 13 | Air and belt | Completed | 2.34 s | Fan and conveyor deliveries. |
| 14 | Catch and signal | Completed | 2.95 s | Ramp capture plus wired impact switch. |
| 15 | Wind and dominoes | Completed | 1.86 s | Fan capture plus four-domino chain. |
| 16 | Powered post | Completed | 2.95 s | Trigger-powered fan plus ramp delivery. |
| 17 | Spring and chain | Completed | 2.31 s | Spring delivery plus domino chain. |
| 18 | Two signals | Completed | 2.29 s | Two independently wired switches/lamps; wired in 3D to disambiguate depth. |
| 19 | Belt and signal | Completed | 2.34 s | Conveyor capture plus fan-triggered lamp. |
| 20 | Start and topple | Completed | 1.86 s | Trigger-powered fan plus domino chain. |
| 21 | Deep routes | Completed | 2.95 s | Four ramps rotated around Y into opposite depth directions. |
| 22 | Cross breezes | Completed | 1.86 s | Opposing depth-axis fans. |
| 23 | Deep springs | Completed | 2.31 s | Two depth-axis spring launches. |
| 24 | Spatial signals | Completed | 2.29 s | Depth-axis ramps/fan and two independently wired lamps. |
| 25–40 | Remaining campaign | Not yet played | — | Must not be counted as passed. |

Screenshots: `.playwright-mcp/level-01-solved.png`, `level-02-solved.png`, `level-03-solved.png`, `level-04-run.png`, `level-05-solved.png`. Failed level-3 attempt: `.playwright-mcp/level-03-run.png`.

## Findings

- **Fan tolerance — reproduced defect repaired:** the direct runner reproduced a Balanced timeout at fan height 3.7841127. Fan instances now author a 0.15 s correction instead of 0.4 s, retaining the same quintic easing, eligibility windows and correction caps; Precise has no correction. Identical UI placement now wins at tick 223. Samples at ticks 0/4/8/12/16/20 show heights 3.7841127/3.7853267/3.790415/3.7966657/3.7998166/3.8. Positive and negative position/orientation perturbations win at both Forgiving (tick 201) and Balanced (tick 223). Six native height errors at each assisted difficulty also win and restore their original placements on Reset; all 63 tests pass. Full browser Reset, bounds and campaign tolerance coverage remain outstanding.
- **Progression:** trying the level selector immediately after success did not change the level in the first attempt; Reset then explicit popup selection worked. Recheck before diagnosing a disabled-control bug.
- **Animation request:** springboard activation needs compression, rebound and settling animation synchronized with launch; tracked in TODO.md.
- **Direct-run navigation:** a level-2 attempt timed out because the first Down key focuses popup row 1; it does not advance to row 2. Updated the driver to send one Down per requested row and verified level-2 selection/wiring/completion. Later rows and scrolling still need verification.
- **Automation:** wait for game startup before input; separate pointer movement/down/up and allow frames between operations. Popup selection is most reliable with visible item coordinates. Fast synthetic actions can obscure input ordering.
- **Front view:** useful for exact height/sideways placement, but overlapping depth lanes require deliberate layer selection and 3D inspection.

## Fan timing regression evidence

The artifact directory now contains 17 records (15 wins, two timeouts), across two puzzle-data hashes. `L03-balanced-reference-direct.json` preserves the failing pre-change run; `L03-balanced-reference-direct-fan-timing.json` preserves the successful identical-input post-change run. Four `L03-*-near-*-direct.json` records cover post-change perturbations, with matching reviewed screenshots. Counts include the historical regression pair and must not be treated as current-build matrix completion. Fan timing is authored in both `content/puzzles.json` and its C# campaign generator; the physics solver and easing formula were not altered.

## Direct-UI Balanced reference pass — all 40 levels

All 40 levels have now been completed through palette clicks, movement arrows, rotation rings, wiring endpoints and Run, with reviewed success screenshots and no recorded browser errors in these successful runs. These are authored-reference QA runs, not blind puzzle solving. Level 3 uses the post-fix fan-timing record. Level 36 first failed during construction at an exact boundary click; the driver now stages boundary parts 0.1 units inside and uses the actual movement arrow to reach the edge, after which the level completed.

| Level | Puzzle | Time | Input/result record |
| --- | --- | --- | --- |
| 1 | First principles | 2.95 s | [JSON](playtest-results/L01-balanced-reference-direct.json) |
| 2 | A little lightbulb moment | 0.97 s | [JSON](playtest-results/L02-balanced-reference-direct.json) |
| 3 | Air mail | 1.86 s | [JSON](playtest-results/L03-balanced-reference-direct-fan-timing.json) |
| 4 | Spring forward | 2.31 s | [JSON](playtest-results/L04-balanced-reference-direct.json) |
| 5 | A different perspective | 1.18 s | [JSON](playtest-results/L05-balanced-reference-direct.json) |
| 6 | The domino effect | 1.06 s | [JSON](playtest-results/L06-balanced-reference-direct.json) |
| 7 | Conveyor courier | 2.34 s | [JSON](playtest-results/L07-balanced-reference-direct.json) |
| 8 | Spring-loaded signal | 1.72 s | [JSON](playtest-results/L08-balanced-reference-direct.json) |
| 9 | A breath of electricity | 0.88 s | [JSON](playtest-results/L09-balanced-reference-direct.json) |
| 10 | Cold start | 1.86 s | [JSON](playtest-results/L10-balanced-reference-direct.json) |
| 11 | Two deliveries | 2.95 s | [JSON](playtest-results/L11-balanced-reference-direct.json) |
| 12 | Bounce and roll | 2.95 s | [JSON](playtest-results/L12-balanced-reference-direct.json) |
| 13 | Air and belt | 2.34 s | [JSON](playtest-results/L13-balanced-reference-direct.json) |
| 14 | Catch and signal | 2.95 s | [JSON](playtest-results/L14-balanced-reference-direct.json) |
| 15 | Wind and dominoes | 1.86 s | [JSON](playtest-results/L15-balanced-reference-direct.json) |
| 16 | Powered post | 2.95 s | [JSON](playtest-results/L16-balanced-reference-direct.json) |
| 17 | Spring and chain | 2.31 s | [JSON](playtest-results/L17-balanced-reference-direct.json) |
| 18 | Two signals | 2.29 s | [JSON](playtest-results/L18-balanced-reference-direct.json) |
| 19 | Belt and signal | 2.34 s | [JSON](playtest-results/L19-balanced-reference-direct.json) |
| 20 | Start and topple | 1.86 s | [JSON](playtest-results/L20-balanced-reference-direct.json) |
| 21 | Deep routes | 2.95 s | [JSON](playtest-results/L21-balanced-reference-direct.json) |
| 22 | Cross breezes | 1.86 s | [JSON](playtest-results/L22-balanced-reference-direct.json) |
| 23 | Deep springs | 2.31 s | [JSON](playtest-results/L23-balanced-reference-direct.json) |
| 24 | Spatial signals | 2.29 s | [JSON](playtest-results/L24-balanced-reference-direct.json) |
| 25 | Three deliveries | 2.95 s | [JSON](playtest-results/L25-balanced-reference-direct.json) |
| 26 | Triple signal | 2.29 s | [JSON](playtest-results/L26-balanced-reference-direct.json) |
| 27 | Cold front | 2.95 s | [JSON](playtest-results/L27-balanced-reference-direct.json) |
| 28 | Chain mail | 2.34 s | [JSON](playtest-results/L28-balanced-reference-direct.json) |
| 29 | Bounce mail | 2.31 s | [JSON](playtest-results/L29-balanced-reference-direct.json) |
| 30 | Relay workshop | 2.29 s | [JSON](playtest-results/L30-balanced-reference-direct.json) |
| 31 | Double cold start | 2.34 s | [JSON](playtest-results/L31-balanced-reference-direct.json) |
| 32 | Triple chain | 1.06 s | [JSON](playtest-results/L32-balanced-reference-direct.json) |
| 33 | Signals and chain | 2.29 s | [JSON](playtest-results/L33-balanced-reference-direct.json) |
| 34 | Cold signals | 2.29 s | [JSON](playtest-results/L34-balanced-reference-direct.json) |
| 35 | Depth delivery office | 2.94 s | [JSON](playtest-results/L35-balanced-reference-direct.json) |
| 36 | Depth telegraph | 2.29 s | [JSON](playtest-results/L36-balanced-reference-direct.json) |
| 37 | Double bridge | 2.34 s | [JSON](playtest-results/L37-balanced-reference-direct.json) |
| 38 | Bridges and signal | 1.72 s | [JSON](playtest-results/L38-balanced-reference-direct.json) |
| 39 | Cold bridges | 1.86 s | [JSON](playtest-results/L39-balanced-reference-direct.json) |
| 40 | The grand contraption | 2.29 s | [JSON](playtest-results/L40-balanced-reference-direct.json) |

Matching screenshots are in `.playwright-mcp/<caseId>.png`. There are 54 outcome records overall (52 wins, two timeouts), including historical and regression runs; this is not 54 completed current-build matrix cells. The full difficulty matrix, independent correction-bounds/easing audit, and browser Reset checks are outstanding.

Playability observations from this pass:

- Foreground ramps and receivers can obscure deeper parts, notably in the combined-route levels. Successful scripted selection does not prove beginner-friendly visibility.
- Three-item inventories require scrolling in the compact palette; scrolled rows can show clipped labels. The runner successfully used the actual scroll control, but discoverability/readability need review.
- Later levels mostly combine independent familiar routes. More simultaneous objectives is not proof of increasing reasoning difficulty or variety.
- Exact build-boundary clicks can be rejected by pixel rounding. The driver workaround avoids this, but a player-facing edge-placement improvement remains desirable.
- On success, the scene stops with some balls still moving and dominoes mid-fall; a short post-success animation period would make the result easier to read.

## Browser Reset and sampled-motion audit

Three fresh level-1 near-positive runs at Forgiving/Balanced/Precise include actual Reset-button clicks and read-only restored transforms. All won and passed the independent C# audit (`dotnet run --project tools/Playtest -- --audit docs/playtest-results/*-reset.json`). Outcome screenshots were reviewed; the Balanced restored screenshot confirms build mode. The audit compares all restored part transforms to run start, including the ball and fixtures.

Implemented checks also cover complete sample cadence, correction caps, zero assistance on Precise/outside all same-kind position windows, and sampled easing. Deliberately corrupted stdin copies verify rejection of excess motion and incorrect Reset transforms; missing Reset is reported incomplete. See the runner README for conservative rotation-bound and assignment-coverage limitations. This remains partial verification, not full nudge-system acceptance. The earlier 54-record count is historical; there are now 57 records.

All 63 native tests remain passing. Normal builds omit the added diagnostic calls. Anvil's pre-write gate required authentication during these edits and returned its allow-with-warning fallback; this was reported while proceeding.

## Levels 2–3: four attempts at every difficulty

Each row covers reference, near-positive, near-negative and outside-window direct-UI attempts, followed by an actual Reset click. All 24 outcome screenshots were reviewed; no browser errors were recorded. Files are `docs/playtest-results/L0{2,3}-<difficulty>-<variant>-direct-reset.json`, with `-outcome.png` screenshots under `.playwright-mcp/`.

| Level | Difficulty | Reference | Positive error | Negative error | Outside window |
| --- | --- | --- | --- | --- | --- |
| 2 | Forgiving | Won, tick 116 | Won, tick 116 | Won, tick 116 | Timeout |
| 2 | Balanced | Won, tick 116 | Won, tick 116 | Won, tick 116 | Timeout |
| 2 | Precise | Won, tick 116 | Won, tick 116 | Won, tick 116 | Timeout |
| 3 | Forgiving | Won, tick 201 | Won, tick 201 | Won, tick 201 | Timeout |
| 3 | Balanced | Won, tick 223 | Won, tick 223 | Won, tick 223 | Timeout |
| 3 | Precise | Timeout | Timeout | Timeout | Timeout |

All 24 pass the implemented sampled-motion/Reset checks. This validates their recorded bounds/easing/ineligibility/Reset observations, not full assistance correctness or puzzle playability. The audit's broader assignment and rotation-eligibility limits remain documented in the runner README.

**Precise usability finding:** all three level-3 reference attempts started the fan at [-4, 3.7841127, 0]. Assisted runs corrected to [-4, 3.8, 0] and won; Precise retained the initial transform and timed out. Small free-drag errors are consequential on this puzzle. A discoverable practical Precise placement path still needs verification/improvement; no hidden Precise correction was added.

The artifact total is now 81 (70 wins, 11 timeouts), including historical/repeated cases. There are 27 Reset-equipped records passing the current audit; [saved audit output](difficulty-audit-2026-09-27.json). Full 480-cell coverage remains incomplete.

## Precise placement: tenth-unit Shift alignment

Movement-arrow Shift snapping now aligns only the active world coordinate to 0.1 units, replacing half-unit displacement snapping. An off-grid start can align to the grid, while other coordinates stay unchanged. The contextual drag status explains the modifier; no button or numeric menu was added. Free dragging, rotation snapping, difficulty settings and simulation physics are unchanged.

`L03-precise-reference-direct-snap-reset.json` records an actual Shift-held green-arrow drag: fan starts at [-4, 3.8, 0], stays unchanged across all recorded samples, and wins at tick 265 (2.21 s). Outcome screenshot reviewed; independent motion/Reset audit passes. The previous free-drag failures remain valid evidence and are not overwritten. This proves one practical keyboard-assisted solution path, not beginner discoverability or mobile precision usability.

All 66 native tests pass, including off-grid alignment on all three axes, preservation of the other coordinates, cancellation and existing undo behavior. There are now 82 outcome records; the full difficulty matrix is still incomplete.

## Level 4: spring difficulty matrix

All twelve level-4 variants now have direct-UI records ending `-direct-reset.json` and reviewed `-outcome.png` screenshots. Construction used palette placement, movement arrows and the rotation ring, with free dragging (no optional Shift movement snap). No game state was injected. All runs had zero recorded browser errors.

| Difficulty | Reference | Near-positive | Near-negative | Outside window |
| --- | ---: | ---: | ---: | ---: |
| Forgiving | Won, tick 265 | Won, tick 265 | Won, tick 265 | Timeout, tick 3600 |
| Balanced | Won, tick 277 | Won, tick 278 | Won, tick 280 | Timeout, tick 3600 |
| Precise | Won, tick 302 | Won, tick 314 | Won, tick 356 | Timeout, tick 3600 |

All three difficulties started the near-positive spring at approximately [-2.916413, 0.78961825, 0.030801335]. Forgiving settled at the authored [-3, 0.8, 0]; Balanced settled at [-2.9909742, 0.79887897, 0.003325997], reflecting its smaller correction cap; Precise retained the initial transform. The outside-window spring remained at [1.0030489, 4.784618, 0] for all difficulties. Actual starting transforms, not recipe offsets, are the evidence.

The small errors also succeeded on Precise, so these outcomes alone do not establish a larger successful placement region. The recorded corrections demonstrate assistance for this spring; the existing native receiver sweep and level-3 browser counterexample provide different, narrower tolerance evidence. None is a substitute for the remaining campaign matrix.

All twelve records pass the independent checker for sampled bounds/easing, Precise/outside-window non-correction, and transform restoration after clicking the real Reset control. Rechecking all Reset-equipped evidence gives 40 passing records. The checker's documented assignment, rotation-eligibility and between-sample rendering limitations still apply.

Screenshots show the expected ball in the basket for successful runs; outside-window runs leave the ball on the solid tabletop. The displaced spring visually overlaps the basket at that camera angle, reinforcing the existing depth-readability finding. The spring's activation animation remains a requested TODO, not something these endpoint screenshots verify.

Current totals: 94 outcome records (80 wins, 14 timeouts), including historical/repeated records; 40 include Reset. Levels 2, 3 and 4 each have their twelve matrix outcomes recorded, but level 3's free-drag Precise reference failed and its separate snapped success does not erase that finding. The full 480-cell matrix remains incomplete.

## Level 5: depth-axis fan matrix

All twelve level-5 variants are recorded as `L05-<difficulty>-<variant>-direct-reset.json`, with reviewed outcome screenshots. The UI-only driver rotated the fan around Y to direct airflow along the depth axis; no numeric placement menus or game-state setters were used. Free movement dragging was retained. All runs recorded zero browser errors.

| Difficulty | Reference | Near-positive | Near-negative | Outside window |
| --- | ---: | ---: | ---: | ---: |
| Forgiving | Won, tick 131 | Won, tick 131 | Won, tick 131 | Timeout, tick 3600 |
| Balanced | Won, tick 141 | Won, tick 141 | Won, tick 141 | Timeout, tick 3600 |
| Precise | Won, tick 154 | Won, tick 197 | Won, tick 154 | Timeout, tick 3600 |

The actual near-positive fan starts at [-0.9117979, 3.7986748, -2.9664342] for all difficulties. Forgiving and Balanced finish their positional correction at [-1, 3.8, -3]; Precise retains its initial position. The outside-window fan starts and remains at [3.0076642, 7.7936745, -3] on all difficulties. The complete position/quaternion samples pass the existing bounded-motion/easing/Reset checker, including zero correction for Precise and the distant placement.

Successful screenshots show the tennis ball at the basket; failed placements leave it on the tabletop. The scene remains readable at the default angle for this sparse arrangement, though the floating fan's height is not obvious from the outcome view alone. Endpoint screenshots do not verify animation fluidity.

As with level 4, all small-error Precise attempts also win. This establishes a playable local region, not a strict win/loss difference between difficulty settings. Faster assisted outcomes cannot by themselves be attributed solely to placement nudges because receiver assistance also changes with difficulty.

All 52 Reset-equipped records pass the implemented audit after this batch. There are 106 total outcome records (89 wins, 17 timeouts), including historical/repeated attempts. Levels 2–5 now each have twelve matrix outcome records; the full 480-case matrix, the audit's documented gaps, and original-physics calibration remain unfinished.

## Reset connection evidence

The diagnostic observer now records the actual directed, typed connection graph at Run and after the real Reset action. The independent C# audit compares those graphs without depending on edge order, rejects duplicates/malformed entries, and reports missing snapshots as incomplete. This adds verification only; gameplay and physics are unchanged. It does not prove that the player constructed the recipe's intended graph, only that Reset preserves the graph actually built.

New direct-UI Balanced reference runs:

- [Level 2](playtest-results/L02-balanced-reference-direct-connections-reset.json): won at tick 116; switch_1 → lamp power link preserved.
- [Level 18](playtest-results/L18-balanced-reference-direct-connections-reset.json): won at tick 275; lane1_receiver → lane1_lamp and lane2_receiver → lane2_lamp power links preserved.

Both outcome screenshots were reviewed and show lit goals with visible connecting wires. Neither run recorded browser errors. Both pass the stronger motion/transform/connection Reset audit. A modified stdin copy with a wrong restored target fails with exit 1; original evidence remains untouched.

All 89 native tests pass, including fourteen audit cases covering matching/empty/reordered graphs, changed sources/targets/types, reversed/missing/extra/duplicate links, absent snapshots, and malformed data.

Re-auditing all 54 Reset-equipped records returns **2 passed, 52 incomplete, 0 failed**. The older 52 still satisfy the previous checks but lack connection snapshots; earlier pass counts are historical and do not satisfy this stronger check. The archive now contains 108 outcomes (91 wins, 17 timeouts), including repeats. Full campaign difficulty coverage and original-physics fidelity remain incomplete.

## Level 6: partial matrix and placement interruption

The Forgiving reference, near-positive, and near-negative direct-UI attempts all win at tick 127. Their three `L06-forgiving-*-direct-reset.json` outcome records pass the connection-aware audit for four placed dominoes, including an explicitly empty connection graph. Screenshots reviewed: success freezes the last domino while still substantially upright, confirming the existing mid-animation success-freeze issue.

The next attempt, `L06-forgiving-outside-window-direct-reset`, stopped during construction with `Timed out: placed part`. It did not reach Run and has no outcome record. Do not count it as a timeout simulation or a completed matrix cell. Investigate the UI placement failure before resuming this recipe; no game variables were changed to bypass it. Balanced/Precise level-6 matrix attempts have not yet run.

Three new records bring the archive to 111 outcomes; five Reset records now pass the stronger audit, while the older 52 lack connection evidence. The full matrix remains incomplete.

A fresh-navigation retry of the failed Forgiving outside-window recipe completed construction with the same input gestures, then timed out at simulation tick 3600 as expected. [Retry evidence](playtest-results/L06-forgiving-outside-window-direct-placement-repro.json) passes the stronger audit for all four dominoes; the screenshot shows the misplaced domino above the chain and the ball on the deck. The earlier failed state had three movable dominoes and one inventory item remaining, with no browser errors. The placement interruption is therefore intermittent and remains unresolved; no game or driver source was changed to claim a fix. The retry brings the archive to 112 outcomes and six connection-aware audit passes (including this separately named retry).

## Level 6: remaining difficulty outcomes

Balanced and Precise now each have all four UI-only variants recorded with Reset. Together with the Forgiving records and separately named outside-window retry, level 6 has twelve distinct matrix outcomes. No construction interruption recurred in these eight attempts, but the earlier intermittent failure is not considered fixed.

| Difficulty | Reference | Near-positive | Near-negative | Outside window |
| --- | ---: | ---: | ---: | ---: |
| Forgiving | Won, tick 127 | Won, tick 127 | Won, tick 127 | Timeout, tick 3600 (retry) |
| Balanced | Won, tick 127 | Won, tick 127 | Won, tick 127 | Timeout, tick 3600 |
| Precise | Won, tick 127 | Won, tick 127 | Won, tick 127 | Timeout, tick 3600 |

All twelve outcomes have reviewed screenshots and no recorded browser errors. All twelve pass the current audit for four placed dominoes, sampled correction bounds/easing and actual Reset restoration, including explicitly empty connection arrays. These checks still do not establish exact target assignment or rendered fluidity between samples.

The near-positive first domino starts at [-2.3095338, 1.0008088, 0.032416027] on all difficulties. Forgiving corrects it to [-2.4, 1, 0]; Balanced ends at [-2.3848424, 1.0001355, 0.0054313447]; Precise leaves it unchanged. Outside-window Balanced/Precise attempts retain [1.6099286, 4.9958086, 0]. Identical success times across all small-error runs mean this batch alone does not demonstrate a larger successful placement region.

The screenshot review continues to show success freezing the final domino before its visible topple finishes. The approved visual style was not changed during testing; this behaviour remains an open playability issue.

Archive totals: 120 outcomes (100 wins, 20 timeouts), including repeats/historical records; 66 have Reset evidence. Auditing those 66 gives **14 passed, 52 incomplete, 0 failed** under the connection-aware checker. The incomplete records lack connection snapshots. The full 480-case campaign matrix and original-physics calibration remain unfinished.
