# CAT-015 · bumper (Pinball bumper) — declaration readiness spec

**Status (22 November 2026).** Delivered: package `elements/Bumper`, id `bumper`. Sections 2 and 3 are read from the package and its tests in this checkout (`path:line`; tests are in `CuriousContraptions.tests/`). One control is owed (the isolated authored-assistance control, Story 15.0); the head's 0.32 s squash is an accepted cosmetic exception (owner decision 8, 22 November 2026). Every residual slice, including the EL-195 closure review, is blocked on the engine programme (owner decision 3a, 22 November 2026); the part's engine gaps are owned by `ENGINE-CONTACT-OBSERVATIONS` (contact-work rows found by compiled handle, on any owner) and `ENGINE-TICK-ALLOCATION` (cooldown state in reusable tables). Browser qualification is pending until the post-catalogue E2E phase. Citations of the form `path@a6c914e` in section 4 are the legacy harvest, not current instructions.

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-015 · `bumper` (display name Pinball bumper) |
| Requirement | [CAT-015](../requirements.md#current-cat-015); [retained behaviour](../requirements.md#todo-157) |
| Mapped identities | EL-195 Pinball bumper ([EL-195 spec](EL-195-pinball-bumper.md)) |
| Disposition | Delivered. Stories 6.2 (radial impulse and finite work store) and 6.3 (multi-angle contacts, `bumper_depth`, `wall_and_bumper`) have terminal scoped Pass; the generic conversion is C-Bumper+Spring; the cap collider is `bumper-cap-solid` (published `057df5ae`) |
| EL-195 order | EL-195's part check runs in the "Launch parts" closure review after the engine programme (owner decision 3a); the CAT-015 requirement row and the EL-195 ledger row are ticked only after Story 15.0's isolated-assistance control |
| Levels | inventory in `bumper_sidekick`, `bumper_depth`, `wall_and_bumper` (Playable); no level places it as a fixture since `wall_return` was removed (owner decision 24d); see [CAT-015-I consumers](../invest/current-consumers.md#cat-015-i) |

## 2. Declaration

Read from `elements/Bumper/BumperElementDeclaration.cs` (cited as `:line`) and `elements/Bumper/bumper.tscn` (cited as `tscn:line`).

**Interfaces** (`:14`): `IContactWorkElement`, `IEnergyStoreElement`, `ISupplyElement`, `ISceneGeometryElement`. Domains Mechanical and Electrical (`:63`). Kinematic mode Static (`:65`); footprint one body, none moving, no constraints (`:47`); mass frame zero (`:69-75`).

**Catalogue** (`:25-33`): id `bumper`, category Motion, palette order 7, colour (0.87, 0.44, 0.35), display name "Pinball bumper".

**Body, colliders and material.** One static body. Material (`:58`): restitution 1, friction 0.3, rolling resistance 0, bounce threshold 0.1 m/s.

| Collider | Shape | Dimensions (m) | Centre (m) | Source |
| --- | --- | --- | --- | --- |
| `col_0_0` | Sphere | radius 0.65 | (0, 0, 0) | `tscn:43-44`, `:73-77` |
| `col_ring2` | Torus | centreline radius 0.49, tube radius 0.018 | (0, 0.43, 0) | `tscn:38-41`, `:79-82` |
| `col_cap` | Cylinder | radius 0.25, height 0.12 | (0, 0.59, 0) | `tscn:27-31`, `:84-87` |

**Parameter** (`:36`).

| Name | Type | Unit | Default | Minimum | Maximum | Editor policy | Run edit |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `strength` | Real | m/s | 8 | 0 | 20 | Clamp | BuildOnly (the default, `CuriousContraptions.Abstractions/ParameterDescriptor.cs:61`) |

**Port** (`:51`).

| `PortId` | Role | Domain | Direction | Signal kind | Position (m) |
| --- | --- | --- | --- | --- | --- |
| `PowerIn` | PowerIn | Electrical | Input | none | (0, 0, 0.65), facing +Z |

**Contact work** (`:39-40`). One `ContactWorkDescriptor`: id `work`, owner body 0, store 1, target speed from `strength`, qualifying approach speed 0.05 m/s, per-target cooldown 72 physical steps (0.15 s at 480 Hz). All three colliders belong to body 0, so they share the row, the store and the cooldown.

**Energy store** (`:42-45`). One `EnergyStoreDescriptor`: id 1, owner body 0, capacity and initial energy both ½ · 1 kg · `strength`² (32 J at 8 m/s, 0 J at 0, 200 J at 20 m/s). Its declared maximum charge power is `float.MaxValue`: the store sets no charging limit.

**Supply** (`:79-81`). One consumer node `consumer` whose target is store 1, bound to `PowerIn`. **Electrical node** (`:83-86`): `coil`, role Consumer, 12 V, 10 Ω, 0 F, 50 W. The 50 W is metadata and does not bound charging; the refill rate is the source's `power_limit` (Battery default 120 W, `elements/Battery/BatteryElementDeclaration.cs:44`). `ContactWorkInstanceTests.SuppliedContactStoreRetainsSourceLimitedChargingAboveFiftyWatts` proves 1 J per 1/120 s tick at 120 W, so a default 32 J store refills in about 0.27 s.

**Event** (`:61`): `Impact`.

**Presentation bindings** (`:16-23`). Pick radius 0.8 m. Five bindings, all driver `Event` on `Impact` with positive magnitude, curve SineSquaredPulse over 0.32 s with its peak at 0.5, overlap SaturatingSum, timed at the event's own physical time, advancing while hidden.

| Target node | Property | From | To |
| --- | --- | --- | --- |
| `Visual/BumperHead` | UniformScale | 1 | 0.88 |
| `Visual/ImpactRing` | UniformScale | 1 | 1.24 |
| `Visual/ImpactRing` | AlbedoRed | 1 | 247/255 |
| `Visual/ImpactRing` | AlbedoGreen | 240/255 | 203/255 |
| `Visual/ImpactRing` | AlbedoBlue | 194/255 | 82/255 |

**Numbers.** The default store is 32 J: one full 8 m/s launch of a 1 kg reference mass, which is the Basketball. A launch of the 4 kg Bowling ball would need up to 128 J and so always takes a partial boost; the 0.35 kg Tennis ball takes up to 11.2 J and the 0.15 kg Baseball (the repair values of coordinator decision 36) up to 4.8 J, so both take a full launch from a full store. The maximum target, 20 m/s, is inside the engine's 64 m/s speed envelope.

## 3. Engine capabilities

Capability probe, 22 November 2026. Counts: 6 present, 0 present but untested, 0 narrower, 1 missing.

| Capability | Status | Symbol | Test or owner |
| --- | --- | --- | --- |
| Finite contact-work row paying a normal boost up to a target speed from a store, with an approach threshold and a per-pair cooldown | present | `ContactWorkDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:217`), compiled by `WorkshopPhysicsCompiler.ContactWorkFor` (`engine/gpu/WorkshopPhysicsCompiler.cs:620`), paid by `ContactWorkImpulse.FromStore` (`engine/gpu/ContactWorkImpulse.cs:22`; called at `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:199`) | `ContactWorkInstanceTests.StrengthEightLaunchesTheBallPaidAtTheTargetSpeedAndTheLedgerCloses`, `.APartiallyAffordableBoostEmptiesTheStoreAndALaterContactIsAnOrdinaryUnpaidBounce`, `.StrengthEndpointsHoldAnEmptyAndATwoHundredJouleStore`, `.ABallInAnotherDepthPlaneNeverPays` |
| Several colliders on one static body share one row, store and cooldown | present | the row is keyed by owner body (`WorkshopPhysicsEngine.cs:165-169`) | `ContactWorkInstanceTests.ExposedCapContactsUseTheFiniteStoreAndOnlyPaidWorkEmitsAPulse`, `.ARealCapThenRingContactSharesTheCooldownEvenWithRechargedWorkAvailable`; `VisibleSolidColliderTests.ABallDroppedOntoTheBumpersUpperRingStopsAtItsDrawnTube`, `.TheExposedCapFaceAndRimStopAndRedirectTheBall`, `.ABallOutsideTheBumperSolidsFallsPastWithoutWorkOrPulse` |
| A supply consumer recharging a declared store from a source, limited by the source | present | `SupplyConsumerDescriptor`, `SupplyEnergyStoreTarget` (`CuriousContraptions.Abstractions/SupplyContractValues.cs:42`, `:51`) | `ContactWorkInstanceTests.ADirectlyWiredBatteryRechargesTheStoreAndAnUnwiredOneDoesNot`, `.SuppliedContactStoreRetainsSourceLimitedChargingAboveFiftyWatts`, `.SwitchedStoreRoutingIsAdmittedAndBindsStorage` |
| Paid work publishes one `Impact` record, and event bindings pulse from it in the animation worker | present | `PresentationBinding` with `PresentationDriver.Event` (`CuriousContraptions.Abstractions/ElementPresentation.cs:100`) | `EventRingProducerTests.EachBumperHitPublishesExactlyOneImpactAtItsOwnTimeAndNoTrigger`, `EventRingConsumerTests.EachPaidBumperImpactCuesExactlyOnePulseAtItsOwnPhysicalTime` |
| Run/Reset and save/load of store and parameter; editor clamp and typed refusal | present | generic instance parameters and store state | `ContactWorkInstanceTests.RunResetAndSaveLoadAreExact`, `PalettePlacementTests.GenericBumperEditorClampsAFiniteStrengthAndRefusesNaNAtomically`, `WorkshopBumperTests.StrengthBoundariesAndPopulationRejectAtomically` |
| The three bumper lessons solve, and each stays unsolved without its cause | present | lesson content in `content/puzzles.json` | `LaunchLessonSolveTests.AuthoredLessonSolvesAndOneMissingCausePreventsTheGoal`, `.EachLaunchRouteSolvesOrStaysUnsolvedAndResetRestoresTheBench` (routes SidekickPaid, SidekickUnpaid, DepthPaid, DepthMiss, WallPaid, WallMiss at precision 0, 0.45 and 1) |
| Physical placement nudging (the "isolated authored assistance" control, legacy fact 5) | missing | none | [Story 15.0](../../../_bmad-output/planning-artifacts/epics.md#story-150-physical-placement-nudging--difficulty-assistance). Its text names only CAT-049 and CAT-050 today; the Batch A summary proposes adding this control (audit G2) |

The element map lists SignalPropagation and JointConstraint for this part; no declared mode uses them. Under D14 the map is navigation only and this probe governs.

### Engine conformance

**Compiled counts.** Bodies 1 (static). Colliders 3: 1 sphere, 1 torus, 1 cylinder. Joints 0. Drives 0. Sensors 0; contact-work rows 1. Domain nodes 2: one supply consumer node, one electrical node (metadata). Stores 1. Presentation bindings 5 (event-driven). Events 1 kind. All counts are fixed at compile time.

**Art and layers.**

| Art node | Mesh (m) | Classification | Moved by |
| --- | --- | --- | --- |
| `Visual/BumperHead` | sphere r 0.65, coral (`tscn:16-19`) | solid, collider `col_0_0` | nothing (static body). The impact binding scales it to 0.88 for 0.32 s: an accepted cosmetic exception (owner decision 8, 22 November 2026) |
| `Visual/Visual_Cap` | cylinder r 0.25, h 0.12 at y 0.59, gold (`tscn:27-31`, `:58-60`) | solid, collider `col_cap` | nothing |
| `Visual/Visual_Ring2` | torus, inner 0.472, outer 0.508 at y 0.43, cream (`tscn:38-41`, `:66-68`) | hollow (torus collider `col_ring2`) | nothing |
| `Visual/Visual_Base` | cylinder r 0.3, h 0.12 at y −0.46, slate (`tscn:21-25`, `:54-56`) | decorative, enclosed: at its lowest face (y −0.52) the head's radius is 0.39 m against the base's 0.3 m | nothing |
| `Visual/ImpactRing` | torus, inner 0.625, outer 0.695 at the equator, cream (`tscn:33-36`, `:62-64`) | decorative, moved by a binding (scale and colour) | presentation bindings |

The table agrees with `VisibleSolidColliderTests.cs:131`. No open visible-solid finding.

**Generic laws.**

| Behaviour | Law | Declared values |
| --- | --- | --- |
| Ordinary bounce and glancing spin | Contact with the restitution pair law and Coulomb friction | restitution 1, bounce threshold 0.1 m/s, friction 0.3 |
| Radial kick | Contact-work law: on a contact of the owner body whose normal approach speed is at least the threshold, and whose pair is outside its cooldown, an added normal impulse raises the separating speed to the target, limited by the store balance; tangential velocity is untouched | target `strength`, threshold 0.05 m/s, cooldown 72 steps, store ½ · 1 kg · `strength`² |
| Partial boost, then plain bounce when empty | The same law with a smaller balance; a store below its minimum payable work never pays | store capacity and balance |
| Recharge | Supply network rule: a consumer bound to a store draws realised finite transfers from one source, at most the source's power limit per tick | consumer → store 1; the source's `power_limit` |
| Ring and head pulse | Event binding law: each positive `Impact` record adds one pulse at its physical time; overlapping pulses saturate | the five bindings |
| A ball in another depth plane gets nothing | No contact, so no row is evaluated | collider geometry |

**Why three colliders.** Visible = solid: the head, the upper ring and the cap are each drawn proud and each has its own collider of the drawn shape. The base is enclosed and has none.

**Performance fitness.** The body is static. No sensor or field volume. Tunnelling is excluded by the engine's general means, 480 Hz substeps with speculative contact margins (envelope: no penetration of a solid at least 1 cm thick at up to 64 m/s, `docs/gpu-f32-physics.md:375`); the thinnest collider here is the 3.6 cm ring tube. Engine gap, not an element fault: for each static-dynamic contact point the engine scans every contact-trigger row and every contact-work row for a matching owner (`WorkshopPhysicsEngine.cs:141`, `:165`) and keeps the per-pair cooldown state in an `ImmutableDictionary` builder (`:1323`). That is work proportional to contacts times declared rows, and managed state inside the tick. Owned by `ENGINE-CONTACT-OBSERVATIONS` (rows found by compiled handle) and `ENGINE-TICK-ALLOCATION` (cooldown state in reusable tables). The part is not designed around it.

**No engine code names the package.** The audit's engine search found no id literal, file or branch for `bumper`. Residue: comments use the Bumper as an example (`engine/gpu/ContactWorkImpulse.cs:11`, `:26`; `CuriousContraptions.Abstractions/ContactWorkDeclarationValues.cs:56`); no finding. The `coil` node's voltage, resistance and power are read by no solver.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A ball approaching at 4 m/s along any of the six axes (bumper rotated 30/40/20°) records exactly one hit, leaves with radial speed > 7.99 m/s along the contact normal, ends outside radius + 0.65, and records a Bumped event; the bumper has one sphere and no boxes. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L35-L71` | carry forward the radial-launch behaviour and target speed 8 m/s; the energy source is the declared finite store (requirement), not a free impulse. Do not carry forward "one sphere": the part has three colliders (visible = solid). |
| 2 | A glancing contact (velocity (2, −4, 0)) transfers tangential momentum into solid-sphere spin (spin > 1 rad/s) and the radial launch does not undo it; a ball in another depth plane (z = 2) never triggers. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L73-L113` | carry forward. |
| 3 | Cooldown is per ball: a re-strike 3 ticks after a hit is suppressed but still gets ordinary collision response; a second ball can trigger during the first ball's cooldown; after 18 more ticks the first ball triggers again. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L115-L171` | carry forward (0.15 s per-target cooldown, current 72 physical steps). |
| 4 | Impact ring: at 0.16 s of a pulse the ring scale is 1.2376–1.24; a second overlapping hit does not reset the current ring value; the ring returns to scale 1 after the pulse; render feedback continues while physics is stopped and never changes the collider or bodies; Reset clears hit count, Active and ring scale. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L151-L177` | carry forward (current impulse envelope). |
| 5 | Placement nudging alone rescues an imperfect bumper at (−3.110, 1.500, 0.035) in bumper_sidekick at precision 0 and 0.45 (corrected to (−3.2, 1.5, 0)) but not at 1; the bumper does not move at Run start; Reset restores the placed pose. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L182-L219` | carry forward as the authored-assistance control; depends on physical placement nudging, owned by Story 15.0 (§3, §6). The poses are the legacy ones; the cap slice re-authored the Sidekick and Depth solutions, so Story 15.0 takes its poses from `content/puzzles.json`. |
| 6 | Stationary and separating (2 m/s away) touching balls do not trigger; a 4 m/s strike replays identical state signatures after Reset. | `CuriousContraptions.tests/BumperTests.cs@a6c914e:L221-L254` | carry forward. |
| 7 | Contact with approach speed 2 m/s is accepted, 0.01 m/s is rejected; mid-pulse (0.08 s) ring scale is 1.12 and colour is halfway to `#f7cb52`; a rejected hit produces no cosmetic pulse; hidden bumpers freeze presentation; Reset and Load restore the baseline. | `CuriousContraptions.tests/BumperOccurrenceTests.cs@a6c914e:L25-L66` | carry forward the threshold, the pulse values and the absent pulse on a rejected hit; do not carry forward the scene `ObserveContact` callback (per-element update loop) or the freeze while hidden: the delivered bindings advance while hidden (`elements/Bumper/BumperElementDeclaration.cs:18-22`). |
| 8 | 64 occurrences in one tick are accepted; 65 reject the whole tick atomically; draining completed occurrences allows the next. | `CuriousContraptions.tests/BumperOccurrenceTests.cs@a6c914e:L82-L108` | do not carry forward: the occurrence relay and its 64-row bound were deleted (Story 9A.25b). Impact records are published through the event ring, whose capacity is the ring's own contract, and no contact-work table has a fixed bound (`CuriousContraptions.Abstractions/ContactWorkDeclarationValues.cs:28`). |
| 9 | An impact effect sees the ordinary contact response first, cannot push a body back through the contact that triggered it, uses the same angular-momentum update off centre, and persistent contact never retriggers while release and re-contact does. | `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L175-L230`; `CuriousContraptions.tests/PhysicsImpactEffectTests.cs@a6c914e:L303-L315` | carry forward the behaviours; do not carry forward the CPU impact-effect callback path. |
| 10 | Reference layouts: bumper_1 at (−3.2, 1.5, 0) left of a ball at (−3, 5, 0), receiver at (2, 0.9, 0); wall_return uses the same bumper fixed; wall_and_bumper places both. | `tools/Campaign/Program.cs@a6c914e:L93-L123` | carry forward the lessons; the solution poses are those in `content/puzzles.json` (re-authored by the cap slice; bumper_depth is the depth-rotated variant). |
| 11 | Authoring: bumper rotation step 0° (no rotation correction), position step 0.25 m. | `tools/Campaign/Program.cs@a6c914e:L544-L545` | carry forward the values. |
| 12 | Launch in the surface frame: for a payload at the contact point (0, 6, 0) above a moving bumper centred at (0, 5, 0) with incoming velocity (4, −1, 3) relative to the surface point velocity, the bumper's single impact command leaves the payload with relative velocity (4, Strength, 3): the radial component becomes Strength and the tangential components are kept, all measured relative to the surface's point velocity. The command does not mutate either body's snapshot and repeats identically; a NaN point throws. | `CuriousContraptions.tests/ImpactFrameTests.cs@a6c914e:L100-L137` | carry forward the normal-only boost with tangential components kept (the law uses the contact's relative velocity). A moving owner has no consumer and is not admitted today: a contact-work row needs a static owner (`WorkshopBumperTests.WorkRequiresUniquePhysicalStaticOwnerAndDynamicTarget`); `ENGINE-CONTACT-OBSERVATIONS` lifts the static-owner requirement. Do not carry forward the throw on a NaN point (game-grade envelope: residuals clamp or continue, never fault the tick) or the CPU impact-callback path (fact 9). |
| 13 | Zero gravity, ball 1.1 m to the side of a bumper placed 0.2 m short of its placement target, which lies toward the ball (correction cap 0.3 m, blend 0.4 s at precision 0; cap 0 at precision 1): at precision 0 the bumper frame is kinematic, its correction blend strikes the ball and launches it faster than Strength along +x (hit count > 0); at precision 1 the frame is static, nothing hits and the ball keeps its pose and zero velocity; Reset restores the saved machine and a second Run replays identical body states. | `CuriousContraptions.tests/ImpactFrameTests.cs@a6c914e:L16-L86` | carry forward the precision-1 control and the exact Reset replay; do not carry forward the strike from correction motion: it conflicts with fact 5 (the bumper does not move at Run start), and placement correction moving a frame during the Run is a legacy mechanism. |

Files harvested:
- `CuriousContraptions.tests/BumperTests.cs`
- `CuriousContraptions.tests/BumperOccurrenceTests.cs`
- `CuriousContraptions.tests/PhysicsImpactEffectTests.cs`
- `CuriousContraptions.tests/ImpactFrameTests.cs`
- `tools/Campaign/Program.cs`

## 5. Acceptance outline

Acceptance: [CAT-015](../requirements.md#current-cat-015) and [retained behaviour](../requirements.md#todo-157). Every row runs the production simulation; none depends on a bumper-specific code path.

- **Construction.** `bumper_sidekick`: place the bumper just left of the falling ball; `wall_and_bumper`: place bumper and wall.
- **Positive.** Radial redirection at the actual contact point, paid from the declared finite store; overlapping gold rings.
- **Negative/control.** Missed, grazing (another depth plane), resting and separating contacts; a repeated return within the cooldown; an exhausted store gives an ordinary bounce with no paid pulse; insufficient but positive energy gives only the affordable partial boost; a rejected hit has no pulse. A disconnected recharge never creates stored energy.
- **Boundaries.** Strength 0 and 20; the per-ball cooldown edge.
- **Run/Reset, Save/Load.** Store, cooldowns and rings reset; strength persists.
- **Integrations.** Contact and cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285); wall and bumper teaching [sequence-task-304](../requirements.md#sequence-task-304) and [todo-163](../requirements.md#todo-163); [IX-01 contact impulse](../requirements.md#interaction-01); campaign first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage). Partners: Wall (CAT-066), Receiver (CAT-004), Battery (CAT-005).
- **Owed.** The "isolated authored assistance" control (fact 5), in Story 15.0.

## 6. Open questions

Resolved:

- **Finite store and recharge.** Owner, 9 October 2026: a typed recharge connection; when the store cannot pay the full boost it pays the affordable part, and at zero the contact is an ordinary bounce. Owner, 10 October 2026: Battery defaults 3,600 J and 120 W. Delivered by Story 6.2 as Battery `Supply` → Bumper `PowerIn`, one supplier per store.
- **Charging limit.** The `coil` node's 50 W is metadata and does not bound charging; the refill rate is the source's `power_limit` (audit H4, verified in section 2); `EL-195-pinball-bumper.md` (:35) computes its refill times from the same source limit.
- **Map families SignalPropagation and JointConstraint.** D14: navigation only.
- **EL-195.** Candidate Duplicate closure with one order (coordinator decision, 22 November 2026; reason: the ledger says EL-195 also retains CAT-015 acceptance): EL-195's part check runs in the "Launch parts" closure review after the engine programme (owner decision 3a); the CAT-015 requirement row and the EL-195 ledger row are ticked only after Story 15.0's isolated-assistance control.
- **Placement nudging.** Story 15.0 owns it; nothing here claims it.
- **Head squash (owner decision 8, 22 November 2026).** The head is a solid sphere with a collider and is scaled to 0.88 for 0.32 s by the impact binding, so for a third of a second the drawn head is up to 7.8 cm inside its collider. It is an accepted transient cosmetic exception (`DESIGN.md:360` asks for the pulse; nothing can rest in the gap during it). No decorative shell is added.

Needs the owner: nothing.

## 7. Integration

- **Collaborators.** Basketball, Bowling ball, Tennis ball and Baseball (payloads); Wall; Battery (recharge), directly or through an Impact switch; Receiver. All delivered.
- **Campaign.** `bumper_sidekick`, `bumper_depth`, `wall_and_bumper` (Playable) in `content/puzzles.json` (`wall_return` was removed under owner decision 24d); first use 1–10 in the [element coverage ledger](../requirements.md#campaign-element-coverage).
- **Coverage binding.** Catalogue key `bumper` in `docs/coverage/catalogue-elements.json`. A level that offers it from the player's inventory needs no lesson-fixture story; a level that locks it as a fixture adds a `FIX-` row to the catalogue fixture table ([current consumers](../invest/current-consumers.md), read by `tools/Coverage/CurrentCatalogue.cs:100`), and needs [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) only for per-parameter or connection locks.

## 8. Delivery

- **Disposition.** Delivered: package `bumper`. Records under `_bmad-output/implementation-artifacts/`: `review-6-2-bumper-battery-integration.md`, `review-6-3-bumper-multi-angle-contacts.md`, `review-c-bumper-spring.md`, `spec-bumper-cap-solid.md` (published `057df5ae`).
- **Residual, in order.** Every residual slice is blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element. (1) The EL-195 part check in the "Launch parts" closure review (data-only tier), after the programme. (2) The isolated authored-assistance control in Story 15.0; then the CAT-015 row and the EL-195 ledger row are ticked. Chrome proof is queued for the post-catalogue phase.
- **Disqualifiers.** A second paid path beside the declared store; a change to the 72-step cooldown or the 0.05 m/s threshold without re-running the three Playable lessons.
