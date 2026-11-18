using System;
using System.Numerics;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Simulation;

internal sealed partial class WorkshopPhysicsEngine
{
    private void BindExtrusions(ReadOnlySpan<byte> world, PhysicsWorldLayout layout)
    {
        foreach (var collider in _colliders)
        {
            var offset = layout.ColliderOffset(collider.Index);
            var joint = ReadU32(world, offset + PhysicsGpuAbi.ColliderExtrusionJointOffset);
            if (joint == 0) continue;
            collider.Extrusion = _joints[checked((int)joint - 1)];
            collider.Cut = ReadF32(world, offset + PhysicsGpuAbi.ColliderExtrusionCutOffset);
            collider.DistalOffset = ReadF32(world, offset + PhysicsGpuAbi.ColliderExtrusionDistalOffset);
            WorkshopCollisionKernels.RefreshCollider(collider);
        }
    }

    private void AddOwnedManifolds(int first)
    {
        var firstManifold = _manifolds.Count;
        for (var i = first; i < _contacts.Count;)
        {
            var contact = _contacts[i];
            contact.FaceA = Face(contact.ColA, -contact.Normal);
            contact.FaceB = Face(contact.ColB, contact.Normal);
            contact.A = Owner(contact.ColA, contact.FaceA);
            contact.B = Owner(contact.ColB, contact.FaceB);
            if (contact.A == contact.B) { _contacts.RemoveAt(i); continue; }
            ContactManifold? match = null;
            for (var m = firstManifold; m < _manifolds.Count; m++)
            {
                var existing = _manifolds[m];
                if (existing.Points[0].A == contact.A && existing.Points[0].B == contact.B) { match = existing; break; }
            }
            if (match is null)
            {
                while (_manifoldPool.Count <= _manifolds.Count) _manifoldPool.Add(new());
                match = _manifoldPool[_manifolds.Count];
                match.Points.Clear();
                _manifolds.Add(match);
            }
            match.Points.Add(contact);
            i++;
        }
    }

    private static SimBody Owner(SimCollider collider, BoxFace? face) =>
        collider.Extrusion is { } joint && face == BoxFace.NegativeY ? joint.A : collider.Body;

    private static BoxFace? Face(SimCollider collider, Vector3 outward)
    {
        if (collider.Shape != ShapeKind.Box) return null;
        var local = new Vector3(Vector3.Dot(outward, collider.U0), Vector3.Dot(outward, collider.U1),
            Vector3.Dot(outward, collider.U2));
        var absolute = Vector3.Abs(local);
        if (absolute.X >= absolute.Y && absolute.X >= absolute.Z) return local.X >= 0 ? BoxFace.PositiveX : BoxFace.NegativeX;
        if (absolute.Y >= absolute.Z) return local.Y >= 0 ? BoxFace.PositiveY : BoxFace.NegativeY;
        return local.Z >= 0 ? BoxFace.PositiveZ : BoxFace.NegativeZ;
    }

    private void RefreshExtrusionContacts()
    {
        foreach (var collider in _colliders)
            if (collider.Extrusion is not null) WorkshopCollisionKernels.RefreshCollider(collider);
        for (var index = _manifolds.Count - 1; index >= 0; index--)
        {
            var manifold = _manifolds[index];
            var invalid = false;
            foreach (var point in manifold.Points)
                invalid |= !Retained(point, true) || !Retained(point, false);
            if (!invalid) continue;
            // Friction and normal blocks couple these anchors. Retire the whole manifold together rather than keep
            // a partial warm load on a changed surface; the next substep generates the remaining physical contacts.
            foreach (var point in manifold.Points) _contacts.Remove(point);
            _manifolds.RemoveAt(index);
        }
    }

    private static bool Retained(ContactPoint contact, bool first)
    {
        var collider = first ? contact.ColA : contact.ColB;
        if (collider.Extrusion is null) return true;
        var body = first ? contact.A : contact.B;
        var face = first ? contact.FaceA : contact.FaceB;
        var outward = first ? -contact.Normal : contact.Normal;
        if (Owner(collider, Face(collider, outward)) != body) return false;
        if (face is BoxFace.NegativeY or BoxFace.PositiveY) return true;
        var arm = first ? new Vector3(contact.RAx, contact.RAy, contact.RAz) : new(contact.RBx, contact.RBy, contact.RBz);
        var anchor = contact.Point + WorkshopConstraintSolver.AnchorDisplacement(body, arm);
        var axial = Vector3.Dot(anchor - collider.Position, collider.U1);
        return axial >= -collider.Hy && axial <= collider.Hy;
    }
}
