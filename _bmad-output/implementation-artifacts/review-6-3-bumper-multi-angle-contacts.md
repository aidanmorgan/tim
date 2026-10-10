# Story 6.3 independent review

Reviewer: `/root/reviewer`; implementation owner: `/root/bumper_implementation`; coordinator: `/root`. Reviewer does not edit deliverables. **SnapshotApproval Pass for the exact candidate below; terminal delivery Incomplete pending publication and deployed verification.**

Entry contract spec-6-3-bumper-multi-angle-contacts.md SHA256773cd7e9ddae6c8bc3b4d0bed9b136a7b964439f21d0e609776c775d27f7ef11 at baseline60ddbbe4f010314a64716bf8748845146b312bdc. Independent full draft, epic6context and roadmap-delta read09b014; source acceptance epics388, CAT015/current-cat-015, todo157/159/160/163, exact source fixture extraction63e987. Story6.2 terminal publication proof remains independently recorded in its own review.

| Criterion / impact | Required independent outcome |
| --- | --- |
| Authored identity and geometry | bumper_depth locked ball(0,5,3), Receiver(0,.9,-2), source90°Y orientation, one Bumper; wall_and_bumper ball(-3,5,0), Receiver(-5,.9,0), one Wall plus Bumper, reference Wall .4×6×1.5. Source profiles/goals/lockedness retained; no fixture movement to ease tests. |
| Contact semantics | Six-axis and genuine qualifying oblique contacts pay affordable normal work after ordinary collision; post-collision tangential response/spin remain. No arbitrary obliqueness cutoff. |
| Negative/boundary | Geometric depth/grazing misses, resting/separating/subthreshold produce no paid work or ring. Per-body cooldown, distinct targets, released return, overlapping eligible rings and rejected-event absence. |
| Authoring / serialization | Typed mode and mixed inventory; reject unknown/overflow/invalid serialized declarations atomically; current wire/save callers updated together. |
| Actual Chrome | Both routes solve through real UI with sampled trajectories, camera/depth placement, actual Wall resize/property checks, meaningful unsuccessful routes and difficulty controls. No solution import/setters. |
| Lifecycle / publication | Exact authored Run/Reset/SaveLoad, cleared runtime occurrence/cooldown/feedback. Applicable builds and serial cat015b plus affected regressions; exact SnapshotApproval then deployed-origin verification. |
| Scope / reuse | Retain approved6.2 finite source/partialboost behavior; reuse only unchanged relevant proof. No new element/network/globalqualification. Shared geometry precision remains at Epic16; physical nudging remains explicit unmet existing obligation, not qualified by manual placement. |

Entry finding E1 resolved in specification/roadmap: original shorthand “glancing contacts fire no impulse” was overbroad. Independent git show a6c914e:CuriousContraptions.tests/BumperTests.cs73–113 (8b4442) explicitly requires oblique(2,-4,0) closing contact to launch radially while friction generates spin and tangential momentum is preserved; separate z2 body misses. Draft correctly distinguishes true closing contacts from false triggers and preserves complete source acceptance. No new physics model or owner decision is needed.

Approve bounded implementation under retained pair. Change shared runtime only for an actual reproduced acceptance defect; do not restore purged physics or invent a deletion. The legacy Bumper-specific physics targeted by the roadmap was already removed; any new superseded path must be identified from actual implementation. Current draft intentionally does not claim runtime completion. Subsequent findings, raw checks, scoped candidate/artifact identities and terminal verdict belong in this same record.

## Frozen candidate and independent verification — 10 October

Original immutable snapshot SHA256 `28282269c61e5f0adf06489fea2890fd48fa2427d83b67e8cf06278ea6ae8ff2` remains authoritative for unchanged source/contracts and 248 Playtest/230 Production files. Independent checks79adca/308955 found all artifact bytes identical before and after the run. Served main WASM, simulation worker and animation worker match those identities; index matched too. An initial guessed /worker.js returned404; the actual simulation/worker.js and animation/worker.js paths were then verified. Direct connector tabs check succeeded. No runtime edit occurred during testing.

Bounded replacements/addition (no duplicate snapshot):
- WorkshopBumperTests.cs:7f7b78d49f891dbb6eb255d3aa9f7bcd90ccdfbbe0b5134b3514070796b91295
- tools/e2e/cat-015b.test.ts:fce55bfbdb0d488115987b56c594f164101d7bbede5b128e52a6c6d99ecf7484
- tools/e2e/workshop-driver.ts:060f8a11f891805d9be017ba67c15603ee60730e97a4e0764fc91bbe142de31e
- tools/e2e/README.md:6b4a37d90269409ff505171b24b6e1b3a94b2dd75b845cab20868145d6f0b930
- tools/workshop-rigid-body.test.mjs:d5448d875b577e9089f108813014819ce2fe7cc50c10fc649b869c5685b53887
- spec-6-3-bumper-multi-angle-contacts.md:21a0f27612ecdb854e6bab739c9ea6b01305904558a44d6b412dc264cfdf767f
- TODO.md:552ec553fdd0800ee720e83ee0a2f07b231098ef874493da17172c82e86d6349
- sprint-status.yaml:d08fd1dbde17f7de2c5033b465119f9efe05286ec4182a445be145ba05766ed4

Independent exact command:
```sh
BUMPER_PREVIEW_URL=http://127.0.0.1:8073/ CAT015B_EVIDENCE_DIR=.anvil/reviewer-6-3-images node --import /Users/aidan/.npm/_npx/fd45a72a545557e9/node_modules/tsx/dist/loader.mjs --test --test-concurrency=1 --test-timeout=150000 --test-force-exit tools/e2e/cat-015a-sidekick.test.ts tools/e2e/cat-015b.test.ts > .anvil/reviewer-6-3-chrome.log 2>&1
```
Session22215, completion2f1336/7df15f: **7/7 Pass, zero skipped/failed,131187.07475ms**. Complete raw log SHA256 c394eee60eebbc01a2d00bd58f0bc5305357feb19793e205db921a4076003387; twelve run-specific authored/outcome/reset PNGs under .anvil/reviewer-6-3-images. Dynamic log/images were Anvil-preview validated before writes; this is not a full secret scan of future binary/output bytes. Console/page-error assertions passed. Force-exit is runner cleanup, not lifecycle evidence: actual live body bytes, capture clearing, neutral ring, authored Save/Load/Reset and repeated Run are asserted explicitly.

| Criterion / impact | Reviewed result |
| --- | --- |
| Exact authored modes, fixed source fixtures and mixed inventory | Native source-profile tests plus independent current declaration/diff read61aee1/2381dd/d7586d; both UI placements/Wall resize inspected through saved bytes and actual trajectories. |
| Qualifying contact, negative routes | Depth and Wall solve; depth miss no event/debit/ring; missing Wall pays but cannot solve; Sidekick paid/unpaid/miss regressions pass. Actual paid WASM spin>1 asserted. |
| Difficulty | Independent numerical Receiver margin/speed/dwell/guide expectations at0/.45/1, native and actual UI. |
| Serialization rejection / atomicity | Both modes reject unknown id, bad inventory/profile/precision through save and command decoders, preserving buffers and accepted construction. Unchanged LoadSave decodes before SubmitConstruction (ui/Workshop.cs799); no malformed load can reach admission. Read9e3546/4e967a. |
| Cooldown | Recording-payment Node boundary proves ordinal1 remains before eligibility and next occurrence73/sequence2 at eligibility. It does not claim actual WASM payment. |
| Genuine finite payment and overlap | Original279 filter includes FromStore normal-projection/finite-budget theory and DeclaredImpulseEnvelopeCombinesOverlapInTheSharedBatchAndReturnsToNeutral. Raw native log confirms279/279; corrected Bumper32 includes payment theory. Independent source read068b03/4e967a and unchanged ContactWorkImpulse, simulation worker and presentation closure relative published60ddbbe justify reuse. Ordinary friction spin plus unchanged current-contact normal impulse application and actual paid-spin Chrome complete oblique coverage without claiming the recording stub executes payment. |
| Builds / regression applicability | Both frozen builds pass; runtime unchanged by test fixes. Prior published6.2 physics/Battery/clock evidence remains applicable to unchanged closure; affected puzzle/picker/inventory path rerun via all7 cases. No global qualification claim. |

## BMAD layer findings and forward re-review

Distinct context-free agents /root/bumper63_blind, /root/bumper63_edge, /root/bumper63_gap launched before coordinator triage; retained independent reviewer /root/reviewer, owner /root/bumper_implementation. No layer restart. Twelve individual findings:
| Finding | Severity / disposition |
| --- | --- |
| B1 enum declarations | Medium, fixed true enums with documented installed tsx loader; seven-case run passes. |
| B2 paid tangent proof | Medium, recording stub limitation explicit; genuine unchanged law/application proof and actual paid spin retained as above. |
| B3 exact cooldown ordinal | Medium, ordinal73/sequence2 and before-boundary unchanged ordinal asserted; Node1/1 raw83f58f. |
| B4 overlapping rings | Low, existing279 overlap control and unchanged relevant inputs establish reuse; no duplicate test. |
| B5 live Reset/replay | Medium, all body bytes/capture/animation plus second Run and authored restoration pass actual Chrome. |
| B6 Receiver difficulty | Medium, independent expected values at three settings pass. |
| B7 malformed serialized modes | Medium, native32 controls and decode-before-admission source closure reviewed. |
| B8 overwritten images | Medium, unique run directory refuses reuse; original attempts preserved. |
| B9 incomplete snapshot | False: original snapshot contains actual paths/full identities. |
| E1 missing dock map | Medium, mode-specific maps used by actual Depth move/Wall resize; Sidekick regression passes. |
| G1 difficulty gap | Medium, same root cause as B6, retained separately and closed by same independent assertions. |
| G2 extra balls admitted | False: global WorkshopPuzzle.Validate enforces exactly one Basketball before advanced dispatch. |

Original Wall invented2m/s assertion failure, preview-listener failure and tsx named-callback serialization failure remain in original owner logs. Forward anonymous RAF-loop fix retains frame count/timeout without product hooks. No unresolved intended-scope finding remains.

## Exact pre-publication approval

Mechanical EOF-only replacement approved after staging check: epic-6-context.md SHA25674450fc27744bd13b8cd1167602093385cbcd0500a3eee7f78acdf3e81d6e7e4; spec-6-3-bumper-multi-angle-contacts.md SHA256f24695de3caf50b2c1ed7cbb91afda9821345eb0984590456d212be83062f906. Each removes exactly one trailing empty line; previous spec hash above is superseded. Runtime approval unchanged.

**SnapshotApproval Pass** for the original snapshot plus replacements above, exact22 paths: its16 source paths; tools/e2e/README.md; immutable snapshot; this review record; previously verified terminal6.2 status closure in review-6-2-bumper-battery-integration.md, spec-6-2-bumper-battery-integration.md and CAT-005-battery.md. Shared TODO/sprint/CAT015 preserve that status closure. Six unrelated historical7.x/question dirty files and unrelated scratch/archive/diagnostic files are excluded. Review record is excluded from self-hashing. Normal commit/push authorized by the owner's standing publication instruction; no force or archive upload.

Publication-dependent checks remain: independently verify committed22-path membership/tree and exclusions; remote exact commit; successful Actions Production build/artifact and Pages deployment identity; bind loaded production assets to CI; actual production-origin Depth and Wall positive routes and meaningful depth-miss/missing-Wall controls, actual Wall resize, difficulty settings and Save/Load/Run/Reset/repeat lifecycle using real UI. Production proof must not pretend diagnostic accounting exists; retain exact local accounting evidence separately. Only then record terminal scoped Pass. Physical nudging, full Battery/network and later device/performance gates remain explicitly outside this completion.
