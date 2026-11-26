# CAT-010 · bellows — element spec

**Current delivery (24 October 2026):** [CAT-010 delivery owner](../../../_bmad-output/planning-artifacts/epics.md#catalogue-slice-ownership). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes.

One current text, repaired 22 November 2026 after the Batch D audit, against checkout `8d1d68aa`. Citations without `@` are `path:line` in that checkout; citations with `@a6c914e` are the legacy harvest and appear only in the closing appendix. Values marked *proposed* are starting values ([D1](../invest/delivery-batches.md#3-family-level-decisions)).

**The slice is in flight in another session.** Story 12.4's contract `spec-12-4-pneumatic-bellows.md` and its prerequisite draft `spec-engine-axial-gas-nozzle.md` are in worktree `tim-12-4-bellows` (baseline `4f193bb2`, planning only, status blocked). This spec's rows are the acceptance authority; the 12.4 contract is re-pointed to them when it resumes. **This spec is blocked on the engine programme (owner decision 3a, 22 November 2026)** and then on `ENGINE-GAS-NETWORK`, which owns the axial gas chamber, the directional nozzle, prismatic directional damping and the prismatic stroke observation (engine design audit, section 6 item 13), on `ENGINE-CAPABILITY-INSTANCES` for the continuous prismatic-travel presentation driver (coordinator decision 29), and on `ENGINE-JOINT-KINDS` for the plate spring's declared rest position (section 4). The in-flight prerequisite draft's chamber and nozzle law is to be carried by `ENGINE-GAS-NETWORK`, not by a separately named slice. Several values below are defined only when that story's contract passes planning, and are marked so. Repaired 23 November 2026 after the second audit (`docs/planning/elements/readiness/audits/reaudit-group-1.md`, CAT-010).

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-010 · `bellows` |
| Requirement anchor | [CAT-010](../requirements.md#current-cat-010) with [sequence-task-312](../requirements.md#sequence-task-312); owner key [CAT-010-I](../invest/current-consumers.md#cat-010-i) |
| Disposition | To deliver as a single element (Story 12.4). No named identity maps to it; [EL-042 Air nozzle](EL-042-air-nozzle.md) is a distinct element |
| Planned package | id and catalogue key `bellows`, directory `elements/Bellows/`, category `ElementCategory.Power`. Palette order: 330 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md); 323 stays reserved while EL-223 is paused) |
| Capability row | [general-engine-element-map, CAT-010](../general-engine-element-map.md) |
| Status | In flight and blocked. No package exists. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` and `ENGINE-ISLANDS-SLEEP`; then `ENGINE-GAS-NETWORK` (chamber, nozzle, directional damping, stroke observation), `ENGINE-CAPABILITY-INSTANCES` (the continuous prismatic-travel presentation driver; coordinator decision 29) and `ENGINE-JOINT-KINDS` (the plate spring's declared rest position) (section 4) |

## 2. Declaration

Closest delivered shapes: the Springboard's sprung slider plate and the Balloon's gas store. Interfaces: `ISliderElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:15`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), and the axial chamber and nozzle interface that the gas prerequisite adds (none exists today).

| Item | Declaration |
| --- | --- |
| Body 0, static root | base box 1.5 × 0.16 × 1.2 m at (0, −0.4, 0); nozzle box 0.45 × 0.19 × 0.24 m at (0.73, −0.12, 0); lower fold box 1.2 × 0.31 × 0.92 m at (0, −0.165, 0) (*proposed*). Material restitution 0, friction 0.3, rolling resistance 0, threshold 0.1 m/s (*proposed*: as the Windmill's support, `elements/Windmill/WindmillElementDeclaration.cs:26`) |
| Body 1, dynamic plate | plate box 1.4 × 0.12 × 1.1 m, centre (0, 0.45, 0) at rest; upper fold box 1.2 × 0.40 × 0.92 m centred 0.26 m below the plate centre (*proposed*). Mass 0.5 kg; solid-box moments of the plate (0.051, 0.132, 0.082) kg·m²; material restitution 0, friction 0.3, rolling resistance 0, threshold 0.1 m/s |
| Slider | prismatic along local +Y, travel −0.4 to 0 m, collision between the two bodies disabled; spring 20 N/m whose declared rest position lies 0.4 m beyond the upper stop, so it pushes the plate against that stop with 20 × 0.4 = 8 N at travel 0 (the plate and upper fold weigh 0.5 × 9.81 = 4.9 N, so the plate rests on the stop; with no preload it would sag 4.9 ÷ 20 = 0.245 m of the 0.4 m travel); damping 4 N·s/m on outward motion and 0 on inward motion, both coefficients declared |
| Chamber and nozzle | one axial gas chamber on the slider coordinate (volume from travel); one directional nozzle at (0.96, −0.12, 0) along local +X with a compression-actuated outlet and a one-way ambient intake. The jet's cone is `reach` long with terminal radius `width`; `force` bounds the push on receivers. Units and ambient follow the pneumatic conventions ([EL-039](EL-039-air-reservoir.md) F1 and F2). The mapping from `force` to the nozzle's declared values, and all gas constants, are **defined when the `ENGINE-GAS-NETWORK` contract passes planning** and are set once in its Code Map. No flow value is derived from plate speed |
| Stroke observer | one occurrence per inward stroke, re-armed when the plate returns to rest; its event kind is added by `ENGINE-GAS-NETWORK` (the prismatic stroke observation), and its consumer is row B4's "one stroke" |
| Footprint | 2 bodies, 1 moving, 1 prismatic constraint. No ports, no electrical or signal node |

| Parameter | Kind | Unit | Min | Max | Default | Editor policy |
| --- | --- | --- | --- | --- | --- | --- |
| `force` | Real | N | smallest positive f32 | 40 | 18 | reject; build-only |
| `reach` | Real | m | smallest positive f32 | 12 | 5 | reject; build-only |
| `width` | Real | m | smallest positive f32 | 4 | 0.85 | reject; build-only |

`force` is (0, 40] as the legacy part's; `reach` and `width` have the Fan's ranges (`elements/Fan/FanElementDeclaration.cs:23`–`:24`). Presentation: `Visual/Plate` is the physical plate body; `Visual/Folds` scale Y = (0.71 + q) / 0.71 for committed travel q. Catalogue: title "Bellows"; description: "Press the spring-loaded top plate to squeeze a burst of air from the side nozzle. Heavy loads hold it down; lifting them lets it refill. Solid obstacles block the burst."

## 3. Art and solidity

| Node | Mesh and colour | Class | Moved by |
| --- | --- | --- | --- |
| `Base` | box, navy `#293954` | solid: box collider | nothing |
| `Nozzle` | box, gold `#e8b764` | solid: box collider | nothing |
| `Mouth` | navy disc r 0.095 m printed flush on the nozzle's end face | decorative: printed flat | nothing |
| `Plate` | box, cream `#fff8e9` | solid: box collider on the plate body | the plate body's pose |
| `Pad` | gold 0.7 × 0.35 m patch printed flush on the plate's top face | decorative: printed flat | the plate body's pose |
| `Folds` | five cyan `#66b8c9` folds with cream rims, all within 1.2 × 0.92 m | bound art inside the two fold colliders (see below) | the travel binding |

**How the folds stay solid.** The two fold boxes meet exactly at rest (0.31 + 0.40 = 0.71 m, the gap between base and plate) and overlap as the plate descends, so the drawn stack is inside a collider at every travel and no collider reaches beyond it. The delivered art test matches each collider to one solid or hollow art node and fails on any collider left over (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:237`–`:286`), so it cannot pass these two boxes as written. The slice therefore adds one general class to that matcher, stated without reference to any part: **bound art that stays inside its listed colliders at both ends of its binding's travel**; the listed colliders are then matched to that art and are not left over. The accordion is kept, as [DESIGN.md](../../../DESIGN.md) "Bellows" and the 12.4 contract's row B10 ask (this spec's resolution under the owner's standing delegation, 22 November 2026: a general matcher class, not a redrawn part). Icon: a folded pump with a side spout. At a glance it differs from the Springboard by the accordion and the side nozzle.

## 4. Engine capabilities

| Capability | Mark | Evidence or owner |
| --- | --- | --- |
| Slider with stops and a spring | present | `PrismaticSliderDescriptor` (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:104`); `SliderInstanceTests.ThePlateHoldsItsLowerAndUpperStops` (`CuriousContraptions.tests/SliderInstanceTests.cs:61`) |
| A slider spring with a declared rest position beyond its upper stop (the plate's 8 N preload) | missing | `PrismaticSliderDescriptor` declares stiffness, damping and starting compression only, with the spring's rest at travel 0 (`CuriousContraptions.Abstractions/MechanicalContractValues.cs:100`–`:114`). Owner: `ENGINE-JOINT-KINDS` (the declared spring rest position: owner decision 6, coordinator decisions 21 and 25) |
| Cone field, occlusion, shared receivers | present | `IAirflowSourceElement` (`MechanicalContracts.cs:40`); `ConicalAirflowTests.ConeHasOpenAxialAndRadialBoundaries` (`CuriousContraptions.tests/ConicalAirflowTests.cs:9`), `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`) |
| Rotor driving a Conveyor downstream through a Mechanical link | present | `WindmillIntegrationTests.PaidShaftTransportsRemoteCargoAndReverseStagesControlDirection` (`CuriousContraptions.tests/WindmillIntegrationTests.cs:79`) |
| Generic lifecycle | present | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| An axial gas chamber on a prismatic coordinate with a committed expansion-work read (row B13), a directional nozzle that emits with no receiver, and one-way ambient refill | missing | `ENGINE-GAS-NETWORK`, the one owner of the chamber and nozzle law (engine design audit, section 6 item 13; it also removes the sphere-only store coupling, audit E10). Today's gas store is an isobaric envelope only (`GasRuntimeTests.FiniteOutflowChangesOneMassAndShapeWithoutThrustOrChangingDrySpinInertia`, `CuriousContraptions.tests/GasRuntimeTests.cs:151`) |
| Slider damping declared per direction of travel | narrower | one coefficient per slider today: `ENGINE-GAS-NETWORK` (the prismatic directional damping it takes, audit section 6 item 13). Existing packages are edited to declare both coefficients; no defaulted second field |
| A repeatable stroke occurrence on a prismatic coordinate | narrower | `CoordinateTravelDescriptor` takes a revolute coordinate and fires once (`CuriousContraptions.Abstractions/CoordinateTravelValues.cs:13`–`:17`): `ENGINE-GAS-NETWORK` (the stroke observation it takes) |
| Continuous prismatic travel as a presentation driver | narrower | only the discrete `CommittedPrismaticPhase` exists (`CuriousContraptions.Abstractions/ElementPresentation.cs:31`). Owner: `ENGINE-CAPABILITY-INSTANCES` (coordinate drivers whatever the joint kind; coordinator decision 29). Element-free text: "a presentation driver reads a prismatic coordinate's committed travel as a continuous value" |

Counts: 4 present, 0 untested, 3 narrower, 2 missing. Three rows belong to `ENGINE-GAS-NETWORK`, the continuous travel driver to `ENGINE-CAPABILITY-INSTANCES` and the spring rest position to `ENGINE-JOINT-KINDS`; see the [batch summary](readiness/batch-d.md). The jet is paid by the chamber's gas; it is not a continuous-work input of the airflow source kind.

## 5. Behaviour and numbers

- **One jet model.** Finite gas compressed in the chamber leaves through the nozzle; that is the only source of the jet (requirement row: "The obsolete scripted stroke/ideal-force implementation is not the model"). The legacy plate-speed source is not part of this spec.
- **Parameters under that law.** `reach` and `width` are the cone's length and terminal radius, as on the Fan. `force` is the upper bound on the push the jet gives its receivers. How `force` sets the nozzle's declared values is defined when the gas prerequisite's contract passes planning.
- **Plate.** The spring pushes back with 8 + 20c N at compression c. Held compression is spring plus residual chamber pressure; the values are the engine slice's to state. A heavy striker (the Bowling ball) pumps and holds the plate down; a light one barely moves it. Row B4 asserts one stroke and no continuing flow, not a depth.
- **Held and refill.** The outlet opens only on inward motion, so a resting load emits nothing and refill is silent. Obstructed refill means a real contact limits the plate's return; removing it lets the plate finish.
- **Default 18 N** (*proposed*) is twice the Fan's 9 N because a burst is short. Whether it carries row B3's cargo is that slice's finding.
- No signal, optical or acoustic read is involved; the stroke occurrence is published at commit.

## 6. Acceptance

The 12.4 contract's rows with its planned tests. Stall guard: 10 s of game time for one impact and its transport, chosen from the longest chain (fall, burst, spin-up, belt travel of 1.5 m). "No emission" is exact zero outward mass. No held depth is asserted.

| Row | Given / When / Then | Planned test | Control and boundary |
| --- | --- | --- | --- |
| B1 | A Bowling ball falls on the plate; a Balloon, then a Tennis ball, crosses a target plane on the jet axis | `BellowsIntegrationTests.ImpactPropelsEachDeclaredReceiver` | missed strike; rotated part |
| B2 | The same compression with no receiver still discharges gas | `BellowsIntegrationTests.UnloadedCompressionActuallyDischarges` | uncompressed emits zero |
| B3 | Default Bellows, and a Windmill joined to a Conveyor by a Mechanical link: cargo starting at X = 1 passes X = 2.5 | `BellowsIntegrationTests.DefaultBurstTransportsBeltCargo` | miss, Wall in the jet, Mechanical link removed; default settings only |
| B4 | A held load, or two loads at once, give one stroke and no continuing flow | `BellowsIntegrationTests.HeldAndSharedLoadsDoNotRepeatEmission` | side and underside contacts emit zero |
| B5 | Refill is silent; an obstruction limits it; a second press counts again | `BellowsIntegrationTests.ObstructedReturnAndSecondPressRemainPhysical` | a new load meets the rising plate without passing through |
| B6 | Five orientations pump along local +X; carrier motion alone pumps nothing | `BellowsIntegrationTests.LocalCompressionIsCarrierIndependent` | moving and turning carrier |
| B7 | Several receivers share one burst; a blocked one gets none | `BellowsIntegrationTests.OneBurstPaysEveryExposedBranch` | exhausted chamber |
| B8 | `force` 0 or 41, `reach` −1, `width` 5 and non-finite values are refused atomically | `BellowsAdmissionTests.InvalidInputsPreserveInstalledConstruction` | the admitted end points |
| B9 | Reset restores poses, the rested plate and zero strokes; save then load replays | `BellowsIntegrationTests.AuthoredResetAndFreshLoadReplay`, plus the generic lifecycle test | pause, step, resume |
| B10 | Plate and folds follow the committed travel; removing art changes no physics | `BellowsArtworkTests.CommittedPlateAndFoldsSharePhysicalTravel` | both ends of travel |
| B11 | The burst rings Wind chimes; a Wall silences them | `ChimesIntegrationTests.BellowsBurstRingsOnlyClearReceiver`, run in Story 14.4 | blocked path |
| B12 | Every drawn solid has its collider, and the folds stay inside their two colliders at both ends of travel | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` with a `bellows` row and the new bound-art class; a control fixture whose bound art leaves its collider fails | both ends of travel |
| B13 | One Bowling-ball strike on the plate: the receivers' total work over the burst ≤ the chamber's committed expansion work ≤ the striker's lost kinetic plus potential energy + 0.5 J (conserved gas and nozzle transfer, physical work) | `BellowsIntegrationTests.ABurstNeverPaysMoreThanTheStrike` | no strike: zero chamber work and zero receiver work |

Every criterion of the requirement row maps to a row: explicit force, reach and width (B8); plate, collider and folds agree (B10, B12); side and miss (B4, B6); held compression (B4); obstructed refill and silent return (B5); repeated cycles (B5); blocked jet (B3, B7); default-distance rotor (B3) and chime (B11); removed Mechanical link (B3); conserved gas and nozzle transfer and physical work (B13; `current-cat-010` and `sequence-task-312`). There is no port, so no wrong-kind connection case. Chrome recipe: post-catalogue.

## 7. Integration

Works with Bowling ball, Basketball, Tennis ball, Balloon, Windmill, Conveyor, Wall (all delivered) and Wind chimes ([CAT-069](CAT-069-wind_chimes.md), planned later; B11 waits for it). Campaign: first use in levels 71–80, reuse 91–100 and 136–150 ([campaign reservations](../invest/campaign-reservations.md)); no level places it today. Coverage binding: catalogue key `bellows` in `docs/coverage/catalogue-elements.json`. Lesson-fixture story (S8) only where a level pre-places it.

**A later slice, not Story 12.4.** Charging an air reservoir from the Bellows ([sequence-task-393](../requirements.md#sequence-task-393); [EL-039](EL-039-air-reservoir.md)) needs a port this declaration does not have. The first `ENGINE-GAS-NETWORK` consumer slice adds a `GasOut` Fluid output on the chamber.

## 8. Delivery

- **Shape.** One element slice, already started; under AGENTS.md it finishes under the rules it started with (all six review layers).
- **Prerequisites, in order.** Registration derivation (published, `06ac9aa2`); blocked on the engine programme (owner decision 3a, 22 November 2026), which precedes every further element, in particular `ENGINE-SPATIAL-QUERIES` (airflow occlusion candidates), `ENGINE-ISLANDS-SLEEP` (continuous-work trials confined to the affected island); then `ENGINE-GAS-NETWORK` with the chamber, nozzle, directional damping and stroke observation, **blocked until the in-flight draft's two open design answers (outlet control and inlet law) are approved**; `ENGINE-CAPABILITY-INSTANCES` with the continuous prismatic-travel presentation driver (coordinator decision 29); `ENGINE-JOINT-KINDS` with the declared spring rest position; then Bellows replanned from their published baseline. The in-flight 12.4 contract must be re-pointed to these names when it resumes.
- **Files.** Adds `elements/Bellows/` (`Bellows.csproj`, `BellowsElementDeclaration.cs`, `bellows.tscn`, `icon.svg`) and `BellowsIntegrationTests.cs`, `BellowsAdmissionTests.cs`, `BellowsArtworkTests.cs` under `CuriousContraptions.tests/`; one line in `CuriousContraptions.slnx`; in `VisibleSolidColliderTests.cs` the bound-art matcher class, its control and one table row. Registry, bundle, palette, capacity and impact membership are derived ([registration derivation](../../../_bmad-output/implementation-artifacts/spec-registration-derivation.md)); until [S3](../../../_bmad-output/implementation-artifacts/spec-s3-remaining-per-package-lists.md) lands it also edits that contract's five test-side lists.
- **Disqualified if** replanning finds that the chamber or nozzle law needs the package's identity, or that the default burst cannot carry B3's cargo without changing another part's settings. The first is an engine finding; the second is a gameplay finding for the owner of the 12.4 contract.

## 9. Open questions

None open in this spec; two are open in the gas prerequisite (outlet control, inlet law) and block it. Resolved:

1. Model: finite gas and a nozzle, one model only (requirement row; this spec's resolution under the owner's standing delegation, 22 November 2026: the nozzle law of `ENGINE-GAS-NETWORK`, because two jet models in one declaration are two rules for one behaviour).
2. Receivers: both are required, a Balloon or Tennis ball (B1) and the rotor and belt (B3); chimes in Story 14.4 (B11).
3. Striker: the Bowling ball.
4. Obstructed refill: a real contact limits the return (B5).
5. Story order: after Windmill and Conveyor.
6. Folds: two box colliders and a general bound-art matcher class (section 3).
7. Pad and mouth printed flush; root restitution 0 (a pump base should not bounce cargo).

## Engine conformance

Against the owner's conformance addendum of 22 November 2026 (element-free engine gate in [AGENTS.md](../../../AGENTS.md); design contracts D1–D7 in `_bmad/custom/element-free-engine-design.md`). **Verdict: conforms on paper; buildable only after the engine programme, `ENGINE-GAS-NETWORK`, `ENGINE-CAPABILITY-INSTANCES` (the travel presentation driver) and `ENGINE-JOINT-KINDS` (the spring rest position).**

1. **Declaration data only.** A declaration class, a scene and an icon. The spring, both damping coefficients, the chamber, the nozzle and the stroke observer are each one declared row. There is no phase machine and no per-part update: "held" and "refilling" are what the slider and the outlet rule do.
2. **Generic laws.** Plate motion: the prismatic joint law with declared stops, spring and damping, and ordinary contact. Pressure, discharge and refill: the axial gas chamber and nozzle law on that joint's coordinate, a general capability any package can declare. Push on receivers: the conical airflow field. "One stroke": a prismatic stroke observation with a declared re-arm.
3. **Layers.** Colliders: five hidden box proxies (base, nozzle, lower fold on the root; plate, upper fold on the plate body). Visuals: the nodes of section 3; none is a collider. Animation: one binding scales `Visual/Folds` from committed travel and writes nothing back.
4. **One-way flow and typing.** The folds read committed travel; physics never reads them. Parameters are f32 with typed units.
5. **Flat state.** 2 bodies (1 static, 1 dynamic); 5 box colliders; 1 prismatic joint with one spring row (with a declared rest position) and two damping coefficients; 0 drives; 1 gas chamber store; 1 nozzle; 1 stroke observer; 0 ports and 0 signal or supply nodes; 1 event kind (added by its engine slice); 1 presentation binding and 1 body-pose mapping. All fixed at compile time.
6. **Performance.** Five boxes; the two fold boxes exist so the drawn stack is solid, not for looks. The cone is bounded (reach at most 12 m, terminal radius at most 4 m); its occlusion candidates come from `ENGINE-SPATIAL-QUERIES`. A heavy ball arriving fast on a 0.12 m plate relies on the engine's speculative contacts (Story 1.5). Island sleeping is not implemented (`docs/gpu-f32-physics.md:292` describes it); a resting plate is still stepped. Owned by `ENGINE-ISLANDS-SLEEP`; nothing in this part works around its absence. The gas law's cost per chamber is `ENGINE-GAS-NETWORK`'s to state.
7. **Tests are scenarios.** Rows B1–B12 run the production simulation; the prerequisite slices prove their laws on declarations that are not this package.

## Appendix: Historical evidence

Evidence of the legacy part at `a6c914e`, kept so the harvest stays traceable. Nothing here is a requirement, a tolerance or a design instruction; sections 1–9 are the spec. The legacy part used a plate-speed force source, which is not the model; legacy tick counts and error bounds in the cited files are history only.

| # | Fact | Citation |
| --- | --- | --- |
| 1 | Legacy geometry, mass, joint, spring, damping and nozzle position (values kept are in section 2) | `parts/BellowsPart.cs@a6c914e:L51-L106`, `L162-L180`; `parts/catalog/bellows.tres@a6c914e:L6-L12` |
| 2 | The part re-applied its spring and damper every substep and kept a phase machine in code: not the design | `parts/BellowsPart.cs@a6c914e:L118-L155` |
| 3 | Emission diagnostics were derived from compression, not from committed transfer: not the design | `parts/BellowsPart.cs@a6c914e:L133-L137` |
| 4 | The plate stayed within its travel and its art matched the solved plate (B10) | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L58-L80` |
| 5 | Chain: striker, bellows, windmill, conveyor, cargo; missed, blocked and disconnected controls; Reset (B3, B9) | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L82-L254` |
| 6 | A heavy striker bottomed and held the plate | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L132-L136` |
| 7 | A resting load held the plate; pushing it off let the plate refill with no emission; a second press counted again (B4, B5) | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L256-L301` |
| 8 | A top impact worked in five orientations; emission along local X (B6) | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L303-L336` |
| 9 | No energy was created | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L338-L370` |
| 10 | Only a top inward stroke pumped (B4) | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L372-L397` |
| 11 | Two simultaneous loads counted one stroke (B4) | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L399-L420` |
| 12 | Reset, save and load, and render calls did not change physics (B9, B10) | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L422-L453` |
| 13 | The burst rang wind chimes; a Wall silenced them (B11) | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L455-L490` |
| 14 | A second load met the returning plate without passing through (B5) | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L492-L518` |
| 15 | Out-of-range and non-finite parameters were refused (B8) | `CuriousContraptions.tests/BellowsTests.cs@a6c914e:L520-L548` |
| 16 | Receivers took work only while the plate compressed (B2, B7) | `CuriousContraptions.tests/LinearAirflowSupplyTests.cs@a6c914e:L77-L120`; `CuriousContraptions.tests/AirflowConservationTests.cs@a6c914e:L13-L106` |
| 17 | A failed substep restored stroke history: a faulting-tick mechanism, not carried | `CuriousContraptions.tests/MechanicalRuntimeCheckpointTests.cs@a6c914e:L34-L76` |
| 18 | No level used the bellows | `docs/coverage/catalogue-elements.json` (empty fixture list) |
| 19 | Outward or zero force supplied nothing; disabled participation stopped transfer (B2) | `CuriousContraptions.tests/CompressionTransferSupplyTests.cs@a6c914e:L11-L56` |
| 20 | Compression was measured relative to the carrier, so carrier motion alone supplied nothing (B6) | `CuriousContraptions.tests/CompressionTransferSupplyTests.cs@a6c914e:L58-L90` |
| 21 | Non-finite or negative ratings and a zero port direction were refused | `CuriousContraptions.tests/CompressionTransferSupplyTests.cs@a6c914e:L92-L111` |
| 22 | An undefined participation value and a missing source or plate collider were refused | `CuriousContraptions.tests/CompressionTransferSupplyTests.cs@a6c914e:L113-L138` |

**Files harvested:**
- `parts/BellowsPart.cs`
- `parts/catalog/bellows.tres`
- `parts/scenes/bellows.tscn` (no element knowledge: script reference only)
- `reference/cpu/MachinePart.cs`
- `engine/PartArt.cs` (ring half-width convention)
- `engine/physics/PersistentContactPair.cs`
- `CuriousContraptions.tests/BellowsTests.cs`
- `CuriousContraptions.tests/LinearAirflowSupplyTests.cs`
- `CuriousContraptions.tests/AirflowConservationTests.cs`
- `CuriousContraptions.tests/MechanicalRuntimeCheckpointTests.cs`
- `CuriousContraptions.tests/CompressionTransferSupplyTests.cs`

Consulted, no bellows-specific knowledge (an unused sealed-gas and nozzle foundation at the baseline): `engine/physics/AdiabaticGasDischarge.cs`, `engine/physics/AxialGasGeometry.cs`, `engine/physics/AxialGasLoad.cs`, `engine/physics/AxialGasPotential.cs`, `engine/physics/ConvergingGasNozzle.cs`, `engine/physics/GasNozzleTransit.cs`, `engine/physics/GasPredictionWork.cs`, `CuriousContraptions.tests/AdiabaticGasDischargeTests.cs`, `CuriousContraptions.tests/AxialGasPotentialTests.cs`, `CuriousContraptions.tests/ConvergingGasNozzleTests.cs`, `CuriousContraptions.tests/GasNozzleTransitTests.cs`, `CuriousContraptions.tests/PhysicsGasNodeTests.cs`.
