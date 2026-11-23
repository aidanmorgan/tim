# EL-170 · Raft — named-identity spec

**Current delivery (24 October 2026):** [EL-170 delivery slice](../invest/named-elements.md#element-170). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

**Repair (audit M-8, M-12, M-2).** A Basketball on the battens rolled off and heeled the raft. The cargo is a flat-based Weight standing across the battens on the centre line, which adds no heel and whose stability is computed below. Separated logs collide; the break measure is declared; H3 and H4 have constructions; the joint-intact observations are counted.

**Repair after the second audit (23 November 2026, `docs/planning/elements/readiness/audits/reaudit-group-2.md`, EL-170).** The battens spanned all three logs 5 mm above the side logs, so after a joint broke a side log rose onto a batten and carried the same load by contact: "separated members change stability" was not established. The audit's suggested layout (logs r 0.15 at x = ±0.40) is 1.10 m wide and does not fit its own rig's 1.0 m Lock chamber (`EL-027-canal-lock-chamber.md:24`). Redesigned under the standing delegation (decide by game intent; decision and calculation in Open questions item 6): three thinner, longer logs with 0.17 m gaps; the battens on the centre log only; a 0.75 kg Weight whose widest collider clears the side logs; a 4.0 m drop. Nothing on one member reaches over another, and no two loose members can hold the Weight dry.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-170 · Raft · Water |
| Anchors | [element-170](../requirements.md#element-170), co-anchored `sequence-task-723`; source record [campaign coverage](../requirements.md#campaign-element-coverage); ledger [element-170](../invest/named-elements.md#element-170), owner S463 |
| Disposition | To deliver as a single element. Not a member of "Floating bodies": it is three bodies held by joints that can break, so it needs a fixed joint and a load limit |
| Planned package | id `raft`, directory `elements/Raft/`, title "Raft" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID`, `ENGINE-JOINT-KINDS` and `ENGINE-FRACTURE` |
| Related | [EL-169](EL-169-cork-float.md), [EL-028](EL-028-buoyant-platform.md), [EL-029](EL-029-boat.md), [EL-004](EL-004-catch-basin.md), [EL-027](EL-027-canal-lock-chamber.md), Weight (delivered) |

## Declaration

Closest delivered shape: none joins several free bodies rigidly. The nearest is a part with several bodies on declared joints (the Powered gate's blade on its frame); the fixed joint is missing (G6).

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, and the fixed-joint declaration `ENGINE-JOINT-KINDS` adds.
- **Bodies (three dynamic logs).** Each a solid Cylinder, radius 0.10, length 0.90, axis Z, at x = −0.37, 0 and +0.37 (gaps of 0.17 m between log surfaces). Body 0, the centre log, is the package root; it is dynamic. Overall 0.94 (x) × 0.90 (z).
- **Battens on body 0 (two boxes).** 0.20 × 0.04 × 0.08 at (0, 0.12, ±0.18): they lie across the top of the centre log only (its top is at y = 0.10), their ends at x = ±0.10, 0.17 m short of the side logs' inner edges (x = ±0.27), and their inner edges 0.28 m apart.
- **Mass and inertia.** 0.10 kg per body. Side logs: centre of mass on the axis; ½·m·r² = 0.0005 kg·m² about Z; m·(3r² + L²)/12 = 0.10 × (0.03 + 0.81)/12 = 0.0070 kg·m² about X and Y. Body 0 includes its battens (0.02 kg of its 0.10): centre of mass declared at (0, 0.02 × 0.12 / 0.10, 0) = (0, 0.024, 0); moments as a log. Log volume π × 0.10² × 0.90 = 0.0283 m³ each; battens 2 × 0.20 × 0.04 × 0.08 = 0.0013 m³; logs and battens 0.0861 m³ in all. Gravity (0, −9.81, 0).
- **Material (f32).** Restitution 0.2, friction 0.6, rolling resistance 0.03, bounce threshold 0.1 m/s.
- **Joints (two fixed).** Centre log to each side log, anchored midway between them at (±0.185, 0, 0). The joined bodies collide with each other (declared), so separated logs and battens meet as solids; while joined nothing touches (0.17 m gaps; the battens end 0.17 m short of the side logs). Each joint is load-limited: it breaks, irreversibly for the Run and restored by Reset, when the peak force it carries in any substep exceeds **20 N**.
- **Ports, stores, parameters.** None. No store, so no thermal node.
- **Presentation.** `Visual/Log0`, `Visual/Log1`, `Visual/Log2` each follow their body's pose. `Visual/Log1/Lashing` and `Visual/Log2/Lashing` hidden once that log's joint has broken (driver missing: a committed joint-intact observation, `ENGINE-FRACTURE`).
- **Palette.** `ElementCategory.Motion`; order 153 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour ochre (194/255, 143/255, 82/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Log0`, `Log1`, `Log2` | Cylinders radius 0.10, length 0.90, ochre `#c28f52` with cream `#fff8e9` end grain printed flat | solid; the three Cylinder colliders | each body's pose |
| `Visual/Log0/Battens` | Two bars 0.20 × 0.04 × 0.08 across the centre log, navy `#293954` | solid; the two box colliders on body 0 | body 0 pose |
| `Visual/Log1/Lashing`, `Visual/Log2/Lashing` | Gold `#f7cb52` bands printed flat round each side log at mid-length, on the face toward the centre log | decorative: printed flat; hidden by a binding | presentation binding |

Nothing is drawn across the 0.17 m gaps: the fixed joints have no visible member, so nothing drawn can be expected to carry a load after a break (visible = solid holds: every drawn solid has its collider).

Icon: three logs side by side with gaps, two short navy bars across the middle one and gold bands on the outer two. It differs from the Floating deck (one slab) by its three round members.

### Behaviour and numbers

Each log floats by L12 a. The joints make them share one waterline; that sharing is the structure. Water is 16 kg/m³; the three logs lift at most 16 × 0.0848 = 1.357 kg, logs and battens 16 × 0.0861 = 1.378 kg. A submerged fraction f of a log of radius r fixes the central angle θ of the submerged segment by θ − sin θ = 2πf; the waterline is −r·cos(θ/2) above the axis, the waterplane width 2r·sin(θ/2), and the segment's centroid 4r·sin³(θ/2) ÷ (3(θ − sin θ)) below the axis.

- **Unloaded.** 0.30 / 1.357 = **22%** of the log volume is under water: θ = 2.198 rad, waterline 0.045 m below the axes (draught 0.055 m), waterplane 0.178 m wide per log.
- **The cargo.** A delivered Weight sinks between about 0.5 and 4.9 kg and floats outside that range (linear size law, `elements/Weight/WeightElementDeclaration.cs:59-62`); a row that immerses one states its mass. A 0.75 kg Weight is a flat-based cylinder of radius 0.04808 × 0.75 + 0.15933 = 0.1954 m and height 2 × (0.03818 × 0.75 + 0.12653) = 0.3103 m: 0.0372 m³, 20.2 kg/m³, so alone it sinks, 0.155 kg heavier than the water it displaces. Its widest colliders are its two band rings, outer radius 0.3633 × (0.136734 × 0.75 + 0.45307) = 0.2019 m (`elements/Weight/WeightElementDeclaration.cs:63-72`, `weight.tscn:31-32`); its base flange is 0.2000 m. Standing centred, its base rests on both battens (contact over x = ±0.10, z = 0.14 to 0.195 each side); its centre of mass is 0.14 + 0.155 = 0.295 m above the log axes. Its widest collider clears the side logs' inner edges (0.27 m from the centre line) by **0.068 m**.
- **Shared displacement.** With the Weight: 1.05 / 1.357 = **77%** under (θ = 4.064 rad), the waterline 0.045 m above the axes, so the Weight's base is 0.14 − 0.045 = **0.095 m above the surface** and the battens stay dry.
- **Stability with the cargo.** Each log's waterplane is 0.90 × 0.179 m. KB = −0.020 m (segment centroid); KG = (0.10 × 0.024 + 0.75 × 0.295) / 1.05 = 0.213 m; V = 1.05 / 16 = 0.0656 m³. Across the logs: I = 3 × 0.90 × 0.179³ / 12 + 2 × 0.90 × 0.179 × 0.37² = 0.0013 + 0.0441 = 0.0454 m⁴, BM = 0.0454 / 0.0656 = 0.692 m, **GM = 0.459 m**. Along the logs: I = 3 × 0.179 × 0.90³ / 12 = 0.0326 m⁴, BM = 0.497 m, **GM = 0.265 m**. The Weight on the battens slides at atan(√(0.6 × 0.3)) = 23° (geometric-mean friction, `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:119`) and tips across the battens' ends at atan(0.10 / 0.155) = 33°. Centred, it adds no heel; no row heels the joined raft past 5°, so large-angle values are not needed.
- **What the joints carry.** Each log's buoyancy is 1.05 × 9.81 / 3 = 3.43 N; a side log weighs 0.98 N, so each joint passes 2.45 N to the centre: an eighth of 20 N.
- **Overload.** Logs and battens carry at most 1.378 − 0.30 = **1.08 kg** clear of the water. A Bowling ball (4 kg, radius 0.28, its own lift fully under 16 × 0.092 = 1.47 kg) needs 4 − 1.47 = 2.53 kg more than it displaces: the raft sinks whole. Each side log, fully under, pulls up with 16 × 0.0283 × 9.81 − 0.98 = 3.46 N on its joint; while the raft accelerates downward at most (4.3 × 9.81 − 2.94) / 4.3 = 9.1 m/s² (the unloaded raft's 2.94 N of buoyancy at the start), the joint carries at most 0.10 × 9.1 + 4.44 − 0.98 = 4.37 N. Both are under 20 N, so the joints hold until something strikes the raft. The battens' tops start 0.14 + 0.045 = 0.185 m above the surface and go under within about 0.25 s; the raft reaches the chamber floor (about 0.76 m below its keel) later, and that landing may break a joint, so H3 reads the tick the battens go under. The ball rests on the battens' inner top edges, its centre √(0.28² − 0.14²) = 0.24 m above them; along the battens it is in neutral balance (level rails exert no force along themselves), it starts centred and at rest, and the symmetric raft sinks without heel, so nothing moves it along them before the battens are under.
- **Separated members.** The same 0.75 kg Weight released with its base 4.0 m above the battens lands at √(2 × 9.81 × 4.0) = 8.86 m/s. Taken up by the raft (pair restitution 0.08 × 0.2 = 0.016, nearly plastic), the four bodies move at 8.86 × 0.75 / 1.05 = 6.33 m/s, so each side log receives 0.10 × 6.33 = 0.633 N·s through its joint during the impact: 304 N if the impact is resolved in one substep (1/480 s), 30 N if it is spread over ten. Either way the peak exceeds 20 N, and each joint carries its own log's share, so **both** joints break whatever order the solver takes them in. Then:
  - The centre member (0.10 kg, 0.0296 m³ with its battens) lifts at most 16 × 0.0296 − 0.10 = 0.37 kg net, under the 0.75 kg Weight. Upright, the pair could float only with the Weight 0.197 m under water (0.85 kg needs 0.0531 m³: 0.0296 from the member, 0.0236 from the Weight's 0.120 m² section). That pair has KB = 0.109 m, BM = π × 0.1954⁴ / 4 / 0.0531 = 0.022 m and KG = (0.10 × 0.024 + 0.75 × 0.295) / 0.85 = 0.263 m: **GM = −0.13 m**, so it capsizes, and the Weight slides off at 23° and sinks.
  - Nothing on the centre member reaches over a side log (battens 0.17 m short, Weight 0.068 m clear), so a side log carries nothing by contact; it floats at its own 22% draught.
  - No two loose members can hold the Weight dry: a side log lifts at most 0.452 − 0.10 = 0.35 kg net, the centre member 0.37 kg, any two at most 0.72 kg against 0.75 kg. Even upright across two members, the Weight must stand at least (0.75 − 0.72) / 16 / 0.120 = 0.016 m deep.
  - So wherever the Weight comes to rest (on the chamber floor, on its side across the logs, leaning on a loose log, or upright across two), it no longer stands upright with its base above the water. Stability has changed because the structure has.
- **No water, no support.** On a dry floor it is three joined logs resting on the floor.
- **Displacement (L4).** Unloaded it displaces 0.30 / 16 = 0.019 m³: 0.019 m of level in a Lock chamber (1.0 m²).
- **Bobbing.** Unloaded waterplane 3 × 0.90 × 0.178 = 0.481 m²; stiffness 156.96 × 0.481 = 75.5 N/m; frequency √(75.5 / 0.30) = 15.9 rad/s (period 0.40 s); immersion damping 1.0 × 0.22 = 0.22 s⁻¹, a damping ratio of 0.22 / (2 × 15.9) = 0.007 at the starting value of c, which `ENGINE-LIQUID` sets for a ratio near 0.5 (L12 a). At the starting value a raft placed with its undersides at the surface (a 0.055 m offset) still bobs 0.055 × e^(−0.11 × 20) = 0.006 m at 20 s. Loaded: stiffness 156.96 × 3 × 0.90 × 0.179 = 75.9 N/m, √(75.9 / 1.05) = 8.5 rad/s (period 0.74 s).
- **Fit.** 0.94 × 0.90 fits the Lock chamber (1.0 × 1.0: 0.03 m and 0.05 m a side) and the Catch basin (1.25 × 1.0).

## Engine capabilities

Map families (navigation only, D14): Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W21 dynamic bodies with declared mass and inertia | present | logs |
| W22 authored state | present | pose |
| A package whose root body is dynamic and carries further bodies on joints | present, untested | the footprint rule counts a dynamic root plus one body per joint (`engine/gpu/WorkshopPhysicsCompiler.cs:245-257`); no delivered package has this form. `ENGINE-JOINT-KINDS` states it and adds the first test |
| W10 immersion | missing | `ENGINE-LIQUID` |
| G6 fixed joint between bodies of one package, with a declared bodies-collide flag | missing | `ENGINE-JOINT-KINDS` |
| G6 load limit on a joint with a declared break measure, irreversible break restored by Reset, and a committed joint-intact observation readable by presentation | missing | [`ENGINE-FRACTURE`](../../../_bmad-output/planning-artifacts/epics.md#story-engine-fracture-load-limited-connections-and-irreversible-break) |

Counts: 2 present, 1 untested, 0 narrower, 3 missing. Three owners, one per capability: immersion, the fixed joint, the load limit.

### Engine conformance (22 November 2026)

1. **Declaration data only.** Three dynamic bodies, five colliders, two fixed joints with a break load, two joint-intact observations, two bindings (the lashing drivers; the logs' art follows their body poses).
2. **Generic laws.** Flotation is L12 a per body. "Shared displacement and structure" is two fixed joints. "Separated members" is a declared load limit exceeded under the general contact law. No raft rule exists.
3. **Three layers.** *Colliders:* three Cylinders, two boxes. *Visuals:* each log follows its body. *Animation:* the lashing art hides on a committed break and writes nothing back.
4. **One-way flow and typing.** Values are f32; a joint's state is a typed intact or broken.
5. **Flat-state counts.** Bodies 3 (dynamic); colliders 5 (3 Cylinders, 2 boxes); joints 2 fixed; break limits 2; joint-intact observations 2; drives 0; sensors 0; liquid nodes 0; thermal nodes 0; ports 0; bindings 2 drivers; parameters 0.
6. **Performance fitness.** Five colliders against the Floating deck's nine. The two joints keep the three bodies in one island until they break. Whether that island may sleep is `ENGINE-ISLANDS-SLEEP`'s; nothing here assumes it.
7. **Tests are scenarios.** Rows H1 to H7.

## Sources and legacy

- Requirement: "Joined buoyant members support cargo through shared displacement and structure"; outcome "Overload or separated members change stability." No variants.
- Campaign coverage row as EL-169.
- **Legacy.** None at `a6c914e` (searched for "raft", "float", "buoyan", "water"). **Files harvested:** none.

## Acceptance outline

Authority: [element-170](../requirements.md#element-170). Tolerances L17. Planned class `RaftTests`. The vessel is a Lock chamber with `initial_volume` 0.80 m³ (depth 0.80 m) and a closed Sluice gate (no Weight, [EL-021](EL-021-sluice-gate.md)) on each of its two sockets, so that neither socket is a free end, unless stated; the raft floats centred in it.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| H1 | Given the raft placed with its logs' undersides at the surface, when Run advances 20 s, then all three log axes are within 0.02 m of one height and the submerged fraction averaged over the last 2 s (five bobbing periods of 0.40 s) is 22% within 5 points | `RaftTests.TheJoinedLogsShareOneWaterline` | H5 | — |
| H2 | Given the raft placed at its loaded waterline (0.045 m above the log axes) with a 0.75 kg Weight standing centred across the battens, all at rest, when Run advances 20 s, then both joints are intact, the heel is under 5°, and averaged over the last 2 s the Weight's base is above the surface (hand 0.095 m) and the submerged fraction is 77% within 6 points | `RaftTests.TheRaftCarriesWhatOneLogCannot` | H4 | — |
| H3 | Given a Bowling ball authored at rest centred on the battens of the unloaded raft, when Run advances 10 s, then the battens' tops go below the surface at a tick when both joints are intact (hand: within about 0.25 s) | `RaftTests.AnOverloadSinksTheWholeRaft` | H2 | — |
| H4 | Given the raft unloaded and at rest on the water, and the same 0.75 kg Weight upright and centred with its base 4.0 m above the battens, released at Run start, then by 2 s both joints are broken, and at 10 s, averaged over the last 2 s, the Weight's base centre is below the surface or its axis is more than 30° from vertical | `RaftTests.SeparatedMembersNoLongerCarryTheLoad` | H2: the Weight set at rest is carried upright with both joints intact, its base 0.095 m above the surface | — |
| H5 | Given an empty chamber, then the raft rests on the floor | `RaftTests.NoWaterNoSupport` | H1 | — |
| H6 | Given H4, when Reset is pressed, then both joints are intact, both lashings are drawn, the logs are at the authored pose, and a second Run repeats H4 | `RaftTests.ResetRestoresTheJoints` | — | — |
| H7 | Given a run, then save and load keep the pose | covered by W22 | — | — |

Generic cover: W22. No port, no parameter. H4's contrast with H2 is the requirement's "separated members change stability": the same load on the same raft, carried upright and dry while joined, and not after the joints break. H4 can fail: if a broken joint still passed load (no break, or a contact path after the break), the Weight would stay upright with its base 0.095 m above the water as in H2. Chrome recipe (pending): float the raft in a filled chamber, stand a Weight on it, then drop a Weight on it from height.

## Open questions

All resolved; none needs the owner.

1. How "separated members" is reached: by breaking a load-limited joint in the Run. A lashing parameter (lashed or loose) was not used: it would need parts present by parameter, a capability with no consumer here.
2. Break load 20 N on the peak force of a substep: a starting value (D1). It is eight times the static joint load under the 0.75 kg Weight (2.45 N), 4.6 times a sinking side log's greatest pull (4.37 N), and a dropped Weight exceeds it however the impact is spread over ten substeps (30 N).
3. Player-built rafts from loose logs: not this part. `ENGINE-ASSEMBLY-JOINTS` owns player-made connections.
4. Number of logs: three, the fewest that show a middle member carrying load to its neighbours.
5. Cargo: a flat-based Weight across the battens, not a ball (decision under the standing delegation, audit M-8): a ball on two parallel battens rolls along them.
6. **Layout (decided 23 November 2026 under the standing delegation, second audit EL-170).** The rule: after a break no member may rest on another, so nothing on the centre member may reach over a side log and the cargo's widest collider must clear the side logs. With the 0.75 kg Weight (0.2019 m) and a 0.068 m clearance the side logs' inner edges sit at 0.27 m; inside the Lock chamber's 1.0 m (0.03 m a side) their outer edges sit at 0.47 m, so the logs are 0.20 m across (r 0.10). Length 0.90 m (0.05 m a side) and mass 0.10 kg per log give the joined raft 77% immersion with the Weight and keep any two loose members (0.72 kg) short of the Weight (0.75 kg). The drop rises from 3.0 to 4.0 m so the lighter logs still load each joint past 20 N with the impact spread over ten substeps. Alternatives considered: the audit's r 0.15 logs at ±0.40 m (1.10 m: does not fit the Lock chamber); the same in the 1.25 m Catch basin (0.40 m deep: the separated centre log and Weight ground with the Weight's base within 0.01 m of the surface, so H4 could not tell the cases apart).

## Integration

- **Works with.** Weight and delivered balls; EL-004, EL-027 (not delivered).
- **Campaign.** Chapter 7, levels 61–70; reuse 81–100, 126–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element, last of the floating parts.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (immersion candidates, L18), `ENGINE-ISLANDS-SLEEP` (a change of level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES`; then `ENGINE-GAS-NETWORK`; then `ENGINE-LIQUID`; `ENGINE-JOINT-KINDS` (fixed joint, dynamic root, bodies-collide flag; also the rest coordinate of the rig's closed Sluice gates, EL-021); `ENGINE-FRACTURE` (break measure, intact observation). `ENGINE-FRACTURE`'s package-declared limits wait only for `ENGINE-JOINT-KINDS` and `ENGINE-CONTACT-OBSERVATIONS` (engine-stories-index C9, from `readiness/batch-g.md`); `ENGINE-ASSEMBLY-JOINTS` is not a prerequisite of this part. Then EL-004, EL-016, EL-021, EL-027; S7.
- **Files.** Adds `elements/Raft/Raft.csproj`, `RaftElementDeclaration.cs`, `raft.tscn`, `icon.svg`, `CuriousContraptions.tests/RaftTests.cs`; edits `CuriousContraptions.slnx` and this spec. Registration, bundle, palette and coverage membership are derived (`spec-registration-derivation.md`); lists S3 owns need an edit until S3 lands.
- **Review tier.** Data-only if the probe finds every missing and untested row delivered.
- **Disqualifiers the slice's probe must re-check.** A package must have a static root body; a fixed joint cannot carry a break load or a declared measure; joined bodies cannot be declared to collide; Reset does not restore a broken joint; a first simulated H4 in which a loose member rests on another after the break (then the gaps or the cargo mass are re-chosen under D1 before the slice plans).

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: the ledger entry links only the campaign coverage row; `sequence-task-723` is co-anchored with `element-170`, not with a todo; no `s463` anchor.
