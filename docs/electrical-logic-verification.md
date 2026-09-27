# Electrical logic solver and supplied Both gate

27 September 2026. Prerequisite batch, not delivery of all electrical gate parts.

The conjunction-only contract has been replaced by typed ElectricalGate rules with operation, two conditions, independent supply and output. Both remains the sole playable electrical logic gate for now; it now needs its lower-front supply socket. No compatibility alias or automatic rewiring exists. Historical browser artifacts retain their original contract as evidence, not supported recipes.

The solver snapshots sources, contact routes and gate rules. Socket dependency SCCs are evaluated in dependency order; monotone components settle from false to their least fixed point. XOR/NOR/NAND cycles throw before any power commit. Per-solve inputs are cleared, so stale previous power cannot seed a cycle. Missing/invalid gate sockets, operations, routes or wires are rejected. This is binary availability, not voltage/current or charge simulation.

## Verification

- 626 native tests pass. The 47 new cases cover four truth rows × five operations × supply present/absent; reversed part/wire order; XOR/NAND reconvergent retraction; three rejected nonmonotone feedback types; AND/OR cycle source removal.
- Existing Both tests updated for independent supply preserve chained same-step propagation and Reset.
- 39 browser-driver tests pass.
- Real UI artifacts retained locally: both-separate-supply-v1.json and both-missing-supply-v1.json under docs/playtest-results. Both use a falling ball/detector/hold timer to produce the second condition. The first condition is continuously powered.
- Inspected holding screenshots show both input lamps lit in each case. With supply, output lights and downstream gate opens; without supply, output stays slate and downstream gate remains closed. Outcome screenshots and sampled frames are retained, not proof of continuous fluidity.
- Both attempts have no reported browser errors, one Run/Reset, and byte-identical construction restoration.
- Anvil graph queries returned ready but no inferred affected tests; the explicit suite supplies evidence. Its write gate was unavailable (authentication), allow-with-warning.

## Next required work

Create electrical OR/XOR/NOR/NAND parts with distinct icons/visual explanations, forward-refactor the shared part class, and add user-facing circuit validation before exposing unsupported feedback to players. Validate potential switched-contact cycles, memory boundaries, invalid authored definitions and more complex reconvergence. The test-only gate probes prove solver behaviour, not playable scene integration. Teach the resulting family in the 75-level campaign. Full repeated difficulty testing remains deferred.
