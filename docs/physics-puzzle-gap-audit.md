# Physics puzzle game gap audit — source research (current adoption is in requirements)

Research date: 28 September 2026. Repository baseline: HEAD `22ee03b` plus the active, uncommitted working tree. This is a design comparison, not a playable-build certification. The catalogue contains 72 resource files (including mode/colour variants); `content/puzzles.json` contains 61 authored puzzle entries. Neither count establishes verified mechanics or completed campaign levels. Shared-physics migration and outstanding per-part UI evidence remain the first delivery priorities.

## Findings

The backlog already captures most obvious Incredible Machine components. Its biggest omissions are **player-built structures and vehicles, reusable assemblies, player puzzle authoring/sharing, and a complete experiment/inspection workflow**. Granular materials and material-processing puzzles offer distinct later expansion directions. These conclusions compare code, active TODO rows, retained research and the sources below; a missing search hit alone is not treated as proof that a feature is absent.

The 150-level campaign, radiation research, positive gamification, grouped palette, grid placement and difficulty-dependent physics are already planned. They are not new discoveries in this audit.

**Planning update, 28 September 2026:** The user has requested that all GAP-01–18 enter the potential-feature backlog and 150-level campaign plan. The [individual TODO specifications](planning/requirements.md#cross-game-gaps) and [exact campaign objective allocation](planning/requirements.md#campaign-gap-allocation) now govern that scope, including Monument Valley-inspired forms, the preserved palette, per-feature controls and per-mode teaching. The evaluation language later in this original audit records its research-stage recommendation; it no longer means these 18 can be omitted from planning. All remain unimplemented/unverified. Existing required scope remains required; expand grouped modes into individual evidence records before implementation.

## Incredible Machine edition coverage

Edition identity matters: **The Even More Incredible Machine** and **The Incredible Machine: Even More Contraptions** are different releases. An expansion's additional puzzles do not prove additional physical systems.

| Edition/family | Evidence inspected or located | Implication and confidence |
| --- | --- | --- |
| The Incredible Machine; The Even More Incredible Machine | Existing [repository research](research.md); original [manual mirror](https://www.retrogames.cz/manualy/DOS/The_Incredible_Machine_-_DOS_-_Manual.pdf) timed out; [Even More manual](https://sierrahelp.com/Documents/Manuals/The_Even_More_Incredible_Machine_-_Manual.pdf) was image-only and attempted screenshot timed out. | Baseline chain reactions are extensively covered by the existing backlog. This pass does not certify an exhaustive edition-by-edition parts delta. |
| The Incredible Machine 2 | [Publisher manual, mirrored PDF](https://pexy.io/wp-content/uploads/2025/06/the-incredible-machine-2-manual.pdf), extracted text inspected. Printed pp. 2–5, 8–12, 28–30 and 39–51 are useful checkpoints. | Strong primary evidence for both workshop features and several currently unresolved historical candidates; see the short register below. Manual intent is not measured runtime behaviour. |
| The Incredible Machine Version 3.0 | [Series history](https://en.wikipedia.org/wiki/The_Incredible_Machine), secondary chronology. | Reported reuse of TIM2 puzzles with interface/platform changes; do not invent a separate set of new physical parts. Exact differences still require its own manual/executable fixture. |
| Return of the Incredible Machine: Contraptions | [Release overview](https://en.wikipedia.org/wiki/Return_of_the_Incredible_Machine%3A_Contraptions), secondary; existing repository walkthrough references. | Treat its parts and sharing features as later-series evidence, never as proof of TIM2 behaviour. Full primary delta remains unresolved. |
| The Incredible Machine: Even More Contraptions | [Archive entry and manual listing](https://www.macintoshrepository.org/5205-the-incredible-machine-even-more-contraptions); download led to a form, not readable manual content. | Release located; detailed mode/part comparison not verified. Do not conflate it with the early Even More expansion. |
| Sid & Al's Incredible Toons; The Incredible Toon Machine | [Series chronology](https://en.wikipedia.org/wiki/The_Incredible_Machine); [Sid & Al manual location](https://wiki.sierrahelp.com/images/f/fb/SidNAls_-_Manual.pdf) returned a bot check. | Relevant adjacent cartoon-interaction branch. Existing character/lure/tool tasks cover much of that direction; individual gag chains remain research questions, not confirmed physics requirements. |
| Ports/mobile: 3DO, Palm and iOS | [Series history](https://en.wikipedia.org/wiki/The_Incredible_Machine); [iOS release record](https://www.mobygames.com/game/54831/the-incredible-machine/releases/). | Track separately if exact historical coverage is desired. This audit has no primary hands-on port inventory; touch UI or extra level packs do not establish unique mechanics. |
| Contraption Maker, spiritual successor | [Developer page](https://spotkin.itch.io/contraption-maker). | Custom content, sharing and cooperative construction are relevant. That page explicitly distinguishes cooperative building from cooperative puzzle solving; do not promise the latter on its evidence. |

### TIM2 evidence register: refine existing work, do not duplicate it

The [manual](https://pexy.io/wp-content/uploads/2025/06/the-incredible-machine-2-manual.pdf) establishes:

- **Printed pp. 40–43:** larger pipe fittings/accelerator tubes, pool equipment, tack and hot-air balloon.
- **Printed pp. 45–46:** flint ignition and powered vacuum.
- **Printed p. 50:** contact-triggered pinball flipper.
- **Printed pp. 2–5, 28–30:** player records, head-to-head and puzzle authoring.
- **Printed p. 39:** deliberately unusual ball behaviour, including a gravity-exempt pool ball and energy-gaining superball.

These resolve part of [todo-227](planning/requirements.md#todo-227), not its entire behavioural verification. Fireworks, missiles and nitroglycerine remain edition-specific questions; failed text searches are not proof of absence. Historical exaggerated physics must not silently become the realistic hard-mode model. Any adopted powered rebound needs an explicit energy source; fictional effects need a clearly stated rule and campaign purpose.

For every outstanding edition, retain edition/build, primary page or timestamp, trigger, output, repeat/reset behaviour and unresolved details under [todo-228](planning/requirements.md#todo-228). No executables were played for this audit.

## Other games: evidence and useful differences

These are design precedents, not recommendations to copy their UI, characters, art or complete physics models.

| Primary source | Verified design direction | Our comparison |
| --- | --- | --- |
| [Crazy Machines 2](https://store.steampowered.com/app/18400/Crazy_Machines_2/) and [Crazy Machines 3](https://store.steampowered.com/app/351920/Crazy_Machines_3/) | Broader physical systems; CM3 exposes part construction/material editing and sharing. | Most physical domains are already planned. Reusable custom assemblies and authoring tools are the more substantial gaps. |
| [Fantastic Contraption VR](https://fantasticcontraption.com/contraptionvr/) | Building moving machines from wheels and rods. | Player-assembled wheeled mechanisms are a distinct missing construction grammar. This source is the VR edition, not evidence for every original browser-game feature. |
| [Poly Bridge manual](https://cdn.cloudflare.steamstatic.com/steam/apps/367450/manuals/ManualPFDable.pdf?t=1567634023), sections 2–3; [Poly Bridge 3](https://store.steampowered.com/app/1850160/Poly_Bridge_3/) | Structural loads, hydraulic phases, stress feedback, selection copying and simulation controls; PB3 also describes build zones. | General structures, informative stress feedback and construction zones extend our component-centric puzzles. Existing releasable joints and actuator plans are partial overlaps. |
| [Algodoo](https://www.algodoo.com/what-is-it/) | Shape construction/editing, physical parameters, forces/velocity plots and scene sharing. | Add contextual experiment tools; retain the game's approachable controls rather than requiring a scientific editor. |
| [Crayon Physics Deluxe](https://store.steampowered.com/app/26900/Crayon_Physics_Deluxe/) | Drawn shapes become physical objects; level editor. | Bounded construction pieces are a useful first step. Arbitrary freehand geometry is a higher-cost alternative, not a prerequisite. |
| [Besiege](https://store.steampowered.com/app/346010/Besiege/) | Assembled, destructible machines; sandbox/editor and multiplayer. | Construction/load failure can serve friendly delivery/rescue challenges. Its combat framing is unnecessary here. |
| [World of Goo 2](https://store.steampowered.com/app/3385670/World_of_Goo_2/) | Structural building, liquids, terrain changes and varied material behaviours. | Deformable structures and terrain manipulation offer later options; generic water/thermal features already overlap our plan. |
| [Sugar, sugar — creator's announcement](https://www.bontegames.com/2011/02/new-bonte-game-sugar-sugar.html) | Drawing routes for falling sugar into cups. | Granular flow is distinct from rigid balls and conserved liquids. A sieve is our proposed extension, not a claimed feature from this source. |
| [Enigmo — developer](https://www.pangeasoft.net/enigmo/info.html) | Routing water/oil/lava droplets with physical tools; authoring and exchanging level sets. | Fluid routing and absorption are largely planned; authorable goals and portable puzzle packs are still weak. |
| [Captain Contraption's Chocolate Factory](https://store.steampowered.com/app/2166920/Captain_Contraptions_Chocolate_Factory/) | Repeating production, coating/breaking ingredients, completion quotas and player-made machines/puzzles. | Material transformation and sustained output goals add decisions beyond a single captured ball. |
| [Opus Magnum — developer](https://www.zachtronics.com/opus-magnum/) | Alternative solutions scored for simplicity, speed and compactness; animated exports and puzzle sharing. | Useful optional mastery/replay precedent. This is an automation puzzle comparison, not a Newtonian physics reference or reason to add mandatory programming. |

## What is present, planned, or genuinely missing?

| Area | Repository evidence | Audit disposition |
| --- | --- | --- |
| Basic rigid-body delivery, ropes, rotation, optics, timing/logic and sound | Catalogue/classes exist; proof status varies and some historical proof is stale after migration. | Existing work to finish and re-verify, not new proposals. |
| Gears, generators, moving containers, cutters, launchers, character interactions and unusual TIM devices | Active TODO sections 4–6 and historical references. | Already planned, often not implemented. Convert multi-part umbrella rows into separate records. |
| Water, pneumatic storage, thermal extensions, advanced light/sound and radiation | Sections 5–6; [component research](component-research.md), [radiation research](radiation-component-research.md). | Extensive planned coverage. Do not add duplicate “water”, “laser”, “radiation” or “steam” tasks. |
| Campaign, guidance, rewards, grid placement, grouped menu and physics difficulty | Existing explicit active tasks. | Preserve and integrate. No extra reward currency, daily pressure or mandatory leaderboards needed. |
| Free workshop and persistence | `ui/Workshop.cs` loads a free workshop; Save/Load uses `user://workshop.json`. | Sandbox exists. Multiple named machines, portable puzzle exchange and player goal authoring are distinct missing capabilities. |
| Simulation inspection | Internal `World.Step()` and diagnostics exist; TODO already mentions pause/slow-time determinism and feedback. | Internal stepping is not a player-accessible pause/step/replay interface. Complete that specification rather than claiming simulation stepping itself is absent. |
| Environment | `MachineData.Gravity` exists; historical environment research already exists. | A visible, validated authoring/preview/save contract for environment presets remains under-specified. This is separate from player difficulty. |
| Goals | `GoalKind` includes capture, activation, power, rotation and timed variants. Other event outcomes are already planned. | Quotas, sustained throughput, ordering and end-state invariants need explicit contracts; avoid duplicate generic “more goals” tasks. |
| Assemblies/structures | Engine joints and planned releasable bridges exist. | Player construction pivots, linked structural members, vehicles and reusable assembly editing are not established by kernel support. |

## New candidate register

Priority here is evaluation order: **A** improves everyday experimentation; **B** adds reusable physical decisions; **C** is an expensive optional expansion. It does not override the TODO's physics-first delivery order. All records are open; names are suggested teaching titles, not reserved campaign slots.

| ID / priority | Candidate and distinct decision | Dependencies, acceptance/control, teaching example |
| --- | --- | --- |
| GAP-01 / B | Structural beam/brace: distribute cargo load with geometry. | Typed endpoints/materials and shared rigid assemblies. Compare a supported triangular frame with an unbraced control under the same load. “Brace Yourself.” |
| GAP-02 / B | Player-placeable pivot/linkage connector: choose what rotates and what stays fixed. | Build on shared joint constraints; bounded attachments and visible motion limits. Demonstrate a driven linkage and disconnected control. Kernel hinges alone do not close this task. “Joint Effort.” |
| GAP-03 / B | Passive wheel/axle: rolling cargo chassis with real traction. | Moving assemblies, collision and mass accounting. Compare roll, slip and blocked wheel; no scripted travel path. “Wheel Meet Again.” |
| GAP-04 / B | Driven wheel: convert supplied power into vehicle motion. | GAP-03 and supported torque/load model. Powered travel, power-loss/coasting and overloaded/stalled controls. Keep separate evidence from the passive wheel. “Driven to Deliver.” |
| GAP-05 / B | Load-limited structural connector: choose strength and controlled failure. | GAP-01/02, measurable load, explicit finite breaking threshold and readable stress cue. Survive below threshold; break above it; restore topology on Reset. Extends existing releasable bridges rather than replacing them. “The Last Straw.” |
| GAP-06 / C | Granular dispenser: finite grains pile, flow and jam. | Deterministic conserved quantity, collisions and a measured performance budget. Empty source stops; jam persists until a physical change clears it. “Against the Grain.” |
| GAP-07 / C | Granular sieve: separate by size through physical apertures. | GAP-06 and an independently specified size distribution. Mixed feed sorts; all-oversize control blocks; no hidden label-based routing. “Sift Happens.” |
| GAP-08 / C | Coating/dye station: transform cargo surface state for a later operation/goal. | Finite material supply and explicit material-state transition, distinct from optical colour channels. Uncoated input fails the relevant downstream condition; depleted station cannot coat. “Coat of Many Colours.” |
| GAP-09 / C | Fragmentation station: break a finite input into usable smaller cargo. | Breakable geometry, bounded fragment budget and conserved mass. Intact control fails a narrow delivery route; fragments remain physical and cannot multiply indefinitely. “A Smashing Success.” |
| GAP-10 / A | Multi-selection and reusable assembly blueprints. | Copy/move/rotate an actual group with internal links preserved; fresh instance IDs, declared exposed ports, inventory charged per constituent, one coherent Undo. Reject dangling external links. “Some Assembly Required.” |
| GAP-11 / A | Player puzzle editor. | Author fixed setup, allowed inventory, goals, hints and environment; test in player restrictions and retain a demonstrated solution separately from the playable puzzle. Reject invalid references and impossible authoring configurations without importing a solution during proof. “Mind Your Own Business.” |
| GAP-12 / A | Portable puzzle/machine files. | Export/import the current validated schema and typed content references, with size limits and clear unsupported-input errors; no migration/fallback aliases. Round-trip a machine and a puzzle independently in a fresh session. A picture is not a playable export. |
| GAP-13 / A | Named save library and local player profiles. | Separate constructions and progress by typed identities, explicit overwrite/delete, thumbnails and touch navigation. Two saves and two profiles survive reload without overwriting each other. Does not require accounts/cloud services. |
| GAP-14 / A | Player simulation transport controls. | Pause, single simulation step and slow/normal playback with identical simulated outcomes at equal simulation time. Clear Run versus Reset states; touch controls. Replay is a separate deterministic recording/seek contract, not an assumed consequence of slow motion. “Wait a Second.” |
| GAP-15 / A | Optional causal inspection. | Selected-object traces of force/load/energy or signal transitions, with units and a visible blocked/unpowered reason; clear overlays on Reset. Compare an intentionally disconnected/stalled machine. Avoid unsolicited full-solution previews and permanent CAD panels. “Cause for Celebration.” |
| GAP-16 / B | Quantitative and sustained goals. | Individually specify quantity, output-rate window, event order and protected end-state constraints as typed goal variants. Reject transient false wins, double-counting and output achieved before the valid interval. “A Steady Job.” |
| GAP-17 / B | Authored construction zones. | Visible bounded allowed/forbidden regions independent of camera/bench clipping. Validate the actual rotated shape and attached assemblies, including grid snapping at boundaries; failed placement explains why. “Room for Improvement.” |
| GAP-18 / B | Authored environment presets. | Expose existing gravity and only implemented atmosphere/other settings with typed presets and faithful previews. Preserve through save/export/replay. Compare the same machine under two authored environments; changing difficulty must not secretly swap the authored world. “Down to Earth.” |

Common completion gate for adopted candidates: enum-typed closed sets through UI/runtime/tests, typed extensible IDs, valid serialization boundaries, focused correctness and production build, actual-UI positive and meaningful negative/control evidence, relevant connections, exact construction restoration after Run/Reset and reload. Keep per-part supported modes identifiable. Record revision, recipes, assertions, retained failures and captures, then commit/push each independently verified element. This audit performs none of that implementation verification.

## Existing commitments that need sharper specifications

- **Historical candidate children:** extend todo-227/228 with one record per named element and edition; use the evidence register above. This closes a research ambiguity, not implementation.
- **Winch, slingshot/catapult and zipline recipes:** references/recipes need explicit constituent mechanisms or dedicated piece specifications before campaign authors rely on them. Do not count a level name as a delivered element.
- **Finite electrical supply/storage:** existing radiation and power plans need a concrete energy-budget dependency. Resolve battery depletion and any adopted storage component individually; do not claim the current binary supply already stores charge. A full resistor/diode/capacitor circuit editor is not implied.
- **Existing character/thermal/container umbrellas:** split each distinct behaviour and meaningful material variant into its own proof record without awarding duplicate credit to overlap.
- **Pause/slow playback and favourite/replay rewards:** link GAP-14 to existing simulation/reward tasks; distinguish player controls, saved construction, rendered clip and reproducible replay.

## Campaign integration and positive play

Keep the planned **150 levels**. Do not quietly add a 151st level or replace an existing required introduction to make these candidates fit. The current catalogue plus all planned elements already needs a mode-level introduction/practice/reuse capacity audit.

If GAP-01–05 are adopted, place construction and passive rolling after basic gravity/contact, pivots after levers, powered wheels after torque/power, and structural limits after stable-load examples. Revisit those skills with cargo, water weight and timed gates. GAP-06–09 need conserved quantity/material-state lessons before any combined processing challenge. Radiation's existing teaching sequence remains accounted for.

For each adopted element, name an exact introduction, a guided practice and at least two later uses in the campaign ledger. Reallocate existing levels explicitly, recording displaced lesson coverage; if the ledger cannot fit, report that capacity conflict instead of compressing several unfamiliar systems into one compulsory level. Titles above are proposals, not a second competing level list.

Teach one new decision, allow a quick successful experiment, then add one already-learned constraint. Offer optional efficiency/compactness/throughput achievements only after a clear base win. Quota goals can reward a robust useful machine without requiring perfection. Hints, slower playback and retries must not reduce progression rewards. Let players save and share a favourite machine privately before asking them to compete publicly. Test enjoyment with players; a physics assertion cannot prove that a puzzle feels rewarding.

## Deliberately separate, higher-cost possibilities

Record these as research alternatives rather than adding mandatory campaign ingredients:

- Deformable/soft-body cargo and excavatable terrain (World of Goo comparison): prototype only if they create a decision that current ropes, springs, fluids and movable barriers cannot express.
- Arbitrary freehand geometry (Crayon Physics/Algodoo): bounded beams and pivots are the smaller useful starting point.
- Community browsing/moderation and cooperative construction (Contraption Maker): first establish local editor, validated exchange and deterministic assemblies. Cooperative puzzle solving, competitive head-to-head and online synchronization are separate designs.
- Electrical circuit design and programmable scripting: inconsistent with the current approachable component UI unless deliberately selected as another product mode.
- Unrestricted teleportation, copied cartoon characters and unexplained energy creation: not required for series parity; retain the existing constrained-physics and original-art direction.

This sweep establishes a sourced shortlist and explicit evidence gaps, not an exhaustive executable audit of every release. The next useful work is to turn high-value accepted candidates into small reproducible prototypes after the current physics and component obligations.
