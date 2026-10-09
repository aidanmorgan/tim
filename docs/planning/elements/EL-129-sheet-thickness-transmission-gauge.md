# EL-129 · Sheet-thickness transmission gauge — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [element-129](../requirements.md#element-129). A toy inspection instrument for fictional toy sources in game units; never real-world gauging guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-129 · Sheet-thickness transmission gauge |
| Type | Radiation |
| Anchor | [requirements.md#element-129](../requirements.md#element-129); [named-elements.md#element-129](../invest/named-elements.md#element-129); scope source [radiation-11](../requirements.md#radiation-11) (umbrella "Transmission gauge", index only) |
| Related identities | Sibling [EL-130 Tank-level gauge](EL-130-tank-level-transmission-gauge.md); sources [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-02](RAD-02-powered-x-ray-emitter.md); sample materials from [EL-127](EL-127-polymer-shield.md), [EL-128](EL-128-dense-shield.md); shared detector defaults [RAD-08](RAD-08-radiation-rate-meter.md); routing via [CAT-019 Conveyor](CAT-019-conveyor.md) and a [powered diverter EL-163](../invest/named-elements.md#element-163). No CAT spec. |
| Proof owner | S618 |
| Roadmap story | unscheduled; campaign chapter 12 (levels 111–120; research slot 88 → 113) |
| Status | not started |

## 2. Declaration

The requirement fixes: a supplied calibrated detector estimates a supported sheet thickness from transmission; a missing source is invalid rather than maximum thickness; thin and thick samples sort correctly (radiation-11 task). Values are proposals.

- **Bodies and shapes.** Static box 0.50 × 0.60 × 0.30 m with a 0.30 × 0.30 m sensing face on local +X (proposed: the RAD-08 envelope). The source is a separate part placed opposite across the sample gap.
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`).
- **Constraints.** none.
- **Typed ports.** `PowerIn` (Electrical, Input) and `Supply` (Electrical, Output), existing sockets (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`); switched route closed while the estimate reads Thick.
- **Calibration and reference path.** At Run admission the gauge pairs with the strongest compatible photon source whose centre lies within ±5° of its face normal, and records the reference reading R_ref (the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration) with dynamic sample bodies excluded) (proposed: an automatic, geometry-based pairing; no wire carries radiation). No paired source, R_ref < 2.0 rate units, or misalignment → state `Invalid` with a visible indicator; the contact stays open (never "thick").
- **Sensors and activation.** `RadiationInstrumentState` plus `TransmissionGaugeMode.SheetThickness` (research typed name; fixed by identity). Estimate L_est = −ln(R / R_ref) / μ(sample material, source band), evaluated at substep endpoints while a sample occupies the path.
- **Work and energy stores.** none.
- **Parameters.**
  - Sample material `RadiationMaterialKind` (Polymer, Dense, ThinScreen), inspector-selected, default Polymer (proposed: the gauge is calibrated for one supported material; others are unsupported and read Invalid).
  - Thick threshold f32, 0.01–0.50 m, default 0.075 m (proposed: halfway between the 0.05 m Standard and 0.10 m Thick shield presets, so the two known samples sort).
- **Cosmetic curves and UI bindings.** Dial ← committed estimated thickness (research row "Dial ← transmission"); Invalid flag; Thin/Thick lamp.
- **Art.** Cream `#fff8e9` case, navy `#293954` dial with a gold `#f7cb52` thickness pointer, engraved sheet-and-arrow mark (proposed: shape cue for the sheet mode).
- **Catalogue and inventory.** Id `sheet_thickness_gauge`, title "Thickness gauge", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** The row names none; the supported sample materials are calibration settings, each separately proven.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/element-02.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); reference-path query excluding dynamic bodies: unscheduled (P0-004, S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F) |
| SignalPropagation | missing | Story 8.1 (supplied contacts); the current `engine/gpu/ActivationNetwork.cs@a6c914e:L7-L14` is activation-only |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; calibration recomputed each Run |

Also used: ElectricalPower (missing; Story 8.1); inspector selectors for material and threshold (unscheduled with this element).

**Dependencies.** A photon source; samples on transport (CAT-019 Conveyor, Story 11.1); a diverter for routing; CAT-005 Battery.

## 4. Sources and legacy

- [element-129](../requirements.md#element-129) and the [radiation-11](../requirements.md#radiation-11) integration task: "Known thin/thick samples sort correctly … Missing source is invalid, not a valid heavy/full reading."
- [Named entry](../invest/named-elements.md#element-129), owner S618; map row (S605, S257); binding `docs/coverage/engine/element-02.json` (proof S618, relation radiation-11).
- [S605](../invest/decisions.md#s605) photon row; research "Inspection" law (valid source and reference path, calibrated on a known sample; dead source, misalignment or unsupported material sets Invalid) and slot 88 "Weighing Without Touch".
- **Legacy.** None. Hits are coverage snapshots only: older radiation-11 binding copy listing this identity `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1964-L2008`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Gamma capsule and supplied gauge 1 m apart facing each other across a conveyor (R_ref ≈ 15.8); gauge `Supply` → a powered diverter; Standard and Thick polymer samples ride through.
- **Positive.** Thick sample (0.10 m, R ≈ 11.7): estimate ≈ 0.10 m, contact closes, diverter sends it to the Thick branch; Standard (0.05 m, R ≈ 13.6) reads ≈ 0.05 m and goes the other way.
- **Negative / controls.** Capsule removed (dead source): Invalid, contact open, both samples take the default branch. Source rotated 10° off the gauge axis: Invalid. Water sample with Polymer calibration: Invalid.
- **Boundaries.** Estimate exactly at the threshold; no sample in path reads 0 m; Dense calibration with dense samples.
- **Run/Reset.** State Unpowered; R_ref recomputed at next Run.
- **Save/Load.** Material, threshold, pose and wires round-trip.
- **Integrations.** Cross-element task [radiation-11 integration (sequence-task-419)](../requirements.md#sequence-task-419) (known thin/thick samples sort; missing source is invalid); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-06 Electrical power transfer](../requirements.md#interaction-06) and [IX-07 Signal propagation](../requirements.md#interaction-07); supplied-port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 111–120 as a RAD-11 gauge mode ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 12 of the [campaign plan](../requirements.md#campaign-plan)), reuse 121–125 and 136–150.

## 6. Open questions

1. Automatic geometric pairing versus an explicit player-made calibration link to a chosen source — owner decision.
2. Is EL-129 a separate part or a mode of one gauge shared with EL-130 (research `TransmissionGaugeMode`) — owner decision.
3. Proposed acceptance cone, minimum valid reading and threshold need S606-D.
