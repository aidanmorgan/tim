using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

/// <summary>Owner decision, 21 Oct 2026 (Story 9R.7): the browser and its workers ship in one bundle, so the command wire, the
/// response wire, the cadence settings record and the save have one schema and no format version. Foreign or malformed bytes are
/// refused by structure (the FlatBuffers verifier, widths, padding and enum ranges) with the existing typed rejection.</summary>
public sealed class WorkshopFormatTests
{
    private static WorkshopSavedConstruction Saved() => new(new(new(1), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("ball"),new(1), 0, 6, 0, 0, 0, 0, 1), WorkshopInput.Generic(RuntimeTestServices.Bundle, new PartId("pipe"),new(2), 0, 3, 0, 0, 0, 0, 1, new InstanceParameterCollection([new(new("length"), 2)])))), new(3));

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
        var schema = WireSchemaText.Read(Path.Combine(twodog.Engine.ResolveProjectDir(), "schemas/workshop_wire.fbs"));
        foreach (var record in new[] { "CadenceSettingsRecord", "SavedConstruction", "WorkshopResponse", "WorkshopCommand" })
            Assert.DoesNotContain(schema[record].Fields, field => field.Name.Contains("version", StringComparison.Ordinal));
    }

    /// <summary>Story 9A.1 (with F.10 merged): no production source, script, schema or bundle tool names a format version. The scan
    /// reads every word containing "Version" or "VERSION" (or a word starting "version"; case-sensitive, so "conversion" is not a
    /// match), so a re-added version enum, constant such as STATE_VERSION, field, check or handshake fails here. The allowed words are the exact non-format uses: the .NET versioning attributes, the FlatBuffers
    /// runtime's own library guard, the browser's user-agent brand list, and three state generation stamps.</summary>
    [Fact]
    public void NoProductionSourceNamesAFormatVersion()
    {
        var root = twodog.Engine.ResolveProjectDir();
        var allowed = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["CuriousContraptions.Simulation/Program.cs"] = ["Versioning"],
            ["CuriousContraptions.Animation.Worker/Program.cs"] = ["Versioning"],
            ["engine/gpu/BrowserWorkshopObservation.cs"] = ["Versioning"],
            ["CuriousContraptions.web/wwwroot/native-clock.ts"] = ["fullVersionList", "version"],
            // A handle's slot generation, so a stale handle cannot address a reused slot.
            ["engine/presentation/AnimationBatch.cs"] = ["Version", "version", "_versions"],
            ["CuriousContraptions.Abstractions/AnimationDefinitions.cs"] = ["Version"],
            // Commit stamps: a prepared commit applies only if nothing committed since it was prepared.
            ["engine/gpu/WorkshopPoseHistory.cs"] = ["Version", "version", "_version"],
            ["engine/gpu/WorkshopClientCursor.cs"] = ["Version", "_version"],
        };
        var word = new Regex(@"\w*(?:Version|VERSION|(?<![A-Za-z0-9])version)\w*");
        var found = new List<string>();
        foreach (var path in ProductionSources(root))
        {
            var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
            var permitted = allowed.GetValueOrDefault(relative, []);
            foreach (Match match in word.Matches(File.ReadAllText(path)))
                if (!permitted.Contains(match.Value)) found.Add($"{relative}: {match.Value}");
        }
        Assert.Empty(found);
    }

    private static readonly string[] SourceTrees = ["engine", "ui", "elements", "CuriousContraptions.Abstractions",
        "CuriousContraptions.Abstractions.Electrical", "CuriousContraptions.Abstractions.Fluid", "CuriousContraptions.Abstractions.Mechanical",
        "CuriousContraptions.Abstractions.Signal"];
    private static readonly string[] SourceProjects = ["CuriousContraptions.Simulation", "CuriousContraptions.Animation.Worker",
        "tools/BundleElements", "tools/WireContract"];
    private static readonly string[] ScriptDirectories = ["CuriousContraptions.web/wwwroot", "CuriousContraptions.Simulation/wwwroot",
        "CuriousContraptions.Animation.Worker/wwwroot"];

    private static IEnumerable<string> ProductionSources(string root)
    {
        static bool Built(string path) => path.Contains("/bin/", StringComparison.Ordinal) || path.Contains("/obj/", StringComparison.Ordinal);
        IEnumerable<string> Files(string directory, string pattern, SearchOption option) =>
            Directory.EnumerateFiles(Path.Combine(root, directory), pattern, option).Where(path => !Built(path.Replace('\\', '/')));
        return SourceTrees.SelectMany(directory => Files(directory, "*.cs", SearchOption.AllDirectories))
            .Concat(SourceProjects.SelectMany(directory => Files(directory, "*.cs", SearchOption.TopDirectoryOnly)))
            .Concat(ScriptDirectories.SelectMany(directory => Files(directory, "*.ts", SearchOption.TopDirectoryOnly)))
            .Concat(ScriptDirectories.SelectMany(directory => Files(directory, "*.js", SearchOption.TopDirectoryOnly)).Where(path => !File.Exists(Path.ChangeExtension(path, ".ts"))))
            .Concat(Files("schemas", "*.fbs", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void ForeignFlatBuffersAreTheTypedSaveRefusal()
    {
        // A well-formed buffer of another root (a command) and a save whose construction table is absent.
        var command = WorkshopWireFlatBuffers.EncodeCommand(new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1),
            CompiledSceneFixture.Compile(Saved().Construction), Session: new(1, 2), Cadence: new(1), Projection: new(1)));
        SaveFixture.Refused(WorkshopSaveRejection.UnknownIdentifier, () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements,command));
        var builder = new Google.FlatBuffers.FlatBufferBuilder(64);
        CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
        CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, 3);
        builder.FinishSizePrefixed(CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder).Value,
            WorkshopWireFlatBuffers.FileIdentifier(CuriousContraptions.Wire.WireRootIdentifier.SavedConstruction));
        SaveFixture.Refused(WorkshopSaveRejection.MissingField, () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements,builder.SizedByteArray()));
        // Control: the same codec admits its own encoding.
        Assert.Equal(Saved(), WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements,WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, Saved())));
    }

    /// <summary>Only the verifier sees this: a part id string whose terminating zero is overwritten still reads as "pipe" through the
    /// length prefix, so a reader without verification would admit the record.</summary>
    [Fact]
    public void AnUnterminatedPartIdStringFailsTheVerifier()
    {
        var bytes = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, Saved());
        var name = "pipe\0"u8.ToArray();
        var at = bytes.AsSpan().IndexOf(name);
        Assert.True(at > 0, "the saved pipe's part id string");
        var corrupted = (byte[])bytes.Clone();
        corrupted[at + 4] = (byte)'x';
        SaveFixture.Refused(WorkshopSaveRejection.VerifierRejected, () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements,corrupted));
    }

    /// <summary>A slot written by the retired versioned schema (magic, version, next body id, construction; a seven-word settings
    /// record led by its version) is not a structurally valid save of the current schema: it is refused, never reinterpreted.</summary>
    [Fact]
    public void ASaveInTheRetiredVersionedLayoutIsTheTypedSaveRefusal()
    {
        var bytes = Convert.FromBase64String(File.ReadAllText(Path.Combine(twodog.Engine.ResolveProjectDir(),
            "CuriousContraptions.tests/Fixtures", "pre-versionless", "retired-versioned-construction.save.base64")));
        SaveFixture.Refused(WorkshopSaveRejection.VerifierRejected, () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements,bytes));
    }

    [Fact]
    public void AnOutOfBoundsTableOffsetFailsTheVerifier()
    {
        var bytes = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, Saved());
        var corrupted = (byte[])bytes.Clone();
        // After the 4-byte size prefix: the root table offset points past the buffer.
        BinaryPrimitives.WriteUInt32LittleEndian(corrupted.AsSpan(4), (uint)bytes.Length * 4);
        SaveFixture.Refused(WorkshopSaveRejection.VerifierRejected, () => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements,corrupted));
        Assert.Equal(WireFaultReason.UnknownIdentifier, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeCommand(corrupted)).Reason);
        Assert.Equal(WireFaultReason.UnknownIdentifier, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(corrupted)).Reason);
    }

    [Fact]
    public void TheCadenceRecordRejectsNonZeroPaddingWhereTheVersionWas()
    {
        var settings = new byte[WorkshopCadenceWire.ByteLength];
        WorkshopCadenceWire.Write(WorkshopCadenceSettings.Default(), settings);
        Assert.Equal(WorkshopCadenceSettings.Default(), WorkshopCadenceWire.Read(settings)); // control
        Assert.Equal((uint)WorkshopCadenceSettings.Default().Simulation, BinaryPrimitives.ReadUInt32LittleEndian(settings));
        settings[24] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopCadenceWire.Read(settings));
    }
}
