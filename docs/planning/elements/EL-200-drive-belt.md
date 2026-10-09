# EL-200 · Drive belt named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-200 |
| Name | Drive belt |
| Type | Mechanical |
| Anchor | [requirements.md#element-200](../requirements.md#element-200); [named entry](../invest/named-elements.md#element-200); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S310 |
| Refines / extends | No CAT spec of its own; the belt is a connection, not a catalogue part. [CAT-019 conveyor](CAT-019-conveyor.md) and [CAT-057 reverse transmission](CAT-057-reverse_transmission.md) list it as a candidate mapping for the legacy mechanical link |
| Related | EL-201 Drive chain (must not be interchangeable), EL-199 Electric motor, EL-202 Conveyor, EL-203 Reverse transmission |
| Roadmap story | unscheduled as a friction-and-tension belt; Stories 11.1–11.2 build the rigid mechanical link that is its nearest precursor |
| Status | not started |

## 2. Declaration

### Bodies and shapes
No collider: a massless loop between two mechanical sockets. Each socket carries a sheave of radius 0.22 m, the sourced port pulley radius (`parts/ConveyorPart.cs@a6c914e:L111-L117`). Belt length is measured from the two socket positions and sheave radii when connected — **proposed**, by analogy with the connect-time rope length (`reference/cpu/MachineWorld.cs@a6c914e:L686-L691`).

### Mass and material
Massless — **proposed**: belt inertia is negligible beside the 0.25–0.5 kg shafts it couples. Belt–sheave friction coefficient from `friction` (see Parameters).

### Constraints and joints
- A frictional coupling between the two sheave hinges. While the transmitted force stays within the capstan capacity F_max = 2·T₀·(e^{μθ} − 1)/(e^{μθ} + 1) (T₀ tension, μ friction, θ wrap angle, π for a straight two-sheave belt), the sheave rims move together: ω_out·r_out = ω_in·r_in. Above it the belt slips and transmits only F_max — **proposed** model; decision owner S310.
- Sign: a belt keeps rotation sense (ratio +r_in/r_out); legacy links compiled to ratio = input coordinate-per-radian ÷ output coordinate-per-radian (`engine/MechanicalNetwork.cs@a6c914e:L46-L49`).

### Typed ports
The belt is a connection of kind Belt between a mechanical output (`Drive`) and a mechanical input (`DriveIn`). One belt per input; an output may fan out (`engine/MechanicalNetwork.cs@a6c914e:L28-L38`). A Belt cannot attach to a chain sprocket socket (S257 mechanical-port: "chain and belt are not interchangeable").

### Sensors and activation
None.

### Work and energy stores
None. Slip dissipates work as heat in the finite ledger; the belt never delivers more power than its input shaft supplies.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `tension` | f32 | 0–200 | 90 | N | **proposed**: with μ = 0.4 and r = 0.22 m, 90 N carries about 22 N·m, just above the 20 N·m motor default (`parts/catalog/motor.tres@a6c914e:L14-L14`), so a slacker belt visibly slips |
| `friction` | f32 | 0.1–0.8 | 0.4 | coefficient μ | **proposed**: rubber-on-metal order of magnitude, inside the solver's friction range 0–1 (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L51-L51`) |

Zero tension transmits nothing ("missing tension prevents full drive transfer").

### Cosmetic curves and UI bindings
Belt artwork: four straight segments following the socket transforms and two marks moving at the driven sheave's rim speed only while running (legacy marks moved at shaft speed × 0.16, `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L17-L60`). During slip the marks follow the belt, not the output sheave — **proposed**. UI: draw the belt with the contextual Connect (kind Belt); `tension` and `friction` through the configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Belt in navy `#293954` with cream `#fff8e9` marks over the sourced cream port pulleys with gold spokes (`parts/ConveyorPart.cs@a6c914e:L115-L117`); [DESIGN mechanical work feedback](../../../DESIGN.md#mechanical-work-feedback). Belt colour **proposed**.

### Catalogue and inventory entry
Not a catalogue part. A connection kind `MechanicalLinkKind.Belt` — **proposed**; an inventory allowance for belts, if any, is open question 2.

### Variants
One outcome in the row; no variants.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-02.json](../../coverage/engine/element-02.json)).

**Exists now**
- Coulomb friction cone in the solver, the model the belt capacity mirrors: `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L701-L719`.
- Typed connection storage (Activation and Electrical only): `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.

**Missing**
- Mechanical domain and shaft hinges: Story 11.1. A frictional, tension-limited shaft coupling row (ShaftTorque/SlidingFriction): unscheduled; decision S257 mechanical-port, next implementation S690 ([decisions](../invest/decisions.md#s257)). Owner S310.

**Dependencies.** CAT-042 motor and CAT-019 conveyor (Story 11.1) as the first drive pair.

## 4. Sources and legacy

- Requirement: "Connected pulleys exchange torque through declared friction and tension"; outcome "Missing tension or disconnected endpoints prevent full drive transfer" ([element-200](../requirements.md#element-200)). Campaign: "belt", first use 11–20 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | A mechanical input accepts one authored belt; links resolve only between matching mechanical sockets | `engine/MechanicalNetwork.cs@a6c914e:L28-L38` | carry forward | Typed topology. |
| 2 | Each link compiles to an always-engaged rigid ratio joint | `engine/MechanicalNetwork.cs@a6c914e:L46-L49`; `engine/SceneTransmissionJoint.cs@a6c914e:L17-L26` | do not carry forward (rigid, slip-free coupling) | The EL row requires friction and tension; a rigid ratio cannot slip. |
| 3 | Accepted: wrong socket or wrong domain refused; a source-free loop refused; a second driver into one input refused; explicit fan-out allowed | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L211` | carry forward | Admission rules. |
| 4 | Belt artwork: four lines and two marks following socket transforms; marks move only while running | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L17-L60` | carry forward | Presentation contract (the `MechanicalBeltVisual` source was already deleted before `a6c914e`). |

**Files harvested:**
- `engine/MechanicalNetwork.cs`
- `engine/SceneTransmissionJoint.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `parts/ConveyorPart.cs` (port pulley geometry and art)
- `parts/catalog/motor.tres` (default torque reference)
- `reference/cpu/MachineWorld.cs` (connect-time length rule)
- Searched with no hit for a friction- or tension-limited belt (`belt` hits are the rigid link above, conveyor tests and level text): `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-200](../requirements.md#element-200) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place battery, motor and conveyor; connect `Supply` → `PowerIn`; draw a Belt from motor `Drive` to conveyor `DriveIn`; set `tension`. Run with a ball on the conveyor.
- **Positive.** At default tension the conveyor turns with the motor and carries the ball.
- **Negative/control.** Tension 0 transmits nothing; a low tension slips under a loaded conveyor (marks run, output lags); a disconnected belt drives nothing; a belt into a chain sprocket is refused.
- **Boundaries.** Tension 0 and 200; friction 0.1 and 0.8; fan-out to two loads; reversed sense through a reverser.
- **Run/Reset.** Shafts and belt marks return to rest.
- **Save/Load.** Belt endpoints, kind, `tension` and `friction` round-trip.
- **Integrations.** Retained belt behaviour [todo-151](../requirements.md#todo-151) (signed, load-aware motion through conveyor input/output and the −1 reverser); campaign row 2 ("... belt ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); couples CAT-042, CAT-019 and CAT-057; contrast with EL-201 chain.

## 6. Open questions

1. Is EL-200 the identity behind the legacy mechanical link that Stories 11.1–11.2 build (candidate per CAT-019/CAT-057), or a separate later element? Unspecified — owner decision.
2. Are belts unlimited connections or counted inventory? Owner decision.
3. Capstan slip model (proposed) versus a simpler friction-limited ratio: owner decision under S690.
