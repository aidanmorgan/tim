---
title: "9.6 — Both Gate"
status: in-progress
baseline: 8aaacd77655c1ce1fb4cfff374bcb8d25df54645
context:
  - TODO.md
  - docs/planning/elements/CAT-013-both_gate.md
  - docs/architecture/element-package-isolation.md
  - docs/gpu-f32-physics.md
---

# Outcome and decisions

Ship the package-owned Both Gate (CAT-013): two separately numbered electrical conditions AND an independent bottom energy supply drive a real Powered Gate. The three lamps show committed condition/output availability. There is no activation port, numeric parameter, stored power or per-element update loop.

Exact acceptance: Story9.6 and CAT-013 G1–G15/A1–A6 as applicable to And. Later operations remain their individual stories; this slice introduces the typed And law, not other packages. Undefined operation and socket values reject. Current9.5b source/contact topology is the base; target socket identity must be retained rather than collapsing all inputs into one owner bit. Power provenance follows PowerIn only, never the condition edges.

Resolved stale CAT questions: Powered Gate is the observable supplied load (A2), not an invented electrical Signal lamp. Future Motor integration remains pending at its named story. Switch electrical mode and Latch supply controls already exist. Presentation uses current f32 declarations/evaluator. Topology compiles on Run; disconnect between Runs or committed source/contact loss tests retraction, with no live wiring mutation.

# Acceptance

- All four And rows × supply present/absent produce exact output, and actual consumer displacement/velocity agrees. Lit conditions without PowerIn never create energy.
- Two-gate chains/reconvergence settle without owner/link-order delay. Source-free mutual cycles remain unpowered; removing a real source or opening a contact clears the loop for ten further ticks; restoration follows current committed state, not retained power.
- FirstIn, SecondIn and PowerIn are distinct typed input sockets. Duplicate identical edges, wrong-domain and undefined inputs reject atomically; distinct-source fan-in remains legal. Existing consumer/source accounting remains constrained; condition sources do not appear as upstream energy provenance.
- Package geometry, material, colour, converging bars, numbered marks, three lamps and four socket anchors match CAT-013. Existing f32 SmoothStep0.1s drives committed state; failed/discarded candidates publish nothing and Reset restores slate/Neither.
- Native wiring controller offers exactly three Battery→gate choices and no suggestion; Cancel changes neither construction nor Undo. Chosen socket persists through save/load, and Reset restores authored physical state. Existing source/contact/Clock behavior remains.
- Browser cat-013 construction/visual proof is retained for post-catalogue gap finding, not claimed complete now.

# Tasks

- [x] Extend typed electrical declarations, bundle validation and socket mappings; create elements/BothGate package, scene, icon and membership. No runtime PartId recognition.
- [x] Replace owner-only electrical edges/reachability in SupplyRoute.cs with socket-aware shared input/output evaluation. Preserve current contact/source path, energy-only UpstreamSources and direct finite-work admission; no parallel fallback.
- [x] Carry immutable committed gate reads through PhysicsTickInputs, PhysicsPresentationRead, current wire schema/generated callers and declared presentation drivers. Preserve candidate commit/discard and Reset ownership.
- [x] Update existing generic wiring UI labels/choices and package validation/scene membership.
- [x] Explicit cleanup: remove superseded owner-only traversal and obsolete assumptions/fixtures/callers; no legacy gate record exists to retain, no compatibility bridge or new isolation Known debt.
- [x] Latest design alignment: package declarations/art own identities, shared typed engines own laws; f32 presentation and existing physical ABI boundary remain explicit, no unrelated9A.44 migration.
- [x] Production C# UNIT tests PRIMARY: truth/supply controls, real consumer motion, chains/cycles/order/source loss/restoration, committed presentation, atomic refusal, native chooser, ticks/Reset/save-load. Integration supplemental; no Chrome until all catalogue elements complete.
- [ ] Build affected closure once per relevant inputs/config; reuse matching --no-build --no-restore tests. Validate schema/strictTS, worker and actual compiler-input membership/resources. Retained reviewer exact SnapshotApproval precedes protected publication and remote/CI/artifact/deployment verification.

# Current state and original proof

Implementation and scoped native checks complete; exact independent SnapshotApproval and publication remain pending. Baseline8aaacd77; prior9A26 terminal receipt11593e carried unchanged. Anvil graphdb7507 not_ready; bounded source reads used.

Owner-only edges/traversal replaced by one socket-aware source-seeded And graph. No per-part recognition, fallback, compatibility reader or added isolation Known debt. Current schema/generated C#/TS carry immutable logic rows; admission checks exact owner population/operation truth and neutral installed state. Numbered UI icons reused; only generic power choice added. Future operation laws, legacy per-part Powered event delivery, Motor integration, common9A.44 and Chrome qualification are not claimed.

Original raw tool references and applicability:
- Debug builds72865f17.34s, d8e8bf10.87s,1121424.28s,afc4d94.28s,35fd7a10.36s,5b29654.37s,a599d26.95s,56c1176.72s all clean. Rebuilds followed changed inputs; tests used --no-build --no-restore.
- Existing SwitchElectrical/PoweredGate/ElectricalSupply/Hold d56e46:66/68 passed2s; two scene parse failures corrected leading-dot decimals. e2a6c5 compile failure corrected test Concat instance-array inference.
- BothGate/PoweredGate3aff03:27/27 passed487ms. Eight truth/supply rows through production physics, actual displacement, cycle/order/reconvergence/source controls, energy-only Clock ancestry, save/exact Reset.
- New nativeee869e:17/18 passed525ms, including chooser, read/response admission and real device failure/discard. Lamp quarter expectation incorrectly used cubic .15625; fixed independent expected quintic .103515625 matching current SmoothStep, no runtime curve change.
- Contact-opening mutual loop/lamp59eb9a:2/2 passed414ms. Battery remains enabled while Hold opens; ten-tick loop retraction after actual consumer motion. Native .025/.05s colour samples, hide/save/Reset; b51005 also passes pause/reprojection and UI icon check.
- Named Latch8ef7e2:1/1 passed125ms: actual Set120 powers121, Reset180 retracts181; prior blade motion; source loss141–150 and restoration151 while conditions remain lit.
- Membership/parity/ports/capacity/schema/isolation/chooser b4290b:147/149 passed13s; stale package count and formerly undefined socket5 corrected to current count and undefined255.6b82ce11/12 exposed new generic socket_power missing in exact icon list; corrected pin then b51005 passes. Historical geometry golden unchanged; current package parity checks new geometry separately.
- Worker050705 clean13.32s /tmp/9-6-worker.binlog; strict TS7073c4 exit0. Current C#/TS generated with verified pinned flatc25.2.10; format/schema native checks in b4290b.
- Bundle validator4c5ecc1/1 passed27ms: valid And, null/undefined operation, missing condition, operation on Source reject. Bundle roundtripcb767f36/36 passed286ms. Initial cb58f2 missing worktree assets resolved by normal restore/buildf2ef8d clean8.42s.
- Actual compiler-input548c3e1/1 passed295ms, original Debug /tmp/9-6-native-membership.binlog and Release worker logs linked in .anvil/9-6-compiler-inputs. First b51005 invocation used relative path under test output and failed before checking; corrected absolute environment path, no source change.

Commands: dotnet build affected project [--no-restore] [-c Release] -v:q [-bl:path]; dotnet test same project --no-build --no-restore --filter affected classes/methods -v:minimal. Native CuriousContraptions.tests, bundle tools/BundleElements.Tests, worker CuriousContraptions.Simulation. TS installed6.0.3 with tools/e2e/tsconfig.json and existing typeRoots. No Chrome or broad repeated test campaign.

Current brief/policy clauses explicitly require cleanup, latest-design alignment, production C# UNIT tests PRIMARY/integration supplemental. Next: exact frozen review, protected publication and exact-SHA CI/artifact/deployment verification.
