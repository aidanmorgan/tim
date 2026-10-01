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
