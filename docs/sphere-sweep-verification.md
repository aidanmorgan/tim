# Finite-radius sweep groundwork

27 September 2026. This is a prerequisite for the electric linear pusher, not a delivered puzzle element.

## Implemented

`SphereSweep.Cast` conservatively advances a finite-radius sphere against a supplied signed-distance surface. Its enum result distinguishes clear travel, contact and an initially overlapping construction. It samples the endpoint, rejects invalid inputs/surface results and rechecks tangent/separating contacts so travel inside a curved bore cannot silently bypass later contact. Double-precision travel accumulation avoids a stalled float increment. Box and sphere surface helpers accompany the existing tube, bend and frustum surfaces.

The geometric tolerance is 0.0001 world units. The query requires finite signed-distance bounds that do not overestimate clearance, outward unit normals and a common rigid coordinate frame; non-uniform scaling is not supported. Tangential paths can require many small samples. This is not yet a broad-phase query or a performance claim.

## Evidence

- `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet --filter FullyQualifiedName~SphereSweepTests`: 20 passed.
- `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`: 843 passed.
- `dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet`: passed.
- Tests cover thin boxes, four rigid rotations, misses, separating/tangent travel, stationary overlap, endpoint contact, bidirectional sphere contact, tube bore/shell separation, curved-bore tangency, narrowing frustums, bends and invalid inputs.
- Initial test compile failed because a static bend tangent method was accessed through an instance. Corrected the test call; no physics change or relaxed assertion was needed.

## Still required

World-level proxy collection and nearest-hit/owner selection, obstruction handling across interacting solids, force/work-limited actuator motion, power and independent direction commands, conflict/power-loss brake, endpoint outputs, original visuals/icon and actual-UI Playwright positive/negative/integration/Reset proof. No gameplay calls this utility yet. No browser proof is claimed; the pusher remains incomplete.
