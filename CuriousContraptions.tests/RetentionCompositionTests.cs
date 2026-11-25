using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class RetentionCompositionTests
{
    private static BundleEntry Entry(bool reorder)
    {
        var source = TestBundle.Entry(new("wound_spring"));
        var mechanical = source.Mechanical!;
        var slider = mechanical.Sliders[0] with { LowerLimit = MechanicalValue.Fixed(-.8f) };
        var mass = MassFrameDeclaration.Constant(.5f, Vector3.Zero, new(.02f), Quaternion.Identity, 0f, Vector3.Zero);
        var moving = slider.MovingBody with { MassFrame = mass };
        PrismaticSliderDescriptor[] sliders = [
            slider with { BaseFrame = new(new(-2f, 0, 0), Quaternion.Identity), Compression = MechanicalValue.Fixed(.2f), MovingBody = moving },
            slider with { BaseFrame = new(new(2f, 0, 0), Quaternion.Identity), Compression = MechanicalValue.Fixed(.4f), MovingBody = moving }];
        var hinge = mechanical.Hinges[0];
        RevoluteHingeDescriptor[] hinges = [
            hinge with { BaseFrame = new(new(-2f, 0, 2f), Quaternion.Identity) },
            hinge with { BaseFrame = new(new(2f, 0, 2f), Quaternion.Identity) }];
        MechanicalTransmissionDescriptor[] relations = [
            new(new(13), new(CoordinateKind.Revolute, 0), new(CoordinateKind.Prismatic, 0), MechanicalValue.Fixed(.1f), TransmissionConstraintKind.NonPositiveVelocity),
            new(new(21), new(CoordinateKind.Revolute, 1), new(CoordinateKind.Prismatic, 1), MechanicalValue.Fixed(.1f), TransmissionConstraintKind.NonPositiveVelocity)];
        PrismaticRetentionDescriptor[] retentions = [new(new(31), new(CoordinateKind.Prismatic, 0), new(13)),
            new(new(7), new(CoordinateKind.Prismatic, 1), new(21))];
        SignalNodeDescriptor[] signals = [new SignalPhysicalRequestDescriptor(new("z_request"), LocalPhysicalRequestTarget.ForRetention(new(31)), PhysicalRequestOperation.Release),
            new SignalPhysicalRequestDescriptor(new("a_request"), LocalPhysicalRequestTarget.ForRetention(new(7)), PhysicalRequestOperation.Release)];
        var input = source.Ports.Single(p => p.Id == PortId.ActivationIn);
        var binding = source.Presentation.Bindings[0];
        return source with {
            Metadata = source.Metadata with { Id = "unfamiliar_dual_store" },
            Footprint = new(5, 4, 2, 2), Colliders = [new(CuriousContraptions.Abstractions.ColliderShapeKind.Sphere, new(.1f), Vector3.Zero, Quaternion.Identity, new(0f, .3f, 0f, .1f), 0f)], ColliderBindings = [],
            Ports = [input with { Id = PortId.FirstIn }, input with { Id = PortId.SecondIn }],
            Mechanical = mechanical with { Sliders = sliders, Hinges = hinges,
                MovingBodyColliders = [mechanical.MovingBodyColliders[0], mechanical.MovingBodyColliders[0],
                    mechanical.MovingBodyColliders[1], mechanical.MovingBodyColliders[1]],
                Transmissions = reorder ? relations.Reverse().ToArray() : relations,
                Retentions = reorder ? retentions.Reverse().ToArray() : retentions },
            Signal = new(reorder ? signals.Reverse().ToArray() : signals, [],
                [new(PortId.FirstIn, new(new("z_request"), SignalTerminal.Input)), new(PortId.SecondIn, new(new("a_request"), SignalTerminal.Input))]),
            Presentation = new(3f, PresentationAxes.None,
                [binding with { Node = "LatchA", Retention = new(31) }, binding with { Node = "LatchB", Retention = new(7) }],
                [new("HeadA", new(1), Vector3.Zero), new("HeadB", new(2), Vector3.Zero),
                 new("ShaftA", new(3), Vector3.Zero), new("ShaftB", new(4), Vector3.Zero)], [], [])
        };
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReorderedTwoStoresKeepExactRequestPresentationAndLifecycleOwnership(bool reorder)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        var entry = Entry(reorder);
        Assert.Empty(BundleValidator.Validate([new("dual", entry)]));
        var malformed = entry with { Signal = entry.Signal! with { Nodes =
            entry.Signal.Nodes.Select(node => node.Id == new SignalNodeId("z_request")
                ? (SignalNodeDescriptor)new SignalPhysicalRequestDescriptor(node.Id, LocalPhysicalRequestTarget.ForRetention(new(99)), PhysicalRequestOperation.Release) : node).ToArray() } };
        Assert.Contains(BundleValidator.Validate([new("foreign", malformed)]), e => e.Code == BundleErrorCode.OutOfRange && e.Detail.EndsWith(".Signal.PhysicalRequest", StringComparison.Ordinal));
        var decoded = Assert.Single(BundleSchema.Read(BundleSchema.WriteEntries([entry])).Elements);
        using var declarations = DeclaredPhysics.Scoped([decoded]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        WorkshopGenericInstance Clock(ulong id, float x) => WorkshopInput.Generic(RuntimeTestServices.Bundle, new("clock"), new(id),
            x, 10f, 0, 0, 0, 0, 1, new([new(new("interval_seconds"), .2f)]));
        var bench = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(
            PlacedParts.Battery(new(1), -10, 10, 0), PlacedParts.Battery(new(2), 10, 10, 0, enabled: ElectricalEnable.Disabled),
            Clock(3, -6), Clock(4, 6), WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(5), 0, 10, 0, 0, 0, 0, 1)),
            Connections: new(
                new WorkshopConnection(new(1), PortId.Supply, new(3), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new(new(2), PortId.Supply, new(4), PortId.PowerIn, WorkshopConnectionDomain.Electrical),
                new(new(3), PortId.ActivationOut, new(5), PortId.FirstIn, WorkshopConnectionDomain.Activation),
                new(new(4), PortId.ActivationOut, new(5), PortId.SecondIn, WorkshopConnectionDomain.Activation)));
        await harness.Require(WorkshopCommandKind.Construct, bench);
        var initial = harness.Committed.Presentation.Retentions.ToArray();
        Assert.Equal(2, initial.Length);
        var first = bench.Instances.Identities[new(5)].Retentions[new(31)];
        var second = bench.Instances.Identities[new(5)].Retentions[new(7)];
        Assert.All(initial, row => Assert.Equal(new GpuBodyId(5), row.Owner));
        Assert.InRange(Read(first).Compression - .2f, -.001f, .001f);
        Assert.InRange(Read(second).Compression - .4f, -.001f, .001f);
        // Preloaded stores admit as retained charge at rest.
        Assert.All(initial, row => Assert.Equal(RetentionPhase.Armed, row.Phase));
        await harness.Run();
        await ReleaseOnly(first, second);
        Assert.True(Latch("LatchA") < 0f);
        Assert.Equal(0f, Latch("LatchB"));
        await Set(1, ElectricalEnable.Disabled); await Set(2, ElectricalEnable.Enabled);
        for (var tick = 0; tick < 40 && Read(second).RequestResult is null; tick++) await harness.AdvanceOne();
        Assert.Equal(RetentionRequestResult.Released, Read(second).RequestResult);
        await harness.AdvanceOne();
        Assert.True(Latch("LatchB") < 0f);
        Assert.Equal(0f, Latch("LatchA"));
        var saved = new WorkshopSavedConstruction(bench, new(20));
        var restored = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle,
            SaveFixture.Elements.Append(new PartId(entry.Metadata.Id)).ToHashSet(),
            WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, saved));
        Assert.Equal(saved, restored);
        await harness.Reset();
        Assert.Equal(initial, harness.Committed.Presentation.Retentions.ToArray());
        await harness.Require(WorkshopCommandKind.Construct, restored.Construction with { Revision = new(3) });
        Assert.Equal(initial, harness.Committed.Presentation.Retentions.ToArray());
        await harness.Run();
        await ReleaseOnly(first, second);
        Assert.True(Latch("LatchA") < 0f);
        Assert.Equal(0f, Latch("LatchB"));

        float Latch(string node)
        {
            var binding = Array.FindIndex(entry.Presentation.Bindings.ToArray(), value => value.Node == node);
            Assert.True(binding >= 0);
            var samples = harness.Presentation!.Advance(new(harness.Committed.Tick.Value * 4));
            return Assert.Single(samples, sample => sample.Owner == new GpuBodyId(5) && sample.Binding == binding).Value.Number;
        }
        PrismaticRetentionRead Read(PrismaticRetentionId id) => harness.Committed.Presentation.Retentions.ToArray().Single(r => r.Id == id);
        async Task Set(ulong id, ElectricalEnable value) => await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(new(id), RunControlLane.SourceEnable, RunControlValue.Of(value)));
        async Task ReleaseOnly(PrismaticRetentionId selected, PrismaticRetentionId quiet)
        {
            for (var tick = 0; tick < 40 && Read(selected).RequestResult is null; tick++) await harness.AdvanceOne();
            Assert.Equal(RetentionRequestResult.Released, Read(selected).RequestResult);
            Assert.Null(Read(quiet).RequestResult);
            Assert.InRange(Read(quiet).Compression - .4f, -.001f, .001f);
            await harness.AdvanceOne();
            var response = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(harness.Latest));
            Assert.Equal(harness.Committed.Presentation.Retentions.ToArray(), response.Read.Presentation.Retentions.ToArray());
        }
    }
}
