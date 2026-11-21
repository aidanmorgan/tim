using System;
using System.Collections.Generic;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

/// <summary>Reusable pre-solve state. Contact manifolds are rebuilt; persistent contact caches remain read-only until acceptance.</summary>
internal sealed class PrismaticTrialWorkspace
{
    private BodyTrialState[] _bodies = new BodyTrialState[16];
    private float[] _jointImpulses = new float[32], _hingeImpulses = new float[32];
    private float[] _transmissionImpulses = new float[4], _tensileImpulses = new float[4];
    private PrismaticTrialState[] _jointStates = new PrismaticTrialState[4];
    private RevoluteTrialState[] _hingeStates = new RevoluteTrialState[4];
    private TransmissionTrialState[] _couplingStates = new TransmissionTrialState[4];
    private TensileTrialState[] _ropeStates = new TensileTrialState[4];
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
        Reserve(ref _jointStates, joints.Count); Reserve(ref _hingeStates, hinges.Count);
        Reserve(ref _couplingStates, transmissions.Count); Reserve(ref _ropeStates, ropes.Count);
        Reserve(ref _tensileImpulses, ropes.Count);
        Reserve(ref _parents, bodySlots);
        Reserve(ref _affected, bodySlots);
        Array.Clear(_affected, 0, bodySlots);
        for (var i = 0; i < bodySlots; i++) _parents[i] = i;
        for (var i = 0; i < bodies.Count; i++) _bodies[i] = BodyTrialState.Capture(bodies[i]);
        for (var i = 0; i < joints.Count; i++)
        {
            _jointStates[i] = PrismaticTrialState.Capture(joints[i]);
            joints[i].Impulses.CopyTo(_jointImpulses, i * PrismaticRows.Count);
            Join(joints[i].A, joints[i].B);
        }
        for (var i = 0; i < hinges.Count; i++)
        {
            _hingeStates[i] = RevoluteTrialState.Capture(hinges[i]);
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
            _couplingStates[i] = TransmissionTrialState.Capture(transmission);
            var a = DynamicEnd(transmission.Driving.A, transmission.Driving.B);
            var b = DynamicEnd(transmission.Driven.A, transmission.Driven.B);
            Join(a, b);
        }
        for (var i = 0; i < ropes.Count; i++)
        {
            var rope = ropes[i];
            _tensileImpulses[i] = rope.Impulse;
            _ropeStates[i] = TensileTrialState.Capture(rope);
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
        RestoreState(bodies, joints, hinges, transmissions, ropes);
        foreach (var joint in joints)
        {
            joint.MotorImpulse = 0f;
            joint.StepWork = 0f;
            joint.EffortMultiplier = _affected[Root(DynamicEnd(joint.A, joint.B).BodySlot)] ? multiplier : 1f;
        }
    }

    /// <summary>Restore the complete entrance without selecting a different motor effort.</summary>
    public void RestoreState(List<SimBody> bodies, List<SimJoint> joints, List<SimRevoluteJoint> hinges,
        List<SimMechanicalTransmission> transmissions, List<SimTensileRoute> ropes)
    {
        for (var i = 0; i < bodies.Count; i++) _bodies[i].Restore(bodies[i]);
        RestoreConstraintImpulses(joints, hinges, transmissions, ropes);
        for (var i = 0; i < joints.Count; i++) _jointStates[i].Restore(joints[i]);
        for (var i = 0; i < hinges.Count; i++) _hingeStates[i].Restore(hinges[i]);
        for (var i = 0; i < ropes.Count; i++) _ropeStates[i].Restore(ropes[i]);
        for (var i = 0; i < transmissions.Count; i++) _couplingStates[i].Restore(transmissions[i]);
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
            _couplingStates[i].Restore(value);
        }
    }

}
