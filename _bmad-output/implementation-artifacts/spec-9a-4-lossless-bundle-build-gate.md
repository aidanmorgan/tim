---
title: 'Story 9A.4: Lossless Bundle Build Gate'
type: 'feature'
created: '2026-10-18'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md#story-9a4-lossless-bundle-build-gate'
  - '{project-root}/_bmad-output/planning-artifacts/architecture-conformance-audit-2026-10-18.md'
  - '{project-root}/docs/architecture/element-package-isolation.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The bundle is lossy and hand-assembled, and nothing builds it.
- `tools/BundleElements/Program.cs:21-36` hand-lists 13 declarations. Its csproj hand-lists 13 `ProjectReference`s.
- `:38-50` serialises anonymous objects with System.Text.Json. STJ ignores the `Vector3` and `Quaternion` fields, so every value is written as `{}` (audit A13).
- Domain payloads are never written: `IMechanicalElement` mass and anchors, `IElectricalElement` nodes, `ISignalElement` triggers, latch and counter.
- `GeneratedAt = DateTime.UtcNow` (`:41`) makes the output nondeterministic.
- It writes three copies (`:54-59`). Only `parts/elements.bundle.json` is tracked, and nothing reads any of them.
- `ElementMetadata` (`CuriousContraptions.Abstractions/ElementMetadata.cs:5-11`) carries an untyped `Parameters` bag and a string `Category`.
- Package categories disagree with the catalogue in 9 of 13 packages:
  - packages use `Balls`, `Obstacles`, `Mechanisms`, `Structures`, `Logic`, `Indicators` and `Sensors`;
  - `parts/catalog/*.tres` uses exactly {Motion, Goals, Power, Control, Structure}.
- Package ports are snake_case strings (`"power_in"`, `elements/SystemClock/SystemClockElementDeclaration.cs:24`), but the runtime uses the `WorkshopSocket` enum.
- Every package `IconPath` names a missing `res://parts/icons/*.png`. The directory does not exist; Story 9A.18 owns icons.
- Scenes exist.

**Approach:**
- One deterministic MSBuild-driven generator discovers `elements/*/*.csproj` and reflects over their `IPuzzleElementDeclaration` implementations.
- It validates them and writes one typed, versioned, lossless bundle.
- A temporary parity guard proves the packages equal today's runtime tables, so no player-visible value changes.

## Boundaries & Constraints

**Always:**
- **f32:** written in shortest round-trip form and re-read bit-exactly. Vectors are `[x, y, z]` and quaternions `[x, y, z, w]`.
- **Closed sets are enums:** `ElementCategory`, `ParameterKind`, `ColliderShapeKind`, `SimulationDomain`, `PortDirection` and the domain roles. JSON enum names are parsed with unknown names rejected.
- **Failures name the package and the field.**

**Never:**
- A runtime reader. Story 9A.14 adds it.
- A hand list or a timestamp.
- A `Half` lane.
- Changing a runtime value to match a package. Packages adopt the runtime values (D2).

## I/O & Edge-Case Matrix

| Scenario | Input | Expected |
|---|---|---|
| Duplicate id | two fixture packages `id: "x"` | build fails `BUNDLE001 duplicate id 'x' (FixtureA, FixtureB)` |
| Non-finite | fixture collider half-extent `NaN` | fails `BUNDLE002 FixtureNaN.Colliders[0].HalfExtents.x` |
| Unknown enum | `(ElementCategory)99` | fails `BUNDLE003 FixtureEnum.Metadata.Category` |
| Body without collider | mechanical fixture, `Colliders` empty | fails `BUNDLE004 FixtureBody.Colliders` |
| Package with 0 or 2 declarations | glob project yields 0/2 types | fails `BUNDLE005` (control for discovery) |
| Signed zero and subnormal | `-0f`, `1e-45f` | round-trip equal bit for bit |

</frozen-after-approval>

## Code Map

- `CuriousContraptions.Abstractions/ElementMetadata.cs`:
  - **delete** `Parameters` and `string Category`;
  - **add** `ElementCategory Category`, `IReadOnlyList<ParameterDescriptor> Parameters` and a nullable `IconPath` (null until 9A.18).
- New: `CuriousContraptions.Abstractions/ElementCategory.cs`, `ParameterDescriptor.cs` (`Key`, `Unit`, `ParameterKind`, `Minimum`/`Maximum`/`Default` f32, `EnumOptions`), and `ElementFootprint.cs`. The footprint is declared; it mirrors `WorkbenchFootprint` (`engine/gpu/WorkbenchCapacity.cs:31`).
- `CuriousContraptions.Abstractions/IPuzzleElementDeclaration.cs`: add `ElementFootprint Footprint`. `ColliderDescriptor.Dimensions` becomes `HalfExtents`; that is what runtime extents are (`WorkshopCanonicalColliders.cs:62` versus the clock package).
- `elements/*/*ElementDeclaration.cs` (13 files): categories, typed parameters, socket-named ports, footprint, and `IconPath = null`.
- `tools/BundleElements/Program.cs` and `tools/BundleElements/BundleElements.csproj`: the generator.
- New: `tools/BundleElements/BundleSchema.cs` (versioned DTOs) and `BundleValidator.cs`.
- `CuriousContraptions.web/CuriousContraptions.web.csproj`: target `GenerateElementBundle` with `BeforeTargets="TwoDogExportGamePack;_WasmGenerateAppBundle"`.
- `.gitignore`: add `parts/elements.bundle.json`, then `git rm --cached` it.
- Tests:
  - new project `tools/BundleElements.Tests/` (in the slnx): round trip, validator fixtures, slnx membership;
  - `CuriousContraptions.tests/BundleCatalogueParityTests.cs` (new, with a `Compile Include` and `ProjectReference`s to the package glob).

## Tasks & Acceptance

**Execution.** Depends on 9A.0. [R:9R.0]: the slnx already lists `elements/*`, the domain Abstractions and `BundleElements` on branch `worktree-agent-a08aebc94d551b53b`. Verify after merge; don't re-add.

1. [ ] **Tests first (red).**
   - `tools/BundleElements.Tests/BundleValidatorTests.cs`: the matrix rows with fixture declarations compiled only into the test project.
   - `BundleRoundTripTests.cs`: for every discovered package, deserialise(serialise(d)) equals d. f32 comparison uses `BitConverter.SingleToInt32Bits`.
   - `SolutionMembershipTests.cs`: glob `elements/*/*.csproj` and the four `CuriousContraptions.Abstractions.*` projects, plus `tools/BundleElements`, must each be ⊆ the slnx `Project Path` entries.
2. [ ] Abstractions types (Code Map). Delete the bag and the string category.
3. [ ] The 13 packages. Set:
   - categories from `.tres`: ball Motion, ball_detector Control, battery Power, bumper Motion, clock Control, counter Control, domino Motion, lamp Goals, latch Control, powered_gate Motion, pressure_plate Control, ramp Structure, wall Structure;
   - ports named exactly as `WorkshopSocket` members (`PowerIn`, `Supply`, `ActivationIn`/`Out`, `SetIn`, `ResetIn`), with domain Signal for runtime `Activation`;
   - footprint equal to `WorkbenchFootprint.Of`;
   - parameter schemas from the `MachineWorld.CaptureInstance` defaults and the element specs: plate `minimum_mass` kg 0.1–16, default 0.5; counter `target_count` whole number; clock `interval_seconds`; ramp and wall dimensions; battery `enabled` enum.
4. [ ] `tools/BundleElements/*`:
   - `ProjectReference Include="../../elements/*/*.csproj"`;
   - load the referenced assemblies from the deps file;
   - reflect concrete public types implementing `IPuzzleElementDeclaration` that have a public parameterless constructor;
   - require exactly one per package;
   - validate, serialise `BundleSchema` (`SchemaVersion = 1`) and write only `$(OutDir)elements.bundle.json` and `parts/elements.bundle.json`;
   - delete the hand list, the anonymous serialiser, `GeneratedAt` and the `AppBundle` copies.
5. [ ] `CuriousContraptions.web.csproj` target. Also make `BundleElements.csproj` run the generator `AfterTargets="Build"`, so that `dotnet build CuriousContraptions.slnx` fails on an invalid package.
6. [ ] `CuriousContraptions.tests/BundleCatalogueParityTests.cs`. For each packaged id, compare against the runtime:
   - the `.tres` metadata (id, category, scene, display key);
   - `WorkshopPorts.For` and `WorkshopConnections.LocalPosition`;
   - `WorkshopCanonicalColliders.For`;
   - `WorkbenchFootprint.Of`;
   - materials;
   - the `CaptureInstance` defaults.
   
   Also assert that the unpackaged set equals exactly {basket, bowling, delay, funnel, pipe, pipe_bend_45, pipe_bend_90, spring, switch, tennis, balloon}. Header comment: "Deleted by Story 9A.14 with the tables it compares."
7. [ ] `.gitignore` and `git rm --cached parts/elements.bundle.json`.

**Acceptance Criteria (1:1 with epics 9A.4):**
- **AC1.** Given the solution, when it builds, then `CuriousContraptions.slnx` contains every `elements/*` project, the four domain Abstractions and `tools/BundleElements`. Membership is proven by test.
- **AC2.** An MSBuild target runs `BundleElements` before the Godot export and before publish. It discovers packages by globbing `elements/*/*.csproj` and reflecting `IPuzzleElementDeclaration`, with no hand list.
- **AC3.** The bundle schema is typed and versioned:
  - f32 vectors `[x, y, z]` and quaternions `[x, y, z, w]`, bit-exact;
  - mechanical (mass, inertia, material, anchors), electrical (nodes) and signal (triggers, latch, counter) payloads;
  - a typed parameter schema;
  - an enum category.
- **AC4.** Generation fails the build, naming the package and field, for a duplicate id, a non-finite value, an unknown enum or a body without a collider (fixture negative controls).
- **AC5.** A round-trip test proves every declaration equals its deserialised entry, f32 bit for bit.
- **AC6.** `BundleCatalogueParityTests` proves that metadata, category, ports, colliders, footprint, material and parameter defaults equal the runtime values for every packaged element, and that the unpackaged ids are exactly the 11 listed.
- **AC7.** Package categories adopt the catalogue's player-facing categories, so palette grouping is unchanged.
- **AC8.** `dotnet build CuriousContraptions.slnx` reports 0 warnings, `dotnet test` passes 100%, and every `tools/e2e/*.test.ts` suite passes 100% in one serial Chrome run against the production preview. There is no runtime change.

## Design Notes

**Decisions (owner playability delegation):**
- **D1. Categories become the 5-member `ElementCategory`** taken from `.tres`. Rationale: the palette must not reshuffle (epics, coordinator decision).
- **D2. Parity compares at the runtime lane's precision.**
  - Package f32 values are narrowed to the runtime type (`(Half)package == runtime`) until Story 9A.44.
  - Packages keep the authored decimal literals. The runtime tables were built from those same literals, so narrowing reproduces them.
  - Packages never author widened-`Half` artefacts. Until 9A.44 the runtime narrows the bundle exactly as it narrows today's literals, so the cutover is invisible.
  - Rationale: the f32 source of truth stays clean, and the refactor is invisible.
- **D3. Footprint is declared, not derived.** Rationale: it is the simplest testable contract. Derivation would need capability declarations owned by 9A.19 and 9A.20.
- **D4. `IconPath` is null until 9A.18.** Rationale: a declared path to a missing file is a false declaration.
- **D5. Port ids use `WorkshopSocket` names** at the bundle boundary. Rationale: one identity end to end. Story 9A.17 parses them into the enum.

**Mutations:**
- Drop one vector component from the DTO: round trip fails.
- Remove the duplicate check: BUNDLE001 fixture passes when it must fail.
- Change one package category: parity fails.
- Delete one package csproj from the slnx: membership fails.
- Restore `GeneratedAt`: the determinism test fails. That test builds twice and compares bytes.

**Impacted suites:**
- new `BundleElements.Tests`;
- `CuriousContraptions.tests` (parity, `PartRegistryTests`);
- `tools/Coverage.Tests` (catalogue paths, [R:9.4b]);
- all Chrome suites (no runtime change).

**Risks:**
- Parity will expose real package/runtime divergences. Fix the packages, never the runtime; any divergence that is a runtime defect goes to the coordinator.
- The generator loads assemblies through the deps file. That is safe here, because the tool is a JIT console app and never trimmed or AOT-compiled. If a package fails to load, generation fails with that package's name.
- The Godot `.pck` export does not include the JSON. That is Story 9A.14's job (`export_presets.cfg` lists files explicitly).

## Verification

- `dotnet build CuriousContraptions.slnx`: expected 0 warnings; `parts/elements.bundle.json` regenerated and ignored (`git check-ignore`).
- `rg -n '"Dimensions": \{\}|"LocalPosition": \{\}' parts/elements.bundle.json`: expected 0.
- `dotnet test tools/BundleElements.Tests`, and `dotnet test CuriousContraptions.tests --filter BundleCatalogueParityTests`: expected pass.

## Legacy Deleted

- `tools/BundleElements/Program.cs:7-19,21-36,38-50,54-59`: the element `using`s, the hand list, the anonymous serialiser, `GeneratedAt` and the `AppBundle` copies.
- The 13 hand-written `ProjectReference`s in `tools/BundleElements/BundleElements.csproj`.
- `ElementMetadata.Parameters` (`IReadOnlyDictionary<string, object>`) and `string Category`.
- `ColliderDescriptor.Dimensions` (renamed to `HalfExtents`).
- The tracked `parts/elements.bundle.json`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Appendix: reference census

- **`elements.bundle.json`:** `tools/BundleElements/Program.cs:56-58` and tracked `parts/elements.bundle.json`. No runtime reader, csproj, `export_presets.cfg`, `.tres`/`.tscn` or doc reference. The `AppBundle/` copies are git-ignored (`.gitignore:13`).
- **`ElementMetadata.Parameters` and `.Category`:** the 13 `elements/*/*ElementDeclaration.cs` files and `BundleElements/Program.cs:44`. No engine, UI or test consumer: `rg IPuzzleElementDeclaration` outside `elements/` finds only Abstractions and BundleElements.
- **Runtime comparators:**
  - `WorkshopPorts.For` at `engine/gpu/WorkshopConnections.cs:19`;
  - `LocalPosition` at `:99`;
  - `WorkshopCanonicalColliders.For` at `engine/gpu/WorkshopCanonicalColliders.cs:20`;
  - `WorkbenchFootprint.Of` at `WorkbenchCapacity.cs:35`;
  - `CaptureInstance` at `engine/MachineWorld.Gpu.cs:90`.
- **slnx:** the main tree lists only Abstractions, FlatBuffers ([R:9.4b]), 2dog, game, tests, Coverage(+Tests) and web. 9R.0 adds the rest.
