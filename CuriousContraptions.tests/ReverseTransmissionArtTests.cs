using System;
using System.Linq;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;
using Godot;
using Xunit;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class ReverseTransmissionArtTests(NativeSceneFixture godot)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeSceneAppliesWheelPosesConveyorWitnessAndEvaluatedLinkArtwork(bool rotated)
    {
        var scene = new Workshop { Name = "Workshop" };
        scene.StartObservation = _ => Task.CompletedTask;
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        scene.ClientFactory = () => Task.FromResult<IWorkshopClient>(new WorkshopIntegrationClient(h));
        godot.Tree.Root.AddChild(scene);
        try
        {
            await scene.Startup;
            var q = rotated ? System.Numerics.Quaternion.CreateFromYawPitchRoll(.4f, .7f, .3f) : System.Numerics.Quaternion.Identity;
            var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
                new(PlacedParts.Battery(new(1), -10f, 4f, 0f),
                    WorkshopInput.Generic(RuntimeTestServices.Bundle, new("motor"), new(2), -7f, 4f, 0f, 0f, 0f, 0f, 1f),
                    WorkshopInput.Generic(RuntimeTestServices.Bundle, new("conveyor"), new(3), -3f, 4f, 0f, 0f, 0f, 0f, 1f),
                    WorkshopInput.Generic(RuntimeTestServices.Bundle, new("reverse_transmission"), new(4), 2f, 4f, 0f, q.X, q.Y, q.Z, q.W)),
                Connections: new(ReverseTransmissionTests.Power(2), ReverseTransmissionTests.Drive(2, 3), ReverseTransmissionTests.Drive(3, 4)));
            await scene.World.ReplaceConstruction(construction);
            void Frame() { scene.World._Process(0); scene._Process(0); }
            Frame();
            var reverse = scene.World.Parts.Single(part => part.AuthoredId == new GpuBodyId(4));
            var conveyor = scene.World.Parts.Single(part => part.AuthoredId == new GpuBodyId(3));
            var input = reverse.GetNode<Node3D>("Visual/InputWheel");
            var output = reverse.GetNode<Node3D>("Visual/OutputWheel");
            var pulley = conveyor.GetNode<Node3D>("Visual/PulleyOut");
            var authored = new[] { input.Transform, output.Transform, pulley.Transform };
            await scene.World.RunWorkshop(); await h.Advance(45); Frame();
            Assert.False(input.Transform.IsEqualApprox(authored[0]));
            Assert.False(output.Transform.IsEqualApprox(authored[1]));
            Assert.False(pulley.Transform.IsEqualApprox(authored[2]));
            var ids = construction.Instances.Identities[new(4)];
            foreach (var pair in new[] { (Node: input, Id: ids.Hinges[0].Body, Port: PortId.DriveIn), (Node: output, Id: ids.Hinges[1].Body, Port: PortId.Drive) })
            {
                Assert.True(h.Committed.Bodies.TryGet(pair.Id, out var body));
                var centre = new Vector3((body.Body.Cell.X + body.Body.Local.X) / 16f,
                    (body.Body.Cell.Y + body.Body.Local.Y) / 16f, (body.Body.Cell.Z + body.Body.Local.Z) / 16f);
                Assert.InRange(pair.Node.GlobalPosition.DistanceTo(centre), 0f, .01f);
                var expected = new Quaternion(body.Rotation.X, body.Rotation.Y, body.Rotation.Z, body.Rotation.W);
                var instance = construction.Instances.Single(part => part.Id == new GpuBodyId(4));
                var localSocket = scene.World.Registry.Ports.LocalPosition(instance.PartId, pair.Port);
                var socket = reverse.ToGlobal(new(localSocket.X, localSocket.Y, localSocket.Z));
                var axis = expected * Vector3.Back;
                Assert.InRange(pair.Node.GlobalBasis.Z.DistanceTo(axis), 0f, .01f);
                Assert.InRange(socket.DistanceTo(centre + axis * .08f), 0f, .01f);
                // A rim point must land within the same1cm artwork allowance as the contacted physical wheel.
                Assert.InRange((pair.Node.GlobalBasis.X * .32f).DistanceTo(expected * (Vector3.Right * .32f)), 0f, .01f);
            }
            var geometry = h.Presentation!.MechanicalGeometry;
            var reverseLink = h.Simulation.Scene.MechanicalLinks.ToArray().Single(link => link.TargetRim.CoordinateSupport == new GpuBodyId(4));
            var reverseRoute = geometry.Single(route => route.Target == reverseLink.Target);
            var rimCentre = input.GlobalPosition + input.GlobalBasis.Z * .07f;
            var upstreamRim = conveyor.ToGlobal(new(1.35f, -.07f, .78f));
            var toward = upstreamRim - rimCentre;
            toward = (toward - input.GlobalBasis.Z * toward.Dot(input.GlobalBasis.Z)).Normalized();
            var expectedAttachment = rimCentre - input.GlobalBasis.Z.Cross(toward) * .32f;
            Assert.Contains(reverseRoute.Points, point =>
                new Vector3(point.X, point.Y, point.Z).DistanceTo(expectedAttachment) <= .01f);
            // The rendered segments below must apply this same physically aligned attachment in both placements.
            var meshes = scene.GetNode<Node3D>("WorkshopConnections").GetChildren().OfType<MeshInstance3D>().Where(node => node.Visible).ToArray();
            var knots = meshes.Where(node => node.Mesh is SphereMesh).ToArray();
            Assert.Equal(geometry.Count * 2, knots.Length);
            var routes = meshes.Where(node => node.Mesh is CylinderMesh c && c.TopRadius == .025f).ToArray();
            Assert.Equal(geometry.Sum(route => route.Points.Length - 1), routes.Length);
            var offset = 0;
            for (var link = 0; link < geometry.Count; link++)
            {
                var route = geometry[link];
                var markers = new[] { route.SourceMarker, route.TargetMarker };
                for (var marker = 0; marker < 2; marker++)
                {
                    var expected = markers[marker];
                    Assert.InRange(knots[link * 2 + marker].Position.DistanceTo(new(expected.X, expected.Y, expected.Z)), 0f, .01f);
                    Assert.Equal(new Color("#f7cb52"), ((StandardMaterial3D)knots[link * 2 + marker].MaterialOverride).AlbedoColor);
                }
                for (var vertex = 1; vertex < route.Points.Length; vertex++)
                {
                    var node = routes[offset++];
                    var cylinder = (CylinderMesh)node.Mesh;
                    var half = node.Quaternion * (Vector3.Up * (cylinder.Height * .5f));
                    var a = route.Points[vertex - 1]; var b = route.Points[vertex];
                    Assert.InRange((node.Position - half).DistanceTo(new(a.X, a.Y, a.Z)), 0f, .01f);
                    Assert.InRange((node.Position + half).DistanceTo(new(b.X, b.Y, b.Z)), 0f, .01f);
                    Assert.Equal(new Color("#293954"), ((StandardMaterial3D)node.MaterialOverride).AlbedoColor);
                }
            }
            await scene.World.ResetWorkshop(); Frame();
            Assert.Equal(authored[0], input.Transform);
            Assert.Equal(authored[1], output.Transform);
            Assert.Equal(authored[2], pulley.Transform);
            Assert.Equal("Click a part with a DriveIn socket. Escape cancels.", Workshop.ConnectionPrompt(WorkshopConnectionDomain.Mechanical));
            Assert.Equal("Disconnect shaft", Workshop.DisconnectLabel(WorkshopConnectionDomain.Mechanical));
            await scene.ChangeConnectionAsync(ReverseTransmissionTests.Drive(3, 4), false);
            Assert.Equal("Shaft disconnected.", scene.FindChild("WorkshopStatus", true, false).Get("text").AsString());
            await scene.ChangeConnectionAsync(ReverseTransmissionTests.Drive(3, 4), true);
            Assert.Equal("Shaft connected.", scene.FindChild("WorkshopStatus", true, false).Get("text").AsString());
        }
        finally { scene.Free(); godot.Engine.Iteration(); }
        Assert.Empty(godot.Errors.Drain());
    }
}
