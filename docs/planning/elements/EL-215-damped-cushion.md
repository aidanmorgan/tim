# EL-215 · Damped cushion named-identity spec

Story 7.0 named-identity spec (Batch J). Baseline commit `a6c914e`; every citation is `path@a6c914e:Lstart-Lend` and resolves with `git show a6c914e:<path> | sed -n 'start,endp'`. **Proposed** marks an unsourced design value with a one-line justification; the owner may revise it. All values are canonical IEEE-754 f32 game values inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope).

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-215 |
| Name | Damped cushion |
| Type | Mechanical |
| Anchor | [requirements.md#element-215](../requirements.md#element-215); [named entry](../invest/named-elements.md#element-215); campaign 1–10, practice 41–50, reuse 96 and 149 ([campaign reservations](../invest/campaign-reservations.md)) |
| Proof owner | S381 |
| Refines / extends | No CAT spec. Same compliant-surface law as [CAT-065 trampoline](CAT-065-trampoline.md), tuned to dissipate rather than return energy |
| Related | EL-216 Capture cradle (same campaign slots), EL-194 Springboard, CAT-065 Trampoline |
| Roadmap story | unscheduled (the compliant-surface capability arrives with Story 10.5) |
| Status | not started |

## 2. Declaration

### Bodies and shapes
All full extents.
- **Base (static).** Box 1.4 × 0.12 × 1.2 m whose top face sits at the end of the stroke, so it is the physical bottom stop (the trampoline's back-plate arrangement, `parts/TrampolinePart.cs@a6c914e:L17-L19`) — **proposed** size: frames the pad with a 0.1 m border.
- **Pad.** A compliant footprint 1.2 × 1.0 m (half extents 0.6 × 0.5), rest height 0.15 m above the base top, maximum stroke 0.15 m — **proposed**: wide enough for a 0.68 m Basketball with margin, and a stroke that holds a dropped Basketball but not a dropped Bowling ball (see Parameters); the declaration shape is the legacy compliant surface (`engine/SceneCompliantSurface.cs@a6c914e:L9-L22`). No collider on the pad itself.

### Mass and material
- Static, no mass. Base and rim material restitution 0.4, threshold 0.1 m/s, friction 0.3 — **proposed**: a bottomed-out impact rebounds from the hard base, so arrest is not guaranteed, as the requirement demands.
- Pad: massless spring-damper; no tangential friction, so a grazing ball keeps its tangential speed (legacy law is normal-only, `engine/physics/CompliantContactLoad.cs@a6c914e:L42-L66`).

### Constraints and joints
None. One unilateral spring-damper per dynamic body over the footprint: normal force = max(0, spring force − c·v) with c = 2·ζ·√(k / m_eff); it never pulls (`engine/physics/CompliantContactLoad.cs@a6c914e:L42-L66`). A body engages only when it enters from above the rest height moving down inside the footprint (`engine/physics/CompliantContactState.cs@a6c914e:L50-L68`).

### Typed ports
None. Passive.

### Sensors and activation
None. Contact phase is internal state: Ready above the pad, Engaged after a downward entry, Unarmed otherwise (rows 6–8 below).

### Work and energy stores
Elastic store ½·k·d²; damping work is debited to a heat entry in the finite ledger so ΔK + ΔU_gravity = −(elastic store) − (heat) (requirement: "deformation/heat loss balances work"). The cushion always starts Unloaded: a preload would be initial energy, never an impact (row 9 below).

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| `stiffness` | f32 | 120–1200 | 300 | N/m | range is the sourced compliant-surface family range (`parts/TrampolinePart.cs@a6c914e:L54-L61`); default **proposed**: a Basketball dropped 1 m onto the pad stops after about 0.091 m of compression, inside the 0.15 m stroke |
| `damping_ratio` | f32 | 0.6–2.0 | 1.2 | ζ | **proposed**: overdamped by default so a normal drop does not rebound; above the trampoline's 0.08–0.8 range by design |

At the defaults a 4 kg Bowling ball dropped 1 m needs about 0.20 m of compression, so it bottoms out on the base after the 0.15 m stroke — the boundary the requirement asks for.

### Cosmetic curves and UI bindings
Pad skin dents under committed contact patches, as the trampoline membrane (`parts/TrampolinePart.cs@a6c914e:L96-L141`); a brief warm tint on heavy absorption — **proposed**, cosmetic only. UI: parameters through the configuration pattern (`ui/WorkshopConfiguration.cs@a6c914e:L43-L75`).

### Art
Pad in the Domino cream `#e8d4a6` (soft, matte), navy `#293954` base, cream `#fff8e9` rim ([DESIGN colour system](../../../DESIGN.md#colour-system)). **Proposed**.

### Catalogue and inventory entry
Id `cushion`, title "Damped cushion", category Motion — **proposed**. Add `WorkshopPartKind.Cushion` with a counted allowance (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

### Variants
One outcome in the row; no variants.

## 3. Engine capabilities

Families: ContactImpulse, ElasticStorage, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction, plus StateTransaction ([map row](../general-engine-element-map.md); [element-03.json](../../coverage/engine/element-03.json)).

**Exists now**
- Static base box, dynamic cargo, restitution and threshold: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L120`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L886-L900`.

**Missing**
- Compliant-surface declaration and worker law (ElasticStorage): Story 10.5 (CAT-065). Heat entry in the finite ledger: unscheduled. Owner S381.

**Dependencies.** Story 10.5 compliant surface.

## 4. Sources and legacy

- Requirement: "A finite compressible dissipative material surface absorbs impact work through the shared contact/deformation model"; outcome "Grazing or bottomed-out impact does not guarantee arrest; deformation/heat loss balances work" ([element-215](../requirements.md#element-215)). Campaign row lists "cushion/cradle" in the first block ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Compliant surface: frame, half X/Z, rest height, maximum stroke, stiffness, damping ratio, initial state; finite positive values | `engine/SceneCompliantSurface.cs@a6c914e:L9-L22` | carry forward (declaration shape) | The shared deformation model. |
| 2 | Force law: interval-mean spring force minus damping 2ζ√(k/m_eff)·v, clamped at zero | `engine/physics/CompliantContactLoad.cs@a6c914e:L42-L66` | carry forward (law), do not carry forward (CPU wrench evaluation) | Dissipation with no pull. |
| 3 | Engagement only from above, moving down, inside the footprint | `engine/physics/CompliantContactState.cs@a6c914e:L50-L68` | carry forward | Grazing and side entry do not engage. |
| 4 | Accepted: outside, separated and fast-separating contacts cannot attract | `CuriousContraptions.tests/CompliantContactTests.cs@a6c914e:L50-L66` | carry forward | No pull. |
| 5 | Accepted: compression work matches the potential across unloaded and saturated boundaries | `CuriousContraptions.tests/CompliantContactTests.cs@a6c914e:L67-L87` | carry forward | Energy balance at bottoming. |
| 6 | Accepted: a body falling onto the surface goes Ready → Engaged once, with one entry record (approach 1 m/s near 0.2 s); after it leaves upward the contact is Ready again and no new entry is recorded | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L32-L57` | carry forward (behaviour), do not carry forward (capture/restore replay, `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L46-L52`) | One engagement per real entry. |
| 7 | Accepted: removing and re-adding the declaration does not rearm a body already embedded below the rest height; it stays Unarmed | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L59-L75` | carry forward | An embedded body gets no spring force. |
| 8 | Accepted: while the body or the frame does not collide, the contact stays Unarmed, the body falls at unchanged speed and no entry is recorded; re-enabling it while embedded leaves it Unarmed | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L77-L94` | carry forward | A non-colliding body is never engaged. |
| 9 | Accepted: a Preloaded initial state pushes the body up at once but records no entry ("Preload is initial energy, not a new impact"); a preload must be declared explicitly | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L96-L107` | carry forward (rule); the cushion uses Unloaded | Preload is not an impact event. |
| 10 | Accepted: duplicate declarations and a preload on a changed initial condition reject atomically, leaving loads and contact states unchanged | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L109-L120` | carry forward (rejection) | Admission. |
| 11 | A failed step restores contact history, bodies and clock (exception-driven rollback) | `CuriousContraptions.tests/CompliantContactStateTests.cs@a6c914e:L122-L140` | do not carry forward | CPU transactional rollback; the worker never faults a tick. |

**Files harvested:**
- `parts/TrampolinePart.cs` (compliant-surface family values and membrane art)
- `engine/SceneCompliantSurface.cs`
- `engine/physics/CompliantContactLoad.cs`
- `engine/physics/CompliantContactState.cs`
- `CuriousContraptions.tests/CompliantContactTests.cs`
- `CuriousContraptions.tests/CompliantContactStateTests.cs`
- Searched with no hit for `cushion`: `parts/`, `engine/` (including `engine/physics/` and `engine/bridge/`), `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/`, `reference/`, `diagnostics/`.

## 5. Acceptance outline

Follow [element-215](../requirements.md#element-215) and the [mechanics profile](../invest/profiles.md#mechanics).
- **Chrome UI recipe.** Place the cushion on the bench and a Basketball 1 m above it; Run. Repeat with a Bowling ball and with a ball rolled across the pad.
- **Positive.** The Basketball lands and stops on the pad with no visible rebound.
- **Negative/control.** A rolling or grazing ball crosses without arrest; a Bowling ball bottoms out and rebounds off the base; a trampoline at the same spot returns the ball; a ball placed already inside the pad volume gets no spring push; a ball passing a non-colliding cushion falls through unchanged.
- **Boundaries.** `stiffness` and `damping_ratio` endpoints; drop at the footprint edge; ledger shows heat equal to lost mechanical energy; a ball that bounces off the base and lands again counts as a new entry.
- **Run/Reset.** Pad uncompressed; heat ledger and contact phases cleared.
- **Save/Load.** Parameters and pose round-trip.
- **Integrations.** Element reservations 1–10, practice 41–50, reuse 96 and 149 ([element-215](../requirements.md#element-215)); campaign row 1 ("cushion/cradle") first use 1–10, reuse 21–30, 41–50, 91–100, 136–150 ([campaign-element-coverage](../requirements.md#campaign-element-coverage)); paired with EL-216 capture cradle and CAT-065 trampoline.

## 6. Open questions

1. Should the pad carry tangential friction (the legacy compliant law has none)? Unspecified — owner decision.
2. Does dissipated heat feed the thermal model (TH elements) or only the ledger? Owner decision.
