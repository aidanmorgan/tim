using System;

namespace CuriousContraptions.Gpu;

public static class PhysicsReadValidation
{
    public static void ValidateScene(this PhysicsBodyReadSet bodies, IPhysicsSceneDeclaration scene, SimulationEpoch epoch, SimulationTick tick)
    {
        bodies.ValidateTime(epoch, tick);
        var index = 0;
        foreach (var declared in scene.Bodies)
        {
            if (declared.Motion != RigidMotionKind.Dynamic) continue;
            if (index >= bodies.Count || bodies[index].Body.Id != declared.Id)
                throw new ArgumentException("Complete body set differs from the installed scene.");
            var value = bodies[index++]; value.Validate();
            if (!F32Bits.Equal(value.LocalCentreOfMass, declared.MassProperties.LocalCentreOfMass))
                throw new ArgumentException("Committed centre of mass differs from declaration.");
            if (tick.Value == 0 && (value.Body.Cell != declared.Cell ||
                !F32Bits.Equal(value.Body.Local, declared.Local) ||
                !value.Body.Velocity.HasSameBits(declared.Velocity) ||
                !F32Bits.Equal(value.Rotation.X, declared.Rotation.X) ||
                !F32Bits.Equal(value.Rotation.Y, declared.Rotation.Y) ||
                !F32Bits.Equal(value.Rotation.Z, declared.Rotation.Z) ||
                !F32Bits.Equal(value.Rotation.W, declared.Rotation.W) ||
                !value.AngularVelocity.HasSameBits(declared.AngularVelocity)))
                throw new ArgumentException("Admission changed a canonical body.");
        }
        if (index != bodies.Count) throw new ArgumentException($"Read contains a foreign dynamic body: expected {index}, received {bodies.Count}, scene bodies {scene.Bodies.Length}, epoch {epoch.Value}, tick {tick.Value}.");
    }

    public static void ValidateAdvance(this PhysicsElectricalRead current, PhysicsElectricalRead previous)
    {
        if (current.Count != previous.Count) throw new ArgumentException("Electrical source population changed.");
        for (var i = 0; i < current.Count; i++)
        {
            var before = previous[i]; var after = current[i];
            if (before.Id != after.Id || before.Owner != after.Owner || after.Remaining.Value > before.Remaining.Value ||
                MathF.Abs(before.Remaining.Value - after.Remaining.Value - after.Debit.Value) >
                    Residual(before.Remaining.Value) + Residual(after.Debit.Value))
                throw new ArgumentException("Electrical source debit changed identity or balance.");
        }
    }
    // Boundary validation only: two ULPs cover a rounded sum/subtraction; ordinary residuals do not fault valid ticks.
    public static float Residual(float balance) => 2f * MathF.Max(float.Epsilon, MathF.BitIncrement(balance) - balance);

    public static void ValidateAdvance(this PhysicsContactWorkRead current, PhysicsContactWorkRead previous)
    {
        if (current.Count != previous.Count) throw new ArgumentException("Work population changed within an epoch.");
        for (var owner=0; owner<current.Count; owner++)
        {
            var before=previous[owner]; var after=current[owner];
            if (before.Id != after.Id || before.Owner != after.Owner || after.OccurrenceCount < before.OccurrenceCount)
                throw new ArgumentException("Work owner state reversed or changed identity.");
        }
    }

    public static void ValidateScene(this PhysicsContactWorkRead current, IPhysicsSceneDeclaration scene)
    {
        if (current.Count != scene.ContactWorks.Length) throw new ArgumentException("Work-owner population differs from scene.");
        for (var i=0; i<current.Count; i++)
        {
            var declared=scene.ContactWorks[i];
            if (current[i].Id != declared.Id || current[i].Owner != declared.Owner ||
                current[i].RemainingEnergy.Value > declared.InitialEnergy.Value)
                throw new ArgumentException("Work owner or reservoir differs from declaration.");
        }
    }

    public static void ValidateAdvance(this PhysicsCaptureRead captures, PhysicsCaptureRead previous)
    {
        for(var i=0;i<previous.Count;i++)
        {
            var prior=previous[i];
            if(prior.Phase==CaptureLatchPhase.Latched && (!captures.TryGet(new(prior.Sensor,prior.Target),out var current) || current!=prior))
                throw new ArgumentException("Earned capture state was lost or changed.");
        }
    }
}
