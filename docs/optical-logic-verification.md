# Optical logic verification

27 September 2026. Component-first batch, not completed campaign/difficulty testing.

Five optical gates implement AND/OR/XOR/NOR/NAND. Numbered controls absorb light; the separate carrier retains 90% power and its existing range/interaction budget. Broadband control thresholds are 0.25 on, 0.225 off. Reception samples controls; BeforeNetworks advances truth on the next fixed tick. Output lamps reflect actual outgoing light, not Boolean truth alone.

## Evidence

579 native tests pass, including 40 actual-beam cases (four truth rows × five operations × carrier present/absent). These assert independent controls, snapshot delay, output power, carrier removal and control reconfiguration. Existing pure-model tests cover hysteresis, invalid inputs, Reset and retraction.

UI-only Playwright artifacts retained locally:
- docs/playtest-results/optical-nor-carrier-v1.json: toolbar/gizmo/socket construction; transmitted carrier and downstream powered gate open.
- docs/playtest-results/optical-nor-no-carrier-v1.json: battery-to-laser connection omitted; no transmitted light, downstream gate closed.

Both have no reported browser errors, one Run/Reset, and byte-identical construction snapshots. Inspected outcome screenshots are .playwright-mcp/<case-id>-outcome.png. Hidden control/receiver faces are not visually verified.

Anvil graph queries returned ready with no inferred affected tests; explicit native tests supply coverage. Its write gate was authentication-required, allow-with-warning, not passed validation.

## Remaining

Actual-world multi-gate scheduling/feedback, rotated/occluded controls, gate-specific range exhaustion, all-operation UI transitions, aim previews, mobile/tiny relief readability, continuous animation review and campaign lessons. General optical engine routing tests do not prove every new combination. Electrical OR/XOR/NOR/NAND still need a nonmonotone solver. Exhaustive difficulty testing remains deferred.
