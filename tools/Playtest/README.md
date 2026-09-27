# Direct-UI difficulty playtest

This replaces the earlier reference pass that used front-view/numeric depth controls.

The MCP input adapter is JavaScript because Playwright MCP executes JavaScript callbacks. Game code and diagnostic instrumentation are C#. The adapter has no game-state setters: it clicks actual palette/action buttons, drags visible 3D translation arrows and rotation rings, clicks wiring endpoints, and presses Run. The only workshop menu setting it changes is the difficulty slider. Puzzle selection is ordinary UI navigation.

Godot renders into a canvas, not DOM buttons. A test-only C# observer emits read-only screen coordinates for rendered controls/handles, like DOM bounding boxes, plus actual simulation samples. No game methods are invoked by the browser driver. Screenshots must be reviewed alongside logs.

Publish instrumentation with:

```sh
dotnet publish CuriousContraptions.web -p:PlaytestDiagnostics=true
```

Normal publishing omits diagnostic calls. Invoke the `directUiAttempt` function in `direct-ui.js` through the Playwright MCP callback, supplying a recipe with `level`, `precision`, `caseId`, `parts` and `connections`. Recipes describe desired pointer actions relative to a reference layout; they are not imported into the game.

Generate the full matrix with `dotnet run --project tools/Playtest`; optionally filter by level, difficulty and variant, e.g. `dotnet run --project tools/Playtest -- 1 balanced near-positive`. This C# tool produces recipes only and has no connection to the game. Each recipe records the puzzle-data SHA-256.

Each part has `slot`, `kind`, `position`, `rotation`, optional `offset` and `rotationOffset`. The adapter reads only rendered UI geometry while building. `CCRUN`, `CCFRAME` and `CCRESULT` are evidence captured after ordinary UI actions.

## Required matrix

All 40 levels × Forgiving (0), Balanced (0.45), Precise (1) × at least four attempts = **480 planned runs**:

1. Reference placement through direct handles.
2. Small positive placement/orientation error on a chosen part.
3. Small negative placement/orientation error.
4. Clearly incorrect placement outside the author's assistance window.

Repeat suspicious/non-repeatable cases. Do not assume a near miss must fail on Precise: alternate valid solutions are possible. Prove assistance by measuring actual position/quaternion changes, bounds, timing and reset behavior as well as win/loss. Record actual starting error, not just requested pointer error.

Acceptance requires all matrix cells to have artifacts, author-window/correction-cap checks, continuous bounded motion, zero assistance on Precise, and no assistance outside eligibility windows. Reference wins alone are insufficient. Instrumentation and a working runner do not mean the matrix is complete.

## Current status

All 40 levels have successful direct-UI Balanced reference runs with reviewed screenshots and no recorded browser errors in those runs. Level 3 uses the post-fix fan timing; level 36 required staging its boundary fan slightly inside and then moving it with the real arrow. There are 54 outcome records (52 wins, two timeouts), including historical/regression records across two campaign hashes. Do not equate artifact count with current-build matrix completion. See [the report](../../docs/browser-playtest.md) for per-level evidence and playability findings.

The 480-case difficulty matrix and independent bounds/easing/browser-Reset audit remain incomplete. Earlier reference runs that used numeric layers do not satisfy this matrix.

## Sampled-motion and Reset audit

The driver now captures `<caseId>-outcome.png`, clicks the real Build again/Back to building button, and records read-only `CCRESET` transforms plus the returned build UI. Old records without Reset evidence are incomplete for this audit.

Run the independent C# checker with:

```sh
dotnet run --project tools/Playtest -- --audit docs/playtest-results/*-reset.json
```

Exit codes: 0 = implemented checks passed; 1 = violation; 2 = incomplete evidence. A filename of `-` reads JSON from stdin, useful for negative tests without altering saved evidence.

Checks cover campaign hash, level/difficulty, browser errors, complete four-tick sample cadence through the first second (or early outcome), position caps, a conservative quaternion-angle bound, zero correction on Precise or outside every same-kind position window, sampled quintic/Slerp motion, and all parts' original transforms after actual browser Reset.

Scope limits: caps use the maximum for the same kind, and the rotation bound is sqrt(3) times the Euler-vector cap. This does not yet prove exact per-slot assignment, rotation-only eligibility, that every eligible part receives its expected correction, or rendered continuity between samples. Directed, typed wiring restoration is now checked when both Run and Reset connection snapshots exist; older records without them are incomplete. This compares restored wiring with the actual starting graph, not whether that graph matches the requested recipe. A passing result is not full nudge-system acceptance.

Three fresh level-1 near-positive records ending `-reset.json` pass at all three difficulties. Negative checks using stdin reject a ten-unit trajectory corruption (exit 1) and a one-unit restored-ball error (exit 1); removing Reset evidence yields incomplete (exit 2). Original evidence was not modified. There are now 57 outcome records, including these repeated audit runs.

## Expanded difficulty evidence

Levels 2 and 3 each now have all 12 planned variants recorded with real UI Reset: reference, positive error, negative error and outside-window at each difficulty. All 24 outcomes have reviewed screenshots and pass the implemented audit. Level 2 has nine wins and three outside-window timeouts; level 3 has six assisted wins, two assisted outside-window timeouts, and four Precise timeouts. Thus a passing motion audit must not be read as a successful/playable puzzle run.

The Precise fan reference failure is a usability finding: identical UI actions start the fan at Y=3.7841127 for all three difficulties. Forgiving/Balanced correct to 3.8 and win; Precise remains unchanged and fails. The practical Precise placement path remains unresolved.

There are now 81 outcome records including repeats/historical runs, with 27 Reset-equipped records passing the current audit. The full matrix has 480 distinct planned cells; artifact counts do not measure its completion. [Audit output](../../docs/difficulty-audit-2026-09-27.json) preserves the current 27-record check.

## Optional UI snapping

A recipe may set `snapMovement: true` to hold Shift during movement-arrow drags. This is an actual keyboard modifier, not a transform setter. It aligns the chosen world coordinate to 0.1 units. Default matrix recipes leave it false so deliberate placement errors remain measurable; snapped reruns are separately named and do not replace free-drag records.

The level-3 Precise reference rerun `L03-precise-reference-direct-snap-reset` wins at tick 265 and passes the sampled-motion/Reset audit with zero fan correction. This verifies the keyboard-assisted placement path; earlier free-drag failures remain recorded. Current artifact total: 82.

## Level-4 matrix update

Level 4 now has twelve direct-UI/Reset records: nine wins (reference and both small errors at all difficulties) and three outside-window timeouts. All outcome screenshots were reviewed. The spring receives bounded correction on assisted settings, no correction on Precise, and none outside the position window. Small-error Precise wins mean these particular win/loss results do not prove tolerance expansion.

All 40 Reset-equipped records pass the implemented audit. Current outcome artifact total: 94, including historical/repeated runs, not 94 distinct completed current-build matrix cells. Full results and actual spring transforms are in [the browser report](../../docs/browser-playtest.md#level-4-spring-difficulty-matrix). The 480-cell matrix remains incomplete.

## Level-5 matrix update

The depth-axis fan puzzle has twelve new UI-only/Reset records: nine wins and three outside-window timeouts, with all screenshots reviewed. Assisted fans correct to the authored position; Precise and distant placements remain unchanged. All small-error Precise runs also win, so this batch does not establish a strict expansion of successful placements.

All 52 Reset-equipped records pass the current audit. The outcome archive contains 106 records including repeats/historical runs. See [level-5 results](../../docs/browser-playtest.md#level-5-depth-axis-fan-matrix). The full 480-case matrix remains incomplete.

## Connection-aware Reset audit (supersedes earlier audit counts)

Run and Reset diagnostics now include `connections` with `from`, `to`, and `type`. The checker rejects lost/extra/reversed/mistyped edges, duplicate edges, and malformed connection data. Array order is irrelevant; an explicitly recorded empty graph is valid. Missing snapshots are incomplete evidence, not an empty graph.

Fourteen native audit cases exercise this contract; all 89 native tests pass. New Balanced UI-only reference runs for levels 2 and 18 both win, preserve their one and two power links respectively, and pass the stronger audit. Screenshots reviewed. Changing one restored target in a copy piped through stdin produces exit 1 without modifying original evidence.

Current archive: 108 outcomes, 54 Reset records. Of those Reset records, **2 pass the new connection-aware audit and 52 are incomplete** solely because they predate connection instrumentation. Previous pass counts above describe the earlier transform-only checks. Retain historical evidence and make fresh captures where stronger acceptance requires it; do not invent empty connection snapshots for old records.

## Level-6 matrix update

All twelve level-6 variants now have reviewed outcomes: nine wins at tick 127 and three outside-window timeouts at tick 3600. The Forgiving outside-window outcome uses the separately named `L06-forgiving-outside-window-direct-placement-repro.json`; include it explicitly when auditing a `*-reset.json` glob. The initial construction interruption remains an unresolved intermittent finding.

Current archive: 120 outcomes, 66 Reset records. The stronger audit reports 14 passed and 52 incomplete (missing historical connection snapshots), with no violations. All twelve level-6 records pass; all small-error Precise attempts also win, so win/loss alone does not establish tolerance expansion. See [level-6 results](../../docs/browser-playtest.md#level-6-remaining-difficulty-outcomes). The full 480-case matrix remains incomplete.
