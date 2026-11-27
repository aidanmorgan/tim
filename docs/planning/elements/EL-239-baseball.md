# EL-239 · Baseball

## 1. Identity and disposition

- **Named identity:** [EL-239 · Baseball](../invest/named-elements.md#element-239) (`baseball`, Specialist, campaign addition; first use 1–10).
- **Sources:** [todo-198](../requirements.md#todo-198), [sequence-task-530](../requirements.md#sequence-task-530), [S636](../invest/scope-corrections.md#s636), and the owner decisions of 21 and 22 November 2026 (0.15 kg, radius 0.26 m, one proud band with its own collider, "the lightest ball").
- **Disposition:** Single element package (`elements/Baseball/`), delivered and repaired to the owner decisions; browser qualification pending post-catalogue.

## 2. Declaration

- **Metadata:** Id `baseball`, category Motion, name `Baseball`, palette order 68, description `"The lightest ball. A fan carries it easily and it lands with a low bounce."`, icon `res://elements/Baseball/icon.svg` (navy stitched-ball icon), scene `res://elements/Baseball/baseball.tscn`, pick radius 0.36 m, no parameters, no ports, no events.
- **Art and solidity (`baseball.tscn`):**
  - `Visual/Sphere`: `SphereMesh`, cream (`Color(0.9098039, 0.8627451, 0.7960784, 1)`), radius 0.26 m, height 0.52 m, paired with `col_sphere` (`SphereShape3D`, radius 0.26 m).
  - `Visual/Band`: `TorusMesh`, coral-red (`Color(0.8509804, 0.4196078, 0.345098, 1)`), inner radius 0.242 m, outer radius 0.27 m (1 cm proud of the sphere), quaternion `(0, 0, -0.3007058, 0.95371695)`, paired with `col_visual_band` (`visible = false`, same mesh and quaternion).
  - No other visual or collider nodes exist; the root visual follows its sole committed physical body.
- **Behaviour and numbers:**
  - `KinematicMode.Dynamic`, `PayloadPolicy.CaptureTarget`.
  - Mass 0.15 kg, constant principal inertia `0.004056` kg·m² (`0.4 * 0.15 * 0.26^2`) on each axis, gravity `(0, -9.81, 0)` m/s².
  - Material: restitution 0.35, friction 0.3, rolling resistance 0.03, bounce threshold 0.1 m/s.
  - Ambient linear drag: 0.04 /s, no buoyancy.
  - Airflow receiver on collider 0 (`col_sphere`) with four normalized surface sample rays.

## 3. Engine capabilities

1. Sphere collider read from the package scene — exists (`CuriousContraptions.Abstractions/SceneGeometry.cs`).
2. A proud band drawn as a torus with its own torus collider on a dynamic ball — exists (`tools/BundleElements/SceneGeometryExtractor.cs`).
3. Constant mass frame independent of collider children — exists (`CuriousContraptions.Abstractions/MassFrameValues.cs`).
4. Collider material shared by a body's colliders — exists (`CuriousContraptions.Abstractions/ColliderDescriptor.cs`).
5. Airflow force on a receiver, falling linearly to zero at the flow speed — exists (`CuriousContraptions.Abstractions/ConicalAirflow.cs`, `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs`).
6. Contact-load threshold on summed declared mass, inclusive — exists (`engine/gpu/ContactLoadSensorDeclaration.cs`).
7. Chamber seating by declared width window over every collider of the payload — exists (`CuriousContraptions.Simulation/WorkshopPhysicsEngine.Chambers.cs`).
8. Visible solid equals collider, including a torus — exists (`CuriousContraptions.tests/VisibleSolidColliderTests.cs`).
9. Zero-parameter instance; forged parameter refused in edit and save — exists (`elements/Baseball/BaseballElementDeclaration.cs`).
10. Run, Pause, Step, Reset, Save, Load of a payload — exists (`engine/gpu/WorkshopSaveCodec.cs`).
11. Coverage admits the package through its spec's binding section — exists (`tools/Coverage/NamedPackageSources.cs`).
12. Generic per-ray airflow occlusion excludes the blocked sample's own demand — exists (`AirflowLineClear` in `CuriousContraptions.Simulation/WorkshopPhysicsEngine.Airflow.cs`).
13. Receiver capture of any dynamic payload by its residence sensor, after a declared dwell — exists (`elements/Receiver/ReceiverElementDeclaration.cs`).

## 4. Legacy harvest

none, the Baseball has no legacy implementation; Files harvested: none.

## 5. Acceptance outline

- **R1 (Declared mass, inertia, material, refusal):** `BaseballTests.FixedPresetCompilesCanonicalMassAndMaterialAndRejectsForeignParameters` — 0.15 kg mass, 0.004056 kg·m² inertia, two colliders sharing one material, typed refusals for non-finite pose and foreign/unknown parameters.
- **R2 (Art and collider match):** `BaseballTests.SceneAndIconExposeBandedSphereMatchingContactGeometry` and `VisibleSolidColliderTests.EverySolidArtPartHasItsColliderAndNoColliderReachesBeyondTheArt(baseball)` — 0.26 m sphere and 0.242/0.27 m torus band, no seam nodes.
- **R3 (S636 bounce comparison):** `BaseballTests.IdenticalDropDistinguishesEveryRetainedPreset` — first rebound peak separated by at least 0.15 m from Basketball, Bowling ball and Tennis ball, with Balloon rising.
- **R4 (Lightest ball in airflow):** `BaseballTests.TheFanMovesTheBaseballMoreThanTheTennisBallAndTheBasketball` and `FanGuidanceTests.ExistingPayloadsReceivePaidLateralGuidanceOnlyWithEnabledFlow` — at least 1.5× Tennis ball speed and 3× Basketball speed after 12 ticks; zero when Fan is off.
- **R5 (Lightest ball on Pressure plate):** `BaseballTests.TwoBaseballsLeaveADefaultPlateOff` — one or two Baseballs leave a default 0.5 kg plate off (resting on the plate), two Tennis balls switch it, and a 0.25 kg plate switches for two Baseballs but not one.
- **R6 (Cannon non-seating):** `BaseballTests.BaseballNeverSeatsInTheCannon` — 0.52–0.54 m across the band never seats in the 0.6 m minimum Cannon chamber while Basketball seats.
- **R7 (Drop and rest):** `BaseballTests.UnpoweredDropSettlesWithoutFreeRebound` — settles within 1 mm of 0.26 m resting height and below 0.05 m/s.
- **R8 (Lifecycle and presentation):** `BaseballTests.RunPauseStepResetSaveLoadRestoresAuthoredBaseball` and `BaseballTests.CommittedPresentationFollowsMovingBodyAndReset`.
- **R9 (Receiver capture):** `BaseballTests.AnAlignedBaseballIsCapturedAndAShiftedOneIsNot` — aligned Baseball captured once within 240 ticks after the 0.35 s dwell; shifted control never captured.

## 6. Open questions

none.

## Package source binding

**Package:** baseball.
**Named identity:** [EL-239](../invest/named-elements.md#element-239).
**Acceptance:** [todo-198](../requirements.md#todo-198), [sequence-task-530](../requirements.md#sequence-task-530).
**Decision:** [S636](../invest/scope-corrections.md#s636).
**Mode records:** Configuration=Fixed.
