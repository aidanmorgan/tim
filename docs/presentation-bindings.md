# Current presentation bindings

This is the current presentation contract under [canonical Half values and WGSL physics](gpu-f16-physics.md) and [engine lifecycle/protocol requirements](engine-contracts.md). Descriptor, feedback and event semantics below are binding; revised Half ABI offsets and byte-length formulas belong to P0-016 before a variant is admitted. Explicit integers, identity tags and approved palette identities retain their correct types. Every numeric value/intermediate must satisfy the current admitted scale/range; platform widening is a declared adapter only.

Registrations select reusable typed evaluator/feedback and final-writer capabilities under the [general-engine contract](engine-contracts.md#general-data-driven-engines). Descriptor and instance IDs identify bindings/state; they never select element-specific code. Source class names below trace migration consumers, not a dispatch permission. Element-specific shapes, curves, colours and resources are declarative inputs to shared evaluators and adapters.

## Browser-owned sealed descriptor registry

BindingDescriptorId is a nonzero U64 stable identity allocated monotonically by the browser for
one session/world generation (or the separate UI animation generation). It is never a target slot.
A descriptor is immutable after registration; its SHA256 covers the exact canonical value record
below, including every parameter. Browser registry maps (generation,descriptor ID,hash) to this
value record and separately maps AnimationTargetId to a live Godot object.
Godot objects, node paths, material instances, parent nodes and baseline engine resources never
leave this browser registry. The pure descriptor contains no delegate, Type name or string selector.

Topology references descriptor ID+SHA256 supplied by the browser capture/compiler; simulation
only echoes those opaque captured values. It never executes or mutates a presentation descriptor.
AnimationHost likewise need not dereference final renderer metadata: its registrations explicitly
name target/property and evaluator/feedback values. It retains the descriptor identity/hash for
result/sample binding validation. Browser rejects unknown/stale/mismatching descriptors before
any property write. Standalone UI registration uses the UI generation and does not invent a world.
The Hello asset hash still binds immutable authored assets; it is not a substitute for this
per-generation captured descriptor identity. No late mutable registry lookup may change semantics.

Descriptor canonical prefix: DescriptorKind E, TargetKind E(Spatial=1,Canvas=2,Material=3,WavefrontGroup=4),
Removal E(RestoreBaseline=1,Detach=2), baseline Affine, baseline RGBA, baseline visible B,
alpha-enabled B, payload length U32. RGBA is four finite canonical Half values; alpha in[0,1],
with approved palette identities preserved through the declared colour adapter. Spatial requires baseline RGBA=(0,0,0,0); Canvas/Material
require identity baseline Affine. WavefrontGroup retains both baseline affine and RGBA values. Baseline visible is the captured original target visibility.
Material opacity requires alpha-enabled=true and unchanged alpha material capability at application.
Hash covers the exact admitted canonical encoding, including every parameter. The descriptor bytes are local browser capture/
validation material, not an additional worker message or a second renderer implementation.

| DescriptorKind tag | Required fields / valid target / final writer |
| --- | --- |
| 1 CosmeticTransform | rotation axis E + translation axis E; Spatial; scalar Rotation/Translation/Scale channels only |
| 2 BlendColour | from RGBA + to RGBA; Canvas/Material; scalar blend plus independent opacity |
| 3 RgbFollow | 0; Canvas/Material; three RGB channels plus optional independent opacity |
| 4 Opacity | 0; Canvas/Material; alpha only, baseline RGB |
| 5 ScalarExtent | axis E,anchor canonical Half,minimum scale canonical Half,initial fraction canonical Half; Spatial; exclusive transform+visibility |
| 6 CommittedRotation | axis E; Spatial; exclusive transform |
| 7 CommittedColour | 0; Canvas/Material; exclusive RGBA |
| 8 PhysicalPose | reference kind E(Fixed=1,Body=2),reference BodyId U64(0 only Fixed),reference offset Pose,read space E(World=1,Reference=2,Relative=3),map kind E(Rigid=1,AxisAffine=2),map bytes U32,variant; Spatial; exclusive transform |
| 9 Wavefront | pattern E(Cone=1,Omnidirectional=2),minimum distance canonical Half,initial radius canonical Half,radius per distance canonical Half,thickness canonical Half,opacity canonical Half,response E(Uniform=1,Strength=2),capacity U32; WavefrontGroup; browser ring/mesh/material registry owns resources |

PhysicalPose Rigid map is offset Pose. AxisAffine map is axis E+rotation Q+position offset V3+
position gain V3+scale offset V3+scale gain V3. Position offset metres, position gain
dimensionless, scale offset dimensionless, scale gain inverse metres; reference transforms are metres/
radians. Read-space/reference semantics are exactly ScenePoseMap.Read/Map and PoseReferenceBinding:
Body reference composes its current pose with offset; Relative applies inverse reference.
The construction Pose and original Affine are retained as local registration values following either map variant, to preserve ScenePoseMap's exact construction-transform special case.
No quaternion conversion discards an anisotropic captured baseline or collider basis.

Axis tags X=1,Y=2,Z=3 are distinct compiler-checked rotation/translation/extent/pose-map enums,
even though their boundary numbers agree. Validate the actual enum type through all callers.
ScalarExtent anchor is metres within its admitted local-coordinate range; minimum scale is strictly positive, normal-f16 and at most 1; initial fraction is in [0,1]. Freeze the actual supported scale/coordinate bounds before admission; reject subnormal/overflowing or unrepresentable values explicitly.
At fraction f, initial=max(minimum,initialFraction), requested=max(minimum,f);
axis scale=requested/initial and local origin offset=baselineBasis*axis*anchor*(initial-requested)/initial.
For Y, anchor=-.5,initial1,minimum.1,f=.25, the Y basis scales.25 and local origin shifts-.375m.
Invalid axes, zero minimum and fractions outside[0,1] reject before any target write.

Colour endpoints are finite with equal baseline alpha; blend changes RGB, opacity independently
changes alpha. RgbFollow exclusively owns RGB and rejects a BlendColour writer. CommittedColour
owns all RGBA and rejects any animation RGB/alpha writer. Cosmetic transform composition is
baselineBasis*axisRotation, then uniform column scale; origin is baselineOrigin plus selected local
translation axis times scalar metres (not baseline-rotated axis). Scale stays within its declared positive normal-f16/invertible range, translation
stays within the declared local-coordinate range, and the final declared platform-adapter transform must remain finite/invertible. Physical/Extent/CommittedRotation
exclude all cosmetic transform writers on the same object, including ancestors of functional
geometry. Physical parent plus separate cosmetic child is permitted in that declared order.
One central adapter writes each final target once after validating the complete outgoing batch.

## Feedback and current caller closure

Browser captures typed declarations and resolves every scene key to stable IDs before registration.
AnimationHost receives an explicit Feedback record with each evaluator registration:
FeedbackKind E, payload length U32, exact variant below. IDs refer to installed committed subscriptions.
No default key, missing channel, unit coercion, scene callback or stale publication is accepted.
The final browser descriptor above stays local; this feedback record is value-only and crosses B→A.

| FeedbackKind tag | Required fields / current source mapping |
| --- | --- |
| 1 Autonomous |0; WorkshopAnimation and autonomous SceneAnimationSignal |
| 2 OwnerActive | Boolean ObservableId U64 + drive E; owner activity copied as typed Boolean observation |
| 3 CounterThreshold | scalar ObservableId U64,positive threshold U32,drive E; exact nonnegative integer count <=Int32.MaxValue, Dimensionless |
| 4 ElectricalInput | Boolean ObservableId U64,drive E; specific compiled input-availability reading |
| 5 ScalarThreshold | ObservableId U64,QuantityUnit E,threshold canonical Half,drive E |
| 6 BooleanObservation | ObservableId U64,drive E |
| 7 ScalarMap | ObservableId U64,QuantityUnit E,inputFrom canonical Half,inputTo canonical Half,mapping E(Linear=1,SineCycle=2); SceneScalarRotationAnimation |
| 8 RelativeAngularVelocity | BodyId U64,reference BodyId U64,source axis E,direction E(Forward=1,Reverse=2); source velocities/reference poses must be subscribed |
| 9 Occurrence | EventStreamId U64,source CapabilityId U64,direction E; SceneOccurrence and acoustic oscillation bindings |
| 10 SpectralChannel | red/green/blue ObservableIds (24),owner-active Boolean ObservableId U64,inactive RGBA,channel E(Red=1,Green=2,Blue=3); three registered RGB followers |
| 11 ColourFollow | Boolean ObservableId U64; OwnerActivity or explicit Boolean captured to the named Boolean subscription |

Drive tags StartStop=1,Endpoint=2, validated against the evaluator's supported mode.
InputFrom/InputTo must be finite, InputTo > InputFrom, and their difference finite. This same
increasing finite-range rule applies to ScalarMap feedback, committed scalar rotation and scalar
extent. Rotation AngleFrom/AngleTo and their difference must be finite; decreasing or equal angle
outputs remain supported. Evaluator endpoints and their difference retain each family’s existing
finite/output-domain validation. No reversed input range or overflow denominator is accepted.
Source unit exact; range saturation and SineCycle
are the current SceneAnimationRun mapping, not behavior inferred from a field label.
RelativeAngularVelocity projects the authoritative body/reference angular velocities into the
reference pose's chosen axis and applies the direction sign; cosmetic integrated phase stays A-owned.
Spectral inputs must be nonnegative GameOpticalPower, a finite total within the admitted f16 optical-power range; active zero total
rejects, inactive uses the explicit captured colour; same OpticalColours.BeamInk mapping as source.

Committed browser-only mappings remain separate typed local registrations, not animation state:
SceneScalarRotation stores ObservableId,unit,inputFrom/inputTo,angleFrom/angleTo (canonical Half radians),
axis; SceneScalarExtent stores ObservableId,unit,inputFrom/inputTo and visibility enum Always1/
OwnerActive2 plus owner-active Boolean ID; TimerColour stores timer-phase Enum ObservableId and
three RGBA palette values in Ready1/Counting2/Finished3 order; SceneEnumRotation/Colour stores
ObservableId,ObservationEnum and a complete explicit enum→angle/RGBA map (no missing/unknown member).
These records use the corresponding descriptor writer kind. Their source values come from one
complete physical publication; browser mapping is cosmetic and never changes physics eligibility.
Timer phase is ObservationEnum2 (CannonPhase remains1). Counter count/elapsed/fraction/input activity
are explicitly registered scalar/Boolean observables; their meaning/unit is compiler-checked,
not an arbitrary property-name query. Committed scalar/enum mappings apply directly at a coherent
display sample without becoming independently integrated cosmetic state.

ScenePoseBatch/ScenePoseMap binds descriptor8; all three read spaces and both map kinds are retained.
Clip/follow/impulse/oscillation rotations and translations bind descriptor1 with independent axes;
scale shares only its cosmetic transform owner. BindColour/FollowingColour/ImpulseColour bind2;
BindFollowingRgb binds3; opacity binds4 or its permitted independent alpha channel.
ClaimScalarExtent binds5; scalar/enum committed rotation binds6; timer/enum colour binds7.
Acoustic wavefront rings bind9 with immutable ring target identities in the browser registry and
typed acoustic event data (including direction/pattern) rather than hidden simulation callbacks.
Acoustic audio targets retain tone→resource maps in the same browser registry; audio consumption
acknowledges admission, not guaranteed audibility. The source's finite wavefront lifetime,
visibility/present-once and capacity rules remain required P0-023/026 consumers.

Register all channels atomically: validate descriptor hash, actual target kind/resource lifetime,
source topology/units/modes, exclusive claims, complete-output reservation and all boundary ranges
before publishing a successful registration. Failure changes neither registry, evaluator counts nor
claims. Removal invalidates descriptor/target references and cancels pending occurrences; RestoreBaseline
restores exact captured transform/colour/visibility, Detach only unbinds. Reset restores then invalidates
the generation. Freed/changed node, parent, material or alpha capability rejects; never retarget a reused
slot or silently recreate a target. Required proofs include each axis, both colour endpoints, RGB+alpha
versus exclusive-colour control, extent boundary, pose-map/read-space variants and exact restoration.

## Wavefront occurrence binding and complete scalar output

A wavefront uses AnimationProperty.PropagationDistance=9, metres, one output per admitted occurrence
group (one cone ring or three omnidirectional planes). Descriptor9 consumes that scalar to derive
the final ring transforms, radii and opacity; the browser never integrates its phase.
For distance d: cone center=origin+direction*d, omnidirectional center=origin; radius=initialRadius+
d*radiusPerDistance; inner/outer=radius∓thickness; alpha=opacity*(Strength mode?strength:1)*(1-d/8).
Visible iff owner presentation visibility and minimumDistance<=d<8. Cone orientation maps local Up
to immutable event direction; omnidirectional planes keep captured orientations. Validate full
outgoing transforms/mesh bounds before writes; immutable event origin/direction/strength/tone are
captured from the same identified committed occurrence, never from interpolated physics.

Browser consumes the reliable occurrence into its bounded ring-group registry, selects the smallest
free stable group identity and registers one A-owned Clip (From0,To8,Duration=8/12,Once,Linear,
Simulation clock) with FeedbackKind WavefrontDistance=12. Payload is world generation I64,
EventStreamId U64,EventSequence U64,emission tick U64,minimum distance canonical Half. It references the exact
S→A occurrence; A waits within its bounded occurrence reservation until both arrive, validates
identity/payload and starts at emissionTick/120. No second cosmetic request producer is introduced.
The descriptor's group resources and immutable event values are local browser registration state;
the per-generation descriptor ID/hash changes for each new occurrence binding. Reusing a freed
ring group never reuses the old instance/descriptor identity.

The authored acoustic visual definition supplies From=0, To=8 metres, Duration=8/12 seconds and Linear/Once to the shared occurrence-driven clip evaluator; the 12 m/s speed is derived from those typed values. FeedbackKind WavefrontDistance=12 binds occurrence identity and present-once lifecycle, not an acoustic-part-specific evaluator. Other admitted instances use the same clip algorithm with their validated declarations; this does not add new ABI variants or change the following fixed acoustic acceptance.

For this declaration, the shared evaluator produces distance=min(8,max(0,(simulationTime-emissionTick/120)*12)).
If it reaches8 before an actual visible presentation, A emits minimumDistance until presentation
acknowledgement, preserving the existing present-once behavior. AnimationAction Presented=12
has world generation I64,stream U64,event sequence U64 and is accepted only for this
instance's matching occurrence after B applies a visible sample. B uses its ordinary reliable
request sequence; duplicates cannot increment presented counts twice. Presented records the first
actual visibility; it never changes emission time or speeds up phase. The two transitions are:
- Before natural distance reaches8, a matching Presented receipt marks the occurrence presented
  and propagation continues at12m/s. The mathematical distance at 0.1 s is 1.2 m; after that receipt 0.2 s gives 2.4 m,
  and 0.65 s gives 7.8 m, quantized within the current declared error budget. It emits 8 (hidden) and completes only at simulation age>=8/12s.
- At age>=8/12s without a matching receipt, retain the same bounded occurrence/instance and emit
  minimumDistance. A matching receipt received in this expired/held state permits the next
  evaluation to emit8 (hidden) and complete. At0.7s an unseen wave is held, never auto-completed.
A delayed receipt is evaluated against current simulation age: if still below range, continue;
if now expired, finish. Lost or delayed receipts leave the same held reservation occupied, never
allocate another instance, silently drop the admitted occurrence or discard its first-visible proof.
Reliable retransmission uses the existing request identity/result rules; stale instance/occurrence
receipts remain rejected. Hidden owners cannot send Presented; their unseen expired waves remain
held until actual visibility or explicit Remove/Reset. Only after completion does B remove the
instance and release its group after the committed removal result.
Hidden groups still count against2516 outputs, occurrence and pending-request byte reservations.
Capacity rejection cannot discard an admitted event or allocate extra rings. The12m/s and8m values
are the current acoustic visual contract, not a replacement propagation law. P0-023/024/026 must
prove source-equivalent cone/omnidirectional, Uniform/Strength, hidden/late first presentation and
exact radius/material/transform restoration. All phase/lifetime state lives in AnimationHost/kernel;
browser owns final mapping, captured resources and the visible-presentation receipt only.

## Sample incarnation and activation

Each sample carries AnimationInstanceId as well as target/property. Within an animation generation,
B allocates strictly increasing instance IDs; no successful, failed or removed identity is reused.
A accepts a new instance only above its allocation high-water mark; exact request retransmission
uses the retained request result and does not register again. Exhaustion requires explicit restart.
The instance has exactly one immutable target/property/descriptor ID/hash/evaluator/feedback tuple.
A whole RGB/group registration reserves all of its instances atomically; failure activates none.
The high-water mark still burns attempted IDs, so rejected IDs cannot later alias delayed traffic.

B keeps pending and active registrations within the existing bounded registration/request quotas.
Only the matching successful registration result activates the pending instance. Until then samples
for it are discarded (never buffered for later replay). A's next complete sample supplies its value;
wavefront present-once remains held until an actual visible sample is acknowledged. Failed
registration releases pending metadata and never changes the prior active binding. Re-registration
requires prior removal completion; it cannot overwrite an active target/property claim.

When B admits Remove, it immediately retires that instance for property writes and discards its
retained samples; it keeps only the bounded pending request/descriptor metadata until the result.
A commits removal atomically before producing the successful result and releases its output claim.
A failure leaves the binding retired in B until explicit lifecycle recovery, never auto-reactivated.
A duplicate control/result cannot activate, remove or release a newer instance. Reset invalidates
all old-generation samples/pending activation before restoring baselines. No per-retired-instance
unbounded tombstone list: monotonic allocation plus the active/pending maps is sufficient.

At capture A emits the exact immutable instance identity. Before applying any record B matches
session, animation generation, active instance, target/property and that instance's captured
descriptor ID/hash; unknown/retired/mismatched records are discarded without resolving through a
new target/property binding. Within each generation, accepted sample envelope sequence strictly
increases; delayed/reordered older samples cannot roll state back. Sample publication sequence
order MUST match capture order: a retained older capture cannot receive a later sequence than a
newer capture. Records are sorted by instance ID; duplicate instances or duplicate target/property
claims reject the envelope. Each envelope is captured from
one atomic animation state after committed controls; all records are validated before any write.
Retained complete histories contain the original identities, and are filtered on retirement, never
retargeted. New registrations may coexist with discarded old records in transit, not in an applied
atomic state. Byte quotas include pending/active maps and histories; there is no new queue.

Wavefront ring reuse follows the same rules. Presented controls identify the instance in the
existing command prefix AND the occurrence's world generation/stream/sequence payload. A accepts
Presented only when both equal the active immutable feedback12 registration. An old presented
receipt, late scalar sample or late registration result cannot complete or animate a reused ring.
P0-016/023/024/026 must update codec, feedback, history, browser application and present-once
consumers together; no target/property-only sample path remains.
