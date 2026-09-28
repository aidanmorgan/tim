using System;
using System.Collections.Generic;

namespace CuriousContraptions.Physics;

public readonly record struct SupportVertexId
{
    public int Value { get; }
    public SupportVertexId(int value)
    {
        if(value<0) throw new ArgumentOutOfRangeException(nameof(value));
        Value=value;
    }
}
public readonly record struct SupportVertex(SupportVertexId Id,CollisionVector Point);

/// <summary>Copied vertices of a supporting feature. Their convex combinations
/// remain inside the shape; projection never invents thickness or surface points.</summary>
public sealed class SupportFeature
{
    private readonly SupportVertex[] _vertices;
    public ReadOnlySpan<SupportVertex> Vertices=>_vertices;
    public SupportFeature(ReadOnlySpan<SupportVertex> vertices)
    {
        if(vertices.Length==0) throw new ArgumentException("A supporting feature cannot be empty.",nameof(vertices));
        _vertices=vertices.ToArray();
        var ids=new HashSet<SupportVertexId>();
        foreach(var vertex in _vertices)
            if(!vertex.Point.IsFinite||!ids.Add(vertex.Id))
                throw new ArgumentException("Feature vertices must be finite with unique identities.",nameof(vertices));
    }

    public static CollisionVector UnitDirection(CollisionVector direction,double planeTolerance)
    {
        if(!double.IsFinite(planeTolerance)||planeTolerance<0)
            throw new ArgumentOutOfRangeException(nameof(planeTolerance));
        var length=direction.Length;
        if(!direction.IsFinite||!double.IsFinite(length)||length==0)
            throw new ArgumentOutOfRangeException(nameof(direction));
        return direction/length;
    }

    public static SupportFeature FromPoints(ReadOnlySpan<CollisionVector> points,CollisionVector direction,double planeTolerance)
    {
        var normal=UnitDirection(direction,planeTolerance);
        if(points.Length==0) throw new ArgumentException("No support points.",nameof(points));
        var maximum=double.NegativeInfinity;
        foreach(var point in points)
        {
            var projection=CollisionVector.Dot(point,normal);
            if(!point.IsFinite||!double.IsFinite(projection)) throw new ArgumentOutOfRangeException(nameof(points));
            maximum=Math.Max(maximum,projection);
        }
        var vertices=new List<SupportVertex>();
        for(var i=0;i<points.Length;i++)
            if(maximum-CollisionVector.Dot(points[i],normal)<=planeTolerance)
                vertices.Add(new(new(i),points[i]));
        return new(vertices.ToArray());
    }
}

public interface IConvexFeatureSupport : IConvexSupport
{
    double RoundingRadius { get; }
    SupportFeature SupportingFeature(CollisionVector direction,double planeTolerance);
}
