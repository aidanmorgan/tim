# Specification: Story 8.2 — Powered Gate Passageway Barrier (CAT-051)

## 1. Context & Overview
Deliver CAT-051: Powered Gate (`powered_gate`) as the first electrical consumer in Curious Contraptions (owner decision, 9 Oct 2026), implementing a physical sliding gold blade barrier across a passageway tube that retracts when powered and returns closed via spring when unpowered.

## 2. Declarative Elements & Physical Properties
- **Catalog Resource:** `parts/catalog/powered_gate.tres`:
  - `Id = "powered_gate"`
  - `Title = "Powered gate"`
  - `Category = "Motion"`
  - `Color = Color(0.97, 0.796, 0.322, 1)` (#f7cb52 gold)
  - `Description = "Electricity retracts the gold shutter. Without power it returns closed, stopping if a ball blocks its path. Its mouths snap to tubes."`
- **Scene:** `parts/scenes/powered_gate.tscn` (rooted directly on generic `MachinePart`):
  - **Visual Presentation (`Visual/`):**
    - `Visual_Sleeve`: Translucent central sleeve, length 0.8 m (half-length 0.4 m), inner radius 0.65 m (bore), outer 0.70 m, color `(0.40, 0.72, 0.79, 0.16)`.
    - `Visual_Collar_Start`, `Visual_Collar_End`: Cream collars at $x = \pm 0.4\text{ m}$, length 0.18 m (half-length 0.09 m), inner radius 0.65 m, outer 0.78 m, `#fff8e9`.
    - `Visual_Housing`: Navy actuator box at $(0, 1.65, 0)$, full size $0.8 \times 0.65 \times 1.8\text{ m}$, `#293954`.
    - `Visual_Rail_Start`, `Visual_Rail_End`: Cream guide rails at $z = \pm 0.82\text{ m}$, center $y = 0.65\text{ m}$, size $0.24 \times 1.7 \times 0.12\text{ m}$, `#fff8e9`.
    - `Visual_Blade`: Gold shutter box, half extents $(0.06, 0.72, 0.72)\text{ m}$, `#f7cb52`.
    - `Visual_Socket`: Electrical socket sphere at $(0, 1.65, 0.92)\text{ m}$, radius 0.09 m, `#e8b764`.
    - `Visual_Indicator`: Indicator sphere at $(0.42, 1.65, 0.55)\text{ m}$, radius 0.07 m (`#556573` unpowered, `#f7cb52` powered, `#e8b764` blocked).
  - **Port Locators:**
    - `Loc_Port_power_in` at $(0, 1.65, 0.92)\text{ m}$ (`WorkshopSocket.PowerIn` in `WorkshopConnectionDomain.Electrical`).
    - `Loc_Port_mouth_start` at $(-0.49, 0, 0)\text{ m}$ facing $-X$.
    - `Loc_Port_mouth_end` at $(0.49, 0, 0)\text{ m}$ facing $+X$.
  - **Physics Colliders:**
    - Frame static colliders (`col_housing`, `col_rail_*`, sleeve colliders) with `visible = false`.
    - Blade dynamic body: box collider half extents $(0.06, 0.72, 0.72)\text{ m}$, mass 1 kg, material $(0.1, 0.1, 0.3)$ (restitution 0.1, threshold 0.1 m/s, friction 0.3, rolling resistance 0).
  - **Prismatic Slider Joint:**
    - Blade to frame along local $+Y$, stroke 1.55 m (travel range $[0, 1.55]\text{ m}$).
    - Return spring: stiffness 14 N/m, rest position 0, damping 4 N·s/m.
    - Powered drive: max speed 2.8 m/s, acceleration 14 m/s², max effort 60 N, max power 120 W.

## 3. Typed Connections & Rejection Invariants
- `WorkshopConnections.GetPorts("powered_gate")` exposes exactly one input port: `WorkshopSocket.PowerIn` in `WorkshopConnectionDomain.Electrical`.
- Connecting an Activation output (e.g. from Delay, Switch, Domino, Ball Detector) to `powered_gate` is rejected.
- Connecting an Electrical supply (from Battery `Supply`) to `powered_gate` `PowerIn` is accepted.

## 4. Verification Requirements
- `anvil check`: 0 warnings on all changed files.
- `dotnet test CuriousContraptions.slnx`: 100% pass rate.
- `npx tsx --test tools/e2e/cat-051.test.ts`: 100% passing across all test cases in real Chrome.
- Independent review and scoped verdict by Murdoch (`bmad_reviewer`).
