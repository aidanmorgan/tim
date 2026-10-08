# P0-004 R2 boundary reconciliation

This corrects the four findings in [the immutable R1 Fail review](../../verification/P0-004/review-r1.md).
It is part of the target design, not a claim that current runtime assemblies are isolated.

## Declaration and signature closure

Target namespaces match their assembly role: CuriousContraptions.Geometry,
CuriousContraptions.Protocol, CuriousContraptions.Simulation,
CuriousContraptions.Construction, CuriousContraptions.SimulationHost,
CuriousContraptions.Animation and CuriousContraptions.AnimationHost. Browser scene code retains
CuriousContraptions and its presentation namespace. Extraction updates all callers; old namespace
aliases and forwarding types are prohibited.

The following is the complete closure of types currently assigned Protocol by the member map.
Each row includes constructors, generated record equality/deconstruction and property accessors.
BCL primitives, Enum, exceptions, spans and arrays introduce no game assembly dependency.
Generic constraints remain unmanaged (and Enum for enum reads); a generic instantiation does not
make an arbitrary payload serializable. P0-005/016 must admit concrete wire payloads explicitly.

| Declaration group | Transitive game dependencies and method disposition |
| --- | --- |
| WorldGeneration, SimulationRevision, CommandSequence, CommandId, CommandEnvelope<T>, CommandResult | All Protocol; CommandOutcome is Protocol. Constructors perform intrinsic numeric validation. CommandAdmission and the inbox implementation remain host-local; no inbox reference enters these values. |
| BinaryInputId, ScalarInputId, RuntimeControlCommand | BinaryInputState and ControlValueKind are Protocol. Constructor signatures contain only these values and primitives. |
| EventStreamId, EventSequence, CommittedEventId, CommittedEvent<T>, PoseReadStamp | All Protocol, including stamp generation/revision/time and event stream/generation/sequence. CommittedEventStream<T> and EventStreamPhase stay SimulationHost. They are not schema dependencies of the record. |
| PhysicsBodyId, PhysicsColliderRevision, PhysicsDriveId, PhysicsGasNodeId, PhysicsJointId, ColliderChildId, MechanicalSourceId, MechanicalTransferId | Extract the identity declarations alone from their current solver files into Protocol; constructors depend only on BCL numeric validation. Local Index representations are not exported: P0-005/016 define typed stable values/generations and compiler/host maps local slots internally. Solver classes remain SimulationCore. |
| BodyPoseRead, BodyVelocityRead, PoseReferenceBinding | PhysicsMotionType, OwnerActivity and PoseReferenceKind are Protocol enums. RigidPose, RigidRotation and CollisionVector are Geometry leaves. BodyPoseRead.RelativePose and PoseReferenceBinding.Fixed/Attached/Resolve use only pure Geometry operations; intrinsic finite/enum checks remain here. Geometry loses its Godot conversion signatures at P0-007. |
| BooleanObservationSlot, BooleanReadKey, BooleanRead; ScalarObservationSlot, ScalarReadKey, ScalarRead; EnumObservationSlot<T>, EnumReadKey<T>, EnumRead<T> | Keys reference Protocol PhysicsBodyId and their own Protocol slot; ScalarUnit is Protocol. Move the current Validate(ReadOnlySpan<BodyPublicationRead>) topology/owner checks into the host publication builder. Intrinsic finite/unit/enum-value validation remains Protocol. Current CannonPhase is the only authored concrete enum-observation type: relocate its declaration to Protocol with the Cannon consumer extraction; no part class dependency remains. |
| ElectricalInputKey, ElectricalInputRead | SocketId and ElectricalAvailability are Protocol. Move the current body-topology Validate method into host publication validation as above. SocketId's declaration moves independently of MachineData's scene/content types. |
| PerformanceRunId, PerformanceCounter, PerformanceSample, PerformanceBatch | PerformanceStage, PerformanceOutcome and PerformanceMetric are Protocol. PerformanceTopology's pure enum relation may reside here; recorder/scheduler instances may not. Batch arrays are producer-owned until copied/frozen, then recipient-owned immutable values. No mutable recorder reference enters a batch. |
| AnimationTargetId, AnimationGeneration, AnimationBinding, AnimationSample, AnimationRead, AnimationFrameWork | AnimationProperty, AnimationPlayback, AnimationStop and AnimationEndpoint are Protocol. AnimationHandle remains an AnimationKernel-local owner/slot/version handle and never becomes a wire identity. |
| AnimationDefinition, AnimationFollowDefinition, AnimationImpulseDefinition, AnimationOscillationDefinition | AnimationCurve, AnimationRepeat, AnimationClock, AnimationImpulseCurve, AnimationImpulseOverlap, AnimationImpulseVisibility and AnimationImpulseTiming are Protocol. Intrinsic constructor validation remains Protocol. Move AnimationDefinition.Evaluate and AnimationImpulseDefinition.Envelope/Smooth/SquaredSine evaluator methods to AnimationKernel; worker evaluation cannot be invoked through Protocol. |
| AnimationOccurrenceId, AnimationImpulseRead, AnimationOscillationRead | Protocol values and primitives only; AnimationImpulseAdmission is Protocol where returned across admission boundaries. Evaluator state classes and their nested phases stay AnimationKernel. |

BodyPublicationRead is **SimulationHost**, with a SimulationCore BodyQueryRead reference.
It is an internal producer aggregate, never the outgoing packet. Its BodyPoseRead, BodyVelocityRead
and OwnerActivity fields reference Protocol, which is permitted. BodyQueryRead and CompoundGeometry
remain Core, including immutable shape references and indexer results. Outgoing publication replaces
Query with stable geometry-resource identity, collider revision and value-only query/results; it
does not move live geometry into Protocol. CollisionParticipation may be a Protocol discriminant,
but simulation alone owns its authoritative value. P0-014 performs this producer split;
P0-016 encodes the resulting admitted values. No Protocol declaration, method parameter, return
value, generic constraint or retained field references Core or Host after extraction.

Closed generic CommittedEvent<AcousticWavefront> is currently an in-process host/consumer use, not
permission to serialize its current contents. Its typed immutable payload conversion remains the
named occurrence/acoustic extraction and P0-016 encoding responsibility. This does not add a
Protocol-to-animation-host declaration reference.

## Producer parent references and recipient replacements

The member map names the primary implementing owner of the current reference after splitting,
not permission to transfer the current mixed object wholesale.

| Current MachineWorld reference | Exclusive target reference and caller replacement |
| --- | --- |
| _scalarObservations, _booleanObservations, _enumObservations | SimulationHost owns publication-source builders. Stage/Capture inside the physical tick reads Core cells under the committed transaction. Reset/removal disposes these host builders. Browser receives immutable typed history; P0-025 owns recipient buffers and P0-026 property bindings. No presenter reference points at the builder. |
| _physicsAssembly | SimulationHost owns the installed topology reference after compiler transfer. Body/query/publication callers run on that context using Core objects. Browser declaration capture, registry lookup, presentation targets and preview geometry become separate browser-owned registries. P0-008 implements the reference/topology split; P0-014 reads it for publication and P0-026 consumes only values. |
| _animations | SimulationHost retains only the reliable occurrence producer/outbox reference needed by RequireReady, Begin, Seal, Commit, Discard and committed publication. The existing render, oscillator feedback and PresentLightCones calls move to typed AnimationHost/Presenter endpoints; their state is not in this producer. P0-014 implements the producer split, P0-022/023/024 extract evaluator/feedback/worker and P0-025/026 implement recipient application. |
| _acoustics | SimulationHost retains only physical-event reservation/staging/commit publication. Wavefront inputs become received typed feedback with explicit stamp; browser AudioStreamPlayer resources/playback and animation wavefront state have separate recipient owners. Remove/read/capture/install callers address the appropriate endpoint by typed identity, never call through a shared SceneAcousticRun. P0-014 splits producer, P0-023/024 handle animation receipt and P0-026 owns audio/resource application. |
| _committedPoses, _compliantReads | Existing producer ownership remains Host. Their current live producer views never serve as browser histories. P0-014/018 publish copied values and P0-025 owns browser interpolation history. |

MachineWorld install (including current lines 628–632), per-tick admission/staging/commit (753–802),
part removal (224,326–330), occurrence reads/emission and render callbacks all require forward caller
replacement. Synchronous calls from physical commit into AnimationBatch capacity, scene registries,
audio or dirty-property application are deleted. Recipient acknowledgements manage transport leases,
not authoritative physics state. Core rollback checkpoints remain owner-local; host Running/lifecycle
checkpoint fields split into a host transaction participant rather than a Core participant retaining
the MachineWorld object. Scene resource restoration is driven from acknowledged construction values,
not by copying Godot fields into physical checkpoints.

## Separately compiled browser entry point

CuriousContraptions.web/Program.cs is source-reconciled, **not semantically compiled by Ownership**.
The four zero-error semantic contexts cover main and tests only. Read-only MSBuild evaluation
enumerates actual web Compile items (currently Program.cs), with web/root project configuration,
NuGet assets/generated imports and evaluated import inputs hashed in the ownership source inventory.
An added/removed compile item or changed captured input invalidates the source contract.
MSBuildAllProjects alone is not asserted to enumerate every SDK import; pinned SDK/package identity,
the captured assets/configuration and explicit web evaluation record bound this Design inspection.
Production web build/deployment qualification remains at its existing gate.

Program.Main owns args and the Engine instance in the browser context. It registers the plugins
initializer from TwoDogWebBoot, constructs Engine, calls Start, reads Tree.CurrentScene.Name and calls
Run to hand execution to the browser loop. The exception branch awaits DisposeAsync then rethrows;
the successful branch returns zero without an explicit disposal call. Do not infer disposal on
success. The async state machine can retain args, engine, builder/awaiter and exception-rethrow state;
all belong to browser-host lifecycle, not simulation or animation workers. Exact generated fields,
external loop retention and disposal behavior are future runtime proof, not census-discovered fields.

TwoDogWebBoot belongs to the main assembly and its callable initializer is already in the semantic
caller closure. Engine, GD, Tree and generated Godot state are external browser resources. The web
entry's caller edges into those APIs are explicitly reconciled here; they are not added to the
semantic graph count. P0-021/028/029/032 own failure/lifecycle, browser scheduling, deployed host and
integration proof. Web project DeepClean deletes AppBundle, so this reconciliation invokes no
Build/Clean/Publish and performs no UI action. Existing browser evidence retains its original bundle
identity and is not renewed by this document.

## Current generic Workshop execution supplement

The current managed capture covers the main and source-linked test contexts; it does not establish the complete S/A worker entry-point, JavaScript, WGSL or generated async-state ownership closure. The scoped member/source refresh uses the [all-mode observer member capture](../../verification/P0-025-map/production-observation-ownership-members-20261004.raw.json). The [preceding Diagnostic capture](../../verification/P0-025-map/generic-diagnostic-ownership-capture-20261004.json.gz) remains original evidence. The current capture records 240 sources, 832 members and zero binding diagnostics, with no source/member removals. Eight source hashes and 178 existing fingerprints were refreshed without changing their owner/rule assignments; the retained immutable motion cache adds `_tick` and `_bodyId` under the existing Protocol/ImmutableContract/P0-025 owner. The census includes Trace/test source inputs but does not enumerate WorkshopTrace storage members; the explicit S owner row below covers that omission without claiming compiler closure. The following actual owners supplement those omissions; they do not increase the compiler census or claim whole-repository qualification.

| Current source boundary | Owner and transfer rule |
| --- | --- |
| `CuriousContraptions.Simulation/Program.cs` | SimulationHost owns the session master origin, command/schedule installation and operation generations, bounded receipt state and worker lifecycle. These fields stay in S. B/A receive validated value copies and never replace S's physical authority. Dispose retires installation ownership before late continuations can commit. In both Production and Diagnostic builds, S also owns the native timestamp locals spanning Advance→EmitRead; they are observation-only critical-path wall time, not numerical state or a second clock authority. |
| `CuriousContraptions.Simulation/WorkshopGpuDevice.cs` | The S host exclusively owns transport/device lifetime, committed generic bytes and capture latches, and one staged candidate. GPU candidate validation precedes latch consumption; exact-sequence Commit replaces both synchronously. Discard/failure retires only the candidate. Source-link compilation in tests is not proof of the actual worker's generated continuation fields. |
| `engine/gpu/physics.wgsl` and the generated `PhysicsGpuAbi` preamble | Simulation numerical authority owns the immutable admitted scene and staged body/support/residence/motion-piece state. One invocation writes one bounded candidate, with no cross-context shared mutation or catalogue identity dispatch. Body/collider/material/sensor IDs and numerical parameters are data. Typed failure prevents publication; B only samples committed pieces for rendering. |
| `CuriousContraptions.Animation.Worker/Program.cs` | AnimationHost owns the A clock mapping, cadence/pulse state, reliable pending controls and generic AnimationBatch instance. The batch/evaluator owns animation state; typed targets bind presentation properties without physical authority. UI tracks use continuous master time; capture opacity endpoints are additionally admitted against the selected committed world occurrence in B. |
| S/A `wwwroot/worker.js` | Each worker owns its ports, WASM instance and bounded publication/receipt slots. S additionally routes the C#-owned stopped/sealed observation flag on existing publish/acknowledgement imports; ordinary Running read receipts do not call the observation export. Clock messages travel on the transferred S/A port. Latest-value publication may coalesce; reliable command/control ownership and exact receipts remain bounded. Termination/rejected bootstrap closes transferred resources. JavaScript routes bytes and WebGPU work; it contains no alternate numerical law. |
| `CuriousContraptions.web/wwwroot/workshop-client.js`, `native-clock.js` and isolation workers | The browser host owns worker handles, transferred buffers, startup/disposal and native timestamp acquisition. Physical/animation payloads are immutable received values. Browser transport failure retires the owned session; no fallback clock or local physical stepping is admitted. |
| `BrowserWorkshopClient`, `WorkshopPoseHistory`, `MachineWorld.Gpu.cs` and `BasketPart.ApplyHalo` | B owns bounded histories, prepared installation/read tokens, frame admission and the sole scene-property writers. Pose and halo consume the same selected committed identity; missing adjacent motion/discrete state holds the prior complete display. Halo application is a binding, not a Receiver timer/evaluator. |
| `CuriousContraptions.Simulation/WorkshopTrace.cs` | S owns 7,200 raw binary64 native-clock millisecond durations and a 1,024-byte encoding scratch buffer in both modes (58,624 retained payload bytes). Diagnostic additionally owns the compact pose/angular/capture projection, totaling 980,352 payload bytes; this is not complete managed/resident occupancy. Begin owns the encoded construction/session/projection and profile, flushes a prior sealed capture before buffer reuse and before a new RunLoop, then resets run ownership. Missing/duplicate/negative/nonfinite/out-of-range timing prevents Complete. `CanFlush` and `Flush` share the same typed stopped-phase predicate; the JS transport requires the pending signal plus zero active requests, pending receipts and queued reads. Schema-v2 output preserves original timing values and terminal reason. Observation failure cannot mutate a physical candidate. Source-linked tests and payload proof remain distinct from omitted worker continuation/retention coverage. |

The current refresh preserves existing owner/rule/migration assignments while updating captured identities, adds the scoped generic declarations and their actual callers, and removes the retired specialized law/table entries. Remaining unassigned geometry/animation members and compiler omissions are explicit P0-004/P0-007/P0-022/P0-024 obligations; an absent main-context call does not prove that A cannot use a member. Required global audit failures retain their owning scopes. Independent [generic runtime review](../../verification/P0-025-map/generic-physics-support-review-20261003.json) binds actual source, input and artifact applicability; this supplement is ownership reconciliation, not a new runtime or numerical Pass.

Current global audit limits remain explicit: the managed capture contains 240 source entries, all present in the 1,901-entry historical ownership view; 1,661 extra entries include 941 generated Ownership-tool sources and 720 prior/unshipped entries. Source membership therefore fails under P0-004; this slice does not delete unrelated assignments. Fifty-six captured members remain unassigned. The capability declaration cutover names current generic owners only for the affected subset; other broad rows still require their existing owners to reconcile unshipped CPU declarations, including Buoyancy’s obsolete BallPart numerical attribution. A refreshed render-adapter hash is not proof of buoyancy or of any unadmitted catalogue mode. Inventory/catalogue statuses and the unrelated solar_shadow/shade drift remain unresolved under their existing catalogue owners.
