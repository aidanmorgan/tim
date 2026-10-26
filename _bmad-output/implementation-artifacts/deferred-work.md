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

- source_spec: `_bmad-output/implementation-artifacts/spec-6-4-passive-springboard.md`
  summary: Make the inherited E2E MCP/Playwright dependency resolution portable across installation locations before qualifying another test environment.
  evidence: BMAD B7; workshop-driver.ts already bootstraps /opt/homebrew/lib/node_modules/@playwright/mcp/package.json, and the new Springboard PNG assertion resolves through that same installation. A differently installed environment cannot launch the existing driver; current Chrome proof uses the declared installation. Resolve the harness dependency and its PNG consumer together without claiming current cross-environment qualification.

- source_spec: `_bmad-output/implementation-artifacts/spec-engine-restitution.md`
  summary: Follow-up engine story "Engine: resting stack settling" (sprint key engine-resting-stack-settling; coordinator decision 3, 24 Oct 2026). Some three-box stacks keep a slow vertical soft-contact oscillation (0.01–0.04 mm per tick) for more than 10 s instead of coming to rest.
  evidence: Pre-existing on base cf35a064 and present on the restitution candidate, depending on the initial gaps and offsets; e = 0 stacks settle on both. After 1 s every point moves below 8.4 × 10⁻³ m/s, under every bounce threshold (0.1 m/s), so restitution cannot act late: the cycle belongs to the soft contact and relax rows. The restitution slice bounds it (StacksStopBouncingAndStayWithinTheRestingEnvelope: late per-tick motion ≤ 0.05 mm, point speed < 0.1 m/s). Raw runs: `evidence-engine-restitution/stacks/`.
  destination: its own engine slice; not a prerequisite of the restitution slice.

- source_spec: `_bmad-output/implementation-artifacts/spec-engine-restitution.md` (review R2-1)
  summary: "Engine: simultaneous/supported-impact restitution" — supported impacts lose bounce. Restitution is solved per manifold, not simultaneously over an island, so an impact through a resting dynamic body loses bounce fidelity: the resting body's contact with its support has about zero approach at the substep start, absorbs the push from the bounce and never returns it. Owner: engine (physics solver). Must be checked before any dynamic plank, crate or seesaw element is delivered.
  evidence: Reproducer `ReviewRound2Apex.ApexOnRestingDynamicBox` (`evidence-engine-restitution-review/round2/probes/ReviewRound2ProbeTests-candidate.cs.txt`; logs `apex-candidate.log`, `apex-base.log`): a ball (r 0.12 m, 0.6 kg) dropped 0.6 m onto a box (half 0.3 × 0.1 × 0.3) resting on the bench, all material (e, 0.1, 0.4); apex over drop height after the first impact. e = 0.55, 2 kg box: 0.020 (base 0.050) against ≈ 0.091 on a static support of the same top (e² of the pair 0.0914). e = 0.55, 0.5 kg box: 0.006 (base 0.011). e = 1, 0.3 kg box: 0.45 (base 0.19); e = 1, 2 kg box: 0.44 (base 0.57). Not an energy gain; current catalogue reach is negligible (a ball onto a Domino gives e² ≈ 0.0008, and balls cannot rest on balls).
  destination: its own engine slice (simultaneous island restitution or an equivalent supported-impact model), before the first dynamic plank, crate or seesaw element.

- source_spec: `_bmad-output/implementation-artifacts/spec-engine-restitution.md` (coordinator deferred item, 24 Oct 2026)
  summary: Test-host SIGSEGV under concurrent load. Owner: engine/test infrastructure. The xUnit test host has twice crashed with exit code 139 (SIGSEGV) in the full native suite with no failing test.
  evidence: (1) 9A.32 implementer run, exit 139 after 2045 tests (`evidence-9a-32/candidate/review-fix-test-run1-crashed-139.txt`; spec-9a-32-domino.md, review-9a-32-domino.md "Crash investigation"). (2) Restitution reviewer run, SIGSEGV after 848 tests while probe builds ran concurrently (`evidence-engine-restitution-review/round2/test-full-run1-crashed.log`). Both were unreproducible in isolation: reruns on the same binaries passed in full.
  destination: test infrastructure investigation (collect a native crash dump on the next occurrence; `--blame-crash` only on CuriousContraptions.tests with `--blame-crash-dump-type mini`).

- source_spec: `_bmad-output/implementation-artifacts/spec-10-2a-weight.md` (F1; coordinator decision, 24 Oct 2026)
  summary: Next engine slice "Engine: lying cylinder axial creep" (shared 10.1 defect, not the Weight's). A cylinder that drops onto its side can settle with its axis tilted about 1.5 × 10⁻⁴ rad and then creeps along its axis at about 0.05 mm/s indefinitely (0.5 mm in 14 s), with its read velocity frozen at a small constant.
  evidence: Reproduced with a plain LawScene cylinder (no package): r 0.3517, h 0.2793, 4 kg, material (0.08, 0.1, 0.3, Crr 0.03), rolled down a 15° box ramp whose bottom corner (not its top face) meets the bench, so it drops ~0.17 m onto its side. A bench roll without a drop, a cylinder set down on its side, and the Weight resting on its side do not creep. Probe source and log: `evidence-10-2a/probe/`; entry check: the 10.2a roll test with the drop ramp goes red (`evidence-10-2a/mutations/m10-drop-ramp-creep-discrimination.log`, `m11-drop-ramp-post-stop-check-only.log`).
  destination: its own engine slice, next after 10.2a (entry check: the drop-ramp case); 10.2a keeps the flush-ramp roll test. RESOLVED by the engine slice "lying cylinder axial creep" (`spec-engine-cylinder-creep.md`, commits 02593110 and 60d6a14c): the friction row resists only the rolling axes of each manifold's curved supports, and every normal row takes one rule with a manifold-coherent slop band, so a lying cylinder dropped, set down level or set down tilted rests without walking and the 10.2a Weight's flush-ramp roll runs straight (`WorkshopPhysicsLawCylinderTests`; Resting Drift row of `docs/gpu-f32-physics.md`). The residuals it measured moved to the next entry.

- source_spec: `_bmad-output/implementation-artifacts/spec-engine-cylinder-creep.md` (owner decision "Land M1+M2, follow-up", 25 Oct 2026)
  summary: The axial-creep residuals, split (coordinator, 25 Oct 2026) into three engine slices. (a) M3, sub-f32-step orientation levelling: a residual tilt below about 5 × 10⁻⁶ rad whose per-substep levelling increment is below the ulp of the quaternion's large components is never levelled while the contact rows keep asking for the levelling rock, so the centre walks along the axis at up to 2.2 × 10⁻⁵ m/s (180 of 200 random lying drops, 126 of them with the rotation bit-frozen). (b) D4, the drop-ramp landing yaw kick: the 10.2a Weight landing on its side from the 15° drop ramp takes a one-off yaw of about 1.8 × 10⁻⁴ rad from unequal friction at the two generator ends (dependent on the friction rows' solve order), 1.25 mm off straight over 7 m. (c) The rank-3 cap block: a cap resting on a plane gives four coplanar points whose normal Jacobians span three motions, so `SolveNormalBlock`'s 4×4 coupling matrix is invertible only through its diagonal regularisation and returns a large alternating null-space solution that falls back to the sequential per-row solve (52–62 % of block solves in the rod diagnosis).
  evidence: Measurements and traces in the draft spec [`spec-engine-orientation-levelling-draft.md`](spec-engine-orientation-levelling-draft.md), which covers all three slices; recorded as an open defect, not an accepted bound, in the Resting Drift row of `docs/gpu-f32-physics.md`. The four tests the predecessor slice removed red are restored by the owning slices with unchanged bounds; none is committed red or given a loosened bound.
  destination: (c) `engine-cap-block` (in progress, spec `spec-engine-cap-block.md`), (b) `engine-two-point-friction` and (a) `engine-orientation-levelling`, in the order of [epics.md engine and collider slices](../planning-artifacts/epics.md#engine-and-collider-slices), with `elements-visible-solid` and `engine-hollow-shapes` between the first and the second; all before 9A.44B.

- source_spec: `_bmad-output/implementation-artifacts/spec-engine-cylinder-creep.md` (review, deferred)
  summary: The prismatic `PrismaticConstraintSolver.SweepConstraints(List<SimJoint> joints, bool useBias)` takes a bool for a closed set (bias on in the solve pass, off in the relax pass), against the AGENTS enum rule.
  evidence: `CuriousContraptions.Simulation/PrismaticConstraintSolver.cs:118`; callers `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:888` (true) and `:952` (false) and `CuriousContraptions.tests/WorkshopPhysicsLawPrismaticTests.cs:257-267`. Pre-existing; flagged by the Blind Hunter and Edge Case Hunter in the creep review.
  destination: the first slice that changes the prismatic solver (unassigned until then): a closed-set enum end to end, callers and tests together.

- source_spec: `_bmad-output/implementation-artifacts/spec-engine-cylinder-creep.md` (Edge Case Hunter, 25 Oct 2026; maybe-false)
  summary: A single-point contact on one curved cylinder side resists rolling only about its own axis, so pitch is left without a second normal row. If true it would be medium; it is unverified.
  evidence: `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs` `RollingAxis`; the spec's deferred list. Settle it by measuring whether a tilted lying cylinder at a single-point contact levels or rocks differently with both tangents resisted.
  destination: `engine-orientation-levelling`, which measures single-point levelling; if it is confirmed, that slice fixes it under the one rolling-axis rule.

- source_spec: AGENTS.md "tests assert behaviour, not hashes" (owner, 24 Oct 2026); coordinator count, 25 Oct 2026
  summary: About 57 test files still use hash or byte-identity patterns, including the save re-encode asserts `Assert.Equal(bytes, WorkshopSaveCodec.Encode(reloaded))` at `CuriousContraptions.tests/ContactWorkInstanceTests.cs:102` and `CuriousContraptions.tests/SliderInstanceTests.cs:145`.
  evidence: A pattern grep at d87b6a1e (SHA-256/hash helpers, byte-sequence equality, bit- or byte-identical wording) finds 52 C# test files and 7 node test files; each still needs reading to tell an output pin from an allowed fixture-integrity or input use.
  destination: `test-behavioral-hygiene` (epics.md stabilization contracts), which inventories every removed assertion and its behavioural replacement.

- source_spec: `_bmad-output/implementation-artifacts/spec-ci-harness-determinism.md` (review pass 1)
  summary: Remaining wall-clock waits in the game-runtime node harnesses (docs/testing-guidance.md rule 7: no wall-clock sleeps). They are `tools/workshop-animation-clock.test.mjs`'s 20 ms `settle`, the 5 ms retry pause inside `tools/workshop-clock-ring.test.mjs`'s bounded exchange loop, and `tools/workshop-observation.test.mjs`'s `until`, which polls a positive condition in 1 ms steps under an attempt deadline. Each should wait on the deciding event or a deterministic quiescence point instead.
  evidence: Not CI failures today. The two that failed (client-lifecycle test 15, observation test 1) and the observation absence checks were made event-driven in this slice.
  destination: the JS-to-TypeScript conversion slice (AGENTS.md TypeScript rule), which rewrites these harnesses.

- source_spec: `_bmad-output/implementation-artifacts/spec-engine-cap-block.md` (HALT, 25 Oct 2026)
  summary: The soft push-out of a deep single-point penetration gains energy. In `FreshSeedDropsGainNoMoreEnergyThanTheBoxControlAndTheSlop`, CylinderOntoBox (Dead) case 179 gains 2.447 mJ in one tick at tick 97. The only contact that tick is a single cylinder-rim-on-box-edge point (feature 0x4042, normal ≈ (0.73, 0.68, −0.09)), 6.5–7.0 mm deep. The solve pass climbs 0.034 → 0.043 N·s per substep (approach −0.12 to −0.15 m/s), and the relax pass falls 0.0087 → 0. This trajectory is only reached when the block solve changes earlier ticks; on main the case stays within its bound.
  evidence: The cap-block attempts (`engine-cap-block-attempt.patch`, spec Change Log iterations 1–2) show it under every block design, while every contact in the jump tick is single-point. Settle it by bounding the soft push-out velocity for deep single-point penetrations to the envelope (`docs/gpu-f32-physics.md` energy bound) and proving the gate on the trajectory.
  destination: `engine-single-point-penetration` ([spec](spec-engine-single-point-penetration.md)), before friction resumes and cap-block retries. The retained friction candidate reproduced 2.408196586 mJ > 2.244929245 mJ. The derived 0.012 m/s normal recovery candidate passes 258 retained-candidate integration cases, including exact case 179, unchanged full energy gates and original/reversed drop controls; full native proof, Auto review and publication remain pending. The earlier cap-block evidence above remains historical, not a waiver.

- source_spec: `_bmad-output/implementation-artifacts/spec-engine-cap-block.md` (HALT, 25 Oct 2026)
  summary: Under the minimum-norm block, box-on-box contacts gain energy over the slop, which raises the gate's own control bound: Dead to 3.156 mJ and Rolling to 6.077 mJ, against about 2.0 mJ before. Resting faces also drift (4 of 60 boxes over 1e-9 m/s), because load distribution moves the per-point friction limits.
  evidence: Spec Change Log iteration 2; `engine-cap-block-attempt.patch`.
  destination: `engine-two-point-friction` (a central friction anchor for multi-point manifolds), then the `engine-cap-block` retry.

- source_spec: `_bmad-output/implementation-artifacts/spec-elements-visible-solid-sweep.md`
  summary: Per-instance collider growth from visible = solid. Static colliders per instance rose from 2 to 15 for the Counter, to 10 for each of the five logic gates, to 9 for the Latch, to 7 for the Hold timer and the Clock, and to 5 for the Pressure plate. Broadphase, narrowphase pair count and compiled table size grow with them.
  evidence: Not measured. A bench of many logic parts is the worst case.
  destination: the named performance qualification gate (P0-034), which profiles affected workloads.
