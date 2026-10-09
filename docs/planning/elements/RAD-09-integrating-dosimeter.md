# RAD-09 · Integrating dosimeter — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-09](../requirements.md#radiation-09) (P1 potential). Game exposure units for fictional toy sources; never real-world dosimetry.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-09 · Integrating dosimeter |
| Type | Radiation (accumulating sensor, supplied contact) |
| Anchor | [requirements.md#radiation-09](../requirements.md#radiation-09); [named-elements.md#radiation-09](../invest/named-elements.md#radiation-09) |
| Related identities | Rate-only counterpart [RAD-08](RAD-08-radiation-rate-meter.md) (shared rate law); burst source [RAD-02 X-ray emitter](RAD-02-powered-x-ray-emitter.md); passive cargo counterpart [RAD-10](RAD-10-exposure-sensitive-cargo-badge.md); feedback partner [RAD-06 shutter](RAD-06-powered-radiation-shutter.md). Must stay distinct from [CAT-033 hold timer](CAT-033-hold_timer.md) (continuous residence timer); bursts come from [CAT-017 Clock](CAT-017-clock.md). No CAT spec refined. |
| Proof owner | S616 |
| Roadmap story | unscheduled; campaign 109, 110 (research slots 84, 85) |
| Status | not started |

## 2. Declaration

The requirement fixes: weighted exposure accumulates across separate bursts; power loss disables the contact but keeps the accumulated measurement within a Run; Run/Reset clears it to the authored initial value; not a continuous hold timer. Values are proposals.

- **Bodies and shapes.** Static box 0.45 × 0.55 × 0.30 m with a 0.30 × 0.30 m sensing face on local +X (proposed: same receiver face as RAD-08 so readings are comparable).
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** `PowerIn` (Electrical, Input) and `Supply` (Electrical, Output), existing sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`); switched contact route `PowerIn` → `Supply`, as in RAD-08.
- **Sensors and activation.** `RadiationInstrumentState` (Unpowered, Measuring, Saturated, Invalid). While Measuring, D(next) = D(now) + R_w · dt at every substep endpoint, with R_w the weighted [shared rate](RAD-08-radiation-rate-meter.md#2-declaration). Contact closes when powered and D ≥ threshold; D never decreases within a Run, so the contact stays closed while powered. Unpowered: contact open, D retained and not advanced (proposed: an electronic instrument does not integrate while off; open question 1).
- **Response weights.** Gamma and XRay 1.0, BetaMinus 0.5, Alpha 0, Neutron 0 (proposed: a "weighted" exposure whose window stops alpha, giving a simple teachable weighting; neutrons belong to EL-131/EL-132).
- **Work and energy stores.** Accumulated exposure D (f32, game exposure units) — a measurement store, not energy. Saturates at 4096 (state Saturated; never wraps).
- **Parameters.**
  - Threshold f32, 1–4096 exposure units, default 30 (proposed: an X-ray emitter at 1 m reads ≈ 15.79, so one 1 s burst gives D ≈ 15.8 (below), and two bursts or one 2 s burst give D ≈ 31.6 (above) with 1.6 units of margin — a threshold equal to the nominal total, such as 32, never trips because f32 accumulation lands just below it; inside the research budget range 1–4096).
  - Authored initial D f32, 0 to threshold, default 0 (proposed: the requirement's "authored initial value" as a level-authoring field; players see it read-only).
- **Cosmetic curves and UI bindings.** Bar ← accumulated D with a threshold mark; contact lamp; info card "remembers exposure" versus the rate meter's "measures now" ([research presentation](../../radiation-component-research.md#presentation)).
- **Art.** Cream `#fff8e9` case, navy `#293954` face with a vertical gold `#f7cb52` fill bar, engraved Σ mark (proposed: the bar shape distinguishes it from the meter's needle).
- **Catalogue and inventory.** Id `integrating_dosimeter`, title "Dosimeter", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** None in the row.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); per-instrument accumulated store; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); sampled path queries: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F) |
| SignalPropagation | missing | Story 8.1 (supplied contacts); the current `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14` is activation-only |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; D commits with the tick and clears on Reset (unscheduled with this element) |

Also used: ElectricalPower (missing; Story 8.1). Threshold selector in the inspector: unscheduled with this element.

**Dependencies.** CAT-005 Battery; RAD-02 X-ray emitter; a burst source on its `EnableIn` (CAT-017 Clock, Story 9.3, with a 1 s pulse width).

## 4. Sources and legacy

- [radiation-09](../requirements.md#radiation-09): "Two separated exposures add to the same total as one equivalent exposure; darkness does not erase the total. Run/Reset clears it to the authored initial value."
- [Named entry](../invest/named-elements.md#radiation-09), owner S616; map notes "Source D: accumulated dose across bursts and retained value on supply loss, not residence reset" and "residence reset is forbidden"; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research row (bar ← accumulated D; slots 84 "A Little at a Time", 85 "Close at the Mark").
- **Legacy.** No dosimeter part, test or level. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1900-L1930`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** A supplied X-ray emitter (Medium band) facing a supplied dosimeter 1 m away; a Clock drives the emitter's `EnableIn` with 1 s enable pulses; dosimeter `Supply` → Powered gate. The emitter switches on and off at tick boundaries, so each burst is a clean 1 s exposure (no blade travel).
- **Positive.** Two 1 s bursts separated by 3 s: D ≈ 31.6 ≥ 30 and the gate releases. One continuous 2 s burst gives the same D within f32 rounding.
- **Negative / controls.** One 1 s burst: D ≈ 15.8, gate shut. The 3 s dark gap does not reduce D. Supply removed after the first burst: contact open, D kept; supply restored before the second burst: release follows. A hold timer (CAT-033) in the same layout resets on the gap; the dosimeter does not.
- **Boundaries.** D crossing exactly 30; saturation at 4096; an Alpha source gives zero D.
- **Run/Reset.** D returns to the authored initial value; state Unpowered.
- **Save/Load.** Threshold, initial value, pose and wires round-trip; the saved construction carries no in-Run D.
- **Integrations.** No row-level cross-element task names the dosimeter; it integrates through interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-06 Electrical power transfer](../requirements.md#interaction-06) and [IX-07 Signal propagation](../requirements.md#interaction-07); supplied-port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 101–110 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 11 of the [campaign plan](../requirements.md#campaign-plan)), reuse 111–120, 124–130 and 136–150, including the shutter-feedback lesson with RAD-06.

## 6. Open questions

1. Does the dosimeter integrate while unpowered (passive film-badge behaviour) or only retain — owner decision.
2. Proposed response weights and the default threshold 30 need S606-D.
3. Is the authored initial value player-editable or level-authored only — owner decision.
4. The Clock emits an activation pulse (CAT-017 `ActivationOut`); whether RAD-02 `EnableIn` accepts it directly or needs a supplied signal stage is the RAD-02 open question 1 — owner decision.
