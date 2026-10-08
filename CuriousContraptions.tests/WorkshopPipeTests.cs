using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

public sealed class WorkshopPipeTests
{
    private static WorkshopPipe Pipe(PipeDimensions dimensions) =>
        WorkshopInput.Pipe(new(2), .125, 2, -.25, 0, 0, .382683432365, .923879532511, dimensions);
    private static WorkshopSavedConstruction Saved(PipeDimensions dimensions) =>
        new(new(new(1), WorkshopCadenceSettings.Default(), new(Pipe(dimensions))), new(3));

    [Fact]
    public void CanonicalLengthPoseIdentityAndFixedProfileRoundTrip()
    {
        foreach (var length in new[] { (Half)1, (Half)3.6, (Half)8 })
        {
            var dimensions = new PipeDimensions(new(length));
            var saved = Saved(dimensions);
            var bytes = WorkshopSaveCodec.Encode(saved);
            Assert.Equal(saved, WorkshopSaveCodec.Decode(bytes));
            Assert.Equal(bytes, WorkshopSaveCodec.Encode(WorkshopSaveCodec.Decode(bytes)));
            var profile = dimensions.Profile;
            profile.Validate();
            Assert.Equal((Half)(length * (Half).5), profile.HalfLength.Value);
            Assert.Equal((Half).65, profile.InnerRadius.Value);
            Assert.Equal((Half).70, profile.MiddleRadius.Value);
            Assert.Equal((Half).78, profile.EndRadius.Value);
            Assert.Equal((Half).09, profile.EndHalfWidth.Value);
        }
    }

    [Fact]
    public void CompilerRetainsOneExposedProfileAndSourceMaterialInFixedWidthAbi()
    {
        var scene = WorkshopPhysicsCompiler.Compile(Saved(PipeDimensions.Default).Construction, new(1, 2));
        var collider = Assert.Single(scene.Colliders.ToArray(), c => c.Shape == ColliderShapeKind.AnnularProfile);
        Assert.Equal(PipeDimensions.Default.Profile, collider.Profile);
        Assert.Equal(RigidLocalPose.Identity, collider.Pose);
        var material = Assert.Single(scene.Materials.ToArray(), m => m.Id == collider.Material);
        Assert.Equal(new Restitution((Half).15), material.Restitution);
        Assert.Equal(new LinearSpeed((Half).1), material.BounceThreshold);
        Assert.Equal(new FrictionCoefficient((Half).3), material.Friction);
        var bytes = PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));
        var index = Array.FindIndex(scene.Colliders.ToArray(), c => c.Id == collider.Id);
        var offset = PhysicsGpuAbi.CollidersOffset + index * PhysicsGpuAbi.ColliderBytes;
        var expected = new[] { collider.Profile.HalfLength.Value, collider.Profile.InnerRadius.Value,
            collider.Profile.MiddleRadius.Value, collider.Profile.EndRadius.Value, collider.Profile.EndHalfWidth.Value };
        for (var lane = 0; lane < expected.Length; lane++)
            Assert.Equal(BitConverter.HalfToUInt16Bits(expected[lane]), BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 48 + lane * 2)));
        Assert.All(bytes.AsSpan(offset + 58, 38).ToArray(), value => Assert.Equal((byte)0, value));
        Assert.Equal(96, PhysicsGpuAbi.ColliderBytes);
        Assert.Equal(19088, PhysicsGpuAbi.ByteLength);
        Assert.Throws<ArgumentException>(() => (collider with { Radius = new((Half).3) }).Validate());
        Assert.Throws<ArgumentException>(() => (collider with { Shape = ColliderShapeKind.Plane }).Validate());
        var old = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(old, 5);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(old, bytes, new(0)));
    }

    [Fact]
    public void MaximumAuthoredPopulationFitsExistingDeclarationCapacities()
    {
        var instances = new WorkshopInstances(
            WorkshopInput.Basketball(new(1), 0, 6, 0, 0, 0, 0, 1),
            WorkshopInput.Receiver(new(2), 3, 1, 0, 0, 0, 0, 1),
            WorkshopInput.Ramp(new(3), -3, 3, 0, 0, 0, 0, 1, RampDimensions.Default),
            WorkshopInput.Ramp(new(4), -1, 2, 0, 0, 0, 0, 1, RampDimensions.Default),
            WorkshopInput.Switch(new(5), 0, 1, 2, 0, 0, 0, 1, ContactTriggerSettings.Default),
            WorkshopInput.Lamp(new(6), 3, 1, 2, 0, 0, 0, 1),
            WorkshopInput.Wall(new(7), -4, 4, -2, 0, 0, 0, 1, WallDimensions.Maximum),
            Pipe(new(new((Half)8))) with { Id = new(8) });
        var scene = WorkshopPhysicsCompiler.Compile(new(new(1), WorkshopCadenceSettings.Default(), instances), new(1, 2));
        Assert.Equal(8, instances.Count); Assert.Equal(9, scene.Bodies.Length);
        Assert.Equal(14, scene.Colliders.Length); Assert.Equal(9, scene.Materials.Length);
        Assert.Equal(16, PhysicsSceneDeclaration.BodyCapacity); Assert.Equal(32, PhysicsSceneDeclaration.ColliderCapacity);
        Assert.Equal(16, PhysicsSceneDeclaration.MaterialCapacity);
    }

    [Fact]
    public void PointerLengthClampsBeforeHalfConversionAndFixedBoreCannotChange()
    {
        var bore = (float)PipeDimensions.BoreDiameter.Value;
        Assert.Equal((Half)8, PipeDimensions.FromInput(float.MaxValue, bore, bore).Length.Value);
        Assert.Equal((Half)1, PipeDimensions.FromInput(float.MinValue, bore, bore).Length.Value);
        foreach (var bad in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Assert.Throws<ArgumentException>(() => PipeDimensions.FromInput(bad, bore, bore));
            Assert.Throws<ArgumentException>(() => PipeDimensions.FromInput(3, bad, bore));
            Assert.Throws<ArgumentException>(() => PipeDimensions.FromInput(3, bore, bad));
        }
        Assert.Throws<ArgumentException>(() => PipeDimensions.FromInput(3, 2, bore));
        Assert.Throws<ArgumentException>(() => PipeDimensions.FromInput(3, bore, 2));
    }

    [Fact]
    public void SerializedLengthPaddingAndPreviousSchemasRejectAtomically()
    {
        var valid = WorkshopSaveCodec.Encode(Saved(PipeDimensions.Default));
        foreach (var value in new[] { (Half)0, (Half).5, (Half)9, Half.NaN, Half.PositiveInfinity })
        {
            var changed = (byte[])valid.Clone();
            BinaryPrimitives.WriteUInt16LittleEndian(changed.AsSpan(24 + WorkshopWire.ConstructionHeaderBytes + 104),
                BitConverter.HalfToUInt16Bits(value));
            Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(changed));
        }
        var padding = (byte[])valid.Clone(); padding[24 + WorkshopWire.ConstructionHeaderBytes + 106] = 1;
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(padding));
        var old = (byte[])valid.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(old.AsSpan(4), 4);
        Assert.Throws<ArgumentException>(() => WorkshopSaveCodec.Decode(old));
        var command = new WorkshopCommand(new(1), WorkshopCommandKind.Construct, new(1), new(1),
            Saved(PipeDimensions.Default).Construction, Session: new(1, 2), Cadence: new(1), Projection: new(1));
        var wire = WorkshopWire.Encode(command); BinaryPrimitives.WriteUInt32LittleEndian(wire.AsSpan(12), 11);
        Assert.Throws<ArgumentException>(() => WorkshopWire.DecodeCommand(wire));
        Assert.Equal(valid, WorkshopSaveCodec.Encode(Saved(PipeDimensions.Default)));
    }

    [Fact]
    public void SinglePipePopulationNoPortsAndForeignPuzzleReject()
    {
        var pipe = Pipe(PipeDimensions.Default);
        new WorkshopInstances(pipe).Validate();
        Assert.Throws<ArgumentException>(() => new WorkshopInstances(pipe, pipe with { Id = new(3) }).Validate());
        Assert.Empty(WorkshopPorts.For(WorkshopPartKind.Pipe).ToArray());
        Assert.False(WorkshopPorts.Has(WorkshopPartKind.Pipe, WorkshopSocket.ActivationIn,
            WorkshopConnectionDomain.Activation, WorkshopPortDirection.Input));
        var puzzle = FirstPrinciples.Create(new(1), WorkshopCadenceSettings.Default(), new(4), new(5), new((Half)1));
        Assert.Throws<ArgumentException>(() => puzzle.WithInstance(pipe).Validate());
        var invalid = Saved(PipeDimensions.Default).Construction.WithInstance(
            WorkshopInput.Lamp(new(3), 3, 2, 0, 0, 0, 0, 1)) with
        {
            Connections = new(new WorkshopConnection(pipe.Id, WorkshopSocket.ActivationOut, new(3),
                WorkshopSocket.ActivationIn, WorkshopConnectionDomain.Activation))
        };
        Assert.Throws<ArgumentException>(() => invalid.Validate());
    }

    [Fact]
    public void AnnularTopologyAndUniformProfileHaveOneUnambiguousDeclaration()
    {
        var profile = PipeDimensions.Default.Profile;
        (profile with { EndHalfWidth = default, EndRadius = profile.MiddleRadius }).Validate();
        foreach (var bad in new[]
        {
            profile with { InnerRadius = profile.MiddleRadius },
            profile with { EndRadius = profile.InnerRadius },
            profile with { EndHalfWidth = profile.HalfLength },
            profile with { EndHalfWidth = default },
            profile with { EndRadius = profile.MiddleRadius },
            profile with { HalfLength = new((Half)5) },
            profile with { InnerRadius = new(Half.NaN) }
        }) Assert.Throws<ArgumentException>(() => bad.Validate());
    }
}

[Collection<NativeSceneCollection>]
public sealed class WorkshopPipeResourceTests(NativeSceneFixture godot)
{
    [Fact]
    public void ActualResourceLocalLengthResizePreservesBorePoseAndRestoresArtwork()
    {
        var registry = new PartRegistry(); registry.Discover();
        var definition = registry.Definitions[WorkshopPartKind.Pipe];
        Assert.Empty(definition.Parameters);
        Assert.Equal(PipeDimensions.Default, definition.Pipe!.Capture());
        var pipe = Assert.IsType<PipePart>(registry.Create(WorkshopPartKind.Pipe));
        try
        {
            godot.Tree.Root.AddChild(pipe);
            pipe.Position = new(1, 3, -1); pipe.Quaternion = Quaternion.FromEuler(new(.2f, .4f, -.3f));
            var position = pipe.Position; var rotation = pipe.Quaternion;
            var initialBounds = PlacementShadows.ArtworkBounds(pipe);
            Assert.Equal(ResizeAxes.X, pipe.ResizableAxes);
            foreach (var length in new[] { (Half)1, (Half)8, (Half)3.6 })
            {
                var dimensions = new PipeDimensions(new(length));
                pipe.ApplyDimensions(dimensions);
                Assert.Equal(dimensions, pipe.CanonicalDimensions);
                Assert.Equal(new Vector3((float)length, (float)PipeDimensions.BoreDiameter.Value,
                    (float)PipeDimensions.BoreDiameter.Value), pipe.Dimensions);
                Assert.Equal(position, pipe.Position); Assert.Equal(rotation, pipe.Quaternion);
                var meshes = pipe.Visual.GetChildren().OfType<MeshInstance3D>().ToArray();
                Assert.Equal((float)length, meshes[0].Scale.X);
                Assert.Equal(-(float)dimensions.Profile.HalfLength.Value, meshes[1].Position.X);
                Assert.Equal((float)dimensions.Profile.HalfLength.Value, meshes[3].Position.X);
                var radius = MathF.Sqrt(MathF.Pow((float)dimensions.Profile.HalfLength.Value +
                    (float)dimensions.Profile.EndHalfWidth.Value, 2) + MathF.Pow((float)dimensions.Profile.EndRadius.Value, 2));
                var ring = pipe.GetChildren().OfType<MeshInstance3D>().Select(mesh => mesh.Mesh).OfType<TorusMesh>().Single();
                Assert.Equal(radius + .025f, ring.OuterRadius);
            }
            Assert.Equal(initialBounds, PlacementShadows.ArtworkBounds(pipe));
            var saved = pipe.CanonicalDimensions;
            Assert.Throws<ArgumentException>(() => pipe.SetDimensions(new(float.NaN, pipe.Dimensions.Y, pipe.Dimensions.Z)));
            Assert.Throws<ArgumentException>(() => pipe.SetDimensions(new(3, 2, 2)));
            Assert.Equal(saved, pipe.CanonicalDimensions);
            Assert.Equal(initialBounds, PlacementShadows.ArtworkBounds(pipe));
            pipe.SetDimensions(new(float.MaxValue, pipe.Dimensions.Y, pipe.Dimensions.Z));
            Assert.Equal((Half)8, pipe.CanonicalDimensions.Length.Value);
            pipe.ApplyDimensions(saved);
            Assert.Equal(initialBounds, PlacementShadows.ArtworkBounds(pipe));
        }
        finally { pipe.GetParent()?.RemoveChild(pipe); pipe.Free(); }
    }
}
