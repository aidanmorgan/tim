using System;
using System.Collections.Generic;
using System.Numerics;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

/// <summary>Reusable pre-solve state. Contact manifolds are rebuilt; persistent contact caches remain read-only until acceptance.</summary>
internal sealed class PrismaticTrialWorkspace
{
    private BodyState[] _bodies = new BodyState[16];
    private float[] _jointImpulses = new float[32], _hingeImpulses = new float[32];
    private float[] _transmissionImpulses = new float[4], _couplingWindowImpulses = new float[4], _tensileImpulses = new float[4];
    private float[] _stepWork = new float[4], _stepPositive = new float[4];
    private int[] _parents = new int[16];
    private bool[] _affected = new bool[16];

    private static void Reserve<T>(ref T[] values, int count)
    {
        if (count <= values.Length) return;
        Array.Resize(ref values, checked(Math.Max(count, values.Length * 2)));
    }

    public void Capture(List<SimBody> bodies, int bodySlots, List<SimJoint> joints, List<SimRevoluteJoint> hinges,
        List<SimMechanicalTransmission> transmissions, List<SimTensileRoute> ropes,
        List<(SimCollider ColA, SimCollider ColB)> pairs)
    {
        Reserve(ref _bodies, bodies.Count);
        Reserve(ref _jointImpulses, checked(joints.Count * PrismaticRows.Count));
        Reserve(ref _hingeImpulses, checked(hinges.Count * RevoluteRows.Count));
        Reserve(ref _transmissionImpulses, transmissions.Count);
        Reserve(ref _couplingWindowImpulses, transmissions.Count);
        Reserve(ref _stepWork, transmissions.Count); Reserve(ref _stepPositive, transmissions.Count);
        Reserve(ref _tensileImpulses, ropes.Count);
        Reserve(ref _parents, bodySlots);
        Reserve(ref _affected, bodySlots);
        Array.Clear(_affected, 0, bodySlots);
        for (var i = 0; i < bodySlots; i++) _parents[i] = i;
        for (var i = 0; i < bodies.Count; i++) _bodies[i] = BodyState.Capture(bodies[i]);
        for (var i = 0; i < joints.Count; i++)
        {
            joints[i].Impulses.CopyTo(_jointImpulses, i * PrismaticRows.Count);
            Join(joints[i].A, joints[i].B);
        }
        for (var i = 0; i < hinges.Count; i++)
        {
            hinges[i].Impulses.CopyTo(_hingeImpulses, i * RevoluteRows.Count);
            Join(hinges[i].A, hinges[i].B);
        }
        CaptureCouplings(transmissions, ropes, pairs);
    }

    private void CaptureCouplings(List<SimMechanicalTransmission> transmissions, List<SimTensileRoute> ropes,
        List<(SimCollider ColA, SimCollider ColB)> pairs)
    {
        for (var i = 0; i < transmissions.Count; i++)
        {
            var transmission = transmissions[i];
            _transmissionImpulses[i] = transmission.Impulse;
            _couplingWindowImpulses[i] = transmission.WindowImpulse;
            _stepWork[i] = transmission.StepWork; _stepPositive[i] = transmission.StepPositive;
            var a = DynamicEnd(transmission.Driving.A, transmission.Driving.B);
            var b = DynamicEnd(transmission.Driven.A, transmission.Driven.B);
            Join(a, b);
        }
        for (var i = 0; i < ropes.Count; i++)
        {
            var rope = ropes[i];
            _tensileImpulses[i] = rope.Impulse;
            var first = DynamicEnd(rope.A, rope.B);
            Join(rope.A, rope.B);
            foreach (var guide in rope.Guides)
            {
                Join(first, guide.Body);
                if (first.Motion != RigidMotionKind.Dynamic) first = guide.Body;
            }
        }
        foreach (var pair in pairs) Join(pair.ColA.Body, pair.ColB.Body);
    }

    private static SimBody DynamicEnd(SimBody a, SimBody b) => a.Motion == RigidMotionKind.Dynamic ? a : b;
    private int Root(int slot)
    {
        while (_parents[slot] != slot)
        {
            _parents[slot] = _parents[_parents[slot]];
            slot = _parents[slot];
        }
        return slot;
    }
    private void Join(SimBody a, SimBody b)
    {
        if (a.Motion != RigidMotionKind.Dynamic || b.Motion != RigidMotionKind.Dynamic) return;
        _parents[Root(a.BodySlot)] = Root(b.BodySlot);
    }

    public void MarkAffected(List<SimJoint> joints, float h)
    {
        foreach (var joint in joints)
            if (!PrismaticMotorSolver.MeasureWork(joint, h))
                _affected[Root(DynamicEnd(joint.A, joint.B).BodySlot)] = true;
    }

    public void Restore(List<SimBody> bodies, List<SimJoint> joints, List<SimRevoluteJoint> hinges,
        List<SimMechanicalTransmission> transmissions, List<SimTensileRoute> ropes, float multiplier)
    {
        for (var i = 0; i < bodies.Count; i++) _bodies[i].Restore(bodies[i]);
        RestoreConstraintImpulses(joints, hinges, transmissions, ropes);
        foreach (var joint in joints)
        {
            joint.MotorImpulse = 0f;
            joint.StepWork = 0f;
            joint.EffortMultiplier = _affected[Root(DynamicEnd(joint.A, joint.B).BodySlot)] ? multiplier : 1f;
        }
        for (var i = 0; i < transmissions.Count; i++)
        {
            transmissions[i].WindowImpulse = _couplingWindowImpulses[i];
            transmissions[i].StepWork = _stepWork[i];
            transmissions[i].StepPositive = _stepPositive[i];
        }
    }

    /// <summary>Restore warm-start memory without undoing accepted velocities or paid channel work.
    /// An instantaneous response must not be applied again as the next substep's warm start.</summary>
    public void RestoreConstraintImpulses(List<SimJoint> joints, List<SimRevoluteJoint> hinges,
        List<SimMechanicalTransmission> transmissions, List<SimTensileRoute> ropes)
    {
        for (var i = 0; i < joints.Count; i++)
            Array.Copy(_jointImpulses, i * PrismaticRows.Count, joints[i].Impulses, 0, PrismaticRows.Count);
        for (var i = 0; i < hinges.Count; i++)
            Array.Copy(_hingeImpulses, i * RevoluteRows.Count, hinges[i].Impulses, 0, RevoluteRows.Count);
        for (var i = 0; i < transmissions.Count; i++) transmissions[i].Impulse = _transmissionImpulses[i];
        for (var i = 0; i < ropes.Count; i++) ropes[i].Impulse = _tensileImpulses[i];
    }

    public void RestoreOutside(HashSet<SimBody> component, List<SimBody> bodies, List<SimMechanicalTransmission> transmissions)
    {
        for (var i = 0; i < bodies.Count; i++)
            if (!component.Contains(bodies[i])) _bodies[i].Restore(bodies[i]);
        for (var i = 0; i < transmissions.Count; i++)
        {
            var value = transmissions[i];
            if (component.Contains(value.Driving.A) || component.Contains(value.Driving.B) ||
                component.Contains(value.Driven.A) || component.Contains(value.Driven.B)) continue;
            value.WindowImpulse = _couplingWindowImpulses[i];
            value.StepWork = _stepWork[i];
            value.StepPositive = _stepPositive[i];
        }
    }

    private readonly record struct BodyState(int CellX, int CellY, int CellZ, Vector3 Local, Vector3 Position,
        Quaternion Rotation, Vector3 Velocity, Vector3 Spin, Quaternion RelativeRotation, float ContactWork,
        Vector3 WindowV, Vector3 WindowW, Vector3 WindowContactTranslationalImpulse, Vector3 WindowContactAngularImpulse)
    {
        public static BodyState Capture(SimBody body) => new(body.CellX, body.CellY, body.CellZ,
            new(body.LocalX, body.LocalY, body.LocalZ), body.Position, body.Rotation, body.LinearVelocity, body.AngularVelocity,
            body.TickRelativeRotation, body.ContactWork, body.WindowV, body.WindowW, body.WindowContactTranslationalImpulse,
            body.WindowContactAngularImpulse);
        public void Restore(SimBody body)
        {
            body.CellX = CellX; body.CellY = CellY; body.CellZ = CellZ;
            body.LocalX = Local.X; body.LocalY = Local.Y; body.LocalZ = Local.Z;
            body.Px = Position.X; body.Py = Position.Y; body.Pz = Position.Z;
            body.Qx = Rotation.X; body.Qy = Rotation.Y; body.Qz = Rotation.Z; body.Qw = Rotation.W;
            body.Vx = Velocity.X; body.Vy = Velocity.Y; body.Vz = Velocity.Z;
            body.Wx = Spin.X; body.Wy = Spin.Y; body.Wz = Spin.Z;
            body.TickRelativeRotation = RelativeRotation;
            body.ContactWork = ContactWork;
            // The open accepted-work window as it stood before the trial: the failed trial's credit and new window are discarded.
            body.WindowV = WindowV; body.WindowW = WindowW;
            body.WindowContactTranslationalImpulse = WindowContactTranslationalImpulse; body.WindowContactAngularImpulse = WindowContactAngularImpulse;
            WorkshopCollisionKernels.UpdateDynamicFrame(body);
        }
    }
}
