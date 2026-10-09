# EL-074 · Wood barrier declaration readiness spec

Story 7.0 named-identity spec. Baseline commit `a6c914e`. Legacy citations use `path@a6c914e:Lstart-Lend` and stay retrievable from git history after the Epic 7 purge. Values marked **proposed** are design values the owner may revise.

## 1. Identity

| Field | Value |
| --- | --- |
| ID | EL-074 |
| Name | Wood barrier |
| Type | Mechanical |
| Requirement anchor | [element-074](../requirements.md#element-074); scope index [todo-199](../requirements.md#todo-199) |
| Named entry | [named-elements.md#element-074](../invest/named-elements.md#element-074); proof owner S644 |
| CAT spec refined or extended | Extends [CAT-066 wall](CAT-066-wall.md) (delivered static resizable box) with its own declared contact and combustible material. The legacy "wooden wall" in level `solar_shadow` was the generic Wall kind (Sources) |
| Related identities | [EL-073 brick barrier](EL-073-brick-barrier.md) (independent material), thermal combustion identities TH-* (Batch N; S554/S555 reaction and ignition), [EL-014](EL-014-water-carrying-bucket.md) (water-family density constant) |
| Roadmap story | unscheduled |
| Status | not started |

## 2. Declaration

### Bodies and shapes

One static box with the Wall's resizable dimensions and bounds: width 0.4–8, height 0.4–6, thickness 0.12–2 m (`engine/gpu/WorkshopWall.cs@a6c914e:L6-L16`). Default 2.0 × 1.5 × 0.2 m. **Proposed**: plank-thickness panel, thinner than the brick default (0.3 m) so the two read apart.

### Mass and material

Static (no mass). Declared wood material, independent of brick:

| Property | Value | Unit | Source |
| --- | --- | --- | --- |
| Restitution | 0.6 | — | **Proposed**: a Basketball rebounds at 0.33 of approach speed, between brick (0.165) and the Wall (0.55) |
| Bounce threshold | 0.1 | m/s | Common to all current declarations (`engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`) |
| Friction | 0.5 | — | **Proposed**: planed wood, smoother than brick (0.7) |
| Rolling resistance | 0 | — | Non-sphere surfaces declare zero |
| Ignition temperature | 573 | K | **Proposed**: order of real wood ignition (~300 °C); frozen by S555 |
| Heat of combustion | 15 | MJ/kg | **Proposed**: order of dry wood, applied per kg of declared (game-scale) mass; frozen by S554 |
| Density | 8 | kg/m³ | **Proposed** on the catalogue's game scale (about 2–44 kg/m³: Domino 2.2, Basketball 6.1, Bowling ball 43.5; `engine/gpu/WorkshopConstruction.cs@a6c914e:L45-L53`, `engine/gpu/WorkshopDomino.cs@a6c914e:L9-L9`): light like the Basketball, a fifth of brick (40). The default panel (0.6 m³) is 4.8 kg of fuel; the largest panel (96 m³) is 768 kg, inside the 1024 kg bound (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L78-L78`). Real softwood (~600 kg/m³) is reference only |
| `fracture_work` | 150 | J | **Proposed**: weaker than brick (400 J) but above a Basketball at 10 m/s (50 J) |

### Constraints and joints

None.

### Typed ports

None.

### Sensors and activation

None (ignition is a material response to thermal state, not a port).

### Work and energy stores

Chemical energy store = density × volume × heat of combustion (finite fuel), consumed only by the shared reaction model once admitted.

### Parameters

| Name | Type | Range | Default | Unit | Source |
| --- | --- | --- | --- | --- | --- |
| width / height / thickness | f32 | 0.4–8 / 0.4–6 / 0.12–2 | 2.0 / 1.5 / 0.2 | m | Bounds from `engine/gpu/WorkshopWall.cs@a6c914e:L9-L10`; defaults **proposed** |

### Cosmetic curves and UI bindings

None for the intact panel. Scorch and burn presentation follow committed thermal state once combustion exists.

### Art

Warm-wood planks in the Physical-wall/Ramp body colour `#c28f52` (`DESIGN.md@a6c914e:L168-L178`) with darker grain lines in the workbench base colour `#b77c42` (`DESIGN.md@a6c914e:L146-L146`); horizontal plank seams are the shape cue that tells wood from brick. Catalogue colour **proposed**: `#c28f52`.

### Catalogue and inventory entry

Id `wood_barrier`, title "Wooden wall", category "Structure"; `WorkshopPartKind.WoodBarrier` appended last to the free inventory (`engine/gpu/WorkshopInventory.cs@a6c914e:L49-L60`). **Proposed**.

### Variants

The requirements row names no variants or modes. One element is specified.

## 3. Engine capabilities

Binding: ContactImpulse, EnvironmentState, FiniteLedger, GeometryQuery, JointConstraint, RigidBodyDynamics, SlidingFriction (map); coverage JSON adds StateTransaction. Neither lists the reaction capabilities (Open question 2).

**Exists now:** static resizable box (`engine/gpu/WorkshopWall.cs@a6c914e:L6-L40`); `ContactMaterialDeclaration` and pair mixing (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L42-L54`; `CuriousContraptions.Simulation/wwwroot/worker.js@a6c914e:L702-L766`).

**Missing**

- Per-kind static material (today every static surface is (1, 0.1, 0.3), `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`): no story; decision owner S644.
- Combustible material fields, reaction and ignition: S554/S555 ([decisions.md#s543](../invest/decisions.md#s543)); thermal identities in Batch N.
- Fracture: S563.

**Dependencies.** None for the intact barrier.

## 4. Sources and legacy

- Requirements row: "Mechanical contact cannot silently use brick parameters." No variants.

| # | Fact | Citation | Disposition | Reason |
| --- | --- | --- | --- | --- |
| 1 | Level `solar_shadow` ("Out of the shade") describes "The wooden wall blocks the flashlight", but places the generic `wall` kind (`shade`). | `content/puzzles.json@a6c914e:L6227-L6230`, `content/puzzles.json@a6c914e:L6426-L6427`; `tools/Campaign/Program.cs@a6c914e:L418-L420` | carry forward as context; do not carry forward "wooden" as a material | Legacy wood was a name in text only; no wood material existed. The level stays a CAT-066 Wall lesson unless the owner reassigns it. |
| 2 | Legacy static surfaces use (1, 0.1, 0.3). | `engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110` | do not carry forward for wood | The row requires independent declared properties. |
| 3 | Wall body colour (0.76, 0.56, 0.32) and cream stripes `#f9e8c9`. | `parts/catalog/wall.tres@a6c914e:L20-L20`; `parts/WallPart.cs@a6c914e:L15-L17` | carry forward (palette) | Warm-wood tone already in the palette. |

**Files harvested:** `content/puzzles.json` (level `solar_shadow`), `tools/Campaign/Program.cs`, `parts/WallPart.cs`, `parts/catalog/wall.tres`. Searched for wood, timber, plank: only the level text above.

## 5. Acceptance outline

Requirement row: [element-074](../requirements.md#element-074).

- **Chrome UI recipe.** Place a Wooden wall and a Brick wall side by side, horizontal, with a Basketball 1 m above each; place a Wooden wall upright across a Ramp run-out. Verify placement, dimensions and each part's declared material from the committed read.
- **Positive.** The ball rebounds from wood to about 0.11 m (e² × 1 m, e = 0.33) and from brick to about 0.03 m; the ramp ball stops at the wooden face.
- **Negative/control.** The committed material of the wooden wall differs from brick in restitution and friction (no shared record); swapping the two parts swaps the rebound heights.
- **Boundaries.** 0.12 m thick at 20 m/s (no tunnelling); out-of-range dimensions rejected.
- **Run/Reset and Save/Load.** Ball poses restore; dimensions and kind round-trip.
- **Integrations.** The scope index [todo-199](../requirements.md#todo-199) defines no separate integration task; shared interactions use [IX-01 contact impulse](../requirements.md#interaction-01), [IX-02 sliding friction](../requirements.md#interaction-02) and, once admitted, [IX-29 reaction ignition](../requirements.md#interaction-29). The [campaign coverage ledger](../requirements.md#campaign-element-coverage) reserves first use of ramp/wall/material variants in levels 1–10.

## 6. Open questions

1. Should level `solar_shadow` switch its wall to the Wooden wall now that one exists? Owner decision.
2. The binding lacks ChemicalReaction/ReactionIgnition; add them when combustion is admitted? Owner decision.
3. If wood ever becomes dynamic (fragments or loose planks), it floats in the water family: 8 kg/m³ is half the proposed liquid density of 16 kg/m³ ([EL-014](EL-014-water-carrying-bucket.md) §2), like the Basketball (6.1). Is floating wood intended? Owner decision.
4. **Material density scale.** Densities are proposed on the catalogue's game scale (about 2–44 kg/m³), not real values; confirm the scale for all structural and combustible materials (it also sets the fuel mass behind the heat of combustion). Owner decision.
