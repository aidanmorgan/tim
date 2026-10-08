# No skipped tests before commit

This checkout uses Anvil's custom anti-pattern registry, rule `TEST-001`,
through `.githooks/pre-commit`. It rejects common xUnit v3 skip settings,
runtime skip calls and exceptions, and JavaScript/TypeScript
`test`/`it`/`describe` skip, fixme, todo and only markers.

The hook reads added, copied, modified and renamed source files **from Git's
index**. It checks their full staged contents. Unstaged changes cannot hide a
staged skip. Deleted files and an empty index need no source check.

Anvil 0.12.0-beta loads `ANVIL_REGISTRY_PATH` as a replacement catalogue.
The hook scopes that environment variable to a separate invocation, then
runs the normal `anvil hook pre-commit` with the normal catalogue.
A scanner error or any suppressed finding also rejects the commit.

## Installed-version evidence

- Homebrew's `anvil/0.12.0-beta/CHANGELOG.md`, “Custom anti-pattern registries
  are explicit-only”, documents `ANVIL_REGISTRY_PATH`.
- The installed catalogue supplies the registry schema, severity values,
  source target and regex detection format.
- `anvil check --help` documents explicit source files, severity and JSON reports.
- `anvil hook pre-commit --help` documents the normal L3 hook.
- A direct C# probe produced zero findings despite loading TEST-001; the
  equivalent supported TypeScript probe produced a blocking finding.
  Therefore the C# adapter copies unchanged staged bytes into temporary
  source snapshots with a supported `.ts` suffix. Snapshot names are printed
  alongside their original repository paths. No source is executed.

## Install and verify

Requires Anvil and .NET SDK 10. From the repository root:

```sh
git config --local core.hooksPath .githooks
chmod +x .githooks/pre-commit
dotnet run --file tools/anvil/VerifyPolicy.cs
```

The current checkout is installed using a small `.git/hooks/pre-commit`
wrapper, preserving the existing hook path configuration. New clones can use
the commands above if they do not use another hook manager; otherwise invoke
`sh .githooks/pre-commit` from that manager's pre-commit command.

Verification creates disposable repositories and uses the actual shell hook,
Anvil executable, Git index, and a real rejected commit. Fixtures are serialized
test inputs in `fixtures.json`; their Source and Expected fields deserialize
into enums with unknown names and numeric values rejected.

Verified on Anvil 0.12.0-beta and .NET SDK 10.0.401: 21 checks passed,
including ordinary tests, xUnit skip settings, conditional/runtime skips,
Playwright skip/fixme/focus, attempted inline suppression, an empty index,
and a rejected commit. Each source case also replaces the worktree copy
after staging to verify index isolation.

This is a textual marker policy, not semantic analysis or a test runner.
Aliases, computed calls, and custom test-discovery implementations are outside
its detection contract. Matching marker text in comments or string literals
can also be rejected. It does not detect deleted tests or prove that tests ran.
Historical artifacts are not rewritten.
