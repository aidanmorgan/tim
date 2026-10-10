# CAT-062 · spring (Springboard) — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-062 · `spring` (display name Springboard) |
| Requirement | [CAT-062](../requirements.md#current-cat-062) |
| Mapped identities | EL-194 Springboard. Related: TH-33 Spring-mounted match (uses a generic spring assembly; own spec). |
| Roadmap | Stories 6.4 (CAT-062a prismatic slider and soft spring constraint), 6.5 (CAT-062b preload energy store, spring_forward) |
| Status | Story6.4 passive implementation candidate; review/publication pending. Legacy `parts/SpringPart.cs` was purged in Story7.4 and remains baseline history only |
| Levels | placed in ready_to_rebound; inventory in spring_forward and 15 others; see [CAT-062-I consumers](../invest/current-consumers.md#cat-062-i) |
| Surviving contract | [Springboard elastic contract](../../springboard-elastic-contract.md) (current document) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Bodies and shapes | Fixed base: static box centre (0, −0.36, 0), size 1.3 × 0.12 × 1.2 (half 0.65, 0.06, 0.6; `AddBox` stores half the size `reference/cpu/MachinePart.cs@a6c914e:L352-L356`) `parts/SpringPart.cs@a6c914e:L86-L90`. Moving plate: dynamic box half (0.65, 0.075, 0.6), i.e. 1.3 × 0.15 × 1.2 m `parts/SpringPart.cs@a6c914e:L18-L24`; `docs/springboard-elastic-contract.md@a6c914e:L9-L9`. |
| Mass and material | Plate 0.25 kg, uniform box inertia `parts/SpringPart.cs@a6c914e:L14-L22`. Legacy plate contact material restitution 0, bounce threshold 0.05, friction 0.1 `parts/SpringPart.cs@a6c914e:L34-L34`. |
| Constraints and joints | Frictionless slider on the plate's local Y; rest centre at local Y 0.14 m; travel −0.25 m to 0 relative to rest; connected-body collision disabled `parts/SpringPart.cs@a6c914e:L35-L49`; `docs/springboard-elastic-contract.md@a6c914e:L11-L12`. |
| Sockets and ports | none (no wire; release needs no signal) `docs/springboard-elastic-contract.md@a6c914e:L20-L22`. |
| Sensors and activation | none physical. The legacy part counted plate hits and published a Bounced event for contacts on the plate with approach speed ≥ 0.05 m/s (§4 fact 15); no springboard acceptance consumes it. |
| Work and energy stores | Elastic potential U = ½ k q² on the slider coordinate; initial compression is finite stored construction energy released when Run begins (8 J at k 400, q 0.2) `docs/springboard-elastic-contract.md@a6c914e:L12-L21`. |
| Parameters | stiffness 120–1200 N/m (default 400); damping 0–8 N·s/m (default 0.2); initial_compression 0–0.20 m (default 0) `parts/SpringPart.cs@a6c914e:L50-L59`, `docs/springboard-elastic-contract.md@a6c914e:L13-L13`; the removed `strength` field rejects `CuriousContraptions.tests/SpringAnimationTests.cs@a6c914e:L154-L169`, `docs/springboard-elastic-contract.md@a6c914e:L16-L16`; enum `engine/SpringParameter.cs@a6c914e:L3-L4`; catalogue defaults `parts/catalog/spring.tres@a6c914e:L14-L14`. |
| Cosmetic curves and UI bindings | The silver coil scales along local Y with committed compression; the plate art uses the physical pose `docs/springboard-elastic-contract.md@a6c914e:L14-L14`. Legacy coil map: base at y −0.3, height 0.365, scale (0.365 + q)/0.365 `parts/SpringPart.cs@a6c914e:L73-L79`. |
| Art | Navy `#273446` base; gold plate in the part colour at y 0.14 − preload; three-turn helix radius 0.27, tube radius 0.025, 96 segments × 8 sides, colour `#ccd9df` `parts/SpringPart.cs@a6c914e:L86-L123`; pick radius 0.7. Scene `parts/scenes/spring.tscn`. Palette Spring `#f5b354` `DESIGN.md@a6c914e:L171-L171`; design rule `DESIGN.md@a6c914e:L300-L300`. Icon `ui/WorkshopIcons.cs@a6c914e:L50-L50`. |
| Catalogue and inventory | `parts/catalog/spring.tres@a6c914e:L6-L14`: id `spring`, title Springboard, category Motion, defaults stiffness 400, damping 0.2, initial compression 0. |

Current candidate tests: `SoftConstraintTests`, `PrismaticConstraintTests`, `WorkshopSpringboardTests`, actual-WASM controls in `tools/workshop-rigid-body.test.mjs`, and actual Chrome `tools/e2e/cat-062.test.ts`. Canonical declaration/resource/art are `WorkshopSpringboard`, `SpringboardResource` and `SpringboardPart`; the shared worker solves generic prismatic rows. [Current implementation/proof scope](../../../_bmad-output/implementation-artifacts/spec-6-4-passive-springboard.md). Preload, named campaign lanes and other later acceptance remain owed. Legacy tests listed in §4.

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): ContactImpulse, ElasticStorage, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| Dynamic box body and box contact | Story 5.1 |

| Missing | Story that builds it |
| --- | --- |
| JointConstraint: generic 1D prismatic slider with travel stops | Story 6.4 |
| ElasticStorage: TGS Soft spring-damper constraint | Story 6.4 (also serves CAT-065 Trampoline) |
| Initial-compression energy store released at Run | Story 6.5 |
| Typed parameter UI for stiffness/damping/initial compression | Story 6.4/6.5 |

Dependencies: a dynamic payload; Receiver for spring_forward; Impact switch and lamp for spring_signal.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The plate is a finite-mass body on a passive elastic slider; contact loads the shared potential; no impact callback prescribes a payload velocity. | `parts/SpringPart.cs@a6c914e:L8-L9` | carry forward. |
| 2 | Elastic and damping loads were added to the slider every tick by the part (`PreparePhysics`). | `parts/SpringPart.cs@a6c914e:L67-L72` | do not carry forward: per-element update loop; the spring becomes a declared constraint in the shared solver. |
| 3 | The elastic law: E = ½ k (q − rest)²; stiffness must be finite and positive. | `engine/physics/AxialElasticPotential.cs@a6c914e:L9-L32` | carry forward the law; do not carry forward the CPU interval-effort implementation. |
| 4 | Rotated 0/0/0, 30/50/70 or 90/0/0, an undamped plate loaded by a ball at 2 m/s compresses 0.01–0.25 m, returns the ball with > 0.2 m/s along its normal, and total energy (plate + ball kinetic + elastic) never exceeds the initial value + 1e−5 J. | `CuriousContraptions.tests/SpringAnimationTests.cs@a6c914e:L43-L71` | carry forward (tilted frame works along its local axis; no free energy). |
| 5 | Zero preload with a resting ball, or 0.2 m preload with the ball 2 m to the side, gives zero ball speed and no hit; 0.2 m preload under the ball launches it (> 1 m/s); initial energy equals ½ · 400 · q² within 1e−5. | `CuriousContraptions.tests/SpringAnimationTests.cs@a6c914e:L73-L95` | carry forward (missed/uncharged controls). |
| 6 | The plate body and its visual share one pose within 1e−6 m every tick; rendering 1 or 4 frames per tick never changes physics; Reset and Load restore the construction exactly. | `CuriousContraptions.tests/SpringAnimationTests.cs@a6c914e:L97-L130` | carry forward. |
| 7 | Stiffness 119 or 1201, damping −0.1 or 8.1, initial compression −0.01 or 0.21, and NaN reject; the removed `strength` property (8.5) rejects. | `CuriousContraptions.tests/SpringAnimationTests.cs@a6c914e:L132-L169` | carry forward the bounds and the obsolete-field rejection; do not carry forward the string property key outside the resource boundary. |
| 8 | spring_forward and spring_signal are won with the spring at precision 1 and 0.45 and lost without it; receiver at (−0.7, 0.55, 0). | `CuriousContraptions.tests/SpringboardLessonTests.cs@a6c914e:L34-L61` | carry forward. |
| 9 | In spring_forward at precision 1 the plate compresses 0.05–0.25 m, the ball rebounds upward faster than 1 m/s and peaks below its start height. | `CuriousContraptions.tests/SpringboardLessonTests.cs@a6c914e:L63-L101` | carry forward (finite height; no energy gain). |
| 10 | A plate at (0, 2.5, 0) tilted −30° with 0.2 m preload lifts a ball resting on it by more than 0.2 m; with zero preload the rise stays below 0.01 m; stored energy at Start equals ½ · 400 · q². | `CuriousContraptions.tests/SpringboardLessonTests.cs@a6c914e:L103-L146` | carry forward. |
| 11 | ready_to_rebound stores 8 J at Start (7.99999–8.00001) on every replay, is won only with the placed basket, and Reset restores the construction exactly. | `CuriousContraptions.tests/SpringboardLessonTests.cs@a6c914e:L148-L176` | carry forward. |
| 12 | spring_forward reference: ball (−3, 3.6, 0), receiver (−0.7, 0.55, 0), spring_1 at (−3, 0.8, 0) tilted −20°. ready_to_rebound: spring locked at (0, 2.5, 0), −30°, initial compression 0.2; ball 0.357 m above along the plate normal; basket_1 solution at (1.8, 1.3, 0) with windows 0.2 m / 5° (0.1 m / 2° at 0.45). | `tools/Campaign/SpringboardLesson.cs@a6c914e:L19-L74` | carry forward (content in `content/puzzles.json`). |
| 13 | Placement assistance widens the set of working spring tilts in spring_forward (−12° to 12°) without relaxing receiver capture; every precise success remains a forgiving success. | `CuriousContraptions.tests/AssistanceTests.cs@a6c914e:L139-L168` | carry forward. |
| 14 | Authoring: spring position step 0.20 m, rotation step 3°. | `tools/Campaign/Program.cs@a6c914e:L544-L545` | carry forward the values. |
| 15 | Each contact on the plate body with approach speed ≥ 0.05 m/s incremented the run-scoped hit count (restored on Reset); the first such contact also recorded the spring's Bounced event at that tick (the event key is per part, so later contacts add no new event); slower or non-plate contacts were ignored. | `parts/SpringPart.cs@a6c914e:L80-L85`; `parts/SpringPart.cs@a6c914e:L31-L33` | carry forward the 0.05 m/s qualifying approach speed and the Bounced event only if a later consumer needs a springboard contact event; do not carry forward the per-part `ObserveContact` callback (per-element update loop). |
| 16 | The springboard returns no impact command for a payload on a moving surface frame and leaves both body snapshots unchanged; only the Bumper returns a launch impulse. | `CuriousContraptions.tests/ImpactFrameTests.cs@a6c914e:L122-L127` | carry forward (confirms fact 1: the plate prescribes no payload velocity). |
| 17 | Zero gravity, ball 0.65 m above a springboard placed 0.2 m below its placement target (correction cap 0.3 m, blend 0.4 s at precision 0; cap 0 at precision 1): at precision 0 the plate frame is kinematic, its correction blend launches the ball upward (hit count > 0, vy > 0); at precision 1 the frame is static, nothing hits and the ball keeps its pose and zero velocity; Reset restores the saved machine and a second Run replays identical body states. | `CuriousContraptions.tests/ImpactFrameTests.cs@a6c914e:L16-L86` | carry forward the precision-1 control (no launch without stored energy) and the exact Reset replay; do not carry forward the launch from correction motion: placement correction moving the frame during the Run is a legacy mechanism, and the current springboard launches only from its declared slider and preload (facts 1, 5). |
| 18 | spring_forward at precision 0 with its authored solution: the receiver moved 0.3 m in +x is still captured within 1,200 ticks; moved 0.5 m in −x it is not. The receiver's precision-0 knots are capture margin 0.3 and guide acceleration 12. | `CuriousContraptions.tests/FlightCampaignTests.cs@a6c914e:L15-L40`; `content/puzzles.json@a6c914e:L781-L796` | carry forward as level-regression boundaries for spring_forward (shared with CAT-004). |

Files harvested:
- `parts/SpringPart.cs`
- `reference/cpu/MachinePart.cs` (AddBox half-size rule)
- `parts/scenes/spring.tscn` (script binding only)
- `engine/SpringParameter.cs`
- `engine/physics/AxialElasticPotential.cs`
- `CuriousContraptions.tests/SpringAnimationTests.cs`
- `CuriousContraptions.tests/SpringboardLessonTests.cs`
- `CuriousContraptions.tests/AssistanceTests.cs`
- `CuriousContraptions.tests/ImpactFrameTests.cs`
- `CuriousContraptions.tests/FlightCampaignTests.cs`
- `tools/Campaign/SpringboardLesson.cs`
- `tools/Campaign/Program.cs`

Not springboard knowledge (wound-spring latches, owned by CAT-071): `CuriousContraptions.tests/PhysicsLatchedSpringTests.cs`, `CuriousContraptions.tests/SpringConstraintObservationTests.cs`, `engine/SceneLatchedSpringDeclaration.cs`, `engine/physics/PhysicsLatchedSpring.cs`.

## 5. Acceptance outline

Acceptance: [CAT-062](../requirements.md#current-cat-062), [retained behaviour](../requirements.md#todo-461) and the [elastic contract](../../springboard-elastic-contract.md).

- **Construction.** spring_forward: place the Springboard under the ball, tilt clockwise, Run. Parameter endpoints through the UI.
- **Positive.** Contact compresses the plate within 0.25 m and rebounds the payload; preload releases at Run.
- **Negative/control.** Unloaded board does not launch; missed ball gets nothing; zero-load stationary assembly stays still; board rotated 180° fails (Story 6.5).
- **Boundaries.** Stops at −0.25 m and 0; parameter endpoints; obsolete `strength` rejects visibly.
- **Run/Reset, Save/Load.** Exact rest/precompression after Reset; parameters persist.
- **Integrations.** spring_signal (switch + lamp), ready_to_rebound, the fifteen composite spring lanes.

## 6. Open questions

- Plate contact material is resolved by the current [EL-194 declaration](EL-194-springboard.md#mass-and-material): preserve restitution 0, threshold 0.05 m/s and friction 0.1 from baseline fact §2. This is existing source authority, not a new tuning choice.
- Story 6.5 names `engine/LatchedSpringStore.cs` for deletion, but no such file exists at `a6c914e` and the contract says preload is not a latch; the story wording needs correcting — owner decision.
