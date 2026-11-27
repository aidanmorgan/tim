using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Extensions.Footnotes;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.0: the architecture authority docs state the sole design. AGENTS.md links the isolation authority, and
/// none of the five other authorities presents a retired design as current (the isolation doc alone names it, in its
/// Current status).
/// <para>DOC-ENGINE-STATUS (VG-1): one link rule over the authority docs and every Markdown file the slice edited in <c>37a259c9..d4b84b4e</c>, within a
/// stated supported subset. Markdown is CommonMark plus pipe tables, footnotes, YAML front matter, GitHub heading
/// identifiers, extended autolinks, emphasis extras and task lists, as parsed by Markdig 1.6.0 (<c>UsePipeTables</c>,
/// <c>UseFootnotes</c>, <c>UseYamlFrontMatter</c>, <c>UseAutoIdentifiers</c>, <c>UseAutoLinks</c>, <c>UseEmphasisExtras</c>,
/// <c>UseTaskLists</c>). Links come from inline links, link reference definitions, autolinks, images and HTML
/// <c>&lt;a href&gt;</c>; anchors come from headings and <c>&lt;a id&gt;</c>. Only the schemes <c>http</c>, <c>https</c> and
/// <c>mailto</c> are external; every other destination is a repository path. Each path must name a present tracked file or
/// directory, case-exact, and each <c>#fragment</c> an anchor of its Markdown target.</para>
/// <para>Anything outside the subset is reported as a typed problem or named below as a limit. Typed problems: a line-number
/// fragment (<c>#L10</c>) or a fragment on a directory (its README), which this rule does not support; an unquoted
/// <c>href</c> or <c>id</c>, or an <c>href</c> or <c>id</c> on an element other than <c>&lt;a&gt;</c>; and a code fence left
/// open at document level, which CommonMark runs to the end of the document, hiding every later link. HTML comments are not
/// read.</para>
/// <para>Named limits, none of which occurs in the checked documents: Markdig's GitHub slugs can differ from GitHub's for
/// combining marks, <c>\p{No}</c> characters, image alt text in headings and empty slugs; submodules and symbolic links are
/// not modelled; markup after a comment block's closing marker on the same line is not read; unclosed raw HTML or comment
/// blocks are not reported; attribute-like text or a <c>&gt;</c> inside a quoted attribute value can be misread; CDATA,
/// processing instructions and declarations are not read; a tag with two offending attributes is reported twice; a fence
/// left open inside a list item or blockquote is not reported (it closes with its container); <c>&lt;img src&gt;</c> and
/// <c>srcset</c> are not read; a reference use with no definition is not reported, since GitHub shows it as text; and
/// Markdig's <c>UseAutoLinks</c> may link bare text (such as <c>ftp://</c>) that GitHub leaves as text.</para></summary>
public sealed class ArchitectureDocsConsistencyTests(ITestOutputHelper output)
{
    private const string IsolationAuthority = "docs/architecture/element-package-isolation.md";

    /// <summary>The authorities that must not present a retired design as current.</summary>
    private static readonly string[] CurrentAuthorities =
    [
        "AGENTS.md",
        "_bmad-output/planning-artifacts/architecture.md",
        "docs/engine-contracts.md",
        "docs/general-engine-design.md",
        "docs/gpu-f32-physics.md",
    ];

    /// <summary>Every Markdown file the DOC-ENGINE-STATUS slice edited (<c>git diff --name-only 37a259c9 d4b84b4e -- '*.md'</c>),
    /// listed explicitly.</summary>
    private static readonly string[] SliceEditedDocuments =
    [
        "DESIGN.md",
        "TODO.md",
        "_bmad-output/implementation-artifacts/deferred-work.md",
        "_bmad-output/implementation-artifacts/run-logs/spec-doc-engine-status.md",
        "_bmad-output/implementation-artifacts/spec-doc-engine-status.md",
        "_bmad-output/planning-artifacts/architecture.md",
        "_bmad-output/planning-artifacts/epics.md",
        "docs/architecture/element-package-isolation.md",
        "docs/browser-physics-performance.md",
        "docs/engine-contracts.md",
        "docs/general-engine-design.md",
        "docs/gpu-f32-physics.md",
        "docs/planning/elements/EL-065-load-hook.md",
        "docs/planning/elements/EL-069-moving-bucket.md",
        "docs/planning/elements/EL-156-single-lobe-cam.md",
        "docs/planning/elements/EL-158-rise-hold-fall-cam.md",
        "docs/planning/elements/readiness/batch-i.md",
        "docs/planning/elements/readiness/completion-ledger.md",
        "docs/planning/elements/readiness/engine-stories-index.md",
        "docs/planning/elements/readiness/owner-decisions-2026-11-22.md",
        "docs/planning/invest/current-consumers.md",
        "docs/planning/invest/delivery-batches.md",
        "docs/planning/invest/named-elements.md",
        "docs/planning/invest/vertical-delivery.md",
        "docs/shared-clock-cadence.md",
        "docs/simulation-presentation-bridge.md",
    ];

    /// <summary>The documents the link rule checks: the six authority docs and the slice's Markdown files, once each.</summary>
    private static readonly string[] LinkCheckedDocuments =
        [.. CurrentAuthorities.Append(IsolationAuthority).Concat(SliceEditedDocuments).Distinct(StringComparer.Ordinal)];

    /// <summary>Retired designs, matched case-insensitively: the catalogue resources, the nonexistent pose-ring script and
    /// engine assembly, and the deleted worker half-precision accessors.</summary>
    private static readonly Regex[] RetiredPhrases =
    [
        // Story 9A.14 deleted the whole res://parts tree: the bundle is the sole catalogue and scenes live in their packages.
        new(@"res://parts/", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"\.tres\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"pose-ring\.js", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"CuriousContraptions\.Engine\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"\bgetF16\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"\bsetF16\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
    ];

    /// <summary>CommonMark plus the GitHub extensions of the supported subset.</summary>
    private static readonly MarkdownPipeline GitHubMarkdown = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UsePipeTables()
        .UseAutoLinks()
        .UseFootnotes()
        .UseEmphasisExtras()
        .UseTaskLists()
        .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
        .Build();

    /// <summary>The only schemes that make a destination external.</summary>
    private static readonly string[] ExternalSchemes = ["http", "https", "mailto"];

    private static readonly Regex Scheme = new(@"^([A-Za-z][A-Za-z0-9+.-]*):", RegexOptions.CultureInvariant);

    /// <summary>A source-line fragment: <c>L10</c>, <c>L10-L20</c>, optionally with columns.</summary>
    private static readonly Regex LineFragment = new(@"^L\d+(?:C\d+)?(?:-L\d+(?:C\d+)?)?$", RegexOptions.CultureInvariant);

    /// <summary>An HTML opening tag and its element name.</summary>
    private static readonly Regex HtmlTag = new(@"<([A-Za-z][A-Za-z0-9-]*)\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>A whole <c>href</c> or <c>id</c> attribute (not <c>data-href</c> or <c>data-id</c>) with a double-quoted,
    /// single-quoted or unquoted value.</summary>
    private static readonly Regex HtmlAttribute =
        new(@"(?<![\w:.-])(href|id)\s*=\s*(?:""([^""]*)""|'([^']*)'|([^\s""'=<>`]+))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex HtmlComment = new(@"<!--[\s\S]*?-->", RegexOptions.CultureInvariant);

    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(60);

    /// <summary>The repository's tracked tree, from one <c>git ls-files</c> per test run, with a tracked file deleted from the
    /// worktree counted as missing.</summary>
    private static readonly Lazy<Func<string, EntryKind>> RepositoryTree =
        new(() => PresentKinds(TrackedKinds(GitTrackedFiles()), path => File.Exists(Path.Combine(Root, path))));

    /// <summary>GitHub slugs of punctuation, an inline code span, a link, emphasis, a backslash escape and an entity.</summary>
    private static readonly string[] GitHubSlugs = ["8-demand-grown-pools--foo_bar", "_bmad-layout", "emphasis-and-strong", "save--load", "see-the-guide"];

    private static string Root => twodog.Engine.ResolveProjectDir();

    private static string Read(string relative) =>
        ReadRepositoryFile(relative) ?? throw new FileNotFoundException("Repository document is missing or untracked.", relative);

    /// <summary>What a repository-relative path names.</summary>
    public enum EntryKind { Missing, File, Directory }

    /// <summary>What is wrong with a link: a target that is not a present tracked file or directory; a fragment its Markdown
    /// target does not define; a fragment form outside the supported subset (a source line, or a directory's README); an
    /// HTML link or anchor form outside the subset; or a document-level code fence never closed.</summary>
    public enum LinkProblemKind { MissingTarget, MissingFragment, UnsupportedFragment, UnsupportedHtml, UnclosedFence }

    /// <summary>A link problem at a document line. <see cref="Detail"/> is the parsed destination (the fragment for a missing
    /// fragment, the tag for unsupported HTML, the fence marker for an unclosed fence).</summary>
    public readonly record struct LinkProblem(string Document, int Line, LinkProblemKind Kind, string Detail);

    /// <summary>One relative link of a document: its line, its destination, its repository-relative target with <c>/</c>
    /// separators (null above the repository root) and its decoded fragment (null when it has none).</summary>
    private readonly record struct RelativeLinkTarget(int Line, string Destination, string? Target, string? Fragment);

    /// <summary>A parsed document: its relative links, the fragments it defines, its HTML outside the subset (line and tag)
    /// and its unclosed document-level code fence, if any.</summary>
    private sealed record ParsedDocument(
        IReadOnlyList<RelativeLinkTarget> Links,
        IReadOnlySet<string> Anchors,
        IReadOnlyList<(int Line, string Tag)> UnsupportedHtml,
        (int Line, string Marker)? UnclosedFence);

    /// <summary>Every retired phrase in <paramref name="text"/>, in order of appearance.</summary>
    public static IReadOnlyList<string> FindRetiredPhrases(string text) =>
        RetiredPhrases.SelectMany(pattern => pattern.Matches(text)).OrderBy(match => match.Index).Select(match => match.Value).ToList();

    /// <summary>The fragments a Markdown document defines: Markdig's GitHub heading identifiers and the quoted <c>id</c> of
    /// each <c>&lt;a&gt;</c> tag.</summary>
    public static IReadOnlySet<string> Anchors(string markdown) => Parse("document.md", markdown).Anchors;

    /// <summary>The one link rule (see the class summary) over <paramref name="documents"/>: files are read through
    /// <paramref name="read"/> and paths classified through <paramref name="kindOf"/>.</summary>
    public static IReadOnlyList<LinkProblem> FindLinkProblems(IEnumerable<string> documents, Func<string, string?> read, Func<string, EntryKind> kindOf)
    {
        var problems = new List<LinkProblem>();
        var parsed = new Dictionary<string, ParsedDocument?>(StringComparer.Ordinal);
        ParsedDocument? ParsedMarkdown(string path)
        {
            if (!parsed.TryGetValue(path, out var found))
                parsed[path] = found = path.EndsWith(".md", StringComparison.Ordinal) && kindOf(path) == EntryKind.File && read(path) is { } text
                    ? Parse(path, text)
                    : null;
            return found;
        }
        foreach (var document in documents)
        {
            var source = ParsedMarkdown(document) ?? throw new FileNotFoundException("Checked document is missing.", document);
            foreach (var link in source.Links)
            {
                var kind = link.Target is null ? EntryKind.Missing : kindOf(link.Target);
                if (kind == EntryKind.Missing)
                    problems.Add(new(document, link.Line, LinkProblemKind.MissingTarget, link.Destination));
                else if (link.Fragment is not { } fragment)
                    continue;
                else if (kind == EntryKind.Directory || LineFragment.IsMatch(fragment))
                    problems.Add(new(document, link.Line, LinkProblemKind.UnsupportedFragment, link.Destination));
                else if (ParsedMarkdown(link.Target!) is not { } target || !target.Anchors.Contains(fragment))
                    problems.Add(new(document, link.Line, LinkProblemKind.MissingFragment, fragment));
            }
            problems.AddRange(source.UnsupportedHtml.Select(html => new LinkProblem(document, html.Line, LinkProblemKind.UnsupportedHtml, html.Tag)));
            if (source.UnclosedFence is { } fence) problems.Add(new(document, fence.Line, LinkProblemKind.UnclosedFence, fence.Marker));
        }
        return problems;
    }

    /// <summary>What a path names in a tree of <paramref name="trackedFiles"/> (repository-relative, <c>/</c> separators):
    /// a tracked file, a directory (any proper prefix of a tracked path ending at a <c>/</c>, and the root <c>""</c>), or
    /// nothing. Matching is ordinal, so case-exact, as on GitHub.</summary>
    public static Func<string, EntryKind> TrackedKinds(IEnumerable<string> trackedFiles)
    {
        var files = trackedFiles.ToHashSet(StringComparer.Ordinal);
        var directories = new HashSet<string>(StringComparer.Ordinal) { "" };
        foreach (var file in files)
            for (var slash = file.IndexOf('/', StringComparison.Ordinal); slash >= 0; slash = file.IndexOf('/', slash + 1))
                directories.Add(file[..slash]);
        return path => files.Contains(path) ? EntryKind.File : directories.Contains(path) ? EntryKind.Directory : EntryKind.Missing;
    }

    /// <summary>A tracked tree in which a tracked file that <paramref name="present"/> does not find is missing.</summary>
    public static Func<string, EntryKind> PresentKinds(Func<string, EntryKind> tracked, Func<string, bool> present) =>
        path => tracked(path) is EntryKind.File && !present(path) ? EntryKind.Missing : tracked(path);

    /// <summary>Parses a document with <see cref="GitHubMarkdown"/>: its links (inline and image links, link reference
    /// definitions, non-email autolinks and HTML <c>href</c>; reference-style uses are checked at their definition), anchors,
    /// unsupported HTML and document-level open fence.</summary>
    private static ParsedDocument Parse(string document, string text)
    {
        var syntax = Markdown.Parse(text, GitHubMarkdown);
        var destinations = new List<(int Line, string Destination)>();
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        var unsupported = new List<(int Line, string Tag)>();
        foreach (var heading in syntax.Descendants<HeadingBlock>())
            if (heading.GetAttributes().Id is { Length: > 0 } id) anchors.Add(id);
        foreach (var link in syntax.Descendants<LinkInline>())
            if (link.Reference is null && link.Url is { } url) destinations.Add((link.Line + 1, url));
        foreach (var definition in syntax.Descendants<LinkReferenceDefinition>())
            if (definition is not (HeadingLinkReferenceDefinition or FootnoteLinkReferenceDefinition) && definition.Url is { } url)
                destinations.Add((definition.Line + 1, url));
        foreach (var autolink in syntax.Descendants<AutolinkInline>())
            if (!autolink.IsEmail && autolink.Url is { } url) destinations.Add((autolink.Line + 1, url));
        void ReadHtml(string html, int firstLine)
        {
            var visible = HtmlComment.Replace(html, comment => new string(comment.Value.Select(c => c == '\n' ? '\n' : ' ').ToArray()));
            foreach (Match tag in HtmlTag.Matches(visible))
            {
                var isAnchor = string.Equals(tag.Groups[1].Value, "a", StringComparison.OrdinalIgnoreCase);
                foreach (Match attribute in HtmlAttribute.Matches(tag.Value))
                {
                    var line = firstLine + visible.AsSpan(0, tag.Index + attribute.Index).Count('\n');
                    if (!isAnchor || attribute.Groups[4].Success)
                    {
                        unsupported.Add((line, tag.Value));
                        continue;
                    }
                    var value = WebUtility.HtmlDecode(attribute.Groups[2].Success ? attribute.Groups[2].Value : attribute.Groups[3].Value);
                    if (string.Equals(attribute.Groups[1].Value, "href", StringComparison.OrdinalIgnoreCase)) destinations.Add((line, value));
                    else anchors.Add(value);
                }
            }
        }
        foreach (var html in syntax.Descendants<HtmlInline>()) ReadHtml(html.Tag ?? "", html.Line + 1);
        foreach (var block in syntax.Descendants<HtmlBlock>())
            if (block.Type != HtmlBlockType.Comment) ReadHtml(block.Lines.ToString(), block.Line + 1);
        var links = new List<RelativeLinkTarget>();
        foreach (var (line, destination) in destinations)
        {
            if (Scheme.Match(destination) is { Success: true } scheme && ExternalSchemes.Contains(scheme.Groups[1].Value, StringComparer.OrdinalIgnoreCase))
                continue;
            var hash = destination.IndexOf('#', StringComparison.Ordinal);
            var path = hash < 0 ? destination : destination[..hash];
            var fragment = hash < 0 || hash == destination.Length - 1 ? null : Uri.UnescapeDataString(destination[(hash + 1)..]);
            links.Add(new(line, destination, path.Length == 0 ? document : Resolve(document, Uri.UnescapeDataString(path)), fragment));
        }
        var unclosed = syntax.Descendants<FencedCodeBlock>().Where(fence => fence.Parent is MarkdownDocument && fence.ClosingFencedCharCount == 0)
            .Select(fence => ((int Line, string Marker)?)(fence.Line + 1, new string(fence.FencedChar, fence.OpeningFencedCharCount)))
            .FirstOrDefault();
        return new(links.OrderBy(link => link.Line).ToList(), anchors, unsupported.OrderBy(html => html.Line).ToList(), unclosed);
    }

    /// <summary>The repository-relative path of <paramref name="file"/> as linked from <paramref name="document"/> (from the
    /// root when it starts with <c>/</c>), or null when it climbs above the repository root.</summary>
    private static string? Resolve(string document, string file)
    {
        var parts = new List<string>();
        var directory = file.StartsWith('/') || !document.Contains('/', StringComparison.Ordinal) ? "" : document[..document.LastIndexOf('/')];
        foreach (var part in $"{directory}/{file}".Split('/'))
        {
            if (part is "" or ".") continue;
            if (part != "..")
                parts.Add(part);
            else if (parts.Count == 0)
                return null;
            else
                parts.RemoveAt(parts.Count - 1);
        }
        return string.Join('/', parts);
    }

    /// <summary>The tracked files of the repository at <see cref="Root"/>, from <c>git ls-files -z</c>, with stdout and
    /// stderr read concurrently. Fails loudly when git cannot run, reports an error or runs past <see cref="GitTimeout"/>.</summary>
    private static string[] GitTrackedFiles()
    {
        var command = $"git -C {Root} ls-files -z";
        var start = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in (string[])["-C", Root, "ls-files", "-z"]) start.ArgumentList.Add(argument);
        Process? process;
        try
        {
            process = Process.Start(start);
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException($"The link check lists tracked files with `{command}`; git must be on PATH.", exception);
        }
        using (process ?? throw new InvalidOperationException($"The link check could not start `{command}`."))
        {
            var listing = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(GitTimeout))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException($"`{command}` did not finish within {GitTimeout.TotalSeconds} s.");
            }
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"`{command}` failed with exit code {process.ExitCode}: {errors.GetAwaiter().GetResult()}");
            return listing.GetAwaiter().GetResult().Split('\0', StringSplitOptions.RemoveEmptyEntries);
        }
    }

    private static string? ReadRepositoryFile(string relative) =>
        RepositoryTree.Value(relative) == EntryKind.File ? File.ReadAllText(Path.Combine(Root, relative)) : null;

    private static string Message(LinkProblem problem) => problem.Kind switch
    {
        LinkProblemKind.MissingTarget => $"no present tracked file or directory for {problem.Detail}",
        LinkProblemKind.MissingFragment => $"the target defines no #{problem.Detail}",
        LinkProblemKind.UnsupportedFragment => $"{problem.Detail}: line-number and directory-README fragments are outside the supported subset",
        LinkProblemKind.UnsupportedHtml => $"{problem.Detail}: only a quoted href or id on <a> is in the supported subset",
        LinkProblemKind.UnclosedFence => $"code fence {problem.Detail} is never closed, so it hides the rest of the document",
        _ => throw new ArgumentOutOfRangeException(nameof(problem), problem.Kind, "Unknown link problem kind."),
    };

    private static IReadOnlyList<LinkProblem> CheckInMemory(params (string Path, string Text)[] documents) =>
        CheckInMemory(documents.Select(document => document.Path), documents);

    /// <summary>The link rule over the first in-memory document, with only <paramref name="tracked"/> in the tracked tree and
    /// only the given documents present.</summary>
    private static IReadOnlyList<LinkProblem> CheckInMemory(IEnumerable<string> tracked, params (string Path, string Text)[] documents)
    {
        var files = documents.ToDictionary(document => document.Path, document => document.Text, StringComparer.Ordinal);
        return FindLinkProblems([documents[0].Path], path => files.GetValueOrDefault(path), PresentKinds(TrackedKinds(tracked), files.ContainsKey));
    }

    private static LinkProblem MissingFragmentAt(int line, string fragment) => new("docs/a.md", line, LinkProblemKind.MissingFragment, fragment);

    private static LinkProblem MissingTargetAt(int line, string destination) => new("docs/a.md", line, LinkProblemKind.MissingTarget, destination);

    [Fact]
    public void AgentsLinksTheIsolationAuthority() =>
        Assert.Contains(Parse("AGENTS.md", Read("AGENTS.md")).Links, link => link.Target == IsolationAuthority);

    [Fact]
    public void NoAuthorityPresentsRetiredDesignAsCurrent()
    {
        var hits = CurrentAuthorities
            .SelectMany(doc => FindRetiredPhrases(Read(doc)).Select(phrase => $"{doc}: {phrase}"))
            .ToList();
        foreach (var hit in hits) output.WriteLine(hit);
        Assert.Empty(hits);
    }

    [Fact]
    public void TheBanCheckRejectsAReinstatedPhrase() =>
        Assert.NotEmpty(FindRetiredPhrases("The registry loads its scenes from res://parts/scenes at startup."));

    [Fact]
    public void EveryRelativeLinkInTheCheckedDocsResolves()
    {
        var problems = FindLinkProblems(LinkCheckedDocuments, ReadRepositoryFile, RepositoryTree.Value);
        foreach (var problem in problems) output.WriteLine($"{problem.Document}:{problem.Line}: {problem.Kind}: {Message(problem)}");
        Assert.Empty(problems);
    }

    [Fact]
    public void TheFragmentCheckReportsAMissingAnchorWithItsFileAndLine() =>
        Assert.Equal(
            [MissingFragmentAt(3, "absent")],
            CheckInMemory(("docs/a.md", "# Title\n\nSee [b](b.md#absent).\n"), ("docs/b.md", "# Present\n")));

    [Fact]
    public void TheFragmentCheckReportsAMissingSameFileAnchor() =>
        Assert.Equal(
            [MissingFragmentAt(2, "elsewhere")],
            CheckInMemory(("docs/a.md", "# Title\nSee [here](#elsewhere).\n")));

    [Fact]
    public void TheFragmentCheckResolvesASameFileHeading() =>
        Assert.Empty(CheckInMemory(("docs/a.md", "# Title\nSee [here](#title).\n")));

    [Fact]
    public void TheFragmentCheckResolvesTheNumberedSlugOfADuplicateHeading() =>
        Assert.Empty(CheckInMemory(
            ("docs/a.md", "See [a](b.md#setup-1) and [b](b.md#setup-2).\n"),
            ("docs/b.md", "## Setup\n\n## Setup 1\n\n## Setup\n")));

    [Fact]
    public void TheFragmentCheckResolvesAnExplicitAnchor() =>
        Assert.Empty(CheckInMemory(
            ("docs/a.md", "See [point](../c/b.md#custom-point).\nSee [root](/c/b.md#custom-point).\nSee [single](/c/b.md#single-point).\nSee [decoded](/c/b.md#a&b).\n"),
            ("c/b.md", "<a id=\"custom-point\"></a>\nText.\n\n<a id='single-point'></a>\nMore.\n\n<a id=\"a&amp;b\"></a>\nLast.\n")));

    [Fact]
    public void TheFragmentCheckIgnoresNameAndDataIdAttributes() =>
        Assert.Equal(
            [MissingFragmentAt(1, "named-point"), MissingFragmentAt(1, "data-point")],
            CheckInMemory(
                ("docs/a.md", "See [n](b.md#named-point) and [d](b.md#data-point).\n"),
                ("docs/b.md", "<a name=\"named-point\"></a>\n<span data-id=\"data-point\"></span>\nText.\n")));

    [Fact]
    public void TheFragmentCheckSlugsPunctuatedHeadingsLikeGitHub() =>
        Assert.Equal(
            GitHubSlugs,
            Anchors("# 8. Demand-grown (pools) & `Foo_bar`\n\n# See [the guide](guide.md)\n\n# _Emphasis_ and **strong**\n\n# \\_bmad layout\n\n# Save &amp; load\n")
                .Order(StringComparer.Ordinal));

    [Fact]
    public void TheFragmentCheckIgnoresHeadingsInsideCodeFences() =>
        Assert.Equal(
            [MissingFragmentAt(1, "ghost"), MissingFragmentAt(6, "after-fence")],
            CheckInMemory(("docs/a.md", "See [ghost](#ghost).\n```sh\n# Ghost\n[inside](#nowhere)\n```\nSee [after](#after-fence).\n")));

    [Fact]
    public void TheLinkCheckReportsALinkToAMissingFile() =>
        Assert.Equal(
            [MissingTargetAt(1, "gone.md#here"), MissingTargetAt(1, "gone.md"), MissingTargetAt(2, "gone.png")],
            CheckInMemory(("docs/a.md", "See [x](gone.md#here) and [y](gone.md).\n![image](gone.png)\n")));

    [Fact]
    public void TheLinkCheckReportsALinkAboveTheRepositoryRoot() =>
        Assert.Equal(
            [MissingTargetAt(1, "../../x.md#y"), MissingTargetAt(1, "../../x.md")],
            CheckInMemory(("docs/a.md", "See [x](../../x.md#y) and [z](../../x.md).\n"), ("x.md", "# Y\n")));

    [Fact]
    public void TheLinkCheckReportsALinkToAnUntrackedFile() =>
        Assert.Equal(
            [MissingTargetAt(1, "local.md")],
            CheckInMemory(["docs/a.md"], ("docs/a.md", "See [x](local.md).\n"), ("docs/local.md", "# Local\n")));

    [Fact]
    public void TheLinkCheckReportsATrackedFileDeletedFromTheWorktree() =>
        Assert.Equal(
            [MissingTargetAt(1, "gone.md")],
            CheckInMemory(["docs/a.md", "docs/gone.md"], ("docs/a.md", "See [g](gone.md).\n")));

    [Fact]
    public void TheLinkCheckResolvesATrackedFileAndDirectoryButNotAPartialName() =>
        Assert.Equal(
            [MissingTargetAt(1, "su")],
            CheckInMemory(
                ["docs/a.md", "docs/sub/b.md"], ("docs/a.md", "See [b](sub/b.md), [sub](sub) and [su](su).\n"), ("docs/sub/b.md", "# B\n")));

    [Fact]
    public void TheLinkCheckReportsAnUnclosedFence() =>
        Assert.Equal(
            [MissingFragmentAt(1, "b"), new LinkProblem("docs/a.md", 3, LinkProblemKind.UnclosedFence, "~~~")],
            CheckInMemory(("docs/a.md", "[shown](#b)\n\n~~~\n[hidden](#c)\n")));

    [Fact]
    public void TheLinkCheckDoesNotReportAFenceClosedByItsContainer() =>
        Assert.Equal(
            [MissingFragmentAt(4, "after"), MissingFragmentAt(9, "later")],
            CheckInMemory(("docs/a.md", "> ```\n> [hidden](#x)\n\n[after](#after)\n\n- ```\n  [inside](#y)\n\n[later](#later)\n")));

    [Fact]
    public void TheLinkCheckSkipsOnlyAClosedFrontMatter() =>
        Assert.Equal(
            [MissingFragmentAt(5, "heading"), MissingTargetAt(2, "gone.md")],
            CheckInMemory(("docs/a.md", "---\ntitle: [x](gone.md)\n# Heading\n---\n[y](#heading)\n"))
                .Concat(CheckInMemory(("docs/a.md", "---\n[z](gone.md)\n"))));

    [Fact]
    public void TheLinkCheckIgnoresCodeSpansAndHtmlComments() =>
        Assert.Equal(
            [MissingFragmentAt(2, "fake")],
            CheckInMemory(("docs/a.md",
                "Quoted `[x](#quoted)` is text.\n`<a id=\"fake\"></a>` [y](#fake)\n\n<!-- [z](gone.md) <a href=\"gone2.md\">z</a> -->\n\nText <!-- <a href=\"gone3.md\">w</a> --> more.\n")));

    [Fact]
    public void TheLinkCheckJudgesLinksInBlockquotesAndQuotedText() =>
        Assert.Equal(
            [MissingTargetAt(1, "gone.md"), MissingTargetAt(2, "also-gone.md")],
            CheckInMemory(("docs/a.md", "He wrote \"see [x](gone.md)\" before.\n> Quoted [y](also-gone.md).\n")));

    [Fact]
    public void TheFragmentCheckResolvesSetextAndBlockquoteHeadings() =>
        Assert.Empty(CheckInMemory(
            ("docs/a.md", "[o](b.md#overview) [d](b.md#details) [q](b.md#quoted)\n"),
            ("docs/b.md", "Overview\n========\n\nDetails\n-------\n\n> ## Quoted\n")));

    [Fact]
    public void TheLinkCheckReadsTitledAngleReferenceAndHrefLinks() =>
        Assert.Equal(
            [MissingFragmentAt(1, "t1"), MissingFragmentAt(3, "t2"), MissingFragmentAt(5, "t3"), MissingFragmentAt(7, "t4"), MissingFragmentAt(11, "t5")],
            CheckInMemory(
                ("docs/a.md",
                    "[t](b.md#t1 \"Title\")\n\n[u](<b.md#t2>)\n\n[ref]: b.md#t3\n\n<a href=\"b.md#t4\">x</a>\n\n<div>\n<a\nhref='b.md#t5'>y</a>\n</div>\n"),
                ("docs/b.md", "# Other\n")));

    [Fact]
    public void TheLinkCheckReportsUnsupportedHtmlAnchorForms() =>
        Assert.Equal(
            [new LinkProblem("docs/a.md", 1, LinkProblemKind.UnsupportedHtml, "<a href=b.md>"),
                new LinkProblem("docs/a.md", 3, LinkProblemKind.UnsupportedHtml, "<span id=\"s\">"),
                new LinkProblem("docs/a.md", 5, LinkProblemKind.UnsupportedHtml, "<area href=\"b.md\">"),
                new LinkProblem("docs/a.md", 7, LinkProblemKind.UnsupportedHtml, "<a id=bare>")],
            CheckInMemory(
                ("docs/a.md", "<a href=b.md>x</a>\n\n<span id=\"s\">y</span>\n\nText <area href=\"b.md\"> more.\n\nText <a id=bare>z</a>.\n"),
                ("docs/b.md", "# B\n")));

    [Fact]
    public void TheFragmentCheckReportsAFragmentOnANonMarkdownTarget() =>
        Assert.Equal(
            [MissingFragmentAt(1, "comment")],
            CheckInMemory(("docs/a.md", "[s](../tools/run.sh#comment)\n"), ("tools/run.sh", "# comment\n")));

    [Fact]
    public void TheLinkCheckReportsUnsupportedFragments() =>
        Assert.Equal(
            [new LinkProblem("docs/a.md", 1, LinkProblemKind.UnsupportedFragment, "b.md#L10"),
                new LinkProblem("docs/a.md", 1, LinkProblemKind.UnsupportedFragment, "sub#readme")],
            CheckInMemory(("docs/a.md", "[l](b.md#L10) and [d](sub#readme)\n"), ("docs/b.md", "# B\n"), ("docs/sub/README.md", "# Readme\n")));

    [Fact]
    public void TheLinkCheckTreatsOnlyHttpHttpsAndMailtoAsExternal() =>
        Assert.Equal(
            [MissingTargetAt(1, "TODO.md:12"), MissingTargetAt(1, "ftp://example.org/x"), MissingTargetAt(1, "ftp://example.org/z"), MissingTargetAt(1, "TODO.md:13")],
            CheckInMemory(("docs/a.md",
                "[a](https://example.org/x) [h](http://example.org/x) [m](mailto:a@example.org) [t](TODO.md:12) [f](ftp://example.org/x) " +
                "<https://example.org/y> <ftp://example.org/z> <TODO.md:13> <a@example.org>\n")));

    [Fact]
    public void TheLinkCheckMatchesPathsCaseSensitively()
    {
        var kindOf = TrackedKinds(["docs/b.md"]);
        Assert.Equal(
            (EntryKind.Missing, EntryKind.Missing, EntryKind.File),
            (kindOf("Docs/b.md"), kindOf("docs/B.md"), kindOf("docs/b.md")));
    }
}
