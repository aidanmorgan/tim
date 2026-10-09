# RAD-10 · Exposure-sensitive cargo badge — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-10](../requirements.md#radiation-10) (P1 potential). Game exposure units for fictional toy sources; never real-world dosimetry.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-10 · Exposure-sensitive cargo badge |
| Type | Radiation (passive detector on dynamic cargo + delivery goal) |
| Anchor | [requirements.md#radiation-10](../requirements.md#radiation-10); [named-elements.md#radiation-10](../invest/named-elements.md#radiation-10) |
| Related identities | Supplied counterpart [RAD-09 Dosimeter](RAD-09-integrating-dosimeter.md); sources [RAD-01](RAD-01-gamma-source-capsule.md); shields [EL-128](EL-128-dense-shield.md); transport [CAT-019 Conveyor](CAT-019-conveyor.md); dock [CAT-004 Basket](CAT-004-basket.md) / Receiver; goal family [EL-123 Protected-end-state goal](../invest/named-elements.md#element-123). No CAT spec refined. |
| Proof owner | S617 |
| Roadmap story | unscheduled; campaign 111, 112 (research slots 86, 87) |
| Status | not started |

## 2. Declaration

The requirement fixes: a passive badge on a dedicated movable cargo piece accumulates exposure along its actual trajectory; delivery succeeds only inside a taught exposure window; dose is kept through stops and lost only on Reset. Values are proposals.

- **Bodies and shapes.** One dynamic box 0.50 × 0.40 × 0.50 m (proposed: a parcel that rides the conveyor and fits a Basket/Receiver capture volume, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L120`). The badge is a 0.15 × 0.15 m plate on the top face, part of the same body (proposed: attached, so no joint can break and dose follows the body identity).
- **Mass and material.** 2.0 kg; restitution 0.2, friction 0.5, rolling resistance 0 (proposed: a cardboard-like parcel that slides and does not bounce; between Basketball 1 kg and Bowling ball 4 kg).
- **Constraints.** none.
- **Typed ports.** none — passive; "a badge records without a signal socket" (research measurement law).
- **Sensors and activation.** Passive detector bound to the cargo `GpuBodyId`; D(next) = D(now) + R_w · dt sampled at every substep endpoint with the speculative margin, so a fast crossing cannot vanish between samples. The badge plate is a 0.15 m receiver face sampled 3 × 3 under the [shared rate law](RAD-08-radiation-rate-meter.md#2-declaration), isotropic (no facing); at 1 m from a Gamma capsule it reads ≈ 15.9. Weights Gamma/XRay 1.0, BetaMinus 0.5, Alpha 0, Neutron 0 (proposed: same weighting as RAD-09 so lessons transfer).
- **Work and energy stores.** Accumulated exposure D (f32, game exposure units, 0–4096). Kept through stops; only Reset clears it.
- **Goal declaration (ObjectiveEvaluation).** Level-authored `ExposureGoalKind` (AtMost, Within) with bounds, evaluated when the parcel's residence sensor in the dock is satisfied (`ResidenceSensorDeclaration`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L123-L141`); `ExposureBandState` (Below, Inside, Above) is published for the badge display. Defaults: Within [0, 24] (proposed: 3 s at 1 m from a Gamma capsule gives ≈ 48 and fails, while the same 3 s behind a Thick dense slab gives ≈ 6.5 and passes).
- **Parameters.** None player-editable; the window is level-authored (inside the research budget range 1–4096).
- **Cosmetic curves and UI bindings.** Badge display ← D: four engraved segments fill progressively and a band glyph shows Below/Inside/Above, with tint as a secondary cue (research "badge tint ← D", plus a shape cue so colour is never alone).
- **Art.** Cream `#e8d4a6` parcel (Domino cream), navy `#293954` strapping, gold `#f7cb52` badge rim (proposed: approved palette values; the badge reads as a small instrument).
- **Catalogue and inventory.** Id `badged_parcel`, title "Badged parcel", category `Radiation` (proposed: snake_case id; [grouped menu](../requirements.md#palette-type-groups)); usually level-placed and locked.

**Variants.** None in the row; AtMost and Within goal kinds are separately proven goal modes.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); per-body accumulated store; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); paths to a moving receiver: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); moving receiver sampled at substep endpoints |
| ObjectiveEvaluation | exists now for Receiver capture only | `engine/gpu/WorkshopGoalEvaluator.cs@a6c914e:L5-L8`; exposure-window predicate combined with arrival: unscheduled (LAW-GOALS-D) |
| ReliablePublication | exists now | `engine/gpu/BrowserWorkshopClient.cs@a6c914e:L17-L17`; publishes the goal phase |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; D commits with the tick and clears on Reset (unscheduled with this element) |
| TypedContracts | exists now | `engine/gpu/WorkshopWire.cs@a6c914e:L6-L19`; new goal and band enums join it |

The cargo composition also uses RigidBodyDynamics, ContactImpulse and SlidingFriction (exist now: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`, `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L623-L726`) and the residence sensor (exists, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L123-L141`).

**Dependencies.** A source; transport (CAT-019 Conveyor, Story 11.1, or a ramp); a dock (Basket/Receiver, delivered).

## 4. Sources and legacy

- [radiation-10](../requirements.md#radiation-10): "A shielded route delivers within budget; the same endpoint reached by an exposed route fails. Cargo retains its dose through stops and loses it only on Reset."
- [Named entry](../invest/named-elements.md#radiation-10), owner S617; map composition "bind dose integration to actual cargo BodyId/trajectory; cargo composition supplies RigidBodyDynamics/ContactImpulse, never an animated path"; binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) photon row; research row (slots 86 "The Sheltered Parcel", 87 "Briefly in the Open").
- **Legacy.** No badge, cargo-dose test or level. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1931-L1963`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** A level with a locked Gamma capsule beside a conveyor, a locked dock and the badged parcel; the player places Dense shields (or reroutes ramps) through palette and gizmo.
- **Positive.** Shielded route: parcel docks with D ≈ 6.5, inside [0, 24]; Solved.
- **Negative / controls.** Same dock reached by the exposed route (3 s at about 1 m): D ≈ 48, Above, not solved. A parcel stopped in the open zone accumulates beyond budget (lesson 87 control).
- **Boundaries.** D exactly at the bound; a fast crossing still accumulates; the parcel pausing in shade keeps its dose; Within window with a non-zero lower bound fails an over-shielded route.
- **Run/Reset.** D returns to 0, pose to authored.
- **Save/Load.** Parcel pose and goal window round-trip; no in-Run D is saved.
- **Integrations.** No row-level cross-element task names the badge; it integrates through interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15), [IX-01 Contact impulse](../requirements.md#interaction-01) and [IX-02 Sliding friction](../requirements.md#interaction-02) (the parcel rides real transport); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 111–120 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 12 of the [campaign plan](../requirements.md#campaign-plan)), reuse 121–125 and 136–150.

## 6. Open questions

1. Is the badge a separate attachable part (to any cargo) or only this dedicated parcel — the row says "dedicated movable cargo piece"; confirm — owner decision.
2. Proposed parcel size, mass and default window need S606-D and LAW-GOALS-D.
3. Should dose be evaluated at dock entry or at level end — owner decision.
