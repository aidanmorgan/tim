using System.Buffers.Binary;
using CuriousContraptions.Wire;
using Google.FlatBuffers;

namespace CuriousContraptions.Tests;

/// <summary>
/// S5c: the vendored verifier's unknown-field rule, tested on <see cref="Verifier"/> directly with the generated Verify actions
/// of small schema tables and no codec. A table's vtable may cover at most its declared fields: a shorter vtable (trailing
/// absent fields) is admitted, one entry more is refused whether that entry is present or zero, at the root, in a child table,
/// through a vector of tables, and in a parent whose child was verified first (the record is kept per table). Slots and field
/// counts come from the schema (WireSchemaLayout). THIRD-PARTY-NOTICES.md records the patch; these tests fail if an upgrade
/// drops it.
/// </summary>
public sealed class VendoredFlatBuffersVerifierTests
{
    private static readonly WireSchemaText Schema = WireSchemaText.Read(Path.Combine(twodog.Engine.ResolveProjectDir(), "schemas", "workshop_wire.fbs"));
    private const string Material = "CapabilityContactMaterialDeclarationRecord";

    private static bool Verifies(byte[] bytes, VerifyTableAction verify) => new Verifier(new ByteBuffer(bytes)).VerifyBuffer(null, false, verify);

    // A material record holding its first field, or every field up to its last, with `appended` entries beyond them.
    private static byte[] MaterialRecord(bool lastFieldSet, int appended)
    {
        var builder = new FlatBufferBuilder(64);
        var fields = WireSchemaLayout.FieldCount(Schema, Material);
        builder.StartTable(fields + appended);
        builder.AddUlong(WireSchemaLayout.Slot(Schema, Material, "id"), 3, 0);
        if (lastFieldSet) builder.AddFloat(WireSchemaLayout.Slot(Schema, Material, "rolling_resistance"), 0.5f, 0);
        if (appended > 0) builder.AddUint(fields + appended - 1, 7, 0);
        builder.Finish(builder.EndTable());
        return builder.SizedByteArray();
    }

    [Fact]
    public void AVtableOfTheDeclaredFieldsOrFewerIsAdmitted()
    {
        var exact = MaterialRecord(lastFieldSet: true, appended: 0);
        Assert.Equal(WireSchemaLayout.VtableOffset(WireSchemaLayout.FieldCount(Schema, Material)), VtableLength(exact, Root(exact)));
        Assert.True(Verifies(exact, CapabilityContactMaterialDeclarationRecordVerify.Verify));
        // Trailing absent fields: the builder writes a vtable that ends at the first field.
        var shorter = MaterialRecord(lastFieldSet: false, appended: 0);
        Assert.Equal(WireSchemaLayout.VtableOffset(1), VtableLength(shorter, Root(shorter)));
        Assert.True(Verifies(shorter, CapabilityContactMaterialDeclarationRecordVerify.Verify));
    }

    [Fact]
    public void OneAppendedEntryIsRefusedWhetherPresentOrZero()
    {
        Assert.False(Verifies(MaterialRecord(lastFieldSet: true, appended: 1), CapabilityContactMaterialDeclarationRecordVerify.Verify));
        // The builder trims trailing zero entries, so the zero entry is forged by clearing the appended one.
        var zero = MaterialRecord(lastFieldSet: true, appended: 1);
        var root = Root(zero);
        Assert.Equal(WireSchemaLayout.VtableOffset(WireSchemaLayout.FieldCount(Schema, Material) + 1), VtableLength(zero, root));
        BinaryPrimitives.WriteUInt16LittleEndian(zero.AsSpan(Vtable(zero, root) + VtableLength(zero, root) - 2), 0);
        Assert.False(Verifies(zero, CapabilityContactMaterialDeclarationRecordVerify.Verify));
    }

    public enum NestingCase { ChildInsideAnExactParent, ParentAfterAVerifiedChild, TableInAVectorOfTables }

    [Theory]
    [InlineData(NestingCase.ChildInsideAnExactParent)]
    [InlineData(NestingCase.ParentAfterAVerifiedChild)]
    [InlineData(NestingCase.TableInAVectorOfTables)]
    public void AnAppendedEntryIsRefusedAtEveryDepth(NestingCase which)
    {
        Assert.True(Verifies(Nested(which, appended: 0), VerifyOf(which)));
        Assert.False(Verifies(Nested(which, appended: 1), VerifyOf(which)));
    }

    private static VerifyTableAction VerifyOf(NestingCase which) =>
        which == NestingCase.TableInAVectorOfTables ? ConstructionRecordVerify.Verify : SavedConstructionVerify.Verify;

    // The parent (SavedConstruction, two fields) declares fewer fields than its child (ConstructionRecord, six), so a single
    // record shared by both would let the child's highest slot admit the parent's appended one.
    private static byte[] Nested(NestingCase which, int appended)
    {
        var builder = new FlatBufferBuilder(256);
        var forge = new SchemaTableForge(Schema, builder);
        int root = which switch
        {
            NestingCase.ChildInsideAnExactParent => forge.Table("SavedConstruction",
                new() { ["construction"] = forge.Table("ConstructionRecord", appended: appended, lastFieldSet: true) }),
            NestingCase.ParentAfterAVerifiedChild => forge.Table("SavedConstruction",
                new() { ["construction"] = forge.Table("ConstructionRecord", lastFieldSet: true) }, appended),
            _ => forge.Table("ConstructionRecord", new() { ["instances"] = forge.VectorOf(forge.Table("InstanceRecord", appended: appended, lastFieldSet: true)) }),
        };
        builder.Finish(root);
        return builder.SizedByteArray();
    }

    // A Verifier reused after refusing a buffer admits a valid one. It does not detect a missing reset of the per-table record:
    // the frames a refusal leaves behind never reach a later verdict.
    [Fact]
    public void AReusedVerifierAdmitsAValidBufferAfterARefusal()
    {
        var verifier = new Verifier(new ByteBuffer(Nested(NestingCase.ChildInsideAnExactParent, appended: 1)));
        Assert.False(verifier.VerifyBuffer(null, false, SavedConstructionVerify.Verify));
        verifier.Buf = new ByteBuffer(Nested(NestingCase.ParentAfterAVerifiedChild, appended: 0));
        Assert.True(verifier.VerifyBuffer(null, false, SavedConstructionVerify.Verify));
    }

    private static int Root(byte[] bytes) => BinaryPrimitives.ReadInt32LittleEndian(bytes);

    private static int Vtable(byte[] bytes, int table) => table - BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(table));

    private static int VtableLength(byte[] bytes, int table) => BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(Vtable(bytes, table)));
}
