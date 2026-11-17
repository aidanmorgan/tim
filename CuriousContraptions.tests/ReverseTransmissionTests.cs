using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed partial class ReverseTransmissionTests
{
    public enum Upstream { Motor, Clutch, Conveyor, Reverse }
    public enum Downstream { Clutch, Conveyor, Reverse }
    private static readonly PartId Reverse = new("reverse_transmission");
    private const float CoupledSpeedAllowance = .05f; // <= .05 rad visible disagreement over the one-second observation.
    public static IEnumerable<object[]> Pairings()
    {
        foreach (var source in Enum.GetValues<Upstream>())
            foreach (var target in Enum.GetValues<Downstream>())
                foreach (var sign in new[] { -1, 1 }) yield return [source, target, sign];
    }
    private static WorkshopGenericInstance Part(PartId kind, ulong id, float x, float speed = 6f, Quaternion? rotation = null)
    {
        var parameters = kind.Value switch
        {
            "motor" or "signed_shaft_source" => new InstanceParameterCollection([new(new("speed"), speed), new(new("torque"), 20f)]),
            "clutch" => new InstanceParameterCollection([new(new("close_seconds"), .05f)]),
            _ => InstanceParameterCollection.Defaults(kind)
        };
        var q = rotation ?? Quaternion.Identity;
        return WorkshopInput.Generic(RuntimeTestServices.Bundle, kind, new(id), x, 4f, 0f, q.X, q.Y, q.Z, q.W, parameters);
    }
    private static BundleEntry SignedSource()
    {
        var template = TestBundle.Entry(new("motor"));
        var entry = template with { Metadata = template.Metadata with { Id = "signed_shaft_source",
            Parameters = template.Metadata.Parameters.Select(parameter => parameter.Key == "speed"
                ? parameter with { Minimum = -20f } : parameter).ToArray() } };
        Assert.Empty(BundleValidator.Validate([new("signed source", entry)]));
        return entry;
    }
    private static PartId Kind(Upstream kind) => kind switch
    {
        Upstream.Motor => new("motor"), Upstream.Clutch => new("clutch"), Upstream.Conveyor => new("conveyor"),
        Upstream.Reverse => Reverse, _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    private static PartId Kind(Downstream kind) => kind switch
    {
        Downstream.Clutch => new("clutch"), Downstream.Conveyor => new("conveyor"), Downstream.Reverse => Reverse,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
    internal static WorkshopConnection Drive(ulong from, ulong to) =>
        new(new(from), PortId.Drive, new(to), PortId.DriveIn, WorkshopConnectionDomain.Mechanical);
    internal static WorkshopConnection Power(ulong to) =>
        new(new(1), PortId.Supply, new(to), PortId.PowerIn, WorkshopConnectionDomain.Electrical);
    private static Vector3 Position(WorkshopRead read, GpuBodyId id)
    {
        Assert.True(read.Bodies.TryGet(id, out var body));
        return new((body.Body.Cell.X + body.Body.Local.X) / 16f, (body.Body.Cell.Y + body.Body.Local.Y) / 16f,
            (body.Body.Cell.Z + body.Body.Local.Z) / 16f);
    }
    private static ActuatorDriveRead Coordinate(WorkshopIntegrationHarness harness, GpuBodyId owner, int index) =>
        Assert.Single(harness.Committed.Presentation.Actuators.ToArray(), row =>
            row.Drive == SupplyConsumer.Revolute(harness.Construction.Instances.Identities[owner].Hinges[index].Joint));

    [Theory]
    [MemberData(nameof(Pairings))]
    public Task EveryCurrentPairingCarriesCargoWithReversalParity(Upstream upstream, Downstream downstream, int sign) =>
        Pairing(upstream, downstream, sign, true);

    public static IEnumerable<object[]> CataloguePairings() =>
        Pairings().Where(row => (int)row[2] == 1).Select(row => new[] { row[0], row[1] });

    [Theory]
    [MemberData(nameof(CataloguePairings))]
    public Task CatalogueMotorDrivesEveryCurrentPairing(Upstream upstream, Downstream downstream) =>
        Pairing(upstream, downstream, 1, false);

    private static async Task<float> Pairing(Upstream upstream, Downstream downstream, int sign, bool signedSource, bool bypass = false)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        using var declarations = DeclaredPhysics.Scoped([SignedSource()]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var parts = new List<IWorkshopInstance> { PlacedParts.Battery(new(1), -22f, 4f, 0f), Part(new(signedSource ? "signed_shaft_source" : "motor"), 2, -18f, sign * 6f) };
        var links = new List<WorkshopConnection> { Power(2) };
        var previous = 2UL;
        if (upstream != Upstream.Motor)
        {
            parts.Add(Part(Kind(upstream), 3, -14f)); links.Add(Drive(previous, 3)); previous = 3;
            if (upstream == Upstream.Clutch) links.Add(Power(3));
        }
        if (!bypass)
        {
            parts.Add(Part(Reverse, 4, -10f, rotation: Quaternion.CreateFromYawPitchRoll(.4f, .2f, .3f)));
            links.Add(Drive(previous, 4)); previous = 4;
        }
        parts.Add(Part(Kind(downstream), 5, -6f)); links.Add(Drive(previous, 5));
        if (downstream == Downstream.Clutch) links.Add(Power(5));
        var conveyorX = -6f;
        if (downstream != Downstream.Conveyor)
        {
            parts.Add(Part(new("conveyor"), 6, 0f)); links.Add(Drive(5, 6)); conveyorX = 0f;
        }
        parts.Add(WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(7), conveyorX - .5f, 4.46f, 0f, 0f, 0f, 0f, 1f));
        links.Reverse(); // Authored edge order must not determine causal transfer.
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(parts.ToArray()), Connections: new(links.ToArray()));
        await harness.Require(WorkshopCommandKind.Construct, construction); await harness.Run();
        var initial = Position(harness.Committed, new(7));
        await harness.Advance(120);
        var reversals = (bypass ? 0 : 1) + (upstream == Upstream.Reverse ? 1 : 0) + (downstream == Downstream.Reverse ? 1 : 0);
        var expectedSign = reversals % 2 == 0 ? sign : -sign;
        Assert.True(expectedSign * (Position(harness.Committed, new(7)).X - initial.X) > .02f,
            "A loaded drivetrain must transport cargo in the parity-selected direction.");
        if (!bypass)
        {
            var a = Coordinate(harness, new(4), 0); var b = Coordinate(harness, new(4), 1);
            Assert.True(MathF.Abs(a.Speed) > .1f, "The reversed relation must actually be moving.");
            Assert.InRange(a.Speed + b.Speed, -CoupledSpeedAllowance, CoupledSpeedAllowance);
        }
        harness.Presentation!.Advance(new PhysicalTime(480));
        Assert.NotEmpty(harness.Presentation.MechanicalGeometry);
        Assert.All(harness.Presentation.MechanicalGeometry, geometry => geometry.Validate());
        return Position(harness.Committed, new(7)).X - initial.X;
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task ReverseTransportsCargoOppositeTheZeroReverserBypass(int sign)
    {
        var reversed = await Pairing(Upstream.Motor, Downstream.Conveyor, sign, true);
        var bypass = await Pairing(Upstream.Motor, Downstream.Conveyor, sign, true, bypass: true);
        // Each independent one-second displacement exceeds the existing 2cm useful-transport threshold.
        Assert.True(-sign * reversed > .02f && sign * bypass > .02f);
    }

    private static WorkshopConstruction Simple(bool powered = true, bool connected = true, int sign = 1, bool signedSource = false)
    {
        var links = new List<WorkshopConnection> { Power(2) };
        if (connected) links.Add(Drive(2, 3));
        return new(new(2), WorkshopCadenceSettings.Default(),
            new(PlacedParts.Battery(new(1), -8f, 4f, 0f, enabled: powered ? ElectricalEnable.Enabled : ElectricalEnable.Disabled),
                Part(new(signedSource ? "signed_shaft_source" : "motor"), 2, -5f, sign * 6f), Part(Reverse, 3, 0f)), Connections: new(links.ToArray()));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task AbsentDriveLeavesBothWheelsAtRest(bool powered, bool connected)
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Simple(powered, connected));
        await harness.Advance(120);
        foreach (var index in new[] { 0, 1 })
        {
            Assert.Equal(0f, Coordinate(harness, new(3), index).Coordinate);
            Assert.Equal(0f, Coordinate(harness, new(3), index).Speed);
        }
    }

    [Fact]
    public async Task SupplyLossAddsNoMotorWorkAndResetRestoresAuthoredState()
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Simple());
        await harness.Advance(120);
        var before = Coordinate(harness, new(3), 0).Speed;
        Assert.True(MathF.Abs(before) > .1f);
        await harness.Require(WorkshopCommandKind.ConfigureRunControl, runControl:
            harness.RunControl(new(1), RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        var slot = Array.FindIndex(harness.Simulation.Scene.Physics.Revolutes.ToArray(),
            row => row.Id == harness.Construction.Instances.Identities[new(2)].Hinges[0].Joint);
        var work = PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), slot).Work;
        await harness.Advance(30);
        Assert.Equal(work, PhysicsGpuAbi.ReadRevoluteState(harness.Device.CommittedWorld.ToArray(), slot).Work);
        Assert.True(MathF.Sign(before) * Coordinate(harness, new(3), 0).Speed > 0f, "Source loss cannot erase stored momentum.");
        await harness.Require(WorkshopCommandKind.Reset);
        foreach (var index in new[] { 0, 1 })
        {
            Assert.Equal(0f, Coordinate(harness, new(3), index).Coordinate);
            Assert.Equal(0f, Coordinate(harness, new(3), index).Speed);
        }
        Assert.Equal(2, harness.Construction.Connections.Count);
        await harness.Run(); await harness.Advance(120);
        Assert.True(MathF.Abs(Coordinate(harness, new(3), 0).Speed) > .1f);
    }

    [Fact]
    public void ConflictingSourcesAndWrongDirectionAreRefused()
    {
        var original = Simple();
        var other = Part(new("motor"), 4, 5f, 12f);
        var instances = new WorkshopInstances(original.Instances.Append(other).ToArray());
        foreach (var link in new[] { Drive(4, 3),
            new WorkshopConnection(new(3), PortId.Drive, new(4), PortId.Drive, WorkshopConnectionDomain.Mechanical) })
        {
            var invalid = original with { Instances = instances, Connections = original.Connections.With(link) };
            var error = Assert.Throws<WorkshopEditRejectedException>(() => invalid.Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
            Assert.Equal(WorkshopEditRejection.InvalidConnection, error.Reason);
            Assert.Equal(2, original.Connections.Count);
        }
    }
}
