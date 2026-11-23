# EL-067 · Steel cable declaration readiness spec

**Current delivery (24 October 2026):** [EL-067 delivery slice](../invest/named-elements.md#element-067). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise. Sections 1 to 3 and 5 to 8 were rewritten on 21 November 2026 against published main `8d1d68aa`; their `path:line` citations are to that checkout.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-067 |
| Name | Steel cable |
| Type | Mechanical (connector) |
| Requirement anchor | [element-067](../requirements.md#element-067); scope index [todo-210](../requirements.md#todo-210) (retained research reference: the TIM2 manual) |
| Named entry | [named-elements.md#element-067](../invest/named-elements.md#element-067); proof owner S336 |
| CAT spec refined or extended | Extends the rope connector law carried in [CAT-058 rope anchor](CAT-058-rope_anchor.md) (EL-205 rope facts R3–R22) with a second connector material. Related: [CAT-053 pulley](CAT-053-pulley.md), [CAT-067 weight](CAT-067-weight.md) |
| Related identities | EL-205 rope (Batch J), [EL-068 tin snips](EL-068-tin-snips.md) (can cut it), [EL-066 scissors](EL-066-scissors.md) (cannot), [EL-063 cable winch](EL-063-cable-winch.md), [EL-064 metal loop anchor](EL-064-metal-loop-anchor.md) |
| Roadmap story | [EL-067 delivery slice](../invest/named-elements.md#element-067); canonical proof owner and prerequisite decisions are recorded there. |
| Status | Not started; blocked on the engine programme (owner decision 3a), relying most on ENGINE-SPATIAL-QUERIES (strand colliders join the one query interface); then blocked on ENGINE-CONNECTION-MATERIALS (solid strand) for the material and its collider, and on ENGINE-TENSILE-EXTENSIONS (cutting clause) for the cutting rows; then S7. No connection material of any kind exists in the code today |
| Disposition | Member of the Family slice "Rope materials" with EL-205 Rope: two connection materials that differ only in declared values and strand art. It is a **connection material, not a placed part** |
| Planned package | `steel_cable`, directory `elements/SteelCable/`, class `SteelCableElementDeclaration`. It is declared through a non-placeable bundle entry kind (no body, scene, palette entry or ports) that ENGINE-CONNECTION-MATERIALS must add; no such kind exists today. No palette entry |
| Coverage binding | Planned, not yet live (section 7); blocked on the coverage follow-on story S7, and on the material entry being in the generated bundle so the binding admits it |
| delivery-batches class | 1b, family "Rope materials", "candidates today" (`docs/planning/invest/delivery-batches.md:29`). **Corrected by code:** the family is right, "today" is not. The second connector material the row asks the probe to settle does not exist, so the family opens only after ENGINE-CONNECTION-MATERIALS and belongs in 1c |

**How a rope is made and cut today.** A rope is a `WorkshopConnection` in the Rope domain between two Tie ports, carrying only a measured length (`engine/gpu/WorkshopConnections.cs:31`, `:41`–`:55`). It is cut by the player's `CutRope` command (`engine/gpu/WorkshopSimulation.cs:11`; the Run-time "Cut rope" button, `ui/WorkshopConnections.cs:73`). That control is kept (owner decision, 22 October 2026: Story 10.2 keeps runtime rope cutting) and cuts a cable as it cuts a rope; a level may withhold it as authored level data (coordinator decision 24l, 23 November 2026). There is no second connection kind and no material field: `WorkshopConnectionDomain` is `{ Activation, Electrical, Rope, Mechanical }` (`WorkshopConnections.cs:11`).

## 2. Declaration

Nearest delivered shape: none. No delivered package declares a connection. The values below are what the package states once the engine capability of section 3 gives it somewhere to state them.

**Interfaces.** The connection-material declaration interface that ENGINE-CONNECTION-MATERIALS adds. No body, mass frame, hinge, port, store or parameter of its own; the strand collider is generated from the material's declared diameter and segment rule (owner decision 1).

**Connection material record.**

| Field | Value | Unit | Reason |
| --- | --- | --- | --- |
| Id | `steel_cable` | typed id | Extensible identity, not an engine enum: the engine must not name a material |
| Display name | "Steel cable" | — | |
| Domain | Rope | `SimulationDomain` | Same domain and the same Tie ports as rope; no new domain or role |
| Span law | tension only, inextensible | — | The delivered span law |
| Stiffness (natural frequency of the length row) | 60 | Hz | The stiffest value the solver admits: the delivered span uses the contact regularisation (`CuriousContraptions.Simulation/WorkshopConstraintSolver.cs:38`, applied at `CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:988`) |
| Damping ratio | 10 | — | `WorkshopConstraintSolver.cs:39`; no bounce on a sudden load |
| Cutting resistance R | 25 | J | Above the scissors' 4 J capacity and inside the tin snips' 5–40 J range |
| Strand diameter d | 0.04 | m | The engagement distance of the cutting contract ([EL-066](EL-066-scissors.md) section 2); equal to the drawn strand |
| Length | 0.05–200, measured at connect | m | The delivered rule (`CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:33`) |
| Mass | none | — | The delivered span law is massless ([CAT-058](CAT-058-rope_anchor.md) entry decision 3) |
| Collider | solid strand of diameter d along the committed span, by the material's declared segment rule | — | Visible = solid: a cable is thick and therefore solid (owner decision 1, 22 November 2026). The segment rule and its cost bound are ENGINE-CONNECTION-MATERIALS' (solid strand); no per-element code |

**Ports.** None of its own. Both ends use existing `Tie` ports (`PortRole.Tie`, `TieA`, `TieB`; Rope; Bidirectional), with the roles unchanged: `Anchor` on fixed parts, `Load` on dynamic bodies, `Guide` on pulleys (`CuriousContraptions.Abstractions/PortDescriptor.cs:74`). The material is a field of the connection, so every Tie port admits it.

**Route rule.** One material per route: a route is one continuous strand over its guides and is cut as a whole (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.cs:388`). A connection whose material differs from the route it would join is refused.

**Choosing it.** When the player connects two Rope-domain ports the connection UI offers the declared materials; rope is the default. There is no per-level allowance: connections are unlimited today (`WorkshopConnections.cs:34`–`:35`).

**Presentation.** The connection presentation draws the strand from the committed span read, as it draws rope: straight when taut, a length-matched curve when slack, dashed while the route is incomplete (`DESIGN.md:343`).

### Art and solidity

| Art | Description | Class |
| --- | --- | --- |
| Strand | pale grey (metal detail, `DESIGN.md:248`), drawn radius 0.02 m, with navy twist marks along it | Connection presentation, not package scene art. Solid: the drawn strand matches the material's strand collider (d = 0.04 m), owner decision 1; blocked on ENGINE-CONNECTION-MATERIALS (solid strand) |
| End ferrules | gold, in place of rope's gold knots | as the strand |

No icon in the palette. In the connection chooser its swatch is a grey twisted strand. At a glance it differs from rope by its navy twist marks and gold ferrules, not by colour alone; both strands are drawn at their declared 0.04 m.

### Behaviour and numbers

- **Tension only.** A taut cable resists lengthening; a slack cable applies no force. "Compression cannot push a load" is the delivered span law: the length row is inactive below the maximum length.
- **Stretch.** Under a hung 8 kg Weight from a fixed anchor the steady stretch is g/ω² = 9.81 / (2π·60)² ≈ 0.07 mm. A cable is inextensible for play.
- **Same stiffness as today's rope.** The delivered rope already runs at the solver's stiffest setting, so the cable cannot be stiffer. The requirement asks that stiffness be a declared material property, and it is. Whether rope becomes visibly more elastic is EL-205's decision; the earlier draft's "less stretch than rope" acceptance is withdrawn unless EL-205 softens rope.
- **Cutting.** The cutting contract is stated once, in [EL-066](EL-066-scissors.md) section 2, and cited here unchanged. This material contributes d = 0.04 m and R = 25 J, so it resists a closing blade with 625 N at the crossing point:

| | Rope, d 0.04 m, R = 1 J (25 N) | Steel cable, d 0.04 m, R = 25 J (625 N) |
| --- | --- | --- |
| Scissors, C = 4 J | cut when the delivered work reaches 1 J | never cut |
| Tin snips, C = `cut_work` 5–40 J, default 30 | always cut | cut when `cut_work` ≥ 25 J and the crossing is within 0.154 m of the pivot ([EL-068](EL-068-tin-snips.md)) |

- **Loads.** A cable carries any load the solver's span carries; there is no breaking strength. A load limit would be the separate identity EL-159 under `ENGINE-FRACTURE`.
- **Winch.** The EL-063 winch winds either material; its capacity (6 m) is the winch's value, not the material's.

## 3. Engine capabilities (capability probe)

| Capability | Status | Symbol | Exercising test, or owner |
| --- | --- | --- | --- |
| Tension-only span between Tie ports; a complete route holds, an unconnected load falls | *present* | `TensileSpanDeclaration` `CuriousContraptions.Abstractions/TensileSpanDeclarationValues.cs:25` | `CuriousContraptions.tests/RopeTests.cs:21` |
| A slack span does not accelerate its load | *present* | same | `CuriousContraptions.tests/RopeTests.cs:34` |
| Guides, routes over pulleys, branch and cycle refusal | *present* | `engine/gpu/RopeTopology.cs:75` | `CuriousContraptions.tests/RopeGuideTests.cs:109`; `CuriousContraptions.tests/RopeTests.cs:230`, `:949` |
| Connect-time length, 0.05–200 m, kept across moves | *present* | `WorkshopConnections.MeasureRope` `engine/gpu/WorkshopConnections.cs:41` | `CuriousContraptions.tests/RopeTests.cs:343` |
| Committed Open, Slack, Taut and Intact, Cut read; strand artwork from it | *present* | `RopeState`, `RopeIntegrity` `TensileSpanDeclarationValues.cs:9`–`:10` | `CuriousContraptions.tests/RopeTests.cs:217`, `:647` |
| **A declared connection material**: declared by a non-placeable bundle entry kind (no body, scene, palette entry or ports), admitted by `BundleValidator`, excluded from the palette and from every per-package placement suite, present in the generated bundle; a typed id with span stiffness and damping, strand diameter, cutting resistance and strand art; chosen in the connection UI with a declared default; carried on `WorkshopConnection`, the wire, the save and the span read; one material per route; an unknown id refused | *missing* | none: `WorkshopConnection` is (source, output, target, input, domain, rope length) (`WorkshopConnections.cs:31`); every package today is a placeable part with at least one body, the validator raises `BodyWithoutCollider`, and `CuriousContraptions.tests/CatalogueIntegrationTests.cs:273` places one instance per admitted declaration | ENGINE-CONNECTION-MATERIALS, its own story and not part of `ENGINE-ASSEMBLY-JOINTS` (it touches bundle schema, wire, save and connection UI and is unrelated to player-built joints). Story text is in the batch summary |
| **A solid strand**: the collider of a span is a property of its declared material (diameter, segment rule), compiled from declarations with a bounded cost, its candidates from the one spatial query interface | *missing* (the delivered rope is drawn with no collider: a recorded visible-solid defect owned by the same story) | none | ENGINE-CONNECTION-MATERIALS (solid strand), owner decision 1 and coordinator decision 16 |
| A material's strand diameter and cutting resistance read by a cutter | *missing* | none | ENGINE-TENSILE-EXTENSIONS, cutting clause (the one cutting law, [EL-066](EL-066-scissors.md) sections 2 and 3; coordinator decision 16) |

**Gaps this identity exposes.**

- **Coverage.** A material entry has no scene or palette entry. Published main admits a named package from the generated bundle's membership (`tools/Coverage/NamedPackageSources.cs`, "membership still comes from the generated bundle"), so ENGINE-CONNECTION-MATERIALS must put material entries in that bundle; the coverage follow-on story S7 then binds it like any other named identity.
- **The delivered rope.** When connections gain a material, the delivered rope becomes the first declared material in the same change (forward-only: no default fill and no migration), and gains its solid strand there. EL-205's disposition is "declared rope material" (R = 1 J, d = 0.04 m), a Family slice member with this identity; [EL-205](EL-205-rope.md) now states the same, so the earlier conflict with Batch J's closure is settled.

### Engine conformance

- **Declaration data only.** One material record: typed id, span stiffness and damping, strand diameter, cutting resistance, strand art. The engine names no material; it reads the record on the connection. No code of this identity runs.
- **Generic laws.** Holding and slack are the delivered tension-only span law with the record's stiffness and damping; cutting is the cutting contract reading d and R.
- **Layers.** Colliders: the solid strand generated per span from the declared diameter and segment rule (owner decision 1); no package-declared collider. Visuals: the strand, drawn by the connection presentation from the committed span read, never used as a collider; its drawn radius equals half the declared d, so what is seen is what collides. Animation: the strand art follows the committed span read (taut, slack, open, cut); nothing is written back.
- **One-way flow and typing.** The material is a typed id carried on the connection, wire and save; an unknown id is refused at the serialisation boundary. Values are f32.
- **Flat state.** 0 package bodies, 0 package colliders, 0 joints, 0 drives, 0 sensors, 0 stores, 0 bindings; one row in the bundle's material table, one material id per tensile route in the compiled span table, and per span the strand colliders the segment rule fixes at compile.
- **Performance.** The span solver reads two constants per span. The strand colliders are bounded by the compiled declarations and found through ENGINE-SPATIAL-QUERIES like any collider; nothing scans the scene. The story states their cost on its reference scene (owner decision 3a performance criterion); no budget is invented here.
- **Tests are scenarios.** Rows K1 to K11 place delivered parts and connect them; none needs a cable-specific path, and row T4 of EL-068 proves a third material is treated by its numbers alone.

**Element dependencies.** Delivered: Rope anchor, Weight, Pulley, Impact lever, Balloon (every part with a Tie). Not delivered: EL-205 rope material (the family sibling), EL-066, EL-068, EL-063.

## 4. Sources and legacy

- Requirements row: "Compression cannot push a load." No variants.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Rope supplies tension only; no equality row; inactive while current length < max − tolerance. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | carry forward (law) | Exactly the row outcome. |
| 2 | Rope link length finite, 0.05–200 m; non-rope links carry no length. | `engine/ConnectionPort.cs@a6c914e:L42-L66` | carry forward | Admission. |
| 3 | Rope state enum {Open, Slack, Taut}. | `engine/ConnectionPort.cs@a6c914e:L12-L12` | carry forward | Typed state. |
| 4 | A slack rope does not push: an upward 1 m/s impulse on the hanging load is not resisted (vertical speed stays > 0.9 m/s). | `CuriousContraptions.tests/RopeTests.cs@a6c914e:L126-L156` | carry forward (acceptance) | "Compression cannot push". |
| 5 | Legacy rope had a single material; no stiffness or cutting property. | `engine/physics/PhysicsRopeJoint.cs@a6c914e:L114-L122` | carry forward as context | Both properties are new. |

**Files harvested:** `engine/physics/PhysicsRopeJoint.cs`, `engine/ConnectionPort.cs`, `engine/RopeNetwork.cs`, `engine/MachineData.cs`, `CuriousContraptions.tests/RopeTests.cs`. Searched for steel, cable, wire rope: only electrical wires, unrelated.

## 5. Acceptance outline

Requirement row: [element-067](../requirements.md#element-067).

- **Chrome UI recipe (pending, post-catalogue).** Place a Rope anchor and a Weight; connect them and choose "Steel cable" in the connection chooser. Verify the material and length from the committed read; Run; lift the Weight with a ball from below and see the cable go slack; Reset.

All tests are planned, in `CuriousContraptions.tests/SteelCableTests.cs`, on the real simulation. Stall guard: 5 s.

| Row | Given / when / then | Planned test | Control | Boundary | Tolerance and its gameplay reason |
| --- | --- | --- | --- | --- | --- |
| K1 it holds | An 8 kg Weight on a 3 m cable from a Rope anchor hangs for 3 s | `SteelCableTests.ACableHoldsTheHeaviestWeight` | The same Weight unconnected falls below 1 m | 0.25 kg Weight | Settled stretch under 5 mm (derived g/(2πf)² = 0.07 mm at 60 Hz; the bound fails any span softer than about 7 Hz, g/(2π·7)² = 5.1 mm, and K3 pins the declared 60 Hz exactly): a cable-hung load must not sag into a 1 cm clearance |
| K2 exact source outcome: compression cannot push a load | The Weight starts 1 m above its hanging height (cable slack): it falls freely until taut; while slack its downward acceleration is g. Then an upward 1 m/s kick to the hanging Weight is not resisted | `SteelCableTests.ASlackCableNeitherPushesNorHoldsItsLoad` | K1 (taut holds) | Two dynamic Weights joined by a cable and pressed together keep ordinary contact and feel no cable force | Free-fall speed after 0.3 s within 5 % of g·t; kicked speed above 0.9 m/s after one tick (the legacy fact 4 bound) |
| K3 declared values reach the solver | The compiled span of a cable connection carries the declared stiffness, damping and cutting resistance; a rope connection in the same scene carries rope's | `SteelCableTests.TheCompiledSpanCarriesItsMaterial` | The rope span | none | Declared constants, exact |
| K4 resistant to scissors | [EL-066](EL-066-scissors.md) row X4 | `ScissorsTests.ScissorsNeverCutAMaterialAboveTheirCapacity` | X1 | — | there |
| K5 cut by tin snips | [EL-068](EL-068-tin-snips.md) rows T1 and T3 | `TinSnipsTests.SuppliedSnipsCutASteelCable` | T3 | — | there |
| K6 every Tie role | A cable route Rope anchor (Anchor) → Pulley (Guide) → Weight (Load) holds the Weight, and a cable from a falling Weight to an Impact lever `TieA` lifts the arm, exactly as rope does (`CuriousContraptions.tests/RopeTests.cs:804`, `CuriousContraptions.tests/ImpactLeverTests.cs:414`) | `SteelCableTests.ACableRunsOverAPulleyBetweenAnAnchorAndALoad` | The same route in rope gives the same heights | Route left open at the Pulley carries nothing | Heights within 0.02 m of the rope run |
| K7 one material per route | Adding a rope span to a cable route at a Pulley is refused with `WorkshopEditRejection.InvalidConnection` and changes nothing | `SteelCableTests.AMixedMaterialRouteIsRefused` | Two cable spans on that Pulley are admitted | none | Typed refusal |
| K8 not for other domains | Choosing the material for an electrical, activation or mechanical connection is refused | `SteelCableTests.AMaterialOnANonRopeConnectionIsRefused` | The rope-domain cable connection | none | Typed refusal |
| K9 length bounds | 0.05 m and 200 m cables are admitted; shorter, longer, NaN and missing lengths are refused | `SteelCableTests.CableLengthKeepsTheSpanBounds` | — | the two bounds | Declared values, exact |
| K10 Reset | After K2, Reset restores length, endpoints, material and poses | `SteelCableTests.ResetRestoresTheCable` | none | A cable cut by snips is restored | Authored values exactly |
| K11 save and load | The material id round-trips; a save naming an unknown material id is refused with a typed reason and nothing is loaded | `SteelCableTests.TheMaterialSurvivesSaveAndAnUnknownOneIsRefused` | A rope connection round-trips as rope | none | Field equality |
| K12 the strand is solid | A Tennis ball dropped 1 m onto the middle of a taut horizontal 3 m cable between two Rope anchors: over 3 s its centre, while within the strand's horizontal footprint, is never more than 0.01 m below the cable's top surface (it rests on the cable or rolls off its side) | `SteelCableTests.ABallDroppedOnACableDoesNotPassThroughIt` | The same ball dropped 0.5 m beside the cable falls past it | none | 0.01 m: a ball passing through a drawn strand is the visible-solid failure owner decision 1 excludes |

The S4 conformance rows are written for placed packages (ports and parameters); this package has neither, so the package tests above stand even after S4. Parameter refusal: none, no parameter.

## 6. Open questions

**Resolved**

1. Same Rope domain with a material on the connection, not a `Cable` domain. Decided now: a new domain would need a second set of Tie ports on every rope part; the batch convention forbids a spec-local domain.
2. Cable is an unlimited connection, not a counted inventory item. Decided now: connections have no limit today (`WorkshopConnections.cs:34`) and no requirement row asks for one (D19).
3. One route, one material. Decided now: a route is one strand and is cut whole.
4. The cable is a connection material declared through a non-placeable bundle entry `steel_cable`, not a placed part and not an engine enum. Decided now: the element-free rule forbids the engine naming steel, and delivery-batches treats belt and chain the same way (`delivery-batches.md:40`).
5. Stiffness 60 Hz and damping ratio 10, the delivered span values, replace the earlier 30 Hz and 1. D15: the earlier values assumed a softer rope that the code does not have.
6. Cutting resistance 25 J. D1 (the spec's value).
7. Strand art. D9.
8. **The cable is solid.** Owner decision 1, 22 November 2026: ropes, cables, hoses and ducts get colliders; only thin wires and the Balloon string stay non-solid drawings. The collider is a property of the declared connection material (diameter, segment rule), delivered by ENGINE-CONNECTION-MATERIALS (solid strand, coordinator decision 16), with no per-element code. The delivered rope's missing collider is a recorded visible-solid defect owned by that story. Row K12 proves it for this material.

**Needs the owner**

None.

## 7. Integration

- **Collaborators.** Every part with a Tie port (Rope anchor, Weight, Pulley, Impact lever, Balloon delivered; hook, winch, bucket, cage, scissors handle to come). Cutters EL-066 and EL-068.
- **Campaign.** Rope and cable first use in levels 11–20 (`docs/planning/invest/campaign-reservations.md:44`). Shared interactions [IX-04 tension transmission](../requirements.md#interaction-04) and, when cut, [IX-37 structural fracture](../requirements.md#interaction-37).
- **Lesson fixture (S8).** A level that uses cable before this slice cannot stand in rope for it where a cutter matters; it needs S8.

### Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of the identity's spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (`origin/main`, `25df3cd5`; pattern EL-239 Baseball). That section is not written here: the reader parses every spec that carries the heading and fails the whole coverage run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `steel_cable` (a non-placeable material entry) |
| Named identity | EL-067, `../invest/named-elements.md#element-067` |
| Acceptance | `todo-210`, `sequence-task-620` |
| Decision | S336, `../invest/scope-corrections.md#s336` |
| Mode records | `Configuration=Fixed` |

What it lacks today: `scope-corrections.md` has no `s336` block; `todo-210` is a scope index not immediately followed by `sequence-task-620` in `requirements.md`; and the material entry must be in the generated bundle (section 3). S7 widens the reader (owner decision 15), including for companion packages and TH, RAD and GAP identities.

## 8. Delivery

- **Slice shape.** Family slice "Rope materials": `rope` (EL-205) and `steel_cable`, each with its own acceptance rows. If the engine slice converts the delivered rope itself, this is a single element slice.
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES (strand colliders as query candidates) and ENGINE-ISLANDS-SLEEP; then (1) ENGINE-CONNECTION-MATERIALS (solid strand): the material declaration kind, connection field, wire, save, UI choice, the strand collider and the delivered rope's conversion; (2) ENGINE-TENSILE-EXTENSIONS (cutting clause) for the cutting rows; rows K1 to K3 and K6 to K12 need only (1), so the material may land before the cutters with K4 and K5 owed by their slices; (3) S7 (the coverage follow-on, which widens the reader, owner decision 15).
- **Files.** Adds `elements/SteelCable/SteelCable.csproj`, `SteelCableElementDeclaration.cs`, the strand art the engine story defines, `CuriousContraptions.tests/SteelCableTests.cs`. No `.tscn` unless the story draws strands from a scene. Edits `CuriousContraptions.slnx` and the trim list until S3 lands; the placement switches in `PackageRuntimeParityTests.cs` do not apply to an entry that is never placed, which the slice must confirm. At delivery: the binding section of section 7. This spec and TODO.
- **Review tier.** Data-only if the engine stories deliver the material with tests; otherwise engine.
- **Disqualifiers the probe must re-check.** The engine slice puts the material on the port instead of the connection (then EL-064 gains a delta and is no longer a closure); it expresses stiffness in a unit in which 60 Hz cannot be stated; it admits mixed-material routes; it declares a material through a placeable package with a dummy body; a material entry cannot be bound for coverage.
