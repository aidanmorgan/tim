# Specification: Story 6.11 — Funnel Hollow Frustum Inlet to the Standard Bore (CAT-030)

## 1. Context & Overview
Deliver CAT-030: Funnel (`funnel`) with hollow tapered frustum gathering balls from a wide 2.6-unit mouth into the standard 1.3-unit tube bore by gravity and contact alone. Completes Epic 6 Motion element catalog delivery.
Follows established declarative glTF/Godot architecture, strict visual vs. collider entity separation, dynamic part discovery via `PartId.Funnel`, and pre-commit verification gates.

## 2. Declarative Asset Specifications
- **Catalog Resource:** `parts/catalog/funnel.tres` (`id: "funnel"`, title: "Funnel", category: "Motion", color: `Color(0.40, 0.72, 0.79, 1.0)`, referencing `res://parts/scenes/funnel.tscn`).
- **Scene:** `parts/scenes/funnel.tscn`:
  - Static hollow tapered frustum along local X:
    - Half-length $h = 0.9\text{ m}$ (length $1.8\text{ m}$).
    - Inlet inner radius $r_1 = 1.3\text{ m}$ (mouth 2.6 m, twice the standard bore) at $x = -0.9\text{ m}$.
    - Outlet inner radius $r_2 = 0.65\text{ m}$ (standard bore 1.3 m) at $x = +0.9\text{ m}$.
    - Shell thickness: $0.05\text{ m}$.
  - Port locators:
    - `Loc_Port_mouth_start` (inlet) at $(-0.99, 0, 0)$ facing $-X$, radius 1.3 m.
    - `Loc_Port_mouth_end` (outlet) at $(0.99, 0, 0)$ facing $+X$, standard bore 0.65 m (compatible with straight pipes and bends).
  - Presentation geometry under `Visual/`:
    - `Visual/Visual_Shell`: transparent cyan/slate frustum shell (`Color(0.40, 0.72, 0.79, 0.16)`).
    - `Visual/Visual_Collar_Start`, `Visual/Visual_Collar_End`: cream (`#fff8e9`) collar meshes at $x = \mp 0.9\text{ m}$.
  - Collision geometry: compound static boxes (`col_*`, `visible = false`) forming the tapered interior guide wall and mouth rims.
- **Web Export:** Include `res://parts/catalog/funnel.tres` in `export_presets.cfg`.

## 3. Engine & Authoring Bindings
- `engine/PartId.cs`: Add `PartId.Funnel = new("funnel")` with numeric ID 19.
- `engine/gpu/WorkshopCanonicalColliders.cs`: Add `FunnelColliders` matching extracted scene shapes.
- `engine/gpu/WorkbenchCapacity.cs`: Footprint `"funnel" => new(1, 0, colliders, 0, 0, 0)`.
- `engine/gpu/WorkshopConstruction.cs`: Factory method `WorkshopInput.Funnel`.
- `engine/gpu/WorkshopWire.cs`: Add `"funnel"` to declared collider serialization.
- `engine/gpu/WorkshopConnections.cs`: Register socket owners for `"funnel"` with empty sockets `[]`.
- `engine/gpu/WorkshopInventory.cs`: Register `PartId.Funnel` in `_canonicalOrder`.
- `engine/MachinePart.cs`: Set `PickRadius = 1.75f`.
- `engine/MachineWorld.Gpu.cs`: Generic part capture binding for `"funnel"`.
- `tools/e2e/workshop-driver.ts`: Wire `funnel` palette coordinates and selection.

## 4. Verification Requirements
- `anvil check --changed`: 0 warnings.
- `dotnet test CuriousContraptions.slnx`: 100% pass rate.
- Playwright E2E in real Chrome:
  - `tools/e2e/cat-030.test.ts`:
    1. Workshop selection: Select funnel from palette, place on canvas.
    2. Negative control: Ball dropped on outer shell deflects/bounces without entering.
    3. Centered drop: Ball dropped into wide 2.6 m inlet descends smoothly, guided by tapered walls, exits 1.3 m outlet into receiver below.
    4. Off-center drop: Ball dropped at $+0.75\text{ m}$ off-axis gathers into funnel without snagging and exits outlet.
    5. Joined pipe route: Funnel outlet snapped above straight pipe or bend, ball smoothly transitions from funnel into tube.
    6. Reset restoration: Reset restores initial poses; re-run replays bit-for-bit.
    7. Save/Load persistence: Save, reload, Load restores construction, re-run solves.
