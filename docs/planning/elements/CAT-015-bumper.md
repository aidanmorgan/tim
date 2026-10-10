# CAT-015 · bumper (Pinball bumper) — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-015 · `bumper` (display name Pinball bumper) |
| Requirement | [CAT-015](../requirements.md#current-cat-015) |
| Mapped identities | EL-195 Pinball bumper |
| Roadmap | Stories 6.2 (CAT-015a radial impulse and finite work store), 6.3 (CAT-015b multi-angle contacts, bumper_depth, wall_and_bumper). Already shipped: the "Bumper demo" contact-work payout (ENGINE-CORE-2a2) and the squash/ring cosmetic (Story 4.2). |
| Status | partial |
| Levels | placed in wall_return; inventory in bumper_sidekick, bumper_depth, wall_and_bumper; see [CAT-015-I consumers](../invest/current-consumers.md#cat-015-i) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | One static body with one static sphere collider, radius 0.65 m `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L119-L126`; full pose `engine/gpu/WorkshopBumper.cs@a6c914e:L29-L39`. |
| Material | restitution 1, bounce threshold 0.1 m/s, friction 0.3 `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L107-L110`. |
| Constraints and joints | none (static head). |
| Sockets and ports | none at the pinned baseline `engine/gpu/WorkshopConnections.cs@a6c914e:L18-L26`. Owner decision (9 Oct 2026): add a typed recharge connection in Story 6.2. Current integration declares electrical PowerIn supplied by Battery Supply, with one supplier per store and bounded source fan-out. Default source limit is120 W at120 Hz; default store capacity32 J. This is new work, not a claim that baseline ports existed. |
| Sensors and activation | Contact qualified by approach speed ≥ 0.05 m/s `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L123-L125`. |
| Work and energy stores | One finite `ContactWorkDeclaration` per bumper: target speed = strength, initial energy = preload, threshold 0.05 m/s, per-target cooldown 72 physical steps (0.15 s at 480 Hz); one reservoir shared by all targets; occurrences Passive or Paid `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L19-L49`. Owner decision (9 Oct 2026): when remaining energy cannot pay the full requested boost, apply only the affordable partial boost and debit its actual positive work; zero energy leaves ordinary contact bounce. A connected source may recharge the finite store only by supplying accounted work. |
| Parameters | strength 0–20 m/s, default 8; reference mass fixed 1 kg; preload = ½ · 1 kg · strength² (default 32 J) `engine/gpu/WorkshopBumper.cs@a6c914e:L5-L28`; resource bits `engine/BumperWorkResource.cs@a6c914e:L8-L21`. |
| Cosmetic curves and UI bindings | ContactWork source, linear, 0.32 s, sine-squared pulse, saturating-sum overlap `engine/gpu/WorkshopCosmetic.cs@a6c914e:L56-L57`; head uniform scale 1 → 0.88; impact ring scale 1 → 1.24, colour `#fff0c2` → `#f7cb52` `parts/BumperPart.cs@a6c914e:L25-L30`. Design rule `DESIGN.md@a6c914e:L301-L301`. |
| Art | `parts/BumperPart.cs@a6c914e:L13-L31`: coral head sphere r 0.65; slate `#273744` pedestal cylinder r 0.3 h 0.12 at y −0.46; gold `#f7cb52` cap r 0.25 h 0.12 at y 0.59; impact ring r 0.66 with half thickness 0.035; small ring r 0.49 with half thickness 0.018 at y 0.43 (torus inner/outer radius = r ∓ half thickness `engine/PartArt.cs@a6c914e:L22-L23`); pick radius 0.8. Scene `parts/scenes/bumper.tscn`. Palette Pinball bumper `#de7059` `DESIGN.md@a6c914e:L177-L177`. Icon `ui/WorkshopIcons.cs@a6c914e:L49-L49`. |
| Catalogue and inventory | `parts/catalog/bumper.tres@a6c914e:L8-L22`: id `bumper`, title Pinball bumper, category Motion, strength 8, reference mass 1, preload 32. |

Current tests: `CuriousContraptions.tests/WorkshopBumperTests.cs` (sphere and preload compile, round trip, owner/target rules, strength bounds), `WorkshopActivationAnimationTests.cs` (contact pulse, impulse envelope overlap), `BasketballResourceTests.cs` (bumper artwork); Chrome `tools/e2e/anim-1b.test.ts` (squash pulse; un-struck bumper emits nothing), `engine-core-2a2.test.ts` (payout).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): AnimationEvaluation, AnimationLifecycle, ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, SignalPropagation, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| ContactImpulse and a finite contact-work reservoir (FiniteWorkActuation, FiniteLedger) | `engine/gpu/ContactWorkDeclaration.cs`, `engine/gpu/PhysicsContactWorkRead.cs`, `CuriousContraptions.Simulation/wwwroot/worker.js` |
| AnimationEvaluation/Lifecycle (impulse envelope) | Story 4.2 |

| Missing | Story that builds it |
| --- | --- |
| Root integration and sidekick acceptance of the independently reviewed paid radial impulse/generic finite store | Story 6.2; isolated model and recharge prerequisite passed, integrated proof pending |
| Glancing/multi-angle qualification; bumper_depth and wall_and_bumper | Story 6.3 |
| SignalPropagation / JointConstraint memberships (no declared mode uses them yet) | owner decision (§6) |
| Physical placement nudging (authored placement correction that moves the bumper's collider; legacy fact 5) | not scheduled, owner decision: the roadmap reports physical nudging unsupported in the current playable mode ([first_principles](../invest/vertical-delivery.md#first-principles)) |
| Remaining sphere geometry, pose and cosmetic lanes retain inherited binary16 precision. Current integration migrates strength, reference mass, preload, contact speeds/debits and resource authoring to f32; `BumperWorkResource` is retired for generic `ContactWorkResource`. | Remaining shared lanes: Epic16 ([f32 migration status](../../gpu-f32-physics.md#f32-migration-status)); affected contact-work integration proof: Story6.2 |

Dependencies: a dynamic body; Wall (CAT-066) for wall_return and wall_and_bumper.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A ball approaching at 4 m/s along any of the six axes (bumper rotated 30/40/20°) records exactly one hit, leaves with radial speed > 7.99 m/s along the contact normal, ends outside radius + 0.65, and records a Bumped event; the bumper has one sphere and no boxes. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L35-L71` | carry forward the radial-launch behaviour and target speed 8 m/s; the energy source must be the declared finite store (requirement), not a free impulse. |
| 2 | A glancing contact (velocity (2, −4, 0)) transfers tangential momentum into solid-sphere spin (spin > 1 rad/s) and the radial launch does not undo it; a ball in another depth plane (z = 2) never triggers. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L73-L113` | carry forward. |
| 3 | Cooldown is per ball: a re-strike 3 ticks after a hit is suppressed but still gets ordinary collision response; a second ball can trigger during the first ball's cooldown; after 18 more ticks the first ball triggers again. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L115-L171` | carry forward (0.15 s per-target cooldown, current 72 physical steps). |
| 4 | Impact ring: at 0.16 s of a pulse the ring scale is 1.2376–1.24; a second overlapping hit does not reset the current ring value; the ring returns to scale 1 after the pulse; render feedback continues while physics is stopped and never changes the collider or bodies; Reset clears hit count, Active and ring scale. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L151-L177` | carry forward (current impulse envelope). |
| 5 | Placement nudging alone rescues an imperfect bumper at (−3.110, 1.500, 0.035) in bumper_sidekick at precision 0 and 0.45 (corrected to (−3.2, 1.5, 0)) but not at 1; the bumper does not move at Run start; Reset restores the placed pose. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L182-L219` | carry forward as the authored-assistance control; depends on physical placement nudging, which is not scheduled (§3, §6). |
| 6 | Stationary and separating (2 m/s away) touching balls do not trigger; a 4 m/s strike replays identical state signatures after Reset. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L221-L254` | carry forward. |
| 7 | Contact with approach speed 2 m/s is accepted, 0.01 m/s is rejected; mid-pulse (0.08 s) ring scale is 1.12 and colour is halfway to `#f7cb52`; a rejected hit produces no cosmetic pulse; hidden bumpers freeze presentation; Reset and Load restore the baseline. | `CuriousContraptions.tests/BumperOccurrenceTests.cs@a6c914e:L25-L66` | carry forward; do not carry forward the scene `ObserveContact` callback (per-element update loop). |
| 8 | 64 occurrences in one tick are accepted; 65 reject the whole tick atomically; draining completed occurrences allows the next. | `CuriousContraptions.tests/BumperOccurrenceTests.cs@a6c914e:L82-L108` | carry forward the atomic-overflow rule; the capacity 64 is a legacy bound (current capacity is set by the contact-work wire). |
| 9 | An impact effect sees the ordinary contact response first, cannot push a body back through the contact that triggered it, uses the same angular-momentum update off centre, and persistent contact never retriggers while release and re-contact does. | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L175-L230`; `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L303-L315` | carry forward the behaviours; do not carry forward the CPU impact-effect callback path. |
| 10 | Reference layouts: bumper_1 at (−3.2, 1.5, 0) left of a ball at (−3, 5, 0), receiver at (2, 0.9, 0); wall_return uses the same bumper fixed; wall_and_bumper places both. | `tools/Campaign/Program.cs@a6c914e:L93-L123` | carry forward (content in `content/puzzles.json`; bumper_depth is the depth-rotated variant at (0, 1.5, 3.2)). |
| 11 | Authoring: bumper rotation step 0° (no rotation correction), position step 0.25 m. | `tools/Campaign/Program.cs@a6c914e:L544-L545` | carry forward the values. |
| 12 | Launch in the surface frame: for a payload at the contact point (0, 6, 0) above a moving bumper centred at (0, 5, 0) with incoming velocity (4, −1, 3) relative to the surface point velocity, the bumper's single impact command leaves the payload with relative velocity (4, Strength, 3): the radial component becomes Strength and the tangential components are kept, all measured relative to the surface's point velocity. The command does not mutate either body's snapshot and repeats identically; a NaN point throws. | `CuriousContraptions.tests/ImpactFrameTests.cs@a6c914e:L100-L137` | carry forward the carrier-relative radial launch with tangential components kept, and the non-mutating, repeatable command; do not carry forward the throw on a NaN point (game-grade envelope: residuals clamp or continue, never fault the tick) or the CPU impact-callback path (fact 9). |
| 13 | Zero gravity, ball 1.1 m to the side of a bumper placed 0.2 m short of its placement target, which lies toward the ball (correction cap 0.3 m, blend 0.4 s at precision 0; cap 0 at precision 1): at precision 0 the bumper frame is kinematic, its correction blend strikes the ball and launches it faster than Strength along +x (hit count > 0); at precision 1 the frame is static, nothing hits and the ball keeps its pose and zero velocity; Reset restores the saved machine and a second Run replays identical body states. | `CuriousContraptions.tests/ImpactFrameTests.cs@a6c914e:L16-L86` | carry forward the precision-1 control and the exact Reset replay; do not carry forward the strike from correction motion: it conflicts with fact 5 (the bumper does not move at Run start), and placement correction moving a frame during the Run is a legacy mechanism. |

Files harvested:
- `CuriousContraptions.tests/BumperTests.cs`
- `CuriousContraptions.tests/BumperOccurrenceTests.cs`
- `CuriousContraptions.tests/PhysicsImpactEffectTests.cs`
- `CuriousContraptions.tests/ImpactFrameTests.cs`
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance: [CAT-015](../requirements.md#current-cat-015) and [retained behaviour](../requirements.md#todo-157).

- **Construction.** bumper_sidekick: place the bumper just left of the falling ball; wall_and_bumper: place bumper and wall.
- **Positive.** Radial redirection at the actual contact point paid from the declared finite store; overlapping gold rings.
- **Negative/control.** Missed, grazing (another depth plane), resting and separating contacts, repeated return within cooldown, exhausted store gives ordinary bounce with no paid pulse; insufficient but positive energy gives only an affordable partial boost; rejected hit has no pulse. Disconnected recharge never creates stored energy.
- **Boundaries.** Strength 0 and 20; per-ball cooldown edge.
- **Run/Reset, Save/Load.** Store, cooldowns and rings reset; strength persists.
- **Integrations.** Contact and cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (bumper); wall and bumper teaching [sequence-task-304](../requirements.md#sequence-task-304) and [todo-163](../requirements.md#todo-163); [IX-01 contact impulse](../requirements.md#interaction-01); campaign first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage) (physical presets row). Partners: Wall (CAT-066) in wall_and_bumper and wall_return, Receiver (CAT-004).
- **Remaining (unmet now).** Stories 6.2 and 6.3 in full; the model/energy decision is resolved and the isolated model has scoped Pass; the "isolated authored assistance" control (fact 5) needs physical placement nudging, which is not scheduled.

## 6. Open questions

- The requirement's "declared finite spring/work storage" and Story 6.2's "only when powered or charged": owner decided on 9 Oct 2026 to add a recharge connection now in Story 6.2. The reviewed direct-source prerequisite uses Battery Supply→Bumper PowerIn, one supplier per store, bounded fan-out and finite120 Hz transfers. It supplies the same finite store rather than introducing an alternate actuator path; full named-element acceptance remains unclaimed. Owner approved Battery defaults on 10 Oct 2026: 3,600 J, 120 W, initially full/enabled, energy/power model; isolated recharge implementation has scoped Pass; root integration proof remains pending.
- Reservoir sizing: the current default pays 32 J shared by all targets (one full-speed launch of a 1 kg reference mass). Owner resolved insufficient energy: affordable partial boost, and at zero energy ordinary bounce. Current integration retains strength-derived store capacity (32 J at8 m/s) and approved source defaults3600 J/120 W. Realized finite source debit funds bounded credits; these choices are resolved.
- Physical placement nudging: not scheduled, owner decision. The requirement keeps "isolated authored assistance" as a bumper control, but the roadmap reports physical nudging unsupported; which story builds it, or whether the control is proven another way, is unspecified.
