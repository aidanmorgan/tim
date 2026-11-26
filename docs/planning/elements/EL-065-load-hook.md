# EL-065 · Load hook declaration readiness spec

**Current delivery (24 October 2026):** [EL-065 delivery slice](../invest/named-elements.md#element-065). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against published main `8d1d68aa`; their `path:line` citations are to that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-065 |
| Name | Load hook |
| Type | Mechanical |
| Requirement anchor | [element-065](../requirements.md#element-065); scope index [todo-193](../requirements.md#todo-193) |
| Named entry | [named-elements.md#element-065](../invest/named-elements.md#element-065); proof owner S333 |
| CAT spec refined or extended | none. Related: [CAT-067 weight](CAT-067-weight.md) (its gold eye is the engagement target), [CAT-058 rope anchor](CAT-058-rope_anchor.md) and [CAT-053 pulley](CAT-053-pulley.md) (rope law) |
| Related identities | [EL-063 cable winch](EL-063-cable-winch.md) (crane), [EL-067 steel cable](EL-067-steel-cable.md), [EL-069 moving bucket](EL-069-moving-bucket.md) and [EL-070 moving cage](EL-070-moving-cage.md) (handle targets) |
| Roadmap story | [EL-065 delivery slice](../invest/named-elements.md#element-065); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-ISLANDS-SLEEP (a hung hook and a carried load at rest) and ENGINE-SPATIAL-QUERIES (contact candidates for the bowl against a moving ring); then S7. Its declaration uses delivered descriptor kinds; one capability it relies on has no test (a box carried through the bore of a moving ring) |
| Disposition | To deliver as a single element |
| Planned package | `load_hook`, directory `elements/LoadHook/`, class `LoadHookElementDeclaration` |
| Coverage binding | Planned, not yet live (section 7); blocked on S7. No CAT number |
| delivery-batches class | 1b, run "Tensile", needs capability probe (`docs/planning/invest/delivery-batches.md:41`). **Confirmed** as a run member, its own slice. The probe is section 3: no engine story of its own is needed beyond the engine programme (owner decision 3a), but the slice is engine tier because of one untested capability |

## 2. Declaration

Follows the delivered **Weight** shape: a dynamic root body with several colliders, a declared mass frame and one `Load` Tie (`elements/Weight/WeightElementDeclaration.cs:12`, `:43`–`:44`, `:96`).

**Interfaces.** `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`) and `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`). `KinematicMode.Dynamic`, `PayloadPolicy.None`, domains Mechanical and Rope.

**Body and colliders.** One dynamic body, four boxes of 0.04 m square bar in the local XY plane. The origin is the Tie point at the top; the bowl centre is directly under it so a loaded hook hangs plumb.

| Collider | Box full size (m) | Centre (m) | Spans |
| --- | --- | --- | --- |
| neck | 0.12 × 0.04 × 0.04 | (−0.04, −0.02, 0) | x −0.10…0.02, y −0.04…0 |
| shank | 0.04 × 0.30 × 0.04 | (−0.08, −0.19, 0) | y −0.34…−0.04 |
| bowl | 0.20 × 0.04 × 0.04 | (0, −0.36, 0) | x −0.10…0.10, y −0.38…−0.34 |
| tip | 0.04 × 0.12 × 0.04 | (0.08, −0.28, 0) | y −0.34…−0.22 |

- **Throat** (shank inner face to tip inner face): 0.12 m wide, 0.12 m deep. This is the batch value; the earlier draft's 0.18 m is replaced.
- **Mouth** (tip top to neck underside, open toward +X): 0.18 m.

**Mass and inertia.** 0.3 kg, split by bar length (neck 0.049, shank 0.122, bowl 0.081, tip 0.049 kg). Centre of mass (−0.026, −0.223, 0). Inertia rule: `MassFrameDeclaration.Constant` with the principal moments of the four boxes summed by the parallel-axis rule about that centre; the slice computes them once and its test recomputes them independently from the table above. The four bars are asymmetric in the XY plane, so the principal frame is turned about Z from the local axes; the slice computes that angle with the moments and declares it in the mass frame. Gravity (0, −9.81, 0), linear drag 0. Unloaded, the hook hangs tilted by atan(0.026 / 0.223) ≈ 7°, which reads as a hook and does not close the mouth.

**Material** (restitution, friction, rolling resistance, bounce threshold): `(0.1, 0.6, 0, 0.1)`. Low bounce and high friction keep an engaged bar seated in the bowl.

**Parameters.** None (D13: fixed size per kind).

**Port.**

| `PortId` | Role | Domain | Direction | Signal | Body | Position (m) | Attachment |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `Tie` | `PortRole.Tie` | Rope | Bidirectional | none | 0 | (0, 0, 0), normal +Y | `RopeAttachmentRole.Load` |

**Stores, sensors, events, presentation bindings.** None. Pick radius 0.35 m. Footprint: 1 body, 1 moving, 0 constraints.

**Palette.** `ElementCategory.Motion`, palette order 106 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), id `load_hook`, display name "Hook", catalogue colour pale grey (metal detail, `DESIGN.md:248`; proposed value `(0.80, 0.85, 0.87)`, D9).

### Art and solidity

| Node | Mesh and size | Class |
| --- | --- | --- |
| `Visual_Neck`, `Visual_Shank`, `Visual_Bowl`, `Visual_Tip` | four pale grey boxes, each the size and pose of its collider | solid, one collider each |

There is no eye ring on the hook: the gold endpoint knot is drawn by the rope presentation at the Tie (`DESIGN.md:343`), so no art exists without a collider. Icon: a grey J-hook with a gold knot above it. At a glance it differs from the Rope anchor (a ring on a square mount) by being an open J with no mount.

### Behaviour and numbers

- **Engagement is contact only.** No joint is created. The load is carried while its bar or ring rests in the bowl; friction 0.6 and the 0.12 m tip keep it there. A body that is not in the bowl gets no force from the hook.
- **What fits.** The agreed handle bar of the bucket (EL-069) and cage (EL-070) is 0.06 m in diameter: 0.03 m clear on each side in the 0.12 m throat and 0.12 m of entry room in the 0.18 m mouth. Both handles lie in the carried part's XY plane, as the Weight's eye does: the bucket's bail is a half torus whose apex bar runs along local X, and the cage's eye is an upright ring (hole 0.22 m). So for all three targets the hook is turned 90° about Y relative to the load, which puts its bowl through the ring or under the bail (cross-spec contract, checked against EL-069 and EL-070).
- **The Weight's eye.** The delivered Weight has a real ring: a Torus collider of centreline radius 0.14·s and tube radius 0.0175·s, where s = 0.1367·mass + 0.4531 (`WeightElementDeclaration.cs:73`–`:75`; art `elements/Weight/weight.tscn`, `Mesh_Eye`). Its hole is 0.245·s across, but the Weight's cylinder fills the lower part of it: the eye centre is at 0.5771·s and the cylinder top at 0.5585·s (`weight.tscn:67`, `:71`), so the usable opening above the cylinder is about 0.141·s high: 0.069 m at 0.25 kg, 0.083 m at 1 kg, 0.141 m at 4 kg. The hook's 0.04 m square bar leaves 0.029 m spare at 0.25 kg and 0.043 m at 1 kg, so the tip threads the eye of every Weight, with little room at the smallest. The ring's tube (at most 0.054 m thick at 8 kg) fits the 0.12 m throat. The eye lies in the Weight's XY plane, so the hook must be turned 90° about Y to thread it.
- **What cannot be hooked.** A Tennis ball (0.5 m across) or any other ball does not fit the 0.18 m mouth.
- **Mass.** 0.3 kg is light beside the Weight's 0.25–8 kg, so a counterweight lesson stays about the loads. With the lightest Weight the hook is more than half the lifted mass; levels that need a fine balance use a heavier load.
- **Rope.** The hook hangs as any `Load`: tension-only, connect-time length, one span end.
- **Strand.** The rope on `Tie` becomes solid with ENGINE-CONNECTION-MATERIALS (solid strand, owner decision 1); the delivered rope's missing collider is a recorded defect owned by that story, not a prerequisite here. The slice's probe re-checks that the strand's collider does not snag the hook's neck where the rope leaves the Tie.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Dynamic root body with several colliders and a declared constant mass frame | *present* | `MassFrameDeclaration` `CuriousContraptions.Abstractions/MassFrameValues.cs:38` | `CuriousContraptions.tests/WeightTests.cs:139`; `CuriousContraptions.tests/CompoundMassAndRollingTests.cs:114` |
| Tie port with the Load role on a dynamic body; hangs, swings, goes slack | *present* | `RopeAttachmentRole.Load` `CuriousContraptions.Abstractions/PortDescriptor.cs:74` | `CuriousContraptions.tests/RopeTests.cs:21`, `:34`, `:194` |
| A rope-hung body lifted over a pulley by a heavier load | *present* | `TensileSpanDeclaration` `CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:25` | `CuriousContraptions.tests/WeightTests.cs:1153` |
| Connected bodies keep ordinary contact while the rope is slack or cut | *present* | same | `CuriousContraptions.tests/RopeTests.cs:385` |
| Resting contact between two dynamic bodies holds without bounce or creep | *present* | `ColliderShapeKind` `CuriousContraptions.Abstractions/PhysicsDeclarationValues.cs:12` | `CuriousContraptions.tests/WeightTests.cs:585` (one Weight stacked on another; cylinders, not boxes) |
| A moving Torus collider with a real hole | *present* | `ColliderShapeKind.Torus` `PhysicsDeclarationValues.cs:12`; moving-hollow path `CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:161` | `CuriousContraptions.tests/WorkshopPhysicsLawHollowTests.cs:1255`, `:1371` |
| **A dynamic box bar threaded through the bore of a dynamic ring carries it under gravity** (hook through the Weight's eye) | *present, untested* | the two rows above together | none. The slice's row H1 is its first test; no engine story is expected |

The doc comment at `PhysicsDeclarationValues.cs:10` places the hollow kinds on static or dynamic bodies (corrected in `dbaacad1`): moving hollows are solved (`WorkshopConstraintSolver.cs:161`) and the Weight ships one.

**Element dependencies.** Delivered: Weight, Rope anchor, Pulley, Wall. Not delivered and not blocking: EL-063 winch, EL-069 bucket, EL-070 cage, EL-067 cable.

### Engine conformance

- **Declaration data only.** One dynamic body of four boxes and one Tie port. No "engaged" flag, joint on seating, callback or engine branch.
- **Generic laws.** Carrying is contact and friction between the bowl and a bar or ring; hanging is the tension-only span law; an unengaged body gets no force because nothing but contact acts.
- **Layers.** Colliders: 4 boxes on the dynamic body. Visuals: four boxes of the same size, none used as a collider. Animation: all four art nodes follow the body pose; no presentation binding. Visible = solid: each bar has its own collider, and no eye ring is drawn.
- **One-way flow and typing.** Nothing is read back from art. Port role, domain and attachment role are enums; values are f32.
- **Flat state.** 1 dynamic body, 4 box colliders, 0 joints, 0 drives, 0 sensors, 0 domain nodes, 0 stores, 0 bindings.
- **Performance.** Four primitives. The bars are 0.04 m thick, above the 1 cm barrier bound that the engine's 480 Hz substeps and speculative margins keep at up to 64 m/s (`docs/gpu-f32-physics.md:396`); no per-part tunnelling check. No island sleeping exists today; a resting hook costs nothing only once ENGINE-ISLANDS-SLEEP (owner decision 3a) is delivered, ahead of this part.
- **Tests are scenarios.** Rows H1 to H9 run the production simulation with delivered parts.

## 4. Sources and legacy

- Requirements row: "Unengaged hook cannot carry a remote body." No variants.
- todo-193 scope index groups EL-064..066.

No legacy hook exists. Shared facts:

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | The Weight's gold eye is a ring r 0.14 m, tube 0.035 m at height 0.72 r, rotated 90° about X; the rope tie sits at r + 0.08. | `parts/WeightPart.cs@a6c914e:L38-L39`; `engine/MachineData.cs@a6c914e:L99-L100` | carry forward | Defines the engagement target size and position. |
| 2 | Weight is a rope `Load` with one `Tie`; Locked means fixed during editing, not immovable during simulation. | `parts/WeightPart.cs@a6c914e:L7-L21` | carry forward | Hook uses the same role. |
| 3 | Connected-body collision stays enabled on rope routes. | via [CAT-058](CAT-058-rope_anchor.md) R17 | carry forward | The hook must collide with its target to engage. |

**Files harvested:** `parts/WeightPart.cs`, `engine/MachineData.cs`, `engine/RopeNetwork.cs`. Searched with no hit: `parts/`, `engine/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign`, `reference/` (hook in the hook-removal sense only, `engine/SceneImpactEffect.cs@a6c914e:L9-L9`, unrelated).

## 5. Acceptance outline

Requirement row: [element-065](../requirements.md#element-065).

- **Chrome UI recipe (pending, post-catalogue).** Place a Pulley high, a heavy Weight on a ledge tied over the Pulley to the Hook's `Tie`; place a light Weight so its eye sits in the hook's bowl; place a second light Weight 1 m away on the bench. Knock the heavy Weight off. Verify placement and links, Run, Reset.

All tests are planned, in `CuriousContraptions.tests/LoadHookTests.cs`, on the real simulation. The lifter is the delivered counterweight route (Hook → Pulley → 8 kg Weight), so no undelivered part is needed. Stall guard: 10 s.

| Row | Given / when / then | Planned test | Control | Boundary | Tolerance and its gameplay reason |
| --- | --- | --- | --- | --- | --- |
| H1 engaged load is carried | Hook turned 90° about Y, its bowl threaded through the eye of a 1 kg Weight; the 8 kg counterweight falls: the Weight rises with the hook by at least 1 m and stays in the bowl | `LoadHookTests.AnEngagedWeightRisesWithTheHook` | H2 | Weights of 0.25 kg (0.069 m opening, 0.029 m spare) and 4 kg | Weight eye stays within 0.10 m of the bowl (less than the 0.12 m tip, so a slipped load fails) |
| H2 exact source outcome: an unengaged hook cannot carry a remote body | A second 1 kg Weight 1 m away on the bench in the same run does not move | `LoadHookTests.ARemoteWeightIsNotCarried` | H1 in the same scene | A Weight whose eye is beside the bowl, 0.05 m outside the tip: the hook rises past it and it stays down | Remote Weight moves less than 0.01 m in 3 s |
| H3 the load is real | With the engaged Weight heavier than the counterweight (8 kg on the hook, 4 kg counterweight) the hook does not rise | `LoadHookTests.AnEngagedLoadHeavierThanTheCounterweightStaysDown` | H1 | Equal 4 kg and 4 kg + hook: the hook side descends | Rise under 0.02 m in 3 s |
| H4 mass and inertia | The compiled mass, centre and principal moments equal the four-box values recomputed in the test | `LoadHookTests.TheCompiledMassFrameIsTheFourBoxSum` | none | none | Relative 1e-4: an f32 sum of declared constants, a defined-interface value |
| H5 the hook is solid | A Tennis ball dropped on the bowl rests on it; the hook swung into a Wall stops at it | `LoadHookTests.TheHookStopsABallAndAWall` | The ball dropped 0.5 m to the side falls past | none | Ball rest height within 0.02 m of bowl top + radius |
| H6 unhooking by slack | Lower the engaged Weight onto the bench until the rope is slack: the Weight rests on the bench and the hook hangs free in the eye without lifting it | `LoadHookTests.ASetDownLoadIsNotDraggedByASlackHook` | H1 | none | Weight moves less than 0.01 m after settling |
| H7 Reset | After H1, Reset restores hook, both Weights and the link | `LoadHookTests.ResetRestoresTheHookAndItsLoads`; S4 row R3 once delivered | none | none | Authored poses exactly |
| H8 save and load | The built scene of H1 round-trips | S4 row R1b once delivered, else `LoadHookTests.ASavedHookLoadsWithItsLink` | none | none | Field equality |
| H9 wrong-kind link and second rope | An electrical or activation link to `Tie`, and a second rope on `Tie`, are refused | S4 row R5 once delivered, else `LoadHookTests.AWrongKindOrSecondLinkIsRefused` | The single rope is admitted | none | Typed refusal |

No parameter refusal row: no parameter. Rows for the bucket and cage handles are owed by EL-069 and EL-070 against this hook's throat; a row for the winch is owed by whichever of EL-063 and EL-065 lands second.

## 6. Open questions

**Resolved**

1. Engagement is by contact only, with no attachment joint created on seating. D19 (the spec's proposal); it is also what "actual supported engagement" in the ledger row says.
2. No `Engaged` output or read. D19: no requirement row or campaign lesson uses one.
3. Which parts can be hooked: any part with a bar or ring that fits the 0.12 m throat. Today that is the Weight's eye; the bucket declares a 0.06 m bail and the cage a 0.06 m eye ring, both in their XY plane like the Weight's eye, so the hook is turned 90° about Y for each. Decided now; no "engageable" flag exists or is needed, because engagement is plain contact.
4. Throat 0.12 m and bar 0.04 m replace the earlier 0.18 m and 0.06 m. Decided now: the batch value for the throat, and a bar thin enough for the smallest Weight's usable eye opening (0.069 m).
5. No eye ring art on the hook. Decided now: visible = solid would need a second collider for a ring the rope knot already draws.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Weight, Pulley, Rope anchor, Wall (delivered). EL-063 winch (lifter), EL-069 bucket and EL-070 cage (handles), EL-067 cable (connection material) are not delivered.
- **Campaign.** Hook and anchor first use in levels 11–20, load variants in 41–50 (`docs/planning/invest/campaign-reservations.md:44`). Shared interactions [IX-04 tension transmission](../requirements.md#interaction-04) and [IX-01 contact impulse](../requirements.md#interaction-01).
- **Lesson fixture (S8).** A level that places the hook before this slice needs S8.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (on `origin/main`, `25df3cd5`; pattern: EL-239 Baseball). The section is not written here: the reader parses every EL spec that carries that heading and refuses one whose preconditions fail. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `load_hook` |
| Named identity | EL-065, `../invest/named-elements.md#element-065` |
| Acceptance | `todo-193` and `sequence-task-618` |
| Decision | S333; `scope-corrections.md` has no `s333` block |
| Mode records | `Configuration=Fixed` |

What it lacks today: an `s333` anchor block in `docs/planning/invest/scope-corrections.md`, and its todo-193 anchor is a scope index, not immediately followed by its sequence-task-618 anchor in `requirements.md`. S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** One element slice. After the engine programme it may precede the winch, bucket and cage.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-ISLANDS-SLEEP and ENGINE-SPATIAL-QUERIES; then S7 (the coverage follow-on, which widens the reader, owner decision 15). S3 and S4 shorten the file list and replace rows H7 to H9 with suite rows. No engine story of its own.
- **Files.** Adds `elements/LoadHook/LoadHook.csproj`, `LoadHookElementDeclaration.cs`, `load_hook.tscn`, `icon.svg`, `CuriousContraptions.tests/LoadHookTests.cs`. No test-project edit (published main compiles every top-level `*.cs` by one rule). Edits, until S3 lands: `CuriousContraptions.slnx`, the trim list in `CuriousContraptions.tests/CSharpStaticGateTests.cs` (ends at `:703`) and the placement switches in `CuriousContraptions.tests/PackageRuntimeParityTests.cs` (`:641`; generic list `:653`). At delivery: the binding section of section 7. This spec and TODO.
- **Review tier.** Engine: one capability is *present, untested*. The diff itself is package and tests only.
- **Disqualifiers the probe must re-check.** H1 fails for a reason in the solver (a thin dynamic box against the bore of a dynamic Torus tunnels or jitters): then the defect is its own engine slice before this one. The bucket or cage spec fixes a handle bar thicker than 0.08 m, or a handle that is not a bar or ring in the part's XY plane. A 0.04 m bar proves too thin for stable contact at 480 Hz substeps: thicken the bar to at most 0.05 m (0.019 m spare at 0.25 kg), or raise H1's lower boundary to the 1 kg Weight and record that the 0.25 kg Weight cannot be hooked. Once the strand is solid (ENGINE-CONNECTION-MATERIALS), its collider snagging the neck.
