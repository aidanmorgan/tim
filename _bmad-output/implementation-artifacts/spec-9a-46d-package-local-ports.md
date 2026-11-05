---
title: 'Story 9A.46d: Add repeated typed port roles through declarations'
type: 'feature'
created: '2026-11-05'
baseline_revision: abc818074e38e3801e96d1246e83923721e18351
status: 'done'
review_loop_iteration: 1
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/element-free-engine-review.md'
  - '{project-root}/_bmad-output/implementation-artifacts/review-engine-abstraction-2026-11-04.md'
warnings: ['oversized']
deferred: []
---

<intent-contract>

## Intent

**Problem:** Ports and sockets currently rely on a global closed `PortSocket` / `WorkshopSocket` enum that conflates port identity with role, direction, and domain. Because `ElementPortCatalogue.Own` maps every socket enum value to a hardcoded domain/direction in a static table and rejects duplicate socket enums on the same element, an element cannot declare multiple ports with the same role (e.g. multiple `ActivationOut` outputs or multiple inputs) or unfamiliar port names without modifying engine socket enums and core source files (Finding V4).

**Approach:**
1. Separate package-local port identity from closed domain, direction, and role enums:
   - Introduce strongly typed `PortId` struct wrapping `string Value` (e.g. `public readonly record struct PortId(string Value)`).
   - Introduce `PortRole` enum capturing the semantic role of the port (`ActivationIn`, `ActivationOut`, `SetIn`, `ResetIn`, `Supply`, `PowerIn`, `FirstIn`, `SecondIn`, `Tie`, `TieA`, `TieB`, `Drive`, `DriveIn`).
   - Update `PortDescriptor` to declare `PortId Id`, `SimulationDomain Domain`, `PortDirection Direction`, and `PortRole Role`.
2. Delete the global socket identity/domain table (`Own(WorkshopSocket)`) and identity-specific engine switches:
   - Domain and direction are read directly from declared port descriptors rather than derived from a global table.
   - Connections (`WorkshopConnection`) reference endpoints by `PortId` on the source and target instances.
   - Connection validation checks endpoint existence, domain match, and direction compatibility using the declared ports of each instance.
3. Wire and serialization:
   - Compile exact endpoints through connection editing, save/load, and wire.
   - In FlatBuffers authoring `ConnectionRecord`, endpoints are mapped to declaration port indices on the respective instances and verified during prevalidation.
   - Authored puzzle definitions and saves resolve ports by string `PortId`.
   - Existing element package port names remain their declared external names ("ActivationOut", "Supply", etc.); no aliases or migration paths.

## Boundaries & Constraints

**Always:**
- Strongly typed package-local port identities using `PortId`.
- Duplicate local port identities on the same element must refuse atomically.
- Distinct elements may freely reuse local port names (e.g., both Battery and Gate can have port "Supply").
- Connections between non-existent ports, wrong domain, or incompatible directions must refuse atomically.
- An element may declare multiple ports sharing the same role (e.g. two `ActivationOut` outputs named "out_a" and "out_b").
- Delete hardcoded `Own(socket)` table and identity-specific switches in `ElementPortCatalogue` and `WorkshopConnections`.
- Zero skipped tests (`Skipped: 0`), no hash/byte assertions, and no mutation testing before the post-catalogue E2E phase.

**Never:**
- No global enum coupling port identity to a single hardcoded domain/direction.
- No element-specific socket checks or hardcoded port count limits in engine or solvers.
- No backwards-compatibility shims, legacy socket aliases, or automatic migration.
- No browser E2E execution during this slice (remains deferred per `BrowserDeferral.cs`).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Existing port names (regression) | Existing elements (Battery, Lamp, Gate, Latch, etc.) with declared ports | Puzzles, connections, and tests function identically with zero behavioral drift | No error expected |
| Unfamiliar port names | Element declaring ports with custom names "custom_in", "custom_out" | Compiles, connects, and evaluates correctly via standard domain laws | No error expected |
| Two outputs sharing one role | Element declaring two `ActivationOut` ports ("out_a", "out_b") | Both endpoints remain distinct; each can be connected independently | No error expected |
| Duplicate port ID on same element | Element declaring two ports with identical `PortId` "port1" | Rejected atomically during catalogue/bundle admission | `DuplicateSocket` / `DuplicatePort` rejection |
| Missing port endpoint | Connection specifying non-existent port on instance | Connection validation rejects atomically | `InvalidConnection` / `UndeclaredSocket` |
| Domain mismatch | Connection linking Signal port to Electrical port | Connection validation rejects atomically | `InvalidConnection` |
| Direction mismatch | Connection linking Input to Input, or Output to Output | Connection validation rejects atomically | `InvalidConnection` |
| Save and load roundtrip | Construction with multi-output element saved to FlatBuffers and loaded back | Endpoints deserialize to exact declared ports without corruption | Identical construction roundtrip |

</intent-contract>

## Design Record (Element-Free Engine Gate)

1. **What behavior is delivered?**
   Element authors can declare multiple ports with the same role and arbitrary package-local names on an element. The engine resolves exact connection endpoints through declared port descriptors without hardcoded socket enum assumptions.
2. **What is reusable?**
   The entire connection validation, catalogue lookup, and wire compilation mechanisms are completely generic and element-agnostic.
3. **What is declared?**
   `PortDescriptor` declares `PortId Id`, `SimulationDomain Domain`, `PortDirection Direction`, and `PortRole Role`.
4. **How does it execute?**
   `ElementPortCatalogue` stores and indexes ports per element by `PortId`. `WorkshopConnections` links instances by `(GpuBodyId, PortId)` and validates domains and directions from declared port metadata.
5. **Who owns changes?**
   `CuriousContraptions.Abstractions` owns port contracts (`PortId`, `PortRole`, `PortDescriptor`). `ElementPortCatalogue` and `WorkshopConnections` own generic port compilation and connection validation. Element packages own their port declarations.
6. **What proves generality?**
   An unfamiliar fixture element with two outputs sharing the `ActivationOut` role and custom port names connects, compiles, simulates, saves, and loads without engine modifications.
7. **What is deleted?**
   The global socket identity/domain mapping table `Own(WorkshopSocket socket)` and identity-specific engine switches in `ElementPortCatalogue.cs` and `WorkshopConnections.cs`.
8. **What remains uncertain?**
   None.

## Code Map

- `CuriousContraptions.Abstractions/PortId.cs`: New strongly typed record struct wrapping `string Value`.
- `CuriousContraptions.Abstractions/PortDescriptor.cs`: Add `PortRole` enum; update `PortDescriptor` to take `PortId` and `PortRole`.
- `CuriousContraptions.Abstractions/ElementPresentation.cs`: Update `Socket` reference to `PortId?`.
- `CuriousContraptions.Abstractions.Signal/SignalContracts.cs`: Update `SignalIndicatorDescriptor` and `SignalMemoryDescriptor` to reference `PortId`.
- `CuriousContraptions.Abstractions.Electrical/ElectricalContracts.cs`: Update `SupplyCapabilityDescriptor` to reference `PortId`.
- `engine/gpu/ElementPortCatalogue.cs`: Key ports by `PortId`, delete `Own` static table, update lookups.
- `engine/gpu/WorkshopConnections.cs`: Update `WorkshopPort` and `WorkshopConnection` to use `PortId`.
- `engine/gpu/WorkshopAuthoringFlatBuffers.cs` & `WorkshopSavePrevalidation.cs`: Map connection endpoints by port index/id.
- `CuriousContraptions.Bundle/BundleValidator.cs`: Update validation to use `PortId` and `PortRole`.
- `elements/*/*ElementDeclaration.cs`: Update existing element declarations to supply `PortId` and `PortRole`.
- `CuriousContraptions.tests/`: Add tests in `CuriousContraptions.tests/PackageLocalPortsTests.cs` covering unfamiliar port names, repeated roles, duplicate refusals, and save/load roundtrips.

## Tasks & Acceptance

**Execution:**
- Create `CuriousContraptions.Abstractions/PortId.cs` and `PortRole` enum.
- Update `PortDescriptor` and capability descriptors in `CuriousContraptions.Abstractions` to use `PortId` and `PortRole`.
- Update `ElementPortCatalogue.cs` and `WorkshopConnections.cs` to delete `Own(WorkshopSocket)` and validate via declared ports.
- Update `WorkshopAuthoringFlatBuffers.cs`, `WorkshopSavePrevalidation.cs`, and `PuzzleDefinition.cs` to serialize and deserialize endpoints.
- Update `BundleValidator.cs` and all element package declarations in `elements/`.
- Add unit and integration tests in `CuriousContraptions.tests/PackageLocalPortsTests.cs` and verify full suite passes.

**Acceptance Criteria:**
- Given unfamiliar port names and two outputs sharing one role on one element, when connect, compile, save and load the construction, then each endpoint remains distinct and works using the existing domain law without core source edits.
- And prove duplicate local identities, missing endpoints, wrong domain/direction and unresolved references refuse atomically; distinct elements may reuse local names.
- And global socket identity/domain table and identity-specific engine switches, replacing current callers in the same slice are deleted, with current callers, packages and serialized contracts updated together. Preserve existing intended gameplay and authored Reset state; do not close the story on renaming alone.

## Spec Change Log

## Review Triage Log

- 2026-11-05 (Iteration 1 Review):
  - Element-Free Engine Auditor: PASS (Scoped pass, generic contracts satisfied).
  - Forward-Only Auditor: PASS (Complete deletion of WorkshopSocket.cs and Own(socket) table; no fallbacks or shims).
  - Verification Gap Reviewer: FAIL.
    - False positive test: PackageLocalPortsTests.cs:106–154 declared dual_trigger with out_alpha and out_beta in catalogue check, but substituted standard switch (single output ActivationOut) during construction, connection, and save/load roundtripping.
    - Wire encoding: WorkshopAuthoringFlatBuffers.cs:183–187 silently defaulted missing port indices to 0u with validate: false instead of throwing when validate: true.
    - Decode boundary test missing: WorkshopAuthoringFlatBuffers.cs:297–298 for out-of-range port index.
    - Prevalidation mutation: WorkshopSavePrevalidation.cs:129–130 missing out-of-range FromSocket test.
    - Premature dead code: WorkshopConnections.cs:141–149 has dead Mechanical domain branch (ElementPortCatalogue rejects Mechanical until Story 9A.46e).
    - Boundary testing: Missing empty/whitespace string boundary tests for PortId, PortIdConverter, and ElementPortCatalogue.Admit.
  - Edge Case Hunter: FAIL.
    - WorkshopAuthoringFlatBuffers.cs:284 instances.ToDictionary throws unhandled System.ArgumentException on duplicate instances instead of WireFaultException.
    - WorkshopAuthoringFlatBuffers.cs:169-170 throws InvalidOperationException when connection references unowned instance with validate: false.
    - ActivationNetwork / SupplyRoute unhandled duplicate edge ArgumentException when multiple connections from same source connect to same target input role.
  - Intent Alignment Auditor: FAIL.
    - AC 1 generality verification gap: custom element dual_trigger was substituted with production switch; test lacked compilation and simulation tick execution under domain laws.
    - ui/WorkshopConnections.cs SocketLabel ignores PortId id and collapses multiple ports sharing a role to identical string.
  - Blind Hunter: FAIL.
    - Confirmed test substitution falsifying AC 1.
    - Confirmed wire exception safety defects.
    - Confirmed UI label collision and hardcoded PortId checks in ui/WorkshopConnections.cs and ui/WorkshopGuidance.cs.
  - Triage Actions:
    1. Fix PackageLocalPortsTests.cs to test genuine custom element (dual_trigger) with out_alpha and out_beta connected to separate lamps, verifying validation, save/load roundtrip with exact endpoints, compilation via WorkshopActivationCompiler, and signal propagation. Add Scoped method to DeclaredPorts and DeclaredPhysics to allow test-scoped registrations.
    2. Remove premature Mechanical branch in WorkshopConnections.cs:141-149.
    3. Fix FlatBuffers encoding and decoding exception safety in WorkshopAuthoringFlatBuffers.cs (check TryAdd for duplicates, handle unowned instances with FirstOrDefault, enforce WireFaultException).
    4. Add duplicate connection rejection in WorkshopConnections.Validate for activation and electrical to prevent graph duplicate edge exceptions.
    5. Add empty/whitespace validation in PortId constructor and corresponding boundary tests.
    6. Disambiguate port labels in ui/WorkshopConnections.cs when custom names or repeated roles are used, and check roles rather than hardcoded PortId in UI connection pickers.

## Verification

**Commands:**
- `dotnet build CuriousContraptions.slnx -c Debug --nologo` -- expected: 0 warnings, 0 errors. Passed (0 warnings, 0 errors).
- `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Debug --nologo --filter "FullyQualifiedName~PackageLocalPortsTests"` -- expected: all pass, 0 failed, Skipped: 0. Passed: 11 passed, 0 failed, 0 skipped.
- Run full verification block from `docs/delivery-workflow.md#local-test-authority` -- expected: all 5 test projects pass with Skipped: 0.
  - Receipts:
    - WorkerAotGuard.Tests.dll: 17 Passed, 0 Failed, 0 Skipped
    - Coverage.Tests.dll: 77 Passed, 0 Failed, 0 Skipped
    - BundleElements.Tests.dll: 236 Passed, 0 Failed, 0 Skipped
    - E2eRunner.Tests.dll: 192 Passed, 0 Failed, 0 Skipped
    - CuriousContraptions.tests.dll: 5,094 Passed, 0 Failed, 0 Skipped
    - Total: 5,616 Passed, 0 Failed, 0 Skipped
  - All 5 binlog captures clean: playtest.binlog, solution.binlog, web.binlog, check-staged-tests.binlog, verify-policy.binlog.
- Run `.githooks/pre-commit` on staged candidate -- expected: exit 0, Anvil clean. Passed: exit 0, 127 static gate tests passed, 0 skipped, Anvil scan 0 warnings/errors.
