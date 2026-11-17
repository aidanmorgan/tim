using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Bundle;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed partial class ReverseTransmissionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HousingSupportsCargoWhileBeyondFootprintIsClear(bool aligned)
    {
        var construction = Simple(powered: false);
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("ball"), new(4),
            aligned ? 0f : 1.5f, 5.2f, -.15f, 0f, 0f, 0f, 1f);
        construction = construction with { Instances = new(construction.Instances.Append(ball).ToArray()) };
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        await h.Advance(120);
        var position = Position(h.Committed, new(4));
        if (aligned)
        {
            // Housing top4.325 plus .34m basketball; 5cm support allowance excludes penetration/pass-through.
            Assert.InRange(position.Y, 4.615f, 4.715f);
            await h.Advance(60);
            Assert.InRange(Position(h.Committed, new(4)).Y, 4.615f, 4.715f);
        }
        else Assert.True(position.Y < 3f, "Outside the housing footprint the payload must fall clear.");
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(1, false)]
    public async Task ExposedWheelHasPhysicalContactAndSignedSurfaceMotion(int sign, bool aligned)
    {
        await using var h = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var template = TestBundle.Entry(new("ball"));
        var probe = template with { Metadata = template.Metadata with { Id = "rim_contact_probe" },
            Colliders = [new(CuriousContraptions.Abstractions.ColliderShapeKind.Sphere, new(.1f), Vector3.Zero,
                Quaternion.Identity, new(0f, .3f, 0f, .1f), 0f)],
            Mechanical = template.Mechanical! with { MassFrame = MassFrameDeclaration.Constant(.1f, Vector3.Zero, new(.0004f),
                Quaternion.Identity, 0f, new(0f, -9.81f, 0f)) } };
        using var declared = DeclaredPhysics.Scoped([probe, SignedSource()]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var construction = Simple(powered: sign != 0, sign: sign == 0 ? 1 : sign, signedSource: true);
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(probe.Metadata.Id), new(4),
            .36f, 4.65f, aligned ? .32f : .7f, 0f, 0f, 0f, 1f);
        construction = construction with { Instances = new(construction.Instances.Append(ball).ToArray()) };
        await h.Require(WorkshopCommandKind.Construct, construction); await h.Run();
        await h.Advance(30);
        var position = Position(h.Committed, new(4));
        if (!aligned) Assert.True(position.Y < 4.4f, "Beyond the exposed axial width the probe falls without wheel support.");
        else if (sign == 0)
            Assert.InRange(position.Y, 4.37f, 4.47f); // .32m rim plus .1m probe, existing5cm physical support allowance.
        else
            Assert.True(-sign * (position.X - .36f) > .01f, "Wheel surface motion must deflect the touching probe in its rotation direction.");
    }

    [Fact]
    public async Task SaveLoadAndResetPreserveRotatedTwoReverserParity()
    {
        var original = Simple();
        var second = Part(Reverse, 4, 5f, rotation: Quaternion.CreateFromYawPitchRoll(.5f, -.2f, .6f));
        original = original with { Instances = new(original.Instances.Append(second).ToArray()),
            Connections = original.Connections.With(Drive(3, 4)) };
        var saved = new WorkshopSavedConstruction(original, new(5));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands,
            RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements,
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        Assert.Equal(saved, loaded); // Authored fields, including rotation, IDs and connections.
        foreach (var construction in new[] { original, loaded.Construction })
        {
            await using var h = await WorkshopIntegrationHarness.Building(construction);
            var built = h.Committed;
            await h.Run(); await h.Advance(120);
            Assert.True(Coordinate(h, new(4), 1).Speed < -.1f, "Two reversers restore the motor coordinate direction.");
            await h.Reset();
            NoFixedCapacityTests.AssertSameState(built, h.Committed, "reverse authored Reset");
            await h.Run(); await h.Advance(120);
            Assert.True(Coordinate(h, new(4), 1).Speed < -.1f);
        }
    }

    [Fact]
    public async Task APassiveTwoReverserLoopIsAdmittedAndRemainsAtRest()
    {
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(Part(Reverse, 2, -3f), Part(Reverse, 3, 3f)), Connections: new(Drive(2, 3), Drive(3, 2)));
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        await h.Advance(120);
        foreach (var owner in new[] { new GpuBodyId(2), new GpuBodyId(3) })
            foreach (var index in new[] { 0, 1 }) Assert.Equal(0f, Coordinate(h, owner, index).Speed);
    }

    [Fact]
    public void UnsupportedMechanicalConnectionsRefuseAtAuthoringAndSaveBoundaries()
    {
        var original = Simple();
        foreach (var link in new[] {
            new WorkshopConnection(new(2), PortId.Drive, new(3), new("BeltIn"), WorkshopConnectionDomain.Mechanical),
            new WorkshopConnection(new(2), new("ChainOut"), new(3), PortId.DriveIn, WorkshopConnectionDomain.Mechanical),
            new WorkshopConnection(new(2), PortId.Drive, new(3), PortId.DriveIn, (WorkshopConnectionDomain)255) })
        {
            var invalid = original with { Connections = new(Power(2), link) };
            Assert.Throws<WorkshopEditRejectedException>(() => invalid.Validate(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle));
            if (link.Domain != WorkshopConnectionDomain.Mechanical)
                Assert.Throws<WorkshopSaveRejectedException>(() => WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands,
                    RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, SaveFixture.Elements, SaveFixture.Forged(invalid, 4)));
            Assert.Equal(2, original.Connections.Count);
        }
    }
    [Fact]
    public async Task OpeningTheDownstreamClutchIsolatesItsCoastingOutput()
    {
        var original = Simple();
        var clutch = Part(new("clutch"), 4, 5f);
        var coil = PlacedParts.Battery(new(5), 8f, 6f, 0f);
        var conveyor = Part(new("conveyor"), 6, 11f);
        var construction = original with { Instances = new(original.Instances.Append(clutch).Append(coil).Append(conveyor).ToArray()),
            Connections = original.Connections.With(Drive(3, 4)).With(Drive(4, 6)).With(
                new(new(5), PortId.Supply, new(4), PortId.PowerIn, WorkshopConnectionDomain.Electrical)) };
        await using var h = await WorkshopIntegrationHarness.Running(construction);
        await h.Advance(120);
        Assert.True(Coordinate(h, new(4), 1).Speed < -1f);
        var loadedSpeed = MathF.Abs(Coordinate(h, new(6), 0).Speed);
        Assert.True(loadedSpeed > 1f);
        await h.Require(WorkshopCommandKind.ConfigureRunControl, runControl:
            h.RunControl(new(5), RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        await h.Advance(12); // Generous interval beyond declared .05s opening.
        var timed = Array.FindIndex(h.Simulation.Scene.Physics.Transmissions.ToArray(), row => row.CloseSeconds > 0f);
        Assert.Equal(0f, PhysicsGpuAbi.ReadMechanicalTransmissionState(h.Device.CommittedWorld.ToArray(), timed).Impulse);
        await h.Require(WorkshopCommandKind.ConfigureRunControl, runControl:
            h.RunControl(new(2), RunControlLane.RevoluteDriveTarget, RunControlValue.Of(0f)));
        await h.Advance(30);
        Assert.InRange(Coordinate(h, new(3), 1).Speed, -.05f, .05f);
        Assert.True(Coordinate(h, new(4), 1).Speed < -1f,
            "After opening, stopping the upstream source must not brake the disconnected output.");
        var coasting = MathF.Abs(Coordinate(h, new(6), 0).Speed);
        Assert.True(coasting < loadedSpeed - CoupledSpeedAllowance,
            "The current Conveyor's declared 0.02 Nm bearing load must dissipate disconnected shaft motion.");
        Assert.InRange(Coordinate(h, new(4), 1).Speed - Coordinate(h, new(6), 0).Speed,
            -CoupledSpeedAllowance, CoupledSpeedAllowance);
        await h.Advance(600); // Five-second stop guard for finite shaft inertia and declared bearing torque.
        Assert.InRange(Coordinate(h, new(6), 0).Speed, -CoupledSpeedAllowance, CoupledSpeedAllowance);
        Assert.InRange(Coordinate(h, new(4), 1).Speed, -CoupledSpeedAllowance, CoupledSpeedAllowance);
    }

}
