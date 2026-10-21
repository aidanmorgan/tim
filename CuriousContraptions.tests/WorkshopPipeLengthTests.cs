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
    private static WorkshopPipe Pipe(float length, ulong id = 1, double x = 0, double y = 3) =>
        WorkshopInput.Pipe(new(id), x, y, 0, 0, 0, 0, 1, new(length));

    // ---------- The declared parameter ----------

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(3.6f)]
    [InlineData(8f)]
    public void LengthsFromOneToEightMetresAreAdmitted(float metres) => new PipeLength(metres).Validate();

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
        Assert.Throws<ArgumentException>(() => new PipeLength(metres).Validate());
        Assert.Throws<ArgumentException>(() => WorkshopInput.Pipe(new(1), 0, 3, 0, 0, 0, 0, 1, new(metres)));
    }

    [Theory]
    [InlineData(.2f, 1f)]
    [InlineData(1f, 1f)]
    [InlineData(2f, 2f)]
    [InlineData(8f, 8f)]
    [InlineData(12f, 8f)]
    [InlineData(-3f, 1f)]
    public void PointerInputClampsFiniteLengths(float input, float committed) =>
        Assert.Equal(committed, PipeLength.FromInput(input).Metres);

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void PointerInputRejectsNonFiniteLengths(float input) =>
        Assert.Throws<ArgumentException>(() => PipeLength.FromInput(input));

    [Fact]
    public void ThePipeIsNeverAGenericInstance() =>
        Assert.Throws<ArgumentException>(() => WorkshopInput.Generic(PartId.Pipe, new(1), 0, 3, 0, 0, 0, 0, 1,
            WorkshopCanonicalColliders.Pipe(PipeLength.Default)));

    // ---------- Colliders, mouths and pick radius scale with length ----------

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(4.2f)]
    [InlineData(8f)]
    public void CollidersSpanTheLengthWithTheSame24Boxes(float metres)
    {
        var colliders = Pipe(metres).DeclaredColliders;
        Assert.Equal(24, colliders.Count);
        foreach (var shell in colliders.Take(16))
        {
            Assert.Equal((Half)(metres / 2), shell.HalfExtents.X);
            Assert.Equal((Half)0, shell.LocalPose.Translation.X);
        }
        var rims = colliders.Skip(16).ToArray();
        Assert.Equal(4, rims.Count(rim => rim.LocalPose.Translation.X == (Half)(-metres / 2)));
        Assert.Equal(4, rims.Count(rim => rim.LocalPose.Translation.X == (Half)(metres / 2)));
        // The bore stays 0.65 m whatever the length.
        foreach (var shell in colliders.Take(16))
        {
            var centre = MathF.Sqrt(MathF.Pow((float)shell.LocalPose.Translation.Y, 2) + MathF.Pow((float)shell.LocalPose.Translation.Z, 2));
            Assert.InRange(centre - (float)shell.HalfExtents.Y, .648f, .652f);
        }
    }

    /// <summary>The default length is the scene's 3.6 m tube: the same 24 box centres and extents. Every pipe now compiles the generated
    /// boxes (a loaded pipe always did); the scene's shell boxes declare the opposite rotation sign about X.</summary>
    [Fact]
    public void TheDefaultLengthHasTheSceneTubeShape()
    {
        var registry = new PartRegistry();
        registry.Load();
        var scene = registry.Definitions[PartId.Pipe].Colliders;
        var generated = WorkshopCanonicalColliders.Pipe(PipeLength.Default);
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
        var length = new PipeLength(metres);
        var start = TubeMouthSnap.LocalMouth(definitions[PartId.Pipe], TubeMouthEnd.Start, length);
        var end = TubeMouthSnap.LocalMouth(definitions[PartId.Pipe], TubeMouthEnd.End, length);
        Assert.Equal(new Vector3(-(metres / 2 + .09f), 0, 0), start);
        Assert.Equal(new Vector3(metres / 2 + .09f, 0, 0), end);

        // A placed pipe of this length joins a default mover exactly at its end collar face.
        IReadOnlyList<IWorkshopInstance> placed = [Pipe(metres)];
        var face = metres / 2 + .09f;
        var near = new Vector3(face + .1f + PipeLength.Default.MouthOffset, 3, 0);
        var snap = TubeMouthSnap.Find(definitions, placed, new(PartId.Pipe, new(2), false, PipeLength.Default), near, Quaternion.Identity);
        Assert.NotNull(snap);
        Assert.Equal(TubeMouthEnd.End, snap.Value.TargetMouth);
        Assert.True(snap.Value.Joint.DistanceTo(new Vector3(face, 3, 0)) <= 1e-5f, $"joint at {snap.Value.Joint}");
        Assert.True(snap.Value.Position.DistanceTo(new Vector3(face + PipeLength.Default.MouthOffset, 3, 0)) <= 1e-5f);

        // A mover of this length joins a default pipe with its own start collar face.
        IReadOnlyList<IWorkshopInstance> fixture = [Pipe(3.6f)];
        var moved = TubeMouthSnap.Find(definitions, fixture, new(PartId.Pipe, new(2), false, length),
            new(PipeLength.Default.MouthOffset + face + .1f, 3, 0), Quaternion.Identity);
        Assert.NotNull(moved);
        Assert.True(moved.Value.Position.DistanceTo(new Vector3(PipeLength.Default.MouthOffset + face, 3, 0)) <= 1e-5f,
            $"mover at {moved.Value.Position}");
    }

    [Fact]
    public void APipeMoverMustDeclareItsLength()
    {
        var registry = new PartRegistry();
        registry.Load();
        Assert.Throws<ArgumentException>(() => TubeMouthSnap.Find(registry.Definitions, [Pipe(2)], new(PartId.Pipe, new(2), false),
            new(3, 3, 0), Quaternion.Identity));
        Assert.Throws<ArgumentException>(() => TubeMouthSnap.Find(registry.Definitions, [Pipe(2)], new(PartId.PipeBend90, new(2), false, new(2)),
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
        var part = registry.Create(PartId.Pipe);
        try
        {
            godot.Tree.Root.AddChild(part);
            part.EnsureConstructed();
            Assert.Equal(PipeLength.Default, part.PipeLength);
            Assert.Equal(ResizeAxes.X, part.ResizableAxes);
            part.ApplyPipeLength(new(metres));
            Assert.Equal(new PipeLength(metres), part.PipeLength);
            Assert.Equal(metres, part.Dimensions.X);
            var radius = MathF.Sqrt(MathF.Pow(metres / 2 + .09f, 2) + .78f * .78f);
            Assert.Equal(radius, part.PickRadius, 1e-5f);
            var shell = (Node3D)part.FindChild("Visual_Shell", recursive: true, owned: false);
            var start = (Node3D)part.FindChild("Visual_Collar_Start", recursive: true, owned: false);
            var end = (Node3D)part.FindChild("Visual_Collar_End", recursive: true, owned: false);
            Assert.Equal(metres / 3.6f, shell.Scale.Y, 1e-5f); // the shell mesh's long axis is its local Y, turned onto X
            Assert.Equal(-metres / 2, start.Position.X, 1e-6f);
            Assert.Equal(metres / 2, end.Position.X, 1e-6f);
            Assert.Equal(Vector3.One, start.Scale); // the collars keep their width

            // The resize handle commits only the length, clamped at the pointer boundary.
            part.SetDimensions(new(12, 5, 5));
            Assert.Equal(new PipeLength(8), part.PipeLength);
            part.SetDimensions(new(.3f, 1, 1));
            Assert.Equal(new PipeLength(1), part.PipeLength);
            Assert.Throws<ArgumentException>(() => part.SetDimensions(new(float.NaN, 1, 1)));
            Assert.Equal(new PipeLength(1), part.PipeLength);
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
    public void CaptureKeepsTheResizedLengthThroughRotateAndMove()
    {
        var world = new MachineWorld();
        try
        {
            world.Registry.Load();
            var resized = Pipe(2, 4);
            typeof(MachineWorld).GetProperty(nameof(MachineWorld.Construction))!.SetValue(world,
                new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), new(resized)));
            var turned = Assert.IsType<WorkshopPipe>(world.CaptureInstance(PartId.Pipe, new(4), new(1, 4, 0),
                new Quaternion(Vector3.Back, Mathf.DegToRad(-45))));
            Assert.Equal(new PipeLength(2), turned.Length);
            var presented = Assert.IsType<WorkshopPipe>(world.CaptureInstance(PartId.Pipe, new(4), new(1, 4, 0), Quaternion.Identity,
                pipeLength: new(5)));
            Assert.Equal(new PipeLength(5), presented.Length);
            var placed = Assert.IsType<WorkshopPipe>(world.CaptureInstance(PartId.Pipe, new(5), new(1, 4, 0), Quaternion.Identity));
            Assert.Equal(PipeLength.Default, placed.Length);
        }
        finally { world.Free(); }
    }

    // ---------- Every codec carries the length ----------

    private static WorkshopConstruction Bench(float metres) =>
        new(new(1), WorkshopCadenceSettings.Default(), new(WorkshopInput.Basketball(new(1), 0, 6, 0, 0, 0, 0, 1), Pipe(metres, 2)));

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(3.6f)]
    [InlineData(8f)]
    public void TheSaveCodecRoundTripsTheLength(float metres)
    {
        var saved = new WorkshopSavedConstruction(Bench(metres), new(3));
        var bytes = WorkshopSaveCodec.Encode(saved);
        var decoded = WorkshopSaveCodec.Decode(bytes);
        Assert.Equal(saved, decoded);
        Assert.Equal(metres, Assert.IsType<WorkshopPipe>(decoded.Construction.Instances[1]).Length.Metres);
        Assert.Equal(bytes, WorkshopSaveCodec.Encode(decoded));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    [InlineData(8f)]
    public void TheCommandWireRoundTripsTheLength(float metres)
    {
        var construction = Bench(metres);
        var bytes = new byte[WorkshopWire.ConstructionBytes(construction)];
        WorkshopWire.WriteConstruction(bytes, construction);
        Assert.Equal(construction, WorkshopWire.ReadConstruction(bytes));
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1), construction, Session: new(1, 2),
            Cadence: new(1), Projection: new(1));
        Assert.Equal(construction, WorkshopWireFlatBuffers.DecodeCommand(WorkshopWireFlatBuffers.EncodeCommand(command)).Construction);
    }

    [Fact]
    public void TheCommandWireRejectsAPipeWithoutItsLength()
    {
        var construction = Bench(2);
        var bytes = new byte[WorkshopWire.ConstructionBytes(construction)];
        WorkshopWire.WriteConstruction(bytes, construction);
        var slot = bytes.AsSpan(WorkshopWire.ConstructionHeaderBytes + WorkshopWire.InstanceBytes, WorkshopWire.InstanceBytes);
        slot[112..116].Clear();
        Assert.Throws<ArgumentException>(() => WorkshopWire.ReadConstruction(bytes));
        WorkshopWire.WriteConstruction(bytes, construction);
        slot[116] = 1; // non-zero padding after the length
        Assert.Throws<ArgumentException>(() => WorkshopWire.ReadConstruction(bytes));
    }

    private static byte[] RawSave(WorkshopConstruction construction)
    {
        var builder = new Google.FlatBuffers.FlatBufferBuilder(512);
        var record = WorkshopWireFlatBuffers.EncodeConstructionRecord(builder, construction, validate: false);
        CuriousContraptions.Wire.SavedConstruction.StartSavedConstruction(builder);
        CuriousContraptions.Wire.SavedConstruction.AddMagic(builder, WorkshopSaveCodec.Magic);
        CuriousContraptions.Wire.SavedConstruction.AddNextBodyId(builder, 3);
        CuriousContraptions.Wire.SavedConstruction.AddConstruction(builder, record);
        builder.Finish(CuriousContraptions.Wire.SavedConstruction.EndSavedConstruction(builder).Value);
        return builder.SizedByteArray();
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
        var stored = bench with { Instances = new(bench.Instances[0], (WorkshopPipe)bench.Instances[1] with { Length = new(metres) }) };
        EditRefusal.Is(WorkshopEditRejection.UnsupportedSave, () => WorkshopSaveCodec.Decode(RawSave(stored)));
        // Control: the same hand-built record with an admitted length decodes.
        Assert.Equal(bench, WorkshopSaveCodec.Decode(RawSave(bench)).Construction);
    }
}
