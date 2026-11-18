using System;
using System.Collections.Generic;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private HashSet<SimBody> _impulseComponent = new(16);

    private void RestoreUnrelatedImpulseState(SimBody payload, SimBody frame)
    {
        _impulseComponent.Clear();
        AddImpulseBody(payload);
        AddImpulseBody(frame);
        int previous;
        do
        {
            previous = _impulseComponent.Count;
            foreach (var joint in _joints) ConnectImpulsePair(joint.A, joint.B);
            foreach (var joint in _revoluteJoints) ConnectImpulsePair(joint.A, joint.B);
            foreach (var transmission in _transmissions)
            {
                if (!transmission.Engaged ||
                    !(_impulseComponent.Contains(transmission.Driving.A) || _impulseComponent.Contains(transmission.Driving.B) ||
                      _impulseComponent.Contains(transmission.Driven.A) || _impulseComponent.Contains(transmission.Driven.B))) continue;
                AddImpulseBody(transmission.Driving.A); AddImpulseBody(transmission.Driving.B);
                AddImpulseBody(transmission.Driven.A); AddImpulseBody(transmission.Driven.B);
            }
            foreach (var route in _tensile)
            {
                if (!route.Active) continue;
                var touches = _impulseComponent.Contains(route.A) || _impulseComponent.Contains(route.B);
                foreach (var guide in route.Guides) touches |= _impulseComponent.Contains(guide.Body);
                if (!touches) continue;
                AddImpulseBody(route.A); AddImpulseBody(route.B);
                foreach (var guide in route.Guides) AddImpulseBody(guide.Body);
            }
            foreach (var contact in _contacts)
                if (contact.LambdaN != 0f) ConnectImpulsePair(contact.A, contact.B);
            foreach (var manifold in _manifolds)
                foreach (var patch in manifold.Patches)
                    if (patch.Lambda != System.Numerics.Vector3.Zero || patch.LambdaR1 != 0f || patch.LambdaR2 != 0f)
                        ConnectImpulseRow(patch.Jacobian.Bodies);
        } while (previous != _impulseComponent.Count);
        // Trial work on an independent component is not part of this transaction, even if the
        // ordinary shared solver prepared its rows. Restore velocity, pose and open work credits.
        _impulseTrial.RestoreOutside(_impulseComponent, _dynamicBodies, _transmissions);
    }

    private void AddImpulseBody(SimBody body)
    {
        if (body.Motion == RigidMotionKind.Dynamic) _impulseComponent.Add(body);
    }

    private void ConnectImpulsePair(SimBody a, SimBody b)
    {
        if (!_impulseComponent.Contains(a) && !_impulseComponent.Contains(b)) return;
        AddImpulseBody(a); AddImpulseBody(b);
    }

    private void ConnectImpulseRow(ReadOnlySpan<SimBody> bodies)
    {
        var touches = false;
        foreach (var body in bodies) touches |= _impulseComponent.Contains(body);
        if (touches)
            foreach (var body in bodies) AddImpulseBody(body);
    }
}
