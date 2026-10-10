# Story 7.4 independent review

**Non-Pipe SnapshotApproval: Pass for the exact candidate below; local commit/tree verification pending. Whole Story 7.4: Incomplete.** The independently approved 637-file cutover is complete; all twelve held straight-Pipe files remain preserved pending owner disposition. Current consumer/policy repairs and diagnostic-only clock capture pass scoped review; fresh full Chrome passes 45/45. The original uninstrumented clock failure remains an unattributed historical reliability observation, not a claimed fix. Earlier checkpoint verdicts below are preserved chronologically and superseded only by their explicit affected re-reviews.

Reviewer: `/root/reviewer`. Implementation owner: `/root/implementation`. Coordinator: `/root`. This reviewer has not edited deliverables. Review-only evidence is not a recursive work item.

## Bounded preparation: 10 October 2026

Actual diff inspected: RequirementDiscovery replaces resource-only catalogue enumeration with CurrentCatalogue.Read; new CurrentCatalogue and tests inspected completely in raw command/result `4bc53e`. Exact three-file candidate:

| Path | SHA256 |
| --- | --- |
| tools/Coverage/CurrentCatalogue.cs | 486a761a803adb7dbd81ea82d6e718c8f5d0507b5e3ed973c5eb3e4074812625 |
| tools/Coverage/RequirementDiscovery.cs | 1baadb4aca1acda153f20def5f1d64489dadf414e0d27260b6f8ebe9f2616245 |
| tools/Coverage.Tests/CurrentCatalogueTests.cs | d4b58ef5707cb53333c6fa4302785825250b36ae481b572932ce22895480b64a |

Actual diff artifact: `.anvil/story-7-4-catalogue-preparation.patch`, owner-reported SHA256 80d533bfa7f185f112978e64da4081cda06a1e3a394b149a920e57e04098ce79. The reviewer binds actual source bytes above; this is not a new final-story immutable snapshot.

| Criterion / impact | Independent evidence / outcome |
| --- | --- |
| Preserve planned identity when resources disappear | 72 current CAT keys exactly equal the 72 HEAD resource Ids; discovery still produces 1,471 obligations. Pass. |
| Preserve mode and fixture boundaries | 104 modes = 72 Fixed plus 32 explicit; 302 exact content instance/kind/locked matches. CAT-I owner, CAT-V criterion and FIX identities remain distinct. Pass. |
| Invalid input / availability | Reviewed duplicate/missing CAT and mode controls, exact enum names and dimension compatibility, fixture drift and missing/wrong/duplicate admitted resources. All present resources validate; available resource hashes are retained separately. Pass for this reader. |
| Evidence currency | Selected CAT section plus declaration path/content determine source hash. Changed declarations and old resource provenance remain stale; no proof flags are copied. Seed remains unreviewed and cannot claim completion. Pass. |
| Runtime / broader tool closure | No runtime change; no new Chrome run justified for these three files. CapabilityInputs still expects old resource locations/register; owner-map and integrated audits remain incomplete, explicitly not covered by this Pass. |

## Original commands and results

- `4bc53e`: `git diff -- tools/Coverage/RequirementDiscovery.cs; cat tools/Coverage/CurrentCatalogue.cs; cat tools/Coverage.Tests/CurrentCatalogueTests.cs` — complete actual source/test review.
- `9ec97a`: `dotnet tools/Coverage.Tests/bin/Release/net10.0/Coverage.Tests.dll -noColor` — exit 0; 54 total, zero errors/failures/skipped/not-run; 0.185 s. Includes actual-root 72/104/302 assertions, malformed inputs, unavailable planned resources and stale-hash controls.
- `fc8d25`: Python invoked `dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll inventory .`, parsed full stdout and compared catalogue keys to `git ls-tree -r --name-only HEAD parts/catalog` plus each `git show HEAD:<resource>` Id. Result: exit 0, 1,471 records, 72 catalogue keys preserved exactly, 302 fixtures, no promoted Mapping/Implementation state, empty stderr. Same command SHA256-hashed all three source files; identities above.
- `e11334`: independent current-consumers/content comparison — 72 unique CAT keys, 302 fixture rows and unique instances, exact fixture match, no anchor/owner mismatch and no unknown kind. Initial broad mode line split included CAT-014 trailing delivery prose and reported 111; this was an inspection parser limitation, not a product defect.
- `456ed8`: corrected sentence-bounded mode extraction — 72 sentences, 104 modes, 72 Fixed, 32 explicit. Actual reader and regression use that boundary.
- Implementer original build/control receipts: warning-as-error build zero warnings `32448d/c1a28e`, 54 tests `ebacbb`; changed three-file Anvil scan reported zero warnings. Complete inventory retained at `.anvil/story-7-4-catalogue-inventory.json`, SHA256 71fbcd0787fbdf0f7892b39fa711c81a396ac5415a52a0f87798fb0c05f4f4bc, with disclosed preview-only validation for its large body.

## Remaining acceptance boundaries

Current catalogue resource presence is not admission, and this reader only validates resource identity/content artifacts: complete retained scene/script reachability is still due before deletion. Missing historical owner IDs must fail explicitly until supported by current semantic authority; no aliases or automatic evidence reseed. Current requirements remain source acceptance, not merely a count. Final audit integration must preserve changed referenced acceptance and implementation evidence as stale when applicable. Pipe answer and full deletion-entry review remain pending.

## Owner membership preparation review — findings open

Actual five-file diff and both new source files inspected in raw `313482`. The narrow owner-membership correction is **Fail pending fixes**, without changing the catalogue-reader Pass or overall Incomplete story status.

- **O1:** heading-family filtering silently excludes real current declared owners `ANIM-1` (engine.md:198) and `MOBILE-02/03/05/06` (refinements.md). Independent heading-family enumeration `f400c8` establishes the missing names; generic `ELEMENT-n` is a placeholder and must not be admitted as a real owner. Required correction: preserve concrete declared families and add actual-authority/positive controls.
- **O2:** fixture rows use `owners.Add` without checking a separate fixture identity set. Duplicate FIX rows, including a conflicting CAT-V with another declared CAT-I, silently coalesce. Required correction: reject duplicate/conflicting fixture identities; repeated legitimate owner headings may still coalesce. Add negative controls for both duplicate and conflicting FIX rows.

Both findings returned to the same implementation owner and coordinator. No deliverables edited; no deletion approved.

## Owner membership affected re-review — scoped Pass

O1 and O2 are resolved by the same owner. This is a Pass for the current-membership boundary only; source-owner relations, policy and integrated audit closure remain Incomplete. The original red source defects remain above. The owner fixed the code before the red-execution request, so no pre-fix failing executable receipt exists; this limitation is explicit.

Independent actual corrected source/diff inspection `83c537` confirms concrete ANIM/MOBILE heading families and a separate FIX identity uniqueness check. Full independent heading census `51c5fc` counted 543 concrete declarations across seven inputs: engine 59, GPU physics 29, refinements 197, decisions 8, scope corrections 28, current consumers 72, campaign 150. The only excluded hyphenated placeholder is ELEMENT-n. This establishes current input coverage, not acceptance of arbitrary future families.

Independent `8c7b95` executed `dotnet tools/Ownership/bin/Release/net10.0/Ownership.dll --oracles .` via Python capture/parsing: exit 0, empty stderr, Positive true, 22 general negatives, ActualHeadingCount 543, all 12 current-boundary negatives rejected, absent S010-D rejected, repeated owner declarations coalesced and assignments unchanged. The duplicate/conflicting FIX regressions exercise the repaired path. Independent `c57fab` executed `--audit /nonexistent-owner-authority-control missing.json`: exit 1, empty stdout, ordinary missing engine.md path message, no stack trace or capture attempt.

Exact five-file candidate hashes independently measured in `8c7b95`:

| Path under tools/Ownership | SHA256 |
| --- | --- |
| CurrentOwnerMembership.cs | 6bdec47a3b117d0d5e30c3d16755041be66de945a5d2bf50fdb033834e0f85dc |
| CurrentOwnerMembershipOracles.cs | e2b7337140fb124fc0e02657b5d75838329547ee0b2046819b96a4388dd1be56 |
| OwnershipOracles.cs | b99aea5edf002f120087f51f80f64e416fa5678c3ad980c39b450d896cdd3814 |
| Program.cs | fb43f6a7b84c026c999346ea42ae4014144d39caebd9b9da893939bbaa3197f4 |
| README.md | 8347a6b6cdee4c7325ee1ea6f733052d7d5e1ce33ccb416a67a8c68205d2fd12 |

Owner's corrected full raw oracle output/diff: `.anvil/story-7-4-owner-membership-corrected-oracles.json` and `.anvil/story-7-4-owner-membership-corrected.patch`; warning-free build `08274d`, zero-warning Anvil/diff check `622c02`. Original actual root audit `14fc09` reached capture then rejected stale historical contract with “Source membership differs”; this was before the final fixture-only strictness addition and is retained as that original result, not a new final integrated audit claim. No contract, assignment, source-owner map or runtime behavior changed. No deletion approved.

## Bounded Coverage integration design approval

Actual appended plan read in `7c4a7a`; model/callers inspected in `da6883` and `6c3324`. Approved reversible implementation of current relations, explicit unresolved diagnostics, planned/admitted resource separation and referenced acceptance-body hashing. This is not implementation acceptance or deletion approval. A single source-clause candidate establishes a scope relation, not automatically a proof-role grant; SourceOwners still requires explicit current role authority. The existing single ProofOwner checks must not weaken into “any allowed owner”. All 1,471 obligations and original proof shards remain preserved; no global reseed or fabricated owners.

Scope applicability: current P0-030/P0-031 engine disposition requires consumer/cleanup closure. docs/coverage/README.md:17,28–30 explicitly distinguish structural currency from runtime qualification and retain unrelated stale source/fixture failures. Repairing deletion-reachable current tool inputs does not require freshly qualifying all 799 future tasks. The final independent shipped-consumer/deletion matrix remains required. Full future assignment failures must remain visible, not silently converted into Pass or expanded into an unrelated prerequisite platform.

## Coverage integration actual review — findings open

Actual sources/callers inspected in `d43205`, `30b580`, `c9e461`: CurrentOwnerRelations, CapabilityInputs, changed CurrentCatalogue acceptance hashing, source-linked WorkId extraction, removed legacy register/default-mode test and replacement controls. Independent `dotnet tools/Coverage.Tests/bin/Release/net10.0/Coverage.Tests.dll -noColor` passes 58/58, zero failures/skips, 0.587 s (`2d5142`). These tests do not close the following findings. Integration is **Fail pending fixes**, while earlier bounded reader Passes remain their exact scoped identities.

- **C1:** ReadResourceClosure chooses a lone ext_resource declaration but ignores actual Scene/root script bindings. Its positive fixture has no [resource]/[node] assignment at all and still succeeds. Required forward fix: resolve actual assigned ExtResource ID and reject missing, duplicate, dangling or wrong bindings; retain path containment and changed-byte controls.
- **C2:** Prepare unconditionally emits UnresolvedImplementationRole for every capability and supplies an empty eligibility set. This is an unfinished policy placeholder, not evidence every current role was inspected and found absent. Required bounded correction: consume explicit reviewed typed role relations, validate exact required roles/membership, report actual missing/conflicting relations and test a satisfiable strict positive path. Existing unmapped future roles may remain unresolved; no demand to freshly reconcile all 799 tasks or reseed historical evidence.

Both findings sent to the retained implementation owner and coordinator. No source-owner cardinality shortcut, new global audit qualification, or deletion approval is permitted by this review.

## Coverage integration affected re-review — scoped Pass

C1 and C2 are resolved. The bounded current-input integration has **Pass** for the exact 13-path patch `.anvil/story-7-4-coverage-integration.patch`, SHA256 **fbe941bfdb3a9c5a4ec80044b2db8407ff0f0947821402ba162028d503bc9c24**. Independent raw `87225c` enumerates all 13 current source hashes alongside that exact patch identity; no additional manifest is created for this intermediate boundary. CurrentOwnerMembership remains its previously approved 6bdec47a3b117d0d5e30c3d16755041be66de945a5d2bf50fdb033834e0f85dc source-linked input.

| Affected criterion | Re-review evidence / outcome |
| --- | --- |
| Actual resource bindings | `9f2f8f` inspects section-scoped Scene/root script assignments, unique ExtResource ID resolution, type/path validation. Missing/duplicate/unknown/unsupported assignment controls now reject. C1 resolved. |
| Satisfiable reviewed-role policy | CurrentImplementationRoles validates explicit matching typed roles and current membership; malformed/unknown/conflicting input cannot grant eligibility. A complete synthetic strict CapabilityAudit passes without claiming runtime qualification. Empty actual map means no reviewed relation supplied, not proof of absent authority. C2 resolved. |
| Source/role boundaries and preservation | Source-clause cardinality alone does not grant proof; ambiguous/missing roles remain named. Catalogue-I grouped criteria and FIX identities stay distinct. Planned absent resources remain inventoried as FutureDeclaration with capabilities. Exact declared current acceptance and linked acceptance-body bytes invalidate catalogue hashes. |
| Tests | Independent `06efe7`: 67 total, zero failed/errors/skipped/not-run, 0.589 s; includes eight C1 controls, C2 positive/negative path, actual deletion simulation and acceptance drift. Owner original red log retains eight failing C1 controls before fix, independently sampled in `e5356d`; full `.anvil/story-7-4-coverage-integration-binding-red.log` is retained. |
| Actual CLI | Independent `87225c` parses complete audit stdout/stderr: exit 1, no stdout, 1,137 diagnostic lines (605 MissingScope, 16 AmbiguousScope, 280 UnresolvedProofRole, 164 UnknownOwner, 71 NoReviewedImplementationRole plus heading), no missing legacy-file failure. Inventory exits 0 with 1,471 records: 799 Task, 216 Element, 37 Thermal, 22 Radiation, 18 Gap, 72 Catalogue, 302 Fixture, 5 Research; all remain unreviewed. |
| Evidence preservation | Independent git diff shows only docs/coverage/README.md changed in docs/coverage; no manifest/shard edits. Original source/element audit nonzero outcomes and complete owner raw checks retained in `.anvil/story-7-4-coverage-integration-checks.json`; no currency/qualification promotion. |

Commands independently executed: `dotnet tools/Coverage.Tests/bin/Release/net10.0/Coverage.Tests.dll -noColor`; `dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll audit-capabilities . docs/coverage/engine-capabilities.json`; `dotnet tools/Coverage/bin/Release/net10.0/Coverage.dll inventory .`. Actual complete diff/source inspection spans `d43205`, `30b580`, `c9e461`, `9f2f8f`, `06efe7` and docs/caller check `e5356d` (combined last output truncated; source corrections were already fully inspected in earlier outputs).

This Pass covers usable current input boundaries, preservation and rejection behavior. It does not mark current implementation-role policy reviewed, global capability manifests current, runtime qualified or Story 7.4 complete. No runtime rebuild or Chrome repeat is necessary for these tool-only changes. Final shipped code/resource/tool consumer matrix, remaining policy/deletion closure, owner Pipe disposition and exact deletion-entry approval remain outstanding.

## Current-policy and non-Pipe deletion entry approval

**Current-policy correction: scoped Pass. Non-Pipe deletion entry: approved for exactly 637 files only.** This is an intermediate action within the authorized purge, not Story 7.4 completion or publication approval. The twelve straight-Pipe files remain held pending owner disposition. Every removal still requires Anvil and immediate exact membership/byte drift equality; active document/export corrections are part of the same cutover. No blanket directory removal or additional file is authorized.

Exact entry inventory `.anvil/story-7-4-non-pipe-entry-inventory.json` SHA256 **680447c67b77ce0357c67388c7c0e006512bb858b1ee313e2d31034f3c56f1c6** contains 649 rows: only rows with disposition `delete-candidate` are approved (637 files, 1,411,121 bytes). Independent `b9d3f0` rehashed every row, found no drift/symlinks, and verified no candidate overlap with all 16 captured Compile contexts. Independent ledger enumeration `4dcbe5` found exactly 649 unique existing paths. Retained straight Pipe comprises WorkshopPipe, AnnularProfile, PipeDimensionsResource, PipePart, PipeArt and their five .uid files, plus pipe.tres/pipe.tscn. CAT-049/050 bend files remain separately harvested deletion candidates; straight Pipe does not consume them.

Independent retained-resource enumeration `cd07a0` starts from git tracked .tres/.tscn files minus the candidate, not the owner's edge list: 24 files, 44 res:// edges, all targets exist, zero deleted targets, exact equality to recorded edges. Balloon/tennis bind retained ball scene/script; Pipe resource/declarations remain. Independent `b9d3f0` rereads the 276 retained source/config paths and finds only the deliberate planned-absence test reference to electrical_nand.tres. Compiled membership plus current fixed PartRegistry admission and typed source review establish current runnable closure; unsupported Pipe remains excluded and unqualified. Four active document references require cutover updates explicitly listed in the spec matrix, inspected in `02f4d5`.

Current-policy patch `.anvil/story-7-4-current-policy.patch` SHA256 **4ebf3b0223b254afaf69ab811d21f4c128691c69cab24fcf9d0cc2eb5c592dd8** and its five current source hashes are independently bound in `02f4d5`. Actual policy and fourteen removed legacy declaration claims inspected in `03676d`; original current state/callers inspected in `3917b8`. Capability/dependency requirements and historical manifests remain preserved. Ownership examples now bind current AnimationKernel arrays, host operation identity and Godot presenter object lists; this is bounded guard coverage, not all-member assignment qualification.

Independent commands `dotnet tools/Ownership/bin/Release/net10.0/Ownership.dll --oracles .` and `dotnet tools/Coverage.Tests/bin/Release/net10.0/Coverage.Tests.dll -noColor`, executed in `c1d63d`/completed `7a4124`, pass: Ownership exit 0, 22 general negative controls, zero actual compiler binding errors, all five real guards resolve and reject wrong ownership. Actual _free callers are precisely ctor, RegisterSlot, Remove and FillFree. Coverage 68/68, zero errors/failures/skips, 0.597 s. Raw owner full oracle artifact retains complete semantic/mutable reference controls; independent corrected source review confirms no guard weakened into current-policy success for the whole inventory.

Post-cutover requirements remain: prove exact removed/retained membership, declaration/link/mode/fixture preservation, current tool operation and explicit stale/unresolved limits, build/unit/resource export and required full serial Chrome acceptance. Final 7.4 status stays Incomplete while Pipe and post-cutover acceptance remain open. No global future capability qualification is inferred or newly demanded.

## Post-cutover and first full Chrome run — runtime Fail

Independent post-cutover `0dae34` confirms exactly the 637 approved git deletions, no remaining candidate, and all twelve held Pipe hashes unchanged. Current-consumers changes only 59 legacy-source clauses: all declaration links resolve, all other text including mode/acceptance controls is byte-identical after removing those clauses. Export changes only remove obsolete physics/Geometry exclusions; admitted resource roots remain identical. Five xUnit size assertions became equivalent Single/Empty assertions without suppressions, reviewed in `a7fa7d`. Owner corrected remaining CAT-002 roadmap wording before freeze. Independently checked all 348 specs and 3,020 baseline ranges across 574 source paths: no invalid range (`9ecd85`).

Frozen diagnostic manifest `.anvil/story-7-4-diagnostic-identity.json` SHA256 **71f1355ae26dc61aaf96a5ddfa9d6bf1e21eef7541a01ecedfeb53cddd384cb2** binds 221 bundle files and 369 relevant inputs. Independent `8d6bb9` verified all bytes and exact bundle membership before the run; five HTTP entries matched. Additional `0f6bbf` matched main WASM plus Simulation/Animation worker bytes over HTTP, totaling eight served entry checks. Direct Playwright connector tabs succeeded. Owner later identified omitted UI input paths and preserved the original manifest: supplement `.anvil/story-7-4-runtime-input-supplement.json` SHA256 **abc59b208791a2c9a3f55014086693514e0ab29d4b56609a65569f710bc6940b** binds ten omitted UI Compile inputs. Independent `c86a1d` confirms every supplemental hash, HEAD byte equality and no missing local authored runtime Compile input in the combined set. No bundle/source mutation or repeat was needed for this supplement.

Reviewer owned the sole full serial actual-Chrome run on http://127.0.0.1:8060/:

`node --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts`

Complete stdout/stderr captured after Anvil preview allow to `.anvil/reviewer-story-7-4-chrome.log`: **11,483 bytes**, SHA256 **cdac3e1599f73961cfc323afb736f7a0fe5dd2547868efe45793645f196bf244**. Preview validation was partial because output did not yet exist; original full output is retained. Raw execution `8d6bb9`, session 72284, terminal `68d8db`/`934d62`: exit 1, **45 tests / 15 suites, 42 pass, 3 fail, zero cancelled/skipped, 372,326.124667 ms**. Independent postrun `1cf164` finds zero bundle or relevant-input drift.

All three failures are ANIM-1b:

1. Switch test fails at driver.run (workshop-driver.ts:659, test:65): “run: the pose ring is unreadable in Build mode”.
2. Delay behavior reaches final error assertion (:130), which reports CCGPU_TRANSPORT_FAILURE 3 / ManagedError: “Clock mapping recovery exhausted its bounded episode.”
3. Bumper behavior reaches final error assertion (:174), reporting the same shared errors-array fault.

This is a real clock/transport diagnostic, not encoded-payload classifier noise. The shared error array can propagate the original fault into later case assertions; that does not erase the initial runtime failure. Cause and purge attribution are not established. Complete failures were sent to the retained implementation owner and coordinator; browser handed over for bounded diagnosis with frozen bundle held. No rerun, error suppression or acceptance weakening approved. Runtime criterion is **Fail pending diagnosis and affected re-review**; whole Story 7.4 remains Incomplete as Pipe/postcutover acceptance is also open. The other 42 exact-candidate results remain evidence subject to relevance assessment after any fix.


## Clock diagnostic review — retained reviewer /root/reviewer

Independent source reads `50568b`, `3c19a6`, `912627` and `b12c31` inspected the actual mapping, startup callers, current contract and native discriminator. The 5 s runtime/clock deadline starts in workshop-client.js:127 only after both managed runtimes announce awaitingBootstrap. BrowserWorkshopClient waits for Browser IsQualified and Animation qualification, initializes, then qualified() clears the timer. This is an actual startup bound, not a new proposed deadline.

Owner native evidence `.anvil/story-7-4-clock-native-corrected.log` (SHA256 `2b7e367659e5962344f0875c42c464999ca9aad3cc1d8815c42d23d98fb25ed1`) source-links the unchanged mapper. Its explicit assertions demonstrate initial eighth 1.295 ms RTT probe entering usable Recovering (current full width 1.895 ms; refresh-horizon width 2.145 ms), a narrower follow-up restoring Ready without epoch change, established recovery remaining usable, and fault exactly on the eighth unsuccessful recovery probe. Independent review of the harness and existing recovery-positive test supports these finite outcomes, not original Chrome causation. Initial scratch compile correction remains separate failed evidence.

The current contract explicitly tests the refresh horizon after initial qualification and permits currently usable Recovering mappings. A caller-only Ready gate would not prevent mapper recovery exhaustion; changing initial admission to require the future horizon would tighten current semantics, but neither is established as the original fault's correction. No runtime change is approved on this evidence. Focused 3/3 and adjacent observed 6/6 passing runs are non-reproduction evidence, not erasure of the original failure. The coordinator authorized one finite observational replay with reconstructed read-only citation/hash workload; original workload command ordering and overlap timing are not recoverable, so exact historical-load reproduction must not be claimed. That result remains pending here. No additional undirected replay is justified.


The single reconstructed finite-load replay is now inspected: `.anvil/story-7-4-clock-finite-load.log`, 3,079,080 bytes, SHA256 `1f622a7cc8443aeea2a3b80980dfbed6ce69c5c6237b2f5b79c43801795677f5`. Independent complete-line JSON parsing (`7d5314`, `f4805d`, `be116d`) confirms 3/3 passing cases, exit 0, five readiness captures with eight records each and zero missed/overwritten records, 136 console records and zero errors. Workload begins at 1791598880603, first readiness returns at 1791598884056, workload ends at 1791598885912: actual startup overlap is established. Its 574 baseline blobs, 350 Markdown files and 2,945 deduplicated ranges are a reconstructed equivalent, not the original 348/3,020 enumeration. All reconstructed ranges and hashes pass. This does not attribute the original failure; no further undirected repetition is approved.

Independent fault-path inspection (`2a76d7`, `e1ea72`) identifies a useful remaining diagnostic gap: Browser ReceiveClockReply records only after Clock.Receive returns, losing a throwing final reply; Service may throw from Observe before a diagnostic record. Animation Reply has no equivalent failure observation at this boundary. A bounded Playtest-only fault snapshot attached to existing clock diagnostics could preserve role/identity, actual input, pre-fault recovery count/time/pending state and retained history before teardown without changing the exception, bounds or termination. This is a proposed diagnostic improvement, not a demonstrated behavioral fix or approval of new code. Original runtime failure remains unresolved and the current slice has no terminal Pass.


## Bounded clock-fault diagnostic implementation review

Retained owner `/root/implementation` added only the shared mapper's PLAYTEST fault emitter, Animation.Worker PLAYTEST definition and affected WorkshopWireTests. Exact final patch `.anvil/story-7-4-clock-fault.patch` SHA256 `66e4f02f7e5997c7799ca602243fe4a474ab2904f4fce42f70042971f38eea2d`. Actual diff inspected in `2616cb`, stronger assertions in `a66618`. The emitter runs at FailRecovery before teardown, captures exact triggering reply or service time, typed role/cause, exact decimal wide identities and time, recovery state/count/start/pending, and at most eight slots. Slots are accurately labelled before teardown and can already include the accepted final reply. Formatting/output exceptions cannot replace the unchanged original exception or Faulted/pending-clear outcome. No bounds, readiness criteria, state transitions, retained buffers or polling were changed. Production compiles no emitter.

Animation.Worker source closure (`6c06ca`, `df78c2`) excludes Browser client; its only other PLAYTEST impact is the existing unused CaptureMode constant. Worker uses standard dotnet.create without a console override (`3ba398`); actual fault-console visibility awaits recurrence, not an invented injected-fault acceptance gate. Existing xunit.runner.json disables parallel test collections and is copied to output (`fb3b5d`), resolving process-global Console.SetError isolation without another change. Tests now assert exact recovery and triggering reply/slot values, both roles, elapsed/service/probe-limit causes, one emission, writer failure preservation and production absence. Owner retains six original missing-output red failures, diagnostic eight green controls and production eight green controls in `.anvil/story-7-4-clock-fault-{red,green,production,exact}.log`.

Independent command `dotnet test CuriousContraptions.tests/CuriousContraptions.tests.csproj -c Release -p:PlaytestDiagnostics=true -warnaserror --filter FullyQualifiedName~WorkshopWireTests --nologo` (`b1c28d`, session 47568, completion `cd20b2`) exited 0: 91 passed, 0 failed/skipped, 877 ms test duration. This includes existing usable Recovering and wire/clock boundaries. Targeted diagnostic implementation is approved for build/freeze and observed runtime checks. This approval is diagnostic-only; it does not resolve or attribute the original Chrome failure and does not grant whole-slice Pass.


## Exact instrumented candidate — full independent Chrome result

Frozen diagnostic manifest `.anvil/story-7-4-clock-fault-diagnostic-identity.json` SHA256 `ec5a739d82ebac9b93140f78b7ff03ea39630dd0e62d773f9aa8e8e5c2d90154` binds 239 bundle files and 379 relevant inputs. Independent `d6f41f` verified every byte and exact bundle membership, no drift; `0f13b4` matched six served entry/payload hashes and directly checked Playwright connector availability. The additional 18 bundle files are the diagnostic JSON dependency closure. Owner's actual browser console-route evidence `.anvil/story-7-4-clock-fault-console-route.json` SHA256 `0a8d29d2326f6ac3404641da029c998f0a43239c3e304ac1ec8a1dfa502a35c2` was independently read: Browser and Animation existing Module.printErr appear as console errors, print as logs; this is output-transport proof only, not a mapper-fault injection or recurrence.

After Anvil approval, an observation-only Node import `.anvil/reviewer-story-7-4-fault-console.mts` attached a full JSON console-error listener before waitForReady. It does not change suites, driver behavior, runtime state or assertions and avoids Node assertion rendering truncating a future fault record. One exclusive full run used `node --import ./.anvil/reviewer-story-7-4-fault-console.mts --test --test-concurrency=1 --test-timeout=150000 tools/e2e/*.test.ts`. No builds or citation/hash workloads ran concurrently. Session 36115 (`94f256` through completion `392b9d`) exited 0: **45 passed across 15 suites, 0 failed/skipped/cancelled**, 373,780.37175 ms test duration. Complete raw stdout/stderr `.anvil/reviewer-story-7-4-instrumented-chrome.log` is 9,335 bytes, SHA256 `27076a3d86969dfb1aadc911fc7390f1180ea2ba3abb84b6c49253e345c2ffc3`. Independent `bc47dd` finds zero raw console errors and zero post-run drift across all 239/379 identities.

The current candidate's full existing Chrome case criterion now passes, including all actual UI behavior, controls and lifecycle assertions in those 45 cases. This is fresh complete coverage, not reuse of the earlier 42 successes. The original uninstrumented clock failure remains unattributed: there is no evidence that purge introduced it, but unchanged clock sources alone do not prove environmental causation or establish that the original stop was intended under its uncaptured inputs. The diagnostic addition is not claimed to fix clock behavior. The original failing raw evidence and unresolved reliability observation remain retained; whole Story 7.4 is still Incomplete, including held Pipe scope and final candidate/publication review. No further replay is justified by this passing result alone.


## Final non-Pipe SnapshotApproval — exact candidate

**SnapshotApproval: Pass for local commit only**, not whole Story 7.4 completion or global qualification. Candidate `_bmad-output/implementation-artifacts/story-7-4-non-pipe-snapshot.json` SHA256 `8ab8cd32413251548d22d89dfd5eeb00ed6d8a64ed4042d6948edc7954ae240c`, based on `ba32b115aec4f3a204581492d3daad158caa2159`, binds 33 current deliverables and 637 exact deleted tracked Git blobs. Complete actual scoped diff `.anvil/story-7-4-final-non-pipe.patch` SHA256 `042fbeb5ddc039a656912800e36da8e0c52f6af5f9a9113ca17358a8abc96b0b`. Reviewer `/root/reviewer` and implementer `/root/implementation` remain distinct; original source, edge-case, adversarial findings/triage and verification-gap work above is retained, not replaced by an identity-only review.

Independent final commands/results: `81cdfc` inspected the snapshot schema/scope; `1db141` verified all hashes, exact deletion membership and original Git blobs, no remaining deleted path, equality to approved entry, no unexplained tracked edits, all twelve Pipe bytes equal source HEAD, all 85 proof JSON bytes equal source HEAD, and all 348 element declarations preserved except three explicitly excluded pre-existing owner-decision edits. `414150` verified complete diff SHA, requirements/named index/puzzles byte equality and authority/proof reference membership. Its initial resource scan limited to parts/ui returned 23 resources/43 links; corrected complete tracked resource enumeration `473955` includes scenes/workshop.tscn and verifies all 24 resources/44 existing links, none targeting a removed path. `c1716b` inspected final ledger correction (637 removed, twelve held, WGSL actual status and verified 7.3 receipt) and clean diff check; `678d8f`/`5e76c9` inspected current guidance/roadmap changes with no acceptance weakening.

| Final criterion / impact | Result and applicability |
| --- | --- |
| Exact deletion and held scope | Pass: 637 exact approved blobs removed; twelve straight-Pipe files unchanged. No additional deletion or Pipe decision implied. |
| Source knowledge and acceptance | Pass: 348 declarations retained; requirements, named index and puzzle content unchanged; prior independently checked baseline citations remain applicable. Three Battery/Bumper owner-decision documents are excluded and preserved. |
| Direct/transitive consumers | Pass: established sixteen Compile contexts and retained resource/string consumer matrix remain applicable. Final 24-resource/44-link check confirms cutover closure. Necessary current tool consumers were repaired and reviewed, including O1/O2 and C1/C2 controls. |
| Tool/evidence semantics | Pass scoped correctness: 1,471 obligations, 72 catalogues, 104 modes and 302 fixtures preserved; 85 proof JSON files unchanged. Stale/unresolved global source/proof roles remain explicit nonzero audits, not promoted or inferred complete. |
| Runtime and ordinary lifecycle | Pass for exact current candidate: warning-free builds/publishes, 651 production units, independent 91 wire controls, retained unaffected 68 Coverage/85 Node checks and fresh independent full 45-case Chrome result above. Relevant artifact/source identities remain exact. No fresh runtime claim derives from status-only final docs. |
| Unresolved original clock observation | Retained, nonblocking for this bounded approval: original failed run is real and unexplained; no specific violated clock invariant or introduced regression was established. Purged sources have no compiled/shipped consumer path into clock behavior, original clock logic was unchanged, and finite native controls match the explicit bounded-stop contract. These facts justify scoped impact exclusion of an unproven causal claim, not a finding that environmental load caused the fault or that diagnostics fixed it. Current complete test coverage is separately established. Future recurrence must use retained fault context; no error suppression or weakened bounds. |
| Scope/publication | Whole Story 7.4 and P0-030/P0-031 remain Incomplete for held Pipe. No push/deployment/global capability or full Battery/Bumper qualification is approved. |

Exactly **672 local commit paths** are approved: snapshot's 33 candidate paths + 637 deletion paths + this review record + the immutable snapshot. Eight explicitly hashed unrelated dirty files remain excluded: prior 7.0/7.1/7.2/7.3 review receipts, owner-question ledger, CAT-005, CAT-015 and EL-196. Local archives and unrelated untracked scratch/diagnostic files remain untouched. The snapshot's 69 retained tool inputs, 15 current authorities and 27 proof references matched at approval.

Only publication-dependent checks remain for this non-Pipe checkpoint: independently verify the created local commit's exact 672-path membership, candidate/snapshot byte identities and deleted blobs/tree absence; verify twelve Pipe files and excluded dirty files remain unchanged and the index contains no unintended staged paths. Review-only receipt may be appended afterward without another recursive commit gate. No pre-publication behavior or preservation criterion is deferred by this approval.






