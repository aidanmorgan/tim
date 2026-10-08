# Deferred work

- source_spec: `_bmad-output/implementation-artifacts/spec-4-2-mechanical-cosmetic-bindings.md`
  summary: ControlHint throws "An animation control is pending" when the single animation lease is busy (hint button, guidance visibility).
  evidence: Code trace Schedule.cs:127-141; pre-existing single-lease design from Story 4.1; reduced by the timer resend fix but not eliminated. Resolve in ANIM-1c or a lease-queue slice.
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
