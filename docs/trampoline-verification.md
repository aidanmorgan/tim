# Passive trampoline verification

Local work on 27 September 2026, based on b2e00cf plus the implementation accompanying this document. Off-centre browser review is complete; campaign and extended interaction/mobile work remain open.

## Model

The trampoline is a finite spring/damper contact surface, not a powered launcher. The body's projected footprint must clear the rigid frame, and contact must enter from the local top. Contact force is max(0, tension × indentation − damping × normal speed). Damping is derived from the authored damping ratio and body mass. Forces are applied once per physics substep; rope projection iterations do not multiply them.

Tension is bounded to 120–1200, damping ratio to 0.08–0.8. Defaults are 180 and 0.12. The rest bed is at local Y=0.2 with a maximum 0.65 stroke. A non-bouncy solid back absorbs overloads; the cream rim is also non-bouncy. No electrical or activation sockets and no imposed launch velocity exist.

The membrane uses independent, massless contact springs and a rendered indentation envelope, not shared cloth inertia or propagating waves. Contact phases use an enum. The visible mesh follows actual indentation and is constrained below each contacting sphere; it is rebuilt only when contact geometry changes. Rendering does not alter physics. Body/box contacts still use the existing stepped solver; no continuous-collision or measured TIM-fidelity claim is made.

## Tests

- **34 focused trampoline cases pass; 823 total native tests pass.**
- **48 Playwright driver-harness tests pass**, supplementary to browser checks.
- Diagnostic and production Release browser publishes succeed (exit 0); production diagnostics are disabled.

Cases include incoming/returned energy for several masses, speeds and tensions; five rotated surface normals; static-load equilibrium; rigid rim, finite-area misses, underside and grazing contacts; genuine tension-dependent compression/contact time; simultaneous contacts with reversed insertion order; overload stroke/energy bounds; exact Reset and JSON replay at three contact times; off-centre rendered-skin bounds; invalid parameters; aimed drop → receiver and missed/wrong-angle controls.

The sampled kinetic-plus-elastic energy bound allows up to 5% fixed-step integration error; tested outgoing speeds remain below 95% of incoming speed. These are bounded regression checks, not an exact energy-conservation theorem for every interacting scene.

```sh
dotnet test CuriousContraptions.tests --no-restore --verbosity quiet
node --test tools/Playtest/direct-ui.test.cjs
dotnet publish CuriousContraptions.web -p:PlaytestDiagnostics=true --no-restore --verbosity quiet
dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet
git diff --check
```

## Real-UI Playwright evidence

Actual palette entries, move/rotation handles, Run and Reset; no game-state setters, imported solutions, numeric placement or storage edits. Each construction has no connections, as expected for this passive element.

- **trampoline-drop-v1:** bed (0,3,0), ball (0,6.999069,0). At 0.82 seconds the ball approaches; at 0.92/1.01 seconds the cyan bed is indented; at 1.12/1.78 seconds the ball rises and the bed returns. Exact Reset; no browser errors.
- **trampoline-missed-v1:** ball moved to depth 2, actual Y=6.9867673. Bed stays flat; the missed ball bounces on the solid workbench and rests below it by 4.55 seconds. Exact Reset; no browser errors.
- **trampoline-receiver-v1:** bed rotated −30° around Z; ball above it; receiver at (5.4,1.195014,0). Ball reaches the receiver by 1.79 seconds and remains inside at 4.58 seconds. Actual quaternion is (0,0,−0.25881904,0.9659258). Exact Reset; no browser errors.
- **trampoline-edge-v1:** final skin-envelope build, ball (0.8,6.9886303,0). At 0.91/1.00 seconds the localized depression follows the load near the frame; at 1.10/1.78 seconds the ball rises and the membrane returns. The frame stays fixed. Exact Reset; no browser errors.

Records: ignored `docs/playtest-results/` JSON named for each case, with complete recipes/actions and actual Run/Reset configurations. All four recipes were checked against actual part kinds and positions (maximum coordinate error 0.013233, below the 0.025-unit check bound), with no unexpected connections. Case-matched motion, holding, outcome and settled images are in `.playwright-mcp/`. CCFRAME excludes dynamic balls; trajectory observations above are visual, with native tests separately asserting capture and energy.

## Retained failures and open work

The first native receiver setup at (3.5,2,0) ricocheted from the far rim. Moving it to (4.5,2,0) still intersected the rim along the descending trajectory. Sampling the path led to the successful receiver placement (5.4,1.2,0); no trampoline forces or capture tolerances were changed to make the reference pass.

A final rendered-skin envelope improves deep/off-centre contact geometry without changing forces. The first three browser cases precede this presentation-only refinement; the final edge case checks the updated renderer.

Introductory campaign/pipe puzzles, broader interacting or stacked loads and rope contacts, extended real-UI boundary checks, mobile readability and sustained animation/performance review remain open. The 75-level goal and per-part evidence audit are not complete.

### Stacked-load investigation (28 September 2026)

On revision `6fac063` plus the new `TrampolineStackTests`, two centred unit-mass balls at Y=4.5401 and 5.2201 above a bed at Y=4 fail the proposed 0.03-unit/second settling requirement after 2,400 ticks (20 seconds), in both insertion orders. The lower body's final speed is 0.059158325; the last twelve samples show a repeating low-amplitude velocity cycle, while its height ranges from 4.431122 to 4.4312134. Compression is approximately 0.1088, consistent with combined weight divided by stiffness (19.62/180). Finite-state, separation, stroke and 1% initial-energy-bound checks pass before the settling assertion; Reset assertions occur after that failure and are therefore not yet proven by these cases.

The retained diagnostic log is `docs/playtest-results/trampoline-stack-native-v1-failure.txt`. Reproduce with `dotnet test CuriousContraptions.tests --no-restore --filter FullyQualifiedName~TrampolineStackTests --logger 'console;verbosity=normal'`. Inspection shows compliant forces are applied after swept rigid flight in each substep; the relationship between that phase ordering and the observed velocity cycle needs investigation. At that failure checkpoint, no production physics or acceptance threshold had been changed. This was an unresolved native test, not browser proof or a completed component. A temporary diagnostic compile error caused by ambiguous `Environment` was corrected to `System.Environment` before the recorded run.

Anvil's graph supplied no useful dependency/test mapping for the solver hooks. Its write gate was authentication-unavailable and allowed edits with a warning; direct inspection and full tests supplemented it.

### Force-timing correction and refreshed evidence

The current substep applies compliant forces before rope velocity constraints and swept flight, rather than leaving the membrane impulse until after collision resolution. Each compliant force is still applied once per substep. No bounce coefficients, energy limits or settling thresholds were relaxed. The two new cases now also require both bodies to stay below 0.03 units/second throughout the final second, and reach/preserve the exact Reset assertions. All **36 trampoline cases and 1,173 native tests pass**. Diagnostic and production Release browser builds pass.

Real-UI replay recipes and the exact adapter snapshot are in [force-timing recipes](trampoline-force-timing-recipes.json). The workshop is now row 61; older recipe rows remain historical evidence. Each new run contains the expected three parts, no links, positions within 0.025 units of the UI recipe, zero console errors and exact Run/Reset configuration equality.

- `trampoline-force-timing-receiver-v1`: 100 frames; tilted bed sends the same ball into the basket. At tick 948 it is at (5.255142,1.1600494,0), velocity (-0.0036621094,0,0). Outcome image inspected and confirms containment.
- `trampoline-force-timing-missed-v1`: 100 frames; Z=2 ball misses bed/receiver and rests on the floor at (0,-0.11999512,2), zero velocity, tick 948.
- `trampoline-force-timing-stack-v1`: 255 frames through tick 2808 (23.4 seconds). UI placed the bed at Y=3.9950578 and balls at Y=4.5391693 and 5.2191696. Across all 45 samples from tick 2280 onward, maximum body speed is 0.009567261. Final ball heights are 4.4265137 and 5.1067047; the inspected outcome image shows the centred supported stack. This is sampled browser settling proof, supplemented by per-tick native checks, not arbitrary off-centre stack stability.

Complete ignored logs are `docs/playtest-results/<caseId>.json`; screenshots are under `.playwright-mcp/`. Source SHA-256: MachineWorld `48398e306ecac17b2480e481134c9ff5101c093c480a6c5d92d507366b26f0d4`, unchanged TrampolinePart `c781daea463a186c43e70aae9d438854e66e678f66e145613abe9ff2fa0203ef`. The original model paragraph records the historical stepped solver; current rigid flight is swept. This correction closes the centred two-ball settling issue only. Rope-loaded contact, broader stacks, campaign/pipe lessons and sustained/mobile visual verification remain open; the trampoline component is still unchecked.

### Rope-loaded contact verification (28 September 2026)

On `8d7f58d` plus test/evidence additions only, five new enum-driven native cases cover a four-unit mass with a short overhead tether, a longer slack tether and no connection. Linked cases run with both endpoint directions. Across 1,200 ticks they assert finite visible motion, the stroke limit, rope extension no greater than 0.002, and kinetic + gravitational + membrane energy no greater than 101% of initial gravitational energy. The short tether prevents membrane contact; the long tether becomes taut during compression but allows upward rebound. Every case proves exact serialized Reset, empty membrane state and an identical second-run state signature. **All 1,178 native tests pass**, and diagnostic/production Release publishes succeed; no production physics changed in this addition.

[Reproducible UI recipes and adapter](trampoline-rope-recipes.json) retain the actual construction method: stage the trampoline beside the weight, connect anchor to weight, lift the weight by its Y handle to introduce slack, then move the trampoline beneath it. The short-tether control connects at the final raised height. No rope-length setter, state injection or numeric placement menu is used.

- `trampoline-rope-slack-v1`: 101 frames through tick 960. One actual Rope/Tie→Tie connection of length 4.2115808. Weight mass 4 starts at Y=5.4897666, reaches Y=3.1992493 and rebounds with sampled upward speed 1.6348267. Maximum sampled rope excess is negative (−0.000180668); final position (0,3.4906616,−0.16055298). The small depth swing follows the UI-placed anchor's Z=−0.2 and its 0.18 socket offset.
- `trampoline-rope-taut-v1`: 100 frames through tick 948. Same part types and one rope of length 1.911638; weight starts at Y=5.499069 and never descends below 5.4989624, well clear of the bed. Maximum sampled rope excess is 0.000006998.
- Both actual configurations match recipe positions within 0.025 units (including post-connection moves), contain exactly the expected three parts and one typed link, report zero browser errors, and restore exact Run/Reset configuration equality. Outcome images were inspected: supported load versus suspended load. Complete ignored logs, actions and screenshots are retained at `docs/playtest-results/<caseId>.json` and `.playwright-mcp/`.

These checks establish the specific fixed-anchor tether interaction, not all pulley networks, off-axis loads or arbitrary rope routing. Broader interactions, introductory campaign/pipe puzzles, refreshed off-centre motion and sustained/mobile visual review remain open. The trampoline entry remains unchecked.

### Introductory lesson and enum parameters (28 September 2026)

“A gentle rebound” is draft lesson 61: fixed ball at (0,7,0), fixed basket at (5.4,1.2,0), one placeable trampoline with reference position (0,3,0) and −30° Z rotation. There are no connections or power requirements. The author supplies bounded position/rotation correction of 0.2 units/5° at Forgiving, 0.1 units/2° at Balanced and zero at Precise, blended over 0.4 seconds. These are authored settings, not proof of the deferred repeated difficulty/monotonicity matrix. Final gradual ordering remains open; this lesson is appended during component development.

The old `TrampolineParameters` string-constant class is removed. Runtime reads now use `TrampolineParameter` enum values through the validated resource boundary, with test authoring updated at that boundary. Canonical names remain `tension` and `damping_ratio`; undefined enum values are rejected. No aliases, fallback or compatibility path was added, and no physics constants changed.

Six native lesson checks cover successful references at all three difficulties and Balanced flat, depth-missed and missing-bed controls, each with exact Reset. A seventh test checks parameter names/invalid enum rejection. **All 1,185 native tests pass**, plus diagnostic and production Release publishes.

[Real-UI recipes and adapter snapshot](trampoline-lesson-recipes.json):

- `trampoline-lesson-reference-v1`: place the bed from the palette, rotate it using the ring and Run. Actual position (0,3,0), quaternion (0,0,−0.25881904,0.9659258). Win at tick 252 (2.1 seconds), 42 diagnostic frames; outcome image shows the original ball in the basket.
- `trampoline-lesson-flat-v1`: same construction with no tilt. The ball rebounds vertically and remains above the bed at (0,3.4859467,0), velocity (0,0.0031585693,0), tick 948. No win during the observed run; 100 frames. Native controls observe through tick 1200.
- Both contain the expected two locked fixtures plus one unlocked trampoline, no connections, zero console errors and exact construction Reset. Outcome images inspected; complete local evidence retained under `docs/playtest-results/<caseId>.json`.

Typed authoring in `tools/Campaign/TrampolineLesson.cs` reproduces checked-in content. A semantic comparison confirms the preceding 60 lessons are unchanged. Canonical full-campaign SHA-256: `e37ef7b11779a5ad363b364a043eddb376f6fd28ed8983367ad615fb2a521637`; TrampolinePart: `1b2bf856fa26639734306812acae3761f9658a1a2b10bb3b19e951abaac0b086`. There are now **61 draft lessons**, and Free workshop moves to row **62**. Historical recipes retain their original row numbers. Pipe integration, broader interaction/motion/mobile evidence, final 75-level progression and exhaustive difficulty testing remain open.

### Rebound-to-pipe integration (28 September 2026)

Three native enum-driven route cases on `ed19b88` plus tests/evidence only prove an aimed rebound, a flat-bed control and a depth-misaligned pipe control. The positive case must cross the inlet within the bore, cross the pipe centre, spend at least 20 ticks fully inside the finite bore, then leave beyond the outlet collar plus ball radius. Interior radial clearance, finite state, a 1% total-energy bound, exact serialized Reset and deterministic replay are asserted. All **1,188 native tests pass**; diagnostic and production Release builds succeed. No production physics changed.

[Real-UI recipes and adapter](trampoline-pipe-recipes.json) use Free workshop row 62, three palette parts and no connections:

- `trampoline-pipe-reference-v1`: bed at (0,3,0), −30° Z; ball at (0,6.999069,0); horizontal pipe at (3,3.091826,0), length 3.6. Four fully interior diagnostic samples at ticks 144, 156, 168 and 180 cross from X=1.7872162 to X=3.8080902 inside the bore. At tick 216 the ball is beyond the far collar at X=5.723572. Images at 1.18 and 1.72 seconds were inspected and show the ball inside the clear pipe and leaving its outlet. 100 total frames.
- `trampoline-pipe-missed-v1`: same tilted rebound and a pipe at Z=2, actual Y=3.096811. No diagnostic sample enters the bore. 101 total frames.
- Both actual configurations contain the three expected kinds at positions within 0.025 units of the recipe, no links, zero errors and exact Run/Reset equality. Full ignored logs retain recipes, actions, configurations, frames and image paths under `docs/playtest-results/<caseId>.json`.

This closes the focused straight-pipe combination check, not a completed campaign puzzle: the workshop construction has no receiver goal. A follow-on lesson, broader routes/loads, sustained motion and mobile review remain open. Historical evidence is unchanged.
