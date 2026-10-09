# RAD-17 · Water moderator tank — element readiness spec

Story 7.0 Batch O named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Requirement row: [radiation-17](../requirements.md#radiation-17) (P3 potential). Toy two-group neutron model in game units; no reactor, multiplication or real-world shielding guidance.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | RAD-17 · Water moderator tank |
| Type | Radiation + Water (conserved-water vessel acting as neutron moderator) |
| Anchor | [requirements.md#radiation-17](../requirements.md#radiation-17); [named-elements.md#radiation-17](../invest/named-elements.md#radiation-17) |
| Related identities | Source [RAD-16](RAD-16-neutron-source-module.md); detectors [EL-131](EL-131-fast-neutron-detector.md), [EL-132](EL-132-slow-neutron-detector.md); absorber [RAD-18](RAD-18-neutron-absorber-panel.md). Water supply and drain from [EL-001 Finite reservoir](EL-001-finite-reservoir.md), [EL-003 Tap](EL-003-tap.md), [EL-022 Water pump](../invest/named-elements.md#element-022), [EL-006 Drain](EL-006-drain.md). Transmission-gauge sibling [EL-130](EL-130-tank-level-transmission-gauge.md). No CAT spec. |
| Proof owner | S627 |
| Roadmap story | unscheduled; campaign 126, 128 (research slots 101, 103) |
| Status | not started |

## 2. Declaration

The requirement fixes: a conserved-water vessel converts a bounded fraction of traversing fast flux into the slow group with documented losses; water level affects the path; filling raises the slow response in the authored range; a dry and a drained tank fail; slowdown is not absorption. Values are proposals.

- **Bodies and shapes.** Static vessel: base 0.70 × 0.05 × 0.70 m and four wall boxes 0.02 m thick forming a 0.60 × 0.90 × 0.60 m open-top tank with a 0.50 × 0.80 × 0.50 m interior (proposed: Battery-scale vessel; the 0.50 m interior is the full wet path).
- **Mass and material.** Static, zero mass; static default contact material (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). Walls use `RadiationMaterialKind.ThinScreen` (EL-126 coefficients, including neutron transmission 0.98 per wall for both groups, counted on every path) (proposed: clear thin walls that barely affect any channel). Contents: `RadiationMaterialKind.Water` with photon μ 4 / 1.5 / 0.75 per metre, charged stopping factor 250, and neutron fractions per 0.50 m wet path Fast 0.30 transmit / 0.55 moderate / 0.15 absorb, Slow 0.85 transmit / 0.15 absorb (proposed: through both walls and a full tank, a fast beam that would read ≈ 15.8 becomes ≈ 4.6 fast + ≈ 8.3 slow, which switches the Slow detector on; outgoing ≤ incoming).
- **Path law.** Only the wet chord counts: L_wet is each sampled path's length below the committed waterline inside the interior ([shared rate law](RAD-08-radiation-rate-meter.md#2-declaration)); fractions scale as T(L) = T_ref^(L/0.50 m) with the removed share split between moderation and absorption in the reference ratio (proposed: monotonic in fill, exactly zero effect when dry).
- **Constraints.** none.
- **Typed ports.** `WaterInlet` (bottom inlet) and `WaterOutlet` (drain), in the shared proposed `WorkshopConnectionDomain.Water` (same typed water ports as EL-001/003/006/022; the current domain enum has only Activation and Electrical, `engine/gpu/WorkshopConnections.cs@a6c914e:L8-L12`).
- **Sensors and activation.** none of its own; the waterline is published state.
- **Work and energy stores.** Conserved water volume, capacity 0.20 m³ (interior volume), never created or destroyed by the tank (FiniteLedger/FluidAdvection).
- **Parameters.** Authored initial fill fraction f32 0–1, default 0 (dry) (proposed: lessons start dry so filling is the player's act).
- **Cosmetic curves and UI bindings.** Waterline ← committed volume (research row); a level scale engraved on the window.
- **Art.** Cream `#fff8e9` frame, cyan `#66b8c9` translucent walls, navy `#293954` base, gold `#f7cb52` level marks (proposed: water family look; no colour-only state).
- **Catalogue and inventory.** Id `moderator_tank`, title "Moderator tank", category `Radiation` (proposed: snake_case id; primary group Radiation per the [grouped menu](../requirements.md#palette-type-groups)).

**Variants.** None in the row; dry, filling, full and draining are states, each separately proven.

## 3. Engine capabilities

Families from the [map row](../general-engine-element-map.md) and [binding](../../coverage/engine/radiation-01.json):

| Family | Status | Basis or builder |
| --- | --- | --- |
| FiniteLedger | missing | unscheduled (S010-D); conserved water and neutron counts; the only current finite store is bumper contact work, `engine/gpu/ContactWorkDeclaration.cs@a6c914e:L5-L9` |
| FluidAdvection | missing | unscheduled (S418-D; liquid boundary decision S416/S470; no water epic yet) |
| GeometryQuery | exists now for contact only; missing for radiation paths | BVH `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L74-L204` and narrowphase `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L325-L620` (the coverage JSON's `engine/gpu/physics.wgsl` basis is not in the tree); wet-chord queries below a waterline: unscheduled (S606-D) |
| IonizingTransport | missing | unscheduled (S606-D/F); fast-to-slow transfer versus absorption |
| StateTransaction | exists now | `engine/gpu/WorkshopSimulation.cs@a6c914e:L44-L161`; water volume restores with Reset (unscheduled with this element) |
| TopologyTransaction | exists now for construction admission only | `engine/gpu/WorkshopSimulation.cs@a6c914e:L113-L151`; in-Run liquid volume and fluid links: unscheduled |

Also needed: the shared Water connection domain and ports (unscheduled with the water family).

**Dependencies.** A water supply and drain (water family, Epic not yet scheduled); RAD-16; EL-131/EL-132.

## 4. Sources and legacy

- [radiation-17](../requirements.md#radiation-17): "Filling the tank increases the slow-group response in the authored range; a dry tank and a drained tank fail that control. Slowdown is not absorption."
- [Named entry](../invest/named-elements.md#radiation-17), owner S627; map row (S416/S470, S605); binding `radiation-01.json`.
- [S605](../invest/decisions.md#s605) neutron row; research "Neutrons" law and slots 101 "Slow Water", 103 "A Rising Shield".
- **Legacy.** No moderator; legacy water code is outside the radiation scope. Hits are coverage snapshots only: older binding copy `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L2165-L2196`, identical to current, and aggregate relation lists — no element knowledge; do not carry forward.

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance outline

- **Construction (actual Chrome UI).** Neutron source and a supplied Slow detector 1 m apart with the tank between; a reservoir and tap wired to `WaterInlet`, a drain on `WaterOutlet`.
- **Positive.** Opening the tap raises the waterline past the beam; the slow reading rises to ≈ 8.3 and the gate releases.
- **Negative / controls.** Dry tank: slow 0 (fast ≈ 15.2 through the two walls). Drained after filling: slow back to 0. The fast reading falls from ≈ 15.2 (dry) to ≈ 4.6 (full) as slow rises, and fast + slow + absorbed (water and walls) equals the incoming count (slowdown is not absorption). Overfilling is impossible: inflow stops at capacity, water conserved.
- **Boundaries.** Waterline exactly at beam height (the lower receiver-sample paths are wet first, giving an intermediate reading); partial chord with a tilted beam; total water before and after equal.
- **Run/Reset.** Volume returns to the authored initial fill.
- **Save/Load.** Initial fill, pose and fluid links round-trip.
- **Integrations.** Moderator in the cross-element task [radiation-19 integration (sequence-task-427)](../requirements.md#sequence-task-427) (moderated flow drives Slow while Fast decreases); interaction processes [IX-15 Ionizing transport](../requirements.md#interaction-15) and [IX-08 Fluid advection](../requirements.md#interaction-08) (conserved fill and drain); fluid-port permutations in [connection permutations](../requirements.md#connection-permutations); family tasks [radiation-foundations](../requirements.md#radiation-foundations), [radiation-proof](../requirements.md#radiation-proof) and [radiation-campaign](../requirements.md#radiation-campaign); campaign first use in levels 121–130 ([campaign element coverage](../requirements.md#campaign-element-coverage); chapter 13 of the [campaign plan](../requirements.md#campaign-plan)), reuse 136–150.

## 6. Open questions

1. Proposed fractions and wet-path scaling need S606-D.
2. Whether the tank refills through its declared WaterInlet only or also by pouring — water-family owner decision.
3. Primary palette group Radiation or Water — owner decision.
