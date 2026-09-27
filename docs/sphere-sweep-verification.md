# Finite-radius sweep groundwork

27 September 2026. This is a prerequisite for the electric linear pusher, not a delivered puzzle element.

## Implemented

`SphereSweep.Cast` conservatively advances a finite-radius sphere against a supplied signed-distance surface. Its enum result distinguishes clear travel, contact and an initially overlapping construction. It samples the endpoint, rejects invalid inputs/surface results and rechecks tangent/separating contacts so travel inside a curved bore cannot silently bypass later contact. Double-precision travel accumulation avoids a stalled float increment. Box and sphere surface helpers accompany the existing tube, bend and frustum surfaces.

`WorldGeometry.Sweep` collects every visible solid box, sphere, tube, bend and frustum, plus dynamic cargo spheres and both workbench proxies. It composes rigid part/proxy poses, returns typed obstacle kind and actual owner, excludes the requested owner, and selects the nearest contact deterministically (workbench, ordinal part ID, proxy order for exact ties). Initial overlap takes priority over a touching contact. Non-rigid proxy transforms are rejected explicitly. The query does not modify construction or cargo velocity.

The geometric tolerance is 0.0001 world units. The query requires finite signed-distance bounds that do not overestimate clearance, outward unit normals and a common rigid coordinate frame; non-uniform scaling is not supported. Tangential paths can require many small samples. This is not yet a broad-phase query or a performance claim.

## Evidence

- `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet --filter FullyQualifiedName~SphereSweepTests`: 20 passed.
- `dotnet test CuriousContraptions.tests --no-restore --verbosity quiet`: 853 passed (including 20 surface-sweep cases and 10 world-query cases).
- `dotnet publish CuriousContraptions.web -c Release -p:PlaytestDiagnostics=false --no-restore --verbosity quiet`: passed.
- Tests cover thin boxes, four rigid rotations, misses, separating/tangent travel, stationary overlap, endpoint contact, bidirectional sphere contact, tube bore/shell separation, curved-bore tangency, narrowing frustums, bends and invalid inputs.
- World-query tests cover deck/base/finite-table misses, nearest dynamic cargo, hidden and ignored owners, collection-order independence, overlap precedence, separating contact, rotated transparent boxes, independently transformed hollow proxies, invalid/scaled inputs and non-mutation.
- Initial test compile failed because a static bend tangent method was accessed through an instance. Corrected the test call; no physics change or relaxed assertion was needed.

## Still required

Actuator integration and obstruction handling across interacting solids, force/work-limited actuator motion, power and independent direction commands, conflict/power-loss brake, endpoint outputs, original visuals/icon and actual-UI Playwright positive/negative/integration/Reset proof. No gameplay calls this utility yet. No browser proof is claimed; the pusher remains incomplete.
