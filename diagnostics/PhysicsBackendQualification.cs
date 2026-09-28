#if DEBUG || PLAYTEST
using Godot;
using System;
using System.Collections.Generic;

namespace CuriousContraptions;

/// <summary>Executable backend qualification, not game collision routing.
/// Each case owns an isolated physics space and frees all server resources.</summary>
public static class PhysicsBackendQualification
{
    public enum Probe { SphereTranslation, BoxTranslation, HullTranslation, CompoundPassage, CompoundWall }
    public readonly record struct Observation(Probe Probe, bool Collided, float SafeFraction,
        int Contacts, Vector3 Normal, bool SpaceAvailable);

    public static Observation Run(Probe probe)
    {
        if (!Enum.IsDefined(probe)) throw new ArgumentOutOfRangeException(nameof(probe));
        var shapes=new List<Shape3D>();
        var bodies=new List<Rid>();
        var space=PhysicsServer3D.SpaceCreate();
        try
        {
            PhysicsServer3D.SpaceSetActive(space,true);
            var compound=probe is Probe.CompoundPassage or Probe.CompoundWall;
            var obstacle=PhysicsServer3D.BodyCreate(); bodies.Add(obstacle);
            PhysicsServer3D.BodySetMode(obstacle,PhysicsServer3D.BodyMode.Static);
            PhysicsServer3D.BodySetSpace(obstacle,space);
            if(compound)
            {
                // Square hollow passage along X, built from four convex walls.
                // No special collision equation for the passage or moving shape.
                foreach(var side in new[]{-1,1})
                {
                    var horizontal=new BoxShape3D { Size=new(2,.2f,2.4f) }; shapes.Add(horizontal);
                    PhysicsServer3D.BodyAddShape(obstacle,horizontal.GetRid(),new(Basis.Identity,new(3,side*1.1f,0)));
                    var vertical=new BoxShape3D { Size=new(2,2,.2f) }; shapes.Add(vertical);
                    PhysicsServer3D.BodyAddShape(obstacle,vertical.GetRid(),new(Basis.Identity,new(3,0,side*1.1f)));
                }
            }
            else
            {
                var wall=new BoxShape3D { Size=new(.2f,4,4) }; shapes.Add(wall);
                PhysicsServer3D.BodyAddShape(obstacle,wall.GetRid(),new(Basis.Identity,new(3,0,0)));
            }
            Shape3D moving=probe switch
            {
                Probe.SphereTranslation=>new SphereShape3D { Radius=.25f },
                Probe.BoxTranslation=>new BoxShape3D { Size=Vector3.One*.5f },
                Probe.HullTranslation or Probe.CompoundPassage or Probe.CompoundWall=>new ConvexPolygonShape3D
                {
                    Points=[new(-.25f,-.25f,-.25f),new(.25f,-.25f,-.25f),
                        new(-.25f,.25f,-.25f),new(.25f,.25f,-.25f),
                        new(-.25f,-.25f,.25f),new(.25f,-.25f,.25f),
                        new(-.25f,.25f,.25f),new(.25f,.25f,.25f)]
                },
                _=>throw new ArgumentOutOfRangeException(nameof(probe))
            };
            shapes.Add(moving);
            var body=PhysicsServer3D.BodyCreate(); bodies.Add(body);
            PhysicsServer3D.BodySetMode(body,PhysicsServer3D.BodyMode.Kinematic);
            PhysicsServer3D.BodySetSpace(body,space);
            PhysicsServer3D.BodyAddShape(body,moving.GetRid());
            using var parameters=new PhysicsTestMotionParameters3D
            {
                From=new(Basis.Identity,new(0,probe==Probe.CompoundWall?1.1f:0,0)),
                Motion=new(6,0,0),Margin=.0001f,MaxCollisions=4
            };
            using var result=new PhysicsTestMotionResult3D();
            var hit=PhysicsServer3D.BodyTestMotion(body,parameters,result);
            return new(probe,hit,result.GetCollisionSafeFraction(),result.GetCollisionCount(),
                hit?result.GetCollisionNormal():Vector3.Zero,PhysicsServer3D.SpaceGetDirectState(space)!=null);
        }
        finally
        {
            foreach(var body in bodies) PhysicsServer3D.FreeRid(body);
            PhysicsServer3D.FreeRid(space);
            foreach(var shape in shapes) shape.Dispose();
        }
    }
}
#endif
