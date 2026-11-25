using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Godot;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Quaternion = System.Numerics.Quaternion;
using Shape = CuriousContraptions.Abstractions.ColliderShapeKind;

namespace CuriousContraptions.Tests;

[Collection<NativeSceneCollectionDefinition>]
public sealed class DirectionalCompositionTests(NativeSceneFixture godot)
{
    private static readonly PartId Kind = new("unfamiliar_dual_carriage");
    private static readonly GpuBodyId Owner = new(1);
    private static readonly string[] Positive = ["advanceA", "advanceB"], Negative = ["returnA", "returnB"];
    private static readonly float[] Travel = [.6f, 1.1f], Cuts = [-.2f, -.35f];
    private static readonly Vector2[] Widths = [new(.04f, .05f), new(.07f, .06f)];
    private static readonly string[] Heads = ["Visual/CarriageA", "Visual/CarriageB"], Lamps = ["Visual/StatusA", "Visual/StatusB"];
    private static float Anchor(int i) => i == 0 ? .5f : -.5f;
    private static ColliderDescriptor Box(Vector3 half, Vector3 offset) => new(Shape.Box, half, offset,
        Quaternion.Identity, new(0, .3f, 0, .1f), 0);

    private static BundleEntry Entry(bool reorder)
    {
        var source = TestBundle.Entry(new("linear_pusher"));
        var sliders = new PrismaticSliderDescriptor[2];
        var moving = new IReadOnlyList<ColliderDescriptor>[2];
        var nodes = new List<SupplyNodeDescriptor> { new SupplyLogicDescriptor(new("bus"), LogicGateOperation.Nor) };
        var edges = new List<SupplyConnection>();
        var ports = new List<PortDescriptor> { new(new("feed"), SimulationDomain.Electrical, PortDirection.Input,
            PortRole.PowerIn, new("Feed", SocketGlyph.Power), null, Vector3.Zero, Vector3.UnitZ) };
        var bindings = new List<SupplyPortBinding> { new(new("feed"), new(new("bus"), SupplyTerminal.PowerIn)) };
        for (var i = 0; i < 2; i++)
        {
            var speed = i == 0 ? .3f : .5f;
            sliders[i] = new(new(new(Anchor(i), 0, 0), Quaternion.Identity), new(Vector3.Zero, Quaternion.Identity),
                MechanicalValue.Fixed(0), MechanicalValue.Fixed(Travel[i]), MechanicalValue.Fixed(0), MechanicalValue.Fixed(0), MechanicalValue.Fixed(0), false,
                new(MassFrameDeclaration.Constant(i == 0 ? .5f : .8f, Vector3.Zero, new(.02f), Quaternion.Identity, 0, Vector3.Zero)),
                new(MechanicalValue.Fixed(Travel[i]), MechanicalValue.Fixed(speed), MechanicalValue.Fixed(2), MechanicalValue.Fixed(10), MechanicalValue.Fixed(10 * speed),
                    PrismaticTargetSelection.Directional, PrismaticInactiveResponse.SelfLock, new(Positive[i]), new(Negative[i])));
            moving[i] = [Box(new(.1f), Vector3.Zero), Box(new(Widths[i].X, -Cuts[i] / 2, Widths[i].Y), new(0, Cuts[i] / 2, 0))];
            nodes.Add(new SupplyConsumerDescriptor(new("motor" + i), [new SupplyDriveTarget(new(CoordinateKind.Prismatic, i))]));
            nodes.Add(new SupplyConsumerDescriptor(new(Positive[i]), [])); nodes.Add(new SupplyConsumerDescriptor(new(Negative[i]), []));
            edges.Add(new(new(new("bus"), SupplyTerminal.Output), new(new("motor" + i), SupplyTerminal.PowerIn)));
            foreach (var (id, glyph, role) in new[] { (Positive[i], SocketGlyph.Extend, PortRole.FirstIn), (Negative[i], SocketGlyph.Retract, PortRole.SecondIn) })
            {
                ports.Add(new(new(id), SimulationDomain.Electrical, PortDirection.Input, role, new(id, glyph), null, new(Anchor(i), 0, 0), Vector3.UnitZ));
                bindings.Add(new(new(id), new(new(id), SupplyTerminal.PowerIn)));
            }
            foreach (var endpoint in new[] { CoordinateEndpoint.Lower, CoordinateEndpoint.Upper })
            {
                var id = (endpoint == CoordinateEndpoint.Lower ? "home" : "end") + i;
                nodes.Add(new SupplyContactDescriptor(new(id), new PrismaticContactControl(new(CoordinateKind.Prismatic, i), endpoint)));
                edges.Add(new(new(new("bus"), SupplyTerminal.Output), new(new(id), SupplyTerminal.PowerIn)));
                ports.Add(new(new(id), SimulationDomain.Electrical, PortDirection.Output, PortRole.Supply,
                    new(id, endpoint == CoordinateEndpoint.Lower ? SocketGlyph.Retracted : SocketGlyph.Extended), null, new(Anchor(i), 0, 0), Vector3.UnitZ));
                bindings.Add(new(new(id), new(new(id), SupplyTerminal.Output)));
            }
        }
        if (reorder) nodes.Reverse();
        return source with
        {
            Metadata = source.Metadata with { Id = Kind.Value, Parameters = [] }, Footprint = new(3, 3, 2, 0),
            Colliders = [Box(new(.1f), Vector3.Zero)], Ports = ports,
            Mechanical = source.Mechanical! with { KinematicMode = KinematicMode.Dynamic,
                MassFrame = MassFrameDeclaration.Constant(2f, Vector3.Zero, new(.2f), Quaternion.Identity, 0, Vector3.Zero),
                Sliders = sliders, MovingBodyColliders = moving, Extrusions = [new(new(CoordinateKind.Prismatic, 0), new(1), new(0), new(1), Cuts[0], 0, Widths[0]),
                    new(new(CoordinateKind.Prismatic, 1), new(2), new(0), new(1), Cuts[1], 0, Widths[1])] },
            Electrical = new([new("distribution", ElectricalRole.LogicGate, 12, .05f, 0, 400)], new(nodes, edges, bindings)),
            Presentation = new(2f, PresentationAxes.None,
                Enumerable.Range(0, 2).Select(i => source.Presentation.Bindings[0] with { Node = Lamps[i], Actuator = new(CoordinateKind.Prismatic, i),
                    PrismaticValues = new(.1f, .3f, .5f, .7f, .9f, .1f) }).ToArray(),
                [new(Heads[0], new(1), Vector3.Zero), new(Heads[1], new(2), Vector3.Zero)],
                [new(Heads[0], Vector3.Zero, "Visual/ShaftA", new(Anchor(0), Cuts[0], 0), -Cuts[0]),
                 new(Heads[1], Vector3.Zero, "Visual/ShaftB", new(Anchor(1), Cuts[1], 0), -Cuts[1])], [])
        };
    }

    [Fact]
    public void UnresolvedAndConflictingDirectionalReferencesRefuseBeforeInstallation()
    {
        var entry = Entry(false);
        var mechanical = entry.Mechanical!;
        var graph = entry.Electrical!.SupplyGraph!;
        var slider = mechanical.Sliders[0];
        var invalid = new List<(BundleEntry Entry, string Field)>
        {
            (entry with { Mechanical = mechanical with { Sliders = [slider with { Drive = slider.Drive!.Value with { Positive = new("missing") } }, mechanical.Sliders[1]] } }, "Drive.CommandReference"),
            (entry with { Electrical = entry.Electrical with { SupplyGraph = graph with { Nodes = [.. graph.Nodes,
                new SupplyConsumerDescriptor(new("secondWriter"), [new SupplyDriveTarget(new(CoordinateKind.Prismatic, 0))])] } } }, "Consumer.Target"),
            (entry with { Presentation = entry.Presentation with { Bindings = [.. entry.Presentation.Bindings, entry.Presentation.Bindings[0]] } }, "duplicate property writer")
        };
        foreach (var reference in new[] { new CoordinateReference(CoordinateKind.Prismatic, 99), new CoordinateReference(CoordinateKind.Revolute, 0) })
        {
            invalid.Add((entry with { Electrical = entry.Electrical with { SupplyGraph = graph with { Nodes = graph.Nodes.Select(n =>
                n.Id == new LocalSupplyNodeId("motor0") ? new SupplyConsumerDescriptor(n.Id, [new SupplyDriveTarget(reference)]) : n).ToArray() } } }, "Consumer.Target"));
            invalid.Add((entry with { Presentation = entry.Presentation with { Bindings = [entry.Presentation.Bindings[0] with { Actuator = reference }, entry.Presentation.Bindings[1]] } }, "Presentation.Actuator"));
        }
        var installed = DeclaredPorts.Current;
        foreach (var (candidate, field) in invalid)
        {
            var errors = BundleValidator.Validate([new("invalid", candidate)]);
            Assert.Contains(errors, error => error.Detail.Contains(field, StringComparison.Ordinal));
            Assert.Throws<ArgumentException>(() => new PartDefinition(RuntimeTestServices.Bundle, Kind, "Dual", "Invalid", ElementCategory.Power,
                Colors.White, candidate.Metadata.IconPath, null, candidate));
            Assert.Same(installed, DeclaredPorts.Current);
        }
    }

    private static Vector3 Position(WorkshopRead read, GpuBodyId id)
    {
        Assert.True(read.Bodies.TryGet(id, out var body));
        return new((body.Body.Cell.X + body.Body.Local.X) / 16f, (body.Body.Cell.Y + body.Body.Local.Y) / 16f, (body.Body.Cell.Z + body.Body.Local.Z) / 16f);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UnequalDirectionalSlidersKeepReferencesOnARotatedMovingSupport(bool reorder)
    {
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default(), observeWorld: true);
        var entry = Entry(reorder);
        Assert.Empty(BundleValidator.Validate([new("dual", entry)]));
        var decoded = Assert.Single(BundleSchema.Read(BundleSchema.WriteEntries([entry])).Elements);
        using var physics = DeclaredPhysics.Scoped([decoded]);
        using var ports = DeclaredPorts.Scoped(TestBundle.Ports.Extend([(Kind, decoded.Ports)]));
        var rotation = Quaternion.CreateFromYawPitchRoll(MathF.PI / 6, 0, MathF.PI / 9);
        var instance = WorkshopInput.Generic(RuntimeTestServices.Bundle, Kind, Owner, 0, 5, 0, rotation.X, rotation.Y, rotation.Z, rotation.W);
        var instances = new List<IWorkshopInstance> { instance };
        var links = new List<WorkshopConnection>();
        for (var i = 0; i < 5; i++)
        {
            var id = new GpuBodyId((ulong)(10 + i));
            instances.Add(PlacedParts.Battery(id, -5 - i * 2, 5, 0, enabled: i == 0 ? ElectricalEnable.Enabled : ElectricalEnable.Disabled));
            var socket = i == 0 ? "feed" : i == 1 ? Positive[0] : i == 2 ? Negative[0] : i == 3 ? Positive[1] : Negative[1];
            links.Add(new(id, PortId.Supply, Owner, new(socket), WorkshopConnectionDomain.Electrical));
        }
        for (var i = 0; i < 4; i++)
        {
            var consumer = new GpuBodyId((ulong)(30 + i));
            instances.Add(WorkshopInput.Generic(RuntimeTestServices.Bundle, new("powered_gate"), consumer,
                10 + i * 3, 5, 10, 0, 0, 0, 1));
            links.Add(new(Owner, new((i % 2 == 0 ? "home" : "end") + i / 2), consumer,
                PortId.PowerIn, WorkshopConnectionDomain.Electrical));
        }
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(instances.ToArray()), Connections: new(links.ToArray()));
        await harness.Require(WorkshopCommandKind.Construct, construction);
        var identities = construction.Instances.Identities[Owner];
        var initial = harness.Committed;
        var centre = (2 * Position(initial, Owner) + .5f * Position(initial, identities.Sliders[0].Body) + .8f * Position(initial, identities.Sliders[1].Body)) / 3.3f;
        var part = BuildVisual(decoded, reorder, rotation);
        var physical = new WorkshopPhysicalPresentation();
        var worldNode = new Node3D();
        try
        {
            godot.Tree.Root.AddChild(worldNode); worldNode.AddChild(part);
            for (var i = 0; i < 2; i++) part.InstallPhysicalIdentity(new(i + 1), identities.Sliders[i].Body, construction.Instances.Identities);
            float Coordinate(int i) => harness.Committed.Presentation.Actuators.ToArray().Single(a => a.Drive == SupplyConsumer.Prismatic(identities.Sliders[i].Joint)).Coordinate;
            PrismaticMotorRead Motor(int i) => harness.Committed.Presentation.Actuators.ToArray().Single(a => a.Drive == SupplyConsumer.Prismatic(identities.Sliders[i].Joint)).Motor!.Value;
            async Task Enable(ulong id, bool value) => await harness.Require(WorkshopCommandKind.ConfigureRunControl,
                runControl: harness.RunControl(new(id), RunControlLane.SourceEnable, RunControlValue.Of(value ? ElectricalEnable.Enabled : ElectricalEnable.Disabled)));
            void Endpoints(bool homeA, bool endA, bool homeB, bool endB)
            {
                var expected = new[] { homeA, endA, homeB, endB };
                var actuators = harness.Committed.Presentation.Actuators.ToArray();
                for (var i = 0; i < 4; i++)
                    Assert.Equal(expected[i], actuators.Single(a => a.Owner == new GpuBodyId((ulong)(30 + i))).SupplyClosed);
            }
            void Observe(PrismaticDrivePhase a, PrismaticDrivePhase b)
            {
                var read = harness.Committed;
                Assert.Equal(a, Motor(0).Phase); Assert.Equal(b, Motor(1).Phase);
                var samples = new List<PresentedBody>();
                foreach (var id in new[] { Owner, identities.Sliders[0].Body, identities.Sliders[1].Body })
                { Assert.True(read.Bodies.TryGet(id, out var body)); samples.Add(new(id, body.Body.Cell, body.Body.Local, body.Rotation)); }
                physical.Apply([part], Transform3D.Identity, new PresentedBodySet(samples.ToArray()));
                foreach (var sample in harness.Presentation!.Advance(AnimationTestTime.Seconds(read.Tick.Value / 120.0)))
                    if (sample.Owner == Owner) part.ApplyDeclaredProperty(sample.Binding, sample.Value);
                Assert.True(read.Bodies.TryGet(Owner, out var root));
                var rootRotation = new Quaternion(root.Rotation.X, root.Rotation.Y, root.Rotation.Z, root.Rotation.W);
                for (var i = 0; i < 2; i++)
                {
                    var relative = Vector3.Transform(Position(read, identities.Sliders[i].Body) - Position(read, Owner), Quaternion.Conjugate(rootRotation));
                    Assert.InRange(Vector3.Distance(relative, new(Anchor(i), Coordinate(i), 0)), 0, .02f); // 2 cm guide clearance over this two-second sequence.
                    var shape = PhysicsGpuAbi.ReadBoxGeometry(harness.CommittedWorld.Span, identities.Sliders[i].Colliders[1]);
                    Assert.Equal(identities.Sliders[i].Body, shape.MaterialOwner); Assert.Equal(Owner, shape.ClippingOwner);
                    Assert.InRange(MathF.Abs(shape.HalfExtents.Y * 2 - (Coordinate(i) - Cuts[i])), 0, .001f); // 1 mm visible shaft-to-head continuity.
                    Assert.Equal(Widths[i].X, shape.HalfExtents.X); Assert.Equal(Widths[i].Y, shape.HalfExtents.Z);
                    var expected = Motor(i).Phase == PrismaticDrivePhase.Positive ? .3f : Motor(i).Phase == PrismaticDrivePhase.Negative ? .5f : .1f;
                    var material = Assert.IsType<StandardMaterial3D>(part.GetNode<MeshInstance3D>(Lamps[i]).MaterialOverride);
                    Assert.InRange(MathF.Abs(material.AlbedoColor.R - expected), 0, .01f);
                    var visual = part.GetNode<Node3D>(Heads[i]).GlobalPosition;
                    Assert.InRange(Vector3.Distance(new(visual.X, visual.Y, visual.Z), Position(read, identities.Sliders[i].Body)), 0, .001f);
                }
                var currentCentre = (2 * Position(read, Owner) + .5f * Position(read, identities.Sliders[0].Body) + .8f * Position(read, identities.Sliders[1].Body)) / 3.3f;
                Assert.InRange(Vector3.Distance(currentCentre, centre), 0, .02f); // Internal motors must not propel the whole free assembly.
            }
            await harness.Run(); await harness.Advance(2); Endpoints(true, false, true, false);
            await Enable(11, true); await harness.Advance(72);
            Endpoints(false, false, true, false);
            Assert.InRange(Coordinate(0), .1f, .5f); Assert.InRange(MathF.Abs(Coordinate(1)), 0, .02f);
            Assert.Equal(0f, Motor(1).Work); Observe(PrismaticDrivePhase.Positive, PrismaticDrivePhase.Holding);
            await Enable(11, false); await Enable(13, true); await harness.Advance(72);
            var held = Coordinate(0); var work = Motor(0).Work; var advance = Coordinate(1);
            Assert.InRange(held, .1f, .5f); Assert.InRange(advance, .1f, .9f);
            Observe(PrismaticDrivePhase.Holding, PrismaticDrivePhase.Positive);
            Endpoints(false, false, false, false);
            await harness.Advance(12); Assert.Equal(work, Motor(0).Work); Assert.InRange(Coordinate(0), held - .02f, held + .02f);
            await Enable(12, true); await harness.Advance(36);
            Assert.True(Coordinate(0) < held - .03f); Assert.True(Coordinate(1) > advance + .03f);
            Observe(PrismaticDrivePhase.Negative, PrismaticDrivePhase.Positive);
            Assert.True(Vector3.Distance(Position(harness.Committed, Owner), Position(initial, Owner)) > .02f);
            // Generous travel guards, not arrival-tick assertions. Independent consumers
            // distinguish all four endpoint references under either node ordering.
            await harness.Advance(240); Endpoints(true, false, false, true);
            await Enable(12, false); await Enable(11, true);
            await Enable(13, false); await Enable(14, true);
            await harness.Advance(60); Endpoints(false, false, false, false);
            await Enable(14, false); await harness.Advance(300);
            Endpoints(false, true, false, false);
            Assert.Empty(godot.Errors.Drain());
        }
        finally { worldNode.Free(); }
    }

    private static MachinePart BuildVisual(BundleEntry entry, bool reorder, Quaternion rotation)
    {
        var part = new MachinePart { AuthoredId = Owner, Position = new(0, 5, 0), Quaternion = new(rotation.X, rotation.Y, rotation.Z, rotation.W) };
        part.Configure(RuntimeTestServices.Core.Presentation, new PartDefinition(RuntimeTestServices.Bundle, Kind, "Dual carriage", "Declared fixture",
            ElementCategory.Power, Colors.White, entry.Metadata.IconPath, null, entry));
        var visual = new Node3D { Name = "Visual" }; part.AddChild(visual);
        foreach (var i in reorder ? new[] { 1, 0 } : new[] { 0, 1 })
        {
            visual.AddChild(new Node3D { Name = i == 0 ? "CarriageA" : "CarriageB", Position = new(Anchor(i), 0, 0) });
            visual.AddChild(new MeshInstance3D { Name = i == 0 ? "ShaftA" : "ShaftB", Position = new(Anchor(i), Cuts[i] / 2, 0), Mesh = new BoxMesh { Size = new(2 * Widths[i].X, -Cuts[i], 2 * Widths[i].Y) } });
            visual.AddChild(new MeshInstance3D { Name = i == 0 ? "StatusA" : "StatusB", Mesh = new BoxMesh(), MaterialOverride = new StandardMaterial3D() });
        }
        return part;
    }
}
