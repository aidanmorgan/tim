# Finite launch-energy groundwork

27 September 2026. This is a tested prerequisite, not a playable cannon or a completed launcher family.

## Contract

`LaunchEnergyStore` starts empty and accepts only supplied power multiplied by elapsed time, capped at its finite authored capacity. Zero power retains the existing charge. Accepted and released work are separately accounted.

A release uses payload mass and incoming axial speed to calculate a positive outgoing axial speed. Actual kinetic-energy gain is deducted from storage; an authored speed limit retains unused charge. Rounding the result to the engine's float velocity representation may round down, never spend more energy than is available. Charge too small to produce a representable energy gain remains stored. Empty, speed-limited and sub-precision releases return explicit enum outcomes and change neither velocity nor energy. Invalid arguments throw before changing state. Reset clears storage and accounting.

An anchored launcher absorbs the impulse reaction, including reversal of negative incoming axial velocity. A caller must preserve transverse velocity and enforce chamber seating/occupancy, load identity, ready/trigger timing, a clear swept muzzle path and collision-safe flight. This scalar utility does not enforce any of those physical interlocks.

## Evidence

- 26 native cases, including a 225-combination mass/speed/charge precision check and 1,000 recharge/release cycles.
- Charging/capacity, partial energy, varied payload masses, positive/negative incoming motion, speed-cap retention, empty/sub-precision shots, work conservation, step partitioning, power loss, Reset and invalid-input atomicity covered.
- Full native suite: `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet` — 909 passed.
- Production web publish: `dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet` — passed.
- No gameplay caller yet; no Playwright proof is claimed.

## Cannon design still to implement and prove

Use a separately wired electrical supply to charge an ideal finite electrical launch store. An activation command is a trigger only, not an energy source. Electrical supply remains a binary ideal source as elsewhere in the game; this is not a voltage/current or battery-depletion model. Do not label it pneumatic pressure.

The playable cannon still needs a physical single-ball chamber and rotatable barrel, real incoming/reloaded payload identity, loaded/empty/ready cues, restrained recoil and an original icon in the existing cream/navy/cyan/gold palette. Empty or obstructed trigger attempts must retain charge. Define and test deterministic same-tick load/trigger behaviour; do not queue a surprising later shot from an empty trigger. Never spawn ammunition or teleport a ball from inlet to muzzle.

Before shipping: test chamber overflow, wrong-size/missed loads, incomplete/no charge, blocked muzzle, 3D aim, repeated shots/recharging, swept collisions and exact Reset/JSON replay, then construct positive and meaningful negative cases through actual UI Playwright controls. Add authoring/current-schema support and focused teaching content. Keep all unfinished requirements explicit and commit/push each individually proven part before starting another.
