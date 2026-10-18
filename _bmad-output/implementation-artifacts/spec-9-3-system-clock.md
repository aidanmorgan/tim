# Specification: Story 9.3 System Clock Periodic Pulse Generator (CAT-017)

## 1. Overview & Context

- **Goal:** Deliver CAT-017 System Clock Periodic Pulse Generator ("Repeating clock") under the modern declarative Tri-Graph architecture and modular element isolation pattern.
- **Role in Contraptions:** The System Clock emits discrete activation pulses at regular intervals (`interval_seconds` $\in [0.1, 12.0]\text{ s}$, default 1.0 s) when energized by an electrical supply connected to its `PowerIn` port. It drives downstream synchronous mechanisms, counters, and pulse-timed actuators via its `ActivationOut` port.
- **Power Coupling:** Powered periodic source. Emits triggers only while `PowerIn` is energized. Power loss immediately stops the clock and resets timing phase without emitting; power restoration starts a fresh full interval with zero catch-up pulses.
- **Zero Legacy:** No legacy classes, compatibility shims, CPU fallback update loops, or string-based dispatch. Follows strict element packaging (`elements/SystemClock/`), pure C# WASM SIMD physics, and lock-free multi-consumer event ringbuffer streaming.

## 2. Technical Architecture & Component Design

### 2.1 Visual & Structural Scene (`parts/scenes/clock.tscn`)
- Root node: `clock` (`MachinePart.cs`).
- Base Foot Box: $1.6 \times 0.2 \times 0.85\text{ m}$ at $(0, -0.95, 0)$, Navy `#293954`.
- Housing Root Box: $1.4 \times 1.7 \times 0.6\text{ m}$ at $(0, 0, 0)$, Ochre `#e8b764`.
- Front Face: $1.14 \times 1.44 \times 0.04\text{ m}$ at $(0, 0, 0.32)$, Cream `#fff8e9`.
- Pendulum Recess: $0.84 \times 1.1 \times 0.02\text{ m}$ at $(0, -0.06, 0.35)$, Slate `#556573`.
- Pendulum Assembly (Cosmetic):
  - Pivot at $(0, 0.4, 0.39)$.
  - Rod: $0.045 \times 0.7 \times 0.035\text{ m}$ (centered at $y = -0.35$ relative to pivot), Cream `#fff8e9`.
  - Bob: Sphere $r = 0.15\text{ m}$ at $(0, -0.7, 0)$ relative to pivot, Gold `#f7cb52`.
  - Pivot Cap: Sphere $r = 0.055\text{ m}$ at $(0, 0.4, 0.44)$, Navy `#293954`.
  - Motion: Swings according to committed oscillator progress: target $\theta = 0.42 \cdot \sin(2\pi \cdot \text{progress})\text{ rad}$.
- Pulse Indicator Lamp:
  - Sphere $r = 0.065\text{ m}$ at $(0, 0.63, 0.39)$.
  - Colors: Slate `#556573` (idle) $\to$ Gold `#f7cb52` flash on each pulse emission.
- Port Locators:
  - `Loc_Port_power_in` at $(-0.78, 0, 0)$, Electrical Input.
  - `Loc_Port_activation_out` at $(0.78, 0, 0)$, Signal Output.
- Static Collision Boxes:
  - Housing: $1.4 \times 1.7 \times 0.6\text{ m}$ at $(0, 0, 0)$.
  - Foot: $1.6 \times 0.2 \times 0.85\text{ m}$ at $(0, -0.95, 0)$.

### 2.2 Declarative Element Package (`elements/SystemClock/`)
- Project: `elements/SystemClock/SystemClock.csproj` referencing `CuriousContraptions.Abstractions`, `CuriousContraptions.Abstractions.Signal`, and `CuriousContraptions.Abstractions.Electrical`.
- Element Class: `SystemClockElementDeclaration` implementing `ISignalElement`, `IElectricalElement`.
- Metadata:
  - `Id`: `"clock"`.
  - `Category`: `"Control"`.
  - `DisplayNameKey`: `"part_clock"`.
  - `ScenePath`: `"res://parts/scenes/clock.tscn"`.
  - `Parameters`: `{"interval_seconds": 1.0f}`.
- Ports:
  - `power_in`: Electrical Input at $(-0.78, 0, 0)$.
  - `activation_out`: Signal Output at $(0.78, 0, 0)$.
- Colliders:
  - Housing box $1.4 \times 1.7 \times 0.6\text{ m}$.
  - Foot box $1.6 \times 0.2 \times 0.85\text{ m}$ offset $(0, -0.95, 0)$.
- Events:
  - `SimulationEventKind.TriggerActivated`, payload `"PulseCount:i32;Interval:f32"`.
- Packaging: Included in `tools/BundleElements` to generate updated `elements.bundle.json`.

### 2.3 Pure C# WASM Simulation & Activation Network
- Repeating Source Rule:
  - In `engine/gpu/ActivationNetwork.cs`, oscillators (`ActivationNodeKind.Oscillator`) are repeating sources. Multiple emissions from an oscillator node across consecutive interval ticks are permitted and do not throw "Duplicate source emission".
- Simulation Oscillator State:
  - Phase: `Stopped` (0) or `Running` (1).
  - Ticks per interval: $N_{\text{ticks}} = \lceil \text{interval\_seconds} \times \text{frequency} \rceil$.
  - State tracking: `DueTick`, `PulseCount`, `LastPulseTick`.
- Execution Logic per Tick:
  - Power Sampling: Determine if `PowerIn` is powered by evaluating incoming electrical connections (Battery / closed contacts).
  - Unpowered: If previously running, transition to `Stopped`, set `DueTick = -1`, no pulse emitted even if unpowered on deadline tick.
  - Newly Powered: Transition to `Running`, set `DueTick = tick + IntervalTicks`. No startup pulse.
  - Running & `tick == DueTick`:
    - Increment `PulseCount`.
    - Set `LastPulseTick = tick`.
    - Advance `DueTick += IntervalTicks`.
    - Emit `ActivationOccurrence(ActivationOccurrenceKind.Trigger, ...)` through `ActivationOut`.
    - Write discrete event to `EventRingBuffer`.
- Reset Restoration:
  - Resets oscillator state to `Stopped`, `DueTick = -1`, `PulseCount = 0`, `LastPulseTick = -1`.
  - Clears pulse lamps to Slate `#556573`.

### 2.4 Wire Serialization & Engine Instances
- `PartId.Clock` (`"clock"`, numeric 23).
- `WorkshopClock` record in `engine/gpu/WorkshopClock.cs`:
  - `Id`: `GpuBodyId`
  - `Cell`: `CellOrigin`
  - `Local`: `LocalPosition`
  - `Rotation`: `CanonicalRotation`
  - `IntervalSeconds`: float (range $[0.1, 12.0]$, default 1.0)
  - `Locked`: bool
- FlatBuffers wire schema and binary encoding support for `clock` and `interval_seconds`.

## 3. Acceptance Criteria

- **AC 1: Placement & Structural Configuration:**
  - System Clock can be placed from toolbox, selected, and wired via contextual UI.
  - Sockets `PowerIn` (Electrical In) and `ActivationOut` (Signal Out) accept valid typed connections.
- **AC 2: Powered Periodic Pulse Generation:**
  - With a 1.0 s interval and electrical supply connected, over 5.0 seconds of simulation, exactly 5 discrete activation pulses are emitted at 1-second intervals ($t = 1.0, 2.0, 3.0, 4.0, 5.0\text{ s}$).
  - Connected Counter (target 5) increments exactly on each pulse and reaches target count 5 at $t = 5.0\text{ s}$.
- **AC 3: Negative Control (Unpowered State):**
  - Without electrical supply connected to `PowerIn`, clock remains `Stopped`. Zero pulses are emitted over 5.0 seconds; connected counter remains at 0 dots.
  - Power loss at deadline suppresses the pulse at that deadline.
- **AC 4: Pause & Reset Invariant:**
  - Pausing simulation halts clock pulse emission and freezes pendulum progress.
  - Reset restores clock to `Stopped`, phase 0, pulse count 0, indicator slate, and downstream counter to 0 without residual state.
- **AC 5: Parameter Boundaries:**
  - `interval_seconds` is configurable in range $[0.1, 12.0]\text{ s}$.
  - Values $< 0.1$, $> 12.0$, NaN, and non-finite values are rejected.
- **AC 6: Persistence (Save & Load):**
  - Saving workbench preserves clock instance, `interval_seconds` parameter, and electrical/activation wiring links.
  - Reloading and loading restores construction and replaying simulation produces identical 5-pulse sequence.
- **AC 7: Chrome Playwright Verification:**
  - `tools/e2e/cat-017.test.ts` passes 100% in Chrome serially against preview server.
  - All existing regression suites (`cat-052`, `cat-051`, `cat-050`, `cat-049`, `cat-048b`, `cat-048a`, `cat-002`, `cat-020`, `cat-030`) pass 100%.
  - `anvil check` reports 0 errors and 0 warnings.
