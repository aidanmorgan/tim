# EL-211 · Solar panel — element readiness spec

Story 7.0 Batch H named-identity spec. Legacy citations are pinned to `a6c914e` and resolve with `git show a6c914e:<path> | sed -n 'start,endp'`. Values marked *proposed* have no legacy or requirement source; each carries a one-line justification, stays inside the [game-grade envelope](../../gpu-f32-physics.md#game-grade-envelope) and may be revised by the owner. Box sizes are full extents (legacy `AddBox` takes the full size and stores half-extents, `reference/cpu/MachinePart.cs@a6c914e:L352-L356`). The catalogue harvest is [CAT-059](CAT-059-solar_panel.md); this spec adds the declared efficiency and the absorbed-energy bound.

## 1. Identity

| Item | Value |
| --- | --- |
| Identity / name | EL-211 · Solar panel |
| Type | Electrical |
| Anchor | [requirements.md#element-211](../requirements.md#element-211); existing record [campaign-element-coverage](../requirements.md#campaign-element-coverage) ("Flashlight, lamp, drawstring source, solar panel …", first use 51–60); [named-elements entry](../invest/named-elements.md#element-211); owner S508 |
| Related | Refines [CAT-059 Solar panel](CAT-059-solar_panel.md) ([current-cat-059](../requirements.md#current-cat-059)); light from [CAT-029](CAT-029-flashlight.md)/[EL-210 Flashlight](EL-210-flashlight.md); supplies loads through [EL-197](EL-197-electrical-wire.md) like [EL-196](EL-196-battery.md); first consumer [CAT-042 Motor](CAT-042-motor.md). |
| Roadmap story | 13.7 "Photovoltaic Solar Panel Energy Conversion" (Epic 13) |
| Status | not started |

## 2. Declaration

| Item | Declaration (value · source) |
| --- | --- |
| Bodies and shapes | Static panel box 0.22 × 1.2 × 1.5 m at the origin and foot box 0.9 × 0.16 × 1.2 m at (0, −0.8, 0) (`parts/SolarPanelPart.cs@a6c914e:L41-L41`, `parts/SolarPanelPart.cs@a6c914e:L45-L45`). Active face is local −X. |
| Mass and material | Static; restitution 1, bounce threshold 0.1 m/s, friction 0.3 (`engine/gpu/WorkshopPhysicsCompiler.cs@a6c914e:L109-L110`); the panel is opaque to light (it absorbs what it receives). |
| Constraints | none. |
| Typed ports | `Supply` (Electrical, Output) at (0.2, −0.5, 0.6) (`parts/SolarPanelPart.cs@a6c914e:L24-L25`). No `PowerIn`, no activation port. |
| Sensors and activation | Nine equal-weight (1/9) light samples on the front face at (−0.13, y, z), y ∈ {−0.35, 0, 0.35}, z ∈ {−0.45, 0, 0.45}, facing −X (`parts/SolarPanelPart.cs@a6c914e:L26-L34`); committed irradiance = sum of sample readings from the finite optical transport (partial occlusion reduces, not erases, harvest P4). |
| Work and energy stores | No store. Absorbed optical power P_abs = irradiance × face area 1.8 m² (1.2 × 1.5 m, the panel's lit face) with 1 game irradiance unit = 50 W/m² **proposed** (puts the legacy 3 m flashlight reading of about 2.7 units at roughly 240 W absorbed, enough to visibly turn the default 120 W Motor through the efficiency below). Electrical output P_elec = `efficiency` × P_abs while irradiance ≥ 1 (the legacy supply threshold, `parts/SolarPanelPart.cs@a6c914e:L12-L12`), else 0; P_elec ≤ P_abs always (outcome). Loads draw at most P_elec, scaled proportionally like a battery ([EL-196](EL-196-battery.md)); unused output is not stored. |
| Parameters | `efficiency`: f32, fixed 0.2 **proposed** (typical photovoltaic efficiency; any value < 1 satisfies "never exceeds absorbed energy"). No player-editable parameter (legacy Parameters `{}`, `parts/catalog/solar_panel.tres@a6c914e:L14-L14`). |
| Cosmetic curves and UI bindings | Four meter marks slate `#556573` → gold `#f7cb52`, mark i lit at irradiance ≥ (i + 1)/4 of the threshold, 0.1 s SmoothStep (`parts/SolarPanelPart.cs@a6c914e:L47-L53`); "four gold marks mean electrical supply is available" (catalogue text). |
| Art | Cream `#fff8e9` panel; nine solar cells 0.025 × 0.29 × 0.39 in catalogue colour `#45639c` at (−0.125, y, z); navy `#293954` foot; gold `#f7cb52` post r 0.1, h 0.3 at (0, −0.65, 0); gold port sphere r 0.085 at the `Supply` socket (`parts/SolarPanelPart.cs@a6c914e:L40-L54`). Toolbox icon `ui/WorkshopIcons.cs@a6c914e:L94-L94`. Palette row "Solar cells" ([DESIGN.md colour system](../../../DESIGN.md#colour-system)). |
| Catalogue / inventory | Id `solar_panel`, Title "Solar panel", Category Power, colour (0.27, 0.39, 0.61); Description "Face the blue cells toward an active flashlight. Walls and objects block light; four gold meter marks mean electrical supply is available." (`parts/catalog/solar_panel.tres@a6c914e:L6-L14`). Levels `solar_motor`, `solar_shadow`, `delayed_solar` grant one panel (harvest P6). |

**Variants.** The requirements row names no variant; CAT-059 lists no enumerated selector. The base declaration is the only required mode.

## 3. Engine capabilities

Families ([element map row](../general-engine-element-map.md); [binding](../../coverage/engine/element-03.json), proof owner S508): ElectricalPower, FiniteLedger, GeometryQuery, OpticalAbsorption, OpticalTransport, SensibleHeat (+ StateTransaction).

**Exists now**
- Static boxes (`engine/gpu/PhysicsDeclarations.cs@a6c914e:L57-L58`, `engine/gpu/PhysicsDeclarations.cs@a6c914e:L89-L90`); `Supply` socket enum value (`engine/gpu/WorkshopConnections.cs@a6c914e:L9-L9`).

**Missing**
- OpticalTransport (flashlight cone, occlusion, samples) — Story 13.1; finite per-channel power — [S484](../invest/decisions.md#s484) (S485 finite-colour, S488 optical-law).
- OpticalAbsorption → electrical conversion with efficiency, and absorbed-but-unconverted energy → heat — [S484](../invest/decisions.md#s484) absorption row (S489), [S543](../invest/decisions.md#s543).
- ElectricalPower source with a finite per-tick power budget — Story 8.1 network plus EL-196 finite accounting; typed role [S257](../invest/decisions.md#s257).

**Dependencies.** Flashlight (CAT-029, Story 13.1), Motor (CAT-042, Story 11.1), wires (EL-197).

## 4. Sources and legacy

- Requirement row [element-211](../requirements.md#element-211): "Absorbed illumination converts to bounded electrical work through declared efficiency"; outcome "Darkness yields no power; output never exceeds absorbed energy". [current-cat-059](../requirements.md#current-cat-059): nine front-face samples, finite cone/range/facing, partial/full occlusion, ambient light never supplies power, paid downstream work obeys the admitted source law.
- Named entry [element-211](../invest/named-elements.md#element-211), owner S508.

| # | Legacy fact | Source | Disposition |
| --- | --- | --- | --- |
| P1 | Nine equal-area samples convert received game light into binary supply; no ambient-sky power, storage or voltage/current model. | `parts/SolarPanelPart.cs@a6c914e:L8-L9` | carry forward samples and no-ambient rule; replace binary supply with bounded power |
| P2 | Supply source = irradiance ≥ 1 (scalar threshold source kind). | `parts/SolarPanelPart.cs@a6c914e:L12-L12`, `parts/SolarPanelPart.cs@a6c914e:L36-L37`, `engine/SceneElectricalSource.cs@a6c914e:L26-L31` | carry forward the threshold as the availability boundary |
| P3 | Needs an active torch, facing (reversed panel reads 0), range (x = 7 reads 0) and a wire; supply does not latch — torch off gives irradiance 0 and no power. | `CuriousContraptions.tests/LightTests.cs@a6c914e:L116-L166` | carry forward |
| P4 | A small occluder reduces irradiance without erasing all samples. | `CuriousContraptions.tests/LightTests.cs@a6c914e:L211-L225` | carry forward |
| P5 | Flashlight emitter: lens (0.66, 0, 0), range 8, 15° half-angle, intensity 24 game units. | `parts/FlashlightPart.cs@a6c914e:L10-L13` | carry forward (light source scale, [EL-210](EL-210-flashlight.md)) |
| P6 | Lessons: `solar_motor`, `solar_shadow` (wall between: panel must stay on the torch side), `delayed_solar` (switch → delay → torch; panel → motor; goal powered ≥ 1 s after the switch). | `content/puzzles.json@a6c914e:L5944-L6225`, `content/puzzles.json@a6c914e:L6226-L6574`, `content/puzzles.json@a6c914e:L6865-L7293`, `tools/Campaign/Program.cs@a6c914e:L225-L246`, `tools/Campaign/Program.cs@a6c914e:L266-L277` | carry forward (Epic 15 inputs) |
| P7 | Activity and a Powered event set in a per-part physics hook. | `parts/SolarPanelPart.cs@a6c914e:L56-L60` | do not carry forward the per-element loop |

- **Files harvested:** `parts/SolarPanelPart.cs`, `parts/catalog/solar_panel.tres`, `parts/FlashlightPart.cs`, `engine/SceneElectricalSource.cs`, `CuriousContraptions.tests/LightTests.cs`, `content/puzzles.json` (three solar levels), `tools/Campaign/Program.cs` (solar modules), `reference/cpu/MachinePart.cs` (`AddBox` extents).

## 5. Acceptance outline

Acceptance authority: [element-211](../requirements.md#element-211), [current-cat-059](../requirements.md#current-cat-059); Story 13.7.

- **Construction (actual Chrome UI).** Flashlight with a ball above its button; Solar panel 3 m in front, cells facing the lens (rotate gizmo); Connect panel `Supply` → Motor `PowerIn`.
- **Positive.** The ball presses the torch; four marks light; the motor turns with power ≤ 0.2 × absorbed.
- **Negative / control.** Torch off, panel reversed, beyond range, a Wall between (`solar_shadow`): no marks and no power (outcome: darkness yields none); no wire: motor still; ambient scene lighting alone: no power.
- **Boundaries.** A small occluder lowers power without zeroing it (P4); irradiance just below and at 1; delivered electrical energy over a run never exceeds absorbed optical energy (ledger check).
- **Run/Reset.** Reset clears irradiance, marks and motor state; replay identical.
- **Save/Load.** Pose and wire round-trip; `solar_motor`, `solar_shadow`, `delayed_solar` replay once the flashlight and motor exist.
- **Integrations.** CAT-059 retained behaviour [todo-134](../requirements.md#todo-134) (`solar_motor`, `solar_shadow`, `delayed_solar`); interaction processes [IX-13 optical transport](../requirements.md#interaction-13), [IX-14 optical absorption](../requirements.md#interaction-14) ("Allocate only the absorbed fraction") and [IX-06 electrical power transfer](../requirements.md#interaction-06) (bounded output to the load). Campaign: "solar panel" is named in the optics row of [campaign-element-coverage](../requirements.md#campaign-element-coverage), first use 51–60 (reuse 71–90, 111–125, 136–150).

## 6. Open questions

1. **Optical power scale.** 1 game irradiance unit = 50 W/m² is a proposal tying legacy light units to watts; S484/S489 owns the finite optical power model: owner decision.
2. **Threshold retention.** Keeping the legacy irradiance ≥ 1 availability threshold under a continuous power model (proposed) versus proportional output from any light: owner decision (also CAT-059 open question 2).
3. **Laser input.** Whether lasers power the panel (Story 13.7 wording): CAT-059 open question 1.
