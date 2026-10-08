using System;
using System.Collections.Generic;

namespace CuriousContraptions.Gpu;

/// <summary>Catalogue authoring boundary: compiles geometry/material values, never selects a numerical law.</summary>
public static class WorkshopPhysicsCompiler
{
    private const ulong InternalIdentityBase = 1UL << 32;
    public static PhysicsSceneDeclaration Compile(WorkshopConstruction construction, PhysicsDocumentId document)
    {
        construction.Validate();
        var bodies = new List<RigidBodyDeclaration>();
        var colliders = new List<ColliderDeclaration>();
        var materials = new List<ContactMaterialDeclaration>();
        var sensors = new List<ResidenceSensorDeclaration>();
        var plane = new GpuBodyId(InternalIdentityBase);
        var planeMaterial = new GpuMaterialId(InternalIdentityBase + 1);
        var height = WorkshopInput.Position((double)WorkshopConstruction.WorkbenchSurface.Value);
        bodies.Add(new(plane, RigidMotionKind.Static, new(0, height.Cell, 0),
            new((Half)0, height.Local, (Half)0), CanonicalRotation.Identity, default, default, new((Half)0), default, new((Half)0)));
        materials.Add(new(planeMaterial, new((Half)1), new((Half).1), new((Half).3)));
        colliders.Add(new(new(InternalIdentityBase + 2), plane, planeMaterial,
            ColliderShapeKind.Plane, RigidLocalPose.Identity, new((Half)0), default));
        var next = InternalIdentityBase + 3;
        if (construction.Ball is { } ball)
        {
            var first = PartIdentities(ball.Id); next = Math.Max(next, first + 16);
            var material = new GpuMaterialId(first);
            bodies.Add(new(ball.Id, RigidMotionKind.Dynamic, ball.Cell, ball.Local, ball.Rotation,
                default, default, ball.Material.Mass,
                new((Half)0, (Half)(-WorkshopConstruction.Gravity.Value), (Half)0), ball.Material.Drag));
            materials.Add(new(material, ball.Material.Bounce, new((Half).1), new((Half).3)));
            colliders.Add(new(new(first + 1), ball.Id, material, ColliderShapeKind.Sphere,
                RigidLocalPose.Identity, ball.Material.Radius, default));
        }
        if (construction.Receiver is { } receiver)
        {
            var first = PartIdentities(receiver.Id); next = Math.Max(next, first + 16);
            var material = new GpuMaterialId(first);
            bodies.Add(new(receiver.Id, RigidMotionKind.Static, receiver.Cell, receiver.Local, receiver.Rotation,
                default, default, new((Half)0), default, new((Half)0)));
            materials.Add(new(material, new((Half).12), new((Half).1), new((Half).3)));
            var walls = ReceiverGeometry.Walls;
            for (var i = 0; i < walls.Length; i++) AddBox(first + 1 + (ulong)i, walls[i].Centre, walls[i].HalfExtents);
            if (construction.Ball is { } target)
                sensors.Add(new(new(first + 6), receiver.Id, target.Id, RigidLocalPose.Identity,
                    new((Half)(-.66), (Half)(-.4), (Half)(-.66)),
                    new((Half).66, (Half)((Half).45 + receiver.Capture.Margin.Value), (Half).66),
                    receiver.Capture.SpeedLimit, receiver.Capture.Dwell, receiver.Capture.Participation));
            void AddBox(ulong id, MetreVector position, MetreVector half) =>
                colliders.Add(new(new(id), receiver.Id, material, ColliderShapeKind.Box,
                    new(position, CanonicalRotation.Identity), new((Half)0), half));
        }
        return new(document, next, bodies.ToArray(), colliders.ToArray(), materials.ToArray(), sensors.ToArray());
    }
    public static GpuSensorId CaptureSensor(WorkshopReceiver receiver) => new(checked(PartIdentities(receiver.Id) + 6));

    private static ulong PartIdentities(GpuBodyId id)
    {
        if (id.Value == 0 || id.Value >= InternalIdentityBase) throw new ArgumentException("Authored body identity exceeds its domain.");
        return checked(InternalIdentityBase + 16 + id.Value * 16);
    }
}
