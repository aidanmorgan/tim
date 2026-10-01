# P0-004 spatial/query authority closure

This corrects [R2-F01](../../verification/P0-004/review-r2.md). R1's four corrections remain intact.
Current ScenePhysicsAssembly, WorldGeometry and SceneCollisionGeometry mix construction, physics,
publication and presentation. Their names are not ownership boundaries.

## Exact authoritative chain

LinearPusherPart.ObservePhysics reads MachineWorld.CollisionGeometry, checks the selected child's
box/source/surface/opacity, creates WithChild replacement, then calls ReplaceCollisionGeometry.
ScenePhysicsAssembly.ReplaceCollider validates the slot, applies PhysicsWorld.ApplyColliderUpdates,
then registers metadata under the new CompoundGeometry key. Existing snapshots retain that geometry
key; restoration relies on recovering the exact corresponding metadata. CaptureQueryBodies,
CollisionGeometry and CapturePublicationReads reject missing metadata. Therefore _queryGeometry is
authoritative collider/query metadata, not a browser preview cache.

CaptureSweep consumes that same committed metadata; SweepSnapshot.FindOverlap/Sweep invoke
PhysicsBody, CompoundMotion, CompoundCollision, ConvexSeparation and ConvexSweep. Trace uses
PointTrace over the selected light/sound/air geometry. These are simulation query kernels.
Moving a rendered node, replacing a preview mesh or changing a browser cache cannot alter them.

## Member and lifetime split

All 69 corrected member identities are explicit in the assignment shards and source-derived policy.
These groups cover the corresponding fields, properties/indexers and nested record properties.

| Current group | Exclusive target and transformation | Implementing child |
| --- | --- | --- |
| ScenePhysicsAssembly._queryGeometry/_queryOwners | Core owns installed collider/query metadata and owner identity mapping. Replace weak scene-object keys with core-owned typed geometry identity, world generation and collider revision. Metadata retains solid/opaque geometry, child surface/source classifications and body query policy. No Node, MachinePart, BodySlot provider or browser dictionary enters the store. | P0-008 installation; P0-007 portable geometry; P0-020 exact lifetime/rollback |
| _bodies/_objects/_initialJoints/_surfaces and Bodies/Objects/InitialJoints/Surfaces | Compiler exclusively constructs the candidate; atomic installation transfers its owned arrays/topology to Core. The assignment denotes the installed owner, not a second compiler store. Host retains a same-context lifecycle reference. Runtime joint additions/removals belong to PhysicsWorld. | P0-008 |
| _declarations/Declarations/_byBody/_jointIds | Installed immutable initial declarations and typed body/joint lookup maps belong to Core. Replace scene-key dictionaries with stable entity/body/joint IDs and generation; retain no scene owners. Browser registry maps its own scene objects to those IDs separately. Constructor-local candidate maps transfer once or are discarded on failure. | P0-008 |
| _referenceBindings | Host owns immutable producer reference-frame definitions from validated installation. The current array is also read during display; split that use into a copied Protocol binding declaration owned by browser history, never a shared array. Core body lookup resolves producer reference poses only in the simulation context. | P0-014 producer; P0-025 recipient |
| _presentationPoses/_publicationReads | Host owns producer pose/publication scratch. CapturePublicationReads currently calls CapturePresentationReads; that capture becomes a host read of one committed Core revision. PresentCommitted uses a separate browser history/sample array. Browser _presenting/_presentationRemoved flags cannot gate producer capture. | P0-014 producer; P0-025 history; P0-026 final application |
| _presenter/_poseBatch/_presenting/_presentationRemoved/_presentationFrame, PresentationTargets/LastPresentation | Presenter owns Godot targets, dirty-property application and local removal/submission lifecycle. Removal sends a typed lifecycle request; it does not clear core collider metadata. | P0-026 |
| MachineWorld._physicsAssembly and PhysicsAssembly getter | Host owns installed topology access. Core consumers receive their same-context typed query/topology service. Browser consumers receive construction bindings, committed read values or stamped query responses; the public getter returning the live mixed assembly is deleted with all callers. | P0-008 and P0-014 |
| SceneCollider/SceneColliderSet/SceneBodyGeometry, including children, All/Geometry/opaque/no-envelope sets and indexer | Core owns immutable query geometry and child metadata. SceneCollider's ConvexInstance remains specialized Core geometry after P0-007 removes Godot coupling. Slot becomes stable body identity plus enum-typed query policy, not BodySlot or a captured provider. WithChild creates a new immutable candidate; successful collider replacement alone installs it. | P0-007; P0-008 installation |
| WorldBodyGeometry and WorldBodyDeclaration | Core-local geometry/pose/initial dynamics/material/participation records. Replace Owner with typed entity identity, Slot with typed body identity, and QueryGeometry with the core query metadata above. Initial scene visibility is captured once into authoritative participation; CreateBody is a core factory invoked by the compiler. Existing scene-bearing records cannot cross the wire. | P0-007; P0-008 caller/capture replacement |
| BodyDynamics | Core initial-value contract containing PhysicsMotionType, InertiaTensor, numeric mass and portable velocities. Its SolidSphere/SolidBox/CreateBody functions remain Core, not browser factories; compiler invokes them from validated numeric declarations. The record is not live-body mutation authority. | P0-007; P0-008 |
| WorldGeometry.PreparedColliderGroup and SweepSnapshot._surfaces | Core owns immutable committed query snapshots, retaining geometry/resource revisions until the snapshot is released. Set is core geometry+child metadata; Part/Slot become stable entity/body IDs. FindOverlap/Sweep and their solver calls remain Core. | P0-007 |
| WorldSweepResult/WorldOverlapResult | Core-local results use typed IDs instead of MachinePart. ConvexSeparationResult stays Core. The host copies admitted status, distance/normal/penetration and typed obstacle/child identities into a stamped Protocol result; no solver object or live metadata alias leaves Core. | P0-007 local contracts; P0-014/016 publication/encoding |
| BodySpatialState.Pose/Participation/Enabled | Protocol value read with Geometry.RigidPose and Protocol.CollisionParticipation. Enabled is an intrinsic enum comparison. Remove Transform from this portable declaration: its Godot conversion exists only in the presenter adapter. Source/member map assigns that conversion property to Presenter. | P0-007 |
| WorldGeometry.SceneGeometry/WorkbenchGeometry; SceneCollisionGeometry authored proxy arrays, envelope/radius, Bodies and HollowPrecision | Browser owns authored numeric declarations and capture caches, not installed Core shapes. Replace retained Core shape objects in these caches with typed primitive/compound declaration values and resource identities. Hollow geometry generation and shape validation run in Core/compiler; browser cache matching compares authored values only. Preserve full original affine basis during capture, including float anisotropy. | P0-007 conversion; P0-008 construction compiler |

A Core placement for immutable query records means owner-local data, not a second physical solver.
Initial authoring/candidate values transfer to installed state once. Recipient preview copies are
derived and cannot mutate installed geometry. Protocol discriminants TraceMedium, SweepBodyMode,
SweepSurfaceKind, SweepObstacleKind, WorldSweepStatus, SceneColliderSource and BodyQueryPolicy move
independently of their current containing files; Core and Presenter retain enum types.
The existing Scene* declarations are replaced forward with role-specific core query types and
browser capture adapters, without old-name aliases or retained dual-context classes.

## Replacement, rollback and snapshots

The installed metadata store is a participant in the same transaction as collider updates.
Validate new geometry, child metadata, material, participation, identity/revision and required
capacity before installation. Commit installs the collider and query metadata together; failure
restores both, their revision/identity maps, allocations and pending publication. Existing query
snapshots remain bound to the exact generation/revision they captured. A new committed collider
cannot silently change an old snapshot, and a restored collider must recover its exact prior
query metadata. Metadata/resource reclamation occurs only after the world and all retained
snapshot/publication leases release that revision. Weak-reference garbage collection is not the
cross-context identity or rollback contract.

Host publication reads committed Core pose, collider revision, query metadata, owner activity and
velocity coherently. It never reads browser visibility, preview geometry or a display sample.
Producer reference-frame resolution uses the same committed revision. Recipient history owns copied
reference bindings and poses; delayed rendering cannot mutate or block producer scratch.

## Complete caller replacement families

| Current caller family | Required target and proof |
| --- | --- |
| LinearPusherPart.ObservePhysics; MachineWorld.CollisionGeometry/ReplaceCollisionGeometry; collider checkpoint/restore | Generic actuator process and Core metadata replacement API, with typed body/child IDs. Preserve wrong-slot/type/source/opacity/participation rejection and exact geometry/material/revision rollback. P0-007/008 plus the LinearPusher CAT-039-I/CAT-039-V children and P0-020/032 own implementation/proof. |
| CannonPart clearance; WorldGeometry.Sweep/CaptureSweep; SweepSnapshot.FindOverlap/Sweep | Core query service owns captured immutable surfaces and solver invocation; Cannon CAT-016-I/CAT-016-V retain its distinct consumer proof. Ignore-owner/body filtering uses stable IDs, never scene references. Tie/reduction order uses the declared canonical typed order rather than scene traversal or local storage slots. Preserve overlap, touching, clear, non-closing, rotated/hollow/compound and stale-snapshot controls. |
| WorldGeometry.Trace; LightNetwork, OpticalNetwork, Acoustics; functional acoustic/spatial consumers | Core traces the correct medium-specific committed geometry. Preserve opaque/light/sound versus all-solid air behavior, owner exclusion and disabled participation. Basket goals, cannon loading, bell/speaker events and rope constraints read Core pose/participation; they cannot read displayed transforms. |
| SceneScalarObservation/SceneBooleanObservation/SceneEnumObservation and ElectricalNetwork publication | Host publication builders receive Core query-owner IDs from installed bindings. Stage/Capture cannot consult a browser registry. Runtime enum/scalar/electrical source cells remain Core-owned. |
| SceneAnimationRun, SceneEnumBinding, SceneLightConeRun and other scene bindings using QueryOwnerId/Body | Compiler/host publishes a typed binding table once per accepted generation; browser/animation recipients resolve their local target IDs using copies. Delete live assembly getter calls. P0-014/023/024/025/026 own these consumer replacements. |
| RopeVisual, LightConeVisual, PlaytestDiagnostics and other CaptureSpatialState display consumers | Consume committed value histories and presenter conversion; construction previews consume authored captured values. Browser trace/sweep preview requests go through stamped query commands/results, not local invocation of specialized solver code. No synchronous wait enters the nonblocking presenter. |
| CaptureConstructionBodies/CapturePhysicsBodies/CapturePhysicsAssembly, scene Geometry cache and Workbench construction | Browser captures typed authored numeric values; compiler validates/builds candidate core geometry/topology and installs atomically. Initial/query eligibility preserves the existing Visible/PhysicsOwner coupling as simulation participation. Reset/save restore acknowledged construction values and bindings, not browser cache contents. |

All other catalogue consumers retain every exact CAT-I/V variant in [catalogue-owners.json](catalogue-owners.json),
which remains the finite source-to-child mapping; shared scripts do not merge their proof obligations.

Current source controls are retained in RuntimeQueryOwnershipTests (replacement, disabled participation,
old snapshot, failed replacement and exact metadata restoration), SlidingBladeOwnershipTests,
SceneBodyGeometryTests, ActuatorBodyGeometryTests, BodyLocalBoxTests, WorldSweepTests,
WorldSweepShapeTests and CompoundWorldSweepTests. They provide source-derived invariants here;
this Design correction does not claim a fresh native or Chrome behavior pass. P0-007/008 and each
affected consumer child must compile/refactor these callers together and run their required native,
production-build and real-UI positive/control/Run/Reset/save proofs. P0-019/020/032 additionally prove
worker authority and lifecycle, and P0-034 retains integrated device/workload budgets.

The complete conservative caller graph remains the same 333,066 edges and 18,551 context identities.
The correction changes 69 placements, not current runtime code or compiler bindings. Existing R2
flow/oracle/runtime-baseline evidence is reusable only after independent applicability verification;
new policy-placement controls and the production audit check the changed owner map.
