using System;
using System.Collections.Generic;
using System.Linq;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

/// <summary>Story 9R.7: the straight pipe's declared f32 `length` (CAT-048 §2: 1–8 m, default 3.6). Colliders, mouths, artwork and
/// the pick radius follow the length; every codec carries it, and a save without it is rejected, never defaulted.</summary>
[Collection<NativeSceneCollectionDefinition>]
public sealed class WorkshopPipeLengthTests(NativeSceneFixture godot)
{
    private static WorkshopGenericInstance Pipe(float length, ulong id = 1, double x = 0, double y = 3) =>
        WorkshopInput.Generic(new PartId("pipe"),new(id), (float)(x), (float)(y), 0, 0, 0, 0, 1, new InstanceParameterCollection([new(new("length"), length)]));

    // ---------- The declared parameter ----------

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(3.6f)]
    [InlineData(8f)]
    public void LengthsFromOneToEightMetresAreAdmitted(float metres) => Pipe(metres).Validate();

    [Theory]
    [InlineData(0f)]
    [InlineData(.999f)]
    [InlineData(8.001f)]
    [InlineData(9f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void LengthsOutsideOneToEightMetresAreRejected(float metres)
    {
        Assert.Throws<ArgumentException>(() => Pipe(metres).Validate());
        Assert.Throws<ArgumentException>(() => WorkshopInput.Generic(new PartId("pipe"),new(1), 0, 3, 0, 0, 0, 0, 1, new InstanceParameterCollection([new(new("length"), metres)])));
    }

    [Fact]
    public void ThePipeUsesItsDeclaredGenericParameters()
    {
        var instance = WorkshopInput.Generic(new PartId("pipe"), new(1), 0, 3, 0, 0, 0, 0, 1);
        Assert.Equal(3.6f, instance.Parameters[new("length")]);
    }

    // ---------- Colliders, mouths and pick radius scale with length ----------

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(4.2f)]
    [InlineData(8f)]
    public void TheShellTubeSpansTheLengthToBothMouthsAndTheCollarsFollowItsEnds(float metres)
    {
        // engine-hollow-shapes: the shell Tube reaches both mouths, L/2 + 0.09 either side of the centre; the collars sit at ±L/2.
        var colliders = Pipe(metres).DeclaredColliders;
        Assert.Equal(3, colliders.Count);
        var shell = colliders[0];
        Assert.Equal(ColliderShapeKind.Tube, shell.ShapeKind);
        Assert.Equal(.5f * metres + .09f, shell.HalfExtents.Y);
        Assert.Equal(0f, shell.LocalPose.Translation.X);
        Assert.Equal((-.5f * metres, .5f * metres), (colliders[1].LocalPose.Translation.X, colliders[2].LocalPose.Translation.X));
        // The bore stays 0.65 m whatever the length.
        Assert.InRange(shell.HalfExtents.X, .65f - 1e-6f, .65f + 1e-6f);
    }

    /// <summary>The default length is the scene's 3.6 m tube: the same hollow colliders' lanes and centres.</summary>
    [Fact]
    public void TheDefaultLengthHasTheSceneTubeShape()
    {
        var registry = new PartRegistry();
        registry.Load();
        var scene = registry.Definitions[new PartId("pipe")].Colliders;
        var generated = DeclaredPhysics.Colliders(new PartId("pipe"));
        Assert.Equal(scene.Count, generated.Count);
        for (var i = 0; i < scene.Count; i++)
        {
            Assert.Equal(scene[i].HalfExtents, generated[i].HalfExtents);
            Assert.Equal((float)scene[i].LocalPose.Translation.X, (float)generated[i].LocalPose.Translation.X, 1e-4f);
            Assert.Equal((float)scene[i].LocalPose.Translation.Y, (float)generated[i].LocalPose.Translation.Y, 1e-4f);
            Assert.Equal((float)scene[i].LocalPose.Translation.Z, (float)generated[i].LocalPose.Translation.Z, 1e-4f);
        }
        Assert.Equal(Pipe(2), Pipe(2)); // equal pipes are equal values (Undo and the fixture checks compare them)
        Assert.NotEqual(Pipe(2), Pipe(2.5f));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(3.6f)]
    [InlineData(8f)]
    public void MouthsSitAtTheCollarFacesOfTheLength(float metres)
    {
        var registry = new PartRegistry();
        registry.Load();
        var definitions = registry.Definitions;
        var length = new InstanceParameterCollection([new(new("length"), metres)]);
        var start = TubeMouthSnap.LocalMouth(definitions[new PartId("pipe")], TubeMouthEnd.Start, length);
        var end = TubeMouthSnap.LocalMouth(definitions[new PartId("pipe")], TubeMouthEnd.End, length);
        Assert.Equal(new Vector3(-(metres / 2 + .09f), 0, 0), start);
        Assert.Equal(new Vector3(metres / 2 + .09f, 0, 0), end);

        // A placed pipe of this length joins a default mover exactly at its end collar face.
        IReadOnlyList<IWorkshopInstance> placed = [Pipe(metres)];
        var face = metres / 2 + .09f;
        var near = new Vector3(face + .1f + 1.89f, 3, 0);
        var snap = TubeMouthSnap.Find(definitions, placed, new(new PartId("pipe"), new(2), false, InstanceParameterCollection.Defaults(new("pipe"))), near, Quaternion.Identity);
        Assert.NotNull(snap);
        Assert.Equal(TubeMouthEnd.End, snap.Value.TargetMouth);
        Assert.True(snap.Value.Joint.DistanceTo(new Vector3(face, 3, 0)) <= 1e-5f, $"joint at {snap.Value.Joint}");
        Assert.True(snap.Value.Position.DistanceTo(new Vector3(face + 1.89f, 3, 0)) <= 1e-5f);

        // A mover of this length joins a default pipe with its own start collar face.
        IReadOnlyList<IWorkshopInstance> fixture = [Pipe(3.6f)];
        var moved = TubeMouthSnap.Find(definitions, fixture, new(new PartId("pipe"), new(2), false, length),
            new(1.89f + face + .1f, 3, 0), Quaternion.Identity);
        Assert.NotNull(moved);
        Assert.True(moved.Value.Position.DistanceTo(new Vector3(1.89f + face, 3, 0)) <= 1e-5f,
            $"mover at {moved.Value.Position}");
    }

    [Fact]
    public void APipeMoverMustDeclareItsLength()
    {
        var registry = new PartRegistry();
        registry.Load();
        Assert.Throws<ArgumentException>(() => TubeMouthSnap.Find(registry.Definitions, [Pipe(2)], new(new PartId("pipe"), new(2), false, InstanceParameterCollection.Empty),
            new(3, 3, 0), Quaternion.Identity));
        Assert.Throws<ArgumentException>(() => TubeMouthSnap.Find(registry.Definitions, [Pipe(2)], new(new PartId("pipe_bend_90"), new(2), false, new InstanceParameterCollection([new(new("length"), 2f)])),
            new(3, 3, 0), Quaternion.Identity));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(3.6f)]
    [InlineData(8f)]
    public void ArtworkAndPickRadiusFollowTheLength(float metres)
    {
        var registry = new PartRegistry();
        registry.Load();
        var part = registry.Create(new PartId("pipe"));
        try
        {
            godot.Tree.Root.AddChild(part);
            part.EnsureConstructed();
            Assert.Equal(3.6f, part.Dimensions.X);
            Assert.Equal(ResizeAxes.X, part.ResizableAxes);
            part.SetDimensions(new(metres, part.Dimensions.Y, part.Dimensions.Z));
            Assert.Equal(metres, part.Dimensions.X);
            Assert.Equal(metres, part.Dimensions.X);
            var radius = MathF.Sqrt(MathF.Pow(metres / 2 + .09f, 2) + .78f * .78f);
            Assert.Equal(radius, part.PickRadius, 1e-5f);
            var shell = (Node3D)part.FindChild("Visual_Shell", recursive: true, owned: false);
            var start = (Node3D)part.FindChild("Visual_Collar_Start", recursive: true, owned: false);
            var end = (Node3D)part.FindChild("Visual_Collar_End", recursive: true, owned: false);
            // The shell mesh's long axis is its local Y, turned onto X: drawn 3.78 m at the default, it spans the length and both mouth
            // reaches (L + 0.18) at every length.
            Assert.Equal((metres + .18f) / 3.78f, shell.Scale.Y, 1e-5f);
            Assert.Equal(-metres / 2, start.Position.X, 1e-6f);
            Assert.Equal(metres / 2, end.Position.X, 1e-6f);
            Assert.Equal(Vector3.One, start.Scale); // the collars keep their width

            // The resize handle commits only the length, clamped at the pointer boundary.
            part.SetDimensions(new(12, 5, 5));
            Assert.Equal(8f, part.Dimensions.X);
            part.SetDimensions(new(.3f, 1, 1));
            Assert.Equal(1f, part.Dimensions.X);
            Assert.Throws<ArgumentException>(() => part.SetDimensions(new(float.NaN, 1, 1)));
            Assert.Equal(1f, part.Dimensions.X);
        }
        finally
        {
            part.GetParent()?.RemoveChild(part);
            part.Free();
        }
    }

    /// <summary>Every commit path captures the pipe it edits: the presented length when the part gives one, else the committed pipe's
    /// own length (a rotate, move or snap of a resized pipe never resets it to 3.6); only a new pipe starts at the default.</summary>
    [Fact]
    public async Task CaptureKeepsTheResizedLengthThroughRotateAndMove()
    {
        var world = new MachineWorld();
        try
        {
            world.Registry.Load();
            var resized = Pipe(2, 4);
            godot.Tree.Root.AddChild(world);
            await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
            await world.InitializeWorkshop(() => Task.FromResult<IWorkshopClient>(new WorkshopIntegrationClient(harness)));
            Assert.Equal(WorkshopCommandOutcome.Applied, (await world.ReplaceConstruction(
                new(new(2), WorkshopCadenceSettings.Default(), new(resized)))).Result.Outcome);
            var turned = Assert.IsType<WorkshopGenericInstance>(world.CaptureInstance(new PartId("pipe"), new(4), new(1, 4, 0),
                new Quaternion(Vector3.Back, Mathf.DegToRad(-45))));
            Assert.Equal(2f, turned.Parameters[new("length")]);
            var presented = Assert.IsType<WorkshopGenericInstance>(world.CaptureInstance(new PartId("pipe"), new(4), new(1, 4, 0), Quaternion.Identity,
                dimensions: new(5, 1.56f, 1.56f)));
            Assert.Equal(5f, presented.Parameters[new("length")]);
            var placed = Assert.IsType<WorkshopGenericInstance>(world.CaptureInstance(new PartId("pipe"), new(5), new(1, 4, 0), Quaternion.Identity));
            Assert.Equal(InstanceParameterCollection.Defaults(new("pipe")), placed.Parameters);
        }
        finally { world.Free(); }
    }

    // ---------- Every codec carries the length ----------

    private static WorkshopConstruction Bench(float metres) =>
        new(new(1), WorkshopCadenceSettings.Default(), new(WorkshopInput.Generic(new PartId("ball"),new(1), 0, 6, 0, 0, 0, 0, 1), Pipe(metres, 2)));

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(3.6f)]
    [InlineData(8f)]
    public void TheSaveCodecRoundTripsTheLength(float metres)
    {
        var saved = new WorkshopSavedConstruction(Bench(metres), new(3));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var decoded = WorkshopSaveCodec.Decode(SaveFixture.Elements,bytes);
        Assert.Equal(saved, decoded);
        Assert.Equal(metres, Assert.IsType<WorkshopGenericInstance>(decoded.Construction.Instances[1]).Parameters[new("length")]);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(8f)]
    public void TheCommandWireRoundTripsTheLength(float metres)
    {
        var construction = Bench(metres);
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), CompiledSceneFixture.Compile(construction), Session: new(1, 2),
            Cadence: new(1), Projection: new(1));
        var decoded = WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)).Scene!;
        Assert.Equal(construction.Revision, decoded.Revision);
        Assert.Equal(command.Scene!.Physics.Colliders.ToArray(), decoded.Physics.Colliders.ToArray());
    }

    [Fact]
    public void TheCommandWireRejectsAPipeWithoutItsLength()
    {
        var bench = Bench(2);
        var stored = bench with { Instances = new(bench.Instances[0], (WorkshopGenericInstance)bench.Instances[1] with { Parameters = InstanceParameterCollection.Empty }) };
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        builder.Finish(WorkshopAuthoringFlatBuffers.EncodeConstructionRecord(builder, stored, validate: false).Value);
        var record = CuriousContraptions.Wire.ConstructionRecord.GetRootAsConstructionRecord(new Google.FlatBuffers.ByteBuffer(builder.SizedByteArray()));
        // A submitted parameter vector must be complete; authoring defaults never repair saved data.
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopAuthoringFlatBuffers.DecodeConstructionRecord(record)).Reason);
    }

    private static byte[] RawSave(WorkshopConstruction construction)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var record = WorkshopAuthoringFlatBuffers.EncodeConstructionRecord(builder, construction, validate: false);
        return SaveFixture.Finish(builder, record, 3);
    }

    [Theory]
    [InlineData(0f)] // the FlatBuffers default: the field is absent from the record
    [InlineData(.5f)]
    [InlineData(9f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void ASaveWithoutAnAdmittedLengthIsRejectedNeverDefaulted(float metres)
    {
        var bench = Bench(2);
        var stored = bench with { Instances = new(bench.Instances[0], (WorkshopGenericInstance)bench.Instances[1] with { Parameters = new([new(new("length"), metres)]) }) };
        SaveFixture.Refused(float.IsFinite(metres) ? WorkshopSaveRejection.OutOfRange : WorkshopSaveRejection.NonFinite, () => WorkshopSaveCodec.Decode(SaveFixture.Elements,RawSave(stored)));
        // Control: the same hand-built record with an admitted length decodes.
        Assert.Equal(bench, WorkshopSaveCodec.Decode(SaveFixture.Elements,RawSave(bench)).Construction);
    }
}
