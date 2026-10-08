using System;
using System.Collections.Generic;
using System.Linq;

namespace CuriousContraptions.Physics;

internal enum BodyPairDomain { Dynamic, Prescribed, Moving }
internal readonly record struct BodyPairIndices(int A,int B);
internal sealed record BodyCandidateResult(IReadOnlyList<BodyPairIndices> Pairs,int NodeTests,int LeafTests);

/// <summary>Persistent spatial topology over world declaration indices. Every query
/// refits all leaves from its own conservative motion bounds; no pose/horizon cache
/// survives a query. Returned candidates own their storage and use canonical order.</summary>
internal sealed class BodyBoundsTree
{
    private enum Axis { X,Y,Z }
    [Flags] private enum MotionSet { None=0, Static=1, Kinematic=2, Dynamic=4 }
    private sealed class Node(int index,MotionSet motions,Node? left,Node? right)
    {
        internal int Index { get; }=index;
        internal MotionSet Motions { get; }=motions;
        internal Node? Left { get; }=left;
        internal Node? Right { get; }=right;
        internal CollisionBounds? Bounds;
    }
    private readonly Node? _root;
    private readonly int _count;
    internal BodyBoundsTree(IReadOnlyList<PhysicsObject> objects)
    {
        ArgumentNullException.ThrowIfNull(objects);
        _count=objects.Count;
        if(_count==0)return;
        var entries=Enumerable.Range(0,_count).Select(i=>(Index:i,
            Center:objects[i].Body.Center,Motion:objects[i].Body.MotionType)).ToArray();
        Node Build(int start,int count)
        {
            if(count==1)
            {
                var entry=entries[start];
                var motion=entry.Motion switch
                {
                    PhysicsMotionType.Static=>MotionSet.Static,
                    PhysicsMotionType.Kinematic=>MotionSet.Kinematic,
                    PhysicsMotionType.Dynamic=>MotionSet.Dynamic,
                    _=>throw new ArgumentOutOfRangeException(nameof(objects))
                };
                return new(entry.Index,motion,null,null);
            }
            var min=entries[start].Center;var max=min;
            for(var i=start+1;i<start+count;i++)
            {
                var p=entries[i].Center;
                min=new(Math.Min(min.X,p.X),Math.Min(min.Y,p.Y),Math.Min(min.Z,p.Z));
                max=new(Math.Max(max.X,p.X),Math.Max(max.Y,p.Y),Math.Max(max.Z,p.Z));
            }
            var extent=max-min;
            var axis=extent.X>=extent.Y&&extent.X>=extent.Z?Axis.X:extent.Y>=extent.Z?Axis.Y:Axis.Z;
            Array.Sort(entries,start,count,Comparer<(int Index,CollisionVector Center,PhysicsMotionType Motion)>.Create((a,b)=>
            {
                var comparison=Component(a.Center,axis).CompareTo(Component(b.Center,axis));
                return comparison!=0?comparison:a.Index.CompareTo(b.Index);
            }));
            var half=count/2;var left=Build(start,half);var right=Build(start+half,count-half);
            return new(-1,left.Motions|right.Motions,left,right);
        }
        _root=Build(0,_count);
    }
    private static double Component(CollisionVector point,Axis axis)=>axis switch
    {
        Axis.X=>point.X,Axis.Y=>point.Y,Axis.Z=>point.Z,
        _=>throw new ArgumentOutOfRangeException(nameof(axis))
    };
    private static CollisionBounds Union(CollisionBounds a,CollisionBounds b)=>new(
        new(Math.Min(a.Minimum.X,b.Minimum.X),Math.Min(a.Minimum.Y,b.Minimum.Y),Math.Min(a.Minimum.Z,b.Minimum.Z)),
        new(Math.Max(a.Maximum.X,b.Maximum.X),Math.Max(a.Maximum.Y,b.Maximum.Y),Math.Max(a.Maximum.Z,b.Maximum.Z)));
    private static bool Eligible(MotionSet a,MotionSet b,BodyPairDomain domain)
    {
        var dynamic=(a&MotionSet.Dynamic)!=0||(b&MotionSet.Dynamic)!=0;
        var prescribed=(a&(MotionSet.Static|MotionSet.Kinematic))!=0&&
            (b&(MotionSet.Static|MotionSet.Kinematic))!=0&&
            ((a|b)&MotionSet.Kinematic)!=0;
        return domain switch
        {
            BodyPairDomain.Dynamic=>dynamic,BodyPairDomain.Prescribed=>prescribed,
            BodyPairDomain.Moving=>dynamic||prescribed,
            _=>throw new ArgumentOutOfRangeException(nameof(domain))
        };
    }
    internal BodyCandidateResult Query(Func<int,CollisionBounds?> bounds,BodyPairDomain domain,double margin)
    {
        ArgumentNullException.ThrowIfNull(bounds);
        if(!Enum.IsDefined(domain)||!double.IsFinite(margin)||margin<0)
            throw new ArgumentOutOfRangeException(nameof(domain));
        CollisionBounds? Refit(Node node)
        {
            if(node.Index>=0)
            {
                var value=bounds(node.Index);
                if(value is { } box&&(!box.Minimum.IsFinite||!box.Maximum.IsFinite||
                    box.Minimum.X>box.Maximum.X||box.Minimum.Y>box.Maximum.Y||box.Minimum.Z>box.Maximum.Z))
                    throw new ArgumentException("Body bounds must be finite and ordered.",nameof(bounds));
                return node.Bounds=value;
            }
            var left=Refit(node.Left!);var right=Refit(node.Right!);
            return node.Bounds=left is { } a&&right is { } b?Union(a,b):left??right;
        }
        var pairs=new List<BodyPairIndices>();var tests=0;var leafTests=0;
        void Visit(Node a,Node b)
        {
            if(!Eligible(a.Motions,b.Motions,domain)||a.Bounds is not { } first||b.Bounds is not { } second)return;
            tests=checked(tests+1);
            if(a.Index>=0&&b.Index>=0&&!ReferenceEquals(a,b))leafTests=checked(leafTests+1);
            if(first.DistanceLowerBound(second)>margin)return;
            if(ReferenceEquals(a,b))
            {
                if(a.Index>=0)return;
                Visit(a.Left!,a.Left!);Visit(a.Left!,a.Right!);Visit(a.Right!,a.Right!);return;
            }
            if(a.Index>=0&&b.Index>=0)
            {
                pairs.Add(a.Index<b.Index?new(a.Index,b.Index):new(b.Index,a.Index));return;
            }
            if(a.Index<0){Visit(a.Left!,b);Visit(a.Right!,b);}
            else{Visit(a,b.Left!);Visit(a,b.Right!);}
        }
        if(_root is not null){Refit(_root);Visit(_root,_root);}
        pairs.Sort((a,b)=>{var order=a.A.CompareTo(b.A);return order!=0?order:a.B.CompareTo(b.B);});
        return new(pairs.AsReadOnly(),tests,leafTests);
    }
}
