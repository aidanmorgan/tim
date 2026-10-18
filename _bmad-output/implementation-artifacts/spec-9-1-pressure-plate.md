# Specification: Story 9.1 Pressure Plate Surface Contact Load Sensor (CAT-052)

## 1. Overview & Context

- **Goal:** Deliver CAT-052 Pressure Plate under the modern declarative Tri-Graph architecture.
- **Role in Contraptions:** The Pressure Plate is a weight-sensitive surface sensor that measures the direct resting mass on its top sensing face. When the mass equals or exceeds the threshold ($M \ge \text{minimum\_mass}$, default 0.5 kg, range 0.1–16 kg), the plate enters the `Loaded` phase and closes its internal electrical contact between `PowerIn` and `Supply`.
- **Zero Legacy:** No legacy classes, shims, or CPU fallback update loops. Operates via declarative data definitions, GLTF/tscn extracted colliders, and pure WebWorker simulation tracking.

## 2. Technical Architecture & Component Design

### 2.1 Visual & Structural Scene (`parts/scenes/pressure_plate.tscn`)
- Root node: `pressure_plate` (`MachinePart.cs`).
- Base Box: $2.2 \times 0.2 \times 2.2\text{ m}$ at $(0, -0.12, 0)$, Navy `#293954`.
- Top Box: $1.8 \times 0.1 \times 1.8\text{ m}$ at $(0, 0.03, 0)$, Cream `#fff8e9`.
- Studs: 4 spheres $r = 0.055\text{ m}$ at $(\pm 1, 0.015, \pm 1)$, Slate `#556573` (Empty), Ochre `#e8b764` (Underweight), Gold `#f7cb52` (Loaded).
- Sockets: Spheres $r = 0.075\text{ m}$ at $(\pm 1.18, -0.08, 0)$, `#e8b764`.
- Centre ring: $r = 0.3\text{ m}$, thickness $0.018\text{ m}$ at $(0, 0.084, 0)$, `#e8b764`.
- Port locators:
  - `Loc_Port_power_in` at $(-1.18, -0.08, 0)$.
  - `Loc_Port_supply` at $(1.18, -0.08, 0)$.
- Collision boxes:
  - Base: $2.2 \times 0.2 \times 2.2\text{ m}$ at $(0, -0.12, 0)$.
  - Top: $1.8 \times 0.1 \times 1.8\text{ m}$ at $(0, 0.03, 0)$.

### 2.2 Declarative Instance (`WorkshopPressurePlate.cs` & `PartId.cs`)
- `PartId.PressurePlate` (`"pressure_plate"`, numeric 21).
- `WorkshopPressurePlate`:
  - `Id`, `Cell`, `Local`, `Rotation`, `MinimumMass` (default 0.5 kg, range [0.1, 16]), `Locked`.
- `WorkshopPorts`:
  - `PowerIn` Electrical Input at $(-1.18, -0.08, 0)$.
  - `Supply` Electrical Output at $(1.18, -0.08, 0)$.
  - Refuses activation connections.

### 2.3 Contact Load Sensing (`ContactLoadSensorDeclaration.cs` & `ContactLoadTracker`)
- Region: $x \in [-0.9 - d, 0.9 + d]$, $y \in [0.08 - d, 0.08 + d]$, $z \in [-0.9 - 10^{-6}, 0.9 + 10^{-6}]$, $d = 0.0001\text{ m}$.
- Normal: $+Y$, alignment $\ge 1 - 10^{-6}$.
- Evaluation:
  - Tracks all dynamic bodies in the scene.
  - Transforms contact point into frame-local space.
  - Counts each directly touching body once by identity.
  - Sums mass: $M = \sum M_{\text{body}}$.
  - Phase:
    - $M = 0 \implies \text{Empty}$
    - $0 < M < \text{minimum\_mass} \implies \text{Underweight}$
    - $M \ge \text{minimum\_mass} \implies \text{Loaded}$
  - Bouncing impact / non-resting rejection: upward velocity or non-contacting points are excluded.

### 2.4 Electrical Switch Routing
- While `Loaded`, closed contact connects `PowerIn` to `Supply`.
- When an electrical source (e.g. Battery) is connected to `PowerIn`, downstream consumers connected to `Supply` receive power when `Loaded`.
- Unsupplied plate: `Loaded` state updates studs to gold, but no downstream power is transmitted.

## 3. Verification Plan

1. **Unit Tests (`CuriousContraptions.tests/PressurePlateTests.cs`):**
   - Minimum mass validation (rejects <= 0, > 16, NaN, Infinity).
   - Positive resting load (Ball $\ge 0.5\text{ kg}$) closes contact; multiple balls sum mass.
   - Underweight control (Tennis ball $0.35\text{ kg} < 0.5\text{ kg}$) produces `Underweight`, contact remains open.
   - Off-face contacts (side, underside, gap, outside) do NOT trigger.
   - Rotated mounting correctly transforms local face coordinates.
   - Removal / lift off clears reading to `Empty` on subsequent tick.
   - Reset clears all accumulated load to `Empty`.
2. **E2E Playwright Suite (`tools/e2e/cat-052.test.ts`):**
   - Workbench placement of Pressure Plate, Battery, and Powered Gate / Lamp.
   - Wiring: Battery `Supply` -> Pressure Plate `PowerIn`; Pressure Plate `Supply` -> Powered Gate `PowerIn`.
   - Positive test: dropping Bowling ball on plate triggers gate opening; removing ball closes gate.
   - Negative test: Domino or light mass below threshold leaves gate closed.
   - Reset restores authoring state; Save/Load roundtrip preserves `minimum_mass` and connections.
3. **Anvil Verification:**
   - `anvil check` reports 0 errors and 0 warnings.
