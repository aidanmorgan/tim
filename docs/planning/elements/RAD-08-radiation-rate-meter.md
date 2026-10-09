# RAD-08 · Radiation rate meter — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-08](../requirements.md#radiation-08) (P1 potential). The instrument reads fictional toy sources in game units; nothing here is real-world dosimetry guidance. This spec also holds the **shared proposed rate law and detector defaults** that the other radiation specs reference.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-08 · Radiation rate meter |
| Type | Radiation (sensor, supplied contact) |
| Anchor | [requirements.md#radiation-08](../requirements.md#radiation-08); [named-elements.md#radiation-08](../invest/named-elements.md#radiation-08) |
| Related identities | Sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md), [RAD-03](RAD-03-alpha-source-cartridge.md), [RAD-04](RAD-04-beta-minus-source-cartridge.md); neutron counterparts [EL-131](EL-131-fast-neutron-detector.md), [EL-132](EL-132-slow-neutron-detector.md). Supply from [CAT-005 Battery](CAT-005-battery.md); typical load [CAT-051 Powered gate](CAT-051-powered_gate.md). No CAT spec is refined. |
| Proof owner | S615 |
| Roadmap story | unscheduled; campaign 101, 108, 115, 116 |
| Status | not started |

## 2. Declaration

The requirement fixes "instantaneous response with typed radiation/energy sensitivity and separate on/off thresholds; contact output controls existing electrical devices". Numbers are proposals unless cited.

- **Bodies and shapes.** One static box 0.50 × 0.60 × 0.30 m (proposed: Battery-sized instrument, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`). Receiver aperture: a 0.30 × 0.30 m sensing face on local +X (proposed: a finite receiver area, as the research requires, small enough that a collimator can address it).
- **Mass and material.** Static, zero mass (static bodies require it, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`). Contact material restitution 1, bounce threshold 0.1 m/s, friction 0.3 — the current static-part default (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** `PowerIn` (Electrical, Input) and `Supply` (Electrical, Output), both existing `WorkshopSocket` members (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`). The contact route `PowerIn` → `Supply` is closed while the detector is on — the same switched-contact route the light receiver uses ([CAT-038 declaration](CAT-038-light_receiver.md#declaration)); the meter creates no electricity. No radiation input port: radiation crosses space.
- **Sensors and activation.** Detector with typed state `RadiationInstrumentState` (Unpowered, Measuring, Saturated, Invalid; [research](../../radiation-component-research.md#typed-boundaries)). Unpowered → contact open, reading frozen at zero. Contact closes when rate ≥ On and opens when rate ≤ Off, evaluated at substep endpoints, after a 0.05 s dwell (proposed: 6 ticks at 120 Hz suppresses single-tick flicker).
- **Shared proposed rate law** (used by every radiation spec; final law is owner question S606-D, open question 1):
  - The receiver face is sampled on a 3 × 3 grid of cell centres (offsets 0 and ±w/3 per axis: ±0.10 m for a 0.30 m face). An emitter whose emitting surface is at most 0.25 m wide is sampled at its centre (every Batch O source); a wider emitter uses a 3 × 3 grid over its face. N = emitter samples × receiver samples (proposed: the research's "finite receiver area", so apertures, partial blades and edges give fractional readings).
  - Each sample pair i is one path. Photons and neutrons: the straight segment; T_i = exp(−Σ μ·L) or the neutron group fraction over the traversed material intervals. Charged particles: T_i = 1 when some launch direction inside the source's emission cone reaches the sample point within range (straight outside field regions, bent inside a RAD-14 region), else 0. Any path outside the emitter's emission cone has T_i = 0. d_i is the path length.
  - R = w · A · (1/N) · Σ T_i / max(d_i, 0.25 m)², in game rate units; A is source activity (game units/s), w the response weight (proposed: inverse square with a 0.25 m floor so contact never yields an unbounded reading; no second distance factor, as the research forbids).
  - Reference values for A = 16, open path, face square to a centred source: 15.79 at 1 m; 3.99 at 2 m, so the default On boundary (4) lies at 1.997 m and Off (3) at 2.307 m.
  - Independent sources add their contributions; two hits are never two votes. Exposure D = ∫ R dt in game exposure units.
- **Work and energy stores.** none (rate only; no accumulated memory — the map's composition note forbids substituting dose for rate).
- **Parameters.**
  - `RadiationSensitivity` enum (Photon, Alpha, BetaMinus), inspector-selected, default Photon (proposed: one meter body with typed channel modes; neutrons belong to EL-131/EL-132).
  - On threshold f32, 0.5–64 rate units, default 4.0; Off threshold f32, 0.25 to On − 0.25, default 3.0 (proposed: gives a Gamma capsule a response region of about 2 m on the 16.8 m bench).
  - Saturation 256 rate units, fixed (proposed: equals the research's maximum activity; above it the state is Saturated and the reading clamps, never wraps).
- **Cosmetic curves and UI bindings.** Needle angle ← committed rate on a logarithmic 0–256 dial; separate On/Off tick marks; contact lamp ← contact state; optional detector clicks whose muting never changes outcomes ([research presentation](../../radiation-component-research.md#presentation)).
- **Art.** Cream `#fff8e9` case, navy `#293954` dial face, gold `#f7cb52` needle, engraved α / β / γ channel mark selected by mode (proposed: shape-coded channel, not colour alone).
- **Catalogue and inventory.** Id `radiation_rate_meter`, title "Rate meter", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

### Variant: Photon (`RadiationSensitivity.Photon`)
- w = 1 for Gamma and XRay of every `PhotonEnergyBand`; 0 for Alpha, BetaMinus, Neutron.

### Variant: Alpha (`RadiationSensitivity.Alpha`)
- w = 1 for Alpha only (proposed: the "alpha-sensitive meter" of lesson 115).

### Variant: BetaMinus (`RadiationSensitivity.BetaMinus`)
- w = 1 for BetaMinus only (lesson 116).

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and sphere/box/plane narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); sampled material-interval path queries: unscheduled (P0-004, S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F) |
| SignalPropagation | missing | Story 8.1 (supplied contacts); the current `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14` is activation-only |
| StateTransaction | exists now | construct/run/reset owner `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; meter state joins it with this element (unscheduled) |

Also used: ElectricalPower supply (missing; Story 8.1). Inspector selectors for mode and thresholds: unscheduled with this element. Cosmetic needle: `CosmeticCurveDeclaration`, `engine/gpu/WorkshopCosmetic.cs@a6c914e:L16-L17` (exists).

**Dependencies.** CAT-005 Battery (Story 8.1); an electrical load such as CAT-051 Powered gate (Story 8.2); any radiation source.

## 4. Sources and legacy

- [radiation-08](../requirements.md#radiation-08): "correct exposure switches the output; wrong channel, blocked path and missing supply do not. Muting clicks leaves results identical."
- [Named entry](../invest/named-elements.md#radiation-08), owner S615; map composition note: "separately supplied meter/output state and response thresholds; numerical rate has no accumulated-dose memory substitution"; binding `radiation-01.json` (proof S615).
- [S605](../invest/decisions.md#s605) photon/alpha rows; research per-element row (needle ← committed rate; lessons 76, 83, 90, 91 → 101, 108, 115, 116) and [first-slice acceptance](../../radiation-component-research.md#chrome-observable-acceptance-for-the-first-radiation-slice).
- **Legacy.** No rate meter in `parts/` (the legacy `sound_meter` is acoustic), engine, tests, levels, Campaign or `reference/`. Hits are coverage snapshots only: the older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1869-L1899`, identical to current, and aggregate relation lists that name this identity — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

Follows the research's first-slice recipe (slot 76 → level 101).

- **Construction (actual Chrome UI).** Place a Gamma capsule 1 m from the meter, wire Battery `Supply` → meter `PowerIn` and meter `Supply` → Powered gate `PowerIn` with Connect; set the mode in the inspector.
- **Positive.** Needle reads about 15.8, contact closes, gate releases the ball to the Receiver (Solved).
- **Negative / controls.** Capsule at 2.5 m (reading about 2.6, gate shut); meter unsupplied (no release); Thick dense slab on the path (about 2.1) versus beside it (15.8); Alpha mode with a Gamma capsule (wrong channel) stays open; audio muted gives an identical result.
- **Boundaries.** On at 1.997 m and Off at 2.307 m (hysteresis); two capsules each at 2.5 m summing to about 5.1, above On; Saturated above 256; undefined mode rejected.
- **Run/Reset.** Needle, state and contact return to Unpowered/open.
- **Save/Load.** Mode, thresholds, pose and wires round-trip.
- **Integrations.** Receiver in the cross-element task [radiation-05 integration (sequence-task-413)](../requirements.md#sequence-task-413) and the photon-only control in [radiation-19 integration (sequence-task-427)](../requirements.md#sequence-task-427); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-06 Electrical power transfer](../requirements.md#interaction-06) and [IX-07 Signal propagation](../requirements.md#interaction-07); supplied-port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 101–110 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 11 of the [campaign plan](../requirements.md#campaign-plan)), reuse 111–120, 124–130 and 136–150.

## 6. Open questions

1. The proposed rate law (3 × 3 receiver sampling, centre-sampled small emitters, inverse square with a 0.25 m floor, game units) needs the S606-D decision — owner decision.
2. Whether thresholds are player-editable or fixed per level — owner decision.
3. Whether one body with typed modes or three distinct inventory items — owner decision.
4. Whether energy-band-selective photon sensitivity is needed in addition to kind sensitivity ("typed radiation/energy sensitivity") — owner decision.
