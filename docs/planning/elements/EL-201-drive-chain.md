# EL-201 · Drive chain named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-201 |
| Name | Drive chain |
| Type | Mechanical |
| Anchor | [requirements.md#element-201](../requirements.md#element-201); [named entry](../invest/named-elements.md#element-201); scope [campaign-element-coverage](../requirements.md#campaign-element-coverage) |
| Proof owner | S358 |
| Refines / extends | No CAT spec; a connection, not a catalogue part. Shares the mechanical ports of [CAT-042 motor](CAT-042-motor.md) and [CAT-019 conveyor](CAT-019-conveyor.md) |
| Related | EL-200 Drive belt (must not be interchangeable), EL-199 Electric motor, EL-110 Flywheel |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes
No collider: a massless closed loop between two sprockets. A sprocket is a mechanical socket declared with attachment kind Sprocket and a tooth count N; its pitch radius is p / (2·sin(π/N)) with pitch p = 0.08 m — **proposed**: at N = 16 the pitch radius is 0.205 m, close to the sourced 0.22 m port pulley (`parts/ConveyorPart.cs@a6c914e:L111-L117`), so sprockets fit the same parts.

### Mass and material
Massless — **proposed**, as EL-200.

### Constraints and joints
- An always-engaged, slip-free ratio coupling between the two sprocket hinges: ω_out = ω_in · N_in / N_out, same rotation sense — **proposed**; the legacy rigid ratio row is the matching law (`engine/MechanicalNetwork.cs@a6c914e:L46-L49`; `engine/SceneTransmissionJoint.cs@a6c914e:L17-L26`).
- No slip: torque is limited only by the shafts and supply. A chain cannot transmit if either end is not a Sprocket socket of the same pitch.

### Typed ports
A connection of kind Chain from a mechanical output to a mechanical input whose sockets both declare attachment kind Sprocket with equal pitch. Attaching a chain to a belt sheave, or a belt to a sprocket, is refused at connection and leaves the construction unchanged (S257 mechanical-port).

### Sensors and activation
None.

### Work and energy stores
None; it transmits shaft work without storing or creating it.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `teeth_in` | enum `SprocketTeeth { T8, T12, T16, T24, T32 }` | 5 values | T16 | teeth | **proposed**: a closed set of ratios (0.25–4) players can count, rather than a free real number |
| `teeth_out` | enum `SprocketTeeth` | 5 values | T16 | teeth | **proposed**, same set |
| pitch | constant | 0.08 | 0.08 | m | **proposed**: one chain pitch so every sprocket is compatible with every chain |

### Cosmetic curves and UI bindings
Chain artwork: link marks spaced at the pitch, advancing by the sprocket's committed rotation × pitch radius only while running; sprocket teeth drawn on the socket wheel — **proposed**. UI: draw with the contextual Connect (kind Chain); tooth counts through the configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Pale grey metal `#ccd8dc` chain on navy `#293954` sprockets with gold `#f7cb52` hub marks ([DESIGN colour system](../../../DESIGN.md#colour-system): "metal details use pale grey"). **Proposed**.

### Catalogue and inventory entry
Not a catalogue part. Connection kind `MechanicalLinkKind.Chain` — **proposed**; sprocket sockets are declared by the parts that carry them.

### Variants
One outcome in the row; no variants.

## 3. Engine capabilities

Families: ContactImpulse, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, JointConstraint, RigidBodyDynamics, ShaftTorque, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

**Exists now**
- Typed connection storage with domain and direction checks (Activation, Electrical): `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L13`.

**Missing**
- Mechanical domain, hinges and the ratio coupling row: Stories 11.1–11.2. Sprocket attachment kind and belt/chain incompatibility: decision S257 mechanical-port, next implementation S690 ([decisions](../invest/decisions.md#s257)). Owner S358.

**Dependencies.** Stories 11.1–11.2 (mechanical domain and ratio row); EL-200 for the incompatibility control.

## 4. Sources and legacy

- Requirement: "Compatible sprockets exchange torque through typed toothed engagement"; outcome "Incompatible attachment fails rather than inheriting belt behaviour" ([element-201](../requirements.md#element-201)). Campaign: "chain", first use 11–20 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Rigid, always-engaged ratio coupling between two axial guides; ratio finite and nonzero; distinct guides | `engine/SceneTransmissionJoint.cs@a6c914e:L17-L26` | carry forward | The slip-free toothed law. |
| 2 | One authored link per input; matching mechanical sockets only | `engine/MechanicalNetwork.cs@a6c914e:L28-L38` | carry forward | Topology. |
| 3 | Accepted: wrong socket and wrong domain are refused | `CuriousContraptions.tests/MechanicalTests.cs@a6c914e:L185-L211` | carry forward | Typed refusal, extended to Chain versus Belt. |
| 4 | Historical status note lists "chain-drive variants" among future work alongside moving-block ratios and rope cutting | `reference/P0-022-before/source-README.md@a6c914e:L99-L99` | carry forward (as gap) | Confirms no legacy chain drive. |

**Files harvested:**
- `engine/SceneTransmissionJoint.cs`
- `engine/MechanicalNetwork.cs`
- `CuriousContraptions.tests/MechanicalTests.cs`
- `parts/ConveyorPart.cs` (port pulley scale)
- `reference/P0-022-before/source-README.md` (future-work note)
- Searched with no hit for `sprocket` or a chain drive (`chain` hits are domino-chain level text in `content/puzzles.json`): `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-201](../requirements.md#element-201) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place battery, motor and conveyor with sprocket sockets; connect `Supply` → `PowerIn`; draw a Chain from motor `Drive` to conveyor `DriveIn`; set `teeth_in` T8 and `teeth_out` T16. Run.
- **Positive.** The conveyor turns at half the motor speed with no slip under load.
- **Negative/control.** A Chain drawn to a belt sheave socket is refused; a Belt drawn to a sprocket is refused; a disconnected chain drives nothing.
- **Boundaries.** Each tooth-count endpoint; heavy load stalls motor and conveyor together (no slip); reverse rotation.
- **Run/Reset.** Shafts and link marks return to rest.
- **Save/Load.** Endpoints, kind and tooth counts round-trip.
- **Integrations.** Campaign row 2 ("... chain ..."): first use 11–20, moving/load variants into 41–50, reuse 31–50, 61–80, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); mechanical-port incompatibility with EL-200 under [S257](../invest/decisions.md#s257); drives CAT-019 conveyor from CAT-042 motor.

## 6. Open questions

1. Which parts carry sprocket sockets, and can one socket be either sheave or sprocket? Unspecified — owner decision.
2. Fixed pitch (proposed) or several chain sizes? Owner decision.
