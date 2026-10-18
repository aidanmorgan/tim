# Specification: Story 8.1 — Battery DC Power Source & Network Graph (CAT-005) & Schema-Driven Wire

## 1. Context & Overview
Deliver CAT-005: Battery (`battery`) as the foundational direct-current (DC) power source and active electrical network graph provider.
In accordance with user architectural directives:
1. **Schema-Driven Binary Transfer:** Replace brittle manually-calculated offsets in binary response structures with a strongly-typed schema-driven layout contract (`tools/e2e/workshop-wire.ts`), eliminating hardcoded offsets (e.g. `SOURCE = 26384, WORK = 3584`, occurrence offsets, etc.) across tests and worker harnesses.
2. **Finite Energy Model:** Deliver approved owner parameters (3,600 J default capacity, 120 W maximum output, initial fraction 1.0, typed `ElectricalEnable` enum).
3. **Strict Entity Separation:** Retain existing scene separation (`Visual/` mesh instances vs. `col_box` with `visible = false` and `Loc_Port_out`).
4. **Dynamic UI Alignment:** Adapt Workshop driver UI interactions for 19+ catalog palette items so controls and settings are reliably targeted.

## 2. Declarative Elements & Physical Properties
- **Catalog Resource:** `parts/catalog/battery.tres` (`id: "battery"`, title: "Battery", category: "Power", color `#de7059`).
- **Scene:** `parts/scenes/battery.tscn`:
  - Static root box: 0.85 × 1.05 × 0.75 m.
  - Port locator: `Loc_Port_out` at $(0, 0.62, 0)$ representing `WorkshopSocket.Supply` in `WorkshopConnectionDomain.Electrical`.
  - Collision shape: `col_box` (`BoxShape3D`, `visible = false`).
  - Presentation geometry: `Visual/Visual_Body`, `Visual/Visual_Stripe`, `Visual/Terminal`, `Visual/Mark_0..3`, `Visual/Visual_PlusH/V`.

## 3. Schema-Driven Wire Contract
- Author `tools/e2e/workshop-wire.ts` defining the typed binary layout of `WorkshopResponse`:
  - `WorkshopWireOffsets`: dynamically computed or schema-pinned offsets for bodies, captures, activations, timers, contact works, motion, and electrical sources.
  - `decodeWorkshopResponse(data: Buffer)`: strongly typed decode returning typed structures for `electrical`, `contactWorks`, `occurrences`, `bodies`, etc.
  - Modernize `tools/e2e/cat-005.test.ts` to decode responses via `WorkshopWire` without manual byte offsets.

## 4. UI Driver Improvements
- Update `tools/e2e/workshop-driver.ts` with dedicated helpers for battery settings and supply toggle or adjust click coordinates to align with the 19-part palette layout.

## 5. Verification Requirements
- `anvil check`: 0 warnings on changed files.
- `dotnet test CuriousContraptions.slnx`: 100% pass rate.
- `npx tsx --test tools/e2e/cat-005.test.ts`: 100% passing across all test cases in real Chrome.
- Independent review and scoped verdict by Murdoch (`bmad_reviewer`).
