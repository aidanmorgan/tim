# Shared production occlusion queries

28 September 2026. Increment based on a6178c503a2eee132431c967bcee9fb3a08b3beb; full physics replacement remains incomplete.

## Production change

WorldGeometry.Trace now casts a point support shape with the shared CompoundCollision/ConvexSweep engine. The separate box, sphere, tube and signed-distance ray algorithms are deleted; no runtime fallback or old/new switch remains. Light, sound and airflow callers retain the enum-typed TraceMedium contract. Opaque declarations block light/sound; all solids block direct airflow.

SceneTraceGeometry compiles immutable support geometry and caches it by part lifetime. Pose, proxy declarations, hinge poses and dynamic radius changes invalidate the cache. Empty media geometry explicitly means no declared blocker. Hollow tubes, bends and frusta use the shared bounded-error builders with maximum surface error 0.005 game units, independent of difficulty. Transparent bores remain hollow, not filled hulls. This changes the former analytic shell representation within that stated error budget.

WorldGeometry.Sweep, WorldFlight, WorldHinges, original body velocity ownership and the old contact/rope solver are NOT replaced by this increment. Full production cutover, obsolete-code removal and refreshed affected-part evidence remain required.

## Native verification

Full suite: **1,711 passed, zero failed/skipped**. Fourteen new WorldTraceTests cover move/rotation/resize/removal/hiding/transparency invalidation, all three media, source/receiver exclusion, sphere declaration changes, open tube bore and shell interception, and invalid queries. Existing light, sound, airflow and campaign tests pass. No assertion tolerances were changed in existing tests.

## Real-UI browser evidence

Diagnostics-enabled web export passed. Actual palette, rotation/translation handles, wiring and Run/Reset controls were used through the existing direct-ui adapter, with no state setters or imported solutions.

| Case | Observed behaviour | Exact Run/Reset | Errors |
| --- | --- | --- | --- |
| shared-trace-solar-positive-v1 | Level 21 wins at tick 207; sampled motor speed reaches 6 | Yes | 0 |
| shared-trace-solar-blocked-v1 | Level 22 shaded panel times out at tick 3600; sampled motor speed remains 0 | Yes | 0 |

Both actual placed panels are at (-1,3,0), facing 180 degrees around Y. Both recorded connections are panel supply to motor power input. Outcome screenshots were inspected: the positive panel receives the cone; the negative wall lies between torch and panel. Byte-identical serialized Run and Reset records independently confirm transforms, properties and typed wiring restoration.

Retained records: docs/playtest-results/<case>.json. Screenshots: .playwright-mcp/<case>-outcome.png. These directories retain local artifacts and are ignored by git; this report records their essential observations.

**Retained verification gap:** the C# campaign audit exited 2 (incomplete) for both records because the submitted recipes omitted the campaign hash. No full campaign/nudge-audit pass is claimed. The independent exact Run/Reset comparison and sampled motor assertions above passed. Reproduction recipes remain in each record; historical artifacts were not rewritten.

Fresh sound/airflow UI proofs, every other affected optical part, continuous animation, browser frame-time/mobile qualification and the full difficulty matrix remain incomplete. Native coverage and these two light-path checks do not substitute for those per-part requirements.

## Source identity

- SceneTraceGeometry.cs: 823389a21fc13632f86f2fd9f0d8221680fa41046678fd339899ff7efc2da217
- WorldGeometry.cs: f7ace78eaff04c15d15d92efe4445f0221ee25f1314809e62f28e7f31008210d
- TubeProxy.cs: 0caf163ed3ce7f52d8ee1641b08dcc437e8f2c446feacf8badc744c092aebe0d
- WorldTraceTests.cs: 6677351dacab72416186f68516bf6794aa20a7dec97a6abc9cd8adb63da224a7

Diagnostics-disabled production Release export passed. A fresh browser context displayed the engine banner and visible canvas with zero console/page errors. This is startup smoke, not additional gameplay proof. git diff --check passed. Anvil dependency queries were ready and pre-write gates allowed the edits.
