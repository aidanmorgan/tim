using System;
using System.Buffers;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

public readonly record struct ColliderChildPair(ColliderChildId A,ColliderChildId B);
public sealed record CompoundCandidateResult(IReadOnlyList<ColliderChildPair> Pairs,int NodeTests,int LeafTests);

internal readonly record struct CompoundNodeIndex(int Value);

/// <summary>Immutable median-split hierarchy over declaration-order child IDs.
/// Only bounds are partitioned; geometry and identity order never change.</summary>
internal sealed class CompoundBoundsTree
{
    private enum Axis { X,Y,Z }
    internal sealed class Node(CompoundNodeIndex index,CollisionBounds bounds,double reach,int count,ColliderChildId? child,Node? left,Node? right)
    {
        internal CompoundNodeIndex Index { get; }=index;
        internal CollisionBounds Bounds { get; }=bounds;
        internal double Reach { get; }=reach;
        internal int Count { get; }=count;
        internal ColliderChildId? Child { get; }=child;
        internal Node? Left { get; }=left;
        internal Node? Right { get; }=right;
    }
    internal Node Root { get; }
    internal int NodeCount { get; }
    internal CompoundBoundsTree(CompoundGeometry geometry)
    {
        var entries=new (ColliderChildId Id,CollisionBounds Bounds)[geometry.Count];
        for(var i=0;i<entries.Length;i++) entries[i]=(new(i),CollisionBounds.Of(geometry[new(i)]));
        var next=0;
        Root=Build(entries,0,entries.Length,ref next);
        NodeCount=next;
    }
    private static double Component(CollisionVector v,Axis axis)=>axis switch
    {
        Axis.X=>v.X,Axis.Y=>v.Y,Axis.Z=>v.Z,_=>throw new ArgumentOutOfRangeException(nameof(axis))
    };
    private static Node Build((ColliderChildId Id,CollisionBounds Bounds)[] entries,int start,int count,ref int next)
    {
        var index=new CompoundNodeIndex(next++);
        var bounds=entries[start].Bounds;
        for(var i=start+1;i<start+count;i++) bounds=Union(bounds,entries[i].Bounds);
        var far=new CollisionVector(Math.Max(Math.Abs(bounds.Minimum.X),Math.Abs(bounds.Maximum.X)),
            Math.Max(Math.Abs(bounds.Minimum.Y),Math.Abs(bounds.Maximum.Y)),Math.Max(Math.Abs(bounds.Minimum.Z),Math.Abs(bounds.Maximum.Z)));
        if(!bounds.Minimum.IsFinite||!bounds.Maximum.IsFinite||!double.IsFinite(far.Length))
            throw new ArgumentException("Compound bounds exceed numeric range.");
        if(count==1) return new(index,bounds,far.Length,count,entries[start].Id,null,null);
        var extent=bounds.Maximum-bounds.Minimum;
        var axis=extent.X>=extent.Y&&extent.X>=extent.Z?Axis.X:extent.Y>=extent.Z?Axis.Y:Axis.Z;
        Array.Sort(entries,start,count,Comparer<(ColliderChildId Id,CollisionBounds Bounds)>.Create((a,b)=>
        {
            var value=(Component(a.Bounds.Minimum,axis)*.5+Component(a.Bounds.Maximum,axis)*.5)
                .CompareTo(Component(b.Bounds.Minimum,axis)*.5+Component(b.Bounds.Maximum,axis)*.5);
            return value!=0?value:a.Id.Index.CompareTo(b.Id.Index);
        }));
        var half=count/2;
        return new(index,bounds,far.Length,count,null,Build(entries,start,half,ref next),Build(entries,start+half,count-half,ref next));
    }
    private static CollisionBounds Union(CollisionBounds a,CollisionBounds b)=>new(
        new(Math.Min(a.Minimum.X,b.Minimum.X),Math.Min(a.Minimum.Y,b.Minimum.Y),Math.Min(a.Minimum.Z,b.Minimum.Z)),
        new(Math.Max(a.Maximum.X,b.Maximum.X),Math.Max(a.Maximum.Y,b.Maximum.Y),Math.Max(a.Maximum.Z,b.Maximum.Z)));

    internal static CollisionBounds Swept(Node node,IRigidTrajectory path,double duration)
    {
        path.At(duration);
        var center=node.Bounds.Minimum*.5+node.Bounds.Maximum*.5;
        var half=node.Bounds.Maximum*.5-node.Bounds.Minimum*.5;
        var pose=path.StartPose;
        var x=pose.Rotation.Apply(new(half.X,0,0));
        var y=pose.Rotation.Apply(new(0,half.Y,0));
        var z=pose.Rotation.Apply(new(0,0,half.Z));
        var extent=new CollisionVector(Math.Abs(x.X)+Math.Abs(y.X)+Math.Abs(z.X),
            Math.Abs(x.Y)+Math.Abs(y.Y)+Math.Abs(z.Y),Math.Abs(x.Z)+Math.Abs(y.Z)+Math.Abs(z.Z));
        center=pose.TransformPoint(center);
        var displacement=path.At(duration).Center-path.StartPose.Center;
        var turn=Math.Min(2*node.Reach,path.AngularSpeedBound*node.Reach*duration)+path.LinearAccelerationBound*duration*duration/8;
        var reserve=64*Math.ScaleB(1,-52)*Math.Max(1,center.Length+extent.Length+displacement.Length+turn);
        turn+=reserve;
        var minimum=center-extent+new CollisionVector(Math.Min(0,displacement.X)-turn,Math.Min(0,displacement.Y)-turn,Math.Min(0,displacement.Z)-turn);
        var maximum=center+extent+new CollisionVector(Math.Max(0,displacement.X)+turn,Math.Max(0,displacement.Y)+turn,Math.Max(0,displacement.Z)+turn);
        if(!minimum.IsFinite||!maximum.IsFinite) throw new InvalidOperationException("Hierarchy motion bounds exceed numeric range.");
        return new(minimum,maximum);
    }

    internal static CompoundCandidateResult Query(CompoundMotion a,CompoundMotion b,double duration,double margin)
    {
        if(!double.IsFinite(duration)||duration<0||!double.IsFinite(margin)||margin<0) throw new ArgumentOutOfRangeException(nameof(duration));
        a.Trajectory.At(duration); b.Trajectory.At(duration);
        // Query-scoped leases: no geometry/path references survive return. Both
        // motions may share the same tree, but occupy disjoint cache ranges.
        var offset=a.Geometry.Tree.NodeCount;
        var count=checked(offset+b.Geometry.Tree.NodeCount);
        var cache=ArrayPool<CollisionBounds>.Shared.Rent(count);
        try
        {
            var computed=ArrayPool<bool>.Shared.Rent(count);
            try
            {
                Array.Clear(computed,0,count);
                CollisionBounds Bounds(Node node,IRigidTrajectory path,int start)
                {
                    var index=start+node.Index.Value;
                    if(!computed[index])
                    {
                        cache[index]=Swept(node,path,duration);
                        computed[index]=true;
                    }
                    return cache[index];
                }
                var pairs=new List<ColliderChildPair>();var nodeTests=0;var leafTests=0;
                void Visit(Node first,Node second)
                {
                    nodeTests++;
                    if(Bounds(first,a.Trajectory,0).DistanceLowerBound(Bounds(second,b.Trajectory,offset))>margin)return;
                    if(first.Child is { } ia&&second.Child is { } ib)
                    {
                        leafTests++;
                        if(CollisionBounds.Swept(a.Child(ia),duration).DistanceLowerBound(CollisionBounds.Swept(b.Child(ib),duration))<=margin)
                            pairs.Add(new(ia,ib));
                        return;
                    }
                    if(first.Child is null&&(second.Child is not null||first.Count>=second.Count))
                    { Visit(first.Left!,second);Visit(first.Right!,second); }
                    else { Visit(first,second.Left!);Visit(first,second.Right!); }
                }
                Visit(a.Geometry.Tree.Root,b.Geometry.Tree.Root);
                pairs.Sort((x,y)=>{var value=x.A.Index.CompareTo(y.A.Index);return value!=0?value:x.B.Index.CompareTo(y.B.Index);});
                return new(pairs.AsReadOnly(),nodeTests,leafTests);
            }
            finally { ArrayPool<bool>.Shared.Return(computed); }
        }
        finally { ArrayPool<CollisionBounds>.Shared.Return(cache); }
    }
}
