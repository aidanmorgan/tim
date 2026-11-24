using CuriousContraptions.Abstractions;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed class BalloonTests
{
    internal static WorkshopGenericInstance Balloon(ulong id = 1, float x = 0, float y = 3, float mass = .5f,
        float radius = .36f, float bounce = .2f, float buoyancy = 11.5f, float drag = .4f) =>
        WorkshopInput.Generic(RuntimeTestServices.Bundle, new("balloon"), new(id), x, y, 0, 0, 0, 0, 1,
            new([new(new("mass"), mass), new(new("radius"), radius), new(new("bounce"), bounce),
                new(new("buoyancy"), buoyancy), new(new("drag"), drag)]));

    private static CanonicalBody Body(WorkshopIntegrationHarness h, ulong id = 1)
    {
        Assert.True(h.Committed.Bodies.TryGet(new(id), out var body));
        return body.Body;
    }
    private static float Y(CanonicalBody body) => (body.Cell.Y + body.Local.Y) / 16f;

    [Fact]
    public async Task StandardRisesAgainstHeavierControlAndVacuumFallsWithTheSameSealedInventory()
    {
        var authored = TennisBallTests.Scene(Balloon(y: 6),
            WorkshopInput.Generic(RuntimeTestServices.Bundle, new("bowling"), new(2), 3, 6, 0, 0, 0, 0, 1));
        await using var standard = await WorkshopIntegrationHarness.Running(authored);
        await using var vacuum = await WorkshopIntegrationHarness.Running(authored with { Atmosphere = AtmospherePreset.Vacuum });
        Assert.Equal(standard.Committed.GasStores[0].State.Mass, vacuum.Committed.GasStores[0].State.Mass);
        Assert.True(vacuum.Committed.GasStores[0].State.Volume > standard.Committed.GasStores[0].State.Volume);
        await standard.Advance(60); await vacuum.Advance(60);
        Assert.True(Y(Body(standard)) > 6.1f);
        Assert.True(Y(Body(standard, 2)) < 5.1f);
        Assert.True(Y(Body(vacuum)) < 5.1f);
        await standard.Advance(1380);
        // After twelve seconds, 0.15 m/s distinguishes the declared terminal-speed law without fixing incidental ticks.
        Assert.InRange(Body(standard).Velocity.Y, 4.075f, 4.375f);
        Assert.Equal(IntegrityPhase.Intact, standard.Committed.IntegrityObservers[0].Phase);
    }

    [Fact]
    public async Task OrdinaryCeilingContactSupportsTheDefaultEnvelopeWithoutRupture()
    {
        var ceiling = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wall"), new(2), 0, 5, 0, 0, 0, 0, 1,
            new([new(new("width"), 3), new(new("height"), .4f), new(new("thickness"), 2)]));
        await using var h = await WorkshopIntegrationHarness.Running(TennisBallTests.Scene(Balloon(), ceiling));
        await h.Advance(960);
        // A two-centimetre resting envelope keeps the sphere visibly seated against the underside at y=4.8.
        Assert.InRange(Y(Body(h)), 4.42f, 4.46f);
        Assert.InRange(MathF.Abs(Body(h).Velocity.Y), 0, .05f);
        Assert.Equal(IntegrityPhase.Intact, h.Committed.IntegrityObservers[0].Phase);
        Assert.Empty(h.Of(SimulationEventKind.IntegrityBroken));
    }

    [Fact]
    public async Task VacuumExpansionRefusesObstructionWithoutChangingInstalledConstruction()
    {
        var wall = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wall"), new(2), 1.1f, 3, 0, 0, 0, 0, 1,
            new([new(new("width"), .4f), new(new("height"), 2), new(new("thickness"), 1)]));
        var authored = TennisBallTests.Scene(Balloon(), wall);
        await using var h = await WorkshopIntegrationHarness.Building(authored);
        var installed = h.Committed;
        var error = await Assert.ThrowsAsync<WorkshopEditRejectedException>(() =>
            h.Execute(WorkshopCommandKind.Construct, authored with { Atmosphere = AtmospherePreset.Vacuum }));
        Assert.Equal(WorkshopEditRejection.InitialOverlap, error.Reason);
        Assert.Equal(authored, h.Construction);
        Assert.Equal(installed.Epoch, h.Committed.Epoch);
        Assert.Equal(installed.GasStores[0], h.Committed.GasStores[0]);
    }

    [Fact]
    public async Task AllControlsAndExplicitAtmosphereSurviveSaveAndReset()
    {
        var authored = TennisBallTests.Scene(Balloon(y: 6, mass: 2, radius: .4f, bounce: .6f, buoyancy: 14, drag: 4)) with
            { Atmosphere = AtmospherePreset.Vacuum };
        var bytes = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(authored, new(2)));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements, bytes).Construction;
        Assert.Equal(AtmospherePreset.Vacuum, loaded.Atmosphere);
        Assert.Equal(((WorkshopGenericInstance)authored.Instances.Single()).Parameters,
            ((WorkshopGenericInstance)loaded.Instances.Single()).Parameters);
        // The Balloon is declared a capture-target payload.
        Assert.True(DeclaredPhysics.IsPayload(new PartId("balloon")));
        await using var h = await WorkshopIntegrationHarness.Building(loaded);
        var initial = h.Committed.GasStores[0];
        await h.Run(); await h.Advance(30);
        await h.Require(WorkshopCommandKind.Pause);
        var tick = h.Committed.Tick;
        await h.Require(WorkshopCommandKind.Step);
        Assert.True(h.Committed.Tick.Value > tick.Value);
        await h.Reset();
        Assert.Equal(initial, h.Committed.GasStores[0]);
        Assert.Equal(6f, Y(Body(h)));
        Assert.Equal(loaded, h.Construction);
    }

    [Fact]
    public async Task TautTieRestrainsLiftWhileSlackAndDisconnectedControlsRise()
    {
        var anchor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("rope_anchor"), new(2),
            0, 1, -.18f, 0, 0, 0, 1);
        var scene = TennisBallTests.Scene(Balloon(), anchor);
        WorkshopConstruction Tether(float length) => scene with { Connections = new(new WorkshopConnection(
            new(2), PortId.Tie, new(1), PortId.Tie, WorkshopConnectionDomain.Rope, length)) };
        await using var taut = await WorkshopIntegrationHarness.Running(Tether(2));
        await using var slack = await WorkshopIntegrationHarness.Running(Tether(8));
        await using var free = await WorkshopIntegrationHarness.Running(scene);
        await taut.Advance(360); await slack.Advance(120); await free.Advance(120);
        // A 2 cm allowance keeps the centre tie visibly at its 2 m reach; free rise exceeds half a metre.
        Assert.InRange(Y(Body(taut)), 2.98f, 3.02f);
        Assert.InRange(MathF.Abs(Body(taut).Velocity.Y), 0, .05f);
        Assert.True(Y(Body(free)) > 3.5f);
        Assert.InRange(MathF.Abs(Y(Body(free)) - Y(Body(slack))), 0, .02f);
        Assert.Equal(IntegrityPhase.Intact, taut.Committed.IntegrityObservers[0].Phase);
        await taut.Reset();
        Assert.Equal(Tether(2), taut.Construction);
        Assert.Equal(3f, Y(Body(taut)));
    }

    [Fact]
    public async Task AConnectedEnvelopeRupturesAndEmptiesWithoutMovingItsDeclaredTie()
    {
        var anchor = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("rope_anchor"), new(2),
            0, 1, -.18f, 0, 0, 0, 1);
        var ceiling = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wall"), new(3), 0, 5, 0, 0, 0, 0, 1,
            new([new(new("width"), 3), new(new("height"), .4f), new(new("thickness"), 2)]));
        var scene = TennisBallTests.Scene(Balloon(mass: 2, buoyancy: 16), anchor, ceiling) with
        {
            Connections = new(new WorkshopConnection(new(2), PortId.Tie, new(1), PortId.Tie,
                WorkshopConnectionDomain.Rope, 5))
        };
        await using var h = await WorkshopIntegrationHarness.Running(scene);
        await h.Advance(960);
        Assert.Equal(IntegrityPhase.Broken, h.Committed.IntegrityObservers[0].Phase);
        Assert.Equal(0, h.Committed.GasStores[0].State.Mass);
        Assert.Single(h.Of(SimulationEventKind.IntegrityBroken));
        Assert.Equal(System.Numerics.Vector3.Zero, DeclaredPorts.Current.LocalPosition(Balloon(), PortId.Tie));
        Assert.Equal(scene.Connections, h.Construction.Connections);
        Assert.True(Y(Body(h)) < 1, "After losing lift, the empty carrier must fall below the ceiling.");
    }

    [Fact]
    public async Task NondefaultLiftAndStrongDragChangeStandardMotion()
    {
        await using var h = await WorkshopIntegrationHarness.Running(TennisBallTests.Scene(
            Balloon(1, -3, 10, mass: 2, radius: .4f, bounce: .6f, buoyancy: 14, drag: 0),
            Balloon(2, 0, 10, mass: 2, radius: .4f, bounce: .6f, buoyancy: 14, drag: 4),
            Balloon(3, 3, 10, mass: 2, radius: .4f, bounce: .6f, buoyancy: 0, drag: 0)));
        var scene = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, h.Construction, new(1, 2));
        foreach (var id in new ulong[] { 1, 2, 3 })
        {
            var body = Assert.Single(scene.Bodies.ToArray(), b => b.Id == new GpuBodyId(id));
            Assert.Equal(2, body.Mass.Value);
            var collider = Assert.Single(scene.Colliders.ToArray(), c => c.Body == body.Id);
            // The authored Standard radius is preserved by physical preparation within 0.1 mm.
            Assert.InRange(collider.Radius.Value, .3999f, .4001f);
            Assert.Equal(.6f, Assert.Single(scene.Materials.ToArray(), m => m.Id == collider.Material).Restitution.Value);
        }
        await h.Advance(120);
        // Net acceleration 14-9.81 predicts 4.19 m/s undamped and about1.03 m/s at drag4 after1s.
        // These broad gameplay speeds catch either a dead binding or the retired .125 engine ceiling.
        Assert.True(Body(h, 1).Velocity.Y > 3.5f);
        Assert.InRange(Body(h, 2).Velocity.Y, .8f, 1.2f);
        Assert.True(Body(h, 3).Velocity.Y < -9);
    }

    [Theory]
    [InlineData("ball")]
    [InlineData("bowling")]
    public async Task ExistingBallsHaveAmbientDragOnlyInStandardAndRestoreTheirEnvironment(string part)
    {
        var authored = TennisBallTests.Scene(WorkshopInput.Generic(RuntimeTestServices.Bundle, new(part), new(1),
            0, 20, 0, 0, 0, 0, 1));
        await using var standard = await WorkshopIntegrationHarness.Running(authored);
        var vacuumScene = authored with { Atmosphere = AtmospherePreset.Vacuum };
        var saved = WorkshopSaveCodec.Encode(RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle, new(vacuumScene, new(2)));
        var loaded = WorkshopSaveCodec.Decode(RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply,
            RuntimeTestServices.Bundle, SaveFixture.Elements, saved).Construction;
        await using var vacuum = await WorkshopIntegrationHarness.Running(loaded);
        foreach (var construction in new[] { authored, loaded })
        {
            var compiled = WorkshopPhysicsCompiler.Compile(RuntimeTestServices.Core, RuntimeTestServices.Bundle, construction, new(1, 2));
            var body = Assert.Single(compiled.Bodies.ToArray(), b => b.Id == new GpuBodyId(1));
            Assert.Equal(0, body.LinearDrag.Value);
            Assert.Equal(-9.81f, body.Gravity.Y);
            Assert.Equal(construction.Atmosphere == AtmospherePreset.Standard ? .04f : 0,
                Assert.Single(compiled.AmbientLoads.ToArray()).LinearDrag);
        }
        await standard.Advance(120); await vacuum.Advance(120);
        // .05m/s bounds at most5cm route error over1s; .04 damping predicts a .19m/s difference.
        Assert.InRange(Body(vacuum).Velocity.Y, -9.86f, -9.76f);
        Assert.True(Body(standard).Velocity.Y > Body(vacuum).Velocity.Y + .1f);
        await standard.Reset(); await vacuum.Reset();
        Assert.Equal(authored, standard.Construction);
        Assert.Equal(vacuumScene, vacuum.Construction);
        Assert.Equal(20, Y(Body(vacuum)));
        Assert.Equal(0, Body(vacuum).Velocity.Y);
    }

    [Fact]
    public async Task RupturePrecedesTimedPartialLossAndPauseStepResumeResetOwnTheOpenStore()
    {
        var ceiling = WorkshopInput.Generic(RuntimeTestServices.Bundle, new("wall"), new(2), 0, 5, 0, 0, 0, 0, 1,
            new([new(new("width"), 3), new(new("height"), .4f), new(new("thickness"), 2)]));
        var authored = TennisBallTests.Scene(Balloon(mass: 2, buoyancy: 16), ceiling);
        await using var h = await WorkshopIntegrationHarness.Running(authored);
        var initial = h.Committed.GasStores[0];
        for (var tick = 0; tick < 600 && h.Committed.IntegrityObservers[0].Phase == IntegrityPhase.Intact; tick++)
            await h.Advance(1);
        var occurrence = h.Committed.IntegrityObservers[0];
        Assert.Equal(IntegrityPhase.Broken, occurrence.Phase);
        var afterOrdinal = h.Committed.Motion!.LastOrdinal - occurrence.Ordinal;
        // Intentional next-substep debit contract. Quarter of one allocation excludes charging the rupture substep.
        var allocation = initial.State.Mass / 480;
        Assert.InRange(h.Committed.GasStores[0].State.Mass,
            initial.State.Mass - allocation * afterOrdinal - allocation / 4,
            initial.State.Mass - allocation * afterOrdinal + allocation / 4);
        await h.Advance(30);
        var partial = h.Committed.GasStores[0];
        Assert.InRange(partial.State.Mass / initial.State.Mass, .70f, .80f);
        Assert.InRange(partial.State.Volume / initial.State.Volume, .70f, .80f);
        Assert.InRange(partial.State.Energy / initial.State.Energy, .70f, .80f);
        Assert.True(partial.EnthalpyOut > 0);
        var visual = Assert.Single(h.Presentation!.Advance(new(h.Committed.Motion!.LastOrdinal)),
            sample => sample.Owner == new GpuBodyId(1) && sample.Binding == 0);
        // A .36m envelope at70–80% gas volume has about.32–.335m radius, visibly distinct from full and empty.
        Assert.InRange(BitConverter.UInt32BitsToSingle(visual.Value.CanonicalBits), .31f, .35f);
        var response = h.Latest with { Scene = h.Simulation.Scene };
        Assert.Equal(partial, WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(response)).Read.GasStores[0]);
        foreach (var invalidState in new[] { partial.State with { Energy = partial.State.Energy * .9f },
            partial.State with { Volume = partial.State.Volume * 1.1f } })
        {
            var invalid = response with { Read = response.Read with
                { GasStores = new([partial with { State = invalidState }]) } };
            Assert.Throws<ArgumentException>(() => WorkshopWireFlatBuffers.EncodeResponse(invalid));
            // A standalone current read gets its owner contract from the installed consumer.
            var received = WorkshopWireFlatBuffers.DecodeResponse(WorkshopWireFlatBuffers.EncodeResponse(
                invalid with { Scene = null, Publication = new(response.Publication.Value + 1) }));
            Assert.Throws<ArgumentException>(() => h.Presentation.Accept(received));
        }
        Assert.Equal(partial, h.Committed.GasStores[0]);
        await h.Require(WorkshopCommandKind.Pause);
        var tickBefore = h.Committed.Tick;
        Assert.Equal(WorkshopRejection.WrongPhase, (await h.AdvanceOne()).Reason);
        Assert.Equal(tickBefore, h.Committed.Tick);
        Assert.Equal(partial, h.Committed.GasStores[0]);
        Assert.Equal(occurrence, h.Committed.IntegrityObservers[0]);
        await h.Require(WorkshopCommandKind.Step);
        var step = h.Committed.GasStores[0];
        Assert.True(h.Committed.Tick.Value > tickBefore.Value);
        Assert.InRange(partial.State.Mass - step.State.Mass, allocation * 3.75f, allocation * 4.25f);
        await h.Require(WorkshopCommandKind.Resume);
        await h.Advance(120);
        Assert.Equal(GasInventoryPhase.Empty, h.Committed.GasStores[0].State.Phase);
        Assert.Equal(occurrence, h.Committed.IntegrityObservers[0]);
        Assert.Single(h.Of(SimulationEventKind.IntegrityBroken));
        var goal = new WorkshopGoal(WorkshopGoalKind.IntegrityBroken, default, default, Integrity: new(new(1), new(1)));
        await h.Reset();
        Assert.Equal(initial, h.Committed.GasStores[0]);
        Assert.Equal(IntegrityPhase.Intact, h.Committed.IntegrityObservers[0].Phase);
        Assert.Equal(authored, h.Construction);
        Assert.Equal(WorkshopGoalPhase.Waiting, WorkshopGoalEvaluator.Evaluate(goal, h.Committed,
            h.Committed.Epoch, h.Construction.Instances.Identities));
    }

    [Fact]
    public void MissingOrUnknownSerializedAtmosphereIsRefusedRatherThanDefaulted()
    {
        var unknown = TennisBallTests.Scene(Balloon()) with { Atmosphere = (AtmospherePreset)99 };
        SaveFixture.Refused(WorkshopSaveRejection.UndefinedEnum, () => WorkshopSaveCodec.Decode(
            RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle,
            SaveFixture.Elements, SaveFixture.Forged(unknown, 2)));
        var builder = new Google.FlatBuffers.FlatBufferBuilder(128);
        var settings = WorkshopCadenceSettings.Default();
        CuriousContraptions.Wire.ConstructionRecord.StartConstructionRecord(builder);
        CuriousContraptions.Wire.ConstructionRecord.AddRevision(builder, 1);
        CuriousContraptions.Wire.ConstructionRecord.AddSettings(builder,
            CuriousContraptions.Wire.CadenceSettingsRecord.CreateCadenceSettingsRecord(builder,
                (uint)settings.Simulation, (uint)settings.Physical, (uint)settings.Animation, (uint)settings.Presentation,
                settings.PresentationRate.Numerator, settings.PresentationRate.Denominator));
        var bytes = SaveFixture.Finish(builder, CuriousContraptions.Wire.ConstructionRecord.EndConstructionRecord(builder), 1);
        SaveFixture.Refused(WorkshopSaveRejection.MissingField, () => WorkshopSaveCodec.Decode(
            RuntimeTestServices.Core.Codecs.Commands, RuntimeTestServices.Core.Supply, RuntimeTestServices.Bundle,
            SaveFixture.Elements, bytes));
    }
}
