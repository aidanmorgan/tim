# EL-001 · Finite reservoir — named-identity spec

Story 7.0 named-identity spec ([readiness spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations `path@a6c914e:Lstart-Lend` resolve with `git show a6c914e:<path>` after the Epic 7 purge. A value marked **proposed** has no legacy or requirement source; it carries a one-line justification and the owner may revise it.

## Identity

| Field | Value |
| --- | --- |
| EL ID / name / type | EL-001 · Finite reservoir · Water |
| Requirement anchor | [element-001](../requirements.md#element-001); source record [todo-334](../requirements.md#todo-334); integration task [sequence-task-384](../requirements.md#sequence-task-384) |
| Named entry | [element-001](../invest/named-elements.md#element-001); per-element proof owner S422 |
| CAT spec refined | None. No catalogue element stores liquid. |
| Related identities | EL-002 Header tank (elevated variant of the same store), EL-003 Tap, EL-004 Catch basin, EL-008–EL-013 pipe kit, EL-018 level switch, EL-026 Communicating tank, EL-030–EL-032 meters |
| Roadmap story | Unscheduled. No epic or story; the roadmap's ELEMENT-n row ([roadmap](../invest/vertical-delivery.md#rolling-playable-roadmap)) places it in campaign chapter 7 "Go with the Flow", levels 61–70. |
| Status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## Declaration

**Shared water values** (identical in every Water-1 spec, EL-001 to EL-022; each is one family constant shared with Batch G):
- Liquid density ρw = 16 kg/m³, one constant for the whole water family, aligned between Batch F (EL-001–EL-022) and Batch G (EL-023–EL-036, EL-165–EL-172) (**proposed**: catalogue bodies have mean densities of 2–44 kg/m³ — Domino 2.2, Basketball 6.1, Bowling ball 43.5; at 16 the instances this batch chose for the S416 buoyancy construction behave as it asks — the Basketball, lighter than water, floats and the Bowling ball, denser, sinks — and water loads stay comparable to ball masses).
- Volume step 2⁻⁴ m³ ([component research, water](../../component-research.md#water), line 26), which weighs 1 kg at ρw.
- Gravity 9.81 m/s² (`engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L123`).
- Orifice discharge Q = Cd · s · A · √(2 g Δh), Cd = 0.6 (**proposed**: the standard sharp-edged orifice coefficient; one bounded law for every aperture; the law itself is owned by S416 advection → S418).
- Standard water mouth: bore radius 0.10 m, collar outer radius 0.16 m, collar length 0.06 m (**proposed**: visibly narrower than the 0.65 m ball-pipe bore, `engine/gpu/WorkshopPipe.cs@a6c914e:L6-L11`, so water and ball pipes are never confused).
- Static water-vessel contact material, one family constant: restitution 0.12, bounce threshold 0.1 m/s, friction 0.3, rolling resistance 0, reusing the Receiver's values (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L57-L63`) (**proposed** reuse: vessels are the same matte toy material as the Basket).
- Buoyancy of floating bodies: immersion damping c = 1.0 1/s, one family constant (**proposed**: aligned with Batch G; full law in [EL-016](EL-016-float.md)); every floating body's buoyant acceleration must stay under the 64 m/s² force-region clamp ([capability inventory](../../gpu-f32-physics.md#capability-inventory)).

**Element declaration:**
- **Bodies and shapes.** One static body: an open-top box tank built from five box colliders, as the Receiver is (`engine/gpu/ReceiverGeometry.cs@a6c914e:L8-L19`). Outer 1.24 × 1.30 × 1.24 m (W × H × D), walls 0.12 m, base 0.15 m; interior 1.0 × 1.15 × 1.0 m (**proposed**: Receiver-scale footprint, walls as thick as the Receiver's 0.06 m half-extent). An overflow notch 0.2 m wide sits 1.0 m above the interior floor (**proposed**: makes capacity exactly 1.0 m³).
- **Mass and material.** Static body, zero mass (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L82`); shared vessel material. Contents mass ρw · V is ledger state, not body mass.
- **Constraints.** None.
- **Typed ports.** `WaterOutlet` mouth on the front wall, centre 0.10 m above the interior floor so the bore bottom is flush with the floor (**proposed**: an "empty" tank then holds no dead volume). `OpenMouth` (top aperture) captures intersecting free-stream liquid. Domain `Water` (**proposed** new `WorkshopConnectionDomain` member; today only Activation and Electrical exist, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`).
- **Sensors and activation.** None owned. Level is observed by EL-018 and EL-030–EL-032.
- **Work and energy stores.** Finite liquid store: capacity 1.0 m³ (16 steps, 16 kg). Ledger: initial volume + explicit input = contained + in transit + sinks + explicit loss ([component research](../../component-research.md#water), line 12). No mechanical work store.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | InitialVolume | f32 | 0–1.0, step 2⁻⁴ | 1.0 | m³ | parameter named by component research line 26; range and default **proposed** (full tank is the First Pour start) |
  | Capacity | f32, fixed by geometry | 1.0 | 1.0 | m³ | **proposed** (fixed; see Open questions) |

- **Cosmetic curves and UI bindings.** Waterline height ← committed volume ([component research](../../component-research.md#water), line 26). Etched fill marks every 0.25 m³ (**proposed**: four readable bands). Requires a new `AnimationFeedbackSource` member for liquid volume (`engine/gpu/WorkshopCosmetic.cs@a6c914e:L6-L16` has None, Activation, Timer, ContactWork, Capture).
- **Art.** Cream body `#fff8e9` (`DESIGN.md@a6c914e:L151-L151`), navy foot `#293954` (`DESIGN.md@a6c914e:L147-L147`), gold fill marks `#f7cb52` (`DESIGN.md@a6c914e:L154-L154`); water shown through a restrained window ([common visual contract](../requirements.md#individual-puzzle-elements)) in cyan `#66b8c9` at the clear-pipe alpha 0.16 (`DESIGN.md@a6c914e:L181-L181`) (**proposed**: reuses the approved cyan; no new palette entry).
- **Catalogue and inventory entry.** Id `reservoir`, title "Water tank", category "Water", kind `Reservoir` (all **proposed**). Counted or unlimited allowance through `PartAllowance` (`engine/gpu/WorkshopInventory.cs@a6c914e:L9-L23`).

**Variants.** The requirements row lists no variants. The elevated store is the separate identity EL-002.

## Engine capabilities

Families from the [element map](../general-engine-element-map.md) and the binding [`element-01.json`](../../coverage/engine/element-01.json) (consumer `element/element-001`, kind FutureDeclaration): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

- **Exists now.** Static bodies and box colliders (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`); constant gravity only (`engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L124`); Run/Reset (`engine/gpu/WorkshopSimulation.cs@a6c914e:L238-L245`) and the save codec (`engine/gpu/WorkshopSaveCodec.cs@a6c914e:L9-L10`); typed connections without a Water domain (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`); a finite ledger only for contact work energy (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L21-L36`).
- **Missing.** Liquid FiniteLedger, FluidAdvection and free-stream packets: decision owner [S416](../invest/decisions.md#s416) advection → S418. PressureWork (head): S416 pressure-work → S419. Water TopologyTransaction (mouth joins): no named owner; see Open questions. JointConstraint is listed by the family but unused by this element. No story builds any of these yet.
- **Element dependencies.** EL-003 Tap or the pipe kit to route the outlet; EL-004 Catch basin as the receiver; EL-012 cap as the blocked-outlet control.

## Sources and legacy

- Requirements row: "Outlet flow depletes inventory; an empty tank supplies none." No variants.
- Named entry, element-map row and coverage binding as in Identity and Engine capabilities. Decision rows: S416 advection and pressure-work; family boundary [fluids](../invest/profiles.md#fluids) ("empty supply emits nothing; overflow spills; a full container holds its declared amount"); refinement [S704](../invest/refinements.md#s704) water-route, overflow, drain.
- Campaign: first use 61–70, reuse 71–90, 114, 126–130, 136–150 ([requirements, campaign first-use table](../requirements.md#campaign-element-coverage)); lesson First Pour ([S820](../invest/refinements.md#s820)); recipes First Pour and Pressure, Not Plenty ([component research](../../component-research.md#water), lines 49 and 61).
- **Legacy search.** `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` at a6c914e contain no liquid element, level or test (terms: water, liquid, reservoir, bucket, siphon, sluice, faucet, hydraulic, gutter). "HighWater" and "buckets" hits are queue counters with no element knowledge. Analogue facts only:

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | A finite store declares capacity > 0 and 0 ≤ initial ≤ capacity; anything else rejects at declaration. | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L7-L19` | Carry forward as the InitialVolume admission rule. Do not carry forward the f64 CPU record. |
| 2 | Charging accepts only min(capacity − contents, offered); debit cannot exceed contents. | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L38-L52` | Carry forward "never above capacity, never below zero". Do not carry forward discarding the excess: liquid above capacity must leave as conserved overflow. |

## Acceptance outline

Point of truth: [element-001](../requirements.md#element-001) and [element acceptance](../requirements.md#accept-element).

- **Chrome recipe.** From the real palette place a Water tank on the bench with the move gizmo; set InitialVolume with the selected part's fill control (**proposed** UI: a waterline handle stepping 2⁻⁴ m³); snap a Tap (EL-003) to `WaterOutlet`, open it, and place a Catch basin (EL-004) under the tap mouth.
- **Positive.** Run: the waterline falls and the basin rises by the same volume; outflow weakens as head drops (full tank empties in about 24 s through a fully open standard mouth, derived from the proposed values).
- **Negative / control.** InitialVolume 0: Run moves no water and the basin stays empty. Cap (EL-012) on the outlet: the level never changes.
- **Boundaries.** InitialVolume at capacity admits; above capacity or negative rejects atomically. Inflow into a full tank spills from the notch onto the floor tray and stays in the ledger. Ledger sum stays equal to the initial volume within f32 rounding (**proposed** tolerance 2⁻²⁰ of the total: one order above f32 resolution, matching the velocity headroom in the [envelope](../../gpu-f32-physics.md#game-grade-envelope)).
- **Run/Reset.** Reset restores the initial volume, every ledger total and pose exactly; a second Run replays identically.
- **Save/Load.** Save, reload the page, Load: pose, InitialVolume and Water joins survive and the run repeats.
- **Integrations.** [sequence-task-384](../requirements.md#sequence-task-384): tank → tap/gutter → basin with visible capacity, overflow and drain loss.

## Open questions

1. ρw = 16 kg/m³ is one water-family constant, aligned with Batch G (EL-023–EL-036, EL-165–EL-172); the owner confirms or replaces it once for the whole family (S416 buoyancy row → S420).
2. Is Capacity fixed per kind, or resizable like the Wall (`engine/gpu/WorkshopWall.cs@a6c914e:L6-L10`)?
3. Are Water mouth joins typed connection edges (persisted like wires) or geometric adjacency recomputed at admission? No decision row owns this; propose adding one under S416.
4. Free-stream packet volume and per-source budget are unspecified — owner S418.
5. A new catalogue category "Water" versus the existing "Motion".
