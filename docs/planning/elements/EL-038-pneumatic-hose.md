# EL-038 · Pneumatic hose — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared sealed-gas facts P1–P20, the `Gas` domain and the "Air" material are in [EL-039](EL-039-air-reservoir.md#shared-sealed-gas-family-facts).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-038 · Pneumatic hose |
| Type | Pneumatic |
| Anchor | [requirements.md#element-038](../requirements.md#element-038); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-038); owner S473 |
| Related | Refines no CAT spec. Connection-kind sibling of [EL-197 Electrical wire](EL-197-electrical-wire.md) and [EL-048 Acoustic duct](EL-048-acoustic-duct.md); joins [EL-037](EL-037-air-compressor.md), [EL-039](EL-039-air-reservoir.md), [EL-040](EL-040-pneumatic-release-valve.md), [EL-041](EL-041-pneumatic-directional-valve.md), [EL-042](EL-042-air-nozzle.md), [EL-043](EL-043-pneumatic-pressure-gauge.md). Not the [CAT-048](../requirements.md#current-cat-048) ball pipe. |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

The hose is an authored typed connection, not a placed rigid part: it starts at a gas Output socket and ends either at a gas Input socket or at a free end.

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | none for physics: the hose has no collider and does not obstruct balls (rope, wire and belt connections are not bodies either). Rendered as a sagging tube between socket world positions. |
| Mass and material | none (massless connection); internal gas "Air" (EL-039 P14). |
| Constraints | none. Endpoints follow the named local sockets under each part transform (connection-endpoint rule, [DESIGN.md](../../../DESIGN.md) electrical-cable paragraph). |
| Typed ports | Consumes one `Gas` Output socket (e.g. `GasOut`) and one `Gas` Input socket (e.g. `GasIn`); resolution requires exactly one matching port at each end, the same domain on both, output → input (legacy rule, harvest H1). Free-end variants have no target socket. |
| Sensors and activation | none. |
| Work and energy stores | A small internal gas volume V_hose = π·(d/2)²·L that shares inventory with whatever it joins (EL-039 P13): it adds capacity, never energy. |
| Parameters | `length`: f32, 0.5–8 m, default 2 m **proposed** (clear-pipe length range 1–8 m and puzzle-bench spans; long enough to route around a wall). Bore d = 0.008 m fixed **proposed** (standard 8 mm pneumatic tube; at 2 m V_hose ≈ 1.0 × 10⁻⁴ m³, 1% of the default tank, so capacity is finite but small). Resistance: flow follows the nozzle law (EL-039 P6) through effective area A_eff = π·(d/2)²·√(1 m / max(L, 1 m)) **proposed** (monotone: a longer hose passes less flow, never more than its bore). `free_end`: closed enum `HoseEnd { Sealed, Vent }`, only for the free-end variants. |
| Cosmetic curves and UI bindings | Static tube; no animation binding (gas-family table "None (static)", [finite-gas-foundation](../../finite-gas-foundation.md#sealed-gas)). Optional pressure tint is not required. A free end shows a cream cap (Sealed) or an open navy mouth (Vent). |
| Art | Translucent cyan `#66b8c9` tube 0.05 m visual diameter with cream `#fff8e9` fittings at each end **proposed**: shape and colour differ from navy electrical cables and gold activation links ([DESIGN.md](../../../DESIGN.md) preservation check: "distinguish connection types through shape as well as colour"). |
| Catalogue / inventory | Id `pneumatic_hose`, Title "Air hose", Category `Air` **proposed**; drawn with the Connect control from a gas socket, budgeted per level like other inventory. Description **proposed**: "Joins air sockets. It carries air only between connected ends; an open free end vents, a capped one holds." |

**Variants** (from the outcome sentence "Open or disconnected hose vents or isolates according to its boundary declaration"):

| Variant | Ends | Behaviour |
| --- | --- | --- |
| Connected | Output socket → Input socket | Shares inventory between both nodes; flow from higher to lower pressure through A_eff; equal pressures → NoFlow. |
| Free end, Vent | Output socket → open end at an authored point | Discharges to ambient (EL-039 P15) through A_eff until the source reaches ambient; never pulls ambient air in above source pressure (forward flow only, P6). |
| Free end, Sealed | Output socket → capped end | Isolates: adds V_hose to the source node; no flow to ambient; downstream sees nothing. |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S473): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction).

**Exists now**
- Typed connection record and validated storage (activation domain only; capacity 8): `engine/gpu/WorkshopConnections.cs@a6c914e:L11-L13`, `engine/gpu/WorkshopConnections.cs@a6c914e:L65-L99`.
- Contextual Connect UI: select part → "Connect <socket>" → click target → choose socket pair (`ui/WorkshopConnections.cs@a6c914e:L37-L72`).

**Missing**
- `Gas` domain and gas socket rows; free-end connection targets — [S257](../invest/decisions.md#s257) typed ports, [S470](../invest/decisions.md#s470) open-versus-sealed (S697).
- FluidAdvection through a lossy link with shared inventory — [S416](../invest/decisions.md#s416) advection, S470 gas-state (S471).
- Hose rendering (sagging tube following sockets) — presentation slice of the first pneumatic story.

**Dependencies.** At least one gas source (EL-037 or a charged EL-039) and one consumer (EL-040/041/042/043 or a piston).

## 4. Sources and legacy

- Requirement row [element-038](../requirements.md#element-038): "Connects compatible gas ports with finite capacity and resistance"; outcome "Open or disconnected hose vents or isolates according to its boundary declaration". Integration task [todo-426](../requirements.md#todo-426) requires testing hose disconnection.
- Named entry [element-038](../invest/named-elements.md#element-038), owner S473. Gas design: "Pneumatic hose (EL-038) | Gas network node pair | length | Carries inventory between seated ports only | None (static)".

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| H1 | A link resolves only with exactly one matching port at each end, the link's domain on both, output (or bidirectional) → input; undefined domains reject. | `engine/ConnectionPort.cs@a6c914e:L40-L67` | carry forward the rule for the Gas domain |
| H2 | Legacy connection domains were Activation, Electrical, Signal, Mechanical and Rope, serialized snake_case; no gas domain existed. | `engine/MachineData.cs@a6c914e:L67-L70` | carry forward snake_case boundary naming; a `gas` domain is new |
| H3 | Rope links carry an explicit length 0.05–200 m; other domains reject a length. | `engine/ConnectionPort.cs@a6c914e:L47-L52` | do not carry forward the rope bounds; hose length has its own range |
| H4 | Typed sockets require matching domain, direction and identity; a duplicate source port or missing socket id rejects. | `CuriousContraptions.tests/ConnectionPortTests.cs@a6c914e:L18-L38` | carry forward as acceptance facts |

- **Files harvested:** `engine/ConnectionPort.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/ConnectionPortTests.cs`.

## 5. Acceptance outline

Acceptance authority: [element-038](../requirements.md#element-038), [pneumatics profile](../invest/profiles.md#pneumatics).

- **Construction (actual Chrome UI).** Charged Air tank and spring-return piston; select the tank, Connect `GasOut`, click the piston, choose `GasOut → GasIn`: a cyan hose appears between the actual sockets. For free ends, Connect `GasOut` then click empty bench and choose Vent or Sealed.
- **Positive.** Connected: opening the valve extends the piston; the tank dial falls by the gas moved plus the small hose fill.
- **Negative / control.** Vent free end: the tank empties to ambient and nothing moves; Sealed free end: the tank holds pressure (only the hose volume fills); no hose: the piston never moves; gas → electrical socket refused; hoses crossing in space never exchange gas.
- **Boundaries.** `length` 0.5 and 8 m (the 8 m hose fills more slowly); equal pressures produce no flow; reversed output→output link refused.
- **Run/Reset.** Reset restores every node and hose fill exactly.
- **Save/Load.** Endpoints, socket names, `length` and `free_end` round-trip.
- **Integrations.** Pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393) ("chooses a valve/air-hose route and drives the appropriate piston"); [todo-426](../requirements.md#todo-426) requires testing hose disconnection, and [todo-427](../requirements.md#todo-427) feeds both chambers through hoses; combination [todo-378](../requirements.md#todo-378) "One Breath"; interaction processes [IX-08 fluid advection](../requirements.md#interaction-08) and [IX-40 gas state evolution](../requirements.md#interaction-40). Campaign: "pneumatic hose" is named in the pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 71–80 (reuse 91–100, 136–150).

## 6. Open questions

1. **Connection or part.** The requirement row says "connects compatible gas ports"; the hose is specified as an authored connection with free-end targets. Whether a hose is instead a placed part with two fittings: owner decision.
2. **Runtime disconnection.** Whether a hose can be pulled off during Run (an event that turns Connected into Vent): owner decision.
3. **Connection capacity.** The current construction caps all connections at 8 (`engine/gpu/WorkshopConnections.cs@a6c914e:L67-L67`); whether hoses share that cap: owner decision.
