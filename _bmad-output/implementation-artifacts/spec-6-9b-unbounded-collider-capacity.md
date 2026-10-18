# Specification: Story 6.9b — Unbounded Collider Capacity & Dynamic ABI Offsets

## 1. Context & Motivation
Under the legacy fixed-capacity GPU prototype architecture, colliders were statically capped at 64 (`PhysicsSceneDeclaration.ColliderCapacity = 64`). Downstream table offsets in `PhysicsGpuAbi.cs` and `worker.js` were hardcoded based on this 64-collider table reservation. In `WorkbenchCapacity.cs`, player contraptions exceeding 64 colliders were rejected with `WorkbenchFullException(WorkbenchTable.Colliders)`.

With compound physics elements introduced in Epic 6 (Straight Pipe = 24 colliders, Ball Detector = 17 colliders, 45° Pipe Bend = 40 colliders), placing just 2 pipe bends (80 colliders) or 3 straight pipes (72 colliders) hits this artificial ceiling and fails validation.

User Directive: **"there should be no limit on colliders in the game"**.
Colliders are internal physics proxy shapes, not player resources or authoring constraints. Players should never be blocked from placing valid parts due to an internal collider table ceiling.

## 2. Acceptance Criteria
1. **No Collider Limitation in Authoring:**
   - Remove `WorkbenchTable.Colliders` validation from `WorkbenchCapacity.cs`. Authoring validation never rejects a construction based on collider counts.
   - `WorkbenchCapacity.Validate()` permits constructions containing any number of compound parts up to the 32-instance budget (e.g. 32 straight pipes = 768 colliders, or 32 pipe bends = 1,280 colliders).
2. **Dynamic ABI Section Offsets:**
   - `Program.cs` exports `AbiOffsets()` returning `[BodiesOffset, CollidersOffset, MaterialsOffset, SensorsOffset, GuidesOffset, TriggersOffset, ContactWorksOffset, OrientationSensorsOffset, MotionOffset, ElectricalSourcesOffset, ElectricalBindingsOffset, PrismaticsOffset, PrismaticRecordsOffset]`.
   - `worker.js` queries `host.AbiOffsets()` upon startup and binds all offset variables dynamically, eliminating all hardcoded magic numbers (`5408`, `11552`, `15680`, `16192`, `20800`, `21824`, etc.).
3. **Scaled Physics State Buffer:**
   - `PhysicsSceneDeclaration.ColliderCapacity` scaled to `2048` (or higher), easily accommodating up to 32 compound puzzle elements without table exhaustion or memory corruption.
4. **Clean Zero-Remnant Test Updates:**
   - Tests asserting the legacy 64 ceiling (`WorkbenchCapacityTests.cs`, `WorkshopWallTests.cs`, `StraightPipeTests.cs`, `BallDetectorTests.cs`, `PipeBend45Tests.cs`) are updated.
   - New unit tests confirm that heavy compound constructions (e.g. 10+ pipes or pipe bends) compile and admit smoothly.
5. **Quality Gates:**
   - `anvil check --changed` reports 0 warnings.
   - 100% `dotnet test CuriousContraptions.slnx` pass rate.
   - 100% Chrome Playwright E2E pass rate across existing suites.
