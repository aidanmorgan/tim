# Story 6.8: Generic Aperture Sensor with Directional Rearm (CAT-002)

## Status: Ready for Implementation

## Context & Purpose
Following the delivery of the Straight Pipe (Story 6.6) and Clear Pipe level (Story 6.7), Story 6.8 delivers **CAT-002: Ball Detector**, an optical passage sensor housed in a hollow cylindrical collar that detects balls passing forward through its aperture and emits an activation signal to connected components (e.g. Signal Lamp).

In strict accordance with the project rules:
1. **Strict Visual/Collider Entity Separation:** Presentation meshes (`Visual_Collar`, `Visual_Housing`, `Visual_Chevron`, `Visual_Socket`) are grouped under `Visual/` and decoupled from physics colliders. Physics colliders are dedicated sibling entities (`col_*` with `visible = false`).
2. **Zero Backwards Compatibility:** No per-part C# classes (`BallDetectorPart.cs`), no custom sensor kernels. Uses generic endpoint-sampled sensor evaluation (Story 3.1 architecture) and standard `ActivationNetwork`.
3. **Discrete Aperture Sensor Mechanics:**
   - Detects forward crossing ($+X$) of the local YZ plane within bore radius $R \le 0.65$ m.
   - Rearms only after whole-body clearance upstream ($X < -0.02 - \text{radius}$).
   - Reverse motion or glancing outside passages do not trigger.
   - Forward crossing emits one discrete activation pulse on `ActivationOut`.

---

## Acceptance Criteria

1. **Declarative Catalog & Scene Definition:**
   - Authored `parts/catalog/ball_detector.tres` defining `PartDefinition` with `Id = "ball_detector"`, `Title = "Ball detector"`, `Category = "Control"`, pointing to `res://parts/scenes/ball_detector.tscn`.
   - Authored `parts/scenes/ball_detector.tscn` rooted on `MachinePart` (`res://engine/MachinePart.cs`):
     - `Visual/`: Presentation meshes (`Visual_Collar` cream `#fff8e9` collar length 0.56 m, outer radius 0.82 m, bore 0.65 m; `Visual_Housing` cyan `#66b8c9` box at `(0, 0.93, 0)`; `Visual_Chevron` navy `#293954` forward arrow; `Visual_Socket` gold `#f7cb52` sphere at `(0, 1.12, 0)`).
     - `col_*` (`visible = false`): 16 circumferential tangent box colliders forming a hollow cylindrical channel of bore radius 0.65 m (length 0.56 m along X), plus a housing box collider at $(0, 0.93, 0)$.
     - Port locators:
       - `Loc_Port_ActivationOut` at `(0, 1.12, 0)`
       - `Loc_Port_mouth_start` at `(-0.28, 0, 0)`
       - `Loc_Port_mouth_end` at `(0.28, 0, 0)`

2. **Aperture Passage Sensor & Activation Integration:**
   - Evaluates forward passage ($+X$) across the aperture plane ($X = 0$) within radius 0.65 m at substep endpoints.
   - Discharges an activation signal on `ActivationOut` upon forward crossing.
   - Rearms only after the dynamic body completely clears upstream ($X < -0.02 - \text{radius}$).
   - Reverse motion ($-X$) or glancing outside trajectories do NOT trigger the sensor.

3. **Pure TypeScript Playwright E2E Suite (`tools/e2e/cat-002.test.ts`):**
   - 1. Construction & Wiring: Place Ball detector and Signal lamp in Free Workshop. Connect detector `ActivationOut` $\to$ lamp `ActivationIn`.
   - 2. Forward pass: Ball dropped/rolled through detector along $+X$. Lamp illuminates once (verified via telemetry or screenshot/animation).
   - 3. Negative controls: Reverse pass (from $+X$ to $-X$) does NOT illuminate lamp. Glancing outside pass does NOT illuminate lamp.
   - 4. Clearance & rearm: Ball clears upstream, passes again $\to$ second illumination.
   - 5. Bit-for-bit Reset replay and Save/Load IndexedDB persistence roundtrip.
   - Passes 100% in real Chrome on port 8060.

4. **Quality Gates:**
   - `anvil check` outputs 0 warnings.
   - `dotnet test CuriousContraptions.slnx` passes 100%.
   - Cumulative Playwright E2E suites pass 100% serially in real Chrome (`cat-002`, `cat-048b`, `cat-048a`, `cat-062`, `cat-015a`, `cat-023a`, `cat-014`, `engine-core-2a1`, `engine-core-2c`).
