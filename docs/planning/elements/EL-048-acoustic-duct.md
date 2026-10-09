# EL-048 · Acoustic duct — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Shared acoustic facts A1–A13 are in [EL-044](EL-044-tone-selective-sound-meter.md#shared-acoustic-family-facts); acoustic route rules R1–R6 in [EL-046](EL-046-listening-horn.md#acoustic-route-rules-shared-by-el-046-el-047-el-048).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-048 · Acoustic duct |
| Type | Sound |
| Anchor | [requirements.md#element-048](../requirements.md#element-048); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-048); owner S535 |
| Related | Refines no CAT spec. Joins [EL-046 Listening horn](EL-046-listening-horn.md) `SoundOut` to [EL-047 Exit horn](EL-047-exit-horn.md) `SoundIn`. Connection-kind sibling of [EL-038 hose](EL-038-pneumatic-hose.md) and [EL-197 wire](EL-197-electrical-wire.md); not the [CAT-048](../requirements.md#current-cat-048) ball pipe (R6). |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

The duct is an authored typed connection from one `SoundOut` to one `SoundIn`, drawn with the Connect control; it is not a placed rigid part.

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | none for physics: no collider, so balls pass through its visual tube and it never occludes light or sound **proposed** (keeps the duct a pure route; the horns are the physical parts). Visual tube diameter 0.18 m **proposed**. |
| Mass and material | none. |
| Constraints | none; endpoints follow the named sockets under each horn's transform. |
| Typed ports | Consumes exactly one Acoustic Output (`SoundOut`) and one Acoustic Input (`SoundIn`); the exact-one-port, same-domain, output → input rule of the legacy resolver applies (`engine/ConnectionPort.cs@a6c914e:L40-L67`). An Acoustic socket never accepts Gas, Electrical or Activation links. |
| Sensors and activation | none. |
| Work and energy stores | none. Transit rule R3: delay L / 12 m/s, strength × e^(−0.05·L); elbow loss below; each routed pulse traverses a given duct at most once (R5). |
| Parameters | `route`: closed enum `DuctRoute { Straight, Elbow }` (variants below). Length L is derived from the socket positions, not typed in **proposed** (no numeric placement menu, AGENTS rule); admitted only when 0.2 m ≤ L ≤ 8 m **proposed** (the 8 m upper bound equals the open-air pulse range, A2, so a duct never outranges free air). |
| Cosmetic curves and UI bindings | A gold `#f7cb52` dot travels along the tube at 12 m/s ← committed routed pulse position (component-research binding "Pulse travel ← committed transit", [component research](../../component-research.md#sound)); dot opacity ∝ strength. |
| Art | Cream `#fff8e9` tube with navy `#293954` bands every 0.5 m **proposed**: different in shape and colour from the cyan air hose, navy electrical cable and gold activation link (DESIGN preservation check: distinguish connection types through shape as well as colour). |
| Catalogue / inventory | Id `acoustic_duct`, Title "Sound duct", Category Sound **proposed**; budgeted per level. Description **proposed**: "Carries caught sound from a listening horn to an exit horn, a little later and a little quieter. Unconnected mouths carry nothing." |

**Variants** (campaign coverage lists "each duct route", [campaign-element-coverage](../requirements.md#campaign-element-coverage); recipe "Quiet Corridor" uses a "duct elbow"):

| Variant | Geometry | Length L | Extra loss |
| --- | --- | --- | --- |
| Straight | one straight segment between the two sockets | Euclidean socket distance | none |
| Elbow | two straight segments meeting at one 90° corner, corner chosen on the horizontal plane through the source socket **proposed** (routes around a wall without a free-form path editor) | sum of both segments | × 0.9 per elbow **proposed** (a bend loses a little more than straight run) |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S535): AcousticPropagation, FiniteLedger, GeometryQuery, SignalPropagation (+ StateTransaction).

**Exists now**
- Typed connection record, validation and contextual Connect UI (activation domain only, capacity 8): `engine/gpu/WorkshopConnections.cs@a6c914e:L11-L13`, `engine/gpu/WorkshopConnections.cs@a6c914e:L83-L99`, `ui/WorkshopConnections.cs@a6c914e:L37-L72`.

**Missing**
- Acoustic domain, sockets and route rendering — [S257](../invest/decisions.md#s257); first acoustic-route story (unscheduled).
- Routed occurrences with delay, loss and loop rejection — [S528](../invest/decisions.md#s528) event-routing → S529.

**Dependencies.** EL-046 and EL-047 (its only endpoints); a sound source and a meter to observe.

## 4. Sources and legacy

- Requirement row [element-048](../requirements.md#element-048): "Routes bounded acoustic energy between compatible ports with travel and loss"; outcome "Disconnected mouths cannot carry a hidden signal".
- Named entry [element-048](../invest/named-elements.md#element-048), owner S535; component research rows "Acoustic network node | Horn/duct typed ports | Validated connected ducts carry attenuated, delayed pulses; ball pipes do not silently gain acoustic capability" and EL-046–048 ([component research](../../component-research.md#sound)); [S528](../invest/decisions.md#s528) event-routing construction: "One horn/duct loop with repeated arrivals and a second strike: the meter triggers once per admissible path, the loop does not grow, the second strike triggers again".
- Legacy: no duct; connection-resolution facts only (`engine/ConnectionPort.cs@a6c914e:L40-L67`; serialized domains `engine/MachineData.cs@a6c914e:L67-L70`, no acoustic domain).
- **Files harvested:** `engine/ConnectionPort.cs`, `engine/MachineData.cs` (connection rules only); acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-048](../requirements.md#element-048), [acoustic profile](../invest/profiles.md#acoustic), [S528](../invest/decisions.md#s528) event-routing construction.

- **Construction (actual Chrome UI).** Speaker → Listening horn; select the horn, Connect `SoundOut`, click the Exit horn, choose `SoundOut → SoundIn` and the route (Straight or Elbow) around a Wall; supplied Sound meter facing the exit horn.
- **Positive.** Each variant separately: the dot travels the tube, the meter triggers after L/12 s; the elbow route arrives slightly weaker than an equal-length straight route.
- **Negative / control.** Delete the duct: the meter stays silent although both horns remain (outcome); two crossing ducts never exchange pulses; a ball pipe placed mouth-to-mouth carries no sound; a loop exit horn → listening horn → same duct does not grow and triggers once per path, and a second strike triggers again (S528).
- **Boundaries.** L at 0.2 m and 8 m admitted, beyond 8 m refused at connection; acoustic → gas or electrical socket refused.
- **Run/Reset.** Reset clears in-transit pulses.
- **Save/Load.** Endpoints, socket names and `route` round-trip.
- **Integrations.** Acoustic integration task [sequence-task-407](../requirements.md#sequence-task-407) ("connected horn/ducts carry bounded sound") between [EL-046](EL-046-listening-horn.md) and [EL-047](EL-047-exit-horn.md); interaction process [IX-12 acoustic propagation](../requirements.md#interaction-12) (path and attenuation). Campaign: "each duct route" is named in the sound row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 71–80 (reuse 81–100, 117–125, 136–150).

## 6. Open questions

1. **Connection or part.** A duct as a connection (proposed) versus placed straight/elbow segments that seat mouth to mouth like the clear pipe: owner decision.
2. **Collision.** Whether a duct should be solid (blocking balls and light): owner decision.
3. **Elbow placement.** Automatic corner (proposed) versus a draggable corner handle: owner decision.
