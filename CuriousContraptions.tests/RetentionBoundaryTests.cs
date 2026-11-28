using System.Buffers.Binary;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Bundle;
using CuriousContraptions.Gpu;
using Google.FlatBuffers;
using Wire = CuriousContraptions.Wire;

namespace CuriousContraptions.Tests;

public sealed class RetentionBoundaryTests
{
    public enum Tag { Mode, Phase, Result }
    private static WorkshopConstruction Construction() => new(new(2), WorkshopCadenceSettings.Default(),
        new(WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wound_spring"), new(1), 0, 4, 0, 0, 0, 0, 1)));

    [Fact]
    public void NullRetentionCollectionReturnsCollectionAndReferringRequestErrors()
    {
        var entry = TestBundle.Entry(new("wound_spring"));
        var invalid = entry with { Mechanical = entry.Mechanical! with { Retentions = null! } };
        var errors = BundleValidator.Validate([new("invalid", invalid)]);
        Assert.Contains(errors, e => e.Code == BundleErrorCode.DeclarationCount && e.Detail.EndsWith(".Retentions", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.EndsWith(".Signal.PhysicalRequest", StringComparison.Ordinal));
        Assert.Empty(BundleValidator.Validate([new("valid", entry)]));
    }

    [Fact]
    public void NullTransmissionCollectionReturnsCollectionAndReferringRetentionErrors()
    {
        var entry = TestBundle.Entry(new("wound_spring"));
        var invalid = entry with { Mechanical = entry.Mechanical! with { Transmissions = null! } };
        var errors = BundleValidator.Validate([new("invalid", invalid)]);
        Assert.Contains(errors, e => e.Code == BundleErrorCode.DeclarationCount && e.Detail.EndsWith(".Transmissions", StringComparison.Ordinal));
        Assert.Contains(errors, e => e.Code == BundleErrorCode.OutOfRange && e.Detail.EndsWith(".Retentions.WindingTransmission", StringComparison.Ordinal));
        Assert.Empty(BundleValidator.Validate([new("valid", entry)]));
    }

    [Theory]
    [InlineData(Tag.Mode, 2u)]
    [InlineData(Tag.Mode, 256u)]
    [InlineData(Tag.Mode, uint.MaxValue)]
    [InlineData(Tag.Phase, 5u)]
    [InlineData(Tag.Phase, 256u)]
    [InlineData(Tag.Phase, uint.MaxValue)]
    [InlineData(Tag.Result, 4u)]
    [InlineData(Tag.Result, 257u)]
    [InlineData(Tag.Result, uint.MaxValue)]
    public async Task FullWidthUnknownAbiTagsCannotNarrowIntoCanonicalValues(Tag field, uint raw)
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Construction());
        var source = harness.Device.CommittedWorld.ToArray();
        await harness.AdvanceOne();
        var candidate = harness.Device.CommittedWorld.ToArray();
        var scene = harness.Simulation.Scene.Physics;
        var relation = Assert.Single(scene.Transmissions.ToArray());
        var fieldOffset = field switch
        {
            Tag.Mode => PhysicsGpuAbi.MechanicalTransmissionRetentionModeOffset,
            Tag.Phase => PhysicsGpuAbi.MechanicalTransmissionRetentionPhaseOffset,
            Tag.Result => PhysicsGpuAbi.MechanicalTransmissionRequestResultOffset,
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        };
        var offset = scene.Layout.GetMechanicalTransmissionOffset(relation.Id.Value) + fieldOffset;
        BinaryPrimitives.WriteUInt32LittleEndian(candidate.AsSpan(offset), raw);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadRetentions(candidate));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate, source,
            harness.Committed.Tick, PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
    }

    [Fact]
    public async Task EveryCanonicalRetentionStateAndResultSurvivesActualAbiAndResponseCodecs()
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Construction());
        var original = Assert.Single(harness.Committed.Presentation.Retentions.ToArray());
        var scene = harness.Simulation.Scene.Physics;
        var offset = scene.Layout.GetMechanicalTransmissionOffset(Assert.Single(scene.Transmissions.ToArray()).Id.Value);
        foreach (var mode in Enum.GetValues<RetentionMode>())
        foreach (var phase in Enum.GetValues<RetentionPhase>())
        foreach (var result in new RetentionRequestResult?[] { null, RetentionRequestResult.Released, RetentionRequestResult.Empty, RetentionRequestResult.AlreadyReleased })
        {
            var bytes = harness.Device.CommittedWorld.ToArray();
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionRetentionModeOffset), (uint)mode);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionRetentionPhaseOffset), (uint)phase);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionRequestResultOffset), result.HasValue ? (uint)result.Value + 1u : 0u);
            if ((mode == RetentionMode.Releasing && phase is not (RetentionPhase.Releasing or RetentionPhase.Blocked)) ||
                (mode == RetentionMode.Retained && phase == RetentionPhase.Releasing))
            {
                Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadRetentions(bytes));
                continue;
            }
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionClosureOffset), mode == RetentionMode.Releasing ? 0f : 1f);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionPhaseOffset), (uint)(mode == RetentionMode.Releasing ? TransmissionEngagementPhase.Open : TransmissionEngagementPhase.Engaged));
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionImpulseOffset), 0f);
            var abi = Assert.Single(PhysicsGpuAbi.ReadRetentions(bytes));
            Assert.Equal(mode, abi.Mode); Assert.Equal(phase, abi.Phase); Assert.Equal(result, abi.RequestResult);
            var expected = original with { Mode = mode, Phase = phase, RequestResult = result,
                Compression = .25f, Potential = 5f, AcceptedElasticEnergy = 6f, ReleasedElasticEnergy = 1f,
                WindingWork = -2f, PositiveWindingTransfer = 3f, ContactWork = 4f, GravityWork = -.5f };
            var response = harness.Latest with { Read = WithRetention(harness.Committed, [expected]) };
            var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response));
            Assert.Equal(expected, Assert.Single(decoded.Read.Presentation.Retentions.ToArray()));
        }
    }

    [Theory]
    [InlineData(Tag.Mode)]
    [InlineData(Tag.Phase)]
    [InlineData(Tag.Result)]
    public async Task UnknownResponseTagsAreTypedRefusals(Tag field)
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Construction());
        var retention = Assert.Single(harness.Committed.Presentation.Retentions.ToArray()) with
            { Mode = RetentionMode.Releasing, Phase = RetentionPhase.Blocked, RequestResult = RetentionRequestResult.AlreadyReleased };
        var response = harness.Latest with { Read = WithRetention(harness.Committed, [retention]) };
        var bytes = WorkshopWireFlatBuffers.EncodeResponse(response);
        var buffer = new ByteBuffer(bytes);
        var root = 4 + buffer.GetInt(4);
        var table = new Table(root, buffer);
        var vector = table.__vector(table.__offset(WireSchemaLayout.VtableOffset<Wire.WorkshopResponse>("retentions")));
        var row = new Table(table.__indirect(vector), buffer);
        var slot = WireSchemaLayout.VtableOffset<Wire.PrismaticRetentionReadRecord>(field switch
        {
            Tag.Mode => "mode",
            Tag.Phase => "phase",
            Tag.Result => "request_result",
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        });
        var at = row.__offset(slot);
        Assert.NotEqual(0, at);
        bytes[row.bb_pos + at] = 255;
        Assert.Equal(WireFaultReason.OutOfRange,
            Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(bytes)).Reason);
        Assert.Null(Assert.Single(harness.Committed.Presentation.Retentions.ToArray()).RequestResult);
    }

    [Fact]
    public async Task DuplicateReadComparisonIncludesEveryRetentionFieldAndPopulation()
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Construction());
        var read = harness.Committed;
        var retention = Assert.Single(read.Presentation.Retentions.ToArray());
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(harness.Latest)).Read;
        Assert.Equal(retention, Assert.Single(decoded.Presentation.Retentions.ToArray()));
        Assert.True(read.HasSameContent(decoded));
        var variations = new[] {
            retention with { Id = new(retention.Id.Value + 1) }, retention with { Owner = new(2) },
            retention with { Coordinate = new(retention.Coordinate.Value + 1) },
            retention with { Mode = RetentionMode.Releasing }, retention with { Phase = RetentionPhase.Blocked },
            retention with { Compression = .1f }, retention with { Potential = 1f },
            retention with { AcceptedElasticEnergy = 1f }, retention with { ReleasedElasticEnergy = 1f },
            retention with { WindingWork = -1f }, retention with { PositiveWindingTransfer = 1f },
            retention with { ContactWork = 1f }, retention with { GravityWork = -1f },
            retention with { RequestResult = RetentionRequestResult.Empty } };
        foreach (var changed in variations)
        {
            Assert.False(read.HasSameContent(WithRetention(read, [changed])));
            Assert.NotEqual(read, WithRetention(read, [changed]));
        }
        Assert.False(read.HasSameContent(WithRetention(read, [])));
        Assert.False(read.HasSameContent(WithRetention(read, [retention, retention with { Id = new(retention.Id.Value + 1) }])));
    }


    public enum DeclarationTag { Driving, Driven, Constraint }

    [Theory]
    [InlineData(DeclarationTag.Driving, 257u, false)]
    [InlineData(DeclarationTag.Driving, 258u, false)]
    [InlineData(DeclarationTag.Driving, uint.MaxValue, false)]
    [InlineData(DeclarationTag.Driving, 0u, false)]
    [InlineData(DeclarationTag.Driven, 257u, false)]
    [InlineData(DeclarationTag.Driven, 258u, false)]
    [InlineData(DeclarationTag.Driven, uint.MaxValue, false)]
    [InlineData(DeclarationTag.Driven, 0u, false)]
    [InlineData(DeclarationTag.Constraint, 257u, false)]
    [InlineData(DeclarationTag.Constraint, 258u, false)]
    [InlineData(DeclarationTag.Constraint, uint.MaxValue, false)]
    [InlineData(DeclarationTag.Constraint, 2u, false)]
    [InlineData(DeclarationTag.Constraint, 256u, true)]
    [InlineData(DeclarationTag.Constraint, uint.MaxValue, true)]
    public async Task UnknownTransmissionDeclarationTagsCannotNarrowIntoCanonicalValues(DeclarationTag field, uint raw, bool bilateral)
    {
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(WorkshopInput.Generic(RuntimeTestServices.Bundle, new(bilateral ? "reverse_transmission" : "wound_spring"),
                new(1), 0, 4, 0, 0, 0, 0, 1)));
        await using var harness = await WorkshopIntegrationHarness.Running(construction);
        var source = harness.Device.CommittedWorld.ToArray();
        await harness.AdvanceOne();
        var original = PhysicsGpuAbi.ReadMechanicalTransmission(harness.Device.CommittedWorld.Span, 0);
        Assert.Equal(CoordinateKind.Revolute, original.Driving.Kind);
        Assert.Equal(bilateral ? CoordinateKind.Revolute : CoordinateKind.Prismatic, original.Driven.Kind);
        Assert.Equal(bilateral ? TransmissionConstraintKind.Bilateral : TransmissionConstraintKind.NonPositiveVelocity, original.ConstraintKind);
        var candidate = harness.Device.CommittedWorld.ToArray();
        var fieldOffset = field switch {
            DeclarationTag.Driving => PhysicsGpuAbi.MechanicalTransmissionDrivingKindOffset,
            DeclarationTag.Driven => PhysicsGpuAbi.MechanicalTransmissionDrivenKindOffset,
            DeclarationTag.Constraint => PhysicsGpuAbi.MechanicalTransmissionKindOffset,
            _ => throw new ArgumentOutOfRangeException(nameof(field)) };
        var offset = harness.Simulation.Scene.Physics.Layout.GetMechanicalTransmissionOffset(original.Id.Value) + fieldOffset;
        BinaryPrimitives.WriteUInt32LittleEndian(candidate.AsSpan(offset), raw);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadMechanicalTransmission(candidate, 0));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate, source,
            harness.Committed.Tick, PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
    }

    [Theory]
    [InlineData(CoordinateKind.Revolute, CoordinateKind.Revolute)]
    [InlineData(CoordinateKind.Revolute, CoordinateKind.Prismatic)]
    [InlineData(CoordinateKind.Prismatic, CoordinateKind.Revolute)]
    [InlineData(CoordinateKind.Prismatic, CoordinateKind.Prismatic)]
    public void CanonicalBilateralCoordinatePairsSurviveAdmissionAndCommittedAbi(CoordinateKind driving, CoordinateKind driven)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var first = Coordinate(driving, 0f);
        var second = Coordinate(driven, 2f);
        scene.Transmission(first, second, .5f);
        var state = scene.Admit().Step(1);
        var read = PhysicsGpuAbi.ReadMechanicalTransmission(state.World, 0);
        Assert.Equal(first, read.Driving);
        Assert.Equal(second, read.Driven);
        Assert.Equal(TransmissionConstraintKind.Bilateral, read.ConstraintKind);
        Assert.Equal(.5f, read.Ratio);
        ConstraintCoordinateReference Coordinate(CoordinateKind kind, float x)
        {
            var body = scene.Box(new(x, 5, 0), new(.1f), 1f, LawMaterial.Domino, gravity: 0);
            var support = new ConstraintFrame(new(x, 0, 0), System.Numerics.Quaternion.Identity);
            var moving = new ConstraintFrame(System.Numerics.Vector3.Zero, System.Numerics.Quaternion.Identity);
            return kind == CoordinateKind.Prismatic
                ? new(scene.Prismatic(frame, body, support, moving, -1f, 1f, 0f, 0f, ConnectedCollision.Disabled))
                : new(scene.Revolute(frame, body, support, moving, -MathF.PI, MathF.PI, 0f, travel: RevoluteTravel.Continuous));
        }
    }

    public enum MonotonicCounter { PositiveWindingTransfer, AcceptedElasticEnergy, ReleasedElasticEnergy }

    [Theory]
    [InlineData(MonotonicCounter.PositiveWindingTransfer)]
    [InlineData(MonotonicCounter.AcceptedElasticEnergy)]
    [InlineData(MonotonicCounter.ReleasedElasticEnergy)]
    public async Task UnilateralPositiveImpulseAndReversedEnergyCountersAreRefused(MonotonicCounter counter)
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Construction());
        var source = harness.Device.CommittedWorld.ToArray();
        await harness.AdvanceOne();
        var candidate = harness.Device.CommittedWorld.ToArray();
        var scene = harness.Simulation.Scene.Physics;
        var relation = Assert.Single(scene.Transmissions.ToArray());
        var offset = scene.Layout.GetMechanicalTransmissionOffset(relation.Id.Value);

        // Control: the unmodified committed pair is a valid candidate.
        PhysicsGpuAbi.ValidateCandidate(candidate, source, harness.Committed.Tick, PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);

        var impulseBytes = candidate.ToArray();
        BinaryPrimitives.WriteSingleLittleEndian(impulseBytes.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionImpulseOffset), .01f);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadMechanicalTransmission(impulseBytes, 0));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(impulseBytes, source,
            harness.Committed.Tick, PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));

        var counterOffset = offset + counter switch
        {
            MonotonicCounter.PositiveWindingTransfer => PhysicsGpuAbi.MechanicalTransmissionPositiveWorkOffset,
            MonotonicCounter.AcceptedElasticEnergy => PhysicsGpuAbi.MechanicalTransmissionAcceptedOffset,
            MonotonicCounter.ReleasedElasticEnergy => PhysicsGpuAbi.MechanicalTransmissionReleasedOffset,
            _ => throw new ArgumentOutOfRangeException(nameof(counter))
        };
        BinaryPrimitives.WriteSingleLittleEndian(source.AsSpan(counterOffset), 1f);
        // Controls: an equal or increased counter is accepted.
        foreach (var admitted in new[] { 1f, 1.5f })
        {
            BinaryPrimitives.WriteSingleLittleEndian(candidate.AsSpan(counterOffset), admitted);
            PhysicsGpuAbi.ValidateCandidate(candidate, source, harness.Committed.Tick,
                PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty);
        }
        BinaryPrimitives.WriteSingleLittleEndian(candidate.AsSpan(counterOffset), .5f);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate, source,
            harness.Committed.Tick, PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
    }

    [Fact]
    public async Task PresentationOwnerValidationRefusesMalformedRetentionPopulationsIdentitiesAndAdmissionState()
    {
        var first = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wound_spring"), new(1), -3, 4, 0, 0, 0, 0, 1);
        var second = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wound_spring"), new(2), 3, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(first, second));
        var compiled = CompiledSceneFixture.Compile(construction);
        await using var harness = await WorkshopIntegrationHarness.Building(construction);
        var built = harness.Committed;
        WorkshopMessages.ValidatePresentationOwners(built, compiled);
        var retentions = built.Presentation.Retentions.ToArray();
        Assert.Equal(2, retentions.Length);

        Assert.Throws<ArgumentException>(() => WorkshopMessages.ValidatePresentationOwners(WithRetention(built, [retentions[0]]), compiled));
        Assert.Throws<ArgumentException>(() => WorkshopMessages.ValidatePresentationOwners(WithRetention(built, [retentions[1], retentions[0]]), compiled));
        foreach (var invalid in new[]
        {
            retentions[0] with { Id = new(999) },
            retentions[0] with { Owner = new(999) },
            retentions[0] with { Coordinate = new(999) },
            retentions[0] with { Mode = RetentionMode.Releasing, Phase = RetentionPhase.Releasing },
            retentions[0] with { Compression = .1f },
            retentions[0] with { AcceptedElasticEnergy = .1f },
            retentions[0] with { ReleasedElasticEnergy = .1f },
            retentions[0] with { WindingWork = -.1f },
            retentions[0] with { PositiveWindingTransfer = .1f },
            retentions[0] with { ContactWork = .1f },
            retentions[0] with { GravityWork = -.1f },
            retentions[0] with { RequestResult = RetentionRequestResult.Empty }
        })
        {
            Assert.Throws<ArgumentException>(() =>
                WorkshopMessages.ValidatePresentationOwners(WithRetention(built, [invalid, retentions[1]]), compiled));
        }
    }

    public enum SceneRetentionDefect { ForeignPhysicalRequest, ForeignDrivenPrismatic, MissingTrackRetention, ForeignTrackRetention }

    [Theory]
    [InlineData(SceneRetentionDefect.ForeignPhysicalRequest)]
    [InlineData(SceneRetentionDefect.ForeignDrivenPrismatic)]
    [InlineData(SceneRetentionDefect.MissingTrackRetention)]
    [InlineData(SceneRetentionDefect.ForeignTrackRetention)]
    public async Task MalformedCompiledAndWireRetentionReferencesRefuseAtomically(SceneRetentionDefect defect)
    {
        var first = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wound_spring"), new(1), -3, 4, 0, 0, 0, 0, 1);
        var second = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wound_spring"), new(2), 3, 4, 0, 0, 0, 0, 1);
        var construction = new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(), new(first, second));
        await using var world = await WorkshopIntegrationHarness.Building(construction);
        var before = world.Committed;
        var original = world.Simulation.Scene;
        var physics = original.Physics;
        var transmissions = physics.Transmissions.ToArray();
        var nodes = original.Activation.Nodes.ToArray();
        var tracks = original.Presentation.ToArray();
        var secondRetention = transmissions.Single(t => t.Owner == second.Id).Retention!.Value;
        var secondPrismatic = transmissions.Single(t => t.Owner == second.Id).Driven;

        Assert.Throws<ArgumentException>(() =>
        {
            switch (defect)
            {
                case SceneRetentionDefect.ForeignPhysicalRequest:
                {
                    var idx = Array.FindIndex(nodes, n => n.Owner == first.Id && n.Kind == ActivationNodeKind.PhysicalRequest);
                    nodes[idx] = nodes[idx] with { RequestTarget = PhysicalRequestTarget.ForRetention(secondRetention) };
                    var activation = new ActivationNetwork(nodes, original.Activation.Edges, original.Activation.Substeps,
                        original.Activation.PassageSensors, original.Activation.Contacts.ToArray());
                    _ = new WorkshopCompiledScene(original.Identity, original.Revision, original.Settings, physics, activation, tracks, original.Owners, original.RunControls, original.MechanicalLinks);
                    break;
                }
                case SceneRetentionDefect.ForeignDrivenPrismatic:
                {
                    var idx = Array.FindIndex(transmissions, t => t.Owner == first.Id);
                    transmissions[idx] = transmissions[idx] with { Driven = secondPrismatic };
                    var invalidPhysics = new PhysicsSceneDeclaration(physics.Document, physics.NextIdentity, physics.Bodies, physics.Colliders,
                        physics.Materials, physics.Sensors, physics.Guides, physics.Triggers, physics.ContactWorks, physics.OrientationSensors,
                        physics.Electrical.Sources.ToArray(), physics.Electrical.Bindings.ToArray(), physics.Prismatics,
                        physics.SupplyGraph.Nodes.ToArray(), physics.SupplyGraph.Edges.ToArray(), physics.ContactLoadSensors,
                        physics.PrismaticDrives, physics.TensileSpans, physics.Revolutes, physics.CompliantSurfaces, physics.RevoluteDrives,
                        physics.RopeGuides, transmissions, physics.DrivenSurfaces);
                    _ = new WorkshopCompiledScene(original.Identity, original.Revision, original.Settings, invalidPhysics, original.Activation, tracks, original.Owners, original.RunControls, original.MechanicalLinks);
                    break;
                }
                case SceneRetentionDefect.MissingTrackRetention:
                case SceneRetentionDefect.ForeignTrackRetention:
                {
                    var idx = Array.FindIndex(tracks, t => t.Owner == first.Id && t.Declaration.Driver == PresentationDriver.CommittedRetentionReleased);
                    tracks[idx] = tracks[idx] with { Retention = defect == SceneRetentionDefect.MissingTrackRetention ? default : secondRetention };
                    _ = new WorkshopCompiledScene(original.Identity, original.Revision, original.Settings, physics, original.Activation, tracks, original.Owners, original.RunControls, original.MechanicalLinks);
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(defect));
            }
        });

        var request = WorkshopWireFlatBuffers.EncodeCommand(world.Command(WorkshopCommandKind.Construct, construction with { Revision = new(3) }));
        var response = WorkshopWireFlatBuffers.EncodeResponse(world.Latest with { Scene = original });
        Poison(request, WireSchemaLayout.VtableOffset<Wire.WorkshopCommand>("scene"));
        Poison(response, WireSchemaLayout.VtableOffset<Wire.WorkshopResponse>("scene"));
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => world.Router.Dispatch(request)).Reason);
        Assert.Equal(WireFaultReason.OutOfRange, Assert.Throws<WireFaultException>(() => WorkshopWireFlatBuffers.DecodeResponse(response)).Reason);
        Assert.Same(original, world.Simulation.Scene);
        Assert.Equal(before, world.Committed);

        void Poison(byte[] bytes, int sceneField)
        {
            var buffer = new ByteBuffer(bytes);
            var scene = CompiledSceneBoundaryTests.Child(new Table(4 + buffer.GetInt(4), buffer), sceneField);
            switch (defect)
            {
                case SceneRetentionDefect.ForeignPhysicalRequest:
                    var node = CompiledSceneBoundaryTests.Rows(scene, WireSchemaLayout.VtableOffset<Wire.CompiledSceneRecord>("activation_nodes"))
                        .Single(row =>
                        {
                            var decoded = new Wire.CapabilityActivationNodeDeclarationRecord().__assign(row.bb_pos, buffer);
                            return decoded.Owner == first.Id.Value && decoded.Kind == (uint)ActivationNodeKind.PhysicalRequest;
                        });
                    var targetOffset = WireSchemaLayout.VtableOffset<Wire.CapabilityActivationNodeDeclarationRecord>("request_target");
                    var position = CompiledSceneBoundaryTests.Slot(node, targetOffset);
                    var target = new Wire.PhysicalRequestTargetRecord().__assign(position, buffer);
                    Assert.Equal((uint)PhysicalRequestTargetKind.Retention, target.Kind);
                    Assert.NotEqual(secondRetention.Value, target.Retention);
                    CompiledSceneBoundaryTests.ReplaceStruct(bytes.AsSpan(position), builder => Wire.PhysicalRequestTargetRecord
                        .CreatePhysicalRequestTargetRecord(builder, Kind: target.Kind, Retention: secondRetention.Value, Actuator: target.Actuator));
                    Assert.Equal(secondRetention.Value, target.Retention);
                    break;
                case SceneRetentionDefect.ForeignDrivenPrismatic:
                    var rel = CompiledSceneBoundaryTests.Rows(scene, WireSchemaLayout.VtableOffset<Wire.CompiledSceneRecord>("transmissions"))
                        .First(r => buffer.GetUlong(CompiledSceneBoundaryTests.Slot(r, WireSchemaLayout.VtableOffset<Wire.MechanicalTransmissionRecord>("owner"))) == first.Id.Value);
                    BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(CompiledSceneBoundaryTests.Slot(rel, WireSchemaLayout.VtableOffset<Wire.MechanicalTransmissionRecord>("driven_coordinate"))), secondPrismatic.Prismatic.Value);
                    break;
                case SceneRetentionDefect.MissingTrackRetention:
                case SceneRetentionDefect.ForeignTrackRetention:
                    var tr = CompiledSceneBoundaryTests.Rows(scene, WireSchemaLayout.VtableOffset<Wire.CompiledSceneRecord>("presentation"))
                        .First(r => buffer.GetUlong(CompiledSceneBoundaryTests.Slot(r, WireSchemaLayout.VtableOffset<Wire.CompiledPresentationRecord>("owner"))) == first.Id.Value);
                    BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(CompiledSceneBoundaryTests.Slot(tr, WireSchemaLayout.VtableOffset<Wire.CompiledPresentationRecord>("retention"))),
                        defect == SceneRetentionDefect.MissingTrackRetention ? 0ul : secondRetention.Value);
                    break;
            }
        }
    }

    public enum CandidateDefect { BilateralToUnilateral, RetentionId, InitialCoordinate }

    // Each change is a canonical single-row value, so only the candidate's declaration comparison can catch it.
    [Theory]
    [InlineData(CandidateDefect.BilateralToUnilateral)]
    [InlineData(CandidateDefect.RetentionId)]
    [InlineData(CandidateDefect.InitialCoordinate)]
    public async Task CanonicalButChangedTransmissionDeclarationsAreRefusedByCandidateComparison(CandidateDefect defect)
    {
        var bilateral = defect == CandidateDefect.BilateralToUnilateral;
        await using var harness = await WorkshopIntegrationHarness.Running(new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(WorkshopInput.Generic(RuntimeTestServices.Bundle, new(bilateral ? "reverse_transmission" : "wound_spring"), new(1), 0, 4, 0, 0, 0, 0, 1))));
        var source = harness.Device.CommittedWorld.ToArray();
        await harness.AdvanceOne();
        var candidate = harness.Device.CommittedWorld.ToArray();
        var original = PhysicsGpuAbi.ReadMechanicalTransmission(candidate, 0);
        var offset = harness.Simulation.Scene.Physics.Layout.GetMechanicalTransmissionOffset(original.Id.Value);
        switch (defect)
        {
            case CandidateDefect.BilateralToUnilateral:
                Assert.Equal(TransmissionConstraintKind.Bilateral, original.ConstraintKind);
                // A non-positive reaction keeps the changed row inside the unilateral impulse domain.
                BinaryPrimitives.WriteUInt32LittleEndian(candidate.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionKindOffset),
                    (uint)TransmissionConstraintKind.NonPositiveVelocity);
                BinaryPrimitives.WriteSingleLittleEndian(candidate.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionImpulseOffset), 0f);
                break;
            case CandidateDefect.RetentionId:
                BinaryPrimitives.WriteUInt64LittleEndian(candidate.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionRetentionOffset),
                    original.Retention!.Value.Value + 1);
                break;
            case CandidateDefect.InitialCoordinate:
                BinaryPrimitives.WriteSingleLittleEndian(candidate.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionRetentionInitialOffset), -.1f);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }
        _ = PhysicsGpuAbi.ReadMechanicalTransmission(candidate, 0);
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate, source,
            harness.Committed.Tick, PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
    }

    public enum StateDefect { BoundBelowLower, BoundAboveRest, ModeOnUnretainedRow }

    [Theory]
    [InlineData(StateDefect.BoundBelowLower)]
    [InlineData(StateDefect.BoundAboveRest)]
    [InlineData(StateDefect.ModeOnUnretainedRow)]
    public async Task RetentionStateOutsideItsOwnedDomainIsRefusedAtRead(StateDefect defect)
    {
        var unretained = defect == StateDefect.ModeOnUnretainedRow;
        await using var harness = await WorkshopIntegrationHarness.Running(new WorkshopConstruction(new(2), WorkshopCadenceSettings.Default(),
            new(WorkshopInput.Generic(RuntimeTestServices.Bundle, new(unretained ? "reverse_transmission" : "wound_spring"), new(1), 0, 4, 0, 0, 0, 0, 1))));
        var source = harness.Device.CommittedWorld.ToArray();
        await harness.AdvanceOne();
        var candidate = harness.Device.CommittedWorld.ToArray();
        var original = PhysicsGpuAbi.ReadMechanicalTransmission(candidate, 0);
        var offset = harness.Simulation.Scene.Physics.Layout.GetMechanicalTransmissionOffset(original.Id.Value);
        switch (defect)
        {
            case StateDefect.BoundBelowLower:
                BinaryPrimitives.WriteSingleLittleEndian(candidate.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionRetentionBoundOffset),
                    PhysicsGpuAbi.ReadPrismatic(candidate, 0).Lower - .01f);
                break;
            case StateDefect.BoundAboveRest:
                BinaryPrimitives.WriteSingleLittleEndian(candidate.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionRetentionBoundOffset), .01f);
                break;
            case StateDefect.ModeOnUnretainedRow:
                Assert.Null(original.Retention);
                BinaryPrimitives.WriteUInt32LittleEndian(candidate.AsSpan(offset + PhysicsGpuAbi.MechanicalTransmissionRetentionModeOffset),
                    (uint)RetentionMode.Releasing);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(defect));
        }
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadMechanicalTransmission(candidate, 0));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate, source,
            harness.Committed.Tick, PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
    }

    [Theory]
    [InlineData(PhysicsGpuAbi.MechanicalTransmissionDrivingOffset, uint.MaxValue)]
    [InlineData(PhysicsGpuAbi.MechanicalTransmissionDrivenOffset, uint.MaxValue)]
    [InlineData(PhysicsGpuAbi.MechanicalTransmissionDrivenOffset, 2147483648u)]
    public async Task FullWidthCoordinateSlotsAreTypedRefusalsBeforeNarrowing(int field, uint raw)
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Construction());
        var source = harness.Device.CommittedWorld.ToArray();
        await harness.AdvanceOne();
        var candidate = harness.Device.CommittedWorld.ToArray();
        var relation = Assert.Single(harness.Simulation.Scene.Physics.Transmissions.ToArray());
        var offset = harness.Simulation.Scene.Physics.Layout.GetMechanicalTransmissionOffset(relation.Id.Value);
        BinaryPrimitives.WriteUInt32LittleEndian(candidate.AsSpan(offset + field), raw);
        // Exact ArgumentException: the browser client classifies it, whereas an OverflowException would escape classification.
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadMechanicalTransmission(candidate, 0));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ReadRetentions(candidate));
        Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate, source,
            harness.Committed.Tick, PhysicsInteractionSnapshot.Empty, PhysicsInteractionSnapshot.Empty));
    }

    [Fact]
    public async Task UncompressedRetentionCompilesPositiveZeroAndSurvivesTheWire()
    {
        await using var harness = await WorkshopIntegrationHarness.Running(Construction());
        var local = Assert.Single(harness.Simulation.Scene.Physics.Transmissions.ToArray());
        Assert.NotNull(local.Retention);
        Assert.Equal(0u, BitConverter.SingleToUInt32Bits(local.InitialRetentionCoordinate));
        var decoded = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(
            harness.Latest with { Scene = harness.Simulation.Scene })).Scene!;
        var wire = Assert.Single(decoded.Physics.Transmissions.ToArray());
        Assert.Equal(local, wire);
        Assert.Equal(0u, BitConverter.SingleToUInt32Bits(wire.InitialRetentionCoordinate));
        // The admitted world's compression is the same canonical +0, not a negated -0.
        Assert.Equal(0UL, harness.Committed.Tick.Value);
        var admitted = Assert.Single(PhysicsGpuAbi.ReadRetentions(harness.Device.CommittedWorld.Span));
        Assert.Equal(0u, BitConverter.SingleToUInt32Bits(admitted.Compression));
    }

    private static WorkshopRead WithRetention(WorkshopRead read, PrismaticRetentionRead[] retentions)
    {
        var p = read.Presentation;
        return read with { Presentation = new(p.Actuators, p.Loads, p.Logic, p.Ropes, p.Guides, p.Surfaces, p.Transmissions, retentions) };
    }
}
