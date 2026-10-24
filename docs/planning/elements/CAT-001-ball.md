# CAT-001 · ball (Basketball) — declaration readiness spec

## 1. Identity

| Field | Value |
| --- | --- |
| CAT ID / kind | CAT-001 · `ball` (display name Basketball) |
| Requirement | [CAT-001](../requirements.md#current-cat-001) |
| Mapped identities | EL-187 Basketball. Related but separate identities with their own specs: EL-076 Programmable ball, EL-077 Soccer ball, EL-091 Cannonball, EL-098 Pool ball. |
| Roadmap | [CAT-001-I](../invest/vertical-delivery.md#ball-drop); Epics 1–2 (ENGINE-CORE-1, 2a1–2a4, 2b1–2b4) |
| Status | delivered; remaining items in §5 |
| Levels | placed in most campaign levels; see [CAT-001-I consumers](../invest/current-consumers.md#cat-001-i) |

## 2. Declaration

| Item | Value and source |
| --- | --- |
| Body and shape | A zero-parameter `WorkshopGenericInstance` of the `ball` package (C-Balls, Story 9A.30): one dynamic homogeneous sphere, radius 0.34 m, declared only by `elements/Ball/BallElementDeclaration.cs` (collider baked from `elements/Ball/ball.tscn`); the shared instance loop of `WorkshopPhysicsCompiler.Compile` compiles its dynamic body, one sphere collider with identity local pose and its material from that declaration, with no ball-specific path. A dynamic body admits exactly one homogeneous sphere or box `engine/gpu/PhysicsDeclarations.cs@a6c914e:L220-L234`. |
| Mass and material | 1 kg; bounce (restitution) 0.55; linear drag 0.04 1/s; no fluid declaration (not buoyant); friction 0.3; bounce threshold 0.1 m/s; rolling resistance 0.035, all package-declared (owner decision 9 Oct 2026). No save, command or content record carries a material: a generic record carrying any typed field or any parameter is refused (`WorkshopWireFlatBuffers.CarriesTypedField`, shared by the decoder and `WorkshopSavePrevalidation`). Gravity 9.81 m/s². Admission bounds: mass 1/1024–1024 kg, drag 0–0.125 `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L86`; restitution and friction 0–1, rolling resistance 0–0.1 `engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`. |
| Constraints and joints | none: a free rigid body. |
| Sockets and ports | none: `WorkshopPorts.For` returns no ports for Basketball `engine/gpu/WorkshopConnections.cs@a6c914e:L18-L26`. |
| Sensors and activation | none owned. It is a payload: `DeclaredPhysics.IsPayload` derives it from the admitted declaration (dynamic, exactly one sphere collider, no slider; a Domino or slider body is not one). Every capture owner (CAT-004 Receiver) declares a residence sensor and guide per payload in ascending identity; in a lesson, Impact switch triggers (CAT-063) and Bumper contact work (CAT-015) name the first payload in instance order, and act on every dynamic body in free play; presentation telemetry follows the lowest-identity payload. |
| Work and energy stores | none. |
| Parameters | none (package `Parameters: []`, decided by game intent in C-Balls): ball kinds teach by differing only in declared material, so the generic editor shows no panel. The former `radius`/`drag`/`buoyancy` save fields are deleted; pre-conversion saves are refused, never migrated. |
| Cosmetic curves and UI bindings | none: `CosmeticCurveDeclaration.None` `engine/gpu/WorkshopConstruction.cs@a6c914e:L67-L77`. |
| Art | Package scene `elements/Ball/ball.tscn`: a sphere mesh of radius 0.34 m committed equal to the baked collider (no runtime resize), with a darker stripe torus (inner 0.322, outer 0.35 m). Palette Ball `#f57d38` (0.96, 0.49, 0.22) `DESIGN.md@a6c914e:L165-L165`. Icon `ui/WorkshopIcons.cs@a6c914e:L39-L39`. |
| Catalogue and inventory | `parts/catalog/ball.tres@a6c914e:L8-L27`: id `ball`, title Basketball, category Motion, material bits equal to the declared kind. Free workshop: unlimited inventory, bounded by typed workbench capacity (`engine/gpu/WorkbenchCapacity.cs`). |
| Placement | Positions are stored as 1/16 m cells plus a local offset `engine/gpu/WorkshopConstruction.cs@a6c914e:L220-L229`; workbench surface y = −0.46 m `engine/gpu/WorkshopConstruction.cs@a6c914e:L123-L124`, compiled as a static plane `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L22-L30`. |

Current tests: `CuriousContraptions.tests/CanonicalBodyTests.cs`, `RigidMassPropertiesTests.cs`, `WorkshopSimulationTests.cs`, `WorkshopSaveTests.cs`, `BasketballResourceTests.cs`, `WorkbenchCapacityTests.cs`, `WorkshopPhysicsEngineTests.cs` and `WorkshopPhysicsLaw*Tests.cs` (native steps of the shipped C# engine: dissipative bounce and rest at the radius, free-flight drag, rolling resistance, the speed envelope, Bumper ledger; `WorkshopPhysicsLawSensorWorkTests` adds paid contact work). Story 9.4b fixed two engine defects these tests exposed: a sphere's contact support rotated with the body, so a fast-rolling ball lost its support, and the end-of-substep guard zeroed a finite velocity whose f32 squared length overflowed; Chrome `tools/e2e/engine-core-2a1.test.ts`, `engine-core-2a2.test.ts`, `engine-core-2a4.test.ts`, `engine-core-2b1.test.ts`, `engine-core-2b2.test.ts`, `cat-014.test.ts` (Basketball control lane).

## 3. Engine capabilities

Families from the [element map row](../general-engine-element-map.md): AerodynamicDrag, Buoyancy, ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, RigidBodyDynamics, SlidingFriction.

| Exists now | Reference |
| --- | --- |
| RigidBodyDynamics, ContactImpulse, SlidingFriction (sphere vs plane/box/sphere, TGS Soft, speculative contacts, rolling resistance) | `CuriousContraptions.Simulation/wwwroot/worker.js`; Stories 1.1–1.5, 6.1b |
| AerodynamicDrag (declared linear drag per substep) | Story 6.1b; `engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L60` |
| GeometryQuery, FiniteLedger (pose ring, committed reads) | `engine/gpu/WorkshopPoseRing.cs`, `engine/gpu/PhysicsMotionRead.cs` |

| Missing | Story that builds it |
| --- | --- |
| Buoyancy (declared zero today; non-zero needs fluid/gas boundary S416/S470) | Epic 12 (Story 12.1 introduces buoyant bodies) |
| EnvironmentState beyond fixed gravity (atmosphere/pressure) | Epic 12; EL-125 authored atmosphere preset (Batch M) |
| Hollow pipe contact for the ramp/pipe/Receiver integration | Stories 6.6, 6.7 |
| f32 declarations: the package declares f32; the compiled body, collider and material lanes (`Metres`, `Kilograms`, `Restitution`, `FrictionCoefficient`, `RollingResistance`) are f32 (Story 9A.44) | Delivered by Story 9A.44 ([numeric authority](../../gpu-f32-physics.md)) |

Dependencies: none. Consumers: Receiver, Impact switch, Bumper, Domino, Ramp, Wall, Pipe family.

## 4. Legacy harvest

| # | Fact | Citation | Disposition |
| --- | --- | --- | --- |
| 1 | The legacy ball declared five authorable fields Radius, Mass, Bounce, Buoyancy, Drag; a ball with Buoyancy > 0 drew a short cream tether line below it. | `reference/cpu/BallPart.cs@a6c914e:L4-L28` | carry forward the five fields and the buoyancy tether art; do not carry forward the string-keyed parameter dictionary (string-typed closed set) or the CPU `BodyDynamics.SolidSphere` path. |
| 2 | An isolated drop from y = 6 at zero pressure reaches first impact at √(2h/g) ± 0.02 s and rebounds to a height ratio of bounce² ± 0.015; samples replay identically after Reset; the ball stays at x = z = 0. | `CuriousContraptions.tests/PhysicsCalibrationTests.cs@a6c914e:L24-L67` | carry forward as acceptance facts (game-grade tolerances); applies to ball, bowling and tennis. |
| 3 | A ball bounces on the visible bench, never penetrates the surface by more than 0.001 m, and settles with rest height within +0.002 m and speed ≤ 0.05 m/s. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L27-L48` | carry forward. |
| 4 | A 40 m/s downward ball is stopped by the bench and rebounds; Reset restores the start position and initial velocity; the bench is not a saved part. | `CuriousContraptions.tests/FloorTests.cs@a6c914e:L69-L85` | carry forward. |
| 5 | The legacy bench was finite: deck half extents 8.4 × 0.03 × 4.9 m at y −0.49 (surface −0.46), base 8.5 × 0.175 × 5 m at y −0.7. A ball placed at x = 10 or z = 6 falls past it, is hidden and records an Escaped event. | `reference/cpu/Workbench.cs@a6c914e:L6-L12`; `CuriousContraptions.tests/FloorTests.cs@a6c914e:L53-L67` | surface height carried forward (current −0.46 m). Finite edges and the Escaped event: see §6 (current workbench is an infinite plane). |
| 6 | Depth separates collisions: a ramp at z = −2 does not alter a ball falling at z = 2; positions and velocities match a ball-only run every tick. | `CuriousContraptions.tests/BasicTests.cs@a6c914e:L204-L234` | carry forward. |
| 7 | A decoy ball captured by the Receiver records a Captured event but does not satisfy a goal naming another ball. | `CuriousContraptions.tests/BasicTests.cs@a6c914e:L236-L258` | carry forward (shared with CAT-004). |
| 8 | Linear drag only removes kinetic energy and replays exactly; invalid drag rates reject. | `CuriousContraptions.tests/BodyDragTests.cs@a6c914e:L12-L48` | carry forward the behaviour; do not carry forward the CPU `BodyDragLoad` midpoint integration (CPU solver path). |
| 9 | The legacy ball accepted a per-instance `radius` property (0.34 and 0.8 used in pipe tests). | `CuriousContraptions.tests/PipeTests.cs@a6c914e:L56-L70` | do not carry forward as an instance property: current balls declare material per kind; an oversize payload is a separate declared kind or owner decision (§6). |
| 10 | The CAT-001-I r1 candidate is preserved as failed on R-N3 with a later hint regression; its native checks did not exercise the hint semantics. | `reference/CAT-001-I-r1/README.md@a6c914e:L1-L17` | carry forward the status fact only (requirement retains R-N3 Fail); preserved UI sources and DLLs are build evidence, no element knowledge. |

Files harvested:
- `reference/cpu/BallPart.cs`
- `reference/cpu/Workbench.cs`
- `reference/CAT-001-I-r1/` (README.md; ui/*.cs, TODO.md, csproj and artifacts/*.dll hold no element knowledge)
- `CuriousContraptions.tests/PhysicsCalibrationTests.cs`
- `CuriousContraptions.tests/FloorTests.cs`
- `CuriousContraptions.tests/BasicTests.cs`
- `CuriousContraptions.tests/BodyDragTests.cs`
- `CuriousContraptions.tests/PipeTests.cs`

## 5. Acceptance outline

Acceptance: [CAT-001](../requirements.md#current-cat-001) and [simulation and element acceptance](../requirements.md#accept-simulation).

- **Construction (Chrome UI).** Free workshop → catalogue → Basketball → place above the bench with the move widget → Run.
- **Positive.** Gravity fall, bench bounce (second apex lower than first), rolling to rest, momentum transfer to a Domino or second ball.
- **Negative/control.** Remove the ball and Run: no body, no contact. Two placement heights give the expected fall/contact order.
- **Boundaries.** Highest and lowest legal placement; 40 m/s impact does not tunnel (Story 1.5 proof).
- **Run/Reset, Save/Load.** Exact canonical pose after Reset; Save/reload/Load restores kind and pose.
- **Integrations.** Ramp (CAT-054), Receiver (CAT-004), Impact switch (CAT-063), Bumper (CAT-015), Domino (CAT-023), pipe family (CAT-048/049/050).
- **Remaining (unmet now).** R-N3 Fail retained until independently resolved; pipe integration waits for Stories 6.6/6.7; non-zero buoyancy and atmosphere integrations wait for Epic 12; visible stripe follows committed pose (proven via presentation, keep in regression).

## 6. Open questions

- Decided (C-Balls, 22 Oct 2026, by game intent): no material field is player- or author-editable per instance (legacy facts 1, 9); each kind's material is its package declaration and the kinds teach by differing only in that.
- Finite workbench edges and the Escaped event (legacy fact 5) versus the current infinite plane: unspecified — owner decision.
- R-N3: its definition is not retrievable at `a6c914e` (the CAT-001-I verification documents were deleted earlier); the requirement keeps it as Fail. Owner to restate or retire R-N3.
