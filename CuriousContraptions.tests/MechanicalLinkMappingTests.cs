using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Xunit;
using Wire = CuriousContraptions.Wire;

namespace CuriousContraptions.Tests;

public sealed partial class MechanicalLinkPresentationTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task UnfamiliarMovingRimsUseIndependentSignedCoordinateMappings(int sign)
    {
        var motor = TestBundle.Entry(Motor);
        var source = motor with { Metadata = motor.Metadata with { Id = "mapped_shaft",
            Parameters = motor.Metadata.Parameters.Select(p => p.Key == "speed" ? p with { Minimum = -20f } : p).ToArray() },
            Ports = motor.Ports.Select(p => p.Id == PortId.Drive ? p with {
                MechanicalRim = new(new(1), new(.1f, .05f, .065f), Quaternion.Identity, .25f, -1.5f) } : p).ToArray() };
        var reverse = TestBundle.Entry(Reverse);
        var target = reverse with { Metadata = reverse.Metadata with { Id = "independent_shafts" },
            Mechanical = reverse.Mechanical! with { Transmissions = [] },
            Ports = reverse.Ports.Select(p => p.Id == PortId.DriveIn ? p with {
                Coordinate = new(CoordinateKind.Revolute, 1), CoordinatePerRadian = -2f,
                MechanicalRim = new(new(2), new(-.08f, .04f, .07f), Quaternion.Identity, .5f, 2.25f) } : p).ToArray() };
        Assert.Empty(BundleValidator.Validate([new("mapped source", source), new("independent target", target)]));
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        using var declarations = DeclaredPhysics.Scoped([source, target]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var construction = Construction(Quaternion.CreateFromYawPitchRoll(.3f, .4f, .2f), new(0f, 4f, 0f), new(target.Metadata.Id));
        construction = construction with { Instances = new(construction.Instances.Select(part => part.Id == new GpuBodyId(2)
            ? WorkshopInput.Generic(RuntimeTestServices.Bundle, new(source.Metadata.Id), part.Id, -5f, 4f, 0f, 0f, 0f, 0f, 1f,
                new InstanceParameterCollection([new(new("speed"), sign * 6f), new(new("torque"), 20f)])) : part).ToArray()) };
        await h.Require(WorkshopCommandKind.Construct, construction); await h.Run(); await h.Advance(30);
        var link = Assert.Single(h.Simulation.Scene.MechanicalLinks);
        var rows = h.Committed.Presentation.Actuators.ToArray();
        var sourceCoordinate = Assert.Single(rows, row => row.Drive.RevoluteJoint ==
            construction.Instances.Identities[new(2)].Hinges[0].Joint).Coordinate;
        var targetCoordinate = Assert.Single(rows, row => row.Drive.RevoluteJoint ==
            construction.Instances.Identities[new(3)].Hinges[1].Joint).Coordinate;
        Assert.Equal(construction.Instances.Identities[new(3)].Hinges[1].Joint, link.TargetRim.Joint);
        Assert.InRange(Assert.Single(rows, row => row.Drive.RevoluteJoint ==
            construction.Instances.Identities[new(3)].Hinges[0].Joint).Coordinate, -.01f, .01f); // Existing inactive-shaft envelope.
        Assert.True(MathF.Abs(sourceCoordinate) > .1f && MathF.Abs(targetCoordinate) > .1f);
        Assert.True(MathF.Abs(MathF.Abs(sourceCoordinate) - MathF.Abs(targetCoordinate)) > .1f,
            "The target witness must observe a different committed travel, so copying the source cannot satisfy its oracle.");
        // The declared port scale is2:1; .05rad is the existing one-second coupled-speed visibility envelope.
        Assert.InRange(2f * MathF.Abs(targetCoordinate) - MathF.Abs(sourceCoordinate), -.05f, .05f);
        var geometry = MechanicalLinkPresentation.Evaluate(link, h.Committed, h.Simulation.Scene.Physics);
        Vector3 Centre(GpuBodyId id, Vector3 offset)
        {
            Assert.True(h.Committed.Bodies.TryGet(id, out var body));
            var rotation = new Quaternion(body.Rotation.X, body.Rotation.Y, body.Rotation.Z, body.Rotation.W);
            Assert.True(MathF.Abs(rotation.Z) > .01f); // Actual moving support, not a static root stand-in.
            return new Vector3((body.Body.Cell.X + body.Body.Local.X) / 16f,
                (body.Body.Cell.Y + body.Body.Local.Y) / 16f, (body.Body.Cell.Z + body.Body.Local.Z) / 16f) + Vector3.Transform(offset, rotation);
        }
        var a = Centre(construction.Instances.Identities[new(2)].Hinges[0].Body, new(.1f, .05f, .065f));
        var b = Centre(construction.Instances.Identities[new(3)].Hinges[1].Body, new(-.08f, .04f, .07f));
        Assert.True(h.Committed.Bodies.TryGet(link.TargetRim.Support, out var targetBody));
        var normal = Vector3.Transform(Vector3.UnitZ, new Quaternion(targetBody.Rotation.X, targetBody.Rotation.Y,
            targetBody.Rotation.Z, targetBody.Rotation.W));
        var u = Vector3.Normalize(a - b - normal * Vector3.Dot(a - b, normal));
        var bm = b - .5f * Vector3.Cross(normal, u);
        var targetOrigin = Enumerable.Range(0, geometry.Points.Length).First(i => Vector3.Distance(geometry.Points[i], bm) < .01f);
        var lengths = new float[geometry.Points.Length];
        for (var i = 1; i < lengths.Length; i++) lengths[i] = lengths[i - 1] + Vector3.Distance(geometry.Points[i - 1], geometry.Points[i]);
        var expectedSource = OracleMarker(geometry, lengths, -.25f * -1.5f * sourceCoordinate);
        var expectedTarget = OracleMarker(geometry, lengths, lengths[targetOrigin] - .5f * 2.25f * targetCoordinate);
        Assert.InRange(Vector3.Distance(expectedSource, geometry.SourceMarker), 0f, .01f);
        Assert.InRange(Vector3.Distance(expectedTarget, geometry.TargetMarker), 0f, .01f);
        var held = MechanicalLinkPresentation.Evaluate(link, h.Committed, h.Simulation.Scene.Physics);
        Assert.Equal(geometry.SourceMarker, held.SourceMarker); Assert.Equal(geometry.TargetMarker, held.TargetMarker);
    }


    [Theory]
    [InlineData(.0625f, false)]
    [InlineData(16f, false)]
    [InlineData(16f, true)]
    public async Task CanonicalWorldExtremesAndCoincidentMovingRimsStayRepresentable(float radius, bool coincident)
    {
        await using var h = await WorkshopIntegrationHarness.Running(Construction(Quaternion.Identity, new(0f, 4f, 0f)));
        await h.Advance(12);
        var scene = h.Simulation.Scene;
        var original = Assert.Single(scene.MechanicalLinks);
        var sourceBody = h.Construction.Instances.Identities[new(2)].Hinges[0].Body;
        var targetBody = h.Construction.Instances.Identities[new(3)].Hinges[0].Body;
        var link = original with {
            Source = original.Source with { Support = sourceBody, Centre = new(16f), Radius = radius, AnglePerCoordinate = float.MaxValue },
            TargetRim = original.TargetRim with { Support = targetBody, Centre = coincident ? new(16f) : new(-16f),
                Radius = radius, AnglePerCoordinate = -float.MaxValue } };
        // Both declared extremes pass the actual compiled capability admission.
        _ = new WorkshopCompiledScene(scene.Identity, scene.Revision, scene.Settings, scene.Physics, scene.Activation,
            scene.Presentation, scene.Owners, scene.RunControls, [link]);
        foreach (var turn in new[] { 0f, .7f })
        {
            var q = Quaternion.CreateFromYawPitchRoll(turn, turn * .5f, turn * .3f);
            var bodies = Enumerable.Range(0, h.Committed.Bodies.Count).Select(i => h.Committed.Bodies[i]).Select(body =>
                body.Body.Id == sourceBody || body.Body.Id == targetBody ? body with {
                    Body = body.Body with { Cell = body.Body.Id == sourceBody || coincident ? new(1024, 1024, 1024) : new(-1024, -1024, -1024),
                        Local = default }, Rotation = new(q.X, q.Y, q.Z, q.W) } : body).ToArray();
            var read = h.Committed with { Bodies = new(bodies) }; // Canonical body validation admits these moving-support observations.
            var route = MechanicalLinkPresentation.Evaluate(link, read, scene.Physics);
            route.Validate();
            var expectedCentre = new Vector3(64f) + Vector3.Transform(new Vector3(16f), q);
            Assert.InRange(Vector3.Distance(route.Points[0], expectedCentre), radius - .01f, radius + .01f);
            Assert.InRange(route.Points.Length, 5, 4213); // Derived 2*2048 cubic segments +2*58 arcs +initial point.
            Assert.True(OnRoute(route.SourceMarker, route) && OnRoute(route.TargetMarker, route));
            var held = MechanicalLinkPresentation.Evaluate(link, read, scene.Physics);
            Assert.Equal(route.SourceMarker, held.SourceMarker); Assert.Equal(route.TargetMarker, held.TargetMarker);
        }
    }

    private static bool OnRoute(Vector3 marker, MechanicalLinkGeometry route) =>
        Enumerable.Range(1, route.Points.Length - 1).Any(i =>
        {
            var a = route.Points[i - 1]; var delta = route.Points[i] - a;
            var length = delta.LengthSquared();
            var t = length > 0f ? Math.Clamp(Vector3.Dot(marker - a, delta) / length, 0f, 1f) : 0f;
            return Vector3.Distance(marker, a + t * delta) <= .01f;
        });

    [Theory]
    [InlineData(float.MaxValue, .605f)]
    [InlineData(.03125f, .605f)]
    [InlineData(32f, .605f)]
    [InlineData(.3f, 17f)]
    public async Task InvalidCircularExtentRefusesAtBundleAndWireWithoutReplacingWorld(float radius, float centreZ)
    {
        var entry = TestBundle.Entry(Motor);
        var bad = entry with { Ports = entry.Ports.Select(port => port.MechanicalRim is { } rim
            ? port with { MechanicalRim = rim with { Radius = radius, Centre = new(0f, 0f, centreZ) } } : port).ToArray() };
        Assert.Contains(BundleValidator.Validate([new("invalid circular feature", bad)]), error => error.Code == BundleErrorCode.OutOfRange);
        await using var h = await WorkshopIntegrationHarness.Building(Construction(Quaternion.Identity, new(0f, 4f, 0f)));
        var installed = h.Simulation.Scene; var before = h.Committed;
        var command = h.Command(WorkshopCommandKind.Construct, h.Construction with { Revision = new(h.Construction.Revision.Value + 1) });
        var bytes = WorkshopWireFlatBuffers.EncodeCommand(command);
        var bb = new Google.FlatBuffers.ByteBuffer(bytes);
        var scene = CompiledSceneBoundaryTests.Child(new Google.FlatBuffers.Table(4 + bb.GetInt(4), bb), WireSchemaLayout.VtableOffset<Wire.WorkshopCommand>("scene"));
        var link = CompiledSceneBoundaryTests.Rows(scene, WireSchemaLayout.VtableOffset<Wire.CompiledSceneRecord>("mechanical_links"))[0];
        var rimTable = CompiledSceneBoundaryTests.Child(link, WireSchemaLayout.VtableOffset<Wire.MechanicalLinkPresentationRecord>("source"));
        System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(CompiledSceneBoundaryTests.Slot(rimTable, WireSchemaLayout.VtableOffset<Wire.MechanicalRimPresentationRecord>("radius"))), radius);
        var centreTable = CompiledSceneBoundaryTests.Child(rimTable, WireSchemaLayout.VtableOffset<Wire.MechanicalRimPresentationRecord>("centre"));
        System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(CompiledSceneBoundaryTests.Slot(centreTable, WireSchemaLayout.VtableOffset<Wire.CapabilityVector3Record>("z"))), centreZ);
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => h.Router.Dispatch(bytes)).Reason);
        Assert.Same(installed, h.Simulation.Scene);
        Assert.Equal(before.Epoch, h.Committed.Epoch); Assert.Equal(before.Tick, h.Committed.Tick);
        Assert.Equal(WorkshopCommandOutcome.Applied, (await h.Dispatch(command)).Result.Outcome);
    }

    private static Vector3 OracleMarker(MechanicalLinkGeometry geometry, float[] lengths, float distance)
    {
        distance %= lengths[^1]; if (distance < 0f) distance += lengths[^1];
        var i = Enumerable.Range(1, lengths.Length - 1).First(index => lengths[index] >= distance && lengths[index] > lengths[index - 1]);
        return geometry.Points[i - 1] + (geometry.Points[i] - geometry.Points[i - 1]) *
            ((distance - lengths[i - 1]) / (lengths[i] - lengths[i - 1]));
    }
}
