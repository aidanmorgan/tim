# Current presentation bindings

This is the binding contract between the separate animation worker, the physics worker's committed results and the main-thread renderer under [canonical IEEE-754 f32 game values and WASM SIMD physics](gpu-f16-physics.md#compilation-model) and the [engine contracts](engine-contracts.md#general-data-driven-engines). Every presentation property of every element has exactly one declared binding in that element's declaration data: either a committed physical pose/state read directly from physics (descriptors 6–8 below) or an animation sample produced by the shared evaluators in the animation worker from the declared feedback below. There are no per-element animation evaluators or update loops; a new element adds bindings, art and curve values only. Slice ANIM-1c deleted the last legacy evaluators (git history is their archive). Descriptor, feedback and event semantics below are binding; revised f32 ABI offsets and byte-length formulas belong to P0-016 before a variant is admitted. Explicit integers, identity tags and approved palette identities retain their correct types. Every numeric value/intermediate must satisfy the current admitted scale/range; platform widening is a declared adapter only.

Registrations select reusable typed evaluator/feedback and final-writer capabilities. Descriptor and instance IDs identify bindings/state; they never select element-specific code. Class names in the feedback table name the retired consumers whose behaviour each declared kind carries; those classes were deleted in ANIM-1c and the names are historical traceability only, never a dispatch permission. Element-specific shapes, curves, colours and resources are declarative inputs to shared evaluators and adapters.

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
alpha-enabled B, payload length U32. RGBA is four finite canonical f32 values; alpha in[0,1],
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
| 5 ScalarExtent | axis E,anchor canonical f32,minimum scale canonical f32,initial fraction canonical f32; Spatial; exclusive transform+visibility |
| 6 CommittedRotation | axis E; Spatial; exclusive transform |
| 7 CommittedColour | 0; Canvas/Material; exclusive RGBA |
| 8 PhysicalPose | reference kind E(Fixed=1,Body=2),reference BodyId U64(0 only Fixed),reference offset Pose,read space E(World=1,Reference=2,Relative=3),map kind E(Rigid=1,AxisAffine=2),map bytes U32,variant; Spatial; exclusive transform |
| 9 Wavefront | pattern E(Cone=1,Omnidirectional=2),minimum distance canonical f32,initial radius canonical f32,radius per distance canonical f32,thickness canonical f32,opacity canonical f32,response E(Uniform=1,Strength=2),capacity U32; WavefrontGroup; browser ring/mesh/material registry owns resources |

PhysicalPose Rigid map is offset Pose. AxisAffine map is axis E+rotation Q+position offset V3+
position gain V3+scale offset V3+scale gain V3. Position offset metres, position gain
dimensionless, scale offset dimensionless, scale gain inverse metres; reference transforms are metres/
radians. Read-space/reference semantics are exactly ScenePoseMap.Read/Map and PoseReferenceBinding:
Body reference composes its current pose with offset; Relative applies inverse reference.
The construction Pose and original Affine are retained as local registration values following either map variant, to preserve ScenePoseMap's exact construction-transform special case.
No quaternion conversion discards an anisotropic captured baseline or collider basis.

Axis tags X=1,Y=2,Z=3 are distinct compiler-checked rotation/translation/extent/pose-map enums,
even though their boundary numbers agree. Validate the actual enum type through all callers.
ScalarExtent anchor is metres within its admitted local-coordinate range; minimum scale is strictly positive, normal-f32 and at most 1; initial fraction is in [0,1]. Freeze the actual supported scale/coordinate bounds before admission; reject subnormal/overflowing or unrepresentable values explicitly.
At fraction f, initial=max(minimum,initialFraction), requested=max(minimum,f);
axis scale=requested/initial and local origin offset=baselineBasis*axis*anchor*(initial-requested)/initial.
For Y, anchor=-.5,initial1,minimum.1,f=.25, the Y basis scales.25 and local origin shifts-.375m.
Invalid axes, zero minimum and fractions outside[0,1] reject before any target write.

Colour endpoints are finite with equal baseline alpha; blend changes RGB, opacity independently
changes alpha. RgbFollow exclusively owns RGB and rejects a BlendColour writer. CommittedColour
owns all RGBA and rejects any animation RGB/alpha writer. Cosmetic transform composition is
baselineBasis*axisRotation, then uniform column scale; origin is baselineOrigin plus selected local
translation axis times scalar metres (not baseline-rotated axis). Scale stays within its declared positive normal-f32/invertible range, translation
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
| 5 ScalarThreshold | ObservableId U64,QuantityUnit E,threshold canonical f32,drive E |
| 6 BooleanObservation | ObservableId U64,drive E |
| 7 ScalarMap | ObservableId U64,QuantityUnit E,inputFrom canonical f32,inputTo canonical f32,mapping E(Linear=1,SineCycle=2); SceneScalarRotationAnimation |
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
are the retired SceneAnimationRun mapping carried forward as declared data, not behavior inferred from a field label.
RelativeAngularVelocity projects the authoritative body/reference angular velocities into the
reference pose's chosen axis and applies the direction sign; cosmetic integrated phase stays A-owned.
Spectral inputs must be nonnegative GameOpticalPower, a finite total within the admitted f32 optical-power range; active zero total
rejects, inactive uses the explicit captured colour; same OpticalColours.BeamInk mapping as source.

Committed browser-only mappings remain separate typed local registrations, not animation state:
SceneScalarRotation stores ObservableId,unit,inputFrom/inputTo,angleFrom/angleTo (canonical f32 radians),
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

Pose batches (the retired ScenePoseBatch/ScenePoseMap behaviour) bind descriptor8; all three read spaces and both map kinds are retained as declared data.
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
EventStreamId U64,EventSequence U64,emission tick U64,minimum distance canonical f32. It references the exact
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

## Declared cosmetic curves (ANIM-1b) and UI bindings (ANIM-1c)

Every currently playable animated part declares its cosmetic curve as typed data next to its physics
record and nowhere else. `CosmeticCurveDeclaration` (`engine/gpu/WorkshopCosmetic.cs`) carries
`Source` (`AnimationFeedbackSource`: None=0, Activation=1, Timer=2, ContactWork=3, Capture=4), `Curve`
(`AnimationCurve`), `Duration` (Half seconds), `ImpulseCurve` (`AnimationImpulseCurve`) and `Overlap`
(`AnimationImpulseOverlap`). `Validate` rejects unknown members and any field a source does not use.
The per-part constants live in `CosmeticCurves`: ImpactSwitch and SignalLamp (Activation, SmoothStep,
0.16 s), Delay (Timer, Linear; the duration is the committed Started..Due interval), PinballBumper
(ContactWork, 0.32 s, SineSquaredPulse, SaturatingSum) and Receiver (Capture, SmoothStep, 0.5 s: the
halo albedo blends from `#bdf4bd` to white while the capture sensor is latched). Each `IWorkshopInstance`
exposes `Cosmetic`; it is never persisted, so the save format and the physics wire are unchanged.
Basketball, Ramp and Wall declare `None`, and a part artwork that binds visuals without a declared curve
is rejected at construction, as is artwork whose declaration differs from its instance's.

Evaluation path, in order and with nothing evaluated on the main thread: committed physics read →
`BrowserWorkshopClient` resolves the owning instance's declaration by body id and sends one
`WorkshopAnimationControl` (Version 5; bytes 92–93 `ImpulseCurve`, 94–95 `Overlap`, both zero unless
the kind is Impulse) → the animation worker evaluates the shared clip or impulse slot → the 60 Hz
sample returns as a 0..1 blend → `TryCosmeticFrame` hands a `WorkshopCosmeticSample` (blend, timer
phase) to `MachinePart.ApplyCosmetic`, whose single binding list drives `WorkshopVisualBinding`.
Capture feedback resolves the latched sensor to its owning Receiver through the compiled scene's residence
sensor declarations (the sensor's frame body; free play compiles one sensor per ball, so the authored id is
never assumed) and sends a ColourBlend Endpoint that the worker ramps 0→1; one receiver owns one capture
target per world, and a rejected or re-captured occurrence releases its slot for a fresh request.
`WorkshopAnimationWire.TargetCapacity` grew to 42 (2·activation + contact + one capture target per sensor + two
UI targets); that sizes the worker's register table only — the Version 5 byte layout is unchanged.

Animation target identities (all declared, none element-keyed):

| Range | Target | Source |
| --- | --- | --- |
| `1` | Hint (`UiCurves.Hint`, Opacity) | player control: Reveal / Hide / Visibility |
| `node + 2` | activation part | `AnimationFeedbackSource.Activation` |
| `2^32 + node` | timer part | `AnimationFeedbackSource.Timer` |
| `2·2^32 + body` | contact owner | `AnimationFeedbackSource.ContactWork` |
| `3·2^32 + body` | capture receiver | `AnimationFeedbackSource.Capture` |
| `2^64 − 1` | Goal (`UiCurves.Goal`, Opacity) | committed goal occurrence |

UI bindings (ANIM-1c) use the same worker path as parts. `UiCurveDeclaration` (`engine/gpu/WorkshopUi.cs`)
declares one `WorkshopUiTarget` (Hint=1, Goal=2), its `WorkshopUiFeedback` (Control or Goal), curve, duration
and neutral opacity (hint 1, goal 0). `IWorkshopClient.ControlUi` queues a Reveal/Hide/Visibility request
for a Control-fed target and `PumpUiControls` sends it only when the single animation lease is free (a
newer Reveal/Hide supersedes anything queued for the target; a Visibility change replaces the queued one),
so pressing the hint while a Delay or any part control is in flight never throws. `IWorkshopClient.TryUiFrame`
returns one `AnimationOpacity` per declared UI target per admitted display frame and `WorkshopUiBinding`
(`engine/WorkshopUiBinding.cs`) is the only Modulate writer; `Workshop.ApplyUiBindings` is the one UI apply
loop and prints `CCGOAL_SOLVED <epoch>` once per world when the goal binding first samples. The hint's Hide
registers a neutral (1) clip and hides the label; the worker keeps publishing that neutral every pulse until
the session ends, as for every registered target.

Animation channel ABI, Version 5 (`WorkshopAnimationWire`; little-endian; both records are 144 bytes):

| Bytes | Control (`ControlBytes`) | Output (`OutputBytes`) |
| --- | --- | --- |
| 0–15 | session | session |
| 16–23 | master generation | master generation |
| 24–31 | cadence revision | cadence revision |
| 32–39 | Sequence | Generation |
| 40–47 | Generation | ordinal (ACK: Sequence; Sample: animation pulse) |
| 48–51 / 48–55 | Kind U32 (48), Visible U32 (52) | AppliedAt I64 |
| 56–63 | Target U64 | Value Half (56), Property U16 (58), kind U32 (60) |
| 64–71 | World U64 | Target U64 |
| 72–79 | From/To/Duration Half (72/74/76), Property U16 (78) | World U64 |
| 80–87 | Curve U32 (80), EventOrdinal U32 (84) | EventOrdinal U32 (80), EventPhase Half (84), Version U16 (86) |
| 88–95 | EventPhase Half (88), Version U16 (90), ImpulseCurve U16 (92), Overlap U16 (94) | zero padding U64 (88) |
| 96–131 | Timer: Started/Due/Observed/InputEmitter U64 (96/104/112/120), Phase U32 (128) | same timer layout |
| 132–143 | zero padding U16 (132), U16 (134), U64 (136) | PulseDuration Half (132, impulse only), zero padding U16 (134), U64 (136) |

Readers reject any non-zero padding, a wrong Version, undefined enum members, an impulse envelope on a non-impulse control, and a PulseDuration outside (0, 30] or carried without a ColourBlend world target or alongside a timer observation.

Impulse overlap is worker-owned: the first Impulse control for a target registers one
`AnimationImpulseDefinition` from the declared envelope (`WorkshopAnimationWire.ImpulseCapacity`
occurrences, Presentation clock, EventTime timing) and every later committed occurrence enqueues by
its occurrence sequence; a target cannot change its envelope within a world, a retransmitted
occurrence is not enqueued twice, and an exhausted slot drops the occurrence with a logged line and
no fault. Timer feedback sends controls only for Counting and Finished intervals; Ready is the
declared neutral. Bindings are keyed by an optional `AnimationTimerPhase`: a phase binding writes
its declared value whenever the sampled phase matches, so the Delay indicator palette is data, not a
branch. Reset applies `WorkshopCosmeticSample.Neutral` (blend 0, Ready) to every binding and the
worker retires old-world slots on commit; non-finite or out-of-range blends keep the last committed
values. ANIM-1b deleted `AnimationPulseSegment`, `AnimationTimerFrame`/`TrySample`, the
main-thread pulse summation, the three per-channel binding lists and the `BindContactWork`/
`BindTimer*` entry points. ANIM-1c deleted the Receiver `ApplyHalo` lerp and its `WorkshopPartKind.Receiver`
presentation branch, the per-target `ControlHint`/`TryHint`/`RecordHintPresentation`/`TryGoalOpacity`/
`TryCaptureOpacity`/`RecordCapturePresentation` client members, `WorkshopHintSample`, the goal label and
hint `Modulate` writes in `ui/`, and the uncompiled evaluators `engine/presentation/ScalarExtentDefinition`,
`SceneAnimationRun`, `SceneAnimationAdapter`, `SceneAcousticRun`, `SceneAcousticMotionRun`,
`SceneAcousticWavefrontRun`, `SceneEnumBinding`, `SceneLightConeRun`, `SceneOccurrenceRun`,
`SceneOpticalPreviews`, `SceneOscillatorFeedback`, `ScenePoseAsset`, `ScenePoseBatch` and
`ui/LightConeVisual`, `OpticalPathVisual`, `MechanicalBeltVisual`, `RopeVisual`, `PulleyRopeRoute`,
`TubePlacementSnap`, `ConnectionChoice`. The six `engine/presentation/Animation*.cs` evaluators remain the
shared worker core.
