---
title: 'Dynamic Puzzle Part Discovery and Extensible PartId Architecture'
type: 'refactor'
created: '2026-10-13'
status: 'done'
route: 'dispatch'
baseline_commit: '9107148592a934ff980b8f6201318968b8df35ea'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/docs/planning/requirements.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Puzzle parts are currently hard-coded into the closed `WorkshopPartKind` enum and manually registered through a 60-line hard-coded list of file paths in `PartRegistry.Discover()`. Adding any new catalogue part requires updating the enum, adding manual load statements, updating massive switch statements across the codebase, and breaking unrelated tests that assert closed enum arrays. This directly violates the core architectural rule that puzzle parts are extensible and discovered dynamically based on what is available.

**Approach:** Replace hard-coded part registration with dynamic directory discovery in `PartRegistry.Discover()` scanning `res://parts/catalog/*.tres` via Godot's `DirAccess.GetFilesAt`. Introduce strongly-typed `PartId` as an extensible identity for puzzle parts. Decouple `PartRegistry.Create`, `PartDefinition`, `MachinePart`, and `WorkshopInventoryPolicy.Free` from the closed enum so new parts can be authored, discovered, and used in Free Play without modifying core engine enum definitions.

## Boundaries & Constraints

**Always:** Follow the AGENTS.md rule: extensible identities use strongly typed IDs and resource strings use named boundaries. `PartRegistry.Discover()` dynamically inspects `res://parts/catalog/` at runtime and loads all valid `PartDefinition` resources. Each registered part is keyed by `PartId`. All existing 12 catalogue parts (Basketball, Bowling ball, Receiver, Ramp, Impact switch, Signal lamp, Wall, Delay, Pinball bumper, Domino, Battery, Springboard) retain their exact identities, visual scenes, and physics bindings.

**Never:** Do not hard-code lists of catalogue file paths in `PartRegistry.Discover()`. Do not require adding an enum member to `WorkshopPartKind` to register or instantiate a catalogue part. No backwards compatibility shims, dual registration paths, or fallback adapters.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Dynamic Discovery | `PartRegistry.Discover()` called | Scans `res://parts/catalog/`, dynamically loads each valid `PartDefinition`, and registers it by `PartId` | Malformed or invalid resources throw `ArgumentException` |
| Extensible Instantiation | `PartRegistry.Create(PartId id)` with registered id | Instantiates the part's scene from `definition.Scene` and calls `Configure(definition)` without switch statements | Unknown `PartId` throws `ArgumentException` |
| Dynamic Free Inventory | Workshop in Free Play mode | Palette dynamically lists every discovered part from `PartRegistry.Definitions` with `PartAllowance.Unlimited` | Empty registry throws `ArgumentException` |
| Catalog Id Preservation | Existing levels in `puzzles.json` with part kind strings | String part kinds resolve directly to `PartId` and instantiate corresponding parts | Unregistered part kind throws `ArgumentException` |
| Existing Part Regression | Run full native test suite (796 tests) | All tests pass with zero regressions | N/A |
| Chrome E2E Regression | Run Playwright E2E suites | Free Workshop and puzzle levels load, display discovered parts, and pass | N/A |

</frozen-after-approval>

## Code Map

- `engine/PartId.cs` -- New strongly typed extensible identity record struct wrapping `string`.
- `engine/PartDefinition.cs` -- Expose `PartId` derived from `Id`; decouple from closed enum.
- `engine/PartRegistry.cs` -- Dynamic discovery via `DirAccess.GetFilesAt("res://parts/catalog")`; registration and creation by `PartId`.
- `engine/MachinePart.cs` -- Store `PartDefinition` and identify parts by `PartId`.
- `engine/gpu/WorkshopInventory.cs` -- Key `PartInventory` by `PartId`; generate `Free` policy dynamically from registered definitions.
- `ui/Workshop.cs` -- Palette and part selection driven by `PartId`.
- `CuriousContraptions.tests/BasketballResourceTests.cs` and other test files -- Update tests asserting closed enum arrays to assert `PartId` / discovered definitions.
- `CuriousContraptions.tests/PartRegistryTests.cs` -- Dedicated unit tests for dynamic discovery, registration, and creation.

## Tasks & Acceptance

**Execution:**
- [x] `engine/PartId.cs` -- Implement `PartId` struct -- Extensible strongly typed part identity.
- [x] `engine/PartDefinition.cs` -- Expose `PartId` -- Decouple definition from closed enum.
- [x] `engine/PartRegistry.cs` -- Implement dynamic directory discovery in `Discover()` -- Scan `res://parts/catalog/` dynamically.
- [x] `engine/PartRegistry.cs` -- Refactor `Create(PartId)` -- Factory instantiates from definition scene without switch statement.
- [x] `engine/MachinePart.cs` -- Update part identity to `PartId` -- Consistent typed identity across scenes.
- [x] `engine/gpu/WorkshopInventory.cs` -- Support `PartId` in inventory and dynamic `Free` policy -- Auto-populate palette from discovery.
- [x] `ui/Workshop.cs` -- Adapt palette and tool choice to `PartId` -- UI reflects dynamically discovered parts.
- [x] `CuriousContraptions.tests/PartRegistryTests.cs` -- Add unit tests for dynamic discovery and `PartId` -- Verify dynamic catalogue loading.
- [x] `CuriousContraptions.tests/*.cs` -- Update existing tests asserting enum collections -- Zero warnings, 100% pass.

**Acceptance Criteria:**
- Given `res://parts/catalog/`, when `PartRegistry.Discover()` executes, then all valid catalogue `.tres` files are discovered and registered dynamically without any hard-coded file path list.
- Given any registered `PartId`, when `PartRegistry.Create(id)` is called, then the part is instantiated and configured from its definition.
- Given Free Workshop mode, when the palette opens, then all discovered parts appear dynamically.
- Given the complete native test suite, when executed via `dotnet test CuriousContraptions.slnx`, then all tests pass with zero warnings.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `dotnet test CuriousContraptions.slnx` -- expected: all native unit tests pass with zero warnings.
- `anvil check --changed` -- expected: zero errors and zero warnings.
