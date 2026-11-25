# No skipped tests before commit

This checkout uses Anvil's custom anti-pattern registry, rule `TEST-001`,
through `.githooks/pre-commit`. It rejects common xUnit v3 skip settings,
runtime skip calls and exceptions, and JavaScript/TypeScript
`test`/`it`/`describe` skip, fixme, todo and only markers.

The hook reads added, copied, modified, renamed and type-changed source files
**from Git's index**. It checks their full staged contents. Unstaged changes
cannot hide a staged skip. Deleted files and an empty index need no skip
check. A staged path whose content git cannot show, such as a gitlink at a
source file's name, refuses the commit by one line that names the path and
carries git's own error.

## Staged-source rule

Several projects compile every C# file in a directory, so a new C# file that
is in the working tree but not staged is compiled and run locally while the
commit would not contain it. Before the skip check, `CheckStagedTests.cs`
refuses such a commit and names each file:

- a C# file that is only intent-to-add (`git add -N`) is refused on every
  commit, because that entry is the committer's own index state;
- an untracked C# file, or a C# file ignored by name, is refused when the
  commit stages source: any path, in any status (added, modified, renamed,
  type-changed or deleted), whose extension this tool scans (`.cs`, `.js`,
  `.mjs`, `.cjs`, `.jsx`, `.ts`, `.tsx`) or is one of `.csproj`, `.props`,
  `.targets`, `.slnx`, `.sln`, `.rsp`, `.editorconfig`, `.json`, `.tscn`. A
  commit that stages none of these, such as one of documents (`.md`, `.yaml`,
  `.toml`), is not refused for it, so a checkout holding someone else's
  untracked files can still commit documents. A C# file under an ignored
  directory (build output, `.tools/`) is never refused.

The refused names are printed one per line between the lines `Not staged:`
and `End of the list.`. The remedies are to stage the file (`git add`, or
`git add -f` for a file ignored by name) or move it outside the repository
tree. Do not delete a file you do not own. The rule checks new files only:
unstaged edits to a tracked file are not checked. One hazard remains: a
wholly untracked new C# file in a commit that stages none of the extensions
above is not caught here; `git status` at handoff covers it. A git query that
fails, including the one that finds the repository, refuses the commit and
names the query and git's error.

The hook reaches this tool only after it has written the index snapshot and
built it, which takes minutes. Run the cheap check first, before a candidate
commit:

```sh
dotnet run --file tools/anvil/CheckStagedTests.cs
```

On every invocation the tool first checks the rule's own decision against
fourteen cases with git's answers supplied, and refuses if one does not hold.
Git's real answers are proven by the verification below.

## C# static gate

Anvil does not analyse C#. First of all, `.githooks/pre-commit` writes the
staged index to a fresh temporary directory (`git checkout-index`, outside
every checkout) and builds it from scratch:

- `CuriousContraptions.slnx`, both default and `-p:PlaytestDiagnostics=true`;
- the web host's own sources, compile only;
- both `tools/anvil` apps.

It then runs `CheckStagedTests.cs` from that snapshot against the real index
(the staged-source rule, then the skip check), then `CSharpStaticGateTests`
in the snapshot, and last the normal Anvil hook. `Directory.Build.props` builds every
C# project and file-based app with the .NET analyzers and warnings as errors
(Story 9R.0). Any C# warning, suppression, widened exemption or swallowed
exception therefore refuses the commit. Unstaged edits and stale build outputs
cannot change the result. CI runs the same builds.

## Skip check

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

The harness must pass whenever a file under `tools/anvil/` changes; the rule
is stated once, in the
[delivery workflow](../../docs/delivery-workflow.md#policy-tool-harness).

Verified on Anvil 0.14.3-alpha.6, git 2.56.1 and .NET SDK 10.0.403: 78 checks
passed. The harness counts one check per `PASS` line, a check passing once
together with its controls, and prints the total only after it has removed
every directory it made.

- One is the harness's own isolation, described below.
- Twenty-six are the skip-rule checks: ordinary tests, xUnit skip settings,
  conditional/runtime skips, Playwright skip/fixme/focus, a Playwright skip
  in each of a `.js`, `.mjs`, `.cjs`, `.jsx` and `.tsx` file, attempted
  inline suppression, rejected fixture enums, an empty index and a rejected
  commit. A fixture that passes must show its scan: its `Anvil snapshot` line
  and the scanner's JSON report. The empty index passes with the rule's
  self-check line and no snapshot line.
- Fifty concern staged source, all against real git:
  - twenty-six written cases. Twenty are refusals, each listing exactly the
    expected files: an untracked, an intent-to-add (with and without other
    staged source), a nested and an upper-case-extension C# file; a C# file
    ignored by name, alone in an untracked directory, beside another ignored
    file there, two directories down, and ignored by a glob pattern; an
    untracked C# file beside a staged deletion, a rename to a non-source
    name, a type change, and a staged `.csproj`, `APP.CSPROJ`, `.props`,
    `.slnx`, `.json` or `.ts` file; and an untracked C# file when the hook
    runs from a subdirectory, listed relative to the top level. Five are
    passes whose output carries the rule's self-check line: an ignored
    directory, a fully staged new file, a staged non-ASCII name, a staged
    source deletion alone, and an untracked C# file when only documents are
    staged. One is a real git failure (an unreadable index), refused with the
    failing query and git's error text;
  - one refusal through a real `git commit`: with a source staged and a new
    C# file untracked, the commit is refused, lists the file and creates no
    commit;
  - one comparison and sixteen refusals for the source-commit set: the set
    the tool prints must equal the sixteen extensions the harness holds as
    its own list, and each member, as the staged file's whole name, refuses
    an untracked C# file;
  - four skip scans: a staged skip in a `.CS` file, in a regular file that
    replaced a symbolic link (a staged type change), and in a file named
    `0:Foo.cs` beside a clean staged `Foo.cs` is blocked, the tool reading
    each staged path as `git show :0:<path> --`. Its control, the same pair
    without the skip beside a committed file literally named `:0:Foo.cs`,
    run with `GIT_TRACE=1`, passes with both paths scanned and shows the
    trace line of git's successful read. A staged gitlink at a source file's
    name is refused by one line naming the path and carrying the error real
    git gives for `git show :0:<path> --`, which occurs once in the output; with
    `GIT_TRACE=1` the same refusal is still one line, carrying git's trace
    line and its error joined by `; `, and no other line holds either;
  - two refusals when git is unusable: outside any repository (a fresh
    directory, with git kept from looking above it), and with no git on the
    child's `PATH`. The hook runs under `/bin/sh`, and `PATH` holds only a
    harness-made directory with a link to the `dotnet` at the root of the
    runtime the harness runs on; if git starts all the same, the case fails
    for want of the refusal text.
- One is the harness's own cleanup: of two temporary directories, the first
  holding a subdirectory it cannot write, removal attempts both, removes the
  second, reports the first and returns false; with the mode restored, the
  first is removed. The mode is restored in a `finally` that never throws;
  the check's own cleanup removes only directories no removal was yet
  attempted on, so it never retries a failed removal.

Every child the harness starts has every `GIT_*` variable removed,
`GIT_CONFIG_NOSYSTEM=1`, `GIT_CONFIG_GLOBAL=/dev/null` and `LC_ALL=C`, then
the case's own variables. The disposable repository sets `core.excludesFile`
and `core.attributesFile` to `/dev/null`, because git reads its default
ignore and attributes files whatever `GIT_CONFIG_GLOBAL` names. The isolation
check sets `GIT_DIR`, `GIT_CONFIG_PARAMETERS`, `GIT_CONFIG_COUNT` with its key
and value, `GIT_CONFIG_GLOBAL` and `XDG_CONFIG_HOME` (configuration, ignore
and attributes files) in the harness's own environment and requires that a
child's git sees none of them; its controls pass a seed through a child's own
environment and see it. A configuration file passed to a child as
`GIT_CONFIG_SYSTEM` is not read either, and its control, the same file with
`GIT_CONFIG_NOSYSTEM=0`, is. The run was made five times: with the pinned
toolchain directory first on `PATH`; with a directory that holds both
`dotnet` and `git` first; with a wrapper script named `dotnet` first; with
`DOTNET_ROOT` unset; and with `DOTNET_ROOT` naming an empty directory.

The tool runs from outside the disposable repository, as the hook runs it
from its snapshot. Each source case also replaces the worktree copy
after staging to verify index isolation.

This is a textual marker policy, not semantic analysis or a test runner.
Aliases, computed calls, and custom test-discovery implementations are outside
its detection contract. Matching marker text in comments or string literals
can also be rejected. It does not detect deleted tests or prove that tests ran.
Historical artifacts are not rewritten.
