# EL-232 · Gears — named-identity spec

**Current delivery (21 November 2026):** [EL-232 ledger entry](../invest/named-elements.md#element-232), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-232 · Gears · Mechanical |
| Requirement anchor | [todo-187](../requirements.md#todo-187) (`sequence-task-365`) |
| Ledger entry / owner | [element-232](../invest/named-elements.md#element-232); owner **S347** (ratio and engagement decision); slice `named-el-232` |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a), `ENGINE-MECHANICAL-LINKS` and `ENGINE-PARAMETER-BOUND-FRAMES`.** Delivery batches lists it in run "rotary" (1b); the mesh link moves it to 1c. One catalogue entry with a `teeth` parameter (D8: a size is a setting of one entry, not several parts). Not a duplicate of the Reverse transmission, which reverses inside one housing at a fixed 1:1. |
| Planned package | id `gear`, directory `elements/Gear/` |
| Related | [CAT-057 Reverse transmission](CAT-057-reverse_transmission.md), [EL-200 Drive belt](EL-200-drive-belt.md), [EL-110 Flywheel](EL-110-flywheel.md), [EL-233 Generator](EL-233-generator.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-CAPABILITY-INSTANCES` (presentation from the declared joint frame, finding E7) and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-MECHANICAL-LINKS` (the mesh as a declared connection material with a toothed engagement class, and loop consistency) and `ENGINE-PARAMETER-BOUND-FRAMES` (rim, collider radius, mass and tooth art bound to `teeth`). |

## 2. Declaration

Shape: the Conveyor's pass-through shaft (`elements/Conveyor/ConveyorElementDeclaration.cs:44`–`:48`) as a bare toothed wheel.

- **Interfaces.** `IHingeElement` (`MechanicalContracts.cs:20`), `ISceneGeometryElement`, `IParameterizedGeometryElement` (`SceneGeometry.cs:10`, `:29`).
- **Bracket (body 0, static).** Plate 0.3 × 0.3 × 0.1 m behind the wheel.
- **Wheel (body 1).** Cylinder of pitch radius 0.025 m × `teeth` and width 0.15 m; mass 0.03 kg × `teeth`; solid-disc inertia from the declared mass and radius (the scaled mass-length-squared operation the Tennis ball uses, `elements/TennisBall/TennisBallElementDeclaration.cs:50`). Continuous hinge, friction torque 0.02 N·m.

| Parameter | Type, unit | Range | Default | Editor | Reason |
| --- | --- | --- | --- | --- | --- |
| `teeth` | WholeNumber | 8–40 | 16 | Reject, build-only | Radius 0.2–1.0 m; 16 teeth matches a 0.4 m wheel. A whole number makes every ratio exact |

| Port | Domain | Direction, role | Position |
| --- | --- | --- | --- |
| `DriveIn` | Mechanical | Input, DriveIn; toothed rim at the pitch radius | Wheel face |
| `Drive` | Mechanical | Output, Drive; the same toothed rim | Wheel face |

- **Connections.** A connection between two toothed rims is a **mesh**, a declared connection material of `ENGINE-MECHANICAL-LINKS` (`readiness/batch-j.md` section 4 item 3: a coupling law kind, its values and the engagement class it requires; here a reversed tooth ratio and the toothed class). The delivered rigid connection is unchanged and stays the default (owner decision 2): speed ratio −`teeth`(driver) ÷ `teeth`(driven), sign reversed. It is admitted only when the two axes are parallel within 2° and the centre distance is from the sum of the pitch radii to 0.03 m more (never less: the pitch cylinders are solid colliders on hinged bodies that cannot move apart, so they must not overlap); otherwise it is refused with a typed reason. A connection between a toothed rim and a plain rim is the ordinary Mechanical link (the delivered rigid connection; coordinator decision 36). No link art is drawn for a mesh: the teeth touching are the picture.
- **Presentation.** Wheel body binding (teeth visibly turn).
- **Palette.** Motion, "Gear"; order 121 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Bracket | Box, cream `#fff8e9` (owner decision 4) | Solid, body 0 |
| Wheel | Toothed disc drawn inside the pitch cylinder, tooth count following `teeth`; ochre `#d99e32` with a navy hub | Solid: the pitch cylinder is the collider. Two meshed gears therefore touch at one line and do not push each other |

Icon: two meshed gears with opposed arrows. It differs from the Motor's rotor and the Pulley by its teeth.

### Behaviour and numbers (S347; ratios by owner decision 8)

- **Ratio:** by tooth count (owner decision 8, 22 November 2026: "gear ratios follow tooth counts"). A 16-tooth gear driving a 32-tooth gear halves the speed, doubles the torque and reverses the direction. Adjacent gears reverse; a train of three restores the direction.
- **Engagement:** by connecting two adjacent gears, as a Mechanical link is connected, never by proximity alone. Moving a gear out of mesh distance in Build removes the mesh.
- **Loops:** a closed ring whose ratios multiply to +1 turns (an even number of gears with consistent tooth counts); any other ring (an odd number, or inconsistent ratios) cannot turn: it stays at rest and its source stalls. It creates no energy. Two sources into one train follow the existing conflicting-source rule.
- A mesh is bilateral: load on the driven gear is felt by the driver.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Continuous hinge with `DriveIn` and `Drive` rims, pass-through | present | `MechanicalLinkPresentationValues.cs:10` | `ConveyorTests.ChainedConveyors_DriveRelaysRotationToDownstreamConveyor` |
| Reversal parity through a coupling | present | `MechanicalContractValues.cs:258` | `ReverseTransmissionTests.EveryCurrentPairingCarriesCargoWithReversalParity` |
| A closed loop stays at rest; conflicting sources refused | present | same | `ClutchTests.RotatedMounts_PreserveDomainsAndSign_ClosedLoopStaysAtRest_AndDuplicateDriveInRejected`; `ReverseTransmissionTests.ConflictingSourcesAndWrongDirectionAreRefused` |
| Load felt upstream | present | same | `ReverseTransmissionLoadTests` (class) |
| A whole-number parameter driving a collider radius and a mass on a hinge's moving body | narrower | `ColliderParameterBinding` is documented for a bounded real parameter (`SceneGeometry.cs:26`), and the delivered bindings (Balloon, Pipe, Ramp, Tennis ball, Wall, Weight) do not target a hinge's moving body | Owner: `ENGINE-PARAMETER-BOUND-FRAMES` (parameter-bound sockets, values and colliders, coordinator decision 16); the slice's probe reads the validator first |
| A tooth count drawn to follow a whole-number parameter | narrower | the delivered parameter presentation is a scale (`PresentationProperty.UniformScale`, `elements/TennisBall/TennisBallElementDeclaration.cs:24`) | `ENGINE-PARAMETER-BOUND-FRAMES` (art present by parameter): a repeat count bound to a parameter, stated for any package |
| **A mesh link between two separately placed parts: reversed sign, ratio from declared tooth counts, adjacency checked at connection** | **missing** | a link between `Drive` and `DriveIn` is a Mechanical link with rim-radius ratio and same sign | Owner: `ENGINE-MECHANICAL-LINKS` (declared connection materials with a coupling law kind and a required engagement class; engine design audit §5 lists "mesh, tooth ratio" there) |
| **A port rim whose radius follows a parameter** | **missing** | `MechanicalRimDescriptor.Radius` is a constant `float` (`MechanicalLinkPresentationValues.cs:10`–`:11`) | `ENGINE-MECHANICAL-LINKS` declares a physical pitch radius on each mechanical port (batch J item 3 c); its binding to `teeth` is `ENGINE-PARAMETER-BOUND-FRAMES` |
| **A closed ring of meshes solved for consistency: it turns when its ratios multiply to +1 and rests otherwise** | **missing** | the cited test shows only a closed loop at rest | `ENGINE-MECHANICAL-LINKS` (the coupling law's consistency condition) |

Counts: 4 present, 0 untested, 2 narrower, 3 missing (two owning stories, each named per row).

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static bracket, one hinged wheel and two rim ports. A mesh is a connection, as a Mechanical link is: its ratio and sign come from the two ports' declared tooth counts through one coupling law, and the loop rule is that law's consistency condition. Load felt upstream is the same bilateral coupling. The missing capabilities are stated for any package: a mesh link between toothed rims and loop consistency, owned by `ENGINE-MECHANICAL-LINKS`; a rim, collider, mass and drawn repeat count bound to a whole-number parameter, owned by `ENGINE-PARAMETER-BOUND-FRAMES`. Nothing reads "gear" in the engine. No element code, callback or engine branch.
- **Three layers.** Colliders (physics): 1 box (bracket) and 1 cylinder (the pitch cylinder) as hidden `col_` proxies; teeth are drawn inside the cylinder and are not colliders, so meshed wheels touch along one line. Visuals: the art nodes, none a collider. Animation: none beyond the body pose. What moves each art node: the bracket nothing (static body 0); the toothed disc and hub the body 1 pose.
- **Flat state.** 2 bodies; 2 colliders (1 box, 1 cylinder); 1 revolute joint; 0 drives; 0 sensors; 0 nodes; 0 stores; 1 body-pose binding, 3 parameter bindings (collider radius, mass, tooth count).
- **Performance.** Two primitives a gear; a train of N gears is N hinges and N − 1 couplings in one island, with no tooth contacts to resolve. The bracket is static. Whether a stopped train may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-187](../requirements.md#todo-187): "Add rotational ports and visible meshing. Begin with a direction-reversal puzzle; prevent contradictory drive loops from creating energy. Ratios need original-game verification." [sequence-task-277](../requirements.md#sequence-task-277) adds "gears of each size" and asks which connection forms each part supports.
- **Campaign.** Chapter 4 "Gears of an Idea" (31–40), lesson "A turn for the better".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: speed ratio ±3 % once steady; "at rest" below 0.1 rad/s; stall guard 20 s.

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| N1 | Motor → 16-tooth gear meshed with a 16-tooth gear → Conveyor | The second gear turns the opposite way at the same speed and the Conveyor carries a ball the reversed way | Control: unmeshed, the second gear and Conveyor stay still | `GearTests.AdjacentGearsReverseDirection` |
| N2 | 16 driving 32; 32 driving 16 | Speed ratio 0.5 and 2 | Boundary: `teeth` 8 and 40 | `GearTests.ToothCountsSetTheRatio` |
| N3 | A train of three | The third turns the same way as the first | — | `GearTests.ThreeGearsRestoreDirection` |
| N4 | A ring of three meshed gears; a ring of four with consistent ratios | The ring of three stays at rest and the Motor stalls with no energy growth; the ring of four turns | — | `GearTests.AContradictoryLoopLocksWithoutEnergy` |
| N5 | A mesh requested with a 0.1 m gap, with a 0.01 m overlap, and with axes 30° apart | Refused with a typed reason | Boundary: a 0.03 m gap and exact contact are admitted | `GearTests.OnlyAdjacentParallelGearsMesh` |
| N6 | The driven gear's Conveyor loaded with a Cannonball | The Motor's speed drops (load felt upstream) | Control: unloaded | `GearTests.LoadIsFeltThroughTheMesh` |
| N7 | Gear to Motor by Mechanical link; gear to Conveyor by Mechanical link | Both work as ordinary Mechanical links with no reversal | — | `GearTests.AGearAlsoTakesAMechanicalLink` |
| N8 | Run, Reset; Save, Load | `teeth`, poses and meshes restored | — | `GearTests.ResetAndReloadRestoreTheTrain` |
| N9 | Electrical wire to a rim; `teeth` 7, 41, 16.5 | Refused with a typed reason | — | `GearTests.WrongKindWiresAndInvalidToothCountsAreRefused` |

**Integration.** Motor, Windmill, Conveyor, Clutch, Reverse transmission, Wound spring, Fan shaft mode (delivered); Mouse motor, Generator, Flywheel, Crank-slider, Rack slide (this programme).

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `gear`; Named identity EL-232; Acceptance todo-187 and sequence-task-365; Decision S347; Mode records `Configuration=Adjustable`. The ledger entry names owner S347 and `scope-corrections.md` has that anchor, so main's reader admits the identity; the section is still written only by the delivering slice, after the engine programme. Levels need S8.

**Delivery.** Prerequisites, in order: blocked on the engine programme (owner decision 3a), in particular `ENGINE-CAPABILITY-INSTANCES` and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-MECHANICAL-LINKS` (the mesh material and loop consistency) and `ENGINE-PARAMETER-BOUND-FRAMES` (the `teeth` bindings). One element slice. Files: the `elements/Gear/` package, `CuriousContraptions.tests/GearTests.cs`, and the hand-edited lists until S3 (S2 is published at `25df3cd5`; no test-project list edit). Review tier: engine. **Disqualified if** the engine story makes meshing automatic by proximity; then the Connections bullet and N5 are rewritten, and the pitch-cylinder colliders must not fight the coupling.

## 6. Open questions

1. *S347: ratios and engagement.* Resolved: ratios follow tooth counts (owner decision 8, 22 November 2026), which settles the requirement's "Ratios need original-game verification" clause (audit O2); engagement by an explicit connection checked for adjacency, under D16.
2. *Several sizes as several parts?* Resolved by D8: one entry with `teeth`.

Needs the owner: nothing.
