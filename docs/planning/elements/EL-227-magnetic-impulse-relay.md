# EL-227 · Magnetic impulse relay — named-identity spec

**Current delivery (22 November 2026):** [EL-227 ledger entry](../invest/named-elements.md#element-227), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending. Reworked 22 November 2026 after the independent audit (`docs/planning/elements/readiness/audits/elements-new-identities.md`, F4, X2, G-C): seats, region, the one-boost rule and rearming now close on this spec's own numbers. Repaired 23 November 2026 after the second audit (`docs/planning/elements/readiness/audits/reaudit-group-1.md`, EL-227): restitution is the product of the two colliders' values, so the blow chain is nearly dead and rows I1 and I7 are re-derived; an output seat pad and lamp show output occupancy.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-227 · Magnetic impulse relay · Mechanical |
| Requirement anchor | [todo-399](../requirements.md#todo-399) (`sequence-task-382`) |
| Ledger entry / owner | [element-227](../invest/named-elements.md#element-227); slice `named-el-227`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a) and `ENGINE-FIELD-FORCE`**, and after a responding ball exists. Delivery batches 1c, "magnetic relay". Distinct from [EL-060 Electromagnet](EL-060-electromagnet.md): unsupplied, unswitched, and it passes an impulse on rather than picking up. |
| Planned package | id `impulse_relay`, directory `elements/ImpulseRelay/` |
| Related | EL-060, [EL-091 Cannonball](EL-091-cannonball.md) (the responding ball), [CAT-067 Weight](CAT-067-weight.md), [CAT-052 Pressure plate](CAT-052-pressure_plate.md) (load-reading shape) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (field membership candidates), `ENGINE-SENSOR-VOLUMES` (the seat's load region evaluated in the tick) and `ENGINE-ISLANDS-SLEEP` (a body entering the region wakes; nothing here assumes sleeping exists); then `ENGINE-FIELD-FORCE` (the one field text, `readiness/batch-l.md` section 4 item 6, with the occupancy-capacity addition proposed in the batch summary) and `ENGINE-PARAMETER-BOUND-FRAMES` (the force bound to `well_energy`); then EL-091 Cannonball with the responding property. |

## 2. Declaration

No delivered part has this shape. Nearest: the Springboard's passive slider (`elements/Springboard/SpringboardElementDeclaration.cs:63`) for the striker, the Pressure plate's load node (`elements/PressurePlate/PressurePlateElementDeclaration.cs:93`) for the occupancy output, plus the field-source declaration `ENGINE-FIELD-FORCE` delivers.

- **Interfaces.** `ISliderElement` (`MechanicalContracts.cs:15`), `ISignalElement` (`SignalContracts.cs:8`), `ISceneGeometryElement` (`SceneGeometry.cs:10`), and the field-source interface from `ENGINE-FIELD-FORCE`.
- **Rail (body 0, static).** Level trough 2.4 × 0.15 × 0.8 m with 0.2 m side rails; a magnet block 0.2 × 0.5 × 0.8 m at its centre with a 0.3 m square bore (four boxes around it), its axis 0.32 m above the trough floor (a Cannonball's centre height). The block's **input face** is at x = −0.1 m and its output face at x = +0.1 m. An input seat pad 0.7 × 0.02 × 0.8 m lies flush in the trough floor next to the input face (x −0.8 to −0.1; a seated Cannonball touches the floor at x −0.47), and an output seat pad of the same size flush next to the output face (x 0.1 to 0.8; a staged Basketball touches the floor at x 0.49). Each is a load region: an unrotated box. Restitution 0, friction 0.3.
- **Striker (body 1).** Rod of radius 0.14 m and length 0.3 m through the block; mass 1 kg; restitution 0.5. Passive slider along X, limits −0.025 to +0.025 m, centring spring 50 N/m, damping 5 N·s/m. Centred, it stands 0.05 m proud of each face; it is never flush, so a ball touches the rod and not the block. Balls of radius 0.25–0.35 m meet it within 0.07 m of its axis.
- **Field source.** Unswitched and unsupplied (payment kind None), declared in the one field text of `ENGINE-FIELD-FORCE` (`readiness/batch-l.md` section 4 item 6): a bounded box region on the static rail, the responding property as its selector, a constant Force vector along +X, membership by centre of mass. **Region: a box on the input side only**, from the input face 1.0 m back along the trough (x from −1.1 to −0.1 m), 0.8 m wide and 0.8 m high. Inside it a responding body is pulled toward the input face with a uniform force `well_energy` ÷ 1.0 m (15 N at the default), bound at compile from the parameter (`ENGINE-PARAMETER-BOUND-FRAMES`). The strength is declared by its work: `well_energy` is the work on a body carried from the region's edge to the face plane. **Occupancy capacity one:** the source acts on the first responding body to enter (the occupant) and on no other until the occupant has left the region. The reaction acts on the static rail.
- **Seats.** Input seat: touching the striker's input tip. A Cannonball there has its centre 0.345 m from the face, inside the region. Staged seat: touching the striker's output tip, outside the region (the region has no output side). A staged Basketball's centre is 0.5 m from the block centre; the trough extends 1.2 m.
- **Signal nodes.** Two `SignalContactLoadDescriptor`s on body 0, local normal +Y, minimum mass fixed 0.1 kg: one on the input seat pad, bound to `ActivationOut`; one on the output seat pad, with no port, driving the output-seat lamp (two load faces on one static frame keep their own state: `ComposedPhysicalSignalTests.TwoLoadFacesOnOneStaticFrameKeepTheirOwnState`). Together they show the requirement's "visible input/output occupancy".

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `well_energy` | Real, J | 5–40 | 15 | Reject, build-only | The finite stored energy the row asks for. A Cannonball gains 65 % of it (it stops a radius short of the face): 9.8 J at the default |

| Port | Domain | Direction, role, kind | Position (m) | Meaning |
| --- | --- | --- | --- | --- |
| `ActivationOut` | Signal | Output, ActivationOut, Level | (−0.45, −0.1, 0.4) | A body rests on the input seat (the Pressure plate's output shape) |

- **Stores.** None; the well is the field's.
- **Presentation.** Striker body binding; a "spent" flag from the field source's committed occupancy; an input-seat lamp and an output-seat lamp, each from its load node's `CommittedContactLoadPhase`.
- **Palette.** Motion, "Impulse relay"; order 136 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Trough, side rails, magnet block | Boxes; block coral `#d96b58` with navy pole bands printed on | Solid, body 0 |
| Input and output seat pads | Flush boxes, gold `#f7cb52` | Solid (each collider is a load region); flush with the floor |
| Striker | Cylinder, gold `#f7cb52` | Solid, body 1 |
| Field glow over the input side | Soft halo | Decorative: glow |
| Spent flag, two seat lamps | Flush tabs | Decorative: printed flat |

Icon: a block between two circles with an arrow leaving the far one. It differs from the Electromagnet by the two seats and through-rod.

### Behaviour and numbers

Hand calculation at the defaults (`well_energy` 15 J, force 15 N over 1.0 m; Cannonball 6 kg, radius 0.32 m; staged Basketball 1 kg):

1. **Gain.** The occupant's centre travels from the region edge (1.0 m from the face) to 0.345 m from it: 0.655 m × 15 N = **9.8 J**. A rolling sphere carries 7/5 of its translational energy, so rolled in at 1 m/s (0.7 × 6 × 1² = 4.2 J) it arrives with 14.0 J, **1.83 m/s** (√(14.0 ÷ 4.2)); from rest at the edge, 1.53 m/s.
2. **Blow.** The occupant strikes the striker, which strikes the staged ball. Restitution is the product of the two colliders' values (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:117`): striker–Cannonball 0.5 × 0.08 = 0.04, striker–Basketball 0.5 × 0.55 = 0.275, so the chain is nearly dead. Cannonball and striker move together at 6 × 1.83 ÷ 7 = 1.57 m/s; the Basketball then leaves at between 7 ÷ 8 × 1.57 = 1.37 m/s (dead) and 1.275 × 7 ÷ 8 × 1.57 = 1.75 m/s (the 0.275 restitution of the last contact). So the staged Basketball leaves at about **1.4–1.8 m/s**, carrying at most ½ × 1 × 1.8² = 1.6 J of the 14.0 J. The striker stops at its +0.025 m limit and the occupant stops against it.
3. **Retained.** The occupant is held at the seat by 15 N. Its rebound off the striker (restitution 0.04: at most 0.07 m/s, 0.02 J) is turned back within a few millimetres, well inside the region. At rest its 15 N pull holds the striker against its +0.025 m limit (the centring spring gives only 50 × 0.025 = 1.25 N). The input seat pad reads it and `ActivationOut` goes on.
4. **One boost per rearming.** While the seat is occupied the source acts on no other body (capacity one). A second responding ball arrives at its own speed and adds only a plain collision. This is the "spent" state.
5. **Physical rearming.** The occupant must be driven out of the region against the field, which takes back what it gained (0.655 × `well_energy`). With delivered parts: a Bowling ball (4 kg) striking the output tip. Its restitution with the striker is 0.14 × 0.5 = 0.07 and the striker's with the Cannonball 0.04, so the blow is nearly dead: Bowling ball, striker and Cannonball move together at 4u ÷ 11 for a strike at u until the striker reaches its −0.025 m limit (0.05 m from where the occupant held it), and the Cannonball leaves the seat sliding at 4u ÷ 11. Once it rolls it keeps 5/7 of that energy. At `well_energy` 5 (gain 3.3 J) a 5 m/s strike sends it off at 1.8 m/s (9.9 J, about 7.1 J rolling); it climbs out against 5 N and leaves the region with about 3.8 J, and the next responding ball to enter is boosted again (row I7). A default (15 J) occupant needs (5/7) × ½ × 6 × (4u ÷ 11)² ≥ 9.8 J, a strike of at least 5.9 m/s; a 4 m/s strike (1.45 m/s, 4.5 J rolling) leaves it seated. Other means (an Electromagnet lift, EL-060) are later partners.
6. **Energy.** Staged-ball kinetic energy ≤ arriving kinetic energy + gain. The field is conservative: gain on the way in equals work paid on the way out, so no loop gains energy.
7. A non-responding ball gains nothing and is not held. A ball arriving on the output side is outside the region.
8. **Which bodies respond (audit X2; Batch I hand-back).** The responding property is declared on a body by one rule of `ENGINE-FIELD-FORCE` for every consumer (`readiness/batch-i.md` section 6), not by a consumer spec. [EL-060](EL-060-electromagnet.md) gives the Weight the property and keeps balls non-responding; the ball family contract forbids adding it in EL-091's slice, which publishes before the field story. So **the Cannonball's declaration gains the property, under that one rule, in the first field consumer's slice (EL-060's or this one, whichever is first).** Every other ball stays non-responding. A Weight used near this relay must stay outside the region (it would become the occupant).
9. Tick order: the field force is part of the mechanical solve; the seat reading commits with the physics tick and is read by the next tick's signal solve (D7).

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Contact impulse through an intermediate free body | present | contact solver | `ImpactLeverTests.AlignedBowlingBallImpact_FlingsPayloadUpward` |
| Passive sprung slider | present | `MechanicalContractValues.cs:104` | `WorkshopSpringboardTests` (class) |
| Contact-load node on a level box of a static frame, as a Level output | present | `SignalContractValues.cs:58`; validator `CuriousContraptions.Bundle/SignalBundleValidation.cs:187`–`:190` | `PressurePlateTests.TheInstanceMinimumMassDecidesItsOwnGate`; `ComposedPhysicalSignalTests.TwoLoadFacesOnOneStaticFrameKeepTheirOwnState` |
| **Declared field source acting on bodies with a responding material property** | **missing** | only gravity, airflow and ambient loads exist (`MechanicalContracts.cs:40`, `:124`) | Owner: `ENGINE-FIELD-FORCE` (its text names EL-227) |
| **An unswitched source declared by its work, with a box region, an occupancy capacity, a committed work ledger and a committed occupancy read** | **missing** | the story's text has only a switched source with strength and falloff | Owner: `ENGINE-FIELD-FORCE`; proposed story text is in the [batch summary](readiness/new-identities.md) |
| **A responding property on a ball** | **missing** | no ball declares one | Owner: `ENGINE-FIELD-FORCE` (the property is declared on a body by one rule); the first field consumer's slice adds it to the Cannonball's declaration (item 8) |
| A field magnitude bound to a real parameter | missing | — | Owner: `ENGINE-PARAMETER-BOUND-FRAMES` (parameter-bound values, coordinator decision 16) |

Counts: 3 present, 0 untested, 0 narrower, 4 missing (owners `ENGINE-FIELD-FORCE` and `ENGINE-PARAMETER-BOUND-FRAMES`, one per row).

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static trough and block, one passive sprung slider, one declared field source, one load sensor and one port. The boost is the field law with the declared work and region; the relay of the blow is rigid contact through a free body; retention is the same field force at rest; "spent" is the source's declared occupancy capacity; rearming is the field doing negative work. Nothing says "when a ball touches the magnet, launch the other". The missing capabilities are stated for any package: a field source with a bounded box region, an occupancy capacity, a work read and an occupancy read, and a responding property declared by one rule, owned by `ENGINE-FIELD-FORCE`; a magnitude bound to a parameter, owned by `ENGINE-PARAMETER-BOUND-FRAMES`. The capacity is not a relay-only rule: any field source may declare it. No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 9 boxes (trough floor, 2 rails, 4 around the 0.3 m square bore, 2 seat pads) and 1 cylinder (striker) as hidden `col_` proxies. Visuals: the art nodes, none a collider. Animation: halo, spent flag and the two seat lamps are bindings from committed occupancy and load state. What moves each art node: trough, rails, block and seat pads nothing (static body 0); the striker the body 1 pose; halo, flag and lamps their presentation bindings.
- **Flat state.** 2 bodies; 10 colliders (9 box, 1 cylinder); 1 prismatic joint; 1 field source; 0 drives; 2 load sensors; 2 signal nodes; 0 stores; 1 body-pose binding, 4 driver bindings.
- **Performance.** The field region is one bounded box (0.64 m³); its candidates come from `ENGINE-SPATIAL-QUERIES` and its work is bounded by the responding bodies inside; it never scans the scene. The rail is static. Whether a retained ball at rest may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. Speeds stay under 6 m/s.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-399](../requirements.md#todo-399): "Compatible arriving ball is retained while a different staged ball departs. Explicit finite stored energy and physical rearming; distinct from electromagnet pickup. Visible input/output occupancy and spent state." Lesson column: "Outgoing ball climbs pipe to switch; retained ball weights a tray. Test wrong material/side, missing staged ball, rapid second arrival, payload identity and no energy-gaining relay loop."
- **Campaign.** Chapter 5 (41–50), "magnetic impulse relay".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: speeds as ranges and inequalities; energy bound + 0.5 J; "retained" means the body stays in the region and rests within 0.05 m of the input seat within 10 s; body identities exact; stall guard 20 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| I1 | Cannonball rolled in at 1 m/s; Basketball staged | The staged ball leaves at 1.2 m/s or more (estimate 1.4–1.8 m/s, item 2) and the Cannonball is retained; they are two different bodies; the output-seat lamp is lit before the arrival and dark once the staged ball has left, and the input-seat lamp is lit once the Cannonball rests; staged-ball kinetic energy ≤ 14.0 J + 0.5 J | Control: `well_energy` 5 gives a slower departure than 40 | `ImpulseRelayTests.TheArrivingBallStaysAndTheStagedBallLeaves` |
| I2 | A Bowling ball (non-responding) rolled in at 1 m/s | It is not retained; the staged ball leaves no faster than with the same relay at `well_energy` 5 and a Cannonball | — | `ImpulseRelayTests.AWrongMaterialGetsNoBoost` |
| I3 | Cannonball arriving on the output side | Not retained, no boost | — | `ImpulseRelayTests.TheWrongSideDoesNothing` |
| I4 | No staged ball | The arriving ball is retained; nothing departs; the output-seat lamp stays dark throughout | — | `ImpulseRelayTests.AMissingStagedBallLaunchesNothing` |
| I5 | A second Cannonball 0.5 s after the first | The second arrival is not accelerated inside the region (its speed at contact ≤ its entry speed + 0.1 m/s); no body is deleted | — | `ImpulseRelayTests.ASpentStageGivesNoSecondBoost` |
| I6 | Two relays in a ring feeding each other, every staged ball a Cannonball (so each departing ball responds to the next relay's field) | Total kinetic energy never exceeds the initial energy plus one gain per relay + 0.5 J | — | `ImpulseRelayTests.ARelayLoopGainsNoEnergy` |
| I7 | `well_energy` 5 (gain 0.655 × 5 = 3.3 J); the retained Cannonball knocked out by a Bowling ball striking the output tip at 5 m/s | The Cannonball leaves the seat at about 4 × 5 ÷ 11 = 1.8 m/s (9.9 J; about 7.1 J once it rolls), leaves the region having lost at least 3.3 − 0.5 J between the seat and the region edge, and the next Cannonball is boosted | Control: a 1 m/s blow (Cannonball 0.36 m/s, 0.4 J) does not free it | `ImpulseRelayTests.RearmingCostsTheWellEnergy` |
| I8 | Departing ball climbs a Pipe to an Impact switch; `ActivationOut` wired to a Lamp | The switch fires, and the lamp is on while the Cannonball is retained | Control: before any arrival the lamp is off | `ImpulseRelayTests.OneBallSignalsAndTheOtherWeighs` |
| I9 | Run, Reset; Save, Load; `well_energy` 4.9; an electrical wire to `ActivationOut` | Restored, with the source unoccupied; both refused with a typed reason | Boundary: 5 and 40 admitted | `ImpulseRelayTests.ResetReloadAndRefusals` |

The lesson's "retained ball weights a tray" is met by the relay's own seat pad and `ActivationOut` (added under D19, because the lesson uses it): the trough is this part's static body, so a separate Pressure plate cannot sit under the seat.

**Integration.** Pipe, Impact switch, Lamp, Bowling ball (delivered); EL-091 Cannonball as the responding ball, EL-060 Electromagnet for contrast (later).

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `impulse_relay`; Named identity EL-227; Acceptance todo-399 and sequence-task-382; owner slice `named-el-227`; Mode records `Configuration=Adjustable`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g). Levels need S8.

**Delivery.** Prerequisites, in order: blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-SENSOR-VOLUMES` and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-FIELD-FORCE` (with the occupancy capacity) and `ENGINE-PARAMETER-BOUND-FRAMES`; EL-091 Cannonball and the responding property on it; the coverage reader admitting a slice-owned package. One element slice. Files: the `elements/ImpulseRelay/` package, `CuriousContraptions.tests/ImpulseRelayTests.cs`, and the hand-edited lists until S3 (S2 is published at `25df3cd5`; no test-project list edit). Review tier: engine. **Disqualified if** the engine story's field cannot be declared by its work with a one-sided region, has no occupancy capacity (then I5 fails: every entering body is boosted), or does not hold a body at the seat against a rebound.

## 6. Open questions

1. *Where is the "stored energy"?* Resolved under the standing delegation: in the field's potential well, declared as `well_energy`. A separate hidden store paid out on contact would be a second rule, and rearming would not be physical.
2. *Which balls respond?* Resolved (item 8): the Cannonball, its declaration gaining the property in the first field consumer's slice; the property itself is one rule of `ENGINE-FIELD-FORCE` (S635).
3. *How is it rearmed without a rope on a ball?* Resolved under the standing delegation: by a reverse blow through the striker. A ball has no rope port, so the earlier "rope and Weight" row could not be built.
4. *Why does a spent stage give no second boost?* Resolved: the source's occupancy capacity is one. A work ledger alone would not stop a second body from being pulled in.

Needs the owner: nothing.
