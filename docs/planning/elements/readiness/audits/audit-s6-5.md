# Planning audit: `spec-s6-table-driven-capability-plumbing.md` (S6, first slice), fifth pass

Audited 24 November 2026, planning phase, read-only. Checkout `spec-drafts`, code at published main `25df3cd5`; nothing built or run. Contract 3,546 words. Binding decisions: `docs/planning/elements/readiness/owner-decisions-2026-11-22.md` (owner 22, 24h; coordinator 37, 38). This pass checks that the fourth audit's findings (`.tools/audit/audit-s6-4.md`: F1 to F3, N1 to N3) are applied and that nothing new blocks.

## Verdict: Scoped Pass (ready-for-dev)

No blocking finding. The fourth audit's F1, F2, F3 and N3 are applied in the text; N1 and N2 were referrals to S6b and S6c and belong to those audits.

## Fourth-audit findings: resolution

| Finding | Status | Evidence |
|---|---|---|
| F1 (M3 could not reach `Query`) | Applied | Intent row M3 `:47`, Always `:33`, probe `:94`, decision `:101`, note 7 `:142`, table row `:169` all use "a delivered part whose declared presentation binds that driver", no re-targeted track. Symbols exist: `PartRegistry.Definitions` (`engine/PartRegistry.cs:26`), `PartDefinition.Presentation` (`engine/PartDefinition.cs:49`) with `ElementPresentation.Bindings` (`CuriousContraptions.Abstractions/ElementPresentation.cs`), `PresentationDriverRules.SupportsMapping` (`:174`), `WorkshopInput.Generic`, `CompiledSceneFixture.Compile` (`CompiledSceneFixture.cs:8`), `WorkshopIntegrationHarness.Observe` (`:418`); `registry.Load()` is used by `ClockTests.cs:16-17`, so the real bundle loads in the test host |
| F2 (division of chains, Slices list) | Applied | `:59`, task 1 `:118`, task 5 `:122`; `delivery-batches.md` already holds the Closure, Inventory and Slices table (`:315-326`) and its Acceptance already states 24h |
| F3 (frontmatter baseline) | Applied | `:6` reads "captured at dispatch (the main that contains the planning work)", and `:53` says `25df3cd5` is the drafting revision only |
| N3 (order statement) | Applied | `:111` "Runs after S5b and before S6b" |

## Independent verification

- **19 continuous drivers.** `PresentationDriver` has 26 values (`ElementPresentation.cs:18-39`): `Event` plus 25 committed. The six discrete ones (`CommittedActuatorState`, `CommittedContactLoadPhase`, `CommittedTimerPhase`, `CommittedTransmissionPhase`, `CommittedPrismaticPhase`, `CommittedFiniteWorkState`) are exactly those `SupportsMapping(..., StateValues)` admits and `Identity` refuses (`:176-179`). So the Identity-admitting values other than `Event` number 19, and they are the 19 case labels of `Query` (`engine/gpu/WorkshopDeclaredPresentation.cs:373-414`; counted by reading, plus the gas-radius `if`). The M3 case set is therefore exactly the set `Query` must route.
- **Query's caller.** `Query` has one caller, `StageRead` (`:298`), which skips `Event` and sends `StateValues` to `MapDiscreteState`; construction stages the read through `ApplyRead(endpoint.Read)` (`:154`), so `new WorkshopDeclaredPresentation(...)` reaches every continuous track's `Query` in M3. M2's refusal text "Declared presentation query is absent" is asserted by the existing test at `ComposableAnimationMappingTests.cs:605-621`.
- **Probe tests.** All 15 named tests exist at the cited lines (checked by `rg`): `DeclaredPresentationTests.cs:237, :415, :490`, `ComposableSignalNodesTests.cs:325`, `HoldTimerTests.cs:470`, `ElectricalNandTests.cs:345`, `BothGateTests.cs:259`, `ClutchGeneralityTests.cs:197-215, :511`, `OwnedFiniteWorkTransactionTests.cs:403`, `FanIntegrationTests.cs:541, :586`, `BalloonTests.cs:198`, `ClutchTests.cs:1448`, `WoundSpringLifecycleTests.cs:56`; and the 12 classes in the filter exist (`ClutchTests` is partial across two files).
- **Other citations.** Part C cites `WorkshopDeclaredPresentation.cs:89-155`, `:402-405`; `batch-o.md:46` cites `:492-502` (both found), so the "re-read by whichever publishes second" instruction is correct. Engine files are unchanged between `8d1d68aa` and `25df3cd5`.
- **Removed protections.** The `switch` had a runtime `default` throw and no compiler exhaustiveness check; the table keeps the throw as the lookup's miss branch (`:30`, AC3) and adds the every-value test M3. Both stated.
- **Forward-only, element-free, f32, typing.** Old `switch` and the inline gas-radius case are deleted with no second path (`:37`); table keyed by the generic enum only; `Func<Track, WorkshopRead, float>`; static lambdas, no reflection or runtime enum enumeration in production; the test enumerates the enum, which is test code. Engine tier, one structural slice; 3,546 words; delta +19, registration check correctly not required (existing test file; the one compile rule at `CuriousContraptions.tests.csproj:77`); no `tools/anvil` change so the harness is not required.
- **Concurrency.** Shares `deferred-work.md`, `TODO.md` and `delivery-batches.md` with S6b to S6f; no pre-programme slice edits `WorkshopDeclaredPresentation.cs` or `ComposableAnimationMappingTests.cs`.

## Non-blocking findings

- **N1. M3's control is by reading.** `:169` says a driver without a row "throws ... and fails its case"; no executed control can reach the miss branch (the undefined value is refused earlier, `:95`). That is the honest limit and AC3 states it. The executed control is that M3 fails with `Assert.NotNull` for a driver no part declares. Add one sentence to the table row: "a new continuous driver is delivered with a declaring package, or M3's NotNull fails the case; the failure message names the driver".
- **N2. Filtered-run baseline.** `:154` expects "the baseline's total + 19" for the filtered run, whose own baseline total is not stated. Say "the total of the same filter run before the change (task 1), plus 19".
- **N3. History text.** `:55-57` narrates the fourth audit and says "re-audit required"; the coordinator replaces it with the recorded Scoped Pass after this audit. It sits outside the intent contract.
- **N4. Document consistency (not this slice).** `docs/planning/elements/readiness/engine-stories-index.md:5` and owner decision 22 list S3, S5 and S6 only; decision 24h adds S5b and S6b to S6f. Documents only.

## Decisions that need the owner

None.

## Evidence limits

- `Query`'s complexity 22 and the measurement counts at `:70` are the contract's own, taken at `8d1d68aa` and re-measured at task 1; I counted case labels by reading, not by the analyzer.
- Whether each declaring part compiles alone through `CompiledSceneFixture.Compile` was not executed; the contract's fallback (the driver's cited M1 scene) covers it.
- Planned tests are not executed proof.
