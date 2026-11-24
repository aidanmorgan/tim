# EL-126 · Thin-screen shield — element readiness spec

**Current delivery (24 October 2026):** [EL-126 delivery slice](../invest/named-elements.md#element-126). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

Story 7.0 Batch O named-identity spec, brought to build-ready detail on 22 November 2026 against checkout `spec-drafts` (published main `8d1d68aa`) and repaired the same day after the independent audit (`docs/planning/elements/readiness/audits/elements-batch-o.md`, findings C-1, C-4, C-12, M-6, X-1 to X-4, section 4 item 3 and the Low note on the Domino citations and inertia) and coordinator decisions 16 and 21. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`; citations without `@` are `path:line` in the checkout. Requirement row: [element-126](../requirements.md#element-126). Lengths are metres; local X is the thickness direction. This is a fictional toy material, never real-world shielding guidance.

**This spec is the lead of the Family slice "Shield materials".** It states the shared body, probe and conformance once; [EL-127](EL-127-polymer-shield.md) and [EL-128](EL-128-dense-shield.md) cite it and state only their own values, calculations and rows, and so does [RAD-18](RAD-18-neutron-absorber-panel.md), which reuses this family's law, fixture F and test class in its own slice at level 127 (coordinator decision 37). Units, the attenuation law and the material rows are in the [radiation conventions](RAD-08-radiation-rate-meter.md#radiation-conventions), cited by item.

## 1. Identity

| Item | Value |
| --- | --- |
| ID / name | EL-126 · Thin-screen shield |
| Type | Radiation |
| Anchor | [requirements.md#element-126](../requirements.md#element-126); [named-elements.md#element-126](../invest/named-elements.md#element-126); scope source [radiation-05](../requirements.md#radiation-05) (umbrella, index only) |
| Owner | S610 |
| Disposition | Member and lead of the Family slice **Shield materials** ([delivery-batches §1](../invest/delivery-batches.md)) with EL-127 and EL-128, taught at 102 and 104. RAD-18 differs from them only in declared values but is split into its own slice at its teaching level, 127, because its scene rows need parts taught at 125 and 126 (coordinator decision 37). The members differ only in declared values: slab thickness, mass, contact material, radiation row, printed pattern, id, title and description. It is not a duplicate: each has its own row and catalogue entry (D8). |
| Planned package | id `thin_screen_shield`, directory `elements/ThinScreenShield/`, class `ThinScreenShieldElementDeclaration` |
| Related identities | Proving partners [RAD-01](RAD-01-gamma-source-capsule.md), [RAD-08](RAD-08-radiation-rate-meter.md); [RAD-03](RAD-03-alpha-source-cartridge.md) and [RAD-04](RAD-04-beta-minus-source-cartridge.md) (undelivered) are the planned parts the law-scene charged beams used here stand in for. No CAT part is refined. |
| Status | not started; no package; blocked on the engine programme (owner decision 3a, 22 November 2026), ENGINE-OPTICAL and ENGINE-RADIATION |
| Run length needed | 30 s (the default); no row is longer than 2 s |

## 2. Declaration

Shape followed: the Domino, a free dynamic box with a constant mass frame and no parameter (`elements/Domino/DominoElementDeclaration.cs:43`–`:53`, `Parameters: []` at `:23`). Items marked **(field)** use contracts that do not exist yet.

| Item | Value (shared by the family unless marked EL-126) |
| --- | --- |
| Interfaces | `IMechanicalElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:7`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`), plus the radiation-row declaration **(field)** |
| Body | One dynamic body, `KinematicMode.Dynamic`, `PayloadPolicy.None`, gravity (0, −9.81, 0) m/s², linear drag 0. Origin at the centre of the foot's underside. |
| Colliders (hidden `col_` proxies) | Foot box 0.30 × 0.06 × 0.80 m centred (0, 0.03, 0). Slab box t × 0.80 × 0.80 m centred (0, 0.46, 0), standing on the foot. EL-126: t = 0.02 m, twice the 1 cm tunnelling floor of the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope). Two colliders; they touch and do not overlap, so no chord is shared (radiation convention 4, overlap rule). |
| Mass and inertia | `MassFrameDeclaration.Constant`. Rule: both boxes at one uniform density; the centre of mass and the principal moments of the two-box composite about it are written as constants in the declaration, as the Domino writes its box tensor (`DominoElementDeclaration.cs:50`). Both boxes are centred on the local Y axis, so the principal frame is the identity. EL-126: 0.4 kg; density 0.4 ÷ (0.0128 + 0.0144) = 14.71 kg/m³, so the slab is 0.188 kg and the foot 0.212 kg; centre of mass at y = (0.188 × 0.46 + 0.212 × 0.03) ÷ 0.4 = 0.232 m. Principal moments about it: X = 0.188 × (0.80² + 0.80²) ÷ 12 + 0.212 × (0.06² + 0.80²) ÷ 12 + 0.188 × 0.228² + 0.212 × 0.202² = 0.0201 + 0.0114 + 0.0098 + 0.0087 = 0.0499 kg·m²; Y = 0.188 × (0.02² + 0.80²) ÷ 12 + 0.212 × (0.30² + 0.80²) ÷ 12 = 0.0100 + 0.0129 = 0.0229 kg·m²; Z = 0.188 × (0.02² + 0.80²) ÷ 12 + 0.212 × (0.30² + 0.06²) ÷ 12 + 0.0184 = 0.0100 + 0.0017 + 0.0184 = 0.0301 kg·m². |
| Contact material | EL-126: restitution 0.2, friction 0.5, rolling resistance 0 (a matt card that does not bounce cargo). |
| Radiation row **(field)** | On both colliders. EL-126: Thin screen: 2, 0.5, 0.25, 400, 20, 1, 1 per metre (Soft, Medium, Hard, Alpha, BetaMinus, NeutronFast, NeutronSlow); no conversion. |
| Ports | None. Radiation crosses space; a wire to a shield has nothing to attach to. |
| Parameters | None. Thickness is fixed per kind and taught by stacking (D13). |
| Stores, sensors, joints, drives | None. |
| Presentation bindings | None. The body pose moves the art. |
| Footprint | `ElementFootprint(Bodies: 1, MovingBodies: 1, PrismaticConstraints: 0, RevoluteConstraints: 0)`; domains Mechanical and the radiation flag **(field)** |
| Catalogue | EL-126: id `thin_screen_shield`, "Thin screen", `ElementCategory.Radiation` **(field)**, palette order 703 (EL-128 704, EL-127 705; coordinator decision 36, [palette allocation](readiness/palette-allocation.md)). Description: "A thin card. It stops alpha, and lets most beta and gamma through." |

### Art and solidity

| Art node | Mesh and size | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Slab` | box t × 0.80 × 0.80 m, EL-126 cream `#fff8e9` | solid (slab box) | body pose |
| `Visual/Foot` | box 0.30 × 0.06 × 0.80 m, navy `#293954` | solid (foot box) | body pose |
| `Visual/Pattern` | printed flat on both slab faces; EL-126 one horizontal groove line | decorative: printed flat | body pose |
| `Visual/Mark` | gold `#f7cb52` particle letter printed flat in a corner | decorative: printed flat | body pose |

Icon: a slab on a foot seen edge-on, with the member's pattern. The members differ by pattern, not colour alone: one groove (thin), two (polymer), three (dense), honeycomb (absorber); the thin screen is also visibly a card, 0.02 m against 0.05 m.

### Compiled counts

Bodies 1 (dynamic). Colliders 2 (boxes); the Domino has 1, and the foot is needed so a thin panel stands without a joint. Joints 0. Drives 0. Sensors 0. Signal nodes 0. Supply nodes 0. Radiation rows 2. Stores 0. Presentation bindings 0.

## 3. Engine capabilities

Map row families (navigation, D14): FiniteLedger, GeometryQuery, IonizingTransport, SignalPropagation. This probe, made on `8d1d68aa`, is the authority until the slice re-runs it. **4 present, 0 untested, 1 narrower, 3 missing.** This table is the family's.
| # | Capability | Status | Symbol | Test opened |
| --- | --- | --- | --- | --- |
| R1 | Free dynamic body with a constant mass frame and declared inertia | present | `MassFrameDeclaration.Constant` (`elements/Domino/DominoElementDeclaration.cs:47`) | `WorkshopDominoTests.DominoCompilesAsOneDynamicBoxWithDeclaredMassMaterialAndGravity` (`CuriousContraptions.tests/WorkshopDominoTests.cs:16`); `WorkshopDominoTests.CompiledInertiaIsTheHomogeneousBoxTensorAboutTheBoxCentre` (`:40`) |
| R2 | Every visible solid has a collider | present | per-package row of the test's `Specs` | `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt` (`CuriousContraptions.tests/VisibleSolidColliderTests.cs:206`) |
| R3 | Run, Reset, save and load of every admitted declaration | present | loop over `TestBundle.Packages` | `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| R4 | A connection to a part that declares no port is refused | present: a link whose target does not own a matching declared port is refused with `WorkshopEditRejection.InvalidConnection` (`engine/gpu/WorkshopConnections.cs:111`–`:114`) | `PortDescriptor` (`CuriousContraptions.Abstractions/PortDescriptor.cs:55`) | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`); its case at `:279`–`:280` connects to the Wall, which declares no port (`elements/Wall/WallElementDeclaration.cs:35`) |
| N1 | Line of sight through solids | narrower: `AirflowLineClear` (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs:141`) answers clear or blocked for airflow rows only and loops over every collider (`:149`); the broadphase offers only `QueryOverlaps` (`CuriousContraptions.Simulation/WorkshopDynamicBvh.cs:145`) | — | `AirflowRuntimeTests.MovingSolidOcclusionIsResampledBeforeTransfer` (`CuriousContraptions.tests/AirflowGeneralityTests.cs:13`). Owner of the segment cast returning ordered crossings: **ENGINE-SPATIAL-QUERIES** |
| M1 | The shared emitter, segment and receiver solve (needed by every acceptance row) | **missing** | — | owner **ENGINE-OPTICAL** |
| M2 | A declared attenuation row on a collider, surface τ 1 for radiation channels, and attenuation by chord over the ordered crossings of the segment cast at this tick's final pose (radiation convention 4) | **missing** | — | owner **ENGINE-RADIATION** |
| M3 | `ElementCategory.Radiation` (`CuriousContraptions.Abstractions/ElementCategory.cs:5`–`:12` has five members) and the radiation `SimulationDomain` flag | **missing** | — | owner **ENGINE-RADIATION** |

Nothing radiation-specific exists in the engine or the packages (searched 22 November 2026; see [RAD-08 section 3](RAD-08-radiation-rate-meter.md#3-engine-capabilities)).

**Dependencies.** A source and a detector: RAD-01 and RAD-08 (undelivered, first). The charged rows S5 to S7 use law-scene collimated emitters, so they need neither RAD-03 nor RAD-04.

## 4. Sources and legacy

- Requirement row [element-126](../requirements.md#element-126) and integration task under [radiation-05](../requirements.md#radiation-05): "equal geometry with different materials produces the declared readings; stacked thickness attenuates monotonically and gaps transmit. Repositioned panels restore."
- Named entry [element-126](../invest/named-elements.md#element-126), owner S610.
- Element map row: FiniteLedger, GeometryQuery, IonizingTransport, SignalPropagation; S605 and S257 decisions.
- Coverage binding: `docs/coverage/engine/element-02.json`, `FutureProduct`, proof owner S610, relation to radiation-05.
- Decision [S605](../invest/decisions.md#s605) photon and alpha rows (S606). The alpha row says "thin shield passes, thick shield stops", which conflicts with RAD-03's "a thin screen suppresses it" (resolved in section 6, item 6).
- Research: "Material shield panel" row (lessons 77, 79, 90, 91 → 102, 104, 115, 116) and the transport laws.
- **Legacy.** Searched `parts/`, `engine/physics/`, `engine/*.cs`, `engine/bridge/`, `CuriousContraptions.tests/`, `content/puzzles.json`, `tools/Campaign` and `reference/` for radiation, shield, alpha, beta, gamma, neutron, attenuation and related terms. No part, law, test, level or lesson exists. Hits are coverage snapshots only: an older copy of the radiation-05 binding that lists this identity, identical to the current binding, `reference/P0-022-before/docs/coverage/engine/task-017.json@a6c914e:L1752-L1803`, and aggregate relation lists — no element knowledge; do not carry forward (superseded by `element-02.json`).

**Files harvested** (all "no element knowledge"):
- `reference/P0-022-before/docs/coverage/engine/task-017.json`
- `reference/P0-022-before/docs/coverage/engine/task-002.json`
- `reference/P0-022-before/docs/coverage/engine/task-003.json`
- `reference/P0-022-before/docs/coverage/engine/task-013.json`
- `reference/P0-022-before/docs/coverage/engine/capabilities-02.json`

## 5. Acceptance

### Behaviour and numbers

All values are starting values (D1). Per channel each crossing of the panel transmits exp(−μ × chord) ([radiation convention 4](RAD-08-radiation-rate-meter.md#radiation-conventions): surface τ 1, chords from the segment cast of ENGINE-SPATIAL-QUERIES, no buildup or scatter).

- **Thin screen, square to the path (chord 0.02 m).** Alpha: e^(−400 × 0.02) = e^−8 = 0.000335. BetaMinus: e^(−20 × 0.02) = e^−0.4 = 0.670. PhotonMedium: e^(−0.5 × 0.02) = e^−0.01 = 0.990. Neutrons: e^−0.02 = 0.980.
- **What a detector sees.** A 1 W Alpha beam leaves the screen at 0.000335 W, under the 0.001 W floor, so the segment ends there: a thin screen stops alpha. A 1 W Beta-minus beam leaves at 0.670 W, above the meter's 0.2 W mark: it passes. A Gamma capsule 1 m from the standard meter reads 0.882 × 0.990 = 0.873 W: barely changed.
- **Thickness decides alpha.** A 0.002 m plate on the default row (Alpha 400 /m) passes e^(−400 × 0.002) = e^−0.8 = 0.449 W, above the mark; the 0.02 m screen leaves 0.000335 W ([RAD-03](RAD-03-alpha-source-cartridge.md), open question 1).
- **Stacking.** BetaMinus through one, two and three screens: 0.670, 0.670² = 0.449, 0.670³ = 0.301 W. The photon step is 1 % per screen, below the ±3 % reading tolerance, so for photons the rows assert only "not greater".
- **Gaps.** Air does not attenuate, so two screens read the same whatever the space between them, and a beam that passes beside a slab is unchanged.
- **Oblique path.** A screen turned 45° about Y has a chord of 0.02 × √2 = 0.0283 m: BetaMinus e^(−20 × 0.0283) = e^−0.566 = 0.568.
- **Body.** The centre of mass is 0.232 m up over a 0.30 m foot, so the panel tips beyond atan(0.15 ÷ 0.232) = 33°. A rolling Bowling ball (4 kg) pushes it along the bench; the attenuation follows the panel's pose.
- **Tick order.** The field solve of tick n runs after the last substep, on the panel's final pose of tick n, and commits readings with the tick ([radiation convention 11](RAD-08-radiation-rate-meter.md#radiation-conventions)); a detector's contact follows in the signal network of tick n + 1.
- **Energy.** Power removed is ledgered to the panel's collider (radiation convention 10); none is created. Whether it warms the panel is ENGINE-THERMAL's to consume, not a property of this part.

### Rows

Planned class `ShieldMaterialTests`, one class for the family; rows marked "family" are theories over the members. **Fixture F:** a Gamma capsule on the +X axis of a supplied Photon rate meter, both authored with their axis 0.70 m above the bench (static bodies need no support; `CatalogueIntegrationTests.cs:273` authors every package at y = 4); the meter's sample rays then lie between 0.60 and 0.80 m above the bench and within 0.10 m of the axis sideways, inside every member's slab (0.06 to 0.86 m), and above any rolling ball up to 0.56 m across. Slabs stand square to the axis between capsule and meter with their feet on the bench; feet are 0.30 m deep, so adjacent slabs stand 0.30 m apart. **Charged fixture:** a law-scene collimated emitter on the same axis, 1 W, paid by a declared steady inventory of 300 J (radiation convention 2; the cartridges' values, convention 14), Alpha with range 2 m or BetaMinus with range 3 m, aimed at a supplied Rate meter whose `sensitivity` matches. Readings and ratios ±3 %. Stall guard 2 s.

| # | Given / When / Then | Test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| S1 | Family: each member compiles as one dynamic body with two boxes, its declared mass, material, composite centre of mass and principal moments (EL-126: 0.232 m; 0.0499, 0.0229, 0.0301 kg·m²) | `ShieldMaterialTests.EachMemberCompilesAsOneDynamicTwoBoxBody` | — | mass exact; centre of mass 0.01 m; moments ±1 % (a tensor that drops the parallel-axis terms is off by more than 30 % about X and Z) |
| S2 | Family (task 413, equal geometry): given fixture F at 1 m with one slab, then the reading is 0.882 × that member's Medium transmission: thin 0.873, polymer 0.759, dense 0.324, absorber 0.798 W | `ShieldMaterialTests.EqualGeometryReadsByMaterial` | no slab: 0.882 W | ±3 % |
| S3 | Family (task 413, stacking): given the capsule at 1.4 m and one, two and three adjacent slabs, then each added slab never raises the reading, and the ratio to the open reading is T, T² and T³ | `ShieldMaterialTests.StackedSlabsAttenuateMonotonically` | no slab | ±3 % on ratios; thin screen photons: not greater only |
| S4 | Family (task 413, gaps): given the capsule at 1.4 m and two slabs 0.30 m apart and then 0.65 m apart (centres 0.45 and 1.10 m from the capsule), the readings are equal; given one slab moved 1 m sideways off the path, the reading is the open one | `ShieldMaterialTests.GapsAndMissedSlabsTransmit` | S2 | ±3 % |
| S5 | Given the Alpha charged fixture with the meter 1 m away, the screen square on the path leaves a strength below 0.001 W and the level off; without the screen the strength is 1 W and the level on; a law-scene plate 0.002 m thick with the default row in the screen's place leaves 0.449 W and the level on (thickness decides) | `ShieldMaterialTests.AThinScreenStopsAlpha` | no screen | below 0.001 W; ±3 % on 0.449 W |
| S6 | Given the Beta-minus charged fixture with the meter 2 m away and one, two and three adjacent screens, the strength is 0.670, 0.449 and 0.301 W and the level stays on (0.301 W is 50 % above the mark) | `ShieldMaterialTests.AThinScreenPassesBeta` | S5 | ±3 % |
| S7 | Given S6 with one screen turned 45° about Y, the strength is 0.568 W, lower than square-on | `ShieldMaterialTests.AnObliquePathIsLonger` | S6 | ±3 % |
| S8 | Family: given a wire dragged from a Battery `Supply` to the panel, the edit is refused and the construction is unchanged | `ShieldMaterialTests.AShieldAcceptsNoConnection` (the rule is R4's) | — | typed refusal |
| S9 | Family (task 413, restore): given fixture F at 1 m with the slab midway and centred 0.25 m to +Z of the axis (it spans z −0.15 to +0.65 m, covering every sample ray), when a Bowling ball launched along +Z at 4 m/s from z = −1.5 m strikes the slab's −Z edge, then once the slab's −Z edge is beyond z = +0.10 m the reading is the open 0.882 W; when Reset, the panel is at its authored pose and the next Run reads as S2. The thin screen, whose photon step (1 %) is inside the tolerance, runs this row with the Beta-minus charged fixture instead: 0.670 W standing, 1 W after | `ShieldMaterialTests.AttenuationFollowsThePoseAndResetRestoresIt` | S2 | pose 0.01 m; ±3 % |
| S10 | Family: save and load keep the authored pose; no parameter exists to refuse | generic: `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (R3) | — | — |
| S11 | Family: in every row the reading with a slab is at most the open reading | asserted inside S2 to S7 and S9 | — | never greater by more than 3 % |

S9's push, worst case the 4 kg dense slab: with restitution 0 the equal-mass slab leaves the 4 m/s ball at 4 × 4 ÷ 8 = 2.0 m/s and, with bench friction at most 0.6, slides 2.0² ÷ (2 × 0.6 × 9.81) = 0.34 m before the following ball pushes it again; it needs 0.25 m. Lighter members move further. The ball's top (0.56 m) stays below the lowest sample ray (0.60 m), so the ball never crosses the path. Every row runs in this slice. Chrome recipe (pending, post-catalogue): capsule, meter, Battery and Powered gate; drag one screen onto the path.

## 6. Open questions

None open.

1. **Static or dynamic — decided now**: dynamic, 0.4 kg. The row says "mass", and a panel a ball can knock aside is a puzzle.
2. **Coefficients — decided** (D1, D16): the Thin screen row of the conventions.
3. **Heating by deposited energy — decided** (D19): not part of this element; absorbed power is ledgered (radiation convention 10) and ENGINE-THERMAL may consume it.
4. **"Per cell" coefficients — decided now**: coefficients are per metre; the research cell is not used.
5. **Thickness presets — decided** (D13): none; one fixed thickness, stacking teaches thickness.
6. **S605's alpha row against RAD-03 — resolved by thickness**: both hold. A 0.002 m default-row plate passes 0.449 W and the 0.02 m Thin screen leaves 0.000335 W, so "thin passes, thick stops" is true of thickness and "a thin screen suppresses it" is true of this screen; row S5 proves both, as RAD-03's open question 1 and its row A3 do.

Needs the owner: none.

## 7. Integration

- **Collaborators.** RAD-01 and RAD-08 (undelivered); Bowling ball (delivered). RAD-03 and RAD-04 meet the screen in their own rows (RAD-03 A10, RAD-04 B7). No port, so no domain to agree.
- **Cross-element task.** [sequence-task-413](../requirements.md#sequence-task-413): rows S2, S3, S4 and S9. Processes [IX-15](../requirements.md#interaction-15) and [IX-01](../requirements.md#interaction-01).
- **Campaign.** Chapter 11, first use 101–110 (lessons 102, 104), alpha and beta lessons 115 and 116; reuse 111–120, 124–130, 136–150. No level in `content/puzzles.json` places it.
- **Coverage binding (planned, not yet live).** Blocked on the coverage follow-on story S7. Main's reader (`tools/Coverage/NamedPackageSources.cs` on `origin/main`) admits an `EL-NNN` identity only when its ledger entry carries exactly one `todo-N` link (`:96`) and its decision has an anchor; the entry for element-126 has no such link and S610 has no anchor in `docs/planning/invest/scope-corrections.md` (checked by search). The slice adds the binding once S7 admits it.
- **Lesson fixtures.** Story [S8](../invest/delivery-batches.md#s8-lesson-fixtures-for-any-admitted-package) is defined and lands after the engine programme (owner decision 22).

## 8. Delivery

- **Slice shape.** Family slice of three packages, EL-126, EL-127 and EL-128, one review, one candidate commit. RAD-18 is its own slice at level 127 (coordinator decision 37).
- **Prerequisites, in order.** Blocked on the engine programme (owner decision 3a), which includes ENGINE-SPATIAL-QUERIES; ENGINE-OPTICAL; ENGINE-RADIATION as amended; RAD-01 and RAD-08; S7 for the binding. No dependency on ENGINE-THERMAL ([ENGINE-RADIATION](../../../_bmad-output/planning-artifacts/epics.md#story-engine-radiation-emission-attenuation-decay-and-exposure)'s *Depends on* line names none).
- **Files.** Per member `elements/<Dir>/<Dir>.csproj`, `<Dir>ElementDeclaration.cs`, `<id>.tscn`, `icon.svg` (here `ThinScreenShield`, `thin_screen_shield`); one guarded line each in `CuriousContraptions.slnx`; `CuriousContraptions.tests/ShieldMaterialTests.cs`; a row each in `VisibleSolidColliderTests`. Registration is derived (`spec-registration-derivation.md`).
- **Review tier.** Data-only if the probe finds every row present after the engine stories; otherwise the engine tier.
- **Disqualifiers.** ENGINE-RADIATION delivering rows by a named material instead of declared coefficients on a collider (then the members cannot differ by data); a law other than radiation convention 4 (re-derive section 5); a thin dynamic box that does not stand or tunnels at 0.02 m (thicken the slab and scale μ to keep μ × chord). In each case the slice stops and the owning story or the conventions are amended first; no second design is kept here.

## 9. Engine conformance

Verdict: **Conforms** (every missing capability has exactly one owning element-free story). These points are the family's.

1. **Declaration data only.** One dynamic body, two boxes, a mass frame, a contact material and a row of seven coefficients. No code path names a shield.
2. **Generic laws.** Rigid-body contact and friction; attenuation by chord from a declared row (radiation convention 4). "Stops alpha" is e^−8, not a rule about alpha and screens.
3. **Three layers.** Colliders: two boxes. Visuals: slab, foot and printed pattern, none used as a collider. Animation: none; the body pose moves the art.
4. **One-way flow and typing.** Physics advances the pose through the substeps; the field solve of the same tick reads the final pose after the last substep, and readings act from the next tick. Channels are an enum; coefficients are f32.
5. **Flat state.** Counts in section 2, fixed at compile time.
6. **Performance fitness.** Two primitives; no sensor or joint; island sleeping does not exist today, so a resting panel is stepped every tick until ENGINE-ISLANDS-SLEEP (owner decision 3a) delivers it; the 0.02 m slab is twice the envelope's tunnelling floor and relies on the engine's general speculative contact, not a per-element check; the panel adds no per-tick field work except as crossings returned by the spatial query's segment cast.
7. **Tests are scenarios.** Rows S1 to S11 run the production simulation through placement, Run and committed reads.
