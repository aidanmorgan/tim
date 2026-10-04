# Simulation controls, sensors and presentation events

Controllers run in the independent simulation host with typed identities, enums and integer ticks. Physical numerical queries remain in [WGSL f16](gpu-f16-physics.md). State, pending input, occurrence delivery and rollback have one owner and commit coherently through the [bridge](simulation-presentation-bridge.md).

## Delay, Hold Timer and oscillator

Delay is one-shot Ready → Counting → Finished. A trigger starts one countdown; busy/finished retriggers are ignored until Reset. Authored delay is 0.1–12 seconds, default 1 second, converted by ceiling to a positive checked integer-tick duration. Reject invalid duration/identity, foreign snapshots, clock discontinuity and deadline overflow.

Hold Timer rearms at completion before networks without emitting an elapsed activation. Delay completes before physics and emits its activation. Busy triggers never restart either deadline. Larger advances preserve every crossed occurrence with its actual due tick and deterministic identity order. Borrowed output cannot be retained without copying. Reset/Load rebuilds fresh controller state.

Periodic clocks preserve phase, deadlines, crossed occurrences and pause/Reset semantics; do not replace simulation time with rendered frames. Supply loss cancels a coincident deadline, disabled time creates no pulse backlog, and restored supply starts a full interval while retaining counter/last-pulse history. Validate consecutive integer ticks, complete inputs and deterministic order atomically. Digital clocks supply no mechanical or electrical energy merely because their artwork moves.

## Counter and latch

Counter target is an integer 1–9, default 3. Increment saturates and emits the threshold once; its contact conducts supplied electricity only after reaching target and creates no supply. Reject fractional/out-of-range/missing parameters and unknown identities; Reset reconstructs zero.

Set/Reset delivered at tick t settle at t+1. Reset dominates a simultaneous Set independent of delivery order; different ticks remain separate and memory persists without inputs. Reject late, unsupported future and undefined commands. Snapshot both adjacent-tick request buckets, phase and clock. Whole-tick rollback includes pending input and reliable occurrences, not just the visible phase.

Optical logic has two independently hysteretic input states and a separately delayed gate decision. Preserve a carrier's retained state and pending inputs across rollback; supply/occlusion controls cannot fabricate a positive result.

## Physical sensors

Basket capture declares a general containment/relative-speed/continuous-residence sensor bound to typed body and receiver-frame identities. Shared WGSL f16 geometry and sensor evaluation compute full containment, relative speed and continuous qualifying residence/dwell numerical results in the moving receiver frame. A reusable C# controller consumes committed results for discrete latch/event transitions and declared integer scheduling only; it never integrates physical residence or evaluates a physical predicate. No Basket class/ID branch or part-local physical predicate/equation is permitted. Exit, excessive relative speed or disabled sensing breaks dwell; sufficient continuous residence latches capture once for the named body. Keep authored margin/speed/dwell and zero-guidance controls.

Passage sensors use owned compound/aperture geometry and detect directional crossings hidden between endpoint samples. Preserve arming/rearming within a trajectory and split-step equivalence; emit once per qualifying passage. Reverse, miss, blocked and disabled controls do not trigger.

Tilt sensing captures the construction reference and can detect tip-and-return within one step. Disabling suppresses new events without clearing a completed latch. Contact-load sensors measure direct-contact mass, count each body once across compound contacts and apply declared region/normal filtering. They do not infer stacked-load propagation or measure force; proximity and visible scene state are not authority.

## Presentation and transactions

Every pulse retains occurrence identity, time, lifetime and explicit overlap, clock, visibility and timing policy. Capacity rejection neither consumes an identity nor acknowledges an undelivered event. Accepted body motion and a moving reference frame are sampled at the same committed display timestamp; no shortest-arc endpoint approximation replaces an admitted multi-turn trajectory.

Failed transactions attempt every required restore, retain restore failures and fault unrecoverable state instead of silently retrying. Qualify controls, declaration order, pending-event failure, disposal/generation change and exact Run/Reset/save through actual UI where supported. Animation never changes controller or physical outcomes.
