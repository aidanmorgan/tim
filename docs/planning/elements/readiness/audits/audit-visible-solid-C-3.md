# Planning audit 3: ELEMENTS-VISIBLE-SOLID-REMAINDER part C (Cannon, Windmill, Balloon)

Contract: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-remainder-c.md` (5,304 words). Checkout `spec-drafts` at published main `25df3cd5` plus uncommitted planning work. Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md`. Phase: planning; read-only; nothing built or run. Audited independently and as one serial set with parts A, B, B2 and the Baseball repair; earlier passes (`audit-visible-solid-C.md`, `-C-2.md`) were not relied on.

## Verdict: Fail

One defect sits inside `<intent-contract>` (F1: an unsupported statement about how the Balloon string's exception is declared). Separately, by its own rule (lines 54, 231–233) this contract depends on part B's `Solid.Inner` and low-restitution theory and is replanned and re-audited on the main that publishes B2; it cannot be ready for development before then whatever its text. Owner decision 5's conditional is applied correctly.

## What was verified at `25df3cd5`

- Scene citations: `cannon.tscn` 88, 103, 116, 128, 155, 163, 170 (proxies `col_` hidden mesh style, `:132–158`); `windmill.tscn` 1 (`load_steps=20`, 21 after `Shape_Output`), 61, 88, 112, 202; `WindmillElementDeclaration.cs:38–39` (nine occluders today: base 0–3, rotor 0–4).
- Imported rings read from the `.obj` sources: jacket (0.48, 0.58, half 0.55), collar (0.48, 0.65, 0.05), sleeve (0.5, 0.565, 0.07), guard (0.745, 0.815, 0.035); 64 outer segments each, faceting 0.7–1.0 mm, inside the 5 mm bound. They agree with the hidden proxies' radii and `wall_thickness`. The sleeve stays inside the jacket's wall over x −0.45 to −0.15.
- Capability symbols: `Chambers.cs:80`, `:112`, `:142`, `:162`; `RotaryAirflowValues.cs:14`; `AirflowSceneValidation.cs:77–80`; `IsobaricGas.cs:130`; `GasStoreDescriptor.cs:9`; `WorkshopDeclaredPresentation.cs:402`, `:637`; `MachinePart.cs:211`; `ElementPresentation.cs:184`; `BundleSchema.cs:46–47`; `MachineWorld.Gpu.cs:251–252`, `:317`; `CannonArtTests.cs:22–24`; `BalloonTests.cs:151–152`; `WindmillAdmissionTests.cs:188`; `WindmillArtworkTests.cs:77`; `.githooks/pre-commit:52–54`. The Balloon's `Envelope` binding is `CommittedGasRadius` / `UniformScale` / clamp (`BalloonElementDeclaration.cs:29–31`); scene paths `Envelope`, `Neck`, `String`, `Carrier` have no `Visual/` prefix, as the row says.
- Drops: C1 indicator apex 0.72, breech top 0.58, rear collar 0.495 m away; C5 `Back` wheel face 0.5, nacelle face 0.275, guard 0.436 m away. Static restitution 0 for both (declarations), so part B's stop-only theory applies.
- C6: with Fans at (0, 5, 0) and (6, 5, 0) and the Windmill at (3, 5, 0) (`WindmillIntegrationTests.cs:213`, `:453–455`), the rear stream's ray to the sample at (−0.12, 0, 0.42) crosses the wheel (x 0.01–0.49, z 0.38–0.5), so the tenth occluder is needed.
- Counts: rows 41 at start (42 with the Baseball repair) + 3 = 44 or 45; AC4 +10; AC2 as stated. `content/puzzles.json` has no Cannon or Windmill.
- Decisions: Mechanical link schematic (owner 2, coordinators 25 and 36) at line 132; the Balloon string non-solid (owner 1); decision 5's conditional is (b) proceed with the named defect, (a) stop and wait for `ENGINE-ONE-WAY-INSTALL` (lines 150–155), and the `first-committed-sample` entry names that owner.

## Blocking finding

### F1. The string's exception is said to be declared by `ENGINE-CONNECTION-MATERIALS` as a "thin-string material"

- Where: line 23, "declared in `ENGINE-CONNECTION-MATERIALS` as the thin-string material".
- Defect: no source says this. Owner decision 1 says "the Balloon string keeps its earlier exception"; Batch I's story text says "Thin wires and the Balloon string stay non-solid drawings" (`docs/planning/elements/readiness/batch-i.md:88`); the engine stories index gives `ENGINE-CONNECTION-MATERIALS` "material record, solid strand, straight static strand, typed values on a connection" (`engine-stories-index.md:79`). The string is package art (`balloon.tscn:46`), not a connection, and `spec-engine-art-solidity.md` (D5, D7) declares it as the package's `NonSolidTether` decorative entry with a cited owner decision, which this contract's own C9 (line 43) relies on.
- Consequence: the read-only intent assigns the exception to the wrong story and mechanism, contradicting C9 and the art-solidity contract.
- Fix: replace "it is the thin-string exception of owner decision 1 (22 November 2026), declared in `ENGINE-CONNECTION-MATERIALS` as the thin-string material, while every rope, cable, hose and duct is solid." with "it keeps its named exception (owner, 21 November 2026; owner decision 1, 22 November 2026), which `ENGINE-ART-SOLIDITY` declares as the package's cited decorative entry, while every rope, cable, hose and duct is solid."
- Classification: wrong specified behaviour (intent text).

## Required fixes (outside the intent)

### R1. State what C8 can and cannot settle for owner decision 5

- Where: lines 86, 150–155, 164.
- Defect: decision 5 makes the outcome depend on "the slice's test". C8 runs the evaluator and the part's application natively; the worker and client legs that deliver the first sample in build mode (`BrowserWorkshopClient.Schedule.cs:89–99`, `BrowserWorkshopClient.DeclaredPresentation.cs:133–147`) are read only. Outcome (b) can be chosen while the browser still shows 0.0625 m for longer than "momentary".
- Fix: add to outcome (b): "C8 settles the native legs only; the `first-committed-sample` entry adds a queued browser case asserting the envelope's radius on the first rendered frame after construction and after Reset, for the post-catalogue E2E phase."
- Classification: test (evidence recorded).

### R2. The Baseball repair's Cannon test, if it has published

- Where: row C4/C12 (line 214).
- Defect: the Baseball repair adds `BaseballTests.BaseballNeverSeatsInTheCannon` (R6, the `CannonIntegrationTests.Vertical` construction with a Basketball and a Baseball). If it publishes before this part, it consumes the four new Cannon colliders.
- Fix: add "`BaseballTests.BaseballNeverSeatsInTheCannon`, if present" to C12's Cannon list and `|FullyQualifiedName~BaseballTests` to the filter.
- Classification: test.

### R3. Baseline precondition

- Where: lines 54–58, 69, 152, 170 cite owner decisions 1, 2, 5, 22, the Repairs table and `deferred-work.md#first-committed-sample`; at `25df3cd5` none exists (the entry is uncommitted, `deferred-work.md:249–253` in this checkout), so "the existing entry ... no second entry" (task 7) would be false at that baseline.
- Fix: add to **Baseline**: "Precondition: the planning commit that adds `docs/planning/elements/readiness/`, the Repairs table and the `first-committed-sample` entry is on main, and parts B and B2 are published; the coordinator captures the baseline at or after them."
- Classification: wrong specified behaviour (Code Map).

### R4. Stale record lines

- Line 58: replace with "Planning audit 3 (`.tools/audit/audit-visible-solid-C-3.md`, 23 November 2026): Fail on F1; re-audit at replanning on part B2's published main, as 'At replanning' requires."
- Line 56: "every product and test file cited here is unchanged" is false for `PackageRuntimeParityTests.cs` (Baseball rows at `fa86969f`, named in C12); say "every file cited with a line number".

## Serialisation

Last of the four: after parts B and B2 (it reuses part B's `Solid.Inner` and `ABallDroppedOntoEachLowRestitutionFeatureStopsAtItsDrawnSurface`, names that agree with part B's text); the Baseball repair before or after. One publisher at a time on `VisibleSolidColliderTests.cs`. Part A's Fan posts and rings and part B2's seam do not reach the Windmill tests' geometry (Fans 3 m from the rotor). No overlap with S3, S5, S6 beyond `TODO.md`, `deferred-work.md` and `delivery-batches.md`.

## Decisions for the owner

None new. Decision 5 is applied as written. The floating track and wheel are correctly left as non-blocking with default "leave as drawn".

## Evidence limits

- Nothing was built or run. `ImportedStep` (5e-5 m) assumes Godot's import stores positions within two 16-bit steps of the `.obj` values; unexecuted.
- The rendered frames between construction or Reset and the first sample were not measured (R1).
- The windmill output spokes (15 mm proud, turned by a binding) stay decorative under part A's D5; the C5 drop lands under a drawn spoke, which is visual only.
