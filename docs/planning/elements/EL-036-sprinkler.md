# EL-036 · Sprinkler — named-identity spec

**Current delivery (24 October 2026):** [EL-036 delivery slice](../invest/named-elements.md#element-036). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

Brought to build-ready detail on 22 November 2026 against checkout `8d1d68aa` and repaired the same day against the independent audit `docs/planning/elements/readiness/audits/elements-batch-g.md` and the owner decisions of 22 November 2026. Liquid conventions L1 to L18 and probe rows W1 to W24 are in [EL-001](EL-001-finite-reservoir.md), which has adopted Batch G's LG1 to LG8 into L3 to L15; LG numbers are kept as short names (LG1 = L5 and L6; LG2 = L3 and L7; LG3, LG4, LG5 = L4, L11, L12; LG6 = L15; LG7 = L12; LG8 = L13). The further additions adopted by coordinator decision 21 (nesting and envelope shielding, buoyancy reaction on a carrying body, contents at the liquid centroid, overlapping shapes counted once, porous and sealed interiors, a mouth below the floor datum, the relief mouth, the crest rule with granted work head, no crest lift limit) are now written into EL-001's rows (its "Batch G's additions" paragraph maps A1 to A9 to L3, L4, L5, L7, L8, L9 and L12; the long wording stays in [readiness/batch-g.md](readiness/batch-g.md) section 2) and are cited here as **L-additions A1 to A9**. Probe rows G1 to G8 are in section 3 of that file. All are cited by number and not restated. Box sizes are full extents in metres.

## Identity

| Field | Value |
| --- | --- |
| Identity | EL-036 · Sprinkler · Water |
| Anchors | [element-036](../requirements.md#element-036), co-anchored `sequence-task-589`; source record [todo-340](../requirements.md#todo-340); integration [sequence-task-531](../requirements.md#sequence-task-531); ledger [element-036](../invest/named-elements.md#element-036), owner S457 |
| Disposition | To deliver as a single element. Not a duplicate of the Water nozzle ([EL-013](EL-013-water-nozzle.md)): it has four outlets instead of one, so it divides its supply into a footprint |
| Planned package | id `sprinkler`, directory `elements/Sprinkler/`, title "Sprinkler" |
| Status | Not started. Blocked on the engine programme (owner decision 3a), then `ENGINE-GAS-NETWORK`, `ENGINE-LIQUID` and `ENGINE-THERMAL` (Delivery) |
| Related | [EL-013](EL-013-water-nozzle.md), [EL-172](EL-172-rain-collector.md), [EL-002](EL-002-header-tank.md), [EL-022](EL-022-water-pump.md), [EL-034](EL-034-sponge.md), [EL-171](EL-171-squeeze-pad.md) |

## Declaration

Closest planned shape: the Water nozzle, a fitting with a fixed exit seat and a declared outlet that is not a mouth.

- **Interfaces.** `ISceneGeometryElement`, `IMechanicalElement`, the liquid fitting and seat declaration of `ENGINE-LIQUID` (W4, W7).
- **Body.** One static body, mass 0. Origin at the box centre.
- **Collider (one).** Box 0.24 × 0.30 × 0.24. Family vessel material (EL-001: restitution 0.12, friction 0.3, rolling resistance 0).
- **Liquid node.** One fitting, zero volume, with four fixed seats, one behind each outlet: each of area π × 0.02² = 0.001257 m², K_s = 0.6 × 0.001257 × 4.42945 = 0.003340. Four in parallel give 0.01336, the Water nozzle's exit exactly.
- **Mouth (one socket).** `Start` at (0, −0.15, 0), normal −Y, pipe bore, Intake, capture 0.20 m and 20° (L5): it stands on the upturned end of a pipe.
- **Outlets (four, not joinable).** Four declared free ends (L8) at (±0.12, 0.10, 0) and (0, 0.10, ±0.12), each aimed outward and 30° above the horizontal: normals (±0.866, 0.5, 0) and (0, 0.5, ±0.866). They are 0.25 m above `Start`. Declared outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; the four seats behind them are `ENGINE-LIQUID`.
- **No store, so no thermal node.** Owner decision 20 attaches a thermal node to every liquid store; the sprinkler is a zero-volume fitting. No row reads temperature.
- **Parameters.** None. Aim is the part's pose (D13).
- **Presentation.** None driven; the four streams are drawn by the shared liquid presentation (W8).
- **Palette.** `ElementCategory.Structure`; palette order 545, after the Water nozzle's 527 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)); colour gold (247/255, 203/255, 82/255).

### Art and solidity

| Node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Head` | Box 0.24 × 0.30 × 0.24 with chamfered top edges inside the box, gold `#f7cb52` | solid; the box collider | body pose (static) |
| `Visual/Jets` | Four navy `#293954` discs, radius 0.02, one on each side face, flush | decorative: printed flat | nothing |
| `Visual/Collar` | Navy band round the base, flush | decorative: printed flat | nothing |
| `Loc_Port_mouth_start` | Socket locator, bore 0.10 | none | nothing |
| Four outlet locators | Points and normals | none | nothing |

Icon: a gold head with four dots and four short arcs. It differs from the Water nozzle (a cone with one tip) by its square head and four jets.

### Behaviour and numbers

- **Law (L7, L8).** Supplied by a store through a fully open Tap ([EL-003](EL-003-tap.md)) there are three stages in series: the store's mouth join (K₀ = 0.08349), the Tap's seat (K₀) and the four seats in parallel (0.01336); pipe and elbow joins are lossless. 1/K² = 2/0.08349² + 1/0.01336² = 286.9 + 5602.6 = 5889.5, so **K = 0.01303**. All four seats sit behind one fitting head, so each passes a quarter.
- **Rated head 2.0 m.** Q = 0.01303 × √2.0 = 0.01843 m³/s in total, **0.00461 m³/s per stream**; each leaves at v = (Q/4) / (0.6 × 0.001257) = 0.00461 / 0.000754 = 6.11 m/s, so v² = 37.3 m²/s², 95% of 2·g·h (the Tap's seat takes the rest beside the mouth join).
- **Footprint.** A stream thrown at 30° returns to outlet height at v²·sin 60° / g = 37.3 × 0.866 / 9.81 = **3.30 m**. In general v² = 18.66 × head, so the distance is 1.65 × head: 1.65 m at 1.0 m of head. The footprint is four spots on a circle whose radius follows the head: at most 3.3 m at the rated head and nothing beyond it.
- **Reach follows supply.** As a Header tank drains from 2.0 m to 1.7 m of head, (2 × 0.25 / 0.01303) × (√2.0 − √1.7) = 38.37 × 0.1104 = **4.2 s**, the spots move in from 3.30 m to 1.65 × 1.7 = 2.80 m.
- **Blocked supply, no spray.** A closed tap upstream, a dry tank or an unjoined `Start` gives no head at the fitting and nothing leaves any outlet.
- **Off-footprint receivers stay dry.** A stream is a set of sample paths (L8): each sample path that ends inside a store's declared interior is credited to it; a sample that meets any other collider first is spilled. A receiver between two spots, or beyond the circle, gets none. What lands on the bench goes to `Spilled`.
- **A blocked jet.** A Wall in front of one outlet takes that stream (`Spilled`); the other three are unchanged, because a stream's fate does not alter the seat behind it.
- **No force on bodies (L8, L12 c).**

**Acceptance geometry (rows S1 to S7).** A Header tank ([EL-002](EL-002-header-tank.md)) at `stand_height` 1.65, full: surface 1.65 + 1.15 = 2.80 m, mouth at 1.80 m. Route: tank mouth → Tap → 90° elbow turned down (drop 0.40 m) → vertical pipe 0.85 m → 90° elbow to horizontal (axis 0.15 m above the bench) → pipe along the bench, laid along −X from the sprinkler → 90° elbow turned up, which rises 0.40 m to 0.55 m ([EL-010](EL-010-water-elbow-90.md)). `Start` sits on that end, so the outlets are 0.55 + 0.25 = **0.80 m** above the bench and the head at the outlets is 2.80 − 0.80 = **2.0 m** when full. Rain collectors ([EL-172](EL-172-rain-collector.md): opening 1.25 × 1.25 m, interior 1.25 × 0.08 × 1.25, floor datum (0, −0.01, 0)) are static and are placed with their rims at 0.80 m, their `End` capped ([EL-012](EL-012-water-pipe-cap.md)). Along +X the landing distance from the +X outlet is 1.65 × head: 3.30 m at 2.0 m, 2.80 m at 1.7 m. A collector centred 3.10 m from that outlet spans 2.475 to 3.725 m and takes the whole of that stream over that range of head. A second collector centred 3.10 m away on the diagonal between the +X and +Z streams is 3.10 × sin 45° = 2.19 m from each stream's line and its opening reaches only 0.625 m toward it: it stays dry.

## Engine capabilities

Map families (navigation only, D14): EnvironmentState, FiniteLedger, FluidAdvection, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, StateTransaction, TopologyTransaction.

| Row | Mark | Use here |
| --- | --- | --- |
| W1 static body and collider | present | head |
| W22 authored state | present | pose |
| W4 fluid port contract | present, untested | fitting |
| W3 sockets and joins | narrower: socket count, package-local ids, capture distance 0.20 m and angle 20°, and outlets that are not joinable are `ENGINE-CAPABILITY-INSTANCES`; compiling joins to edges is `ENGINE-LIQUID`; the port record is `ENGINE-GAS-NETWORK` | 1 socket (`Start`), 0 immersed ports, 4 outlets that are not joinable |
| W7 fitting, seats, flow, with four seats on one fitting | missing | `ENGINE-LIQUID` |
| W8 free ends that are not mouths, their streams and sample-path credit | missing | `ENGINE-LIQUID`; path candidates from `ENGINE-SPATIAL-QUERIES` |

Counts: 2 present, 1 untested, 1 narrower, 2 missing. No store, so no thermal-node row.

### Engine conformance (22 November 2026)

1. **Declaration data only.** A static body, one collider, one fitting with four seats and four outlets, one socket. There are no packets (L8).
2. **Generic laws.** Division is four equal seats under one head (L7). The footprint is four ballistic sample-path sets (L8). "Blocked supply prevents spray" is no head at the fitting.
3. **Three layers.** *Collider:* one box. *Visuals:* the table above; streams are the shared liquid presentation. *Animation:* none declared.
4. **One-way flow and typing.** Liquid state → shared stream presentation; nothing flows back. Mouth id and role are enums; values f32.
5. **Flat-state counts.** Bodies 1 (static); colliders 1 box; joints 0; drives 0; sensors 0; signal nodes 0; liquid nodes 1 fitting; seats 4; thermal nodes 0 (no store); sockets 1; immersed ports 0; outlets 4; bindings 0; parameters 0.
6. **Performance fitness.** One box. Four streams of a fixed number of sample paths per substep while it sprays, candidates from `ENGINE-SPATIAL-QUERIES` (L18); none while it is dry.
7. **Tests are scenarios.** Rows S1 to S7.

## Sources and legacy

- Requirement: "Supplied pressure divides liquid into a bounded spray footprint"; outcome "Blocked supply prevents spray and off-footprint receivers remain dry." No variants.
- Integration [sequence-task-531](../requirements.md#sequence-task-531). Component research [water](../../component-research.md#water): "collection is accounted"; "Spray ← committed rate". Its "distributed packet source with finite budget" is replaced by streams (L8, decided in EL-001).
- **Legacy.** None at `a6c914e` (searched for "sprinkler", "spray", "water"). The pneumatic jet regions of `docs/finite-gas-foundation.md` are a separate gas family. **Files harvested:** none.

## Acceptance outline

Authority: [element-036](../requirements.md#element-036). Tolerances L17. Planned class `SprinklerTests`. Every row uses the acceptance geometry; the Tap's `opening` is 1 unless stated, and it is fixed for the Run.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| S1 | Given the acceptance geometry, when the tank falls 0.30 m (stall guard 13 s), then the on-footprint collector has gained a quarter of the tank's loss (0.25 m² × 0.30 m ÷ 4 = 0.019 m³) within 0.004 m³ | `SprinklerTests.AStreamLandsInTheFootprint` | S2, S3 | — |
| S2 | Given the same run, then the diagonal collector has gained nothing (under 0.004 m³) | `SprinklerTests.AnOffFootprintReceiverStaysDry` | S1 | a collector centred 4.5 m out on +X (spanning 3.875 to 5.125 m, beyond 3.30 m) also gains nothing |
| S3 | Given the Tap at `opening` 0, when Run advances 10 s, then every committed flow is under 0.0005 m³/s (L17) and `Spilled` is 0 | `SprinklerTests.ABlockedSupplyPreventsSpray` | S1 | `Start` unjoined: the same |
| S4 | Given the four committed outlet flows at 2.0 m of head, then each is 0.00461 m³/s within 10% and each is within 10% of their mean (a lopsided ring would put one spot's collector more than a tenth off the others) | `SprinklerTests.TheSupplyDividesEqually` | — | — |
| S5 | Given a Wall 0.3 m in front of the +X outlet, then the +X collector gains nothing and the other three flows are as S4 | `SprinklerTests.ABlockedJetDoesNotChangeTheOthers` | S1 | — |
| S6 | Given S1, then tank, collectors, `Spilled` and `Drained` satisfy the ledger identity at every sampled tick | `SprinklerTests.SprayIsConserved` | — | — |
| S7 | Given a run, then Reset restores every store and zeroes the accounts; save and load keep the pose and the join | `SprinklerTests.ResetAndSaveLoadRestore` | — | — |

Hand calculations. S1: the fall takes 4.2 s (Behaviour), stall guard 3 × 4.2 = 12.7 → 13 s; loss 0.25 × 0.30 = 0.075 m³, a quarter 0.019 m³. S4: 0.01843 / 4 = 0.00461 m³/s.

Generic cover: W22. No wire port, no parameter. S1, S2 and S5 need EL-172 and EL-012; S1 uses a catch test in place of a dry-surface assertion, so nothing depends on where a stream meets the bench. Chrome recipe (pending): stand the sprinkler on an upturned elbow fed from a Header tank through a Tap, place collectors around it, open the tap before Run.

## Open questions

All resolved; none needs the owner.

1. Packets and a budget: none (L8). The supply is the budget.
2. Rotating head: none (D19). Four fixed jets give a bounded footprint.
3. Footprint parameter: none (D13). Head sets the radius; the pose sets the direction.
4. Four outlets rather than more: four is the smallest count that reads as a ring, and each outlet is one stream's sample paths per substep.

## Integration

- **Works with.** EL-002, EL-003, EL-008, EL-010, EL-012, EL-022, EL-172, EL-034, EL-171, EL-029 (none delivered); Wall (delivered).
- **Campaign.** Chapter 9 "Stored Potential", levels 81–90; reuse 91–100, 131–140, 146–150. Needs the lesson-fixture story (S8).

## Delivery

- **Slice shape.** Single element, after EL-013 and with or after EL-172.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (stream samples, immersion and point queries, L18), `ENGINE-ISLANDS-SLEEP` (a change of contents or level wakes what it loads; not delivered, and nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared sockets, outlets and immersed-port locators); then `ENGINE-GAS-NETWORK` (the one fluid port record); then `ENGINE-LIQUID`; then `ENGINE-THERMAL` (every liquid store is a thermal node, owner decisions 18 and 20); then EL-002, EL-003, EL-008, EL-010, EL-012, EL-013, EL-172; then S7.
- **Files.** Adds `elements/Sprinkler/Sprinkler.csproj`, `SprinklerElementDeclaration.cs`, `sprinkler.tscn`, `icon.svg`, `CuriousContraptions.tests/SprinklerTests.cs`; edits `CuriousContraptions.slnx` and this spec. Other lists as EL-001.
- **Review tier.** Data-only.
- **Disqualifiers.** A fitting limited to one outlet; outlets that cannot be declared without a socket; an outlet normal that cannot be declared off the body axes.

## Coverage binding (planned, not yet live)

Blocked on the coverage follow-on story S7. The named-identity binding section (the EL-239 form) cannot be written yet: umbrella `todo-340`; `sequence-task-589` co-anchored with `element-036`, not with a todo; no `s457` anchor.
