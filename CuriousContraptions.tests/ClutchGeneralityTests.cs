using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Xunit;
using Wire = CuriousContraptions.Wire;

namespace CuriousContraptions.Tests;

public sealed partial class ClutchTests
{
    private static BundleEntry TimedEntry(float seconds, bool cosmetics = true)
    {
        var template = TestBundle.Entry(ClutchId);
        return template with
        {
            Metadata = template.Metadata with { Id = "timed_transmission_probe", Parameters = [], PaletteOrder = 253 },
            Mechanical = template.Mechanical! with
            {
                Transmissions = [new(new(1), new(CoordinateKind.Revolute, 0), new(CoordinateKind.Revolute, 1),
                    MechanicalValue.Fixed(1f), TransmissionConstraintKind.Bilateral, MechanicalValue.Fixed(seconds))]
            },
            Presentation = template.Presentation with
            {
                Bindings = cosmetics ? [new("Visual/Lamp", PresentationProperty.LocalY, PresentationDriver.CommittedTransmissionPhase,
                    PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                    Transmission: new(1), FourStateValues: new(0.1f, 0.3f, 0.6f, 0.9f))] : []
            }
        };
    }

    private static async Task InSyntheticWorld(BundleEntry entry, Func<WorkshopIntegrationHarness, WorkshopConstruction, Task> scenario, float? ratio = null)
    {
        Assert.Empty(BundleValidator.Validate([new("probe", entry)]));
        await using var harness = await WorkshopIntegrationHarness.Initialized(WorkshopCadenceSettings.Default());
        using var physics = DeclaredPhysics.Scoped([entry]);
        using var ports = DeclaredPorts.Scoped(ElementPortCatalogue.FromBundle(DeclaredPhysics.Entries.Values));
        var part = WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(3), -2.5f, RigY, 0f, 0f, 0f, 0f, 1f,
            ratio.HasValue ? new([new(new("ratio"), ratio.Value)]) : default);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(PlacedParts.Battery(new(1), -8.5f, RigY, 0f), MotorPart(2), part),
            Connections: new([Power(1, 2), Power(1, 3), Drive(2, 3)]));
        await harness.Require(WorkshopCommandKind.Construct, construction);
        await scenario(harness, construction);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public Task FourPhasesAreIndependentlyMappedAndCosmeticsDoNotControlCoupling(bool cosmetics) =>
        InSyntheticWorld(TimedEntry(0.2f, cosmetics), async (harness, construction) =>
        {
            void Observe(TransmissionEngagementPhase expectedPhase, float expectedValue)
            {
                Assert.Equal(expectedPhase, SingleTransmission(harness.Committed, new(3)).Phase);
                if (cosmetics)
                {
                    var sample = Assert.Single(harness.Presentation!.Advance(
                        new PhysicalTime(harness.Committed.Tick.Value * 4)), row => row.Owner == new GpuBodyId(3));
                    Assert.Equal(expectedValue, sample.Value.Number);
                }
            }
            Observe(TransmissionEngagementPhase.Open, 0.1f);
            await harness.Run();
            await harness.Advance(12);
            Observe(TransmissionEngagementPhase.Closing, 0.3f);
            var outputId = construction.Instances.Identities[new(3)].Hinges[1].Body;
            Assert.InRange(BodyAngular(harness.Committed, outputId).Length(), 0f, IdleSpeedAllowance);
            await harness.Advance(24);
            Observe(TransmissionEngagementPhase.Engaged, 0.6f);
            Assert.True(BodyAngular(harness.Committed, outputId).Length() > 1f);
            await harness.Require(WorkshopCommandKind.ConfigureRunControl,
                runControl: harness.RunControl(new(1), RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
            await harness.AdvanceOne();
            Observe(TransmissionEngagementPhase.Opening, 0.9f);
            Assert.True(BodyAngular(harness.Committed, outputId).Length() > 1f);
            await harness.Reset();
            Observe(TransmissionEngagementPhase.Open, 0.1f);
        });

    [Theory]
    [InlineData(float.Epsilon)]
    [InlineData(0.0005f)]
    [InlineData(120f)]
    public Task GenericDurationUsesClockTimeBeyondProductBounds(float seconds) =>
        InSyntheticWorld(TimedEntry(seconds), async (harness, _) =>
        {
            await harness.Run();
            await harness.AdvanceOne();
            var row = SingleTransmission(harness.Committed, new(3));
            if (seconds < 1f / 120f)
            {
                Assert.Equal(TransmissionEngagementPhase.Engaged, row.Phase);
                Assert.Equal(1f, row.Closure);
            }
            else
            {
                await harness.Advance(119);
                row = SingleTransmission(harness.Committed, new(3));
                Assert.Equal(TransmissionEngagementPhase.Closing, row.Phase);
                // One elapsed second of the declared 120-second linear timer. This is the timer's mathematical contract.
                Assert.InRange(row.Closure, (1f / 120f) * 0.99f, (1f / 120f) * 1.01f);
            }
        });

    [Fact]
    public void GenericDurationAndFourStateMappingsRejectUnsupportedInputs()
    {
        foreach (var seconds in new[] { -0f, -1f, float.NaN, float.PositiveInfinity,
                     MathF.BitIncrement(MechanicalTransmissionDeclaration.MaximumCloseSeconds) })
            Assert.Contains(BundleValidator.Validate([new("probe", TimedEntry(seconds))]), error => error.Code == BundleErrorCode.OutOfRange);
        Assert.Empty(BundleValidator.Validate([new("probe", TimedEntry(MechanicalTransmissionDeclaration.MaximumCloseSeconds))]));
        var entry = TimedEntry(0.2f);
        foreach (var binding in new[]
        {
            entry.Presentation.Bindings[0] with { FourStateValues = null },
            entry.Presentation.Bindings[0] with { FourStateValues = new(0f, 1f, 2f, float.NaN) },
            entry.Presentation.Bindings[0] with { StateValues = new(0f, 1f, 2f) }
        })
            Assert.Contains(BundleValidator.Validate([new("probe", entry with
            {
                Presentation = entry.Presentation with { Bindings = [binding] }
            })]), error => error.Code == BundleErrorCode.OutOfRange);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConnectionOrderDoesNotChangeClosureOrTransmission(bool reversed)
    {
        var original = StandardConveyorRig(closeSeconds: 0.2f, cadence: SimulationCadence.Hz120, includeBall: false);
        var construction = reversed ? original with { Connections = new(original.Connections.Reverse().ToArray()) } : original;
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        await harness.Advance(48);
        Assert.Equal(TransmissionEngagementPhase.Engaged, SingleTransmission(harness.Committed, new(3)).Phase);
        var ids = construction.Instances.Identities[new(3)];
        var input = BodyAngular(harness.Committed, ids.Hinges[0].Body).Z;
        var output = BodyAngular(harness.Committed, ids.Hinges[1].Body).Z;
        Assert.True(output > 1f);
        Assert.InRange(output - input, -CoupledSpeedAllowance, CoupledSpeedAllowance);
    }

    [Theory]
    [InlineData(1.5f)]
    [InlineData(2.5f)]
    [InlineData(-1.5f)]
    public Task ParameterBoundRatioCompilesAndTransmitsAuthoredSignAndMagnitude(float ratio)
    {
        var entry = TimedEntry(0.05f);
        entry = entry with
        {
            Metadata = entry.Metadata with { Parameters = [new("ratio", ParameterUnit.None, ParameterKind.Real,
                ratio > 0 ? 1f : -3f, ratio > 0 ? 3f : -1f, ratio > 0 ? 1f : -1f, [])] },
            Mechanical = entry.Mechanical! with { Transmissions = [entry.Mechanical!.Transmissions[0] with { Ratio = MechanicalValue.FromParameter("ratio") }] }
        };
        return InSyntheticWorld(entry, async (harness, construction) =>
        {
            var ids = construction.Instances.Identities[new(3)];
            Assert.Equal(ratio, Assert.Single(harness.Simulation.Scene.Physics.Transmissions.ToArray(), t => t.Owner == new GpuBodyId(3)).Ratio);
            await harness.Run();
            await harness.Advance(96);
            var input = BodyAngular(harness.Committed, ids.Hinges[0].Body).Z;
            var output = BodyAngular(harness.Committed, ids.Hinges[1].Body).Z;
            Assert.True(input > 1f);
            Assert.InRange(output - ratio * input, -CoupledSpeedAllowance, CoupledSpeedAllowance);
            foreach (var invalid in new[] { 0f, float.NaN, 4f })
                Assert.Throws<ArgumentException>(() => WorkshopInput.Generic(RuntimeTestServices.Bundle, new(entry.Metadata.Id), new(8),
                    0f, RigY, 0f, 0f, 0f, 0f, 1f, new([new(new("ratio"), invalid)])));
            foreach (var invalidParameter in new[]
            {
                entry.Metadata.Parameters[0] with { Unit = ParameterUnit.Second },
                entry.Metadata.Parameters[0] with { Minimum = -1f, Maximum = 1f },
                entry.Metadata.Parameters[0] with { Maximum = float.NaN }
            })
                Assert.NotEmpty(BundleValidator.Validate([new("probe", entry with { Metadata = entry.Metadata with { Parameters = [invalidParameter] } })]));
            Assert.NotEmpty(BundleValidator.Validate([new("probe", entry with { Metadata = entry.Metadata with { Parameters = [] } })]));
        }, ratio);
    }

    private static BundleEntry ChildSupportEntry()
    {
        var template = TestBundle.Entry(ClutchId);
        var hinges = template.Mechanical!.Hinges.ToArray();
        hinges[1] = hinges[1] with { Support = new(1), BaseFrame = new(new(1.3f, 0f, 0f), Quaternion.Identity) };
        return template with
        {
            Metadata = template.Metadata with { Id = "child_support_probe", PaletteOrder = 254 },
            Mechanical = template.Mechanical with { Hinges = hinges }
        };
    }

    [Fact]
    public Task DifferentHingeSupportsPreserveProvenanceAndReadTheSelectedChildCoordinate() =>
        InSyntheticWorld(ChildSupportEntry(), async (harness, construction) =>
        {
            var compiled = harness.Simulation.Scene;
            var joints = compiled.Physics.Revolutes.ToArray();
            var ids = construction.Instances.Identities[new(3)];
            var input = joints.Single(j => j.Id == ids.Hinges[0].Joint);
            var output = joints.Single(j => j.Id == ids.Hinges[1].Joint);
            Assert.Equal(new GpuBodyId(3), input.BodyA);
            Assert.Equal(input.BodyB, output.BodyA);
            await harness.Run();
            await harness.Advance(12);
            var read = harness.Committed;
            var selected = Assert.Single(read.Presentation.Actuators.ToArray(), row => row.Drive == SupplyConsumer.Revolute(output.Id));
            var first = Assert.Single(read.Presentation.Actuators.ToArray(), row => row.Drive == SupplyConsumer.Revolute(input.Id));
            Assert.Equal(input.BodyB, selected.Owner);
            Assert.True(MathF.Abs(selected.Coordinate - first.Coordinate) > 0.01f);
            var samples = harness.Presentation!.Advance(new PhysicalTime(read.Tick.Value * 4));
            Assert.Equal(selected.Coordinate, Assert.Single(samples, sample => sample.Owner == new GpuBodyId(3) && sample.Binding == 3).Value.Number);
            await harness.Advance(72);
            Assert.Equal(TransmissionEngagementPhase.Engaged, SingleTransmission(harness.Committed, new(3)).Phase);
            var final = harness.Committed.Presentation.Actuators.ToArray();
            var firstSpeed = Assert.Single(final, row => row.Drive == SupplyConsumer.Revolute(input.Id)).Speed;
            var secondSpeed = Assert.Single(final, row => row.Drive == SupplyConsumer.Revolute(output.Id)).Speed;
            Assert.InRange(secondSpeed - firstSpeed, -CoupledSpeedAllowance, CoupledSpeedAllowance);
            await harness.Reset();
            Assert.Equal(0f, Assert.Single(harness.Committed.Presentation.Actuators.ToArray(), row => row.Drive == SupplyConsumer.Revolute(output.Id)).Coordinate);
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ChildSupportInitialTransformsFollowDependenciesInsteadOfDeclarationOrder(bool reversed)
    {
        var entry = ChildSupportEntry();
        var hinges = entry.Mechanical!.Hinges.ToArray();
        hinges[0] = hinges[0] with { InitialAngle = MechanicalValue.Fixed(30f) };
        if (reversed)
        {
            hinges = [hinges[1] with { Support = new(2) }, hinges[0]];
            entry = entry with
            {
                Ports = entry.Ports.Select(p => p.Coordinate is { } c ? p with { Coordinate = c with { Index = 1 - c.Index } } : p).ToArray(),
                Presentation = entry.Presentation with
                {
                    Bindings = entry.Presentation.Bindings.Select(b => b.Actuator is { } c ? b with { Actuator = c with { Index = 1 - c.Index } } : b).ToArray(),
                    Bodies = entry.Presentation.Bodies.Select(b => b with { Body = new(3 - b.Body.Value) }).ToArray()
                }
            };
        }
        entry = entry with { Mechanical = entry.Mechanical! with
        {
            Hinges = hinges,
            Transmissions = [entry.Mechanical!.Transmissions[0] with
            {
                Driving = new(CoordinateKind.Revolute, reversed ? 1 : 0),
                Driven = new(CoordinateKind.Revolute, reversed ? 0 : 1)
            }]
        }};
        return InSyntheticWorld(entry, async (harness, construction) =>
        {
            var ids = construction.Instances.Identities[new(3)];
            var child = ids.Hinges[reversed ? 0 : 1].Body;
            var support = ids.Hinges[reversed ? 1 : 0].Body;
            var joint = harness.Simulation.Scene.Physics.Revolutes.ToArray().Single(j => j.BodyB == child);
            Assert.Equal(support, joint.BodyA);
            var body = harness.Simulation.Scene.Physics.Bodies.ToArray().Single(b => b.Id == child);
            var position = new Vector3((body.Cell.X + body.Local.X) / 16f, (body.Cell.Y + body.Local.Y) / 16f, (body.Cell.Z + body.Local.Z) / 16f);
            var expected = new Vector3(-2.5f - .65f, RigY, .43f) + Vector3.Transform(new(1.3f, 0f, 0f), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 6f));
            Assert.InRange(Vector3.Distance(position, expected), 0f, .001f); // sub-millimetre attachment alignment, same plate clearance.
            await harness.Run();
            await harness.Advance(6);
            await harness.Reset();
            Assert.Equal(0f, Assert.Single(harness.Committed.Presentation.Actuators.ToArray(), a => a.Drive == SupplyConsumer.Revolute(joint.Id)).Coordinate);
        });
    }

    [Fact]
    public async Task CompiledMembershipPreservesExplicitOwnersAcrossSupportsAndRejectsInvalidConstraints()
    {
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(ClutchPart(3), ClutchPart(4)));
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var scene = harness.Simulation.Scene;
        var source = scene.Physics;
        var joints = source.Revolutes.ToArray();
        WorkshopCompiledScene With(RevoluteConstraintDeclaration[] candidate, params GpuBodyId[] extraOwners)
        {
            var physics = new PhysicsSceneDeclaration(source.Document, source.NextIdentity + 10, source.Bodies, source.Colliders,
                source.Materials, source.Sensors, source.Guides, electricalSources: source.Electrical.Sources,
                supplyNodes: source.SupplyGraph.Nodes, supplyEdges: source.SupplyGraph.Edges,
                revolutes: candidate, transmissions: source.Transmissions);
            return new(scene.Identity, scene.Revision, scene.Settings, physics, scene.Activation, scene.Presentation,
                scene.Owners.Concat(extraOwners), scene.RunControls);
        }
        _ = With(joints, new GpuBodyId(99)); // a logical owner is not required to invent a physical body.
        var a = joints.First(j => j.BodyA == new GpuBodyId(3));
        var b = joints.First(j => j.BodyA == new GpuBodyId(4));
        var cross = b with { Id = new(source.NextIdentity + 1), BodyA = a.BodyB };
        var crossSupported = With([.. joints, cross]); // A→a, B→b, a→b does not reassign either child.
        Assert.Equal(new GpuBodyId(3), crossSupported.Physics.Bodies.ToArray().Single(body => body.Id == a.BodyB).Owner);
        Assert.Equal(new GpuBodyId(4), crossSupported.Physics.Bodies.ToArray().Single(body => body.Id == b.BodyB).Owner);
        Assert.Throws<ArgumentException>(() => With([.. joints, a with { Id = cross.Id }])); // duplicate unordered body pair
        Assert.Throws<ArgumentException>(() => With([.. joints, a with { Id = cross.Id, BodyA = a.BodyB, BodyB = new(3) }])); // reversed duplicate pair; distinct-pair cycles remain legal
        Assert.Throws<ArgumentException>(() => With(joints.Select(j => j.Id == a.Id ? j with { BodyA = new(99) } : j).ToArray(), new GpuBodyId(99)));
        var prior = harness.Committed.Tick;
        await harness.AdvanceOne();
        Assert.Equal(prior.Value + 1, harness.Committed.Tick.Value);
        Assert.Equal(TransmissionEngagementPhase.Open, SingleTransmission(harness.Committed, new(3)).Phase);
    }

    [Fact]
    public Task UntimedInternalTransmissionRequiresDeclaredRootAndBothOwnedEndpoints()
    {
        var entry = TimedEntry(.2f, false);
        entry = entry with
        {
            Metadata = entry.Metadata with { Id = "untimed_membership_probe" },
            Mechanical = entry.Mechanical! with { Transmissions = [entry.Mechanical.Transmissions[0] with { CloseSeconds = null }] },
            Electrical = entry.Electrical! with { SupplyGraph = new(
                [new SupplyConsumerDescriptor(new("consumer"), [])], [],
                [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]) }
        };
        return InSyntheticWorld(entry, async (harness, construction) =>
        {
            var scene = harness.Simulation.Scene;
            var physics = scene.Physics;
            var internalRow = Assert.Single(physics.Transmissions.ToArray(), row => row.Owner == new GpuBodyId(3));
            Assert.Equal(0f, internalRow.CloseSeconds);
            Assert.Single(physics.Transmissions.ToArray(), row => row.Owner == default);
            var command = harness.Command(WorkshopCommandKind.Construct, construction);
            var frame = WorkshopWireFlatBuffers.EncodeCommand(command);
            Assert.Equal(2, WorkshopWireFlatBuffers.DecodeCommand(frame).Scene!.Physics.Transmissions.Length);
            var input = construction.Instances.Identities[new(3)].Hinges[0].Body;
            var output = construction.Instances.Identities[new(3)].Hinges[1].Body;
            var unlisted = physics.Colliders.ToArray().Single(c => c.Shape == CuriousContraptions.Gpu.ColliderShapeKind.Plane).Body;
            var priorTick = harness.Committed.Tick;
            var priorRevision = harness.Committed.Revision;
            foreach (var owner in new[] { new GpuBodyId(2), input, unlisted, new GpuBodyId(99) })
            {
                PhysicsSceneDeclaration Hostile() => new(physics.Document, physics.NextIdentity, physics.Bodies, physics.Colliders,
                    physics.Materials, physics.Sensors, physics.Guides, electricalSources: physics.Electrical.Sources,
                    supplyNodes: physics.SupplyGraph.Nodes, supplyEdges: physics.SupplyGraph.Edges,
                    revolutes: physics.Revolutes, revoluteDrives: physics.RevoluteDrives,
                    transmissions: physics.Transmissions.ToArray().Select(row => row.Id == internalRow.Id ? row with { Owner = owner } : row).ToArray());
                // A logical owner without a body cannot own physical transmission state even before package admission.
                if (owner == new GpuBodyId(99)) Assert.Throws<ArgumentException>(() => Hostile());
                else
                {
                    // Standalone physical existence remains legal; package installation owns authorization.
                    var hostile = Hostile();
                    Assert.Throws<ArgumentException>(() => new WorkshopCompiledScene(scene.Identity, scene.Revision, scene.Settings,
                        hostile, scene.Activation, scene.Presentation, scene.Owners, scene.RunControls));
                }
                RejectWire(frame, "owner", owner.Value);
            }
            // Remove only the external coupling from hostile-input fixtures so its occupied driving role cannot mask ownership.
            PhysicsSceneDeclaration WithEndpoints(MechanicalTransmissionDeclaration transmission) => new(
                physics.Document, physics.NextIdentity, physics.Bodies, physics.Colliders, physics.Materials, physics.Sensors,
                physics.Guides, electricalSources: physics.Electrical.Sources, supplyNodes: physics.SupplyGraph.Nodes,
                supplyEdges: physics.SupplyGraph.Edges, revolutes: physics.Revolutes, revoluteDrives: physics.RevoluteDrives,
                transmissions: [transmission]);
            var endpointControl = new WorkshopCompiledScene(scene.Identity, scene.Revision, scene.Settings, WithEndpoints(internalRow),
                scene.Activation, scene.Presentation, scene.Owners, scene.RunControls);
            var endpointFrame = WorkshopWireFlatBuffers.EncodeCommand(command with { Scene = endpointControl });
            Assert.Single(WorkshopWireFlatBuffers.DecodeCommand(endpointFrame).Scene!.Physics.Transmissions.ToArray());
            var foreignJoint = construction.Instances.Identities[new(2)].Hinges[0].Joint;
            foreach (var (transmission, field) in new[]
            {
                (internalRow with { Driving = new(foreignJoint) }, "driving_coordinate"),
                (internalRow with { Driven = new(foreignJoint) }, "driven_coordinate")
            })
            {
                Assert.Equal(internalRow.Owner, transmission.Owner);
                Assert.Equal(1, new[] { transmission.Driving == internalRow.Driving,
                    transmission.Driven == internalRow.Driven }.Count(unchanged => unchanged));
                var hostile = WithEndpoints(transmission); // Must admit physically: no missing/duplicate coordinate can mask the guard.
                Assert.Throws<ArgumentException>(() => new WorkshopCompiledScene(scene.Identity, scene.Revision, scene.Settings,
                    hostile, scene.Activation, scene.Presentation, scene.Owners, scene.RunControls));
                RejectWire(endpointFrame, field, foreignJoint.Value);
            }
            void RejectWire(byte[] validFrame, string field, ulong value)
            {
                var bad = (byte[])validFrame.Clone();
                var bytes = new Google.FlatBuffers.ByteBuffer(bad);
                static int Field(Google.FlatBuffers.ByteBuffer buffer, int table, int field) =>
                    table + buffer.GetUshort(table - buffer.GetInt(table) + WireSchemaLayout.VtableOffset(field));
                static int Target(Google.FlatBuffers.ByteBuffer buffer, int table, int field)
                {
                    var position = Field(buffer, table, field);
                    return position + buffer.GetInt(position);
                }
                var root = 4 + bytes.GetInt(4);
                var compiled = Target(bytes, root, WireSchemaLayout.Slot<Wire.WorkshopCommand>("scene"));
                var vector = Target(bytes, compiled, WireSchemaLayout.Slot<Wire.CompiledSceneRecord>("transmissions"));
                var changed = false;
                for (var index = 0; index < bytes.GetInt(vector); index++)
                {
                    var element = vector + 4 + index * 4;
                    var row = element + bytes.GetInt(element);
                    if (bytes.GetUlong(Field(bytes, row, WireSchemaLayout.Slot<Wire.MechanicalTransmissionRecord>("id"))) != internalRow.Id.Value) continue;
                    System.Buffers.Binary.BinaryPrimitives.WriteUInt64LittleEndian(bad.AsSpan(Field(bytes, row, WireSchemaLayout.Slot<Wire.MechanicalTransmissionRecord>(field))), value);
                    changed = true;
                }
                Assert.True(changed);
                Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => harness.Router.Dispatch(bad)).Reason);
                Assert.Same(scene, harness.Simulation.Scene);
                Assert.Equal(priorTick, harness.Committed.Tick);
                Assert.Equal(priorRevision, harness.Committed.Revision);
            }
            await harness.Run();
            await harness.Advance(36);
            Assert.Equal(TransmissionEngagementPhase.Engaged, SingleTransmission(harness.Committed, new(3)).Phase);
            var inputSpeed = BodyAngular(harness.Committed, input).Z;
            var outputSpeed = BodyAngular(harness.Committed, output).Z;
            Assert.True(inputSpeed > 1f);
            Assert.InRange(outputSpeed - inputSpeed, -CoupledSpeedAllowance, CoupledSpeedAllowance);
            await harness.Reset();
            Assert.Equal(Vector3.Zero, BodyAngular(harness.Committed, input));
            Assert.Equal(Vector3.Zero, BodyAngular(harness.Committed, output));
            Assert.Equal(TransmissionEngagementPhase.Engaged, SingleTransmission(harness.Committed, new(3)).Phase);
        });
    }

    [Fact]
    public async Task ObsoleteRuntimeBindingFramesAndUndefinedCurrentFieldsRefuseAtomically()
    {
        var construction = StandardConveyorRig(0.2f, includeBall: false);
        await using var harness = await WorkshopIntegrationHarness.Building(construction);
        var priorScene = harness.Simulation.Scene;
        var priorTick = harness.Committed.Tick;
        // historical layout: begin
        static byte[] Obsolete(bool command)
        {
            var builder = new Google.FlatBuffers.FlatBufferBuilder(1024);
            builder.StartTable(17);
            builder.AddUint(11, 1u, 0u); // removed phase field of the obsolete binding shape
            builder.AddFloat(13, 0.5f, 0f); // removed intermediate field
            var binding = builder.EndTable();
            builder.StartTable(11); builder.AddOffset(5, binding, 0);
            var track = builder.EndTable();
            builder.StartVector(4, 1, 4); builder.AddOffset(track);
            var tracks = builder.EndVector();
            builder.StartTable(36); builder.AddOffset(29, tracks.Value, 0);
            var scene = builder.EndTable();
            builder.StartTable(command ? 12 : 32); builder.AddOffset(command ? 11 : 27, scene, 0);
            var root = builder.EndTable();
            builder.FinishSizePrefixed(root, command ? "CCWC" : "CCWR");
            return builder.SizedByteArray();
        }
        // historical layout: end
        Assert.Equal(WireFaultReason.UnknownIdentifier, Assert.Throws<WireFaultException>(() => harness.Router.Dispatch(Obsolete(true))).Reason);
        Assert.Equal(WireFaultReason.UnknownIdentifier, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(Obsolete(false))).Reason);
        var current = WorkshopWireFlatBuffers.EncodeCommand(harness.Command(WorkshopCommandKind.Construct, construction));
        Assert.Equal(construction.Instances.Count, WorkshopWireFlatBuffers.DecodeCommand(current).Scene!.Owners.Length);
        var buffer = new Google.FlatBuffers.ByteBuffer(current);
        static int Target(Google.FlatBuffers.ByteBuffer bytes, int table, int field)
        {
            var vtable = table - bytes.GetInt(table);
            var location = table + bytes.GetUshort(vtable + WireSchemaLayout.VtableOffset(field));
            return location + bytes.GetInt(location);
        }
        var rootPosition = 4 + buffer.GetInt(4);
        var scenePosition = Target(buffer, rootPosition, WireSchemaLayout.Slot<Wire.WorkshopCommand>("scene"));
        var vector = Target(buffer, scenePosition, WireSchemaLayout.Slot<Wire.CompiledSceneRecord>("presentation"));
        var trackPosition = vector + 4 + buffer.GetInt(vector + 4);
        var bindingPosition = Target(buffer, trackPosition, WireSchemaLayout.Slot<Wire.CompiledPresentationRecord>("declaration"));
        var bindingVtable = bindingPosition - buffer.GetInt(bindingPosition);
        var beyondFields = WireSchemaLayout.VtableOffset(WireSchemaLayout.FieldCount<Wire.CapabilityPresentationBindingRecord>() + 1);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(current.AsSpan(bindingVtable), checked((ushort)beyondFields));
        Assert.Throws<WireFaultException>(() => harness.Router.Dispatch(current));
        Assert.Same(priorScene, harness.Simulation.Scene);
        Assert.Equal(priorTick, harness.Committed.Tick);
        await harness.Run();
        await harness.AdvanceOne();
        Assert.Equal(TransmissionEngagementPhase.Closing, SingleTransmission(harness.Committed, new(3)).Phase);
    }

    [Fact]
    public void HingeSupportIsRequiredAndRejectsMissingSelfAndCyclicReferences()
    {
        var entry = ChildSupportEntry();
        var json = BundleSchema.WriteEntries([entry]);
        Assert.Equal(new PackageBodyIndex(1), Assert.Single(BundleSchema.Read(json).Elements).Mechanical!.Hinges[1].Support);
        var document = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        document["Elements"]![0]!["Mechanical"]!["Hinges"]![0]!.AsObject().Remove("Support");
        Assert.Throws<System.Text.Json.JsonException>(() => BundleSchema.Read(document.ToJsonString()));
        foreach (var support in new[] { -1, 2, 99 })
        {
            var hinges = entry.Mechanical!.Hinges.ToArray();
            hinges[1] = hinges[1] with { Support = new(support) };
            Assert.Contains(BundleValidator.Validate([new("probe", entry with { Mechanical = entry.Mechanical with { Hinges = hinges } })]),
                error => error.Detail.Contains("Hinges.Support", StringComparison.Ordinal));
        }
        var cycle = entry.Mechanical!.Hinges.ToArray();
        cycle[0] = cycle[0] with { Support = new(2) };
        Assert.Contains(BundleValidator.Validate([new("probe", entry with { Mechanical = entry.Mechanical with { Hinges = cycle } })]),
            error => error.Detail.Contains("Hinges.Support", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(-91f, false)]
    [InlineData(-90f, true)]
    [InlineData(30f, true)]
    [InlineData(90f, true)]
    [InlineData(91f, false)]
    public void AuthoredHingeDegreesAreValidatedAgainstRadianLimits(float degrees, bool accepted)
    {
        var entry = ChildSupportEntry();
        var hinges = entry.Mechanical!.Hinges.ToArray();
        hinges[0] = hinges[0] with { InitialAngle = MechanicalValue.Fixed(degrees), LowerLimit = -MathF.PI / 2f,
            UpperLimit = MathF.PI / 2f, Travel = RevoluteTravel.Limited };
        var errors = BundleValidator.Validate([new("probe", entry with { Mechanical = entry.Mechanical with { Hinges = hinges } })]);
        Assert.Equal(!accepted, errors.Any(error => error.Detail.Contains("InitialAngle", StringComparison.Ordinal)));
    }

    [Fact]
    public Task ChildSupportedDriveAcceptsOwnedTargetChangesAndRefusesForeignRows()
    {
        var entry = ChildSupportEntry();
        var mechanical = entry.Mechanical!;
        var hinges = mechanical.Hinges.ToArray();
        hinges[1] = hinges[1] with { Drive = new(MechanicalValue.FromParameter("speed"), MechanicalValue.Fixed(20f)) };
        entry = entry with
        {
            Metadata = entry.Metadata with { Parameters = [new("speed", ParameterUnit.RadianPerSecond, ParameterKind.Real,
                0f, 20f, 6f, [], RunEdit: ParameterRunEdit.RunControl)] },
            Mechanical = mechanical with { Hinges = hinges, Transmissions = [] },
            Electrical = entry.Electrical! with { SupplyGraph = new(
                [new SupplyConsumerDescriptor(new("consumer"), [new SupplyDriveTarget(new(CoordinateKind.Revolute, 1))])], [],
                [new(PortId.PowerIn, new(new("consumer"), SupplyTerminal.PowerIn))]) },
            Presentation = entry.Presentation with { Bindings = [
                new("Visual/Lamp", PresentationProperty.LocalY, PresentationDriver.CommittedActuatorDrive,
                    PresentationValueMapping.Identity, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                    Actuator: new(CoordinateKind.Revolute, 1)),
                new("Visual/Lamp", PresentationProperty.LocalZ, PresentationDriver.CommittedActuatorState,
                    PresentationValueMapping.StateValues, 0f, 1f, PresentationCurve.Linear, 0f, PresentationOverlap.Replace, 0f,
                    Actuator: new(CoordinateKind.Revolute, 1), StateValues: new(0f, 1f, 2f))] }
        };
        return InSyntheticWorld(entry, async (harness, construction) =>
        {
            var scene = harness.Simulation.Scene;
            var row = Assert.Single(scene.RunControls.Where(r => r.Owner == new GpuBodyId(3)));
            var identities = construction.Instances.Identities[new(3)];
            await harness.Run();
            await harness.Advance(36);
            await harness.Require(WorkshopCommandKind.ConfigureRunControl,
                runControl: harness.RunControl(new(3), RunControlLane.RevoluteDriveTarget, RunControlValue.Of(12f)));
            await harness.Advance(36);
            Assert.Equal(12f, harness.Committed.RunControl(scene.Physics, row).Scalar);
            var poweredSamples = harness.Presentation!.Advance(new PhysicalTime(harness.Committed.Tick.Value * 4)).Where(sample => sample.Owner == new GpuBodyId(3)).ToArray();
            Assert.Equal(2, poweredSamples.Length);
            Assert.All(poweredSamples, sample => Assert.Equal(1f, sample.Value.Number));
            var refusal = (await harness.Execute(WorkshopCommandKind.ConfigureRunControl,
                runControl: new(new(2), row.Id, RunControlValue.Of(0f)))).Result;
            Assert.Equal((WorkshopCommandOutcome.Rejected, WorkshopRejection.ForeignRunControl), (refusal.Outcome, refusal.Reason));
            Assert.Equal(12f, harness.Committed.RunControl(scene.Physics, row).Scalar);
            var target = Assert.Single(Enumerable.Range(0, harness.Committed.DriveTargets.Count).Select(i => harness.Committed.DriveTargets[i]), t => t.Joint == identities.Hinges[1].Joint);
            Assert.Equal(identities.Hinges[0].Body, target.Owner);
            var forgedTargets = Enumerable.Range(0, harness.Committed.DriveTargets.Count)
                .Select(i => harness.Committed.DriveTargets[i]).Select(t => t.Joint == target.Joint ? t with { Owner = new(3) } : t).ToArray();
            var forgedRead = harness.Committed with { DriveTargets = new(forgedTargets) };
            Assert.Throws<ArgumentException>(() => forgedRead.RunControl(scene.Physics, row));
            Assert.Throws<ArgumentException>(() => forgedRead.RunControl(scene.Physics, new(3), RunControlLane.RevoluteDriveTarget, joint: target.Joint));
            var actuator = Assert.Single(harness.Committed.Presentation.Actuators.ToArray(), a => a.Drive == SupplyConsumer.Revolute(target.Joint));
            Assert.InRange(actuator.Speed, 12f - CoupledSpeedAllowance, 12f + CoupledSpeedAllowance);
            Assert.Throws<ArgumentException>(() => harness.Committed.RunControl(scene.Physics, row with { Owner = new(2) }));
            Assert.Throws<ArgumentException>(() => new WorkshopCompiledScene(scene.Identity, scene.Revision, scene.Settings,
                scene.Physics, scene.Activation, scene.Presentation, scene.Owners,
                scene.RunControls.Select(r => r.Id == row.Id ? r with { Owner = new(2) } : r)));
            await harness.Require(WorkshopCommandKind.ConfigureRunControl,
                runControl: harness.RunControl(new(1), RunControlLane.SourceEnable, RunControlValue.Of(ElectricalEnable.Disabled)));
            await harness.Advance(6);
            Assert.False(Assert.Single(harness.Committed.Presentation.Actuators.ToArray(), a => a.Drive == SupplyConsumer.Revolute(target.Joint)).SupplyClosed);
            var unpoweredSamples = harness.Presentation!.Advance(new PhysicalTime(harness.Committed.Tick.Value * 4)).Where(sample => sample.Owner == new GpuBodyId(3)).ToArray();
            Assert.Equal(2, unpoweredSamples.Length);
            Assert.All(unpoweredSamples, sample => Assert.Equal(0f, sample.Value.Number));
            await harness.Reset();
            Assert.Equal(6f, harness.Committed.RunControl(scene.Physics, row).Scalar);
        });
    }

    [Fact]
    public async Task TransmissionOwnershipAndMissingJointRefuseAtAdmissionAndReadBoundaries()
    {
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(PlacedParts.Battery(new(1), -8f, RigY, 0f), ClutchPart()),
            Connections: new([Power(1, 3)]));
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var scene = harness.Simulation.Scene;
        var source = scene.Physics;
        var transmission = Assert.Single(source.Transmissions.ToArray());
        PhysicsSceneDeclaration WithTransmission(MechanicalTransmissionDeclaration value) =>
            new(source.Document, source.NextIdentity, source.Bodies, source.Colliders,
                source.Materials, source.Sensors, source.Guides, electricalSources: source.Electrical.Sources,
                supplyNodes: source.SupplyGraph.Nodes, supplyEdges: source.SupplyGraph.Edges,
                revolutes: source.Revolutes, transmissions: [value]);
        _ = WithTransmission(transmission);
        Assert.Throws<ArgumentException>(() => WithTransmission(transmission with { Driving = new(new RevoluteConstraintId(ulong.MaxValue)) }));
        Assert.Throws<ArgumentException>(() => WithTransmission(transmission with { Owner = new(ulong.MaxValue) }));
        // An existing but different owner still cannot consume this package's supplied transmission.
        Assert.Throws<ArgumentException>(() => WithTransmission(transmission with { Owner = new(1) }));
        var read = harness.Committed;
        var presentation = read.Presentation;
        var observed = Assert.Single(presentation.Transmissions.ToArray());
        WorkshopRead WithRows(params MechanicalTransmissionRead[] rows) => read with
        {
            Presentation = new(presentation.Actuators, presentation.Loads, presentation.Logic,
                presentation.Ropes, presentation.Guides, presentation.Surfaces, rows)
        };
        WorkshopMessages.ValidatePresentationOwners(WithRows(observed), scene);
        Assert.Throws<ArgumentException>(() => WorkshopMessages.ValidatePresentationOwners(WithRows(), scene));
        Assert.Throws<ArgumentException>(() => WorkshopMessages.ValidatePresentationOwners(
            WithRows(observed with { Owner = new(1) }), scene));
        Assert.Throws<ArgumentException>(() => WorkshopMessages.ValidatePresentationOwners(
            WithRows(observed with { Transmission = new(ulong.MaxValue) }), scene));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(17)]
    public async Task TransmissionTablesFollowActualDemandAndResetEverySlot(int count)
    {
        var parts = Enumerable.Range(0, count).Select(i => (IWorkshopInstance)ClutchPart((ulong)i + 3,
            position: new Vector3(i * 3f, RigY, 0f))).ToArray();
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(parts));
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        await harness.Advance(2);
        var rows = harness.Committed.Presentation.Transmissions.ToArray();
        Assert.Equal(count, rows.Length);
        Assert.All(rows, row => Assert.Equal(TransmissionEngagementPhase.Open, row.Phase));
        await harness.Reset();
        Assert.Equal(count, harness.Committed.Presentation.Transmissions.Length);
        Assert.All(harness.Committed.Presentation.Transmissions.ToArray(), row => Assert.Equal(0f, row.Closure));
    }

    [Fact]
    public void OccupiedMechanicalInputIsRefusedWhenLoadingSavedConstruction()
    {
        var valid = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(MotorPart(10), MotorPart(11, position: new Vector3(-8f, RigY, 0f)), ClutchPart(12)),
            Connections: new([Drive(10, 12)]));
        var invalid = valid with { Connections = valid.Connections.With(Drive(11, 12)) };
        var bytes = SaveFixture.Forged(invalid, 13);
        SaveFixture.Refused(WorkshopSaveRejection.InvalidConnection, () =>
            WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
                RuntimeTestServices.Bundle, SaveFixture.Elements, bytes));
        var canonical = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(valid, new(13)));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements, canonical);
        Assert.Equal(valid.Connections, loaded.Construction.Connections);
    }

    [Fact]
    public async Task HoldTimerSupplyReleasesCouplingWhileMotorKeepsRunning()
    {
        var ball = WorkshopInput.Generic(RuntimeTestServices.Bundle, BallId, new(7), 0f, 3f, 0f, 0f, 0f, 0f, 1f);
        var trigger = WorkshopInput.Generic(RuntimeTestServices.Bundle, SwitchId, new(8), 0f, 1f, 0f, 0f, 0f, 0f, 1f);
        var hold = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("hold_timer"), new(9), 4f, 2f, 0f, 0f, 0f, 0f, 1f,
            new([new(new("hold_seconds"), 0.6f)]));
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(PlacedParts.Battery(new(1), -8.5f, RigY, 0f), MotorPart(), ClutchPart(), ball, trigger, hold),
            Connections: new([Power(1, 2), Power(1, 9), Power(9, 3), Drive(2, 3),
                new(new(8), PortId.ActivationOut, new(9), PortId.ActivationIn, WorkshopConnectionDomain.Activation)]));
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var engaged = false;
        var released = false;
        for (var tick = 0; tick < 600; tick++)
        {
            await harness.AdvanceOne();
            var phase = SingleTransmission(harness.Committed, new(3)).Phase;
            engaged |= phase == TransmissionEngagementPhase.Engaged;
            if (engaged && phase == TransmissionEngagementPhase.Opening)
            {
                released = true;
                var ids = construction.Instances.Identities[new(3)];
                Assert.True(BodyAngular(harness.Committed, ids.Hinges[0].Body).Length() > 1f);
                Assert.True(BodyAngular(harness.Committed, ids.Hinges[1].Body).Length() > 1f);
                break;
            }
        }
        Assert.True(engaged, "The timed electrical supply must first engage the coupling.");
        Assert.True(released, "The hold timer must release while the motor continues spinning.");
    }
}

