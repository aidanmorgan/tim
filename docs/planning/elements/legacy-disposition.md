# Legacy disposition ledger

This ledger accounts for every tracked path that Epic 7 (Stories 7.1–7.4) deletes, as inventoried in [research-epic-7-legacy-inventory.md](../../../_bmad-output/implementation-artifacts/research-epic-7-legacy-inventory.md) at baseline commit `a6c914e`. It was built in the Story 7.0 closing pass from the legacy sections (§4, "Legacy harvest" or "Sources and legacy") of every [element spec](README.md). **Stories 7.1–7.4 may not delete a path until it has a row here.**

The deletion scope was listed with `git ls-tree -r --name-only a6c914e` and the inventory's scope rules: compiled files in `CuriousContraptions.csproj` and `CuriousContraptions.tests.csproj` are excluded; `parts/scenes/*.tscn` and `parts/catalog/*.tres` rows cover the entries that bind a deleted script.

**Row rule.** "Harvested into" lists every spec whose legacy section cites the path (spec IDs resolve through the [index](README.md)). A path no spec cites carries a reason code from the legend below ("no element knowledge"), or **GAP** when it plausibly holds element knowledge that no spec captures. All batch associations are synchronized with the current specs; the frozen candidate and its narrow corrections are covered by [fresh independent review](../../../_bmad-output/implementation-artifacts/review-7-0-resumed.md).

**Granularity.**
- One row per file for `parts/*.cs`, `engine/physics/`, `engine/bridge/`, dead `engine/*.cs` and the uncompiled tests. A file's Godot `.uid` sidecar shares its row ("+ `.uid`").
- One row per file for `parts/scenes/*.tscn` and `parts/catalog/*.tres`.
- One row per file for the parked GPU pipe declarations `engine/gpu/WorkshopPipe.cs` and `engine/gpu/AnnularProfile.cs`.
- One row per folder (or root file) for `reference/`, the deleted `tools/<tool>` folders, `diagnostics/` and `CuriousContraptions.Geometry/`.

## Counts

| Scope | Rows | Harvested | No element knowledge | GAP |
| --- | --- | --- | --- | --- |
| 7.1 `reference/` (folders and root files) | 26 | 10 | 16 | 0 |
| 7.2 `tools/`, `diagnostics/`, `CuriousContraptions.Geometry/` (folders) | 23 | 3 | 20 | 0 |
| 7.3 uncompiled `CuriousContraptions.tests/` | 417 | 239 | 178 | 0 |
| 7.4 `engine/physics/` | 135 | 59 | 76 | 0 |
| 7.4 `engine/bridge/` | 13 | 4 | 9 | 0 |
| 7.4 dead `engine/*.cs` | 71 | 50 | 21 | 0 |
| 7.4 parked GPU pipe declarations | 2 | 2 | 0 | 0 |
| 7.4 uncompiled `parts/*.cs` | 43 | 43 | 0 | 0 |
| 7.4 `parts/scenes/*.tscn` | 60 | 60 | 0 | 0 |
| 7.4 `parts/catalog/*.tres` | 60 | 60 | 0 | 0 |

"Harvested" includes 3 sources whose behaviour is harvested only through their tests (noted in the row). "No element knowledge" includes 11 tests of a harvested source (noted in the row).

## Reason codes

| Code | No element knowledge because |
| --- | --- |
| N-SOLVER | Generic legacy CPU solver internals (constraints, impulses, contact, loads, iteration, trajectories). Replaced by the worker TGS Soft solver; do not carry forward (CPU solver path). |
| N-GEOM | Generic legacy CPU geometry, collision and sweep queries. Replaced by the worker broadphase and narrowphase. |
| N-TRANSFER | Generic legacy mechanical-transfer and power-port framework (CPU). Element drive behaviour is harvested from the part scripts and element tests (CAT-018, CAT-019, CAT-042, CAT-057, CAT-070, CAT-071). |
| N-INFRA | Legacy lifecycle, publication, observation, identity, parameter or serialization infrastructure. The current path is `WorkshopSimulation`, the worker pose ring and typed declarations. |
| N-PRESENT | Legacy presentation, animation or render-cadence infrastructure. Replaced by the Epic 4 animation worker and the current presentation bindings. |
| N-PERF | Performance, allocation or backend-qualification instrumentation. |
| N-QUAL | Generic backend-qualification diagnostics (body, collision, joint, motor, rope and sweep probes), compiled only by `tools/Performance` and the P0-007 probes. |
| N-FIXTURE | Test fixture or helper. |
| N-TOOL | Checks for the deleted Playtest audit tooling. |
| N-PRODUCT | Campaign id and progress bookkeeping (a product feature, not an element). |
| N-PROBE | P0 review or probe tool (inputs, diffs, harness projects): review evidence, not element behaviour. |
| N-GEOMLIB | `CuriousContraptions.Geometry` value types (vectors, poses, affine transforms), used only by the P0-007 tools and `PortableGeometryProof`. |
| N-ARTEFACT | Build or CI artefacts: restore assets JSON, DLLs, evidence archives, pages `artifact.zip`. |
| N-SNAPSHOT | Pre-change snapshot of files that remain in the current tree (worker, `MachineWorld`, workshop client, tests); superseded by the current sources. |
| N-BUILD | Godot import-exclusion marker. |
| N-DOC | Describes the hashes of the `reference/cpu` snapshots; no element facts. |

## 7.1 LEGACY-0a: `reference/`

| Folder or file | Tracked files | Harvested into | Notes |
| --- | --- | --- | --- |
| `reference/.gdignore` | 1 | — | N-BUILD |
| `reference/CAT-001-I-r1` | 8 | CAT-001 | |
| `reference/P0-022-before` | 24 | CAT-005, CAT-007, CAT-013, CAT-016, CAT-017, CAT-018, CAT-019, CAT-020, CAT-024, CAT-025, CAT-026, CAT-027, CAT-033, CAT-034, CAT-037, CAT-039, CAT-051, CAT-052, CAT-053, CAT-057, CAT-058, EL-060, EL-063, EL-066, EL-068, EL-126, EL-127, EL-128, EL-129, EL-130, EL-131, EL-132, EL-156, EL-157, EL-158, EL-201, EL-207, TH-01, TH-02, TH-03, TH-04, TH-08, TH-09, TH-10, TH-12, TH-14, TH-15, TH-16, TH-21, TH-22, TH-23, TH-26, TH-27, TH-28, TH-29, TH-30, TH-31, TH-32, TH-33, TH-34, TH-37, RAD-01, RAD-02, RAD-03, RAD-04, RAD-06, RAD-07, RAD-08, RAD-09, RAD-10, RAD-12, RAD-13, RAD-14, RAD-15, RAD-16, RAD-17, RAD-18, RAD-20, RAD-21, RAD-22 | |
| `reference/P0-022-candidate-r1` | 9 | CAT-016 | |
| `reference/P0-022-candidate-r2` | 7 | — | N-ARTEFACT |
| `reference/P0-025-activation-r1.tar.gz` | 1 | — | N-ARTEFACT |
| `reference/P0-025-before` | 22 | — | N-SNAPSHOT |
| `reference/P0-025-candidate-r1.tar.gz` | 1 | — | N-ARTEFACT |
| `reference/P0-025-clock-recovery-20261003.tar.gz` | 1 | — | N-ARTEFACT |
| `reference/P0-025-response-v2-production-appbundle-20261003.tar.gz` | 1 | — | N-ARTEFACT |
| `reference/README.md` | 1 | — | N-DOC |
| `reference/cpu` | 6 | CAT-001, CAT-002, CAT-003, CAT-005, CAT-006, CAT-007, CAT-008, CAT-009, CAT-010, CAT-011, CAT-012, CAT-013, CAT-014, CAT-016, CAT-017, CAT-018, CAT-020, CAT-021, CAT-028, CAT-029, CAT-031, CAT-032, CAT-033, CAT-034, CAT-036, CAT-037, CAT-038, CAT-039, CAT-040, CAT-041, CAT-042, CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, CAT-051, CAT-053, CAT-055, CAT-056, CAT-057, CAT-058, CAT-059, CAT-060, CAT-061, CAT-062, CAT-064, CAT-065, CAT-067, CAT-068, CAT-069, CAT-070, CAT-071, CAT-072, EL-016, EL-028, EL-029, EL-044, EL-076, EL-077, EL-089, EL-090, EL-098, EL-120, EL-121, EL-122, EL-123, EL-124, EL-125, EL-133, EL-138, EL-139, EL-140, EL-141, EL-142, EL-143, EL-144, EL-145, EL-146, EL-153, EL-154, EL-169, EL-170, EL-173, EL-174, EL-175, EL-176, EL-177, EL-178, EL-180, EL-183, EL-187, EL-189, EL-196, EL-200, EL-204, EL-205, EL-208, EL-210, EL-211, EL-212, EL-213, EL-214, TH-06, TH-25 | Includes `MachinePart.cs` (AddBox half-size rule, default material) and `MachineWorld.cs` (rope length at connect). |
| `reference/p020-clean-worker-assets-20261004` | 2 | — | N-ARTEFACT |
| `reference/p020-pages-37167090792` | 1 | — | N-ARTEFACT |
| `reference/p020-pages-37167784179` | 1 | — | N-ARTEFACT |
| `reference/p020-pages-37168599707` | 1 | — | N-ARTEFACT |
| `reference/p025-production-isolated-20261004` | 218 | CAT-042, CAT-053, CAT-058, CAT-067 | 206 of the tracked files are under `src/` (a source snapshot); the rest are build timings, worker scripts, `inputs.json` and `reconciliation.patch`. |
| `reference/p054-contact-replay` | 3 | CAT-054 | |
| `reference/p054-guide` | 6 | CAT-004, CAT-005, CAT-019, CAT-042, CAT-053, CAT-054, CAT-058, CAT-064, CAT-065, CAT-067, CAT-071 | |
| `reference/p054-pages-37177119773` | 1 | — | N-ARTEFACT |
| `reference/p054-pages-37178846416` | 1 | — | N-ARTEFACT |
| `reference/p063-pages-37183382369` | 1 | — | N-ARTEFACT |
| `reference/p066-pages-37187386165` | 1 | — | N-ARTEFACT |
| `reference/pipe` | 68 | CAT-048 | |
| `reference/switch-lamp` | 3 | CAT-063, TH-36 | |
| `reference/wall` | 3 | CAT-066 | |

**Untracked.** `reference/p025-production-isolated-20261004/{diagnostic,production}-appbundle/` (2,474 untracked files): archive at Story 7.1, no element source. Ignored files under `reference/` (1,816, including the p025 `src` build outputs, `primitive-current`, `bin/` and `obj/`) are not tracked and have no row.

## 7.2 LEGACY-0b: `tools/`, `diagnostics/` and `CuriousContraptions.Geometry`

| Folder | Tracked files | Harvested into | Notes |
| --- | --- | --- | --- |
| `tools/Campaign/` | 5 | CAT-003, CAT-004, CAT-005, CAT-014, CAT-015, CAT-018, CAT-019, CAT-022, CAT-023, CAT-028, CAT-029, CAT-033, CAT-035, CAT-048, CAT-049, CAT-050, CAT-053, CAT-054, CAT-057, CAT-058, CAT-059, CAT-062, CAT-063, CAT-064, CAT-065, CAT-066, CAT-067, CAT-071, EL-001, EL-023, EL-024, EL-025, EL-028, EL-039, EL-053, EL-054, EL-057, EL-058, EL-060, EL-061, EL-062, EL-065, EL-066, EL-070, EL-074, EL-075, EL-076, EL-126, EL-128, EL-138, EL-181, EL-193, EL-194, EL-211, TH-01, TH-02, TH-03, TH-04, TH-05, TH-06, TH-07, TH-08, TH-09, TH-10, TH-11, TH-12, TH-13, TH-14, TH-15, TH-16, TH-18, TH-21, TH-22, TH-23, TH-26, TH-27, TH-28, TH-29, TH-30, TH-31, TH-32, TH-33, TH-34, TH-36, TH-37, RAD-01 | Includes the springboard, trampoline and wound-spring lessons. |
| `tools/LifecycleContractReview/` | 1 | — | N-PROBE |
| `tools/P0-007-actual-path-probe/` | 7 | CAT-007, CAT-016, CAT-018, CAT-034, CAT-039, CAT-051, CAT-052, CAT-057, CAT-065, CAT-071 | |
| `tools/P0-007-binarysum-review/` | 2 | — | N-PROBE |
| `tools/P0-007-caller-review/` | 2 | — | N-PROBE |
| `tools/P0-007-cancellation-review/` | 2 | — | N-PROBE |
| `tools/P0-007-contact-replay/` | 2 | — | N-PROBE |
| `tools/P0-007-contraction-review/` | 2 | — | N-PROBE |
| `tools/P0-007-frozen-review/` | 3 | — | N-PROBE |
| `tools/P0-007-newton-review/` | 3 | — | N-PROBE |
| `tools/P0-007-review/` | 2 | — | N-PROBE |
| `tools/P0-007-slip-review/` | 2 | — | N-PROBE |
| `tools/Performance/` | 5 | — | N-PERF |
| `tools/Performance.Tests/` | 2 | — | N-PERF |
| `tools/Playtest/` | 7 | CAT-016, CAT-071 | |
| `tools/PortableGeometryProof/` | 3 | — | N-PROBE |
| `tools/p0-003-reconstruction/` | 10 | — | N-PROBE |
| `tools/p0-003-review/` | 2 | — | N-PROBE |
| `tools/p0-004-review-r2/` | 2 | — | N-PROBE |
| `tools/p0-004-review/` | 2 | — | N-PROBE |
| `tools/p0-007-probe/` | 2 | — | N-PROBE |
| `diagnostics/` | 50 | — | N-QUAL |
| `CuriousContraptions.Geometry/` | 6 | — | N-GEOMLIB |

`tools/p0-002-review` is empty and untracked. `tools/Coverage.Tests/` (4 tracked files) is kept by ruling; see [Kept by ruling](#kept-by-ruling).

## 7.3 LEGACY-0c: uncompiled `CuriousContraptions.tests/`

| File | Harvested into | Notes |
| --- | --- | --- |
| `CuriousContraptions.tests/AccelerationDriveTests.cs` | CAT-042 | |
| `CuriousContraptions.tests/AccelerationReactionSystemTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/AccelerationSolverTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/AcousticEmitterCheckpointTests.cs` | CAT-009, CAT-061, CAT-069 | |
| `CuriousContraptions.tests/AcousticMotionBindingTests.cs` | CAT-009 | |
| `CuriousContraptions.tests/AcousticMotionTests.cs` | CAT-009, CAT-061 | |
| `CuriousContraptions.tests/AcousticTests.cs` | CAT-061, EL-044, EL-180 | |
| `CuriousContraptions.tests/AcousticWavefrontBindingTests.cs` | CAT-009 | |
| `CuriousContraptions.tests/AcousticWavefrontTests.cs` | CAT-009, CAT-061, CAT-069 | |
| `CuriousContraptions.tests/ActuatorBodyGeometryTests.cs` | CAT-007, CAT-051 | |
| `CuriousContraptions.tests/AdiabaticGasDischargeTests.cs` | CAT-010, EL-039 | |
| `CuriousContraptions.tests/AdmissibleImpulseResponseTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/AffineDeclarationTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/AirJetBoundaryTests.cs` | CAT-028, EL-108, EL-209 | |
| `CuriousContraptions.tests/AirJetGeometryTests.cs` | CAT-028, EL-108, EL-209 | |
| `CuriousContraptions.tests/AirJetOcclusionTests.cs` | CAT-028, EL-108, EL-209 | |
| `CuriousContraptions.tests/AirJetWorldTests.cs` | EL-108, EL-209 | |
| `CuriousContraptions.tests/AirflowConservationTests.cs` | CAT-010 | |
| `CuriousContraptions.tests/AlignedPowerPortTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/AngularPathMeasureTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/AngularPowerPortTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/AngularVelocityAnimationTests.cs` | CAT-042 | |
| `CuriousContraptions.tests/AnimationBatchTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/AnimationEndpointTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/AnimationFollowTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/AnimationImpulseTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/AnimationOscillationTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/AnimationPulseEnvelopeTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/AnimationRateTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/ArtworkBoundsTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/AssistanceOwnershipTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/AssistancePhysicsTests.cs` | CAT-029, CAT-039 | |
| `CuriousContraptions.tests/AssistanceTests.cs` | CAT-054, CAT-062 | |
| `CuriousContraptions.tests/AuthoredOrientationSceneTests.cs` | CAT-016, CAT-034, CAT-054 | |
| `CuriousContraptions.tests/AxialDampingTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/AxialEffortTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/AxialElasticPotentialTests.cs` | — | N-SOLVER. Tests `engine/physics/AxialElasticPotential.cs`, harvested by CAT-062, CAT-071. |
| `CuriousContraptions.tests/AxialGasPotentialTests.cs` | CAT-010, TH-19 | |
| `CuriousContraptions.tests/AxialMotionOwnershipTests.cs` | CAT-007, CAT-039, CAT-051, CAT-070 | |
| `CuriousContraptions.tests/BallDetectorTests.cs` | CAT-002 | |
| `CuriousContraptions.tests/BasicTests.cs` | CAT-001, CAT-004, CAT-054 | |
| `CuriousContraptions.tests/BasketGuideTests.cs` | CAT-004, EL-192 | |
| `CuriousContraptions.tests/BasketResidenceOwnershipTests.cs` | CAT-004, EL-192 | |
| `CuriousContraptions.tests/BeamCombinerTests.cs` | CAT-006, CAT-011, CAT-021, CAT-031, CAT-040, CAT-041, CAT-055, CAT-068, CAT-072, EL-143, EL-144, EL-145, EL-149, EL-150, EL-151, EL-152, EL-174, EL-175, EL-213 | |
| `CuriousContraptions.tests/BeamShutterTests.cs` | CAT-007 | |
| `CuriousContraptions.tests/BeamSplitterTests.cs` | CAT-008, CAT-012, CAT-021, CAT-032, CAT-038, CAT-040, CAT-056, CAT-068, CAT-072 | |
| `CuriousContraptions.tests/BellTests.cs` | CAT-009, CAT-060, CAT-066 | |
| `CuriousContraptions.tests/BellowsTests.cs` | CAT-010, CAT-066, CAT-069, CAT-070 | |
| `CuriousContraptions.tests/BilateralConstraintBlockTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/BilateralResponseMapTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/BilateralResponseProjectionTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/BilateralScratchTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/BinaryCircuitTests.cs` | CAT-005, CAT-013, CAT-024, CAT-025, CAT-026, CAT-027 | |
| `CuriousContraptions.tests/BodyBoundsTreeTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/BodyDiagnosticTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/BodyDragTests.cs` | CAT-001 | |
| `CuriousContraptions.tests/BodyDynamicsTests.cs` | — | N-SOLVER. Tests `engine/BodyDynamics.cs`, harvested by CAT-067. |
| `CuriousContraptions.tests/BodyLocalBoxTests.cs` | CAT-007, CAT-034, CAT-051, CAT-070 | |
| `CuriousContraptions.tests/BodyPoseReadTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/BodySlotTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/BodyTrajectoryTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/BooleanAnimationSignalTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/BooleanColourBindingTests.cs` | CAT-043, EL-138, EL-139, EL-140, EL-141, EL-142 | |
| `CuriousContraptions.tests/BooleanObservationTests.cs` | CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, EL-138, EL-139, EL-140, EL-141, EL-142 | |
| `CuriousContraptions.tests/BothGateTests.cs` | CAT-013 | |
| `CuriousContraptions.tests/BumperOccurrenceTests.cs` | CAT-015, EL-195 | |
| `CuriousContraptions.tests/BumperTests.cs` | CAT-015, EL-124, EL-195 | |
| `CuriousContraptions.tests/CampaignProgressTests.cs` | — | N-PRODUCT |
| `CuriousContraptions.tests/CannonChamberGeometryTests.cs` | CAT-016 | |
| `CuriousContraptions.tests/CannonDiagnosticTests.cs` | CAT-016 | |
| `CuriousContraptions.tests/CannonEnergyOwnershipTests.cs` | CAT-016 | |
| `CuriousContraptions.tests/CannonFeederTests.cs` | CAT-016 | |
| `CuriousContraptions.tests/CannonRuntimeCheckpointTests.cs` | CAT-016 | |
| `CuriousContraptions.tests/CannonTests.cs` | CAT-016, CAT-051, EL-091, EL-095 | |
| `CuriousContraptions.tests/CannonWorkloadTests.cs` | CAT-016 | |
| `CuriousContraptions.tests/ClockPendulumTests.cs` | CAT-017 | |
| `CuriousContraptions.tests/ClockPulseFeedbackTests.cs` | CAT-017 | |
| `CuriousContraptions.tests/ClockTests.cs` | CAT-017 | |
| `CuriousContraptions.tests/ClutchTests.cs` | CAT-018 | |
| `CuriousContraptions.tests/ColourOpticsTests.cs` | CAT-011, CAT-012, CAT-031, CAT-032, CAT-055, CAT-056, EL-143, EL-144, EL-145, EL-146, EL-147, EL-148, EL-174, EL-176, EL-177, EL-178 | |
| `CuriousContraptions.tests/CommandAcknowledgementTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedActivityTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedBooleanTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedCompliantContactTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedCounterTests.cs` | CAT-020 | |
| `CuriousContraptions.tests/CommittedDisplayClockTests.cs` | — | N-PRESENT. Tests `engine/bridge/CommittedDisplayClock.cs`, harvested by CAT-017, EL-081. |
| `CuriousContraptions.tests/CommittedElectricalTests.cs` | CAT-005 | |
| `CuriousContraptions.tests/CommittedEnumTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedEventStreamTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedFeedbackAdapterTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedLightConeTests.cs` | CAT-029 | |
| `CuriousContraptions.tests/CommittedPoseBufferTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedPoseWorldTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedPublicationMemoryTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedScalarTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedTimerTests.cs` | CAT-033 | |
| `CuriousContraptions.tests/CommittedTraceTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CommittedVelocityTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/CompliantContactStateTests.cs` | EL-215 | |
| `CuriousContraptions.tests/CompliantContactTests.cs` | EL-215 | |
| `CuriousContraptions.tests/CompliantSurfaceBindingTests.cs` | CAT-065 | |
| `CuriousContraptions.tests/CompositePowerPortTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/CompoundBoundsAllocationTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/CompoundCollisionTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/CompoundOverlapTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/CompoundWorldSweepTests.cs` | CAT-016 | |
| `CuriousContraptions.tests/CompressionTransferSupplyTests.cs` | CAT-010 | Facts 19–22 (Batch D). |
| `CuriousContraptions.tests/ConnectionChoiceTests.cs` | EL-197 | |
| `CuriousContraptions.tests/ConnectionPortTests.cs` | CAT-035, CAT-058, CAT-063, EL-038, EL-197 | |
| `CuriousContraptions.tests/ConstrainedPoweredImpulseTests.cs` | EL-054, EL-055 | |
| `CuriousContraptions.tests/ConstraintMassRangeTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ConstraintMassScratchTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ConstraintTrajectorySpeedTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ConstructionDiagnosticTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/ConstructionLifecycleTests.cs` | CAT-048, CAT-066 | |
| `CuriousContraptions.tests/ConstructionResetTests.cs` | CAT-016 | |
| `CuriousContraptions.tests/ConstructionSnapshotTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/ContactConstraintTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ContactForceTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ContactFrictionBudgetTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ContactGapTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/ContactMaterialTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ContactParticipationTests.cs` | CAT-009, CAT-065 | |
| `CuriousContraptions.tests/ContactPatchTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/ContactSlipSweepTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/ConvergingGasNozzleTests.cs` | CAT-010, EL-039, EL-042, EL-092, EL-100, TH-17, TH-20, TH-35 | |
| `CuriousContraptions.tests/ConvexDistanceTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/ConvexPenetrationTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/ConvexPoseTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/ConvexRoundedTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/ConvexSweepTests.cs` | — | N-GEOM. Tests `engine/physics/ConvexSweep.cs`, harvested by CAT-052. |
| `CuriousContraptions.tests/ConveyorRuntimeTests.cs` | CAT-019, CAT-042, CAT-057 | |
| `CuriousContraptions.tests/ConveyorTests.cs` | CAT-019, EL-202 | |
| `CuriousContraptions.tests/CoreSupportTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/CounterAnimationTests.cs` | CAT-020 | |
| `CuriousContraptions.tests/CounterDiagnosticTests.cs` | CAT-020 | |
| `CuriousContraptions.tests/CounterTests.cs` | CAT-002, CAT-020, CAT-035, CAT-051, EL-183 | |
| `CuriousContraptions.tests/CoupledFrictionContactTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/CoupledImpulseTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/CylindricalRegionTests.cs` | — | N-GEOM. Tests `engine/physics/CylindricalRegion.cs`, harvested by CAT-016. |
| `CuriousContraptions.tests/DelayPresentationTests.cs` | CAT-022 | |
| `CuriousContraptions.tests/DelayTests.cs` | CAT-022, CAT-035 | |
| `CuriousContraptions.tests/DisplayClockIntegrationTests.cs` | CAT-029 | |
| `CuriousContraptions.tests/DrivenContactTests.cs` | CAT-019 | |
| `CuriousContraptions.tests/DrivenSurfaceWorldTests.cs` | CAT-019 | |
| `CuriousContraptions.tests/ElasticWorldTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ElectricalAnimationTests.cs` | CAT-013, CAT-024, CAT-025, CAT-026, CAT-027 | |
| `CuriousContraptions.tests/ElectricalContactBindingTests.cs` | CAT-012, CAT-020, CAT-021, CAT-032, CAT-033, CAT-037, CAT-038, CAT-040, CAT-056, CAT-060, CAT-068, CAT-072, EL-146, EL-147, EL-148, EL-149, EL-150, EL-151, EL-152, EL-214 | |
| `CuriousContraptions.tests/ElectricalLogicTests.cs` | CAT-013, CAT-024, CAT-025, CAT-026, CAT-027, EL-133, EL-134, EL-135, EL-136, EL-137 | |
| `CuriousContraptions.tests/ElectricalPlanTests.cs` | CAT-005 | |
| `CuriousContraptions.tests/ElectricalSourceTests.cs` | CAT-005, CAT-059 | |
| `CuriousContraptions.tests/ElectricalTests.cs` | CAT-005, CAT-042, CAT-063, EL-197, EL-198 | |
| `CuriousContraptions.tests/EnergyObservationTests.cs` | CAT-016 | |
| `CuriousContraptions.tests/EnumContractTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/EnumObservationTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/EnumPresentationTests.cs` | CAT-016 | |
| `CuriousContraptions.tests/ExplicitPoseReferenceTests.cs` | CAT-071 | |
| `CuriousContraptions.tests/FixtureParts.cs` | — | N-FIXTURE |
| `CuriousContraptions.tests/FlashlightContactTests.cs` | CAT-029, EL-210 | |
| `CuriousContraptions.tests/FlashlightPresentationTests.cs` | CAT-029 | |
| `CuriousContraptions.tests/FlightCampaignTests.cs` | CAT-004, CAT-048, CAT-050, CAT-062 | |
| `CuriousContraptions.tests/FloorTests.cs` | CAT-001, CAT-014, CAT-064, EL-076, EL-077, EL-083, EL-123, EL-187, EL-188, EL-189 | |
| `CuriousContraptions.tests/ForcedTrajectoryTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/FrictionBoundaryConvergenceTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/FrustumBoxIntersectionTests.cs` | CAT-030 | |
| `CuriousContraptions.tests/FunnelTests.cs` | CAT-030 | |
| `CuriousContraptions.tests/GasChamberWorldTests.cs` | CAT-003, EL-039, EL-041, TH-19, TH-24 | |
| `CuriousContraptions.tests/GasNozzleTransitTests.cs` | CAT-010, EL-039, EL-092 | |
| `CuriousContraptions.tests/GeometryQueryScene.cs` | — | N-FIXTURE |
| `CuriousContraptions.tests/GuideSupportBoundaryTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/HingeConstructionTests.cs` | CAT-034 | |
| `CuriousContraptions.tests/HoldTimerPresentationTests.cs` | CAT-033 | |
| `CuriousContraptions.tests/HoldTimerTests.cs` | CAT-033, CAT-051 | |
| `CuriousContraptions.tests/HollowBoxTestProbe.cs` | — | N-FIXTURE |
| `CuriousContraptions.tests/HollowGeometryTests.cs` | EL-058, EL-071 | |
| `CuriousContraptions.tests/HollowSurfaceTests.cs` | EL-072 | |
| `CuriousContraptions.tests/ImpactFrameTests.cs` | CAT-015, CAT-062 | |
| `CuriousContraptions.tests/ImpactLeverFrustumObstructionTests.cs` | CAT-034 | |
| `CuriousContraptions.tests/ImpactLeverObstructionTests.cs` | CAT-034 | |
| `CuriousContraptions.tests/ImpactLeverSphereObstructionTests.cs` | CAT-034 | |
| `CuriousContraptions.tests/ImpactLeverTests.cs` | CAT-034 | |
| `CuriousContraptions.tests/ImpactLeverTubeObstructionTests.cs` | CAT-034 | |
| `CuriousContraptions.tests/ImpactRuntimeCheckpointTests.cs` | CAT-002, CAT-034 | |
| `CuriousContraptions.tests/ImpulseReactionLinearizationTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ImpulseSolverTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ImpulseTransformAdapterTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/InertiaTensorTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/InitialEnergyStoreTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/InitialParticipationTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/JetReceiverFieldTests.cs` | CAT-028 | |
| `CuriousContraptions.tests/JetTransferForcePathTests.cs` | CAT-028 | |
| `CuriousContraptions.tests/JetTransferImpedanceTests.cs` | CAT-028 | |
| `CuriousContraptions.tests/JointBoundaryTests.cs` | CAT-034, EL-056 | |
| `CuriousContraptions.tests/JointConstraintTests.cs` | CAT-034, EL-113, EL-114, EL-115, EL-117 | |
| `CuriousContraptions.tests/JointDiagnosticTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/JointRangeTests.cs` | CAT-034, EL-112, EL-114, EL-117, EL-161 | |
| `CuriousContraptions.tests/LaserAnimationTests.cs` | CAT-036 | |
| `CuriousContraptions.tests/LatchAnimationTests.cs` | CAT-037 | |
| `CuriousContraptions.tests/LatchDiagnosticTests.cs` | CAT-037 | |
| `CuriousContraptions.tests/LatchTests.cs` | CAT-037 | |
| `CuriousContraptions.tests/LeverFixture.cs` | CAT-034 | |
| `CuriousContraptions.tests/LightConeBindingTests.cs` | CAT-029, EL-210 | |
| `CuriousContraptions.tests/LightTests.cs` | CAT-029, CAT-059, CAT-066, CAT-067, EL-153, EL-154, EL-210, EL-211 | |
| `CuriousContraptions.tests/LinearAirflowSupplyTests.cs` | CAT-010, CAT-028 | |
| `CuriousContraptions.tests/LinearPusherTests.cs` | CAT-039, CAT-067, TH-05 | |
| `CuriousContraptions.tests/LinearRigidTrajectoryTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/LoadConstructionTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/LogicGateTests.cs` | CAT-013, CAT-024, CAT-025, CAT-026, CAT-027, CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, EL-133, EL-138, EL-139, EL-140, EL-141, EL-142 | |
| `CuriousContraptions.tests/MachineStepLifecycleTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/MappedImpulseTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/MechanicalDiagnosticTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/MechanicalPortSpeedPathTests.cs` | — | N-TRANSFER. Tests `engine/physics/MechanicalPortSpeedPath.cs`, harvested by CAT-042. |
| `CuriousContraptions.tests/MechanicalPowerPortTests.cs` | — | N-TRANSFER. Tests `engine/physics/MechanicalPowerPort.cs`, harvested by CAT-042. |
| `CuriousContraptions.tests/MechanicalRuntimeCheckpointTests.cs` | CAT-010, CAT-071 | |
| `CuriousContraptions.tests/MechanicalSourceBindingTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/MechanicalSourceLedgerTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/MechanicalSourceRatingTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/MechanicalTests.cs` | CAT-019, CAT-042, CAT-057, EL-055, EL-110, EL-199, EL-200, EL-201, EL-202, EL-203 | |
| `CuriousContraptions.tests/MechanicalTransferBoundaryTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/MechanicalTransferFieldTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/MechanicalTransferLedgerTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/MechanicalTransferLoadTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/MechanicalTransferSaturationTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/MechanicalTransferWorkTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/MechanicalWorkTests.cs` | CAT-018, CAT-019, CAT-042, CAT-057, CAT-071 | |
| `CuriousContraptions.tests/MidpointConstraintEvaluationTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/MirrorTests.cs` | CAT-041, EL-212 | |
| `CuriousContraptions.tests/MotorAccountingOwnershipTests.cs` | CAT-039, CAT-042 | |
| `CuriousContraptions.tests/MotorIndicatorTests.cs` | CAT-042 | |
| `CuriousContraptions.tests/MotorPredictionTests.cs` | CAT-042, EL-055, EL-057, EL-068, TH-05 | |
| `CuriousContraptions.tests/MultiBodyConstraintTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/NativeSceneStartupTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/NetworkSpatialOwnershipTests.cs` | CAT-002, CAT-006, CAT-016, CAT-028, CAT-029, CAT-036, CAT-038, CAT-041, CAT-045, CAT-059, CAT-060, CAT-061, CAT-069, CAT-070, EL-214 | |
| `CuriousContraptions.tests/NonlinearIterationTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/NormalizationLinearizationTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/NozzleCoupledJetReceiverTests.cs` | CAT-028 | |
| `CuriousContraptions.tests/ObjectiveOwnershipTests.cs` | EL-120 | |
| `CuriousContraptions.tests/OccurrenceBindingTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/OpticalControlCheckpointTests.cs` | CAT-036, CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, EL-138, EL-139, EL-140, EL-141, EL-142 | |
| `CuriousContraptions.tests/OpticalLogicTests.cs` | CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, EL-138, EL-139, EL-140, EL-141, EL-142 | |
| `CuriousContraptions.tests/OpticalObservationTests.cs` | CAT-006, CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, EL-138, EL-139, EL-140, EL-141, EL-142, EL-213 | |
| `CuriousContraptions.tests/OpticalPortsTests.cs` | CAT-006, CAT-008, CAT-011, CAT-012, CAT-021, CAT-031, CAT-032, CAT-036, CAT-038, CAT-040, CAT-041, CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, CAT-055, CAT-056, CAT-068, CAT-072 | |
| `CuriousContraptions.tests/OpticalPreviewBindingTests.cs` | CAT-041 | |
| `CuriousContraptions.tests/OpticalPreviewPresentationTests.cs` | CAT-006, CAT-008, CAT-011, CAT-031, CAT-041, CAT-055, EL-212 | |
| `CuriousContraptions.tests/OpticalRuntimeCheckpointTests.cs` | CAT-006, CAT-012, CAT-021, CAT-032, CAT-038, CAT-040, CAT-056, CAT-059, CAT-068, CAT-072, EL-146, EL-147, EL-148, EL-149, EL-150, EL-151, EL-152 | |
| `CuriousContraptions.tests/OpticalTests.cs` | CAT-012, CAT-021, CAT-032, CAT-036, CAT-038, CAT-040, CAT-056, CAT-068, CAT-072, EL-176, EL-177, EL-178, EL-214 | |
| `CuriousContraptions.tests/OscillationAdapterTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/OscillatorObservationTests.cs` | CAT-017 | |
| `CuriousContraptions.tests/ParameterValidationOwnershipTests.cs` | CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, EL-138, EL-139, EL-140, EL-141, EL-142 | |
| `CuriousContraptions.tests/PartOrientationTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/PartParameterNameTests.cs` | CAT-018 | |
| `CuriousContraptions.tests/PartRuntimeCheckpointTests.cs` | CAT-018, CAT-057, CAT-071 | |
| `CuriousContraptions.tests/PerformanceAuditTests.cs` | — | N-PERF |
| `CuriousContraptions.tests/PerformanceRecorderTests.cs` | — | N-PERF |
| `CuriousContraptions.tests/PersistentContactTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/PhysicsBackendQualificationTests.cs` | — | N-PERF |
| `CuriousContraptions.tests/PhysicsBodyOwnershipTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/PhysicsBodyTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/PhysicsCalibrationTests.cs` | CAT-001, CAT-014, CAT-064, EL-076, EL-077, EL-091, EL-098, EL-124, EL-125, EL-187, EL-188, EL-189 | |
| `CuriousContraptions.tests/PhysicsColliderUpdateTests.cs` | EL-112 | |
| `CuriousContraptions.tests/PhysicsContactLoadSensorTests.cs` | CAT-052 | |
| `CuriousContraptions.tests/PhysicsEnergyStoreTests.cs` | — | N-SOLVER. Tests `engine/physics/PhysicsEnergyStore.cs`, harvested by CAT-016, EL-001, EL-022, EL-078, EL-092, EL-093, EL-095, EL-097, EL-104, EL-105, EL-106. |
| `CuriousContraptions.tests/PhysicsGasNodeTests.cs` | CAT-010 | |
| `CuriousContraptions.tests/PhysicsImpactEffectTests.cs` | CAT-015, EL-084, EL-088, EL-101, EL-106 | |
| `CuriousContraptions.tests/PhysicsImpactJointTests.cs` | EL-159, EL-185, EL-186, GAP-09 | |
| `CuriousContraptions.tests/PhysicsJointTests.cs` | EL-056, EL-113, EL-114, EL-115 | |
| `CuriousContraptions.tests/PhysicsJointUpdateTests.cs` | EL-115, EL-117, EL-159 | |
| `CuriousContraptions.tests/PhysicsLatchedSpringTests.cs` | CAT-062, CAT-071 | |
| `CuriousContraptions.tests/PhysicsLoadSetTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/PhysicsMotionHistoryTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/PhysicsMotorTests.cs` | CAT-042 | |
| `CuriousContraptions.tests/PhysicsMutationVisibilityTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/PhysicsPassageSensorTests.cs` | CAT-002, EL-109, EL-164 | |
| `CuriousContraptions.tests/PhysicsResidenceSensorTests.cs` | CAT-004 | |
| `CuriousContraptions.tests/PhysicsServoTests.cs` | EL-163 | |
| `CuriousContraptions.tests/PhysicsSpatialWorkTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/PhysicsTiltSensorTests.cs` | CAT-023 | |
| `CuriousContraptions.tests/PhysicsWorldTests.cs` | — | N-SOLVER. Tests `engine/physics/PhysicsWorld.cs`, harvested by CAT-016, CAT-019, CAT-052, CAT-065, CAT-071, EL-039, EL-093, EL-106. |
| `CuriousContraptions.tests/PhysicsWrenchTests.cs` | EL-028, EL-029, EL-125, EL-169, EL-170 | |
| `CuriousContraptions.tests/PipeBendTests.cs` | CAT-049, CAT-050, EL-009, EL-010, EL-072 | |
| `CuriousContraptions.tests/PipeResizeTests.cs` | CAT-048 | |
| `CuriousContraptions.tests/PipeTests.cs` | CAT-001, CAT-048, EL-071, EL-076, EL-082, EL-083, EL-087, EL-088, EL-089, EL-098, EL-099, EL-102 | |
| `CuriousContraptions.tests/PivotContactReproductionTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/PlanarGuideTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/PlaytestAuditTests.cs` | — | N-TOOL |
| `CuriousContraptions.tests/PoseSamplingTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/PositionProjectorTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/PoweredGateTests.cs` | CAT-051, EL-162, EL-163, EL-184, EL-216 | |
| `CuriousContraptions.tests/PredictionSamplesTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/PreparedConfigurationTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/PrescribedBodyTests.cs` | EL-156, EL-157, EL-158 | |
| `CuriousContraptions.tests/PrescribedCollisionTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/PressureContactOwnershipTests.cs` | CAT-052 | |
| `CuriousContraptions.tests/PressurePlateTests.cs` | CAT-051, CAT-052, CAT-064, EL-076, EL-077 | |
| `CuriousContraptions.tests/PublicationAllocationStressTests.cs` | — | N-PERF |
| `CuriousContraptions.tests/PulleyRopeRouteTests.cs` | CAT-053 | |
| `CuriousContraptions.tests/PulseEnvelopeAdapterTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/PusherServoOwnershipTests.cs` | CAT-039 | |
| `CuriousContraptions.tests/QueryAssertions.cs` | — | N-FIXTURE |
| `CuriousContraptions.tests/QuinticTrajectoryTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ReactionRayLimitTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ReceiverAnimationTests.cs` | CAT-012, CAT-021, CAT-032, CAT-038, CAT-040, CAT-056, CAT-068, CAT-072, EL-146, EL-147, EL-148, EL-149, EL-150, EL-151, EL-152, EL-176, EL-177, EL-178, EL-214 | |
| `CuriousContraptions.tests/RemainingReadingCheckpointTests.cs` | CAT-028, CAT-039, CAT-053 | |
| `CuriousContraptions.tests/RenderCadenceTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/RequiredPhysicsParameterTests.cs` | CAT-003, CAT-005, CAT-014, CAT-054, CAT-064, CAT-066, EL-076, EL-080, EL-208 | |
| `CuriousContraptions.tests/RgbAnimationTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/RigidPoseTrajectoryTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/RopeOwnershipTests.cs` | CAT-053, CAT-067 | |
| `CuriousContraptions.tests/RopeTests.cs` | CAT-053, CAT-058, CAT-067, EL-067, EL-184, EL-204, EL-205, EL-206 | |
| `CuriousContraptions.tests/RotaryCaptureDeclarationTests.cs` | CAT-070 | |
| `CuriousContraptions.tests/RotaryCaptureForcePathTests.cs` | CAT-070 | |
| `CuriousContraptions.tests/RotaryCaptureMaterialTests.cs` | CAT-070 | |
| `CuriousContraptions.tests/RotaryCaptureMotionTests.cs` | CAT-070 | |
| `CuriousContraptions.tests/RotaryTransmissionPartTests.cs` | CAT-018, CAT-057 | |
| `CuriousContraptions.tests/RotatingBoxObstacleSweepTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/RotatingBoxSweepTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/RotatingFrustumSweepTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/RotatingTubeSweepTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/RotorAirflowSupplyTests.cs` | CAT-028, CAT-070 | |
| `CuriousContraptions.tests/RoundedSeparationTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/RoutedRopeTests.cs` | CAT-053, CAT-058, EL-205, EL-207 | |
| `CuriousContraptions.tests/RuntimeBinaryInputTests.cs` | CAT-005, CAT-042 | |
| `CuriousContraptions.tests/RuntimePhysicsCutoverTests.cs` | CAT-034 | |
| `CuriousContraptions.tests/RuntimeQueryOwnershipTests.cs` | CAT-039 | |
| `CuriousContraptions.tests/SampledPresentationTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/SampledTraceTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/ScalarObservationTests.cs` | CAT-059 | |
| `CuriousContraptions.tests/SceneAcousticRunTests.cs` | CAT-009 | |
| `CuriousContraptions.tests/SceneAnimationAdapterTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/SceneAnimationEndpointTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/SceneAnimationRunTests.cs` | CAT-028 | |
| `CuriousContraptions.tests/SceneBodyDynamicsTests.cs` | CAT-007, CAT-034, CAT-051 | |
| `CuriousContraptions.tests/SceneBodyGeometryTests.cs` | CAT-034 | |
| `CuriousContraptions.tests/SceneColourAnimationTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/SceneGeometryUpdateTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/SceneImpulseAnimationTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/SceneJointDeclarationTests.cs` | CAT-034, EL-114 | |
| `CuriousContraptions.tests/SceneJointIdentityTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/SceneMechanicalTransferTests.cs` | CAT-070 | |
| `CuriousContraptions.tests/ScenePlungerBindingTests.cs` | CAT-071, EL-056, EL-094 | |
| `CuriousContraptions.tests/ScenePoseBatchTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/SceneRopeBindingTests.cs` | CAT-053, CAT-058, CAT-067, EL-064, EL-205 | |
| `CuriousContraptions.tests/SealedGasStateTests.cs` | CAT-003, EL-039, TH-17, TH-24 | |
| `CuriousContraptions.tests/SegmentOcclusionSweepTests.cs` | — | N-GEOM. Tests `engine/physics/SegmentOcclusionSweep.cs`, harvested by EL-082, EL-083, EL-087, EL-088, EL-105. |
| `CuriousContraptions.tests/SharedContactMigrationTests.cs` | EL-056 | |
| `CuriousContraptions.tests/SharedHingeMechanicsTests.cs` | EL-114, EL-160, EL-161 | |
| `CuriousContraptions.tests/SharedImpactRequirementsTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/SharedSphereSweepTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/SignedSweepTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/SimulationCommandInboxTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/SimulationCountersTests.cs` | CAT-020 | |
| `CuriousContraptions.tests/SimulationLatchesTests.cs` | CAT-037 | |
| `CuriousContraptions.tests/SimulationOscillatorsTests.cs` | CAT-017 | |
| `CuriousContraptions.tests/SimulationTimersTests.cs` | CAT-033 | |
| `CuriousContraptions.tests/SimulationTransactionTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/SlidingBladeDynamicsTests.cs` | CAT-007, CAT-051 | |
| `CuriousContraptions.tests/SlidingBladeOwnershipTests.cs` | CAT-007, CAT-051 | |
| `CuriousContraptions.tests/SocketIdentityTests.cs` | — | N-INFRA |
| `CuriousContraptions.tests/SolarAnimationTests.cs` | CAT-059 | |
| `CuriousContraptions.tests/SoundMeterAnimationTests.cs` | CAT-060 | |
| `CuriousContraptions.tests/SoundMeterCheckpointTests.cs` | CAT-060 | |
| `CuriousContraptions.tests/SoundMeterTests.cs` | CAT-060, CAT-061, EL-043, EL-044, EL-050, EL-180 | |
| `CuriousContraptions.tests/SourceRotorTests.cs` | CAT-042, CAT-070, EL-199 | |
| `CuriousContraptions.tests/SpatialCandidateTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/SpectralBindingTests.cs` | CAT-006 | |
| `CuriousContraptions.tests/SpringAnimationTests.cs` | CAT-062 | |
| `CuriousContraptions.tests/SpringConstraintObservationTests.cs` | CAT-062 | |
| `CuriousContraptions.tests/SpringboardLessonTests.cs` | CAT-062 | |
| `CuriousContraptions.tests/StoredFlowBoundaryTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/StoredFlowRoundingTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/StoredFlowSourceTests.cs` | EL-011, EL-022 | |
| `CuriousContraptions.tests/StoredFlowWorldTests.cs` | CAT-028 | |
| `CuriousContraptions.tests/SupplyControl.cs` | CAT-005, CAT-037 | |
| `CuriousContraptions.tests/SupportFootprintTests.cs` | — | N-GEOM. Tests `engine/physics/SupportFootprint.cs`, harvested by CAT-065. |
| `CuriousContraptions.tests/SurfaceMotorBudgetTests.cs` | CAT-019 | |
| `CuriousContraptions.tests/TangentQuadraticTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/TimerDiagnosticTests.cs` | CAT-017, CAT-033 | |
| `CuriousContraptions.tests/TimerObservationTests.cs` | CAT-033 | |
| `CuriousContraptions.tests/TrampolineCampaignTests.cs` | CAT-065 | |
| `CuriousContraptions.tests/TrampolineOwnershipTests.cs` | CAT-065 | |
| `CuriousContraptions.tests/TrampolinePipeTests.cs` | CAT-048, CAT-065 | |
| `CuriousContraptions.tests/TrampolineRopeTests.cs` | CAT-058, CAT-065, CAT-067, EL-205 | |
| `CuriousContraptions.tests/TrampolineRuntimeCheckpointTests.cs` | CAT-065 | |
| `CuriousContraptions.tests/TrampolineSkinTests.cs` | CAT-065 | |
| `CuriousContraptions.tests/TrampolineStackTests.cs` | CAT-065 | |
| `CuriousContraptions.tests/TrampolineTests.cs` | CAT-065, CAT-067 | |
| `CuriousContraptions.tests/TransferBodyImpulseTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/TransferRatingRoundingTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/TransferSourceStallTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/TransferStepErrorTests.cs` | — | N-TRANSFER |
| `CuriousContraptions.tests/TranslationAnimationTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/TransmissionJointTests.cs` | CAT-018, CAT-057 | |
| `CuriousContraptions.tests/TubeBoxIntersectionTests.cs` | CAT-048 | |
| `CuriousContraptions.tests/TubePlacementSnapTests.cs` | CAT-048, EL-008, EL-009, EL-010, EL-012, EL-072 | |
| `CuriousContraptions.tests/WallLessonTests.cs` | CAT-066 | |
| `CuriousContraptions.tests/WallTests.cs` | CAT-066 | |
| `CuriousContraptions.tests/WindChimeTests.cs` | CAT-028, CAT-030, CAT-048, CAT-049, CAT-050, CAT-060, CAT-069, EL-125 | |
| `CuriousContraptions.tests/WindmillTests.cs` | CAT-057, CAT-070 | |
| `CuriousContraptions.tests/WorkshopAnimationTests.cs` | — | N-PRESENT |
| `CuriousContraptions.tests/WorkshopInteractionTests.cs` | CAT-048, CAT-066 | |
| `CuriousContraptions.tests/WorkshopLifetimeTests.cs` | CAT-054 | |
| `CuriousContraptions.tests/WorkshopPipeTests.cs` | CAT-048 | |
| `CuriousContraptions.tests/WorldAngularTravelTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/WorldFlightTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/WorldPlacementTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/WorldPoweredImpulseTests.cs` | — | N-SOLVER |
| `CuriousContraptions.tests/WorldSweepShapeTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/WorldSweepTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/WorldTraceTests.cs` | — | N-GEOM |
| `CuriousContraptions.tests/WoundSpringBrowserWorkloadTests.cs` | CAT-071 | |
| `CuriousContraptions.tests/WoundSpringCampaignTests.cs` | CAT-071 | |
| `CuriousContraptions.tests/WoundSpringDiagnosticTests.cs` | CAT-071 | |
| `CuriousContraptions.tests/WoundSpringOwnershipTests.cs` | CAT-071 | |
| `CuriousContraptions.tests/WoundSpringRuntimeTests.cs` | CAT-071, EL-053, EL-056, EL-057, EL-063 | |
| `CuriousContraptions.tests/WoundSpringTests.cs` | CAT-051, CAT-067, CAT-071 | |
| `CuriousContraptions.tests/WoundSpringTests.cs.orig` | CAT-067, CAT-071 | Stray merge backup (inventory: junk). |
| `CuriousContraptions.tests/WrenchPathWorkTests.cs` | — | N-TRANSFER |

## 7.4 LEGACY-0d: `engine/physics/`

| File | Harvested into | Notes |
| --- | --- | --- |
| `engine/physics/AccelerationDrive.cs` (+ `.uid`) | CAT-042 | |
| `engine/physics/AccelerationReactionSystem.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/AccelerationSolver.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/AdiabaticGasDischarge.cs` (+ `.uid`) | CAT-010, EL-003, EL-039, TH-17 | |
| `engine/physics/AdmissibleImpulseResponse.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/AirJetBoundaryPath.cs` (+ `.uid`) | CAT-028 | |
| `engine/physics/AirJetGeometry.cs` (+ `.uid`) | CAT-028 | |
| `engine/physics/AirJetStreamlinePath.cs` (+ `.uid`) | CAT-028 | |
| `engine/physics/AlignedPowerPort.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/AngularPathMeasure.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/AxialDampingLoad.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/AxialEffortLoad.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/AxialElasticLoad.cs` (+ `.uid`) | CAT-071 | |
| `engine/physics/AxialElasticPotential.cs` (+ `.uid`) | CAT-062, CAT-071 | |
| `engine/physics/AxialGasGeometry.cs` (+ `.uid`) | CAT-010, EL-039, EL-168, TH-19, TH-24 | |
| `engine/physics/AxialGasLoad.cs` (+ `.uid`) | CAT-010, EL-039, TH-19, TH-24 | |
| `engine/physics/AxialGasPotential.cs` (+ `.uid`) | CAT-010, EL-039, TH-19 | |
| `engine/physics/BilateralConstraintBlock.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/BilateralResponseMap.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/BinaryProductSum.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/BodyBoundsTree.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/BodyColliderGeometry.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/BodyDragLoad.cs` (+ `.uid`) | CAT-003, EL-028, EL-029 | |
| `engine/physics/BodyQueryGeometry.cs` (+ `.uid`) | CAT-006, CAT-008, CAT-009, CAT-011, CAT-012, CAT-021, CAT-028, CAT-029, CAT-031, CAT-032, CAT-036, CAT-038, CAT-040, CAT-041, CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, CAT-055, CAT-056, CAT-059, CAT-060, CAT-068, CAT-072, EL-044, EL-050 | |
| `engine/physics/BodyTrajectory.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/BodyWrench.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/CompliantContactLoad.cs` (+ `.uid`) | CAT-052, CAT-065, EL-215 | |
| `engine/physics/CompliantContactState.cs` (+ `.uid`) | CAT-065, EL-215 | |
| `engine/physics/CompositePowerPort.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/CompoundBoundsTree.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/CompoundCollision.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/CompressionSpringPotential.cs` (+ `.uid`) | CAT-065 | |
| `engine/physics/ConfigurationTrajectory.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/ConstantAxialEffortLoad.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/ConstrainedPoweredImpulse.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/ConstraintAcceleration.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/ConstraintGradient.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/ConstraintMassMatrix.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/ContactConstraint.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/ContactForce.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/ContactGap.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/ContactKinematics.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/ContactManifold.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/ContactPatch.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/ContactSlipPath.cs` (+ `.uid`) | CAT-019 | |
| `engine/physics/ContactSlipSweep.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/ConvergingGasNozzle.cs` (+ `.uid`) | CAT-010, EL-013, EL-019, EL-039, EL-042, TH-17, TH-20, TH-35 | |
| `engine/physics/ConvexDistance.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/ConvexGeometry.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/ConvexPenetration.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/ConvexPose.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/ConvexRounded.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/ConvexSeparation.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/ConvexSweep.cs` (+ `.uid`) | CAT-052 | |
| `engine/physics/CoupledImpulsePair.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/CylindricalRegion.cs` (+ `.uid`) | CAT-016 | |
| `engine/physics/DrivenSurface.cs` (+ `.uid`) | CAT-019, EL-202 | |
| `engine/physics/ForcePrediction.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/GasNozzleTransit.cs` (+ `.uid`) | CAT-010, EL-039, EL-092, TH-17, TH-20 | |
| `engine/physics/GasPredictionWork.cs` (+ `.uid`) | CAT-010, EL-039, TH-19 | |
| `engine/physics/HollowGeometry.cs` (+ `.uid`) | CAT-030, CAT-048, CAT-049, CAT-050 | |
| `engine/physics/IRigidTrajectory.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/IdealGasMaterial.cs` (+ `.uid`) | CAT-003, EL-039, TH-11, TH-13, TH-17, TH-18, TH-24, TH-35 | |
| `engine/physics/ImpulseConstraint.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/ImpulseCorrectionWork.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/ImpulseSweepAcceleration.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/JetTransferForcePath.cs` (+ `.uid`) | CAT-028 | |
| `engine/physics/JetTransferImpedance.cs` (+ `.uid`) | CAT-028, EL-103 | |
| `engine/physics/JointConstraints.cs` (+ `.uid`) | CAT-034, CAT-058 | |
| `engine/physics/JointEquations.cs` (+ `.uid`) | CAT-018, CAT-034, CAT-057, CAT-058, EL-111, EL-112, EL-161 | |
| `engine/physics/LinearRigidTrajectory.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/MaterialContact.cs` (+ `.uid`) | CAT-019 | |
| `engine/physics/MechanicalPortSpeedPath.cs` (+ `.uid`) | CAT-042 | |
| `engine/physics/MechanicalPowerPort.cs` (+ `.uid`) | CAT-042 | |
| `engine/physics/MechanicalSourceRating.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/MechanicalTransferBoundaryPath.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/MechanicalTransferImpulse.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/MechanicalTransferLedger.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/MechanicalTransferLoad.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/MechanicalTransferSource.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/MotorPredictionSupply.cs` (+ `.uid`) | CAT-042 | |
| `engine/physics/NewtonDirection.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/NonlinearIteration.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/NormalizationLinearization.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/NozzleCoupledJetReceiver.cs` (+ `.uid`) | CAT-028, EL-036 | |
| `engine/physics/PersistentContactPair.cs` (+ `.uid`) | CAT-003, CAT-010, CAT-016, CAT-019, CAT-034, CAT-039, CAT-051, CAT-052, CAT-064, CAT-065, CAT-069, EL-194 | |
| `engine/physics/PhysicsBody.cs` (+ `.uid`) | — | N-INFRA |
| `engine/physics/PhysicsColliderUpdate.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/PhysicsContactLoadSensor.cs` (+ `.uid`) | CAT-052 | |
| `engine/physics/PhysicsEnergyStore.cs` (+ `.uid`) | CAT-016, EL-001, EL-022, EL-078, EL-092, EL-093, EL-095, EL-097, EL-104, EL-105, EL-106 | |
| `engine/physics/PhysicsGasNode.cs` (+ `.uid`) | CAT-003, EL-039 | |
| `engine/physics/PhysicsImpactEffects.cs` (+ `.uid`) | EL-084, EL-088, EL-101, EL-106 | |
| `engine/physics/PhysicsJoint.cs` (+ `.uid`) | CAT-034, EL-056, EL-057, EL-061, EL-070, EL-112, EL-114, EL-117 | |
| `engine/physics/PhysicsJointChange.cs` (+ `.uid`) | CAT-034, EL-115, EL-159, EL-160, EL-185, EL-186 | |
| `engine/physics/PhysicsLatchedSpring.cs` (+ `.uid`) | CAT-037, CAT-062, CAT-071, EL-185 | |
| `engine/physics/PhysicsLoadSet.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/PhysicsMotionHistory.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/PhysicsMotorCommand.cs` (+ `.uid`) | CAT-042, EL-199 | |
| `engine/physics/PhysicsPassageSensor.cs` (+ `.uid`) | CAT-002, EL-099 | |
| `engine/physics/PhysicsResidenceSensor.cs` (+ `.uid`) | CAT-004 | Not cited directly; behaviour harvested through `PhysicsResidenceSensorTests.cs`. |
| `engine/physics/PhysicsRopeJoint.cs` (+ `.uid`) | CAT-053, CAT-058, EL-062, EL-063, EL-067, EL-085, EL-205 | |
| `engine/physics/PhysicsServo.cs` (+ `.uid`) | CAT-039, EL-163, EL-164 | |
| `engine/physics/PhysicsSpatialWork.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/PhysicsTiltSensor.cs` (+ `.uid`) | CAT-023 | |
| `engine/physics/PhysicsTransmissionJoint.cs` (+ `.uid`) | CAT-018, CAT-057, EL-053, EL-054, EL-055, EL-061, EL-063, EL-203 | |
| `engine/physics/PhysicsWorld.cs` (+ `.uid`) | CAT-016, CAT-019, CAT-052, CAT-065, CAT-071, EL-039, EL-093, EL-106 | |
| `engine/physics/PhysicsWrenchCommand.cs` (+ `.uid`) | — | N-INFRA |
| `engine/physics/PlanarGuideLoad.cs` (+ `.uid`) | CAT-004, EL-192 | Not cited directly; behaviour harvested through `BasketGuideTests.cs`. |
| `engine/physics/PointTrace.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/PositionConstraints.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/PositionEquation.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/PositionProjector.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/PositiveDemandBudget.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/PoweredImpulse.cs` (+ `.uid`) | EL-079, EL-097, EL-103 | |
| `engine/physics/PredictionSamples.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/PrescribedBodyMotion.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/QuinticRigidTrajectory.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/ReactionRayLimit.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/RigidPoseTrajectory.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/RotaryCaptureDeclaration.cs` (+ `.uid`) | CAT-070 | |
| `engine/physics/RotaryCaptureForcePath.cs` (+ `.uid`) | CAT-070 | |
| `engine/physics/RotaryCaptureMaterial.cs` (+ `.uid`) | CAT-070 | |
| `engine/physics/SealedGasState.cs` (+ `.uid`) | CAT-003, EL-039, EL-168, TH-17, TH-19, TH-24, TH-25, TH-35 | |
| `engine/physics/SegmentOcclusionSweep.cs` (+ `.uid`) | EL-082, EL-083, EL-087, EL-088, EL-105 | |
| `engine/physics/StoredFlowSource.cs` (+ `.uid`) | CAT-028, EL-011, EL-022 | |
| `engine/physics/SupportFeature.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/SupportFootprint.cs` (+ `.uid`) | CAT-065 | |
| `engine/physics/TangentQuadratic.cs` (+ `.uid`) | — | N-SOLVER |
| `engine/physics/TransferResidualTotals.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/TransferSpeedPath.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/TransferStepError.cs` (+ `.uid`) | — | N-TRANSFER |
| `engine/physics/WorldQueryContracts.cs` (+ `.uid`) | EL-044 | |
| `engine/physics/WorldSweepSnapshot.cs` (+ `.uid`) | — | N-GEOM |
| `engine/physics/WrenchPathWork.cs` (+ `.uid`) | — | N-TRANSFER |

| `engine/physics/PreciseScalar.cs.uid` | — | N-INFRA: orphan Godot identity sidecar; the corresponding source is absent at a6c914e, so it contains no element behaviour. |

## 7.4 LEGACY-0d: `engine/bridge/`

| File | Harvested into | Notes |
| --- | --- | --- |
| `engine/bridge/BinaryInputCommand.cs` (+ `.uid`) | — | N-INFRA |
| `engine/bridge/BodyPoseRead.cs` (+ `.uid`) | — | N-INFRA |
| `engine/bridge/BodyQueryRead.cs` (+ `.uid`) | — | N-INFRA |
| `engine/bridge/BooleanRead.cs` (+ `.uid`) | — | N-INFRA |
| `engine/bridge/CommittedDisplayClock.cs` (+ `.uid`) | CAT-017, EL-081 | |
| `engine/bridge/CommittedEventStream.cs` (+ `.uid`) | — | N-INFRA |
| `engine/bridge/CommittedPoseBuffer.Compliant.cs` (+ `.uid`) | CAT-065 | |
| `engine/bridge/CommittedPoseBuffer.Enums.cs` (+ `.uid`) | — | N-INFRA |
| `engine/bridge/CommittedPoseBuffer.cs` (+ `.uid`) | — | N-INFRA |
| `engine/bridge/ElectricalInputRead.cs` (+ `.uid`) | CAT-005 | |
| `engine/bridge/PoseReferenceBinding.cs` (+ `.uid`) | — | N-INFRA |
| `engine/bridge/ScalarRead.cs` (+ `.uid`) | CAT-006, CAT-059 | |
| `engine/bridge/SimulationCommandInbox.cs` (+ `.uid`) | — | N-INFRA |

## 7.4 LEGACY-0d: dead `engine/*.cs`

The 15 compiled `engine/*.cs` files listed in `CuriousContraptions.csproj` are excluded.

| File | Harvested into | Notes |
| --- | --- | --- |
| `engine/AcousticAudio.cs` (+ `.uid`) | CAT-009, CAT-061, CAT-069, EL-044, EL-052 | |
| `engine/Acoustics.cs` (+ `.uid`) | CAT-009, CAT-060, CAT-061, CAT-069, EL-044, EL-046, EL-050, EL-051 | |
| `engine/AirflowNetwork.cs` (+ `.uid`) | CAT-028, CAT-070, EL-042, EL-045, EL-108, EL-209 | |
| `engine/BendProxy.cs` (+ `.uid`) | CAT-049, CAT-050 | |
| `engine/BinaryCircuit.cs` (+ `.uid`) | CAT-005, CAT-013, CAT-024, CAT-025, CAT-026, CAT-027, EL-133, EL-135, EL-181, EL-197, EL-198 | |
| `engine/BodyDynamics.cs` (+ `.uid`) | CAT-067 | |
| `engine/BodySlot.cs` (+ `.uid`) | — | N-INFRA |
| `engine/ChimeAssembly.cs` (+ `.uid`) | CAT-069 | |
| `engine/ConnectionPort.cs` (+ `.uid`) | CAT-005, CAT-018, CAT-019, CAT-042, CAT-053, CAT-057, CAT-058, EL-014, EL-038, EL-048, EL-053, EL-063, EL-067, EL-155, EL-175, EL-184, EL-197, EL-205 | |
| `engine/ElectricalNetwork.cs` (+ `.uid`) | CAT-005, CAT-013, EL-135, EL-136, EL-137 | |
| `engine/FrustumProxy.cs` (+ `.uid`) | CAT-030 | |
| `engine/LightNetwork.cs` (+ `.uid`) | CAT-029, CAT-059, EL-153, EL-154, EL-155, EL-173, EL-210, TH-06 | |
| `engine/LogicGate.cs` (+ `.uid`) | CAT-013, CAT-024, CAT-025, CAT-026, CAT-027, CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, EL-133, EL-134, EL-135, EL-136, EL-137, EL-138, EL-139, EL-140, EL-141, EL-142 | |
| `engine/MachineData.cs` (+ `.uid`) | CAT-003, CAT-005, CAT-012, CAT-017, CAT-018, CAT-019, CAT-020, CAT-021, CAT-032, CAT-033, CAT-034, CAT-037, CAT-038, CAT-039, CAT-040, CAT-042, CAT-049, CAT-050, CAT-052, CAT-053, CAT-056, CAT-057, CAT-058, CAT-060, CAT-067, CAT-068, CAT-071, CAT-072, EL-009, EL-010, EL-014, EL-022, EL-038, EL-048, EL-053, EL-054, EL-063, EL-065, EL-067, EL-072, EL-089, EL-120, EL-121, EL-124, EL-125, EL-146, EL-147, EL-148, EL-149, EL-150, EL-151, EL-152, EL-155, EL-175, EL-197, EL-204, EL-206, EL-207, EL-214 | |
| `engine/MachineEvent.cs` (+ `.uid`) | CAT-042 | |
| `engine/MachinePart.Transaction.cs` (+ `.uid`) | — | N-INFRA |
| `engine/MachineWorld.Controls.cs` (+ `.uid`) | — | N-INFRA |
| `engine/MachineWorld.Transaction.cs` (+ `.uid`) | — | N-INFRA |
| `engine/MechanicalNetwork.cs` (+ `.uid`) | CAT-018, CAT-019, CAT-042, CAT-057, EL-053, EL-054, EL-110, EL-200, EL-201 | |
| `engine/OpticalColour.cs` (+ `.uid`) | CAT-006, CAT-011, CAT-012, CAT-021, CAT-031, CAT-032, CAT-038, CAT-040, CAT-055, CAT-056, CAT-068, CAT-072, EL-119, EL-138, EL-139, EL-140, EL-141, EL-142, EL-143, EL-144, EL-145, EL-146, EL-147, EL-148, EL-149, EL-150, EL-151, EL-152, EL-174, EL-176, EL-177, EL-178, EL-214 | |
| `engine/OpticalNetwork.cs` (+ `.uid`) | CAT-006, CAT-007, CAT-008, CAT-011, CAT-012, CAT-021, CAT-031, CAT-032, CAT-036, CAT-038, CAT-040, CAT-041, CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, CAT-055, CAT-056, CAT-068, CAT-072, EL-118, EL-119, EL-138, EL-139, EL-140, EL-141, EL-142, EL-143, EL-144, EL-145, EL-146, EL-173, EL-174, EL-175, EL-212, EL-213, TH-06, TH-07 | |
| `engine/PartAssistance.cs` (+ `.uid`) | — | N-INFRA |
| `engine/PartOrientation.cs` (+ `.uid`) | — | N-INFRA |
| `engine/PartParameterName.cs` (+ `.uid`) | — | N-INFRA |
| `engine/PartParameterState.cs` (+ `.uid`) | — | N-INFRA |
| `engine/PartParameterValues.cs` (+ `.uid`) | — | N-INFRA |
| `engine/PerformanceContracts.cs` (+ `.uid`) | — | N-PERF |
| `engine/PerformanceRecorder.cs` (+ `.uid`) | — | N-PERF |
| `engine/PipeDimensionsResource.cs` (+ `.uid`) | CAT-048 | |
| `engine/RopeNetwork.cs` (+ `.uid`) | CAT-034, CAT-053, CAT-058, CAT-067, EL-062, EL-063, EL-064, EL-065, EL-067, EL-085, EL-155, EL-205, EL-206, EL-207 | |
| `engine/SceneBinaryInputDeclaration.cs` (+ `.uid`) | — | N-INFRA |
| `engine/SceneBooleanObservation.cs` (+ `.uid`) | CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, EL-138, EL-139, EL-140, EL-141, EL-142 | |
| `engine/SceneCollisionGeometry.cs` (+ `.uid`) | CAT-030, CAT-048, CAT-049, CAT-050 | |
| `engine/SceneCompliantSurface.cs` (+ `.uid`) | CAT-065, EL-215 | |
| `engine/SceneContactLoadSensorDeclaration.cs` (+ `.uid`) | CAT-052, EL-059 | |
| `engine/SceneCounterDeclaration.cs` (+ `.uid`) | CAT-020 | |
| `engine/SceneDrivenSurface.cs` (+ `.uid`) | CAT-019 | |
| `engine/SceneElectricalContact.cs` (+ `.uid`) | CAT-005, CAT-039, CAT-052, EL-198 | |
| `engine/SceneElectricalSource.cs` (+ `.uid`) | CAT-005, EL-196, EL-211 | |
| `engine/SceneEnergyStoreDeclaration.cs` (+ `.uid`) | CAT-016 | |
| `engine/SceneEnumObservation.cs` (+ `.uid`) | — | N-INFRA |
| `engine/SceneGeometryAdapter.cs` (+ `.uid`) | — | N-INFRA |
| `engine/SceneImpactEffect.cs` (+ `.uid`) | EL-065 | |
| `engine/SceneJointDeclaration.cs` (+ `.uid`) | CAT-034 | |
| `engine/SceneLatchDeclaration.cs` (+ `.uid`) | CAT-037 | |
| `engine/SceneLatchedSpringDeclaration.cs` (+ `.uid`) | CAT-037, CAT-062, CAT-071 | |
| `engine/SceneMechanicalTransferBindings.cs` (+ `.uid`) | — | N-INFRA |
| `engine/SceneOrientation.cs` (+ `.uid`) | — | N-INFRA |
| `engine/SceneOscillatorDeclaration.cs` (+ `.uid`) | CAT-017 | |
| `engine/ScenePassageSensorDeclaration.cs` (+ `.uid`) | CAT-002, EL-109, EL-164 | |
| `engine/ScenePhysicsAssembly.cs` (+ `.uid`) | — | N-INFRA |
| `engine/SceneResidenceSensorDeclaration.cs` (+ `.uid`) | CAT-004 | Not cited directly; behaviour harvested through `PhysicsResidenceSensorTests.cs`. |
| `engine/SceneRopeJoint.cs` (+ `.uid`) | CAT-053, CAT-058, EL-205 | |
| `engine/SceneRotaryShaft.cs` (+ `.uid`) | CAT-018, CAT-042, CAT-057, CAT-071, EL-053, EL-110, EL-111, EL-156, EL-157, EL-158, EL-203 | |
| `engine/SceneScalarInputDeclaration.cs` (+ `.uid`) | — | N-INFRA |
| `engine/SceneScalarObservation.cs` (+ `.uid`) | — | N-INFRA |
| `engine/SceneServoDeclaration.cs` (+ `.uid`) | CAT-039, EL-163 | |
| `engine/SceneTiltSensorDeclaration.cs` (+ `.uid`) | CAT-023 | |
| `engine/SceneTimerDeclaration.cs` (+ `.uid`) | CAT-033 | |
| `engine/SceneTransmissionJoint.cs` (+ `.uid`) | CAT-018, CAT-057, CAT-071, EL-053, EL-200, EL-201 | |
| `engine/SimulationCounters.cs` (+ `.uid`) | CAT-020, EL-181, EL-183 | |
| `engine/SimulationLatches.cs` (+ `.uid`) | CAT-037, EL-069 | |
| `engine/SimulationOscillators.cs` (+ `.uid`) | CAT-017 | |
| `engine/SimulationState.cs` (+ `.uid`) | — | N-INFRA |
| `engine/SimulationTimers.cs` (+ `.uid`) | CAT-033 | |
| `engine/SimulationTransaction.cs` (+ `.uid`) | — | N-INFRA |
| `engine/SlidingBlade.cs` (+ `.uid`) | CAT-007, CAT-051, EL-109, EL-111, EL-163, EL-184, EL-216 | |
| `engine/SpringParameter.cs` (+ `.uid`) | CAT-062, CAT-071 | |
| `engine/TubeMouth.cs` (+ `.uid`) | CAT-030, CAT-048, CAT-049, CAT-050, CAT-051, EL-005, EL-012, EL-071, EL-072 | |
| `engine/TubeProxy.cs` (+ `.uid`) | CAT-016, CAT-048, CAT-051, CAT-070, CAT-071, EL-184 | |
| `engine/WorldGeometry.cs` (+ `.uid`) | CAT-006, CAT-007, CAT-008, CAT-011, CAT-012, CAT-021, CAT-029, CAT-031, CAT-032, CAT-036, CAT-038, CAT-040, CAT-041, CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, CAT-055, CAT-056, CAT-059, CAT-068, CAT-072 | |

## 7.4 LEGACY-0d: parked GPU pipe declarations

`engine/gpu/WorkshopPipe.cs` and `engine/gpu/AnnularProfile.cs` are excluded from every build at `a6c914e` (`CuriousContraptions.csproj` L29). Whether Story 6.6 keeps them as its starting point or Story 7.4 deletes them is an owner decision recorded in [CAT-048 §6](CAT-048-pipe.md#6-open-questions); each has its own row either way.

| File | Harvested into | Notes |
| --- | --- | --- |
| `engine/gpu/AnnularProfile.cs` (+ `.uid`) | CAT-048, EL-071 | Parked; disposition decided by Story 7.4. |
| `engine/gpu/WorkshopPipe.cs` (+ `.uid`) | CAT-048, EL-071, EL-102, EL-107 | Parked; disposition decided by Story 7.4. |

## 7.4 LEGACY-0d: uncompiled `parts/*.cs`

The nine compiled scripts (Ball, Basket, Ramp, Switch, Lamp, Wall, Delay, Bumper, Domino) are excluded.

| File | Harvested into | Notes |
| --- | --- | --- |
| `parts/BallDetectorPart.cs` (+ `.uid`) | CAT-002 | |
| `parts/BatteryPart.cs` (+ `.uid`) | CAT-005, EL-196 | |
| `parts/BeamCombinerPart.cs` (+ `.uid`) | CAT-006, EL-174, EL-175, EL-213 | |
| `parts/BeamShutterPart.cs` (+ `.uid`) | CAT-007 | |
| `parts/BeamSplitterPart.cs` (+ `.uid`) | CAT-008 | |
| `parts/BellPart.cs` (+ `.uid`) | CAT-009 | |
| `parts/BellowsPart.cs` (+ `.uid`) | CAT-010 | |
| `parts/CannonPart.cs` (+ `.uid`) | CAT-016, EL-091, EL-095 | |
| `parts/ClockPart.cs` (+ `.uid`) | CAT-017, EL-075, EL-104 | |
| `parts/ClutchPart.cs` (+ `.uid`) | CAT-018, EL-116 | |
| `parts/ColourFilterPart.cs` (+ `.uid`) | CAT-011, CAT-031, CAT-055, EL-143, EL-144, EL-145, EL-173, TH-06 | |
| `parts/ConveyorPart.cs` (+ `.uid`) | CAT-019, EL-110, EL-111, EL-116, EL-156, EL-157, EL-158, EL-200, EL-201, EL-202, EL-209 | |
| `parts/CounterPart.cs` (+ `.uid`) | CAT-020, EL-075, EL-081, EL-183 | |
| `parts/ElectricalLogicPart.cs` (+ `.uid`) | CAT-013, CAT-024, CAT-025, CAT-026, CAT-027, EL-041, EL-133, EL-134, EL-135, EL-136, EL-137 | |
| `parts/FanPart.cs` (+ `.uid`) | CAT-028, EL-042, EL-108, EL-209 | |
| `parts/FlashlightPart.cs` (+ `.uid`) | CAT-029, EL-153, EL-154, EL-155, EL-173, EL-210, EL-211, TH-06, TH-07, TH-36 | |
| `parts/FunnelPart.cs` (+ `.uid`) | CAT-030, EL-005 | |
| `parts/HoldTimerPart.cs` (+ `.uid`) | CAT-033 | |
| `parts/ImpactLeverPart.cs` (+ `.uid`) | CAT-034, EL-109, EL-163 | |
| `parts/LaserPart.cs` (+ `.uid`) | CAT-036, EL-176, EL-177, EL-178 | |
| `parts/LatchPart.cs` (+ `.uid`) | CAT-037, EL-041, EL-163, EL-164, EL-181, EL-183, EL-185 | |
| `parts/LightReceiverPart.cs` (+ `.uid`) | CAT-012, CAT-021, CAT-032, CAT-038, CAT-040, CAT-056, CAT-068, CAT-072, EL-146, EL-147, EL-148, EL-149, EL-150, EL-151, EL-152, EL-214 | |
| `parts/LinearPusherPart.cs` (+ `.uid`) | CAT-039, EL-068 | |
| `parts/MirrorPart.cs` (+ `.uid`) | CAT-041, EL-212 | |
| `parts/MotorPart.cs` (+ `.uid`) | CAT-042, EL-037, EL-110, EL-116, EL-156, EL-157, EL-158, EL-196, EL-199, GAP-04 | |
| `parts/OpticalLogicPart.cs` (+ `.uid`) | CAT-043, CAT-044, CAT-045, CAT-046, CAT-047, EL-138, EL-139, EL-140, EL-141, EL-142 | |
| `parts/PipeArt.cs` (+ `.uid`) | CAT-030, CAT-048, CAT-049, CAT-050 | |
| `parts/PipeBendPart.cs` (+ `.uid`) | CAT-049, CAT-050, EL-009, EL-010, EL-072, EL-107, EL-108, EL-109, EL-162 | |
| `parts/PipePart.cs` (+ `.uid`) | CAT-048, EL-071, EL-102, EL-107 | |
| `parts/PoweredGatePart.cs` (+ `.uid`) | CAT-051, EL-021, EL-107, EL-162, EL-163, EL-184, EL-216 | |
| `parts/PressurePlatePart.cs` (+ `.uid`) | CAT-052, EL-059 | |
| `parts/PulleyPart.cs` (+ `.uid`) | CAT-053, EL-206, EL-207 | |
| `parts/ReverseTransmissionPart.cs` (+ `.uid`) | CAT-057, EL-110, EL-203 | |
| `parts/RopeAnchorPart.cs` (+ `.uid`) | CAT-058, EL-064 | |
| `parts/SolarPanelPart.cs` (+ `.uid`) | CAT-059, EL-153, EL-154, EL-196, EL-210, EL-211 | |
| `parts/SoundMeterPart.cs` (+ `.uid`) | CAT-060, EL-018, EL-043, EL-044, EL-045, EL-049, EL-181, EL-182 | |
| `parts/SpeakerPart.cs` (+ `.uid`) | CAT-061, EL-040, EL-044, EL-045, EL-046, EL-047, EL-049, EL-179, EL-180 | |
| `parts/SpringPart.cs` (+ `.uid`) | CAT-062, EL-053, EL-054, EL-156, EL-157, EL-158, EL-194 | |
| `parts/TrampolinePart.cs` (+ `.uid`) | CAT-065, EL-215 | |
| `parts/WeightPart.cs` (+ `.uid`) | CAT-067, EL-014, EL-054, EL-062, EL-063, EL-065, EL-069, EL-070, EL-080, EL-110, EL-155, EL-204, EL-207 | |
| `parts/WindChimesPart.cs` (+ `.uid`) | CAT-069 | |
| `parts/WindmillPart.cs` (+ `.uid`) | CAT-070 | |
| `parts/WoundSpringPart.cs` (+ `.uid`) | CAT-071, EL-053, EL-054, EL-055, EL-056, EL-057, EL-063 | |

## 7.4 LEGACY-0d: `parts/scenes/*.tscn`

Scenes that bind a deleted script. The nine scenes that bind a compiled script (`ball`, `basket`, `bumper`, `delay`, `domino`, `goal_light`, `ramp`, `switch`, `wall`) are kept.

| File | Harvested into | Notes |
| --- | --- | --- |
| `parts/scenes/ball_detector.tscn` | CAT-002 | |
| `parts/scenes/battery.tscn` | CAT-005 | |
| `parts/scenes/beam_combiner.tscn` | CAT-006 | |
| `parts/scenes/beam_shutter.tscn` | CAT-007 | |
| `parts/scenes/beam_splitter.tscn` | CAT-008 | |
| `parts/scenes/bell.tscn` | CAT-009 | |
| `parts/scenes/bellows.tscn` | CAT-010 | |
| `parts/scenes/blue_filter.tscn` | CAT-011, EL-145 | |
| `parts/scenes/blue_receiver.tscn` | CAT-012, EL-148 | |
| `parts/scenes/both_gate.tscn` | CAT-013, EL-133 | |
| `parts/scenes/cannon.tscn` | CAT-016 | |
| `parts/scenes/clock.tscn` | CAT-017 | |
| `parts/scenes/clutch.tscn` | CAT-018 | |
| `parts/scenes/conveyor.tscn` | CAT-019 | |
| `parts/scenes/counter.tscn` | CAT-020 | |
| `parts/scenes/cyan_receiver.tscn` | CAT-021, EL-150 | |
| `parts/scenes/electrical_nand.tscn` | CAT-024, EL-137 | |
| `parts/scenes/electrical_nor.tscn` | CAT-025, EL-136 | |
| `parts/scenes/electrical_or.tscn` | CAT-026, EL-134 | |
| `parts/scenes/electrical_xor.tscn` | CAT-027, EL-135 | |
| `parts/scenes/fan.tscn` | CAT-028 | |
| `parts/scenes/flashlight.tscn` | CAT-029 | |
| `parts/scenes/funnel.tscn` | CAT-030 | |
| `parts/scenes/green_filter.tscn` | CAT-031, EL-144 | |
| `parts/scenes/green_receiver.tscn` | CAT-032, EL-147 | |
| `parts/scenes/hold_timer.tscn` | CAT-033 | |
| `parts/scenes/impact_lever.tscn` | CAT-034 | |
| `parts/scenes/laser.tscn` | CAT-036 | |
| `parts/scenes/latch.tscn` | CAT-037 | |
| `parts/scenes/light_receiver.tscn` | CAT-038, EL-214 | |
| `parts/scenes/linear_pusher.tscn` | CAT-039 | |
| `parts/scenes/magenta_receiver.tscn` | CAT-040, EL-151 | |
| `parts/scenes/mirror.tscn` | CAT-041 | |
| `parts/scenes/motor.tscn` | CAT-042 | |
| `parts/scenes/optical_and.tscn` | CAT-043, EL-138 | |
| `parts/scenes/optical_nand.tscn` | CAT-044, EL-142 | |
| `parts/scenes/optical_nor.tscn` | CAT-045, EL-141 | |
| `parts/scenes/optical_or.tscn` | CAT-046, EL-139 | |
| `parts/scenes/optical_xor.tscn` | CAT-047, EL-140 | |
| `parts/scenes/pipe.tscn` | CAT-048 | |
| `parts/scenes/pipe_bend_45.tscn` | CAT-049 | |
| `parts/scenes/pipe_bend_90.tscn` | CAT-050 | |
| `parts/scenes/powered_gate.tscn` | CAT-051 | |
| `parts/scenes/pressure_plate.tscn` | CAT-052 | |
| `parts/scenes/pulley.tscn` | CAT-053 | |
| `parts/scenes/red_filter.tscn` | CAT-055, EL-143 | |
| `parts/scenes/red_receiver.tscn` | CAT-056, EL-146 | |
| `parts/scenes/reverse_transmission.tscn` | CAT-057 | |
| `parts/scenes/rope_anchor.tscn` | CAT-058 | |
| `parts/scenes/solar_panel.tscn` | CAT-059 | |
| `parts/scenes/sound_meter.tscn` | CAT-060 | |
| `parts/scenes/speaker.tscn` | CAT-061 | |
| `parts/scenes/spring.tscn` | CAT-062 | |
| `parts/scenes/trampoline.tscn` | CAT-065 | |
| `parts/scenes/weight.tscn` | CAT-067, EL-204 | |
| `parts/scenes/white_receiver.tscn` | CAT-068, EL-152 | |
| `parts/scenes/wind_chimes.tscn` | CAT-069 | |
| `parts/scenes/windmill.tscn` | CAT-070 | |
| `parts/scenes/wound_spring.tscn` | CAT-071 | |
| `parts/scenes/yellow_receiver.tscn` | CAT-072, EL-149 | |

## 7.4 LEGACY-0d: `parts/catalog/*.tres`

Catalogue entries other than the ten that the compiled game uses (`ball`, `basket`, `bowling`, `bumper`, `delay`, `domino`, `lamp`, `ramp`, `switch`, `wall`). All sixty bind a deleted scene. `balloon.tres` and `tennis.tres`, which bind the kept `ball.tscn`, are kept ([Kept by ruling](#kept-by-ruling)).

| File | Harvested into | Notes |
| --- | --- | --- |
| `parts/catalog/ball_detector.tres` | CAT-002 | |
| `parts/catalog/battery.tres` | CAT-005, EL-196 | |
| `parts/catalog/beam_combiner.tres` | CAT-006, EL-213 | |
| `parts/catalog/beam_shutter.tres` | CAT-007 | |
| `parts/catalog/beam_splitter.tres` | CAT-008 | |
| `parts/catalog/bell.tres` | CAT-009 | |
| `parts/catalog/bellows.tres` | CAT-010 | |
| `parts/catalog/blue_filter.tres` | CAT-011, EL-145 | |
| `parts/catalog/blue_receiver.tres` | CAT-012, EL-148 | |
| `parts/catalog/both_gate.tres` | CAT-013, EL-133 | |
| `parts/catalog/cannon.tres` | CAT-016, EL-091, EL-095 | |
| `parts/catalog/clock.tres` | CAT-017, EL-075 | |
| `parts/catalog/clutch.tres` | CAT-018 | |
| `parts/catalog/conveyor.tres` | CAT-019, EL-107, EL-202 | |
| `parts/catalog/counter.tres` | CAT-020, EL-081, EL-183 | |
| `parts/catalog/cyan_receiver.tres` | CAT-021, EL-150 | |
| `parts/catalog/electrical_nand.tres` | CAT-024, EL-137 | |
| `parts/catalog/electrical_nor.tres` | CAT-025, EL-136 | |
| `parts/catalog/electrical_or.tres` | CAT-026, EL-134 | |
| `parts/catalog/electrical_xor.tres` | CAT-027, EL-135 | |
| `parts/catalog/fan.tres` | CAT-028, EL-090, EL-100, EL-108, EL-209 | |
| `parts/catalog/flashlight.tres` | CAT-029, EL-210 | |
| `parts/catalog/funnel.tres` | CAT-030 | |
| `parts/catalog/green_filter.tres` | CAT-031, EL-144 | |
| `parts/catalog/green_receiver.tres` | CAT-032, EL-147 | |
| `parts/catalog/hold_timer.tres` | CAT-033 | |
| `parts/catalog/impact_lever.tres` | CAT-034, EL-096, EL-186 | |
| `parts/catalog/laser.tres` | CAT-036 | |
| `parts/catalog/latch.tres` | CAT-037 | |
| `parts/catalog/light_receiver.tres` | CAT-038, EL-214 | |
| `parts/catalog/linear_pusher.tres` | CAT-039, EL-097 | |
| `parts/catalog/magenta_receiver.tres` | CAT-040, EL-151 | |
| `parts/catalog/mirror.tres` | CAT-041, EL-212 | |
| `parts/catalog/motor.tres` | CAT-042, EL-037, EL-079, EL-196, EL-199, EL-200 | |
| `parts/catalog/optical_and.tres` | CAT-043, EL-138 | |
| `parts/catalog/optical_nand.tres` | CAT-044, EL-142 | |
| `parts/catalog/optical_nor.tres` | CAT-045, EL-141 | |
| `parts/catalog/optical_or.tres` | CAT-046, EL-139 | |
| `parts/catalog/optical_xor.tres` | CAT-047, EL-140 | |
| `parts/catalog/pipe.tres` | CAT-048, EL-008, EL-071, EL-103 | |
| `parts/catalog/pipe_bend_45.tres` | CAT-049, EL-009 | |
| `parts/catalog/pipe_bend_90.tres` | CAT-050, EL-010, EL-072 | |
| `parts/catalog/powered_gate.tres` | CAT-051, EL-184 | |
| `parts/catalog/pressure_plate.tres` | CAT-052, CAT-064, EL-059, EL-076, EL-077, GAP-06 | |
| `parts/catalog/pulley.tres` | CAT-053, EL-206 | |
| `parts/catalog/red_filter.tres` | CAT-055, EL-143 | |
| `parts/catalog/red_receiver.tres` | CAT-056, EL-146 | |
| `parts/catalog/reverse_transmission.tres` | CAT-057, EL-203 | |
| `parts/catalog/rope_anchor.tres` | CAT-058, EL-064 | |
| `parts/catalog/solar_panel.tres` | CAT-059, EL-211 | |
| `parts/catalog/sound_meter.tres` | CAT-060, EL-044 | |
| `parts/catalog/speaker.tres` | CAT-061, EL-180 | |
| `parts/catalog/spring.tres` | EL-194 | |
| `parts/catalog/trampoline.tres` | CAT-065 | |
| `parts/catalog/weight.tres` | CAT-052, CAT-067, EL-014, EL-085, EL-204 | |
| `parts/catalog/white_receiver.tres` | CAT-068, EL-152 | |
| `parts/catalog/wind_chimes.tres` | CAT-069 | |
| `parts/catalog/windmill.tres` | CAT-070 | |
| `parts/catalog/wound_spring.tres` | CAT-071 | |
| `parts/catalog/yellow_receiver.tres` | CAT-072, EL-149 | |

## Kept files whose consumers are deleted

Tracked files that survive Epic 7 although their consumers are deleted. Listed for traceability, not deletion. Recipe files are Chrome UI recipe inputs for the legacy Playtest/MCP adapter (`tools/Playtest`, deleted at 7.2); source files are hash manifests whose listed files are mostly paths Epic 7 deletes.

| File | Deleted consumers | Notes |
| --- | --- | --- |
| `docs/cannon-recipes.json` | `tools/Playtest` | Cited by CAT-016 (open question 10). |
| `docs/committed-compliant-recipes.json` | `tools/Playtest` | Cited by CAT-065. |
| `docs/flight-regression-recipes.json` | `tools/Playtest` | Not cited by any spec. |
| `docs/impact-lever-frustum-recipes.json` | `tools/Playtest` | Cited by CAT-034. |
| `docs/impact-lever-obstruction-recipes.json` | `tools/Playtest` | Cited by CAT-034. |
| `docs/impact-lever-sphere-recipes.json` | `tools/Playtest` | Cited by CAT-034. |
| `docs/impact-lever-tube-recipes.json` | `tools/Playtest` | Cited by CAT-034. |
| `docs/impact-lever-ui-recipes.json` | `tools/Playtest` | Cited by CAT-034. |
| `docs/linear-pusher-recipes.json` | `tools/Playtest` | Not cited by any spec. |
| `docs/mechanical-work-recipes.json` | `tools/Playtest` | Not cited by any spec. |
| `docs/shared-sweep-ui-recipes.json` | `tools/Playtest` | Not cited by any spec. |
| `docs/trampoline-force-timing-recipes.json` | `tools/Playtest` | Not cited by any spec. |
| `docs/trampoline-lesson-recipes.json` | `tools/Playtest` | Not cited by any spec. |
| `docs/trampoline-pipe-recipes.json` | `tools/Playtest` | Not cited by any spec. |
| `docs/trampoline-rope-recipes.json` | `tools/Playtest` | Not cited by any spec. |
| `docs/wound-spring-recipes.json` | `tools/Playtest` | Not cited by any spec. |
| `docs/committed-motor-intervals-sources.json` | hashed files (574 of 671 deleted) | |
| `docs/contact-coupling-sources.json` | hashed baseline files | |
| `docs/contact-scratch-sources.json` | hashed files (558 of 647 deleted) | |
| `docs/coupled-motor-prediction-sources.json` | hashed files (574 of 671 deleted) | |
| `docs/held-wrench-path-work-sources.json` | hashed files (571 of 668 deleted) | |
| `docs/motor-power-sources.json` | hashed files (574 of 671 deleted) | |
| `docs/owned-counters-sources.json` | hashed files (563 of 658 deleted) | |
| `docs/owned-hold-timers-sources.json` | hashed files (560 of 651 deleted) | |
| `docs/owned-latches-sources.json` | hashed files (565 of 662 deleted) | |
| `docs/owned-oscillators-sources.json` | hashed files (561 of 654 deleted) | |
| `docs/owned-timers-final-sources.json` | hashed files (559 of 650 deleted) | |
| `docs/owned-timers-sources.json` | hashed files (559 of 650 deleted) | |
| `docs/performance-audit-sources.json` | `tools/Performance`, `PerformanceAuditTests.cs` | |
| `docs/performance-run-identity-sources.json` | `tools/Performance` | |
| `docs/prediction-constraint-work-sources.json` | hashed files (558 of 647 deleted) | |
| `docs/prediction-samples-sources.json` | hashed files (558 of 647 deleted) | |
| `docs/spatial-work-sources.json` | hashed source files | |
| `docs/springboard-lesson-sources.json` | hashed files (556 of 644 deleted) | |
| `docs/springboard-precharged-sources.json` | hashed files (556 of 645 deleted) | |

### Kept by ruling

Tracked files that the inventory left unclear, kept by ruling in the closing pass (9 Oct 2026). Listed for traceability, not deletion.

| File | Harvested into | Ruling |
| --- | --- | --- |
| `tools/Coverage.Tests/` (4 tracked files) | — | Kept: it tests the kept `tools/Coverage`, which the inventory keeps with edits at Story 7.4 (drop its hard-coded `engine/physics` paths). |
| `parts/catalog/balloon.tres` | CAT-003, EL-016, EL-090, EL-101, EL-208, TH-25 | Kept: a catalogue variant of the compiled BallPart (binds the kept `ball.tscn`), delivered in Story 12.1. |
| `parts/catalog/tennis.tres` | CAT-052, CAT-064, EL-076, EL-189 | Kept: a catalogue variant of the compiled BallPart (binds the kept `ball.tscn`), delivered in Story 12.1. |

## Gaps

Every deletion-scope path above has a row. The current G/N source associations are synchronized; independent enumeration covers all 1,580 tracked deletion-scope paths with no uncovered path. No harvest gap remains within the reviewed scope. This is declaration readiness, not authorization to skip the purge stories' own checks.

Resolved in the closing pass (9 Oct 2026):
- `CuriousContraptions.tests/ImpactFrameTests.cs`: harvested into CAT-015 (facts 12–13) and CAT-062 (facts 16–17).
- `CuriousContraptions.tests/FlightCampaignTests.cs`: harvested into CAT-004 (fact 14), CAT-048 (fact 23), CAT-050 (fact 12) and CAT-062 (fact 18).
- `CuriousContraptions.tests/PhysicsTiltSensorTests.cs`, `engine/physics/PhysicsTiltSensor.cs` and `engine/SceneTiltSensorDeclaration.cs`: harvested into CAT-023 (facts 4–9).
- `CuriousContraptions.tests/CompressionTransferSupplyTests.cs`: harvested into CAT-010 by Batch D (facts 19–22).
- `tools/Coverage.Tests/`, `parts/catalog/balloon.tres` and `parts/catalog/tennis.tres`: kept by ruling ([Kept by ruling](#kept-by-ruling)).
