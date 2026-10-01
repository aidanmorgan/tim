# Numerical clocks, histories and overload

No worker timer callback or render fraction defines elapsed time. All clocks use monotonic
context-local readings converted at the boundary to P0-005 nonnegative I64 nanoseconds.
Retain integer ticks; physical seconds are tick/120, four outer substeps per tick.
There is no simulation slow-motion mode. Presentation-time rate1 and simulation-time rate1
while Running are distinct domains; paused simulation time is constant.
Animation definitions retain their explicit Presentation or Simulation clock.

## Calibration and uncertainty

B maps S and A independently using the existing ClockProbe/ClockReply fields; no S↔A clock
message, new wire field or assumed common performance.timeOrigin. A needs no monotonic S clock
to interpret an authoritative tick. For probe timestamps b0,w1,w2,b3, all converted to ns:
r=(b3-b0)-(w2-w1) must be nonnegative; w2>=w1, b3>=b0.
Let q be a conservative bound on EACH probe timestamp's quantization error, frozen for both
contexts from measured/configured platform resolution,0<=q<=100000ns. Use the larger endpoint
bound when they differ. Otherwise calibration is Unsupported/Clock fault. The one normative
worker-minus-browser offset interval at probe receipt is
L=w2-b3-2q, U=w1-b0+2q. The two timestamp errors in each difference are both included.
At elapsed B time a, expand to [L-d,U+d], d=500*a/1000000. Its midpoint is (L+U)/2 and its
offset uncertainty is (U-L)/2+d; these are derived views of this same interval, never separate bounds.
For a sample worker stamp w with its OWN encoded timestamp uncertainty qs, map to browser
[w-qs-(U+d), w+qs-(L-d)]. qs is not already included in the probe errors and must be added once.
Require0<=qs<=100000ns and mapped-sample halfwidth<=1000000ns; no sample with excessive
uncertainty is displayed/qualified, even if the offset estimate alone meets1ms.

Collect8 valid probes, spaced10ms at startup, retain the most recent8 (one outstanding per peer;
lost probe expires after250ms). Select smallest current expanded offset halfwidth, newest on ties.
Reject negative raw RTT, wrong sequence/session/clock generation, timestamp reversal and duplicate
reply. Retain raw times/rejections. Refresh every250ms in foreground. The500ppm expansion is a
frozen maximum drift assumption requiring independent actual-clock qualification, not established
by fitting. At receipt compare the NEW quantization-expanded offset interval with the OLD interval
expanded to that same B receipt time; touching endpoints overlap. Disjoint intervals produce
ClockSuspect, stop interpolation and require fresh8-probe calibration. A new estimate does not
shrink/erase the recorded uncertainty of already retained samples. Never rescale physics.
At age>500ms or any mapped sample halfwidth>1ms the mapping/sample is invalid. Expired mapping
never becomes zero uncertainty. The arithmetic still needs independently measured<=1ms actual
mapping error for P0-025/032/034 qualification.

Example (ms): b0=100,w1=150.2,w2=150.3,b3=100.7,q=.01:
r=.6; raw offset[49.6,50.2]; normative offset[49.58,50.22], midpoint49.9,halfwidth.32.
At250ms drift adds.125 →[49.455,50.345], offset halfwidth.445.
For w=175,qs=.01, mapped interval[124.645,125.555], midpoint125.1,halfwidth.455.
At age0, the sample interval[124.77,125.43] contains125.41 for actual offset49.59.
Two raw point-offset probes50.0 and50.03 with q=.01 overlap after expansion
([49.98,50.02] and[50.01,50.05]);50.05 gives[50.03,50.07], genuinely disjoint
at zero elapsed drift. Exactly touching50.04 is accepted at zero drift.
b3=100.05 gives negative raw r and rejects. A sample halfwidth1.001ms rejects.
At clock counter overflow/reversal or worker restart invalidate the clock generation and entire
affected history; a new runtime session is required for producer restart. No interpolation across it.

## Wall and simulation axes

PhysicalState carries authoritative simulation time and a SimulationMonotonic capture stamp.
AnimationSamples carries AnimationMonotonic capture stamp and the existing envelope world
generation/tick/revision of its exact last complete S feedback. Standalone UI has world generation0
and tick/revision0. This assigns existing envelope fields, adding no channel or layout.
Each A evaluation uses one immutable complete physical sample plus admitted occurrence state;
Simulation-clock evaluators use that sample's tick/120 with no wall-time extrapolation.
Presentation-clock evaluators use A monotonic elapsed from their generation origin.
A may evaluate at60Hz while S ticks120Hz; missed cosmetic evaluations do not drop reliable events.

B retains both axes for each A sample. Simulation-bound records are indexed by that envelope's
feedback tick, presentation-bound records by mapped A capture time. A captures one atomic batch
after controls then feedback, preserving capture/sequence order per P0-005. Repeated same feedback
tick provides no new simulated time; B keeps the newest complete sample at that tick for
same-tick cosmetic controls. B does not relabel it as newly committed physical motion.
Actual physical wall age uses S capture time; simulation-feedback wall age additionally includes
the referenced S capture time (resolved from retained stamped metadata), never A's newer timestamp.
If the referenced S stamp is unavailable, feedback-age evidence and synchronized application are
Incomplete/pending, not zero-age. Recipient stores retain that stamp within the budgets below.
A sample's feedback revision must refer to the exact installed S topology and same world generation.

Choose one requested display wall time D=Bnow-16,666,667ns (fixed delay).
Determine the bracket of complete S samples by mapped capture time, then interpolate their
authoritative simulation times to obtain T. All physical poses and simulation-bound cosmetic
records use this same T. Simulation discontinuity (generation/topology/Run/Reset/Load or explicit
clock invalidation) reseeds endpoints and never interpolates across it. Boolean/enums/events
are step-valued from the latest commit <=T; scalar physical quantities are held at that commit
unless their registered definition explicitly declares continuous interpolation. Rigid poses
use position lerp and shortest-arc normalized rotation interpolation; full affine baselines remain
captured values, not quaternion-normalized geometry. Purely cosmetic scalar records interpolate
their compatible samples; event occurrence identity is never interpolated.

A Simulation-clock bracket must contain T; otherwise hold the previous complete coherent
display state (physical parent and all dependent cosmetics together). Autonomous UI samples may
still advance at D; they do not depend on a world. Presentation-clock cosmetics attached to a
physical parent use their bracket at D composed with the parent at T in the declared order.
If any required bracket or mapped timestamp is missing/invalid, do not independently clamp each
body/channel to a different time. No extrapolation or reconstruction of physics from animations.
Last successful T and D are nondecreasing in a generation. An invalid/stalled mapping holds;
recalibration seeds a new display epoch before resuming, never rewinds a prior epoch.

Pause/Completed applies the last committed endpoint immediately on the matching result/event and
reseeds both endpoints to it; Simulation-clock effects hold there. Presentation-time autonomous
effects continue while visible. Step applies the exact new endpoint once and reseeds; Resume
starts a new wall-to-simulation segment at the paused tick without adding paused elapsed time.
The matching Pause/Step result and full sample must refer to the same or explicitly later terminal
commit; never display an earlier tick as completion. B maps goal event revision/tick to the same
endpoint. Pause does not remove or acknowledge undelivered occurrences.

## Finite recipient histories and memory

B keeps THREE separate histories: at most3 complete physical S samples,3 complete world-animation
samples and3 complete standalone-UI animation samples. The latter two are distinguished by
(RuntimeSession,WorldGeneration,AnimationGeneration), never merged into one envelope or ring.
The same A worker capture clock maps both; world samples also retain their S feedback axis.
World-dependent composition uses world history at T/D as above; standalone UI uses its own
history at D and may progress while world brackets are held. Retiring one stream removes only
its identities/samples; delayed older sequences cannot retarget another stream.

All copied wire histories count inside B's existing256KiB role budget:
192KiB histories/metadata,32KiB retained request/result/query/event/registration wire values,
32KiB packing/decoding scratch. Installed browser descriptors/scene resources count separately
as owned runtime state under resident/linear limits, never as uncounted retained wire copies.
Let P,W,U be maximum complete envelope bytes for physical/world-animation/UI streams.
Absent streams use0; every active stream includes its envelope even if it presently has no
visible outputs. Freeze admission 3*(P+W+U)+1024<=196608 bytes.1024 is a fixed metadata
reservation:9 retained sample metadata records64 bytes each,3 stream endpoint records64 each,
and256 bytes clock/reference bookkeeping. Per-sample original S feedback stamp is retained
inside those64-byte metadata records. Managed object/allocator overhead also counts toward
resident/linear limits. Actual encoded field widths must fit those fixed records, not heap estimates.

For P=44057,W=6773,U=6773 the requirement is173833 bytes and both256-output streams fit.
P=23497,W=6773,U=6773 needs112153. With no UI stream,44057/6773/0 needs153514.
The review counterexample44057/6773/22009 needs219541 and rejects; the earlier512-byte
metadata estimate already required219029, not153002. It is not a required P0-003 workload.
Even individually valid64KiB envelopes do not guarantee combined admission.
P0-003 Animation stress has at most256 declared outputs; Visual256 has256 world outputs.
Even their stronger simultaneous256+256 case with the velocity-rich physical sample fits.
Existing required UI consumers must be counted in U at actual capture; these examples reserve
256 UI outputs, not merely one hint animation. If a later required consumer/workload's exact
inventory exceeds the joint cap, reopen its prerequisite contract for independent review;
never trim a required workload, silently lower history depth or claim qualification.

Peak replacement uses a single reusable192KiB reservation. Precommit candidate metadata and
sealed transfer remain charged to the32KiB retained-values and1MiB staging stores, respectively;
no new-generation history is retained before commit. After authority commit, retire old affected
histories before copying the new seed, holding the last already-applied scene (not a saved extra
wire sample). UI history stays while world changes. Thus peak is3*(max(Pold+Wold,Pnew+Wnew)+U)+1024,
not the sum of both world histories, and both old/new installed quotas must pass before commit.
UI replacement during navigation is serialized after world replacement: its peak uses
3*(Pnew+Wnew+max(Uold,Unew))+1024. In-flight old/new transport allocations remain charged to
the existing edge pools until returned; stale arriving history is validated/discarded in the
owned lease with fixed scratch, never secretly copied. New data received before old retirement
is discarded/returned and a later complete seed supplies it; reliable topology/control/result
records are retained under their separate existing quotas and never dropped.

A reserves two complete S feedback samples plus exact referenced-stamp/occurrence metadata inside
its existing768KiB role quota; at maximum2*65536=128KiB leaves640KiB for other retained payloads/
scratch. S retains output/result/events within its1MiB. Every count and byte admission must pass;
installed evaluator/core state is separately resident state, never a hiding place for wire copies.
One active1MiB transfer staging per recipient and six512KiB edge pools remain P0-005's8MiB total.
Direct validation/copy from the owned incoming lease must fit fixed scratch; no extra64KiB decode
buffer outside accounting. Topology transfer/history installation uses pre-reserved candidate
memory and releases old generation after completion, within peak—not merely steady-state—caps.
The serialized retirement/copy rule above always applies; it is not an optional fallback.
Never retain hidden copies for rollback of an already committed replacement.

On receive, retain newest ordered complete samples; discard oldest only within the installed ring
capacity, returning the original transport lease. Rendering slower than receipt may miss a bracket;
that produces the coherent hold above, not array mixing. History shortage is measured and failing
age/performance remains visible. Depth is finite correctness policy, not a claim the target is met.
At120Hz three physical samples span16.666667ms; three60Hz A samples span33.333333ms.
Missing deliveries can exhaust either. P0-025/034 must measure whether the fixed delay/history meets
33.3ms p95/50ms p99 age at both60/90Hz on the declared workloads; native arithmetic cannot pass it.

## Scheduling, visibility and overload

S wakes independently, computes whole due ticks from monotonic elapsed at120Hz and executes at
most4 ticks per scheduler turn, then yields to control/returns. Each tick retains four substeps.
No skipped tick, adaptive physics dt or renderer-driven catch-up. Debt remains measured; if debt
exceeds100ms stop scheduling new ticks at the last commit, emit sticky Capacity fault and enter
ReversibleFailure. Explicit Reset/Load/recovery is needed; never silently erase debt and resume.
A evaluates at60Hz independently, at most1 evaluation per turn at actual elapsed clock time.
Missed cosmetic samples may be replaced, not reliable occurrences; analytic evaluator phase is
advanced to current admitted clock time, with existing present-once rules preserved.

Every scheduler turn services returns first, then up to8 control envelopes, then up to8 data
envelopes before its bounded evaluation/tick group. Rotate the finite incoming edges round-robin;
no permanently preferred peer. Pending controls force a yield between whole ticks, not interruption
of a transaction. Target foreground service gap<=8.333334ms S,<=16.666667ms A; actual gap/CPU is
recorded. These algorithmic limits do not promise OS scheduling or cancel a long synchronous solver.
>50ms unexplained application work remains P0-003 failure even if watchdog eventually recovers.

A history age>50ms or reliable age>50ms immediately flags stale/performance failure while keeping
UI controls responsive; it is not a new acceptable latency. Bracket shortage freezes affected world
display. At500ms unresolved reliable stall stop S as above;1000ms missing liveness faults Worker.
A stall does not synchronously block physical computation; eventual bounded event/output capacity
can backpressure S before commit. Do not discard reliable physical events to keep ticking.

On page hidden, B requests Pause via ordinary lifecycle control and hides cosmetic targets through
existing SetVisible requests. If browser scheduling is suspended before those arrive, no invented
Pause acknowledgement: S continues only until its own bounded debt/backpressure rules stop it.
No hidden-tab performance pass. Foreground resumes calibration, validates session/result identities,
seeds coherent histories and remains paused/faulted until explicit Resume/Reset. Autonomous
presentation phase uses elapsed monotonic time; hidden intervals skip sampling, not phase identity.
Present-once wavefronts/impulses remain bounded and held until actual visibility receipt or explicit
Remove/Reset; merely becoming visible, a timeout, or an expired natural lifetime is not a receipt.
Reduced-motion selects declared cosmetic policies at registration, never changes physical clocks,
topology, inputs, solver tolerances or authoritative outcomes.
