using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed partial class MechanicalLinkPresentationTests
{
    private static readonly PartId Motor = new("motor");
    private static readonly PartId Reverse = new("reverse_transmission");
    private static WorkshopConstruction Construction(Quaternion q, Vector3 at, PartId? target = null) =>
        new(new(2), WorkshopCadenceSettings.Default(),
            new(PlacedParts.Battery(new(1), -8f, 4f, 0f),
                WorkshopInput.Generic(RuntimeTestServices.Bundle, Motor, new(2), -5f, 4f, 0f, 0f, 0f, 0f, 1f),
                WorkshopInput.Generic(RuntimeTestServices.Bundle, target ?? Reverse, new(3), at.X, at.Y, at.Z, q.X, q.Y, q.Z, q.W)),
            Connections: new(ReverseTransmissionTests.Power(2), ReverseTransmissionTests.Drive(2, 3)));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task DeclaredRimsProduceClosedTangentRoutesAndCommittedMarkers(int placement)
    {
        var q = placement == 1 ? Quaternion.CreateFromYawPitchRoll(.4f, .7f, .3f) : Quaternion.Identity;
        // Case2 aligns the support centres along the source rim normal.
        var at = placement == 2 ? new Vector3(-4.64f, 4f, 3f) : new Vector3(0f, 4f, 0f);
        await using var h = await WorkshopIntegrationHarness.Running(Construction(q, at));
        await h.Advance(60);
        h.Presentation!.Advance(new PhysicalTime(240));
        var geometry = Assert.Single(h.Presentation.MechanicalGeometry);
        var link = Assert.Single(h.Simulation.Scene.MechanicalLinks);
        var a = new Vector3(-5f, 4f, .605f);
        var b = at + Vector3.Transform(new(-.36f, 0f, .39f), q);
        var towards = b - a; towards.Z = 0f;
        var u = towards.Length() > 0f ? Vector3.Normalize(towards) : Vector3.UnitX;
        var expectedStart = a + .3f * Vector3.Cross(Vector3.UnitZ, u);
        Assert.True(Vector3.Distance(expectedStart, geometry.Points[0]) <= .01f, "Strand must meet the declared visible rim.");
        Assert.True(Vector3.Distance(geometry.Points[0], geometry.Points[^1]) <= .01f);
        var distinct = geometry.Points.First(point => Vector3.Distance(point, geometry.Points[0]) > .01f);
        Assert.True(Vector3.Dot(distinct - geometry.Points[0], u) > 0f, "Connector must leave in the rim tangent direction.");
        Assert.All(geometry.Points, point => Assert.True(MechanicalRimDescriptor.Finite(point)));
        var normalB = Vector3.Transform(Vector3.UnitZ, q);
        var projectedB = a - b - normalB * Vector3.Dot(a - b, normalB);
        var radialB = projectedB.Length() > 0f ? Vector3.Normalize(projectedB) : Vector3.Transform(Vector3.UnitX, q);
        var transverseB = Vector3.Cross(normalB, radialB);
        var bm = b - .32f * transverseB; var bp = b + .32f * transverseB;
        var startWrap = Enumerable.Range(0, geometry.Points.Length).First(index => Vector3.Distance(geometry.Points[index], bm) <= .01f);
        var endWrap = Enumerable.Range(startWrap + 1, geometry.Points.Length - startWrap - 1)
            .First(index => Vector3.Distance(geometry.Points[index], bp) <= .01f);
        foreach (var point in geometry.Points.Skip(startWrap).Take(endWrap - startWrap + 1))
        {
            Assert.InRange(Vector3.Distance(point, b), .31f, .33f);
            Assert.InRange(Vector3.Dot(point - b, normalB), -.01f, .01f);
            Assert.True(Vector3.Dot(point - b, radialB) <= .01f, "The wrap follows the exposed outer semicircle.");
        }
        var coordinate = Assert.Single(h.Committed.Presentation.Actuators.ToArray(),
            row => row.Drive.RevoluteJoint == link.Source.Joint);
        Assert.True(MathF.Abs(coordinate.Coordinate) > .1f);
        Assert.True(Vector3.Distance(geometry.SourceMarker, geometry.Points[0]) > .01f, "Moving shaft must move its witness.");

        // Same committed read, later cosmetic evaluations: holding cannot integrate speed into invented travel.
        var held = geometry.SourceMarker;
        h.Presentation.Advance(new PhysicalTime(240));
        Assert.Equal(held, Assert.Single(h.Presentation.MechanicalGeometry).SourceMarker);
        await h.Require(WorkshopCommandKind.Reset);
        h.Presentation!.Advance(default(PhysicalTime));
        var reset = Assert.Single(h.Presentation.MechanicalGeometry);
        Assert.Equal(reset.Points[0], reset.SourceMarker);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RemovingCosmeticsKeepsTheSamePhysicalReversal(bool cosmetics)
    {
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var template = TestBundle.Entry(Reverse);
        var entry = template with { Metadata = template.Metadata with { Id = "unfamiliar_reverse" } };
        if (!cosmetics) entry = entry with { Ports = entry.Ports.Select(port => port with { MechanicalRim = null }).ToArray() };
        using var declarations = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        await h.Require(WorkshopCommandKind.Construct, Construction(Quaternion.Identity, new(0f, 4f, 0f), new(entry.Metadata.Id)));
        await h.Run(); await h.Advance(120);
        var ids = h.Construction.Instances.Identities[new(3)];
        var input = Assert.Single(h.Committed.Presentation.Actuators.ToArray(), row => row.Drive.RevoluteJoint == ids.Hinges[0].Joint);
        var output = Assert.Single(h.Committed.Presentation.Actuators.ToArray(), row => row.Drive.RevoluteJoint == ids.Hinges[1].Joint);
        Assert.True(input.Speed < -.1f);
        Assert.True(output.Speed > .1f);
        Assert.InRange(input.Speed + output.Speed, -.05f, .05f);
        h.Presentation!.Advance(new PhysicalTime(480));
        Assert.Equal(cosmetics ? 1 : 0, h.Presentation.MechanicalGeometry.Count);
    }

    public enum InvalidRim { ZeroRadius, NonfiniteRadius, MissingSupport, InvalidFrame, WrongDomain }
    [Theory]
    [InlineData(InvalidRim.ZeroRadius)]
    [InlineData(InvalidRim.NonfiniteRadius)]
    [InlineData(InvalidRim.MissingSupport)]
    [InlineData(InvalidRim.InvalidFrame)]
    [InlineData(InvalidRim.WrongDomain)]
    public void InvalidOptionalMetadataIsRefused(InvalidRim defect)
    {
        var entry = TestBundle.Entry(Motor);
        var ports = entry.Ports.ToArray();
        var index = Array.FindIndex(ports, port => port.Id == PortId.Drive);
        var rim = ports[index].MechanicalRim!.Value;
        rim = defect switch
        {
            InvalidRim.ZeroRadius => rim with { Radius = 0f },
            InvalidRim.NonfiniteRadius => rim with { Radius = float.NaN },
            InvalidRim.MissingSupport => rim with { Support = new(99) },
            InvalidRim.InvalidFrame => rim with { Frame = default },
            InvalidRim.WrongDomain => rim,
            _ => throw new ArgumentOutOfRangeException(nameof(defect))
        };
        ports[index] = ports[index] with { MechanicalRim = rim };
        if (defect == InvalidRim.WrongDomain) ports[0] = ports[0] with { MechanicalRim = rim };
        var errors = BundleValidator.Validate([new("probe", entry with { Ports = ports })]);
        Assert.Contains(errors, error => error.Code == BundleErrorCode.OutOfRange);
    }

    [Fact]
    public async Task GeometryCodecPreservesSemanticValuesAndRefusesMalformedSamples()
    {
        await using var h = await WorkshopIntegrationHarness.Running(Construction(Quaternion.Identity, new(0f, 4f, 0f)));
        await h.Advance(20);
        h.Presentation!.Advance(new PhysicalTime(80));
        var geometry = Assert.Single(h.Presentation.MechanicalGeometry);
        var sample = new MechanicalLinkSample(h.Committed.Epoch, h.Schedule.World.Epoch.Value, new(1), new(100), geometry);
        var codec = RuntimeTestServices.Core.Codecs.Animation;
        var bytes = codec.Geometry(h.Latest.Session, h.Latest.MasterGeneration, h.Latest.Cadence, sample);
        var decoded = codec.ReadGeometry(bytes, h.Latest.Session, h.Latest.MasterGeneration, h.Latest.Cadence);
        Assert.Equal(geometry.Target, decoded.Geometry.Target);
        Assert.Equal(geometry.Tick, decoded.Geometry.Tick);
        Assert.Equal(geometry.SourceMarker, decoded.Geometry.SourceMarker);
        Assert.Equal(geometry.TargetMarker, decoded.Geometry.TargetMarker);
        Assert.Equal(geometry.Points.Length, decoded.Geometry.Points.Length);
        Assert.Throws<ArgumentException>(() => codec.ReadGeometry(bytes.AsSpan(0, bytes.Length - 1), h.Latest.Session, h.Latest.MasterGeneration, h.Latest.Cadence));
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(128), float.NaN);
        Assert.Throws<ArgumentException>(() => codec.ReadGeometry(bytes, h.Latest.Session, h.Latest.MasterGeneration, h.Latest.Cadence));
    }
    public enum BadSample { Target, World, Epoch, Cadence, Pulse, FutureTime, FutureTick, Truncated }
    [Theory]
    [InlineData(BadSample.Target)]
    [InlineData(BadSample.World)]
    [InlineData(BadSample.Epoch)]
    [InlineData(BadSample.Cadence)]
    [InlineData(BadSample.Pulse)]
    [InlineData(BadSample.FutureTime)]
    [InlineData(BadSample.FutureTick)]
    [InlineData(BadSample.Truncated)]
    public async Task ClientRejectsInvalidGeometryAtomicallyAndRetiresItOnReset(BadSample defect)
    {
        using var session = new WorkshopActivationAnimationTests.NativeClient();
        await session.Construct(Construction(Quaternion.Identity, new(0f, 4f, 0f)));
        await session.Client.Execute(WorkshopCommandKind.Run);
        await session.Advance(12); session.Frame();
        var scene = session.InstalledScene;
        var route = MechanicalLinkPresentation.Evaluate(Assert.Single(scene.MechanicalLinks), session.Committed, scene.Physics);
        var schedule = session.Schedule;
        var valid = new MechanicalLinkSample(session.Committed.Epoch, schedule.World.Epoch.Value,
            new(6), new(100_000_000), route);
        var codec = RuntimeTestServices.Core.Codecs.Animation;
        byte[] Encode(MechanicalLinkSample sample, CadenceRevision? cadence = null) =>
            codec.Geometry(new(101, 206), new(1), cadence ?? schedule.Revision, sample);
        session.Client.ReceiveAnimation(Encode(valid));
        var physical = new WorkshopPresentationSample(default, new(100_000_000),
            new(new(session.Committed.Tick.Value * schedule.Settings.PhysicalStepsPerCommit)), PresentationQuality.Terminal);
        var admitted = Assert.Single(session.Client.MechanicalFrames(session.FrameNumber, physical));
        Assert.Equal(route.Target, admitted.Target); Assert.Equal(route.Tick, admitted.Tick);
        Assert.Equal(route.SourceMarker, admitted.SourceMarker); Assert.Equal(route.TargetMarker, admitted.TargetMarker);
        Assert.Equal(route.Points.ToArray(), admitted.Points.ToArray());
        var next = valid with { Pulse = new(7), AppliedAt = new(116_666_667) };
        var invalid = defect switch
        {
            BadSample.Target => next with { Geometry = route with { Target = new(route.Target.Value + 999) } },
            BadSample.World => next with { World = new(valid.World.Value + 1) },
            BadSample.Epoch => next with { Generation = valid.Generation + 1 },
            BadSample.FutureTime => next with { AppliedAt = new(1_000_000_000), Pulse = new(60) },
            BadSample.FutureTick => next with { Geometry = route with { Tick = new(999) }, Pulse = new(7) },
            BadSample.Pulse => valid,
            _ => next
        };
        var bytes = Encode(invalid, defect == BadSample.Cadence ? new(schedule.Revision.Value + 1) : null);
        if (defect == BadSample.Truncated) bytes = bytes[..^1];
        Assert.Throws<ArgumentException>(() => session.Client.ReceiveAnimation(bytes));
        Assert.Same(admitted, Assert.Single(session.Client.MechanicalFrames(session.FrameNumber, physical)));
        await session.Client.Execute(WorkshopCommandKind.Reset);
        session.Frame();
        Assert.Empty(session.Client.MechanicalFrames(session.FrameNumber, physical));
        session.Client.ReceiveAnimation(Encode(valid));
        Assert.Empty(session.Client.MechanicalFrames(session.FrameNumber, physical));
        var currentSchedule = session.Schedule;
        var currentRoute = MechanicalLinkPresentation.Evaluate(Assert.Single(session.InstalledScene.MechanicalLinks),
            session.Committed, session.InstalledScene.Physics);
        var current = new MechanicalLinkSample(session.Committed.Epoch, currentSchedule.World.Epoch.Value,
            currentSchedule.FirstAnimation, RuntimeTestServices.Core.Scheduling.Deadline(currentSchedule.Settings.AnimationRate,
                currentSchedule.FirstAnimation), currentRoute);
        session.Client.ReceiveAnimation(codec.Geometry(new(101, 206), new(1), currentSchedule.Revision, current));
        var resetPhysical = physical with { SimulationTime = new(new(0)) };
        var resetFrame = Assert.Single(session.Client.MechanicalFrames(session.FrameNumber, resetPhysical));
        session.Client.ReceiveAnimation(Encode(valid));
        Assert.Same(resetFrame, Assert.Single(session.Client.MechanicalFrames(session.FrameNumber, resetPhysical)));
    }

    public enum BadCompiledRim { ForeignSupport, MissingJoint, Radius, DuplicateTarget }
    [Theory]
    [InlineData(BadCompiledRim.ForeignSupport)]
    [InlineData(BadCompiledRim.MissingJoint)]
    [InlineData(BadCompiledRim.Radius)]
    [InlineData(BadCompiledRim.DuplicateTarget)]
    public async Task InvalidCompiledGeometryCannotReplaceTheInstalledScene(BadCompiledRim defect)
    {
        await using var h = await WorkshopIntegrationHarness.Running(Construction(Quaternion.Identity, new(0f, 4f, 0f)));
        await h.Advance(12);
        var installed = h.Simulation.Scene;
        var link = Assert.Single(installed.MechanicalLinks);
        var bad = defect switch
        {
            BadCompiledRim.ForeignSupport => link with { Source = link.Source with { Support = new(3) } },
            BadCompiledRim.MissingJoint => link with { Source = link.Source with { Joint = new(ulong.MaxValue) } },
            BadCompiledRim.Radius => link with { Source = link.Source with { Radius = 0f } },
            _ => link
        };
        Assert.Throws<ArgumentException>(() => new WorkshopCompiledScene(installed.Identity, installed.Revision,
            installed.Settings, installed.Physics, installed.Activation, installed.Presentation, installed.Owners,
            installed.RunControls, defect == BadCompiledRim.DuplicateTarget ? [link, bad] : [bad]));
        Assert.Same(installed, h.Simulation.Scene);
        await h.Advance(12);
        Assert.Contains(h.Committed.Presentation.Actuators.ToArray(), row => MathF.Abs(row.Speed) > .1f);
    }

}
