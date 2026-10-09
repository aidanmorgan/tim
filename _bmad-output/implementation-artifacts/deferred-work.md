# Deferred work

- source_spec: `_bmad-output/implementation-artifacts/spec-5-1-dynamic-box-rigid-body-upright-stability.md`
  summary: Apply the declared LinearDrag (body record byte 66) in the physics worker (own slice); balls currently roll without decay because the 0.95 resting-damping hack was deleted and the worker never read the drag.
  evidence: worker.js reads mass/gravity/COM/inertia but not offset 66; Basketball declares drag .04 1/s; ENGINE-CORE/ANIM suites pass without it (cumulative 46/46). RESOLVED by Story 6.1b (ENGINE-DRAG, `spec-6-1b-engine-drag-application.md`): the worker applies body record +66 as an exact per-substep decay after gravity, and declared ball rolling resistance (material record +14) stops rolling at every sphere contact.

- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: ControlHint throws "An animation control is pending" when the single animation lease is busy (hint button, guidance visibility).
  evidence: Code trace Schedule.cs:127-141; pre-existing single-lease design from Story 4.1; reduced by the timer resend fix but not eliminated. RESOLVED by Story 4.3 (ANIM-1c): `BrowserWorkshopClient.ControlUi` queues the request and `PumpUiControls` sends it when the lease is free; unit facts `UiControlQueuesBehindTheLeaseAndSendsTheDeclaredHintClipWhenFree` and `QueuedRevealSurvivesAPendingPreparationAndSendsOnceItClears` (CuriousContraptions.tests/WorkshopActivationAnimationTests.cs).
- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: Wire Endpoint controls cannot express a 0→1 ramp (From must equal To), so the worker substitutes 0→1 for ColourBlend endpoints.
  evidence: WorkshopHint.cs Validate and worker Program.cs branch; design wart from 4.1, harmless today.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: Impulse occurrences start at control receipt time, not committed event time.
  evidence: Worker comment states committed-history capability is missing; cosmetic latency only.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: `_contactPulses` is never pruned within a world and faults at capacity after ~35 h of continuous hits.
  evidence: Capacity 8 × 1601 × 16; prune completed pulses and make exhaustion a logged drop.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: tools/e2e/workshop-driver.ts hard-codes the homebrew @playwright/mcp path and 1440×900 pixel anchors.
  evidence: Pre-existing from ENGINE-CORE stories; resolve playwright from a project dependency.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: Palette values are retyped as Half fractions in part artwork instead of named palette constants.
  evidence: Pre-existing 4.1 pattern; AGENTS requires approved palette identities keep their types.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: Stale-cadence ACK may null a contact pulse's request while the worker keeps emitting samples, throwing "does not own" after pause/resume mid-pulse (unverified, medium if true).
  evidence: Would be settled by a Chrome pause/resume during a bumper pulse.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: Uncompiled legacy tests still reference deleted members (BumperPart.HitCount etc.).
  evidence: Pre-existing; covered by Epic 7 story 7-3 purge.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: Worker impulse-slot admission (retransmit guard, envelope mismatch) is not unit-testable because Program.cs is not compiled into tests.
  evidence: Extract a shared slot type into CuriousContraptions.Animation and test it.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: ValidateWorkshopRead compiles the full physics scene on every presented read on the main thread.
  evidence: Pre-existing; address at the named performance gate.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: Earlier-epic uncommitted changes in MachineWorld.Gpu.cs (WorkshopFault cleared on Applied, Admitting guard, construct→ack rewiring) are undocumented and untested.
  evidence: Present in the pre-4.2 working tree; not this story's change.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: SendAnimation comment overstates that a failed JS send faults the transport (JS animationControl throws without fail()).
  evidence: Murdoch pass-2 trace: every reachable throw path is already a faulted transport; wording fix or explicit fail() in the JS guard.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-3-legacy-presentation-code-retirement.md`
  summary: A validator-admitted full free-play population (32 parts, 16 dynamic bodies, 16 sensors/guides, 8 triggers) is proven to compile but not to run on the live worker in Chrome.
  evidence: Murdoch 4.3 F5; belongs to the named stress/qualification gate (playable-first policy).
- source_spec: `_bmad-output/implementation-artifacts/spec-4-3-legacy-presentation-code-retirement.md`
  summary: Free-play Ramp palette row anchor (130,511) in tools/e2e/workshop-driver.ts is unexercised by any Chrome test.
  evidence: Murdoch 4.3 F4; first free-play Ramp e2e should confirm it.
- source_spec: `_bmad-output/implementation-artifacts/spec-4-3-legacy-presentation-code-retirement.md`
  summary: Capture feedback slots are keyed by position in Read.Captures rather than sensor id (unverified risk if a partial latch list were ever published).
  evidence: Blind hunter; Murdoch verified latch equality guards; settle by pinning "every compiled sensor is published in id order on every read".
- source_spec: `_bmad-output/implementation-artifacts/spec-4-3-legacy-presentation-code-retirement.md`
  summary: Uncompiled parts/, engine/ScenePhysicsAssembly.cs, tools/Ownership and ~45 uncompiled tests still reference the deleted presentation types; WorkshopConstruction.Receiver first-match accessor is ambiguous once multiple receivers are admitted.
  evidence: Epic 7 purge scope (stories 7-3/7-4); make the accessor single-or-throw or remove it then.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-1-dynamic-box-rigid-body-upright-stability.md`
  summary: worker.js supportExtent for a box is computed once per tick from the tick-start orientation; stale for a rotating box inside a guide region.
  evidence: Unreachable today (guides target only WorkshopBall); recompute per substep when boxes become guide targets.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-1-dynamic-box-rigid-body-upright-stability.md`
  summary: Broadphase AABB for a plane collider assumes a +Y normal; a tilted static plane would be culled.
  evidence: Only the +Y bench plane exists; reject non-+Y planes at admission or derive the AABB from the normal when needed.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-1-dynamic-box-rigid-body-upright-stability.md`
  summary: Joint 4x4 manifold solve falls back to sequential rows whenever any row would go negative (typical mid-topple), mixes soft/speculative row scales, and allocates per substep.
  evidence: Blind/edge-case hunters; Murdoch probes show stable rest (0.39 mm, no creep) so quality not performance is at stake; address at the named performance gate.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-1-dynamic-box-rigid-body-upright-stability.md`
  summary: Box-on-Ramp/Wall contact and the 16-dynamic-body rejection are proven in Node/unit only, not in Chrome.
  evidence: Murdoch F6; Story 5.2's domino_effect has no Ramp (bench cascade only), so the Chrome proof of Box-on-Ramp/Wall contact remains open for a later slice (CAT-014 or the first Ramp-routed box level); the 16-body rejection stays unit-proven.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-1-dynamic-box-rigid-body-upright-stability.md`
  summary: The physics worker is JavaScript doubles, not the wasm-simd128 f32 kernel AGENTS.md describes.
  evidence: Murdoch F10; pre-existing architecture gap, roadmap Epic 16 / named performance gate.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-domino-cascade-orientation-threshold-sensor.md`
  summary: The host does not cross-check an orientation sensor's fired state against the committed body pose; fired state is worker-trusted like contact triggers.
  evidence: Blind hunter; recorded design decision; a host-side pose check from the motion piece would make it verifiable.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-domino-cascade-orientation-threshold-sensor.md`
  summary: Precision slider is shown for DominoEffect but has no effect; CAT-023 "striker must not directly hit the second tile" is unasserted.
  evidence: Blind hunter; difficulty/nudging and campaign tuning belong to Epic 15.
- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-domino-cascade-orientation-threshold-sensor.md`
  summary: worker.js supportContact resolves exactly parallel support edges to an endpoint; node-cost rule duplicated in WorkshopActivationCompiler and WorkbenchCapacity.ValidateConnected; per-sensor linear body search per substep; clip-id uniqueness has no observing test.
  evidence: Murdoch F2 (finite, non-launching), blind hunter, verification-gap; quality/performance gate items.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-bowling-ball-dynamic-sphere.md`
  summary: Apply the declared LinearDrag (body record byte 66, Basketball and Bowling ball 0.04 1/s) in the physics worker. Required before CAT-014's "both balls rest without jitter" acceptance can hold; today struck balls keep rolling (the two-lane e2e asserts vertical rest and lane containment, not a stop).
  evidence: harness two-lane fact shows both balls rolling at 0.6–0.8 m/s after 5 s; Story 6.1 Never: drag application. destination: roadmap slice ENGINE-DRAG (vertical-delivery ordered-roadmap note), immediately after CAT-014. RESOLVED by Story 6.1b (ENGINE-DRAG): linear drag plus declared rolling resistance (Basketball 0.035, Bowling ball 0.03); the harness two-lane fact and `cat-014` tests 1 and 4 assert both balls below 0.02 m/s from 6 s and under 1 mm of movement over the following second.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1b-engine-drag-application.md`
  summary: Rolling resistance is scoped to static supports (a dynamic body against a static one) to preserve the marginal CAT-014 Domino outcome. A ball rolling across a dynamic body (for example a lying Domino) is not decelerated by its coefficient while on it.
  evidence: in the two-lane strike the Bowling ball rolls over the tile's top edge for about 0.26 s; with the row applied between the two dynamic bodies, that support phase alone (not the impact substeps) turns the marginal Bowling-lane topple (90°) into a rock (about 11°; harness sweep 10.6°). RESOLVED by the owner decision of 9 Oct 2026 (spec 6.1b Spec Change Log): rolling resistance acts at every sphere contact with the reaction on the partner, the Bowling ball became 0.28 m / 4 kg, and the unchanged two-lane geometry still separates the kinds (Basketball lane 3.5–9°, Bowling lane 90° for release centres 0.8–1.4 m; harness fact "ball rolling on a dynamic box slab" pins the every-contact row).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1b-engine-drag-application.md`
  summary: Declared linear drag 0.04 1/s alone is below the committed binary16 velocity resolution in free flight: its per-tick decrement (0.017% at 240 Hz) rounds back at 240 Hz and at most magnitudes at 120 Hz, so a flying ball keeps its horizontal speed. On the bench the rolling-resistance decrement carries it.
  evidence: harness drag fact needs the 0.125 1/s bound to survive every cadence (0.05% per 240 Hz tick); docs/gpu-f32-physics.md game-grade envelope "Committed Velocity Resolution". destination: Story 6.1c ENGINE-F32-VELOCITY (f32 committed velocity; owner decision 9 Oct 2026). RESOLVED by Story 6.1c: committed velocity and angular velocity are f32 in the body record, motion piece and response record; harness facts show declared drag 0.04 slowing a flying ball as 2e^-0.04t within 1% on every committed tick at 60/120/240 Hz, and a 5 m/s rolling Basketball decelerating on every 240 Hz tick and resting (about 16 s); the 0.125 drag fact now holds within 1%.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-bowling-ball-dynamic-sphere.md`
  summary: CAT-014 "Domino/lever loading against a matched Basketball control" — the lever half is unproven because the Impact lever is not admitted.
  evidence: requirements CAT-014; Story 6.1 proves only Domino loading. destination: the ELEMENT-n slice for CAT-034 Impact lever (vertical-delivery ordered roadmap), which must add the Bowling vs Basketball lever-loading control.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-bowling-ball-dynamic-sphere.md`
  summary: Domino offset centre of mass (CAT-023 requirement) re-deferred explicitly as a Domino item; CAT-014 ships homogeneous spheres only.
  evidence: Story 6.1 Never: offset centre of mass; requirements CAT-023 delivery notes. destination: roadmap slice CAT-023c (vertical-delivery ordered-roadmap note), a CAT-023 follow-up row.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1b-engine-drag-application.md`
  summary: Rolling-resistance details unpinned or approximate: larger-of-pair radius and Crr rules have no mutation-killing test; per-axis diagonal angular masses (no 2x2 block) against anisotropic dynamic boxes; row order vs normal rows unobservable; rolling torque on speculative (not yet touching) contacts; no re-clamp after the restitution pass (resolved: added by Story 6.1c, pinned by Story 6.1d); worker has no guard for negative/NaN Crr (host admission validates); e2e rest window measured by wall clock not committed ticks.
  evidence: Murdoch pass 3 L1; verification-gap, edge-case and blind-hunter pass 2. destination: physics quality pass with Story 6.1c or the Epic 16 qualification gate.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1b-engine-drag-application.md`
  summary: docs/gpu-f32-physics.md:12 says in the present tense that the solver is implemented in C# compiled to WebAssembly.
  evidence: Murdoch pass 3 L3 (pre-existing). destination: Story 6.1c docs update. RESOLVED by Story 6.1c: the paragraph states WASM SIMD as the target and the JavaScript module worker (f32 velocity, binary16 pose commits) as today's solver.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1c-f32-committed-velocity-precision.md`
  summary: The clamp headroom shrank from 1/512 (binary16 rounding) to 2^-20 (f32 rounding), so the relax sweeps and restitution pass (which run after the pre-integration clamp) could otherwise commit a speed the host rejects; Story 6.1c therefore clamps again after those passes. No harness or Chrome fact drives a contact past the envelope, so the second clamp is pinned only by inspection.
  evidence: worker.js substep order (clamp, integrate, relax, restitution, clamp); the envelope-clamp harness fact has no contact. destination: Epic 16 qualification gate (extreme-speed contact cases). RESOLVED by Story 6.1d: the harness fact "a heavy body striking a light one at the envelope" drives restitution past 64 m/s in the last substep of a tick (a mutant without the second clamp commits 126.7 m/s), and a direct `settleVelocity` fact pins the end-of-substep guard (a finite velocity of any size clamps; only a non-finite component drops the motion).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1d-velocity-hardening.md`
  summary: Above about 5 m/s the contact rows act intermittently on a rolling sphere: at 240 Hz the spin stays constant for runs of ticks while drag alone slows the speed, then friction re-couples the ball (10 m/s: 6 stalled ticks in the first second; 20 m/s: 28; 30 m/s: 65; 40 m/s: no rolling resistance for the first ~100 ticks). The average deceleration over a second matches $(5/7)(C_{rr}g + c\,v)$ within 10% up to 30 m/s (9% short at 40 m/s). Not a precision effect (f32 steps are about $10^{-6}$ m/s at these speeds); likely the TGS relax pass seeing the rotated contact anchor of a fast-spinning sphere as separation. Gameplay ball speeds stay below this range today.
  evidence: scratch probes over 5–40 m/s (spec 6.1d Implementation Notes); harness facts pin the per-tick claim at 5 m/s and the rate at 20 m/s; docs/gpu-f32-physics.md "Committed Velocity Resolution" is narrowed accordingly. destination: physics quality pass or the Epic 16 qualification gate (high-speed rolling contact).
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1d-velocity-hardening.md`
  summary: Motion pieces (pre-existing, unchanged by 6.1d): the worker writes each piece from the pose at the end of its substep but labels it with the substep's start ordinal, so between-tick samples lead the physics by one substep (about 2 ms); and it writes the body origin into the lanes the host reads as the centre-of-mass cell, which agree only while the local centre of mass is zero (true of every compiled body today; the CAT-023c offset-COM Domino will break it).
  evidence: worker.js motion piece recording (start = sourceOrdinal + s after the substep's integration; cell/local from the body origin) against `PhysicsMotionRead.TrySample` (extrapolates from the start ordinal; subtracts the rotated local COM). destination: CAT-023c (offset centre of mass) or the presentation qualification gate.
- source_spec: `_bmad-output/implementation-artifacts/spec-6-1d-velocity-hardening.md`
  summary: Two e2e driver gaps (Murdoch pass 2, Low).
    - **Run acknowledgement:** the one-acknowledgement-per-input check runs without settle frames for Run (`settle=false`). A late duplicate acknowledgement can escape it if it arrives before the next command starts.
    - **Save size:** the save-slot check hard-codes `SAVE_BYTES = 5720` instead of deriving it from the C# codec. A codec change fails loudly but needs a driver edit.
    - **Accepted limitation:** Construct and Save both end in Build mode, so their acknowledgements are told apart only by order and the save-slot check.
  evidence: `tools/e2e/workshop-driver.ts:441`; `engine/gpu/WorkshopSaveCodec.cs:12,22`.
  destination: the next e2e driver change, or the Epic 16 qualification gate.

