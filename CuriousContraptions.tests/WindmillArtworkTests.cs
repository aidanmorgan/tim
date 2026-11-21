using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using Godot;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class WindmillArtworkTests(NativeSceneFixture godot)
{
    [Fact]
    public async Task ResponseEditorRefusesInvalidValuesWithoutChangingUndoOrInstalledConstruction()
    {
        var scene = new Workshop { Name = "Workshop" };
        scene.StartObservation = _ => Task.CompletedTask;
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        scene.ClientFactory = () => Task.FromResult<IWorkshopClient>(new WorkshopIntegrationClient(h));
        godot.Tree.Root.AddChild(scene);
        try
        {
            await scene.Startup;
            await scene.World.ReplaceConstruction(TennisBallTests.Scene(WindmillIntegrationTests.Windmill()));
            var original = scene.World.Construction;
            await scene.ApplyDeclaredParametersAsync(new(2), new([new(new("radians_per_force"), 4)]));
            Assert.Equal(4, Assert.IsType<WorkshopGenericInstance>(scene.World.Construction.Instances.Single()).Parameters[new("radians_per_force")]);
            var accepted = scene.World.Construction;
            var acceptedRead = h.Committed;
            foreach (var value in new[] { .09f, 4.01f, float.NaN, float.PositiveInfinity })
            {
                await scene.ApplyDeclaredParametersAsync(new(2), new([new(new("radians_per_force"), value)]));
                Assert.Equal(accepted, scene.World.Construction);
                Assert.Equal(acceptedRead.Revision, h.Committed.Revision);
                Assert.NotEqual("Parameters updated.", scene.FindChild("WorkshopStatus", true, false).Get("text").AsString());
            }
            await scene.UndoAsync();
            Assert.Equal(original.Instances, scene.World.Construction.Instances);
            Assert.Equal("rad/s/N", Workshop.ParameterUnits(ParameterUnit.RadianPerSecondPerNewton));
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
        Assert.Empty(godot.Errors.Drain());
    }

    [Fact]
    public async Task NativeRotorAndOutputFollowCommittedStateAndResetAuthoredTransforms()
    {
        var scene = new Workshop { Name = "Workshop" };
        scene.StartObservation = _ => Task.CompletedTask;
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        scene.ClientFactory = () => Task.FromResult<IWorkshopClient>(new WorkshopIntegrationClient(h));
        godot.Tree.Root.AddChild(scene);
        try
        {
            await scene.Startup;
            await scene.World.ReplaceConstruction(TennisBallTests.Scene(FanIntegrationTests.Fan(), WindmillIntegrationTests.Windmill()));
            void Frame() { scene.World._Process(0); scene._Process(0); }
            Frame();
            var part = scene.World.Parts.Single(p => p.AuthoredId == new GpuBodyId(2));
            var rotor = part.GetNode<Node3D>("Visual/Rotor");
            var output = part.GetNode<Node3D>("Visual/Output");
            var blade = part.GetNode<MeshInstance3D>("Visual/Rotor/Arm0/Blade");
            var authoredRotor = rotor.Transform;
            var authoredOutput = output.Transform;
            var authoredBlade = blade.Transform;
            await scene.World.RunWorkshop(); await h.Advance(120); Frame();
            Assert.False(rotor.Transform.IsEqualApprox(authoredRotor));
            Assert.False(output.Transform.IsEqualApprox(authoredOutput));
            Assert.Equal(authoredBlade, blade.Transform);
            var rotorId = h.Construction.Instances.Identities[new(2)].Hinges[0].Body;
            Assert.True(h.Committed.Bodies.TryGet(rotorId, out var body));
            var expected = new Quaternion(body.Rotation.X, body.Rotation.Y, body.Rotation.Z, body.Rotation.W);
            Assert.InRange(rotor.GlobalBasis.Y.DistanceTo(expected * Vector3.Up), 0, .001f);
            var expectedOutput = new Basis(Vector3.Back, -WindmillIntegrationTests.Coordinate(h).Coordinate);
            Assert.InRange(output.Basis.X.DistanceTo(expectedOutput.X), 0, .001f);
            var guard = part.GetNode<MeshInstance3D>("Visual/Guard");
            Assert.NotNull(guard.Mesh);
            const string cream = "fff8e9";
            Assert.Equal(cream, Assert.IsType<StandardMaterial3D>(guard.MaterialOverride).AlbedoColor.ToHtml(false));
            var wheel = part.GetNode<MeshInstance3D>("Visual/Output/Wheel");
            Assert.Equal(cream, Assert.IsType<StandardMaterial3D>(wheel.Mesh.SurfaceGetMaterial(0)).AlbedoColor.ToHtml(false));
            Assert.Equal(4, Enumerable.Range(0, 4).Count(i =>
                part.GetNode<MeshInstance3D>($"Visual/Rotor/Arm{i}/Blade").Mesh is BoxMesh));
            await scene.World.ResetWorkshop(); Frame();
            Assert.Equal(authoredRotor, rotor.Transform);
            Assert.Equal(authoredOutput, output.Transform);
            Assert.Equal(authoredBlade, blade.Transform);
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
        Assert.Empty(godot.Errors.Drain());
    }
}
