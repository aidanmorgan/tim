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
    internal CompoundBoundsTree Tree { get; }
    public int Count=>_children.Length;
    public ConvexInstance this[ColliderChildId child]=>_children[child.Index];
    public CompoundGeometry(ReadOnlySpan<ConvexInstance> children)
    {
        if(children.Length==0) throw new ArgumentException("A compound requires convex children.",nameof(children));
        _children=children.ToArray();
        foreach(var child in _children)
            if(child.Geometry is null) throw new ArgumentException("Uninitialised compound child.",nameof(children));
        Tree=new(this);
    }
}

public readonly record struct CompoundOverlapResult(ColliderChildId ChildA,ColliderChildId ChildB,
    ConvexSeparationResult Separation);

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
        var bounds=Of(motion.At(0)); var displacement=motion.Trajectory.At(duration).Center-motion.CenterAtStart;
        // Support planes can move by at most integrated angular speed times
        // the support derivative radius, and never by more than twice reach.
        // Zero rotation and acceleration give exact translated child AABBs.
        // Smooth translation stays within its acceleration bound * duration² / 8
        // of its endpoint chord, including a return to the start position.
        var turn=Math.Min(2*motion.Reach,motion.AngularSpeedBound*motion.RotationalReach*duration)+
            motion.LinearAccelerationBound*duration*duration/8;
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
    internal CompoundGeometry Geometry=>_geometry??throw new InvalidOperationException("Uninitialised compound motion.");
    internal IRigidTrajectory Trajectory=>_trajectory??throw new InvalidOperationException("Uninitialised compound motion.");
    public int Count=>_geometry?.Count??throw new InvalidOperationException("Uninitialised compound motion.");
    public CompoundMotion(CompoundGeometry geometry,IRigidTrajectory trajectory)
    {
        ArgumentNullException.ThrowIfNull(geometry); ArgumentNullException.ThrowIfNull(trajectory);
        if(!double.IsFinite(trajectory.AngularSpeedBound*geometry.Tree.Root.Reach))
            throw new ArgumentOutOfRangeException(nameof(trajectory));
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
    /// <summary>First declaration-ordered pair exceeding the allowed initial
    /// penetration. Uncertain separation bounds reject conservatively.</summary>
    public static CompoundOverlapResult? FindOverlap(CompoundMotion a,CompoundMotion b,double maximumPenetration,out CompoundCandidateResult candidates)
    {
        if(!double.IsFinite(maximumPenetration)||maximumPenetration<0)
            throw new ArgumentOutOfRangeException(nameof(maximumPenetration));
        candidates=Candidates(a,b,0,0);
        foreach(var pair in candidates.Pairs)
        {
            var separation=ConvexSeparation.Query(a.Child(pair.A).At(0),b.Child(pair.B).At(0));
            if(separation.LowerBound < -maximumPenetration) return new(pair.A,pair.B,separation);
        }
        return null;
    }

    public static CompoundCandidateResult Candidates(CompoundMotion a,CompoundMotion b,double duration,double margin)=>
        CompoundBoundsTree.Query(a,b,duration,margin);

    public static CompoundSweepResult Cast(CompoundMotion a,CompoundMotion b,double duration,double minimumSeparation)
    {
        if(!double.IsFinite(minimumSeparation)) throw new ArgumentOutOfRangeException(nameof(minimumSeparation));
        if(!double.IsFinite(duration)||duration<0) throw new ArgumentOutOfRangeException(nameof(duration));
        var best=new CompoundSweepResult(ConvexSweepStatus.Clear,duration,null,null,null,0);
        var calls=0;
        foreach(var pair in Candidates(a,b,duration,Math.Max(0,minimumSeparation)+ConvexDistance.DefaultTolerance).Pairs)
        {
            var hit=ConvexSweep.Cast(a.Child(pair.A),b.Child(pair.B),best.Time,minimumSeparation);
            calls++;
            if(hit.Status==ConvexSweepStatus.Clear) continue;
            if(best.Status!=ConvexSweepStatus.Clear&&hit.Time>=best.Time) continue;
            best=new(hit.Status,hit.Time,pair.A,pair.B,hit.Separation,calls);
        }
        return best with { NarrowPhaseCalls=calls };
    }
}
