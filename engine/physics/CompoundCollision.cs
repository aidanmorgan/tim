using Godot;
using System;

namespace CuriousContraptions.Physics;

public readonly record struct ColliderChildId
{
    public int Index { get; }
    public ColliderChildId(int index)
    {
        if(index<0) throw new ArgumentOutOfRangeException(nameof(index));
        Index=index;
    }
}

public sealed class CompoundGeometry
{
    private readonly ConvexInstance[] _children;
    public int Count=>_children.Length;
    public ConvexInstance this[ColliderChildId child]=>_children[child.Index];
    public CompoundGeometry(ReadOnlySpan<ConvexInstance> children)
    {
        if(children.Length==0) throw new ArgumentException("A compound requires convex children.",nameof(children));
        _children=children.ToArray();
        foreach(var child in _children)
            if(child.Geometry is null) throw new ArgumentException("Uninitialised compound child.",nameof(children));
    }
}

public readonly record struct CompoundSweepResult(ConvexSweepStatus Status,double Time,
    ColliderChildId? ChildA,ColliderChildId? ChildB,ConvexSeparationResult? Separation,int NarrowPhaseCalls);

/// <summary>A conservative AABB derived from support points, not shape types.</summary>
public readonly record struct CollisionBounds(CollisionVector Minimum,CollisionVector Maximum)
{
    public static CollisionBounds Of<T>(T shape) where T : IConvexSupport =>
        new(new(shape.Support(new(-1,0,0)).X,shape.Support(new(0,-1,0)).Y,shape.Support(new(0,0,-1)).Z),
            new(shape.Support(new(1,0,0)).X,shape.Support(new(0,1,0)).Y,shape.Support(new(0,0,1)).Z));

    public static CollisionBounds Swept(ConvexMotion motion,double duration)
    {
        motion.At(duration); // Validate time even when used only by the broad phase.
        var bounds=Of(motion.At(0)); var displacement=motion.LinearVelocity*duration;
        // Support planes can move by at most integrated angular speed times
        // the support derivative radius, and never by more than twice reach.
        // Zero rotation naturally gives exact translated child AABBs.
        var turn=Math.Min(2*motion.Reach,motion.AngularSpeedBound*motion.RotationalReach*duration);
        var minimum=bounds.Minimum+new CollisionVector(Math.Min(0,displacement.X)-turn,
            Math.Min(0,displacement.Y)-turn,Math.Min(0,displacement.Z)-turn);
        var maximum=bounds.Maximum+new CollisionVector(Math.Max(0,displacement.X)+turn,
            Math.Max(0,displacement.Y)+turn,Math.Max(0,displacement.Z)+turn);
        if(!minimum.IsFinite||!maximum.IsFinite) throw new InvalidOperationException("Swept bounds exceed numerical range.");
        return new(minimum,maximum);
    }

    public double DistanceLowerBound(CollisionBounds other)
    {
        var gap=new CollisionVector(Math.Max(0,Math.Max(other.Minimum.X-Maximum.X,Minimum.X-other.Maximum.X)),
            Math.Max(0,Math.Max(other.Minimum.Y-Maximum.Y,Minimum.Y-other.Maximum.Y)),
            Math.Max(0,Math.Max(other.Minimum.Z-Maximum.Z,Minimum.Z-other.Maximum.Z)));
        return gap.Length;
    }
}

public readonly struct CompoundMotion
{
    private readonly CompoundGeometry _geometry;
    private readonly IRigidTrajectory _trajectory;
    public int Count=>_geometry?.Count??throw new InvalidOperationException("Uninitialised compound motion.");
    public CompoundMotion(CompoundGeometry geometry,IRigidTrajectory trajectory)
    {
        ArgumentNullException.ThrowIfNull(geometry); ArgumentNullException.ThrowIfNull(trajectory);
        for(var i=0;i<geometry.Count;i++) _=new ConvexMotion(geometry[new(i)],trajectory);
        _geometry=geometry; _trajectory=trajectory;
    }
    public ConvexMotion Child(ColliderChildId id)
    {
        if(_geometry is null) throw new InvalidOperationException("Uninitialised compound motion.");
        return new(_geometry[id],_trajectory);
    }
}

/// <summary>Compound shapes share the same convex sweep, irrespective of
/// their source part. Child indices are stable declaration-order identities.</summary>
public static class CompoundCollision
{
    public static CompoundSweepResult Cast(CompoundMotion a,CompoundMotion b,double duration,double minimumSeparation)
    {
        if(!double.IsFinite(minimumSeparation)) throw new ArgumentOutOfRangeException(nameof(minimumSeparation));
        if(!double.IsFinite(duration)||duration<0) throw new ArgumentOutOfRangeException(nameof(duration));
        var best=new CompoundSweepResult(ConvexSweepStatus.Clear,duration,null,null,null,0);
        var calls=0;
        for(var i=0;i<a.Count;i++)
        {
            var first=a.Child(new(i));
            var firstBounds=CollisionBounds.Swept(first,duration);
            for(var j=0;j<b.Count;j++)
            {
                var second=b.Child(new(j));
                var secondBounds=CollisionBounds.Swept(second,duration);
                if(firstBounds.DistanceLowerBound(secondBounds)>Math.Max(0,minimumSeparation)+ConvexDistance.DefaultTolerance) continue;
                var hit=ConvexSweep.Cast(first,second,best.Time,minimumSeparation);
                calls++;
                if(hit.Status==ConvexSweepStatus.Clear) continue;
                if(best.Status!=ConvexSweepStatus.Clear&&hit.Time>=best.Time) continue;
                best=new(hit.Status,hit.Time,new(i),new(j),hit.Separation,calls);
            }
        }
        return best with { NarrowPhaseCalls=calls };
    }
}
