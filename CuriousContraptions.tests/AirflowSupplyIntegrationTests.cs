using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;
using CuriousContraptions.Simulation;

namespace CuriousContraptions.Tests;

public sealed partial class AirflowRuntimeTests
{


    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void MixedChargingAndDirectPaymentsValidateTheirConnectedSummaries(bool enabled, bool reverse)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var cargo = scene.Sphere(new(1, 5, 0), .1f, 1000, LawMaterial.Domino, default, default, 0, 0);
        scene.AirflowTarget(cargo);
        var source = scene.LimitedSource(frame, 14400, 12000);
        for (var i = 0; i < 8; i++)
            scene.StoreConsumer(frame, source, scene.EnergyStore(frame, 10, 0, 1.2f));
        for (var i = 0; i < 32; i++) scene.ElectricalAirflow(frame, source, 4, 12);
        if (reverse) scene.ReverseResourceDeclarations();
        var world = scene.Admit();
        var input = new PhysicsTickInputs([enabled ? ElectricalEnable.Enabled : ElectricalEnable.Disabled], [])
        { AirflowEnabled = Enumerable.Repeat(true, 32).ToArray(), StorageRoutes = Enumerable.Repeat(true, 8).ToArray() };
        var candidate = new byte[world.Admitted.Length];
        var interactions = new WorkshopPhysicsEngine().Advance(world.Admitted, candidate, input, PhysicsInteractionSnapshot.Empty);
        PhysicsGpuAbi.ValidateCandidate(candidate, world.Admitted, new(1), interactions, PhysicsInteractionSnapshot.Empty);
        var payment = PhysicsGpuAbi.ReadElectrical(candidate)[0];
        var stores = PhysicsGpuAbi.ReadEnergyStores(candidate);
        var flows = PhysicsGpuAbi.ReadAirflow(candidate);
        Assert.Equal(enabled ? 136u : 0u, payment.Transactions);
        Assert.Equal(enabled, LawState.Read(world.Scene, candidate, cargo).Velocity.X > 0);
        Assert.All(stores, store => Assert.Equal(enabled, store.CommitSupplied.Value > 0));
        Assert.All(flows, flow => Assert.Equal(enabled, flow.CommitWork.Value > 0));
        if (enabled)
        {
            // 32 sources at 48 W for 1/120 s plus eight 0.01 J charges: about 12.88 J.
            Assert.InRange(payment.Debit.Value, 12.7f, 12.9f);
            var delivered = stores.Sum(s => (double)s.CommitSupplied.Value) + flows.Sum(s => (double)s.CommitWork.Value);
            Assert.InRange(payment.Debit.Value - delivered, -.001, .001);
        }
        else Assert.Equal(0, payment.Debit.Value);
    }

    [Fact]
    public void UnderchargedReleaseIsNotRetriedAfterTheSamePhaseCreditsItsStore()
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 3, 0), new(.1f), colliderOffset: new(0, -2, 0));
        var payload = scene.Sphere(new(0, 3, 0), .35f, 1, new(0, 0, 0, 0), default, default, 0, 0);
        var store = scene.EnergyStore(frame, .1f, .08f, 2.4f);
        var release = scene.FiniteWork(frame, payload, .1f, .08f, sharedStore: store, targetSpeed: .2f);
        var provider = scene.StaticBox(new(0, 8, 0), new(.1f));
        var supply = scene.LimitedSource(provider, .02f, 2.4f);
        scene.StoreConsumer(frame, supply, store);
        var nozzle = scene.StaticBox(new(0, 10, 0), new(.1f));
        scene.Airflow(nozzle, new(ContinuousWorkInputKind.Store, store, default, default, 0, true));
        var world = scene.Admit();
        var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Contact, new(1), new(0, 0),
            new(new(1), frame, new(1), new(0, 0), new(0), ActivationCauseKind.Physical));
        var inputs = new PhysicsTickInputs([ElectricalEnable.Enabled], [])
        {
            AirflowEnabled = [true], StorageRoutes = [true],
            PhysicalRequests = [new(PhysicalRequestTarget.ForActuator(release), PhysicalRequestOperation.Release, occurrence)]
        };
        var candidate = new byte[world.Admitted.Length];
        var interactions = new WorkshopPhysicsEngine().Advance(world.Admitted, candidate, inputs, PhysicsInteractionSnapshot.Empty);
        _ = PhysicsGpuAbi.ValidateCandidate(candidate, world.Admitted, new(1), interactions, PhysicsInteractionSnapshot.Empty);
        var request = Assert.Single(PhysicsGpuAbi.ReadFiniteWorkActuators(candidate));
        Assert.Equal(FiniteWorkRequestResult.Uncharged, request.LastResult);
        Assert.Equal(0f, request.LastDebit.Value);
        Assert.Equal(Vector3.Zero, LawState.Read(world.Scene, candidate, payload).Velocity);
        var balance = Assert.Single(PhysicsGpuAbi.ReadEnergyStores(candidate));
        // Exact full-charge admission is causal: a later 0.02 J credit cannot retroactively fire this occurrence.
        Assert.InRange(balance.Energy.Value, .09999f, .10001f);
        Assert.InRange(balance.Supplied.Value, .01999f, .02001f);
        Assert.InRange(PhysicsGpuAbi.ReadElectrical(candidate)[0].Debit.Value, .01999f, .02001f);
        Assert.Equal(0f, Assert.Single(PhysicsGpuAbi.ReadAirflow(candidate)).Work.Value);
    }



    [Theory]
    [InlineData(0u, true)]
    [InlineData(1u, true)]
    [InlineData(256u, false)]
    [InlineData(257u, false)]
    [InlineData(uint.MaxValue, false)]
    public void AvailabilityAdmissionValidatesTheFullRawDiscriminant(uint raw, bool valid)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var store = scene.EnergyStore(frame, 10, 10);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Store, store, default, default, 0, true));
        var world = scene.Admit();
        var engine = new WorkshopPhysicsEngine();
        var candidate = new byte[world.Admitted.Length];
        var interactions = engine.Advance(world.Admitted, candidate,
            new PhysicsTickInputs([], []) { AirflowEnabled = [true] }, PhysicsInteractionSnapshot.Empty);
        var offset = world.Scene.World.AirflowSourceOffset(0);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(
            candidate.AsSpan(offset + PhysicsGpuAbi.AirflowAvailabilityOffset), raw);
        // No exposed receiver: either legal availability carries zero power; controls isolate the discriminant.
        System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(candidate.AsSpan(offset + PhysicsGpuAbi.AirflowObservedSpeedOffset), 0);
        if (valid)
            PhysicsGpuAbi.ValidateCandidate(candidate, world.Admitted, new(1), interactions, PhysicsInteractionSnapshot.Empty);
        else
            Assert.Throws<ArgumentException>(() => PhysicsGpuAbi.ValidateCandidate(candidate, world.Admitted,
                new(1), interactions, PhysicsInteractionSnapshot.Empty));
        Assert.Equal(10, PhysicsGpuAbi.ReadEnergyStores(world.Admitted)[0].Balance.Bulk);
        Assert.Equal(0f, PhysicsGpuAbi.ReadAirflow(world.Admitted)[0].CommitWork.Value);
    }

    public enum ObservationDamage { WorkDecreased, LossDecreased, WorkBeyondPower, ExcessPower, WrongFlowSpeed, UnavailablePower }

    [Theory]
    [InlineData(ObservationDamage.WorkDecreased)]
    [InlineData(ObservationDamage.LossDecreased)]
    [InlineData(ObservationDamage.WorkBeyondPower)]
    [InlineData(ObservationDamage.ExcessPower)]
    [InlineData(ObservationDamage.WrongFlowSpeed)]
    [InlineData(ObservationDamage.UnavailablePower)]
    public void WorkerBoundaryRejectsImpossibleContinuousWorkObservations(ObservationDamage damage)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var cargo = scene.Sphere(new(1, 5, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        var store = scene.EnergyStore(frame, 10, 10);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Store, store, default, default, 0, true));
        scene.AirflowTarget(cargo);
        var world = scene.Admit();
        var engine = new WorkshopPhysicsEngine();
        var inputs = new PhysicsTickInputs([], []) { AirflowEnabled = [true] };
        var first = new byte[world.Admitted.Length];
        var firstInteractions = engine.Advance(world.Admitted, first, inputs, PhysicsInteractionSnapshot.Empty);
        _ = PhysicsGpuAbi.ValidateCandidate(first, world.Admitted, new(1), firstInteractions, PhysicsInteractionSnapshot.Empty);
        var second = new byte[first.Length];
        var secondInteractions = engine.Advance(first, second, inputs, firstInteractions);
        _ = PhysicsGpuAbi.ValidateCandidate(second, first, new(2), secondInteractions, firstInteractions);
        var offset = world.Scene.World.AirflowSourceOffset(0);
        var field = damage switch
        {
            ObservationDamage.WorkDecreased or ObservationDamage.WorkBeyondPower => PhysicsGpuAbi.AirflowWorkOffset,
            ObservationDamage.LossDecreased => PhysicsGpuAbi.AirflowLossOffset,
            ObservationDamage.WrongFlowSpeed or ObservationDamage.UnavailablePower => PhysicsGpuAbi.AirflowObservedSpeedOffset,
            _ => PhysicsGpuAbi.AirflowObservedPowerOffset
        };
        // The source is rated at 108 W and 12 m/s; 100 J in a 1/120 s commit cannot be paid.
        var value = damage switch
        {
            ObservationDamage.WorkBeyondPower => 100f,
            ObservationDamage.ExcessPower => 216f,
            ObservationDamage.WrongFlowSpeed => 24f,
            _ => 0f
        };
        System.Buffers.Binary.BinaryPrimitives.WriteSingleLittleEndian(second.AsSpan(offset + field), value);
        Assert.Throws<ArgumentException>(() =>
            PhysicsGpuAbi.ValidateCandidate(second, first, new(2), secondInteractions, firstInteractions));
        var preserved = Assert.Single(PhysicsGpuAbi.ReadAirflow(first));
        Assert.True(preserved.Work.Value > 0 && preserved.JetLoss.Value > 0);
        Assert.Equal(12f, preserved.FlowSpeed);
    }


    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DiscreteReleasePrecedesSharedContinuousJouleAllocation(bool phaseSupply, bool reverseDeclarations)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 3, 0), new(.1f), colliderOffset: new(0, -2, 0));
        var otherFrame = scene.StaticBox(new(0, 3, 4), new(.1f), colliderOffset: new(0, -2, 0));
        var payload = scene.Sphere(new(0, 3, 0), .35f, 1, new(0, 0, 0, 0), default, default, 0, 0);
        var otherPayload = scene.Sphere(new(0, 3, 4), .35f, 1, new(0, 0, 0, 0), default, default, 0, 0);
        var store = scene.EnergyStore(frame, .1f, .1f, 2.4f);
        var firstRequest = scene.FiniteWork(frame, payload, .1f, .1f, sharedStore: store, targetSpeed: .2f);
        var secondRequest = scene.FiniteWork(otherFrame, otherPayload, .1f, .1f, sharedStore: store, targetSpeed: .2f);
        var firstNozzle = scene.StaticBox(new(0, 7, 0), new(.1f));
        var secondNozzle = scene.StaticBox(new(0, 7, 4), new(.1f));
        var firstCargo = scene.Sphere(new(1, 7, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        var secondCargo = scene.Sphere(new(1, 7, 4), .1f, 2, LawMaterial.Domino, default, default, 0, 0);
        var input = new ContinuousWorkInput(ContinuousWorkInputKind.Store, store, default, default, 0, true);
        var firstFlow = scene.Airflow(firstNozzle, input, 4.8f, 4);
        var secondFlow = scene.Airflow(secondNozzle, input, 4.8f, 8);
        scene.AirflowTarget(firstCargo); scene.AirflowTarget(secondCargo);
        var directFlow = default(AirflowSourceId);
        var directCargo = default(GpuBodyId);
        if (phaseSupply)
        {
            var provider = scene.StaticBox(new(0, 12, 4), new(.1f));
            var supply = scene.LimitedSource(provider, .03f, 3.6f);
            scene.StoreConsumer(frame, supply, store);
            var nozzle = scene.StaticBox(new(0, 12, 0), new(.1f));
            directCargo = scene.Sphere(new(1, 12, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
            directFlow = scene.ElectricalAirflow(nozzle, supply, 4.8f, 4);
            scene.AirflowTarget(directCargo);
        }
        if (reverseDeclarations) scene.ReverseResourceDeclarations();
        var world = scene.Admit();
        var occurrence = new ActivationOccurrence(ActivationOccurrenceKind.Contact, new(1), new(0, 0),
            new(new(1), frame, new(1), new(0, 0), new(0), ActivationCauseKind.Physical));
        var inputs = new PhysicsTickInputs(phaseSupply ? [ElectricalEnable.Enabled] : [], [])
        {
            AirflowEnabled = Enumerable.Repeat(true, world.Scene.AirflowSources.Length).ToArray(),
            StorageRoutes = phaseSupply ? [true] : [],
            PhysicalRequests = [
                new(PhysicalRequestTarget.ForActuator(firstRequest), PhysicalRequestOperation.Release, occurrence),
                new(PhysicalRequestTarget.ForActuator(secondRequest), PhysicalRequestOperation.Release, occurrence)]
        };
        var candidate = new byte[world.Admitted.Length];
        var interactions = new WorkshopPhysicsEngine().Advance(world.Admitted, candidate, inputs, PhysicsInteractionSnapshot.Empty);
        _ = PhysicsGpuAbi.ValidateCandidate(candidate, world.Admitted, new(1), interactions, PhysicsInteractionSnapshot.Empty);
        var requests = PhysicsGpuAbi.ReadFiniteWorkActuators(candidate).ToDictionary(row => row.Id);
        Assert.Equal(FiniteWorkRequestResult.Fired, requests[firstRequest].LastResult);
        Assert.Equal(FiniteWorkRequestResult.Uncharged, requests[secondRequest].LastResult);
        Assert.Equal(0f, requests[secondRequest].LastDebit.Value);
        // This isolated arithmetic fixture distinguishes duplicated budgets and a second full-charge release.
        const float joules = .00001f, speed = .00001f;
        Assert.InRange(requests[firstRequest].LastDebit.Value, .02f - joules, .02f + joules);
        Assert.InRange(LawState.Read(world.Scene, candidate, payload).Velocity.Y, .2f - speed, .2f + speed);
        Assert.Equal(Vector3.Zero, LawState.Read(world.Scene, candidate, otherPayload).Velocity);
        var flows = PhysicsGpuAbi.ReadAirflow(candidate).ToDictionary(row => row.Id);
        var available = phaseSupply ? .09f : .08f;
        Assert.InRange(flows[firstFlow].Work.Value, available / 3 - joules, available / 3 + joules);
        Assert.InRange(flows[secondFlow].Work.Value, available * 2 / 3 - joules, available * 2 / 3 + joules);
        Assert.InRange(LawState.Read(world.Scene, candidate, firstCargo).Velocity.X,
            available / 12 - speed, available / 12 + speed);
        Assert.InRange(LawState.Read(world.Scene, candidate, secondCargo).Velocity.X,
            available / 24 - speed, available / 24 + speed);
        var balance = Assert.Single(PhysicsGpuAbi.ReadEnergyStores(candidate));
        Assert.InRange(balance.Energy.Value, 0, joules);
        Assert.InRange(balance.Supplied.Value, (phaseSupply ? .01f : 0) - joules, (phaseSupply ? .01f : 0) + joules);
        if (phaseSupply)
        {
            var battery = PhysicsGpuAbi.ReadElectrical(candidate)[0];
            Assert.InRange(battery.Debit.Value, .03f - joules, .03f + joules);
            Assert.InRange(flows[directFlow].Work.Value, .02f - joules, .02f + joules);
            Assert.InRange(LawState.Read(world.Scene, candidate, directCargo).Velocity.X, .005f - speed, .005f + speed);
        }
    }
}
