# EL-052 · Water-tuned bottle — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents. Shared acoustic facts A1–A13 are in [EL-044](EL-044-tone-selective-sound-meter.md#shared-acoustic-family-facts); sustained emission S1 and the airflow inlet in [EL-045](EL-045-air-whistle.md); the water conventions (domain `Water`, density 16 kg/m³, volume step 2⁻⁴ m³, `OpenMouth` capture) follow Batch F, e.g. [EL-001](EL-001-finite-reservoir.md) and [EL-003](EL-003-tap.md).

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-052 · Water-tuned bottle |
| Type | Sound |
| Anchor | [requirements.md#element-052](../requirements.md#element-052); scope index [todo-352](../requirements.md#todo-352); [named-elements entry](../invest/named-elements.md#element-052); owner S539 |
| Related | Refines no CAT spec. A resonator ([EL-049](EL-049-acoustic-resonator.md)) whose band follows conserved water fill, excited by airflow like [EL-045](EL-045-air-whistle.md); filled from a [EL-003 Tap](EL-003-tap.md) over an [EL-001 reservoir](EL-001-finite-reservoir.md); heard by [EL-044](EL-044-tone-selective-sound-meter.md). Campaign: "water-tuned bottle follows chapter 7 water". |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static bottle: body box 0.6 × 1.0 × 0.6 m centred at (0, 0, 0) with interior 0.5 × 1.0 × 0.5 m (0.25 m³) and a neck box 0.25 × 0.3 × 0.25 m on top at (0, 0.65, 0) **proposed** (interior volume = 4 water steps of 2⁻⁴ m³, so each fill mark is one step). Collision uses the outer boxes; the interior is a liquid store, not a rigid cavity. |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`). Glass art is transparent for presentation only. |
| Constraints | none. |
| Typed ports | `OpenMouth` (Water) at the neck top (0, 0.8, 0) captures intersecting free-stream liquid, as the reservoir's open mouth ([EL-001](EL-001-finite-reservoir.md)) **proposed**. The air mouth (neck opening) is also the airflow-receiver point. No electrical or activation port. |
| Sensors and activation | Fill V (m³) committed per tick from conserved inflow; overflow above 0.25 m³ spills from the mouth. Airflow excitation as the [EL-045](EL-045-air-whistle.md) Airflow variant: on at committed mouth flow ≥ 4 m/s, off at ≤ 3.6 m/s, strength = min(1, flow/12). |
| Work and energy stores | Finite water store V (FiniteLedger, conserved; it only changes by inflow, spill or a declared drain). No acoustic store; sound only while excited (S1). |
| Parameters | `initial_fill`: integer steps 0–4 (× 2⁻⁴ m³), default 0 **proposed** (dry start; filling is the lesson). The tone is not a parameter: it is derived from the committed fill (variants). |
| Cosmetic curves and UI bindings | Waterline ← committed volume (component research binding "Waterline ← volume", [component research](../../component-research.md#sound)); four engraved fill marks on the glass, each with its band's one/two/three bar symbol; wavefronts per emitted pulse (S1). |
| Art | Translucent cyan `#66b8c9` glass with cream `#fff8e9` fill marks and navy `#293954` band symbols; blue water as in the water family; navy base ring. Geometry **proposed**. |
| Catalogue / inventory | Id `tuned_bottle`, Title "Singing bottle", Category Sound **proposed**. Description **proposed**: "Blow air across its neck to make it sing. The water level, not a switch, sets the note: dry, and each fill mark, sound different." |

**Variants** (outcome: "Changing fill changes supported tone; dry and filled cases remain distinct"). Mapping **proposed** (more water → smaller air cavity → higher pitch, as in the cited bottle-resonance experiment; one step per mark keeps the marks readable and conserved):

| Variant | Committed fill V | Air cavity | Tone when excited |
| --- | --- | --- | --- |
| Dry | 0 (< 1 step) | 0.25 m³ | Low |
| Mark 1 | 1 step (0.0625 m³) ≤ V < 2 steps | 0.1875 m³ | Mid |
| Mark 2 | 2 steps ≤ V < 3 steps | 0.125 m³ | High |
| Over-full | V ≥ 3 steps | ≤ 0.0625 m³ | silent (cavity too small) |

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S539): AcousticPropagation, AerodynamicDrag, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, RigidBodyDynamics, SignalPropagation, TopologyTransaction (+ StateTransaction).

**Exists now**
- Static boxes: `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`.

**Missing**
- Liquid store, free-stream capture and conserved transfer — water family, [S416](../invest/decisions.md#s416) advection (S418); Batch F specs.
- Open-airflow receiver — Story 12.2 (CAT-028 Fan); [S470](../invest/decisions.md#s470).
- AcousticPropagation with sustained emission and a state-derived tone — Stories 14.1–14.3 plus S1; [S528](../invest/decisions.md#s528).

**Dependencies.** Water source (EL-001 + EL-003 or a bucket), Fan or Bellows, and a tuned meter (EL-044) to prove the tone.

## 4. Sources and legacy

- Requirement row [element-052](../requirements.md#element-052): "Conserved fill volume alters a resonant cavity's tone"; outcome "Changing fill changes supported tone; dry and filled cases remain distinct". Scope index [todo-352](../requirements.md#todo-352): water-tuned bottles keep their individual contract; campaign: "water-tuned bottle follows chapter 7 water" ([campaign-element-coverage](../requirements.md#campaign-element-coverage)).
- Named entry [element-052](../invest/named-elements.md#element-052), owner S539; component research: "Water store + resonator with level-selected band | fill marks | Fill level selects a marked band; airflow across the mouth excites it | Waterline ← volume"; recipe "Fill to Sing: tap fills bottle to its mid-band; airflow excites it; tuned meter closes the tap and opens a hatch" ([component research](../../component-research.md#sound)).
- Legacy: no bottle, liquid store or tuned resonance exists at a6c914e; tone bands and presentation frequencies are A1 and A11 (`engine/AcousticAudio.cs@a6c914e:L14-L14`).
- **Files harvested:** acoustic files as listed in EL-044.

## 5. Acceptance outline

Acceptance authority: [element-052](../requirements.md#element-052), [acoustic profile](../invest/profiles.md#acoustic).

- **Construction (actual Chrome UI).** Reservoir + Tap above the bottle mouth; Fan aimed across the neck; three Tuned sound meters (Low, Mid, High) nearby, each supplied and wired to its own lamp.
- **Positive.** Dry bottle + fan: only the Low meter lights. Open the tap to fill one mark: only Mid lights. Two marks: only High.
- **Negative / control.** Fan off or blocked: silent at every fill. Over-full: silent. Water never appears or disappears (tank loss = bottle gain + spill). A dry and a Mark-2 bottle under the same fan light different meters (outcome).
- **Boundaries.** Fill exactly at each step boundary; overflow at 0.25 m³; airflow just above and below 4 m/s.
- **Run/Reset.** Reset restores `initial_fill`, the tank level and silence.
- **Save/Load.** `initial_fill` and pose round-trip.
- **Integrations.** Acoustic integration task [sequence-task-407](../requirements.md#sequence-task-407) ("water-tuned bottles each retain their later individual contract"); water integration task [sequence-task-384](../requirements.md#sequence-task-384) (tank → tap → collector with visible remaining capacity and overflow), which supplies the fill; combination [todo-378](../requirements.md#todo-378) ("The Mill's Song" sound-controlled sluice); interaction processes [IX-12 acoustic propagation](../requirements.md#interaction-12), [IX-08 fluid advection](../requirements.md#interaction-08) (conserved fill) and [IX-11 aerodynamic drag](../requirements.md#interaction-11) (airflow excitation). Campaign: the sound row of [campaign-element-coverage](../requirements.md#campaign-element-coverage) names "water-tuned bottle", first use 71–80 but "water-tuned bottle follows chapter 7 water" (reuse 81–100, 117–125, 136–150).

## 6. Open questions

1. **Fill-to-tone mapping.** The step mapping and the silent over-full state are proposals: owner decision.
2. **Excitation.** Airflow only (proposed) versus also a strike (bell-like): owner decision.
3. **Water units.** The bottle adopts Batch F's 16 kg/m³ and 2⁻⁴ m³ step proposals; it changes if those change.
