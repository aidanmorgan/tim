# EL-229 · Hydraulic piston — named-identity spec

**Current delivery (21 November 2026):** [EL-229 ledger entry](../invest/named-elements.md#element-229), ledgered 21 November 2026; no earlier spec, no legacy. Code is cited as `path:line` in checkout `spec-drafts` (`8d1d68aa`), abstraction files by file name. Values are f32; **proposed** values are starting values (D1). Native tests; browser qualification pending.

## 1. Identity and disposition

| Field | Value |
| --- | --- |
| ID / name / type | EL-229 · Hydraulic piston · Water |
| Requirement anchor | [todo-428](../requirements.md#todo-428) (`sequence-task-391`) |
| Ledger entry / owner | [element-229](../invest/named-elements.md#element-229); slice `named-el-229`, no earlier S owner |
| Disposition | **To deliver as a single element, after the engine programme (owner decision 3a), `ENGINE-GAS-NETWORK` and `ENGINE-LIQUID` (owner decision 18).** Delivery batches 1c, liquid run. Not a family with the pneumatic pistons (EL-230, EL-231): the working fluid is incompressible and conserved by volume, and the ports are liquid ports. |
| Planned package | id `hydraulic_piston`, directory `elements/HydraulicPiston/` |
| Related | [EL-001 Finite reservoir](EL-001-finite-reservoir.md), [EL-002 Header tank](EL-002-header-tank.md), [EL-008 Straight water pipe](EL-008-straight-water-pipe.md), [EL-240 Waterwheel](EL-240-waterwheel.md), [EL-230 Spring-return pneumatic piston](EL-230-spring-return-pneumatic-piston.md) |
| Status | Not started. Blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-ISLANDS-SLEEP` (a change of level wakes what it loads; nothing here assumes sleeping exists) and `ENGINE-CAPABILITY-INSTANCES` (declared snap sockets); then `ENGINE-GAS-NETWORK` (the one fluid port record, coordinator decision 16); then `ENGINE-LIQUID`, including the head-on-coordinate wording proposed below (section 3). |

## 2. Declaration

Nearest delivered shape: the Linear pusher's slider and rod (`elements/LinearPusher/LinearPusherElementDeclaration.cs:28`), with the supplied drive replaced by liquid pressure.

- **Interfaces.** `ISliderElement` (`MechanicalContracts.cs:15`), `ISceneGeometryElement` (`SceneGeometry.cs:10`), and the liquid store declaration on the one fluid port contract (today `IFluidElement`, `FluidContracts.cs:5`, which `ENGINE-GAS-NETWORK` reshapes), as [EL-001](EL-001-finite-reservoir.md) section 2.
- **Cylinder (body 0, static).** Upright clear barrel, outer radius 0.16 m, height 0.8 m, on a base 0.5 × 0.1 × 0.5 m.
- **Rod and tray (body 1).** Piston head inside the barrel, rod of radius 0.04 m, and a tray 0.7 × 0.06 × 0.7 m with 0.08 m lips on top; mass 0.5 kg. Slider along +Y, limits 0 to 0.5 m, no spring, no drive.
- **Chamber.** A store under the piston head with bore area 0.05 m² (radius 0.126 m); its capacity is bound to the rod coordinate, area × rod travel, 0 to 0.025 m³ (EL-001 L4 and LG4). Liquid entering raises the rod; the load pushing down expels it. The store is also a thermal node (owner decision 20; EL-001 L2): it starts empty at the ambient temperature of the thermal conventions; the node is `ENGINE-THERMAL`'s and no row here reads it.

| Port | Domain | Direction, role | Position (m) | Meaning |
| --- | --- | --- | --- | --- |
| `Start` | Fluid, mouth-joined socket, pipe bore 0.10 m | Bidirectional | (−0.16, 0.1, 0) | From the head (a tank above); reverse loading pushes liquid back through it (Y6) |
| `End` | Fluid, mouth-joined socket, pipe bore 0.10 m | Discharge | (0.16, 0.1, 0) | Return path to a lower vessel |

The mouths follow the liquid conventions ([EL-001](EL-001-finite-reservoir.md) L5, L6): declared snap sockets of the water kit (capture 0.20 m and 20°), ids by flow, joined to water pipes only by the authoring snap; there is no hose and no wire. A valve on either line is a separate part (EL-003 tap family, EL-021 sluice). An unjoined `End` is a free end; with no tap on it the return is open.

- **Parameters.** None (D13: size fixed per kind). Mode records: Fixed.
- **Presentation.** Rod body binding; liquid level in the clear barrel from the chamber's committed volume; the two connected tanks show their own levels.
- **Palette.** Motion, "Hydraulic piston"; order 154 (coordinator decision 36, [palette allocation](readiness/palette-allocation.md)).

### Art and solidity

| Node | Mesh | Class |
| --- | --- | --- |
| Barrel | Hollow cylinder, sky blue `#7ebcd4` at alpha 0.16, with solid end caps | Solid shell: hollow collider as the clear Pipe |
| Base | Box, cream `#fff8e9` (owner decision 4) | Solid, body 0 |
| Rod, tray, lips | Cylinder and boxes, navy `#1c2836` and gold | Solid, body 1 |
| Liquid column | Scaled cylinder inside the barrel | Decorative: enclosed and moved by a binding |
| Port marks | Rings printed flush on the barrel; the mouths are socket locators with no proud mesh (the pneumatic batch's convention) | Decorative: printed flat |

Icon: a cylinder with a rising rod and a water drop. It differs from the pneumatic pistons by the drop and the visible liquid column.

### Behaviour and numbers

- Law: force on the rod = ρ·g·h·A, where h is the head (free surface of the feeding vessel above the piston face) and ρ is the family's 16 kg/m³ (D2; EL-001 L2). A 3 m head gives 471 Pa above ambient and **23.5 N**: it lifts the 0.5 kg rod and tray plus a 1 kg Basketball (14.7 N) but not a 4 kg Bowling ball (44 N). A 6 m head (47.1 N) starts a Bowling ball on the tray (4.5 kg, 44.1 N) but cannot finish: the head falls as the piston face rises, and at 5.62 m (after 0.38 m of travel) the forces balance. An 8 m head lifts it the full stroke (7.5 m of head left at the top, 58.9 N).
- Volume is conserved: raising the rod 0.5 m takes 0.025 m³ from the feeding vessel, whose level falls, which lowers the head.
- With a closed tap on the `Start` line the rod is held (incompressible). Opening a tap on the `End` line lets the load push the liquid out to the lower vessel and the rod descends. A closed return tap holds it up.
- Reverse loading: a load too heavy for the head pushes liquid back up the feed line until the heads balance.
- No perpetual loop: the returned liquid arrives lower than it started; raising it again costs at least the work the piston did.

## 3. Engine capabilities

| Capability | Mark | Symbol | Test |
| --- | --- | --- | --- |
| Passive slider carrying a load on a tray | present | `MechanicalContractValues.cs:104` | `LinearPusherTests.VerticalPackageLiftsCargoOnlyWithSufficientDeclaredForce` |
| A fluid port contract | narrower | declared and bundle-validated, no solver or consumer (`FluidContractValues.cs:12`, `:18`) | none |
| **Conserved liquid volume in vessels, head-driven flow between them** | **missing** | no liquid solver | Owner: `ENGINE-LIQUID` |
| **Head pressure doing work on a declared prismatic coordinate, with the swept volume conserved** | **missing** | the liquid conventions admit a hydrostatic load on a coordinate only as two rows of `ENGINE-LIQUID`; this part uses the second, the intake-head force ρ·g·h·A on a prismatic coordinate bound to a store's capacity (EL-001 L12 c; LG4) | Owner: `ENGINE-LIQUID`, its bound-store row (EL-001 L12 c). The story text (`_bmad-output/planning-artifacts/epics.md`, ENGINE-LIQUID, "Bodies") already carries this part's clause: "a store whose capacity is bound to a prismatic coordinate receives force ρ·g·h·A on it from the head at its intake, paid by the volume entering at that head". The volume stays in the store, which is a bound-capacity store (L4, LG4), until expelled through a declared mouth |

Counts: 1 present, 0 untested, 1 narrower, 2 missing.

### Engine conformance (owner addendum, 22 November 2026)

- **Declaration data only; generic laws.** A static barrel and base, one passive slider carrying a rod and tray, one liquid chamber on that coordinate and two fluid ports. Lifting is the head law ρ·g·h·A on the declared bore area; holding is incompressibility with the port closed; descent is the load expelling volume; cargo rides by contact. The missing capability is stated for any package: liquid doing paid work on a declared prismatic coordinate with the swept volume conserved, owned by `ENGINE-LIQUID` (its capture-region row, EL-001 L12 c, quoted in section 3). No element code, callback or engine branch.
- **Three layers.** Colliders (physics): on body 0 one tube (barrel), one box (base) and one cylinder (bottom cap); on body 1 two cylinders (piston head, rod) and five boxes (tray and four lips), as hidden `col_` proxies from the admitted shape set. Visuals: the art nodes, none a collider. Animation: the liquid column is scaled by a binding from the chamber's committed volume. What moves each art node: barrel, base and port marks nothing (static body 0); rod, tray and lips the body 1 pose; the liquid column its presentation binding.
- **Flat state.** 2 bodies; 10 colliders (1 tube, 6 box, 3 cylinder); 1 prismatic joint; 1 liquid store with a bound capacity (also 1 thermal node); 2 mouth sockets; 0 drives; 0 sensors; 0 signal nodes; 1 body-pose and 1 driver binding.
- **Performance.** Seven colliders more than the Linear pusher: the hollow barrel and a lipped tray. Port collars were redrawn flush so they need no colliders. The barrel is static. Whether a held rod at rest may sleep is `ENGINE-ISLANDS-SLEEP`'s (owner decision 3a); island sleeping is not delivered and nothing here assumes it. Rod speed is set by flow and stays low.
- **One-way flow, typing, tests.** Physics → animation → renderer; nothing writes back. Closed sets are enums, ids typed, values f32. Every acceptance row is a scenario on the production simulation.

## 4. Sources and legacy

- **Requirement.** [todo-428](../requirements.md#todo-428): "Finite liquid displacement, return path, pressure/head and resisting load; visible rod and tank levels. Not a duplicate pneumatic cylinder with different colour."
- **Campaign.** Chapter 7 "Go with the Flow" (61–70); coverage row "… hydraulic piston, distinct flow/volume/pressure meters".
- **Legacy.** None.
- **Files harvested.** None.

## 5. Acceptance outline

Tolerances: the liquid conventions' (EL-001 L17: volume 0.004 m³, level 0.01 m); rod position ±0.02 m; "does not lift" means under 0.02 m of travel in 20 s; stall guard 60 s (liquid lessons are slow). A row run to the 60 s guard declares a Run length of 60 s for its level (`ENGINE-RUN-LENGTH`, owner decisions 13 and 17).

| Row | Given / When | Then | Control and boundary | Planned test |
| --- | --- | --- | --- | --- |
| Y1 | Tank 3 m above; Basketball on the tray; feed opened | The rod rises 0.5 m with the ball; tank volume falls by 0.025 m³ | Control: feed closed, no motion | `HydraulicPistonTests.AnElevatedTankLiftsALoadedTray` |
| Y2 | Bowling ball on the tray at 3 m head | It is not lifted | Boundary: at 8 m head it is lifted the full stroke; at 6 m it starts and stalls within 0.05 m of 0.38 m | `HydraulicPistonTests.InsufficientHeadDoesNotLift` |
| Y3 | Any run | Tank + chamber + lower vessel + spill volume is constant | — | `HydraulicPistonTests.LiquidIsConserved` |
| Y4 | Raised, feed closed, return sealed, 10 s | The rod holds | Control: return opened, it descends | `HydraulicPistonTests.ASealedReturnHoldsTheLoad` |
| Y5 | Lower vessel already full when the return opens | The excess overflows as conserved spill; nothing vanishes | — | `HydraulicPistonTests.ReturnOverflowIsConserved` |
| Y6 | A Cannonball dropped on a raised tray with the feed open | Liquid is pushed back toward the tank until heads balance | — | `HydraulicPistonTests.ReverseLoadingPushesLiquidBack` |
| Y7 | Later-partner row, run in EL-022 Water pump's slice (it needs the Waterwheel EL-240 and the pump): return water drives a Waterwheel that drives a pump back to the tank | The loop runs down; the tank never regains its starting level. The pump is open when unpowered (coordinator decision 16), so a stopped loop drains back unless a check valve is placed (`readiness/batch-f.md` section 7) | — | `HydraulicPistonTests.AWheelAndPumpLoopGainsNothing` |
| Y8 | Run, Reset; Save, Load; an electrical wire offered to the part; a pipe of channel bore offered to `Start` | Levels and rod restored; the wire is refused (the part has no wire port) and the bores do not join | — | `HydraulicPistonTests.ResetReloadAndWrongKindRefusal` |

**Integration.** All liquid: EL-001/002 tanks, EL-008 pipes, EL-003 tap, EL-240 Waterwheel, EL-022 pump (none delivered). No Weight meets liquid in these rows (coordinator decision 21 asks a spec to state its mass where one does).

**Coverage binding (planned, not yet live).** Published main binds a named-identity package through a final section of its spec that `tools/Coverage/NamedPackageSources.cs` reads (pattern: EL-239 Baseball); that section is not written here, because the reader parses every `EL-*.md` that carries its heading. It is written in the syntax current on main when this identity's slice starts. Planned values: Package `hydraulic_piston`; Named identity EL-229; Acceptance todo-428 and sequence-task-391; owner slice `named-el-229`; Mode records `Configuration=Fixed`. A slice-owned package is admitted only once main's reader admits one (the coverage follow-on story S7, which owner decision 15 also has widen the reader; EL-223 is paused, owner decision 24g). Levels need S8.

**Delivery.** Prerequisites, in order: blocked on the engine programme (owner decision 3a), in particular `ENGINE-SPATIAL-QUERIES`, `ENGINE-ISLANDS-SLEEP` and `ENGINE-CAPABILITY-INSTANCES`; then `ENGINE-GAS-NETWORK` (fluid port record); then `ENGINE-LIQUID` with its capture-region row (section 3); the reservoir, a pipe and a tap; the coverage reader admitting a slice-owned package. One element slice; row Y7 follows in EL-022's slice, after EL-240. Files: the `elements/HydraulicPiston/` package, `CuriousContraptions.tests/HydraulicPistonTests.cs`, and the hand-edited lists until S3 (S2 is published at `25df3cd5`; no test-project list edit). Review tier: engine. **Disqualified if** `ENGINE-LIQUID` fixes a different density or pressure scale: the bore area is then re-chosen so that a 3 m head lifts a Basketball and not a Bowling ball.

## 6. Open questions

1. *Units and density.* Resolved by D2 (16 kg/m³) and by `ENGINE-LIQUID`'s Code Map for anything further.
2. *A built-in valve?* Resolved by D19: no; taps and sluices are their own parts.

Needs the owner: nothing.
