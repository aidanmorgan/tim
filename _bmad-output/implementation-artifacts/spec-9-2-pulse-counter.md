# Specification: Story 9.2 Pulse Counter Event Accumulator (CAT-020)

## 1. Overview & Context

- **Goal:** Deliver CAT-020 Pulse Counter Event Accumulator under the modern declarative Tri-Graph architecture.
- **Role in Contraptions:** The Pulse Counter is an event accumulator that accepts trigger activations on its `ActivationIn` port, counts accepted events up to an authored target ($N \in [1, 9]$, default 3), and upon reaching $N$, emits a single activation pulse through `ActivationOut` and latches an internal series electrical contact between `PowerIn` and `Supply`. Additional pulses beyond $N$ saturate the counter without re-emitting.
- **Zero Legacy:** No legacy classes, shims, or CPU fallback update loops. Operates via declarative data definitions, GLTF/tscn extracted colliders, and pure WebWorker simulation tracking.

## 2. Technical Architecture & Component Design

### 2.1 Visual & Structural Scene (`parts/scenes/counter.tscn`)
- Root node: `counter` (`MachinePart.cs`).
- Base Foot Box: $1.8 \times 0.16 \times 0.85\text{ m}$ at $(0, -0.72, 0)$, Navy `#293954`.
- Housing Root Box: $1.65 \times 1.25 \times 0.65\text{ m}$ at $(0, 0, 0)$, Ochre `#e8b764`.
- Front Face: $1.4 \times 1.02 \times 0.04\text{ m}$ at $(0, 0, 0.35)$, Cream `#fff8e9`.
- Dot Lamp Indicators: Spheres $r = 0.10\text{ m}$ at $z = 0.4\text{ m}$ in rows of 3:
  - Dot layout for lamp $i \in [0, N-1]$:
    $\text{row} = \lfloor i / 3 \rfloor$; $\text{rows} = \lfloor (N + 2) / 3 \rfloor$; $\text{cols} = \min(3, N - 3 \cdot \text{row})$;
    $x = (i \pmod 3 - (\text{cols} - 1)/2) \cdot 0.36$;
    $y = ((\text{rows} - 1)/2 - \text{row}) \cdot 0.3$;
    $z = 0.4\text{ m}$.
  - Colors: Slate `#556573` (unreached) $\to$ Gold `#f7cb52` (reached $M \ge i + 1$).
- Sockets and Port locators:
  - `Loc_Port_activation_in` at $(0, 0.72, 0.1)$.
  - `Loc_Port_activation_out` at $(0, -0.72, 0.1)$.
  - `Loc_Port_power_in` at $(-0.93, 0, 0)$.
  - `Loc_Port_supply` at $(0.93, 0, 0)$.
- Collision boxes (Static):
  - Housing: $1.65 \times 1.25 \times 0.65\text{ m}$ at $(0, 0, 0)$.
  - Foot: $1.8 \times 0.16 \times 0.85\text{ m}$ at $(0, -0.72, 0)$.

### 2.2 Catalogue & Declarative Instance (`WorkshopCounter.cs` & `PartId.cs`)
- `PartId.Counter` (`"counter"`, numeric 22).
- `WorkshopCounter`:
  - `Id`: `GpuBodyId`
  - `Cell`: `IntVector3`
  - `Local`: `MetreVector`
  - `Rotation`: `CanonicalRotation`
  - `TargetCount`: int in range [1, 9], default 3.
  - `Locked`: bool
- `WorkshopPorts`:
  - `ActivationIn` (Input, Activation) at $(0, 0.72, 0.1)$.
  - `ActivationOut` (Output, Activation) at $(0, -0.72, 0.1)$.
  - `PowerIn` (Input, Electrical) at $(-0.93, 0, 0)$.
  - `Supply` (Output, Electrical) at $(0.93, 0, 0)$.

### 2.3 Activation Network & Event Accumulation
- Accepts incoming activations on `ActivationIn`.
- Edge/event filtering: a continuous held input does not increment repeatedly; distinct physical trigger events or unique timestamps increment the counter by 1.
- States:
  - $0 \le \text{Count} < N \implies \text{Counting}$. Contact open.
  - $\text{Count} = N \implies \text{Reached}$. Contact closed. Emits single activation through `ActivationOut`.
  - $\text{Count} > N \implies \text{Saturated}$. Contact remains closed; no further emissions.
- Reset restoration: clears Count to 0, state to `Counting`, opens electrical contact, and resets dot lamps to Slate.

### 2.4 Electrical Switch Routing
- While `Reached` or `Saturated`, closed contact connects `PowerIn` to `Supply`.
- If an electrical source (Battery) is connected to `PowerIn`, downstream consumers connected to `Supply` receive power once target count is reached.
- Unsupplied counter: functions normally as an activation counter, lighting dot lamps and emitting activation pulse, but transmits no electrical power.

## 3. Verification Plan

1. **Unit Tests (`CuriousContraptions.tests/CounterTests.cs`):**
   - Parameter boundary validation: target 1–9 accepted; $\le 0, \ge 10$, non-integers, NaN rejected.
   - Counting progression: distinct triggers increment count; target emission fired exactly once.
   - Saturated state: 20 additional triggers leave state unchanged and do not re-emit.
   - Electrical contact: closed only when `Reached`/`Saturated` and power is supplied.
   - Reset restoration: restores count to 0, contact open.
   - Save/Load roundtrip: preserves `target_count` without persisting runtime transient counts.
2. **E2E Playwright Suite (`tools/e2e/cat-020.test.ts`):**
   - Place Ball Detector, Counter (target 3), Signal Lamp, and Powered Gate on Workbench.
   - Wire Ball Detector -> Counter -> Signal Lamp (Activation domain).
   - Wire Battery -> Counter -> Powered Gate (Electrical domain).
   - Negative control: 2 ball crossings do not emit, gate closed, lamp unlit, 2 dots lit.
   - Positive trigger: 3rd ball crossing emits activation, lamp illuminates, gate opens.
   - Saturation: 4th ball crossing does not glitch or re-trigger.
   - Exact Reset restoration and Save/Load persistence in Chrome.
3. **Anvil Verification:**
   - 0 errors, 0 warnings across all modified and newly created files.
