---
title: 'Catalogue conformance suite for every package (S4, all seven rows)'
type: 'refactor'
created: '2026-11-21'
status: 'blocked'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/docs/delivery-workflow.md'
  - '{project-root}/docs/planning/invest/delivery-batches.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Each element slice hand-writes the same baseline proofs for its package (parameters survive a save, bad parameter values are refused, Reset restores the authored state, ports refuse the wrong kind of link), and missing or weak baseline tests are the largest review finding class. Some of these proofs exist for every package already, but weakly, and others exist only for the packages whose slices remembered them.

**Approach:** Add one conformance suite that runs each delivered package through the production save codec, parameter validation, connection validation and Run/Reset, with a deliberately broken in-memory declaration proving each new row can fail. Delete the per-package tests that only repeat a row, and record where the rows that already exist for every package live. The art-against-collider row reads the solidity each package declares for its meshes, which the prerequisite slice `ENGINE-ART-SOLIDITY` provides; this slice does not start until that slice is published.

## Boundaries & Constraints

**Always:** Every row runs the real production code through a defined interface: `WorkshopInput.Generic`, `WorkshopSaveCodec`, `WorkshopAuthoringFlatBuffers.MapInstance`, `WorkshopConnections.Validate`, the Workshop editor's declared-parameter operation and `WorkshopSimulation` through the shared native harness. Expected values come from the package's declared data (bounds, ports, defaults), never from observed output. A refusal asserts its typed reason wherever the seam has one, and that the admitted construction is unchanged. The suite's cases are every delivered identity, taken from the loaded bundle, so a new package is covered with no edit for rows R1, R2 and R5; R3's placement comes from `PackageRuntimeParityTests.Placed` until S3 derives it. A broken declaration exists only in memory, through the scoped declaration seam, and is restored on dispose. A test is deleted only when a named row covers every assertion it makes; the record names each one. Follow the handoff order, Anvil before each write, one retained implementer, the engine review tier and one full local round with zero failures and `Skipped: 0`.

**Never:** An engine, bundle-schema, wire or tool change; the one production change is the behaviour-preserving extraction in task 8. Any art classification, collider or scene change: a row 4 violation in a delivered package returns to planning. A fixture package directory, project or bundle entry on disk. A stub or mutated copy of production code standing in for a broken package. Widening `NoFixedCapacityTests.AssertSameState` for its other callers. Deleting or editing the literal parameter table in `PackageRuntimeParityTests`. A per-package list, switch or expected-value table inside the suite. Hashes, whole-buffer comparison, fixed arrival ticks, exact equality on a simulated float other than authored Reset state, mutants, browser runs, skipped tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| R1a parameter round trip | Each delivered package; each declared parameter in turn at its declared minimum, its maximum, their midpoint (truncated for a whole-number or choice parameter) and its declared default, and for a real parameter also one f32 step above the minimum and one below the maximum; the others at their defaults | Save then load returns the same saved construction, and the loaded instance holds exactly the probed value | A package with no parameter round-trips its default instance with an empty parameter set |
| R1b connection round trip | Each declared port of each delivered package, joined to a delivered port that the production validator admits, with both parts configured so the two ports are available | Save then load returns the same construction with that one connection: same owners, ports, domain and rope length | A port that is available in no configuration, or that no delivered port may join, fails the row naming the package and port |
| R2 parameter refusal | Each declared parameter one step below its minimum, one step above its maximum, zero where zero lies below the minimum, minimum + 0.5 where it is a whole number or choice (skipped when minimum equals maximum), NaN, +∞ and −∞ | Refused at all six seams. Three report a typed reason: the editor's declared-parameter operation (`WorkshopEditRejection.OutOfRange`), the save decode (`OutOfRange` for a finite value, `NonFinite` otherwise) and the command instance decoder (`WireFaultReason.OutOfRange`). Three refuse by exception: the parameter collection, placement and an edit of an admitted construction. Where a parameter declares the clamping editor policy, the editor sets a finite value to the nearer bound instead of refusing it, and refuses only a non-finite one | The admitted construction still holds the default and its save still loads with the default. After an editor refusal the construction, its revision and the Undo stack are unchanged, the status line shows the refusal's message and the journal records a rejection, not a fault |
| R2 control | The same parameter at exactly its minimum and exactly its maximum | Admitted at all six seams | No error expected |
| R3 restoration | Each delivered package placed alone, run for 240 ticks at 120 Hz, then Reset | Every committed table of bodies, stores and observers equals the built state, and the committed tick equals the built tick: bodies, captures, activations, timers, contact work, memory, electrical, energy stores, gas stores, chamber regions, finite-work actuators, drive targets, integrity observers, airflow and coordinate-travel observers | A state that was run and not reset is reported as different; a difference in any one table is reported naming that table |
| R5 wrong-kind link | Each declared port of each delivered package: its R1b link with the domain changed to each of the other three, a rope length added or removed as that domain requires, and the other endpoint replaced by a delivered port valid for that domain; and for an input or output port a link joining it to the same port of a second instance | Refused with `WorkshopEditRejection.InvalidConnection` | The catalogue admits the port with its own declared domain and direction; a package with no port refuses any link that names it |
| R4 visible solid | Each delivered package's scene, at its default parameters and at each collider-bound parameter's minimum and maximum, with the solidity it declares for every mesh | Every mesh declared solid, hollow or an arc chord has one matching collider on the body that owns it, and no collider is left without art | A declared solid with no collider, or a collider with no art, fails naming the package and the node or collider |
| Broken declaration | A real entry re-identified and given one defect: a default above its maximum; a minimum above its maximum; ports absent from the installed port catalogue; a mesh declared solid that has no collider | Rows R1a, R2, R5 and R4 respectively report a violation naming the parameter, port or node | The installed declarations and port catalogue are unchanged after the scope ends |

</intent-contract>

## Code Map

Delivery shape: single structural slice (no element, family or closure). Review tier: **engine**: the suite is shared test-harness code, and one capability is *present, untested*. `baseline_revision` is captured at dispatch (coordinator decision 38, 23 November 2026): the published main that contains the planning work, captured at replanning before first implementation; scope guards compare against it. Drafted on `25df3cd5` (S1 and S2 published, 45 packages, the Baseball included), the drafting revision only, not a baseline. Line numbers were measured on `8d1d68aa`. S3 runs before the programme (owner decision 22), so R3's placement is derived by the time this slice starts.

State: **blocked on the engine programme (owner decision 22) and on prerequisites.** S4 is not on decision 22's list, so it waits for the programme (orders 1–10). The chain is `ELEMENTS-VISIBLE-SOLID-REMAINDER` (parts A, B, B2 and C; part A is `spec-elements-visible-solid-remainder.md`) and the Baseball repair, then `ENGINE-ART-SOLIDITY` (`spec-engine-art-solidity.md`, itself after the programme), then this slice. Owner: coordinator. Retry condition: `ENGINE-ART-SOLIDITY` published; this contract is then replanned from that revision (see At replanning). Nothing here is dispatched before then.

Planning verdict: returned to planning by the owner decisions of 21 November 2026 (hold all of S4; editor seam typed). The second-pass Scoped Pass (planning auditor `a79254a41d8e5a20c`) covered rows 1, 2, 3 and 5 as then written and does not cover the editor seam or row 4.

Owned scope:

- `CuriousContraptions.tests/CatalogueConformanceTests.cs` -- new: the suite, its row functions, the controls and the broken-declaration tests.
- No test-project edit: `CuriousContraptions.tests.csproj:77` compiles every top-level `*.cs` (S2, `25df3cd5`); `EnableDefaultCompileItems` stays false and needs no line.
- `CuriousContraptions.tests/F32BehaviourIdentityTests.cs` -- line 75, the Reset assertion of `AnElementPlacedAloneKeepsItsEventOrderResetsAndReplays`.
- Deleted theories and the data member each alone uses:
  - `CuriousContraptions.tests/WeightTests.cs` 84–112 (`RefusedMasses`, 7 cases);
  - `CuriousContraptions.tests/TrampolineTests.cs` 363–389 (`RefusedParameters`, 10);
  - `CuriousContraptions.tests/MotorTests.cs` 1019–1056 (`RefusedParameters`, 10);
  - `CuriousContraptions.tests/ConveyorTests.cs` 487–510 (`RefusedParameters`, 21);
  - `CuriousContraptions.tests/ImpactLeverTests.cs` 459–481 (two theories, 4 + 4);
  - `CuriousContraptions.tests/ClockGuardTests.cs` 143–155 (8);
  - `CuriousContraptions.tests/DeclaredOscillatorTests.cs` 53–79 (8);
  - `CuriousContraptions.tests/WorkshopPipeLengthTests.cs` 222–234 (4).
- `ui/WorkshopConfiguration.cs` -- 284–313, `Workshop.ApplyDeclaredParametersAsync`: extract its clamp-and-validate step (task 8).
- `CuriousContraptions.tests/PalettePlacementTests.cs` -- one new theory beside the scene helpers `BenchScene` (973) and `ClickBench` (task 9).
- `CuriousContraptions.tests/CSharpStaticGateTests.cs` -- the pinned tables that name the handler (`EditValidationPaths` 880–892, the catch count at 60), only if the extraction moves what they pin.
- `docs/delivery-workflow.md` -- lines 62 and 65, the registration-check paragraph and filter.
- `docs/planning/invest/delivery-batches.md` -- section S4 (lines 245–257).
- `_bmad-output/implementation-artifacts/deferred-work.md` -- the S4 entry at line 266 and the additions in task 7.
- `TODO.md`, this contract and `_bmad-output/implementation-artifacts/run-logs/spec-s4-catalogue-conformance-suite.md`.

### Capability probe

Performed while drafting, on `8d1d68aa`: nine present, one present and untested, three narrower (one repaired here), one missing until the prerequisite publishes.

- **Present.** Delivered identities and decoded entries: `SaveFixture.Elements` (`CuriousContraptions.tests/SaveFixture.cs:13`), `TestBundle.Entry` and `TestBundle.Ports` (`CuriousContraptions.tests/BundlePortsTests.cs:19–27`). Test: `F32BehaviourIdentityTests.AnElementPlacedAloneKeepsItsEventOrderResetsAndReplays` (member data at 28–33).
- **Present.** Save codec: `WorkshopSaveCodec.Encode` and `Decode` (`engine/gpu/WorkshopSaveCodec.cs:19,48`). Test: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`.
- **Present.** Declared-bound validation: `InstanceParameterCollection.Validate` (`engine/gpu/InstanceParameters.cs:37–49`) and `WorkshopInput.Generic` (`engine/gpu/WorkshopConstruction.cs:31–39`). Test: `WeightTests.AnOutOfRangeOrNonFiniteMassIsRefusedAtEverySeamAndChangesNothing`.
- **Narrower.** Those two seams and `WorkshopConstruction.Validate` refuse with `ArgumentException` and a message, not a typed reason. Row 2 asserts the exception type there (owner decision O3).
- **Narrower, repaired here (editor seam).** `Workshop.ApplyDeclaredParametersAsync` (`ui/WorkshopConfiguration.cs:284–313`) builds `WorkshopEditRejection.OutOfRange` at 302 and hands it to `WorkshopUiFailure.Report` (`ui/WorkshopUiFailure.cs:27–37`), which records a `WorkshopFaultDiagnostic` (`CuriousContraptions.Abstractions/RuntimeDiagnostics.cs:28`: kind, site, exception type, detail) and returns the message. A native test can drive the operation without a browser, as `PalettePlacementTests.ThePlateMinimumMassEditorRefusesOutOfRangeWithoutClampAndAppliesAsOneUndo` (191) and the helper at 1041–1045 do, and can observe the rejection kind, the exception type, the status text, the construction and Undo. It cannot observe the reason enum: the record does not carry it, and carrying it would change the diagnostics wire. The smallest change that makes the reason assertable is task 8: the clamp-and-validate step becomes one internal method that throws the typed refusal, and the handler catches and reports it exactly as now. No test names `WorkshopEditRejection.OutOfRange` today.
- **Present.** Typed save refusal: `WorkshopSavePrevalidation` (`engine/gpu/WorkshopSavePrevalidation.cs:104` `NonFinite`, `:113` `OutOfRange`). Test: `WorkshopSaveRejectionTests.EveryViolationIsItsTypedRefusal`.
- **Present.** Typed command-instance refusal: `WorkshopAuthoringFlatBuffers.MapInstance` (`engine/gpu/WorkshopAuthoringFlatBuffers.cs:335`). Test: `DeclaredContactLoadTests.AMissingOrOutOfRangeMinimumMassIsRefusedAtBothBoundaries`.
- **Present.** A stored save of one generic record with chosen parameter values: `PayloadInstanceTests.GenericSave` (`CuriousContraptions.tests/PayloadInstanceTests.cs:348`) and `SaveFixture.Refused` (`SaveFixture.cs:33`). Test: `TrampolineTests.AnOutOfRangeOrNonFiniteParameterIsRefusedAtEverySeamAndChangesNothing`.
- **Present.** Port admission and link refusal: `ElementPortCatalogue.Has` and `Of` (`engine/gpu/ElementPortCatalogue.cs:225,219`), `WorkshopPort.IsAvailable` (`engine/gpu/WorkshopConnections.cs:28`), `WorkshopConnections.Validate` (`:71`, refusal at `:114`). Test: `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused`.
- **Present.** Run and Reset of one construction: `WorkshopIntegrationHarness.ObserveReset` (`CuriousContraptions.tests/WorkshopIntegrationHarness.cs:420`). Test: the `F32BehaviourIdentityTests` theory above.
- **Present, untested.** Comparison across Reset of the eight tables that `NoFixedCapacityTests.AssertSameState` (`CuriousContraptions.tests/NoFixedCapacityTests.cs:279–302`) leaves out, although production compares them in `WorkshopRead.SamePhysicalContent` (`CuriousContraptions.Abstractions/WorkshopRead.cs:19–31`): `PhysicsEnergyStoreRead.HasSameBits` (`PhysicsEnergyStoreRead.cs:26`), `PhysicsGasRead.HasSameValues` and `PhysicsIntegrityRead.HasSameValues` (`PhysicsGasRead.cs:40,69`), `PhysicsChamberRead.HasSameBits` and `PhysicsFiniteWorkActuatorRead.HasSameBits` (`PhysicsChamberRead.cs:21,44`), `PhysicsDriveTargetRead.HasSameBits` (`RevoluteConstraintValues.cs:78`), `PhysicsAirflowRead.HasSameValues` (`PhysicsAirflowRead.cs:25`), `PhysicsCoordinateTravelRead.HasSameValues` (`CoordinateTravelValues.cs:53`). No test asserts them restored for every package; this slice adds it (R3). `Presentation` and `Motion` are excluded as derived or transient.
- **Present.** Scoped installation of an in-memory declaration: `DeclaredPhysics.Scoped` and `Contains` (`engine/gpu/DeclaredPhysics.cs:41,110`), `DeclaredPorts.Scoped` and `ElementPortCatalogue.Extend` (`engine/gpu/ElementPortCatalogue.cs:320,89`). Test: `BundleElementIdTests.AnUnfamiliarDeclarationPassesSaveAndWorkerAdmission`.
- **Narrower (row 7).** No production query states which capability families a package uses. What is observable is already proven for every package and is adopted, not rewritten: it compiles and steps (`WorkbenchCapacityTests.EveryFreePaletteKindCompilesAndStepsPastTheRetiredPartTable`), its project references only the abstractions it implements (`PackageReferenceTests.EveryPackageReferencesExactlyTheAbstractionsItsDeclarationImplements`), its compiled rows produce exactly its declared event kinds (`EventRingProducerTests.EveryPackageDeclaresExactlyTheKindsItsCompiledRowsProduce`), and no shipping source names a package (`ElementIsolationTests.ShippingSourcesContainNoPackageCoupling`).
- **Missing until the prerequisite publishes (row 4).** Nothing declares which meshes of a package's art are solid, hollow, arc chords or decorative; the classification is the hand table `VisibleSolidColliderTests.Specs` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:65–161`, 33 of 45 packages at `25df3cd5`, the Baseball not among them until its repair). `ENGINE-ART-SOLIDITY` adds `ISceneGeometryElement.SceneArt`, classifies all 44 and makes `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` read it for every package. Row 4 is then adopted where it lives, like rows 6 and 7, and this slice adds its broken-declaration control.

Row 6 (catalogue metadata resolves) is present for every package and is adopted: `PartRegistryTests.Load_RegistersEveryBundleElementFromItsPackageInPaletteOrder` (identity, title, description, category, colour, icon path, scene path and loaded scene), `BundleValidatorTests.EveryRealPackageShipsItsIconAndScene`, and the broken cases in `PartRegistryTests.AnInvalidBundleRefusesTheWholeLoadNamingTheProblem`.

### How the broken declaration stays out of the product

It is a `BundleEntry` value built in the test from a real entry and installed with the two scoped seams. It is never a directory under `elements/`, a project, or a line of `elements/elements.bundle.json`, so none of these see it: the generator's glob (`tools/BundleElements/BundleElements.csproj:26`) and `PackageDiscovery`; `CSharpStaticGateTests.TheSolutionListsEveryCSharpProject` and `SolutionMembershipTests`; `CurrentCatalogueTests` (declaration resources against bundle identities); the E2eRunner package mapping (bundle scene paths); the export filter (`export_presets.cfg:9`). `ElementIsolationTests.ShippingSourcesContainNoPackageCoupling` and the `PartRegistryTests` source scans read shipping sources, not the test project. The test assembly runs without parallelism (`CuriousContraptions.tests/BundlePortsTests.cs:11`), so a scope cannot leak into another test. Rows 6 and 7 already have their broken packages: the `Fixture` declaration class of `tools/BundleElements.Tests/Fixtures.cs`, compiled only into that test project. Row 3 has no declaration that could break it, because Reset is engine behaviour; its proofs that it can fail are the not-reset state and the per-table control.

### Direct consumers

- `docs/delivery-workflow.md:65` -- the registration filter gains `FullyQualifiedName~CatalogueConformanceTests` and `FullyQualifiedName~F32BehaviourIdentityTests.AnElementPlacedAlone`, so all four rows run before handoff in every element slice.
- No schema, generated file, TypeScript caller, hook or tool changes. `tools/anvil/CheckStagedTests.cs` scans the staged new file for skip markers as it does any test source.

### Decisions

Decided, with reason:

- **D1. Rows 3, 6 and 7 stay in the classes that already prove them for every package.** Moving passing tests is churn; the suite's summary comment lists all seven rows and where each lives.
- **D2. Row 3 strengthens the existing all-package theory instead of adding a second simulation per package.** The theory already runs each package alone for 240 ticks and resets it.
- **D3. Cases come from `SaveFixture.Elements`.** It is the delivered set the save boundary admits, and the pattern is already used for theory data.
- **D4. The 22-entry hand list `WorkshopSaveRejectionTests.EveryKind()` (125–149) is not widened here.** It feeds a pre-validation parity sweep over pose and integer lanes, which is outside the seven rows; task 7 records it as deferred work.
- **D5. Kept, because each asserts more than a row:**
  - a reloaded run or a compiled value: `WeightTests.SaveRoundTripsPoseRotationAndMassExactlyWithNoWireChange`, `TrampolineTests.SaveRoundTripsPoseRotationTensionAndDampingExactly`, `WorkshopWallTests.ThreeCanonicalDimensionsPoseAndIdentityRoundTripAtDefaultAndBothLimits`, `DeclaredContactLoadTests.AdmittedMinimumMassesRoundTripAtBothBoundaries`;
  - evaluated geometry and unknown keys: `WorkshopWallTests.InvalidSubmittedParametersRejectAtomicallyWhileExactF32ValuesRoundTrip`;
  - a missing or undeclared parameter: `DeclaredContactLoadTests.AMissingOrOutOfRangeMinimumMassIsRefusedAtBothBoundaries`, `DeclaredOscillatorTests.AnUndeclaredOrMissingParameterIsRefusedAtBothBoundaries`, `WeightTests.AMissingMassOrAnUnknownKeyIsRefusedWithoutDefaultFill`;
  - the loaded construction's lifecycle and the empty bench: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad`.

### Owner decisions (21 November 2026)

- **O1. Hold all of S4.** The suite is one slice with all seven rows, blocked on `ENGINE-ART-SOLIDITY`. No S4a or S4b.
- **O2. The literal parameter table** (`PackageRuntimeParityTests.cs:105–127, 468–638`) is deleted in a follow-up slice after this suite publishes; it is a queued item of its own and is not touched here.
- **O3. Go through the editor.** Row 2's edit seam is the editor operation, so three seams assert a typed reason and three assert refusal.

### At replanning

Re-verify on the revision that publishes `ENGINE-ART-SOLIDITY`, before this contract can pass planning:

1. The name and shape of the solidity declaration, the name of the all-package visible-solid test, and that it runs for every delivered identity with no hand table.
2. Every line number, the delivered count N and the three baseline totals (each the current measurement at that revision); R3's placement as S3 derived it.
3. That the nine deletions still exist unchanged and nothing new repeats a row.
4. The partner search of task 3 and the editor-theory time on that revision; the registration check must stay under a minute or the editor term leaves the filter.
5. The static-gate pins named under Owned scope against the extraction.
6. A second planning audit of the whole contract.
7. The `PrintedFlat` and `Enclosed` checks of task 11: what the declaration offers to find each entry's face or enclosing solid, the test names, and their cases in the totals.

### Planning record

No run log exists yet; the coordinator moves these two entries to it at first dispatch.

- **R1b partner search.** Over the delivered ports in declaration order: same domain, complementary direction (rope with rope, at 1 m), each part at its defaults or with one choice option set until `WorkshopPort.IsAvailable` holds; the first pair `WorkshopConstruction.Validate` admits is used. Read from `elements/elements.bundle.json`, not run: 76 ports in seven classes, each with a complement; only the three `fan` inputs are gated; both level outputs have the `lamp` level input. Watch the pulley's `Guide` socket and the gated `fan` ports in task 3.
- **Deleted tests and their cover.** The Weight, Trampoline, Motor, Conveyor and Clock theories refuse out-of-range, zero, negative and non-finite values at one to five of R2's seams (R2); the Oscillator theory adds that both bounds load bit for bit (R2 control, R1a); the Pipe theory round-trips 1, 2, 3.6 and 8 m (R1a at minimum, default and maximum). The two Impact lever theories pass one of the part's two parameters, so they were refused for an incomplete set whatever the value; that refusal stays proven by `WeightTests.AMissingMassOrAnUnknownKeyIsRefusedWithoutDefaultFill` and `WorkshopWallTests.InvalidSubmittedParametersRejectAtomicallyWhileExactF32ValuesRoundTrip`, and their out-of-range values by R2.

## Tasks & Acceptance

**Execution:**

1. `CuriousContraptions.tests/CatalogueConformanceTests.cs` -- create the class in the native scene collection with a summary listing the seven rows and where each is proven; add theory data from `SaveFixture.Elements` in ordinal order. No test-project edit (the S2 glob compiles it).
2. `CuriousContraptions.tests/CatalogueConformanceTests.cs` -- write each new row as a function from an identity to a list of violations (`ParameterRoundTripViolations`, `ConnectionRoundTripViolations`, `ParameterRefusalViolations`, `WrongKindLinkViolations`), and the four theories that assert each list empty. The functions read the installed declarations (`DeclaredPhysics.Of`, `DeclaredPorts.Current`), not `TestBundle`, pass `SaveFixture.Elements.Append(id)` to `Decode` (precedent `BundleElementIdTests.cs:113–126`), and turn a thrown refusal into a violation carrying its type and message -- one function serves the delivered packages and a re-identified declaration.
3. `CuriousContraptions.tests/CatalogueConformanceTests.cs` -- before writing R1b, run the partner search once and record the admitted pair for each of the 76 ports in the run log -- AC5 decides a port without one.
4. `CuriousContraptions.tests/CatalogueConformanceTests.cs` -- add `internal static IReadOnlyList<string> RestorationViolations(WorkshopRead built, WorkshopRead reset)` covering the fifteen tables of R3; it reports a tick difference and, per table, a count difference or a content difference, naming the table. `CuriousContraptions.tests/F32BehaviourIdentityTests.cs:75` -- replace the `AssertSameState` call with an assertion that this list is empty -- R3.
5. `CuriousContraptions.tests/CatalogueConformanceTests.cs` -- add the three broken-declaration facts, the not-reset control and `EveryComparedTableReportsADifference`, as the table specifies.
6. The eight test files listed under Owned scope -- delete the nine theories (76 cases) and the four data members only they use (`WeightTests.cs:84`, `TrampolineTests.cs:363`, `MotorTests.cs:1019`, `ConveyorTests.cs:487`); record each deletion and its covering row in the run log and in the Auto Run Result.
7. `docs/delivery-workflow.md:62,65` -- state what the suite covers and that a new package needs no baseline test for the seven rows; add the four filter terms. `docs/planning/invest/delivery-batches.md` S4 -- mark the story delivered with all seven rows and name the tests. `_bmad-output/implementation-artifacts/deferred-work.md` and `TODO.md` -- record, each with source, evidence and owner: the `EveryKind()` list (D4), the queued deletion of the literal parameter table (O2, owner: the slice after this one), any table that no delivered package populates, and any store whose charged restoration has no interaction test.
8. `ui/WorkshopConfiguration.cs` -- extract from `ApplyDeclaredParametersAsync` (291–305) one `internal static` method that takes the part's declared parameters and the submitted values, applies the declared editor policy and validation, and throws `WorkshopEditRejectedException` with `WorkshopEditRejection.OutOfRange`; the handler calls it, catches that exception and reports it as it does now. No other behaviour changes -- makes the typed reason assertable without a diagnostics wire change.
9. `CuriousContraptions.tests/PalettePlacementTests.cs` -- add the editor theory named in the table: per delivered identity, `BenchScene(id, PlacementAnswer.Applied)`, `ClickBench`, then `ApplyDeclaredParametersAsync` with each R2 probe; and assert the reason by calling the extracted method with the same values -- R2 editor seam.
10. `CuriousContraptions.tests/CatalogueConformanceTests.cs` -- add the row 4 controls named in the table, built on the test declaration class the prerequisite uses for its own refusals -- R4.
11. `CuriousContraptions.tests/FaceMarkingTests.cs` -- drive the flush-print check from every `PrintedFlat` entry of every package's declared solidity and delete its hand lists `Parts` (9–13) and `IsPrint` (25–30); add the executed check for `Enclosed` entries. The declaration carries no face or enclosing-solid reference (`ENGINE-ART-SOLIDITY`, decision D9): the test finds the face a printed mesh lies on, and the solid an enclosed mesh lies in, geometrically from the package's colliders, and fails an entry for which no collider face or enclosing collider exists -- row 4's decorative reasons.

Tasks 8 and 9 precede task 2's refusal row only in that R2's theory names six seams; they share no code.

**Acceptance Criteria:**

- AC1: Given a package identity that the suite's source never names, installed through the scoped seams from a real entry, when the four row functions run for it, then each returns no violation, so a new package is covered by rows R1, R2 and R5 with no suite edit.
- AC2: Given the candidate and N delivered identities at its baseline, when the full local round runs, then every project reports zero failed and `Skipped: 0`, and the total is the current full local round at the baseline + 5N + 7 − 76 (five N-case theories and seven facts added; 7 + 10 + 10 + 21 + 4 + 4 + 8 + 8 + 4 cases removed, recounted at replanning). With N = 45 (drafting revision) that is + 156. The registration check grows by 7N + 7, because the existing R3 and R4 theories and the editor theory join the filter.
- AC3: Given the nine deleted theories, when the run log is read, then each is named with the row that covers every one of its assertions.
- AC4: Given the candidate diff, when its paths are listed, then no path lies outside `CuriousContraptions.tests/`, `docs/`, `_bmad-output/` and `TODO.md`.
- AC5: Given a delivered package for which a row reports a violation, when the implementer confirms it at the cited seam, then it is repaired as a declaration or product defect in this slice. If the repair needs a solver, ABI, wire or shared-presentation change, work stops and the finding returns to planning. A port that the validator can admit only inside a larger construction also returns to planning; it is not repaired as a defect. A row 4 violation in a delivered package is an art or collider defect and returns to planning; it is not repaired here.

## Spec Change Log

## Review Triage Log

## Design Notes

1. **Behaviour and scope.** Seven baseline behaviours proven for every delivered package through production seams; nothing a player sees changes. Source: `docs/planning/invest/delivery-batches.md` S4, all seven rows.
2. **Reusable primitive.** Row functions that return violations, so the same code proves the catalogue and shows a broken declaration failing. No new production primitive.
3. **Declared composition.** Probes, links and expectations are computed from each installed entry's declared parameters and ports. The suite holds no identity, no per-package branch and no expected-value table.
4. **Execution and ownership.** Test code only. Scoped declarations are disposed in the test that installs them; the assembly runs serially.
5. **Generality.** Every delivered identity, every declared parameter and port, the zero-parameter and zero-port boundaries, an unfamiliar identity (AC1) and three broken declarations.
6. **Forward-only.** Deletes nine per-package theories (76 cases) that only repeat rows 1 and 2. The tests kept beside a row are those in D5, each for an assertion the row does not make.
7. **Prerequisites.** `ENGINE-ART-SOLIDITY` published; blocked until then. Rules E1, E2, E4, E7, E8 and E9 apply (E2 for the typed editor refusal); E3, E5, E6 and D1–D7 are untouched: the one production change is an extraction with unchanged behaviour.

The R1b partner search and the cover for each deleted test are in the Code Map's Planning record.

## Verification

Executor for every command: the implementation owner. Raw output goes under `.tools/s4-evidence/`; each result is one line here citing its run-log entry. A change to test source, the project file or the toolchain invalidates the logs and test binaries.

### Acceptance-to-test table

| Rows | Named discriminating test | Control and boundary | Run |
|---|---|---|---|
| R1a | `CatalogueConformanceTests.EveryDeclaredParameterRoundTripsThroughSaveAndLoad` (new theory, N cases) | Control: `ADefaultAboveItsMaximumFailsTheParameterRoundTripRow` (new fact; the `ramp` entry re-identified with `length` defaulting above its maximum fails the default probe of `length` at `InstanceParameterCollection.Validate` and the violation names `length`). Boundary: minimum, maximum and their one-step neighbours; a package with no parameter | planned |
| R1b | `CatalogueConformanceTests.EveryDeclaredPortRoundTripsAConnectionThroughSaveAndLoad` (new theory, N cases) | Control: each R5 refusal is this row's admitted link with its domain changed and its other endpoint replaced. Boundary: a package with no port; a port inactive at its defaults | planned |
| R2 | `CatalogueConformanceTests.EveryDeclaredParameterRefusesValuesOutsideItsBoundsAtEverySeam` (new theory, N cases) | Control: exactly minimum and exactly maximum are admitted at the five seams this theory drives; `AMinimumAboveItsMaximumFailsTheParameterRefusalRow` (new fact) reports that a declared bound is refused. Boundary: one f32 step outside a real bound, one whole step outside a whole-number or choice bound, zero below a positive minimum | planned |
| R2 editor seam | `PalettePlacementTests.EveryDeclaredParameterEditorRefusesOutOfRangeWithItsTypedReason` (new theory, N cases): for each probe the journal gains one `WorkshopFaultDiagnostic` of kind `Rejection` and type `WorkshopEditRejectedException`, the status line shows its message, the construction, its revision and the Undo stack are unchanged, and the extracted method throws with reason `OutOfRange` | Control: exactly minimum and exactly maximum are applied as one Undo step. Boundary: a clamping parameter takes the nearer bound for a finite probe and refuses NaN | planned |
| R4 | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (existing after the prerequisite, N cases, unchanged) | Controls: `CatalogueConformanceTests.ADeclaredSolidWithoutAColliderFailsTheVisibleSolidRow` and `AColliderWithoutArtFailsTheVisibleSolidRow` (new facts). Boundary: each collider-bound parameter at its minimum and maximum | planned |
| R3 | `F32BehaviourIdentityTests.AnElementPlacedAloneKeepsItsEventOrderResetsAndReplays` (existing theory, N cases, now asserting `RestorationViolations` empty) | Controls: `CatalogueConformanceTests.AStateThatWasRunAndNotResetFailsTheRestorationRow` (new fact; for `ball`, the last run read against the built read reports the tick and the body table, and the ball has moved more than 0.05 m); `CatalogueConformanceTests.EveryComparedTableReportsADifference` (new fact; for each of the fifteen tables, the built read of the first delivered package whose table is non-empty, chosen from the reads with no identity named, against an empty bench's built read reports a violation naming that table; a table no delivered package populates is listed in the run log). Boundary: packages that declare no store | planned |
| R5 | `CatalogueConformanceTests.EveryDeclaredPortRefusesALinkOfTheWrongKind` (new theory, N cases) | Control: the unchanged R1b link is admitted and the catalogue admits each port with its own domain and direction; `PortsMissingFromTheCatalogueFailThePortRow` (new fact; the `switch` entry re-identified and installed without its ports reports a violation naming the port). Boundary: a package with no port | planned |
| AC1, scope end | Each of the three broken-declaration facts first runs the unmodified re-identified entry through its row and requires no violation, and after dispose asserts `DeclaredPhysics.Contains(id)` is false and `DeclaredPorts.Current` equals the catalogue captured before | The defect is the single difference | planned |
| Rows 6, 7 | Existing tests named in the capability probe, unchanged | Existing | planned |

The one physical quantity is the control's 0.05 m: after 2 s of free fall from 3 m a ball has reached the bench about 2.7 m below, so 0.05 m is a visible move with a wide margin, and it is five orders of magnitude above f32 spacing at that height. It proves the comparison saw a changed state; it is not a bound on the fall. Reset equality is exact because authored Reset state is exact by contract, and it is compared table by table as decoded typed rows, never as bytes. Probed parameter values are exact because a saved field holds the value written.

R3 proves restoration of what a lone, unpowered run changes. Restoration of a charged or drained store stays with the interaction tests, named here by title and not re-read: `GasRuntimeTests.ExactContactOpensFiniteStoreAndResetRestoresPreparedState` (gas), `MotorTests.ResetRestoresTheAuthoredRotorAndZeroesTheDriveStateAndLamp` (drive target), `LatchSupplyFixtureTests.RunResetRestoresLatchOffBladeClosedAuthoredBattery` (electrical), `CannonIntegrationTests.TwentyRunResetsAndFiveSaveLoadsRestoreOnlyAuthoredConstruction` (energy store), `WorkshopSimulationTests.PaidWorkStepInstallationRejectsAtomicallyAndResetRestoresPreload` (finite-work actuator). Task 7 records in deferred work any store for which the implementer finds none.

**Commands:**

- The compiler-proof capture in `docs/delivery-workflow.md#reproducible-local-verification`, up to and including the `verify-policy.binlog` build -- expected: five logs, zero warnings and errors.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root "$PWD"` -- expected: PASS with both canary `PASS` lines and the `Coupling dependencies:` summary. Run when the candidate first compiles and again here.
- The registration check in `docs/delivery-workflow.md#new-package-registration-check`, with the four new filter terms (`CatalogueConformanceTests`, `F32BehaviourIdentityTests.AnElementPlacedAlone`, `PalettePlacementTests.EveryDeclaredParameterEditor`, `VisibleSolidColliderTests.EverySolidArtPart`) -- expected: baseline count + 7N + 7, 0 failed, 0 skipped, still under a minute.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~CatalogueConformanceTests|FullyQualifiedName~F32BehaviourIdentityTests|FullyQualifiedName~WeightTests|FullyQualifiedName~TrampolineTests|FullyQualifiedName~MotorTests|FullyQualifiedName~ConveyorTests|FullyQualifiedName~ImpactLeverTests|FullyQualifiedName~ClockGuardTests|FullyQualifiedName~DeclaredOscillatorTests|FullyQualifiedName~WorkshopPipeLengthTests|FullyQualifiedName~PalettePlacementTests|FullyQualifiedName~VisibleSolidColliderTests|FullyQualifiedName~CSharpStaticGateTests"` -- expected: 0 failed, 0 skipped; every test in the table appears; the nine deleted theories do not. The four new theories simulate nothing and the suite adds one 240-tick run and about N constructs, so `CatalogueConformanceTests` alone is expected under 30 s.
- `git add -N CuriousContraptions.tests/CatalogueConformanceTests.cs _bmad-output/implementation-artifacts/run-logs/spec-s4-catalogue-conformance-suite.md`, then `git diff --check <baseline_revision>` -- expected: no output.
- One full local round under `caffeinate -i`, the last two lines of the capture block -- expected: baseline total + 5N + 7 − 76 (re-measure N and the baseline at replanning), 0 failed, `Skipped: 0` for every project.
- At the candidate commit, the unmodified `.githooks/pre-commit` -- expected: pass. Its test selection is unchanged in count, and its snapshot must compile the new file.

**Manual checks (if no CLI):**

- Read `CatalogueConformanceTests.cs` for package identities: the only ones are `ramp` and `switch`, from which the broken declarations are built, and `ball` in the not-reset control.

### Running beside other work

This slice cannot start until the engine programme and `ENGINE-ART-SOLIDITY` are published. After that it can be implemented in its own worktree beside an element slice, but publication is serialised. Shared files touched: `docs/delivery-workflow.md`, `_bmad-output/implementation-artifacts/deferred-work.md`, `TODO.md`, `docs/planning/invest/delivery-batches.md`; their cited lines are re-read at replanning (S2 has published and moved them). S3 and this slice both edit `CuriousContraptions.tests/F32BehaviourIdentityTests.cs` (S3 line 39 and the test name above it, this slice line 75) and `docs/delivery-workflow.md:62`; after S3, R3's placement is derived and AC1 extends to it. Whichever of these publishes second merges main once and repeats the full round. An element slice that publishes first is covered by rows R1, R2 and R5 with no edit; if it added a per-package copy of a row, that copy is left for its own closure.
