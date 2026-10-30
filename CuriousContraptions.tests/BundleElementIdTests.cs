using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CuriousContraptions.Gpu;
using CuriousContraptions.Wire;
using Google.FlatBuffers;
using WorkshopCommand = CuriousContraptions.Gpu.WorkshopCommand;

namespace CuriousContraptions.Tests;

/// <summary>Story 9A.15: saves and messages carry the bundle's string element id, and nothing numeric identifies an element.
/// A stored save is validated against the loaded bundle's elements at the save boundary; the worker's command decode refuses an id
/// it cannot decode. Each refusal is typed, names the id and keeps nothing of the refused input.</summary>
public sealed class BundleElementIdTests
{
    private static string Root => twodog.Engine.ResolveProjectDir();

    private static WorkshopConstruction Construction(params IWorkshopInstance[] instances) =>
        new(new(1), WorkshopCadenceSettings.Default(), new(instances));

    private static WorkshopSavedConstruction RampSave() =>
        new(Construction(WorkshopInput.Generic(new PartId("ramp"), new(1), 0, 1, 0, 0, 0, 0, 1)), new(2));

    /// <summary>A construction naming <paramref name="element"/>, encoded without domain validation (a tampered save or message).</summary>
    private static WorkshopConstruction Naming(string element) =>
        Construction(new WorkshopGenericInstance(new PartId(element), new(1), default, default, CanonicalRotation.Identity));

    private static WorkshopCommand Construct(WorkshopConstruction construction) => new(new(1), WorkshopCommandKind.Construct, new(1),
        default, CompiledSceneFixture.Compile(construction), Session: new(1, 2), Cadence: new(1), Projection: new(1));

    private static string[] SavedElementIds(byte[] save)
    {
        var construction = WorkshopSaveCodec.Open(save).Construction!.Value;
        return Enumerable.Range(0, construction.InstancesLength).Select(i => construction.Instances(i)!.Value.PartId).ToArray();
    }

    // ---------- AC1: each instance carries its typed PartId string, validated against the loaded bundle ----------

    [Fact]
    public void OpaqueIdentityDisplaysItsValueAndRefusesAnUninitializedIdentity()
    {
        Assert.Equal("custom-package", new PartId("custom-package").ToString());
        Assert.Throws<InvalidOperationException>(() => default(PartId).ToString());
    }

    [Fact]
    public void ASaveCarriesEachInstancesBundleElementIdString()
    {
        var save = new WorkshopSavedConstruction(Construction(WorkshopInput.Generic(new PartId("ball"),new(1), 0, 3, 0, 0, 0, 0, 1),
            WorkshopInput.Generic(new PartId("ramp"), new(2), 0, 1, 0, 0, 0, 0, 1), WorkshopInput.Generic(new PartId("funnel"), new(3), 3, 1, 0, 0, 0, 0, 1)), new(4));
        var bytes = WorkshopSaveCodec.Encode(save);
        Assert.Equal(["ball", "ramp", "funnel"], SavedElementIds(bytes));
        Assert.Equal(save.Construction.Instances, WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes).Construction.Instances);
    }

    [Fact]
    public void AWorkerMessageCarriesCompiledOwnersAndBodies()
    {
        var construction = Construction(WorkshopInput.Generic(new PartId("ball"),new(1), 0, 3, 0, 0, 0, 0, 1), WorkshopInput.Generic(new PartId("pipe_bend_90"), new(2), 3, 1, 0, 0, 0, 0, 1));
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(Construct(construction));
        var record = CommandRoot(bytes).Scene!.Value;
        Assert.Equal(2, record.OwnersLength);
        var scene = WorkshopWireFlatBuffers.DecodeCommand(bytes).Scene!;
        Assert.Equal(construction.Instances.Select(instance => instance.Id), scene.Owners);
        Assert.All(scene.Owners, owner => Assert.Contains(scene.Physics.Bodies.ToArray(), body => body.Id == owner));
    }

    private static Wire.WorkshopCommand CommandRoot(byte[] bytes) =>
        Wire.WorkshopCommand.GetRootAsWorkshopCommand(new ByteBuffer(bytes, Google.FlatBuffers.FlatBufferConstants.SizePrefixLength));

    /// <summary>The save boundary validates against the bundle it is handed, not a list of its own: a delivered element that the
    /// loaded bundle does not declare is refused, naming it, and the same save loads once the bundle declares it.</summary>
    [Fact]
    public void ASaveIsValidatedAgainstTheLoadedBundle()
    {
        var bytes = WorkshopSaveCodec.Encode(RampSave());
        var withoutRamp = SaveFixture.Elements.Where(id => id != new PartId("ramp")).ToHashSet();
        var refusal = SaveFixture.Refused(WorkshopSaveRejection.UnknownElement, () => WorkshopSaveCodec.Decode(withoutRamp, bytes));
        Assert.Contains("'ramp'", refusal.Message, StringComparison.Ordinal);
        Assert.Equal(RampSave().Construction.Instances, WorkshopSaveCodec.Decode(SaveFixture.Elements, bytes).Construction.Instances);
    }

    /// <summary>The loaded set is exactly the bundle's deliverable elements: every one of them is a known element at the worker's
    /// decode too, so no element the save boundary admits can escape as an unknown element after it (parity).</summary>
    [Fact]
    public void EveryLoadedElementIsKnownToTheWorkerDecode()
    {
        Assert.Equal(30, SaveFixture.Elements.Count);
        foreach (var element in SaveFixture.Elements)
        {
            var builder = new FlatBufferBuilder(256);
            var name = builder.CreateString(element.Value);
            InstanceRecord.StartInstanceRecord(builder);
            InstanceRecord.AddId(builder, 1);
            InstanceRecord.AddPartId(builder, name);
            InstanceRecord.AddCell(builder, Vec3i.CreateVec3i(builder, 0, 1, 0));
            InstanceRecord.AddLocal(builder, Vec3f.CreateVec3f(builder, 0, 0, 0));
            InstanceRecord.AddRotation(builder, Quat4f.CreateQuat4f(builder, 0, 0, 0, 1));
            builder.Finish(InstanceRecord.EndInstanceRecord(builder).Value);
            var record = InstanceRecord.GetRootAsInstanceRecord(builder.DataBuffer);
            // A parameter-less record may miss a required field; it must never be an unknown element.
            try { WorkshopAuthoringFlatBuffers.MapInstance(record); }
            catch (WireFaultException fault) { Assert.NotEqual(WireFaultReason.UnknownElement, fault.Reason); }
        }
    }

    // ---------- AC2: an unknown id is rejected atomically, naming the id ----------

    [Theory]
    [InlineData("kite")]
    [InlineData("tennis")]
    [InlineData("balloon")]
    public void ASaveNamingAnUnknownElementIsRefusedNamingIt(string element)
    {
        var refusal = SaveFixture.Refused(WorkshopSaveRejection.UnknownElement,
            () => WorkshopSaveCodec.Decode(SaveFixture.Elements, SaveFixture.Forged(Naming(element), 2)));
        Assert.Contains($"'{element}'", refusal.Message, StringComparison.Ordinal);
        Assert.StartsWith("This save cannot be loaded: ", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AStoredSaveNamingAnUnknownElementIsRefusedNamingIt()
    {
        var stored = Convert.ToBase64String(SaveFixture.Forged(Naming("kite"), 2));
        var refusal = SaveFixture.Refused(WorkshopSaveRejection.UnknownElement, () => WorkshopSaveCodec.DecodeStored(SaveFixture.Elements, stored));
        Assert.Contains("'kite'", refusal.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("kite")]
    [InlineData("tennis")]
    public void MainCompilationRefusesAnUnknownElementNamingIt(string element)
    {
        var fault = Assert.Throws<WorkshopEditRejectedException>(() =>
            WorkshopSceneCompiler.Compile(Naming(element), new(1, 2), new(1)));
        Assert.Equal(WorkshopEditRejection.UnsupportedPart, fault.Reason);
        Assert.Contains($"'{element}'", fault.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MainClientRefusesAnUnknownElementBeforeDispatch()
    {
        using var host = new WorkshopActivationAnimationTests.NativeClient();
        var port = host.Client.Observation;
        var before = port.Receipts(port.Session, default).Value!.Length;
        var fault = await Assert.ThrowsAsync<WorkshopEditRejectedException>(() =>
            host.Client.Execute(WorkshopCommandKind.Construct, Naming("kite")));
        Assert.Equal(WorkshopEditRejection.UnsupportedPart, fault.Reason);
        Assert.Equal(before, port.Receipts(port.Session, default).Value!.Length);
        await host.Construct(Naming("lamp"));
    }

    // ---------- AC3: no numeric part identity remains in code, schema or save ----------

    private static readonly string[] SourceRoots = ["engine", "ui", "elements", "schemas", "tools", "CuriousContraptions.Abstractions",
        "CuriousContraptions.Abstractions.Electrical", "CuriousContraptions.Abstractions.Fluid", "CuriousContraptions.Abstractions.Mechanical",
        "CuriousContraptions.Abstractions.Signal", "CuriousContraptions.Animation", "CuriousContraptions.Animation.Worker",
        "CuriousContraptions.Bundle", "CuriousContraptions.Simulation", "CuriousContraptions.web", "CuriousContraptions.tests"];

    private static readonly string[] SourceExtensions = [".cs", ".ts", ".js", ".mjs", ".fbs", ".csproj", ".json"];

    private static bool IsBuildOutput(string path) => path.Split(Path.DirectorySeparatorChar)
        .Any(segment => segment is "bin" or "obj" or "node_modules" or ".godot" or "AppBundle");

    /// <summary>The numeric switches are deleted (the names are split here so this file is no hit): a re-added conversion fails.</summary>
    [Fact]
    public void NoNumericPartIdConversionRemains()
    {
        var conversion = new Regex(@"\b(To|From)" + "Numeric" + @"Id\b");
        var hits = SourceRoots.Select(directory => Path.Combine(Root, directory)).Where(Directory.Exists)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            .Where(path => SourceExtensions.Contains(Path.GetExtension(path)) && !IsBuildOutput(Path.GetRelativePath(Root, path)))
            .Where(path => conversion.IsMatch(File.ReadAllText(path))).Select(path => Path.GetRelativePath(Root, path)).ToArray();
        Assert.Empty(hits);
        Assert.Null(typeof(PartId).GetMethod("To" + "NumericId"));
        Assert.Null(typeof(PartId).GetMethod("From" + "NumericId"));
    }

    /// <summary>Every schema field that names an element (part_id, inventory_part, any part or element field) is a string.</summary>
    [Fact]
    public void NoSchemaFieldCarriesANumericPartId()
    {
        var schema = File.ReadAllText(Path.Combine(Root, "schemas", "workshop_authoring.fbs"));
        var fields = Regex.Matches(schema, @"^\s*([a-z_][a-z0-9_]*)\s*:\s*([A-Za-z0-9_\[\]]+)\s*;", RegexOptions.Multiline)
            .Select(match => (Name: match.Groups[1].Value, Type: match.Groups[2].Value)).ToArray();
        var elementFields = fields.Where(field => Regex.IsMatch(field.Name, @"(^|_)(part|element)(_|$)")).ToArray();
        Assert.Equal(["part_id", "inventory_part"], elementFields.Select(field => field.Name));
        Assert.All(elementFields, field => Assert.Equal("string", field.Type));
    }
}
