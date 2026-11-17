using System;
using System.Linq;
using System.Threading.Tasks;
using System.Buffers.Binary;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed partial class ReverseTransmissionTests
{
    [Fact]
    public async Task CommittedTransmissionReactionOwnsFreshEngineAndAbandonedCandidateExecution()
    {
        await using var h = await WorkshopIntegrationHarness.Running(Simple());
        await h.Advance(1);
        var physics = h.Simulation.Scene.Physics;
        var inputs = new PhysicsTickInputs([ElectricalEnable.Enabled],
            physics.Revolutes.ToArray().Select(row => physics.RevoluteDrives.ToArray().Any(drive => drive.Joint == row.Id)).ToArray())
        { DriveTarget = [6f], TransmissionClosed = new bool[physics.Transmissions.Length] };
        var original = h.Device.CommittedWorld.ToArray();
        var engine = new WorkshopPhysicsEngine();
        var first = new byte[original.Length];
        var interactions = engine.Advance(original, first, inputs, PhysicsInteractionSnapshot.Empty);
        var continued = new byte[original.Length];
        engine.Advance(first, continued, inputs, interactions);
        var restored = new byte[original.Length];
        new WorkshopPhysicsEngine().Advance(first, restored, inputs, interactions);
        var resumed = new byte[original.Length];
        // The earlier candidate was never committed here; reusing the engine must reload the authoritative first state.
        engine.Advance(first, resumed, inputs, interactions);
        var expected = PhysicsGpuAbi.ReadPresentation(continued, inputs, false).Actuators.ToArray();
        foreach (var candidate in new[] { restored, resumed })
        {
            var actual = PhysicsGpuAbi.ReadPresentation(candidate, inputs, false).Actuators.ToArray();
            Assert.Equal(expected.Select(row => row.Drive), actual.Select(row => row.Drive));
            for (var index = 0; index < expected.Length; index++)
            {
                Assert.InRange(actual[index].Speed - expected[index].Speed, -.05f, .05f);
                Assert.InRange(actual[index].Coordinate - expected[index].Coordinate, -.01f, .01f);
            }
        }
        var invalid = original.ToArray();
        var offset = PhysicsGpuAbi.Layout(invalid).MechanicalTransmissionOffset(0) + PhysicsGpuAbi.MechanicalTransmissionImpulseOffset;
        BinaryPrimitives.WriteSingleLittleEndian(invalid.AsSpan(offset), float.NaN);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadMechanicalTransmissionState(invalid, 0));
        var transport = new BrowserGpuTransport(new CuriousContraptions.Abstractions.EventRingPublication());
        try
        {
        await transport.Initialize();
        await Assert.ThrowsAsync<ArgumentException>(() => transport.Stage(invalid, WorkshopGpuOperation.Admit, PhysicsTickInputs.Admission));
        await transport.Stage(first, WorkshopGpuOperation.Admit, PhysicsTickInputs.Admission);
        Assert.Equal(PhysicsGpuAbi.ReadMechanicalTransmissionState(first, 0),
            PhysicsGpuAbi.ReadMechanicalTransmissionState(transport.Read().World.Span, 0));
        }
        finally { transport.Dispose(); }
        var afterRefusal = new byte[original.Length];
        engine.Advance(first, afterRefusal, inputs, interactions);
        Assert.All(PhysicsGpuAbi.ReadPresentation(afterRefusal, inputs, false).Actuators.ToArray(),
            row => Assert.True(float.IsFinite(row.Speed) && float.IsFinite(row.Coordinate)));
    }

    [Fact]
    public async Task DisengagedTransmissionRefusesRetainedReaction()
    {
        var baseScene = Simple();
        var clutch = Part(new("clutch"), 4, 5f);
        var construction = baseScene with { Instances = new(baseScene.Instances.Append(clutch).ToArray()),
            Connections = baseScene.Connections.With(Drive(3, 4)) };
        await using var h = await WorkshopIntegrationHarness.Building(construction);
        var index = Array.FindIndex(h.Simulation.Scene.Physics.Transmissions.ToArray(), row => row.CloseSeconds > 0f);
        var bytes = h.Device.CommittedWorld.ToArray();
        Assert.Equal(0f, PhysicsGpuAbi.ReadMechanicalTransmissionState(bytes, index).Impulse);
        var offset = PhysicsGpuAbi.Layout(bytes).MechanicalTransmissionOffset(index) + PhysicsGpuAbi.MechanicalTransmissionImpulseOffset;
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset), .1f);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadMechanicalTransmissionState(bytes, index));
        await h.Run(); await h.Advance(12);
        Assert.Equal(0f, Coordinate(h, new(4), 1).Speed);
    }
}
