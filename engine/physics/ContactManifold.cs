using System;

namespace CuriousContraptions.Physics;

public enum ContactManifoldStatus { Clear, Contact }
public sealed class ContactManifold
{
    private readonly ContactPatchPoint[] _points;
    public ContactManifoldStatus Status { get; }
    public CollisionVector Normal { get; }
    public ReadOnlySpan<ContactPatchPoint> Points=>_points;
    private ContactManifold(ContactManifoldStatus status,CollisionVector normal,ContactPatchPoint[] points)
    { Status=status; Normal=normal; _points=points; }

    /// <summary>Distance/penetration bounds choose the shared normal. Certified
    /// witnesses participate in both supporting features on every query; this
    /// preserves curved contacts within the query's bounded angular uncertainty.
    /// No pair dispatch or empty-patch substitution is used.</summary>
    public static ContactManifold Query<TA,TB>(TA a,TB b,double contactDistance=ConvexSweep.ContactDistance,
        double tolerance=ConvexDistance.DefaultTolerance)
        where TA:IConvexFeatureSupport where TB:IConvexFeatureSupport
    {
        if(!double.IsFinite(contactDistance)||contactDistance<0) throw new ArgumentOutOfRangeException(nameof(contactDistance));
        var distance=ConvexDistance.Query(a,b,tolerance);
        if(distance.LowerBound>contactDistance) return new(ContactManifoldStatus.Clear,distance.Normal,[]);
        CollisionVector normal,pointA,pointB;
        if(distance.Status==ConvexDistanceStatus.Separated)
        { normal=distance.Normal; pointA=distance.PointA; pointB=distance.PointB; }
        else
        {
            var penetration=ConvexPenetration.Query(a,b,tolerance);
            normal=penetration.Normal; pointA=penetration.PointA; pointB=penetration.PointB;
        }
        if(!normal.IsFinite||Math.Abs(normal.Length-1)>1e-10)
            throw new InvalidOperationException("A physical contact requires a defined unit normal.");
        var featureA=IncludeWitness(a.SupportingFeature(-normal,tolerance),pointA);
        var featureB=IncludeWitness(b.SupportingFeature(normal,tolerance),pointB);
        var patch=ContactPatch.Clip(featureA,featureB,normal,tolerance);
        if(patch.Points.Length==0) throw new InvalidOperationException("Certified contact features did not intersect.");
        foreach(var contact in patch.Points)
            if(contact.Separation>contactDistance+3*tolerance)
                throw new InvalidOperationException("Contact patch exceeds the certified separation bound.");
        return new(ContactManifoldStatus.Contact,normal,patch.Points.ToArray());
    }

    private static SupportFeature IncludeWitness(SupportFeature feature,CollisionVector point)
    {
        var vertices=new SupportVertex[feature.Vertices.Length+1];
        var maximum=0;
        for(var i=0;i<feature.Vertices.Length;i++)
        {
            vertices[i]=feature.Vertices[i];
            maximum=Math.Max(maximum,vertices[i].Id.Value);
        }
        // This is a query-local geometric sample, not a persistent feature ID.
        vertices[^1]=new(new(checked(maximum+1)),point);
        return new(vertices);
    }
}
