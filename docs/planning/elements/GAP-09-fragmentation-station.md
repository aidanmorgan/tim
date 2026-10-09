# GAP-09 · Fragmentation station — named-identity readiness spec

Story 7.0 readiness spec ([spec](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md)). Citations are `path@a6c914e:Lstart-Lend`. Values marked **proposed** have no legacy or requirement source; each carries a one-line justification and the owner may revise it.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | GAP-09 · Fragmentation station · Mechanical (P3 potential) |
| Anchor | [requirements.md#gap-09](../requirements.md#gap-09); [named-elements entry](../invest/named-elements.md#gap-09) |
| Related identities | Fragments inherit coating/dye state from [EL-118](EL-118-coating-station.md)/[EL-119](EL-119-dye-station.md); breaking shares the fracture family with [EL-159](EL-159-tension-limited-connector.md)–[EL-161](EL-161-bending-limited-connector.md); "broken" goal predicate ([requirements](../requirements.md#sequence-task-361)); fragile cargo EL-084. No CAT refines it. |
| Roadmap story | Unscheduled. Campaign: introduction 95 "A Smashing Success", practice 96, reuse 99, 141, 150 ([gap-09](../requirements.md#gap-09)). |
| Status | Not started. Disposition potential/conditional ([element map](../general-engine-element-map.md)). |

## 2. Declaration

Variants: "A supplied press or impact station" ([gap-09](../requirements.md#gap-09)) — `Press` and `Impact`, specified separately. The row also requires "the fracture threshold and each supported material"; each material is a typed value with its own thresholds.

- **Bodies and shapes (shared).** A static arch over a navy catch tray, a moving head, and an input cargo block.
  - **Proposed** arch 1.4 m wide × 1.8 m tall × 1.0 m deep; tray 1.2 × 0.1 × 1.0 m — clears a 0.5 m block with room for the head stroke.
  - **Proposed** input cargo: a dynamic `Breakable block`, a 0.5 m cube — the fragments (0.25 m cubes) then pass a 0.4 m route that the intact block cannot, the row's "can pass a smaller route".
  - **Proposed** fragments: exactly 8 cubes of 0.25 m, each 1/8 of the parent mass, placed in the parent's octants with the parent's velocity — "a bounded, deterministic set"; 8 + the head fits the 16 dynamic-body table (`engine/gpu/PhysicsBodyReadSet.cs@a6c914e:L30-L30`).
  - **Proposed** head and block restitution 0.1 each, so the product rule (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L642-L642`) gives e = 0.01 and an impact keeps almost all its normal energy in the block.
- **Head and guide declaration.** **Proposed** shared dynamic head box 0.60 × 0.10 × 0.60 m, mass 3 kg in both variants: wider than the 0.5 m cargo block and small enough for the arch. The tray top is 0.10 m above the base datum; the intact block top is 0.60 m. A vertical prismatic joint to the static arch fixes head orientation and horizontal position, with its underside travelling from 0.10 to 1.60 m; the uppermost head top is 1.70 m, below the arch crossbar's proposed 1.75 m underside. Contact with cargo stops actual motion before the lower joint stop.
- **Variant constraints.** **Proposed Impact:** a releasable lock on that slider holds the head underside at 0.60 m + drop_height; ActivationIn releases it once, leaving gravity and contact to move the head. **Proposed Press:** the same slider has a downward drive capped at 200 N, drawing finite supply work; an unsupplied static holding lock prevents the head falling, while supplied release admits the driven stroke. Reset restores the initial lock and head pose. These are shared joint/lock/drive declarations, not a station-specific update loop.
- **Fracture rule (shared, two measures declared by the block's material).**
  - **Impact energy.** Delivered work per contact event W = ½·J·(v_before + v_after), with the normal relative velocity signed positive toward the block (a rebound has v_after < 0). This is the normal kinetic energy lost in the impact, ½·μ·v²·(1 − e²). The block rests on the static tray, so for a blow from above the reduced mass μ is the striker's mass. The block breaks when W ≥ its fracture energy.
  - **Crush force.** **Proposed** the block also breaks when the tick-averaged normal contact force on it stays at or above its crush strength on every tick for 0.5 s — a sustained press load; a one-tick impact spike followed by a resting head never qualifies.
  - The block is replaced by its fragments atomically at the tick boundary (TopologyTransaction). Below both thresholds it stays intact: "Insufficient impact leaves cargo intact".
- **Materials.** Closed enum `FragmentMaterial { Ceramic, Wood }`.
  - **Proposed** `Ceramic`: block mass 1.6 kg, fracture energy 9 J, crush strength 120 N — a Basketball (e = 0.55 × 0.1) must fall about 0.92 m onto it to break it (9 J at μ = 1 kg); a resting Bowling ball (39.2 N) or resting head (29.4 N) never crushes it.
  - **Proposed** `Wood`: block mass 1.2 kg, fracture energy 15 J, crush strength 180 N — needs a heavier blow or press, so material choice matters.
  - Fragments inherit material, coating and dye state: "fragments conserve mass and inherit explicit material state".
- **Variant `Impact`.** A 3 kg dynamic hammer head held up by a latch, released by `ActivationIn`. With e = 0.01 and μ = 3 kg, W ≈ 3 × 9.81 × h = 29.4·h J for a drop height h:

  | Drop height | W | Ceramic (9 J) | Wood (15 J) |
  | --- | --- | --- | --- |
  | 0.2 m | 5.9 J | intact | intact |
  | 0.4 m | 11.8 J | breaks | intact |
  | 0.6 m | 17.7 J | breaks | breaks |

- **Variant `Press`.** **Proposed** a linear drive pushing the head down with a 200 N maximum force — above both crush strengths, so a supplied press held on the block for 0.5 s breaks either material; electrical `PowerIn` socket (`engine/gpu/WorkshopConnections.cs@a6c914e:L8-L11`). Unsupplied: the head does not move; no work, no fracture.
- **Typed ports.** `PowerIn` (Press) or `ActivationIn` (Impact).
- **Sensors and activation.** Per-block contact work and crush accumulators; emits a typed `Broken` occurrence for goals.
- **Work and energy stores.** Press: finite supply work. Impact: gravitational potential of the raised head only; the station never adds work.
- **Parameters.**

  | Name | Type | Range | Default | Unit | Source |
  | --- | --- | --- | --- | --- | --- |
  | `variant` | enum `FragmentationDrive { Press, Impact }` | closed set | `Impact` | — | variants from [gap-09](../requirements.md#gap-09) |
  | `material` (on the block) | enum `FragmentMaterial { Ceramic, Wood }` | closed set | `Ceramic` | — | **proposed** (above) |
  | `drop_height` (Impact) | f32 | 0.2–1.0 | 0.6 | m | **proposed** — 5.9 J to 29.4 J spans both thresholds inside the arch |

- **Cosmetic curves and UI bindings.** Head motion follows the committed pose; calm bounded separation, no sparks or gritty debris ([gap-09 visual style](../requirements.md#gap-09)).
- **Art.** Rounded cream arch `#fff8e9`, cyan moving head `#66b8c9`, gold impact face `#f7cb52`, navy catch tray `#293954` (`DESIGN.md@a6c914e:L147-L154`, `DESIGN.md@a6c914e:L172-L172`).
- **Catalogue and inventory.** **Proposed** ids `fragmentation_station` ("Smasher", category Materials) and `breakable_block` ("Breakable block", category Motion); appended last in the Free palette (`engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`).

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md), [binding](../../coverage/engine/gap-01.json)): EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, StructuralFracture, TopologyTransaction.

- **Exists now.** Dynamic boxes with mass properties (`engine/gpu/RigidMassProperties.cs@a6c914e:L26-L38`); a per-contact work reservoir pattern (`engine/gpu/ContactWorkDeclaration.cs@a6c914e:L10-L38`); per-contact pre-solve approach speed is already tracked for restitution (`CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L651-L656`).
- **Missing.**
  - StructuralFracture and body replacement mid-Run: S543 fracture row, named next S563 ("Just above the break condition the object splits into bounded pieces; just below it holds; unsupported material refused", [decisions](../invest/decisions.md#s543)); owner S641.
  - Per-contact impulse and post-solve velocity read-back for W: owner S641.
  - Slider/latch for the head: Story 6.4 prismatic slider; latch release via activation (Epic 9).
  - Electrical supply for `Press`: Story 8.1.
- **Dependencies.** Battery (CAT-005) for Press; Switch (CAT-063) for Impact release; a narrow route (Walls) for the size control.

## 4. Sources and legacy

- **Requirements.** "Insufficient impact leaves cargo intact; adequate supplied work creates finite fragments that can pass a smaller route. Blocked/unsupplied controls fail appropriately; fragments cannot create extra feed or lose inherited state on save/Reset" ([gap-09](../requirements.md#gap-09)).
- **Audit.** "Breakable geometry, bounded fragment budget and conserved mass. Intact control fails a narrow delivery route; fragments remain physical and cannot multiply indefinitely" (`docs/physics-puzzle-gap-audit.md@a6c914e:L88-L88`).
- **Legacy.** None: no fracture or fragment code, test or level exists. The impact-scheduled joint/collider change atomicity (`CuriousContraptions.tests/PhysicsImpactJointTests.cs@a6c914e:L35-L54`) is the nearest topology precedent; carry forward atomicity only.

## 5. Acceptance outline

Point of truth: [gap-09](../requirements.md#gap-09).

- **Chrome recipe.** Place a Smasher (Impact) over a Breakable block; wire a Switch to `ActivationIn`; place two Walls forming a 0.4 m route beyond the tray.
- **Positive (Impact).** Ceramic block, drop 0.6 m (W = 17.7 J ≥ 9 J): the block breaks into 8 fragments; they pass the 0.4 m route; total fragment mass equals the block's 1.6 kg.
- **Positive (Press).** Supplied press on a Wood block: 200 N held for 0.5 s ≥ 180 N crush strength, the block breaks.
- **Negative or control.** Ceramic block, drop 0.2 m (W = 5.9 J < 9 J): intact, cannot pass the route. Wood block, drop 0.4 m (11.8 J < 15 J): intact, while a Ceramic block at 0.4 m breaks. Switch not struck: head stays latched. Press unsupplied: nothing moves. Press blocked by a Wall between head and block: the block feels no load and stays intact.
- **Boundaries.** Exactly 8 fragments, never more; fragments never re-fracture into more pieces (proposed one level only).
- **Run/Reset.** Reset restores the intact block, raised head and latch.
- **Save/Load.** Variant, material and drop height survive save; fragments are runtime-only and inherited state survives Reset as the original block's state.

- **Integrations.** Verify Switch release and Battery supply independently; coat or dye the cargo through EL-118/EL-119 before impact and check every fragment inherits that material state while passing the narrow Wall route.

## 6. Open questions

1. Fracture measures: impact energy lost plus sustained crush force (proposed) versus a single measure. Unspecified — owner decision.
2. Materials and thresholds (proposed Ceramic 9 J / 120 N, Wood 15 J / 180 N).
3. Whether fragments can fracture again (proposed no).
4. Whether the breakable block is its own catalogue part or a material mode of existing cargo.
