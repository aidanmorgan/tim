using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Tests;

public sealed partial class AirflowRuntimeTests
{

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DeclaredElasticCoordinateFundsContinuousWorkWithoutAnInstantaneousDrive(bool charged)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var rotor = scene.Sphere(new(0, 5, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        scene.DeclareMassFrame(rotor, new(.02f));
        var coordinate = scene.Revolute(frame, rotor, ConstraintFrame.Identity,
            new(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -.5f)), -MathF.PI, MathF.PI, 0);
        scene.RevoluteSpring(coordinate, charged ? .4f : 0);
        var cargo = scene.Sphere(new(1, 5, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Coordinate, default, default, coordinate, 1, true),
            .02f, 12, exclusions: [scene.ColliderOf(rotor)]);
        scene.AirflowTarget(cargo);
        var result = scene.Admit().Step(120);
        var work = Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World)).Work.Value;
        if (charged)
        {
            Assert.True(work > 0, "The ordinary integrated spring must be able to pay a positive airflow load.");
            Assert.True(result[cargo].Velocity.X > 0);
            // The initially stationary spring contains 1/2 * .4 * .5² = .05 J.
            // No external drive exists; cumulative source work must remain inside that finite budget.
            Assert.InRange(work, 0, .0505f);
        }
        else
        {
            Assert.Equal(0f, work);
            Assert.Equal(Vector3.Zero, result[cargo].Velocity);
            Assert.Equal(Vector3.Zero, result[rotor].Angular);
        }
    }


    [Theory]
    [InlineData(1f, 9f, 1f, 10f, true)]
    [InlineData(2f, 9f, 1f, 10f, true)]
    [InlineData(1f, 0f, 1f, 10f, false)]
    [InlineData(1f, 9f, 0f, 10f, false)]
    [InlineData(1f, 9f, 1f, 0f, false)]
    public void FiniteConeTransfersRatedForceAccordingToMass(float mass, float force, float pressure, float energy, bool moves)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var cargo = scene.Sphere(new(1, 5, 0), .1f, mass, LawMaterial.Domino, default, default, 0, 0);
        var store = scene.EnergyStore(frame, 10, energy);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Store, store, default, default, 0, true), force, pressure: pressure);
        scene.AirflowTarget(cargo);
        var world = scene.Admit();
        var final = world.Step(1);
        var read = Assert.Single(PhysicsGpuAbi.ReadAirflow(final.World));
        if (!moves)
        {
            Assert.Equal(Vector3.Zero, final[cargo].Velocity);
            Assert.Equal(0f, read.Work.Value);
            return;
        }
        // Over 1/120 s, v cannot exceed F*t/m. At that upper speed slip remains above
        // 99% for these fixtures, so this independent bracket detects lost force or wrong mass.
        var upper = force / mass / 120f;
        Assert.InRange(final[cargo].Velocity.X, upper * .99f, upper);
        var paidMomentum = read.Work.Value / 12f;
        Assert.InRange(mass * final[cargo].Velocity.X - paidMomentum, -.0001f, .0001f);
        // 0.1 mN s allows ordinary accumulation, below 1% of the smaller expected impulse.
        Assert.True(PhysicsGpuAbi.ReadEnergyStores(final.World)[0].Balance.CompareTo(energy) < 0);
        Assert.True(read.JetLoss.Value > 0);
        Assert.Equal(Vector3.Zero, final[cargo].Angular);
    }

    [Fact]
    public void DifferentFlowSpeedsShareOneFiniteStoreInJoules()
    {
        var scene = new LawScene(false);
        var firstFrame = scene.StaticBox(new(0, 3, 0), new(.1f));
        var secondFrame = scene.StaticBox(new(0, 7, 0), new(.1f));
        var first = scene.Sphere(new(1, 3, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        var second = scene.Sphere(new(1, 7, 0), .1f, 2, LawMaterial.Domino, default, default, 0, 0);
        var store = scene.EnergyStore(firstFrame, .08f, .08f);
        var input = new ContinuousWorkInput(ContinuousWorkInputKind.Store, store, default, default, 0, true);
        scene.Airflow(firstFrame, input, 4.8f, 4);
        scene.Airflow(secondFrame, input, 4.8f, 8);
        scene.AirflowTarget(first); scene.AirflowTarget(second);
        var result = scene.Admit().Step(1);
        // Demands .04/.08 J share .08 J, so both accepted impulses are .006666... N s.
        var flows = PhysicsGpuAbi.ReadAirflow(result.World);
        Assert.InRange(flows[0].Work.Value, .02666f, .02668f);
        Assert.InRange(flows[1].Work.Value, .05332f, .05335f);
        Assert.InRange(result[first].Velocity.X, .00666f, .00668f);
        Assert.InRange(result[second].Velocity.X, .00333f, .00334f);
        Assert.InRange(PhysicsGpuAbi.ReadEnergyStores(result.World)[0].Energy.Value, 0, .000001f);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MultipleSourcesPayOneCoordinateOrCoupledCoordinatesWithoutCopyingEnergy(bool sharedCoordinate)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var rotor = scene.Sphere(new(0, 5, 0), .1f, 1, LawMaterial.Domino, default, new(0, 0, 4), 0, 0);
        scene.DeclareMassFrame(rotor, new(.02f));
        var first = scene.Revolute(frame, rotor, ConstraintFrame.Identity, ConstraintFrame.Identity, -MathF.PI, MathF.PI, 0,
            travel: RevoluteTravel.Continuous);
        var otherRotor = rotor;
        var second = first;
        if (!sharedCoordinate)
        {
            otherRotor = scene.Sphere(new(0, 5, 1), .1f, 1, LawMaterial.Domino, default, new(0, 0, 4), 0, 0);
            scene.DeclareMassFrame(otherRotor, new(.02f));
            second = scene.Revolute(frame, otherRotor, new(new(0, 0, 1), Quaternion.Identity), ConstraintFrame.Identity,
                -MathF.PI, MathF.PI, 0, travel: RevoluteTravel.Continuous);
            scene.Transmission(new(first), new(second), 1);
        }
        var cargo = scene.Sphere(new(1, 5, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        GpuColliderId[] internalColliders = sharedCoordinate ? [scene.ColliderOf(rotor)] : [scene.ColliderOf(rotor), scene.ColliderOf(otherRotor)];
        scene.Airflow(frame, new(ContinuousWorkInputKind.Coordinate, default, default, first, 1, true), 4.2f, 4,
            exclusions: internalColliders);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Coordinate, default, default, second, 1, true), 2.1f, 8,
            exclusions: internalColliders);
        scene.AirflowTarget(cargo);
        var result = scene.Admit(SimulationCadence.Hz240).Step(1);
        var flows = PhysicsGpuAbi.ReadAirflow(result.World);
        var paid = flows.Sum(flow => flow.Work.Value);
        Assert.InRange(paid, .139f, .141f);
        Assert.InRange(result[cargo].Velocity.X, .026f, .0264f);
        var loss = .01f * (16 - result[rotor].Angular.LengthSquared());
        if (!sharedCoordinate) loss += .01f * (16 - result[otherRotor].Angular.LengthSquared());
        Assert.InRange(loss - paid, -.0001f, .0001f);
        Assert.True(result[rotor].Angular.Z >= 0 && result[otherRotor].Angular.Z >= 0);
    }

    [Theory]
    [InlineData(4f, true, true)]
    [InlineData(0f, true, false)]
    [InlineData(4f, false, false)]
    public void CoordinateLoadPaysFromFiniteRotorEnergy(float initialRate, bool connected, bool moves)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var rotor = scene.Sphere(new(0, 5, 0), .1f, 1, LawMaterial.Domino, default, new(0, 0, initialRate), 0, 0);
        scene.DeclareMassFrame(rotor, new(.02f));
        var coordinate = scene.Revolute(frame, rotor, ConstraintFrame.Identity, ConstraintFrame.Identity, -MathF.PI, MathF.PI, 0,
            travel: RevoluteTravel.Continuous);
        var cargo = scene.Sphere(new(1, 5, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Coordinate, default, default, coordinate, 1, connected),
            2.8f, 12, exclusions: [scene.ColliderOf(rotor)]);
        scene.AirflowTarget(cargo);
        var world = scene.Admit(SimulationCadence.Hz240);
        var result = world.Step(1);
        var flow = Assert.Single(PhysicsGpuAbi.ReadAirflow(result.World));
        if (!moves)
        {
            Assert.Equal(0f, flow.Work.Value);
            Assert.Equal(Vector3.Zero, result[cargo].Velocity);
            Assert.InRange(result[rotor].Angular.Z, initialRate - .0001f, initialRate + .0001f);
            return;
        }
        // The isolated rotor starts with .16 J. Two windows request about .14 J; neither may use free drive work.
        Assert.InRange(flow.Work.Value, .139f, .141f);
        var kineticLoss = .01f * (initialRate * initialRate - result[rotor].Angular.LengthSquared());
        Assert.InRange(kineticLoss - flow.Work.Value, -.0001f, .0001f);
        Assert.True(result[cargo].Velocity.X > .011f);
        Assert.True(result[rotor].Angular.Z >= 0);
        var angle = 2 * MathF.Atan2(result[rotor].Rotation.Z, result[rotor].Rotation.W);
        Assert.True(angle > 0, "Payable forward rotation must advance the actual coordinate.");
        var exhausted = world.Step(120);
        var totalPaid = Assert.Single(PhysicsGpuAbi.ReadAirflow(exhausted.World)).Work.Value;
        // The isolated .02 kg m² rotor has exactly .16 J initially; a half-second run must use
        // nearly all payable energy without reversing it or creating an unbounded source.
        Assert.InRange(totalPaid, .159f, .1601f);
        Assert.InRange(exhausted[rotor].Angular.Z, 0, .32f);
        Assert.True(exhausted[cargo].Velocity.X > .012f);
    }

    [Theory]
    [InlineData(.0001f, 14400f, true)]
    [InlineData(0f, 14400f, false)]
    [InlineData(.0001f, 0f, false)]
    public void SmallContinuousPaymentsDoNotMateriallyShortenFiniteOperation(float force, float initial, bool payable)
    {
        var scene = new LawScene(false);
        var frame = scene.StaticBox(new(0, 5, 0), new(.1f));
        var cargo = scene.Sphere(new(1, 5, 0), .1f, 1, LawMaterial.Domino, default, default, 0, 0);
        var store = scene.EnergyStore(frame, 14400, initial);
        scene.Airflow(frame, new(ContinuousWorkInputKind.Store, store, default, default, 0, true), force, 12);
        scene.AirflowTarget(cargo);
        double accepted = 0;
        var result = scene.Admit().Step(1200, (_, state) =>
            accepted += Assert.Single(PhysicsGpuAbi.ReadEnergyStores(state.World)).CommitReleased.Value);
        var balance = Assert.Single(PhysicsGpuAbi.ReadEnergyStores(result.World)).Balance;
        var depletion = initial - (double)balance.Bulk - balance.Remainder;
        if (!payable)
        {
            Assert.Equal(0, accepted);
            Assert.Equal(0, depletion);
            Assert.Equal(Vector3.Zero, result[cargo].Velocity);
            return;
        }
        // Ten seconds requests at most .012 J; 1% loss would materially shorten finite operation.
        // The former whole-ULP debit wastes roughly 4.7 J and fails this independent budget.
        Assert.InRange(accepted, .0118, .01201);
        Assert.InRange(depletion - accepted, 0, .00012);
        Assert.True(result[cargo].Position.X > 1.004f); // Authored force implies about 5 mm travel.
        Assert.True(result[cargo].Velocity.X > .0009f);
    }
}
