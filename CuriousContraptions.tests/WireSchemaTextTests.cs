using System.Reflection;
using CuriousContraptions.Wire;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

/// <summary>
/// S5c: the one schema reader (<see cref="WireSchemaText"/>) admits the real schema file and nothing outside its closed
/// grammar, and its model agrees with the code the pinned compiler generates from that file: the same declarations of the same
/// kinds, and every table's declared field count equal to the generated type's <c>Add</c> methods, so the slots
/// <see cref="WireSchemaLayout"/> derives from declaration order are the generated ones. Every expectation is derived from the
/// generated assembly or the model, never a hand-written count.
/// </summary>
public sealed class WireSchemaTextTests
{
    private static readonly string SchemaPath = Path.Combine(twodog.Engine.ResolveProjectDir(), "schemas", "workshop_wire.fbs");
    private static readonly Assembly Generated = typeof(WireRootIdentifier).Assembly;

    private const string Header = "namespace CuriousContraptions.Wire;\nattribute \"authoring\";\n";
    private const string Footer = "table Holder { value: uint32; }\nroot_type Holder;\nfile_identifier \"TEST\";\n";

    [Fact]
    public void EveryTableFieldCountEqualsItsGeneratedAddMethods()
    {
        var real = WireSchemaText.Read(SchemaPath);
        Assert.Empty(FieldCountViolations(real, GeneratedAddMethods));

        // Control: a schema text with one field more than its generated type is refused, naming the table.
        var fields = WireSchemaLayout.FieldCount(real, nameof(SavedConstruction));
        var extra = File.ReadAllText(SchemaPath).Replace("table SavedConstruction (authoring) {\n", "table SavedConstruction (authoring) {\n    s5c_extra: uint32;\n", StringComparison.Ordinal);
        Assert.Equal([$"SavedConstruction declares {fields + 1} fields, but its generated type has {fields} Add methods"],
            FieldCountViolations(WireSchemaText.Parse(extra), GeneratedAddMethods));
    }

    /// <summary>Every table whose declared field count differs from the number of generated <c>Add</c> methods.</summary>
    internal static List<string> FieldCountViolations(WireSchemaText schema, Func<string, int> generatedAddMethods) =>
        schema.Declarations.Where(declaration => declaration.Kind == WireDeclarationKind.Table)
            .Select(table => (table.Name, Declared: table.Fields.Count, Generated: generatedAddMethods(table.Name)))
            .Where(table => table.Declared != table.Generated)
            .Select(table => $"{table.Name} declares {table.Declared} fields, but its generated type has {table.Generated} Add methods").ToList();

    // The generated table type has one static Add<Field>(FlatBufferBuilder, value) per field and no other Add method.
    private static int GeneratedAddMethods(string table) =>
        (Generated.GetType($"{WireSchemaText.Namespace}.{table}") ?? throw new InvalidOperationException($"No generated type {table}."))
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Count(method => method.Name.StartsWith("Add", StringComparison.Ordinal) && method.GetParameters() is [{ ParameterType: var first }, _] && first == typeof(FlatBufferBuilder));

    // The kind the pinned compiler's output gives each schema type: an enum, a table (it has a Verify class) or a struct.
    private static WireDeclarationKind GeneratedKind(Type type) =>
        type.IsEnum ? WireDeclarationKind.Enum
        : Generated.GetType($"{type.FullName}Verify") is not null ? WireDeclarationKind.Table
        : WireDeclarationKind.Struct;

    [Fact]
    public void OnlyTheClosedGrammarIsAccepted()
    {
        // Boundary: the real file is admitted, and its model declares exactly the generated schema types, each of its generated kind.
        var real = WireSchemaText.Read(SchemaPath);
        var generated = Generated.GetTypes()
            .Where(type => type.Namespace == WireSchemaText.Namespace && !type.IsNested && !(type.IsAbstract && type.IsSealed && type.Name.EndsWith("Verify", StringComparison.Ordinal)))
            .ToDictionary(type => type.Name, GeneratedKind, StringComparer.Ordinal);
        Assert.Equal(generated.OrderBy(type => type.Key, StringComparer.Ordinal),
            real.Declarations.Select(declaration => KeyValuePair.Create(declaration.Name, declaration.Kind)).OrderBy(type => type.Key, StringComparer.Ordinal));
        Assert.Equal(nameof(SavedConstruction), real.RootType);
        Assert.Equal(CuriousContraptions.Gpu.WorkshopWireFlatBuffers.FileIdentifier(WireRootIdentifier.SavedConstruction), real.FileIdentifier);
        // The reader's member values (hexadecimal in the file) are the values the pinned compiler generated.
        Assert.Equal(Enum.GetValues<WireRootIdentifier>().Select(member => (Enum.GetName(member)!, (ulong)member)),
            real[nameof(WireRootIdentifier)].Members.Select(member => (member.Name, member.Value)));
        // The marked declarations are exactly the authored-construction and save records (S5c contract, "Authoring declarations").
        string[] authored = [nameof(CadenceSettingsRecord), nameof(ConnectionRecord), nameof(AssistanceKnotRecord), nameof(ParameterRecord),
            nameof(InstanceRecord), nameof(IntegrityGoalRecord), nameof(PuzzleRecord), nameof(ConstructionRecord), nameof(SavedConstruction)];
        Assert.Equal(authored.Order(StringComparer.Ordinal),
            real.Declarations.Where(declaration => declaration.Authoring).Select(declaration => declaration.Name).Order(StringComparer.Ordinal));

        // Boundary: an enum whose members have no values takes 0, 1, 2 in order, as the compiler assigns them.
        var implicitValues = WireSchemaText.Parse(Header + "enum Kind : uint8 { A, B, C }\n" + Footer);
        Assert.Equal([("A", 0UL), ("B", 1UL), ("C", 2UL)], implicitValues["Kind"].Members.Select(member => (member.Name, member.Value)));

        // Controls: each construct outside the grammar is refused, naming the construct and its line. One row per refusal the
        // contract's "One schema reader" bullets name (the parser review oracle).
        (string Construct, string Refusal)[] refused =
        [
            ("/* old */\n", "line 3: a block comment '/*' is not in the wire schema grammar"),
            ("include \"other.fbs\";\n", "line 3: 'include' is not in the wire schema grammar"),
            ("union Choice { Holder }\n", "line 3: 'union' is not in the wire schema grammar"),
            ("rpc_service Api { Call(Holder):Holder; }\n", "line 3: 'rpc_service' is not in the wire schema grammar"),
            ("file_extension \"bin\";\n", "line 3: 'file_extension' is not in the wire schema grammar"),
            ("table Old { value: uint32 (deprecated); }\n", "line 3: the field attribute 'deprecated' is not in the wire schema grammar"),
            ("table Ordered { value: uint32 (id: 0); }\n", "line 3: the field attribute 'id' is not in the wire schema grammar"),
            ("table Keyed { value: uint32 (key); }\n", "line 3: the field attribute 'key' is not in the wire schema grammar"),
            ("table Nested { bytes: [ubyte] (nested_flatbuffer: \"Holder\"); }\n", "line 3: the field attribute 'nested_flatbuffer' is not in the wire schema grammar"),
            ("struct Aligned (force_align: 16) { x: float; }\n", "line 3: the declaration attribute 'force_align' is not in the wire schema grammar"),
            ("enum Flags : uint8 (bit_flags) { A, B }\n", "line 3: the enum attribute 'bit_flags' is not in the wire schema grammar"),
            ("struct Fixed { values: [float:3]; }\n", "line 3: a fixed array '[T:n]' is not in the wire schema grammar"),
            ("namespace Other;\n", "line 3: a second 'namespace' declaration is not in the wire schema grammar"),
            // A scalar outside the list and an unresolved type name are one refusal: a name that is not an admitted scalar,
            // string or declaration resolves to no declaration. This row proves both.
            ("table Narrow { value: int16; }\n", "line 3: the type 'int16' of Narrow.value resolves to no declaration"),
            // An attribute on an enum: even the one declaration attribute, authoring, is refused there.
            ("enum Marked : uint8 (authoring) { A }\n", "line 3: the enum attribute 'authoring' is not in the wire schema grammar"),
            ("enum K : float { A }\n", "line 3: the enum K has the type 'float', which is not an integer scalar"),
            ("table Defaulted { value: uint32 = 5; }\n", "line 3: the default '5' is not a decimal float or null"),
        ];
        foreach (var (construct, refusal) in refused)
            Assert.Equal(refusal, Assert.Throws<FormatException>(() => WireSchemaText.Parse(Header + construct + Footer)).Message);
    }
}
