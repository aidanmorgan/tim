# EL-194 · Springboard named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-194 |
| Name | Springboard |
| Type | Mechanical |
| Anchor | [requirements.md#element-194](../requirements.md#element-194); [named entry](../invest/named-elements.md#element-194); contract [springboard-elastic-contract.md](../../springboard-elastic-contract.md) |
| Proof owner | S300 |
| Refines / extends | Refines [CAT-062 spring](CAT-062-spring.md) ([requirement](../requirements.md#current-cat-062)) |
| Related | CAT-065 Trampoline (shares the elastic capability, different declaration), EL-215 Damped cushion |
| Roadmap story | Story 6.4 (slider and spring) and Story 6.5 (preload and `spring_forward`) |
| Status | not started |

## 2. Declaration

All values below are sourced by the [springboard contract](../../springboard-elastic-contract.md) and the legacy part unless marked proposed.

### Bodies and shapes
- **Plate (dynamic).** Box half extents 0.65 × 0.075 × 0.6 m (full 1.3 × 0.15 × 1.2 m) (`parts/SpringPart.cs@a6c914e:L18-L18`, `parts/SpringPart.cs@a6c914e:L90-L92`).
- **Base (static).** Box full size 1.3 × 0.12 × 1.2 m at (0, −0.36, 0); `AddBox` stores halves (`parts/SpringPart.cs@a6c914e:L89-L89`).
- Rest centre of the plate at local Y = 0.14 m (`parts/SpringPart.cs@a6c914e:L14-L14`).

### Mass and material
- Plate 0.25 kg, homogeneous box inertia (`parts/SpringPart.cs@a6c914e:L16-L16`, `parts/SpringPart.cs@a6c914e:L20-L24`).
- Plate material restitution 0, bounce threshold 0.05 m/s, friction 0.1 (`parts/SpringPart.cs@a6c914e:L34-L34`; argument order `engine/physics/PersistentContactPair.cs@a6c914e:L18-L29`): the spring, not contact restitution, returns energy.

### Constraints and joints
- Frictionless slider along the plate's local Y, travel −0.25 m to 0 relative to rest, connected collision disabled (`parts/SpringPart.cs@a6c914e:L35-L48`).
- TGS Soft spring on that slider with stiffness k and damping, potential U = ½·k·q² ([contract](../../springboard-elastic-contract.md)); legacy applied it as per-step elastic and damping loads (`parts/SpringPart.cs@a6c914e:L67-L72`).

### Typed ports
| Socket | Domain | Direction | Source |
| --- | --- | --- | --- |
| `ActivationOut` | Activation | Output | **proposed**: the row requires "typed signal integration"; the legacy raised a `Bounced` event on plate contact (`parts/SpringPart.cs@a6c914e:L80-L85`) |

### Sensors and activation
A contact trigger on the plate with approach speed ≥ 0.05 m/s (`parts/SpringPart.cs@a6c914e:L82-L82`) emits on `ActivationOut`, using the existing trigger declaration (`engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L7-L18`).

### Work and energy stores
Elastic store ½·k·q². Initial compression is finite construction energy released when Run begins; no powered recharge or automatic launch ([contract](../../springboard-elastic-contract.md)).

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `stiffness` | f32 | 120–1200 | 400 | N/m | `parts/SpringPart.cs@a6c914e:L50-L59`, `parts/catalog/spring.tres@a6c914e:L14-L14` |
| `damping` | f32 | 0–8 | 0.2 | N·s/m | same |
| `initial_compression` | f32 | 0–0.20 | 0 | m | same |

Parameter keys are an enum (`SpringParameter`); the removed `strength` field rejects ([contract](../../springboard-elastic-contract.md)).

### Cosmetic curves and UI bindings
- The silver coil (`#ccd9df`, radius 0.27 m, 3 turns, height 0.365 m at y −0.3) scales with committed compression; the plate art uses the physical pose (`parts/SpringPart.cs@a6c914e:L17-L17`, `parts/SpringPart.cs@a6c914e:L73-L79`, `parts/SpringPart.cs@a6c914e:L94-L94`).
- UI: the three parameters through the contextual configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`). Pick radius 0.7 m (`parts/SpringPart.cs@a6c914e:L88-L88`).

### Art
Plate in the Spring colour `#f5b354`; base `#273446`; coil `#ccd9df` (`parts/SpringPart.cs@a6c914e:L89-L94`; `parts/catalog/spring.tres@a6c914e:L13-L13`; [DESIGN colour system](../../../DESIGN.md#colour-system)).

### Catalogue and inventory entry
Id `spring`, title "Springboard", category Motion, "A rigid plate compresses its finite spring and returns stored energy." (`parts/catalog/spring.tres@a6c914e:L8-L14`). Add `WorkshopPartKind.Springboard` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
- **Passive (uncharged).** `initial_compression` = 0. A payload loads the plate through contact and gets back stored energy less damping; a missed or absent payload gets nothing.
- **Precharged.** `initial_compression` > 0. The plate starts compressed and releases at Run start; a payload resting on it is launched by that finite energy only, and an empty plate moves but transfers no work. The precharged lesson places the plate at −30° with 0.2 m compression and a ball 0.357 m above along the plate normal (`tools/Campaign/SpringboardLesson.cs@a6c914e:L38-L47`).

## 3. Engine capabilities

Families: ContactImpulse, ElasticStorage, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Dynamic box plate and contact: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`; contact triggers for the signal: `engine/gpu/ContactTriggerDeclaration.cs@a6c914e:L7-L18`.

**Missing**
- Prismatic slider and unified TGS Soft spring (JointConstraint, ElasticStorage): Story 6.4.
- Preload energy store and `spring_forward`: Story 6.5. Owner S300.

**Dependencies.** None beyond Stories 6.4–6.5; CAT-065 trampoline reuses the elastic capability.

## 4. Sources and legacy

- Requirement: "Declared finite elastic store transfers work through physical compression and release"; outcome "Uncharged or missed contact cannot impart a free launch"; qualify passive/precharged contact, typed signal integration, no-spring/disconnected/missed controls, all fifteen composite lane instances, parameter editing and exact Reset/save ([element-194](../requirements.md#element-194)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Rest height 0.14, stroke 0.25, plate 0.25 kg, plate half extents (0.65, 0.075, 0.6) | `parts/SpringPart.cs@a6c914e:L14-L18` | carry forward | Contract values. |
| 2 | Parameter validation 120–1200, 0–8, 0–0.20 with an explicit message | `parts/SpringPart.cs@a6c914e:L50-L59` | carry forward | Admission bounds. |
| 3 | Elastic and damping loads applied in `PreparePhysics` | `parts/SpringPart.cs@a6c914e:L67-L72` | do not carry forward (per-part loop) | Becomes a declared soft spring row. |
| 4 | `Bounced` event and hit counter on plate contact ≥ 0.05 m/s | `parts/SpringPart.cs@a6c914e:L80-L85` | carry forward (event as typed output), do not carry forward (string event) | Typed signal integration. |
| 5 | Lesson `spring_forward`: ball at (−3, 3.6, 0), receiver at (−0.7, 0.55, 0), solution springboard at (−3, 0.8, 0) tilted −20° | `tools/Campaign/SpringboardLesson.cs@a6c914e:L25-L36`; `content/puzzles.json@a6c914e:L707-L917` | carry forward | First lesson. |
| 6 | Composite lanes: `Add` places each module lane at (X offset, Z offset) with a yaw about Y, rotating every module part's position and orientation and prefixing its id `laneN_` | `tools/Campaign/Program.cs@a6c914e:L323-L345` | carry forward (layout rule) | How the fifteen lanes are built. |
| 7 | The fifteen springboard lane instances, as (level id, lane, module, X, Z, yaw°): (`bounce_and_roll`, 2, spring, 0, 2, 0); (`spring_and_chain`, 1, spring, 0, −2, 0); (`two_signals`, 2, spring_signal, 0, 2, 0); (`deep_springs`, 1, spring, −2, 0, 90); (`deep_springs`, 2, spring, 2, 0, −90); (`three_deliveries`, 3, spring, 0, 3, 0); (`triple_signal`, 2, spring_signal, 0, 0, 0); (`cold_front`, 3, spring, 0, 3, 0); (`bounce_mail`, 1, spring, 0, −3, 0); (`double_cold_start`, 2, spring, 0, 0, 0); (`signals_and_chain`, 2, spring_signal, 0, 0, 0); (`cold_signals`, 2, spring_signal, 0, 0, 0); (`depth_delivery`, 3, spring, 3, 0, −90); (`depth_telegraph`, 2, spring_signal, 0, 0, 90); (`bridges_and_signal`, 2, spring_signal, 0, 0, 0) | `tools/Campaign/Program.cs@a6c914e:L448-L450`, `tools/Campaign/Program.cs@a6c914e:L463-L468`, `tools/Campaign/Program.cs@a6c914e:L483-L485`, `tools/Campaign/Program.cs@a6c914e:L489-L499`, `tools/Campaign/Program.cs@a6c914e:L503-L505`, `tools/Campaign/Program.cs@a6c914e:L509-L511`, `tools/Campaign/Program.cs@a6c914e:L515-L526`, `tools/Campaign/Program.cs@a6c914e:L530-L532` | carry forward | The requirement's "all fifteen composite lane instances". |
| 8 | Module `spring` is the `spring_forward` lesson; module `spring_signal` is the same lane with the receiver replaced by a switch, a locked lamp at (4.5, 1, 0), goal Activated on the lamp and a solution wire switch → lamp | `tools/Campaign/Program.cs@a6c914e:L9-L9`, `tools/Campaign/Program.cs@a6c914e:L14-L17`, `tools/Campaign/Program.cs@a6c914e:L52-L63` | carry forward | Lane content. |

**Files harvested:**
- `parts/SpringPart.cs`
- `parts/catalog/spring.tres`
- `engine/physics/PersistentContactPair.cs` (material argument order)
- `tools/Campaign/SpringboardLesson.cs`
- `tools/Campaign/Program.cs` (composite lanes and modules)
- `content/puzzles.json` (level `spring_forward`)
- Searched with no further springboard knowledge for this identity: `CuriousContraptions.tests/` (the springboard tests belong to [CAT-062](CAT-062-spring.md), Batch A), `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-194](../requirements.md#element-194) and the [contract](../../springboard-elastic-contract.md#chrome-observable-acceptance-cat-062).
- **Chrome UI recipe.** Open `spring_forward`; place the springboard under the ball and tilt it with the gizmo; Run.
- **Positive.** Passive: the ball compresses the plate and rebounds toward the receiver. Precharged: a resting ball is launched once.
- **Negative/control.** No springboard, a missed ball and an unloaded plate give no launch; a wire into the springboard is refused; the second bounce apex is lower than the first.
- **Boundaries.** Each parameter endpoint; tilted frame; every one of the fifteen composite lane instances in fact 7: `bounce_and_roll`, `spring_and_chain`, `two_signals`, `deep_springs` (two yawed lanes), `three_deliveries`, `triple_signal`, `cold_front`, `bounce_mail`, `double_cold_start`, `signals_and_chain`, `cold_signals`, `depth_delivery` (yawed), `depth_telegraph` (yawed) and `bridges_and_signal`.
- **Run/Reset.** Plate returns to authored compression.
- **Save/Load.** Parameters, pose and the `ActivationOut` wire round-trip; `strength` rejects.
- **Integrations.** Campaign row 1 (springboard) first use 1–10, reuse 21–30, 41–50, 91–100, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); retained springboard behaviour [todo-461](../requirements.md#todo-461); switch-and-lamp lanes with CAT-063 and CAT-035.

## 6. Open questions

1. Is `ActivationOut` on plate contact (proposed) the required "typed signal integration", or is an input meant? Unspecified — owner decision.
