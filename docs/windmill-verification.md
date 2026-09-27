# Windmill verification

Verified locally on 27 September 2026. This delivers a catalogued C# windmill within the existing **ideal signed-speed** mechanical model; it does not implement torque, aerodynamic efficiency, load sharing or conserved shaft work.

## Behaviour and architecture

- Fan airflow is sampled at four equally weighted points across the rotor. Each parallel streamline is checked against scene geometry; one blocked sample reduces the effective drive by a quarter. This is a bounded area approximation, not continuous blade aerodynamics.
- Local rotor-axis force sets signed target speed, with a small cut-in threshold, bounded maximum and continuous acceleration/deceleration. Rear airflow reverses rotation; edge-on or opposing airflow does not produce sustained drive.
- The typed mechanical output can drive conveyors and reverse transmissions in the same simulation substep. Existing connection rules reject competing sources.
- Rotor, output pulley and mechanical output share simulation angle/speed. Rendering does not advance physics. Fixed guard, hub and stand proxies provide collision; individual blades are visual and do not strike passing objects.
- Shared airflow receivers now expose weighted samples. Wind chimes use one sail sample and dynamic bodies use their centre. The old single-target API was removed, not retained as an alias.
- Catalog, current-schema construction save/load, Reset, toolbox icon and conveyor help are integrated. Default output at force 9 is 6 rad/s, giving the default conveyor 4 units/s; the rotor acceleration is 18 rad/s² and maximum 12 rad/s.

## Automated checks

Commands:

```sh
dotnet test CuriousContraptions.tests --no-restore --verbosity quiet
node --test tools/Playtest/direct-ui.test.cjs
dotnet publish CuriousContraptions.web -p:PlaytestDiagnostics=true --no-restore --verbosity quiet
dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet
git diff --check
```

The full native suite passes **709 tests**, including **22 new windmill cases**. The existing UI driver suite passes **39 tests**. Diagnostic and production Release publishes both succeeded (exit 0); the production build has playtest diagnostics disabled.

Windmill cases cover front/rear drive, part/connection ordering, zero/one/two reversing stages, fan-out through conveyors, same-substep propagation, accumulated turn events, visual angle agreement, coast-down, exact construction restoration and JSON reload/replay. Five assembly poses cover all rotation axes and a combined rotation. None/partial/full occlusion produces 6/4.5/0 rad/s. Ball transport requires both wind and a real belt; fan-off, wall-blocked and disconnected controls do not move the ball sideways. The ball begins below the entire jet and falls farther away.

Additional tests cover edge-on wind, equal opposing sources, cut-in threshold, smooth signed reversal, speed cap, competing/reversed connection rejection, invalid response parameters, and invalid sample weights rejected before any receiver changes.

## Real-UI browser evidence

Playwright MCP used the actual palette labels, placement clicks, move/rotate gizmos, Connect action and Run/Reset. No game-state setters, imported solutions, storage changes or numeric placement menus. Read-only console diagnostics confirm construction, explicit connection ports and exact Reset. These are free-workshop controls, not campaign wins or the postponed difficulty matrix.

Local evidence in ignored `docs/playtest-results/`; screenshots in `.playwright-mcp/`:

| Attempt | Observed result |
| --- | --- |
| `windmill-belt-v1` | Fan → windmill → physical belt → conveyor transports the ball. Exact construction Reset. Initial runner result was not persisted; its diagnostic subset was recovered from console log lines 39809–39960. Retained rather than presenting it as a full runner record. |
| `windmill-belt-v2` | Repeated construction with full runner record. Explicit mechanical drive-to-drive_in connection checked before acceptance. Motion screenshot shows ball on belt at ~0.61 s, then beyond its right end at ~1.57 s. No browser errors; exact Reset. |
| `windmill-no-belt-v1` | Same intended geometry without connection. Rotor changes angle, conveyor stays idle, ball remains on its left side at ~1.57 and ~4.36 s. No browser errors; exact Reset. |
| `windmill-blocked-v1` | Wall turned 90° between fan and rotor, belt present. Rotor/pulley retain their rest pose and ball remains on belt at ~1.61 and ~4.40 s. No browser errors; exact Reset. |

Actual connected-run coordinates differ slightly from requested placement: conveyor (1.9,3,-0.1), ball (0.9,~4.499,-0.1), fan (-4,~5.984,0), windmill (-1,~5.999,0). The disconnected attempt uses conveyor (2,3,0), ball (1,~4.499,0). Their relative ball-to-conveyor geometry matches; neither ball intersects the fan jet. The wall lands near (-2.4,~5.993,0). Selection/placement shifts are preserved as a UI issue, not silently declared exact or fixed.

The attempt adapter uses deliberately slower click timing, fan drag screenshots and an explicit post-Run connection assertion. This does not prove the previously observed missed-drag/wire cause fixed. CCFRAME omits dynamic ball positions, so screenshots—not those snapshots—establish browser ball movement; native tests separately assert displacement and transported events.

## Failures and remaining work

- Initial compilation exposed a missing required TubeProxy opacity argument; fixed explicitly. The collection-count test warning was corrected to Assert.Single. No failing native tests remain in the final run.
- Windmill intro/combined campaign lessons are still needed; campaign remains 58 drafts toward 75.
- Torque/load-aware transmission, fan external power input and energy budgeting remain outstanding. Do not infer realistic lifting capacity or perpetual-loop prevention from this ideal speed component.
- Four sample points can change response in steps as an obstruction moves. Spin acceleration limits velocity changes but is not a continuous area solver.
- Reviewed screenshots establish visual states and motion progression, not continuous fluidity, mobile performance or all-angle icon readability. Sustained rendered motion/performance review remains tracked.
- Investigate UI selection/placement drift and strengthen permanent driver construction/connection assertions. Preserve current and earlier failed attempts.
- Anvil graph context was available; its write gate returned authentication-unavailable with allow-with-warning. This is not a successful gate scan.
