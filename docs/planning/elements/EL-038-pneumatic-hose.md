# EL-038 · Pneumatic hose — element readiness spec

**Current delivery (24 October 2026):** [EL-038 delivery slice](../invest/named-elements.md#element-038). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch H named-identity spec, brought to build-ready detail on 21 November 2026 against `8d1d68aa`. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations of the form `path:line` are this checkout. Values marked *proposed* are starting values (decision D1). Shared sealed-gas facts P1–P20, the family contract F1–F11 and the capability rows G1–G14 are in [EL-039](EL-039-air-reservoir.md). Repaired on 22 November 2026 against the Batch H audit.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-038 · Pneumatic hose |
| Type | Pneumatic |
| Anchor | [requirements.md#element-038](../requirements.md#element-038); scope index [todo-376](../requirements.md#todo-376); [named-elements entry](../invest/named-elements.md#element-038); owner S473 |
| Related | Refines no CAT spec. Connection-kind sibling of [EL-197 Electrical wire](EL-197-electrical-wire.md) and [EL-048 Acoustic duct](EL-048-acoustic-duct.md); joins [EL-037](EL-037-air-compressor.md), [EL-039](EL-039-air-reservoir.md), [EL-040](EL-040-pneumatic-release-valve.md), [EL-041](EL-041-pneumatic-directional-valve.md), [EL-042](EL-042-air-nozzle.md), [EL-043](EL-043-pneumatic-pressure-gauge.md) and [EL-045](EL-045-air-whistle.md). Not the [CAT-048](../requirements.md#current-cat-048) ball pipe. |
| Roadmap story | [EL-038 delivery slice](../invest/named-elements.md#element-038); canonical proof owner and prerequisite decisions are recorded there. |
| Status | not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` (the strand's contact candidates) and `ENGINE-ISLANDS-SLEEP` (a resting strand); then blocked on ENGINE-CONNECTION-MATERIALS (solid strand), owner decision 1; then `ENGINE-GAS-NETWORK` (owner decision 18). |
| Disposition | To deliver as a single identity that is a **connection kind, not a part**. It is not a duplicate: it has its own capacity, resistance and end boundary. |
| Planned package | None. A hose is a link of the Fluid domain between two declared ports, like a wire, rope or Mechanical link; it has no package id, directory, scene or palette row (not placeable: a connection kind; coordinator decision 36). The published coverage binding names a package, so it cannot bind this identity as written (section 7). |

## 2. Declaration

The hose is an authored typed connection. It starts at a gas Output port and ends at a gas Input port or at a declared free end. Closest delivered shape: the connection record itself, `WorkshopConnection` (`engine/gpu/WorkshopConnections.cs:31`), which today carries a source, an output port, a target, an input port, a domain and an optional rope length.

| Item | Declaration |
| --- | --- |
| Interfaces | None of its own: it is not a declaration. Each end is a port of a part that declares the fluid port contract (G5). |
| Bodies, colliders, mass | None of its own as a declaration: the hose is a solid strand (owner decision 1). Its collider is a property of its declared connection material, compiled at Run by `ENGINE-CONNECTION-MATERIALS`, never per-element code. The hose connection material declares diameter 0.05 m, linear density 0.3 kg/m *proposed*, friction 0.6 *proposed*, restitution 0.1 *proposed*, bounce threshold 0.1 m/s *proposed*, and the segment rule `ENGINE-CONNECTION-MATERIALS` defines; the engine derives the segment count from that rule (product constants are declaration data, F10 and D13). Blocked on ENGINE-CONNECTION-MATERIALS (solid strand). |
| Domain | Fluid (F3). The link is Output → Input. An Output takes several hoses, each its own line (F3, F11); only an identical duplicate link is refused, as for every delivered connection (`engine/gpu/WorkshopConnections.cs:132`–`:137`). |
| Capacity | Fixed line volume 4.0 × 10⁻⁴ m³ *proposed* (a 16 mm bore over a nominal 2 m; 4% of the default tank, so it is finite but small). It shares inventory with what it joins and adds capacity, never energy. |
| Resistance | Fixed effective throat area 2.0 × 10⁻⁴ m² *proposed* (the bore area). It equals the largest valve `opening` and nozzle `throat_area`, so a hose never throttles harder than the part it feeds, and it still makes equalisation take time. |
| Parameters | None on a hose instance (D13: capacity and size are fixed per kind). The earlier `length` parameter is withdrawn; the drawn length follows the two sockets. |
| Home of the constants | The line volume and throat area are declared values of the Fluid connection kind in the bundle, compiled to rows; the engine holds no hose constant (F10). The declaration format of a connection material (diameter, segment rule, linear density, contact material, line volume, throat area) is `ENGINE-CONNECTION-MATERIALS`'s (owner decision 1, coordinator decision 16); the line row that reads volume and throat is `ENGINE-GAS-NETWORK`'s. Belt and chain kinds are `ENGINE-MECHANICAL-LINKS` (owner decision 2), a separate mechanism. |
| End boundary | A connected end has none. A free end is `Vent` (discharges to ambient, F2) or `Sealed` (holds). Closed enum `LineEnd { Sealed, Vent }`. A free end is fixed at the authored bench point chosen with the Connect control; the strand's last segment is anchored there. A strand end fixed at an authored world point is a row of `ENGINE-CONNECTION-MATERIALS`. |
| Stores | The line volume above. At Run start and after Reset it holds gas at the pressure and temperature given by F9: the store on its Output end, else the store on its Input end, else ambient. |
| Presentation | A tube drawn along the committed poses of the strand's segments between the two sockets; no animation binding (gas-family table "None (static)", [finite-gas-foundation](../../finite-gas-foundation.md#sealed-gas)). A free end shows a cream cap (Sealed) or an open navy mouth (Vent) on the strand's last segment. |
| Palette | No row. The hose is drawn with the Connect control from a gas socket. Player text: "Air hose — joins air sockets. It carries air only between connected ends; an open free end vents, a capped one holds." |

**Variants** (from the outcome "Open or disconnected hose vents or isolates according to its boundary declaration"):

| Variant | Ends | Behaviour |
| --- | --- | --- |
| Connected | Output port → Input port | Flow from higher to lower pressure through the throat; equal pressures give no flow. |
| Free end, Vent | Output port → open end | Discharges to ambient until the source reaches ambient; never draws ambient air in (forward flow only, P6). |
| Free end, Sealed | Output port → capped end | The line starts at the source's pressure (F9) and holds it: nothing moves and nothing reaches ambient. |
| Removed | no hose | Each port falls back to its own declared boundary, Sealed for every pneumatic port (F4). |

### Art and solidity

| Node | Mesh and size | Class | Reason |
| --- | --- | --- | --- |
| Tube | Translucent cyan `#66b8c9` tube, 0.05 m diameter | solid | owner decision 1: a hose is a solid strand; its collider is the connection material's (diameter 0.05 m, `ENGINE-CONNECTION-MATERIALS`); the art follows the segments' committed poses |
| End fittings | Cream `#fff8e9` collars at each socket | decorative | drawn flush on the socket |
| Free-end cap or mouth | Cream cap or navy ring, inside the 0.05 m strand diameter | solid | part of the strand's last segment, within its collider; follows that segment's pose |

At a glance it differs from the navy electrical cable and the gold activation link by thickness and translucency ([DESIGN.md](../../../DESIGN.md) preservation check: "distinguish connection types through shape as well as colour"). No icon of its own.

### Behaviour and numbers

- **Law.** Nozzle-law flow (P6) through the fixed throat between the two joined stores; the line volume is part of the conserved network. The line's volume is joined to the store on its Output end without loss; its one throat is at its Input end (or at its free end). The settled values below do not depend on where the throat sits; H-A2's one-tick difference and H-A4's venting time do.
- **Checks (line start state F9).** A default tank (0.01 m³, 400 000 Pa) joined to an ambient default tank settles at 253 600 Pa: (4000 + 160 + 1013.25) J over 0.0204 m³; the 160 J is the line, which starts at 400 000 Pa. A default tank on a Sealed free end stays at 400 000 Pa. Finite capacity shows where the line is a large share of the network: a 0.002 m³ tank at 600 000 Pa joined to an ambient 0.002 m³ tank settles at 373 300 Pa, (1200 + 240 + 202.65) J over 0.0044 m³, where a hose with no volume would give 350 700 Pa. One tank feeding two ambient default tanks through two hoses on its `GasOut` settles at 206 100 Pa: (4000 + 320 + 2026.5) J over 0.0308 m³. None of these depends on temperature. At 400 000 Pa and the ambient temperature of F2 (288 K) the choked rate through the hose throat is 2.0 × 10⁻⁴ × 400 000 × √(1.4 ÷ (287 × 288)) × (2 ÷ 2.4)³ = 0.1905 kg/s. The gas step runs once per 1/480 s substep (EL-039 F5), so one substep moves at most 0.1905 ÷ 480 = 0.40 g of the 48.4 g charge (400 000 × 0.01 ÷ (287 × 288)), and one default 1/120 s tick at most four times that, 1.59 g, which lowers the tank by at most 0.00159 × 287 × 288 ÷ 0.01 = 13 100 Pa. Each path moves at most the amount that equalises the two volumes it joins, applied one path at a time in compiled order (the sequential clamp of F5): equalisation is visibly not instant.
- **Tick order.** None of its own; transfer happens in the gas step of each substep, first in the substep and before the liquid step, in the [one combined tick order](EL-176-red-laser-emitter.md#combined-tick-order) (EL-039 F5; coordinator decision 26, 23 November 2026). A solver trial replay does not repeat it.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-01.json), proof owner S473): EnvironmentState, FiniteLedger, FluidAdvection, GasState, GeometryQuery, JointConstraint, PressureWork, RigidBodyDynamics, TopologyTransaction (+ StateTransaction). Under D14 the row is navigation; this probe (21 November 2026, `8d1d68aa`) is the authority. Evidence for G rows is in EL-039 section 3.

| Capability | Mark | Evidence or owner |
| --- | --- | --- |
| Wrong-kind and Output-to-Output refusal; authored-state Reset and save/load (G12) | present | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`); `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| Fluid port contract (G5) | present, untested | `IFluidElement` (`CuriousContraptions.Abstractions.Fluid/FluidContracts.cs:5`) |
| Authored connection record | narrower | `WorkshopConnection` (`engine/gpu/WorkshopConnections.cs:31`) always has a target part and admits Activation, Electrical, Rope and Mechanical only (`:11`). It has no Fluid domain and no free-end target. Owner: `ENGINE-GAS-NETWORK` |
| Link drawn between two sockets from committed state | narrower | Only mechanical rims have a declared link route: `MechanicalLinkGeometry` (`CuriousContraptions.Abstractions/MechanicalLinkPresentationValues.cs:30`), `MechanicalLinkPresentationTests.DeclaredRimsProduceClosedTangentRoutesAndCommittedMarkers` (`CuriousContraptions.tests/MechanicalLinkPresentationTests.cs:29`). A hose tube is a strand drawn along its segments' committed poses. Owner: `ENGINE-CONNECTION-MATERIALS` |
| Gas connection kind and the gas `PortId` members (G6; role Bidirectional, no new `PortRole` member, EL-039 F3) | missing | `ENGINE-GAS-NETWORK` |
| Lines with volume, resistance and conserved transfer (G7) | missing | `ENGINE-GAS-NETWORK` |
| A line end with a declared boundary (Vent or Sealed) | missing | `ENGINE-GAS-NETWORK`; its text names "a closed valve or disconnected line" but not a vented open end. Gap recorded in the batch summary |
| Several links on one Output port (G13) | present, untested | `WorkshopConnections.ValidateIdentity` refuses only an identical link (`engine/gpu/WorkshopConnections.cs:132`–`:137`); no test was opened, and none can exist for Fluid before G6 |
| A connection kind with declared values (line volume, throat), the F9 start state and transfer clamped to equalisation (G14) | missing | `ENGINE-GAS-NETWORK`; proposed text in `readiness/batch-h.md` section 4 |
| A solid strand: a connection whose drawn strand collides, with its collider a property of the declared connection material (diameter, segment rule) and cost bounded by compiled declarations | missing | `ENGINE-CONNECTION-MATERIALS` (owner decision 1). The delivered rope draws a thick strand with no collider; that recorded visible-solid defect is owned by the same story |
| A strand end fixed at an authored world point (a free end's last segment anchored at the bench point chosen with the Connect control) | missing | `ENGINE-CONNECTION-MATERIALS` |

Counts: 1 present, 2 present but untested, 2 narrower, 6 missing. Nothing here needs element-specific engine code. Bellows: no `elements/Bellows` package and no `spec-12-4-pneumatic-bellows.md` exist in this checkout.

**Dependencies.** At least one gas store (a charged EL-039) and one consumer.

### Engine conformance (22 November 2026)

Checked against the owner's conformance instruction of 22 November 2026 and `_bmad/custom/element-free-engine-design.md`.

1. **Declaration data only.** A hose is a link record plus the declared values of its connection kind (F10). It has no package, script, callback or loop, and the engine may not branch on "hose": it sees a line row with a volume, a throat and two ends.
2. **Generic laws.** One law: nozzle-law transfer between two volumes through a declared throat, conserving mass and energy, clamped to equalisation (P6, F5). Vent and Sealed are the two values of a declared end boundary. The start state is F9. Every row of section 5 is a consequence of these and the declared numbers.
3. **Three layers.** *Colliders:* the strand's segment colliders, compiled from the declared connection material by `ENGINE-CONNECTION-MATERIALS` (owner decision 1); the hose declares none itself. *Visuals:* the tube and free-end cap or mouth follow the strand segments' committed poses (body pose); the collars are drawn flush on the sockets and moved by nothing; no art is a collider. *Animation:* none; no binding drives any hose art.
4. **One-way flow and typing.** The link is authored on the main thread and compiled at Run; the drawn tube reads socket positions and never writes to physics. Domain, port ids and `LineEnd` are enums; volume, area and pressure are f32.
5. **Flat-state counts per hose.** Declared by the hose: bodies 0; joints 0; drives 0; sensors 0; domain nodes 1 (one line row with its gas state); stores 1 (the line volume); bindings 0. Strand segments (bodies, colliders, joints) are compiled from the connection material by `ENGINE-CONNECTION-MATERIALS`, a fixed count per link at compile time. Fixed at compile time, because links cannot change during Run (P12).
6. **Performance fitness.** Constant gas work per line per substep (one row per store and per path, EL-039 F5). The strand's contact candidates come from `ENGINE-SPATIAL-QUERIES`; a resting strand is integrated every substep today, and sleeping is `ENGINE-ISLANDS-SLEEP`. Line tables are sized at Run from the compiled declarations, with no fixed scene ceiling (design contract D5); the row layout is `ENGINE-GAS-NETWORK`'s.
7. **Tests are scenarios.** H-A1 to H-A11 author constructions, run the production simulation and assert committed pressures, masses and refusals.

## 4. Sources and legacy

- Requirement row [element-038](../requirements.md#element-038): "Connects compatible gas ports with finite capacity and resistance"; outcome "Open or disconnected hose vents or isolates according to its boundary declaration". Integration task [todo-426](../requirements.md#todo-426) requires testing hose disconnection.
- Named entry [element-038](../invest/named-elements.md#element-038), owner S473. Gas design: "Pneumatic hose (EL-038) | Gas network node pair | length | Carries inventory between seated ports only | None (static)".

| # | Fact | Source | Disposition |
| --- | --- | --- | --- |
| H1 | A link resolves only with exactly one matching port at each end, the link's domain on both, output (or bidirectional) → input; undefined domains reject. | `engine/ConnectionPort.cs@a6c914e:L40-L67` | carry forward the rule for the Fluid domain |
| H2 | Legacy connection domains were Activation, Electrical, Signal, Mechanical and Rope, serialized snake_case; no gas domain existed. | `engine/MachineData.cs@a6c914e:L67-L70` | carry forward snake_case boundary naming; the gas link is new |
| H3 | Rope links carry an explicit length 0.05–200 m; other domains reject a length. | `engine/ConnectionPort.cs@a6c914e:L47-L52` | do not carry forward the rope bounds; the hose has no length value |
| H4 | Typed sockets require matching domain, direction and identity; a duplicate source port or missing socket id rejects. | `CuriousContraptions.tests/ConnectionPortTests.cs@a6c914e:L18-L38` | carry forward as acceptance facts |

- **Files harvested:** `engine/ConnectionPort.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/ConnectionPortTests.cs`.

## 5. Acceptance outline

Acceptance authority: [element-038](../requirements.md#element-038), [pneumatics profile](../invest/profiles.md#pneumatics). Tolerances and stall guards are EL-039 F6. Planned test class `PneumaticHoseTests`. Tanks are default Air tanks.

| # | Given / When / Then | Planned test | Control | Boundary |
| --- | --- | --- | --- | --- |
| H-A1 | Given a 400 000 Pa tank joined by a hose to an ambient tank, when Run advances until settled (stall guard 20 s), then both read 253 600 Pa and total gas mass (tanks and line) is unchanged | `PneumaticHoseTests.AConnectedHoseSharesGasBetweenItsEnds` | no hose: both tanks keep their pressure | equal pressures: no flow |
| H-A2 | Given the same pair, then after one tick the pressures still differ by more than 2000 Pa (finite resistance: one tick's four gas substeps move at most 1.59 g, lowering the charged tank by at most 13 100 Pa against a 298 700 Pa difference) | `PneumaticHoseTests.EqualisationIsNotInstant` | — | — |
| H-A3 | Finite capacity. Given a 0.002 m³ tank at 600 000 Pa joined by a hose to an ambient 0.002 m³ tank, then they settle at 373 300 Pa (a hose with no volume would give 350 700 Pa; the 22 600 Pa gap is three times the F6 tolerance of 7500 Pa). And given a 400 000 Pa tank on a Sealed free end, then it holds 400 000 Pa for 20 s and total mass is unchanged | `PneumaticHoseTests.TheLineAddsItsOwnVolumeAndASealedEndHolds` | H-A4 | — |
| H-A4 | Given a 400 000 Pa tank on a Vent free end, then within 20 s it reads ambient and never falls below it | `PneumaticHoseTests.AVentFreeEndEmptiesTheTankToAmbient` | H-A3 holds its charge | an ambient tank on a Vent end does not change |
| H-A5 | Given two hose pairs whose tubes lie across each other (solid strands touch, they do not pass through), one pair charged and one at ambient, then only the charged pair changes | `PneumaticHoseTests.CrossingHosesDoNotExchangeGas` | — | — |
| H-A6 | Given a hose removed in the build, then each tank holds its own charge (each port Sealed, F4) | `PneumaticHoseTests.ARemovedHoseIsolatesBothPorts` | H-A1 | — |
| H-A7 | Given Output → Output, or a gas port → an electrical or signal port, then each is refused with a typed reason | `PneumaticHoseTests.WrongKindAndReversedLinksAreRefused` | Output → Input is admitted; a second hose on the same Output is admitted (H-A11) | the identical link offered twice is refused |
| H-A8 | Given a run that moved gas, when Reset is pressed, then every store is at its authored state and every line at its F9 start state | `PneumaticHoseTests.ResetRestoresEveryStoreAndLine` | — | — |
| H-A9 | Given connected, Vent and Sealed hoses, when saved and loaded, then endpoints, socket names and end kinds are equal | `PneumaticHoseTests.SaveLoadKeepsEndpointsAndEndKinds` | — | an undefined end kind is refused |
| H-A10 | Later partner ([todo-426](../requirements.md#todo-426)): with the spring-return piston EL-230, a build without the hose never moves the piston | `SpringReturnPistonTests.AMissingHoseMovesNothing`, in the EL-230 slice | the connected build strokes | — |
| H-A11 | Tee. Given one 400 000 Pa tank whose `GasOut` carries two hoses to two ambient default tanks, then all three settle at 206 100 Pa and total mass is unchanged | `PneumaticHoseTests.TwoHosesOnOneOutletShareTheSameStore` | with one hose removed the other pair settles at 253 600 Pa and the third tank stays at ambient | — |
| H-A12 | Visible = solid (owner decisions 1 and 2; the same solid-strand capability as EL-200 B13). Given a hose lying across the bench between two tanks, its strand axis along X, and a default Ball (the Basketball, r 0.34 m, rolling resistance 0.035, `elements/Ball/BallElementDeclaration.cs:38`) released with its lowest point 0.5 m above the strand's top and its centre 0.05 m to the +Z side of the strand axis, then over the run the Ball's centre never comes nearer the axis of the nearest committed strand segment than 0.34 + 0.025 = 0.365 m (ball radius plus strand radius) less the contact slop, so it does not pass through: it rests on the strand or rolls off its side; within the 20 s stall guard it is at rest; the gas rows above are unchanged by the contact. (Passing through would bring the centre to 0.34 m above the bench, 0.315 m above the axis, so to within 0.315 m of it, 0.05 m inside the bound. With the 0.05 m offset the first contact is on an incline of asin(0.05 / 0.365) = 7.9°, four times the 2.0° (atan 0.035) the Ball's rolling resistance could hold, so it is expected to roll off to +Z and rest on the bench against the tube, its centre √(0.365² − 0.315²) = 0.18 m from the axis; the row does not assert the side, because the strand may also move under the blow.) | `PneumaticHoseTests.ABallIsDeflectedByTheHoseAndDoesNotPassThrough` | the Ball released 1 m to the +Z side of the hose reaches the bench without touching it | a Ball released the same way over a Vent free end's cap is likewise not passed through |

Generic cover today: G12 for the delivered domains only; every row above is new until the S4 conformance suite exists.

**Chrome recipe (pending the post-catalogue browser phase).** Charged Air tank and spring-return piston; select the tank, Connect `GasOut`, click the piston, choose `GasOut → GasIn`: a cyan hose appears between the actual sockets. For free ends, Connect `GasOut`, click empty bench and choose Vent or Sealed.

**Integrations.** Pneumatic integration task [sequence-task-393](../requirements.md#sequence-task-393); [todo-426](../requirements.md#todo-426) and [todo-427](../requirements.md#todo-427); combination [todo-378](../requirements.md#todo-378) "One Breath"; interaction processes [IX-08 fluid advection](../requirements.md#interaction-08) and [IX-40 gas state evolution](../requirements.md#interaction-40).

## 6. Open questions

1. **Connection or part.** Resolved under the standing delegation: a connection, drawn with the Connect control. Reason: the row says "connects compatible gas ports", and wires, ropes and Mechanical links are already connections; a second way to join ports would be a second rule.
2. **Runtime disconnection.** Resolved (D19): not during Run. Connections are fixed when Run starts (P12); "disconnected" is proven by removing the hose in the build (H-A6).
3. **Connection capacity.** Resolved: the harvest-era cap of 8 connections no longer exists; `WorkshopConnections` holds an unbounded list (`engine/gpu/WorkshopConnections.cs:59`).
4. **Length.** Resolved (D13): no length parameter; capacity and resistance are fixed per kind.
5. **Line start state.** Resolved by coordinator decision under the owner's standing delegation (22 November 2026), recorded in EL-039 as F9 and decision 5: the store on the Output end, else the store on the Input end, else ambient. The earlier text "restored to the pressure of its upstream store" and the ambient-line arithmetic contradicted each other; every value here is recomputed under F9.
6. **One hose per port.** Withdrawn (same decision, F3 and F11). It was a new rule with no code behind it, and it made a gauged tank feeding a consumer impossible to build.
7. **Home of the constants.** Resolved (F10): declared data of the connection kind, not engine constants.

8. **Solid or drawn-only tube.** Decided by owner decision 1, 22 November 2026: ropes, cables, hoses and ducts are solid. The hose tube and its free-end cap or mouth collide through the declared connection material, owned by `ENGINE-CONNECTION-MATERIALS`; the earlier owner item and its "no collider" default are withdrawn. Row H-A12 proves it.

Needs the owner: none.

## 7. Integration

- **Works with.** Every pneumatic part, EL-045 Air whistle and the pistons EL-230 and EL-231. None is delivered.
- **Port agreement.** Output → Input in the Fluid domain, with the port ids of F3.
- **Campaign.** Named in the pneumatic row of [campaign-element-coverage](../requirements.md#campaign-element-coverage): first use 71–80, reuse 91–100 and 136–150. It is first taught at level **71**, where the hose is taught before the Air reservoir ([EL-039](EL-039-air-reservoir.md)) (coordinator decision 37; [per-level allocation](readiness/campaign-coverage.md#6-per-level-allocation-and-progression-check-owner-decisions-24e-and-24f)). No entry in `content/puzzles.json` uses it today; a level that pre-places a hose needs the lesson-fixture story ([S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package)), which lands after the engine programme (owner decision 22), so such a level waits for it.
- **Coverage binding (planned, not yet live).** The package binding section that main's `tools/Coverage/NamedPackageSources.cs` reads, in the form published for EL-239, names a package in its first line, and this identity has none. **Blocked on the coverage follow-on story S7,** which must define how a connection kind is covered; until it does, coverage of EL-038 is the tests H-A1 to H-A11 cited from the ledger entry. This identity also lacks the form's other preconditions: its own co-anchored `todo-NNN` and `sequence-task-NNN` pair in `requirements.md` (the ledger entry links only the umbrella `todo-376`) and an `<a id="s473">` anchor for owner S473 in `docs/planning/invest/scope-corrections.md`.

## 8. Delivery

- **Slice shape.** A small slice of its own, directly after EL-039: tests, the ledger row and this spec's status. It adds no package.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES` and `ENGINE-ISLANDS-SLEEP`; then blocked on ENGINE-CONNECTION-MATERIALS (solid strand), owner decision 1; then `ENGINE-GAS-NETWORK`, including the free-end boundary; EL-039 Air reservoir (the only store to test with, so its slice is delivered first; delivery order is not lesson order: level 71 fixes two reservoirs as lesson fixtures under S8 so that the player places the hose first, coordinator decisions 37 and 41).
- **Files.** Adds `CuriousContraptions.tests/PneumaticHoseTests.cs` (compiled by the test project's one rule; no compile line). Edits the ledger entry's status and this spec. No `elements/` directory, no solution line. The connection-kind declaration itself is delivered by `ENGINE-GAS-NETWORK`.
- **Review tier.** Data-only if the slice contains only tests and ledger text; engine tier if it has to touch the connection record, the wire or shared presentation.
- **Disqualifiers.** A line that cannot end at a declared boundary, or a Connect control that cannot offer a free end (each returns the work to `ENGINE-GAS-NETWORK`); hose drawing that needs per-link code, a strand end that cannot be fixed at an authored point, or a hose strand without a collider (each returns it to `ENGINE-CONNECTION-MATERIALS`).
