# Selection and connection verification

27 September 2026.

## Reproduced defect and fix

Selecting a movable part immediately enabled planar dragging and pushed Undo. A subsequent mouse-motion event—even at the same location—ran grid snapping on an off-grid position. Selection release could also run tube snapping without an intentional move.

Three new native regression cases (zero and ±1 pixel jitter) **all failed before the fix**. They now prove unchanged part position and that one Undo removes the prior placement rather than undoing an empty selection.

Planar selection now records the press location. Movement and its single Undo snapshot begin only after a six-pixel threshold. Selection-only release does not invoke tube snapping. The existing move/rotate/resize gizmos are unchanged; actual planar drags preserve grab offset and release-over-UI behaviour.

## Verification

- Full native suite: **738 passed**; focused workshop interaction suite: **47 passed**.
- Permanent Playwright adapter suite: **48 passed**, including nine new typed-link checks. Required links must appear in actual Run diagnostics with matching endpoints, domain, sockets and authored rope length. Missing or incorrect links fail and retain actual construction evidence rather than count as lifecycle success. No automatic retry or connection substitution.
- Diagnostic and production Release publishes passed (exit 0); production diagnostics are disabled.
- Real-UI MCP attempt `selection-jitter-belt-v1`: palette-built windmill/fan/conveyor/ball, one-pixel held-pointer jitter on each part, then real mechanical connection, Run and Reset. All four selections retained exactly equal before/after projected positions and correct selected identity. Conveyor stayed at (2,3,0), ball at (1,~4.499,0), unlike the earlier connected attempts' 0.1 X/Z shifts. The ball travelled off the conveyor; Reset construction matched exactly; no browser errors.
- Local full record: ignored `docs/playtest-results/selection-jitter-belt-v1.json`; reviewed holding screenshot and motion captures in `.playwright-mcp/`. Read-only console diagnostics only; no browser state setters or imported constructions.

Commands: `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`; `node --test tools/Playtest/direct-ui.test.cjs`; diagnostic and Release `dotnet publish CuriousContraptions.web` with PlaytestDiagnostics true/false respectively; `git diff --check`.

## Limits

This proves the reproduced selection/grid-snap defect fixed. It does not prove every previously missed fan drag, missing wire, selector timing issue, lost pointer event or mobile gesture fixed. The general driver's required-link assertion is covered by fake-page unit tests; the browser variant uses the same mechanical endpoint check and extra jitter probes. Fake pages are not browser gameplay evidence. Comprehensive placement verification must still distinguish intentional tube snapping from unintended drift.

Anvil graph context was available; write validation was authentication-unavailable with allow-with-warning, not a passed scan.
