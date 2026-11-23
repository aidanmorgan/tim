---
title: 'Table-driven wire response codec (S6b)'
type: 'refactor'
created: '2026-11-23'
status: 'draft'
baseline_revision: 'captured at dispatch (the main that contains the planning work)'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '{project-root}/docs/testing-guidance.md'
  - '{project-root}/_bmad/custom/element-free-engine-design.md'
  - '{project-root}/_bmad/custom/project-engineering-contract.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-s6-table-driven-capability-plumbing.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Every capability that publishes committed state adds a read family to the wire response, and each family is threaded by hand through four places in `engine/gpu/WorkshopWireFlatBuffers.cs`: an encode call and an `Add` call in `EncodeResponse` (`:462`, complexity 23), a decode loop or call in `DecodeResponseRecord` (`:580`, complexity 25, the gate's limit), and the literal response field count 41 in `OpenFrame` (`:45`). The next read family fails the maintainability gate inside an unrelated engine slice, and a family encoded but not decoded, or a field count not bumped, is caught only by a failing round trip.

**Approach:** One static table of read-family codecs: each row pairs one family's encoder, its `Add` into the response and its decoder, and the response encoder and decoder walk the table for the families and handle only the 17 fixed response fields themselves. The admitted response field count is the fixed count plus the table's row count. The schema, the set of fields and every decoded value are unchanged; where vectors and fields sit inside the buffer is not a contract (no byte-identity acceptance).

## Boundaries & Constraints

**Always:**
- Each of the 24 read families of `WorkshopRead` (`CuriousContraptions.Abstractions/WorkshopRead.cs:6-11`, the presentation read's eight families counted separately) has exactly one row; each of the 41 fields of `table WorkshopResponse` (`schemas/workshop_wire.fbs:282`) is either one of the 17 fixed fields or one row's field.
- Encoded responses decode to values equal field by field to those decoded today; refusals keep their types and messages, including "Runtime record contains undefined fields." and the absent-motion `WireFaultException`.
- The table is built once from static method groups; the per-tick encode path allocates nothing beyond today's and uses no reflection or runtime enum enumeration (the simulation worker, the web host and the trimmed, ahead-of-time Animation worker all run this codec).
- On each path `WorkshopMessages.ValidatePhysicalRead`, `ValidateCapture`, `ValidateScheduleIdentity`, `ValidatePresentationOwners` and the memory check run in the order they run today (encode: memory, capture, schedule identity, owners, physical read; decode: physical read, capture, schedule identity, owners, memory), and each family's decoder runs in today's order (bodies, captures, activations, timers, energy stores, contact works, electrical, drive targets, ropes, guides, surfaces, transmissions, logic, actuator drives, contact loads, activation memory, motion, then retentions, chambers, finite work, gas, integrity, airflow, coordinate travel), so the first refusal for an input with two defects is unchanged. No encoder refuses an input (read at planning: none throws), so the order in which the encoders run is not observable.

**Never:**
- A change to `schemas/*.fbs`, the generated accessors, the schema's field set, `WorkshopRead`, any read family's own encoder or decoder body, or the command codec.
- A new enum invented only to key the table; a retained copy of the old sequence; a default row; a test-only accessor on the production type.
- Suppressions, gate threshold changes, hashes or byte comparisons of encoded responses, mutants, browser runs.

## I/O & Edge-Case Matrix

| Row | Input / State | Expected Output / Behavior | Error Handling |
|-----|--------------|---------------------------|----------------|
| R1 | A read built on the base read `WireFaultTests.PhysicalRead(1)` (one body, its committed motion, one capture latch), with exactly one further family populated by one row valid under the production validators, for each of the other 21 families; the base families (bodies, motion, captures) are themselves each exercised by a case that varies them | Decodes to the same family values, field by field; every family other than the base read's three and the one under test decodes empty; the base read's three decode equal to the base | No error expected |
| R2 | The same base read with every family populated at once | Every family round-trips | No error expected |
| R3 | A response buffer built with the raw builder whose table carries 42 slots (one beyond the schema's 41) and otherwise a verifier-admitted response | Refused | `WireFaultException`, `WireFaultReason.OutOfRange`, "Runtime record contains undefined fields." |
| R4 | A read buffer built without a motion vector at tick 5 | Refused on decode | `WireFaultException`, `WireFaultReason.MissingField`, "The committed read at tick 5 has no motion." |
| R5 | An acknowledgement and an admission read (no motion, tick 0) | Round-trip as today | No error expected |

</intent-contract>

## Code Map

Baseline: captured at dispatch (coordinator decision 38, 23 November 2026): the published main that contains the planning work and the slices before this one; scope guards compare against it. Drafted on `25df3cd5`, the drafting revision only, not a baseline; every line cited here was read there on 24 November 2026. **Status: draft; runs before the engine programme** (owner decision 24h, 23 November 2026: S5b straight after S5, and S6's follow-ons S6b to S6g with S6; coordinator decision 42, 24 November 2026: S6f splits into S6f and S6g). Prerequisites: S5 and S5b published (S5's Never clause forbids changing the hand-written codecs while it is in flight; its generated types replace `engine/gpu/WorkshopWireGenerated.cs`, so cite generated types, never that file's lines) and the first S6 slice (`spec-s6-table-driven-capability-plumbing.md`) published. Review tier: **engine**. Delivery shape: one structural slice, the response codec named by S6 owner decision O1 (21 November 2026).

Planning verdict: re-audit required. The fifth audit (`docs/planning/elements/readiness/audits/audit-s6b-5.md`, Fail) raised B1 to B7 and N1 to N8; all are applied here on 24 November 2026: B1 (the byte claim removed), B2 (validator and decoder order stated as today's), B3 (R1 on a base read), B4 (R3's 42-slot builder), B5 (R4's decode test), B6 (the row shape), B7 and the closure findings of `audit-s6e-5.md` and `audit-s6f-5.md` (the inventory below).

### S6 follow-on inventory (shared by S6b to S6g; the one place every chain is assigned)

Owner decision 24h closes S6 when every per-capability enum chain is a table with a test that every enum value has a route; coordinator decision 42 adds that every chain is assigned to one slice or excluded with a reason, and none is claimed twice. This section is recorded in `docs/planning/invest/delivery-batches.md`, story S6; each follow-on contract cites it.

**Denominator.** 211 production enum declarations (210 names; `ColliderShapeKind` is declared twice) at `25df3cd5`: `ast-grep scan --inline-rules '{"id":"en","language":"csharp","rule":{"kind":"enum_declaration"}}'` over the roots of the switch command below, generated files and `obj/` dropped (`engine/gpu/WorkshopWireGenerated.cs` holds 8 more, generated and not counted). Framework enums (`Key`, `JsonValueKind`) are not declarations of this repository and are outside the denominator.

**A chain** is, in one member: (a) a `switch` statement or expression, or an `==`, `!=` or `is` ladder, that names two or more distinct members of one enum; or (b) a `switch` whose arms are type or declaration patterns that map three or more distinct descriptor types to a closed capability value. A single-member comparison (`x.Kind == E.V` selecting one branch or finding one node) is not a chain: the closing run lists each one it keeps, with file:line.

**Commands**, run at the closing review of S6g on the candidate. (1) Switches: `ast-grep scan --inline-rules '{"id":"sw","language":"csharp","rule":{"any":[{"kind":"switch_statement"},{"kind":"switch_expression"}]}}' --json=stream engine CuriousContraptions.Bundle CuriousContraptions.Core CuriousContraptions.Simulation CuriousContraptions.Abstractions CuriousContraptions.Abstractions.Mechanical CuriousContraptions.Abstractions.Signal CuriousContraptions.Abstractions.Electrical CuriousContraptions.Abstractions.Fluid CuriousContraptions.Animation CuriousContraptions.Animation.Worker CuriousContraptions.web ui`, generated files dropped, arm labels grouped by enum, switches with two or more distinct members. (2) Ladders: the member scan: every method, constructor, local function and property that names two or more distinct members of one enum in a `==`, `!=`, `is`, `or`, `not` or `case` position. (3) Type-pattern dispatch: the same switch scan restricted to arms that are type or declaration patterns, three or more distinct types per switch. (4) Wide members: every member that names three or more distinct members of one enum anywhere. All four ran on 24 November 2026 at `25df3cd5` (ast-grep 0.51.3). Command 3 matches arm labels by pattern, so it misses arms with `when` guards: it reproduced ten of the fifth audit's type-pattern sites and found three more, over diagnostic record types (`engine/gpu/WorkshopObservationProjection.cs:588`, `WorkshopWireFlatBuffers.Observation.cs:80` and `:179`), excluded below with the diagnostics; the audit's two guarded sites (`SignalContracts.cs:17`, `RunControlBindings.cs:158`) were taken from `audit-s6b-5.md` B7 and read. These are text and syntax-tree scans, not type-resolved, so a chain written through a `using static` or a local alias is not seen and the lists below are a floor.

**Rule.** After the candidate of S6g, commands (1) to (4), grouped by enum, print only: (i) members that are table rows or the tables' own lookup, for an in-scope enum; (ii) members the run log lists as single-member comparisons kept; (iii) enums named in the exclusion lists below. Any other enum that prints is not added to a slice and not excluded by the implementer: it is recorded in the run log and returned to the coordinator before review.

**In scope: 38 enum names (39 declarations), by slice.** S6b: no enum (the 24 read families of the response). S6 first slice: one chain of a S6c enum (`PresentationDriver`, the continuous committed query). S6c (8): `PresentationDriver`, `PresentationProperty`, `AnimationProperty`, `WorkshopVisualProperty`, `PresentationCurve`, `AnimationCurve`, `AnimationImpulseCurve`, `PresentationValueMapping`. S6d (8): `ActivationNodeKind`, `SignalNodeKind`, `ActivationOccurrenceKind`, `SimulationEventKind`, `SignalTerminal`, `PhysicalRequestTargetKind`, `WorkshopGoalKind`, and `TimerCompletionPolicy` as a column of the node-kind row. S6e (14): `SupplyNodeKind`, `SupplyConsumerKind`, `SupplyTerminal`, `LogicGateOperation`, `RunControlLane`, `RunControlValueKind`, `PortRole`, `SimulationDomain`, `WorkshopConnectionDomain`, `PortDirection`, `PortSignalKind`, the engine copies `WorkshopPortDirection` and `WorkshopSignalKind`, and `ParameterUnit` (the unit label switch of `ui/WorkshopConfiguration.cs:255`, 18 arms; electrical added `Joule` and `Watt` and the gas, liquid and thermal engines add units, so it grows with capabilities). S6f (3 names, 4 declarations): `ColliderShapeKind` (both declarations), `ShapeKind`, and the private placement enum `Shape` (`engine/PlacementPose.cs:62`). S6g (5): `CoordinateKind`, `ContinuousWorkInputKind`, `BodyTargetKind`, `TargetSelectorKind`, `MechanicalValueSource`.

**Chains the fifth audits found unassigned, each now assigned to exactly one slice** (sites as read on `25df3cd5`; each is owned scope in that slice's contract):

| Chain | Enum | Slice |
|---|---|---|
| `engine/gpu/WorkshopConnections.cs:96` `Validate`, `:132` `ValidateIdentity`, `:140` `ValidateActivation`, `:153` `ValidateElectrical`, `:161` `ValidateRope`, `:170` `ValidateMechanical` | `WorkshopConnectionDomain`, `PortRole`, `SupplyTerminal` | S6e |
| `CuriousContraptions.Abstractions/ActivationTimerValues.cs:62` `Validate` | `ActivationOccurrenceKind` | S6d |
| `CuriousContraptions.Abstractions/EventRingBuffer.cs:21` `Validate` | `SimulationEventKind` | S6d |
| `engine/gpu/WorkshopGoalEvaluator.cs:13`, `:20`, `:27`, `:37`, `:41` | `WorkshopGoalKind` | S6d |
| `engine/PlacementPose.cs:73` `Reach`, `:80` `Partner`, `:87` `SupportDistance`, `:187` `Pose`, `:227` `Vertical`, `:251` `Overlaps` | private `Shape` | S6f |
| `engine/presentation/AnimationBatch.cs:87` `RegisterOscillation` (two arms) | `AnimationProperty` | S6c |
| `CuriousContraptions.Bundle/RunControlBindings.cs:13` `Lanes` (two arms) | `RunControlLane` | S6e |
| `CuriousContraptions.Bundle/RunControlBindings.cs:26` `Matches` (two arms) | `MechanicalValueSource` | S6g |
| `CuriousContraptions.Bundle/DrivenSurfaceValidation.cs:47` `ValidTravel` (two arms) | `MechanicalValueSource` | S6g |
| `CuriousContraptions.Simulation/WorkshopPhysicsEngine.ContinuousCommit.cs:51` `CommitContinuousWork` (two arms) | `ContinuousWorkInputKind` | S6g |
| Type-pattern dispatch (descriptor class to a signal capability): `CuriousContraptions.Abstractions/SignalContractValues.cs:33-44` (descriptor class to `SignalNodeKind`), `CuriousContraptions.Bundle/SignalBundleValidation.cs:114`, `engine/gpu/WorkshopActivationCompiler.cs:51`, `CuriousContraptions.Abstractions.Signal/SignalContracts.cs:17` (`SignalTerminals.Kind`), `CuriousContraptions.Bundle/RunControlBindings.cs:158` (`IsBoundToSignal`: which descriptor binds a parameter) | `SignalNodeKind`, `SignalTerminal` | S6d |
| Type-pattern dispatch (descriptor class to a supply capability): `CuriousContraptions.Abstractions/SupplyContractValues.cs:58` (`SupplyTerminals.Admits`: descriptor to admitted terminals), `CuriousContraptions.Bundle/SupplyBundleValidation.cs:58`, `:87` | `SupplyNodeKind`, `SupplyTerminal` | S6e. `engine/gpu/WorkshopSupplyCompiler.cs:73` (descriptor to node) and `:92` (target type to consumer) stay: they switch on descriptor types and no table keyed by an enum can replace them; their `CoordinateKind` property patterns at `:94-97` are S6g's |
| Type-pattern dispatch (airflow input descriptors): `CuriousContraptions.Bundle/AirflowDomain.cs:131`, `engine/gpu/WorkshopPhysicsCompiler.Airflow.cs:68` | `ContinuousWorkInputKind` | S6g |

**Double claims resolved (one owner each).** `engine/gpu/WorkshopActivationCompiler.cs:36-42` (`SignalTerminal` to `PortRole`) and its inverse `engine/gpu/ActivationNetwork.cs:131-137`: S6e, as the two directions of one `PortRole` row column; S6d keeps `:51-70` and no longer lists `:36` or `:134`. `CuriousContraptions.Abstractions.Signal/SignalObservationRules.cs`: `Supports` (`:8-19`, keyed by `PresentationDriver`) is S6c's, as the driver row's "observed signal node kinds" column; `Emits` (`:21-30`, keyed by `SimulationEventKind`) is S6d's, as the event-kind row's "emitted by node kinds" column.

**Excluded, with reason: 172 names in the lists below** (119 named by the first inventory and 53 added by the 24 November check, which read every use of every enum in neither the in-scope list nor the first exclusion list, at `25df3cd5`; `ObservationPayload`, generated, is outside the denominator). The closing run reprints these enums, which is not a finding. Each is a lifecycle, state, result, message, row, mode or declared field of one capability or one law, and none has a switch, a ladder or a wide member today (commands 1 to 4, then each use read): none gains a path when a capability is added. The first dispatch keyed by one of them must be a table, owned by the slice or story that adds that dispatch.
- *Lifecycle, phase and state.* `WorkshopSimulationPhase`, `ActivationPhase`, `ActivationTimerPhase`, `ChamberPhase`, `CaptureLatchPhase`, `TransmissionEngagementPhase`, `PrismaticDrivePhase`, `FiniteWorkObservationState`, `ActuatorObservationState`, `ContactLoadPhase`, `IntegrityPhase`, `RopeIntegrity`, `CompliantSampleState`, `ClockMappingState`, `WorldPlayback`, `TickAttempt`, `PresentationBoundary`, `ResidencePhase`, `RetentionPhase`, `PuzzleAvailability`, `EvaluationStage`, `CoordinateTravelPhase`, `GasOutletPhase`, `OccurrencePhase`, `OscillatorPhase`, `PassageSensorPhase`, `RopeState`, `VisitState`, `WorkshopGoalPhase`, `WorkshopCaptureStage`, `WorkshopTransportState`, `AnimationServiceTurn`, `ScheduleTransition`, `PresentationQuality`, `TraceEnd`, `ClockRecalibrationCause`, `AnimationPlayback` (three states of one slot; each comparison names one), `BodyCandidateStatus`, `PhysicsCandidateStatus`, `GasInventoryPhase`, `OrientationSensorPhase` (`Armed`, `Fired`), `SimulationLatchPhase` (`Off`, `On`), `SignalContactState`, `ManifoldSoftness`, `GpuDeviceOwnership`, `ContactPass`, `TraceTimingStatus`, `ClockBookkeepingFlags` (one flag).
- *Commands, outcomes, results and refusals.* `WorkshopCommandKind`, `WorkshopCommandOutcome`, `WorkshopRejection`, `WorkshopFaultKind`, `WireFaultReason`, `WorkerEnvelopeRejection`, `SupplyTopologyRefusal`, `PlacementRefusal`, `ClockDerivationOutcome`, `MaterialQueryResult`, `FiniteWorkClearance`, `FiniteWorkRequestResult`, `ClockProbeOutcome`, `EventPublicationPreparation`, `WorkshopEditRejection`, `WorkshopSaveRejection`, `BundleErrorCode`, `ElementPortRejection`, `EngineProviderRefusal`, `FiniteWorkTransactionRefusal`, `ReadReceiptRejection`, `RetentionRequestResult`, `SignalTopologyRejection`, `WorkshopObservationRefusal`, `MaterialContainmentResult`, `ContactWorkStore`, `PrismaticCommand` (a drive's per-tick decided state), `AnimationDeferralCause`, `BenchPlacementRejection` (its `refusal switch` at `ui/WorkshopBenchPlacement.cs:48` is keyed by the excluded `PlacementRefusal`), `BoxQueryRefusal`, `PhysicsFailure`, `PosePublicationPreparation`, `StateLatchRequest` (a two-bit request mask), `PhysicalRequestOperation` (one member, `Release`).
- *Cadence, clock, messages and diagnostics.* `SimulationCadence`, `AnimationCadence`, `PresentationCadence`, `ScheduleControlKind`, `WorkshopRuntimeRole`, `AnimationClock`, `AnimationControlKind`, `AnimationOutputKind`, `WorkshopGpuOperation`, `WorkshopDiagnosticCategory`, `WorkshopResponseKind`, `WorkshopCaptureKind`, `ExpectedRevisionKind`, `ClockDerivationReport`, `NativeClockProfile` (one member), `WorkshopClockDomain`, `WorkshopCaptureMode`, `WorkshopObservationQuery` (one member), `PhysicalStepProfile` (one member), `FaultSite`, `CellScale` and `TimeScale` (one wire unit each); UI: `WorkshopUiTarget`, `WorkshopUiControl`, `WorkshopUiFeedback`; parameter kinds: `ParameterKind`, `ParameterRunEdit`. The type-pattern switches over diagnostic record types (`WorkshopObservationProjection.cs:588`, `WorkshopWireFlatBuffers.Observation.cs:80`, `:179`) are diagnostics and are excluded with `ObservationPayload`.
- *Two-valued and small modes, options and flags.* `AtmospherePreset`, `CoordinateEndpoint`, `ElectricalEnable`, `ElectricalInputState` (a two-bit mask), `SensorParticipation`, `RevoluteTravel`, `ResizeAxes`, `ActivationCauseKind`, `AnimationStop`, `EnergyExpressionKind`, `PrismaticDriveKind`, `RevoluteDriveKind`, `RetentionMode`, `TimerBoundary`, `TimerElapsedSignal`, `RopeAttachmentRole` (read inside `BundleValidator.Port`, S6e's), `PrincipalMomentOperation`; animation evaluator options of one primitive: `AnimationRepeat`, `AnimationImpulseTiming`, `AnimationImpulseOverlap`, `PresentationOverlap`, `AnimationKind`; body motion: `RigidMotionKind`, `KinematicMode` (a kinematic body would be a new law owned by its first consumer); `AssistanceValue` (authoring template field); two-valued selectors and flags, each member compared at single sites and never switched or laddered: `ActivationTransitionKind`, `AirflowAvailability`, `AirflowSampling`, `AnimationEndpoint`, `AnimationImpulseVisibility`, `BodyMassSourceKind`, `CollisionParticipation`, `ConnectedCollision`, `ContactSupport`, `ContactWorkEffect`, `ContactWorkLimit`, `ParameterEditorPolicy`, `PartAllowanceKind`, `PayloadPolicy`, `PresentationEventMagnitude`, `PrismaticInactiveResponse`, `PrismaticTargetSelection`, `RopeWinding`, `TransmissionConstraintKind`, `TubeMouthEnd`, `WorkshopPlacementMode` (one member, `Manual`).
- *Geometry components, features and constraint rows of one law.* `GeometryComponent`, `MaterialComponent`, `BoxFace`, `HollowSurface`, `HollowContactSupport`, `CylinderFeature`, `HollowFeature`, `RollingSupport`, `PrismaticRowKind`, `RevoluteRowKind` (rows of one constraint law: `ENGINE-JOINT-KINDS` owns their growth).
- *Enums that `ENGINE-CAPABILITY-INSTANCES` deletes or replaces.* `SocketGlyph` (audit E3), `TubeEnd` (E5), `PresentationAxes` (E7).
- *Declared descriptor fields that no code dispatches on.* `ElectricalRole` (read at one site, `CuriousContraptions.Bundle/SignalBundleValidation.cs:167`, for `Consumer` only), `FluidPortRole` (declared and converted at the bundle boundary, never read), `ElementCategory` (a metadata field: the bench groups by it, no per-member route), `EngineProviderId` (an identity named in provider refusals). Each would gain members with a capability, but nothing dispatches on it today; the first chain keyed by one is a table, owned by the slice that adds it.
- The command codec `DecodeCommand` (`:156`, complexity 24) is keyed by `WorkshopCommandKind` and is excluded with it; member complexity limits belong to `ENGINE-CAPABILITY-INSTANCES` (owner decision 24h).

**Final counts at the drafting revision.** 210 enum names: 38 in scope (S6c 8, S6d 8, S6e 14, S6f 3, S6g 5), 172 excluded and named (119 from the first inventory, 53 from the 24 November check of the enums in neither list, none of which has a switch, ladder or wide member naming two or more of its members: by commands 1 to 4 and a read of each use), so 38 + 172 = 210 and no name is unclassified; none of the 53 is assigned to a slice, so no follow-on contract's scope line changes. Chains the earlier inventory missed, each now assigned to one slice: 13 (S6c 1, S6d 4, S6e 3, S6f 1, S6g 4), in the table above; the original exclusion list named 51 existing enums and gains 121 (19 the fifth audits named, 49 more from the 24 November re-run of commands 1 to 4, and the 53 above); 2 double claims resolved (one owner each); the other chains of the 38 enums are the sites listed in each slice's contract, one owner per site.

**Hot-path rule (coordinator decision 42; owner decision 3a).** A table that replaces a dispatch on a per-tick, per-frame or per-pair path states a measured before and after on a reference scene and must not regress; no fixed percentage. The implementation owner measures with a throwaway harness kept in the session scratch folder (not committed, no flag that disables an engine feature), on the pinned toolchain, five runs each on the baseline and on the candidate, and records every run's median in the run log with the scene's definition. The candidate passes if its median is not above the slowest of the baseline's five runs; otherwise the slice returns to the coordinator before review. Cold paths (admission, validation) record nothing. Each contract names its reference scene. The Animation worker and the simulation worker are trimmed and ahead-of-time compiled in CI only: trimmed safety of the tables (no reflection, no runtime enum enumeration) is by design and review, and the run log records it unverified locally.

### This slice

Owned scope:
- `engine/gpu/WorkshopWireFlatBuffers.cs` -- `:45` the response field count becomes the fixed count plus the row count; `:462-569` `EncodeResponse` walks the table for the families; `:580-717` `DecodeResponseRecord` walks the table; the inline decode loops at `:603-614` (bodies), `:616-621` (captures), `:623-631` (activations), `:636-642` (energy stores), `:643-648` (contact works), `:650-655` (electrical), `:656-661` (drive targets), `:670-675` (electrical logic), `:676-684` (actuator drives), `:685-690` (contact loads) and the rope encode loop `:471-486` move into per-family static functions beside the existing ones (`:206-460`, `:787-876`); the new table and its row type.
- `engine/gpu/WorkshopWireFlatBuffers.Airflow.cs`, `.Chambers.cs`, `.CoordinateTravel.cs`, `.GasRead.cs`, `.Retention.cs` -- unchanged bodies; their encoders and decoders (`Chambers.cs:73`, `:85`, `:97`, `:109`; `Retention.cs:10`, `:25`; `GasRead.cs:10`, `:23`, `:36`, `:47`; `Airflow.cs:11`, `:24`; `CoordinateTravel.cs:16`, `:28`) become row entries.
- `CuriousContraptions.tests/WorkshopResponseRouteTests.cs` -- new: R1 to R4 and the route-completeness case.
- `docs/planning/invest/delivery-batches.md` (story S6: the inventory and the slice list), `deferred-work.md`, `TODO.md`, `sprint-status.yaml` (key `s6b-response-codec-read-family-table`), this contract and its run log.

**Row shape (decided).** A row is a private `readonly record struct ReadFamily(EncodeFamily Encode, AddFamily Add, DecodeFamily Decode)` of three `static` method-group delegates. `EncodeFamily` returns `VectorOffset?` (null: the field is absent; the eight families that today always add their field, even empty, and the families that add only on a value decode alike, because an absent vector and an empty vector both decode empty). `AddFamily` is the generated `Wire.WorkshopResponse.Add<Field>`. `DecodeFamily(Wire.WorkshopResponse fb, in ReadContext context, ref ReadParts parts)` writes one member of the private mutable `ReadParts` struct, where `ReadContext` carries the epoch, the tick and the decoded bodies, and the table lists bodies before motion (motion's decoder needs both). `EncodeResponse` holds the offsets in a stack-allocated span of `Families.Length` entries (no heap allocation is added) and calls `StartWorkshopResponse` after every vector exists, as today. `ReadParts` and the one `new WorkshopRead(...)` call remain the one place a new family is named besides its row. Alternative considered and not chosen: splitting both members into helper methods with no table meets AC1 without the accumulator but leaves the field count and the family list as separate edits; the table is chosen because the route-completeness test then covers both.

Direct consumers (no signature changes): `WorkshopWireFlatBuffers.EncodeResponse` and `DecodeResponse` are called from `CuriousContraptions.Core/CoreRuntimeCodecs.cs:19` (the reusable-builder overload the simulation worker uses), `CuriousContraptions.Core/CoreCommandCodec.cs:13-14`, `CuriousContraptions.Abstractions/RuntimeCodecs.cs:13-14`, `:141`, `engine/gpu/WorkshopCommandExecutor.cs:76`, `engine/gpu/WorkshopObservation.cs:180`, `engine/gpu/BrowserWorkshopClient.cs:207`, `:327`, `engine/gpu/BrowserWorkshopClient.Schedule.cs:61`, `CuriousContraptions.Simulation/Program.cs:341` (`EmitRead`), `Program.Schedule.cs:89`, `CuriousContraptions.Animation.Worker/Program.cs:152`, `:215` (the trimmed, ahead-of-time Animation worker decodes responses) and 28 test files (`rg -l "EncodeResponse\(" CuriousContraptions.tests`). `WorkshopWireFlatBuffers.Observation.cs` encodes a nested response at `:119` and decodes it at `:158` through the same two functions; unchanged. `tools/e2e/run-reset-snapshot.ts:44` hand-lists the 41 response fields in schema order; it changes only if the schema does, and it does not here.

### Capability probe

Nothing in a solver law, the worker ABI, the wire schema or a shared presentation contract changes. Each existing test below asserts decoded values produced by the real codec.

- **Present:** bodies, captures, contact works, electrical, activations, timers -- `WorkshopWireFlatBuffersTests.Response_PhysicalRead_RoundTrips` (`:381`; it populates energy stores but does not assert them and asserts no motion).
- **Present:** motion -- `WireFaultTests.MotionRoundTripsItsIntervalAndOwnedSamples` (`:161`).
- **Present:** acknowledgement round trip -- `WorkshopWireFlatBuffersTests.Response_Acknowledgement_RoundTrips` (`:364`); admission without motion -- `WireFaultTests.AdmissionWithoutMotionStaysWithoutMotion`.
- **Present:** retentions -- `RetentionBoundaryTests`, `RetentionCompositionTests` (both encode responses); gas stores and integrity -- `BalloonTests`; chambers and finite-work actuators -- `OwnedFiniteWorkTransactionTests`; transmissions -- `ClutchTests`; drive targets and actuator drives -- `MotorTests`; activation memory -- `ClockTests`; ropes and guides -- `RopeTests`; compliant surfaces -- `TrampolineTests`; airflow and coordinate travel -- `ComposedPhysicalObservationTests`; contact loads and electrical logic -- `ComposedPhysicalSignalTests`, `BothGateTests` (each file listed by `rg -l "EncodeResponse\(" CuriousContraptions.tests`).
- **Present, encode-side control:** `WireFaultTests.CommittedReadWithoutMotionIsRejected` (`:193`) asserts `ArgumentException` at encode (`ValidatePhysicalRead`, `WorkshopMessages.cs:29`), not the decode refusal.
- **Present, untested:** that every schema field has exactly one route; the undefined-field refusal for a response (`rg "undefined fields" CuriousContraptions.tests` is empty); the decode refusal of a committed read without motion (`WireFaultException`, `MissingField`, thrown only by `DecodeResponseRecord` at `:696-698`; `rg "has no motion|MissingField" CuriousContraptions.tests` finds none); the energy-store family's decoded values. This slice adds the first three (route completeness, R3, R4) and R1 asserts the fourth.

### Decisions

- **Closed set from the schema, not a new enum (decided).** The 41 response fields are the closed set; inventing an enum to key the table would be a second declaration of the schema.
- **Derived field count is interim (decided).** The count 41 becomes 17 plus the row count. `ENGINE-WIRE-ABI` (owner decisions 24j, 24k) later derives the count from generated code; S6b's derivation is deleted then. `:50` and `:53` (the binding-shape checks at scene slot 27 and command slot 11), which `spec-s5-generate-wire-accessors-in-build.md:110` assigns to `ENGINE-WIRE-ABI`, stay untouched.
- **Command codec excluded (decided).** Keyed by lifecycle commands, not capabilities; see the inventory.
- **Proof by round trip per family (decided).** A row wired to the wrong family, or a family missing a row, fails R1 for that family.

### Concurrency

After S5, S5b and the first S6 slice; before S6c to S6g (one publisher at a time). S6c to S6g do not edit the wire codec except S6e (`WorkshopWireFlatBuffers.Compiled.cs:663-693`, the supply-consumer switches) and S6g (`Compiled.cs:65-77`, coordinate references), which are another file. The new test file is staged with `git add` before the candidate commit. Does not touch the test project file, `content/puzzles.json`, coverage or impact tables.

## Tasks & Acceptance

**Execution:**
1. `.tools/s6b-evidence/` -- run the S6 measurement (first S6 contract's Verification, with `CA1502: 12` in the measurement configuration, judged against AC1's 14) and record `EncodeResponse`, `DecodeResponseRecord` and the file's other members -- before. Run the hot-path measurement (S6b reference scene below) on the baseline.
2. `engine/gpu/WorkshopWireFlatBuffers.cs` and the five partial files -- per-family functions extracted; the table; both paths walk it; the derived field count -- R1 to R5.
3. `CuriousContraptions.tests/WorkshopResponseRouteTests.cs` -- the table's tests.
4. `.tools/s6b-evidence/` -- repeat both measurements -- after.
5. Planning records -- the inventory, S6b's result, S6 open until S6g publishes.

**Acceptance Criteria:**
- AC1: Given the measurement, when it runs on the candidate, then `EncodeResponse` and `DecodeResponseRecord` report no CA1502 warning at threshold 14, and no member this slice adds reports one.
- AC2: Given the maintainability gate, when it runs, then both canaries pass and violations are 0.
- AC3: Given the full local round, when it completes, then failures are 0 and skipped are 0 in every project, and the total is the baseline round plus 28 (R1 24, R2 1, completeness 1, R3 1, R4 1), each case named in the run log.
- AC4: Given the hot-path rule, when the S6b reference scene is measured on the baseline and the candidate, then the candidate's median `EncodeResponse` plus `DecodeResponse` time is not above the slowest baseline run. Reference scene: the R2 read (every family populated) encoded with the reusable builder and decoded, 10,000 round trips per run.

## Spec Change Log

## Review Triage Log

## Design Notes

1. Behaviour and scope: no behaviour change; the read-family threading becomes data (story S6, O1).
2. Reusable primitive: one row per read family. A new family adds a schema field, a `WorkshopRead` member, a `ReadParts` member, one row and its two functions; what it no longer adds is a branch to two complex members and a count literal.
3. Declared composition: the schema declares the fields; the table names no element or package.
4. Execution and ownership: same publisher, same builder reuse; immutable static table; the table walk is the per-tick cost AC4 measures.
5. Generality: route completeness is checked against the schema text, so a field added to the schema without a row fails a test; a new family needs the `WorkshopRead` and `ReadParts` members as well as its row, which R1 and the completeness case exercise.
6. Forward-only, deleted: the inline sequences in `EncodeResponse` and `DecodeResponseRecord` and the literal 41.
7. Prerequisites: S5, S5b, first S6 slice. E2, E3, E4, E5, E7, E8, E9 apply; E1, E6, D6, D7 unaffected.

## Verification

Executor: implementation owner. Environment: `PATH=~/.local/share/tim-toolchain/dotnet-10.0.403:~/.local/share/tim-toolchain/node-v26.12.1-darwin-arm64/bin:$PATH`, `DOTNET_ROOT=~/.local/share/tim-toolchain/dotnet-10.0.403`. The measurement rebuilds the root game project in place, so it runs before the compiler-log captures.

**Commands:**
- The measurement (task 1 and 4), before and after logs at `CA1502: 12`, judged against 14 -- expected before: `DecodeResponseRecord` 25, `EncodeResponse` 23 (measured 23 November 2026 at `25df3cd5`); after: AC1.
- The five compiler-log capture commands -- expected: every build succeeds.
- `dotnet run --file tools/anvil/VerifyPolicy.cs -- --maintainability --project-root .` -- expected: AC2.
- The registration check filter (a test file is added) -- expected: 0 failed, 0 skipped.
- `dotnet test CuriousContraptions.slnx -c Debug -f net10.0 --no-build --no-restore --nologo -noAutoResponse --filter "FullyQualifiedName~WorkshopResponseRouteTests|FullyQualifiedName~WorkshopWireFlatBuffersTests|FullyQualifiedName~WireFaultTests|FullyQualifiedName~WorkshopWireDecodeBoundaryTests|FullyQualifiedName~WorkshopReadTests|FullyQualifiedName~WorkshopWireTests"` -- expected: 0 failed, 0 skipped.
- `git add -N` on new files, then `git diff --check <baseline_revision>` -- expected: clean.
- One full local round -- expected: AC3.

**Manual checks (if no CLI):**
- Read the owned diff: every read family appears in exactly one row, and the fixed fields are encoded and decoded outside the table.

### Acceptance-to-test table

No tolerance is chosen: decoded values are compared exactly, because the codec carries f32 values bit for bit by contract (canonical serialized field values, AGENTS.md hard rule allowances). No test asserts buffer bytes.

| Rows | Named discriminating test | Control and boundary | Execution |
|---|---|---|---|
| R1 | `WorkshopResponseRouteTests.EachReadFamilyRoundTripsAlone` (theory over the 24 families; each case builds on `WireFaultTests.PhysicalRead(1)` and populates one family, or varies bodies, motion or captures, with one row valid under the production validators; prerequisites such as a read body for a capture target, a gas store's body, a motion interval for a broken-integrity ordinal are part of the case) | Control: every family other than the base read's three and the one under test decodes empty; the base read's three decode equal to the base. Boundary: the family populated with two rows keeps its order, and with zero rows decodes empty (absent and empty vectors alike) | planned |
| R2 | `WorkshopResponseRouteTests.EveryReadFamilyRoundTripsTogether` (the same base read, all families) | Control: R1's single-family responses | planned |
| Route completeness | `WorkshopResponseRouteTests.EverySchemaResponseFieldIsAFixedFieldOrExactlyOneReadFamily` (parses `table WorkshopResponse` from `schemas/workshop_wire.fbs`; each field's accessor name maps to the fixed list or to one family decoded by R1) | Control: a schema text with one extra field fails naming it | planned |
| R3 | `WorkshopResponseRouteTests.ATableOneFieldBeyondTheSchemaIsRefused`: built with the raw builder as `WireFaultTests.MessageWithoutSessionIsAMissingField` (`:200`) builds its response: `StartTable(42)`, the session and `physical_commit` as a valid response needs, and a byte at slot 41 so the vtable really has 42 slots. Asserted: `WireFaultReason.OutOfRange` and "Runtime record contains undefined fields." | Control: the same builder with `StartTable(41)` and no slot 41 passes the count check and is refused later with `WireFaultReason.MissingField` (no session), which proves the verifier admits the shape and only the count differs; the table's own 41 fields are admitted (R2). If the vendored verifier refuses the 42-slot buffer, the case returns to planning | planned |
| R4 | `WorkshopResponseRouteTests.ACommittedReadBuiltWithoutMotionIsAMissingFieldOnDecode`: raw builder as R3 with a session, `tick` 5, `kind` Read, `publication` 1, `physical_commit` and no motion vector; expected `WireFaultException`, `MissingField` | Control: the same buffer at tick 0 decodes with `Motion` null; `WireFaultTests.CommittedReadWithoutMotionIsRejected` (existing) is the encode-side control | planned |
| R5 | `WorkshopWireFlatBuffersTests.Response_Acknowledgement_RoundTrips` and `WireFaultTests.AdmissionWithoutMotionStaysWithoutMotion` (existing) | Their own controls | planned |
| AC1 to AC4 | Measurement, gate, full round, hot-path comparison | Before and after values and the five runs in the run log | planned |
