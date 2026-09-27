# Socket enum refactor verification

27 September 2026. Tested base: 0af472a plus the refactor committed with this document.

## Contract and scope

Socket identity is now the closed C# enum `SocketId`, not a bag of string constants. Connection ports/specifications, activation wiring, electrical gate/route equations, mechanical sources/routes, power/speed lookup, world connection APIs, UI choices and campaign tooling all use the enum. The old `SocketIds` class and string socket overloads are removed; there is no compatibility path.

The JSON boundary writes exact current snake-case names derived from enum members. It accepts only those names with ordinal matching. Unknown names, case variants, whitespace variants, numeric tokens/strings and undefined enum values are rejected. Missing nullable connection endpoints remain invalid topology; they are not inferred. Current authored campaign names already satisfy this contract, so no content migration is needed or performed.

The independent Playwright evidence auditor now parses socket identities into the same enum before comparing Run/Reset. Identically malformed socket names in both captures cannot pass. Historical captures remain unchanged, not inputs to a compatibility reader.

## Native and tooling evidence

- **789 native tests passed**, including **33 new cases**: all eleven socket names round-trip exactly; eighteen malformed input tokens fail conversion and audit; three undefined runtime enum values fail serialization/resolution; every discovered catalogue part exposes defined, unique socket identities and valid structural electrical/mechanical routes.
- Existing native tests exercise campaign loading/solutions, connection direction/domain/identity, explicit socket selection, missing sockets, duplicate wiring, activation commands, mechanical transfer, electrical truth tables/feedback and ropes/Reset.
- **48 Playwright-adapter tests passed** (not a substitute for browser proof).
- Campaign and Playtest tool projects build with zero warnings/errors.
- Diagnostic and production Release web publishes succeed (exit 0); production diagnostics are disabled.
- Repository C# search finds no old `SocketIds` references or string socket API signatures.

Commands:

```sh
dotnet test CuriousContraptions.tests --no-restore --verbosity quiet
dotnet build tools/Playtest --no-restore --verbosity quiet
dotnet build tools/Campaign --no-restore --verbosity quiet
node --test tools/Playtest/direct-ui.test.cjs
dotnet publish CuriousContraptions.web -p:PlaytestDiagnostics=true --no-restore --verbosity quiet
dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet
git diff --check
```

## Browser regression evidence

Playwright MCP uses real palette selection, placement/move handles, connection controls, Run and Reset. Read-only diagnostics verify construction and typed links; no state setters, imported saves/solutions, numeric placement or storage edits.

- **socket-enum-timed-v1:** repeat the eight-part timed-clutch circuit from [clutch verification](clutch-verification.md). Six exact links cover activation, electrical and mechanical sockets. At 1.60 seconds the hold timer supplies the engaged clutch and cargo moves right. At 4.39/4.88 seconds the timer is idle, clutch open and output stationary while the input still turns. Exact construction Reset; no browser errors.
- **socket-enum-rope-v1:** anchor (0,3,0), weight (1.5,3,0), one bidirectional tie-to-tie rope. Actual length 1.6211436 matches the intended socket-to-socket distance within 0.0001. Captures at 0.62/1.58/4.36 seconds show the attached weight swinging around the fixed anchor. Exact construction/length Reset; no browser errors.
- **socket-enum-no-rope-v1:** same anchor and weight placements, with no connection. At 1.58 and 4.37 seconds the weight rests on the floor below its starting horizontal position rather than swinging around the anchor. No hidden connection is present. Exact Reset; no browser errors.

Local JSON records are under ignored `docs/playtest-results/`, with case-matched screenshots in `.playwright-mcp/`. The full recipes/actions, actual parts, typed links and Reset are retained.

## Failures and limits

The first compile found an electrical-test helper still accepting a string input; it was forward-refactored to `SocketId`. An existing audit fixture used the invalid name `power` as supposedly valid input; it now uses the current name `power_in`, and explicit rejection tests cover the invalid name. No aliases were added.

This is a connection-contract refactor, not a claim that every catalogue part now has complete individual browser evidence. The per-part audit and its missing proofs remain open. Catalogue IDs, extensible parameter keys, other protocol/UI selectors and remaining enum audits are separate work; the whole repository is not declared magic-string-free.

Anvil supplied symbols but no usable C# dependency/test mapping. Full native tests and direct caller inspection supplemented it. Its write gate was authentication-unavailable and allowed edits with a warning, not a successful validation.
