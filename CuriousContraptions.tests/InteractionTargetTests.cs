using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;
using Xunit;

namespace CuriousContraptions.Tests;

public sealed class InteractionTargetTests
{
    private const int TicksPerSecond = 120;

    private static WorkshopConstruction Free(params IWorkshopInstance[] instances) =>
        new(new(2), WorkshopCadenceSettings.Default(), new(instances));

    private static byte[] AdmitScene(PhysicsSceneDeclaration scene) =>
        PhysicsGpuAbi.Admission(scene, new(1), new(SimulationCadence.Hz120, PhysicalStepProfile.Canonical480Hz, new(1)));

    [Fact]
    public void MultipleEligibleBodies_PreservesExplicitTargetAcrossInstanceOrders_Workshop()
    {
        var ballA = WorkshopInput.Generic(new PartId("ball"), new GpuBodyId(10), -2, 4, 0, 0, 0, 0, 1);
        var ballB = WorkshopInput.Generic(new PartId("ball"), new GpuBodyId(20), 2, 4, 0, 0, 0, 0, 1);
        var selector = TargetSelector.Named("20");

        var order1 = new WorkshopInstances(ballA, ballB);
        var order2 = new WorkshopInstances(ballB, ballA);

        var resolved1 = WorkshopPhysicsCompiler.ResolveTarget(selector, default, order1);
        var resolved2 = WorkshopPhysicsCompiler.ResolveTarget(selector, default, order2);

        Assert.Equal(new BodyTargetSet(BodyTargetKind.NamedBody, new GpuBodyId(20)), resolved1);
        Assert.Equal(new BodyTargetSet(BodyTargetKind.NamedBody, new GpuBodyId(20)), resolved2);
    }

    [Theory]
    [InlineData("bumper_sidekick")]
    [InlineData("wall_and_bumper")]
    [InlineData("delayed_signal")]
    public void MultipleEligibleBodies_PreservesExplicitTargetAcrossInstanceOrders_AuthoredPuzzle(string lessonId)
    {
        var level = PuzzleCatalogue.Create(new(lessonId), new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var targetBody = level.Puzzle.InteractionTarget;
        Assert.NotEqual(default, targetBody);

        var ballA = WorkshopInput.Generic(new PartId("ball"), new GpuBodyId(100), -2, 4, 0, 0, 0, 0, 1);
        var ballB = WorkshopInput.Generic(new PartId("ball"), new GpuBodyId(200), 2, 4, 0, 0, 0, 0, 1);

        var order1 = new WorkshopInstances(level.Instances.Concat([ballA, ballB]).ToArray());
        var order2 = new WorkshopInstances(level.Instances.Concat([ballB, ballA]).ToArray());

        // Explicit target by ID follows the declaration regardless of instance order
        var explicitSelector = TargetSelector.Named("200");
        Assert.Equal(new BodyTargetSet(BodyTargetKind.NamedBody, new GpuBodyId(200)),
            WorkshopPhysicsCompiler.ResolveTarget(explicitSelector, level.Puzzle, order1));
        Assert.Equal(new BodyTargetSet(BodyTargetKind.NamedBody, new GpuBodyId(200)),
            WorkshopPhysicsCompiler.ResolveTarget(explicitSelector, level.Puzzle, order2));

        // Explicit target by interaction_target alias follows puzzle interaction target regardless of instance order
        var puzzleTargetSelector = TargetSelector.Named("interaction_target");
        Assert.Equal(new BodyTargetSet(BodyTargetKind.NamedBody, targetBody),
            WorkshopPhysicsCompiler.ResolveTarget(puzzleTargetSelector, level.Puzzle, order1));
        Assert.Equal(new BodyTargetSet(BodyTargetKind.NamedBody, targetBody),
            WorkshopPhysicsCompiler.ResolveTarget(puzzleTargetSelector, level.Puzzle, order2));

        // Default AllDynamic compiles uniformly to AllDynamic in both Free Workshop and authored puzzles (Requirement D4)
        Assert.Equal(new BodyTargetSet(BodyTargetKind.AllDynamic, default),
            WorkshopPhysicsCompiler.ResolveTarget(TargetSelector.AllDynamic, default, order1));
        Assert.Equal(new BodyTargetSet(BodyTargetKind.AllDynamic, default),
            WorkshopPhysicsCompiler.ResolveTarget(TargetSelector.AllDynamic, level.Puzzle, order1));
        Assert.Equal(new BodyTargetSet(BodyTargetKind.AllDynamic, default),
            WorkshopPhysicsCompiler.ResolveTarget(TargetSelector.AllDynamic, level.Puzzle, order2));
    }

    [Fact]
    public void AllPayloads_CompilesUniformlyToAllPayloads()
    {
        var ballA = WorkshopInput.Generic(new PartId("ball"), new GpuBodyId(10), -2, 4, 0, 0, 0, 0, 1);
        var instances = new WorkshopInstances(ballA);
        var puzzle = new WorkshopPuzzle(new("test"), WorkshopPlacementMode.Manual, new(1f), default, 0, default, default, default, default, InteractionTarget: new GpuBodyId(10));

        Assert.Equal(new BodyTargetSet(BodyTargetKind.AllPayloads, default),
            WorkshopPhysicsCompiler.ResolveTarget(TargetSelector.AllPayloads, default, instances));
        Assert.Equal(new BodyTargetSet(BodyTargetKind.AllPayloads, default),
            WorkshopPhysicsCompiler.ResolveTarget(TargetSelector.AllPayloads, puzzle, instances));
    }

    [Fact]
    public void ModifiedInteractionTarget_FailsPuzzleValidation()
    {
        var level = PuzzleCatalogue.Create(new("bumper_sidekick"), new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var tampered = level with { Puzzle = level.Puzzle with { InteractionTarget = new GpuBodyId(99999) } };

        var ex = Assert.Throws<WorkshopEditRejectedException>(() => tampered.Validate());
        Assert.Equal(WorkshopEditRejection.PuzzleSettings, ex.Reason);
        Assert.Contains("Authored interaction target changed", ex.Message);
    }

    [Fact]
    public void SaveLoadRoundtrip_PreservesInteractionTargetBitExact()
    {
        var level = PuzzleCatalogue.Create(new("bumper_sidekick"), new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        Assert.NotEqual(default, level.Puzzle.InteractionTarget);

        var builder = new Google.FlatBuffers.FlatBufferBuilder(1024);
        builder.Finish(WorkshopAuthoringFlatBuffers.EncodeConstructionRecord(builder, level).Value);
        var bytes = builder.SizedByteArray();

        var decoded = WorkshopAuthoringFlatBuffers.DecodeConstructionRecord(
            CuriousContraptions.Wire.ConstructionRecord.GetRootAsConstructionRecord(new Google.FlatBuffers.ByteBuffer(bytes)));

        Assert.Equal(level.Puzzle.InteractionTarget, decoded.Puzzle.InteractionTarget);
    }

    [Fact]
    public void NoPayloadMechanism_CompilesRowsInWorkshopAndAuthoredPuzzle()
    {
        var bumper = PlacedParts.Bumper(new(1), 0, 1, 0, 8f);
        var switchPart = WorkshopInput.Generic(new PartId("switch"), new(2), 2, 1, 0, 0, 0, 0, 1);
        var wall = WorkshopInput.Generic(new PartId("wall"), new(3), -2, 1, 0, 0, 0, 0, 1, WallFixture.Default);

        var workshopConstruction = Free(bumper, switchPart, wall);
        Assert.Null(Payloads.Find(workshopConstruction));
        var workshopScene = WorkshopPhysicsCompiler.Compile(workshopConstruction, new(1, 2));

        Assert.True(workshopScene.ContactWorks.Length > 0);
        Assert.True(workshopScene.Triggers.Length > 0);
        Assert.Equal(BodyTargetKind.AllDynamic, workshopScene.ContactWorks[0].Targets.Kind);
        Assert.Equal(BodyTargetKind.AllDynamic, workshopScene.Triggers[0].Targets.Kind);

        var dominoLevel = PuzzleCatalogue.Create(new("domino_effect"), new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var instancesWithoutPayload = new WorkshopInstances(bumper, switchPart, wall);
        Assert.Null(Payloads.Find(new WorkshopConstruction(new(1), WorkshopCadenceSettings.Default(), instancesWithoutPayload)));

        var resolvedWork = WorkshopPhysicsCompiler.ResolveTarget(TargetSelector.AllDynamic, dominoLevel.Puzzle, instancesWithoutPayload);
        var resolvedTrigger = WorkshopPhysicsCompiler.ResolveTarget(TargetSelector.AllDynamic, dominoLevel.Puzzle, instancesWithoutPayload);

        Assert.Equal(BodyTargetKind.AllDynamic, resolvedWork.Kind);
        Assert.Equal(BodyTargetKind.AllDynamic, resolvedTrigger.Kind);
    }

    [Fact]
    public void NonselectedBodyControl_OnlyTargetBodyTriggersWork()
    {
        var ball1 = WorkshopInput.Generic(new PartId("ball"), new(1), 0, 4.5f, 0, 0, 0, 0, 1);
        var ball2 = WorkshopInput.Generic(new PartId("ball"), new(2), 10, 10, 0, 0, 0, 0, 1);
        var bumper = PlacedParts.Bumper(new(3), 0, 3, 0, 8f);
        var construction = Free(ball1, ball2, bumper);
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        var workOffset = scene.Layout.GetContactWorkOffset(scene.ContactWorks[0].Id);

        // Case A (Non-selected target): Bumper targets Ball 2; Ball 1 hits Bumper -> no work
        var nonTargetWorld = AdmitScene(scene);
        BinaryPrimitives.WriteUInt32LittleEndian(nonTargetWorld.AsSpan(workOffset + PhysicsGpuAbi.ContactWorkTargetKindOffset), (uint)BodyTargetKind.NamedBody);
        BinaryPrimitives.WriteUInt32LittleEndian(nonTargetWorld.AsSpan(workOffset + PhysicsGpuAbi.ContactWorkNamedTargetOffset), (uint)scene.Layout.GetBodySlot(new(2)));

        var nonTargetLayout = PhysicsGpuAbi.Layout(nonTargetWorld);
        var nonTargetStore = nonTargetLayout.ContactWorkOffset(0);
        WorkshopPhysicsEngineTests.StepWorld(nonTargetWorld, 2 * TicksPerSecond, (tick, world, interactions) =>
        {
            var available = BinaryPrimitives.ReadSingleLittleEndian(world.AsSpan(nonTargetStore + PhysicsGpuAbi.ContactWorkAvailableOffset));
            Assert.Equal(32f, available);
            var count = BinaryPrimitives.ReadUInt32LittleEndian(world.AsSpan(nonTargetStore + PhysicsGpuAbi.ContactWorkOccurrenceCountOffset));
            Assert.Equal(0u, count);
        });

        // Case B (Selected target): Bumper targets Ball 1; Ball 1 hits Bumper -> paid work debited
        var targetWorld = AdmitScene(scene);
        BinaryPrimitives.WriteUInt32LittleEndian(targetWorld.AsSpan(workOffset + PhysicsGpuAbi.ContactWorkTargetKindOffset), (uint)BodyTargetKind.NamedBody);
        BinaryPrimitives.WriteUInt32LittleEndian(targetWorld.AsSpan(workOffset + PhysicsGpuAbi.ContactWorkNamedTargetOffset), (uint)scene.Layout.GetBodySlot(new(1)));

        var targetLayout = PhysicsGpuAbi.Layout(targetWorld);
        var targetStore = targetLayout.ContactWorkOffset(0);
        var targetPaid = false;
        WorkshopPhysicsEngineTests.StepWorld(targetWorld, 2 * TicksPerSecond, (tick, world, interactions) =>
        {
            var count = BinaryPrimitives.ReadUInt32LittleEndian(world.AsSpan(targetStore + PhysicsGpuAbi.ContactWorkOccurrenceCountOffset));
            if (count > 0) targetPaid = true;
        });
        Assert.True(targetPaid, "the target ball reaches the Bumper and triggers paid work");
        var finalAvailable = BinaryPrimitives.ReadSingleLittleEndian(targetWorld.AsSpan(targetStore + PhysicsGpuAbi.ContactWorkAvailableOffset));
        Assert.True(finalAvailable < 32f);
    }

    [Fact]
    public void NonselectedBodyControl_OnlyTargetBodyActivatesSwitch()
    {
        var ball1 = WorkshopInput.Generic(new PartId("ball"), new(1), 0, 3f, 0, 0, 0, 0, 1);
        var ball2 = WorkshopInput.Generic(new PartId("ball"), new(2), 10, 10, 0, 0, 0, 0, 1);
        var switchPart = WorkshopInput.Generic(new PartId("switch"), new(3), 0, 1, 0, 0, 0, 0, 1);
        var construction = Free(ball1, ball2, switchPart);
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        var triggerOffset = scene.Layout.GetTriggerOffset(scene.Triggers[0].Id);

        // Case A (Non-selected target): Switch targets Ball 2; Ball 1 hits Switch -> no activation
        var nonTargetWorld = AdmitScene(scene);
        BinaryPrimitives.WriteUInt32LittleEndian(nonTargetWorld.AsSpan(triggerOffset + PhysicsGpuAbi.TriggerTargetKindOffset), (uint)BodyTargetKind.NamedBody);
        BinaryPrimitives.WriteUInt32LittleEndian(nonTargetWorld.AsSpan(triggerOffset + PhysicsGpuAbi.TriggerNamedTargetOffset), (uint)scene.Layout.GetBodySlot(new(2)));

        var nonTargetLayout = PhysicsGpuAbi.Layout(nonTargetWorld);
        var nonTargetTriggerOffset = nonTargetLayout.TriggerOffset(0);
        WorkshopPhysicsEngineTests.StepWorld(nonTargetWorld, 2 * TicksPerSecond, (tick, world, interactions) =>
        {
            var count = BinaryPrimitives.ReadUInt32LittleEndian(world.AsSpan(nonTargetTriggerOffset + PhysicsGpuAbi.TriggerCountOffset));
            Assert.Equal(0u, count);
        });

        // Case B (Selected target): Switch targets Ball 1; Ball 1 hits Switch -> activation fires
        var targetWorld = AdmitScene(scene);
        BinaryPrimitives.WriteUInt32LittleEndian(targetWorld.AsSpan(triggerOffset + PhysicsGpuAbi.TriggerTargetKindOffset), (uint)BodyTargetKind.NamedBody);
        BinaryPrimitives.WriteUInt32LittleEndian(targetWorld.AsSpan(triggerOffset + PhysicsGpuAbi.TriggerNamedTargetOffset), (uint)scene.Layout.GetBodySlot(new(1)));

        var targetLayout = PhysicsGpuAbi.Layout(targetWorld);
        var targetTriggerOffset = targetLayout.TriggerOffset(0);
        var targetFired = false;
        WorkshopPhysicsEngineTests.StepWorld(targetWorld, 2 * TicksPerSecond, (tick, world, interactions) =>
        {
            var count = BinaryPrimitives.ReadUInt32LittleEndian(world.AsSpan(targetTriggerOffset + PhysicsGpuAbi.TriggerCountOffset));
            if (count > 0) targetFired = true;
        });
        Assert.True(targetFired, "the target ball reaches the Switch and triggers activation");
    }

    [Fact]
    public async Task ZeroMatches_DynamicTargetWithZeroDynamicBodiesCompilesAndRuns()
    {
        var bumper = PlacedParts.Bumper(new(1), 0, 3, 0, 8f);
        var switchPart = WorkshopInput.Generic(new PartId("switch"), new(2), 2, 1, 0, 0, 0, 0, 1);
        var wall = WorkshopInput.Generic(new PartId("wall"), new(3), 0, 1, 0, 0, 0, 0, 1, WallFixture.Default);

        var construction = Free(bumper, switchPart, wall);
        var scene = WorkshopPhysicsCompiler.Compile(construction, new(1, 2));

        Assert.Single(scene.ContactWorks.ToArray());
        Assert.Single(scene.Triggers.ToArray());
        Assert.Equal(BodyTargetKind.AllDynamic, scene.ContactWorks[0].Targets.Kind);
        Assert.Equal(BodyTargetKind.AllDynamic, scene.Triggers[0].Targets.Kind);

        var (reads, ledgers) = await WorkshopIntegrationHarness.ObserveAccounting(construction, 60);
        Assert.All(reads, read =>
        {
            Assert.Equal(32f, read.ContactWorks[0].RemainingEnergy.Value);
            for (var i = 0; i < read.Activations.Count; i++)
                Assert.Equal(ActivationPhase.Clear, read.Activations[i].Phase);
        });
        Assert.All(ledgers, ledger => Assert.Equal(0, ledger.OccurrenceCount));
    }

    [Fact]
    public void UnknownNamedTarget_Refusals()
    {
        var ball = WorkshopInput.Generic(new PartId("ball"), new(1), 0, 2, 0, 0, 0, 0, 1);
        var instances = new WorkshopInstances(ball);

        Assert.Throws<ArgumentException>(() => TargetSelector.Named(""));
        Assert.Throws<ArgumentException>(() => TargetSelector.Named("   "));

        var exWorkshop = Assert.Throws<WorkshopEditRejectedException>(() =>
            WorkshopPhysicsCompiler.ResolveNamedTarget("99999", default, instances));
        Assert.Equal(WorkshopEditRejection.PuzzleSettings, exWorkshop.Reason);

        var level = PuzzleCatalogue.Create(new("bumper_sidekick"), new(1), WorkshopCadenceSettings.Default(), new(1), new(.45f));
        var exAuthored = Assert.Throws<WorkshopEditRejectedException>(() =>
            WorkshopPhysicsCompiler.ResolveNamedTarget("nonexistent_slot", level.Puzzle, level.Instances));
        Assert.Equal(WorkshopEditRejection.PuzzleSettings, exAuthored.Reason);

        var invalidJson = """
        [
          {
            "id": "bad_target_puzzle",
            "title": "Bad Target",
            "order": 1,
            "goal": { "kind": "captured", "source": "fixture_1", "target": "fixture_2" },
            "interaction_target": "nonexistent_fixture",
            "inventory": [{ "part": "ball", "count": 1 }],
            "placements": [
              { "slot": "fixture_1", "instance": { "part": "ball", "cell": [0, 0, 0], "rotation": [0, 0, 0, 1] } },
              { "slot": "fixture_2", "instance": { "part": "basket", "cell": [0, 0, 0], "rotation": [0, 0, 0, 1] } }
            ],
            "solution": []
          }
        ]
        """;
        Assert.Throws<InvalidDataException>(() => PuzzleContentReader.ReadIndex(invalidJson));
    }
}
