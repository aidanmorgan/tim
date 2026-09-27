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

## Playable family follow-up

The shared ElectricalLogicPart replaces BothGatePart (including scene references and script UID); no alias is retained. Four new scenes expose OR/XOR/NOR/NAND. The existing 47 logic cases now instantiate actual catalogue scenes, not test probes. A new open-switch feedback test verifies Start rejection without changing the construction or entering Run: 627 native tests total.

ElectricalRoute now explicitly declares Closed alongside fixed socket identities. All six contact parts were forward-updated. Before Start, validation includes their open routes so a contact cannot later close an undetected nonmonotone loop. The UI catches the specific feedback exception and remains in build mode. This conservatively rejects any potential XOR/NOR/NAND wire cycle even when a contact starts open; it does not silently invent a delay. Memory controls and supplied contacts remain distinct.

New UI-only evidence:
- electrical-xor-timed-v1: retained failure during level selection; no Run/Reset occurred.
- electrical-xor-timed-v2: both inputs on → output off; timer expires → one input on → output on.
- electrical-or-timed-v1: output remains on with two, then one powered condition.
- electrical-nor-timed-v1: timed input on → output off; both conditions off → output on.
- electrical-nand-timed-v1: both inputs on → output off; one expires → output on.
- Each accepted timed attempt has zero reported browser errors, exactly one Run/Reset, and byte-identical restored construction. Holding/outcome screenshots were inspected for the distinct output states and original operation symbols.
- electrical-feedback-rejected-v1: native/browser rejection works; screenshot exposed a clipped explanation. Retained as a visual failure and shortened the UI message.
- electrical-feedback-rejected-v2: no browser errors, no Run event, remains in build mode; inspected screenshot shows the complete one-line explanation. No Reset is expected because simulation never started.

The earlier “next required work” paragraph describes the prerequisite batch; gate scenes/icons and pre-run feedback validation are now implemented. Campaign lessons, mobile readability, large-network performance and expanded memory-boundary cases remain pending. Sampled screenshots are not continuous animation verification.
