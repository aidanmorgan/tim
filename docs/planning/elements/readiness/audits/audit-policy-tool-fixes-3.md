# Planning audit 3: `spec-policy-tool-fixes.md` (policy-tool and harness fixes, lane 4)

Independent planning auditor (context-free), 24 November 2026, against `0959e883` and the worktree drafts. No file edited; probes ran only as small file-based apps and one git repository in the session scratchpad.

**Verdict: Incomplete**

One blocking finding (finding 1): the text that answered round-2 finding 1 rests on a premise a direct probe disproves. The open owner decision O1 would not block a Scoped Pass by itself, because the contract says dispatch waits for it. Nothing breaks the forward-only rules or the element-free engine rules.

## Round-2 findings

| # | Subject | Status |
|---|---|---|
| 1 | AC2 (e)–(g) and refusal clauses never reach harness code | Applied, but D2's replacement reason is false when probed (new finding 1). Not resolved |
| 2 | AC2 lettering | Resolved |
| 3 | README range `:146–149` | Resolved (`:116–149`) |
| 4 | D3 derivation would throw | Resolved (`requireSuccess: false`, non-zero exit, first non-empty line) |
| 5 | Lettered-level registration count | Resolved |
| 6 | Boundaries overstated P1 and P9 | Resolved |

## New findings

1. **Blocking. D2's premise is false; P2's input column and the Capability-probe wording follow from it.** D2 says `dotnet run` always supplies `DOTNET_ROOT`, naming the root its apphost resolved. Probed with the pinned SDK 10.0.403: a caller with no `DOTNET_ROOT` gives the app `DOTNET_ROOT_ARM64=<pinned root>` and `DOTNET_HOST_PATH=<pinned>/dotnet`, but `DOTNET_ROOT` unset; a caller `DOTNET_ROOT` (`/nonexistent-root`, or Homebrew's `/opt/homebrew/opt/dotnet/libexec`, this session's shell default) passes through unchanged; in every case `RuntimeEnvironment.GetRuntimeDirectory()` reports `<pinned>/shared/Microsoft.NETCore.App/10.0.14/`, because `DOTNET_ROOT_ARM64` takes precedence. So under D2 a caller without `DOTNET_ROOT` gets a null root and one with a stale or other `DOTNET_ROOT` gets another install's muxer: the no-git case again depends on how `dotnet` is configured, and no AC catches it because Verification always sets `DOTNET_ROOT`. Fix: D2 links `<root>/dotnet`, `<root>` being three levels above `RuntimeEnvironment.GetRuntimeDirectory()`, reading no environment variable; P2's input column and the probe wording follow; AC2 gains (d) `DOTNET_ROOT` unset and (e) `DOTNET_ROOT` naming an empty scratch directory, each passing with the AC1 total; the P2 table row lists them as controls; keep (c) under the Verification environment. Feasibility: a probe built warning-free with `TreatWarningsAsErrors` and `Nullable` printed `<pinned>/dotnet`; a fresh `dotnet run --file` succeeds with `PATH` holding only that link and `DOTNET_ROOT` unset (`DOTNET_ROOT_ARM64` set, as a hook child inherits), so D2's stop condition is not triggered; with a caller `DOTNET_ROOT` of `/nonexistent-root`, `dotnet run` still runs the app. Do not use `DOTNET_HOST_PATH`: it is absent whenever the caller sets `DOTNET_ROOT`.

2. **Non-blocking. Boundaries.** The harness runs an isolated hook (`VerifyPolicy.cs:34–36`; its total line says "Isolated staged-test hook only; full production hook unqualified"), not `.githooks/pre-commit`. Fix: "…runs real git and the harness's isolated hook (which runs the real `CheckStagedTests.cs`) against a disposable repository…".

3. **Non-blocking. D7 lane admission.** Owner decision 24m says the lanes "share only `TODO.md`, `deferred-work.md` and `delivery-batches.md`". This slice also shares `completion-ledger.md` with lane 3 (OS-04 against CA-01 and OS-02). Fix: the same ruling covers `completion-ledger.md`; if it counts as code, this slice runs after lane 3's slice publishes; cite 24m's "share only" sentence as the text being ruled on.

## What was verified (against `0959e883`)

- Round-2 fixes applied: AC2 (a)–(c) only; D3 `requireSuccess: false`; README `:116–149`; lettered-level re-measure stated; Boundaries name P1 and P9; no stated `DOTNET_ROOT` requirement remains in D5, tasks 9 and 10, or "What this removes".
- Probes (git 2.56.1, SDK 10.0.403): `git rev-parse --local-env-vars` lists 15 names; with `GIT_CONFIG_GLOBAL=/dev/null` and `GIT_CONFIG_NOSYSTEM=1`, `git config --show-origin --get-all core.hooksPath` outputs exactly `file:.git/config\t.git-hooks\n`; the sentinel control and the hand-kept-list counter-case can fail (`GIT_CONFIG_COUNT` leaks); `git show :Module.cs` on a staged gitlink exits 128 with `fatal: bad object :Module.cs`, and since `:238` does not capture stderr, D3's "exactly once, inside the refusal line" fails on the unchanged tool; the AC2(c) control holds on this host; the isolated hook is `exec dotnet run --file <tool>` only, so a link-only `PATH` suffices.
- Anchors match `0959e883` (`VerifyPolicy.cs`, `CheckStagedTests.cs`, README `:52,114,116–149`, `delivery-workflow.md:58,67,70`, `bmad-loop-protocol.md:54–60,180`, `role-cards.md:33–40`, `deferred-work.md:299–330`).
- AC7 is achievable (the role card's lowercase "whenever" does not match its grep).
- F1–F19 cover every item at `deferred-work.md:306–328` and OS-04; F8 and F12 closed per their owner lines.
- No sibling slice edits `tools/anvil/*`.
- 4,660 words; engine tier correct; no fallback, compatibility path, output hash or skipped test; no engine, package, wire or presentation code touched.
