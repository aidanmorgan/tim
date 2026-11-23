# EL-093 · Toy dynamite charge — named-identity spec

**Current delivery (24 October 2026):** [EL-093 delivery slice](../invest/named-elements.md#element-093). Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md); browser qualification is deferred until every named element passes. Pinned legacy citations describe the harvest baseline, not current implementation instructions.

**Build-ready revision, 22 November 2026 (checkout `8d1d68aa`).** Every section is written against current code; a `path:line` citation without a commit is in this checkout, and `path@a6c914e:Lx-Ly` resolves with `git show a6c914e:<path>`. Values marked **starting value** are adopted under decision D1 of [delivery batches](../invest/delivery-batches.md#3-family-level-decisions). All game values are canonical f32. No number here was simulated; each is a hand calculation shown beside it.

**Safety boundary.** An abstract toy store of energy in joules that releases one declared impulse field. No real composition, formulation, quantity, ignition chemistry or construction recipe is modelled or described (requirement row).

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-093 · Toy dynamite charge · Specialist |
| Anchor | [requirements.md#element-093](../requirements.md#element-093); [named entry](../invest/named-elements.md#element-093); scope index [todo-197](../requirements.md#todo-197) |
| Proof owner | S663 |
| Disposition | To deliver as a single element, the first consumer of the blast capability. [EL-106](EL-106-impact-sensitive-toy-charge.md) follows it and is not a Family member with it: the release cause differs (a signal here, a contact shock there), which is behaviour, not a value. |
| Planned package | id `toy_dynamite`, directory `elements/ToyDynamite/`, class `ToyDynamiteElementDeclaration` |
| Refines / extends | No CAT spec. Body shape follows the Domino (a free dynamic body with a Signal port, `elements/Domino/DominoElementDeclaration.cs:29`) |
| Status | not started; blocked on the engine programme (owner decision 3a), then ENGINE-IGNITION-BLAST, then S7 |

## 2. Declaration

The package implements `IEnergyStoreElement` (`CuriousContraptions.Abstractions.Mechanical/MechanicalContracts.cs:35`), `ISignalElement` (`CuriousContraptions.Abstractions.Signal/SignalContracts.cs:8`), `ISceneGeometryElement` (`CuriousContraptions.Abstractions/SceneGeometry.cs:10`) and the blast-source interface that ENGINE-IGNITION-BLAST adds. `KinematicMode.Dynamic`, `SupportedDomains = Mechanical | Signal`. Footprint: 1 body, no constraints.

### Body and colliders

One dynamic body, long axis local +Y. Mass 0.3 kg (**starting value**: lighter than a Tennis ball's 0.35 kg, so a blast nearby can throw it). Mass frame: centre at the origin; principal moments of a solid cylinder of radius 0.09 m and length 0.6 m: axial ½·0.3·0.09² = 0.00122 kg·m², transverse 0.3·(3·0.09² + 0.6²)/12 = 0.00961 kg·m². Gravity (0, −9.81, 0); mass-frame `linearDrag` 0.04 /s (`CuriousContraptions.Abstractions/MassFrameValues.cs:39`). Material: restitution 0.1, friction 0.5, rolling resistance 0.03, bounce threshold 0.1 m/s (**starting values**: a paper-wrapped stick that does not bounce or roll far).

| Collider | Shape | Dimensions (m) | Offset |
| --- | --- | --- | --- |
| Stick | Cylinder about +Y | radius 0.09, half-length 0.30 | origin |
| Cap | Cylinder about +Y | radius 0.10, half-length 0.03 | (0, 0.27, 0) |

### Store and blast source

- **Store** `1` (`EnergyStoreDescriptor`, `CuriousContraptions.Abstractions/EnergyStoreValues.cs:73`): capacity and initial energy both the parameter `energy`; charge power 0. Full at Run, never refilled.
- **Blast source** `1` (missing capability): frame body 0, origin at the centre of mass; reference impulse 3 N·s at distance 0 (fixed, **starting value**); linear falloff to zero at the parameter `blast_radius`; paid from store 1; released by the signal node `release`; the reaction applied to this body.
- **Signal node:** the blast story's release request node `release`; port binding `ActivationIn` → `release`, `SignalTerminal.Input`. The shape is the Cannon's release node (`SignalPhysicalRequestDescriptor`, `elements/Cannon/CannonElementDeclaration.cs:47`–`:51`).

### Ports

| `PortId` | Domain | Direction | Role | Kind | Local position |
| --- | --- | --- | --- | --- | --- |
| `ActivationIn` | Signal | Input | `ActivationIn` | Pulse | cap top, (0, 0.30, 0), normal +Y |

The other end is the `ActivationOut` (Signal, Output, role `ActivationOut`, Pulse) of [EL-094](EL-094-detonation-plunger.md), the Impact switch or the Domino. Both ends agree in domain and kind.

### Parameters

| Key | Kind | Unit | Default | Min | Max | Editor policy | Reason |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `energy` | Real | Joule | 40 | 5 | 100 | Reject | near the Cannon's 45 J default (`CannonElementDeclaration.cs:17`): a comparable toy |
| `blast_radius` | Real | Metre | 1.5 | 0.5 | 3 | Reject | reaches neighbouring parts, not the 16.8 m bench |

Build-only (D11). The reference impulse is fixed per kind (D13); the unit set has no newton-second (`CuriousContraptions.Abstractions/ParameterDescriptor.cs:15`).

### Metadata and bindings

Category `Motion`, display name "Toy charge", palette order 170 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)), colour (0.94, 0.43, 0.33). Events: the blast story's release event. Presentation: `Visual/Puff` `UniformScale` on `PresentationDriver.Event` for that event, `SineSquaredPulse`, 0.4 s, 0 → 1.0 m ring; `Visual/Wrap` albedo on `PresentationDriver.CommittedStoredFraction`, `ClampToEndpoints` (coral when full, slate `#556573` when empty).

### Art and solidity

| Node | Mesh | Class | Moved by |
| --- | --- | --- | --- |
| `Visual/Wrap` | coral `#f06e54` cylinder with two cream `#fff8e9` bands printed flat | solid, Stick collider | body pose |
| `Visual/Cap` | navy `#293954` disc, 1 cm proud | solid, Cap collider | body pose |
| `Visual/Socket` | gold `#f7cb52` ring printed flat on the cap | decorative: printed flat | body pose |
| `Visual/Puff` | cream ring, hidden at scale 0 | decorative: moved by a binding | binding |

No fuse is drawn: a proud fuse would be a solid without a collider. Icon: a navy stick with a cap and a gold starburst. It differs from the Jumpy flask (EL-106) by being a capped stick with a socket, where the flask is a round stoppered bottle with none.

### Behaviour and numbers

Laws: rigid-body dynamics and contact; the one-shot blast impulse with solid occlusion (missing); the finite-store ledger; signal routing.

- **Release.** One pulse on `ActivationIn` releases the blast; routing is same-tick and the impulses are applied in the physics step that follows. A pulse on a spent charge is answered with a typed refusal and does nothing.
- **Impulse on each body.** Every dynamic body whose centre of mass is within `blast_radius` and joined to the origin by a segment that no solid crosses (the source's and the target's own colliders excluded; candidates from ENGINE-SPATIAL-QUERIES) receives J(d) = 3 × (1 − d ÷ `blast_radius`) N·s along the outward direction at its centre of mass. The charge itself receives the negative sum.
- **Energy cap.** If Σ J² ÷ 2m over the targets and the charge, evaluated from rest, exceeds `energy`, every impulse is scaled by the same factor, the envelope's proportional rule ([envelope](../../gpu-f32-physics.md#game-grade-envelope)). The store then reads zero for the rest of the Run.
- **Hand values, `blast_radius` 1.5 m.** A 1 kg Basketball at 0.5 m: J = 3 × (1 − 0.5/1.5) = 2.0 N·s, 2.0 m/s, 2.0 J. The same ball at 1.0 m: 1.0 N·s, 1.0 m/s. At 2.0 m: nothing. A 0.35 kg Tennis ball touching the stick (d ≈ 0.34 m): J = 2.32 N·s, 6.6 m/s, under the Bumper's 8 m/s target. A 4 kg Bowling ball at 0.5 m: 0.5 m/s.
- **Recoil.** With one Basketball at 0.5 m the charge takes 2.0 N·s the other way: 2.0/0.3 = 6.7 m/s and 6.7 J. Total 8.7 J, under the 40 J cap.
- **Cap binding.** Eight Tennis balls with `radius` 0.0625 m and `mass` 0.01 kg on a 0.3 m circle around the stick (0.24 m apart along the circle, 0.15 m clear of the stick) would each take 3 × (1 − 0.3/1.5) = 2.4 N·s, 2.4²/(2 × 0.01) = 288 J each, 2 304 J in all; the ring is symmetric, so the charge takes no net reaction. The cap scales all eight by √(40/2304) = 0.13: 5 J and about 31.6 m/s each, under the 64 m/s linear envelope.
- **Domino.** A Domino (0.4 kg, 1.1 m tall, 0.25 m thick, `elements/Domino/DominoElementDeclaration.cs:47`) tips when the impulse at its centre gives it the energy to lift its centre over the edge: rise √(0.55² + 0.125²) − 0.55 = 0.014 m, 0.4 × 9.81 × 0.014 = 0.055 J; about the bottom edge I = 0.4 × 4 × (0.55² + 0.125²)/3 = 0.170 kg·m², so (0.55 J)²/(2 × 0.170) ≥ 0.055 gives J ≥ 0.25 N·s. At 1.0 m it receives 1.0 N·s and falls; at 1.45 m it receives 0.1 N·s and stands.
- **"Pressure impulse".** The requirement's phrase is met by an impulse field. As an illustration only: 2.0 N·s on a Basketball's cross-section (π × 0.34² = 0.363 m²) over one 1/480 s substep is an overpressure of 2.0/(0.363 × 0.00208) = 2 650 Pa, that is 103 975 Pa absolute against the 101 325 Pa ambient. The engine does not compute pressure.
- **Fragile parts.** A blast is not a contact; a fragile part breaks only by its own contact criterion, for example when thrown against a Wall.

## 3. Engine capabilities

Map row families (navigation only, D14): ChemicalReaction, EnvironmentState, FiniteLedger, FiniteWorkActuation, GeometryQuery, RigidBodyDynamics, StateTransaction.

| Capability | Status | Evidence |
| --- | --- | --- |
| Free dynamic body with a declared mass frame and two colliders | present | `CompoundMassAndRollingTests.CollisionChildrenDoNotChangeDeclaredFreeFlight` (`CuriousContraptions.tests/CompoundMassAndRollingTests.cs:283`) |
| Finite store full at Run, never refilled | present | `EnergyStoreValues.cs:73`; `FanIntegrationTests.ActualHubDoesNotOccludeThePaidConeButOffAndVacuumDoNotTransfer` (`CuriousContraptions.tests/FanIntegrationTests.cs:481`) reads a 14 400 J store that starts full and only falls |
| A Signal pulse input that requests a one-shot physical release, refused without spending when it cannot fire | present for a chamber actuator only: narrower | `SignalPhysicalRequestDescriptor` (`CuriousContraptions.Abstractions/SignalContractValues.cs:65`) targets a retention or a finite-work actuator (`CuriousContraptions.Abstractions/PhysicalRequestValues.cs:20`–`:21`); `CannonIntegrationTests.PhysicalLoadChargesAndFiresOnlyWithItsSeparateSupply` (`CuriousContraptions.tests/CannonIntegrationTests.cs:550`). A blast target kind is added by ENGINE-IGNITION-BLAST |
| Store-fraction and event-cue presentation drivers | present | `ElementPresentation.cs:18`–`:37`; same Cannon test |
| Wrong-domain connection refusal | present | `BundlePortsTests.AWrongDomainOrOutputToOutputLinkIsStillRefused` (`CuriousContraptions.tests/BundlePortsTests.cs:262`) |
| **One-shot radial impulse with solid occlusion, paid from a finite store, spent until Reset, released by a signal** | missing | nothing in the engine (`epics.md` ENGINE-IGNITION-BLAST, "Present today: nothing"). Owner: **ENGINE-IGNITION-BLAST** (`engine-ignition-blast`). Its text has only a thermal ignition threshold; the additions this part needs are in the Batch L summary; candidates and occlusion come from ENGINE-SPATIAL-QUERIES, and the blast half does not wait for ENGINE-THERMAL |

Counts: 4 present, 0 present-untested, 1 narrower, 1 missing. Nothing needs element-specific engine code.

## 4. Sources and legacy

- Requirement: "Finite toy energy store releases one declared pressure impulse when triggered"; outcome "Spent charge cannot repeat; no real formulation or construction recipe" ([element-093](../requirements.md#element-093)).
- No legacy charge exists (`git grep -i` for "dynamite", "explos", "blast", "detonat" at `a6c914e` finds nothing).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| L1 | A store has finite capacity and an initial energy between 0 and capacity | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L7-L19` | carry forward (the current descriptor) |
| L2 | A debit is finite, non-negative and no larger than the stored energy | `engine/physics/PhysicsEnergyStore.cs@a6c914e:L47-L52` | carry forward the rule; not the CPU double ledger |
| L3 | Release is atomic: failure restores the world, and the debit equals the work supplied | `engine/physics/PhysicsWorld.cs@a6c914e:L1250-L1267` | carry forward as "release and spend commit together" |

**Files harvested:** `engine/physics/PhysicsEnergyStore.cs`, `engine/physics/PhysicsWorld.cs` (energy-store release only).

## 5. Acceptance outline

Requirement row: [element-093](../requirements.md#element-093). Planned test class `ToyDynamiteTests`. Speeds are read on the first committed tick after the release; a 15 % band covers centre-of-mass distance and bench friction in that tick.

| # | Given / When / Then | Planned test | Control | Boundary and tolerance |
| --- | --- | --- | --- | --- |
| A1 | Given Basketballs at 0.5 m and 1.0 m and a pulse, then the near ball leaves at about 2.0 m/s, the far one at about 1.0 m/s, each directly away | `OnePulseReleasesOneImpulseFieldThatFallsOffWithDistance` | A2 | each speed within 15 %; near faster than far |
| A2 | Given no pulse, then nothing moves and the store stays full | `WithoutAPulseNothingHappens` | A1 | every body under 0.01 m/s for 3 s |
| A3 | Given a spent charge and a second pulse, then nothing moves and the store stays zero | `ASpentChargeCannotRepeat` | A1 | no body gains more than 0.01 m/s; the request reads refused |
| A4 | Given a Wall between the charge and a ball at 1.0 m, then that ball does not move | `ASolidWallShieldsTheBodyBehindIt` | the unshielded ball of A1 | under 0.01 m/s |
| A5 | Given balls at 1.4 m and 1.6 m, then only the first moves | `BodiesBeyondTheRadiusReceiveNothing` | — | 1.4 m: at least 0.1 m/s (hand 0.2); 1.6 m: under 0.01 m/s |
| A6 | Given eight Tennis balls with `radius` 0.0625 m and `mass` 0.01 kg on a 0.3 m circle, then the kinetic energy added to all bodies is at most `energy` and the eight speeds are equal | `TheStoreCapsTheTotalEnergyByOneCommonFactor` | A1 is uncapped | total ≤ 40.4 J (1 %); speeds within 5 % of each other |
| A7 | Given one Basketball at 0.5 m, then the charge recoils the other way with equal momentum | `TheChargeTakesTheReaction` | — | momenta equal and opposite within 15 % |
| A8 | Given Dominoes at 1.0 m and 1.45 m, then the first falls and the second stands | `ANearDominoFallsAndAFarOneStands` | — | fallen means its orientation signal fired; 4 s guard |
| A9 | Given `energy` 4 J or `blast_radius` 3.5 m, then the edit is refused | `OutOfRangeValuesAreRefused` | defaults accepted | typed refusal |
| A10 | Reset restores pose, the full store and every thrown body; save and load keep parameters and the wire | `ResetAndSaveLoadRestoreTheCharge` | — | also the generic `CatalogueIntegrationTests.EveryAdmittedDeclarationRetainsAuthoredStateAcrossRunResetSaveLoad` (`CuriousContraptions.tests/CatalogueIntegrationTests.cs:273`) |
| A11 | A Battery `Supply` offered to `ActivationIn` is refused | covered generically (`BundlePortsTests.cs:262`) | an `ActivationOut` is accepted | typed refusal |
| A12 | Given [EL-094](EL-094-detonation-plunger.md) wired to the charge and a Bowling ball dropped on the plunger, then the charge releases after the stroke and the energy added equals the charge's store debit, with nothing from the plunger | `ThePlungerCommandsTheChargeAndSuppliesNoEnergy` (in `PlungerTests`) | plunger unwired: no release | added energy ≤ `energy` |

### Integration

- Trigger sources: [EL-094](EL-094-detonation-plunger.md) (planned), CAT-063 Impact switch, CAT-023 Domino, CAT-052 Pressure plate (delivered). Targets: every delivered ball, Domino, Weight. Shield: CAT-066 Wall.
- Campaign: chapter 10, levels 91–100, "dynamite/plunger abstraction" (`docs/planning/requirements.md:2411`; `:2295`; `docs/planning/invest/campaign-reservations.md:360`); reuse 136–150.
- A level that fixes a charge as authored scenery needs S8.

### Delivery

- **Shape.** Single element slice; A12 lands with whichever of EL-093 and EL-094 is second.
- **Prerequisites, in order.** The engine programme (owner decision 3a), of which this part leans on ENGINE-SPATIAL-QUERIES (blast candidates and occlusion) and ENGINE-ISLANDS-SLEEP; then ENGINE-IGNITION-BLAST with the additions named in the summary (its blast half is not after ENGINE-THERMAL); then S7 (section 7). S3 optional. Port roles, node arity, target masks and mouth identities are written in the form ENGINE-CAPABILITY-INSTANCES leaves; the names here are today's.
- **Files.** `elements/ToyDynamite/` (project, declaration, `toy_dynamite.tscn`, `icon.svg`); `CuriousContraptions.tests/ToyDynamiteTests.cs`; one `CuriousContraptions.slnx` line; until S3, the trim list and placement switch.
- **Review tier.** Data-only if the engine story publishes every row above with tests; otherwise engine tier.
- **Disqualifiers the probe re-checks.** The story published without a signal-commanded release, without the reaction on the source body, or without the common energy cap.

## 6. Open questions

All resolved.

1. **Linear falloff with a common energy cap** (D16: decided by the engine story for every consumer; this spec's values are starting values, D1).
2. **A blast does not break fragile parts directly**; they break by their own contact criterion (decided now: one failure rule per fragile part, not a second one).
3. **The spent husk stays** as the same body (D19): nothing is destroyed or spawned.
4. **Strength is `energy` and `blast_radius`**; the reference impulse is fixed (D13).

Needs the owner: none.

## Engine conformance (22 November 2026)

Checked against the owner addendum of 22 November 2026 (AGENTS.md element-free engine gate and Tri-Graph rule; `_bmad/custom/element-free-engine-design.md`; `docs/gpu-f32-physics.md`).

1. **Declaration data only.** One body, one store, one blast source, one release node, one port, two parameters, two bindings. No handler runs "when triggered".
2. **Generic laws.** The release is the signal network delivering a request to a declared blast source; the impulse field, its occlusion, cap and reaction are the general blast law, owned by one element-free story; "spent" is an empty store.
3. **Three layers.** Colliders: two cylinders on the body. Visuals: wrap and cap follow the body pose; bands and socket are printed flat. Animation: the puff and the wrap colour read the release event and the committed store fraction and write nothing back.
4. **Flow and typing.** One typed Signal input; typed parameters with units.
5. **Flat state.** Bodies 1 (dynamic); colliders 2 (cylinders); joints 0; drives 0; blast sources 1; sensors 0; signal nodes 1; stores 1; presentation bindings 2.
6. **Performance.** Two primitives. The blast is one bounded-radius query in one step of the Run, not per-tick work, with its candidates from ENGINE-SPATIAL-QUERIES. No sleeping exists today: resting cost is owned by ENGINE-ISLANDS-SLEEP (owner decision 3a). No fuse cord or cable is drawn, so the solid-strand rule does not apply.
7. **Tests are scenarios.** Every row runs the production simulation with delivered targets.

**Verdict: conforms** (one general capability missing, with one owner; the audit fixes of 22 November 2026 are applied).

## 7. Coverage binding (planned, not yet live)

Published main binds a named-identity package through a final section of its spec headed "Package source binding", read by `tools/Coverage/NamedPackageSources.cs` (pattern: EL-239 Baseball). The section is not written here: the reader parses every spec carrying that heading and fails the whole run when a field does not resolve. **Blocked on the coverage follow-on story S7.**

| Field | Planned value |
| --- | --- |
| Package | `toy_dynamite` |
| Named identity | EL-093, `../invest/named-elements.md#element-093` |
| Acceptance | `todo-197`, `sequence-task-646` |
| Decision | S663 |
| Mode records | `Configuration=Fixed` |

What it lacks today: `docs/planning/invest/scope-corrections.md` has no `s663` block; `todo-197` is a scope index shared by EL-091 to EL-095 (`docs/planning/requirements.md:4100`) and is not co-anchored with this identity's own task, `sequence-task-646`, which sits beside `element-093` (`docs/planning/requirements.md:5141`).
