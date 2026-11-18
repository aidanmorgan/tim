using System.Buffers.Binary;
using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class RetentionWorkTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeclaredSharedSupportTransfersSignedWorkAndBackdrivesWithoutCreatingEnergy(bool movingSupport)
    {
        var template = TestBundle.Entry(new("motor"));
        var mechanical = template.Mechanical!;
        var material = new MaterialProperties(0f, .3f, 0f, .1f);
        var collider = new ColliderDescriptor(CuriousContraptions.Abstractions.ColliderShapeKind.Sphere, new(.1f), Vector3.Zero, Quaternion.Identity, material, 0f);
        var mass = MassFrameDeclaration.Constant(.5f, Vector3.Zero, new(.02f), Quaternion.Identity, 0f, Vector3.Zero);
        var slider = new PrismaticSliderDescriptor(new(new(1f, 0, 0), Quaternion.Identity),
            new(new(.25f, 0, 0), Quaternion.Identity), MechanicalValue.Fixed(-.8f), MechanicalValue.Fixed(0f),
            MechanicalValue.Fixed(40f), MechanicalValue.Fixed(0f), MechanicalValue.Fixed(.25f), false, new(mass, [collider]), null);
        var hinge = mechanical.Hinges[0] with { BaseFrame = new(new(-1f, 0, 0), Quaternion.Identity),
            MovingFrame = new(Vector3.Zero, Quaternion.Identity), FrictionTorque = MechanicalValue.Fixed(0f),
            MovingBody = new(mass, [collider]) };
        var entry = template with {
            Metadata = template.Metadata with { Id = "unfamiliar_reciprocal_work", Parameters = template.Metadata.Parameters
                .Select(p => p.Key == "speed" ? p with { Minimum = -4f, Maximum = 4f, Default = -2f } : p).ToArray() },
            Footprint = new(3, movingSupport ? 3 : 2, 1, 1),
            Ports = template.Ports.Where(p => p.Id == PortId.PowerIn).ToArray(),
            Colliders = [collider],
            Mechanical = mechanical with { KinematicMode = movingSupport ? KinematicMode.Dynamic : KinematicMode.Static,
                MassFrame = movingSupport ? MassFrameDeclaration.Constant(1f, Vector3.Zero, new(.2f), Quaternion.Identity, 0f, Vector3.Zero) : mechanical.MassFrame,
                Sliders = [slider], Hinges = [hinge],
                Transmissions = [new(new(31), new(CoordinateKind.Revolute, 0), new(CoordinateKind.Prismatic, 0),
                    MechanicalValue.Fixed(.1f), TransmissionConstraintKind.Bilateral)] },
            Presentation = template.Presentation with { Bindings = [], Bodies = [new("Slider", new(1), Vector3.Zero), new("Shaft", new(2), Vector3.Zero)], Spans = [], Parameters = [] }
        };
        Assert.Empty(BundleValidator.Validate([new("work", entry)]));
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        using var declarations = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(PlacedParts.Battery(new(1), -4, 10, 0),
                WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(2), 0, 10, 0, 0, 0, 0, 1)),
            Connections: new(new WorkshopConnection(new(1), PortId.Supply, new(2), PortId.PowerIn, WorkshopConnectionDomain.Electrical)));
        await harness.Require(WorkshopCommandKind.Construct, construction); await harness.Run();
        var scene = harness.Simulation.Scene.Physics;
        var origin = harness.Committed;
        var budget = SpringTestEnergy.Measure(scene, origin, origin);
        var allowance = SpringTestEnergy.Allowance(scene);
        var relation = Assert.Single(scene.Transmissions.ToArray());
        // This is the published committed ABI observation, not private solver bodies, impulses or iteration state.
        float Work() => PhysicsGpuAbi.ReadMechanicalTransmissionState(harness.Device.CommittedWorld.Span,
            scene.Layout.GetMechanicalTransmissionSlot(relation.Id.Value)).Work;
        for (var tick = 0; tick < 36; tick++)
        {
            await harness.AdvanceOne();
            Assert.True(SpringTestEnergy.Measure(scene, harness.Committed, origin) <= budget + SpringTestEnergy.SourceWork(harness) + allowance);
        }
        var woundWork = Work();
        Assert.True(woundWork > .01f, "The driven coordinate must receive more than the 0.005J work allowance.");
        if (movingSupport)
        {
            Assert.True(harness.Committed.Bodies.TryGet(new(2), out var frame));
            Assert.True(MathF.Abs(frame.AngularVelocity.Z) > .01f, "The shared support must carry angular reaction, not act as a fixed fixture.");
            Assert.True(MathF.Abs(frame.Body.Velocity.Y) > .01f, "The shared support must carry linear reaction.");
        }
        await harness.Require(WorkshopCommandKind.ConfigureRunControl,
            runControl: harness.RunControl(new(1), RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
        var passiveOrigin = harness.Committed;
        var passiveBudget = SpringTestEnergy.Measure(scene, passiveOrigin, passiveOrigin);
        var paid = SpringTestEnergy.SourceWork(harness);
        var minimumWork = woundWork;
        for (var tick = 0; tick < 120; tick++)
        {
            await harness.AdvanceOne();
            minimumWork = MathF.Min(minimumWork, Work());
            Assert.Equal(paid, SpringTestEnergy.SourceWork(harness));
            Assert.True(SpringTestEnergy.Measure(scene, harness.Committed, passiveOrigin) <= passiveBudget + allowance,
                "Backdrive and the merged shared support cannot invent mechanical energy.");
        }
        Assert.True(minimumWork < woundWork - .01f, "Backdrive must remove signed coordinate work; positive-only correction accumulation is invalid.");
        await harness.Reset();
        Assert.Equal(0f, Work());
    }
}
