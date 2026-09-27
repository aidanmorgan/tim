# Bellows verification

Local verification, 27 September 2026. Bellows are a new C# catalog part, scene and original icon. They convert a physical top impact into a finite downward stroke and a local +X air burst. This uses the existing ideal airflow network, not compressed-air storage, pressure/volume conservation or measured TIM physics.

## Contract

- Enum phases: Ready → Compressing → Held → Refilling → Ready.
- Only dynamic visible bodies hitting the local top face at at least 0.8 units/s start a stroke. Side, underside and gentle resting contacts do not. Same-tick impacts coalesce to the strongest before the following tick commits; they do not produce stacked pulses.
- Stroke is proportional to capped incoming normal-impact energy, up to 0.4 units. Compression speed is 0.8 units/s; a full stroke emits for approximately 0.5 seconds. The final fractional step scales force by actual travel. Default jet force is 18, reach 5 and radius 0.85.
- The top plate and collision box move together. Accordion folds scale with plate height. Contact is non-bouncy. A resting ball holds the compressed plate; no repeated pulses occur under static load.
- Clearing the plate permits silent return at 0.4 units/s. A new body in the return sweep stops refill rather than being pushed through. Refill itself emits no air.
- No activation or power sockets: the physical impact is the input. Typed airflow and downstream windmill/belt connections carry its effects; it is not a remotely triggered fan.

## Automated evidence

```sh
dotnet test CuriousContraptions.tests --no-restore --verbosity quiet
node --test tools/Playtest/direct-ui.test.cjs
dotnet publish CuriousContraptions.web -p:PlaytestDiagnostics=true --no-restore --verbosity quiet
dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet
git diff --check
```

Full native suite: **735 passed**, including **26 bellows cases**. UI-driver suite: **39 passed**. Diagnostic and production Release web publishes both succeeded (exit 0); production playtest diagnostics are disabled.

Coverage includes actual falling-ball impact → airflow → windmill → conveyor/cargo, missed impact, blocked air, disconnected belt, moving plate/visual agreement, finite stroke and downstream coast-down, held-load suppression, unload/refill/retrigger, five 3D poses, strength/mass/threshold and integrated-burst bounds, side/underside rejection, simultaneous impact ordering, Reset at intermediate times, JSON construction reload/replay, invalid parameters, chime actuation/occlusion, and a new load interrupting refill. Render calls cannot mutate simulation.

## Real-UI browser evidence

Used the user-requested Playwright MCP workflow: palette labels, placement, 3D move handles, real belt connection, Run and Reset. No state setters, imported solutions, storage edits or numeric placement controls. The adapter explicitly verifies the actual mechanical connection in read-only Run diagnostics.

- **bellows-belt-v1:** striker lands on top. At ~0.63 seconds plate is raised and cargo is still left; at ~1.11 seconds folds are compressed and cargo is moving right; at ~1.59 seconds cargo is beyond the conveyor. At ~4.38 seconds the striker still holds the plate down. Exact construction Reset; no browser errors.
- **bellows-missed-v1:** striker placed two units away in depth. Plate remains raised, windmill rests and cargo stays at the conveyor's left side at ~1.58 and ~4.36 seconds. Exact construction Reset; no browser errors.

Records: ignored local `docs/playtest-results/bellows-belt-v1.json` and `bellows-missed-v1.json`. Corresponding motion-0/1/2, holding, outcome and failure-if-any captures are in `.playwright-mcp/`. Actual positive positions include pump (-4,5.9958,0), striker (-4,8.9994,0), windmill (-1,5.8795,0), conveyor (2,3,0), cargo (1,3.5990,0); the negative changes striker depth to 2. Both records preserve actual positions, not merely the intended recipe. CCFRAME excludes dynamic ball positions; screenshots establish browser cargo motion, with native tests separately asserting displacement/events.

## Failures and limitations retained

- Initial compilation failed because the collection extension namespace for TryAdd was missing; explicitly imported System.Collections.Generic. The final build/tests are clean. An initial tool-script syntax error applied no edits.
- Anvil graph access worked, but write validation was authentication-unavailable and allowed with a warning; this is not a successful security scan.
- The stroke/jet relation is a gameplay approximation. Pressure, reservoir capacity, hoses, nozzles/valves and pneumatic pistons remain separate unfinished tasks. Multiple receivers are not a conserved energy split.
- The custom solver uses kinematic plate motion and non-bouncy contact, not a fully coupled mass/spring/air-pressure system. Accordion sides are visual; collision proxies cover plate, base and nozzle. No continuous swept-collision claim is made.
- Native tests cover refill/retrigger and blocked-air paths; those paths still need extended browser interaction beyond the positive/missed controls.
- Sampled captures show compression and transfer, not sustained frame-rate/mobile fluidity. Introductory campaign lessons, load-aware mechanics, author-specific forgiveness and comprehensive difficulty sweeps remain outstanding; campaign is still 58 drafts toward 75.
