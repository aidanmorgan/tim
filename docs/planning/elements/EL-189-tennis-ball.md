# EL-189 · Tennis ball — current named-identity contract

**Current delivery:** [EL-189 delivery slice](../invest/named-elements.md#element-189), Story 12.1a. [CAT-064](CAT-064-tennis.md) and the [approved native spec](../../../_bmad-output/implementation-artifacts/spec-12-1a-tennis-ball.md) record current declarations and source decisions. Apply the [package/bundle architecture](../../architecture/element-package-isolation.md) and [native unit/integration acceptance](../../testing-guidance.md). Browser qualification remains deferred until every named element passes.

The [Story 7.0 readiness record](../../../_bmad-output/implementation-artifacts/spec-7-0-element-implementation-readiness.md) and citations pinned to a6c914e are historical evidence. They do not add current acceptance or restore retired engine paths. Historical citations use `path@a6c914e:Lstart-Lend`.

## 1. Identity

| Field | Value |
| --- | --- |
| ID / name / type | EL-189 · Tennis ball · Gravity |
| Anchor | [requirements.md#element-189](../requirements.md#element-189); [named-elements entry](../invest/named-elements.md#element-189); scope source [campaign element coverage](../requirements.md#campaign-element-coverage) |
| Refines | [CAT-064 tennis](CAT-064-tennis.md) ([requirement](../requirements.md#current-cat-064)); the CAT spec holds the full legacy harvest (lessons, pressure-plate control). Contrasts: [EL-187](EL-187-basketball.md), [EL-188](EL-188-bowling-ball.md). |
| Roadmap story | [EL-189 delivery slice](../invest/named-elements.md#element-189); canonical proof owner and prerequisite decisions are recorded there. |
| Harvest-baseline status | Not started. No `WorkshopPartKind` member (`engine/gpu/WorkshopPartKind.cs@a6c914e:L3-L3`). |

## 2. Current declaration

Tennis is a configurable dynamic spherical capture payload. Its distinct response follows declared material, not identity-selected physics. It is the data-only elements/TennisBall package, admitted through the same shared compilation and simulation contracts as other bodies.

- **Bodies and shapes:** one dynamic sphere. Radius drives all three sphere half-extents and the complete Visual subtree scale radius/0.25; explicit principal moments are 0.4×mass×radius².
- **Five BuildOnly controls:** all values are canonical f32 and persist in decoded save/load. Unknown, missing, nonfinite or out-of-domain values refuse atomically.

| Field | Unit | Inclusive range | Default |
|---|---|---|---|
| mass | kg | 1/1024–1024 | 0.35 |
| bounce | dimensionless | 0–1 | 0.85 |
| radius | m | 1/16–16 | 0.25 |
| buoyancy | m/s² | 0–16 | 0 |
| drag | 1/s | 0–0.125 | 0.04 |

The approved source resolution retains 0.35 kg/0.25 m from `parts/catalog/tennis.tres@a6c914e:L14-L14` to preserve one-versus-two-ball loading of a 0.5 kg Pressure plate. Current parent Story 12.1 restitution 0.85 supersedes historical 0.78. Friction 0.3 and bounce threshold 0.1 m/s retain the source defaults (`reference/cpu/MachinePart.cs@a6c914e:L236-L236`). Rolling resistance is 0: the former proposed 0.04 is not current behavior. No unprescribed rolling-loss mechanism or promise of rolling arrest is added.

- **Constraints, ports and stores:** none. No owned sensor or activation row; the ball participates as a Receiver capture target and a physical Switch/Pressure plate load.
- **Artwork and presentation:** distinct script-free native felt-yellow sphere, cream tilted ring seam and matching icon in elements/TennisBall. Palette color remains #b8db59 (`DESIGN.md@a6c914e:L167-L167`). Existing C# MachinePart ownership and shared parameter presentation preserve complete visual scaling; cosmetics never write physical state.
- **Catalogue:** identity tennis, title Tennis ball, Motion category, package-owned metadata and bundle discovery into the Free palette. No element-specific engine arm or named PartId constant is required.

## 3. Shared capabilities and atmosphere ownership

Families remain applicable by supported mode ([element map](../general-engine-element-map.md), [binding](../../coverage/engine/element-02.json)): ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction and StateTransaction. This slice uses existing rigid-body/contact/payload/lifecycle laws plus shared typed principal-moment, world-acceleration, damping, exact-body material and geometry/presentation bindings.

Story 12.1b migrates Tennis to the shared explicit construction atmosphere: Standard has relative pressure 1 and Vacuum 0. World acceleration is (0,-9.81+buoyancy×relative pressure,0), with linear damping drag×relative pressure. Every admitted buoyancy value executes. Tennis owns no gas store; its product drag range remains bounded independently of the generic nonnegative drag law.

The former “finite atmosphere” handoff is clarified under EL-125: ambient Standard/Vacuum are explicit unbounded boundary reservoirs; finite sealed gas stores and pressure-work admission belong to 12.1b/S470. Spatial profiles and gas/liquid network coupling remain with their first consuming slices, retaining S471/S697 acceptance. Fan 12.2 owns conserved airflow transfer, light/heavy trajectory comparisons, no-fan/missed/blocked-jet controls and the 22 source airflow lessons. Balloon is a separate element. The two explicit boundary presets do not claim full EL-125 lesson or spatial-profile qualification.

## 4. Sources and legacy

- **Requirements.** Row: "Light compliant spherical cargo retains distinct calibrated contact properties"; outcome "Its bounce differs from the bowling ball because of declared material, not IDs" ([element-189](../requirements.md#element-189)).

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | Catalogue parameters mass 0.35, bounce 0.78, radius 0.25, buoyancy 0, drag 0.04 as a string-keyed dictionary. | `parts/catalog/tennis.tres@a6c914e:L14-L14` | Historical defaults; current bounce is 0.85 and all five controls use the shared typed parameter contract. No legacy dictionary reader returns. |
| 2 | Isolated drop: rebound ratio bounce² ± 0.015 (0.61 for 0.78), timing √(2h/g) ± 0.02 s. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L14-L81` | Historical observation only; current causal bounce/rest and game-fidelity acceptance below governs. |
| 3 | Dropped from y = 3 it bounces and settles within 2400 ticks without sinking. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L26-L51` | Historical observation only; current causal bounce/rest and game-fidelity acceptance below governs. |
| 4 | Further facts (pressure plate, 22 airflow lessons). | [CAT-064 spec](CAT-064-tennis.md) | Held in the CAT spec. |

Files consulted: `parts/catalog/tennis.tres`, `reference/cpu/MachinePart.cs`, `CuriousContraptions.tests/PhysicsCalibrationTests.cs`, `CuriousContraptions.tests/FloorTests.cs`.


## 5. Current acceptance

Point of truth: [element-189](../requirements.md#element-189), [CAT-064](../requirements.md#current-cat-064) and the native spec.

- **Native positive/control:** matched Tennis/Basketball/Bowling drops demonstrate material-dependent rebound and stable supported rest. Independent mass/radius configurations affect inertia and physical response; zero/nonzero drag and below/above-gravity buoyancy controls distinguish motion.
- **Boundaries:** all five admitted parameter domains, malformed/missing/unknown values, physical/visual scale agreement and generic typed declaration refusals. Material is configurable within those domains, not fixed by kind.
- **Lifecycle:** exact authored configuration/pose and zero velocity restore on Reset. Decoded save/load preserves all five fields and semantic rerun behavior. Rejected edits preserve revision, Undo and installed consumers.
- **Integration:** Receiver capture/miss, Switch impact/miss and the retained 0.5 kg Pressure plate one/two-ball control are required now.
- **Chrome recipe, deferred:** Free Workshop palette and lift-gizmo placement/configuration, matched ball lanes, Run/Reset and save/reload/Load remain post-catalogue obligations. The historical lane recipe is `tools/e2e/cat-014.test.ts@a6c914e:L105-L125`; it is not a browser gate for this slice.
- **Integrations.** Contact/cargo connection audit [sequence-task-285](../requirements.md#sequence-task-285) (balls of every type); interaction rows IX-01 contact impulse ([interaction-01](../requirements.md#interaction-01)), IX-02 sliding friction ([interaction-02](../requirements.md#interaction-02)) and, for the Fan lessons of Story 12.2, IX-11 aerodynamic drag ([interaction-11](../requirements.md#interaction-11)). Campaign first use: the "Basketball, bowling, tennis …" row of the [campaign element coverage](../requirements.md#campaign-element-coverage) — first use 1–10; combination and spaced reuse 21–30, 41–50, 91–100, 136–150.

Use independently justified quantity-specific tolerances, meaningful causal order and finite stall guards under the current game-fidelity policy. Historical exact arrival timing, replay identity and numerical wording in the harvest do not create current acceptance. No output hashes or bit-for-bit material-by-kind restrictions apply.

## 6. Historical implementation interpretation

The following pinned references explain retired assumptions only; none instructs implementation to recreate an enum member, engine material arm, fixed-by-kind input or shared artwork:

- Fixed material and prior range contracts: `engine/gpu/WorkshopConstruction.cs@a6c914e:L54-L62`; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54` and `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L120`.
- Shared legacy ball art: `parts/BallPart.cs@a6c914e:L9-L17`.
- Historical catalogue presentation: `parts/catalog/tennis.tres@a6c914e:L8-L14`; `engine/gpu/WorkshopInventory.cs@a6c914e:L47-L60`.
- Retired compilation/worker and admission locations: `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L32-L42`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L622-L765`; `engine/gpu/WorkbenchCapacity.cs@a6c914e:L34-L36`; `engine/gpu/WorkshopConnections.cs@a6c914e:L24-L24`.

Bounce, rolling resistance, configuration and art are resolved above and in CAT-064; no open default decision remains.
