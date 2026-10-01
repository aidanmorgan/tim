# Lifecycle and completion

Normative P0-006 decisions. B is Browser, S SimulationHost, A AnimationHost.
Wire commands/outcomes remain [P0-005](../P0-005/wire.md); internal enums below do not add
wire fields. HostMode is Building, Running, Paused, Completed or Faulted.
Completed means goal completion or the existing 3600-tick/30-s Run limit; it retains runtime
state for display, never becomes editable construction. Faulted prohibits further simulation.
SessionPhase is Starting, Ready, Recovering, Disposed or Unsupported.
TransitionPhase is Idle, Preparing, Committing or AwaitingRecipients.
BrowserOperation is PendingAdmission, PendingCommit, PendingApplication, Completed, Rejected
or Indeterminate. Each operation retains typed original generation/sequence; text labels do
not select behavior. A command admission receipt means only PendingCommit.

Startup allocates a fresh session with generation1, revision0, tick0 and an empty validated
Building construction; initial level selection uses the same explicit ReplaceConstruction barrier.
No user command is admitted before matching Ready/schema/resource handshakes and clock readiness.
Preparing/AwaitingRecipients may admit bounded requests, but dependent lifecycle application waits
for Idle; returns, acknowledgements and fault reports are always serviced. Cancel can discard a
still-precommit candidate; after the commit point it cannot undo installation. A new Reset/Load
cannot overtake completion of the previous install or exceed the two-generation result window.
Fault/liveness deadlines remain active while waiting. Exit/dispose admits no new work and retires
the session; disposal never reports queued work as Applied.

World animation and standalone UI have distinct active evaluator generations, but share ONE
aggregate128-entry pending/result pool in A and at most TWO retained result generations, exactly
as P0-005 requires. They do not each receive128 entries. B assigns globally increasing positive
animation generations without reuse and one contiguous request sequence per generation.
A generation's immutable stream identity is (RuntimeSession, WorldGeneration, AnimationGeneration):
WorldGeneration0 denotes UI; a positive value denotes its owning world. No message combines them.
Clock generation identifies the A runtime monotonic origin, not either evaluator or result lifetime.
Both streams use that same A clock origin; physical SimulationTime retains the S world generation.

On world Run/Reset/Load, B stops admitting new controls for the retiring world animation generation,
retires its samples, and resolves/acknowledges every accepted request through its last admitted
sequence. Then ResetGeneration, sent in that old generation, cancels its evaluator instances and
creates the new empty generation; its result still belongs to the old generation. New-generation
registrations/controls wait until that reset result and all prior results are cumulatively acknowledged
and the original allocation Return confirms A processed the acknowledgement. Acknowledgement
returns are transport completion, not an invented extra semantic result. No third result-generation
window is created. If no world evaluator exists at startup, its first globally fresh generation is
registered after the same aggregate/window admission check, with no fictitious old reset.

UI generation and its requests/results stay live during ordinary world replacement; they count in
the same128 and two-generation quotas throughout. Example UI7 + world8 retained: world8 Reset
creates evaluator9, but result generations remain7/8 until8's final ack; only then can9 admit a
result and retained generations become7/9. UI controls in7 continue while quota is available.
A further world replacement9→10 waits for prior completion and follows the same sequence; no
drop/overwrite of old terminal outcomes. Navigation replaces world first, finishes its result/ack
barrier, then replaces UI by the same algorithm; it never admits third/fourth result windows.
The browser operation remains PendingApplication through both replacements. Normal Pause/Resume
does not replace either evaluator generation. UI teardown/navigation restores exact UI baselines.

There are at most two active evaluator namespaces (UI/current world). Retiring instances are
removed before new instances are installed, within the aggregate2516-output and impulse bounds.
One existing AnimationBatch owner can remove that namespace's declared handles without globally
resetting surviving UI handles; no second physics owner, generic storage framework or alias path.
The new namespace is empty until acknowledged retirement; target/property claims never overlap.
The aggregate result pool is not partitioned or multiplied: saturation returns Full. B drains and
acks results before admitting more controls; returns/acks need no result slot, so even128 retained
entries cannot deadlock their own release. Round-robin service prevents a world stream from
permanently starving UI. Reliable age>50ms still fails performance; at500ms unresolved retirement
stall the world remains stopped and recovery is offered, and at1000ms missing liveness the session
faults. No timeout discards terminal results or reports reset completion. UI may continue within its
remaining quota until explicit recovery retires the session; no indefinite silent pending state.
If both streams occupy all128 results, neither gets invented capacity; responsive browser pending/
recovery controls remain independent of A evaluation. P0-032 must prove this saturated case.

A session restart retires both namespaces. Animation generation/instance/descriptor exhaustion is
explicit Exhausted failure, never wrap or reuse. Run's seed and new-world registrations complete
before first tick scheduling. Existing wire ResetGeneration/result/ack fields are unchanged.

## Complete command/mode matrix

The table covers all 16 CommandKind values. Groups list each member explicitly.
A = apply the specified transaction; I = committed InvalidMode with no authority mutation.
Mode validation follows validated schema/session/admission; stale revision, dependencies,
unsupported target/capability and out-of-range results take their P0-005 meanings.
Wrong session/generation/schema or capacity at admission reserves no command/result.
AlreadyCompleted outcomes are not invented: repeated new Run/Pause/Resume/Step uses I;
exact retransmission retains the original outcome under the original ID.

| Command | Building | Running | Paused | Completed | Faulted |
| --- | --- | --- | --- | --- | --- |
| ReplaceConstruction | A → Building | A → Building | A → Building | A → Building | A → Building if safe recovery below |
| CreateEntity, ReplaceEntity, RemoveEntity, Connect, Disconnect | A → Building | I | I | I | I |
| BinaryInput, ScalarInput, Pulse | I | A in next/exact tick | admitted, wait for Step/Resume tick | I | I |
| Run | A → Running | I | I | I | I |
| Pause | I | A → Paused | I | I | I |
| Resume | I | I | A → Running | I | I |
| Step (exactly one tick) | I | I | A → Paused or Completed | I | I |
| Reset | A → Building | A → Building | A → Building | A → Building | A → Building if safe recovery below |
| Save (Construction only) | A → Building | I | I | I | I |
| Cancel | A, unchanged mode | A, unchanged mode | A, unchanged mode | A, unchanged mode | A if coordinator remains usable |

Building Reset preserves the exact current construction, advances generation and invalidates
old pending work; it is not an undocumented no-op. Runtime Reset restores the captured pre-Run
construction, including typed links, settings and authored velocities. Run keeps world generation,
starts tick0 and captures that construction exactly; revision increases once. Reset/Load advance
generation by one and increase the installed revision once from the old revision (no wrap).
New generation's tick is0. Authority revisions are monotonic across lifecycle changes in a session;
generation still gates identity. A gameplay tick increments revision once. Save/Pause/Resume/Cancel
each successful lifecycle command increments revision once. Step's result reports the one tick's
revision; it must not add a second revision for its enclosing lifecycle request. Application
rejection does not increment authority revision. Counter overflow faults Exhausted before mutation.

Faulted has two subreasons: ReversibleFailure (all participant restoration succeeded) and
IntegrityLost (any restore or commit invariant failed). Only ReversibleFailure permits Reset/Load/
Cancel within the session, using a new complete candidate and the retained known-good construction;
Run/Resume/Step cannot retry damaged runtime. IntegrityLost rejects all new application requests
with InvalidMode while control returns/acks/faults remain serviced; explicit session recovery is
required. Pending accepted operations become Indeterminate on process termination, never fabricated
wire success/failure results. FaultReason.Transaction/Worker records the concrete cause.
A failed tick restores the due commands to pending exactly; the stopped faulted world does not
automatically re-run them. Reset then invalidates them, Cancel cancels them, or session recovery
abandons them explicitly. P0-032 may deliberately retry the same transaction in an isolated test
to prove determinism; production retry is never hidden.

## Authority commit and failure atomicity

All operations occur between whole committed ticks. Arrival during a tick waits for admission
at the next boundary; an ExactTick that became late rejects TooLate. Return/ack handling is
separate from authority mutation. Lifecycle-only control reservations stay available under normal
data saturation; a future input cannot block Pause/Reset/Cancel.

Prepare fully before crossing the commit point:
1. Validate mode, generation, expected revision, dependency, canonical current schema, resource/
   capability IDs, units, graph/topology and capacities. For Run capture exact construction and
   compile simulation; for Load build a separate candidate from the sealed bytes. For Reset build
   from the captured construction. Candidate storage is within existing resident/transfer caps.
2. Allocate/validate every candidate array/map/checkpoint, output/result/event record, topology
   transfer and seed, before touching installed authority. Reserve bounded result and required
   outgoing semantic stores. Record disposal lists. No scene callback is in a physical transaction.
3. Enroll all mutable authority participants; capture without authority mutation. Perform reversible
   changes, prepare complete outputs and validate successful restoration resources. Any failure
   here restores admitted participants in reverse order and discards uncommitted publication.
4. The commit region contains only prevalidated assignments to already-owned references/counts
   and checkpoint-release state, with checked arithmetic performed beforehand. No allocation,
   serialization, property getter with external code, delegate, renderer, transport send or
   fallible resource disposal is allowed there. This is a required implementation restriction,
   not a current-source guarantee. P0-014/020 must inspect the generated call chain and inject
   failures immediately before/after it. Old resources remain on a preallocated retirement list.
5. Commit installs one authority revision and the already-reserved immutable result/outbox record.
   Actual transfer, recipient application and old-resource disposal follow. Their failure cannot
   undo committed physics or manufacture a retry. The retained result remains retransmittable.

Successful Load atomically switches construction, world mode/generation/revision, captured settings,
stable-ID maps, physical participation and full seed. It invalidates old pending commands
(InvalidatedByBarrier) and pending queries (Cancelled) in their original reserved slots.
Completed results/replies keep their first terminal outcome, even across the barrier.
P0-005's maximum two retained generations still applies: a further barrier waits for earlier
acknowledgements or explicit session recovery. It cannot delete results to create space.

Failed Load before authority commit preserves old generation, revision, mode, current physical
state, Run construction, pending commands/queries/events and completed results exactly.
Its own admitted command receives the applicable terminal rejection (schema invalidity at admission,
OutOfRange/InvalidCapability/UnsupportedTarget/Capacity/InvalidMode/StaleRevision at application).
Unexpected exception/restore failure is a Fault and no committed command result, as P0-005 requires.
A semantic rejection may consume its own command sequence/reservation; it never edits other work.
Unsealed/invalid transfer disposal acknowledges disposal only, not successful Load.
A Cancel that wins before Load commit cancels its pending command and discards the candidate;
after commit it returns AlreadyCommitted and never restores the previous level.

## Browser completion and exact construction

B keeps the last acknowledged construction and Run snapshot until the new generation has committed.
Preparing a Load must not call the current LoadLevel destructive path. The browser candidate includes
level/puzzle identity, sandbox flag, picker selection, precision, realistic-friction setting, NextId/
stable allocator high-water marks, part parameters/poses/locks, connections/ports, goals, palette counts,
layer/filter, selected entity/link/tool, undo history, camera, captured visual baselines and save identity.
Failed precommit Load leaves this entire record and its live resources unchanged; temporary loading
indicator/error text is separately owned operation presentation and may change. No clamping saved
parameters, silently replacing unknown puzzle, or old-format conversion.

After committed Load/Reset B retires old generation for all property writes immediately, acknowledges
correctly identified old terminal results for release, clears old histories and prepares the new
bindings from the captured candidate. A ResetGeneration cancels old world animation state and returns
its matching result; independent UI animations use their separate animation generation. New topology
must be installed/acknowledged by required recipients before state using it is presented.
B operation completion requires: matching Applied result, exact new topology/full seed, A reset and
required world registrations completed, new-generation bindings applied atomically to the scene,
and old-generation owned resources released. Pending operation remains visible until all hold.
A transport Credit, Transfer ack, S result alone, or queued A reset never completes the UI operation.

Browser target validation/all allocations occur before a final finite property batch.
External Godot setters/resources may still fail. Such a post-authority-commit failure is explicit
presentation failure/Indeterminate UI, keeps physics stopped at a boundary through Pause, and
requires recovery; never claim failed Load preserved an old world after S already committed.
Freeze last complete displayed scene while resolving; no mixed old/new property batch is accepted
as proof. P0-026 must exercise setters/resource disappearance and restore baseline exactly.
Navigation is ReplaceConstruction with the selected level's canonical construction; selection
changes only after successful completion. Undo is an exact construction replacement and follows
the same barrier. No additional wire command or alternate synchronous path exists.

Run captures construction once and completes B's running UI after seed and required bindings are
installed. S does not advance its first tick until topology recipients are ready; subsequent motion
is independent of scene cadence. Acknowledging topology is bounded data admission, not a per-frame
render rendezvous. Pause completes at its authority result and B applies the last committed endpoint;
Step completes only after the tick and endpoint are displayed. Goal completion and tick3600 stop S
at the committed boundary; B derives completion from the reliable Goal occurrence/current tick.
Save stays forbidden throughout runtime, including Paused and Completed.

## Save durability and recovery

Successful Save returns the exact sealed construction transfer at its command's generation/revision.
B validates the transfer digest/correlation and writes to a newly staged local record before replacing
the one current persisted save atomically through the platform's transactional storage API.
Storage commit is a separate browser result: S Applied means snapshot created, not saved on device.
Failed write/quota/permission leaves the previous saved record byte-identical and reports failure.
Crash during storage transaction yields exactly old or new complete record, never a partial record;
unsupported transactional storage is an explicit unavailable Save capability, no direct-overwrite
fallback. Load reads/validates before requesting replacement. Future runtime saves are unsupported,
not an unnamed future decision. Cosmetics and transient occurrences are not persisted.

On worker crash, B freezes last complete display, marks outstanding operations Indeterminate
(Worker failure), terminates both workers and retires the entire RuntimeSession. No automatic
command replay, implicit Load or recreation of old transient events. Explicit user Restart creates
a new session after disposal; user chooses the retained acknowledged construction or persisted
construction, then performs a fresh Load. That is explicit recovery input, not a runtime snapshot.
Session bootstrap Unsupported covers missing worker/runtime capabilities; never simulate on B.

Freeze these recovery limits (monotonic wall time, not simulated time):
- Pending indication target <=100ms; poll/service targets below are engineering bounds, not hard
  real-time promises. Total cold usable startup retains P0-003's5s budget.
- Initial worker handshake+clock readiness:5s from start; deadline expires to StartupFailed.
- Active foreground peer liveness: one ClockProbe every250ms; no valid reply for1000ms → Worker fault.
  Return/ack traffic alone cannot qualify a missing clock; missing worker progress remains visible.
- Reliable oldest item >50ms marks performance failure/backpressure immediately;500ms unresolved
  stalls stop new simulation ticks and request recovery. This is not a relaxed50ms budget.
- Candidate preparation/recipient lifecycle completion >2000ms shows TimedOut and offers Cancel/
  recovery, retaining original ID/results; timeout never means rollback or automatic retry.
- Termination/disposal confirmation:1000ms. If detached payloads/worker lifetime cannot be proved
  reclaimed within the existing8MiB budget, RecoveryBlocked; do not start replacement workers or
  allocate another pool. Explicit page reload is the stated prerequisite to clear browser-owned
  terminated resources; it is not claimed to preserve an in-flight runtime operation.
- At most one active recovery attempt; no automatic restart loop. All retry choices are explicit.
