---
title: '9.5b Impact Switch electrical mode'
type: feature
created: '2026-10-22'
status: in-review
route: dispatch
story_key: 9-5b-impact-switch-electrical-mode
baseline: 9225e4923748e1489fbf8a472802b2dfeff77546
context:
  - docs/planning/elements/CAT-063-switch.md
  - docs/architecture/element-package-isolation.md
  - docs/gpu-f32-physics.md
---

# 9.5b — Impact Switch electrical mode

<frozen-after-approval>

## Intent and observable outcome

A qualifying impact closes the Impact Switch's independently supplied PowerIn→Supply contact. A Battery routed through it powers and physically opens a Powered Gate; ActivationOut remains its unchanged one-shot contract. Story9.5b and CAT063 facts1–5, todo147 and IX06/IX07 remain authoritative. C-Switch has terminal scoped Pass on the baseline (CI39664614218:2074/2074; deployment7235903146).

## Boundaries and acceptance

- Given Battery→switch→Powered Gate and an inclusive qualifying impact, when simulation advances, then the committed latch closes the electrical contact and the gate blade moves. New physical contact affects the following integration step, matching current shared actuation timing.
- Given a miss, below-threshold impact, absent/disabled supply or missing wire, then the gate remains unpowered. A closed contact is not an energy source.
- Given both electrical and activation connections, then the same one-shot latch drives both independently. Supply loss clears power but preserves activation/contact memory; restoration powers a still-closed contact. Reset clears both domains and restores the button; save/load preserves typed wiring and parameters.
- Given contact chains and sourced cycles, then propagation is coherent and independent of instance/edge order. An unsourced cycle never supplies. Recompute each snapshot from admitted enabled sources, not retained power.
- Given ambiguous target sockets, native connection-controller selection/Cancel/Undo preserves the exact chosen domain and no uncommitted wire.
- Current package geometry, threshold, presentation and physics ABI remain unchanged except visible electrical ports. No element-id engine branch, alternate solver, fallback, compatibility path, new platform or new isolation debt.
- Motor shaft momentum, switched_motor/gated_belt campaign goals and direct-wire-bypass lesson remain explicitly required with the named future Motor/campaign integration; gate/current-native controls do not claim those unavailable outcomes complete. The current direct-path control must distinguish gate power from impact causality.
- C# UNIT tests executing production simulation are PRIMARY proof; integration supplemental. Chrome cat-063-electrical and related construction/presentation cases remain pending until ALL catalogue elements are implemented.

</frozen-after-approval>

## Necessary dependency closure and design

Current SupplyFeed compiles only a direct source or one immediately sourced contact. Current ContactClosed also excludes contact-trigger nodes. These are actual capability gaps, not justification to waive chains/cycles: replace the one-contact supply route with one declared directed graph compiled at Run. Each tick seeds enabled sources and propagates through committed closed declared contacts to a least fixed point, projecting consumer reachability into existing SupplyClosed inputs. No physical wire layout change. Admit fan-in only on declared SwitchedContact targets for sourced cycles; other inputs retain cardinality. Typed duplicate/wrong-domain rejection remains. Finite-work storage retains one direct source: indirect or converged-source routes reject explicitly before binding construction. Clock source provenance uses the same cycle-safe topology query and rejects multiple upstream sources; it retains its existing source-based behavior. Contact ownership derives from declared supply capability and admitted activation/timer state, not package identity.

## Tasks and code map

- [x] elements/ImpactSwitch/ImpactSwitchElementDeclaration.cs and switch.tscn: declare ISupplyElement, electrical domain, PowerIn/Supply sockets and visible art/locators; preserve ActivationOut, collider/material and button bindings.
- [x] engine/gpu/WorkshopPhysicsCompiler.cs, SupplyRoute.cs, PhysicsDeclarations.cs, WorkshopConnections.cs and existing device/network callers: replace one-intermediate route limitation with bounded generic compiled graph and coherent current-state projection; retain source accounting and lifecycle ownership.
- [x] Dead-code removal: delete superseded SupplyFeed/one-switch route and obsolete callers/fixtures/comments; no forwarding compatibility helper or dual routing path.
- [x] Latest-design alignment: package-owned data/art; strongly typed closed capabilities and IDs; shared f32 simulation authority, unchanged remaining9A44 common numeric lanes; no per-element update loop.
- [x] CuriousContraptions.tests: actual gate motion versus paired controls, chain/cycle/order/source-loss/restoration, both domains, boundaries/ticks, Reset/save/load and native connection-controller ambiguity/Cancel/Undo. Keep existing latch/counter/plate/Hold/electrical accounting regressions within changed closure.
- [x] Current CAT063/spec/TODO/sprint plus C-Switch terminal receipt: record truthful native outcome and future Motor/browser limits; no separate admin publication.
- [ ] Matching build once per relevant input/config, affected tests --no-build --no-restore, package/schema/deletion/isolation and worker build; exact retained-reviewer SnapshotApproval before protected publication and final remote/CI/deployment Pass.

## Verification and baseline

Reuse independently applicable baseline2074/2074 on exact9225 for unchanged C-Switch contact/ABI/presentation. New graph/current package needs actual native controls; do not claim baseline proves new electrical behavior. Planned affected test classes: electrical supply, state latch/counter/plate/Hold, connection controller, package ports/roundtrip, and new switch electrical tests. If project/resources change, include existing exact compiler-input guard against matching build logs. No routine browser or global performance campaign.

## Current state and evidence

Coherent candidate ready for retained independent review; no known scoped runtime failure. Production C# UNIT simulation tests are primary. Native UI controller checks use actual controller methods with a complete admitted read fixture; integration is supplemental. No Chrome qualification claimed.

Original tool receipts (no copied manifests):
- Baseline exact9225 CI2074/2074, independently verified cae641; C-Switch receipt carried.
- First build8a8d91: four diagnostics from missing Electrical abstraction project reference. Corrected declared dependency. Build7561d0: two BUNDLE013 locator mismatches; changed shorthand position to existing Transform3D serialization, preserving validator. Build5b696d: three test params-array target-type errors; explicit SupplyEdge fixes. Build57bbcc: one List.Length fixture typo; Count fix.
- First clean Debugaf1eac:0warnings7.62s. Native802de4:146/147 in3s; sole old contact fan-in rejection assertion corrected to positive while duplicate/wrong-domain/consumer negatives retained. New actual electrical simulation cases passed.
- Matching Debugd4be1a:0warnings4.86s. Focused9943ec:77/79 in1s; all simulation controls passed, failures were incomplete UI fixture body read and nine stale isolation rows. Fixture now obtains actual compiled admission dynamic bodies; exact stale Known/Generic rows removed, no new debt.
- Final Debug631af8:0warnings4.80s. Corrected UI/ratchet/reversed-identity cycles8501be:44/44 in1s. Remaining affected shared device/capacity/contact/Delay/multi-body classes1acf25:102/102 in3s, same outputs.
- Release workerad8da3:0warnings12.72s; actual .anvil/9-5b-compiler-inputs/worker.binlog. Exact compiler-input guard673f04:1/1 in232ms against those logs, matching Debug test output.
- Core new proof SwitchElectricalTests: physical gate movement versus miss/below/wire/off controls; simultaneous signal lamp; source-loss/restoration preserves latch; two-contact chain, seeded/unseeded cycle, reversed identities/edge order; cyclic source-off/on; exact save/replay/Reset; cycle-safe clock provenance and isolated finite-work direct positive/indirect/converged rejection.
- Native chooser exercises both output choices, mismatched target no-choice, Cancel/no command/no Undo, accepted signal/electrical links and one Undo each. Package port fixture asserts both new anchors. Existing threshold, body/material, Hold, plate/latch/counter, finite-work allocation and clock proof remains applicable by relevant inputs, subject to reviewer decision.

Deleted production SupplyFeed, SupplyRouteKind, SupplyRouteDeclaration and SupplyRoute single-contact records. ContactClosed has declared owner bindings instead of a node-kind whitelist. Old unbounded clock traversal deleted. Existing PoweredGate identity ownership stays with its named later conversion; no new named-arm debt. Anvil validates every manual write. Shared graph was necessary because the old one-hop representation could not meet exact source-seeded-cycle acceptance; no MNA/ABI/solver expansion.
