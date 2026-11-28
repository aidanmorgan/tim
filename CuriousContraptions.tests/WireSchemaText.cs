using System.Globalization;
using System.Text.RegularExpressions;

namespace CuriousContraptions.Tests;

/// <summary>The kinds of declaration the wire schema makes; each generates one C# type of its name.</summary>
internal enum WireDeclarationKind { Enum, Struct, Table }

/// <summary>What a field's type names: a scalar, <c>string</c>, or a declaration of the schema.</summary>
internal enum WireTypeKind { Scalar, String, Declared }

/// <summary>The admitted scalars, one member per width and signedness. Each schema spelling is converted once by the parser;
/// spellings of the same width (<c>ubyte</c>/<c>uint8</c>, <c>int</c>/<c>int32</c>, <c>uint</c>/<c>uint32</c>,
/// <c>ulong</c>/<c>uint64</c>) map to one member, because nothing reads the spelling.</summary>
internal enum WireScalar { Bool, UInt8, Int32, UInt32, Int64, UInt64, Float, Double }

/// <summary>A field's type: <c>Scalar</c> is set exactly when <c>Kind</c> is <see cref="WireTypeKind.Scalar"/>, and <c>Name</c>
/// (the declaration it names) exactly when <c>Kind</c> is <see cref="WireTypeKind.Declared"/>.</summary>
internal sealed record WireType(WireTypeKind Kind, WireScalar? Scalar, string? Name, bool Vector);

internal sealed record WireField(string Name, WireType Type, bool Required, int Line);

internal sealed record WireEnumMember(string Name, ulong Value);

internal sealed record WireDeclaration(WireDeclarationKind Kind, string Name, bool Authoring, int Line, WireScalar? Underlying,
    IReadOnlyList<WireField> Fields, IReadOnlyList<WireEnumMember> Members);

/// <summary>
/// S5c: the one reader of the wire schema file (<c>schemas/workshop_wire.fbs</c>). It accepts exactly the subset of the
/// FlatBuffers schema language that the file uses and refuses anything else with a <see cref="FormatException"/> naming the
/// construct and its line: <c>//</c> comments; exactly one <c>namespace CuriousContraptions.Wire;</c>, one
/// <c>attribute "authoring";</c>, one <c>root_type</c> and one <c>file_identifier</c>; enums of an integer scalar with decimal or
/// <c>0x</c> member values (an omitted value is the previous one plus one, the first 0); structs of scalar, struct or enum
/// fields; tables whose fields are scalars, <c>string</c>, declarations or vectors of one of those, with a decimal float or
/// <c>null</c> default and the <c>required</c> attribute; and the <c>authoring</c> mark on a struct or table. <c>include</c>,
/// <c>union</c>, <c>rpc_service</c>, <c>file_extension</c>, block comments, fixed arrays, any other attribute (<c>deprecated</c>,
/// <c>id:</c>, <c>key</c>, …) and a type name that resolves to no declaration are refused. Every structural reader of the
/// schema in the tests reads this model. Its contract is the grammar it accepts and the refusals it names, for any text,
/// synthetic texts included; a construct the pinned flatc itself refuses (a duplicate member, an out-of-range value) is
/// unspecified, because the build compiles the file before any test reads it.
/// </summary>
internal sealed class WireSchemaText
{
    public const string Namespace = "CuriousContraptions.Wire";
    public const string AuthoringAttribute = "authoring";

    /// <summary>The scalar spellings the file uses (closed to the measured inventory; another needs its own grammar slice), each
    /// with the <see cref="WireScalar"/> it is converted to.</summary>
    private static readonly IReadOnlyDictionary<string, WireScalar> ScalarSpellings = new Dictionary<string, WireScalar>(StringComparer.Ordinal)
    {
        ["bool"] = WireScalar.Bool, ["ubyte"] = WireScalar.UInt8, ["uint8"] = WireScalar.UInt8, ["int"] = WireScalar.Int32,
        ["int32"] = WireScalar.Int32, ["uint"] = WireScalar.UInt32, ["uint32"] = WireScalar.UInt32, ["int64"] = WireScalar.Int64,
        ["ulong"] = WireScalar.UInt64, ["uint64"] = WireScalar.UInt64, ["float"] = WireScalar.Float, ["double"] = WireScalar.Double,
    };

    private readonly Dictionary<string, WireDeclaration> _byName;

    private WireSchemaText(IReadOnlyList<WireDeclaration> declarations, string rootType, string fileIdentifier)
    {
        Declarations = declarations;
        RootType = rootType;
        FileIdentifier = fileIdentifier;
        _byName = declarations.ToDictionary(declaration => declaration.Name, StringComparer.Ordinal);
    }

    public IReadOnlyList<WireDeclaration> Declarations { get; }
    public string RootType { get; }
    public string FileIdentifier { get; }

    public WireDeclaration this[string name] => _byName[name];

    public bool Declares(string name) => _byName.ContainsKey(name);

    public static WireSchemaText Read(string path) => Parse(File.ReadAllText(path));

    public static WireSchemaText Parse(string text)
    {
        var parser = new Parser(Tokens(text), text.Count(character => character == '\n') + 1);
        var schema = parser.Schema();
        Resolve(schema);
        return schema;
    }

    private static FormatException Refused(int line, string message) => new($"line {line}: {message}");

    private static void Resolve(WireSchemaText schema)
    {
        foreach (var declaration in schema.Declarations)
        foreach (var field in declaration.Fields)
        {
            if (field.Type is not { Kind: WireTypeKind.Declared, Name: { } name }) continue;
            if (!schema.Declares(name))
                throw Refused(field.Line, $"the type '{name}' of {declaration.Name}.{field.Name} resolves to no declaration");
            if (declaration.Kind == WireDeclarationKind.Struct && schema[name].Kind == WireDeclarationKind.Table)
                throw Refused(field.Line, $"the struct field {declaration.Name}.{field.Name} names the table '{name}'");
        }
        if (!schema.Declares(schema.RootType) || schema[schema.RootType].Kind != WireDeclarationKind.Table)
            throw Refused(0, $"root_type '{schema.RootType}' names no table");
    }

    private sealed record Token(string Text, int Line, bool Quoted);

    private static readonly Regex Lexeme = new(@"\G(?:(?<space>[ \t\r]+)|(?<newline>\n)|(?<comment>//[^\n]*)|(?<block>/\*)|""(?<string>[^""\n]*)""|(?<word>[A-Za-z_][A-Za-z0-9_.]*)|(?<number>-?[0-9][A-Za-z0-9_.]*)|(?<punct>[{}()\[\]:;,=])|(?<other>.))");

    private static List<Token> Tokens(string text)
    {
        var tokens = new List<Token>();
        var line = 1;
        for (var match = Lexeme.Match(text); match.Success && match.Length > 0; match = match.NextMatch())
        {
            if (match.Groups["newline"].Success) line++;
            else if (match.Groups["block"].Success) throw Refused(line, "a block comment '/*' is not in the wire schema grammar");
            else if (match.Groups["other"].Success) throw Refused(line, $"the character '{match.Value}' is not in the wire schema grammar");
            else if (match.Groups["string"].Success) tokens.Add(new(match.Groups["string"].Value, line, true));
            else if (!match.Groups["space"].Success && !match.Groups["comment"].Success) tokens.Add(new(match.Value, line, false));
        }
        return tokens;
    }

    private sealed class Parser(List<Token> tokens, int lastLine)
    {
        private static readonly string[] RefusedKeywords = ["include", "union", "rpc_service", "file_extension"];
        private static readonly string[] OnceKeywords = ["namespace", "attribute", "root_type", "file_identifier"];
        private int _next;
        private readonly List<WireDeclaration> _declarations = [];
        private readonly Dictionary<string, string> _once = new(StringComparer.Ordinal);

        public WireSchemaText Schema()
        {
            while (_next < tokens.Count) TopLevel(Take());
            foreach (var keyword in OnceKeywords.Where(keyword => !_once.ContainsKey(keyword)))
                throw Refused(lastLine, $"the schema has no '{keyword}' declaration");
            var duplicate = _declarations.GroupBy(declaration => declaration.Name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
                throw Refused(duplicate.Last().Line, $"'{duplicate.Key}' is declared more than once");
            return new(_declarations, _once["root_type"], _once["file_identifier"]);
        }

        private void TopLevel(Token keyword)
        {
            if (keyword.Quoted || RefusedKeywords.Contains(keyword.Text, StringComparer.Ordinal))
                throw Refused(keyword.Line, $"'{keyword.Text}' is not in the wire schema grammar");
            switch (keyword.Text)
            {
                case "namespace": Once(keyword, Word(), Namespace); break;
                case "attribute": Once(keyword, Quoted(), AuthoringAttribute); break;
                case "root_type": Once(keyword, Word(), null); break;
                case "file_identifier": Once(keyword, FourCharacters(), null); break;
                case "enum": _declarations.Add(Enum(keyword.Line)); break;
                case "struct": _declarations.Add(Composite(WireDeclarationKind.Struct, keyword.Line)); break;
                case "table": _declarations.Add(Composite(WireDeclarationKind.Table, keyword.Line)); break;
                default: throw Refused(keyword.Line, $"'{keyword.Text}' is not in the wire schema grammar");
            }
        }

        private void Once(Token keyword, Token value, string? required)
        {
            if (_once.ContainsKey(keyword.Text))
                throw Refused(keyword.Line, $"a second '{keyword.Text}' declaration is not in the wire schema grammar");
            if (required is not null && value.Text != required)
                throw Refused(keyword.Line, $"'{keyword.Text} {value.Text}' is not in the wire schema grammar (only {required})");
            Expect(";");
            _once[keyword.Text] = value.Text;
        }

        private WireDeclaration Enum(int line)
        {
            var name = Word();
            Expect(":");
            var underlying = Word();
            if (!ScalarSpellings.TryGetValue(underlying.Text, out var scalar) || scalar is WireScalar.Bool or WireScalar.Float or WireScalar.Double)
                throw Refused(underlying.Line, $"the enum {name.Text} has the type '{underlying.Text}', which is not an integer scalar");
            if (Peek("(")) Attributes(null, "enum");
            Expect("{");
            var members = new List<WireEnumMember>();
            do
            {
                var member = Word();
                var value = Peek("=") ? EnumValue() : members.Count == 0 ? 0UL : members[^1].Value + 1;
                members.Add(new(member.Text, value));
            }
            while (Accept(","));
            Expect("}");
            return new(WireDeclarationKind.Enum, name.Text, false, line, scalar, [], members);
        }

        private ulong EnumValue()
        {
            Expect("=");
            var value = Take();
            if (Regex.IsMatch(value.Text, @"\A[0-9]+\z")) return ulong.Parse(value.Text, CultureInfo.InvariantCulture);
            if (Regex.IsMatch(value.Text, @"\A0x[0-9A-Fa-f]+\z")) return ulong.Parse(value.Text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            throw Refused(value.Line, $"the enum value '{value.Text}' is not a decimal or 0x integer");
        }

        private WireDeclaration Composite(WireDeclarationKind kind, int line)
        {
            var name = Word();
            var authoring = Peek("(") && Attributes(AuthoringAttribute, "declaration");
            Expect("{");
            var fields = new List<WireField>();
            while (!Accept("}")) fields.Add(Field(kind));
            return new(kind, name.Text, authoring, line, null, fields, []);
        }

        private WireField Field(WireDeclarationKind owner)
        {
            var name = Word();
            Expect(":");
            var type = Type();
            var table = owner == WireDeclarationKind.Table;
            if (!table && (type.Vector || type.Kind == WireTypeKind.String))
                throw Refused(name.Line, $"the struct field '{name.Text}' is a vector or a string");
            if (!table && Peek("=")) throw Refused(name.Line, $"the struct field '{name.Text}' has a default");
            if (Peek("=")) Default();
            var required = Peek("(") && Attributes(table ? "required" : null, "field");
            Expect(";");
            return new(name.Text, type, required, name.Line);
        }

        private WireType Type()
        {
            if (!Accept("[")) return Element(Word());
            var element = Element(Word());
            if (Peek(":")) throw Refused(tokens[_next].Line, "a fixed array '[T:n]' is not in the wire schema grammar");
            Expect("]");
            return element with { Vector = true };
        }

        private static WireType Element(Token name) =>
            ScalarSpellings.TryGetValue(name.Text, out var scalar) ? new(WireTypeKind.Scalar, scalar, null, false)
            : name.Text == "string" ? new(WireTypeKind.String, null, null, false)
            : new(WireTypeKind.Declared, null, name.Text, false);

        // A table field's default is checked against the grammar and not kept: no reader uses it.
        private void Default()
        {
            Expect("=");
            var value = Take();
            if (value.Text != "null" && !Regex.IsMatch(value.Text, @"\A-?[0-9]+\.[0-9]+\z"))
                throw Refused(value.Line, $"the default '{value.Text}' is not a decimal float or null");
        }

        // An attribute list holding exactly the one admitted attribute; anything else ('deprecated', 'id', 'key', ...) is refused by name.
        private bool Attributes(string? admitted, string place)
        {
            Expect("(");
            var attribute = Word();
            if (admitted is null || attribute.Text != admitted || !Peek(")"))
                throw Refused(attribute.Line, $"the {place} attribute '{attribute.Text}' is not in the wire schema grammar");
            Expect(")");
            return true;
        }

        private Token Take() => _next < tokens.Count ? tokens[_next++] : throw Refused(lastLine, "the schema ends inside a declaration");

        private bool Peek(string text) => _next < tokens.Count && !tokens[_next].Quoted && tokens[_next].Text == text;

        private bool Accept(string text)
        {
            if (!Peek(text)) return false;
            _next++;
            return true;
        }

        private void Expect(string text)
        {
            var token = Take();
            if (token.Quoted || token.Text != text) throw Refused(token.Line, $"expected '{text}' but found '{token.Text}'");
        }

        private Token Word()
        {
            var token = Take();
            if (token.Quoted || !Regex.IsMatch(token.Text, @"\A[A-Za-z_][A-Za-z0-9_.]*\z"))
                throw Refused(token.Line, $"expected a name but found '{token.Text}'");
            return token;
        }

        private Token Quoted()
        {
            var token = Take();
            return token.Quoted ? token : throw Refused(token.Line, $"expected a quoted string but found '{token.Text}'");
        }

        private Token FourCharacters()
        {
            var token = Quoted();
            return token.Text.Length == 4 ? token : throw Refused(token.Line, $"the file_identifier \"{token.Text}\" is not four characters");
        }
    }
}

/// <summary>S5c: a table field's position from its place in the declaration (field i is builder slot i and vtable offset
/// 4 + 2i). Fields are never reordered or removed, so the declaration order is the layout.</summary>
internal static class WireSchemaLayout
{
    private static readonly Lazy<WireSchemaText> Current = new(() => WireSchemaText.Read(
        Path.Combine(twodog.Engine.ResolveProjectDir(), "schemas", "workshop_wire.fbs")));

    public static int FieldCount<T>() => FieldCount(Current.Value, typeof(T).Name);
    public static int Slot<T>(string field) => Slot(Current.Value, typeof(T).Name, field);
    public static int VtableOffset<T>(string field) => VtableOffset(Slot<T>(field));

    public static int FieldCount(WireSchemaText schema, string table) => Table(schema, table).Fields.Count;

    /// <summary>The builder slot (field index) of a field.</summary>
    public static int Slot(WireSchemaText schema, string table, string field)
    {
        var fields = Table(schema, table).Fields;
        for (var index = 0; index < fields.Count; index++)
            if (fields[index].Name == field) return index;
        throw new ArgumentException($"The table {table} declares no field {field}.", nameof(field));
    }

    /// <summary>The vtable offset of a builder slot: the vtable's two header entries, then one 16-bit entry per field.</summary>
    public static int VtableOffset(int slot) => 4 + 2 * slot;

    private static WireDeclaration Table(WireSchemaText schema, string table) =>
        schema[table] is { Kind: WireDeclarationKind.Table } declaration ? declaration : throw new ArgumentException($"{table} is not a table.", nameof(table));
}
