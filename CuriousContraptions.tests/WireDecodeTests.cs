using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using CuriousContraptions.Wire;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

/// <summary>
/// S5c row S8: the verifier's derived rule refuses a field the schema does not define at every root, in a nested table no
/// hand-written count ever covered, and in a save, through the production decode boundaries, with the typed reason and the
/// root's identifier. Each refused buffer differs from an admitted twin only by its appended vtable entry; the twin sets its
/// table's last schema field, so the rule admits exactly the declared slots. Field counts and slots come from the schema
/// (WireSchemaLayout), never from a hand-written number.
/// </summary>
public sealed class WireDecodeTests
{
    private static readonly WireSchemaText Schema = WireSchemaText.Read(Path.Combine(twodog.Engine.ResolveProjectDir(), "schemas", "workshop_wire.fbs"));

    public enum AppendedFieldCase
    {
        CommandRoot, ResponseRoot, ObservationRoot, SaveRoot, NestedSceneRow, SaveInstanceRecord, ZeroEntryAtTheCommandRoot,
    }

    [Theory]
    [InlineData(AppendedFieldCase.CommandRoot)]
    [InlineData(AppendedFieldCase.ResponseRoot)]
    [InlineData(AppendedFieldCase.ObservationRoot)]
    [InlineData(AppendedFieldCase.SaveRoot)]
    [InlineData(AppendedFieldCase.NestedSceneRow)]
    [InlineData(AppendedFieldCase.SaveInstanceRecord)]
    [InlineData(AppendedFieldCase.ZeroEntryAtTheCommandRoot)]
    public void AnAppendedFieldIsRefusedAtEveryRootInANestedTableAndInASave(AppendedFieldCase which)
    {
        var root = RootOf(which);

        // Control: the twin with every declared slot (its last field set) and nothing beyond is admitted by the frame boundary.
        WorkshopWireFlatBuffers.OpenFrame(Buffer(which, appended: false), root, VerifierOf(root));

        var refused = Buffer(which, appended: true);
        if (root == WireRootIdentifier.SavedConstruction)
        {
            var refusal = SaveFixture.Refused(WorkshopSaveRejection.VerifierRejected, () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands,
                RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, refused));
            Assert.Contains(WorkshopWireFlatBuffers.FileIdentifier(root), refusal.Message, StringComparison.Ordinal);
            return;
        }
        var fault = Assert.Throws<WireFaultException>(() => Decode(root, refused));
        Assert.Equal(WireFaultReason.VerifierRejected, fault.Reason);
        Assert.Contains(WorkshopWireFlatBuffers.FileIdentifier(root), fault.Message, StringComparison.Ordinal);
    }

    private static WireRootIdentifier RootOf(AppendedFieldCase which) => which switch
    {
        AppendedFieldCase.CommandRoot or AppendedFieldCase.NestedSceneRow or AppendedFieldCase.ZeroEntryAtTheCommandRoot => WireRootIdentifier.WorkshopCommand,
        AppendedFieldCase.ResponseRoot => WireRootIdentifier.WorkshopResponse,
        AppendedFieldCase.ObservationRoot => WireRootIdentifier.WorkshopObservationBatch,
        _ => WireRootIdentifier.SavedConstruction,
    };

    private static VerifyTableAction VerifierOf(WireRootIdentifier root) => root switch
    {
        WireRootIdentifier.WorkshopCommand => WorkshopCommandVerify.Verify,
        WireRootIdentifier.WorkshopResponse => WorkshopResponseVerify.Verify,
        WireRootIdentifier.WorkshopObservationBatch => WorkshopObservationBatchVerify.Verify,
        _ => SavedConstructionVerify.Verify,
    };

    private static void Decode(WireRootIdentifier root, byte[] bytes)
    {
        switch (root)
        {
            case WireRootIdentifier.WorkshopCommand: WorkshopWireFlatBuffers.DecodeCommand(bytes); break;
            case WireRootIdentifier.WorkshopResponse: WorkshopWireFlatBuffers.DecodeResponse(bytes); break;
            default: WorkshopWireFlatBuffers.DecodeObservations(bytes); break;
        }
    }

    /// <summary>The case's buffer: its table with one entry appended beyond the schema, or the admitted twin without it. A
    /// root's table is named as its WireRootIdentifier member (schema decision D2).</summary>
    private static byte[] Buffer(AppendedFieldCase which, bool appended)
    {
        var root = RootOf(which);
        var rootTable = Enum.GetName(root)!;
        var builder = new FlatBufferBuilder(1024);
        var forge = new SchemaTableForge(Schema, builder);
        var extra = appended ? 1 : 0;
        int table = which switch
        {
            AppendedFieldCase.NestedSceneRow => forge.Table(rootTable, new() { ["scene"] = forge.Table("CompiledSceneRecord",
                new() { ["materials"] = forge.VectorOf(forge.Table("CapabilityContactMaterialDeclarationRecord", appended: extra, lastFieldSet: true)) }) }),
            AppendedFieldCase.SaveInstanceRecord => forge.Table(rootTable, new() { ["construction"] = forge.Table("ConstructionRecord",
                new() { ["instances"] = forge.VectorOf(forge.Table("InstanceRecord", appended: extra, lastFieldSet: true)) }) }),
            _ => forge.Table(rootTable, appended: extra, lastFieldSet: true),
        };
        builder.FinishSizePrefixed(table, WorkshopWireFlatBuffers.FileIdentifier(root));
        var bytes = builder.SizedByteArray();
        if (which == AppendedFieldCase.ZeroEntryAtTheCommandRoot && appended) ZeroLastVtableEntry(bytes);
        return bytes;
    }

    // The builder trims trailing zero vtable entries, so the zero entry is forged: the appended field's entry, the last one of
    // the root's vtable, is cleared, leaving a vtable one entry longer than the table with that entry absent.
    private static void ZeroLastVtableEntry(byte[] bytes)
    {
        var root = FlatBufferConstants.SizePrefixLength + BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(FlatBufferConstants.SizePrefixLength));
        var vtable = root - BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(root));
        var length = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(vtable));
        var commandFields = WireSchemaLayout.FieldCount(Schema, Enum.GetName(WireRootIdentifier.WorkshopCommand)!);
        Assert.Equal(WireSchemaLayout.VtableOffset(commandFields + 1), length);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(vtable + length - 2), 0);
    }
}

/// <summary>Builds tables of the schema with the raw FlatBuffers builder: every required field present (an empty vector, an
/// empty string, a minimal table, a zero struct written by the generated creator), the given offset fields, optionally the
/// last declared field (a scalar is written as 1), and optionally entries appended beyond the declared fields, the last of
/// them holding a value. Slots come from the declaration order.</summary>
internal sealed class SchemaTableForge(WireSchemaText schema, FlatBufferBuilder builder)
{
    public int Table(string name, Dictionary<string, int>? given = null, int appended = 0, bool lastFieldSet = false)
    {
        var fields = schema[name].Fields;
        var offsets = new Dictionary<int, int>();
        for (var slot = 0; slot < fields.Count; slot++)
        {
            if (given is not null && given.TryGetValue(fields[slot].Name, out var offset)) offsets[slot] = offset;
            else if (Wanted(fields, slot, lastFieldSet) && IsOffset(fields[slot].Type)) offsets[slot] = OffsetValue(fields[slot]);
        }
        builder.StartTable(fields.Count + appended);
        foreach (var (slot, offset) in offsets) builder.AddOffset(slot, offset, 0);
        for (var slot = 0; slot < fields.Count; slot++)
        {
            if (!Wanted(fields, slot, lastFieldSet) || offsets.ContainsKey(slot) || IsOffset(fields[slot].Type)) continue;
            if (StructName(fields[slot].Type) is { } structName) builder.AddStruct(slot, ZeroStruct(structName), 0);
            else AddOne(slot, fields[slot].Type);
        }
        if (appended > 0) builder.AddUint(fields.Count + appended - 1, 7, 0);
        return builder.EndTable();
    }

    public int VectorOf(int child)
    {
        builder.StartVector(sizeof(int), 1, sizeof(int));
        builder.AddOffset(child);
        return builder.EndVector().Value;
    }

    private static bool Wanted(IReadOnlyList<WireField> fields, int slot, bool lastFieldSet) =>
        fields[slot].Required || (lastFieldSet && slot == fields.Count - 1);

    private bool IsOffset(WireType type) => type.Vector || type.Kind == WireTypeKind.String ||
        (type is { Kind: WireTypeKind.Declared, Name: { } name } && schema[name].Kind == WireDeclarationKind.Table);

    private string? StructName(WireType type) =>
        type is { Vector: false, Kind: WireTypeKind.Declared, Name: { } name } && schema[name].Kind == WireDeclarationKind.Struct ? name : null;

    private int OffsetValue(WireField field)
    {
        if (field.Type.Vector)
        {
            builder.StartVector(sizeof(int), 0, sizeof(int));
            return builder.EndVector().Value;
        }
        return field.Type is { Kind: WireTypeKind.Declared, Name: { } name } ? Table(name) : builder.CreateString("").Value;
    }

    // A scalar or enum field written as 1, by the width its scalar (or the enum's underlying scalar) declares. Each WireScalar
    // member has its own case. The compiler does not check this statement for completeness; the last arm follows the
    // repository convention for a value outside the enum.
    private void AddOne(int slot, WireType type)
    {
        var scalar = type is { Kind: WireTypeKind.Declared, Name: { } name } ? schema[name].Underlying : type.Scalar;
        switch (scalar)
        {
            case WireScalar.Bool: builder.AddBool(slot, true, false); break;
            case WireScalar.UInt8: builder.AddByte(slot, 1, 0); break;
            case WireScalar.Float: builder.AddFloat(slot, 1, 0); break;
            case WireScalar.Double: builder.AddDouble(slot, 1, 0); break;
            case WireScalar.Int32 or WireScalar.UInt32: builder.AddUint(slot, 1, 0); break;
            case WireScalar.Int64 or WireScalar.UInt64: builder.AddUlong(slot, 1, 0); break;
            case null: throw new ArgumentException($"{type} is neither a scalar nor an enum.", nameof(type));
            default: throw new System.Diagnostics.UnreachableException($"The scalar {scalar} is not a WireScalar member.");
        }
    }

    // The generated creator writes the struct inline, immediately before the table records its slot.
    private int ZeroStruct(string name)
    {
        var type = typeof(WireRootIdentifier).Assembly.GetType($"{WireSchemaText.Namespace}.{name}", throwOnError: true)!;
        var create = type.GetMethod("Create" + name) ?? throw new MissingMethodException(type.FullName, "Create" + name);
        var arguments = create.GetParameters()
            .Select(parameter => parameter.ParameterType == typeof(FlatBufferBuilder) ? builder : Activator.CreateInstance(parameter.ParameterType)).ToArray();
        var offset = create.Invoke(null, arguments)!;
        return (int)offset.GetType().GetField("Value")!.GetValue(offset)!;
    }
}
