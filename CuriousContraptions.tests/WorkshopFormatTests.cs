using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Owner decision, 21 Oct 2026 (Story 9R.7): the browser and its workers ship in one bundle, so the command wire, the
/// response wire, the cadence settings record and the save have one schema and no format version. Foreign or malformed bytes are
/// refused by structure (the FlatBuffers verifier, widths, padding and enum ranges) with the existing typed rejection.</summary>
public sealed class WorkshopFormatTests
{
    private static WorkshopSavedConstruction Saved() => new(new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Basketball(new(1), 0, 6, 0, 0, 0, 0, 1), WorkshopInput.Pipe(new(2), 0, 3, 0, 0, 0, 0, 1, new(2)))), new(3));

    [Fact]
    public void NoWireOrSaveRecordDeclaresAFormatVersion()
    {
        foreach (var record in new[] { typeof(CuriousContraptions.Wire.WorkshopCommand), typeof(CuriousContraptions.Wire.WorkshopResponse),
                     typeof(CuriousContraptions.Wire.SavedConstruction), typeof(CuriousContraptions.Wire.CadenceSettingsRecord) })
            // ValidateVersion is the FlatBuffers runtime's own library guard, not a format field.
            Assert.DoesNotContain(record.GetMembers(), member => member.Name is "Version" or "AddVersion");
        var engine = typeof(WorkshopSaveCodec).Assembly.GetTypes().Select(type => type.Name).ToHashSet();
        Assert.DoesNotContain("WorkshopWireVersion", engine);
        Assert.DoesNotContain("WorkshopSaveVersion", engine);
        Assert.DoesNotContain("WorkshopSettingsVersion", engine);
        var schema = File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "schemas/workshop_wire.fbs"));
        foreach (var table in new[] { "struct CadenceSettingsRecord", "table SavedConstruction", "table WorkshopResponse", "table WorkshopCommand" })
        {
            var start = schema.IndexOf(table, StringComparison.Ordinal);
            Assert.True(start >= 0, table);
            var body = schema[start..schema.IndexOf('}', start)];
            Assert.DoesNotContain("version", body, StringComparison.Ordinal);
        }
    }

    /// <summary>The exact field list of each versionless record: a re-added field (a version or anything else) fails here, whatever its
    /// name, and the generated slot numbering cannot drift from the schema unnoticed.</summary>
    [Fact]
    public void TheVersionlessRecordsDeclareExactlyTheirFields()
    {
        var schema = File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(), "schemas/workshop_wire.fbs"));
        string[] Fields(string header)
        {
            var start = schema.IndexOf(header + " {", StringComparison.Ordinal);
            Assert.True(start >= 0, header);
            var body = schema[(schema.IndexOf('{', start) + 1)..schema.IndexOf('}', start)];
            return body.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
                .Select(line => line[..line.IndexOf(':')].Trim()).ToArray();
        }
        Assert.Equal(["simulation", "physical", "animation", "presentation", "presentation_rate_numerator", "presentation_rate_denominator"],
            Fields("struct CadenceSettingsRecord"));
        Assert.Equal(["magic", "next_body_id", "construction"], Fields("table SavedConstruction"));
        Assert.Equal(["sequence", "kind", "outcome", "reason", "phase", "epoch", "tick", "revision", "publication", "master_generation",
            "cadence", "projection", "session", "capture", "bodies", "captures", "activations", "timers", "contact_works", "occurrences",
            "electrical", "activation_memory"], Fields("table WorkshopResponse"));
        Assert.Equal(["sequence", "kind", "epoch", "revision", "revision_kind", "session", "cadence", "projection", "electrical", "settings",
            "target", "construction", "construction_record"], Fields("table WorkshopCommand"));
        // The generated settings struct takes exactly the six words (builder plus six arguments).
        Assert.Equal(7, typeof(CuriousContraptions.Wire.CadenceSettingsRecord).GetMethod("CreateCadenceSettingsRecord")!.GetParameters().Length);
    }

    [Fact]
    public void ForeignFlatBuffersAreTheTypedSaveRefusal()
    {
        // A well-formed buffer of another root (a command) and a save whose construction table is absent.
        var command = WorkshopWireFlatBuffers.EncodeCommand(new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1),
            Saved().Construction, Session: new(1, 2), Cadence: new(1), Projection: new(1)));
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(command));
        var builder = new Google.FlatBuffers.FlatBufferBuilder(64);
        CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
        CuriousContraptions.Wire.SavedConstruction.AddMagic(builder, WorkshopSaveCodec.Magic);
        CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, 3);
        builder.Finish(CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder).Value);
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(builder.SizedByteArray()));
        // Control: the same codec admits its own encoding.
        Assert.Equal(Saved(), WorkshopSaveCodec.Decode(WorkshopSaveCodec.Encode(Saved())));
    }

    /// <summary>Only the verifier sees this: a part id string whose terminating zero is overwritten still reads as "pipe" through the
    /// length prefix, so a reader without verification would admit the record.</summary>
    [Fact]
    public void AnUnterminatedPartIdStringFailsTheVerifier()
    {
        var bytes = WorkshopSaveCodec.Encode(Saved());
        var name = "pipe\0"u8.ToArray();
        var at = bytes.AsSpan().IndexOf(name);
        Assert.True(at > 0, "the saved pipe's part id string");
        var corrupted = (byte[])bytes.Clone();
        corrupted[at + 4] = (byte)'x';
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(corrupted));
    }

    /// <summary>A slot written by the retired versioned schema (magic, version, next body id, construction; a seven-word settings
    /// record led by its version) is not a structurally valid save of the current schema: it is refused, never reinterpreted.</summary>
    [Fact]
    public void ASaveInTheRetiredVersionedLayoutIsTheTypedSaveRefusal()
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(256);
        var settings = WorkshopCadenceSettings.Default();
        var instances = CuriousContraptions.Wire.ConstructionRecord.CreateInstancesVector(builder, []);
        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, 1);
        builder.Prep(4, 28); // the retired settings struct: version, then the six cadence words
        builder.PutUint(settings.PresentationRate.Denominator); builder.PutUint(settings.PresentationRate.Numerator);
        builder.PutUint((uint)settings.Presentation); builder.PutUint((uint)settings.Animation);
        builder.PutUint((uint)settings.Physical); builder.PutUint((uint)settings.Simulation); builder.PutUint(1);
        builder.AddStruct(1, builder.Offset, 0);
        CuriousContraptions.Wire.ConstructionRecord.AddInstances(builder, instances);
        var record = CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder);
        builder.StartTable(4); // the retired SavedConstruction: magic, version, next body id, construction
        builder.AddUint(0, WorkshopSaveCodec.Magic, 0);
        builder.AddUint(1, 13, 0);
        builder.AddUlong(2, 1, 0);
        builder.AddOffset(3, record.Value, 0);
        builder.Finish(builder.EndTable());
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(builder.SizedByteArray()));
    }

    [Fact]
    public void AnOutOfBoundsTableOffsetFailsTheVerifier()
    {
        var bytes = WorkshopSaveCodec.Encode(Saved());
        var corrupted = (byte[])bytes.Clone();
        BinaryPrimitives.WriteUInt32LittleEndian(corrupted, (uint)bytes.Length * 4); // the root table offset points past the buffer
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(corrupted));
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.DecodeCommand(corrupted));
        Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.DecodeResponse(corrupted));
    }

    [Fact]
    public void TheBinaryHeadersRejectNonZeroPaddingWhereTheVersionWas()
    {
        var command = WorkshopWire.Encode(new WorkshopCommand(new(1), WorkshopCommandKind.Run, new(1), new(1), null,
            Session: new(1, 2), Cadence: new(1), Projection: new(1)));
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(command.AsSpan(12)));
        WorkshopWire.DecodeCommand(command); // control
        command[12] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeCommand(command));

        var settings = new byte[WorkshopCadenceWire.ByteLength];
        WorkshopCadenceWire.Write(WorkshopCadenceSettings.Default(), settings);
        Assert.Equal(WorkshopCadenceSettings.Default(), WorkshopCadenceWire.Read(settings)); // control
        Assert.Equal((uint)WorkshopCadenceSettings.Default().Simulation, BinaryPrimitives.ReadUInt32LittleEndian(settings));
        settings[24] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopCadenceWire.Read(settings));
    }
}
