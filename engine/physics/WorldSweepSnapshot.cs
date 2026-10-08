using System;
using CuriousContraptions.Physics;
namespace CuriousContraptions;

internal readonly record struct PreparedColliderGroup(ColliderQuerySet Set,RigidPose Pose,SweepObstacleKind Kind,PhysicsBodyId Body);

    public sealed class WorldSweepSnapshot
    {
        private readonly PreparedColliderGroup[] _surfaces;
        internal WorldSweepSnapshot(PreparedColliderGroup[] surfaces)
        {
            ArgumentNullException.ThrowIfNull(surfaces);
            _surfaces=(PreparedColliderGroup[])surfaces.Clone();
        }

        public WorldOverlapResult? FindOverlap(CompoundGeometry geometry,RigidPose pose,double maximumPenetration)
        {
            if(!double.IsFinite(maximumPenetration)||maximumPenetration<0)
                throw new ArgumentOutOfRangeException(nameof(maximumPenetration));
            var moving=new PhysicsBody(new(0),PhysicsMotionType.Static,pose,default,default);
            var a=new CompoundMotion(geometry,moving.CreateTrajectory(0,default));
            foreach(var surface in _surfaces)
            {
                var body=new PhysicsBody(new(1),PhysicsMotionType.Static,surface.Pose,default,default);
                var overlap=CompoundCollision.FindOverlap(a,new(surface.Set.Geometry,body.CreateTrajectory(0,default)),maximumPenetration,out _);
                if(overlap is { } hit) return new(surface.Kind,surface.Body,hit.ChildA,hit.ChildB,
                    surface.Set.Surface(hit.ChildB),hit.Separation);
            }
            return null;
        }

        public WorldSweepResult Sweep(CompoundGeometry geometry,RigidPose pose,CollisionVector displacement)
        {
            ArgumentNullException.ThrowIfNull(geometry);
            var travel=displacement; var length=travel.Length;
            if(!double.IsFinite(length)) throw new ArgumentOutOfRangeException(nameof(displacement));
            var moving=new PhysicsBody(new(0),PhysicsMotionType.Kinematic,pose,travel,default).CreateTrajectory(1,default);
            var a=new CompoundMotion(geometry,moving);
            var best=new WorldSweepResult(WorldSweepStatus.Clear,length,default,SweepObstacleKind.None,null,SweepSurfaceKind.None,0);
            double earliest=1;
            foreach(var surface in _surfaces)
            {
                var stationary=new PhysicsBody(new(1),PhysicsMotionType.Static,surface.Pose,default,default).CreateTrajectory(1,default);
                var b=new CompoundMotion(surface.Set.Geometry,stationary);
                foreach(var pair in CompoundCollision.Candidates(a,b,1,ConvexSweep.ContactDistance).Pairs)
                {
                    var childA=a.Child(pair.A); var childB=b.Child(pair.B);
                    var initial=ConvexSeparation.Query(childA.At(0),childB.At(0));
                    var overlapping=initial.UpperBound < -ConvexSweep.ContactDistance;
                    ConvexSeparationResult separation; double time;
                    if(overlapping) { separation=initial; time=0; }
                    else
                    {
                        // For fixed-orientation convex bodies, a non-closing support
                        // plane certifies the complete linear path. Other compound
                        // children are still queried, including opposite bore walls.
                        var gap=CollisionVector.Dot(initial.Normal,childA.At(0).Support(-initial.Normal)-childB.At(0).Support(initial.Normal));
                        if(gap>=-ConvexSweep.ContactDistance&&CollisionVector.Dot(initial.Normal,travel)>=0) continue;
                        var hit=ConvexSweep.Cast(childA,childB,earliest,0);
                        if(hit.Status==ConvexSweepStatus.Clear) continue;
                        separation=hit.Separation; time=hit.Time;
                        if(best.Status!=WorldSweepStatus.Clear&&time>=earliest) continue;
                    }
                    var normal=separation.Normal;
                    best=new(overlapping?WorldSweepStatus.Overlapping:WorldSweepStatus.Contact,time*length,
                        normal,surface.Kind,surface.Body,surface.Set.Surface(pair.B),
                        Math.Max(0,-separation.LowerBound));
                    earliest=time;
                    if(overlapping) return best; // Stable declaration-order initial-overlap priority.
                }
            }
            return best;
        }
    }
