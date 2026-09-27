# Electrically controlled clutch verification

Update, 28 September 2026: the speed-only network described in this historical verification has been forward-refactored to shared torque/work allowances. See [current mechanical verification](mechanical-work-verification.md) for native and real-UI regressions; dynamic inertia/slip and broader aerodynamic modelling remain unfinished.

Local verification, 27 September 2026, based on revision 8f136a4 plus the clutch implementation committed with this document. Core component behaviour is verified below; this does not complete campaign integration or the broader component goal.

## Contract

The C# clutch has enum phases Open, Closing, Engaged and Opening. A separately supplied electrical coil closes two plates over an authored 0.05–2 seconds (default 0.2). Only full closure enables its internal mechanical route. An upstream shaft is still required: electricity alone cannot create rotation. Signed input speed passes unchanged to the output when engaged.

Power loss disconnects the speed route immediately while the visible plates separate over the same duration. Input and output pulley/plate angles independently follow simulated shaft speed. This is the existing ideal shaft-speed model, not a torque, load, slip, freewheel or energy-conserving model.

Disabled routes remain in structural validation: an open clutch cannot hide cycles or competing upstream drives. The mandatory Enabled field forward-refactors existing conveyor/reverser routes; no old constructor or compatibility path remains.

## Automated evidence

```sh
dotnet test CuriousContraptions.tests --no-restore --verbosity quiet
node --test tools/Playtest/direct-ui.test.cjs
dotnet publish CuriousContraptions.web -p:PlaytestDiagnostics=true --no-restore --verbosity quiet
dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet
git diff --check
```

- Full native suite: **756 passed**, including **18 clutch cases**.
- Playwright driver harness: **48 passed**. These are adapter tests, not browser behavioural proof.
- Diagnostic web publish: succeeded (exit 0).
- Production Release publish: succeeded (exit 0), with playtest diagnostics disabled.

Native cases cover both supplies independently, positive and negative shaft direction, reversed identifier/connection order, downstream ball transport, power loss and re-engagement, bounded close duration, four orientations, invalid parameters, structural loops/competing drives while open, hold-timer release, visual/simulation agreement and exact construction Reset/JSON reload/replay.

Initial power-cycle test failed because it attempted to reconnect a wire while Running. World.Connect correctly rejected the operation. The corrected test pre-wires an electrical switch and changes its contact state, retaining the build/run restriction. Both the original failure and corrected full-suite result are retained in the session.

## Real-UI evidence

Used Playwright MCP with actual palette selection, 3D move handles, connection controls, Run and Reset. No game-state setters, imported construction, numeric placement or storage edits. Read-only run diagnostics verify every intended typed link and the actual construction.

- **clutch-powered-v1:** motor (-4,4.9856563,0), clutch (-1,4.996786,0), conveyor (2,3,0), battery (-4,1.9870987,2), cargo (1,4.4989867,0). Battery supplies motor and clutch independently; motor → clutch → conveyor uses mechanical drive ports. At 0.62 seconds cargo is on the moving belt; at 1.58 seconds it has left the right end. Plates are closed. Exact Reset; no browser errors.
- **clutch-unpowered-v1:** same actual construction, omitting only battery → clutch. At 1.62 and 4.41 seconds the input changes angle, plates remain separated, output/belt spokes remain stationary and cargo stays at the left. Exact Reset; no browser errors.
- **clutch-no-shaft-power-v1:** same five-part arrangement, omitting only battery → motor. At 1.58 and 4.37 seconds the coil has closed the plates but both shafts, conveyor and cargo remain stationary. This verifies the clutch is not a mechanical power source. All three intended links are present; exact Reset and no browser errors.
- **clutch-reversed-v1:** insert reverse transmission (-4,3,-2) between motor and clutch; place cargo at (2,4.498902,0). All five typed links verified. At 0.64 seconds cargo is on the belt, which displays its leftward arrow; at 1.62 seconds cargo has left the left end. Exact Reset; no browser errors.
- **clutch-timed-v1:** raise motor/clutch to approximately Y=6, add hold timer (2,5.995034,0), switch (4,3,-2) and striker (4,5.9937534,-2). Battery independently supplies motor and timer; switch activation triggers timer; timer supplies clutch. All six typed links verified. At 1.59 seconds the timer is holding, plates are closed and cargo has moved right. At 4.38 and 4.88 seconds the timer is idle, plates are open and output/belt spokes remain at the same angle while the input changes angle. Exact Reset; no browser errors.

Local records: ignored `docs/playtest-results/` JSON files named for all five cases above, including recipes, actions, construction, connections and Reset. All intended part kinds and exact connection counts were checked; maximum component-wise placement error against each UI recipe was 0.014344 units (below the 0.025-unit verification bound). All five complete Run constructions exactly match their Reset constructions. Matching motion-0/1/2, holding and outcome captures are in `.playwright-mcp/`. CCFRAME excludes dynamic ball positions: cargo movement above is a visual observation, supplemented by separate native displacement/event assertions.

## Outstanding verification and scope

Sampled images do not establish sustained animation fluidity or mobile readability. Introductory campaign levels, full difficulty sweeps and broader component coverage remain open; the campaign still has 58 draft levels toward 75.

Anvil graph access worked but provided no C# test mapping; native tests were inspected and run directly. Its pre-write gate was authentication-unavailable and permitted edits with a warning, not a passed validation.

## Deployment follow-up

The initial GitHub Pages run for 0af472a (36324963657) passed its build/test job but failed deployment while requesting GitHub's OIDC identity token (request timeout). The workflow already grants id-token write permission. The failed deployment job was retried without changing permissions or implementation. The rerun completed successfully, confirming deployment of 0af472a.
