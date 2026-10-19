using System.Text;
using System.Text.RegularExpressions;

namespace CuriousContraptions.E2eRunner;

/// <summary>The mechanical timing rules (spec TimingDetector task). R0 is an error in every suite; R1-R4 make a suite SerialAlone.</summary>
internal enum TimingRule
{
    /// <summary>R0: page code passed as a string or template, new Function or eval: unscannable.</summary>
    UnscannableCode = 1,
    /// <summary>R1: a loop whose condition reads a wall clock and that never awaits.</summary>
    BusyWaitLoop,
    /// <summary>R2: a reassigned or redefined clock, or page.clock / context.clock.</summary>
    ClockTampering,
    /// <summary>R3: a child process or thread.</summary>
    ProcessOrThreadSpawn,
    /// <summary>R4: a wall-clock difference that is not a direct operand of a relational operator.</summary>
    WallClockDifference,
}

/// <summary>One detector hit: the scanned file (repository-relative), its line, the rule and the code excerpt.</summary>
internal sealed record TimingHit(string File, int Line, TimingRule Rule, string Excerpt)
{
    public override string ToString() => $"{File}:{Line} {Rule}: {Excerpt}";
}

/// <summary>A TypeScript or JavaScript source with comments and literal contents blanked to spaces (newlines kept), so rule
/// patterns only see code. String, template and regex delimiters stay; template <c>${…}</c> code stays.</summary>
internal sealed class ScannedSource
{
    private static readonly HashSet<string> RegexKeywords = new(StringComparer.Ordinal)
    {
        "return", "typeof", "instanceof", "in", "of", "new", "delete", "void", "throw", "case", "do", "else", "yield", "await",
    };

    private readonly int[] _lineStarts;

    public string Original { get; }
    public string Code { get; }
    private readonly bool[] _isCode;

    public ScannedSource(string original)
    {
        ArgumentNullException.ThrowIfNull(original);
        Original = original;
        var code = new StringBuilder(original.Length);
        _isCode = new bool[original.Length];
        Lex(original, code, _isCode);
        Code = code.ToString();
        var starts = new List<int> { 0 };
        for (var index = 0; index < original.Length; index++) if (original[index] == '\n') starts.Add(index + 1);
        _lineStarts = [.. starts];
    }

    /// <summary>Whether the character at <paramref name="index"/> is code (not inside a comment or a literal's content).</summary>
    public bool IsCode(int index) => index >= 0 && index < _isCode.Length && _isCode[index];

    public int LineOf(int index)
    {
        var line = Array.BinarySearch(_lineStarts, index);
        return (line >= 0 ? line : ~line - 1) + 1;
    }

    public string LineText(int line)
    {
        var start = _lineStarts[line - 1];
        var end = line < _lineStarts.Length ? _lineStarts[line] - 1 : Original.Length;
        return Original[start..end].Trim();
    }

    private enum LexState { Code, LineComment, BlockComment, SingleQuoted, DoubleQuoted, Template, Regex }

    private static void Lex(string source, StringBuilder output, bool[] isCode)
    {
        var state = LexState.Code;
        var templates = new Stack<int>();
        var braceDepth = 0;
        var inClass = false;
        var lastSignificant = '\0';
        var lastWord = "";
        for (var index = 0; index < source.Length; index++)
        {
            var character = source[index];
            var next = index + 1 < source.Length ? source[index + 1] : '\0';
            switch (state)
            {
                case LexState.Code:
                    if (character == '/' && next == '/') { state = LexState.LineComment; Blank(output, character); break; }
                    if (character == '/' && next == '*') { state = LexState.BlockComment; Blank(output, character); break; }
                    Keep(output, isCode, index, character);
                    if (character == '\'') state = LexState.SingleQuoted;
                    else if (character == '"') state = LexState.DoubleQuoted;
                    else if (character == '`') state = LexState.Template;
                    else if (character == '/' && StartsRegex(lastSignificant, lastWord)) { state = LexState.Regex; inClass = false; }
                    else if (character == '{') braceDepth++;
                    else if (character == '}')
                    {
                        if (templates.Count > 0 && templates.Peek() == braceDepth) { templates.Pop(); state = LexState.Template; }
                        braceDepth--;
                    }
                    if (char.IsWhiteSpace(character)) break;
                    if (IsWordCharacter(character)) lastWord = IsWordCharacter(lastSignificant) ? lastWord + character : character.ToString();
                    else lastWord = "";
                    lastSignificant = character;
                    break;
                case LexState.LineComment:
                    if (character == '\n') { state = LexState.Code; output.Append('\n'); }
                    else Blank(output, character);
                    break;
                case LexState.BlockComment:
                    Blank(output, character);
                    if (character == '*' && next == '/') { Blank(output, next); index++; state = LexState.Code; }
                    break;
                case LexState.SingleQuoted:
                case LexState.DoubleQuoted:
                    if (character == '\\') { Blank(output, character); if (next != '\0') { Blank(output, next); index++; } break; }
                    if (character == (state == LexState.SingleQuoted ? '\'' : '"') || character == '\n')
                    {
                        Keep(output, isCode, index, character);
                        state = LexState.Code;
                        lastSignificant = '"';
                        lastWord = "";
                        break;
                    }
                    Blank(output, character);
                    break;
                case LexState.Template:
                    if (character == '\\') { Blank(output, character); if (next != '\0') { Blank(output, next); index++; } break; }
                    if (character == '`') { Keep(output, isCode, index, character); state = LexState.Code; lastSignificant = '`'; lastWord = ""; break; }
                    if (character == '$' && next == '{')
                    {
                        Keep(output, isCode, index, character);
                        Keep(output, isCode, index + 1, next);
                        index++;
                        braceDepth++;
                        templates.Push(braceDepth);
                        state = LexState.Code;
                        lastSignificant = '{';
                        lastWord = "";
                        break;
                    }
                    Blank(output, character);
                    break;
                case LexState.Regex:
                    if (character == '\\') { Blank(output, character); if (next != '\0') { Blank(output, next); index++; } break; }
                    if (character == '[') inClass = true;
                    else if (character == ']') inClass = false;
                    else if ((character == '/' && !inClass) || character == '\n')
                    {
                        Keep(output, isCode, index, character);
                        state = LexState.Code;
                        lastSignificant = ')';
                        lastWord = "";
                        break;
                    }
                    Blank(output, character);
                    break;
                default:
                    throw new InvalidOperationException($"Undefined lexer state {state}.");
            }
        }
    }

    private static bool IsWordCharacter(char character) => char.IsAsciiLetterOrDigit(character) || character is '_' or '$';

    /// <summary>A '/' starts a regex literal unless it follows an operand (identifier, number, ')' , ']', '}' or a literal).</summary>
    private static bool StartsRegex(char lastSignificant, string lastWord)
    {
        if (lastSignificant == '\0') return true;
        if (IsWordCharacter(lastSignificant)) return RegexKeywords.Contains(lastWord);
        return lastSignificant is not (')' or ']' or '}' or '"' or '`');
    }

    private static void Keep(StringBuilder output, bool[] isCode, int index, char character)
    {
        output.Append(character);
        isCode[index] = true;
    }

    private static void Blank(StringBuilder output, char character) => output.Append(character == '\n' ? '\n' : ' ');
}

/// <summary>The lexical timing detector over a suite and its local import closure.</summary>
internal static partial class TimingDetector
{
    [GeneratedRegex(@"\b(?:evaluate|evaluateHandle|addInitScript|waitForFunction)\s*\(\s*['""`]")]
    private static partial Regex StringPageCode();
    [GeneratedRegex(@"\bnew\s+Function\s*\(|(?<![.\w$])eval\s*\(")]
    private static partial Regex DynamicCode();
    [GeneratedRegex(@"\b(?:while|for|do)\b")]
    private static partial Regex LoopKeyword();
    [GeneratedRegex(@"\b(?:performance|Date)\s*\.\s*now\s*\(\s*\)")]
    private static partial Regex WallClockRead();
    [GeneratedRegex(@"\bawait\b")]
    private static partial Regex Await();
    [GeneratedRegex(@"\b(?:performance|Date)\s*\.\s*now\s*=(?![=>])|(?:\bglobalThis\s*\.\s*|(?<![.\w$]))Date\s*=(?![=>])")]
    private static partial Regex ClockAssignment();
    [GeneratedRegex(@"\bObject\s*\.\s*defineProperty\s*\(")]
    private static partial Regex DefineProperty();
    [GeneratedRegex(@"\b(?:performance|Date)\b|['""](?:now|Date)['""]")]
    private static partial Regex ClockTarget();
    [GeneratedRegex(@"\b(?:page|context)\s*\.\s*clock\b")]
    private static partial Regex PlaywrightClock();
    [GeneratedRegex(@"\b(?:from|import|require)\s*\(?\s*(['""])(?:node:)?(?:child_process|worker_threads)\1")]
    private static partial Regex SpawningImport();
    [GeneratedRegex(@"\bnew\s+Worker\s*\(")]
    private static partial Regex NewWorker();
    [GeneratedRegex(@"\b(?:from|import)\s*\(?\s*(['""])(\.{1,2}/[^'""]+)\1")]
    private static partial Regex LocalImport();

    /// <summary>Every rule hit in one source (the file label is used for attribution only).</summary>
    public static IReadOnlyList<TimingHit> Scan(string file, string text)
    {
        var source = new ScannedSource(text);
        var hits = new List<TimingHit>();
        void Add(int index, TimingRule rule) => hits.Add(new TimingHit(file, source.LineOf(index), rule, source.LineText(source.LineOf(index))));

        foreach (Match match in StringPageCode().Matches(source.Code)) Add(match.Index, TimingRule.UnscannableCode);
        foreach (Match match in DynamicCode().Matches(source.Code)) Add(match.Index, TimingRule.UnscannableCode);
        foreach (var index in BusyWaitLoops(source.Code)) Add(index, TimingRule.BusyWaitLoop);
        foreach (Match match in ClockAssignment().Matches(source.Code)) Add(match.Index, TimingRule.ClockTampering);
        foreach (Match match in DefineProperty().Matches(source.Code))
        {
            var open = match.Index + match.Length - 1;
            var close = MatchingClose(source.Code, open);
            if (ClockTarget().IsMatch(source.Original[open..close])) Add(match.Index, TimingRule.ClockTampering);
        }
        foreach (Match match in PlaywrightClock().Matches(source.Code)) Add(match.Index, TimingRule.ClockTampering);
        foreach (Match match in SpawningImport().Matches(source.Original))
            if (source.IsCode(match.Index)) Add(match.Index, TimingRule.ProcessOrThreadSpawn);
        foreach (Match match in NewWorker().Matches(source.Code)) Add(match.Index, TimingRule.ProcessOrThreadSpawn);
        foreach (var index in WallClockDifferences(source.Code)) Add(index, TimingRule.WallClockDifference);
        return [.. hits.OrderBy(hit => hit.Line).ThenBy(hit => hit.Rule)];
    }

    /// <summary>The local modules a source imports (relative specifiers only), as written.</summary>
    public static IReadOnlyList<string> LocalImports(string text)
    {
        var source = new ScannedSource(text);
        return [.. LocalImport().Matches(source.Original).Where(match => source.IsCode(match.Index)).Select(match => match.Groups[2].Value)];
    }

    /// <summary>A suite file and every local module it imports, transitively (repository-relative paths, sorted).</summary>
    public static IReadOnlyList<string> ImportClosure(string repositoryRoot, string suitePath)
    {
        var seen = new SortedSet<string>(StringComparer.Ordinal);
        var pending = new Stack<string>([suitePath]);
        while (pending.TryPop(out var relative))
        {
            if (!seen.Add(relative)) continue;
            var full = Path.Combine(repositoryRoot, relative);
            if (!File.Exists(full)) throw new FileNotFoundException($"Imported module {relative} does not exist.", full);
            var directory = Path.GetDirectoryName(relative) ?? "";
            foreach (var specifier in LocalImports(File.ReadAllText(full)))
                pending.Push(Path.GetRelativePath(repositoryRoot, Path.GetFullPath(Path.Combine(repositoryRoot, directory, specifier))).Replace('\\', '/'));
        }
        return [.. seen];
    }

    /// <summary>Every hit in a suite's import closure: a hit in a shared module is attributed to every suite importing it.</summary>
    public static IReadOnlyList<TimingHit> ScanSuite(string repositoryRoot, string suitePath) =>
        [.. ImportClosure(repositoryRoot, suitePath).SelectMany(path => Scan(path, File.ReadAllText(Path.Combine(repositoryRoot, path))))];

    /// <summary>The rule's SerialAlone reason (R0 has none: it is an error in every suite).</summary>
    public static TimingReason? ReasonFor(TimingRule rule) => rule switch
    {
        TimingRule.UnscannableCode => null,
        TimingRule.BusyWaitLoop => TimingReason.InjectsStall,
        TimingRule.ClockTampering => TimingReason.TampersClock,
        TimingRule.ProcessOrThreadSpawn => TimingReason.InjectsLoad,
        TimingRule.WallClockDifference => TimingReason.MeasuresWallTime,
        _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Undefined timing rule."),
    };

    // R1: a while/for/do loop whose condition reads a wall clock and with no await anywhere in the loop statement.
    private static IEnumerable<int> BusyWaitLoops(string code)
    {
        var doTails = new HashSet<int>();
        foreach (Match match in LoopKeyword().Matches(code))
        {
            if (doTails.Contains(match.Index)) continue;
            var after = SkipSpace(code, match.Index + match.Length);
            string condition;
            int end;
            if (match.Value == "do")
            {
                var bodyEnd = StatementEnd(code, after);
                var tail = SkipSpace(code, bodyEnd);
                if (!code.AsSpan(tail).StartsWith("while", StringComparison.Ordinal)) continue;
                doTails.Add(tail);
                var open = SkipSpace(code, tail + 5);
                if (open >= code.Length || code[open] != '(') continue;
                var close = MatchingClose(code, open);
                condition = code[(open + 1)..close];
                end = close + 1;
            }
            else
            {
                if (after >= code.Length || code[after] != '(') continue;
                var close = MatchingClose(code, after);
                var header = code[(after + 1)..close];
                if (match.Value == "for")
                {
                    var parts = SplitTopLevel(header, ';');
                    if (parts.Count != 3) continue;
                    condition = parts[1];
                }
                else condition = header;
                end = StatementEnd(code, SkipSpace(code, close + 1));
            }
            if (WallClockRead().IsMatch(condition) && !Await().IsMatch(code[match.Index..Math.Min(end, code.Length)])) yield return match.Index;
        }
    }

    // R4: now() - operand, unless the difference is a direct operand of <, <=, > or >=.
    private static IEnumerable<int> WallClockDifferences(string code)
    {
        foreach (Match read in WallClockRead().Matches(code))
        {
            var minus = SkipSpace(code, read.Index + read.Length);
            if (minus >= code.Length || code[minus] != '-' || (minus + 1 < code.Length && code[minus + 1] is '-' or '=')) continue;
            var position = OperandEnd(code, SkipSpace(code, minus + 1));
            while (true)
            {
                var operatorIndex = SkipSpace(code, position);
                if (operatorIndex < code.Length && code[operatorIndex] is '+' or '-' && !(operatorIndex + 1 < code.Length && code[operatorIndex + 1] is '+' or '-' or '='))
                    position = OperandEnd(code, SkipSpace(code, operatorIndex + 1));
                else break;
            }
            if (IsRelationalAfter(code, SkipSpace(code, position)) || IsRelationalBefore(code, read.Index)) continue;
            yield return read.Index;
        }
    }

    private static bool IsRelationalAfter(string code, int index)
    {
        if (index >= code.Length || code[index] is not ('<' or '>')) return false;
        var next = index + 1 < code.Length ? code[index + 1] : '\0';
        return next != code[index];
    }

    private static bool IsRelationalBefore(string code, int index)
    {
        var position = index - 1;
        while (position >= 0 && char.IsWhiteSpace(code[position])) position--;
        if (position < 0) return false;
        if (code[position] == '=') position--;
        if (position < 0 || code[position] is not ('<' or '>')) return false;
        var before = position > 0 ? code[position - 1] : '\0';
        return before != code[position] && before != '=';
    }

    // The end of one operand: a parenthesised expression, or a name/number followed by member, index and call suffixes.
    private static int OperandEnd(string code, int index)
    {
        if (index >= code.Length) return index;
        if (code[index] == '(') index = MatchingClose(code, index) + 1;
        else while (index < code.Length && (char.IsAsciiLetterOrDigit(code[index]) || code[index] is '_' or '$' or '.')) index++;
        while (index < code.Length)
        {
            if (code[index] is '(' or '[') index = MatchingClose(code, index) + 1;
            else if (code[index] == '.' || (code[index] == '?' && index + 1 < code.Length && code[index + 1] == '.'))
            {
                index += code[index] == '?' ? 2 : 1;
                while (index < code.Length && (char.IsAsciiLetterOrDigit(code[index]) || code[index] is '_' or '$')) index++;
            }
            else break;
        }
        return index;
    }

    private static int SkipSpace(string code, int index)
    {
        while (index < code.Length && char.IsWhiteSpace(code[index])) index++;
        return index;
    }

    /// <summary>The index of the bracket closing the one at <paramref name="open"/> (or the end of the text).</summary>
    private static int MatchingClose(string code, int open)
    {
        var depth = 0;
        for (var index = open; index < code.Length; index++)
        {
            if (code[index] is '(' or '[' or '{') depth++;
            else if (code[index] is ')' or ']' or '}' && --depth == 0) return index;
        }
        return code.Length;
    }

    /// <summary>The end (exclusive) of the statement starting at <paramref name="start"/>: a block, or up to its top-level ';'.</summary>
    private static int StatementEnd(string code, int start)
    {
        if (start >= code.Length) return code.Length;
        if (code[start] == '{') return MatchingClose(code, start) + 1;
        var depth = 0;
        for (var index = start; index < code.Length; index++)
        {
            if (code[index] is '(' or '[' or '{') depth++;
            else if (code[index] is ')' or ']' or '}') { if (--depth < 0) return index; }
            else if (code[index] == ';' && depth == 0) return index + 1;
        }
        return code.Length;
    }

    private static List<string> SplitTopLevel(string text, char separator)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] is '(' or '[' or '{') depth++;
            else if (text[index] is ')' or ']' or '}') depth--;
            else if (text[index] == separator && depth == 0) { parts.Add(text[start..index]); start = index + 1; }
        }
        parts.Add(text[start..]);
        return parts;
    }
}
